using Heum.Contracts.Events;
using Heum.Infrastructure.Keycloak.Services;
using Microsoft.Extensions.Logging;

namespace Heum.Functions.Handlers;

/// <summary>
/// Encapsulates the business logic for onboarding a new user: asks the identity provider to
/// email them a password-set link. Decoupled from the Service Bus trigger so the same logic
/// can be driven by a Worker or a test without an Azure Functions host.
/// </summary>
public sealed class UserOnboardingHandler(
    IIdentityProviderService identityProvider,
    ILogger<UserOnboardingHandler> logger)
{
    private const string UpdatePasswordAction = "UPDATE_PASSWORD";

    public async Task HandleAsync(UserOnboardingRequestedEvent evt, CancellationToken ct)
    {
        logger.LogInformation(
            "Sending onboarding email for tenant {TenantId} to user {KeycloakUserId} ({Email}).",
            evt.TenantId, evt.KeycloakUserId, evt.Email);

        await identityProvider.SendRequiredActionsEmailAsync(
            evt.KeycloakUserId,
            [UpdatePasswordAction],
            ct);

        logger.LogInformation(
            "Onboarding email requested for tenant {TenantId} user {Email}.",
            evt.TenantId, evt.Email);
    }
}
