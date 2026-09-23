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

/// <summary>Trạng thái Đề nghị đăng ký xe lái thử Đại lý (DMS.Sales Car_TestCar TestCarStatus: Pending = 'P' [Chờ duyệt], Approved = 'A' [NPP duyệt kế hoạch Cấp 1], Finished = 'F' [NPP duyệt hoàn tất Cấp 2 và kích hoạt FlagTestCar = '1'], Rejected = 'R' [Từ chối], Cancelled = 'C' [Hủy]).</summary>
public enum TestCarStatus { Pending = 0, Approved = 1, Finished = 2, Rejected = 3, Cancelled = 4 }

/// <summary>Trạng thái dòng xe trong đề nghị đăng ký xe lái thử (DMS.Sales Car_TestCarDtl TestCarStatusDtl: Pending = 'P', Approved = 'A', Finished = 'F', Rejected = 'R', Cancelled = 'C').</summary>
public enum TestCarDetailStatus { Pending = 0, Approved = 1, Finished = 2, Rejected = 3, Cancelled = 4 }

/// <summary>Đề nghị / Phê duyệt Đăng ký Xe Lái Thử Đại lý - NPP (DMS.Sales Car_TestCar / CarTestCarController / 05_QUAN_LY_XE.md): Đại lý lập đề nghị đưa xe trong đơn hàng/kho vào đội xe Demo lái thử (TestCarCode format {yyMM}TC{seq:D4}), thời hạn hiệu lực lái thử (EffDateStart -> EffDateEnd), quy trình duyệt 2 cấp NPP (ApproveAHQ duyệt kế hoạch, ApproveFHQ duyệt chính thức đưa vào sử dụng và kích hoạt cờ xe lái thử FlagTestCar = '1' cho xe ô tô).</summary>
public sealed class CarTestCar
{
    public long Id { get; set; }
    public Guid OrgId { get; set; }
    public string TestCarCode { get; set; } = ""; // Số đề nghị đăng ký xe lái thử (PK TestCarCode, format: {yyMM}TC{seq:D4}, vd: 2603TC0001)
    public string DealerCode { get; set; } = ""; // Mã đại lý đăng ký
    public string DealerName { get; set; } = ""; // Tên đại lý (MD_DealerName)
    public TestCarStatus TestCarStatus { get; set; } = TestCarStatus.Pending; // Trạng thái đề nghị: P -> A -> F / R / C
    public int TotalCars { get; set; } // Tổng số xe đăng ký lái thử
    public decimal TotalAmount { get; set; } // Tổng giá trị xe lái thử (VNĐ)
    public string? Remark { get; set; } // Ghi chú / Mục đích đăng ký lái thử
    public string? RejectReason { get; set; } // Lý do NPP từ chối duyệt
    public DateTime? RejectDate { get; set; } // Ngày NPP từ chối
    public string? RejectBy { get; set; } // Người từ chối
    public string? CancelReason { get; set; } // Lý do hủy đề nghị
    public DateTime? CancelledDate { get; set; } // Ngày hủy đề nghị
    public string? CancelledBy { get; set; } // Người thực hiện hủy
    public DateTime CreatedDate { get; set; } = DateTime.Now; // Ngày tạo đề nghị
    public string? CreatedBy { get; set; } // Người tạo đề nghị
    public DateTime? ApprovedDate { get; set; } // Ngày NPP thẩm định duyệt kế hoạch Cấp 1 (ApproveAHQ)
    public string? ApprovedBy { get; set; } // Chuyên viên / Lãnh đạo NPP duyệt Cấp 1
    public DateTime? FinishedDate { get; set; } // Ngày NPP phê duyệt hoàn tất Cấp 2 đưa vào sử dụng (ApproveFHQ)
    public string? FinishedBy { get; set; } // Lãnh đạo NPP phê duyệt Cấp 2
    public DateTime? LUDateTime { get; set; } // Thời gian cập nhật cuối
    public string? LUBy { get; set; } // Người cập nhật cuối

    public List<CarTestCarDetail> Details { get; set; } = new();
}

/// <summary>Chi tiết dòng xe trong Đề nghị đăng ký xe lái thử (DMS.Sales Car_TestCarDtl): liên kết số khung VIN, mã xe CarId, Model, Spec, Color, đơn giá, thời hạn hiệu lực lái thử (EffDateStart đến EffDateEnd), cờ xe lái thử FlagTestCar và trạng thái dòng xe.</summary>
public sealed class CarTestCarDetail
{
    public long Id { get; set; }
    public long TestCarId { get; set; }
    public string TestCarCode { get; set; } = "";
    public string CarId { get; set; } = ""; // Mã định danh xe hệ thống
    public string Vin { get; set; } = ""; // Số khung VIN xe (17 ký tự)
    public string Model { get; set; } = ""; // Tên model xe (Santa Fe, Tucson, Creta, Accent...)
    public string? ModelCode { get; set; } // Mã model
    public string? SpecCode { get; set; } // Mã cấu hình xe (spec)
    public string? SpecDescription { get; set; } // Mô tả bản xe
    public string? ColorCode { get; set; } // Mã màu xe
    public string? ColorName { get; set; } // Tên màu xe
    public string? EngineNo { get; set; } // Số máy
    public string? SOCode { get; set; } // Số đơn đặt hàng liên kết (CC_SOCode)
    public decimal UnitPriceActual { get; set; } // Giá xe thực tế (VNĐ)
    public DateTime EffDateStart { get; set; } = DateTime.Today; // Thời gian đăng ký lái thử từ ngày
    public DateTime EffDateEnd { get; set; } = DateTime.Today.AddMonths(6); // Thời gian đăng ký lái thử đến ngày
    public bool FlagTestCar { get; set; } // Cờ xe lái thử (FlagTestCar: true khi duyệt hoàn tất Finished)
    public TestCarDetailStatus Status { get; set; } = TestCarDetailStatus.Pending; // Trạng thái dòng xe: P -> A -> F / R / C
    public string? Remark { get; set; } // Ghi chú dòng xe
}

/// <summary>Trạng thái Yêu cầu đóng thùng xe ô tô thương mại (DMS.Sales Sto_CBReq CBReqStatus: Pending = 'P' [Chờ duyệt], Approved = 'A' [NPP duyệt lệnh đóng thùng], Completed = 'D' [Nghiệm thu hoàn tất đóng thùng], Rejected = 'R' [Từ chối], Cancelled = 'C' [Đã hủy]).</summary>
public enum CarBodyRequestStatus { Pending = 0, Approved = 1, Completed = 2, Rejected = 3, Cancelled = 4 }

/// <summary>Trạng thái dòng xe trong yêu cầu đóng thùng (DMS.Sales Sto_CBReqDetail CBReqDtlStatus: Pending = 'P', Approved = 'A', Completed = 'D', Rejected = 'R', Cancelled = 'C').</summary>
public enum CarBodyRequestDtlStatus { Pending = 0, Approved = 1, Completed = 2, Rejected = 3, Cancelled = 4 }

/// <summary>Yêu cầu đóng thùng xe ô tô thương mại / xe tải (DMS.Sales Sto_CBReq / StoCBReqController / Storage.cs / StoCBReq.txt / YC Đóng Thùng.xlsx): quy trình điều chuyển xe chassis chưa đóng thùng (TypeCB='N') sang xưởng/kho đóng thùng (StorageType=DT), quản lý loại thùng (thùng mui bạt, thùng kín, thùng lửng, thùng đông lạnh...), NPP thẩm định phê duyệt (ApproveHQ), cập nhật chi tiết dòng xe (DetailUpdateHQ), từ chối (RejectHQ), hủy yêu cầu (CancelHQ) và nghiệm thu kỹ thuật xuất xưởng hoàn tất đóng thùng (CompleteHQ đồng bộ TypeCB='Y').</summary>
public sealed class CarBodyRequest
{
    public long Id { get; set; }
    public Guid OrgId { get; set; }
    public string CBReqNo { get; set; } = ""; // Số yêu cầu đóng thùng (PK CBReqNo, format: {yyMM}CBR{seq:D6}, vd: 2603CBR000001)
    public CarBodyRequestStatus Status { get; set; } = CarBodyRequestStatus.Pending; // Trạng thái: P -> A -> D / R / C
    public int TotalCars { get; set; } // Tổng số lượng xe cần đóng thùng
    public string StorageCodeTo { get; set; } = ""; // Kho/Xưởng đóng thùng đích (bắt buộc StorageType = DT)
    public string? StorageNameTo { get; set; } // Tên xưởng đóng thùng (vd: Xưởng đóng thùng Hiệp Hòa, Nam Việt, Quyền Auto...)
    public string? Remark { get; set; } // Ghi chú yêu cầu kỹ thuật đóng thùng
    public string? RejectReason { get; set; } // Lý do từ chối duyệt
    public DateTime? RejectDate { get; set; }
    public string? RejectBy { get; set; }
    public string? CancelReason { get; set; } // Lý do hủy yêu cầu
    public DateTime? CancelDate { get; set; }
    public string? CancelBy { get; set; }
    public DateTime CreatedDate { get; set; } = DateTime.Now; // Ngày tạo yêu cầu
    public string? CreatedBy { get; set; }
    public DateTime? ApprovedDate { get; set; } // Ngày NPP phê duyệt
    public string? ApprovedBy { get; set; }
    public DateTime? CompletedDate { get; set; } // Ngày nghiệm thu hoàn tất đóng thùng
    public string? CompletedBy { get; set; }
    public DateTime? LUDateTime { get; set; }
    public string? LUBy { get; set; }

    public List<CarBodyRequestDetail> Details { get; set; } = new();
}

/// <summary>Chi tiết dòng xe ô tô trong Yêu cầu đóng thùng (DMS.Sales Sto_CBReqDetail): theo dõi từng số khung VIN xe chassis, kho xuất bốc xe (StorageCodeFrom), xưởng nhận xe đóng thùng (StorageCodeTo), loại thùng cần đóng (LoaiThung), quy chuẩn thùng nghiệm thu và trạng thái xử lý.</summary>
public sealed class CarBodyRequestDetail
{
    public long Id { get; set; }
    public long CBRequestId { get; set; }
    public string CBReqNo { get; set; } = "";
    public string CarId { get; set; } = ""; // Mã định danh xe hệ thống
    public string Vin { get; set; } = ""; // Số khung VIN (17 ký tự)
    public string Model { get; set; } = ""; // Model xe thương mại (Mighty EX8, Porter H150, Mighty W11S...)
    public string? ModelCode { get; set; } // Mã model
    public string? SpecCode { get; set; } // Mã cấu hình xe chassis
    public string? SpecDescription { get; set; } // Mô tả cấu hình xe
    public string? ColorCode { get; set; } // Mã màu cabin
    public string? ColorName { get; set; } // Tên màu sơn
    public string? EngineNo { get; set; } // Số máy
    public string StorageCodeFrom { get; set; } = ""; // Kho hiện tại đang lưu trữ xe sắt xi
    public string? StorageNameFrom { get; set; } // Tên kho xuất xe
    public string StorageCodeTo { get; set; } = ""; // Kho/Xưởng đóng thùng đích (StorageType = DT)
    public string? StorageNameTo { get; set; } // Tên kho/xưởng đóng thùng
    public string LoaiThung { get; set; } = ""; // Loại thùng: Thùng mui bạt, Thùng kín Inox, Thùng kín Composite, Thùng đông lạnh, Thùng lửng...
    public string? ActualSpec { get; set; } // Cấu hình quy chuẩn thùng sau khi đóng xong
    public string TypeCB { get; set; } = "N"; // Tình trạng đóng thùng: "N" = Chưa đóng thùng (chassis), "Y" = Đã hoàn tất đóng thùng
    public CarBodyRequestDtlStatus Status { get; set; } = CarBodyRequestDtlStatus.Pending; // Trạng thái dòng: P -> A -> D / R / C
    public string? InspectionResult { get; set; } // Kết quả nghiệm thu kỹ thuật thùng xe (vd: PASSED, Đạt chuẩn an toàn kỹ thuật)
    public string? SerialNo { get; set; } // Số sê-ri phiếu xuất xưởng thùng xe
    public string? Remark { get; set; } // Ghi chú chi tiết dòng xe
}

/// <summary>Trạng thái lượt lái thử xe của khách hàng tại đại lý (DMS.Sales Dlr_DriveTest DriverTestStatus: Pending = 'P' [Chờ duyệt], Approved = 'A' [NPP duyệt chấp thuận], Rejected = 'R' [Từ chối duyệt], Cancelled = 'C' [Hủy]).</summary>
public enum DriveTestStatus { Pending = 0, Approved = 1, Rejected = 2, Cancelled = 3 }

/// <summary>Hình thức nhóm sự kiện lái thử xe (DMS.Sales Dlr_DriveTest DriverTestGroup: Showroom = 'SHOWROOM' [Tại Showroom đại lý], Event = 'EVENT' [Sự kiện / Roadshow lái thử ngoài trời]).</summary>
public enum DriveTestGroup { Showroom = 0, Event = 1 }

/// <summary>Loại hình nguồn kinh phí tài trợ lái thử (DMS.Sales Dlr_DriveTest DriverTestType: HTV = 'HTV' [NPP tài trợ / hỗ trợ kinh phí], Dealer = 'Dealer' [Đại lý tự túc kinh phí]).</summary>
public enum DriveTestType { HTV = 0, Dealer = 1 }

/// <summary>Quản lý Lượt lái thử xe của Khách hàng tại Đại lý (DMS.Sales Dlr_DriveTest / DlrDriveTestController / DealerRetail.cs): ghi nhận lịch trình, thông tin khách hàng, bằng lái GPLX, xe lái thử demo (DrvTestPlateNo), kết quả trải nghiệm, ý định mua xe và phê duyệt hỗ trợ kinh phí từ NPP.</summary>
public sealed class DealerDriveTest
{
    public long Id { get; set; }
    public Guid OrgId { get; set; }
    public string DriveTestCode { get; set; } = ""; // Mã lượt lái thử dạng {yyMM}DT{seq:D4} (DMS40 SequenceType: DRIVETEST)
    public string DealerCode { get; set; } = ""; // Mã đại lý tổ chức lái thử
    public string? DealerName { get; set; } // Tên đại lý
    public DriveTestGroup DriverTestGroup { get; set; } = DriveTestGroup.Showroom; // SHOWROOM hoặc EVENT
    public DriveTestType DriverTestType { get; set; } = DriveTestType.HTV; // HTV (NPP tài trợ) hoặc Dealer (Đại lý tự túc)
    public DateTime DriveDTime { get; set; } = DateTime.Today; // Ngày giờ thực hiện lái thử xe
    public string CustomerCode { get; set; } = ""; // Mã khách hàng đại lý (DLS_DealerCustomer)
    public string FullName { get; set; } = ""; // Họ và tên khách hàng
    public string Gender { get; set; } = "M"; // Giới tính khách hàng: "M" = Nam, "F" = Nữ
    public string RangeAgeCode { get; set; } = "1990"; // Năm sinh của khách hàng (chuẩn 4 số năm sinh từ 1940 đến 2010 theo rule DMS.Sales)
    public string DriverLicenseNo { get; set; } = ""; // Số Giấy phép lái xe (GPLX bắt buộc đối với khách lái thử)
    public string? IDCardNo { get; set; } // Số CCCD / CMND khách hàng
    public string PhoneNo { get; set; } = ""; // Số điện thoại liên hệ của khách hàng
    public string? Email { get; set; } // Địa chỉ email khách hàng
    public string? Address { get; set; } // Địa chỉ liên hệ của khách hàng
    public string? ProvinceCode { get; set; } // Mã tỉnh thành cư trú
    public string? ProvinceName { get; set; } // Tên tỉnh thành
    public string ModelCode { get; set; } = ""; // Mã model dòng xe khách đăng ký trải nghiệm (Santa Fe, Tucson, Creta, Accent...)
    public string? ModelName { get; set; } // Tên thương mại dòng xe
    public string DrvTestPlateNo { get; set; } = ""; // Biển số xe lái thử Demo của đại lý (liên kết đội xe Mst_CarDriverTest / CarTestCar)
    public string? DrvTestVIN { get; set; } // Số khung VIN của xe lái thử
    public string? SalesManCode { get; set; } // Mã tư vấn bán hàng (TVBH) đồng hành lái thử
    public string? SalesManName { get; set; } // Tên tư vấn bán hàng
    public string? Evaluation { get; set; } // Đánh giá trải nghiệm của khách: Rất hài lòng, Hài lòng, Bình thường, Không hài lòng
    public string? CustomerIntent { get; set; } // Ý định mua xe: WillBuy (Sắp ký HĐ), Considering (Cân nhắc đối thủ), Consulting (Cần tư vấn thêm), NoDemand (Chưa có nhu cầu)
    public DateTime? EstimatedContractDate { get; set; } // Ngày dự kiến ký hợp đồng bán lẻ nếu khách có ý định mua
    public decimal SupportAmount { get; set; } // Số tiền đề nghị NPP hỗ trợ/tài trợ chi phí lái thử (VNĐ)
    public decimal ApprovedAmount { get; set; } // Số tiền NPP thực tế duyệt tài trợ (VNĐ)
    public DriveTestStatus Status { get; set; } = DriveTestStatus.Pending; // Trạng thái xử lý lượt lái thử: P -> A / R / C
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public string? CreatedBy { get; set; }
    public DateTime? ApprovedDate { get; set; }
    public string? ApprovedBy { get; set; }
    public DateTime? RejectDate { get; set; }
    public string? RejectBy { get; set; }
    public string? RejectRemark { get; set; } // Lý do NPP từ chối duyệt chi phí
    public DateTime? CancelledDate { get; set; }
    public string? CancelledBy { get; set; }
    public string? CancelRemark { get; set; } // Lý do đại lý hủy lượt lái thử
    public string? Remark { get; set; } // Ghi chú bổ sung
    public bool FlagActive { get; set; } = true;
}

/// <summary>Trạng thái Lệnh điều chuyển đóng thùng xe (DMS.Sales Sto_RearrangeCB RearCBStatus: Pending = 'P' [Chờ duyệt], Approved = 'A' [NPP duyệt lệnh], Rejected = 'R' [Từ chối duyệt], Cancelled = 'C' [Hủy lệnh]).</summary>
public enum StoRearrangeCBStatus { Pending = 0, Approved = 1, Rejected = 2, Cancelled = 3 }

/// <summary>Trạng thái từng dòng xe trong Lệnh điều chuyển đóng thùng (DMS.Sales Sto_RearrangeCBDetail RearCBDtlStatus: Pending = 'P', Approved = 'A', Rejected = 'R', Cancelled = 'C').</summary>
public enum StoRearrangeCBDtlStatus { Pending = 0, Approved = 1, Rejected = 2, Cancelled = 3 }

/// <summary>Lệnh điều chuyển đóng thùng xe ô tô thương mại (DMS.Sales Sto_RearrangeCB / StoRearrangeCBController / Storage.cs / StoRearrangeCB.txt): điều chuyển xe chassis chưa đóng thùng (TypeCB='N') đã có Yêu cầu đóng thùng (Sto_CBReq) sang kho/xưởng đóng thùng (StorageType=DT), quản lý ngày bắt đầu và ngày giao dự kiến, NPP phê duyệt lệnh (ApproveHQ), từ chối (RejectHQ), hủy lệnh (CancelHQ), cập nhật chi tiết xe (DetailUpdateHQ) và báo cáo thống kê.</summary>
public sealed class StorageRearrangeCBOrder
{
    public long Id { get; set; }
    public Guid OrgId { get; set; }
    public string StoRearCBNo { get; set; } = ""; // Số lệnh điều chuyển đóng thùng (PK StoRearCBNo, format: {yyMM}RCB{seq:D6}, vd: 2603RCB000001)
    public StoRearrangeCBStatus Status { get; set; } = StoRearrangeCBStatus.Pending; // Trạng thái: P -> A / R / C
    public int TotalCars { get; set; } // Tổng số lượng xe điều chuyển đóng thùng
    public string StorageCodeTo { get; set; } = ""; // Kho/Xưởng đóng thùng đích (bắt buộc StorageType = DT)
    public string? StorageNameTo { get; set; } // Tên xưởng đóng thùng
    public string? Remark { get; set; } // Ghi chú lệnh đóng thùng
    public string? RejectReason { get; set; } // Lý do từ chối duyệt
    public DateTime? RejectDate { get; set; }
    public string? RejectBy { get; set; }
    public string? CancelReason { get; set; } // Lý do hủy lệnh
    public DateTime? CancelDate { get; set; }
    public string? CancelBy { get; set; }
    public DateTime CreatedDate { get; set; } = DateTime.Now; // Ngày tạo lệnh
    public string? CreatedBy { get; set; }
    public DateTime? ApprovedDate { get; set; } // Ngày NPP phê duyệt
    public string? ApprovedBy { get; set; }
    public DateTime? LUDateTime { get; set; }
    public string? LUBy { get; set; }

    public List<StorageRearrangeCBDetail> Details { get; set; } = new();
}

/// <summary>Chi tiết dòng xe ô tô trong Lệnh điều chuyển đóng thùng (DMS.Sales Sto_RearrangeCBDetail): theo dõi từng số khung VIN xe chassis, kho xuất bốc xe (StorageCodeFrom), xưởng nhận xe đóng thùng (StorageCodeTo), số Yêu cầu đóng thùng gốc (CBReqNo), ngày bắt đầu dự kiến (ExpectedStartDate), ngày giao dự kiến (ExpectedEndDate), loại thùng và trạng thái duyệt xe.</summary>
public sealed class StorageRearrangeCBDetail
{
    public long Id { get; set; }
    public long StoRearCBId { get; set; }
    public string StoRearCBNo { get; set; } = "";
    public string CarId { get; set; } = ""; // Mã định danh xe hệ thống
    public string Vin { get; set; } = ""; // Số khung VIN (17 ký tự)
    public string Model { get; set; } = ""; // Model xe thương mại (Mighty EX8, Porter H150...)
    public string? ModelCode { get; set; } // Mã model
    public string? SpecCode { get; set; } // Mã cấu hình xe chassis
    public string? SpecDescription { get; set; } // Mô tả cấu hình xe
    public string? ColorCode { get; set; } // Mã màu cabin
    public string? ColorName { get; set; } // Tên màu sơn
    public string? EngineNo { get; set; } // Số máy
    public string StorageCodeFrom { get; set; } = ""; // Kho hiện tại đang lưu giữ xe
    public string? StorageNameFrom { get; set; } // Tên kho xuất xe
    public string StorageCodeTo { get; set; } = ""; // Kho/Xưởng đóng thùng đích (StorageType = DT)
    public string? StorageNameTo { get; set; } // Tên kho/xưởng đóng thùng
    public string CBReqNo { get; set; } = ""; // Số Yêu cầu đóng thùng gốc đã duyệt (bắt buộc)
    public DateTime ExpectedStartDate { get; set; } // Ngày bắt đầu vận chuyển / đóng thùng dự kiến
    public DateTime ExpectedEndDate { get; set; } // Ngày giao dự kiến hoàn tất
    public string LoaiThung { get; set; } = ""; // Loại thùng cần đóng
    public string? ActualSpec { get; set; } // Cấu hình quy chuẩn thực tế
    public string TypeCB { get; set; } = "N"; // Tình trạng xe: "N" = Chassis chưa đóng thùng
    public StoRearrangeCBDtlStatus Status { get; set; } = StoRearrangeCBDtlStatus.Pending; // Trạng thái dòng xe
    public DateTime? ConfirmDate { get; set; } // Ngày xác nhận lịch giao/tiến độ
    public string? ConfirmBy { get; set; } // Người xác nhận
    public string? Remark { get; set; } // Ghi chú dòng xe
}

/// <summary>Mã lý do hủy xe ô tô tiêu chuẩn (DMS.Sales Mst_CarCancelType / CarCancelController / Error.idN.DMSSales.Car.cs): CCT01 Lỗi kỹ thuật nhà máy HTMV, CCT02 Khách hàng hủy HĐ, CCT03 Đại lý quá hạn thanh toán/bảo lãnh, CCT04 Tái điều phối lô khác, CCT05 Hư hại lưu kho bãi vận chuyển, CCT99 Lý do khác).</summary>
public enum CarCancelReasonCode { TechnicalFault = 0, CustomerCancel = 1, PaymentOverdue = 2, Reallocation = 3, StorageDamage = 4, Other = 5 }

/// <summary>Danh mục lý do hủy xe ô tô (DMS.Sales Mst_CarCancelType): quản lý mã, tên và mô tả nguyên nhân hủy xe trong hệ thống.</summary>
public sealed class CarCancelReasonMaster
{
    public long Id { get; set; }
    public string Code { get; set; } = ""; // Mã lý do hủy: CCT01, CCT02, CCT03, CCT04, CCT05, CCT99
    public string Name { get; set; } = ""; // Tên lý do hủy
    public string Description { get; set; } = ""; // Mô tả chi tiết
    public bool FlagActive { get; set; } = true; // Trạng thái kích hoạt
}

/// <summary>Hồ sơ định danh & trạng thái xe ô tô (DMS.Sales Car_Car / CarCancelController / Car.1.cs / FrmMngCarCancel / FrmCarCancel / FrmCapNhatTTHuyXe): quản lý thông tin xe, số khung VIN, dòng xe, đại lý, trạng thái hoạt động FlagActive ('1'=hoạt động, '0'=đã hủy), loại hủy xe CarCancelType, ngày hủy, lý do hủy, cùng các cờ điều hành xe hủy (FlagEarlyCancel '1'=xe sắp hủy, FlagMapVIN '1'=cho phép ghép VIN, FlagCarDeliveryOrder '1'=cho phép lập lệnh xuất xe, FlagTestCar '1'=xe lái thử demo) và thông số hoàn thành nghĩa vụ giao xe.</summary>
public sealed class CarRecord
{
    public long Id { get; set; }
    public Guid OrgId { get; set; }
    public string CarId { get; set; } = ""; // Mã định danh xe hệ thống (CarId, vd: 2605C64655BN7I-CKD, CAR2026-SF0988)
    public string Vin { get; set; } = ""; // Số khung xe (17 ký tự)
    public string ModelCode { get; set; } = ""; // Mã model xe (SF25, TU20, CR15, AC14...)
    public string ModelName { get; set; } = ""; // Tên model xe (Santa Fe, Tucson, Creta, Accent...)
    public string? SpecCode { get; set; } // Mã cấu hình xe (spec)
    public string? SpecDescription { get; set; } // Mô tả bản cấu hình xe
    public string? ColorCode { get; set; } // Mã màu ngoại thất
    public string? ColorName { get; set; } // Tên màu ngoại thất
    public string? EngineNo { get; set; } // Số máy
    public string DealerCode { get; set; } = ""; // Mã đại lý quản lý xe (VN001, VN002...)
    public string? DealerName { get; set; } // Tên đại lý
    public string? StorageCode { get; set; } // Mã kho lưu giữ hiện tại
    public string? StorageName { get; set; } // Tên kho
    public string? SoCode { get; set; } // Mã đơn hàng bán xe gắn với xe (Sales Order)
    public decimal PriceActual { get; set; } // Giá bán buôn thực tế của xe (VNĐ)
    public string PaymentStatus { get; set; } = "P"; // Trạng thái thanh toán của xe (Car_Car.PaymentStatus): 'P' = Chưa thanh toán xong, 'F' = Đã thanh toán đủ
    public string? MapVINRanking { get; set; } // Thứ tự ưu tiên Map VIN (Car_Car.MapVINRanking), chỉ sửa được khi xe chưa gán VIN
    public decimal PaymentDepositAmount { get; set; } // Số tiền cọc đã thanh toán hoàn tất (VNĐ)
    public decimal GuaranteeAmount { get; set; } // Giá trị bảo lãnh ngân hàng đã mở cho xe (VNĐ)
    public decimal DutyCompletedAmount { get; set; } // Giá trị đã hoàn thành nghĩa vụ giao xe (VNĐ) = cọc + bảo lãnh
    public string FlagActive { get; set; } = "1"; // '1' = Xe đang hoạt động bình thường, '0' = Xe đã hủy (CarCancel)
    public string FlagAllowChangeVIN { get; set; } = "1"; // '1' = Cho phép đổi VIN, '0' = Khóa đổi VIN
    public string? CarCancelType { get; set; } // Loại hủy xe (CCT01, CCT02... hoặc 'NONE' khi phục hồi)
    public string? CarCancelRemark { get; set; } // Ghi chú nguyên nhân hủy xe
    public DateTime? CarCancelDate { get; set; } // Ngày thực hiện hủy xe
    public string? CarCancelBy { get; set; } // Người thực hiện hủy xe
    public string FlagEarlyCancel { get; set; } = "0"; // '1' = Xe sắp bị hủy (cảnh báo quá hạn cọc/bảo lãnh), '0' = Bình thường
    public string FlagMapVIN { get; set; } = "1"; // '1' = Cho phép ghép số khung VIN vào đơn hàng, '0' = Khóa ghép VIN
    public string FlagCarDeliveryOrder { get; set; } = "1"; // '1' = Cho phép tạo lệnh xuất xe, '0' = Khóa tạo lệnh xuất xe
    public string FlagTestCar { get; set; } = "0"; // '1' = Xe demo lái thử đại lý, '0' = Xe thương mại thông thường
    public DateTime CreatedAt { get; set; } = DateTime.Now; // Ngày nhập thông tin xe
    public DateTime? LogLUDateTime { get; set; } // Thời gian cập nhật cuối
    public string? LogLUBy { get; set; } // Người cập nhật cuối
}

/// <summary>Nhật ký vết điều hành hủy & phục hồi xe ô tô (DMS.Sales Car_Car LogLUDateTime / LogLUBy / CarCancel audit): lưu vết từng hành động hủy xe, phục hồi xe đã hủy, cập nhật cờ điều hành xe.</summary>
public sealed class CarCancelLog
{
    public long Id { get; set; }
    public Guid OrgId { get; set; }
    public string CarId { get; set; } = "";
    public string Vin { get; set; } = "";
    public string Action { get; set; } = ""; // "Cancel" | "Restore" | "UpdateFlags"
    public string? CancelType { get; set; } // Loại hủy liên quan
    public string? Remark { get; set; } // Ghi chú thao tác
    public string? PerformedBy { get; set; } // Người thực hiện
    public DateTime PerformedAt { get; set; } = DateTime.Now; // Thời điểm thực hiện
}

/// <summary>Hình thức gán số khung VIN xe ô tô (DMS.Sales Car_Car MapVINType: A = Auto gán tự động theo thuật toán phân bổ, M = Manual gán thủ công).</summary>
public enum MapVinType { Auto = 0, Manual = 1 }

/// <summary>Phương thức / Tiêu chí thuật toán phân bổ Map VIN tự động (DMS.Sales Auto_MapVIN ATMVType / DMS40.0.20.MapVIN.cs: Normal FIFO, PriorityDeposit theo tỷ lệ cọc/nghĩa vụ, NewDealer ưu tiên đại lý mới, NewSpec ưu tiên spec mới, Special đặc biệt).</summary>
public enum MapVinMethod { Normal = 0, PriorityDeposit = 1, NewDealer = 2, NewSpec = 3, Special = 4, Manual = 5 }

/// <summary>Trạng thái phiên phân bổ Map VIN (DMS.Sales Auto_MapVIN: Pending -> Simulated -> Approved/Completed hoặc Cancelled).</summary>
public enum MapVinSessionStatus { Pending = 0, Simulated = 1, Approved = 2, Cancelled = 3 }

/// <summary>Trạng thái từng dòng xe trong phiên phân bổ Map VIN (DMS.Sales Auto_MapVIN_Car_Car: Pending, Mapped gán thành công, Unmapped đã gỡ VIN, Rejected không đủ điều kiện/hết xe kho).</summary>
public enum MapVinDetailStatus { Pending = 0, Mapped = 1, Unmapped = 2, Rejected = 3 }

/// <summary>Trạng thái tồn kho xe có số khung VIN vật lý (DMS.Sales Car_VIN: Available sẵn sàng trong kho, Mapped đã gán xe thương mại, Delivered đã xuất kho giao, Locked khóa kỹ thuật/sửa chữa).</summary>
public enum CarVinStatus { Available = 0, Mapped = 1, Delivered = 2, Locked = 3 }

/// <summary>Danh mục xe vật lý có số khung VIN trong kho bãi nhà máy/NPP (DMS.Sales Car_VIN / CarVINController / 05_QUAN_LY_XE.md): lưu thông tin kỹ thuật số khung 17 ký tự, số máy, model, bản cấu hình spec, màu sắc, kho lưu trữ, loại lắp ráp và trạng thái khả dụng cho Map VIN.</summary>
public sealed class CarVinInventory
{
    public long Id { get; set; }
    public Guid OrgId { get; set; }
    public string Vin { get; set; } = ""; // Số khung xe chuẩn 17 ký tự (VIN)
    public string ModelCode { get; set; } = ""; // Mã model (SF25, TU20, CR15, AC14...)
    public string ModelName { get; set; } = ""; // Tên dòng xe
    public string? SpecCode { get; set; } // Mã cấu hình xe (spec)
    public string? SpecDescription { get; set; } // Mô tả bản cấu hình
    public string? ColorCode { get; set; } // Mã màu ngoại thất
    public string? ColorName { get; set; } // Tên màu ngoại thất
    public string? EngineNo { get; set; } // Số máy
    public string StorageCode { get; set; } = ""; // Mã kho lưu bãi hiện tại
    public string? StorageName { get; set; } // Tên kho lưu bãi
    public string AssemblyStatus { get; set; } = "CKD"; // CBU | CKD | DKD
    public string? ProductionMonth { get; set; } // Tháng sản xuất (YYYYMM)
    public CarVinStatus Status { get; set; } = CarVinStatus.Available; // Trạng thái tồn kho
    public string? MappedCarId { get; set; } // Mã CarId xe thương mại đang gán giữ VIN
    public string? DeclarationNo { get; set; } // Số tờ khai hải quan nếu là xe nhập khẩu CBU (CT_TKHQ)
    public DateTime? CustomsClearanceDate { get; set; } // Ngày thông quan hải quan chính thức
    public string? LastPmtBDRefNo { get; set; } // Số bảng kê bảo dưỡng gần nhất (Car_VIN.PmtBDRefNo)
    public DateTime? LastPmtBDDate { get; set; } // Ngày bảo dưỡng gần nhất (Car_VIN.PmtBDDate)
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}

