# Product Specification Scraping - Implementation Plan

## Overview
This document outlines the implementation plan for adding end-to-end product specification scraping capabilities to the TechTicker system. The feature will integrate the existing `HtmlUtilities.cs` table parsing capabilities into the `WebScrapingService.cs` and extend the frontend to display parsed specifications.

## Current State Analysis

### Existing Components
- **WebScrapingService.cs**: Handles product data scraping (name, price, stock, images)
- **HtmlUtilities.cs**: Advanced table parsing with support for multiple vendor formats
- **Frontend**: Product display with basic information
- **Site Configuration**: Selectors for basic product data
- **Scrape Logs**: Detailed logging of scraping operations

### Missing Components
- Specification selector in site configuration
- Integration between WebScrapingService and HtmlUtilities
- Specification storage and display
- Enhanced logging for specification parsing

## Implementation Plan

### Phase 1: Backend Data Models & Configuration

#### 1.1 Update Site Configuration DTOs
**File**: `TechTicker.Application/DTOs/SiteConfigurationDto.cs`

```csharp
// Add to existing ScrapingSelectorsDto
public class ScrapingSelectorsDto
{
    // ... existing selectors ...
    public string? SpecificationTableSelector { get; set; }
    public string? SpecificationContainerSelector { get; set; }
    public SpecificationParsingOptions? SpecificationOptions { get; set; }
}

public class SpecificationParsingOptions
{
    public bool EnableCaching { get; set; } = true;
    public bool ThrowOnError { get; set; } = false;
    public int MaxCacheEntries { get; set; } = 1000;
    public TimeSpan CacheExpiry { get; set; } = TimeSpan.FromHours(24);
    public string PreferredVendor { get; set; } = "Generic";
}
```

#### 1.2 Update Scraping Command/Result Models
**File**: `TechTicker.Application/DTOs/ScrapingDto.cs`

```csharp
// Add to existing ScrapeProductPageCommand
public class ScrapeProductPageCommand
{
    // ... existing properties ...
    public bool ScrapeSpecifications { get; set; } = false;
}

// Add to existing ScrapingResult
public class ScrapingResult
{
    // ... existing properties ...
    public ProductSpecificationResult? Specifications { get; set; }
}

public class ProductSpecificationResult
{
    public bool IsSuccess { get; set; }
    public string? ErrorMessage { get; set; }
    public Dictionary<string, object> Specifications { get; set; } = new();
    public Dictionary<string, TypedSpecification> TypedSpecifications { get; set; } = new();
    public Dictionary<string, CategoryGroup> CategorizedSpecs { get; set; } = new();
    public ParseMetadata Metadata { get; set; } = new();
    public QualityMetrics Quality { get; set; } = new();
    public long ParsingTimeMs { get; set; }
}
```

#### 1.3 Update Database Entities
**File**: `TechTicker.Domain/Entities/ScraperRunLog.cs`

```csharp
// Add specification-related properties
public class ScraperRunLog
{
    // ... existing properties ...
    
    // Specification Parsing Results
    public string? SpecificationData { get; set; } // JSON of parsed specifications
    public string? SpecificationMetadata { get; set; } // JSON of parsing metadata
    public int? SpecificationCount { get; set; }
    public string? SpecificationParsingStrategy { get; set; }
    public double? SpecificationQualityScore { get; set; }
    public long? SpecificationParsingTime { get; set; }
    public string? SpecificationError { get; set; }
}
```

**File**: `TechTicker.Domain/Entities/ProductSellerMapping.cs`

```csharp
// Add specification storage
public class ProductSellerMapping
{
    // ... existing properties ...
    public string? LatestSpecifications { get; set; } // JSON of latest parsed specs
    public DateTime? SpecificationsLastUpdated { get; set; }
    public double? SpecificationsQualityScore { get; set; }
}
```

### Phase 2: Backend Service Integration

#### 2.1 Update WebScrapingService
**File**: `TechTicker.ScrapingWorker/Services/WebScrapingService.cs`

