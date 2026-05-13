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

        // สร้างตาราง JigStateSnapshots ถ้ายังไม่มี (สำหรับระบบยกเลิกรายการ)
        try
        {
            context.Database.ExecuteSqlRaw(@"
                IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='JigStateSnapshots' AND xtype='U')
                CREATE TABLE JigStateSnapshots (
                    Id NVARCHAR(450) NOT NULL PRIMARY KEY,
                    TransactionId NVARCHAR(450) NOT NULL,
                    JigUid NVARCHAR(450) NOT NULL,
                    PreviousStatus NVARCHAR(MAX) NOT NULL,
                    PreviousCondition NVARCHAR(MAX) NOT NULL,
                    PreviousLocatorId NVARCHAR(450) NULL,
                    CreatedAt DATETIME2 NOT NULL DEFAULT GETDATE()
                )");
            
            // สร้าง Index ถ้ายังไม่มี
            context.Database.ExecuteSqlRaw(@"
                IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name='IX_JigStateSnapshots_TransactionId')
                CREATE INDEX IX_JigStateSnapshots_TransactionId ON JigStateSnapshots(TransactionId)");
        }
        catch { /* ข้ามถ้า DB ไม่รองรับ */ }

        context.SaveChanges();
    }
}
