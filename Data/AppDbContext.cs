using Microsoft.EntityFrameworkCore;
using MiniSales.Models;
namespace MiniSales.Data;
public sealed class AppDbContext(DbContextOptions<AppDbContext> opt) : DbContext(opt)
{
    public DbSet<Org> Orgs => Set<Org>();
    public DbSet<SalesOrder> Orders => Set<SalesOrder>();
    public DbSet<Payment> Payments => Set<Payment>();
    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<Org>().HasIndex(x => x.ApiKey).IsUnique();
        b.Entity<SalesOrder>().HasIndex(x => new { x.OrgId, x.Code }).IsUnique();
        b.Entity<SalesOrder>().Property(x => x.Status).HasConversion<int>();
    }
}
