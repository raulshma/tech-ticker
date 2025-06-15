# TechTicker UI Development Tasks (CRM & Core Functionality)

This document outlines the tasks required to build the TechTicker UI using Angular and the ng-zorro component library. The focus is on CRM functionalities and core application features, excluding alerts and notifications as per the requirement.

## Guiding Principles & Common Patterns

*   **Modularity:** Features should be developed in their own lazy-loaded Angular modules.
*   **Admin Modules:** Admin-specific feature modules must be protected by an admin role guard.
*   **CRUD Operations (Admin):** For entities managed by admins, the following components are typically required:
    *   **List View:** Display items in an `ng-zorro` table with pagination, sorting, and search/filter capabilities.
    *   **Detail View:** A page or modal to display comprehensive information about a single item.
    *   **Create/Edit Form:** A reactive form for creating new items (typically `POST` to `/api/entity`) and updating existing items (typically `PUT` to `/api/entity/{id}`).
    *   **Delete Action:** Functionality to delete items (typically `DELETE` to `/api/entity/{id}`), always with a confirmation dialog.
*   **API Endpoints:** Unless specified otherwise, API interactions will target endpoints under the `/api/` prefix (e.g., `/api/users`, `/api/products`).
*   **User Experience (UX):**
    *   **Responsive Design:** All UI elements must be responsive across various screen sizes.
    *   **Feedback:** Consistent use of `ng-zorro` message/notification components for success, error, and warning feedback. Loading indicators for API calls.
    *   **Accessibility (A11y):** Adherence to web accessibility best practices.
*   **Testing:** Unit tests for components and services are essential. End-to-end (E2E) tests for key user flows are highly recommended.

## Phase 1: Project Setup & Core UI

*   [x] **Set up ng-zorro-antd:**
    *   [x] Install `ng-zorro-antd` library.
    *   [x] Configure ng-zorro in the Angular project (imports, styles, etc.).
*   [x] **Basic Application Layout:**
    *   [x] Implement a main layout structure (e.g., sidebar navigation, header, content area) using ng-zorro layout components.
    *   [x] Create a theme file or customize ng-zorro theme variables.
*   [x] **Routing:**
    *   [x] Set up Angular routing for different modules/features.
    *   [x] Implement lazy loading for feature modules.
*   [x] **API Service Integration:**
    *   [x] Create a core API service/module to handle HTTP requests to the backend API Gateway (primarily using `/api/...` endpoints).
    *   [x] Implement base error handling and request/response interceptors (e.g., for adding auth tokens).

## Phase 2: Authentication & User Profile

*   [x] **Authentication Module:**
    *   [x] Create an Authentication feature module.
    *   [x] **Login Page:**
        *   [x] Design and implement the login form (using `/api/users/login`).
        *   [x] Handle successful login (store token, navigate to dashboard).
        *   [x] Display login errors.
    *   [x] **Registration Page:**
        *   [x] Design and implement the registration form (using `/api/users/register`).
        *   [x] Handle successful registration.
        *   [x] Display registration errors.
    *   [x] **Auth Guard:**
        *   [x] Implement an `AuthGuard` to protect routes requiring authentication.
    *   [x] **Logout Functionality:**
        *   [x] Implement logout (clear token, navigate to login).
*   [x] **User Profile Management (Self-Service):**
    *   [x] Create a User Profile page/section.
    *   [x] Display current user's profile information (`GET /api/users/me`).
    *   [x] Allow users to update their own profile (`PUT /api/users/me` or `/api/users/{id}` using their own ID).
    *   [x] Allow users to change their password (`PUT /api/users/me/password` or `/api/users/{id}/password`).

## Phase 3: User Management (Admin CRM Task)

*   [x] **User Management Module (Admin):** Adhering to "Admin Modules" and "CRUD Operations" principles.
    *   [x] **List Users:** (`GET /api/users/search` or `/api/users`).
    *   [x] **View User Details:** (`GET /api/users/{id}`).
    *   [x] **Edit User:** (`PUT /api/users/{id}`).
    *   [x] **Manage User Roles:** Interface to assign/remove roles (`POST /api/users/{id}/roles`, `DELETE /api/users/{id}/roles/{roleNameOrId}`).
    *   [x] **Change User Status:** Interface to activate/deactivate accounts (`PUT /api/users/{id}/status`).
    *   [x] **(Optional) Create User by Admin:** (`POST /api/users`).

