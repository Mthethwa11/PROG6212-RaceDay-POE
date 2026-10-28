using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using RaceDay.Web.Services;

namespace RaceDay.Web.Filters;

/// <summary>
/// Put on a controller or action to protect it.
///   [RoleAuthorize]                      any logged-in user
///   [RoleAuthorize(Roles.Organiser)]     Organisers only
/// Not logged in  -> redirected to the login page.
/// Wrong role     -> redirected to the access denied page.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class RoleAuthorizeAttribute : ActionFilterAttribute
{
    private readonly string[] _roles;

    public RoleAuthorizeAttribute(params string[] roles)
    {
        _roles = roles;
    }

    public override void OnActionExecuting(ActionExecutingContext context)
    {
        var session = context.HttpContext.Session;

        if (!session.IsAuthenticated())
        {
            var request = context.HttpContext.Request;
            string? returnUrl = HttpMethods.IsGet(request.Method)
                ? request.Path + request.QueryString
                : null;

            context.Result = new RedirectToActionResult("Login", "Account", new { returnUrl });
            return;
        }

        if (_roles.Length > 0 &&
            !_roles.Contains(session.GetRole() ?? string.Empty, StringComparer.OrdinalIgnoreCase))
        {
            context.Result = new RedirectToActionResult("AccessDenied", "Account", null);
        }
    }
}
