using DataTrackerApi.Repositories;

namespace DataTrackerApi.Tests.Repositories;

public class JsonRepositoryTests
{
    private readonly HttpClient _httpClient;
    private readonly JsonRepository _repository;
    private readonly string _filePath = @"data\testdata.txt";

    public JsonRepositoryTests()
    {
        _httpClient = new HttpClient();
        _repository = new JsonRepository( _httpClient );
    }

    [Fact]
    // [Trait("Tag", "TestOnly")]
    public async Task GetJsonAsync_ShouldReturnJsonString()
    {
        var url = "https://api.frankfurter.dev/v2/rates?base=TWD&quotes=JPY,USD";
        var jsonString = await _repository.GetJsonAsync(url);

        Assert.False(string.IsNullOrEmpty(jsonString));
    }

    [Fact]
    // [Trait("Tag", "TestOnly")]
    public async Task SaveJsonAsync_ShouldSaveJsonToFile()
    {
        string jsonString = "[{\"date\":\"2026-04-13\",\"base\":\"TWD\",\"quote\":\"JPY\",\"rate\":5.0267}]";

        string savedFilePath = await _repository.SaveJsonAsync( jsonString, _filePath );
        Console.WriteLine( $"File saved at: {savedFilePath}" );

        Assert.True( File.Exists( savedFilePath ) );
    }

    [Fact]
    // [Trait("Tag", "TestOnly")]
    public async Task ReadJsonAsync_ShouldReadJsonFromFile()
    {
        string jsonString = await _repository.ReadJsonAsync( _filePath );
        Console.WriteLine($"File content:\n{jsonString}");

        Assert.False(string.IsNullOrEmpty(jsonString));
    }

}
