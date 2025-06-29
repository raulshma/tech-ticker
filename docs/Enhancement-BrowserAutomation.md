## Enhancement: Browser Automation Integration

**Purpose:**  
Enable scraping of JavaScript-heavy e-commerce sites that cannot be reliably scraped using static HTTP requests and HTML parsing. This enhancement introduces browser automation (using Playwright or Puppeteer) to render pages, execute JavaScript, and extract dynamic content, significantly expanding the range of supported sites.

---

### 1. Overview

Many modern e-commerce sites rely on client-side JavaScript to render product details, prices, and stock status. Traditional scraping methods (e.g., `HttpClient` + `HtmlAgilityPack`) are insufficient for these sites. Integrating browser automation allows TechTicker to:

- Render and interact with JavaScript-heavy pages
- Extract content that only appears after scripts execute or after user-like interactions (e.g., scrolling, clicking)
- Bypass some anti-bot measures by mimicking real browser behavior

---

### 2. Technology Stack

- **Primary Libraries:**  
  - **Playwright** (preferred for .NET integration, cross-browser support, and modern API)  
  - **Puppeteer** (alternative, especially if Node.js-based microservice is preferred)
- **Integration Approach:**  
  - .NET wrapper for Playwright (`Microsoft.Playwright`)
  - Browser automation services run in isolated containers with required browser binaries (Chromium, Firefox, WebKit)
  - Resource limits and security hardening applied to containers

---

### 3. Architecture & Workflow

1. **ScrapingOrchestrationModule** determines if a mapping requires browser automation (based on site config or admin override).
2. **ScrapeProductPageCommand** includes a flag or profile indicating "browser required".
3. **ScraperModule** routes the command:
   - If browser automation is required, invokes Playwright/Puppeteer service.
   - Otherwise, uses standard HTTP + HTML parsing.
4. **Browser Automation Service**:
   - Launches a headless browser instance (optionally headful for debugging)
   - Navigates to the target URL using a realistic user-agent and proxy (if configured)
   - Waits for page load and optionally for specific selectors to appear
   - Executes custom scripts if needed (e.g., scrolling, clicking "show price" buttons)
   - Extracts required data using selectors (CSS/XPath)
   - Returns HTML or structured data to the ScraperModule
5. **Data Extraction & Normalization** proceeds as usual.

---

### 4. Configuration

- **Site Configuration:**
  - `RequiresBrowserAutomation` (bool): Indicates if browser automation is needed for this site
  - `BrowserAutomationProfile`: Optional settings (e.g., wait time, actions to perform, preferred browser)
- **Scraping Settings:**
  - `BrowserAutomation:Enabled` (global toggle)
  - `BrowserAutomation:MaxConcurrentBrowsers` (resource control)
  - `BrowserAutomation:DefaultTimeoutSeconds`
  - `BrowserAutomation:AllowedDomains` (security whitelist)
- **Proxy Support:**  
  - Full support for proxies in browser context (HTTP, SOCKS)
- **User-Agent & Headers:**  
  - Settable per browser context/session

---

### 5. Security & Resource Management

- **Isolation:**  
  - Each browser instance runs in a sandboxed environment/container
- **Resource Limits:**  
  - Max concurrent browsers, memory, and CPU limits enforced
- **Timeouts:**  
  - Hard timeouts for navigation and extraction to prevent resource exhaustion
- **Access Control:**  
  - Only allow scraping of whitelisted domains to prevent abuse

---

### 6. Error Handling & Logging

- **Error Types:**  
  - Navigation errors, selector timeouts, JavaScript execution errors, CAPTCHA detection
- **Retries:**  
  - Configurable retry logic for transient failures
- **Logging:**  
  - Detailed logs for browser actions, errors, and resource usage
- **Metrics:**  
  - Track browser usage, success/failure rates, and performance

---

### 7. API & Messaging Changes

- **ScrapeProductPageCommand**:
  - Add `requiresBrowserAutomation` flag
  - Add `browserAutomationProfile` (optional)
- **ScrapingResultEvent**:
  - Include browser-specific error codes (e.g., "NAVIGATION_TIMEOUT", "JS_ERROR", "CAPTCHA_DETECTED")
- **SiteConfig API**:
  - Support for setting and updating browser automation requirements

---

### 8. Testing & Monitoring

- **Automated Tests:**  
  - Unit and integration tests for browser automation flows
  - Mocked browser sessions for CI environments
- **Monitoring:**  
  - Real-time dashboard for browser automation health and resource usage
  - Alerts for excessive failures or resource exhaustion

---

### 9. Admin Controls

- **Admin UI:**  
  - Indicate which mappings/sites require browser automation
  - Show browser automation status and recent errors in Scraper Logs
  - Allow manual triggering of browser-based scraping for testing

---

### 10. Future Considerations

- **Advanced Evasion:**  
  - Stealth plugins, CAPTCHA solving, and human-like interaction scripting
- **Distributed Browser Grid:**  
  - Scale browser automation horizontally across multiple nodes/containers
- **Fallback Logic:**  
  - Automatically switch between HTTP and browser automation based on failure patterns

---

**Status:**  
*Planned/Proposed. Not yet implemented in production. This enhancement will be prioritized for sites where static scraping is insufficient.*

---

**References:**  
- [Playwright for .NET Documentation](https://playwright.dev/dotnet/docs/intro)
- [Puppeteer Documentation](https://pptr.dev/)
- [Microsoft.Playwright NuGet](https://www.nuget.org/packages/Microsoft.Playwright/) 