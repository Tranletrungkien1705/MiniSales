using Microsoft.EntityFrameworkCore;
using MiniSales.Models;

namespace MiniSales.Data;

public static class Seeder
{
    public static async Task SeedAsync(AppDbContext db)
    {
        await db.Database.EnsureCreatedAsync();

        // Đảm bảo bảng DeliveryOrders tồn tại trong trường hợp database sqlite đã được tạo từ trước
        try
        {
            await db.Database.ExecuteSqlRawAsync(@"
                CREATE TABLE IF NOT EXISTS DeliveryOrders (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    OrgId TEXT NOT NULL,
                    DeliveryOrderNo TEXT NOT NULL,
                    OrderId INTEGER NOT NULL,
                    OrderCode TEXT NOT NULL,
                    DealerCode TEXT NOT NULL,
                    CustomerName TEXT NOT NULL,
                    Phone TEXT NOT NULL,
                    Vin TEXT,
                    Model TEXT NOT NULL,
                    RecipientName TEXT NOT NULL,
                    RecipientPhone TEXT NOT NULL,
                    DeliveryAddress TEXT NOT NULL,
                    TransporterName TEXT,
                    PlateNo TEXT,
                    DeliveryExpectedDate TEXT,
                    DeliveredAt TEXT,
                    Status INTEGER NOT NULL,
                    Remark TEXT,
                    HandoverNote TEXT,
                    CreatedAt TEXT NOT NULL,
                    ApprovedAt TEXT,
                    CancelledAt TEXT
                );
                CREATE UNIQUE INDEX IF NOT EXISTS IX_DeliveryOrders_OrgId_DeliveryOrderNo ON DeliveryOrders(OrgId, DeliveryOrderNo);

                CREATE TABLE IF NOT EXISTS DealerOrders (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    OrgId TEXT NOT NULL,
                    OrderNo TEXT NOT NULL,
                    DealerCode TEXT NOT NULL,
                    DealerName TEXT NOT NULL,
                    OrderType INTEGER NOT NULL,
                    OrderMonth TEXT NOT NULL,
                    SalesPolicyCode TEXT,
                    Status INTEGER NOT NULL,
                    TotalQuantity INTEGER NOT NULL,
                    TotalAmount REAL NOT NULL,
                    Remark TEXT,
                    RejectReason TEXT,
                    CreatedAt TEXT NOT NULL,
                    Approved1At TEXT,
                    Approved2At TEXT,
                    CancelledAt TEXT
                );
                CREATE UNIQUE INDEX IF NOT EXISTS IX_DealerOrders_OrgId_OrderNo ON DealerOrders(OrgId, OrderNo);

                CREATE TABLE IF NOT EXISTS DealerOrderItems (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    OrderId INTEGER NOT NULL,
                    Model TEXT NOT NULL,
                    SpecCode TEXT,
                    ColorCode TEXT,
                    RequestedQuantity INTEGER NOT NULL,
                    ApprovedQuantity INTEGER NOT NULL,
                    UnitPrice REAL NOT NULL,
                    TotalAmount REAL NOT NULL,
                    Remark TEXT,
                    FOREIGN KEY(OrderId) REFERENCES DealerOrders(Id) ON DELETE CASCADE
                );

                CREATE TABLE IF NOT EXISTS PaymentGuarantees (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    OrgId TEXT NOT NULL,
                    GuaranteeNo TEXT NOT NULL,
                    BankGuaranteeNo TEXT NOT NULL,
                    BankCode TEXT NOT NULL,
                    BankName TEXT NOT NULL,
                    DealerCode TEXT NOT NULL,
                    DealerName TEXT NOT NULL,
                    GuaranteeType INTEGER NOT NULL,
                    Status INTEGER NOT NULL,
                    TotalAmount REAL NOT NULL,
                    AllocatedAmount REAL NOT NULL,
                    Term INTEGER NOT NULL,
                    DateOpen TEXT NOT NULL,
                    DateExpired TEXT NOT NULL,
                    Fee REAL NOT NULL,
                    BankCodeMonitor TEXT,
                    DateRecieveGrtRoot TEXT,
                    Remark TEXT,
                    RemarkReject TEXT,
                    CreatedAt TEXT NOT NULL,
                    ApprovedAt TEXT,
                    CancelledAt TEXT
                );
                CREATE UNIQUE INDEX IF NOT EXISTS IX_PaymentGuarantees_OrgId_GuaranteeNo ON PaymentGuarantees(OrgId, GuaranteeNo);

                CREATE TABLE IF NOT EXISTS PaymentGuaranteeDetails (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    GuaranteeId INTEGER NOT NULL,
                    Vin TEXT,
                    Model TEXT NOT NULL,
                    OrderNo TEXT,
                    GuaranteeValue REAL NOT NULL,
                    NumberOfDaysDeferredPayment INTEGER NOT NULL,
                    DateStart TEXT,
                    DateEnd TEXT,
                    Remark TEXT,
                    FOREIGN KEY(GuaranteeId) REFERENCES PaymentGuarantees(Id) ON DELETE CASCADE
                );

                CREATE TABLE IF NOT EXISTS CarDocRequests (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    OrgId TEXT NOT NULL,
                    DRListCode TEXT NOT NULL,
                    DealerCode TEXT NOT NULL,
                    DealerName TEXT NOT NULL,
                    RequestType INTEGER NOT NULL,
                    Status INTEGER NOT NULL,
                    LetterRepresentationNo TEXT,
                    LetterRepresentationDate TEXT,
                    LoanSupportDay INTEGER NOT NULL,
                    TotalCars INTEGER NOT NULL,
                    Remark TEXT,
                    RejectReason TEXT,
                    CreatedAt TEXT NOT NULL,
                    Approved1At TEXT,
                    Approved2At TEXT,
                    CancelledAt TEXT
                );
                CREATE UNIQUE INDEX IF NOT EXISTS IX_CarDocRequests_OrgId_DRListCode ON CarDocRequests(OrgId, DRListCode);

                CREATE TABLE IF NOT EXISTS CarDocRequestDetails (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    RequestId INTEGER NOT NULL,
                    Vin TEXT NOT NULL,
                    Model TEXT NOT NULL,
                    EngineNo TEXT,
                    OrderNo TEXT,
                    DeliveryOrderNo TEXT,
                    BankCode TEXT,
                    BankGuaranteeNo TEXT,
                    DocumentsStatus TEXT NOT NULL,
                    ExpectedDate TEXT,
                    HandoverDate TEXT,
                    RecipientName TEXT,
                    RecipientPhone TEXT,
                    Remark TEXT,
                    FOREIGN KEY(RequestId) REFERENCES CarDocRequests(Id) ON DELETE CASCADE
                );

                CREATE TABLE IF NOT EXISTS CarInvoices (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    OrgId TEXT NOT NULL,
                    InvoiceCode TEXT NOT NULL,
                    InvoiceNo TEXT,
                    InvoiceSerial TEXT NOT NULL,
                    LookupCode TEXT,
                    DealerCode TEXT NOT NULL,
                    DealerName TEXT NOT NULL,
                    Issuer TEXT NOT NULL,
                    BankCode TEXT NOT NULL,
                    BankName TEXT,
                    InvoiceType INTEGER NOT NULL,
                    AdjType INTEGER NOT NULL,
                    RefInvoiceCode TEXT,
                    Status INTEGER NOT NULL,
                    VatRate REAL NOT NULL,
                    SubTotal REAL NOT NULL,
                    VatAmount REAL NOT NULL,
                    TotalAmount REAL NOT NULL,
                    TotalCars INTEGER NOT NULL,
                    BuyerTaxCode TEXT,
                    BuyerAddress TEXT,
                    Reason TEXT,
                    Remark TEXT,
                    InvoiceDate TEXT NOT NULL,
                    CreatedAt TEXT NOT NULL,
                    IssuedAt TEXT,
                    CancelledAt TEXT
                );
                CREATE UNIQUE INDEX IF NOT EXISTS IX_CarInvoices_OrgId_InvoiceCode ON CarInvoices(OrgId, InvoiceCode);

                CREATE TABLE IF NOT EXISTS CarInvoiceDetails (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    InvoiceId INTEGER NOT NULL,
                    Vin TEXT NOT NULL,
                    Model TEXT NOT NULL,
                    SpecCode TEXT,
                    ColorCode TEXT,
                    EngineNo TEXT,
                    UnitPrice REAL NOT NULL,
                    VatRate REAL NOT NULL,
                    VatAmount REAL NOT NULL,
                    TotalAmount REAL NOT NULL,
                    OrderNo TEXT,
                    DeliveryOrderNo TEXT,
                    Remark TEXT,
                    FOREIGN KEY(InvoiceId) REFERENCES CarInvoices(Id) ON DELETE CASCADE
                );

                CREATE TABLE IF NOT EXISTS CarRetrieveOrders (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    OrgId TEXT NOT NULL,
                    RetrieveOrderNo TEXT NOT NULL,
                    DealerCode TEXT NOT NULL,
                    DealerName TEXT NOT NULL,
                    Status INTEGER NOT NULL,
                    StorageCode TEXT NOT NULL,
                    StorageName TEXT NOT NULL,
                    TotalCars INTEGER NOT NULL,
                    Remark TEXT,
                    RejectReason TEXT,
                    CreatedBy TEXT,
                    CreatedAt TEXT NOT NULL,
                    ApprovedBy TEXT,
                    ApprovedAt TEXT,
                    CompletedAt TEXT,
                    CancelledAt TEXT
                );
                CREATE UNIQUE INDEX IF NOT EXISTS IX_CarRetrieveOrders_OrgId_RetrieveOrderNo ON CarRetrieveOrders(OrgId, RetrieveOrderNo);

                CREATE TABLE IF NOT EXISTS CarRetrieveOrderDetails (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    RetrieveOrderId INTEGER NOT NULL,
                    CarId TEXT NOT NULL,
                    Vin TEXT NOT NULL,
                    Model TEXT NOT NULL,
                    DeliveryOrderNo TEXT,
                    StorageCode TEXT NOT NULL,
                    Status INTEGER NOT NULL,
                    ExpectedStartDate TEXT,
                    ExpectedEndDate TEXT,
                    ActualOutDate TEXT,
                    ActualEndDate TEXT,
                    EngineNo TEXT,
                    Color TEXT,
                    Remark TEXT,
                    FOREIGN KEY(RetrieveOrderId) REFERENCES CarRetrieveOrders(Id) ON DELETE CASCADE
                );

                CREATE TABLE IF NOT EXISTS ContractCancels (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    OrgId TEXT NOT NULL,
                    ContractCNo TEXT NOT NULL,
                    DealerCode TEXT NOT NULL,
                    DealerName TEXT NOT NULL,
                    Status INTEGER NOT NULL,
                    TotalCars INTEGER NOT NULL,
                    Remark TEXT,
                    RejectReason TEXT,
                    CreatedBy TEXT,
                    CreatedAt TEXT NOT NULL,
                    ApprovedBy TEXT,
                    ApprovedAt TEXT,
                    CancelledAt TEXT
                );
                CREATE UNIQUE INDEX IF NOT EXISTS IX_ContractCancels_OrgId_ContractCNo ON ContractCancels(OrgId, ContractCNo);

                CREATE TABLE IF NOT EXISTS ContractCancelCars (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    ContractCancelId INTEGER NOT NULL,
                    OrderCode TEXT NOT NULL,
                    OrderId INTEGER,
                    Vin TEXT,
                    Model TEXT NOT NULL,
                    SpecCode TEXT,
                    ColorCode TEXT,
                    CtrCTDNo TEXT NOT NULL,
                    CtrCTDName TEXT NOT NULL,
                    CtrCType TEXT NOT NULL,
                    Remark TEXT,
                    FOREIGN KEY(ContractCancelId) REFERENCES ContractCancels(Id) ON DELETE CASCADE
                );

                CREATE TABLE IF NOT EXISTS ContractCancelDtls (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    ContractCancelId INTEGER NOT NULL,
                    OrderCode TEXT NOT NULL,
                    Model TEXT NOT NULL,
                    SpecCode TEXT,
                    ColorCode TEXT,
                    Qty INTEGER NOT NULL,
                    Remark TEXT,
                    FOREIGN KEY(ContractCancelId) REFERENCES ContractCancels(Id) ON DELETE CASCADE
                );

                CREATE TABLE IF NOT EXISTS TransportMinutes (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    OrgId TEXT NOT NULL,
                    TransportMinutesNo TEXT NOT NULL,
                    DealerCode TEXT NOT NULL,
                    DealerName TEXT NOT NULL,
                    TransporterName TEXT,
                    PlateNo TEXT,
                    DriverName TEXT,
                    DriverPhone TEXT,
                    BankCode TEXT,
                    BankName TEXT,
                    TransportMinutesDate TEXT NOT NULL,
                    Status INTEGER NOT NULL,
                    DLSignStatus INTEGER NOT NULL,
                    HTCSignStatus INTEGER NOT NULL,
                    TotalCars INTEGER NOT NULL,
                    DLCreatedBy TEXT,
                    DLCreatedAt TEXT NOT NULL,
                    DLApprBy TEXT,
                    DLApprAt TEXT,
                    HTCApprBy TEXT,
                    HTCApprAt TEXT,
                    HTCCancelBy TEXT,
                    HTCCancelAt TEXT,
                    CancelReason TEXT,
                    FilePath TEXT,
                    Remark TEXT
                );
                CREATE UNIQUE INDEX IF NOT EXISTS IX_TransportMinutes_OrgId_TransportMinutesNo ON TransportMinutes(OrgId, TransportMinutesNo);

                CREATE TABLE IF NOT EXISTS TransportMinutesDetails (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    TransportMinutesId INTEGER NOT NULL,
                    CarId TEXT NOT NULL,
                    Vin TEXT NOT NULL,
                    Model TEXT NOT NULL,
                    SpecCode TEXT,
                    ColorCode TEXT,
                    EngineNo TEXT,
                    DeliveryOrderNo TEXT,
                    OrderNo TEXT,
                    GuaranteeNo TEXT,
                    Odometer INTEGER NOT NULL,
                    ConditionNote TEXT,
                    Status INTEGER NOT NULL,
                    Remark TEXT,
                    FOREIGN KEY(TransportMinutesId) REFERENCES TransportMinutes(Id) ON DELETE CASCADE
                );

                CREATE TABLE IF NOT EXISTS DealerContracts (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    OrgId TEXT NOT NULL,
                    ContractNo TEXT NOT NULL,
                    DealerCode TEXT NOT NULL,
                    DealerName TEXT NOT NULL,
                    ContractDate TEXT NOT NULL,
                    ContractType INTEGER NOT NULL,
                    ParentContractNo TEXT,
                    PaymentType INTEGER NOT NULL,
                    BankCode TEXT,
                    BankName TEXT,
                    DepositPercent REAL NOT NULL,
                    GuaranteeDays INTEGER NOT NULL,
                    TotalCars INTEGER NOT NULL,
                    TotalAmount REAL NOT NULL,
                    Status INTEGER NOT NULL,
                    DealerSignStatus INTEGER NOT NULL,
                    HQSignStatus INTEGER NOT NULL,
                    DealerSignedBy TEXT,
                    DealerSignedAt TEXT,
                    HQSignedBy TEXT,
                    HQSignedAt TEXT,
                    Approved1At TEXT,
                    RejectReason TEXT,
                    CancelReason TEXT,
                    SignedFilePath TEXT,
                    Remark TEXT,
                    CreatedBy TEXT,
                    CreatedAt TEXT NOT NULL,
                    CancelledAt TEXT
                );
                CREATE UNIQUE INDEX IF NOT EXISTS IX_DealerContracts_OrgId_ContractNo ON DealerContracts(OrgId, ContractNo);

                CREATE TABLE IF NOT EXISTS DealerContractDetails (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    ContractId INTEGER NOT NULL,
                    CarId TEXT NOT NULL,
                    Vin TEXT NOT NULL,
                    Model TEXT NOT NULL,
                    SpecCode TEXT,
                    ColorCode TEXT,
                    ProductionYear INTEGER NOT NULL,
                    UnitPrice REAL NOT NULL,
                    VatRate REAL NOT NULL,
                    TotalAmount REAL NOT NULL,
                    OrderNo TEXT,
                    Remark TEXT,
                    FOREIGN KEY(ContractId) REFERENCES DealerContracts(Id) ON DELETE CASCADE
                );

                CREATE TABLE IF NOT EXISTS RetailContracts (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    OrgId TEXT NOT NULL,
                    ContractNo TEXT NOT NULL,
                    ContractNoUser TEXT NOT NULL,
                    DealerCode TEXT NOT NULL,
                    DealerName TEXT NOT NULL,
                    DealerCodeBuyer TEXT,
                    CustomerCode TEXT NOT NULL,
                    CustomerName TEXT NOT NULL,
                    CustomerPhone TEXT NOT NULL,
                    CustomerIdCardType TEXT NOT NULL,
                    CustomerIdCardNo TEXT NOT NULL,
                    CustomerDateOfBirth TEXT,
                    CustomerAddress TEXT,
                    CustomerType INTEGER NOT NULL,
                    TransactorCode TEXT,
                    TransactorName TEXT,
                    TransactorPhone TEXT,
                    DriverCode TEXT,
                    DealerSaleCode TEXT,
                    SalesType TEXT NOT NULL,
                    SMCode TEXT NOT NULL,
                    SMName TEXT,
                    ContractDate TEXT NOT NULL,
                    PaymentType INTEGER NOT NULL,
                    BankCode TEXT,
                    BankName TEXT,
                    BankLoanAmount REAL NOT NULL,
                    DepositAmount REAL NOT NULL,
                    PaidAmount REAL NOT NULL,
                    TotalCars INTEGER NOT NULL,
                    TotalAmount REAL NOT NULL,
                    Status INTEGER NOT NULL,
                    FlagDealFinish INTEGER NOT NULL,
                    VersionCount INTEGER NOT NULL,
                    VersionDTimeCurr TEXT NOT NULL,
                    ApprovedAt TEXT,
                    ApprovedBy TEXT,
                    CancelledAt TEXT,
                    CancelledBy TEXT,
                    CancelReason TEXT,
                    Remark TEXT,
                    CreatedBy TEXT,
                    CreatedAt TEXT NOT NULL
                );
                CREATE UNIQUE INDEX IF NOT EXISTS IX_RetailContracts_OrgId_ContractNo ON RetailContracts(OrgId, ContractNo);

                CREATE TABLE IF NOT EXISTS RetailContractDetails (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    ContractId INTEGER NOT NULL,
                    ContractNo TEXT NOT NULL,
                    Model TEXT NOT NULL,
                    SpecCode TEXT,
                    ColorCode TEXT,
                    ProductionYear INTEGER NOT NULL,
                    Qty INTEGER NOT NULL,
                    QtyDelivery INTEGER NOT NULL,
                    QtyCancel INTEGER NOT NULL,
                    UnitPrice REAL NOT NULL,
                    Discount REAL NOT NULL,
                    TotalAmount REAL NOT NULL,
                    DlvExpectedDate TEXT,
                    Remark TEXT,
                    FOREIGN KEY(ContractId) REFERENCES RetailContracts(Id) ON DELETE CASCADE
                );

                CREATE TABLE IF NOT EXISTS RetailContractCars (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    ContractId INTEGER NOT NULL,
                    ContractNo TEXT NOT NULL,
                    CtrCarId TEXT NOT NULL,
                    Model TEXT NOT NULL,
                    SpecCode TEXT,
                    ColorCode TEXT,
                    Vin TEXT,
                    Status INTEGER NOT NULL,
                    DeliveryDate TEXT,
                    Remark TEXT,
                    FOREIGN KEY(ContractId) REFERENCES RetailContracts(Id) ON DELETE CASCADE
                );

                CREATE TABLE IF NOT EXISTS RetailContractHistories (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    ContractId INTEGER NOT NULL,
                    ContractNo TEXT NOT NULL,
                    VersionCount INTEGER NOT NULL,
                    VersionDTimeCurr TEXT NOT NULL,
                    Model TEXT NOT NULL,
                    SpecCode TEXT,
                    ColorCode TEXT,
                    Qty INTEGER NOT NULL,
                    UnitPrice REAL NOT NULL,
                    TotalAmount REAL NOT NULL,
                    DlvExpectedDate TEXT,
                    Remark TEXT,
                    LoggedBy TEXT,
                    LogDateTime TEXT NOT NULL,
                    FOREIGN KEY(ContractId) REFERENCES RetailContracts(Id) ON DELETE CASCADE
                );

                CREATE TABLE IF NOT EXISTS DealerPayments (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    OrgId TEXT NOT NULL,
                    PaymentNo TEXT NOT NULL,
                    DealerCode TEXT NOT NULL,
                    DealerName TEXT NOT NULL,
                    ContractNo TEXT,
                    PaymentType INTEGER NOT NULL,
                    PaymentMethod INTEGER NOT NULL,
                    TotalAmount REAL NOT NULL,
                    PaymentDate TEXT NOT NULL,
                    PaymentDueDate TEXT,
                    PaymentEndDate TEXT,
                    Status INTEGER NOT NULL,
                    BankPaymentNo TEXT,
                    BankCodeSend TEXT,
                    BankAccountSend TEXT,
                    BankCodeReceive TEXT,
                    BankAccountReceive TEXT,
                    Funds TEXT,
                    BankLending TEXT,
                    InterestRate REAL,
                    LoanPeriod INTEGER,
                    GuaranteeType TEXT,
                    DepositPercent REAL NOT NULL,
                    AccountingRecordNo TEXT,
                    AccountingRecordDate TEXT,
                    Remark TEXT,
                    RejectReason TEXT,
                    CancelReason TEXT,
                    CreatedBy TEXT,
                    CreatedAt TEXT NOT NULL,
                    ApprovedBy TEXT,
                    ApprovedAt TEXT,
                    FinishedBy TEXT,
                    FinishedAt TEXT,
                    CancelledBy TEXT,
                    CancelledAt TEXT
                );
                CREATE UNIQUE INDEX IF NOT EXISTS IX_DealerPayments_OrgId_PaymentNo ON DealerPayments(OrgId, PaymentNo);

                CREATE TABLE IF NOT EXISTS DealerPaymentDetails (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    PaymentId INTEGER NOT NULL,
                    PaymentNo TEXT NOT NULL,
                    CarId TEXT NOT NULL,
                    Vin TEXT NOT NULL,
                    Model TEXT NOT NULL,
                    Amount REAL NOT NULL,
                    GuaranteeNo TEXT,
                    Remark TEXT,
                    FOREIGN KEY(PaymentId) REFERENCES DealerPayments(Id) ON DELETE CASCADE
                );

                CREATE TABLE IF NOT EXISTS RetailDeals (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    OrgId TEXT NOT NULL,
                    DealNo TEXT NOT NULL,
                    DealNoUser TEXT NOT NULL,
                    DealerCode TEXT NOT NULL,
                    DealerName TEXT NOT NULL,
                    DealerCodeBuyer TEXT,
                    DlrContractNo TEXT,
                    SalesType TEXT NOT NULL,
                    CustomerType INTEGER NOT NULL,
                    DealDate TEXT NOT NULL,
                    CustomerCodeBuyer TEXT NOT NULL,
                    BuyerFullName TEXT NOT NULL,
                    BuyerPhone TEXT NOT NULL,
                    BuyerIdCardNo TEXT NOT NULL,
                    BuyerAddress TEXT,
                    CustomerCodeHolder TEXT,
                    HolderFullName TEXT,
                    CustomerCodeDriver TEXT,
                    DriverFullName TEXT,
                    SMCode TEXT NOT NULL,
                    SMName TEXT,
                    PaymentType INTEGER NOT NULL,
                    BankCode TEXT,
                    BankName TEXT,
                    BankLoanAmount REAL NOT NULL,
                    FlagPDI INTEGER NOT NULL,
                    ReasonNotPDI TEXT,
                    CtmCareFlag INTEGER NOT NULL,
                    CtmCareUpdDate TEXT,
                    CtmCareUpdBy TEXT,
                    Status INTEGER NOT NULL,
                    TotalCars INTEGER NOT NULL,
                    TotalAmount REAL NOT NULL,
                    Remark TEXT,
                    CreatedBy TEXT,
                    CreatedAt TEXT NOT NULL,
                    CancelledBy TEXT,
                    CancelledAt TEXT,
                    CancelReason TEXT
                );
                CREATE UNIQUE INDEX IF NOT EXISTS IX_RetailDeals_OrgId_DealNo ON RetailDeals(OrgId, DealNo);

                CREATE TABLE IF NOT EXISTS RetailDealDetails (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    DealId INTEGER NOT NULL,
                    DealNo TEXT NOT NULL,
                    CarId TEXT NOT NULL,
                    Vin TEXT NOT NULL,
                    Model TEXT NOT NULL,
                    SpecCode TEXT,
                    ColorCode TEXT,
                    Price REAL NOT NULL,
                    PlateNo TEXT,
                    CusInvoiceNo TEXT,
                    CusInvoiceDate TEXT,
                    DeliveryStatus INTEGER NOT NULL,
                    DeliveryDate TEXT,
                    Remark TEXT,
                    FOREIGN KEY(DealId) REFERENCES RetailDeals(Id) ON DELETE CASCADE
                );

                CREATE TABLE IF NOT EXISTS RetailDealAttachments (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    DealId INTEGER NOT NULL,
                    DealNo TEXT NOT NULL,
                    FileType TEXT NOT NULL,
                    FileName TEXT NOT NULL,
                    FilePath TEXT NOT NULL,
                    Remark TEXT,
                    UploadedAt TEXT NOT NULL,
                    UploadedBy TEXT,
                    FOREIGN KEY(DealId) REFERENCES RetailDeals(Id) ON DELETE CASCADE
                );

                CREATE TABLE IF NOT EXISTS StorageRearranges (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    OrgId TEXT NOT NULL,
                    RearrangeNo TEXT NOT NULL,
                    StorageCodeFrom TEXT NOT NULL,
                    StorageNameFrom TEXT NOT NULL,
                    StorageCodeTo TEXT NOT NULL,
                    StorageNameTo TEXT NOT NULL,
                    RearrangeType INTEGER NOT NULL,
                    Status INTEGER NOT NULL,
                    TotalCars INTEGER NOT NULL,
                    TransporterName TEXT,
                    PlateNo TEXT,
                    DriverName TEXT,
                    DriverPhone TEXT,
                    Remark TEXT,
                    RejectReason TEXT,
                    CancelReason TEXT,
                    CreatedBy TEXT,
                    CreatedAt TEXT NOT NULL,
                    Approved1By TEXT,
                    Approved1At TEXT,
                    Approved2By TEXT,
                    Approved2At TEXT,
                    CancelledBy TEXT,
                    CancelledAt TEXT
                );
                CREATE UNIQUE INDEX IF NOT EXISTS IX_StorageRearranges_OrgId_RearrangeNo ON StorageRearranges(OrgId, RearrangeNo);

                CREATE TABLE IF NOT EXISTS StorageRearrangeDetails (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    RearrangeId INTEGER NOT NULL,
                    RearrangeNo TEXT NOT NULL,
                    CarId TEXT NOT NULL,
                    Vin TEXT NOT NULL,
                    Model TEXT NOT NULL,
                    SpecCode TEXT,
                    ColorCode TEXT,
                    EngineNo TEXT,
                    StorageCodeFrom TEXT NOT NULL,
                    StorageCodeTo TEXT NOT NULL,
                    ExpectedStartDate TEXT,
                    ExpectedEndDate TEXT,
                    ActualOutDate TEXT,
                    ActualInDate TEXT,
                    Status INTEGER NOT NULL,
                    ConfirmDate TEXT,
                    ConfirmBy TEXT,
                    Remark TEXT,
                    FOREIGN KEY(RearrangeId) REFERENCES StorageRearranges(Id) ON DELETE CASCADE
                );
            ");
        }
        catch
        {
            // Bỏ qua nếu DB là Postgres hoặc đã có bảng
        }

        var orgId = TenantContext.DefaultOrgId;
        if (!await db.Orgs.AnyAsync(o => o.Id == orgId))
            db.Orgs.Add(new Org { Id = orgId, Name = "Demo Sales", ApiKey = "demo-sales" });
        await db.SaveChangesAsync();

        if (!await db.Orders.AnyAsync(o => o.OrgId == orgId))
        {
            var o1 = new SalesOrder
            {
                OrgId = orgId,
                Code = "SO2603010001",
                CustomerName = "Công ty CP Vận tải An Phát",
                Phone = "0988123456",
                Vin = "KMHE281BBSA129841",
                Model = "Santa Fe 2.5 HTRAC",
                DealerCode = "VN001",
                ListPrice = 1350000000m,
                Discount = 30000000m,
                NetPrice = 1320000000m,
                Paid = 1320000000m,
                Status = SalesStatus.Paid,
                CreatedAt = DateTime.Now.AddDays(-5)
            };

            var o2 = new SalesOrder
            {
                OrgId = orgId,
                Code = "SO2602150002",
                CustomerName = "Nguyễn Văn Hùng",
                Phone = "0912345678",
                Vin = "KMHE281BBSA987654",
                Model = "Tucson 2.0 AT",
                DealerCode = "VN002",
                ListPrice = 950000000m,
                Discount = 10000000m,
                NetPrice = 940000000m,
                Paid = 940000000m,
                Status = SalesStatus.Delivered,
                CreatedAt = DateTime.Now.AddDays(-15),
                DeliveredAt = DateTime.Now.AddDays(-10)
            };

            var o3 = new SalesOrder
            {
                OrgId = orgId,
                Code = "SO2603200003",
                CustomerName = "Lê Thị Mai",
                Phone = "0909876543",
                Vin = "KMHE281BBSA334455",
                Model = "Creta 1.5 Cao Cấp",
                DealerCode = "VN001",
                ListPrice = 740000000m,
                Discount = 20000000m,
                NetPrice = 720000000m,
                Paid = 50000000m,
                Status = SalesStatus.Deposited,
                CreatedAt = DateTime.Now.AddDays(-2)
            };

            db.Orders.AddRange(o1, o2, o3);
            await db.SaveChangesAsync();

            // Seed Payments
            db.Payments.AddRange(
                new Payment { OrgId = orgId, OrderId = o1.Id, Kind = "Payment", Amount = 1320000000m, RefNo = "PAY-SF-001", At = DateTime.Now.AddDays(-4) },
                new Payment { OrgId = orgId, OrderId = o2.Id, Kind = "Payment", Amount = 940000000m, RefNo = "PAY-TU-002", At = DateTime.Now.AddDays(-12) },
                new Payment { OrgId = orgId, OrderId = o3.Id, Kind = "Deposit", Amount = 50000000m, RefNo = "DEP-CR-003", At = DateTime.Now.AddDays(-2) }
            );

            // Seed DeliveryOrders
            var do1 = new DeliveryOrder
            {
                OrgId = orgId,
                DeliveryOrderNo = "DO2603010001",
                OrderId = o1.Id,
                OrderCode = o1.Code,
                DealerCode = o1.DealerCode,
                CustomerName = o1.CustomerName,
                Phone = o1.Phone,
                Vin = o1.Vin,
                Model = o1.Model,
                RecipientName = "Trần Văn An (Tài xế nhận xe)",
                RecipientPhone = "0912888999",
                DeliveryAddress = "Kho vận Hyundai Thành Công, KCN Gián Khẩu, Ninh Bình",
                TransporterName = "Đội xe Vận tải Nam Trung",
                PlateNo = "29C-888.99",
                DeliveryExpectedDate = DateTime.Now.AddDays(1),
                Status = DeliveryOrderStatus.Approved,
                Remark = "Xe đã kiểm tra PDI đạt tiêu chuẩn xuất kho",
                CreatedAt = DateTime.Now.AddDays(-3),
                ApprovedAt = DateTime.Now.AddDays(-2)
            };

            var do2 = new DeliveryOrder
            {
                OrgId = orgId,
                DeliveryOrderNo = "DO2602150002",
                OrderId = o2.Id,
                OrderCode = o2.Code,
                DealerCode = o2.DealerCode,
                CustomerName = o2.CustomerName,
                Phone = o2.Phone,
                Vin = o2.Vin,
                Model = o2.Model,
                RecipientName = "Nguyễn Văn Hùng",
                RecipientPhone = "0912345678",
                DeliveryAddress = "Đại lý Hyundai Đông Đô, Cầu Giấy, Hà Nội",
                TransporterName = "Xe cứu hộ / Chuyên dùng HTC",
                PlateNo = "30H-123.45",
                Status = DeliveryOrderStatus.Delivered,
                HandoverNote = "Đã bàn giao xe kèm 2 chìa smartkey, sổ bảo hành và biên bản kiểm tra đạt chuẩn.",
                CreatedAt = DateTime.Now.AddDays(-12),
                ApprovedAt = DateTime.Now.AddDays(-11),
                DeliveredAt = o2.DeliveredAt
            };

            db.DeliveryOrders.AddRange(do1, do2);
            await db.SaveChangesAsync();
        }

        if (!await db.DealerOrders.AnyAsync(o => o.OrgId == orgId))
        {
            var ord1 = new DealerOrder
            {
                OrgId = orgId,
                OrderNo = "ORD2603010001",
                DealerCode = "VN001",
                DealerName = "Hyundai Đông Đô",
                OrderType = DealerOrderType.Plan,
                OrderMonth = "2026-03",
                SalesPolicyCode = "CSC",
                Status = DealerOrderStatus.Approved2,
                TotalQuantity = 8,
                TotalAmount = 9450000000m,
                Remark = "Đơn đặt xe kế hoạch định kỳ tháng 3/2026 - Đại lý phân phối khu vực Miền Bắc",
                CreatedAt = DateTime.Now.AddDays(-10),
                Approved1At = DateTime.Now.AddDays(-8),
                Approved2At = DateTime.Now.AddDays(-6),
                Items = new List<DealerOrderItem>
                {
                    new DealerOrderItem
                    {
                        Model = "Santa Fe 2.5 HTRAC",
                        SpecCode = "SF25-PRE",
                        ColorCode = "Trắng",
                        RequestedQuantity = 5,
                        ApprovedQuantity = 5,
                        UnitPrice = 1350000000m,
                        TotalAmount = 6750000000m,
                        Remark = "Bản Premium nội thất nâu"
                    },
                    new DealerOrderItem
                    {
                        Model = "Tucson 2.0 AT",
                        SpecCode = "TU20-STD",
                        ColorCode = "Đen",
                        RequestedQuantity = 3,
                        ApprovedQuantity = 3,
                        UnitPrice = 900000000m,
                        TotalAmount = 2700000000m,
                        Remark = "Bản xăng tiêu chuẩn"
                    }
                }
            };

            var ord2 = new DealerOrder
            {
                OrgId = orgId,
                OrderNo = "ORD2603150002",
                DealerCode = "VN002",
                DealerName = "Hyundai Nam Trung",
                OrderType = DealerOrderType.Unplan,
                OrderMonth = "2026-03",
                SalesPolicyCode = "STANDARD",
                Status = DealerOrderStatus.Pending,
                TotalQuantity = 2,
                TotalAmount = 1480000000m,
                Remark = "Đơn đặt bổ sung xe Creta phục vụ sự kiện lái thử",
                CreatedAt = DateTime.Now.AddDays(-1),
                Items = new List<DealerOrderItem>
                {
                    new DealerOrderItem
                    {
                        Model = "Creta 1.5 Cao Cấp",
                        SpecCode = "CR15-PRE",
                        ColorCode = "Đỏ",
                        RequestedQuantity = 2,
                        ApprovedQuantity = 0,
                        UnitPrice = 740000000m,
                        TotalAmount = 1480000000m,
                        Remark = "Xe trưng bày showroom"
                    }
                }
            };

            db.DealerOrders.AddRange(ord1, ord2);
            await db.SaveChangesAsync();
        }

        if (!await db.PaymentGuarantees.AnyAsync(o => o.OrgId == orgId))
        {
            var grt1 = new PaymentGuarantee
            {
                OrgId = orgId,
                GuaranteeNo = "GRT2603010001",
                BankGuaranteeNo = "BG2603-VCB-00128",
                BankCode = "VCB",
                BankName = "Vietcombank Sở Giao Dịch",
                DealerCode = "VN001",
                DealerName = "Hyundai Đông Đô",
                GuaranteeType = GuaranteeType.BL,
                Status = GuaranteeStatus.Approved,
                TotalAmount = 10000000000m,
                AllocatedAmount = 2250000000m,
                Term = 12,
                DateOpen = DateTime.Today.AddDays(-15),
                DateExpired = DateTime.Today.AddDays(350),
                Fee = 15000000m,
                BankCodeMonitor = "VCB",
                DateRecieveGrtRoot = DateTime.Today.AddDays(-14),
                Remark = "Bảo lãnh thanh toán mua xe theo hạn mức năm 2026",
                CreatedAt = DateTime.Now.AddDays(-15),
                ApprovedAt = DateTime.Now.AddDays(-14),
                Details = new List<PaymentGuaranteeDetail>
                {
                    new PaymentGuaranteeDetail
                    {
                        Vin = "KMHE281BBSA129841",
                        Model = "Santa Fe 2.5 HTRAC",
                        OrderNo = "ORD2603010001",
                        GuaranteeValue = 1350000000m,
                        NumberOfDaysDeferredPayment = 30,
                        DateStart = DateTime.Today.AddDays(-14),
                        DateEnd = DateTime.Today.AddDays(16),
                        Remark = "Bảo lãnh lô xe Santa Fe hợp đồng SO2603010001"
                    },
                    new PaymentGuaranteeDetail
                    {
                        Vin = "KMHE281BBSA987654",
                        Model = "Tucson 2.0 AT",
                        OrderNo = "ORD2603010001",
                        GuaranteeValue = 900000000m,
                        NumberOfDaysDeferredPayment = 45,
                        DateStart = DateTime.Today.AddDays(-14),
                        DateEnd = DateTime.Today.AddDays(31),
                        Remark = "Bảo lãnh lô xe Tucson hợp đồng SO2602150002"
                    }
                }
            };

            var grt2 = new PaymentGuarantee
            {
                OrgId = orgId,
                GuaranteeNo = "GRT2603150002",
                BankGuaranteeNo = "LC2603-BIDV-88910",
                BankCode = "BIDV",
                BankName = "BIDV Thăng Long",
                DealerCode = "VN002",
                DealerName = "Hyundai Nam Trung",
                GuaranteeType = GuaranteeType.LC,
                Status = GuaranteeStatus.Pending,
                TotalAmount = 5000000000m,
                AllocatedAmount = 0m,
                Term = 6,
                DateOpen = DateTime.Today.AddDays(-2),
                DateExpired = DateTime.Today.AddMonths(6),
                Fee = 7500000m,
                Remark = "Thư tín dụng (LC) nhập bổ sung xe đợt lái thử tháng 3/2026",
                CreatedAt = DateTime.Now.AddDays(-2)
            };

            db.PaymentGuarantees.AddRange(grt1, grt2);
            await db.SaveChangesAsync();
        }

        if (!await db.CarDocRequests.AnyAsync(o => o.OrgId == orgId))
        {
            var dr1 = new CarDocRequest
            {
                OrgId = orgId,
                DRListCode = "DNGT2603010001",
                DealerCode = "VN001",
                DealerName = "Hyundai Đông Đô",
                RequestType = DocReqType.Dealer,
                Status = DocReqStatus.Approved2,
                LetterRepresentationNo = "CV-2026/03/HĐĐ-01",
                LetterRepresentationDate = DateTime.Today.AddDays(-10),
                LoanSupportDay = 0,
                TotalCars = 1,
                Remark = "Đề nghị rút bộ hồ sơ gốc phục vụ đăng ký xe lăn bánh cho khách hàng doanh nghiệp",
                CreatedAt = DateTime.Now.AddDays(-5),
                Approved1At = DateTime.Now.AddDays(-4),
                Approved2At = DateTime.Now.AddDays(-2),
                Details = new List<CarDocRequestDetail>
                {
                    new CarDocRequestDetail
                    {
                        Vin = "KMHE281BBSA129841",
                        Model = "Santa Fe 2.5 HTRAC",
                        EngineNo = "G4KP-102941",
                        OrderNo = "ORD2603010001",
                        DeliveryOrderNo = "DO2603010001",
                        BankCode = "VCB",
                        BankGuaranteeNo = "BG2603-VCB-00128",
                        DocumentsStatus = "Full",
                        ExpectedDate = DateTime.Today.AddDays(-2),
                        HandoverDate = DateTime.Today.AddDays(-2),
                        RecipientName = "Trần Văn An (Đại diện đại lý)",
                        RecipientPhone = "0912888999",
                        Remark = "Đã bàn giao đầy đủ Hóa đơn GTGT số 0001234, CQ số 88921/ĐK và Tờ khai HQ"
                    }
                }
            };

            var dr2 = new CarDocRequest
            {
                OrgId = orgId,
                DRListCode = "DNGT2603150002",
                DealerCode = "VN002",
                DealerName = "Hyundai Nam Trung",
                RequestType = DocReqType.Normal,
                Status = DocReqStatus.Pending,
                LetterRepresentationNo = "CV-2026/03/HNT-05",
                LetterRepresentationDate = DateTime.Today.AddDays(-2),
                LoanSupportDay = 30,
                TotalCars = 1,
                Remark = "Đề nghị giao hồ sơ xe Tucson theo hạn mức bảo lãnh ngân hàng hỗ trợ vốn lưu động",
                CreatedAt = DateTime.Now.AddDays(-1),
                Details = new List<CarDocRequestDetail>
                {
                    new CarDocRequestDetail
                    {
                        Vin = "KMHE281BBSA987654",
                        Model = "Tucson 2.0 AT",
                        EngineNo = "G4NL-983102",
                        OrderNo = "ORD2603010001",
                        DeliveryOrderNo = "DO2602150002",
                        BankCode = "BIDV",
                        BankGuaranteeNo = "LC2603-BIDV-88910",
                        DocumentsStatus = "Full",
                        ExpectedDate = DateTime.Today.AddDays(3),
                        RecipientName = "Nguyễn Văn Hùng",
                        RecipientPhone = "0912345678",
                        Remark = "Hồ sơ chờ NPP duyệt danh sách xuất chứng nhận chất lượng"
                    }
                }
            };

            db.CarDocRequests.AddRange(dr1, dr2);
            await db.SaveChangesAsync();
        }

        if (!await db.CarInvoices.AnyAsync(o => o.OrgId == orgId))
        {
            var inv1 = new CarInvoice
            {
                OrgId = orgId,
                InvoiceCode = "INV2603010001",
                InvoiceNo = "0001234",
                InvoiceSerial = "1C26TBB",
                LookupCode = "A7K9M2P4X1",
                DealerCode = "VN001",
                DealerName = "Hyundai Đông Đô",
                Issuer = "HTC",
                BankCode = "VCB",
                BankName = "Vietcombank Thăng Long",
                InvoiceType = InvoiceType.Root,
                AdjType = InvoiceAdjType.Normal,
                Status = InvoiceStatus.Issued,
                VatRate = 10m,
                SubTotal = 1910000000m,
                VatAmount = 191000000m,
                TotalAmount = 2101000000m,
                TotalCars = 2,
                BuyerTaxCode = "0102345678",
                BuyerAddress = "98 Nguyễn Trãi, Thanh Xuân, Hà Nội",
                Remark = "Hóa đơn VAT bán buôn lô 02 xe Santa Fe & Creta xuất theo bảo lãnh VCB",
                InvoiceDate = DateTime.Today.AddDays(-5),
                CreatedAt = DateTime.Now.AddDays(-5),
                IssuedAt = DateTime.Now.AddDays(-5),
                Details = new List<CarInvoiceDetail>
                {
                    new CarInvoiceDetail
                    {
                        Vin = "KMHE281BBSA129841",
                        Model = "Santa Fe 2.5 HTRAC",
                        SpecCode = "SF25-PRE-01",
                        ColorCode = "WW2",
                        EngineNo = "G4KP-102941",
                        UnitPrice = 1250000000m,
                        VatRate = 10m,
                        VatAmount = 125000000m,
                        TotalAmount = 1375000000m,
                        OrderNo = "ORD2603010001",
                        DeliveryOrderNo = "DO2603010001",
                        Remark = "Xe giao đợt 1 tháng 3/2026"
                    },
                    new CarInvoiceDetail
                    {
                        Vin = "KMHE281BBSA334455",
                        Model = "Creta 1.5 Cao Cấp",
                        SpecCode = "CR15-PRE-02",
                        ColorCode = "R3R",
                        EngineNo = "G4FL-334411",
                        UnitPrice = 660000000m,
                        VatRate = 10m,
                        VatAmount = 66000000m,
                        TotalAmount = 726000000m,
                        OrderNo = "ORD2603010001",
                        DeliveryOrderNo = "DO2603200003",
                        Remark = "Xe giao đợt 2 tháng 3/2026"
                    }
                }
            };

            var inv2 = new CarInvoice
            {
                OrgId = orgId,
                InvoiceCode = "INV2603150002",
                InvoiceNo = null,
                InvoiceSerial = "1C26TBB",
                LookupCode = null,
                DealerCode = "VN002",
                DealerName = "Hyundai Nam Trung",
                Issuer = "HTC",
                BankCode = "BIDV",
                BankName = "BIDV Thăng Long",
                InvoiceType = InvoiceType.Root,
                AdjType = InvoiceAdjType.Normal,
                Status = InvoiceStatus.Draft,
                VatRate = 10m,
                SubTotal = 860000000m,
                VatAmount = 86000000m,
                TotalAmount = 946000000m,
                TotalCars = 1,
                BuyerTaxCode = "0109876543",
                BuyerAddress = "45 Lê Văn Lương, Cầu Giấy, Hà Nội",
                Remark = "Hóa đơn VAT nháp chờ đại lý xác nhận hoàn tất hồ sơ bảo lãnh thanh toán BIDV",
                InvoiceDate = DateTime.Today.AddDays(-1),
                CreatedAt = DateTime.Now.AddDays(-1),
                Details = new List<CarInvoiceDetail>
                {
                    new CarInvoiceDetail
                    {
                        Vin = "KMHE281BBSA987654",
                        Model = "Tucson 2.0 AT",
                        SpecCode = "TU20-STD-01",
                        ColorCode = "NKA",
                        EngineNo = "G4NL-983102",
                        UnitPrice = 860000000m,
                        VatRate = 10m,
                        VatAmount = 86000000m,
                        TotalAmount = 946000000m,
                        OrderNo = "ORD2603010001",
                        DeliveryOrderNo = "DO2602150002",
                        Remark = "Hóa đơn nháp chờ phát hành"
                    }
                }
            };

            db.CarInvoices.AddRange(inv1, inv2);
            await db.SaveChangesAsync();
        }

        if (!await db.CarRetrieveOrders.AnyAsync(o => o.OrgId == orgId))
        {
            var ro1 = new CarRetrieveOrder
            {
                OrgId = orgId,
                RetrieveOrderNo = "RO2603100001",
                DealerCode = "VN002",
                DealerName = "Hyundai Nam Trung",
                Status = CarRetrieveStatus.Completed,
                StorageCode = "OTHER",
                StorageName = "Kho Trung Tâm Phân Phối Hà Nam",
                TotalCars = 1,
                Remark = "Thu hồi xe Creta đổi lô điều phối phục vụ đại lý khu vực miền Bắc",
                CreatedBy = "NPP_DISPATCHER",
                CreatedAt = DateTime.Now.AddDays(-10),
                ApprovedBy = "NPP_HQ_DIRECTOR",
                ApprovedAt = DateTime.Now.AddDays(-9),
                CompletedAt = DateTime.Now.AddDays(-6),
                Details = new List<CarRetrieveOrderDetail>
                {
                    new CarRetrieveOrderDetail
                    {
                        CarId = "CAR2026-CR0192",
                        Vin = "KMHE281BBSA556677",
                        Model = "Creta 1.5 Cao Cấp",
                        DeliveryOrderNo = "DO2602150002",
                        StorageCode = "OTHER",
                        Status = CarRetrieveDtlStatus.Completed,
                        ExpectedStartDate = DateTime.Today.AddDays(-8),
                        ExpectedEndDate = DateTime.Today.AddDays(-5),
                        ActualOutDate = DateTime.Today.AddDays(-7),
                        ActualEndDate = DateTime.Today.AddDays(-6),
                        EngineNo = "G4FL-881920",
                        Color = "Trắng",
                        Remark = "Xe đã kiểm tra nguyên trạng bàn giao về kho an toàn"
                    }
                }
            };

            var ro2 = new CarRetrieveOrder
            {
                OrgId = orgId,
                RetrieveOrderNo = "RO2603200002",
                DealerCode = "VN001",
                DealerName = "Hyundai Đông Đô",
                Status = CarRetrieveStatus.Pending,
                StorageCode = "OTHER",
                StorageName = "Kho Trung Tâm Phân Phối Hà Nam",
                TotalCars = 1,
                Remark = "Thu hồi xe Santa Fe do đại lý quá hạn giải ngân bảo lãnh ngân hàng",
                CreatedBy = "NPP_FINANCE",
                CreatedAt = DateTime.Now.AddDays(-1),
                Details = new List<CarRetrieveOrderDetail>
                {
                    new CarRetrieveOrderDetail
                    {
                        CarId = "CAR2026-SF0988",
                        Vin = "KMHE281BBSA987654",
                        Model = "Tucson 2.0 AT",
                        DeliveryOrderNo = "DO2603010001",
                        StorageCode = "OTHER",
                        Status = CarRetrieveDtlStatus.Pending,
                        ExpectedStartDate = DateTime.Today.AddDays(1),
                        ExpectedEndDate = DateTime.Today.AddDays(5),
                        EngineNo = "G4NL-983102",
                        Color = "Đen",
                        Remark = "Chờ ban lãnh đạo NPP duyệt lệnh thu hồi vận chuyển"
                    }
                }
            };

            db.CarRetrieveOrders.AddRange(ro1, ro2);
            await db.SaveChangesAsync();
        }

        if (!await db.ContractCancels.AnyAsync(o => o.OrgId == orgId))
        {
            var cc1 = new ContractCancelOrder
            {
                OrgId = orgId,
                ContractCNo = "CCN2603010001",
                DealerCode = "VN002",
                DealerName = "Hyundai Nam Trung",
                Status = ContractCancelStatus.Approved,
                TotalCars = 1,
                Remark = "Đề nghị hủy xe Accent do khách hàng không đủ điều kiện giải ngân ngân hàng",
                CreatedBy = "DEALER_SALESMAN",
                CreatedAt = DateTime.Now.AddDays(-6),
                ApprovedBy = "NPP_HQ_DIRECTOR",
                ApprovedAt = DateTime.Now.AddDays(-5),
                Cars = new List<ContractCancelCar>
                {
                    new ContractCancelCar
                    {
                        OrderCode = "SO2602150099",
                        Vin = "KMHE281BBSA112233",
                        Model = "Accent 1.4 AT",
                        SpecCode = "AC14-AT-01",
                        ColorCode = "WH1",
                        CtrCTDNo = "RC4.LOANFAIL",
                        CtrCTDName = "Ngân hàng từ chối cho vay trả góp / hồ sơ tín dụng không đạt",
                        CtrCType = "RC4",
                        Remark = "Hồ sơ tín dụng ngân hàng VIB từ chối giải ngân"
                    }
                },
                Details = new List<ContractCancelDtl>
                {
                    new ContractCancelDtl
                    {
                        OrderCode = "SO2602150099",
                        Model = "Accent 1.4 AT",
                        SpecCode = "AC14-AT-01",
                        ColorCode = "WH1",
                        Qty = 1,
                        Remark = "Gom tự động từ 1 xe đề nghị hủy"
                    }
                }
            };

            var cc2 = new ContractCancelOrder
            {
                OrgId = orgId,
                ContractCNo = "CCN2603200002",
                DealerCode = "VN001",
                DealerName = "Hyundai Đông Đô",
                Status = ContractCancelStatus.Pending,
                TotalCars = 1,
                Remark = "Khách hàng tạm dừng mua xe do có kế hoạch tài chính cá nhân đột xuất",
                CreatedBy = "DEALER_SALESMAN",
                CreatedAt = DateTime.Now.AddDays(-1),
                Cars = new List<ContractCancelCar>
                {
                    new ContractCancelCar
                    {
                        OrderCode = "SO2603200003",
                        Vin = "KMHE281BBSA334455",
                        Model = "Creta 1.5 Cao Cấp",
                        SpecCode = "CR15-PRE-02",
                        ColorCode = "R3R",
                        CtrCTDNo = "RC3.STOPBUY",
                        CtrCTDName = "Khách hàng tạm dừng mua xe / đổi kế hoạch tài chính cá nhân",
                        CtrCType = "RC3",
                        Remark = "Khách hàng xin rút lại đặt cọc hoặc chuyển sang quý 4/2026"
                    }
                },
                Details = new List<ContractCancelDtl>
                {
                    new ContractCancelDtl
                    {
                        OrderCode = "SO2603200003",
                        Model = "Creta 1.5 Cao Cấp",
                        SpecCode = "CR15-PRE-02",
                        ColorCode = "R3R",
                        Qty = 1,
                        Remark = "Gom tự động từ 1 xe đề nghị hủy"
                    }
                }
            };

            db.ContractCancels.AddRange(cc1, cc2);
            await db.SaveChangesAsync();
        }

        if (!await db.TransportMinutes.AnyAsync(o => o.OrgId == orgId))
        {
            var tm1 = new CarTransportMinutes
            {
                OrgId = orgId,
                TransportMinutesNo = "BBBG2603010001",
                DealerCode = "VN001",
                DealerName = "Hyundai Đông Đô",
                TransporterName = "Đội xe Vận tải Nam Trung",
                PlateNo = "29C-888.99",
                DriverName = "Trần Văn An",
                DriverPhone = "0912888999",
                BankCode = "VCB",
                BankName = "Vietcombank Thăng Long",
                TransportMinutesDate = DateTime.Today.AddDays(-2),
                Status = TransportMinutesStatus.Completed,
                DLSignStatus = DLSignStatus.Approved,
                HTCSignStatus = HTCSignStatus.Approved,
                TotalCars = 1,
                DLCreatedBy = "DEALER_STORE",
                DLCreatedAt = DateTime.Now.AddDays(-3),
                DLApprBy = "DEALER_MANAGER",
                DLApprAt = DateTime.Now.AddDays(-2),
                HTCApprBy = "NPP_DISPATCH_LEAD",
                HTCApprAt = DateTime.Now.AddDays(-2),
                FilePath = "/storage/docs/esign/BBBG2603010001_signed.pdf",
                Remark = "Biên bản bàn giao xe Santa Fe đợt 1 tháng 3/2026, xe mới 100% đầy đủ hồ sơ pháp lý",
                Cars = new List<CarTransportMinutesDetail>
                {
                    new CarTransportMinutesDetail
                    {
                        CarId = "CAR2026-SF0988",
                        Vin = "KMHE281BBSA129841",
                        Model = "Santa Fe 2.5 HTRAC",
                        SpecCode = "SF25-PRE-01",
                        ColorCode = "WW2",
                        EngineNo = "G4KP-102941",
                        DeliveryOrderNo = "DO2603010001",
                        OrderNo = "SO2603010001",
                        GuaranteeNo = "BG2603-VCB-00128",
                        Odometer = 12,
                        ConditionNote = "Xe nguyên vẹn, sơn đẹp không trầy xước, 2 chìa smartkey, lốp dự phòng, sổ bảo hành",
                        Status = TransportMinutesCarStatus.Approved,
                        Remark = "Đã bàn giao và ký nhận trực tiếp tại bãi xe đại lý"
                    }
                }
            };

            var tm2 = new CarTransportMinutes
            {
                OrgId = orgId,
                TransportMinutesNo = "BBBG2603150002",
                DealerCode = "VN002",
                DealerName = "Hyundai Nam Trung",
                TransporterName = "Xe cứu hộ / Chuyên dùng HTC",
                PlateNo = "30H-123.45",
                DriverName = "Lê Hoàng Quân",
                DriverPhone = "0987654321",
                BankCode = "BIDV",
                BankName = "BIDV Thăng Long",
                TransportMinutesDate = DateTime.Today.AddDays(-1),
                Status = TransportMinutesStatus.DealerApproved,
                DLSignStatus = DLSignStatus.Approved,
                HTCSignStatus = HTCSignStatus.Pending,
                TotalCars = 1,
                DLCreatedBy = "DEALER_STORE",
                DLCreatedAt = DateTime.Now.AddDays(-2),
                DLApprBy = "DEALER_DIRECTOR",
                DLApprAt = DateTime.Now.AddDays(-1),
                Remark = "Đại lý đã kiểm tra xe Tucson và ký xác nhận nhận xe, chờ điều phối NPP ký đóng biên bản",
                Cars = new List<CarTransportMinutesDetail>
                {
                    new CarTransportMinutesDetail
                    {
                        CarId = "CAR2026-TU1102",
                        Vin = "KMHE281BBSA987654",
                        Model = "Tucson 2.0 AT",
                        SpecCode = "TU20-STD-01",
                        ColorCode = "NKA",
                        EngineNo = "G4NL-983102",
                        DeliveryOrderNo = "DO2602150002",
                        OrderNo = "SO2602150002",
                        GuaranteeNo = "LC2603-BIDV-88910",
                        Odometer = 8,
                        ConditionNote = "Xe mới nguyên bản, phụ kiện theo xe đầy đủ tiêu chuẩn HTC",
                        Status = TransportMinutesCarStatus.Approved,
                        Remark = "Đại lý đã tiếp nhận xe tại kho Đông Đô"
                    }
                }
            };

            var tm3 = new CarTransportMinutes
            {
                OrgId = orgId,
                TransportMinutesNo = "BBBG2603200003",
                DealerCode = "VN001",
                DealerName = "Hyundai Đông Đô",
                TransporterName = "Đội xe Vận tải Nam Trung",
                PlateNo = "29C-888.99",
                DriverName = "Trần Văn An",
                DriverPhone = "0912888999",
                BankCode = "DEALER",
                TransportMinutesDate = DateTime.Today,
                Status = TransportMinutesStatus.Pending,
                DLSignStatus = DLSignStatus.Pending,
                HTCSignStatus = HTCSignStatus.Pending,
                TotalCars = 1,
                DLCreatedBy = "NPP_DISPATCHER",
                DLCreatedAt = DateTime.Now,
                Remark = "Biên bản bàn giao xe Creta đang vận chuyển trên đường tới đại lý",
                Cars = new List<CarTransportMinutesDetail>
                {
                    new CarTransportMinutesDetail
                    {
                        CarId = "CAR2026-CR0192",
                        Vin = "KMHE281BBSA334455",
                        Model = "Creta 1.5 Cao Cấp",
                        SpecCode = "CR15-PRE-02",
                        ColorCode = "R3R",
                        EngineNo = "G4FL-334411",
                        DeliveryOrderNo = "DO2603010001",
                        OrderNo = "SO2603200003",
                        Odometer = 15,
                        ConditionNote = "Xe xuất kho nguyên niêm phong",
                        Status = TransportMinutesCarStatus.Pending,
                        Remark = "Chờ đại lý kiểm tra nghiệm thu"
                    }
                }
            };

            db.TransportMinutes.AddRange(tm1, tm2, tm3);
            await db.SaveChangesAsync();
        }

        if (!await db.DealerContracts.AnyAsync(o => o.OrgId == orgId))
        {
            var ctr1 = new DealerContract
            {
                OrgId = orgId,
                ContractNo = "2603DRC00001",
                DealerCode = "VN001",
                DealerName = "Hyundai Đông Đô",
                ContractDate = DateTime.Today.AddDays(-10),
                ContractType = DealerContractType.Standard,
                PaymentType = DealerContractPaymentType.Guarantee,
                BankCode = "VCB",
                BankName = "Vietcombank Thăng Long",
                DepositPercent = 10m,
                GuaranteeDays = 15,
                TotalCars = 2,
                TotalAmount = 2266000000m,
                Status = DealerContractStatus.Signed,
                DealerSignStatus = DealerContractSignStatus.Signed,
                HQSignStatus = DealerContractSignStatus.Signed,
                DealerSignedBy = "Nguyễn Văn Hưng (Giám đốc Đại lý Đông Đô)",
                DealerSignedAt = DateTime.Now.AddDays(-9),
                HQSignedBy = "Phạm Quang Minh (Phó TGĐ Phân phối HTC)",
                HQSignedAt = DateTime.Now.AddDays(-8),
                Approved1At = DateTime.Now.AddDays(-9),
                SignedFilePath = "/storage/contracts/2603DRC00001_signed_full.pdf",
                Remark = "Hợp đồng mua bán lô 02 xe Santa Fe & Creta tháng 03/2026 thanh toán qua bảo lãnh Vietcombank",
                CreatedBy = "DEALER_SALES_ADMIN",
                CreatedAt = DateTime.Now.AddDays(-10),
                Details = new List<DealerContractDetail>
                {
                    new DealerContractDetail
                    {
                        CarId = "CAR2026-SF0988",
                        Vin = "KMHE281BBSA129841",
                        Model = "Santa Fe 2.5 HTRAC",
                        SpecCode = "SF25-PRE-01",
                        ColorCode = "WW2",
                        ProductionYear = 2026,
                        UnitPrice = 1320000000m,
                        VatRate = 10m,
                        TotalAmount = 1452000000m,
                        OrderNo = "ORD2603010001",
                        Remark = "Xe giao đợt 1 tháng 3 theo kế hoạch phân bổ"
                    },
                    new DealerContractDetail
                    {
                        CarId = "CAR2026-CR0192",
                        Vin = "KMHE281BBSA334455",
                        Model = "Creta 1.5 Cao Cấp",
                        SpecCode = "CR15-PRE-02",
                        ColorCode = "R3R",
                        ProductionYear = 2026,
                        UnitPrice = 740000000m,
                        VatRate = 10m,
                        TotalAmount = 814000000m,
                        OrderNo = "ORD2603010001",
                        Remark = "Xe giao đợt 2 theo thỏa thuận bổ sung"
                    }
                }
            };

            var ctr2 = new DealerContract
            {
                OrgId = orgId,
                ContractNo = "2603DRC00002",
                DealerCode = "VN002",
                DealerName = "Hyundai Nam Trung",
                ContractDate = DateTime.Today.AddDays(-3),
                ContractType = DealerContractType.Standard,
                PaymentType = DealerContractPaymentType.LC,
                BankCode = "BIDV",
                BankName = "BIDV Thăng Long",
                DepositPercent = 10m,
                GuaranteeDays = 30,
                TotalCars = 1,
                TotalAmount = 946000000m,
                Status = DealerContractStatus.PreliminaryApproved,
                DealerSignStatus = DealerContractSignStatus.Signed,
                HQSignStatus = DealerContractSignStatus.Pending,
                DealerSignedBy = "Lê Hồng Quân (Đại diện Nam Trung)",
                DealerSignedAt = DateTime.Now.AddDays(-2),
                Approved1At = DateTime.Now.AddDays(-2),
                SignedFilePath = "/storage/contracts/2603DRC00002_dlr_signed.pdf",
                Remark = "Đại lý đã ký số điện tử hợp đồng mua xe Tucson mở LC, chờ NPP ký duyệt cấp 2",
                CreatedBy = "DEALER_SALES_ADMIN",
                CreatedAt = DateTime.Now.AddDays(-3),
                Details = new List<DealerContractDetail>
                {
                    new DealerContractDetail
                    {
                        CarId = "CAR2026-TU1102",
                        Vin = "KMHE281BBSA987654",
                        Model = "Tucson 2.0 AT",
                        SpecCode = "TU20-STD-01",
                        ColorCode = "NKA",
                        ProductionYear = 2026,
                        UnitPrice = 860000000m,
                        VatRate = 10m,
                        TotalAmount = 946000000m,
                        OrderNo = "ORD2603150002",
                        Remark = "Xe lái thử và trưng bày showroom Nam Trung"
                    }
                }
            };

            var ctr3 = new DealerContract
            {
                OrgId = orgId,
                ContractNo = "2603DRC00003",
                DealerCode = "VN001",
                DealerName = "Hyundai Đông Đô",
                ContractDate = DateTime.Today,
                ContractType = DealerContractType.Standard,
                PaymentType = DealerContractPaymentType.Cash,
                BankCode = "DEALER",
                DepositPercent = 20m,
                GuaranteeDays = 0,
                TotalCars = 1,
                TotalAmount = 495000000m,
                Status = DealerContractStatus.Draft,
                DealerSignStatus = DealerContractSignStatus.Pending,
                HQSignStatus = DealerContractSignStatus.Pending,
                Remark = "Hợp đồng nháp mua buôn 01 xe Accent 1.4 AT thanh toán tiền mặt trực tiếp",
                CreatedBy = "DEALER_SALES_ADMIN",
                CreatedAt = DateTime.Now,
                Details = new List<DealerContractDetail>
                {
                    new DealerContractDetail
                    {
                        CarId = "CAR2026-AC9011",
                        Vin = "KMHE281BBSA667788",
                        Model = "Accent 1.4 AT",
                        SpecCode = "AC14-AT-01",
                        ColorCode = "WH1",
                        ProductionYear = 2026,
                        UnitPrice = 450000000m,
                        VatRate = 10m,
                        TotalAmount = 495000000m,
                        Remark = "Hợp đồng chờ nộp tiền đặt cọc 20%"
                    }
                }
            };

            db.DealerContracts.AddRange(ctr1, ctr2, ctr3);
            await db.SaveChangesAsync();
        }

        if (!await db.RetailContracts.AnyAsync(o => o.OrgId == orgId))
        {
            var rc1 = new DlrRetailContract
            {
                OrgId = orgId,
                ContractNo = "RC26090001",
                ContractNoUser = "HDBL-2026/09-01",
                DealerCode = "VN001",
                DealerName = "Hyundai Đông Đô",
                CustomerCode = "CUS00128",
                CustomerName = "Nguyễn Thành Long",
                CustomerPhone = "0988123456",
                CustomerIdCardType = "CCCD",
                CustomerIdCardNo = "001095012345",
                CustomerDateOfBirth = new DateTime(1985, 6, 15),
                CustomerAddress = "18 Tam Trinh, Hoàng Mai, Hà Nội",
                CustomerType = RetailCustomerType.Individual,
                SalesType = "RETAIL",
                SMCode = "SM001",
                SMName = "Trần Tuấn Anh",
                ContractDate = DateTime.Today.AddDays(-5),
                PaymentType = RetailPaymentType.Installment,
                BankCode = "VCB",
                BankName = "Vietcombank Thăng Long",
                BankLoanAmount = 900000000m,
                DepositAmount = 100000000m,
                PaidAmount = 100000000m,
                TotalCars = 1,
                TotalAmount = 1320000000m,
                Status = RetailContractStatus.Approved,
                FlagDealFinish = false,
                VersionCount = 1,
                VersionDTimeCurr = DateTime.Now.AddDays(-5),
                ApprovedAt = DateTime.Now.AddDays(-4),
                ApprovedBy = "DEALER_MANAGER",
                Remark = "Hợp đồng bán lẻ xe Santa Fe trả góp qua Vietcombank, đã đặt cọc và duyệt hồ sơ vay",
                CreatedBy = "DEALER_USER",
                CreatedAt = DateTime.Now.AddDays(-5),
                Details = new List<DlrRetailContractDetail>
                {
                    new DlrRetailContractDetail
                    {
                        ContractNo = "RC26090001",
                        Model = "Santa Fe 2.5 HTRAC",
                        SpecCode = "SF25-PRE-01",
                        ColorCode = "WW2",
                        ProductionYear = 2026,
                        Qty = 1,
                        QtyDelivery = 0,
                        QtyCancel = 0,
                        UnitPrice = 1350000000m,
                        Discount = 30000000m,
                        TotalAmount = 1320000000m,
                        DlvExpectedDate = DateTime.Today.AddDays(10),
                        Remark = "Tặng dán phim cách nhiệt và thảm lót sàn cao cấp"
                    }
                },
                Cars = new List<DlrRetailContractCar>
                {
                    new DlrRetailContractCar
                    {
                        ContractNo = "RC26090001",
                        CtrCarId = "RC26090001.01",
                        Model = "Santa Fe 2.5 HTRAC",
                        SpecCode = "SF25-PRE-01",
                        ColorCode = "WW2",
                        Vin = "KMHE281BBSA129841",
                        Status = RetailContractCarStatus.Pending,
                        Remark = "Đã gán VIN chờ hoàn tất giải ngân từ Vietcombank"
                    }
                },
                Histories = new List<DlrRetailContractDtlHis>
                {
                    new DlrRetailContractDtlHis
                    {
                        ContractNo = "RC26090001",
                        VersionCount = 1,
                        VersionDTimeCurr = DateTime.Now.AddDays(-5),
                        Model = "Santa Fe 2.5 HTRAC",
                        SpecCode = "SF25-PRE-01",
                        ColorCode = "WW2",
                        Qty = 1,
                        UnitPrice = 1350000000m,
                        TotalAmount = 1320000000m,
                        DlvExpectedDate = DateTime.Today.AddDays(10),
                        Remark = "Phiên bản khởi tạo hợp đồng",
                        LoggedBy = "DEALER_USER",
                        LogDateTime = DateTime.Now.AddDays(-5)
                    }
                }
            };

            var rc2 = new DlrRetailContract
            {
                OrgId = orgId,
                ContractNo = "RC26090002",
                ContractNoUser = "HDBL-2026/09-02",
                DealerCode = "VN002",
                DealerName = "Hyundai Nam Trung",
                CustomerCode = "CUS00299",
                CustomerName = "Công ty TNHH Vận tải Du lịch Sao Mai",
                CustomerPhone = "0904888999",
                CustomerIdCardType = "MST",
                CustomerIdCardNo = "0108899881",
                CustomerAddress = "120 Nguyễn Xiển, Thanh Xuân, Hà Nội",
                CustomerType = RetailCustomerType.Corporate,
                TransactorCode = "TRAN01",
                TransactorName = "Nguyễn Văn Tuấn (Giám đốc)",
                TransactorPhone = "0904888999",
                SalesType = "FLEET",
                SMCode = "SM002",
                SMName = "Hoàng Thị Dung",
                ContractDate = DateTime.Today.AddDays(-1),
                PaymentType = RetailPaymentType.Cash,
                BankLoanAmount = 0m,
                DepositAmount = 200000000m,
                PaidAmount = 200000000m,
                TotalCars = 2,
                TotalAmount = 1700000000m,
                Status = RetailContractStatus.Pending,
                FlagDealFinish = false,
                VersionCount = 1,
                VersionDTimeCurr = DateTime.Now.AddDays(-1),
                Remark = "Hợp đồng mua theo lô 02 xe Custin cho doanh nghiệp, trả thẳng, chờ duyệt HĐ",
                CreatedBy = "DEALER_USER",
                CreatedAt = DateTime.Now.AddDays(-1),
                Details = new List<DlrRetailContractDetail>
                {
                    new DlrRetailContractDetail
                    {
                        ContractNo = "RC26090002",
                        Model = "Custin 1.5T-GDi Cao Cấp",
                        SpecCode = "CU15-PRE-01",
                        ColorCode = "SL1",
                        ProductionYear = 2026,
                        Qty = 2,
                        QtyDelivery = 0,
                        QtyCancel = 0,
                        UnitPrice = 860000000m,
                        Discount = 20000000m,
                        TotalAmount = 1700000000m,
                        DlvExpectedDate = DateTime.Today.AddDays(15),
                        Remark = "Chiết khấu bán theo lô doanh nghiệp"
                    }
                },
                Cars = new List<DlrRetailContractCar>
                {
                    new DlrRetailContractCar
                    {
                        ContractNo = "RC26090002",
                        CtrCarId = "RC26090002.01",
                        Model = "Custin 1.5T-GDi Cao Cấp",
                        SpecCode = "CU15-PRE-01",
                        ColorCode = "SL1",
                        Status = RetailContractCarStatus.Pending,
                        Remark = "Xe 1/2 lô doanh nghiệp"
                    },
                    new DlrRetailContractCar
                    {
                        ContractNo = "RC26090002",
                        CtrCarId = "RC26090002.02",
                        Model = "Custin 1.5T-GDi Cao Cấp",
                        SpecCode = "CU15-PRE-01",
                        ColorCode = "SL1",
                        Status = RetailContractCarStatus.Pending,
                        Remark = "Xe 2/2 lô doanh nghiệp"
                    }
                },
                Histories = new List<DlrRetailContractDtlHis>
                {
                    new DlrRetailContractDtlHis
                    {
                        ContractNo = "RC26090002",
                        VersionCount = 1,
                        VersionDTimeCurr = DateTime.Now.AddDays(-1),
                        Model = "Custin 1.5T-GDi Cao Cấp",
                        SpecCode = "CU15-PRE-01",
                        ColorCode = "SL1",
                        Qty = 2,
                        UnitPrice = 860000000m,
                        TotalAmount = 1700000000m,
                        DlvExpectedDate = DateTime.Today.AddDays(15),
                        Remark = "Khởi tạo hợp đồng bán lô",
                        LoggedBy = "DEALER_USER",
                        LogDateTime = DateTime.Now.AddDays(-1)
                    }
                }
            };

            var rc3 = new DlrRetailContract
            {
                OrgId = orgId,
                ContractNo = "RC26080003",
                ContractNoUser = "HDBL-2026/08-03",
                DealerCode = "VN002",
                DealerName = "Hyundai Nam Trung",
                CustomerCode = "CUS00311",
                CustomerName = "Phạm Hoàng Giang",
                CustomerPhone = "0912345678",
                CustomerIdCardType = "CCCD",
                CustomerIdCardNo = "031088001122",
                CustomerDateOfBirth = new DateTime(1988, 11, 20),
                CustomerAddress = "88 Lê Lợi, TP. Hải Phòng",
                CustomerType = RetailCustomerType.Individual,
                SalesType = "RETAIL",
                SMCode = "SM001",
                SMName = "Trần Tuấn Anh",
                ContractDate = DateTime.Today.AddDays(-20),
                PaymentType = RetailPaymentType.Cash,
                BankLoanAmount = 0m,
                DepositAmount = 50000000m,
                PaidAmount = 940000000m,
                TotalCars = 1,
                TotalAmount = 940000000m,
                Status = RetailContractStatus.Finished,
                FlagDealFinish = true,
                VersionCount = 1,
                VersionDTimeCurr = DateTime.Now.AddDays(-20),
                ApprovedAt = DateTime.Now.AddDays(-18),
                ApprovedBy = "DEALER_MANAGER",
                Remark = "Hợp đồng đã hoàn tất thanh toán đủ 100% và bàn giao xe cho khách hàng",
                CreatedBy = "DEALER_USER",
                CreatedAt = DateTime.Now.AddDays(-20),
                Details = new List<DlrRetailContractDetail>
                {
                    new DlrRetailContractDetail
                    {
                        ContractNo = "RC26080003",
                        Model = "Tucson 2.0 AT",
                        SpecCode = "TU20-STD-01",
                        ColorCode = "NKA",
                        ProductionYear = 2026,
                        Qty = 1,
                        QtyDelivery = 1,
                        QtyCancel = 0,
                        UnitPrice = 950000000m,
                        Discount = 10000000m,
                        TotalAmount = 940000000m,
                        DlvExpectedDate = DateTime.Today.AddDays(-10),
                        Remark = "Đã giao xe kèm bảo hiểm vật chất 1 năm"
                    }
                },
                Cars = new List<DlrRetailContractCar>
                {
                    new DlrRetailContractCar
                    {
                        ContractNo = "RC26080003",
                        CtrCarId = "RC26080003.01",
                        Model = "Tucson 2.0 AT",
                        SpecCode = "TU20-STD-01",
                        ColorCode = "NKA",
                        Vin = "KMHE281BBSA987654",
                        Status = RetailContractCarStatus.Delivered,
                        DeliveryDate = DateTime.Now.AddDays(-10),
                        Remark = "Khách hàng đã ký biên bản bàn giao xe và nhận đủ giấy tờ"
                    }
                },
                Histories = new List<DlrRetailContractDtlHis>
                {
                    new DlrRetailContractDtlHis
                    {
                        ContractNo = "RC26080003",
                        VersionCount = 1,
                        VersionDTimeCurr = DateTime.Now.AddDays(-20),
                        Model = "Tucson 2.0 AT",
                        SpecCode = "TU20-STD-01",
                        ColorCode = "NKA",
                        Qty = 1,
                        UnitPrice = 950000000m,
                        TotalAmount = 940000000m,
                        DlvExpectedDate = DateTime.Today.AddDays(-10),
                        Remark = "Khởi tạo hợp đồng",
                        LoggedBy = "DEALER_USER",
                        LogDateTime = DateTime.Now.AddDays(-20)
                    }
                }
            };

            db.RetailContracts.AddRange(rc1, rc2, rc3);
            await db.SaveChangesAsync();
        }

        if (!await db.DealerPayments.AnyAsync(o => o.OrgId == orgId))
        {
            var pmt1 = new DealerPayment
            {
                OrgId = orgId,
                PaymentNo = "PMT26090001",
                DealerCode = "VN001",
                DealerName = "Hyundai Đông Đô",
                ContractNo = "2603DRC00001",
                PaymentType = WholesalePaymentType.PMT,
                PaymentMethod = WholesalePaymentMethod.BankTransfer,
                TotalAmount = 226600000m,
                PaymentDate = DateTime.Today.AddDays(-9),
                PaymentDueDate = DateTime.Today.AddDays(-7),
                PaymentEndDate = DateTime.Today.AddDays(20),
                Status = WholesalePaymentStatus.Finished,
                BankPaymentNo = "UNC-VCB-260901",
                BankCodeSend = "TCB",
                BankAccountSend = "19033889911",
                BankCodeReceive = "VCB",
                BankAccountReceive = "0011001234567",
                Funds = "0",
                DepositPercent = 10m,
                AccountingRecordNo = "SAP-AR-2026090188",
                AccountingRecordDate = DateTime.Today.AddDays(-8),
                Remark = "Thanh toán cọc 10% lô 02 xe Santa Fe & Creta hợp đồng mua buôn 2603DRC00001 qua Vietcombank",
                CreatedBy = "DEALER_ACCOUNTANT",
                CreatedAt = DateTime.Now.AddDays(-9),
                ApprovedBy = "NPP_ACCOUNTING_STAFF",
                ApprovedAt = DateTime.Now.AddDays(-9),
                FinishedBy = "KETOAN_TRUONG_HTC",
                FinishedAt = DateTime.Now.AddDays(-8),
                Details = new List<DealerPaymentDetail>
                {
                    new DealerPaymentDetail
                    {
                        PaymentNo = "PMT26090001",
                        CarId = "CAR2026-SF0988",
                        Vin = "KMHE281BBSA129841",
                        Model = "Santa Fe 2.5 HTRAC",
                        Amount = 145200000m,
                        GuaranteeNo = "BG2603-VCB-00128",
                        Remark = "Phân bổ cọc 10% xe Santa Fe"
                    },
                    new DealerPaymentDetail
                    {
                        PaymentNo = "PMT26090001",
                        CarId = "CAR2026-CR0192",
                        Vin = "KMHE281BBSA334455",
                        Model = "Creta 1.5 Cao Cấp",
                        Amount = 81400000m,
                        Remark = "Phân bổ cọc 10% xe Creta"
                    }
                }
            };

            var pmt2 = new DealerPayment
            {
                OrgId = orgId,
                PaymentNo = "PMT26090002",
                DealerCode = "VN002",
                DealerName = "Hyundai Nam Trung",
                ContractNo = "2603DRC00002",
                PaymentType = WholesalePaymentType.PMT,
                PaymentMethod = WholesalePaymentMethod.BankTransfer,
                TotalAmount = 946000000m,
                PaymentDate = DateTime.Today.AddDays(-2),
                PaymentDueDate = DateTime.Today.AddDays(5),
                PaymentEndDate = DateTime.Today.AddDays(30),
                Status = WholesalePaymentStatus.Approved,
                BankPaymentNo = "UNC-BIDV-88219",
                BankCodeSend = "BIDV",
                BankAccountSend = "1241000998877",
                BankCodeReceive = "VCB",
                BankAccountReceive = "0011001234567",
                Funds = "1",
                BankLending = "BIDV Thăng Long",
                InterestRate = 7.5m,
                LoanPeriod = 6,
                GuaranteeType = "LC",
                DepositPercent = 10m,
                Remark = "Ủy nhiệm chi thanh toán lô xe Tucson mở LC ngân hàng BIDV đã được NPP duyệt chấp thuận, chuyển phòng kế toán hạch toán",
                CreatedBy = "DEALER_ACCOUNTANT",
                CreatedAt = DateTime.Now.AddDays(-2),
                ApprovedBy = "NPP_SALES_DIRECTOR",
                ApprovedAt = DateTime.Now.AddDays(-1),
                Details = new List<DealerPaymentDetail>
                {
                    new DealerPaymentDetail
                    {
                        PaymentNo = "PMT26090002",
                        CarId = "CAR2026-TU1102",
                        Vin = "KMHE281BBSA987654",
                        Model = "Tucson 2.0 AT",
                        Amount = 946000000m,
                        GuaranteeNo = "LC2603-BIDV-88910",
                        Remark = "Thanh toán giá trị xe Tucson theo hạn mức tín dụng LC BIDV"
                    }
                }
            };

            var pmt3 = new DealerPayment
            {
                OrgId = orgId,
                PaymentNo = "PMT26090003",
                DealerCode = "VN001",
                DealerName = "Hyundai Đông Đô",
                ContractNo = "2603DRC00003",
                PaymentType = WholesalePaymentType.PMT,
                PaymentMethod = WholesalePaymentMethod.BankTransfer,
                TotalAmount = 99000000m,
                PaymentDate = DateTime.Today,
                PaymentDueDate = DateTime.Today.AddDays(7),
                Status = WholesalePaymentStatus.Pending,
                BankPaymentNo = "UNC-2609-AC003",
                BankCodeSend = "VCB",
                BankAccountSend = "001100998877",
                BankCodeReceive = "VCB",
                BankAccountReceive = "0011001234567",
                Funds = "0",
                DepositPercent = 20m,
                Remark = "Đại lý Đông Đô lập ủy nhiệm chi nộp cọc 20% cho hợp đồng mua buôn Accent, chờ NPP kiểm tra và duyệt xác nhận",
                CreatedBy = "DEALER_ACCOUNTANT",
                CreatedAt = DateTime.Now,
                Details = new List<DealerPaymentDetail>
                {
                    new DealerPaymentDetail
                    {
                        PaymentNo = "PMT26090003",
                        CarId = "CAR2026-AC9011",
                        Vin = "KMHE281BBSA667788",
                        Model = "Accent 1.4 AT",
                        Amount = 99000000m,
                        Remark = "Tiền đặt cọc 20% xe Accent"
                    }
                }
            };

            db.DealerPayments.AddRange(pmt1, pmt2, pmt3);
            await db.SaveChangesAsync();
        }

        if (!await db.RetailDeals.AnyAsync(o => o.OrgId == orgId))
        {
            var dl1 = new RetailDeal
            {
                OrgId = orgId,
                DealNo = "DEAL26090001",
                DealNoUser = "GD-2026/09-01",
                DealerCode = "VN001",
                DealerName = "Hyundai Đông Đô",
                DlrContractNo = "RC26080003",
                SalesType = "RETAIL",
                CustomerType = RetailCustomerType.Individual,
                DealDate = DateTime.Today.AddDays(-15),
                CustomerCodeBuyer = "CUS00311",
                BuyerFullName = "Phạm Hoàng Giang",
                BuyerPhone = "0912345678",
                BuyerIdCardNo = "031088001122",
                BuyerAddress = "88 Lê Lợi, TP. Hải Phòng",
                CustomerCodeHolder = "CUS00311",
                HolderFullName = "Phạm Hoàng Giang",
                CustomerCodeDriver = "CUS00311",
                DriverFullName = "Phạm Hoàng Giang",
                SMCode = "SM001",
                SMName = "Trần Tuấn Anh",
                PaymentType = RetailPaymentType.Cash,
                BankLoanAmount = 0m,
                FlagPDI = true,
                CtmCareFlag = true,
                CtmCareUpdDate = DateTime.Now.AddDays(-12),
                CtmCareUpdBy = "NPP_CSKH_AUDITOR",
                Status = RetailDealStatus.Delivered,
                TotalCars = 1,
                TotalAmount = 940000000m,
                Remark = "Giao dịch bán lẻ xe Tucson hoàn tất bàn giao và hóa đơn khách hàng",
                CreatedBy = "DEALER_SALES",
                CreatedAt = DateTime.Now.AddDays(-15),
                Details = new List<RetailDealDetail>
                {
                    new RetailDealDetail
                    {
                        DealNo = "DEAL26090001",
                        CarId = "CAR2026-TU1102",
                        Vin = "KMHE281BBSA987654",
                        Model = "Tucson 2.0 AT",
                        SpecCode = "TU20-STD-01",
                        ColorCode = "NKA",
                        Price = 940000000m,
                        PlateNo = "15A-987.65",
                        CusInvoiceNo = "HD-KH-2026-0012",
                        CusInvoiceDate = DateTime.Today.AddDays(-14),
                        DeliveryStatus = RetailDealDeliveryStatus.Delivered,
                        DeliveryDate = DateTime.Now.AddDays(-10),
                        Remark = "Đã giao xe và bấm biển thành công"
                    }
                },
                Attachments = new List<RetailDealAttach>
                {
                    new RetailDealAttach
                    {
                        DealNo = "DEAL26090001",
                        FileType = "BILL",
                        FileName = "hoadon_vat_khach_hang_tucson.pdf",
                        FilePath = "/storage/deals/DEAL26090001/hoadon_vat_khach_hang_tucson.pdf",
                        Remark = "Hóa đơn GTGT xuất cho khách hàng cá nhân",
                        UploadedAt = DateTime.Now.AddDays(-14),
                        UploadedBy = "DEALER_ACCOUNTING"
                    },
                    new RetailDealAttach
                    {
                        DealNo = "DEAL26090001",
                        FileType = "INSURANCE",
                        FileName = "bao_hiem_vat_chat_tucson.pdf",
                        FilePath = "/storage/deals/DEAL26090001/bao_hiem_vat_chat_tucson.pdf",
                        Remark = "Bảo hiểm vật chất 1 năm Bảo Việt",
                        UploadedAt = DateTime.Now.AddDays(-13),
                        UploadedBy = "DEALER_SALES"
                    }
                }
            };

            var dl2 = new RetailDeal
            {
                OrgId = orgId,
                DealNo = "DEAL26090002",
                DealNoUser = "GD-2026/09-02",
                DealerCode = "VN001",
                DealerName = "Hyundai Đông Đô",
                DlrContractNo = "RC26090001",
                SalesType = "RETAIL",
                CustomerType = RetailCustomerType.Individual,
                DealDate = DateTime.Today.AddDays(-3),
                CustomerCodeBuyer = "CUS00128",
                BuyerFullName = "Nguyễn Thành Long",
                BuyerPhone = "0988123456",
                BuyerIdCardNo = "001095012345",
                BuyerAddress = "18 Tam Trinh, Hoàng Mai, Hà Nội",
                CustomerCodeHolder = "CUS00128",
                HolderFullName = "Nguyễn Thành Long",
                CustomerCodeDriver = "CUS00128",
                DriverFullName = "Nguyễn Thành Long",
                SMCode = "SM001",
                SMName = "Trần Tuấn Anh",
                PaymentType = RetailPaymentType.Installment,
                BankCode = "VCB",
                BankName = "Vietcombank Thăng Long",
                BankLoanAmount = 900000000m,
                FlagPDI = true,
                CtmCareFlag = true,
                CtmCareUpdDate = DateTime.Now.AddDays(-2),
                CtmCareUpdBy = "NPP_CSKH_AUDITOR",
                Status = RetailDealStatus.Verified,
                TotalCars = 1,
                TotalAmount = 1320000000m,
                Remark = "Giao dịch xe Santa Fe trả góp đã kiểm chứng CSKH, đã có biển số, chờ ngày khách nhận xe",
                CreatedBy = "DEALER_SALES",
                CreatedAt = DateTime.Now.AddDays(-3),
                Details = new List<RetailDealDetail>
                {
                    new RetailDealDetail
                    {
                        DealNo = "DEAL26090002",
                        CarId = "CAR2026-SF0988",
                        Vin = "KMHE281BBSA129841",
                        Model = "Santa Fe 2.5 HTRAC",
                        SpecCode = "SF25-PRE-01",
                        ColorCode = "WW2",
                        Price = 1320000000m,
                        PlateNo = "30K-998.88",
                        CusInvoiceNo = "HD-KH-2026-0035",
                        CusInvoiceDate = DateTime.Today.AddDays(-3),
                        DeliveryStatus = RetailDealDeliveryStatus.Pending,
                        Remark = "Chờ bàn giao ngày đẹp theo yêu cầu khách hàng"
                    }
                },
                Attachments = new List<RetailDealAttach>
                {
                    new RetailDealAttach
                    {
                        DealNo = "DEAL26090002",
                        FileType = "BILL",
                        FileName = "hoadon_banle_santafe.pdf",
                        FilePath = "/storage/deals/DEAL26090002/hoadon_banle_santafe.pdf",
                        Remark = "Hóa đơn bán lẻ điện tử",
                        UploadedAt = DateTime.Now.AddDays(-3),
                        UploadedBy = "DEALER_ACCOUNTING"
                    }
                }
            };

            var dl3 = new RetailDeal
            {
                OrgId = orgId,
                DealNo = "DEAL26090003",
                DealNoUser = "GD-2026/09-03",
                DealerCode = "VN002",
                DealerName = "Hyundai Nam Trung",
                SalesType = "FLEET",
                CustomerType = RetailCustomerType.Corporate,
                DealDate = DateTime.Today,
                CustomerCodeBuyer = "CUS00299",
                BuyerFullName = "Công ty TNHH Vận tải Du lịch Sao Mai",
                BuyerPhone = "0904888999",
                BuyerIdCardNo = "0108899881",
                BuyerAddress = "120 Nguyễn Xiển, Thanh Xuân, Hà Nội",
                CustomerCodeHolder = "TRAN01",
                HolderFullName = "Nguyễn Văn Tuấn (Giám đốc)",
                CustomerCodeDriver = "DRV001",
                DriverFullName = "Lê Hồng Sơn",
                SMCode = "SM002",
                SMName = "Hoàng Thị Dung",
                PaymentType = RetailPaymentType.Cash,
                BankLoanAmount = 0m,
                FlagPDI = false,
                ReasonNotPDI = "Giao xe nguyên bản về kho khách hàng tự vận hành PDI",
                CtmCareFlag = false,
                Status = RetailDealStatus.Pending,
                TotalCars = 1,
                TotalAmount = 850000000m,
                Remark = "Giao dịch xe Custin bán lô doanh nghiệp mới lập, chưa kiểm chứng CSKH",
                CreatedBy = "DEALER_SALES",
                CreatedAt = DateTime.Now,
                Details = new List<RetailDealDetail>
                {
                    new RetailDealDetail
                    {
                        DealNo = "DEAL26090003",
                        CarId = "CAR2026-CU0211",
                        Vin = "KMHE281BBSA556677",
                        Model = "Custin 1.5T-GDi Cao Cấp",
                        SpecCode = "CU15-PRE-01",
                        ColorCode = "SL1",
                        Price = 850000000m,
                        DeliveryStatus = RetailDealDeliveryStatus.Pending,
                        Remark = "Xe mới nhập kho đại lý chuẩn bị kiểm tra giao"
                    }
                }
            };

            db.RetailDeals.AddRange(dl1, dl2, dl3);
            await db.SaveChangesAsync();
        }

        if (!await db.StorageRearranges.AnyAsync(o => o.OrgId == orgId))
        {
            var sr1 = new StorageRearrangeOrder
            {
                OrgId = orgId,
                RearrangeNo = "SR26090001",
                StorageCodeFrom = "KHO_NINHBINH",
                StorageNameFrom = "Tổng kho Nhà máy Hyundai Ninh Bình",
                StorageCodeTo = "KHO_HANOI",
                StorageNameTo = "Tổng kho Phân phối Miền Bắc - Hà Nội",
                RearrangeType = StorageRearrangeType.Normal,
                Status = StorageRearrangeStatus.Approved2,
                TotalCars = 2,
                TransporterName = "Công ty CP Vận tải Ô tô Nam Trung",
                PlateNo = "29C-992.88",
                DriverName = "Phạm Tuấn Vũ",
                DriverPhone = "0912348888",
                CreatedBy = "NPP_DISPATCHER",
                CreatedAt = DateTime.Now.AddDays(-5),
                Approved1By = "NPP_DISPATCH_LEAD",
                Approved1At = DateTime.Now.AddDays(-4),
                Approved2By = "NPP_WAREHOUSE_DIRECTOR",
                Approved2At = DateTime.Now.AddDays(-3),
                Remark = "Điều chuyển lô 02 xe Santa Fe & Creta bổ sung tồn kho trung tâm Hà Nội phục vụ kế hoạch giao xe tháng 9/2026",
                Details = new List<StorageRearrangeDetail>
                {
                    new StorageRearrangeDetail
                    {
                        RearrangeNo = "SR26090001",
                        CarId = "CAR2026-SF0988",
                        Vin = "KMHE281BBSA129841",
                        Model = "Santa Fe 2.5 HTRAC",
                        SpecCode = "SF25-PRE-01",
                        ColorCode = "WW2",
                        EngineNo = "G4KP-102941",
                        StorageCodeFrom = "KHO_NINHBINH",
                        StorageCodeTo = "KHO_HANOI",
                        ExpectedStartDate = DateTime.Today.AddDays(-4),
                        ExpectedEndDate = DateTime.Today.AddDays(-3),
                        ActualOutDate = DateTime.Today.AddDays(-4),
                        ActualInDate = DateTime.Today.AddDays(-3),
                        Status = StorageRearrangeDtlStatus.Completed,
                        ConfirmBy = "KHO_HANOI_LEAD",
                        ConfirmDate = DateTime.Now.AddDays(-3),
                        Remark = "Đã nhập bãi xe Hà Nội, tình trạng hoàn hảo"
                    },
                    new StorageRearrangeDetail
                    {
                        RearrangeNo = "SR26090001",
                        CarId = "CAR2026-CR0192",
                        Vin = "KMHE281BBSA334455",
                        Model = "Creta 1.5 Cao Cấp",
                        SpecCode = "CR15-PRE-02",
                        ColorCode = "R3R",
                        EngineNo = "G4FL-334411",
                        StorageCodeFrom = "KHO_NINHBINH",
                        StorageCodeTo = "KHO_HANOI",
                        ExpectedStartDate = DateTime.Today.AddDays(-4),
                        ExpectedEndDate = DateTime.Today.AddDays(-3),
                        ActualOutDate = DateTime.Today.AddDays(-4),
                        ActualInDate = DateTime.Today.AddDays(-3),
                        Status = StorageRearrangeDtlStatus.Completed,
                        ConfirmBy = "KHO_HANOI_LEAD",
                        ConfirmDate = DateTime.Now.AddDays(-3),
                        Remark = "Đã nhập bãi xe Hà Nội"
                    }
                }
            };

            var sr2 = new StorageRearrangeOrder
            {
                OrgId = orgId,
                RearrangeNo = "SR26090002",
                StorageCodeFrom = "KHO_HAIPHONG",
                StorageNameFrom = "Kho Cảng Tân Vũ - Hải Phòng",
                StorageCodeTo = "KHO_DANANG",
                StorageNameTo = "Tổng kho Phân phối Miền Trung - Đà Nẵng",
                RearrangeType = StorageRearrangeType.Urgent,
                Status = StorageRearrangeStatus.Approved1,
                TotalCars = 1,
                TransporterName = "Đội xe Chuyên dùng Đường bộ HTC",
                PlateNo = "15C-778.66",
                DriverName = "Nguyễn Thành Chung",
                DriverPhone = "0987112233",
                CreatedBy = "NPP_DISPATCHER",
                CreatedAt = DateTime.Now.AddDays(-2),
                Approved1By = "NPP_DISPATCH_LEAD",
                Approved1At = DateTime.Now.AddDays(-1),
                Remark = "Điều chuyển khẩn cấp 01 xe Tucson chi viện cho thị trường miền Trung theo hợp đồng bán buôn",
                Details = new List<StorageRearrangeDetail>
                {
                    new StorageRearrangeDetail
                    {
                        RearrangeNo = "SR26090002",
                        CarId = "CAR2026-TU1102",
                        Vin = "KMHE281BBSA987654",
                        Model = "Tucson 2.0 AT",
                        SpecCode = "TU20-STD-01",
                        ColorCode = "NKA",
                        EngineNo = "G4NL-983102",
                        StorageCodeFrom = "KHO_HAIPHONG",
                        StorageCodeTo = "KHO_DANANG",
                        ExpectedStartDate = DateTime.Today.AddDays(-1),
                        ExpectedEndDate = DateTime.Today.AddDays(2),
                        Status = StorageRearrangeDtlStatus.Approved1,
                        Remark = "Xe đang làm thủ tục xuất bãi cảng Tân Vũ"
                    }
                }
            };

            var sr3 = new StorageRearrangeOrder
            {
                OrgId = orgId,
                RearrangeNo = "SR26090003",
                StorageCodeFrom = "KHO_NINHBINH",
                StorageNameFrom = "Tổng kho Nhà máy Hyundai Ninh Bình",
                StorageCodeTo = "SHOWROOM_VN001",
                StorageNameTo = "Showroom Trưng bày Đại lý Đông Đô",
                RearrangeType = StorageRearrangeType.Showroom,
                Status = StorageRearrangeStatus.Pending,
                TotalCars = 1,
                CreatedBy = "NPP_DISPATCHER",
                CreatedAt = DateTime.Now,
                Remark = "Lập kế hoạch chuyển 01 xe Custin trưng bày sự kiện ra mắt showroom mới đại lý Đông Đô",
                Details = new List<StorageRearrangeDetail>
                {
                    new StorageRearrangeDetail
                    {
                        RearrangeNo = "SR26090003",
                        CarId = "CAR2026-CU0211",
                        Vin = "KMHE281BBSA556677",
                        Model = "Custin 1.5T-GDi Cao Cấp",
                        SpecCode = "CU15-PRE-01",
                        ColorCode = "SL1",
                        EngineNo = "G4FS-881920",
                        StorageCodeFrom = "KHO_NINHBINH",
                        StorageCodeTo = "SHOWROOM_VN001",
                        ExpectedStartDate = DateTime.Today.AddDays(1),
                        ExpectedEndDate = DateTime.Today.AddDays(3),
                        Status = StorageRearrangeDtlStatus.Pending,
                        Remark = "Chờ phê duyệt điều chuyển trưng bày"
                    }
                }
            };

            db.StorageRearranges.AddRange(sr1, sr2, sr3);
            await db.SaveChangesAsync();
        }
    }
}
