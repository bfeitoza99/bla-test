# syntax=docker/dockerfile:1

# ---------------------------------------------------------------------------
# Build stage — .NET 10 SDK. Build context is backend/ so the central
# package-management files (Directory.Packages.props / Directory.Build.props)
# are available to restore.
# ---------------------------------------------------------------------------
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy the solution-wide MSBuild props first so restore sees central package
# management and shared build settings.
COPY Directory.Packages.props Directory.Build.props ./

# Copy only the project files needed to publish the API + its transitive refs.
# (Restore is cached on these unless a .csproj changes.)
COPY src/Bla.Domain/Bla.Domain.csproj          src/Bla.Domain/
COPY src/Bla.Application/Bla.Application.csproj src/Bla.Application/
COPY src/Bla.Infrastructure/Bla.Infrastructure.csproj src/Bla.Infrastructure/
COPY src/Bla.Api/Bla.Api.csproj                src/Bla.Api/

RUN dotnet restore src/Bla.Api/Bla.Api.csproj

# Copy the rest of the source for the four projects the API depends on.
COPY src/Bla.Domain/         src/Bla.Domain/
COPY src/Bla.Application/    src/Bla.Application/
COPY src/Bla.Infrastructure/ src/Bla.Infrastructure/
COPY src/Bla.Api/            src/Bla.Api/

RUN dotnet publish src/Bla.Api/Bla.Api.csproj \
        -c Release \
        --no-restore \
        -o /app/publish

# ---------------------------------------------------------------------------
# Runtime stage — ASP.NET Core 10 runtime only (no SDK).
# ---------------------------------------------------------------------------
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

COPY --from=build /app/publish ./

# Kestrel listens on 8080 (HTTP only inside the container; HTTPS is terminated
# elsewhere). Matches the compose port mapping and the RUNBOOK URLs.
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

ENTRYPOINT ["dotnet", "Bla.Api.dll"]
