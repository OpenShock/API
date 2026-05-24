# syntax = devthefuture/dockerfile-x

FROM ./docker/Base.Dockerfile#build-common AS build-api

# Node + pnpm: required by the API.csproj MSBuild target that renders the
# React-Email templates to SmtpTemplates/dist/*.liquid during publish.
RUN apk add --no-cache nodejs npm \
 && npm install -g --no-audit --no-fund pnpm@10.33.2

COPY --link API/*.csproj API/
RUN dotnet restore API/API.csproj

COPY --link API/. API/

RUN dotnet publish --no-restore -c Release API/API.csproj -o /app

# final is the final runtime stage for running the app
FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS final-api
WORKDIR /app

COPY docker/entrypoint.sh /entrypoint.sh
RUN chmod +x /entrypoint.sh
RUN apk update && apk add --no-cache openssl

COPY --link --from=build-api /app .
COPY docker/appsettings.API.json /app/appsettings.Container.json

ENTRYPOINT ["/bin/ash", "/entrypoint.sh", "OpenShock.API.dll"]
