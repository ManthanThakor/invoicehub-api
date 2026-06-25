using Application.DTOs;
using Application.Services.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Security.Claims;

namespace InvoiceHub.API.Controllers.Identity;

[ApiController]
[Route("api/auth")]
[EnableRateLimiting("AuthApi")]
[Tags("Auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _auth;

    public AuthController(IAuthService auth) => _auth = auth;

    private string IpAddress =>
        Request.Headers.TryGetValue("X-Forwarded-For", out var ip)
            ? ip.ToString()
            : HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

    /// <summary>Register a new user (with or without a company).</summary>
    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<AuthResponseDto>>> Register(
        [FromBody] RegisterDto dto, CancellationToken ct)
    {
        var result = await _auth.RegisterAsync(dto, IpAddress, ct);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    /// <summary>Login with email and password.</summary>
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<AuthResponseDto>>> Login(
        [FromBody] LoginDto dto, CancellationToken ct)
    {
        var result = await _auth.LoginAsync(dto, IpAddress, ct);
        return result.Success ? Ok(result) : Unauthorized(result);
    }

    /// <summary>Refresh an expired access token using a refresh token.</summary>
    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<AuthResponseDto>>> Refresh(
        [FromBody] string refreshToken, CancellationToken ct)
    {
        var result = await _auth.RefreshTokenAsync(refreshToken, IpAddress, ct);
        return result.Success ? Ok(result) : Unauthorized(result);
    }

    /// <summary>Logout — revoke the refresh token.</summary>
    [HttpPost("logout")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<bool>>> Logout(
        [FromBody] string refreshToken, CancellationToken ct)
    {
        var result = await _auth.RevokeTokenAsync(refreshToken, IpAddress, ct);
        return Ok(result);
    }

    /// <summary>Google OAuth login / register.</summary>
    [HttpPost("google")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<AuthResponseDto>>> GoogleLogin(
        [FromBody] string googleIdToken, CancellationToken ct)
    {
        var result = await _auth.GoogleLoginAsync(googleIdToken, IpAddress, ct);
        return result.Success ? Ok(result) : Unauthorized(result);
    }

    /// <summary>Verify email address using the token sent in the verification email.</summary>
    [HttpGet("verify-email")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<bool>>> VerifyEmail(
        [FromQuery] string token, CancellationToken ct)
    {
        var result = await _auth.VerifyEmailAsync(token, ct);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    /// <summary>Resend email verification link.</summary>
    [HttpPost("resend-verification")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<bool>>> ResendVerification(
        [FromBody] string email, CancellationToken ct)
    {
        var result = await _auth.ResendVerificationAsync(email, ct);
        return Ok(result);
    }

    /// <summary>Request a password reset email.</summary>
    [HttpPost("forgot-password")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<bool>>> ForgotPassword(
        [FromBody] ForgotPasswordDto dto, CancellationToken ct)
    {
        var result = await _auth.ForgotPasswordAsync(dto, ct);
        return Ok(result);
    }

    /// <summary>Reset password using the token from the reset email.</summary>
    [HttpPost("reset-password")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<bool>>> ResetPassword(
        [FromBody] ResetPasswordDto dto, CancellationToken ct)
    {
        var result = await _auth.ResetPasswordAsync(dto, ct);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    /// <summary>Change password for the currently authenticated user.</summary>
    [HttpPost("change-password")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<bool>>> ChangePassword(
        [FromBody] ChangePasswordDto dto, CancellationToken ct)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var result = await _auth.ChangePasswordAsync(userId, dto, ct);
        return result.Success ? Ok(result) : BadRequest(result);
    }
}
