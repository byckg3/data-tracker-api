using DataTrackerApi.Features.Users.Models;

public record CreateUserCommand( string Username, string Email )
{
    public User ToEntity()
    {
        return new User( Email, Username );
    }
}