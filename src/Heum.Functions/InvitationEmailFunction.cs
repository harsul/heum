using Azure.Messaging.ServiceBus;
using Heum.Contracts.Events;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net.Mail;

namespace Heum.Functions;

/// <summary>
/// Consumes InvitationCreatedEvent messages and emails the invited address a link containing
/// the invitation token so the recipient can accept and complete onboarding.
/// </summary>
public class InvitationEmailFunction(
    IOptions<SmtpOptions> smtpOptions,
    ILogger<InvitationEmailFunction> logger)
{
    [Function(nameof(InvitationEmailFunction))]
    public async Task RunAsync(
        [ServiceBusTrigger("user-events", "invitation-email-sub", Connection = "messaging")]
        ServiceBusReceivedMessage message,
        CancellationToken cancellationToken)
    {
        InvitationCreatedEvent? @event;
        try
        {
            @event = message.Body.ToObjectFromJson<InvitationCreatedEvent>();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Could not deserialize message {MessageId} into an InvitationCreatedEvent.", message.MessageId);
            return;
        }

        if (@event is null || string.IsNullOrWhiteSpace(@event.Email))
        {
            logger.LogError("Message {MessageId} did not contain a valid invitation; skipping.", message.MessageId);
            return;
        }

        var opts = smtpOptions.Value;
        var acceptUrl = $"{opts.AppBaseUrl.TrimEnd('/')}/accept-invitation?token={Uri.EscapeDataString(@event.Token)}";

        logger.LogInformation(
            "Sending invitation email for tenant {TenantId} to {Email}.",
            @event.TenantId, @event.Email);

        try
        {
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
            mail.To.Add(@event.Email);
            await smtp.SendMailAsync(mail, cancellationToken);

            logger.LogInformation(
                "Invitation email sent to {Email} for tenant {TenantId}.",
                @event.Email, @event.TenantId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "Failed to send invitation email to {Email} for tenant {TenantId}.",
                @event.Email, @event.TenantId);
            throw;
        }
    }
}
