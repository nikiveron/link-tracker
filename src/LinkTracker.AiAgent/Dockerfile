FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Копируем csproj зависимостей
COPY src/LinkTracker.Contracts/LinkTracker.Contracts.csproj LinkTracker.Contracts/
COPY src/LinkTracker.AiAgent/LinkTracker.AiAgent.csproj LinkTracker.AiAgent/
COPY src/LinkTracker.AiAgent/appsettings.json LinkTracker.AiAgent/

# Restore
RUN dotnet restore LinkTracker.AiAgent/LinkTracker.AiAgent.csproj

# Копируем исходники
COPY src/LinkTracker.Contracts/ LinkTracker.Contracts/
COPY src/LinkTracker.AiAgent/ LinkTracker.AiAgent/

# Publish
WORKDIR /src/LinkTracker.AiAgent
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false

# Runtime image
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "LinkTracker.AiAgent.dll"]


