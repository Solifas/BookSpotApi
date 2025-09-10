# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy solution and project files
COPY BookSpot.sln ./
COPY BookSpot.API/BookSpot.API.csproj BookSpot.API/
COPY BookSpot.Application/BookSpot.Application.csproj BookSpot.Application/
COPY BookSpot.Domain/BookSpot.Domain.csproj BookSpot.Domain/
COPY BookSpot.Infrastructure/BookSpot.Infrastructure.csproj BookSpot.Infrastructure/

# Restore dependencies
RUN dotnet restore

# Copy source code
COPY . .

# Build the application
WORKDIR /src/BookSpot.API
RUN dotnet build -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Create non-root user
RUN groupadd -r bookspot && useradd -r -g bookspot bookspot

# Copy published application
COPY --from=publish /app/publish .

# Set ownership
RUN chown -R bookspot:bookspot /app
USER bookspot

# Expose port
EXPOSE 8080

# Set environment variables
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

# Health check
HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
    CMD curl -f http://localhost:8080/health || exit 1

# Entry point
ENTRYPOINT ["dotnet", "BookSpot.API.dll"]