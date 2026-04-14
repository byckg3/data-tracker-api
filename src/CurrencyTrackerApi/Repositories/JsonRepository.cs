using System.Text.Json;
using System.Text.Json.Nodes;

namespace CurrencyTrackerApi.Repositories;

public class JsonRepository
{
    private readonly HttpClient _httpClient;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly string _baseDir;

    public JsonRepository( HttpClient httpClient )
    {
        _httpClient = httpClient;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        };
        _baseDir = AppDomain.CurrentDomain.BaseDirectory;
    }

    public async Task<string> GetJsonAsync( string url )
    {
        try
        {
            var response = await _httpClient.GetAsync( url );
            // response.EnsureSuccessStatusCode();
            var responseBody = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode)
            {
                throw new HttpRequestException(
                    $"Request failed. StatusCode = {(int)response.StatusCode} {response.StatusCode},\nBody: {responseBody}"
                );

            }
            return responseBody;
        }
        catch (Exception ex)
        {
            Console.WriteLine( $"Error fetching exchange rates:\n{ex.Message}" );
            throw;
        }
    }

    public async Task<string> SaveJsonAsync( string jsonString, string filePath )
    {
        try
        {
            string fullPath = GetFullPath( filePath );
            string? folderPath = Path.GetDirectoryName( fullPath );
            if ( !string.IsNullOrEmpty( folderPath ) )
            {
                Directory.CreateDirectory( folderPath );
            }

            var jsonNode = JsonNode.Parse( jsonString ) ??
                throw new InvalidOperationException( "Failed to parse JSON string." );

            string prettyJson = jsonNode.ToJsonString( _jsonOptions );
            await File.WriteAllTextAsync( fullPath, prettyJson );

            return fullPath;
        }
        catch ( Exception ex )
        {
            Console.WriteLine( $"Error saving JSON to file {filePath}:\n{ex.Message}" );
            throw;
        }
    }

    public async Task<string> ReadJsonAsync( string filePath )
    {
        try
        {
            string fullPath = GetFullPath( filePath );
            if (!File.Exists( fullPath ))
            {
                throw new FileNotFoundException( $"File not found: {filePath}" );
            }

            string jsonString = await File.ReadAllTextAsync( fullPath );
            using ( JsonDocument.Parse( jsonString ) )
            {
                // Just to validate the JSON format
                return jsonString;
            }
        }
        catch ( Exception ex )
        {
            Console.WriteLine( $"Error reading JSON from file {filePath}:\n{ex.Message}" );
            throw;
        }
    }

    private string GetFullPath( string path )
    {
        string relativePath = path.TrimStart( '\\', '/' );
        string fullPath = Path.GetFullPath( _baseDir + relativePath );

        return fullPath;
    }
}