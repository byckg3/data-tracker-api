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

try
{
    var builder = WebApplication.CreateBuilder( args );
    FileSettings.BaseDirectory = builder.Environment.ContentRootPath;

    var logBaseDir = Path.Combine( FileSettings.BaseDirectory, "logs" );
    Log.Logger = new LoggerConfiguration()
        .MinimumLevel.Information()
        .Enrich.FromLogContext()
        .WriteTo.Logger( lc =>
            lc.Filter.ByExcluding( Matching.WithProperty( "ConnId" ) )
              .WriteTo.Console( outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}" )
        )
        .WriteTo.Logger( lc =>
            lc.Filter.ByIncludingOnly( Matching.WithProperty( "ConnId" ) )
              .WriteTo.Async( asyncWrite =>
                    asyncWrite.Map(
                        keyPropertyName: "ConnId",
                        defaultKey: "Global",
                        configure: ( connId, mapWrite ) => mapWrite.File(
                            path: Path.Combine( logBaseDir, connId, "status-.log" ),
                            rollingInterval: RollingInterval.Hour,
                            buffered: true,
                            flushToDiskInterval: TimeSpan.FromSeconds( 5 ),
                            retainedFileCountLimit: 24,             // 只保留最近 24 個檔案（一天）
                            fileSizeLimitBytes: 100 * 1024 * 1024,  // 單個檔案上限 100MB
                            outputTemplate: "{Message:lj}{NewLine}"
                        )
                    ),
                    bufferSize: 10000
            )
        )
        .CreateLogger();

    builder.Host.UseSerilog();

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
                    .AddSingleton<WebSocketService>()
                    .AddSingleton<DataChannel<ClientMessage>>()
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

    app.Run();
}
catch ( Exception ex )
{
    Log.Fatal( ex, "Application terminated unexpectedly" );
}
finally
{
    Log.CloseAndFlush();
}