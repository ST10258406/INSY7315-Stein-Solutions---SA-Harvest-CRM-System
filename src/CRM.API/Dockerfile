# ---- Stage 1: Build ----
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy csproj files first (layer caching — deps only reinstall if these change)
COPY ["src/CRM.API/CRM.API.csproj", "src/CRM.API/"]
COPY ["src/CRM.Application/CRM.Application.csproj", "src/CRM.Application/"]
COPY ["src/CRM.Infrastructure/CRM.Infrastructure.csproj", "src/CRM.Infrastructure/"]
COPY ["src/CRM.Domain/CRM.Domain.csproj", "src/CRM.Domain/"]

RUN dotnet restore "src/CRM.API/CRM.API.csproj"

# Now copy everything else and build. What gets copied is controlled by the
# .dockerignore at the repository root — keep secrets (.env) out of it.
COPY . .
WORKDIR "/src/src/CRM.API"
RUN dotnet publish "CRM.API.csproj" -c Release -o /app/publish --no-restore

# ---- Stage 2: Runtime ----
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

# Run as the non-root user built into the .NET 8+ images ($APP_UID, uid 1654).
# Port 8080 is non-privileged, so no root is needed to bind it. Set App Service's
# WEBSITES_PORT=8080 to match.
USER $APP_UID
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

# No HEALTHCHECK instruction: the aspnet runtime image ships without curl/wget, and
# Azure App Service ignores Dockerfile HEALTHCHECK anyway. Configure App Service's
# own health check against GET /health (see WebApplicationExtensions.MapHealthChecks).
ENTRYPOINT ["dotnet", "CRM.API.dll"]
