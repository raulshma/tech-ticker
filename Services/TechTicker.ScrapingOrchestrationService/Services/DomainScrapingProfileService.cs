using Microsoft.EntityFrameworkCore;
using TechTicker.ScrapingOrchestrationService.Data;
using TechTicker.ScrapingOrchestrationService.Models;

namespace TechTicker.ScrapingOrchestrationService.Services
{
    public interface IDomainScrapingProfileService
    {
        Task<DomainScrapingProfile> GetOrCreateProfileAsync(string domain);
        Task<bool> CanMakeRequestAsync(string domain);
        Task UpdateLastRequestTimeAsync(string domain);
        Task<(string UserAgent, Dictionary<string, string> Headers)> GetScrapingConfigurationAsync(string domain);
    }

    public class DomainScrapingProfileService : IDomainScrapingProfileService
    {
        private readonly ScrapingOrchestrationDbContext _context;
        private readonly ILogger<DomainScrapingProfileService> _logger;
        private readonly Random _random = new();

        public DomainScrapingProfileService(
            ScrapingOrchestrationDbContext context,
            ILogger<DomainScrapingProfileService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<DomainScrapingProfile> GetOrCreateProfileAsync(string domain)
        {
            var profile = await _context.DomainScrapingProfiles
                .FirstOrDefaultAsync(p => p.Domain == domain);

            if (profile == null)
            {
                profile = CreateDefaultProfile(domain);
                _context.DomainScrapingProfiles.Add(profile);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Created new domain scraping profile for {Domain}", domain);
            }

            return profile;
        }

        public async Task<bool> CanMakeRequestAsync(string domain)
        {
            var profile = await GetOrCreateProfileAsync(domain);
            
            if (profile.NextAllowedRequestAt == null)
                return true;

            return DateTimeOffset.UtcNow >= profile.NextAllowedRequestAt;
        }

        public async Task UpdateLastRequestTimeAsync(string domain)
        {
            var profile = await GetOrCreateProfileAsync(domain);
            var now = DateTimeOffset.UtcNow;
            
            // Calculate randomized delay between min and max
            var delayMs = _random.Next(profile.MinDelayMs, profile.MaxDelayMs + 1);
            
            profile.LastRequestAt = now;
            profile.NextAllowedRequestAt = now.AddMilliseconds(delayMs);
            profile.UpdatedAt = now;

            await _context.SaveChangesAsync();
            
            _logger.LogDebug("Updated last request time for {Domain}. Next allowed at: {NextAllowed}", 
                domain, profile.NextAllowedRequestAt);
        }

        public async Task<(string UserAgent, Dictionary<string, string> Headers)> GetScrapingConfigurationAsync(string domain)
        {
            var profile = await GetOrCreateProfileAsync(domain);
            
            // Select a random user agent
            var userAgent = profile.UserAgentList[_random.Next(profile.UserAgentList.Count)];
            
            // Select a random header profile
            var headerProfileKeys = profile.HeaderProfiles.Keys.ToList();
            var selectedHeaderProfile = headerProfileKeys.Count > 0 
                ? profile.HeaderProfiles[headerProfileKeys[_random.Next(headerProfileKeys.Count)]]
                : new Dictionary<string, string>();

            return (userAgent, selectedHeaderProfile);
        }

        private DomainScrapingProfile CreateDefaultProfile(string domain)
        {
            var now = DateTimeOffset.UtcNow;
            
            return new DomainScrapingProfile
            {
                Domain = domain,
                UserAgentList = GetDefaultUserAgents(),
                HeaderProfiles = GetDefaultHeaderProfiles(),
                MinDelayMs = 2000, // 2 seconds
                MaxDelayMs = 8000, // 8 seconds
                CreatedAt = now,
                UpdatedAt = now
            };
        }

        private List<string> GetDefaultUserAgents()
        {
            return new List<string>
            {
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36",
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/119.0.0.0 Safari/537.36",
                "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36",
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:121.0) Gecko/20100101 Firefox/121.0",
                "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/17.2 Safari/605.1.15"
            };
        }

        private Dictionary<string, Dictionary<string, string>> GetDefaultHeaderProfiles()
        {
            return new Dictionary<string, Dictionary<string, string>>
            {
                ["chrome_windows"] = new Dictionary<string, string>
                {
                    ["Accept"] = "text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*/*;q=0.8",
                    ["Accept-Language"] = "en-US,en;q=0.9",
                    ["Accept-Encoding"] = "gzip, deflate, br",
                    ["DNT"] = "1",
                    ["Connection"] = "keep-alive",
                    ["Upgrade-Insecure-Requests"] = "1",
                    ["Sec-Fetch-Dest"] = "document",
                    ["Sec-Fetch-Mode"] = "navigate",
                    ["Sec-Fetch-Site"] = "none",
                    ["Sec-Fetch-User"] = "?1",
                    ["sec-ch-ua"] = "\"Not_A Brand\";v=\"8\", \"Chromium\";v=\"120\", \"Google Chrome\";v=\"120\"",
                    ["sec-ch-ua-mobile"] = "?0",
                    ["sec-ch-ua-platform"] = "\"Windows\""
                },
                ["firefox_windows"] = new Dictionary<string, string>
                {
                    ["Accept"] = "text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,*/*;q=0.8",
                    ["Accept-Language"] = "en-US,en;q=0.5",
                    ["Accept-Encoding"] = "gzip, deflate, br",
                    ["DNT"] = "1",
                    ["Connection"] = "keep-alive",
                    ["Upgrade-Insecure-Requests"] = "1",
                    ["Sec-Fetch-Dest"] = "document",
                    ["Sec-Fetch-Mode"] = "navigate",
                    ["Sec-Fetch-Site"] = "none",
                    ["Sec-Fetch-User"] = "?1"
                },
                ["safari_mac"] = new Dictionary<string, string>
                {
                    ["Accept"] = "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8",
                    ["Accept-Language"] = "en-US,en;q=0.9",
                    ["Accept-Encoding"] = "gzip, deflate, br",
                    ["Connection"] = "keep-alive",
                    ["Upgrade-Insecure-Requests"] = "1",
                    ["Sec-Fetch-Dest"] = "document",
                    ["Sec-Fetch-Mode"] = "navigate",
                    ["Sec-Fetch-Site"] = "none"
                }
            };
        }
    }
}
