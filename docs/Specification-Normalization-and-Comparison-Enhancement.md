# Specification Normalization and Comparison Enhancement Plan

## 1. Overview

This document outlines a plan to enhance the product specification system in TechTicker. The current system faces challenges with inconsistent specification keys scraped from different e-commerce sites, which hinders effective product comparison.

The goal is to create a robust system that:
- **Normalizes** scraped specification keys to a canonical format.
- **Standardizes** specification structures per product category.
- **Enables** reliable and accurate side-by-side product comparison.

As seen in the example scraping, keys like `GPU`, `Graphics Processing`, `MEMORY SIZE`, and `Memory Size` refer to the same attributes but are stored as separate specifications. This plan addresses this issue directly.

## 2. Proposed Architecture: Canonical Specification Model

The core of the new system will be a **Canonical Specification Model**. This model will be defined for each product category (e.g., "Graphics Cards", "CPUs", "Motherboards") and will serve as the single source of truth for all product specifications within that category.

### 2.1. Canonical Model Structure

A canonical model will be defined in a JSON file and represented by the following C# classes:

```csharp
// In TechTicker.Domain/Entities/Canonical
public class CanonicalSpecificationTemplate
{
    public string Category { get; set; }
    public List<CanonicalProperty> Properties { get; set; }
}

public class CanonicalProperty
{
    public string CanonicalName { get; set; } // e.g., "memory_size_gb"
    public string DisplayName { get; set; }   // e.g., "Memory Size"
    public List<string> Aliases { get; set; } // e.g., ["memory size", "mem size", "ram"]
    public SpecificationType DataType { get; set; } // string, number, dimensions, etc.
    public string Unit { get; set; }          // "GB", "MHz", "mm"
    public bool IsPrimaryIdentifier { get; set; } // Helps resolve ambiguity
}

// In TechTicker.Domain/Entities/Canonical
public class NormalizedSpecificationValue
{
    public object Value { get; set; }
    public string Unit { get; set; }
    public SpecificationType DataType { get; set; }
    public string RawValue { get; set; }
    public string CanonicalName { get; set; }
    public double Confidence { get; set; }
}
```

### 2.2. Normalization Flow

The scraping and normalization process will be updated as follows:

1.  **Load Template**: When parsing a product's specifications, load the appropriate `CanonicalSpecificationTemplate` based on the product's category.
2.  **Scrape Raw Data**: Scrape the key-value pairs from the source HTML as is currently done.
3.  **Normalize Key**: For each scraped key:
    a.  Perform initial syntactic cleaning (lowercase, trim).
    b.  Search for the cleaned key in the `Aliases` list of all properties in the canonical template.
4.  **Map to Canonical Property**:
    a.  If a match is found, the scraped data is mapped to the corresponding `CanonicalName`.
    b.  The value is then parsed, converted to the standard unit (if applicable), and validated against the defined `DataType`.
5.  **Store Normalized Data**: The final, normalized specifications will be stored. The raw scraped data can be kept for debugging purposes, but the normalized version will be used for display and comparison.

This approach centralizes the normalization logic into configurable JSON files, making it easy to update and expand without changing C# code.

## 3. Edge Case Handling and Advanced Scenarios

To build a truly robust system, the following edge cases and scenarios must be addressed.

### 3.1. Key Matching Ambiguity
- **Problem**: A scraped key might match aliases for multiple canonical properties (e.g., "size" could map to "Card Size" and "Memory Size").
- **Solution**:
    1.  **Contextual Scoring**: The `SpecificationNormalizer` will use a scoring algorithm. A match found alongside other properties from the same category gets a higher score.
    2.  **Primary Identifier**: The `IsPrimaryIdentifier` flag in `CanonicalProperty` will give precedence to critical properties like "Model" or "SKU" to resolve ties.
    3.  **Logging**: Any ambiguities that cannot be resolved with high confidence will be logged for manual review.

### 3.2. Unmatched Keys
- **Problem**: A new or rare specification key is scraped that has no alias in the canonical template.
- **Solution**:
    1.  **"Uncategorized" Bucket**: Unmatched key-value pairs will be stored in a separate `UncategorizedSpecifications` JSONB column in the `Product` entity.
    2.  **Feedback Loop**: A weekly report or a dashboard will highlight the most frequent unmatched keys, allowing an administrator to update the canonical templates.

### 3.3. Value Parsing and Unit Conversion
- **Problem**: Values may come in different units (e.g., "8 GB", "8192 MB") or contain complex, multi-part information (e.g., "2505 MHz (Reference card: 2460 MHz)").
- **Solution**:
    1.  **Unit Conversion Engine**: The `SpecificationNormalizer` will include a sub-component for unit conversion. The canonical template will define a standard unit for each property (e.g., "GB" for memory size). The normalizer will parse the scraped value and convert it to the standard unit.
    2.  **Complex Value Parsers**: For specific `DataType`s like `Compound`, custom regex or logic will be used to extract multiple data points from a single string. For the clock speed example, it would extract both the "Core Clock" and the "Reference Clock".

