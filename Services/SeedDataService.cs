using Inventory.Models;
using Inventory.Data;

namespace Inventory.Services;

public class SeedDataService
{
    public void SeedDatabase(AppDbContext context)
    {
        try
        {
            // Attempt to add new columns to JigSpecs table. It will fail if they already exist, so we use Try/Catch
            Microsoft.EntityFrameworkCore.RelationalDatabaseFacadeExtensions.ExecuteSqlRaw(context.Database, 
                "ALTER TABLE JigSpecs ADD [Week] nvarchar(max) DEFAULT ''; " +
                "ALTER TABLE JigSpecs ADD [Item] nvarchar(max) DEFAULT ''; " +
                "ALTER TABLE JigSpecs ADD [Rev] nvarchar(max) DEFAULT ''; " +
                "ALTER TABLE JigSpecs ADD [PictureUrl] nvarchar(max) DEFAULT ''; " +
                "ALTER TABLE JigSpecs ADD [ToyNumber] nvarchar(max) DEFAULT ''; " +
                "ALTER TABLE JigSpecs ADD [PartNumber] nvarchar(max) DEFAULT ''; " +
                "ALTER TABLE JigSpecs ADD [PartType] nvarchar(max) DEFAULT ''; " +
                "ALTER TABLE JigSpecs ADD [JigType] nvarchar(max) DEFAULT ''; " +
                "ALTER TABLE JigSpecs ADD [ToolNo] nvarchar(max) DEFAULT ''; " +
                "ALTER TABLE JigSpecs ADD [ToolType] nvarchar(max) DEFAULT ''; " +
                "ALTER TABLE JigSpecs ADD [TotalStepPrint] nvarchar(max) DEFAULT ''; " +
                "ALTER TABLE JigSpecs ADD [UnitAmount] nvarchar(max) DEFAULT ''; " +
                "ALTER TABLE JigSpecs ADD [Feed] nvarchar(max) DEFAULT ''; " +
                "ALTER TABLE JigSpecs ADD [Scan] nvarchar(max) DEFAULT '';");
        }
        catch { }

        try 
        {
            Microsoft.EntityFrameworkCore.RelationalDatabaseFacadeExtensions.ExecuteSqlRaw(context.Database, 
                "UPDATE JigSpecs SET " +
                "[Week] = ISNULL([Week], ''), " +
                "[Item] = ISNULL([Item], ''), " +
                "[Rev] = ISNULL([Rev], ''), " +
                "[PictureUrl] = ISNULL([PictureUrl], ''), " +
                "[ToyNumber] = ISNULL([ToyNumber], ''), " +
                "[PartNumber] = ISNULL([PartNumber], ''), " +
                "[PartType] = ISNULL([PartType], ''), " +
                "[JigType] = ISNULL([JigType], ''), " +
                "[ToolNo] = ISNULL([ToolNo], ''), " +
                "[ToolType] = ISNULL([ToolType], ''), " +
                "[TotalStepPrint] = ISNULL([TotalStepPrint], ''), " +
                "[UnitAmount] = ISNULL([UnitAmount], ''), " +
                "[Feed] = ISNULL([Feed], ''), " +
                "[Scan] = ISNULL([Scan], '')");
        } 
        catch { }

        try
        {
            Microsoft.EntityFrameworkCore.RelationalDatabaseFacadeExtensions.ExecuteSqlRaw(context.Database, 
                "ALTER TABLE PhysicalJigs ADD [Tool] nvarchar(max) DEFAULT ''; " +
                "ALTER TABLE PhysicalJigs ADD [NamePlateBlack] nvarchar(max) DEFAULT ''; " +
                "ALTER TABLE PhysicalJigs ADD [NamePlateWhite] nvarchar(max) DEFAULT ''; " +
                "ALTER TABLE PhysicalJigs ADD [Part] nvarchar(max) DEFAULT ''; " +
                "ALTER TABLE PhysicalJigs ADD [JigType] nvarchar(max) DEFAULT ''; " +
                "ALTER TABLE PhysicalJigs ADD [StepPrint] nvarchar(max) DEFAULT ''; " +
                "ALTER TABLE PhysicalJigs ADD [HG] nvarchar(max) DEFAULT ''; " +
                "ALTER TABLE PhysicalJigs ADD [FS] nvarchar(max) DEFAULT ''; " +
                "ALTER TABLE PhysicalJigs ADD [IssueDate] nvarchar(max) DEFAULT ''; " +
                "ALTER TABLE PhysicalJigs ADD [JigCapacity] nvarchar(max) DEFAULT '';");
        }
        catch { }

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
