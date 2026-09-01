using System.Net;
using Heum.Data;
using Heum.Data.Models;
using Heum.Server.Features.Invitations.Models;
using Heum.Server.xIntegration.Clients;
using Heum.Server.xIntegration.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Heum.Server.xIntegration.Tests;

[Collection(nameof(IntegrationCollection))]
public class InvitationsEndpointTests(IntegrationFixture fixture) : IAsyncLifetime
{
    private Guid _tenantId;

    async ValueTask IAsyncLifetime.InitializeAsync()
    {
        await fixture.ResetDatabaseAsync();

        await using var scope = fixture.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<HeumDbContext>();
        var tenant = Tenant.Register("Invite Corp", "invite-corp", TimeProvider.System);
        var settings = TenantSettings.CreateDefault(tenant.Id, TimeProvider.System);
        db.Tenants.Add(tenant);
        db.TenantSettings.Add(settings);
        await db.SaveChangesAsync();
        _tenantId = tenant.Id;
    }

    ValueTask IAsyncDisposable.DisposeAsync() => ValueTask.CompletedTask;

    // --- POST /api/invitations (create) ---

    [Fact]
    public async Task CreateInvitation_Returns201_ForTenantAdmin()
    {
        var api = fixture.GetClient<IInvitationsApi>(ClientScope.TenantAdmin(_tenantId));

        var response = await api.CreateInvitationAsync(
            new CreateInvitationRequest { Email = "newuser@invite.com" },
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal("newuser@invite.com", response.Content!.Email);
        Assert.Equal("Pending", response.Content.Status);
    }

    [Fact]
    public async Task CreateInvitation_Returns409_ForDuplicatePendingEmail()
    {
        var api = fixture.GetClient<IInvitationsApi>(ClientScope.TenantAdmin(_tenantId));

        await api.CreateInvitationAsync(
            new CreateInvitationRequest { Email = "dup@invite.com" },
            TestContext.Current.CancellationToken);

        var response = await api.CreateInvitationAsync(
            new CreateInvitationRequest { Email = "dup@invite.com" },
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task CreateInvitation_Returns401_WithoutAuth()
    {
        var api = fixture.GetClient<IInvitationsApi>(ClientScope.Anonymous);

        var response = await api.CreateInvitationAsync(
            new CreateInvitationRequest { Email = "anon@invite.com" },
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task CreateInvitation_Returns403_ForNonAdminRole()
    {
        var api = fixture.GetClient<IInvitationsApi>(ClientScope.Authenticated("User", _tenantId));

        var response = await api.CreateInvitationAsync(
            new CreateInvitationRequest { Email = "user@invite.com" },
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    // --- GET /api/invitations ---

    [Fact]
    public async Task ListInvitations_Returns200_WithCreatedInvitations()
    {
        var api = fixture.GetClient<IInvitationsApi>(ClientScope.TenantAdmin(_tenantId));
        await api.CreateInvitationAsync(
            new CreateInvitationRequest { Email = "list1@invite.com" },
            TestContext.Current.CancellationToken);
        await api.CreateInvitationAsync(
            new CreateInvitationRequest { Email = "list2@invite.com" },
            TestContext.Current.CancellationToken);

        var response = await api.ListInvitationsAsync(cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(2, response.Content!.TotalCount);
    }

    [Fact]
    public async Task ListInvitations_Returns401_WithoutAuth()
    {
        var api = fixture.GetClient<IInvitationsApi>(ClientScope.Anonymous);

        var response = await api.ListInvitationsAsync(cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // --- POST /api/invitations/{id}/revoke ---

    [Fact]
    public async Task RevokeInvitation_Returns204_ForPendingInvitation()
    {
        var api = fixture.GetClient<IInvitationsApi>(ClientScope.TenantAdmin(_tenantId));
        var created = await api.CreateInvitationAsync(
            new CreateInvitationRequest { Email = "revoke@invite.com" },
            TestContext.Current.CancellationToken);

        var response = await api.RevokeInvitationAsync(
            created.Content!.Id,
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task RevokeInvitation_Returns404_ForUnknownId()
    {
        var api = fixture.GetClient<IInvitationsApi>(ClientScope.TenantAdmin(_tenantId));

        var response = await api.RevokeInvitationAsync(
            Guid.NewGuid(),
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task RevokeInvitation_Returns401_WithoutAuth()
    {
        var api = fixture.GetClient<IInvitationsApi>(ClientScope.Anonymous);

        var response = await api.RevokeInvitationAsync(
            Guid.NewGuid(),
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // --- POST /api/invitations/accept ---

    [Fact]
    public async Task AcceptInvitation_Returns200_WithValidToken()
    {
        var api = fixture.GetClient<IInvitationsApi>(ClientScope.TenantAdmin(_tenantId));
        var created = await api.CreateInvitationAsync(
            new CreateInvitationRequest { Email = "accept@invite.com" },
            TestContext.Current.CancellationToken);

        await using var scope = fixture.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<HeumDbContext>();
        var invitation = await db.Invitations
            .IgnoreQueryFilters()
            .FirstAsync(i => i.Id == created.Content!.Id, TestContext.Current.CancellationToken);

        var anonApi = fixture.GetClient<IInvitationsApi>(ClientScope.Anonymous);
        var response = await anonApi.AcceptInvitationAsync(
            new AcceptInvitationRequest { Token = invitation.Token },
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task AcceptInvitation_Returns400_WithInvalidToken()
    {
        var api = fixture.GetClient<IInvitationsApi>(ClientScope.Anonymous);

        var response = await api.AcceptInvitationAsync(
            new AcceptInvitationRequest { Token = "nonexistent-token" },
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task AcceptInvitation_Returns409_WhenKeycloakEmailConflicts()
    {
        var api = fixture.GetClient<IInvitationsApi>(ClientScope.TenantAdmin(_tenantId));
        var created = await api.CreateInvitationAsync(
            new CreateInvitationRequest { Email = "conflict@invite.com" },
            TestContext.Current.CancellationToken);

        await using var scope = fixture.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<HeumDbContext>();
        var invitation = await db.Invitations
            .IgnoreQueryFilters()
            .FirstAsync(i => i.Id == created.Content!.Id, TestContext.Current.CancellationToken);

        fixture.FakeKeycloak.ExceptionToThrow =
            new HttpRequestException("Conflict", null, HttpStatusCode.Conflict);

        var anonApi = fixture.GetClient<IInvitationsApi>(ClientScope.Anonymous);
        var response = await anonApi.AcceptInvitationAsync(
            new AcceptInvitationRequest { Token = invitation.Token },
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task AcceptInvitation_IsAccessibleWithoutAuth()
    {
        var api = fixture.GetClient<IInvitationsApi>(ClientScope.Anonymous);

        var response = await api.AcceptInvitationAsync(
            new AcceptInvitationRequest { Token = "any-token" },
            TestContext.Current.CancellationToken);

        Assert.NotEqual(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.NotEqual(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
