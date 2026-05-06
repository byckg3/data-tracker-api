### commands
- dotnet --version
- dotnet build
- dotnet run [<FILE_NAME>.cs] [--launch-profile https] [--project src\<PATH>\<PROJECT_NAME>.csproj]
- dotnet sln add <PROJECT_NAME>.csproj
- dotnet new webapi -controllers
- dotnet dev-certs https --trust
- dotnet test --filter "Tag=TestOnly"
- dotnet publish src\<PATH>\<PROJECT_NAME>.csproj -c Release -o ./publish

### urls
- http://localhost:5253/scalar


### refs
- [A tour of the C# language](https://learn.microsoft.com/zh-tw/dotnet/csharp/tour-of-csharp/)