using Grpc.Core;
using TechTicker.Grpc.Contracts.Mappings;
using TechTicker.Shared.Utilities;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.Logging;

namespace TechTicker.Grpc.Clients.Mappings
{
    public class MappingGrpcClient : IMappingGrpcClient
    {
        private readonly MappingGrpcService.MappingGrpcServiceClient _client;
        private readonly ILogger<MappingGrpcClient> _logger;

        public MappingGrpcClient(
            MappingGrpcService.MappingGrpcServiceClient client,
            ILogger<MappingGrpcClient> logger)
        {
            _client = client;
            _logger = logger;
        }

        public async Task<Result<MappingResponse>> GetMappingAsync(string mappingId)
        {
            try
            {
                var request = new GetMappingRequest { MappingId = mappingId };
                var response = await _client.GetMappingAsync(request);
                return Result<MappingResponse>.Success(response);
            }
            catch (RpcException ex) when (ex.StatusCode == StatusCode.NotFound)
            {
                return Result<MappingResponse>.Failure("RESOURCE_NOT_FOUND", $"Mapping with ID {mappingId} not found");
            }
            catch (RpcException ex)
            {
                _logger.LogError(ex, "gRPC error getting mapping {MappingId}", mappingId);
                return Result<MappingResponse>.Failure("GRPC_ERROR", ex.Status.Detail);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error getting mapping {MappingId}", mappingId);
                return Result<MappingResponse>.Failure("UNKNOWN_ERROR", "An unexpected error occurred");
            }
        }

        public async Task<Result<GetMappingsResponse>> GetMappingsByProductAsync(string canonicalProductId, int pageNumber = 1, int pageSize = 50)
        {
            try
            {
                var request = new GetMappingsByProductRequest 
                { 
                    CanonicalProductId = canonicalProductId,
                    PageNumber = pageNumber,
                    PageSize = pageSize
                };
                var response = await _client.GetMappingsByProductAsync(request);
                return Result<GetMappingsResponse>.Success(response);
            }
            catch (RpcException ex)
            {
                _logger.LogError(ex, "gRPC error getting mappings for product {ProductId}", canonicalProductId);
                return Result<GetMappingsResponse>.Failure("GRPC_ERROR", ex.Status.Detail);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error getting mappings for product {ProductId}", canonicalProductId);
                return Result<GetMappingsResponse>.Failure("UNKNOWN_ERROR", "An unexpected error occurred");
            }
        }

        public async Task<Result<GetMappingsResponse>> GetActiveMappingsAsync(int pageNumber = 1, int pageSize = 100, string? sellerNameFilter = null)
        {
            try
            {
                var request = new GetActiveMappingsRequest 
                { 
                    PageNumber = pageNumber,
                    PageSize = pageSize,
                    SellerNameFilter = sellerNameFilter ?? string.Empty
                };
                var response = await _client.GetActiveMappingsAsync(request);
                return Result<GetMappingsResponse>.Success(response);
            }
            catch (RpcException ex)
            {
                _logger.LogError(ex, "gRPC error getting active mappings");
                return Result<GetMappingsResponse>.Failure("GRPC_ERROR", ex.Status.Detail);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error getting active mappings");
                return Result<GetMappingsResponse>.Failure("UNKNOWN_ERROR", "An unexpected error occurred");
            }
        }

        public async Task<Result<GetMappingsResponse>> GetMappingsBySellerAsync(string sellerName, int pageNumber = 1, int pageSize = 50)
        {
            try
            {
                var request = new GetMappingsBySellerRequest 
                { 
                    SellerName = sellerName,
                    PageNumber = pageNumber,
                    PageSize = pageSize
                };
                var response = await _client.GetMappingsBySellerAsync(request);
                return Result<GetMappingsResponse>.Success(response);
            }
            catch (RpcException ex)
            {
                _logger.LogError(ex, "gRPC error getting mappings for seller {SellerName}", sellerName);
                return Result<GetMappingsResponse>.Failure("GRPC_ERROR", ex.Status.Detail);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error getting mappings for seller {SellerName}", sellerName);
                return Result<GetMappingsResponse>.Failure("UNKNOWN_ERROR", "An unexpected error occurred");
            }
        }

        public async Task<Result<bool>> MappingExistsAsync(string mappingId)
        {
            try
            {
                var request = new MappingExistsRequest { MappingId = mappingId };
                var response = await _client.MappingExistsAsync(request);
                return Result<bool>.Success(response.Exists);
            }
            catch (RpcException ex)
            {
                _logger.LogError(ex, "gRPC error checking if mapping {MappingId} exists", mappingId);
                return Result<bool>.Failure("GRPC_ERROR", ex.Status.Detail);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error checking if mapping {MappingId} exists", mappingId);
                return Result<bool>.Failure("UNKNOWN_ERROR", "An unexpected error occurred");
            }
        }

        public async Task<Result<bool>> UpdateScrapingTimestampsAsync(string mappingId, DateTimeOffset? lastScrapedAt, DateTimeOffset? nextScrapeAt)
        {
            try
            {
                var request = new UpdateScrapingTimestampsRequest 
                { 
                    MappingId = mappingId
                };

                if (lastScrapedAt.HasValue)
                {
                    request.LastScrapedAt = Timestamp.FromDateTimeOffset(lastScrapedAt.Value);
                }                if (nextScrapeAt.HasValue)
                {
                    request.NextScrapeAt = Timestamp.FromDateTimeOffset(nextScrapeAt.Value);
                }

                var response = await _client.UpdateScrapingTimestampsAsync(request);
                return Result<bool>.Success(response.Success);
            }
            catch (RpcException ex)
            {
                _logger.LogError(ex, "gRPC error updating scraping timestamps for mapping {MappingId}", mappingId);
                return Result<bool>.Failure("GRPC_ERROR", ex.Status.Detail);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error updating scraping timestamps for mapping {MappingId}", mappingId);
                return Result<bool>.Failure("UNKNOWN_ERROR", "An unexpected error occurred");
            }
        }
    }
}
