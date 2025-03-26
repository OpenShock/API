FROM openshock.local/openshock/build-common AS build-cron

COPY --link Cron/*.csproj Cron/
RUN dotnet restore Cron/Cron.csproj

COPY --link Cron/. Cron/

RUN dotnet publish --no-restore -c Release Cron/Cron.csproj -o /app

# final is the final runtime stage for running the app
FROM mcr.microsoft.com/dotnet/aspnet:9.0-alpine AS final-cron
WORKDIR /app

COPY docker/entrypoint.sh /entrypoint.sh
RUN chmod +x /entrypoint.sh
RUN apk update && apk add --no-cache openssl

COPY --link --from=build-cron /app .
COPY docker/appsettings.Cron.json /app/appsettings.Container.json

ENTRYPOINT ["/bin/ash", "/entrypoint.sh", "OpenShock.Cron.dll"]