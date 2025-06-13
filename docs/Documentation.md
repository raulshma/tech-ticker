Okay, I've combined the `Product-Seller Mapping Service` and `Scraper Site Configuration Service` into a new service called `Product Mapping & Site Configuration Service`. I've updated the relevant sections, re-numbered the microservices, and ensured consistency throughout the document.

Here is the updated Software Design Document:

## TechTicker: E-commerce Price Tracker & Alerter - Software Design Document

**Version:** 1.3
**Date:** June 9, 2025
**System Model:** Admin-Managed Exact Product URLs (Focus on Reduced Detectability, with Product Categories)

**Table of Contents:**

1.  Introduction
2.  System Architecture
3.  Detailed Microservice Descriptions
    *   3.1 Product Catalog Service
    *   3.2 Product Mapping & Site Configuration Service
    *   3.3 Scraping Orchestration Service
    *   3.4 Scraper Service
    *   3.5 Price Normalization & Ingestion Service
    *   3.6 Price History Service
    *   3.7 User Service
    *   3.8 Alert Definition Service
    *   3.9 Alert Evaluation Service
    *   3.10 Notification Service
    *   3.11 API Gateway / Frontend API Service
4.  Messaging Backbone
5.  Core Data Models
6.  Cross-Cutting Concerns
    *   6.1 Authentication & Authorization
    *   6.2 Logging
    *   6.3 Configuration
    *   6.4 Monitoring & Health Checks
    *   6.5 Error Handling Strategy
    *   6.6 Scraper Evasion Techniques (Within Constraints)
7.  Deployment Considerations
8.  Development Roadmap
9.  Non-Functional Requirements
10. Future Enhancements

---

### 1. Introduction

**1.1 Purpose**
The TechTicker system is designed to track prices of specified computer hardware and related products across multiple e-commerce websites. It aims to provide users with historical price data and alert them to price changes based on user-defined rules. Products are organized into categories for better management and user experience. This document outlines the microservices-based architecture for the system where administrators explicitly define products (with categories) and their corresponding URLs on seller sites, along with scraper configurations, with a focus on implementing scraping techniques to reduce detectability within given constraints.

**1.2 Scope**
*   Admin management of a canonical product catalog, including product categories.
*   Admin management of exact product URLs on e-commerce sites, mapped to canonical products.
*   Admin management of scraper configurations (CSS/XPath selectors) per e-commerce site domain, and linking them to product mappings.
*   Automated, periodic scraping of product prices and stock availability from these exact URLs, employing techniques to minimize detection.
*   Storage and retrieval of historical price data.
*   User registration and authentication.
*   User-defined alerts for price changes.
*   Email notifications for triggered alerts.
*   API for a frontend client application, supporting product categorization.

**1.3 Goals**
*   **Accuracy:** Provide accurate and timely price information.
*   **Organization:** Effectively categorize products for ease of browsing and management.
*   **Stealth:** Implement scraping methods that aim to reduce the likelihood of detection by target websites, within the constraint of not using external paid services.
*   **Reliability:** Ensure consistent operation of scraping and alerting mechanisms.
*   **Scalability:** Design services to handle a growing number of products, users, and tracked sites.
*   **Maintainability:** Enable easier updates and maintenance of individual components, especially scraper logic.
*   **Usability:** Provide a clear API for frontend development and straightforward admin interfaces (to be developed separately).

---

### 2. System Architecture

**2.1 High-Level Overview**
TechTicker employs a microservices architecture. Each service has a distinct responsibility and communicates with others primarily through a message broker (e.g., RabbitMQ) for asynchronous operations and via REST APIs for synchronous requests. This promotes loose coupling, independent scalability, and resilience.

**(Conceptual Diagram Description)**
*Imagine a diagram with user/admin interfaces at the top, an API Gateway as the entry point. Below the gateway, various microservices are depicted: Product Catalog, User Service, Alert Definition Service (often synchronous with Gateway), Product Mapping & Site Configuration Service. Further backend services like Scraping Orchestration, Scraper Service, Price History, etc., are shown interacting via a central Message Broker. Arrows indicate REST communication (solid lines) and message flows (dashed lines).*

**2.2 Technology Stack (Proposed)**
*   **Backend Services:** C# with ASP.NET Core (for APIs), .NET Worker Services (for background tasks).
*   **Database:** PostgreSQL (for relational data like catalogs, users, alerts), potentially a time-series database like InfluxDB or TimescaleDB for price history.
*   **Message Broker:** RabbitMQ (or Azure Service Bus, Apache Kafka).
*   **Web Scraping Library:** AngleSharp or HtmlAgilityPack.
*   **API Gateway:** Ocelot or YARP.
*   **Authentication:** JWT (JSON Web Tokens).
*   **Containerization:** Docker.
*   **Logging:** Serilog or NLog, potentially with a centralized logging platform (ELK Stack, Seq).

---

### 3. Detailed Microservice Descriptions

Each service description will cover its purpose, key features, API endpoints (example), data storage (example schema), inter-service communication, and specific considerations.

**3.1 Product Catalog Service**
*   **Purpose:** Manages the canonical, admin-defined list of products and their categories.
*   **Key Features:**
    *   **Category Management:** CRUD operations for product categories (e.g., "GPU", "CPU", "Motherboard", "SSD", "RAM").
    *   **Product Management:** CRUD operations for products, with each product assigned to one primary category (simplification for V1.2, can be expanded to many-to-many).
    *   Detailed product attributes (name, manufacturer, model, SKU, specs).
    *   Search/filter products, including filtering by category.
