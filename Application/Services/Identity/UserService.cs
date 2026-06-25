using Application.DTOs;
using Application.Services.System;
using Application.Services.Utilities;
using BCrypt.Net;
using Core.Entities;
using Core.Enums;
using Core.Interfaces.Repositories;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;

namespace Application.Services.Identity
{

    public class UserService : IUserService
    {
        private readonly IUserRepository _users;
        private readonly IFileService _files;
        private readonly IEmailService _email;
        private readonly IUnitOfWork _uow;
        private readonly ILogger<UserService> _log;
        private readonly IConfiguration _config;

        public UserService(
            IUserRepository users,
            IFileService files,
            IEmailService email,
            IUnitOfWork uow,
            ILogger<UserService> log,
            IConfiguration config)
        {
            _users = users; _files = files; _email = email;
            _uow = uow; _log = log; _config = config;
        }

        public async Task<ApiResponse<PagedResult<UserDto>>> GetListAsync(
            Guid tenantId, int page, int pageSize, CancellationToken ct = default)
        {
            var query = _users.Query(tenantId).OrderBy(u => u.FirstName);
            var total = await query.CountAsync(ct);
            var items = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(u => AuthService.MapUser(u))
                .ToListAsync(ct);

            return ApiResponse<PagedResult<UserDto>>.Ok(
                new PagedResult<UserDto>(items, total, page, pageSize));
        }

        public async Task<ApiResponse<UserDto>> GetAsync(
            Guid tenantId, Guid userId, CancellationToken ct = default)
        {
            var user = await _users.GetByIdWithTenantAsync(userId, tenantId, ct);
            if (user == null) return ApiResponse<UserDto>.Fail("User not found.");
            return ApiResponse<UserDto>.Ok(AuthService.MapUser(user));
        }

        public async Task<ApiResponse<UserDto>> GetMeAsync(Guid userId, CancellationToken ct = default)
        {
            var user = await _users.GetByIdAsync(userId, ct);
            if (user == null) return ApiResponse<UserDto>.Fail("User not found.");
            return ApiResponse<UserDto>.Ok(AuthService.MapUser(user));
        }

        public async Task<ApiResponse<UserDto>> CreateAsync(
            Guid tenantId, CreateUserDto dto, CancellationToken ct = default)
        {
            if (!await _users.IsEmailUniqueAsync(dto.Email, ct: ct))
                return ApiResponse<UserDto>.Fail("A user with this email already exists.");

            var user = new User
            {
                TenantId = tenantId,
                FirstName = dto.FirstName,
                LastName = dto.LastName,
                Email = dto.Email.ToLowerInvariant().Trim(),
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password, workFactor: 12),
                PhoneNumber = dto.PhoneNumber,
                Role = dto.Role,
                Status = UserStatus.Active,
                EmailVerified = true // Admin-created users skip verification
            };

            await _users.AddAsync(user, ct);
            await _uow.SaveChangesAsync(ct);

            // Send team invite email with password
            _ = _email.SendTeamInviteAsync(user.Email, user.FirstName, dto.Password);

            _log.LogInformation("User created: {Email} with role {Role} for tenant {TenantId}",
                user.Email, user.Role, tenantId);

            return ApiResponse<UserDto>.Ok(AuthService.MapUser(user), "User created and invitation sent.");
        }

        public async Task<ApiResponse<UserDto>> UpdateAsync(
            Guid tenantId, Guid userId, UpdateUserDto dto, CancellationToken ct = default)
        {
            var user = await _users.GetByIdWithTenantAsync(userId, tenantId, ct);
            if (user == null) return ApiResponse<UserDto>.Fail("User not found.");

            user.FirstName = dto.FirstName;
            user.LastName = dto.LastName;
            user.PhoneNumber = dto.PhoneNumber;
            user.Role = dto.Role;
            user.Status = dto.Status;

            _users.Update(user);
            await _uow.SaveChangesAsync(ct);

            _log.LogInformation("User updated: {UserId}", userId);
            return ApiResponse<UserDto>.Ok(AuthService.MapUser(user), "User updated.");
        }

        public async Task<ApiResponse<bool>> DeleteAsync(
            Guid tenantId, Guid userId, CancellationToken ct = default)
        {
            var user = await _users.GetByIdWithTenantAsync(userId, tenantId, ct);
            if (user == null) return ApiResponse<bool>.Fail("User not found.");

            // Check if this is the last admin
            if (user.Role == UserRole.Admin)
            {
                var adminCount = await _users.CountAsync(
                    u => u.TenantId == tenantId && u.Role == UserRole.Admin && u.Id != userId, ct);
                if (adminCount == 0)
                    return ApiResponse<bool>.Fail("Cannot delete the last admin user.");
            }

            _users.SoftDelete(user);
            await _uow.SaveChangesAsync(ct);

            _log.LogInformation("User soft-deleted: {UserId}", userId);
            return ApiResponse<bool>.Ok(true, "User deleted.");
        }

        public async Task<ApiResponse<string>> UploadProfilePictureAsync(
            Guid userId, IFormFile file, CancellationToken ct = default)
        {
            var user = await _users.GetByIdAsync(userId, ct);
            if (user == null) return ApiResponse<string>.Fail("User not found.");

            if (!_files.IsValidImage(file))
                return ApiResponse<string>.Fail("Invalid image. Please upload JPG, PNG, or WebP (max 5MB).");

            // Delete old profile picture
            if (!string.IsNullOrEmpty(user.ProfilePicture))
                await _files.DeleteAsync(user.ProfilePicture);

            // Compress & save (max 400px width for profile pictures)
            var compressed = _files.CompressImage(file, maxWidthPx: 400);
            var path = await _files.SaveAsync(compressed, $"profiles/{userId}", ct);

            user.ProfilePicture = path;
            _users.Update(user);
            await _uow.SaveChangesAsync(ct);

            _log.LogInformation("Profile picture updated for user {UserId}", userId);
            return ApiResponse<string>.Ok(_files.GetPublicUrl(path), "Profile picture updated.");
        }

        public async Task<ApiResponse<bool>> DeleteProfilePictureAsync(
            Guid userId, CancellationToken ct = default)
        {
            var user = await _users.GetByIdAsync(userId, ct);
            if (user == null) return ApiResponse<bool>.Fail("User not found.");

            if (!string.IsNullOrEmpty(user.ProfilePicture))
            {
                await _files.DeleteAsync(user.ProfilePicture);
                user.ProfilePicture = null;
                _users.Update(user);
                await _uow.SaveChangesAsync(ct);
            }

            return ApiResponse<bool>.Ok(true, "Profile picture removed.");
        }

        public async Task<ApiResponse<bool>> UpdateStatusAsync(
            Guid tenantId, Guid userId, UserStatus status, CancellationToken ct = default)
        {
            var user = await _users.GetByIdWithTenantAsync(userId, tenantId, ct);
            if (user == null) return ApiResponse<bool>.Fail("User not found.");

            user.Status = status;
            _users.Update(user);
            await _uow.SaveChangesAsync(ct);

            _log.LogInformation("User {UserId} status changed to {Status}", userId, status);
            return ApiResponse<bool>.Ok(true, $"User status updated to {status}.");
        }

    }

}
