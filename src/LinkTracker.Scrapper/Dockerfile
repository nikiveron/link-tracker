# src/LinkTracker.Scrapper/Dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Копируем файлы проектов
COPY ["src/LinkTracker.Scrapper/LinkTracker.Scrapper.csproj", "LinkTracker.Scrapper/"]
RUN dotnet restore "LinkTracker.Scrapper/LinkTracker.Scrapper.csproj"

# Копируем все исходники
COPY src/ .
WORKDIR "/src/LinkTracker.Scrapper"
RUN dotnet build "LinkTracker.Scrapper.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "LinkTracker.Scrapper.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

ENTRYPOINT ["dotnet", "LinkTracker.Scrapper.dll"]