```csharp
public partial class WebScrapingService
{
    private readonly ILogger<WebScrapingService> _logger;
    private readonly ProxyAwareHttpClientService _proxyHttpClient;
    private readonly IScraperRunLogService _scraperRunLogService;
    private readonly IImageScrapingService _imageScrapingService;
    private readonly ITableParser _tableParser; // Add this
    private readonly IMemoryCache _cache; // Add this

    // Add to constructor
    public WebScrapingService(
        ILogger<WebScrapingService> logger,
        ProxyAwareHttpClientService proxyHttpClient,
        IScraperRunLogService scraperRunLogService,
        IImageScrapingService imageScrapingService,
        ITableParser tableParser, // Add this
        IMemoryCache cache) // Add this
    {
        // ... existing initialization ...
        _tableParser = tableParser;
        _cache = cache;
    }

    // Update main scraping method
    public async Task<ScrapingResult> ScrapeProductPageAsync(ScrapeProductPageCommand command)
    {
        // ... existing scraping logic ...

        // Add specification scraping after successful data extraction
        ProductSpecificationResult? specificationResult = null;
        if (command.ScrapeSpecifications && !string.IsNullOrEmpty(command.Selectors.SpecificationTableSelector))
        {
            var specStopwatch = Stopwatch.StartNew();
            try
            {
                specificationResult = await ScrapeProductSpecificationsAsync(
                    document, 
                    command.Selectors.SpecificationTableSelector,
                    command.Selectors.SpecificationContainerSelector,
                    command.Selectors.SpecificationOptions,
                    command.MappingId);
                
                specStopwatch.Stop();
                specificationResult.ParsingTimeMs = specStopwatch.ElapsedMilliseconds;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Specification scraping failed for mapping {MappingId}", command.MappingId);
                specificationResult = new ProductSpecificationResult
                {
                    IsSuccess = false,
                    ErrorMessage = ex.Message,
                    ParsingTimeMs = specStopwatch.ElapsedMilliseconds
                };
            }
        }

        // Update result with specifications
        var result = new ScrapingResult
        {
            // ... existing properties ...
            Specifications = specificationResult
        };

        // Update run log with specification data
        if (runId.HasValue && specificationResult != null)
        {
            await UpdateRunLogWithSpecifications(runId.Value, specificationResult);
        }

        return result;
    }

    private async Task<ProductSpecificationResult> ScrapeProductSpecificationsAsync(
        IDocument document,
        string tableSelector,
        string? containerSelector,
        SpecificationParsingOptions? options,
        Guid mappingId)
    {
        try
        {
            _logger.LogInformation("Starting specification parsing for mapping {MappingId}", mappingId);

            // Find specification tables
            var tables = string.IsNullOrEmpty(containerSelector)
                ? document.QuerySelectorAll(tableSelector)
                : document.QuerySelector(containerSelector)?.QuerySelectorAll(tableSelector);

            if (tables == null || !tables.Any())
            {
                return new ProductSpecificationResult
                {
                    IsSuccess = false,
                    ErrorMessage = "No specification tables found with the provided selector"
                };
            }

            // Convert to HTML and parse with HtmlUtilities
            var htmlContent = string.Join("\n", tables.Select(t => t.OuterHtml));
            
            var parsingOptions = new ParsingOptions
            {
                EnableCaching = options?.EnableCaching ?? true,
                ThrowOnError = options?.ThrowOnError ?? false,
                MaxCacheEntries = options?.MaxCacheEntries ?? 1000,
                CacheExpiry = options?.CacheExpiry ?? TimeSpan.FromHours(24)
            };

            var parseResult = await _tableParser.ParseAsync(htmlContent, parsingOptions);

            if (!parseResult.Success || !parseResult.Data.Any())
            {
                return new ProductSpecificationResult
                {
                    IsSuccess = false,
                    ErrorMessage = "Failed to parse specification tables",
                    Metadata = new ParseMetadata
                    {
                        Warnings = parseResult.Warnings,
                        ProcessingTimeMs = (long)parseResult.ProcessingTime.TotalMilliseconds
                    }
                };
            }

            // Merge all parsed specifications
            var mergedSpec = MergeSpecifications(parseResult.Data);

            _logger.LogInformation("Successfully parsed {SpecCount} specifications for mapping {MappingId}", 
                mergedSpec.Specifications.Count, mappingId);

            return new ProductSpecificationResult
            {
                IsSuccess = true,
                Specifications = mergedSpec.Specifications,
                TypedSpecifications = mergedSpec.TypedSpecifications,
                CategorizedSpecs = mergedSpec.CategorizedSpecs,
                Metadata = mergedSpec.Metadata,
                Quality = mergedSpec.Quality,
                ParsingTimeMs = mergedSpec.Metadata.ProcessingTimeMs
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing specifications for mapping {MappingId}", mappingId);
            return new ProductSpecificationResult
            {
                IsSuccess = false,
                ErrorMessage = ex.Message
            };
        }
    }

    private ProductSpecification MergeSpecifications(List<ProductSpecification> specifications)
    {
        if (specifications.Count == 1)
            return specifications[0];

        var merged = new ProductSpecification
        {
            Id = Guid.NewGuid().ToString(),
            Metadata = specifications.First().Metadata,
            Source = specifications.First().Source
        };

        foreach (var spec in specifications)
        {
            // Merge specifications
            foreach (var kvp in spec.Specifications)
            {
                if (!merged.Specifications.ContainsKey(kvp.Key))
                    merged.Specifications[kvp.Key] = kvp.Value;
            }

            // Merge typed specifications
            foreach (var kvp in spec.TypedSpecifications)
            {
                if (!merged.TypedSpecifications.ContainsKey(kvp.Key))
                    merged.TypedSpecifications[kvp.Key] = kvp.Value;
            }

            // Merge categorized specs
            foreach (var kvp in spec.CategorizedSpecs)
            {
                if (!merged.CategorizedSpecs.ContainsKey(kvp.Key))
                    merged.CategorizedSpecs[kvp.Key] = kvp.Value;
                else
                {
                    // Merge specifications within the same category
                    foreach (var specKvp in kvp.Value.Specifications)
                    {
                        if (!merged.CategorizedSpecs[kvp.Key].Specifications.ContainsKey(specKvp.Key))
                            merged.CategorizedSpecs[kvp.Key].Specifications[specKvp.Key] = specKvp.Value;
                    }
                }
            }
        }

        // Calculate merged quality metrics
        merged.Quality = new QualityMetrics
        {
            OverallScore = specifications.Average(s => s.Quality.OverallScore),
            StructureConfidence = specifications.Average(s => s.Quality.StructureConfidence),
            TypeDetectionAccuracy = specifications.Average(s => s.Quality.TypeDetectionAccuracy),
            CompletenessScore = specifications.Average(s => s.Quality.CompletenessScore)
        };

        return merged;
    }

    private async Task UpdateRunLogWithSpecifications(Guid runId, ProductSpecificationResult specResult)
    {
        try
        {
            await _scraperRunLogService.UpdateRunLogAsync(runId, new UpdateScraperRunLogDto
            {
                SpecificationData = specResult.IsSuccess ? 
                    JsonSerializer.Serialize(specResult.Specifications) : null,
                SpecificationMetadata = JsonSerializer.Serialize(specResult.Metadata),
                SpecificationCount = specResult.Specifications?.Count,
                SpecificationParsingStrategy = specResult.Metadata?.Structure.ToString(),
                SpecificationQualityScore = specResult.Quality?.OverallScore,
                SpecificationParsingTime = specResult.ParsingTimeMs,
                SpecificationError = specResult.IsSuccess ? null : specResult.ErrorMessage,
                DebugNotes = $"Specification parsing: {(specResult.IsSuccess ? "Success" : "Failed")} - {specResult.Specifications?.Count ?? 0} specs parsed"
            });
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to update run log with specification data for run {RunId}", runId);
        }
    }
}
```

