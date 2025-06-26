using TechTicker.Application.DTOs;
using TechTicker.Domain.Entities;

namespace TechTicker.Application.Services.Interfaces;

/// <summary>
/// Service interface for mapping between domain entities and DTOs
/// </summary>
public interface IMappingService
{
    // Category mappings
    CategoryDto MapToDto(Category category);
    Category MapToEntity(CreateCategoryDto createDto);
    void MapToEntity(UpdateCategoryDto updateDto, Category category);

    // Product mappings
    ProductDto MapToDto(Product product);
    Product MapToEntity(CreateProductDto createDto);
    void MapToEntity(UpdateProductDto updateDto, Product product);

    // ProductSellerMapping mappings
    ProductSellerMappingDto MapToDto(ProductSellerMapping mapping);
    ProductSellerMapping MapToEntity(CreateProductSellerMappingDto createDto);
    void MapToEntity(UpdateProductSellerMappingDto updateDto, ProductSellerMapping mapping);

    // ScraperSiteConfiguration mappings
    ScraperSiteConfigurationDto MapToDto(ScraperSiteConfiguration config);
    ScraperSiteConfiguration MapToEntity(CreateScraperSiteConfigurationDto createDto);
    void MapToEntity(UpdateScraperSiteConfigurationDto updateDto, ScraperSiteConfiguration config);

    // PriceHistory mappings
    PriceHistoryDto MapToDto(PriceHistory priceHistory);
    CurrentPriceDto MapToCurrentPriceDto(PriceHistory priceHistory);

    // AlertRule mappings
    AlertRuleDto MapToDto(AlertRule alertRule);
    AlertRule MapToEntity(CreateAlertRuleDto createDto, Guid userId);
    void MapToEntity(UpdateAlertRuleDto updateDto, AlertRule alertRule);

    // User mappings
    UserDto MapToDto(ApplicationUser user, IEnumerable<string> roles);
    ApplicationUser MapToEntity(CreateUserDto createDto);
    ApplicationUser MapToEntity(RegisterUserDto registerDto);
    void MapToEntity(UpdateUserDto updateDto, ApplicationUser user);
}
