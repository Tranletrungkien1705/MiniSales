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

/// <summary>Trạng thái hóa đơn VAT xuất đại lý (DMS.Sales VAT_HTCInvoice VatHTCStatus: Draft (P=Mới tạo/nháp) -> Issued (F=Đã phát hành điện tử) -> Cancelled (C=Đã hủy/thu hồi)).</summary>
public enum InvoiceStatus { Draft = 0, Issued = 1, Cancelled = 2 }

/// <summary>Loại hóa đơn (DMS.Sales VAT_HTCInvoice SourceInvoiceCode: Root = HĐ gốc INVOICEROOT, Adjust = HĐ điều chỉnh INVOICEADJ, Replace = HĐ thay thế INVOICEREPLACE).</summary>
public enum InvoiceType { Root = 0, Adjust = 1, Replace = 2 }

/// <summary>Kiểu điều chỉnh hóa đơn (DMS.Sales VAT_HTCInvoice InvoiceAdjType: Normal = Bình thường, Increase = Điều chỉnh tăng, Decrease = Điều chỉnh giảm).</summary>
public enum InvoiceAdjType { Normal = 0, Increase = 1, Decrease = 2 }

/// <summary>Hóa đơn VAT bán buôn xe ô tô NPP xuất cho Đại lý (DMS.Sales VAT_HTCInvoice): quản lý phát hành hóa đơn điện tử, điều chỉnh, thay thế và hủy thu hồi.</summary>
public sealed class CarInvoice
{
    public long Id { get; set; }
    public Guid OrgId { get; set; }
    public string InvoiceCode { get; set; } = ""; // Mã tham chiếu nội bộ (HTCInvoiceCode)
    public string? InvoiceNo { get; set; } // Số hóa đơn điện tử VAT (HTCInvoiceNo)
    public string InvoiceSerial { get; set; } = "1C26TBB"; // Ký hiệu mẫu hóa đơn (InvoiceIDCode)
    public string? LookupCode { get; set; } // Mã tra cứu hóa đơn điện tử (OS_HDDT_InvoiceCode / Qinvoice)
    public string DealerCode { get; set; } = "";
    public string DealerName { get; set; } = "";
    public string Issuer { get; set; } = "HTC"; // Pháp nhân xuất: HTC | HTCLD (FlagisHTC)
    public string BankCode { get; set; } = "DEALER"; // Mã ngân hàng bảo lãnh thanh toán (nếu có, hoặc "DEALER")
    public string? BankName { get; set; }
    public InvoiceType InvoiceType { get; set; } = InvoiceType.Root;
    public InvoiceAdjType AdjType { get; set; } = InvoiceAdjType.Normal;
    public string? RefInvoiceCode { get; set; } // Tham chiếu mã HĐ gốc khi là HĐ điều chỉnh hoặc thay thế
    public InvoiceStatus Status { get; set; } = InvoiceStatus.Draft;
    public decimal VatRate { get; set; } = 10m; // Thuế suất VAT chung (%)
    public decimal SubTotal { get; set; } // Cộng tiền hàng trước thuế (CongTien_Hang)
    public decimal VatAmount { get; set; } // Tiền thuế GTGT (CongTien_ThueGTGT)
    public decimal TotalAmount { get; set; } // Tổng tiền thanh toán gồm thuế (CongTien_TT)
    public int TotalCars { get; set; } // Tổng số lượng xe
    public string? BuyerTaxCode { get; set; } // MST đại lý mua hàng
    public string? BuyerAddress { get; set; } // Địa chỉ đại lý
    public string? Reason { get; set; } // Lý do điều chỉnh / thay thế / hủy (Adj_DeleteReason)
    public string? Remark { get; set; }
    public DateTime InvoiceDate { get; set; } = DateTime.Today;
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime? IssuedAt { get; set; } // Ngày phát hành điện tử
    public DateTime? CancelledAt { get; set; } // Ngày hủy / thu hồi

    public List<CarInvoiceDetail> Details { get; set; } = new();
}

/// <summary>Chi tiết dòng xe ô tô (VIN) trong hóa đơn VAT xuất đại lý (DMS.Sales VAT_HTCInvoiceDetail).</summary>
public sealed class CarInvoiceDetail
{
    public long Id { get; set; }
    public long InvoiceId { get; set; }
    public string Vin { get; set; } = "";
    public string Model { get; set; } = "";
    public string? SpecCode { get; set; }
    public string? ColorCode { get; set; }
    public string? EngineNo { get; set; }
    public decimal UnitPrice { get; set; } // Đơn giá xe trước VAT (DonGia)
    public decimal VatRate { get; set; } = 10m; // Thuế suất (%)
    public decimal VatAmount { get; set; } // Tiền thuế GTGT xe (TienThueGTGT)
    public decimal TotalAmount { get; set; } // Thành tiền gồm VAT (UnitPriceNew)
    public string? OrderNo { get; set; } // Số đơn đặt xe liên quan (OSO_SOCode)
    public string? DeliveryOrderNo { get; set; } // Số lệnh xuất xe liên quan
    public string? Remark { get; set; }
}

/// <summary>Trạng thái Lệnh thu hồi xe (DMS.Sales Sto_CarRetrieve RetrieveStatus: Pending (P=chờ duyệt) -> Approved (A=đã duyệt) / Rejected (R=từ chối) -> Completed (F=hoàn tất về kho), Cancelled (C=hủy)).</summary>
public enum CarRetrieveStatus { Pending = 0, Approved = 1, Rejected = 2, Completed = 3, Cancelled = 4 }

/// <summary>Trạng thái dòng xe trong lệnh thu hồi (DMS.Sales Sto_CarRetrieveDetail RetrieveDtlStatus: Pending -> Approved -> Completed / Rejected).</summary>
public enum CarRetrieveDtlStatus { Pending = 0, Approved = 1, Rejected = 2, Completed = 3 }

/// <summary>Lệnh thu hồi xe từ Đại lý về kho Nhà phân phối (DMS.Sales Sto_CarRetrieve): quản lý quy trình thu hồi xe do vi phạm bảo lãnh, đổi lô hoặc điều phối lại nguồn xe.</summary>
public sealed class CarRetrieveOrder
{
    public long Id { get; set; }
    public Guid OrgId { get; set; }
    public string RetrieveOrderNo { get; set; } = ""; // Số lệnh thu hồi (PK Sto_CarRetrieve)
    public string DealerCode { get; set; } = ""; // Mã đại lý bị thu hồi
    public string DealerName { get; set; } = ""; // Tên đại lý
    public CarRetrieveStatus Status { get; set; } = CarRetrieveStatus.Pending;
    public string StorageCode { get; set; } = "OTHER"; // Kho nhận xe thu hồi (mặc định OTHER)
    public string StorageName { get; set; } = "Kho Trung Tâm Phân Phối";
    public int TotalCars { get; set; } // Tổng số lượng xe thu hồi
    public string? Remark { get; set; } // Ghi chú lệnh
    public string? RejectReason { get; set; } // Lý do từ chối thu hồi
    public string? CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public string? ApprovedBy { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime? CancelledAt { get; set; }

    public List<CarRetrieveOrderDetail> Details { get; set; } = new();
}

/// <summary>Chi tiết dòng xe ô tô trong Lệnh thu hồi (DMS.Sales Sto_CarRetrieveDetail).</summary>
public sealed class CarRetrieveOrderDetail
{
    public long Id { get; set; }
    public long RetrieveOrderId { get; set; }
    public string CarId { get; set; } = ""; // Mã xe hệ thống
    public string Vin { get; set; } = ""; // Số VIN
    public string Model { get; set; } = ""; // Dòng xe / Model
    public string? DeliveryOrderNo { get; set; } // Số lệnh xuất xe đã giao trước đó
    public string StorageCode { get; set; } = "OTHER"; // Kho thu hồi
    public CarRetrieveDtlStatus Status { get; set; } = CarRetrieveDtlStatus.Pending;
    public DateTime? ExpectedStartDate { get; set; } // Ngày dự kiến bắt đầu thu hồi
    public DateTime? ExpectedEndDate { get; set; } // Ngày dự kiến kết thúc thu hồi
    public DateTime? ActualOutDate { get; set; } // Ngày xuất xe khỏi kho đại lý (RetrieveOutDate)
    public DateTime? ActualEndDate { get; set; } // Ngày nhập xe vào kho NPP (RetrieveEndDate)
    public string? EngineNo { get; set; } // Số máy
    public string? Color { get; set; } // Màu xe
    public string? Remark { get; set; } // Ghi chú dòng xe
}

/// <summary>Trạng thái Đề nghị hủy hợp đồng bán xe (DMS.Sales Dlr_ContractCancel ContractCancelStatus: Pending (P=chờ duyệt) -> Approved (A=đã duyệt hủy) / Rejected (R=từ chối) / Cancelled (C=hủy đề nghị)).</summary>
public enum ContractCancelStatus { Pending = 0, Approved = 1, Rejected = 2, Cancelled = 3 }

/// <summary>Đề nghị hủy hợp đồng bán lẻ xe ô tô từ Đại lý gửi Nhà phân phối (DMS.Sales Dlr_ContractCancel): quản lý quy trình hủy hợp đồng/hủy xe do đổi màu, không vay được NH hoặc dừng mua.</summary>
public sealed class ContractCancelOrder
{
    public long Id { get; set; }
    public Guid OrgId { get; set; }
    public string ContractCNo { get; set; } = ""; // Số đề nghị hủy (PK Dlr_ContractCancel)
    public string DealerCode { get; set; } = ""; // Mã đại lý
    public string DealerName { get; set; } = ""; // Tên đại lý
    public ContractCancelStatus Status { get; set; } = ContractCancelStatus.Pending;
    public int TotalCars { get; set; } // Tổng số lượng xe đề nghị hủy
    public string? Remark { get; set; } // Ghi chú chung
    public string? RejectReason { get; set; } // Lý do từ chối duyệt
    public string? CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public string? ApprovedBy { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public DateTime? CancelledAt { get; set; }

    public List<ContractCancelCar> Cars { get; set; } = new();
    public List<ContractCancelDtl> Details { get; set; } = new();
}

/// <summary>Chi tiết từng xe cần hủy trong hợp đồng bán lẻ (DMS.Sales Dlr_ContractCancelCar - đơn vị hủy cốt lõi theo xe).</summary>
public sealed class ContractCancelCar
{
    public long Id { get; set; }
    public long ContractCancelId { get; set; }
    public string OrderCode { get; set; } = ""; // Số HĐ bán lẻ (DlrContractNo)
    public long? OrderId { get; set; } // ID hợp đồng bán xe
    public string? Vin { get; set; } // Số VIN xe
    public string Model { get; set; } = ""; // Model xe
    public string? SpecCode { get; set; } // Mã spec / cấu hình
    public string? ColorCode { get; set; } // Mã màu
    public string CtrCTDNo { get; set; } = ""; // Mã lý do hủy (Mst_CtrCancelTypeDtl: RC1.PRICE, RC2.COLOR, RC3.STOPBUY, RC4.LOANFAIL, RC5.OTHER)
    public string CtrCTDName { get; set; } = ""; // Tên lý do hủy
    public string CtrCType { get; set; } = ""; // Nhóm lý do hủy (RC1..RC5)
    public string? Remark { get; set; } // Ghi chú chi tiết cho xe (bắt buộc khi nhóm RC5)
}

/// <summary>Bảng gom cấu hình xe hủy (DMS.Sales Dlr_ContractCancelDtl - server tự dẫn xuất từ danh sách xe, Qty = đếm xe).</summary>
public sealed class ContractCancelDtl
{
    public long Id { get; set; }
    public long ContractCancelId { get; set; }
    public string OrderCode { get; set; } = ""; // Số HĐ bán lẻ
    public string Model { get; set; } = ""; // Model xe
    public string? SpecCode { get; set; }
    public string? ColorCode { get; set; }
    public int Qty { get; set; } // Số lượng xe gom theo cấu hình
    public string? Remark { get; set; }
}

/// <summary>Trạng thái Biên bản bàn giao xe (DMS.Sales Car_TransportMinutes TransportMinutesStatus: Pending (P=Chờ ký duyệt) -> DealerApproved (A1=Đại lý đã ký nhận) -> Completed (F=NPP duyệt hoàn tất bàn giao), Cancelled (C=Đã hủy)).</summary>
public enum TransportMinutesStatus { Pending = 0, DealerApproved = 1, Completed = 2, Cancelled = 3 }

/// <summary>Trạng thái Đại lý ký nhận xe (DMS.Sales Car_TransportMinutes DLTransportMinutesStatus: Pending -> Approved).</summary>
public enum DLSignStatus { Pending = 0, Approved = 1 }

/// <summary>Trạng thái NPP xác nhận hoàn tất bàn giao (DMS.Sales Car_TransportMinutes HTCTransportMinutesStatus: Pending -> Approved).</summary>
public enum HTCSignStatus { Pending = 0, Approved = 1 }

/// <summary>Trạng thái dòng xe trong biên bản bàn giao (DMS.Sales Car_TransportMinutesDetail TransportMinutesDtlStatus: Pending -> Approved / Rejected).</summary>
public enum TransportMinutesCarStatus { Pending = 0, Approved = 1, Rejected = 2 }

/// <summary>Biên bản bàn giao xe / Biên bản giao nhận xe (DMS.Sales Car_TransportMinutes - BBBG/BBGN): quản lý bàn giao thực tế lô xe giữa NPP, Đại lý, Đơn vị vận chuyển và Ngân hàng.</summary>
public sealed class CarTransportMinutes
{
    public long Id { get; set; }
    public Guid OrgId { get; set; }
    public string TransportMinutesNo { get; set; } = ""; // Số biên bản BBBG (PK Car_TransportMinutes)
    public string DealerCode { get; set; } = ""; // Mã đại lý
    public string DealerName { get; set; } = ""; // Tên đại lý
    public string? TransporterName { get; set; } // Nhà vận chuyển / Đội xe
    public string? PlateNo { get; set; } // Biển số xe chở xe
    public string? DriverName { get; set; } // Lái xe bàn giao
    public string? DriverPhone { get; set; } // SĐT lái xe
    public string? BankCode { get; set; } // Ngân hàng tài trợ bảo lãnh (nếu có)
    public string? BankName { get; set; }
    public DateTime TransportMinutesDate { get; set; } = DateTime.Today; // Ngày lập biên bản
    public TransportMinutesStatus Status { get; set; } = TransportMinutesStatus.Pending;
    public DLSignStatus DLSignStatus { get; set; } = DLSignStatus.Pending;
    public HTCSignStatus HTCSignStatus { get; set; } = HTCSignStatus.Pending;
    public int TotalCars { get; set; } // Tổng số xe trong biên bản
    public string? DLCreatedBy { get; set; }
    public DateTime DLCreatedAt { get; set; } = DateTime.Now;
    public string? DLApprBy { get; set; }
    public DateTime? DLApprAt { get; set; }
    public string? HTCApprBy { get; set; }
    public DateTime? HTCApprAt { get; set; }
    public string? HTCCancelBy { get; set; }
    public DateTime? HTCCancelAt { get; set; }
    public string? CancelReason { get; set; }
    public string? FilePath { get; set; } // Đường dẫn file biên bản scan/ký số E-Sign
    public string? Remark { get; set; }

