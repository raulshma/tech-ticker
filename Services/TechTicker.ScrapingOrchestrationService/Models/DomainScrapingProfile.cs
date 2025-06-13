using System.ComponentModel.DataAnnotations;

namespace TechTicker.ScrapingOrchestrationService.Models
{
    /// <summary>
    /// Represents scraping profile configuration for a specific domain
    /// Used to manage request patterns and reduce detectability
    /// </summary>
    public class DomainScrapingProfile
    {
        [Key]
        public string Domain { get; set; } = null!;

        /// <summary>
        /// List of User-Agent strings to rotate between
        /// </summary>
        public List<string> UserAgentList { get; set; } = new();

        /// <summary>
        /// Dictionary of HTTP header profiles to use
        /// </summary>
        public Dictionary<string, Dictionary<string, string>> HeaderProfiles { get; set; } = new();

        /// <summary>
        /// Minimum delay between requests to the same domain (in milliseconds)
        /// </summary>
        public int MinDelayMs { get; set; } = 1000;

        /// <summary>
        /// Maximum delay between requests to the same domain (in milliseconds)
        /// </summary>
        public int MaxDelayMs { get; set; } = 5000;

        /// <summary>
        /// Last time a request was made to this domain
        /// </summary>
        public DateTimeOffset? LastRequestAt { get; set; }

        /// <summary>
        /// Next allowed request time for this domain
        /// </summary>
        public DateTimeOffset? NextAllowedRequestAt { get; set; }

        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset UpdatedAt { get; set; }
    }
}
