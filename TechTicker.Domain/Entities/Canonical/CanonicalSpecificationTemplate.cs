using System.Collections.Generic;

namespace TechTicker.Domain.Entities.Canonical;

public class CanonicalSpecificationTemplate
{
    public string Category { get; set; } = null!; // Product category identifier, e.g., "Graphics Card"
    public List<CanonicalProperty> Properties { get; set; } = new();
} 