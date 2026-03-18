using shared.Models;
using backend.Data;
using Microsoft.EntityFrameworkCore;

namespace backend.Services;

public class SeedDataService
{
    public void SeedDatabase(AppDbContext context)
    {
        try 
        {
            // Ensure non-nullable string columns always have empty string (not NULL)
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

        // Seed default admin user only if no users exist
        // NOTE: Change this password immediately after first login
        if (!context.Users.Any())
        {
            context.Users.Add(new UserAccount 
            { 
                EmployeeId = "admin", 
                Name = "Administrator", 
                Role = "Admin", 
                Password = "admin" 
            });
        }

        // Seed minimal location zones required for the system to function
        // Production zones and cleaning zone are required for Scan In/Out
        if (!context.Locators.Any(l => l.Type == "Production"))
        {
            context.Locators.Add(new Locator { Id = "ZONE-A", Site = "SITE1", Cabinet = "-", Shelf = "Zone A", Position = "-", Type = "Production" });
            context.Locators.Add(new Locator { Id = "ZONE-B", Site = "SITE1", Cabinet = "-", Shelf = "Zone B", Position = "-", Type = "Production" });
        }
        if (!context.Locators.Any(l => l.Type == "Cleaning"))
        {
            context.Locators.Add(new Locator { Id = "CLEANING-ZONE", Site = "SITE1", Cabinet = "-", Shelf = "Cleaning Zone", Position = "-", Type = "Cleaning" });
        }

        // NOTE: All actual company data (Jig Specs, Physical Jigs, Locators, Users)
        // must be entered via the Admin UI or imported via Excel/CSV import.
        // Do NOT hardcode real company data in this file.

        context.SaveChanges();
    }
}