### 3.4. New Product Categories
- **Problem**: A product is scraped from a category for which no canonical template exists.
- **Solution**:
    1.  **Fallback Mechanism**: If no template is found, the system will save all scraped specifications to the `UncategorizedSpecifications` bucket.
    2.  **Alerting**: An alert will be triggered for a system administrator to create a new canonical template for the new category.

## 4. Implementation Plan

### Step 1: Define Canonical Models

- **Create C# Classes**: Implement the `CanonicalSpecificationTemplate`, `CanonicalProperty`, and `NormalizedSpecificationValue` classes in the `TechTicker.Domain` project.
- **Create JSON Files**:
    - Create a new directory: `TechTicker.Shared/CanonicalSpecs/`.
    - Inside, create `graphics_card.json` with a comprehensive list of properties and aliases for graphics cards.
    - **Example `graphics_card.json` entry**:
      ```json
      {
        "CanonicalName": "cuda_cores",
        "DisplayName": "CUDA Cores",
        "Aliases": ["cuda cores", "cudaæ cores", "gpu core (cuda core)"],
        "DataType": "Number",
        "Unit": null,
        "IsPrimaryIdentifier": false
      }
      ```

### Step 2: Create a Specification Normalization Service

- **Create `SpecificationNormalizer`**: In `TechTicker.Application`, create a new service `SpecificationNormalizer`.
- **Responsibilities**:
    - Caching canonical templates to avoid file I/O on every run.
    - Implementing the key matching, value parsing, and unit conversion logic.
    - Taking raw scraped specifications and a category as input.
    - Returning a dictionary of normalized `[CanonicalName, NormalizedSpecificationValue]` pairs and a list of uncategorized pairs.

### Step 3: Integrate Normalization into the Scraping Process

- **Modify `UniversalTableParser`**:
    - Inject the `ISpecificationNormalizer` service.
    - After parsing a table into a `ProductSpecification` object, pass the result to the normalizer.
    - The `ProcessUniversalTableAsync` method in `HtmlUtilities.cs` is the ideal place for this integration.
    - The final `ProductSpecification` object should contain the normalized and uncategorized data.

### Step 4: Update Database and Data Access

- **Modify `Product` Entity**: Add two new JSONB columns to the `Product` entity in `TechTicker.Domain` to store the normalized and uncategorized specifications.
  ```csharp
  public Dictionary<string, NormalizedSpecificationValue> NormalizedSpecifications { get; set; }
  public Dictionary<string, string> UncategorizedSpecifications { get; set; }
  ```
- **Update `TechTickerDbContext`**: Configure the new properties using `.HasColumnType("jsonb")`.
- **Create Migration**: Generate a new EF Core migration to apply the schema change.

### Step 5: Data Backfilling and Maintenance
- **Create a One-Time Migration Service**: Develop a background service or a CLI command that can be run once to process all existing products in the database. This service will apply the normalization logic to their current specifications and populate the new `NormalizedSpecifications` and `UncategorizedSpecifications` fields.
- **Admin Interface**: (Future Enhancement) Create a simple admin UI to view uncategorized specifications and easily add new aliases to the JSON templates.

### Step 6: Enhance the Product Comparison Feature

- **Update Comparison API**: Modify the API endpoint for product comparison to use the `NormalizedSpecifications`.
- **Update Frontend**: The Angular frontend (`CompareProducts` component) will now receive structured, consistent data. This simplifies the UI logic, as it can iterate through a predictable set of keys for any two products in the same category. The display will be cleaner and the comparisons more accurate.

## 5. Benefits

- **Accuracy**: Eliminates data duplication and ensures comparisons are made on an "apples-to-apples" basis.
- **Maintainability**: Normalization rules are stored in JSON, not hard-coded. Adding new aliases or properties is easy.
- **Scalability**: The system can be easily extended to support new product categories by simply adding new JSON template files.
- **Improved User Experience**: The product comparison feature will become significantly more reliable and useful to the end-user.

## 6. Implementation Status

| Step | Description | Status |
|------|-------------|--------|
| 1 | Define Canonical Models | ✅ Completed |
| 2 | Create Specification Normalization Service | ✅ Completed |
| 3 | Integrate Normalization into the Scraping Process | ✅ Completed |
| 4 | Update Database and Data Access | ✅ Completed |
| 5 | Data Backfilling and Maintenance | ✅ Completed |
| 6 | Enhance the Product Comparison Feature | ✅ Completed |
