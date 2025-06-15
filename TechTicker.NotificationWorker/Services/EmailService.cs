using MailKit.Net.Smtp;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;
using TechTicker.Application.Messages;
using TechTicker.NotificationWorker.Configuration;

namespace TechTicker.NotificationWorker.Services;

/// <summary>
/// Service for sending email notifications
/// </summary>
public class EmailService
{
    private readonly EmailConfiguration _emailConfig;
    private readonly ILogger<EmailService> _logger;

    public EmailService(
        IOptions<EmailConfiguration> emailConfig,
        ILogger<EmailService> logger)
    {
        _emailConfig = emailConfig.Value;
        _logger = logger;
    }

    public async Task SendAlertNotificationAsync(AlertTriggeredEvent alertEvent)
    {
        try
        {
            if (!_emailConfig.EnableEmailSending)
            {
                _logger.LogInformation("Email sending is disabled. Skipping alert notification for user {UserId}", alertEvent.UserId);
                return;
            }

            var message = CreateAlertEmailMessage(alertEvent);
            await SendEmailAsync(message);

            _logger.LogInformation("Successfully sent alert notification email to {Email} for product {ProductName}", 
                alertEvent.UserEmail, alertEvent.ProductName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send alert notification email to {Email}", alertEvent.UserEmail);
            throw;
        }
    }

    private MimeMessage CreateAlertEmailMessage(AlertTriggeredEvent alertEvent)
    {
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(_emailConfig.FromName, _emailConfig.FromEmail));
        message.To.Add(new MailboxAddress(alertEvent.UserFirstName ?? alertEvent.UserEmail, alertEvent.UserEmail));
        
        var subject = GetAlertSubject(alertEvent);
        message.Subject = subject;

        var bodyBuilder = new BodyBuilder();
        bodyBuilder.HtmlBody = CreateAlertEmailHtml(alertEvent);
        bodyBuilder.TextBody = CreateAlertEmailText(alertEvent);
        
        message.Body = bodyBuilder.ToMessageBody();

        return message;
    }

    private string GetAlertSubject(AlertTriggeredEvent alertEvent)
    {
        return alertEvent.RuleDescription switch
        {
            var desc when desc.Contains("Price below") => $"🔔 Price Alert: {alertEvent.ProductName} is now ${alertEvent.TriggeringPrice:F2}",
            var desc when desc.Contains("drop") => $"📉 Price Drop Alert: {alertEvent.ProductName} price dropped!",
            var desc when desc.Contains("Back in stock") => $"📦 Stock Alert: {alertEvent.ProductName} is back in stock!",
            _ => $"🔔 TechTicker Alert: {alertEvent.ProductName}"
        };
    }

    private string CreateAlertEmailHtml(AlertTriggeredEvent alertEvent)
    {
        var greeting = !string.IsNullOrEmpty(alertEvent.UserFirstName) 
            ? $"Hi {alertEvent.UserFirstName}," 
            : "Hello,";

        var categoryInfo = !string.IsNullOrEmpty(alertEvent.ProductCategoryName) 
            ? $" in {alertEvent.ProductCategoryName}" 
            : "";

        return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset=""utf-8"">
    <title>TechTicker Alert</title>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background-color: #2c3e50; color: white; padding: 20px; text-align: center; }}
        .content {{ padding: 20px; background-color: #f8f9fa; }}
        .alert-box {{ background-color: #d4edda; border: 1px solid #c3e6cb; padding: 15px; margin: 15px 0; border-radius: 5px; }}
        .price {{ font-size: 24px; font-weight: bold; color: #28a745; }}
        .button {{ display: inline-block; padding: 12px 24px; background-color: #007bff; color: white; text-decoration: none; border-radius: 5px; margin: 10px 0; }}
        .footer {{ text-align: center; padding: 20px; color: #666; font-size: 12px; }}
    </style>
</head>
<body>
    <div class=""container"">
        <div class=""header"">
            <h1>🔔 TechTicker Alert</h1>
        </div>
        <div class=""content"">
            <p>{greeting}</p>
            
            <p>Great news! Your price alert for <strong>{alertEvent.ProductName}</strong>{categoryInfo} has been triggered.</p>
            
            <div class=""alert-box"">
                <h3>Alert Details:</h3>
                <p><strong>Product:</strong> {alertEvent.ProductName}</p>
                <p><strong>Seller:</strong> {alertEvent.SellerName}</p>
                <p><strong>Current Price:</strong> <span class=""price"">${alertEvent.TriggeringPrice:F2}</span></p>
                <p><strong>Stock Status:</strong> {alertEvent.TriggeringStockStatus}</p>
                <p><strong>Alert Rule:</strong> {alertEvent.RuleDescription}</p>
            </div>
            
            <p>Don't miss out on this opportunity!</p>
            
            <a href=""{alertEvent.ProductPageUrl}"" class=""button"">View Product Page</a>
            
            <p><small>This alert was triggered on {alertEvent.Timestamp:MMMM dd, yyyy} at {alertEvent.Timestamp:HH:mm} UTC.</small></p>
        </div>
        <div class=""footer"">
            <p>You're receiving this email because you set up a price alert on TechTicker.</p>
            <p>© 2024 TechTicker. All rights reserved.</p>
        </div>
    </div>
</body>
</html>";
    }

    private string CreateAlertEmailText(AlertTriggeredEvent alertEvent)
    {
        var greeting = !string.IsNullOrEmpty(alertEvent.UserFirstName) 
            ? $"Hi {alertEvent.UserFirstName}," 
            : "Hello,";

        var categoryInfo = !string.IsNullOrEmpty(alertEvent.ProductCategoryName) 
            ? $" in {alertEvent.ProductCategoryName}" 
            : "";

        return $@"{greeting}

Great news! Your price alert for {alertEvent.ProductName}{categoryInfo} has been triggered.

Alert Details:
- Product: {alertEvent.ProductName}
- Seller: {alertEvent.SellerName}
- Current Price: ${alertEvent.TriggeringPrice:F2}
- Stock Status: {alertEvent.TriggeringStockStatus}
- Alert Rule: {alertEvent.RuleDescription}

Don't miss out on this opportunity!

View the product page: {alertEvent.ProductPageUrl}

This alert was triggered on {alertEvent.Timestamp:MMMM dd, yyyy} at {alertEvent.Timestamp:HH:mm} UTC.

---
You're receiving this email because you set up a price alert on TechTicker.
© 2024 TechTicker. All rights reserved.";
    }

    private async Task SendEmailAsync(MimeMessage message)
    {
        using var client = new SmtpClient();
        
        try
        {
            await client.ConnectAsync(_emailConfig.SmtpHost, _emailConfig.SmtpPort, _emailConfig.UseSsl);
            
            if (!string.IsNullOrEmpty(_emailConfig.Username))
            {
                await client.AuthenticateAsync(_emailConfig.Username, _emailConfig.Password);
            }
            
            await client.SendAsync(message);
            await client.DisconnectAsync(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SMTP error while sending email to {To}", message.To.ToString());
            throw;
        }
    }
}
