using System.Collections.Generic;
using TechTicker.Shared.Utilities.Html;

namespace TechTicker.Domain.Entities.Canonical;

public class CanonicalProperty
{
    public string CanonicalName { get; set; } = null!; // e.g., "memory_size_gb"
    public string DisplayName { get; set; } = null!;  // e.g., "Memory Size"
    public List<string> Aliases { get; set; } = new(); // e.g., ["memory size", "mem size", "ram"]
    public SpecificationType DataType { get; set; } = SpecificationType.Text;
    public string? Unit { get; set; }              // Standard unit, e.g., "GB"
    public bool IsPrimaryIdentifier { get; set; }  // Critical property flag
} 