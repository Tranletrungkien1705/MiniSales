using Microsoft.EntityFrameworkCore;
using MiniSales.Models;
namespace MiniSales.Data;
public sealed class AppDbContext(DbContextOptions<AppDbContext> opt) : DbContext(opt)
{
    public DbSet<Org> Orgs => Set<Org>();
    public DbSet<SalesOrder> Orders => Set<SalesOrder>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<DeliveryOrder> DeliveryOrders => Set<DeliveryOrder>();
    public DbSet<DealerOrder> DealerOrders => Set<DealerOrder>();
    public DbSet<DealerOrderItem> DealerOrderItems => Set<DealerOrderItem>();
    public DbSet<PaymentGuarantee> PaymentGuarantees => Set<PaymentGuarantee>();
    public DbSet<PaymentGuaranteeDetail> PaymentGuaranteeDetails => Set<PaymentGuaranteeDetail>();
    public DbSet<CarDocRequest> CarDocRequests => Set<CarDocRequest>();
    public DbSet<CarDocRequestDetail> CarDocRequestDetails => Set<CarDocRequestDetail>();
    public DbSet<CarInvoice> CarInvoices => Set<CarInvoice>();
    public DbSet<CarInvoiceDetail> CarInvoiceDetails => Set<CarInvoiceDetail>();
    public DbSet<CarRetrieveOrder> CarRetrieveOrders => Set<CarRetrieveOrder>();
    public DbSet<CarRetrieveOrderDetail> CarRetrieveOrderDetails => Set<CarRetrieveOrderDetail>();
    public DbSet<ContractCancelOrder> ContractCancels => Set<ContractCancelOrder>();
    public DbSet<ContractCancelCar> ContractCancelCars => Set<ContractCancelCar>();
    public DbSet<ContractCancelDtl> ContractCancelDtls => Set<ContractCancelDtl>();
    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<Org>().HasIndex(x => x.ApiKey).IsUnique();
        b.Entity<SalesOrder>().HasIndex(x => new { x.OrgId, x.Code }).IsUnique();
        b.Entity<SalesOrder>().Property(x => x.Status).HasConversion<int>();
        b.Entity<DeliveryOrder>().HasIndex(x => new { x.OrgId, x.DeliveryOrderNo }).IsUnique();
        b.Entity<DeliveryOrder>().Property(x => x.Status).HasConversion<int>();
        b.Entity<DealerOrder>().HasIndex(x => new { x.OrgId, x.OrderNo }).IsUnique();
        b.Entity<DealerOrder>().Property(x => x.OrderType).HasConversion<int>();
        b.Entity<DealerOrder>().Property(x => x.Status).HasConversion<int>();
        b.Entity<DealerOrder>().HasMany(x => x.Items).WithOne().HasForeignKey(x => x.OrderId).OnDelete(DeleteBehavior.Cascade);
        b.Entity<PaymentGuarantee>().HasIndex(x => new { x.OrgId, x.GuaranteeNo }).IsUnique();
        b.Entity<PaymentGuarantee>().Property(x => x.GuaranteeType).HasConversion<int>();
        b.Entity<PaymentGuarantee>().Property(x => x.Status).HasConversion<int>();
        b.Entity<PaymentGuarantee>().HasMany(x => x.Details).WithOne().HasForeignKey(x => x.GuaranteeId).OnDelete(DeleteBehavior.Cascade);
        b.Entity<CarDocRequest>().HasIndex(x => new { x.OrgId, x.DRListCode }).IsUnique();
        b.Entity<CarDocRequest>().Property(x => x.RequestType).HasConversion<int>();
        b.Entity<CarDocRequest>().Property(x => x.Status).HasConversion<int>();
        b.Entity<CarDocRequest>().HasMany(x => x.Details).WithOne().HasForeignKey(x => x.RequestId).OnDelete(DeleteBehavior.Cascade);
        b.Entity<CarInvoice>().HasIndex(x => new { x.OrgId, x.InvoiceCode }).IsUnique();
        b.Entity<CarInvoice>().Property(x => x.InvoiceType).HasConversion<int>();
        b.Entity<CarInvoice>().Property(x => x.AdjType).HasConversion<int>();
        b.Entity<CarInvoice>().Property(x => x.Status).HasConversion<int>();
        b.Entity<CarInvoice>().HasMany(x => x.Details).WithOne().HasForeignKey(x => x.InvoiceId).OnDelete(DeleteBehavior.Cascade);
        b.Entity<CarRetrieveOrder>().HasIndex(x => new { x.OrgId, x.RetrieveOrderNo }).IsUnique();
        b.Entity<CarRetrieveOrder>().Property(x => x.Status).HasConversion<int>();
        b.Entity<CarRetrieveOrder>().HasMany(x => x.Details).WithOne().HasForeignKey(x => x.RetrieveOrderId).OnDelete(DeleteBehavior.Cascade);
        b.Entity<CarRetrieveOrderDetail>().Property(x => x.Status).HasConversion<int>();
        b.Entity<ContractCancelOrder>().HasIndex(x => new { x.OrgId, x.ContractCNo }).IsUnique();
        b.Entity<ContractCancelOrder>().Property(x => x.Status).HasConversion<int>();
        b.Entity<ContractCancelOrder>().HasMany(x => x.Cars).WithOne().HasForeignKey(x => x.ContractCancelId).OnDelete(DeleteBehavior.Cascade);
        b.Entity<ContractCancelOrder>().HasMany(x => x.Details).WithOne().HasForeignKey(x => x.ContractCancelId).OnDelete(DeleteBehavior.Cascade);
    }
}
