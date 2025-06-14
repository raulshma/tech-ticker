# TechTicker Price History Service

## Overview

The **TechTicker Price History Service** stores and provides access to historical price data for products across multiple e-commerce sellers. This service acts as the time-series data store for the TechTicker system, providing APIs for querying price trends, statistics, and historical data.

## Architecture

This service follows the established TechTicker microservices patterns:

- **Controllers**: REST API endpoints for querying price history data
- **Services**: Core business logic for data operations and storage
- **Workers**: Background services that consume normalized price data messages
- **Data**: Entity Framework Core context and database models
- **Messages**: Event messages for inter-service communication
- **Models**: Data structures for API requests, responses, and database entities

## Key Features

### 📊 Price History Storage
- **Time-Series Storage**: Efficiently stores price points with timestamps
- **Product Mapping**: Links price data to canonical product IDs
- **Seller Tracking**: Maintains price data per seller for comparison
- **Stock Status History**: Tracks availability changes over time

### 🔍 Query APIs
- **Historical Data**: Retrieve price history with date range filtering
- **Pagination**: Efficient pagination for large datasets
- **Seller Filtering**: Filter data by specific sellers
- **Latest Prices**: Quick access to most recent price points

### 📈 Price Statistics
- **Min/Max/Average**: Calculate price statistics for products
- **Time Range Analysis**: Statistics over specific time periods
- **Multi-Seller Comparison**: Compare prices across different sellers
- **Trend Analysis**: Identify price patterns and changes

### 📨 Message Processing
- **RabbitMQ Integration**: Consumes `PricePointRecordedEvent` messages
- **Real-time Ingestion**: Immediate storage of normalized price data
- **Error Handling**: Robust error handling and message acknowledgment
- **Scalable Processing**: Supports high-volume price data ingestion

## API Endpoints

### Price History Queries
- `GET /api/pricehistory/products/{productId}` - Get price history for a product
  - Query parameters: `sellerName`, `startDate`, `endDate`, `pageNumber`, `pageSize`
- `GET /api/pricehistory/products/{productId}/statistics` - Get price statistics
  - Query parameters: `sellerName`
- `GET /api/pricehistory/products/{productId}/sellers/{sellerName}/latest` - Get latest price from seller
- `GET /api/pricehistory/products/{productId}/sellers` - Get all sellers for a product

### Health Monitoring
- `GET /health` - Service health check endpoint
- `GET /alive` - Service liveness endpoint

## Data Storage

### Database Schema (PostgreSQL)
```sql
price_history (
    id BIGINT PRIMARY KEY,
    timestamp TIMESTAMPTZ NOT NULL,
    canonical_product_id UUID NOT NULL,
    seller_name VARCHAR(100) NOT NULL,
    price DECIMAL(10,2) NOT NULL,
    stock_status VARCHAR(50) NOT NULL,
    source_url VARCHAR(2048),
    product_name VARCHAR(500),
    original_stock_status VARCHAR(200),
    created_at TIMESTAMPTZ DEFAULT CURRENT_TIMESTAMP
);
```

### Indexes for Performance
- `IX_PriceHistory_ProductId_Timestamp` - Product queries with time ordering
- `IX_PriceHistory_ProductId_Seller_Timestamp` - Product-seller specific queries
- `IX_PriceHistory_Timestamp` - Time-based queries
- `IX_PriceHistory_Seller_Timestamp` - Seller-specific queries

## Message Contracts

### Input: PricePointRecordedEvent (from Price Normalization Service)
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

## Configuration

### RabbitMQ Settings
```json
{
  "RabbitMQ": {
    "PricePointRecordedExchange": "price-point-recorded",
    "PriceHistoryQueue": "price-history-queue",
    "PricePointRecordedRoutingKey": "price.point.recorded"
  }
}
```

### Database Connection
```json
{
  "ConnectionStrings": {
    "pricehistorydb": "Host=localhost;Database=techtickerpricehistory;Username=techticker;Password=password"
  }
}
```

## Integration Points

### With Price Normalization Service
- **Consumes**: `PricePointRecordedEvent` messages with normalized price data
- **Storage**: Immediately stores validated price points in time-series database
- **Acknowledgment**: Proper message acknowledgment for reliable processing

