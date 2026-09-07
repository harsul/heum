using System.Net;
using Heum.Data;
using Heum.Data.Models;
using Heum.Server.Features.Invitations.Services;
using Heum.Server.xUnit.Fakes;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Heum.Server.xUnit;

public sealed class InvitationServiceTests : IDisposable
{
    private readonly HeumDbContext _db;
    private readonly FakeKeycloakService _keycloak = new();
    private readonly FakeDomainEventCollector _events = new();
    private readonly FakeEntitlementService _entitlements = new();
    private readonly InvitationService _service;
    private static readonly Guid TenantId = Guid.NewGuid();

    public InvitationServiceTests()
    {
        var options = new DbContextOptionsBuilder<HeumDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        _db = new HeumDbContext(options, new FakeTenantProvider(TenantId));
        _service = new InvitationService(_db, _keycloak, _events, _entitlements, TimeProvider.System);
    }

    public void Dispose() => _db.Dispose();

    [Fact]
    public async Task CreateAsync_ReturnsInvitation_WhenValid()
    {
        var result = await _service.CreateAsync(TenantId, "new@corp.com", "admin-id", TestContext.Current.CancellationToken);

        Assert.NotNull(result.Invitation);
        Assert.False(result.DuplicatePending);
        Assert.Equal(1, await _db.Invitations.CountAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task CreateAsync_ReturnsDuplicatePending_WhenPendingExists()
    {
        // seed a pending invitation
        _db.Invitations.Add(Invitation.Create(TenantId, "dup@corp.com", "admin", TimeSpan.FromDays(7), TimeProvider.System));
        await _db.SaveChangesAsync();

        var result = await _service.CreateAsync(TenantId, "dup@corp.com", "admin", TestContext.Current.CancellationToken);

        Assert.True(result.DuplicatePending);
        Assert.Null(result.Invitation);
    }

    [Fact]
    public async Task CreateAsync_ReturnsEntitlementExceeded_WhenUserLimitReached()
    {
        _entitlements.IntToReturn = 0; // max_users = 0
        _keycloak.AssignableRolesToReturn = [];

        var result = await _service.CreateAsync(TenantId, "blocked@corp.com", "admin", TestContext.Current.CancellationToken);

        Assert.True(result.EntitlementExceeded);
    }

    [Fact]
    public async Task ListAsync_ReturnsOnlyTenantInvitations()
    {
        var otherTenant = Guid.NewGuid();
        _db.Invitations.Add(Invitation.Create(TenantId, "a@corp.com", "admin", TimeSpan.FromDays(7), TimeProvider.System));
        _db.Invitations.Add(Invitation.Create(otherTenant, "b@other.com", "admin", TimeSpan.FromDays(7), TimeProvider.System));
        await _db.SaveChangesAsync();

        var (items, total) = await _service.ListAsync(TenantId, null, 1, 10, TestContext.Current.CancellationToken);

        Assert.Equal(1, total);
        Assert.All(items, i => Assert.Equal(TenantId, i.TenantId));
    }

    [Fact]
    public async Task ListAsync_WithSearch_FiltersByEmail()
    {
        _db.Invitations.Add(Invitation.Create(TenantId, "alice@corp.com", "admin", TimeSpan.FromDays(7), TimeProvider.System));
        _db.Invitations.Add(Invitation.Create(TenantId, "bob@corp.com", "admin", TimeSpan.FromDays(7), TimeProvider.System));
        await _db.SaveChangesAsync();

        var (items, total) = await _service.ListAsync(TenantId, "alice", 1, 10, TestContext.Current.CancellationToken);

        Assert.Equal(1, total);
        Assert.Equal("alice@corp.com", items[0].Email);
    }

    [Fact]
    public async Task AcceptAsync_ReturnsNotAccepted_WhenTokenInvalid()
    {
        var result = await _service.AcceptAsync("invalid-token", TestContext.Current.CancellationToken);

        Assert.False(result.Accepted);
        Assert.False(result.EmailConflict);
    }

    [Fact]
    public async Task AcceptAsync_ReturnsNotAccepted_WhenTokenExpired()
    {
        // Use a TimeProvider that returns a time well in the future so the invitation is expired
        var pastTime = DateTimeOffset.UtcNow.AddDays(-8);
        var invitation = Invitation.Create(TenantId, "exp@corp.com", "admin", TimeSpan.FromDays(7), TimeProvider.System);
        // We can't set ExpiresAtUtc directly, so seed with a 0-second validity using a fake time
        // Instead, create with TimeProvider.System then accept with a future time by seeding manually
        // Use negative validity workaround: create expired by seeding DB directly
        typeof(Invitation)
            .GetProperty(nameof(Invitation.ExpiresAtUtc))!
            .SetValue(invitation, DateTime.UtcNow.AddDays(-1));
        _db.Invitations.Add(invitation);
        await _db.SaveChangesAsync();

        var result = await _service.AcceptAsync(invitation.Token, TestContext.Current.CancellationToken);

        Assert.False(result.Accepted);
    }

    [Fact]
    public async Task AcceptAsync_CreatesUser_WhenValid()
    {
        var invitation = Invitation.Create(TenantId, "valid@corp.com", "admin", TimeSpan.FromDays(7), TimeProvider.System);
        _db.Invitations.Add(invitation);
        await _db.SaveChangesAsync();

        var result = await _service.AcceptAsync(invitation.Token, TestContext.Current.CancellationToken);

        Assert.True(result.Accepted);
        Assert.Equal(1, _keycloak.CreateTenantUserCallCount);
    }

    [Fact]
    public async Task AcceptAsync_ReturnsEmailConflict_WhenKeycloakConflicts()
    {
        var invitation = Invitation.Create(TenantId, "conflict@corp.com", "admin", TimeSpan.FromDays(7), TimeProvider.System);
        _db.Invitations.Add(invitation);
        await _db.SaveChangesAsync();
        _keycloak.ExceptionToThrow = new HttpRequestException("Conflict", null, HttpStatusCode.Conflict);

        var result = await _service.AcceptAsync(invitation.Token, TestContext.Current.CancellationToken);

        Assert.False(result.Accepted);
        Assert.True(result.EmailConflict);
    }

    [Fact]
    public async Task RevokeAsync_ReturnsTrue_WhenPendingInvitation()
    {
        var invitation = Invitation.Create(TenantId, "revoke@corp.com", "admin", TimeSpan.FromDays(7), TimeProvider.System);
        _db.Invitations.Add(invitation);
        await _db.SaveChangesAsync();

        var result = await _service.RevokeAsync(TenantId, invitation.Id, TestContext.Current.CancellationToken);

        Assert.True(result);
    }

    [Fact]
    public async Task RevokeAsync_ReturnsFalse_WhenNotFound()
    {
        var result = await _service.RevokeAsync(TenantId, Guid.NewGuid(), TestContext.Current.CancellationToken);

        Assert.False(result);
    }

    [Fact]
    public async Task RevokeAsync_ReturnsFalse_WhenAlreadyAccepted()
    {
        var invitation = Invitation.Create(TenantId, "done@corp.com", "admin", TimeSpan.FromDays(7), TimeProvider.System);
        invitation.Accept(TimeProvider.System);
        _db.Invitations.Add(invitation);
        await _db.SaveChangesAsync();

        var result = await _service.RevokeAsync(TenantId, invitation.Id, TestContext.Current.CancellationToken);

        Assert.False(result);
    }
}
