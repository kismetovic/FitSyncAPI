FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Copy only .csproj files first so the restore layer is cached separately from source changes
COPY ["FITSync/FITSync.WebAPI.csproj", "FITSync/"]
COPY ["FITSync.Infrastructure/FITSync.Infrastructure.csproj", "FITSync.Infrastructure/"]
COPY ["FITSync.Contracts/FITSync.Contracts.csproj", "FITSync.Contracts/"]
COPY ["FITSync.Domain/FITSync.Domain.csproj", "FITSync.Domain/"]
RUN dotnet restore "./FITSync/FITSync.WebAPI.csproj"

# Copy all source and build
COPY . .
WORKDIR "/src/FITSync"
RUN dotnet build "FITSync.WebAPI.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "FITSync.WebAPI.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "FITSync.WebAPI.dll"]