*   **API Endpoints (Example):**
    *   **Categories:**
        *   `POST /api/categories` (Admin): Create new category.
            *   Request Body: `{ "name": "Graphics Processing Unit", "slug": "gpu", "description": "Dedicated graphics cards" }`
            *   Response: `201 Created` with category details.
        *   `GET /api/categories`: List categories.
        *   `GET /api/categories/{categoryIdOrSlug}`: Get category by ID or slug.
        *   `PUT /api/categories/{categoryId}` (Admin): Update category.
        *   `DELETE /api/categories/{categoryId}` (Admin): Delete category.
    *   **Products:**
        *   `POST /api/products` (Admin): Create new product.
            *   Request Body: `{ "name": "Nvidia RTX 4070 Ti FE", "manufacturer": "Nvidia", "modelNumber": "PG141-SKU331", "categoryId": "uuid-of-gpu-category", "specifications": { "memory": "12GB GDDR6X" } }`
            *   Response: `201 Created` with product details.
        *   `GET /api/products?categoryId={idOrSlug}&search={term}`: List products, optionally filtered by category and search term.
        *   `GET /api/products/{productId}`: Get product by ID (response includes category info).
        *   `PUT /api/products/{productId}` (Admin): Update product.
        *   `DELETE /api/products/{productId}` (Admin): Delete product.
*   **Data Storage (PostgreSQL):**
    *   `Categories`
        *   `CategoryId` (UUID, PK)
        *   `Name` (VARCHAR(100), NOT NULL, UNIQUE)
        *   `Slug` (VARCHAR(100), NOT NULL, UNIQUE, Index)
        *   `Description` (TEXT, NULLABLE)
        *   `CreatedAt` (TIMESTAMPTZ, NOT NULL)
        *   `UpdatedAt` (TIMESTAMPTZ, NOT NULL)
    *   `Products`
        *   `ProductId` (UUID, PK)
        *   `Name` (VARCHAR(255), NOT NULL)
        *   `Manufacturer` (VARCHAR(100))
        *   `ModelNumber` (VARCHAR(100))
        *   `SKU` (VARCHAR(100), UNIQUE)
        *   `CategoryId` (UUID, FK to `Categories.CategoryId`, NOT NULL)
        *   `Description` (TEXT)
        *   `Specifications` (JSONB)
        *   `IsActive` (BOOLEAN, DEFAULT TRUE)
        *   `CreatedAt` (TIMESTAMPTZ, NOT NULL)
        *   `UpdatedAt` (TIMESTAMPTZ, NOT NULL)
*   **Inter-service Communication:**
    *   Consumed by: `Product Mapping & Site Configuration Service`, `Alert Definition Service`, `API Gateway`.
*   **Considerations:** Strategy for deleting categories (soft delete, prevent delete if products exist). Indexing for category filtering.

**3.2 Product Mapping & Site Configuration Service**
*   **Purpose:** Manages admin-defined mappings of canonical products to their exact URLs on e-commerce sites. Also manages admin-defined CSS/XPath selectors for extracting data from product pages on specific e-commerce site domains, and links these configurations to product mappings.
*   **Key Features:**
    *   **Mapping Management:**
        *   CRUD operations for product-seller mappings (`ProductSellerMappings`).
        *   Links a `CanonicalProductId` to a `SellerName` and an `ExactProductUrl`.
        *   Flag for active scraping (`IsActiveForScraping`).
        *   Optional scraping frequency override.
        *   Links to site-specific scraper configurations via `SiteConfigId`.
    *   **Site Configuration Management:**
        *   CRUD operations for site-specific scraper configurations (`ScraperSiteConfigurations`).
        *   Stores selectors for product name, price, stock, and optionally on-page seller name, scoped by `SiteDomain`.
*   **API Endpoints (Example):**
    *   **Mappings:**
        *   `POST /api/mappings` (Admin): Create a new mapping.
            *   Request Body: `{ "canonicalProductId": "uuid", "sellerName": "Newegg US", "exactProductUrl": "http://newegg.com/product-xyz", "isActiveForScraping": true, "siteConfigId": "uuid-of-newegg-config" }`
            *   Response: `201 Created` with mapping details.
        *   `GET /api/mappings?canonicalProductId={id}`: List mappings for a product.
        *   `GET /api/mappings/active`: List all active mappings (used by Orchestrator).
        *   `PUT /api/mappings/{mappingId}` (Admin): Update mapping.
        *   `DELETE /api/mappings/{mappingId}` (Admin): Delete mapping.
    *   **Site Configurations:**
        *   `POST /api/site-configs` (Admin): Create new site config.
            *   Request Body: `{ "siteDomain": "newegg.com", "productNameSelector": "h1.product-title", "priceSelector": ".price-current", "stockSelector": "#stock-status", "sellerNameOnPageSelector": ".seller-name" }`
            *   Response: `201 Created` with config details.
        *   `GET /api/site-configs?domain={domainName}`: Get config for a domain.
        *   `GET /api/site-configs/{siteConfigId}`: Get config by ID.
        *   `PUT /api/site-configs/{siteConfigId}` (Admin): Update site config.
        *   `DELETE /api/site-configs/{siteConfigId}` (Admin): Delete site config.