    public List<CarTransportMinutesDetail> Cars { get; set; } = new();
}

/// <summary>Chi tiết dòng xe ô tô trong Biên bản bàn giao (DMS.Sales Car_TransportMinutesDetail).</summary>
public sealed class CarTransportMinutesDetail
{
    public long Id { get; set; }
    public long TransportMinutesId { get; set; }
    public string CarId { get; set; } = ""; // Mã định danh xe hệ thống
    public string Vin { get; set; } = ""; // Số VIN (17 ký tự)
    public string Model { get; set; } = ""; // Dòng xe / Model
    public string? SpecCode { get; set; } // Mã cấu hình
    public string? ColorCode { get; set; } // Màu xe
    public string? EngineNo { get; set; } // Số máy
    public string? DeliveryOrderNo { get; set; } // Số lệnh xuất xe DO liên quan
    public string? OrderNo { get; set; } // Số đơn hàng / Hợp đồng bán lẻ
    public string? GuaranteeNo { get; set; } // Số bảo lãnh ngân hàng
    public int Odometer { get; set; } = 10; // Số km đồng hồ khi bàn giao
    public string? ConditionNote { get; set; } // Tình trạng xe (2 chìa, phụ kiện, lốp phụ, nguyên vẹn)
    public TransportMinutesCarStatus Status { get; set; } = TransportMinutesCarStatus.Pending;
    public string? Remark { get; set; }
}

/// <summary>Trạng thái Hợp đồng mua bán buôn xe Đại lý - NPP (DMS.Sales DMS40_CT_DealerContract DlrCtrStatus: Draft (NS=NotSign/Mới lập) -> PreliminaryApproved (A1=NPP duyệt sơ bộ) -> Signed (S=Đã ký 2 bên, hiệu lực) -> Adjusted (AJ=Đã bị điều chỉnh), Cancelled (C=Đã hủy)).</summary>
public enum DealerContractStatus { Draft = 0, PreliminaryApproved = 1, Signed = 2, Adjusted = 3, Cancelled = 4 }

/// <summary>Loại hợp đồng mua bán buôn xe (DMS.Sales DMS40_CT_DealerContract FlagDlrCtrAdjust: Standard = Hợp đồng chuẩn, Adjust = Hợp đồng điều chỉnh từ HĐ gốc).</summary>
public enum DealerContractType { Standard = 0, Adjust = 1 }

/// <summary>Phương thức thanh toán hợp đồng bán buôn xe đại lý (DMS.Sales DMS40_CT_DealerContract DCPType: CASH = Tiền mặt, BL = Bảo lãnh thanh toán, LC = Thư tín dụng, UPAS = Thư tín dụng UPAS LC).</summary>
public enum DealerContractPaymentType { Cash = 0, Guarantee = 1, LC = 2, UpasLC = 3 }

/// <summary>Trạng thái ký số hợp đồng từng bên (DMS.Sales DMS40_CT_DealerContract DlrSignStatus / HTCSignStatus: Pending -> Signed).</summary>
public enum DealerContractSignStatus { Pending = 0, Signed = 1 }

/// <summary>Hợp đồng mua bán buôn xe ô tô giữa Nhà phân phối và Đại lý (DMS.Sales DMS40_CT_DealerContract / CT_DealerContract): thỏa thuận mua bán lô xe ô tô, điều khoản thanh toán, bảo lãnh ngân hàng và quy trình ký số 2 bên.</summary>
public sealed class DealerContract
{
    public long Id { get; set; }
    public Guid OrgId { get; set; }
    public string ContractNo { get; set; } = ""; // Số hợp đồng (PK DlrCtrNo, vd: 2609DRC00001)
    public string DealerCode { get; set; } = ""; // Mã đại lý mua xe
    public string DealerName { get; set; } = ""; // Tên đại lý mua xe
    public DateTime ContractDate { get; set; } = DateTime.Today; // Ngày hợp đồng
    public DealerContractType ContractType { get; set; } = DealerContractType.Standard;
    public string? ParentContractNo { get; set; } // Số HĐ gốc nếu là HĐ điều chỉnh (DlrCtrNoParent)
    public DealerContractPaymentType PaymentType { get; set; } = DealerContractPaymentType.Cash;
    public string? BankCode { get; set; } // Mã ngân hàng bảo lãnh / tài trợ (BankCodeMD: BIDV, VCB, VPB...)
    public string? BankName { get; set; } // Tên ngân hàng bảo lãnh (BankNameMD)
    public decimal DepositPercent { get; set; } = 10m; // Tỷ lệ đặt cọc (%)
    public int GuaranteeDays { get; set; } = 15; // Thời hạn thanh toán bảo lãnh (ngày)
    public int TotalCars { get; set; } // Tổng số lượng xe trong hợp đồng
    public decimal TotalAmount { get; set; } // Tổng giá trị hợp đồng gồm VAT
    public DealerContractStatus Status { get; set; } = DealerContractStatus.Draft;
    public DealerContractSignStatus DealerSignStatus { get; set; } = DealerContractSignStatus.Pending;
    public DealerContractSignStatus HQSignStatus { get; set; } = DealerContractSignStatus.Pending;
    public string? DealerSignedBy { get; set; }
    public DateTime? DealerSignedAt { get; set; }
    public string? HQSignedBy { get; set; }
    public DateTime? HQSignedAt { get; set; }
    public DateTime? Approved1At { get; set; } // Thời điểm NPP thẩm tra & duyệt sơ bộ
    public string? RejectReason { get; set; } // Lý do từ chối duyệt (RejectHQ)
    public string? CancelReason { get; set; } // Lý do hủy hợp đồng (CancelDL)
    public string? SignedFilePath { get; set; } // File hợp đồng scan/ký điện tử (FileName / FileUrl)
    public string? Remark { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime? CancelledAt { get; set; }

    public List<DealerContractDetail> Details { get; set; } = new();
}

/// <summary>Chi tiết dòng xe ô tô trong Hợp đồng mua bán buôn đại lý (DMS.Sales DMS40_CT_DealerContractDetail).</summary>
public sealed class DealerContractDetail
{
    public long Id { get; set; }
    public long ContractId { get; set; }
    public string CarId { get; set; } = ""; // Mã định danh xe hệ thống
    public string Vin { get; set; } = ""; // Số khung xe (OriginNo / VIN)
    public string Model { get; set; } = ""; // Dòng xe / Model
    public string? SpecCode { get; set; } // Cấu hình / Spec
    public string? ColorCode { get; set; } // Mã màu xe
    public int ProductionYear { get; set; } = DateTime.Today.Year; // Năm sản xuất
    public decimal UnitPrice { get; set; } // Đơn giá xe trước VAT
    public decimal VatRate { get; set; } = 10m; // Thuế suất VAT (%)
    public decimal TotalAmount { get; set; } // Thành tiền gồm VAT
    public string? OrderNo { get; set; } // Số đơn đặt hàng đại lý (SOCode)
    public string? Remark { get; set; }
}

/// <summary>Loại khách hàng mua xe bán lẻ (DMS.Sales DLS_DealerCustomer CustomerType: Individual = Cá nhân, Corporate = Doanh nghiệp/Tổ chức).</summary>
public enum RetailCustomerType { Individual = 0, Corporate = 1 }

/// <summary>Phương thức thanh toán hợp đồng bán lẻ xe (DMS.Sales Dlr_Contract PmtType: Cash = 1 [Trả thẳng], Installment = 2 [Trả góp ngân hàng]).</summary>
public enum RetailPaymentType { Cash = 0, Installment = 1 }

/// <summary>Trạng thái hợp đồng bán lẻ xe ô tô (DMS.Sales Dlr_Contract DlrCtrStatus: Pending = 'P' [Chờ duyệt/Đang lập], Approved = 'A' [Đã duyệt, hiệu lực], Finished = 'F' [Hoàn thành giao đủ xe], Cancelled = 'C' [Đã hủy]).</summary>
public enum RetailContractStatus { Pending = 0, Approved = 1, Finished = 2, Cancelled = 3 }

/// <summary>Trạng thái từng xe trong hợp đồng bán lẻ (DMS.Sales Dlr_ContractCar Status: Pending = Chờ giao, Delivered = Đã bàn giao, Cancelled = Đã hủy).</summary>
public enum RetailContractCarStatus { Pending = 0, Delivered = 1, Cancelled = 2 }

/// <summary>Hợp đồng bán lẻ xe ô tô giữa Đại lý và Khách hàng (DMS.Sales Dlr_Contract / DlrContractController / DEALER_RETAIL_CONTRACT_FLOW.md): quản lý ký kết bán lẻ, thông tin khách hàng, tư vấn bán hàng, phương thức thanh toán trả thẳng/trả góp, lịch sử phiên bản chi tiết và tiến độ bàn giao xe.</summary>
public sealed class DlrRetailContract
{
    public long Id { get; set; }
    public Guid OrgId { get; set; }
    public string ContractNo { get; set; } = ""; // Số HĐ bán lẻ hệ thống (PK DlrContractNo, vd: RC2026-0001)
    public string ContractNoUser { get; set; } = ""; // Số HĐ do đại lý tự nhập (DlrContractNoUser)
    public string DealerCode { get; set; } = ""; // Mã đại lý
    public string DealerName { get; set; } = ""; // Tên đại lý
    public string? DealerCodeBuyer { get; set; } // Mã đại lý mua nếu là bán ngang (cross-sell)
    public string CustomerCode { get; set; } = ""; // Mã khách hàng (DLS_DealerCustomer)
    public string CustomerName { get; set; } = ""; // Tên khách hàng sở hữu
    public string CustomerPhone { get; set; } = ""; // SĐT khách hàng
    public string CustomerIdCardType { get; set; } = "CCCD"; // CMND | CCCD | Hộ chiếu | MST
    public string CustomerIdCardNo { get; set; } = ""; // Số CMND/CCCD/MST
    public DateTime? CustomerDateOfBirth { get; set; } // Ngày sinh
    public string? CustomerAddress { get; set; } // Địa chỉ
    public RetailCustomerType CustomerType { get; set; } = RetailCustomerType.Individual;
    public string? TransactorCode { get; set; } // Mã người giao dịch đại diện
    public string? TransactorName { get; set; } // Tên người giao dịch
    public string? TransactorPhone { get; set; } // SĐT người giao dịch
    public string? DriverCode { get; set; } // Người lái xe (CustomerCode_Driver)
    public string? DealerSaleCode { get; set; } // Điểm bán hàng / Showroom (DealerSaleCode)
    public string SalesType { get; set; } = "RETAIL"; // RETAIL | FLEET | STAFF | CROSS
    public string SMCode { get; set; } = ""; // Mã nhân viên tư vấn bán hàng (SMCode)
    public string? SMName { get; set; } // Tên nhân viên tư vấn bán hàng
    public DateTime ContractDate { get; set; } = DateTime.Today; // Ngày ký HĐ
    public RetailPaymentType PaymentType { get; set; } = RetailPaymentType.Cash;
    public string? BankCode { get; set; } // Mã ngân hàng tài trợ trả góp (Bắt buộc khi PaymentType = Installment)
    public string? BankName { get; set; } // Tên ngân hàng tài trợ
    public decimal BankLoanAmount { get; set; } // Số tiền vay ngân hàng
    public decimal DepositAmount { get; set; } // Tiền đặt cọc
    public decimal PaidAmount { get; set; } // Tiền khách đã trả
    public int TotalCars { get; set; } // Tổng số xe
    public decimal TotalAmount { get; set; } // Tổng giá trị hợp đồng
    public RetailContractStatus Status { get; set; } = RetailContractStatus.Pending;
    public bool FlagDealFinish { get; set; } // Cờ hoàn tất tất cả xe đã bàn giao (FlagDealFinish = "Y")
    public int VersionCount { get; set; } = 1; // Số phiên bản chi tiết (tăng khi sửa detail)
    public DateTime VersionDTimeCurr { get; set; } = DateTime.Now; // Thời điểm phiên bản hiện tại
    public DateTime? ApprovedAt { get; set; }
    public string? ApprovedBy { get; set; }
    public DateTime? CancelledAt { get; set; }
    public string? CancelledBy { get; set; }
    public string? CancelReason { get; set; }
    public string? Remark { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public List<DlrRetailContractDetail> Details { get; set; } = new();
    public List<DlrRetailContractCar> Cars { get; set; } = new();
    public List<DlrRetailContractDtlHis> Histories { get; set; } = new();
}

/// <summary>Chi tiết dòng cấu hình xe trong hợp đồng bán lẻ (DMS.Sales Dlr_ContractDtl - gom theo Spec/Model/Color).</summary>
public sealed class DlrRetailContractDetail
{
    public long Id { get; set; }
    public long ContractId { get; set; }
    public string ContractNo { get; set; } = "";
    public string Model { get; set; } = ""; // Tên dòng xe (SantaFe, Tucson, Accent...)
    public string? SpecCode { get; set; } // Cấu hình / Bản xe
    public string? ColorCode { get; set; } // Mã màu
    public int ProductionYear { get; set; } = DateTime.Today.Year;
    public int Qty { get; set; } // Số lượng xe đặt mua
    public int QtyDelivery { get; set; } // Số xe đã giao
    public int QtyCancel { get; set; } // Số xe đã hủy
    public decimal UnitPrice { get; set; } // Đơn giá niêm yết
    public decimal Discount { get; set; } // Giảm giá / Khuyến mại
    public decimal TotalAmount { get; set; } // Thành tiền = Qty * UnitPrice - Discount
    public DateTime? DlvExpectedDate { get; set; } // Ngày hẹn giao xe
    public string? Remark { get; set; }
}

/// <summary>Chi tiết từng xe cụ thể được nở dòng từ hợp đồng bán lẻ (DMS.Sales Dlr_ContractCar - mã CtrCarId theo dạng &lt;SốHĐ&gt;.01, .02... gắn VIN thực tế khi bàn giao).</summary>
public sealed class DlrRetailContractCar
{
    public long Id { get; set; }
    public long ContractId { get; set; }
    public string ContractNo { get; set; } = "";
    public string CtrCarId { get; set; } = ""; // Mã định danh xe HĐ: <ContractNo>.01, <ContractNo>.02
    public string Model { get; set; } = "";
    public string? SpecCode { get; set; }
    public string? ColorCode { get; set; }
    public string? Vin { get; set; } // Số VIN gán khi có xe từ kho
    public RetailContractCarStatus Status { get; set; } = RetailContractCarStatus.Pending;
    public DateTime? DeliveryDate { get; set; } // Ngày bàn giao thực tế
    public string? Remark { get; set; }
}

/// <summary>Lịch sử thay đổi các phiên bản chi tiết hợp đồng bán lẻ (DMS.Sales Dlr_ContractDtlHis - lưu vết mỗi lần sửa khi còn ở trạng thái Pending).</summary>
public sealed class DlrRetailContractDtlHis
{
    public long Id { get; set; }
    public long ContractId { get; set; }
    public string ContractNo { get; set; } = "";
    public int VersionCount { get; set; }
    public DateTime VersionDTimeCurr { get; set; } = DateTime.Now;
    public string Model { get; set; } = "";
    public string? SpecCode { get; set; }
    public string? ColorCode { get; set; }
    public int Qty { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TotalAmount { get; set; }
    public DateTime? DlvExpectedDate { get; set; }
    public string? Remark { get; set; }
    public string? LoggedBy { get; set; }
    public DateTime LogDateTime { get; set; } = DateTime.Now;
}

/// <summary>Loại thanh toán tiền xe đại lý - NPP (DMS.Sales Pmt_Payment PaymentType: PMT = Thanh toán cọc / thông thường, PMA = Thanh toán điều chỉnh, PMC = Tất toán giải phóng xe).</summary>
public enum WholesalePaymentType { PMT = 0, PMA = 1, PMC = 2 }

/// <summary>Phương thức thanh toán tiền xe (DMS.Sales Pmt_Payment PaymentMethod: CK = Chuyển khoản, TM = Tiền mặt, TMCK = Tiền mặt + CK, TTD = Thẻ tín dụng).</summary>
public enum WholesalePaymentMethod { BankTransfer = 0, Cash = 1, CashAndTransfer = 2, CreditCard = 3 }

/// <summary>Trạng thái phiếu thanh toán (DMS.Sales Pmt_Payment PaymentStatus: Pending = 'P' [Chờ duyệt], Approved = 'A' [NPP duyệt], Finished = 'F' [Kế toán hoàn tất hạch toán chứng từ ERP/SAP], Rejected = 'R' [Từ chối], Cancelled = 'C' [Hủy]).</summary>
public enum WholesalePaymentStatus { Pending = 0, Approved = 1, Finished = 2, Rejected = 3, Cancelled = 4 }

/// <summary>Chứng từ / Phiếu thanh toán tiền mua buôn xe ô tô Đại lý - NPP / Ủy nhiệm chi UNC (DMS.Sales Pmt_Payment): quản lý thanh toán cọc xe, thanh toán đợt theo hợp đồng bán buôn, giải ngân qua ngân hàng bảo lãnh, phê duyệt cấp NPP và xác nhận số chứng từ hạch toán kế toán.</summary>
public sealed class DealerPayment
{
    public long Id { get; set; }
    public Guid OrgId { get; set; }
    public string PaymentNo { get; set; } = ""; // Mã phiếu thanh toán / UNC (PK Pmt_Payment, vd: PMT2026-0001, UNC2609-0001)
    public string DealerCode { get; set; } = ""; // Mã đại lý nộp tiền
    public string DealerName { get; set; } = ""; // Tên đại lý
    public string? ContractNo { get; set; } // Số hợp đồng mua buôn liên quan (DlrCtrNo / DealerContract)
    public WholesalePaymentType PaymentType { get; set; } = WholesalePaymentType.PMT; // PMT | PMA | PMC
    public WholesalePaymentMethod PaymentMethod { get; set; } = WholesalePaymentMethod.BankTransfer; // CK | TM | TMCK | TTD
    public decimal TotalAmount { get; set; } // Tổng tiền thanh toán (VNĐ)
    public DateTime PaymentDate { get; set; } = DateTime.Today; // Ngày thanh toán
    public DateTime? PaymentDueDate { get; set; } // Ngày đến hạn thanh toán
    public DateTime? PaymentEndDate { get; set; } // Ngày kết thúc hiệu lực thanh toán / bảo lãnh
    public WholesalePaymentStatus Status { get; set; } = WholesalePaymentStatus.Pending; // P -> A -> F / R / C
    public string? BankPaymentNo { get; set; } // Số ủy nhiệm chi UNC / mã giao dịch ngân hàng
    public string? BankCodeSend { get; set; } // Mã ngân hàng chuyển tiền của đại lý
    public string? BankAccountSend { get; set; } // Số tài khoản trích nợ của đại lý
    public string? BankCodeReceive { get; set; } = "VCB"; // Mã ngân hàng thụ hưởng nhận tiền (NPP)
    public string? BankAccountReceive { get; set; } = "0011001234567"; // Số tài khoản nhận tiền của NPP
    public string? Funds { get; set; } = "0"; // Nguồn vốn: 0 = Vốn tự có, 1 = Vay ngân hàng giải ngân, 2 = Hạn mức tín dụng
    public string? BankLending { get; set; } // Ngân hàng tài trợ cho vay giải ngân
    public decimal? InterestRate { get; set; } // Lãi suất vay (%)
    public int? LoanPeriod { get; set; } // Kỳ hạn vay (tháng)
    public string? GuaranteeType { get; set; } // Loại bảo lãnh (BL, LC, UPAS) nếu thanh toán giải phóng bảo lãnh
    public decimal DepositPercent { get; set; } = 10m; // Tỷ lệ cọc áp dụng (%)
    public string? AccountingRecordNo { get; set; } // Số chứng từ kế toán ERP / SAP (bắt buộc khi kế toán hoàn tất)
    public DateTime? AccountingRecordDate { get; set; } // Ngày hạch toán kế toán
    public string? Remark { get; set; } // Ghi chú phiếu thanh toán
    public string? RejectReason { get; set; } // Lý do từ chối duyệt (RejectHQ/RejectDL)
    public string? CancelReason { get; set; } // Lý do hủy phiếu (CancelHQ)
    public string? CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public string? ApprovedBy { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public string? FinishedBy { get; set; }
    public DateTime? FinishedAt { get; set; }
    public string? CancelledBy { get; set; }
    public DateTime? CancelledAt { get; set; }

    public List<DealerPaymentDetail> Details { get; set; } = new();
}

/// <summary>Chi tiết dòng xe ô tô phân bổ thanh toán trong phiếu (DMS.Sales Pmt_PaymentDetail): phân bổ số tiền thanh toán cho từng xe / số khung VIN và liên kết bảo lãnh ngân hàng.</summary>
public sealed class DealerPaymentDetail
{
    public long Id { get; set; }
    public long PaymentId { get; set; }
    public string PaymentNo { get; set; } = "";
    public string CarId { get; set; } = ""; // Mã định danh xe hệ thống
    public string Vin { get; set; } = ""; // Số khung xe (17 ký tự)
    public string Model { get; set; } = ""; // Model dòng xe (SantaFe, Tucson, Accent, Creta...)
    public decimal Amount { get; set; } // Số tiền thanh toán phân bổ cho xe này (VNĐ)
    public string? GuaranteeNo { get; set; } // Mã chứng thư bảo lãnh ngân hàng liên quan (nếu có)
    public string? Remark { get; set; } // Ghi chú dòng xe
}

/// <summary>Trạng thái giao dịch bán lẻ xe (DMS.Sales DLS_Deal: Pending = 0 [Chờ kiểm chứng/giao xe], Verified = 1 [NPP/CSKH đã kiểm chứng], Delivered = 2 [Đã bàn giao xe], Cancelled = 3 [Đã hủy]).</summary>
public enum RetailDealStatus { Pending = 0, Verified = 1, Delivered = 2, Cancelled = 3 }

/// <summary>Trạng thái bàn giao từng xe trong giao dịch bán lẻ (DMS.Sales DLS_DealDetail DeliveryStatus: Pending = 0, Delivered = 1).</summary>
public enum RetailDealDeliveryStatus { Pending = 0, Delivered = 1 }

/// <summary>Giao dịch bán lẻ xe ô tô Đại lý - Khách hàng (DMS.Sales DLS_Deal / DlsDealController / DLS_DEAL_CREATE_FLOW.md): quản lý hoàn tất giao dịch bán lẻ ô tô, 3 vai trò khách hàng (Buyer/Holder/Driver), phương thức thanh toán trả thẳng/trả góp ngân hàng, kiểm tra PDI trước giao xe, kiểm chứng CSKH (CtmCareFlag), cập nhật biển số xe, hóa đơn xuất khách và bàn giao xe.</summary>
public sealed class RetailDeal
{
    public long Id { get; set; }
    public Guid OrgId { get; set; }
    public string DealNo { get; set; } = ""; // Số giao dịch hệ thống (PK DLS_Deal, vd: DEAL26090001)
    public string DealNoUser { get; set; } = ""; // Số giao dịch do đại lý tự đặt (DealNoUser)
    public string DealerCode { get; set; } = ""; // Mã đại lý bán
    public string DealerName { get; set; } = ""; // Tên đại lý bán
    public string? DealerCodeBuyer { get; set; } // Mã đại lý mua (nếu bán buôn ngang đại lý SalesType=F7)
    public string? DlrContractNo { get; set; } // Số hợp đồng bán lẻ liên kết (nếu tạo từ HĐBL, FlagInitDeal=Y)
    public string SalesType { get; set; } = "RETAIL"; // Kiểu bán lẻ: RETAIL, FLEET, STAFF, F7_CROSS_DEALER
    public RetailCustomerType CustomerType { get; set; } = RetailCustomerType.Individual; // Phân loại KH
    public DateTime DealDate { get; set; } = DateTime.Today; // Ngày giao dịch (DealDate <= Today)
    public string CustomerCodeBuyer { get; set; } = ""; // Khách hàng chủ sở hữu (Buyer)
    public string BuyerFullName { get; set; } = "";
    public string BuyerPhone { get; set; } = "";
    public string BuyerIdCardNo { get; set; } = "";
    public string? BuyerAddress { get; set; }
    public string? CustomerCodeHolder { get; set; } // Khách hàng đứng tên đăng ký xe (Holder, mặc định trùng Buyer)
    public string? HolderFullName { get; set; }
    public string? CustomerCodeDriver { get; set; } // Người lái xe chính (Driver, mặc định trùng Buyer)
    public string? DriverFullName { get; set; }
    public string SMCode { get; set; } = ""; // Tư vấn bán hàng (SMCode)
    public string? SMName { get; set; }
    public RetailPaymentType PaymentType { get; set; } = RetailPaymentType.Cash; // Phương thức thanh toán (Trả thẳng / Trả góp)
    public string? BankCode { get; set; } // Ngân hàng tài trợ (bắt buộc khi trả góp)
    public string? BankName { get; set; }
    public decimal BankLoanAmount { get; set; } // Số tiền vay ngân hàng
    public bool FlagPDI { get; set; } = true; // Cờ PDI: true = Đã PDI, false = Bỏ qua PDI (bắt buộc ReasonNotPDI)
    public string? ReasonNotPDI { get; set; } // Lý do không làm PDI
    public bool CtmCareFlag { get; set; } // Trạng thái kiểm chứng CSKH HQ ('1' = Đã kiểm chứng, khóa sửa)
    public DateTime? CtmCareUpdDate { get; set; } // Ngày kiểm chứng
    public string? CtmCareUpdBy { get; set; } // Người kiểm chứng
    public RetailDealStatus Status { get; set; } = RetailDealStatus.Pending;
    public int TotalCars { get; set; } // Tổng số xe
    public decimal TotalAmount { get; set; } // Tổng giá trị giao dịch
    public string? Remark { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public string? CancelledBy { get; set; }
    public DateTime? CancelledAt { get; set; }
    public string? CancelReason { get; set; }

    public List<RetailDealDetail> Details { get; set; } = new();
    public List<RetailDealAttach> Attachments { get; set; } = new();
}

/// <summary>Chi tiết xe trong giao dịch bán lẻ (DMS.Sales DLS_DealDetail): quản lý từng xe, số khung VIN, giá bán, biển số xe, số hóa đơn xuất khách và trạng thái bàn giao thực tế.</summary>
public sealed class RetailDealDetail
{
    public long Id { get; set; }
    public long DealId { get; set; }
    public string DealNo { get; set; } = "";
    public string CarId { get; set; } = ""; // Mã định danh xe kho đại lý
    public string Vin { get; set; } = ""; // Số VIN (17 ký tự)
    public string Model { get; set; } = ""; // Dòng xe (Santa Fe, Tucson, Accent...)
    public string? SpecCode { get; set; } // Cấu hình / Spec
    public string? ColorCode { get; set; } // Mã màu xe
    public decimal Price { get; set; } // Giá bán xe (> 0)
    public string? PlateNo { get; set; } // Biển số xe đăng ký (vd: 30K-123.45)
    public string? CusInvoiceNo { get; set; } // Số hóa đơn GTGT xuất cho khách hàng
    public DateTime? CusInvoiceDate { get; set; } // Ngày hóa đơn xuất khách (đi cặp với CusInvoiceNo)
    public RetailDealDeliveryStatus DeliveryStatus { get; set; } = RetailDealDeliveryStatus.Pending;
    public DateTime? DeliveryDate { get; set; } // Ngày bàn giao xe thực tế
    public string? Remark { get; set; }
}

/// <summary>Chứng từ / Ảnh đính kèm giao dịch bán lẻ (DMS.Sales DLS_DealAttachFile / DLS_DealAttachFileBill / DLS_DealAttachFileInsurrance): lưu ảnh hóa đơn xuất khách, bảo hiểm, đăng kiểm.</summary>
public sealed class RetailDealAttach
{
    public long Id { get; set; }
    public long DealId { get; set; }
    public string DealNo { get; set; } = "";
    public string FileType { get; set; } = "BILL"; // BILL = Hóa đơn KH, INSURANCE = Bảo hiểm, REGISTRATION = Đăng kiểm, OTHER = Khác
    public string FileName { get; set; } = ""; // Tên file
    public string FilePath { get; set; } = ""; // Đường dẫn / URL file
    public string? Remark { get; set; }
    public DateTime UploadedAt { get; set; } = DateTime.Now;
    public string? UploadedBy { get; set; }
}

/// <summary>Loại điều chuyển kho xe ô tô (DMS.Sales Sto_StorageRearrange RearrangeType: Normal = Điều chuyển thông thường / phân bổ, Urgent = Điều chuyển khẩn cấp, Showroom = Trưng bày showroom, Maintenance = Bảo quản / chuyển bãi).</summary>
public enum StorageRearrangeType { Normal = 0, Urgent = 1, Showroom = 2, Maintenance = 3 }

/// <summary>Trạng thái Lệnh điều chuyển kho xe (DMS.Sales Sto_StorageRearrange RearrangeStatus: Pending = 'P' [Chờ duyệt cấp 1], Approved1 = 'A1' [NPP duyệt kế hoạch điều chuyển], Approved2 = 'A2' [NPP duyệt cấp 2 / Xuất kho thực tế hoàn tất], Rejected = 'R' [Từ chối], Cancelled = 'C' [Đã hủy]).</summary>
public enum StorageRearrangeStatus { Pending = 0, Approved1 = 1, Approved2 = 2, Rejected = 3, Cancelled = 4 }

/// <summary>Trạng thái từng dòng xe trong lệnh điều chuyển kho (DMS.Sales Sto_StorageRearrangeDetail RearrangeDtlStatus: Pending = Chờ duyệt, Approved1 = Đã duyệt kế hoạch, InTransit = Đang vận chuyển xuất kho, Completed = Đã nhập kho đích, Rejected = Từ chối).</summary>
public enum StorageRearrangeDtlStatus { Pending = 0, Approved1 = 1, InTransit = 2, Completed = 3, Rejected = 4 }

/// <summary>Lệnh điều chuyển kho xe ô tô Nhà phân phối & Đại lý (DMS.Sales Sto_StorageRearrange / StoStorageRearrangeController / Storage.1.cs): quản lý điều phối di chuyển lô xe giữa các tổng kho (Kho nhà máy, Kho cảng, Kho trung chuyển, Kho đại lý), quy trình phê duyệt 2 cấp (Approve1 duyệt kế hoạch, Approve2 xuất - nhập kho thực tế hoàn tất), quản lý lịch trình và kiểm đếm xe.</summary>
public sealed class StorageRearrangeOrder
{
    public long Id { get; set; }
    public Guid OrgId { get; set; }
    public string RearrangeNo { get; set; } = ""; // Mã lệnh điều chuyển (PK StorageRearrangeNo, vd: SR26090001)
    public string StorageCodeFrom { get; set; } = ""; // Mã kho xuất phát (StorageCodeFrom)
    public string StorageNameFrom { get; set; } = ""; // Tên kho xuất phát
    public string StorageCodeTo { get; set; } = ""; // Mã kho đích đến (StorageCodeTo)
    public string StorageNameTo { get; set; } = ""; // Tên kho đích đến
    public StorageRearrangeType RearrangeType { get; set; } = StorageRearrangeType.Normal; // Loại chuyển kho
    public StorageRearrangeStatus Status { get; set; } = StorageRearrangeStatus.Pending; // Trạng thái lệnh (P -> A1 -> A2 / R / C)
    public int TotalCars { get; set; } // Tổng số lượng xe điều chuyển
    public string? TransporterName { get; set; } // Đơn vị phụ trách vận chuyển điều chuyển
    public string? PlateNo { get; set; } // Biển số xe chuyên chở (xe lồng)
    public string? DriverName { get; set; } // Tên tài xế phụ trách
    public string? DriverPhone { get; set; } // SĐT tài xế
    public string? Remark { get; set; } // Ghi chú lệnh điều chuyển
    public string? RejectReason { get; set; } // Lý do từ chối duyệt (Reject1HQ/Reject2HQ)
    public string? CancelReason { get; set; } // Lý do hủy lệnh
    public string? CreatedBy { get; set; } // Người lập lệnh
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public string? Approved1By { get; set; } // Người duyệt cấp 1 (Duyệt kế hoạch)
    public DateTime? Approved1At { get; set; } // Ngày duyệt cấp 1
    public string? Approved2By { get; set; } // Người duyệt cấp 2 (Hoàn tất xuất kho)
    public DateTime? Approved2At { get; set; } // Ngày duyệt cấp 2
    public string? CancelledBy { get; set; }
    public DateTime? CancelledAt { get; set; }

