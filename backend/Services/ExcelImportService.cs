using backend.Data;
using shared.Models;
using Microsoft.EntityFrameworkCore;
using ExcelDataReader;

namespace backend.Services;

/// <summary>
/// Service สำหรับนำเข้าข้อมูลจิกจากไฟล์ Excel — รองรับ Template ยืดหยุ่น, สร้าง ID อัตโนมัติ,
/// และจัดการ Part Mapping ให้อัตโนมัติ
/// </summary>
public class ExcelImportService
{
    private readonly AppDbContext _context;
    private readonly JigService _jigService;

    public ExcelImportService(AppDbContext context, JigService jigService)
    {
        _context = context;
        _jigService = jigService;
    }

    /// <summary>
    /// ประมวลผลไฟล์ Excel — ค้นหา Header อัตโนมัติ, สร้าง/อัปเดตจิก, และจัดการ Part Mapping
    /// </summary>
    public async Task<(int inserted, int updated, List<string> errors)> ProcessExcelFileAsync(IFormFile file)
    {
        int inserted = 0;
        int updated = 0;
        var errors = new List<string>();

        // ติดตามลำดับ ID ใน Batch เดียวกัน
        var nextSuffixMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var partMappingsToAdd = new HashSet<(string ToolNo, string PartNumber, string ToyNumber)>();

        using var stream = file.OpenReadStream();
        using var reader = ExcelReaderFactory.CreateReader(stream);

        bool isHeaderFound = false;
        var colMap = new Dictionary<string, List<int>>(StringComparer.OrdinalIgnoreCase);

        while (reader.Read())
        {
            if (!isHeaderFound)
            {
                // ค้นหา Header แบบยืดหยุ่นจากทุกคอลัมน์ เพื่อรองรับ Template Excel ที่หลากหลาย
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    var raw = reader.GetValue(i)?.ToString()?.Trim();
                    if (string.IsNullOrEmpty(raw)) continue;
                    
                    // มาตรฐานชื่อคอลัมน์โดยลบจุดและช่องว่าง (ตัวอย่าง: "Tool No." → "toolno")
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
                // ฟังก์ชันช่วย: ดึงค่าจาก Excel โดยจับคู่ชื่อคอลัมน์แบบ Fuzzy
                // ตรวจสอบชื่อหลักก่อน แล้ว Fallback ไปชื่อทางเลือก (ตัวอย่าง: "toynumber" หรือ "toyno")
                string GetVal(string key, int occurrence = 0) {
                    var std = key.Replace(".", "").Replace(" ", "").ToLower();
                    
                    if (colMap.TryGetValue(std, out var indices)) {
                        if (occurrence < indices.Count) return reader.GetValue(indices[occurrence])?.ToString()?.Trim() ?? "";
                    }
                    
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
                
                var rawStep = GetVal("Total Step Print");
                if (string.IsNullOrEmpty(rawStep)) rawStep = GetVal("Step Print");
                var stepPrint = ExtractStepNumber(rawStep);
                if (stepPrint == "-") stepPrint = "";

                var partType = GetVal("Part Type", 0); 
                var jigType = GetVal("JIG Type"); 
                if (string.IsNullOrEmpty(jigType)) jigType = GetVal("Part Type", 1); 
                
                var date = FormatExcelDate(GetVal("Date"));
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

                var tempJigForSmartCode = new Jig {
                    ToolNo = toolNo, StepPrint = stepPrint, PartType = partType,
                    Date = date, Feed = feed, Scan = scan, QtyPrint = qtyPrint,
                    HeightJig = heightJig, JigType = jigType, Process = process
                };
                var smartCode = _jigService.GenerateSmartCodeName(tempJigForSmartCode);
                
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
                    existing.SmartCodeName = smartCode;
                    _jigService.SanitizeJig(existing);
                    existing.UpdatedAt = DateTime.UtcNow;
                    updated++;
                }
                else
                {
                    if (!nextSuffixMap.ContainsKey(toolNo ?? ""))
                    {
                        nextSuffixMap[toolNo ?? ""] = await _jigService.GetMaxSuffixFromDb(toolNo ?? "");
                    }
                    nextSuffixMap[toolNo ?? ""]++;

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
                    _jigService.SanitizeJig(newJig);
                    inserted++;
                }
            }
            catch (Exception ex)
            {
                errors.Add($"Row error: {ex.Message}");
            }
        }

        await _context.SaveChangesAsync();

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

        return (inserted, updated, errors);
    }

