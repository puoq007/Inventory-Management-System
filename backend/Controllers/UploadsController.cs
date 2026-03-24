using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UploadsController : ControllerBase
{
    private readonly IWebHostEnvironment _env;

    public UploadsController(IWebHostEnvironment env)
    {
        _env = env;
    }

    [HttpPost("image")]
    [Authorize(Roles = "Admin,Engineer")]
    public async Task<IActionResult> UploadImage([FromForm] IFormFile file, [FromForm] string? specId)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest("No file uploaded.");
        }

        try
        {
            var extension = Path.GetExtension(file.FileName);
            var fileName = !string.IsNullOrWhiteSpace(specId) 
                ? $"{specId}{extension}" 
                : Guid.NewGuid().ToString() + extension;
            var uploadsFolder = Path.Combine(_env.WebRootPath, "images", "specs");

            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder);
            }

            var filePath = Path.Combine(uploadsFolder, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            // Return the URL path
            var urlPath = $"/images/specs/{fileName}";
            return Ok(new { url = urlPath });
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Internal server error: {ex.Message}");
        }
    }
}