#### 2.2 Register Services
**File**: `TechTicker.ScrapingWorker/Program.cs`

```csharp
// Add to service registration
builder.Services.AddSingleton<ITableParser, UniversalTableParser>();
builder.Services.AddMemoryCache();
```

#### 2.3 Update Browser Automation Support
**File**: `TechTicker.ScrapingWorker/Services/WebScrapingService.cs`

```csharp
// Update browser automation method to support specifications
private async Task<ScrapingResult> ScrapeWithBrowserAutomationAsync(ScrapeProductPageCommand command)
{
    // ... existing browser automation logic ...

    // Add specification scraping after basic data extraction
    ProductSpecificationResult? specificationResult = null;
    if (command.ScrapeSpecifications && !string.IsNullOrEmpty(command.Selectors.SpecificationTableSelector))
    {
        try
        {
            // Get page content for specification parsing
            var pageContent = await page.ContentAsync();
            var config = Configuration.Default;
            var context = BrowsingContext.New(config);
            var document = await context.OpenAsync(req => req.Content(pageContent));

            specificationResult = await ScrapeProductSpecificationsAsync(
                document,
                command.Selectors.SpecificationTableSelector,
                command.Selectors.SpecificationContainerSelector,
                command.Selectors.SpecificationOptions,
                command.MappingId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Browser automation specification scraping failed for mapping {MappingId}", command.MappingId);
        }
    }

    return new ScrapingResult
    {
        // ... existing properties ...
        Specifications = specificationResult
    };
}
```

### Phase 3: Frontend Implementation

#### 3.1 Update Site Configuration Components
**File**: `TechTicker.Frontend/src/app/features/site-configs/components/site-config-form/site-config-form.component.html`

