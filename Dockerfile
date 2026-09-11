# src/LinkTracker.AiAgent/Dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Копируем файлы проектов
COPY ["src/LinkTracker.AiAgent/LinkTracker.AiAgent.csproj", "LinkTracker.AiAgent/"]
RUN dotnet restore "LinkTracker.AiAgent/LinkTracker.AiAgent.csproj"

# Копируем все исходники
COPY src/ .
WORKDIR "/src/LinkTracker.AiAgent"
RUN dotnet build "LinkTracker.AiAgent.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "LinkTracker.AiAgent.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

ENTRYPOINT ["dotnet", "LinkTracker.AiAgent.dll"]