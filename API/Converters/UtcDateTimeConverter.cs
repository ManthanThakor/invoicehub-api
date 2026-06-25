using System.Text.Json;
using System.Text.Json.Serialization;

namespace InvoiceHub.API.Converters;

public class UtcDateTimeConverter : JsonConverter<DateTime>
{
    public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var value = reader.GetDateTime();
        return value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
        };
    }

    public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.Kind == DateTimeKind.Utc
            ? value.ToString("yyyy-MM-ddTHH:mm:ssZ")
            : value.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ"));
    }
}

public class NullableUtcDateTimeConverter : JsonConverter<DateTime?>
{
    public override DateTime? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null) return null;
        var value = reader.GetDateTime();
        return value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
        };
    }

    public override void Write(Utf8JsonWriter writer, DateTime? value, JsonSerializerOptions options)
    {
        if (!value.HasValue) { writer.WriteNullValue(); return; }
        var dt = value.Value;
        writer.WriteStringValue(dt.Kind == DateTimeKind.Utc
            ? dt.ToString("yyyy-MM-ddTHH:mm:ssZ")
            : dt.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ"));
    }
}
