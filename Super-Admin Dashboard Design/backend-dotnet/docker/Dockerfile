# =================================
# Build Stage
# =================================
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy solution and project files
COPY SuperAdminDashboard.sln .
COPY src/SuperAdminDashboard.Domain/*.csproj src/SuperAdminDashboard.Domain/
COPY src/SuperAdminDashboard.Application/*.csproj src/SuperAdminDashboard.Application/
COPY src/SuperAdminDashboard.Infrastructure/*.csproj src/SuperAdminDashboard.Infrastructure/
COPY src/SuperAdminDashboard.API/*.csproj src/SuperAdminDashboard.API/

# Restore dependencies
RUN dotnet restore

# Copy source code
COPY . .

# Build and publish
WORKDIR /src/src/SuperAdminDashboard.API
RUN dotnet publish -c Release -o /app/publish --no-restore

# =================================
# Runtime Stage
# =================================
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Create non-root user
RUN addgroup --system --gid 1001 dotnet && \
    adduser --system --uid 1001 --ingroup dotnet dotnet

# Copy published files
COPY --from=build /app/publish .

# Set ownership
RUN chown -R dotnet:dotnet /app

# Switch to non-root user
USER dotnet

# Expose port
EXPOSE 5000

# Health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=5s --retries=3 \
    CMD curl --fail http://localhost:5000/health || exit 1

# Set environment variables
ENV ASPNETCORE_URLS=http://+:5000
ENV ASPNETCORE_ENVIRONMENT=Production

# Run application
ENTRYPOINT ["dotnet", "SuperAdminDashboard.API.dll"]
