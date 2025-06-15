## TechTicker: E-commerce Price Tracker & Alerter - Detailed Software Specification

**Version:** 1.5
**Date:** June 10, 2025
**System Model:** Admin-Managed Exact Product URLs (Focus on Reduced Detectability, with Product Categories)
**Architecture Model:** Monolith with .NET Aspire
**Target Audience:** Development Team / Automated Coding Agent

**Preamble for Coding Agent:**
This document aims to provide a highly detailed specification for building the TechTicker application. While it strives for completeness, some low-level implementation details (e.g., specific UI element interactions if a UI is implied, or exhaustive edge-case error handling beyond what's specified) might still require standard best practices or further clarification. Focus on implementing the described logic, data structures, and interactions precisely. Use the recommended technologies and libraries. Pay close attention to data validation, error handling, and logging as outlined.

**Table of Contents:**

1.  Introduction
    *   1.1 Purpose
    *   1.2 Scope
    *   1.3 Goals
2.  System Architecture
    *   2.1 High-Level Overview
    *   2.2 Technology Stack (Proposed & Specified)
3.  Detailed Application Module Descriptions
    *   3.1 Product Catalog Module
    *   3.2 Product Mapping & Site Configuration Module
    *   3.3 Scraping Orchestration Module
    *   3.4 Scraper Module
    *   3.5 Price Normalization & Ingestion Module
    *   3.6 Price History Module
    *   3.7 User Authentication & Management Module
    *   3.8 Alert Definition Module
    *   3.9 Alert Evaluation Module
    *   3.10 Notification Module
    *   3.11 API Layer
4.  Messaging Backbone for Background Tasks
5.  Core Data Models (PostgreSQL with EF Core)
6.  Cross-Cutting Concerns
    *   6.1 Authentication & Authorization
    *   6.2 Logging
    *   6.3 Configuration
    *   6.4 Monitoring & Health Checks
    *   6.5 Error Handling Strategy
    *   6.6 Scraper Evasion Techniques (Within Constraints)
7.  Deployment Considerations
8.  Development Roadmap (Summary)
9.  Non-Functional Requirements
10. Future Enhancements

---

### 1. Introduction

**1.1 Purpose**
The TechTicker system is designed to track prices of specified computer hardware and related products across multiple e-commerce websites. It aims to provide users with historical price data and alert them to price changes based on user-defined rules. Products are organized into categories for better management and user experience. This document outlines the **monolith architecture, orchestrated and developed using .NET Aspire,** for the system where administrators explicitly define products (with categories) and their corresponding URLs on seller sites, along with scraper configurations, with a focus on implementing scraping techniques to reduce detectability within given constraints.

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
*   **Scalability:** Design the application to handle a growing number of products, users, and tracked sites, with options for scaling the entire monolith or specific background worker components.
*   **Maintainability:** Leverage .NET Aspire to organize the monolith into well-defined projects/modules, facilitating easier updates and maintenance of individual functionalities, especially scraper logic.
*   **Usability:** Provide a clear API for frontend development and straightforward admin interfaces (to be developed separately).

---

### 2. System Architecture

**2.1 High-Level Overview**
TechTicker employs a **monolithic architecture built with .NET Aspire**. The application consists of a primary ASP.NET Core web project handling API requests and user interactions, and several .NET Worker Service projects for background tasks like scraping and notifications. These projects are organized within a single .NET Aspire solution, allowing for unified configuration, service discovery (during development), and simplified orchestration.

While a monolith, logical separation of concerns is maintained by structuring the application into distinct modules or components, often corresponding to separate class libraries or well-defined namespaces within the main projects. Communication between these modules is primarily through direct method calls (dependency injection) for synchronous operations within the web application. For decoupling long-running or resource-intensive tasks such as scraping and notifications, a message broker (e.g., RabbitMQ) is utilized for asynchronous communication between the API layer and background worker components.

**.NET Aspire facilitates:**
*   **Development Orchestration:** The **AppHost** project defines and launches the various parts of the application (web API, worker services, databases, messaging queues) during development.
*   **Service Discovery & Configuration:** Simplifies connection string management and service-to-service communication setup in the development environment.
*   **Observability:** Integrates with OpenTelemetry for logging, metrics, and tracing, accessible via the Aspire Dashboard.
*   **Deployment:** Provides tools and patterns to simplify deployment to containerized environments.

**2.2 Technology Stack (Proposed & Specified)**
*   **Application Framework:** .NET 8+ with ASP.NET Core (for the main web application/API) and .NET Worker Services (for background tasks).
*   **Orchestration & Development:** **.NET Aspire**.
*   **Database:** PostgreSQL. Use **Npgsql** as the ADO.NET provider. Use **Entity Framework Core (EF Core)** as the ORM.
*   **Message Broker:** RabbitMQ. Use the **RabbitMQ.Client** .NET library.
*   **Web Scraping Library:** **AngleSharp** for HTML parsing. `HttpClientFactory` for making HTTP requests.
*   **Authentication:** JWT (JSON Web Tokens). Use **ASP.NET Core Identity** for user management and **`Microsoft.AspNetCore.Authentication.JwtBearer`** for JWT validation.
*   **Containerization:** Docker. .NET Aspire will assist in generating Dockerfiles.
*   **Logging:** **Serilog** integrated with .NET Aspire's OpenTelemetry support. Configure Serilog to write to console (for Aspire Dashboard) and potentially a file or a structured logging sink in production.
*   **Project Structure (Explicit):**
    *   `TechTicker.AppHost`: The Aspire orchestrator project. This project will define resources like PostgreSQL, RabbitMQ, and add references to the `ApiService` and worker projects.
    *   `TechTicker.ApiService`: ASP.NET Core Web API project. Contains API Controllers, business logic services, EF Core DbContext, and data repositories. Handles synchronous operations and publishes messages for asynchronous tasks. Reference `TechTicker.Domain`, `TechTicker.DataAccess`, `TechTicker.Application`.
    *   `TechTicker.ScrapingWorker`: .NET Worker Service project. Contains `ScrapingOrchestrationService` (as a hosted service) and `ScraperService` (message consumer). Reference `TechTicker.Domain`, `TechTicker.Application` (for interfaces/DTOs if needed), and message broker client.
    *   `TechTicker.NotificationWorker`: .NET Worker Service project. Contains `NotificationService` (message consumer). Reference `TechTicker.Domain`, `TechTicker.Application` (for interfaces/DTOs if needed), and message broker client.
    *   `TechTicker.Domain`: Class library for core domain entities (e.g., Product, User, AlertRule). These are POCOs.
    *   `TechTicker.DataAccess`: Class library for EF Core `DbContext`, migrations, and repository implementations. Reference `TechTicker.Domain`.
    *   `TechTicker.Application`: Class library for application service interfaces, DTOs (Data Transfer Objects used for API and messages), and core business logic interfaces. Reference `TechTicker.Domain`.
    *   `TechTicker.ServiceDefaults`: A shared project for common configurations (health checks, OpenTelemetry, Serilog setup), provided by Aspire.

---

### 3. Detailed Application Module Descriptions

**Implementation Note for Coding Agent:**
*   All API endpoints should perform input validation. Return `400 Bad Request` with a clear error message (e.g., using `ValidationProblemDetails`) for invalid input.
*   Use asynchronous programming (`async`/`await`) for all I/O-bound operations (database access, HTTP requests, message publishing/consuming).
*   Implement appropriate logging for all significant operations, errors, and decisions.
*   Ensure EF Core queries are efficient (e.g., avoid N+1 problems, use projections).

**3.1 Product Catalog Module**
*   **Purpose:** Manages products and categories.
*   **Implementation:** Services (`CategoryService`, `ProductService`) and repositories (`ICategoryRepository`, `IProductRepository`) within `TechTicker.ApiService`, using `TechTicker.DataAccess` and `TechTicker.Domain`.
*   **API Endpoints (exposed by `TechTicker.ApiService`):**

    *   **Categories:**
        *   `POST /api/categories` (Admin Only)
            *   Request Body:
                ```json
                {
                  "name": "Graphics Processing Unit",
                  "slug": "gpu",
                  "description": "Dedicated graphics cards for gaming and professional workloads"
                }
                ```
            *   Response (`201 Created`):
                ```json
                {
                  "categoryId": "uuid-generated-by-server",
                  "name": "Graphics Processing Unit",
                  "slug": "gpu",
                  "description": "Dedicated graphics cards for gaming and professional workloads",
                  "createdAt": "timestamp",
                  "updatedAt": "timestamp"
                }
                ```            *   Error Responses: `400 Bad Request` (validation errors, e.g., name missing, slug already exists), `401 Unauthorized`, `403 Forbidden`.
        *   `GET /api/categories`
            *   Response (`200 OK`):
                ```json
                [
                  {
                    "categoryId": "uuid1", "name": "GPU", "slug": "gpu", "description": "...", "createdAt": "...", "updatedAt": "..."
                  },
                  {
                    "categoryId": "uuid2", "name": "CPU", "slug": "cpu", "description": "...", "createdAt": "...", "updatedAt": "..."
                  }
                ]
                ```
        *   `GET /api/categories/{categoryIdOrSlug}`
            *   Response (`200 OK`): (Single category object as in POST response)
            *   Error Responses: `404 Not Found`.
        *   `PUT /api/categories/{categoryId}` (Admin Only)
            *   Request Body: (Similar to POST, all fields optional for update)
                ```json
                {
                  "name": "Graphics Cards",
                  "description": "Updated description"
                }
                ```
            *   Response (`200 OK`): (Updated category object)
            *   Error Responses: `400 Bad Request`, `401 Unauthorized`, `403 Forbidden`, `404 Not Found`.
        *   `DELETE /api/categories/{categoryId}` (Admin Only)
            *   Response (`204 No Content`)
            *   Error Responses: `401 Unauthorized`, `403 Forbidden`, `404 Not Found`, `400 Bad Request` (e.g., if category contains products and deletion is restricted). Implement soft delete or prevent deletion if products are associated (default to prevent deletion if products exist).

    *   **Products:**
        *   `POST /api/products` (Admin Only)
            *   Request Body:
                ```json
                {
                  "name": "Nvidia RTX 4070 Ti SUPER",
                  "manufacturer": "Nvidia",
                  "modelNumber": "PG141-SKU331",
                  "sku": "NV-4070TIS-FE-SKU",
                  "categoryId": "uuid-of-gpu-category",
                  "description": "High-performance graphics card.",
                  "specifications": { "memory": "16GB GDDR6X", "cudaCores": 8448 }
                }
                ```
            *   Response (`201 Created`): (Full product object including generated `productId`, `createdAt`, `updatedAt`, `isActive: true`)
            *   Error Responses: `400 Bad Request` (validation, categoryId not found), `401 Unauthorized`, `403 Forbidden`.
        *   `GET /api/products?categoryId={idOrSlug}&search={term}&page={pageNumber}&pageSize={size}`
            *   Response (`200 OK`): Paginated list of products.
                ```json
                {
                  "items": [ /* product objects */ ],
                  "pageNumber": 1,
                  "pageSize": 10,
                  "totalItems": 100,
                  "totalPages": 10
                }
                ```
        *   `GET /api/products/{productId}`
            *   Response (`200 OK`): (Full product object, including category details nested or as IDs)
            *   Error Responses: `404 Not Found`.
        *   `PUT /api/products/{productId}` (Admin Only)
            *   Request Body: (Similar to POST, all fields optional)
            *   Response (`200 OK`): (Updated product object)
            *   Error Responses: `400 Bad Request`, `401 Unauthorized`, `403 Forbidden`, `404 Not Found`.
        *   `DELETE /api/products/{productId}` (Admin Only)
            *   Response (`204 No Content`) (Typically soft delete by setting `IsActive = false`)
            *   Error Responses: `401 Unauthorized`, `403 Forbidden`, `404 Not Found`.
*   **Data Storage (PostgreSQL - see Section 5 for full schema):** `Categories`, `Products`.
*   **Business Logic Details:**
    *   Slug generation: If a slug is not provided for a category, generate it from the name (lowercase, replace spaces with hyphens, ensure uniqueness by appending a number if needed).
    *   Product deletion: Default to soft delete (set `IsActive = false`).

**3.2 Product Mapping & Site Configuration Module**
*   **Purpose:** Manages product-URL mappings and site-specific scraper configurations.
*   **Implementation:** Services and repositories in `TechTicker.ApiService`.
*   **API Endpoints (exposed by `TechTicker.ApiService`):**

    *   **Mappings:**
        *   `POST /api/mappings` (Admin Only)
            *   Request Body:
                ```json
                {
                  "canonicalProductId": "uuid-product",
                  "sellerName": "Newegg US",
                  "exactProductUrl": "https://www.newegg.com/some-product/p/N82E16814126683",
                  "isActiveForScraping": true,
                  "scrapingFrequencyOverride": "PT4H",
                  "siteConfigId": "uuid-site-config-for-newegg-com"
                }
                ```
            *   Response (`201 Created`): (Full mapping object including generated `mappingId`, `createdAt`, `updatedAt`, `nextScrapeAt` initialized)
            *   Error Responses: `400 Bad Request` (validation, productId or siteConfigId not found), `401 Unauthorized`, `403 Forbidden`.
        *   `GET /api/mappings?canonicalProductId={id}`
            *   Response (`200 OK`): List of mapping objects.
        *   `GET /api/mappings/active` (Internal use by Orchestrator, or Admin)
            *   Response (`200 OK`): List of active mapping objects.
        *   `PUT /api/mappings/{mappingId}` (Admin Only)
            *   Request Body: (Similar to POST, all fields optional)
            *   Response (`200 OK`): (Updated mapping object)
            *   Error Responses: `400 Bad Request`, `401 Unauthorized`, `403 Forbidden`, `404 Not Found`.
        *   `DELETE /api/mappings/{mappingId}` (Admin Only)
            *   Response (`204 No Content`)
            *   Error Responses: `401 Unauthorized`, `403 Forbidden`, `404 Not Found`.

    *   **Site Configurations:**
        *   `POST /api/site-configs` (Admin Only)
            *   Request Body:
                ```json
                {
                  "siteDomain": "newegg.com",
                  "productNameSelector": "h1.product-title",
                  "priceSelector": ".price-current > strong",
                  "stockSelector": "#stock-status",
                  "sellerNameOnPageSelector": ".seller-name"
                }
                ```
            *   Response (`201 Created`): (Full site config object including `siteConfigId`, `createdAt`, `updatedAt`)
            *   Error Responses: `400 Bad Request` (validation, domain already exists), `401 Unauthorized`, `403 Forbidden`.
        *   `GET /api/site-configs?domain={domainName}`
            *   Response (`200 OK`): Single site config object.
            *   Error Responses: `404 Not Found`.
        *   `GET /api/site-configs/{siteConfigId}`
            *   Response (`200 OK`): Single site config object.
            *   Error Responses: `404 Not Found`.
        *   `PUT /api/site-configs/{siteConfigId}` (Admin Only)
            *   Request Body: (Similar to POST, all fields optional)
            *   Response (`200 OK`): (Updated site config object)
            *   Error Responses: `400 Bad Request`, `401 Unauthorized`, `403 Forbidden`, `404 Not Found`.
        *   `DELETE /api/site-configs/{siteConfigId}` (Admin Only)
            *   Response (`204 No Content`)
            *   Error Responses: `401 Unauthorized`, `403 Forbidden`, `404 Not Found`, `400 Bad Request` (e.g., if config is in use by mappings - restrict deletion).
*   **Data Storage (PostgreSQL):** `ProductSellerMappings`, `ScraperSiteConfigurations`.
*   **Business Logic Details:**
    *   `NextScrapeAt` initialization: When a mapping is created or activated, `NextScrapeAt` should be set to `NOW()`.
    *   `SiteDomain` extraction: When a mapping is created, extract the domain from `ExactProductUrl` to help suggest or find `SiteConfigId`.
    *   Prevent deletion of `ScraperSiteConfigurations` if they are actively used by `ProductSellerMappings`.

**3.3 Scraping Orchestration Module**
*   **Purpose:** Schedules and initiates scraping tasks.
*   **Implementation:** A hosted service (`ScrapingOrchestrationHostedService`) within `TechTicker.ScrapingWorker`.
*   **Logic:**
    1.  Runs periodically (e.g., every 1 minute, configurable: `Orchestrator:CheckIntervalSeconds`).
    2.  Fetches active mappings due for scraping: Query `ProductSellerMappings` where `IsActiveForScraping = true` AND `NextScrapeAt <= NOW()`. Order by `NextScrapeAt`. Limit batch size (e.g., 100 mappings per run, configurable: `Orchestrator:BatchSize`).
    3.  For each mapping:
        *   Fetch its `ScraperSiteConfiguration` (if `SiteConfigId` is present). If no config, log an error and skip.
        *   Determine `UserAgent` and `Headers` (see Section 6.6). Maintain a list of User-Agents in configuration (`Scraping:UserAgents`: `["UA1", "UA2"]`). Rotate or pick randomly.
        *   Construct `ScrapeProductPageCommand` (see Section 4).
        *   Publish the command to RabbitMQ.
        *   Update the mapping: `LastScrapedAt = NOW()`, `NextScrapeAt = NOW() + GetScrapeInterval(mapping)`. `GetScrapeInterval`: Use `ScrapingFrequencyOverride` if present. Otherwise, use a global default (e.g., "PT6H", configurable: `Scraping:DefaultFrequency`). Add a random jitter (e.g., +/- 10% of interval, configurable: `Scraping:FrequencyJitterPercentage`).
*   **Interaction with other Modules:**
    *   Reads from `ProductSellerMappings` and `ScraperSiteConfigurations` (direct DB access via EF Core context injected into the worker).
    *   Publishes `ScrapeProductPageCommand` to RabbitMQ.
    *   Subscribes to `ScrapingResultEvent` to potentially adjust `NextScrapeAt` on failures (e.g., back-off strategy).
*   **Configuration Keys (appsettings.json for `TechTicker.ScrapingWorker`):**
    *   `Orchestrator:CheckIntervalSeconds` (e.g., 60)
    *   `Orchestrator:BatchSize` (e.g., 100)
    *   `Scraping:UserAgents` (JSON array of strings)
    *   `Scraping:DefaultFrequency` (e.g., "PT6H")
    *   `Scraping:FrequencyJitterPercentage` (e.g., 0.10 for 10%)
    *   `Scraping:FailureBackoffSeconds` (e.g., array for retry backoffs: `[60, 300, 1800]`)

**3.4 Scraper Module**
*   **Purpose:** Fetches and parses product pages.
*   **Implementation:** A message consumer (`ScraperMessageConsumer`) in `TechTicker.ScrapingWorker` that handles `ScrapeProductPageCommand`.
*   **Logic (on receiving `ScrapeProductPageCommand`):**
    1.  Log reception of command.
    2.  Create `HttpClient` using `IHttpClientFactory`. Configure it: Set `User-Agent` header from `command.ScrapingProfile.UserAgent`. Add other headers from `command.ScrapingProfile.Headers`. Configure `CookieContainer` (`HttpClientHandler.UseCookies = true`). Set timeout (e.g., 30 seconds, configurable: `Scraper:HttpRequestTimeoutSeconds`).
    3.  Perform HTTP GET request to `command.ExactProductUrl`.
    4.  Handle HTTP response: If not success (2xx), log error, publish `ScrapingResultEvent` with `WasSuccessful = false`, `ErrorCode` (e.g., "HTTP_ERROR_XXX", "TIMEOUT"), `HttpStatusCode`, and return.
    5.  Parse HTML content using AngleSharp: `IBrowsingContext context = BrowsingContext.New(Configuration.Default.WithDefaultLoader()); IDocument document = await context.OpenAsync(req => req.Content(htmlContent));`
    6.  Extract data using selectors from `command.Selectors`: `productName = document.QuerySelector(command.Selectors.ProductNameSelector)?.TextContent.Trim(); priceStr = document.QuerySelector(command.Selectors.PriceSelector)?.TextContent.Trim(); stockStr = document.QuerySelector(command.Selectors.StockSelector)?.TextContent.Trim();` Handle cases where selectors don't find elements.
    7.  Attempt to parse price: Remove currency symbols, thousands separators. `Decimal.TryParse(cleanedPriceStr, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal price);`
    8.  Publish `RawPriceDataEvent` (see Section 4) if price is successfully parsed. Include `ScrapedProductName`.
    9.  Publish `ScrapingResultEvent` (see Section 4) with `WasSuccessful = true` (if core data like price was extracted) or `false` with `ErrorCode` (e.g., "PARSING_ERROR_PRICE") if critical data is missing.
*   **Configuration Keys (appsettings.json for `TechTicker.ScrapingWorker`):**
    *   `Scraper:HttpRequestTimeoutSeconds` (e.g., 30)
*   **Error Handling:** Catch exceptions during HTTP request, parsing. Publish failure `ScrapingResultEvent`. If CAPTCHA is suspected (heuristic: look for keywords), use `ErrorCode = "CAPTCHA_DETECTED"`.

**3.5 Price Normalization & Ingestion Module**
*   **Purpose:** Validates and standardizes raw scraped data.
*   **Implementation:** A message consumer (`RawPriceDataConsumer`) in `TechTicker.ApiService` (or a separate worker) that handles `RawPriceDataEvent`.
*   **Logic (on receiving `RawPriceDataEvent`):**
    1.  Log reception.
    2.  Validate data: `ScrapedPrice` must be > 0. `CanonicalProductId` must exist.
    3.  Normalize `ScrapedStockStatus`: Convert to uppercase. Map common phrases (e.g., "IN STOCK" -> "IN_STOCK", "OUT OF STOCK" -> "OUT_OF_STOCK"). Default to "UNKNOWN". This list should be configurable.
    4.  If validation passes: Construct `PricePointRecordedEvent` (see Section 4) and publish it.
    5.  If validation fails, log error and do not publish.
*   **Configuration Keys (appsettings.json for `TechTicker.ApiService`):**
    *   `Normalization:StockStatusMappings` (JSON object, e.g., `{"IN STOCK": "IN_STOCK", "ADD TO CART": "IN_STOCK"}`)

**3.6 Price History Module**
*   **Purpose:** Stores and provides access to historical price data.
*   **Implementation:** Message consumer (`PricePointRecordedConsumer`) in `TechTicker.ApiService` (or worker) handles `PricePointRecordedEvent`. API endpoints in `TechTicker.ApiService` for querying. Uses `PriceHistoryRepository` in `TechTicker.DataAccess`.
*   **Logic (on receiving `PricePointRecordedEvent`):**
    1.  Log reception.
    2.  Create new `PriceHistory` entity from event data.
    3.  Save to database using `PriceHistoryRepository`.
*   **API Endpoints (exposed by `TechTicker.ApiService`):**
    *   `GET /api/products/{canonicalProductId}/price-history?sellerName={sellerName}&startDate={dateISOString}&endDate={dateISOString}&limit={limit}`
        *   Response (`200 OK`):
            ```json
            [
              { "timestamp": "iso-datetime", "price": 123.45, "stockStatus": "IN_STOCK", "sourceUrl": "..." },
              ...
            ]
            ```
        *   Error Responses: `400 Bad Request`, `404 Not Found`.

**3.7 User Authentication & Management Module**
*   **Purpose:** Manages user accounts and authentication.
*   **Implementation:** Uses ASP.NET Core Identity, configured in `TechTicker.ApiService`. `ApplicationUser` class inheriting from `IdentityUser<Guid>`.
*   **API Endpoints (exposed by `TechTicker.ApiService`):**
    *   `POST /api/auth/register`
        *   Request Body: `{ "email": "...", "password": "...", "firstName": "...", "lastName": "..." }`
        *   Response (`200 OK` or `201 Created`): `{ "userId": "...", "email": "...", "message": "..." }`
        *   Error Responses: `400 Bad Request`.
    *   `POST /api/auth/login`
        *   Request Body: `{ "email": "...", "password": "..." }`
        *   Response (`200 OK`): `{ "token": "jwt_token_here", "userId": "...", "email": "..." }`
        *   Error Responses: `400 Bad Request`, `401 Unauthorized`.
    *   `GET /api/users/me` (Authenticated: Requires JWT)
        *   Response (`200 OK`): `{ "userId": "...", "email": "...", "firstName": "...", "lastName": "..." }`
        *   Error Responses: `401 Unauthorized`.
*   **Security Details:** Password Hashing (ASP.NET Core Identity). JWT Claims: `sub` (UserId), `email`, `jti`, `exp`, `iss`, `aud`.
*   **Configuration Keys (appsettings.json for `TechTicker.ApiService`):**
    *   `Jwt:Key`, `Jwt:Issuer`, `Jwt:Audience`, `Jwt:DurationInMinutes`.

**3.8 Alert Definition Module**
*   **Purpose:** Allows users to manage price alert rules.
*   **Implementation:** Services and repositories in `TechTicker.ApiService`.
*   **API Endpoints (exposed by `TechTicker.ApiService`, all require authentication):**
    *   `POST /api/alerts`
        *   Request Body: `{ "canonicalProductId": "...", "conditionType": "PRICE_BELOW", "thresholdValue": 99.99, "percentageValue": 10.0, "specificSellerName": "...", "notificationFrequencyMinutes": 1440 }`
        *   Response (`201 Created`): (Full alert rule object)
        *   Error Responses: `400 Bad Request`, `401 Unauthorized`.
    *   `GET /api/alerts`
        *   Response (`200 OK`): List of user's alert rule objects.
    *   `GET /api/alerts/product/{canonicalProductId}`
        *   Response (`200 OK`): List of active alert rules for a product.
    *   `PUT /api/alerts/{alertRuleId}`
        *   Request Body: (Similar to POST, all fields optional)
        *   Response (`200 OK`): (Updated alert rule object)
        *   Error Responses: `400 Bad Request`, `401 Unauthorized`, `403 Forbidden`, `404 Not Found`.
    *   `DELETE /api/alerts/{alertRuleId}`
        *   Response (`204 No Content`)
        *   Error Responses: `401 Unauthorized`, `403 Forbidden`, `404 Not Found`.
*   **Data Storage (PostgreSQL):** `AlertRules` table.

**3.9 Alert Evaluation Module**
*   **Purpose:** Evaluates new price points against alert rules.
*   **Implementation:** Message consumer (`PricePointAlertEvaluator`) in `TechTicker.ApiService` (or worker) handles `PricePointRecordedEvent`.
*   **Logic (on receiving `PricePointRecordedEvent` `ppe`):**
    1.  Log reception.
    2.  Fetch active alert rules for `ppe.CanonicalProductId`, filtered by `SpecificSellerName` if applicable.
    3.  For each matching `alertRule`:
        *   Check `NotificationFrequencyMinutes`: If `alertRule.LastNotifiedAt` is recent, skip.
        *   Evaluate `alertRule.ConditionType`:
            *   **`PRICE_BELOW`**: If `ppe.Price < alertRule.ThresholdValue`.
            *   **`PERCENT_DROP_FROM_LAST`**: Fetch last price for product/seller; if `ppe.Price` is lower by `alertRule.PercentageValue`.
            *   **`BACK_IN_STOCK`**: Fetch last stock status; if was "OUT_OF_STOCK" and `ppe.StockStatus` is "IN_STOCK".
        *   If condition met: Fetch product details, construct `AlertTriggeredEvent` (see Section 4), publish it, and update `alertRule.LastNotifiedAt`.
*   **Interaction with other Modules:** Consumes `PricePointRecordedEvent`. Publishes `AlertTriggeredEvent`. Reads from `AlertRules`, `PriceHistory`, `Products`, `Categories`.

**3.10 Notification Module**
*   **Purpose:** Sends notifications (email).
*   **Implementation:** Message consumer (`AlertNotificationConsumer`) in `TechTicker.NotificationWorker` handles `AlertTriggeredEvent`. Use **MailKit** for SMTP.
*   **Logic (on receiving `AlertTriggeredEvent` `ate`):**
    1.  Log reception.
    2.  Fetch user's email for `ate.UserId`.
    3.  Format email (Subject: "TechTicker Price Alert: {ate.ProductName}", Body with details).
    4.  Send email using MailKit and SMTP settings.
    5.  Log success/failure. Implement retry logic.
*   **Configuration Keys (appsettings.json for `TechTicker.NotificationWorker`):**
    *   `Smtp:Host`, `Smtp:Port`, `Smtp:Username`, `Smtp:Password`, `Smtp:FromAddress`, `Smtp:UseSsl`.

**3.11 API Layer**
*   **Purpose:** Entry point for client requests.
*   **Implementation:** ASP.NET Core Controllers within `TechTicker.ApiService`.
*   **Responsibilities:** Routing, AuthN/AuthZ, Input Validation, Response Formatting, Global Exception Handling.

---

### 4. Messaging Backbone for Background Tasks

*   **Technology:** RabbitMQ. Use `RabbitMQ.Client`.
*   **Implementation Notes:** Define durable exchanges and queues. Use persistent messages. Manual acknowledgements. Implement DLX. Manage `IConnection` and `IModel`. .NET Aspire `AppHost` adds RabbitMQ as a resource.

*   **Exchanges and Message Payloads:**

    *   **`scraping_commands_exchange` (Direct Exchange)**
        *   Queue: `scraping_commands_queue` (bound with routing key `scrape.product.page`)
        *   Message: `ScrapeProductPageCommand` (Routing Key: `scrape.product.page`)
            *   Payload:
                ```json
                {
                  "mappingId": "uuid",
                  "canonicalProductId": "uuid",
                  "sellerName": "string",
                  "exactProductUrl": "string (URL)",
                  "selectors": {
                    "productNameSelector": "string (CSS selector)",
                    "priceSelector": "string (CSS selector)",
                    "stockSelector": "string (CSS selector)",
                    "sellerNameOnPageSelector": "string (CSS selector, optional)"
                  },
                  "scrapingProfile": {
                    "userAgent": "string",
                    "headers": { /* dictionary of string key-value pairs, optional */ }
                  }
                }
                ```
    *   **`scraping_results_exchange` (Topic Exchange)**
        *   Message: `ScrapingResultEvent` (Routing Key: `scrape.result.{success|failure}.{mappingId}`)
            *   Payload:
                ```json
                {
                  "mappingId": "uuid",
                  "wasSuccessful": true,
                  "timestamp": "datetimeoffset (ISO 8601)",
                  "errorMessage": "string, null if successful",
                  "errorCode": "string, e.g., BLOCKED_BY_CAPTCHA, HTTP_ERROR_403, PARSING_ERROR_PRICE, null if successful",
                  "httpStatusCode": "integer, null if not applicable"
                }
                ```
    *   **`price_data_exchange` (Topic Exchange)**
        *   Message: `RawPriceDataEvent` (Routing Key: `price.data.raw.{productId}`)
            *   Payload:
                ```json
                {
                  "canonicalProductId": "uuid",
                  "sellerName": "string",
                  "scrapedPrice": 123.45,
                  "scrapedStockStatus": "string, e.g., In Stock, Out of Stock",
                  "timestamp": "datetimeoffset (ISO 8601)",
                  "sourceUrl": "string (URL)",
                  "scrapedProductName": "string, optional"
                }
                ```
        *   Message: `PricePointRecordedEvent` (Routing Key: `price.data.recorded.{productId}`)
            *   Payload:
                ```json
                {
                  "canonicalProductId": "uuid",
                  "sellerName": "string",
                  "price": 123.45,
                  "stockStatus": "string, e.g., IN_STOCK, OUT_OF_STOCK, UNKNOWN",
                  "sourceUrl": "string (URL)",
                  "timestamp": "datetimeoffset (ISO 8601)"
                }
                ```
    *   **`alert_notifications_exchange` (Topic Exchange)**
        *   Message: `AlertTriggeredEvent` (Routing Key: `alert.triggered.{userId}.{productId}`)
            *   Payload:
                ```json
                {
                  "alertRuleId": "uuid",
                  "userId": "uuid",
                  "canonicalProductId": "uuid",
                  "productName": "string",
                  "productCategoryName": "string, optional",
                  "sellerName": "string",
                  "triggeringPrice": 123.45,
                  "triggeringStockStatus": "string",
                  "ruleDescription": "string, e.g., Price below $100",
                  "productPageUrl": "string (URL)",
                  "timestamp": "datetimeoffset (ISO 8601)"
                }
                ```
---

### 5. Core Data Models (PostgreSQL with EF Core)

**Implementation Notes:** Use EF Core Fluent API in `DbContext`. Generate and apply migrations. Define indexes. `Id` fields are `Guid` (PK, auto-generated). `CreatedAt`/`UpdatedAt` auto-managed (`DateTime.UtcNow`).

*   **`Category`** (`Categories` table)
    *   `CategoryId` (Guid, PK), `Name` (VARCHAR(100), NN, IX, UQ), `Slug` (VARCHAR(100), NN, IX, UQ), `Description` (TEXT, NULL), `CreatedAt` (TIMESTAMPTZ, NN), `UpdatedAt` (TIMESTAMPTZ, NN). Nav: `ICollection<Product> Products`.

*   **`Product`** (`Products` table)
    *   `ProductId` (Guid, PK), `Name` (VARCHAR(255), NN, IX), `Manufacturer` (VARCHAR(100), NULL), `ModelNumber` (VARCHAR(100), NULL), `SKU` (VARCHAR(100), NULL, IX, UQ if NN), `CategoryId` (Guid, FK `Categories.CategoryId`, NN, IX, ON DELETE RESTRICT), `Description` (TEXT, NULL), `Specifications` (JSONB, NULL), `IsActive` (BOOLEAN, NN, DEF TRUE, IX), `CreatedAt` (TIMESTAMPTZ, NN), `UpdatedAt` (TIMESTAMPTZ, NN). Nav: `Category Category`, `ICollection<ProductSellerMapping> ProductSellerMappings`, `ICollection<AlertRule> AlertRules`.

*   **`ProductSellerMapping`** (`ProductSellerMappings` table)
    *   `MappingId` (Guid, PK), `CanonicalProductId` (Guid, FK `Products.ProductId`, NN, IX, ON DELETE CASCADE), `SellerName` (VARCHAR(100), NN), `ExactProductUrl` (VARCHAR(2048), NN), `IsActiveForScraping` (BOOLEAN, NN, DEF TRUE, IX), `ScrapingFrequencyOverride` (VARCHAR(50), NULL), `SiteConfigId` (Guid, FK `ScraperSiteConfigurations.SiteConfigId`, NULL, IX, ON DELETE SET NULL), `LastScrapedAt` (TIMESTAMPTZ, NULL), `NextScrapeAt` (TIMESTAMPTZ, NULL, IX), `CreatedAt` (TIMESTAMPTZ, NN), `UpdatedAt` (TIMESTAMPTZ, NN). Nav: `Product Product`, `ScraperSiteConfiguration SiteConfiguration`.

*   **`ScraperSiteConfiguration`** (`ScraperSiteConfigurations` table)
    *   `SiteConfigId` (Guid, PK), `SiteDomain` (VARCHAR(255), NN, UQ, IX), `ProductNameSelector` (TEXT, NN), `PriceSelector` (TEXT, NN), `StockSelector` (TEXT, NN), `SellerNameOnPageSelector` (TEXT, NULL), `CreatedAt` (TIMESTAMPTZ, NN), `UpdatedAt` (TIMESTAMPTZ, NN). Nav: `ICollection<ProductSellerMapping> ProductSellerMappings`.

*   **`PriceHistory`** (`PriceHistory` table)
    *   `PriceHistoryId` (Guid, PK), `Timestamp` (TIMESTAMPTZ, NN, IX), `CanonicalProductId` (Guid, NN, IX, FK `Products.ProductId` ON DELETE CASCADE), `SellerName` (VARCHAR(100), NN, IX), `Price` (DECIMAL(10, 2), NN), `StockStatus` (VARCHAR(50), NN), `SourceUrl` (VARCHAR(2048), NN). Composite Index: `(CanonicalProductId, SellerName, Timestamp DESC)`.

*   **`User`** (ASP.NET Core Identity tables: `AspNetUsers`, etc. `ApplicationUser` extends `IdentityUser<Guid>`)
    *   `Id` (Guid, PK), `Email` (VARCHAR(256), IX, UQ), `PasswordHash` (TEXT), `FirstName` (VARCHAR(100), NULL), `LastName` (VARCHAR(100), NULL), `CreatedAt` (TIMESTAMPTZ, NN), `UpdatedAt` (TIMESTAMPTZ, NN). Nav: `ICollection<AlertRule> AlertRules`.

*   **`AlertRule`** (`AlertRules` table)
    *   `AlertRuleId` (Guid, PK), `UserId` (Guid, FK `AspNetUsers.Id`, NN, IX, ON DELETE CASCADE), `CanonicalProductId` (Guid, FK `Products.ProductId`, NN, IX, ON DELETE CASCADE), `ConditionType` (VARCHAR(50), NN), `ThresholdValue` (DECIMAL(10,2), NULL), `PercentageValue` (DECIMAL(5,2), NULL), `SpecificSellerName` (VARCHAR(100), NULL, IX), `IsActive` (BOOLEAN, NN, DEF TRUE, IX), `LastNotifiedAt` (TIMESTAMPTZ, NULL), `NotificationFrequencyMinutes` (INT, NN, DEF 1440), `CreatedAt` (TIMESTAMPTZ, NN), `UpdatedAt` (TIMESTAMPTZ, NN). Nav: `User User`, `Product Product`.

---

### 6. Cross-Cutting Concerns

**6.1 Authentication & Authorization:**
    *   JWT as detailed. Admin Role: Define "Admin" role, seed admin user, protect APIs with `[Authorize(Roles = "Admin")]`. User Ownership for `AlertRule` modifications.

**6.2 Logging:**
    *   **Serilog:** Configure in `Program.cs` of all projects via `TechTicker.ServiceDefaults`.
    *   **Format:** Structured logging (JSON for production). Include Timestamp, Level, Message, Exception, CorrelationId, UserId, ProductId, MappingId.
    *   **Correlation IDs:** Generate at request/message start, pass along.
    *   **Aspire Dashboard:** For development viewing.

**6.3 Configuration:**
    *   `appsettings.json` per project, `appsettings.{Environment}.json`. User Secrets for dev. Environment variables for prod.
    *   **.NET Aspire AppHost:**
        ```csharp
        var builder = DistributedApplication.CreateBuilder(args);
        var postgres = builder.AddPostgres("postgres").WithPgAdmin().AddDatabase("techtickerdb");
        var rabbitmq = builder.AddRabbitMQ("rabbitmq");
        var apiService = builder.AddProject<Projects.TechTicker_ApiService>("apiservice")
                                .WithReference(postgres).WithReference(rabbitmq);
        builder.AddProject<Projects.TechTicker_ScrapingWorker>("scrapingworker")
               .WithReference(postgres).WithReference(rabbitmq);
        // ... other workers ...
        builder.Build().Run();
        ```

**6.4 Monitoring & Health Checks:**
    *   **Health Checks:** Implement in all projects via `TechTicker.ServiceDefaults`. Add DB, RabbitMQ checks. View in Aspire Dashboard.
    *   **Metrics & Distributed Tracing:** OpenTelemetry via Aspire.

**6.5 Error Handling Strategy:**
    *   **API:** Global exception handler (ProblemDetails).
    *   **Workers:** Try-catch, DLX for unrecoverable messages.
    *   **Retry Policies:** Polly for transient errors.

**6.6 Scraper Evasion Techniques (Within Constraints)**
    *   **User-Agent Rotation:** Config: `Scraping:UserAgents: ["...", "..."]`. Orchestrator picks one.
    *   **HTTP Header Management:** Config: `Scraping:DefaultHeaders: { ... }`. Orchestrator adds.
    *   **Request Timing and Delays:** Orchestrator uses `DefaultFrequency` and `FrequencyJitterPercentage`. Scraper can add `Scraper:PreRequestDelayMsRange: [1000, 5000]`.
    *   **Basic Cookie Handling:** `HttpClientHandler.UseCookies = true`.
    *   **Limitations:** No IP Rotation, JS Challenges, Advanced Bots, CAPTCHAs.

---

### 7. Deployment Considerations

*   **Containerization:** Aspire helps generate Dockerfiles. Build images for `ApiService` and workers.
*   **Orchestration:** Aspire can output K8s manifests.
*   **Configuration in Production:** Environment variables.
*   **Database Migrations:** Apply via pipeline or carefully on startup.
*   **Scaling:** `ApiService` (load balancer), Workers (multiple instances consuming from RabbitMQ).

---

### 8. Development Roadmap (Summary)

1.  **Phase 1: Foundation:** Aspire setup, Domain, DataAccess, Product Catalog, Mapping & Site Config, basic Scraping (Orchestration, Scraper), Price Normalization & History.
2.  **Phase 2: User Features:** User Auth, Alert Definition, Alert Evaluation, Notification Module.
3.  **Phase 3: API Refinement & Testing:** Thorough API and message flow testing, logging, error handling.
4.  **Phase 4: Scaling & Refinement:** Optimizations, evasion technique refinement, deployment prep.

---

### 9. Non-Functional Requirements

*   **Scalability:** Horizontal scaling for ApiService and Workers.
*   **Reliability/Availability:** Robust error handling, message queues for resilience.
*   **Maintainability:** Modular design within Aspire.
*   **Security:** Secure APIs, data storage, common vulnerabilities.
*   **Performance:** Optimized queries, balanced scraping frequency.
*   **Stealthiness:** Minimize detection within constraints.

---

### 10. Future Enhancements

*   Hierarchical Categories.
*   Category-Specific Alert Rules.
*   Faceted Search.
*   Search-Based Product Discovery.
*   Advanced Alert Conditions.
*   Additional Notification Channels (SMS, Push).
*   Limited Browser Automation Integration (Puppeteer/Playwright).
*   Dynamic Evasion Strategy Adjustment.
*   ML for Selector Finding/Maintenance.
*   User-Contributed Mappings/Selectors.
*   Affiliate Link Integration.
*   Comparative Analysis Features.
*   Admin Dashboard.