# TechTicker Scraping Orchestration Service

## Overview

The Scraping Orchestration Service is responsible for scheduling and initiating scraping tasks for mapped product URLs. It implements strategies to manage scraping patterns and reduce detectability while maintaining efficient data collection.

## Key Features

### Dynamic Request Scheduling
- Implements variable and randomized delays between requests to the same domain
- Configurable min/max delay settings per domain
- Prevents easily identifiable, regular request patterns

### Domain-Specific Scraping Profiles
- Manages User-Agent rotation for each domain
- Maintains multiple HTTP header profiles to simulate different browsers
- Tracks last request times and enforces delays

### Intelligent Scheduling
- Identifies product URLs due for scraping based on frequency settings
- Supports per-mapping frequency overrides
- Groups requests by domain to manage rate limits effectively

### Message-Based Architecture
- Publishes `ScrapeProductPageCommand` messages to RabbitMQ
- Processes `ScrapingResultEvent` messages for scheduling updates
- Loose coupling with scraper services

## Configuration

### Application Settings

```json
{
  "ScrapingOrchestration": {
    "IntervalMinutes": 5,           // How often to check for mappings due for scraping
    "MaxMappingsPerCycle": 50,      // Maximum mappings to process per cycle
    "MaxConcurrentDomains": 10      // Maximum domains to process concurrently
  }
}
```

### Scraping Frequencies

The service supports ISO 8601 duration strings for frequency overrides:
- `PT1H` - Every hour
- `PT4H` - Every 4 hours (default)
- `PT12H` - Every 12 hours
- `P1D` - Daily
- `P7D` - Weekly

## Domain Scraping Profiles

Each domain gets a default profile with:
- Multiple realistic User-Agent strings
- Browser-specific header profiles (Chrome, Firefox, Safari)
- Randomized delays (2-8 seconds by default)
- Request timing tracking

## Database Schema

### DomainScrapingProfiles Table
```sql
Domain (PK)                    VARCHAR(255)
UserAgentList                  JSONB
HeaderProfiles                 JSONB
MinDelayMs                     INT
MaxDelayMs                     INT
LastRequestAt                  TIMESTAMPTZ
NextAllowedRequestAt           TIMESTAMPTZ
CreatedAt                      TIMESTAMPTZ
UpdatedAt                      TIMESTAMPTZ
```

## Message Contracts

### ScrapeProductPageCommand (Published)
```json
{
  "MappingId": "uuid",
  "CanonicalProductId": "uuid",
  "SellerName": "string",
  "ExactProductUrl": "string",
  "Selectors": {
    "ProductNameSelector": "string",
    "PriceSelector": "string",
    "StockSelector": "string",
    "SellerNameOnPageSelector": "string?"
  },
  "ScrapingProfile": {
    "UserAgent": "string",
    "Headers": { "key": "value" }
  },
  "ScheduledAt": "datetime"
}
```

### ScrapingResultEvent (Consumed)
```json
{
  "MappingId": "uuid",
  "WasSuccessful": "boolean",
  "Timestamp": "datetime",
  "ErrorMessage": "string?",
  "ErrorCode": "string?",
  "HttpStatusCode": "int?"
}
```

## Dependencies

- **Entity Framework Core** - Database access
- **RabbitMQ.Client** - Message publishing
- **TechTicker.Shared** - Common models and utilities
- **TechTicker.ServiceDefaults** - Aspire service defaults

## Anti-Detection Strategies

1. **User-Agent Rotation**: Cycles through realistic browser User-Agent strings
2. **Header Variation**: Uses different HTTP header profiles mimicking various browsers
3. **Request Timing**: Implements randomized delays between requests to same domain
4. **Rate Limiting**: Respects per-domain request timing to avoid overwhelming servers

## Monitoring

The service provides detailed logging at various levels:
- **Information**: Cycle summaries, mapping counts
- **Debug**: Individual mapping processing, domain rate limiting
- **Trace**: Detailed request scheduling and profile selection

## Deployment

The service runs as a .NET Worker Service and can be deployed as:
- Windows Service
- Linux systemd service
- Docker container
- Part of Aspire application

## Future Enhancements

- Support for proxy rotation (when infrastructure allows)
- Machine learning-based delay optimization
- Advanced error handling and retry logic
- Metrics collection for scraping success rates
- Dynamic profile adjustment based on success rates
