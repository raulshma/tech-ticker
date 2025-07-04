using TechTicker.Shared.Utilities.Html;

namespace TechTicker.Domain.Entities.Canonical;

public class NormalizedSpecificationValue
{
    public object? Value { get; set; }
    public string? Unit { get; set; }
    public SpecificationType DataType { get; set; } = SpecificationType.Text;
    public string RawValue { get; set; } = null!;  // Original scraped value
    public string CanonicalName { get; set; } = null!; // Link back to CanonicalProperty
    public double Confidence { get; set; }
}