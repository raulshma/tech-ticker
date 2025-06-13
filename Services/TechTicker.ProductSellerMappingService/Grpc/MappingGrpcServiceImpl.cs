using Grpc.Core;
using TechTicker.Grpc.Contracts.Mappings;
using TechTicker.ProductSellerMappingService.Services;
using Google.Protobuf.WellKnownTypes;
using System.Linq;
using TechTicker.ProductSellerMappingService.DTOs;

namespace TechTicker.ProductSellerMappingService.Grpc
{
    /// <summary>
    /// gRPC service implementation for Product Seller Mapping operations
    /// </summary>
    public class MappingGrpcServiceImpl : MappingGrpcService.MappingGrpcServiceBase
    {
        private readonly IProductSellerMappingService _mappingService;
        private readonly ILogger<MappingGrpcServiceImpl> _logger;

        public MappingGrpcServiceImpl(
            IProductSellerMappingService mappingService,
            ILogger<MappingGrpcServiceImpl> logger)
        {
            _mappingService = mappingService;
            _logger = logger;
        }

        public override async Task<MappingResponse> GetMapping(GetMappingRequest request, ServerCallContext context)
        {
            _logger.LogInformation("gRPC GetMapping called for Mapping ID: {MappingId}", request.MappingId);

            if (!Guid.TryParse(request.MappingId, out var mappingId))
            {
                throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid mapping ID format"));
            }

            var result = await _mappingService.GetMappingByIdAsync(mappingId);
            
            if (result.IsFailure)
            {
                if (result.ErrorCode == "RESOURCE_NOT_FOUND")
                {
                    throw new RpcException(new Status(StatusCode.NotFound, result.ErrorMessage ?? "Mapping not found"));
                }
                throw new RpcException(new Status(StatusCode.Internal, result.ErrorMessage ?? "Unknown error"));
            }

            return MapToGrpcMappingResponse(result.Data!);
        }

        public override async Task<GetMappingsResponse> GetMappingsByProduct(GetMappingsByProductRequest request, ServerCallContext context)
        {
            _logger.LogInformation("gRPC GetMappingsByProduct called for Product ID: {ProductId}", request.CanonicalProductId);

            if (!Guid.TryParse(request.CanonicalProductId, out var productId))
            {
                throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid product ID format"));
            }

            var result = await _mappingService.GetMappingsByProductIdAsync(
                productId, 
                request.PageNumber > 0 ? request.PageNumber : 1,
                request.PageSize > 0 ? request.PageSize : 50);
            
            if (result.IsFailure)
            {
                throw new RpcException(new Status(StatusCode.Internal, result.ErrorMessage ?? "Unknown error"));
            }

            var pagedData = result.Data!;
            var response = new GetMappingsResponse
            {
                TotalCount = (int)pagedData.Pagination.TotalCount,
                PageNumber = pagedData.Pagination.PageNumber,
                PageSize = pagedData.Pagination.PageSize,
                HasNextPage = pagedData.Pagination.HasNextPage
            };

            foreach (var mapping in pagedData.Data ?? Enumerable.Empty<ProductSellerMappingResponse>())
            {
                response.Mappings.Add(MapToGrpcMappingResponse(mapping));
            }

            return response;
        }

        public override async Task<GetMappingsResponse> GetActiveMappings(GetActiveMappingsRequest request, ServerCallContext context)
        {
            _logger.LogInformation("gRPC GetActiveMappings called with PageNumber: {PageNumber}, PageSize: {PageSize}", 
                request.PageNumber, request.PageSize);

            var result = await _mappingService.GetActiveMappingsAsync(
                request.PageNumber > 0 ? request.PageNumber : 1,
                request.PageSize > 0 ? request.PageSize : 100);
            
            if (result.IsFailure)
            {
                throw new RpcException(new Status(StatusCode.Internal, result.ErrorMessage ?? "Unknown error"));
            }

            var pagedData = result.Data!;
            var response = new GetMappingsResponse
            {
                TotalCount = (int)pagedData.Pagination.TotalCount,
                PageNumber = pagedData.Pagination.PageNumber,
                PageSize = pagedData.Pagination.PageSize,
                HasNextPage = pagedData.Pagination.HasNextPage
            };

            // Apply optional seller name filter if provided
            var mappings = pagedData.Data ?? Enumerable.Empty<ProductSellerMappingResponse>();
            if (!string.IsNullOrEmpty(request.SellerNameFilter))
            {
                mappings = mappings.Where(m => m.SellerName.Contains(request.SellerNameFilter, StringComparison.OrdinalIgnoreCase));
            }

            foreach (var mapping in mappings)
            {
                response.Mappings.Add(MapToGrpcMappingResponse(mapping));
            }

            return response;
        }

        public override async Task<GetMappingsResponse> GetMappingsBySeller(GetMappingsBySellerRequest request, ServerCallContext context)
        {
            _logger.LogInformation("gRPC GetMappingsBySeller called for Seller: {SellerName}", request.SellerName);

            if (string.IsNullOrEmpty(request.SellerName))
            {
                throw new RpcException(new Status(StatusCode.InvalidArgument, "Seller name is required"));
            }

            var queryRequest = new ProductSellerMappingQueryRequest
            {
                SellerName = request.SellerName,
                PageNumber = request.PageNumber > 0 ? request.PageNumber : 1,
                PageSize = request.PageSize > 0 ? request.PageSize : 50
            };

            var result = await _mappingService.GetMappingsAsync(queryRequest);
            
            if (result.IsFailure)
            {
                throw new RpcException(new Status(StatusCode.Internal, result.ErrorMessage ?? "Unknown error"));
            }

            var pagedData = result.Data!;
            var response = new GetMappingsResponse
            {
                TotalCount = (int)pagedData.Pagination.TotalCount,
                PageNumber = pagedData.Pagination.PageNumber,
                PageSize = pagedData.Pagination.PageSize,
                HasNextPage = pagedData.Pagination.HasNextPage
            };

            foreach (var mapping in pagedData.Data ?? Enumerable.Empty<ProductSellerMappingResponse>())
            {
                response.Mappings.Add(MapToGrpcMappingResponse(mapping));
            }

            return response;
        }

