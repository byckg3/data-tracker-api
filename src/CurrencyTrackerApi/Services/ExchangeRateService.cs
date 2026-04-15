using System.Text.Json;
using CurrencyTrackerApi.Infrastructure.Settings;
using CurrencyTrackerApi.Models;
using CurrencyTrackerApi.Repositories;

namespace CurrencyTrackerApi.Services;

public class ExchangeRateService
{
    private readonly JsonRepository _repository;
    private readonly string _baseUrl = "https://api.frankfurter.dev/v2/rates";
    private readonly string _filePath = Path.Combine( "data", "testdata.txt" );

    public ExchangeRateService( JsonRepository repository )
    {
        _repository = repository;
    }

    public async Task<CurrencyExchange> FetchExchangeRatesAsync( string baseCurrency, IEnumerable<string>? quoteCurrencies )
    {
        var currencyExchange = new CurrencyExchange();
        try
        {
            var url = $"{_baseUrl}?base={baseCurrency}";
            if ( quoteCurrencies != null && quoteCurrencies.Any() )
            {
                url += $"&quotes={string.Join( ",", quoteCurrencies )}";
            }

            var jsonString = await _repository.GetJsonAsync( url );
            Console.WriteLine( $"Fetched exchange rates JSON:\n{jsonString}" );

            var filePath = await _repository.SaveJsonAsync( jsonString, _filePath );
            var json = await _repository.ReadJsonAsync( _filePath );

            var exchangeRateDtos = JsonSerializer.Deserialize<List<ExchangeRateDto>>( json, JsonOptions.Default ) ?? [];
            if ( exchangeRateDtos.Count > 0 )
            {
                currencyExchange.Date = exchangeRateDtos[ 0 ].Date;
                currencyExchange.Base = exchangeRateDtos[ 0 ].BaseCurrency;
            }

            foreach ( var item in exchangeRateDtos )
            {
                currencyExchange.Rates[ item.QuoteCurrency ] = item.Rate;
                Console.WriteLine( $"{item.Date} {item.BaseCurrency} -> {item.QuoteCurrency}: {item.Rate}" );
            }
            return currencyExchange;
        }
        catch ( Exception ex )
        {
            Console.WriteLine( $"Error fetching exchange rates:\n{ex.Message}" );
            throw;
        }
    }
}