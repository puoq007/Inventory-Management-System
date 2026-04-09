using System.ComponentModel.DataAnnotations;

namespace shared.Models;

public class UserAccount
{
    public string EmployeeId { get; set; } = "";
    public string Name { get; set; } = "";
    public string Role { get; set; } = "";
    public string? Password { get; set; }
}

public class Locator
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Site { get; set; } = ""; // e.g., "MBK1", "Mintech"
    public string Cabinet { get; set; } = ""; // e.g., "1", "2", ... "10"
    public string Shelf { get; set; } = ""; // e.g., "1", "2", ... "5"
    public string Position { get; set; } = ""; // e.g., "1", "2", ... "10"
    public string Type { get; set; } = "Store"; // Store, Production, Cleaning

    public string GetGeneratedId()
    {
        string s = (Site ?? "").Trim();
        string c = (Cabinet ?? "").Trim();
        string h = (Shelf ?? "").Trim();

        if (string.IsNullOrEmpty(s) && string.IsNullOrEmpty(c) && string.IsNullOrEmpty(h))
            return Id; // Keep current ID if everything is empty (shouldn't happen on new/save)

        // Pattern: MBK{Site}-{Cabinet}-{Shelf}
        // If Cabinet or Shelf is "-", we might omit them or keep them?
        // Let's stick to the user's "MBK1-AO-1" example.
        string final = $"MBK{s}";
        if (!string.IsNullOrEmpty(c)) final += $"-{c}";
        if (!string.IsNullOrEmpty(h)) final += $"-{h}";
        return final;
    }

    public string GetName(string lang) => (Cabinet == "-") ? $"{Site} {Shelf}" : (lang == "TH" ? $"{Site} ตู้ {Cabinet} ชั้น {Shelf}" : $"{Site} Cabinet {Cabinet} Shelf {Shelf}");
    public string Name => GetName("EN");
}

public class PartMaster
{
    [Key]
    public string PartNumber { get; set; } = string.Empty;
}

public class JigPartMapping
{
    [Key]
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string ToolNo { get; set; } = string.Empty; // Maps to Jig.ToolNo
    public string PartNumber { get; set; } = string.Empty; // Maps to PartMaster.PartNumber
}



public class TransactionRow
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public DateTime Timestamp { get; set; } = DateTime.Now;
    public string JigUid { get; set; } = ""; // Point to Jig.Uid instead of Jig.Id
    public string Action { get; set; } = ""; // CheckOut, CheckIn, ReportIssue
    public string Destination { get; set; } = ""; // Where it was taken to
    public string User { get; set; } = "";
}

public class Jig
{
    [Key]
    public string Uid { get; set; } = Guid.NewGuid().ToString(); // Internal Primary Key
    
    [Required]
    public string Id { get; set; } = string.Empty; // Logical ID (Editable)
    
    // Smart Code Name Details
    public string? SmartCodeName { get; set; }
    public string? ToolNo { get; set; }
    public string? StepPrint { get; set; }
    public string? PartType { get; set; }
    public string? Date { get; set; }
    public string? Feed { get; set; }
    public string? Scan { get; set; }
    public string? QtyPrint { get; set; }
    public string? HeightJig { get; set; }
    public string? JigType { get; set; }
    public string? Process { get; set; }
    
    // Extra Details from Excel
    public string? PartNumber { get; set; }
    public string? Rev { get; set; }

    // Image
    public string? ImageUrl { get; set; } // Base64 data URI or file path

    // Status Trackers
    public string Status { get; set; } = "Available"; // Available, InUse, Evaluation, Scrapped
    public string Condition { get; set; } = "Good"; // Good, NeedsCleaning, Broken
    public string? LocatorId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
