using shared.Models;
using backend.Data;

namespace backend.Services;

public class SeedDataService
{
    private void AddColumn(AppDbContext context, string table, string column)
    {
        try 
        {
#pragma warning disable EF1002 // Vulnerability to SQL injection
            Microsoft.EntityFrameworkCore.RelationalDatabaseFacadeExtensions.ExecuteSqlRaw(context.Database, $"ALTER TABLE {table} ADD [{column}] nvarchar(max) DEFAULT '';"); 
#pragma warning restore EF1002 // Vulnerability to SQL injection
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
        bool hasOldData = false; // Set to false to prevent dropping data again
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

        bool resetLocators = false;
        if (resetLocators)
        {
            Microsoft.EntityFrameworkCore.RelationalDatabaseFacadeExtensions.ExecuteSqlRaw(context.Database, "DELETE FROM Locators");
            context.SaveChanges();
        }
    }
}
