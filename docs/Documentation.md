## TechTicker: E-commerce Price Tracker & Alerter - Detailed Software Specification

**Version:** 2.1
**Date:** June 28, 2025
**System Model:** Admin-Managed Exact Product URLs (Focus on Reduced Detectability, with Product Categories)
**Architecture Model:** Monolith with .NET Aspire Backend & Angular SPA Frontend
**Target Audience:** Development Team / Automated Coding Agent
**Status:** Fully Implemented and Operational with Advanced Features

**Preamble:**
This document provides a comprehensive specification of the fully implemented TechTicker application, including its backend services and Angular frontend for administration and CRM. The system is operational and includes all described features plus recent enhancements. This documentation reflects the actual implemented state of the system as of June 2025, including all APIs, frontend components, background workers, database schema, proxy management, bulk operations, and comprehensive testing infrastructure.

**Table of Contents:**

1.  Introduction
    *   1.1 Purpose
    *   1.2 Scope
    *   1.3 Goals
2.  System Architecture
    *   2.1 High-Level Overview
    *   2.2 Technology Stack (Proposed & Specified)
        *   2.2.1 Backend Technology Stack
        *   2.2.2 Frontend Technology Stack
3.  Detailed Application Module Descriptions (Backend)
    *   3.1 Product Catalog Module
    *   3.2 Product Mapping & Site Configuration Module
    *   3.3 Scraping Orchestration Module
    *   3.4 Scraper Module
    *   3.5 Price Normalization & Ingestion Module
    *   3.6 Price History Module
    *   3.7 User Authentication & Management Module (Backend)
    *   3.8 Alert Definition Module
    *   3.9 Alert Evaluation Module
    *   3.10 Notification Module
    *   3.11 Proxy Management Module
    *   3.12 API Layer
4.  Messaging Backbone for Background Tasks
5.  Core Data Models (PostgreSQL with EF Core)
6.  Cross-Cutting Concerns
    *   6.1 Authentication & Authorization
    *   6.2 Logging
    *   6.3 Configuration
    *   6.4 Monitoring & Health Checks
    *   6.5 Error Handling Strategy
    *   6.6 Scraper Evasion Techniques (Within Constraints)
7.  Frontend Application (Angular)
    *   7.1 Purpose & Target Audience
    *   7.2 Key Features & Modules
    *   7.3 UI/UX Considerations
    *   7.4 Interaction with Backend API
    *   7.5 Build and Deployment
    *   7.6 Image Processing & Gallery Features
    *   7.7 Notification Settings Interface
8.  Deployment Considerations
    *   8.1 Backend Deployment
    *   8.2 Frontend Deployment
9.  Development Roadmap (Summary)
10. Non-Functional Requirements
11. Testing Infrastructure
12. Future Enhancements

---

### 1. Introduction

**1.1 Purpose**
The TechTicker system is designed to track prices of specified computer hardware and related products across multiple e-commerce websites. It aims to provide users with historical price data and alert them to price changes based on user-defined rules. Products are organized into categories for better management and user experience. This document outlines the **monolith architecture for the backend, orchestrated and developed using .NET Aspire,** and a **Single Page Application (SPA) frontend built with Angular** for administration and CRM functionalities. The system relies on administrators explicitly defining products (with categories) and their corresponding URLs on seller sites, along with scraper configurations, with a focus on implementing scraping techniques to reduce detectability within given constraints.

**1.2 Scope**
*   **Backend:**
    *   Admin management of a canonical product catalog, including product categories.
    *   Admin management of exact product URLs on e-commerce sites, mapped to canonical products.
    *   Admin management of scraper configurations (CSS/XPath selectors) per e-commerce site domain, and linking them to product mappings.
    *   Automated, periodic scraping of product prices and stock availability from these exact URLs, employing techniques to minimize detection.
    *   Storage and retrieval of historical price data.
    *   User registration and authentication.
    *   User-defined alerts for price changes.
    *   Email notifications for triggered alerts.
    *   API for client applications, supporting product categorization and administrative functions.
*   **Frontend (Angular Admin & CRM Portal):**
    *   Secure login for administrators.
    *   Dashboard for system overview.
    *   CRUD interfaces for Categories, Products, Product Mappings, and Site Configurations.
    *   User management interface (view users, manage roles if applicable).
    *   View all alert rules and their statuses.
    *   View price history for products.
    *   Interface for basic system monitoring or log viewing (if supported by API).

**1.3 Goals**
*   **Accuracy:** Provide accurate and timely price information.
*   **Organization:** Effectively categorize products for ease of browsing and management.
*   **Stealth:** Implement scraping methods that aim to reduce the likelihood of detection by target websites, within the constraint of not using external paid services.
*   **Reliability:** Ensure consistent operation of scraping and alerting mechanisms.
*   **Scalability:** Design the application (both backend and frontend interactions) to handle a growing number of products, users, and tracked sites, with options for scaling the backend monolith or specific background worker components.
*   **Maintainability:** Leverage .NET Aspire to organize the backend monolith into well-defined projects/modules. Employ best practices for Angular development for frontend maintainability.
*   **Usability:** Provide a clear API for the frontend, and intuitive interfaces within the Angular admin portal.

---

### 2. System Architecture

**2.1 High-Level Overview**
TechTicker employs a **monolithic architecture for its backend, built with .NET Aspire**. The backend consists of a primary ASP.NET Core web project handling API requests and user interactions, and several .NET Worker Service projects for background tasks like scraping and notifications. These projects are organized within a single .NET Aspire solution, allowing for unified configuration, service discovery (during development), and simplified orchestration.

The **frontend is a Single Page Application (SPA) built using Angular**. This frontend application communicates with the backend exclusively through the defined RESTful API Layer (Section 3.11). It handles all administrative and CRM functionalities.

While the backend is a monolith, logical separation of concerns is maintained by structuring the application into distinct modules or components, often corresponding to separate class libraries or well-defined namespaces within the main projects. Communication between these backend modules is primarily through direct method calls (dependency injection) for synchronous operations within the web application. For decoupling long-running or resource-intensive tasks such as scraping and notifications, a message broker (e.g., RabbitMQ) is utilized for asynchronous communication between the API layer and background worker components.

**.NET Aspire facilitates (for the backend):**
*   **Development Orchestration:** The **AppHost** project defines and launches the various parts of the application (web API, worker services, databases, messaging queues) during development.
*   **Service Discovery & Configuration:** Simplifies connection string management and service-to-service communication setup in the development environment.
*   **Observability:** Integrates with OpenTelemetry for logging, metrics, and tracing, accessible via the Aspire Dashboard.
*   **Deployment:** Provides tools and patterns to simplify deployment to containerized environments.

**2.2 Technology Stack (Proposed & Specified)**

**2.2.1 Backend Technology Stack**
*   **Application Framework:** .NET 9.0 with ASP.NET Core (for the main web application/API) and .NET Worker Services (for background tasks).
*   **Orchestration & Development:** **.NET Aspire 9.3.1**.
*   **Database:** PostgreSQL. Use **Npgsql** as the ADO.NET provider. Use **Entity Framework Core (EF Core)** as the ORM.
*   **Message Broker:** RabbitMQ with **Aspire.RabbitMQ.Client 9.3.1**.
*   **Web Scraping Library:** **HtmlAgilityPack** for HTML parsing. `HttpClientFactory` for making HTTP requests with proxy support.
*   **Authentication:** JWT (JSON Web Tokens). Use **ASP.NET Core Identity** for user management and **`Microsoft.AspNetCore.Authentication.JwtBearer`** for JWT validation.
*   **Notifications:** **Discord.Net.Webhook 3.17.4** for Discord notifications.
*   **Containerization:** Docker. .NET Aspire will assist in generating Dockerfiles.
*   **Logging:** **Serilog** integrated with .NET Aspire's OpenTelemetry support. Configure Serilog to write to console (for Aspire Dashboard) and potentially a file or a structured logging sink in production.
*   **Project Structure (Implemented):**
    *   `TechTicker.AppHost`: The Aspire orchestrator project. Defines resources like PostgreSQL, RabbitMQ, and orchestrates all services during development.
    *   `TechTicker.ApiService`: ASP.NET Core Web API project. Contains API Controllers, business logic services, EF Core DbContext, and data repositories. Handles synchronous operations and publishes messages for asynchronous tasks.
    *   `TechTicker.ScrapingWorker`: .NET Worker Service project. Contains `ScrapingOrchestrationService` and `ScraperService` with proxy support and anti-detection techniques.
    *   `TechTicker.NotificationWorker`: .NET Worker Service project. Contains Discord notification service with user preference management.
    *   `TechTicker.Domain`: Class library for core domain entities including proxy configurations and alert history.
    *   `TechTicker.DataAccess`: Class library for EF Core `DbContext`, migrations, and repository implementations with comprehensive entity configurations.
    *   `TechTicker.Application`: Class library for application services, DTOs, messaging, and business logic with proxy management and bulk operations.
    *   `TechTicker.Shared`: Common utilities, response models, exception handling, and authentication extensions.
    *   `TechTicker.Frontend`: Angular 20 SPA with comprehensive admin interface, proxy management, and bulk operations.
    *   `TechTicker.ServiceDefaults`: Shared configurations for health checks, OpenTelemetry, and Serilog setup.

**2.2.2 Frontend Technology Stack**
*   **Framework:** Angular 20 (latest stable version).
*   **Language:** TypeScript.
*   **State Management:** Angular services with RxJS BehaviorSubjects for reactive state management.
*   **UI Component Library:** Angular Material for comprehensive UI components and consistent design.
*   **HTTP Client:** Angular's built-in `HttpClientModule` with NSwag-generated TypeScript client.
*   **API Client Generation:** NSwag for automatic TypeScript client generation from OpenAPI specifications.
*   **Routing:** Angular Router with lazy-loaded feature modules.
*   **Forms:** Angular Reactive Forms with comprehensive validation.
*   **Styling:** SCSS with Angular Material theming.
*   **Build Tool:** Angular CLI.
*   **Testing:** Jasmine and Karma for unit tests, with comprehensive test coverage.
*   **Authentication:** JWT-based authentication with role-based access control (RBAC).
*   **Image Handling:** Custom image gallery component with lazy loading and optimization.
*   **Charts & Visualization:** ng2-charts with Chart.js for performance monitoring and analytics.
*   **Virtual Scrolling:** Angular CDK virtual scrolling for large datasets and proxy lists.
*   **Bulk Operations:** Advanced bulk import/export functionality with progress indicators.

