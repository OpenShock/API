FROM mcr.microsoft.com/dotnet/aspnet:9.0-alpine
WORKDIR /app
COPY publish .
ENTRYPOINT ["dotnet", "OpenShock.API.dll"]
