using System.Text.Json;
using System.Text.Json.Serialization;

namespace CurrencyTrackerApi.Infrastructure.Settings;

public static class JsonOptions
{
    public static readonly JsonSerializerOptions Default = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true,
        // Use camelCase naming convention for JSON output
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };
}

// 使用方式
// var json = JsonSerializer.Serialize(myObj, JsonOptions.Default);