```html
<!-- Add after existing selectors -->
<div class="form-group">
  <label for="specificationTableSelector">Specification Table Selector</label>
  <input
    type="text"
    id="specificationTableSelector"
    name="specificationTableSelector"
    [(ngModel)]="siteConfig.selectors.specificationTableSelector"
    class="form-control"
    placeholder="table.specifications, .spec-table, etc.">
  <small class="form-text text-muted">
    CSS selector for specification tables on the product page
  </small>
</div>

<div class="form-group">
  <label for="specificationContainerSelector">Specification Container Selector (Optional)</label>
  <input
    type="text"
    id="specificationContainerSelector"
    name="specificationContainerSelector"
    [(ngModel)]="siteConfig.selectors.specificationContainerSelector"
    class="form-control"
    placeholder=".spec-container, #specifications, etc.">
  <small class="form-text text-muted">
    CSS selector for the container holding specification tables
  </small>
</div>

<div class="form-group">
  <div class="form-check">
    <input
      type="checkbox"
      id="enableSpecificationScraping"
      name="enableSpecificationScraping"
      [(ngModel)]="siteConfig.enableSpecificationScraping"
      class="form-check-input">
    <label for="enableSpecificationScraping" class="form-check-label">
      Enable Specification Scraping
    </label>
  </div>
</div>
```

#### 3.2 Create Specification Display Component
**File**: `TechTicker.Frontend/src/app/shared/components/product-specifications/product-specifications.component.ts`

```typescript
import { Component, Input } from '@angular/core';

export interface ProductSpecification {
  isSuccess: boolean;
  errorMessage?: string;
  specifications: { [key: string]: any };
  typedSpecifications: { [key: string]: TypedSpecification };
  categorizedSpecs: { [key: string]: CategoryGroup };
  metadata: ParseMetadata;
  quality: QualityMetrics;
  parsingTimeMs: number;
}

export interface TypedSpecification {
  value: any;
  type: string;
  unit: string;
  numericValue?: number;
  confidence: number;
  category: string;
  hasMultipleValues: boolean;
  valueCount: number;
  alternatives: SpecificationValue[];
}

export interface CategoryGroup {
  name: string;
  specifications: { [key: string]: TypedSpecification };
  order: number;
  confidence: number;
  isExplicit: boolean;
  itemCount: number;
  multiValueCount: number;
}

@Component({
  selector: 'app-product-specifications',
  templateUrl: './product-specifications.component.html',
  styleUrls: ['./product-specifications.component.scss']
})
export class ProductSpecificationsComponent {
  @Input() specifications?: ProductSpecification;
  @Input() showMetadata = false;

  activeTab = 'categorized';
  
  get hasSpecifications(): boolean {
    return this.specifications?.isSuccess && 
           Object.keys(this.specifications.specifications || {}).length > 0;
  }

  get categoryEntries(): [string, CategoryGroup][] {
    if (!this.specifications?.categorizedSpecs) return [];
    return Object.entries(this.specifications.categorizedSpecs)
      .sort(([,a], [,b]) => a.order - b.order);
  }

  get flatSpecifications(): [string, TypedSpecification][] {
    if (!this.specifications?.typedSpecifications) return [];
    return Object.entries(this.specifications.typedSpecifications);
  }

  formatValue(spec: TypedSpecification): string {
    if (spec.hasMultipleValues && Array.isArray(spec.value)) {
      return spec.value.join(', ');
    }
    
    if (typeof spec.value === 'object' && spec.value !== null) {
      return JSON.stringify(spec.value);
    }
    
    return String(spec.value || '');
  }

  getTypeIcon(type: string): string {
    const icons: { [key: string]: string } = {
      'Text': 'text-fields',
      'Numeric': '123',
      'Memory': 'memory',
      'Clock': 'schedule',
      'Power': 'power',
      'Dimension': 'straighten',
      'Interface': 'cable',
      'Resolution': 'display_settings',
      'List': 'list'
    };
    
    return icons[type] || 'info';
  }

  getConfidenceColor(confidence: number): string {
    if (confidence >= 0.9) return 'success';
    if (confidence >= 0.7) return 'warning';
    return 'danger';
  }
}
```

**File**: `TechTicker.Frontend/src/app/shared/components/product-specifications/product-specifications.component.html`