    public List<StorageRearrangeDetail> Details { get; set; } = new();
}

/// <summary>Chi tiết dòng xe ô tô trong Lệnh điều chuyển kho (DMS.Sales Sto_StorageRearrangeDetail): theo dõi từng số khung VIN, dòng xe, kho xuất, kho đến, ngày dự kiến và ngày thực tế xuất/nhập kho.</summary>
public sealed class StorageRearrangeDetail
{
    public long Id { get; set; }
    public long RearrangeId { get; set; }
    public string RearrangeNo { get; set; } = "";
    public string CarId { get; set; } = ""; // Mã định danh xe hệ thống
    public string Vin { get; set; } = ""; // Số khung xe (17 ký tự)
    public string Model { get; set; } = ""; // Dòng xe (Santa Fe, Tucson, Creta, Accent, Custin...)
    public string? SpecCode { get; set; } // Mã cấu hình
    public string? ColorCode { get; set; } // Mã màu xe
    public string? EngineNo { get; set; } // Số máy
    public string StorageCodeFrom { get; set; } = ""; // Kho xuất xe
    public string StorageCodeTo { get; set; } = ""; // Kho nhận xe
    public DateTime? ExpectedStartDate { get; set; } // Ngày dự kiến xuất kho vận chuyển
    public DateTime? ExpectedEndDate { get; set; } // Ngày dự kiến nhận xe tại kho đích
    public DateTime? ActualOutDate { get; set; } // Ngày thực tế xuất kho (RearrangeOutDate)
    public DateTime? ActualInDate { get; set; } // Ngày thực tế nhận vào kho đích (RearrangeEndDate)
    public StorageRearrangeDtlStatus Status { get; set; } = StorageRearrangeDtlStatus.Pending;
    public DateTime? ConfirmDate { get; set; } // Ngày xác nhận kiểm đếm xe
    public string? ConfirmBy { get; set; } // Người xác nhận kiểm đếm xe
    public string? Remark { get; set; } // Ghi chú dòng xe
}

/// <summary>Loại yêu cầu vận tải xe (DMS.Sales Sto_TranspReqType: CARTRANSPORT = Vận chuyển theo lệnh xuất xe, CARRETRIEVE = Vận chuyển thu hồi xe, STORAGEREARRANGE = Vận chuyển điều chuyển kho nội bộ, STORAGEREARRCB = Vận chuyển điều chuyển đóng thùng).</summary>
public enum TransportRequestType { CarTransport = 0, CarRetrieve = 1, StorageRearrange = 2, StorageRearrCB = 3 }

/// <summary>Trạng thái yêu cầu vận tải xe (DMS.Sales Sto_TranspReq TranspReqStatus: Pending = 'P' [Chờ duyệt HQ / điều phối xe], Approved = 'A' [HQ đã duyệt lệnh vận chuyển], InTransit = 'T' [Đang vận chuyển trên đường], Completed = 'F' [Đã hoàn tất vận chuyển / giao nhận], Rejected = 'R' [Từ chối], Cancelled = 'C' [Đã hủy]).</summary>
public enum TransportRequestStatus { Pending = 0, Approved = 1, InTransit = 2, Completed = 3, Rejected = 4, Cancelled = 5 }

/// <summary>Trạng thái từng dòng xe trong yêu cầu vận tải (DMS.Sales Sto_TranspReqDtl TranspReqDtlStatus: Pending = Chờ duyệt, Approved = Đã duyệt, InTransit = Đang vận chuyển, Completed = Hoàn tất, Rejected = Từ chối).</summary>
public enum TransportRequestDtlStatus { Pending = 0, Approved = 1, InTransit = 2, Completed = 3, Rejected = 4 }

/// <summary>Yêu cầu vận tải xe ô tô / Lệnh vận chuyển xe từ Kho NPP tới Đại lý hoặc giữa các điểm giao nhận (DMS.Sales Sto_TranspReq / StoTranspReqController / Storage.cs): điều động đơn vị vận tải, hợp đồng vận tải, xe chuyên dụng chở xe, quy trình phê duyệt điều phối, từ chối (chặn khi đã có BBGN), gỡ xe (tự động xóa lệnh khi hết xe) và xác nhận hoàn tất giao nhận.</summary>
public sealed class TransportRequest
{
    public long Id { get; set; }
    public Guid OrgId { get; set; }
    public string TranspReqNo { get; set; } = ""; // Mã YCVT (PK TranspReqNo, vd: TR26090001, YCVT2609-0001)
    public string DealerCode { get; set; } = ""; // Mã đại lý nhận xe / liên quan
    public string DealerName { get; set; } = ""; // Tên đại lý
    public string TransporterCode { get; set; } = ""; // Mã đơn vị vận tải (Mst_Transporter)
    public string TransporterName { get; set; } = ""; // Tên công ty / đội xe vận tải
    public string? TransportContractNo { get; set; } // Số hợp đồng vận tải
    public TransportRequestType TranspReqType { get; set; } = TransportRequestType.CarTransport; // Phân loại vận tải
    public TransportRequestStatus Status { get; set; } = TransportRequestStatus.Pending; // Trạng thái YCVT (P -> A -> T -> F / R / C)
    public int TotalCars { get; set; } // Tổng số xe chuyên chở
    public string? PlateNo { get; set; } // Biển số xe lồng / xe chở xe
    public string? DriverName { get; set; } // Tài xế lái xe chuyên chở
    public string? DriverPhone { get; set; } // SĐT tài xế
    public DateTime? ExpectedStartDate { get; set; } // Ngày dự kiến bốc xe xuất bãi
    public DateTime? ExpectedEndDate { get; set; } // Ngày dự kiến giao xe đến nơi
    public string? Remark { get; set; } // Ghi chú yêu cầu
    public string? RejectReason { get; set; } // Lý do từ chối (RejectHQ)
    public string? CancelReason { get; set; } // Lý do hủy yêu cầu
    public string? CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public string? ApprovedBy { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? CancelledBy { get; set; }
    public DateTime? CancelledAt { get; set; }

    public List<TransportRequestDetail> Details { get; set; } = new();
}

/// <summary>Chi tiết dòng xe ô tô trong Yêu cầu vận tải (DMS.Sales Sto_TranspReqDtl): liên kết số khung VIN, mã xe, lệnh nguồn tham chiếu (RefOrdNo: Lệnh xuất xe, Thu hồi, Điều chuyển kho), kho xuất và kho nhận.</summary>
public sealed class TransportRequestDetail
{
    public long Id { get; set; }
    public long TranspRequestId { get; set; }
    public string TranspReqNo { get; set; } = "";
    public TransportRequestType TranspReqType { get; set; } = TransportRequestType.CarTransport; // Loại YCVT dòng xe
    public string RefOrdNo { get; set; } = ""; // Số lệnh nguồn (Lệnh xuất xe DO, Lệnh thu hồi, Lệnh điều chuyển)
    public string CarId { get; set; } = ""; // Mã xe hệ thống
    public string Vin { get; set; } = ""; // Số khung VIN (17 ký tự)
    public string Model { get; set; } = ""; // Dòng xe (Santa Fe, Tucson, Creta, Accent...)
    public string? SpecCode { get; set; } // Mã spec / phiên bản
    public string? ColorCode { get; set; } // Mã màu xe
    public string? EngineNo { get; set; } // Số máy
    public string? StorageCodeFrom { get; set; } // Kho bốc xe đi
    public string? StorageCodeTo { get; set; } // Kho nhận xe đến
    public TransportRequestDtlStatus Status { get; set; } = TransportRequestDtlStatus.Pending;
    public DateTime? ActualOutDate { get; set; } // Ngày thực tế xe rời kho
    public DateTime? ActualInDate { get; set; } // Ngày thực tế xe nhập bãi đích
    public string? Remark { get; set; }
}

/// <summary>Trạng thái Công văn gia hạn / phát hành bảo lãnh (DMS.Sales Pmt_GrtClaimExt GrtClaimExtStatus: Pending = 'P' [Chờ ký điện tử], Signed = 'S' [Đã ký số và gửi ngân hàng], Cancelled = 'C' [Đã hủy]).</summary>
public enum GuaranteeExtStatus { Pending = 0, Signed = 1, Cancelled = 2 }

/// <summary>Trạng thái dòng xe trong công văn gia hạn bảo lãnh (DMS.Sales Pmt_GrtClaimExtDtl GrtClaimExtStatusDtl: Pending = 'P', Signed = 'S', Cancelled = 'C').</summary>
public enum GuaranteeExtDtlStatus { Pending = 0, Signed = 1, Cancelled = 2 }

/// <summary>Công văn đề nghị gia hạn / phát hành bảo lãnh thanh toán bán buôn xe đại lý - NPP (DMS.Sales Pmt_GrtClaimExt / PmtGrtClaimExtController / PaymentGrtExt.cs): Đại lý lập công văn xin kéo dài thêm N ngày hiệu lực bảo lãnh (NumberOfGuaranteeExt) hoặc xin mở bảo lãnh cho xe chưa có hiệu lực (TotalCarNoStart), chọn pháp nhân HTC/HTCLD (FlagisHTC), NPP thẩm tra ký số điện tử (E-Sign) cập nhật kéo dài thời hạn bảo lãnh ngân hàng.</summary>
public sealed class PaymentGuaranteeExt
{
    public long Id { get; set; }
    public Guid OrgId { get; set; }
    public string GrtClaimExtNo { get; set; } = ""; // Số hiệu công văn: {yyMMdd}-{seq:D3}/CVGH/{DealerCode} (vd: 260923-001/CVGH/VN001)
    public string DealerCode { get; set; } = ""; // Mã đại lý
    public string DealerName { get; set; } = ""; // Tên đại lý
    public int NumberOfGuaranteeExt { get; set; } = 30; // Số ngày đề nghị gia hạn (VD: 15, 30 ngày)
    public int TotalCars { get; set; } // Tổng số xe trong công văn
    public int TotalCarNoStart { get; set; } // Tổng số xe chưa có ngày hiệu lực bảo lãnh (cần mở bảo lãnh mới)
    public string FlagisHTC { get; set; } = "1"; // Pháp nhân phát hành CV: "1" = HTC (Hyundai Thành Công VN), "2" = HTCLD (HTC Liên Doanh)
    public GuaranteeExtStatus Status { get; set; } = GuaranteeExtStatus.Pending; // Trạng thái CV: P -> S / C
    public DateTime? SignDateTime { get; set; } // Ngày giờ ký điện tử E-Sign
    public string? SignBy { get; set; } // Người đại diện NPP ký điện tử
    public string? FileSigned { get; set; } // File PDF công văn đã ký điện tử
    public string? Remark { get; set; } // Ghi chú nội dung công văn
    public string? CancelReason { get; set; } // Lý do hủy công văn
    public DateTime? CancelDateTime { get; set; } // Ngày giờ hủy công văn
    public string? CancelBy { get; set; } // Người thực hiện hủy
    public string? CreatedBy { get; set; } // Người lập công văn
    public DateTime CreatedDateTime { get; set; } = DateTime.Now; // Ngày giờ lập công văn
    public DateTime? LUDateTime { get; set; } // Ngày giờ cập nhật cuối
    public string? LUBy { get; set; } // Người cập nhật cuối

