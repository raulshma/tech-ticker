using System.Text.Json;
using TechTicker.Application.DTOs;
using TechTicker.Application.Services.Interfaces;
using TechTicker.Domain.Entities;
using TechTicker.Shared.Utilities;

namespace TechTicker.Application.Services;

/// <summary>
/// Service implementation for mapping between domain entities and DTOs
/// </summary>
public class MappingService : IMappingService
{
    public CategoryDto MapToDto(Category category)
    {
        return new CategoryDto
        {
            CategoryId = category.CategoryId,
            Name = category.Name,
            Slug = category.Slug,
            Description = category.Description,
            CreatedAt = category.CreatedAt,
            UpdatedAt = category.UpdatedAt
        };
    }

    public Category MapToEntity(CreateCategoryDto createDto)
    {
        return new Category
        {
            CategoryId = Guid.NewGuid(),
            Name = createDto.Name,
            Slug = string.IsNullOrWhiteSpace(createDto.Slug) 
                ? StringUtilities.ToSlug(createDto.Name) 
                : createDto.Slug,
            Description = createDto.Description
        };
    }

    public void MapToEntity(UpdateCategoryDto updateDto, Category category)
    {
        if (!string.IsNullOrWhiteSpace(updateDto.Name))
            category.Name = updateDto.Name;

        if (!string.IsNullOrWhiteSpace(updateDto.Slug))
            category.Slug = updateDto.Slug;

        if (updateDto.Description != null)
            category.Description = updateDto.Description;
    }

    public ProductDto MapToDto(Product product)
    {
        return new ProductDto
        {
            ProductId = product.ProductId,
            Name = product.Name,
            Manufacturer = product.Manufacturer,
            ModelNumber = product.ModelNumber,
            SKU = product.SKU,
            CategoryId = product.CategoryId,
            Description = product.Description,
            Specifications = product.SpecificationsDict,
            IsActive = product.IsActive,
            CreatedAt = product.CreatedAt,
            UpdatedAt = product.UpdatedAt,
            Category = product.Category != null ? MapToDto(product.Category) : null
        };
    }

    public Product MapToEntity(CreateProductDto createDto)
    {
        return new Product
        {
            ProductId = Guid.NewGuid(),
            Name = createDto.Name,
            Manufacturer = createDto.Manufacturer,
            ModelNumber = createDto.ModelNumber,
            SKU = createDto.SKU,
            CategoryId = createDto.CategoryId,
            Description = createDto.Description,
            SpecificationsDict = createDto.Specifications,
            IsActive = true
        };
    }

    public void MapToEntity(UpdateProductDto updateDto, Product product)
    {
        if (!string.IsNullOrWhiteSpace(updateDto.Name))
            product.Name = updateDto.Name;

        if (updateDto.Manufacturer != null)
            product.Manufacturer = updateDto.Manufacturer;

        if (updateDto.ModelNumber != null)
            product.ModelNumber = updateDto.ModelNumber;

        if (updateDto.SKU != null)
            product.SKU = updateDto.SKU;

        if (updateDto.CategoryId.HasValue)
            product.CategoryId = updateDto.CategoryId.Value;

        if (updateDto.Description != null)
            product.Description = updateDto.Description;

        if (updateDto.Specifications != null)
            product.SpecificationsDict = updateDto.Specifications;

        if (updateDto.IsActive.HasValue)
            product.IsActive = updateDto.IsActive.Value;
    }

    public ProductSellerMappingDto MapToDto(ProductSellerMapping mapping)
    {
        return new ProductSellerMappingDto
        {
            MappingId = mapping.MappingId,
            CanonicalProductId = mapping.CanonicalProductId,
            SellerName = mapping.SellerName,
            ExactProductUrl = mapping.ExactProductUrl,
            IsActiveForScraping = mapping.IsActiveForScraping,
            ScrapingFrequencyOverride = mapping.ScrapingFrequencyOverride,
            SiteConfigId = mapping.SiteConfigId,
            LastScrapedAt = mapping.LastScrapedAt,
            NextScrapeAt = mapping.NextScrapeAt,
            LastScrapeStatus = mapping.LastScrapeStatus,
            LastScrapeErrorCode = mapping.LastScrapeErrorCode,
            ConsecutiveFailureCount = mapping.ConsecutiveFailureCount,
            CreatedAt = mapping.CreatedAt,
            UpdatedAt = mapping.UpdatedAt,
            Product = mapping.Product != null ? MapToDto(mapping.Product) : null,
            SiteConfiguration = mapping.SiteConfiguration != null ? MapToDto(mapping.SiteConfiguration) : null
        };
    }

