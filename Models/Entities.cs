namespace MiniSales.Models;

public sealed class Org
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = "";
    public string ApiKey { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}

/// <summary>Hợp đồng bán xe (DMS.Sales): KH → đặt cọc → thanh toán đủ → giao.</summary>
public enum SalesStatus { Draft = 0, Deposited = 1, Paid = 2, Delivered = 3, Cancelled = 4 }

public sealed class SalesOrder
{
    public long Id { get; set; }
    public Guid OrgId { get; set; }
    public string Code { get; set; } = "";
    public string CustomerName { get; set; } = "";
    public string Phone { get; set; } = "";
    public string? Vin { get; set; }
    public string Model { get; set; } = "";
    public string DealerCode { get; set; } = "";
    public decimal ListPrice { get; set; }
    public decimal Discount { get; set; }
    public decimal NetPrice { get; set; }
    public decimal Paid { get; set; }
    public SalesStatus Status { get; set; } = SalesStatus.Draft;
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime? DeliveredAt { get; set; }
}

/// <summary>Phiếu thu (cọc / thanh toán) gắn hợp đồng.</summary>
public sealed class Payment
{
    public long Id { get; set; }
    public Guid OrgId { get; set; }
    public long OrderId { get; set; }
    public string Kind { get; set; } = "Deposit";   // Deposit | Payment
    public decimal Amount { get; set; }
    public string? RefNo { get; set; }               // mã giao dịch MiniPay
    public DateTime At { get; set; } = DateTime.Now;
}

/// <summary>Trạng thái lệnh xuất/giao xe (DMS.Sales Car_DeliveryOrder): Lập lệnh -> Duyệt xuất kho -> Bàn giao thành công.</summary>
public enum DeliveryOrderStatus { Pending = 0, Approved = 1, Delivered = 2, Cancelled = 3 }

/// <summary>Lệnh giao xe / Lệnh xuất xe (DMS.Sales Car_DeliveryOrder + Sto_DlvMinutes): quản lý xuất kho và bàn giao xe cho đại lý / khách hàng.</summary>
public sealed class DeliveryOrder
{
    public long Id { get; set; }
    public Guid OrgId { get; set; }
    public string DeliveryOrderNo { get; set; } = "";
    public long OrderId { get; set; }
    public string OrderCode { get; set; } = "";
    public string DealerCode { get; set; } = "";
    public string CustomerName { get; set; } = "";
    public string Phone { get; set; } = "";
    public string? Vin { get; set; }
    public string Model { get; set; } = "";
    public string RecipientName { get; set; } = "";
    public string RecipientPhone { get; set; } = "";
    public string DeliveryAddress { get; set; } = "";
    public string? TransporterName { get; set; }
    public string? PlateNo { get; set; }
    public DateTime? DeliveryExpectedDate { get; set; }
    public DateTime? DeliveredAt { get; set; }
    public DeliveryOrderStatus Status { get; set; } = DeliveryOrderStatus.Pending;
    public string? Remark { get; set; }
    public string? HandoverNote { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime? ApprovedAt { get; set; }
    public DateTime? CancelledAt { get; set; }
}
