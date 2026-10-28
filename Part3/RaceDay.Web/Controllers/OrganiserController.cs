using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Filters;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

[RoleAuthorize(Roles.Organiser)]
public class OrganiserController : Controller
{
    public IActionResult Dashboard() => View();
}