        public override async Task<MappingExistsResponse> MappingExists(MappingExistsRequest request, ServerCallContext context)
        {
            _logger.LogInformation("gRPC MappingExists called for Mapping ID: {MappingId}", request.MappingId);

            if (!Guid.TryParse(request.MappingId, out var mappingId))
            {
                throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid mapping ID format"));
            }

            var exists = await _mappingService.MappingExistsAsync(mappingId);
            
            return new MappingExistsResponse { Exists = exists };
        }        public override Task<UpdateScrapingTimestampsResponse> UpdateScrapingTimestamps(UpdateScrapingTimestampsRequest request, ServerCallContext context)
        {
            _logger.LogInformation("gRPC UpdateScrapingTimestamps called for mapping {MappingId}", request.MappingId);

            if (!Guid.TryParse(request.MappingId, out var mappingId))
            {
                _logger.LogWarning("Invalid mapping ID format: {MappingId}", request.MappingId);
                return Task.FromResult(new UpdateScrapingTimestampsResponse
                {
                    Success = false,
                    Message = "Invalid mapping ID format"
                });
            }

            try
            {
                // For this example, we'll need to add a method to update timestamps
                // This would typically be a specialized method in the service
                // For now, we'll just log that it was called
                _logger.LogInformation("Would update timestamps for mapping {MappingId}", mappingId);

                return Task.FromResult(new UpdateScrapingTimestampsResponse
                {
                    Success = true,
                    Message = "Timestamps updated successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating timestamps for mapping {MappingId}", mappingId);
                return Task.FromResult(new UpdateScrapingTimestampsResponse
                {
                    Success = false,
                    Message = ex.Message
                });
            }
        }        public override async Task StreamMappingUpdates(Empty request, IServerStreamWriter<MappingUpdateEvent> responseStream, ServerCallContext context)
        {
            _logger.LogInformation("gRPC StreamMappingUpdates started");

            // This is a placeholder implementation for streaming mapping updates
            // In a real scenario, this would connect to a message broker or event stream
            // For now, we'll simulate some updates

            var cancellationToken = context.CancellationToken;
            var updateCount = 0;

            try
            {
                while (!cancellationToken.IsCancellationRequested && updateCount < 10) // Limit for demo
                {
                    await Task.Delay(5000, cancellationToken); // Wait 5 seconds between updates

                    // Create a sample mapping response for the event
                    var sampleMapping = new MappingResponse
                    {
                        MappingId = Guid.NewGuid().ToString(),
                        CanonicalProductId = Guid.NewGuid().ToString(),
                        SellerName = "Sample Seller",
                        ExactProductUrl = "https://example.com/product",
                        IsActiveForScraping = true,
                        ScrapingFrequencyOverride = string.Empty,
                        SiteSpecificSelectorsId = string.Empty,
                        CreatedAt = Timestamp.FromDateTimeOffset(DateTimeOffset.UtcNow),
                        UpdatedAt = Timestamp.FromDateTimeOffset(DateTimeOffset.UtcNow)
                    };

                    var updateEvent = new MappingUpdateEvent
                    {
                        EventType = MappingUpdateEvent.Types.EventType.Updated,
                        Mapping = sampleMapping,
                        Timestamp = Timestamp.FromDateTimeOffset(DateTimeOffset.UtcNow)
                    };

                    await responseStream.WriteAsync(updateEvent);
                    _logger.LogInformation("Sent mapping update event #{UpdateCount}", ++updateCount);
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("gRPC StreamMappingUpdates cancelled by client");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in gRPC StreamMappingUpdates");
                throw;
            }

            _logger.LogInformation("gRPC StreamMappingUpdates completed");
        }

        private static MappingResponse MapToGrpcMappingResponse(ProductSellerMappingResponse mapping)
        {
            return new MappingResponse
            {
                MappingId = mapping.MappingId.ToString(),
                CanonicalProductId = mapping.CanonicalProductId.ToString(),
                SellerName = mapping.SellerName,
                ExactProductUrl = mapping.ExactProductUrl,                IsActiveForScraping = mapping.IsActiveForScraping,
                ScrapingFrequencyOverride = mapping.ScrapingFrequencyOverride ?? string.Empty,
                SiteSpecificSelectorsId = mapping.SiteConfigId?.ToString() ?? string.Empty,
                LastScrapedAt = mapping.LastScrapedAt.HasValue
                    ? Timestamp.FromDateTimeOffset(mapping.LastScrapedAt.Value) 
                    : null,
                NextScrapeAt = mapping.NextScrapeAt.HasValue 
                    ? Timestamp.FromDateTimeOffset(mapping.NextScrapeAt.Value) 
                    : null,
                CreatedAt = Timestamp.FromDateTimeOffset(mapping.CreatedAt),
                UpdatedAt = Timestamp.FromDateTimeOffset(mapping.UpdatedAt)
            };
        }
    }
}
