## Commands
### dotnet
- dotnet --version
- dotnet build
- dotnet run [<FILE_NAME>.cs] [--launch-profile https] [--project /<PROJECT_DIR>/<PROJECT_NAME>.csproj]
- dotnet sln add <PROJECT_NAME>.csproj
- dotnet new webapi -controllers
- dotnet dev-certs https --trust
- dotnet test --filter "Tag=TestOnly"
- dotnet publish src\<PATH>\<PROJECT_NAME>.csproj -c Release -o ./publish

### dotnet user-secrets
- dotnet user-secrets init
- dotnet user-secrets set "ConnectionStrings:DefaultConnection" "<URL>" [--project /<PROJECT_DIR>/<PROJECT_NAME>.csproj]
- dotnet user-secrets list --project src/DataTrackerApi/DataTrackerApi.csproj

### dotnet ef
- dotnet ef migrations add AddUserTable --output-dir Infrastructure/Persistence/Migrations
- dotnet ef migrations remove
- dotnet ef database update --project src/DataTrackerApi/DataTrackerApi.csproj

### tool
- dotnet tool install --global dotnet-dump
- dotnet-dump ps
- dotnet-dump collect -p [PID]
- dotnet-dump analyze [DUMP_FILE_NAME]
- syncblk
- dumpasync

### docker
- docker build -t byckg3/data-tracker-api .
- docker run -it --rm -p 5253:8080 \
  -e ASPNETCORE_ENVIRONMENT=Development \
  byckg3/data-tracker-api
- docker compose up -d
- docker compose down

### wsl2
- ip addr show eth0 | grep inet
- nc -l 8080

### urls
- http://localhost:5253/scalar


### refs
- [C# documentation](https://learn.microsoft.com/en-us/dotnet/csharp/tour-of-csharp/)
- [.NET fundamentals documentation](https://learn.microsoft.com/zh-tw/dotnet/fundamentals/)
- [Entity Framework Core](https://learn.microsoft.com/zh-tw/ef/core/)
- [Mermaid Live Editor](https://mermaid.live/edit)
- [Grafana Alloy](https://grafana.com/docs/grafana-cloud/send-data/alloy/)
- [System.IO.Pipelines](https://learn.microsoft.com/zh-tw/dotnet/standard/io/pipelines#pipereader-common-problems)