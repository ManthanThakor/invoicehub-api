using Core.Enums;

namespace Core.Entities;

public class RefreshToken
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public bool IsDeleted { get; set; } = false;
    public DateTime? DeletedAt { get; set; }
    public Guid? CreatedBy { get; set; }
    public Guid? UpdatedBy { get; set; }
    public Guid? DeletedBy { get; set; }

    public string Token { get; set; } = null!;
    public DateTime ExpiresAt { get; set; }
    public bool IsRevoked { get; set; } = false;
    public string? RevokedReason { get; set; }
    public string? CreatedByIp { get; set; }
    public string? RevokedByIp { get; set; }

    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
}