using TechTicker.Domain.Entities;

namespace TechTicker.DataAccess.Repositories.Interfaces;

/// <summary>
/// Repository interface for ProductSellerMapping entities
/// </summary>
public interface IProductSellerMappingRepository : IRepository<ProductSellerMapping>
{
    Task<IEnumerable<ProductSellerMapping>> GetByProductIdAsync(Guid productId);
    Task<IEnumerable<ProductSellerMapping>> GetActiveMappingsAsync();
    Task<IEnumerable<ProductSellerMapping>> GetMappingsDueForScrapingAsync(int batchSize = 100);
    Task<ProductSellerMapping?> GetByIdWithConfigurationAsync(Guid mappingId);
}
