using Microsoft.EntityFrameworkCore;
using TechTicker.DataAccess.Repositories.Interfaces;
using TechTicker.Domain.Entities;

namespace TechTicker.DataAccess.Repositories;

/// <summary>
/// Repository implementation for Category entities
/// </summary>
public class CategoryRepository : Repository<Category>, ICategoryRepository
{
    public CategoryRepository(TechTickerDbContext context) : base(context)
    {
    }

    public async Task<Category?> GetBySlugAsync(string slug)
    {
        return await _dbSet.FirstOrDefaultAsync(c => c.Slug == slug);
    }

    public async Task<bool> SlugExistsAsync(string slug, Guid? excludeId = null)
    {
        var query = _dbSet.Where(c => c.Slug == slug);
        
        if (excludeId.HasValue)
        {
            query = query.Where(c => c.CategoryId != excludeId.Value);
        }

        return await query.AnyAsync();
    }

    public async Task<Category?> GetByIdOrSlugAsync(string idOrSlug)
    {
        // Try to parse as Guid first
        if (Guid.TryParse(idOrSlug, out var id))
        {
            return await GetByIdAsync(id);
        }

        // Otherwise, treat as slug
        return await GetBySlugAsync(idOrSlug);
    }
}
