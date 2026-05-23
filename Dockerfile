# https://hub.docker.com/_/microsoft-dotnet
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /app

# copy csproj and restore as distinct layers
# COPY *.sln .
COPY src/DataTrackerApi/DataTrackerApi.csproj ./DataTrackerApi/
RUN dotnet restore "DataTrackerApi/DataTrackerApi.csproj"

# copy everything else and build app
COPY src/DataTrackerApi/. ./DataTrackerApi/
RUN dotnet build ./DataTrackerApi/DataTrackerApi.csproj -c release

# FROM build AS test
# RUN dotnet test --no-build -c Release

FROM build AS publish
RUN dotnet publish ./DataTrackerApi/DataTrackerApi.csproj -c release -o ./publish --no-restore -p:UseAppHost=false

# final stage/image
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app

COPY --from=publish /app/publish .

USER app
EXPOSE 8080
ENTRYPOINT [ "dotnet", "DataTrackerApi.dll" ]