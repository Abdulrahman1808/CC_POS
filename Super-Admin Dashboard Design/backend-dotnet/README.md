# Super Admin Dashboard - ASP.NET Core Backend

Production-grade backend API for the Super Admin Dashboard built with Clean Architecture.

## 🚀 Technology Stack

- **Framework**: ASP.NET Core 8
- **Language**: C# 12
- **ORM**: Entity Framework Core 8
- **Database**: PostgreSQL
- **Authentication**: JWT with refresh tokens
- **Real-time**: SignalR
- **Validation**: FluentValidation
- **Mapping**: AutoMapper
- **CQRS**: MediatR
- **Logging**: Serilog
- **Documentation**: Swagger/OpenAPI

## 📋 Prerequisites

- .NET 8 SDK
- PostgreSQL 16+
- (Optional) Docker & Docker Compose

## 🛠️ Quick Start

### Local Development

```bash
# Navigate to backend directory
cd backend-dotnet

# Restore packages
dotnet restore

# Update database connection string in appsettings.Development.json

# Run migrations
cd src/SuperAdminDashboard.API
dotnet ef migrations add InitialCreate --project ../SuperAdminDashboard.Infrastructure
dotnet ef database update --project ../SuperAdminDashboard.Infrastructure

# Run the API
dotnet run
```

### Using Docker

```bash
cd docker
docker-compose up -d
```

## 📚 API Documentation

Once running, visit: `http://localhost:5000/api/docs`

## 🏗️ Project Structure

```
backend-dotnet/
├── src/
│   ├── SuperAdminDashboard.Domain/        # Core entities, enums, interfaces
│   ├── SuperAdminDashboard.Application/   # Business logic, DTOs, validators
│   ├── SuperAdminDashboard.Infrastructure/# Data access, services
│   └── SuperAdminDashboard.API/           # Controllers, middleware, SignalR
├── tests/
├── docker/
└── SuperAdminDashboard.sln
```

## 🔑 Default Credentials

After seeding:
- **Email:** admin@superadmin.com
- **Password:** SuperAdmin@123!

## 📡 API Endpoints

### Authentication
- `POST /api/v1/auth/login` - Login
- `POST /api/v1/auth/logout` - Logout
- `POST /api/v1/auth/refresh` - Refresh token
- `GET /api/v1/auth/me` - Current user

### Tenants
- `GET /api/v1/tenants` - List tenants
- `POST /api/v1/tenants` - Create tenant
- `GET /api/v1/tenants/{id}` - Get tenant
- `PUT /api/v1/tenants/{id}` - Update tenant
- `PATCH /api/v1/tenants/{id}/status` - Update status
- `DELETE /api/v1/tenants/{id}` - Delete tenant

### Users
- `GET /api/v1/users` - List users
- `POST /api/v1/users` - Create user
- `GET /api/v1/users/{id}` - Get user
- `PUT /api/v1/users/{id}` - Update user
- `PATCH /api/v1/users/{id}/status` - Update status
- `DELETE /api/v1/users/{id}` - Delete user

### Analytics
- `GET /api/v1/analytics/overview` - Dashboard KPIs
- `GET /api/v1/analytics/tenants/growth` - Growth charts
- `GET /api/v1/analytics/revenue` - Revenue metrics

### Settings
- `GET /api/v1/settings` - List settings
- `PUT /api/v1/settings/{key}` - Update setting

### SignalR Hub
- `ws://localhost:5000/hubs/admin-dashboard` - Real-time notifications

## 🔒 Security Features

- JWT authentication with access/refresh tokens
- Role-based authorization (SuperAdmin, Admin, Viewer)
- Password hashing (BCrypt)
- Rate limiting
- CORS protection
- Global exception handling
- Request logging with correlation IDs
- Audit logging

## 🔄 Real-time Events

The SignalR hub broadcasts the following events:
- `CustomerLoggedIn` - Customer login activity
- `CustomerPurchased` - Purchase events
- `TenantStatusChanged` - Tenant status updates
- `SystemError` - System error alerts

## 🐳 Docker Commands

```bash
# Build and start
docker-compose up -d --build

# View logs
docker-compose logs -f api

# Stop services
docker-compose down

# Reset database
docker-compose down -v
docker-compose up -d
```

## 📊 Health Checks

- `GET /health` - Basic health check
- `GET /ready` - Readiness check (includes database)
