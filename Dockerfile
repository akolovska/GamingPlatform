# Builds the whole solution, runs the tests, and makes a small image for the web app.
#
#   docker build -t gamingplatform .                         the app (stage "final")
#   docker build --target test -t gamingplatform-tests .     the tests
#   docker run --rm -e TEST_CONNECTION_STRING="Server=...;Database=GamingPlatform_Tests;..." gamingplatform-tests
#
# The tests need a SQL Server (see .github/workflows/ci.yml). Secrets are never baked into the
# image: .dockerignore leaves out .env; pass them as environment variables when you run the app.

# ---------- Runtime base (also what Visual Studio's "Container (Dockerfile)" profile runs) ----------
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

# ---------- Build ----------
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
# Project files first: 'dotnet restore' stays cached until a dependency changes
COPY GamingPlatform2.sln ./
COPY GamingPlatform.Domain/GamingPlatform.Domain.csproj GamingPlatform.Domain/
COPY GamingPlatform.Repository/GamingPlatform.Repository.csproj GamingPlatform.Repository/
COPY GamingPlatform.Service/GamingPlatform.Service.csproj GamingPlatform.Service/
COPY GamingPlatform.Web/GamingPlatform.Web.csproj GamingPlatform.Web/
COPY GamingPlatform.Tests/GamingPlatform.Tests.csproj GamingPlatform.Tests/
RUN dotnet restore GamingPlatform2.sln
COPY . .
RUN dotnet build GamingPlatform2.sln -c Release --no-restore

# ---------- Tests ----------
FROM build AS test
# TEST_CONNECTION_STRING must point at a SQL Server; the database name must end in _Tests
ENTRYPOINT ["dotnet", "test", "GamingPlatform.Tests/GamingPlatform.Tests.csproj", "-c", "Release", "--no-build", "--logger", "console;verbosity=normal"]

# ---------- Publish ----------
FROM build AS publish
RUN dotnet publish GamingPlatform.Web/GamingPlatform.Web.csproj -c Release --no-build -o /app/publish /p:UseAppHost=false

# ---------- The app ----------
FROM base AS final
COPY --from=publish /app/publish .
# Uploaded covers, screenshots and avatars live here: mount a volume so they survive new containers
RUN mkdir -p wwwroot/uploads && chown -R $APP_UID wwwroot/uploads
# Run as the image's unprivileged user, not root
USER $APP_UID
ENTRYPOINT ["dotnet", "GamingPlatform.Web.dll"]