    public List<PaymentGuaranteeExtDetail> Details { get; set; } = new();
}

/// <summary>Chi tiết dòng xe ô tô trong Công văn gia hạn / phát hành bảo lãnh (DMS.Sales Pmt_GrtClaimExtDtl): liên kết số khung VIN, mã xe, hợp đồng mua bán buôn, mã thư bảo lãnh ngân hàng, ngày hiệu lực và ngày hết hạn mới sau khi gia hạn.</summary>
public sealed class PaymentGuaranteeExtDetail
{
    public long Id { get; set; }
    public long GrtClaimExtId { get; set; }
    public string GrtClaimExtNo { get; set; } = "";
    public string CarId { get; set; } = ""; // Mã xe hệ thống
    public string Vin { get; set; } = ""; // Số khung xe (17 ký tự)
    public string Model { get; set; } = ""; // Model dòng xe (Santa Fe, Tucson, Creta, Accent...)
    public string? SpecCode { get; set; } // Mã cấu hình
    public string? ColorCode { get; set; } // Mã màu xe
    public string? ContractNo { get; set; } // Số hợp đồng mua buôn đại lý (DlrCtrNo)
    public string? GuaranteeNo { get; set; } // Mã bảo lãnh hệ thống
    public string? BankGuaranteeNo { get; set; } // Số thư bảo lãnh ngân hàng
    public string? BankCode { get; set; } // Mã ngân hàng phát hành (GrtBankCode: VCB, BIDV, TCB...)
    public string? BankCodeMonitor { get; set; } // Mã ngân hàng giám sát
    public string? GuaranteeType { get; set; } = "BL"; // Loại bảo lãnh: BL | LCTC | LCUP | EPLC
    public DateTime? DateOpen { get; set; } // Ngày mở bảo lãnh
    public DateTime? GrtDateStart { get; set; } // Ngày bắt đầu hiệu lực (null = chưa có hiệu lực)
    public DateTime? GrtDateExpired { get; set; } // Ngày hết hạn bảo lãnh hiện tại
    public DateTime? GrtDateEnd { get; set; } // Ngày kết thúc bảo lãnh mới dự kiến (sau khi gia hạn thêm N ngày)
    public decimal GuaranteeValue { get; set; } // Giá trị bảo lãnh xe (VNĐ)
    public decimal UnitPriceActual { get; set; } // Giá xe thực tế
    public GuaranteeExtDtlStatus Status { get; set; } = GuaranteeExtDtlStatus.Pending; // Trạng thái xe trong CV (P -> S / C)
    public string? Remark { get; set; } // Ghi chú dòng xe
}

/// <summary>Trạng thái Phiếu yêu cầu kiểm tra xe trước khi giao PDI (DMS.Sales Dlr_PDIRequest DlrPDIReqStatus: Pending = 'P' [Chờ kiểm tra/chờ duyệt], Approved = 'A' [Nghiệm thu PDI đạt chuẩn], Cancelled = 'C' [Hủy yêu cầu]).</summary>
public enum PdiRequestStatus { Pending = 0, Approved = 1, Cancelled = 2 }

/// <summary>Trạng thái từng dòng xe trong phiếu yêu cầu PDI (DMS.Sales Dlr_PDIRequestDtl DlrPDIReqDtlStatus: Pending = 'P' [Chờ kiểm tra], Approved = 'A' [Đạt chuẩn PDI], Cancelled = 'C' [Hủy xe]).</summary>
public enum PdiRequestDtlStatus { Pending = 0, Approved = 1, Cancelled = 2 }

/// <summary>Yêu cầu kiểm tra xe trước khi giao PDI của Đại lý (DMS.Sales Dlr_PDIRequest / DlrPDIRequestController / DealerRetail.cs): quản lý quy trình kiểm tra chất lượng kỹ thuật xe ô tô mới trước khi bàn giao cho khách hàng (PDI - Pre-Delivery Inspection), yêu cầu lắp đặt phụ kiện (FlagAccessory), liên kết hợp đồng bán lẻ xe và đồng bộ lệnh dịch vụ xưởng RO.</summary>
public sealed class PdiRequest
{
    public long Id { get; set; }
    public Guid OrgId { get; set; }
    public string DlrPdiReqNo { get; set; } = ""; // Số yêu cầu PDI (DlrPdiReqNo: format {yyMM}PRN{seq:D5})
    public string DealerCode { get; set; } = ""; // Mã đại lý lập yêu cầu
    public string DealerName { get; set; } = ""; // Tên đại lý
    public bool FlagAccessory { get; set; } // Lắp đặt phụ kiện kèm theo ('1' = Có, '0' = Không)
    public PdiRequestStatus Status { get; set; } = PdiRequestStatus.Pending; // Trạng thái PDI: Pending, Approved, Cancelled
    public int TotalCars { get; set; } // Tổng số lượng xe yêu cầu PDI
    public string? Remark { get; set; } // Nội dung / Ghi chú kiểm tra
    public DateTime? ApprovedDate { get; set; } // Ngày duyệt / nghiệm thu PDI
    public string? ApprovedBy { get; set; } // Người duyệt / nghiệm thu PDI
    public DateTime? CancelledDate { get; set; } // Ngày hủy yêu cầu PDI
    public string? CancelledBy { get; set; } // Người thực hiện hủy
    public string? CancelReason { get; set; } // Lý do hủy yêu cầu PDI
    public string? CreatedBy { get; set; } // Người tạo yêu cầu
    public DateTime CreatedDate { get; set; } = DateTime.Now; // Ngày tạo yêu cầu
    public DateTime? LUDateTime { get; set; } // Ngày cập nhật cuối
    public string? LUBy { get; set; } // Người cập nhật cuối

