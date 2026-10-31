using Microsoft.AspNetCore.Http;

namespace RaceDay.Web.Services;

/// <summary>Keys used in the MVC session. Nothing here is stored in a database.</summary>
public static class SessionKeys
{
    public const string UserId = "UserId";
    public const string Role = "Role";
    public const string FullName = "FullName";
    public const string ProfilePictureUrl = "ProfilePictureUrl";

    // The cookies issued by the Part 2 API (login cookie + API session cookie).
    // The MVC app forwards them on every API call so the API knows who the user is.
    public const string ApiCookies = "ApiCookies";
}

public static class Roles
{
    public const string Organiser = "Organiser";
    public const string Participant = "Participant";
}

public static class UserSessionExtensions
{
    public static void SignIn(this ISession session, string userId, string role, string fullName)
    {
        session.SetString(SessionKeys.UserId, userId);
        session.SetString(SessionKeys.Role, role);
        session.SetString(SessionKeys.FullName, fullName);
    }

    // Clears everything, including the stored API cookies.
    public static void SignOut(this ISession session) => session.Clear();

    public static bool IsAuthenticated(this ISession session) =>
        !string.IsNullOrEmpty(session.GetString(SessionKeys.UserId));

    public static string? GetUserId(this ISession session) => session.GetString(SessionKeys.UserId);

    public static string? GetRole(this ISession session) => session.GetString(SessionKeys.Role);

    public static string? GetFullName(this ISession session) => session.GetString(SessionKeys.FullName);

    public static string? GetProfilePictureUrl(this ISession session) =>
        session.GetString(SessionKeys.ProfilePictureUrl);

    /// <summary>Remembers the profile picture URL so the navigation bar can show it. Null or empty removes it.</summary>
    public static void SetProfilePictureUrl(this ISession session, string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
            session.Remove(SessionKeys.ProfilePictureUrl);
        else
            session.SetString(SessionKeys.ProfilePictureUrl, url);
    }

    public static bool IsOrganiser(this ISession session) =>
        string.Equals(session.GetRole(), Roles.Organiser, StringComparison.OrdinalIgnoreCase);

    public static bool IsParticipant(this ISession session) =>
        string.Equals(session.GetRole(), Roles.Participant, StringComparison.OrdinalIgnoreCase);
}
