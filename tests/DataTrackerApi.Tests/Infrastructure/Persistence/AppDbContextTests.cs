using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

using DataTrackerApi.Features.Users;
using DataTrackerApi.Infrastructure.Persistence;
using DataTrackerApi.Infrastructure.Settings;

namespace DataTrackerApi.Tests.Infrastructure.Persistence;

[Trait( "Tag", "TestOnly" )]
public class AppDbContextTests
{
    private readonly string _connectionString;
    public AppDbContextTests()
    {
        var config = new ConfigurationBuilder().SetBasePath( FileSettings.SolutionDirectory )
                                               .AddJsonFile( "appsettings.json", optional: true )
                                               .AddJsonFile( "appsettings.Development.json", optional: true )
                                               .AddEnvironmentVariables()
                                               .Build();
        _connectionString = config.GetConnectionString( "TestConnection" )?? string.Empty;
        // Console.WriteLine( $"Project Directory: {FileSettings.SolutionDirectory}" );
        // Console.WriteLine( $"Using Connection String: {_connectionString}" );
    }

    [Fact]
    public void TestDatabaseConnection()
    {
        Assert.False( string.IsNullOrEmpty( _connectionString ) );

        using var context = CreateDbContext();
        var canConnect = context.Database.CanConnect();

        Assert.True( canConnect, "Unable to connect to the database with the provided connection string." );
    }

    [Fact]
    public void OnModelCreating_ShouldApplyConfigurationsFromAssembly()
    {
        using var context = CreateDbContext();
        var model = context.Model;

        var userEntityType = model.FindEntityType( typeof( User ) );

        Assert.NotNull( userEntityType );
        Assert.Equal( "users", userEntityType.GetTableName() );
    }

    private AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
                            .UseNpgsql( _connectionString )
                            .Options;

        return new AppDbContext( options );
    }
}