## Phase 4: Product Catalog Management (Admin CRM Task)

*   [x] **Product Catalog Module (Admin):** Adhering to "Admin Modules" and "CRUD Operations" principles.
*   [x] **Category Management:**
    *   [x] **List Categories:** (`GET /api/categories`).
    *   [x] **Create/Edit Category:** Form for name, slug (auto-generated or manual), description (`POST /api/categories`, `PUT /api/categories/{categoryId}`).
    *   [x] **Delete Category:** (`DELETE /api/categories/{categoryId}`), considering products within the category.
*   [x] **Product Management:**
    *   [x] **List Products:** (`GET /api/products`), filter by name, SKU, category (e.g., `?categoryId={idOrSlug}&search={term}`).
    *   [x] **View Product Details:** (`GET /api/products/{productId}`), including all attributes.
    *   [x] **Create/Edit Product:** (`POST /api/products`, `PUT /api/products/{productId}`), including category assignment (dropdown from Category API).
    *   [x] **Delete Product:** (`DELETE /api/products/{productId}`).

## Phase 5: Product Mapping & Site Configuration (Admin CRM Task)

*   [x] **Mappings & Configurations Module (Admin):** Adhering to "Admin Modules" and "CRUD Operations" principles.
*   [x] **Site Configuration Management:**
    *   [x] **List Site Configurations:** (`GET /api/site-configs`).
    *   [x] **View Site Configuration Details:** (`GET /api/site-configs/{siteConfigId}` or `?domain={domainName}`).
    *   [x] **Create/Edit Site Configuration:** Form for domain, selectors, etc. (`POST /api/site-configs`, `PUT /api/site-configs/{siteConfigId}`).
    *   [x] **Delete Site Configuration:** (`DELETE /api/site-configs/{siteConfigId}`).
*   [x] **Product-Seller Mapping Management:**
    *   [x] **List Mappings:** (`GET /api/mappings`), filter by canonical product ID (`?canonicalProductId={id}`).
    *   [x] **View Mapping Details:** Show linked site configuration.
    *   [x] **Create/Edit Mapping:** Form for canonical product (searchable dropdown), seller, URL, `isActiveForScraping`, `SiteConfigId` (dropdown), optional scraping frequency (`POST /api/mappings`, `PUT /api/mappings/{mappingId}`).
    *   [x] **Delete Mapping:** (`DELETE /api/mappings/{mappingId}`).
    *   [x] **Display Scraping Status:**
        *   [x] Show `LastScrapedAt` and `NextScrapeAt` if available in the mapping data.

## Phase 6: Price History Viewing

*   [x] **Price History Display:**
    *   [x] On the product details page (or a dedicated history page per product).
    *   [x] Fetch price history for a canonical product (`GET /api/products/{canonicalProductId}/price-history`).
    *   [x] Allow filtering by `sellerName`, `startDate`, `endDate`.
    *   [x] Display data in a chart (e.g., using ng-zorro charts or another library like ngx-charts) and/or a table.
    *   [x] Show price and stock status over time.

## Phase 7: General UI/UX Enhancements & Testing

*   [ ] **Global Search (Optional):**
    *   [ ] Implement a global search bar to find products, categories, users, etc.
*   [ ] **Dashboard (Optional):**
    *   [ ] Create a dashboard page summarizing key information.
*   [ ] **Review & Refine:**
    *   [ ] Ensure "Responsive Design", "Error Handling & Feedback", and "Accessibility (A11y)" principles from the "Guiding Principles" section are met across the application.
*   [ ] **Testing:**
    *   [ ] Write comprehensive unit tests for components and services.
    *   [ ] (Optional) Set up and write E2E tests for key user flows.

This list should provide a good starting point for developing the TechTicker UI. Remember to break these down further into smaller, manageable tasks as needed.
