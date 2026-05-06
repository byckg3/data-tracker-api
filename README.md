# Data Tracker API

ASP.NET Core Web API (.NET 10) for real-time data tracking and playback.

Core capabilities:

- Accept client messages through WebSocket.
- Echo messages back to the same client connection.
- Persist incoming messages to connection-scoped log files.
- Replay historical movement logs via REST streaming or Server-Sent Events (SSE).

## Tech Stack

- .NET 10 (`net10.0`)
- ASP.NET Core Web API
- Scalar OpenAPI (`Scalar.AspNetCore`)
- Serilog (Console + File + Map + Async sinks)
- xUnit

## Project Structure

```text
src/DataTrackerApi/
  Controllers/      # API entry points (Playback / WebSocket)
  Services/         # Business logic for playback and websocket workflows
  Infrastructure/   # Shared settings and channels
  Models/           # DTOs and message models

tests/DataTrackerApi.Tests/
  Services/
```

## Quick Start

### 1) Prerequisites

- .NET SDK 10

Check installed version:

```bash
dotnet --version
```

### 2) Build

```bash
dotnet build
```

### 3) Run API

From the project root:

```bash
dotnet run --project src/DataTrackerApi/DataTrackerApi.csproj
```

Default URLs:

- `http://localhost:5253`
- `http://localhost:5000`

Scalar API docs are available in Development mode:

- `http://localhost:5253/scalar`

## API Overview

### 1) Playback (JSON stream)

`GET /api/Playback/{connectionId}/{date}`

Reads a specific log file and returns movement records as an async stream.

### 2) Playback (SSE)

`GET /api/Playback/sse/{connectionId}/{date}`

Streams movement records in `text/event-stream` format.

`date` maps to the log filename suffix: `status-{date}.log`.

### 3) WebSocket Endpoint

`GET /` (upgrade to WebSocket)

After connection:

- Server receives client messages.
- Server echoes the same message back.
- Message payloads are forwarded to the background write pipeline.

## Logs

- Base log folder: `src/DataTrackerApi/logs/`
- Each connection ID gets its own subfolder.
- File pattern: `status-*.log` (hourly rolling)

## Testing

Run all tests:

```bash
dotnet test
```

Run tagged tests only (when `[Trait("Tag", "TestOnly")]` is used):

```bash
dotnet test --filter "Tag=TestOnly"
```

## Configuration

Use `appsettings.Example.json` as a template for local settings.
