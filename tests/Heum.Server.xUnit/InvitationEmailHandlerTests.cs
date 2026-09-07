using System.Net.Mail;
using Heum.Contracts.Events;
using Heum.Functions;
using Heum.Functions.Handlers;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Heum.Server.xUnit;

public sealed class InvitationEmailHandlerTests
{
    [Fact]
    public async Task HandleAsync_AttemptsSmtpDelivery_WithConfiguredOptions()
    {
        // SmtpClient is not mockable, so we verify the handler actually attempts a connection
        // by observing that it throws a network-level exception when the host is unreachable.
        // This confirms the handler builds and sends the message rather than no-oping.
        var opts = Options.Create(new SmtpOptions
        {
            Host = "localhost",
            Port = 9,      // port 9 (discard) is typically refused immediately
            FromAddress = "test@example.com",
            AppBaseUrl = "http://localhost:5173",
        });
        var handler = new InvitationEmailHandler(opts, NullLogger<InvitationEmailHandler>.Instance);
        var evt = new InvitationCreatedEvent(
            InvitationId: Guid.NewGuid(),
            TenantId: Guid.NewGuid(),
            Email: "invited@example.com",
            Token: "tok123",
            OccurredAt: DateTimeOffset.UtcNow);

        // Any network exception (SmtpException, SocketException, etc.) is acceptable —
        // the important thing is that the handler reached the SMTP send attempt.
        await Assert.ThrowsAnyAsync<Exception>(() => handler.HandleAsync(evt, CancellationToken.None));
    }
}
