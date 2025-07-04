using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace TechTicker.Application.DTOs;

public class ProductSellerMappingBulkUpdateDto
{
    public List<BulkCreateProductSellerMappingDto> Create { get; set; } = new();
    public List<BulkUpdateProductSellerMappingDto> Update { get; set; } = new();
    public List<Guid> DeleteIds { get; set; } = new();
}

public class BulkCreateProductSellerMappingDto
{
    [Required]
    [MaxLength(100)]
    public string SellerName { get; set; } = null!;

    [Required]
    [MaxLength(2048)]
    [Url]
    public string ExactProductUrl { get; set; } = null!;

    public bool IsActiveForScraping { get; set; } = true;

    [MaxLength(50)]
    public string? ScrapingFrequencyOverride { get; set; }

    public Guid? SiteConfigId { get; set; }
}

public class BulkUpdateProductSellerMappingDto
{
    [Required]
    public Guid MappingId { get; set; }

    [MaxLength(100)]
    public string? SellerName { get; set; }

    [MaxLength(2048)]
    [Url]
    public string? ExactProductUrl { get; set; }

    public bool? IsActiveForScraping { get; set; }

    [MaxLength(50)]
    public string? ScrapingFrequencyOverride { get; set; }

    public Guid? SiteConfigId { get; set; }
} 