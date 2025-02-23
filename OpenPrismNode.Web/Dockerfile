# Stage 1: Build & Publish
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release

WORKDIR /src

# Copy csproj files and restore
COPY ["OpenPrismNode.Web/OpenPrismNode.Web.csproj", "OpenPrismNode.Web/"]
COPY ["OpenPrismNode.Sync/OpenPrismNode.Sync.csproj", "OpenPrismNode.Sync/"]
COPY ["OpenPrismNode.Core/OpenPrismNode.Core.csproj", "OpenPrismNode.Core/"]
COPY ["OpenPrismNode.Grpc/OpenPrismNode.Grpc.csproj", "OpenPrismNode.Grpc/"]

RUN dotnet restore "OpenPrismNode.Web/OpenPrismNode.Web.csproj"

# Copy everything else and build
COPY . .
WORKDIR "/src/OpenPrismNode.Web"
RUN dotnet build "OpenPrismNode.Web.csproj" -c $BUILD_CONFIGURATION -o /app/build

# Publish
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "OpenPrismNode.Web.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# Stage 2: Final runtime image
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final

WORKDIR /app

# Expose the ports defined in your appsettings.json
#   5001 = HTTPS (REST + Blazor UI)
#   50053 = gRPC
EXPOSE 5001
EXPOSE 50053

COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "OpenPrismNode.Web.dll"]