*   **Data Storage (PostgreSQL):**
    *   `ProductSellerMappings`
        *   `MappingId` (UUID, PK)
        *   `CanonicalProductId` (UUID, FK to `Products.ProductId`, NOT NULL)
        *   `SellerName` (VARCHAR(100), NOT NULL)
        *   `ExactProductUrl` (VARCHAR(2048), NOT NULL)
        *   `IsActiveForScraping` (BOOLEAN, DEFAULT TRUE)
        *   `ScrapingFrequencyOverride` (VARCHAR(50)) // e.g., "PT4H" for ISO 8601 duration
        *   `SiteConfigId` (UUID, FK to `ScraperSiteConfigurations.SiteConfigId`, NULLABLE)
        *   `LastScrapedAt` (TIMESTAMPTZ, NULLABLE)
        *   `NextScrapeAt` (TIMESTAMPTZ, NULLABLE, Index)
        *   `CreatedAt` (TIMESTAMPTZ, NOT NULL)
        *   `UpdatedAt` (TIMESTAMPTZ, NOT NULL)
    *   `ScraperSiteConfigurations`
        *   `SiteConfigId` (UUID, PK)
        *   `SiteDomain` (VARCHAR(255), UNIQUE, NOT NULL) // e.g., "newegg.com", "amazon.in"
        *   `ProductNameSelector` (TEXT, NOT NULL)
        *   `PriceSelector` (TEXT, NOT NULL)
        *   `StockSelector` (TEXT, NOT NULL)
        *   `SellerNameOnPageSelector` (TEXT, NULLABLE) // For marketplaces
        *   `CreatedAt` (TIMESTAMPTZ, NOT NULL)
        *   `UpdatedAt` (TIMESTAMPTZ, NOT NULL)
*   **Inter-service Communication:**
    *   Consumed by: `Scraping Orchestration Service`.
    *   Uses: `Product Catalog Service` (for validation/info when creating mappings).
*   **Considerations:**
    *   Index on `ProductSellerMappings.IsActiveForScraping` and `ProductSellerMappings.NextScrapeAt`.
    *   Strategy for deleting `ScraperSiteConfigurations` (e.g., prevent delete if referenced by mappings, or soft delete).
    *   The `Scraping Orchestration Service` may cache `ScraperSiteConfigurations` to reduce load.
    *   A `SiteConfigId` in `ProductSellerMappings` being NULL could imply that scraping for that mapping uses some default or is not possible without specific selectors.

**3.3 Scraping Orchestration Service**
*   **Purpose:** Schedules and initiates scraping tasks for mapped product URLs, incorporating strategies to manage scraping patterns.
*   **Key Features:**
    *   Periodically identifies product URLs due for scraping from the `Product Mapping & Site Configuration Service`.
    *   Retrieves associated `ScraperSiteConfiguration` selectors.
    *   **Dynamic Request Scheduling:** Implements variable and randomized delays between requests to the same domain to avoid easily identifiable, regular patterns. Delays can be configured per domain or globally.
    *   Publishes `ScrapeProductPageCommand` messages.
    *   Updates `LastScrapedAt` and `NextScrapeAt` based on results (via `Product Mapping & Site Configuration Service` or direct DB update if collocated/eventual consistency).
*   **API Endpoints:** Typically none (background worker). May expose health check.
*   **Data Storage:** May maintain a local cache or short-term state for scheduling.
    *   `DomainScrapingProfileCache`: `Domain (PK)`, `UserAgentList (JSONB)`, `HeaderProfiles (JSONB)`, `MinDelayMs`, `MaxDelayMs`.
*   **Inter-service Communication:**
    *   Consumes data from: `Product Mapping & Site Configuration Service`.
    *   Publishes to Message Broker: `ScrapeProductPageCommand`
    *   Subscribes via Message Broker: `ScrapingResultEvent`
*   **Message `ScrapeProductPageCommand` (Published):**
    *   `MappingId` (UUID)
    *   `CanonicalProductId` (UUID)
    *   `SellerName` (string)
    *   `ExactProductUrl` (string)
    *   `Selectors` (object: { `ProductNameSelector`, `PriceSelector`, `StockSelector`, `SellerNameOnPageSelector` })
    *   `ScrapingProfile` (object: { `UserAgent` (string to use for this request), `Headers` (dictionary of additional headers) })
*   **Technology:** .NET Worker Service.
*   **Considerations:** Complex scheduling logic to balance data freshness with inconspicuous request patterns. Management of request rates per domain. Updating mapping's `LastScrapedAt`/`NextScrapeAt` could be done by sending a command back to `Product Mapping & Site Configuration Service` or by this service if it has write access (less ideal for separation).

**3.4 Scraper Service**
*   **Purpose:** Fetches and parses product pages using provided URLs and selectors, employing techniques to appear more like a regular browser.
*   **Key Features:**
    *   Consumes `ScrapeProductPageCommand`.
    *   **Dynamic HTTP Client Configuration:** For each request:
        *   Uses the `UserAgent` provided in the `ScrapingProfile` of the command.
        *   Applies any additional `Headers` from the `ScrapingProfile`.
        *   Manages cookies received from the site and sends them back on subsequent requests to the same domain within a session (if configured).
    *   Performs HTTP GET requests.
    *   Uses AngleSharp/HtmlAgilityPack for parsing.
    *   Extracts price, stock, name.
    *   Publishes results or errors.
