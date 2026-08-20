namespace Heum.Server.xIntegration.Infrastructure;

public sealed class ClientScope
{
    private ClientScope() { }

    internal string? Roles { get; init; }
    internal Guid? TenantId { get; init; }
    internal string Subject { get; init; } = "test-user";

    public static ClientScope Anonymous { get; } = new();
    public static ClientScope SystemAdmin { get; } = new() { Roles = "SystemAdmin", Subject = "sys-admin-1" };

    public static ClientScope TenantAdmin(Guid tenantId) =>
        new() { Roles = "Admin,User", TenantId = tenantId, Subject = "tenant-admin-1" };

    public static ClientScope Authenticated(string roles, Guid? tenantId = null, string subject = "test-user") =>
        new() { Roles = roles, TenantId = tenantId, Subject = subject };
}
