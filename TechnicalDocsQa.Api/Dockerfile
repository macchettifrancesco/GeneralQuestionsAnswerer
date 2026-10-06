# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj and restore dependencies
COPY TechnicalDocsQa.Api.csproj .
RUN dotnet restore

# Copy everything else and build
COPY . .
RUN dotnet build -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# Create storage directories
RUN mkdir -p Storage/documents Storage/registry

EXPOSE 8080

COPY --from=publish /app/publish .

ENTRYPOINT ["dotnet", "TechnicalDocsQa.Api.dll"]
