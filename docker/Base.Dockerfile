FROM mcr.microsoft.com/dotnet/sdk:9.0-alpine AS build-common
WORKDIR /src

COPY --link Common/*.csproj Common/
COPY --link *.props .
RUN dotnet restore Common/Common.csproj

COPY --link Common/. Common/
COPY --link .git/ .git/

RUN dotnet build --no-restore -c Release Common/Common.csproj

FROM build-common AS test-common
WORKDIR /src

COPY --link Common.Tests/*.csproj Common.Tests/
RUN dotnet restore Common.Tests/Common.Tests.csproj
COPY --link Common.Tests/. Common.Tests/
RUN dotnet build --no-restore -c Release Common.Tests/Common.Tests.csproj
ENTRYPOINT ["dotnet", "test", "--no-build", "-c", "Release", "Common.Tests/Common.Tests.csproj"]

