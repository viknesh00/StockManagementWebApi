# Stock Management Web API

A RESTful Web API for managing stock inventory, built with ASP.NET Core 8.0.

## Table of Contents

- [Overview](#overview)
- [Technology Stack](#technology-stack)
- [Prerequisites](#prerequisites)
- [Getting Started](#getting-started)
- [Project Structure](#project-structure)
- [API Documentation](#api-documentation)
- [Configuration](#configuration)
- [Development Guidelines](#development-guidelines)
- [Database](#database)

---

## Overview

This API provides endpoints for managing stock operations including:

- Inbound and outbound stock tracking (CII and Non-CII)
- Company management
- User management and authentication
- Stock returns processing
- Excel import/export capabilities
- Dashboard analytics

## Technology Stack

| Component | Technology |
|-----------|------------|
| Runtime | .NET 8.0 |
| Framework | ASP.NET Core |
| Database | SQL Server (Azure) |
| ORM | Entity Framework Core 9.0 |
| API Docs | Swashbuckle (Swagger) |
| Excel | EPPlus 7.5.2 |

## Prerequisites

- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- SQL Server (local or Azure)
- IDE: Visual Studio 2022 / VS Code / Rider

## Getting Started

### 1. Clone the repository

```bash
git clone <repository-url>
cd StockManagementWebApi
```

### 2. Configure database connection

Update `appsettings.json` with your SQL Server connection string:

```json
{
  "ConnectionStrings": {
    "MyDBConnection": "Server=<server>;Initial Catalog=<database>;..."
  }
}
```

### 3. Apply migrations

```bash
dotnet ef database update
```

### 4. Run the application

```bash
dotnet run
```

The API will launch with Swagger UI at:
- HTTP: `http://localhost:5243/swagger`
- HTTPS: `https://localhost:7270/swagger`

## Project Structure

```
StockManagementWebApi/
├── Common/
│   ├── Controllers/        # BaseApiController with standard response helpers
│   ├── Exceptions/         # Custom exception types
│   ├── Files/              # File upload handling
│   └── Models/             # ApiResponse envelope
├── Controllers/            # API endpoints
├── Middleware/              # CorrelationId, GlobalException handling
├── Models/                 # EF Core entities and DTOs
├── Services/               # Business logic layer
├── Properties/             # launchSettings.json
├── Program.cs              # Application entry point
└── appsettings.json        # Configuration
```

### Key Components

| Component | Purpose |
|-----------|---------|
| `BaseApiController` | Base class providing standard response helpers (`Success`, `Created`, `BadRequest`, etc.) |
| `ApiResponse` | Uniform response envelope for all endpoints |
| `CorrelationIdMiddleware` | Adds trace identifier to every request |
| `GlobalExceptionMiddleware` | Catches unhandled exceptions and returns standard error responses |

## API Documentation

### Endpoints

| Controller | Description |
|------------|-------------|
| `LoginController` | Authentication |
| `SmCompaniesController` | Company CRUD |
| `SmUsersController` | User management |
| `UserManagementController` | User role management |
| `SmInboundStockCiisController` | Inbound CII stock |
| `SmInboundStockNonCiisController` | Inbound non-CII stock |
| `SmOutboundStockCiisController` | Outbound CII stock |

### Response Format

All endpoints return a standard envelope:

```json
{
  "success": true,
  "statusCode": 200,
  "message": "Request processed successfully.",
  "data": {},
  "errors": [],
  "traceId": "00-abc123-...",
  "timestamp": "2026-07-22T00:00:00Z"
}
```

## Configuration

### appsettings.json

| Key | Description |
|-----|-------------|
| `ConnectionStrings.MyDBConnection` | SQL Server connection string |
| `EPPlus.LicenseContext` | EPPlus license type (Commercial) |
| `Logging` | Log levels and providers |

### Environment Variables

Set `ASPNETCORE_ENVIRONMENT` to control behavior:
- `Development` - Enables Swagger, detailed error responses
- `Production` - Disables debug features

## Development Guidelines

### Adding a New Endpoint

1. Create entity model in `Models/`
2. Create DTO in `Models/` if needed
3. Create service interface and implementation in `Services/`
4. Register service in `Program.cs` (use `AddScoped`)
5. Create controller inheriting `BaseApiController`
6. Use response helpers: `Success()`, `Created()`, `NotFound()`, etc.

### Exception Handling

Throw custom exceptions from `Common/Exceptions/` instead of returning error responses directly. The global middleware will catch them and return the standard envelope.

### Logging

Use the injected `ILogger<T>` with structured scopes:

```csharp
using var scope = _logger.BeginScope(new { CorrelationId, StockId });
_logger.LogInformation("Processing stock update");
```

## Database

The application uses Entity Framework Core with SQL Server. 

### Connection

- **Server**: `stockmgmtapi.database.windows.net`
- **Database**: `devstockmgmt`

### Migrations

```bash
# Create a migration
dotnet ef migrations add <MigrationName>

# Apply migrations
dotnet ef database update

# Rollback
dotnet ef database update <PreviousMigration>
```
