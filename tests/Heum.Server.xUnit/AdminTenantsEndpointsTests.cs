using Heum.Data;
using Heum.Data.Models;
using Heum.Server.Features.Admin.Tenants;
using Heum.Server.Features.Admin.Tenants.Models;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace Heum.Server.xUnit;

public class AdminTenantsEndpointsTests
{
    private static HeumDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<HeumDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new HeumDbContext(options);
    }

    [Fact]
    public async Task ListTenantsAsync_ReturnsAllTenantsOrderedByName()
    {
        await using var db = CreateDbContext();
        db.Tenants.AddRange(
            new Tenant { Id = Guid.NewGuid(), Name = "Zeta Co", Slug = "zeta" },
            new Tenant { Id = Guid.NewGuid(), Name = "Acme Co", Slug = "acme" });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await AdminTenantsEndpoints.ListTenantsAsync(db, CancellationToken.None);

        Assert.Equal(["Acme Co", "Zeta Co"], result.Value!.Select(t => t.Name));
    }

    [Fact]
    public async Task GetTenantAsync_ReturnsNotFound_WhenTenantDoesNotExist()
    {
        await using var db = CreateDbContext();

        var result = await AdminTenantsEndpoints.GetTenantAsync(Guid.NewGuid(), db, CancellationToken.None);

        Assert.IsType<NotFound>(result.Result);
    }

    [Fact]
    public async Task UpdateTenantAsync_UpdatesNameAndIsActive()
    {
        await using var db = CreateDbContext();
        var tenant = new Tenant { Id = Guid.NewGuid(), Name = "Old Name", Slug = "old", IsActive = true };
        db.Tenants.Add(tenant);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await AdminTenantsEndpoints.UpdateTenantAsync(
            tenant.Id,
            new UpdateTenantRequest { Name = "New Name", IsActive = false },
            db,
            CancellationToken.None);

        var ok = Assert.IsType<Ok<TenantResponse>>(result.Result);
        Assert.Equal("New Name", ok.Value!.Name);
        Assert.False(ok.Value.IsActive);
        Assert.NotNull(ok.Value.UpdatedAtUtc);
    }

    [Fact]
    public async Task DeactivateTenantAsync_SetsIsActiveFalse()
    {
        await using var db = CreateDbContext();
        var tenant = new Tenant { Id = Guid.NewGuid(), Name = "Acme", Slug = "acme", IsActive = true };
        db.Tenants.Add(tenant);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await AdminTenantsEndpoints.DeactivateTenantAsync(tenant.Id, db, CancellationToken.None);

        var ok = Assert.IsType<Ok<TenantResponse>>(result.Result);
        Assert.False(ok.Value!.IsActive);
    }

    [Fact]
    public async Task ReactivateTenantAsync_SetsIsActiveTrue()
    {
        await using var db = CreateDbContext();
        var tenant = new Tenant { Id = Guid.NewGuid(), Name = "Acme", Slug = "acme", IsActive = false };
        db.Tenants.Add(tenant);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await AdminTenantsEndpoints.ReactivateTenantAsync(tenant.Id, db, CancellationToken.None);

        var ok = Assert.IsType<Ok<TenantResponse>>(result.Result);
        Assert.True(ok.Value!.IsActive);
    }
}
