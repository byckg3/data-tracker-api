using System.Text.Json.Serialization;

// [
//     {"date":"2026-04-10","base":"TWD","quote":"JPY","rate":5.0207},
//     {"date":"2026-04-10","base":"TWD","quote":"USD","rate":0.03154}
// ]
namespace CurrencyTrackerApi.Models;

public record ExchangeRateDto
{
    public required DateOnly Date { get; init; }

    [JsonPropertyName( "base" )]
    public required string BaseCurrency { get; init; }

    [JsonPropertyName( "quote" )]
    public required string QuoteCurrency { get; init; }

    public required decimal Rate { get; init; }
}