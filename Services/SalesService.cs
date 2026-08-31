using Microsoft.EntityFrameworkCore;
using MiniSales.Data;
using MiniSales.Models;

namespace MiniSales.Services;

public record CreateOrderDto(string CustomerName, string Phone, string? Vin, string Model, string? DealerCode, decimal ListPrice, decimal Discount);
public record PayDto(decimal Amount, string? RefNo);

public interface ISalesService
{
    Task<object> CreateAsync(CreateOrderDto dto);
    Task<object> ListAsync(string? status, string? dealer);
    Task<object?> DetailAsync(string code);
    Task<object?> PayAsync(string code, string kind, decimal amount, string? refNo);
    Task<object?> DeliverAsync(string code);
    Task<object?> CancelAsync(string code);
    Task<object> StatsAsync();
}

public sealed class SalesService(AppDbContext db, ITenantContext tenant) : ISalesService
{
    private Guid Org => tenant.OrgId;

    public async Task<object> CreateAsync(CreateOrderDto dto)
    {
        if (dto.ListPrice <= 0) throw new InvalidOperationException("ListPrice phải > 0.");
        if (dto.Discount < 0 || dto.Discount > dto.ListPrice) throw new InvalidOperationException("Discount không hợp lệ.");
        var code = "SO" + DateTime.Now.ToString("yyMMddHHmmss");
        var o = new SalesOrder
        {
            OrgId = Org, Code = code, CustomerName = dto.CustomerName.Trim(), Phone = dto.Phone.Trim(),
            Vin = dto.Vin?.Trim().ToUpperInvariant(), Model = dto.Model.Trim(), DealerCode = dto.DealerCode?.Trim() ?? "",
            ListPrice = dto.ListPrice, Discount = dto.Discount, NetPrice = dto.ListPrice - dto.Discount, Status = SalesStatus.Draft
        };
        db.Orders.Add(o);
        await db.SaveChangesAsync();
        return new { o.Code, o.CustomerName, o.Model, o.NetPrice, status = o.Status.ToString() };
    }

    public async Task<object> ListAsync(string? status, string? dealer)
    {
        var q = db.Orders.Where(x => x.OrgId == Org);
        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<SalesStatus>(status, true, out var st)) q = q.Where(x => x.Status == st);
        if (!string.IsNullOrWhiteSpace(dealer)) q = q.Where(x => x.DealerCode == dealer);
        var items = await q.OrderByDescending(x => x.Id).Take(500).Select(x => new
        {
            x.Code, x.CustomerName, x.Phone, x.Vin, x.Model, x.DealerCode, x.NetPrice, x.Paid,
            outstanding = x.NetPrice - x.Paid, status = x.Status.ToString(), x.CreatedAt, x.DeliveredAt
        }).ToListAsync();
        return new { count = items.Count, items };
    }

    private async Task<SalesOrder?> Get(string code)
    {
        code = code.Trim().ToUpperInvariant();
        return await db.Orders.FirstOrDefaultAsync(x => x.OrgId == Org && x.Code == code);
    }

    public async Task<object?> DetailAsync(string code)
    {
        var o = await Get(code);
        if (o is null) return null;
        var pays = await db.Payments.Where(p => p.OrgId == Org && p.OrderId == o.Id).OrderBy(p => p.Id)
            .Select(p => new { p.Kind, p.Amount, p.RefNo, p.At }).ToListAsync();
        return new
        {
            o.Code, o.CustomerName, o.Phone, o.Vin, o.Model, o.DealerCode,
            o.ListPrice, o.Discount, o.NetPrice, o.Paid, outstanding = o.NetPrice - o.Paid,
            status = o.Status.ToString(), o.CreatedAt, o.DeliveredAt, payments = pays
        };
    }

    // Thu cọc/thanh toán: cộng dồn Paid; đủ tiền → Paid, có tiền → Deposited.
    public async Task<object?> PayAsync(string code, string kind, decimal amount, string? refNo)
    {
        if (amount <= 0) throw new InvalidOperationException("Amount phải > 0.");
        var o = await Get(code);
        if (o is null) return null;
        if (o.Status is SalesStatus.Delivered or SalesStatus.Cancelled) throw new InvalidOperationException("Hợp đồng đã đóng.");
        if (o.Paid + amount > o.NetPrice) throw new InvalidOperationException($"Vượt số phải trả (còn {o.NetPrice - o.Paid}).");
        db.Payments.Add(new Payment { OrgId = Org, OrderId = o.Id, Kind = kind == "Payment" ? "Payment" : "Deposit", Amount = amount, RefNo = refNo });
        o.Paid += amount;
        o.Status = o.Paid >= o.NetPrice ? SalesStatus.Paid : SalesStatus.Deposited;
        await db.SaveChangesAsync();
        return new { o.Code, paid = o.Paid, outstanding = o.NetPrice - o.Paid, status = o.Status.ToString() };
    }

    // Giao xe: chỉ khi đã thanh toán đủ (Paid).
    public async Task<object?> DeliverAsync(string code)
    {
        var o = await Get(code);
        if (o is null) return null;
        if (o.Status != SalesStatus.Paid) throw new InvalidOperationException("Chưa thanh toán đủ — không thể giao.");
        o.Status = SalesStatus.Delivered; o.DeliveredAt = DateTime.Now;
        await db.SaveChangesAsync();
        return new { o.Code, status = o.Status.ToString(), o.Vin, o.DeliveredAt };   // Vin để MiniVehicle deliver
    }

    public async Task<object?> CancelAsync(string code)
    {
        var o = await Get(code);
        if (o is null) return null;
        if (o.Status == SalesStatus.Delivered) throw new InvalidOperationException("Đã giao — không hủy.");
        o.Status = SalesStatus.Cancelled;
        await db.SaveChangesAsync();
        return new { o.Code, status = o.Status.ToString() };
    }

    public async Task<object> StatsAsync()
    {
        var q = db.Orders.Where(x => x.OrgId == Org);
        return new
        {
            total = await q.CountAsync(),
            draft = await q.CountAsync(x => x.Status == SalesStatus.Draft),
            deposited = await q.CountAsync(x => x.Status == SalesStatus.Deposited),
            paid = await q.CountAsync(x => x.Status == SalesStatus.Paid),
            delivered = await q.CountAsync(x => x.Status == SalesStatus.Delivered),
            revenueDelivered = await q.Where(x => x.Status == SalesStatus.Delivered).SumAsync(x => (decimal?)x.NetPrice) ?? 0,
            collected = await q.SumAsync(x => (decimal?)x.Paid) ?? 0
        };
    }
}
