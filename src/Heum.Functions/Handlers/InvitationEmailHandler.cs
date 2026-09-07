using System.Net.Mail;
using Heum.Contracts.Events;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Heum.Functions.Handlers;

/// <summary>
/// Encapsulates the business logic for sending an invitation email via SMTP. Decoupled from
/// the Service Bus trigger so the same logic can be driven by a Worker or a test without an
/// Azure Functions host.
/// </summary>
public sealed class InvitationEmailHandler(
    IOptions<SmtpOptions> smtpOptions,
    ILogger<InvitationEmailHandler> logger)
{
    public async Task HandleAsync(InvitationCreatedEvent evt, CancellationToken ct)
    {
        var opts = smtpOptions.Value;
        var acceptUrl = $"{opts.AppBaseUrl.TrimEnd('/')}/accept-invitation?token={Uri.EscapeDataString(evt.Token)}";

        logger.LogInformation(
            "Sending invitation email for tenant {TenantId} to {Email}.",
            evt.TenantId, evt.Email);

        using var smtp = new SmtpClient(opts.Host, opts.Port);
        using var mail = new MailMessage
        {
            From = new MailAddress(opts.FromAddress),
            Subject = "You've been invited",
            Body = $"""
                <p>You have been invited to join an organization.</p>
                <p><a href="{acceptUrl}">Accept your invitation</a></p>
                <p>This invitation expires in 7 days. If you did not expect this email, you can safely ignore it.</p>
                """,
            IsBodyHtml = true,
        };
        mail.To.Add(evt.Email);
        await smtp.SendMailAsync(mail, ct);

        logger.LogInformation(
            "Invitation email sent to {Email} for tenant {TenantId}.",
            evt.Email, evt.TenantId);
    }
}
