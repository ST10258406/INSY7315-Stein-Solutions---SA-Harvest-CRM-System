# ---- Stage 1: Build ----
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy csproj files first (layer caching — deps only reinstall if these change)
COPY ["src/CRM.API/CRM.API.csproj", "src/CRM.API/"]
COPY ["src/CRM.Application/CRM.Application.csproj", "src/CRM.Application/"]
COPY ["src/CRM.Infrastructure/CRM.Infrastructure.csproj", "src/CRM.Infrastructure/"]
COPY ["src/CRM.Domain/CRM.Domain.csproj", "src/CRM.Domain/"]

RUN dotnet restore "src/CRM.API/CRM.API.csproj"

# Now copy everything else and build
COPY . .
WORKDIR "/src/src/CRM.API"
RUN dotnet publish "CRM.API.csproj" -c Release -o /app/publish

# ---- Stage 2: Runtime ----
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

EXPOSE 8080
ENTRYPOINT ["dotnet", "CRM.API.dll"]