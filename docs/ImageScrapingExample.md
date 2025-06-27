# Image Scraping Implementation

## Overview

The image scraping functionality has been implemented to download and store product images locally in a structured directory format. Images are organized by product ID and stored in the `C:\TechTicker\Images\Products\{productId}\` directory.

## Directory Structure

```
C:\TechTicker\Images\Products\
└── {productId}\
    ├── {guid1}.jpg
    ├── {guid2}.png
    └── {guid3}.webp
```

## Configuration

### Image Storage Settings (appsettings.json)

```json
{
  "ImageStorage": {
    "BasePath": "C:\\TechTicker\\Images\\Products"
  },
  "ImageScraping": {
    "MaxImagesPerProduct": 5,
    "MaxImageSizeBytes": 10485760,
    "AllowedContentTypes": ["image/jpeg", "image/jpg", "image/png", "image/webp", "image/gif"],
    "DownloadTimeoutSeconds": 60
  }
}
```

### Site Configuration Example

For vishalperipherals.com, add the following configuration:

```sql
INSERT INTO "ScraperSiteConfigurations" (
    "SiteConfigId",
    "SiteDomain", 
    "ProductNameSelector",
    "PriceSelector",
    "StockSelector",
    "ImageSelector",
    "IsEnabled",
    "CreatedAt",
    "UpdatedAt"
) VALUES (
    gen_random_uuid(),
    'vishalperipherals.com',
    'h1.product-title, .product-name',
    '.price, .product-price',
    '.stock-status, .availability',
    '.slick-slide img',
    true,
    NOW(),
    NOW()
);
```

## Image Selector Examples

### vishalperipherals.com
- **Selector**: `.slick-slide img`
- **Description**: Targets images within the product image slider
- **Attributes extracted**: `data-zoom-image`, `src`, `srcset`

### Amazon-style sites
- **Selector**: `#landingImage, .a-dynamic-image`
- **Description**: Main product image and dynamic images

### Generic e-commerce
- **Selector**: `.product-image img, .gallery-image img`
- **Description**: Common product image containers

## Database Schema Changes

### Product Entity
- `PrimaryImageUrl` (string): Main product image path
- `AdditionalImageUrls` (JSON): Array of additional image paths
- `OriginalImageUrls` (JSON): Array of original scraped URLs for reference
- `ImageLastUpdated` (DateTime): Timestamp of last image update

### ScraperSiteConfiguration Entity
- `ImageSelector` (string): CSS selector for finding product images

## How It Works

1. **Image Discovery**: Uses CSS selectors to find image elements on the product page
2. **URL Extraction**: Extracts image URLs from multiple attributes (data-zoom-image, src, srcset)
3. **Image Download**: Downloads images using HttpClient with timeout and size limits
4. **Local Storage**: Saves images to organized directory structure with unique filenames
5. **Database Storage**: Stores relative paths in the Product entity

## Image Processing Features

- **Multiple Format Support**: JPEG, PNG, WebP, GIF, BMP
- **Size Validation**: Maximum 10MB per image
- **Duplicate Prevention**: Unique filenames prevent conflicts
- **High-Resolution Priority**: Prefers zoom images and high-resolution variants
- **Parallel Processing**: Downloads multiple images concurrently with rate limiting
- **Error Handling**: Graceful failure handling for individual images

## Usage Example

```csharp
// The scraping service automatically handles images when ImageSelector is configured
var command = new ScrapeProductPageCommand
{
    MappingId = mappingId,
    CanonicalProductId = productId,
    SellerName = "Vishal Peripherals",
    ExactProductUrl = "https://vishalperipherals.com/products/rtx-5090",
    Selectors = new ScrapingSelectors
    {
        ProductNameSelector = "h1.product-title",
        PriceSelector = ".price",
        StockSelector = ".stock-status",
        ImageSelector = ".slick-slide img" // This enables image scraping
    }
};

// Images will be automatically downloaded and stored during scraping
var result = await webScrapingService.ScrapeProductPageAsync(command);

// Access image URLs from the result
var primaryImage = result.PrimaryImageUrl; // e.g., "images/products/123e4567-e89b-12d3-a456-426614174000/abc123.jpg"
var additionalImages = result.AdditionalImageUrls; // List of additional image paths
```

## Serving Images

To serve the images via your web application, configure static file serving:

```csharp
// In Program.cs - Configure static file serving for images from C drive
var imageStoragePath = builder.Configuration["ImageStorage:BasePath"] ?? "C:\\TechTicker\\Images\\Products";
if (Directory.Exists(imageStoragePath))
{
    app.UseStaticFiles(new StaticFileOptions
    {
        FileProvider = new PhysicalFileProvider(imageStoragePath),
        RequestPath = "/images/products"
    });
}

app.UseStaticFiles(); // This will serve files from wwwroot

// Images will be accessible at: https://yourapp.com/images/products/{productId}/{filename}
```

## Monitoring and Logging

The image scraping service provides detailed logging:
- Image discovery and extraction
- Download progress and failures
- Storage operations
- Performance metrics

Check logs for troubleshooting image scraping issues.
