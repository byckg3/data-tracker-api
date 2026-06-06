namespace DataTrackerApi.Infrastructure.Settings;
public static class FileSettings
{
    // private readonly IWebHostEnvironment _env;
    // public readonly string ContentRootDirectory;
    public static string BaseDirectory { get; set; } = AppContext.BaseDirectory;
    public static string ProjectDirectory { get; set; } = Directory.GetParent( AppContext.BaseDirectory )?
                                                                   .Parent?.Parent?.Parent?
                                                                   .FullName ?? AppContext.BaseDirectory;

    public static string SolutionDirectory => GetSolutionDirectory();

    public static string ClientBaseDirectory => Path.Combine( BaseDirectory, "logs", "clients" );
    public static string ClientFileNameFormat = "yyyyMMdd_HHmmss";
    public static string ClientFileNameSuffix = ".txt";

    private static string GetSolutionDirectory()
    {
        var currentDir = new DirectoryInfo( AppContext.BaseDirectory );
        while ( currentDir != null && !currentDir.EnumerateFiles( "*.sln*" ).Any() )
        {
            currentDir = currentDir.Parent;
        }

        if ( currentDir == null )
        {
            throw new Exception( "Solution directory not found." );
        }
        return currentDir.FullName;
    }
}