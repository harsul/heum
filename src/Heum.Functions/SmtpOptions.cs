namespace Heum.Functions;

public sealed class SmtpOptions
{
    public string Host { get; set; } = "localhost";
    public int Port { get; set; } = 1025;
    public string FromAddress { get; set; } = "noreply@example.com";
    public string AppBaseUrl { get; set; } = "http://localhost:5173";
}
