using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Filters;
using RaceDay.Web.Models;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

/// <summary>View and update your own details and upload a profile picture. Any logged-in user.</summary>
[RoleAuthorize]
[Route("profile")]
public class ProfileController : RaceDayControllerBase
{
    private readonly IApiClient _api;

    public ProfileController(IApiClient api)
    {
        _api = api;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        var me = await _api.GetAsync<UserProfileDto>("api/users/me");

        var redirect = HandleAuthFailure(me);
        if (redirect != null) return redirect;

        if (!me.Success || me.Data is null)
        {
            TempData["Error"] = me.Error ?? "We couldn't load your profile. Please try again.";
            return RedirectToAction("Index", "Home");
        }

        // Keep the navigation bar's picture in step with what the API has.
        HttpContext.Session.SetProfilePictureUrl(me.Data.ProfilePictureUrl);

        return View(new ProfileViewModel
        {
            FullName = me.Data.FullName,
            Email = me.Data.Email,
            ProfilePictureUrl = me.Data.ProfilePictureUrl,
            Role = me.Data.Role,
            CreatedAt = me.Data.CreatedAt
        });
    }

    [HttpPost("details")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateDetails(ProfileViewModel model)
    {
        if (ModelState.IsValid)
        {
            var result = await _api.PutAsync<UserProfileDto>("api/users/me", new
            {
                fullName = model.FullName.Trim(),
                email = model.Email.Trim()
            });

            var redirect = HandleAuthFailure(result);
            if (redirect != null) return redirect;

            if (result.Success)
            {
                HttpContext.Session.SetString(SessionKeys.FullName, result.Data?.FullName ?? model.FullName.Trim());
                TempData["Success"] = "Your details have been updated.";
                return RedirectToAction(nameof(Index));
            }

            // For example: another account already uses that email.
            ModelState.AddModelError(string.Empty,
                result.Error ?? "We couldn't save your changes. Please try again.");
        }

        // Show the page again with what was typed, plus the current picture and account info.
        var me = await _api.GetAsync<UserProfileDto>("api/users/me");
        var failure = HandleAuthFailure(me);
        if (failure != null) return failure;

        if (me.Success && me.Data is not null)
        {
            model.ProfilePictureUrl = me.Data.ProfilePictureUrl;
            model.Role = me.Data.Role;
            model.CreatedAt = me.Data.CreatedAt;
        }
        return View("Index", model);
    }

    [HttpPost("picture")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UploadPicture(IFormFile? pictureFile)
    {
        if (pictureFile is null || pictureFile.Length == 0)
        {
            TempData["Error"] = "Please choose a picture to upload.";
            return RedirectToAction(nameof(Index));
        }

        // The file goes to the API, which validates it and stores it in Azure Blob Storage.
        var result = await _api.UploadAsync<ImageUploadDto>("api/users/me/picture", pictureFile);

        var redirect = HandleAuthFailure(result);
        if (redirect != null) return redirect;

        if (!result.Success)
        {
            TempData["Error"] = result.Error ?? "We couldn't upload that picture. Please try again.";
            return RedirectToAction(nameof(Index));
        }

        HttpContext.Session.SetProfilePictureUrl(result.Data?.Url);
        TempData["Success"] = "Your profile picture has been updated.";
        return RedirectToAction(nameof(Index));
    }
}