*   **API Endpoints:** None (background worker). Health check.
*   **Data Storage:** None persistent.
*   **Inter-service Communication:**
    *   Subscribes via Message Broker: `ScrapeProductPageCommand`
    *   Publishes to Message Broker: `RawPriceDataEvent`, `ScrapingResultEvent`
*   **Message `RawPriceDataEvent` (Published on success):**
    *   `CanonicalProductId` (UUID)
    *   `SellerName` (string)
    *   `ScrapedPrice` (decimal)
    *   `ScrapedStockStatus` (string, e.g., "In Stock", "Out of Stock")
    *   `Timestamp` (DateTimeOffset UTC)
    *   `SourceUrl` (string, the `ExactProductURL` scraped)
    *   `ScrapedProductName` (string, for verification)
*   **Message `ScrapingResultEvent` (Published always):**
    *   `MappingId` (UUID)
    *   `WasSuccessful` (boolean)
    *   `Timestamp` (DateTimeOffset UTC)
    *   `ErrorMessage` (string, if !WasSuccessful)
    *   `ErrorCode` (string, e.g., "BLOCKED_BY_CAPTCHA", "HTTP_ERROR", "PARSING_ERROR")
    *   `HttpStatusCode` (int, if applicable)
*   **Technology:** .NET Worker Service, AngleSharp/HtmlAgilityPack, `HttpClientFactory`.
*   **Considerations:**
    *   Header Management: Meticulous management of HTTP headers (see Section 6.6).
    *   Cookie Jar: Implementation of a cookie jar per domain/session.
    *   JavaScript Dependency: Identify and flag sites unscrapable by this method.
    *   Error Handling for Anti-Bot Measures: Detect and report specific error types.
    *   Limitations: No IP rotation is a primary detection vector.

**3.5 Price Normalization & Ingestion Service**
*   **Purpose:** Validates raw scraped data and prepares it for storage and further processing.
*   **Key Features:**
    *   Consumes `RawPriceDataEvent`.
    *   Validates data types (e.g., price is numeric).
    *   Normalizes stock status strings if needed (e.g., "Available" -> "IN_STOCK").
    *   Publishes `PricePointRecordedEvent`.
*   **API Endpoints:** None (background worker). Health check.
*   **Data Storage:** None persistent.
*   **Inter-service Communication:**
    *   Subscribes via Message Broker: `RawPriceDataEvent`
    *   Publishes to Message Broker: `PricePointRecordedEvent`
*   **Message `PricePointRecordedEvent` (Published):**
    *   `CanonicalProductId` (UUID)
    *   `SellerName` (string)
    *   `Price` (decimal)
    *   `StockStatus` (string, e.g., "IN_STOCK", "OUT_OF_STOCK", "UNKNOWN")
    *   `SourceUrl` (string)
    *   `Timestamp` (DateTimeOffset UTC)
*   **Technology:** .NET Worker Service.
*   **Considerations:** Logic for handling currency symbols/formats if prices are scraped from international sites. Defining a clear enum/set of values for `StockStatus`.

**3.6 Price History Service**
*   **Purpose:** Stores and provides access to historical price data.
*   **Key Features:**
    *   Consumes `PricePointRecordedEvent`.
    *   Stores price points with timestamps.
    *   Provides API to query price history.
*   **API Endpoints (Example):**
    *   `GET /api/products/{canonicalProductId}/price-history?sellerName={sellerName}&startDate={date}&endDate={date}`
        *   Response: `200 OK` with `[ { "timestamp": "...", "price": 123.45, "stockStatus": "IN_STOCK" }, ... ]`
*   **Data Storage (Time-series DB like InfluxDB or PostgreSQL):**
    *   `PriceHistory`
        *   `Timestamp` (TIMESTAMPTZ, NOT NULL, Part of composite PK or time-series index)
        *   `CanonicalProductId` (UUID, NOT NULL, Part of composite PK or tag)
        *   `SellerName` (VARCHAR(100), NOT NULL, Part of composite PK or tag)
        *   `Price` (DECIMAL(10, 2), NOT NULL)
        *   `StockStatus` (VARCHAR(50), NOT NULL)
        *   `SourceUrl` (VARCHAR(2048))
*   **Inter-service Communication:**
    *   Subscribes via Message Broker: `PricePointRecordedEvent`
    *   Consumed by: `API Gateway`, `Alert Evaluation Service` (for % drop calculations).
*   **Technology:** .NET Worker Service (ingestion), ASP.NET Core Web API (querying).
*   **Considerations:** Efficient querying of time-series data. Data retention policies (archiving/deleting old data).

**3.7 User Service**
*   **Purpose:** Manages user accounts, registration, and authentication.
*   **Key Features:**
    *   User registration with email/password.
    *   User login, issuing JWT.
    *   Password hashing and management.
    *   (Optional) Profile management.
*   **API Endpoints (Example):**
    *   `POST /api/users/register`
        *   Request: `{ "email": "user@example.com", "password": "..." }`
        *   Response: `201 Created` or `400 Bad Request`
    *   `POST /api/users/login`
        *   Request: `{ "email": "user@example.com", "password": "..." }`
        *   Response: `200 OK` with `{ "token": "jwt_token_here", "userId": "uuid" }`
    *   `GET /api/users/me` (Authenticated): Get current user details.