    public List<PdiRequestDetail> Details { get; set; } = new();
}

/// <summary>Chi tiết dòng xe ô tô trong Yêu cầu PDI (DMS.Sales Dlr_PDIRequestDtl): theo dõi từng số khung VIN, dòng xe, hợp đồng bán lẻ, mã xe hợp đồng CtrCarId, thông tin khách hàng, lệnh sửa chữa/báo giá xưởng RO (Repair Order) và kết quả kiểm tra.</summary>
public sealed class PdiRequestDetail
{
    public long Id { get; set; }
    public long PdiRequestId { get; set; }
    public string DlrPdiReqNo { get; set; } = ""; // Số phiếu PDI
    public string Vin { get; set; } = ""; // Số khung VIN (17 ký tự)
    public string Model { get; set; } = ""; // Model tên xe (Santa Fe, Creta, Accent, Tucson...)
    public string? ModelCode { get; set; } // Mã model
    public string? SpecCode { get; set; } // Mã cấu hình xe
    public string? ColorCode { get; set; } // Mã màu xe
    public string? ColorName { get; set; } // Tên màu ngoại thất/nội thất
    public string DlrContractNo { get; set; } = ""; // Số hợp đồng bán lẻ xe
    public string CtrCarId { get; set; } = ""; // Mã xe trong hợp đồng bán lẻ (vd: RC26090001.01)
    public string? CustomerName { get; set; } // Tên khách hàng theo hợp đồng
    public string? CustomerPhone { get; set; } // Số điện thoại khách hàng
    public string? DealNo { get; set; } // Số giao dịch bán lẻ nếu có
    public DateTime? DlvExpectedDate { get; set; } // Ngày giao xe dự kiến cho khách
    public string? RONo { get; set; } // Số lệnh sửa chữa / báo giá RO dịch vụ (Repair Order No)
    public DateTime? ROCreatedDate { get; set; } // Ngày tạo lệnh RO
    public DateTime? ROFinishedDate { get; set; } // Ngày hoàn thành lệnh RO
    public string ROStatus { get; set; } = "NORE"; // Trạng thái RO: NORE (chưa có), OPEN, COMPLETED
    public PdiRequestDtlStatus Status { get; set; } = PdiRequestDtlStatus.Pending; // Trạng thái kiểm tra dòng xe
    public string? InspectionResult { get; set; } // Kết quả kiểm tra: PASSED (Đạt chuẩn), PENDING_FIX (Cần khắc phục), FAILED
    public string? Remark { get; set; } // Ghi chú chi tiết hạng mục / phụ kiện cần lắp
}

/// <summary>Trạng thái Đề nghị chiết khấu thanh toán mua buôn xe (DMS.Sales Req_PaymentDiscount PmtDctStatus: Draft = 'NotSign' [Đang lập/Chưa ký duyệt], Approved = 'Approved' [NPP duyệt chiết khấu], Signed = 'Sign' [Hai bên đã ký số hoàn tất], Rejected = 'Reject' [NPP từ chối duyệt], Cancelled = 'Cancel' [NPP/Đại lý đã hủy]).</summary>
public enum PaymentDiscountStatus { Draft = 0, Approved = 1, Signed = 2, Rejected = 3, Cancelled = 4 }

/// <summary>Trạng thái ký số điện tử của Đại lý và NPP (DMS.Sales Req_PaymentDiscount DlrSignStatus / HTCSignStatus: Pending = 'Pending' [Chờ ký], Signed = 'Signed' / 'Approved1' / 'Approved2' [Đã ký số], Rejected = 'Reject', Cancelled = 'Cancel').</summary>
public enum PaymentDiscountSignStatus { Pending = 0, Signed = 1, Rejected = 2, Cancelled = 3 }

/// <summary>Đề nghị chiết khấu thanh toán mua buôn xe ô tô Đại lý - NPP (DMS.Sales Req_PaymentDiscount / ReqPaymentDiscountController / ReqPaymentDiscount.cs): chính sách khuyến khích đại lý thanh toán sớm tiền mua xe theo các đợt tất toán bảo lãnh ngân hàng (Phase 1/2/3), quản lý tính toán số ngày thanh toán sớm, tỷ lệ chiết khấu, số tiền chiết khấu được duyệt, luồng phê duyệt NPP và ký số điện tử 2 bên.</summary>
public sealed class PaymentDiscount
{
    public long Id { get; set; }
    public Guid OrgId { get; set; }
    public string PaymentDiscountNo { get; set; } = ""; // Số hiệu đề nghị (PK PaymentDiscountNo: format {yyyyMMdd}-{seq:D3}/DNCK/{DealerCode}, vd: 20260922-001/DNCK/VN001)
    public string DealerCode { get; set; } = ""; // Mã đại lý
    public string DealerName { get; set; } = ""; // Tên đại lý
    public string? CompanyName { get; set; } // Tên pháp nhân công ty đại lý
    public DateTime? DateEndFrom { get; set; } // Ngày tất toán bảo lãnh từ
    public DateTime? DateEndTo { get; set; } // Ngày tất toán bảo lãnh đến
    public int QtyCar { get; set; } // Tổng số lượng xe đề nghị hưởng chiết khấu
    public decimal SumTotalDiscountPrice { get; set; } // Tổng tiền chiết khấu thanh toán được hưởng (VNĐ)
    public decimal DiscountPercent { get; set; } = 0.5m; // Tỷ lệ chiết khấu thanh toán chuẩn (%)
    public decimal PenaltyPercent { get; set; } = 0.05m; // Tỷ lệ phạt chậm nộp (%)
    public PaymentDiscountStatus Status { get; set; } = PaymentDiscountStatus.Draft; // Trạng thái đề nghị: Draft -> Approved -> Signed / Rejected / Cancelled
    public PaymentDiscountSignStatus DealerSignStatus { get; set; } = PaymentDiscountSignStatus.Pending; // Trạng thái ký số ĐL
    public DateTime? DealerSignDate { get; set; } // Ngày giờ ĐL ký số
    public string? DealerSignBy { get; set; } // Người đại diện ĐL ký số
    public string? DealerSignFile { get; set; } // Tệp PDF văn bản đề nghị ĐL đã ký số
    public PaymentDiscountSignStatus HQSignStatus { get; set; } = PaymentDiscountSignStatus.Pending; // Trạng thái ký số NPP
    public DateTime? HQApproveDate { get; set; } // Ngày NPP phê duyệt hồ sơ chiết khấu
    public string? HQApproveBy { get; set; } // Cán bộ thẩm định NPP duyệt
    public DateTime? HQSignDate { get; set; } // Ngày Lãnh đạo NPP ký số hoàn tất quyết toán chiết khấu
    public string? HQSignBy { get; set; } // Lãnh đạo NPP ký số
    public string? HQSignFile { get; set; } // Tệp PDF văn bản quyết toán chiết khấu đã hoàn tất ký số 2 bên
    public string? RejectReason { get; set; } // Lý do NPP từ chối duyệt
    public string? CancelReason { get; set; } // Lý do hủy đề nghị
    public string? Remark { get; set; } // Ghi chú nội dung đề nghị
    public string? CreatedBy { get; set; } // Người lập đề nghị
    public DateTime CreatedAt { get; set; } = DateTime.Now; // Ngày tạo đề nghị
    public DateTime? LUDateTime { get; set; } // Ngày cập nhật cuối
    public string? LUBy { get; set; } // Người cập nhật cuối