```html
<div class="product-specifications" *ngIf="specifications">
  <!-- Error State -->
  <div *ngIf="!specifications.isSuccess" class="alert alert-warning">
    <i class="material-icons">warning</i>
    <strong>Specification Parsing Failed:</strong>
    {{ specifications.errorMessage }}
  </div>

  <!-- Success State -->
  <div *ngIf="hasSpecifications">
    <div class="spec-header d-flex justify-content-between align-items-center mb-3">
      <h5 class="mb-0">
        <i class="material-icons me-2">description</i>
        Product Specifications
      </h5>
      <div class="spec-quality">
        <span class="badge" [ngClass]="'bg-' + getConfidenceColor(specifications.quality.overallScore)">
          Quality: {{ (specifications.quality.overallScore * 100) | number:'1.0-0' }}%
        </span>
      </div>
    </div>

    <!-- Tab Navigation -->
    <ul class="nav nav-tabs mb-3">
      <li class="nav-item">
        <a class="nav-link" 
           [class.active]="activeTab === 'categorized'"
           (click)="activeTab = 'categorized'">
          <i class="material-icons me-1">category</i>
          By Category
        </a>
      </li>
      <li class="nav-item">
        <a class="nav-link" 
           [class.active]="activeTab === 'flat'"
           (click)="activeTab = 'flat'">
          <i class="material-icons me-1">list</i>
          All Specifications
        </a>
      </li>
      <li class="nav-item" *ngIf="showMetadata">
        <a class="nav-link" 
           [class.active]="activeTab === 'metadata'"
           (click)="activeTab = 'metadata'">
          <i class="material-icons me-1">analytics</i>
          Metadata
        </a>
      </li>
    </ul>

    <!-- Categorized View -->
    <div *ngIf="activeTab === 'categorized'" class="categorized-specs">
      <div *ngFor="let [categoryName, category] of categoryEntries" class="category-group mb-4">
        <div class="category-header">
          <h6 class="category-title">
            {{ category.name }}
            <span class="badge bg-secondary ms-2">{{ category.itemCount }}</span>
            <span *ngIf="category.multiValueCount > 0" class="badge bg-info ms-1">
              {{ category.multiValueCount }} multi-value
            </span>
          </h6>
        </div>
        <div class="category-specs">
          <div class="row">
            <div *ngFor="let [key, spec] of Object.entries(category.specifications)" 
                 class="col-md-6 col-lg-4 mb-2">
              <div class="spec-item">
                <div class="spec-key">
                  <i class="material-icons spec-icon">{{ getTypeIcon(spec.type) }}</i>
                  {{ key }}
                  <span *ngIf="spec.unit" class="spec-unit">({{ spec.unit }})</span>
                </div>
                <div class="spec-value">{{ formatValue(spec) }}</div>
                <div *ngIf="spec.confidence < 0.8" class="spec-confidence">
                  <small class="text-muted">
                    Confidence: {{ (spec.confidence * 100) | number:'1.0-0' }}%
                  </small>
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>

    <!-- Flat View -->
    <div *ngIf="activeTab === 'flat'" class="flat-specs">
      <div class="row">
        <div *ngFor="let [key, spec] of flatSpecifications" class="col-md-6 col-lg-4 mb-3">
          <div class="spec-card">
            <div class="spec-header">
              <i class="material-icons spec-icon">{{ getTypeIcon(spec.type) }}</i>
              <span class="spec-key">{{ key }}</span>
              <span *ngIf="spec.unit" class="spec-unit">({{ spec.unit }})</span>
            </div>
            <div class="spec-value">{{ formatValue(spec) }}</div>
            <div class="spec-meta">
              <small class="text-muted">
                Type: {{ spec.type }} | 
                Confidence: {{ (spec.confidence * 100) | number:'1.0-0' }}%
                <span *ngIf="spec.hasMultipleValues"> | Multi-value ({{ spec.valueCount }})</span>
              </small>
            </div>
          </div>
        </div>
      </div>
    </div>

    <!-- Metadata View -->
    <div *ngIf="activeTab === 'metadata' && showMetadata" class="metadata-view">
      <div class="row">
        <div class="col-md-6">
          <h6>Parsing Information</h6>
          <ul class="list-unstyled">
            <li><strong>Structure:</strong> {{ specifications.metadata.structure }}</li>
            <li><strong>Processing Time:</strong> {{ specifications.parsingTimeMs }}ms</li>
            <li><strong>Total Rows:</strong> {{ specifications.metadata.totalRows }}</li>
            <li><strong>Data Rows:</strong> {{ specifications.metadata.dataRows }}</li>
            <li><strong>Multi-value Specs:</strong> {{ specifications.metadata.multiValueSpecs }}</li>
          </ul>
        </div>
        <div class="col-md-6">
          <h6>Quality Metrics</h6>
          <ul class="list-unstyled">
            <li><strong>Overall Score:</strong> 
              <span class="badge" [ngClass]="'bg-' + getConfidenceColor(specifications.quality.overallScore)">
                {{ (specifications.quality.overallScore * 100) | number:'1.0-0' }}%
              </span>
            </li>
            <li><strong>Structure Confidence:</strong> {{ (specifications.quality.structureConfidence * 100) | number:'1.0-0' }}%</li>
            <li><strong>Type Detection:</strong> {{ (specifications.quality.typeDetectionAccuracy * 100) | number:'1.0-0' }}%</li>
            <li><strong>Completeness:</strong> {{ (specifications.quality.completenessScore * 100) | number:'1.0-0' }}%</li>
          </ul>
        </div>
      </div>
    </div>
  </div>

  <!-- Empty State -->
  <div *ngIf="specifications.isSuccess && !hasSpecifications" class="alert alert-info">
    <i class="material-icons">info</i>
    No specifications found on this page.
  </div>
</div>
```

