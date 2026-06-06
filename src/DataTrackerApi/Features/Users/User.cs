namespace DataTrackerApi.Features.Users;

public class User
{
    public int Id { get; private set; }

    public Guid PublicId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    public User( string email, string name = "" )
    {
        Email = email;
        Name = name;
        PublicId = Guid.CreateVersion7();
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = CreatedAt;
    }

    private User() { }
}