    public List<PaymentDiscountDetail> Details { get; set; } = new();
}

/// <summary>Chi tiết dòng xe ô tô trong Đề nghị chiết khấu thanh toán (DMS.Sales Req_PaymentDiscountDtl): theo dõi từng số khung VIN, dòng xe, mã bảo lãnh ngân hàng (Pmt_Guarantee), thời hạn thanh toán và tính toán chiết khấu thanh toán sớm 3 đợt (Phase 1, 2, 3).</summary>
public sealed class PaymentDiscountDetail
{
    public long Id { get; set; }
    public long PaymentDiscountId { get; set; }
    public string PaymentDiscountNo { get; set; } = "";
    public string CarId { get; set; } = ""; // Mã xe hệ thống
    public string Vin { get; set; } = ""; // Số khung xe (17 ký tự)
    public string Model { get; set; } = ""; // Tên model xe (Santa Fe, Tucson, Creta, Accent, Custin...)
    public string? SpecCode { get; set; } // Mã phiên bản xe
    public string? SpecDescription { get; set; } // Mô tả bản xe
    public string? SOCode { get; set; } // Số đơn đặt hàng buôn liên quan (Ord_SalesOrderRoot)
    public string GuaranteeNo { get; set; } = ""; // Mã bảo lãnh ngân hàng liên quan (Pmt_Guarantee)
    public string? BankGuaranteeNo { get; set; } // Số thư bảo lãnh phía ngân hàng phát hành
    public string? BankCode { get; set; } // Ngân hàng phát hành thư bảo lãnh (VCB, BIDV, TCB...)
    public DateTime? DateOpen { get; set; } // Ngày mở thư bảo lãnh
    public int Term { get; set; } = 30; // Thời hạn bảo lãnh (ngày)
    public DateTime? DateStart { get; set; } // Ngày hiệu lực bảo lãnh / Ngày giao hồ sơ xe
    public DateTime? DateEnd { get; set; } // Ngày đến hạn tất toán bảo lãnh xe
    public decimal UnitPrice { get; set; } // Đơn giá niêm yết xe
    public decimal UnitPriceActual { get; set; } // Đơn giá thực tế xe bán cho đại lý
    public decimal GuaranteeValue { get; set; } // Giá trị bảo lãnh xe tại ngân hàng
    public DateTime? PaymentEndDatePhase1 { get; set; } // Ngày thanh toán đợt 1
    public decimal AmountPhase1 { get; set; } // Số tiền đại lý thanh toán đợt 1
    public int DiscountDateNumberPhase1 { get; set; } // Số ngày thanh toán sớm đợt 1
    public decimal DiscountPercentPhase1 { get; set; } // Tỷ lệ chiết khấu đợt 1 (%)
    public decimal DiscountPricePhase1 { get; set; } // Số tiền chiết khấu hưởng đợt 1
    public DateTime? PaymentEndDatePhase2 { get; set; } // Ngày thanh toán đợt 2
    public decimal AmountPhase2 { get; set; } // Số tiền đại lý thanh toán đợt 2
    public int DiscountDateNumberPhase2 { get; set; } // Số ngày thanh toán sớm đợt 2
    public decimal DiscountPercentPhase2 { get; set; } // Tỷ lệ chiết khấu đợt 2 (%)
    public decimal DiscountPricePhase2 { get; set; } // Số tiền chiết khấu hưởng đợt 2
    public DateTime? PaymentEndDatePhase3 { get; set; } // Ngày thanh toán đợt 3
    public decimal AmountPhase3 { get; set; } // Số tiền đại lý thanh toán đợt 3
    public int DiscountDateNumberPhase3 { get; set; } // Số ngày thanh toán sớm đợt 3
    public decimal DiscountPercentPhase3 { get; set; } // Tỷ lệ chiết khấu đợt 3 (%)
    public decimal DiscountPricePhase3 { get; set; } // Số tiền chiết khấu hưởng đợt 3
    public decimal TotalAmount { get; set; } // Tổng tiền thanh toán xe = Phase1 + Phase2 + Phase3
    public decimal TotalDiscountPrice { get; set; } // Tổng tiền chiết khấu được hưởng = Phase1 + Phase2 + Phase3
    public string? Remark { get; set; } // Ghi chú dòng xe
}

/// <summary>Trạng thái Biên bản hủy Hợp đồng bán buôn xe Đại lý - NPP (DMS.Sales DMS40_DlrCtr_CancelMinutes CancelMinutesStatus: Draft = 'NS' [Chưa ký/Đang lập], Signed = 'S' [Hai bên đã ký số hoàn tất thanh lý], Cancelled = 'C' [Biên bản đã hủy]).</summary>
public enum DealerContractCancelMinutesStatus { Draft = 0, Signed = 1, Cancelled = 2 }

/// <summary>Trạng thái ký số Biên bản hủy HĐ bán buôn của Đại lý và NPP (DMS.Sales DMS40_DlrCtr_CancelMinutes DlrSignCcMnStatus / HTCSignCcMnStatus: Pending = 'P' [Chờ ký], Approved = 'A' [Đại lý đã duyệt/ký số], Approved1 = 'A1' [NPP thẩm tra cấp 1], Approved2 = 'A2' [Lãnh đạo NPP ký số hoàn tất], Rejected = 'R' [Từ chối]).</summary>
public enum DealerContractCancelMinutesSignStatus { Pending = 0, Approved = 1, Approved1 = 2, Approved2 = 3, Rejected = 4 }

/// <summary>Biên bản thanh lý / Hủy hợp đồng mua bán buôn xe ô tô Đại lý - NPP (DMS.Sales DMS40_DlrCtr_CancelMinutes / DlrCtrCancelMinutesController / DMS40.0.34.Contract.cs): quy trình thanh lý hợp đồng bán buôn theo lô đã ký kết, kiểm tra điều kiện ràng buộc không vướng Lệnh xuất xe (DeliveryOrder) hay Bảo lãnh ngân hàng (PaymentGuarantee), quy trình phê duyệt ký số điện tử 2 bên (Đại lý ký số ApproveDL, NPP thẩm tra Approve1HQ, Lãnh đạo NPP ký số hoàn tất Approve2HQ và tự động cập nhật DealerContract sang Cancelled).</summary>
public sealed class DealerContractCancelMinutes
{
    public long Id { get; set; }
    public Guid OrgId { get; set; }
    public string CancelMinutesNo { get; set; } = ""; // Số biên bản hủy (PK CancelMinutesNo, format: {ContractNo}.CM{seq:D2}, vd: 2603DRC00001.CM01)
    public string ContractNo { get; set; } = ""; // Số hợp đồng mua buôn đại lý (DlrCtrNo)
    public string DealerCode { get; set; } = ""; // Mã đại lý
    public string DealerName { get; set; } = ""; // Tên đại lý
    public DateTime ContractDate { get; set; } // Ngày hợp đồng gốc
    public int TotalCars { get; set; } // Tổng số xe theo hợp đồng gốc
    public decimal TotalAmount { get; set; } // Tổng giá trị hợp đồng gốc (VNĐ)
    public string? Remark { get; set; } // Ghi chú / Lý do hủy hợp đồng
    public DealerContractCancelMinutesStatus CancelMinutesStatus { get; set; } = DealerContractCancelMinutesStatus.Draft;
    public DealerContractCancelMinutesSignStatus DealerSignStatus { get; set; } = DealerContractCancelMinutesSignStatus.Pending;
    public DealerContractCancelMinutesSignStatus HQSignStatus { get; set; } = DealerContractCancelMinutesSignStatus.Pending;
    public DateTime? DealerSignedAt { get; set; } // Ngày Đại lý ký số
    public string? DealerSignedBy { get; set; } // Người đại diện Đại lý ký số
    public DateTime? HQAppr1At { get; set; } // Ngày NPP thẩm định duyệt cấp 1
    public string? HQAppr1By { get; set; } // Chuyên viên thẩm định NPP
    public DateTime? HQAppr2At { get; set; } // Ngày Lãnh đạo NPP ký số hoàn tất cấp 2
    public string? HQAppr2By { get; set; } // Lãnh đạo NPP ký số
    public DateTime? RejectAt { get; set; } // Ngày NPP từ chối
    public string? RejectBy { get; set; }
    public string? RejectReason { get; set; } // Lý do NPP từ chối
    public DateTime? CancelledAt { get; set; } // Ngày hủy biên bản
    public string? CancelledBy { get; set; }
    public string? CancelReason { get; set; } // Lý do hủy biên bản
    public string? SignedFilePath { get; set; } // Đường dẫn / URL file PDF biên bản đã ký số (FilePath / FileName)
    public string? CreatedBy { get; set; } // Người lập biên bản
    public DateTime CreatedAt { get; set; } = DateTime.Now; // Ngày lập
    public DateTime? LUDateTime { get; set; } // Ngày cập nhật cuối
    public string? LUBy { get; set; } // Người cập nhật cuối
}

/// <summary>Trạng thái Công văn yêu cầu / đòi tiền thực hiện bảo lãnh thanh toán bán buôn xe (DMS.Sales Pmt_GrtClaim SignStatus: Pending = 'P' [Chờ ký/duyệt], Approved = 'A' [Đã duyệt và ký số điện tử phát hành tới ngân hàng], Rejected = 'R' [NPP từ chối duyệt], Cancelled = 'C' [Đã hủy]).</summary>
public enum GuaranteeClaimStatus { Pending = 0, Approved = 1, Rejected = 2, Cancelled = 3 }

/// <summary>Trạng thái xử lý dòng VIN xe trong công văn đòi tiền bảo lãnh (DMS.Sales Pmt_GrtClaim VinSignStatus / Pmt_GrtClaimDetail: Pending = 'P' [Chưa xử lý], Approved = 'A' [Đã xử lý / ký duyệt], Cancelled = 'C' [Đã hủy]).</summary>
public enum ClaimVinSignStatus { Pending = 0, Approved = 1, Cancelled = 2 }

/// <summary>Công văn Yêu cầu / Đòi tiền thực hiện bảo lãnh ngân hàng thanh toán mua buôn xe ô tô Đại lý - NPP (DMS.Sales Pmt_GrtClaim / PmtGrtClaimController / 17_CONG_VAN_BON_CHUC_NANG.md): NPP phát hành công văn đòi tiền ngân hàng bảo lãnh (loại YC) khi đại lý vi phạm hạn nợ hoặc đại lý chủ động đề nghị ngân hàng trích nợ thực hiện bảo lãnh (loại DN), quy trình ký số điện tử E-Sign hoàn tất và phát hành tới ngân hàng giám sát.</summary>
public sealed class PaymentGuaranteeClaim
{
    public long Id { get; set; }
    public Guid OrgId { get; set; }
    public string GrtClaimNo { get; set; } = ""; // Số hiệu công văn: CV-{yyMMdd}-{seq:D3}/{DealerCode} (vd: CV-260323-001/VN001)
    public string DealerCode { get; set; } = ""; // Mã đại lý
    public string DealerName { get; set; } = ""; // Tên đại lý
    public string GrtClaimTypeCode { get; set; } = "YC"; // Loại công văn: "DN" = Đề nghị thực hiện BL, "YC" = YC thực hiện BL thanh toán
    public string FlagisHTC { get; set; } = "1"; // Pháp nhân phát hành: "1" = HTC (Hyundai Thành Công VN), "2" = HTCLD (Liên doanh)
    public string? BankCode { get; set; } // Ngân hàng phát hành bảo lãnh (PMGBankCode)
    public string? BankName { get; set; } // Tên ngân hàng phát hành bảo lãnh
    public string? BankCodeMonitor { get; set; } // Ngân hàng giám sát (phía NPP quản lý bảo lãnh)
    public string? BankAccountNo { get; set; } // Số tài khoản ngân hàng liên quan / thụ hưởng
    public int TotalCars { get; set; } // Tổng số lượng xe ô tô trong công văn
    public decimal TotalClaimAmount { get; set; } // Tổng số tiền yêu cầu đòi bảo lãnh (VNĐ)
    public decimal TotalGuaranteeValue { get; set; } // Tổng giá trị bảo lãnh tương ứng (VNĐ)
    public GuaranteeClaimStatus Status { get; set; } = GuaranteeClaimStatus.Pending; // Trạng thái công văn: P -> A / R / C
    public ClaimVinSignStatus VinSignStatus { get; set; } = ClaimVinSignStatus.Pending; // Trạng thái tổng hợp xử lý VIN xe: P -> A / C
    public DateTime? SignDate { get; set; } // Ngày giờ ký số điện tử E-Sign
    public string? SignBy { get; set; } // Người đại diện NPP / Lãnh đạo ký số
    public string? FileSigned { get; set; } // Đường dẫn server file PDF đã ký số
    public string? FileName { get; set; } // Tên file PDF hiển thị
    public string? Remark { get; set; } // Ghi chú nội dung công văn
    public string? RejectReason { get; set; } // Lý do NPP từ chối duyệt công văn
    public DateTime? RejectDateTime { get; set; } // Ngày giờ từ chối
    public string? RejectBy { get; set; } // Người từ chối
    public string? CancelReason { get; set; } // Lý do hủy công văn
    public DateTime? CancelDateTime { get; set; } // Ngày giờ hủy công văn
    public string? CancelBy { get; set; } // Người thực hiện hủy
    public string? CreatedBy { get; set; } // Người tạo công văn
    public DateTime CreatedDate { get; set; } = DateTime.Today; // Ngày tạo (DATE)
    public DateTime CreatedDateTime { get; set; } = DateTime.Now; // Ngày giờ tạo đầy đủ
    public DateTime? LUDateTime { get; set; } // Ngày cập nhật cuối
    public string? LUBy { get; set; } // Người cập nhật cuối