---

### 3. Detailed Application Module Descriptions (Backend)

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
                ```
            *   Error Responses: `400 Bad Request` (validation errors, e.g., name missing, slug already exists), `401 Unauthorized`, `403 Forbidden`.
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
*   **Recent Enhancements:**
    *   Enhanced product catalog with improved search and filtering
    *   Support for product specifications stored as JSONB
    *   Comprehensive audit trail for all product changes
    *   Bulk operations for product management
    *   Advanced categorization with hierarchical support
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
    5.  Parse HTML content using HtmlAgilityPack: `var document = new HtmlDocument(); document.LoadHtml(htmlContent);`
    6.  Extract data using selectors from `command.Selectors`: `productName = document.DocumentNode.SelectSingleNode(command.Selectors.ProductNameSelector)?.InnerText.Trim(); priceStr = document.DocumentNode.SelectSingleNode(command.Selectors.PriceSelector)?.InnerText.Trim(); stockStr = document.DocumentNode.SelectSingleNode(command.Selectors.StockSelector)?.InnerText.Trim();` Handle cases where selectors don't find elements.
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

**3.7 User Authentication & Management Module (Backend)**
*   **Purpose:** Manages user accounts, authentication, and role-based access control (RBAC) for API access.
*   **Implementation:** Uses ASP.NET Core Identity with custom RBAC system, configured in `TechTicker.ApiService`. `ApplicationUser` class inheriting from `IdentityUser<Guid>`.
*   **RBAC System:** Implements a comprehensive permission-based authorization system with three main roles:
    *   **Admin:** Full system access with all administrative privileges
    *   **Moderator:** Can manage content and moderate user activities
    *   **User:** Standard user with basic access to user features
*   **API Endpoints (exposed by `TechTicker.ApiService`):**
    *   `POST /api/auth/register`
        *   Request Body: `{ "email": "...", "password": "...", "firstName": "...", "lastName": "..." }`
        *   Response (`201 Created`): `{ "userId": "...", "email": "...", "message": "..." }`
        *   Error Responses: `400 Bad Request`.
    *   `POST /api/auth/login`
        *   Request Body: `{ "email": "...", "password": "..." }`
        *   Response (`200 OK`): `{ "token": "jwt_token_here", "userId": "...", "email": "...", "roles": ["..."] }`
        *   Error Responses: `400 Bad Request`, `401 Unauthorized`.
    *   `GET /api/auth/me` (Authenticated: Requires JWT)
        *   Response (`200 OK`): `{ "userId": "...", "email": "...", "firstName": "...", "lastName": "...", "roles": ["..."] }`
        *   Error Responses: `401 Unauthorized`.
    *   **Admin User Management Endpoints:**
        *   `GET /api/users` (Admin Only)
            *   Response (`200 OK`): Paginated list of users with roles and permissions.
        *   `GET /api/users/{userId}` (Admin Only)
            *   Response (`200 OK`): Single detailed user object with roles.
        *   `PUT /api/users/{userId}` (Admin Only)
            *   Request Body: `{ "firstName": "...", "lastName": "...", "email": "...", "roles": ["Admin", "User"] }`
            *   Response (`200 OK`): Updated user object.
        *   `POST /api/users` (Admin Only)
            *   Request Body: `{ "email": "...", "password": "...", "firstName": "...", "lastName": "...", "roles": ["User"] }`
            *   Response (`201 Created`): New user object.
        *   `DELETE /api/users/{userId}` (Admin Only)
            *   Response (`204 No Content`). Implements soft delete.
    *   **Role Management Endpoints:**
        *   `GET /api/roles` (Admin Only)
            *   Response (`200 OK`): List of all roles with user counts.
        *   `GET /api/permissions` (Admin Only)
            *   Response (`200 OK`): List of all permissions grouped by category.
*   **Security Details:**
    *   Password Hashing (ASP.NET Core Identity with PBKDF2)
    *   JWT Claims: `sub` (UserId), `email`, `role` (multiple), `permission` (multiple), `jti`, `exp`, `iss`, `aud`
    *   Permission-based authorization using custom `RequirePermission` attribute
    *   Role hierarchy with inherited permissions
*   **Configuration Keys (appsettings.json for `TechTicker.ApiService`):**
    *   `Jwt:Key`, `Jwt:Issuer`, `Jwt:Audience`, `Jwt:DurationInMinutes` (default: 1440 minutes)

**3.8 Alert Definition Module**
*   **Purpose:** Allows users to manage price alert rules.
*   **Implementation:** Services and repositories in `TechTicker.ApiService`.
*   **API Endpoints (exposed by `TechTicker.ApiService`, all require authentication unless specified otherwise):**
    *   `POST /api/alerts` (Authenticated User)
        *   Request Body: `{ "canonicalProductId": "...", "conditionType": "PRICE_BELOW", "thresholdValue": 99.99, "percentageValue": 10.0, "specificSellerName": "...", "notificationFrequencyMinutes": 1440 }`
        *   Response (`201 Created`): (Full alert rule object)
        *   Error Responses: `400 Bad Request`, `401 Unauthorized`.
    *   `GET /api/alerts` (Authenticated User - gets their own alerts)
        *   Response (`200 OK`): List of user's alert rule objects.
    *   `GET /api/alerts/product/{canonicalProductId}` (Authenticated User - gets their own alerts for a product)
        *   Response (`200 OK`): List of active alert rules for a product for the current user.
    *   `PUT /api/alerts/{alertRuleId}` (Authenticated User - must own the alert)
        *   Request Body: (Similar to POST, all fields optional)
        *   Response (`200 OK`): (Updated alert rule object)
        *   Error Responses: `400 Bad Request`, `401 Unauthorized`, `403 Forbidden`, `404 Not Found`.
    *   `DELETE /api/alerts/{alertRuleId}` (Authenticated User - must own the alert)
        *   Response (`204 No Content`)
        *   Error Responses: `401 Unauthorized`, `403 Forbidden`, `404 Not Found`.
    *   **Admin Alert Management Endpoints (New/Enhanced for Admin Frontend):**
        *   `GET /api/admin/alerts?userId={userId}&productId={productId}&page={pageNumber}&pageSize={size}` (Admin Only)
            *   Response (`200 OK`): Paginated list of all alert rules, filterable. `{ "items": [alert rule objects], ... }`
*   **Data Storage (PostgreSQL):** `AlertRules` table.

**3.9 Alert Evaluation Module**
*   **Purpose:** Evaluates new price points against alert rules.
*   **Implementation:** Message consumer (`PricePointAlertEvaluator`) in `TechTicker.ApiService` (or worker) handles `PricePointRecordedEvent`.
*   **Logic (on receiving `PricePointRecordedEvent` `ppe`):**
    1.  Log reception.
    2.  Fetch active alert rules for `ppe.CanonicalProductId`, filtered by `SpecificSellerName` if applicable (or if `SpecificSellerName` is null in rule, it applies to all sellers for that product).
    3.  For each matching `alertRule`:
        *   Check `NotificationFrequencyMinutes`: If `alertRule.LastNotifiedAt` is not null and `NOW() - alertRule.LastNotifiedAt < alertRule.NotificationFrequencyMinutes`, skip.
        *   Evaluate `alertRule.ConditionType`:
            *   **`PRICE_BELOW`**: If `ppe.Price < alertRule.ThresholdValue`.
            *   **`PERCENT_DROP_FROM_LAST`**: Fetch last *recorded* price for `ppe.CanonicalProductId` and `ppe.SellerName` from `PriceHistory`. If this is the first price, or if `ppe.Price` is lower than the last price by at least `alertRule.PercentageValue` percent of the last price.
            *   **`BACK_IN_STOCK`**: Fetch last *recorded* stock status for `ppe.CanonicalProductId` and `ppe.SellerName` from `PriceHistory`. If the last known status was "OUT_OF_STOCK" (or "UNKNOWN") and `ppe.StockStatus` is "IN_STOCK".
        *   If condition met: Fetch product details (`Products` table), user details (`AspNetUsers` table for email), construct `AlertTriggeredEvent` (see Section 4), publish it to RabbitMQ, and update `alertRule.LastNotifiedAt = NOW()` and `alertRule.IsActive = false` (if it's a one-time alert, or re-evaluate if it should be re-enabled after some time or by user). This needs careful consideration: alerts might be recurring or one-shot. Default to one-shot for now by setting `IsActive = false` after triggering. User can re-enable.
*   **Interaction with other Modules:** Consumes `PricePointRecordedEvent`. Publishes `AlertTriggeredEvent`. Reads from `AlertRules`, `PriceHistory`, `Products`, `AspNetUsers`.

**3.10 Notification Module**
*   **Purpose:** Sends Discord notifications based on user preferences with comprehensive configuration options.
*   **Implementation:** Message consumer (`AlertNotificationConsumer`) in `TechTicker.NotificationWorker` handles `AlertTriggeredEvent`. Uses **Discord.Net.Webhook 3.17.4** for Discord webhook integration.
*   **User Notification Preferences:** Each user can configure:
    *   Discord webhook URL (personal Discord server/channel)
    *   Enable/disable Discord notifications
    *   Select up to 5 products for notifications
    *   Custom bot name and avatar URL
    *   Notification frequency preferences
    *   Channel-specific settings
*   **Logic (on receiving `AlertTriggeredEvent` `ate`):**
    1.  Log reception with detailed context.
    2.  Fetch user's notification preferences from database.
    3.  Check if Discord notifications are enabled for the user.
    4.  Verify user has configured a Discord webhook URL.
    5.  Check if the triggered product is in user's selected notification products.
    6.  If all conditions met, send Discord notification with rich embed:
        *   Title: "🔔 TechTicker Alert"
        *   Product name and category with enhanced formatting
        *   Current price with color coding (green for good deals, red for price increases)
        *   Seller information with reputation indicators
        *   Stock status with appropriate icons and availability details
        *   Alert rule description with condition details
        *   Direct link to product page with tracking parameters
        *   Timestamp with timezone support
        *   Performance metrics and trend indicators
    7.  Use custom bot name and avatar if configured by user.
    8.  Log success/failure with comprehensive retry logic for transient Discord API errors.
    9.  Track notification delivery metrics for performance monitoring.
*   **API Endpoints for User Preferences:**
    *   `GET /api/notification-preferences` - Get user's notification settings
    *   `PUT /api/notification-preferences` - Update user's notification settings
    *   `GET /api/notification-preferences/products` - Get available products for notification selection
    *   `POST /api/notification-preferences/test-webhook` - Test Discord webhook configuration
    *   `GET /api/notification-preferences/summary` - Get notification preferences summary
    *   `GET /api/notification-preferences/stats` - Get notification delivery statistics
*   **Configuration Keys (appsettings.json for `TechTicker.NotificationWorker`):**
    *   `Discord:BotName` (default bot name), `Discord:AvatarUrl` (default avatar), `Discord:EnableDiscordNotifications` (global toggle)
    *   `Discord:RetryAttempts`, `Discord:RetryDelaySeconds`, `Discord:MaxConcurrentNotifications`

**3.11 Proxy Management Module**
*   **Purpose:** Comprehensive proxy configuration and management system for enhanced scraping stealth and reliability.
*   **Implementation:** Services (`ProxyService`, `ProxyPoolService`, `ProxyHealthMonitorService`) in `TechTicker.Application` with API endpoints in `TechTicker.ApiService`.
*   **Key Features:**
    *   **Proxy Configuration Management:** Support for HTTP, HTTPS, SOCKS4, and SOCKS5 proxies
    *   **Bulk Import/Export:** CSV and text-based bulk proxy import with parsing validation
    *   **Health Monitoring:** Automated proxy health checks with configurable intervals
    *   **Pool Management:** Intelligent proxy rotation and selection strategies
    *   **Performance Tracking:** Comprehensive metrics and analytics for proxy performance
    *   **Virtual Scrolling:** Optimized UI for managing large proxy lists
*   **API Endpoints (exposed by `TechTicker.ApiService`):**
    *   **Proxy CRUD Operations:**
        *   `POST /api/proxies` - Create new proxy configuration
        *   `GET /api/proxies` - Get paginated proxy list with filtering
        *   `GET /api/proxies/{id}` - Get specific proxy configuration
        *   `PUT /api/proxies/{id}` - Update proxy configuration
        *   `DELETE /api/proxies/{id}` - Delete proxy configuration
    *   **Bulk Operations:**
        *   `POST /api/proxies/bulk-import` - Import multiple proxies from CSV/text
        *   `POST /api/proxies/parse-text` - Parse proxy text for validation
        *   `PUT /api/proxies/bulk-enable` - Bulk enable/disable proxies
        *   `DELETE /api/proxies/bulk-delete` - Bulk delete proxies
    *   **Testing & Health:**
        *   `POST /api/proxies/{id}/test` - Test individual proxy
        *   `POST /api/proxies/bulk-test` - Test multiple proxies
        *   `GET /api/proxies/stats` - Get proxy statistics and health metrics
        *   `GET /api/proxies/health-summary` - Get overall proxy pool health
*   **Frontend Components:**
    *   **Proxy List Component:** Virtual scrolling list with bulk operations
    *   **Bulk Import Component:** Drag-and-drop import with progress indicators
    *   **Proxy Form Component:** Comprehensive proxy configuration form
    *   **Health Dashboard:** Real-time proxy health monitoring
*   **Data Storage:** `ProxyConfigurations` table with comprehensive proxy metadata
*   **Background Services:**
    *   `ProxyHealthMonitorService`: Automated health checking
    *   `ProxyPoolService`: Intelligent proxy selection and rotation

**3.12 API Layer**
*   **Purpose:** Entry point for client requests (including the Angular Admin Frontend).
*   **Implementation:** ASP.NET Core Controllers within `TechTicker.ApiService`.
*   **Responsibilities:**
    *   **Routing:** Mapping HTTP requests to controller actions.
    *   **Authentication & Authorization:** Validating JWTs, checking roles (`[Authorize(Roles = "Admin")]` for admin endpoints, `[Authorize]` for general user endpoints).
    *   **Input Validation:** Using Data Annotations and FluentValidation (or similar) on DTOs. Returning `400 Bad Request` with `ValidationProblemDetails`.
    *   **Request Deserialization & Response Serialization:** Handling JSON.
    *   **Calling Business Logic Services:** Orchestrating calls to application services.
    *   **Response Formatting:** Constructing appropriate HTTP responses (status codes, headers, body).
    *   **Global Exception Handling:** Middleware to catch unhandled exceptions and return standardized error responses (e.g., `500 Internal Server Error` with `ProblemDetails`).
    *   **CORS Configuration:** Enabling Cross-Origin Resource Sharing to allow the Angular frontend (served from a different domain/port during development and potentially in production) to make requests to the API.

---

### 4. Messaging Backbone for Background Tasks

*   **Technology:** RabbitMQ. Use `RabbitMQ.Client`.
*   **Implementation Notes:**
    *   Define durable exchanges and queues to survive broker restarts.
    *   Use persistent messages to survive broker restarts.
    *   Use manual acknowledgements (`ack`/`nack`) in consumers to ensure messages are processed reliably. `Nack` with `requeue=false` for unrecoverable errors to send to DLX.
    *   Implement Dead Letter Exchanges (DLX) and Dead Letter Queues (DLQ) for messages that cannot be processed after retries.
    *   Manage `IConnection` and `IModel` instances carefully (e.g., one connection per application, channels per thread/task).
    *   .NET Aspire `AppHost` will add RabbitMQ as a discoverable resource, simplifying connection string management in development.
    *   Consumers should be idempotent where possible.

*   **Exchanges and Message Payloads (Implemented Configuration):**

    *   **`techticker.scraping` (Topic Exchange)**
        *   Purpose: Routes scraping commands and results
        *   Queues: `scrape.commands`, `scraping.results`
        *   Message: `ScrapeProductPageCommand` (Published by `ScrapingOrchestrationService`)
            *   Routing Key: `scrape.command`
            *   Payload:
                ```json
                {
                  "mappingId": "uuid", // ProductSellerMapping.MappingId
                  "canonicalProductId": "uuid", // Product.ProductId
                  "sellerName": "string", // ProductSellerMapping.SellerName
                  "exactProductUrl": "string (URL)", // ProductSellerMapping.ExactProductUrl
                  "selectors": { // From ScraperSiteConfiguration
                    "productNameSelector": "string (CSS selector)",
                    "priceSelector": "string (CSS selector)",
                    "stockSelector": "string (CSS selector)",
                    "sellerNameOnPageSelector": "string (CSS selector, optional)"
                  },
                  "scrapingProfile": {
                    "userAgent": "string", // Selected User-Agent
                    "headers": { /* dictionary of string key-value pairs, optional */ } // Additional headers
                  }
                }
                ```
    *   **`scraping_results_exchange` (Topic Exchange)**
        *   Purpose: Publishes results of scraping attempts, can be consumed by orchestrator or logging/monitoring.
        *   Message: `ScrapingResultEvent` (Published by `ScraperModule`)
            *   Routing Key: `scrape.result.{success|failure}.{mappingId}` (e.g., `scrape.result.success.uuid-mapping`, `scrape.result.failure.uuid-mapping`)
            *   Payload:
                ```json
                {
                  "mappingId": "uuid",
                  "wasSuccessful": true, // or false
                  "timestamp": "datetimeoffset (ISO 8601)",
                  "errorMessage": "string, null if successful",
                  "errorCode": "string, e.g., BLOCKED_BY_CAPTCHA, HTTP_ERROR_403, PARSING_ERROR_PRICE, TIMEOUT, null if successful",
                  "httpStatusCode": "integer, null if not applicable" // e.g., 200, 403, 404, 500
                }
                ```
    *   **`price_data_exchange` (Topic Exchange)**
        *   Purpose: Handles the flow of price data from raw scraped info to normalized and recorded points.
        *   Message: `RawPriceDataEvent` (Published by `ScraperModule` after successful parsing)
            *   Routing Key: `price.data.raw.{canonicalProductId}`
            *   Payload:
                ```json
                {
                  "canonicalProductId": "uuid",
                  "sellerName": "string",
                  "scrapedPrice": 123.45, // Decimal as number
                  "scrapedStockStatus": "string, e.g., In Stock, Out of Stock, Available for Preorder", // Raw text
                  "timestamp": "datetimeoffset (ISO 8601)", // Time of scrape
                  "sourceUrl": "string (URL)", // The URL that was scraped
                  "scrapedProductName": "string, optional", // Product name as seen on the page
                  "primaryImageUrl": "string, optional", // Primary product image URL
                  "additionalImageUrls": ["string"], // Additional product image URLs
                  "originalImageUrls": ["string"] // Original scraped image URLs
                }
                ```
        *   Message: `PricePointRecordedEvent` (Published by price normalization after validation)
            *   Routing Key: `pricedata.recorded`
            *   Payload:
                ```json
                {
                  "canonicalProductId": "uuid",
                  "sellerName": "string",
                  "price": 123.45, // Decimal as number, normalized
                  "stockStatus": "string, e.g., IN_STOCK, OUT_OF_STOCK, PREORDER, UNKNOWN", // Normalized
                  "sourceUrl": "string (URL)",
                  "timestamp": "datetimeoffset (ISO 8601)" // Time of scrape
                }
                ```
    *   **`techticker.alerts` (Topic Exchange)**
        *   Purpose: Triggers Discord notifications when alerts are met
        *   Queue: `alerts.triggered`
        *   Message: `AlertTriggeredEvent` (Published by `AlertEvaluationService`)
            *   Routing Key: `alert.triggered`
            *   Payload:
                ```json
                {
                  "alertRuleId": "uuid",
                  "userId": "uuid", // User who owns the alert
                  "userEmail": "string", // User's email
                  "userFirstName": "string, optional", // User's first name
                  "canonicalProductId": "uuid",
                  "productName": "string", // Canonical Product Name
                  "productCategoryName": "string, optional", // Category of the product
                  "sellerName": "string", // Seller involved in the price point
                  "triggeringPrice": 123.45, // The price that triggered the alert
                  "triggeringStockStatus": "string", // The stock status at the time of trigger
                  "ruleDescription": "string, e.g., Price below $100.00", // Human-readable description of the rule
                  "productPageUrl": "string (URL)", // The specific seller's product page URL
                  "timestamp": "datetimeoffset (ISO 8601)" // When the alert was triggered
                }
                ```

**Implemented Queue Configuration:**
*   All queues are durable and persistent
*   Manual acknowledgments with retry logic
*   Dead letter queues for failed messages
*   Connection pooling and channel management
*   Aspire integration for service discovery and configuration
---

### 5. Core Data Models (PostgreSQL with EF Core)

**Implementation Notes:**
*   Use EF Core Fluent API in `OnModelCreating` method of the `DbContext` for precise configuration (indexes, constraints, relationships, column types).
*   Generate and apply migrations (`dotnet ef migrations add InitialCreate`, `dotnet ef database update`).
*   All `Id` fields will be `Guid` and serve as Primary Keys, typically auto-generated by the database or EF Core (`ValueGeneratedOnAdd()`).
*   `CreatedAt` and `UpdatedAt` timestamps should be `DateTimeOffset` (to store timezone info, or `DateTime` mapped to `TIMESTAMPTZ` and always store as UTC). Managed automatically (e.g., via interceptors or overriding `SaveChanges`).
*   Define appropriate indexes for frequently queried columns and foreign keys.
*   **Current Implementation Status:** All entities are fully implemented with comprehensive EF Core configurations, migrations applied, and production-ready.

*   **`Category`** (`Categories` table)
    *   `CategoryId` (Guid, PK)
    *   `Name` (VARCHAR(100), NN, Unique Index)
    *   `Slug` (VARCHAR(100), NN, Unique Index)
    *   `Description` (TEXT, NULL)
    *   `CreatedAt` (TIMESTAMPTZ, NN)
    *   `UpdatedAt` (TIMESTAMPTZ, NN)
    *   Navigation Property: `ICollection<Product> Products` (One-to-Many with `Product`)

*   **`Product`** (`Products` table)
    *   `ProductId` (Guid, PK)
    *   `Name` (VARCHAR(255), NN, Index)
    *   `Manufacturer` (VARCHAR(100), NULL)
    *   `ModelNumber` (VARCHAR(100), NULL)
    *   `SKU` (VARCHAR(100), NULL, Unique Index if not null)
    *   `CategoryId` (Guid, FK to `Categories.CategoryId`, NN, Index, ON DELETE RESTRICT)
    *   `Description` (TEXT, NULL)
    *   `Specifications` (JSONB, NULL) - Store product-specific attributes like RAM, storage, color, etc.
    *   `IsActive` (BOOLEAN, NN, DEFAULT TRUE, Index) - For soft deletes.
    *   `CreatedAt` (TIMESTAMPTZ, NN)
    *   `UpdatedAt` (TIMESTAMPTZ, NN)
    *   Navigation Properties: `Category Category`, `ICollection<ProductSellerMapping> ProductSellerMappings`, `ICollection<AlertRule> AlertRules`.

*   **`ProductSellerMapping`** (`ProductSellerMappings` table)
    *   `MappingId` (Guid, PK)
    *   `CanonicalProductId` (Guid, FK to `Products.ProductId`, NN, Index, ON DELETE CASCADE)
    *   `SellerName` (VARCHAR(100), NN) - E.g., "Amazon", "Newegg", "BestBuy".
    *   `ExactProductUrl` (VARCHAR(2048), NN) - The specific URL on the seller's site.
    *   `IsActiveForScraping` (BOOLEAN, NN, DEFAULT TRUE, Index)
    *   `ScrapingFrequencyOverride` (VARCHAR(50), NULL) - ISO 8601 Duration format, e.g., "PT1H" for 1 hour.
    *   `SiteConfigId` (Guid, FK to `ScraperSiteConfigurations.SiteConfigId`, NULL, Index, ON DELETE SET NULL) - Allows using a shared config or specific override.
    *   `LastScrapedAt` (TIMESTAMPTZ, NULL)
    *   `NextScrapeAt` (TIMESTAMPTZ, NULL, Index) - Used by orchestrator to schedule next scrape.
    *   `LastScrapeStatus` (VARCHAR(50), NULL) - e.g., "SUCCESS", "FAILURE_HTTP", "FAILURE_PARSING"
    *   `LastScrapeErrorCode` (VARCHAR(50), NULL) - e.g., "CAPTCHA_DETECTED"
    *   `ConsecutiveFailureCount` (INT, NN, DEFAULT 0)
    *   `CreatedAt` (TIMESTAMPTZ, NN)
    *   `UpdatedAt` (TIMESTAMPTZ, NN)
    *   Navigation Properties: `Product Product`, `ScraperSiteConfiguration SiteConfiguration`.
    *   Unique Constraint: `(CanonicalProductId, SellerName, ExactProductUrl)` to prevent duplicate mappings.

*   **`ScraperSiteConfiguration`** (`ScraperSiteConfigurations` table)
    *   `SiteConfigId` (Guid, PK)
    *   `SiteDomain` (VARCHAR(255), NN, Unique Index) - E.g., "amazon.com", "newegg.com".
    *   `ProductNameSelector` (TEXT, NN) - CSS or XPath selector.
    *   `PriceSelector` (TEXT, NN) - CSS or XPath selector for price.
    *   `StockSelector` (TEXT, NN) - CSS or XPath selector for stock status.
    *   `SellerNameOnPageSelector` (TEXT, NULL) - If seller name needs to be extracted from page (for marketplaces).
    *   `DefaultUserAgent` (TEXT, NULL) - Specific UA for this site if needed.
    *   `AdditionalHeaders` (JSONB, NULL) - Key-value pairs for site-specific headers.
    *   `IsEnabled` (BOOLEAN, NN, DEFAULT TRUE)
    *   `CreatedAt` (TIMESTAMPTZ, NN)
    *   `UpdatedAt` (TIMESTAMPTZ, NN)
    *   Navigation Property: `ICollection<ProductSellerMapping> ProductSellerMappings`.

*   **`PriceHistory`** (`PriceHistory` table)
    *   `PriceHistoryId` (Guid, PK)
    *   `Timestamp` (TIMESTAMPTZ, NN, Index) - When the price was recorded.
    *   `CanonicalProductId` (Guid, FK to `Products.ProductId`, NN, Index, ON DELETE CASCADE)
    *   `MappingId` (Guid, FK to `ProductSellerMappings.MappingId`, NN, Index, ON DELETE CASCADE) - Links to the specific URL/seller mapping.
    *   `SellerName` (VARCHAR(100), NN, Index) - Denormalized for easier querying, but consistent with Mapping.
    *   `Price` (DECIMAL(10, 2), NN)
    *   `StockStatus` (VARCHAR(50), NN) - Normalized status: "IN_STOCK", "OUT_OF_STOCK", "PREORDER", "UNKNOWN".
    *   `SourceUrl` (VARCHAR(2048), NN) - The URL from which this price was scraped.
    *   `ScrapedProductNameOnPage` (VARCHAR(512), NULL) - Name as it appeared on the page, for reference.
    *   Composite Index: `(CanonicalProductId, SellerName, Timestamp DESC)` for typical queries.
    *   Composite Index: `(MappingId, Timestamp DESC)`.

*   **`User` (ApplicationUser)** (ASP.NET Core Identity tables: `AspNetUsers`, `AspNetUserRoles`, `AspNetRoles`, etc.)
    *   `Id` (Guid, PK) - Inherited from `IdentityUser<Guid>`.
    *   `Email` (VARCHAR(256), Unique Index) - Inherited.
    *   `PasswordHash` (TEXT) - Inherited.
    *   `FirstName` (VARCHAR(100), NULL) - Custom field.
    *   `LastName` (VARCHAR(100), NULL) - Custom field.
    *   `CreatedAt` (TIMESTAMPTZ, NN) - Custom field.
    *   `UpdatedAt` (TIMESTAMPTZ, NN) - Custom field.
    *   `IsActive` (BOOLEAN, NN, DEFAULT TRUE) - Custom field for soft deactivation.
    *   Navigation Properties: `ICollection<AlertRule> AlertRules`, `ICollection<IdentityUserRole<Guid>> UserRoles`.

*   **`AlertRule`** (`AlertRules` table)
    *   `AlertRuleId` (Guid, PK)
    *   `UserId` (Guid, FK to `AspNetUsers.Id`, NN, Index, ON DELETE CASCADE)
    *   `CanonicalProductId` (Guid, FK to `Products.ProductId`, NN, Index, ON DELETE CASCADE)
    *   `ConditionType` (VARCHAR(50), NN) - E.g., "PRICE_BELOW", "PERCENT_DROP_FROM_LAST", "BACK_IN_STOCK".
    *   `ThresholdValue` (DECIMAL(10,2), NULL) - For "PRICE_BELOW".
    *   `PercentageValue` (DECIMAL(5,2), NULL) - For "PERCENT_DROP_FROM_LAST" (e.g., 10.00 for 10%).
    *   `SpecificSellerName` (VARCHAR(100), NULL, Index) - If alert is for a specific seller, else applies to any seller for the product.
    *   `IsActive` (BOOLEAN, NN, DEFAULT TRUE, Index) - User can enable/disable alerts. Can also be set to false after triggering if it's a one-shot alert.
    *   `LastNotifiedAt` (TIMESTAMPTZ, NULL) - To manage notification frequency.
    *   `NotificationFrequencyMinutes` (INT, NN, DEFAULT 1440) - How often to notify for the same condition (e.g., 24 hours = 1440 mins). 0 means notify always if condition met and not recently notified.
    *   `TriggerCount` (INT, NN, DEFAULT 0)
    *   `CreatedAt` (TIMESTAMPTZ, NN)
    *   `UpdatedAt` (TIMESTAMPTZ, NN)
    *   Navigation Properties: `User User`, `Product Product`.
    *   Index: `(CanonicalProductId, IsActive)`
    *   Index: `(UserId, IsActive)`

*   **`UserNotificationPreferences`** (`UserNotificationPreferences` table)
    *   `UserNotificationPreferencesId` (Guid, PK)
    *   `UserId` (Guid, FK to `AspNetUsers.Id`, NN, Unique Index, ON DELETE CASCADE)
    *   `DiscordWebhookUrl` (VARCHAR(500), NULL) - User's Discord webhook URL
    *   `IsDiscordNotificationEnabled` (BOOLEAN, NN, DEFAULT FALSE)
    *   `NotificationProductIds` (TEXT, NULL) - JSON array of product IDs (up to 5)
    *   `CustomBotName` (VARCHAR(100), NULL) - Custom Discord bot name
    *   `CustomAvatarUrl` (VARCHAR(500), NULL) - Custom Discord bot avatar URL
    *   `CreatedAt` (TIMESTAMPTZ, NN)
    *   `UpdatedAt` (TIMESTAMPTZ, NN)
    *   Navigation Properties: `User User`

*   **`Permission`** (`Permissions` table)
    *   `PermissionId` (Guid, PK)
    *   `Name` (VARCHAR(100), NN, Unique Index) - E.g., "products.read", "alerts.create"
    *   `Description` (VARCHAR(255), NULL)
    *   `Category` (VARCHAR(50), NN, Index) - E.g., "Products", "Alerts", "System"
    *   `CreatedAt` (TIMESTAMPTZ, NN)
    *   `UpdatedAt` (TIMESTAMPTZ, NN)
    *   Navigation Properties: `ICollection<RolePermission> RolePermissions`

*   **`RolePermission`** (`RolePermissions` table)
    *   `RolePermissionId` (Guid, PK)
    *   `RoleId` (Guid, FK to `AspNetRoles.Id`, NN, Index, ON DELETE CASCADE)
    *   `PermissionId` (Guid, FK to `Permissions.PermissionId`, NN, Index, ON DELETE CASCADE)
    *   `CreatedAt` (TIMESTAMPTZ, NN)
    *   Navigation Properties: `IdentityRole<Guid> Role`, `Permission Permission`
    *   Unique Constraint: `(RoleId, PermissionId)`

*   **`ProxyConfiguration`** (`ProxyConfigurations` table)
    *   `ProxyConfigurationId` (Guid, PK)
    *   `Name` (VARCHAR(255), NN) - Friendly name for the proxy
    *   `Host` (VARCHAR(255), NN, Index) - Proxy server hostname or IP
    *   `Port` (INT, NN, Index) - Proxy server port
    *   `ProxyType` (VARCHAR(20), NN, Index) - HTTP, HTTPS, SOCKS4, SOCKS5
    *   `Username` (VARCHAR(255), NULL) - Authentication username
    *   `Password` (VARCHAR(255), NULL) - Authentication password (encrypted)
    *   `IsActive` (BOOLEAN, NN, DEFAULT TRUE, Index) - Whether proxy is enabled
    *   `IsHealthy` (BOOLEAN, NN, DEFAULT TRUE, Index) - Current health status
    *   `LastHealthCheck` (TIMESTAMPTZ, NULL) - Last health check timestamp
    *   `HealthCheckUrl` (VARCHAR(2048), NULL) - Custom health check URL
    *   `ResponseTimeMs` (INT, NULL) - Last response time in milliseconds
    *   `SuccessCount` (INT, NN, DEFAULT 0) - Total successful requests
    *   `FailureCount` (INT, NN, DEFAULT 0) - Total failed requests
    *   `LastUsedAt` (TIMESTAMPTZ, NULL) - Last time proxy was used
    *   `Notes` (TEXT, NULL) - Additional notes or comments
    *   `CreatedAt` (TIMESTAMPTZ, NN)
    *   `UpdatedAt` (TIMESTAMPTZ, NN)
    *   Unique Constraint: `(Host, Port, ProxyType)` to prevent duplicate proxy configurations
    *   Index: `(IsActive, IsHealthy)` for efficient proxy pool queries
    *   Index: `(LastHealthCheck)` for health monitoring

*   **`AlertHistory`** (`AlertHistories` table)
    *   `AlertHistoryId` (Guid, PK)
    *   `AlertRuleId` (Guid, FK to `AlertRules.AlertRuleId`, NN, Index, ON DELETE CASCADE)
    *   `UserId` (Guid, FK to `AspNetUsers.Id`, NN, Index, ON DELETE CASCADE)
    *   `CanonicalProductId` (Guid, FK to `Products.ProductId`, NN, Index, ON DELETE CASCADE)
    *   `TriggeredAt` (TIMESTAMPTZ, NN, Index)
    *   `SellerName` (VARCHAR(100), NN)
    *   `TriggeringPrice` (DECIMAL(10,2), NN)
    *   `TriggeringStockStatus` (VARCHAR(50), NN)
    *   `RuleDescription` (VARCHAR(500), NN)
    *   `NotificationSent` (BOOLEAN, NN, DEFAULT FALSE)
    *   `NotificationSentAt` (TIMESTAMPTZ, NULL)
    *   `ProductPageUrl` (VARCHAR(2048), NN)
    *   Navigation Properties: `AlertRule AlertRule`, `ApplicationUser User`, `Product Product`
    *   Index: `(UserId, TriggeredAt DESC)` for user alert history
    *   Index: `(CanonicalProductId, TriggeredAt DESC)` for product alert history

*   **`ScraperRunLog`** (`ScraperRunLogs` table)
    *   `ScraperRunLogId` (Guid, PK)
    *   `MappingId` (Guid, FK to `ProductSellerMappings.MappingId`, NN, Index, ON DELETE CASCADE)
    *   `Status` (VARCHAR(50), NN, Index) - E.g., "STARTED", "SUCCESS", "FAILED", "TIMEOUT"
    *   `StartedAt` (TIMESTAMPTZ, NN, Index)
    *   `CompletedAt` (TIMESTAMPTZ, NULL)
    *   `ErrorMessage` (TEXT, NULL)
    *   `ErrorCode` (VARCHAR(50), NULL, Index)
    *   `HttpStatusCode` (INT, NULL)
    *   `ResponseTimeMs` (INT, NULL)
    *   `ScrapedPrice` (DECIMAL(10,2), NULL)
    *   `ScrapedStockStatus` (VARCHAR(50), NULL)
    *   `UserAgent` (VARCHAR(500), NULL)
    *   `ProxyUsed` (VARCHAR(255), NULL) - Proxy that was used (if any)
    *   `ParentRunId` (Guid, FK to `ScraperRunLogs.ScraperRunLogId`, NULL, Index) - For retry attempts
    *   `RetryAttempt` (INT, NN, DEFAULT 0)
    *   Navigation Properties: `ProductSellerMapping Mapping`, `ScraperRunLog ParentRun`, `ICollection<ScraperRunLog> RetryAttempts`
    *   Index: `(MappingId, StartedAt DESC)`
    *   Index: `(Status, StartedAt DESC)`

---

### 6. Cross-Cutting Concerns

**6.1 Authentication & Authorization:**
    *   **JWT:** Backend API will use JWTs for stateless authentication. `TechTicker.ApiService` will issue tokens on login.
        *   Claims: `sub` (UserId), `email`, `role` (e.g., "Admin", "User"), `jti` (JWT ID), `exp` (Expiration), `iss` (Issuer), `aud` (Audience).
    *   **ASP.NET Core Identity:** Used for user management (storage, password hashing, role management).
    *   **Authorization Policies:**
        *   Admin Role: Define an "Admin" role. Seed an initial admin user.
        *   Protect admin-specific API endpoints using `[Authorize(Roles = "Admin")]`.
        *   Protect user-specific API endpoints (e.g., managing their own alerts) with `[Authorize]`. Ownership checks will be done within the service layer (e.g., user can only modify their own alert rules).
    *   **Frontend Token Handling:** The Angular frontend will store the JWT (e.g., in `localStorage` or `sessionStorage` – consider security implications like XSS) and send it in the `Authorization: Bearer <token>` header for API requests. An HTTP interceptor in Angular will automate this.
    *   **CORS:** Configure CORS policies in `TechTicker.ApiService` to allow requests from the domain where the Angular frontend is hosted (especially important for development with `ng serve` on a different port).

**6.2 Logging:**
    *   **Library:** **Serilog** for structured logging.
    *   **Integration:** Integrated with .NET Aspire's OpenTelemetry support for unified observability.
    *   **Configuration:** Configured in `Program.cs` of all service projects (`ApiService`, workers) potentially via `TechTicker.ServiceDefaults`.
    *   **Sinks:**
        *   Development: Console (visible in Aspire Dashboard), Seq.
        *   Production: File, and a structured logging service (e.g., Elasticsearch via Serilog.Sinks.Elasticsearch, Seq, Splunk, Azure Monitor).
    *   **Format:** Structured logging (JSON preferred for production sinks).
    *   **Key Information to Log:** Timestamp, LogLevel, Message, Exception details (if any), CorrelationId, RequestId, UserId (if authenticated), ServiceName, `ProductId`, `MappingId` where relevant.
    *   **Correlation IDs:** Implement middleware in `ApiService` to generate/propagate a correlation ID (e.g., via `X-Correlation-ID` header). Pass this ID through message queues to workers.
    *   **Aspire Dashboard:** Use for viewing logs, traces, and metrics during development.

**6.3 Configuration:**
    *   **Sources:** `appsettings.json` per project, `appsettings.{Environment}.json` (e.g., `appsettings.Development.json`).
    *   **Secrets Management:**
        *   Development: User Secrets (`dotnet user-secrets set Key Value`).
        *   Production: Environment variables, Azure Key Vault, HashiCorp Vault, or similar.
    *   **.NET Aspire AppHost (`TechTicker.AppHost`):**
        *   Defines and configures resources like PostgreSQL, RabbitMQ.
        *   Injects connection strings and service discovery information into dependent projects during development.
        *   Example `Program.cs` snippet:
            ```csharp
            var builder = DistributedApplication.CreateBuilder(args);

            var postgres = builder.AddPostgres("postgres") // Resource name "postgres"
                                  .WithPgAdmin() // Optional: add PgAdmin service
                                  .AddDatabase("techtickerdb"); // Database name "techtickerdb"

            var rabbitmq = builder.AddRabbitMQ("rabbitmq"); // Resource name "rabbitmq"

            var apiService = builder.AddProject<Projects.TechTicker_ApiService>("apiservice")
                                    .WithReference(postgres) // Inject connection string for "techtickerdb"
                                    .WithReference(rabbitmq); // Inject connection string for RabbitMQ

            var scrapingWorker = builder.AddProject<Projects.TechTicker_ScrapingWorker>("scrapingworker")
                                   .WithReference(postgres)
                                   .WithReference(rabbitmq);

            var notificationWorker = builder.AddProject<Projects.TechTicker_NotificationWorker>("notificationworker")
                                     .WithReference(rabbitmq)
                                     .WithReference(postgres); // If it needs DB access, e.g., to fetch user details not in event

            // Add Angular frontend if serving via Aspire in dev (advanced, or just run 'ng serve' separately)
            // builder.AddNpmApp("angularfrontend", "../TechTicker.Frontend") // Example path to Angular app
            //        .WithHttpEndpoint(targetPort: 4200, port: 4201) // Map ng serve port
            //        .As πολλές φορές(); // If it's a launch setting and not a resource

            builder.Build().Run();
            ```

**6.4 Monitoring & Health Checks:**
    *   **Health Checks:**
        *   Implement ASP.NET Core Health Checks in `ApiService` and worker services (via `TechTicker.ServiceDefaults`).
        *   Checks for: Database connectivity, RabbitMQ connectivity, critical external services (if any).
        *   Expose a health check endpoint (e.g., `/health`).
        *   Viewable in Aspire Dashboard.
    *   **Metrics:**
        *   Use OpenTelemetry via .NET Aspire for collecting metrics (e.g., request rates, error rates, queue lengths, processing times).
        *   Expose metrics endpoint for Prometheus scraping or push to a metrics backend.
        *   Viewable in Aspire Dashboard (e.g., via Prometheus/Grafana integration if set up by Aspire).
    *   **Distributed Tracing:**
        *   Use OpenTelemetry via .NET Aspire for end-to-end tracing across services and message queues.
        *   Viewable in Aspire Dashboard (e.g., via Jaeger/Zipkin integration if set up by Aspire).

**6.5 Error Handling Strategy:**
    *   **API Layer (`TechTicker.ApiService`):**
        *   Global exception handling middleware: Catches unhandled exceptions.
        *   Returns standardized `ProblemDetails` (RFC 7807) responses.
        *   For validation errors, return `400 Bad Request` with `ValidationProblemDetails`.
    *   **Worker Services:**
        *   Robust try-catch blocks within message consumers.
        *   **Retry Policies:** Use Polly for transient errors (e.g., network issues, temporary database unavailability). Implement exponential backoff.
        *   **Dead Letter Queues (DLQ):** For messages that consistently fail processing after retries, `nack` them without requeueing to send them to a DLQ configured on RabbitMQ. Monitor DLQs.
    *   **Scraping Failures:** Specific error codes in `ScrapingResultEvent` (e.g., "CAPTCHA_DETECTED", "HTTP_ERROR_403", "PARSING_ERROR") to allow for targeted responses or adjustments by the `ScrapingOrchestrationModule` (e.g., backoff for a specific site or mapping).

**6.6 Scraper Evasion Techniques (Within Constraints - no paid services, no advanced browser automation initially)**
*   **User-Agent Rotation:**
    *   Configuration: `Scraping:UserAgents: ["UA_Chrome_Desktop_Win", "UA_Firefox_Desktop_Mac", ...]` (list of realistic, common UAs).
    *   Implementation: `ScrapingOrchestrationModule` randomly selects a User-Agent from the configured list for each `ScrapeProductPageCommand`. The `ScraperModule` uses this UA.
*   **HTTP Header Management:**
    *   Configuration: `Scraping:DefaultHeaders: { "Accept-Language": "en-US,en;q=0.9", "Accept": "text/html,application/xhtml+xml,..." }`.
    *   Implementation: `ScrapingOrchestrationModule` can add these default headers. `ScraperSiteConfiguration` can also define site-specific headers that override or add to defaults.
*   **Request Timing and Delays (Jitter):**
    *   `ScrapingOrchestrationModule`: Uses `Scraping:DefaultFrequency` (e.g., "PT6H") and `Scraping:FrequencyJitterPercentage` (e.g., 0.1 for +/- 10%) to randomize `NextScrapeAt`.
    *   (Optional) `ScraperModule`: Can add a small, random pre-request delay (e.g., 1-5 seconds, configurable: `Scraper:PreRequestDelayMsRange: [1000, 5000]`) before making the HTTP call.
*   **Basic Cookie Handling:**
    *   `ScraperModule`: Use `HttpClientHandler.UseCookies = true` and `CookieContainer = new CookieContainer()` per `HttpClient` instance (or per domain if managing cookies more granularly, though `HttpClientFactory` often manages handler lifetimes). This allows cookies set by the server to be sent back on subsequent requests to the same domain during a "session" (if the handler is reused appropriately).
*   **Referrer Policy:** Consider setting a common referrer if it helps, or stripping it. `Scraping:DefaultHeaders: { "Referrer-Policy": "no-referrer-when-downgrade" }`.
*   **Avoiding Obvious Bot Patterns:**
    *   Vary request patterns slightly (handled by jitter).
    *   Ensure headers mimic real browsers.
*   **Limitations:**
    *   No IP Rotation (would require proxies).
    *   No sophisticated JavaScript challenge solving (would require browser automation like Playwright/Puppeteer).
    *   Limited CAPTCHA handling (detection and backoff, not solving).
    *   Vulnerable to advanced bot detection systems. The goal is "reduced detectability," not "undetectable."

---

### 7. Frontend Application (Angular)

**7.1 Purpose & Target Audience**
The Angular frontend serves as a comprehensive interface for both administrators and users to manage the TechTicker system. It provides a modern, responsive GUI for product catalog browsing, alert management, notification settings, and administrative functions.

**Target Audience:**
- **Administrators:** Full system management capabilities
- **Moderators:** Content management and moderation
- **Users:** Product browsing, alert management, and notification preferences

**7.2 Key Features & Modules**
The Angular application is fully implemented with a modular structure using lazy-loaded feature modules and comprehensive RBAC integration.

**Implemented Core Features:**
*   **Shared Module:** Contains common components, services, and utilities
    *   `AuthService` with full RBAC support (role and permission checking)
    *   `AuthGuard`, `AdminGuard`, and `RoleGuard` for route protection
    *   HTTP Interceptors (`AuthInterceptor`, `ErrorInterceptor`) for token management and error handling
    *   `AppLayoutComponent` with responsive sidebar navigation and admin menu
    *   `ImageGalleryComponent` for product image display with lazy loading and optimization
    *   NSwag-generated TypeScript API client for type-safe backend communication
    *   RBAC module with permission-based component visibility

**Implemented Feature Modules:**

*   **Auth Module:** (Lazy-loaded)
    *   `LoginComponent` with JWT authentication and enhanced UX
    *   Automatic token refresh and session management
    *   Role-based redirection after login with dashboard routing

*   **Dashboard Module:** (Protected by AuthGuard)
    *   System overview with real-time statistics and comprehensive metrics
    *   Quick access to key management sections with role-based visibility
    *   Performance metrics and system health indicators with charts
    *   Recent activity feeds and system alerts with notifications

*   **Catalog Module:** (Public product browsing)
    *   `ProductCatalogComponent` with grid/list view toggle
    *   `ProductCardComponent` with image gallery integration
    *   Advanced filtering by category, price range, and availability
    *   Product detail view with price history charts
    *   Integration with alert creation

*   **Categories Module:** (Admin/Moderator access)
    *   `CategoriesListComponent` with full CRUD operations
    *   `CategoryFormComponent` for creating/editing categories
    *   Bulk operations and category management
    *   Real-time validation and error handling

*   **Products Module:** (Admin/Moderator access)
    *   `ProductsListComponent` with advanced filtering and sorting
    *   `ProductFormComponent` with image upload and management
    *   `PriceHistoryComponent` with interactive charts
    *   Bulk import/export capabilities
    *   Product image gallery management
*   **Mappings Module:** (User/Admin/Moderator access)
    *   `MappingsListComponent` with comprehensive mapping management
    *   `MappingFormComponent` for creating/editing product-seller mappings
    *   Real-time scraping status and error monitoring
    *   Bulk operations for mapping management
    *   Integration with site configuration selection

*   **Site Configs Module:** (Admin access only)
    *   `SiteConfigsListComponent` for managing scraper configurations
    *   `SiteConfigFormComponent` with CSS selector testing
    *   Domain-based configuration management
    *   Scraper testing and validation tools

*   **Users Module:** (Admin access only)
    *   `UsersListComponent` with comprehensive user management
    *   `UserFormComponent` for creating/editing users and roles
    *   Role assignment and permission management
    *   User activity monitoring and statistics

*   **Alerts Module:** (User/Admin access)
    *   `AlertsListComponent` for personal alert management
    *   `AlertFormComponent` with advanced condition configuration
    *   `AlertPerformanceComponent` for admin monitoring
    *   Real-time alert status and performance metrics
    *   Integration with product selection and notification preferences

*   **Notification Settings Module:** (User/Admin access)
    *   `NotificationSettingsComponent` for Discord webhook configuration
    *   Product selection for notifications (up to 5 products)
    *   Custom bot name and avatar configuration
    *   Webhook testing and validation
    *   Notification preferences summary and management

*   **Proxy Management Module:** (Admin access only)
    *   `ProxyListComponent` with virtual scrolling for large datasets
    *   `BulkImportComponent` with drag-and-drop CSV/text import
    *   `ProxyFormComponent` for individual proxy configuration
    *   `ProxyHealthDashboard` for real-time health monitoring
    *   Bulk operations: enable/disable, test, delete with progress indicators
    *   Advanced filtering and search capabilities
    *   Performance analytics and usage statistics

*   **Scraper Logs Module:** (Admin access only)
    *   `ScraperLogsComponent` for monitoring scraping activities
    *   Real-time log viewing and filtering with enhanced search
    *   Error analysis and performance monitoring with charts
    *   Proxy usage tracking and correlation analysis
    *   Scraper run statistics and trends

**7.3 UI/UX Considerations**
*   **Responsive Design:** The application should be usable on desktop and tablet devices. Use a responsive grid system (from UI library or custom).
*   **Intuitive Navigation:** Clear and consistent navigation structure (e.g., sidebar for main sections, breadcrumbs).
*   **User Feedback:**
    *   Loading indicators (spinners, progress bars) for API calls.
    *   Success/error messages/toasts (e.g., using Angular Material Snackbar or PrimeNG Toast).
    *   Clear visual cues for interactive elements.
*   **Form Validation:**
    *   Client-side validation using Angular Reactive Forms for immediate feedback.
    *   Display clear error messages next to form fields.
    *   Server-side validation errors should also be displayed appropriately.
*   **Accessibility (A11y):** Adhere to WCAG AA standards where feasible (semantic HTML, ARIA attributes, keyboard navigation, color contrast).
*   **Data Tables:** Use robust data table components from the chosen UI library (e.g., Angular Material Table, PrimeNG Table) with features like sorting, filtering, and pagination for managing lists of items.
*   **Consistency:** Maintain consistent design language, component usage, and interaction patterns throughout the application.

**7.4 Interaction with Backend API**
*   The Angular frontend will consume the RESTful API endpoints exposed by `TechTicker.ApiService`.
*   An Angular service (e.g., `ApiService` or feature-specific services like `ProductService`, `CategoryService`) will encapsulate `HttpClient` calls.
*   **Authentication:**
    *   `AuthService` will handle login/logout.
    *   On successful login, the JWT received from the backend is stored securely (e.g., `localStorage` for persistence across browser sessions, or `sessionStorage` for session-only. Be mindful of XSS; `HttpOnly` cookies are more secure but require backend setup and are harder to manage with SPAs for token renewal).
    *   A `JwtInterceptor` (HTTP interceptor) will automatically attach the JWT to the `Authorization` header of outgoing API requests.
*   **Error Handling:**
    *   An `ErrorInterceptor` (HTTP interceptor) will catch HTTP errors globally.
        *   For `401 Unauthorized` (e.g., invalid/expired token), it can redirect to the login page or attempt token refresh if implemented.
        *   For `403 Forbidden`, display an appropriate message.
        *   For other errors (e.g., `500 Internal Server Error`), display a generic error message.
    *   Component-level error handling for specific API calls where custom logic is needed.
*   **State Management:**
    *   For simple state, services with RxJS `BehaviorSubject` can be used.
    *   For complex state or many interconnected components, consider NgRx (Redux pattern) or Akita (simpler object-oriented store). This helps manage data consistency and makes the application more predictable. Example: store lists of products, categories, current user info.

**7.5 Build and Deployment**
*   **Development Server:** `ng serve` (uses webpack-dev-server with live-reloading).
*   **Build:** Use Angular CLI (`ng build --configuration production`) to create optimized static assets (HTML, CSS, JavaScript bundles with tree-shaking, minification, AOT compilation).
*   **Deployment (Static Assets):**
    *   The output from `ng build` (typically in a `dist/project-name/browser` folder) contains static files.
    *   These files can be served by any static web server (Nginx, Apache, Caddy) or a cloud storage service configured for static website hosting (AWS S3 + CloudFront, Azure Blob Storage + CDN, Google Cloud Storage).
    *   Ensure the web server is configured to handle SPA routing (i.e., redirect all 404s to `index.html`).
*   **Environment Configuration:** Angular's `src/environments/` folder (`environment.ts`, `environment.prod.ts`, etc.) is used to manage environment-specific variables like the API base URL. `ng build --configuration production` will use `environment.prod.ts`.

**7.6 Image Processing & Gallery Features**
The frontend includes comprehensive image handling capabilities:

*   **Image Gallery Component:** (`ImageGalleryComponent`)
    *   Displays primary and additional product images
    *   Lazy loading for performance optimization
    *   Responsive design with configurable dimensions
    *   Thumbnail navigation for multiple images
    *   Fallback handling for missing images
    *   Integration with product cards and detail views

*   **Image Upload and Management:**
    *   Drag-and-drop image upload interface
    *   Multiple image selection and preview
    *   Image validation (format, size, dimensions)
    *   Progress indicators for upload operations
    *   Image reordering and deletion capabilities

*   **Image Storage Integration:**
    *   Local directory-based image storage
    *   Optimized image serving with caching
    *   Automatic image format detection
    *   URL-based image referencing system

**7.7 Notification Settings Interface**
Comprehensive user notification management:

*   **Discord Integration:**
    *   Discord webhook URL configuration and validation
    *   Real-time webhook testing functionality
    *   Custom bot name and avatar configuration
    *   Rich embed notification previews

*   **Product Selection:**
    *   Interactive product selection (up to 5 products)
    *   Product search and filtering capabilities
    *   Visual product cards with current pricing
    *   Easy addition/removal of notification products

*   **Preferences Management:**
    *   Enable/disable notification toggles
    *   Notification frequency settings
    *   Summary dashboard showing current configuration
    *   Validation and error handling for all settings

---

### 8. Deployment Considerations

**8.1 Backend Deployment**
*   **Containerization:**
    *   .NET Aspire assists in generating Dockerfiles for `ApiService` and worker services.
    *   Build Docker images for each service (e.g., using a CI/CD pipeline).
    *   Push images to a container registry (Docker Hub, Azure Container Registry, AWS ECR, Google Container Registry).
*   **Orchestration (Production):**
    *   .NET Aspire can output Kubernetes manifests or a Docker Compose file suitable for development/testing, which can be adapted for production.
    *   Kubernetes (K8s) is a common choice for orchestrating containerized applications at scale.
    *   Alternatives: Azure Container Apps, AWS ECS, Google Cloud Run, Docker Swarm.
*   **Configuration in Production:**
    *   Pass configuration (connection strings, API keys, RabbitMQ credentials, JWT settings) via environment variables to containers.
    *   Use secrets management tools (Azure Key Vault, AWS Secrets Manager, HashiCorp Vault) integrated with the orchestrator.
*   **Database Migrations:**
    *   Apply EF Core migrations as a step in the CI/CD pipeline (recommended for control).
    *   Or, as an entry point script in the `ApiService` container (less ideal for production, can cause issues with multiple instances starting simultaneously).
    *   Tools like `DbUp` can also be used for more robust deployment-time migrations.
*   **Scaling:**
    *   `TechTicker.ApiService`: Scale horizontally by increasing the number of container instances behind a load balancer.
    *   Worker Services (`TechTicker.ScrapingWorker`, `TechTicker.NotificationWorker`): Scale horizontally by running multiple instances. RabbitMQ will distribute messages among consumers on the same queue.
*   **Logging & Monitoring (Production):**
    *   Aggregate logs from all containers into a centralized logging system (ELK stack, Splunk, Datadog, Azure Monitor Logs).
    *   Monitor metrics using Prometheus/Grafana, Datadog, Azure Monitor Metrics, or similar, integrated with your orchestrator.
    *   Set up alerts for critical errors or performance degradation.

**8.2 Frontend Deployment**
*   **Build:** The Angular application is built into static files (HTML, CSS, JS bundles) using `ng build --configuration production`.
*   **Serving Static Files:**
    *   **Option 1 (Dedicated Web Server/CDN - Recommended):**
        *   Deploy the static files from the `dist/project-name/browser` folder to a web server like Nginx or Apache.
        *   Configure the web server to serve the `index.html` file for any routes that don't match a static file (to support Angular's client-side routing). Example Nginx config: `try_files $uri $uri/ /index.html;`
        *   For better performance and global availability, use a Content Delivery Network (CDN) like AWS CloudFront, Azure CDN, Cloudflare, Akamai, pointing to the origin server or a cloud storage bucket.
    *   **Option 2 (Serve from ASP.NET Core - Simpler for small setups):**
        *   Configure the `TechTicker.ApiService` (ASP.NET Core application) to serve the static files of the Angular application.
        *   Use `app.UseSpaStaticFiles()` and `app.UseSpa()` middleware or simply `app.UseDefaultFiles()` and `app.UseStaticFiles()` pointing to the Angular build output.
        *   This can simplify the deployment if a separate web server for the frontend is not desired, but may not be as performant or scalable as a dedicated CDN/web server setup.
*   **CORS:** Ensure the backend (`TechTicker.ApiService`) is configured with appropriate CORS policies to allow requests from the domain(s) where the Angular frontend is served (e.g., `https://admin.techticker.com`).
*   **Environment Configuration:**
    *   Angular's environment files (`environment.ts`, `environment.prod.ts`) are used during the build process to embed configuration like the API base URL.
    *   If runtime configuration of the API URL is needed (e.g., deploying the same frontend build to different environments pointing to different APIs), a common technique is to have a `config.json` file in the `assets` folder that is loaded by Angular at startup. This `config.json` can be modified post-build or by the deployment pipeline.
