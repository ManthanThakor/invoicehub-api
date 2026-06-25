using Application.DTOs;

namespace Application.Services.Identity
{
    public interface IAuthService
    {
        Task<ApiResponse<AuthResponseDto>> RegisterAsync(RegisterDto dto, string ipAddress, CancellationToken ct = default);
        Task<ApiResponse<AuthResponseDto>> LoginAsync(LoginDto dto, string ipAddress, CancellationToken ct = default);
        Task<ApiResponse<AuthResponseDto>> RefreshTokenAsync(string refreshToken, string ipAddress, CancellationToken ct = default);
        Task<ApiResponse<bool>> RevokeTokenAsync(string refreshToken, string ipAddress, CancellationToken ct = default);
        Task<ApiResponse<AuthResponseDto>> GoogleLoginAsync(string googleIdToken, string ipAddress, CancellationToken ct = default);
        Task<ApiResponse<bool>> VerifyEmailAsync(string token, CancellationToken ct = default);
        Task<ApiResponse<bool>> ResendVerificationAsync(string email, CancellationToken ct = default);
        Task<ApiResponse<bool>> ForgotPasswordAsync(ForgotPasswordDto dto, CancellationToken ct = default);
        Task<ApiResponse<bool>> ResetPasswordAsync(ResetPasswordDto dto, CancellationToken ct = default);
        Task<ApiResponse<bool>> ChangePasswordAsync(Guid userId, ChangePasswordDto dto, CancellationToken ct = default);
    }

}