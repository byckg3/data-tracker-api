using System.Text.Json;
using CurrencyTrackerApi.Models;
using CurrencyTrackerApi.Repositories;

namespace CurrencyTrackerApi.Services;

public class ExchangeRateService
{
    private readonly JsonRepository _repository;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly string _baseUrl = "https://api.frankfurter.dev/v2/rates";
    private readonly string _filePath = @"data\testdata.txt";

    public ExchangeRateService( JsonRepository repository )
    {
        _repository = repository;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        };
    }

    public async Task<List<ExchangeRateDto>> FetchExchangeRatesAsync( string baseCurrency, IEnumerable<string> quoteCurrencies )
    {
        List<ExchangeRateDto> exchangeRateDtos = [];
        // string fileName = "testdata.txt";
        try
        {
            var url = $"{_baseUrl}?base={baseCurrency}&quotes={string.Join( ",", quoteCurrencies )}";
            var jsonString = await _repository.GetJsonAsync( url );

            var filePath = await _repository.SaveJsonAsync( jsonString, _filePath );
            var json = await _repository.ReadJsonAsync( _filePath );

            exchangeRateDtos = JsonSerializer.Deserialize<List<ExchangeRateDto>>( json, _jsonOptions ) ?? [];
            foreach (var item in exchangeRateDtos)
            {
                Console.WriteLine( $"{item.Date} {item.BaseCurrency} -> {item.QuoteCurrency}: {item.Rate}" );
            }
        }
        catch ( Exception ex )
        {
            Console.WriteLine( $"Error fetching exchange rates:\n{ex.Message}" );
        }
        return exchangeRateDtos;
    }
}