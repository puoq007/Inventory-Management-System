namespace Inventory.Models;

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
    public string Condition { get; set; } = "Good";
    
    // New fields from Physical Jig table
    public string NamePlateWhite { get; set; } = "";
    public string StepPrint { get; set; } = "";
    public string HG { get; set; } = "";
    public string FS { get; set; } = "";
    public string IssueDate { get; set; } = "";
    public string JigCapacity { get; set; } = "";
}

public class Locator
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Site { get; set; } = ""; // e.g., "MBK1", "Mintech"
    public string Cabinet { get; set; } = ""; // e.g., "1", "2", ... "10"
    public string Shelf { get; set; } = ""; // e.g., "1", "2", ... "5"
    public string Position { get; set; } = ""; // e.g., "1", "2", ... "10"

    public string Name => $"{Site} ตู้ {Cabinet} ชั้นที่ {Shelf} ตัวที่ {Position}";
}

public class TransactionRow
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public DateTime Timestamp { get; set; } = DateTime.Now;
    public string JigId { get; set; } = "";
    public string Action { get; set; } = ""; // CheckOut, CheckIn, ReportIssue
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