    public List<PaymentGuaranteeClaimDetail> Details { get; set; } = new();
}

/// <summary>Chi tiết dòng xe ô tô trong Công văn yêu cầu / đòi tiền thực hiện bảo lãnh (DMS.Sales Pmt_GrtClaimDetail): liên kết số khung VIN, mã xe, hợp đồng mua bán buôn, thư bảo lãnh ngân hàng, giá trị xe và số tiền đòi bảo lãnh.</summary>
public sealed class PaymentGuaranteeClaimDetail
{
    public long Id { get; set; }
    public long GrtClaimId { get; set; }
    public string GrtClaimNo { get; set; } = "";
    public string CarId { get; set; } = ""; // Mã xe hệ thống
    public string Vin { get; set; } = ""; // Số khung xe (17 ký tự)
    public string Model { get; set; } = ""; // Tên model (Santa Fe, Tucson, Creta, Accent...)
    public string? ModelCode { get; set; } // Mã model
    public string? SpecCode { get; set; } // Mã cấu hình xe (spec)
    public string? SpecDescription { get; set; } // Mô tả đặc tả kỹ thuật xe
    public string? ColorCode { get; set; } // Mã màu xe
    public string? ColorName { get; set; } // Tên màu xe (Ngoại/Nội)
    public string? ContractNo { get; set; } // Số hợp đồng mua buôn đại lý (DlrCtrNo)
    public bool FlagDealerContractDMS40 { get; set; } = true; // Là HĐĐT DMS 4.0
    public string? GuaranteeNo { get; set; } // Mã bảo lãnh hệ thống
    public string? BankGuaranteeNo { get; set; } // Số thư bảo lãnh ngân hàng
    public string? BankCode { get; set; } // Mã ngân hàng phát hành BL
    public string? BankName { get; set; } // Tên ngân hàng phát hành BL
    public string? BankCodeMonitor { get; set; } // Mã ngân hàng giám sát
    public DateTime? DateOpen { get; set; } // Ngày mở bảo lãnh
    public DateTime? DateStart { get; set; } // Ngày bắt đầu hiệu lực bảo lãnh
    public DateTime? DateEnd { get; set; } // Ngày hết hạn thanh toán bảo lãnh
    public decimal UnitPriceActual { get; set; } // Giá xe thực tế (VNĐ)
    public decimal GuaranteeValue { get; set; } // Giá trị bảo lãnh xe (VNĐ)
    public decimal ClaimAmount { get; set; } // Số tiền yêu cầu đòi bảo lãnh cho xe này (VNĐ)
    public ClaimVinSignStatus Status { get; set; } = ClaimVinSignStatus.Pending; // Trạng thái xử lý VIN: P / A / C
    public string? Remark { get; set; } // Ghi chú chi tiết dòng xe
}

/// <summary>Trạng thái đề nghị / biên bản hủy chọn ngân hàng bảo lãnh hợp đồng mua buôn xe ô tô Đại lý - NPP (DMS.Sales DMS40_DlrCtr_CancelBankMD CancelBankMDStatus: Pending = 'P' [Chờ duyệt], BankApproved = 'A' [Ngân hàng bảo lãnh / Thẩm tra đã thẩm định chấp thuận], Finished = 'F' [NPP hoàn tất duyệt và tự động gỡ bỏ liên kết BankCodeMD khỏi DealerContract], Rejected = 'R' [Từ chối], Cancelled = 'C' [Đã hủy]).</summary>
public enum DealerContractCancelBankMDStatus { Pending = 0, BankApproved = 1, Finished = 2, Rejected = 3, Cancelled = 4 }

/// <summary>Đề nghị / Biên bản hủy chọn Ngân hàng phát hành bảo lãnh hợp đồng mua bán buôn xe Đại lý - NPP (DMS.Sales DMS40_DlrCtr_CancelBankMD / DlrCtrCancelBankMDController / DMS40.0.34.Contract.cs / CANCEL_BANK_MD_FLOW.md): Đại lý lập đề nghị hủy ngân hàng bảo lãnh đã liên kết với hợp đồng mua buôn, kiểm tra ràng buộc không vướng Lệnh xuất xe (DeliveryOrder), Bảo lãnh ngân hàng (PaymentGuarantee), Phiếu thanh toán (DealerPayment) hay Biên bản hủy HĐ (DealerContractCancelMinutes), quy trình phê duyệt thẩm tra (Ngân hàng/Thẩm tra BankApproved, NPP hoàn tất FinishHq) và tự động gỡ bỏ BankCodeMD trên DealerContract để đại lý liên kết ngân hàng mới hoặc chuyển phương thức thanh toán.</summary>
public sealed class DealerContractCancelBankMD
{
    public long Id { get; set; }
    public Guid OrgId { get; set; }
    public string CancelBankMDNo { get; set; } = ""; // Số biên bản / đề nghị hủy chọn NH (PK CancelBankMDNo, format: {ContractNo}.CB{seq:D2} hoặc HNB{yyMMdd}{seq:D3})
    public string ContractNo { get; set; } = ""; // Số hợp đồng mua buôn đại lý (DlrCtrNo)
    public string DealerCode { get; set; } = ""; // Mã đại lý
    public string DealerName { get; set; } = ""; // Tên đại lý
    public string BankCodeMD { get; set; } = ""; // Mã ngân hàng bảo lãnh cần hủy liên kết (VCB, BIDV, VPB, TCB...)
    public string? BankName { get; set; } // Tên ngân hàng bảo lãnh
    public int TotalCars { get; set; } // Tổng số xe theo hợp đồng gốc
    public decimal TotalAmount { get; set; } // Tổng giá trị hợp đồng gốc (VNĐ)
    public DealerContractCancelBankMDStatus Status { get; set; } = DealerContractCancelBankMDStatus.Pending;
    public string? RemarkDlr { get; set; } // Lý do / Ghi chú của Đại lý (đổi ngân hàng, hết hạn mức tín dụng...)
    public string? RemarkBank { get; set; } // Ý kiến thẩm tra / phản hồi của Ngân hàng bảo lãnh
    public string? RemarkHQ { get; set; } // Ý kiến thẩm định / phê duyệt của NPP
    public string? RejectReason { get; set; } // Lý do từ chối duyệt
    public string? CancelReason { get; set; } // Lý do đại lý hủy đề nghị
    public string? CreatedBy { get; set; } // Người lập đề nghị
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime? BankApprovedAt { get; set; } // Thời điểm Ngân hàng / Thẩm tra chấp thuận
    public string? BankApprovedBy { get; set; }
    public DateTime? FinishedAt { get; set; } // Thời điểm NPP duyệt hoàn tất (gỡ BankCode khỏi HĐ)
    public string? FinishedBy { get; set; }
    public DateTime? RejectedAt { get; set; } // Thời điểm từ chối duyệt
    public string? RejectedBy { get; set; }
    public DateTime? CancelledAt { get; set; } // Thời điểm đại lý hủy
    public string? CancelledBy { get; set; }
    public DateTime? LUDateTime { get; set; }
    public string? LUBy { get; set; }
}

/// <summary>Trạng thái Biên bản bàn giao hóa đơn & chứng từ xe theo Hối phiếu Ngân hàng (DMS.Sales Car_BankBillMinutes Status: Draft = 'D' [Nháp/Đang lập], Handover = 'H' [Đã bàn giao chứng từ sang Ngân hàng], BankReceived = 'A' [Ngân hàng đã tiếp nhận thẩm định hồ sơ gốc], Settled = 'S' [Đã quyết toán hối phiếu / giải ngân thanh toán], Cancelled = 'C' [Đã hủy]).</summary>
public enum BankBillMinutesStatus { Draft = 0, Handover = 1, BankReceived = 2, Settled = 3, Cancelled = 4 }

/// <summary>Trạng thái xử lý dòng VIN xe trong Biên bản bàn giao hối phiếu ngân hàng (DMS.Sales Car_BankBillMinutesDtl: Pending = 'P' [Chờ tiếp nhận], Received = 'R' [Ngân hàng đã nhận hồ sơ], Settled = 'S' [Đã thanh toán quyết toán], Cancelled = 'C' [Đã hủy]).</summary>
public enum BankBillMinutesDetailStatus { Pending = 0, Received = 1, Settled = 2, Cancelled = 3 }

/// <summary>Biên bản bàn giao hóa đơn chứng từ xe ô tô theo Hối phiếu Ngân hàng (DMS.Sales Car_BankBillMinutes / CarBankBillMinutesController / PaymentGrtExt.cs / HTC.QuanLyBienBanBanGiaoTheoHoiPhieu.xlsx): tập hợp bộ chứng từ xe gốc (Hóa đơn GTGT HTC/TCG, Phiếu kiểm tra chất lượng xuất xưởng CO/CQ, Tờ khai Hải quan, BBBG vận tải) kèm Hối phiếu thương mại đòi tiền gửi Ngân hàng thụ lý bảo lãnh thanh toán bán buôn xe, quy trình bàn giao hồ sơ, ngân hàng tiếp nhận và thẩm định, quyết toán giải ngân hối phiếu.</summary>
public sealed class BankBillMinutes
{
    public long Id { get; set; }
    public Guid OrgId { get; set; }
    public string BankBillMnNo { get; set; } = ""; // Số biên bản bàn giao hối phiếu (PK BankBillMnNo: format {yyMM}BBM{seq:D5}, vd: 2603BBM00001)
    public string BankCode { get; set; } = ""; // Mã ngân hàng nhận hối phiếu (VCB, BIDV, TCB, VPB...)
    public string? BankName { get; set; } // Tên ngân hàng nhận hối phiếu
    public string DealerCode { get; set; } = ""; // Mã đại lý ký phát hối phiếu
    public string DealerName { get; set; } = ""; // Tên đại lý ký phát hối phiếu
    public DateTime BankBillDate { get; set; } = DateTime.Today; // Ngày lập hối phiếu
    public DateTime? BankBillReceiveDate { get; set; } // Ngày ngân hàng tiếp nhận hồ sơ hối phiếu
    public DateTime? BankBillPrintDate { get; set; } // Ngày in / xuất trình hối phiếu
    public BankBillMinutesStatus Status { get; set; } = BankBillMinutesStatus.Draft; // Trạng thái biên bản: Draft -> Handover -> BankReceived -> Settled / Cancelled
    public int TotalCars { get; set; } // Tổng số lượng xe trong biên bản bàn giao
    public decimal TotalClaimAmount { get; set; } // Tổng số tiền hối phiếu đòi ngân hàng thanh toán (VNĐ)
    public string? BankOfficer { get; set; } // Cán bộ đại diện ngân hàng tiếp nhận hồ sơ
    public string? HTCOfficer { get; set; } // Cán bộ NPP phụ trách bàn giao chứng từ
    public string? Remark { get; set; } // Ghi chú nội dung bàn giao hối phiếu
    public string? CancelReason { get; set; } // Lý do hủy biên bản
    public DateTime? CancelledAt { get; set; } // Thời điểm hủy biên bản
    public string? CancelledBy { get; set; } // Người thực hiện hủy
    public DateTime? HandoverAt { get; set; } // Thời điểm bàn giao sang ngân hàng
    public string? HandoverBy { get; set; } // Người bàn giao sang ngân hàng
    public DateTime? BankReceivedAt { get; set; } // Thời điểm ngân hàng ký nhận hồ sơ
    public string? BankReceivedBy { get; set; } // Đại diện ngân hàng tiếp nhận
    public DateTime? SettledAt { get; set; } // Thời điểm quyết toán / giải ngân hối phiếu hoàn tất
    public string? SettledBy { get; set; } // Cán bộ kế toán xác nhận quyết toán
    public string? CreatedBy { get; set; } // Người tạo biên bản
    public DateTime CreatedAt { get; set; } = DateTime.Now; // Ngày giờ tạo
    public DateTime? LUDateTime { get; set; } // Ngày cập nhật cuối
    public string? LUBy { get; set; } // Người cập nhật cuối

