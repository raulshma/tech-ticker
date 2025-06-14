# ScrapingOrchestrationService Database Separation Fix

## Problem Description

The `TechTicker.ScrapingOrchestrationService` was originally configured to use the same database entities as the `ProductSellerMappingService`, which caused migration conflicts because:

1. Both services tried to manage the same entities (`ProductSellerMapping`, `ScraperSiteConfiguration`)
2. EF migrations would conflict when both services tried to create/modify the same tables
3. This violated the microservice principle of database per service

## Solution Implementation

### 1. Database Separation

**Before:**
- ScrapingOrchestrationService included `ProductSellerMapping` and `ScraperSiteConfiguration` in its DbContext
- Used single database reference `scraping-orchestration`

**After:**
- ScrapingOrchestrationService only manages its own entities (`DomainScrapingProfile`)
- Reads shared entities through `ProductSellerMappingDbContext`
- Uses both `scraping-orchestration` and `product-seller-mapping` databases

### 2. Key Changes Made

#### ScrapingOrchestrationDbContext.cs
- **Removed**: `ProductSellerMapping` and `ScraperSiteConfiguration` DbSets and configurations
- **Kept**: Only `DomainScrapingProfile` entities that belong to this service
- **Added**: Clear documentation about separation of concerns

#### Program.cs (ScrapingOrchestrationService)
- **Added**: Reference to `ProductSellerMappingDbContext` for read-only access
- **Added**: Project reference to `TechTicker.ProductSellerMappingService`

#### ScrapingSchedulerService.cs
- **Changed**: Uses `ProductSellerMappingDbContext` instead of `ScrapingOrchestrationDbContext`
- **Reason**: This service primarily works with `ProductSellerMapping` entities

#### TechTicker.Host (Aspire)
- **Added**: `productSellerMappingDb` reference to ScrapingOrchestrationService
- **Maintains**: Separate `scrapingOrchestrationDb` for service's own entities

#### MigrationService
- **Added**: Support for `ScrapingOrchestrationDbContext` migrations
- **Added**: Project references to all services with databases
- **Result**: Proper migration handling for all database contexts

### 3. Database Architecture

```
┌─────────────────────────────────────┐
│ ProductSellerMappingService         │
│ ├── ProductSellerMapping (Owner)    │
│ └── ScraperSiteConfiguration (Owner)│
└─────────────────────────────────────┘
               ▲
               │ Read-Only Access
               │
┌─────────────────────────────────────┐
│ ScrapingOrchestrationService        │
│ ├── DomainScrapingProfile (Owner)   │
│ ├── ProductSellerMapping (Read)     │
│ └── ScraperSiteConfiguration (Read) │
└─────────────────────────────────────┘
```

### 4. Migration Strategy

1. **ScrapingOrchestrationService**: Manages only `DomainScrapingProfile` table
2. **ProductSellerMappingService**: Continues to own and manage its entities
3. **MigrationService**: Runs migrations for all database contexts separately
4. **No Conflicts**: Each service only creates/modifies tables it owns

### 5. Benefits

✅ **Proper Separation of Concerns**: Each service owns its data
✅ **No Migration Conflicts**: Services don't compete for the same tables  
✅ **Clean Architecture**: Follows microservice database patterns
✅ **Maintainable**: Clear ownership and responsibility boundaries
✅ **Scalable**: Services can evolve their schemas independently

### 6. Files Modified

- `Services/TechTicker.ScrapingOrchestrationService/Data/ScrapingOrchestrationDbContext.cs`
- `Services/TechTicker.ScrapingOrchestrationService/Program.cs`
- `Services/TechTicker.ScrapingOrchestrationService/Services/ScrapingSchedulerService.cs`
- `Services/TechTicker.ScrapingOrchestrationService/TechTicker.ScrapingOrchestrationService.csproj`
- `Aspire/TechTicker.Host/Program.cs`
- `Aspire/TechTicker.MigrationService/Program.cs`
- `Aspire/TechTicker.MigrationService/Worker.cs`
- `Aspire/TechTicker.MigrationService/TechTicker.MigrationService.csproj`

### 7. Testing

- ✅ Solution builds successfully
- ✅ ScrapingOrchestrationService migration created
- ✅ All database contexts properly configured
- ✅ No circular dependencies

## Next Steps

1. **Run Migrations**: Execute the migration service to apply all database schemas
2. **Test Services**: Verify that ScrapingOrchestrationService can read from ProductSellerMapping database
3. **Monitor**: Ensure no further migration conflicts occur

## Notes

- The ScrapingOrchestrationService now properly follows the "database per service" pattern
- Shared data access is handled through explicit cross-service database references
- This approach maintains loose coupling while enabling necessary data access