    public ProductSellerMapping MapToEntity(CreateProductSellerMappingDto createDto)
    {
        return new ProductSellerMapping
        {
            MappingId = Guid.NewGuid(),
            CanonicalProductId = createDto.CanonicalProductId,
            SellerName = createDto.SellerName,
            ExactProductUrl = createDto.ExactProductUrl,
            IsActiveForScraping = createDto.IsActiveForScraping,
            ScrapingFrequencyOverride = createDto.ScrapingFrequencyOverride,
            SiteConfigId = createDto.SiteConfigId,
            NextScrapeAt = DateTimeOffset.UtcNow // Initialize for immediate scraping
        };
    }

    public void MapToEntity(UpdateProductSellerMappingDto updateDto, ProductSellerMapping mapping)
    {
        if (!string.IsNullOrWhiteSpace(updateDto.SellerName))
            mapping.SellerName = updateDto.SellerName;

        if (!string.IsNullOrWhiteSpace(updateDto.ExactProductUrl))
            mapping.ExactProductUrl = updateDto.ExactProductUrl;

        if (updateDto.IsActiveForScraping.HasValue)
            mapping.IsActiveForScraping = updateDto.IsActiveForScraping.Value;

        if (updateDto.ScrapingFrequencyOverride != null)
            mapping.ScrapingFrequencyOverride = updateDto.ScrapingFrequencyOverride;

        if (updateDto.SiteConfigId != null)
            mapping.SiteConfigId = updateDto.SiteConfigId;
    }

    public ScraperSiteConfigurationDto MapToDto(ScraperSiteConfiguration config)
    {
        return new ScraperSiteConfigurationDto
        {
            SiteConfigId = config.SiteConfigId,
            SiteDomain = config.SiteDomain,
            ProductNameSelector = config.ProductNameSelector,
            PriceSelector = config.PriceSelector,
            StockSelector = config.StockSelector,
            SellerNameOnPageSelector = config.SellerNameOnPageSelector,
            DefaultUserAgent = config.DefaultUserAgent,
            AdditionalHeaders = config.AdditionalHeadersDict,
            IsEnabled = config.IsEnabled,
            CreatedAt = config.CreatedAt,
            UpdatedAt = config.UpdatedAt
        };
    }

    public ScraperSiteConfiguration MapToEntity(CreateScraperSiteConfigurationDto createDto)
    {
        return new ScraperSiteConfiguration
        {
            SiteConfigId = Guid.NewGuid(),
            SiteDomain = createDto.SiteDomain,
            ProductNameSelector = createDto.ProductNameSelector,
            PriceSelector = createDto.PriceSelector,
            StockSelector = createDto.StockSelector,
            SellerNameOnPageSelector = createDto.SellerNameOnPageSelector,
            DefaultUserAgent = createDto.DefaultUserAgent,
            AdditionalHeadersDict = createDto.AdditionalHeaders,
            IsEnabled = createDto.IsEnabled
        };
    }

    public void MapToEntity(UpdateScraperSiteConfigurationDto updateDto, ScraperSiteConfiguration config)
    {
        if (!string.IsNullOrWhiteSpace(updateDto.SiteDomain))
            config.SiteDomain = updateDto.SiteDomain;

        if (!string.IsNullOrWhiteSpace(updateDto.ProductNameSelector))
            config.ProductNameSelector = updateDto.ProductNameSelector;

        if (!string.IsNullOrWhiteSpace(updateDto.PriceSelector))
            config.PriceSelector = updateDto.PriceSelector;

        if (!string.IsNullOrWhiteSpace(updateDto.StockSelector))
            config.StockSelector = updateDto.StockSelector;

        if (updateDto.SellerNameOnPageSelector != null)
            config.SellerNameOnPageSelector = updateDto.SellerNameOnPageSelector;

        if (updateDto.DefaultUserAgent != null)
            config.DefaultUserAgent = updateDto.DefaultUserAgent;

        if (updateDto.AdditionalHeaders != null)
            config.AdditionalHeadersDict = updateDto.AdditionalHeaders;

        if (updateDto.IsEnabled.HasValue)
            config.IsEnabled = updateDto.IsEnabled.Value;
    }

