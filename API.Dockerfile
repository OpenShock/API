FROM mcr.microsoft.com/dotnet/sdk:7.0-alpine AS build

WORKDIR /src
COPY . .
WORKDIR "/src/ShockLinkAPI"

RUN dotnet publish "API.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:7.0-alpine
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "ShockLink.API.dll"]