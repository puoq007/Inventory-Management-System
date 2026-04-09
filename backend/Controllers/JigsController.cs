using backend.Data;
using shared.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ExcelDataReader;

namespace backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class JigsController : ControllerBase
{
    private readonly AppDbContext _context;

    public JigsController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Jig>>> GetJigs()
    {
        return await _context.Jigs.ToListAsync();
    }

    [HttpGet("parts")]
    public async Task<ActionResult<IEnumerable<PartMaster>>> GetAllParts()
    {
        return await _context.PartMasters.ToListAsync();
    }

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

    [HttpPost]
    public async Task<ActionResult<Jig>> PostJig(Jig jig)
    {
        SanitizeJig(jig);
        jig.CreatedAt = DateTime.UtcNow;
        jig.UpdatedAt = DateTime.UtcNow;

        // Auto-generate ID if empty (ToolNo-01 format)
        if (string.IsNullOrWhiteSpace(jig.Id))
        {
            jig.Id = await GenerateNextJigId(jig.ToolNo);
        }
        
        // Auto-generate SmartCodeName if empty
        if (string.IsNullOrWhiteSpace(jig.SmartCodeName))
        {
            var parts = new List<string>();
            if (!string.IsNullOrWhiteSpace(jig.ToolNo)) parts.Add(jig.ToolNo);
            if (!string.IsNullOrWhiteSpace(jig.StepPrint)) parts.Add(jig.StepPrint);
            if (!string.IsNullOrWhiteSpace(jig.PartType)) parts.Add(jig.PartType);
            if (!string.IsNullOrWhiteSpace(jig.Date)) parts.Add(jig.Date);
            if (!string.IsNullOrWhiteSpace(jig.Feed) && !string.IsNullOrWhiteSpace(jig.Scan)) 
                parts.Add($"{jig.Feed}/{jig.Scan}");
            else if (!string.IsNullOrWhiteSpace(jig.Feed)) parts.Add(jig.Feed);
            else if (!string.IsNullOrWhiteSpace(jig.Scan)) parts.Add(jig.Scan);
            if (!string.IsNullOrWhiteSpace(jig.QtyPrint)) parts.Add(jig.QtyPrint);
            if (!string.IsNullOrWhiteSpace(jig.HeightJig)) parts.Add(jig.HeightJig);
            if (!string.IsNullOrWhiteSpace(jig.JigType)) parts.Add(jig.JigType);
            if (!string.IsNullOrWhiteSpace(jig.Process)) parts.Add(jig.Process);
            jig.SmartCodeName = string.Join(" ", parts);
        }

        // Check if logical Id is already taken
        if (await _context.Jigs.AnyAsync(j => j.Id == jig.Id))
        {
            return BadRequest("Jig ID already exists");
        }

        _context.Jigs.Add(jig);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetJig), new { uid = jig.Uid }, jig);
    }

    [HttpPut("{uid}")]
    public async Task<IActionResult> PutJig(string uid, Jig jig)
    {
        if (uid != jig.Uid)
        {
            return BadRequest();
        }

        // Check if logical Id is taken by another record
        if (await _context.Jigs.AnyAsync(j => j.Id == jig.Id && j.Uid != uid))
        {
            return BadRequest("New Jig ID already exists");
        }

        SanitizeJig(jig);
        jig.UpdatedAt = DateTime.UtcNow;
        
        // Update SmartCodeName
        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(jig.ToolNo)) parts.Add(jig.ToolNo);
        if (!string.IsNullOrWhiteSpace(jig.StepPrint)) parts.Add(jig.StepPrint);
        if (!string.IsNullOrWhiteSpace(jig.PartType)) parts.Add(jig.PartType);
        if (!string.IsNullOrWhiteSpace(jig.Date)) parts.Add(jig.Date);
        if (!string.IsNullOrWhiteSpace(jig.Feed) && !string.IsNullOrWhiteSpace(jig.Scan)) 
            parts.Add($"{jig.Feed}/{jig.Scan}");
        else if (!string.IsNullOrWhiteSpace(jig.Feed)) parts.Add(jig.Feed);
        else if (!string.IsNullOrWhiteSpace(jig.Scan)) parts.Add(jig.Scan);
        if (!string.IsNullOrWhiteSpace(jig.QtyPrint)) parts.Add(jig.QtyPrint);
        if (!string.IsNullOrWhiteSpace(jig.HeightJig)) parts.Add(jig.HeightJig);
        if (!string.IsNullOrWhiteSpace(jig.JigType)) parts.Add(jig.JigType);
        if (!string.IsNullOrWhiteSpace(jig.Process)) parts.Add(jig.Process);
        jig.SmartCodeName = string.Join(" ", parts);

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

    [HttpPost("{uid}/image")]
    [RequestSizeLimit(5_000_000)] // 5MB limit
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

        // Delete old image if exists
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

    [HttpPost("upload")]
    public async Task<IActionResult> UploadExcel(IFormFile file)
    {
        if (file == null || file.Length == 0) return BadRequest("No file uploaded");

        int inserted = 0;
        int updated = 0;
        var errors = new List<string>();

        // For sequential ID tracking in same batch
        var nextSuffixMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var partMappingsToAdd = new HashSet<(string ToolNo, string PartNumber, string ToyNumber)>();


        try
        {
            using var stream = file.OpenReadStream();
            using var reader = ExcelReaderFactory.CreateReader(stream);

            bool isHeaderFound = false;
            var colMap = new Dictionary<string, List<int>>(StringComparer.OrdinalIgnoreCase);

            while (reader.Read())
            {
                if (!isHeaderFound)
                {
                    // Scan for headers
                    for (int i = 0; i < reader.FieldCount; i++)
                    {
                        var raw = reader.GetValue(i)?.ToString()?.Trim();
                        if (string.IsNullOrEmpty(raw)) continue;
                        
                        // Standardize: remove dots, extra spaces
                        var std = raw.Replace(".", "").Replace(" ", "").ToLower();
                        if (!colMap.ContainsKey(std)) colMap[std] = new List<int>();
                        colMap[std].Add(i);
                    }

                    if (colMap.ContainsKey("toynumber") || colMap.ContainsKey("toolno"))
                    {
                        isHeaderFound = true;
                    }
                    continue;
                }

                try
                {
                    // Helper to get value by fuzzy matching and handling duplicates
                    string GetVal(string key, int occurrence = 0) {
                        var std = key.Replace(".", "").Replace(" ", "").ToLower();
                        
                        // Try exact standardized match
                        if (colMap.TryGetValue(std, out var indices)) {
                            if (occurrence < indices.Count) return reader.GetValue(indices[occurrence])?.ToString()?.Trim() ?? "";
                        }
                        
                        // Try common abbreviations
                        var altKeys = new Dictionary<string, string> {
                            { "toynumber", "toyno" },
                            { "partnumber", "partno" },
                            { "toolno", "toolnumber" }
                        };
                        
                        foreach (var alt in altKeys) {
                            if (std == alt.Key && colMap.TryGetValue(alt.Value, out var altIndices)) {
                                if (occurrence < altIndices.Count) return reader.GetValue(altIndices[occurrence])?.ToString()?.Trim() ?? "";
                            }
                            if (std == alt.Value && colMap.TryGetValue(alt.Key, out var primaryIndices)) {
                                if (occurrence < primaryIndices.Count) return reader.GetValue(primaryIndices[occurrence])?.ToString()?.Trim() ?? "";
                            }
                        }
                        
                        return "";
                    }

                    var toolNo = GetVal("Tool No.");
                    var stepPrint = GetVal("Total Step Print");
                    if (string.IsNullOrEmpty(stepPrint)) stepPrint = GetVal("Step Print");

                    var partType = GetVal("Part Type", 0); // 1st Part Type
                    var jigType = GetVal("JIG Type"); 
                    if (string.IsNullOrEmpty(jigType)) jigType = GetVal("Part Type", 1); // 2nd Part Type fallback
                    
                    var date = GetVal("Date");
                    var feed = GetVal("Feed");
                    var scan = GetVal("Scan");
                    var qtyPrint = GetVal("Qty / Print");
                    if (string.IsNullOrEmpty(qtyPrint)) qtyPrint = GetVal("QtyPrint");
                    
                    var heightJig = GetVal("Height Jig");
                    var process = GetVal("Process");
                    var partNumber = GetVal("Part Number");
                    var rev = GetVal("Rev.");

                    if (string.IsNullOrEmpty(toolNo) && string.IsNullOrEmpty(partNumber)) continue;
                    
                    if (!string.IsNullOrWhiteSpace(toolNo))
                    {
                        if (!string.IsNullOrWhiteSpace(partNumber))
                            partMappingsToAdd.Add((toolNo.Trim(), partNumber.Trim(), ""));
                    }

                    // Re-generate SmartCodeName based on the same logic as UI
                    var smartCodeParts = new List<string>();
                    if (!string.IsNullOrWhiteSpace(toolNo)) smartCodeParts.Add(toolNo);
                    if (!string.IsNullOrWhiteSpace(stepPrint)) smartCodeParts.Add(stepPrint);
                    if (!string.IsNullOrWhiteSpace(partType)) smartCodeParts.Add(partType);
                    if (!string.IsNullOrWhiteSpace(date)) smartCodeParts.Add(date);
                    if (!string.IsNullOrWhiteSpace(feed) && !string.IsNullOrWhiteSpace(scan)) smartCodeParts.Add($"{feed}/{scan}");
                    else if (!string.IsNullOrWhiteSpace(feed)) smartCodeParts.Add(feed);
                    else if (!string.IsNullOrWhiteSpace(scan)) smartCodeParts.Add(scan);
                    if (!string.IsNullOrWhiteSpace(qtyPrint)) smartCodeParts.Add(qtyPrint);
                    if (!string.IsNullOrWhiteSpace(heightJig)) smartCodeParts.Add(heightJig);
                    if (!string.IsNullOrWhiteSpace(jigType)) smartCodeParts.Add(jigType);
                    if (!string.IsNullOrWhiteSpace(process)) smartCodeParts.Add(process);
                    
                    var smartCode = string.Join(" ", smartCodeParts);
                    
                    var existing = await _context.Jigs.FirstOrDefaultAsync(j => j.SmartCodeName == smartCode && !string.IsNullOrEmpty(smartCode));
                    
                    if (existing != null)
                    {
                        existing.ToolNo = toolNo;
                        existing.StepPrint = stepPrint;
                        existing.PartType = partType;
                        existing.Date = date;
                        existing.Feed = feed;
                        existing.Scan = scan;
                        existing.QtyPrint = qtyPrint;
                        existing.HeightJig = heightJig;
                        existing.JigType = jigType;
                        existing.Process = process;
                        existing.PartNumber = partNumber;
                        existing.Rev = rev;
                        SanitizeJig(existing);
                        existing.UpdatedAt = DateTime.UtcNow;
                        updated++;
                    }
                    else
                    {
                        // Sequential ID generation
                        if (!nextSuffixMap.ContainsKey(toolNo))
                        {
                            nextSuffixMap[toolNo] = await GetMaxSuffixFromDb(toolNo);
                        }
                        nextSuffixMap[toolNo]++;

                        var newId = string.IsNullOrWhiteSpace(toolNo) 
                            ? "JIG-" + Guid.NewGuid().ToString().Substring(0, 6).ToUpper() 
                            : $"{toolNo}-{nextSuffixMap[toolNo]:D2}";

                        var newJig = new Jig
                        {
                            Id = newId,
                            SmartCodeName = smartCode,
                            ToolNo = toolNo,
                            StepPrint = stepPrint,
                            PartType = partType,
                            Date = date,
                            Feed = feed,
                            Scan = scan,
                            QtyPrint = qtyPrint,
                            HeightJig = heightJig,
                            JigType = jigType,
                            Process = process,
                            PartNumber = partNumber,
                            Rev = rev,
                            Status = "Available",
                            Condition = "Good",
                            CreatedAt = DateTime.UtcNow,
                            UpdatedAt = DateTime.UtcNow
                        };
                        _context.Jigs.Add(newJig);
                        SanitizeJig(newJig);
                        inserted++;
                    }
                }
                catch (Exception ex)
                {
                    errors.Add($"Row error: {ex.Message}");
                }
            }

            await _context.SaveChangesAsync();

            // Process Part Mappings
            foreach (var map in partMappingsToAdd)
            {
                var partMaster = await _context.PartMasters.FindAsync(map.PartNumber);
                if (partMaster == null) 
                {
                    _context.PartMasters.Add(new PartMaster { PartNumber = map.PartNumber });
                    await _context.SaveChangesAsync();
                }

                if (!await _context.JigPartMappings.AnyAsync(m => m.ToolNo == map.ToolNo && m.PartNumber == map.PartNumber))
                {
                    _context.JigPartMappings.Add(new JigPartMapping { ToolNo = map.ToolNo, PartNumber = map.PartNumber });
                    await _context.SaveChangesAsync();
                }
            }



            return Ok(new { inserted, updated, errors });
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Internal server error: {ex.Message}");
        }
    }

    private async Task<string> GenerateNextJigId(string? toolNo)
    {
        if (string.IsNullOrWhiteSpace(toolNo)) return "JIG-" + Guid.NewGuid().ToString().Substring(0, 6).ToUpper();
        int nextNum = (await GetMaxSuffixFromDb(toolNo)) + 1;
        return $"{toolNo}-{nextNum:D2}";
    }

    private async Task<int> GetMaxSuffixFromDb(string toolNo)
    {
        var prefix = toolNo + "-";
        var existingIds = await _context.Jigs
            .Where(j => j.Id.StartsWith(prefix))
            .Select(j => j.Id)
            .ToListAsync();

        int max = 0;
        foreach (var id in existingIds)
        {
            var suffix = id.Substring(prefix.Length);
            if (int.TryParse(suffix, out int num))
            {
                if (num > max) max = num;
            }
        }
        return max;
    }

    private void SanitizeJig(Jig jig)
    {
        if (jig == null) return;
        
        // Fields where NO spaces should exist (Identifiers)
        jig.Id = CleanAllSpaces(jig.Id) ?? "";
        jig.ToolNo = CleanAllSpaces(jig.ToolNo);
        jig.PartNumber = CleanAllSpaces(jig.PartNumber);
        jig.LocatorId = CleanAllSpaces(jig.LocatorId);
        jig.Rev = CleanAllSpaces(jig.Rev);
        jig.Date = CleanAllSpaces(jig.Date);
        jig.Feed = CleanAllSpaces(jig.Feed);
        jig.Scan = CleanAllSpaces(jig.Scan);
        jig.QtyPrint = CleanAllSpaces(jig.QtyPrint);
        jig.HeightJig = CleanAllSpaces(jig.HeightJig);

        // Fields where spaces might exist but should be normalized (Descriptions)
        jig.PartType = CleanAllSpaces(jig.PartType); // Usually one word like BODY
        jig.StepPrint = NormalizeSpaces(jig.StepPrint); // e.g. "Front - Rear"
        jig.JigType = NormalizeSpaces(jig.JigType);
        jig.Process = NormalizeSpaces(jig.Process);
        jig.Status = CleanAllSpaces(jig.Status) ?? "Available";
        jig.Condition = CleanAllSpaces(jig.Condition) ?? "Good";
        
        jig.SmartCodeName = NormalizeSpaces(jig.SmartCodeName);
    }

    private string? CleanAllSpaces(string? val)
    {
        if (string.IsNullOrWhiteSpace(val)) return val?.Trim();
        return new string(val.Where(c => !char.IsWhiteSpace(c)).ToArray());
    }

    private string? NormalizeSpaces(string? val)
    {
        if (string.IsNullOrWhiteSpace(val)) return val?.Trim();
        // Replace multiple spaces with one
        return System.Text.RegularExpressions.Regex.Replace(val.Trim(), @"\s+", " ");
    }

    private bool JigExists(string uid)
    {
        return _context.Jigs.Any(e => e.Uid == uid);
    }
}
