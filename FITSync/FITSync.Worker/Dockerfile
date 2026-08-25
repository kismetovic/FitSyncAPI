# The aspnet image, not the plain runtime one: the worker links FITSync.Infrastructure,
# which carries a framework reference to Microsoft.AspNetCore.App (Identity, HTTP
# abstractions). The worker still exposes no HTTP endpoint of its own.
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Restore layer is cached separately from source changes
COPY ["FITSync.Worker/FITSync.Worker.csproj", "FITSync.Worker/"]
COPY ["FITSync.Infrastructure/FITSync.Infrastructure.csproj", "FITSync.Infrastructure/"]
COPY ["FITSync.Contracts/FITSync.Contracts.csproj", "FITSync.Contracts/"]
COPY ["FITSync.Domain/FITSync.Domain.csproj", "FITSync.Domain/"]
RUN dotnet restore "./FITSync.Worker/FITSync.Worker.csproj"

COPY . .
WORKDIR "/src/FITSync.Worker"
RUN dotnet build "FITSync.Worker.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "FITSync.Worker.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "FITSync.Worker.dll"]
