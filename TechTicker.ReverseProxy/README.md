# TechTicker API Gateway

The TechTicker API Gateway serves as the single entry point for all client requests to the TechTicker microservices ecosystem. Built with ASP.NET Core and YARP (Yet Another Reverse Proxy), it provides authentication, authorization, rate limiting, request routing, and response aggregation capabilities.

## Features

### Core Functionality
- **Request Routing**: Routes requests to appropriate backend services based on URL patterns
- **Authentication**: JWT Bearer token validation for protected endpoints
- **Authorization**: Role-based access control with multiple authorization policies
- **Rate Limiting**: Configurable rate limiting with different policies for different endpoint types
- **CORS**: Cross-Origin Resource Sharing support with configurable origins
- **Health Checks**: Comprehensive health monitoring for downstream services

### Advanced Features
- **Request Validation**: Input validation and request size limits
- **Response Aggregation**: Enhanced response metadata and future support for multi-service aggregation
- **Detailed Logging**: Request/response logging with correlation IDs
- **Error Handling**: Standardized error responses across all services
- **Security Headers**: Automatic security headers for all responses

## Architecture

The API Gateway follows the patterns established in the TechTicker documentation:

### Supported Routes

| Route Pattern | Target Service | Auth Required | Rate Limit Policy |
|---------------|----------------|---------------|-------------------|
| `/api/v1/categories/**` | Product Service | Yes | readonly |
| `/api/v1/products/**` | Product Service | Yes | readonly |
| `/api/v1/mappings/**` | Product Seller Mapping Service | Yes | default |
| `/api/v1/site-configs/**` | Product Seller Mapping Service | Yes | default |
| `/api/v1/auth/connect/**` | User Service | No | auth |
| `/connect/**` | User Service | No | auth |
| `/api/v1/users/**` | User Service | Yes | default |
| `/api/v1/prices/**` | Price History Service | Yes | readonly |
| `/api/v1/alerts/**` | Alert Definition Service | Yes | default |

### Authentication Flow

1. Client obtains JWT token from User Service (`/connect/token`)
2. Client includes token in `Authorization: Bearer <token>` header
3. API Gateway validates token and extracts user claims
4. Request is forwarded to backend service with user context headers

### Rate Limiting Policies

- **auth**: 10 requests/minute (for authentication endpoints)
- **default**: 100 requests/minute (standard API operations)
- **readonly**: 200 requests/minute (read-only operations like GET requests)

## Configuration

### JWT Settings
```json
{
  "Jwt": {
    "Issuer": "https://localhost:7001",
    "Audience": "techticker-api",
    "SecretKey": "your-secret-key-here"
  }
}
```

### CORS Settings
```json
{
  "Cors": {
    "AllowedOrigins": ["http://localhost:3000"],
    "AllowedMethods": ["GET", "POST", "PUT", "DELETE", "PATCH", "OPTIONS"],
    "AllowedHeaders": ["*"],
    "AllowCredentials": true
  }
}
```

### Rate Limiting Settings
```json
{
  "RateLimiting": {
    "DefaultPolicy": {
      "PermitLimit": 100,
      "Window": "00:01:00",
      "QueueLimit": 50
    }
  }
}
```

## Request/Response Headers

### Added Request Headers
The gateway automatically adds these headers to downstream requests:

- `X-Correlation-ID`: Unique identifier for request tracing
- `X-API-Gateway`: Gateway identifier
- `X-Gateway-Version`: Gateway version
- `X-Gateway-Timestamp`: Request timestamp
- `X-Original-Client-IP`: Original client IP address
- `X-User-ID`: Authenticated user ID (if available)
- `X-User-Roles`: User roles (if available)

### Added Response Headers
- `X-API-Gateway`: Gateway identifier
- `X-Gateway-Version`: Gateway version
- `X-Response-Time`: Response timestamp
- `X-Correlation-ID`: Request correlation ID
- Security headers (X-Content-Type-Options, X-Frame-Options, etc.)

## Health Checks

The gateway provides multiple health check endpoints:

- `/health`: Basic health check
- `/health/ready`: Readiness probe
- `/health/live`: Liveness probe

Health checks include monitoring of all downstream services with automatic discovery from configuration.

## Monitoring and Observability

### Logging
- Structured logging with correlation IDs
- Request/response logging with timing information
- Error logging with detailed context
- JWT authentication events

### Metrics
Integration with application metrics for:
- Request rates by endpoint
- Response times
- Error rates
- Rate limiting violations
- Authentication failures

## Error Handling

The gateway provides standardized error responses:

```json
{
  "error": "Error Title",
  "message": "Detailed error message",
  "statusCode": 400,
  "requestId": "trace-identifier",
  "timestamp": "2025-06-15T10:30:00Z"
}
```

## Development

### Running Locally
```bash
dotnet run --project TechTicker.ReverseProxy
```

### Testing
Use the provided `.http` files or configure your preferred API client:

```http
### Get Categories
GET https://localhost:7000/api/v1/categories
Authorization: Bearer <jwt-token>

### Create Product
POST https://localhost:7000/api/v1/products
Authorization: Bearer <jwt-token>
Content-Type: application/json

{
  "name": "Test Product",
  "categoryId": "guid-here"
}
```

## Production Considerations

### Security
- Use strong JWT secret keys
- Configure specific CORS origins
- Enable HTTPS only
- Monitor authentication failures
- Implement IP whitelisting if needed

### Performance
- Adjust rate limiting based on expected load
- Monitor downstream service health
- Configure appropriate timeouts
- Consider horizontal scaling

### Monitoring
- Set up comprehensive logging aggregation
- Configure alerts for high error rates
- Monitor response times
- Track rate limiting violations

## Integration with Aspire

The API Gateway is fully integrated with .NET Aspire for:
- Service discovery and registration
- Health checks
- Logging and metrics
- Configuration management
- Dependency injection

## Future Enhancements

- **Response Caching**: Cache frequently accessed data
- **Request Transformation**: Advanced request/response transformation
- **Circuit Breaker**: Automatic circuit breaking for failing services
- **Load Balancing**: Advanced load balancing strategies
- **API Versioning**: Support for multiple API versions
- **WebSocket Support**: Real-time communication support

### Startup Dependencies and Health Monitoring

The API Gateway implements comprehensive startup dependency management:

- **Service Dependencies**: Waits for all downstream API services before becoming ready
- **Health Validation**: Continuous monitoring of downstream service health
- **Startup Health Check**: Validates all dependencies before accepting traffic
- **Background Monitoring**: Periodic health checks with alerting

For detailed information, see [STARTUP_DEPENDENCIES.md](STARTUP_DEPENDENCIES.md).

### Scalar API Documentation

Enhanced API documentation powered by Scalar:

- **Interactive Interface**: Beautiful, responsive documentation UI
- **Code Generation**: Client code samples in multiple languages
- **Authentication Testing**: Built-in JWT token testing
- **Comprehensive Documentation**: All endpoints, models, and examples

For detailed information, see [SCALAR_DOCUMENTATION.md](SCALAR_DOCUMENTATION.md).
