using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;
using Scalar.AspNetCore;
using Serilog;
using Serilog.Filters;

using DataTrackerApi.Infrastructure.Channels;
using DataTrackerApi.Infrastructure.Settings;
using DataTrackerApi.Models;
using DataTrackerApi.Repositories;
using DataTrackerApi.Services;
using DataTrackerApi.Services.Workers;
using DataTrackerApi.Infrastructure.Persistence;
using DataTrackerApi.Features.Users.Services;

try
{
    var builder = WebApplication.CreateBuilder( args );
    FileSettings.BaseDirectory = builder.Environment.ContentRootPath;

    // builder.Configuration
    //        .SetBasePath( FileSettings.SolutionDirectory )
    //        .AddJsonFile( "appsettings.json", optional: true )
    //        .AddJsonFile( "appsettings.Development.json", optional: true )
    //        .AddEnvironmentVariables();

    var connectionString = builder.Configuration.GetConnectionString( "DefaultConnection" );
    if ( string.IsNullOrEmpty( connectionString ) )
    {
        throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
    }

    builder.Services.AddDbContextPool<AppDbContext>( options =>
    {
        options.UseNpgsql( connectionString );
    } );

    builder.Host.UseSerilog((context, services, configuration) =>
    {
        configuration.ReadFrom.Configuration(context.Configuration);

        // if (context.HostingEnvironment.IsProduction()) {}
        configuration.WriteTo.Logger( lc =>
            lc.Filter.ByIncludingOnly( Matching.WithProperty( "ConnId" ) )
              .WriteTo.Async(
                bufferSize: 20000,
                configure: asyncConfig =>
                {
                    asyncConfig.Map(
                        keyPropertyName: "ConnId",
                        defaultKey: "Global",
                        configure: (id, mapConfig) =>
                        {
                            mapConfig.File(
                                path: Path.Combine(FileSettings.ClientBaseDirectory, id,  FileSettings.ClientFileNameSuffix),
                                rollingInterval: RollingInterval.Hour,
                                buffered: true,
                                flushToDiskInterval: TimeSpan.FromSeconds(5),
                                retainedFileCountLimit: 24,             // 只保留最近 24 個檔案
                                fileSizeLimitBytes: 100 * 1024 * 1024,  // 單個檔案上限 100MB
                                outputTemplate: "{Message:lj}{NewLine}"
                            );
                        }
                    );
                }
            )
        );
    });

    builder.Services.AddCors( options =>
    {
        options.AddDefaultPolicy( policy =>
        {
            string[] allowedOrigins = [ "http://localhost:4200", "https://localhost", "https://frontend.com" ];
            policy.WithOrigins( allowedOrigins )
                .AllowCredentials()
                .AllowAnyMethod()
                .AllowAnyHeader();
        } );
    } );

    // Add services to the container.
    builder.Services.AddControllers()
                    .AddJsonOptions( options =>
                        {
                            options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
                        }
                    );
    // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
    builder.Services.AddOpenApi()
                    .AddScoped<ExchangeRateService>()
                    .AddScoped<PlaybackService>()
                    .AddScoped<UserService>()
                    .AddSingleton<WebSocketService>()
                    .AddSingleton<DataDispatcher<ClientMessage>>()
                    .AddSingleton<ClientFileManager>()
                    .AddHostedService<ClientMessageConsumer>()
                    .AddHostedService<ClientFileMonitor>()
                    .AddHostedService<ClientFileFlushScheduler>();
    builder.Services.AddHttpClient<JsonRepository>();

    // builder.Services.AddSignalR();

    var app = builder.Build();

    // Configure the HTTP request pipeline.
    if (app.Environment.IsDevelopment())
    {
        app.MapOpenApi();
        app.MapScalarApiReference();
    }
    app.UseCors();

    app.UseHttpsRedirection();

    app.UseAuthorization();

    var webSocketOptions = new WebSocketOptions
    {
        KeepAliveInterval = TimeSpan.FromMinutes( 2 ),
    };
    app.UseWebSockets( webSocketOptions );

    app.MapControllers();

    // app.MapHub<ChatHub>( "/chatHub" );

    await app.RunAsync();
}
catch ( Exception ex ) when ( ex.GetType().Name is not "HostAbortedException" )
{
    Log.Fatal( ex, "Application terminated unexpectedly" );
}
finally
{
    Log.CloseAndFlush();
}