*   **Data Storage (PostgreSQL):**
    *   `Users`
        *   `UserId` (UUID, PK)
        *   `Email` (VARCHAR(255), UNIQUE, NOT NULL)
        *   `PasswordHash` (TEXT, NOT NULL)
        *   `FirstName` (VARCHAR(100))
        *   `LastName` (VARCHAR(100))
        *   `CreatedAt` (TIMESTAMPTZ, NOT NULL)
        *   `UpdatedAt` (TIMESTAMPTZ, NOT NULL)
*   **Inter-service Communication:**
    *   Consumed by: `API Gateway` (for auth), `Alert Definition Service` (to link alerts to users), `Notification Service` (to get user contact info).
*   **Technology:** ASP.NET Core Web API. Use Identity framework or roll custom with secure password hashing (e.g., BCrypt).

**3.8 Alert Definition Service**
*   **Purpose:** Allows authenticated users to create and manage price alert rules. Product category information is available if alerts are to be filtered or defined at a category level in the future.
*   **Key Features:**
    *   CRUD operations for alert rules.
    *   Links rules to users and canonical products.
    *   Supports different condition types.
*   **API Endpoints (Example):**
    *   `POST /api/alerts` (Authenticated): Create new alert rule.
        *   Request: `{ "canonicalProductId": "uuid", "conditionType": "PRICE_BELOW", "thresholdValue": 99.99, "specificSellerName": null, "isActive": true }`
        *   Response: `201 Created` with rule details.
    *   `GET /api/alerts` (Authenticated): List user's alert rules.
    *   `GET /api/alerts/product/{canonicalProductId}` (Internal for Alert Evaluation): Get active rules for a product.
    *   `PUT /api/alerts/{alertRuleId}` (Authenticated): Update rule.
    *   `DELETE /api/alerts/{alertRuleId}` (Authenticated): Delete rule.
*   **Data Storage (PostgreSQL):**
    *   `AlertRules`
        *   `AlertRuleId` (UUID, PK)
        *   `UserId` (UUID, FK to `Users.UserId`, NOT NULL)
        *   `CanonicalProductId` (UUID, FK to `Products.ProductId`, NOT NULL)
        *   `ConditionType` (VARCHAR(50), NOT NULL) // e.g., "PRICE_BELOW", "PERCENT_DROP_FROM_LAST", "BACK_IN_STOCK"
        *   `ThresholdValue` (DECIMAL(10,2), NULLABLE)
        *   `PercentageValue` (DECIMAL(5,2), NULLABLE)
        *   `SpecificSellerName` (VARCHAR(100), NULLABLE)
        *   `IsActive` (BOOLEAN, DEFAULT TRUE)
        *   `LastNotifiedAt` (TIMESTAMPTZ, NULLABLE) // To prevent spamming
        *   `NotificationFrequencyMinutes` (INT, DEFAULT 1440) // e.g., only notify once per day for same rule trigger
        *   `CreatedAt` (TIMESTAMPTZ, NOT NULL)
        *   `UpdatedAt` (TIMESTAMPTZ, NOT NULL)
*   **Inter-service Communication:**
    *   Consumed by: `API Gateway`, `Alert Evaluation Service`.
    *   Uses: `User Service` (auth context), `Product Catalog Service` (product info).
*   **Technology:** ASP.NET Core Web API.

**3.9 Alert Evaluation Service**
*   **Purpose:** Evaluates new price points against user-defined alert rules.
*   **Key Features:**
    *   Consumes `PricePointRecordedEvent`.
    *   Fetches relevant alert rules.
    *   Performs rule logic.
    *   Publishes `AlertTriggeredEvent` if a rule condition is met.
*   **API Endpoints:** None (background worker). Health check.
*   **Data Storage:** None persistent.
*   **Inter-service Communication:**
    *   Subscribes via Message Broker: `PricePointRecordedEvent`
    *   Publishes to Message Broker: `AlertTriggeredEvent`
    *   Consumes data from: `Alert Definition Service` (to get rules), `Price History Service` (for % drop calculations).
*   **Message `AlertTriggeredEvent` (Published):**
    *   `AlertRuleId` (UUID)
    *   `UserId` (UUID)
    *   `CanonicalProductId` (UUID)
    *   `ProductName` (string)
    *   `ProductCategoryName` (string, optional)
    *   `SellerName` (string)
    *   `TriggeringPrice` (decimal)
    *   `TriggeringStockStatus` (string)
    *   `RuleDescription` (string, e.g., "Price below $100")
    *   `ProductPageUrl` (string)
    *   `Timestamp` (DateTimeOffset UTC)
*   **Technology:** .NET Worker Service.
*   **Considerations:** Complex rule evaluation logic. Efficient querying of alert rules. Ensuring `LastNotifiedAt` and `NotificationFrequencyMinutes` are respected to prevent spam.

**3.10 Notification Service**
*   **Purpose:** Sends notifications to users when their alerts are triggered. Category information can be included in notification messages for better context.
*   **Key Features:**
    *   Consumes `AlertTriggeredEvent`.
    *   Formats notification messages (including product name, category, price, seller, link).
    *   Sends notifications (initially email).
    *   Logs notification status.
    *   Updates `LastNotifiedAt` on the alert rule.
