using Microsoft.EntityFrameworkCore;
using TechTicker.ProductSellerMappingService.Data;
using TechTicker.ProductSellerMappingService.DTOs;
using TechTicker.Shared.Common;
using TechTicker.Shared.Exceptions;
using TechTicker.Shared.Models;
using TechTicker.Shared.Utilities;
using TechTicker.Grpc.Clients.Products;

namespace TechTicker.ProductSellerMappingService.Services
{    /// <summary>
    /// Service for managing product seller mappings
    /// </summary>
    public class ProductSellerMappingService : IProductSellerMappingService
    {
        private readonly ProductSellerMappingDbContext _context;
        private readonly IProductGrpcClient _productGrpcClient;

        public ProductSellerMappingService(ProductSellerMappingDbContext context, IProductGrpcClient productGrpcClient)
        {
            _context = context;
            _productGrpcClient = productGrpcClient;
        }        public async Task<Result<ProductSellerMappingResponse>> CreateMappingAsync(CreateProductSellerMappingRequest request)
        {
            try
            {
                // Validate that the canonical product exists via gRPC
                var productExists = await _productGrpcClient.ProductExistsAsync(request.CanonicalProductId.ToString());
                if (!productExists)
                {
                    return Result<ProductSellerMappingResponse>.Failure(
                        $"Product with ID {request.CanonicalProductId} does not exist.",
                        "PRODUCT_NOT_FOUND"
                    );
                }

                // Check for duplicate mapping
                if (await IsDuplicateMappingAsync(request.CanonicalProductId, request.SellerName))
                {
                    return Result<ProductSellerMappingResponse>.Failure(
                        "A mapping for this product and seller already exists.",
                        "CONFLICT"
                    );
                }

                var mapping = new ProductSellerMapping
                {
                    MappingId = Guid.NewGuid(),
                    CanonicalProductId = request.CanonicalProductId,
                    SellerName = request.SellerName,                    ExactProductUrl = request.ExactProductUrl,
                    IsActiveForScraping = request.IsActiveForScraping,
                    ScrapingFrequencyOverride = request.ScrapingFrequencyOverride,
                    SiteConfigId = request.SiteConfigId,
                    CreatedAt = DateTimeOffset.UtcNow,
                    UpdatedAt = DateTimeOffset.UtcNow
                };

                _context.ProductSellerMappings.Add(mapping);
                await _context.SaveChangesAsync();

                var response = MapToResponse(mapping);
                return Result<ProductSellerMappingResponse>.Success(response);
            }
            catch (Exception ex)
            {
                return Result<ProductSellerMappingResponse>.Failure(ex);
            }
        }

        public async Task<Result<PagedResponse<ProductSellerMappingResponse>>> GetMappingsAsync(ProductSellerMappingQueryRequest query)
        {
            try
            {
                var queryable = _context.ProductSellerMappings.AsQueryable();

                // Apply filters
                if (query.CanonicalProductId.HasValue)
                {
                    queryable = queryable.Where(m => m.CanonicalProductId == query.CanonicalProductId.Value);
                }

                if (!string.IsNullOrEmpty(query.SellerName))
                {
                    queryable = queryable.Where(m => m.SellerName == query.SellerName);
                }

                if (query.IsActiveForScraping.HasValue)
                {
                    queryable = queryable.Where(m => m.IsActiveForScraping == query.IsActiveForScraping.Value);
                }

                if (!string.IsNullOrEmpty(query.Search))
                {
                    var searchTerm = query.Search.ToLower();
                    queryable = queryable.Where(m => 
                        m.SellerName.ToLower().Contains(searchTerm) ||
                        m.ExactProductUrl.ToLower().Contains(searchTerm));
                }

                // Get total count
                var totalCount = await queryable.CountAsync();

                // Apply pagination and ordering
                var mappings = await queryable
                    .OrderByDescending(m => m.CreatedAt)
                    .Skip((query.PageNumber - 1) * query.PageSize)
                    .Take(query.PageSize)
                    .ToListAsync();

                var responses = mappings.Select(MapToResponse).ToList();

                var pagedResponse = PagedResponse<ProductSellerMappingResponse>.SuccessResult(
                    responses,
                    query.PageNumber,
                    query.PageSize,
                    totalCount
                );

                return Result<PagedResponse<ProductSellerMappingResponse>>.Success(pagedResponse);
            }
            catch (Exception ex)
            {
                return Result<PagedResponse<ProductSellerMappingResponse>>.Failure(ex);
            }
        }

        public async Task<Result<PagedResponse<ProductSellerMappingResponse>>> GetMappingsByProductIdAsync(Guid canonicalProductId, int pageNumber = 1, int pageSize = 50)
        {
            var query = new ProductSellerMappingQueryRequest
            {
                CanonicalProductId = canonicalProductId,
                PageNumber = pageNumber,
                PageSize = pageSize
            };

            return await GetMappingsAsync(query);
        }

