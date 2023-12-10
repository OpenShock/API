FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine
WORKDIR /app
COPY publish .
ENTRYPOINT ["dotnet", "OpenShock.API.dll"]
