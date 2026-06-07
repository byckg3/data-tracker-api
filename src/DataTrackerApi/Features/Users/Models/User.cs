using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DataTrackerApi.Features.Users.Models;

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

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure( EntityTypeBuilder<User> builder )
    {
        builder.ToTable( "users" );

        builder.HasKey( u => u.Id );

        builder.Property( u => u.Id )
               .ValueGeneratedOnAdd();

        builder.Property( x => x.Email )
               .IsRequired()
               .HasMaxLength( 200 );

        builder.Property( x => x.Name )
               .HasMaxLength( 100 );

        builder.HasIndex( x => x.Email )
               .IsUnique();

        builder.HasIndex( x => x.PublicId )
               .IsUnique();
    }
}