# TechTicker Scraping Orchestration Service - Implementation Summary

## Overview

Successfully implemented the **Scraping Orchestration Service** as specified in the TechTicker architecture documentation. This service acts as the central coordinator for web scraping activities, managing request scheduling, domain-specific scraping profiles, and communication with scraper services.

## 🎯 Key Features Implemented

### 1. **Dynamic Request Scheduling**
- ✅ Variable and randomized delays between requests to the same domain
- ✅ Configurable min/max delay settings per domain (2-8 seconds default)
- ✅ Prevents easily identifiable, regular request patterns
- ✅ Domain-based rate limiting with `NextAllowedRequestAt` tracking

### 2. **Domain-Specific Scraping Profiles**
- ✅ Automatic creation of default profiles for new domains
- ✅ User-Agent rotation with realistic browser strings
- ✅ Multiple HTTP header profiles (Chrome, Firefox, Safari)
- ✅ JSON-based storage for flexible profile management

### 3. **Intelligent Scheduling System**
- ✅ Identifies product URLs due for scraping based on frequency settings
- ✅ Supports per-mapping frequency overrides (ISO 8601 format)
- ✅ Default 4-hour scraping frequency with customizable options
- ✅ Automatic `NextScrapeAt` calculation after successful attempts

### 4. **Message-Based Architecture**
- ✅ Publishes `ScrapeProductPageCommand` messages to RabbitMQ
- ✅ Consumes `ScrapingResultEvent` messages for scheduling updates
- ✅ Retry logic based on error types (CAPTCHA, rate limiting, etc.)
- ✅ Loose coupling with scraper services

### 5. **Anti-Detection Strategies**
- ✅ User-Agent rotation across realistic browser strings
- ✅ HTTP header variation mimicking different browsers
- ✅ Randomized request timing to avoid patterns
- ✅ Domain-specific request rate management

## 📁 Project Structure

```
services/TechTicker.ScrapingOrchestrationService/
├── Data/
│   └── ScrapingOrchestrationDbContext.cs
├── Messages/
│   ├── ScrapeProductPageCommand.cs
│   └── ScrapingResultEvent.cs
├── Models/
│   └── DomainScrapingProfile.cs
├── Properties/
│   └── launchSettings.json
├── Services/
│   ├── DomainScrapingProfileService.cs
│   ├── MessagePublisherService.cs
│   └── ScrapingSchedulerService.cs
├── Workers/
│   ├── ScrapingOrchestrationWorker.cs
│   └── ScrapingResultConsumerWorker.cs
├── appsettings.json
├── appsettings.Development.json
├── Program.cs
├── README.md
├── TechTicker.ScrapingOrchestrationService.csproj
└── TechTicker.ScrapingOrchestrationService.http
```

## 🗃️ Database Schema

### DomainScrapingProfiles Table
```sql
CREATE TABLE DomainScrapingProfiles (
    Domain VARCHAR(255) PRIMARY KEY,
    UserAgentList JSONB NOT NULL,
    HeaderProfiles JSONB NOT NULL,
    MinDelayMs INTEGER NOT NULL DEFAULT 2000,
    MaxDelayMs INTEGER NOT NULL DEFAULT 8000,
    LastRequestAt TIMESTAMPTZ,
    NextAllowedRequestAt TIMESTAMPTZ,
    CreatedAt TIMESTAMPTZ NOT NULL,
    UpdatedAt TIMESTAMPTZ NOT NULL
);

CREATE INDEX idx_domain_next_allowed ON DomainScrapingProfiles(NextAllowedRequestAt);
```

## 📨 Message Contracts

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

## ⚙️ Configuration

### Application Settings
```json
{
  "ScrapingOrchestration": {
    "IntervalMinutes": 5,           // Check interval
    "MaxMappingsPerCycle": 50,      // Max mappings per cycle
    "MaxConcurrentDomains": 10      // Max domains processed concurrently
  }
}
```

### Supported Frequency Overrides
- `PT1H` - Every hour
- `PT4H` - Every 4 hours (default) 
- `PT12H` - Every 12 hours
- `P1D` - Daily
- `P7D` - Weekly

## 🔧 Integration Points

### Dependencies
- **Database**: Shares `product-seller-mapping` database for read access
- **Message Broker**: RabbitMQ for command publishing and result consumption
- **Aspire**: Integrated with service discovery and configuration management

### Aspire Configuration
```csharp
builder.AddProject<Projects.TechTicker_ScrapingOrchestrationService>("techticker-scrapingorchestrationservice")
    .WithReference(productSellerMappingDb)
    .WithReference(rabbitmq)
    .WaitFor(postgres)
    .WaitFor(rabbitmq);
```

## 🚀 Deployment Ready

- ✅ **Solution Integration**: Added to TechTicker.sln
- ✅ **Aspire Integration**: Configured in TechTicker.Host
- ✅ **Build Success**: Compiles without errors
- ✅ **Documentation**: Comprehensive README and inline docs
- ✅ **Testing Support**: HTTP file with sample requests

## 🔄 Operational Flow

1. **Periodic Check**: Worker wakes up every 5 minutes (configurable)
2. **Mapping Discovery**: Queries database for mappings due for scraping
3. **Domain Grouping**: Groups mappings by domain for rate limiting
4. **Profile Selection**: Selects appropriate scraping profile for each domain
5. **Command Publishing**: Publishes scraping commands to message queue
6. **Schedule Update**: Updates `NextScrapeAt` times for processed mappings
7. **Result Processing**: Handles scraping results for retry scheduling

## 🎛️ Error Handling & Retry Logic

| Error Type | Retry Delay | Description |
|------------|-------------|-------------|
| `BLOCKED_BY_CAPTCHA` | 2 hours | Wait longer for CAPTCHA blocks |
| `HTTP_ERROR` (429) | 1 hour | Rate limited by server |
| `HTTP_ERROR` (5xx) | 30 minutes | Server errors |
| `PARSING_ERROR` | 15 minutes | Quick retry for parsing issues |
| Default | 30 minutes | Standard retry delay |

## 📊 Monitoring & Logging

- **Information Level**: Cycle summaries, mapping counts, domain processing
- **Debug Level**: Individual mapping processing, rate limiting decisions
- **Warning Level**: Configuration issues, missing site configs
- **Error Level**: Database errors, messaging failures, unexpected exceptions

## ✅ Implementation Status

All requirements from the Software Design Document have been successfully implemented:

- [x] Periodically identifies product URLs due for scraping
- [x] Retrieves associated ScraperSiteConfiguration selectors  
- [x] Implements dynamic request scheduling with variable delays
- [x] Publishes ScrapeProductPageCommand messages
- [x] Updates LastScrapedAt and NextScrapeAt based on results
- [x] Manages domain-specific scraping profiles
- [x] Handles scraping result events for retry logic
- [x] Follows established project patterns and architecture