*   **Containerization (Optional):** The Nginx (or other web server serving the Angular app) can itself be run in a Docker container for consistency with the backend deployment.

---

### 9. Implementation Status (Completed)

**✅ All Phases Completed Successfully**

The TechTicker system has been fully implemented and is operational. All planned features have been delivered with additional enhancements:

**Completed Core Features:**
1.  **✅ Backend Foundation:** Complete .NET 9.0 Aspire setup with all core services
2.  **✅ Database Layer:** Full EF Core implementation with PostgreSQL and comprehensive migrations
3.  **✅ API Layer:** Comprehensive REST API with OpenAPI documentation and NSwag integration
4.  **✅ Authentication & Authorization:** Full RBAC system with JWT and permission-based access
5.  **✅ Scraping System:** Complete web scraping with orchestration, monitoring, and proxy support
6.  **✅ Messaging Infrastructure:** RabbitMQ integration with reliable message processing and error handling
7.  **✅ Alert System:** Advanced alert rules with evaluation, triggering, and performance monitoring
8.  **✅ Notification System:** Discord notifications with comprehensive user preferences and customization
9.  **✅ Frontend Application:** Complete Angular 20 SPA with all admin features and responsive design
10. **✅ Proxy Management:** Comprehensive proxy configuration, health monitoring, and bulk operations
11. **✅ Testing Infrastructure:** Extensive unit and integration tests with 230+ test cases
12. **✅ Performance Monitoring:** Real-time metrics, analytics, and system health monitoring
10. **✅ User Management:** Full CRM capabilities with role management

