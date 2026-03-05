using Inventory.Models;
using Inventory.Data;

namespace Inventory.Services;

public class SeedDataService
{
    private void AddColumn(AppDbContext context, string table, string column)
    {
        try 
        { 
            Microsoft.EntityFrameworkCore.RelationalDatabaseFacadeExtensions.ExecuteSqlRaw(context.Database, $"ALTER TABLE {table} ADD [{column}] nvarchar(max) DEFAULT '';"); 
        } 
        catch { }
    }

    public void SeedDatabase(AppDbContext context)
    {
        string[] jigSpecsCols = { "Week", "Item", "Rev", "PictureUrl", "ToyNumber", "PartNumber", "PartType", "JigType", "ToolNo", "ToolType", "TotalStepPrint", "UnitAmount", "Feed", "Scan" };
        foreach (var col in jigSpecsCols)
        {
            AddColumn(context, "JigSpecs", col);
        }

        string[] physicalJigsCols = { "Tool", "NamePlateBlack", "NamePlateWhite", "Part", "JigType", "StepPrint", "HG", "FS", "IssueDate", "JigCapacity" };
        foreach (var col in physicalJigsCols)
        {
            AddColumn(context, "PhysicalJigs", col);
        }

        try 
        {
            Microsoft.EntityFrameworkCore.RelationalDatabaseFacadeExtensions.ExecuteSqlRaw(context.Database, 
                "UPDATE PhysicalJigs SET " +
                "[Tool] = ISNULL([Tool], ''), " +
                "[NamePlateBlack] = ISNULL([NamePlateBlack], ''), " +
                "[NamePlateWhite] = ISNULL([NamePlateWhite], ''), " +
                "[Part] = ISNULL([Part], ''), " +
                "[JigType] = ISNULL([JigType], ''), " +
                "[StepPrint] = ISNULL([StepPrint], ''), " +
                "[HG] = ISNULL([HG], ''), " +
                "[FS] = ISNULL([FS], ''), " +
                "[IssueDate] = ISNULL([IssueDate], ''), " +
                "[JigCapacity] = ISNULL([JigCapacity], '')");
        } 
        catch { }

        // Force clearing to re-run seed data with populated properties
        bool hasOldData = false;
        if (hasOldData)
        {
            Microsoft.EntityFrameworkCore.RelationalDatabaseFacadeExtensions.ExecuteSqlRaw(context.Database, "DELETE FROM Transactions");
            Microsoft.EntityFrameworkCore.RelationalDatabaseFacadeExtensions.ExecuteSqlRaw(context.Database, "DELETE FROM PhysicalJigs");
            Microsoft.EntityFrameworkCore.RelationalDatabaseFacadeExtensions.ExecuteSqlRaw(context.Database, "DELETE FROM PartJigMappings");
            Microsoft.EntityFrameworkCore.RelationalDatabaseFacadeExtensions.ExecuteSqlRaw(context.Database, "DELETE FROM JigSpecs");
            Microsoft.EntityFrameworkCore.RelationalDatabaseFacadeExtensions.ExecuteSqlRaw(context.Database, "DELETE FROM Locators");
        }

        if (!context.Users.Any())
        {
            context.Users.Add(new UserAccount { EmployeeId = "admin", Name = "Administrator", Role = "Admin", Password = "admin" });
            context.SaveChanges();
        }

        if (!context.Locators.Any())
        {
            // Locators data omitted for Git repository security
        }

        if (!context.JigSpecs.Any())
        {
            // Company data omitted for Git repository security
        }

        if (!context.PartJigMappings.Any())
        {
            // Company data omitted for Git repository security
        }

        if (!context.PhysicalJigs.Any())
        {
            // Company data omitted for Git repository security
        }
    }
}
