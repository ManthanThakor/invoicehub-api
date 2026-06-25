using Core.Enums;

namespace Application.DTOs;

public record LoginDto(string Email, string Password);

public record RegisterDto(
    string FirstName,
    string LastName,
    string Email,
    string Password,
    string ConfirmPassword,
    string? CompanyName = null,
    string? GSTIN = null,
    string? PhoneNumber = null,
    bool CreateCompany = false
);

public record AuthResponseDto(
    string AccessToken,
    string RefreshToken,
    DateTime AccessTokenExpiry,
    UserDto User
);

public record ResetPasswordDto(string Token, string NewPassword, string ConfirmPassword);
public record ChangePasswordDto(string CurrentPassword, string NewPassword, string ConfirmPassword);
public record ForgotPasswordDto(string Email);

// ══════════════════════════════════════════════════════════════════════
// Internal Google OAuth Token Handling
// ══════════════════════════════════════════════════════════════════════
internal sealed record GoogleTokenPayload(
    string Sub,
    string Email,
    string? Name,
    string? GivenName,
    string? FamilyName,
    string? Picture,
    bool EmailVerified
);
