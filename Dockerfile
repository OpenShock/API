FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build-env

WORKDIR /build

COPY *.sln .

COPY API/*.csproj ./API/
COPY Common/*.csproj ./Common/
COPY Cron/*.csproj ./Cron/
COPY LiveControlGateway/*.csproj ./LiveControlGateway/
COPY MigrationHelper/*.csproj ./MigrationHelper/
COPY ServicesCommon/*.csproj ./ServicesCommon/

RUN dotnet restore

COPY . .

RUN dotnet publish -c Release

# API target
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS api

WORKDIR /app

COPY --from=build-env /build/API/bin/Release/net8.0/publish .

ENTRYPOINT ["dotnet", "OpenShock.API.dll"]

# Cron target
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS cron

WORKDIR /app

COPY --from=build-env /build/Cron/bin/Release/net8.0/publish .

ENTRYPOINT ["dotnet", "OpenShock.Cron.dll"]

# LiveControlGateway target
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS livecontrolgateway

WORKDIR /app

COPY --from=build-env /build/LiveControlGateway/bin/Release/net8.0/publish .

ENTRYPOINT ["dotnet", "OpenShock.LiveControlGateway.dll"]
