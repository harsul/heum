namespace Heum.Data.Auditing;

/// <summary>
/// Abstraction that resolves the identity of the user responsible for the current unit of work.
/// Implemented outside the Data project (e.g. in the Web API layer) so that Heum.Data has no
/// dependency on ASP.NET Core / HTTP concepts.
/// </summary>
public interface ICurrentUserService
{
    /// <summary>
    /// The identifier of the user performing the current operation, or a sentinel value
    /// (e.g. "System") when there is no authenticated user (background jobs, migrations, etc.).
    /// </summary>
    string UserId { get; }
}
