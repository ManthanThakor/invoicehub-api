using System.Collections.Generic;

namespace Application.DTOs;

public sealed record GroqResponse(IEnumerable<GroqChoice>? Choices);
public sealed record GroqChoice(GroqMessage? Message);
public sealed record GroqMessage(string? Content);
    