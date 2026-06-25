using Core.Enums;

namespace InvoiceHub.Application.DTOs.System;

/// <summary>
/// DTO for notification log entries
/// </summary>
public record NotificationLogDto(
    Guid Id,
    NotificationType Type,
    NotificationStatus Status,
    string Subject,
    string? Recipient,
    int RetryCount,
    DateTime? SentAt,
    string? ErrorMessage,
    DateTime CreatedAt
);
