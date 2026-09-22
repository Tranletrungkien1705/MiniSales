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

/// <summary>Loại đơn đặt hàng xe đại lý (DMS.Sales Ord_SalesOrderRoot SOType: P = Plan theo kế hoạch tháng, U = Unplan ngoài kế hoạch).</summary>
public enum DealerOrderType { Plan = 0, Unplan = 1 }

/// <summary>Trạng thái đơn đặt hàng xe đại lý (DMS.Sales Ord_SalesOrderRoot: P=Pending -> A1=Approved1 -> A2=Approved2/Finished, R=Rejected, C=Cancelled).</summary>
public enum DealerOrderStatus { Pending = 0, Approved1 = 1, Approved2 = 2, Rejected = 3, Cancelled = 4 }

/// <summary>Đơn đặt hàng xe của Đại lý gửi Nhà phân phối (DMS.Sales Ord_SalesOrderRoot).</summary>
public sealed class DealerOrder
{
    public long Id { get; set; }
    public Guid OrgId { get; set; }
    public string OrderNo { get; set; } = "";
    public string DealerCode { get; set; } = "";
    public string DealerName { get; set; } = "";
    public DealerOrderType OrderType { get; set; } = DealerOrderType.Plan;
    public string OrderMonth { get; set; } = ""; // yyyy-MM
    public string? SalesPolicyCode { get; set; } // CSC / Standard
    public DealerOrderStatus Status { get; set; } = DealerOrderStatus.Pending;
    public int TotalQuantity { get; set; }
    public decimal TotalAmount { get; set; }
    public string? Remark { get; set; }
    public string? RejectReason { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime? Approved1At { get; set; }
    public DateTime? Approved2At { get; set; }
    public DateTime? CancelledAt { get; set; }

    public List<DealerOrderItem> Items { get; set; } = new();
}

/// <summary>Chi tiết dòng xe trong đơn đặt hàng đại lý (DMS.Sales DMS40_Ord_SalesOrderRootDetail).</summary>
public sealed class DealerOrderItem
{
    public long Id { get; set; }
    public long OrderId { get; set; }
    public string Model { get; set; } = "";
    public string? SpecCode { get; set; }
    public string? ColorCode { get; set; }
    public int RequestedQuantity { get; set; }
    public int ApprovedQuantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TotalAmount { get; set; }
    public string? Remark { get; set; }
}

/// <summary>Loại bảo lãnh (DMS.Sales Pmt_Guarantee GuaranteeType: BL = Bảo lãnh thanh toán, LC = Thư tín dụng Letter of Credit).</summary>
public enum GuaranteeType { BL = 0, LC = 1 }

/// <summary>Trạng thái bảo lãnh ngân hàng (DMS.Sales Pmt_Guarantee GuaranteeStatus: Pending -> Approved / Rejected / Cancelled).</summary>
public enum GuaranteeStatus { Pending = 0, Approved = 1, Rejected = 2, Cancelled = 3 }

/// <summary>Bảo lãnh thanh toán ngân hàng cho Đại lý mua xe từ Nhà phân phối (DMS.Sales Pmt_Guarantee).</summary>
public sealed class PaymentGuarantee
{
    public long Id { get; set; }
    public Guid OrgId { get; set; }
    public string GuaranteeNo { get; set; } = "";
    public string BankGuaranteeNo { get; set; } = "";
    public string BankCode { get; set; } = "";
    public string BankName { get; set; } = "";
    public string DealerCode { get; set; } = "";
    public string DealerName { get; set; } = "";
    public GuaranteeType GuaranteeType { get; set; } = GuaranteeType.BL;
    public GuaranteeStatus Status { get; set; } = GuaranteeStatus.Pending;
    public decimal TotalAmount { get; set; }
    public decimal AllocatedAmount { get; set; }
    public int Term { get; set; }
    public DateTime DateOpen { get; set; } = DateTime.Today;
    public DateTime DateExpired { get; set; }
    public decimal Fee { get; set; }
    public string? BankCodeMonitor { get; set; } // Ngân hàng giám sát / theo dõi giải ngân
    public DateTime? DateRecieveGrtRoot { get; set; } // Ngày nhận bản gốc bảo lãnh
    public string? Remark { get; set; }
    public string? RemarkReject { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime? ApprovedAt { get; set; }
    public DateTime? CancelledAt { get; set; }

    public List<PaymentGuaranteeDetail> Details { get; set; } = new();
}

/// <summary>Chi tiết xe / đơn hàng gắn bảo lãnh thanh toán (DMS.Sales Pmt_GuaranteeDetail).</summary>
public sealed class PaymentGuaranteeDetail
{
    public long Id { get; set; }
    public long GuaranteeId { get; set; }
    public string? Vin { get; set; }
    public string Model { get; set; } = "";
    public string? OrderNo { get; set; }
    public decimal GuaranteeValue { get; set; }
    public int NumberOfDaysDeferredPayment { get; set; }
    public DateTime? DateStart { get; set; }
    public DateTime? DateEnd { get; set; }
    public string? Remark { get; set; }
}

/// <summary>Loại đề nghị giao/rút hồ sơ xe (DMS.Sales Car_DocReqList TypeCRR: Dealer = Đại lý rút hồ sơ giao khách, Normal = NPP bàn giao định kỳ, Special = Đề nghị hồ sơ đặc thù / thế chấp NH).</summary>
public enum DocReqType { Dealer = 0, Normal = 1, Special = 2 }

/// <summary>Trạng thái đề nghị giao/rút hồ sơ xe (DMS.Sales Car_DocReqList RequestStatus: Pending -> Approved1 (NPP duyệt danh sách) -> Approved2 (Đã bàn giao hồ sơ gốc), Rejected, Cancelled).</summary>
public enum DocReqStatus { Pending = 0, Approved1 = 1, Approved2 = 2, Rejected = 3, Cancelled = 4 }

/// <summary>Đề nghị giao hồ sơ xe / giải chấp ngân hàng (DMS.Sales Car_DocReqList): quản lý bàn giao hồ sơ gốc ô tô (CQ, hóa đơn gốc, tờ khai) cho đại lý/khách hàng.</summary>
public sealed class CarDocRequest
{
    public long Id { get; set; }
    public Guid OrgId { get; set; }
    public string DRListCode { get; set; } = "";
    public string DealerCode { get; set; } = "";
    public string DealerName { get; set; } = "";
    public DocReqType RequestType { get; set; } = DocReqType.Dealer;
    public DocReqStatus Status { get; set; } = DocReqStatus.Pending;
    public string? LetterRepresentationNo { get; set; } // Số công văn / tờ trình ủy quyền
    public DateTime? LetterRepresentationDate { get; set; } // Ngày công văn ủy quyền
    public int LoanSupportDay { get; set; } // Số ngày hỗ trợ vay vốn
    public int TotalCars { get; set; }
    public string? Remark { get; set; }
    public string? RejectReason { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime? Approved1At { get; set; }
    public DateTime? Approved2At { get; set; }
    public DateTime? CancelledAt { get; set; }

    public List<CarDocRequestDetail> Details { get; set; } = new();
}

/// <summary>Chi tiết xe ô tô trong đề nghị giao hồ sơ (DMS.Sales Car_DocReqDtl).</summary>
public sealed class CarDocRequestDetail
{
    public long Id { get; set; }
    public long RequestId { get; set; }
    public string Vin { get; set; } = "";
    public string Model { get; set; } = "";
    public string? EngineNo { get; set; }
    public string? OrderNo { get; set; }
    public string? DeliveryOrderNo { get; set; }
    public string? BankCode { get; set; }
    public string? BankGuaranteeNo { get; set; }
    public string DocumentsStatus { get; set; } = "Full"; // Full | MissingCQ | WaitBankRelease
    public DateTime? ExpectedDate { get; set; }
    public DateTime? HandoverDate { get; set; } // DRFullDocDate - Ngày bàn giao đầy đủ hồ sơ gốc
    public string? RecipientName { get; set; }
    public string? RecipientPhone { get; set; }
    public string? Remark { get; set; }
}


