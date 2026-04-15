namespace CurrencyTrackerApi.Infrastructure.Settings;
public static class FileSettings
{
    // private readonly IWebHostEnvironment _env;
    // public readonly string ContentRootDirectory;
    public static string BaseDirectory { get; set; } = AppContext.BaseDirectory;
}