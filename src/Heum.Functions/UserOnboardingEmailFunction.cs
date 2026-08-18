using Azure.Messaging.ServiceBus;
using Heum.Contracts.Events;
using Heum.Infrastructure.Keycloak.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace Heum.Functions;

/// <summary>
/// Consumes UserOnboardingRequestedEvent messages from the user-events topic and asks Keycloak
/// to email the new user a link that lets them set a password. Running this out-of-band keeps
/// the tenant registration / add-user HTTP requests fast and lets Service Bus retry transient
/// Keycloak failures.
/// </summary>
public class UserOnboardingEmailFunction(
    IKeycloakService keycloakService,
    ILogger<UserOnboardingEmailFunction> logger)
{
    private const string UpdatePasswordAction = "UPDATE_PASSWORD";

    [Function(nameof(UserOnboardingEmailFunction))]
    public async Task RunAsync(
        [ServiceBusTrigger("user-events", "user-onboarding-sub", Connection = "messaging")]
        ServiceBusReceivedMessage message,
        CancellationToken cancellationToken)
    {
        UserOnboardingRequestedEvent? @event;
        try
        {
            @event = message.Body.ToObjectFromJson<UserOnboardingRequestedEvent>();
        }
        catch (Exception ex)
        {
            // Malformed payloads will never succeed, so complete them instead of retrying forever.
            logger.LogError(ex, "Could not deserialize message {MessageId} into a UserOnboardingRequestedEvent.", message.MessageId);
            return;
        }

        if (@event is null || string.IsNullOrWhiteSpace(@event.KeycloakUserId))
        {
            logger.LogError("Message {MessageId} did not contain a Keycloak user id; skipping.", message.MessageId);
            return;
        }

        logger.LogInformation(
            "Sending onboarding email for tenant {TenantId} to Keycloak user {KeycloakUserId} ({Email}).",
            @event.TenantId, @event.KeycloakUserId, @event.Email);

        try
        {
            await keycloakService.SendRequiredActionsEmailAsync(
                @event.KeycloakUserId,
                [UpdatePasswordAction],
                cancellationToken);

            logger.LogInformation(
                "Onboarding email requested for tenant {TenantId} user {Email}.",
                @event.TenantId, @event.Email);
        }
        catch (Exception ex)
        {
            // Rethrow so Service Bus can retry and eventually dead-letter the message.
            logger.LogError(ex,
                "Failed to send the onboarding email for tenant {TenantId} (Keycloak user {KeycloakUserId}).",
                @event.TenantId, @event.KeycloakUserId);
            throw;
        }
    }
}
