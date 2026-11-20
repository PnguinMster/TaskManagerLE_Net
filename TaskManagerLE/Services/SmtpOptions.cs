namespace TaskManagerLE.Services;

// Bound from the "Smtp" config section (options pattern). Keep Password out of committed files:
// use user-secrets locally, or the Smtp__Password environment variable on the host.
public class SmtpOptions
{
    public const string SectionName = "Smtp";

    public string Host { get; set; } = "";
    public int Port { get; set; } = 587;
    public string UserName { get; set; } = "";
    public string Password { get; set; } = "";
    public string FromAddress { get; set; } = "";
    public string FromName { get; set; } = "TaskManagerLE";
}