        public async Task<Result<PagedResponse<ProductSellerMappingResponse>>> GetActiveMappingsAsync(int pageNumber = 1, int pageSize = 100)
        {
            var query = new ProductSellerMappingQueryRequest
            {
                IsActiveForScraping = true,
                PageNumber = pageNumber,
                PageSize = pageSize
            };

            return await GetMappingsAsync(query);
        }

        public async Task<Result<ProductSellerMappingResponse>> GetMappingByIdAsync(Guid mappingId)
        {
            try
            {
                var mapping = await _context.ProductSellerMappings
                    .FirstOrDefaultAsync(m => m.MappingId == mappingId);

                if (mapping == null)
                {
                    return Result<ProductSellerMappingResponse>.Failure(
                        $"Mapping with ID {mappingId} not found.",
                        "RESOURCE_NOT_FOUND"
                    );
                }

                var response = MapToResponse(mapping);
                return Result<ProductSellerMappingResponse>.Success(response);
            }
            catch (Exception ex)
            {
                return Result<ProductSellerMappingResponse>.Failure(ex);
            }
        }

        public async Task<Result<ProductSellerMappingResponse>> UpdateMappingAsync(Guid mappingId, UpdateProductSellerMappingRequest request)
        {
            try
            {
                var mapping = await _context.ProductSellerMappings
                    .FirstOrDefaultAsync(m => m.MappingId == mappingId);

                if (mapping == null)
                {
                    return Result<ProductSellerMappingResponse>.Failure(
                        $"Mapping with ID {mappingId} not found.",
                        "RESOURCE_NOT_FOUND"
                    );
                }

                // Check for duplicate if seller name changed
                if (mapping.SellerName != request.SellerName)
                {
                    if (await IsDuplicateMappingAsync(mapping.CanonicalProductId, request.SellerName, mappingId))
                    {
                        return Result<ProductSellerMappingResponse>.Failure(
                            "A mapping for this product and seller already exists.",
                            "CONFLICT"
                        );
                    }
                }

                // Update properties                mapping.SellerName = request.SellerName;
                mapping.ExactProductUrl = request.ExactProductUrl;
                mapping.IsActiveForScraping = request.IsActiveForScraping;
                mapping.ScrapingFrequencyOverride = request.ScrapingFrequencyOverride;
                mapping.SiteConfigId = request.SiteConfigId;
                mapping.UpdatedAt = DateTimeOffset.UtcNow;

                await _context.SaveChangesAsync();

                var response = MapToResponse(mapping);
                return Result<ProductSellerMappingResponse>.Success(response);
            }
            catch (Exception ex)
            {
                return Result<ProductSellerMappingResponse>.Failure(ex);
            }
        }

        public async Task<Result> DeleteMappingAsync(Guid mappingId)
        {
            try
            {
                var mapping = await _context.ProductSellerMappings
                    .FirstOrDefaultAsync(m => m.MappingId == mappingId);

                if (mapping == null)
                {
                    return Result.Failure(
                        $"Mapping with ID {mappingId} not found.",
                        "RESOURCE_NOT_FOUND"
                    );
                }

                _context.ProductSellerMappings.Remove(mapping);
                await _context.SaveChangesAsync();

                return Result.Success();
            }
            catch (Exception ex)
            {
                return Result.Failure(ex);
            }
        }

        public async Task<bool> MappingExistsAsync(Guid mappingId)
        {
            return await _context.ProductSellerMappings
                .AnyAsync(m => m.MappingId == mappingId);
        }

        public async Task<bool> IsDuplicateMappingAsync(Guid canonicalProductId, string sellerName, Guid? excludeMappingId = null)
        {
            var query = _context.ProductSellerMappings
                .Where(m => m.CanonicalProductId == canonicalProductId && 
                           m.SellerName == sellerName);

            if (excludeMappingId.HasValue)
            {
                query = query.Where(m => m.MappingId != excludeMappingId.Value);
            }

            return await query.AnyAsync();
        }

        private static ProductSellerMappingResponse MapToResponse(ProductSellerMapping mapping)
        {
            return new ProductSellerMappingResponse
            {
                MappingId = mapping.MappingId,
                CanonicalProductId = mapping.CanonicalProductId,
                SellerName = mapping.SellerName,
                ExactProductUrl = mapping.ExactProductUrl,                IsActiveForScraping = mapping.IsActiveForScraping,
                ScrapingFrequencyOverride = mapping.ScrapingFrequencyOverride,
                SiteConfigId = mapping.SiteConfigId,
                LastScrapedAt = mapping.LastScrapedAt,
                NextScrapeAt = mapping.NextScrapeAt,
                CreatedAt = mapping.CreatedAt,
                UpdatedAt = mapping.UpdatedAt
            };
        }
    }
}
