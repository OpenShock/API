FROM mcr.microsoft.com/dotnet/sdk:9.0-alpine AS build-common
WORKDIR /src

COPY --link Common/*.csproj Common/
COPY --link *.props .
RUN dotnet restore Common/Common.csproj

COPY --link Common/. Common/
COPY --link CodeGen/. CodeGen/
COPY --link .git/ .

RUN dotnet build --no-restore -c Release Common/Common.csproj