using DataTrackerApi.Models;
using DataTrackerApi.Services;
using Microsoft.AspNetCore.Mvc;

namespace DataTrackerApi.Controllers;

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
    public async Task<IActionResult> Get( [FromQuery( Name = "base" )] string baseData,
                                          [FromQuery] string? quotes )
    {
        string[] quoteArray = quotes?.Split( ',' ) ?? [];
        var DataQuotation = await _exchangeRateService.FetchExchangeRatesAsync( baseData, quoteArray );

        return Ok( DataQuotation );
    }
}
