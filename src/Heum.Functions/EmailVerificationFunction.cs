using Azure.Messaging.ServiceBus;
using Heum.Contracts.Events;
using Heum.Server.Services.Keycloak;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace Heum.Functions;

/// <summary>
/// Consumes TenantCreatedEvent messages from the tenant-events topic and asks Keycloak to email
/// the freshly provisioned tenant admin a VERIFY_EMAIL action link. Running this out-of-band keeps
/// the tenant registration HTTP request fast and lets Service Bus retry transient Keycloak failures.
/// </summary>
public class EmailVerificationFunction(
    IKeycloakAdminClient keycloakAdminClient,
    ILogger<EmailVerificationFunction> logger)
{
    private const string VerifyEmailAction = "VERIFY_EMAIL";

    [Function(nameof(EmailVerificationFunction))]
    public async Task RunAsync(
        [ServiceBusTrigger("tenant-events", "email-verification-sub", Connection = "messaging")]
        ServiceBusReceivedMessage message,
        CancellationToken cancellationToken)
    {
        TenantCreatedEvent? @event;
        try
        {
            @event = message.Body.ToObjectFromJson<TenantCreatedEvent>();
        }
        catch (Exception ex)
        {
            // Malformed payloads will never succeed, so complete them instead of retrying forever.
            logger.LogError(ex, "Could not deserialize message {MessageId} into a TenantCreatedEvent.", message.MessageId);
            return;
        }

        if (@event is null || string.IsNullOrWhiteSpace(@event.KeycloakUserId))
        {
            logger.LogError("Message {MessageId} did not contain a Keycloak user id; skipping.", message.MessageId);
            return;
        }

        logger.LogInformation(
            "Sending email verification for tenant {TenantId} ({Slug}) to Keycloak user {KeycloakUserId}.",
            @event.TenantId, @event.Slug, @event.KeycloakUserId);

        try
        {
            await keycloakAdminClient.SendRequiredActionsEmailAsync(
                @event.KeycloakUserId,
                [VerifyEmailAction],
                cancellationToken);

            logger.LogInformation(
                "Email verification requested for tenant {TenantId} admin {AdminEmail}.",
                @event.TenantId, @event.AdminEmail);
        }
        catch (Exception ex)
        {
            // Rethrow so Service Bus can retry and eventually dead-letter the message.
            logger.LogError(ex,
                "Failed to send the email verification link for tenant {TenantId} (Keycloak user {KeycloakUserId}).",
                @event.TenantId, @event.KeycloakUserId);
            throw;
        }
    }
}
