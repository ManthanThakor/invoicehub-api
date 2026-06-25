using Core.Common;
using Core.Enums;

namespace Core.Entities;

public class Notification : BaseEntity
{
    public NotificationType Type { get; set; }
    public NotificationStatus Status { get; set; } = NotificationStatus.Queued;
    public string Subject { get; set; } = null!;
    public string Body { get; set; } = null!;
    public string? Recipient { get; set; }   // email / phone
    public string? ReferenceType { get; set; }
    public Guid? ReferenceId { get; set; }
    public string? ErrorMessage { get; set; }
    public int RetryCount { get; set; } = 0;
    public DateTime? SentAt { get; set; }


    public Guid? UserId { get; set; }
    public User? User { get; set; }
}
