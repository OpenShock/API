FROM mcr.microsoft.com/dotnet/aspnet:7.0-alpine
WORKDIR /app
COPY *.dll .
ENTRYPOINT ["dotnet", "ShockLink.API.dll"]
