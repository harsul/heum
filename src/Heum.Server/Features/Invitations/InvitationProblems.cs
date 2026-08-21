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
}
