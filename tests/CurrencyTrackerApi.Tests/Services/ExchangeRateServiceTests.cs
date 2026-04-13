using CurrencyTrackerApi.Services;
using CurrencyTrackerApi.Repositories;

namespace CurrencyTrackerApi.Tests.Services;

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
        List<string> quotes = [ "JPY", "USD" ];
        var dtos = await _service.FetchExchangeRatesAsync("TWD", quotes);

        Assert.Equal(2, quotes.Count);
        Assert.Equal("TWD", dtos[0].BaseCurrency);
        Assert.Contains(dtos[0].QuoteCurrency, quotes);
        Assert.True(dtos[0].Rate > 0);
    }
}