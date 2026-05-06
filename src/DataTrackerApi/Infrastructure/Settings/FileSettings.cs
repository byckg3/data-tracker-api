namespace DataTrackerApi.Infrastructure.Settings;
public static class FileSettings
{
    // private readonly IWebHostEnvironment _env;
    // public readonly string ContentRootDirectory;
    public static string BaseDirectory { get; set; } = AppContext.BaseDirectory;
    public static string ProjectDirectory { get; set; } = Directory.GetParent( AppContext.BaseDirectory )?
                                                                   .Parent?.Parent?.Parent?
                                                                   .FullName ?? AppContext.BaseDirectory;
}