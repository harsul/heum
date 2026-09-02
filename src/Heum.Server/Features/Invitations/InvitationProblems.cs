using Microsoft.AspNetCore.Mvc;

namespace Heum.Server.Features.Invitations;

internal static class InvitationProblems
{
    public static ProblemDetails DuplicatePending(string email) => new()
    {
        Title = "Invitation already pending",
        Detail = $"An active invitation already exists for '{email}'.",
        Status = StatusCodes.Status409Conflict,
    };

    public static ProblemDetails InvalidToken() => new()
    {
        Title = "Invalid invitation",
        Detail = "The invitation token is invalid, expired, or has already been used.",
        Status = StatusCodes.Status400BadRequest,
    };

    public static ProblemDetails UserLimitReached() => new()
    {
        Title = "User limit reached",
        Detail = "Your plan's maximum user limit has been reached. Upgrade your plan to invite more users.",
        Status = StatusCodes.Status403Forbidden,
    };
}
