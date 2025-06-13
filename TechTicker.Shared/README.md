# TechTicker.Shared

This project contains common response models, exception handling logic, and utilities shared across all TechTicker microservices.

## Features

### 1. Common Response Models

#### ApiResponse<T>
Generic API response wrapper for consistent response structure:

```csharp
// Success with data
var response = ApiResponse<Product>.SuccessResult(product, "Product retrieved successfully");

// Success without data
var response = ApiResponse.SuccessResult("Operation completed successfully");

// Failure with error message
var response = ApiResponse<Product>.FailureResult("Product not found", 404);

// Failure with multiple errors
var response = ApiResponse<Product>.FailureResult(
    new List<string> { "Name is required", "Price must be positive" },
    "Validation failed"
);
```

#### PagedResponse<T>
For paginated API responses:

```csharp
var response = PagedResponse<Product>.SuccessResult(
    products, 
    pageNumber: 1, 
    pageSize: 10, 
    totalCount: 100
);
```

### 2. Exception Handling

#### Custom Exceptions
- `NotFoundException`: When a resource is not found
- `ValidationException`: When validation fails
- `BusinessRuleException`: When business rules are violated
- `ConflictException`: When a conflict occurs (e.g., duplicate resource)
- `UnauthorizedException`: When operation is not authorized
- `AuthenticationException`: When authentication fails
- `ExternalServiceException`: When external service communication fails

```csharp
// Usage examples
throw new NotFoundException("Product", productId);
throw new ValidationException("Name", "Product name is required");
throw new BusinessRuleException("Cannot delete product with existing orders");
throw new ConflictException("Product", "SKU already exists");
```

#### Global Exception Handling Middleware
Automatically converts exceptions to consistent API responses:

```csharp
// In Program.cs or Startup.cs
app.UseTechTickerExceptionHandling();
```

### 3. Result Pattern

The `Result<T>` class provides a clean way to handle operations that may succeed or fail:

```csharp
// Service method returning Result
public async Task<Result<Product>> GetProductAsync(Guid id)
{
    var product = await _repository.GetByIdAsync(id);
    if (product == null)
        return Result<Product>.Failure("Product not found", "RESOURCE_NOT_FOUND");
        
    return Result<Product>.Success(product);
}

// Controller using Result
[HttpGet("{id}")]
public async Task<IActionResult> GetProduct(Guid id)
{
    var result = await _productService.GetProductAsync(id);
    return HandleResult(result);
}
```

### 4. Base Controller

Inherit from `BaseApiController` for consistent response handling:

```csharp
[Route("api/[controller]")]
public class ProductsController : BaseApiController
{
    [HttpGet("{id}")]
    public async Task<IActionResult> GetProduct(Guid id)
    {
        var product = await _productService.GetProductAsync(id);
        return Ok(product, "Product retrieved successfully");
    }

    [HttpGet]
    public async Task<IActionResult> GetProducts([FromQuery] PaginationRequest request)
    {
        var (products, totalCount) = await _productService.GetProductsAsync(request);
        return OkPaged(products, request.PageNumber, request.PageSize, totalCount);
    }
}
```

### 5. Utilities

#### String Utilities
```csharp
var slug = StringUtilities.ToSlug("Product Name With Spaces"); // "product-name-with-spaces"
var masked = StringUtilities.MaskSensitiveInfo("user@example.com"); // "us********om"
var truncated = StringUtilities.Truncate("Very long text...", 10); // "Very lo..."
```

#### Validation Utilities
```csharp
var isValidEmail = ValidationUtilities.IsValidEmail("user@example.com");
var isValidPhone = ValidationUtilities.IsValidPhoneNumber("+1234567890");
var isValidUrl = ValidationUtilities.IsValidUrl("https://example.com");
var isValidGuid = ValidationUtilities.IsValidGuid("123e4567-e89b-12d3-a456-426614174000");
```

#### JSON Utilities
```csharp
var json = JsonUtilities.Serialize(product);
var product = JsonUtilities.Deserialize<Product>(json);
var productOrDefault = JsonUtilities.DeserializeOrDefault(json, new Product());
```

#### Hash Utilities
```csharp
var md5Hash = HashUtilities.GenerateMD5("input string");
var sha256Hash = HashUtilities.GenerateSHA256("input string");
var sha512Hash = HashUtilities.GenerateSHA512("input string");
```

### 6. Setup and Configuration

#### 1. Add Package Reference
In your microservice project, add a reference to TechTicker.Shared:

```xml
<ProjectReference Include="..\TechTicker.Shared\TechTicker.Shared.csproj" />
```

#### 2. Configure Services
In your `Program.cs`:

```csharp
using TechTicker.Shared.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Add shared services
builder.Services.AddTechTickerShared();

var app = builder.Build();

// Add exception handling middleware (should be early in pipeline)
app.UseTechTickerExceptionHandling();

// Add correlation ID middleware
app.UseCorrelationId();

// Other middleware...
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.Run();
```

#### 3. Use Base Controller
Make your controllers inherit from `BaseApiController`:

```csharp
[Route("api/[controller]")]
public class YourController : BaseApiController
{
    // Your controller actions
}
```

### 7. Constants

Use predefined constants from `ApplicationConstants` and `TechTickerConstants`:

```csharp
// HTTP headers
Request.Headers[ApplicationConstants.Headers.CorrelationId]

// Error codes
throw new NotFoundException("Product not found", ApplicationConstants.ErrorCodes.ResourceNotFound);

// Validation limits
if (name.Length > TechTickerConstants.Products.MaxNameLength)
    throw new ValidationException("Name", "Name is too long");
```

## Best Practices

1. **Always use Result pattern in service layer** for operations that can fail
2. **Inherit from BaseApiController** for consistent response formatting
3. **Use custom exceptions** instead of throwing generic exceptions
4. **Apply GlobalExceptionHandlingMiddleware** early in the pipeline
5. **Use correlation IDs** for request tracing across services
6. **Validate input** using the provided validation utilities
7. **Follow consistent naming** using the provided constants

## Migration Guide

To migrate existing controllers to use the shared components:

1. Change controller base class to `BaseApiController`
2. Replace direct `Ok()` calls with the base controller methods
3. Update exception handling to use custom exceptions
4. Wrap service responses in `Result<T>` pattern
5. Add the global exception handling middleware

Example migration:

```csharp
// Before
[HttpGet("{id}")]
public async Task<IActionResult> GetProduct(Guid id)
{
    try
    {
        var product = await _productService.GetProductAsync(id);
        if (product == null)
            return NotFound();
        return Ok(product);
    }
    catch (Exception ex)
    {
        return BadRequest(ex.Message);
    }
}

// After
[HttpGet("{id}")]
public async Task<IActionResult> GetProduct(Guid id)
{
    var result = await _productService.GetProductAsync(id);
    return HandleResult(result);
}
```

This shared library ensures consistency, reduces code duplication, and provides a solid foundation for building robust APIs across all TechTicker microservices.
