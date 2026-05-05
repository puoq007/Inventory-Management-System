using shared.Models;
using Microsoft.EntityFrameworkCore;

namespace backend.Data;

/// <summary>
/// บริบทของฐานข้อมูล (Entity Framework Core) — จัดการตารางทั้งหมดในระบบ
/// รองรับ MS SQL Server ผ่าน Connection String ใน appsettings.json
/// </summary>
public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    /// <summary>ตารางผู้ใช้งาน</summary>
    public DbSet<UserAccount> Users { get; set; } = null!;
    /// <summary>ตารางตำแหน่งจัดเก็บ</summary>
    public DbSet<Locator> Locators { get; set; } = null!;
    /// <summary>ตารางธุรกรรมการใช้งานจิก (เบิก/คืน/แจ้งปัญหา)</summary>
    public DbSet<TransactionRow> Transactions { get; set; } = null!;
    /// <summary>ตารางจิก (หัวใจของระบบ)</summary>
    public DbSet<Jig> Jigs { get; set; } = null!;
    /// <summary>ตารางชิ้นส่วน Part Number</summary>
    public DbSet<PartMaster> PartMasters { get; set; } = null!;
    /// <summary>ตารางเชื่อมจิก-ชิ้นส่วน (Many-to-Many)</summary>
    public DbSet<JigPartMapping> JigPartMappings { get; set; } = null!;


    /// <summary>
    /// กำหนด Primary Key, Index และ Constraint ของแต่ละตาราง
    /// </summary>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<UserAccount>().HasKey(u => u.EmployeeId);
        modelBuilder.Entity<Locator>().HasKey(l => l.Id);
        modelBuilder.Entity<TransactionRow>().HasKey(t => t.Id);
        
        modelBuilder.Entity<Jig>().HasKey(j => j.Uid);
        modelBuilder.Entity<Jig>().HasIndex(j => j.Id).IsUnique();

        modelBuilder.Entity<PartMaster>().HasKey(p => p.PartNumber);
        modelBuilder.Entity<JigPartMapping>().HasKey(m => m.Id);
        modelBuilder.Entity<JigPartMapping>().HasIndex(m => new { m.ToolNo, m.PartNumber }).IsUnique();


    }
}
