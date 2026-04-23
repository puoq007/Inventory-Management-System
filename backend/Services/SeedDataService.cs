using shared.Models;
using backend.Data;
using Microsoft.EntityFrameworkCore;

namespace backend.Services;

public class SeedDataService
{
    public void SeedDatabase(AppDbContext context)
    {
        // Seed default admin user only if no users exist
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
        if (!context.Locators.Any(l => l.Type == "Production"))
        {
            context.Locators.Add(new Locator { Id = "ZONE-A", Site = "SITE1", Cabinet = "-", Shelf = "Zone A", Type = "Production" });
            context.Locators.Add(new Locator { Id = "ZONE-B", Site = "SITE1", Cabinet = "-", Shelf = "Zone B", Type = "Production" });
        }
        if (!context.Locators.Any(l => l.Type == "Cleaning"))
        {
            context.Locators.Add(new Locator { Id = "CLEANING-ZONE", Site = "SITE1", Cabinet = "-", Shelf = "Cleaning Zone", Type = "Cleaning" });
        }

        context.SaveChanges();
    }
}
