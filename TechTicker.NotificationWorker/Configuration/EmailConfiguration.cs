namespace TechTicker.NotificationWorker.Configuration;

/// <summary>
/// Configuration for email settings
/// </summary>
public class EmailConfiguration
{
    public const string SectionName = "Email";

    public string SmtpHost { get; set; } = "localhost";
    public int SmtpPort { get; set; } = 587;
    public bool UseSsl { get; set; } = true;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string FromEmail { get; set; } = "noreply@techticker.com";
    public string FromName { get; set; } = "TechTicker";
    public bool EnableEmailSending { get; set; } = true;
}
