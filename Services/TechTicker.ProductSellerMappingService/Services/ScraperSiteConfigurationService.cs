using Microsoft.EntityFrameworkCore;
using TechTicker.ProductSellerMappingService.Data;
using TechTicker.ProductSellerMappingService.DTOs;
using TechTicker.Shared.Common;
using TechTicker.Shared.Models;
using TechTicker.Shared.Utilities;

namespace TechTicker.ProductSellerMappingService.Services
{
    /// <summary>
    /// Service for managing scraper site configurations
    /// </summary>
    public class ScraperSiteConfigurationService : IScraperSiteConfigurationService
    {
        private readonly ProductSellerMappingDbContext _context;

        public ScraperSiteConfigurationService(ProductSellerMappingDbContext context)
        {
            _context = context;
        }

        public async Task<Result<ScraperSiteConfigurationResponse>> CreateSiteConfigurationAsync(CreateScraperSiteConfigurationRequest request)
        {
            try
            {
                // Check if a configuration for this domain already exists
                var existingConfig = await _context.ScraperSiteConfigurations
                    .FirstOrDefaultAsync(sc => sc.SiteDomain == request.SiteDomain);

                if (existingConfig != null)
                {
                    return Result<ScraperSiteConfigurationResponse>.Failure(
                        $"A site configuration for domain '{request.SiteDomain}' already exists",
                        "CONFLICT");
                }

                var siteConfig = new ScraperSiteConfiguration
                {
                    SiteConfigId = Guid.NewGuid(),
                    SiteDomain = request.SiteDomain,
                    ProductNameSelector = request.ProductNameSelector,
                    PriceSelector = request.PriceSelector,
                    StockSelector = request.StockSelector,
                    SellerNameOnPageSelector = request.SellerNameOnPageSelector,
                    CreatedAt = DateTimeOffset.UtcNow,
                    UpdatedAt = DateTimeOffset.UtcNow
                };

                _context.ScraperSiteConfigurations.Add(siteConfig);
                await _context.SaveChangesAsync();

                var response = MapToResponse(siteConfig);
                return Result<ScraperSiteConfigurationResponse>.Success(response);
            }
            catch (Exception ex)
            {
                return Result<ScraperSiteConfigurationResponse>.Failure(ex);
            }
        }

        public async Task<Result<ScraperSiteConfigurationResponse>> GetSiteConfigurationAsync(Guid siteConfigId)
        {
            try
            {
                var siteConfig = await _context.ScraperSiteConfigurations
                    .FirstOrDefaultAsync(sc => sc.SiteConfigId == siteConfigId);

                if (siteConfig == null)
                {
                    return Result<ScraperSiteConfigurationResponse>.Failure(
                        $"Site configuration with ID '{siteConfigId}' not found",
                        "NOT_FOUND");
                }

                var response = MapToResponse(siteConfig);
                return Result<ScraperSiteConfigurationResponse>.Success(response);
            }
            catch (Exception ex)
            {
                return Result<ScraperSiteConfigurationResponse>.Failure(ex);
            }
        }

        public async Task<Result<ScraperSiteConfigurationResponse>> GetSiteConfigurationByDomainAsync(string domain)
        {
            try
            {
                var siteConfig = await _context.ScraperSiteConfigurations
                    .FirstOrDefaultAsync(sc => sc.SiteDomain == domain);

                if (siteConfig == null)
                {
                    return Result<ScraperSiteConfigurationResponse>.Failure(
                        $"Site configuration for domain '{domain}' not found",
                        "NOT_FOUND");
                }

                var response = MapToResponse(siteConfig);
                return Result<ScraperSiteConfigurationResponse>.Success(response);
            }
            catch (Exception ex)
            {
                return Result<ScraperSiteConfigurationResponse>.Failure(ex);
            }
        }

