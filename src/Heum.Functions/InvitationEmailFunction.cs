using Azure.Messaging.ServiceBus;
using Heum.Contracts.Events;
using Heum.Functions.Handlers;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace Heum.Functions;

/// <summary>
/// Service Bus trigger adapter. Deserializes and validates the message, then delegates
/// all business logic to <see cref="InvitationEmailHandler"/>.
/// </summary>
public class InvitationEmailFunction(
    InvitationEmailHandler handler,
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

        try
        {
            await handler.HandleAsync(@event, cancellationToken);
        }
        catch (Exception ex)
        {
            // Rethrow so Service Bus can retry and eventually dead-letter the message.
            logger.LogError(ex,
                "Failed to send invitation email to {Email} for tenant {TenantId}.",
                @event.Email, @event.TenantId);
            throw;
        }
    }
}
