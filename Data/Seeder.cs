using Microsoft.EntityFrameworkCore;
using MiniSales.Models;
namespace MiniSales.Data;
public static class Seeder
{
    public static async Task SeedAsync(AppDbContext db)
    {
        await db.Database.EnsureCreatedAsync();
        if (!await db.Orgs.AnyAsync(o => o.Id == TenantContext.DefaultOrgId))
            db.Orgs.Add(new Org { Id = TenantContext.DefaultOrgId, Name = "Demo Sales", ApiKey = "demo-sales" });
        await db.SaveChangesAsync();
    }
}
