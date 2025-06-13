using Grpc.Core;
using TechTicker.Grpc.Contracts.Products;
using TechTicker.ProductService.Services;
using Google.Protobuf.WellKnownTypes;
using System.Text.Json;

namespace TechTicker.ProductService.Grpc
{
    /// <summary>
    /// gRPC service implementation for Product operations
    /// </summary>
    public class ProductGrpcServiceImpl : ProductGrpcService.ProductGrpcServiceBase
    {
        private readonly IProductService _productService;
        private readonly ICategoryService _categoryService;
        private readonly ILogger<ProductGrpcServiceImpl> _logger;

        public ProductGrpcServiceImpl(
            IProductService productService,
            ICategoryService categoryService,
            ILogger<ProductGrpcServiceImpl> logger)
        {
            _productService = productService;
            _categoryService = categoryService;
            _logger = logger;
        }

        public override async Task<ProductResponse> GetProduct(GetProductRequest request, ServerCallContext context)
        {
            _logger.LogInformation("gRPC GetProduct called for ID: {ProductId}", request.ProductId);

            if (!Guid.TryParse(request.ProductId, out var productId))
            {
                throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid product ID format"));
            }

            var result = await _productService.GetProductByIdAsync(productId);
            
            if (result.IsFailure)
            {
                var statusCode = result.ErrorCode switch
                {
                    "RESOURCE_NOT_FOUND" => StatusCode.NotFound,
                    "VALIDATION_FAILED" => StatusCode.InvalidArgument,
                    _ => StatusCode.Internal
                };
                throw new RpcException(new Status(statusCode, result.ErrorMessage ?? "Unknown error"));
            }

            return MapToGrpcProductResponse(result.Data!);
        }

        public override async Task<GetProductsResponse> GetProducts(GetProductsRequest request, ServerCallContext context)
        {
            _logger.LogInformation("gRPC GetProducts called for {Count} products", request.ProductIds.Count);

            var productIds = new List<Guid>();
            foreach (var productIdString in request.ProductIds)
            {
                if (Guid.TryParse(productIdString, out var productId))
                {
                    productIds.Add(productId);
                }
            }

            var products = new List<ProductResponse>();
            
            // Get products in batch (could be optimized with a batch method in the service)
            foreach (var productId in productIds)
            {
                var result = await _productService.GetProductByIdAsync(productId);
                if (result.IsSuccess && result.Data != null)
                {
                    products.Add(MapToGrpcProductResponse(result.Data));
                }
            }

            return new GetProductsResponse
            {
                Products = { products }
            };
        }

        public override async Task<ProductExistsResponse> ProductExists(ProductExistsRequest request, ServerCallContext context)
        {
            _logger.LogInformation("gRPC ProductExists called for ID: {ProductId}", request.ProductId);

            if (!Guid.TryParse(request.ProductId, out var productId))
            {
                return new ProductExistsResponse { Exists = false };
            }

            // We can implement a more efficient exists check in the service layer
            var result = await _productService.GetProductByIdAsync(productId);
            
            return new ProductExistsResponse { Exists = result.IsSuccess };
        }

        public override async Task<CategoryResponse> GetCategory(GetCategoryRequest request, ServerCallContext context)
        {
            _logger.LogInformation("gRPC GetCategory called for ID: {CategoryId}", request.CategoryId);

            var result = await _categoryService.GetCategoryByIdOrSlugAsync(request.CategoryId, includeProductCount: true);
            
            if (result.IsFailure)
            {
                var statusCode = result.ErrorCode switch
                {
                    "RESOURCE_NOT_FOUND" => StatusCode.NotFound,
                    "VALIDATION_FAILED" => StatusCode.InvalidArgument,
                    _ => StatusCode.Internal
                };
                throw new RpcException(new Status(statusCode, result.ErrorMessage ?? "Unknown error"));
            }

            return MapToGrpcCategoryResponse(result.Data!);
        }

        public override async Task<GetCategoriesResponse> GetCategories(GetCategoriesRequest request, ServerCallContext context)
        {
            _logger.LogInformation("gRPC GetCategories called for {Count} categories", request.CategoryIds.Count);

            var categories = new List<CategoryResponse>();
            
            // Get categories in batch
            foreach (var categoryIdString in request.CategoryIds)
            {
                var result = await _categoryService.GetCategoryByIdOrSlugAsync(categoryIdString, includeProductCount: true);
                if (result.IsSuccess && result.Data != null)
                {
                    categories.Add(MapToGrpcCategoryResponse(result.Data));
                }
            }

            return new GetCategoriesResponse
            {
                Categories = { categories }
            };
        }

        public override async Task<CategoryExistsResponse> CategoryExists(CategoryExistsRequest request, ServerCallContext context)
        {
            _logger.LogInformation("gRPC CategoryExists called for ID: {CategoryId}", request.CategoryId);

            var result = await _categoryService.GetCategoryByIdOrSlugAsync(request.CategoryId, includeProductCount: false);
            
            return new CategoryExistsResponse { Exists = result.IsSuccess };
        }

        public override async Task StreamProductUpdates(Empty request, IServerStreamWriter<ProductUpdateEvent> responseStream, ServerCallContext context)
        {
            _logger.LogInformation("gRPC StreamProductUpdates started");

            // This would typically connect to a message broker or event system
            // For now, we'll implement a simple polling mechanism as an example
            
            while (!context.CancellationToken.IsCancellationRequested)
            {
                try
                {
                    // In a real implementation, this would listen to actual product update events
                    // from a message broker like RabbitMQ, Azure Service Bus, etc.
                    
                    await Task.Delay(TimeSpan.FromSeconds(30), context.CancellationToken);
                    
                    // Example: Send a heartbeat or check for actual updates
                    // This is just a placeholder implementation
                }
                catch (OperationCanceledException)
                {
                    _logger.LogInformation("gRPC StreamProductUpdates cancelled");
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in StreamProductUpdates");
                    break;
                }
            }
        }

        private static ProductResponse MapToGrpcProductResponse(DTOs.ProductResponse product)
        {
            var grpcProduct = new ProductResponse
            {
                ProductId = product.ProductId.ToString(),
                Name = product.Name,
                Manufacturer = product.Manufacturer,
                ModelNumber = product.ModelNumber,
                Sku = product.SKU ?? string.Empty,
                Description = product.Description ?? string.Empty,
                IsActive = product.IsActive,
                CreatedAt = Timestamp.FromDateTimeOffset(product.CreatedAt),
                UpdatedAt = Timestamp.FromDateTimeOffset(product.UpdatedAt)
            };

            if (product.Category != null)
            {
                grpcProduct.Category = MapToGrpcCategoryResponse(product.Category);
            }

            if (product.Specifications.HasValue && product.Specifications.Value.ValueKind == JsonValueKind.Object)
            {
                foreach (var spec in product.Specifications.Value.EnumerateObject())
                {
                    grpcProduct.Specifications[spec.Name] = spec.Value.ToString();
                }
            }

            return grpcProduct;
        }

        private static CategoryResponse MapToGrpcCategoryResponse(DTOs.CategoryResponse category)
        {
            return new CategoryResponse
            {
                CategoryId = category.CategoryId.ToString(),
                Name = category.Name,
                Slug = category.Slug,
                Description = category.Description ?? string.Empty,
                ProductCount = category.ProductCount ?? 0,
                CreatedAt = Timestamp.FromDateTimeOffset(category.CreatedAt),
                UpdatedAt = Timestamp.FromDateTimeOffset(category.UpdatedAt)
            };
        }
    }
}