/// <summary>Phiên phân bổ & gán số khung VIN xe ô tô (DMS.Sales Auto_MapVIN / AutoMapVINController / DMS40.0.20.MapVIN.cs / 05_QUAN_LY_XE.md): quản lý đợt chạy phân bổ Map VIN cho các đơn đặt hàng/hợp đồng đại lý còn thiếu VIN, lưu mã đợt ATMVNo, phương thức phân bổ, tổng số xe xử lý và kết quả.</summary>
public sealed class MapVinSession
{
    public long Id { get; set; }
    public Guid OrgId { get; set; }
    public string SessionNo { get; set; } = ""; // Số đợt phân bổ Map VIN (ATMVNo, vd: 2609MV0001)
    public MapVinType MapType { get; set; } = MapVinType.Auto;
    public MapVinMethod Method { get; set; } = MapVinMethod.Normal;
    public MapVinSessionStatus Status { get; set; } = MapVinSessionStatus.Pending;
    public string? DealerCode { get; set; } // Lọc theo đại lý nhận phân bổ (tùy chọn)
    public string? ModelCode { get; set; } // Lọc theo model phân bổ (tùy chọn)
    public int TotalRequested { get; set; } // Tổng số xe cần gán VIN trong phiên
    public int TotalMapped { get; set; } // Số lượng xe đã gán thành công
    public int TotalUnmapped { get; set; } // Số lượng xe chưa gán được / từ chối
    public string? Remark { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public string? ApprovedBy { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public DateTime? CancelledAt { get; set; }
    public string? CancelReason { get; set; }

    public List<MapVinSessionDetail> Details { get; set; } = new();
}

/// <summary>Chi tiết xe trong phiên phân bổ Map VIN (DMS.Sales Auto_MapVIN_Car_Car / Auto_MapVIN_MapRoundSpec): lưu liên kết giữa xe thương mại trong đơn hàng và số khung VIN kho bãi được phân bổ.</summary>
public sealed class MapVinSessionDetail
{
    public long Id { get; set; }
    public long SessionId { get; set; }
    public string CarId { get; set; } = ""; // Mã xe thương mại Car_Car
    public string? SoCode { get; set; } // Mã đơn đặt hàng / hợp đồng bán xe
    public string DealerCode { get; set; } = ""; // Mã đại lý
    public string? DealerName { get; set; }
    public string ModelCode { get; set; } = ""; // Model xe
    public string ModelName { get; set; } = "";
    public string? SpecCode { get; set; }
    public string? ColorCode { get; set; }
    public string? ColorName { get; set; }
    public string? Vin { get; set; } // Số khung VIN được gán (17 ký tự)
    public string? EngineNo { get; set; } // Số máy
    public string? StorageCode { get; set; } // Kho xuất xe
    public string? StorageName { get; set; }
    public MapVinDetailStatus Status { get; set; } = MapVinDetailStatus.Pending;
    public string? RejectReason { get; set; } // Lý do chưa map được (hết xe kho, không khớp cấu hình...)
    public DateTime? MappedAt { get; set; }
    public string? MappedBy { get; set; }
}

/// <summary>Nhật ký kiểm toán vết Map VIN / Gỡ VIN / Đổi VIN xe ô tô (DMS.Sales MapVIN.cs Car_VIN_Map_HQ / Car_VIN_Unmap_HQ audit trail): lưu vết chi tiết từng thao tác gán hoặc gỡ số khung VIN.</summary>
public sealed class MapVinAuditLog
{
    public long Id { get; set; }
    public Guid OrgId { get; set; }
    public string CarId { get; set; } = ""; // Mã xe thương mại
    public string? Vin { get; set; } // Số khung VIN tác động
    public string Action { get; set; } = ""; // "ManualMap" | "AutoMap" | "Unmap" | "SwapVin"
    public string? SessionNo { get; set; } // Số đợt Map VIN liên quan
    public string? Remark { get; set; } // Ghi chú / lý do gán hoặc gỡ
    public string? PerformedBy { get; set; }
    public DateTime PerformedAt { get; set; } = DateTime.Now;
}

/// <summary>Loại đề nghị xuất hóa đơn bán lẻ xe ô tô (DMS.Sales Req_RequestInvoice ReqInvoiceType: Root = Hóa đơn gốc, Adjusted = Hóa đơn điều chỉnh, Replaced = Hóa đơn thay thế).</summary>
public enum RetailInvoiceType { Root = 0, Adjusted = 1, Replaced = 2 }

/// <summary>Trạng thái đề nghị xuất hóa đơn bán lẻ xe ô tô (DMS.Sales Req_RequestInvoice ReqInvoiceStatus: Pending = 'P' [Mới tạo], Issued = 'F' [Đã phát hành], Adjusted = 'A' [Đã điều chỉnh], Replaced = 'C' [Đã thay thế], Rejected = 'R' [Từ chối], Cancelled = 'CAN' [Đã hủy]).</summary>
public enum RetailInvoiceRequestStatus { Pending = 0, Issued = 1, Adjusted = 2, Replaced = 3, Rejected = 4, Cancelled = 5 }

/// <summary>Trạng thái hóa đơn điện tử e-Invoice (DMS.Sales Req_RequestInvoice InvoiceStatus / QInvoice: Draft = 'DRAFT' [Nháp], Pending = 'PENDING' [Đã cấp số chờ ký], Approved = 'APPROVED' [Đã ký số], SentTCT = 'SENTTCT' [Đã truyền cơ quan thuế], Issued = 'ISSUED' [Đã phát hành hợp lệ], Cancelled = 'CANCELED' [Đã hủy]).</summary>
public enum RetailInvoiceEInvoiceStatus { Draft = 0, Pending = 1, Approved = 2, SentTCT = 3, Issued = 4, Cancelled = 5 }

/// <summary>Loại điều chỉnh hóa đơn bán lẻ (DMS.Sales Req_RequestInvoice AdjType: None = Không, Increase = Tăng giá trị/thuế, Decrease = Giảm giá trị/thuế, Information = Điều chỉnh thông tin không thay đổi tiền).</summary>
public enum RetailInvoiceAdjType { None = 0, Increase = 1, Decrease = 2, Information = 3 }

/// <summary>Đề nghị xuất Hóa đơn GTGT Bán lẻ Xe ô tô & Dịch vụ Phụ kiện cho Khách hàng Đại lý (DMS.Sales Req_RequestInvoice / ReqRequestInvoiceController / ReqInvoice.cs / 11_HOA_DON_VAT.md): Đại lý lập đề nghị xuất hóa đơn GTGT điện tử (e-Invoice) cho khách hàng cá nhân / doanh nghiệp mua xe ô tô kèm phụ kiện lắp thêm hoặc gói bảo hiểm/dịch vụ theo Hợp đồng bán lẻ (DlrRetailContract) và Giao dịch bán lẻ (RetailDeal), hỗ trợ hóa đơn gốc (Root), hóa đơn điều chỉnh (Adjusted), hóa đơn thay thế (Replaced), quy trình cấp số hóa đơn điện tử qua QInvoice, ký số điện tử và truyền dữ liệu cơ quan thuế TCT, đồng bộ vào Deal bán lẻ.</summary>
public sealed class RetailInvoiceRequest
{
    public long Id { get; set; }
    public Guid OrgId { get; set; }
    public string ReqInvoiceNo { get; set; } = ""; // Số đề nghị xuất HĐ (PK ReqInvoiceNo, format: {yyMM}RI{seq:D4}, vd: 2603RI0001)
    public string DealerCode { get; set; } = ""; // Mã đại lý xuất hóa đơn
    public string? DealerName { get; set; } // Tên đại lý
    public RetailInvoiceType ReqInvoiceType { get; set; } = RetailInvoiceType.Root; // Loại đề nghị: Gốc, Điều chỉnh, Thay thế
    public string? ReqInvoiceBaseNo { get; set; } // Số đề nghị gốc liên quan khi Điều chỉnh hoặc Thay thế
    public string? DlrContractNo { get; set; } // Số hợp đồng bán lẻ xe ô tô liên quan (Dlr_Contract)
    public string? DealNo { get; set; } // Số giao dịch bán lẻ liên kết (DLS_Deal)
    public string CustomerCode { get; set; } = ""; // Mã khách hàng
    public string CustomerName { get; set; } = ""; // Tên khách hàng sở hữu xe
    public string CustomerType { get; set; } = "INDIVIDUAL"; // INDIVIDUAL | ORGANIZATION
    public string? CustomerAddress { get; set; } // Địa chỉ xuất hóa đơn
    public string? CustomerEmail { get; set; } // Email nhận hóa đơn điện tử
    public string? CustomerPhone { get; set; } // Điện thoại liên hệ
    public string? MST { get; set; } // Mã số thuế DN hoặc số CCCD/CMND cá nhân
    public string? TInvoiceCode { get; set; } // Mẫu hóa đơn điện tử (vd: 1/001, 01GTKT0/001)
    public string? InvoiceCode { get; set; } // Mã tra cứu hóa đơn điện tử (LookupCode / GUID tra cứu QInvoice)
    public string? InvoiceNo { get; set; } // Số hóa đơn chính thức (7-8 chữ số, vd: 0001234)
    public DateTime? InvoiceDate { get; set; } // Ngày hóa đơn
    public string? InvoiceSign { get; set; } // Ký hiệu hóa đơn (vd: 1C26TAA, C26TMB)
    public string? FormNo { get; set; } // Mẫu số hóa đơn (vd: 1/001)
    public RetailInvoiceEInvoiceStatus InvoiceStatus { get; set; } = RetailInvoiceEInvoiceStatus.Draft; // Trạng thái HĐĐT
    public string? InvoiceBaseNo { get; set; } // Số hóa đơn gốc khi điều chỉnh/thay thế
    public RetailInvoiceAdjType AdjType { get; set; } = RetailInvoiceAdjType.None; // Loại điều chỉnh
    public string? Remark { get; set; } // Lý do xuất / điều chỉnh / thay thế
    public RetailInvoiceRequestStatus ReqInvoiceStatus { get; set; } = RetailInvoiceRequestStatus.Pending; // Trạng thái đề nghị
    public string FlagInvoice { get; set; } = "0"; // "0" = Chưa xuất xong, "1" = Đã phát hành hoàn tất
    public int FlagIsQInvoice { get; set; } = 1; // 1 = Tích hợp hệ thống QInvoice điện tử, 0 = Xuất ngoài
    public int QtyCtrCarId { get; set; } // Số lượng xe xuất trên hóa đơn
    public decimal TotalTPBeforeVAT { get; set; } // Tổng tiền hàng trước thuế VAT (xe + phụ kiện)
    public decimal TotalValVAT { get; set; } // Tổng tiền thuế GTGT (VNĐ)
    public decimal TotalTPAfterVAT { get; set; } // Tổng cộng tiền thanh toán sau VAT (VNĐ)
    public string? TaxAuthorityCode { get; set; } // Mã cơ quan thuế cấp khi truyền CQT thành công
    public string? InvoiceFileUrl { get; set; } // Đường dẫn file PDF bản thể hiện hóa đơn điện tử
    public string? InvoiceFileName { get; set; } // Tên file PDF hóa đơn
    public string? CreatedBy { get; set; } // Người lập đề nghị
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public string? IssuedBy { get; set; } // Người cấp số hóa đơn
    public DateTime? IssuedAt { get; set; }
    public string? ApprovedBy { get; set; } // Người ký số hóa đơn
    public DateTime? ApprovedAt { get; set; }
    public string? TaxTransmittedBy { get; set; } // Người gửi cơ quan thuế
    public DateTime? TaxTransmittedAt { get; set; }
    public string? CancelReason { get; set; } // Lý do hủy hóa đơn
    public string? CancelledBy { get; set; }
    public DateTime? CancelledAt { get; set; }
    public DateTime? LUDateTime { get; set; }
    public string? LUBy { get; set; }

    public List<RetailInvoiceRequestDetail> Details { get; set; } = new();
    public List<RetailInvoiceRequestProduct> Products { get; set; } = new();
}

/// <summary>Chi tiết dòng xe ô tô trong Đề nghị xuất hóa đơn bán lẻ (DMS.Sales Req_RequestInvoiceDtl): quản lý số khung VIN, thông tin đăng kiểm CQNo/số PXX FGFormNo, số hóa đơn buôn NPP HTC, đơn giá xe trước VAT, tỷ lệ thuế và thành tiền sau thuế.</summary>
public sealed class RetailInvoiceRequestDetail
{
    public long Id { get; set; }
    public long RetailInvoiceRequestId { get; set; }
    public string ReqInvoiceNo { get; set; } = "";
    public string CtrCarId { get; set; } = ""; // Mã xe hợp đồng bán lẻ (CarId trong Dlr_ContractCar)
    public string Vin { get; set; } = ""; // Số khung VIN chuẩn 17 ký tự
    public string BrandName { get; set; } = "Hyundai"; // Nhãn hiệu xe
    public string CarType { get; set; } = "Ô tô con"; // Loại xe: Ô tô con, Xe tải, SUV...
    public string ModelCode { get; set; } = ""; // Mã model (SF25, TU20, CR15, AC14...)
    public string ModelName { get; set; } = ""; // Tên thương mại dòng xe (Santa Fe, Tucson, Creta...)
    public string? SpecCode { get; set; } // Mã cấu hình xe (spec)
    public string? SpecDescription { get; set; } // Mô tả bản xe (1.6 Turbo HTRAC, 2.0 AT Tiêu chuẩn...)
    public string? ColorCode { get; set; } // Mã màu ngoại thất
    public string? ColorName { get; set; } // Tên màu xe
    public string? EngineNo { get; set; } // Số máy
    public int NumberOfSeats { get; set; } = 5; // Số chỗ ngồi
    public string? HTCInvoiceNo { get; set; } // Số hóa đơn bán buôn NPP HTC xuất tương ứng
    public DateTime? HTCInvoiceDate { get; set; } // Ngày hóa đơn bán buôn
    public string? ProductionYearActual { get; set; } // Năm sản xuất xe
    public string? CQNo { get; set; } // Số chứng nhận chất lượng xuất xưởng / Đăng kiểm (CBU)
    public string? FGFormNo { get; set; } // Số phiếu xuất xưởng PXX (CKD)
    public decimal UPBeforeVAT { get; set; } // Đơn giá xe trước VAT (VNĐ)
    public decimal VatRate { get; set; } = 10m; // Tỷ lệ thuế GTGT (%)
    public decimal ValVAT { get; set; } // Tiền thuế GTGT (VNĐ)
    public decimal UPAfterVAT { get; set; } // Đơn giá xe sau VAT (VNĐ)
    public string Status { get; set; } = "Pending"; // Pending | Approved | Invoiced | Cancelled
    public string? Remark { get; set; }
}

/// <summary>Chi tiết dòng hàng hóa, phụ kiện & dịch vụ kèm theo xe trong Đề nghị xuất hóa đơn bán lẻ (DMS.Sales Req_RequestInvoicePrd): quản lý các món phụ kiện (phim cách nhiệt, camera hành trình, lót sàn...) hoặc dịch vụ gia tăng được xuất cùng trên hóa đơn GTGT.</summary>
public sealed class RetailInvoiceRequestProduct
{
    public long Id { get; set; }
    public long RetailInvoiceRequestId { get; set; }
    public string ReqInvoiceNo { get; set; } = "";
    public int Idx { get; set; } = 1; // STT dòng phụ kiện/dịch vụ
    public string ProductCode { get; set; } = ""; // Mã phụ kiện/dịch vụ (PK-FILM3M, PK-CAMDASH, DV-COATING...)
    public string ProductName { get; set; } = ""; // Tên hàng hóa / phụ kiện / dịch vụ
    public string Unit { get; set; } = "Gói"; // Đơn vị tính: Bộ, Gói, Chiếc, Lần...
    public int Qty { get; set; } = 1; // Số lượng
    public decimal VatRate { get; set; } = 10m; // Tỷ lệ thuế VAT (%)
    public decimal UnitPrice { get; set; } // Đơn giá trước VAT
    public decimal TPBeforeVAT { get; set; } // Thành tiền trước VAT = Qty * UnitPrice
    public decimal ValVAT { get; set; } // Tiền thuế VAT
    public decimal TPAfterVAT { get; set; } // Thành tiền sau VAT = TPBeforeVAT + ValVAT
    public bool FlagIsProduct { get; set; } = true; // true = Phụ kiện/Hàng hóa vật lý, false = Dịch vụ công
    public string? Remark { get; set; }
}

/// <summary>Hình thức đề nghị giải chấp xe ô tô (DMS.Sales RD_ReqRedeem / RD_ReqRedeemDtl TypeDMReq: Direct = Giải chấp trực tiếp, Guarantee = Giải chấp theo thư bảo lãnh ngân hàng).</summary>
public enum CarRedeemType { Direct = 0, Guarantee = 1 }

/// <summary>Trạng thái đề nghị giải chấp xe ô tô (DMS.Sales RD_ReqRedeem DMReqStatus: Pending = 'P' [Chờ duyệt], Approved = 'A' [Đã duyệt hoàn tất], Rejected = 'R' [Từ chối], Cancelled = 'C' [Hủy]).</summary>
public enum CarRedeemStatus { Pending = 0, Approved = 1, Rejected = 2, Cancelled = 3 }

/// <summary>Trạng thái dòng xe trong đề nghị giải chấp (DMS.Sales RD_ReqRedeemDtl DMReqDtlStatus: Pending = 'P' [Chờ duyệt], Approved = 'A' [Đã giải chấp], Rejected = 'R' [Từ chối], Cancelled = 'C' [Hủy]).</summary>
public enum CarRedeemDtlStatus { Pending = 0, Approved = 1, Rejected = 2, Cancelled = 3 }

/// <summary>Quản lý Đề nghị Giải chấp Xe ô tô Thế chấp Ngân hàng / Nhà phân phối (DMS.Sales RD_ReqRedeem / RDReqRedeemController / Redeem.cs / Thế chấp, Giải chấp, Bàn giao hồ sơ.xlsx): quản lý thủ tục giải chấp tài sản bảo đảm ngân hàng để rút giấy tờ xe ô tô gốc (CQ, phiếu kiểm tra chất lượng, tờ khai hải quan), chuyển ngân hàng bàn giao tài sản sang HTC.HO và bàn giao xe cho khách hàng.</summary>
public sealed class CarRedeemRequest
{
    public long Id { get; set; }
    public Guid OrgId { get; set; }
    public string ReqDMNo { get; set; } = ""; // Số ĐN giải chấp (PK ReqDMNo, format: {yyMM}RDM{seq:D5}, vd: 2603RDM00001)
    public string DealerCode { get; set; } = ""; // Mã đại lý đề nghị
    public string? DealerName { get; set; } // Tên đại lý
    public CarRedeemStatus Status { get; set; } = CarRedeemStatus.Pending; // Trạng thái đề nghị
    public CarRedeemType RedeemType { get; set; } = CarRedeemType.Direct; // Loại giải chấp (Trực tiếp / Bảo lãnh)
    public int TotalCars { get; set; } // Tổng số lượng xe trong đề nghị
    public int ApprovedCars { get; set; } // Số lượng xe đã được giải chấp hoàn tất
    public string? BankCode { get; set; } // Mã ngân hàng thế chấp / bàn giao tài sản chính
    public string? BankName { get; set; } // Tên ngân hàng thế chấp
    public string? Remark { get; set; } // Ghi chú đề nghị giải chấp
    public string? CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public string? ApprovedBy { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public string? RejectedBy { get; set; }
    public DateTime? RejectedAt { get; set; }
    public string? RejectReason { get; set; }
    public string? CancelledBy { get; set; }
    public DateTime? CancelledAt { get; set; }
    public string? CancelReason { get; set; }

    public List<CarRedeemRequestDetail> Details { get; set; } = new();
}

/// <summary>Chi tiết dòng xe ô tô trong Đề nghị giải chấp (DMS.Sales RD_ReqRedeemDtl): theo dõi từng số khung VIN, mã định danh xe CarId, số máy, số kiểm định CQNo, ngân hàng thế chấp ban đầu (MortageBankCode: VCB, TCB, CTG, BIDV...), mã Đề nghị giao giấy tờ DRListCode, số bảo lãnh và trạng thái duyệt giải chấp (khi duyệt đổi MortageBankCode thành HTC.HO).</summary>
public sealed class CarRedeemRequestDetail
{
    public long Id { get; set; }
    public long RedeemRequestId { get; set; }
    public string ReqDMNo { get; set; } = ""; // Số ĐN giải chấp
    public string CarId { get; set; } = ""; // Mã xe thương mại hệ thống Car_Car
    public string Vin { get; set; } = ""; // Số khung VIN chuẩn 17 ký tự
    public string ModelCode { get; set; } = ""; // Mã model (SF25, TU20, CR15, AC14...)
    public string ModelName { get; set; } = ""; // Tên model xe
    public string? SpecCode { get; set; } // Mã cấu hình
    public string? SpecDescription { get; set; } // Mô tả bản xe (AC_SpecDescription)
    public string? ColorCode { get; set; } // Mã màu
    public string? ColorName { get; set; } // Tên màu xe
    public string? EngineNo { get; set; } // Số máy
    public string? CQNo { get; set; } // Số chứng nhận chất lượng xuất xưởng / Đăng kiểm
    public string? CONo { get; set; } // Số chứng nhận nguồn gốc xuất xứ
    public string? DeclarationNo { get; set; } // Số tờ khai hải quan (TKHQ)
    public string MortageBankCode { get; set; } = ""; // Ngân hàng bàn giao tài sản ban đầu; khi duyệt chuyển sang 'HTC.HO'
    public string? MortageBankName { get; set; } // Tên ngân hàng thế chấp
    public CarRedeemType TypeDMReq { get; set; } = CarRedeemType.Direct; // Loại giải chấp dòng xe (Direct / Guarantee)
    public string DealerCode { get; set; } = ""; // Đại lý sở hữu xe
    public string? DealerName { get; set; }
    public string? GuaranteeNo { get; set; } // Số thư bảo lãnh thanh toán liên quan (Pmt_Guarantee)
    public string? DRListCode { get; set; } // Số Đề nghị giao giấy tờ xe (Car_DocReqList)
    public string? DocumentsStatus { get; set; } = "Full"; // Trạng thái giấy tờ xe (Full / Approved)
    public CarRedeemDtlStatus Status { get; set; } = CarRedeemDtlStatus.Pending; // Trạng thái dòng: Pending -> Approved
    public DateTime? ApprovedAt { get; set; }
    public string? ApprovedBy { get; set; }
    public string? Remark { get; set; }
}

/// <summary>Trạng thái Yêu cầu Bảo hiểm Xe ô tô (DMS.Sales Ins_InsuranceReq InsReqStatus: Pending = 'P' [Chờ duyệt], Approved = 'A' [Đã duyệt], Rejected = 'R' [Từ chối], Cancelled = 'C' [Hủy]).</summary>
public enum CarInsuranceStatus { Pending = 0, Approved = 1, Rejected = 2, Cancelled = 3 }

/// <summary>Trạng thái từng dòng xe trong Yêu cầu Bảo hiểm (DMS.Sales Ins_InsuranceReqDtl InsReqDtlStatus: Pending = 'P', Approved = 'A', Rejected = 'R', Cancelled = 'C').</summary>
public enum CarInsuranceDtlStatus { Pending = 0, Approved = 1, Rejected = 2, Cancelled = 3 }

/// <summary>Loại chứng từ gốc phát sinh bảo hiểm xe (DMS.Sales TConst.Sto_TranspReqType: CARTRANSPORT = Lệnh giao xe, CARRETRIEVE = Lệnh thu hồi, STORAGEREARRANGE = Điều chuyển kho, STORAGEREARRCB = Điều chuyển đóng thùng).</summary>
public enum CarInsuranceRefOrdType { CarTransport = 0, CarRetrieve = 1, StorageRearrange = 2, StorageRearrangeCB = 3 }

/// <summary>Yêu cầu Bảo hiểm Xe ô tô Vận chuyển & Giao nhận (DMS.Sales Ins_InsuranceReq + Ins_InsuranceReqDtl / Master.cs / 08_BAO_HIEM.md): quản lý quy trình gửi yêu cầu bảo hiểm cho các lô xe ô tô vận chuyển (theo Lệnh giao xe DO, Thu hồi xe, Điều chuyển kho hoặc Điều chuyển đóng thùng) tới các công ty bảo hiểm (BIC, PTI, PJICO, Bảo Việt...), duyệt bảo hiểm kèm số đơn PolicyNo, quản lý phí bảo hiểm và kiểm soát an toàn vận tải.</summary>
public sealed class CarInsuranceRequest
{
    public long Id { get; set; }
    public Guid OrgId { get; set; }
    public string InsReqNo { get; set; } = ""; // Mã yêu cầu bảo hiểm (PK InsReqNo, format: {yyMM}IN{seq:D5}, vd: 2603IN00001)
    public string InsCompanyCode { get; set; } = ""; // Mã công ty bảo hiểm: BIC, PTI, PJICO, BV, MIC...
    public string? InsCompanyName { get; set; } // Tên công ty bảo hiểm
    public string InsTypeCode { get; set; } = ""; // Mã loại hình bảo hiểm: BHVC (Vật chất), BHTNDS (TNDS), BHVT (Vận chuyển)...
    public string? InsTypeName { get; set; } // Tên loại hình bảo hiểm
    public DateTime EffectiveDate { get; set; } = DateTime.Today; // Ngày bắt đầu hiệu lực bảo hiểm
    public DateTime? ExpiryDate { get; set; } // Ngày hết hạn bảo hiểm
    public int TotalCars { get; set; } // Tổng số xe trong yêu cầu
    public int ApprovedCars { get; set; } // Số xe đã được duyệt bảo hiểm
    public decimal TotalAmount { get; set; } // Tổng giá trị bảo hiểm các xe (tổng UnitPrice)
    public decimal TotalPremiumAmount { get; set; } // Tổng phí bảo hiểm phải đóng (tổng InsAmount)
    public string? PolicyNo { get; set; } // Số đơn / hợp đồng bảo hiểm được công ty bảo hiểm cấp
    public DateTime? PolicyDate { get; set; } // Ngày cấp đơn bảo hiểm
    public CarInsuranceStatus Status { get; set; } = CarInsuranceStatus.Pending; // Trạng thái yêu cầu: P -> A / R / C
    public string? Remark { get; set; } // Ghi chú yêu cầu bảo hiểm
    public string? RejectReason { get; set; } // Lý do từ chối duyệt
    public DateTime? RejectedDate { get; set; }
    public string? RejectedBy { get; set; }
    public string? CancelReason { get; set; } // Lý do hủy yêu cầu
    public DateTime? CancelledDate { get; set; }
    public string? CancelledBy { get; set; }
    public DateTime CreatedDate { get; set; } = DateTime.Now; // Ngày lập yêu cầu
    public string? CreatedBy { get; set; }
    public DateTime? ApprovedDate { get; set; } // Ngày duyệt yêu cầu bảo hiểm
    public string? ApprovedBy { get; set; }
    public DateTime? LUDateTime { get; set; }
    public string? LUBy { get; set; }

    public List<CarInsuranceRequestDetail> Details { get; set; } = new();
}

/// <summary>Chi tiết dòng xe ô tô trong Yêu cầu Bảo hiểm (DMS.Sales Ins_InsuranceReqDtl): quản lý số khung VIN xe, mã định danh CarId, loại và số chứng từ gốc (Lệnh giao DO, Thu hồi, Điều chuyển kho, Đóng thùng), đơn vị vận tải, giá trị xe tính bảo hiểm, tỷ lệ phí bảo hiểm (Rate), số ngày bảo hiểm và số tiền phí bảo hiểm (InsAmount).</summary>
public sealed class CarInsuranceRequestDetail
{
    public long Id { get; set; }
    public long InsuranceRequestId { get; set; }
    public string InsReqNo { get; set; } = ""; // Mã yêu cầu bảo hiểm
    public string CarId { get; set; } = ""; // Mã xe thương mại hệ thống Car_Car
    public string Vin { get; set; } = ""; // Số khung VIN xe (17 ký tự)
    public string? ModelCode { get; set; } // Mã model
    public string ModelName { get; set; } = ""; // Tên model xe (Santa Fe, Tucson, Creta, Accent...)
    public string? SpecCode { get; set; } // Mã cấu hình xe (spec)
    public string? SpecDescription { get; set; } // Mô tả bản xe
    public string? ColorCode { get; set; } // Mã màu xe
    public string? ColorName { get; set; } // Tên màu xe
    public string? EngineNo { get; set; } // Số máy
    public CarInsuranceRefOrdType RefOrdType { get; set; } = CarInsuranceRefOrdType.CarTransport; // Loại lệnh nguồn: CARTRANSPORT, CARRETRIEVE, STORAGEREARRANGE, STORAGEREARRCB
    public string RefOrdNo { get; set; } = ""; // Số lệnh nguồn tương ứng
    public string? TransporterCode { get; set; } // Mã đơn vị vận chuyển
    public string? TransporterName { get; set; } // Tên đơn vị vận chuyển
    public string? LocationFrom { get; set; } // Địa điểm bốc xe xuất phát
    public string? LocationTo { get; set; } // Địa điểm nhận xe đến
    public DateTime? ExpectedStartDate { get; set; } // Ngày dự kiến bắt đầu vận chuyển
    public decimal Price { get; set; } // Giá trị xe tính bảo hiểm (VNĐ)
    public decimal Rate { get; set; } = 0.15m; // Tỷ lệ phí bảo hiểm (%)
    public int InsuranceDay { get; set; } = 30; // Số ngày bảo hiểm
    public decimal InsAmount { get; set; } // Phí bảo hiểm xe = Price * Rate / 100 (VNĐ)
    public string? PolicyNo { get; set; } // Số đơn bảo hiểm riêng cho xe (nếu có)
    public CarInsuranceDtlStatus Status { get; set; } = CarInsuranceDtlStatus.Pending; // Trạng thái dòng xe: P -> A / R / C
    public string? Remark { get; set; } // Ghi chú dòng xe
    public string? ApprovedBy { get; set; }
    public DateTime? ApprovedDate { get; set; }
}

/// <summary>Trạng thái Kế hoạch Vận tải xe ô tô (DMS.Sales Sto_TranspPlan TPStatus: Pending = 'P' [Chờ duyệt điều phối], Approved = 'A' [Đã duyệt kế hoạch], Finished = 'F' [Hoàn thành vận chuyển], Cancelled = 'C' [Hủy kế hoạch]).</summary>
public enum TransportPlanStatus { Pending = 0, Approved = 1, Finished = 2, Cancelled = 3 }

/// <summary>Trạng thái tiếp nhận của Đơn vị Vận tải đối với Kế hoạch (DMS.Sales Sto_TranspPlan TransporterStatus: Pending = 'P' [Chờ tiếp nhận], Accepted = 'A' [Đơn vị vận tải đã tiếp nhận xe], Delivering = 'D' [Đang trên đường vận chuyển], Finished = 'F' [Đã giao xe hoàn tất], Rejected = 'R' [Từ chối]).</summary>
public enum TransporterPlanStatus { Pending = 0, Accepted = 1, Delivering = 2, Finished = 3, Rejected = 4 }

/// <summary>Phương thức vận chuyển kế hoạch (DMS.Sales Sto_TranspPlan TPType: Road = 'ROAD' [Đường bộ], Water = 'WATER' [Đường thủy], Rail = 'RAIL' [Đường sắt]).</summary>
public enum TransportPlanType { Road = 0, Water = 1, Rail = 2 }

/// <summary>Kế hoạch Vận tải Xe ô tô (DMS.Sales Sto_TranspPlan / TranspPlan.cs / 09_VAN_CHUYEN.md): Quản lý việc lập kế hoạch vận chuyển xe từ kho/nhà máy đến đại lý theo kế hoạch kiểm tra chất lượng CQ, kế hoạch ngày vận chuyển dự kiến, phân bổ đơn vị vận tải, tự động sinh tuyến đường vận chuyển Tỉnh/Huyện đi - đến, điều phối kế hoạch theo 3 góc nhìn (Kế hoạch, Bán hàng, Logistics), mapping số khung VIN thực tế khi xe hoàn tất kiểm định và phê duyệt kế hoạch vận tải.</summary>
public sealed class TransportPlan
{
    public long Id { get; set; }
    public Guid OrgId { get; set; }
    public string PlanNo { get; set; } = ""; // Mã kế hoạch vận tải ({yyMM}TP{seq:D4}, vd: 2603TP0001)
    public string VINPlan { get; set; } = ""; // Mã số khung kế hoạch / VIN dự kiến
    public string? VIN { get; set; } // Số khung thực tế (17 ký tự, null nếu chưa gán VIN thật)
    public bool FlagRealVin { get; set; } // Cờ đã gán VIN thật: false = VIN kế hoạch, true = đã gán số khung thật
    public string? CarId { get; set; } // Mã định danh xe trong hệ thống
    public string? RefNo { get; set; } // Mã tham chiếu (Số đơn hàng, Số hợp đồng, Số WO)
    public string? LCTemp { get; set; } // Số LC tạm
    public string ModelCode { get; set; } = ""; // Mã model (TUCSON, SANTAFE, CRETA, ACCENT...)
    public string ModelName { get; set; } = ""; // Tên model xe
    public string? SpecCode { get; set; } // Mã phiên bản / ActualSpec
    public string? ColorCode { get; set; } // Mã màu xe
    public string? ColorName { get; set; } // Tên màu xe
    public string StorageCode { get; set; } = ""; // Mã kho xuất phát
    public string? StorageName { get; set; } // Tên kho xuất phát
    public string DealerCode { get; set; } = ""; // Mã đại lý tiếp nhận
    public string? DealerName { get; set; } // Tên đại lý tiếp nhận
    public DateTime? CQStartDate { get; set; } // Ngày bắt đầu kiểm tra chất lượng Car Quality (CQ)
    public DateTime ExpectedDate { get; set; } // Ngày vận chuyển dự kiến (ExpectedDate >= CQStartDate và >= Today khi tạo)
    public DateTime? ActualDate { get; set; } // Ngày vận chuyển thực tế hoàn thành
    public string? TransporterCode { get; set; } // Mã đơn vị vận tải (Mst_Transporter)
    public string? TransporterName { get; set; } // Tên đơn vị vận tải
    public TransportPlanType TPType { get; set; } = TransportPlanType.Road; // Phương thức vận chuyển: Road, Water, Rail
    public string? FProvinceCode { get; set; } // Mã tỉnh/thành xuất phát (từ Kho)
    public string? FProvinceName { get; set; } // Tên tỉnh/thành xuất phát
    public string? FDistrictCode { get; set; } // Mã quận/huyện xuất phát
    public string? FDistrictName { get; set; } // Tên quận/huyện xuất phát
    public string? TProvinceCode { get; set; } // Mã tỉnh/thành đích đến (từ Đại lý)
    public string? TProvinceName { get; set; } // Tên tỉnh/thành đích đến
    public string? TDistrictCode { get; set; } // Mã quận/huyện đích đến
    public string? TDistrictName { get; set; } // Tên quận/huyện đích đến
    public TransportPlanStatus Status { get; set; } = TransportPlanStatus.Pending; // Trạng thái kế hoạch: Pending -> Approved -> Finished / Cancelled
    public TransporterPlanStatus TransporterStatus { get; set; } = TransporterPlanStatus.Pending; // Trạng thái đơn vị vận tải
    public string? Remark { get; set; } // Ghi chú
    public string? CancelReason { get; set; } // Lý do hủy kế hoạch
    public DateTime? CancelledDate { get; set; }
    public string? CancelledBy { get; set; }
    public DateTime CreatedDate { get; set; } = DateTime.Now;
    public string? CreatedBy { get; set; }
    public DateTime? ApprovedDate { get; set; }
    public string? ApprovedBy { get; set; }
    public DateTime? FinishedDate { get; set; }
    public string? FinishedBy { get; set; }
    public DateTime? LUDateTime { get; set; }
    public string? LUBy { get; set; }
}

/// <summary>Trạng thái Biên bản Đối soát Thưởng Bán hàng Xe Ô tô (DMS.Sales Rec_SaleAwardMinutes SaleAwardMinutesStatus: Pending = 'Pending' [Chờ duyệt cấp 1], Approved1 = 'Approved1' [Trưởng phòng duyệt cấp 1], Approved2 = 'Approved2' [Lãnh đạo NPP duyệt cấp 2], Finished = 'Finished' [Đại lý đã ký nhận hoàn tất], Rejected = 'Rejected' [Từ chối duyệt], Canceled = 'Canceled' [Đã hủy]).</summary>
public enum SaleAwardMinutesStatus
{
    Pending = 0,
    Approved1 = 1,
    Approved2 = 2,
    Finished = 3,
    Rejected = 4,
    Canceled = 5
}

/// <summary>Biên bản Đối soát Thưởng Bán hàng Xe Ô tô Đại lý - NPP (DMS.Sales Rec_SaleAwardMinutes / RecSaleAwardMinutesController / Minutes.cs / RecSaleAwardMinutes.txt): Quản lý việc lập biên bản đối soát và quyết toán thưởng doanh số, thưởng kích cầu bán buôn/bán lẻ theo từng công văn/quyết định (DocumentNo), quy trình phê duyệt 2 cấp NPP (Trưởng phòng Approve1HQ, Lãnh đạo Approve2HQ), Đại lý ký nhận/ký số điện tử (FinishDL nộp file PDF), từ chối duyệt (RejectHQ) và hủy biên bản (CancelHQ).</summary>
public sealed class SaleAwardMinutes
{
    public long Id { get; set; }
    public Guid OrgId { get; set; }
    public string SaleAwardMinutesNo { get; set; } = ""; // Mã biên bản thưởng ({yyMM}SAM{seq:D5}, vd: 2603SAM00001)
    public string DealerCode { get; set; } = ""; // Mã đại lý
    public string DealerName { get; set; } = ""; // Tên đại lý
    public SaleAwardMinutesStatus SaleAwardMinutesStatus { get; set; } = SaleAwardMinutesStatus.Pending; // Trạng thái biên bản
    public int TotalCars { get; set; } // Tổng số xe trong biên bản
    public decimal TotalAwardAmount { get; set; } // Tổng tiền thưởng (VNĐ)
    public string? FilePath { get; set; } // Đường dẫn lưu trữ file biên bản ký
    public string? FileName { get; set; } // Tên file đã ký nộp qua FinishDL
    public string? FileUrl { get; set; } // Đường dẫn tải file biên bản
    public string? Remark { get; set; } // Ghi chú biên bản
    public string? RejectReason { get; set; } // Lý do NPP từ chối duyệt
    public DateTime? RejectDTime { get; set; }
    public string? RejectBy { get; set; }
    public string? CancelReason { get; set; } // Lý do hủy biên bản
    public DateTime? CancelDTime { get; set; }
    public string? CancelBy { get; set; }
    public DateTime CreateDTime { get; set; } = DateTime.Now;
    public string CreateBy { get; set; } = "";
    public DateTime? Appr1DTime { get; set; }
    public string? Appr1By { get; set; }
    public DateTime? Appr2DTime { get; set; }
    public string? Appr2By { get; set; }
    public DateTime? FinishDTime { get; set; }
    public string? FinishBy { get; set; }
    public DateTime? LogLUDateTime { get; set; }
    public string? LogLUBy { get; set; }

    public List<SaleAwardMinutesDetail> Details { get; set; } = new();
}

/// <summary>Chi tiết dòng xe trong Biên bản Đối soát Thưởng Bán hàng (DMS.Sales Rec_SaleAwardMinutesDtl): quản lý số khung VIN xe, mã xe CarId, số công văn thưởng (DocumentNo bắt buộc), số tiền thưởng (AmountAward > 0 bắt buộc), thông tin model, phiên bản, màu sắc, năm sản xuất, ngày bán lẻ/giao xe từ giao dịch bán lẻ (DeliveryDate).</summary>
public sealed class SaleAwardMinutesDetail
{
    public long Id { get; set; }
    public long SaleAwardMinutesId { get; set; }
    public string SaleAwardMinutesNo { get; set; } = ""; // Mã biên bản thưởng
    public string Vin { get; set; } = ""; // Số khung xe (17 ký tự)
    public string? CarId { get; set; } // Mã xe thương mại trong Car_Car
    public string DealerCode { get; set; } = ""; // Mã đại lý
    public string? DealerName { get; set; } // Tên đại lý
    public string DocumentNo { get; set; } = ""; // Số công văn / quyết định thưởng (bắt buộc)
    public decimal AmountAward { get; set; } // Số tiền thưởng cho xe (bắt buộc > 0)
    public string? ModelCode { get; set; } // Mã model xe
    public string? ModelName { get; set; } // Tên model xe
    public string? SpecCode { get; set; } // Mã spec / phiên bản xe
    public string? SpecDescription { get; set; } // Mô tả xe
    public string? ColorCode { get; set; } // Mã màu
    public string? ColorName { get; set; } // Tên màu
    public int? VinYear { get; set; } // Năm sản xuất
    public DateTime? DeliveryDate { get; set; } // Ngày giao xe bán lẻ (từ Dls_DealDetail)
    public string? DealNo { get; set; } // Mã giao dịch bán lẻ liên kết
    public string? Remark { get; set; } // Ghi chú dòng xe
    public DateTime? LogLUDateTime { get; set; }
    public string? LogLUBy { get; set; }
}

/// <summary>Trạng thái Kế hoạch Kinh doanh Đại lý (DMS.Sales BPL_BusinessPlan BusinessPlanStatus: Pending = 'P' [Chờ duyệt], Approved1 = 'A1' [Trưởng phòng duyệt cấp 1], Approved2 = 'A2' [Lãnh đạo Khối duyệt cấp 2 / Chốt kế hoạch], Cancelled = 'C' [Hủy kế hoạch]).</summary>
public enum BusinessPlanStatus
{
    Pending = 0,
    Approved1 = 1,
    Approved2 = 2,
    Cancelled = 3
}

/// <summary>Kế hoạch Kinh doanh Bán buôn / Bán lẻ Xe Ô tô Đại lý - NPP (DMS.Sales BPL_BusinessPlan / BPLBusinessPlanController / BusinessPlan.cs / BPLBusinessPlan.txt): Quản lý chỉ tiêu kế hoạch kinh doanh năm (YearPlan) cho từng Đại lý (DealerCode), phân rã sản lượng bán lẻ (Retail - Deal) và bán buôn (Wholesale - Order) theo 12 tháng (M1..M12) cho từng model xe (ModelCode), quản lý tồn kho và back-order đầu kỳ (BO_TotalQtyBO), quy trình thẩm định & phê duyệt 2 cấp NPP (Trưởng phòng Approve1HQ, Lãnh đạo Khối Approve2HQ chốt kế hoạch và chuyển Version sang ACTUAL), bỏ duyệt (UnApprove2HQ) và hủy kế hoạch (Cancel).</summary>
public sealed class BusinessPlan
{
    public long Id { get; set; }
    public Guid OrgId { get; set; }
    public string BusinessPlanCode { get; set; } = ""; // Mã kế hoạch ({yyMM}BPL{seq:D4}, vd: 2603BPL0001)
    public string DealerCode { get; set; } = ""; // Mã đại lý (VS058, VN001...)
    public string DealerName { get; set; } = ""; // Tên đại lý
    public int YearPlan { get; set; } // Năm kế hoạch (vd: 2026)
    public string Version { get; set; } = "INIT"; // Phiên bản kế hoạch: INIT (khởi tạo) -> ACTUAL (chính thức sau duyệt)
    public int TimesPlan { get; set; } = 1; // Kế hoạch lần thứ N
    public BusinessPlanStatus Status { get; set; } = BusinessPlanStatus.Pending; // Trạng thái: Pending -> Approved1 -> Approved2 / Cancelled
    public string? HTCStaffInCharge { get; set; } // Chuyên viên / Quản lý NPP phụ trách đại lý
    public int TotalRtlTarget { get; set; } // Tổng sản lượng bán lẻ mục tiêu cả năm (tính từ tổng các model)
    public int TotalOrdTarget { get; set; } // Tổng sản lượng bán buôn / đặt hàng mục tiêu cả năm
    public int TotalBOInitial { get; set; } // Tổng tồn kho + Back-order đầu kỳ
    public string? Remark { get; set; } // Ghi chú chung kế hoạch
    public string? RejectReason { get; set; } // Lý do từ chối (nếu có)
    public string? CancelReason { get; set; } // Lý do hủy kế hoạch
    public string? CancelledBy { get; set; }
    public DateTime? CancelledAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public string CreatedBy { get; set; } = "";
    public DateTime? Approve1At { get; set; }
    public string? Approve1By { get; set; }
    public DateTime? Approve2At { get; set; }
    public string? Approve2By { get; set; }
    public DateTime? LogLUDateTime { get; set; }
    public string? LogLUBy { get; set; }

    public List<BusinessPlanDetail> Details { get; set; } = new();
}

/// <summary>Chi tiết chỉ tiêu Kế hoạch Kinh doanh theo từng Model xe (DMS.Sales BPL_BusinessPlanDtl): phân rã mục tiêu bán lẻ (Rtl) và đặt buôn (Ord) cho 12 tháng (M1..M12), tính tổng sản lượng cả năm (Rtl_TotalQtyDeal, Ord_TotalQtyOrder), so sánh với năm trước (Rtl_QtyPre, Ord_QtyPre) và tính tỷ lệ tăng trưởng (%).</summary>
public sealed class BusinessPlanDetail
{
    public long Id { get; set; }
    public long BusinessPlanId { get; set; }
    public string BusinessPlanCode { get; set; } = "";
    public string ModelCode { get; set; } = ""; // Mã model (SANTAFE, TUCSON, CRETA, ACCENT, ELANTRA...)
    public string ModelName { get; set; } = ""; // Tên model xe

    // Bán lẻ (Retail - Deal) theo 12 tháng:
    public int Rtl_TotalQtyDeal { get; set; } // Tổng sản lượng bán lẻ năm kế hoạch = sum(M1..M12)
    public int Rtl_QtyM1 { get; set; }
    public int Rtl_QtyM2 { get; set; }
    public int Rtl_QtyM3 { get; set; }
    public int Rtl_QtyM4 { get; set; }
    public int Rtl_QtyM5 { get; set; }
    public int Rtl_QtyM6 { get; set; }
    public int Rtl_QtyM7 { get; set; }
    public int Rtl_QtyM8 { get; set; }
    public int Rtl_QtyM9 { get; set; }
    public int Rtl_QtyM10 { get; set; }
    public int Rtl_QtyM11 { get; set; }
    public int Rtl_QtyM12 { get; set; }
    public int Rtl_QtyPre { get; set; } // Thực tế bán lẻ năm trước (n-1)
    public decimal Rtl_GrowthRate { get; set; } // Tỷ lệ tăng trưởng bán lẻ (%)

    // Bán buôn / Đặt hàng NPP (Wholesale / Order) theo 12 tháng:
    public int Ord_TotalQtyOrder { get; set; } // Tổng sản lượng đặt buôn năm kế hoạch = sum(M1..M12)
    public int Ord_QtyM1 { get; set; }
    public int Ord_QtyM2 { get; set; }
    public int Ord_QtyM3 { get; set; }
    public int Ord_QtyM4 { get; set; }
    public int Ord_QtyM5 { get; set; }
    public int Ord_QtyM6 { get; set; }
    public int Ord_QtyM7 { get; set; }
    public int Ord_QtyM8 { get; set; }
    public int Ord_QtyM9 { get; set; }
    public int Ord_QtyM10 { get; set; }
    public int Ord_QtyM11 { get; set; }
    public int Ord_QtyM12 { get; set; }
    public int Ord_QtyPre { get; set; } // Thực tế đặt buôn năm trước (n-1)
    public decimal Ord_GrowthRate { get; set; } // Tỷ lệ tăng trưởng bán buôn (%)

    // Tồn kho và Back-order:
    public int BO_TotalQtyBO { get; set; } // Số lượng tồn kho + B/O năm trước chuyển sang
    public string? Remark { get; set; } // Ghi chú model
}

/// <summary>Trạng thái ký duyệt bảng tính Chi phí tài chính & Chiết khấu thanh toán (DMS.Sales DMS40_FnExp_Calc_FnExp_PmDc DlrSignStatus / HTCSignStatus: Pending = 'Pending', Approved1 = 'Approve1', Approved2 = 'Approve2', Cancelled = 'Cancel').</summary>
public enum FnExpSignStatus
{
    Pending = 0,
    Approved1 = 1,
    Approved2 = 2,
    Cancelled = 3
}

/// <summary>Trạng thái tổng thể bảng tính Chi phí tài chính & Chiết khấu thanh toán (DMS.Sales DMS40_FnExp_Calc_FnExp_PmDc FnExpStatus: NotSign = 'NotSign' [Đang lưu nháp / chờ duyệt], Approved = 'Approved' [Đã duyệt 2 cấp hoàn tất], Cancelled = 'Cancel' [Đã hủy]).</summary>
public enum FnExpStatus
{
    NotSign = 0,
    Approved = 1,
    Cancelled = 2
}

/// <summary>Bảng tính Hỗ trợ Chi phí tài chính (CPTC) & Chiết khấu thanh toán TCG (CKTT) Đại lý - NPP (DMS.Sales DMS40_FnExp_Calc_FnExp_PmDc / CalcFnExpPmDcController / DMS40.0.40.FnExp_PmtDisc.cs / CalcFnExpPmDc.txt): NPP tính toán định kỳ hỗ trợ lãi suất cọc chậm, bảo lãnh ngân hàng và chiết khấu thanh toán sớm cho đại lý theo kỳ tính (TermFrom -> TermTo) và kỳ đối chiếu trước (TermPrevFrom -> TermPrevTo), quy trình duyệt 2 cấp 2 bên (Đại lý đối soát bước 1 ConfirmMultiDL, HTC duyệt bước 1 ApproveMultiHQ; Lãnh đạo HTC duyệt bước 2 Approve2HQ sinh URL chứng từ FilePathFnExp & FilePathPmtDC, Lãnh đạo Đại lý duyệt bước 2 Approve2DL chốt quyết toán).</summary>
public sealed class FnExpCalcSheet
{
    public long Id { get; set; }
    public Guid OrgId { get; set; }
    public string CaNo { get; set; } = ""; // Số bảng tính ({yyMMdd}-{seq:D3}/CPTC/{DealerCode}, vd: 260520-001/CPTC/VS058)
    public string DealerCode { get; set; } = ""; // Mã đại lý (VS058, VN012...)
    public string DealerName { get; set; } = ""; // Tên đại lý
    public DateTime TermFrom { get; set; } // Kỳ tính hiện tại - Từ ngày
    public DateTime TermTo { get; set; } // Kỳ tính hiện tại - Đến ngày
    public DateTime TermPrevFrom { get; set; } // Kỳ tính trước - Từ ngày
    public DateTime TermPrevTo { get; set; } // Kỳ tính trước - Đến ngày
    public decimal FnExpPercent { get; set; } // % Chi phí tài chính CPTC (ví dụ: 5.5% / năm)
    public decimal PmtDsTCGPercent { get; set; } // % Chiết khấu thanh toán CKTT (ví dụ: 2.0%)
    public string? CAName { get; set; } // Tên bảng tính
    public string? FlagEarlyCancel { get; set; } // Cờ hủy sớm (1/0)
    public string FlagisHTC { get; set; } = "HTC"; // Pháp nhân ("HTC" / "HTV")
    public FnExpSignStatus DlrSignStatus { get; set; } = FnExpSignStatus.Pending; // Trạng thái ký Đại lý (Pending -> Approved1 -> Approved2 / Cancelled)
    public FnExpSignStatus HTCSignStatus { get; set; } = FnExpSignStatus.Pending; // Trạng thái ký HTC (Pending -> Approved1 -> Approved2 / Cancelled)
    public FnExpStatus Status { get; set; } = FnExpStatus.NotSign; // Trạng thái bảng tính: NotSign -> Approved / Cancelled

    public int TotalCars { get; set; } // Tổng số xe trong bảng tính
    public decimal TotalFnDepositAmount { get; set; } // Tổng CPTC phần tiền cọc chậm
    public decimal TotalFnGrtAmount { get; set; } // Tổng CPTC phần bảo lãnh thanh toán
    public decimal TotalFnAmount { get; set; } // Tổng Chi phí tài chính = TotalFnDepositAmount + TotalFnGrtAmount
    public decimal TotalPDAmount { get; set; } // Tổng Chiết khấu thanh toán sớm
    public decimal NetSettlementAmount { get; set; } // Tổng số tiền thanh toán thực nhận / bù trừ = TotalPDAmount - TotalFnAmount

    public string? FilePathFnExp { get; set; } // Đường dẫn / URL file chứng từ CPTC (được tạo sau khi HTC duyệt cấp 2)
    public string? FilePathPmtDC { get; set; } // Đường dẫn / URL file chứng từ CKTT (được tạo sau khi HTC duyệt cấp 2)

    public DateTime? DlrAppr1At { get; set; } // Thời gian Đại lý xác nhận cấp 1
    public string? DlrAppr1By { get; set; } // Người Đại lý xác nhận cấp 1
    public DateTime? HTCAppr1At { get; set; } // Thời gian HTC duyệt cấp 1
    public string? HTCAppr1By { get; set; } // Người HTC duyệt cấp 1
    public DateTime? DlrAppr2At { get; set; } // Thời gian Đại lý duyệt cấp 2
    public string? DlrAppr2By { get; set; } // Người Đại lý duyệt cấp 2
    public DateTime? HTCAppr2At { get; set; } // Thời gian HTC duyệt cấp 2
    public string? HTCAppr2By { get; set; } // Người HTC duyệt cấp 2

    public string? CancelReason { get; set; } // Lý do hủy bảng tính
    public string? CancelledBy { get; set; }
    public DateTime? CancelledAt { get; set; }

    public string? Remark { get; set; } // Ghi chú chung
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public string CreatedBy { get; set; } = "";
    public DateTime? LogLUDateTime { get; set; }
    public string? LogLUBy { get; set; }

    public List<FnExpCalcSheetDetail> Details { get; set; } = new();
}

/// <summary>Chi tiết dòng xe trong Bảng tính Chi phí tài chính & Chiết khấu thanh toán (DMS.Sales DMS40_FnExp_Calc_FnExp_PmDcDtl): lưu vết thông tin từng xe (CarId, VIN, Giá bán, Ngày duyệt đơn, Hạn nghĩa vụ cọc, Thời hạn bảo lãnh, Ngày hoàn thành thanh toán 100%, Số ngày tính CPTC cọc/BL, Tiền CPTC và Số ngày hưởng CKTT sớm, Tiền CKTT).</summary>
public sealed class FnExpCalcSheetDetail
{
    public long Id { get; set; }
    public long SheetId { get; set; }
    public string CaNo { get; set; } = "";
    public string CarId { get; set; } = ""; // Mã xe hệ thống
    public string Vin { get; set; } = ""; // Số khung VIN (17 ký tự)
    public string ModelCode { get; set; } = ""; // Model xe
    public string ModelName { get; set; } = ""; // Tên model xe
    public decimal UnitPriceActual { get; set; } // Giá bán xe thực tế
    public DateTime? CarCancelDate { get; set; } // Ngày xe hủy (nếu có)
    public DateTime? SodApprovedDate { get; set; } // Ngày duyệt đơn đặt hàng (SO Approved Date)
    public DateTime? SodDepositDutyEndDate { get; set; } // Ngày hết hạn nộp tiền cọc
    public DateTime? DateStart { get; set; } // Ngày bắt đầu hiệu lực bảo lãnh
    public DateTime? DateEnd { get; set; } // Ngày hết hạn hiệu lực bảo lãnh
    public DateTime? TotalCompletedDate { get; set; } // Ngày thanh toán hoàn thành nghĩa vụ (>= 100% giá xe)
    public decimal TermActual { get; set; } // Thời hạn thực tế của bảo lãnh (ngày)

    public decimal FnDepositCountDate { get; set; } // Số ngày tính CPTC cọc chậm
    public decimal FnDepositAmount { get; set; } // Số tiền CPTC cọc
    public decimal FnGrtCountDate { get; set; } // Số ngày tính CPTC bảo lãnh
    public decimal FnGrtAmount { get; set; } // Số tiền CPTC bảo lãnh
    public decimal FnTotalAmount { get; set; } // Tổng CPTC xe = FnDepositAmount + FnGrtAmount

    public decimal PDCountDate { get; set; } // Số ngày được hưởng CKTT sớm
    public decimal PDAmount { get; set; } // Số tiền CKTT được hưởng

    public FnExpStatus Status { get; set; } = FnExpStatus.NotSign; // Trạng thái dòng
    public string? Remark { get; set; } // Ghi chú dòng xe
    public DateTime? LogLUDateTime { get; set; }
    public string? LogLUBy { get; set; }
}

/// <summary>Trạng thái hiệu lực chính sách bán hàng (DMS.Sales SPL_SalesPolicyMst FlagMstValid & Status: Draft = '0' [Nháp], Valid = '1' [Hiệu lực/Đã duyệt], Expired = '2' [Hết hạn], Cancelled = '3' [Hủy/Dừng áp dụng]).</summary>
public enum SalesPolicyStatus
{
    Draft = 0,
    Valid = 1,
    Expired = 2,
    Cancelled = 3
}

/// <summary>Loại chính sách bán hàng / hỗ trợ (DMS.Sales SPL_SalesPolicyMst SPSRType: Wholesale = 'WHOLESALE' [Bán buôn], Retail = 'RETAIL' [Hỗ trợ bán lẻ], Campaign = 'CAMPAIGN' [Chiến dịch khuyến mại], Special = 'SPECIAL' [Chính sách đặc thù theo vùng/dự án]).</summary>
public enum SalesPolicyType
{
    Wholesale = 0,
    Retail = 1,
    Campaign = 2,
    Special = 3
}

/// <summary>Hình thức hỗ trợ kinh doanh chính sách (DMS.Sales SPL_SalesPolicyMst FormBusinessSupportCode: Cash = 'CASH' [Hỗ trợ tiền mặt], InterestRate = 'INTEREST' [Hỗ trợ lãi suất vay ngân hàng], Accessory = 'ACCESSORY' [Gói phụ kiện chính hãng], Insurance = 'INSURANCE' [Tặng bảo hiểm vật chất xe], RegistrationFee = 'REGISFEE' [Hỗ trợ 50-100% lệ phí trước bạ]).</summary>
public enum BusinessSupportForm
{
    Cash = 0,
    InterestRate = 1,
    Accessory = 2,
    Insurance = 3,
    RegistrationFee = 4
}

/// <summary>Quản lý Chính sách Bán hàng & Hỗ trợ Kinh doanh Xe Ô tô Đại lý - NPP (DMS.Sales SPL_SalesPolicyMst + SPL_SalesPolicyMstDetail / SPLSalesPolicyMstController / SalesPolicy.cs / SPLSalesPolicyMst.txt / Mst.ChinhSachHoTroKinhDoanh.xlsx): NPP ban hành chính sách hỗ trợ bán lẻ/bán buôn xe theo văn bản quyết định (SPNo), thời hạn áp dụng (StartDate -> EndDate), hình thức hỗ trợ (tiền mặt, lãi suất, phụ kiện, trước bạ), phân bổ chi tiết mức hỗ trợ cho từng dòng xe (ModelCode, SpecCode) và danh sách đại lý (DealerCode).</summary>
public sealed class SalesPolicyMaster
{
    public long Id { get; set; }
    public Guid OrgId { get; set; }
    public string SPSRCode { get; set; } = ""; // Mã chính sách (SPSR{yyMM}{seq:D4}, vd: SPSR26030001)
    public string SPNo { get; set; } = ""; // Số hiệu văn bản quyết định (vd: 15/2026/QĐ-HTV-CSP)
    public DateTime StartDate { get; set; } // Ngày bắt đầu hiệu lực chính sách
    public DateTime EndDate { get; set; } // Ngày kết thúc hiệu lực chính sách
    public SalesPolicyType SPSRType { get; set; } = SalesPolicyType.Retail; // Loại chính sách
    public BusinessSupportForm FormBusinessSupportCode { get; set; } = BusinessSupportForm.Cash; // Hình thức hỗ trợ kinh doanh
    public string? SPSRRoot { get; set; } // Mã chính sách gốc (nếu là chính sách điều chỉnh/bổ sung)
    public string? FilePath { get; set; } // Đường dẫn lưu file văn bản quyết định đã ký
    public string? FileName { get; set; } // Tên file văn bản
    public string FlagMstValid { get; set; } = "1"; // '1' = Hiệu lực, '0' = Không hiệu lực/Dừng
    public SalesPolicyStatus Status { get; set; } = SalesPolicyStatus.Valid; // Trạng thái chính sách
    public string? Remark { get; set; } // Nội dung / mô tả về việc ban hành chính sách
    public DateTime? ApprovedAt { get; set; } // Ngày giờ phê duyệt ban hành
    public string? ApprovedBy { get; set; } // Lãnh đạo Khối Kinh doanh / NPP phê duyệt
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public string? CreatedBy { get; set; }
    public DateTime? LUDateTime { get; set; }
    public string? LUBy { get; set; }

    public List<SalesPolicyDetail> Details { get; set; } = new();
}

/// <summary>Chi tiết phạm vi áp dụng Chính sách Bán hàng (DMS.Sales SPL_SalesPolicyMstDetail): quy định mức hỗ trợ (AmountSupport / SupportPercent) cho từng Model xe, phiên bản Spec, năm sản xuất và mã đại lý được hưởng.</summary>
public sealed class SalesPolicyDetail
{
    public long Id { get; set; }
    public long SalesPolicyId { get; set; }
    public string SPSRCode { get; set; } = ""; // Tham chiếu mã chính sách
    public string DealerCode { get; set; } = ""; // Mã đại lý (VS058, VN012 hoặc 'ALL' cho toàn hệ thống)
    public string ModelCode { get; set; } = ""; // Mã model xe (CRETA, SANTAFE, TUCSON, ACCENT, BN7I-CKD...)
    public string? ModelName { get; set; } // Tên model xe
    public string? SpecCode { get; set; } // Mã phiên bản xe
    public string? SpecDescription { get; set; } // Mô tả chi tiết bản xe
    public string? YearOfManufacture { get; set; } // Năm sản xuất xe (2025, 2026...)
    public decimal AmountSupport { get; set; } // Mức tiền hỗ trợ trên mỗi xe (VNĐ)
    public decimal SupportPercent { get; set; } // Tỷ lệ % hỗ trợ trên giá trị xe (nếu có)
    public int MinQty { get; set; } = 1; // Số lượng bán tối thiểu để kích hoạt mức hỗ trợ
    public string? Remark { get; set; } // Ghi chú chi tiết dòng xe
}

/// <summary>Trạng thái Hồ sơ đề nghị Quyết toán Hỗ trợ Bán lẻ theo chính sách (DMS.Sales SPL_SPSupportRetail Status: Pending = 'P' [Chờ NPP thẩm tra], Approved = 'A' [NPP duyệt hỗ trợ], Paid = 'F' [Đã thanh toán/giải ngân chi trả cho Đại lý], Rejected = 'R' [Từ chối], Cancelled = 'C' [Hủy]).</summary>
public enum SupportRetailStatus
{
    Pending = 0,
    Approved = 1,
    Paid = 2,
    Rejected = 3,
    Cancelled = 4
}

/// <summary>Hồ sơ Đề nghị Quyết toán Hỗ trợ Bán lẻ theo Chính sách Xe Ô tô (DMS.Sales SPL_SPSupportRetail / SPLSPSupportRetailController / SalesPolicy.cs / SPLSPSupportRetail.txt / RptSPLSPSupportRetailController): Đại lý lập đề nghị quyết toán hỗ trợ cho từng xe bán lẻ đã giao khách theo chính sách (SPSRCode/SPNo), hệ thống tự động đối soát liên kết số đơn hàng bán buôn (OSODSOCode), ngày xác nhận ĐH (OSODApprovedDate), số hóa đơn bán buôn HTC (HTCInvoiceNo), ngày hóa đơn (HTCInvoiceDate), ngày xuất xe thực tế (CDODDeliveryOutDate), ngày hoàn tất toàn bộ nghĩa vụ tài chính và bảo lãnh (DateFullStatus), NPP thẩm tra duyệt số tiền (AmountHTCAppr) và kế toán thanh toán chi trả cho đại lý.</summary>
public sealed class SupportRetail
{
    public long Id { get; set; }
    public Guid OrgId { get; set; }
    public string SupportCode { get; set; } = ""; // Mã hồ sơ đề nghị ({yyMM}SPR{seq:D5}, vd: 2603SPR00001)
    public string SPSRCode { get; set; } = ""; // Mã chính sách bán hàng liên kết (SPSRCode)
    public string SPNo { get; set; } = ""; // Số hiệu văn bản chính sách
    public string DealerCode { get; set; } = ""; // Mã đại lý đề nghị quyết toán
    public string? DealerName { get; set; } // Tên đại lý
    public string Vin { get; set; } = ""; // Số khung VIN xe bán lẻ (17 ký tự)
    public string? CarId { get; set; } // Mã định danh xe trong hệ thống Car_Car
    public string? ModelCode { get; set; } // Model xe (CRETA, ACCENT, SANTAFE...)
    public string? ModelName { get; set; } // Tên model xe
    public decimal AmountSupport { get; set; } // Số tiền đại lý đề nghị được hỗ trợ (VNĐ)
    public decimal AmountHTCAppr { get; set; } // Số tiền NPP thẩm tra và phê duyệt hỗ trợ (VNĐ)
    public DateTime DateSupport { get; set; } // Tháng / kỳ hỗ trợ (đầu tháng)
    public DateTime? HTCDatePayment { get; set; } // Ngày NPP thực tế thanh toán giải ngân hỗ trợ
    public SupportRetailStatus Status { get; set; } = SupportRetailStatus.Pending; // Trạng thái hồ sơ
    public string? Remark { get; set; } // Ghi chú đề nghị hỗ trợ

    // Các trường đối soát liên kết chuỗi nghiệp vụ bán hàng:
    public DateTime? OSODApprovedDate { get; set; } // Ngày NPP phê duyệt đơn đặt hàng buôn
    public string? OSODSOCode { get; set; } // Số đơn hàng buôn xe (Ord_SalesOrder)
    public string? HTCInvoiceNo { get; set; } // Số hóa đơn VAT bán buôn NPP xuất (VAT_HTCInvoice)
    public DateTime? HTCInvoiceDate { get; set; } // Ngày hóa đơn bán buôn
    public DateTime? CDODDeliveryOutDate { get; set; } // Ngày xe xuất kho thực tế theo Lệnh giao xe (Car_DeliveryOrder)
    public DateTime? DateFullStatus { get; set; } // Ngày hoàn thành toàn bộ nghĩa vụ tài chính và bảo lãnh xe
    public string? PRDiscountNo { get; set; } // Số đề nghị chiết khấu thanh toán liên quan (nếu có)

    public DateTime? ApprovedAt { get; set; } // Ngày NPP duyệt hỗ trợ
    public string? ApprovedBy { get; set; }
    public DateTime? RejectedAt { get; set; } // Ngày từ chối
    public string? RejectedBy { get; set; }
    public string? RejectReason { get; set; } // Lý do từ chối hỗ trợ
    public DateTime? CancelledAt { get; set; } // Ngày hủy hồ sơ
    public string? CancelledBy { get; set; }
    public string? CancelReason { get; set; } // Lý do hủy hồ sơ
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public string? CreatedBy { get; set; }
}

/// <summary>Trạng thái Kế hoạch dự kiến đặt hàng xe ô tô Đại lý (DMS.Sales Plan_EstimateOrder PLEOrdStatus: Pending = 'P' [Mới tạo/Đại lý lập dự kiến], Approved1 = 'A1' [Trưởng phòng BH duyệt cấp 1/nhu cầu], Approved2 = 'A2' [Lãnh đạo Khối duyệt cấp 2 chốt kế hoạch/chuyển HTMV], Cancelled = 'C' [Đã hủy]).</summary>
public enum PlanEstimateOrderStatus
{
    Pending = 0,
    Approved1 = 1,
    Approved2 = 2,
    Cancelled = 3
}

/// <summary>Kế hoạch Dự kiến Đặt hàng Xe Ô tô Đại lý - NPP (DMS.Sales Plan_EstimateOrder / PlanEstimateOrderController / Order.cs / PlanEstimateOrder.txt / 04_DON_HANG_DOANH_SO.md): Quản lý dự báo nhu cầu đặt xe của Đại lý theo kỳ tháng (MonthEstimate yyyy-MM), kết nối với Kế hoạch kinh doanh năm (BusinessPlanCode) và phân vùng địa lý (AreaRootCode), tổng hợp sản lượng đặt buôn dự kiến T(N+1) và dự kiến bán lẻ các tháng N, N+1, N+2, N+3, quy trình phê duyệt 2 cấp NPP (Trưởng phòng Approve1HQ, Lãnh đạo Khối Approve2HQ chốt kế hoạch chuyển nhà máy sản xuất HTMV) và hủy/xóa kế hoạch.</summary>
public sealed class PlanEstimateOrder
{
    public long Id { get; set; }
    public Guid OrgId { get; set; }
    public string PLEOrdNo { get; set; } = ""; // Mã số đơn ước tính ({yyMM}PLE{seq:D5}, vd: 2603PLE00001)
    public string DealerCode { get; set; } = ""; // Mã đại lý (VS058, VN001, VN012...)
    public string DealerName { get; set; } = ""; // Tên đại lý
    public string MonthEstimate { get; set; } = ""; // Tháng dự kiến (yyyy-MM, vd: 2026-04)
    public string? BusinessPlanCode { get; set; } // Mã kế hoạch kinh doanh năm liên kết (BPL_BusinessPlan)
    public string? AreaRootCode { get; set; } // Vùng miền phụ trách (MB: Miền Bắc, MT: Miền Trung, MN: Miền Nam)
    public PlanEstimateOrderStatus Status { get; set; } = PlanEstimateOrderStatus.Pending; // Trạng thái kế hoạch: Pending -> Approved1 -> Approved2 / Cancelled

    // Tổng hợp sản lượng:
    public int TotalQtyN1 { get; set; } // Tổng sản lượng đề xuất đặt hàng T(N+1)
    public int TotalSellCusN0 { get; set; } // Tổng dự kiến bán lẻ tháng N
    public int TotalSellCusN1 { get; set; } // Tổng dự kiến bán lẻ tháng N+1
    public int TotalSellCusN2 { get; set; } // Tổng dự kiến bán lẻ tháng N+2
    public int TotalSellCusN3 { get; set; } // Tổng dự kiến bán lẻ tháng N+3

    public string? Remark { get; set; } // Ghi chú đơn ước tính
    public string? CancelReason { get; set; } // Lý do hủy kế hoạch
    public string? CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public string? Approve1By { get; set; } // Người duyệt cấp 1 (Trưởng phòng)
    public DateTime? Approve1At { get; set; } // Thời điểm duyệt cấp 1
    public string? Approve2By { get; set; } // Người duyệt cấp 2 (Lãnh đạo Khối)
    public DateTime? Approve2At { get; set; } // Thời điểm duyệt cấp 2
    public string? CancelledBy { get; set; }
    public DateTime? CancelledAt { get; set; }
    public DateTime? LogLUDateTime { get; set; }
    public string? LogLUBy { get; set; }

    public List<PlanEstimateOrderDetail> Details { get; set; } = new();
}

/// <summary>Chi tiết dòng xe trong Kế hoạch dự kiến đặt hàng (DMS.Sales Plan_EstimateOrderDtl): quản lý số liệu theo từng model/spec xe gồm lượng tồn kho thực tế, xe trên đường, đơn hàng cũ chưa xuất kho, xe đang map VIN, dự báo bán lẻ 4 tháng liên tiếp N..N+3, tính toán Tổng hàng có, Tỷ lệ hàng có, Đề xuất đặt hàng T(N+1) và Dự phòng tồn kho an toàn.</summary>
public sealed class PlanEstimateOrderDetail
{
    public long Id { get; set; }
    public long PlanEstimateOrderId { get; set; }
    public string PLEOrdNo { get; set; } = "";
    public string ModelCode { get; set; } = ""; // Mã dòng xe (SANTAFE, TUCSON, CRETA, ACCENT, STARGAZER, CUSTIN, IONIQ5...)
    public string ModelName { get; set; } = ""; // Tên model xe
    public string? SpecCode { get; set; } // Mã cấu hình xe (SF25-PRE, TU20-STD, CR15-PRE...)
    public string? SpecDescription { get; set; } // Mô tả bản xe

    // Dữ liệu hiện trạng kho và chuỗi cung ứng:
    public int QtySellCustomer { get; set; } // Xe đã ký hợp đồng bán lẻ chờ giao
    public int QtySellDealer { get; set; } // Xe bán buôn giữa các đại lý
    public int QtyInStock { get; set; } // Tồn kho thực tế tại bãi đại lý
    public int QtyOnWay { get; set; } // Xe NPP đang vận chuyển trên đường
    public int QtyBuyDealer { get; set; } // Xe mua ngang từ đại lý khác
    public int QtyUnknown { get; set; } // Lượng xe đang kiểm tra chưa xác định
    public int QtyBOChuaXuatKho { get; set; } // Đơn hàng cũ còn nợ tồn (Back-Order) chưa xuất kho
    public int QtyBODangMapVIN { get; set; } // Đơn hàng đang được ghép số khung VIN
    public int QtyBOKhongVINQuaKhu { get; set; } // BO chưa có VIN kỳ trước
    public int QtyBOKhongVINHienTai { get; set; } // BO chưa có VIN kỳ hiện tại
    public int QtyBOKhongVINTuongLai { get; set; } // BO chưa có VIN kỳ tới
    public int QtyOrdCarID { get; set; } // Đã có CarID định danh
    public int QtyOrdNotCarID { get; set; } // Chưa có CarID

    // Dự báo kế hoạch đặt hàng và bán lẻ:
    public int QtyEOrdN1 { get; set; } // Sản lượng ĐỀ XUẤT ĐẶT HÀNG T(N+1)
    public decimal TiLeDatTN1 { get; set; } // Tỉ lệ đặt hàng T(N+1) (%)
    public int QtyESellCusN0 { get; set; } // Dự kiến bán lẻ Tháng N
    public int QtyESellCusN1 { get; set; } // Dự kiến bán lẻ Tháng N+1
    public int QtyESellCusN2 { get; set; } // Dự kiến bán lẻ Tháng N+2
    public int QtyESellCusN3 { get; set; } // Dự kiến bán lẻ Tháng N+3

    // Chỉ số cân đối nguồn hàng và tồn kho an toàn:
    public int TongHangCo { get; set; } // Tổng hàng có = QtyInStock + QtyOnWay + QtyBuyDealer + QtyBODangMapVIN
    public decimal TiLeHangCo { get; set; } // Tỉ lệ tổng hàng có (%)
    public int DuPhongTonKho { get; set; } // Tồn kho dự phòng = TongHangCo + QtyEOrdN1 - (QtyESellCusN0 + QtyESellCusN1)
    public decimal TiLeDuPhongTonKho { get; set; } // Tỉ lệ dự phòng tồn kho (%)
    public string? Remark { get; set; } // Ghi chú dòng xe
}

/// <summary>Loại danh sách đóng gói xe nhập khẩu (DMS.Sales CT_PackingList PLType: CBU = 0 [Xe nguyên chiếc nhập khẩu], CKD = 1 [Bộ linh kiện rời lắp ráp]).</summary>
public enum PackingListType
{
    CBU = 0,
    CKD = 1
}

/// <summary>Trạng thái Danh sách đóng gói / Lô xe nhập khẩu (DMS.Sales CT_PackingList PLStatus: Draft = 'Draft' [Nháp/chờ xếp tàu], Shipping = 'Shipping' [Đang vận chuyển đường biển], PortArrived = 'PortArrived' [Đã cập cảng biển/kiểm hóa], Completed = 'Completed' [Nghiệm thu nhập kho hoàn tất], Cancelled = 'Cancelled' [Đã hủy lô]).</summary>
public enum PackingListStatus
{
    Draft = 0,
    Shipping = 1,
    PortArrived = 2,
    Completed = 3,
    Cancelled = 4
}

/// <summary>Trạng thái từng xe ô tô trong Packing List (DMS.Sales Car_VINForPL / CT_PackingListDetail Status: Draft = 0, OnBoard = 1, PortArrived = 2, StockIn = 3, Cancelled = 4).</summary>
public enum PackingListDetailStatus
{
    Draft = 0,
    OnBoard = 1,
    PortArrived = 2,
    StockIn = 3,
    Cancelled = 4
}

/// <summary>Quản lý Danh sách Đóng gói / Lô xe Ô tô Nhập khẩu Packing List & Lịch trình Vận chuyển Tàu biển / Nghiệm thu Cảng biển (DMS.Sales CT_PackingList + Car_VINForPL / CTPackingListController.cs / CTPackingList.txt / 05_QUAN_LY_XE.md / PL.xlsx): Quản lý tiếp nhận lô xe nhập khẩu từ Hyundai Motor Company (HMC) theo Hợp đồng ngoại thương (ContractNo) và Thư tín dụng thanh toán quốc tế (LCNo), theo dõi hải trình tàu biển (VesselName, VoyageNo, ShippingDateStart, ShippingDateEndExpected, ShippingDateEnd), giám định tình trạng hư hại trầy xước vỏ xe trong vận chuyển đường biển (FlagRepair, RepairRemark) và nghiệm thu nhập kho bãi lưu trữ, tự động cấp số khung VIN vào kho xe khả dụng sẵn sàng cho Map VIN.</summary>
public sealed class PackingList
{
    public long Id { get; set; }
    public Guid OrgId { get; set; }
    public string PackingListNo { get; set; } = ""; // Mã Packing List ({yyMM}PL{seq:D4}, vd: 2603PL0001)
    public string ContractNo { get; set; } = ""; // Số hợp đồng ngoại thương (HMC-2026-VN01...)
    public string? LCNo { get; set; } // Số thư tín dụng L/C (LC2601001...)
    public string PortCode { get; set; } = ""; // Mã cảng tiếp nhận (CANG_CAT_LAI, CANG_HAI_PHONG, CANG_DA_NANG, CANG_CAI_MEP...)
    public string? PortName { get; set; } // Tên cảng tiếp nhận
    public PackingListType PLType { get; set; } = PackingListType.CBU; // Loại hình: CBU (xe nguyên chiếc) / CKD (linh kiện)
    public PackingListStatus Status { get; set; } = PackingListStatus.Draft; // Trạng thái PL
    public DateTime? ShippingDateStart { get; set; } // Ngày tàu xuất phát rời cảng nước ngoài (ETD / OnBoard Date)
    public DateTime? ShippingDateEndExpected { get; set; } // Ngày dự kiến tàu cập cảng đích (ETA)
    public DateTime? ShippingDateEnd { get; set; } // Ngày thực tế tàu cập cảng đích (ATA / Port Arrived Date)
    public string? VesselName { get; set; } // Tên tàu vận tải chuyên dụng chở ô tô (vd: GLOVIS COURAGE, MORNING CAROLINE...)
    public string? VoyageNo { get; set; } // Số chuyến hải trình (vd: V2603S)
    public int TotalCars { get; set; } // Tổng số lượng xe trong lô Packing List
    public int TotalRepaired { get; set; } // Tổng số xe phát hiện trầy xước/lỗi vận chuyển cần khắc phục sửa chữa
    public string? Remark { get; set; } // Ghi chú lô xe
    public string? CancelReason { get; set; } // Lý do hủy lô xe
    public string? CancelledBy { get; set; }
    public DateTime? CancelledAt { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime? LUDateTime { get; set; }
    public string? LUBy { get; set; }

    public List<PackingListDetail> Details { get; set; } = new();
}

/// <summary>Chi tiết dòng xe ô tô trong Packing List nhập khẩu (DMS.Sales Car_VINForPL / CT_PackingListDetail): Quản lý từng số khung xe VIN 17 ký tự, số máy, mã chìa khóa, model xe, phiên bản cấu hình, mã màu ngoại thất, lệnh sản xuất HMC, cờ đánh dấu hư hại xước xát vỏ xe do vận chuyển biển (FlagRepair), ghi chú sửa chữa và kho lưu bãi tiếp nhận.</summary>
public sealed class PackingListDetail
{
    public long Id { get; set; }
    public long PackingListId { get; set; }
    public string PackingListNo { get; set; } = "";
    public string Vin { get; set; } = ""; // Số khung xe chuẩn 17 ký tự (VIN)
    public string? EngineNo { get; set; } // Số máy
    public string? KeyNo { get; set; } // Mã chìa khóa xe
    public string ModelCode { get; set; } = ""; // Mã model (SANTAFE, PALISADE, IONIQ5, TUCSON, CRETA, CUSTIN, ACCENT...)
    public string ModelName { get; set; } = ""; // Tên model xe
    public string? SpecCode { get; set; } // Mã cấu hình xe (SF25-PRE-01, PL22-EXC-01...)
    public string? SpecDescription { get; set; } // Mô tả bản xe
    public string? ColorCode { get; set; } // Mã màu ngoại thất (WW2, NKA, MB1, WH1, R3R...)
    public string? ColorName { get; set; } // Tên màu xe
    public string? WorkOrderNo { get; set; } // Lệnh sản xuất HMC (WO-HMC-2026-001)
    public string? ProductionMonth { get; set; } // Tháng sản xuất xe tại HMC (yyyy-MM)
    public bool FlagRepair { get; set; } = false; // Cờ xe phát hiện lỗi/hư hại vận chuyển đường biển cần sửa chữa
    public string? RepairRemark { get; set; } // Biên bản giám định hư hại / ghi chú sửa chữa xe
    public string? StorageCodeCurrent { get; set; } // Kho tiếp nhận lưu bãi hiện tại (KHO_TONG_HN, KHO_TONG_SG, KHO_CANG_HP, KHO_CANG_CL...)
    public DateTime? StoreDate { get; set; } // Ngày thực tế xe nhập kho bãi lưu trữ
    public PackingListDetailStatus Status { get; set; } = PackingListDetailStatus.Draft; // Trạng thái dòng xe
    public string? Remark { get; set; }
}

/// <summary>Loại giao dịch ngân hàng điện tử (DMS.Sales RQ_BankingTransactions BkTransType: PMT = Giải ngân thanh toán xe buôn, GRT = Đề nghị bảo lãnh thanh toán xe, GRT_LC = Đề nghị mở L/C tín dụng chứng từ mua buôn xe).</summary>
public enum BankingTransType
{
    PMT = 0,
    GRT = 1,
    GRT_LC = 2
}

/// <summary>Trạng thái xử lý nội bộ Đề nghị giao dịch ngân hàng (DMS.Sales RQ_BankingTransactions BkTransStatus: Draft = '0' [Nháp], Pending = '1' [Chờ ngân hàng xử lý], Approved = '2' [Đã duyệt / Phát hành bảo lãnh], Disbursed = '3' [Đã giải ngân thanh toán], Rejected = '4' [Ngân hàng từ chối], Cancelled = '5' [Đại lý đã hủy]).</summary>
public enum BankingTransStatus
{
    Draft = 0,
    Pending = 1,
    Approved = 2,
    Disbursed = 3,
    Rejected = 4,
    Cancelled = 5
}

/// <summary>Trạng thái kết nối phía Ngân hàng đối tác (DMS.Sales RQ_BankingTransactions BkTransBankStatus: Draft = 0, Pending = 1, BankReceived = 2, BankApproved = 3, Disbursed = 4, BankRejected = 5).</summary>
public enum BankingTransBankStatus
{
    Draft = 0,
    Pending = 1,
    BankReceived = 2,
    BankApproved = 3,
    Disbursed = 4,
    BankRejected = 5
}

/// <summary>Phương thức giải ngân ngân hàng (DirectTransfer = Chuyển khoản trực tiếp sang tài khoản NPP, LoanDisbursement = Giải ngân theo hợp đồng hạn mức vay tín dụng).</summary>
public enum BankingTransDisbursementType
{
    DirectTransfer = 0,
    LoanDisbursement = 1
}

/// <summary>Loại chứng từ hồ sơ đính kèm giao dịch ngân hàng (Contract = Hợp đồng mua xe, BankStatement = Sao kê tài khoản, GuaranteeLetter = Thư cam kết cấp tín dụng, Invoice = Hóa đơn VAT, Authorization = Giấy ủy quyền, Other = Khác).</summary>
public enum BankingTransFileType
{
    Contract = 0,
    BankStatement = 1,
    GuaranteeLetter = 2,
    Invoice = 3,
    Authorization = 4,
    Other = 5
}

/// <summary>Đề nghị Giao dịch Ngân hàng Điện tử / Kết nối Ngân hàng Tài trợ Xe Ô tô Đại lý - NPP (DMS.Sales RQ_BankingTransactions + RQ_BankingTransPmt + RQ_BankingTransGrt / RQBankingTransactionsController / ConnectBanking.cs / KetNoiSoPhuOnline): Đại lý lập đề nghị giao dịch gửi sang ngân hàng tài trợ (VPBank, VietinBank, VIB, BIDV, Techcombank...) để xin Giải ngân thanh toán (PMT), Phát hành bảo lãnh (GRT) hoặc Mở thư tín dụng (GRT_LC) cho các xe thuộc Hợp đồng mua bán buôn xe (DealerContract), đẩy điện giao dịch sang Open Banking (PushBankDL), ngân hàng thẩm duyệt (BankApprove cấp LDNo/MDNo) và thực hiện giải ngân chi trả vào tài khoản NPP (Disburse).</summary>
public sealed class BankingTransaction
{
    public long Id { get; set; }
    public Guid OrgId { get; set; }
    public string RQ_BankingTransNo { get; set; } = ""; // Mã đề nghị giao dịch ({yyMM}BKT{seq:D4}, vd: 2603BKT0001)
    public string DealerCode { get; set; } = ""; // Mã đại lý
    public string DealerName { get; set; } = ""; // Tên đại lý
    public string BankCode { get; set; } = ""; // Mã ngân hàng liên kết (VPB, CTG, VIB, BIDV, TCB...)
    public string BankName { get; set; } = ""; // Tên ngân hàng liên kết
    public string? TaxCode { get; set; } // Mã số thuế / ĐKKD đại lý
    public BankingTransType TransType { get; set; } = BankingTransType.PMT; // Loại giao dịch: PMT, GRT, GRT_LC
    public BankingTransStatus Status { get; set; } = BankingTransStatus.Draft; // Trạng thái đề nghị
    public BankingTransBankStatus BankStatus { get; set; } = BankingTransBankStatus.Draft; // Trạng thái phản hồi phía ngân hàng
    public string? RefBankCode { get; set; } // Mã tham chiếu điện tử sinh từ cổng Open Banking
    public string? BankRemark { get; set; } // Ý kiến / thông báo phản hồi từ ngân hàng
    public string? Remark { get; set; } // Ghi chú của đại lý
    public decimal TotalAmount { get; set; } // Tổng số tiền đề nghị giao dịch (VNĐ)
    public int TotalCars { get; set; } // Tổng số lượng xe phân bổ

    // Thông tin nghiệp vụ Giải ngân thanh toán (PMT):
    public string? PaymentNo { get; set; } // Mã phiếu thanh toán liên kết nếu có (PMT...)
    public string? PaymentType { get; set; } = "Payment"; // Loại thanh toán: Deposit, Payment, Settlement
    public BankingTransDisbursementType DisbursementType { get; set; } = BankingTransDisbursementType.LoanDisbursement; // Loại giải ngân
    public decimal TransferAmount { get; set; } // Số tiền đề nghị giải ngân (VNĐ)
    public int? LoanPeriod { get; set; } = 3; // Kỳ hạn vay (tháng)
    public DateTime? LoanPeriodDate { get; set; } // Ngày đáo hạn khoản vay
    public decimal? InterestRate { get; set; } = 7.5m; // Lãi suất vay (%/năm)
    public string? ReceivingUnit { get; set; } = "CÔNG TY CỔ PHẦN LIÊN DOANH Ô TÔ HYUNDAI THÀNH CÔNG VIỆT NAM"; // Đơn vị thụ hưởng
    public string? BankAccountReceive { get; set; } = "111000123456"; // Tài khoản thụ hưởng
    public string? BankNameReceive { get; set; } = "VietinBank - Chi nhánh Hà Nội"; // Ngân hàng thụ hưởng
    public string? CreditContractNo { get; set; } // Số hợp đồng cấp hạn mức tín dụng với ngân hàng
    public DateTime? DisbursementRequestDate { get; set; } // Ngày đề nghị giải ngân
    public string? LDNo { get; set; } // Số khế ước nhận nợ do ngân hàng cấp sau duyệt
    public decimal DisbursementAmount { get; set; } // Số tiền thực tế ngân hàng đã giải ngân
    public DateTime? DisbursementDate { get; set; } // Ngày thực tế giải ngân thành công

    // Thông tin nghiệp vụ Bảo lãnh ngân hàng (GRT / GRT_LC):
    public string? GuaranteeType { get; set; } = "Bảo lãnh thanh toán mua buôn xe ô tô"; // Loại bảo lãnh
    public int? DateExpiredValue { get; set; } = 90; // Thời hạn hiệu lực bảo lãnh (ngày)
    public string? GrtForm { get; set; } = "Thư bảo lãnh điện tử có chữ ký số CA"; // Hình thức phát hành bảo lãnh
    public string? GrtReceive { get; set; } = "HYUNDAI THANH CONG VIETNAM"; // Bên nhận bảo lãnh
    public string? MDNo { get; set; } // Số chứng thư bảo lãnh do ngân hàng phát hành sau duyệt
    public decimal GrtAmount { get; set; } // Số tiền bảo lãnh được cấp
    public DateTime? GrtDateStart { get; set; } // Ngày bắt đầu hiệu lực bảo lãnh
    public DateTime? GrtDateEnd { get; set; } // Ngày kết thúc hiệu lực bảo lãnh

    // Thời gian và người thao tác:
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public string? CreatedBy { get; set; }
    public DateTime? PushedToBankAt { get; set; }
    public string? PushedToBankBy { get; set; }
    public DateTime? BankApprovedAt { get; set; }
    public string? BankApprovedBy { get; set; }
    public DateTime? DisbursedAt { get; set; }
    public string? DisbursedBy { get; set; }
    public DateTime? RejectedAt { get; set; }
    public string? RejectedBy { get; set; }
    public string? RejectReason { get; set; }
    public DateTime? CancelledAt { get; set; }
    public string? CancelledBy { get; set; }
    public string? CancelReason { get; set; }
    public DateTime? LUDateTime { get; set; }
    public string? LUBy { get; set; }

    public List<BankingTransactionDetail> Details { get; set; } = new();
    public List<BankingTransactionAttachFile> Attachments { get; set; } = new();
}

/// <summary>Chi tiết dòng xe phân bổ trong Đề nghị giao dịch ngân hàng (DMS.Sales RQ_BankingTransPmtDtl + RQ_BankingTransGrtDtl): gắn với số khung VIN, model xe, mã phụ lục hợp đồng bán buôn DlrCtrNo, đơn đặt hàng SOCode, tỷ lệ phân bổ vay/bảo lãnh và số tiền phân bổ tương ứng.</summary>
public sealed class BankingTransactionDetail
{
    public long Id { get; set; }
    public long BankingTransactionId { get; set; }
    public string RQ_BankingTransNo { get; set; } = ""; // Tham chiếu mã đề nghị
    public string CarId { get; set; } = ""; // Mã định danh xe trong hệ thống Car_Car
    public string Vin { get; set; } = ""; // Số khung VIN xe (17 ký tự)
    public string ModelCode { get; set; } = ""; // Mã model (SANTAFE, TUCSON, CRETA, ACCENT...)
    public string ModelName { get; set; } = ""; // Tên model xe
    public string? SpecCode { get; set; } // Mã cấu hình / spec
    public string? SpecDescription { get; set; } // Mô tả bản xe
    public string? ColorCode { get; set; } // Mã màu xe
    public string? DlrCtrNo { get; set; } // Số phụ lục hợp đồng mua bán buôn xe (DealerContract)
    public string? SOCode { get; set; } // Số đơn đặt buôn xe (Ord_SalesOrderRoot)
    public string? HTCInvoiceNo { get; set; } // Số hóa đơn VAT bán buôn nếu đã xuất
    public decimal AmountActual { get; set; } // Giá trị thực tế của xe (VNĐ)
    public decimal AllocPercent { get; set; } = 100m; // Tỷ lệ phân bổ vốn vay / bảo lãnh (%: ví dụ 70%, 80%, 100%)
    public decimal AllocAmount { get; set; } // Số tiền phân bổ = AmountActual * AllocPercent / 100 (VNĐ)
    public string? Remark { get; set; } // Ghi chú dòng xe
}

/// <summary>Hồ sơ chứng từ đính kèm Đề nghị giao dịch ngân hàng điện tử (DMS.Sales RQ_BankingTransAttachFile + RQ_BankingTransBankFile): lưu hợp đồng mua bán buôn, ủy nhiệm chi, cam kết bảo lãnh ba bên, chứng thư số điện tử và trạng thái ký số hồ sơ gửi ngân hàng.</summary>
public sealed class BankingTransactionAttachFile
{
    public long Id { get; set; }
    public long BankingTransactionId { get; set; }
    public string RQ_BankingTransNo { get; set; } = "";
    public int FileIndex { get; set; } = 1;
    public BankingTransFileType FileType { get; set; } = BankingTransFileType.Contract;
    public string FileName { get; set; } = "";
    public string FilePath { get; set; } = "";
    public string? FileUrl { get; set; }
    public long? FileSize { get; set; }
    public string? SerialNumber { get; set; } // Serial chữ ký số Token USB / HSM
    public string? CaSubject { get; set; } // Chủ thể chứng thư số CA
    public bool FlagSigned { get; set; } = false; // Cờ đã ký số điện tử
    public DateTime? SignedAt { get; set; } // Thời gian ký số
    public string? SignedBy { get; set; } // Người ký số
    public string? Remark { get; set; } // Ghi chú tài liệu
    public DateTime UploadedAt { get; set; } = DateTime.Now;
    public string? UploadedBy { get; set; }
}

/// <summary>Trạng thái tờ khai hải quan xe ô tô nhập khẩu (DMS.Sales CT_TKHQ: Draft = 'Draft' [Nháp/mới lập], Submitted = 'Submitted' [Đã nộp tờ khai hải quan điện tử VNACCS], TaxPaid = 'TaxPaid' [Đã nộp thuế vào NSNN], Cleared = 'Cleared' [Đã hoàn tất thông quan hải quan/giải phóng xe], Cancelled = 'Cancelled' [Đã hủy tờ khai]).</summary>
public enum CustomsDeclarationStatus
{
    Draft = 0,
    Submitted = 1,
    TaxPaid = 2,
    Cleared = 3,
    Cancelled = 4
}

/// <summary>Phân luồng hải quan điện tử (VNACCS/VCIS: Green = Luồng xanh [Thông quan tự động], Yellow = Luồng vàng [Kiểm tra hồ sơ], Red = Luồng đỏ [Kiểm tra hồ sơ và kiểm hóa thực tế lô xe]).</summary>
public enum CustomsChannel
{
    Green = 0,
    Yellow = 1,
    Red = 2
}

/// <summary>Trạng thái từng dòng xe ô tô trong tờ khai hải quan (DMS.Sales Car_VIN_TKHQ / CT_TKHQ Detail Status: Pending = 0 [Chờ xử lý hải quan], TaxPaid = 1 [Đã nộp thuế], Cleared = 2 [Đã thông quan], Released = 3 [Đã xuất bãi cảng], Cancelled = 4 [Đã hủy]).</summary>
public enum CustomsDeclarationDetailStatus
{
    Pending = 0,
    TaxPaid = 1,
    Cleared = 2,
    Released = 3,
    Cancelled = 4
}

/// <summary>Quản lý Tờ khai Hải quan Xe Ô tô Nhập khẩu (DMS.Sales CT_TKHQ + Car_VIN_TKHQ / CTTKHQController.cs / CTTKHQ.txt / Contract.cs / 05_QUAN_LY_XE.md): Quản lý khai báo hải quan điện tử cho các lô xe ô tô nguyên chiếc (CBU) cập cảng biển (Hải Phòng - HPH, Cát Lái - SGN, Cái Mép - CM...), liên kết danh sách số khung xe VIN 17 ký tự, số hợp đồng ngoại thương (ContractNo) và thư tín dụng (LCNo), theo dõi phân luồng hải quan (Green/Yellow/Red), cập nhật ngày nộp thuế nhập khẩu và thuế TTĐB vào ngân sách nhà nước (UpdateTaxPaymentDateHQ), thẩm định và nghiệm thu thông quan chính thức (Customs Clearance) để giải phóng lô xe khỏi kho cảng, đủ điều kiện pháp lý để đưa vào phân bổ Map VIN hoặc xuất kho giao cho đại lý.</summary>
public sealed class CustomsDeclaration
{
    public long Id { get; set; }
    public Guid OrgId { get; set; }
    public string DeclarationNo { get; set; } = ""; // Số tờ khai hải quan ({yyMM}TK{seq:D5} hoặc mã tờ khai hải quan điện tử 12 chữ số)
    public string PortCode { get; set; } = ""; // Cảng làm thủ tục hải quan (HPH, SGN, CM, DAN...)
    public string? PortName { get; set; } // Tên cảng biển tiếp nhận
    public string? ContractNo { get; set; } // Số hợp đồng ngoại thương liên quan (CT_ContractOversea)
    public DateTime OpenDate { get; set; } = DateTime.Today; // Ngày mở tờ khai hải quan
    public DateTime? TaxPaymentDate { get; set; } // Ngày nộp thuế hải quan (UpdateTaxPaymentDateHQ)
    public string? TaxReceiptNo { get; set; } // Số chứng từ / biên lai nộp thuế kho bạc nhà nước
    public string? TaxPayerBank { get; set; } // Ngân hàng trích nộp thuế (Vietcombank, BIDV, VietinBank...)
    public string? CustomsOffice { get; set; } // Chi cục hải quan cửa khẩu thụ lý
    public CustomsChannel Channel { get; set; } = CustomsChannel.Yellow; // Phân luồng tờ khai (Xanh, Vàng, Đỏ)
    public CustomsDeclarationStatus Status { get; set; } = CustomsDeclarationStatus.Draft; // Trạng thái tờ khai
    public int TotalCars { get; set; } // Tổng số lượng xe ô tô trong tờ khai
    public decimal DutiableAmount { get; set; } // Tổng trị giá tính thuế (CIF quy đổi VNĐ)
    public decimal ImportTaxAmount { get; set; } // Thuế nhập khẩu ô tô
    public decimal SpecialConsumptionTaxAmount { get; set; } // Thuế tiêu thụ đặc biệt (SCT)
    public decimal VatAmount { get; set; } // Thuế giá trị gia tăng (VAT hàng nhập khẩu)
    public decimal TotalTaxAmount { get; set; } // Tổng số tiền thuế các loại phải nộp = ImportTax + SCT + VAT
    public DateTime? CustomsClearanceDate { get; set; } // Ngày cấp phép thông quan chính thức
    public string? CustomsOfficer { get; set; } // Hải quan viên thụ lý / kiểm hóa
    public string? Remark { get; set; } // Ghi chú tờ khai hải quan
    public string? CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public string? SubmittedBy { get; set; }
    public DateTime? SubmittedAt { get; set; }
    public string? TaxPaidBy { get; set; }
    public string? ClearedBy { get; set; }
    public string? CancelledBy { get; set; }
    public DateTime? CancelledAt { get; set; }
    public string? CancelReason { get; set; }
    public DateTime? LUDateTime { get; set; }
    public string? LUBy { get; set; }

    public List<CustomsDeclarationDetail> Details { get; set; } = new();
}

/// <summary>Chi tiết dòng xe ô tô trong tờ khai hải quan nhập khẩu (DMS.Sales Car_VIN_TKHQ / CT_TKHQ Detail): Quản lý từng số khung xe VIN 17 ký tự, số máy, model xe, bản cấu hình spec, màu ngoại thất, số Packing List và LC liên kết, trị giá tính thuế CIF, thuế nhập khẩu, thuế tiêu thụ đặc biệt, thuế GTGT và trạng thái thông quan của xe.</summary>
public sealed class CustomsDeclarationDetail
{
    public long Id { get; set; }
    public long DeclarationId { get; set; }
    public string DeclarationNo { get; set; } = "";
    public string Vin { get; set; } = ""; // Số khung xe chuẩn 17 ký tự (VIN)
    public string? EngineNo { get; set; } // Số máy
    public string ModelCode { get; set; } = ""; // Mã model (SANTAFE, TUCSON, PALISADE, IONIQ5...)
    public string ModelName { get; set; } = ""; // Tên model xe
    public string? SpecCode { get; set; } // Mã bản cấu hình
    public string? SpecDescription { get; set; } // Mô tả bản cấu hình
    public string? ColorCode { get; set; } // Mã màu ngoại thất
    public string? ColorName { get; set; } // Tên màu ngoại thất
    public string? PackingListNo { get; set; } // Số Packing List lô xe nhập khẩu
    public string? LCNo { get; set; } // Số LC thanh toán quốc tế
    public string? ContractNo { get; set; } // Số hợp đồng ngoại thương
    public decimal DutiableValue { get; set; } // Trị giá tính thuế của xe (VNĐ)
    public decimal ImportTax { get; set; } // Thuế nhập khẩu của xe
    public decimal SpecialConsumptionTax { get; set; } // Thuế TTĐB của xe
    public decimal Vat { get; set; } // Thuế GTGT của xe
    public decimal TotalTax { get; set; } // Tổng tiền thuế của xe
    public CustomsDeclarationDetailStatus Status { get; set; } = CustomsDeclarationDetailStatus.Pending;
    public DateTime? TaxPaymentDate { get; set; } // Ngày nộp thuế của xe
    public DateTime? ClearedAt { get; set; } // Ngày thông quan của xe
    public string? Remark { get; set; }
}

/// <summary>Trạng thái Bảng kê Quyết toán Chi phí Quản lý Thiết bị GPS Xe Ô tô (DMS.Sales Pmt_PaymentGPS: Draft = 'Draft' [Nháp], Pending = 'Pending' [Chờ duyệt đối soát], Approved1 = 'Approve1' [Bán hàng HTV duyệt cấp 1], Approved2 = 'Approve2' [TCKT HTV duyệt cấp 2 & ký số], TCMSSigned = 'TCMSSigned' [TCMS ký số hoàn tất quyết toán], Rejected = 'Reject' [Từ chối duyệt], Cancelled = 'Cancel' [Đã hủy]).</summary>
public enum PaymentGPSStatus
{
    Draft = 0,
    Pending = 1,
    Approved1 = 2,
    Approved2 = 3,
    TCMSSigned = 4,
    Rejected = 5,
    Cancelled = 6
}

/// <summary>Trạng thái ký số điện tử biên bản quyết toán GPS (DMS.Sales HTVSignStatus / TCMSSignStatus: ChuaKy = 0, DaKy = 1).</summary>
public enum PaymentGPSSignStatus
{
    ChuaKy = 0,
    DaKy = 1
}

/// <summary>Bảng kê Quyết toán Chi phí Quản lý Thiết bị Giám sát Định vị GPS Xe Ô tô HTV - TCMS (DMS.Sales Pmt_PaymentGPS / PmtPaymentGPSController / Payment.cs / PmtPaymentGPS.txt / 10_GPS_TRACKING.md): Định kỳ hàng tháng NPP Hyundai Thành Công (HTV) và Công ty Quản lý Thiết bị Viễn thông (TCMS) lập bảng kê đối soát chi phí quản lý dịch vụ thiết bị GPS lắp trên xe ô tô lưu kho bãi, xe vận chuyển đường bộ và xe giao đại lý, tính phí theo số ngày kích hoạt thực tế (ActualCostGPSDate) trừ số ngày khấu trừ gián đoạn tín hiệu (DeductDate), nhân đơn giá ngày (Mst_UnitPriceGPS), quy trình phê duyệt đối soát 2 cấp 2 bên (HTV Bán hàng duyệt cấp 1 Approve1HQ, HTV TCKT duyệt cấp 2 & ký số E-Sign Approve2HQ, TCMS ký số biên bản đối soát hoàn tất TCMSSign).</summary>
public sealed class PaymentGPSOrder
{
    public long Id { get; set; }
    public Guid OrgId { get; set; }
    public string PaymentGPSNo { get; set; } = ""; // Mã bảng kê ({yyMM}PG{seq:D4}, vd: 2603PG0001)
    public string PmtMonth { get; set; } = ""; // Tháng quyết toán (yyyy-MM, vd: 2026-03)
    public DateTime FromDate { get; set; } // Ngày bắt đầu kỳ tính phí
    public DateTime ToDate { get; set; } // Ngày kết thúc kỳ tính phí
    public int TotalCars { get; set; } // Tổng số lượng xe tính phí trong bảng kê
    public decimal AmountTotal { get; set; } // Tổng chi phí GPS trước thuế VAT (VNĐ)
    public decimal VatRate { get; set; } = 10m; // Thuế suất VAT (%)
    public decimal UnitPriceVAT { get; set; } // Tiền thuế GTGT (VNĐ) = AmountTotal * VatRate / 100
    public decimal TotalAmountVAT { get; set; } // Tổng số tiền thanh toán sau VAT (VNĐ) = AmountTotal + UnitPriceVAT
    public PaymentGPSStatus Status { get; set; } = PaymentGPSStatus.Draft;

    // Ký số phía Nhà phân phối HTV:
    public PaymentGPSSignStatus HTVSignStatus { get; set; } = PaymentGPSSignStatus.ChuaKy;
    public DateTime? HTVSignDate { get; set; }
    public string? HTVSignBy { get; set; }

    // Ký số phía Đơn vị dịch vụ quản lý thiết bị GPS (TCMS):
    public PaymentGPSSignStatus TCMSSignStatus { get; set; } = PaymentGPSSignStatus.ChuaKy;
    public DateTime? TCMSSignDate { get; set; }
    public string? TCMSSignBy { get; set; }

    public string? FilePath { get; set; } // Đường dẫn / URL file quyết toán PDF ký số 2 bên
    public string? Remark { get; set; } // Ghi chú bảng kê
    public string? RejectReason { get; set; } // Lý do từ chối duyệt
    public string? CancelReason { get; set; } // Lý do hủy bảng kê

    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public string? CreatedBy { get; set; }
    public DateTime? Approved1At { get; set; }
    public string? Approved1By { get; set; }
    public DateTime? Approved2At { get; set; }
    public string? Approved2By { get; set; }
    public DateTime? RejectedAt { get; set; }
    public string? RejectedBy { get; set; }
    public DateTime? CancelledAt { get; set; }
    public string? CancelledBy { get; set; }
    public DateTime? LUDateTime { get; set; }
    public string? LUBy { get; set; }

    public List<PaymentGPSDetail> Details { get; set; } = new();
}

/// <summary>Chi tiết dòng xe ô tô tính phí quản lý thiết bị GPS trong kỳ (DMS.Sales Pmt_PaymentGPSDetail): theo dõi từng số khung VIN, model xe, mã thiết bị GPS gắn xe (GPSDvNo), ngày map kích hoạt GPS (GPSStartDate), ngày bán lẻ giao khách (RetailDate), khoảng ngày tính phí trong kỳ (CostGPSStartDate -> CostGPSEndDate), số ngày dự kiến (PlanCostGPSDate), số ngày khấu trừ vi phạm/lỗi tín hiệu (DeductDate), số ngày tính phí thực tế (ActualCostGPSDate), đơn giá GPS/ngày (PriceGPS) và thành tiền chi phí GPS (AmountGPS).</summary>
public sealed class PaymentGPSDetail
{
    public long Id { get; set; }
    public long PaymentGPSId { get; set; }
    public string PaymentGPSNo { get; set; } = "";
    public string Vin { get; set; } = ""; // Số khung xe chuẩn 17 ký tự (VIN)
    public string? CarId { get; set; } // Mã định danh xe hệ thống
    public string ModelCode { get; set; } = ""; // Mã model (SANTAFE, TUCSON, CRETA, ACCENT, ELANTRA...)
    public string ModelName { get; set; } = ""; // Tên thương mại dòng xe
    public string? SpecCode { get; set; } // Mã cấu hình xe
    public string? ColorCode { get; set; } // Mã màu xe
    public string? PlateNo { get; set; } // Biển số xe (nếu có)
    public string? DealerCode { get; set; } // Đại lý đang nhận xe / bán xe
    public string? DealerName { get; set; } // Tên đại lý
    public string GPSDvNo { get; set; } = ""; // Mã thiết bị giám sát hành trình GPS gắn xe
    public DateTime GPSStartDate { get; set; } // Thời điểm map gắn GPS vào xe
    public DateTime? RetailDate { get; set; } // Ngày xe bàn giao bán lẻ cho khách hàng
    public DateTime CostGPSStartDate { get; set; } // Ngày bắt đầu tính phí GPS trong kỳ này
    public DateTime CostGPSEndDate { get; set; } // Ngày kết thúc tính phí GPS trong kỳ này
    public int PlanCostGPSDate { get; set; } // Số ngày tính phí GPS dự kiến = (CostGPSEndDate - CostGPSStartDate).Days + 1
    public int DeductDate { get; set; } // Số ngày khấu trừ (mất tín hiệu, bảo hành, nằm kho chờ)
    public int ActualCostGPSDate { get; set; } // Số ngày tính phí thực tế = PlanCostGPSDate - DeductDate
    public decimal PriceGPS { get; set; } = 1500m; // Đơn giá dịch vụ GPS theo ngày (VNĐ/ngày)
    public decimal AmountGPS { get; set; } // Thành tiền phí GPS = ActualCostGPSDate * PriceGPS (VNĐ)
    public string? Remark { get; set; } // Ghi chú dòng xe
}

/// <summary>Bảng đơn giá định mức dịch vụ quản lý thiết bị GPS xe ô tô (DMS.Sales Mst_UnitPriceGPS / MstUnitPriceGPSController / MstUnitPriceGPS.txt): quản lý đơn giá cước quản lý định vị theo ngày hoặc tháng áp dụng theo kỳ hiệu lực (EffStartDate -> EffEndDate).</summary>
public sealed class GpsUnitPriceMaster
{
    public long Id { get; set; }
    public string PriceCode { get; set; } = ""; // Mã biểu giá (vd: GPS-STD-2026)
    public string PriceName { get; set; } = ""; // Tên gói cước định vị GPS
    public decimal DailyRate { get; set; } = 1500m; // Đơn giá theo ngày (VNĐ/ngày/xe, vd: 1,500 đ)
    public decimal MonthlyRate { get; set; } = 45000m; // Đơn giá theo tháng quy chuẩn (VNĐ/tháng/xe, vd: 45,000 đ)
    public DateTime EffStartDate { get; set; } = new DateTime(2026, 1, 1); // Ngày bắt đầu áp dụng
    public DateTime? EffEndDate { get; set; } // Ngày kết thúc áp dụng (null nếu đang hiệu lực)
    public bool IsActive { get; set; } = true;
    public string? Remark { get; set; }
}

/// <summary>Trạng thái Bảng kê Quyết toán Chi phí Lưu kho Xe Ô tô HTV - TCMS (DMS.Sales Pmt_PaymentStorage: Pending = 'P' [Chờ duyệt cấp 1], Approved1 = 'A1' [Quản lý kho/Trưởng phòng bán hàng duyệt cấp 1], Approved2 = 'A2' [Lãnh đạo HTC/HTV duyệt cấp 2], Finished = 'F' [Ký số 2 bên hoàn tất/quyết toán xong], Rejected = 'R' [Từ chối duyệt], Cancelled = 'C' [Đã hủy]).</summary>
public enum PaymentStorageStatus
{
    Pending = 0,
    Approved1 = 1,
    Approved2 = 2,
    Finished = 3,
    Rejected = 4,
    Cancelled = 5
}

/// <summary>Trạng thái ký số điện tử biên bản quyết toán lưu kho (DMS.Sales HTVSignStatus / TCMSSignStatus: ChuaKy = 0 ['P'], DaKy = 1 ['A']).</summary>
public enum PaymentStorageSignStatus
{
    ChuaKy = 0,
    DaKy = 1
}

/// <summary>Bảng kê Quyết toán Chi phí Lưu kho Xe Ô tô HTV - TCMS (DMS.Sales Pmt_PaymentStorage / PmtPaymentStorageController / PaymentStorage.cs / PmtPaymentStorage.txt / ChiPhiLuuKhoHTV.TCMS.xlsx): Định kỳ hàng tháng hoặc theo kỳ liên tiếp, Nhà phân phối ô tô HTV và Công ty Cổ phần Vận hành Kho vận TCMS lập bảng kê đối soát thanh toán chi phí lưu kho bãi và bọc che phủ (Bạt Che Phủ - BCP) cho các xe ô tô lưu bãi, xe vận chuyển, xe bàn giao đại lý; kiểm tra tính liên tục không ngắt quãng giữa các kỳ tính (StartDTime = LastEndDTime + 1 ngày); quy trình phê duyệt đối soát 2 cấp 2 bên (Trưởng phòng bán hàng/kho duyệt cấp 1 Approve1HQ, Lãnh đạo HTC/HTV duyệt cấp 2 Approve2HQ, TCMS ký số điện tử TCMSESignHQ, HTV ký số điện tử HTVESignHQ và cập nhật hóa đơn GTGT do TCMS xuất UploadFileInvoiceHQ).</summary>
public sealed class PaymentStorageOrder
{
    public long Id { get; set; }
    public Guid OrgId { get; set; }
    public string PaymentStorageNo { get; set; } = ""; // Mã bảng kê ({yyMM}PST{seq:D5}, vd: 2603PST00001)
    public string PmtMonth { get; set; } = ""; // Tháng thanh toán (yyyy-MM-01 hoặc yyyy-MM, vd: 2026-03-01)
    public DateTime PmtPeriodStartDTime { get; set; } // Ngày bắt đầu kỳ tính (phải thuộc PmtMonth)
    public DateTime PmtPeriodEndDTime { get; set; } // Ngày kết thúc kỳ tính (phải thuộc PmtMonth, >= StartDTime)
    public decimal VAT { get; set; } = 10m; // Thuế suất VAT (%)
    public int TotalCars { get; set; } // Tổng số lượng xe tính phí trong bảng kê
    public decimal AmountBCP { get; set; } // Tổng chi phí bọc che phủ trước VAT = SUM(Detail.AmountBCP)
    public decimal AmountLK { get; set; } // Tổng chi phí lưu kho trước VAT = SUM(Detail.AmountLK)
    public decimal AmountTotal { get; set; } // Tổng chi phí trước VAT = AmountBCP + AmountLK = SUM(Detail.AmountTotal)
    public decimal AmountVAT { get; set; } // Tiền thuế GTGT = AmountTotal * VAT / 100
    public decimal AmountVATTotal { get; set; } // Tổng chi phí thanh toán sau VAT = AmountTotal + AmountVAT
    public PaymentStorageStatus Status { get; set; } = PaymentStorageStatus.Pending;

    // Ký số phía Nhà phân phối HTV:
    public PaymentStorageSignStatus HTVSignStatus { get; set; } = PaymentStorageSignStatus.ChuaKy;
    public DateTime? HTVSignDTime { get; set; }
    public string? HTVSignBy { get; set; }

    // Ký số phía Đơn vị dịch vụ quản lý kho bãi TCMS:
    public PaymentStorageSignStatus TCMSSignStatus { get; set; } = PaymentStorageSignStatus.ChuaKy;
    public DateTime? TCMSSignDTime { get; set; }
    public string? TCMSSignBy { get; set; }

    // File hợp đồng / biên bản điện tử và hóa đơn GTGT:
    public string? FilePath { get; set; } // File hợp đồng điện tử ký số
    public string? FileUrl { get; set; }
    public string? FileInvPath { get; set; } // File hóa đơn GTGT do TCMS xuất
    public string? FileInvUrl { get; set; }
    public string? InvoiceNo { get; set; } // Số hóa đơn GTGT dịch vụ lưu kho
    public DateTime? InvoiceDate { get; set; } // Ngày lập hóa đơn GTGT

    public string? Remark { get; set; } // Ghi chú phiếu
    public string? RejectReason { get; set; } // Lý do từ chối duyệt
    public string? CancelReason { get; set; } // Lý do hủy phiếu

    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public string? CreatedBy { get; set; }
    public DateTime? Approve1At { get; set; }
    public string? Approve1By { get; set; }
    public DateTime? Approve2At { get; set; }
    public string? Approve2By { get; set; }
    public DateTime? RejectedAt { get; set; }
    public string? RejectedBy { get; set; }
    public DateTime? CancelledAt { get; set; }
    public string? CancelledBy { get; set; }
    public DateTime? LogLUDateTime { get; set; }
    public string? LogLUBy { get; set; }

    public List<PaymentStorageDetail> Details { get; set; } = new();
}

/// <summary>Chi tiết dòng xe ô tô tính phí lưu kho & bọc che phủ trong kỳ (DMS.Sales Pmt_PaymentStorageDetail): theo dõi từng số khung VIN 17 ký tự, số tham chiếu RefNo (lệnh xuất kho, điều chuyển, nhập cảng...), kho lưu bãi StorageCode, ngày vào kho StorageDateIn, ngày ra kho StorageDateOut, khoảng ngày tính phí trong kỳ (DateBegin -> DateEnd), số ngày trễ vận chuyển miễn giảm DelayTransport, số ngày tính phí thực tế DateCount, tiền bọc che phủ AmountBCP và tiền lưu kho AmountLK, tổng tiền dòng xe AmountTotal.</summary>
public sealed class PaymentStorageDetail
{
    public long Id { get; set; }
    public long PaymentStorageId { get; set; }
    public string PaymentStorageNo { get; set; } = "";
    public string Vin { get; set; } = ""; // Số khung xe chuẩn 17 ký tự (VIN, bắt buộc)
    public string RefNo { get; set; } = ""; // Số tham chiếu giao dịch kho bãi (bắt buộc, vd: DO, LK, RCB, TH...)
    public string? CarId { get; set; } // Mã định danh xe hệ thống
    public string ModelCode { get; set; } = ""; // Mã model (SANTAFE, TUCSON, CRETA, ACCENT, PALISADE, IONIQ5...)
    public string ModelName { get; set; } = ""; // Tên thương mại dòng xe
    public string? SpecCode { get; set; } // Mã cấu hình xe
    public string? ColorCode { get; set; } // Mã màu xe
    public string StorageCode { get; set; } = ""; // Mã kho bãi (KHO_NB, KHO_DY, KHO_CANG_HP, KHO_CANG_CM...)
    public string? StorageName { get; set; } // Tên kho bãi
    public DateTime? StorageDateIn { get; set; } // Ngày xe nhập kho bãi
    public DateTime? StorageDateOut { get; set; } // Ngày xe xuất khỏi kho bãi
    public DateTime DateBegin { get; set; } // Ngày bắt đầu tính phí trong kỳ
    public DateTime DateEnd { get; set; } // Ngày kết thúc tính phí trong kỳ
    public int DelayTransport { get; set; } = 0; // Số ngày miễn phí do trễ điều động vận tải
    public int DateCount { get; set; } // Số ngày tính phí thực tế = (DateEnd - DateBegin + 1) - DelayTransport
    public decimal PriceBCP { get; set; } // Đơn giá bọc che phủ (VNĐ/xe/ngày)
    public decimal PriceLK { get; set; } // Đơn giá lưu kho bãi (VNĐ/xe/ngày)
    public decimal AmountBCP { get; set; } // Tiền bọc che phủ = DateCount * PriceBCP (VNĐ)
    public decimal AmountLK { get; set; } // Tiền lưu kho = DateCount * PriceLK (VNĐ)
    public decimal AmountTotal { get; set; } // Tổng tiền xe = AmountBCP + AmountLK (VNĐ)
    public string? DealerCode { get; set; } // Mã đại lý đích / đại lý nhận xe
    public string? DealerName { get; set; } // Tên đại lý
    public string? PackingListNo { get; set; } // Số Packing List lô xe nhập khẩu
    public string? DeliveryOrderNo { get; set; } // Số lệnh giao xe xuất kho
    public string? StorageRearrangeNoIn { get; set; } // Số lệnh điều chuyển kho đến
    public string? StorageRearrangeNoOut { get; set; } // Số lệnh điều chuyển kho đi
    public string? RetrieveOrderNo { get; set; } // Số lệnh thu hồi xe
    public string? InvoiceFactoryDate { get; set; } // Ngày hóa đơn xuất xưởng nhà máy
    public string? Remark { get; set; } // Ghi chú dòng xe
}

/// <summary>Bảng đơn giá định mức chi phí kho bãi theo dòng xe & kho bãi (DMS.Sales Mst_InventoryCost + Mst_InventoryCost_Model / MstInventoryCostController / MstInventoryCost.txt): Quản lý biểu giá chi phí bọc che phủ (CostTypeCode = 'BCP') và chi phí lưu kho (CostTypeCode = 'LK') theo từng kho lưu bãi (StorageCode: KHO_NB, KHO_DY, KHO_CANG_HP, KHO_CANG_CM) và model xe.</summary>
public sealed class InventoryCostMaster
{
    public long Id { get; set; }
    public string CostTypeCode { get; set; } = ""; // BCP (Bọc che phủ) | LK (Lưu kho)
    public string CostTypeName { get; set; } = ""; // Tên loại chi phí
    public string StorageCode { get; set; } = ""; // Mã kho bãi
    public string StorageName { get; set; } = ""; // Tên kho bãi
    public string ModelCode { get; set; } = ""; // Mã dòng xe (SANTAFE, TUCSON, CRETA, ACCENT, STARGAZER, PALISADE, IONIQ5...)
    public string ModelName { get; set; } = ""; // Tên model
    public decimal UnitPrice { get; set; } // Đơn giá chi phí (VNĐ/xe/ngày)
    public bool IsActive { get; set; } = true;
    public string? Remark { get; set; }
}

/// <summary>Trạng thái Bảng kê Quyết toán Chi phí Bảo dưỡng Xe Ô tô HTV - TCMS (DMS.Sales Pmt_PaymentBD: Pending = 'P' [Chờ duyệt cấp 1], Approved1 = 'A1' [Trưởng phòng bán hàng & kho duyệt cấp 1], Approved2 = 'A2' [Lãnh đạo HTC/HTV duyệt cấp 2], Finished = 'F' [Ký số 2 bên hoàn tất/quyết toán xong], Rejected = 'R' [Từ chối duyệt], Cancelled = 'C' [Đã hủy]).</summary>
public enum PaymentBDStatus
{
    Pending = 0,
    Approved1 = 1,
    Approved2 = 2,
    Finished = 3,
    Rejected = 4,
    Cancelled = 5
}

/// <summary>Trạng thái ký số điện tử biên bản quyết toán chi phí bảo dưỡng (DMS.Sales HTVSignStatus / TCMSSignStatus: ChuaKy = 0 ['P'], DaKy = 1 ['A']).</summary>
public enum PaymentBDSignStatus
{
    ChuaKy = 0,
    DaKy = 1
}

/// <summary>Bảng kê Quyết toán Chi phí Bảo dưỡng Xe Ô tô HTV - TCMS (DMS.Sales Pmt_PaymentBD / PmtPaymentBDController / PaymentBD.cs / ChiPhiBaoDuong.HTV.TCMS.xlsx): Định kỳ hàng tháng hoặc theo kỳ liên tiếp, Nhà phân phối ô tô HTV và Công ty Cổ phần Vận hành Kho vận TCMS lập bảng kê đối soát thanh toán chi phí bảo dưỡng (bảo trì định kỳ 15 ngày/lần) cho các xe ô tô lưu bãi; kiểm tra tính liên tục không ngắt quãng giữa các kỳ tính (StartDTime = LastEndDTime + 1 ngày); quy trình phê duyệt đối soát 2 cấp 2 bên (Trưởng phòng bán hàng/kho duyệt cấp 1 Approve1HQ, Lãnh đạo HTC/HTV duyệt cấp 2 Approve2HQ, TCMS ký số điện tử TCMSESignHQ, HTV ký số điện tử HTVESignHQ và ghi mốc bảo dưỡng gần nhất về Car_VIN).</summary>
public sealed class PaymentBDOrder
{
    public long Id { get; set; }
    public Guid OrgId { get; set; }
    public string PaymentBDNo { get; set; } = ""; // Mã bảng kê ({yyMM}PBD{seq:D5}, vd: 2603PBD00001)
    public string PmtMonth { get; set; } = ""; // Tháng thanh toán (yyyy-MM, vd: 2026-03)
    public DateTime PmtPeriodStartDTime { get; set; } // Ngày bắt đầu kỳ tính (phải liên tiếp kỳ trước)
    public DateTime PmtPeriodEndDTime { get; set; } // Ngày kết thúc kỳ tính (>= StartDTime)
    public decimal VAT { get; set; } = 10m; // Thuế suất VAT (%)
    public int TotalCars { get; set; } // Tổng số lượng xe tính phí trong bảng kê
    public int TotalBDCount { get; set; } // Tổng số lần bảo dưỡng trong kỳ = SUM(Detail.BDCount)
    public decimal AmountTotal { get; set; } // Tổng chi phí bảo dưỡng trước VAT = SUM(Detail.AmountTotal)
    public decimal AmountVAT { get; set; } // Tiền thuế GTGT = AmountTotal * VAT / 100
    public decimal AmountVATTotal { get; set; } // Tổng chi phí thanh toán sau VAT = AmountTotal + AmountVAT
    public PaymentBDStatus Status { get; set; } = PaymentBDStatus.Pending;

    // Ký số phía Nhà phân phối HTV:
    public PaymentBDSignStatus HTVSignStatus { get; set; } = PaymentBDSignStatus.ChuaKy;
    public DateTime? HTVSignDTime { get; set; }
    public string? HTVSignBy { get; set; }

    // Ký số phía Đơn vị dịch vụ quản lý kho bãi TCMS:
    public PaymentBDSignStatus TCMSSignStatus { get; set; } = PaymentBDSignStatus.ChuaKy;
    public DateTime? TCMSSignDTime { get; set; }
    public string? TCMSSignBy { get; set; }

    // File hợp đồng / biên bản điện tử:
    public string? FilePath { get; set; } // File hợp đồng điện tử ký số
    public string? FileUrl { get; set; }

    public string? Remark { get; set; } // Ghi chú phiếu
    public string? RejectReason { get; set; } // Lý do từ chối duyệt
    public string? CancelReason { get; set; } // Lý do hủy phiếu

    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public string? CreatedBy { get; set; }
    public DateTime? Approve1At { get; set; }
    public string? Approve1By { get; set; }
    public DateTime? Approve2At { get; set; }
    public string? Approve2By { get; set; }
    public DateTime? RejectedAt { get; set; }
    public string? RejectedBy { get; set; }
    public DateTime? CancelledAt { get; set; }
    public string? CancelledBy { get; set; }
    public DateTime? LogLUDateTime { get; set; }
    public string? LogLUBy { get; set; }

    public List<PaymentBDDetail> Details { get; set; } = new();
}

/// <summary>Chi tiết dòng xe ô tô tính phí bảo dưỡng trong kỳ (DMS.Sales Pmt_PaymentBDDetail): theo dõi từng số khung VIN 17 ký tự, số tham chiếu RefNo (lệnh xuất kho, điều chuyển, nhập cảng...), kho lưu bãi StorageCode, ngày vào kho StorageDateIn, ngày ra kho StorageDateOut, ngày hóa đơn nhà máy InvoiceFactoryDate, ngày bảo dưỡng kỳ gần nhất LatePmtBDDate, ngày bảo dưỡng lần 1 (BDDate1, mốc 15 ngày) và lần 2 (BDDate2, mốc 30 ngày), số lần bảo dưỡng trong kỳ BDCount, đơn giá UnitPrice và tiền bảo dưỡng BDAmount.</summary>
public sealed class PaymentBDDetail
{
    public long Id { get; set; }
    public long PaymentBDId { get; set; }
    public string PaymentBDNo { get; set; } = "";
    public string Vin { get; set; } = ""; // Số khung xe chuẩn 17 ký tự (VIN, bắt buộc)
    public string RefNo { get; set; } = ""; // Số tham chiếu giao dịch kho bãi (bắt buộc)
    public string? CarId { get; set; } // Mã định danh xe hệ thống
    public string ModelCode { get; set; } = ""; // Mã model (SANTAFE, TUCSON, CRETA, ACCENT...)
    public string ModelName { get; set; } = ""; // Tên thương mại dòng xe
    public string? SpecCode { get; set; } // Mã cấu hình xe
    public string? ColorCode { get; set; } // Mã màu xe
    public string StorageCode { get; set; } = ""; // Mã kho bãi (KHO_NB, KHO_DY, KHO_CANG_HP, KHO_CANG_CM...)
    public string? StorageName { get; set; } // Tên kho bãi
    public DateTime? StorageDateIn { get; set; } // Ngày xe nhập kho bãi
    public DateTime? StorageDateOut { get; set; } // Ngày xe xuất khỏi kho bãi
    public string? InvoiceFactoryDate { get; set; } // Ngày hóa đơn xuất xưởng nhà máy
    public DateTime? LatePmtBDDate { get; set; } // Ngày bảo dưỡng kỳ gần nhất (mốc Car_VIN.PmtBDDate)
    public DateTime? BDDate1 { get; set; } // Ngày bảo dưỡng lần 1 trong kỳ (mốc 15 ngày)
    public DateTime? BDDate2 { get; set; } // Ngày bảo dưỡng lần 2 trong kỳ (mốc 30 ngày)
    public int BDCount { get; set; } // Số lần bảo dưỡng trong kỳ này (0/1/2)
    public decimal UnitPrice { get; set; } // Đơn giá bảo dưỡng (VNĐ/lần)
    public decimal BDAmount { get; set; } // Tiền bảo dưỡng = BDCount * UnitPrice (VNĐ)
    public decimal AmountTotal { get; set; } // Tổng tiền dòng xe = BDAmount (VNĐ)
    public string? DealerCode { get; set; } // Mã đại lý đích / đại lý nhận xe
    public string? DealerName { get; set; } // Tên đại lý
    public string? PackingListNo { get; set; } // Số Packing List lô xe nhập khẩu
    public string? DeliveryOrderNo { get; set; } // Số lệnh giao xe xuất kho
    public string? StorageRearrangeNoIn { get; set; } // Số lệnh điều chuyển kho đến
    public string? StorageRearrangeNoOut { get; set; } // Số lệnh điều chuyển kho đi
    public string? RetrieveOrderNo { get; set; } // Số lệnh thu hồi xe
    public string? Remark { get; set; } // Ghi chú dòng xe
}

/// <summary>Trạng thái thế chấp / giải chấp hồ sơ gốc xe ô tô tại ngân hàng (DMS.Sales Car_VIN StatusMortageEnd: Free = '2' [Chưa thế chấp / tự do], Mortgaged = '0' [Đang thế chấp ngân hàng], Redeemed = '1' [Đã giải chấp hoàn tất]).</summary>
public enum CarMortageStatus
{
    Free = 0,
    Mortgaged = 1,
    Redeemed = 2
}

/// <summary>Hồ sơ Giấy tờ Xe Ô tô & Đăng kiểm CQ/CO & Thế chấp Ngân hàng / Hóa đơn nhà máy (Car VIN Profile Management - DMS.Sales Car_VINProfile + Car_VIN + Pmt_GuaranteeDetail / CarVINProfileController.cs / CarVINProfile.txt / 05_QUAN_LY_XE.md): Quản lý toàn diện hồ sơ pháp lý, kỹ thuật và ngân hàng của từng số khung xe VIN 17 ký tự trong suốt vòng đời phân phối; kiểm soát giấy chứng nhận chất lượng xuất xưởng (CQNo, QCNo, FGFormNo, CQStartDate, CQEndDate), chứng nhận nguồn gốc xuất xứ CO (CONo, CODate), hóa đơn nhà máy sản xuất (InvoiceNoFactory, InvoiceFactoryDate), hóa đơn chuyển nhượng (InvoiceNoTransferred, InvoiceTransferredDate), thông tin thùng xe ô tô thương mại (LoaiThung, CabinCertificateNo, CabinCONo, CabinInvoiceNo), bàn giao hồ sơ gốc cho ngân hàng tài trợ thế chấp (BillNo, HandOverBankCode, MortageEndDate), giải chấp hồ sơ gốc (RedeemDate, StatusMortageEnd), thời hạn bảo lãnh ngân hàng & bảo hành (DateStart, DateExpired, GuaranteeValue), cờ cho phép Đại lý lập đề nghị rút hồ sơ (FlagDocReq) và gửi email nhắc hồ sơ chứng từ.</summary>
public sealed class CarVinProfile
{
    public long Id { get; set; }
    public Guid OrgId { get; set; }
    public string Vin { get; set; } = ""; // Số khung xe chuẩn 17 ký tự (VIN, Unique theo Org)
    public string? CarId { get; set; } // Mã định danh xe thương mại (vd: 2605C64655BN7I-CKD)
    public string DealerCode { get; set; } = ""; // Mã đại lý quản lý xe (VS058, VN001, VN012...)
    public string? DealerName { get; set; } // Tên đại lý
    public string ModelCode { get; set; } = ""; // Mã model (SANTAFE, TUCSON, CRETA, ACCENT, STARGAZER, PORTER, MIGHTY...)
    public string ModelName { get; set; } = ""; // Tên model thương mại
    public string? SpecCode { get; set; } // Mã cấu hình xe (SF25-PRE, TU20-STD, H150-TK...)
    public string? SpecDescription { get; set; } // Mô tả cấu hình xe
    public string? ColorCode { get; set; } // Mã màu ngoại thất
    public string? ColorName { get; set; } // Tên màu xe
    public string? EngineNo { get; set; } // Số máy (Engine Number)
    public string? StorageCode { get; set; } // Mã kho lưu bãi hiện tại (KHO_NB, KHO_DY, KHO_CANG_HP...)
    public string? StorageName { get; set; } // Tên kho lưu bãi

    // Thông tin Đăng kiểm CLXX & Xuất xưởng:
    public string? CQNo { get; set; } // Số Đăng kiểm / Chứng nhận chất lượng CLXX
    public DateTime? CQStartDate { get; set; } // Ngày bắt đầu hiệu lực đăng kiểm
    public DateTime? CQEndDate { get; set; } // Ngày hết hạn đăng kiểm
    public string? FGFormNo { get; set; } // Số phiếu kiểm tra chất lượng xuất xưởng PXX (xe CKD)
    public string? QCNo { get; set; } // Số giấy chứng nhận an toàn kỹ thuật xe
    public DateTime? IssuedDate { get; set; } // Ngày cấp GCN / PXX

    // Chứng nhận nguồn gốc xuất xứ (CO):
    public string? CONo { get; set; } // Số giấy chứng nhận xuất xứ hàng hóa CO
    public DateTime? CODate { get; set; } // Ngày cấp CO
    public DateTime? CODateExpected { get; set; } // Ngày dự kiến có CO

    // Hóa đơn nhà máy & Hóa đơn chuyển nhượng:
    public string? InvoiceNoFactory { get; set; } // Số hóa đơn nhà máy sản xuất
    public DateTime? InvoiceFactoryDate { get; set; } // Ngày hóa đơn nhà máy
    public string? InvoiceFactorySearch { get; set; } // Mã tra cứu hóa đơn điện tử nhà máy
    public string? InvoiceNoTransferred { get; set; } // Số hóa đơn chuyển nhượng nội bộ NPP - Đại lý
    public DateTime? InvoiceTransferredDate { get; set; } // Ngày hóa đơn chuyển nhượng
    public string? InvoiceTransferredSearch { get; set; } // Mã tra cứu hóa đơn chuyển nhượng
    public string? InvoiceSpecName { get; set; } // Tên loại xe trên hóa đơn GTGT

    // Thông tin thùng xe ô tô thương mại (Cabin / Crate Box):
    public bool TypeCB { get; set; } = false; // true = Xe có đóng thùng thương mại
    public string? LoaiThung { get; set; } // Loại thùng: THUNG_KIN, THUNG_BAT, THUNG_DONG_LANH, THUNG_LUNG
    public string? CabinCertificateNo { get; set; } // Số GCN chất lượng thùng xe
    public DateTime? CabinCertificateDate { get; set; } // Ngày nhận phiếu chứng nhận thùng
    public string? CabinCONo { get; set; } // Số chứng nhận nguồn gốc xuất xứ thùng
    public string? CabinInvoiceNo { get; set; } // Số hóa đơn thùng xe
    public DateTime? CabinInvoiceDate { get; set; } // Ngày hóa đơn thùng xe

    // Thế chấp ngân hàng & Bàn giao hồ sơ gốc:
    public CarMortageStatus MortageStatus { get; set; } = CarMortageStatus.Free; // Free / Mortgaged / Redeemed
    public string? MortageBankCode { get; set; } // Mã ngân hàng thế chấp tài sản (VCB, TCB, BIDV, CTG, VPB...)
    public string? MortageBankName { get; set; } // Tên ngân hàng thế chấp tài sản
    public DateTime? MortageStartDate { get; set; } // Ngày bắt đầu thế chấp tài sản
    public string? HandOverBankCode { get; set; } // Mã ngân hàng nhận bàn giao hồ sơ gốc
    public string? HandOverBankName { get; set; } // Tên ngân hàng nhận bàn giao hồ sơ gốc
    public string? BillNo { get; set; } // Số vận đơn / số biên nhận bàn giao hồ sơ gốc
    public DateTime? MortageEndDate { get; set; } // Ngày bàn giao hồ sơ gốc cho ngân hàng
    public DateTime? RedeemDate { get; set; } // Ngày giải chấp ngân hàng hoàn tất
    public DateTime? LogDateTimeStatusMortageEnd { get; set; } // Thời điểm ghi nhận giải chấp

    // Bảo lãnh & Bảo hành điện tử:
    public string? GuaranteeNo { get; set; } // Mã bảo lãnh thanh toán (Pmt_Guarantee)
    public string? BankGuaranteeNo { get; set; } // Số bảo lãnh do ngân hàng phát hành
    public DateTime? DateStart { get; set; } // Ngày kích hoạt hiệu lực bảo lãnh / bảo hành xe
    public DateTime? DateExpired { get; set; } // Ngày hết hạn bảo lãnh / bảo hành xe
    public decimal? GuaranteeValue { get; set; } // Giá trị bảo lãnh xe (VNĐ)
    public bool FlagDtlDiscount { get; set; } = false; // Cờ giảm giá / chiết khấu theo bảo lãnh
    public bool FlagGrtExt { get; set; } = false; // Cờ gia hạn bảo lãnh

    // Trạng thái hồ sơ & Cờ Đề nghị rút hồ sơ:
    public string DocumentsStatus { get; set; } = "0"; // "0" = Chưa đủ hồ sơ gốc, "1" = Đã đủ hồ sơ gốc
    public string FlagDocReq { get; set; } = "0"; // "0" = Khóa rút hồ sơ, "1" = Đã mở cờ cho phép Đại lý lập ĐNGT rút hồ sơ
    public DateTime? DocDeliveryReqDate { get; set; } // Ngày đại lý lập ĐNGT rút hồ sơ
    public DateTime? SendMailDocDate { get; set; } // Thời điểm gửi email nhắc hồ sơ gần nhất
    public string? Remark { get; set; } // Ghi chú hồ sơ xe

    // Audit fields:
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public string? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }
}

/// <summary>Trạng thái Phiếu hiệu suất / Proforma Invoice (DMS.Sales Ord_PerformanceInvoice: Pending = 0 [Chờ xác nhận], Confirmed = 1 [Đã chốt kế hoạch], Contracted = 2 [Đã vào HĐ ngoại], Cancelled = 3 [Đã hủy]).</summary>
public enum PerformanceInvoiceStatus
{
    Pending = 0,
    Confirmed = 1,
    Contracted = 2,
    Cancelled = 3
}

/// <summary>Quản lý Phiếu hiệu suất / Proforma Invoice đặt hàng sản xuất & nhập khẩu ô tô lô lớn giữa NPP và Nhà máy (DMS.Sales Ord_PerformanceInvoice / OrdPerformanceInvoiceController / OrdPerformanceInvoice.txt / Order.cs / Car.cs): Quản lý kế hoạch đặt hàng xe theo tháng đặt hàng (OrderMonth) và tháng sản xuất (ProductionMonth), tính tháng dự kiến giao (ExpectedMonth = ProductionMonth + 1 tháng), điều khiển cờ tự động sinh Packing List (FlagAutoPL), liên kết thư tín dụng L/C (LCTemp), lệnh sản xuất nhà máy (WorkOrderNo), cấu hình và màu sắc xe, phục vụ chốt hợp đồng ngoại thương (CT_ContractOversea) và điều phối chuỗi cung ứng xe ô tô.</summary>
public sealed class PerformanceInvoice
{
    public long Id { get; set; }
    public Guid OrgId { get; set; }
    public string RefNo { get; set; } = ""; // Số hiệu PI (PK RefNo, format {yyMM}PI{seq:D4}, vd: 2603PI0001)
    public string OrderMonth { get; set; } = ""; // Tháng đặt hàng (yyyy-MM-01, vd: 2026-03-01)
    public string ProductionMonth { get; set; } = ""; // Tháng sản xuất (yyyy-MM-01, vd: 2026-04-01)
    public string ExpectedMonth { get; set; } = ""; // Tháng dự kiến hoàn tất/giao (yyyy-MM-01, ProductionMonth + 1 tháng)
    public string FlagAutoPL { get; set; } = "1"; // Cờ tự động sinh Packing List: "1" = Có, "0" = Không
    public PerformanceInvoiceStatus Status { get; set; } = PerformanceInvoiceStatus.Pending; // Trạng thái PI
    public int TotalQuantity { get; set; } // Tổng số lượng xe trong PI
    public decimal TotalAmount { get; set; } // Tổng giá trị PI
    public string? Remark { get; set; } // Ghi chú PI
    public string? CancelReason { get; set; } // Lý do hủy PI
    public string? CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public string? UpdatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string? ConfirmedBy { get; set; }
    public DateTime? ConfirmedAt { get; set; }
    public string? CancelledBy { get; set; }
    public DateTime? CancelledAt { get; set; }

    public List<PerformanceInvoiceDetail> Details { get; set; } = new();
}

/// <summary>Chi tiết từng dòng xe trong Phiếu hiệu suất / Proforma Invoice (DMS.Sales Ord_PerformanceInvoiceDetail): quản lý số lượng xe theo mã L/C tạm, mã cấu hình spec, model, màu ngoại thất, lệnh sản xuất WorkOrderNo, mã cảng nhập khẩu và mã nhà máy sản xuất, theo dõi liên kết số hợp đồng ngoại thương ContractNo.</summary>
public sealed class PerformanceInvoiceDetail
{
    public long Id { get; set; }
    public long PerformanceInvoiceId { get; set; }
    public string RefNo { get; set; } = ""; // Khóa ngoại số PI
    public string LCTemp { get; set; } = ""; // Mã L/C tạm thời (vd: LC2603001)
    public string SpecCode { get; set; } = ""; // Mã cấu hình xe (SF25-PRE, TU20-STD...)
    public string ModelCode { get; set; } = ""; // Mã model (SANTAFE, TUCSON, CRETA, ACCENT, BN7I-CKD...)
    public string? ModelName { get; set; } // Tên model xe
    public string ColorCode { get; set; } = ""; // Mã màu ngoại thất (WHT, BLK, RED, SIL...)
    public string? ColorName { get; set; } // Tên màu xe
    public string WorkOrderNo { get; set; } = ""; // Số lệnh sản xuất nhà máy (WO2603001)
    public string PortCode { get; set; } = ""; // Cảng nhập khẩu (HPH: Hải Phòng, SGN: Sài Gòn, CM: Cái Mép...)
    public string PlantCode { get; set; } = ""; // Nhà máy sản xuất (HMC-ASAN, HMC-ULSAN, HTMV-NB...)
    public int Quantity { get; set; } // Số lượng xe đặt hàng
    public decimal UnitPrice { get; set; } // Đơn giá xe dự kiến
    public decimal TotalAmount { get; set; } // Thành tiền = Quantity * UnitPrice
    public string? ContractNo { get; set; } // Số hợp đồng ngoại thương (CT_ContractOversea) khi được gắn vào HĐ
    public string FlagAutoPL { get; set; } = "1"; // Cờ tự động PL theo dòng
    public string? Remark { get; set; }
}

/// <summary>Trạng thái Hợp đồng Ngoại thương mua bán xe ô tô nhập khẩu (DMS.Sales CT_ContractOversea: Draft = 0 [Mới lập], Active = 1 [Đã ký kết/Hiệu lực], LinkedLC = 2 [Đã mở L/C ngân hàng], Completed = 3 [Hoàn tất nhận hàng & thanh toán], Cancelled = 4 [Đã hủy hợp đồng]).</summary>
public enum ContractOverseaStatus
{
    Draft = 0,
    Active = 1,
    LinkedLC = 2,
    Completed = 3,
    Cancelled = 4
}

/// <summary>Loại hình Thư tín dụng ngoại thương (DMS.Sales CT_LC LCType: AtSight = 0 [Trả ngay], Usance = 1 [Trả chậm], UPAS = 2 [UPAS L/C]).</summary>
public enum LCType
{
    AtSight = 0,
    Usance = 1,
    UPAS = 2
}

/// <summary>Trạng thái Thư tín dụng ngoại thương (DMS.Sales CT_LC LCStatus: Active = 0 [Đang hiệu lực], Settled = 1 [Đã quyết toán/giải tỏa], Cancelled = 2 [Đã hủy/hết hiệu lực]).</summary>
public enum LCStatus
{
    Active = 0,
    Settled = 1,
    Cancelled = 2
}

/// <summary>Quản lý Hợp đồng Ngoại thương mua bán xe ô tô nhập khẩu giữa NPP và Tập đoàn sản xuất nước ngoài HMC (DMS.Sales CT_ContractOversea / CTContractOverseaController / CTContractOversea.txt / Contract.cs): Quản lý số hợp đồng ngoại ContractNo, liên kết các dòng PI (Ord_PerformanceInvoiceDetail), liên kết thư tín dụng L/C (CT_LC), điều kiện giao hàng Incoterms, cảng đích, theo dõi số lượng và giá trị USD lô xe nhập khẩu.</summary>
public sealed class ContractOversea
{
    public long Id { get; set; }
    public Guid OrgId { get; set; }
    public string ContractNo { get; set; } = ""; // Số hợp đồng ngoại thương (PK / Mã HĐ, format {yyMM}HMC{seq:D4} hoặc {yyMM}DRC{seq:D5})
    public DateTime ContractDate { get; set; } = DateTime.Today; // Ngày ký kết HĐ ngoại
    public string SupplierCode { get; set; } = "HMC"; // Mã đối tác ngoại thương (HMC)
    public string SupplierName { get; set; } = "Hyundai Motor Company Korea"; // Tên đối tác ngoại thương
    public string? LCNo { get; set; } // Số thư tín dụng L/C liên kết (CT_LC.LCNo)
    public string? BankName { get; set; } // Ngân hàng mở L/C
    public string Incoterms { get; set; } = "CIF Hai Phong"; // Điều kiện giao nhận ngoại thương (CIF, FOB, CFR...)
    public string DestinationPort { get; set; } = "HPH"; // Cảng đích nhận hàng (HPH, SGN, CM...)
    public string Currency { get; set; } = "USD"; // Tiền tệ thanh toán ngoại thương
    public DateTime? ExpectedDeliveryDate { get; set; } // Ngày dự kiến giao hàng cảng
    public int TotalQuantity { get; set; } // Tổng số lượng xe theo HĐ
    public decimal TotalAmount { get; set; } // Tổng trị giá HĐ (USD)
    public ContractOverseaStatus Status { get; set; } = ContractOverseaStatus.Active; // Trạng thái HĐ ngoại
    public string? Remark { get; set; } // Ghi chú hợp đồng
    public string? CancelReason { get; set; } // Lý do hủy HĐ
    public string? CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public string? UpdatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string? CancelledBy { get; set; }
    public DateTime? CancelledAt { get; set; }
}

/// <summary>Quản lý Thư tín dụng / Letter of Credit (L/C) Ngoại thương thanh toán lô xe nhập khẩu (DMS.Sales CT_LC / CTLCController / CTLC.txt / Contract.cs): Quản lý số hiệu L/C mở tại ngân hàng tài trợ thương mại (VCB, BIDV, CTG...), liên kết hợp đồng ngoại thương ContractNo, theo dõi loại L/C (AtSight, Usance, UPAS), số tiền USD bảo lãnh thanh toán, thời hạn hiệu lực và ngày giao hàng muộn nhất.</summary>
public sealed class LetterOfCredit
{
    public long Id { get; set; }
    public Guid OrgId { get; set; }
    public string LCNo { get; set; } = ""; // Số hiệu L/C (PK / Mã LC, format LC{yyMM}{seq:D4})
    public string ContractNo { get; set; } = ""; // Số hợp đồng ngoại thương liên kết (CT_ContractOversea.ContractNo)
    public string BankCode { get; set; } = "VCB"; // Mã ngân hàng phát hành (VCB, BIDV, CTG, TCB, MB...)
    public string BankName { get; set; } = "Ngân hàng TMCP Ngoại thương Việt Nam (Vietcombank)"; // Tên ngân hàng phát hành
    public string? BankBranch { get; set; } = "Sở Giao Dịch Hà Nội"; // Chi nhánh ngân hàng phát hành
    public LCType LCType { get; set; } = LCType.AtSight; // Loại hình thư tín dụng
    public decimal Amount { get; set; } // Giá trị L/C (USD)
    public string Currency { get; set; } = "USD"; // Đồng tiền L/C (USD)
    public DateTime IssueDate { get; set; } = DateTime.Today; // Ngày phát hành L/C
    public DateTime ExpireDate { get; set; } = DateTime.Today.AddDays(90); // Ngày hết hạn hiệu lực L/C
    public DateTime? LatestShipmentDate { get; set; } // Ngày giao hàng lên tàu muộn nhất (Latest Shipment Date)
    public LCStatus Status { get; set; } = LCStatus.Active; // Trạng thái L/C
    public string? Remark { get; set; } // Ghi chú thư tín dụng
    public string? CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public string? UpdatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

/// <summary>Trạng thái Bảng kê Quyết toán Chi phí PDI Xe Ô tô HTV - TCMS (DMS.Sales Pmt_PaymentPDI: Pending = 0 ['P' - Chờ duyệt cấp 1], Approved1 = 1 ['A1' - Trưởng phòng/Quản lý PDI duyệt cấp 1], Approved2 = 2 ['A2' - Lãnh đạo HTC/HTV duyệt cấp 2], Finished = 3 ['F' - Ký số 2 bên hoàn tất/quyết toán xong], Rejected = 4 ['R' - Từ chối duyệt], Cancelled = 5 ['C' - Đã hủy]).</summary>
public enum PaymentPDIStatus
{
    Pending = 0,
    Approved1 = 1,
    Approved2 = 2,
    Finished = 3,
    Rejected = 4,
    Cancelled = 5
}

/// <summary>Trạng thái ký số điện tử biên bản quyết toán PDI & Rửa xe (DMS.Sales HTVSignStatus / TCMSSignStatus: ChuaKy = 0 ['P'], DaKy = 1 ['A'/'S']).</summary>
public enum PaymentPDISignStatus
{
    ChuaKy = 0,
    DaKy = 1
}

/// <summary>Bảng kê Quyết toán Chi phí PDI & Rửa xe Ô tô HTV - TCMS (DMS.Sales Pmt_PaymentPDI / PmtPaymentPDIController / PaymentPDI.cs / PmtPaymentPDI.txt / ChiPhiPDI.HTV.TCMS.xlsx): Định kỳ hàng tháng hoặc theo kỳ liên tiếp, Nhà phân phối ô tô HTV và Công ty Cổ phần Vận hành Kho vận TCMS lập bảng kê đối soát thanh toán chi phí kiểm tra trước khi giao xe (PDI - Pre-Delivery Inspection) cho xe nội địa CKD (PDINAmount) và xe nhập khẩu CBU (PDIXAmount), chi phí rửa xe (RXAmount); kiểm tra ràng buộc kỳ thanh toán liên tiếp không ngắt quãng (StartDTime = LastEndDTime + 1 ngày); các bảng kê trước phải ở trạng thái đã kết thúc (F, C, R); quy trình phê duyệt đối soát 2 cấp 2 bên (NPP duyệt cấp 1 Approve1HQ, Lãnh đạo NPP duyệt cấp 2 Approve2HQ, TCMS ký số điện tử TCMSESignHQ 2 file PDF PDI & RX, HTV ký số điện tử HTVESignHQ 2 file và tự động hoàn tất Finished, đính kèm hóa đơn GTGT do TCMS xuất UploadFileInvoicePDIHQ và UploadFileInvoiceRXHQ).</summary>
public sealed class PaymentPDIOrder
{
    public long Id { get; set; }
    public Guid OrgId { get; set; }
    public string PmtPDINo { get; set; } = ""; // Mã bảng kê ({yyMM}PDI{seq:D5}, vd: 2603PDI00001)
    public string PmtMonth { get; set; } = ""; // Tháng thanh toán (yyyy-MM-01 hoặc yyyy-MM, vd: 2026-03-01)
    public DateTime PmtPeriodStartDTime { get; set; } // Ngày bắt đầu kỳ thanh toán
    public DateTime PmtPeriodEndDTime { get; set; } // Ngày kết thúc kỳ thanh toán
    public decimal VAT { get; set; } = 10m; // Thuế GTGT % (mặc định 10%)
    public int TotalCars { get; set; } // Tổng số lượng xe tính PDI & RX

    // Các khoản mục chi phí:
    public decimal PDINTotalAmount { get; set; } // Tổng tiền PDI xe nội địa CKD
    public decimal PDIXTotalAmount { get; set; } // Tổng tiền PDI xe nhập khẩu CBU
    public decimal PDITotalAmount { get; set; } // Tổng tiền PDI = PDINTotalAmount + PDIXTotalAmount
    public decimal RXTotalAmount { get; set; } // Tổng tiền rửa xe
    public decimal TotalAmount { get; set; } // Tổng chi phí trước VAT = PDITotalAmount + RXTotalAmount
    public decimal AmountVAT { get; set; } // Tiền thuế VAT = TotalAmount * VAT / 100
    public decimal TotalAmountAfterVAT { get; set; } // Tổng chi phí thanh toán sau VAT = TotalAmount + AmountVAT
    public PaymentPDIStatus Status { get; set; } = PaymentPDIStatus.Pending;

    // Ký số phía Nhà phân phối HTV:
    public PaymentPDISignStatus HTVSignStatus { get; set; } = PaymentPDISignStatus.ChuaKy;
    public DateTime? HTVSignDTime { get; set; }
    public string? HTVSignBy { get; set; }

    // Ký số phía Đơn vị dịch vụ kiểm định kho bãi TCMS:
    public PaymentPDISignStatus TCMSSignStatus { get; set; } = PaymentPDISignStatus.ChuaKy;
    public DateTime? TCMSSignDTime { get; set; }
    public string? TCMSSignBy { get; set; }

    // Hồ sơ file ký số điện tử (2 file: PDI và Rửa xe RX):
    public string? PDIFilePath { get; set; } // Đường dẫn lưu trữ file PDF ký số PDI
    public string? PDIFileUrl { get; set; }
    public string? RXFilePath { get; set; } // Đường dẫn lưu trữ file PDF ký số RX
    public string? RXFileUrl { get; set; }

    // Hóa đơn GTGT chi phí do TCMS xuất (2 hóa đơn: PDI và RX):
    public string? PDIFileInvPath { get; set; } // File hóa đơn VAT dịch vụ PDI
    public string? PDIFileInvUrl { get; set; }
    public DateTime? PDIFileInvUpDTime { get; set; }
    public string? PDIFileInvUpBy { get; set; }
    public string? PDIInvoiceNo { get; set; }
    public DateTime? PDIInvoiceDate { get; set; }

    public string? RXFileInvPath { get; set; } // File hóa đơn VAT dịch vụ Rửa xe
    public string? RXFileInvUrl { get; set; }
    public DateTime? RXFileInvUpDTime { get; set; }
    public string? RXFileInvUpBy { get; set; }
    public string? RXInvoiceNo { get; set; }
    public DateTime? RXInvoiceDate { get; set; }

    // Ghi chú & lý do:
    public string? Remark { get; set; }
    public string? RejectReason { get; set; }
    public string? CancelReason { get; set; }

    // Audit logs & phê duyệt:
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public string? CreatedBy { get; set; }
    public DateTime? Approve1At { get; set; }
    public string? Approve1By { get; set; }
    public DateTime? Approve2At { get; set; }
    public string? Approve2By { get; set; }
    public DateTime? RejectedAt { get; set; }
    public string? RejectedBy { get; set; }
    public DateTime? CancelledAt { get; set; }
    public string? CancelledBy { get; set; }
    public DateTime LogLUDateTime { get; set; } = DateTime.Now;
    public string? LogLUBy { get; set; }

    public List<PaymentPDIDetail> Details { get; set; } = new();
}

/// <summary>Chi tiết dòng xe ô tô tính phí PDI & rửa xe trong kỳ (DMS.Sales Pmt_PaymentPDIDetail): theo dõi từng số khung VIN 17 ký tự, mã định danh CarId, model, spec, màu xe, phân loại xe nội địa CKD ('N') hay nhập khẩu CBU ('X'), số tham chiếu vào kho RefNoInit, kho ban đầu StorageCodeInit, ngày vào kho StorageDateInit, ngày hóa đơn nhà máy InvoiceFactoryDate, kho xuất/ngày xuất/lệnh giao xe nếu có, chi phí PDI nội địa PDINAmount, chi phí PDI nhập khẩu PDIXAmount, tổng chi phí PDI dòng xe PDIAmount, chi phí rửa xe RXAmount và tổng tiền dòng xe TotalAmount.</summary>
public sealed class PaymentPDIDetail
{
    public long Id { get; set; }
    public long PaymentPDIId { get; set; }
    public string PmtPDINo { get; set; } = "";
    public string Vin { get; set; } = ""; // Số khung xe chuẩn 17 ký tự (VIN, bắt buộc)
    public string? CarId { get; set; }
    public string ModelCode { get; set; } = "";
    public string ModelName { get; set; } = "";
    public string? SpecCode { get; set; }
    public string? ColorCode { get; set; }
    public string PDIType { get; set; } = "N"; // "N" = CKD Nội địa, "X" = CBU Nhập khẩu

    // Thông tin kho và chứng từ gốc:
    public string? RefNoInit { get; set; } // Số phiếu kiểm tra / nhập kho ban đầu
    public string? StorageCodeInit { get; set; } // Kho lưu bãi ban đầu
    public DateTime? StorageDateInit { get; set; } // Ngày nhập kho
    public string? InvoiceFactoryDate { get; set; } // Ngày hóa đơn nhà máy

    public string? RefNoCost { get; set; } // Số tham chiếu chi phí
    public string? StorageCodeCost { get; set; }

    public string? RefNoOut { get; set; } // Số phiếu xuất kho nếu đã xuất
    public string? StorageCodeOut { get; set; }
    public DateTime? StorageDateOut { get; set; }
    public string? DeliveryOrderNo { get; set; } // Lệnh giao xe liên kết

    public string? DealerCode { get; set; } // Đại lý nhận xe
    public string? DealerName { get; set; }

    // Chi phí chi tiết theo dòng xe:
    public decimal PDINAmount { get; set; } // Tiền PDI nội địa CKD
    public decimal PDIXAmount { get; set; } // Tiền PDI xuất / nhập khẩu CBU
    public decimal PDIAmount { get; set; } // Tổng tiền PDI dòng xe = PDINAmount + PDIXAmount
    public decimal RXAmount { get; set; } // Tiền rửa xe
    public decimal TotalAmount { get; set; } // Tổng tiền dòng xe = PDIAmount + RXAmount

    public string? Remark { get; set; }
    public DateTime LogLUDateTime { get; set; } = DateTime.Now;
    public string? LogLUBy { get; set; }
}

/// <summary>Trạng thái Bảng kê Quyết toán Chi phí Vận chuyển & Phạt Chậm Vận Tải Xe Ô tô HTV - TCMS / Đơn vị Vận tải (DMS.Sales Pmt_TransportIns: Pending = 0 ['P' - Chờ duyệt cấp 1], Approved1 = 1 ['A1' - Bộ phận Logistics duyệt cấp 1], Approved2 = 2 ['A2' - Lãnh đạo HTC/HTV & TCKT duyệt cấp 2], Finished = 3 ['F' - Ký số 2 bên hoàn tất/quyết toán xong], Cancelled = 4 ['C' - Đã hủy]).</summary>
public enum TransportInsStatus
{
    Pending = 0,
    Approved1 = 1,
    Approved2 = 2,
    Finished = 3,
    Cancelled = 4
}

/// <summary>Trạng thái ký số điện tử biên bản quyết toán chi phí vận chuyển & bảo hiểm (DMS.Sales HTVSignStatus / TCMSSignStatus: ChuaKy = 0 ['P'], DaKy = 1 ['A']).</summary>
public enum TransportInsSignStatus
{
    ChuaKy = 0,
    DaKy = 1
}

/// <summary>Trạng thái dòng xe ô tô trong bảng kê thanh toán vận tải (DMS.Sales TrasportInsDtlStatus: Pending = 0 ['P' - Chờ duyệt], Approved = 1 ['A' - Chấp thuận], Rejected = 2 ['R' - Bác bỏ/không thanh toán]).</summary>
public enum TransportInsDetailStatus
{
    Pending = 0,
    Approved = 1,
    Rejected = 2
}

/// <summary>Bảng kê Quyết toán Chi phí Vận chuyển & Phạt Chậm Vận Tải Xe Ô tô HTV - TCMS / Đơn vị Vận tải (DMS.Sales Pmt_TransportIns / PmtTransportInsController / Payment.cs / PmtTransportIns.txt): Định kỳ hàng tháng, Nhà phân phối xe ô tô HTV/HTC và Công ty Cổ phần Vận hành Kho vận TCMS / Đơn vị vận chuyển lập bảng kê đối soát chi phí vận chuyển ô tô từ nhà máy/kho đến đại lý, đối soát ngày xuất kho (InvStartDate), hạn mức số ngày vận chuyển quy định theo tuyến đường (ExpectedDays), ngày đến theo định mức (ExpectedDlvEndDate) so với ngày xe đến thực tế bàn giao cho đại lý (InvEndDate); tự động tính số ngày giao chậm (DelayDate) và số tiền phạt chậm (DelayPenaty); tính phí bảo hiểm hàng hóa vận chuyển trên đường (InsuranceCost = PriceCar * InsurancePercent); tính tổng phí thanh toán từng xe (TotalPrice = InsuranceCost + TransportCost - DelayPenaty) và tổng hợp thuế GTGT (TotalAmount, VAT, UnitPriceVAT, TotalAmountVAT); quy trình phê duyệt đối soát 2 cấp (Logistics duyệt cấp 1 Approve1HQ, Lãnh đạo HTV/TCKT duyệt cấp 2 Approve2HQ), quy trình ký số điện tử 2 bên (TCMS ký số TCMSSignHQ, HTV ký số HTVESignHQ hoàn tất quyết toán Finished), in phiếu thanh toán PDF và xuất dữ liệu đối soát Master + Detail.</summary>
public sealed class TransportInsOrder
{
    public long Id { get; set; }
    public Guid OrgId { get; set; }
    public string TransportInsNo { get; set; } = ""; // Số phiếu bảng kê thanh toán ({yyMM}TI{seq:D4}, vd: 2603TI0001)
    public string PmtMonth { get; set; } = ""; // Tháng thanh toán (yyyy-MM-01, vd: 2026-03-01)

    // Tổng hợp tài chính:
    public decimal TotalAmount { get; set; } // Tổng tiền trước VAT = TotalAmountVAT * 100 / (100 + VAT)
    public decimal VAT { get; set; } = 10.0m; // Thuế suất VAT (%), mặc định 10%
    public decimal UnitPriceVAT { get; set; } // Tiền thuế VAT = TotalAmountVAT - TotalAmount
    public decimal TotalAmountVAT { get; set; } // Tổng tiền sau VAT = Σ(TotalPrice dòng xe)

    // Thống kê số lượng & chi phí thành phần:
    public int TotalCars { get; set; } // Tổng số xe trong bảng kê
    public decimal TotalTransportCost { get; set; } // Tổng cước vận tải
    public decimal TotalDelayPenalty { get; set; } // Tổng tiền phạt chậm giao xe
    public decimal TotalInsuranceCost { get; set; } // Tổng phí bảo hiểm vận tải

    // Trạng thái phê duyệt:
    public TransportInsStatus Status { get; set; } = TransportInsStatus.Pending;

    // Ký số phía Nhà phân phối HTV:
    public TransportInsSignStatus HTVSignStatus { get; set; } = TransportInsSignStatus.ChuaKy;
    public DateTime? HTVSignDTime { get; set; }
    public string? HTVSignBy { get; set; }

    // Ký số phía Đơn vị vận hành kho vận / Vận tải TCMS:
    public TransportInsSignStatus TCMSSignStatus { get; set; } = TransportInsSignStatus.ChuaKy;
    public DateTime? TCMSSignDTime { get; set; }
    public string? TCMSSignBy { get; set; }

    // File hợp đồng điện tử / biên bản đã ký số:
    public string? FilePath { get; set; }

    // Lịch sử duyệt 2 cấp:
    public DateTime? App1DTime { get; set; }
    public string? App1By { get; set; }
    public DateTime? App2DTime { get; set; }
    public string? App2By { get; set; }

    // Hủy bảng kê:
    public DateTime? CancelDTime { get; set; }
    public string? CancelBy { get; set; }
    public string? CancelReason { get; set; }

    public string? Remark { get; set; }
    public DateTime CreateDateTime { get; set; } = DateTime.Now;
    public string? CreateBy { get; set; }
    public DateTime LogLUDateTime { get; set; } = DateTime.Now;
    public string? LogLUBy { get; set; }

    public List<TransportInsOrderDetail> Details { get; set; } = new();
}

/// <summary>Chi tiết dòng xe ô tô tính phí vận chuyển & phạt chậm trong bảng kê (DMS.Sales Pmt_TransportInsDetail): theo dõi từng số khung VIN 17 ký tự, mã xe CarId, số biên bản giao nhận xe BBGN/BBBG DlvMnNo, loại lệnh vận chuyển TranspReqType, tuyến đường vận tải (nơi đi - nơi đến), ngày xuất kho InvStartDate, hạn mức số ngày vận chuyển theo định mức ExpectedDays, ngày đến theo định mức ExpectedDlvEndDate, ngày xe đến thực tế InvEndDate, số ngày giao chậm DelayDate, cước vận chuyển TransportCost, tiền phạt chậm DelayPenaty, giá trị xe PriceCar, tỷ lệ bảo hiểm InsurancePercent, số HĐ bảo hiểm InsuranceContractNo, chi phí bảo hiểm InsuranceCost và tổng thanh toán dòng xe TotalPrice = InsuranceCost + TransportCost - DelayPenaty.</summary>
public sealed class TransportInsOrderDetail
{
    public long Id { get; set; }
    public long TransportInsOrderId { get; set; }
    public string TransportInsNo { get; set; } = "";
    public string Vin { get; set; } = ""; // Số khung xe chuẩn 17 ký tự (VIN, bắt buộc)
    public string? CarId { get; set; } // Mã định danh xe trong hệ thống
    public string? DlvMnNo { get; set; } // Số lệnh biên bản giao nhận xe BBGN / Sto_DlvMinutes
    public string? TranspReqType { get; set; } = "CARTRANSPORT"; // Loại vận chuyển: CARTRANSPORT, CARRETRIEVE, STORAGEREARRANGE, STORAGEREARRCB

    // Thông tin xe:
    public string? ModelCode { get; set; }
    public string? ModelName { get; set; }
    public string? SpecCode { get; set; }
    public string? ColorName { get; set; }
    public string? EngineNo { get; set; }

    // Tuyến đường vận chuyển:
    public string? FStorageCode { get; set; } // Mã kho/nơi đi
    public string? FProvinceName { get; set; } // Tỉnh/thành nơi đi
    public string? TStorageCode { get; set; } // Mã nơi đến (đại lý hoặc kho đến)
    public string? TProvinceName { get; set; } // Tỉnh/thành nơi đến

    // Thời gian & định mức:
    public DateTime? InvStartDate { get; set; } // Ngày xuất kho / lăn bánh
    public int ExpectedDays { get; set; } // Định mức số ngày vận chuyển theo quy chuẩn tuyến đường
    public DateTime? ExpectedDlvEndDate { get; set; } // Ngày đến theo định mức (= InvStartDate + ExpectedDays)
    public DateTime? InvEndDate { get; set; } // Ngày đến thực tế bàn giao đại lý
    public int DelayDate { get; set; } // Số ngày chậm bàn giao xe: max(0, InvEndDate - ExpectedDlvEndDate)

    // Cước phí & Phạt chậm & Bảo hiểm:
    public decimal TransportCost { get; set; } // Phí vận chuyển (>= 0)
    public decimal DelayPenaty { get; set; } // Tiền phạt chậm do trễ hạn (>= 0)
    public decimal PriceCar { get; set; } // Giá trị xe tính phí bảo hiểm
    public decimal InsurancePercent { get; set; } // Tỷ lệ phí bảo hiểm vận chuyển (%)
    public string? InsuranceContractNo { get; set; } // Số hợp đồng bảo hiểm vận tải
    public decimal InsuranceCost { get; set; } // Tiền phí bảo hiểm = PriceCar * InsurancePercent
    public decimal TotalPrice { get; set; } // Tổng tiền dòng xe = InsuranceCost + TransportCost - DelayPenaty

    // Trạng thái dòng & ghi chú:
    public TransportInsDetailStatus Status { get; set; } = TransportInsDetailStatus.Pending;
    public string? FProvinceRemark { get; set; } // Lý do sửa nơi đến/đi
    public string? StandardRemark { get; set; } // Lý do sửa ngày định mức
    public string? Remark { get; set; } // Ghi chú

    public DateTime LogLUDateTime { get; set; } = DateTime.Now;
    public string? LogLUBy { get; set; }
}

/// <summary>Trạng thái Bảng kê Quyết toán Chi phí Thiết bị AVN Xe Ô tô HTV - TCMS (DMS.Sales Pmt_PaymentAVN: Draft = 'P' [Bản nháp], TCMSApproved = 'A1' [TCMS duyệt cấp 1], HTVApproved = 'A2' [HTV duyệt cấp 2], Signed = 'F' [Ký số 2 bên hoàn tất], Settled = 'S' [Đã quyết toán/giải ngân], Rejected = 'R' [Từ chối duyệt], Cancelled = 'C' [Đã hủy]).</summary>
public enum PaymentAVNStatus
{
    Draft = 0,
    TCMSApproved = 1,
    HTVApproved = 2,
    Signed = 3,
    Settled = 4,
    Rejected = 5,
    Cancelled = 6
}

/// <summary>Trạng thái ký số điện tử biên bản quyết toán AVN (DMS.Sales TCMSSignStatus / HTVSignStatus: ChuaKy = 0 ['P'], DaKy = 1 ['A']).</summary>
public enum PaymentAVNSignStatus
{
    ChuaKy = 0,
    DaKy = 1
}

/// <summary>Trạng thái dòng xe tính phí thiết bị AVN (DMS.Sales Pmt_PaymentAVNDetail: Pending = 'P', Approved = 'A', Adjusted = 'U', Cancelled = 'C').</summary>
public enum PaymentAVNDetailStatus
{
    Pending = 0,
    Approved = 1,
    Adjusted = 2,
    Cancelled = 3
}

/// <summary>Bảng kê Quyết toán Chi phí Thiết bị AVN (Audio-Visual Navigation - màn hình giải trí & định vị dẫn đường) trên Xe Ô tô HTV - TCMS (DMS.Sales Pmt_PaymentAVN / PmtPaymentAVNController / Payment.cs / Pmt_PaymentAVN.txt): Định kỳ hàng tháng, Nhà phân phối ô tô HTV và Công ty Cổ phần Vận hành Kho vận TCMS lập bảng kê đối soát thanh toán chi phí thiết bị AVN (màn hình AVN + thẻ bản đồ định vị) đã lắp đặt trên từng xe ô tô theo lô VIN; tra cứu đơn giá thiết bị AVN theo danh mục Mst_UnitPriceAVN; quy trình phê duyệt đối soát 2 cấp 2 bên (TCMS duyệt cấp 1 TCMSApproveHQ, HTV duyệt cấp 2 HTVApproveHQ, TCMS ký số điện tử TCMSSignHQ, HTV ký số điện tử HTVSignHQ hoàn tất quyết toán Signed), quyết toán/giải ngân SettleHQ, từ chối duyệt RejectHQ, hủy bảng kê CancelHQ, xóa nháp DeleteDraft, thêm/gỡ xe theo VIN và in bảng kê quyết toán chi phí AVN.</summary>
public sealed class PaymentAVNOrder
{
    public long Id { get; set; }
    public Guid OrgId { get; set; }
    public string PaymentAVNNo { get; set; } = ""; // Mã bảng kê ({yyMM}AVN{seq:D4}, vd: 2603AVN0001)
    public string PmtMonth { get; set; } = ""; // Tháng quyết toán (yyyy-MM, vd: 2026-03)
    public string SupplierCode { get; set; } = "MOBIS-VN"; // Mã đơn vị cung cấp / lắp ráp thiết bị AVN
    public string SupplierName { get; set; } = "Công ty TNHH Mobis Auto Parts Việt Nam"; // Tên nhà cung cấp / đối tác AVN
    public int TotalCars { get; set; } // Tổng số lượng xe gắn thiết bị AVN trong kỳ
    public decimal AmountTotal { get; set; } // Tổng chi phí AVN trước thuế VAT (VNĐ) = Σ UnitPriceAVN
    public decimal VatRate { get; set; } = 10m; // Thuế suất VAT (%)
    public decimal UnitPriceVAT { get; set; } // Tiền thuế GTGT (VNĐ) = AmountTotal * VatRate / 100
    public decimal TotalAmountVAT { get; set; } // Tổng số tiền thanh toán sau VAT (VNĐ) = AmountTotal + UnitPriceVAT
    public PaymentAVNStatus Status { get; set; } = PaymentAVNStatus.Draft;

    // Ký số phía Đơn vị dịch vụ thiết bị AVN (TCMS):
    public PaymentAVNSignStatus TCMSSignStatus { get; set; } = PaymentAVNSignStatus.ChuaKy;
    public DateTime? TCMSSignDate { get; set; }
    public string? TCMSSignBy { get; set; }

    // Ký số phía Nhà phân phối HTV:
    public PaymentAVNSignStatus HTVSignStatus { get; set; } = PaymentAVNSignStatus.ChuaKy;
    public DateTime? HTVSignDate { get; set; }
    public string? HTVSignBy { get; set; }

    public string? FilePath { get; set; } // Đường dẫn / URL file quyết toán PDF ký số 2 bên
    public string? BankTxnRef { get; set; } // Mã giao dịch / ủy nhiệm chi giải ngân quyết toán
    public string? Remark { get; set; } // Ghi chú bảng kê
    public string? RejectReason { get; set; } // Lý do từ chối duyệt
    public string? CancelReason { get; set; } // Lý do hủy bảng kê

    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public string? CreatedBy { get; set; }
    public DateTime? Approved1At { get; set; }
    public string? Approved1By { get; set; }
    public DateTime? Approved2At { get; set; }
    public string? Approved2By { get; set; }
    public DateTime? SettledAt { get; set; }
    public string? SettledBy { get; set; }
    public DateTime? RejectedAt { get; set; }
    public string? RejectedBy { get; set; }
    public DateTime? CancelledAt { get; set; }
    public string? CancelledBy { get; set; }
    public DateTime? LUDateTime { get; set; }
    public string? LUBy { get; set; }

    public List<PaymentAVNDetail> Details { get; set; } = new();
}

/// <summary>Chi tiết dòng xe ô tô tính phí thiết bị AVN trong kỳ (DMS.Sales Pmt_PaymentAVNDetail): theo dõi từng số khung VIN 17 ký tự, số máy EngineNo, model/spec/màu xe, mã chủng loại thiết bị AVN (AVNCode), số serial thiết bị dập trên vỏ/màn hình (SerialNo), đơn giá thiết bị AVN (UnitPriceAVN) tra theo danh mục Mst_UnitPriceAVN, ngày lắp ráp/kích hoạt hệ thống AVN (AVNDate) và ngày xe hoàn thiện nhập kho lưu bãi nhà máy (InStorageDate).</summary>
public sealed class PaymentAVNDetail
{
    public long Id { get; set; }
    public long PaymentAVNId { get; set; }
    public string PaymentAVNNo { get; set; } = "";
    public string Vin { get; set; } = ""; // Số khung xe chuẩn 17 ký tự (VIN)
    public string? CarId { get; set; } // Mã định danh xe hệ thống
    public string? EngineNo { get; set; } // Số máy xe
    public string ModelCode { get; set; } = ""; // Mã model (SANTAFE, TUCSON, CRETA, ACCENT, ELANTRA...)
    public string ModelName { get; set; } = ""; // Tên thương mại dòng xe
    public string? SpecCode { get; set; } // Mã cấu hình xe
    public string? ColorCode { get; set; } // Mã màu xe
    public string? ColorName { get; set; } // Tên màu xe
    public string AVNCode { get; set; } = ""; // Mã chủng loại thiết bị AVN (AVN-SANTAFE-GEN5, AVN-TUCSON-1025...)
    public string SerialNo { get; set; } = ""; // Số serial thiết bị AVN dập trên vỏ/màn hình
    public decimal UnitPriceAVN { get; set; } // Đơn giá thiết bị AVN (VNĐ) tra theo danh mục Mst_UnitPriceAVN
    public DateTime? AVNDate { get; set; } // Ngày lắp ráp / kích hoạt hệ thống AVN
    public DateTime? InStorageDate { get; set; } // Ngày xe hoàn thiện nhập kho lưu bãi nhà máy
    public PaymentAVNDetailStatus Status { get; set; } = PaymentAVNDetailStatus.Pending; // Trạng thái dòng
    public string? Remark { get; set; } // Ghi chú chi tiết dòng xe
}

/// <summary>Bảng đơn giá định mức thiết bị AVN theo dòng xe (DMS.Sales Mst_UnitPriceAVN / MstUnitPriceAVNController / Mst_UnitPriceAVN.txt): quản lý đơn giá thiết bị màn hình giải trí & định vị dẫn đường AVN áp dụng theo kỳ hiệu lực (EffStartDate -> EffEndDate).</summary>
public sealed class AvnUnitPriceMaster
{
    public long Id { get; set; }
    public string AvnCode { get; set; } = ""; // Mã chủng loại thiết bị AVN (vd: AVN-STF-GEN5-PREM)
    public string AvnName { get; set; } = ""; // Tên thiết bị AVN
    public string ModelCode { get; set; } = ""; // Dòng xe áp dụng (SANTAFE, TUCSON, CRETA...)
    public decimal UnitPrice { get; set; } // Đơn giá thiết bị AVN (VNĐ)
    public DateTime EffStartDate { get; set; } = new DateTime(2026, 1, 1); // Ngày bắt đầu áp dụng
    public DateTime? EffEndDate { get; set; } // Ngày kết thúc áp dụng (null nếu đang hiệu lực)
    public bool IsActive { get; set; } = true;
    public string? Remark { get; set; }
}


/// <summary>Giới tính khách hàng đại lý (DMS.Sales DLS_DealerCustomer.Gender: Male = 'M' [Nam], Female = 'F' [Nữ], Other = 'O' [Khác]).</summary>
public enum DealerCustomerGender
{
    Male = 0,
    Female = 1,
    Other = 2
}

/// <summary>Loại khách hàng đại lý (DMS.Sales DLS_DealerCustomer.CustomerType: Personal = 'Personal' [Cá nhân], Business = 'Business' [Doanh nghiệp]).</summary>
public enum DealerCustomerType
{
    Personal = 0,
    Business = 1
}

/// <summary>Khách hàng Đại lý (DMS.Sales DLS_DealerCustomer / DLSDealerCustomerController / DealerSales.cs / DLS_DealerCustomer.txt): Đại lý quản lý hồ sơ khách hàng mua xe (cá nhân/doanh nghiệp) gồm thông tin định danh (họ tên, giới tính, ngày sinh, giấy tờ tùy thân), liên hệ (điện thoại, email, địa chỉ, tỉnh/xã), nguồn khách hàng (CustomerBaseCode), loại khách hàng và thông tin doanh nghiệp (người đại diện, chức vụ, MST, tài khoản ngân hàng). Mã khách hàng sinh tự động theo quy chuẩn {yyMM}CTM{seq:D5} (SequenceType = "CTM").</summary>
public sealed class DealerCustomer
{
    public long Id { get; set; }
    public Guid OrgId { get; set; }
    public string CustomerCode { get; set; } = ""; // Mã khách hàng ({yyMM}CTM{seq:D5}, vd: 2606CTM06125)
    public string DealerCode { get; set; } = ""; // Mã đại lý tạo (chỉ dùng khi Create, không cho đổi khi Update)
    public string? DealerName { get; set; } // Tên đại lý tạo

    // Thông tin định danh:
    public string FullName { get; set; } = ""; // Họ tên / Tên khách hàng (bắt buộc)
    public string? FullNameEN { get; set; } // Tên khách hàng tiếng Anh
    public DealerCustomerGender Gender { get; set; } = DealerCustomerGender.Male; // Giới tính (bắt buộc)
    public DateTime? DateOfBirth { get; set; } // Ngày sinh nhật / Ngày thành lập
    public string? IDCardType { get; set; } // Loại giấy tờ tùy thân (CMND, CCCD, Hộ chiếu...)
    public string? IDCardNo { get; set; } // Số giấy tờ tùy thân (chỉ chứa [a-zA-Z0-9])

    // Liên hệ:
    public string PhoneNo { get; set; } = ""; // Số điện thoại (bắt buộc)
    public string? Email { get; set; } // Email (không bắt buộc, nếu có phải đúng format)
    public string? Address { get; set; } // Địa chỉ (bắt buộc)
    public string? ProvinceCode { get; set; } // Mã tỉnh/thành (bắt buộc)
    public string? ProvinceName { get; set; } // Tên tỉnh/thành
    public string? DistrictCode { get; set; } // Mã quận/huyện - xã/phường (bắt buộc)
    public string? DistrictName { get; set; } // Tên quận/huyện - xã/phường

    // Phân loại & nguồn khách hàng:
    public string CustomerBaseCode { get; set; } = ""; // Mã nguồn khách hàng (bắt buộc, tham chiếu Mst_CustomerBase)
    public string? CustomerBaseName { get; set; } // Tên nguồn khách hàng
    public DealerCustomerType CustomerType { get; set; } = DealerCustomerType.Personal; // Loại khách hàng (Cá nhân/Doanh nghiệp)

    // Thông tin doanh nghiệp:
    public string? TaxCode { get; set; } // Mã số thuế
    public string? RepresentName { get; set; } // Người đại diện (dành cho KH doanh nghiệp)
    public string? Position { get; set; } // Chức vụ
    public string? CusAccountBank { get; set; } // Số tài khoản ngân hàng

    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public string? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }
}

/// <summary>Loại thao tác cập nhật giá xe ô tô (DMS.Sales CarUpdPriceController / Car_Car_Update01_HQ): UpdatePrice = cập nhật giá bán buôn thực tế, UpdateMapVINRanking = cập nhật thứ tự ưu tiên Map VIN, LockChangeVIN = khóa đổi số khung VIN.</summary>
public enum CarPriceUpdateAction { UpdatePrice = 0, UpdateMapVINRanking = 1, LockChangeVIN = 2 }

/// <summary>Nhật ký cập nhật giá xe ô tô (DMS.Sales CarUpdPriceController / Car_Car_Update01_HQ / MapVIN.cs): lưu vết mỗi lần NPP cập nhật giá bán buôn thực tế (UnitPriceActual) của xe, thứ tự ưu tiên Map VIN (MapVINRanking) hoặc khóa đổi số khung VIN (FlagAllowChangeVIN), kèm trạng thái thanh toán trước/sau khi cập nhật giá.</summary>
public sealed class CarPriceUpdateLog
{
    public long Id { get; set; }
    public Guid OrgId { get; set; }
    public string CarId { get; set; } = ""; // Mã định danh xe hệ thống (Car_Car)
    public string? Vin { get; set; } // Số khung VIN tại thời điểm cập nhật
    public string ModelCode { get; set; } = ""; // Mã model xe
    public string? DealerCode { get; set; } // Mã đại lý quản lý xe
    public CarPriceUpdateAction Action { get; set; } = CarPriceUpdateAction.UpdatePrice; // Loại thao tác
    public decimal? OldUnitPriceActual { get; set; } // Giá bán buôn thực tế trước khi cập nhật
    public decimal? NewUnitPriceActual { get; set; } // Giá bán buôn thực tế sau khi cập nhật
    public string? OldPaymentStatus { get; set; } // Trạng thái thanh toán trước khi cập nhật (P/F)
    public string? NewPaymentStatus { get; set; } // Trạng thái thanh toán sau khi cập nhật (P/F)
    public string? Remark { get; set; } // Ghi chú thao tác
    public string? PerformedBy { get; set; } // Người thực hiện cập nhật
    public DateTime PerformedAt { get; set; } = DateTime.Now; // Thời điểm thực hiện
}

/// <summary>Định mức bảo hành xe ô tô theo dòng xe (DMS.Sales Mst_WarrantyExpires / Master.cs / Mst_WarrantyExpires_Get|Create|Update|Delete|Import): danh mục quy định thời hạn bảo hành (số tháng) và số km giới hạn bảo hành áp dụng cho từng model xe. Khi Đại lý lập giao dịch bán lẻ (DLS_Deal), hệ thống tra cứu định mức này theo ModelCode để tự động điền WarrantyExpires / WarrantyKM và tính ngày hết hạn bảo hành (WarrantyExpiresDate = ngày bàn giao + số tháng).</summary>
public sealed class WarrantyExpiresMaster
{
    public long Id { get; set; }
    public Guid OrgId { get; set; }
    public string ModelCode { get; set; } = ""; // Mã dòng xe (khóa chính nghiệp vụ, tham chiếu Mst_CarModel.ModelCode)
    public string? ModelName { get; set; } // Tên dòng xe (tra cứu từ Mst_CarModel)
    public decimal WarrantyExpires { get; set; } // Thời hạn bảo hành theo số tháng (>= 0)
    public decimal WarrantyKM { get; set; } // Số km giới hạn bảo hành (>= 0)
    public string FlagActive { get; set; } = "1"; // Cờ hiệu lực (1 = đang áp dụng, 0 = ngừng áp dụng)
    public string? Remark { get; set; } // Ghi chú
    public DateTime LogLUDateTime { get; set; } = DateTime.Now; // Thời điểm cập nhật gần nhất
    public string? LogLUBy { get; set; } // Người cập nhật gần nhất
}

/// <summary>Phân vùng HTV (DMS.Sales Mst_Zone / DealerZone.cs / FrmMst_Zone): danh mục phân vùng kinh doanh của HTV (vd: MB = Miền Bắc, MN = Miền Nam). ZoneCode là khóa nghiệp vụ duy nhất; WinForm chỉ cho sửa cờ hiệu lực FlagActive (ZoneCode/ZoneName readonly sau khi tạo).</summary>
public sealed class ZoneMaster
{
    public long Id { get; set; }
    public Guid OrgId { get; set; }
    public string ZoneCode { get; set; } = ""; // Mã phân vùng (khóa nghiệp vụ duy nhất, vd: MB, MN)
    public string? ZoneName { get; set; } // Tên phân vùng (vd: Miền Bắc, Miền Nam)
    public string? Remark { get; set; } // Ghi chú
    public string FlagActive { get; set; } = "1"; // Cờ hiệu lực (1 = đang áp dụng, 0 = ngừng áp dụng)
    public DateTime LogLUDTime { get; set; } = DateTime.Now; // Thời điểm cập nhật gần nhất
    public string? LogLUBy { get; set; } // Người cập nhật gần nhất
}

/// <summary>Thiết lập phân vùng HTV cho đại lý (DMS.Sales Mst_DealerZone / DealerZone.cs / FrmMst_DealerZone): gán 1 đại lý vào 1 phân vùng HTV. Khóa nghiệp vụ = (DealerCode, ZoneCode). Quy tắc: MỖI ĐẠI LÝ chỉ thuộc 1 phân vùng ACTIVE — khi gán phân vùng mới, các mapping active cũ của đại lý đó bị set FlagActive='0'.</summary>
public sealed class DealerZone
{
    public long Id { get; set; }
    public Guid OrgId { get; set; }
    public string DealerCode { get; set; } = ""; // Mã đại lý (tham chiếu Mst_Dealer.DealerCode)
    public string? DealerName { get; set; } // Tên đại lý (tra cứu từ Mst_Dealer)
    public string ZoneCode { get; set; } = ""; // Mã phân vùng (tham chiếu Mst_Zone.ZoneCode)
    public string? ZoneName { get; set; } // Tên phân vùng (tra cứu từ Mst_Zone)
    public string? Remark { get; set; } // Ghi chú
    public string FlagActive { get; set; } = "1"; // Cờ hiệu lực (1 = đang áp dụng, 0 = ngừng áp dụng)
    public DateTime LogLUDTime { get; set; } = DateTime.Now; // Thời điểm cập nhật gần nhất
    public string? LogLUBy { get; set; } // Người cập nhật gần nhất
}

/// <summary>Danh mục Tỉnh/Thành (DMS.Sales Mst_Province / Master.cs / Mst_Province_Get): danh mục tỉnh/thành phục vụ tra cứu tuyến vận tải và làm FK cho Kho bãi (Mst_Storage.ProvinceCode). ProvinceCode là khóa nghiệp vụ duy nhất; ProvinceName bắt buộc; FlagActive là cờ hiệu lực.</summary>
public sealed class ProvinceMaster
{
    public long Id { get; set; }
    public Guid OrgId { get; set; }
    public string ProvinceCode { get; set; } = ""; // Mã tỉnh/thành (khóa nghiệp vụ duy nhất, vd: HNI, HCM)
    public string ProvinceName { get; set; } = ""; // Tên tỉnh/thành (bắt buộc)
    public string FlagActive { get; set; } = "1"; // Cờ hiệu lực (1 = đang áp dụng, 0 = ngừng áp dụng)
    public DateTime LogLUDateTime { get; set; } = DateTime.Now; // Thời điểm cập nhật gần nhất
    public string? LogLUBy { get; set; } // Người cập nhật gần nhất
}

/// <summary>Loại kho (DMS.Sales Mst_Storage.StorageType / TConst.MstStorageStorageType): DT = Kho đóng thùng (xưởng đóng thùng xe thương mại), BT = Kho bãi thường.</summary>
public enum StorageType { DT = 0, BT = 1 }

/// <summary>Danh mục Kho bãi (DMS.Sales Mst_Storage / Master.cs / Mst_Storage_Get|Create|Update|Delete|Import): danh sách các kho chứa xe của HTC (xe lưu tại kho trước khi xuất cho đại lý). StorageCode là khóa nghiệp vụ duy nhất; StorageName bắt buộc; ProvinceCode là FK bắt buộc tham chiếu Mst_Province; StorageType chỉ nhận DT (kho đóng thùng) / BT (kho bãi thường); FlagInventoryCost đánh dấu kho có tính chi phí lưu kho. Update chỉ cho sửa StorageName/StorageAddress/FlagInventoryCost (ProvinceCode/StorageType readonly sau khi tạo).</summary>
public sealed class StorageMaster
{
    public long Id { get; set; }
    public Guid OrgId { get; set; }
    public string StorageCode { get; set; } = ""; // Mã kho (khóa nghiệp vụ duy nhất, vd: K01, K02)
    public string ProvinceCode { get; set; } = ""; // Mã tỉnh/thành (FK bắt buộc, tham chiếu Mst_Province.ProvinceCode)
    public string? ProvinceName { get; set; } // Tên tỉnh/thành (tra cứu từ Mst_Province)
    public string StorageName { get; set; } = ""; // Tên kho (bắt buộc)
    public string? StorageAddress { get; set; } // Địa chỉ kho
    public StorageType StorageType { get; set; } = StorageType.BT; // Loại kho (DT = đóng thùng, BT = bãi thường)
    public string FlagInventoryCost { get; set; } = "0"; // Cờ tính chi phí lưu kho (1 = có tính, 0 = không)
    public DateTime LogLUDateTime { get; set; } = DateTime.Now; // Thời điểm cập nhật gần nhất
    public string? LogLUBy { get; set; } // Người cập nhật gần nhất
}

/// <summary>Hạn mức độ trễ vận tải theo Kho & Đại lý (DMS.Sales Mst_DelayTransports / Master.1.cs / MstDelayTransportsController / Mst_DelayTransports_Get|Create|Update|Delete|Import): danh mục quy định số ngày trễ vận tải tối đa (DelayTransport) được chấp nhận khi giao xe từ một Kho (StorageCode) tới một Đại lý (DealerCode). Khóa nghiệp vụ = (StorageCode, DealerCode). Dùng để đối soát phạt chậm vận tải (Pmt_TransportIns) và tính chi phí lưu kho (Pmt_PaymentStorage).</summary>
public sealed class DelayTransportMaster
{
    public long Id { get; set; }
    public Guid OrgId { get; set; }
    public string StorageCode { get; set; } = ""; // Mã kho (khóa nghiệp vụ, tham chiếu Mst_Storage.StorageCode)
    public string? StorageName { get; set; } // Tên kho (tra cứu từ Mst_Storage)
    public string DealerCode { get; set; } = ""; // Mã đại lý (khóa nghiệp vụ, tham chiếu Mst_Dealer.DealerCode)
    public string? DealerName { get; set; } // Tên đại lý (tra cứu từ Mst_Dealer)
    public decimal DelayTransport { get; set; } // Hạn mức độ trễ vận tải (số ngày, >= 0)
    public string FlagActive { get; set; } = "1"; // Cờ hiệu lực (1 = đang áp dụng, 0 = ngừng áp dụng)
    public DateTime LogLUDateTime { get; set; } = DateTime.Now; // Thời điểm cập nhật gần nhất
    public string? LogLUBy { get; set; } // Người cập nhật gần nhất
}

/// <summary>Danh mục Dòng xe (Model) (DMS.Sales Mst_CarModel / Master.Car.cs / Mst_CarModel_Get|Create|Update|Delete|Import): cấp cao nhất trong phân cấp sản phẩm Model → Spec (phiên bản) → Color (màu sắc). ModelCode là khóa nghiệp vụ duy nhất; ModelProductionCode (mã sản xuất) và ModelName bắt buộc; FlagBusinessPlan đánh dấu dòng xe dùng cho kế hoạch kinh doanh; FlagActive là cờ hiệu lực.</summary>
public sealed class CarModelMaster
{
    public long Id { get; set; }
    public Guid OrgId { get; set; }
    public string ModelCode { get; set; } = ""; // Mã dòng xe (khóa nghiệp vụ duy nhất, vd: SF25, TU20)
    public string ModelProductionCode { get; set; } = ""; // Mã sản xuất của dòng xe (bắt buộc)
    public string ModelName { get; set; } = ""; // Tên dòng xe (bắt buộc)
    public string FlagBusinessPlan { get; set; } = "1"; // Cờ dùng cho kế hoạch kinh doanh (1 = có, 0 = không)
    public string FlagActive { get; set; } = "1"; // Cờ hiệu lực (1 = đang áp dụng, 0 = ngừng áp dụng)
    public DateTime LogLUDateTime { get; set; } = DateTime.Now; // Thời điểm cập nhật gần nhất
    public string? LogLUBy { get; set; } // Người cập nhật gần nhất
}

/// <summary>Danh mục Màu sắc xe (DMS.Sales Mst_CarColor / Master.Car.cs / Mst_CarColor_Get|Create|Update|Delete|Import): màu sắc cho từng dòng xe, gồm màu ngoại thất (ColorExt*) và màu nội thất (ColorInt*). Khóa nghiệp vụ = (ModelCode, ColorCode); ModelCode phải tồn tại và đang hoạt động (Mst_CarModel). ColorFee là phụ phí màu (>= 0).</summary>
public sealed class CarColorMaster
{
    public long Id { get; set; }
    public Guid OrgId { get; set; }
    public string ModelCode { get; set; } = ""; // Mã dòng xe (khóa nghiệp vụ, tham chiếu Mst_CarModel.ModelCode)
    public string ColorCode { get; set; } = ""; // Mã màu (khóa nghiệp vụ, duy nhất trong 1 dòng xe)
    public string ColorExtType { get; set; } = ""; // Loại màu ngoại thất (bắt buộc)
    public string ColorExtCode { get; set; } = ""; // Mã màu ngoại thất (bắt buộc)
    public string ColorExtName { get; set; } = ""; // Tên màu ngoại thất (EN, bắt buộc)
    public string ColorExtNameVN { get; set; } = ""; // Tên màu ngoại thất (VN, bắt buộc)
    public string ColorIntCode { get; set; } = ""; // Mã màu nội thất (bắt buộc)
    public string ColorIntName { get; set; } = ""; // Tên màu nội thất (EN, bắt buộc)
    public string ColorIntNameVN { get; set; } = ""; // Tên màu nội thất (VN, bắt buộc)
    public decimal ColorFee { get; set; } // Phụ phí màu (>= 0)
    public string? Remark { get; set; } // Ghi chú
    public string FlagActive { get; set; } = "1"; // Cờ hiệu lực (1 = đang áp dụng, 0 = ngừng áp dụng)
    public DateTime LogLUDateTime { get; set; } = DateTime.Now; // Thời điểm cập nhật gần nhất
    public string? LogLUBy { get; set; } // Người cập nhật gần nhất
}

/// <summary>Giới tính khách hàng đến thăm đại lý (DMS.Sales Dlr_CtmVisit Gender: M = Nam, F = Nữ).</summary>
public enum CustomerVisitGender { Male = 0, Female = 1 }

/// <summary>Lượt khách đến thăm đại lý (DMS.Sales Dlr_CtmVisit / DlrCtmVisitController / DLR_CtmVisit_Create_DL): ghi nhận mỗi lượt khách hàng ghé showroom đại lý để tư vấn xe, phục vụ phân tích nhu cầu thị trường theo giới tính, nhóm tuổi và dòng xe quan tâm. Khóa nghiệp vụ = (CtmVisitCode, DealerCode).</summary>
public sealed class CustomerVisit
{
    public long Id { get; set; }
    public Guid OrgId { get; set; }
    public string CtmVisitCode { get; set; } = ""; // Mã lượt thăm (khóa nghiệp vụ, sinh tự động hoặc user nhập)
    public string DealerCode { get; set; } = ""; // Mã đại lý tạo lượt thăm
    public string? DealerName { get; set; } // Tên đại lý (tra cứu từ dữ liệu đại lý)
    public CustomerVisitGender Gender { get; set; } = CustomerVisitGender.Male; // Giới tính khách (M/F)
    public string? RangeAgeCode { get; set; } // Nhóm tuổi khách (vd: 25-34)
    public string? ModelCode { get; set; } // Mã model xe khách quan tâm
    public string? ModelName { get; set; } // Tên dòng xe quan tâm (tra cứu từ Mst_CarModel)
    public DateTime VisitDTime { get; set; } = DateTime.Now; // Thời điểm khách đến thăm
    public string FlagActive { get; set; } = "1"; // Cờ hiệu lực (1 = đang áp dụng, 0 = ngừng áp dụng)
    public string? Remark { get; set; } // Ghi chú
    public string? CreatedBy { get; set; } // Người tạo lượt thăm
    public DateTime CreatedAt { get; set; } = DateTime.Now; // Thời điểm tạo
    public string? LogLUBy { get; set; } // Người cập nhật gần nhất
    public DateTime LogLUDTime { get; set; } = DateTime.Now; // Thời điểm cập nhật gần nhất
}/// <summary>Nhà vận chuyển (DMS.Sales Mst_Transporter / Master.1.cs / Mst_Transporter_Get|Create|Update|Delete|Import): danh mục công ty vận tải xe từ kho NPP tới đại lý. TransporterCode là khóa nghiệp vụ duy nhất; bắt buộc có TransporterName và TransportContractNo (số hợp đồng vận chuyển). Lưu thông tin pháp nhân (địa chỉ, điện thoại, fax), người đại diện (giám đốc) và người liên hệ.</summary>
public sealed class TransporterMaster
{
    public long Id { get; set; }
    public Guid OrgId { get; set; }
    public string TransporterCode { get; set; } = ""; // Mã nhà vận chuyển (khóa nghiệp vụ duy nhất)
    public string TransporterName { get; set; } = ""; // Tên nhà vận chuyển (bắt buộc)
    public string TransportContractNo { get; set; } = ""; // Số hợp đồng vận chuyển (bắt buộc)
    public string? Address { get; set; } // Địa chỉ
    public string? PhoneNo { get; set; } // Số điện thoại
    public string? FaxNo { get; set; } // Số fax
    public string? DirectorFullName { get; set; } // Tên giám đốc
    public string? DirectorPhoneNo { get; set; } // Điện thoại giám đốc
    public string? ContactorFullName { get; set; } // Tên người liên hệ
    public string? ContactorPhoneNo { get; set; } // Điện thoại người liên hệ
    public string? Remark { get; set; } // Ghi chú
    public string FlagActive { get; set; } = "1"; // Cờ hiệu lực (1 = đang áp dụng, 0 = ngừng áp dụng)
    public DateTime LogLUDateTime { get; set; } = DateTime.Now; // Thời điểm cập nhật gần nhất
    public string? LogLUBy { get; set; } // Người cập nhật gần nhất
}

/// <summary>Xe chuyên dùng của nhà vận chuyển (DMS.Sales Mst_TransporterCar / Master.1.cs / Mst_TransporterCar_Get|Create|Update|Delete|Import): danh mục biển số xe tải chở xe của từng nhà vận chuyển. Khóa nghiệp vụ = (TransporterCode, PlateNo). FK TransporterCode phải tồn tại và đang hoạt động.</summary>
public sealed class TransporterCar
{
    public long Id { get; set; }
    public Guid OrgId { get; set; }
    public string TransporterCode { get; set; } = ""; // Mã nhà vận chuyển (tham chiếu Mst_Transporter.TransporterCode)
    public string PlateNo { get; set; } = ""; // Biển số xe chuyên dùng (bắt buộc)
    public string FlagActive { get; set; } = "1"; // Cờ hiệu lực (1 = đang áp dụng, 0 = ngừng áp dụng)
    public DateTime LogLUDateTime { get; set; } = DateTime.Now; // Thời điểm cập nhật gần nhất
    public string? LogLUBy { get; set; } // Người cập nhật gần nhất
}

/// <summary>Tài xế của nhà vận chuyển (DMS.Sales Mst_TransporterDriver / Master.1.cs / Mst_TransporterDriver_Get|Create|Update|Delete|Import): danh mục tài xế lái xe chuyên dùng của từng nhà vận chuyển. Khóa nghiệp vụ = (TransporterCode, DriverId). Bắt buộc DriverFullName và DriverLicenseNo (số GPLX). FK TransporterCode phải tồn tại.</summary>
public sealed class TransporterDriver
{
    public long Id { get; set; }
    public Guid OrgId { get; set; }
    public string TransporterCode { get; set; } = ""; // Mã nhà vận chuyển (tham chiếu Mst_Transporter.TransporterCode)
    public string DriverId { get; set; } = ""; // Mã tài xế (khóa nghiệp vụ trong phạm vi nhà vận chuyển)
    public string DriverFullName { get; set; } = ""; // Tên tài xế (bắt buộc)
    public string DriverLicenseNo { get; set; } = ""; // Số giấy phép lái xe (bắt buộc)
    public string? DriverPhoneNo { get; set; } // Điện thoại tài xế
    public string FlagActive { get; set; } = "1"; // Cờ hiệu lực (1 = đang áp dụng, 0 = ngừng áp dụng)
    public DateTime LogLUDateTime { get; set; } = DateTime.Now; // Thời điểm cập nhật gần nhất
    public string? LogLUBy { get; set; } // Người cập nhật gần nhất
}

/// <summary>Hạn mức đặt hàng xe theo Đại lý & Quy cách (DMS.Sales Mng_Quota / Master.cs / Mng_Quota_Get|Create|Update|Delete|Import): danh mục quy định số lượng xe tối đa (QtyQuota) mà mỗi Đại lý được phép đặt cho từng quy cách xe (SpecCode). Khóa nghiệp vụ = (DealerCode, SpecCode). FK DealerCode phải tồn tại và đang hoạt động (Mst_Dealer). Khi tạo/import, QtyQuota phải là số >= 0. Update chỉ cho sửa QtyQuota (Master Fixed Updateable Scope).</summary>
public sealed class QuotaMaster
{
    public long Id { get; set; }
    public Guid OrgId { get; set; }
    public string DealerCode { get; set; } = ""; // Mã đại lý (khóa nghiệp vụ, tham chiếu Mst_Dealer.DealerCode)
    public string? DealerName { get; set; } // Tên đại lý (tra cứu từ Mst_Dealer)
    public string SpecCode { get; set; } = ""; // Mã quy cách xe (khóa nghiệp vụ, tham chiếu Mst_CarSpec.SpecCode)
    public string? SpecDescription { get; set; } // Mô tả quy cách xe (tra cứu từ Mst_CarSpec)
    public string? ModelCode { get; set; } // Mã dòng xe (tra cứu từ Mst_CarModel qua SpecCode)
    public string? ModelName { get; set; } // Tên dòng xe (tra cứu từ Mst_CarModel)
    public decimal QtyQuota { get; set; } // Số lượng hạn mức đặt hàng (>= 0)
    public string FlagActive { get; set; } = "1"; // Cờ hiệu lực (1 = đang áp dụng, 0 = ngừng áp dụng)
    public string? UpdateBy { get; set; } // Người cập nhật hạn mức gần nhất
    public DateTime? UpdateDTime { get; set; } // Thời điểm cập nhật hạn mức gần nhất
    public DateTime LogLUDateTime { get; set; } = DateTime.Now; // Thời điểm ghi log cập nhật gần nhất
    public string? LogLUBy { get; set; } // Người ghi log cập nhật gần nhất
}

/// <summary>Ngân hàng đối tác (DMS.Sales Mst_Bank / Master.cs / Mst_Bank_Get|Create|Update|Delete|Import): danh mục ngân hàng cấp bảo lãnh / tài trợ cho đại lý mua xe. BankCode là khóa nghiệp vụ duy nhất; bắt buộc BankName. Các cờ nghiệp vụ: FlagPaymentBank (NH thanh toán), FlagMortageBank (NH hỗ trợ thế chấp/giải chấp), FlagMonitorBank (NH giám sát — chỉ chi hội sở BankCodeParent='ALL' mới được bật). BankCode được tham chiếu trong DealerContract (BankCodeMD), PaymentGuarantee, DealerPayment.</summary>
public sealed class BankMaster
{
    public long Id { get; set; }
    public Guid OrgId { get; set; }
    public string BankCode { get; set; } = ""; // Mã ngân hàng (khóa nghiệp vụ duy nhất)
    public string? BankCodeParent { get; set; } // Mã ngân hàng cha (chi hội sở dùng 'ALL')
    public string? ProvinceCode { get; set; } // Mã tỉnh/thành (tham chiếu Mst_Province)
    public string BankName { get; set; } = ""; // Tên ngân hàng (bắt buộc)
    public string? PhoneNo { get; set; } // Số điện thoại liên hệ
    public string? FaxNo { get; set; } // Số fax
    public string? BenBankCode { get; set; } // Mã ngân hàng thụ hưởng
    public string? Address { get; set; } // Địa chỉ chi nhánh
    public string? PICEmail { get; set; } // Email người liên lạc (nhiều email cách nhau bởi dấu phẩy)
    public string? PICName { get; set; } // Tên người liên lạc
    public string FlagPaymentBank { get; set; } = "0"; // Cờ NH thanh toán (1 = có, 0 = không)
    public string FlagMortageBank { get; set; } = "0"; // Cờ NH hỗ trợ thế chấp (1 = có, 0 = không)
    public string FlagMonitorBank { get; set; } = "0"; // Cờ NH giám sát (1 = có, 0 = không; chỉ chi hội sở BankCodeParent='ALL')
    public string? Remark { get; set; } // Ghi chú
    public string FlagActive { get; set; } = "1"; // Cờ hiệu lực (1 = đang áp dụng, 0 = ngừng áp dụng)
    public DateTime LogLUDateTime { get; set; } = DateTime.Now; // Thời điểm cập nhật gần nhất
    public string? LogLUBy { get; set; } // Người cập nhật gần nhất
}

/// <summary>Loại vi phạm chế tài nhân viên bán hàng (DMS.Sales Mst_ViolateType.ViolateTypeId: TT = Tạm thời [có thời hạn], VV = Vĩnh viễn [không thời hạn]).</summary>
public enum ViolateType { Temporary = 0, Permanent = 1 }

/// <summary>Chế tài / Vi phạm Nhân viên Bán hàng (DMS.Sales HR_SalesManViolate / MasterData.HR.cs / HR_SalesManViolate_Get|Create|Update): ghi nhận vi phạm chế tài của nhân viên bán hàng đại lý (tạm thời có thời hạn hoặc vĩnh viễn). Khóa nghiệp vụ = (SMCode, ViolateNumber) — ViolateNumber tự tăng theo từng nhân viên. Nhân viên đã vi phạm Vĩnh viễn (VV) thì không được lập vi phạm mới; vi phạm Tạm thời (TT) bắt buộc có ngày kết thúc và phải lớn hơn ngày kết thúc vi phạm gần nhất.</summary>
public sealed class SalesManViolate
{
    public long Id { get; set; }
    public Guid OrgId { get; set; }
    public string SMCode { get; set; } = ""; // Mã nhân viên bán hàng (khóa nghiệp vụ, tham chiếu Mst_SalesMan.SMCode)
    public int ViolateNumber { get; set; } = 1; // Số thứ tự vi phạm của nhân viên (tự tăng, khóa nghiệp vụ cùng SMCode)
    public string DealerCode { get; set; } = ""; // Mã đại lý của nhân viên (phải khớp Mst_SalesMan.DealerCode)
    public string? DealerName { get; set; } // Tên đại lý (tra cứu từ dữ liệu đại lý)
    public ViolateType ViolateType { get; set; } = ViolateType.Temporary; // Loại vi phạm (TT tạm thời / VV vĩnh viễn)
    public DateTime ViolateDateStart { get; set; } = DateTime.Today; // Ngày bắt đầu hiệu lực chế tài (bắt buộc)
    public DateTime? ViolateDateEnd { get; set; } // Ngày kết thúc chế tài (bắt buộc với TT, để trống với VV)
    public string? SMHyundaiCode { get; set; } // Mã nhân viên Hyundai (phải khớp Mst_SalesMan.SMHyundaiCode)
    public string? SMName { get; set; } // Tên nhân viên bán hàng
    public DateTime? SMDateOfBirth { get; set; } // Ngày sinh nhân viên
    public string? IdentityCardNo { get; set; } // Số CMND/CCCD nhân viên
    public string? SMPhoneNo { get; set; } // Số điện thoại nhân viên
    public string? SMType { get; set; } // Loại nhân viên bán hàng
    public string? SMStatus { get; set; } // Trạng thái nhân viên tại thời điểm ghi nhận vi phạm
    public string? Remark { get; set; } // Ghi chú / lý do vi phạm
    public string FlagActive { get; set; } = "1"; // Cờ hiệu lực (1 = đang áp dụng, 0 = ngừng áp dụng)
    public string? CreatedBy { get; set; } // Người tạo vi phạm
    public DateTime CreatedAt { get; set; } = DateTime.Now; // Thời điểm tạo
    public string? UpdateBy { get; set; } // Người cập nhật gần nhất
    public DateTime? UpdateDTime { get; set; } // Thời điểm cập nhật gần nhất
    public DateTime LogLUDateTime { get; set; } = DateTime.Now; // Thời điểm ghi log cập nhật gần nhất
    public string? LogLUBy { get; set; } // Người ghi log cập nhật gần nhất
}

/// <summary>Trạng thái Yêu cầu vận chuyển khi chuyển kho (DMS.Sales Sto_RearrangeTranspReq / Sto_TranspReq TranspReqStatus: Pending = 'P' [Chờ duyệt], Approved = 'A' [Đã duyệt điều xe], Rejected = 'R' [Từ chối], Cancelled = 'C' [Đã hủy]).</summary>
public enum RearrangeTranspReqStatus { Pending = 0, Approved = 1, Rejected = 2, Cancelled = 3 }

/// <summary>Trạng thái từng dòng xe trong YCVT chuyển kho (DMS.Sales Sto_RearrangeTranspReqDtl TranspReqDtlStatus: Pending = 'P', Approved = 'A', Rejected = 'R', Cancelled = 'C').</summary>
public enum RearrangeTranspReqDtlStatus { Pending = 0, Approved = 1, Rejected = 2, Cancelled = 3 }

/// <summary>Yêu cầu vận chuyển khi chuyển kho xe ô tô (DMS.Sales Sto_RearrangeTranspReq / Sto_RearrangeTranspReqDtl / Storage.cs / FrmMngRearrangeTranspReq): gom nhóm các xe VIN (mỗi VIN gắn 1 lệnh điều chuyển kho Sto_StorageRearrange đã duyệt A2) vào 1 yêu cầu vận chuyển theo nhà vận chuyển, phục vụ điều độ xe lồng khi chuyển kho nội bộ. Khóa nghiệp vụ = SRTReqNo.</summary>
public sealed class RearrangeTransportRequest
{
    public long Id { get; set; }
    public Guid OrgId { get; set; }
    public string SRTReqNo { get; set; } = ""; // Mã yêu cầu vận chuyển chuyển kho (PK SRTReqNo, vd: SRT26090001)
    public string TransporterCode { get; set; } = ""; // Mã nhà vận chuyển (Mst_Transporter, phải tồn tại + FlagActive='1')
    public string? TransporterName { get; set; } // Tên nhà vận chuyển (tra cứu từ Mst_Transporter)
    public string? TransportContractNo { get; set; } // Số hợp đồng vận chuyển
    public string? DealerCode { get; set; } // Mã đại lý liên quan (nếu có)
    public string? StorageCodeFrom { get; set; } // Kho xuất phát (tổng hợp từ các dòng xe)
    public string? StorageCodeTo { get; set; } // Kho đích đến (tổng hợp từ các dòng xe)
    public RearrangeTranspReqStatus Status { get; set; } = RearrangeTranspReqStatus.Pending; // Trạng thái YCVT (P -> A / R / C)
    public int TotalCars { get; set; } // Tổng số xe trong yêu cầu
    public string? PlateNo { get; set; } // Biển số xe lồng / xe chuyên dùng chở xe
    public string? DriverName { get; set; } // Tài xế lái xe chuyên chở
    public string? DriverPhone { get; set; } // SĐT tài xế
    public DateTime? ExpectedStartDate { get; set; } // Ngày dự kiến bốc xe xuất kho
    public DateTime? ExpectedEndDate { get; set; } // Ngày dự kiến giao xe đến kho đích
    public string? Remark { get; set; } // Ghi chú yêu cầu
    public string? RejectReason { get; set; } // Lý do từ chối (RejectHQ)
    public string? CancelReason { get; set; } // Lý do hủy yêu cầu
    public string? CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public string? ApprovedBy { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public string? CancelledBy { get; set; }
    public DateTime? CancelledAt { get; set; }

    public List<RearrangeTransportRequestDetail> Details { get; set; } = new();
}

/// <summary>Chi tiết dòng xe trong YCVT chuyển kho (DMS.Sales Sto_RearrangeTranspReqDtl): liên kết số khung VIN, lệnh điều chuyển kho nguồn (RefOrdNo = StorageRearrangeNo), kho xuất và kho nhận.</summary>
public sealed class RearrangeTransportRequestDetail
{
    public long Id { get; set; }
    public long RearrangeTranspReqId { get; set; }
    public string SRTReqNo { get; set; } = "";
    public string Vin { get; set; } = ""; // Số khung VIN (17 ký tự)
    public string? Model { get; set; } // Dòng xe
    public string? RefOrdNo { get; set; } // Số lệnh điều chuyển kho nguồn (StorageRearrangeNo)
    public string? StorageCodeFrom { get; set; } // Kho xuất xe
    public string? StorageCodeTo { get; set; } // Kho nhận xe
    public RearrangeTranspReqDtlStatus Status { get; set; } = RearrangeTranspReqDtlStatus.Pending;
    public string? Remark { get; set; }
}

/// <summary>Trạng thái dòng Nhu cầu đặt hàng (DMS.Sales DMS40_Ord_SalesOrderRoot_ApprDemand): Pending = 'P' [Chờ phân bổ], Allocated = 'A' [Đã phân bổ cung], Rejected = 'R' [Từ chối], Cancelled = 'C' [Đã hủy]).</summary>
public enum OrderDemandStatus { Pending = 0, Allocated = 1, Rejected = 2, Cancelled = 3 }

/// <summary>Trạng thái dòng Cung cấp (DMS.Sales DMS40_Ord_SalesOrderRoot_ApprSupply1/2/3): Pending = 'P' [Chờ phân bổ], Allocated = 'A' [Đã phân bổ], Cancelled = 'C' [Đã hủy]).</summary>
public enum OrderSupplyStatus { Pending = 0, Allocated = 1, Cancelled = 2 }

/// <summary>Trạng thái phiên phân bổ Nhu cầu/Cung (DMS.Sales DMS40_Ord_SalesOrderRoot_ApprAuto): Draft = 'D' [Nháp], Allocated = 'A' [Đã phân bổ], Finished = 'F' [Hoàn tất], Cancelled = 'C' [Đã hủy]).</summary>
public enum OrderAllocationStatus { Draft = 0, Allocated = 1, Finished = 2, Cancelled = 3 }

/// <summary>Phiên phân bổ Nhu cầu/Cung đơn đặt hàng xe (DMS.Sales DMS40_Ord_SalesOrderRoot_ApprAuto / DMS40.0.30.Order.cs / 04_DON_HANG_DOANH_SO.md): gom nhu cầu (Demand) của các đại lý và cơ cấu cung (Supply) của NPP trong 1 kỳ tháng, chạy thuật toán phân bổ theo tỷ lệ (SupplyPercent / DemandPercent / Rank) để chốt số lượng xe cấp cho từng đại lý. Khóa nghiệp vụ = AllocationNo.</summary>
public sealed class OrderAllocationSession
{
    public long Id { get; set; }
    public Guid OrgId { get; set; }
    public string AllocationNo { get; set; } = ""; // Mã phiên phân bổ (PK, vd: {yyMM}ALC{seq:D4})
    public string PeriodMonth { get; set; } = ""; // Kỳ tháng phân bổ (YYYYMM, vd: 202601)
    public string? DealerCode { get; set; } // Mã đại lý (nếu phân bổ đơn lẻ theo 1 đại lý; null = phân bổ toàn bộ đại lý trong kỳ)
    public string? SalesPolicyCode { get; set; } // Mã chính sách bán hàng áp dụng (Mst_SalesPolicy)
    public OrderAllocationStatus Status { get; set; } = OrderAllocationStatus.Draft; // Trạng thái phiên phân bổ
    public int TotalDemandQty { get; set; } // Tổng số lượng nhu cầu ban đầu (QtyInit)
    public int TotalSupplyQty { get; set; } // Tổng số lượng cung khả dụng (QtyInit)
    public int TotalAllocatedQty { get; set; } // Tổng số lượng đã phân bổ thực tế (QtyProcess)
    public int TotalUnmetQty { get; set; } // Tổng số lượng nhu cầu không được đáp ứng (QtyRemain)
    public string? Remark { get; set; } // Ghi chú phiên phân bổ
    public string? RejectReason { get; set; } // Lý do từ chối
    public string? CancelReason { get; set; } // Lý do hủy phiên
    public string? CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public string? AllocatedBy { get; set; } // Người chạy phân bổ
    public DateTime? AllocatedAt { get; set; }
    public string? FinishedBy { get; set; } // Người chốt hoàn tất phân bổ
    public DateTime? FinishedAt { get; set; }
    public string? CancelledBy { get; set; }
    public DateTime? CancelledAt { get; set; }

    public List<OrderDemandAllocation> Demands { get; set; } = new();
    public List<OrderSupplyAllocation> Supplies { get; set; } = new();
}

/// <summary>Dòng Nhu cầu đặt hàng của đại lý (DMS.Sales DMS40_Ord_SalesOrderRoot_ApprDemand): số lượng xe đại lý đề xuất mua theo Spec/Model/Color trong kỳ, kèm kết quả phân bổ (QtyProcess, QtyRemain, SupplyPercent, Rank).</summary>
public sealed class OrderDemandAllocation
{
    public long Id { get; set; }
    public long AllocationId { get; set; }
    public string AllocationNo { get; set; } = "";
    public string DealerCode { get; set; } = ""; // Mã đại lý đặt nhu cầu
    public string PeriodMonth { get; set; } = ""; // Kỳ tháng (YYYYMM)
    public string SpecCode { get; set; } = ""; // Mã phiên bản xe (Mst_CarSpec)
    public string ModelCode { get; set; } = ""; // Mã model xe (Mst_CarModel)
    public string? ColorCode { get; set; } // Mã màu xe (Mst_CarColor)
    public string? AssemblyStatus { get; set; } // Loại lắp ráp (CBU / CKD)
    public int QtyInit { get; set; } // Số lượng nhu cầu ban đầu
    public int QtyProcess { get; set; } // Số lượng được phân bổ thực tế (QtyProcess1/2/3)
    public int QtyRemain { get; set; } // Số lượng nhu cầu còn lại chưa được đáp ứng
    public decimal DemandPercent { get; set; } // Tỷ lệ nhu cầu của đại lý so với tổng nhu cầu cùng Spec/Model/Color
    public int Rank { get; set; } // Thứ hạng ưu tiên phân bổ (theo DemandPercent giảm dần)
    public OrderDemandStatus Status { get; set; } = OrderDemandStatus.Pending;
    public string? Remark { get; set; }
}

/// <summary>Dòng Cơ cấu Cung cấp của NPP (DMS.Sales DMS40_Ord_SalesOrderRoot_ApprSupply1/2/3): số lượng xe NPP có thể cung theo Spec/Model/Color trong kỳ, kèm tỷ lệ cung (SupplyPercent) và thứ hạng (Rank).</summary>
public sealed class OrderSupplyAllocation
{
    public long Id { get; set; }
    public long AllocationId { get; set; }
    public string AllocationNo { get; set; } = "";
    public string PeriodMonth { get; set; } = ""; // Kỳ tháng (YYYYMM)
    public string SpecCode { get; set; } = ""; // Mã phiên bản xe
    public string ModelCode { get; set; } = ""; // Mã model xe
    public string? ColorCode { get; set; } // Mã màu xe
    public int QtyInit { get; set; } // Số lượng cung cấp ban đầu
    public int QtyProcess { get; set; } // Số lượng đã phân bổ cho các đại lý
    public int QtyRemain { get; set; } // Số lượng cung còn lại chưa phân bổ
    public decimal SupplyPercent { get; set; } // Tỷ lệ cung của dòng này so với tổng cung cùng Spec/Model/Color
    public int Rank { get; set; } // Thứ hạng ưu tiên (theo SupplyPercent giảm dần)
    public OrderSupplyStatus Status { get; set; } = OrderSupplyStatus.Pending;
    public string? Remark { get; set; }
}

/// <summary>Phiên bản bảng cước phí vận tải (DMS.Sales Mst_TranspFeeVer / MstTranspFeeController / Mst_TranspFeeVer_Get|Create|Delete): header quản lý phiên bản bảng cước phí vận tải xe. TFVCode là khóa nghiệp vụ duy nhất. Mỗi lần tạo phiên bản mới, toàn bộ bảng cước phí hiện hành (Mst_TranspFee) bị xóa và thay bằng dữ liệu phiên bản mới (chỉ phiên bản mới nhất có dữ liệu chi tiết).</summary>
public sealed class TransportFeeVersion
{
    public long Id { get; set; }
    public Guid OrgId { get; set; }
    public string TFVCode { get; set; } = ""; // Mã phiên bản cước phí (khóa nghiệp vụ duy nhất, vd: TFV001)
    public string FlagActive { get; set; } = "1"; // Cờ hiệu lực (1 = đang áp dụng, 0 = ngừng áp dụng)
    public DateTime CreatedDate { get; set; } = DateTime.Now; // Ngày tạo phiên bản
    public DateTime LogLUDateTime { get; set; } = DateTime.Now; // Thời điểm cập nhật gần nhất
    public string? LogLUBy { get; set; } // Người cập nhật gần nhất

    public List<TransportFeeDetail> Details { get; set; } = new();
}

/// <summary>Dòng cước phí vận tải theo tuyến (DMS.Sales Mst_TranspFee): mỗi dòng = 1 tuyến (tỉnh/huyện giao -> tỉnh/huyện nhận) + 1 nhà vận chuyển + 1 model xe, kèm cước phí (ValFee) và số ngày định mức (ExpectedDays). Khóa nghiệp vụ = (ProvinceCodeFrom, DistrictCodeFrom, ProvinceCodeTo, DistrictCodeTo, TransporterCode, ModelCode). Khi tạo phiên bản, server tự tách chuỗi ModelCode/TransporterCode cách phẩy thành nhiều dòng và tự sinh thêm tuyến ngược (đảo From/To).</summary>
public sealed class TransportFeeDetail
{
    public long Id { get; set; }
    public long VersionId { get; set; }
    public string TFVCode { get; set; } = ""; // Mã phiên bản cước phí (FK -> TransportFeeVersion.TFVCode)
    public string ProvinceCodeFrom { get; set; } = ""; // Mã tỉnh giao (bắt buộc)
    public string DistrictCodeFrom { get; set; } = ""; // Mã huyện giao (bắt buộc)
    public string ProvinceCodeTo { get; set; } = ""; // Mã tỉnh nhận (bắt buộc)
    public string DistrictCodeTo { get; set; } = ""; // Mã huyện nhận (bắt buộc)
    public string TransporterCode { get; set; } = ""; // Mã nhà vận chuyển (bắt buộc)
    public string ModelCode { get; set; } = ""; // Mã model xe (bắt buộc)
    public decimal ValFee { get; set; } // Cước phí vận tải (> 0)
    public int ExpectedDays { get; set; } // Số ngày định mức vận chuyển (> 0)
    public bool IsReverseRoute { get; set; } // Cờ tuyến tự sinh ngược (đảo From/To) — bám #tbl_genAuto
    public DateTime LogLUDateTime { get; set; } = DateTime.Now; // Thời điểm cập nhật gần nhất
    public string? LogLUBy { get; set; } // Người cập nhật gần nhất
}

/// <summary>Lịch sử cước phí vận tải (DMS.Sales Mst_TranspFeeHist): lưu vết mỗi lần tạo phiên bản cước phí, giữ nguyên chuỗi TransporterCodeList/ModelCodeList (cách phẩy) như dữ liệu nhập gốc để tra cứu lịch sử mọi phiên bản.</summary>
public sealed class TransportFeeHistory
{
    public long Id { get; set; }
    public Guid OrgId { get; set; }
    public string TFVCode { get; set; } = ""; // Mã phiên bản cước phí
    public string ProvinceCodeFrom { get; set; } = ""; // Mã tỉnh giao
    public string DistrictCodeFrom { get; set; } = ""; // Mã huyện giao
    public string ProvinceCodeTo { get; set; } = ""; // Mã tỉnh nhận
    public string DistrictCodeTo { get; set; } = ""; // Mã huyện nhận
    public string? TransporterCodeList { get; set; } // Danh sách mã nhà vận chuyển (chuỗi cách phẩy, dữ liệu nhập gốc)
    public string? ModelCodeList { get; set; } // Danh sách mã model xe (chuỗi cách phẩy, dữ liệu nhập gốc)
    public decimal ValFee { get; set; } // Cước phí vận tải
    public int ExpectedDays { get; set; } // Số ngày định mức
    public DateTime LogLUDateTime { get; set; } = DateTime.Now; // Thời điểm ghi log
    public string? LogLUBy { get; set; } // Người ghi log
}
/// <summary>Trạng thái nhân viên bán hàng đại lý (DMS.Sales Mst_SalesMan.SMStatus / TConst.SMStatus: NGHIVIEC = '0' [Nghỉ việc], CHINGTHUC = '1' [Chính thức], THUVIEC = '2' [Thử việc], CTVIEN = '3' [Cộng tác viên]).</summary>
public enum SalesManStatus { Resigned = 0, Official = 1, Probation = 2, Collaborator = 3 }

/// <summary>Loại nhân viên bán hàng đại lý (DMS.Sales Mst_SalesMan.SMType / TConst.SMType: GD = Giám đốc, TPBH = Trưởng phòng bán hàng, TPDV = Trưởng phòng dịch vụ, TVBH = Tư vấn bán hàng, GVDTNB = Giảng viên đào tạo nội bộ).</summary>
public enum SalesManType { Director = 0, SalesManager = 1, ServiceManager = 2, SalesConsultant = 3, Trainer = 4 }

/// <summary>Nhân viên bán hàng đại lý (DMS.Sales Mst_SalesMan / MasterData.HR.cs / Mst_SalesMan_Get|CreateMulti|Update|UpdateStatus|CreateAfterApprove|UpdateMultiByHTV): quản lý hồ sơ nhân sự bán hàng của đại lý (tư vấn bán hàng, trưởng phòng, giám đốc...). Khóa nghiệp vụ = SMCode. Trạng thái SMStatus điều khiển cờ hiệu lực FlagActive (NGHIVIEC -> FlagActive='0').</summary>
public sealed class SalesMan
{
    public long Id { get; set; }
    public Guid OrgId { get; set; }
    public string SMCode { get; set; } = ""; // Mã nhân viên bán hàng (khóa nghiệp vụ duy nhất)
    public string? SMHyundaiCode { get; set; } // Mã nhân viên Hyundai (mã định danh trên hệ thống HMC)
    public string DealerCode { get; set; } = ""; // Mã đại lý (Mst_Dealer, phải tồn tại + active)
    public string? DealerName { get; set; } // Tên đại lý (tra cứu từ dữ liệu đại lý)
    public string SMName { get; set; } = ""; // Họ tên nhân viên (bắt buộc)
    public string SMGender { get; set; } = "M"; // Giới tính (M = Nam, F = Nữ)
    public DateTime? SMDateOfBirth { get; set; } // Ngày sinh (bắt buộc)
    public string? SMPhoneNo { get; set; } // Số điện thoại (bắt buộc)
    public string? SMEmail { get; set; } // Email (bắt buộc với loại nhân viên có FlagEmail = 1)
    public string? SMAddress { get; set; } // Địa chỉ (bắt buộc)
    public string? ProvinceCode { get; set; } // Mã tỉnh/thành (Mst_Province, phải tồn tại + active)
    public string? QualificationCode { get; set; } // Mã trình độ (Mst_Qualification, phải tồn tại + active)
    public string? SMSpecialized { get; set; } // Chuyên môn (bắt buộc)
    public string? SMYearExperence { get; set; } // Số năm kinh nghiệm
    public DateTime? SMStartDate { get; set; } // Ngày bắt đầu làm việc (bắt buộc)
    public DateTime? SMEndDate { get; set; } // Ngày nghỉ việc (bắt buộc khi NGHIVIEC)
    public string? DepartmentCode { get; set; } // Mã bộ phận (Mst_Department, phải tồn tại + active)
    public string? SMPosition { get; set; } // Chức vụ (Mst_Position, phải tồn tại + active)
    public string? SMPostionCode { get; set; } // Mã vị trí (Mst_Position, phải tồn tại + active)
    public SalesManType SMType { get; set; } = SalesManType.SalesConsultant; // Loại nhân viên bán hàng
    public string? CertificateCode { get; set; } // Mã chứng chỉ
    public SalesManStatus SMStatus { get; set; } = SalesManStatus.Probation; // Trạng thái nhân viên
    public string? SMReason { get; set; } // Lý do nghỉ việc (bắt buộc khi NGHIVIEC)
    public string? SMDesc { get; set; } // Mô tả chi tiết lý do nghỉ việc (bắt buộc khi NGHIVIEC)
    public string? IdentityCardNo { get; set; } // Số CMND/CCCD (bắt buộc)
    public string? WebsiteLink { get; set; } // Link website (bắt buộc với TVBH)
    public string? FacebookLink { get; set; } // Link Facebook (bắt buộc với TVBH)
    public string? FanpageLink { get; set; } // Link Fanpage (bắt buộc với TVBH)
    public string? GroupLink { get; set; } // Link Group (bắt buộc với TVBH)
    public string? ZaloLink { get; set; } // Link Zalo (bắt buộc với TVBH)
    public string? DealerSaleCode { get; set; } // Mã điểm bán hàng (Mst_DealerSale, bắt buộc với TVBH/GVDTNB)
    public string FlagActive { get; set; } = "1"; // Cờ hiệu lực (1 = đang làm việc, 0 = đã nghỉ việc)
    public string? CreatedBy { get; set; } // Người tạo
    public DateTime CreatedAt { get; set; } = DateTime.Now; // Thời điểm tạo
    public string? UpdateBy { get; set; } // Người cập nhật gần nhất
    public DateTime? UpdateDTime { get; set; } // Thời điểm cập nhật gần nhất
    public string? UpdateStatusBy { get; set; } // Người cập nhật trạng thái gần nhất
    public DateTime? UpdateStatusDtime { get; set; } // Thời điểm cập nhật trạng thái gần nhất
    public DateTime LogLUDateTime { get; set; } = DateTime.Now; // Thời điểm ghi log cập nhật gần nhất
    public string? LogLUBy { get; set; } // Người ghi log cập nhật gần nhất
}

/// <summary>Lịch sử nhân viên bán hàng nghỉ việc (DMS.Sales Mst_SalesManHistoryInactive / MasterData.HR.cs / Mst_SalesMan_UpdateStatusX): lưu vết snapshot nhân viên tại thời điểm chuyển sang nghỉ việc (NGHIVIEC), phục vụ tra cứu lịch sử và kiểm tra ràng buộc ngày bắt đầu làm lại.</summary>
public sealed class SalesManHistoryInactive
{
    public long Id { get; set; }
    public Guid OrgId { get; set; }
    public string SMCode { get; set; } = ""; // Mã nhân viên bán hàng
    public string? SMHyundaiCode { get; set; } // Mã nhân viên Hyundai
    public string DealerCode { get; set; } = ""; // Mã đại lý
    public SalesManStatus SMStatus { get; set; } // Trạng thái nhân viên tại thời điểm nghỉ việc
    public string? IdentityCardNo { get; set; } // Số CMND/CCCD
    public string SMFlagActive { get; set; } = "1"; // Cờ hiệu lực tại thời điểm nghỉ việc
    public DateTime? SMStartDate { get; set; } // Ngày bắt đầu làm việc
    public DateTime? SMEndDate { get; set; } // Ngày nghỉ việc
    public string? SMReason { get; set; } // Lý do nghỉ việc
    public string? SMDesc { get; set; } // Mô tả chi tiết lý do nghỉ việc
    public DateTime InactiveDateTime { get; set; } = DateTime.Now; // Thời điểm ghi nhận nghỉ việc
    public string? InactiveBy { get; set; } // Người ghi nhận nghỉ việc
}/// <summary>Hạng mục công việc bảo dưỡng định kỳ (DMS.Sales Mst_MaintainTask / Master.cs / Mst_MaintainTask_Get|Create|Update|Delete|Import): danh mục loại công việc bảo dưỡng xe (thay dầu, kiểm tra phanh...). Khóa nghiệp vụ = MtnTkCode.</summary>
public sealed class MaintainTask
{
    public long Id { get; set; }
    public Guid OrgId { get; set; }
    public string MtnTkCode { get; set; } = ""; // Mã hạng mục công việc (khóa nghiệp vụ duy nhất)
    public string MtnTkName { get; set; } = ""; // Tên hạng mục công việc (bắt buộc)
    public string? MtnTkType { get; set; } // Loại hạng mục công việc
    public string FlagActive { get; set; } = "1"; // Cờ hiệu lực ('1' = active, '0' = inactive)
    public DateTime LogLUDateTime { get; set; } = DateTime.Now; // Thời điểm ghi log cập nhật gần nhất
    public string? LogLUBy { get; set; } // Người ghi log cập nhật gần nhất
}

/// <summary>Chi tiết hạng mục công việc bảo dưỡng (DMS.Sales Mst_MaintainTaskItem / Master.1.cs / Mst_MaintainTaskItem_Get|Create|Update|Delete|Import): nội dung công việc cụ thể thuộc một loại công việc bảo dưỡng. Khóa nghiệp vụ = (MtnTkCode, MtnTkItemCode); FK MtnTkCode phải tồn tại trong Mst_MaintainTask.</summary>
public sealed class MaintainTaskItem
{
    public long Id { get; set; }
    public Guid OrgId { get; set; }
    public string MtnTkCode { get; set; } = ""; // Mã loại công việc bảo dưỡng (FK Mst_MaintainTask, khóa chính 1)
    public string MtnTkItemCode { get; set; } = ""; // Mã hạng mục công việc (khóa chính 2)
    public string MtnTkItemName { get; set; } = ""; // Tên/nội dung hạng mục công việc (bắt buộc)
    public int ViewIdx { get; set; } // Thứ tự hiển thị
    public string FlagActive { get; set; } = "1"; // Cờ hiệu lực ('1' = active, '0' = inactive)
    public DateTime LogLUDateTime { get; set; } = DateTime.Now; // Thời điểm ghi log cập nhật gần nhất
    public string? LogLUBy { get; set; } // Người ghi log cập nhật gần nhất
}/// <summary>Ngưỡng tồn kho đại lý theo model (DMS.Sales Mst_DealerInventoryThreshold / Master.cs / Mst_DealerInventoryThreshold_Get|Update|Import|Delete): danh mục quy định số lượng tồn kho tối thiểu (Qty) mà mỗi Đại lý cần duy trì cho từng dòng xe (ModelCode). Khóa nghiệp vụ = (DealerCode, ModelCode). FK DealerCode phải tồn tại và đang hoạt động (Mst_Dealer); FK ModelCode phải tồn tại và đang hoạt động (Mst_CarModel). Update chỉ cho sửa Qty + FlagActive (Master Fixed Updateable Scope).</summary>
public sealed class DealerInventoryThreshold
{
    public long Id { get; set; }
    public Guid OrgId { get; set; }
    public string DealerCode { get; set; } = ""; // Mã đại lý (khóa nghiệp vụ, tham chiếu Mst_Dealer.DealerCode)
    public string? DealerName { get; set; } // Tên đại lý (tra cứu từ Mst_Dealer)
    public string ModelCode { get; set; } = ""; // Mã dòng xe (khóa nghiệp vụ, tham chiếu Mst_CarModel.ModelCode)
    public string? ModelName { get; set; } // Tên dòng xe (tra cứu từ Mst_CarModel)
    public decimal Qty { get; set; } // Ngưỡng tồn kho tối thiểu (số lượng, >= 0)
    public string FlagActive { get; set; } = "1"; // Cờ hiệu lực (1 = đang áp dụng, 0 = ngừng áp dụng)
    public DateTime LogLUDateTime { get; set; } = DateTime.Now; // Thời điểm ghi log cập nhật gần nhất
    public string? LogLUBy { get; set; } // Người ghi log cập nhật gần nhất
}