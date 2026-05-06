using System.Text.Json;
using System.Text.Json.Serialization;

namespace DataTrackerApi.Infrastructure.Settings;

public static class JsonOptions
{
    public static readonly JsonSerializerOptions Default = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true,
        // Use camelCase naming convention for JSON string output
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };
}