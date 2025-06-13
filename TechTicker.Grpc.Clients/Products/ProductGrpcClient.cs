using Grpc.Core;
using Microsoft.Extensions.Logging;
using TechTicker.Grpc.Contracts.Products;

namespace TechTicker.Grpc.Clients.Products
{
    /// <summary>
    /// Client implementation for Product gRPC service operations
    /// </summary>
    public class ProductGrpcClient : IProductGrpcClient
    {
        private readonly ProductGrpcService.ProductGrpcServiceClient _client;
        private readonly ILogger<ProductGrpcClient> _logger;

        public ProductGrpcClient(
            ProductGrpcService.ProductGrpcServiceClient client,
            ILogger<ProductGrpcClient> logger)
        {
            _client = client;
            _logger = logger;
        }

        public async Task<ProductResponse?> GetProductAsync(string productId, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Getting product {ProductId} via gRPC", productId);

                var request = new GetProductRequest { ProductId = productId };
                var response = await _client.GetProductAsync(request, cancellationToken: cancellationToken);

                return response;
            }
            catch (RpcException ex) when (ex.StatusCode == StatusCode.NotFound)
            {
                _logger.LogWarning("Product {ProductId} not found", productId);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting product {ProductId} via gRPC", productId);
                throw;
            }
        }

        public async Task<IEnumerable<ProductResponse>> GetProductsAsync(IEnumerable<string> productIds, CancellationToken cancellationToken = default)
        {
            try
            {
                var productIdList = productIds.ToList();
                _logger.LogInformation("Getting {Count} products via gRPC", productIdList.Count);

                var request = new GetProductsRequest();
                request.ProductIds.AddRange(productIdList);

                var response = await _client.GetProductsAsync(request, cancellationToken: cancellationToken);

                return response.Products;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting products via gRPC");
                throw;
            }
        }

        public async Task<bool> ProductExistsAsync(string productId, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Checking if product {ProductId} exists via gRPC", productId);

                var request = new ProductExistsRequest { ProductId = productId };
                var response = await _client.ProductExistsAsync(request, cancellationToken: cancellationToken);

                return response.Exists;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if product {ProductId} exists via gRPC", productId);
                return false;
            }
        }

        public async Task<CategoryResponse?> GetCategoryAsync(string categoryIdOrSlug, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Getting category {CategoryId} via gRPC", categoryIdOrSlug);

                var request = new GetCategoryRequest { CategoryId = categoryIdOrSlug };
                var response = await _client.GetCategoryAsync(request, cancellationToken: cancellationToken);

                return response;
            }
            catch (RpcException ex) when (ex.StatusCode == StatusCode.NotFound)
            {
                _logger.LogWarning("Category {CategoryId} not found", categoryIdOrSlug);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting category {CategoryId} via gRPC", categoryIdOrSlug);
                throw;
            }
        }

        public async Task<IEnumerable<CategoryResponse>> GetCategoriesAsync(IEnumerable<string> categoryIds, CancellationToken cancellationToken = default)
        {
            try
            {
                var categoryIdList = categoryIds.ToList();
                _logger.LogInformation("Getting {Count} categories via gRPC", categoryIdList.Count);

                var request = new GetCategoriesRequest();
                request.CategoryIds.AddRange(categoryIdList);

                var response = await _client.GetCategoriesAsync(request, cancellationToken: cancellationToken);

                return response.Categories;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting categories via gRPC");
                throw;
            }
        }

        public async Task<bool> CategoryExistsAsync(string categoryId, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Checking if category {CategoryId} exists via gRPC", categoryId);

                var request = new CategoryExistsRequest { CategoryId = categoryId };
                var response = await _client.CategoryExistsAsync(request, cancellationToken: cancellationToken);

                return response.Exists;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if category {CategoryId} exists via gRPC", categoryId);
                return false;
            }
        }
    }
}
