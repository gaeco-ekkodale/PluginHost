# Basis-Image für die Laufzeit
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:80
ENV ASPNETCORE_ENVIRONMENT=Production
EXPOSE 80

# Build-Stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Kopiere alle Projektdateien für bessere Layer-Caching
COPY Api/PluginHost.Api.csproj Api/
COPY Infrastructure/PluginHost.Infrastructure.csproj Infrastructure/
COPY Domain/PluginHost.Domain.csproj Domain/

# Restore Abhängigkeiten für alle Projekte
RUN dotnet restore Api/PluginHost.Api.csproj

# Kopiere den restlichen Source Code
COPY Api/ Api/
COPY Infrastructure/ Infrastructure/
COPY Domain/ Domain/

# Build das API-Projekt
WORKDIR /src/Api
RUN dotnet build PluginHost.Api.csproj -c Release -o /app/build

# Publish-Stage
FROM build AS publish
RUN dotnet publish PluginHost.Api.csproj -c Release -o /app/publish /p:UseAppHost=false

# Final-Stage für das Produktions-Image
FROM base AS final
WORKDIR /app

# Kopiere den publish-Ordner aus der vorherigen Stage
COPY --from=publish /app/publish .

# Startet die Anwendung
ENTRYPOINT ["dotnet", "PluginHost.Api.dll"]
