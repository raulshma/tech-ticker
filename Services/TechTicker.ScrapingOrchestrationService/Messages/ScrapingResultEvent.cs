namespace TechTicker.ScrapingOrchestrationService.Messages
{
    /// <summary>
    /// Event message received from the Scraper Service after attempting to scrape a product page
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
}