    public List<BankBillMinutesDetail> Details { get; set; } = new();
}

/// <summary>Chi tiết dòng xe trong Biên bản bàn giao hối phiếu ngân hàng (DMS.Sales Car_BankBillMinutesDtl): quản lý số khung VIN, thông tin xe, số hợp đồng mua buôn, số thư bảo lãnh ngân hàng, số hóa đơn VAT NPP xuất, số BBBG vận chuyển, chứng từ gốc CO/CQ và số tiền hối phiếu đòi ngân hàng theo xe.</summary>
public sealed class BankBillMinutesDetail
{
    public long Id { get; set; }
    public long BankBillMinutesId { get; set; }
    public string BankBillMnNo { get; set; } = "";
    public string CarId { get; set; } = ""; // Mã định danh xe hệ thống
    public string Vin { get; set; } = ""; // Số khung VIN (17 ký tự)
    public string Model { get; set; } = ""; // Tên model xe (Santa Fe, Tucson, Creta, Accent...)
    public string? ModelCode { get; set; } // Mã model
    public string? SpecCode { get; set; } // Mã cấu hình xe (spec)
    public string? SpecDescription { get; set; } // Mô tả bản xe
    public string? ColorCode { get; set; } // Mã màu xe
    public string? EngineNo { get; set; } // Số máy
    public string? ContractNo { get; set; } // Số hợp đồng mua buôn đại lý (DlrCtrNo)
    public string? BankGuaranteeNo { get; set; } // Số thư bảo lãnh ngân hàng / Số LC
    public string? GuaranteeBankCode { get; set; } // Ngân hàng phát hành bảo lãnh
    public DateTime? GuaranteeDateOpen { get; set; } // Ngày phát hành BL/LC
    public string? HTCInvoiceNo { get; set; } // Số hóa đơn VAT HTC xuất cho đại lý
    public string? TCGInvoiceNo { get; set; } // Số hóa đơn TCG
    public string? InvoiceNoFactory { get; set; } // Hóa đơn nhà máy
    public string? TransportMinutesNo { get; set; } // Số biên bản bàn giao xe vận chuyển (BBBG)
    public string? CQNo { get; set; } // Số đăng kiểm / Phiếu kiểm tra chất lượng xuất xưởng
    public string? CONo { get; set; } // Số chứng nhận nguồn gốc xuất xứ CO
    public string? CabinCONo { get; set; } // Số nguồn gốc thùng (xe tải)
    public string? DeclarationNo { get; set; } // Số tờ khai hải quan (xe nhập khẩu)
    public decimal ClaimAmount { get; set; } // Số tiền hối phiếu đòi ngân hàng chiết khấu/thanh toán theo xe (VNĐ)
    public BankBillMinutesDetailStatus Status { get; set; } = BankBillMinutesDetailStatus.Pending;
    public string? Remark { get; set; } // Ghi chú dòng xe
}