        public async Task<Result<PagedResponse<ScraperSiteConfigurationResponse>>> GetSiteConfigurationsAsync(ScraperSiteConfigurationQueryRequest request)
        {
            try
            {
                var query = _context.ScraperSiteConfigurations.AsQueryable();

                // Apply filters
                if (!string.IsNullOrWhiteSpace(request.SiteDomain))
                {
                    query = query.Where(sc => sc.SiteDomain == request.SiteDomain);
                }

                if (!string.IsNullOrWhiteSpace(request.Search))
                {
                    query = query.Where(sc => sc.SiteDomain.Contains(request.Search));
                }

                // Get total count
                var totalCount = await query.CountAsync();

                // Apply pagination
                var siteConfigs = await query
                    .OrderBy(sc => sc.SiteDomain)
                    .Skip((request.PageNumber - 1) * request.PageSize)
                    .Take(request.PageSize)
                    .ToListAsync();                var responses = siteConfigs.Select(MapToResponse).ToList();
                
                var pagedResponse = PagedResponse<ScraperSiteConfigurationResponse>.SuccessResult(
                    responses,
                    request.PageNumber,
                    request.PageSize,
                    totalCount
                );

                return Result<PagedResponse<ScraperSiteConfigurationResponse>>.Success(pagedResponse);
            }
            catch (Exception ex)
            {
                return Result<PagedResponse<ScraperSiteConfigurationResponse>>.Failure(ex);
            }
        }

        public async Task<Result<ScraperSiteConfigurationResponse>> UpdateSiteConfigurationAsync(Guid siteConfigId, UpdateScraperSiteConfigurationRequest request)
        {
            try
            {
                var siteConfig = await _context.ScraperSiteConfigurations
                    .FirstOrDefaultAsync(sc => sc.SiteConfigId == siteConfigId);

                if (siteConfig == null)
                {
                    return Result<ScraperSiteConfigurationResponse>.Failure(
                        $"Site configuration with ID '{siteConfigId}' not found",
                        "NOT_FOUND");
                }

                // Check if domain change would conflict with existing configuration
                if (siteConfig.SiteDomain != request.SiteDomain)
                {
                    var existingConfig = await _context.ScraperSiteConfigurations
                        .FirstOrDefaultAsync(sc => sc.SiteDomain == request.SiteDomain && sc.SiteConfigId != siteConfigId);

                    if (existingConfig != null)
                    {
                        return Result<ScraperSiteConfigurationResponse>.Failure(
                            $"A site configuration for domain '{request.SiteDomain}' already exists",
                            "CONFLICT");
                    }
                }

                // Update properties
                siteConfig.SiteDomain = request.SiteDomain;
                siteConfig.ProductNameSelector = request.ProductNameSelector;
                siteConfig.PriceSelector = request.PriceSelector;
                siteConfig.StockSelector = request.StockSelector;
                siteConfig.SellerNameOnPageSelector = request.SellerNameOnPageSelector;
                siteConfig.UpdatedAt = DateTimeOffset.UtcNow;

                await _context.SaveChangesAsync();

                var response = MapToResponse(siteConfig);
                return Result<ScraperSiteConfigurationResponse>.Success(response);
            }
            catch (Exception ex)
            {
                return Result<ScraperSiteConfigurationResponse>.Failure(ex);
            }
        }

        public async Task<Result<bool>> DeleteSiteConfigurationAsync(Guid siteConfigId)
        {
            try
            {
                var siteConfig = await _context.ScraperSiteConfigurations
                    .Include(sc => sc.ProductSellerMappings)
                    .FirstOrDefaultAsync(sc => sc.SiteConfigId == siteConfigId);

                if (siteConfig == null)
                {
                    return Result<bool>.Failure(
                        $"Site configuration with ID '{siteConfigId}' not found",
                        "NOT_FOUND");
                }

                // Check if any mappings are still using this configuration
                if (siteConfig.ProductSellerMappings.Any())
                {
                    return Result<bool>.Failure(
                        "Cannot delete site configuration that is still referenced by product seller mappings",
                        "CONFLICT");
                }

                _context.ScraperSiteConfigurations.Remove(siteConfig);
                await _context.SaveChangesAsync();

                return Result<bool>.Success(true);
            }
            catch (Exception ex)
            {
                return Result<bool>.Failure(ex);
            }
        }

        private static ScraperSiteConfigurationResponse MapToResponse(ScraperSiteConfiguration siteConfig)
        {
            return new ScraperSiteConfigurationResponse
            {
                SiteConfigId = siteConfig.SiteConfigId,
                SiteDomain = siteConfig.SiteDomain,
                ProductNameSelector = siteConfig.ProductNameSelector,
                PriceSelector = siteConfig.PriceSelector,
                StockSelector = siteConfig.StockSelector,
                SellerNameOnPageSelector = siteConfig.SellerNameOnPageSelector,
                CreatedAt = siteConfig.CreatedAt,
                UpdatedAt = siteConfig.UpdatedAt
            };
        }
    }
}
