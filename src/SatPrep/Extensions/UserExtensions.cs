using System.Security.Claims;

namespace SatPrep.Extensions;

/// <summary>Helpers for reading the signed-in user from the auth cookie.</summary>
public static class UserExtensions
{
    /// <summary>Returns the database id of the signed-in user (0 if anonymous).</summary>
    /// <param name="user">The current ClaimsPrincipal (PageModel.User).</param>
    public static int UserId(this ClaimsPrincipal user)
        => int.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0;
}
