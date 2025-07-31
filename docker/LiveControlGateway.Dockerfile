# syntax = devthefuture/dockerfile-x

FROM ./docker/Base.Dockerfile#build-common AS build-gateway

COPY --link LiveControlGateway/*.csproj LiveControlGateway/
RUN dotnet restore LiveControlGateway/LiveControlGateway.csproj

COPY --link LiveControlGateway/. LiveControlGateway/

RUN dotnet publish --no-restore -c Release LiveControlGateway/LiveControlGateway.csproj -o /app

# final is the final runtime stage for running the app
FROM mcr.microsoft.com/dotnet/aspnet:9.0-alpine AS final-gateway
WORKDIR /app

COPY docker/entrypoint.sh /entrypoint.sh
RUN chmod +x /entrypoint.sh
RUN apk update && apk add --no-cache openssl

COPY --link --from=build-gateway /app .
COPY docker/appsettings.LiveControlGateway.json /app/appsettings.Container.json

ENTRYPOINT ["/bin/ash", "/entrypoint.sh", "OpenShock.LiveControlGateway.dll"]