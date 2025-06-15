namespace TechTicker.Application.Messages;

/// <summary>
/// Event published after a scraping attempt
/// </summary>
public class ScrapingResultEvent
{
    public Guid MappingId { get; set; }
    public bool WasSuccessful { get; set; }
    public DateTimeOffset Timestamp { get; set; }
    public string? ErrorMessage { get; set; }
    public string? ErrorCode { get; set; }
    public int? HttpStatusCode { get; set; }
}
