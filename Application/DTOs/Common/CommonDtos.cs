namespace Application.DTOs;

public record PagedResult<T>(
    IEnumerable<T> Items,
    int TotalCount,
    int Page,
    int PageSize
)
{
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
    public bool HasNextPage => Page < TotalPages;
    public bool HasPreviousPage => Page > 1;
}

public record AddressDto(
    string? Line1,
    string? Line2,
    string? City,
    string? State,
    string? StateCode,
    string? PinCode,
    string? Country = "India"
);

public record ApiResponse<T>(bool Success, string? Message, T? Data, IEnumerable<string>? Errors = null)
{
    public static ApiResponse<T> Ok(T data, string? message = null) => new(true, message, data);
    public static ApiResponse<T> Fail(string message, IEnumerable<string>? errors = null) => new(false, message, default, errors);
}

public record SelectOptionDto(Guid Value, string Label, string? SubLabel = null);
