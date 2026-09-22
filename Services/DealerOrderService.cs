using Microsoft.EntityFrameworkCore;
using MiniSales.Data;
using MiniSales.Models;

namespace MiniSales.Services;

public record CreateDealerOrderItemDto(
    string Model,
    string? SpecCode,
    string? ColorCode,
    int RequestedQuantity,
    decimal UnitPrice,
    string? Remark
);

public record CreateDealerOrderDto(
    string DealerCode,
    string? DealerName,
    string? OrderType, // "Plan" | "Unplan"
    string OrderMonth, // "yyyy-MM"
    string? SalesPolicyCode,
    string? Remark,
    List<CreateDealerOrderItemDto>? Items
);

public record ApproveItemQtyDto(
    string Model,
    string? ColorCode,
    int ApprovedQuantity
);

public record ApproveDealerOrderDto(
    List<ApproveItemQtyDto>? ItemQuantities,
    string? Remark
);

public record RejectDealerOrderDto(
    string Reason
);

public record CancelDealerOrderDto(
    string? Reason
);

public interface IDealerOrderService
{
    Task<object> CreateAsync(CreateDealerOrderDto dto);
    Task<object> ListAsync(string? status, string? dealer, string? orderType, string? month);
    Task<object?> DetailAsync(string orderNo);
    Task<object?> Approve1Async(string orderNo, ApproveDealerOrderDto? dto);
    Task<object?> Approve2Async(string orderNo);
    Task<object?> RejectAsync(string orderNo, RejectDealerOrderDto dto);
    Task<object?> CancelAsync(string orderNo, CancelDealerOrderDto? dto);
    Task<object> StatsAsync();
}

public sealed class DealerOrderService(AppDbContext db, ITenantContext tenant) : IDealerOrderService
{
    private Guid Org => tenant.OrgId;