*   **API Endpoints:** None (background worker). Health check.
*   **Data Storage:** None persistent (logs externally).
*   **Inter-service Communication:**
    *   Subscribes via Message Broker: `AlertTriggeredEvent`
    *   Consumes data from: `User Service` (to get user email).
    *   Calls API of: `Alert Definition Service` (to update `LastNotifiedAt` on the `AlertRule`).
*   **Technology:** .NET Worker Service. MailKit for SMTP.
*   **Considerations:** Template engine for email formatting. Integration with other notification channels (SMS, Push) in future. Retry logic for sending notifications.

**3.11 API Gateway / Frontend API Service**
*   **Purpose:** Single entry point for all client requests, routing them to appropriate backend services. Will expose category management endpoints and product filtering by category.
*   **Key Features:**
    *   Request routing.
    *   Authentication (validates JWT, passes user context).
    *   Response aggregation (if needed).
    *   Rate limiting, SSL termination.
*   **API Endpoints (Examples including category):**
    *   `GET /api/v1/categories` -> `Product Catalog Service`
    *   `GET /api/v1/categories/{idOrSlug}/products` -> `Product Catalog Service`
    *   `GET /api/v1/products?categorySlug={slug}&search={term}` -> `Product Catalog Service`
    *   `POST /api/v1/mappings` -> `Product Mapping & Site Configuration Service`
    *   `POST /api/v1/site-configs` -> `Product Mapping & Site Configuration Service`
    *   `POST /api/v1/auth/login` -> `User Service`
    *   `GET /api/v1/users/me/alerts` -> `Alert Definition Service`
*   **Data Storage:** None.
*   **Inter-service Communication:** Synchronous HTTP/gRPC calls to all other API-exposed services.
*   **Technology:** ASP.NET Core Web API with Ocelot or YARP.
*   **Considerations:** Configuration management for routes. Performance overhead. Caching common responses.

---

### 4. Messaging Backbone

*   **Technology:** RabbitMQ (or Azure Service Bus, Kafka).
*   **Key Queues/Topics & Exchanges:**
    *   **`scraping_commands_exchange` (Direct/Topic Exchange)**
        *   Routing Key/Topic: `scrape.product.page`
        *   Message: `ScrapeProductPageCommand` (now includes `ScrapingProfile`)
        *   Published by: `Scraping Orchestration Service`
        *   Consumed by: `Scraper Service` (multiple instances can consume from a shared queue bound to this)
    *   **`scraping_results_exchange` (Direct/Topic Exchange)**
        *   Routing Key/Topic: `scrape.result.product.page`
        *   Message: `ScrapingResultEvent` (now includes `ErrorCode`)
        *   Published by: `Scraper Service`
        *   Consumed by: `Scraping Orchestration Service`
    *   **`price_data_exchange` (Topic Exchange)**
        *   Routing Key/Topic: `price.data.raw`
        *   Message: `RawPriceDataEvent`
        *   Published by: `Scraper Service`
        *   Consumed by: `Price Normalization & Ingestion Service`
        *   Routing Key/Topic: `price.data.recorded`
        *   Message: `PricePointRecordedEvent`
        *   Published by: `Price Normalization & Ingestion Service`
        *   Consumed by: `Price History Service`, `Alert Evaluation Service`
    *   **`alert_notifications_exchange` (Topic Exchange)**
        *   Routing Key/Topic: `alert.triggered`
        *   Message: `AlertTriggeredEvent`
        *   Published by: `Alert Evaluation Service`
        *   Consumed by: `Notification Service`
*   **Error Queues/Dead Letter Exchanges (DLX):** Configure for unprocessable messages or persistent errors for later inspection.
*   **Message Durability:** Important messages (commands, critical events) should be persisted.
*   **Idempotency:** Consumers should be designed to handle duplicate messages gracefully where possible.

---

### 5. Core Data Models

