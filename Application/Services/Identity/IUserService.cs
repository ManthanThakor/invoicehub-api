using Application.DTOs;
using Core.Enums;
using Microsoft.AspNetCore.Http;

namespace Application.Services.Identity
{
    public interface IUserService
    {
        Task<ApiResponse<PagedResult<UserDto>>> GetListAsync(Guid tenantId, int page, int pageSize, CancellationToken ct = default);
        Task<ApiResponse<UserDto>> GetAsync(Guid tenantId, Guid userId, CancellationToken ct = default);
        Task<ApiResponse<UserDto>> GetMeAsync(Guid userId, CancellationToken ct = default);
        Task<ApiResponse<UserDto>> CreateAsync(Guid tenantId, CreateUserDto dto, CancellationToken ct = default);
        Task<ApiResponse<UserDto>> UpdateAsync(Guid tenantId, Guid userId, UpdateUserDto dto, CancellationToken ct = default);
        Task<ApiResponse<bool>> DeleteAsync(Guid tenantId, Guid userId, CancellationToken ct = default);
        Task<ApiResponse<string>> UploadProfilePictureAsync(Guid userId, IFormFile file, CancellationToken ct = default);
        Task<ApiResponse<bool>> DeleteProfilePictureAsync(Guid userId, CancellationToken ct = default);
        Task<ApiResponse<bool>> UpdateStatusAsync(Guid tenantId, Guid userId, UserStatus status, CancellationToken ct = default);
    }
}