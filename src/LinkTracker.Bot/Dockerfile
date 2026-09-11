# src/LinkTracker.Bot/Dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app

# Устанавливаем корневые сертификаты
RUN apt-get update && apt-get install -y ca-certificates && update-ca-certificates
# Внутри контейнера
RUN apt-get update && apt-get install -y curl

EXPOSE 80
EXPOSE 443


FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Копируем .csproj файлы
COPY ["src/LinkTracker.Bot/LinkTracker.Bot.csproj", "LinkTracker.Bot/"]
COPY ["src/LinkTracker.Scrapper/LinkTracker.Scrapper.csproj", "LinkTracker.Scrapper/"]
RUN dotnet restore "LinkTracker.Bot/LinkTracker.Bot.csproj"

# Копируем все исходники
COPY src/ .

WORKDIR "/src/LinkTracker.Bot"
RUN dotnet build "LinkTracker.Bot.csproj" -c Release -o /app/build

FROM build AS publish
# Удаляем все appsettings.json не из Bot перед публикацией, чтобы избежать конфликтов
RUN find /src -name "appsettings*.json" ! -path "*/LinkTracker.Bot/*" -delete
RUN dotnet publish "LinkTracker.Bot.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
COPY src/LinkTracker.Bot/.env .env

ENTRYPOINT ["dotnet", "LinkTracker.Bot.dll"]