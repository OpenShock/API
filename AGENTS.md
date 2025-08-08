# OpenShock Backend repository

## Overview
This repo hosts several C# projects that together form the backend of the OpenShock system. All projects target **.NET 9** (preview). The solution file `OpenShockBackend.slnx` includes the API, Cron jobs, a WebSocket gateway, helper utilities and test projects.

Project directories:
- `API` – main ASP.NET Core API
- `LiveControlGateway` – WebSocket gateway service
- `Cron` – Hangfire based cron daemon
- `Common` – shared classes and EF Core database context
- `MigrationHelper` – CLI helper for EF Core migrations
- `SeedE2E` – utilities to seed E2E test data
- `Common.Tests` – unit tests for the Common library
- `API.IntegrationTests` – integration tests for the API

## Prerequisites
- **.NET 9 SDK** (preview) must be installed in order to build or run the projects.
- **Docker** is required when running the integration tests because they spin up Postgres and Redis containers through Testcontainers.

## Building
Run the following from the repository root:

```bash
dotnet restore
# Build all projects in the solution
dotnet build OpenShockBackend.slnx -c Release
```

## Running locally
Development helpers reside in `Dev/`. The file `Dev/docker-compose.yml` spins up Postgres, Redis and the WebUI for local use. Example steps:

```bash
# Start dependencies for development
cd Dev
docker compose up -d
```

The API itself can then be executed with:

```bash
dotnet run --project API/API.csproj
```

Environment variables are documented in `README.md` and in the `.env` file used by the production `docker-compose.yml`.

## Tests
Unit tests and integration tests can be executed with `dotnet test`. Integration tests require Docker.

```bash
# Unit tests
dotnet test Common.Tests/Common.Tests.csproj -c Release

# Integration tests (needs Docker)
dotnet test API.IntegrationTests/API.IntegrationTests.csproj -c Release
```

## Additional notes
- The repository does not contain build artifacts. Avoid committing the `bin/` or `obj/` folders.
- `Dev/setupUsersecrets.sh` populates development secrets if you are using the .NET user secrets feature.