**Additional Features Implemented:**
*   **Image Processing:** Complete image upload, storage, and gallery system
*   **Performance Monitoring:** Comprehensive logging and monitoring capabilities
*   **Scraper Run Logging:** Detailed scraping activity tracking and analysis
*   **Advanced RBAC:** Permission-based authorization beyond basic roles
*   **Product Catalog:** Public-facing product browsing with price history
*   **Notification Preferences:** User-configurable Discord webhook settings
*   **Testing Infrastructure:** Comprehensive unit and integration test coverage
*   **NSwag Integration:** Automatic TypeScript client generation

**Current System Capabilities:**
*   **Fully Operational:** All services running and communicating properly with high availability
*   **Production Ready:** Containerized with Docker and Aspire orchestration for seamless deployment
*   **Scalable Architecture:** Horizontal scaling support for all components with load balancing
*   **Comprehensive Monitoring:** Full observability with logging, metrics, and real-time dashboards
*   **User-Friendly Interface:** Modern Angular 20 frontend with responsive design and accessibility
*   **Robust Error Handling:** Comprehensive error handling, recovery mechanisms, and graceful degradation
*   **Advanced Proxy Support:** Intelligent proxy rotation, health monitoring, and bulk management
*   **Performance Optimized:** Virtual scrolling, lazy loading, and optimized database queries
*   **Comprehensive Testing:** 230+ test cases covering all critical business logic and workflows
*   **Security Hardened:** RBAC authorization, encrypted credentials, and secure communication

