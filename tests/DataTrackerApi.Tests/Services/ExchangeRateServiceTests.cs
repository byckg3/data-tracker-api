using DataTrackerApi.Services;
using DataTrackerApi.Repositories;

namespace DataTrackerApi.Tests.Services;

public class ExchangeRateServiceTests
{
    private readonly HttpClient _httpClient;
    private readonly JsonRepository _repository;
    private readonly ExchangeRateService _service;

    public ExchangeRateServiceTests()
    {
        _httpClient = new HttpClient();
        _repository = new JsonRepository(_httpClient);
        _service = new ExchangeRateService(_repository);
    }

    [Fact]
    // [Trait("Tag", "TestOnly")]
    public async Task FetchExchangeRatesAsync_ShouldReturnExchangeRateDtos()
    {
        var baseCurrency = "TWD";
        var quotes = new[] { "JPY", "USD" };

        var currencyExchange = await _service.FetchExchangeRatesAsync( baseCurrency, quotes );

        Assert.Equal( quotes.Length, currencyExchange.Rates.Count );
        Assert.Equal( baseCurrency, currencyExchange.Base );
        Assert.Contains( quotes[ 0 ], currencyExchange.Rates.Keys);
        Assert.Contains( quotes[ 1 ], currencyExchange.Rates.Keys);
        Assert.True( currencyExchange.Rates[ quotes[ 0 ] ] > 0 );
        Assert.True( currencyExchange.Rates[ quotes[ 1 ] ] > 0 );
    }
}