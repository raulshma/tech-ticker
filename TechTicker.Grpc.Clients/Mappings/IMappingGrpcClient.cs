using TechTicker.Grpc.Contracts.Mappings;
using TechTicker.Shared.Utilities;

namespace TechTicker.Grpc.Clients.Mappings
{
    public interface IMappingGrpcClient
    {
        Task<Result<MappingResponse>> GetMappingAsync(string mappingId);
        Task<Result<GetMappingsResponse>> GetMappingsByProductAsync(string canonicalProductId, int pageNumber = 1, int pageSize = 50);
        Task<Result<GetMappingsResponse>> GetActiveMappingsAsync(int pageNumber = 1, int pageSize = 100, string? sellerNameFilter = null);
        Task<Result<GetMappingsResponse>> GetMappingsBySellerAsync(string sellerName, int pageNumber = 1, int pageSize = 50);
        Task<Result<bool>> MappingExistsAsync(string mappingId);
        Task<Result<bool>> UpdateScrapingTimestampsAsync(string mappingId, DateTimeOffset? lastScrapedAt, DateTimeOffset? nextScrapeAt);
    }
}
