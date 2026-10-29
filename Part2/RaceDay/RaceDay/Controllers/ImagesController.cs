using Azure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Data;
using RaceDay.Dtos;
using RaceDay.Models;
using RaceDay.Services;

namespace RaceDay.Controllers
{
    /// <summary>
    /// Image uploads. Files are validated here and stored in Azure Blob Storage;
    /// only the resulting public URL is saved in the database.
    /// </summary>
    [ApiController]
    [Route("api")]
    public class ImagesController : ControllerBase
    {
        private const long MaxFileBytes = 5 * 1024 * 1024; // 5 MB

        private static readonly Dictionary<string, string> AllowedTypes = new(StringComparer.OrdinalIgnoreCase)
        {
            ["image/jpeg"] = ".jpg",
            ["image/png"] = ".png",
            ["image/webp"] = ".webp"
        };

        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IBlobStorageService _blobs;

        public ImagesController(ApplicationDbContext db, UserManager<ApplicationUser> userManager,
            IBlobStorageService blobs)
        {
            _db = db;
            _userManager = userManager;
            _blobs = blobs;
        }

        /// <summary>
        /// Uploads (or replaces) the banner image of an event.
        /// </summary>
        /// <remarks>
        /// Organiser only, and only the Organiser who created the event. Send the image as
        /// multipart/form-data in a field called "file" (JPEG, PNG or WebP, up to 5 MB).
        /// </remarks>
        /// <param name="id">The event ID.</param>
        /// <param name="file">The image file.</param>
        /// <response code="200">The image was stored. Its public URL is returned.</response>
        /// <response code="400">No file, a file that is too large, or not a JPEG, PNG or WebP image.</response>
        /// <response code="401">The caller is not logged in.</response>
        /// <response code="403">The caller is not an Organiser, or does not own this event.</response>
        /// <response code="404">No event exists with that ID.</response>
        [HttpPost("events/{id:int}/banner")]
        [Authorize(Roles = "Organiser")]
        [Consumes("multipart/form-data")]
        [RequestSizeLimit(6 * 1024 * 1024)]
        [ProducesResponseType(typeof(ImageUploadResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<ImageUploadResponse>> UploadBanner(int id, IFormFile? file)
        {
            var ev = await _db.Events.FirstOrDefaultAsync(e => e.EventId == id);
            if (ev == null)
            {
                return NotFound();
            }

            if (ev.OrganiserId != _userManager.GetUserId(User))
            {
                return StatusCode(StatusCodes.Status403Forbidden);
            }

            var problem = await ValidateImageAsync(file);
            if (problem != null)
            {
                return BadRequest(new { message = problem });
            }

            try
            {
                var blobName = $"{id}/{Guid.NewGuid():N}{AllowedTypes[file!.ContentType]}";

                string url;
                await using (var stream = file.OpenReadStream())
                {
                    url = await _blobs.UploadAsync(_blobs.EventBannersContainer, blobName, stream, file.ContentType);
                }

                var oldUrl = ev.BannerUrl;
                ev.BannerUrl = url;
                await _db.SaveChangesAsync();

                if (!string.IsNullOrEmpty(oldUrl))
                {
                    await _blobs.DeleteAsync(oldUrl);
                }

                return Ok(new ImageUploadResponse { Url = url });
            }
            catch (BlobStorageNotConfiguredException)
            {
                return StatusCode(StatusCodes.Status503ServiceUnavailable,
                    new { message = "Image storage is not set up on the server yet." });
            }
            catch (RequestFailedException)
            {
                return StatusCode(StatusCodes.Status502BadGateway,
                    new { message = "Image storage is unavailable right now. Please try again." });
            }
        }

        // ------------------------------------------------------------ validation

        private static async Task<string?> ValidateImageAsync(IFormFile? file)
        {
            if (file == null || file.Length == 0)
            {
                return "Please choose an image to upload.";
            }

            if (file.Length > MaxFileBytes)
            {
                return "That image is too large. Please choose one under 5 MB.";
            }

            if (!AllowedTypes.ContainsKey(file.ContentType))
            {
                return "Only JPEG, PNG or WebP images are allowed.";
            }

            // Check the first bytes too, so a renamed non-image file can't be uploaded.
            var header = new byte[12];
            await using var stream = file.OpenReadStream();
            var read = await stream.ReadAtLeastAsync(header, header.Length, throwOnEndOfStream: false);
            if (read < header.Length || !MatchesSignature(header, file.ContentType))
            {
                return "That file doesn't look like a valid image.";
            }

            return null;
        }

        private static bool MatchesSignature(byte[] h, string contentType) =>
            contentType.ToLowerInvariant() switch
            {
                "image/jpeg" => h[0] == 0xFF && h[1] == 0xD8 && h[2] == 0xFF,
                "image/png" => h[0] == 0x89 && h[1] == 0x50 && h[2] == 0x4E && h[3] == 0x47,
                "image/webp" => h[0] == 0x52 && h[1] == 0x49 && h[2] == 0x46 && h[3] == 0x46 &&
                                h[8] == 0x57 && h[9] == 0x45 && h[10] == 0x42 && h[11] == 0x50,
                _ => false
            };
    }
}