**File**: `TechTicker.Frontend/src/app/shared/components/product-specifications/product-specifications.component.scss`

```scss
.product-specifications {
  .spec-header {
    border-bottom: 1px solid #dee2e6;
    padding-bottom: 0.5rem;
  }

  .category-group {
    .category-header {
      background-color: #f8f9fa;
      padding: 0.75rem 1rem;
      border-radius: 0.375rem;
      margin-bottom: 1rem;
      
      .category-title {
        margin: 0;
        color: #495057;
        font-weight: 600;
      }
    }
    
    .category-specs {
      padding-left: 1rem;
    }
  }

  .spec-item, .spec-card {
    background: #ffffff;
    border: 1px solid #e9ecef;
    border-radius: 0.375rem;
    padding: 0.75rem;
    transition: all 0.2s ease;
    
    &:hover {
      border-color: #007bff;
      box-shadow: 0 2px 4px rgba(0,123,255,0.1);
    }
  }

  .spec-key, .spec-header {
    font-weight: 600;
    color: #495057;
    margin-bottom: 0.25rem;
    display: flex;
    align-items: center;
    
    .spec-icon {
      font-size: 1rem;
      margin-right: 0.5rem;
      color: #6c757d;
    }
    
    .spec-unit {
      font-size: 0.875rem;
      color: #6c757d;
      margin-left: 0.25rem;
    }
  }

  .spec-value {
    color: #212529;
    font-size: 0.9rem;
    margin-bottom: 0.25rem;
    word-break: break-word;
  }

  .spec-confidence, .spec-meta {
    font-size: 0.75rem;
    color: #6c757d;
  }

  .spec-quality {
    .badge {
      font-size: 0.75rem;
    }
  }

  .nav-tabs {
    border-bottom: 1px solid #dee2e6;
    
    .nav-link {
      color: #495057;
      border: none;
      border-bottom: 2px solid transparent;
      
      &:hover {
        border-color: transparent;
        background-color: #f8f9fa;
      }
      
      &.active {
        color: #007bff;
        background-color: transparent;
        border-bottom-color: #007bff;
      }
      
      i {
        font-size: 1rem;
      }
    }
  }

  .metadata-view {
    background-color: #f8f9fa;
    padding: 1rem;
    border-radius: 0.375rem;
    
    h6 {
      color: #495057;
      margin-bottom: 0.75rem;
    }
    
    ul {
      li {
        margin-bottom: 0.5rem;
        
        strong {
          color: #495057;
        }
      }
    }
  }
}
```

#### 3.3 Update Product Detail Pages
**File**: `TechTicker.Frontend/src/app/features/products/components/product-detail/product-detail.component.html`

```html
<!-- Add after existing product information -->
<div class="row mt-4">
  <div class="col-12">
    <app-product-specifications 
      [specifications]="productSpecifications"
      [showMetadata]="isAdmin">
    </app-product-specifications>
  </div>
</div>
```

#### 3.4 Update Scraper Logs Display
**File**: `TechTicker.Frontend/src/app/features/scraper-logs/components/scraper-log-detail/scraper-log-detail.component.html`

```html
<!-- Add specification information section -->
<div class="row mt-3" *ngIf="log.specificationData">
  <div class="col-12">
    <div class="card">
      <div class="card-header">
        <h6 class="mb-0">
          <i class="material-icons me-2">description</i>
          Specification Parsing Results
        </h6>
      </div>
      <div class="card-body">
        <div class="row">
          <div class="col-md-6">
            <p><strong>Strategy:</strong> {{ log.specificationParsingStrategy }}</p>
            <p><strong>Specifications Count:</strong> {{ log.specificationCount }}</p>
            <p><strong>Parsing Time:</strong> {{ log.specificationParsingTime }}ms</p>
            <p><strong>Quality Score:</strong> 
              <span class="badge" [ngClass]="getQualityBadgeClass(log.specificationQualityScore)">
                {{ (log.specificationQualityScore * 100) | number:'1.0-0' }}%
              </span>
            </p>
          </div>
          <div class="col-md-6" *ngIf="log.specificationError">
            <div class="alert alert-warning">
              <strong>Error:</strong> {{ log.specificationError }}
            </div>
          </div>
        </div>
        
        <!-- Parsed Specifications Preview -->
        <div class="mt-3" *ngIf="parsedSpecifications">
          <h6>Parsed Specifications Preview</h6>
          <app-product-specifications 
            [specifications]="parsedSpecifications"
            [showMetadata]="true">
          </app-product-specifications>
        </div>
      </div>
    </div>
  </div>
</div>
```