### With Alert Evaluation Service
- **API Access**: Provides historical price data for percentage drop calculations
- **Statistics**: Delivers price statistics for alert rule evaluation
- **Real-time**: Latest price data for real-time alert processing

### With API Gateway / Frontend
- **REST APIs**: Complete price history query capabilities
- **Pagination**: Efficient data access for frontend applications
- **Filtering**: Flexible filtering options for user interfaces

### With Aspire Host
- **Service Discovery**: Integrated with .NET Aspire orchestration
- **Database Provisioning**: Automatic PostgreSQL database setup
- **Health Monitoring**: Comprehensive health checks and observability

## Performance Considerations

### Database Optimization
- **Partitioning**: Table partitioning by date for large datasets
- **Indexing**: Optimized indexes for common query patterns
- **Retention Policies**: Configurable data retention for historical data
- **Connection Pooling**: Efficient database connection management

### API Performance
- **Pagination**: Efficient pagination prevents large result sets
- **Caching**: Response caching for frequently accessed data
- **Async Operations**: Non-blocking database operations
- **Query Optimization**: Optimized Entity Framework queries

### Scalability
- **Horizontal Scaling**: Stateless design allows multiple instances
- **Database Scaling**: Read replicas for query performance
- **Message Processing**: Scalable message consumption
- **Resource Management**: Efficient memory and CPU usage

## Error Handling

### API Error Responses
- **400 Bad Request**: Invalid query parameters or request format
- **404 Not Found**: Product or price data not found
- **500 Internal Server Error**: Database or system errors

### Message Processing Errors
- **Transient Errors**: Automatic retry with exponential backoff
- **Permanent Errors**: Dead letter queue for manual investigation
- **Database Errors**: Connection resilience and retry logic
- **Validation Errors**: Comprehensive error logging and reporting

## Monitoring & Observability

### Structured Logging
- **Query Performance**: Database query execution times
- **Message Processing**: Price point ingestion rates and success
- **API Usage**: Request patterns and response times
- **Error Tracking**: Detailed error logging with correlation IDs

### Metrics
- **Storage Metrics**: Price points stored per hour/day
- **Query Metrics**: API response times and success rates
- **Database Metrics**: Connection pool usage, query performance
- **Business Metrics**: Price data coverage per product/seller

### Health Checks
- **Database Connectivity**: PostgreSQL connection health
- **Message Broker**: RabbitMQ connectivity and queue health
- **Service Health**: Overall service status and dependencies

## Development

### Running Locally
```bash
# Start PostgreSQL and RabbitMQ (via Docker or local installation)
# Update connection strings in appsettings.Development.json

# Run the service
dotnet run
```

### Database Migrations
```bash
# Add new migration
dotnet ef migrations add MigrationName

# Update database
dotnet ef database update
```

### Testing
```bash
# Run unit tests
dotnet test

# Test message processing (publish test messages to RabbitMQ)
# Test API endpoints via Swagger UI
```

## Future Enhancements

### Advanced Analytics
- **Price Trend Analysis**: Machine learning for price prediction
- **Anomaly Detection**: Identify unusual price movements
- **Seasonal Patterns**: Detect and report seasonal pricing trends
- **Competitive Analysis**: Advanced seller comparison features

### Performance Optimizations
- **Time-Series Database**: Migration to specialized time-series database (InfluxDB, TimescaleDB)
- **Data Compression**: Optimize storage for large historical datasets
- **Caching Layer**: Redis caching for frequently accessed data
- **Query Optimization**: Advanced query optimization and materialized views

### API Enhancements
- **GraphQL Support**: Flexible data querying with GraphQL
- **Real-time Updates**: WebSocket support for real-time price updates
- **Export Capabilities**: CSV/Excel export for historical data
- **Advanced Filtering**: More sophisticated filtering and search options

## Dependencies

- **.NET 9.0**: Latest runtime and framework features
- **Entity Framework Core**: Database ORM and migrations
- **PostgreSQL**: Primary database for time-series data
- **RabbitMQ.Client**: Message broker integration
- **TechTicker.Shared**: Common utilities and Result pattern
- **TechTicker.ServiceDefaults**: Aspire service defaults and configuration
