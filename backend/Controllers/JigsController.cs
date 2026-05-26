using backend.Data;
using shared.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using backend.Services;

namespace backend.Controllers;

/// <summary>
/// Controller จัดการข้อมูลจิกในระบบ — CRUD, อัปโหลดรูปภาพ, จัดการ Part Mapping, และนำเข้า Excel
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class JigsController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly JigService _jigService;
    private readonly ExcelImportService _excelService;

    public JigsController(AppDbContext context, JigService jigService, ExcelImportService excelService)
    {
        _context = context;
        _jigService = jigService;
        _excelService = excelService;
    }

    /// <summary>ดึงรายการจิกทั้งหมดจากฐานข้อมูล</summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Jig>>> GetJigs()
    {
        return await _context.Jigs.ToListAsync();
    }

    /// <summary>ดึงรายการ Part Number ทั้งหมดในระบบ</summary>
    [HttpGet("parts")]
    public async Task<ActionResult<IEnumerable<PartMaster>>> GetAllParts()
    {
        return await _context.PartMasters.ToListAsync();
    }

    /// <summary>ดึง Part Number ที่เชื่อมกับ ToolNo ที่ระบุ</summary>
    /// <param name="toolNo">ToolNo ที่ต้องการค้นหา</param>
    [HttpGet("parts/{toolNo}")]
    public async Task<ActionResult<IEnumerable<PartMaster>>> GetPartsForTool(string toolNo)
    {
        var partNumbers = await _context.JigPartMappings
            .Where(m => m.ToolNo == toolNo)
            .Select(m => m.PartNumber)
            .ToListAsync();
            
        return await _context.PartMasters
            .Where(p => partNumbers.Contains(p.PartNumber))
            .ToListAsync();
    }

    /// <summary>อัปเดต Part Mapping ของ ToolNo ที่ระบุ — ลบของเดิมแล้วสร้างใหม่ทั้งหมดใน Transaction</summary>
    /// <param name="toolNo">ToolNo ที่ต้องการอัปเดต</param>
    /// <param name="parts">รายการ Part ใหม่ที่ต้องการเชื่อม</param>
    [HttpPost("parts/{toolNo}")]
    public async Task<IActionResult> UpdatePartsForTool(string toolNo, [FromBody] List<PartMaster> parts)
    {
        if (string.IsNullOrWhiteSpace(toolNo)) return BadRequest();
        
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            parts = parts.Where(p => !string.IsNullOrWhiteSpace(p.PartNumber))
                         .GroupBy(p => p.PartNumber)
                         .Select(g => g.First())
                         .ToList();

            foreach (var part in parts)
            {
                var existing = await _context.PartMasters.FindAsync(part.PartNumber);
                if (existing == null)
                {
                    _context.PartMasters.Add(new PartMaster { PartNumber = part.PartNumber });
                }
            }
            await _context.SaveChangesAsync();

            var oldMappings = await _context.JigPartMappings.Where(m => m.ToolNo == toolNo).ToListAsync();
            _context.JigPartMappings.RemoveRange(oldMappings);

            foreach (var part in parts)
            {
                _context.JigPartMappings.Add(new JigPartMapping { ToolNo = toolNo, PartNumber = part.PartNumber });
            }
            
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
            return Ok();
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            return StatusCode(500, ex.Message);
        }
    }

    /// <summary>ดึงจิกตามรหัสภายใน (Uid)</summary>
    /// <param name="uid">รหัส Uid ของจิก</param>
    [HttpGet("{uid}")]
    public async Task<ActionResult<Jig>> GetJig(string uid)
    {
        var jig = await _context.Jigs.FindAsync(uid);

        if (jig == null)
        {
            return NotFound();
        }

        return jig;
    }

    /// <summary>ลงทะเบียนจิกใหม่ — สร้าง ID อัตโนมัติถ้าไม่ระบุ และสร้าง SmartCodeName</summary>
    /// <param name="jig">ข้อมูลจิกที่ต้องการลงทะเบียน</param>
    [HttpPost]
    public async Task<ActionResult<Jig>> PostJig(Jig jig)
    {
        _jigService.SanitizeJig(jig);
        jig.CreatedAt = DateTime.UtcNow;
        jig.UpdatedAt = DateTime.UtcNow;

        if (string.IsNullOrWhiteSpace(jig.Id))
        {
            var prefix = _jigService.ExtractIdPrefix(jig);
            jig.Id = await _jigService.GenerateNextJigId(prefix);
        }
        
        if (string.IsNullOrWhiteSpace(jig.SmartCodeName))
        {
            jig.SmartCodeName = _jigService.GenerateSmartCodeName(jig);
        }

        if (await _context.Jigs.AnyAsync(j => j.Id == jig.Id))
        {
            return BadRequest("Jig ID already exists");
        }

        _context.Jigs.Add(jig);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetJig), new { uid = jig.Uid }, jig);
    }

    /// <summary>อัปเดตข้อมูลจิกที่มีอยู่ — สร้าง SmartCodeName ใหม่อัตโนมัติ</summary>
    /// <param name="uid">รหัส Uid ของจิก</param>
    /// <param name="jig">ข้อมูลจิกที่อัปเดตแล้ว</param>
    [HttpPut("{uid}")]
    public async Task<IActionResult> PutJig(string uid, Jig jig)
    {
        if (uid != jig.Uid)
        {
            return BadRequest();
        }

        if (await _context.Jigs.AnyAsync(j => j.Id == jig.Id && j.Uid != uid))
        {
            return BadRequest("New Jig ID already exists");
        }

        _jigService.SanitizeJig(jig);
        jig.UpdatedAt = DateTime.UtcNow;
        jig.SmartCodeName = _jigService.GenerateSmartCodeName(jig);

        _context.Entry(jig).State = EntityState.Modified;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!JigExists(uid))
            {
                return NotFound();
            }
            else
            {
                throw;
            }
        }

        return NoContent();
    }

    /// <summary>อัปโหลดรูปภาพจิก (JPEG/PNG/WebP, สูงสุด 5MB)</summary>
    /// <param name="uid">รหัส Uid ของจิก</param>
    /// <param name="file">ไฟล์รูปภาพ</param>
    [HttpPost("{uid}/image")]
    [RequestSizeLimit(5_000_000)]
    public async Task<IActionResult> UploadImage(string uid, IFormFile file)
    {
        if (file == null || file.Length == 0) return BadRequest("No file uploaded");
        
        var jig = await _context.Jigs.FindAsync(uid);
        if (jig == null) return NotFound();

        var allowedTypes = new[] { "image/jpeg", "image/png", "image/webp" };
        if (!allowedTypes.Contains(file.ContentType))
            return BadRequest("Only JPEG, PNG, and WebP images are allowed");

        var uploadsDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
        Directory.CreateDirectory(uploadsDir);

        if (!string.IsNullOrEmpty(jig.ImageUrl))
        {
            var oldPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", jig.ImageUrl.TrimStart('/'));
            if (System.IO.File.Exists(oldPath)) System.IO.File.Delete(oldPath);
        }

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (string.IsNullOrEmpty(ext)) ext = ".jpg";
        var fileName = $"{uid}{ext}";
        var filePath = Path.Combine(uploadsDir, fileName);

        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        jig.ImageUrl = $"/uploads/{fileName}";
        jig.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return Ok(new { imageUrl = jig.ImageUrl });
    }

    /// <summary>ลบรูปภาพที่แนบกับจิก</summary>
    /// <param name="uid">รหัส Uid ของจิก</param>
    [HttpDelete("{uid}/image")]
    public async Task<IActionResult> DeleteImage(string uid)
    {
        var jig = await _context.Jigs.FindAsync(uid);
        if (jig == null) return NotFound();

        if (!string.IsNullOrEmpty(jig.ImageUrl))
        {
            var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", jig.ImageUrl.TrimStart('/'));
            if (System.IO.File.Exists(filePath)) System.IO.File.Delete(filePath);
        }

        jig.ImageUrl = null;
        jig.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return NoContent();
    }

    /// <summary>ลบจิกออกจากระบบ</summary>
    /// <param name="uid">รหัส Uid ของจิกที่ต้องการลบ</param>
    [HttpDelete("{uid}")]
    public async Task<IActionResult> DeleteJig(string uid)
    {
        var jig = await _context.Jigs.FindAsync(uid);
        if (jig == null)
        {
            return NotFound();
        }

        _context.Jigs.Remove(jig);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    /// <summary>นำเข้าจิกจำนวนมากจากไฟล์ Excel (.xlsx/.xls)</summary>
    /// <param name="file">ไฟล์ Excel ที่มีข้อมูลจิก</param>
    [HttpPost("upload")]
    public async Task<IActionResult> UploadExcel(IFormFile file)
    {
        if (file == null || file.Length == 0) return BadRequest("No file uploaded");

        try
        {
            var result = await _excelService.ProcessExcelFileAsync(file);
            return Ok(new { result.inserted, result.updated, result.errors });
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Internal server error: {ex.Message}");
        }
    }

    private bool JigExists(string uid)
    {
        return _context.Jigs.Any(e => e.Uid == uid);
    }
}
