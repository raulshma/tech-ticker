# TechTicker ProductSellerMappingService

A microservice for managing product seller mappings in the TechTicker ecosystem. This service handles the relationships between canonical products and seller-specific product URLs, enabling the Orchestrator service to determine which products to scrape from which sellers.

## Overview

The ProductSellerMappingService is responsible for:
- Managing mappings between canonical products and seller-specific URLs
- Providing active mappings to the Orchestrator for scraping operations
- Maintaining scraping configuration and frequency overrides
- Tracking scraping history and scheduling information

## Architecture

The service follows clean architecture principles with:
- **Controllers**: HTTP API endpoints following RESTful conventions
- **Services**: Business logic and domain operations
- **DTOs**: Data transfer objects for API boundaries
- **Data**: Entity Framework Core with PostgreSQL
- **Shared Infrastructure**: Common utilities, exception handling, and response patterns

## Key Features

### 🎯 Core Functionality
- **CRUD Operations**: Complete management of product seller mappings
- **Duplicate Prevention**: Ensures unique product-seller combinations
- **Active Mapping Filtering**: Optimized endpoint for orchestrator consumption
- **Flexible Querying**: Search, filter, and paginate mappings efficiently

### 🛡️ Quality & Reliability
- **Global Exception Handling**: Consistent error responses across all endpoints
- **Input Validation**: Comprehensive validation with detailed error messages
- **Correlation ID Tracking**: Request tracing across service boundaries
- **Result Pattern**: Robust error handling and success/failure flow

### 📊 Performance & Scalability
- **Database Optimization**: Indexed queries for common access patterns
- **Pagination**: Efficient handling of large datasets
- **Caching-Ready**: Structured for future caching implementation

## API Endpoints

### Core CRUD Operations
- `POST /api/mappings` - Create new mapping
- `GET /api/mappings` - List mappings with filtering
- `GET /api/mappings/{id}` - Get specific mapping
- `PUT /api/mappings/{id}` - Update mapping
- `DELETE /api/mappings/{id}` - Delete mapping

### Specialized Endpoints
- `GET /api/mappings/active` - Get active mappings (for Orchestrator)

### Filtering & Search
- Filter by canonical product ID, seller name, or active status
- Full-text search across seller names and URLs
- Pagination support for all list operations

## Data Model

```csharp
public class ProductSellerMapping
{
    public Guid MappingId { get; set; }
    public Guid CanonicalProductId { get; set; }        // Reference to canonical product
    public string SellerName { get; set; }               // e.g., "Newegg US"
    public string ExactProductUrl { get; set; }          // Direct URL to product
    public bool IsActiveForScraping { get; set; }        // Enable/disable scraping
    public string? ScrapingFrequencyOverride { get; set; } // Custom frequency
    public Guid? SiteSpecificSelectorsId { get; set; }   // CSS selector reference
    public DateTimeOffset? LastScrapedAt { get; set; }   // Last scrape timestamp
    public DateTimeOffset? NextScrapeAt { get; set; }    // Next scheduled scrape
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
```

## Business Rules

1. **Uniqueness**: Each canonical product can only have one mapping per seller
2. **URL Validation**: Product URLs must be valid and accessible
3. **Active Status**: Only active mappings are used for scraping
4. **Audit Trail**: All operations are tracked with timestamps

## Integration Points

### With Orchestrator Service
- Provides active mappings for scraping schedules
- Receives scraping results and updates timestamps
- Manages scraping frequency and configuration

### With Product Service
- References canonical products via `CanonicalProductId`
- Maintains loose coupling through ID references

### With Admin Interface
- Full CRUD operations for mapping management
- Advanced filtering and search capabilities
- Bulk operations support (future enhancement)

## Getting Started

### Prerequisites
- .NET 9.0 SDK
- PostgreSQL database
- TechTicker.Shared library

### Configuration
The service uses Aspire for configuration and service discovery:
```json
{
  "ConnectionStrings": {
    "productmapping": "Host=localhost;Database=techticker_productmapping;Username=techticker;Password=..."
  }
}
```

### Running the Service
```bash
dotnet run
```

The service will be available at `http://localhost:5268` (development)

### Database Setup
Entity Framework migrations are used for database schema management:
```bash
dotnet ef migrations add InitialCreate
dotnet ef database update
```

## Development

### Project Structure
```
TechTicker.ProductSellerMappingService/
├── Controllers/
│   └── ProductSellerMappingController.cs
├── Services/
│   ├── IProductSellerMappingService.cs
│   └── ProductSellerMappingService.cs
├── DTOs/
│   └── ProductSellerMappingDTOs.cs
├── Data/
│   └── ProductSellerMappingDbContext.cs
├── Program.cs
└── TechTicker.ProductSellerMappingService.http
```

### Key Dependencies
- **TechTicker.Shared**: Common utilities and infrastructure
- **Entity Framework Core**: Data access and ORM
- **Aspire**: Service configuration and discovery
- **Scalar**: API documentation and testing

### Testing
Use the provided HTTP file for manual testing:
- `TechTicker.ProductSellerMappingService.http`

Comprehensive examples included for all endpoints and scenarios.

## Response Format

All endpoints follow the standardized TechTicker response format:

### Success Response
```json
{
  "success": true,
  "message": "Operation completed successfully",
  "data": { /* response data */ },
  "correlationId": "unique-request-id"
}
```

### Paginated Response
```json
{
  "success": true,
  "data": [/* items */],
  "pageNumber": 1,
  "pageSize": 50,
  "totalCount": 100,
  "totalPages": 2,
  "hasPreviousPage": false,
  "hasNextPage": true,
  "correlationId": "unique-request-id"
}
```

### Error Response
```json
{
  "success": false,
  "message": "Error description",
  "errors": ["Detailed error messages"],
  "correlationId": "unique-request-id"
}
```

## Monitoring & Observability

- **Correlation IDs**: Every request has a unique correlation ID for tracing
- **Structured Logging**: JSON-formatted logs with contextual information
- **Health Checks**: Built-in health endpoints for monitoring
- **Metrics**: Performance and usage metrics (via Aspire)

## Security Considerations

- **Input Validation**: All inputs validated at API boundary
- **SQL Injection Prevention**: Parameterized queries via Entity Framework
- **URL Validation**: Product URLs validated for format and accessibility
- **Data Sanitization**: All string inputs sanitized and length-limited

## Future Enhancements

- **Bulk Operations**: Support for bulk create/update/delete operations
- **Scheduling Integration**: Direct integration with job scheduling services
- **Caching Layer**: Redis caching for frequently accessed mappings
- **Event Sourcing**: Audit trail of all mapping changes
- **Health Monitoring**: Enhanced monitoring of mapping health and success rates

## Documentation

- [API Documentation](./MAPPING_API.md) - Detailed API reference
- [HTTP Test File](./TechTicker.ProductSellerMappingService.http) - Manual testing examples

## Contributing

Follow the established patterns from other TechTicker services:
1. Use the shared response format
2. Implement proper error handling
3. Add comprehensive validation
4. Include correlation ID support
5. Follow clean architecture principles

---

Built with ❤️ for the TechTicker ecosystem