    public PriceHistoryDto MapToDto(PriceHistory priceHistory)
    {
        return new PriceHistoryDto
        {
            Timestamp = priceHistory.Timestamp,
            Price = priceHistory.Price,
            StockStatus = priceHistory.StockStatus,
            SourceUrl = priceHistory.SourceUrl,
            ScrapedProductNameOnPage = priceHistory.ScrapedProductNameOnPage
        };
    }

    public AlertRuleDto MapToDto(AlertRule alertRule)
    {
        return new AlertRuleDto
        {
            AlertRuleId = alertRule.AlertRuleId,
            UserId = alertRule.UserId,
            CanonicalProductId = alertRule.CanonicalProductId,
            ConditionType = alertRule.ConditionType,
            ThresholdValue = alertRule.ThresholdValue,
            PercentageValue = alertRule.PercentageValue,
            SpecificSellerName = alertRule.SpecificSellerName,
            NotificationFrequencyMinutes = alertRule.NotificationFrequencyMinutes,
            IsActive = alertRule.IsActive,
            LastNotifiedAt = alertRule.LastNotifiedAt,
            CreatedAt = alertRule.CreatedAt,
            UpdatedAt = alertRule.UpdatedAt,
            RuleDescription = alertRule.RuleDescription,
            User = alertRule.User != null ? MapToDto(alertRule.User, new List<string>()) : null,
            Product = alertRule.Product != null ? MapToDto(alertRule.Product) : null
        };
    }

    public AlertRule MapToEntity(CreateAlertRuleDto createDto, Guid userId)
    {
        return new AlertRule
        {
            AlertRuleId = Guid.NewGuid(),
            UserId = userId,
            CanonicalProductId = createDto.CanonicalProductId,
            ConditionType = createDto.ConditionType,
            ThresholdValue = createDto.ThresholdValue,
            PercentageValue = createDto.PercentageValue,
            SpecificSellerName = createDto.SpecificSellerName,
            NotificationFrequencyMinutes = createDto.NotificationFrequencyMinutes,
            IsActive = true
        };
    }

    public void MapToEntity(UpdateAlertRuleDto updateDto, AlertRule alertRule)
    {
        if (!string.IsNullOrWhiteSpace(updateDto.ConditionType))
            alertRule.ConditionType = updateDto.ConditionType;

        if (updateDto.ThresholdValue.HasValue)
            alertRule.ThresholdValue = updateDto.ThresholdValue;

        if (updateDto.PercentageValue.HasValue)
            alertRule.PercentageValue = updateDto.PercentageValue;

        if (updateDto.SpecificSellerName != null)
            alertRule.SpecificSellerName = updateDto.SpecificSellerName;

        if (updateDto.NotificationFrequencyMinutes.HasValue)
            alertRule.NotificationFrequencyMinutes = updateDto.NotificationFrequencyMinutes.Value;

        if (updateDto.IsActive.HasValue)
            alertRule.IsActive = updateDto.IsActive.Value;
    }

    public UserDto MapToDto(ApplicationUser user, IEnumerable<string> roles)
    {
        return new UserDto
        {
            UserId = user.Id,
            Email = user.Email!,
            FirstName = user.FirstName,
            LastName = user.LastName,
            FullName = user.FullName,
            IsActive = user.IsActive,
            CreatedAt = user.CreatedAt,
            UpdatedAt = user.UpdatedAt,
            Roles = roles
        };
    }

    public ApplicationUser MapToEntity(CreateUserDto createDto)
    {
        return new ApplicationUser
        {
            Id = Guid.NewGuid(),
            Email = createDto.Email,
            UserName = createDto.Email,
            FirstName = createDto.FirstName,
            LastName = createDto.LastName,
            IsActive = true
        };
    }

    public ApplicationUser MapToEntity(RegisterUserDto registerDto)
    {
        return new ApplicationUser
        {
            Id = Guid.NewGuid(),
            Email = registerDto.Email,
            UserName = registerDto.Email,
            FirstName = registerDto.FirstName,
            LastName = registerDto.LastName,
            IsActive = true
        };
    }

    public void MapToEntity(UpdateUserDto updateDto, ApplicationUser user)
    {
        if (!string.IsNullOrWhiteSpace(updateDto.Email))
        {
            user.Email = updateDto.Email;
            user.UserName = updateDto.Email;
        }

        if (updateDto.FirstName != null)
            user.FirstName = updateDto.FirstName;

        if (updateDto.LastName != null)
            user.LastName = updateDto.LastName;

        if (updateDto.IsActive.HasValue)
            user.IsActive = updateDto.IsActive.Value;
    }
}
