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

    public string GetName(string lang) => (Cabinet == "-") ? $"{Site} {Shelf}" : (lang == "TH" ? $"{Site} ตู้ {Cabinet} ชั้น {Shelf}" : $"{Site} Cabinet {Cabinet} Shelf {Shelf}");
    public string Name => GetName("EN");
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
    public string? ToyNumber { get; set; }
    public string? PartNumber { get; set; }
    public string? Rev { get; set; }

    // Status Trackers
    public string Status { get; set; } = "Available"; // Available, InUse, Evaluation, Scrapped
    public string Condition { get; set; } = "Good"; // Good, NeedsCleaning, Broken
    public string? LocatorId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
