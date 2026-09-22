using Microsoft.EntityFrameworkCore;
using MiniSales.Data;
using MiniSales.Models;

namespace MiniSales.Services;

public record CreateDeliveryOrderDto(
    string OrderCode,
    string RecipientName,
    string RecipientPhone,
    string DeliveryAddress,
    string? TransporterName,
    string? PlateNo,
    DateTime? DeliveryExpectedDate,
    string? Remark
);

public record CompleteDeliveryOrderDto(
    string? HandoverNote,
    DateTime? ActualDeliveredAt
);

public record CancelDeliveryOrderDto(
    string? Reason
);

public interface IDeliveryOrderService
{
    Task<object> CreateAsync(CreateDeliveryOrderDto dto);
    Task<object> ListAsync(string? status, string? dealer, string? orderCode);
    Task<object?> DetailAsync(string doNo);
    Task<object?> ApproveAsync(string doNo);
    Task<object?> CompleteAsync(string doNo, CompleteDeliveryOrderDto dto);
    Task<object?> CancelAsync(string doNo, string? reason);
    Task<object> StatsAsync();
}

public sealed class DeliveryOrderService(AppDbContext db, ITenantContext tenant) : IDeliveryOrderService
{
    private Guid Org => tenant.OrgId;

    public async Task<object> CreateAsync(CreateDeliveryOrderDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.OrderCode))
            throw new InvalidOperationException("OrderCode không được để trống.");
        if (string.IsNullOrWhiteSpace(dto.RecipientName))
            throw new InvalidOperationException("RecipientName (tên người nhận/tài xế) không được để trống.");
        if (string.IsNullOrWhiteSpace(dto.RecipientPhone))
            throw new InvalidOperationException("RecipientPhone (SĐT người nhận) không được để trống.");
        if (string.IsNullOrWhiteSpace(dto.DeliveryAddress))
            throw new InvalidOperationException("DeliveryAddress (địa điểm giao/kho xuất) không được để trống.");

        var orderCode = dto.OrderCode.Trim().ToUpperInvariant();
        var order = await db.Orders.FirstOrDefaultAsync(x => x.OrgId == Org && x.Code == orderCode);
        if (order is null)
            throw new InvalidOperationException($"Không tìm thấy hợp đồng bán xe mã '{dto.OrderCode}'.");

        if (order.Status == SalesStatus.Cancelled)
            throw new InvalidOperationException("Hợp đồng đã bị hủy — không thể lập lệnh giao xe.");
        if (order.Status == SalesStatus.Delivered)
            throw new InvalidOperationException("Hợp đồng đã hoàn thành giao xe trước đó.");
        if (order.Status == SalesStatus.Draft)
            throw new InvalidOperationException("Hợp đồng chưa đặt cọc hoặc thanh toán — không thể lập lệnh giao xe.");
        if (string.IsNullOrWhiteSpace(order.Vin))
            throw new InvalidOperationException("Hợp đồng chưa được gán số khung VIN — không thể lập lệnh giao xe.");

        // Kiểm tra xem đã có lệnh giao đang mở cho hợp đồng này chưa
        var hasActive = await db.DeliveryOrders.AnyAsync(x =>
            x.OrgId == Org && x.OrderId == order.Id &&
            (x.Status == DeliveryOrderStatus.Pending || x.Status == DeliveryOrderStatus.Approved));
        if (hasActive)
            throw new InvalidOperationException($"Hợp đồng {order.Code} đã có lệnh giao xe đang chờ xử lý.");

        var doNo = "DO" + DateTime.Now.ToString("yyMMddHHmmss");
        var item = new DeliveryOrder
        {
            OrgId = Org,
            DeliveryOrderNo = doNo,
            OrderId = order.Id,
            OrderCode = order.Code,
            DealerCode = order.DealerCode,
            CustomerName = order.CustomerName,
            Phone = order.Phone,
            Vin = order.Vin,
            Model = order.Model,
            RecipientName = dto.RecipientName.Trim(),
            RecipientPhone = dto.RecipientPhone.Trim(),
            DeliveryAddress = dto.DeliveryAddress.Trim(),
            TransporterName = dto.TransporterName?.Trim(),
            PlateNo = dto.PlateNo?.Trim().ToUpperInvariant(),
            DeliveryExpectedDate = dto.DeliveryExpectedDate,
            Status = DeliveryOrderStatus.Pending,
            Remark = dto.Remark?.Trim(),
            CreatedAt = DateTime.Now
        };

        db.DeliveryOrders.Add(item);
        await db.SaveChangesAsync();

        return new
        {
            deliveryOrderNo = item.DeliveryOrderNo,
            orderCode = item.OrderCode,
            customerName = item.CustomerName,
            vin = item.Vin,
            model = item.Model,
            recipientName = item.RecipientName,
            deliveryAddress = item.DeliveryAddress,
            status = item.Status.ToString(),
            createdAt = item.CreatedAt
        };
    }

    public async Task<object> ListAsync(string? status, string? dealer, string? orderCode)
    {
        var q = db.DeliveryOrders.Where(x => x.OrgId == Org);
        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<DeliveryOrderStatus>(status, true, out var st))
            q = q.Where(x => x.Status == st);
        if (!string.IsNullOrWhiteSpace(dealer))
            q = q.Where(x => x.DealerCode == dealer.Trim());
        if (!string.IsNullOrWhiteSpace(orderCode))
        {
            var code = orderCode.Trim().ToUpperInvariant();
            q = q.Where(x => x.OrderCode == code);
        }

        var items = await q.OrderByDescending(x => x.Id).Take(500).Select(x => new
        {
            x.DeliveryOrderNo,
            x.OrderCode,
            x.DealerCode,
            x.CustomerName,
            x.Phone,
            x.Vin,
            x.Model,
            x.RecipientName,
            x.RecipientPhone,
            x.DeliveryAddress,
            x.TransporterName,
            x.PlateNo,
            status = x.Status.ToString(),
            x.DeliveryExpectedDate,
            x.DeliveredAt,
            x.CreatedAt,
            x.ApprovedAt
        }).ToListAsync();

        return new { count = items.Count, items };
    }

    private async Task<DeliveryOrder?> GetByNo(string doNo)
    {
        var code = doNo.Trim().ToUpperInvariant();
        return await db.DeliveryOrders.FirstOrDefaultAsync(x => x.OrgId == Org && x.DeliveryOrderNo == code);
    }

    public async Task<object?> DetailAsync(string doNo)
    {
        var item = await GetByNo(doNo);
        if (item is null) return null;

        var order = await db.Orders.FirstOrDefaultAsync(x => x.OrgId == Org && x.Id == item.OrderId);

        return new
        {
            item.DeliveryOrderNo,
            item.OrderCode,
            item.DealerCode,
            item.CustomerName,
            item.Phone,
            item.Vin,
            item.Model,
            item.RecipientName,
            item.RecipientPhone,
            item.DeliveryAddress,
            item.TransporterName,
            item.PlateNo,
            item.DeliveryExpectedDate,
            item.DeliveredAt,
            status = item.Status.ToString(),
            item.Remark,
            item.HandoverNote,
            item.CreatedAt,
            item.ApprovedAt,
            item.CancelledAt,
            orderInfo = order is null ? null : new
            {
                order.NetPrice,
                order.Paid,
                outstanding = order.NetPrice - order.Paid,
                orderStatus = order.Status.ToString()
            }
        };
    }

    public async Task<object?> ApproveAsync(string doNo)
    {
        var item = await GetByNo(doNo);
        if (item is null) return null;

        if (item.Status != DeliveryOrderStatus.Pending)
            throw new InvalidOperationException($"Lệnh giao xe {item.DeliveryOrderNo} không ở trạng thái Chờ duyệt (hiện tại: {item.Status}).");

        item.Status = DeliveryOrderStatus.Approved;
        item.ApprovedAt = DateTime.Now;
        await db.SaveChangesAsync();

        return new
        {
            item.DeliveryOrderNo,
            item.OrderCode,
            status = item.Status.ToString(),
            item.ApprovedAt
        };
    }

    public async Task<object?> CompleteAsync(string doNo, CompleteDeliveryOrderDto dto)
    {
        var item = await GetByNo(doNo);
        if (item is null) return null;

        if (item.Status != DeliveryOrderStatus.Approved)
            throw new InvalidOperationException($"Lệnh giao xe {item.DeliveryOrderNo} phải được Duyệt (Approved) trước khi bàn giao xe (hiện tại: {item.Status}).");

        var order = await db.Orders.FirstOrDefaultAsync(x => x.OrgId == Org && x.Id == item.OrderId);
        if (order is null)
            throw new InvalidOperationException("Không tìm thấy hợp đồng bán xe liên kết.");

        // Kiểm tra hoàn thành nghĩa vụ tài chính trước khi xuất bàn giao xe theo chuẩn DMS.Sales
        if (order.Status != SalesStatus.Paid && order.Paid < order.NetPrice)
            throw new InvalidOperationException($"Hợp đồng {order.Code} chưa thanh toán đủ (đã nộp {order.Paid:N0}/{order.NetPrice:N0} đ) — chưa đủ điều kiện xuất kho bàn giao xe.");

        var deliveredTime = dto.ActualDeliveredAt ?? DateTime.Now;
        item.Status = DeliveryOrderStatus.Delivered;
        item.DeliveredAt = deliveredTime;
        item.HandoverNote = dto.HandoverNote?.Trim();

        // Đồng bộ cập nhật hoàn tất Hợp đồng bán xe
        order.Status = SalesStatus.Delivered;
        order.DeliveredAt = deliveredTime;

        await db.SaveChangesAsync();

        return new
        {
            item.DeliveryOrderNo,
            item.OrderCode,
            orderStatus = order.Status.ToString(),
            status = item.Status.ToString(),
            item.Vin,
            item.DeliveredAt,
            item.HandoverNote
        };
    }

    public async Task<object?> CancelAsync(string doNo, string? reason)
    {
        var item = await GetByNo(doNo);
        if (item is null) return null;

        if (item.Status == DeliveryOrderStatus.Delivered)
            throw new InvalidOperationException($"Lệnh giao xe {item.DeliveryOrderNo} đã hoàn thành bàn giao — không thể hủy.");
        if (item.Status == DeliveryOrderStatus.Cancelled)
            throw new InvalidOperationException($"Lệnh giao xe {item.DeliveryOrderNo} đã bị hủy trước đó.");

        item.Status = DeliveryOrderStatus.Cancelled;
        item.CancelledAt = DateTime.Now;
        if (!string.IsNullOrWhiteSpace(reason))
            item.Remark = string.IsNullOrWhiteSpace(item.Remark)
                ? $"[Hủy: {reason.Trim()}]"
                : $"{item.Remark} [Hủy: {reason.Trim()}]";

        await db.SaveChangesAsync();

        return new
        {
            item.DeliveryOrderNo,
            status = item.Status.ToString(),
            item.CancelledAt,
            item.Remark
        };
    }

    public async Task<object> StatsAsync()
    {
        var q = db.DeliveryOrders.Where(x => x.OrgId == Org);
        return new
        {
            total = await q.CountAsync(),
            pending = await q.CountAsync(x => x.Status == DeliveryOrderStatus.Pending),
            approved = await q.CountAsync(x => x.Status == DeliveryOrderStatus.Approved),
            delivered = await q.CountAsync(x => x.Status == DeliveryOrderStatus.Delivered),
            cancelled = await q.CountAsync(x => x.Status == DeliveryOrderStatus.Cancelled)
        };
    }
}
