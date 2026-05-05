using System.ComponentModel.DataAnnotations;

namespace shared.Models;

/// <summary>
/// โมเดลบัญชีผู้ใช้งาน — เก็บข้อมูลพนักงานสำหรับเข้าสู่ระบบและกำหนดสิทธิ์
/// </summary>
public class UserAccount
{
    /// <summary>รหัสพนักงาน (Primary Key)</summary>
    public string EmployeeId { get; set; } = "";
    /// <summary>ชื่อ-นามสกุลพนักงาน</summary>
    public string Name { get; set; } = "";
    /// <summary>สิทธิ์การใช้งาน: Admin, Engineer, ProdLead, Operator</summary>
    public string Role { get; set; } = "";
    /// <summary>รหัสผ่าน (nullable สำหรับ Role ที่ไม่ต้องกรอก)</summary>
    public string? Password { get; set; }
}

/// <summary>
/// โมเดลตำแหน่งจัดเก็บจิก — ประกอบด้วย Site, ตู้, ชั้น และประเภทโซน
/// </summary>
public class Locator
{
    /// <summary>รหัสตำแหน่ง (สร้างอัตโนมัติจาก Site-Cabinet-Shelf)</summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();
    /// <summary>รหัส Site เช่น "1" = MBK1, "2" = Mintech</summary>
    public string Site { get; set; } = "";
    /// <summary>หมายเลขตู้ เช่น "1", "2", ... "10"</summary>
    public string Cabinet { get; set; } = "";
    /// <summary>หมายเลขชั้น เช่น "1", "2", ... "5"</summary>
    public string Shelf { get; set; } = "";
    /// <summary>ประเภทโซน: Store (จัดเก็บ), Production (ผลิต), Cleaning (ล้าง)</summary>
    public string Type { get; set; } = "Store";

    /// <summary>
    /// สร้างรหัสตำแหน่งอัตโนมัติจาก Site + Cabinet + Shelf เช่น "MBK1-2-3"
    /// </summary>
    public string GetGeneratedId()
    {
        string s = (Site ?? "").Trim();
        string c = (Cabinet ?? "").Trim();
        string h = (Shelf ?? "").Trim();

        if (string.IsNullOrEmpty(s) && string.IsNullOrEmpty(c) && string.IsNullOrEmpty(h))
            return Id;

        // Map site code to display name for ID prefix
        string sitePrefix = s switch { "1" => "MBK1", "2" => "Mintech", _ => $"MBK{s}" };

        string final = sitePrefix;
        if (!string.IsNullOrEmpty(c)) final += $"-{c}";
        if (!string.IsNullOrEmpty(h)) final += $"-{h}";
        return final;
    }

    /// <summary>คืนชื่อตำแหน่งตามภาษาที่เลือก ("TH" หรือ "EN")</summary>
    public string GetName(string lang) => (Cabinet == "-")
        ? $"{SiteDisplayName} {Shelf}"
        : (lang == "TH"
            ? $"{SiteDisplayName} ตู้ {Cabinet} ชั้น {Shelf}"
            : $"{SiteDisplayName} Cabinet {Cabinet} Shelf {Shelf}");
    /// <summary>ชื่อตำแหน่งแบบอังกฤษ (ค่า default)</summary>
    public string Name => GetName("EN");

    /// <summary>แปลงรหัส Site เป็นชื่อแสดงผล</summary>
    public string SiteDisplayName => Site switch { "1" => "MBK1", "2" => "Mintech", _ => Site };
}

/// <summary>
/// โมเดลชิ้นส่วน — เก็บรายการ Part Number ที่ใช้ในระบบ
/// </summary>
public class PartMaster
{
    /// <summary>รหัสชิ้นส่วน (Primary Key)</summary>
    [Key]
    public string PartNumber { get; set; } = string.Empty;
}

/// <summary>
/// ตารางเชื่อมความสัมพันธ์ระหว่างจิก (ToolNo) กับชิ้นส่วน (PartNumber) — แบบ Many-to-Many
/// </summary>
public class JigPartMapping
{
    /// <summary>รหัสอ้างอิง (Primary Key, สร้างอัตโนมัติ)</summary>
    [Key]
    public string Id { get; set; } = Guid.NewGuid().ToString();
    /// <summary>รหัสเครื่องมือ — เชื่อมกับ Jig.ToolNo</summary>
    public string ToolNo { get; set; } = string.Empty;
    /// <summary>รหัสชิ้นส่วน — เชื่อมกับ PartMaster.PartNumber</summary>
    public string PartNumber { get; set; } = string.Empty;
}



