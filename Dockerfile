FROM mcr.microsoft.com/dotnet/aspnet:7.0-alpine
WORKDIR /app
COPY publish .
ENTRYPOINT ["dotnet", "ShockLink.API.dll"]
