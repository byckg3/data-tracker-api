using System.Text.Json.Serialization;
using CurrencyTrackerApi.Hubs;
using CurrencyTrackerApi.Infrastructure.Settings;
using CurrencyTrackerApi.Repositories;
using CurrencyTrackerApi.Services;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder( args );
FileSettings.BaseDirectory = builder.Environment.ContentRootPath;
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
                .AddScoped<ExchangeRateService>();
builder.Services.AddHttpClient<JsonRepository>();

builder.Services.AddSignalR();

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

app.MapControllers();

app.MapHub<ChatHub>( "/chatHub" );

app.Run();