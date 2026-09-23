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

                CREATE TABLE IF NOT EXISTS TransportRequests (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    OrgId TEXT NOT NULL,
                    TranspReqNo TEXT NOT NULL,
                    DealerCode TEXT NOT NULL,
                    DealerName TEXT NOT NULL,
                    TransporterCode TEXT NOT NULL,
                    TransporterName TEXT NOT NULL,
                    TransportContractNo TEXT,
                    TranspReqType INTEGER NOT NULL,
                    Status INTEGER NOT NULL,
                    TotalCars INTEGER NOT NULL,
                    PlateNo TEXT,
                    DriverName TEXT,
                    DriverPhone TEXT,
                    ExpectedStartDate TEXT,
                    ExpectedEndDate TEXT,
                    Remark TEXT,
                    RejectReason TEXT,
                    CancelReason TEXT,
                    CreatedBy TEXT,
                    CreatedAt TEXT NOT NULL,
                    ApprovedBy TEXT,
                    ApprovedAt TEXT,
                    CompletedAt TEXT,
                    CancelledBy TEXT,
                    CancelledAt TEXT
                );
                CREATE UNIQUE INDEX IF NOT EXISTS IX_TransportRequests_OrgId_TranspReqNo ON TransportRequests(OrgId, TranspReqNo);

                CREATE TABLE IF NOT EXISTS TransportRequestDetails (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    TranspRequestId INTEGER NOT NULL,
                    TranspReqNo TEXT NOT NULL,
                    TranspReqType INTEGER NOT NULL,
                    RefOrdNo TEXT NOT NULL,
                    CarId TEXT NOT NULL,
                    Vin TEXT NOT NULL,
                    Model TEXT NOT NULL,
                    SpecCode TEXT,
                    ColorCode TEXT,
                    EngineNo TEXT,
                    StorageCodeFrom TEXT,
                    StorageCodeTo TEXT,
                    Status INTEGER NOT NULL,
                    ActualOutDate TEXT,
                    ActualInDate TEXT,
                    Remark TEXT,
                    FOREIGN KEY(TranspRequestId) REFERENCES TransportRequests(Id) ON DELETE CASCADE
                );

                CREATE TABLE IF NOT EXISTS GuaranteeExts (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    OrgId TEXT NOT NULL,
                    GrtClaimExtNo TEXT NOT NULL,
                    DealerCode TEXT NOT NULL,
                    DealerName TEXT NOT NULL,
                    NumberOfGuaranteeExt INTEGER NOT NULL,
                    TotalCars INTEGER NOT NULL,
                    TotalCarNoStart INTEGER NOT NULL,
                    FlagisHTC TEXT NOT NULL,
                    Status INTEGER NOT NULL,
                    SignDateTime TEXT,
                    SignBy TEXT,
                    FileSigned TEXT,
                    Remark TEXT,
                    CancelReason TEXT,
                    CancelDateTime TEXT,
                    CancelBy TEXT,
                    CreatedBy TEXT,
                    CreatedDateTime TEXT NOT NULL,
                    LUDateTime TEXT,
                    LUBy TEXT
                );
                CREATE UNIQUE INDEX IF NOT EXISTS IX_GuaranteeExts_OrgId_GrtClaimExtNo ON GuaranteeExts(OrgId, GrtClaimExtNo);

                CREATE TABLE IF NOT EXISTS GuaranteeExtDetails (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    GrtClaimExtId INTEGER NOT NULL,
                    GrtClaimExtNo TEXT NOT NULL,
                    CarId TEXT NOT NULL,
                    Vin TEXT NOT NULL,
                    Model TEXT NOT NULL,
                    SpecCode TEXT,
                    ColorCode TEXT,
                    ContractNo TEXT,
                    GuaranteeNo TEXT,
                    BankGuaranteeNo TEXT,
                    BankCode TEXT,
                    BankCodeMonitor TEXT,
                    GuaranteeType TEXT,
                    DateOpen TEXT,
                    GrtDateStart TEXT,
                    GrtDateExpired TEXT,
                    GrtDateEnd TEXT,
                    GuaranteeValue REAL NOT NULL,
                    UnitPriceActual REAL NOT NULL,
                    Status INTEGER NOT NULL,
                    Remark TEXT,
                    FOREIGN KEY(GrtClaimExtId) REFERENCES GuaranteeExts(Id) ON DELETE CASCADE
                );

                CREATE TABLE IF NOT EXISTS PdiRequests (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    OrgId TEXT NOT NULL,
                    DlrPdiReqNo TEXT NOT NULL,
                    DealerCode TEXT NOT NULL,
                    DealerName TEXT NOT NULL,
                    FlagAccessory INTEGER NOT NULL,
                    Status INTEGER NOT NULL,
                    TotalCars INTEGER NOT NULL,
                    Remark TEXT,
                    ApprovedDate TEXT,
                    ApprovedBy TEXT,
                    CancelledDate TEXT,
                    CancelledBy TEXT,
                    CancelReason TEXT,
                    CreatedBy TEXT,
                    CreatedDate TEXT NOT NULL,
                    LUDateTime TEXT,
                    LUBy TEXT
                );
                CREATE UNIQUE INDEX IF NOT EXISTS IX_PdiRequests_OrgId_DlrPdiReqNo ON PdiRequests(OrgId, DlrPdiReqNo);

                CREATE TABLE IF NOT EXISTS PdiRequestDetails (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    PdiRequestId INTEGER NOT NULL,
                    DlrPdiReqNo TEXT NOT NULL,
                    Vin TEXT NOT NULL,
                    Model TEXT NOT NULL,
                    ModelCode TEXT,
                    SpecCode TEXT,
                    ColorCode TEXT,
                    ColorName TEXT,
                    DlrContractNo TEXT NOT NULL,
                    CtrCarId TEXT NOT NULL,
                    CustomerName TEXT,
                    CustomerPhone TEXT,
                    DealNo TEXT,
                    DlvExpectedDate TEXT,
                    RONo TEXT,
                    ROCreatedDate TEXT,
                    ROFinishedDate TEXT,
                    ROStatus TEXT NOT NULL,
                    Status INTEGER NOT NULL,
                    InspectionResult TEXT,
                    Remark TEXT,
                    FOREIGN KEY(PdiRequestId) REFERENCES PdiRequests(Id) ON DELETE CASCADE
                );

                CREATE TABLE IF NOT EXISTS PaymentDiscounts (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    OrgId TEXT NOT NULL,
                    PaymentDiscountNo TEXT NOT NULL,
                    DealerCode TEXT NOT NULL,
                    DealerName TEXT NOT NULL,
                    CompanyName TEXT,
                    DateEndFrom TEXT,
                    DateEndTo TEXT,
                    QtyCar INTEGER NOT NULL,
                    SumTotalDiscountPrice REAL NOT NULL,
                    DiscountPercent REAL NOT NULL,
                    PenaltyPercent REAL NOT NULL,
                    Status INTEGER NOT NULL,
                    DealerSignStatus INTEGER NOT NULL,
                    DealerSignDate TEXT,
                    DealerSignBy TEXT,
                    DealerSignFile TEXT,
                    HQSignStatus INTEGER NOT NULL,
                    HQApproveDate TEXT,
                    HQApproveBy TEXT,
                    HQSignDate TEXT,
                    HQSignBy TEXT,
                    HQSignFile TEXT,
                    RejectReason TEXT,
                    CancelReason TEXT,
                    Remark TEXT,
                    CreatedBy TEXT,
                    CreatedAt TEXT NOT NULL,
                    LUDateTime TEXT,
                    LUBy TEXT
                );
                CREATE UNIQUE INDEX IF NOT EXISTS IX_PaymentDiscounts_OrgId_PaymentDiscountNo ON PaymentDiscounts(OrgId, PaymentDiscountNo);

                CREATE TABLE IF NOT EXISTS PaymentDiscountDetails (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    PaymentDiscountId INTEGER NOT NULL,
                    PaymentDiscountNo TEXT NOT NULL,
                    CarId TEXT NOT NULL,
                    Vin TEXT NOT NULL,
                    Model TEXT NOT NULL,
                    SpecCode TEXT,
                    SpecDescription TEXT,
                    SOCode TEXT,
                    GuaranteeNo TEXT NOT NULL,
                    BankGuaranteeNo TEXT,
                    BankCode TEXT,
                    DateOpen TEXT,
                    Term INTEGER NOT NULL,
                    DateStart TEXT,
                    DateEnd TEXT,
                    UnitPrice REAL NOT NULL,
                    UnitPriceActual REAL NOT NULL,
                    GuaranteeValue REAL NOT NULL,
                    PaymentEndDatePhase1 TEXT,
                    AmountPhase1 REAL NOT NULL,
                    DiscountDateNumberPhase1 INTEGER NOT NULL,
                    DiscountPercentPhase1 REAL NOT NULL,
                    DiscountPricePhase1 REAL NOT NULL,
                    PaymentEndDatePhase2 TEXT,
                    AmountPhase2 REAL NOT NULL,
                    DiscountDateNumberPhase2 INTEGER NOT NULL,
                    DiscountPercentPhase2 REAL NOT NULL,
                    DiscountPricePhase2 REAL NOT NULL,
                    PaymentEndDatePhase3 TEXT,
                    AmountPhase3 REAL NOT NULL,
                    DiscountDateNumberPhase3 INTEGER NOT NULL,
                    DiscountPercentPhase3 REAL NOT NULL,
                    DiscountPricePhase3 REAL NOT NULL,
                    TotalAmount REAL NOT NULL,
                    TotalDiscountPrice REAL NOT NULL,
                    Remark TEXT,
                    FOREIGN KEY(PaymentDiscountId) REFERENCES PaymentDiscounts(Id) ON DELETE CASCADE
                );

                CREATE TABLE IF NOT EXISTS DealerContractCancelMinutes (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    OrgId TEXT NOT NULL,
                    CancelMinutesNo TEXT NOT NULL,
                    ContractNo TEXT NOT NULL,
                    DealerCode TEXT NOT NULL,
                    DealerName TEXT NOT NULL,
                    ContractDate TEXT NOT NULL,
                    TotalCars INTEGER NOT NULL,
                    TotalAmount REAL NOT NULL,
                    Remark TEXT,
                    CancelMinutesStatus INTEGER NOT NULL,
                    DealerSignStatus INTEGER NOT NULL,
                    HQSignStatus INTEGER NOT NULL,
                    DealerSignedAt TEXT,
                    DealerSignedBy TEXT,
                    HQAppr1At TEXT,
                    HQAppr1By TEXT,
                    HQAppr2At TEXT,
                    HQAppr2By TEXT,
                    RejectAt TEXT,
                    RejectBy TEXT,
                    RejectReason TEXT,
                    CancelledAt TEXT,
                    CancelledBy TEXT,
                    CancelReason TEXT,
                    SignedFilePath TEXT,
                    CreatedBy TEXT,
                    CreatedAt TEXT NOT NULL,
                    LUDateTime TEXT,
                    LUBy TEXT
                );
                CREATE UNIQUE INDEX IF NOT EXISTS IX_DealerContractCancelMinutes_OrgId_CancelMinutesNo ON DealerContractCancelMinutes(OrgId, CancelMinutesNo);

                CREATE TABLE IF NOT EXISTS GuaranteeClaims (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    OrgId TEXT NOT NULL,
                    GrtClaimNo TEXT NOT NULL,
                    DealerCode TEXT NOT NULL,
                    DealerName TEXT NOT NULL,
                    GrtClaimTypeCode TEXT NOT NULL,
                    FlagisHTC TEXT NOT NULL,
                    BankCode TEXT,
                    BankName TEXT,
                    BankCodeMonitor TEXT,
                    BankAccountNo TEXT,
                    TotalCars INTEGER NOT NULL,
                    TotalClaimAmount REAL NOT NULL,
                    TotalGuaranteeValue REAL NOT NULL,
                    Status INTEGER NOT NULL,
                    VinSignStatus INTEGER NOT NULL,
                    SignDate TEXT,
                    SignBy TEXT,
                    FileSigned TEXT,
                    FileName TEXT,
                    Remark TEXT,
                    RejectReason TEXT,
                    RejectDateTime TEXT,
                    RejectBy TEXT,
                    CancelReason TEXT,
                    CancelDateTime TEXT,
                    CancelBy TEXT,
                    CreatedBy TEXT,
                    CreatedDate TEXT NOT NULL,
                    CreatedDateTime TEXT NOT NULL,
                    LUDateTime TEXT,
                    LUBy TEXT
                );
                CREATE UNIQUE INDEX IF NOT EXISTS IX_GuaranteeClaims_OrgId_GrtClaimNo ON GuaranteeClaims(OrgId, GrtClaimNo);

                CREATE TABLE IF NOT EXISTS GuaranteeClaimDetails (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    GrtClaimId INTEGER NOT NULL,
                    GrtClaimNo TEXT NOT NULL,
                    CarId TEXT NOT NULL,
                    Vin TEXT NOT NULL,
                    Model TEXT NOT NULL,
                    ModelCode TEXT,
                    SpecCode TEXT,
                    SpecDescription TEXT,
                    ColorCode TEXT,
                    ColorName TEXT,
                    ContractNo TEXT,
                    FlagDealerContractDMS40 INTEGER NOT NULL,
                    GuaranteeNo TEXT,
                    BankGuaranteeNo TEXT,
                    BankCode TEXT,
                    BankName TEXT,
                    BankCodeMonitor TEXT,
                    DateOpen TEXT,
                    DateStart TEXT,
                    DateEnd TEXT,
                    UnitPriceActual REAL NOT NULL,
                    GuaranteeValue REAL NOT NULL,
                    ClaimAmount REAL NOT NULL,
                    Status INTEGER NOT NULL,
                    Remark TEXT,
                    FOREIGN KEY(GrtClaimId) REFERENCES GuaranteeClaims(Id) ON DELETE CASCADE
                );

                CREATE TABLE IF NOT EXISTS DealerContractCancelBankMDs (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    OrgId TEXT NOT NULL,
                    CancelBankMDNo TEXT NOT NULL,
                    ContractNo TEXT NOT NULL,
                    DealerCode TEXT NOT NULL,
                    DealerName TEXT NOT NULL,
                    BankCodeMD TEXT NOT NULL,
                    BankName TEXT,
                    TotalCars INTEGER NOT NULL,
                    TotalAmount REAL NOT NULL,
                    Status INTEGER NOT NULL,
                    RemarkDlr TEXT,
                    RemarkBank TEXT,
                    RemarkHQ TEXT,
                    RejectReason TEXT,
                    CancelReason TEXT,
                    CreatedBy TEXT,
                    CreatedAt TEXT NOT NULL,
                    BankApprovedAt TEXT,
                    BankApprovedBy TEXT,
                    FinishedAt TEXT,
                    FinishedBy TEXT,
                    RejectedAt TEXT,
                    RejectedBy TEXT,
                    CancelledAt TEXT,
                    CancelledBy TEXT,
                    LUDateTime TEXT,
                    LUBy TEXT
                );
                CREATE UNIQUE INDEX IF NOT EXISTS IX_DealerContractCancelBankMDs_OrgId_CancelBankMDNo ON DealerContractCancelBankMDs(OrgId, CancelBankMDNo);

                CREATE TABLE IF NOT EXISTS BankBillMinutes (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    OrgId TEXT NOT NULL,
                    BankBillMnNo TEXT NOT NULL,
                    BankCode TEXT NOT NULL,
                    BankName TEXT,
                    DealerCode TEXT NOT NULL,
                    DealerName TEXT NOT NULL,
                    BankBillDate TEXT NOT NULL,
                    BankBillReceiveDate TEXT,
                    BankBillPrintDate TEXT,
                    Status INTEGER NOT NULL,
                    TotalCars INTEGER NOT NULL,
                    TotalClaimAmount REAL NOT NULL,
                    BankOfficer TEXT,
                    HTCOfficer TEXT,
                    Remark TEXT,
                    CancelReason TEXT,
                    CancelledAt TEXT,
                    CancelledBy TEXT,
                    HandoverAt TEXT,
                    HandoverBy TEXT,
                    BankReceivedAt TEXT,
                    BankReceivedBy TEXT,
                    SettledAt TEXT,
                    SettledBy TEXT,
                    CreatedBy TEXT,
                    CreatedAt TEXT NOT NULL,
                    LUDateTime TEXT,
                    LUBy TEXT
                );
                CREATE UNIQUE INDEX IF NOT EXISTS IX_BankBillMinutes_OrgId_BankBillMnNo ON BankBillMinutes(OrgId, BankBillMnNo);

                CREATE TABLE IF NOT EXISTS BankBillMinutesDetails (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    BankBillMinutesId INTEGER NOT NULL,
                    BankBillMnNo TEXT NOT NULL,
                    CarId TEXT NOT NULL,
                    Vin TEXT NOT NULL,
                    Model TEXT NOT NULL,
                    ModelCode TEXT,
                    SpecCode TEXT,
                    SpecDescription TEXT,
                    ColorCode TEXT,
                    EngineNo TEXT,
                    ContractNo TEXT,
                    BankGuaranteeNo TEXT,
                    GuaranteeBankCode TEXT,
                    GuaranteeDateOpen TEXT,
                    HTCInvoiceNo TEXT,
                    TCGInvoiceNo TEXT,
                    InvoiceNoFactory TEXT,
                    TransportMinutesNo TEXT,
                    CQNo TEXT,
                    CONo TEXT,
                    CabinCONo TEXT,
                    DeclarationNo TEXT,
                    ClaimAmount REAL NOT NULL,
                    Status INTEGER NOT NULL,
                    Remark TEXT,
                    FOREIGN KEY(BankBillMinutesId) REFERENCES BankBillMinutes(Id) ON DELETE CASCADE
                );

                CREATE TABLE IF NOT EXISTS CarTestCars (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    OrgId TEXT NOT NULL,
                    TestCarCode TEXT NOT NULL,
                    DealerCode TEXT NOT NULL,
                    DealerName TEXT NOT NULL,
                    TestCarStatus INTEGER NOT NULL,
                    TotalCars INTEGER NOT NULL,
                    TotalAmount REAL NOT NULL,
                    Remark TEXT,
                    RejectReason TEXT,
                    RejectDate TEXT,
                    RejectBy TEXT,
                    CancelReason TEXT,
                    CancelledDate TEXT,
                    CancelledBy TEXT,
                    CreatedDate TEXT NOT NULL,
                    CreatedBy TEXT,
                    ApprovedDate TEXT,
                    ApprovedBy TEXT,
                    FinishedDate TEXT,
                    FinishedBy TEXT,
                    LUDateTime TEXT,
                    LUBy TEXT
                );
                CREATE UNIQUE INDEX IF NOT EXISTS IX_CarTestCars_OrgId_TestCarCode ON CarTestCars(OrgId, TestCarCode);

                CREATE TABLE IF NOT EXISTS CarTestCarDetails (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    TestCarId INTEGER NOT NULL,
                    TestCarCode TEXT NOT NULL,
                    CarId TEXT NOT NULL,
                    Vin TEXT NOT NULL,
                    Model TEXT NOT NULL,
                    ModelCode TEXT,
                    SpecCode TEXT,
                    SpecDescription TEXT,
                    ColorCode TEXT,
                    ColorName TEXT,
                    EngineNo TEXT,
                    SOCode TEXT,
                    UnitPriceActual REAL NOT NULL,
                    EffDateStart TEXT NOT NULL,
                    EffDateEnd TEXT NOT NULL,
                    FlagTestCar INTEGER NOT NULL,
                    Status INTEGER NOT NULL,
                    Remark TEXT,
                    FOREIGN KEY(TestCarId) REFERENCES CarTestCars(Id) ON DELETE CASCADE
                );

                CREATE TABLE IF NOT EXISTS CarBodyRequests (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    OrgId TEXT NOT NULL,
                    CBReqNo TEXT NOT NULL,
                    Status INTEGER NOT NULL,
                    TotalCars INTEGER NOT NULL,
                    StorageCodeTo TEXT NOT NULL,
                    StorageNameTo TEXT,
                    Remark TEXT,
                    RejectReason TEXT,
                    RejectDate TEXT,
                    RejectBy TEXT,
                    CancelReason TEXT,
                    CancelDate TEXT,
                    CancelBy TEXT,
                    CreatedDate TEXT NOT NULL,
                    CreatedBy TEXT,
                    ApprovedDate TEXT,
                    ApprovedBy TEXT,
                    CompletedDate TEXT,
                    CompletedBy TEXT,
                    LUDateTime TEXT,
                    LUBy TEXT
                );
                CREATE UNIQUE INDEX IF NOT EXISTS IX_CarBodyRequests_OrgId_CBReqNo ON CarBodyRequests(OrgId, CBReqNo);

                CREATE TABLE IF NOT EXISTS CarBodyRequestDetails (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    CBRequestId INTEGER NOT NULL,
                    CBReqNo TEXT NOT NULL,
                    CarId TEXT NOT NULL,
                    Vin TEXT NOT NULL,
                    Model TEXT NOT NULL,
                    ModelCode TEXT,
                    SpecCode TEXT,
                    SpecDescription TEXT,
                    ColorCode TEXT,
                    ColorName TEXT,
                    EngineNo TEXT,
                    StorageCodeFrom TEXT NOT NULL,
                    StorageNameFrom TEXT,
                    StorageCodeTo TEXT NOT NULL,
                    StorageNameTo TEXT,
                    LoaiThung TEXT NOT NULL,
                    ActualSpec TEXT,
                    TypeCB TEXT NOT NULL,
                    Status INTEGER NOT NULL,
                    InspectionResult TEXT,
                    SerialNo TEXT,
                    Remark TEXT,
                    FOREIGN KEY(CBRequestId) REFERENCES CarBodyRequests(Id) ON DELETE CASCADE
                );

                CREATE TABLE IF NOT EXISTS DriveTests (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    OrgId TEXT NOT NULL,
                    DriveTestCode TEXT NOT NULL,
                    DealerCode TEXT NOT NULL,
                    DealerName TEXT,
                    DriverTestGroup INTEGER NOT NULL,
                    DriverTestType INTEGER NOT NULL,
                    DriveDTime TEXT NOT NULL,
                    CustomerCode TEXT NOT NULL,
                    FullName TEXT NOT NULL,
                    Gender TEXT NOT NULL,
                    RangeAgeCode TEXT NOT NULL,
                    DriverLicenseNo TEXT NOT NULL,
                    IDCardNo TEXT,
                    PhoneNo TEXT NOT NULL,
                    Email TEXT,
                    Address TEXT,
                    ProvinceCode TEXT,
                    ProvinceName TEXT,
                    ModelCode TEXT NOT NULL,
                    ModelName TEXT,
                    DrvTestPlateNo TEXT NOT NULL,
                    DrvTestVIN TEXT,
                    SalesManCode TEXT,
                    SalesManName TEXT,
                    Evaluation TEXT,
                    CustomerIntent TEXT,
                    EstimatedContractDate TEXT,
                    SupportAmount REAL NOT NULL,
                    ApprovedAmount REAL NOT NULL,
                    Status INTEGER NOT NULL,
                    FlagActive INTEGER NOT NULL DEFAULT 1,
                    CreatedDate TEXT NOT NULL,
                    CreatedBy TEXT,
                    ApprovedDate TEXT,
                    ApprovedBy TEXT,
                    RejectDate TEXT,
                    RejectBy TEXT,
                    RejectRemark TEXT,
                    CancelledDate TEXT,
                    CancelledBy TEXT,
                    CancelRemark TEXT,
                    Remark TEXT
                );
                CREATE UNIQUE INDEX IF NOT EXISTS IX_DriveTests_OrgId_DriveTestCode ON DriveTests(OrgId, DriveTestCode);

                CREATE TABLE IF NOT EXISTS StorageRearrangeCBOrders (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    OrgId TEXT NOT NULL,
                    StoRearCBNo TEXT NOT NULL,
                    Status INTEGER NOT NULL,
                    TotalCars INTEGER NOT NULL,
                    StorageCodeTo TEXT NOT NULL,
                    StorageNameTo TEXT,
                    Remark TEXT,
                    RejectReason TEXT,
                    RejectDate TEXT,
                    RejectBy TEXT,
                    CancelReason TEXT,
                    CancelDate TEXT,
                    CancelBy TEXT,
                    CreatedDate TEXT NOT NULL,
                    CreatedBy TEXT,
                    ApprovedDate TEXT,
                    ApprovedBy TEXT,
                    LUDateTime TEXT,
                    LUBy TEXT
                );
                CREATE UNIQUE INDEX IF NOT EXISTS IX_StorageRearrangeCBOrders_OrgId_StoRearCBNo ON StorageRearrangeCBOrders(OrgId, StoRearCBNo);

                CREATE TABLE IF NOT EXISTS StorageRearrangeCBDetails (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    StoRearCBId INTEGER NOT NULL,
                    StoRearCBNo TEXT NOT NULL,
                    CarId TEXT NOT NULL,
                    Vin TEXT NOT NULL,
                    Model TEXT NOT NULL,
                    ModelCode TEXT,
                    SpecCode TEXT,
                    SpecDescription TEXT,
                    ColorCode TEXT,
                    ColorName TEXT,
                    EngineNo TEXT,
                    StorageCodeFrom TEXT NOT NULL,
                    StorageNameFrom TEXT,
                    StorageCodeTo TEXT NOT NULL,
                    StorageNameTo TEXT,
                    CBReqNo TEXT NOT NULL,
                    ExpectedStartDate TEXT NOT NULL,
                    ExpectedEndDate TEXT NOT NULL,
                    LoaiThung TEXT NOT NULL,
                    ActualSpec TEXT,
                    TypeCB TEXT NOT NULL,
                    Status INTEGER NOT NULL,
                    ConfirmDate TEXT,
                    ConfirmBy TEXT,
                    Remark TEXT,
                    FOREIGN KEY(StoRearCBId) REFERENCES StorageRearrangeCBOrders(Id) ON DELETE CASCADE
                );

                CREATE TABLE IF NOT EXISTS CarCancelReasons (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Code TEXT NOT NULL,
                    Name TEXT NOT NULL,
                    Description TEXT NOT NULL,
                    FlagActive INTEGER NOT NULL
                );
                CREATE UNIQUE INDEX IF NOT EXISTS IX_CarCancelReasons_Code ON CarCancelReasons(Code);

                CREATE TABLE IF NOT EXISTS Cars (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    OrgId TEXT NOT NULL,
                    CarId TEXT NOT NULL,
                    Vin TEXT NOT NULL,
                    ModelCode TEXT NOT NULL,
                    ModelName TEXT NOT NULL,
                    SpecCode TEXT,
                    SpecDescription TEXT,
                    ColorCode TEXT,
                    ColorName TEXT,
                    EngineNo TEXT,
                    DealerCode TEXT NOT NULL,
                    DealerName TEXT,
                    StorageCode TEXT,
                    StorageName TEXT,
                    SoCode TEXT,
                    PriceActual REAL NOT NULL,
                    PaymentDepositAmount REAL NOT NULL,
                    GuaranteeAmount REAL NOT NULL,
                    DutyCompletedAmount REAL NOT NULL,
                    FlagActive TEXT NOT NULL,
                    FlagAllowChangeVIN TEXT NOT NULL,
                    CarCancelType TEXT,
                    CarCancelRemark TEXT,
                    CarCancelDate TEXT,
                    CarCancelBy TEXT,
                    FlagEarlyCancel TEXT NOT NULL,
                    FlagMapVIN TEXT NOT NULL,
                    FlagCarDeliveryOrder TEXT NOT NULL,
                    FlagTestCar TEXT NOT NULL,
                    CreatedAt TEXT NOT NULL,
                    LogLUDateTime TEXT,
                    LogLUBy TEXT
                );
                CREATE UNIQUE INDEX IF NOT EXISTS IX_Cars_OrgId_CarId ON Cars(OrgId, CarId);
                CREATE INDEX IF NOT EXISTS IX_Cars_OrgId_Vin ON Cars(OrgId, Vin);

                CREATE TABLE IF NOT EXISTS CarCancelLogs (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    OrgId TEXT NOT NULL,
                    CarId TEXT NOT NULL,
                    Vin TEXT NOT NULL,
                    Action TEXT NOT NULL,
                    CancelType TEXT,
                    Remark TEXT,
                    PerformedBy TEXT,
                    PerformedAt TEXT NOT NULL
                );
                CREATE INDEX IF NOT EXISTS IX_CarCancelLogs_OrgId_CarId ON CarCancelLogs(OrgId, CarId);

                CREATE TABLE IF NOT EXISTS CarVinInventories (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    OrgId TEXT NOT NULL,
                    Vin TEXT NOT NULL,
                    ModelCode TEXT NOT NULL,
                    ModelName TEXT NOT NULL,
                    SpecCode TEXT,
                    SpecDescription TEXT,
                    ColorCode TEXT,
                    ColorName TEXT,
                    EngineNo TEXT,
                    StorageCode TEXT NOT NULL,
                    StorageName TEXT,
                    AssemblyStatus TEXT NOT NULL,
                    ProductionMonth TEXT,
                    Status INTEGER NOT NULL,
                    MappedCarId TEXT,
                    CreatedAt TEXT NOT NULL
                );
                CREATE UNIQUE INDEX IF NOT EXISTS IX_CarVinInventories_OrgId_Vin ON CarVinInventories(OrgId, Vin);

                CREATE TABLE IF NOT EXISTS MapVinSessions (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    OrgId TEXT NOT NULL,
                    SessionNo TEXT NOT NULL,
                    MapType INTEGER NOT NULL,
                    Method INTEGER NOT NULL,
                    Status INTEGER NOT NULL,
                    DealerCode TEXT,
                    ModelCode TEXT,
                    TotalRequested INTEGER NOT NULL,
                    TotalMapped INTEGER NOT NULL,
                    TotalUnmapped INTEGER NOT NULL,
                    Remark TEXT,
                    CreatedBy TEXT,
                    CreatedAt TEXT NOT NULL,
                    ApprovedBy TEXT,
                    ApprovedAt TEXT,
                    CancelledAt TEXT,
                    CancelReason TEXT
                );
                CREATE UNIQUE INDEX IF NOT EXISTS IX_MapVinSessions_OrgId_SessionNo ON MapVinSessions(OrgId, SessionNo);

                CREATE TABLE IF NOT EXISTS MapVinSessionDetails (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    SessionId INTEGER NOT NULL,
                    CarId TEXT NOT NULL,
                    SoCode TEXT,
                    DealerCode TEXT NOT NULL,
                    DealerName TEXT,
                    ModelCode TEXT NOT NULL,
                    ModelName TEXT NOT NULL,
                    SpecCode TEXT,
                    ColorCode TEXT,
                    ColorName TEXT,
                    Vin TEXT,
                    EngineNo TEXT,
                    StorageCode TEXT,
                    StorageName TEXT,
                    Status INTEGER NOT NULL,
                    RejectReason TEXT,
                    MappedAt TEXT,
                    MappedBy TEXT
                );

                CREATE TABLE IF NOT EXISTS MapVinAuditLogs (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    OrgId TEXT NOT NULL,
                    CarId TEXT NOT NULL,
                    Vin TEXT,
                    Action TEXT NOT NULL,
                    SessionNo TEXT,
                    Remark TEXT,
                    PerformedBy TEXT,
                    PerformedAt TEXT NOT NULL
                );
                CREATE INDEX IF NOT EXISTS IX_MapVinAuditLogs_OrgId_CarId ON MapVinAuditLogs(OrgId, CarId);

                CREATE TABLE IF NOT EXISTS RetailInvoiceRequests (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    OrgId TEXT NOT NULL,
                    ReqInvoiceNo TEXT NOT NULL,
                    DealerCode TEXT NOT NULL,
                    DealerName TEXT,
                    ReqInvoiceType INTEGER NOT NULL,
                    ReqInvoiceBaseNo TEXT,
                    DlrContractNo TEXT,
                    DealNo TEXT,
                    CustomerCode TEXT NOT NULL,
                    CustomerName TEXT NOT NULL,
                    CustomerType TEXT NOT NULL,
                    CustomerAddress TEXT,
                    CustomerEmail TEXT,
                    CustomerPhone TEXT,
                    MST TEXT,
                    TInvoiceCode TEXT,
                    InvoiceCode TEXT,
                    InvoiceNo TEXT,
                    InvoiceDate TEXT,
                    InvoiceSign TEXT,
                    FormNo TEXT,
                    InvoiceStatus INTEGER NOT NULL,
                    InvoiceBaseNo TEXT,
                    AdjType INTEGER NOT NULL,
                    Remark TEXT,
                    ReqInvoiceStatus INTEGER NOT NULL,
                    FlagInvoice TEXT NOT NULL,
                    FlagIsQInvoice INTEGER NOT NULL,
                    QtyCtrCarId INTEGER NOT NULL,
                    TotalTPBeforeVAT REAL NOT NULL,
                    TotalValVAT REAL NOT NULL,
                    TotalTPAfterVAT REAL NOT NULL,
                    TaxAuthorityCode TEXT,
                    InvoiceFileUrl TEXT,
                    InvoiceFileName TEXT,
                    CreatedBy TEXT,
                    CreatedAt TEXT NOT NULL,
                    IssuedBy TEXT,
                    IssuedAt TEXT,
                    ApprovedBy TEXT,
                    ApprovedAt TEXT,
                    TaxTransmittedBy TEXT,
                    TaxTransmittedAt TEXT,
                    CancelReason TEXT,
                    CancelledBy TEXT,
                    CancelledAt TEXT,
                    LUDateTime TEXT,
                    LUBy TEXT
                );
                CREATE UNIQUE INDEX IF NOT EXISTS IX_RetailInvoiceRequests_OrgId_ReqInvoiceNo ON RetailInvoiceRequests(OrgId, ReqInvoiceNo);

                CREATE TABLE IF NOT EXISTS RetailInvoiceRequestDetails (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    RetailInvoiceRequestId INTEGER NOT NULL,
                    ReqInvoiceNo TEXT NOT NULL,
                    CtrCarId TEXT NOT NULL,
                    Vin TEXT NOT NULL,
                    BrandName TEXT NOT NULL,
                    CarType TEXT NOT NULL,
                    ModelCode TEXT NOT NULL,
                    ModelName TEXT NOT NULL,
                    SpecCode TEXT,
                    SpecDescription TEXT,
                    ColorCode TEXT,
                    ColorName TEXT,
                    EngineNo TEXT,
                    NumberOfSeats INTEGER NOT NULL,
                    HTCInvoiceNo TEXT,
                    HTCInvoiceDate TEXT,
                    ProductionYearActual TEXT,
                    CQNo TEXT,
                    FGFormNo TEXT,
                    UPBeforeVAT REAL NOT NULL,
                    VatRate REAL NOT NULL,
                    ValVAT REAL NOT NULL,
                    UPAfterVAT REAL NOT NULL,
                    Status TEXT NOT NULL,
                    Remark TEXT,
                    FOREIGN KEY(RetailInvoiceRequestId) REFERENCES RetailInvoiceRequests(Id) ON DELETE CASCADE
                );

                CREATE TABLE IF NOT EXISTS RetailInvoiceRequestProducts (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    RetailInvoiceRequestId INTEGER NOT NULL,
                    ReqInvoiceNo TEXT NOT NULL,
                    Idx INTEGER NOT NULL,
                    ProductCode TEXT NOT NULL,
                    ProductName TEXT NOT NULL,
                    Unit TEXT NOT NULL,
                    Qty INTEGER NOT NULL,
                    VatRate REAL NOT NULL,
                    UnitPrice REAL NOT NULL,
                    TPBeforeVAT REAL NOT NULL,
                    ValVAT REAL NOT NULL,
                    TPAfterVAT REAL NOT NULL,
                    FlagIsProduct INTEGER NOT NULL,
                    Remark TEXT,
                    FOREIGN KEY(RetailInvoiceRequestId) REFERENCES RetailInvoiceRequests(Id) ON DELETE CASCADE
                );

                CREATE TABLE IF NOT EXISTS CarRedeemRequests (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    OrgId TEXT NOT NULL,
                    ReqDMNo TEXT NOT NULL,
                    DealerCode TEXT NOT NULL,
                    DealerName TEXT,
                    Status INTEGER NOT NULL,
                    RedeemType INTEGER NOT NULL,
                    TotalCars INTEGER NOT NULL,
                    ApprovedCars INTEGER NOT NULL,
                    BankCode TEXT,
                    BankName TEXT,
                    Remark TEXT,
                    CreatedBy TEXT,
                    CreatedAt TEXT NOT NULL,
                    ApprovedBy TEXT,
                    ApprovedAt TEXT,
                    RejectedBy TEXT,
                    RejectedAt TEXT,
                    RejectReason TEXT,
                    CancelledBy TEXT,
                    CancelledAt TEXT,
                    CancelReason TEXT
                );
                CREATE UNIQUE INDEX IF NOT EXISTS IX_CarRedeemRequests_OrgId_ReqDMNo ON CarRedeemRequests(OrgId, ReqDMNo);

                CREATE TABLE IF NOT EXISTS CarRedeemRequestDetails (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    RedeemRequestId INTEGER NOT NULL,
                    ReqDMNo TEXT NOT NULL,
                    CarId TEXT NOT NULL,
                    Vin TEXT NOT NULL,
                    ModelCode TEXT NOT NULL,
                    ModelName TEXT NOT NULL,
                    SpecCode TEXT,
                    SpecDescription TEXT,
                    ColorCode TEXT,
                    ColorName TEXT,
                    EngineNo TEXT,
                    CQNo TEXT,
                    CONo TEXT,
                    DeclarationNo TEXT,
                    MortageBankCode TEXT NOT NULL,
                    MortageBankName TEXT,
                    TypeDMReq INTEGER NOT NULL,
                    DealerCode TEXT NOT NULL,
                    DealerName TEXT,
                    GuaranteeNo TEXT,
                    DRListCode TEXT,
                    DocumentsStatus TEXT,
                    Status INTEGER NOT NULL,
                    ApprovedAt TEXT,
                    ApprovedBy TEXT,
                    Remark TEXT,
                    FOREIGN KEY(RedeemRequestId) REFERENCES CarRedeemRequests(Id) ON DELETE CASCADE
                );

                CREATE TABLE IF NOT EXISTS CarInsuranceRequests (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    OrgId TEXT NOT NULL,
                    InsReqNo TEXT NOT NULL,
                    InsCompanyCode TEXT NOT NULL,
                    InsCompanyName TEXT,
                    InsTypeCode TEXT NOT NULL,
                    InsTypeName TEXT,
                    EffectiveDate TEXT NOT NULL,
                    ExpiryDate TEXT,
                    TotalCars INTEGER NOT NULL,
                    ApprovedCars INTEGER NOT NULL,
                    TotalAmount REAL NOT NULL,
                    TotalPremiumAmount REAL NOT NULL,
                    PolicyNo TEXT,
                    PolicyDate TEXT,
                    Status INTEGER NOT NULL,
                    Remark TEXT,
                    RejectReason TEXT,
                    RejectedDate TEXT,
                    RejectedBy TEXT,
                    CancelReason TEXT,
                    CancelledDate TEXT,
                    CancelledBy TEXT,
                    CreatedDate TEXT NOT NULL,
                    CreatedBy TEXT,
                    ApprovedDate TEXT,
                    ApprovedBy TEXT,
                    LUDateTime TEXT,
                    LUBy TEXT
                );
                CREATE UNIQUE INDEX IF NOT EXISTS IX_CarInsuranceRequests_OrgId_InsReqNo ON CarInsuranceRequests(OrgId, InsReqNo);

                CREATE TABLE IF NOT EXISTS CarInsuranceRequestDetails (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    InsuranceRequestId INTEGER NOT NULL,
                    InsReqNo TEXT NOT NULL,
                    CarId TEXT NOT NULL,
                    Vin TEXT NOT NULL,
                    ModelCode TEXT,
                    ModelName TEXT NOT NULL,
                    SpecCode TEXT,
                    SpecDescription TEXT,
                    ColorCode TEXT,
                    ColorName TEXT,
                    EngineNo TEXT,
                    RefOrdType INTEGER NOT NULL,
                    RefOrdNo TEXT NOT NULL,
                    TransporterCode TEXT,
                    TransporterName TEXT,
                    LocationFrom TEXT,
                    LocationTo TEXT,
                    ExpectedStartDate TEXT,
                    Price REAL NOT NULL,
                    Rate REAL NOT NULL,
                    InsuranceDay INTEGER NOT NULL,
                    InsAmount REAL NOT NULL,
                    PolicyNo TEXT,
                    Status INTEGER NOT NULL,
                    Remark TEXT,
                    ApprovedBy TEXT,
                    ApprovedDate TEXT,
                    FOREIGN KEY(InsuranceRequestId) REFERENCES CarInsuranceRequests(Id) ON DELETE CASCADE
                );
            ");
        }
        catch
        {
            // Bỏ qua nếu DB là Postgres hoặc đã có bảng
        }

        try
        {
            await db.Database.ExecuteSqlRawAsync("ALTER TABLE DriveTests ADD COLUMN FlagActive INTEGER NOT NULL DEFAULT 1;");
        }
        catch
        {
            // Bỏ qua nếu cột đã tồn tại
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

        if (!await db.TransportRequests.AnyAsync(o => o.OrgId == orgId))
        {
            var tr1 = new TransportRequest
            {
                OrgId = orgId,
                TranspReqNo = "TR26090001",
                DealerCode = "VN001",
                DealerName = "Hyundai Đông Đô",
                TransporterCode = "TRANS_NAMTRUNG",
                TransporterName = "Công ty CP Vận tải Nam Trung",
                TransportContractNo = "HDVC-2026/08-NT",
                TranspReqType = TransportRequestType.CarTransport,
                Status = TransportRequestStatus.Completed,
                TotalCars = 1,
                PlateNo = "29C-888.99",
                DriverName = "Trần Văn An",
                DriverPhone = "0912888999",
                ExpectedStartDate = DateTime.Today.AddDays(-3),
                ExpectedEndDate = DateTime.Today.AddDays(-2),
                Remark = "Yêu cầu vận tải giao xe Santa Fe về kho đại lý Đông Đô theo Lệnh xuất xe DO2603010001",
                CreatedBy = "NPP_LOGISTICS",
                CreatedAt = DateTime.Now.AddDays(-4),
                ApprovedBy = "TP_DIEUPHOI_LOGISTICS",
                ApprovedAt = DateTime.Now.AddDays(-3),
                CompletedAt = DateTime.Now.AddDays(-2),
                Details = new List<TransportRequestDetail>
                {
                    new TransportRequestDetail
                    {
                        TranspReqNo = "TR26090001",
                        TranspReqType = TransportRequestType.CarTransport,
                        RefOrdNo = "DO2603010001",
                        CarId = "CAR2026-SF0988",
                        Vin = "KMHE281BBSA129841",
                        Model = "Santa Fe 2.5 HTRAC",
                        SpecCode = "SF25-PRE-01",
                        ColorCode = "WW2",
                        EngineNo = "G4KP-102941",
                        StorageCodeFrom = "KHO_HANOI",
                        StorageCodeTo = "KHO_VN001_DONGDO",
                        Status = TransportRequestDtlStatus.Completed,
                        ActualOutDate = DateTime.Today.AddDays(-3),
                        ActualInDate = DateTime.Today.AddDays(-2),
                        Remark = "Xe đã giao đến đại lý và ký nhận BBBG2603010001"
                    }
                }
            };

            var tr2 = new TransportRequest
            {
                OrgId = orgId,
                TranspReqNo = "TR26090002",
                DealerCode = "VN002",
                DealerName = "Hyundai Nam Trung",
                TransporterCode = "TRANS_CHUYENDUNG",
                TransporterName = "Đội xe Chuyên dùng Đường bộ HTC",
                TransportContractNo = "HDVC-2026/09-NB",
                TranspReqType = TransportRequestType.StorageRearrange,
                Status = TransportRequestStatus.InTransit,
                TotalCars = 1,
                PlateNo = "15C-778.66",
                DriverName = "Nguyễn Thành Chung",
                DriverPhone = "0987112233",
                ExpectedStartDate = DateTime.Today.AddDays(-1),
                ExpectedEndDate = DateTime.Today.AddDays(2),
                Remark = "Yêu cầu vận tải điều chuyển xe Tucson từ Cảng Hải Phòng về Tổng kho Đà Nẵng",
                CreatedBy = "NPP_LOGISTICS",
                CreatedAt = DateTime.Now.AddDays(-2),
                ApprovedBy = "TP_DIEUPHOI_LOGISTICS",
                ApprovedAt = DateTime.Now.AddDays(-1),
                Details = new List<TransportRequestDetail>
                {
                    new TransportRequestDetail
                    {
                        TranspReqNo = "TR26090002",
                        TranspReqType = TransportRequestType.StorageRearrange,
                        RefOrdNo = "SR26090002",
                        CarId = "CAR2026-TU1102",
                        Vin = "KMHE281BBSA987654",
                        Model = "Tucson 2.0 AT",
                        SpecCode = "TU20-STD-01",
                        ColorCode = "NKA",
                        EngineNo = "G4NL-983102",
                        StorageCodeFrom = "KHO_HAIPHONG",
                        StorageCodeTo = "KHO_DANANG",
                        Status = TransportRequestDtlStatus.InTransit,
                        ActualOutDate = DateTime.Today.AddDays(-1),
                        Remark = "Xe đang trên đường vận chuyển dọc tuyến Quốc lộ 1A"
                    }
                }
            };

            var tr3 = new TransportRequest
            {
                OrgId = orgId,
                TranspReqNo = "TR26090003",
                DealerCode = "VN001",
                DealerName = "Hyundai Đông Đô",
                TransporterCode = "TRANS_NAMTRUNG",
                TransporterName = "Công ty CP Vận tải Nam Trung",
                TransportContractNo = "HDVC-2026/08-NT",
                TranspReqType = TransportRequestType.CarRetrieve,
                Status = TransportRequestStatus.Pending,
                TotalCars = 1,
                PlateNo = "29C-665.55",
                DriverName = "Hoàng Quốc Việt",
                DriverPhone = "0915334455",
                ExpectedStartDate = DateTime.Today.AddDays(1),
                ExpectedEndDate = DateTime.Today.AddDays(2),
                Remark = "Yêu cầu vận tải chở xe thu hồi từ kho đại lý Đông Đô về kho trung tâm NPP",
                CreatedBy = "NPP_LOGISTICS",
                CreatedAt = DateTime.Now,
                Details = new List<TransportRequestDetail>
                {
                    new TransportRequestDetail
                    {
                        TranspReqNo = "TR26090003",
                        TranspReqType = TransportRequestType.CarRetrieve,
                        RefOrdNo = "CR26090001",
                        CarId = "CAR2026-CR0192",
                        Vin = "KMHE281BBSA334455",
                        Model = "Creta 1.5 Cao Cấp",
                        SpecCode = "CR15-PRE-02",
                        ColorCode = "R3R",
                        EngineNo = "G4FL-334411",
                        StorageCodeFrom = "KHO_VN001_DONGDO",
                        StorageCodeTo = "KHO_HANOI",
                        Status = TransportRequestDtlStatus.Pending,
                        Remark = "Chờ ban điều phối NPP phê duyệt lệnh điều xe"
                    }
                }
            };

            db.TransportRequests.AddRange(tr1, tr2, tr3);
            await db.SaveChangesAsync();
        }

        if (!await db.GuaranteeExts.AnyAsync(o => o.OrgId == orgId))
        {
            var ext1 = new PaymentGuaranteeExt
            {
                OrgId = orgId,
                GrtClaimExtNo = "260310-001/CVGH/VN001",
                DealerCode = "VN001",
                DealerName = "Hyundai Đông Đô",
                NumberOfGuaranteeExt = 30,
                TotalCars = 1,
                TotalCarNoStart = 0,
                FlagisHTC = "1",
                Status = GuaranteeExtStatus.Signed,
                SignDateTime = DateTime.Now.AddDays(-2),
                SignBy = "Phạm Quang Minh (Phó TGĐ Phân phối HTC)",
                FileSigned = "/storage/grtclaimext/260310-001_CVGH_VN001_signed.pdf",
                Remark = "Công văn xin gia hạn bảo lãnh thanh toán 30 ngày cho lô xe Santa Fe hợp đồng mua buôn 2603DRC00001",
                CreatedBy = "DEALER_ACCOUNTANT",
                CreatedDateTime = DateTime.Now.AddDays(-4),
                LUDateTime = DateTime.Now.AddDays(-2),
                LUBy = "NPP_HQ_DIRECTOR",
                Details = new List<PaymentGuaranteeExtDetail>
                {
                    new PaymentGuaranteeExtDetail
                    {
                        GrtClaimExtNo = "260310-001/CVGH/VN001",
                        CarId = "CAR2026-SF0988",
                        Vin = "KMHE281BBSA129841",
                        Model = "Santa Fe 2.5 HTRAC",
                        SpecCode = "SF25-PRE-01",
                        ColorCode = "WW2",
                        ContractNo = "2603DRC00001",
                        GuaranteeNo = "BG2603-VCB-00128",
                        BankGuaranteeNo = "BL-VCB-20260301-88",
                        BankCode = "VCB",
                        BankCodeMonitor = "VCB_HO",
                        GuaranteeType = "BL",
                        DateOpen = DateTime.Today.AddDays(-20),
                        GrtDateStart = DateTime.Today.AddDays(-15),
                        GrtDateExpired = DateTime.Today.AddDays(5),
                        GrtDateEnd = DateTime.Today.AddDays(35),
                        GuaranteeValue = 1320000000m,
                        UnitPriceActual = 1320000000m,
                        Status = GuaranteeExtDtlStatus.Signed,
                        Remark = "Đã được NPP ký số và ngân hàng Vietcombank chấp thuận gia hạn thời hạn trả chậm"
                    }
                }
            };

            var ext2 = new PaymentGuaranteeExt
            {
                OrgId = orgId,
                GrtClaimExtNo = "260315-001/CVGH/VN002",
                DealerCode = "VN002",
                DealerName = "Hyundai Nam Trung",
                NumberOfGuaranteeExt = 15,
                TotalCars = 1,
                TotalCarNoStart = 1,
                FlagisHTC = "2",
                Status = GuaranteeExtStatus.Pending,
                Remark = "Đề nghị phát hành và kích hoạt bảo lãnh mới cho xe Tucson mở LC ngân hàng BIDV",
                CreatedBy = "DEALER_SALES_ADMIN",
                CreatedDateTime = DateTime.Now.AddDays(-1),
                Details = new List<PaymentGuaranteeExtDetail>
                {
                    new PaymentGuaranteeExtDetail
                    {
                        GrtClaimExtNo = "260315-001/CVGH/VN002",
                        CarId = "CAR2026-TU1102",
                        Vin = "KMHE281BBSA987654",
                        Model = "Tucson 2.0 AT",
                        SpecCode = "TU20-STD-01",
                        ColorCode = "NKA",
                        ContractNo = "2603DRC00002",
                        GuaranteeNo = "LC2603-BIDV-0099",
                        BankGuaranteeNo = "LC-BIDV-20260315",
                        BankCode = "BIDV",
                        BankCodeMonitor = "BIDV_THANG_LONG",
                        GuaranteeType = "LC",
                        DateOpen = DateTime.Today.AddDays(-3),
                        GrtDateStart = null, // Chưa có ngày hiệu lực (TotalCarNoStart = 1)
                        GrtDateExpired = DateTime.Today.AddDays(12),
                        GrtDateEnd = DateTime.Today.AddDays(27),
                        GuaranteeValue = 860000000m,
                        UnitPriceActual = 860000000m,
                        Status = GuaranteeExtDtlStatus.Pending,
                        Remark = "Chờ ban lãnh đạo NPP ký điện tử công văn để ngân hàng giải tỏa ngày hiệu lực"
                    }
                }
            };

            var ext3 = new PaymentGuaranteeExt
            {
                OrgId = orgId,
                GrtClaimExtNo = "260320-002/CVGH/VN001",
                DealerCode = "VN001",
                DealerName = "Hyundai Đông Đô",
                NumberOfGuaranteeExt = 30,
                TotalCars = 1,
                TotalCarNoStart = 0,
                FlagisHTC = "1",
                Status = GuaranteeExtStatus.Cancelled,
                CancelReason = "Đại lý đã thanh toán tất toán toàn bộ tiền mặt mua xe, không cần gia hạn hạn mức bảo lãnh",
                CancelDateTime = DateTime.Now.AddHours(-12),
                CancelBy = "DEALER_ACCOUNTANT",
                Remark = "Công văn xin gia hạn bảo lãnh xe Creta",
                CreatedBy = "DEALER_ACCOUNTANT",
                CreatedDateTime = DateTime.Now.AddDays(-2),
                LUDateTime = DateTime.Now.AddHours(-12),
                LUBy = "DEALER_ACCOUNTANT",
                Details = new List<PaymentGuaranteeExtDetail>
                {
                    new PaymentGuaranteeExtDetail
                    {
                        GrtClaimExtNo = "260320-002/CVGH/VN001",
                        CarId = "CAR2026-CR0192",
                        Vin = "KMHE281BBSA334455",
                        Model = "Creta 1.5 Cao Cấp",
                        SpecCode = "CR15-PRE-02",
                        ColorCode = "R3R",
                        ContractNo = "2603DRC00001",
                        GuaranteeNo = "BG2603-VCB-00128",
                        BankGuaranteeNo = "BL-VCB-20260301-88",
                        BankCode = "VCB",
                        GuaranteeType = "BL",
                        DateOpen = DateTime.Today.AddDays(-20),
                        GrtDateStart = DateTime.Today.AddDays(-15),
                        GrtDateExpired = DateTime.Today.AddDays(10),
                        GrtDateEnd = DateTime.Today.AddDays(40),
                        GuaranteeValue = 740000000m,
                        UnitPriceActual = 740000000m,
                        Status = GuaranteeExtDtlStatus.Cancelled,
                        Remark = "Đã hủy do đại lý thanh toán tiền mặt"
                    }
                }
            };

            db.GuaranteeExts.AddRange(ext1, ext2, ext3);
            await db.SaveChangesAsync();
        }

        if (!await db.PdiRequests.AnyAsync(o => o.OrgId == orgId))
        {
            var pdi1 = new PdiRequest
            {
                OrgId = orgId,
                DlrPdiReqNo = "2609PRN00001",
                DealerCode = "VN001",
                DealerName = "Hyundai Đông Đô",
                FlagAccessory = true,
                Status = PdiRequestStatus.Approved,
                TotalCars = 2,
                Remark = "Kiểm tra kỹ thuật PDI toàn diện và lắp gói phụ kiện dán phim cách nhiệt + lót sàn trước khi bàn giao xe",
                ApprovedDate = DateTime.Now.AddDays(-1),
                ApprovedBy = "NPP_PDI_LEAD",
                CreatedBy = "DEALER_PDI_STAFF",
                CreatedDate = DateTime.Now.AddDays(-3),
                LUDateTime = DateTime.Now.AddDays(-1),
                LUBy = "NPP_PDI_LEAD",
                Details = new List<PdiRequestDetail>
                {
                    new PdiRequestDetail
                    {
                        DlrPdiReqNo = "2609PRN00001",
                        Vin = "KMHE281BBSA129841",
                        Model = "Santa Fe 2.5 HTRAC",
                        ModelCode = "TM",
                        SpecCode = "SF25-PRE-01",
                        ColorCode = "WW2",
                        ColorName = "Trắng ngọc trai / Đen",
                        DlrContractNo = "RC26090001",
                        CtrCarId = "RC26090001.01",
                        CustomerName = "Trần Đình Trọng",
                        CustomerPhone = "0912345678",
                        DealNo = "DEAL26090001",
                        DlvExpectedDate = DateTime.Today.AddDays(5),
                        RONo = "RO-PDI-2609-0012",
                        ROCreatedDate = DateTime.Now.AddDays(-2),
                        ROFinishedDate = DateTime.Now.AddDays(-1),
                        ROStatus = "COMPLETED",
                        Status = PdiRequestDtlStatus.Approved,
                        InspectionResult = "PASSED",
                        Remark = "PDI hoàn tất 100 hạng mục kỹ thuật, lắp phụ kiện dán phim Lumbar chính hãng đạt chuẩn"
                    },
                    new PdiRequestDetail
                    {
                        DlrPdiReqNo = "2609PRN00001",
                        Vin = "KMHE281BBSA334455",
                        Model = "Creta 1.5 Cao Cấp",
                        ModelCode = "SU2b",
                        SpecCode = "CR15-PRE-02",
                        ColorCode = "R3R",
                        ColorName = "Đỏ mận / Nâu",
                        DlrContractNo = "RC26090001",
                        CtrCarId = "RC26090001.02",
                        CustomerName = "Trần Đình Trọng",
                        CustomerPhone = "0912345678",
                        DealNo = "DEAL26090001",
                        DlvExpectedDate = DateTime.Today.AddDays(5),
                        RONo = "RO-PDI-2609-0013",
                        ROCreatedDate = DateTime.Now.AddDays(-2),
                        ROFinishedDate = DateTime.Now.AddDays(-1),
                        ROStatus = "COMPLETED",
                        Status = PdiRequestDtlStatus.Approved,
                        InspectionResult = "PASSED",
                        Remark = "PDI hoàn tất không phát hiện lỗi kỹ thuật"
                    }
                }
            };

            var pdi2 = new PdiRequest
            {
                OrgId = orgId,
                DlrPdiReqNo = "2609PRN00002",
                DealerCode = "VN002",
                DealerName = "Hyundai Nam Trung",
                FlagAccessory = false,
                Status = PdiRequestStatus.Pending,
                TotalCars = 1,
                Remark = "Đại lý Nam Trung gửi đề nghị nghiệm thu PDI xe Custin giao doanh nghiệp",
                CreatedBy = "DEALER_USER",
                CreatedDate = DateTime.Now.AddDays(-1),
                Details = new List<PdiRequestDetail>
                {
                    new PdiRequestDetail
                    {
                        DlrPdiReqNo = "2609PRN00002",
                        Vin = "KMHE281BBSA556677",
                        Model = "Custin 1.5T-GDi Cao Cấp",
                        ModelCode = "KU",
                        SpecCode = "CU15-PRE-01",
                        ColorCode = "SL1",
                        ColorName = "Bạc / Be xám",
                        DlrContractNo = "RC26090002",
                        CtrCarId = "RC26090002.01",
                        CustomerName = "Công ty TNHH Vận tải Du lịch Sao Mai",
                        CustomerPhone = "0904888999",
                        DealNo = "DEAL26090003",
                        DlvExpectedDate = DateTime.Today.AddDays(8),
                        RONo = null,
                        ROCreatedDate = null,
                        ROFinishedDate = null,
                        ROStatus = "NORE",
                        Status = PdiRequestDtlStatus.Pending,
                        InspectionResult = null,
                        Remark = "Đang kiểm tra hệ thống phanh ABS, cân bằng điện tử ESP và áp suất lốp trước khi xuất bãi"
                    }
                }
            };

            var pdi3 = new PdiRequest
            {
                OrgId = orgId,
                DlrPdiReqNo = "2609PRN00003",
                DealerCode = "VN001",
                DealerName = "Hyundai Đông Đô",
                FlagAccessory = false,
                Status = PdiRequestStatus.Cancelled,
                TotalCars = 1,
                Remark = "Yêu cầu PDI hủy do xe bị trầy xước nhẹ trong quá trình bốc dỡ cần sơn lại tại xưởng sơn",
                CancelledDate = DateTime.Now.AddDays(-2),
                CancelledBy = "DEALER_PDI_STAFF",
                CancelReason = "Xe bị cọ xát nhẹ cản trước khi hạ sàn xe chuyên dùng, cần sơn lại trước khi tạo phiếu PDI mới",
                CreatedBy = "DEALER_PDI_STAFF",
                CreatedDate = DateTime.Now.AddDays(-4),
                Details = new List<PdiRequestDetail>
                {
                    new PdiRequestDetail
                    {
                        DlrPdiReqNo = "2609PRN00003",
                        Vin = "KMHE281BBSA667788",
                        Model = "Accent 1.4 AT",
                        ModelCode = "HC",
                        SpecCode = "AC14-AT-01",
                        ColorCode = "WH1",
                        ColorName = "Trắng tuyết",
                        DlrContractNo = "RC26090003",
                        CtrCarId = "RC26090003.01",
                        CustomerName = "Lê Hoàng Yến",
                        CustomerPhone = "0987654321",
                        DealNo = null,
                        DlvExpectedDate = DateTime.Today.AddDays(12),
                        RONo = "RO-PAINT-2609-08",
                        ROCreatedDate = DateTime.Now.AddDays(-3),
                        ROFinishedDate = null,
                        ROStatus = "OPEN",
                        Status = PdiRequestDtlStatus.Cancelled,
                        InspectionResult = "FAILED",
                        Remark = "Xước cản trước bên phụ, chuyển xưởng làm đồng sơn"
                    }
                }
            };

            db.PdiRequests.AddRange(pdi1, pdi2, pdi3);
            await db.SaveChangesAsync();
        }

        if (!await db.PaymentDiscounts.AnyAsync(o => o.OrgId == orgId))
        {
            var pd1 = new PaymentDiscount
            {
                OrgId = orgId,
                PaymentDiscountNo = "20260901-001/DNCK/VN001",
                DealerCode = "VN001",
                DealerName = "Hyundai Đông Đô",
                CompanyName = "Công ty Cổ phần Ô tô Đông Đô",
                DateEndFrom = DateTime.Today.AddDays(-25),
                DateEndTo = DateTime.Today.AddDays(10),
                QtyCar = 2,
                SumTotalDiscountPrice = 10450000m,
                DiscountPercent = 0.5m,
                PenaltyPercent = 0.05m,
                Status = PaymentDiscountStatus.Signed,
                DealerSignStatus = PaymentDiscountSignStatus.Signed,
                DealerSignDate = DateTime.Now.AddDays(-6),
                DealerSignBy = "Nguyễn Văn Hùng (Tổng Giám Đốc ĐL)",
                DealerSignFile = "DNCK_SIGNED_DL_20260901-001_DNCK_VN001.pdf",
                HQSignStatus = PaymentDiscountSignStatus.Signed,
                HQApproveDate = DateTime.Now.AddDays(-5),
                HQApproveBy = "Trần Trọng Nghĩa (Trưởng phòng Tài chính NPP)",
                HQSignDate = DateTime.Now.AddDays(-4),
                HQSignBy = "Lê Hoàng Quân (Tổng Giám Đốc NPP)",
                HQSignFile = "DNCK_FINAL_SIGNED_20260901-001_DNCK_VN001.pdf",
                Remark = "Quyết toán chiết khấu thanh toán đợt tháng 9/2026 cho 02 xe Santa Fe & Creta thanh toán sớm 15 ngày so với hạn bảo lãnh ngân hàng Vietcombank",
                CreatedBy = "DEALER_ACCOUNTANT",
                CreatedAt = DateTime.Now.AddDays(-8),
                LUDateTime = DateTime.Now.AddDays(-4),
                LUBy = "Lê Hoàng Quân (Tổng Giám Đốc NPP)",
                Details = new List<PaymentDiscountDetail>
                {
                    new PaymentDiscountDetail
                    {
                        PaymentDiscountNo = "20260901-001/DNCK/VN001",
                        CarId = "CAR2026-SF0988",
                        Vin = "KMHE281BBSA129841",
                        Model = "Santa Fe 2.5 HTRAC",
                        SpecCode = "SF25-PRE-01",
                        SpecDescription = "Santa Fe 2.5 xăng cao cấp HTRAC",
                        SOCode = "ORD2603010001",
                        GuaranteeNo = "BG2603-VCB-00128",
                        BankGuaranteeNo = "BL-VCB-20260301-88",
                        BankCode = "VCB",
                        DateOpen = DateTime.Today.AddDays(-30),
                        Term = 30,
                        DateStart = DateTime.Today.AddDays(-25),
                        DateEnd = DateTime.Today.AddDays(5),
                        UnitPrice = 1350000000m,
                        UnitPriceActual = 1350000000m,
                        GuaranteeValue = 1350000000m,
                        PaymentEndDatePhase1 = DateTime.Today.AddDays(-10),
                        AmountPhase1 = 1350000000m,
                        DiscountDateNumberPhase1 = 15,
                        DiscountPercentPhase1 = 0.5m,
                        DiscountPricePhase1 = 6750000m,
                        TotalAmount = 1350000000m,
                        TotalDiscountPrice = 6750000m,
                        Remark = "Thanh toán đợt 1 toàn bộ 100% trước hạn bảo lãnh 15 ngày"
                    },
                    new PaymentDiscountDetail
                    {
                        PaymentDiscountNo = "20260901-001/DNCK/VN001",
                        CarId = "CAR2026-CR0192",
                        Vin = "KMHE281BBSA334455",
                        Model = "Creta 1.5 Cao Cấp",
                        SpecCode = "CR15-PRE-02",
                        SpecDescription = "Creta 1.5 CVT bản cao cấp",
                        SOCode = "ORD2603010001",
                        GuaranteeNo = "BG2603-VCB-00128",
                        BankGuaranteeNo = "BL-VCB-20260301-88",
                        BankCode = "VCB",
                        DateOpen = DateTime.Today.AddDays(-30),
                        Term = 30,
                        DateStart = DateTime.Today.AddDays(-25),
                        DateEnd = DateTime.Today.AddDays(5),
                        UnitPrice = 740000000m,
                        UnitPriceActual = 740000000m,
                        GuaranteeValue = 740000000m,
                        PaymentEndDatePhase1 = DateTime.Today.AddDays(-10),
                        AmountPhase1 = 740000000m,
                        DiscountDateNumberPhase1 = 15,
                        DiscountPercentPhase1 = 0.5m,
                        DiscountPricePhase1 = 3700000m,
                        TotalAmount = 740000000m,
                        TotalDiscountPrice = 3700000m,
                        Remark = "Thanh toán sớm đủ điều kiện hưởng mức chiết khấu tối đa"
                    }
                }
            };

            var pd2 = new PaymentDiscount
            {
                OrgId = orgId,
                PaymentDiscountNo = "20260915-001/DNCK/VN002",
                DealerCode = "VN002",
                DealerName = "Hyundai Nam Trung",
                CompanyName = "Công ty Cổ phần Ô tô Nam Trung",
                DateEndFrom = DateTime.Today.AddDays(-10),
                DateEndTo = DateTime.Today.AddDays(20),
                QtyCar = 1,
                SumTotalDiscountPrice = 4300000m,
                DiscountPercent = 0.5m,
                PenaltyPercent = 0.05m,
                Status = PaymentDiscountStatus.Approved,
                DealerSignStatus = PaymentDiscountSignStatus.Signed,
                DealerSignDate = DateTime.Now.AddDays(-2),
                DealerSignBy = "Phạm Tuấn Vũ (Giám đốc ĐL Nam Trung)",
                DealerSignFile = "DNCK_SIGNED_DL_20260915-001_DNCK_VN002.pdf",
                HQSignStatus = PaymentDiscountSignStatus.Pending,
                HQApproveDate = DateTime.Now.AddDays(-1),
                HQApproveBy = "Nguyễn Thị Phương (Phòng Tài chính NPP)",
                Remark = "Đề nghị chiết khấu thanh toán xe Tucson 2.0 AT mở bảo lãnh LC ngân hàng BIDV, NPP đã thẩm tra đạt tiêu chuẩn, chờ Lãnh đạo ký số phê duyệt",
                CreatedBy = "DEALER_ACCOUNTANT",
                CreatedAt = DateTime.Now.AddDays(-3),
                LUDateTime = DateTime.Now.AddDays(-1),
                LUBy = "Nguyễn Thị Phương (Phòng Tài chính NPP)",
                Details = new List<PaymentDiscountDetail>
                {
                    new PaymentDiscountDetail
                    {
                        PaymentDiscountNo = "20260915-001/DNCK/VN002",
                        CarId = "CAR2026-TU1102",
                        Vin = "KMHE281BBSA987654",
                        Model = "Tucson 2.0 AT",
                        SpecCode = "TU20-STD-01",
                        SpecDescription = "Tucson 2.0 xăng tiêu chuẩn",
                        SOCode = "ORD2603010002",
                        GuaranteeNo = "LC2603-BIDV-0099",
                        BankGuaranteeNo = "LC-BIDV-20260315",
                        BankCode = "BIDV",
                        DateOpen = DateTime.Today.AddDays(-15),
                        Term = 30,
                        DateStart = DateTime.Today.AddDays(-10),
                        DateEnd = DateTime.Today.AddDays(20),
                        UnitPrice = 860000000m,
                        UnitPriceActual = 860000000m,
                        GuaranteeValue = 860000000m,
                        PaymentEndDatePhase1 = DateTime.Today.AddDays(-3),
                        AmountPhase1 = 860000000m,
                        DiscountDateNumberPhase1 = 12,
                        DiscountPercentPhase1 = 0.5m,
                        DiscountPricePhase1 = 4300000m,
                        TotalAmount = 860000000m,
                        TotalDiscountPrice = 4300000m,
                        Remark = "Thanh toán đợt 1 toàn bộ số tiền xe Tucson"
                    }
                }
            };

            var pd3 = new PaymentDiscount
            {
                OrgId = orgId,
                PaymentDiscountNo = "20260922-002/DNCK/VN001",
                DealerCode = "VN001",
                DealerName = "Hyundai Đông Đô",
                CompanyName = "Công ty Cổ phần Ô tô Đông Đô",
                DateEndFrom = DateTime.Today,
                DateEndTo = DateTime.Today.AddDays(28),
                QtyCar = 1,
                SumTotalDiscountPrice = 2450000m,
                DiscountPercent = 0.5m,
                PenaltyPercent = 0.05m,
                Status = PaymentDiscountStatus.Draft,
                DealerSignStatus = PaymentDiscountSignStatus.Pending,
                HQSignStatus = PaymentDiscountSignStatus.Pending,
                Remark = "Đại lý lập dự thảo đề nghị chiết khấu thanh toán xe Accent mở bảo lãnh MB Bank, đang đối chiếu chứng từ trước khi trình ký số",
                CreatedBy = "DEALER_ACCOUNTANT",
                CreatedAt = DateTime.Now,
                Details = new List<PaymentDiscountDetail>
                {
                    new PaymentDiscountDetail
                    {
                        PaymentDiscountNo = "20260922-002/DNCK/VN001",
                        CarId = "CAR2026-AC0088",
                        Vin = "KMHE281BBSA667788",
                        Model = "Accent 1.4 AT",
                        SpecCode = "AC14-AT-01",
                        SpecDescription = "Accent 1.4 AT bản đặc biệt",
                        SOCode = "ORD2603010003",
                        GuaranteeNo = "BG2609-MB-00331",
                        BankGuaranteeNo = "BL-MB-20260920-12",
                        BankCode = "MB",
                        DateOpen = DateTime.Today.AddDays(-5),
                        Term = 30,
                        DateStart = DateTime.Today.AddDays(-2),
                        DateEnd = DateTime.Today.AddDays(28),
                        UnitPrice = 490000000m,
                        UnitPriceActual = 490000000m,
                        GuaranteeValue = 490000000m,
                        PaymentEndDatePhase1 = DateTime.Today,
                        AmountPhase1 = 490000000m,
                        DiscountDateNumberPhase1 = 28,
                        DiscountPercentPhase1 = 0.5m,
                        DiscountPricePhase1 = 2450000m,
                        TotalAmount = 490000000m,
                        TotalDiscountPrice = 2450000m,
                        Remark = "Thanh toán ngay trong tuần đầu sau khi phát hành bảo lãnh"
                    }
                }
            };

            var pd4 = new PaymentDiscount
            {
                OrgId = orgId,
                PaymentDiscountNo = "20260910-001/DNCK/VN001",
                DealerCode = "VN001",
                DealerName = "Hyundai Đông Đô",
                CompanyName = "Công ty Cổ phần Ô tô Đông Đô",
                DateEndFrom = DateTime.Today.AddDays(-20),
                DateEndTo = DateTime.Today.AddDays(5),
                QtyCar = 1,
                SumTotalDiscountPrice = 0m,
                DiscountPercent = 0.5m,
                PenaltyPercent = 0.05m,
                Status = PaymentDiscountStatus.Cancelled,
                CancelReason = "Đại lý phát sinh chậm thanh toán đợt 2 quá thời hạn quy định, không đủ điều kiện hưởng chiết khấu thanh toán sớm",
                Remark = "Đề nghị bị hủy do quá hạn thanh toán bảo lãnh ngân hàng",
                CreatedBy = "DEALER_ACCOUNTANT",
                CreatedAt = DateTime.Now.AddDays(-12),
                LUDateTime = DateTime.Now.AddDays(-7),
                LUBy = "NPP_FINANCE_DIRECTOR",
                Details = new List<PaymentDiscountDetail>
                {
                    new PaymentDiscountDetail
                    {
                        PaymentDiscountNo = "20260910-001/DNCK/VN001",
                        CarId = "CAR2026-CU0211",
                        Vin = "KMHE281BBSA556677",
                        Model = "Custin 1.5T-GDi Cao Cấp",
                        SpecCode = "CU15-PRE-01",
                        SpecDescription = "Custin 1.5T cao cấp",
                        GuaranteeNo = "BG2603-VCB-00128",
                        BankGuaranteeNo = "BL-VCB-20260301-88",
                        BankCode = "VCB",
                        DateOpen = DateTime.Today.AddDays(-30),
                        Term = 30,
                        DateStart = DateTime.Today.AddDays(-25),
                        DateEnd = DateTime.Today.AddDays(5),
                        UnitPrice = 850000000m,
                        UnitPriceActual = 850000000m,
                        GuaranteeValue = 850000000m,
                        PaymentEndDatePhase1 = DateTime.Today.AddDays(-5),
                        AmountPhase1 = 850000000m,
                        DiscountDateNumberPhase1 = 0,
                        DiscountPercentPhase1 = 0m,
                        DiscountPricePhase1 = 0m,
                        TotalAmount = 850000000m,
                        TotalDiscountPrice = 0m,
                        Remark = "Đã quá hạn ngày thanh toán đợt hưởng ưu đãi"
                    }
                }
            };

            db.PaymentDiscounts.AddRange(pd1, pd2, pd3, pd4);
            await db.SaveChangesAsync();
        }

        if (!await db.DealerContractCancelMinutes.AnyAsync(o => o.OrgId == orgId))
        {
            // Đảm bảo tồn tại các DealerContract liên quan phục vụ demo và test
            if (!await db.DealerContracts.AnyAsync(o => o.OrgId == orgId && o.ContractNo == "2603DRC00004"))
            {
                db.DealerContracts.Add(new DealerContract
                {
                    OrgId = orgId,
                    ContractNo = "2603DRC00004",
                    DealerCode = "VN002",
                    DealerName = "Hyundai Nam Trung",
                    ContractDate = DateTime.Today.AddDays(-6),
                    ContractType = DealerContractType.Standard,
                    PaymentType = DealerContractPaymentType.Cash,
                    BankCode = "DEALER",
                    DepositPercent = 10m,
                    TotalCars = 1,
                    TotalAmount = 620000000m,
                    Status = DealerContractStatus.Signed,
                    DealerSignStatus = DealerContractSignStatus.Signed,
                    HQSignStatus = DealerContractSignStatus.Signed,
                    DealerSignedBy = "Lê Hồng Quân (Đại diện Nam Trung)",
                    DealerSignedAt = DateTime.Now.AddDays(-5),
                    HQSignedBy = "Phạm Quang Minh (Phó TGĐ Phân phối HTC)",
                    HQSignedAt = DateTime.Now.AddDays(-5),
                    Remark = "Hợp đồng mua buôn 01 xe Stargazer X",
                    CreatedBy = "DEALER_SALES_ADMIN",
                    CreatedAt = DateTime.Now.AddDays(-6),
                    Details = new List<DealerContractDetail>
                    {
                        new DealerContractDetail
                        {
                            CarId = "CAR2026-SG0109",
                            Vin = "KMHE281BBSA778899",
                            Model = "Stargazer X 1.5 Cao Cấp",
                            SpecCode = "SG15-PRE-01",
                            ColorCode = "MB1",
                            ProductionYear = 2026,
                            UnitPrice = 563636364m,
                            VatRate = 10m,
                            TotalAmount = 620000000m,
                            OrderNo = "ORD2603150002",
                            Remark = "Xe phân bổ tháng 3"
                        }
                    }
                });
            }

            if (!await db.DealerContracts.AnyAsync(o => o.OrgId == orgId && o.ContractNo == "2603DRC00005"))
            {
                db.DealerContracts.Add(new DealerContract
                {
                    OrgId = orgId,
                    ContractNo = "2603DRC00005",
                    DealerCode = "VN001",
                    DealerName = "Hyundai Đông Đô",
                    ContractDate = DateTime.Today.AddDays(-14),
                    ContractType = DealerContractType.Standard,
                    PaymentType = DealerContractPaymentType.Guarantee,
                    BankCode = "VCB",
                    BankName = "Vietcombank Thăng Long",
                    DepositPercent = 10m,
                    TotalCars = 1,
                    TotalAmount = 769000000m,
                    Status = DealerContractStatus.Cancelled,
                    DealerSignStatus = DealerContractSignStatus.Signed,
                    HQSignStatus = DealerContractSignStatus.Signed,
                    DealerSignedBy = "Nguyễn Văn Hưng (Giám đốc Đại lý Đông Đô)",
                    DealerSignedAt = DateTime.Now.AddDays(-13),
                    HQSignedBy = "Phạm Quang Minh (Phó TGĐ Phân phối HTC)",
                    HQSignedAt = DateTime.Now.AddDays(-12),
                    CancelledAt = DateTime.Now.AddDays(-5),
                    CancelReason = "Hủy theo Biên bản thanh lý / hủy HĐ số 2603DRC00005.CM01 đã ký hoàn tất 2 bên. Lý do: Hai bên thỏa thuận thanh lý chấm dứt hợp đồng do nhà máy tạm ngừng phiên bản sản xuất",
                    Remark = "Hợp đồng mua buôn xe Elantra đã được thanh lý theo biên bản hủy",
                    CreatedBy = "DEALER_SALES_ADMIN",
                    CreatedAt = DateTime.Now.AddDays(-14),
                    Details = new List<DealerContractDetail>
                    {
                        new DealerContractDetail
                        {
                            CarId = "CAR2026-EL0402",
                            Vin = "KMHE281BBSA445566",
                            Model = "Elantra 2.0 AT Cao Cấp",
                            SpecCode = "EL20-PRE-01",
                            ColorCode = "BK1",
                            ProductionYear = 2026,
                            UnitPrice = 699090909m,
                            VatRate = 10m,
                            TotalAmount = 769000000m,
                            OrderNo = "ORD2603010001",
                            Remark = "Xe thuộc hợp đồng đã thanh lý"
                        }
                    }
                });
            }

            if (!await db.DealerContracts.AnyAsync(o => o.OrgId == orgId && o.ContractNo == "2603DRC00006"))
            {
                db.DealerContracts.Add(new DealerContract
                {
                    OrgId = orgId,
                    ContractNo = "2603DRC00006",
                    DealerCode = "VN001",
                    DealerName = "Hyundai Đông Đô",
                    ContractDate = DateTime.Today.AddDays(-2),
                    ContractType = DealerContractType.Standard,
                    PaymentType = DealerContractPaymentType.Cash,
                    BankCode = "DEALER",
                    DepositPercent = 10m,
                    TotalCars = 1,
                    TotalAmount = 1589000000m,
                    Status = DealerContractStatus.Signed,
                    DealerSignStatus = DealerContractSignStatus.Signed,
                    HQSignStatus = DealerContractSignStatus.Signed,
                    DealerSignedBy = "Nguyễn Văn Hưng (Giám đốc Đại lý Đông Đô)",
                    DealerSignedAt = DateTime.Now.AddDays(-1),
                    HQSignedBy = "Phạm Quang Minh (Phó TGĐ Phân phối HTC)",
                    HQSignedAt = DateTime.Now.AddDays(-1),
                    Remark = "Hợp đồng mua buôn 01 xe Palisade Exclusive 7 chỗ",
                    CreatedBy = "DEALER_SALES_ADMIN",
                    CreatedAt = DateTime.Now.AddDays(-2),
                    Details = new List<DealerContractDetail>
                    {
                        new DealerContractDetail
                        {
                            CarId = "CAR2026-PL0991",
                            Vin = "KMHE281BBSA998877",
                            Model = "Palisade 2.2D Exclusive",
                            SpecCode = "PL22-EXC-01",
                            ColorCode = "WH1",
                            ProductionYear = 2026,
                            UnitPrice = 1444545455m,
                            VatRate = 10m,
                            TotalAmount = 1589000000m,
                            OrderNo = "ORD2603010001",
                            Remark = "Xe kế hoạch phân bổ tháng 3"
                        }
                    }
                });
            }

            await db.SaveChangesAsync();

            // Seed Biên bản thanh lý / hủy hợp đồng bán buôn
            var cm1 = new DealerContractCancelMinutes
            {
                OrgId = orgId,
                CancelMinutesNo = "2603DRC00005.CM01",
                ContractNo = "2603DRC00005",
                DealerCode = "VN001",
                DealerName = "Hyundai Đông Đô",
                ContractDate = DateTime.Today.AddDays(-14),
                TotalCars = 1,
                TotalAmount = 769000000m,
                Remark = "Hai bên thỏa thuận thanh lý chấm dứt hợp đồng do nhà máy tạm ngừng phiên bản sản xuất",
                CancelMinutesStatus = DealerContractCancelMinutesStatus.Signed,
                DealerSignStatus = DealerContractCancelMinutesSignStatus.Approved,
                HQSignStatus = DealerContractCancelMinutesSignStatus.Approved2,
                DealerSignedAt = DateTime.Now.AddDays(-7),
                DealerSignedBy = "Nguyễn Văn Hưng (Giám đốc Đại lý Đông Đô)",
                HQAppr1At = DateTime.Now.AddDays(-6),
                HQAppr1By = "Trần Tuấn Kiệt (Chuyên viên Thẩm định NPP)",
                HQAppr2At = DateTime.Now.AddDays(-5),
                HQAppr2By = "Phạm Quang Minh (Phó TGĐ Phân phối HTC)",
                SignedFilePath = "/storage/cancel_minutes/2603DRC00005.CM01_Full_Signed.pdf",
                CreatedBy = "DEALER_SALES_ADMIN",
                CreatedAt = DateTime.Now.AddDays(-8),
                LUDateTime = DateTime.Now.AddDays(-5),
                LUBy = "Phạm Quang Minh (Phó TGĐ Phân phối HTC)"
            };

            var cm2 = new DealerContractCancelMinutes
            {
                OrgId = orgId,
                CancelMinutesNo = "2603DRC00004.CM01",
                ContractNo = "2603DRC00004",
                DealerCode = "VN002",
                DealerName = "Hyundai Nam Trung",
                ContractDate = DateTime.Today.AddDays(-6),
                TotalCars = 1,
                TotalAmount = 620000000m,
                Remark = "Đại lý xin rút hợp đồng xe Stargazer do đối tác dừng dự án kinh doanh dịch vụ vận tải, NPP chuyên viên đã thẩm tra chuyển Lãnh đạo ký số",
                CancelMinutesStatus = DealerContractCancelMinutesStatus.Draft,
                DealerSignStatus = DealerContractCancelMinutesSignStatus.Approved,
                HQSignStatus = DealerContractCancelMinutesSignStatus.Approved1,
                DealerSignedAt = DateTime.Now.AddDays(-2),
                DealerSignedBy = "Lê Hồng Quân (Đại diện Nam Trung)",
                HQAppr1At = DateTime.Now.AddDays(-1),
                HQAppr1By = "Trần Tuấn Kiệt (Chuyên viên Thẩm định NPP)",
                SignedFilePath = "/storage/cancel_minutes/2603DRC00004.CM01_DL_Signed.pdf",
                CreatedBy = "DEALER_SALES_ADMIN",
                CreatedAt = DateTime.Now.AddDays(-3),
                LUDateTime = DateTime.Now.AddDays(-1),
                LUBy = "Trần Tuấn Kiệt (Chuyên viên Thẩm định NPP)"
            };

            var cm3 = new DealerContractCancelMinutes
            {
                OrgId = orgId,
                CancelMinutesNo = "2603DRC00006.CM01",
                ContractNo = "2603DRC00006",
                DealerCode = "VN001",
                DealerName = "Hyundai Đông Đô",
                ContractDate = DateTime.Today.AddDays(-2),
                TotalCars = 1,
                TotalAmount = 1589000000m,
                Remark = "Đại lý lập dự thảo biên bản thanh lý hợp đồng bán buôn xe Palisade đang hoàn thiện hồ sơ trình ký",
                CancelMinutesStatus = DealerContractCancelMinutesStatus.Draft,
                DealerSignStatus = DealerContractCancelMinutesSignStatus.Pending,
                HQSignStatus = DealerContractCancelMinutesSignStatus.Pending,
                CreatedBy = "DEALER_SALES_ADMIN",
                CreatedAt = DateTime.Now
            };

            db.DealerContractCancelMinutes.AddRange(cm1, cm2, cm3);
            await db.SaveChangesAsync();
        }

        if (!await db.GuaranteeClaims.AnyAsync(o => o.OrgId == orgId))
        {
            var gcl1 = new PaymentGuaranteeClaim
            {
                OrgId = orgId,
                GrtClaimNo = "CV-260320-001/VN001",
                DealerCode = "VN001",
                DealerName = "Hyundai Đông Đô",
                GrtClaimTypeCode = "YC",
                FlagisHTC = "1",
                BankCode = "VCB",
                BankName = "Vietcombank Thăng Long",
                BankCodeMonitor = "VCB",
                BankAccountNo = "0011004123899",
                TotalCars = 2,
                TotalClaimAmount = 2090000000m,
                TotalGuaranteeValue = 2090000000m,
                Status = GuaranteeClaimStatus.Approved,
                VinSignStatus = ClaimVinSignStatus.Approved,
                SignDate = DateTime.Now.AddDays(-2),
                SignBy = "Phạm Quang Minh (Phó TGĐ Phân phối HTC)",
                FileName = "CV-260320-001_VN001_Signed.pdf",
                FileSigned = "/storage/grtclaim/CV-260320-001_VN001_Signed.pdf",
                Remark = "Công văn yêu cầu Ngân hàng Vietcombank thực hiện nghĩa vụ bảo lãnh thanh toán cho 02 xe ô tô Santa Fe và Creta theo Thư bảo lãnh số BL-VCB-20260301-88 do đại lý quá hạn thanh toán đợt 2",
                CreatedBy = "DEALER_SALES_ADMIN",
                CreatedDate = DateTime.Today.AddDays(-3),
                CreatedDateTime = DateTime.Now.AddDays(-3),
                LUDateTime = DateTime.Now.AddDays(-2),
                LUBy = "Phạm Quang Minh (Phó TGĐ Phân phối HTC)",
                Details = new List<PaymentGuaranteeClaimDetail>
                {
                    new PaymentGuaranteeClaimDetail
                    {
                        GrtClaimNo = "CV-260320-001/VN001",
                        CarId = "CAR2026-SF0988",
                        Vin = "KMHE281BBSA129841",
                        Model = "Santa Fe 2.5 HTRAC",
                        ModelCode = "TM",
                        SpecCode = "SF25-PRE-01",
                        SpecDescription = "Santa Fe 2.5 xăng cao cấp HTRAC",
                        ColorCode = "WW2",
                        ColorName = "Trắng ngọc trai / Đen",
                        ContractNo = "2603DRC00001",
                        FlagDealerContractDMS40 = true,
                        GuaranteeNo = "BG2603-VCB-00128",
                        BankGuaranteeNo = "BL-VCB-20260301-88",
                        BankCode = "VCB",
                        BankName = "Vietcombank Thăng Long",
                        BankCodeMonitor = "VCB",
                        DateOpen = DateTime.Today.AddDays(-30),
                        DateStart = DateTime.Today.AddDays(-25),
                        DateEnd = DateTime.Today.AddDays(5),
                        UnitPriceActual = 1350000000m,
                        GuaranteeValue = 1350000000m,
                        ClaimAmount = 1350000000m,
                        Status = ClaimVinSignStatus.Approved,
                        Remark = "Xe quá hạn thanh toán theo cam kết bảo lãnh"
                    },
                    new PaymentGuaranteeClaimDetail
                    {
                        GrtClaimNo = "CV-260320-001/VN001",
                        CarId = "CAR2026-CR0192",
                        Vin = "KMHE281BBSA334455",
                        Model = "Creta 1.5 Cao Cấp",
                        ModelCode = "SU2b",
                        SpecCode = "CR15-PRE-02",
                        SpecDescription = "Creta 1.5 CVT bản cao cấp",
                        ColorCode = "R3R",
                        ColorName = "Đỏ mận / Nâu",
                        ContractNo = "2603DRC00001",
                        FlagDealerContractDMS40 = true,
                        GuaranteeNo = "BG2603-VCB-00128",
                        BankGuaranteeNo = "BL-VCB-20260301-88",
                        BankCode = "VCB",
                        BankName = "Vietcombank Thăng Long",
                        BankCodeMonitor = "VCB",
                        DateOpen = DateTime.Today.AddDays(-30),
                        DateStart = DateTime.Today.AddDays(-25),
                        DateEnd = DateTime.Today.AddDays(5),
                        UnitPriceActual = 740000000m,
                        GuaranteeValue = 740000000m,
                        ClaimAmount = 740000000m,
                        Status = ClaimVinSignStatus.Approved,
                        Remark = "Trích thu tiền bảo lãnh xe Creta thanh toán cho HTC"
                    }
                }
            };

            var gcl2 = new PaymentGuaranteeClaim
            {
                OrgId = orgId,
                GrtClaimNo = "CV-260322-001/VN002",
                DealerCode = "VN002",
                DealerName = "Hyundai Nam Trung",
                GrtClaimTypeCode = "DN",
                FlagisHTC = "2",
                BankCode = "BIDV",
                BankName = "BIDV Hà Nội",
                BankCodeMonitor = "BIDV",
                BankAccountNo = "12410000678912",
                TotalCars = 1,
                TotalClaimAmount = 620000000m,
                TotalGuaranteeValue = 620000000m,
                Status = GuaranteeClaimStatus.Pending,
                VinSignStatus = ClaimVinSignStatus.Pending,
                Remark = "Đại lý Nam Trung chủ động đề nghị Ngân hàng BIDV trích tiền từ hạn mức bảo lãnh BL-BIDV-20260305-12 thanh toán lô xe Stargazer X cho Nhà phân phối HTCLD",
                CreatedBy = "DEALER_SALES_ADMIN",
                CreatedDate = DateTime.Today.AddDays(-1),
                CreatedDateTime = DateTime.Now.AddDays(-1),
                Details = new List<PaymentGuaranteeClaimDetail>
                {
                    new PaymentGuaranteeClaimDetail
                    {
                        GrtClaimNo = "CV-260322-001/VN002",
                        CarId = "CAR2026-SG0109",
                        Vin = "KMHE281BBSA778899",
                        Model = "Stargazer X 1.5 Cao Cấp",
                        ModelCode = "KS",
                        SpecCode = "SG15-PRE-01",
                        SpecDescription = "Stargazer X 1.5 IVT bản cao cấp",
                        ColorCode = "MB1",
                        ColorName = "Xám từ tính / Đen",
                        ContractNo = "2603DRC00004",
                        FlagDealerContractDMS40 = true,
                        GuaranteeNo = "BG2603-BIDV-00130",
                        BankGuaranteeNo = "BL-BIDV-20260305-12",
                        BankCode = "BIDV",
                        BankName = "BIDV Hà Nội",
                        BankCodeMonitor = "BIDV",
                        DateOpen = DateTime.Today.AddDays(-20),
                        DateStart = DateTime.Today.AddDays(-18),
                        DateEnd = DateTime.Today.AddDays(12),
                        UnitPriceActual = 620000000m,
                        GuaranteeValue = 620000000m,
                        ClaimAmount = 620000000m,
                        Status = ClaimVinSignStatus.Pending,
                        Remark = "Đại lý đề nghị thanh toán giải tỏa bảo lãnh để nhận hồ sơ gốc"
                    }
                }
            };

            var gcl3 = new PaymentGuaranteeClaim
            {
                OrgId = orgId,
                GrtClaimNo = "CV-260318-001/VN001",
                DealerCode = "VN001",
                DealerName = "Hyundai Đông Đô",
                GrtClaimTypeCode = "YC",
                FlagisHTC = "1",
                BankCode = "TCB",
                BankName = "Techcombank Hoàn Kiếm",
                BankCodeMonitor = "TCB",
                BankAccountNo = "19033445566778",
                TotalCars = 1,
                TotalClaimAmount = 850000000m,
                TotalGuaranteeValue = 850000000m,
                Status = GuaranteeClaimStatus.Rejected,
                VinSignStatus = ClaimVinSignStatus.Cancelled,
                RejectReason = "Đại lý đã xuất trình ủy nhiệm chi chuyển khoản thanh toán trực tiếp trước thời điểm ký duyệt công văn đòi bảo lãnh",
                RejectDateTime = DateTime.Now.AddDays(-4),
                RejectBy = "Lê Thị Thu Thủy (Trưởng phòng Tài vụ HTC)",
                Remark = "Công văn đòi tiền bảo lãnh từ chối duyệt do đối tác đại lý đã hoàn tất thanh toán tiền mặt/chuyển khoản",
                CreatedBy = "DEALER_SALES_ADMIN",
                CreatedDate = DateTime.Today.AddDays(-5),
                CreatedDateTime = DateTime.Now.AddDays(-5),
                LUDateTime = DateTime.Now.AddDays(-4),
                LUBy = "Lê Thị Thu Thủy (Trưởng phòng Tài vụ HTC)",
                Details = new List<PaymentGuaranteeClaimDetail>
                {
                    new PaymentGuaranteeClaimDetail
                    {
                        GrtClaimNo = "CV-260318-001/VN001",
                        CarId = "CAR2026-CU0211",
                        Vin = "KMHE281BBSA556677",
                        Model = "Custin 1.5T-GDi Cao Cấp",
                        ModelCode = "KU",
                        SpecCode = "CU15-PRE-01",
                        SpecDescription = "Custin 1.5T máy xăng tăng áp cao cấp",
                        ColorCode = "GY1",
                        ColorName = "Xám kim loại / Đen",
                        ContractNo = "2603DRC00002",
                        FlagDealerContractDMS40 = true,
                        GuaranteeNo = "BG2603-TCB-00129",
                        BankGuaranteeNo = "BL-TCB-20260310-09",
                        BankCode = "TCB",
                        BankName = "Techcombank Hoàn Kiếm",
                        BankCodeMonitor = "TCB",
                        DateOpen = DateTime.Today.AddDays(-28),
                        DateStart = DateTime.Today.AddDays(-22),
                        DateEnd = DateTime.Today.AddDays(8),
                        UnitPriceActual = 850000000m,
                        GuaranteeValue = 850000000m,
                        ClaimAmount = 850000000m,
                        Status = ClaimVinSignStatus.Cancelled,
                        Remark = "Đã thu tiền thanh toán trực tiếp, không gọi bảo lãnh ngân hàng"
                    }
                }
            };

            var gcl4 = new PaymentGuaranteeClaim
            {
                OrgId = orgId,
                GrtClaimNo = "CV-260315-001/VN001",
                DealerCode = "VN001",
                DealerName = "Hyundai Đông Đô",
                GrtClaimTypeCode = "YC",
                FlagisHTC = "1",
                BankCode = "VCB",
                BankName = "Vietcombank Thăng Long",
                BankCodeMonitor = "VCB",
                BankAccountNo = "0011004123899",
                TotalCars = 1,
                TotalClaimAmount = 490000000m,
                TotalGuaranteeValue = 490000000m,
                Status = GuaranteeClaimStatus.Cancelled,
                VinSignStatus = ClaimVinSignStatus.Cancelled,
                CancelReason = "Đại lý và NPP đã ký biên bản gia hạn thời hạn bảo lãnh theo công văn CVGH số 260315-001/CVGH/VN001",
                CancelDateTime = DateTime.Now.AddDays(-7),
                CancelBy = "DEALER_SALES_ADMIN",
                Remark = "Hủy yêu cầu đòi tiền bảo lãnh xe Accent do được phê duyệt gia hạn thêm 30 ngày",
                CreatedBy = "DEALER_SALES_ADMIN",
                CreatedDate = DateTime.Today.AddDays(-10),
                CreatedDateTime = DateTime.Now.AddDays(-10),
                LUDateTime = DateTime.Now.AddDays(-7),
                LUBy = "DEALER_SALES_ADMIN",
                Details = new List<PaymentGuaranteeClaimDetail>
                {
                    new PaymentGuaranteeClaimDetail
                    {
                        GrtClaimNo = "CV-260315-001/VN001",
                        CarId = "CAR2026-AC0512",
                        Vin = "KMHE281BBSA667788",
                        Model = "Accent 1.5 AT Cao Cấp",
                        ModelCode = "BN7",
                        SpecCode = "AC15-PRE-01",
                        SpecDescription = "Accent 1.5 số tự động bản cao cấp",
                        ColorCode = "WH1",
                        ColorName = "Trắng tuyết / Đen",
                        ContractNo = "2603DRC00003",
                        FlagDealerContractDMS40 = true,
                        GuaranteeNo = "BG2603-VCB-00128",
                        BankGuaranteeNo = "BL-VCB-20260301-88",
                        BankCode = "VCB",
                        BankName = "Vietcombank Thăng Long",
                        BankCodeMonitor = "VCB",
                        DateOpen = DateTime.Today.AddDays(-30),
                        DateStart = DateTime.Today.AddDays(-25),
                        DateEnd = DateTime.Today.AddDays(5),
                        UnitPriceActual = 490000000m,
                        GuaranteeValue = 490000000m,
                        ClaimAmount = 490000000m,
                        Status = ClaimVinSignStatus.Cancelled,
                        Remark = "Hủy dòng xe đòi bảo lãnh do đã gia hạn hạn mức"
                    }
                }
            };

            db.GuaranteeClaims.AddRange(gcl1, gcl2, gcl3, gcl4);
            await db.SaveChangesAsync();
        }

        if (!await db.DealerContractCancelBankMDs.AnyAsync(o => o.OrgId == orgId))
        {
            // Đảm bảo tồn tại các DealerContract liên quan phục vụ demo đề nghị hủy chọn ngân hàng bảo lãnh
            if (!await db.DealerContracts.AnyAsync(o => o.OrgId == orgId && o.ContractNo == "2603DRC00007"))
            {
                db.DealerContracts.Add(new DealerContract
                {
                    OrgId = orgId,
                    ContractNo = "2603DRC00007",
                    DealerCode = "VN001",
                    DealerName = "Hyundai Đông Đô",
                    ContractDate = DateTime.Today.AddDays(-15),
                    ContractType = DealerContractType.Standard,
                    PaymentType = DealerContractPaymentType.Guarantee,
                    BankCode = null, // Đã được gỡ sau khi hoàn tất đề nghị hủy ngân hàng 2603DRC00007.CB01
                    BankName = null,
                    DepositPercent = 10m,
                    GuaranteeDays = 15,
                    TotalCars = 2,
                    TotalAmount = 1680000000m,
                    Status = DealerContractStatus.Signed,
                    DealerSignStatus = DealerContractSignStatus.Signed,
                    HQSignStatus = DealerContractSignStatus.Signed,
                    DealerSignedBy = "Nguyễn Văn Hưng (Giám đốc Đại lý Đông Đô)",
                    DealerSignedAt = DateTime.Now.AddDays(-14),
                    HQSignedBy = "Phạm Quang Minh (Phó TGĐ Phân phối HTC)",
                    HQSignedAt = DateTime.Now.AddDays(-13),
                    Remark = "Đã hủy liên kết ngân hàng bảo lãnh theo biên bản 2603DRC00007.CB01 ngày " + DateTime.Now.AddDays(-5).ToString("yyyy-MM-dd HH:mm") + ". Có thể cập nhật lại ngân hàng bảo lãnh mới hoặc đổi phương thức thanh toán.",
                    CreatedBy = "DEALER_SALES_ADMIN",
                    CreatedAt = DateTime.Now.AddDays(-15),
                    Details = new List<DealerContractDetail>
                    {
                        new DealerContractDetail
                        {
                            CarId = "CAR2026-TC0081",
                            Vin = "KMHE281BBSA889901",
                            Model = "Tucson 2.0 Xăng Đặc Biệt",
                            SpecCode = "TC20-SPE-01",
                            ColorCode = "WH1",
                            ProductionYear = 2026,
                            UnitPrice = 909090909m,
                            VatRate = 10m,
                            TotalAmount = 1000000000m,
                            OrderNo = "ORD2603120005",
                            Remark = "Xe phân bổ tháng 3"
                        },
                        new DealerContractDetail
                        {
                            CarId = "CAR2026-AC0515",
                            Vin = "KMHE281BBSA889902",
                            Model = "Accent 1.5 AT Đặc Biệt",
                            SpecCode = "AC15-SPE-01",
                            ColorCode = "BK1",
                            ProductionYear = 2026,
                            UnitPrice = 618181818m,
                            VatRate = 10m,
                            TotalAmount = 680000000m,
                            OrderNo = "ORD2603120006",
                            Remark = "Xe phân bổ tháng 3"
                        }
                    }
                });
            }

            if (!await db.DealerContracts.AnyAsync(o => o.OrgId == orgId && o.ContractNo == "2603DRC00008"))
            {
                db.DealerContracts.Add(new DealerContract
                {
                    OrgId = orgId,
                    ContractNo = "2603DRC00008",
                    DealerCode = "VN002",
                    DealerName = "Hyundai Nam Trung",
                    ContractDate = DateTime.Today.AddDays(-8),
                    ContractType = DealerContractType.Standard,
                    PaymentType = DealerContractPaymentType.Guarantee,
                    BankCode = "BIDV",
                    BankName = "BIDV Cầu Giấy",
                    DepositPercent = 10m,
                    GuaranteeDays = 15,
                    TotalCars = 1,
                    TotalAmount = 850000000m,
                    Status = DealerContractStatus.Signed,
                    DealerSignStatus = DealerContractSignStatus.Signed,
                    HQSignStatus = DealerContractSignStatus.Signed,
                    DealerSignedBy = "Lê Hồng Quân (Đại diện Nam Trung)",
                    DealerSignedAt = DateTime.Now.AddDays(-7),
                    HQSignedBy = "Phạm Quang Minh (Phó TGĐ Phân phối HTC)",
                    HQSignedAt = DateTime.Now.AddDays(-7),
                    Remark = "Hợp đồng mua buôn 01 xe Custin, thanh toán bảo lãnh ngân hàng BIDV Cầu Giấy",
                    CreatedBy = "DEALER_SALES_ADMIN",
                    CreatedAt = DateTime.Now.AddDays(-8),
                    Details = new List<DealerContractDetail>
                    {
                        new DealerContractDetail
                        {
                            CarId = "CAR2026-CU0215",
                            Vin = "KMHE281BBSA990011",
                            Model = "Custin 1.5T-GDi Tiêu Chuẩn",
                            SpecCode = "CU15-STD-01",
                            ColorCode = "WH1",
                            ProductionYear = 2026,
                            UnitPrice = 772727273m,
                            VatRate = 10m,
                            TotalAmount = 850000000m,
                            OrderNo = "ORD2603140003",
                            Remark = "Xe phân bổ đợt 2"
                        }
                    }
                });
            }

            if (!await db.DealerContracts.AnyAsync(o => o.OrgId == orgId && o.ContractNo == "2603DRC00009"))
            {
                db.DealerContracts.Add(new DealerContract
                {
                    OrgId = orgId,
                    ContractNo = "2603DRC00009",
                    DealerCode = "VN003",
                    DealerName = "Hyundai Tây Hồ",
                    ContractDate = DateTime.Today.AddDays(-5),
                    ContractType = DealerContractType.Standard,
                    PaymentType = DealerContractPaymentType.Guarantee,
                    BankCode = "VPB",
                    BankName = "VPBank Thăng Long",
                    DepositPercent = 10m,
                    GuaranteeDays = 15,
                    TotalCars = 1,
                    TotalAmount = 740000000m,
                    Status = DealerContractStatus.Signed,
                    DealerSignStatus = DealerContractSignStatus.Signed,
                    HQSignStatus = DealerContractSignStatus.Signed,
                    DealerSignedBy = "Vũ Đình Trọng (Giám đốc Đại lý Tây Hồ)",
                    DealerSignedAt = DateTime.Now.AddDays(-4),
                    HQSignedBy = "Phạm Quang Minh (Phó TGĐ Phân phối HTC)",
                    HQSignedAt = DateTime.Now.AddDays(-4),
                    Remark = "Hợp đồng mua buôn 01 xe Creta Cao Cấp, đại lý gắn bảo lãnh VPBank",
                    CreatedBy = "DEALER_SALES_ADMIN",
                    CreatedAt = DateTime.Now.AddDays(-5),
                    Details = new List<DealerContractDetail>
                    {
                        new DealerContractDetail
                        {
                            CarId = "CAR2026-CR0195",
                            Vin = "KMHE281BBSA990022",
                            Model = "Creta 1.5 Cao Cấp",
                            SpecCode = "CR15-PRE-02",
                            ColorCode = "BK1",
                            ProductionYear = 2026,
                            UnitPrice = 672727273m,
                            VatRate = 10m,
                            TotalAmount = 740000000m,
                            OrderNo = "ORD2603160001",
                            Remark = "Xe phân bổ tháng 3"
                        }
                    }
                });
            }

            await db.SaveChangesAsync();

            var cb1 = new DealerContractCancelBankMD
            {
                OrgId = orgId,
                CancelBankMDNo = "2603DRC00007.CB01",
                ContractNo = "2603DRC00007",
                DealerCode = "VN001",
                DealerName = "Hyundai Đông Đô",
                BankCodeMD = "VCB",
                BankName = "Vietcombank Thăng Long",
                TotalCars = 2,
                TotalAmount = 1680000000m,
                Status = DealerContractCancelBankMDStatus.Finished,
                RemarkDlr = "Đại lý xin hủy chọn ngân hàng Vietcombank do hết hạn mức cấp tín dụng cho quý 1, xin chuyển sang bảo lãnh BIDV",
                RemarkBank = "Ngân hàng Vietcombank xác nhận hợp đồng chưa phát hành thư bảo lãnh chính thức và đồng ý giải phóng liên kết",
                RemarkHQ = "NPP phê duyệt hoàn tất hủy chọn ngân hàng bảo lãnh, đã gỡ liên kết BankCodeMD trên hợp đồng",
                CreatedBy = "DEALER_SALES_ADMIN",
                CreatedAt = DateTime.Now.AddDays(-7),
                BankApprovedAt = DateTime.Now.AddDays(-6),
                BankApprovedBy = "Nguyễn Thu Hà (Chuyên viên Tín dụng VCB)",
                FinishedAt = DateTime.Now.AddDays(-5),
                FinishedBy = "Phạm Quang Minh (Phó TGĐ Phân phối HTC)",
                LUDateTime = DateTime.Now.AddDays(-5),
                LUBy = "Phạm Quang Minh (Phó TGĐ Phân phối HTC)"
            };

            var cb2 = new DealerContractCancelBankMD
            {
                OrgId = orgId,
                CancelBankMDNo = "2603DRC00008.CB01",
                ContractNo = "2603DRC00008",
                DealerCode = "VN002",
                DealerName = "Hyundai Nam Trung",
                BankCodeMD = "BIDV",
                BankName = "BIDV Cầu Giấy",
                TotalCars = 1,
                TotalAmount = 850000000m,
                Status = DealerContractCancelBankMDStatus.BankApproved,
                RemarkDlr = "Đại lý đề nghị hủy liên kết bảo lãnh BIDV để chuyển thanh toán trả thẳng bằng tiền mặt / chuyển khoản",
                RemarkBank = "BIDV Cầu Giấy đồng ý không phát hành bảo lãnh cho hợp đồng 2603DRC00008",
                CreatedBy = "DEALER_SALES_ADMIN",
                CreatedAt = DateTime.Now.AddDays(-3),
                BankApprovedAt = DateTime.Now.AddDays(-2),
                BankApprovedBy = "Trần Mạnh Cường (Trưởng phòng KHDN BIDV Cầu Giấy)",
                LUDateTime = DateTime.Now.AddDays(-2),
                LUBy = "Trần Mạnh Cường (Trưởng phòng KHDN BIDV Cầu Giấy)"
            };

            var cb3 = new DealerContractCancelBankMD
            {
                OrgId = orgId,
                CancelBankMDNo = "2603DRC00009.CB01",
                ContractNo = "2603DRC00009",
                DealerCode = "VN003",
                DealerName = "Hyundai Tây Hồ",
                BankCodeMD = "VPB",
                BankName = "VPBank Thăng Long",
                TotalCars = 1,
                TotalAmount = 740000000m,
                Status = DealerContractCancelBankMDStatus.Pending,
                RemarkDlr = "Đại lý xin thay đổi ngân hàng bảo lãnh sang VietinBank do chính sách lãi suất ưu đãi hơn",
                CreatedBy = "DEALER_SALES_ADMIN",
                CreatedAt = DateTime.Now.AddDays(-1),
                LUDateTime = DateTime.Now.AddDays(-1),
                LUBy = "DEALER_SALES_ADMIN"
            };

            var cb4 = new DealerContractCancelBankMD
            {
                OrgId = orgId,
                CancelBankMDNo = "2603DRC00009.CB02",
                ContractNo = "2603DRC00009",
                DealerCode = "VN003",
                DealerName = "Hyundai Tây Hồ",
                BankCodeMD = "VPB",
                BankName = "VPBank Thăng Long",
                TotalCars = 1,
                TotalAmount = 740000000m,
                Status = DealerContractCancelBankMDStatus.Rejected,
                RemarkDlr = "Đại lý xin rút bảo lãnh ngân hàng VPBank",
                RejectReason = "Đại lý chưa gửi kèm công văn giải trình lý do hủy chọn ngân hàng và xác nhận số dư khả dụng",
                RejectedAt = DateTime.Now.AddDays(-3),
                RejectedBy = "Lê Thị Thu Thủy (Trưởng phòng Tài vụ HTC)",
                CreatedBy = "DEALER_SALES_ADMIN",
                CreatedAt = DateTime.Now.AddDays(-4),
                LUDateTime = DateTime.Now.AddDays(-3),
                LUBy = "Lê Thị Thu Thủy (Trưởng phòng Tài vụ HTC)"
            };

            var cb5 = new DealerContractCancelBankMD
            {
                OrgId = orgId,
                CancelBankMDNo = "2603DRC00008.CB02",
                ContractNo = "2603DRC00008",
                DealerCode = "VN002",
                DealerName = "Hyundai Nam Trung",
                BankCodeMD = "BIDV",
                BankName = "BIDV Cầu Giấy",
                TotalCars = 1,
                TotalAmount = 850000000m,
                Status = DealerContractCancelBankMDStatus.Cancelled,
                RemarkDlr = "Đại lý xin đổi sang Vietcombank",
                CancelReason = "Đại lý chủ động rút đề nghị do ngân hàng BIDV đã gia hạn thêm hạn mức cấp bảo lãnh cho đại lý",
                CancelledAt = DateTime.Now.AddDays(-4),
                CancelledBy = "DEALER_SALES_ADMIN",
                CreatedBy = "DEALER_SALES_ADMIN",
                CreatedAt = DateTime.Now.AddDays(-5),
                LUDateTime = DateTime.Now.AddDays(-4),
                LUBy = "DEALER_SALES_ADMIN"
            };

            db.DealerContractCancelBankMDs.AddRange(cb1, cb2, cb3, cb4, cb5);
            await db.SaveChangesAsync();
        }

        if (!await db.BankBillMinutes.AnyAsync(o => o.OrgId == orgId))
        {
            var bbm1 = new BankBillMinutes
            {
                OrgId = orgId,
                BankBillMnNo = "2603BBM00001",
                BankCode = "VCB",
                BankName = "Ngân hàng TMCP Ngoại thương Việt Nam (Vietcombank Thăng Long)",
                DealerCode = "VN001",
                DealerName = "Hyundai Đông Đô",
                BankBillDate = DateTime.Today.AddDays(-12),
                BankBillReceiveDate = DateTime.Today.AddDays(-10),
                BankBillPrintDate = DateTime.Today.AddDays(-12),
                Status = BankBillMinutesStatus.Settled,
                TotalCars = 2,
                TotalClaimAmount = 2060000000m,
                BankOfficer = "Nguyễn Thu Hà (Chuyên viên Tín dụng VCB Thăng Long)",
                HTCOfficer = "Trần Văn Bình (Chuyên viên Tài vụ HTC)",
                Remark = "Biên bản bàn giao bộ chứng từ hối phiếu gốc lô xe Santa Fe & Creta đòi tiền theo bảo lãnh Vietcombank đã giải ngân hoàn tất",
                HandoverAt = DateTime.Now.AddDays(-11),
                HandoverBy = "Trần Văn Bình (Chuyên viên Tài vụ HTC)",
                BankReceivedAt = DateTime.Now.AddDays(-10),
                BankReceivedBy = "Nguyễn Thu Hà (Chuyên viên Tín dụng VCB Thăng Long)",
                SettledAt = DateTime.Now.AddDays(-8),
                SettledBy = "Lê Thị Thu Thủy (Trưởng phòng Tài vụ HTC)",
                CreatedBy = "Trần Văn Bình (Chuyên viên Tài vụ HTC)",
                CreatedAt = DateTime.Now.AddDays(-12),
                Details = new List<BankBillMinutesDetail>
                {
                    new BankBillMinutesDetail
                    {
                        BankBillMnNo = "2603BBM00001",
                        CarId = "CAR2026-SF0988",
                        Vin = "KMHE281BBSA129841",
                        Model = "Santa Fe 2.5 HTRAC",
                        SpecCode = "SF25-PRE-01",
                        SpecDescription = "Santa Fe 2.5 xăng cao cấp 2 cầu",
                        ColorCode = "WW2",
                        EngineNo = "G4KP-102941",
                        ContractNo = "2603DRC00001",
                        BankGuaranteeNo = "BL-VCB-20260301-88",
                        GuaranteeBankCode = "VCB",
                        GuaranteeDateOpen = DateTime.Today.AddDays(-30),
                        HTCInvoiceNo = "HD-HTC-2603-0012",
                        TransportMinutesNo = "BBBG-2603-0001",
                        CQNo = "CQ2026-SF-012984",
                        CONo = "CO2026-VN-98412",
                        ClaimAmount = 1320000000m,
                        Status = BankBillMinutesDetailStatus.Settled,
                        Remark = "Đã nhận tiền giải ngân hối phiếu ngân hàng"
                    },
                    new BankBillMinutesDetail
                    {
                        BankBillMnNo = "2603BBM00001",
                        CarId = "CAR2026-CR0192",
                        Vin = "KMHE281BBSA334455",
                        Model = "Creta 1.5 Cao Cấp",
                        SpecCode = "CR15-PRE-02",
                        SpecDescription = "Creta 1.5 CVT bản cao cấp",
                        ColorCode = "R3R",
                        EngineNo = "G4FL-334455",
                        ContractNo = "2603DRC00001",
                        BankGuaranteeNo = "BL-VCB-20260301-88",
                        GuaranteeBankCode = "VCB",
                        GuaranteeDateOpen = DateTime.Today.AddDays(-30),
                        HTCInvoiceNo = "HD-HTC-2603-0013",
                        TransportMinutesNo = "BBBG-2603-0002",
                        CQNo = "CQ2026-CR-033445",
                        CONo = "CO2026-VN-33445",
                        ClaimAmount = 740000000m,
                        Status = BankBillMinutesDetailStatus.Settled,
                        Remark = "Đã nhận tiền giải ngân hối phiếu ngân hàng"
                    }
                }
            };

            var bbm2 = new BankBillMinutes
            {
                OrgId = orgId,
                BankBillMnNo = "2603BBM00002",
                BankCode = "BIDV",
                BankName = "Ngân hàng TMCP Đầu tư và Phát triển Việt Nam (BIDV Cầu Giấy)",
                DealerCode = "VN002",
                DealerName = "Hyundai Nam Trung",
                BankBillDate = DateTime.Today.AddDays(-4),
                BankBillReceiveDate = DateTime.Today.AddDays(-2),
                BankBillPrintDate = DateTime.Today.AddDays(-4),
                Status = BankBillMinutesStatus.BankReceived,
                TotalCars = 1,
                TotalClaimAmount = 620000000m,
                BankOfficer = "Trần Mạnh Cường (Trưởng phòng KHDN BIDV Cầu Giấy)",
                HTCOfficer = "Đỗ Thị Quỳnh (Chuyên viên QLHĐ & Hồ sơ HTC)",
                Remark = "Ngân hàng BIDV đã tiếp nhận đầy đủ bộ hồ sơ xe gốc kèm hối phiếu xe Stargazer X, đang làm thủ tục giải ngân tất toán",
                HandoverAt = DateTime.Now.AddDays(-3),
                HandoverBy = "Đỗ Thị Quỳnh (Chuyên viên QLHĐ & Hồ sơ HTC)",
                BankReceivedAt = DateTime.Now.AddDays(-2),
                BankReceivedBy = "Trần Mạnh Cường (Trưởng phòng KHDN BIDV Cầu Giấy)",
                CreatedBy = "Đỗ Thị Quỳnh (Chuyên viên QLHĐ & Hồ sơ HTC)",
                CreatedAt = DateTime.Now.AddDays(-4),
                Details = new List<BankBillMinutesDetail>
                {
                    new BankBillMinutesDetail
                    {
                        BankBillMnNo = "2603BBM00002",
                        CarId = "CAR2026-SG0109",
                        Vin = "KMHE281BBSA778899",
                        Model = "Stargazer X 1.5 Cao Cấp",
                        SpecCode = "SG15-PRE-01",
                        SpecDescription = "Stargazer X 1.5 IVT bản cao cấp",
                        ColorCode = "MB1",
                        EngineNo = "G4FL-778899",
                        ContractNo = "2603DRC00004",
                        BankGuaranteeNo = "BL-BIDV-20260305-12",
                        GuaranteeBankCode = "BIDV",
                        GuaranteeDateOpen = DateTime.Today.AddDays(-20),
                        HTCInvoiceNo = "HD-HTC-2603-0021",
                        TransportMinutesNo = "BBBG-2603-0005",
                        CQNo = "CQ2026-SG-077889",
                        CONo = "CO2026-VN-77889",
                        ClaimAmount = 620000000m,
                        Status = BankBillMinutesDetailStatus.Received,
                        Remark = "Ngân hàng đã ký tiếp nhận hồ sơ gốc"
                    }
                }
            };

            var bbm3 = new BankBillMinutes
            {
                OrgId = orgId,
                BankBillMnNo = "2603BBM00003",
                BankCode = "TCB",
                BankName = "Ngân hàng TMCP Kỹ thương Việt Nam (Techcombank Hoàn Kiếm)",
                DealerCode = "VN001",
                DealerName = "Hyundai Đông Đô",
                BankBillDate = DateTime.Today.AddDays(-2),
                BankBillPrintDate = DateTime.Today.AddDays(-2),
                Status = BankBillMinutesStatus.Handover,
                TotalCars = 1,
                TotalClaimAmount = 850000000m,
                BankOfficer = "Hoàng Văn Tuấn (Cán bộ QHKH TCB)",
                HTCOfficer = "Trần Văn Bình (Chuyên viên Tài vụ HTC)",
                Remark = "Đã bàn giao bộ chứng từ hối phiếu xe Custin sang Techcombank Hoàn Kiếm, đang chờ cán bộ ngân hàng kiểm đếm và ký xác nhận tiếp nhận",
                HandoverAt = DateTime.Now.AddDays(-1),
                HandoverBy = "Trần Văn Bình (Chuyên viên Tài vụ HTC)",
                CreatedBy = "Trần Văn Bình (Chuyên viên Tài vụ HTC)",
                CreatedAt = DateTime.Now.AddDays(-2),
                Details = new List<BankBillMinutesDetail>
                {
                    new BankBillMinutesDetail
                    {
                        BankBillMnNo = "2603BBM00003",
                        CarId = "CAR2026-CU0211",
                        Vin = "KMHE281BBSA556677",
                        Model = "Custin 1.5T-GDi Cao Cấp",
                        SpecCode = "CU15-PRE-01",
                        SpecDescription = "Custin 1.5T máy xăng tăng áp cao cấp",
                        ColorCode = "GY1",
                        EngineNo = "G4FS-556677",
                        ContractNo = "2603DRC00002",
                        BankGuaranteeNo = "BL-TCB-20260310-09",
                        GuaranteeBankCode = "TCB",
                        GuaranteeDateOpen = DateTime.Today.AddDays(-28),
                        HTCInvoiceNo = "HD-HTC-2603-0025",
                        TransportMinutesNo = "BBBG-2603-0008",
                        CQNo = "CQ2026-CU-055667",
                        CONo = "CO2026-VN-55667",
                        ClaimAmount = 850000000m,
                        Status = BankBillMinutesDetailStatus.Pending,
                        Remark = "Hồ sơ đã chuyển giao sang phòng giao dịch Techcombank"
                    }
                }
            };

            var bbm4 = new BankBillMinutes
            {
                OrgId = orgId,
                BankBillMnNo = "2603BBM00004",
                BankCode = "VPB",
                BankName = "Ngân hàng TMCP Việt Nam Thịnh Vượng (VPBank Thăng Long)",
                DealerCode = "VN003",
                DealerName = "Hyundai Tây Hồ",
                BankBillDate = DateTime.Today,
                Status = BankBillMinutesStatus.Draft,
                TotalCars = 1,
                TotalClaimAmount = 740000000m,
                Remark = "Biên bản bàn giao hối phiếu nháp xe Creta Cao Cấp, đang rà soát chứng từ hóa đơn và phiếu kiểm tra chất lượng xuất xưởng",
                CreatedBy = "Đỗ Thị Quỳnh (Chuyên viên QLHĐ & Hồ sơ HTC)",
                CreatedAt = DateTime.Now,
                Details = new List<BankBillMinutesDetail>
                {
                    new BankBillMinutesDetail
                    {
                        BankBillMnNo = "2603BBM00004",
                        CarId = "CAR2026-CR0195",
                        Vin = "KMHE281BBSA990022",
                        Model = "Creta 1.5 Cao Cấp",
                        SpecCode = "CR15-PRE-02",
                        SpecDescription = "Creta 1.5 CVT bản cao cấp",
                        ColorCode = "BK1",
                        EngineNo = "G4FL-990022",
                        ContractNo = "2603DRC00009",
                        BankGuaranteeNo = "BL-VPB-20260318-03",
                        GuaranteeBankCode = "VPB",
                        GuaranteeDateOpen = DateTime.Today.AddDays(-5),
                        HTCInvoiceNo = "HD-HTC-2603-0030",
                        TransportMinutesNo = "BBBG-2603-0011",
                        CQNo = "CQ2026-CR-099002",
                        CONo = "CO2026-VN-99002",
                        ClaimAmount = 740000000m,
                        Status = BankBillMinutesDetailStatus.Pending,
                        Remark = "Hồ sơ nháp đang hoàn thiện"
                    }
                }
            };

            var bbm5 = new BankBillMinutes
            {
                OrgId = orgId,
                BankBillMnNo = "2603BBM00005",
                BankCode = "BIDV",
                BankName = "Ngân hàng TMCP Đầu tư và Phát triển Việt Nam (BIDV Hà Nội)",
                DealerCode = "VN002",
                DealerName = "Hyundai Nam Trung",
                BankBillDate = DateTime.Today.AddDays(-10),
                Status = BankBillMinutesStatus.Cancelled,
                TotalCars = 1,
                TotalClaimAmount = 860000000m,
                CancelReason = "Đại lý Nam Trung đã chuyển khoản thanh toán trực tiếp 100% tiền mua xe Tucson qua tài khoản ngân hàng, không xuất trình hối phiếu đòi tiền ngân hàng bảo lãnh",
                CancelledAt = DateTime.Now.AddDays(-8),
                CancelledBy = "Trần Văn Bình (Chuyên viên Tài vụ HTC)",
                CreatedBy = "Trần Văn Bình (Chuyên viên Tài vụ HTC)",
                CreatedAt = DateTime.Now.AddDays(-10),
                Details = new List<BankBillMinutesDetail>
                {
                    new BankBillMinutesDetail
                    {
                        BankBillMnNo = "2603BBM00005",
                        CarId = "CAR2026-TU1102",
                        Vin = "KMHE281BBSA987654",
                        Model = "Tucson 2.0 AT",
                        SpecCode = "TU20-STD-01",
                        SpecDescription = "Tucson 2.0 số tự động tiêu chuẩn",
                        ColorCode = "NKA",
                        EngineNo = "G4NL-987654",
                        ContractNo = "2603DRC00002",
                        BankGuaranteeNo = "BL-BIDV-20260302-05",
                        GuaranteeBankCode = "BIDV",
                        GuaranteeDateOpen = DateTime.Today.AddDays(-25),
                        HTCInvoiceNo = "HD-HTC-2603-0008",
                        TransportMinutesNo = "BBBG-2603-0003",
                        CQNo = "CQ2026-TU-098765",
                        CONo = "CO2026-VN-98765",
                        ClaimAmount = 860000000m,
                        Status = BankBillMinutesDetailStatus.Cancelled,
                        Remark = "Đã hủy do đại lý thanh toán chuyển khoản trực tiếp"
                    }
                }
            };

            db.BankBillMinutes.AddRange(bbm1, bbm2, bbm3, bbm4, bbm5);
            await db.SaveChangesAsync();
        }

        if (!await db.TestCars.AnyAsync(o => o.OrgId == orgId))
        {
            var tc1 = new CarTestCar
            {
                OrgId = orgId,
                TestCarCode = "2603TC0001",
                DealerCode = "VN001",
                DealerName = "Hyundai Đông Đô",
                TestCarStatus = TestCarStatus.Finished,
                TotalCars = 2,
                TotalAmount = 2090000000m,
                Remark = "Đăng ký 02 xe Santa Fe & Creta làm xe lái thử showroom Đông Đô phục vụ chiến dịch lái thử trải nghiệm SUV Hyundai quý 1 & 2",
                CreatedBy = "DEALER_SALES_ADMIN",
                CreatedDate = DateTime.Today.AddDays(-30),
                ApprovedDate = DateTime.Today.AddDays(-28),
                ApprovedBy = "Lê Hoàng Quân (Trưởng phòng Bán hàng NPP)",
                FinishedDate = DateTime.Today.AddDays(-26),
                FinishedBy = "Phạm Quang Minh (Phó TGĐ Phân phối HTC)",
                LUDateTime = DateTime.Today.AddDays(-26),
                LUBy = "Phạm Quang Minh (Phó TGĐ Phân phối HTC)",
                Details = new List<CarTestCarDetail>
                {
                    new CarTestCarDetail
                    {
                        TestCarCode = "2603TC0001",
                        CarId = "CAR2026-SF0988",
                        Vin = "KMHE281BBSA129841",
                        Model = "Santa Fe 2.5 HTRAC",
                        ModelCode = "TM",
                        SpecCode = "SF25-PRE-01",
                        SpecDescription = "Santa Fe 2.5 xăng cao cấp dẫn động 4 bánh HTRAC",
                        ColorCode = "WW2",
                        ColorName = "Trắng ngọc trai",
                        EngineNo = "G4KP-102941",
                        SOCode = "ORD2603010001",
                        UnitPriceActual = 1350000000m,
                        EffDateStart = DateTime.Today.AddDays(-26),
                        EffDateEnd = DateTime.Today.AddDays(339),
                        FlagTestCar = true,
                        Status = TestCarDetailStatus.Finished,
                        Remark = "Xe demo showroom Đông Đô"
                    },
                    new CarTestCarDetail
                    {
                        TestCarCode = "2603TC0001",
                        CarId = "CAR2026-CR0192",
                        Vin = "KMHE281BBSA334455",
                        Model = "Creta 1.5 Cao Cấp",
                        ModelCode = "SU2",
                        SpecCode = "CR15-PRE-02",
                        SpecDescription = "Creta 1.5 CVT bản cao cấp",
                        ColorCode = "R3R",
                        ColorName = "Đỏ mận",
                        EngineNo = "G4FL-334411",
                        SOCode = "ORD2603010001",
                        UnitPriceActual = 740000000m,
                        EffDateStart = DateTime.Today.AddDays(-26),
                        EffDateEnd = DateTime.Today.AddDays(339),
                        FlagTestCar = true,
                        Status = TestCarDetailStatus.Finished,
                        Remark = "Xe lái thử đô thị B-SUV"
                    }
                }
            };

            var tc2 = new CarTestCar
            {
                OrgId = orgId,
                TestCarCode = "2603TC0002",
                DealerCode = "VN002",
                DealerName = "Hyundai Nam Trung",
                TestCarStatus = TestCarStatus.Approved,
                TotalCars = 1,
                TotalAmount = 950000000m,
                Remark = "Đăng ký xe Tucson 2.0 AT làm xe lái thử khách hàng tuyến đường dài và cao tốc",
                CreatedBy = "DEALER_SALES_ADMIN",
                CreatedDate = DateTime.Today.AddDays(-10),
                ApprovedDate = DateTime.Today.AddDays(-8),
                ApprovedBy = "Lê Hoàng Quân (Trưởng phòng Bán hàng NPP)",
                LUDateTime = DateTime.Today.AddDays(-8),
                LUBy = "Lê Hoàng Quân (Trưởng phòng Bán hàng NPP)",
                Details = new List<CarTestCarDetail>
                {
                    new CarTestCarDetail
                    {
                        TestCarCode = "2603TC0002",
                        CarId = "CAR2026-TU1102",
                        Vin = "KMHE281BBSA987654",
                        Model = "Tucson 2.0 AT",
                        ModelCode = "NX4",
                        SpecCode = "TU20-STD-01",
                        SpecDescription = "Tucson 2.0 máy xăng số tự động tiêu chuẩn",
                        ColorCode = "NKA",
                        ColorName = "Đen Phantom",
                        EngineNo = "G4NL-983102",
                        SOCode = "ORD2602150002",
                        UnitPriceActual = 950000000m,
                        EffDateStart = DateTime.Today.AddDays(-8),
                        EffDateEnd = DateTime.Today.AddDays(174),
                        FlagTestCar = false,
                        Status = TestCarDetailStatus.Approved,
                        Remark = "Đã duyệt Cấp 1 chuyên viên, chờ lãnh đạo NPP ký phê duyệt Cấp 2"
                    }
                }
            };

            var tc3 = new CarTestCar
            {
                OrgId = orgId,
                TestCarCode = "2603TC0003",
                DealerCode = "VN003",
                DealerName = "Hyundai Tây Hồ",
                TestCarStatus = TestCarStatus.Pending,
                TotalCars = 1,
                TotalAmount = 850000000m,
                Remark = "Đại lý Tây Hồ đăng ký mới xe Custin 1.5T làm xe lái thử gia đình MPV 7 chỗ cao cấp",
                CreatedBy = "DEALER_SALES_ADMIN",
                CreatedDate = DateTime.Today.AddDays(-2),
                LUDateTime = DateTime.Today.AddDays(-2),
                LUBy = "DEALER_SALES_ADMIN",
                Details = new List<CarTestCarDetail>
                {
                    new CarTestCarDetail
                    {
                        TestCarCode = "2603TC0003",
                        CarId = "CAR2026-CU0211",
                        Vin = "KMHE281BBSA556677",
                        Model = "Custin 1.5T-GDi Cao Cấp",
                        ModelCode = "KU",
                        SpecCode = "CU15-PRE-01",
                        SpecDescription = "Custin 1.5T máy xăng tăng áp cao cấp",
                        ColorCode = "GY1",
                        ColorName = "Xám kim loại",
                        EngineNo = "G4FS-556677",
                        SOCode = "ORD2603100001",
                        UnitPriceActual = 850000000m,
                        EffDateStart = DateTime.Today,
                        EffDateEnd = DateTime.Today.AddMonths(6),
                        FlagTestCar = false,
                        Status = TestCarDetailStatus.Pending,
                        Remark = "Hồ sơ mới gửi, chờ chuyên viên NPP thẩm tra"
                    }
                }
            };

            var tc4 = new CarTestCar
            {
                OrgId = orgId,
                TestCarCode = "2603TC0004",
                DealerCode = "VN001",
                DealerName = "Hyundai Đông Đô",
                TestCarStatus = TestCarStatus.Rejected,
                TotalCars = 1,
                TotalAmount = 620000000m,
                Remark = "Đăng ký xe Stargazer X 1.5 làm xe lái thử bổ sung cho chi nhánh thứ 2",
                RejectReason = "Đại lý đã sử dụng đủ định mức xe lái thử phân khúc MPV theo chính sách đại lý năm 2026 của NPP",
                RejectDate = DateTime.Today.AddDays(-5),
                RejectBy = "Lê Hoàng Quân (Trưởng phòng Bán hàng NPP)",
                CreatedBy = "DEALER_SALES_ADMIN",
                CreatedDate = DateTime.Today.AddDays(-7),
                LUDateTime = DateTime.Today.AddDays(-5),
                LUBy = "Lê Hoàng Quân (Trưởng phòng Bán hàng NPP)",
                Details = new List<CarTestCarDetail>
                {
                    new CarTestCarDetail
                    {
                        TestCarCode = "2603TC0004",
                        CarId = "CAR2026-SG0109",
                        Vin = "KMHE281BBSA778899",
                        Model = "Stargazer X 1.5 Cao Cấp",
                        ModelCode = "KS",
                        SpecCode = "SG15-PRE-01",
                        SpecDescription = "Stargazer X 1.5 IVT bản cao cấp",
                        ColorCode = "MB1",
                        ColorName = "Xám từ tính",
                        EngineNo = "G4FL-778899",
                        SOCode = "ORD2603050001",
                        UnitPriceActual = 620000000m,
                        EffDateStart = DateTime.Today.AddDays(-7),
                        EffDateEnd = DateTime.Today.AddDays(173),
                        FlagTestCar = false,
                        Status = TestCarDetailStatus.Rejected,
                        Remark = "Từ chối duyệt theo quy định định mức xe demo"
                    }
                }
            };

            var tc5 = new CarTestCar
            {
                OrgId = orgId,
                TestCarCode = "2603TC0005",
                DealerCode = "VN002",
                DealerName = "Hyundai Nam Trung",
                TestCarStatus = TestCarStatus.Cancelled,
                TotalCars = 1,
                TotalAmount = 540000000m,
                Remark = "Đăng ký xe Accent 1.5 AT làm xe demo",
                CancelReason = "Đại lý chủ động xin hủy đề nghị do xe này đã được khách hàng ký hợp đồng mua bán buôn theo lô",
                CancelledDate = DateTime.Today.AddDays(-12),
                CancelledBy = "DEALER_SALES_ADMIN",
                CreatedBy = "DEALER_SALES_ADMIN",
                CreatedDate = DateTime.Today.AddDays(-15),
                LUDateTime = DateTime.Today.AddDays(-12),
                LUBy = "DEALER_SALES_ADMIN",
                Details = new List<CarTestCarDetail>
                {
                    new CarTestCarDetail
                    {
                        TestCarCode = "2603TC0005",
                        CarId = "CAR2026-AC0088",
                        Vin = "KMHE281BBSA112233",
                        Model = "Accent 1.5 AT Đặc Biệt",
                        ModelCode = "BN7",
                        SpecCode = "AC15-SPE-01",
                        SpecDescription = "Accent 1.5 AT bản đặc biệt",
                        ColorCode = "WH1",
                        ColorName = "Trắng tuyết",
                        EngineNo = "G4FL-112233",
                        SOCode = "ORD2602100001",
                        UnitPriceActual = 540000000m,
                        EffDateStart = DateTime.Today.AddDays(-15),
                        EffDateEnd = DateTime.Today.AddDays(165),
                        FlagTestCar = false,
                        Status = TestCarDetailStatus.Cancelled,
                        Remark = "Đã hủy theo đề nghị đại lý"
                    }
                }
            };

            db.TestCars.AddRange(tc1, tc2, tc3, tc4, tc5);
            await db.SaveChangesAsync();
        }

        if (!await db.BodyRequests.AnyAsync(o => o.OrgId == orgId))
        {
            var cbr1 = new CarBodyRequest
            {
                OrgId = orgId,
                CBReqNo = "2603CBR000001",
                Status = CarBodyRequestStatus.Completed,
                TotalCars = 2,
                StorageCodeTo = "KHO_DT_01",
                StorageNameTo = "Xưởng Đóng Thùng Chuyên Dụng Hiệp Hòa",
                Remark = "Đóng thùng kín Inox 2 lớp tiêu chuẩn vận chuyển hàng bưu chính cho dòng Mighty EX8",
                CreatedDate = DateTime.Today.AddDays(-14),
                CreatedBy = "NPP_PLANNING_CV",
                ApprovedDate = DateTime.Today.AddDays(-13),
                ApprovedBy = "Vũ Thành Long (Phó Giám đốc Khối Xe Thương Mại NPP)",
                CompletedDate = DateTime.Today.AddDays(-2),
                CompletedBy = "Nguyễn Văn Hùng (Kỹ sư Giám sát Xưởng Đóng Thùng Hiệp Hòa)",
                LUDateTime = DateTime.Today.AddDays(-2),
                LUBy = "Nguyễn Văn Hùng (Kỹ sư Giám sát Xưởng Đóng Thùng Hiệp Hòa)",
                Details = new List<CarBodyRequestDetail>
                {
                    new CarBodyRequestDetail
                    {
                        CBReqNo = "2603CBR000001",
                        CarId = "CAR2026-EX8-881",
                        Vin = "KMHEC81BBSA600881",
                        Model = "Hyundai Mighty EX8 GT S2",
                        ModelCode = "EX8",
                        SpecCode = "EX8-CHASSIS-01",
                        SpecDescription = "Mighty EX8 GT S2 Sắt xi bản dài",
                        ColorCode = "WH1",
                        ColorName = "Trắng",
                        EngineNo = "D4CC-600881",
                        StorageCodeFrom = "KHO_TONG_HN",
                        StorageNameFrom = "Kho Tổng Hyundai Ninh Bình",
                        StorageCodeTo = "KHO_DT_01",
                        StorageNameTo = "Xưởng Đóng Thùng Chuyên Dụng Hiệp Hòa",
                        LoaiThung = "Thùng kín Inox",
                        ActualSpec = "Thùng kín Inox 304 dày 0.8mm dập sóng mở cửa hông",
                        TypeCB = "Y",
                        Status = CarBodyRequestDtlStatus.Completed,
                        InspectionResult = "PASSED - Đạt chuẩn an toàn kỹ thuật Cục Đăng Kiểm",
                        SerialNo = "CB-2026-HH-0118",
                        Remark = "Đã hoàn thành bàn giao nghiệm thu xuất xưởng"
                    },
                    new CarBodyRequestDetail
                    {
                        CBReqNo = "2603CBR000001",
                        CarId = "CAR2026-EX8-882",
                        Vin = "KMHEC81BBSA600882",
                        Model = "Hyundai Mighty EX8 GT S2",
                        ModelCode = "EX8",
                        SpecCode = "EX8-CHASSIS-01",
                        SpecDescription = "Mighty EX8 GT S2 Sắt xi bản dài",
                        ColorCode = "WH1",
                        ColorName = "Trắng",
                        EngineNo = "D4CC-600882",
                        StorageCodeFrom = "KHO_TONG_HN",
                        StorageNameFrom = "Kho Tổng Hyundai Ninh Bình",
                        StorageCodeTo = "KHO_DT_01",
                        StorageNameTo = "Xưởng Đóng Thùng Chuyên Dụng Hiệp Hòa",
                        LoaiThung = "Thùng kín Inox",
                        ActualSpec = "Thùng kín Inox 304 dày 0.8mm dập sóng mở cửa hông",
                        TypeCB = "Y",
                        Status = CarBodyRequestDtlStatus.Completed,
                        InspectionResult = "PASSED - Đạt chuẩn an toàn kỹ thuật Cục Đăng Kiểm",
                        SerialNo = "CB-2026-HH-0119",
                        Remark = "Đã hoàn thành bàn giao nghiệm thu xuất xưởng"
                    }
                }
            };

            var cbr2 = new CarBodyRequest
            {
                OrgId = orgId,
                CBReqNo = "2603CBR000002",
                Status = CarBodyRequestStatus.Approved,
                TotalCars = 1,
                StorageCodeTo = "KHO_DT_QUYENAUTO",
                StorageNameTo = "Xưởng Đóng Thùng Quyền Auto (Đông Lạnh)",
                Remark = "Gia công đóng thùng đông lạnh âm sâu (-18 độ C) vận chuyển dược phẩm vắc-xin cho xe New Porter H150",
                CreatedDate = DateTime.Today.AddDays(-6),
                CreatedBy = "NPP_PLANNING_CV",
                ApprovedDate = DateTime.Today.AddDays(-5),
                ApprovedBy = "Vũ Thành Long (Phó Giám đốc Khối Xe Thương Mại NPP)",
                LUDateTime = DateTime.Today.AddDays(-5),
                LUBy = "Vũ Thành Long (Phó Giám đốc Khối Xe Thương Mại NPP)",
                Details = new List<CarBodyRequestDetail>
                {
                    new CarBodyRequestDetail
                    {
                        CBReqNo = "2603CBR000002",
                        CarId = "CAR2026-H150-771",
                        Vin = "KMHEB81BBSA500771",
                        Model = "Hyundai New Porter H150",
                        ModelCode = "H150",
                        SpecCode = "H150-CHASSIS-02",
                        SpecDescription = "New Porter H150 Sắt xi cabin đơn",
                        ColorCode = "BL1",
                        ColorName = "Xanh Hyundai",
                        EngineNo = "D4CB-500771",
                        StorageCodeFrom = "KHO_TONG_SG",
                        StorageNameFrom = "Kho Tổng Nam Bộ Hiệp Phước",
                        StorageCodeTo = "KHO_DT_QUYENAUTO",
                        StorageNameTo = "Xưởng Đóng Thùng Quyền Auto (Đông Lạnh)",
                        LoaiThung = "Thùng đông lạnh",
                        ActualSpec = "Thùng Composite Foam PU cách nhiệt dày 80mm máy lạnh Thermal Master T1400",
                        TypeCB = "N",
                        Status = CarBodyRequestDtlStatus.Approved,
                        Remark = "Đang tiến hành lắp khung thùng và dàn lạnh tại xưởng Quyền Auto"
                    }
                }
            };

            var cbr3 = new CarBodyRequest
            {
                OrgId = orgId,
                CBReqNo = "2603CBR000003",
                Status = CarBodyRequestStatus.Pending,
                TotalCars = 1,
                StorageCodeTo = "KHO_DT_02",
                StorageNameTo = "Xưởng Đóng Thùng Ô Tô Nam Việt",
                Remark = "Đóng thùng mui bạt 5 bửng mở bọc tôn mạ kẽm cho xe tải trung Hyundai New Mighty 110SP",
                CreatedDate = DateTime.Today.AddDays(-1),
                CreatedBy = "DEALER_COMMERCIAL_ADMIN",
                LUDateTime = DateTime.Today.AddDays(-1),
                LUBy = "DEALER_COMMERCIAL_ADMIN",
                Details = new List<CarBodyRequestDetail>
                {
                    new CarBodyRequestDetail
                    {
                        CBReqNo = "2603CBR000003",
                        CarId = "CAR2026-110SP-661",
                        Vin = "KMHEE81BBSA700661",
                        Model = "Hyundai New Mighty 110SP",
                        ModelCode = "110SP",
                        SpecCode = "110SP-CHASSIS-01",
                        SpecDescription = "Mighty 110SP 7 tấn sắt xi",
                        ColorCode = "BL1",
                        ColorName = "Xanh Hyundai",
                        EngineNo = "D4GA-700661",
                        StorageCodeFrom = "KHO_TONG_HN",
                        StorageNameFrom = "Kho Tổng Hyundai Ninh Bình",
                        StorageCodeTo = "KHO_DT_02",
                        StorageNameTo = "Xưởng Đóng Thùng Ô Tô Nam Việt",
                        LoaiThung = "Thùng mui bạt",
                        ActualSpec = "Thùng mui bạt tiêu chuẩn 5 bửng bạt simili Hàn Quốc",
                        TypeCB = "N",
                        Status = CarBodyRequestDtlStatus.Pending,
                        Remark = "Chờ phê duyệt lệnh điều chuyển và chi phí đóng thùng"
                    }
                }
            };

            var cbr4 = new CarBodyRequest
            {
                OrgId = orgId,
                CBReqNo = "2603CBR000004",
                Status = CarBodyRequestStatus.Rejected,
                TotalCars = 1,
                StorageCodeTo = "KHO_DT_01",
                StorageNameTo = "Xưởng Đóng Thùng Chuyên Dụng Hiệp Hòa",
                Remark = "Yêu cầu đóng thùng lửng chở kính chuyên dụng cho xe Mighty W11S",
                RejectReason = "Bản vẽ thiết kế giá chữ A chở kính chưa được Cục Đăng Kiểm phê duyệt mẫu hồ sơ hoán cải",
                RejectDate = DateTime.Today.AddDays(-4),
                RejectBy = "Vũ Thành Long (Phó Giám đốc Khối Xe Thương Mại NPP)",
                CreatedDate = DateTime.Today.AddDays(-6),
                CreatedBy = "DEALER_COMMERCIAL_ADMIN",
                LUDateTime = DateTime.Today.AddDays(-4),
                LUBy = "Vũ Thành Long (Phó Giám đốc Khối Xe Thương Mại NPP)",
                Details = new List<CarBodyRequestDetail>
                {
                    new CarBodyRequestDetail
                    {
                        CBReqNo = "2603CBR000004",
                        CarId = "CAR2026-W11S-551",
                        Vin = "KMHEF81BBSA800551",
                        Model = "Hyundai Mighty W11S",
                        ModelCode = "W11S",
                        SpecCode = "W11S-CHASSIS-01",
                        SpecDescription = "Mighty W11S Tải trung sắt xi",
                        ColorCode = "WH1",
                        ColorName = "Trắng",
                        EngineNo = "D4GA-800551",
                        StorageCodeFrom = "KHO_TONG_SG",
                        StorageNameFrom = "Kho Tổng Nam Bộ Hiệp Phước",
                        StorageCodeTo = "KHO_DT_01",
                        StorageNameTo = "Xưởng Đóng Thùng Chuyên Dụng Hiệp Hòa",
                        LoaiThung = "Thùng lửng chuyên dụng",
                        ActualSpec = "Thùng lửng gắn giá chữ A chở kính",
                        TypeCB = "N",
                        Status = CarBodyRequestDtlStatus.Rejected,
                        Remark = "Hồ sơ hoán cải bị từ chối"
                    }
                }
            };

            var cbr5 = new CarBodyRequest
            {
                OrgId = orgId,
                CBReqNo = "2603CBR000005",
                Status = CarBodyRequestStatus.Cancelled,
                TotalCars = 1,
                StorageCodeTo = "KHO_DT_02",
                StorageNameTo = "Xưởng Đóng Thùng Ô Tô Nam Việt",
                Remark = "Đóng thùng kín Inox cho Mighty EX8 GTL",
                CancelReason = "Khách hàng đổi nhu cầu sang mua xe sắt xi tự về đóng thùng theo thiết kế chuyên dùng riêng của doanh nghiệp",
                CancelDate = DateTime.Today.AddDays(-8),
                CancelBy = "DEALER_COMMERCIAL_ADMIN",
                CreatedDate = DateTime.Today.AddDays(-10),
                CreatedBy = "DEALER_COMMERCIAL_ADMIN",
                LUDateTime = DateTime.Today.AddDays(-8),
                LUBy = "DEALER_COMMERCIAL_ADMIN",
                Details = new List<CarBodyRequestDetail>
                {
                    new CarBodyRequestDetail
                    {
                        CBReqNo = "2603CBR000005",
                        CarId = "CAR2026-EX8L-441",
                        Vin = "KMHEC81BBSA600441",
                        Model = "Hyundai Mighty EX8 GTL",
                        ModelCode = "EX8L",
                        SpecCode = "EX8L-CHASSIS-01",
                        SpecDescription = "Mighty EX8 GTL Sắt xi siêu dài",
                        ColorCode = "WH1",
                        ColorName = "Trắng",
                        EngineNo = "D4CC-600441",
                        StorageCodeFrom = "KHO_TONG_HP",
                        StorageNameFrom = "Kho Trung Chuyển Hải Phòng",
                        StorageCodeTo = "KHO_DT_02",
                        StorageNameTo = "Xưởng Đóng Thùng Ô Tô Nam Việt",
                        LoaiThung = "Thùng kín Inox",
                        ActualSpec = "Thùng kín Inox tiêu chuẩn",
                        TypeCB = "N",
                        Status = CarBodyRequestDtlStatus.Cancelled,
                        Remark = "Đã hủy theo văn bản đề nghị của đại lý"
                    }
                }
            };

            db.BodyRequests.AddRange(cbr1, cbr2, cbr3, cbr4, cbr5);
            await db.SaveChangesAsync();
        }

        if (!await db.DriveTests.AnyAsync(o => o.OrgId == orgId))
        {
            var dt1 = new DealerDriveTest
            {
                OrgId = orgId,
                DriveTestCode = "2603DT0001",
                DealerCode = "VN001",
                DealerName = "Hyundai Đông Đô",
                DriverTestGroup = DriveTestGroup.Showroom,
                DriverTestType = DriveTestType.HTV,
                DriveDTime = DateTime.Today.AddDays(-3).AddHours(9).AddMinutes(30),
                CustomerCode = "CUS-VN001-0021",
                FullName = "Nguyễn Văn Hùng",
                Gender = "M",
                RangeAgeCode = "1988",
                DriverLicenseNo = "B2-0108892348",
                IDCardNo = "001088001234",
                PhoneNo = "0982123456",
                Email = "hung.nv@gmail.com",
                Address = "Toà nhà Dolphin Plaza, Mỹ Đình 2, Nam Từ Liêm, Hà Nội",
                ProvinceCode = "HN",
                ProvinceName = "Hà Nội",
                ModelCode = "TUCSON",
                ModelName = "Hyundai Tucson FL 2024",
                DrvTestPlateNo = "30H-889.12",
                DrvTestVIN = "KMHCT81CRPU100001",
                SalesManCode = "SM001",
                SalesManName = "Nguyễn Minh Đức",
                Evaluation = "Rất hài lòng về độ cách âm, cảm giác lái và tính năng ga tự động thích ứng Smart Cruise Control",
                CustomerIntent = "WillBuy",
                EstimatedContractDate = DateTime.Today.AddDays(7),
                SupportAmount = 500000m,
                ApprovedAmount = 500000m,
                Status = DriveTestStatus.Approved,
                CreatedDate = DateTime.Today.AddDays(-4),
                CreatedBy = "SM001",
                ApprovedDate = DateTime.Today.AddDays(-2),
                ApprovedBy = "Lê Hoàng Quân (Trưởng phòng Marketing & Bán hàng NPP)",
                Remark = "Lái thử tại showroom Đông Đô cung đường Đại lộ Thăng Long, khách hài lòng và dự kiến chốt hợp đồng trong tuần tới",
                FlagActive = true
            };

            var dt2 = new DealerDriveTest
            {
                OrgId = orgId,
                DriveTestCode = "2603DT0002",
                DealerCode = "VN002",
                DealerName = "Hyundai Nam Trung",
                DriverTestGroup = DriveTestGroup.Event,
                DriverTestType = DriveTestType.HTV,
                DriveDTime = DateTime.Today.AddDays(-1).AddHours(14).AddMinutes(15),
                CustomerCode = "CUS-VN002-0055",
                FullName = "Trần Thị Minh Trang",
                Gender = "F",
                RangeAgeCode = "1994",
                DriverLicenseNo = "B1-029481992",
                IDCardNo = "025194008765",
                PhoneNo = "0912888999",
                Email = "minhtrang.tran@vnn.vn",
                Address = "Khu đô thị Ecopark, Văn Giang, Hưng Yên",
                ProvinceCode = "HY",
                ProvinceName = "Hưng Yên",
                ModelCode = "SANTAFE",
                ModelName = "Hyundai Santa Fe 2024",
                DrvTestPlateNo = "30H-992.34",
                DrvTestVIN = "KMHCT81CRPU100002",
                SalesManCode = "SM003",
                SalesManName = "Lê Quốc Bảo",
                Evaluation = "Xe tăng tốc êm, khoang lái rộng rãi, hệ thống camera 360 sắc nét, điều hòa mát nhanh",
                CustomerIntent = "Considering",
                EstimatedContractDate = DateTime.Today.AddDays(15),
                SupportAmount = 1200000m,
                ApprovedAmount = 0m,
                Status = DriveTestStatus.Pending,
                CreatedDate = DateTime.Today.AddDays(-1),
                CreatedBy = "SM003",
                Remark = "Khách tham gia sự kiện Roadshow Lái thử & Trải nghiệm SUV Hyundai cuối tuần tại Ecopark, hồ sơ đang chờ NPP duyệt chi phí",
                FlagActive = true
            };

            var dt3 = new DealerDriveTest
            {
                OrgId = orgId,
                DriveTestCode = "2603DT0003",
                DealerCode = "VN003",
                DealerName = "Hyundai Tây Hồ",
                DriverTestGroup = DriveTestGroup.Showroom,
                DriverTestType = DriveTestType.Dealer,
                DriveDTime = DateTime.Today.AddHours(10).AddMinutes(0),
                CustomerCode = "CUS-VN003-0089",
                FullName = "Vũ Đình Nam",
                Gender = "M",
                RangeAgeCode = "1996",
                DriverLicenseNo = "B2-031996228",
                IDCardNo = "001096009871",
                PhoneNo = "0904556677",
                Email = "nam.vd@fpt.com",
                Address = "68 Hoàng Hoa Thám, Ba Đình, Hà Nội",
                ProvinceCode = "HN",
                ProvinceName = "Hà Nội",
                ModelCode = "CRETA",
                ModelName = "Hyundai Creta Smart 2024",
                DrvTestPlateNo = "51K-771.56",
                DrvTestVIN = "KMHCT81CRPU100003",
                SalesManCode = "SM005",
                SalesManName = "Phạm Tuấn Anh",
                Evaluation = "Hài lòng, xe nhỏ gọn dễ luồn lách phố thị, âm thanh Bose sống động",
                CustomerIntent = "WillBuy",
                EstimatedContractDate = DateTime.Today.AddDays(5),
                SupportAmount = 350000m,
                ApprovedAmount = 0m,
                Status = DriveTestStatus.Pending,
                CreatedDate = DateTime.Today,
                CreatedBy = "SM005",
                Remark = "Đại lý Tây Hồ tự túc kinh phí xăng xe lái thử cho khách hàng trải nghiệm nội đô",
                FlagActive = true
            };

            var dt4 = new DealerDriveTest
            {
                OrgId = orgId,
                DriveTestCode = "2603DT0004",
                DealerCode = "VN001",
                DealerName = "Hyundai Đông Đô",
                DriverTestGroup = DriveTestGroup.Event,
                DriverTestType = DriveTestType.HTV,
                DriveDTime = DateTime.Today.AddDays(-6).AddHours(15).AddMinutes(0),
                CustomerCode = "CUS-VN001-0033",
                FullName = "Đặng Văn Lâm",
                Gender = "M",
                RangeAgeCode = "1992",
                DriverLicenseNo = "B2-021992881",
                IDCardNo = "034092001199",
                PhoneNo = "0934112233",
                Email = "lam.dv@gmail.com",
                Address = "Thành phố Bắc Ninh, Tỉnh Bắc Ninh",
                ProvinceCode = "BN",
                ProvinceName = "Bắc Ninh",
                ModelCode = "CUSTIN",
                ModelName = "Hyundai Custin MPV",
                DrvTestPlateNo = "43A-662.88",
                DrvTestVIN = "KMHCT81CRPU100004",
                SalesManCode = "SM002",
                SalesManName = "Hoàng Thị Dung",
                Evaluation = "Chưa đánh giá",
                CustomerIntent = "NoDemand",
                EstimatedContractDate = null,
                SupportAmount = 1500000m,
                ApprovedAmount = 0m,
                Status = DriveTestStatus.Rejected,
                CreatedDate = DateTime.Today.AddDays(-7),
                CreatedBy = "SM002",
                RejectDate = DateTime.Today.AddDays(-5),
                RejectBy = "Lê Hoàng Quân (Trưởng phòng Marketing & Bán hàng NPP)",
                RejectRemark = "Địa điểm sự kiện tổ chức ngoài phạm vi địa bàn kinh doanh được phân quyền của đại lý và vượt quá hạn mức ngân sách quý",
                Remark = "Hồ sơ đề nghị hỗ trợ kinh phí lái thử xe MPV bị NPP từ chối",
                FlagActive = true
            };

            var dt5 = new DealerDriveTest
            {
                OrgId = orgId,
                DriveTestCode = "2603DT0005",
                DealerCode = "VN002",
                DealerName = "Hyundai Nam Trung",
                DriverTestGroup = DriveTestGroup.Showroom,
                DriverTestType = DriveTestType.Dealer,
                DriveDTime = DateTime.Today.AddDays(-10).AddHours(11).AddMinutes(0),
                CustomerCode = "CUS-VN002-0077",
                FullName = "Lê Cẩm Nhung",
                Gender = "F",
                RangeAgeCode = "1998",
                DriverLicenseNo = "B1-029881122",
                IDCardNo = "001198007788",
                PhoneNo = "0977223344",
                Email = "nhung.lc@gmail.com",
                Address = "Cầu Giấy, Hà Nội",
                ProvinceCode = "HN",
                ProvinceName = "Hà Nội",
                ModelCode = "ACCENT",
                ModelName = "Hyundai Accent Thế hệ mới 2024",
                DrvTestPlateNo = "30H-889.12",
                DrvTestVIN = "KMHCT81CRPU100001",
                SalesManCode = "SM004",
                SalesManName = "Đỗ Văn Thắng",
                Evaluation = "Không thực hiện do bận việc gia đình",
                CustomerIntent = "NoDemand",
                EstimatedContractDate = null,
                SupportAmount = 300000m,
                ApprovedAmount = 0m,
                Status = DriveTestStatus.Cancelled,
                CreatedDate = DateTime.Today.AddDays(-12),
                CreatedBy = "SM004",
                CancelledDate = DateTime.Today.AddDays(-10),
                CancelledBy = "SM004",
                CancelRemark = "Khách hàng thông báo hủy lịch lái thử vào phút chót do có việc đột xuất, đại lý xác nhận hủy lượt lái thử",
                Remark = "Đã hủy lịch hẹn lái thử của khách",
                FlagActive = true
            };

            db.DriveTests.AddRange(dt1, dt2, dt3, dt4, dt5);
            await db.SaveChangesAsync();
        }

        if (!await db.StorageRearrangeCBOrders.AnyAsync(o => o.OrgId == orgId))
        {
            var rcb1 = new StorageRearrangeCBOrder
            {
                OrgId = orgId,
                StoRearCBNo = "2603RCB000001",
                Status = StoRearrangeCBStatus.Approved,
                TotalCars = 2,
                StorageCodeTo = "KHO_DT_01",
                StorageNameTo = "Xưởng Đóng Thùng Ô Tô Hiệp Hòa",
                Remark = "Lệnh điều chuyển 02 xe Porter H150 chassis sang xưởng Hiệp Hòa gia công đóng thùng mui bạt và thùng kín Inox",
                CreatedDate = DateTime.Today.AddDays(-4),
                CreatedBy = "NPP_LOGISTICS_CV",
                ApprovedDate = DateTime.Today.AddDays(-3),
                ApprovedBy = "Vũ Thành Long (Phó Giám đốc Khối Xe Thương Mại NPP)",
                LUDateTime = DateTime.Today.AddDays(-3),
                LUBy = "Vũ Thành Long (Phó Giám đốc Khối Xe Thương Mại NPP)",
                Details = new List<StorageRearrangeCBDetail>
                {
                    new StorageRearrangeCBDetail
                    {
                        StoRearCBNo = "2603RCB000001",
                        CarId = "CAR2026-H150-112",
                        Vin = "KMHEC81BBSA100112",
                        Model = "Hyundai Porter H150",
                        ModelCode = "H150",
                        SpecCode = "H150-CHASSIS-01",
                        SpecDescription = "Porter H150 Sắt xi cabin đơn",
                        ColorCode = "WH1",
                        ColorName = "Trắng tuyết",
                        EngineNo = "D4CB-100112",
                        StorageCodeFrom = "KHO_TONG_HN",
                        StorageNameFrom = "Kho Tổng Hà Nội - KCN Đài Tư",
                        StorageCodeTo = "KHO_DT_01",
                        StorageNameTo = "Xưởng Đóng Thùng Ô Tô Hiệp Hòa",
                        CBReqNo = "2603CBR000001",
                        ExpectedStartDate = DateTime.Today.AddDays(-3),
                        ExpectedEndDate = DateTime.Today.AddDays(4),
                        LoaiThung = "Thùng mui bạt",
                        ActualSpec = "Thùng mui bạt 5 bửng mở sàn sắt 2.5mm bạt simili Hàn Quốc",
                        TypeCB = "N",
                        Status = StoRearrangeCBDtlStatus.Approved,
                        ConfirmDate = DateTime.Today.AddDays(-3),
                        ConfirmBy = "HQ_LOGISTICS",
                        Remark = "Tiến độ đạt 60%, khung xương đã lắp xong"
                    },
                    new StorageRearrangeCBDetail
                    {
                        StoRearCBNo = "2603RCB000001",
                        CarId = "CAR2026-H150-113",
                        Vin = "KMHEC81BBSA100113",
                        Model = "Hyundai Porter H150",
                        ModelCode = "H150",
                        SpecCode = "H150-CHASSIS-01",
                        SpecDescription = "Porter H150 Sắt xi cabin đơn",
                        ColorCode = "BL1",
                        ColorName = "Xanh Hyundai",
                        EngineNo = "D4CB-100113",
                        StorageCodeFrom = "KHO_TONG_HN",
                        StorageNameFrom = "Kho Tổng Hà Nội - KCN Đài Tư",
                        StorageCodeTo = "KHO_DT_01",
                        StorageNameTo = "Xưởng Đóng Thùng Ô Tô Hiệp Hòa",
                        CBReqNo = "2603CBR000001",
                        ExpectedStartDate = DateTime.Today.AddDays(-3),
                        ExpectedEndDate = DateTime.Today.AddDays(5),
                        LoaiThung = "Thùng kín Inox",
                        ActualSpec = "Thùng kín vách Inox 430 dập sóng sàn dập lá me",
                        TypeCB = "N",
                        Status = StoRearrangeCBDtlStatus.Approved,
                        ConfirmDate = DateTime.Today.AddDays(-3),
                        ConfirmBy = "HQ_LOGISTICS",
                        Remark = "Chuẩn bị lắp vách Inox và cửa hông"
                    }
                }
            };

            var rcb2 = new StorageRearrangeCBOrder
            {
                OrgId = orgId,
                StoRearCBNo = "2603RCB000002",
                Status = StoRearrangeCBStatus.Pending,
                TotalCars = 1,
                StorageCodeTo = "KHO_DT_QUYENAUTO",
                StorageNameTo = "Xưởng Đóng Thùng Quyền Auto (Đông Lạnh)",
                Remark = "Lệnh điều chuyển xe New Porter H150 chassis sang xưởng Quyền Auto đóng thùng đông lạnh chuyên dụng theo YCĐT 2603CBR000002",
                CreatedDate = DateTime.Today.AddDays(-1),
                CreatedBy = "NPP_LOGISTICS_CV",
                LUDateTime = DateTime.Today.AddDays(-1),
                LUBy = "NPP_LOGISTICS_CV",
                Details = new List<StorageRearrangeCBDetail>
                {
                    new StorageRearrangeCBDetail
                    {
                        StoRearCBNo = "2603RCB000002",
                        CarId = "CAR2026-H150-771",
                        Vin = "KMHEB81BBSA500771",
                        Model = "Hyundai New Porter H150",
                        ModelCode = "H150",
                        SpecCode = "H150-CHASSIS-02",
                        SpecDescription = "New Porter H150 Sắt xi cabin đơn",
                        ColorCode = "BL1",
                        ColorName = "Xanh Hyundai",
                        EngineNo = "D4CB-500771",
                        StorageCodeFrom = "KHO_TONG_SG",
                        StorageNameFrom = "Kho Tổng Nam Bộ Hiệp Phước",
                        StorageCodeTo = "KHO_DT_QUYENAUTO",
                        StorageNameTo = "Xưởng Đóng Thùng Quyền Auto (Đông Lạnh)",
                        CBReqNo = "2603CBR000002",
                        ExpectedStartDate = DateTime.Today,
                        ExpectedEndDate = DateTime.Today.AddDays(7),
                        LoaiThung = "Thùng đông lạnh",
                        ActualSpec = "Thùng Composite Foam PU cách nhiệt dày 80mm máy lạnh Thermal Master T1400",
                        TypeCB = "N",
                        Status = StoRearrangeCBDtlStatus.Pending,
                        Remark = "Hồ sơ mới gửi sang xưởng Quyền Auto, chờ NPP phê duyệt lệnh điều chuyển"
                    }
                }
            };

            var rcb3 = new StorageRearrangeCBOrder
            {
                OrgId = orgId,
                StoRearCBNo = "2603RCB000003",
                Status = StoRearrangeCBStatus.Rejected,
                TotalCars = 1,
                StorageCodeTo = "KHO_DT_02",
                StorageNameTo = "Xưởng Đóng Thùng Ô Tô Nam Việt",
                Remark = "Lệnh điều chuyển xe Mighty EX8 GTL đóng thùng kín Inox",
                RejectReason = "Kho bãi xưởng đóng thùng Nam Việt đang quá tải đơn hàng theo tiến độ tháng 3, đề nghị chuyển lệnh sang xưởng Hiệp Hòa hoặc An Khang",
                RejectDate = DateTime.Today.AddDays(-5),
                RejectBy = "Vũ Thành Long (Phó Giám đốc Khối Xe Thương Mại NPP)",
                CreatedDate = DateTime.Today.AddDays(-6),
                CreatedBy = "NPP_LOGISTICS_CV",
                LUDateTime = DateTime.Today.AddDays(-5),
                LUBy = "Vũ Thành Long (Phó Giám đốc Khối Xe Thương Mại NPP)",
                Details = new List<StorageRearrangeCBDetail>
                {
                    new StorageRearrangeCBDetail
                    {
                        StoRearCBNo = "2603RCB000003",
                        CarId = "CAR2026-EX8L-441",
                        Vin = "KMHEC81BBSA600441",
                        Model = "Hyundai Mighty EX8 GTL",
                        ModelCode = "EX8L",
                        SpecCode = "EX8L-CHASSIS-01",
                        SpecDescription = "Mighty EX8 GTL Sắt xi siêu dài",
                        ColorCode = "WH1",
                        ColorName = "Trắng",
                        EngineNo = "D4CC-600441",
                        StorageCodeFrom = "KHO_TONG_HP",
                        StorageNameFrom = "Kho Trung Chuyển Hải Phòng",
                        StorageCodeTo = "KHO_DT_02",
                        StorageNameTo = "Xưởng Đóng Thùng Ô Tô Nam Việt",
                        CBReqNo = "2603CBR000005",
                        ExpectedStartDate = DateTime.Today.AddDays(-5),
                        ExpectedEndDate = DateTime.Today.AddDays(2),
                        LoaiThung = "Thùng kín Inox",
                        ActualSpec = "Thùng kín Inox tiêu chuẩn",
                        TypeCB = "N",
                        Status = StoRearrangeCBDtlStatus.Rejected,
                        Remark = "Từ chối duyệt do xưởng quá tải"
                    }
                }
            };

            var rcb4 = new StorageRearrangeCBOrder
            {
                OrgId = orgId,
                StoRearCBNo = "2603RCB000004",
                Status = StoRearrangeCBStatus.Cancelled,
                TotalCars = 1,
                StorageCodeTo = "KHO_DT_01",
                StorageNameTo = "Xưởng Đóng Thùng Ô Tô Hiệp Hòa",
                Remark = "Lệnh điều chuyển đóng thùng lửng xe Porter H150",
                CancelReason = "Hủy lệnh do khách hàng đại lý đổi phương án yêu cầu bàn giao xe chassis sắt xi tự đóng thùng ngoài",
                CancelDate = DateTime.Today.AddDays(-7),
                CancelBy = "NPP_LOGISTICS_CV",
                CreatedDate = DateTime.Today.AddDays(-8),
                CreatedBy = "NPP_LOGISTICS_CV",
                LUDateTime = DateTime.Today.AddDays(-7),
                LUBy = "NPP_LOGISTICS_CV",
                Details = new List<StorageRearrangeCBDetail>
                {
                    new StorageRearrangeCBDetail
                    {
                        StoRearCBNo = "2603RCB000004",
                        CarId = "CAR2026-H150-990",
                        Vin = "KMHEC81BBSA990111",
                        Model = "Hyundai Porter H150",
                        ModelCode = "H150",
                        SpecCode = "H150-CHASSIS-01",
                        SpecDescription = "Porter H150 Sắt xi cabin đơn",
                        ColorCode = "WH1",
                        ColorName = "Trắng tuyết",
                        EngineNo = "D4CB-990111",
                        StorageCodeFrom = "KHO_TONG_HN",
                        StorageNameFrom = "Kho Tổng Hà Nội - KCN Đài Tư",
                        StorageCodeTo = "KHO_DT_01",
                        StorageNameTo = "Xưởng Đóng Thùng Ô Tô Hiệp Hòa",
                        CBReqNo = "2603CBR000001",
                        ExpectedStartDate = DateTime.Today.AddDays(-7),
                        ExpectedEndDate = DateTime.Today,
                        LoaiThung = "Thùng lửng",
                        ActualSpec = "Thùng lửng sắt xi",
                        TypeCB = "N",
                        Status = StoRearrangeCBDtlStatus.Cancelled,
                        Remark = "Đã hủy theo yêu cầu đại lý"
                    }
                }
            };

            db.StorageRearrangeCBOrders.AddRange(rcb1, rcb2, rcb3, rcb4);
            await db.SaveChangesAsync();
        }

        if (!await db.CarCancelReasons.AnyAsync())
        {
            var reasons = new List<CarCancelReasonMaster>
            {
                new CarCancelReasonMaster { Code = "CCT01", Name = "Lỗi kỹ thuật nhà máy HTMV", Description = "Phát hiện lỗi kỹ thuật chất lượng xuất xưởng từ nhà máy sản xuất HTMV cần thu hồi khắc phục", FlagActive = true },
                new CarCancelReasonMaster { Code = "CCT02", Name = "Khách hàng hủy hợp đồng", Description = "Khách hàng cá nhân/doanh nghiệp hủy cọc hợp đồng bán lẻ và từ chối nhận bàn giao xe", FlagActive = true },
                new CarCancelReasonMaster { Code = "CCT03", Name = "Đại lý quá hạn thanh toán", Description = "Đại lý quá hạn thực hiện nghĩa vụ thanh toán cọc hoặc phát hành bảo lãnh ngân hàng theo quy định", FlagActive = true },
                new CarCancelReasonMaster { Code = "CCT04", Name = "Tái phân bổ điều chuyển lô", Description = "Ban điều phối kinh doanh NPP tái phân bổ xe cho đơn hàng ưu tiên hoặc đối tác chiến lược khác", FlagActive = true },
                new CarCancelReasonMaster { Code = "CCT05", Name = "Hư hại vận chuyển lưu kho", Description = "Xe bị trầy xước, va chạm hoặc sự cố ngoại cảnh trong quá trình vận chuyển đường dài hoặc lưu bãi", FlagActive = true },
                new CarCancelReasonMaster { Code = "CCT99", Name = "Lý do hủy khác", Description = "Các lý do điều hành phát sinh khác theo phê duyệt của Lãnh đạo NPP (bắt buộc nhập lý do chi tiết)", FlagActive = true }
            };
            db.CarCancelReasons.AddRange(reasons);
            await db.SaveChangesAsync();
        }

        if (!await db.Cars.AnyAsync(o => o.OrgId == orgId))
        {
            var c1 = new CarRecord
            {
                OrgId = orgId,
                CarId = "CAR2026-SF0988",
                Vin = "KMHE281BBSA129841",
                ModelCode = "SF25",
                ModelName = "Santa Fe 2.5 HTRAC",
                SpecCode = "SF25-PRE-01",
                SpecDescription = "Santa Fe 2.5 xăng cao cấp dẫn động HTRAC",
                ColorCode = "WW2",
                ColorName = "Trắng ngọc trai",
                EngineNo = "G4KP-129841",
                DealerCode = "VN001",
                DealerName = "Hyundai Đông Đô",
                StorageCode = "KHO_TONG_HN",
                StorageName = "Kho Tổng Hyundai Ninh Bình",
                SoCode = "ORD2603010001",
                PriceActual = 1350000000m,
                PaymentDepositAmount = 135000000m,
                GuaranteeAmount = 1215000000m,
                DutyCompletedAmount = 1350000000m,
                FlagActive = "1",
                FlagAllowChangeVIN = "1",
                CarCancelType = null,
                CarCancelRemark = null,
                CarCancelDate = null,
                CarCancelBy = null,
                FlagEarlyCancel = "0",
                FlagMapVIN = "1",
                FlagCarDeliveryOrder = "1",
                FlagTestCar = "0",
                CreatedAt = DateTime.Today.AddDays(-20),
                LogLUDateTime = DateTime.Today.AddDays(-5),
                LogLUBy = "HE_THONG_BAN_HANG"
            };

            var c2 = new CarRecord
            {
                OrgId = orgId,
                CarId = "CAR2026-TU1102",
                Vin = "KMHE281BBSA987654",
                ModelCode = "TU20",
                ModelName = "Tucson 2.0 AT",
                SpecCode = "TU20-STD-01",
                SpecDescription = "Tucson 2.0 xăng tiêu chuẩn bản nâng cấp",
                ColorCode = "NKA",
                ColorName = "Đen Phantom",
                EngineNo = "G4NM-987654",
                DealerCode = "VN002",
                DealerName = "Hyundai Nam Trung",
                StorageCode = "KHO_TONG_SG",
                StorageName = "Kho Tổng Nam Bộ Hiệp Phước",
                SoCode = "ORD2603010002",
                PriceActual = 950000000m,
                PaymentDepositAmount = 95000000m,
                GuaranteeAmount = 855000000m,
                DutyCompletedAmount = 950000000m,
                FlagActive = "1",
                FlagAllowChangeVIN = "1",
                CarCancelType = null,
                CarCancelRemark = null,
                CarCancelDate = null,
                CarCancelBy = null,
                FlagEarlyCancel = "0",
                FlagMapVIN = "1",
                FlagCarDeliveryOrder = "1",
                FlagTestCar = "0",
                CreatedAt = DateTime.Today.AddDays(-25),
                LogLUDateTime = DateTime.Today.AddDays(-10),
                LogLUBy = "HE_THONG_BAN_HANG"
            };

            var c3 = new CarRecord
            {
                OrgId = orgId,
                CarId = "CAR2026-CR0192",
                Vin = "KMHE281BBSA334455",
                ModelCode = "CR15",
                ModelName = "Creta 1.5 Cao Cấp",
                SpecCode = "CR15-PRE-02",
                SpecDescription = "Creta 1.5 CVT bản cao cấp 2 tông màu",
                ColorCode = "R2N",
                ColorName = "Đỏ đô nóc đen",
                EngineNo = "G4FL-334455",
                DealerCode = "VN001",
                DealerName = "Hyundai Đông Đô",
                StorageCode = "KHO_TONG_HN",
                StorageName = "Kho Tổng Hà Nội - Đài Tư",
                SoCode = "ORD2603010003",
                PriceActual = 740000000m,
                PaymentDepositAmount = 0m,
                GuaranteeAmount = 0m,
                DutyCompletedAmount = 0m,
                FlagActive = "1",
                FlagAllowChangeVIN = "1",
                CarCancelType = null,
                CarCancelRemark = "Cảnh báo sớm: Đại lý chậm nộp UNC thanh toán cọc 10% quá 5 ngày, nguy cơ bị hủy ghép VIN",
                CarCancelDate = null,
                CarCancelBy = null,
                FlagEarlyCancel = "1", // Xe sắp bị hủy
                FlagMapVIN = "1",
                FlagCarDeliveryOrder = "0",
                FlagTestCar = "0",
                CreatedAt = DateTime.Today.AddDays(-10),
                LogLUDateTime = DateTime.Today.AddDays(-1),
                LogLUBy = "Đặng Phương Nam (Phòng Bán buôn HTC)"
            };

            var c4 = new CarRecord
            {
                OrgId = orgId,
                CarId = "2605C64655BN7I-CKD",
                Vin = "KMHE281BBSA556677",
                ModelCode = "SF25",
                ModelName = "Santa Fe 2.5 Xăng Cao Cấp",
                SpecCode = "SF25-PRE-01",
                SpecDescription = "Santa Fe 2.5 CKD xăng cao cấp",
                ColorCode = "WH1",
                ColorName = "Trắng",
                EngineNo = "G4KP-556677",
                DealerCode = "VN001",
                DealerName = "Hyundai Đông Đô",
                StorageCode = "KHO_TONG_HN",
                StorageName = "Kho Tổng Hyundai Ninh Bình",
                SoCode = "ORD2603010008",
                PriceActual = 1350000000m,
                PaymentDepositAmount = 0m,
                GuaranteeAmount = 0m,
                DutyCompletedAmount = 0m,
                FlagActive = "0", // ĐÃ HỦY
                FlagAllowChangeVIN = "1",
                CarCancelType = "CCT02",
                CarCancelRemark = "Khách hàng hủy hợp đồng mua xe do chuyển công tác nước ngoài, đại lý xin hủy xe để hoàn cọc",
                CarCancelDate = DateTime.Today.AddDays(-12),
                CarCancelBy = "Lê Văn Hùng (Chuyên viên Điều phối NPP)",
                FlagEarlyCancel = "0",
                FlagMapVIN = "0",
                FlagCarDeliveryOrder = "0",
                FlagTestCar = "0",
                CreatedAt = DateTime.Today.AddDays(-30),
                LogLUDateTime = DateTime.Today.AddDays(-12),
                LogLUBy = "Lê Văn Hùng (Chuyên viên Điều phối NPP)"
            };

            var c5 = new CarRecord
            {
                OrgId = orgId,
                CarId = "2605C64656BN7I-CKD",
                Vin = "KMHE281BBSA667788",
                ModelCode = "TU20",
                ModelName = "Tucson 2.0 Dầu Cao Cấp",
                SpecCode = "TU20-DSL-02",
                SpecDescription = "Tucson 2.0 Diesel máy dầu đặc biệt",
                ColorCode = "BK1",
                ColorName = "Đen",
                EngineNo = "D4HD-667788",
                DealerCode = "VN002",
                DealerName = "Hyundai Nam Trung",
                StorageCode = "KHO_TONG_SG",
                StorageName = "Kho Tổng Nam Bộ Hiệp Phước",
                SoCode = "ORD2603010009",
                PriceActual = 1060000000m,
                PaymentDepositAmount = 0m,
                GuaranteeAmount = 0m,
                DutyCompletedAmount = 0m,
                FlagActive = "0", // ĐÃ HỦY
                FlagAllowChangeVIN = "1",
                CarCancelType = "CCT03",
                CarCancelRemark = "Đại lý Nam Trung quá hạn mở thư bảo lãnh ngân hàng 15 ngày làm việc theo hợp đồng bán buôn",
                CarCancelDate = DateTime.Today.AddDays(-8),
                CarCancelBy = "Nguyễn Thị Phương (Phòng Quản lý Bán buôn HTC)",
                FlagEarlyCancel = "0",
                FlagMapVIN = "0",
                FlagCarDeliveryOrder = "0",
                FlagTestCar = "0",
                CreatedAt = DateTime.Today.AddDays(-28),
                LogLUDateTime = DateTime.Today.AddDays(-8),
                LogLUBy = "Nguyễn Thị Phương (Phòng Quản lý Bán buôn HTC)"
            };

            var c6 = new CarRecord
            {
                OrgId = orgId,
                CarId = "2605C64657BN7I-CKD",
                Vin = "KMHE281BBSA778899",
                ModelCode = "I10",
                ModelName = "Grand i10 Sedan 1.2 AT",
                SpecCode = "I10-SED-01",
                SpecDescription = "Grand i10 Sedan 1.2 số tự động",
                ColorCode = "SL1",
                ColorName = "Bạc",
                EngineNo = "G4LA-778899",
                DealerCode = "VN003",
                DealerName = "Hyundai Tây Hồ",
                StorageCode = "KHO_TONG_HN",
                StorageName = "Kho Tổng Hyundai Ninh Bình",
                SoCode = "ORD2603010010",
                PriceActual = 435000000m,
                PaymentDepositAmount = 0m,
                GuaranteeAmount = 0m,
                DutyCompletedAmount = 0m,
                FlagActive = "0", // ĐÃ HỦY
                FlagAllowChangeVIN = "1",
                CarCancelType = "CCT01",
                CarCancelRemark = "Nhà máy HTMV phát hiện lỗi cụm thước lái trong kiểm định xuất xưởng, yêu cầu hủy lệnh cấp cho đại lý",
                CarCancelDate = DateTime.Today.AddDays(-5),
                CarCancelBy = "Hoàng Minh Trí (KCS Nhà máy HTMV)",
                FlagEarlyCancel = "0",
                FlagMapVIN = "0",
                FlagCarDeliveryOrder = "0",
                FlagTestCar = "0",
                CreatedAt = DateTime.Today.AddDays(-15),
                LogLUDateTime = DateTime.Today.AddDays(-5),
                LogLUBy = "Hoàng Minh Trí (KCS Nhà máy HTMV)"
            };

            var c7 = new CarRecord
            {
                OrgId = orgId,
                CarId = "2605C64658BN7I-CKD",
                Vin = "KMHE281BBSA889900",
                ModelCode = "EL16",
                ModelName = "Elantra 1.6 AT",
                SpecCode = "EL16-STD-01",
                SpecDescription = "Elantra 1.6 AT Sedan",
                ColorCode = "RD1",
                ColorName = "Đỏ đô",
                EngineNo = "G4FG-889900",
                DealerCode = "VN001",
                DealerName = "Hyundai Đông Đô",
                StorageCode = "KHO_TONG_HN",
                StorageName = "Kho Tổng Hà Nội - Đài Tư",
                SoCode = "ORD2603010011",
                PriceActual = 669000000m,
                PaymentDepositAmount = 0m,
                GuaranteeAmount = 0m,
                DutyCompletedAmount = 0m,
                FlagActive = "0", // ĐÃ HỦY
                FlagAllowChangeVIN = "1",
                CarCancelType = "CCT04",
                CarCancelRemark = "Điều chuyển tái phân bổ lô xe cho hợp đồng cung ứng xe fleet doanh nghiệp Mai Linh",
                CarCancelDate = DateTime.Today.AddDays(-2),
                CarCancelBy = "Lê Văn Hùng (Chuyên viên Điều phối NPP)",
                FlagEarlyCancel = "0",
                FlagMapVIN = "0",
                FlagCarDeliveryOrder = "0",
                FlagTestCar = "0",
                CreatedAt = DateTime.Today.AddDays(-12),
                LogLUDateTime = DateTime.Today.AddDays(-2),
                LogLUBy = "Lê Văn Hùng (Chuyên viên Điều phối NPP)"
            };

            var c8 = new CarRecord
            {
                OrgId = orgId,
                CarId = "CAR2026-CS2001",
                Vin = "KMHE281BBSA112233",
                ModelCode = "CS20",
                ModelName = "Custin 2.0T Cao Cấp",
                SpecCode = "CS20-PRE-01",
                SpecDescription = "Custin 2.0 Turbo Bản Cao Cấp 7 chỗ",
                ColorCode = "WW1",
                ColorName = "Trắng tuyết",
                EngineNo = "G4NN-112233",
                DealerCode = "VN001",
                DealerName = "Hyundai Đông Đô",
                StorageCode = "SHOWROOM_DONGDO",
                StorageName = "Showroom Hyundai Đông Đô",
                SoCode = "ORD2603010012",
                PriceActual = 999000000m,
                PaymentDepositAmount = 0m,
                GuaranteeAmount = 0m,
                DutyCompletedAmount = 0m,
                FlagActive = "1",
                FlagAllowChangeVIN = "1",
                CarCancelType = null,
                CarCancelRemark = "Xe lái thử phục vụ khách hàng trải nghiệm tại đại lý",
                CarCancelDate = null,
                CarCancelBy = null,
                FlagEarlyCancel = "0",
                FlagMapVIN = "0",
                FlagCarDeliveryOrder = "0",
                FlagTestCar = "1", // Xe lái thử Demo
                CreatedAt = DateTime.Today.AddDays(-18),
                LogLUDateTime = DateTime.Today.AddDays(-18),
                LogLUBy = "DEALER_ADMIN"
            };

            var c9 = new CarRecord
            {
                OrgId = orgId,
                CarId = "CAR2026-AC1401",
                Vin = "KMHE281BBSA223344",
                ModelCode = "AC14",
                ModelName = "Accent 1.4 AT",
                SpecCode = "AC14-STD-01",
                SpecDescription = "Accent 1.4 AT Sedan gia đình",
                ColorCode = "SL2",
                ColorName = "Bạc",
                EngineNo = "G4LC-223344",
                DealerCode = "VN003",
                DealerName = "Hyundai Tây Hồ",
                StorageCode = "KHO_TONG_HN",
                StorageName = "Kho Tổng Hyundai Ninh Bình",
                SoCode = "ORD2603010015",
                PriceActual = 542000000m,
                PaymentDepositAmount = 0m,
                GuaranteeAmount = 0m,
                DutyCompletedAmount = 0m,
                FlagActive = "1",
                FlagAllowChangeVIN = "1",
                CarCancelType = null,
                CarCancelRemark = null,
                CarCancelDate = null,
                CarCancelBy = null,
                FlagEarlyCancel = "0",
                FlagMapVIN = "1",
                FlagCarDeliveryOrder = "1",
                FlagTestCar = "0",
                CreatedAt = DateTime.Today.AddDays(-8),
                LogLUDateTime = DateTime.Today.AddDays(-8),
                LogLUBy = "HE_THONG_BAN_HANG"
            };

            db.Cars.AddRange(c1, c2, c3, c4, c5, c6, c7, c8, c9);

            var logs = new List<CarCancelLog>
            {
                new CarCancelLog
                {
                    OrgId = orgId,
                    CarId = "2605C64655BN7I-CKD",
                    Vin = "KMHE281BBSA556677",
                    Action = "Cancel",
                    CancelType = "CCT02",
                    Remark = "Khách hàng hủy hợp đồng mua xe do chuyển công tác nước ngoài",
                    PerformedBy = "Lê Văn Hùng (Chuyên viên Điều phối NPP)",
                    PerformedAt = DateTime.Today.AddDays(-12)
                },
                new CarCancelLog
                {
                    OrgId = orgId,
                    CarId = "2605C64656BN7I-CKD",
                    Vin = "KMHE281BBSA667788",
                    Action = "Cancel",
                    CancelType = "CCT03",
                    Remark = "Đại lý Nam Trung quá hạn mở thư bảo lãnh ngân hàng 15 ngày làm việc",
                    PerformedBy = "Nguyễn Thị Phương (Phòng Quản lý Bán buôn HTC)",
                    PerformedAt = DateTime.Today.AddDays(-8)
                },
                new CarCancelLog
                {
                    OrgId = orgId,
                    CarId = "2605C64657BN7I-CKD",
                    Vin = "KMHE281BBSA778899",
                    Action = "Cancel",
                    CancelType = "CCT01",
                    Remark = "Nhà máy HTMV phát hiện lỗi cụm thước lái trong kiểm định xuất xưởng",
                    PerformedBy = "Hoàng Minh Trí (KCS Nhà máy HTMV)",
                    PerformedAt = DateTime.Today.AddDays(-5)
                },
                new CarCancelLog
                {
                    OrgId = orgId,
                    CarId = "2605C64658BN7I-CKD",
                    Vin = "KMHE281BBSA889900",
                    Action = "Cancel",
                    CancelType = "CCT04",
                    Remark = "Điều chuyển tái phân bổ lô xe cho hợp đồng cung ứng xe fleet doanh nghiệp Mai Linh",
                    PerformedBy = "Lê Văn Hùng (Chuyên viên Điều phối NPP)",
                    PerformedAt = DateTime.Today.AddDays(-2)
                },
                new CarCancelLog
                {
                    OrgId = orgId,
                    CarId = "CAR2026-CR0192",
                    Vin = "KMHE281BBSA334455",
                    Action = "UpdateFlags",
                    CancelType = null,
                    Remark = "Cập nhật cờ điều hành: MapVIN=1, EarlyCancel=1, CarDO=0, TestCar=0. Ghi chú cảnh báo chậm thanh toán",
                    PerformedBy = "Đặng Phương Nam (Phòng Bán buôn HTC)",
                    PerformedAt = DateTime.Today.AddDays(-1)
                }
            };
            db.CarCancelLogs.AddRange(logs);

            await db.SaveChangesAsync();
        }

        if (!await db.CarVinInventories.AnyAsync(v => v.OrgId == orgId))
        {
            // Thêm các xe thương mại chưa có VIN để phục vụ phân bổ Map VIN
            var c10 = new CarRecord
            {
                OrgId = orgId,
                CarId = "CAR2026-SF2501",
                Vin = "",
                ModelCode = "SF25",
                ModelName = "Santa Fe 2.5 Xăng Cao Cấp",
                SpecCode = "SF25-PRE-01",
                SpecDescription = "Santa Fe 2.5 CKD xăng cao cấp",
                ColorCode = "WH1",
                ColorName = "Trắng",
                DealerCode = "VN001",
                DealerName = "Hyundai Đông Đô",
                StorageCode = "KHO_TONG_HN",
                StorageName = "Kho Tổng Hyundai Ninh Bình",
                SoCode = "ORD2603010018",
                PriceActual = 1350000000m,
                PaymentDepositAmount = 135000000m, // Đã cọc 10%
                GuaranteeAmount = 1215000000m,
                DutyCompletedAmount = 1350000000m,
                FlagActive = "1",
                FlagAllowChangeVIN = "1",
                FlagEarlyCancel = "0",
                FlagMapVIN = "1", // Cho phép Map VIN
                FlagCarDeliveryOrder = "0",
                FlagTestCar = "0",
                CreatedAt = DateTime.Today.AddDays(-6),
                LogLUDateTime = DateTime.Today.AddDays(-6),
                LogLUBy = "HE_THONG_BAN_HANG"
            };

            var c11 = new CarRecord
            {
                OrgId = orgId,
                CarId = "CAR2026-TU2005",
                Vin = "",
                ModelCode = "TU20",
                ModelName = "Tucson 2.0 AT",
                SpecCode = "TU20-STD-01",
                SpecDescription = "Tucson 2.0 xăng tiêu chuẩn bản nâng cấp",
                ColorCode = "NKA",
                ColorName = "Đen Phantom",
                DealerCode = "VN002",
                DealerName = "Hyundai Nam Trung",
                StorageCode = "KHO_TONG_SG",
                StorageName = "Kho Tổng Nam Bộ Hiệp Phước",
                SoCode = "ORD2603010019",
                PriceActual = 950000000m,
                PaymentDepositAmount = 190000000m, // Đã cọc 20%
                GuaranteeAmount = 760000000m,
                DutyCompletedAmount = 950000000m,
                FlagActive = "1",
                FlagAllowChangeVIN = "1",
                FlagEarlyCancel = "0",
                FlagMapVIN = "1", // Cho phép Map VIN
                FlagCarDeliveryOrder = "0",
                FlagTestCar = "0",
                CreatedAt = DateTime.Today.AddDays(-4),
                LogLUDateTime = DateTime.Today.AddDays(-4),
                LogLUBy = "HE_THONG_BAN_HANG"
            };

            var c12 = new CarRecord
            {
                OrgId = orgId,
                CarId = "CAR2026-AC1409",
                Vin = "",
                ModelCode = "AC14",
                ModelName = "Accent 1.4 AT",
                SpecCode = "AC14-STD-01",
                SpecDescription = "Accent 1.4 AT Sedan gia đình",
                ColorCode = "SL2",
                ColorName = "Bạc",
                DealerCode = "VN003",
                DealerName = "Hyundai Tây Hồ",
                StorageCode = "KHO_TONG_HN",
                StorageName = "Kho Tổng Hyundai Ninh Bình",
                SoCode = "ORD2603010020",
                PriceActual = 542000000m,
                PaymentDepositAmount = 54200000m, // Đã cọc 10%
                GuaranteeAmount = 0m,
                DutyCompletedAmount = 54200000m,
                FlagActive = "1",
                FlagAllowChangeVIN = "1",
                FlagEarlyCancel = "0",
                FlagMapVIN = "1", // Cho phép Map VIN
                FlagCarDeliveryOrder = "0",
                FlagTestCar = "0",
                CreatedAt = DateTime.Today.AddDays(-3),
                LogLUDateTime = DateTime.Today.AddDays(-3),
                LogLUBy = "HE_THONG_BAN_HANG"
            };

            db.Cars.AddRange(c10, c11, c12);

            // Kho xe vật lý có số khung VIN (CarVinInventory)
            var vins = new List<CarVinInventory>
            {
                // Các xe đã map
                new CarVinInventory
                {
                    OrgId = orgId,
                    Vin = "KMHE281BBSA129841",
                    ModelCode = "SF25",
                    ModelName = "Santa Fe 2.5 HTRAC",
                    SpecCode = "SF25-PRE-01",
                    SpecDescription = "Santa Fe 2.5 CKD xăng cao cấp",
                    ColorCode = "WH1",
                    ColorName = "Trắng",
                    EngineNo = "G4KP-129841",
                    StorageCode = "KHO_TONG_HN",
                    StorageName = "Kho Tổng Hyundai Ninh Bình",
                    AssemblyStatus = "CKD",
                    ProductionMonth = "202602",
                    Status = CarVinStatus.Mapped,
                    MappedCarId = "CAR2026-SF0988",
                    CreatedAt = DateTime.Today.AddDays(-20)
                },
                new CarVinInventory
                {
                    OrgId = orgId,
                    Vin = "KMHE281BBSA987654",
                    ModelCode = "TU20",
                    ModelName = "Tucson 2.0 AT",
                    SpecCode = "TU20-STD-01",
                    SpecDescription = "Tucson 2.0 xăng tiêu chuẩn bản nâng cấp",
                    ColorCode = "NKA",
                    ColorName = "Đen Phantom",
                    EngineNo = "G4NM-987654",
                    StorageCode = "KHO_TONG_SG",
                    StorageName = "Kho Tổng Nam Bộ Hiệp Phước",
                    AssemblyStatus = "CKD",
                    ProductionMonth = "202602",
                    Status = CarVinStatus.Mapped,
                    MappedCarId = "CAR2026-TU1102",
                    CreatedAt = DateTime.Today.AddDays(-25)
                },
                new CarVinInventory
                {
                    OrgId = orgId,
                    Vin = "KMHE281BBSA334455",
                    ModelCode = "CR15",
                    ModelName = "Creta 1.5 Cao Cấp",
                    SpecCode = "CR15-PRE-02",
                    SpecDescription = "Creta 1.5 CVT bản cao cấp 2 tông màu",
                    ColorCode = "R2N",
                    ColorName = "Đỏ đô nóc đen",
                    EngineNo = "G4FL-334455",
                    StorageCode = "KHO_TONG_HN",
                    StorageName = "Kho Tổng Hà Nội - Đài Tư",
                    AssemblyStatus = "CKD",
                    ProductionMonth = "202602",
                    Status = CarVinStatus.Mapped,
                    MappedCarId = "CAR2026-CR0192",
                    CreatedAt = DateTime.Today.AddDays(-10)
                },
                new CarVinInventory
                {
                    OrgId = orgId,
                    Vin = "KMHE281BBSA223344",
                    ModelCode = "AC14",
                    ModelName = "Accent 1.4 AT",
                    SpecCode = "AC14-STD-01",
                    SpecDescription = "Accent 1.4 AT Sedan gia đình",
                    ColorCode = "SL2",
                    ColorName = "Bạc",
                    EngineNo = "G4LC-223344",
                    StorageCode = "KHO_TONG_HN",
                    StorageName = "Kho Tổng Hyundai Ninh Bình",
                    AssemblyStatus = "CKD",
                    ProductionMonth = "202603",
                    Status = CarVinStatus.Mapped,
                    MappedCarId = "CAR2026-AC1401",
                    CreatedAt = DateTime.Today.AddDays(-8)
                },

                // Các xe còn trống trong kho (Available) sẵn sàng cho Map VIN
                new CarVinInventory
                {
                    OrgId = orgId,
                    Vin = "KMHE281BBSB100001",
                    ModelCode = "SF25",
                    ModelName = "Santa Fe 2.5 Xăng Cao Cấp",
                    SpecCode = "SF25-PRE-01",
                    SpecDescription = "Santa Fe 2.5 CKD xăng cao cấp",
                    ColorCode = "WH1",
                    ColorName = "Trắng",
                    EngineNo = "G4KP-100001",
                    StorageCode = "KHO_TONG_HN",
                    StorageName = "Kho Tổng Hyundai Ninh Bình",
                    AssemblyStatus = "CKD",
                    ProductionMonth = "202603",
                    Status = CarVinStatus.Available,
                    MappedCarId = null,
                    CreatedAt = DateTime.Today.AddDays(-5)
                },
                new CarVinInventory
                {
                    OrgId = orgId,
                    Vin = "KMHE281BBSB100002",
                    ModelCode = "TU20",
                    ModelName = "Tucson 2.0 AT",
                    SpecCode = "TU20-STD-01",
                    SpecDescription = "Tucson 2.0 xăng tiêu chuẩn bản nâng cấp",
                    ColorCode = "NKA",
                    ColorName = "Đen Phantom",
                    EngineNo = "G4NM-100002",
                    StorageCode = "KHO_TONG_SG",
                    StorageName = "Kho Tổng Nam Bộ Hiệp Phước",
                    AssemblyStatus = "CKD",
                    ProductionMonth = "202603",
                    Status = CarVinStatus.Available,
                    MappedCarId = null,
                    CreatedAt = DateTime.Today.AddDays(-5)
                },
                new CarVinInventory
                {
                    OrgId = orgId,
                    Vin = "KMHE281BBSB100003",
                    ModelCode = "CR15",
                    ModelName = "Creta 1.5 Cao Cấp",
                    SpecCode = "CR15-PRE-02",
                    SpecDescription = "Creta 1.5 CVT bản cao cấp 2 tông màu",
                    ColorCode = "R2N",
                    ColorName = "Đỏ đô nóc đen",
                    EngineNo = "G4FL-100003",
                    StorageCode = "KHO_TONG_HN",
                    StorageName = "Kho Tổng Hà Nội - Đài Tư",
                    AssemblyStatus = "CKD",
                    ProductionMonth = "202603",
                    Status = CarVinStatus.Available,
                    MappedCarId = null,
                    CreatedAt = DateTime.Today.AddDays(-4)
                },
                new CarVinInventory
                {
                    OrgId = orgId,
                    Vin = "KMHE281BBSB100004",
                    ModelCode = "AC14",
                    ModelName = "Accent 1.4 AT",
                    SpecCode = "AC14-STD-01",
                    SpecDescription = "Accent 1.4 AT Sedan gia đình",
                    ColorCode = "SL2",
                    ColorName = "Bạc",
                    EngineNo = "G4LC-100004",
                    StorageCode = "KHO_TONG_HN",
                    StorageName = "Kho Tổng Hyundai Ninh Bình",
                    AssemblyStatus = "CKD",
                    ProductionMonth = "202603",
                    Status = CarVinStatus.Available,
                    MappedCarId = null,
                    CreatedAt = DateTime.Today.AddDays(-3)
                },
                new CarVinInventory
                {
                    OrgId = orgId,
                    Vin = "KMHE281BBSB100005",
                    ModelCode = "CS20",
                    ModelName = "Custin 2.0T Cao Cấp",
                    SpecCode = "CS20-PRE-01",
                    SpecDescription = "Custin 2.0 Turbo Bản Cao Cấp 7 chỗ",
                    ColorCode = "WW1",
                    ColorName = "Trắng tuyết",
                    EngineNo = "G4NN-100005",
                    StorageCode = "KHO_TONG_SG",
                    StorageName = "Kho Tổng Nam Bộ Hiệp Phước",
                    AssemblyStatus = "CBU",
                    ProductionMonth = "202602",
                    Status = CarVinStatus.Available,
                    MappedCarId = null,
                    CreatedAt = DateTime.Today.AddDays(-2)
                },
                new CarVinInventory
                {
                    OrgId = orgId,
                    Vin = "KMHE281BBSB100006",
                    ModelCode = "I10",
                    ModelName = "Grand i10 Sedan 1.2 AT",
                    SpecCode = "I10-SED-01",
                    SpecDescription = "Grand i10 Sedan 1.2 số tự động",
                    ColorCode = "SL1",
                    ColorName = "Bạc",
                    EngineNo = "G4LA-100006",
                    StorageCode = "KHO_TONG_HN",
                    StorageName = "Kho Tổng Hyundai Ninh Bình",
                    AssemblyStatus = "CKD",
                    ProductionMonth = "202603",
                    Status = CarVinStatus.Available,
                    MappedCarId = null,
                    CreatedAt = DateTime.Today.AddDays(-2)
                }
            };
            db.CarVinInventories.AddRange(vins);

            // Phiên Map VIN mẫu ban đầu
            var s1 = new MapVinSession
            {
                OrgId = orgId,
                SessionNo = $"{DateTime.Today:yyMM}MV0001",
                MapType = MapVinType.Auto,
                Method = MapVinMethod.Normal,
                Status = MapVinSessionStatus.Approved,
                DealerCode = null,
                ModelCode = null,
                TotalRequested = 2,
                TotalMapped = 2,
                TotalUnmapped = 0,
                Remark = "Phiên tự động phân bổ Map VIN đợt 1 tháng theo kế hoạch sản xuất HTMV",
                CreatedBy = "HỆ THỐNG PHÂN BỔ MAP VIN",
                CreatedAt = DateTime.Today.AddDays(-15),
                ApprovedBy = "Đặng Phương Nam (Phòng Bán buôn HTC)",
                ApprovedAt = DateTime.Today.AddDays(-15),
                Details = new List<MapVinSessionDetail>
                {
                    new MapVinSessionDetail
                    {
                        CarId = "CAR2026-SF0988",
                        SoCode = "ORD2603010001",
                        DealerCode = "VN001",
                        DealerName = "Hyundai Đông Đô",
                        ModelCode = "SF25",
                        ModelName = "Santa Fe 2.5 HTRAC",
                        SpecCode = "SF25-PRE-01",
                        ColorCode = "WH1",
                        ColorName = "Trắng",
                        Vin = "KMHE281BBSA129841",
                        EngineNo = "G4KP-129841",
                        StorageCode = "KHO_TONG_HN",
                        StorageName = "Kho Tổng Hyundai Ninh Bình",
                        Status = MapVinDetailStatus.Mapped,
                        MappedAt = DateTime.Today.AddDays(-15),
                        MappedBy = "Đặng Phương Nam (Phòng Bán buôn HTC)"
                    },
                    new MapVinSessionDetail
                    {
                        CarId = "CAR2026-TU1102",
                        SoCode = "ORD2603010002",
                        DealerCode = "VN002",
                        DealerName = "Hyundai Nam Trung",
                        ModelCode = "TU20",
                        ModelName = "Tucson 2.0 AT",
                        SpecCode = "TU20-STD-01",
                        ColorCode = "NKA",
                        ColorName = "Đen Phantom",
                        Vin = "KMHE281BBSA987654",
                        EngineNo = "G4NM-987654",
                        StorageCode = "KHO_TONG_SG",
                        StorageName = "Kho Tổng Nam Bộ Hiệp Phước",
                        Status = MapVinDetailStatus.Mapped,
                        MappedAt = DateTime.Today.AddDays(-15),
                        MappedBy = "Đặng Phương Nam (Phòng Bán buôn HTC)"
                    }
                }
            };
            db.MapVinSessions.Add(s1);

            var mapLogs = new List<MapVinAuditLog>
            {
                new MapVinAuditLog
                {
                    OrgId = orgId,
                    CarId = "CAR2026-SF0988",
                    Vin = "KMHE281BBSA129841",
                    Action = "AutoMap",
                    SessionNo = s1.SessionNo,
                    Remark = $"Tự động ghép số khung VIN KMHE281BBSA129841 theo phiên {s1.SessionNo}",
                    PerformedBy = "Đặng Phương Nam (Phòng Bán buôn HTC)",
                    PerformedAt = DateTime.Today.AddDays(-15)
                },
                new MapVinAuditLog
                {
                    OrgId = orgId,
                    CarId = "CAR2026-TU1102",
                    Vin = "KMHE281BBSA987654",
                    Action = "AutoMap",
                    SessionNo = s1.SessionNo,
                    Remark = $"Tự động ghép số khung VIN KMHE281BBSA987654 theo phiên {s1.SessionNo}",
                    PerformedBy = "Đặng Phương Nam (Phòng Bán buôn HTC)",
                    PerformedAt = DateTime.Today.AddDays(-15)
                }
            };
            db.MapVinAuditLogs.AddRange(mapLogs);

            await db.SaveChangesAsync();
        }

        if (!await db.RetailInvoiceRequests.AnyAsync(o => o.OrgId == orgId))
        {
            var ri1 = new RetailInvoiceRequest
            {
                OrgId = orgId,
                ReqInvoiceNo = "2603RI0001",
                DealerCode = "VN001",
                DealerName = "Hyundai Đông Đô",
                ReqInvoiceType = RetailInvoiceType.Root,
                DlrContractNo = "RC26090001",
                DealNo = "DEAL26090001",
                CustomerCode = "CUS00128",
                CustomerName = "Nguyễn Thành Long",
                CustomerType = "INDIVIDUAL",
                CustomerAddress = "18 Tam Trinh, Hoàng Mai, Hà Nội",
                CustomerEmail = "long.nt@gmail.com",
                CustomerPhone = "0988123456",
                MST = "001095012345",
                TInvoiceCode = "1/001",
                InvoiceCode = "LK9A8B7C6D5E",
                InvoiceNo = "0000018",
                InvoiceDate = DateTime.Today.AddDays(-3),
                InvoiceSign = "1C26TAA",
                FormNo = "1/001",
                InvoiceStatus = RetailInvoiceEInvoiceStatus.Issued,
                AdjType = RetailInvoiceAdjType.None,
                Remark = "Hóa đơn GTGT điện tử bán lẻ xe Santa Fe 2.5 HTRAC và gói phụ kiện cao cấp",
                ReqInvoiceStatus = RetailInvoiceRequestStatus.Issued,
                FlagInvoice = "1",
                FlagIsQInvoice = 1,
                QtyCtrCarId = 1,
                TotalTPBeforeVAT = 1220000000m,
                TotalValVAT = 122000000m,
                TotalTPAfterVAT = 1342000000m,
                TaxAuthorityCode = "TCT260320-A1B2C3D4",
                InvoiceFileUrl = "/invoices/retail/2603RI0001.pdf",
                InvoiceFileName = "2603RI0001_0000018.pdf",
                CreatedBy = "DEALER_ACCOUNTANT",
                CreatedAt = DateTime.Today.AddDays(-4),
                IssuedBy = "Trần Thị Mai (Kế toán Trưởng Đông Đô)",
                IssuedAt = DateTime.Today.AddDays(-3),
                ApprovedBy = "Nguyễn Hoàng Nam (Giám đốc Đại lý Đông Đô)",
                ApprovedAt = DateTime.Today.AddDays(-3),
                TaxTransmittedBy = "HỆ THỐNG Q-INVOICE TCT",
                TaxTransmittedAt = DateTime.Today.AddDays(-3),
                Details = new List<RetailInvoiceRequestDetail>
                {
                    new RetailInvoiceRequestDetail
                    {
                        ReqInvoiceNo = "2603RI0001",
                        CtrCarId = "RC26090001.01",
                        Vin = "KMHE281BBSA129841",
                        BrandName = "Hyundai",
                        CarType = "Ô tô con",
                        ModelCode = "SF25",
                        ModelName = "Santa Fe 2.5 HTRAC",
                        SpecCode = "SF25-PRE-01",
                        SpecDescription = "Santa Fe 2.5 xăng cao cấp dẫn động 4 bánh HTRAC",
                        ColorCode = "WW2",
                        ColorName = "Trắng ngọc trai",
                        EngineNo = "G4KP-102941",
                        NumberOfSeats = 7,
                        HTCInvoiceNo = "INV260301-0012",
                        HTCInvoiceDate = DateTime.Today.AddDays(-10),
                        ProductionYearActual = "2026",
                        CQNo = "CQ-2026-SF129841",
                        FGFormNo = "PXX-2026-9812",
                        UPBeforeVAT = 1200000000m,
                        VatRate = 10m,
                        ValVAT = 120000000m,
                        UPAfterVAT = 1320000000m,
                        Status = "Invoiced",
                        Remark = "Xe đã bàn giao biên bản BBBG đầy đủ"
                    }
                },
                Products = new List<RetailInvoiceRequestProduct>
                {
                    new RetailInvoiceRequestProduct
                    {
                        ReqInvoiceNo = "2603RI0001",
                        Idx = 1,
                        ProductCode = "PK-FILM3M",
                        ProductName = "Gói dán phim cách nhiệt 3M Crystalline cao cấp toàn xe",
                        Unit = "Gói",
                        Qty = 1,
                        VatRate = 10m,
                        UnitPrice = 15000000m,
                        TPBeforeVAT = 15000000m,
                        ValVAT = 1500000m,
                        TPAfterVAT = 16500000m,
                        FlagIsProduct = true,
                        Remark = "Bảo hành 10 năm điện tử chính hãng 3M"
                    },
                    new RetailInvoiceRequestProduct
                    {
                        ReqInvoiceNo = "2603RI0001",
                        Idx = 2,
                        ProductCode = "PK-CAMDASH",
                        ProductName = "Camera hành trình Vietmap SpeedMap M1 cảnh báo tốc độ",
                        Unit = "Bộ",
                        Qty = 1,
                        VatRate = 10m,
                        UnitPrice = 5000000m,
                        TPBeforeVAT = 5000000m,
                        ValVAT = 500000m,
                        TPAfterVAT = 5500000m,
                        FlagIsProduct = true,
                        Remark = "Kèm thẻ nhớ 64GB tốc độ cao"
                    }
                }
            };

            var ri2 = new RetailInvoiceRequest
            {
                OrgId = orgId,
                ReqInvoiceNo = "2603RI0002",
                DealerCode = "VN002",
                DealerName = "Hyundai Nam Trung",
                ReqInvoiceType = RetailInvoiceType.Root,
                DlrContractNo = "RC26090002",
                DealNo = "DEAL26090002",
                CustomerCode = "CUS00204",
                CustomerName = "Công ty CP Dịch vụ Vận tải Nam Trung",
                CustomerType = "ORGANIZATION",
                CustomerAddress = "Tòa nhà Nam Trung, Lê Văn Lương kéo dài, Hà Đông, Hà Nội",
                CustomerEmail = "ketoan@namtrungtrans.vn",
                CustomerPhone = "0912345678",
                MST = "0108998877",
                TInvoiceCode = "1/001",
                InvoiceCode = "LK3D4E5F6G7H",
                InvoiceNo = "0000019",
                InvoiceDate = DateTime.Today.AddDays(-2),
                InvoiceSign = "1C26TAA",
                FormNo = "1/001",
                InvoiceStatus = RetailInvoiceEInvoiceStatus.Issued,
                AdjType = RetailInvoiceAdjType.None,
                Remark = "Hóa đơn điện tử bán xe Tucson 2.0 AT xuất cho khách hàng doanh nghiệp",
                ReqInvoiceStatus = RetailInvoiceRequestStatus.Issued,
                FlagInvoice = "1",
                FlagIsQInvoice = 1,
                QtyCtrCarId = 1,
                TotalTPBeforeVAT = 862500000m,
                TotalValVAT = 86250000m,
                TotalTPAfterVAT = 948750000m,
                TaxAuthorityCode = "TCT260321-X9Y8Z7W6",
                InvoiceFileUrl = "/invoices/retail/2603RI0002.pdf",
                InvoiceFileName = "2603RI0002_0000019.pdf",
                CreatedBy = "DEALER_ACCOUNTANT",
                CreatedAt = DateTime.Today.AddDays(-3),
                IssuedBy = "Lê Thị Hồng (Kế toán Trưởng Nam Trung)",
                IssuedAt = DateTime.Today.AddDays(-2),
                ApprovedBy = "Vũ Nam Trung (Giám đốc)",
                ApprovedAt = DateTime.Today.AddDays(-2),
                TaxTransmittedBy = "HỆ THỐNG Q-INVOICE TCT",
                TaxTransmittedAt = DateTime.Today.AddDays(-2),
                Details = new List<RetailInvoiceRequestDetail>
                {
                    new RetailInvoiceRequestDetail
                    {
                        ReqInvoiceNo = "2603RI0002",
                        CtrCarId = "RC26090002.01",
                        Vin = "KMHE281BBSA987654",
                        BrandName = "Hyundai",
                        CarType = "Ô tô con",
                        ModelCode = "TU20",
                        ModelName = "Tucson 2.0 AT",
                        SpecCode = "TU20-STD-01",
                        SpecDescription = "Tucson 2.0 máy xăng số tự động tiêu chuẩn",
                        ColorCode = "NKA",
                        ColorName = "Đen Phantom",
                        EngineNo = "G4NM-987654",
                        NumberOfSeats = 5,
                        HTCInvoiceNo = "INV260301-0015",
                        HTCInvoiceDate = DateTime.Today.AddDays(-12),
                        ProductionYearActual = "2026",
                        CQNo = "CQ-2026-TU987654",
                        FGFormNo = "PXX-2026-7788",
                        UPBeforeVAT = 860000000m,
                        VatRate = 10m,
                        ValVAT = 86000000m,
                        UPAfterVAT = 946000000m,
                        Status = "Invoiced",
                        Remark = "Đã xuất hóa đơn doanh nghiệp đầy đủ MST"
                    }
                },
                Products = new List<RetailInvoiceRequestProduct>
                {
                    new RetailInvoiceRequestProduct
                    {
                        ReqInvoiceNo = "2603RI0002",
                        Idx = 1,
                        ProductCode = "PK-FLOOR",
                        ProductName = "Bộ thảm lót sàn da 6D carbon cao cấp xe Tucson",
                        Unit = "Bộ",
                        Qty = 1,
                        VatRate = 10m,
                        UnitPrice = 2500000m,
                        TPBeforeVAT = 2500000m,
                        ValVAT = 250000m,
                        TPAfterVAT = 2750000m,
                        FlagIsProduct = true,
                        Remark = "Màu đen viền chỉ đỏ"
                    }
                }
            };

            var ri3 = new RetailInvoiceRequest
            {
                OrgId = orgId,
                ReqInvoiceNo = "2603RI0003",
                DealerCode = "VN001",
                DealerName = "Hyundai Đông Đô",
                ReqInvoiceType = RetailInvoiceType.Root,
                DlrContractNo = "RC26090003",
                DealNo = "DEAL26090003",
                CustomerCode = "CUS00311",
                CustomerName = "Lê Thị Mai",
                CustomerType = "INDIVIDUAL",
                CustomerAddress = "25 phố Vọng, Hai Bà Trưng, Hà Nội",
                CustomerEmail = "maile89@gmail.com",
                CustomerPhone = "0909876543",
                MST = "001189033445",
                TInvoiceCode = "1/001",
                InvoiceSign = "1C26TAA",
                FormNo = "1/001",
                InvoiceStatus = RetailInvoiceEInvoiceStatus.Draft,
                AdjType = RetailInvoiceAdjType.None,
                Remark = "Đề nghị xuất hóa đơn xe Creta 1.5 Cao Cấp kèm gói phủ bóng Ceramic",
                ReqInvoiceStatus = RetailInvoiceRequestStatus.Pending,
                FlagInvoice = "0",
                FlagIsQInvoice = 1,
                QtyCtrCarId = 1,
                TotalTPBeforeVAT = 662545455m,
                TotalValVAT = 66254545m,
                TotalTPAfterVAT = 728800000m,
                CreatedBy = "DEALER_ACCOUNTANT",
                CreatedAt = DateTime.Today.AddDays(-1),
                Details = new List<RetailInvoiceRequestDetail>
                {
                    new RetailInvoiceRequestDetail
                    {
                        ReqInvoiceNo = "2603RI0003",
                        CtrCarId = "RC26090003.01",
                        Vin = "KMHE281BBSA334455",
                        BrandName = "Hyundai",
                        CarType = "Ô tô con",
                        ModelCode = "CR15",
                        ModelName = "Creta 1.5 Cao Cấp",
                        SpecCode = "CR15-PRE-02",
                        SpecDescription = "Creta 1.5 CVT bản cao cấp 2 tông màu",
                        ColorCode = "R3R",
                        ColorName = "Đỏ mận",
                        EngineNo = "G4FL-334455",
                        NumberOfSeats = 5,
                        HTCInvoiceNo = "INV260301-0020",
                        HTCInvoiceDate = DateTime.Today.AddDays(-8),
                        ProductionYearActual = "2026",
                        CQNo = "CQ-2026-CR334455",
                        FGFormNo = "PXX-2026-6655",
                        UPBeforeVAT = 654545455m,
                        VatRate = 10m,
                        ValVAT = 65454545m,
                        UPAfterVAT = 720000000m,
                        Status = "Pending",
                        Remark = "Chờ kế toán trưởng cấp số hóa đơn điện tử"
                    }
                },
                Products = new List<RetailInvoiceRequestProduct>
                {
                    new RetailInvoiceRequestProduct
                    {
                        ReqInvoiceNo = "2603RI0003",
                        Idx = 1,
                        ProductCode = "DV-CERAMIC",
                        ProductName = "Gói phủ bóng Ceramic bảo vệ bề mặt sơn xe 3 lớp",
                        Unit = "Gói",
                        Qty = 1,
                        VatRate = 10m,
                        UnitPrice = 8000000m,
                        TPBeforeVAT = 8000000m,
                        ValVAT = 800000m,
                        TPAfterVAT = 8800000m,
                        FlagIsProduct = false,
                        Remark = "Bảo dưỡng lớp phủ định kỳ 6 tháng/lần"
                    }
                }
            };

            var ri4 = new RetailInvoiceRequest
            {
                OrgId = orgId,
                ReqInvoiceNo = "2603RI0004",
                DealerCode = "VN001",
                DealerName = "Hyundai Đông Đô",
                ReqInvoiceType = RetailInvoiceType.Adjusted,
                ReqInvoiceBaseNo = "2603RI0001",
                InvoiceBaseNo = "0000018",
                DlrContractNo = "RC26090001",
                DealNo = "DEAL26090001",
                CustomerCode = "CUS00128",
                CustomerName = "Nguyễn Thành Long",
                CustomerType = "INDIVIDUAL",
                CustomerAddress = "18 Tam Trinh, Hoàng Mai, Hà Nội",
                CustomerEmail = "long.nt@gmail.com",
                CustomerPhone = "0988123456",
                MST = "001095012345",
                TInvoiceCode = "1/001",
                InvoiceSign = "1C26TAA",
                FormNo = "1/001",
                InvoiceStatus = RetailInvoiceEInvoiceStatus.Draft,
                AdjType = RetailInvoiceAdjType.Increase,
                Remark = "Đề nghị xuất hóa đơn điều chỉnh tăng giá trị do bổ sung gói bảo hiểm vật chất thân vỏ PTI 1 năm",
                ReqInvoiceStatus = RetailInvoiceRequestStatus.Pending,
                FlagInvoice = "0",
                FlagIsQInvoice = 1,
                QtyCtrCarId = 0,
                TotalTPBeforeVAT = 18000000m,
                TotalValVAT = 1800000m,
                TotalTPAfterVAT = 19800000m,
                CreatedBy = "DEALER_ACCOUNTANT",
                CreatedAt = DateTime.Today,
                Products = new List<RetailInvoiceRequestProduct>
                {
                    new RetailInvoiceRequestProduct
                    {
                        ReqInvoiceNo = "2603RI0004",
                        Idx = 1,
                        ProductCode = "DV-INSURANCE",
                        ProductName = "Gói Bảo hiểm vật chất thân vỏ xe ô tô PTI (thời hạn 1 năm)",
                        Unit = "Gói",
                        Qty = 1,
                        VatRate = 10m,
                        UnitPrice = 18000000m,
                        TPBeforeVAT = 18000000m,
                        ValVAT = 1800000m,
                        TPAfterVAT = 19800000m,
                        FlagIsProduct = false,
                        Remark = "Phí bảo hiểm bổ sung điều chỉnh tăng vào hợp đồng mua xe"
                    }
                }
            };

            var ri5 = new RetailInvoiceRequest
            {
                OrgId = orgId,
                ReqInvoiceNo = "2603RI0005",
                DealerCode = "VN003",
                DealerName = "Hyundai Tây Hồ",
                ReqInvoiceType = RetailInvoiceType.Root,
                CustomerCode = "CUS00999",
                CustomerName = "Hoàng Minh Tuấn",
                CustomerType = "INDIVIDUAL",
                CustomerPhone = "0982223344",
                InvoiceStatus = RetailInvoiceEInvoiceStatus.Cancelled,
                AdjType = RetailInvoiceAdjType.None,
                Remark = "Khách hàng đổi sang đứng tên đăng ký công ty, hủy để lập đề nghị mới theo MST doanh nghiệp",
                ReqInvoiceStatus = RetailInvoiceRequestStatus.Cancelled,
                FlagInvoice = "0",
                FlagIsQInvoice = 1,
                QtyCtrCarId = 0,
                TotalTPBeforeVAT = 0m,
                TotalValVAT = 0m,
                TotalTPAfterVAT = 0m,
                CancelReason = "Khách hàng đổi chủ thể mua xe sang doanh nghiệp, hủy đề nghị cá nhân",
                CancelledBy = "DEALER_ADMIN",
                CancelledAt = DateTime.Today,
                CreatedBy = "DEALER_ACCOUNTANT",
                CreatedAt = DateTime.Today.AddDays(-2)
            };

            db.RetailInvoiceRequests.AddRange(ri1, ri2, ri3, ri4, ri5);
            await db.SaveChangesAsync();
        }

        if (!await db.CarRedeemRequests.AnyAsync(o => o.OrgId == orgId))
        {
            var rdm1 = new CarRedeemRequest
            {
                OrgId = orgId,
                ReqDMNo = "2603RDM00001",
                DealerCode = "VN001",
                DealerName = "Hyundai Đông Đô",
                Status = CarRedeemStatus.Approved,
                RedeemType = CarRedeemType.Direct,
                TotalCars = 2,
                ApprovedCars = 2,
                BankCode = "VCB",
                BankName = "Ngân hàng Ngoại thương Việt Nam (Vietcombank)",
                Remark = "Đề nghị giải chấp lô 02 xe Santa Fe & Tucson thế chấp Vietcombank rút hồ sơ gốc bàn giao khách hàng",
                CreatedBy = "DL_NV_HO",
                CreatedAt = DateTime.Today.AddDays(-10),
                ApprovedBy = "HTC_SUPERVISOR",
                ApprovedAt = DateTime.Today.AddDays(-8),
                Details = new List<CarRedeemRequestDetail>
                {
                    new CarRedeemRequestDetail
                    {
                        ReqDMNo = "2603RDM00001",
                        CarId = "CAR-KMHE281BBSA129841",
                        Vin = "KMHE281BBSA129841",
                        ModelCode = "SF25",
                        ModelName = "Santa Fe 2.5 HTRAC",
                        SpecCode = "SF25-PRE",
                        SpecDescription = "Santa Fe 2.5 xăng cao cấp",
                        ColorCode = "WH",
                        ColorName = "Trắng",
                        EngineNo = "G4KP123456",
                        CQNo = "CQ-2026-129841",
                        CONo = "CO-2026-HQ12984",
                        DeclarationNo = "TKHQ-2026-9841",
                        MortageBankCode = "HTC.HO",
                        MortageBankName = "Nhà phân phối ô tô Hyundai (HTC Head Office - Không thế chấp)",
                        TypeDMReq = CarRedeemType.Direct,
                        DealerCode = "VN001",
                        DealerName = "Hyundai Đông Đô",
                        DRListCode = "DRL2603001",
                        DocumentsStatus = "Full",
                        Status = CarRedeemDtlStatus.Approved,
                        ApprovedAt = DateTime.Today.AddDays(-8),
                        ApprovedBy = "HTC_SUPERVISOR",
                        Remark = "Đã hoàn tất thanh toán cọc và nghĩa vụ tài chính, giải chấp tài sản gốc"
                    },
                    new CarRedeemRequestDetail
                    {
                        ReqDMNo = "2603RDM00001",
                        CarId = "CAR-KMHE281BBSA129842",
                        Vin = "KMHE281BBSA129842",
                        ModelCode = "TU20",
                        ModelName = "Tucson 2.0 AT",
                        SpecCode = "TU20-STD",
                        SpecDescription = "Tucson 2.0 máy dầu đặc biệt",
                        ColorCode = "BK",
                        ColorName = "Đen",
                        EngineNo = "D4HD654321",
                        CQNo = "CQ-2026-129842",
                        CONo = "CO-2026-HQ12985",
                        DeclarationNo = "TKHQ-2026-9842",
                        MortageBankCode = "HTC.HO",
                        MortageBankName = "Nhà phân phối ô tô Hyundai (HTC Head Office - Không thế chấp)",
                        TypeDMReq = CarRedeemType.Direct,
                        DealerCode = "VN001",
                        DealerName = "Hyundai Đông Đô",
                        DRListCode = "DRL2603001",
                        DocumentsStatus = "Full",
                        Status = CarRedeemDtlStatus.Approved,
                        ApprovedAt = DateTime.Today.AddDays(-8),
                        ApprovedBy = "HTC_SUPERVISOR",
                        Remark = "Đã hoàn tất thủ tục giải chấp tài sản bảo đảm ngân hàng"
                    }
                }
            };

            var rdm2 = new CarRedeemRequest
            {
                OrgId = orgId,
                ReqDMNo = "2603RDM00002",
                DealerCode = "VN002",
                DealerName = "Hyundai Nam Trung",
                Status = CarRedeemStatus.Pending,
                RedeemType = CarRedeemType.Guarantee,
                TotalCars = 2,
                ApprovedCars = 1,
                BankCode = "TCB",
                BankName = "Ngân hàng Kỹ thương Việt Nam (Techcombank)",
                Remark = "Đề nghị giải chấp theo thư bảo lãnh ngân hàng Techcombank đợt thanh toán quý 1",
                CreatedBy = "DL_SALES_MNGR",
                CreatedAt = DateTime.Today.AddDays(-2),
                Details = new List<CarRedeemRequestDetail>
                {
                    new CarRedeemRequestDetail
                    {
                        ReqDMNo = "2603RDM00002",
                        CarId = "CAR-KMHE281BBSA129843",
                        Vin = "KMHE281BBSA129843",
                        ModelCode = "CR15",
                        ModelName = "Creta 1.5 Cao Cấp",
                        SpecCode = "CR15-PRE",
                        SpecDescription = "Creta 1.5 cao cấp 2 tone",
                        ColorCode = "RD",
                        ColorName = "Đỏ",
                        EngineNo = "G4FL789012",
                        CQNo = "CQ-2026-129843",
                        CONo = "CO-2026-HQ12986",
                        DeclarationNo = "TKHQ-2026-9843",
                        MortageBankCode = "HTC.HO",
                        MortageBankName = "Nhà phân phối ô tô Hyundai (HTC Head Office - Không thế chấp)",
                        TypeDMReq = CarRedeemType.Guarantee,
                        DealerCode = "VN002",
                        DealerName = "Hyundai Nam Trung",
                        GuaranteeNo = "BL26030001",
                        DRListCode = "DRL2603002",
                        DocumentsStatus = "Full",
                        Status = CarRedeemDtlStatus.Approved,
                        ApprovedAt = DateTime.Today.AddDays(-1),
                        ApprovedBy = "HTC_SUPERVISOR",
                        Remark = "Đã đối soát thư bảo lãnh Techcombank khớp lệnh UNC"
                    },
                    new CarRedeemRequestDetail
                    {
                        ReqDMNo = "2603RDM00002",
                        CarId = "CAR-KMHE281BBSA129844",
                        Vin = "KMHE281BBSA129844",
                        ModelCode = "SF25",
                        ModelName = "Santa Fe 2.5 HTRAC",
                        SpecCode = "SF25-PRE",
                        SpecDescription = "Santa Fe 2.5 xăng cao cấp",
                        ColorCode = "GR",
                        ColorName = "Xám",
                        EngineNo = "G4KP345678",
                        CQNo = "CQ-2026-129844",
                        CONo = "CO-2026-HQ12987",
                        DeclarationNo = "TKHQ-2026-9844",
                        MortageBankCode = "TCB",
                        MortageBankName = "Ngân hàng Kỹ thương Việt Nam (Techcombank)",
                        TypeDMReq = CarRedeemType.Guarantee,
                        DealerCode = "VN002",
                        DealerName = "Hyundai Nam Trung",
                        GuaranteeNo = "BL26030001",
                        DRListCode = "DRL2603002",
                        DocumentsStatus = "Full",
                        Status = CarRedeemDtlStatus.Pending,
                        Remark = "Chờ phòng Tài chính NPP kiểm tra đối trừ dư nợ bảo lãnh"
                    }
                }
            };

            var rdm3 = new CarRedeemRequest
            {
                OrgId = orgId,
                ReqDMNo = "2603RDM00003",
                DealerCode = "VN003",
                DealerName = "Hyundai Tây Hồ",
                Status = CarRedeemStatus.Rejected,
                RedeemType = CarRedeemType.Direct,
                TotalCars = 1,
                ApprovedCars = 0,
                BankCode = "BIDV",
                BankName = "Ngân hàng Đầu tư và Phát triển Việt Nam (BIDV)",
                Remark = "Đề nghị giải chấp rút hồ sơ xe Stargazer 1.5 AT",
                CreatedBy = "DL_ACCOUNTANT",
                CreatedAt = DateTime.Today.AddDays(-5),
                RejectedBy = "HTC_ACCOUNTING",
                RejectedAt = DateTime.Today.AddDays(-4),
                RejectReason = "Đại lý chưa thanh toán đủ số dư nợ bảo lãnh quá hạn tại BIDV, yêu cầu tất toán trước khi giải chấp",
                Details = new List<CarRedeemRequestDetail>
                {
                    new CarRedeemRequestDetail
                    {
                        ReqDMNo = "2603RDM00003",
                        CarId = "CAR-KMHE281BBSA129845",
                        Vin = "KMHE281BBSA129845",
                        ModelCode = "SG15",
                        ModelName = "Stargazer X 1.5",
                        SpecCode = "SG15-PRE",
                        SpecDescription = "Stargazer X Cao Cấp",
                        ColorCode = "WH",
                        ColorName = "Trắng",
                        EngineNo = "G4FL901234",
                        CQNo = "CQ-2026-129845",
                        CONo = "CO-2026-HQ12988",
                        DeclarationNo = "TKHQ-2026-9845",
                        MortageBankCode = "BIDV",
                        MortageBankName = "Ngân hàng Đầu tư và Phát triển Việt Nam (BIDV)",
                        TypeDMReq = CarRedeemType.Direct,
                        DealerCode = "VN003",
                        DealerName = "Hyundai Tây Hồ",
                        DocumentsStatus = "Full",
                        Status = CarRedeemDtlStatus.Rejected,
                        Remark = "Từ chối: Chưa tất toán nợ quá hạn"
                    }
                }
            };

            db.CarRedeemRequests.AddRange(rdm1, rdm2, rdm3);
            await db.SaveChangesAsync();
        }

        if (!await db.InsuranceRequests.AnyAsync(o => o.OrgId == orgId))
        {
            var ins1 = new CarInsuranceRequest
            {
                OrgId = orgId,
                InsReqNo = "2603IN00001",
                InsCompanyCode = "BIC",
                InsCompanyName = "Tổng công ty Bảo hiểm BIDV",
                InsTypeCode = "BHVT",
                InsTypeName = "Bảo hiểm hàng hóa vận tải",
                EffectiveDate = DateTime.Today.AddDays(-10),
                ExpiryDate = DateTime.Today.AddDays(20),
                TotalCars = 2,
                ApprovedCars = 2,
                TotalAmount = 2_300_000_000m,
                TotalPremiumAmount = 3_450_000m,
                PolicyNo = "POL-BIC-2603-00088",
                PolicyDate = DateTime.Today.AddDays(-10),
                Status = CarInsuranceStatus.Approved,
                Remark = "Bảo hiểm vận chuyển lô xe giao cho đại lý Hyundai Đông Đô",
                CreatedBy = "TRANSP_DISPATCHER",
                CreatedDate = DateTime.Today.AddDays(-11),
                ApprovedBy = "BIC_OFFICER_01",
                ApprovedDate = DateTime.Today.AddDays(-10),
                Details = new List<CarInsuranceRequestDetail>
                {
                    new CarInsuranceRequestDetail
                    {
                        InsReqNo = "2603IN00001",
                        CarId = "CAR-KMHE281BBSA129841",
                        Vin = "KMHE281BBSA129841",
                        ModelCode = "SF25",
                        ModelName = "Santa Fe 2.5 HTRAC",
                        SpecCode = "SF-PREM",
                        SpecDescription = "Santa Fe 2.5 Xăng Cao Cấp",
                        ColorCode = "WH",
                        ColorName = "Trắng Tinh Khôi",
                        EngineNo = "G4KP-098231",
                        RefOrdType = CarInsuranceRefOrdType.CarTransport,
                        RefOrdNo = "DO2603010001",
                        TransporterCode = "TRP-VTA",
                        TransporterName = "Công ty TNHH Vận Tải An Phát",
                        LocationFrom = "Kho Tổng NPP Hyundai Hà Nam",
                        LocationTo = "Hyundai Đông Đô - Giải Phóng, Hà Nội",
                        ExpectedStartDate = DateTime.Today.AddDays(-10),
                        Price = 1_350_000_000m,
                        Rate = 0.15m,
                        InsuranceDay = 30,
                        InsAmount = 2_025_000m,
                        PolicyNo = "POL-BIC-2603-00088",
                        Status = CarInsuranceDtlStatus.Approved,
                        ApprovedBy = "BIC_OFFICER_01",
                        ApprovedDate = DateTime.Today.AddDays(-10),
                        Remark = "Xe kiểm tra ngoại quan nguyên đai nguyên kiện"
                    },
                    new CarInsuranceRequestDetail
                    {
                        InsReqNo = "2603IN00001",
                        CarId = "CAR-KMHE281BBSA129842",
                        Vin = "KMHE281BBSA129842",
                        ModelCode = "TU20",
                        ModelName = "Tucson 2.0 AT Đặc Biệt",
                        SpecCode = "TU-SPEC",
                        SpecDescription = "Tucson 2.0 AT Xăng Đặc Biệt",
                        ColorCode = "BK",
                        ColorName = "Đen Huyền Bí",
                        EngineNo = "G4NL-112349",
                        RefOrdType = CarInsuranceRefOrdType.CarTransport,
                        RefOrdNo = "DO2603010002",
                        TransporterCode = "TRP-VTA",
                        TransporterName = "Công ty TNHH Vận Tải An Phát",
                        LocationFrom = "Kho Tổng NPP Hyundai Hà Nam",
                        LocationTo = "Hyundai Đông Đô - Giải Phóng, Hà Nội",
                        ExpectedStartDate = DateTime.Today.AddDays(-10),
                        Price = 950_000_000m,
                        Rate = 0.15m,
                        InsuranceDay = 30,
                        InsAmount = 1_425_000m,
                        PolicyNo = "POL-BIC-2603-00088",
                        Status = CarInsuranceDtlStatus.Approved,
                        ApprovedBy = "BIC_OFFICER_01",
                        ApprovedDate = DateTime.Today.AddDays(-10),
                        Remark = "Xe xuất kho đúng tiêu chuẩn kỹ thuật xuất xưởng"
                    }
                }
            };

            var ins2 = new CarInsuranceRequest
            {
                OrgId = orgId,
                InsReqNo = "2603IN00002",
                InsCompanyCode = "PTI",
                InsCompanyName = "Tổng công ty Cổ phần Bảo hiểm Bưu điện",
                InsTypeCode = "BHVC",
                InsTypeName = "Bảo hiểm vật chất xe ô tô",
                EffectiveDate = DateTime.Today.AddDays(-2),
                ExpiryDate = DateTime.Today.AddDays(28),
                TotalCars = 2,
                ApprovedCars = 0,
                TotalAmount = 1_415_000_000m,
                TotalPremiumAmount = 3_537_500m,
                Status = CarInsuranceStatus.Pending,
                Remark = "Đề nghị cấp bảo hiểm thân vỏ xe điều chuyển liên kho nội bộ và xe thu hồi đại lý",
                CreatedBy = "STO_DISPATCHER",
                CreatedDate = DateTime.Today.AddDays(-2),
                Details = new List<CarInsuranceRequestDetail>
                {
                    new CarInsuranceRequestDetail
                    {
                        InsReqNo = "2603IN00002",
                        CarId = "CAR-KMHE281BBSA129843",
                        Vin = "KMHE281BBSA129843",
                        ModelCode = "CR15",
                        ModelName = "Creta 1.5 Cao Cấp",
                        SpecCode = "CR-PREM",
                        SpecDescription = "Creta 1.5 Cao Cấp 2 Tone",
                        ColorCode = "RD",
                        ColorName = "Đỏ Quyến Rũ",
                        EngineNo = "G4FL-449102",
                        RefOrdType = CarInsuranceRefOrdType.StorageRearrange,
                        RefOrdNo = "SR2603000001",
                        TransporterCode = "LOG-HTC",
                        TransporterName = "Đội Vận Tải Nội Bộ HTC",
                        LocationFrom = "Kho Cảng Đình Vũ Hải Phòng",
                        LocationTo = "Kho Tổng NPP Hyundai Hà Nam",
                        ExpectedStartDate = DateTime.Today.AddDays(-1),
                        Price = 730_000_000m,
                        Rate = 0.25m,
                        InsuranceDay = 30,
                        InsAmount = 1_825_000m,
                        Status = CarInsuranceDtlStatus.Pending,
                        Remark = "Xe CBU nhập khẩu nguyên chiếc đã xong thủ tục hải quan"
                    },
                    new CarInsuranceRequestDetail
                    {
                        InsReqNo = "2603IN00002",
                        CarId = "CAR-KMHE281BBSA129844",
                        Vin = "KMHE281BBSA129844",
                        ModelCode = "ST15",
                        ModelName = "Stargazer X Cao Cấp",
                        SpecCode = "ST-PREM",
                        SpecDescription = "Stargazer X 1.5 CVT Cao Cấp",
                        ColorCode = "SL",
                        ColorName = "Bạc Ánh Kim",
                        EngineNo = "G4FL-991203",
                        RefOrdType = CarInsuranceRefOrdType.CarRetrieve,
                        RefOrdNo = "CR2603000001",
                        TransporterCode = "LOG-HTC",
                        TransporterName = "Đội Vận Tải Nội Bộ HTC",
                        LocationFrom = "Hyundai Nam Trung - Nam Định",
                        LocationTo = "Kho Tổng NPP Hyundai Hà Nam",
                        ExpectedStartDate = DateTime.Today,
                        Price = 685_000_000m,
                        Rate = 0.25m,
                        InsuranceDay = 30,
                        InsAmount = 1_712_500m,
                        Status = CarInsuranceDtlStatus.Pending,
                        Remark = "Xe thu hồi từ đại lý về phục vụ điều phối vùng"
                    }
                }
            };

            var ins3 = new CarInsuranceRequest
            {
                OrgId = orgId,
                InsReqNo = "2603IN00003",
                InsCompanyCode = "PJICO",
                InsCompanyName = "Tổng công ty Cổ phần Bảo hiểm Petrolimex",
                InsTypeCode = "BHVT",
                InsTypeName = "Bảo hiểm hàng hóa vận tải",
                EffectiveDate = DateTime.Today.AddDays(-5),
                ExpiryDate = DateTime.Today.AddDays(25),
                TotalCars = 1,
                ApprovedCars = 0,
                TotalAmount = 820_000_000m,
                TotalPremiumAmount = 1_230_000m,
                Status = CarInsuranceStatus.Rejected,
                Remark = "Bảo hiểm vận chuyển xe chassis sang xưởng đóng thùng Nam Việt",
                RejectReason = "Tuyến vận tải đường đèo dốc đặc biệt mùa mưa lũ vượt khung rủi ro tiêu chuẩn, cần bổ sung phụ phí rủi ro cao",
                CreatedBy = "TRANSP_STAFF",
                CreatedDate = DateTime.Today.AddDays(-5),
                RejectedBy = "PJICO_UW_02",
                RejectedDate = DateTime.Today.AddDays(-4),
                Details = new List<CarInsuranceRequestDetail>
                {
                    new CarInsuranceRequestDetail
                    {
                        InsReqNo = "2603IN00003",
                        CarId = "CAR-KMHE281BBSA129845",
                        Vin = "KMHE281BBSA129845",
                        ModelCode = "EX8",
                        ModelName = "Hyundai Mighty EX8 GTL",
                        SpecCode = "EX8-CHAS",
                        SpecDescription = "Mighty EX8 Chassis Xe Tải",
                        ColorCode = "BL",
                        ColorName = "Xanh Cửu Long",
                        EngineNo = "D4CC-552140",
                        RefOrdType = CarInsuranceRefOrdType.StorageRearrangeCB,
                        RefOrdNo = "2603RCB000001",
                        TransporterCode = "LOG-HTC",
                        TransporterName = "Đội Vận Chuyển Chassis Chuyên Nghiệp",
                        LocationFrom = "Kho Tổng NPP Hyundai Hà Nam",
                        LocationTo = "Xưởng Đóng Thùng Nam Việt - Hưng Yên",
                        ExpectedStartDate = DateTime.Today.AddDays(-4),
                        Price = 820_000_000m,
                        Rate = 0.15m,
                        InsuranceDay = 30,
                        InsAmount = 1_230_000m,
                        Status = CarInsuranceDtlStatus.Rejected,
                        Remark = "Từ chối duyệt: Cần thẩm định lại tuyến đường vận tải"
                    }
                }
            };

            db.InsuranceRequests.AddRange(ins1, ins2, ins3);
            await db.SaveChangesAsync();
        }
    }
}
