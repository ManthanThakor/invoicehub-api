using Core.Common;
using Core.Enums;

namespace Core.Entities;

public class AuditLog : BaseEntity
{
    public string EntityType { get; set; } = null!;
    public Guid EntityId { get; set; }
    public string Action { get; set; } = null!;    // "Create" | "Update" | "Delete" | "Login"
    public string? OldValues { get; set; }         // JSON
    public string? NewValues { get; set; }         // JSON
    public string? ChangedProperties { get; set; } // JSON array
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }


    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
}
