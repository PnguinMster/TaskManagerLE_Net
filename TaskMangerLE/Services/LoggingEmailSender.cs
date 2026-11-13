using Microsoft.AspNetCore.Identity.UI.Services;

namespace TaskMangerLE.Services;

// Dev-only stand-in for a real mail provider: "sends" an email by writing it to the console log.
// To send real mail, write another IEmailSender (e.g. SMTP) and register it in Program.cs instead.
public class LoggingEmailSender(ILogger<LoggingEmailSender> logger) : IEmailSender
{
    public Task SendEmailAsync(string email, string subject, string htmlMessage)
    {
        logger.LogInformation("Email to {Email} | {Subject}\n{Body}", email, subject, htmlMessage);
        return Task.CompletedTask;
    }
}
