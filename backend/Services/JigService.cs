using backend.Data;
using Microsoft.EntityFrameworkCore;
using shared.Models;
using System.Text.RegularExpressions;

namespace backend.Services;

/// <summary>
/// Service จัดการ Business Logic ของจิก — สร้าง ID อัตโนมัติ, ทำความสะอาดข้อมูล, สร้าง SmartCodeName
/// </summary>
public class JigService
{
    private readonly AppDbContext _context;

    public JigService(AppDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// สร้างรหัสจิกถัดไปจาก Prefix (ค่าเริ่มต้นคือ Tool Prefix เช่น JBM10)
    /// </summary>
    public async Task<string> GenerateNextJigId(string? prefix)
    {
        if (string.IsNullOrWhiteSpace(prefix)) 
            prefix = "TOOL";
            
        int nextNum = (await GetMaxSuffixFromDb(prefix)) + 1;
        return $"{prefix}-{nextNum:D2}";
    }

    /// <summary>
    /// ดึง Prefix สำหรับสร้าง ID จากข้อมูลจิก โดยให้ความสำคัญกับ ToolNo (เช่น HTM39, BK043)
    /// </summary>
    public string ExtractIdPrefix(Jig jig)
    {
        // ลำดับความสำคัญ: ToolNo → PartNumber → PartType → "TOOL"
        if (!string.IsNullOrWhiteSpace(jig.ToolNo))
            return CleanAllSpaces(jig.ToolNo)?.ToUpper() ?? "TOOL";

        if (!string.IsNullOrWhiteSpace(jig.PartNumber))
        {
            var p = jig.PartNumber.Trim();
            if (p.Contains("-"))
            {
                var prefix = p.Split('-')[0];
                return CleanAllSpaces(prefix)?.ToUpper() ?? "TOOL";
            }
            return CleanAllSpaces(p)?.ToUpper() ?? "TOOL";
        }

        if (!string.IsNullOrWhiteSpace(jig.PartType))
            return CleanAllSpaces(jig.PartType)?.ToUpper() ?? "TOOL";

        return "TOOL";
    }

    /// <summary>ดึงหมายเลขลำดับสูงสุดที่มีอยู่ในฐานข้อมูลสำหรับ ToolNo ที่ระบุ</summary>
    public async Task<int> GetMaxSuffixFromDb(string toolNo)
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

    /// <summary>
    /// ทำความสะอาดข้อมูลจิกก่อนบันทึก — ลบ Whitespace จากรหัสและแปลงช่องว่างซ้ำซ้อนในคำอธิบาย
    /// </summary>
    public void SanitizeJig(Jig jig)
    {
        if (jig == null) return;
        
        // ฟิลด์ที่ไม่ควรมีช่องว่าง (รหัส)
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

        // ฟิลด์ที่อาจมีช่องว่างแต่ต้องแปลงเป็นช่องว่างเดียว (คำอธิบาย)
        jig.PartType = CleanAllSpaces(jig.PartType); 
        jig.StepPrint = NormalizeSpaces(jig.StepPrint); 
        jig.JigType = NormalizeSpaces(jig.JigType);
        jig.Process = NormalizeSpaces(jig.Process);
        jig.Status = CleanAllSpaces(jig.Status) ?? "Available";
        jig.Condition = CleanAllSpaces(jig.Condition) ?? "Good";
        
        jig.SmartCodeName = NormalizeSpaces(jig.SmartCodeName);
    }

    /// <summary>
    /// สร้างชื่อ Smart Code โดยต่อคำสำคัญของจิกเข้าด้วยกัน — ใช้สำหรับสร้าง QR Code และระบุตัวอย่างรวดเร็ว
    /// </summary>
    public string GenerateSmartCodeName(Jig jig)
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
        return string.Join(" ", parts);
    }

    /// <summary>ลบ Whitespace ทั้งหมดออกจากค่า — ใช้สำหรับรหัสที่ห้ามมีช่องว่าง</summary>
    /// <param name="val">ค่าที่ต้องการทำความสะอาด</param>
    /// <returns>ค่าที่ไม่มี Whitespace หรือ null</returns>
    public string? CleanAllSpaces(string? val)
    {
        if (string.IsNullOrWhiteSpace(val)) return val?.Trim();
        return new string(val.Where(c => !char.IsWhiteSpace(c)).ToArray());
    }

    /// <summary>แปลงช่องว่างซ้ำซ้อนเป็นช่องว่างเดียว — ใช้สำหรับคำอธิบายและ Label</summary>
    /// <param name="val">ค่าที่ต้องการทำความสะอาด</param>
    /// <returns>ค่าที่ทำความสะอาดแล้ว หรือ null</returns>
    public string? NormalizeSpaces(string? val)
    {
        if (string.IsNullOrWhiteSpace(val)) return val?.Trim();
        return Regex.Replace(val.Trim(), @"\s+", " ");
    }
}
