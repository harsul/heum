using Heum.Data.Auditing;

namespace Heum.Server.xUnit.Fakes;

public sealed class FakeCurrentUserService : ICurrentUserService
{
    public string UserId { get; set; } = "test-user";
}
