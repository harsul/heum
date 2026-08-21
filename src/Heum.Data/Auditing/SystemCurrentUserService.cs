namespace Heum.Data.Auditing;

public sealed class SystemCurrentUserService : ICurrentUserService
{
    public string UserId => "System";
}