### Phase 4: Database Migration

#### 4.1 Create Migration
**File**: `TechTicker.DataAccess/Migrations/AddSpecificationSupport.cs`

```csharp
public partial class AddSpecificationSupport : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Add specification columns to ScraperRunLog
        migrationBuilder.AddColumn<string>(
            name: "SpecificationData",
            table: "ScraperRunLogs",
            type: "jsonb",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "SpecificationMetadata",
            table: "ScraperRunLogs",
            type: "jsonb",
            nullable: true);

        migrationBuilder.AddColumn<int>(
            name: "SpecificationCount",
            table: "ScraperRunLogs",
            type: "integer",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "SpecificationParsingStrategy",
            table: "ScraperRunLogs",
            type: "text",
            nullable: true);

        migrationBuilder.AddColumn<double>(
            name: "SpecificationQualityScore",
            table: "ScraperRunLogs",
            type: "double precision",
            nullable: true);

        migrationBuilder.AddColumn<long>(
            name: "SpecificationParsingTime",
            table: "ScraperRunLogs",
            type: "bigint",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "SpecificationError",
            table: "ScraperRunLogs",
            type: "text",
            nullable: true);

        // Add specification columns to ProductSellerMapping
        migrationBuilder.AddColumn<string>(
            name: "LatestSpecifications",
            table: "ProductSellerMappings",
            type: "jsonb",
            nullable: true);

        migrationBuilder.AddColumn<DateTime>(
            name: "SpecificationsLastUpdated",
            table: "ProductSellerMappings",
            type: "timestamp with time zone",
            nullable: true);

        migrationBuilder.AddColumn<double>(
            name: "SpecificationsQualityScore",
            table: "ProductSellerMappings",
            type: "double precision",
            nullable: true);

        // Add specification selectors to SiteConfiguration
        migrationBuilder.AddColumn<string>(
            name: "SpecificationTableSelector",
            table: "SiteConfigurations",
            type: "text",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "SpecificationContainerSelector",
            table: "SiteConfigurations",
            type: "text",
            nullable: true);

        migrationBuilder.AddColumn<bool>(
            name: "EnableSpecificationScraping",
            table: "SiteConfigurations",
            type: "boolean",
            nullable: false,
            defaultValue: false);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Remove specification columns
        migrationBuilder.DropColumn(name: "SpecificationData", table: "ScraperRunLogs");
        migrationBuilder.DropColumn(name: "SpecificationMetadata", table: "ScraperRunLogs");
        migrationBuilder.DropColumn(name: "SpecificationCount", table: "ScraperRunLogs");
        migrationBuilder.DropColumn(name: "SpecificationParsingStrategy", table: "ScraperRunLogs");
        migrationBuilder.DropColumn(name: "SpecificationQualityScore", table: "ScraperRunLogs");
        migrationBuilder.DropColumn(name: "SpecificationParsingTime", table: "ScraperRunLogs");
        migrationBuilder.DropColumn(name: "SpecificationError", table: "ScraperRunLogs");
        
        migrationBuilder.DropColumn(name: "LatestSpecifications", table: "ProductSellerMappings");
        migrationBuilder.DropColumn(name: "SpecificationsLastUpdated", table: "ProductSellerMappings");
        migrationBuilder.DropColumn(name: "SpecificationsQualityScore", table: "ProductSellerMappings");
        
        migrationBuilder.DropColumn(name: "SpecificationTableSelector", table: "SiteConfigurations");
        migrationBuilder.DropColumn(name: "SpecificationContainerSelector", table: "SiteConfigurations");
        migrationBuilder.DropColumn(name: "EnableSpecificationScraping", table: "SiteConfigurations");
    }
}
```

### Phase 5: Testing & Quality Assurance

#### 5.1 Unit Tests
**File**: `TechTicker.Application.Tests/Services/SpecificationScrapingServiceTests.cs`

