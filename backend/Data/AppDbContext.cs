using shared.Models;
using Microsoft.EntityFrameworkCore;

namespace backend.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<UserAccount> Users { get; set; } = null!;
    public DbSet<Locator> Locators { get; set; } = null!;
    public DbSet<TransactionRow> Transactions { get; set; } = null!;
    public DbSet<Jig> Jigs { get; set; } = null!;
    public DbSet<PartMaster> PartMasters { get; set; } = null!;
    public DbSet<JigPartMapping> JigPartMappings { get; set; } = null!;


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
