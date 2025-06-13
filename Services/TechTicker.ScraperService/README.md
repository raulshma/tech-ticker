# TechTicker Scraper Service

## Overview

The **TechTicker Scraper Service** is responsible for executing the actual web scraping operations. It consumes scraping commands from the orchestration service and performs HTTP requests to extract product pricing and availability data from e-commerce websites.

## Architecture

This service follows the Worker Service pattern and includes:

- **Workers**: Background services that consume messages and coordinate scraping operations
- **Services**: Core business logic for web scraping, HTML parsing, and message handling
- **Models**: Data structures for scraping configuration and results
- **Messages**: Command and event messages for inter-service communication

## Key Features

### 🌐 Web Scraping Engine
- **HTTP Client Management**: Configurable HTTP clients with cookie support and session management
- **Anti-Detection Features**: User-Agent rotation, custom headers, and randomized timing
- **Retry Logic**: Configurable retry attempts with exponential backoff
- **Timeout Handling**: Configurable request timeouts to prevent hanging requests

### 🔍 HTML Parsing
- **AngleSharp Integration**: Robust HTML parsing using AngleSharp library
- **CSS Selector Support**: Flexible extraction using CSS selectors
- **Price Normalization**: Intelligent price parsing handling various formats and currencies
- **Stock Status Detection**: Smart stock status normalization from various text formats

### 📨 Message Processing
- **RabbitMQ Integration**: Consumes `ScrapeProductPageCommand` messages
- **Event Publishing**: Publishes `RawPriceDataEvent` and `ScrapingResultEvent` messages
- **Error Handling**: Comprehensive error categorization and reporting

### 🛡️ Error Handling & Resilience
- **Categorized Errors**: Detailed error codes for different failure scenarios
- **Rate Limiting Detection**: Automatic detection of rate limiting and CAPTCHA blocks
- **Network Error Handling**: Graceful handling of network timeouts and connectivity issues
- **Parsing Error Recovery**: Robust error handling for malformed HTML or missing selectors

## Configuration

### Application Settings (`appsettings.json`)

```json
{
  "RabbitMQ": {
    "ConnectionString": "amqp://localhost:5672",
    "ScrapeCommandQueue": "scrape-product-page-commands",
    "RawPriceDataExchange": "raw-price-data",
    "ScrapingResultExchange": "scraping-results"
  },
  "ScrapingSettings": {
    "RequestTimeoutSeconds": 30,
    "MaxRetryAttempts": 3,
    "RetryDelaySeconds": 5,
    "EnableCookieManagement": true,
    "DefaultUserAgent": "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36"
  }
}
```

## Message Contracts

### Input: ScrapeProductPageCommand
```csharp
{
  "MappingId": "uuid",
  "CanonicalProductId": "uuid", 
  "SellerName": "Amazon US",
  "ExactProductUrl": "https://amazon.com/product-xyz",
  "Selectors": {
    "ProductNameSelector": ".product-title",
    "PriceSelector": ".price-current",
    "StockSelector": ".stock-status",
    "SellerNameOnPageSelector": ".seller-name"
  },
  "ScrapingProfile": {
    "UserAgent": "Mozilla/5.0...",
    "Headers": {
      "Accept": "text/html,application/xhtml+xml",
      "Accept-Language": "en-US,en;q=0.9"
    }
  }
}
```

### Output: RawPriceDataEvent (on success)
```csharp
{
  "CanonicalProductId": "uuid",
  "SellerName": "Amazon US",
  "ScrapedPrice": 299.99,
  "ScrapedStockStatus": "IN_STOCK",
  "Timestamp": "2025-06-13T10:30:00Z",
  "SourceUrl": "https://amazon.com/product-xyz",
  "ScrapedProductName": "Sample Product Name"
}
```

### Output: ScrapingResultEvent (always)
```csharp
{
  "MappingId": "uuid",
  "WasSuccessful": true,
  "Timestamp": "2025-06-13T10:30:00Z",
  "ErrorMessage": null,
  "ErrorCode": null,
  "HttpStatusCode": 200
}
```

## Error Codes

| Code | Description |
|------|-------------|
| `HTTP_ERROR` | General HTTP errors (4xx, 5xx responses) |
| `PARSING_ERROR` | Failed to parse HTML or extract data |
| `TIMEOUT_ERROR` | Request timeout |
| `BLOCKED_BY_CAPTCHA` | CAPTCHA or bot detection (403, 401) |
| `RATE_LIMITED` | Too many requests (429) |
| `NETWORK_ERROR` | Network connectivity issues |
| `INVALID_SELECTOR` | CSS selector is invalid |
| `PRICE_NOT_FOUND` | Could not extract price from page |
| `UNKNOWN_ERROR` | Unexpected errors |

## Anti-Detection Features

### HTTP Headers Management
- Realistic browser headers (Accept, Accept-Language, Accept-Encoding)
- User-Agent rotation from orchestration service
- Custom headers per scraping profile
- Connection keep-alive management

### Request Patterns
- Randomized delays between requests (1-3 seconds)
- Configurable retry logic with backoff
- Cookie jar management per domain
- Session persistence across requests

### Limitations
- **No IP Rotation**: The service doesn't rotate IP addresses (would require proxy services)
- **No JavaScript Support**: Uses static HTML parsing (no browser automation)
- **Basic Evasion**: Limited to HTTP-level techniques

## Dependencies

- **.NET 9.0**: Latest .NET runtime
- **AngleSharp**: HTML parsing and CSS selector support
- **RabbitMQ.Client**: Message broker integration
- **TechTicker.Shared**: Common utilities and patterns
- **Microsoft.Extensions.Http**: HTTP client factory and configuration

## Deployment

### Docker
The service is designed to run in Docker containers with:
- Horizontal scaling support (multiple instances)
- Health check endpoints
- Configuration via environment variables
- Graceful shutdown handling

### Integration
- **Queue-based Processing**: Consumes from RabbitMQ queues
- **Event-driven Architecture**: Publishes events for downstream processing
- **Service Discovery**: Integrates with .NET Aspire service discovery
- **Observability**: Structured logging and telemetry support

## Monitoring & Observability

### Logging
- Structured logging with Serilog
- Request/response tracing
- Error categorization and context
- Performance metrics

### Health Checks
- RabbitMQ connectivity
- HTTP client configuration
- Service startup validation

### Metrics (Future)
- Scraping success rates per domain
- Average response times
- Error distribution by type
- Queue processing metrics

## Development

### Running Locally
1. Ensure RabbitMQ is running
2. Configure connection strings in `appsettings.Development.json`
3. Run with `dotnet run` or through the Aspire host

### Testing
- Unit tests for HTML parsing logic
- Integration tests for message processing
- Mock HTTP responses for scraping tests
- Error scenario validation

## Security Considerations

### Rate Limiting
- Respect robots.txt (future enhancement)
- Implement domain-specific rate limits
- Monitor for anti-bot measures

### Data Privacy
- No personal data storage
- Temporary HTML content only
- Secure configuration management

### Compliance
- Follow website terms of service
- Implement respectful scraping practices
- Monitor for blocking and adjust behavior
