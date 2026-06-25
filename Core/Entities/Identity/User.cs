using Core.Common;
using Core.Enums;

namespace Core.Entities;

public class User : BaseEntity
{
    public string FirstName { get; set; } = null!;
    public string LastName { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string? PhoneNumber { get; set; }
    public string? ProfilePicture { get; set; }
    public string PasswordHash { get; set; } = null!;
    public UserRole Role { get; set; } = UserRole.Viewer;
    public UserStatus Status { get; set; } = UserStatus.PendingVerification;
    public bool EmailVerified { get; set; } = false;
    public string? EmailVerificationToken { get; set; }
    public DateTime? EmailVerificationExpiry { get; set; }
    public string? PasswordResetToken { get; set; }
    public DateTime? PasswordResetExpiry { get; set; }
    public string? GoogleId { get; set; }  // Google OAuth
    public DateTime? LastLoginAt { get; set; }
    public string? LastLoginIp { get; set; }

    // Relations

    public Tenant Tenant { get; set; } = null!;
    public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
    public ICollection<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();
}