```csharp
[TestClass]
public class SpecificationScrapingServiceTests
{
    private Mock<ITableParser> _mockTableParser;
    private Mock<ILogger<WebScrapingService>> _mockLogger;
    private WebScrapingService _service;

    [TestInitialize]
    public void Setup()
    {
        _mockTableParser = new Mock<ITableParser>();
        _mockLogger = new Mock<ILogger<WebScrapingService>>();
        // ... setup other mocks
        
        _service = new WebScrapingService(
            _mockLogger.Object,
            // ... other dependencies
            _mockTableParser.Object,
            new MemoryCache(new MemoryCacheOptions()));
    }

    [TestMethod]
    public async Task ScrapeProductSpecifications_WithValidSelector_ReturnsSpecifications()
    {
        // Arrange
        var command = new ScrapeProductPageCommand
        {
            ScrapeSpecifications = true,
            Selectors = new ScrapingSelectorsDto
            {
                SpecificationTableSelector = "table.specs"
            }
        };

        var expectedSpecs = new List<ProductSpecification>
        {
            new ProductSpecification
            {
                Specifications = new Dictionary<string, object>
                {
                    ["Graphics Engine"] = "NVIDIA GeForce RTX 4090",
                    ["Memory"] = "24 GB GDDR6X"
                }
            }
        };

        _mockTableParser.Setup(x => x.ParseAsync(It.IsAny<string>(), It.IsAny<ParsingOptions>()))
            .ReturnsAsync(new ParsingResult<List<ProductSpecification>>
            {
                Success = true,
                Data = expectedSpecs
            });

        // Act
        var result = await _service.ScrapeProductPageAsync(command);

        // Assert
        Assert.IsTrue(result.Specifications.IsSuccess);
        Assert.AreEqual(2, result.Specifications.Specifications.Count);
    }
}
```

#### 5.2 Integration Tests
**File**: `TechTicker.Application.Tests/Integration/SpecificationScrapingIntegrationTests.cs`

```csharp
[TestClass]
public class SpecificationScrapingIntegrationTests
{
    [TestMethod]
    public async Task EndToEndSpecificationScraping_AmazonProduct_ParsesCorrectly()
    {
        // Test with real Amazon product page HTML
        var html = LoadTestHtml("amazon-rtx4090.html");
        var parser = new UniversalTableParser();
        
        var result = await parser.ParseAsync(html);
        
        Assert.IsTrue(result.Success);
        Assert.IsTrue(result.Data.Any());
        Assert.IsTrue(result.Data.First().Specifications.ContainsKey("Graphics Engine"));
    }
}
```

### Phase 6: Deployment & Configuration

#### 6.1 Configuration Updates
**File**: `TechTicker.ApiService/appsettings.json`

```json
{
  "SpecificationScraping": {
    "DefaultCacheExpiry": "24:00:00",
    "MaxCacheEntries": 1000,
    "EnableDetailedLogging": true,
    "QualityThreshold": 0.7
  }
}
```

#### 6.2 Documentation Updates
- Update API documentation with new specification endpoints
- Add specification selector examples for common sites
- Create troubleshooting guide for specification parsing issues

## Success Metrics

### Technical Metrics
- **Parsing Success Rate**: >85% for major e-commerce sites
- **Quality Score**: Average >0.7 for parsed specifications
- **Performance**: <2 seconds for specification parsing
- **Coverage**: Support for 5+ major vendor table formats

### Business Metrics
- **User Engagement**: Increased time on product pages
- **Data Completeness**: >90% of products have specifications
- **Support Reduction**: Fewer queries about product details

## Risk Mitigation

### Technical Risks
- **Parsing Failures**: Graceful degradation, detailed error logging
- **Performance Impact**: Caching, async processing, optional feature
- **Memory Usage**: Cache size limits, TTL expiration

### Business Risks
- **Site Structure Changes**: Monitoring, alerting, easy selector updates
- **Legal Compliance**: Respect robots.txt, rate limiting

## Future Enhancements

### Phase 7: Advanced Features
- **AI-Powered Specification Extraction**: Use LLM for unstructured data
- **Specification Comparison**: Compare products side-by-side
- **Specification Search**: Search products by specifications
- **Specification Validation**: Cross-reference with official specs

### Phase 8: Analytics & Insights
- **Specification Trends**: Track specification changes over time
- **Quality Analytics**: Monitor parsing quality across sites
- **Performance Metrics**: Detailed parsing performance analytics

## Conclusion

This implementation plan provides a comprehensive approach to integrating product specification scraping into the TechTicker system. The plan leverages existing infrastructure while adding robust specification parsing capabilities that will enhance the user experience and provide valuable product data for analysis and comparison. 