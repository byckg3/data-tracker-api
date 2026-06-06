using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using DataTrackerApi.Features.Users;

namespace DataTrackerApi.Infrastructure.Persistence.Configurations;


public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure( EntityTypeBuilder<User> builder )
    {
        builder.ToTable( "users" );

        builder.HasKey( u => u.Id );

        builder.Property(u => u.Id)
               .ValueGeneratedOnAdd();

        builder.Property(x => x.Email)
               .IsRequired()
               .HasMaxLength( 200 );

        builder.Property(x => x.Name)
               .HasMaxLength( 100 );

        builder.HasIndex( x => x.Email )
               .IsUnique();

        builder.HasIndex( x => x.PublicId )
               .IsUnique();
    }
}