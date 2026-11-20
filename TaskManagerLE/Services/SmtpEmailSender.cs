using MailKit;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Extensions.Options;
using MimeKit;

namespace TaskManagerLE.Services;

// Sends real mail through an SMTP provider (Gmail with an app password for this demo). Used outside Development; see Program.cs.
public class SmtpEmailSender(IOptions<SmtpOptions> options) : IEmailSender
{
    private readonly SmtpOptions _smtp = options.Value;

    public async Task SendEmailAsync(string email, string subject, string htmlMessage)
    {
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(_smtp.FromName, _smtp.FromAddress));
        message.To.Add(MailboxAddress.Parse(email));
        message.Subject = subject;
        message.Body = new TextPart("html") { Text = htmlMessage };

        using var client = new SmtpClient();
        // StartTls on port 587: connect in plain text, then upgrade to an encrypted connection before logging in.
        await client.ConnectAsync(_smtp.Host, _smtp.Port, SecureSocketOptions.StartTls);
        await client.AuthenticateAsync(_smtp.UserName, _smtp.Password);
        // MailKit 5 wraps the message in a SendRequest; sender and recipients come from the message headers.
        await client.SendAsync(new SendRequest(message));
        await client.DisconnectAsync(true);
    }
}