/// <summary>
/// บันทึกธุรกรรมการใช้งานจิก — ทุกครั้งที่มีการเบิก/คืน/แจ้งปัญหาจะสร้าง record ใหม่
/// </summary>
public class TransactionRow
{
    /// <summary>รหัสธุรกรรม (Primary Key, สร้างอัตโนมัติ)</summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();
    /// <summary>วันเวลาที่ทำรายการ</summary>
    public DateTime Timestamp { get; set; } = DateTime.Now;
    /// <summary>รหัสจิกภายใน — เชื่อมกับ Jig.Uid</summary>
    public string JigUid { get; set; } = "";
    /// <summary>ประเภทรายการ: CheckOut (เบิก), CheckIn (คืน), ReportIssue (แจ้งปัญหา)</summary>
    public string Action { get; set; } = "";
    /// <summary>ปลายทางที่นำจิกไป เช่น ชื่อตำแหน่ง หรือ สายการผลิต</summary>
    public string Destination { get; set; } = "";
    /// <summary>ชื่อผู้ทำรายการ</summary>
    public string User { get; set; } = "";
}

/// <summary>
/// โมเดลจิก (Jig) — หัวใจของระบบ เก็บข้อมูลจิกทั้งหมดรวมถึงสถานะ สภาพ และตำแหน่งจัดเก็บ
/// </summary>
public class Jig
{
    /// <summary>รหัสภายใน (Primary Key) — ไม่เปลี่ยนแปลง ใช้อ้างอิงในระบบ</summary>
    [Key]
    public string Uid { get; set; } = Guid.NewGuid().ToString();
    
    /// <summary>รหัสจิกที่แสดงผล (Unique, แก้ไขได้) — ใช้สำหรับ QR Code และค้นหา</summary>
    [Required]
    public string Id { get; set; } = string.Empty;
    
    // --- ข้อมูล Smart Code (ประกอบรวมเป็นชื่อจิกอัตโนมัติ) ---
    /// <summary>ชื่อ Smart Code ที่สร้างจากฟิลด์ต่างๆ รวมกัน</summary>
    public string? SmartCodeName { get; set; }
    /// <summary>รหัสเครื่องมือ (Tool Number)</summary>
    public string? ToolNo { get; set; }
    /// <summary>ขั้นตอนการพิมพ์ เก็บเป็นตัวเลขคั่นด้วย "-" เช่น "1-2-3"</summary>
    public string? StepPrint { get; set; }
    /// <summary>ประเภทชิ้นส่วน เช่น Body, Wing, Window</summary>
    public string? PartType { get; set; }
    /// <summary>วันที่สร้างจิก (รูปแบบ dd/MM/yy)</summary>
    public string? Date { get; set; }
    /// <summary>ค่า Feed ของจิก</summary>
    public string? Feed { get; set; }
    /// <summary>ค่า Scan ของจิก</summary>
    public string? Scan { get; set; }
    /// <summary>จำนวนพิมพ์ต่อรอบ</summary>
    public string? QtyPrint { get; set; }
    /// <summary>ความสูงจิก เช่น "40", "50", "HMR Plywood"</summary>
    public string? HeightJig { get; set; }
    /// <summary>ชนิดจิก เช่น Plywood, Stack, Flip</summary>
    public string? JigType { get; set; }
    /// <summary>กระบวนการผลิต เช่น PIM, PL, VUM</summary>
    public string? Process { get; set; }
    
    // --- ข้อมูลเพิ่มเติมจาก Excel ---
    /// <summary>หมายเลขชิ้นส่วนหลัก (Legacy — ใช้ค่าแรกจาก JigPartMapping)</summary>
    public string? PartNumber { get; set; }
    /// <summary>หมายเลข Revision</summary>
    public string? Rev { get; set; }

    // --- รูปภาพ ---
    /// <summary>URL รูปภาพจิก (เก็บเป็น path ไฟล์หรือ Base64)</summary>
    public string? ImageUrl { get; set; }

    // --- สถานะและตำแหน่ง ---
    /// <summary>สถานะจิก: Available (พร้อมใช้), InUse (ใช้งาน), Evaluation (ประเมิน), Scrapped (จำหน่าย)</summary>
    public string Status { get; set; } = "Available";
    /// <summary>สภาพจิก: Good (ดี), NeedsCleaning (ต้องล้าง), Broken (ชำรุด)</summary>
    public string Condition { get; set; } = "Good";
    /// <summary>รหัสตำแหน่งจัดเก็บปัจจุบัน — เชื่อมกับ Locator.Id</summary>
    public string? LocatorId { get; set; }
    /// <summary>วันเวลาที่สร้างข้อมูล</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    /// <summary>วันเวลาที่อัปเดตล่าสุด</summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
