using CurrencyTrackerApi.Models;
using CurrencyTrackerApi.Services;
using Microsoft.AspNetCore.Mvc;

namespace CurrencyTrackerApi.Controllers;

[ApiController]
[Route( "api/[controller]" )]
public class ExchangeRateController : ControllerBase
{
    private readonly ExchangeRateService _exchangeRateService;

    public ExchangeRateController( ExchangeRateService exchangeRateService )
    {
        _exchangeRateService = exchangeRateService;
    }

    [HttpGet( Name = "GetExchangeRates" )]
    public async Task<IActionResult> Get( [FromQuery( Name = "base" )] string baseCurrency,
                                          [FromQuery] string? quotes )
    {
        string[] quoteArray = quotes?.Split( ',' ) ?? [];
        var exchangeRates = await _exchangeRateService.FetchExchangeRatesAsync( baseCurrency, quoteArray );

        return Ok( exchangeRates );
    }
}
