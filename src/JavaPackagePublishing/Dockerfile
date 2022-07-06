
FROM maven:3.8.6-eclipse-temurin-8-alpine AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["JavaPackagePublishing.csproj", "JavaPackagePublishing/"]
RUN dotnet restore "JavaPackagePublishing/JavaPackagePublishing.csproj"

WORKDIR "/src/JavaPackagePublishing"
COPY . .
RUN dotnet build "JavaPackagePublishing.csproj" -a x64 --sc true -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "JavaPackagePublishing.csproj" -a x64 --sc true -c Release -p:PublishTrimmed=true -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
CMD ["/bin/sh"]