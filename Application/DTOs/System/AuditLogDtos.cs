namespace Application.DTOs;

public record AuditLogDto(
    Guid Id,
    string EntityType,
    Guid EntityId,
    string Action,
    string? OldValues,
    string? NewValues,
    string? ChangedProperties,
    string? IpAddress,
    Guid UserId,
    string UserName,
    DateTime CreatedAt
);

public record AuditLogFilterDto(
    int Page = 1,
    int PageSize = 20,
    string? EntityType = null,
    Guid? EntityId = null,
    string? Action = null,
    Guid? UserId = null,
    DateTime? FromDate = null,
    DateTime? ToDate = null
);
