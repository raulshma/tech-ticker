using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TechTicker.Domain.Entities;

/// <summary>
/// Entity for saved browser automation test results
/// </summary>
[Table("SavedTestResults")]
public class SavedTestResult
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = null!;

    [MaxLength(1000)]
    public string? Description { get; set; }

    [Required]
    [MaxLength(500)]
    public string TestUrl { get; set; } = null!;

    public bool Success { get; set; }

    public DateTime SavedAt { get; set; } = DateTime.UtcNow;

    public DateTime ExecutedAt { get; set; }

    public int Duration { get; set; } // milliseconds

    public int ActionsExecuted { get; set; }

    public int ErrorCount { get; set; }

    [Required]
    [MaxLength(64)]
    public string ProfileHash { get; set; } = null!;

    [Required]
    public Guid CreatedBy { get; set; }

    // Navigation property to user
    [ForeignKey(nameof(CreatedBy))]
    public ApplicationUser CreatedByUser { get; set; } = null!;

    // JSON columns for complex data
    [Column(TypeName = "jsonb")]
    public string TestResultJson { get; set; } = "{}";

    [Column(TypeName = "jsonb")]
    public string ProfileJson { get; set; } = "{}";

    [Column(TypeName = "jsonb")]
    public string OptionsJson { get; set; } = "{}";

    [Column(TypeName = "jsonb")]
    public string? MetadataJson { get; set; }

    // Large data storage
    [Column(TypeName = "text")]
    public string? ScreenshotsData { get; set; } // Base64 concatenated screenshots

    [Column(TypeName = "text")]
    public string? VideoRecording { get; set; } // Base64 video data

    // Navigation to tags
    public ICollection<SavedTestResultTag> Tags { get; set; } = new List<SavedTestResultTag>();
}

/// <summary>
/// Entity for test result tags (many-to-many relationship)
/// </summary>
[Table("SavedTestResultTags")]
public class SavedTestResultTag
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid SavedTestResultId { get; set; }

    [Required]
    [MaxLength(50)]
    public string Tag { get; set; } = null!;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    [ForeignKey(nameof(SavedTestResultId))]
    public SavedTestResult SavedTestResult { get; set; } = null!;
}

/// <summary>
/// Entity for test execution history
/// </summary>
[Table("TestExecutionHistory")]
public class TestExecutionHistory
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [MaxLength(50)]
    public string SessionId { get; set; } = null!;

    public Guid? SavedTestResultId { get; set; }

    [Required]
    [MaxLength(500)]
    public string TestUrl { get; set; } = null!;

    [Required]
    [MaxLength(64)]
    public string ProfileHash { get; set; } = null!;

    public bool Success { get; set; }

    public DateTime ExecutedAt { get; set; }

    public int Duration { get; set; }

    public int ActionsExecuted { get; set; }

    public int ErrorCount { get; set; }

    [Required]
    public Guid ExecutedBy { get; set; }

    [MaxLength(200)]
    public string? SessionName { get; set; }

    [Required]
    [MaxLength(20)]
    public string BrowserEngine { get; set; } = "chromium";

    [Required]
    [MaxLength(20)]
    public string DeviceType { get; set; } = "desktop";

    // Navigation properties
    [ForeignKey(nameof(SavedTestResultId))]
    public SavedTestResult? SavedTestResult { get; set; }

    [ForeignKey(nameof(ExecutedBy))]
    public ApplicationUser ExecutedByUser { get; set; } = null!;
} 