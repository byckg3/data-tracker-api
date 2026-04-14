using System.Text.Json.Serialization;

namespace CurrencyTrackerApi.Models;

public class CurrencyExchange
{
    public DateOnly? Date { get; set; }
    public string? Base { get; set; }

    public Dictionary<string, decimal> Rates { get; set; } = [];
}