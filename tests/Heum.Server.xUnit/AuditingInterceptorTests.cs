using System.Text.Json;
using Heum.Data;
using Heum.Data.Auditing;
using Heum.Data.Models;
using Heum.Server.xUnit.Fakes;
using Microsoft.EntityFrameworkCore;

namespace Heum.Server.xUnit;

public sealed class AuditingInterceptorTests : IDisposable
{
    private readonly FakeCurrentUserService _user = new() { UserId = "test-user" };
    private readonly HeumDbContext _db;

    public AuditingInterceptorTests()
    {
        var interceptor = new AuditingInterceptor(_user, TimeProvider.System);
        var options = new DbContextOptionsBuilder<HeumDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .AddInterceptors(interceptor)
            .Options;
        _db = new HeumDbContext(options);
    }

    public void Dispose() => _db.Dispose();

    // --- Insert ---

    [Fact]
    public async Task Insert_CreatesAuditTrail_WithInsertAction()
    {
        var tenant = Tenant.Register("Corp", "corp", TimeProvider.System);
        _db.Tenants.Add(tenant);
        await _db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var audit = await _db.AuditTrails.SingleAsync(cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal(AuditAction.Insert, audit.Action);
        Assert.Equal(nameof(Tenant), audit.EntityName);
        Assert.Equal(tenant.Id.ToString(), audit.PrimaryKey);
        Assert.Equal("test-user", audit.UserId);
    }

    [Fact]
    public async Task Insert_SetsNewValues_AndNoOldValues()
    {
        var tenant = Tenant.Register("Corp", "corp", TimeProvider.System);
        _db.Tenants.Add(tenant);
        await _db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var audit = await _db.AuditTrails.SingleAsync(cancellationToken: TestContext.Current.CancellationToken);
        Assert.NotNull(audit.NewValues);
        Assert.Null(audit.OldValues);

        var newValues = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(audit.NewValues);
        Assert.Equal("Corp", newValues!["Name"].GetString());
        Assert.Equal("corp", newValues["Slug"].GetString());
    }

    // --- Update ---

    [Fact]
    public async Task Update_CreatesAuditTrail_WithUpdateAction()
    {
        var tenant = Tenant.Register("Corp", "corp", TimeProvider.System);
        _db.Tenants.Add(tenant);
        await _db.SaveChangesAsync(TestContext.Current.CancellationToken);

        tenant.Rename("Corp Renamed", TimeProvider.System);
        await _db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var update = await _db.AuditTrails.SingleAsync(a => a.Action == AuditAction.Update, cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal(AuditAction.Update, update.Action);
        Assert.Equal(nameof(Tenant), update.EntityName);
        Assert.Equal(tenant.Id.ToString(), update.PrimaryKey);
    }

    [Fact]
    public async Task Update_RecordsBothOldAndNewValues_ForModifiedProperties()
    {
        var tenant = Tenant.Register("Corp", "corp", TimeProvider.System);
        _db.Tenants.Add(tenant);
        await _db.SaveChangesAsync(TestContext.Current.CancellationToken);

        tenant.Rename("Corp Renamed", TimeProvider.System);
        await _db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var update = await _db.AuditTrails.SingleAsync(a => a.Action == AuditAction.Update, cancellationToken: TestContext.Current.CancellationToken);
        Assert.NotNull(update.OldValues);
        Assert.NotNull(update.NewValues);

        var oldValues = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(update.OldValues);
        var newValues = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(update.NewValues);
        Assert.Equal("Corp", oldValues!["Name"].GetString());
        Assert.Equal("Corp Renamed", newValues!["Name"].GetString());
    }

    [Fact]
    public async Task Update_OnlyIncludesModifiedProperties_NotUnchangedOnes()
    {
        var tenant = Tenant.Register("Corp", "corp", TimeProvider.System);
        _db.Tenants.Add(tenant);
        await _db.SaveChangesAsync(TestContext.Current.CancellationToken);

        tenant.Rename("Corp Renamed", TimeProvider.System);
        await _db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var update = await _db.AuditTrails.SingleAsync(a => a.Action == AuditAction.Update, cancellationToken: TestContext.Current.CancellationToken);
        var newValues = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(update.NewValues!);
        Assert.False(newValues!.ContainsKey("Slug"), "Slug was not changed and should not appear in update values");
    }

    // --- Delete ---

    [Fact]
    public async Task Delete_CreatesAuditTrail_WithDeleteAction()
    {
        var tenant = Tenant.Register("Corp", "corp", TimeProvider.System);
        _db.Tenants.Add(tenant);
        await _db.SaveChangesAsync(TestContext.Current.CancellationToken);

        _db.Tenants.Remove(tenant);
        await _db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var delete = await _db.AuditTrails.SingleAsync(a => a.Action == AuditAction.Delete, cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal(AuditAction.Delete, delete.Action);
        Assert.Equal(nameof(Tenant), delete.EntityName);
    }

    [Fact]
    public async Task Delete_SetsOldValues_AndNoNewValues()
    {
        var tenant = Tenant.Register("Corp", "corp", TimeProvider.System);
        _db.Tenants.Add(tenant);
        await _db.SaveChangesAsync(TestContext.Current.CancellationToken);

        _db.Tenants.Remove(tenant);
        await _db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var delete = await _db.AuditTrails.SingleAsync(a => a.Action == AuditAction.Delete, cancellationToken: TestContext.Current.CancellationToken);
        Assert.NotNull(delete.OldValues);
        Assert.Null(delete.NewValues);

        var oldValues = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(delete.OldValues);
        Assert.Equal("Corp", oldValues!["Name"].GetString());
    }

    // --- Self-audit prevention ---

    [Fact]
    public async Task AuditTrailChanges_AreNotSelfAudited()
    {
        // Add an AuditTrail directly and verify it does not create another AuditTrail.
        var audit = new AuditTrail
        {
            EntityName = "SomeEntity",
            PrimaryKey = "1",
            Action = AuditAction.Insert,
            UserId = "system",
            TimestampUtc = DateTime.UtcNow,
        };
        _db.AuditTrails.Add(audit);
        await _db.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Only the one we added exists — no recursive audit was created.
        Assert.Equal(1, await _db.AuditTrails.CountAsync(cancellationToken: TestContext.Current.CancellationToken));
    }

    // --- UserId ---

    [Fact]
    public async Task UserId_IsRecordedFromCurrentUserService()
    {
        _user.UserId = "specific-user-123";
        var tenant = Tenant.Register("Corp", "corp", TimeProvider.System);
        _db.Tenants.Add(tenant);
        await _db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var audit = await _db.AuditTrails.SingleAsync(cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal("specific-user-123", audit.UserId);
    }
}
