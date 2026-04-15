using System.Text.Json.Serialization;
using CurrencyTrackerApi.Infrastructure.Settings;
using CurrencyTrackerApi.Repositories;
using CurrencyTrackerApi.Services;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder( args );
FileSettings.BaseDirectory = builder.Environment.ContentRootPath;

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

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();