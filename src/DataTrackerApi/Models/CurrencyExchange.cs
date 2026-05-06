using System.Text.Json.Serialization;

namespace DataTrackerApi.Models;

public class CurrencyExchange
{
    public DateOnly? Date { get; set; }
    public string? Base { get; set; }

    public Dictionary<string, decimal> Rates { get; set; } = [];
}