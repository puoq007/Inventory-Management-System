using System.Net.Http.Headers;
using backend.Services;
using Microsoft.AspNetCore.Mvc;

namespace backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UploadController : ControllerBase
{
    private readonly ExcelImportService _excelService;

    public UploadController(ExcelImportService excelService)
    {
        _excelService = excelService;
    }

    [HttpPost("jigspecs")]
    public async Task<IActionResult> UploadJigSpecs(IFormFile file)
    {
        if (file == null || file.Length == 0) return BadRequest("No file uploaded");
        using var stream = file.OpenReadStream();
        var result = await _excelService.ImportJigSpecsAsync(stream, file.FileName);
        return Ok(result);
    }

    [HttpPost("physicaljigs")]
    public async Task<IActionResult> UploadPhysicalJigs(IFormFile file)
    {
        if (file == null || file.Length == 0) return BadRequest("No file uploaded");
        using var stream = file.OpenReadStream();
        var result = await _excelService.ImportPhysicalJigsAsync(stream, file.FileName);
        return Ok(result);
    }
}
