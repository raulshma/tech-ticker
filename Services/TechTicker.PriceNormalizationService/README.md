# TechTicker Price Normalization & Ingestion Service

## Overview

The **TechTicker Price Normalization & Ingestion Service** is responsible for validating and normalizing raw price data scraped from e-commerce websites. This service acts as an intermediary between the Scraper Service and downstream services like Price History and Alert Evaluation.

## Architecture

This service follows the Worker Service pattern and includes:

- **Workers**: Background services that consume raw price data messages and coordinate normalization operations
- **Services**: Core business logic for price validation, stock status normalization, and message handling
- **Models**: Data structures for normalization configuration and validation results
- **Messages**: Event messages for inter-service communication

## Key Features

### 🔍 Price Validation & Normalization
- **Price Range Validation**: Configurable minimum and maximum price thresholds
- **Decimal Precision**: Standardizes prices to 2 decimal places
- **Currency Handling**: Validates price is positive and within reasonable bounds
- **Error Detection**: Identifies and reports invalid price data

### 📊 Stock Status Normalization
- **Status Mapping**: Converts various stock status texts to standardized values
- **Intelligent Recognition**: Recognizes common stock status patterns across different sites
- **Fallback Handling**: Provides default status for unrecognized text
- **Original Status Preservation**: Maintains original text for audit purposes

### 📨 Message Processing
- **RabbitMQ Integration**: Consumes `RawPriceDataEvent` messages from Scraper Service
- **Event Publishing**: Publishes `PricePointRecordedEvent` messages for downstream services
- **Error Handling**: Comprehensive error categorization and reporting
- **Message Acknowledgment**: Proper message acknowledgment and retry handling

### 🛡️ Data Quality & Validation
- **Required Field Validation**: Ensures all mandatory fields are present and valid
- **Strict/Lenient Modes**: Configurable validation strictness
- **Validation Reporting**: Detailed validation error messages
- **Data Integrity**: Maintains data consistency across the pipeline

## Normalized Stock Status Values

The service normalizes stock statuses to the following standard values:

- `IN_STOCK`: Product is available for purchase
- `OUT_OF_STOCK`: Product is not currently available
- `LIMITED_STOCK`: Product has limited availability
- `PRE_ORDER`: Product is available for pre-order
- `DISCONTINUED`: Product is no longer manufactured
- `UNKNOWN`: Status could not be determined

## Configuration

### Application Settings (`appsettings.json`)

```json
{
  "RabbitMQ": {
    "ConnectionString": "amqp://localhost:5672",
    "RawPriceDataExchange": "raw-price-data",
    "RawPriceDataQueue": "raw-price-data-queue",
    "RawPriceDataRoutingKey": "raw.price.data",
    "PricePointRecordedExchange": "price-point-recorded",
    "PricePointRecordedRoutingKey": "price.point.recorded"
  },
  "NormalizationSettings": {
    "MinPrice": 0.01,
    "MaxPrice": 999999.99,
    "StrictValidation": true,
    "DefaultStockStatus": "UNKNOWN"
  }
}
```

### Normalization Settings

- **MinPrice**: Minimum acceptable price value (default: 0.01)
- **MaxPrice**: Maximum acceptable price value (default: 999999.99)
- **StrictValidation**: Whether to reject invalid data or process with warnings (default: true)
- **DefaultStockStatus**: Default status for unrecognized stock text (default: "UNKNOWN")

## Message Contracts

### Input: RawPriceDataEvent (from Scraper Service)
```json
{
  "CanonicalProductId": "550e8400-e29b-41d4-a716-446655440000",
  "SellerName": "Amazon US",
  "ScrapedPrice": 299.99,
  "ScrapedStockStatus": "In Stock",
  "Timestamp": "2025-06-14T10:30:00Z",
  "SourceUrl": "https://amazon.com/product-xyz",
  "ScrapedProductName": "Sample Product Name"
}
```

### Output: PricePointRecordedEvent (to downstream services)
```json
{
  "CanonicalProductId": "550e8400-e29b-41d4-a716-446655440000",
  "SellerName": "Amazon US",
  "Price": 299.99,
  "StockStatus": "IN_STOCK",
  "SourceUrl": "https://amazon.com/product-xyz",
  "Timestamp": "2025-06-14T10:30:00Z",
  "ProductName": "Sample Product Name",
  "OriginalStockStatus": "In Stock"
}
```

## Error Handling

The service categorizes errors into specific types:

- **INVALID_PRICE**: Price is not a valid number or is negative/zero
- **PRICE_OUT_OF_RANGE**: Price falls outside configured min/max bounds
- **INVALID_STOCK_STATUS**: Stock status cannot be normalized
- **MISSING_REQUIRED_DATA**: Required fields are missing or empty
- **VALIDATION_ERROR**: General validation or processing error

## Integration Points

### With Scraper Service
- **Consumes**: `RawPriceDataEvent` messages with scraped product data
- **Validation**: Validates and normalizes scraped data
- **Quality Control**: Ensures data quality before forwarding

### With Price History Service
- **Publishes**: `PricePointRecordedEvent` with normalized price data
- **Format**: Standardized data ready for historical storage

### With Alert Evaluation Service
- **Publishes**: `PricePointRecordedEvent` for alert rule evaluation
- **Real-time**: Immediate forwarding of normalized price changes

### With Aspire Host
- **Service Discovery**: Integrated with .NET Aspire orchestration
- **Health Checks**: Automatic health monitoring
- **Configuration**: Environment-based configuration management

## Performance Considerations

- **Message Processing**: Single-threaded message processing for data consistency
- **Memory Usage**: Minimal memory footprint with no persistent storage
- **Throughput**: Designed for high-throughput price data processing
- **Error Recovery**: Robust error handling with message retry capabilities

## Monitoring & Observability

- **Structured Logging**: Comprehensive logging of normalization activities
- **Correlation IDs**: Request tracing across service boundaries
- **Metrics**: Processing rates, validation success/failure rates
- **Health Checks**: Service health and RabbitMQ connectivity monitoring

## Development

### Prerequisites
- .NET 9.0 SDK
- RabbitMQ server
- Access to TechTicker.Shared and TechTicker.ServiceDefaults projects

### Running the Service
```bash
cd Services/TechTicker.PriceNormalizationService
dotnet run
```

### Testing
The service can be tested by:
1. Publishing test `RawPriceDataEvent` messages to the input queue
2. Monitoring output queues for `PricePointRecordedEvent` messages
3. Checking service logs for normalization activity

## Future Enhancements

- **Currency Conversion**: Support for multiple currencies with conversion rates
- **Advanced Validation**: Machine learning-based price anomaly detection
- **Custom Rules**: Configurable validation rules per product category
- **Batch Processing**: Bulk processing capabilities for high-volume scenarios
- **Metrics Dashboard**: Real-time monitoring dashboard for normalization metrics

## Dependencies

- **RabbitMQ.Client**: Message broker communication
- **System.Text.Json**: JSON serialization
- **TechTicker.Shared**: Common utilities and patterns
- **TechTicker.ServiceDefaults**: Aspire service defaults

---

Built with ❤️ for the TechTicker ecosystem
