
FROM mcr.microsoft.com/dotnet/sdk:6.0.302-alpine3.16-amd64 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:6.0.302-alpine3.16-amd64 AS build
WORKDIR /src
COPY ["NetPackagePublishing.csproj", "NetPackagePublishing/"]
RUN dotnet restore "NetPackagePublishing/NetPackagePublishing.csproj"

WORKDIR "/src/NetPackagePublishing"
COPY . .
RUN dotnet build "NetPackagePublishing.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "NetPackagePublishing.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app

ENV \
    # Configure web servers to bind to port 80 when present
    ASPNETCORE_URLS=http://+:80 \
    # Enable detection of running in a container
    DOTNET_RUNNING_IN_CONTAINER=true \
    # ASP.NET Environment
    ASPNETCORE_ENVIRONMENT=Production \
    # Dotnet SDK Installation Directory
    DOTNET_INSTALLATION_DIR=/usr/share/dotnet

COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet","NetPackagePublishing.dll"]