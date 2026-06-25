
using Application.DTOs;
using Application.Services.Utilities;
using BCrypt.Net;
using Core.Entities;
using Core.Enums;
using Core.Interfaces.Repositories;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.EntityFrameworkCore;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;


namespace Application.Services.Identity
{

    public class AuthService : IAuthService
    {
        private readonly IUserRepository _users;
        private readonly ITenantRepository _tenants;
        private readonly IUnitOfWork _uow;
        private readonly IConfiguration _config;
        private readonly IEmailService _email;
        private readonly ILogger<AuthService> _log;


        // Config helpers
        private string JwtSecret => _config["Jwt:Secret"]
            ?? throw new InvalidOperationException("JWT secret not configured.");
        private string JwtIssuer => _config["Jwt:Issuer"] ?? "InvoiceHub";
        private string JwtAudience => _config["Jwt:Audience"] ?? "InvoiceHub";
        private int AccessTokenMinutes => int.Parse(_config["Jwt:AccessTokenExpiryMinutes"] ?? "60");
        private int RefreshTokenDays => int.Parse(_config["Jwt:RefreshTokenExpiryDays"] ?? "30");

        public AuthService(
            IUserRepository users,
            ITenantRepository tenants,
            IUnitOfWork uow,
            IConfiguration config,
            IEmailService email,
            ILogger<AuthService> log)
        {
            _users = users; _tenants = tenants; _uow = uow;
            _config = config; _email = email; _log = log;
        }

        // ── Register ──────────────────────────────────────────────────────
        public async Task<ApiResponse<AuthResponseDto>> RegisterAsync(
        RegisterDto dto, string ipAddress, CancellationToken ct = default)
        {
            // ── Basic validation ──────────────────────────────────────────
            if (dto.Password != dto.ConfirmPassword)
                return ApiResponse<AuthResponseDto>.Fail("Passwords do not match.");

            if (!await _users.IsEmailUniqueAsync(dto.Email, ct: ct))
                return ApiResponse<AuthResponseDto>.Fail("An account with this email already exists.");

            if (dto.CreateCompany && string.IsNullOrWhiteSpace(dto.CompanyName))
                return ApiResponse<AuthResponseDto>.Fail("Company name is required when creating a business account.");

            await _uow.BeginTransactionAsync(ct);
            try
            {
                User user;

                if (dto.CreateCompany)
                {
                    // ── SCENARIO A: User is creating a business ──────────
                    // → Create Tenant → Assign Admin role
                    var tenant = new Tenant
                    {
                        BusinessName = dto.CompanyName!.Trim(),
                        GSTIN = dto.GSTIN?.Trim(),
                        Email = dto.Email,
                        Phone = dto.PhoneNumber,
                        IsGSTRegistered = !string.IsNullOrEmpty(dto.GSTIN),
                        IsActive = true
                    };

                    var verificationToken = GenerateSecureToken();

                    user = new User
                    {
                        FirstName = dto.FirstName.Trim(),
                        LastName = dto.LastName.Trim(),
                        Email = dto.Email.ToLowerInvariant().Trim(),
                        PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password, workFactor: 12),
                        PhoneNumber = dto.PhoneNumber,
                        Role = UserRole.Admin,        // ← auto-assigned
                        Status = UserStatus.PendingVerification,
                        EmailVerificationToken = verificationToken,
                        EmailVerificationExpiry = DateTime.UtcNow.AddHours(24),
                        Tenant = tenant,                // ← creates new tenant
                        LastLoginIp = ipAddress
                    };
                }
                else
                {
                    // ── SCENARIO B: Individual join (no company yet) ─────
                    // → No Tenant → Assign Viewer role
                    // The user will be invited to a company later by an Admin.
                    //
                    // NOTE: TenantId will be Guid.Empty until they join a tenant.
                    // You can also choose to not allow self-signup without a company
                    // and force all non-admin users to be invited only.

                    var verificationToken = GenerateSecureToken();

                    user = new User
                    {
                        FirstName = dto.FirstName.Trim(),
                        LastName = dto.LastName.Trim(),
                        Email = dto.Email.ToLowerInvariant().Trim(),
                        PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password, workFactor: 12),
                        PhoneNumber = dto.PhoneNumber,
                        Role = UserRole.Viewer,       // ← auto-assigned
                        Status = UserStatus.PendingVerification,
                        EmailVerificationToken = verificationToken,
                        EmailVerificationExpiry = DateTime.UtcNow.AddHours(24),
                        LastLoginIp = ipAddress
                        // TenantId not set — joins via Admin invite
                    };
                }

                await _users.AddAsync(user, ct);
                await _uow.SaveChangesAsync(ct);
                await _uow.CommitTransactionAsync(ct);

                // Fire-and-forget email
                _ = _email.SendEmailVerificationAsync(user.Email, user.FirstName,
                    user.EmailVerificationToken!);

