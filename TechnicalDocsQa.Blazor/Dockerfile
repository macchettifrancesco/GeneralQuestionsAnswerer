# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj and restore dependencies
COPY ["TechnicalDocsQa.Blazor.csproj", "./"]
RUN dotnet restore "TechnicalDocsQa.Blazor.csproj"

# Copy everything else and build
COPY . .
RUN dotnet build "TechnicalDocsQa.Blazor.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish "TechnicalDocsQa.Blazor.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Final stage - nginx to serve the static files
FROM nginx:alpine
COPY --from=publish /app/publish/wwwroot /usr/share/nginx/html
COPY nginx.conf /etc/nginx/nginx.conf
EXPOSE 80
