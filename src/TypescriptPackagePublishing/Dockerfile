
FROM node:18.6.0-alpine3.16 AS base
WORKDIR /app
EXPOSE 80

# Directory where pip executable is located
ENV NPM_INSTALLATION_DIR=/usr/local/bin

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["TypescriptPackagePublishing.csproj", "TypescriptPackagePublishing/"]
RUN dotnet restore "TypescriptPackagePublishing/TypescriptPackagePublishing.csproj"

WORKDIR "/src/TypescriptPackagePublishing"
COPY . .
RUN dotnet build "TypescriptPackagePublishing.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "TypescriptPackagePublishing.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app

RUN apk add --no-cache \
    ca-certificates \
    \
    # .NET CORE dependencies
    krb5-libs \
    libgcc \
    libintl \
    libssl1.1 \
    libstdc++ \
    zlib

ENV \
    # Configure web servers to bind to port 80 when present
    ASPNETCORE_URLS=http://+:80 \
    # Enable detection of running in a container
    DOTNET_RUNNING_IN_CONTAINER=true \
    # Set the invariant mode since ICU package isn't included (see https://github.com/dotnet/announcements/issues/20)
    DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=true \
    # .NET Runtime version
    DOTNET_VERSION=6.0.6 \
    # ASP.NET Core version
    ASPNET_VERSION=6.0.6 \
    # ASP.NET Environment
    ASPNETCORE_ENVIRONMENT=Production

# Install .NET Runtime
RUN wget -O dotnet.tar.gz https://dotnetcli.azureedge.net/dotnet/Runtime/$DOTNET_VERSION/dotnet-runtime-$DOTNET_VERSION-linux-musl-x64.tar.gz \
    && dotnet_sha512='c950e64d7d95e979032cf2eccbc41911380141e8fb529aca5c05f23c4eb480d0182a8f26c64305efbfe409af3ad7ed66197b10e861ac795098c653ce6b1aa41f' \
    && echo "$dotnet_sha512  dotnet.tar.gz" | sha512sum -c - \
    && mkdir -p /usr/share/dotnet \
    && tar -oxzf dotnet.tar.gz -C /usr/share/dotnet \
    && rm dotnet.tar.gz \
    && ln -s /usr/share/dotnet/dotnet /usr/bin/dotnet \
    # and then ASP.NET Core
    && wget -O aspnetcore.tar.gz https://dotnetcli.azureedge.net/dotnet/aspnetcore/Runtime/$ASPNET_VERSION/aspnetcore-runtime-$ASPNET_VERSION-linux-musl-x64.tar.gz \
    && aspnetcore_sha512='0ecaf750ecf0eb1da601baed2c71a7a09994fe82ef11c4896c57e07d480e5820fb9f92b5fe55e35bf72f067894b85d7d33d3955f15b670d4282ec5049e52f124' \
    && echo "$aspnetcore_sha512  aspnetcore.tar.gz" | sha512sum -c - \
    && tar -oxzf aspnetcore.tar.gz -C /usr/share/dotnet ./shared/Microsoft.AspNetCore.App \
    && rm aspnetcore.tar.gz

COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet","TypescriptPackagePublishing.dll"]