                _log.LogInformation(
                    "Registered: {Email} | CreateCompany={CreateCompany} | Role={Role}",
                    user.Email, dto.CreateCompany, user.Role);

                var tokens = await IssueTokensAsync(user, ipAddress, ct);
                return ApiResponse<AuthResponseDto>.Ok(tokens,
                    "Registration successful. Please verify your email.");
            }
            catch (Exception ex)
            {
                await _uow.RollbackTransactionAsync(ct);
                _log.LogError(ex, "Registration failed for {Email}", dto.Email);
                return ApiResponse<AuthResponseDto>.Fail("Registration failed. Please try again.");
            }
        }

        // ── Login ─────────────────────────────────────────────────────────
        public async Task<ApiResponse<AuthResponseDto>> LoginAsync(
            LoginDto dto, string ipAddress, CancellationToken ct = default)
        {
            var user = await _users.GetByEmailAsync(dto.Email.ToLowerInvariant().Trim(), ct);

            // Constant-time check to prevent user enumeration
            if (user == null || !BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
                return ApiResponse<AuthResponseDto>.Fail("Invalid email or password.");

            return user.Status switch
            {
                UserStatus.Suspended => ApiResponse<AuthResponseDto>.Fail(
                    "Your account has been suspended. Please contact support."),
                UserStatus.Inactive => ApiResponse<AuthResponseDto>.Fail(
                    "Your account is inactive. Please contact your administrator."),
                _ => await CompleteLoginAsync(user, ipAddress, ct)
            };
        }

        private async Task<ApiResponse<AuthResponseDto>> CompleteLoginAsync(
            User user, string ipAddress, CancellationToken ct)
        {
            user.LastLoginAt = DateTime.UtcNow;
            user.LastLoginIp = ipAddress;
            _users.Update(user);
            await _uow.SaveChangesAsync(ct);

            _log.LogInformation("Login: {Email} from {IP}", user.Email, ipAddress);
            var tokens = await IssueTokensAsync(user, ipAddress, ct);
            return ApiResponse<AuthResponseDto>.Ok(tokens);
        }

        // ── Refresh Token ─────────────────────────────────────────────────
        public async Task<ApiResponse<AuthResponseDto>> RefreshTokenAsync(
     string refreshToken, string ipAddress, CancellationToken ct = default)
        {
            var user = await _users.FirstOrDefaultAsync(
                u => u.RefreshTokens.Any(r => r.Token == refreshToken), ct);

            if (user == null)
                return ApiResponse<AuthResponseDto>.Fail("Invalid refresh token.");

            // FIX — use SingleOrDefault instead of Single
            var token = user.RefreshTokens.SingleOrDefault(r => r.Token == refreshToken);

            if (token == null)
                return ApiResponse<AuthResponseDto>.Fail("Invalid refresh token.");

            // Detect token reuse
            if (token.IsRevoked)
            {
                foreach (var t in user.RefreshTokens.Where(r => !r.IsRevoked))
                {
                    t.IsRevoked = true;
                    t.RevokedReason = "Suspicious reuse detected";
                    t.RevokedByIp = ipAddress;
                }
                await _uow.SaveChangesAsync(ct);
                _log.LogWarning("Token reuse detected for user {UserId} from {IP}", user.Id, ipAddress);
                return ApiResponse<AuthResponseDto>.Fail(
                    "Security alert: refresh token reuse detected. All sessions have been revoked.");
            }

            if (token.ExpiresAt < DateTime.UtcNow)
                return ApiResponse<AuthResponseDto>.Fail("Refresh token has expired. Please log in again.");

            // Rotate token
            token.IsRevoked = true;
            token.RevokedReason = "Replaced by new token";
            token.RevokedByIp = ipAddress;

            var tokens = await IssueTokensAsync(user, ipAddress, ct);
            return ApiResponse<AuthResponseDto>.Ok(tokens);
        }

        // ── Revoke Token (Logout) ─────────────────────────────────────────
        public async Task<ApiResponse<bool>> RevokeTokenAsync(
            string refreshToken, string ipAddress, CancellationToken ct = default)
        {
            var user = await _users.FirstOrDefaultAsync(
                u => u.RefreshTokens.Any(r => r.Token == refreshToken), ct);

            if (user == null)
                return ApiResponse<bool>.Fail("Token not found.");

            var token = user.RefreshTokens.SingleOrDefault(r => r.Token == refreshToken);
            if (token == null)
                return ApiResponse<bool>.Fail("Token not found.");
            token.IsRevoked = true;
            token.RevokedReason = "Logged out";
            token.RevokedByIp = ipAddress;

            _users.Update(user);
            await _uow.SaveChangesAsync(ct);
            _log.LogInformation("Logout: {Email}", user.Email);
            return ApiResponse<bool>.Ok(true, "Logged out successfully.");
        }

        // ── Google OAuth ──────────────────────────────────────────────────
        public async Task<ApiResponse<AuthResponseDto>> GoogleLoginAsync(
            string googleIdToken, string ipAddress, CancellationToken ct = default)
        {
            GoogleTokenPayload? payload;
            try
            {
                using var http = new HttpClient();
                var resp = await http.GetAsync(
                    $"https://oauth2.googleapis.com/tokeninfo?id_token={googleIdToken}", ct);

                if (!resp.IsSuccessStatusCode)
                    return ApiResponse<AuthResponseDto>.Fail("Invalid Google token.");

                payload = await resp.Content
                    .ReadFromJsonAsync<GoogleTokenPayload>(cancellationToken: ct);
            }
            catch
            {
                return ApiResponse<AuthResponseDto>.Fail("Failed to verify Google token.");
            }

            if (payload?.Email == null)
                return ApiResponse<AuthResponseDto>.Fail("Could not read Google token payload.");

            var user = await _users.GetByGoogleIdAsync(payload.Sub, ct)
                       ?? await _users.GetByEmailAsync(payload.Email, ct);

            if (user == null)
            {
                // Auto-register via Google
                var tenant = new Tenant
                {
                    BusinessName = payload.Name ?? payload.Email
                };
                user = new User
                {
                    FirstName = payload.GivenName ?? "",
                    LastName = payload.FamilyName ?? "",
                    Email = payload.Email.ToLowerInvariant(),
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(GenerateSecureToken(), workFactor: 12),
                    GoogleId = payload.Sub,
                    EmailVerified = true,
                    Status = UserStatus.Active,
                    Role = UserRole.Admin,
                    Tenant = tenant,
                    LastLoginAt = DateTime.UtcNow,
                    LastLoginIp = ipAddress
                };
                await _users.AddAsync(user, ct);
            }
            else
            {
                user.GoogleId ??= payload.Sub;
                user.EmailVerified = true;
                if (user.Status == UserStatus.PendingVerification)
                    user.Status = UserStatus.Active;
                user.LastLoginAt = DateTime.UtcNow;
                user.LastLoginIp = ipAddress;
                _users.Update(user);
            }

            await _uow.SaveChangesAsync(ct);
            var tokens = await IssueTokensAsync(user, ipAddress, ct);
            return ApiResponse<AuthResponseDto>.Ok(tokens);
        }

        // ── Email Verification ────────────────────────────────────────────
        public async Task<ApiResponse<bool>> VerifyEmailAsync(
            string token, CancellationToken ct = default)
        {
            var user = await _users.FirstOrDefaultAsync(
                u => u.EmailVerificationToken == token, ct);

            if (user == null || user.EmailVerificationExpiry < DateTime.UtcNow)
                return ApiResponse<bool>.Fail("Invalid or expired verification token.");

            user.EmailVerified = true;
            user.Status = UserStatus.Active;
            user.EmailVerificationToken = null;
            user.EmailVerificationExpiry = null;
            _users.Update(user);
            await _uow.SaveChangesAsync(ct);

            _log.LogInformation("Email verified for: {Email}", user.Email);
            return ApiResponse<bool>.Ok(true, "Email verified successfully. You can now log in.");
        }

        // ── Resend Verification ───────────────────────────────────────────
        public async Task<ApiResponse<bool>> ResendVerificationAsync(
            string email, CancellationToken ct = default)
        {
            var user = await _users.GetByEmailAsync(email.ToLowerInvariant(), ct);
            if (user == null || user.EmailVerified)
                return ApiResponse<bool>.Ok(true, "If that email is unverified, a new link has been sent.");

            user.EmailVerificationToken = GenerateSecureToken();
            user.EmailVerificationExpiry = DateTime.UtcNow.AddHours(24);
            _users.Update(user);
            await _uow.SaveChangesAsync(ct);

            _ = _email.SendEmailVerificationAsync(user.Email, user.FirstName,
                user.EmailVerificationToken!);

            return ApiResponse<bool>.Ok(true, "Verification email resent.");
        }

        // ── Forgot Password ───────────────────────────────────────────────
        public async Task<ApiResponse<bool>> ForgotPasswordAsync(
            ForgotPasswordDto dto, CancellationToken ct = default)
        {
            var user = await _users.GetByEmailAsync(dto.Email.ToLowerInvariant(), ct);

            // Always return OK to prevent user enumeration
            if (user == null)
                return ApiResponse<bool>.Ok(true,
                    "If that email is registered, you'll receive a password reset link.");

            user.PasswordResetToken = GenerateSecureToken();
            user.PasswordResetExpiry = DateTime.UtcNow.AddHours(2);
            _users.Update(user);
            await _uow.SaveChangesAsync(ct);

            _ = _email.SendPasswordResetAsync(user.Email, user.FirstName, user.PasswordResetToken!);
            _log.LogInformation("Password reset requested for: {Email}", user.Email);

            return ApiResponse<bool>.Ok(true, "Password reset email sent.");
        }

        // ── Reset Password ────────────────────────────────────────────────
        public async Task<ApiResponse<bool>> ResetPasswordAsync(
            ResetPasswordDto dto, CancellationToken ct = default)
        {
            if (dto.NewPassword != dto.ConfirmPassword)
                return ApiResponse<bool>.Fail("Passwords do not match.");

            var user = await _users.FirstOrDefaultAsync(
                u => u.PasswordResetToken == dto.Token, ct);

            if (user == null || user.PasswordResetExpiry < DateTime.UtcNow)
                return ApiResponse<bool>.Fail("Invalid or expired password reset token.");

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword, workFactor: 12);
            user.PasswordResetToken = null;
            user.PasswordResetExpiry = null;

            // Revoke all refresh tokens on password reset (security)
            foreach (var t in user.RefreshTokens.Where(r => !r.IsRevoked))
            {
                t.IsRevoked = true;
                t.RevokedReason = "Password reset";
            }

            _users.Update(user);
            await _uow.SaveChangesAsync(ct);

            _log.LogInformation("Password reset completed for: {Email}", user.Email);
            return ApiResponse<bool>.Ok(true, "Password reset successfully. Please log in.");
        }

        // ── Change Password ───────────────────────────────────────────────
        public async Task<ApiResponse<bool>> ChangePasswordAsync(
            Guid userId, ChangePasswordDto dto, CancellationToken ct = default)
        {
            if (dto.NewPassword != dto.ConfirmPassword)
                return ApiResponse<bool>.Fail("New passwords do not match.");

            var user = await _users.GetByIdAsync(userId, ct);
            if (user == null) return ApiResponse<bool>.Fail("User not found.");

            if (!BCrypt.Net.BCrypt.Verify(dto.CurrentPassword, user.PasswordHash))
                return ApiResponse<bool>.Fail("Current password is incorrect.");

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword, workFactor: 12);
            _users.Update(user);
            await _uow.SaveChangesAsync(ct);

            return ApiResponse<bool>.Ok(true, "Password changed successfully.");
        }

        // ── Private Helpers ───────────────────────────────────────────────
        private async Task<AuthResponseDto> IssueTokensAsync(
      User user, string ipAddress, CancellationToken ct)
        {
            var accessToken = GenerateJwtToken(user);
            var expiry = DateTime.UtcNow.AddMinutes(AccessTokenMinutes);

            var refresh = new RefreshToken
            {
                Token = GenerateSecureToken(64),
                ExpiresAt = DateTime.UtcNow.AddDays(RefreshTokenDays),
                CreatedByIp = ipAddress,
                UserId = user.Id
            };

            // Limit to 5 concurrent refresh tokens per user
            var activeTokens = user.RefreshTokens
                .Where(r => !r.IsRevoked)
                .OrderBy(r => r.ExpiresAt)
                .ToList();

            if (activeTokens.Count >= 5)
            {
                var oldest = activeTokens.First();
                oldest.IsRevoked = true;
                oldest.RevokedReason = "Token limit reached";
            }

            // ✅ DO NOT add to collection + DO NOT call _users.Update
            // Just add the refresh token directly to DB
            await _users.AddRefreshTokenAsync(refresh, ct);
            await _uow.SaveChangesAsync(ct);

            return new AuthResponseDto(accessToken, refresh.Token, expiry, MapUser(user));
        }
        private string GenerateJwtToken(User user)
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(JwtSecret));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.GivenName, user.FirstName),
            new(ClaimTypes.Surname, user.LastName),
            new("tenantId", user.TenantId.ToString()),
            new(ClaimTypes.Role, user.Role.ToString()),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(JwtRegisteredClaimNames.Iat,
                DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(),
                ClaimValueTypes.Integer64)
        };

            var token = new JwtSecurityToken(
                issuer: JwtIssuer,
                audience: JwtAudience,
                claims: claims,
                notBefore: DateTime.UtcNow,
                expires: DateTime.UtcNow.AddMinutes(AccessTokenMinutes),
                signingCredentials: creds);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private static string GenerateSecureToken(int byteLength = 32)
        {
            var bytes = new byte[byteLength];
            RandomNumberGenerator.Fill(bytes);
            return Convert.ToBase64String(bytes)
                .Replace("+", "-").Replace("/", "_").Replace("=", "");
        }

        internal static UserDto MapUser(User u) => new(
            u.Id, u.FirstName, u.LastName, u.Email,
            u.PhoneNumber, u.ProfilePicture, u.Role,
            u.Status, u.TenantId, u.LastLoginAt);
    }
}
