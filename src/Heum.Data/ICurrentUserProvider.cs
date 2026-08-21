namespace Heum.Data;

public interface ICurrentUserProvider
{
    string? UserId { get; }
}
