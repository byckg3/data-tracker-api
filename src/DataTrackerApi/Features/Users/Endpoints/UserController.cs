using DataTrackerApi.Features.Users.Models;
using DataTrackerApi.Features.Users.Services;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
public class UserController : ControllerBase
{
    private readonly UserService _userService;
    private readonly ILogger<UserController> _logger;

    public UserController( UserService userService, ILogger<UserController> logger )
    {
        _userService = userService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<List<User>>> GetAll()
    {
        try
        {
            var users = await _userService.GetAllAsync();
            return Ok( users );
        }
        catch ( Exception ex )
        {
            _logger.LogError( ex, "Error in UserController.GetAll" );
            return StatusCode( 500, "Internal server error" );
        }
    }
}