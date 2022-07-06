
FROM maven:3.8.6-eclipse-temurin-8-alpine AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["JavaPackagePublishing.csproj", "JavaPackagePublishing/"]
RUN dotnet restore "JavaPackagePublishing/JavaPackagePublishing.csproj"

WORKDIR "/src/JavaPackagePublishing"
COPY . .
RUN dotnet build "JavaPackagePublishing.csproj" --sc true -c Release -r linux-x64 -o /app/build

FROM build AS publish
RUN dotnet publish "JavaPackagePublishing.csproj" --sc true -c Release -r linux-x64 -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
CMD ["/bin/sh"]