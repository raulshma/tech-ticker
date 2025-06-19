# TechTicker: E-commerce Price Tracker & Alerter

[![.NET](https://github.com/raulshma/tech-ticker/actions/workflows/dotnet.yml/badge.svg)](https://github.com/raulshma/tech-ticker/actions/workflows/dotnet.yml)

TechTicker is a comprehensive system designed to track prices of specified computer hardware and related products across multiple e-commerce websites. It provides users with historical price data and alerts them to price changes based on user-defined rules. The system features an administration portal for managing products, categories, and system configurations.

## Goals

*   **Accuracy:** Provide accurate and timely price information.
*   **Organization:** Effectively categorize products for ease of browsing and management.
*   **Stealth:** Implement scraping methods that aim to reduce the likelihood of detection by target websites.
*   **Reliability:** Ensure consistent operation of scraping and alerting mechanisms.
*   **Scalability:** Design the application to handle a growing number of products, users, and tracked sites.
*   **Maintainability:** Leverage .NET Aspire for backend organization and Angular best practices for the frontend.
*   **Usability:** Provide a clear API and an intuitive admin portal.

## System Architecture

TechTicker employs a **monolithic architecture for its backend, built with .NET Aspire**. The backend consists of:
*   A primary ASP.NET Core web project (`TechTicker.ApiService`) handling API requests.
*   Several .NET Worker Service projects (`TechTicker.ScrapingWorker`, `TechTicker.NotificationWorker`) for background tasks like scraping and notifications.
These projects are organized within a single .NET Aspire solution, orchestrated by the `TechTicker.AppHost` project.

The **frontend is a Single Page Application (SPA) built using Angular** (`TechTicker.Frontend`). It serves as the administration and CRM portal, communicating with the backend via a RESTful API.

A message broker (RabbitMQ) is used for asynchronous communication between the API layer and background worker components.

## Technology Stack

### Backend
*   **Framework:** .NET 8+ with ASP.NET Core, .NET Worker Services
*   **Orchestration & Development:** .NET Aspire
*   **Database:** PostgreSQL with Entity Framework Core (EF Core)
*   **Message Broker:** RabbitMQ
*   **Web Scraping:** HtmlAgilityPack, HttpClientFactory
*   **Authentication:** JWT (JSON Web Tokens) via ASP.NET Core Identity
*   **Logging:** Serilog integrated with OpenTelemetry
*   **Containerization:** Docker (facilitated by .NET Aspire)

### Frontend
*   **Framework:** Angular (v20)
*   **CLI:** Angular CLI
*   **Package Manager:** npm

## Project Structure

The solution is organized into the following main projects:

*   `TechTicker.AppHost/`: .NET Aspire application host for orchestrating services during development.
*   `TechTicker.ServiceDefaults/`: Shared configurations and extensions for .NET Aspire services.
*   `TechTicker.ApiService/`: The main ASP.NET Core web API.
*   `TechTicker.Frontend/`: Angular SPA for administration and CRM. (See `TechTicker.Frontend/README.md` for more details)
*   `TechTicker.ScrapingWorker/`: Worker service for web scraping tasks.
*   `TechTicker.NotificationWorker/`: Worker service for sending notifications.
*   `TechTicker.Domain/`: Core domain entities and business logic.
*   `TechTicker.DataAccess/`: Data access layer using EF Core and PostgreSQL.
*   `TechTicker.Application/`: Application services, DTOs, and other application-level concerns.
*   `TechTicker.Shared/`: Common utilities, response models, and exception handling. (See `TechTicker.Shared/README.md` for more details)

## Key Features

*   **Product Catalog Management:** Administer products, including categorization.
*   **Price Tracking:** Scrape and store historical price data for specified products.
*   **Alerting System:** Notify users of price changes based on defined rules.
*   **User Management:** Secure authentication and authorization for administrators.
*   **Stealthy Scraping:** Implements techniques to reduce detectability, including proxy integration.
*   **Admin & CRM Portal:** Angular-based frontend for system management.

## API Endpoints Overview

The `TechTicker.ApiService` exposes a RESTful API for managing the system. Key resource endpoints include:

*   `/api/products`: For managing products (CRUD operations).
*   `/api/categories`: For managing product categories.
*   `/api/alerts`: For managing price alert rules.
*   `/api/auth`: For user authentication and authorization.
*   `/api/siteconfigs`: For managing configurations for different e-commerce sites.

For detailed API specifications, refer to the Swagger/OpenAPI documentation available when the `TechTicker.ApiService` is running (typically at `/swagger`).

## Getting Started

### Backend Development
The backend services are orchestrated using .NET Aspire.
1.  Ensure you have the .NET 8 SDK installed.
2.  Open the `TechTicker.sln` solution file in Visual Studio or your preferred IDE.
3.  Set `TechTicker.AppHost` as the startup project.
4.  Run the `TechTicker.AppHost` project. This will launch all backend services, including the API, worker services, database, and message broker (as configured in `Program.cs` of `TechTicker.AppHost`).

### Frontend Development
The frontend is an Angular application.
1.  Ensure Node.js (18+) and Angular CLI are installed.
2.  Navigate to the `TechTicker.Frontend/` directory.
3.  Install dependencies: `npm install`
4.  Start the development server: `ng serve`
5.  Open your browser and navigate to `http://localhost:4200/`.

For more details on frontend development, refer to `TechTicker.Frontend/README.md`.

## Configuration

Each service (`TechTicker.ApiService`, `TechTicker.ScrapingWorker`, `TechTicker.NotificationWorker`, `TechTicker.AppHost`) uses `appsettings.json` and environment-specific `appsettings.{Environment}.json` files for configuration.

Key configurations include:
*   **Database Connection Strings:** Found in `TechTicker.ApiService/appsettings.json` and overridden by Aspire for development.
*   **RabbitMQ Connection:** Managed by .NET Aspire in development and configurable for production.
*   **JWT Settings:** For authentication in `TechTicker.ApiService/appsettings.json`.
*   **Logging Levels:** Configurable per service.

When deploying, ensure these configurations are appropriately set using environment variables or other configuration providers.

## Deployment

*   **Backend:** .NET Aspire provides tools and patterns to simplify deployment to containerized environments (e.g., Docker, Azure Container Apps). Dockerfiles can be generated to assist with this process.
*   **Frontend:** The Angular application can be built using `ng build` and the output in the `dist/` directory can be deployed to any static web hosting service.

## Contributing

Contributions to TechTicker are welcome! If you'd like to contribute, please follow these general guidelines:

1.  **Fork the repository.**
2.  **Create a new branch** for your feature or bug fix: `git checkout -b feature/your-feature-name` or `git checkout -b fix/issue-number`.
3.  **Make your changes** and ensure they adhere to the project's coding style and conventions.
4.  **Write unit tests** for any new functionality or bug fixes.
5.  **Ensure all tests pass.**
6.  **Commit your changes** with a clear and descriptive commit message.
7.  **Push your branch** to your forked repository.
8.  **Create a pull request** to the main repository's `main` or `develop` branch (please clarify which branch is primary for PRs if not `main`).

Please ensure your pull request describes the changes made and any relevant context.

## License

This project is licensed under the MIT License. See the `LICENSE` file for more details. (Note: A `LICENSE` file should be added to the repository root).

## Documentation

For more detailed information on the system's design, modules, and specifications, please refer to the [Detailed Software Specification](docs/Documentation.md).
