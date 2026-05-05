using backend.Data;
using shared.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using ExcelDataReader;

namespace backend.Controllers;

/// <summary>
/// Controller จัดการตำแหน่งจัดเก็บจิก — CRUD, เปลี่ยนชื่อ, และนำเข้า Excel
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class LocatorsController : ControllerBase
{
    private readonly AppDbContext _context;

    public LocatorsController(AppDbContext context)
    {
        _context = context;
    }

    /// <summary>ดึงตำแหน่งทั้งหมด</summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Locator>>> GetLocators()
    {
        return await _context.Locators.ToListAsync();
    }

    /// <summary>ดึงตำแหน่งตามรหัส</summary>
    /// <param name="id">รหัสตำแหน่ง</param>
    [HttpGet("{id}")]
    public async Task<ActionResult<Locator>> GetLocator(string id)
    {
        var locator = await _context.Locators.FindAsync(id);
        if (locator == null) return NotFound();
        return locator;
    }

    /// <summary>สร้างตำแหน่งใหม่ (สิทธิ์ Admin/ProdLead)</summary>
    /// <param name="locator">ข้อมูลตำแหน่ง</param>
    [HttpPost]
    [Authorize(Roles = "Admin,ProdLead")]
    public async Task<ActionResult<Locator>> PostLocator(Locator locator)
    {
        if (string.IsNullOrEmpty(locator.Id))
            locator.Id = Guid.NewGuid().ToString();

        _context.Locators.Add(locator);
        await _context.SaveChangesAsync();
        return CreatedAtAction(nameof(GetLocator), new { id = locator.Id }, locator);
    }

    /// <summary>อัปเดตตำแหน่ง</summary>
    /// <param name="id">รหัสตำแหน่ง</param>
    /// <param name="locator">ข้อมูลที่อัปเดต</param>
    [HttpPut("{id}")]
    [Authorize(Roles = "Admin,ProdLead")]
    public async Task<IActionResult> PutLocator(string id, Locator locator)
    {
        if (id != locator.Id) return BadRequest();
        _context.Entry(locator).State = EntityState.Modified;
        await _context.SaveChangesAsync();
        return NoContent();
    }

    /// <summary>เปลี่ยนชื่อตำแหน่ง — สร้าง record ใหม่, ย้ายข้อมูลจิกที่อ้างอิง, แล้วลบ record เดิม</summary>
    [HttpPost("{oldId}/rename")]
    [Authorize(Roles = "Admin,ProdLead")]
    public async Task<IActionResult> RenameLocator(string oldId, [FromBody] Locator updatedLocator)
    {
        var existing = await _context.Locators.FindAsync(oldId);
        if (existing == null) return NotFound($"Locator '{oldId}' not found.");

        var newId = updatedLocator.Id?.Trim();
        if (string.IsNullOrEmpty(newId))
            return BadRequest("New ID cannot be empty.");

        if (newId != oldId)
        {
            var conflict = await _context.Locators.FindAsync(newId);
            if (conflict != null)
                return BadRequest($"Locator ID '{newId}' already exists.");
        }

        using var tx = await _context.Database.BeginTransactionAsync();
        try
        {
            if (newId != oldId)
            {
                // 1. สร้างตำแหน่งใหม่ด้วย ID ใหม่ + ข้อมูลที่อัปเดต
                var newLocator = new Locator
                {
                    Id       = newId,
                    Site     = updatedLocator.Site,
                    Cabinet  = updatedLocator.Cabinet,
                    Shelf    = updatedLocator.Shelf,
                    Type     = updatedLocator.Type
                };

                _context.Locators.Add(newLocator);
                await _context.SaveChangesAsync();

                // 2. ลบตำแหน่งเดิม
                _context.Locators.Remove(existing);
                await _context.SaveChangesAsync();

                await tx.CommitAsync();
                return Ok(newLocator);
            }
            else
            {
                // ID ไม่เปลี่ยน — อัปเดตเฉพาะข้อมูลอื่น
                existing.Site     = updatedLocator.Site;
                existing.Cabinet  = updatedLocator.Cabinet;
                existing.Shelf    = updatedLocator.Shelf;
                existing.Type     = updatedLocator.Type;
                await _context.SaveChangesAsync();
                await tx.CommitAsync();
                return Ok(existing);
            }
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync();
            return StatusCode(500, $"Rename failed: {ex.Message}");
        }
    }

    /// <summary>ลบตำแหน่ง</summary>
    /// <param name="id">รหัสตำแหน่งที่ต้องการลบ</param>
    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin,ProdLead")]
    public async Task<IActionResult> DeleteLocator(string id)
    {
        var locator = await _context.Locators.FindAsync(id);
        if (locator == null) return NotFound();
        _context.Locators.Remove(locator);
        await _context.SaveChangesAsync();
        return NoContent();
    }

    /// <summary>นำเข้าตำแหน่งจำนวนมากจาก Excel (.xlsx/.xls)</summary>
    /// <param name="file">ไฟล์ Excel ที่มีข้อมูลตำแหน่ง</param>
    [HttpPost("upload")]
    [Authorize(Roles = "Admin,ProdLead")]
    [RequestSizeLimit(10_000_000)]
    public async Task<IActionResult> UploadExcel(IFormFile file)
    {
        if (file == null || file.Length == 0) return BadRequest("No file uploaded");

        int inserted = 0;
        int updated = 0;
        var errors = new List<string>();

        try
        {
            System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
            using var stream = file.OpenReadStream();
            using var reader = ExcelDataReader.ExcelReaderFactory.CreateReader(stream);

            bool headerFound = false;
            int colSite = -1, colCabinet = -1, colShelf = -1, colType = -1;

            while (reader.Read())
            {
                if (!headerFound)
                {
                    for (int i = 0; i < reader.FieldCount; i++)
                    {
                        var h = reader.GetValue(i)?.ToString()?.Trim().ToLower() ?? "";
                        if (h.Contains("หมู่") || h.Contains("site") || h.Contains("พื้นที่")) colSite = i;
                        else if (h.Contains("ตู้") || h.Contains("cabinet")) colCabinet = i;
                        else if (h.Contains("ชั้น") || h.Contains("shelf")) colShelf = i;
                        else if (h.Contains("โซน") || h.Contains("zone") || h.Contains("type")) colType = i;
                    }
                    if (colSite >= 0 && colCabinet >= 0 && colShelf >= 0)
                        headerFound = true;
                    continue;
                }

                try
                {
                    var site = reader.GetValue(colSite)?.ToString()?.Trim() ?? "";
                    var cabinet = reader.GetValue(colCabinet)?.ToString()?.Trim() ?? "";
                    var shelf = reader.GetValue(colShelf)?.ToString()?.Trim() ?? "";
                    var type = colType >= 0 ? reader.GetValue(colType)?.ToString()?.Trim() ?? "Store" : "Store";

                    if (string.IsNullOrEmpty(site) || string.IsNullOrEmpty(cabinet) || string.IsNullOrEmpty(shelf))
                        continue;

                    // แปลงชื่อโซนภาษาไทยเป็นอังกฤษ
                    type = type.ToLower() switch {
                        "คลัง" or "store" or "storage" => "Store",
                        "ผลิต" or "production" => "Production",
                        "ล้าง" or "cleaning" => "Cleaning",
                        _ => "Store"
                    };

                    var loc = new Locator { Site = site, Cabinet = cabinet, Shelf = shelf, Type = type };
                    loc.Id = loc.GetGeneratedId();

                    var existing = await _context.Locators.FindAsync(loc.Id);
                    if (existing != null)
                    {
                        existing.Site = loc.Site;
                        existing.Cabinet = loc.Cabinet;
                        existing.Shelf = loc.Shelf;
                        existing.Type = loc.Type;
                        updated++;
                    }
                    else
                    {
                        _context.Locators.Add(loc);
                        inserted++;
                    }
                }
                catch (Exception ex)
                {
                    errors.Add($"Row error: {ex.Message}");
                }
            }

            await _context.SaveChangesAsync();
            return Ok(new { inserted, updated, errors });
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Import failed: {ex.Message}");
        }
    }
}