    public async Task<object> CreateAsync(CreateDealerOrderDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.DealerCode))
            throw new InvalidOperationException("DealerCode không được để trống.");
        if (string.IsNullOrWhiteSpace(dto.OrderMonth))
            throw new InvalidOperationException("OrderMonth (tháng đặt xe, định dạng yyyy-MM) không được để trống.");
        if (dto.Items is null || dto.Items.Count == 0)
            throw new InvalidOperationException("Đơn đặt hàng phải có ít nhất 1 dòng xe đặt.");

        var orderMonth = dto.OrderMonth.Trim();
        var orderType = string.Equals(dto.OrderType, "Unplan", StringComparison.OrdinalIgnoreCase)
            ? DealerOrderType.Unplan
            : DealerOrderType.Plan;

        var policy = dto.SalesPolicyCode?.Trim();
        if (string.IsNullOrWhiteSpace(policy))
        {
            policy = orderType == DealerOrderType.Plan ? "CSC" : "STANDARD";
        }

        var orderNo = "ORD" + DateTime.Now.ToString("yyMMddHHmmss");
        var items = new List<DealerOrderItem>();
        int totalQty = 0;
        decimal totalAmount = 0m;

        foreach (var it in dto.Items)
        {
            if (string.IsNullOrWhiteSpace(it.Model))
                throw new InvalidOperationException("Tên model xe trong danh sách không được để trống.");
            if (it.RequestedQuantity <= 0)
                throw new InvalidOperationException($"Số lượng đặt cho xe {it.Model} phải lớn hơn 0 (nhập: {it.RequestedQuantity}).");
            if (it.UnitPrice < 0)
                throw new InvalidOperationException($"Đơn giá xe {it.Model} không được âm.");

            var lineAmount = it.RequestedQuantity * it.UnitPrice;
            totalQty += it.RequestedQuantity;
            totalAmount += lineAmount;

            items.Add(new DealerOrderItem
            {
                Model = it.Model.Trim(),
                SpecCode = it.SpecCode?.Trim(),
                ColorCode = it.ColorCode?.Trim(),
                RequestedQuantity = it.RequestedQuantity,
                ApprovedQuantity = 0,
                UnitPrice = it.UnitPrice,
                TotalAmount = lineAmount,
                Remark = it.Remark?.Trim()
            });
        }

        var order = new DealerOrder
        {
            OrgId = Org,
            OrderNo = orderNo,
            DealerCode = dto.DealerCode.Trim().ToUpperInvariant(),
            DealerName = string.IsNullOrWhiteSpace(dto.DealerName) ? dto.DealerCode.Trim() : dto.DealerName.Trim(),
            OrderType = orderType,
            OrderMonth = orderMonth,
            SalesPolicyCode = policy,
            Status = DealerOrderStatus.Pending,
            TotalQuantity = totalQty,
            TotalAmount = totalAmount,
            Remark = dto.Remark?.Trim(),
            CreatedAt = DateTime.Now,
            Items = items
        };

        db.DealerOrders.Add(order);
        await db.SaveChangesAsync();

        return new
        {
            orderNo = order.OrderNo,
            dealerCode = order.DealerCode,
            dealerName = order.DealerName,
            orderType = order.OrderType.ToString(),
            orderMonth = order.OrderMonth,
            salesPolicyCode = order.SalesPolicyCode,
            status = order.Status.ToString(),
            totalQuantity = order.TotalQuantity,
            totalAmount = order.TotalAmount,
            itemCount = order.Items.Count,
            createdAt = order.CreatedAt
        };
    }

    public async Task<object> ListAsync(string? status, string? dealer, string? orderType, string? month)
    {
        var q = db.DealerOrders.Where(x => x.OrgId == Org);

        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<DealerOrderStatus>(status, true, out var st))
            q = q.Where(x => x.Status == st);
        if (!string.IsNullOrWhiteSpace(dealer))
            q = q.Where(x => x.DealerCode == dealer.Trim().ToUpperInvariant());
        if (!string.IsNullOrWhiteSpace(orderType) && Enum.TryParse<DealerOrderType>(orderType, true, out var ot))
            q = q.Where(x => x.OrderType == ot);
        if (!string.IsNullOrWhiteSpace(month))
            q = q.Where(x => x.OrderMonth == month.Trim());

        var items = await q.OrderByDescending(x => x.Id).Take(500).Select(x => new
        {
            x.OrderNo,
            x.DealerCode,
            x.DealerName,
            orderType = x.OrderType.ToString(),
            x.OrderMonth,
            x.SalesPolicyCode,
            status = x.Status.ToString(),
            x.TotalQuantity,
            x.TotalAmount,
            x.CreatedAt,
            x.Approved1At,
            x.Approved2At
        }).ToListAsync();

        return new { count = items.Count, items };
    }

    private async Task<DealerOrder?> GetByNo(string orderNo)
    {
        var code = orderNo.Trim().ToUpperInvariant();
        return await db.DealerOrders
            .Include(x => x.Items)
            .FirstOrDefaultAsync(x => x.OrgId == Org && x.OrderNo == code);
    }

    public async Task<object?> DetailAsync(string orderNo)
    {
        var item = await GetByNo(orderNo);
        if (item is null) return null;

        return new
        {
            item.OrderNo,
            item.DealerCode,
            item.DealerName,
            orderType = item.OrderType.ToString(),
            item.OrderMonth,
            item.SalesPolicyCode,
            status = item.Status.ToString(),
            item.TotalQuantity,
            item.TotalAmount,
            item.Remark,
            item.RejectReason,
            item.CreatedAt,
            item.Approved1At,
            item.Approved2At,
            item.CancelledAt,
            items = item.Items.Select(it => new
            {
                it.Id,
                it.Model,
                it.SpecCode,
                it.ColorCode,
                it.RequestedQuantity,
                it.ApprovedQuantity,
                it.UnitPrice,
                it.TotalAmount,
                it.Remark
            }).ToList()
        };
    }

    public async Task<object?> Approve1Async(string orderNo, ApproveDealerOrderDto? dto)
    {
        var item = await GetByNo(orderNo);
        if (item is null) return null;

        if (item.Status != DealerOrderStatus.Pending)
            throw new InvalidOperationException($"Đơn hàng {item.OrderNo} không ở trạng thái Chờ duyệt (hiện tại: {item.Status}).");

        // Cập nhật số lượng duyệt từng dòng xe nếu có danh sách chỉ định
        if (dto?.ItemQuantities is not null && dto.ItemQuantities.Count > 0)
        {
            foreach (var approvedLine in dto.ItemQuantities)
            {
                var matched = item.Items.FirstOrDefault(i =>
                    string.Equals(i.Model, approvedLine.Model, StringComparison.OrdinalIgnoreCase) &&
                    (string.IsNullOrEmpty(approvedLine.ColorCode) || string.Equals(i.ColorCode, approvedLine.ColorCode, StringComparison.OrdinalIgnoreCase)));
                if (matched != null)
                {
                    if (approvedLine.ApprovedQuantity < 0)
                        throw new InvalidOperationException($"Số lượng duyệt cho xe {matched.Model} không được âm.");
                    if (approvedLine.ApprovedQuantity > matched.RequestedQuantity)
                        throw new InvalidOperationException($"Số lượng duyệt ({approvedLine.ApprovedQuantity}) vượt quá số lượng đặt ({matched.RequestedQuantity}) cho xe {matched.Model}.");
                    matched.ApprovedQuantity = approvedLine.ApprovedQuantity;
                }
            }
        }
        else
        {
            // Mặc định duyệt 100% số lượng đặt nếu không chỉ định cắt giảm
            foreach (var line in item.Items)
            {
                line.ApprovedQuantity = line.RequestedQuantity;
            }
        }

        item.Status = DealerOrderStatus.Approved1;
        item.Approved1At = DateTime.Now;
        if (!string.IsNullOrWhiteSpace(dto?.Remark))
        {
            item.Remark = string.IsNullOrWhiteSpace(item.Remark)
                ? dto.Remark.Trim()
                : $"{item.Remark} | Duyệt C1: {dto.Remark.Trim()}";
        }

        await db.SaveChangesAsync();

        return new
        {
            item.OrderNo,
            item.DealerCode,
            status = item.Status.ToString(),
            item.Approved1At,
            approvedItems = item.Items.Select(i => new
            {
                i.Model,
                i.ColorCode,
                i.RequestedQuantity,
                i.ApprovedQuantity
            }).ToList()
        };
    }

    public async Task<object?> Approve2Async(string orderNo)
    {
        var item = await GetByNo(orderNo);
        if (item is null) return null;

        if (item.Status != DealerOrderStatus.Approved1)
            throw new InvalidOperationException($"Đơn hàng {item.OrderNo} phải qua Duyệt cấp 1 (Approved1) trước khi chốt duyệt cấp 2 (hiện tại: {item.Status}).");

        item.Status = DealerOrderStatus.Approved2;
        item.Approved2At = DateTime.Now;
        await db.SaveChangesAsync();

        return new
        {
            item.OrderNo,
            item.DealerCode,
            status = item.Status.ToString(),
            item.Approved2At,
            totalQuantity = item.Items.Sum(i => i.ApprovedQuantity),
            totalAmount = item.Items.Sum(i => i.ApprovedQuantity * i.UnitPrice)
        };
    }

    public async Task<object?> RejectAsync(string orderNo, RejectDealerOrderDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Reason))
            throw new InvalidOperationException("Cần cung cấp lý do từ chối duyệt đơn hàng.");

        var item = await GetByNo(orderNo);
        if (item is null) return null;

        if (item.Status != DealerOrderStatus.Pending && item.Status != DealerOrderStatus.Approved1)
            throw new InvalidOperationException($"Đơn hàng {item.OrderNo} ở trạng thái {item.Status} — không thể từ chối.");

        item.Status = DealerOrderStatus.Rejected;
        item.RejectReason = dto.Reason.Trim();
        await db.SaveChangesAsync();

        return new
        {
            item.OrderNo,
            status = item.Status.ToString(),
            item.RejectReason
        };
    }

    public async Task<object?> CancelAsync(string orderNo, CancelDealerOrderDto? dto)
    {
        var item = await GetByNo(orderNo);
        if (item is null) return null;

        if (item.Status == DealerOrderStatus.Approved2)
            throw new InvalidOperationException($"Đơn hàng {item.OrderNo} đã chốt duyệt cấp 2 — không thể hủy.");
        if (item.Status == DealerOrderStatus.Cancelled)
            throw new InvalidOperationException($"Đơn hàng {item.OrderNo} đã bị hủy trước đó.");

        item.Status = DealerOrderStatus.Cancelled;
        item.CancelledAt = DateTime.Now;
        if (!string.IsNullOrWhiteSpace(dto?.Reason))
            item.Remark = string.IsNullOrWhiteSpace(item.Remark)
                ? $"[Hủy: {dto.Reason.Trim()}]"
                : $"{item.Remark} [Hủy: {dto.Reason.Trim()}]";

        await db.SaveChangesAsync();

        return new
        {
            item.OrderNo,
            status = item.Status.ToString(),
            item.CancelledAt,
            item.Remark
        };
    }

    public async Task<object> StatsAsync()
    {
        var q = db.DealerOrders.Where(x => x.OrgId == Org);
        return new
        {
            total = await q.CountAsync(),
            pending = await q.CountAsync(x => x.Status == DealerOrderStatus.Pending),
            approved1 = await q.CountAsync(x => x.Status == DealerOrderStatus.Approved1),
            approved2 = await q.CountAsync(x => x.Status == DealerOrderStatus.Approved2),
            rejected = await q.CountAsync(x => x.Status == DealerOrderStatus.Rejected),
            cancelled = await q.CountAsync(x => x.Status == DealerOrderStatus.Cancelled),
            totalAmount = await q.Where(x => x.Status == DealerOrderStatus.Approved2).SumAsync(x => x.TotalAmount)
        };
    }
}
