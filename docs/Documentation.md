Okay, here is the complete, updated TechTicker documentation including the Angular frontend for management and CRM.

## TechTicker: E-commerce Price Tracker & Alerter - Detailed Software Specification

**Version:** 1.6
**Date:** June 15, 2025
**System Model:** Admin-Managed Exact Product URLs (Focus on Reduced Detectability, with Product Categories)
**Architecture Model:** Monolith with .NET Aspire Backend & Angular SPA Frontend
**Target Audience:** Development Team / Automated Coding Agent

**Preamble for Coding Agent:**
This document aims to provide a highly detailed specification for building the TechTicker application, including its backend services and a dedicated Angular frontend for administration and CRM. While it strives for completeness, some low-level implementation details (e.g., specific UI element interactions or exhaustive edge-case error handling beyond what's specified) might still require standard best practices or further clarification. Focus on implementing the described logic, data structures, and interactions precisely. Use the recommended technologies and libraries. Pay close attention to data validation, error handling, and logging as outlined.

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
7.  Frontend Application (Angular)
    *   7.1 Purpose & Target Audience
    *   7.2 Key Features & Modules
    *   7.3 UI/UX Considerations
    *   7.4 Interaction with Backend API
    *   7.5 Build and Deployment
8.  Deployment Considerations
    *   8.1 Backend Deployment
    *   8.2 Frontend Deployment
9.  Development Roadmap (Summary)
10. Non-Functional Requirements
11. Future Enhancements

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
*   **Application Framework:** .NET 8+ with ASP.NET Core (for the main web application/API) and .NET Worker Services (for background tasks).
*   **Orchestration & Development:** **.NET Aspire**.
*   **Database:** PostgreSQL. Use **Npgsql** as the ADO.NET provider. Use **Entity Framework Core (EF Core)** as the ORM.
*   **Message Broker:** RabbitMQ. Use the **RabbitMQ.Client** .NET library.
*   **Web Scraping Library:** **HtmlAgilityPack** for HTML parsing. `HttpClientFactory` for making HTTP requests.
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

**2.2.2 Frontend Technology Stack**
*   **Framework:** Angular (latest stable version, e.g., Angular 17+).
*   **Language:** TypeScript.
*   **State Management:** NgRx or Akita (choose one, e.g., NgRx for robust, scalable solutions). Alternatively, Angular services with RxJS BehaviorSubjects for simpler state.
*   **UI Component Library:** Angular Material or a similar comprehensive library (e.g., PrimeNG) for rapid UI development and consistency.
*   **HTTP Client:** Angular's built-in `HttpClientModule`.
*   **Routing:** Angular Router.
*   **Forms:** Angular Reactive Forms.
*   **Styling:** SCSS or CSS-in-JS (Styled Components if a suitable Angular library exists, otherwise SCSS is standard).
*   **Build Tool:** Angular CLI.
*   **Testing:** Jest or Karma/Jasmine for unit tests, Cypress or Protractor (if still supported/preferred) for E2E tests.

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
*   **Purpose:** Manages user accounts and authentication for API access.
*   **Implementation:** Uses ASP.NET Core Identity, configured in `TechTicker.ApiService`. `ApplicationUser` class inheriting from `IdentityUser<Guid>`.
*   **API Endpoints (exposed by `TechTicker.ApiService`):**
    *   `POST /api/auth/register` (Consider if self-registration is allowed or if admins create users)
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
    *   **Admin User Management Endpoints (New/Enhanced for Admin Frontend):**
        *   `GET /api/admin/users` (Admin Only)
            *   Response (`200 OK`): Paginated list of users `{ "items": [user objects], "pageNumber": ..., "pageSize": ..., "totalItems": ..., "totalPages": ... }`. User object should include `userId`, `email`, `firstName`, `lastName`, `roles`, `createdAt`.
        *   `GET /api/admin/users/{userId}` (Admin Only)
            *   Response (`200 OK`): Single detailed user object.
        *   `PUT /api/admin/users/{userId}` (Admin Only)
            *   Request Body: `{ "firstName": "...", "lastName": "...", "email": "...", "roles": ["Admin", "User"] }` (fields for update).
            *   Response (`200 OK`): Updated user object.
        *   `POST /api/admin/users` (Admin Only - for creating users by admin)
            *   Request Body: `{ "email": "...", "password": "...", "firstName": "...", "lastName": "...", "roles": ["User"] }`
            *   Response (`201 Created`): New user object.
        *   `DELETE /api/admin/users/{userId}` (Admin Only)
            *   Response (`204 No Content`). (Consider soft delete or deactivation).
*   **Security Details:** Password Hashing (ASP.NET Core Identity). JWT Claims: `sub` (UserId), `email`, `role`, `jti`, `exp`, `iss`, `aud`. Ensure `role` claim is populated correctly based on `AspNetUserRoles`.
*   **Configuration Keys (appsettings.json for `TechTicker.ApiService`):**
    *   `Jwt:Key`, `Jwt:Issuer`, `Jwt:Audience`, `Jwt:DurationInMinutes`.

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
*   **Purpose:** Sends notifications (email).
*   **Implementation:** Message consumer (`AlertNotificationConsumer`) in `TechTicker.NotificationWorker` handles `AlertTriggeredEvent`. Use **MailKit** for SMTP.
*   **Logic (on receiving `AlertTriggeredEvent` `ate`):**
    1.  Log reception.
    2.  Fetch user's email for `ate.UserId` (this should ideally be part of the `AlertTriggeredEvent` payload to avoid another DB lookup, or the event can contain `UserEmail`). Let's assume `UserEmail` is in the event.
    3.  Format email:
        *   Subject: `TechTicker Price Alert: {ate.ProductName}`
        *   Body:
            ```
            Hi [User's First Name, if available, else User],

            A price alert you set for "{ate.ProductName}" has been triggered!

            Product: {ate.ProductName}
            Category: {ate.ProductCategoryName}
            Seller: {ate.SellerName}
            Alert Condition: {ate.RuleDescription}
            Current Price: ${ate.TriggeringPrice}
            Stock Status: {ate.TriggeringStockStatus}

            View Product: {ate.ProductPageUrl} (this should be the exact product URL from ProductSellerMapping if available, or a link to the product on TechTicker if we build a user-facing site)

            Thank you,
            The TechTicker Team
            ```
    4.  Send email using MailKit and SMTP settings from configuration.
    5.  Log success/failure. Implement retry logic for transient SMTP errors (e.g., using Polly within the consumer). If retries fail, move to Dead Letter Queue.
*   **Configuration Keys (appsettings.json for `TechTicker.NotificationWorker`):**
    *   `Smtp:Host`, `Smtp:Port`, `Smtp:Username`, `Smtp:Password`, `Smtp:FromAddress`, `Smtp:FromName` (e.g., "TechTicker Alerts"), `Smtp:UseSsl`.

**3.11 API Layer**
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

*   **Exchanges and Message Payloads:**

    *   **`scraping_commands_exchange` (Direct Exchange)**
        *   Purpose: Routes scraping commands to the scraper worker.
        *   Queue: `scraping_commands_queue` (bound with routing key `scrape.product.page`)
        *   Message: `ScrapeProductPageCommand` (Published by `ScrapingOrchestrationModule`)
            *   Routing Key: `scrape.product.page`
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
                  "scrapedProductName": "string, optional" // Product name as seen on the page
                }
                ```
        *   Message: `PricePointRecordedEvent` (Published by `PriceNormalizationIngestionModule` after validation and normalization)
            *   Routing Key: `price.data.recorded.{canonicalProductId}`
            *   Payload:
                ```json
                {
                  "canonicalProductId": "uuid",
                  "sellerName": "string",
                  "price": 123.45, // Decimal as number, normalized
                  "stockStatus": "string, e.g., IN_STOCK, OUT_OF_STOCK, PREORDER, UNKNOWN", // Normalized
                  "sourceUrl": "string (URL)",
                  "timestamp": "datetimeoffset (ISO 8601)" // Time of scrape (can be same as RawPriceDataEvent or slightly later)
                }
                ```
    *   **`alert_notifications_exchange` (Topic Exchange)**
        *   Purpose: Triggers notifications when alerts are met.
        *   Message: `AlertTriggeredEvent` (Published by `AlertEvaluationModule`)
            *   Routing Key: `alert.triggered.{userId}.{productId}` (e.g., `alert.triggered.uuid-user.uuid-product`)
            *   Payload:
                ```json
                {
                  "alertRuleId": "uuid",
                  "userId": "uuid", // User who owns the alert
                  "userEmail": "string", // User's email for notification
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
---

### 5. Core Data Models (PostgreSQL with EF Core)

**Implementation Notes:**
*   Use EF Core Fluent API in `OnModelCreating` method of the `DbContext` for precise configuration (indexes, constraints, relationships, column types).
*   Generate and apply migrations (`dotnet ef migrations add InitialCreate`, `dotnet ef database update`).
*   All `Id` fields will be `Guid` and serve as Primary Keys, typically auto-generated by the database or EF Core (`ValueGeneratedOnAdd()`).
*   `CreatedAt` and `UpdatedAt` timestamps should be `DateTimeOffset` (to store timezone info, or `DateTime` mapped to `TIMESTAMPTZ` and always store as UTC). Managed automatically (e.g., via interceptors or overriding `SaveChanges`).
*   Define appropriate indexes for frequently queried columns and foreign keys.

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
The Angular frontend serves as the primary interface for administrators to manage the TechTicker system and perform CRM-like functions. It provides a user-friendly GUI to interact with the backend API for tasks such as product catalog management, scraper configuration, user oversight, and monitoring alert activities.

**Target Audience:** System Administrators.

**7.2 Key Features & Modules**
The Angular application will be structured into modules for better organization and lazy loading where appropriate.

*   **Core Module:** (Imported once in AppModule)
    *   Authentication services (`AuthService` for login, logout, token management, checking auth state).
    *   `AuthGuard` for protecting admin routes.
    *   HTTP Interceptors (e.g., `JwtInterceptor` to add auth token, `ErrorInterceptor` for global error handling like 401/403).
    *   Navigation components (e.g., `SidebarComponent`, `NavbarComponent`).
    *   Shared UI components (e.g., custom loaders, confirmation dialogs).
    *   Core services (e.g., `NotificationService` for toasts/snackbars).

*   **Auth Module:** (Potentially lazy-loaded)
    *   Login page/component (`LoginComponent`).
    *   (Optional) Forgot password/reset password components if implemented in backend.

*   **Admin Module:** (Main lazy-loaded module, protected by `AuthGuard`)
    *   **AdminLayoutComponent:** Provides the main structure (sidebar, navbar, content area) for admin pages.
    *   **Dashboard Module/Component:** (Routed within AdminModule)
        *   Overview of system statistics (e.g., total products, active mappings, recent alerts, users count).
        *   Charts or quick summaries.
        *   Links to key management sections.
    *   **Category Management Module:** (Routed within AdminModule, potentially lazy-loaded itself)
        *   `CategoryListComponent`: Displays categories in a table (with pagination, search, sort).
        *   `CategoryFormComponent`: For creating/editing categories (modal or separate page).
        *   Communicates with `/api/categories` backend endpoints.
    *   **Product Management Module:** (Routed within AdminModule, potentially lazy-loaded)
        *   `ProductListComponent`: Displays products (table with pagination, search by name/SKU, filter by category, sort).
        *   `ProductFormComponent`: For creating/editing products (including category selection, JSON editor for specifications).
        *   Communicates with `/api/products` backend endpoints.
    *   **Mapping Management Module:** (Routed within AdminModule, potentially lazy-loaded)
        *   `MappingListComponent`: Displays product-seller mappings (table with pagination, search, filter by product/seller).
        *   `MappingFormComponent`: For creating/editing mappings (linking product, seller, URL, site config).
        *   Ability to toggle `isActiveForScraping`.
        *   Communicates with `/api/mappings` backend endpoints.
    *   **Site Configuration Module:** (Routed within AdminModule, potentially lazy-loaded)
        *   `SiteConfigListComponent`: Displays site configurations (table with pagination, search by domain).
        *   `SiteConfigFormComponent`: For creating/editing site configurations (selectors for price, stock, etc.).
        *   Communicates with `/api/site-configs` backend endpoints.
    *   **User Management Module (CRM):** (Routed within AdminModule, potentially lazy-loaded)
        *   `UserListComponent`: Displays users (table with pagination, search by email/name, filter by role).
        *   `UserFormComponent`: For creating/editing users (profile info, assign roles).
        *   Communicates with `/api/admin/users` backend endpoints.
    *   **Alert Management Module (Admin View):** (Routed within AdminModule, potentially lazy-loaded)
        *   `AdminAlertListComponent`: Displays all alert rules across all users (table with pagination, search, filter by user/product).
        *   View details of an alert rule.
        *   (Optional) Admin ability to enable/disable or delete alerts.
        *   Communicates with `/api/admin/alerts` backend endpoints.
    *   **Price History Module (Admin View):** (Routed within AdminModule, potentially lazy-loaded)
        *   `PriceHistoryViewerComponent`: Interface to select a product (and optionally seller) to view its price history chart (using a charting library like ng2-charts/Chart.js or ECharts) and data table.
        *   Communicates with `/api/products/{canonicalProductId}/price-history` backend endpoint.

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

### 9. Development Roadmap (Summary)

1.  **Phase 1: Backend Foundation (Sprint 1-2):**
    *   .NET Aspire project setup.
    *   `TechTicker.Domain`, `TechTicker.DataAccess` (EF Core, initial models for Category, Product).
    *   `TechTicker.ApiService`: Category API, Product API (CRUD).
    *   User Authentication (ASP.NET Core Identity setup, JWT issuance, login/register API).
    *   Initial `TechTicker.ServiceDefaults` (logging, health checks).
2.  **Phase 2: Backend Scraping Core (Sprint 3-4):**
    *   Product Mapping & Site Configuration: Models, Repositories, APIs.
    *   `TechTicker.ScrapingWorker`: `ScrapingOrchestrationModule`, `ScraperModule` (basic HTTP/HtmlAgilityPack scraping).
    *   RabbitMQ setup within Aspire, define initial messages (`ScrapeProductPageCommand`, `ScrapingResultEvent`).
    *   `PriceNormalizationIngestionModule` and `PriceHistoryModule` (models, consumers, basic API for history).
3.  **Phase 3: Angular Frontend - Foundation & Core Admin (Sprint 5-7):**
    *   Angular project setup (`ng new techticker-admin-portal`).
    *   Core Module (AuthService, Guards, Interceptors), Auth Module (Login Page).
    *   Admin Module layout (sidebar, navbar).
    *   Dashboard (basic placeholder).
    *   Category Management UI (list, create, edit, delete forms and views).
    *   Product Management UI (list, create, edit, delete forms and views).
4.  **Phase 4: Backend Alerts & Admin APIs (Sprint 8-9):**
    *   Alert Definition Module (model, API for users to create their own alerts).
    *   Alert Evaluation Module (consumer for `PricePointRecordedEvent`, logic, `AlertTriggeredEvent`).
    *   `TechTicker.NotificationWorker`: `NotificationModule` (consumer for `AlertTriggeredEvent`, email sending via MailKit).
    *   Admin API Endpoints: For managing users (`/api/admin/users`), viewing all alerts (`/api/admin/alerts`).
5.  **Phase 5: Angular Frontend - Advanced Admin & CRM (Sprint 10-12):**
    *   Mapping Management UI.
    *   Site Configuration UI.
    *   User Management (CRM) UI (list users, edit roles, create users).
    *   Admin View for All Alerts.
    *   Admin View for Price History (with charts).
6.  **Phase 6: Integration, Testing, Evasion & Refinement (Sprint 13-14):**
    *   End-to-end testing of backend and frontend flows.
    *   Refine scraper evasion techniques based on testing.
    *   Performance testing and optimization (API, database queries, frontend load times).
    *   Comprehensive logging and error handling review.
    *   Security review and hardening.
7.  **Phase 7: Deployment Prep & Documentation (Sprint 15):**
    *   Finalize Dockerfiles and deployment scripts/manifests (e.g., K8s or Docker Compose for production).
    *   Set up CI/CD pipelines.
    *   Complete all technical documentation and user guides for admins.
    *   User Acceptance Testing (UAT).
8.  **Phase 8: Go-Live & Post-Launch Support (Sprint 16):**
    *   Production deployment.
    *   Monitoring and initial support.

---

### 10. Non-Functional Requirements

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

*   **User-Facing Frontend:** A separate Angular (or other) application for end-users to browse products, view price histories, and manage their own alerts (beyond just API access).
*   **Hierarchical Categories:** Support for nested product categories.
*   **Category-Specific Alert Rules:** Allow users to set alerts for entire categories (e.g., "any GPU below $300").
*   **Faceted Search & Filtering:** Advanced search capabilities on the user-facing frontend.
*   **Automated Product Discovery (Advanced):** Instead of only exact URLs, allow admins to input search terms for a site, and the system attempts to find relevant product pages (very complex, high risk of errors/detection).
*   **Advanced Alert Conditions:** More complex rules (e.g., "price dropped by X% AND is in stock," "price is X% lower than average of last 30 days").
*   **Additional Notification Channels:** SMS (via Twilio/Vonage), Push Notifications (for mobile/web apps), Slack/Discord webhooks.
*   **Browser Automation Integration (Playwright/Puppeteer):** For specific sites that heavily rely on JavaScript or have strong anti-bot measures (use sparingly due to resource intensity and maintenance). This would likely be a separate, specialized worker.
*   **Dynamic Evasion Strategy Adjustment:** ML-based system to monitor scraper success rates and automatically adjust UAs, headers, delays, or even suggest proxy usage per site.
*   **ML for Selector Finding/Maintenance:** Tools or services that can help automatically identify or suggest updates for CSS/XPath selectors when site layouts change.
*   **User-Contributed Mappings/Selectors:** Allow trusted users to submit new product mappings or scraper selectors, with an admin approval workflow.
*   **Affiliate Link Integration:** Option to automatically append affiliate tags to product URLs shown to users.
*   **Comparative Analysis Features:** "Product A vs. Product B" price trends, best price across all tracked sellers for a given product.
*   **Enhanced Admin Dashboard:** More detailed analytics, system health overview, scraper performance metrics, DLQ management interface.
*   **Audit Logging:** Comprehensive logging of all admin actions performed through the frontend/API.
*   **Internationalization (i18n) / Localization (l10n):** Support for multiple languages and regions in both frontend and backend (data, notifications).
*   **Proxy Integration:** Allow configuration and use of proxy servers (rotating, residential) for scraping to improve stealth and avoid IP bans. Also add functionality to test the proxy servers before use.