*   **`Category`**: `CategoryId` (PK, UUID), `Name` (VARCHAR, NOT NULL, UNIQUE), `Slug` (VARCHAR, NOT NULL, UNIQUE), `Description` (TEXT), `CreatedAt` (TIMESTAMPTZ), `UpdatedAt` (TIMESTAMPTZ).
*   **`Product`**: `ProductId` (PK, UUID), `Name` (VARCHAR, NOT NULL), `Manufacturer` (VARCHAR), `ModelNumber` (VARCHAR), `SKU` (VARCHAR, UNIQUE), `CategoryId` (FK to `Categories.CategoryId`, NOT NULL), `Description` (TEXT), `Specifications` (JSONB), `IsActive` (BOOLEAN), `CreatedAt` (TIMESTAMPTZ), `UpdatedAt` (TIMESTAMPTZ).
*   **`ProductSellerMapping`**: `MappingId` (PK, UUID), `CanonicalProductId` (FK to `Products.ProductId`), `SellerName` (VARCHAR), `ExactProductUrl` (VARCHAR), `IsActiveForScraping` (BOOLEAN), `ScrapingFrequencyOverride` (VARCHAR), `SiteConfigId` (FK to `ScraperSiteConfigurations.SiteConfigId`, NULLABLE), `LastScrapedAt` (TIMESTAMPTZ), `NextScrapeAt` (TIMESTAMPTZ), `CreatedAt` (TIMESTAMPTZ), `UpdatedAt` (TIMESTAMPTZ).
*   **`ScraperSiteConfiguration`**: `SiteConfigId` (PK, UUID), `SiteDomain` (VARCHAR, UNIQUE), `ProductNameSelector` (TEXT), `PriceSelector` (TEXT), `StockSelector` (TEXT), `SellerNameOnPageSelector` (TEXT), `CreatedAt` (TIMESTAMPTZ), `UpdatedAt` (TIMESTAMPTZ).
*   **`PriceHistory`**: `Timestamp` (TIMESTAMPTZ), `CanonicalProductId` (UUID), `SellerName` (VARCHAR), `Price` (DECIMAL), `StockStatus` (VARCHAR), `SourceUrl` (VARCHAR). (Composite PK or appropriate time-series indexing).
*   **`User`**: `UserId` (PK, UUID), `Email` (VARCHAR, UNIQUE), `PasswordHash` (TEXT), `FirstName` (VARCHAR), `LastName` (VARCHAR), `CreatedAt` (TIMESTAMPTZ), `UpdatedAt` (TIMESTAMPTZ).
*   **`AlertRule`**: `AlertRuleId` (PK, UUID), `UserId` (FK), `CanonicalProductId` (FK), `ConditionType` (VARCHAR), `ThresholdValue` (DECIMAL), `PercentageValue` (DECIMAL), `SpecificSellerName` (VARCHAR), `IsActive` (BOOLEAN), `LastNotifiedAt` (TIMESTAMPTZ), `NotificationFrequencyMinutes` (INT), `CreatedAt` (TIMESTAMPTZ), `UpdatedAt` (TIMESTAMPTZ).

Relationships are primarily through Foreign Keys as described in service data storage sections.

---

### 6. Cross-Cutting Concerns

**6.1 Authentication & Authorization:**
    *   JWT for securing APIs. `User Service` issues tokens. `API Gateway` validates them.
    *   Admin roles/permissions for protected admin APIs in `Product Catalog Service` and `Product Mapping & Site Configuration Service`.
**6.2 Logging:**
    *   Structured logging (e.g., Serilog) in all services.
    *   Include Correlation IDs to trace requests across services.
    *   Centralized log aggregation (ELK Stack, Seq, Grafana Loki).
    *   Detailed logging for scraping attempts: URL, headers used, response status, identified blocks.
**6.3 Configuration:**
    *   `appsettings.json` for each service.
    *   Environment variables for sensitive data.
    *   Centralized configuration for:
        *   Lists of User-Agent strings.
        *   Profiles of HTTP headers.
        *   Default and per-domain delay parameters for scraping.
**6.4 Monitoring & Health Checks:**
    *   Each service exposes a `/health` endpoint.
    *   Metrics (Prometheus, AppMetrics) for request rates, error rates, queue lengths, processing times.
    *   Specific metrics for scraping: success rate per site, types of errors encountered (e.g., CAPTCHA, block), average time to scrape.
    *   Distributed tracing (OpenTelemetry).
**6.5 Error Handling Strategy:**
    *   Consistent error response formats from APIs.
    *   Retry mechanisms for transient failures.
    *   Dead-letter queues for unrecoverable message processing errors.
    *   Specific error codes/types in `ScrapingResultEvent` to differentiate scraping issues (e.g., `PARSING_ERROR`, `HTTP_403_FORBIDDEN`, `CAPTCHA_DETECTED`).
**6.6 Scraper Evasion Techniques (Within Constraints)**
This section details techniques employed by the `Scraping Orchestration Service` and `Scraper Service` to reduce the likelihood of detection by e-commerce websites, acknowledging the limitations of not using external paid services (especially IP rotation).

*   **User-Agent Rotation:**
    *   The `Scraping Orchestration Service` will maintain a configurable list of common, up-to-date browser User-Agent strings.
    *   For each `ScrapeProductPageCommand`, it will select a User-Agent from this list (e.g., randomly or rotating sequentially) and include it in the `ScrapingProfile`.
    *   The `Scraper Service` will use this specific User-Agent for the HTTP request.
*   **HTTP Header Management:**
    *   Beyond `User-Agent`, other HTTP headers can make requests look more legitimate. The `Scraping Orchestration Service` can define profiles of common headers (e.g., `Accept`, `Accept-Language`, `Accept-Encoding`, `Referer`).
    *   **Referer Header:** For a product page scrape, the `Referer` could be set to a plausible preceding page, like a search results page or category page for that site (though this adds complexity if not actually navigating from such pages). If not easily determined, it might be omitted or set to the site's base URL.
    *   These headers will be part of the `ScrapingProfile` in `ScrapeProductPageCommand`.
*   **Request Timing and Delays:**
    *   The `Scraping Orchestration Service` will implement configurable, randomized delays between consecutive requests to the same domain. This avoids fixed-interval patterns that are easy to detect.
    *   The goal is to mimic more human-like browsing patterns rather than rapid-fire machine requests.
*   **Basic Cookie Handling:**
    *   The `Scraper Service` can be equipped with a basic cookie handler (`HttpClientHandler.UseCookies = true`). It will automatically receive and send cookies for a given domain during a "session".
