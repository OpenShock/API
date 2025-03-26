FROM openshock.local/openshock/build-common AS build-api

COPY --link API/*.csproj API/
RUN dotnet restore API/API.csproj

COPY --link API/. API/

RUN dotnet publish --no-restore -c Release API/API.csproj -o /app

# final is the final runtime stage for running the app
FROM mcr.microsoft.com/dotnet/aspnet:9.0-alpine AS final-api
WORKDIR /app

COPY docker/entrypoint.sh /entrypoint.sh
RUN chmod +x /entrypoint.sh
RUN apk update && apk add --no-cache openssl

COPY --link --from=build-api /app .
COPY docker/appsettings.API.json /app/appsettings.Container.json

ENTRYPOINT ["/bin/ash", "/entrypoint.sh", "OpenShock.API.dll"]