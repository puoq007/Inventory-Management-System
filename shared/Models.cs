namespace shared.Models;

public class UserAccount
{
    public string EmployeeId { get; set; } = "";
    public string Name { get; set; } = "";
    public string Role { get; set; } = "";
    public string? Password { get; set; }
}

public class JigSpec
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = "";
    public int JigRequired { get; set; }
    
    // New fields from specification table
    public string Week { get; set; } = "";
    public string Item { get; set; } = "";
    public string Rev { get; set; } = "";
    public string PictureUrl { get; set; } = "";
    public string ToyNumber { get; set; } = "";
    public string PartNumber { get; set; } = "";
    public string PartType { get; set; } = "";
    public string JigType { get; set; } = "";
    public string ToolNo { get; set; } = "";
    public string ToolType { get; set; } = "";
    public string TotalStepPrint { get; set; } = "";
    public string UnitAmount { get; set; } = ""; 
    public string Feed { get; set; } = "";
    public string Scan { get; set; } = "";
}

public class PartJigMapping
{
    public string PartNumber { get; set; } = "";
    public string SpecId { get; set; } = "";
}

public class PhysicalJig
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string SpecId { get; set; } = "";
    public string Status { get; set; } = "Available"; // Available, InUse, NeedsCleaning, Evaluation
    public string LocatorId { get; set; } = ""; // Relates to Locator.Id
    public string CurrentDestination { get; set; } = ""; // Where the jig currently is when Checked Out
    public string Condition { get; set; } = "Good";
    public string HomeLocatorId { get; set; } = ""; // Original storage location

    
    // New fields from Physical Jig table
    public string Tool { get; set; } = "";
    public string NamePlateBlack { get; set; } = "";
    public string NamePlateWhite { get; set; } = "";
    public string Part { get; set; } = "";
    public string JigType { get; set; } = "";
    public string StepPrint { get; set; } = "";
    public string HG { get; set; } = "";
    public string FS { get; set; } = "";
    public string IssueDate { get; set; } = "";
    public string JigCapacity { get; set; } = "";

    // Combined fields from Spec Table - REMOVED (Retrieve from JigSpec instead)
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
    public string Name => GetName("EN"); // Default for backward compatibility
}

public class TransactionRow
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public DateTime Timestamp { get; set; } = DateTime.Now;
    public string JigId { get; set; } = "";
    public string Action { get; set; } = ""; // CheckOut, CheckIn, ReportIssue
    public string Destination { get; set; } = ""; // Where it was taken to
    public string User { get; set; } = "";
}

public class CurrentStatusRow
{
    public string PartNumber { get; set; } = "";
    public string SpecName { get; set; } = "";
    public int Available { get; set; }
    public int InUse { get; set; }
    public int Total { get; set; }
}
