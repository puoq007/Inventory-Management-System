using shared.Models;
using Microsoft.EntityFrameworkCore;

namespace backend.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<UserAccount> Users { get; set; } = null!;
    public DbSet<JigSpec> JigSpecs { get; set; } = null!;
    public DbSet<PartJigMapping> PartJigMappings { get; set; } = null!;
    public DbSet<PhysicalJig> PhysicalJigs { get; set; } = null!;
    public DbSet<Locator> Locators { get; set; } = null!;
    public DbSet<TransactionRow> Transactions { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<UserAccount>().HasKey(u => u.EmployeeId);
        modelBuilder.Entity<JigSpec>().HasKey(s => s.Id);
        modelBuilder.Entity<PartJigMapping>().HasKey(m => new { m.PartNumber, m.SpecId });
        modelBuilder.Entity<PhysicalJig>().HasKey(j => j.Id);
        modelBuilder.Entity<Locator>().HasKey(l => l.Id);
        modelBuilder.Entity<TransactionRow>().HasKey(t => t.Id);
    }
}
