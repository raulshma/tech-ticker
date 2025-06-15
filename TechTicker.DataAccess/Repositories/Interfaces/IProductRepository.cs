using TechTicker.Domain.Entities;

namespace TechTicker.DataAccess.Repositories.Interfaces;

/// <summary>
/// Repository interface for Product entities
/// </summary>
public interface IProductRepository : IRepository<Product>
{
    Task<(IEnumerable<Product> Items, int TotalCount)> GetProductsAsync(
        Guid? categoryId = null,
        string? search = null,
        int pageNumber = 1,
        int pageSize = 10,
        bool includeInactive = false);

    Task<Product?> GetByIdWithCategoryAsync(Guid id);
    Task<IEnumerable<Product>> GetByCategoryIdAsync(Guid categoryId);
}
