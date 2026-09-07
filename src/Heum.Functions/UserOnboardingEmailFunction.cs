using Azure.Messaging.ServiceBus;
using Heum.Contracts.Events;
using Heum.Functions.Handlers;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace Heum.Functions;

/// <summary>
/// Service Bus trigger adapter. Deserializes and validates the message, then delegates
/// all business logic to <see cref="UserOnboardingHandler"/>.
/// </summary>
public class UserOnboardingEmailFunction(
    UserOnboardingHandler handler,
    ILogger<UserOnboardingEmailFunction> logger)
{
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

        try
        {
            await handler.HandleAsync(@event, cancellationToken);
        }
        catch (Exception ex)
        {
            // Rethrow so Service Bus can retry and eventually dead-letter the message.
            logger.LogError(ex,
                "Failed to send the onboarding email for tenant {TenantId} (user {KeycloakUserId}).",
                @event.TenantId, @event.KeycloakUserId);
            throw;
        }
    }
}
