using Application.DTOs;
using Application.Services.Identity;
using Core.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace InvoiceHub.API.Controllers.Identity;

[ApiController]
[Route("api/users")]
[Authorize]
[Tags("Users")]
public class UsersController : ControllerBase
{
    private readonly IUserService _users;

    public UsersController(IUserService users) => _users = users;

    private Guid TenantId => Guid.Parse(User.FindFirstValue("tenantId")!);
    private Guid UserId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    /// <summary>Get the currently authenticated user's profile.</summary>
    [HttpGet("me")]
    [Authorize(Policy = "AllRoles")]
    public async Task<ActionResult<ApiResponse<UserDto>>> GetMe(CancellationToken ct)
    {
        var result = await _users.GetMeAsync(UserId, ct);
        return result.Success ? Ok(result) : NotFound(result);
    }

    /// <summary>Upload or replace profile picture for the current user.</summary>
    [HttpPost("me/profile-picture")]
    [Authorize(Policy = "AllRoles")]
    public async Task<ActionResult<ApiResponse<string>>> UploadProfilePicture(
        IFormFile file, CancellationToken ct)
    {
        var result = await _users.UploadProfilePictureAsync(UserId, file, ct);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    /// <summary>Delete profile picture for the current user.</summary>
    [HttpDelete("me/profile-picture")]
    [Authorize(Policy = "AllRoles")]
    public async Task<ActionResult<ApiResponse<bool>>> DeleteProfilePicture(CancellationToken ct)
    {
        var result = await _users.DeleteProfilePictureAsync(UserId, ct);
        return Ok(result);
    }

    /// <summary>List all users in the tenant (paginated).</summary>
    [HttpGet]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<ApiResponse<PagedResult<UserDto>>>> GetList(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
    {
        var result = await _users.GetListAsync(TenantId, page, pageSize, ct);
        return Ok(result);
    }

    /// <summary>Get a specific user by ID.</summary>
    [HttpGet("{id:guid}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<ApiResponse<UserDto>>> Get(Guid id, CancellationToken ct)
    {
        var result = await _users.GetAsync(TenantId, id, ct);
        return result.Success ? Ok(result) : NotFound(result);
    }

    /// <summary>Invite a new team member (creates user + sends invite email).</summary>
    [HttpPost]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<ApiResponse<UserDto>>> Create(
        [FromBody] CreateUserDto dto, CancellationToken ct)
    {
        var result = await _users.CreateAsync(TenantId, dto, ct);
        return result.Success ? CreatedAtAction(nameof(Get), new { id = result.Data!.Id }, result) : BadRequest(result);
    }

    /// <summary>Update user details and role.</summary>
    [HttpPut("{id:guid}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<ApiResponse<UserDto>>> Update(
        Guid id, [FromBody] UpdateUserDto dto, CancellationToken ct)
    {
        var result = await _users.UpdateAsync(TenantId, id, dto, ct);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    /// <summary>Soft-delete a user (cannot delete the last admin).</summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<ApiResponse<bool>>> Delete(Guid id, CancellationToken ct)
    {
        var result = await _users.DeleteAsync(TenantId, id, ct);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    /// <summary>Update user status (Active/Inactive/Suspended).</summary>
    [HttpPatch("{id:guid}/status")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<ApiResponse<bool>>> UpdateStatus(
        Guid id, [FromBody] UserStatus status, CancellationToken ct)
    {
        var result = await _users.UpdateStatusAsync(TenantId, id, status, ct);
        return result.Success ? Ok(result) : BadRequest(result);
    }
}
