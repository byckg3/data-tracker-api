# Data Tracker API — Copilot Instructions

## Project Overview

ASP.NET Core Web API (.NET 10) using a Controller → Service → Repository three-layer architecture for real-time data tracking via WebSocket, log persistence, and historical playback.

- **Core features**: WebSocket communication, log streaming (REST & SSE), message echo and persistence
- **Optional features**: Exchange rate queries via Frankfurter API
- **Test framework**: xUnit
- **JSON serialization**: System.Text.Json

## Code Style

- Spaces inside parentheses: `Method( param1, param2 )`
- Space before block braces: `if ( condition )`
- Use `var` for local variable declarations
- Prefer primary constructors: `public class Foo( Dep dep ) { ... }`
- Use `async/await` throughout; suffix async methods with `Async`
- Use `[]` for collection initialization instead of `new List<T>()`

## Architecture

### Controller
- Inherit `ControllerBase`, annotate with `[ApiController]` and `[Route("api/[controller]")]`
- Responsible only for receiving requests, calling Services, and returning `IActionResult`
- No business logic

### Service
- Encapsulates business logic; depends on Repository
- Must not directly perform HTTP calls or file I/O

### Repository
- Responsible only for data access (HTTP calls, file read/write)
- Resolve relative paths via `FileSettings.BaseDirectory`

## JSON Configuration

Always use `JsonOptions.Default` (located in `Infrastructure/Settings/JsonOptions.cs`):

```csharp
JsonSerializer.Deserialize<T>( json, JsonOptions.Default )
JsonSerializer.Serialize( obj, JsonOptions.Default )
```

## Error Handling

- Wrap Repository and Service methods in `try/catch`; log then re-throw
- Use `Console.WriteLine` for error logging (structured logging not yet introduced)
- Never swallow exceptions; ensure messages are human-readable

## Security Rules (Code Review Focus)

- **CORS**: Explicitly enumerate `AllowedOrigins`; never use `AllowAnyOrigin()`
- **Input validation**: Validate Controller query parameters for non-empty and correct format before passing to Service
- **Path safety**: All file paths must be resolved through `GetFullPath()` to prevent path traversal
- **HTTP status codes**: Throw an exception when `response.IsSuccessStatusCode` is false; never silently ignore failures
- **Sensitive config**: API keys and connection strings must go in `appsettings.json` or environment variables — never hardcoded

## Unit Testing

- Test classes go in `tests/DataTrackerApi.Tests/`, mirroring the `src` directory structure
- Test method naming: `MethodName_Scenario_ExpectedResult` (e.g. `StreamLogAsync_ShouldYieldMovementRecords`)
- Mark integration tests (requiring external connections) with `[Trait("Tag", "TestOnly")]` to filter with `--filter "Tag=TestOnly"`
- Use `[Fact]` for standard tests; use `[Theory]` + `[InlineData]` for parameterized tests

## Code Review Checklist

When reviewing a PR, verify:

1. **Layer boundaries**: Does any logic cross layers (e.g. Controller calling Repository directly)?
2. **Async consistency**: Is there any synchronous blocking (`.Result`, `.Wait()`) mixed with `async/await`?
3. **CORS safety**: Are new origins explicitly listed? No wildcards?
4. **Exception handling**: Are there empty `catch` blocks or swallowed exceptions?
5. **Path operations**: Is user input concatenated directly into file paths?
6. **JSON consistency**: Is `JsonOptions.Default` used everywhere?
7. **Test coverage**: Do new features include corresponding `[Fact]` tests?
8. **`Console.WriteLine`**: Is any sensitive data (tokens, passwords) being printed?