    private static readonly Dictionary<string, string> _stepNameToNumber = new(StringComparer.OrdinalIgnoreCase)
    {
        ["L"] = "1", ["R"] = "2", ["Hood"] = "3", ["Roof"] = "4",
        ["Re.Hood"] = "5", ["Front"] = "6", ["Rear"] = "7", ["Under"] = "8",
        ["Re.L"] = "9", ["Re.R"] = "10",
        ["Read Hood"] = "5", ["Read"] = "7"
    };

    /// <summary>
    /// แปลงค่า Step Print จาก Excel (ตัวอย่าง: "L, R", "1-2") เป็นตัวเลขมาตรฐาน (ตัวอย่าง: "1-2")
    /// รองรับการแปลงชื่อเป็นตัวเลข เช่น "Hood" → "3"
    /// </summary>
    public string ExtractStepNumber(string? stepStr)
    {
        if (string.IsNullOrEmpty(stepStr)) return "-";
        
        var items = stepStr.Split(new[] { ',', '-' }, StringSplitOptions.RemoveEmptyEntries);
        var resultNums = new List<string>();
        
        foreach (var item in items)
        {
            var trimmed = item.Trim();
            
            if (trimmed.Contains(':')) 
            {
                var prefix = trimmed.Split(':')[0].Trim();
                if (int.TryParse(prefix, out _)) { resultNums.Add(prefix); continue; }
            }
            
            if (int.TryParse(trimmed, out _)) { resultNums.Add(trimmed); continue; }
            
            var lower = trimmed.ToLowerInvariant();
            if (lower.Contains("re.hood") || lower.Contains("read hood")) { resultNums.Add("5"); continue; }
            if (lower.Contains("re.l")) { resultNums.Add("9"); continue; }
            if (lower.Contains("re.r")) { resultNums.Add("10"); continue; }
            
            var tokens = trimmed.Split(new[] { ' ', '.', '_', '/' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var token in tokens)
            {
                if (_stepNameToNumber.TryGetValue(token, out var num))
                {
                    resultNums.Add(num);
                }
            }
        }
        
        var finalNums = resultNums.Where(n => !string.IsNullOrEmpty(n)).Distinct().ToList();
        return finalNums.Any() ? string.Join("-", finalNums) : "-";
    }

    /// <summary>
    /// แปลงรูปแบบวันที่จาก Excel เป็นรูปแบบ DD/MM/YYYY
    /// รองรับทั้งปี พ.ศ. (BE) และ ค.ศ. (CE)
    /// </summary>
    public string FormatExcelDate(string? dString)
    {
        if (string.IsNullOrWhiteSpace(dString) || dString == "-") return "";
        try {
            var d = dString.Split(' ')[0].Split('T')[0];
            d = d.Replace("-", "/");
            var parts = d.Split('/');
            if (parts.Length == 3) {
                if (!int.TryParse(parts[0], out int p1)) return dString;
                if (!int.TryParse(parts[1], out int p2)) return dString;
                if (!int.TryParse(parts[2], out int p3)) return dString;
                
                int day, month, year;
                if (p1 > 31) { year = p1; month = p2; day = p3; }
                else { day = p1; month = p2; year = p3; }
                
                if (year >= 2500) year -= 543;
                if (year < 100) year += 2000;
                
                return $"{day:D2}/{month:D2}/{year}";
            }
        } catch { }
        return dString ?? "";
    }
}