*   **Connection Keep-Alive:**
    *   Utilize HTTP Keep-Alive where appropriate.
*   **Order of Header Fields:**
    *   Being aware of common browser patterns for header order can be a minor factor.
*   **TLS/SSL Fingerprinting:**
    *   This is a known limitation without specialized tools.
*   **Behavioral Analysis Evasion (Limited):**
    *   Randomized timing is a crude attempt to mimic variable human interaction speed.
*   **Limitations (Crucial to Acknowledge):**
    *   **No IP Rotation:** All requests originate from the server's IP address(es). This is the single biggest factor for detection by any reasonably sophisticated anti-bot system.
    *   **JavaScript Challenges:** Sites heavily reliant on client-side JavaScript to render prices or content will be difficult or impossible to scrape accurately with `HttpClient` alone.
    *   **Advanced Bot Detection:** Sophisticated anti-bot services are very hard to bypass with these techniques alone.
    *   **CAPTCHAs:** This system will not be able to solve CAPTCHAs. If a CAPTCHA is encountered, the scrape for that page will fail.

The aim of these techniques is to bypass *simple* IP-based rate limiters or naive bot detection scripts.

---

### 7. Deployment Considerations

*   **Containerization:** Dockerize each microservice.
*   **Orchestration:** Kubernetes (K8s) is recommended.
*   **CI/CD Pipeline:** Automated build, test, and deployment.
*   **Environment Configuration:** Separate configurations for development, staging, and production.
*   **IP Address Management:** If server IPs get blocked, acquiring new IPs is the primary recourse without paid proxies. This is not a sustainable long-term strategy against aggressive blocking.
*   **Resource Allocation:** Scraping, especially if done frequently or with many instances, can be network and CPU intensive.

---

### 8. Development Roadmap (Summary)

1.  **Phase 1: Foundation (Admin & Core Scraping):**
    *   Implement `Product Catalog Service` (including Category CRUD & assigning categories to products).
    *   Implement `Product Mapping & Site Configuration Service` (managing mappings and site-specific selectors).
    *   Implement `Scraping Orchestration` (with basic evasion config), `Scraper` (with basic evasion, for 1-2 sites), `Price Normalization`, `Price History`.
    *   Manual data verification.
2.  **Phase 2: User Alerts & Notifications:** Implement `User Service`, `Alert Definition`, `Alert Evaluation`, `Notification Service` (email). Test with Phase 1 data.
3.  **Phase 3: API Gateway & Basic UI:**
    *   Set up `API Gateway`.
    *   Develop minimal frontend for core user interactions (product view including categories, price history, alert management). Allow browsing/filtering by category.
4.  **Phase 4: Scaling & Refinement:** Scale scraper instances, add more site support, refine evasion techniques based on observed success/failure, improve error handling, build admin UIs.

---

### 9. Non-Functional Requirements

*   **Scalability:**
    *   Scraper services should be horizontally scalable.
    *   Messaging system should handle high throughput.
    *   Databases chosen should support expected load.
*   **Reliability/Availability:**
    *   Individual service failures should not cascade.
    *   Message queues provide resilience.
    *   Scraping reliability will be impacted by anti-bot measures from target sites.
*   **Maintainability:**
    *   Clear separation of concerns.
    *   Configurations for selectors and evasion techniques should be easily updatable.
*   **Security:**
    *   Secure admin interfaces and APIs.
    *   Protection against common web vulnerabilities.
    *   Secure storage of sensitive data.
*   **Performance:**
    *   Scraping frequency will be balanced against evasion tactics (delays).
    *   API response times for user-facing operations should be fast.
    *   Price history queries should be optimized.
*   **Stealthiness:** The system should strive to minimize its detectable footprint when interacting with target websites, within the defined constraints of not using paid services. Success is measured by the ability to retrieve data without triggering immediate blocks from less sophisticated anti-bot measures.

---

### 10. Future Enhancements

*   **Hierarchical Categories:** Support nested categories with drill-down capabilities.
*   **Category-Specific Alert Rules:** Allow users to set alerts for an entire category (e.g., "any GPU from Nvidia drops by 10%").
*   **Faceted Search:** Advanced filtering on the frontend based on categories and other product specifications.
*   **Search-Based Product Discovery:** Implement model where system searches for products on e-commerce sites.
*   **Advanced Alert Conditions:** More complex rules.
*   **Additional Notification Channels:** SMS, Push Notifications.
*   **Limited Browser Automation Integration:** For specific, high-value sites unscrapable via direct HTTP, consider adding an optional service using Puppeteer Sharp or Playwright (resource-intensive, used sparingly).
*   **Dynamic Evasion Strategy Adjustment:** Based on success/failure rates per site, attempt to dynamically adjust delays, User-Agent profiles, or header sets (advanced).
*   **Machine Learning for Selector Finding/Maintenance:** Explore ML to suggest updates to broken CSS/XPath selectors (very advanced).
*   **User-Contributed Product Mappings/Selector Updates:** Community features (with moderation).
*   **Affiliate Link Integration:** If applicable for monetization.
*   **Comparative Analysis Features:** "Best deal for product X across all sellers."
*   **Admin Dashboard:** A dedicated web interface for all admin functionalities.

---

This document provides a comprehensive blueprint for the TechTicker system (V1.3). Each section can be further expanded during detailed design and implementation phases.