---

### 10. Testing Infrastructure

**Comprehensive Test Coverage:**
The TechTicker application includes a robust testing infrastructure with over 230 test cases covering all critical components and business logic.

**Test Projects:**
*   **TechTicker.Domain.Tests** (131 tests)
    *   Entity validation and business rules
    *   Computed properties and domain logic
    *   Data integrity and constraints
    *   Complete coverage of all domain entities

*   **TechTicker.Application.Tests** (65+ tests)
    *   Service layer business logic
    *   Mapping and transformation logic
    *   Alert processing and evaluation
    *   Messaging and event handling
    *   Proxy management and health monitoring

*   **TechTicker.ScrapingWorker.Tests** (36 tests)
    *   Web scraping functionality
    *   Data processing and normalization
    *   Worker service orchestration
    *   Error handling and retry logic

**Testing Patterns and Practices:**
*   **Unit Testing:** Isolated testing of individual components with mocking
*   **Integration Testing:** End-to-end testing of service interactions
*   **Repository Testing:** In-memory database testing for data access
*   **Message Testing:** RabbitMQ message processing validation
*   **Error Scenario Testing:** Comprehensive error handling validation
*   **Performance Testing:** Load testing for critical operations

**Test Infrastructure Features:**
*   **Automated Test Execution:** Continuous integration with automated test runs
*   **Code Coverage Analysis:** Comprehensive coverage reporting and metrics
*   **Test Data Management:** Fixture-based test data with cleanup
*   **Mocking Framework:** Extensive use of mocks for isolated testing
*   **Assertion Libraries:** Rich assertion patterns for validation
*   **Test Documentation:** Clear test naming and documentation standards

**Quality Assurance:**
*   **High Test Coverage:** 80%+ code coverage across all projects
*   **Regression Protection:** Comprehensive test suite prevents regressions
*   **Continuous Validation:** Automated testing in CI/CD pipeline
*   **Performance Benchmarks:** Performance testing for critical paths
*   **Error Scenario Coverage:** Extensive testing of failure conditions

### 11. Non-Functional Requirements

*   **Scalability:**
    *   Backend: `ApiService` and Worker Services must be horizontally scalable. Design for statelessness where possible.
    *   Database: PostgreSQL should be configurable for read replicas if high read load is anticipated in the future.
    *   Frontend: Being static assets, scales with the serving infrastructure (CDN/web server).
*   **Reliability/Availability:**
    *   Aim for >99.9% uptime for core services.
    *   Robust error handling, retries, and dead-lettering in message queues.
    *   Graceful degradation if parts of the system (e.g., a specific scraper site) fail.
*   **Maintainability:**
    *   Backend: Clean code, well-defined modules within .NET Aspire, good separation of concerns, comprehensive unit and integration tests.
    *   Frontend: Modular Angular architecture, component-based design, consistent coding standards, unit tests (Jest/Karma) and E2E tests (Cypress/Protractor).
    *   Clear documentation.
*   **Security:**
    *   Backend: Secure APIs (HTTPS, JWT auth, role-based authorization), protection against common vulnerabilities (SQLi, XSS via output encoding if serving HTML, CSRF if using cookies for auth - JWT in header helps mitigate). Input validation.
    *   Frontend: Protection against XSS (Angular's built-in sanitization helps, but be careful with `[innerHTML]`), secure JWT storage, HTTPS. No sensitive logic on the client side.
    *   Regular dependency updates to patch vulnerabilities.
*   **Performance:**
    *   API Response Times: P95 for most GET requests < 500ms (excluding external scraping time).
    *   Scraping: Efficient parsing, respect for target site resources (avoid overwhelming sites).
    *   Frontend Load Time: Initial load (Largest Contentful Paint) < 3 seconds on a decent connection. Smooth UI interactions.
    *   Database: Optimized queries, appropriate indexing.
*   **Stealthiness (Scraping):** Minimize detection by target websites within the specified constraints (e.g., avoid rapid, aggressive scraping from a single IP).
*   **Usability (Frontend):**
    *   Admin portal must be intuitive for non-technical or semi-technical administrators.
    *   Efficient workflows for common tasks (e.g., adding a new product and its mappings).
    *   Clear feedback and error messaging.
*   **Data Integrity:** Ensure accuracy and consistency of data (prices, product info, user alerts).
*   **Extensibility:** Architecture should allow for future enhancements (e.g., new notification channels, advanced scraping techniques) without major refactoring.

---

### 11. Future Enhancements

**Note:** The following features represent potential future enhancements beyond the current fully implemented system:

*   **Hierarchical Categories:** Support for nested product categories
*   **Category-Specific Alert Rules:** Allow users to set alerts for entire categories (e.g., "any GPU below $300")
*   **Advanced Alert Conditions:** More complex rules (e.g., "price dropped by X% AND is in stock")
*   **Additional Notification Channels:** SMS, Push Notifications, Slack integration
*   **Browser Automation Integration:** Playwright/Puppeteer for JavaScript-heavy sites
*   **Dynamic Evasion Strategy Adjustment:** ML-based scraper optimization
*   **ML for Selector Maintenance:** Automatic selector updates when sites change
*   **User-Contributed Mappings:** Community-driven product mapping submissions
*   **Affiliate Link Integration:** Automatic affiliate tag appending
*   **Comparative Analysis Features:** Product comparison and trend analysis
*   **Internationalization:** Multi-language and region support
*   **~~Proxy Integration~~:** ✅ **COMPLETED** - Comprehensive proxy management system implemented
*   **Real-time WebSocket Updates:** Live price updates and notifications in the UI
*   **Advanced Reporting:** Comprehensive reporting and export capabilities
*   **Mobile Application:** Native mobile app for iOS and Android with push notifications
*   **AI-Powered Features:** Machine learning for price prediction and trend analysis

---

### 12. Current System Summary

**TechTicker v2.1** is a fully operational e-commerce price tracking and alerting system with the following key characteristics:

**✅ Implemented Features:**
- Complete .NET 9.0 backend with Aspire 9.3.1 orchestration
- Angular 20 frontend with comprehensive admin interface and responsive design
- PostgreSQL database with full EF Core integration and optimized queries
- RabbitMQ messaging for reliable background processing with error handling
- JWT-based authentication with comprehensive RBAC authorization system
- Web scraping with advanced anti-detection techniques and proxy support
- Discord notification system with rich user preferences and customization
- Product catalog with image gallery support and virtual scrolling
- Real-time price monitoring and alerting with performance analytics
- Comprehensive proxy management with bulk operations and health monitoring
- Advanced testing infrastructure with 230+ test cases and high coverage
- Performance monitoring with real-time dashboards and metrics
- Bulk import/export functionality with progress indicators
- Virtual scrolling optimization for large datasets
- Comprehensive logging, monitoring, and observability
- Docker containerization ready for production deployment

**🎯 System Capabilities:**
- **Multi-user Support:** Admin, Moderator, and User roles
- **Product Management:** Categories, products, and seller mappings
- **Price Tracking:** Automated scraping with configurable frequency
- **Alert System:** Flexible alert rules with Discord notifications
- **Image Processing:** Upload, storage, and gallery display
- **Performance Monitoring:** Detailed logging and scraper analytics
- **Responsive UI:** Modern Angular interface with Material Design

**🚀 Production Ready:**
- Fully tested with comprehensive test coverage (230+ tests)
- Scalable architecture with horizontal scaling support
- Robust error handling and recovery mechanisms
- Security hardened with RBAC and input validation
- Monitoring and observability built-in
- Documentation complete and up-to-date

---

## 📝 Documentation Update Summary

**Last Updated:** June 28, 2025
**Version:** 2.1
**Status:** Current and Accurate

This documentation has been updated to reflect the current state of the TechTicker application as of June 2025. All features described are fully implemented and operational. Key updates include:

**✅ Recently Completed Features:**
- Comprehensive proxy management system with bulk operations
- Advanced testing infrastructure with 230+ test cases covering all critical components
- Real-time performance monitoring and analytics dashboards
- Virtual scrolling optimization for large datasets
- Enhanced Discord notification system with rich customization
- Bulk import/export functionality with progress indicators
- Upgraded to .NET 9.0 and Angular 20 with latest dependencies

**🔧 Technical Improvements:**
- Enhanced error handling and recovery mechanisms
- Optimized database queries and performance
- Improved security with comprehensive RBAC implementation
- Advanced proxy health monitoring and rotation
- Production-ready containerization with Docker

The application is actively maintained and continues to evolve with new features and improvements. All documentation reflects the actual implemented state of the system.