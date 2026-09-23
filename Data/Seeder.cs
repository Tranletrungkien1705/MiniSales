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

                CREATE TABLE IF NOT EXISTS TransportPlans (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    OrgId TEXT NOT NULL,
                    PlanNo TEXT NOT NULL,
                    VINPlan TEXT NOT NULL,
                    VIN TEXT,
                    FlagRealVin INTEGER NOT NULL DEFAULT 0,
                    CarId TEXT,
                    RefNo TEXT,
                    LCTemp TEXT,
                    ModelCode TEXT NOT NULL,
                    ModelName TEXT NOT NULL,
                    SpecCode TEXT,
                    ColorCode TEXT,
                    ColorName TEXT,
                    StorageCode TEXT NOT NULL,
                    StorageName TEXT,
                    DealerCode TEXT NOT NULL,
                    DealerName TEXT,
                    CQStartDate TEXT,
                    ExpectedDate TEXT NOT NULL,
                    ActualDate TEXT,
                    TransporterCode TEXT,
                    TransporterName TEXT,
                    TPType INTEGER NOT NULL DEFAULT 0,
                    FProvinceCode TEXT,
                    FProvinceName TEXT,
                    FDistrictCode TEXT,
                    FDistrictName TEXT,
                    TProvinceCode TEXT,
                    TProvinceName TEXT,
                    TDistrictCode TEXT,
                    TDistrictName TEXT,
                    Status INTEGER NOT NULL DEFAULT 0,
                    TransporterStatus INTEGER NOT NULL DEFAULT 0,
                    Remark TEXT,
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
                CREATE UNIQUE INDEX IF NOT EXISTS IX_TransportPlans_OrgId_PlanNo ON TransportPlans(OrgId, PlanNo);
                CREATE INDEX IF NOT EXISTS IX_TransportPlans_OrgId_VINPlan ON TransportPlans(OrgId, VINPlan);
                CREATE INDEX IF NOT EXISTS IX_TransportPlans_OrgId_Status ON TransportPlans(OrgId, Status);

                CREATE TABLE IF NOT EXISTS SaleAwardMinutes (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    OrgId TEXT NOT NULL,
                    SaleAwardMinutesNo TEXT NOT NULL,
                    DealerCode TEXT NOT NULL,
                    DealerName TEXT NOT NULL,
                    SaleAwardMinutesStatus INTEGER NOT NULL DEFAULT 0,
                    TotalCars INTEGER NOT NULL DEFAULT 0,
                    TotalAwardAmount REAL NOT NULL DEFAULT 0,
                    FilePath TEXT,
                    FileName TEXT,
                    FileUrl TEXT,
                    Remark TEXT,
                    RejectReason TEXT,
                    RejectDTime TEXT,
                    RejectBy TEXT,
                    CancelReason TEXT,
                    CancelDTime TEXT,
                    CancelBy TEXT,
                    CreateDTime TEXT NOT NULL,
                    CreateBy TEXT NOT NULL,
                    Appr1DTime TEXT,
                    Appr1By TEXT,
                    Appr2DTime TEXT,
                    Appr2By TEXT,
                    FinishDTime TEXT,
                    FinishBy TEXT,
                    LogLUDateTime TEXT,
                    LogLUBy TEXT
                );
                CREATE UNIQUE INDEX IF NOT EXISTS IX_SaleAwardMinutes_OrgId_SaleAwardMinutesNo ON SaleAwardMinutes(OrgId, SaleAwardMinutesNo);
                CREATE INDEX IF NOT EXISTS IX_SaleAwardMinutes_OrgId_DealerCode ON SaleAwardMinutes(OrgId, DealerCode);
                CREATE INDEX IF NOT EXISTS IX_SaleAwardMinutes_OrgId_Status ON SaleAwardMinutes(OrgId, SaleAwardMinutesStatus);

                CREATE TABLE IF NOT EXISTS SaleAwardMinutesDetails (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    SaleAwardMinutesId INTEGER NOT NULL,
                    SaleAwardMinutesNo TEXT NOT NULL,
                    Vin TEXT NOT NULL,
                    CarId TEXT,
                    DealerCode TEXT NOT NULL,
                    DealerName TEXT,
                    DocumentNo TEXT NOT NULL,
                    AmountAward REAL NOT NULL,
                    ModelCode TEXT,
                    ModelName TEXT,
                    SpecCode TEXT,
                    SpecDescription TEXT,
                    ColorCode TEXT,
                    ColorName TEXT,
                    VinYear INTEGER,
                    DeliveryDate TEXT,
                    DealNo TEXT,
                    Remark TEXT,
                    LogLUDateTime TEXT,
                    LogLUBy TEXT,
                    FOREIGN KEY(SaleAwardMinutesId) REFERENCES SaleAwardMinutes(Id) ON DELETE CASCADE
                );
                CREATE INDEX IF NOT EXISTS IX_SaleAwardMinutesDetails_Vin ON SaleAwardMinutesDetails(Vin);
                CREATE INDEX IF NOT EXISTS IX_SaleAwardMinutesDetails_DocumentNo ON SaleAwardMinutesDetails(DocumentNo);

                CREATE TABLE IF NOT EXISTS BusinessPlans (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    OrgId TEXT NOT NULL,
                    BusinessPlanCode TEXT NOT NULL,
                    DealerCode TEXT NOT NULL,
                    DealerName TEXT NOT NULL,
                    YearPlan INTEGER NOT NULL DEFAULT 2026,
                    Version TEXT NOT NULL DEFAULT 'INIT',
                    TimesPlan INTEGER NOT NULL DEFAULT 1,
                    Status INTEGER NOT NULL DEFAULT 0,
                    HTCStaffInCharge TEXT,
                    TotalRtlTarget INTEGER NOT NULL DEFAULT 0,
                    TotalOrdTarget INTEGER NOT NULL DEFAULT 0,
                    TotalBOInitial INTEGER NOT NULL DEFAULT 0,
                    Remark TEXT,
                    RejectReason TEXT,
                    CancelReason TEXT,
                    CancelledBy TEXT,
                    CancelledAt TEXT,
                    CreatedAt TEXT NOT NULL,
                    CreatedBy TEXT NOT NULL,
                    Approve1At TEXT,
                    Approve1By TEXT,
                    Approve2At TEXT,
                    Approve2By TEXT,
                    LogLUDateTime TEXT,
                    LogLUBy TEXT
                );
                CREATE UNIQUE INDEX IF NOT EXISTS IX_BusinessPlans_OrgId_BusinessPlanCode ON BusinessPlans(OrgId, BusinessPlanCode);
                CREATE INDEX IF NOT EXISTS IX_BusinessPlans_OrgId_Dealer_Year ON BusinessPlans(OrgId, DealerCode, YearPlan);
                CREATE INDEX IF NOT EXISTS IX_BusinessPlans_OrgId_Status ON BusinessPlans(OrgId, Status);

                CREATE TABLE IF NOT EXISTS BusinessPlanDetails (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    BusinessPlanId INTEGER NOT NULL,
                    BusinessPlanCode TEXT NOT NULL,
                    ModelCode TEXT NOT NULL,
                    ModelName TEXT NOT NULL,
                    Rtl_TotalQtyDeal INTEGER NOT NULL DEFAULT 0,
                    Rtl_QtyM1 INTEGER NOT NULL DEFAULT 0,
                    Rtl_QtyM2 INTEGER NOT NULL DEFAULT 0,
                    Rtl_QtyM3 INTEGER NOT NULL DEFAULT 0,
                    Rtl_QtyM4 INTEGER NOT NULL DEFAULT 0,
                    Rtl_QtyM5 INTEGER NOT NULL DEFAULT 0,
                    Rtl_QtyM6 INTEGER NOT NULL DEFAULT 0,
                    Rtl_QtyM7 INTEGER NOT NULL DEFAULT 0,
                    Rtl_QtyM8 INTEGER NOT NULL DEFAULT 0,
                    Rtl_QtyM9 INTEGER NOT NULL DEFAULT 0,
                    Rtl_QtyM10 INTEGER NOT NULL DEFAULT 0,
                    Rtl_QtyM11 INTEGER NOT NULL DEFAULT 0,
                    Rtl_QtyM12 INTEGER NOT NULL DEFAULT 0,
                    Rtl_QtyPre INTEGER NOT NULL DEFAULT 0,
                    Rtl_GrowthRate REAL NOT NULL DEFAULT 0,
                    Ord_TotalQtyOrder INTEGER NOT NULL DEFAULT 0,
                    Ord_QtyM1 INTEGER NOT NULL DEFAULT 0,
                    Ord_QtyM2 INTEGER NOT NULL DEFAULT 0,
                    Ord_QtyM3 INTEGER NOT NULL DEFAULT 0,
                    Ord_QtyM4 INTEGER NOT NULL DEFAULT 0,
                    Ord_QtyM5 INTEGER NOT NULL DEFAULT 0,
                    Ord_QtyM6 INTEGER NOT NULL DEFAULT 0,
                    Ord_QtyM7 INTEGER NOT NULL DEFAULT 0,
                    Ord_QtyM8 INTEGER NOT NULL DEFAULT 0,
                    Ord_QtyM9 INTEGER NOT NULL DEFAULT 0,
                    Ord_QtyM10 INTEGER NOT NULL DEFAULT 0,
                    Ord_QtyM11 INTEGER NOT NULL DEFAULT 0,
                    Ord_QtyM12 INTEGER NOT NULL DEFAULT 0,
                    Ord_QtyPre INTEGER NOT NULL DEFAULT 0,
                    Ord_GrowthRate REAL NOT NULL DEFAULT 0,
                    BO_TotalQtyBO INTEGER NOT NULL DEFAULT 0,
                    Remark TEXT,
                    FOREIGN KEY(BusinessPlanId) REFERENCES BusinessPlans(Id) ON DELETE CASCADE
                );
                CREATE INDEX IF NOT EXISTS IX_BusinessPlanDetails_BusinessPlanId ON BusinessPlanDetails(BusinessPlanId);
                CREATE INDEX IF NOT EXISTS IX_BusinessPlanDetails_ModelCode ON BusinessPlanDetails(ModelCode);

                CREATE TABLE IF NOT EXISTS FnExpCalcSheets (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    OrgId TEXT NOT NULL,
                    CaNo TEXT NOT NULL,
                    DealerCode TEXT NOT NULL,
                    DealerName TEXT NOT NULL,
                    TermFrom TEXT NOT NULL,
                    TermTo TEXT NOT NULL,
                    TermPrevFrom TEXT NOT NULL,
                    TermPrevTo TEXT NOT NULL,
                    FnExpPercent REAL NOT NULL DEFAULT 5.5,
                    PmtDsTCGPercent REAL NOT NULL DEFAULT 2.0,
                    CAName TEXT,
                    FlagEarlyCancel TEXT,
                    FlagisHTC TEXT NOT NULL DEFAULT 'HTC',
                    DlrSignStatus INTEGER NOT NULL DEFAULT 0,
                    HTCSignStatus INTEGER NOT NULL DEFAULT 0,
                    Status INTEGER NOT NULL DEFAULT 0,
                    TotalCars INTEGER NOT NULL DEFAULT 0,
                    TotalFnDepositAmount REAL NOT NULL DEFAULT 0,
                    TotalFnGrtAmount REAL NOT NULL DEFAULT 0,
                    TotalFnAmount REAL NOT NULL DEFAULT 0,
                    TotalPDAmount REAL NOT NULL DEFAULT 0,
                    NetSettlementAmount REAL NOT NULL DEFAULT 0,
                    FilePathFnExp TEXT,
                    FilePathPmtDC TEXT,
                    DlrAppr1At TEXT,
                    DlrAppr1By TEXT,
                    HTCAppr1At TEXT,
                    HTCAppr1By TEXT,
                    DlrAppr2At TEXT,
                    DlrAppr2By TEXT,
                    HTCAppr2At TEXT,
                    HTCAppr2By TEXT,
                    CancelReason TEXT,
                    CancelledBy TEXT,
                    CancelledAt TEXT,
                    Remark TEXT,
                    CreatedAt TEXT NOT NULL,
                    CreatedBy TEXT NOT NULL,
                    LogLUDateTime TEXT,
                    LogLUBy TEXT
                );
                CREATE UNIQUE INDEX IF NOT EXISTS IX_FnExpCalcSheets_OrgId_CaNo ON FnExpCalcSheets(OrgId, CaNo);
                CREATE INDEX IF NOT EXISTS IX_FnExpCalcSheets_OrgId_Dealer_Term ON FnExpCalcSheets(OrgId, DealerCode, TermFrom, TermTo);

                CREATE TABLE IF NOT EXISTS FnExpCalcSheetDetails (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    SheetId INTEGER NOT NULL,
                    CaNo TEXT NOT NULL,
                    CarId TEXT NOT NULL,
                    Vin TEXT NOT NULL,
                    ModelCode TEXT NOT NULL,
                    ModelName TEXT NOT NULL,
                    UnitPriceActual REAL NOT NULL DEFAULT 0,
                    CarCancelDate TEXT,
                    SodApprovedDate TEXT,
                    SodDepositDutyEndDate TEXT,
                    DateStart TEXT,
                    DateEnd TEXT,
                    TotalCompletedDate TEXT,
                    TermActual REAL NOT NULL DEFAULT 60,
                    FnDepositCountDate REAL NOT NULL DEFAULT 0,
                    FnDepositAmount REAL NOT NULL DEFAULT 0,
                    FnGrtCountDate REAL NOT NULL DEFAULT 0,
                    FnGrtAmount REAL NOT NULL DEFAULT 0,
                    FnTotalAmount REAL NOT NULL DEFAULT 0,
                    PDCountDate REAL NOT NULL DEFAULT 0,
                    PDAmount REAL NOT NULL DEFAULT 0,
                    Status INTEGER NOT NULL DEFAULT 0,
                    Remark TEXT,
                    LogLUDateTime TEXT,
                    LogLUBy TEXT,
                    FOREIGN KEY(SheetId) REFERENCES FnExpCalcSheets(Id) ON DELETE CASCADE
                );
                CREATE INDEX IF NOT EXISTS IX_FnExpCalcSheetDetails_SheetId ON FnExpCalcSheetDetails(SheetId);
                CREATE INDEX IF NOT EXISTS IX_FnExpCalcSheetDetails_Vin ON FnExpCalcSheetDetails(Vin);

                CREATE TABLE IF NOT EXISTS SalesPolicies (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    OrgId TEXT NOT NULL,
                    SPSRCode TEXT NOT NULL,
                    SPNo TEXT NOT NULL,
                    StartDate TEXT NOT NULL,
                    EndDate TEXT NOT NULL,
                    SPSRType INTEGER NOT NULL DEFAULT 1,
                    FormBusinessSupportCode INTEGER NOT NULL DEFAULT 0,
                    SPSRRoot TEXT,
                    FilePath TEXT,
                    FileName TEXT,
                    FlagMstValid TEXT NOT NULL DEFAULT '1',
                    Status INTEGER NOT NULL DEFAULT 1,
                    Remark TEXT,
                    ApprovedAt TEXT,
                    ApprovedBy TEXT,
                    CreatedAt TEXT NOT NULL,
                    CreatedBy TEXT,
                    LUDateTime TEXT,
                    LUBy TEXT
                );
                CREATE UNIQUE INDEX IF NOT EXISTS IX_SalesPolicies_OrgId_SPSRCode ON SalesPolicies(OrgId, SPSRCode);
                CREATE INDEX IF NOT EXISTS IX_SalesPolicies_OrgId_SPNo ON SalesPolicies(OrgId, SPNo);

                CREATE TABLE IF NOT EXISTS SalesPolicyDetails (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    SalesPolicyId INTEGER NOT NULL,
                    SPSRCode TEXT NOT NULL,
                    DealerCode TEXT NOT NULL,
                    ModelCode TEXT NOT NULL,
                    ModelName TEXT,
                    SpecCode TEXT,
                    SpecDescription TEXT,
                    YearOfManufacture TEXT,
                    AmountSupport REAL NOT NULL DEFAULT 0,
                    SupportPercent REAL NOT NULL DEFAULT 0,
                    MinQty INTEGER NOT NULL DEFAULT 1,
                    Remark TEXT,
                    FOREIGN KEY(SalesPolicyId) REFERENCES SalesPolicies(Id) ON DELETE CASCADE
                );
                CREATE INDEX IF NOT EXISTS IX_SalesPolicyDetails_SalesPolicyId ON SalesPolicyDetails(SalesPolicyId);
                CREATE INDEX IF NOT EXISTS IX_SalesPolicyDetails_SPSRCode ON SalesPolicyDetails(SPSRCode);
                CREATE INDEX IF NOT EXISTS IX_SalesPolicyDetails_DealerCode ON SalesPolicyDetails(DealerCode);
                CREATE INDEX IF NOT EXISTS IX_SalesPolicyDetails_ModelCode ON SalesPolicyDetails(ModelCode);

                CREATE TABLE IF NOT EXISTS SupportRetails (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    OrgId TEXT NOT NULL,
                    SupportCode TEXT NOT NULL,
                    SPSRCode TEXT NOT NULL,
                    SPNo TEXT NOT NULL,
                    DealerCode TEXT NOT NULL,
                    DealerName TEXT,
                    Vin TEXT NOT NULL,
                    CarId TEXT,
                    ModelCode TEXT,
                    ModelName TEXT,
                    AmountSupport REAL NOT NULL DEFAULT 0,
                    AmountHTCAppr REAL NOT NULL DEFAULT 0,
                    DateSupport TEXT NOT NULL,
                    HTCDatePayment TEXT,
                    Status INTEGER NOT NULL DEFAULT 0,
                    Remark TEXT,
                    OSODApprovedDate TEXT,
                    OSODSOCode TEXT,
                    HTCInvoiceNo TEXT,
                    HTCInvoiceDate TEXT,
                    CDODDeliveryOutDate TEXT,
                    DateFullStatus TEXT,
                    PRDiscountNo TEXT,
                    ApprovedAt TEXT,
                    ApprovedBy TEXT,
                    RejectedAt TEXT,
                    RejectedBy TEXT,
                    RejectReason TEXT,
                    CancelledAt TEXT,
                    CancelledBy TEXT,
                    CancelReason TEXT,
                    CreatedAt TEXT NOT NULL,
                    CreatedBy TEXT
                );
                CREATE UNIQUE INDEX IF NOT EXISTS IX_SupportRetails_OrgId_SupportCode ON SupportRetails(OrgId, SupportCode);
                CREATE INDEX IF NOT EXISTS IX_SupportRetails_OrgId_SPSRCode_Vin ON SupportRetails(OrgId, SPSRCode, Vin);
                CREATE INDEX IF NOT EXISTS IX_SupportRetails_OrgId_DealerCode ON SupportRetails(OrgId, DealerCode);
                CREATE INDEX IF NOT EXISTS IX_SupportRetails_OrgId_Vin ON SupportRetails(OrgId, Vin);

                CREATE TABLE IF NOT EXISTS PlanEstimateOrders (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    OrgId TEXT NOT NULL,
                    PLEOrdNo TEXT NOT NULL,
                    DealerCode TEXT NOT NULL,
                    DealerName TEXT NOT NULL,
                    MonthEstimate TEXT NOT NULL,
                    BusinessPlanCode TEXT,
                    AreaRootCode TEXT,
                    Status INTEGER NOT NULL DEFAULT 0,
                    TotalQtyN1 INTEGER NOT NULL DEFAULT 0,
                    TotalSellCusN0 INTEGER NOT NULL DEFAULT 0,
                    TotalSellCusN1 INTEGER NOT NULL DEFAULT 0,
                    TotalSellCusN2 INTEGER NOT NULL DEFAULT 0,
                    TotalSellCusN3 INTEGER NOT NULL DEFAULT 0,
                    Remark TEXT,
                    CancelReason TEXT,
                    CreatedBy TEXT,
                    CreatedAt TEXT NOT NULL,
                    Approve1By TEXT,
                    Approve1At TEXT,
                    Approve2By TEXT,
                    Approve2At TEXT,
                    CancelledBy TEXT,
                    CancelledAt TEXT,
                    LogLUDateTime TEXT,
                    LogLUBy TEXT
                );
                CREATE UNIQUE INDEX IF NOT EXISTS IX_PlanEstimateOrders_OrgId_PLEOrdNo ON PlanEstimateOrders(OrgId, PLEOrdNo);
                CREATE INDEX IF NOT EXISTS IX_PlanEstimateOrders_OrgId_Dealer_Month ON PlanEstimateOrders(OrgId, DealerCode, MonthEstimate);
                CREATE INDEX IF NOT EXISTS IX_PlanEstimateOrders_OrgId_Status ON PlanEstimateOrders(OrgId, Status);

                CREATE TABLE IF NOT EXISTS PlanEstimateOrderDetails (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    PlanEstimateOrderId INTEGER NOT NULL,
                    PLEOrdNo TEXT NOT NULL,
                    ModelCode TEXT NOT NULL,
                    ModelName TEXT NOT NULL,
                    SpecCode TEXT,
                    SpecDescription TEXT,
                    QtySellCustomer INTEGER NOT NULL DEFAULT 0,
                    QtySellDealer INTEGER NOT NULL DEFAULT 0,
                    QtyInStock INTEGER NOT NULL DEFAULT 0,
                    QtyOnWay INTEGER NOT NULL DEFAULT 0,
                    QtyBuyDealer INTEGER NOT NULL DEFAULT 0,
                    QtyUnknown INTEGER NOT NULL DEFAULT 0,
                    QtyBOChuaXuatKho INTEGER NOT NULL DEFAULT 0,
                    QtyBODangMapVIN INTEGER NOT NULL DEFAULT 0,
                    QtyBOKhongVINQuaKhu INTEGER NOT NULL DEFAULT 0,
                    QtyBOKhongVINHienTai INTEGER NOT NULL DEFAULT 0,
                    QtyBOKhongVINTuongLai INTEGER NOT NULL DEFAULT 0,
                    QtyOrdCarID INTEGER NOT NULL DEFAULT 0,
                    QtyOrdNotCarID INTEGER NOT NULL DEFAULT 0,
                    QtyEOrdN1 INTEGER NOT NULL DEFAULT 0,
                    TiLeDatTN1 REAL NOT NULL DEFAULT 0,
                    QtyESellCusN0 INTEGER NOT NULL DEFAULT 0,
                    QtyESellCusN1 INTEGER NOT NULL DEFAULT 0,
                    QtyESellCusN2 INTEGER NOT NULL DEFAULT 0,
                    QtyESellCusN3 INTEGER NOT NULL DEFAULT 0,
                    TongHangCo INTEGER NOT NULL DEFAULT 0,
                    TiLeHangCo REAL NOT NULL DEFAULT 0,
                    DuPhongTonKho INTEGER NOT NULL DEFAULT 0,
                    TiLeDuPhongTonKho REAL NOT NULL DEFAULT 0,
                    Remark TEXT,
                    FOREIGN KEY(PlanEstimateOrderId) REFERENCES PlanEstimateOrders(Id) ON DELETE CASCADE
                );
                CREATE INDEX IF NOT EXISTS IX_PlanEstimateOrderDetails_PLEOrdNo ON PlanEstimateOrderDetails(PLEOrdNo);
                CREATE INDEX IF NOT EXISTS IX_PlanEstimateOrderDetails_ModelCode ON PlanEstimateOrderDetails(ModelCode);

                CREATE TABLE IF NOT EXISTS PackingLists (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    OrgId TEXT NOT NULL,
                    PackingListNo TEXT NOT NULL,
                    ContractNo TEXT NOT NULL,
                    LCNo TEXT,
                    PortCode TEXT NOT NULL,
                    PortName TEXT,
                    PLType INTEGER NOT NULL DEFAULT 0,
                    Status INTEGER NOT NULL DEFAULT 0,
                    ShippingDateStart TEXT,
                    ShippingDateEndExpected TEXT,
                    ShippingDateEnd TEXT,
                    VesselName TEXT,
                    VoyageNo TEXT,
                    TotalCars INTEGER NOT NULL DEFAULT 0,
                    TotalRepaired INTEGER NOT NULL DEFAULT 0,
                    Remark TEXT,
                    CancelReason TEXT,
                    CancelledBy TEXT,
                    CancelledAt TEXT,
                    CreatedBy TEXT,
                    CreatedAt TEXT NOT NULL,
                    LUDateTime TEXT,
                    LUBy TEXT
                );
                CREATE UNIQUE INDEX IF NOT EXISTS IX_PackingLists_OrgId_PackingListNo ON PackingLists(OrgId, PackingListNo);
                CREATE INDEX IF NOT EXISTS IX_PackingLists_OrgId_ContractNo ON PackingLists(OrgId, ContractNo);
                CREATE INDEX IF NOT EXISTS IX_PackingLists_OrgId_LCNo ON PackingLists(OrgId, LCNo);
                CREATE INDEX IF NOT EXISTS IX_PackingLists_OrgId_PortCode ON PackingLists(OrgId, PortCode);

                CREATE TABLE IF NOT EXISTS PackingListDetails (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    PackingListId INTEGER NOT NULL,
                    PackingListNo TEXT NOT NULL,
                    Vin TEXT NOT NULL,
                    EngineNo TEXT,
                    KeyNo TEXT,
                    ModelCode TEXT NOT NULL,
                    ModelName TEXT NOT NULL,
                    SpecCode TEXT,
                    SpecDescription TEXT,
                    ColorCode TEXT,
                    ColorName TEXT,
                    WorkOrderNo TEXT,
                    ProductionMonth TEXT,
                    FlagRepair INTEGER NOT NULL DEFAULT 0,
                    RepairRemark TEXT,
                    StorageCodeCurrent TEXT,
                    StoreDate TEXT,
                    Status INTEGER NOT NULL DEFAULT 0,
                    Remark TEXT,
                    FOREIGN KEY(PackingListId) REFERENCES PackingLists(Id) ON DELETE CASCADE
                );
                CREATE INDEX IF NOT EXISTS IX_PackingListDetails_PackingListNo ON PackingListDetails(PackingListNo);
                CREATE INDEX IF NOT EXISTS IX_PackingListDetails_Vin ON PackingListDetails(Vin);
                CREATE INDEX IF NOT EXISTS IX_PackingListDetails_ModelCode ON PackingListDetails(ModelCode);

                CREATE TABLE IF NOT EXISTS BankingTransactions (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    OrgId TEXT NOT NULL,
                    RQ_BankingTransNo TEXT NOT NULL,
                    DealerCode TEXT NOT NULL,
                    DealerName TEXT NOT NULL,
                    BankCode TEXT NOT NULL,
                    BankName TEXT NOT NULL,
                    TaxCode TEXT,
                    TransType INTEGER NOT NULL DEFAULT 0,
                    Status INTEGER NOT NULL DEFAULT 0,
                    BankStatus INTEGER NOT NULL DEFAULT 0,
                    RefBankCode TEXT,
                    BankRemark TEXT,
                    Remark TEXT,
                    TotalAmount REAL NOT NULL DEFAULT 0,
                    TotalCars INTEGER NOT NULL DEFAULT 0,
                    PaymentNo TEXT,
                    PaymentType TEXT,
                    DisbursementType INTEGER NOT NULL DEFAULT 0,
                    TransferAmount REAL NOT NULL DEFAULT 0,
                    LoanPeriod INTEGER,
                    LoanPeriodDate TEXT,
                    InterestRate REAL,
                    ReceivingUnit TEXT,
                    BankAccountReceive TEXT,
                    BankNameReceive TEXT,
                    CreditContractNo TEXT,
                    DisbursementRequestDate TEXT,
                    LDNo TEXT,
                    DisbursementAmount REAL NOT NULL DEFAULT 0,
                    DisbursementDate TEXT,
                    GuaranteeType TEXT,
                    DateExpiredValue INTEGER,
                    GrtForm TEXT,
                    GrtReceive TEXT,
                    MDNo TEXT,
                    GrtAmount REAL NOT NULL DEFAULT 0,
                    GrtDateStart TEXT,
                    GrtDateEnd TEXT,
                    CreatedAt TEXT NOT NULL,
                    CreatedBy TEXT,
                    PushedToBankAt TEXT,
                    PushedToBankBy TEXT,
                    BankApprovedAt TEXT,
                    BankApprovedBy TEXT,
                    DisbursedAt TEXT,
                    DisbursedBy TEXT,
                    RejectedAt TEXT,
                    RejectedBy TEXT,
                    RejectReason TEXT,
                    CancelledAt TEXT,
                    CancelledBy TEXT,
                    CancelReason TEXT,
                    LUDateTime TEXT,
                    LUBy TEXT
                );
                CREATE UNIQUE INDEX IF NOT EXISTS IX_BankingTransactions_OrgId_RQ_BankingTransNo ON BankingTransactions(OrgId, RQ_BankingTransNo);
                CREATE INDEX IF NOT EXISTS IX_BankingTransactions_OrgId_DealerCode ON BankingTransactions(OrgId, DealerCode);
                CREATE INDEX IF NOT EXISTS IX_BankingTransactions_OrgId_BankCode ON BankingTransactions(OrgId, BankCode);

                CREATE TABLE IF NOT EXISTS BankingTransactionDetails (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    BankingTransactionId INTEGER NOT NULL,
                    RQ_BankingTransNo TEXT NOT NULL,
                    CarId TEXT NOT NULL,
                    Vin TEXT NOT NULL,
                    ModelCode TEXT NOT NULL,
                    ModelName TEXT NOT NULL,
                    SpecCode TEXT,
                    SpecDescription TEXT,
                    ColorCode TEXT,
                    DlrCtrNo TEXT,
                    SOCode TEXT,
                    HTCInvoiceNo TEXT,
                    AmountActual REAL NOT NULL DEFAULT 0,
                    AllocPercent REAL NOT NULL DEFAULT 100,
                    AllocAmount REAL NOT NULL DEFAULT 0,
                    Remark TEXT,
                    FOREIGN KEY(BankingTransactionId) REFERENCES BankingTransactions(Id) ON DELETE CASCADE
                );
                CREATE INDEX IF NOT EXISTS IX_BankingTransactionDetails_RQ_BankingTransNo ON BankingTransactionDetails(RQ_BankingTransNo);
                CREATE INDEX IF NOT EXISTS IX_BankingTransactionDetails_CarId ON BankingTransactionDetails(CarId);
                CREATE INDEX IF NOT EXISTS IX_BankingTransactionDetails_Vin ON BankingTransactionDetails(Vin);

                CREATE TABLE IF NOT EXISTS BankingTransactionAttachFiles (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    BankingTransactionId INTEGER NOT NULL,
                    RQ_BankingTransNo TEXT NOT NULL,
                    FileIndex INTEGER NOT NULL DEFAULT 1,
                    FileType INTEGER NOT NULL DEFAULT 0,
                    FileName TEXT NOT NULL,
                    FilePath TEXT NOT NULL,
                    FileUrl TEXT,
                    FileSize INTEGER,
                    SerialNumber TEXT,
                    CaSubject TEXT,
                    FlagSigned INTEGER NOT NULL DEFAULT 0,
                    SignedAt TEXT,
                    SignedBy TEXT,
                    Remark TEXT,
                    UploadedAt TEXT NOT NULL,
                    UploadedBy TEXT,
                    FOREIGN KEY(BankingTransactionId) REFERENCES BankingTransactions(Id) ON DELETE CASCADE
                );
                CREATE INDEX IF NOT EXISTS IX_BankingTransactionAttachFiles_RQ_BankingTransNo ON BankingTransactionAttachFiles(RQ_BankingTransNo);

                CREATE TABLE IF NOT EXISTS CustomsDeclarations (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    OrgId TEXT NOT NULL,
                    DeclarationNo TEXT NOT NULL,
                    PortCode TEXT NOT NULL,
                    PortName TEXT,
                    ContractNo TEXT,
                    OpenDate TEXT NOT NULL,
                    TaxPaymentDate TEXT,
                    TaxReceiptNo TEXT,
                    TaxPayerBank TEXT,
                    CustomsOffice TEXT,
                    Channel INTEGER NOT NULL DEFAULT 1,
                    Status INTEGER NOT NULL DEFAULT 0,
                    TotalCars INTEGER NOT NULL DEFAULT 0,
                    DutiableAmount REAL NOT NULL DEFAULT 0,
                    ImportTaxAmount REAL NOT NULL DEFAULT 0,
                    SpecialConsumptionTaxAmount REAL NOT NULL DEFAULT 0,
                    VatAmount REAL NOT NULL DEFAULT 0,
                    TotalTaxAmount REAL NOT NULL DEFAULT 0,
                    CustomsClearanceDate TEXT,
                    CustomsOfficer TEXT,
                    Remark TEXT,
                    CreatedBy TEXT,
                    CreatedAt TEXT NOT NULL,
                    SubmittedBy TEXT,
                    SubmittedAt TEXT,
                    TaxPaidBy TEXT,
                    ClearedBy TEXT,
                    CancelledBy TEXT,
                    CancelledAt TEXT,
                    CancelReason TEXT,
                    LUDateTime TEXT,
                    LUBy TEXT
                );
                CREATE UNIQUE INDEX IF NOT EXISTS IX_CustomsDeclarations_OrgId_DeclarationNo ON CustomsDeclarations(OrgId, DeclarationNo);
                CREATE INDEX IF NOT EXISTS IX_CustomsDeclarations_OrgId_PortCode ON CustomsDeclarations(OrgId, PortCode);
                CREATE INDEX IF NOT EXISTS IX_CustomsDeclarations_OrgId_ContractNo ON CustomsDeclarations(OrgId, ContractNo);

                CREATE TABLE IF NOT EXISTS CustomsDeclarationDetails (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    DeclarationId INTEGER NOT NULL,
                    DeclarationNo TEXT NOT NULL,
                    Vin TEXT NOT NULL,
                    EngineNo TEXT,
                    ModelCode TEXT NOT NULL,
                    ModelName TEXT NOT NULL,
                    SpecCode TEXT,
                    SpecDescription TEXT,
                    ColorCode TEXT,
                    ColorName TEXT,
                    PackingListNo TEXT,
                    LCNo TEXT,
                    ContractNo TEXT,
                    DutiableValue REAL NOT NULL DEFAULT 0,
                    ImportTax REAL NOT NULL DEFAULT 0,
                    SpecialConsumptionTax REAL NOT NULL DEFAULT 0,
                    Vat REAL NOT NULL DEFAULT 0,
                    TotalTax REAL NOT NULL DEFAULT 0,
                    Status INTEGER NOT NULL DEFAULT 0,
                    TaxPaymentDate TEXT,
                    ClearedAt TEXT,
                    Remark TEXT,
                    FOREIGN KEY(DeclarationId) REFERENCES CustomsDeclarations(Id) ON DELETE CASCADE
                );
                CREATE INDEX IF NOT EXISTS IX_CustomsDeclarationDetails_DeclarationNo ON CustomsDeclarationDetails(DeclarationNo);
                CREATE INDEX IF NOT EXISTS IX_CustomsDeclarationDetails_Vin ON CustomsDeclarationDetails(Vin);
                CREATE INDEX IF NOT EXISTS IX_CustomsDeclarationDetails_ModelCode ON CustomsDeclarationDetails(ModelCode);

                CREATE TABLE IF NOT EXISTS GpsUnitPrices (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    PriceCode TEXT NOT NULL,
                    PriceName TEXT NOT NULL,
                    DailyRate REAL NOT NULL DEFAULT 1500,
                    MonthlyRate REAL NOT NULL DEFAULT 45000,
                    EffStartDate TEXT NOT NULL,
                    EffEndDate TEXT,
                    IsActive INTEGER NOT NULL DEFAULT 1,
                    Remark TEXT
                );
                CREATE UNIQUE INDEX IF NOT EXISTS IX_GpsUnitPrices_PriceCode ON GpsUnitPrices(PriceCode);

                CREATE TABLE IF NOT EXISTS PaymentGPSOrders (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    OrgId TEXT NOT NULL,
                    PaymentGPSNo TEXT NOT NULL,
                    PmtMonth TEXT NOT NULL,
                    FromDate TEXT NOT NULL,
                    ToDate TEXT NOT NULL,
                    TotalCars INTEGER NOT NULL DEFAULT 0,
                    AmountTotal REAL NOT NULL DEFAULT 0,
                    VatRate REAL NOT NULL DEFAULT 10,
                    UnitPriceVAT REAL NOT NULL DEFAULT 0,
                    TotalAmountVAT REAL NOT NULL DEFAULT 0,
                    Status INTEGER NOT NULL DEFAULT 0,
                    HTVSignStatus INTEGER NOT NULL DEFAULT 0,
                    HTVSignDate TEXT,
                    HTVSignBy TEXT,
                    TCMSSignStatus INTEGER NOT NULL DEFAULT 0,
                    TCMSSignDate TEXT,
                    TCMSSignBy TEXT,
                    FilePath TEXT,
                    Remark TEXT,
                    RejectReason TEXT,
                    CancelReason TEXT,
                    CreatedAt TEXT NOT NULL,
                    CreatedBy TEXT,
                    Approved1At TEXT,
                    Approved1By TEXT,
                    Approved2At TEXT,
                    Approved2By TEXT,
                    RejectedAt TEXT,
                    RejectedBy TEXT,
                    CancelledAt TEXT,
                    CancelledBy TEXT,
                    LUDateTime TEXT,
                    LUBy TEXT
                );
                CREATE UNIQUE INDEX IF NOT EXISTS IX_PaymentGPSOrders_OrgId_PaymentGPSNo ON PaymentGPSOrders(OrgId, PaymentGPSNo);
                CREATE INDEX IF NOT EXISTS IX_PaymentGPSOrders_OrgId_PmtMonth ON PaymentGPSOrders(OrgId, PmtMonth);

                CREATE TABLE IF NOT EXISTS PaymentGPSDetails (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    PaymentGPSId INTEGER NOT NULL,
                    PaymentGPSNo TEXT NOT NULL,
                    Vin TEXT NOT NULL,
                    CarId TEXT,
                    ModelCode TEXT NOT NULL,
                    ModelName TEXT NOT NULL,
                    SpecCode TEXT,
                    ColorCode TEXT,
                    PlateNo TEXT,
                    DealerCode TEXT,
                    DealerName TEXT,
                    GPSDvNo TEXT NOT NULL,
                    GPSStartDate TEXT NOT NULL,
                    RetailDate TEXT,
                    CostGPSStartDate TEXT NOT NULL,
                    CostGPSEndDate TEXT NOT NULL,
                    PlanCostGPSDate INTEGER NOT NULL DEFAULT 0,
                    DeductDate INTEGER NOT NULL DEFAULT 0,
                    ActualCostGPSDate INTEGER NOT NULL DEFAULT 0,
                    PriceGPS REAL NOT NULL DEFAULT 1500,
                    AmountGPS REAL NOT NULL DEFAULT 0,
                    Remark TEXT,
                    FOREIGN KEY(PaymentGPSId) REFERENCES PaymentGPSOrders(Id) ON DELETE CASCADE
                );
                CREATE INDEX IF NOT EXISTS IX_PaymentGPSDetails_PaymentGPSNo ON PaymentGPSDetails(PaymentGPSNo);
                CREATE INDEX IF NOT EXISTS IX_PaymentGPSDetails_Vin ON PaymentGPSDetails(Vin);
                CREATE INDEX IF NOT EXISTS IX_PaymentGPSDetails_GPSDvNo ON PaymentGPSDetails(GPSDvNo);

                CREATE TABLE IF NOT EXISTS InventoryCosts (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    CostTypeCode TEXT NOT NULL,
                    CostTypeName TEXT NOT NULL,
                    StorageCode TEXT NOT NULL,
                    StorageName TEXT NOT NULL,
                    ModelCode TEXT NOT NULL,
                    ModelName TEXT NOT NULL,
                    UnitPrice REAL NOT NULL DEFAULT 0,
                    IsActive INTEGER NOT NULL DEFAULT 1,
                    Remark TEXT
                );
                CREATE UNIQUE INDEX IF NOT EXISTS IX_InventoryCosts_Storage_CostType_Model ON InventoryCosts(StorageCode, CostTypeCode, ModelCode);

                CREATE TABLE IF NOT EXISTS PaymentStorageOrders (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    OrgId TEXT NOT NULL,
                    PaymentStorageNo TEXT NOT NULL,
                    PmtMonth TEXT NOT NULL,
                    PmtPeriodStartDTime TEXT NOT NULL,
                    PmtPeriodEndDTime TEXT NOT NULL,
                    VAT REAL NOT NULL DEFAULT 10,
                    TotalCars INTEGER NOT NULL DEFAULT 0,
                    AmountBCP REAL NOT NULL DEFAULT 0,
                    AmountLK REAL NOT NULL DEFAULT 0,
                    AmountTotal REAL NOT NULL DEFAULT 0,
                    AmountVAT REAL NOT NULL DEFAULT 0,
                    AmountVATTotal REAL NOT NULL DEFAULT 0,
                    Status INTEGER NOT NULL DEFAULT 0,
                    HTVSignStatus INTEGER NOT NULL DEFAULT 0,
                    HTVSignDTime TEXT,
                    HTVSignBy TEXT,
                    TCMSSignStatus INTEGER NOT NULL DEFAULT 0,
                    TCMSSignDTime TEXT,
                    TCMSSignBy TEXT,
                    FilePath TEXT,
                    FileUrl TEXT,
                    FileInvPath TEXT,
                    FileInvUrl TEXT,
                    InvoiceNo TEXT,
                    InvoiceDate TEXT,
                    Remark TEXT,
                    RejectReason TEXT,
                    CancelReason TEXT,
                    CreatedAt TEXT NOT NULL,
                    CreatedBy TEXT,
                    Approve1At TEXT,
                    Approve1By TEXT,
                    Approve2At TEXT,
                    Approve2By TEXT,
                    RejectedAt TEXT,
                    RejectedBy TEXT,
                    CancelledAt TEXT,
                    CancelledBy TEXT,
                    LogLUDateTime TEXT,
                    LogLUBy TEXT
                );
                CREATE UNIQUE INDEX IF NOT EXISTS IX_PaymentStorageOrders_OrgId_PaymentStorageNo ON PaymentStorageOrders(OrgId, PaymentStorageNo);
                CREATE INDEX IF NOT EXISTS IX_PaymentStorageOrders_OrgId_PmtMonth ON PaymentStorageOrders(OrgId, PmtMonth);

                CREATE TABLE IF NOT EXISTS PaymentStorageDetails (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    PaymentStorageId INTEGER NOT NULL,
                    PaymentStorageNo TEXT NOT NULL,
                    Vin TEXT NOT NULL,
                    RefNo TEXT NOT NULL,
                    CarId TEXT,
                    ModelCode TEXT NOT NULL,
                    ModelName TEXT NOT NULL,
                    SpecCode TEXT,
                    ColorCode TEXT,
                    StorageCode TEXT NOT NULL,
                    StorageName TEXT,
                    StorageDateIn TEXT,
                    StorageDateOut TEXT,
                    DateBegin TEXT NOT NULL,
                    DateEnd TEXT NOT NULL,
                    DelayTransport INTEGER NOT NULL DEFAULT 0,
                    DateCount INTEGER NOT NULL DEFAULT 0,
                    PriceBCP REAL NOT NULL DEFAULT 0,
                    PriceLK REAL NOT NULL DEFAULT 0,
                    AmountBCP REAL NOT NULL DEFAULT 0,
                    AmountLK REAL NOT NULL DEFAULT 0,
                    AmountTotal REAL NOT NULL DEFAULT 0,
                    DealerCode TEXT,
                    DealerName TEXT,
                    PackingListNo TEXT,
                    DeliveryOrderNo TEXT,
                    StorageRearrangeNoIn TEXT,
                    StorageRearrangeNoOut TEXT,
                    RetrieveOrderNo TEXT,
                    InvoiceFactoryDate TEXT,
                    Remark TEXT,
                    FOREIGN KEY(PaymentStorageId) REFERENCES PaymentStorageOrders(Id) ON DELETE CASCADE
                );
                CREATE INDEX IF NOT EXISTS IX_PaymentStorageDetails_PaymentStorageNo ON PaymentStorageDetails(PaymentStorageNo);
                CREATE INDEX IF NOT EXISTS IX_PaymentStorageDetails_Vin ON PaymentStorageDetails(Vin);
                CREATE INDEX IF NOT EXISTS IX_PaymentStorageDetails_RefNo ON PaymentStorageDetails(RefNo);
                CREATE INDEX IF NOT EXISTS IX_PaymentStorageDetails_StorageCode ON PaymentStorageDetails(StorageCode);

                CREATE TABLE IF NOT EXISTS CarVinProfiles (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    OrgId TEXT NOT NULL,
                    Vin TEXT NOT NULL,
                    CarId TEXT,
                    DealerCode TEXT NOT NULL,
                    DealerName TEXT,
                    ModelCode TEXT NOT NULL,
                    ModelName TEXT NOT NULL,
                    SpecCode TEXT,
                    SpecDescription TEXT,
                    ColorCode TEXT,
                    ColorName TEXT,
                    EngineNo TEXT,
                    StorageCode TEXT,
                    StorageName TEXT,
                    CQNo TEXT,
                    CQStartDate TEXT,
                    CQEndDate TEXT,
                    FGFormNo TEXT,
                    QCNo TEXT,
                    IssuedDate TEXT,
                    CONo TEXT,
                    CODate TEXT,
                    CODateExpected TEXT,
                    InvoiceNoFactory TEXT,
                    InvoiceFactoryDate TEXT,
                    InvoiceFactorySearch TEXT,
                    InvoiceNoTransferred TEXT,
                    InvoiceTransferredDate TEXT,
                    InvoiceTransferredSearch TEXT,
                    InvoiceSpecName TEXT,
                    TypeCB INTEGER NOT NULL DEFAULT 0,
                    LoaiThung TEXT,
                    CabinCertificateNo TEXT,
                    CabinCertificateDate TEXT,
                    CabinCONo TEXT,
                    CabinInvoiceNo TEXT,
                    CabinInvoiceDate TEXT,
                    MortageStatus INTEGER NOT NULL DEFAULT 0,
                    MortageBankCode TEXT,
                    MortageBankName TEXT,
                    MortageStartDate TEXT,
                    HandOverBankCode TEXT,
                    HandOverBankName TEXT,
                    BillNo TEXT,
                    MortageEndDate TEXT,
                    RedeemDate TEXT,
                    LogDateTimeStatusMortageEnd TEXT,
                    GuaranteeNo TEXT,
                    BankGuaranteeNo TEXT,
                    DateStart TEXT,
                    DateExpired TEXT,
                    GuaranteeValue REAL,
                    FlagDtlDiscount INTEGER NOT NULL DEFAULT 0,
                    FlagGrtExt INTEGER NOT NULL DEFAULT 0,
                    DocumentsStatus TEXT NOT NULL DEFAULT '0',
                    FlagDocReq TEXT NOT NULL DEFAULT '0',
                    DocDeliveryReqDate TEXT,
                    SendMailDocDate TEXT,
                    Remark TEXT,
                    CreatedAt TEXT NOT NULL,
                    CreatedBy TEXT,
                    UpdatedAt TEXT,
                    UpdatedBy TEXT
                );
                CREATE UNIQUE INDEX IF NOT EXISTS IX_CarVinProfiles_OrgId_Vin ON CarVinProfiles(OrgId, Vin);
                CREATE INDEX IF NOT EXISTS IX_CarVinProfiles_OrgId_DealerCode ON CarVinProfiles(OrgId, DealerCode);
                CREATE INDEX IF NOT EXISTS IX_CarVinProfiles_OrgId_CarId ON CarVinProfiles(OrgId, CarId);
                CREATE INDEX IF NOT EXISTS IX_CarVinProfiles_OrgId_MortageStatus ON CarVinProfiles(OrgId, MortageStatus);
                CREATE INDEX IF NOT EXISTS IX_CarVinProfiles_OrgId_FlagDocReq ON CarVinProfiles(OrgId, FlagDocReq);

                CREATE TABLE IF NOT EXISTS PerformanceInvoices (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    OrgId TEXT NOT NULL,
                    RefNo TEXT NOT NULL,
                    OrderMonth TEXT NOT NULL,
                    ProductionMonth TEXT NOT NULL,
                    ExpectedMonth TEXT NOT NULL,
                    FlagAutoPL TEXT NOT NULL DEFAULT '1',
                    Status INTEGER NOT NULL DEFAULT 0,
                    TotalQuantity INTEGER NOT NULL DEFAULT 0,
                    TotalAmount REAL NOT NULL DEFAULT 0,
                    Remark TEXT,
                    CancelReason TEXT,
                    CreatedBy TEXT,
                    CreatedAt TEXT NOT NULL,
                    UpdatedBy TEXT,
                    UpdatedAt TEXT,
                    ConfirmedBy TEXT,
                    ConfirmedAt TEXT,
                    CancelledBy TEXT,
                    CancelledAt TEXT
                );
                CREATE UNIQUE INDEX IF NOT EXISTS IX_PerformanceInvoices_OrgId_RefNo ON PerformanceInvoices(OrgId, RefNo);
                CREATE INDEX IF NOT EXISTS IX_PerformanceInvoices_OrgId_ProductionMonth ON PerformanceInvoices(OrgId, ProductionMonth);
                CREATE INDEX IF NOT EXISTS IX_PerformanceInvoices_OrgId_OrderMonth ON PerformanceInvoices(OrgId, OrderMonth);
                CREATE INDEX IF NOT EXISTS IX_PerformanceInvoices_OrgId_Status ON PerformanceInvoices(OrgId, Status);

                CREATE TABLE IF NOT EXISTS PerformanceInvoiceDetails (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    PerformanceInvoiceId INTEGER NOT NULL,
                    RefNo TEXT NOT NULL,
                    LCTemp TEXT NOT NULL,
                    SpecCode TEXT NOT NULL,
                    ModelCode TEXT NOT NULL,
                    ModelName TEXT,
                    ColorCode TEXT NOT NULL,
                    ColorName TEXT,
                    WorkOrderNo TEXT NOT NULL,
                    PortCode TEXT NOT NULL,
                    PlantCode TEXT NOT NULL,
                    Quantity INTEGER NOT NULL DEFAULT 1,
                    UnitPrice REAL NOT NULL DEFAULT 0,
                    TotalAmount REAL NOT NULL DEFAULT 0,
                    ContractNo TEXT,
                    FlagAutoPL TEXT NOT NULL DEFAULT '1',
                    Remark TEXT,
                    FOREIGN KEY(PerformanceInvoiceId) REFERENCES PerformanceInvoices(Id) ON DELETE CASCADE
                );
                CREATE INDEX IF NOT EXISTS IX_PerformanceInvoiceDetails_RefNo ON PerformanceInvoiceDetails(RefNo);
                CREATE INDEX IF NOT EXISTS IX_PerformanceInvoiceDetails_LCTemp ON PerformanceInvoiceDetails(LCTemp);
                CREATE INDEX IF NOT EXISTS IX_PerformanceInvoiceDetails_WorkOrderNo ON PerformanceInvoiceDetails(WorkOrderNo);
                CREATE INDEX IF NOT EXISTS IX_PerformanceInvoiceDetails_ContractNo ON PerformanceInvoiceDetails(ContractNo);
                CREATE INDEX IF NOT EXISTS IX_PerformanceInvoiceDetails_ModelCode ON PerformanceInvoiceDetails(ModelCode);
                CREATE INDEX IF NOT EXISTS IX_PerformanceInvoiceDetails_PortCode ON PerformanceInvoiceDetails(PortCode);

                CREATE TABLE IF NOT EXISTS ContractOverseas (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    OrgId TEXT NOT NULL,
                    ContractNo TEXT NOT NULL,
                    ContractDate TEXT NOT NULL,
                    SupplierCode TEXT NOT NULL,
                    SupplierName TEXT NOT NULL,
                    LCNo TEXT,
                    BankName TEXT,
                    Incoterms TEXT NOT NULL,
                    DestinationPort TEXT NOT NULL,
                    Currency TEXT NOT NULL DEFAULT 'USD',
                    ExpectedDeliveryDate TEXT,
                    TotalQuantity INTEGER NOT NULL DEFAULT 0,
                    TotalAmount REAL NOT NULL DEFAULT 0,
                    Status INTEGER NOT NULL DEFAULT 1,
                    Remark TEXT,
                    CancelReason TEXT,
                    CreatedBy TEXT,
                    CreatedAt TEXT NOT NULL,
                    UpdatedBy TEXT,
                    UpdatedAt TEXT,
                    CancelledBy TEXT,
                    CancelledAt TEXT
                );
                CREATE UNIQUE INDEX IF NOT EXISTS IX_ContractOverseas_OrgId_ContractNo ON ContractOverseas(OrgId, ContractNo);
                CREATE INDEX IF NOT EXISTS IX_ContractOverseas_OrgId_LCNo ON ContractOverseas(OrgId, LCNo);
                CREATE INDEX IF NOT EXISTS IX_ContractOverseas_OrgId_Status ON ContractOverseas(OrgId, Status);
                CREATE INDEX IF NOT EXISTS IX_ContractOverseas_OrgId_SupplierCode ON ContractOverseas(OrgId, SupplierCode);

                CREATE TABLE IF NOT EXISTS LettersOfCredit (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    OrgId TEXT NOT NULL,
                    LCNo TEXT NOT NULL,
                    ContractNo TEXT NOT NULL,
                    BankCode TEXT NOT NULL,
                    BankName TEXT NOT NULL,
                    BankBranch TEXT,
                    LCType INTEGER NOT NULL DEFAULT 0,
                    Amount REAL NOT NULL DEFAULT 0,
                    Currency TEXT NOT NULL DEFAULT 'USD',
                    IssueDate TEXT NOT NULL,
                    ExpireDate TEXT NOT NULL,
                    LatestShipmentDate TEXT,
                    Status INTEGER NOT NULL DEFAULT 0,
                    Remark TEXT,
                    CreatedBy TEXT,
                    CreatedAt TEXT NOT NULL,
                    UpdatedBy TEXT,
                    UpdatedAt TEXT
                );
                CREATE UNIQUE INDEX IF NOT EXISTS IX_LettersOfCredit_OrgId_LCNo ON LettersOfCredit(OrgId, LCNo);
                CREATE INDEX IF NOT EXISTS IX_LettersOfCredit_OrgId_ContractNo ON LettersOfCredit(OrgId, ContractNo);
                CREATE INDEX IF NOT EXISTS IX_LettersOfCredit_OrgId_BankCode ON LettersOfCredit(OrgId, BankCode);
                CREATE INDEX IF NOT EXISTS IX_LettersOfCredit_OrgId_Status ON LettersOfCredit(OrgId, Status);

                CREATE TABLE IF NOT EXISTS TransportInsOrders (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    OrgId TEXT NOT NULL,
                    TransportInsNo TEXT NOT NULL,
                    PmtMonth TEXT NOT NULL,
                    TotalAmount REAL NOT NULL DEFAULT 0,
                    VAT REAL NOT NULL DEFAULT 10,
                    UnitPriceVAT REAL NOT NULL DEFAULT 0,
                    TotalAmountVAT REAL NOT NULL DEFAULT 0,
                    TotalCars INTEGER NOT NULL DEFAULT 0,
                    TotalTransportCost REAL NOT NULL DEFAULT 0,
                    TotalDelayPenalty REAL NOT NULL DEFAULT 0,
                    TotalInsuranceCost REAL NOT NULL DEFAULT 0,
                    Status INTEGER NOT NULL DEFAULT 0,
                    HTVSignStatus INTEGER NOT NULL DEFAULT 0,
                    HTVSignDTime TEXT,
                    HTVSignBy TEXT,
                    TCMSSignStatus INTEGER NOT NULL DEFAULT 0,
                    TCMSSignDTime TEXT,
                    TCMSSignBy TEXT,
                    FilePath TEXT,
                    App1DTime TEXT,
                    App1By TEXT,
                    App2DTime TEXT,
                    App2By TEXT,
                    CancelDTime TEXT,
                    CancelBy TEXT,
                    CancelReason TEXT,
                    Remark TEXT,
                    CreateDateTime TEXT NOT NULL,
                    CreateBy TEXT,
                    LogLUDateTime TEXT NOT NULL,
                    LogLUBy TEXT
                );
                CREATE UNIQUE INDEX IF NOT EXISTS IX_TransportInsOrders_OrgId_TransportInsNo ON TransportInsOrders(OrgId, TransportInsNo);
                CREATE INDEX IF NOT EXISTS IX_TransportInsOrders_OrgId_PmtMonth ON TransportInsOrders(OrgId, PmtMonth);
                CREATE INDEX IF NOT EXISTS IX_TransportInsOrders_OrgId_Status ON TransportInsOrders(OrgId, Status);

                CREATE TABLE IF NOT EXISTS TransportInsOrderDetails (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    TransportInsOrderId INTEGER NOT NULL,
                    TransportInsNo TEXT NOT NULL,
                    Vin TEXT NOT NULL,
                    CarId TEXT,
                    DlvMnNo TEXT,
                    TranspReqType TEXT NOT NULL DEFAULT 'CARTRANSPORT',
                    ModelCode TEXT,
                    ModelName TEXT,
                    SpecCode TEXT,
                    ColorName TEXT,
                    EngineNo TEXT,
                    FStorageCode TEXT,
                    FProvinceName TEXT,
                    TStorageCode TEXT,
                    TProvinceName TEXT,
                    InvStartDate TEXT,
                    ExpectedDays INTEGER NOT NULL DEFAULT 2,
                    ExpectedDlvEndDate TEXT,
                    InvEndDate TEXT,
                    DelayDate INTEGER NOT NULL DEFAULT 0,
                    TransportCost REAL NOT NULL DEFAULT 0,
                    DelayPenaty REAL NOT NULL DEFAULT 0,
                    PriceCar REAL NOT NULL DEFAULT 0,
                    InsurancePercent REAL NOT NULL DEFAULT 0.0005,
                    InsuranceContractNo TEXT,
                    InsuranceCost REAL NOT NULL DEFAULT 0,
                    TotalPrice REAL NOT NULL DEFAULT 0,
                    Status INTEGER NOT NULL DEFAULT 0,
                    FProvinceRemark TEXT,
                    StandardRemark TEXT,
                    Remark TEXT,
                    LogLUDateTime TEXT NOT NULL,
                    LogLUBy TEXT,
                    FOREIGN KEY(TransportInsOrderId) REFERENCES TransportInsOrders(Id) ON DELETE CASCADE
                );
                CREATE INDEX IF NOT EXISTS IX_TransportInsOrderDetails_TransportInsNo ON TransportInsOrderDetails(TransportInsNo);
                CREATE INDEX IF NOT EXISTS IX_TransportInsOrderDetails_Vin ON TransportInsOrderDetails(Vin);
                CREATE INDEX IF NOT EXISTS IX_TransportInsOrderDetails_DlvMnNo ON TransportInsOrderDetails(DlvMnNo);

                CREATE TABLE IF NOT EXISTS AvnUnitPrices (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    AvnCode TEXT NOT NULL,
                    AvnName TEXT NOT NULL,
                    ModelCode TEXT NOT NULL,
                    UnitPrice REAL NOT NULL DEFAULT 0,
                    EffStartDate TEXT NOT NULL,
                    EffEndDate TEXT,
                    IsActive INTEGER NOT NULL DEFAULT 1,
                    Remark TEXT
                );
                CREATE UNIQUE INDEX IF NOT EXISTS IX_AvnUnitPrices_AvnCode ON AvnUnitPrices(AvnCode);

                CREATE TABLE IF NOT EXISTS PaymentAVNOrders (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    OrgId TEXT NOT NULL,
                    PaymentAVNNo TEXT NOT NULL,
                    PmtMonth TEXT NOT NULL,
                    SupplierCode TEXT NOT NULL DEFAULT 'MOBIS-VN',
                    SupplierName TEXT NOT NULL DEFAULT '',
                    TotalCars INTEGER NOT NULL DEFAULT 0,
                    AmountTotal REAL NOT NULL DEFAULT 0,
                    VatRate REAL NOT NULL DEFAULT 10,
                    UnitPriceVAT REAL NOT NULL DEFAULT 0,
                    TotalAmountVAT REAL NOT NULL DEFAULT 0,
                    Status INTEGER NOT NULL DEFAULT 0,
                    TCMSSignStatus INTEGER NOT NULL DEFAULT 0,
                    TCMSSignDate TEXT,
                    TCMSSignBy TEXT,
                    HTVSignStatus INTEGER NOT NULL DEFAULT 0,
                    HTVSignDate TEXT,
                    HTVSignBy TEXT,
                    FilePath TEXT,
                    BankTxnRef TEXT,
                    Remark TEXT,
                    RejectReason TEXT,
                    CancelReason TEXT,
                    CreatedAt TEXT NOT NULL,
                    CreatedBy TEXT,
                    Approved1At TEXT,
                    Approved1By TEXT,
                    Approved2At TEXT,
                    Approved2By TEXT,
                    SettledAt TEXT,
                    SettledBy TEXT,
                    RejectedAt TEXT,
                    RejectedBy TEXT,
                    CancelledAt TEXT,
                    CancelledBy TEXT,
                    LUDateTime TEXT,
                    LUBy TEXT
                );
                CREATE UNIQUE INDEX IF NOT EXISTS IX_PaymentAVNOrders_OrgId_PaymentAVNNo ON PaymentAVNOrders(OrgId, PaymentAVNNo);
                CREATE INDEX IF NOT EXISTS IX_PaymentAVNOrders_OrgId_PmtMonth ON PaymentAVNOrders(OrgId, PmtMonth);
                CREATE INDEX IF NOT EXISTS IX_PaymentAVNOrders_OrgId_Status ON PaymentAVNOrders(OrgId, Status);

                CREATE TABLE IF NOT EXISTS PaymentAVNDetails (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    PaymentAVNId INTEGER NOT NULL,
                    PaymentAVNNo TEXT NOT NULL,
                    Vin TEXT NOT NULL,
                    CarId TEXT,
                    EngineNo TEXT,
                    ModelCode TEXT NOT NULL,
                    ModelName TEXT NOT NULL,
                    SpecCode TEXT,
                    ColorCode TEXT,
                    ColorName TEXT,
                    AvnCode TEXT NOT NULL,
                    SerialNo TEXT NOT NULL,
                    UnitPriceAVN REAL NOT NULL DEFAULT 0,
                    AVNDate TEXT,
                    InStorageDate TEXT,
                    Status INTEGER NOT NULL DEFAULT 0,
                    Remark TEXT,
                    FOREIGN KEY(PaymentAVNId) REFERENCES PaymentAVNOrders(Id) ON DELETE CASCADE
                );
                CREATE INDEX IF NOT EXISTS IX_PaymentAVNDetails_PaymentAVNNo ON PaymentAVNDetails(PaymentAVNNo);
                CREATE INDEX IF NOT EXISTS IX_PaymentAVNDetails_Vin ON PaymentAVNDetails(Vin);
                CREATE INDEX IF NOT EXISTS IX_PaymentAVNDetails_AvnCode ON PaymentAVNDetails(AvnCode);

                CREATE TABLE IF NOT EXISTS WarrantyExpires (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    OrgId TEXT NOT NULL,
                    ModelCode TEXT NOT NULL,
                    ModelName TEXT,
                    WarrantyExpires REAL NOT NULL DEFAULT 0,
                    WarrantyKM REAL NOT NULL DEFAULT 0,
                    FlagActive TEXT NOT NULL DEFAULT '1',
                    Remark TEXT,
                    LogLUDateTime TEXT NOT NULL,
                    LogLUBy TEXT
                );
                CREATE UNIQUE INDEX IF NOT EXISTS IX_WarrantyExpires_OrgId_ModelCode ON WarrantyExpires(OrgId, ModelCode);

                CREATE TABLE IF NOT EXISTS Zones (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    OrgId TEXT NOT NULL,
                    ZoneCode TEXT NOT NULL,
                    ZoneName TEXT,
                    Remark TEXT,
                    FlagActive TEXT NOT NULL DEFAULT '1',
                    LogLUDTime TEXT NOT NULL,
                    LogLUBy TEXT
                );
                CREATE UNIQUE INDEX IF NOT EXISTS IX_Zones_OrgId_ZoneCode ON Zones(OrgId, ZoneCode);

                CREATE TABLE IF NOT EXISTS DealerZones (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    OrgId TEXT NOT NULL,
                    DealerCode TEXT NOT NULL,
                    DealerName TEXT,
                    ZoneCode TEXT NOT NULL,
                    ZoneName TEXT,
                    Remark TEXT,
                    FlagActive TEXT NOT NULL DEFAULT '1',
                    LogLUDTime TEXT NOT NULL,
                    LogLUBy TEXT
                );
                CREATE UNIQUE INDEX IF NOT EXISTS IX_DealerZones_OrgId_DealerCode_ZoneCode ON DealerZones(OrgId, DealerCode, ZoneCode);
                CREATE INDEX IF NOT EXISTS IX_DealerZones_OrgId_DealerCode ON DealerZones(OrgId, DealerCode);
                CREATE INDEX IF NOT EXISTS IX_DealerZones_OrgId_ZoneCode ON DealerZones(OrgId, ZoneCode);

                CREATE TABLE IF NOT EXISTS DelayTransports (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    OrgId TEXT NOT NULL,
                    StorageCode TEXT NOT NULL,
                    StorageName TEXT,
                    DealerCode TEXT NOT NULL,
                    DealerName TEXT,
                    DelayTransport REAL NOT NULL DEFAULT 0,
                    FlagActive TEXT NOT NULL DEFAULT '1',
                    LogLUDateTime TEXT NOT NULL,
                    LogLUBy TEXT
                );
                CREATE UNIQUE INDEX IF NOT EXISTS IX_DelayTransports_OrgId_StorageCode_DealerCode ON DelayTransports(OrgId, StorageCode, DealerCode);
                CREATE INDEX IF NOT EXISTS IX_DelayTransports_OrgId_StorageCode ON DelayTransports(OrgId, StorageCode);
                CREATE INDEX IF NOT EXISTS IX_DelayTransports_OrgId_DealerCode ON DelayTransports(OrgId, DealerCode);

                CREATE TABLE IF NOT EXISTS TransportFeeVersions (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    OrgId TEXT NOT NULL,
                    TFVCode TEXT NOT NULL,
                    FlagActive TEXT NOT NULL DEFAULT '1',
                    CreatedDate TEXT NOT NULL,
                    LogLUDateTime TEXT NOT NULL,
                    LogLUBy TEXT
                );
                CREATE UNIQUE INDEX IF NOT EXISTS IX_TransportFeeVersions_OrgId_TFVCode ON TransportFeeVersions(OrgId, TFVCode);

                CREATE TABLE IF NOT EXISTS TransportFeeDetails (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    VersionId INTEGER NOT NULL,
                    TFVCode TEXT NOT NULL,
                    ProvinceCodeFrom TEXT NOT NULL,
                    DistrictCodeFrom TEXT NOT NULL,
                    ProvinceCodeTo TEXT NOT NULL,
                    DistrictCodeTo TEXT NOT NULL,
                    TransporterCode TEXT NOT NULL,
                    ModelCode TEXT NOT NULL,
                    ValFee REAL NOT NULL DEFAULT 0,
                    ExpectedDays INTEGER NOT NULL DEFAULT 0,
                    IsReverseRoute INTEGER NOT NULL DEFAULT 0,
                    LogLUDateTime TEXT NOT NULL,
                    LogLUBy TEXT
                );
                CREATE INDEX IF NOT EXISTS IX_TransportFeeDetails_TFVCode ON TransportFeeDetails(TFVCode);
                CREATE INDEX IF NOT EXISTS IX_TransportFeeDetails_Route ON TransportFeeDetails(ProvinceCodeFrom, DistrictCodeFrom, ProvinceCodeTo, DistrictCodeTo);
                CREATE INDEX IF NOT EXISTS IX_TransportFeeDetails_TransporterCode ON TransportFeeDetails(TransporterCode);
                CREATE INDEX IF NOT EXISTS IX_TransportFeeDetails_ModelCode ON TransportFeeDetails(ModelCode);

                CREATE TABLE IF NOT EXISTS TransportFeeHistories (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    OrgId TEXT NOT NULL,
                    TFVCode TEXT NOT NULL,
                    ProvinceCodeFrom TEXT NOT NULL,
                    DistrictCodeFrom TEXT NOT NULL,
                    ProvinceCodeTo TEXT NOT NULL,
                    DistrictCodeTo TEXT NOT NULL,
                    TransporterCodeList TEXT,
                    ModelCodeList TEXT,
                    ValFee REAL NOT NULL DEFAULT 0,
                    ExpectedDays INTEGER NOT NULL DEFAULT 0,
                    LogLUDateTime TEXT NOT NULL,
                    LogLUBy TEXT
                );
                CREATE INDEX IF NOT EXISTS IX_TransportFeeHistories_OrgId_TFVCode ON TransportFeeHistories(OrgId, TFVCode);

                CREATE TABLE IF NOT EXISTS SalesManViolates (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    OrgId TEXT NOT NULL,
                    SMCode TEXT NOT NULL,
                    ViolateNumber INTEGER NOT NULL DEFAULT 1,
                    DealerCode TEXT NOT NULL,
                    DealerName TEXT,
                    ViolateType INTEGER NOT NULL,
                    ViolateDateStart TEXT NOT NULL,
                    ViolateDateEnd TEXT,
                    SMHyundaiCode TEXT,
                    SMName TEXT,
                    SMDateOfBirth TEXT,
                    IdentityCardNo TEXT,
                    SMPhoneNo TEXT,
                    SMType TEXT,
                    SMStatus TEXT,
                    Remark TEXT,
                    FlagActive TEXT NOT NULL DEFAULT '1',
                    CreatedBy TEXT,
                    CreatedAt TEXT NOT NULL,
                    UpdateBy TEXT,
                    UpdateDTime TEXT,
                    LogLUDateTime TEXT NOT NULL,
                    LogLUBy TEXT
                );
                CREATE UNIQUE INDEX IF NOT EXISTS IX_SalesManViolates_OrgId_SMCode_ViolateNumber ON SalesManViolates(OrgId, SMCode, ViolateNumber);
                CREATE INDEX IF NOT EXISTS IX_SalesManViolates_OrgId_SMCode ON SalesManViolates(OrgId, SMCode);
                CREATE INDEX IF NOT EXISTS IX_SalesManViolates_OrgId_DealerCode ON SalesManViolates(OrgId, DealerCode);

                CREATE TABLE IF NOT EXISTS CustomerVisits (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    OrgId TEXT NOT NULL,
                    CtmVisitCode TEXT NOT NULL,
                    DealerCode TEXT NOT NULL,
                    DealerName TEXT,
                    Gender INTEGER NOT NULL,
                    RangeAgeCode TEXT,
                    ModelCode TEXT,
                    ModelName TEXT,
                    VisitDTime TEXT NOT NULL,
                    FlagActive TEXT NOT NULL DEFAULT '1',
                    Remark TEXT,
                    CreatedBy TEXT,
                    CreatedAt TEXT NOT NULL,
                    LogLUBy TEXT,
                    LogLUDTime TEXT NOT NULL
                );
                CREATE UNIQUE INDEX IF NOT EXISTS IX_CustomerVisits_OrgId_CtmVisitCode_DealerCode ON CustomerVisits(OrgId, CtmVisitCode, DealerCode);
                CREATE INDEX IF NOT EXISTS IX_CustomerVisits_OrgId_DealerCode ON CustomerVisits(OrgId, DealerCode);
                CREATE INDEX IF NOT EXISTS IX_CustomerVisits_OrgId_ModelCode ON CustomerVisits(OrgId, ModelCode);

                CREATE TABLE IF NOT EXISTS Transporters (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    OrgId TEXT NOT NULL,
                    TransporterCode TEXT NOT NULL,
                    TransporterName TEXT NOT NULL,
                    TransportContractNo TEXT NOT NULL,
                    Address TEXT,
                    PhoneNo TEXT,
                    FaxNo TEXT,
                    DirectorFullName TEXT,
                    DirectorPhoneNo TEXT,
                    ContactorFullName TEXT,
                    ContactorPhoneNo TEXT,
                    Remark TEXT,
                    FlagActive TEXT NOT NULL DEFAULT '1',
                    LogLUDateTime TEXT NOT NULL,
                    LogLUBy TEXT
                );
                CREATE UNIQUE INDEX IF NOT EXISTS IX_Transporters_OrgId_TransporterCode ON Transporters(OrgId, TransporterCode);

                CREATE TABLE IF NOT EXISTS TransporterCars (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    OrgId TEXT NOT NULL,
                    TransporterCode TEXT NOT NULL,
                    PlateNo TEXT NOT NULL,
                    FlagActive TEXT NOT NULL DEFAULT '1',
                    LogLUDateTime TEXT NOT NULL,
                    LogLUBy TEXT
                );
                CREATE UNIQUE INDEX IF NOT EXISTS IX_TransporterCars_OrgId_TransporterCode_PlateNo ON TransporterCars(OrgId, TransporterCode, PlateNo);
                CREATE INDEX IF NOT EXISTS IX_TransporterCars_OrgId_TransporterCode ON TransporterCars(OrgId, TransporterCode);

                CREATE TABLE IF NOT EXISTS TransporterDrivers (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    OrgId TEXT NOT NULL,
                    TransporterCode TEXT NOT NULL,
                    DriverId TEXT NOT NULL,
                    DriverFullName TEXT NOT NULL,
                    DriverLicenseNo TEXT NOT NULL,
                    DriverPhoneNo TEXT,
                    FlagActive TEXT NOT NULL DEFAULT '1',
                    LogLUDateTime TEXT NOT NULL,
                    LogLUBy TEXT
                );
                CREATE UNIQUE INDEX IF NOT EXISTS IX_TransporterDrivers_OrgId_TransporterCode_DriverId ON TransporterDrivers(OrgId, TransporterCode, DriverId);
                CREATE INDEX IF NOT EXISTS IX_TransporterDrivers_OrgId_TransporterCode ON TransporterDrivers(OrgId, TransporterCode);

                CREATE TABLE IF NOT EXISTS Quotas (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    OrgId TEXT NOT NULL,
                    DealerCode TEXT NOT NULL,
                    DealerName TEXT,
                    SpecCode TEXT NOT NULL,
                    SpecDescription TEXT,
                    ModelCode TEXT,
                    ModelName TEXT,
                    QtyQuota REAL NOT NULL DEFAULT 0,
                    FlagActive TEXT NOT NULL DEFAULT '1',
                    UpdateBy TEXT,
                    UpdateDTime TEXT,
                    LogLUDateTime TEXT NOT NULL,
                    LogLUBy TEXT
                );
                CREATE UNIQUE INDEX IF NOT EXISTS IX_Quotas_OrgId_DealerCode_SpecCode ON Quotas(OrgId, DealerCode, SpecCode);
                CREATE INDEX IF NOT EXISTS IX_Quotas_OrgId_DealerCode ON Quotas(OrgId, DealerCode);
                CREATE INDEX IF NOT EXISTS IX_Quotas_OrgId_SpecCode ON Quotas(OrgId, SpecCode);

                CREATE TABLE IF NOT EXISTS Banks (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    OrgId TEXT NOT NULL,
                    BankCode TEXT NOT NULL,
                    BankCodeParent TEXT,
                    ProvinceCode TEXT,
                    BankName TEXT NOT NULL,
                    PhoneNo TEXT,
                    FaxNo TEXT,
                    BenBankCode TEXT,
                    Address TEXT,
                    PICEmail TEXT,
                    PICName TEXT,
                    FlagPaymentBank TEXT NOT NULL DEFAULT '0',
                    FlagMortageBank TEXT NOT NULL DEFAULT '0',
                    FlagMonitorBank TEXT NOT NULL DEFAULT '0',
                    Remark TEXT,
                    FlagActive TEXT NOT NULL DEFAULT '1',
                    LogLUDateTime TEXT NOT NULL,
                    LogLUBy TEXT
                );
                CREATE UNIQUE INDEX IF NOT EXISTS IX_Banks_OrgId_BankCode ON Banks(OrgId, BankCode);
                CREATE INDEX IF NOT EXISTS IX_Banks_OrgId_BankCodeParent ON Banks(OrgId, BankCodeParent);

                CREATE TABLE IF NOT EXISTS CarModels (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    OrgId TEXT NOT NULL,
                    ModelCode TEXT NOT NULL,
                    ModelProductionCode TEXT NOT NULL DEFAULT '',
                    ModelName TEXT NOT NULL DEFAULT '',
                    FlagBusinessPlan TEXT NOT NULL DEFAULT '1',
                    FlagActive TEXT NOT NULL DEFAULT '1',
                    LogLUDateTime TEXT NOT NULL,
                    LogLUBy TEXT
                );
                CREATE UNIQUE INDEX IF NOT EXISTS IX_CarModels_OrgId_ModelCode ON CarModels(OrgId, ModelCode);

                CREATE TABLE IF NOT EXISTS CarColors (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    OrgId TEXT NOT NULL,
                    ModelCode TEXT NOT NULL,
                    ColorCode TEXT NOT NULL,
                    ColorExtType TEXT NOT NULL DEFAULT '',
                    ColorExtCode TEXT NOT NULL DEFAULT '',
                    ColorExtName TEXT NOT NULL DEFAULT '',
                    ColorExtNameVN TEXT NOT NULL DEFAULT '',
                    ColorIntCode TEXT NOT NULL DEFAULT '',
                    ColorIntName TEXT NOT NULL DEFAULT '',
                    ColorIntNameVN TEXT NOT NULL DEFAULT '',
                    ColorFee REAL NOT NULL DEFAULT 0,
                    Remark TEXT,
                    FlagActive TEXT NOT NULL DEFAULT '1',
                    LogLUDateTime TEXT NOT NULL,
                    LogLUBy TEXT
                );
                CREATE UNIQUE INDEX IF NOT EXISTS IX_CarColors_OrgId_ModelCode_ColorCode ON CarColors(OrgId, ModelCode, ColorCode);
                CREATE INDEX IF NOT EXISTS IX_CarColors_OrgId_ModelCode ON CarColors(OrgId, ModelCode);
            ");
        }
        catch
        {
            // Bỏ qua nếu DB là Postgres hoặc đã có bảng
        }

        try
        {
            await db.Database.ExecuteSqlRawAsync("ALTER TABLE CarVinInventories ADD COLUMN DeclarationNo TEXT;");
            await db.Database.ExecuteSqlRawAsync("ALTER TABLE CarVinInventories ADD COLUMN CustomsClearanceDate TEXT;");
        }
        catch
        {
            // Bỏ qua nếu cột đã tồn tại
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

        if (!await db.TransportPlans.AnyAsync(o => o.OrgId == orgId))
        {
            var plan1 = new TransportPlan
            {
                OrgId = orgId,
                PlanNo = "2603TP0001",
                VINPlan = "PL-SANTAFE-2603-01",
                VIN = "KMHE281BBSA129841",
                FlagRealVin = true,
                CarId = "CAR-SF-001",
                RefNo = "CT26010001",
                ModelCode = "SANTAFE",
                ModelName = "Hyundai Santa Fe 2.5 HTRAC",
                SpecCode = "SF-2.5-GAS",
                ColorCode = "WH",
                ColorName = "Trắng ngọc trai",
                StorageCode = "KHO_TONG",
                StorageName = "Kho Tổng Nhà máy Ninh Bình",
                DealerCode = "VN001",
                DealerName = "Hyundai Hà Nội",
                CQStartDate = DateTime.Today.AddDays(-5),
                ExpectedDate = DateTime.Today.AddDays(2),
                TransporterCode = "TRP-SAOMAI",
                TransporterName = "Công ty Vận tải Sao Mai Express",
                TPType = TransportPlanType.Road,
                FProvinceCode = "NB",
                FProvinceName = "Ninh Bình",
                FDistrictCode = "NB-GK",
                FDistrictName = "Gia Viễn",
                TProvinceCode = "HN",
                TProvinceName = "Hà Nội",
                TDistrictCode = "HN-CG",
                TDistrictName = "Cầu Giấy",
                Status = TransportPlanStatus.Approved,
                TransporterStatus = TransporterPlanStatus.Accepted,
                Remark = "Kế hoạch vận tải xe Santa Fe giao đại lý Hyundai Hà Nội, xe đã kiểm định chất lượng CQ xuất xưởng",
                CreatedBy = "HQ_LOGISTICS_PLANNER",
                CreatedDate = DateTime.Today.AddDays(-4),
                ApprovedBy = "HQ_LOGISTICS_DIRECTOR",
                ApprovedDate = DateTime.Today.AddDays(-3)
            };

            var plan2 = new TransportPlan
            {
                OrgId = orgId,
                PlanNo = "2603TP0002",
                VINPlan = "PL-TUCSON-2603-02",
                VIN = null,
                FlagRealVin = false,
                CarId = null,
                RefNo = "CT26010002",
                ModelCode = "TUCSON",
                ModelName = "Hyundai Tucson 2.0 AT",
                SpecCode = "TUC-2.0-GAS",
                ColorCode = "BK",
                ColorName = "Đen Ánh Kim",
                StorageCode = "KHO_DONG_ANH",
                StorageName = "Kho Trung chuyển Đông Anh",
                DealerCode = "VN065",
                DealerName = "Hyundai Đông Đô",
                CQStartDate = DateTime.Today.AddDays(-1),
                ExpectedDate = DateTime.Today.AddDays(4),
                TransporterCode = "TRP-ANVIET",
                TransporterName = "Đội xe Chuyên dùng An Việt Logistics",
                TPType = TransportPlanType.Road,
                FProvinceCode = "HN",
                FProvinceName = "Hà Nội",
                FDistrictCode = "HN-DA",
                FDistrictName = "Đông Anh",
                TProvinceCode = "HN",
                TProvinceName = "Hà Nội",
                TDistrictCode = "HN-DD",
                TDistrictName = "Đống Đa",
                Status = TransportPlanStatus.Pending,
                TransporterStatus = TransporterPlanStatus.Pending,
                Remark = "Kế hoạch điều phối xe Tucson chờ hoàn tất gán số khung thực tế sau dây chuyền lắp ráp",
                CreatedBy = "HQ_LOGISTICS_PLANNER",
                CreatedDate = DateTime.Today.AddDays(-2)
            };

            var plan3 = new TransportPlan
            {
                OrgId = orgId,
                PlanNo = "2603TP0003",
                VINPlan = "PL-CRETA-2603-03",
                VIN = "KMHE281BBSA129843",
                FlagRealVin = true,
                CarId = "CAR-CR-003",
                RefNo = "SO2603010003",
                ModelCode = "CRETA",
                ModelName = "Hyundai Creta 1.5 Cao Cấp",
                SpecCode = "CR-1.5-PRE",
                ColorCode = "RD",
                ColorName = "Đỏ Mận",
                StorageCode = "KHO_HAI_PHONG",
                StorageName = "Kho Cảng Tân Vũ Hải Phòng",
                DealerCode = "VS058",
                DealerName = "Hyundai Bình Dương",
                CQStartDate = DateTime.Today.AddDays(-10),
                ExpectedDate = DateTime.Today.AddDays(-2),
                ActualDate = DateTime.Today.AddDays(-1),
                TransporterCode = "TRP-TRUONGHAI",
                TransporterName = "Tổng công ty Vận tải Biển & Đường bộ Trường Hải",
                TPType = TransportPlanType.Water,
                FProvinceCode = "HP",
                FProvinceName = "Hải Phòng",
                FDistrictCode = "HP-HB",
                FDistrictName = "Hồng Bàng",
                TProvinceCode = "BD",
                TProvinceName = "Bình Dương",
                TDistrictCode = "BD-TDM",
                TDistrictName = "Thủ Dầu Một",
                Status = TransportPlanStatus.Finished,
                TransporterStatus = TransporterPlanStatus.Finished,
                Remark = "Kế hoạch vận tải xe Creta nhập khẩu bằng đường biển từ Hải Phòng vào Bình Dương hoàn tất giao xe",
                CreatedBy = "HQ_LOGISTICS_PLANNER",
                CreatedDate = DateTime.Today.AddDays(-8),
                ApprovedBy = "HQ_LOGISTICS_DIRECTOR",
                ApprovedDate = DateTime.Today.AddDays(-7),
                FinishedBy = "TRP_DISPATCHER",
                FinishedDate = DateTime.Today.AddDays(-1)
            };

            db.TransportPlans.AddRange(plan1, plan2, plan3);
            await db.SaveChangesAsync();
        }

        if (!await db.SaleAwardMinutes.AnyAsync(m => m.OrgId == orgId))
        {
            var award1 = new SaleAwardMinutes
            {
                OrgId = orgId,
                SaleAwardMinutesNo = "2603SAM00001",
                DealerCode = "VS058",
                DealerName = "Hyundai Bình Dương",
                SaleAwardMinutesStatus = SaleAwardMinutesStatus.Finished,
                TotalCars = 2,
                TotalAwardAmount = 30000000m,
                FilePath = "/uploads/sale_awards/2603SAM00001/2603SAM00001_Signed_VS058.pdf",
                FileName = "2603SAM00001_Signed_VS058.pdf",
                FileUrl = "/api/files/download/2603SAM00001_Signed_VS058.pdf",
                Remark = "Biên bản đối soát thưởng bán lẻ xe đợt 1 tháng 3/2026 - Đại lý đã ký số hoàn tất quyết toán",
                CreateDTime = DateTime.Today.AddDays(-15),
                CreateBy = "HQ_SALES_SPECIALIST",
                Appr1DTime = DateTime.Today.AddDays(-14),
                Appr1By = "HQ_SALES_MNG_LAN",
                Appr2DTime = DateTime.Today.AddDays(-12),
                Appr2By = "HQ_SALES_DIR_TUAN",
                FinishDTime = DateTime.Today.AddDays(-10),
                FinishBy = "VS058_DIRECTOR_HUNG",
                LogLUDateTime = DateTime.Today.AddDays(-10),
                LogLUBy = "VS058_DIRECTOR_HUNG",
                Details = new List<SaleAwardMinutesDetail>
                {
                    new()
                    {
                        SaleAwardMinutesNo = "2603SAM00001",
                        Vin = "RLUGT41DBST012693",
                        CarId = "CAR-ST012693",
                        DealerCode = "VS058",
                        DealerName = "Hyundai Bình Dương",
                        DocumentNo = "CV-2026/02-HTV-SALES",
                        AmountAward = 20000000m,
                        ModelCode = "SANTAFE",
                        ModelName = "Hyundai Santa Fe 2.2D HTRAC",
                        SpecCode = "SF-2.2D-PRE",
                        SpecDescription = "Santa Fe Máy dầu Cao cấp",
                        ColorCode = "WH",
                        ColorName = "Trắng Tuyết",
                        VinYear = 2026,
                        DeliveryDate = DateTime.Today.AddDays(-20),
                        DealNo = "DEAL2603001",
                        Remark = "Thưởng kích cầu dòng Santa Fe cao cấp",
                        LogLUDateTime = DateTime.Today.AddDays(-15),
                        LogLUBy = "HQ_SALES_SPECIALIST"
                    },
                    new()
                    {
                        SaleAwardMinutesNo = "2603SAM00001",
                        Vin = "KMHE281BBSA129843",
                        CarId = "CAR-SA129843",
                        DealerCode = "VS058",
                        DealerName = "Hyundai Bình Dương",
                        DocumentNo = "CV-2026/02-HTV-SALES",
                        AmountAward = 10000000m,
                        ModelCode = "CRETA",
                        ModelName = "Hyundai Creta 1.5 Cao Cấp",
                        SpecCode = "CR-1.5-PRE",
                        SpecDescription = "Creta Bản Cao Cấp",
                        ColorCode = "RD",
                        ColorName = "Đỏ Mận",
                        VinYear = 2026,
                        DeliveryDate = DateTime.Today.AddDays(-18),
                        DealNo = "DEAL2603002",
                        Remark = "Thưởng chỉ tiêu bán lẻ Creta đạt target tuần",
                        LogLUDateTime = DateTime.Today.AddDays(-15),
                        LogLUBy = "HQ_SALES_SPECIALIST"
                    }
                }
            };

            var award2 = new SaleAwardMinutes
            {
                OrgId = orgId,
                SaleAwardMinutesNo = "2603SAM00002",
                DealerCode = "VN065",
                DealerName = "Hyundai Đông Đô",
                SaleAwardMinutesStatus = SaleAwardMinutesStatus.Approved2,
                TotalCars = 2,
                TotalAwardAmount = 25000000m,
                Remark = "Lãnh đạo Khối Bán hàng đã duyệt chốt thưởng, chờ Đại lý tải file ký đóng dấu",
                CreateDTime = DateTime.Today.AddDays(-6),
                CreateBy = "HQ_SALES_SPECIALIST",
                Appr1DTime = DateTime.Today.AddDays(-5),
                Appr1By = "HQ_SALES_MNG_LAN",
                Appr2DTime = DateTime.Today.AddDays(-3),
                Appr2By = "HQ_SALES_DIR_TUAN",
                LogLUDateTime = DateTime.Today.AddDays(-3),
                LogLUBy = "HQ_SALES_DIR_TUAN",
                Details = new List<SaleAwardMinutesDetail>
                {
                    new()
                    {
                        SaleAwardMinutesNo = "2603SAM00002",
                        Vin = "KL4CJ63E8CB109283",
                        CarId = "CAR-CB109283",
                        DealerCode = "VN065",
                        DealerName = "Hyundai Đông Đô",
                        DocumentNo = "CV-2026/03-HTV-TUCSON",
                        AmountAward = 15000000m,
                        ModelCode = "TUCSON",
                        ModelName = "Hyundai Tucson 2.0 AT",
                        SpecCode = "TUC-2.0-GAS",
                        SpecDescription = "Tucson Bản Xăng Đặc biệt",
                        ColorCode = "BK",
                        ColorName = "Đen Ánh Kim",
                        VinYear = 2026,
                        DeliveryDate = DateTime.Today.AddDays(-9),
                        DealNo = "DEAL2603003",
                        Remark = "Thưởng chuyên đề tăng trưởng doanh số Tucson",
                        LogLUDateTime = DateTime.Today.AddDays(-6),
                        LogLUBy = "HQ_SALES_SPECIALIST"
                    },
                    new()
                    {
                        SaleAwardMinutesNo = "2603SAM00002",
                        Vin = "RLUGT41DBST012694",
                        CarId = "CAR-ST012694",
                        DealerCode = "VN065",
                        DealerName = "Hyundai Đông Đô",
                        DocumentNo = "CV-2026/03-HTV-TUCSON",
                        AmountAward = 10000000m,
                        ModelCode = "ACCENT",
                        ModelName = "Hyundai Accent 1.4 AT",
                        SpecCode = "ACC-1.4-AT",
                        SpecDescription = "Accent 1.4 AT Đặc biệt",
                        ColorCode = "WH",
                        ColorName = "Trắng Sữa",
                        VinYear = 2026,
                        DeliveryDate = DateTime.Today.AddDays(-8),
                        DealNo = "DEAL2603004",
                        Remark = "Thưởng đại lý vượt mốc giao xe sedan",
                        LogLUDateTime = DateTime.Today.AddDays(-6),
                        LogLUBy = "HQ_SALES_SPECIALIST"
                    }
                }
            };

            var award3 = new SaleAwardMinutes
            {
                OrgId = orgId,
                SaleAwardMinutesNo = "2603SAM00003",
                DealerCode = "VN012",
                DealerName = "Hyundai Hà Đông",
                SaleAwardMinutesStatus = SaleAwardMinutesStatus.Pending,
                TotalCars = 1,
                TotalAwardAmount = 15000000m,
                Remark = "Chuyên viên đối soát vừa lập biên bản theo công văn thưởng thế hệ mới, chờ Trưởng phòng duyệt cấp 1",
                CreateDTime = DateTime.Today.AddDays(-1),
                CreateBy = "HQ_SALES_SPECIALIST",
                LogLUDateTime = DateTime.Today.AddDays(-1),
                LogLUBy = "HQ_SALES_SPECIALIST",
                Details = new List<SaleAwardMinutesDetail>
                {
                    new()
                    {
                        SaleAwardMinutesNo = "2603SAM00003",
                        Vin = "RLUGT41DBST012695",
                        CarId = "CAR-ST012695",
                        DealerCode = "VN012",
                        DealerName = "Hyundai Hà Đông",
                        DocumentNo = "CV-2026/03-HTV-SANTAFE",
                        AmountAward = 15000000m,
                        ModelCode = "SANTAFE",
                        ModelName = "Hyundai Santa Fe Calligraphy 2.5T",
                        SpecCode = "SF-2.5T-CAL",
                        SpecDescription = "Santa Fe Calligraphy Turbo",
                        ColorCode = "GY",
                        ColorName = "Xám Kim Loại",
                        VinYear = 2026,
                        DeliveryDate = DateTime.Today.AddDays(-3),
                        DealNo = "DEAL2603005",
                        Remark = "Thưởng bàn giao xe Santa Fe mới",
                        LogLUDateTime = DateTime.Today.AddDays(-1),
                        LogLUBy = "HQ_SALES_SPECIALIST"
                    }
                }
            };

            db.SaleAwardMinutes.AddRange(award1, award2, award3);
            await db.SaveChangesAsync();
        }

        // Seed Kế hoạch Kinh doanh Bán buôn / Bán lẻ xe Ô tô Đại lý (BPL_BusinessPlan)
        if (!await db.BusinessPlans.AnyAsync(x => x.OrgId == orgId))
        {
            var plan1 = new BusinessPlan
            {
                OrgId = orgId,
                BusinessPlanCode = "2603BPL0001",
                DealerCode = "VS058",
                DealerName = "Hyundai Bình Dương",
                YearPlan = 2026,
                Version = "ACTUAL",
                TimesPlan = 1,
                Status = BusinessPlanStatus.Approved2,
                HTCStaffInCharge = "HQ_PLAN_SPECIALIST_NAM",
                TotalRtlTarget = 1480,
                TotalOrdTarget = 1550,
                TotalBOInitial = 115,
                Remark = "Kế hoạch kinh doanh xe năm 2026 đã được Lãnh đạo Khối Bán hàng phê duyệt chốt chính thức",
                CreatedAt = DateTime.Today.AddDays(-60),
                CreatedBy = "HQ_PLANNING",
                Approve1At = DateTime.Today.AddDays(-55),
                Approve1By = "HQ_PLAN_MNG_LAN",
                Approve2At = DateTime.Today.AddDays(-50),
                Approve2By = "HQ_SALES_DIR_TUAN",
                LogLUDateTime = DateTime.Today.AddDays(-50),
                LogLUBy = "HQ_SALES_DIR_TUAN",
                Details = new List<BusinessPlanDetail>
                {
                    new()
                    {
                        BusinessPlanCode = "2603BPL0001",
                        ModelCode = "SANTAFE",
                        ModelName = "Hyundai Santa Fe All New",
                        Rtl_TotalQtyDeal = 330,
                        Rtl_QtyM1 = 22, Rtl_QtyM2 = 22, Rtl_QtyM3 = 23,
                        Rtl_QtyM4 = 27, Rtl_QtyM5 = 28, Rtl_QtyM6 = 28,
                        Rtl_QtyM7 = 28, Rtl_QtyM8 = 28, Rtl_QtyM9 = 28,
                        Rtl_QtyM10 = 34, Rtl_QtyM11 = 35, Rtl_QtyM12 = 35,
                        Rtl_QtyPre = 290,
                        Rtl_GrowthRate = 13.79m,
                        Ord_TotalQtyOrder = 345,
                        Ord_QtyM1 = 23, Ord_QtyM2 = 23, Ord_QtyM3 = 24,
                        Ord_QtyM4 = 28, Ord_QtyM5 = 29, Ord_QtyM6 = 30,
                        Ord_QtyM7 = 30, Ord_QtyM8 = 30, Ord_QtyM9 = 30,
                        Ord_QtyM10 = 36, Ord_QtyM11 = 36, Ord_QtyM12 = 36,
                        Ord_QtyPre = 305,
                        Ord_GrowthRate = 13.11m,
                        BO_TotalQtyBO = 25,
                        Remark = "Model chủ lực phân khúc D-SUV"
                    },
                    new()
                    {
                        BusinessPlanCode = "2603BPL0001",
                        ModelCode = "TUCSON",
                        ModelName = "Hyundai Tucson Turbo",
                        Rtl_TotalQtyDeal = 380,
                        Rtl_QtyM1 = 25, Rtl_QtyM2 = 25, Rtl_QtyM3 = 26,
                        Rtl_QtyM4 = 31, Rtl_QtyM5 = 32, Rtl_QtyM6 = 32,
                        Rtl_QtyM7 = 32, Rtl_QtyM8 = 32, Rtl_QtyM9 = 33,
                        Rtl_QtyM10 = 39, Rtl_QtyM11 = 40, Rtl_QtyM12 = 41,
                        Rtl_QtyPre = 340,
                        Rtl_GrowthRate = 11.76m,
                        Ord_TotalQtyOrder = 400,
                        Ord_QtyM1 = 26, Ord_QtyM2 = 27, Ord_QtyM3 = 27,
                        Ord_QtyM4 = 33, Ord_QtyM5 = 34, Ord_QtyM6 = 34,
                        Ord_QtyM7 = 34, Ord_QtyM8 = 34, Ord_QtyM9 = 35,
                        Ord_QtyM10 = 41, Ord_QtyM11 = 42, Ord_QtyM12 = 43,
                        Ord_QtyPre = 355,
                        Ord_GrowthRate = 12.68m,
                        BO_TotalQtyBO = 30,
                        Remark = "C-SUV giữ nhịp tăng trưởng 12%"
                    },
                    new()
                    {
                        BusinessPlanCode = "2603BPL0001",
                        ModelCode = "CRETA",
                        ModelName = "Hyundai Creta B-SUV",
                        Rtl_TotalQtyDeal = 420,
                        Rtl_QtyM1 = 28, Rtl_QtyM2 = 28, Rtl_QtyM3 = 28,
                        Rtl_QtyM4 = 35, Rtl_QtyM5 = 35, Rtl_QtyM6 = 35,
                        Rtl_QtyM7 = 35, Rtl_QtyM8 = 35, Rtl_QtyM9 = 35,
                        Rtl_QtyM10 = 42, Rtl_QtyM11 = 42, Rtl_QtyM12 = 42,
                        Rtl_QtyPre = 380,
                        Rtl_GrowthRate = 10.53m,
                        Ord_TotalQtyOrder = 440,
                        Ord_QtyM1 = 29, Ord_QtyM2 = 29, Ord_QtyM3 = 30,
                        Ord_QtyM4 = 37, Ord_QtyM5 = 37, Ord_QtyM6 = 37,
                        Ord_QtyM7 = 37, Ord_QtyM8 = 37, Ord_QtyM9 = 37,
                        Ord_QtyM10 = 45, Ord_QtyM11 = 45, Ord_QtyM12 = 45,
                        Ord_QtyPre = 395,
                        Ord_GrowthRate = 11.39m,
                        BO_TotalQtyBO = 35,
                        Remark = "B-SUV doanh số ổn định"
                    },
                    new()
                    {
                        BusinessPlanCode = "2603BPL0001",
                        ModelCode = "ACCENT",
                        ModelName = "Hyundai Accent Sedan",
                        Rtl_TotalQtyDeal = 350,
                        Rtl_QtyM1 = 23, Rtl_QtyM2 = 23, Rtl_QtyM3 = 24,
                        Rtl_QtyM4 = 29, Rtl_QtyM5 = 29, Rtl_QtyM6 = 30,
                        Rtl_QtyM7 = 29, Rtl_QtyM8 = 29, Rtl_QtyM9 = 30,
                        Rtl_QtyM10 = 36, Rtl_QtyM11 = 36, Rtl_QtyM12 = 38,
                        Rtl_QtyPre = 320,
                        Rtl_GrowthRate = 9.38m,
                        Ord_TotalQtyOrder = 365,
                        Ord_QtyM1 = 24, Ord_QtyM2 = 24, Ord_QtyM3 = 25,
                        Ord_QtyM4 = 30, Ord_QtyM5 = 31, Ord_QtyM6 = 31,
                        Ord_QtyM7 = 30, Ord_QtyM8 = 31, Ord_QtyM9 = 31,
                        Ord_QtyM10 = 38, Ord_QtyM11 = 38, Ord_QtyM12 = 40,
                        Ord_QtyPre = 335,
                        Ord_GrowthRate = 8.96m,
                        BO_TotalQtyBO = 25,
                        Remark = "Sedan thế hệ mới"
                    }
                }
            };

            var plan2 = new BusinessPlan
            {
                OrgId = orgId,
                BusinessPlanCode = "2603BPL0002",
                DealerCode = "VN065",
                DealerName = "Hyundai Đông Đô",
                YearPlan = 2026,
                Version = "INIT",
                TimesPlan = 1,
                Status = BusinessPlanStatus.Approved1,
                HTCStaffInCharge = "HQ_PLAN_SPECIALIST_HOANG",
                TotalRtlTarget = 1120,
                TotalOrdTarget = 1180,
                TotalBOInitial = 80,
                Remark = "Trưởng phòng kế hoạch kinh doanh đã thẩm tra và duyệt cấp 1, đang trình Lãnh đạo Khối phê duyệt chốt",
                CreatedAt = DateTime.Today.AddDays(-20),
                CreatedBy = "HQ_PLANNING",
                Approve1At = DateTime.Today.AddDays(-15),
                Approve1By = "HQ_PLAN_MNG_LAN",
                LogLUDateTime = DateTime.Today.AddDays(-15),
                LogLUBy = "HQ_PLAN_MNG_LAN",
                Details = new List<BusinessPlanDetail>
                {
                    new()
                    {
                        BusinessPlanCode = "2603BPL0002",
                        ModelCode = "SANTAFE",
                        ModelName = "Hyundai Santa Fe All New",
                        Rtl_TotalQtyDeal = 300,
                        Rtl_QtyM1 = 20, Rtl_QtyM2 = 20, Rtl_QtyM3 = 20,
                        Rtl_QtyM4 = 25, Rtl_QtyM5 = 25, Rtl_QtyM6 = 25,
                        Rtl_QtyM7 = 25, Rtl_QtyM8 = 25, Rtl_QtyM9 = 25,
                        Rtl_QtyM10 = 30, Rtl_QtyM11 = 30, Rtl_QtyM12 = 30,
                        Rtl_QtyPre = 260,
                        Rtl_GrowthRate = 15.38m,
                        Ord_TotalQtyOrder = 315,
                        Ord_QtyM1 = 21, Ord_QtyM2 = 21, Ord_QtyM3 = 21,
                        Ord_QtyM4 = 26, Ord_QtyM5 = 26, Ord_QtyM6 = 27,
                        Ord_QtyM7 = 26, Ord_QtyM8 = 26, Ord_QtyM9 = 27,
                        Ord_QtyM10 = 31, Ord_QtyM11 = 32, Ord_QtyM12 = 32,
                        Ord_QtyPre = 275,
                        Ord_GrowthRate = 14.55m,
                        BO_TotalQtyBO = 20,
                        Remark = "Kế hoạch đẩy mạnh Santa Fe tại khu vực miền Bắc"
                    },
                    new()
                    {
                        BusinessPlanCode = "2603BPL0002",
                        ModelCode = "TUCSON",
                        ModelName = "Hyundai Tucson Turbo",
                        Rtl_TotalQtyDeal = 350,
                        Rtl_QtyM1 = 23, Rtl_QtyM2 = 23, Rtl_QtyM3 = 24,
                        Rtl_QtyM4 = 29, Rtl_QtyM5 = 29, Rtl_QtyM6 = 30,
                        Rtl_QtyM7 = 29, Rtl_QtyM8 = 29, Rtl_QtyM9 = 30,
                        Rtl_QtyM10 = 35, Rtl_QtyM11 = 35, Rtl_QtyM12 = 37,
                        Rtl_QtyPre = 310,
                        Rtl_GrowthRate = 12.90m,
                        Ord_TotalQtyOrder = 370,
                        Ord_QtyM1 = 24, Ord_QtyM2 = 25, Ord_QtyM3 = 25,
                        Ord_QtyM4 = 31, Ord_QtyM5 = 31, Ord_QtyM6 = 31,
                        Ord_QtyM7 = 31, Ord_QtyM8 = 31, Ord_QtyM9 = 31,
                        Ord_QtyM10 = 37, Ord_QtyM11 = 37, Ord_QtyM12 = 39,
                        Ord_QtyPre = 325,
                        Ord_GrowthRate = 13.85m,
                        BO_TotalQtyBO = 25,
                        Remark = "Tucson bản xăng đặc biệt"
                    },
                    new()
                    {
                        BusinessPlanCode = "2603BPL0002",
                        ModelCode = "CRETA",
                        ModelName = "Hyundai Creta B-SUV",
                        Rtl_TotalQtyDeal = 470,
                        Rtl_QtyM1 = 31, Rtl_QtyM2 = 31, Rtl_QtyM3 = 32,
                        Rtl_QtyM4 = 39, Rtl_QtyM5 = 39, Rtl_QtyM6 = 40,
                        Rtl_QtyM7 = 39, Rtl_QtyM8 = 39, Rtl_QtyM9 = 40,
                        Rtl_QtyM10 = 47, Rtl_QtyM11 = 47, Rtl_QtyM12 = 49,
                        Rtl_QtyPre = 420,
                        Rtl_GrowthRate = 11.90m,
                        Ord_TotalQtyOrder = 495,
                        Ord_QtyM1 = 33, Ord_QtyM2 = 33, Ord_QtyM3 = 34,
                        Ord_QtyM4 = 41, Ord_QtyM5 = 41, Ord_QtyM6 = 42,
                        Ord_QtyM7 = 41, Ord_QtyM8 = 41, Ord_QtyM9 = 42,
                        Ord_QtyM10 = 49, Ord_QtyM11 = 50, Ord_QtyM12 = 51,
                        Ord_QtyPre = 440,
                        Ord_GrowthRate = 12.50m,
                        BO_TotalQtyBO = 35,
                        Remark = "Creta dẫn đầu thị phần B-SUV"
                    }
                }
            };

            var plan3 = new BusinessPlan
            {
                OrgId = orgId,
                BusinessPlanCode = "2603BPL0003",
                DealerCode = "VN012",
                DealerName = "Hyundai Hà Đông",
                YearPlan = 2026,
                Version = "INIT",
                TimesPlan = 1,
                Status = BusinessPlanStatus.Pending,
                HTCStaffInCharge = "HQ_PLAN_SPECIALIST_HOANG",
                TotalRtlTarget = 850,
                TotalOrdTarget = 890,
                TotalBOInitial = 60,
                Remark = "Đại lý vừa nộp dự thảo kế hoạch năm 2026, đang chờ Trưởng phòng kế hoạch thẩm tra sơ bộ",
                CreatedAt = DateTime.Today.AddDays(-5),
                CreatedBy = "VN012_DIRECTOR",
                LogLUDateTime = DateTime.Today.AddDays(-5),
                LogLUBy = "VN012_DIRECTOR",
                Details = new List<BusinessPlanDetail>
                {
                    new()
                    {
                        BusinessPlanCode = "2603BPL0003",
                        ModelCode = "SANTAFE",
                        ModelName = "Hyundai Santa Fe All New",
                        Rtl_TotalQtyDeal = 250,
                        Rtl_QtyM1 = 16, Rtl_QtyM2 = 17, Rtl_QtyM3 = 17,
                        Rtl_QtyM4 = 21, Rtl_QtyM5 = 21, Rtl_QtyM6 = 21,
                        Rtl_QtyM7 = 21, Rtl_QtyM8 = 21, Rtl_QtyM9 = 21,
                        Rtl_QtyM10 = 25, Rtl_QtyM11 = 25, Rtl_QtyM12 = 27,
                        Rtl_QtyPre = 220,
                        Rtl_GrowthRate = 13.64m,
                        Ord_TotalQtyOrder = 260,
                        Ord_QtyM1 = 17, Ord_QtyM2 = 17, Ord_QtyM3 = 18,
                        Ord_QtyM4 = 22, Ord_QtyM5 = 22, Ord_QtyM6 = 22,
                        Ord_QtyM7 = 22, Ord_QtyM8 = 22, Ord_QtyM9 = 22,
                        Ord_QtyM10 = 26, Ord_QtyM11 = 26, Ord_QtyM12 = 28,
                        Ord_QtyPre = 230,
                        Ord_GrowthRate = 13.04m,
                        BO_TotalQtyBO = 20,
                        Remark = "Dự thảo chỉ tiêu Santa Fe"
                    },
                    new()
                    {
                        BusinessPlanCode = "2603BPL0003",
                        ModelCode = "ACCENT",
                        ModelName = "Hyundai Accent Sedan",
                        Rtl_TotalQtyDeal = 600,
                        Rtl_QtyM1 = 40, Rtl_QtyM2 = 40, Rtl_QtyM3 = 40,
                        Rtl_QtyM4 = 50, Rtl_QtyM5 = 50, Rtl_QtyM6 = 50,
                        Rtl_QtyM7 = 50, Rtl_QtyM8 = 50, Rtl_QtyM9 = 50,
                        Rtl_QtyM10 = 60, Rtl_QtyM11 = 60, Rtl_QtyM12 = 60,
                        Rtl_QtyPre = 540,
                        Rtl_GrowthRate = 11.11m,
                        Ord_TotalQtyOrder = 630,
                        Ord_QtyM1 = 42, Ord_QtyM2 = 42, Ord_QtyM3 = 42,
                        Ord_QtyM4 = 52, Ord_QtyM5 = 53, Ord_QtyM6 = 53,
                        Ord_QtyM7 = 52, Ord_QtyM8 = 53, Ord_QtyM9 = 53,
                        Ord_QtyM10 = 63, Ord_QtyM11 = 63, Ord_QtyM12 = 63,
                        Ord_QtyPre = 565,
                        Ord_GrowthRate = 11.50m,
                        BO_TotalQtyBO = 40,
                        Remark = "Dự thảo chỉ tiêu Accent dịch vụ & cá nhân"
                    }
                }
            };

            db.BusinessPlans.AddRange(plan1, plan2, plan3);
            await db.SaveChangesAsync();
        }

        if (!await db.FnExpCalcSheets.AnyAsync())
        {
            var sheet1 = new FnExpCalcSheet
            {
                OrgId = orgId,
                CaNo = "260320-001/CPTC/VS058",
                DealerCode = "VS058",
                DealerName = "Hyundai Bình Dương",
                TermFrom = new DateTime(2026, 3, 1),
                TermTo = new DateTime(2026, 3, 31),
                TermPrevFrom = new DateTime(2026, 2, 1),
                TermPrevTo = new DateTime(2026, 2, 28),
                FnExpPercent = 5.5m,
                PmtDsTCGPercent = 2.0m,
                CAName = "Bảng tính CPTC & CKTT kỳ 03/2026 - Hyundai Bình Dương",
                FlagEarlyCancel = "0",
                FlagisHTC = "HTC",
                DlrSignStatus = FnExpSignStatus.Approved2,
                HTCSignStatus = FnExpSignStatus.Approved2,
                Status = FnExpStatus.Approved,
                TotalCars = 3,
                TotalFnDepositAmount = 1450000m,
                TotalFnGrtAmount = 2650000m,
                TotalFnAmount = 4100000m,
                TotalPDAmount = 11850000m,
                NetSettlementAmount = 7750000m,
                FilePathFnExp = "https://ftest.ecore.vn/20260320/CPTC_260320-001_CPTC_VS058.xlsx",
                FilePathPmtDC = "https://ftest.ecore.vn/20260320/CKTT_260320-001_CPTC_VS058.xlsx",
                DlrAppr1At = new DateTime(2026, 3, 20, 10, 15, 0),
                DlrAppr1By = "VS058_ACCOUNTANT",
                HTCAppr1At = new DateTime(2026, 3, 20, 14, 30, 0),
                HTCAppr1By = "HTC_FIN_SPECIALIST",
                DlrAppr2At = new DateTime(2026, 3, 21, 9, 0, 0),
                DlrAppr2By = "VS058_DIRECTOR",
                HTCAppr2At = new DateTime(2026, 3, 21, 11, 20, 0),
                HTCAppr2By = "HTC_FINANCE_DIRECTOR",
                Remark = "Đã quyết toán hoàn tất CPTC và chiết khấu thanh toán sớm kỳ 03/2026",
                CreatedAt = new DateTime(2026, 3, 20, 8, 30, 0),
                CreatedBy = "HTC_FIN_SPECIALIST",
                Details = new List<FnExpCalcSheetDetail>
                {
                    new()
                    {
                        CaNo = "260320-001/CPTC/VS058",
                        CarId = "2603C101-ACCENT",
                        Vin = "KMHCT41BPST030101",
                        ModelCode = "ACCENT",
                        ModelName = "Hyundai Accent 1.5 AT Đặc Biệt",
                        UnitPriceActual = 569000000m,
                        SodApprovedDate = new DateTime(2026, 2, 10),
                        SodDepositDutyEndDate = new DateTime(2026, 2, 17),
                        DateStart = new DateTime(2026, 2, 20),
                        DateEnd = new DateTime(2026, 4, 20),
                        TotalCompletedDate = new DateTime(2026, 3, 10),
                        TermActual = 60,
                        FnDepositCountDate = 10,
                        FnDepositAmount = 391188m,
                        FnGrtCountDate = 0,
                        FnGrtAmount = 0,
                        FnTotalAmount = 391188m,
                        PDCountDate = 41,
                        PDAmount = 2592078m,
                        Status = FnExpStatus.Approved,
                        Remark = "Thanh toán sớm 41 ngày trước hạn bảo lãnh"
                    },
                    new()
                    {
                        CaNo = "260320-001/CPTC/VS058",
                        CarId = "2603C102-CRETA",
                        Vin = "KMHCT41BPST030102",
                        ModelCode = "CRETA",
                        ModelName = "Hyundai Creta B-SUV 1.5 Cao Cấp",
                        UnitPriceActual = 699000000m,
                        SodApprovedDate = new DateTime(2026, 2, 12),
                        SodDepositDutyEndDate = new DateTime(2026, 2, 19),
                        DateStart = new DateTime(2026, 2, 22),
                        DateEnd = new DateTime(2026, 4, 22),
                        TotalCompletedDate = new DateTime(2026, 3, 15),
                        TermActual = 60,
                        FnDepositCountDate = 15,
                        FnDepositAmount = 720844m,
                        FnGrtCountDate = 0,
                        FnGrtAmount = 0,
                        FnTotalAmount = 720844m,
                        PDCountDate = 38,
                        PDAmount = 2951333m,
                        Status = FnExpStatus.Approved,
                        Remark = "Thanh toán sớm 38 ngày trước hạn bảo lãnh"
                    },
                    new()
                    {
                        CaNo = "260320-001/CPTC/VS058",
                        CarId = "2603C103-TUCSON",
                        Vin = "KMHCT41BPST030103",
                        ModelCode = "TUCSON",
                        ModelName = "Hyundai Tucson 1.6 Turbo AWD",
                        UnitPriceActual = 959000000m,
                        SodApprovedDate = new DateTime(2026, 2, 15),
                        SodDepositDutyEndDate = new DateTime(2026, 2, 22),
                        DateStart = new DateTime(2026, 2, 25),
                        DateEnd = new DateTime(2026, 4, 25),
                        TotalCompletedDate = new DateTime(2026, 3, 5),
                        TermActual = 60,
                        FnDepositCountDate = 5,
                        FnDepositAmount = 337968m,
                        FnGrtCountDate = 10,
                        FnGrtAmount = 2650000m,
                        FnTotalAmount = 2987968m,
                        PDCountDate = 51,
                        PDAmount = 6306589m,
                        Status = FnExpStatus.Approved,
                        Remark = "Thanh toán sớm 51 ngày trước hạn bảo lãnh"
                    }
                }
            };

            var sheet2 = new FnExpCalcSheet
            {
                OrgId = orgId,
                CaNo = "260322-001/CPTC/VN012",
                DealerCode = "VN012",
                DealerName = "Hyundai Hà Đông",
                TermFrom = new DateTime(2026, 3, 1),
                TermTo = new DateTime(2026, 3, 31),
                TermPrevFrom = new DateTime(2026, 2, 1),
                TermPrevTo = new DateTime(2026, 2, 28),
                FnExpPercent = 5.5m,
                PmtDsTCGPercent = 2.0m,
                CAName = "Bảng tính CPTC & CKTT kỳ 03/2026 - Hyundai Hà Đông",
                FlagEarlyCancel = "0",
                FlagisHTC = "HTC",
                DlrSignStatus = FnExpSignStatus.Approved1,
                HTCSignStatus = FnExpSignStatus.Approved1,
                Status = FnExpStatus.NotSign,
                TotalCars = 2,
                TotalFnDepositAmount = 850000m,
                TotalFnGrtAmount = 0,
                TotalFnAmount = 850000m,
                TotalPDAmount = 6500000m,
                NetSettlementAmount = 5650000m,
                DlrAppr1At = new DateTime(2026, 3, 22, 11, 0, 0),
                DlrAppr1By = "VN012_ACCOUNTANT",
                HTCAppr1At = new DateTime(2026, 3, 22, 15, 30, 0),
                HTCAppr1By = "HTC_FIN_SPECIALIST",
                Remark = "Đại lý và HTC đã đối soát bước 1, chờ Lãnh đạo HTC phê duyệt cấp 2",
                CreatedAt = new DateTime(2026, 3, 22, 9, 0, 0),
                CreatedBy = "HTC_FIN_SPECIALIST",
                Details = new List<FnExpCalcSheetDetail>
                {
                    new()
                    {
                        CaNo = "260322-001/CPTC/VN012",
                        CarId = "2603C201-SANTAFE",
                        Vin = "KMHCT41BPST030201",
                        ModelCode = "SANTAFE",
                        ModelName = "Hyundai Santa Fe 2.5 All New Cao Cấp",
                        UnitPriceActual = 1369000000m,
                        SodApprovedDate = new DateTime(2026, 2, 18),
                        SodDepositDutyEndDate = new DateTime(2026, 2, 25),
                        DateStart = new DateTime(2026, 2, 28),
                        DateEnd = new DateTime(2026, 4, 28),
                        TotalCompletedDate = new DateTime(2026, 3, 12),
                        TermActual = 60,
                        FnDepositCountDate = 12,
                        FnDepositAmount = 850000m,
                        FnGrtCountDate = 0,
                        FnGrtAmount = 0,
                        FnTotalAmount = 850000m,
                        PDCountDate = 47,
                        PDAmount = 3574722m,
                        Status = FnExpStatus.NotSign,
                        Remark = "Thanh toán sớm 47 ngày"
                    },
                    new()
                    {
                        CaNo = "260322-001/CPTC/VN012",
                        CarId = "2603C202-CRETA",
                        Vin = "KMHCT41BPST030202",
                        ModelCode = "CRETA",
                        ModelName = "Hyundai Creta B-SUV 1.5 Tiêu Chuẩn",
                        UnitPriceActual = 599000000m,
                        SodApprovedDate = new DateTime(2026, 2, 20),
                        SodDepositDutyEndDate = new DateTime(2026, 2, 27),
                        DateStart = new DateTime(2026, 3, 2),
                        DateEnd = new DateTime(2026, 5, 2),
                        TotalCompletedDate = new DateTime(2026, 3, 18),
                        TermActual = 60,
                        FnDepositCountDate = 0,
                        FnDepositAmount = 0,
                        FnGrtCountDate = 0,
                        FnGrtAmount = 0,
                        FnTotalAmount = 0,
                        PDCountDate = 45,
                        PDAmount = 2925278m,
                        Status = FnExpStatus.NotSign,
                        Remark = "Thanh toán sớm 45 ngày"
                    }
                }
            };

            var sheet3 = new FnExpCalcSheet
            {
                OrgId = orgId,
                CaNo = "260323-001/CPTC/VN065",
                DealerCode = "VN065",
                DealerName = "Hyundai Phạm Văn Đồng",
                TermFrom = new DateTime(2026, 3, 1),
                TermTo = new DateTime(2026, 3, 31),
                TermPrevFrom = new DateTime(2026, 2, 1),
                TermPrevTo = new DateTime(2026, 2, 28),
                FnExpPercent = 5.5m,
                PmtDsTCGPercent = 2.0m,
                CAName = "Bảng tính CPTC & CKTT kỳ 03/2026 - Hyundai Phạm Văn Đồng",
                FlagEarlyCancel = "0",
                FlagisHTC = "HTC",
                DlrSignStatus = FnExpSignStatus.Pending,
                HTCSignStatus = FnExpSignStatus.Pending,
                Status = FnExpStatus.NotSign,
                TotalCars = 1,
                TotalFnDepositAmount = 350000m,
                TotalFnGrtAmount = 0,
                TotalFnAmount = 350000m,
                TotalPDAmount = 2350000m,
                NetSettlementAmount = 2000000m,
                Remark = "Bảng tính mới tạo, gửi đại lý đối soát số liệu",
                CreatedAt = new DateTime(2026, 3, 23, 14, 0, 0),
                CreatedBy = "HTC_FIN_SPECIALIST",
                Details = new List<FnExpCalcSheetDetail>
                {
                    new()
                    {
                        CaNo = "260323-001/CPTC/VN065",
                        CarId = "2603C301-ELANTRA",
                        Vin = "KMHCT41BPST030301",
                        ModelCode = "ELANTRA",
                        ModelName = "Hyundai Elantra N-Line Turbo",
                        UnitPriceActual = 769000000m,
                        SodApprovedDate = new DateTime(2026, 2, 25),
                        SodDepositDutyEndDate = new DateTime(2026, 3, 4),
                        DateStart = new DateTime(2026, 3, 5),
                        DateEnd = new DateTime(2026, 5, 5),
                        TotalCompletedDate = new DateTime(2026, 3, 20),
                        TermActual = 60,
                        FnDepositCountDate = 16,
                        FnDepositAmount = 350000m,
                        FnGrtCountDate = 0,
                        FnGrtAmount = 0,
                        FnTotalAmount = 350000m,
                        PDCountDate = 46,
                        PDAmount = 2350000m,
                        Status = FnExpStatus.NotSign,
                        Remark = "Chờ đại lý xác nhận đối soát"
                    }
                }
            };

            db.FnExpCalcSheets.AddRange(sheet1, sheet2, sheet3);
            await db.SaveChangesAsync();
        }

        if (!await db.SalesPolicies.AnyAsync())
        {
            var policy1 = new SalesPolicyMaster
            {
                OrgId = orgId,
                SPSRCode = "SPSR26030001",
                SPNo = "15/2026/QĐ-HTV-CSP",
                StartDate = new DateTime(2026, 3, 1),
                EndDate = new DateTime(2026, 3, 31),
                SPSRType = SalesPolicyType.Retail,
                FormBusinessSupportCode = BusinessSupportForm.Cash,
                FlagMstValid = "1",
                Status = SalesPolicyStatus.Valid,
                Remark = "Chương trình hỗ trợ kích cầu bán lẻ xe Hyundai tháng 03/2026",
                ApprovedAt = new DateTime(2026, 3, 1, 8, 30, 0),
                ApprovedBy = "LEADER_HTC",
                CreatedAt = new DateTime(2026, 3, 1, 8, 0, 0),
                CreatedBy = "HTC_SALES_SPECIALIST",
                Details = new List<SalesPolicyDetail>
                {
                    new() { SPSRCode = "SPSR26030001", DealerCode = "VS058", ModelCode = "SANTAFE", ModelName = "Santa Fe 2.5 HTRAC", SpecCode = "SF25-PREM", SpecDescription = "Bản cao cấp máy xăng 2.5 HTRAC", YearOfManufacture = "2026", AmountSupport = 25000000m, MinQty = 1, Remark = "Hỗ trợ 25 triệu/xe bán lẻ" },
                    new() { SPSRCode = "SPSR26030001", DealerCode = "VS058", ModelCode = "CRETA", ModelName = "Creta 1.5 Smart", SpecCode = "CR15-SMT", SpecDescription = "Bản thông minh 1.5 CVT", YearOfManufacture = "2026", AmountSupport = 15000000m, MinQty = 1, Remark = "Hỗ trợ 15 triệu/xe bán lẻ" },
                    new() { SPSRCode = "SPSR26030001", DealerCode = "VN012", ModelCode = "TUCSON", ModelName = "Tucson 2.0 AT Dầu", SpecCode = "TU20-DSL", SpecDescription = "Bản máy dầu 2.0 AT Đặc biệt", YearOfManufacture = "2026", AmountSupport = 20000000m, MinQty = 1, Remark = "Hỗ trợ 20 triệu/xe bán lẻ" },
                    new() { SPSRCode = "SPSR26030001", DealerCode = "VN001", ModelCode = "ACCENT", ModelName = "Accent 1.5 AT", SpecCode = "AC15-AT", SpecDescription = "Bản 1.5 AT Đặc biệt 2026", YearOfManufacture = "2026", AmountSupport = 10000000m, MinQty = 1, Remark = "Hỗ trợ 10 triệu/xe bán lẻ" },
                    new() { SPSRCode = "SPSR26030001", DealerCode = "ALL", ModelCode = "STARGAZER", ModelName = "Stargazer X", SpecCode = "SG-X", SpecDescription = "MPV 7 chỗ gầm cao", YearOfManufacture = "2026", AmountSupport = 18000000m, MinQty = 1, Remark = "Hỗ trợ toàn quốc 18 triệu/xe" }
                }
            };

            var policy2 = new SalesPolicyMaster
            {
                OrgId = orgId,
                SPSRCode = "SPSR26020002",
                SPNo = "08/2026/QĐ-HTV-TB",
                StartDate = new DateTime(2026, 2, 1),
                EndDate = new DateTime(2026, 4, 30),
                SPSRType = SalesPolicyType.Campaign,
                FormBusinessSupportCode = BusinessSupportForm.RegistrationFee,
                FlagMstValid = "1",
                Status = SalesPolicyStatus.Valid,
                Remark = "Chính sách hỗ trợ 50-100% lệ phí trước bạ xe ô tô Hyundai gầm cao Quý 1/2026",
                ApprovedAt = new DateTime(2026, 2, 1, 9, 0, 0),
                ApprovedBy = "LEADER_HTC",
                CreatedAt = new DateTime(2026, 2, 1, 8, 30, 0),
                CreatedBy = "HTC_SALES_SPECIALIST",
                Details = new List<SalesPolicyDetail>
                {
                    new() { SPSRCode = "SPSR26020002", DealerCode = "ALL", ModelCode = "SANTAFE", ModelName = "Santa Fe All-New", SpecCode = "SF-TURBO", SpecDescription = "Bản 1.6 Turbo Hybrid", YearOfManufacture = "2026", AmountSupport = 50000000m, MinQty = 1, Remark = "Hỗ trợ 50% trước bạ tương đương 50 triệu" },
                    new() { SPSRCode = "SPSR26020002", DealerCode = "ALL", ModelCode = "TUCSON", ModelName = "Tucson Turbo HTRAC", SpecCode = "TU-TURBO", SpecDescription = "Bản 1.6 Turbo", YearOfManufacture = "2026", AmountSupport = 40000000m, MinQty = 1, Remark = "Hỗ trợ 50% trước bạ tương đương 40 triệu" }
                }
            };

            var policy3 = new SalesPolicyMaster
            {
                OrgId = orgId,
                SPSRCode = "SPSR26010003",
                SPNo = "02/2026/QĐ-HTV-PK",
                StartDate = new DateTime(2026, 1, 1),
                EndDate = new DateTime(2026, 6, 30),
                SPSRType = SalesPolicyType.Wholesale,
                FormBusinessSupportCode = BusinessSupportForm.Accessory,
                FlagMstValid = "1",
                Status = SalesPolicyStatus.Valid,
                Remark = "Chính sách tặng gói phụ kiện cao cấp chính hãng Hyundai Accent 2026",
                ApprovedAt = new DateTime(2026, 1, 2, 9, 30, 0),
                ApprovedBy = "LEADER_HTC",
                CreatedAt = new DateTime(2026, 1, 2, 8, 45, 0),
                CreatedBy = "HTC_SALES_SPECIALIST",
                Details = new List<SalesPolicyDetail>
                {
                    new() { SPSRCode = "SPSR26010003", DealerCode = "ALL", ModelCode = "ACCENT", ModelName = "Accent All-New", SpecCode = "AC15-PREM", SpecDescription = "Bản cao cấp 1.5 CVT", YearOfManufacture = "2026", AmountSupport = 12000000m, MinQty = 1, Remark = "Gói dán phim, camera, lót sàn chính hãng" }
                }
            };

            db.SalesPolicies.AddRange(policy1, policy2, policy3);
            await db.SaveChangesAsync();

            // Seed hồ sơ hỗ trợ bán lẻ SupportRetails
            var sup1 = new SupportRetail
            {
                OrgId = orgId,
                SupportCode = "2603SPR00001",
                SPSRCode = "SPSR26030001",
                SPNo = "15/2026/QĐ-HTV-CSP",
                DealerCode = "VS058",
                DealerName = "Hyundai Bình Dương",
                Vin = "RLUGT41DBST012693",
                CarId = "CAR2026-SF0988",
                ModelCode = "SANTAFE",
                ModelName = "Santa Fe 2.5 HTRAC",
                AmountSupport = 25000000m,
                AmountHTCAppr = 25000000m,
                DateSupport = new DateTime(2026, 3, 1),
                HTCDatePayment = new DateTime(2026, 3, 20),
                Status = SupportRetailStatus.Paid,
                Remark = "Đã thẩm tra đủ hồ sơ và thanh toán giải ngân chi trả đại lý qua VCB",
                OSODSOCode = "SO2603010001",
                OSODApprovedDate = new DateTime(2026, 2, 25),
                HTCInvoiceNo = "0001245",
                HTCInvoiceDate = new DateTime(2026, 3, 5),
                CDODDeliveryOutDate = new DateTime(2026, 3, 10),
                DateFullStatus = new DateTime(2026, 3, 12),
                ApprovedAt = new DateTime(2026, 3, 15, 14, 0, 0),
                ApprovedBy = "HTC_SALES_MANAGER",
                CreatedAt = new DateTime(2026, 3, 12, 10, 0, 0),
                CreatedBy = "DEALER_STAFF"
            };

            var sup2 = new SupportRetail
            {
                OrgId = orgId,
                SupportCode = "2603SPR00002",
                SPSRCode = "SPSR26030001",
                SPNo = "15/2026/QĐ-HTV-CSP",
                DealerCode = "VS058",
                DealerName = "Hyundai Bình Dương",
                Vin = "KMHE281BBSA129841",
                CarId = "CAR2026-CR0122",
                ModelCode = "CRETA",
                ModelName = "Creta 1.5 Smart",
                AmountSupport = 15000000m,
                AmountHTCAppr = 15000000m,
                DateSupport = new DateTime(2026, 3, 1),
                Status = SupportRetailStatus.Approved,
                Remark = "Hồ sơ đạt chuẩn đối soát hoàn tất nghĩa vụ bán lẻ, chờ kế toán chuyển tiền",
                OSODSOCode = "SO2603010002",
                OSODApprovedDate = new DateTime(2026, 2, 28),
                HTCInvoiceNo = "0001248",
                HTCInvoiceDate = new DateTime(2026, 3, 8),
                CDODDeliveryOutDate = new DateTime(2026, 3, 14),
                DateFullStatus = new DateTime(2026, 3, 15),
                ApprovedAt = new DateTime(2026, 3, 18, 16, 30, 0),
                ApprovedBy = "HTC_SALES_MANAGER",
                CreatedAt = new DateTime(2026, 3, 15, 9, 30, 0),
                CreatedBy = "DEALER_STAFF"
            };

            var sup3 = new SupportRetail
            {
                OrgId = orgId,
                SupportCode = "2603SPR00003",
                SPSRCode = "SPSR26030001",
                SPNo = "15/2026/QĐ-HTV-CSP",
                DealerCode = "VN012",
                DealerName = "Hyundai Hà Đông",
                Vin = "KMHCT81CBDU048215",
                CarId = "CAR2026-TU0056",
                ModelCode = "TUCSON",
                ModelName = "Tucson 2.0 AT Dầu",
                AmountSupport = 20000000m,
                AmountHTCAppr = 0,
                DateSupport = new DateTime(2026, 3, 1),
                Status = SupportRetailStatus.Pending,
                Remark = "Đại lý nộp hồ sơ đối soát bán lẻ trong tháng",
                OSODSOCode = "SO2603010003",
                OSODApprovedDate = new DateTime(2026, 3, 2),
                HTCInvoiceNo = "0001252",
                HTCInvoiceDate = new DateTime(2026, 3, 10),
                CDODDeliveryOutDate = new DateTime(2026, 3, 18),
                DateFullStatus = new DateTime(2026, 3, 20),
                CreatedAt = new DateTime(2026, 3, 21, 11, 0, 0),
                CreatedBy = "DEALER_STAFF"
            };

            var sup4 = new SupportRetail
            {
                OrgId = orgId,
                SupportCode = "2603SPR00004",
                SPSRCode = "SPSR26030001",
                SPNo = "15/2026/QĐ-HTV-CSP",
                DealerCode = "VN001",
                DealerName = "Hyundai Ngọc An",
                Vin = "KMHD351ABLU837201",
                CarId = "CAR2026-AC0341",
                ModelCode = "ACCENT",
                ModelName = "Accent 1.5 AT",
                AmountSupport = 10000000m,
                AmountHTCAppr = 0,
                DateSupport = new DateTime(2026, 3, 1),
                Status = SupportRetailStatus.Rejected,
                Remark = "Đề nghị hỗ trợ bán lẻ bị từ chối",
                RejectReason = "Hợp đồng bán lẻ bị hủy do khách hàng không được duyệt vay tín dụng",
                RejectedAt = new DateTime(2026, 3, 22, 10, 15, 0),
                RejectedBy = "HTC_SALES_MANAGER",
                CreatedAt = new DateTime(2026, 3, 20, 15, 45, 0),
                CreatedBy = "DEALER_STAFF"
            };

            db.SupportRetails.AddRange(sup1, sup2, sup3, sup4);
            await db.SaveChangesAsync();
        }

        if (!await db.PlanEstimateOrders.AnyAsync())
        {
            var ple1 = new PlanEstimateOrder
            {
                OrgId = orgId,
                PLEOrdNo = "2603PLE00001",
                DealerCode = "VS058",
                DealerName = "Hyundai Bình Dương",
                MonthEstimate = "2026-04",
                BusinessPlanCode = "2603BPL0001",
                AreaRootCode = "MN",
                Status = PlanEstimateOrderStatus.Approved2,
                TotalQtyN1 = 48,
                TotalSellCusN0 = 42,
                TotalSellCusN1 = 46,
                TotalSellCusN2 = 46,
                TotalSellCusN3 = 50,
                Remark = "Kế hoạch dự kiến đặt hàng tháng 04/2026 - đã được Lãnh đạo Khối duyệt chuyển nhà máy HTMV",
                CreatedBy = "VS058_PLAN_LEAD",
                CreatedAt = new DateTime(2026, 3, 20, 9, 0, 0),
                Approve1By = "HTC_SALES_LEADER",
                Approve1At = new DateTime(2026, 3, 21, 10, 30, 0),
                Approve2By = "HTC_DIV_DIRECTOR",
                Approve2At = new DateTime(2026, 3, 22, 14, 0, 0),
                LogLUDateTime = new DateTime(2026, 3, 22, 14, 0, 0),
                LogLUBy = "HTC_DIV_DIRECTOR",
                Details = new List<PlanEstimateOrderDetail>
                {
                    new()
                    {
                        PLEOrdNo = "2603PLE00001",
                        ModelCode = "SANTAFE",
                        ModelName = "Hyundai Santa Fe All-New",
                        SpecCode = "SF25-PREM",
                        SpecDescription = "Santa Fe 2.5 HTRAC Xăng Cao cấp",
                        QtySellCustomer = 5,
                        QtyInStock = 4,
                        QtyOnWay = 2,
                        QtyBuyDealer = 0,
                        QtyBODangMapVIN = 1,
                        QtyBOChuaXuatKho = 2,
                        TongHangCo = 7,
                        TiLeHangCo = 31.8m,
                        QtyESellCusN0 = 10,
                        QtyESellCusN1 = 12,
                        QtyESellCusN2 = 12,
                        QtyESellCusN3 = 14,
                        QtyEOrdN1 = 14,
                        TiLeDatTN1 = 116.7m,
                        DuPhongTonKho = -1,
                        TiLeDuPhongTonKho = -4.5m,
                        Remark = "Ưu tiên phân bổ bản màu trắng ngọc trai"
                    },
                    new()
                    {
                        PLEOrdNo = "2603PLE00001",
                        ModelCode = "TUCSON",
                        ModelName = "Hyundai Tucson Turbo",
                        SpecCode = "TU20-TRB",
                        SpecDescription = "Tucson 1.6T HTRAC Turbo",
                        QtySellCustomer = 4,
                        QtyInStock = 3,
                        QtyOnWay = 1,
                        QtyBuyDealer = 0,
                        QtyBODangMapVIN = 1,
                        QtyBOChuaXuatKho = 1,
                        TongHangCo = 5,
                        TiLeHangCo = 25.0m,
                        QtyESellCusN0 = 9,
                        QtyESellCusN1 = 11,
                        QtyESellCusN2 = 11,
                        QtyESellCusN3 = 12,
                        QtyEOrdN1 = 12,
                        TiLeDatTN1 = 109.1m,
                        DuPhongTonKho = -3,
                        TiLeDuPhongTonKho = -15.0m,
                        Remark = "Khách đặt chờ nhiều dòng máy dầu và turbo"
                    },
                    new()
                    {
                        PLEOrdNo = "2603PLE00001",
                        ModelCode = "CRETA",
                        ModelName = "Hyundai Creta Smart",
                        SpecCode = "CR15-SMT",
                        SpecDescription = "Creta 1.5 Smart CVT",
                        QtySellCustomer = 6,
                        QtyInStock = 5,
                        QtyOnWay = 2,
                        QtyBuyDealer = 0,
                        QtyBODangMapVIN = 2,
                        QtyBOChuaXuatKho = 2,
                        TongHangCo = 9,
                        TiLeHangCo = 40.9m,
                        QtyESellCusN0 = 11,
                        QtyESellCusN1 = 11,
                        QtyESellCusN2 = 11,
                        QtyESellCusN3 = 12,
                        QtyEOrdN1 = 11,
                        TiLeDatTN1 = 100.0m,
                        DuPhongTonKho = -2,
                        TiLeDuPhongTonKho = -9.1m,
                        Remark = "Dự kiến giao cho khách mua cá nhân"
                    },
                    new()
                    {
                        PLEOrdNo = "2603PLE00001",
                        ModelCode = "ACCENT",
                        ModelName = "Hyundai Accent Sedan",
                        SpecCode = "AC15-AT",
                        SpecDescription = "Accent 1.5 AT Đặc biệt",
                        QtySellCustomer = 6,
                        QtyInStock = 5,
                        QtyOnWay = 2,
                        QtyBuyDealer = 0,
                        QtyBODangMapVIN = 1,
                        QtyBOChuaXuatKho = 2,
                        TongHangCo = 8,
                        TiLeHangCo = 33.3m,
                        QtyESellCusN0 = 12,
                        QtyESellCusN1 = 12,
                        QtyESellCusN2 = 12,
                        QtyESellCusN3 = 12,
                        QtyEOrdN1 = 11,
                        TiLeDatTN1 = 91.7m,
                        DuPhongTonKho = -5,
                        TiLeDuPhongTonKho = -20.8m,
                        Remark = "Dự kiến nhu cầu bán lô taxi và kinh doanh"
                    }
                }
            };

            var ple2 = new PlanEstimateOrder
            {
                OrgId = orgId,
                PLEOrdNo = "2603PLE00002",
                DealerCode = "VN001",
                DealerName = "Hyundai Đông Đô",
                MonthEstimate = "2026-04",
                BusinessPlanCode = "2603BPL0002",
                AreaRootCode = "MB",
                Status = PlanEstimateOrderStatus.Approved1,
                TotalQtyN1 = 36,
                TotalSellCusN0 = 34,
                TotalSellCusN1 = 38,
                TotalSellCusN2 = 38,
                TotalSellCusN3 = 40,
                Remark = "Kế hoạch ước tính tháng 04/2026 - Trưởng phòng bán hàng NPP đã thẩm duyệt cấp 1",
                CreatedBy = "VN001_PLAN_STAFF",
                CreatedAt = new DateTime(2026, 3, 21, 14, 0, 0),
                Approve1By = "HTC_SALES_LEADER",
                Approve1At = new DateTime(2026, 3, 22, 16, 0, 0),
                LogLUDateTime = new DateTime(2026, 3, 22, 16, 0, 0),
                LogLUBy = "HTC_SALES_LEADER",
                Details = new List<PlanEstimateOrderDetail>
                {
                    new()
                    {
                        PLEOrdNo = "2603PLE00002",
                        ModelCode = "SANTAFE",
                        ModelName = "Hyundai Santa Fe All-New",
                        SpecCode = "SF25-PREM",
                        SpecDescription = "Santa Fe 2.5 HTRAC Xăng Cao cấp",
                        QtySellCustomer = 4,
                        QtyInStock = 3,
                        QtyOnWay = 1,
                        QtyBODangMapVIN = 1,
                        TongHangCo = 5,
                        TiLeHangCo = 26.3m,
                        QtyESellCusN0 = 9,
                        QtyESellCusN1 = 10,
                        QtyESellCusN2 = 10,
                        QtyESellCusN3 = 11,
                        QtyEOrdN1 = 10,
                        TiLeDatTN1 = 100.0m,
                        DuPhongTonKho = -4,
                        TiLeDuPhongTonKho = -21.1m,
                        Remark = "Nhu cầu cao thị trường miền Bắc"
                    },
                    new()
                    {
                        PLEOrdNo = "2603PLE00002",
                        ModelCode = "CRETA",
                        ModelName = "Hyundai Creta Smart",
                        SpecCode = "CR15-SMT",
                        SpecDescription = "Creta 1.5 Smart CVT",
                        QtySellCustomer = 5,
                        QtyInStock = 4,
                        QtyOnWay = 1,
                        QtyBODangMapVIN = 1,
                        TongHangCo = 6,
                        TiLeHangCo = 31.6m,
                        QtyESellCusN0 = 9,
                        QtyESellCusN1 = 10,
                        QtyESellCusN2 = 10,
                        QtyESellCusN3 = 11,
                        QtyEOrdN1 = 10,
                        TiLeDatTN1 = 100.0m,
                        DuPhongTonKho = -3,
                        TiLeDuPhongTonKho = -15.8m,
                        Remark = "Màu đỏ mận và trắng"
                    },
                    new()
                    {
                        PLEOrdNo = "2603PLE00002",
                        ModelCode = "ACCENT",
                        ModelName = "Hyundai Accent Sedan",
                        SpecCode = "AC15-AT",
                        SpecDescription = "Accent 1.5 AT Đặc biệt",
                        QtySellCustomer = 7,
                        QtyInStock = 6,
                        QtyOnWay = 2,
                        QtyBODangMapVIN = 1,
                        TongHangCo = 9,
                        TiLeHangCo = 32.1m,
                        QtyESellCusN0 = 13,
                        QtyESellCusN1 = 15,
                        QtyESellCusN2 = 15,
                        QtyESellCusN3 = 15,
                        QtyEOrdN1 = 13,
                        TiLeDatTN1 = 86.7m,
                        DuPhongTonKho = -6,
                        TiLeDuPhongTonKho = -21.4m,
                        Remark = "Số lượng đặt buôn theo chỉ tiêu tháng"
                    },
                    new()
                    {
                        PLEOrdNo = "2603PLE00002",
                        ModelCode = "CUSTIN",
                        ModelName = "Hyundai Custin MPV",
                        SpecCode = "CU15-PRE",
                        SpecDescription = "Custin 1.5T-GDi Cao Cấp",
                        QtySellCustomer = 2,
                        QtyInStock = 1,
                        QtyOnWay = 1,
                        QtyBODangMapVIN = 0,
                        TongHangCo = 2,
                        TiLeHangCo = 28.6m,
                        QtyESellCusN0 = 3,
                        QtyESellCusN1 = 4,
                        QtyESellCusN2 = 4,
                        QtyESellCusN3 = 4,
                        QtyEOrdN1 = 3,
                        TiLeDatTN1 = 75.0m,
                        DuPhongTonKho = -2,
                        TiLeDuPhongTonKho = -28.6m,
                        Remark = "Phục vụ đơn khách hàng doanh nghiệp"
                    }
                }
            };

            var ple3 = new PlanEstimateOrder
            {
                OrgId = orgId,
                PLEOrdNo = "2603PLE00003",
                DealerCode = "VN012",
                DealerName = "Hyundai Hà Đông",
                MonthEstimate = "2026-04",
                BusinessPlanCode = null,
                AreaRootCode = "MB",
                Status = PlanEstimateOrderStatus.Pending,
                TotalQtyN1 = 30,
                TotalSellCusN0 = 28,
                TotalSellCusN1 = 30,
                TotalSellCusN2 = 32,
                TotalSellCusN3 = 34,
                Remark = "Đại lý Hà Đông lập dự kiến đặt hàng tháng 04/2026, chờ NPP xem xét thẩm tra",
                CreatedBy = "VN012_PLAN_STAFF",
                CreatedAt = new DateTime(2026, 3, 23, 11, 0, 0),
                Details = new List<PlanEstimateOrderDetail>
                {
                    new()
                    {
                        PLEOrdNo = "2603PLE00003",
                        ModelCode = "SANTAFE",
                        ModelName = "Hyundai Santa Fe All-New",
                        SpecCode = "SF25-PREM",
                        SpecDescription = "Santa Fe 2.5 HTRAC Xăng Cao cấp",
                        QtySellCustomer = 3,
                        QtyInStock = 2,
                        QtyOnWay = 1,
                        QtyBODangMapVIN = 1,
                        TongHangCo = 4,
                        TiLeHangCo = 26.7m,
                        QtyESellCusN0 = 7,
                        QtyESellCusN1 = 8,
                        QtyESellCusN2 = 8,
                        QtyESellCusN3 = 9,
                        QtyEOrdN1 = 8,
                        TiLeDatTN1 = 100.0m,
                        DuPhongTonKho = -3,
                        TiLeDuPhongTonKho = -20.0m
                    },
                    new()
                    {
                        PLEOrdNo = "2603PLE00003",
                        ModelCode = "TUCSON",
                        ModelName = "Hyundai Tucson Turbo",
                        SpecCode = "TU20-TRB",
                        SpecDescription = "Tucson 1.6T HTRAC Turbo",
                        QtySellCustomer = 3,
                        QtyInStock = 2,
                        QtyOnWay = 1,
                        QtyBODangMapVIN = 0,
                        TongHangCo = 3,
                        TiLeHangCo = 21.4m,
                        QtyESellCusN0 = 6,
                        QtyESellCusN1 = 8,
                        QtyESellCusN2 = 8,
                        QtyESellCusN3 = 8,
                        QtyEOrdN1 = 8,
                        TiLeDatTN1 = 100.0m,
                        DuPhongTonKho = -3,
                        TiLeDuPhongTonKho = -21.4m
                    },
                    new()
                    {
                        PLEOrdNo = "2603PLE00003",
                        ModelCode = "CRETA",
                        ModelName = "Hyundai Creta Smart",
                        SpecCode = "CR15-SMT",
                        SpecDescription = "Creta 1.5 Smart CVT",
                        QtySellCustomer = 4,
                        QtyInStock = 3,
                        QtyOnWay = 1,
                        QtyBODangMapVIN = 1,
                        TongHangCo = 5,
                        TiLeHangCo = 35.7m,
                        QtyESellCusN0 = 7,
                        QtyESellCusN1 = 7,
                        QtyESellCusN2 = 8,
                        QtyESellCusN3 = 8,
                        QtyEOrdN1 = 7,
                        TiLeDatTN1 = 100.0m,
                        DuPhongTonKho = -2,
                        TiLeDuPhongTonKho = -14.3m
                    },
                    new()
                    {
                        PLEOrdNo = "2603PLE00003",
                        ModelCode = "ACCENT",
                        ModelName = "Hyundai Accent Sedan",
                        SpecCode = "AC15-AT",
                        SpecDescription = "Accent 1.5 AT Đặc biệt",
                        QtySellCustomer = 4,
                        QtyInStock = 4,
                        QtyOnWay = 1,
                        QtyBODangMapVIN = 1,
                        TongHangCo = 6,
                        TiLeHangCo = 40.0m,
                        QtyESellCusN0 = 8,
                        QtyESellCusN1 = 7,
                        QtyESellCusN2 = 8,
                        QtyESellCusN3 = 9,
                        QtyEOrdN1 = 7,
                        TiLeDatTN1 = 100.0m,
                        DuPhongTonKho = -2,
                        TiLeDuPhongTonKho = -13.3m
                    }
                }
            };

            var ple4 = new PlanEstimateOrder
            {
                OrgId = orgId,
                PLEOrdNo = "2603PLE00004",
                DealerCode = "VN002",
                DealerName = "Hyundai Nam Trung",
                MonthEstimate = "2026-04",
                BusinessPlanCode = null,
                AreaRootCode = "MB",
                Status = PlanEstimateOrderStatus.Cancelled,
                TotalQtyN1 = 20,
                TotalSellCusN0 = 18,
                TotalSellCusN1 = 20,
                TotalSellCusN2 = 20,
                TotalSellCusN3 = 22,
                Remark = "Kế hoạch đã hủy do đại lý tái cơ cấu hạn mức tín dụng",
                CancelReason = "Đại lý xin hủy để gom chung với đơn hàng SOU đột xuất quý 2",
                CreatedBy = "VN002_STAFF",
                CreatedAt = new DateTime(2026, 3, 19, 10, 0, 0),
                CancelledBy = "HTC_SALES_LEADER",
                CancelledAt = new DateTime(2026, 3, 20, 15, 0, 0),
                LogLUDateTime = new DateTime(2026, 3, 20, 15, 0, 0),
                LogLUBy = "HTC_SALES_LEADER",
                Details = new List<PlanEstimateOrderDetail>
                {
                    new()
                    {
                        PLEOrdNo = "2603PLE00004",
                        ModelCode = "SANTAFE",
                        ModelName = "Hyundai Santa Fe All-New",
                        SpecCode = "SF25-PREM",
                        SpecDescription = "Santa Fe 2.5 HTRAC Xăng Cao cấp",
                        QtySellCustomer = 2,
                        QtyInStock = 2,
                        TongHangCo = 2,
                        QtyESellCusN0 = 8,
                        QtyESellCusN1 = 10,
                        QtyEOrdN1 = 10
                    },
                    new()
                    {
                        PLEOrdNo = "2603PLE00004",
                        ModelCode = "ACCENT",
                        ModelName = "Hyundai Accent Sedan",
                        SpecCode = "AC15-AT",
                        SpecDescription = "Accent 1.5 AT Đặc biệt",
                        QtySellCustomer = 3,
                        QtyInStock = 3,
                        TongHangCo = 3,
                        QtyESellCusN0 = 10,
                        QtyESellCusN1 = 10,
                        QtyEOrdN1 = 10
                    }
                }
            };

            db.PlanEstimateOrders.AddRange(ple1, ple2, ple3, ple4);
            await db.SaveChangesAsync();
        }

        if (!await db.PackingLists.AnyAsync(o => o.OrgId == orgId))
        {
            var pl1 = new PackingList
            {
                OrgId = orgId,
                PackingListNo = "2603PL0001",
                ContractNo = "HMC-2026-VN01",
                LCNo = "LC2601001",
                PortCode = "CANG_CAT_LAI",
                PortName = "Cảng Cát Lái (TP. Hồ Chí Minh)",
                PLType = PackingListType.CBU,
                Status = PackingListStatus.Completed,
                ShippingDateStart = DateTime.Today.AddDays(-22),
                ShippingDateEndExpected = DateTime.Today.AddDays(-12),
                ShippingDateEnd = DateTime.Today.AddDays(-11),
                VesselName = "GLOVIS COURAGE",
                VoyageNo = "V2603S",
                TotalCars = 3,
                TotalRepaired = 1,
                Remark = "Lô xe nguyên chiếc CBU nhập khẩu từ Ulsan HMC cập cảng Cát Lái, đã thông quan và nghiệm thu nhập kho an toàn",
                CreatedBy = "IMPORT_HQ_SPECIALIST",
                CreatedAt = DateTime.Today.AddDays(-25),
                LUDateTime = DateTime.Today.AddDays(-10),
                LUBy = "WAREHOUSE_MANAGER_SG",
                Details = new List<PackingListDetail>
                {
                    new()
                    {
                        PackingListNo = "2603PL0001",
                        Vin = "KMHE281BBSB200001",
                        EngineNo = "G4KP-200001",
                        KeyNo = "K2001",
                        ModelCode = "SANTAFE",
                        ModelName = "Hyundai Santa Fe All-New",
                        SpecCode = "SF25-PRE-01",
                        SpecDescription = "Santa Fe 2.5 HTRAC Xăng Cao Cấp nhập khẩu",
                        ColorCode = "WW2",
                        ColorName = "Trắng ngọc trai",
                        WorkOrderNo = "WO-HMC-202602-011",
                        ProductionMonth = "2026-02",
                        FlagRepair = false,
                        StorageCodeCurrent = "KHO_TONG_SG",
                        StoreDate = DateTime.Today.AddDays(-10),
                        Status = PackingListDetailStatus.StockIn,
                        Remark = "Xe hoàn hảo nguyên trạng"
                    },
                    new()
                    {
                        PackingListNo = "2603PL0001",
                        Vin = "KMHE281BBSB200002",
                        EngineNo = "D4HB-200002",
                        KeyNo = "K2002",
                        ModelCode = "PALISADE",
                        ModelName = "Hyundai Palisade Exclusive",
                        SpecCode = "PL22-EXC-01",
                        SpecDescription = "Palisade 2.2D Exclusive 7 chỗ CBU",
                        ColorCode = "BK1",
                        ColorName = "Đen Ánh Kim",
                        WorkOrderNo = "WO-HMC-202602-012",
                        ProductionMonth = "2026-02",
                        FlagRepair = true,
                        RepairRemark = "Xước nhẹ lớp màng bảo vệ cản sau do cọ xát dây chằng trên boong tàu, đã xử lý sơn bóng đạt chuẩn KCS xuất xưởng",
                        StorageCodeCurrent = "KHO_TONG_SG",
                        StoreDate = DateTime.Today.AddDays(-10),
                        Status = PackingListDetailStatus.StockIn,
                        Remark = "Đã sửa chữa và nghiệm thu đạt KCS"
                    },
                    new()
                    {
                        PackingListNo = "2603PL0001",
                        Vin = "KMHE281BBSB200003",
                        EngineNo = "G4KP-200003",
                        KeyNo = "K2003",
                        ModelCode = "SANTAFE",
                        ModelName = "Hyundai Santa Fe All-New",
                        SpecCode = "SF25-PRE-01",
                        SpecDescription = "Santa Fe 2.5 HTRAC Xăng Cao Cấp nhập khẩu",
                        ColorCode = "MB1",
                        ColorName = "Xanh ánh kim",
                        WorkOrderNo = "WO-HMC-202602-013",
                        ProductionMonth = "2026-02",
                        FlagRepair = false,
                        StorageCodeCurrent = "KHO_TONG_SG",
                        StoreDate = DateTime.Today.AddDays(-10),
                        Status = PackingListDetailStatus.StockIn,
                        Remark = "Xe hoàn hảo nguyên trạng"
                    }
                }
            };

            var pl2 = new PackingList
            {
                OrgId = orgId,
                PackingListNo = "2603PL0002",
                ContractNo = "HMC-2026-VN02",
                LCNo = "LC2602003",
                PortCode = "CANG_HAI_PHONG",
                PortName = "Cảng Hải Phòng (Tân Vũ / Đình Vũ)",
                PLType = PackingListType.CBU,
                Status = PackingListStatus.PortArrived,
                ShippingDateStart = DateTime.Today.AddDays(-12),
                ShippingDateEndExpected = DateTime.Today.AddDays(-2),
                ShippingDateEnd = DateTime.Today.AddDays(-1),
                VesselName = "MORNING CAROLINE",
                VoyageNo = "V2603N",
                TotalCars = 3,
                TotalRepaired = 1,
                Remark = "Lô xe Ioniq 5 EV và Custin CBU cập cảng Hải Phòng, đang phối hợp Chi cục Hải quan làm thủ tục thông quan giám định",
                CreatedBy = "IMPORT_HQ_SPECIALIST",
                CreatedAt = DateTime.Today.AddDays(-15),
                LUDateTime = DateTime.Today.AddDays(-1),
                LUBy = "IMPORT_LOGISTICS_HP",
                Details = new List<PackingListDetail>
                {
                    new()
                    {
                        PackingListNo = "2603PL0002",
                        Vin = "KMHE281BBSB200004",
                        EngineNo = "EM07-200004",
                        KeyNo = "K2004",
                        ModelCode = "IONIQ5",
                        ModelName = "Hyundai Ioniq 5 EV Thuần Điện",
                        SpecCode = "IQ5-EV-01",
                        SpecDescription = "Ioniq 5 Prestige Long Range pin 72.6 kWh",
                        ColorCode = "WH1",
                        ColorName = "Trắng Gravity",
                        WorkOrderNo = "WO-HMC-202602-088",
                        ProductionMonth = "2026-02",
                        FlagRepair = false,
                        StorageCodeCurrent = "KHO_CANG_HP",
                        Status = PackingListDetailStatus.PortArrived,
                        Remark = "Đang lưu bãi chờ thông quan"
                    },
                    new()
                    {
                        PackingListNo = "2603PL0002",
                        Vin = "KMHE281BBSB200005",
                        EngineNo = "EM07-200005",
                        KeyNo = "K2005",
                        ModelCode = "IONIQ5",
                        ModelName = "Hyundai Ioniq 5 EV Thuần Điện",
                        SpecCode = "IQ5-EV-01",
                        SpecDescription = "Ioniq 5 Prestige Long Range pin 72.6 kWh",
                        ColorCode = "MB2",
                        ColorName = "Xám nhám Digital Teal",
                        WorkOrderNo = "WO-HMC-202602-089",
                        ProductionMonth = "2026-02",
                        FlagRepair = true,
                        RepairRemark = "Gương chiếu hậu bên lái bị trầy mỏng trong container cố định, đã lập biên bản giám định bảo hiểm hàng hải",
                        StorageCodeCurrent = "KHO_CANG_HP",
                        Status = PackingListDetailStatus.PortArrived,
                        Remark = "Chờ xử lý phụ tùng thay thế KCS"
                    },
                    new()
                    {
                        PackingListNo = "2603PL0002",
                        Vin = "KMHE281BBSB200006",
                        EngineNo = "G4NN-200006",
                        KeyNo = "K2006",
                        ModelCode = "CUSTIN",
                        ModelName = "Hyundai Custin MPV 7 Chỗ",
                        SpecCode = "CS20-PRE-01",
                        SpecDescription = "Custin 2.0T Cao Cấp ghế thương gia",
                        ColorCode = "WW1",
                        ColorName = "Trắng tuyết",
                        WorkOrderNo = "WO-HMC-202602-090",
                        ProductionMonth = "2026-02",
                        FlagRepair = false,
                        StorageCodeCurrent = "KHO_CANG_HP",
                        Status = PackingListDetailStatus.PortArrived,
                        Remark = "Đang lưu bãi chờ thông quan"
                    }
                }
            };

            var pl3 = new PackingList
            {
                OrgId = orgId,
                PackingListNo = "2603PL0003",
                ContractNo = "HMC-2026-VN03",
                LCNo = "LC2603001",
                PortCode = "CANG_CAT_LAI",
                PortName = "Cảng Cát Lái (TP. Hồ Chí Minh)",
                PLType = PackingListType.CBU,
                Status = PackingListStatus.Shipping,
                ShippingDateStart = DateTime.Today.AddDays(-3),
                ShippingDateEndExpected = DateTime.Today.AddDays(7),
                VesselName = "HOEGH TARGET",
                VoyageNo = "V2603W",
                TotalCars = 2,
                TotalRepaired = 0,
                Remark = "Lô xe Palisade & Santa Fe Hybrid đang trên hải trình từ cảng Pyeongtaek về Cát Lái",
                CreatedBy = "IMPORT_HQ_SPECIALIST",
                CreatedAt = DateTime.Today.AddDays(-4),
                Details = new List<PackingListDetail>
                {
                    new()
                    {
                        PackingListNo = "2603PL0003",
                        Vin = "KMHE281BBSB200007",
                        EngineNo = "G4FT-200007",
                        KeyNo = "K2007",
                        ModelCode = "SANTAFE",
                        ModelName = "Hyundai Santa Fe All-New",
                        SpecCode = "SF16-HYB-01",
                        SpecDescription = "Santa Fe Hybrid 1.6T-GDi AWD",
                        ColorCode = "WW2",
                        ColorName = "Trắng ngọc trai",
                        WorkOrderNo = "WO-HMC-202603-001",
                        ProductionMonth = "2026-03",
                        FlagRepair = false,
                        Status = PackingListDetailStatus.OnBoard,
                        Remark = "Tàu đang trên biển"
                    },
                    new()
                    {
                        PackingListNo = "2603PL0003",
                        Vin = "KMHE281BBSB200008",
                        EngineNo = "D4HB-200008",
                        KeyNo = "K2008",
                        ModelCode = "PALISADE",
                        ModelName = "Hyundai Palisade Exclusive",
                        SpecCode = "PL22-PRE-01",
                        SpecDescription = "Palisade 2.2D Prestige 6 chỗ cao cấp",
                        ColorCode = "R3R",
                        ColorName = "Đỏ mận",
                        WorkOrderNo = "WO-HMC-202603-002",
                        ProductionMonth = "2026-03",
                        FlagRepair = false,
                        Status = PackingListDetailStatus.OnBoard,
                        Remark = "Tàu đang trên biển"
                    }
                }
            };

            var pl4 = new PackingList
            {
                OrgId = orgId,
                PackingListNo = "2603PL0004",
                ContractNo = "HMC-2026-VN04",
                LCNo = "LC2603002",
                PortCode = "CANG_CAI_MEP",
                PortName = "Cảng Quốc tế Cái Mép - Thị Vải (Bà Rịa - Vũng Tàu)",
                PLType = PackingListType.CKD,
                Status = PackingListStatus.Draft,
                ShippingDateEndExpected = DateTime.Today.AddDays(18),
                VesselName = "GLOVIS CORONA",
                VoyageNo = "V2604S",
                TotalCars = 2,
                TotalRepaired = 0,
                Remark = "Lô linh kiện lắp ráp CKD phục vụ nhà máy sản xuất HTMV, chuẩn bị nhận lệnh xuất bến",
                CreatedBy = "IMPORT_HQ_SPECIALIST",
                CreatedAt = DateTime.Today.AddDays(-1),
                Details = new List<PackingListDetail>
                {
                    new()
                    {
                        PackingListNo = "2603PL0004",
                        Vin = "KMHE281BBSB200009",
                        EngineNo = "G4FL-200009",
                        KeyNo = "K2009",
                        ModelCode = "CRETA",
                        ModelName = "Hyundai Creta Smart / Premium",
                        SpecCode = "CR15-CKD-01",
                        SpecDescription = "Bộ linh kiện đóng gói CKD Creta 1.5",
                        ColorCode = "WW2",
                        ColorName = "Trắng",
                        WorkOrderNo = "WO-HMC-202603-030",
                        ProductionMonth = "2026-03",
                        FlagRepair = false,
                        Status = PackingListDetailStatus.Draft,
                        Remark = "Chờ đóng container"
                    },
                    new()
                    {
                        PackingListNo = "2603PL0004",
                        Vin = "KMHE281BBSB200010",
                        EngineNo = "G4LC-200010",
                        KeyNo = "K2010",
                        ModelCode = "ACCENT",
                        ModelName = "Hyundai Accent Sedan",
                        SpecCode = "AC15-CKD-01",
                        SpecDescription = "Bộ linh kiện đóng gói CKD Accent 1.5 AT",
                        ColorCode = "BK1",
                        ColorName = "Đen",
                        WorkOrderNo = "WO-HMC-202603-031",
                        ProductionMonth = "2026-03",
                        FlagRepair = false,
                        Status = PackingListDetailStatus.Draft,
                        Remark = "Chờ đóng container"
                    }
                }
            };

            db.PackingLists.AddRange(pl1, pl2, pl3, pl4);
            await db.SaveChangesAsync();
        }

        if (!await db.BankingTransactions.AnyAsync(o => o.OrgId == orgId))
        {
            var bkt1 = new BankingTransaction
            {
                OrgId = orgId,
                RQ_BankingTransNo = "2603BKT0001",
                DealerCode = "VN001",
                DealerName = "Hyundai Đông Đô",
                BankCode = "VPB",
                BankName = "Ngân hàng TMCP Việt Nam Thịnh Vượng (VPBank)",
                TaxCode = "0102030405",
                TransType = BankingTransType.PMT,
                Status = BankingTransStatus.Disbursed,
                BankStatus = BankingTransBankStatus.Disbursed,
                RefBankCode = "VPB-2603180930-5821",
                BankRemark = "VPBank phê duyệt và hoàn tất giải ngân tiền mua xe vào tài khoản NPP theo hợp đồng hạn mức tín dụng 2026/VPB-DONGDO.",
                Remark = "Đề nghị giải ngân vốn vay thương mại lô 02 xe Santa Fe & Tucson theo HĐ bán buôn 2603DRC00001",
                TotalAmount = 1500000000m,
                TotalCars = 2,

                // PMT:
                PaymentNo = "PMT260318-001",
                PaymentType = "Payment",
                DisbursementType = BankingTransDisbursementType.LoanDisbursement,
                TransferAmount = 1500000000m,
                LoanPeriod = 3,
                LoanPeriodDate = DateTime.Today.AddMonths(3),
                InterestRate = 7.2m,
                ReceivingUnit = "CÔNG TY CỔ PHẦN LIÊN DOANH Ô TÔ HYUNDAI THÀNH CÔNG VIỆT NAM",
                BankAccountReceive = "111000123456",
                BankNameReceive = "VietinBank - Chi nhánh Hà Nội",
                CreditContractNo = "HDTD-VPB-2026-0088",
                DisbursementRequestDate = DateTime.Today.AddDays(-6),
                LDNo = "LD2603-VPB-0081",
                DisbursementAmount = 1500000000m,
                DisbursementDate = DateTime.Today.AddDays(-4),

                CreatedAt = DateTime.Today.AddDays(-7),
                CreatedBy = "DEALER_FINANCE_VN001",
                PushedToBankAt = DateTime.Today.AddDays(-6),
                PushedToBankBy = "DEALER_FINANCE_VN001",
                BankApprovedAt = DateTime.Today.AddDays(-5),
                BankApprovedBy = "Nguyễn Thu Hà (Chuyên viên Tín dụng KHDN VPBank)",
                DisbursedAt = DateTime.Today.AddDays(-4),
                DisbursedBy = "Trần Đình Trọng (Kiểm soát viên Giao dịch VPBank)",

                Details = new List<BankingTransactionDetail>
                {
                    new()
                    {
                        RQ_BankingTransNo = "2603BKT0001",
                        CarId = "CAR2026-SF0101",
                        Vin = "KMHE281BBSA100101",
                        ModelCode = "SANTAFE",
                        ModelName = "Hyundai Santa Fe 2.5 HTRAC",
                        SpecCode = "SF25-PRE-01",
                        SpecDescription = "Santa Fe 2.5 Xăng Cao Cấp AWD",
                        ColorCode = "WW2",
                        DlrCtrNo = "2603DRC00001",
                        SOCode = "ORD2603010001",
                        HTCInvoiceNo = "0001201",
                        AmountActual = 1050000000m,
                        AllocPercent = 100m,
                        AllocAmount = 1050000000m,
                        Remark = "Giải ngân 100% giá trị xe Santa Fe"
                    },
                    new()
                    {
                        RQ_BankingTransNo = "2603BKT0001",
                        CarId = "CAR2026-TU0201",
                        Vin = "KMHE281BBSA200201",
                        ModelCode = "TUCSON",
                        ModelName = "Hyundai Tucson 2.0 AT",
                        SpecCode = "TU20-PRE-01",
                        SpecDescription = "Tucson 2.0 Xăng Đặc Biệt",
                        ColorCode = "NKA",
                        DlrCtrNo = "2603DRC00001",
                        SOCode = "ORD2603010001",
                        HTCInvoiceNo = "0001202",
                        AmountActual = 900000000m,
                        AllocPercent = 50m,
                        AllocAmount = 450000000m,
                        Remark = "Giải ngân 50% vốn vay ngân hàng (50% còn lại đối ứng vốn tự có)"
                    }
                },
                Attachments = new List<BankingTransactionAttachFile>
                {
                    new()
                    {
                        RQ_BankingTransNo = "2603BKT0001",
                        FileIndex = 1,
                        FileType = BankingTransFileType.Contract,
                        FileName = "HopDong_BanBuon_2603DRC00001.pdf",
                        FilePath = "/storage/contracts/2603DRC00001.pdf",
                        FlagSigned = true,
                        SerialNumber = "5404123899ABCDEF",
                        CaSubject = "CÔNG TY CỔ PHẦN HYUNDAI ĐÔNG ĐÔ",
                        SignedAt = DateTime.Today.AddDays(-6),
                        SignedBy = "Nguyễn Văn Hưng (Giám đốc Đại lý)",
                        UploadedAt = DateTime.Today.AddDays(-7),
                        UploadedBy = "DEALER_FINANCE_VN001"
                    },
                    new()
                    {
                        RQ_BankingTransNo = "2603BKT0001",
                        FileIndex = 2,
                        FileType = BankingTransFileType.Authorization,
                        FileName = "GiayDeNghiGiaiNgan_VPBank_Signed.pdf",
                        FilePath = "/storage/banking/VPB_DeNghiGiaiNgan_2603BKT0001.pdf",
                        FlagSigned = true,
                        SerialNumber = "5404123899ABCDEF",
                        CaSubject = "CÔNG TY CỔ PHẦN HYUNDAI ĐÔNG ĐÔ",
                        SignedAt = DateTime.Today.AddDays(-6),
                        SignedBy = "Nguyễn Văn Hưng (Giám đốc Đại lý)",
                        UploadedAt = DateTime.Today.AddDays(-6),
                        UploadedBy = "DEALER_FINANCE_VN001"
                    }
                }
            };

            var bkt2 = new BankingTransaction
            {
                OrgId = orgId,
                RQ_BankingTransNo = "2603BKT0002",
                DealerCode = "VN002",
                DealerName = "Hyundai Nam Trung",
                BankCode = "CTG",
                BankName = "Ngân hàng TMCP Công Thương Việt Nam (VietinBank)",
                TaxCode = "0103040506",
                TransType = BankingTransType.GRT,
                Status = BankingTransStatus.Approved,
                BankStatus = BankingTransBankStatus.BankApproved,
                RefBankCode = "CTG-2603201415-9923",
                BankRemark = "VietinBank chi nhánh Đống Đa đồng ý phát hành cam kết bảo lãnh thanh toán vô điều kiện cho NPP Hyundai Thành Công.",
                Remark = "Đề nghị phát hành thư bảo lãnh thanh toán bán buôn lô xe Creta và Accent theo HĐ 2603DRC00002",
                TotalAmount = 1200000000m,
                TotalCars = 2,

                // GRT:
                GuaranteeType = "Bảo lãnh thanh toán mua buôn xe ô tô",
                DateExpiredValue = 90,
                GrtForm = "Thư bảo lãnh điện tử ký số CA",
                GrtReceive = "HYUNDAI THANH CONG VIETNAM",
                MDNo = "MD2603-CTG-0012",
                GrtAmount = 1200000000m,
                GrtDateStart = DateTime.Today.AddDays(-2),
                GrtDateEnd = DateTime.Today.AddDays(88),

                CreatedAt = DateTime.Today.AddDays(-4),
                CreatedBy = "DEALER_FINANCE_VN002",
                PushedToBankAt = DateTime.Today.AddDays(-3),
                PushedToBankBy = "DEALER_FINANCE_VN002",
                BankApprovedAt = DateTime.Today.AddDays(-2),
                BankApprovedBy = "Lê Hồng Phong (Trưởng phòng KHDN VietinBank Đống Đa)",

                Details = new List<BankingTransactionDetail>
                {
                    new()
                    {
                        RQ_BankingTransNo = "2603BKT0002",
                        CarId = "CAR2026-CR0301",
                        Vin = "KMHE281BBSA300301",
                        ModelCode = "CRETA",
                        ModelName = "Hyundai Creta 1.5 Smart",
                        SpecCode = "CR15-SMT-01",
                        SpecDescription = "Creta 1.5 Smart CVT",
                        ColorCode = "WH1",
                        DlrCtrNo = "2603DRC00002",
                        SOCode = "ORD2603010002",
                        AmountActual = 600000000m,
                        AllocPercent = 100m,
                        AllocAmount = 600000000m,
                        Remark = "Bảo lãnh thanh toán mua xe Creta"
                    },
                    new()
                    {
                        RQ_BankingTransNo = "2603BKT0002",
                        CarId = "CAR2026-AC0401",
                        Vin = "KMHE281BBSA400401",
                        ModelCode = "ACCENT",
                        ModelName = "Hyundai Accent 1.5 AT",
                        SpecCode = "AC15-AT-01",
                        SpecDescription = "Accent 1.5 AT Đặc biệt",
                        ColorCode = "MB1",
                        DlrCtrNo = "2603DRC00002",
                        SOCode = "ORD2603010002",
                        AmountActual = 600000000m,
                        AllocPercent = 100m,
                        AllocAmount = 600000000m,
                        Remark = "Bảo lãnh thanh toán mua xe Accent"
                    }
                },
                Attachments = new List<BankingTransactionAttachFile>
                {
                    new()
                    {
                        RQ_BankingTransNo = "2603BKT0002",
                        FileIndex = 1,
                        FileType = BankingTransFileType.GuaranteeLetter,
                        FileName = "DeNghiCapBaoLanh_VietinBank.pdf",
                        FilePath = "/storage/banking/CTG_DeNghiBL_2603BKT0002.pdf",
                        FlagSigned = true,
                        SerialNumber = "6899443322AABBCC",
                        CaSubject = "CÔNG TY CỔ PHẦN HYUNDAI NAM TRUNG",
                        SignedAt = DateTime.Today.AddDays(-3),
                        SignedBy = "Trần Mạnh Cường (Tổng Giám đốc)",
                        UploadedAt = DateTime.Today.AddDays(-4),
                        UploadedBy = "DEALER_FINANCE_VN002"
                    }
                }
            };

            var bkt3 = new BankingTransaction
            {
                OrgId = orgId,
                RQ_BankingTransNo = "2603BKT0003",
                DealerCode = "VN003",
                DealerName = "Hyundai Tây Hồ",
                BankCode = "VIB",
                BankName = "Ngân hàng TMCP Quốc tế Việt Nam (VIB)",
                TaxCode = "0104050607",
                TransType = BankingTransType.PMT,
                Status = BankingTransStatus.Pending,
                BankStatus = BankingTransBankStatus.BankReceived,
                RefBankCode = "VIB-2603221015-7712",
                BankRemark = "Ngân hàng VIB đã tiếp nhận hồ sơ đề nghị giải ngân qua Open Banking API, đang chờ cán bộ tín dụng thẩm định danh mục xe.",
                Remark = "Đề nghị giải ngân thanh toán mua xe Stargazer X thế chấp bằng quyền đòi nợ HĐ mua xe",
                TotalAmount = 850000000m,
                TotalCars = 1,

                // PMT:
                PaymentType = "Payment",
                DisbursementType = BankingTransDisbursementType.LoanDisbursement,
                TransferAmount = 850000000m,
                LoanPeriod = 3,
                LoanPeriodDate = DateTime.Today.AddMonths(3),
                InterestRate = 7.8m,
                ReceivingUnit = "CÔNG TY CỔ PHẦN LIÊN DOANH Ô TÔ HYUNDAI THÀNH CÔNG VIỆT NAM",
                BankAccountReceive = "111000123456",
                BankNameReceive = "VietinBank - Chi nhánh Hà Nội",
                CreditContractNo = "HDTD-VIB-2026-031",
                DisbursementRequestDate = DateTime.Today.AddDays(1),

                CreatedAt = DateTime.Today.AddDays(-1),
                CreatedBy = "DEALER_FINANCE_VN003",
                PushedToBankAt = DateTime.Today.AddDays(-1).AddHours(2),
                PushedToBankBy = "DEALER_FINANCE_VN003",

                Details = new List<BankingTransactionDetail>
                {
                    new()
                    {
                        RQ_BankingTransNo = "2603BKT0003",
                        CarId = "CAR2026-SG0501",
                        Vin = "KMHE281BBSA500501",
                        ModelCode = "STARGAZER",
                        ModelName = "Hyundai Stargazer X",
                        SpecCode = "SG15-PRE-01",
                        SpecDescription = "Stargazer X 1.5 Cao Cấp",
                        ColorCode = "BK1",
                        DlrCtrNo = "2603DRC00003",
                        AmountActual = 850000000m,
                        AllocPercent = 100m,
                        AllocAmount = 850000000m,
                        Remark = "Giải ngân tiền xe Stargazer X"
                    }
                },
                Attachments = new List<BankingTransactionAttachFile>
                {
                    new()
                    {
                        RQ_BankingTransNo = "2603BKT0003",
                        FileIndex = 1,
                        FileType = BankingTransFileType.Contract,
                        FileName = "HopDongMuaBan_Stargazer_Signed.pdf",
                        FilePath = "/storage/contracts/HD_Stargazer_VN003.pdf",
                        FlagSigned = true,
                        SerialNumber = "9988776655CCDDEE",
                        CaSubject = "CÔNG TY CỔ PHẦN HYUNDAI TÂY HỒ",
                        SignedAt = DateTime.Today.AddDays(-1),
                        SignedBy = "Vũ Quang Hải (Giám đốc)",
                        UploadedAt = DateTime.Today.AddDays(-1),
                        UploadedBy = "DEALER_FINANCE_VN003"
                    }
                }
            };

            var bkt4 = new BankingTransaction
            {
                OrgId = orgId,
                RQ_BankingTransNo = "2603BKT0004",
                DealerCode = "VN001",
                DealerName = "Hyundai Đông Đô",
                BankCode = "BIDV",
                BankName = "Ngân hàng TMCP Đầu tư và Phát triển Việt Nam (BIDV)",
                TaxCode = "0102030405",
                TransType = BankingTransType.GRT_LC,
                Status = BankingTransStatus.Rejected,
                BankStatus = BankingTransBankStatus.BankRejected,
                RefBankCode = "BIDV-2603191100-3341",
                BankRemark = "Vượt hạn mức tín dụng dự phòng khả dụng của đại lý tại BIDV chi nhánh Cầu Giấy trong quý 1/2026.",
                RejectReason = "Vượt hạn mức bảo lãnh khả dụng. Đề nghị đại lý bổ sung thêm tài sản bảo đảm hoặc thanh toán giảm nợ gốc trước khi xin mở L/C mới.",
                Remark = "Đề nghị mở Thư tín dụng chứng từ L/C mua lô xe Custin",
                TotalAmount = 900000000m,
                TotalCars = 1,

                // GRT:
                GuaranteeType = "Bảo lãnh mở thư tín dụng L/C mua buôn xe",
                DateExpiredValue = 60,
                GrtForm = "Thư tín dụng L/C điện tử MT700 Swift",
                GrtReceive = "HYUNDAI THANH CONG VIETNAM",
                GrtAmount = 900000000m,
                GrtDateStart = DateTime.Today.AddDays(-3),
                GrtDateEnd = DateTime.Today.AddDays(57),

                CreatedAt = DateTime.Today.AddDays(-4),
                CreatedBy = "DEALER_FINANCE_VN001",
                PushedToBankAt = DateTime.Today.AddDays(-3),
                PushedToBankBy = "DEALER_FINANCE_VN001",
                RejectedAt = DateTime.Today.AddDays(-2),
                RejectedBy = "Hoàng Tuấn Anh (Trưởng phòng Tín dụng Doanh nghiệp BIDV Cầu Giấy)",

                Details = new List<BankingTransactionDetail>
                {
                    new()
                    {
                        RQ_BankingTransNo = "2603BKT0004",
                        CarId = "CAR2026-CS0601",
                        Vin = "KMHE281BBSA600601",
                        ModelCode = "CUSTIN",
                        ModelName = "Hyundai Custin 2.0T",
                        SpecCode = "CS20-PRE-01",
                        SpecDescription = "Custin 2.0T Cao Cấp",
                        ColorCode = "WW1",
                        DlrCtrNo = "2603DRC00004",
                        AmountActual = 900000000m,
                        AllocPercent = 100m,
                        AllocAmount = 900000000m,
                        Remark = "Đề nghị mở L/C cho xe Custin"
                    }
                }
            };

            var bkt5 = new BankingTransaction
            {
                OrgId = orgId,
                RQ_BankingTransNo = "2603BKT0005",
                DealerCode = "VN002",
                DealerName = "Hyundai Nam Trung",
                BankCode = "TCB",
                BankName = "Ngân hàng TMCP Kỹ Thương Việt Nam (Techcombank)",
                TaxCode = "0103040506",
                TransType = BankingTransType.PMT,
                Status = BankingTransStatus.Draft,
                BankStatus = BankingTransBankStatus.Draft,
                Remark = "Bản nháp hồ sơ đề nghị vay giải ngân mua xe Elantra thế chấp bằng hợp đồng mua bán buôn",
                TotalAmount = 760000000m,
                TotalCars = 1,

                // PMT:
                PaymentType = "Payment",
                DisbursementType = BankingTransDisbursementType.LoanDisbursement,
                TransferAmount = 760000000m,
                LoanPeriod = 3,
                LoanPeriodDate = DateTime.Today.AddMonths(3),
                InterestRate = 7.0m,
                ReceivingUnit = "CÔNG TY CỔ PHẦN LIÊN DOANH Ô TÔ HYUNDAI THÀNH CÔNG VIỆT NAM",
                BankAccountReceive = "111000123456",
                BankNameReceive = "VietinBank - Chi nhánh Hà Nội",
                CreditContractNo = "HDTD-TCB-2026-055",
                DisbursementRequestDate = DateTime.Today.AddDays(3),

                CreatedAt = DateTime.Today,
                CreatedBy = "DEALER_FINANCE_VN002",

                Details = new List<BankingTransactionDetail>
                {
                    new()
                    {
                        RQ_BankingTransNo = "2603BKT0005",
                        CarId = "CAR2026-EL0701",
                        Vin = "KMHE281BBSA700701",
                        ModelCode = "ELANTRA",
                        ModelName = "Hyundai Elantra 2.0 AT",
                        SpecCode = "EL20-PRE-01",
                        SpecDescription = "Elantra 2.0 AT Cao Cấp",
                        ColorCode = "BK1",
                        DlrCtrNo = "2603DRC00002",
                        AmountActual = 760000000m,
                        AllocPercent = 100m,
                        AllocAmount = 760000000m,
                        Remark = "Phân bổ 100% giá trị xe Elantra"
                    }
                }
            };

            db.BankingTransactions.AddRange(bkt1, bkt2, bkt3, bkt4, bkt5);
            await db.SaveChangesAsync();
        }

        if (!await db.CustomsDeclarations.AnyAsync(x => x.OrgId == orgId))
        {
            // Seed 4 Tờ khai Hải quan mẫu đại diện cho 4 trạng thái và 4 luồng/cảng nhập khẩu
            var tk1 = new CustomsDeclaration
            {
                OrgId = orgId,
                DeclarationNo = "2603TK00001",
                PortCode = "HPH",
                PortName = "Cảng Hải Phòng (Đình Vũ / Lạch Huyện)",
                ContractNo = "HMC-2026-VN01",
                OpenDate = DateTime.Today.AddDays(-15),
                TaxPaymentDate = DateTime.Today.AddDays(-12),
                TaxReceiptNo = "BLT-20260308-HPH01",
                TaxPayerBank = "Vietcombank - Chi nhánh Thăng Long",
                CustomsOffice = "Chi cục Hải quan Cửa khẩu Cảng Hải Phòng KV3",
                Channel = CustomsChannel.Green,
                Status = CustomsDeclarationStatus.Cleared,
                TotalCars = 3,
                DutiableAmount = 2550000000m,
                ImportTaxAmount = 1275000000m,
                SpecialConsumptionTaxAmount = 2295000000m,
                VatAmount = 612000000m,
                TotalTaxAmount = 4182000000m,
                CustomsClearanceDate = DateTime.Today.AddDays(-12),
                CustomsOfficer = "Vũ Hải Đăng - Hải quan KV3 Đình Vũ",
                Remark = "Lô xe Palisade CBU nhập khẩu từ Ulsan Hàn Quốc, đã hoàn tất nộp thuế và thông quan giải phóng xe",
                CreatedBy = "HQ_LOGISTICS",
                CreatedAt = DateTime.Today.AddDays(-15),
                SubmittedBy = "HQ_LOGISTICS",
                SubmittedAt = DateTime.Today.AddDays(-14),
                TaxPaidBy = "HQ_FINANCE",
                ClearedBy = "Vũ Hải Đăng",
                Details = new List<CustomsDeclarationDetail>
                {
                    new()
                    {
                        DeclarationNo = "2603TK00001",
                        Vin = "KMHE281BBSA900101",
                        EngineNo = "D4HB-900101",
                        ModelCode = "PALISADE",
                        ModelName = "Hyundai Palisade 2.2D Prestige",
                        SpecCode = "PAL-PRE-01",
                        SpecDescription = "Palisade 2.2 Diesel Prestige 6 chỗ",
                        ColorCode = "WW1",
                        ColorName = "Trắng ngọc trai",
                        PackingListNo = "2603PL0001",
                        LCNo = "LC26010088",
                        ContractNo = "HMC-2026-VN01",
                        DutiableValue = 850000000m,
                        ImportTax = 425000000m,
                        SpecialConsumptionTax = 765000000m,
                        Vat = 204000000m,
                        TotalTax = 1394000000m,
                        Status = CustomsDeclarationDetailStatus.Cleared,
                        TaxPaymentDate = DateTime.Today.AddDays(-12),
                        ClearedAt = DateTime.Today.AddDays(-12),
                        Remark = "Thông quan luồng xanh, đủ điều kiện lưu hành"
                    },
                    new()
                    {
                        DeclarationNo = "2603TK00001",
                        Vin = "KMHE281BBSA900102",
                        EngineNo = "D4HB-900102",
                        ModelCode = "PALISADE",
                        ModelName = "Hyundai Palisade 2.2D Exclusive",
                        SpecCode = "PAL-EXC-01",
                        SpecDescription = "Palisade 2.2 Diesel Exclusive 7 chỗ",
                        ColorCode = "BK1",
                        ColorName = "Đen Abyss",
                        PackingListNo = "2603PL0001",
                        LCNo = "LC26010088",
                        ContractNo = "HMC-2026-VN01",
                        DutiableValue = 850000000m,
                        ImportTax = 425000000m,
                        SpecialConsumptionTax = 765000000m,
                        Vat = 204000000m,
                        TotalTax = 1394000000m,
                        Status = CustomsDeclarationDetailStatus.Cleared,
                        TaxPaymentDate = DateTime.Today.AddDays(-12),
                        ClearedAt = DateTime.Today.AddDays(-12),
                        Remark = "Thông quan luồng xanh, đủ điều kiện lưu hành"
                    },
                    new()
                    {
                        DeclarationNo = "2603TK00001",
                        Vin = "KMHE281BBSA900103",
                        EngineNo = "G6DN-900103",
                        ModelCode = "PALISADE",
                        ModelName = "Hyundai Palisade 3.8 V6 Exclusive",
                        SpecCode = "PAL-V6-01",
                        SpecDescription = "Palisade 3.8 V6 Xăng Cao Cấp 7 chỗ",
                        ColorCode = "BL2",
                        ColorName = "Xanh Moonlight",
                        PackingListNo = "2603PL0001",
                        LCNo = "LC26010088",
                        ContractNo = "HMC-2026-VN01",
                        DutiableValue = 850000000m,
                        ImportTax = 425000000m,
                        SpecialConsumptionTax = 765000000m,
                        Vat = 204000000m,
                        TotalTax = 1394000000m,
                        Status = CustomsDeclarationDetailStatus.Cleared,
                        TaxPaymentDate = DateTime.Today.AddDays(-12),
                        ClearedAt = DateTime.Today.AddDays(-12),
                        Remark = "Thông quan luồng xanh, đủ điều kiện lưu hành"
                    }
                }
            };

            var tk2 = new CustomsDeclaration
            {
                OrgId = orgId,
                DeclarationNo = "2603TK00002",
                PortCode = "SGN",
                PortName = "Cảng Cát Lái - TP. Hồ Chí Minh",
                ContractNo = "HMC-2026-VN02",
                OpenDate = DateTime.Today.AddDays(-8),
                TaxPaymentDate = DateTime.Today.AddDays(-5),
                TaxReceiptNo = "BLT-20260315-SGN02",
                TaxPayerBank = "BIDV - Chi nhánh TP. Hồ Chí Minh",
                CustomsOffice = "Chi cục Hải quan Cửa khẩu Cảng Sài Gòn KV1",
                Channel = CustomsChannel.Yellow,
                Status = CustomsDeclarationStatus.TaxPaid,
                TotalCars = 2,
                DutiableAmount = 1240000000m,
                ImportTaxAmount = 620000000m,
                SpecialConsumptionTaxAmount = 744000000m,
                VatAmount = 260400000m,
                TotalTaxAmount = 1624400000m,
                Remark = "Lô xe MPV Custin CBU cập cảng Cát Lái, đã nộp thuế đầy đủ vào Kho bạc Nhà nước, chờ kiểm tra hồ sơ thông quan",
                CreatedBy = "HQ_LOGISTICS",
                CreatedAt = DateTime.Today.AddDays(-8),
                SubmittedBy = "HQ_LOGISTICS",
                SubmittedAt = DateTime.Today.AddDays(-7),
                TaxPaidBy = "HQ_FINANCE",
                Details = new List<CustomsDeclarationDetail>
                {
                    new()
                    {
                        DeclarationNo = "2603TK00002",
                        Vin = "KMHE281BBSA900201",
                        EngineNo = "G4NN-900201",
                        ModelCode = "CUSTIN",
                        ModelName = "Hyundai Custin 2.0T Cao Cấp",
                        SpecCode = "CS20-PRE-01",
                        SpecDescription = "Custin 2.0 Turbo Bản Cao Cấp 7 chỗ",
                        ColorCode = "WW1",
                        ColorName = "Trắng tuyết",
                        PackingListNo = "2603PL0002",
                        LCNo = "LC26010092",
                        ContractNo = "HMC-2026-VN02",
                        DutiableValue = 620000000m,
                        ImportTax = 310000000m,
                        SpecialConsumptionTax = 372000000m,
                        Vat = 130200000m,
                        TotalTax = 812200000m,
                        Status = CustomsDeclarationDetailStatus.TaxPaid,
                        TaxPaymentDate = DateTime.Today.AddDays(-5),
                        Remark = "Đã nộp thuế vào NSNN, chờ công chức hải quan ký thông quan"
                    },
                    new()
                    {
                        DeclarationNo = "2603TK00002",
                        Vin = "KMHE281BBSA900202",
                        EngineNo = "G4FS-900202",
                        ModelCode = "CUSTIN",
                        ModelName = "Hyundai Custin 1.5T Đặc Biệt",
                        SpecCode = "CS15-SPC-01",
                        SpecDescription = "Custin 1.5 Turbo Bản Đặc Biệt 7 chỗ",
                        ColorCode = "GY1",
                        ColorName = "Xám Kim Loại",
                        PackingListNo = "2603PL0002",
                        LCNo = "LC26010092",
                        ContractNo = "HMC-2026-VN02",
                        DutiableValue = 620000000m,
                        ImportTax = 310000000m,
                        SpecialConsumptionTax = 372000000m,
                        Vat = 130200000m,
                        TotalTax = 812200000m,
                        Status = CustomsDeclarationDetailStatus.TaxPaid,
                        TaxPaymentDate = DateTime.Today.AddDays(-5),
                        Remark = "Đã nộp thuế vào NSNN, chờ công chức hải quan ký thông quan"
                    }
                }
            };

            var tk3 = new CustomsDeclaration
            {
                OrgId = orgId,
                DeclarationNo = "2603TK00003",
                PortCode = "CM",
                PortName = "Cảng Quốc tế Cái Mép - Bà Rịa Vũng Tàu",
                ContractNo = "HMC-2026-VN03",
                OpenDate = DateTime.Today.AddDays(-4),
                TaxPayerBank = "VietinBank - Chi nhánh Vũng Tàu",
                CustomsOffice = "Chi cục Hải quan Cửa khẩu Cảng Cái Mép",
                Channel = CustomsChannel.Yellow,
                Status = CustomsDeclarationStatus.Submitted,
                TotalCars = 2,
                DutiableAmount = 1800000000m,
                ImportTaxAmount = 900000000m,
                SpecialConsumptionTaxAmount = 81000000m,
                VatAmount = 278100000m,
                TotalTaxAmount = 1259100000m,
                Remark = "Lô xe điện Ioniq 5 CBU công nghệ cao, thuế suất TTĐB ưu đãi 3%, đã nộp tờ khai điện tử VNACCS đang chờ duyệt biểu thuế",
                CreatedBy = "HQ_LOGISTICS",
                CreatedAt = DateTime.Today.AddDays(-4),
                SubmittedBy = "HQ_LOGISTICS",
                SubmittedAt = DateTime.Today.AddDays(-3),
                Details = new List<CustomsDeclarationDetail>
                {
                    new()
                    {
                        DeclarationNo = "2603TK00003",
                        Vin = "KMHE281BBSA900301",
                        EngineNo = "EM07-900301",
                        ModelCode = "IONIQ5",
                        ModelName = "Hyundai Ioniq 5 Prestige",
                        SpecCode = "IQ5-PRE-01",
                        SpecDescription = "Ioniq 5 EV Prestige pin 72.6 kWh",
                        ColorCode = "WW1",
                        ColorName = "Trắng Atlas",
                        PackingListNo = "2603PL0003",
                        LCNo = "LC26010095",
                        ContractNo = "HMC-2026-VN03",
                        DutiableValue = 900000000m,
                        ImportTax = 450000000m,
                        SpecialConsumptionTax = 40500000m,
                        Vat = 139050000m,
                        TotalTax = 629550000m,
                        Status = CustomsDeclarationDetailStatus.Pending,
                        Remark = "Chờ lệnh trích nộp thuế từ ngân hàng"
                    },
                    new()
                    {
                        DeclarationNo = "2603TK00003",
                        Vin = "KMHE281BBSA900302",
                        EngineNo = "EM07-900302",
                        ModelCode = "IONIQ5",
                        ModelName = "Hyundai Ioniq 5 Exclusive",
                        SpecCode = "IQ5-EXC-01",
                        SpecDescription = "Ioniq 5 EV Exclusive pin 58 kWh",
                        ColorCode = "SL1",
                        ColorName = "Bạc Gravity Gold",
                        PackingListNo = "2603PL0003",
                        LCNo = "LC26010095",
                        ContractNo = "HMC-2026-VN03",
                        DutiableValue = 900000000m,
                        ImportTax = 450000000m,
                        SpecialConsumptionTax = 40500000m,
                        Vat = 139050000m,
                        TotalTax = 629550000m,
                        Status = CustomsDeclarationDetailStatus.Pending,
                        Remark = "Chờ lệnh trích nộp thuế từ ngân hàng"
                    }
                }
            };

            var tk4 = new CustomsDeclaration
            {
                OrgId = orgId,
                DeclarationNo = "2603TK00004",
                PortCode = "HPH",
                PortName = "Cảng Hải Phòng (Đình Vũ / Lạch Huyện)",
                ContractNo = "HMC-2026-VN04",
                OpenDate = DateTime.Today.AddDays(-1),
                TaxPayerBank = "Vietcombank",
                CustomsOffice = "Chi cục Hải quan Cửa khẩu Cảng Hải Phòng KV3",
                Channel = CustomsChannel.Red,
                Status = CustomsDeclarationStatus.Draft,
                TotalCars = 2,
                DutiableAmount = 840000000m,
                ImportTaxAmount = 420000000m,
                SpecialConsumptionTaxAmount = 441000000m,
                VatAmount = 170100000m,
                TotalTaxAmount = 1031100000m,
                Remark = "Lô xe Stargazer X CBU đang lập hồ sơ nháp, dự kiến chuyển giao cán bộ kiểm hóa luồng đỏ",
                CreatedBy = "HQ_LOGISTICS",
                CreatedAt = DateTime.Today.AddDays(-1),
                Details = new List<CustomsDeclarationDetail>
                {
                    new()
                    {
                        DeclarationNo = "2603TK00004",
                        Vin = "KMHE281BBSA900401",
                        EngineNo = "G4FL-900401",
                        ModelCode = "STARGAZER",
                        ModelName = "Hyundai Stargazer X Cao Cấp",
                        SpecCode = "SG-PRE-01",
                        SpecDescription = "Stargazer X 1.5 CVT Bản Cao Cấp 7 chỗ",
                        ColorCode = "WW1",
                        ColorName = "Trắng mờ",
                        PackingListNo = "2603PL0004",
                        LCNo = "LC26010099",
                        ContractNo = "HMC-2026-VN04",
                        DutiableValue = 420000000m,
                        ImportTax = 210000000m,
                        SpecialConsumptionTax = 220500000m,
                        Vat = 85050000m,
                        TotalTax = 515550000m,
                        Status = CustomsDeclarationDetailStatus.Pending,
                        Remark = "Hồ sơ nháp, chuẩn bị truyền VNACCS"
                    },
                    new()
                    {
                        DeclarationNo = "2603TK00004",
                        Vin = "KMHE281BBSA900402",
                        EngineNo = "G4FL-900402",
                        ModelCode = "STARGAZER",
                        ModelName = "Hyundai Stargazer X Tiêu Chuẩn",
                        SpecCode = "SG-STD-01",
                        SpecDescription = "Stargazer X 1.5 CVT Bản Tiêu Chuẩn 7 chỗ",
                        ColorCode = "BK1",
                        ColorName = "Đen Midnight",
                        PackingListNo = "2603PL0004",
                        LCNo = "LC26010099",
                        ContractNo = "HMC-2026-VN04",
                        DutiableValue = 420000000m,
                        ImportTax = 210000000m,
                        SpecialConsumptionTax = 220500000m,
                        Vat = 85050000m,
                        TotalTax = 515550000m,
                        Status = CustomsDeclarationDetailStatus.Pending,
                        Remark = "Hồ sơ nháp, chuẩn bị truyền VNACCS"
                    }
                }
            };

            db.CustomsDeclarations.AddRange(tk1, tk2, tk3, tk4);

            // Seed Bảng kê Quyết toán Chi phí Quản lý Thiết bị GPS Xe Ô tô HTV - TCMS
            if (!await db.GpsUnitPrices.AnyAsync())
            {
                db.GpsUnitPrices.AddRange(
                    new GpsUnitPriceMaster
                    {
                        PriceCode = "GPS-STD-2026",
                        PriceName = "Gói cước giám sát định vị tiêu chuẩn ô tô HTV",
                        DailyRate = 1500m,
                        MonthlyRate = 45000m,
                        EffStartDate = new DateTime(2026, 1, 1),
                        IsActive = true,
                        Remark = "Biểu phí tiêu chuẩn dịch vụ viễn thông giám sát xe ô tô HTV - TCMS áp dụng từ 01/01/2026"
                    },
                    new GpsUnitPriceMaster
                    {
                        PriceCode = "GPS-VIP-2026",
                        PriceName = "Gói cước xe điện cao cấp & chuyên dùng",
                        DailyRate = 2000m,
                        MonthlyRate = 60000m,
                        EffStartDate = new DateTime(2026, 1, 1),
                        IsActive = true,
                        Remark = "Biểu phí xe điện và xe chuyên dùng có tần suất truyền tin định vị 10s/lần"
                    }
                );
            }

            if (!await db.PaymentGPSOrders.AnyAsync(o => o.OrgId == orgId))
            {
                var pg1 = new PaymentGPSOrder
                {
                    OrgId = orgId,
                    PaymentGPSNo = "2603PG0001",
                    PmtMonth = "2026-02",
                    FromDate = new DateTime(2026, 2, 1),
                    ToDate = new DateTime(2026, 2, 28),
                    TotalCars = 3,
                    AmountTotal = 118500m,
                    VatRate = 10m,
                    UnitPriceVAT = 11850m,
                    TotalAmountVAT = 130350m,
                    Status = PaymentGPSStatus.TCMSSigned,
                    HTVSignStatus = PaymentGPSSignStatus.DaKy,
                    HTVSignDate = new DateTime(2026, 3, 5, 14, 30, 0),
                    HTVSignBy = "Hoàng Quốc Việt (Kế toán Trưởng HTV)",
                    TCMSSignStatus = PaymentGPSSignStatus.DaKy,
                    TCMSSignDate = new DateTime(2026, 3, 6, 9, 15, 0),
                    TCMSSignBy = "Nguyễn Văn Hùng (Giám đốc Khối Viễn thông TCMS)",
                    FilePath = "/reports/payment-gps/2603PG0001_FullySigned.pdf",
                    Remark = "Quyết toán phí dịch vụ GPS xe ô tô tháng 02/2026 đã đối soát hoàn tất 2 bên",
                    CreatedBy = "HTC_GPS_ADMIN",
                    CreatedAt = new DateTime(2026, 3, 1, 8, 0, 0),
                    Approved1At = new DateTime(2026, 3, 3, 10, 0, 0),
                    Approved1By = "Trần Văn Long (Trưởng phòng Bán hàng HTV)",
                    Approved2At = new DateTime(2026, 3, 5, 14, 30, 0),
                    Approved2By = "Hoàng Quốc Việt (Kế toán Trưởng HTV)",
                    Details = new List<PaymentGPSDetail>
                    {
                        new()
                        {
                            PaymentGPSNo = "2603PG0001",
                            Vin = "KMHE281BBSA129841",
                            CarId = "CAR2026-SF0988",
                            ModelCode = "SANTAFE",
                            ModelName = "Hyundai Santa Fe 2.5 HTRAC",
                            SpecCode = "SF25-PRE-01",
                            ColorCode = "WW2",
                            DealerCode = "VN001",
                            DealerName = "Hyundai Đông Đô",
                            GPSDvNo = "GPS-129841",
                            GPSStartDate = new DateTime(2026, 1, 10),
                            CostGPSStartDate = new DateTime(2026, 2, 1),
                            CostGPSEndDate = new DateTime(2026, 2, 28),
                            PlanCostGPSDate = 28,
                            DeductDate = 0,
                            ActualCostGPSDate = 28,
                            PriceGPS = 1500m,
                            AmountGPS = 42000m,
                            Remark = "Định vị hoạt động ổn định trên cung đường vận tải"
                        },
                        new()
                        {
                            PaymentGPSNo = "2603PG0001",
                            Vin = "KMHE281BBSA987654",
                            CarId = "CAR2026-TU1102",
                            ModelCode = "TUCSON",
                            ModelName = "Hyundai Tucson 1.6 Turbo",
                            SpecCode = "TU16-TRB-01",
                            ColorCode = "BK1",
                            DealerCode = "VN002",
                            DealerName = "Hyundai Nam Trung",
                            GPSDvNo = "GPS-987654",
                            GPSStartDate = new DateTime(2026, 1, 15),
                            CostGPSStartDate = new DateTime(2026, 2, 1),
                            CostGPSEndDate = new DateTime(2026, 2, 28),
                            PlanCostGPSDate = 28,
                            DeductDate = 1,
                            ActualCostGPSDate = 27,
                            PriceGPS = 1500m,
                            AmountGPS = 40500m,
                            Remark = "Khấu trừ 01 ngày mất tín hiệu tại hầm kho chi nhánh"
                        },
                        new()
                        {
                            PaymentGPSNo = "2603PG0001",
                            Vin = "KMHE281BBSA334455",
                            CarId = "CAR2026-CR0192",
                            ModelCode = "CRETA",
                            ModelName = "Hyundai Creta 1.5 Cao Cấp",
                            SpecCode = "CR15-PRE-02",
                            ColorCode = "R3R",
                            DealerCode = "VN001",
                            DealerName = "Hyundai Đông Đô",
                            GPSDvNo = "GPS-334455",
                            GPSStartDate = new DateTime(2026, 2, 5),
                            CostGPSStartDate = new DateTime(2026, 2, 5),
                            CostGPSEndDate = new DateTime(2026, 2, 28),
                            PlanCostGPSDate = 24,
                            DeductDate = 0,
                            ActualCostGPSDate = 24,
                            PriceGPS = 1500m,
                            AmountGPS = 36000m,
                            Remark = "Kích hoạt ngày 05/02 sau khi lắp xong thiết bị"
                        }
                    }
                };

                var pg2 = new PaymentGPSOrder
                {
                    OrgId = orgId,
                    PaymentGPSNo = "2603PG0002",
                    PmtMonth = "2026-03",
                    FromDate = new DateTime(2026, 3, 1),
                    ToDate = new DateTime(2026, 3, 31),
                    TotalCars = 3,
                    AmountTotal = 117000m,
                    VatRate = 10m,
                    UnitPriceVAT = 11700m,
                    TotalAmountVAT = 128700m,
                    Status = PaymentGPSStatus.Approved2,
                    HTVSignStatus = PaymentGPSSignStatus.DaKy,
                    HTVSignDate = DateTime.Today.AddDays(-2),
                    HTVSignBy = "Hoàng Quốc Việt (Kế toán Trưởng HTV)",
                    TCMSSignStatus = PaymentGPSSignStatus.ChuaKy,
                    FilePath = "/reports/payment-gps/2603PG0002_HTVSigned.pdf",
                    Remark = "Bảng kê chi phí GPS kỳ 03/2026 HTV đã thẩm duyệt và ký số, đang chờ TCMS ký xác nhận",
                    CreatedBy = "HTC_GPS_ADMIN",
                    CreatedAt = DateTime.Today.AddDays(-5),
                    Approved1At = DateTime.Today.AddDays(-3),
                    Approved1By = "Trần Văn Long (Trưởng phòng Bán hàng HTV)",
                    Approved2At = DateTime.Today.AddDays(-2),
                    Approved2By = "Hoàng Quốc Việt (Kế toán Trưởng HTV)",
                    Details = new List<PaymentGPSDetail>
                    {
                        new()
                        {
                            PaymentGPSNo = "2603PG0002",
                            Vin = "KMHE281BBSA556677",
                            CarId = "CAR2026-CU0211",
                            ModelCode = "CUSTIN",
                            ModelName = "Hyundai Custin 1.5T-GDi Cao Cấp",
                            SpecCode = "CU15-PRE-01",
                            ColorCode = "SL1",
                            DealerCode = "VN001",
                            DealerName = "Hyundai Đông Đô",
                            GPSDvNo = "GPS-556677",
                            GPSStartDate = new DateTime(2026, 2, 15),
                            CostGPSStartDate = new DateTime(2026, 3, 1),
                            CostGPSEndDate = new DateTime(2026, 3, 31),
                            PlanCostGPSDate = 31,
                            DeductDate = 0,
                            ActualCostGPSDate = 31,
                            PriceGPS = 1500m,
                            AmountGPS = 46500m,
                            Remark = "Tính đủ 31 ngày trong tháng"
                        },
                        new()
                        {
                            PaymentGPSNo = "2603PG0002",
                            Vin = "KMHE281BBSA778899",
                            CarId = "CAR2026-SG0109",
                            ModelCode = "STARGAZER",
                            ModelName = "Hyundai Stargazer X 1.5 Cao Cấp",
                            SpecCode = "SG15-PRE-01",
                            ColorCode = "MB1",
                            DealerCode = "VN002",
                            DealerName = "Hyundai Nam Trung",
                            GPSDvNo = "GPS-778899",
                            GPSStartDate = new DateTime(2026, 2, 20),
                            RetailDate = new DateTime(2026, 3, 20),
                            CostGPSStartDate = new DateTime(2026, 3, 1),
                            CostGPSEndDate = new DateTime(2026, 3, 20),
                            PlanCostGPSDate = 20,
                            DeductDate = 0,
                            ActualCostGPSDate = 20,
                            PriceGPS = 1500m,
                            AmountGPS = 30000m,
                            Remark = "Xe đã bàn giao giao dịch bán lẻ ngày 20/03, dừng tính phí từ ngày giao"
                        },
                        new()
                        {
                            PaymentGPSNo = "2603PG0002",
                            Vin = "KMHE281BBSA900101",
                            CarId = "CAR2026-PAL-001",
                            ModelCode = "PALISADE",
                            ModelName = "Hyundai Palisade 2.2D Exclusive",
                            SpecCode = "PAL-EXC-01",
                            ColorCode = "WW1",
                            GPSDvNo = "GPS-900101",
                            GPSStartDate = new DateTime(2026, 3, 5),
                            CostGPSStartDate = new DateTime(2026, 3, 5),
                            CostGPSEndDate = new DateTime(2026, 3, 31),
                            PlanCostGPSDate = 27,
                            DeductDate = 0,
                            ActualCostGPSDate = 27,
                            PriceGPS = 1500m,
                            AmountGPS = 40500m,
                            Remark = "Lắp GPS sau khi hoàn tất thủ tục hải quan ngày 05/03"
                        }
                    }
                };

                var pg3 = new PaymentGPSOrder
                {
                    OrgId = orgId,
                    PaymentGPSNo = "2603PG0003",
                    PmtMonth = "2026-03",
                    FromDate = new DateTime(2026, 3, 1),
                    ToDate = new DateTime(2026, 3, 31),
                    TotalCars = 2,
                    AmountTotal = 88000m,
                    VatRate = 10m,
                    UnitPriceVAT = 8800m,
                    TotalAmountVAT = 96800m,
                    Status = PaymentGPSStatus.Pending,
                    HTVSignStatus = PaymentGPSSignStatus.ChuaKy,
                    TCMSSignStatus = PaymentGPSSignStatus.ChuaKy,
                    Remark = "Bảng kê chi phí thiết bị GPS đợt bổ sung xe điện Ioniq 5 cập bến tháng 3/2026, chờ phê duyệt cấp 1",
                    CreatedBy = "HTC_LOGISTICS_GPS",
                    CreatedAt = DateTime.Today.AddDays(-2),
                    Details = new List<PaymentGPSDetail>
                    {
                        new()
                        {
                            PaymentGPSNo = "2603PG0003",
                            Vin = "KMHE281BBSA900301",
                            CarId = "CAR2026-IQ5-001",
                            ModelCode = "IONIQ5",
                            ModelName = "Hyundai Ioniq 5 Prestige",
                            SpecCode = "IQ5-PRE-01",
                            ColorCode = "WW1",
                            GPSDvNo = "GPS-900301",
                            GPSStartDate = new DateTime(2026, 3, 10),
                            CostGPSStartDate = new DateTime(2026, 3, 10),
                            CostGPSEndDate = new DateTime(2026, 3, 31),
                            PlanCostGPSDate = 22,
                            DeductDate = 0,
                            ActualCostGPSDate = 22,
                            PriceGPS = 2000m,
                            AmountGPS = 44000m,
                            Remark = "Gói VIP định vị xe điện 2,000 đ/ngày"
                        },
                        new()
                        {
                            PaymentGPSNo = "2603PG0003",
                            Vin = "KMHE281BBSA900302",
                            CarId = "CAR2026-IQ5-002",
                            ModelCode = "IONIQ5",
                            ModelName = "Hyundai Ioniq 5 Exclusive",
                            SpecCode = "IQ5-EXC-01",
                            ColorCode = "SL1",
                            GPSDvNo = "GPS-900302",
                            GPSStartDate = new DateTime(2026, 3, 10),
                            CostGPSStartDate = new DateTime(2026, 3, 10),
                            CostGPSEndDate = new DateTime(2026, 3, 31),
                            PlanCostGPSDate = 22,
                            DeductDate = 0,
                            ActualCostGPSDate = 22,
                            PriceGPS = 2000m,
                            AmountGPS = 44000m,
                            Remark = "Gói VIP định vị xe điện 2,000 đ/ngày"
                        }
                    }
                };

                var pg4 = new PaymentGPSOrder
                {
                    OrgId = orgId,
                    PaymentGPSNo = "2603PG0004",
                    PmtMonth = "2026-03",
                    FromDate = new DateTime(2026, 3, 1),
                    ToDate = new DateTime(2026, 3, 31),
                    TotalCars = 2,
                    AmountTotal = 51000m,
                    VatRate = 10m,
                    UnitPriceVAT = 5100m,
                    TotalAmountVAT = 56100m,
                    Status = PaymentGPSStatus.Draft,
                    HTVSignStatus = PaymentGPSSignStatus.ChuaKy,
                    TCMSSignStatus = PaymentGPSSignStatus.ChuaKy,
                    Remark = "Bảng kê nháp xe lô Stargazer X đang kiểm tra đối chiếu dữ liệu thiết bị viễn thông",
                    CreatedBy = "HTC_GPS_ADMIN",
                    CreatedAt = DateTime.Today.AddDays(-1),
                    Details = new List<PaymentGPSDetail>
                    {
                        new()
                        {
                            PaymentGPSNo = "2603PG0004",
                            Vin = "KMHE281BBSA900401",
                            CarId = "CAR2026-SG-001",
                            ModelCode = "STARGAZER",
                            ModelName = "Hyundai Stargazer X Cao Cấp",
                            SpecCode = "SG-PRE-01",
                            ColorCode = "WW1",
                            GPSDvNo = "GPS-900401",
                            GPSStartDate = new DateTime(2026, 3, 15),
                            CostGPSStartDate = new DateTime(2026, 3, 15),
                            CostGPSEndDate = new DateTime(2026, 3, 31),
                            PlanCostGPSDate = 17,
                            DeductDate = 0,
                            ActualCostGPSDate = 17,
                            PriceGPS = 1500m,
                            AmountGPS = 25500m,
                            Remark = "Lô xe Stargazer mới map GPS"
                        },
                        new()
                        {
                            PaymentGPSNo = "2603PG0004",
                            Vin = "KMHE281BBSA900402",
                            CarId = "CAR2026-SG-002",
                            ModelCode = "STARGAZER",
                            ModelName = "Hyundai Stargazer X Tiêu Chuẩn",
                            SpecCode = "SG-STD-01",
                            ColorCode = "BK1",
                            GPSDvNo = "GPS-900402",
                            GPSStartDate = new DateTime(2026, 3, 15),
                            CostGPSStartDate = new DateTime(2026, 3, 15),
                            CostGPSEndDate = new DateTime(2026, 3, 31),
                            PlanCostGPSDate = 17,
                            DeductDate = 0,
                            ActualCostGPSDate = 17,
                            PriceGPS = 1500m,
                            AmountGPS = 25500m,
                            Remark = "Lô xe Stargazer mới map GPS"
                        }
                    }
                };

                db.PaymentGPSOrders.AddRange(pg1, pg2, pg3, pg4);
            }

            // Đồng bộ DeclarationNo cho các xe trong kho xe CarVinInventory
            var v1 = await db.CarVinInventories.FirstOrDefaultAsync(x => x.OrgId == orgId && x.Vin == "KMHE281BBSA900101");
            if (v1 != null) { v1.DeclarationNo = "2603TK00001"; v1.CustomsClearanceDate = DateTime.Today.AddDays(-12); }
            var v2 = await db.CarVinInventories.FirstOrDefaultAsync(x => x.OrgId == orgId && x.Vin == "KMHE281BBSA900102");
            if (v2 != null) { v2.DeclarationNo = "2603TK00001"; v2.CustomsClearanceDate = DateTime.Today.AddDays(-12); }
            var v3 = await db.CarVinInventories.FirstOrDefaultAsync(x => x.OrgId == orgId && x.Vin == "KMHE281BBSA900103");
            if (v3 != null) { v3.DeclarationNo = "2603TK00001"; v3.CustomsClearanceDate = DateTime.Today.AddDays(-12); }

            // Bổ sung thêm 2 xe CBU khả dụng chưa mở tờ khai để phục vụ test chức năng lập tờ khai mới
            if (!await db.CarVinInventories.AnyAsync(x => x.OrgId == orgId && x.Vin == "KMHE281BBSC990001"))
            {
                db.CarVinInventories.AddRange(
                    new CarVinInventory
                    {
                        OrgId = orgId,
                        Vin = "KMHE281BBSC990001",
                        ModelCode = "PALISADE",
                        ModelName = "Hyundai Palisade 2.2D Prestige",
                        SpecCode = "PAL-PRE-01",
                        SpecDescription = "Palisade 2.2 Diesel Prestige 6 chỗ",
                        ColorCode = "WW1",
                        ColorName = "Trắng ngọc trai",
                        EngineNo = "D4HB-990001",
                        StorageCode = "KHO_CANG_HP",
                        StorageName = "Kho Bãi Cảng Hải Phòng Đình Vũ",
                        AssemblyStatus = "CBU",
                        ProductionMonth = "202602",
                        Status = CarVinStatus.Available,
                        CreatedAt = DateTime.Today.AddDays(-3)
                    },
                    new CarVinInventory
                    {
                        OrgId = orgId,
                        Vin = "KMHE281BBSC990002",
                        ModelCode = "IONIQ5",
                        ModelName = "Hyundai Ioniq 5 Prestige",
                        SpecCode = "IQ5-PRE-01",
                        SpecDescription = "Ioniq 5 EV Prestige pin 72.6 kWh",
                        ColorCode = "SL1",
                        ColorName = "Bạc Gravity Gold",
                        EngineNo = "EM07-990002",
                        StorageCode = "KHO_CANG_CM",
                        StorageName = "Kho Bãi Cảng Cái Mép Vũng Tàu",
                        AssemblyStatus = "CBU",
                        ProductionMonth = "202602",
                        Status = CarVinStatus.Available,
                        CreatedAt = DateTime.Today.AddDays(-3)
                    }
                );
            }

            // Seed master data bảng đơn giá định mức lưu kho & bạt che phủ Mst_InventoryCost
            if (!await db.InventoryCosts.AnyAsync())
            {
                var costs = new List<InventoryCostMaster>
                {
                    // KHO_NB: Kho Nhà máy Ninh Bình
                    new() { CostTypeCode = "BCP", CostTypeName = "Bạt Che Phủ", StorageCode = "KHO_NB", StorageName = "Kho Nhà máy Ninh Bình", ModelCode = "SANTAFE", ModelName = "Santa Fe", UnitPrice = 35000m, IsActive = true },
                    new() { CostTypeCode = "BCP", CostTypeName = "Bạt Che Phủ", StorageCode = "KHO_NB", StorageName = "Kho Nhà máy Ninh Bình", ModelCode = "TUCSON", ModelName = "Tucson", UnitPrice = 30000m, IsActive = true },
                    new() { CostTypeCode = "BCP", CostTypeName = "Bạt Che Phủ", StorageCode = "KHO_NB", StorageName = "Kho Nhà máy Ninh Bình", ModelCode = "CRETA", ModelName = "Creta", UnitPrice = 25000m, IsActive = true },
                    new() { CostTypeCode = "BCP", CostTypeName = "Bạt Che Phủ", StorageCode = "KHO_NB", StorageName = "Kho Nhà máy Ninh Bình", ModelCode = "ACCENT", ModelName = "Accent", UnitPrice = 20000m, IsActive = true },
                    new() { CostTypeCode = "BCP", CostTypeName = "Bạt Che Phủ", StorageCode = "KHO_NB", StorageName = "Kho Nhà máy Ninh Bình", ModelCode = "STARGAZER", ModelName = "Stargazer", UnitPrice = 25000m, IsActive = true },
                    new() { CostTypeCode = "BCP", CostTypeName = "Bạt Che Phủ", StorageCode = "KHO_NB", StorageName = "Kho Nhà máy Ninh Bình", ModelCode = "PALISADE", ModelName = "Palisade", UnitPrice = 40000m, IsActive = true },
                    new() { CostTypeCode = "BCP", CostTypeName = "Bạt Che Phủ", StorageCode = "KHO_NB", StorageName = "Kho Nhà máy Ninh Bình", ModelCode = "IONIQ5", ModelName = "Ioniq 5", UnitPrice = 40000m, IsActive = true },

                    new() { CostTypeCode = "LK", CostTypeName = "Lưu Kho Bãi", StorageCode = "KHO_NB", StorageName = "Kho Nhà máy Ninh Bình", ModelCode = "SANTAFE", ModelName = "Santa Fe", UnitPrice = 50000m, IsActive = true },
                    new() { CostTypeCode = "LK", CostTypeName = "Lưu Kho Bãi", StorageCode = "KHO_NB", StorageName = "Kho Nhà máy Ninh Bình", ModelCode = "TUCSON", ModelName = "Tucson", UnitPrice = 45000m, IsActive = true },
                    new() { CostTypeCode = "LK", CostTypeName = "Lưu Kho Bãi", StorageCode = "KHO_NB", StorageName = "Kho Nhà máy Ninh Bình", ModelCode = "CRETA", ModelName = "Creta", UnitPrice = 40000m, IsActive = true },
                    new() { CostTypeCode = "LK", CostTypeName = "Lưu Kho Bãi", StorageCode = "KHO_NB", StorageName = "Kho Nhà máy Ninh Bình", ModelCode = "ACCENT", ModelName = "Accent", UnitPrice = 35000m, IsActive = true },
                    new() { CostTypeCode = "LK", CostTypeName = "Lưu Kho Bãi", StorageCode = "KHO_NB", StorageName = "Kho Nhà máy Ninh Bình", ModelCode = "STARGAZER", ModelName = "Stargazer", UnitPrice = 40000m, IsActive = true },
                    new() { CostTypeCode = "LK", CostTypeName = "Lưu Kho Bãi", StorageCode = "KHO_NB", StorageName = "Kho Nhà máy Ninh Bình", ModelCode = "PALISADE", ModelName = "Palisade", UnitPrice = 60000m, IsActive = true },
                    new() { CostTypeCode = "LK", CostTypeName = "Lưu Kho Bãi", StorageCode = "KHO_NB", StorageName = "Kho Nhà máy Ninh Bình", ModelCode = "IONIQ5", ModelName = "Ioniq 5", UnitPrice = 60000m, IsActive = true },

                    // KHO_DY: Kho Trung chuyển Duyên Yên
                    new() { CostTypeCode = "BCP", CostTypeName = "Bạt Che Phủ", StorageCode = "KHO_DY", StorageName = "Kho Trung chuyển Duyên Yên", ModelCode = "SANTAFE", ModelName = "Santa Fe", UnitPrice = 35000m, IsActive = true },
                    new() { CostTypeCode = "BCP", CostTypeName = "Bạt Che Phủ", StorageCode = "KHO_DY", StorageName = "Kho Trung chuyển Duyên Yên", ModelCode = "TUCSON", ModelName = "Tucson", UnitPrice = 30000m, IsActive = true },
                    new() { CostTypeCode = "BCP", CostTypeName = "Bạt Che Phủ", StorageCode = "KHO_DY", StorageName = "Kho Trung chuyển Duyên Yên", ModelCode = "ACCENT", ModelName = "Accent", UnitPrice = 20000m, IsActive = true },
                    new() { CostTypeCode = "LK", CostTypeName = "Lưu Kho Bãi", StorageCode = "KHO_DY", StorageName = "Kho Trung chuyển Duyên Yên", ModelCode = "SANTAFE", ModelName = "Santa Fe", UnitPrice = 50000m, IsActive = true },
                    new() { CostTypeCode = "LK", CostTypeName = "Lưu Kho Bãi", StorageCode = "KHO_DY", StorageName = "Kho Trung chuyển Duyên Yên", ModelCode = "TUCSON", ModelName = "Tucson", UnitPrice = 45000m, IsActive = true },
                    new() { CostTypeCode = "LK", CostTypeName = "Lưu Kho Bãi", StorageCode = "KHO_DY", StorageName = "Kho Trung chuyển Duyên Yên", ModelCode = "ACCENT", ModelName = "Accent", UnitPrice = 35000m, IsActive = true },

                    // KHO_CANG_HP: Kho Bãi Cảng Đình Vũ Hải Phòng
                    new() { CostTypeCode = "BCP", CostTypeName = "Bạt Che Phủ", StorageCode = "KHO_CANG_HP", StorageName = "Kho Bãi Cảng Hải Phòng Đình Vũ", ModelCode = "PALISADE", ModelName = "Palisade", UnitPrice = 45000m, IsActive = true },
                    new() { CostTypeCode = "BCP", CostTypeName = "Bạt Che Phủ", StorageCode = "KHO_CANG_HP", StorageName = "Kho Bãi Cảng Hải Phòng Đình Vũ", ModelCode = "IONIQ5", ModelName = "Ioniq 5", UnitPrice = 45000m, IsActive = true },
                    new() { CostTypeCode = "LK", CostTypeName = "Lưu Kho Bãi", StorageCode = "KHO_CANG_HP", StorageName = "Kho Bãi Cảng Hải Phòng Đình Vũ", ModelCode = "PALISADE", ModelName = "Palisade", UnitPrice = 65000m, IsActive = true },
                    new() { CostTypeCode = "LK", CostTypeName = "Lưu Kho Bãi", StorageCode = "KHO_CANG_HP", StorageName = "Kho Bãi Cảng Hải Phòng Đình Vũ", ModelCode = "IONIQ5", ModelName = "Ioniq 5", UnitPrice = 65000m, IsActive = true },

                    // KHO_CANG_CM: Kho Bãi Cảng Cái Mép Vũng Tàu
                    new() { CostTypeCode = "BCP", CostTypeName = "Bạt Che Phủ", StorageCode = "KHO_CANG_CM", StorageName = "Kho Bãi Cảng Cái Mép Vũng Tàu", ModelCode = "PALISADE", ModelName = "Palisade", UnitPrice = 45000m, IsActive = true },
                    new() { CostTypeCode = "BCP", CostTypeName = "Bạt Che Phủ", StorageCode = "KHO_CANG_CM", StorageName = "Kho Bãi Cảng Cái Mép Vũng Tàu", ModelCode = "IONIQ5", ModelName = "Ioniq 5", UnitPrice = 45000m, IsActive = true },
                    new() { CostTypeCode = "LK", CostTypeName = "Lưu Kho Bãi", StorageCode = "KHO_CANG_CM", StorageName = "Kho Bãi Cảng Cái Mép Vũng Tàu", ModelCode = "PALISADE", ModelName = "Palisade", UnitPrice = 65000m, IsActive = true },
                    new() { CostTypeCode = "LK", CostTypeName = "Lưu Kho Bãi", StorageCode = "KHO_CANG_CM", StorageName = "Kho Bãi Cảng Cái Mép Vũng Tàu", ModelCode = "IONIQ5", ModelName = "Ioniq 5", UnitPrice = 65000m, IsActive = true }
                };
                db.InventoryCosts.AddRange(costs);
            }

            // Seed bảng kê quyết toán chi phí lưu kho xe Pmt_PaymentStorage
            if (!await db.PaymentStorageOrders.AnyAsync(o => o.OrgId == orgId))
            {
                var pst1 = new PaymentStorageOrder
                {
                    OrgId = orgId,
                    PaymentStorageNo = "2603PST00001",
                    PmtMonth = "2026-03",
                    PmtPeriodStartDTime = new DateTime(2026, 3, 1),
                    PmtPeriodEndDTime = new DateTime(2026, 3, 15),
                    VAT = 10m,
                    TotalCars = 3,
                    AmountBCP = 1275000m,
                    AmountLK = 1950000m,
                    AmountTotal = 3225000m,
                    AmountVAT = 322500m,
                    AmountVATTotal = 3547500m,
                    Status = PaymentStorageStatus.Finished,
                    HTVSignStatus = PaymentStorageSignStatus.DaKy,
                    HTVSignDTime = new DateTime(2026, 3, 17, 10, 30, 0),
                    HTVSignBy = "TGD_HTV_ESIGN",
                    TCMSSignStatus = PaymentStorageSignStatus.DaKy,
                    TCMSSignDTime = new DateTime(2026, 3, 16, 15, 0, 0),
                    TCMSSignBy = "GD_TCMS_ESIGN",
                    FilePath = "/esign/htv/2603PST00001_contract_signed.pdf",
                    FileUrl = "/esign/htv/2603PST00001_contract_signed.pdf",
                    FileInvPath = "HD_TCMS_2603_0019.pdf",
                    FileInvUrl = "/invoices/storage/HD_TCMS_2603_0019.pdf",
                    InvoiceNo = "HD-TCMS-2603-0019",
                    InvoiceDate = new DateTime(2026, 3, 18),
                    Remark = "Quyết toán chi phí lưu kho bãi và bọc che phủ đợt 1 tháng 3/2026 tại Kho Ninh Bình và Duyên Yên",
                    CreatedBy = "HTC_STORAGE_ADMIN",
                    CreatedAt = new DateTime(2026, 3, 15, 8, 0, 0),
                    Approve1At = new DateTime(2026, 3, 15, 14, 0, 0),
                    Approve1By = "TP_KHO_VAN_HTC",
                    Approve2At = new DateTime(2026, 3, 16, 9, 30, 0),
                    Approve2By = "LANH_DAO_HTC",
                    Details = new List<PaymentStorageDetail>
                    {
                        new()
                        {
                            PaymentStorageNo = "2603PST00001",
                            Vin = "KMHE281BBSA129841",
                            RefNo = "DO2603010001",
                            CarId = "CAR2026-SF0101",
                            ModelCode = "SANTAFE",
                            ModelName = "Santa Fe 2.5 HTRAC",
                            SpecCode = "SF25-PRE-01",
                            ColorCode = "WW1",
                            StorageCode = "KHO_NB",
                            StorageName = "Kho Nhà máy Ninh Bình",
                            StorageDateIn = new DateTime(2026, 2, 20),
                            StorageDateOut = new DateTime(2026, 3, 15),
                            DateBegin = new DateTime(2026, 3, 1),
                            DateEnd = new DateTime(2026, 3, 15),
                            DelayTransport = 0,
                            DateCount = 15,
                            PriceBCP = 35000m,
                            PriceLK = 50000m,
                            AmountBCP = 525000m,
                            AmountLK = 750000m,
                            AmountTotal = 1275000m,
                            DealerCode = "VN001",
                            DealerName = "Hyundai Đông Đô",
                            DeliveryOrderNo = "DO2603010001",
                            Remark = "Lưu bãi đạt chuẩn bọc che phủ"
                        },
                        new()
                        {
                            PaymentStorageNo = "2603PST00001",
                            Vin = "KMHE281BBSA987654",
                            RefNo = "DO2602150002",
                            CarId = "CAR2026-TU1102",
                            ModelCode = "TUCSON",
                            ModelName = "Tucson 2.0 AT",
                            SpecCode = "TU20-STD-01",
                            ColorCode = "NKA",
                            StorageCode = "KHO_NB",
                            StorageName = "Kho Nhà máy Ninh Bình",
                            StorageDateIn = new DateTime(2026, 2, 25),
                            StorageDateOut = new DateTime(2026, 3, 15),
                            DateBegin = new DateTime(2026, 3, 1),
                            DateEnd = new DateTime(2026, 3, 15),
                            DelayTransport = 0,
                            DateCount = 15,
                            PriceBCP = 30000m,
                            PriceLK = 45000m,
                            AmountBCP = 450000m,
                            AmountLK = 675000m,
                            AmountTotal = 1125000m,
                            DealerCode = "VN001",
                            DealerName = "Hyundai Đông Đô",
                            DeliveryOrderNo = "DO2602150002",
                            Remark = "Xe xuất giao đại lý hoàn tất"
                        },
                        new()
                        {
                            PaymentStorageNo = "2603PST00001",
                            Vin = "KMHE281BBSA667788",
                            RefNo = "PMT26090003",
                            CarId = "CAR2026-AC9011",
                            ModelCode = "ACCENT",
                            ModelName = "Accent 1.4 AT",
                            SpecCode = "AC14-AT-01",
                            ColorCode = "WH1",
                            StorageCode = "KHO_DY",
                            StorageName = "Kho Trung chuyển Duyên Yên",
                            StorageDateIn = new DateTime(2026, 3, 1),
                            StorageDateOut = new DateTime(2026, 3, 15),
                            DateBegin = new DateTime(2026, 3, 1),
                            DateEnd = new DateTime(2026, 3, 15),
                            DelayTransport = 0,
                            DateCount = 15,
                            PriceBCP = 20000m,
                            PriceLK = 35000m,
                            AmountBCP = 300000m,
                            AmountLK = 525000m,
                            AmountTotal = 825000m,
                            DealerCode = "VN001",
                            DealerName = "Hyundai Đông Đô",
                            Remark = "Xe Accent chuyển kho Duyên Yên"
                        }
                    }
                };

                var pst2 = new PaymentStorageOrder
                {
                    OrgId = orgId,
                    PaymentStorageNo = "2603PST00002",
                    PmtMonth = "2026-03",
                    PmtPeriodStartDTime = new DateTime(2026, 3, 16),
                    PmtPeriodEndDTime = new DateTime(2026, 3, 31),
                    VAT = 10m,
                    TotalCars = 2,
                    AmountBCP = 1440000m,
                    AmountLK = 2080000m,
                    AmountTotal = 3520000m,
                    AmountVAT = 352000m,
                    AmountVATTotal = 3872000m,
                    Status = PaymentStorageStatus.Approved2,
                    HTVSignStatus = PaymentStorageSignStatus.ChuaKy,
                    TCMSSignStatus = PaymentStorageSignStatus.ChuaKy,
                    Remark = "Bảng kê quyết toán đợt 2 tháng 3/2026 đã được Lãnh đạo HTC duyệt, chờ ký số 2 bên",
                    CreatedBy = "HTC_STORAGE_ADMIN",
                    CreatedAt = new DateTime(2026, 3, 31, 10, 0, 0),
                    Approve1At = new DateTime(2026, 3, 31, 14, 0, 0),
                    Approve1By = "TP_KHO_VAN_HTC",
                    Approve2At = new DateTime(2026, 3, 31, 16, 30, 0),
                    Approve2By = "LANH_DAO_HTC",
                    Details = new List<PaymentStorageDetail>
                    {
                        new()
                        {
                            PaymentStorageNo = "2603PST00002",
                            Vin = "KMHE281BBSA900101",
                            RefNo = "REF-26030001",
                            CarId = "CAR2026-PAL-001",
                            ModelCode = "PALISADE",
                            ModelName = "Hyundai Palisade 2.2D Prestige",
                            SpecCode = "PAL-PRE-01",
                            ColorCode = "WW1",
                            StorageCode = "KHO_CANG_HP",
                            StorageName = "Kho Bãi Cảng Hải Phòng Đình Vũ",
                            StorageDateIn = new DateTime(2026, 3, 10),
                            StorageDateOut = null,
                            DateBegin = new DateTime(2026, 3, 16),
                            DateEnd = new DateTime(2026, 3, 31),
                            DelayTransport = 0,
                            DateCount = 16,
                            PriceBCP = 45000m,
                            PriceLK = 65000m,
                            AmountBCP = 720000m,
                            AmountLK = 1040000m,
                            AmountTotal = 1760000m,
                            DealerCode = "VN001",
                            DealerName = "Hyundai Đông Đô",
                            PackingListNo = "2603PL0001",
                            Remark = "Xe CBU lưu bãi cảng Đình Vũ bọc phủ tiêu chuẩn"
                        },
                        new()
                        {
                            PaymentStorageNo = "2603PST00002",
                            Vin = "KMHE281BBSA900102",
                            RefNo = "REF-26030002",
                            CarId = "CAR2026-IQ5-001",
                            ModelCode = "IONIQ5",
                            ModelName = "Hyundai Ioniq 5 Prestige",
                            SpecCode = "IQ5-PRE-01",
                            ColorCode = "SL1",
                            StorageCode = "KHO_CANG_CM",
                            StorageName = "Kho Bãi Cảng Cái Mép Vũng Tàu",
                            StorageDateIn = new DateTime(2026, 3, 12),
                            StorageDateOut = null,
                            DateBegin = new DateTime(2026, 3, 16),
                            DateEnd = new DateTime(2026, 3, 31),
                            DelayTransport = 0,
                            DateCount = 16,
                            PriceBCP = 45000m,
                            PriceLK = 65000m,
                            AmountBCP = 720000m,
                            AmountLK = 1040000m,
                            AmountTotal = 1760000m,
                            DealerCode = "VS058",
                            DealerName = "Hyundai Miền Nam",
                            PackingListNo = "2603PL0001",
                            Remark = "Xe điện Ioniq 5 lưu kho Cái Mép"
                        }
                    }
                };

                var pst3 = new PaymentStorageOrder
                {
                    OrgId = orgId,
                    PaymentStorageNo = "2603PST00003",
                    PmtMonth = "2026-03",
                    PmtPeriodStartDTime = new DateTime(2026, 3, 1),
                    PmtPeriodEndDTime = new DateTime(2026, 3, 20),
                    VAT = 10m,
                    TotalCars = 1,
                    AmountBCP = 500000m,
                    AmountLK = 800000m,
                    AmountTotal = 1300000m,
                    AmountVAT = 130000m,
                    AmountVATTotal = 1430000m,
                    Status = PaymentStorageStatus.Approved1,
                    HTVSignStatus = PaymentStorageSignStatus.ChuaKy,
                    TCMSSignStatus = PaymentStorageSignStatus.ChuaKy,
                    Remark = "Bảng kê lô xe Stargazer đã được Trưởng phòng bán hàng & kho duyệt cấp 1",
                    CreatedBy = "HTC_STORAGE_ADMIN",
                    CreatedAt = DateTime.Today.AddDays(-5),
                    Approve1At = DateTime.Today.AddDays(-4),
                    Approve1By = "TP_KHO_VAN_HTC",
                    Details = new List<PaymentStorageDetail>
                    {
                        new()
                        {
                            PaymentStorageNo = "2603PST00003",
                            Vin = "KMHE281BBSA900401",
                            RefNo = "REF-26030003",
                            CarId = "CAR2026-SG-001",
                            ModelCode = "STARGAZER",
                            ModelName = "Hyundai Stargazer X Cao Cấp",
                            SpecCode = "SG-PRE-01",
                            ColorCode = "WW1",
                            StorageCode = "KHO_NB",
                            StorageName = "Kho Nhà máy Ninh Bình",
                            StorageDateIn = new DateTime(2026, 3, 1),
                            StorageDateOut = new DateTime(2026, 3, 20),
                            DateBegin = new DateTime(2026, 3, 1),
                            DateEnd = new DateTime(2026, 3, 20),
                            DelayTransport = 0,
                            DateCount = 20,
                            PriceBCP = 25000m,
                            PriceLK = 40000m,
                            AmountBCP = 500000m,
                            AmountLK = 800000m,
                            AmountTotal = 1300000m,
                            DealerCode = "VN001",
                            DealerName = "Hyundai Đông Đô",
                            Remark = "Lô xe Stargazer lưu kho bãi"
                        }
                    }
                };

                var pst4 = new PaymentStorageOrder
                {
                    OrgId = orgId,
                    PaymentStorageNo = "2603PST00004",
                    PmtMonth = "2026-03",
                    PmtPeriodStartDTime = new DateTime(2026, 3, 21),
                    PmtPeriodEndDTime = new DateTime(2026, 3, 31),
                    VAT = 10m,
                    TotalCars = 2,
                    AmountBCP = 550000m,
                    AmountLK = 850000m,
                    AmountTotal = 1400000m,
                    AmountVAT = 140000m,
                    AmountVATTotal = 1540000m,
                    Status = PaymentStorageStatus.Pending,
                    HTVSignStatus = PaymentStorageSignStatus.ChuaKy,
                    TCMSSignStatus = PaymentStorageSignStatus.ChuaKy,
                    Remark = "Bảng kê nháp đang kiểm tra đối soát số ngày khấu trừ trễ vận chuyển với TCMS",
                    CreatedBy = "HTC_STORAGE_ADMIN",
                    CreatedAt = DateTime.Today.AddDays(-1),
                    Details = new List<PaymentStorageDetail>
                    {
                        new()
                        {
                            PaymentStorageNo = "2603PST00004",
                            Vin = "KMHE281BBSA900402",
                            RefNo = "REF-26030004",
                            CarId = "CAR2026-SG-002",
                            ModelCode = "STARGAZER",
                            ModelName = "Hyundai Stargazer X Tiêu Chuẩn",
                            SpecCode = "SG-STD-01",
                            ColorCode = "BK1",
                            StorageCode = "KHO_NB",
                            StorageName = "Kho Nhà máy Ninh Bình",
                            StorageDateIn = new DateTime(2026, 3, 21),
                            StorageDateOut = null,
                            DateBegin = new DateTime(2026, 3, 21),
                            DateEnd = new DateTime(2026, 3, 31),
                            DelayTransport = 1,
                            DateCount = 10,
                            PriceBCP = 25000m,
                            PriceLK = 40000m,
                            AmountBCP = 250000m,
                            AmountLK = 400000m,
                            AmountTotal = 650000m,
                            DealerCode = "VN001",
                            DealerName = "Hyundai Đông Đô",
                            Remark = "Có khấu trừ 1 ngày trễ xe"
                        },
                        new()
                        {
                            PaymentStorageNo = "2603PST00004",
                            Vin = "KMHE281BBSA900103",
                            RefNo = "REF-26030005",
                            CarId = "CAR2026-CR-003",
                            ModelCode = "CRETA",
                            ModelName = "Hyundai Creta 1.5 Cao Cấp",
                            SpecCode = "CR15-PRE-01",
                            ColorCode = "GY1",
                            StorageCode = "KHO_NB",
                            StorageName = "Kho Nhà máy Ninh Bình",
                            StorageDateIn = new DateTime(2026, 3, 22),
                            StorageDateOut = null,
                            DateBegin = new DateTime(2026, 3, 22),
                            DateEnd = new DateTime(2026, 3, 31),
                            DelayTransport = 0,
                            DateCount = 10,
                            PriceBCP = 25000m,
                            PriceLK = 40000m,
                            AmountBCP = 250000m,
                            AmountLK = 400000m,
                            AmountTotal = 650000m,
                            DealerCode = "VN012",
                            DealerName = "Hyundai Lê Văn Lương",
                            Remark = "Xe Creta chờ xuất kho"
                        }
                    }
                };

                db.PaymentStorageOrders.AddRange(pst1, pst2, pst3, pst4);
            }

            if (!await db.CarVinProfiles.AnyAsync(o => o.OrgId == orgId))
            {
                var vp1 = new CarVinProfile
                {
                    OrgId = orgId,
                    Vin = "KMHE281BBSA129841",
                    CarId = "CAR2026-SF0988",
                    DealerCode = "VN001",
                    DealerName = "Hyundai Đông Đô",
                    ModelCode = "SANTAFE",
                    ModelName = "Hyundai Santa Fe 2.5 HTRAC",
                    SpecCode = "SF25-PRE-01",
                    SpecDescription = "Santa Fe 2.5 xăng Cao Cấp dẫn động 4 bánh HTRAC",
                    ColorCode = "WW2",
                    ColorName = "Trắng ngọc trai",
                    EngineNo = "G4KP-1029384",
                    StorageCode = "KHO_NB",
                    StorageName = "Kho Nhà máy Hyundai Ninh Bình",
                    CQNo = "CQ-2026-SF-00129",
                    CQStartDate = new DateTime(2026, 2, 10),
                    CQEndDate = new DateTime(2029, 2, 10),
                    FGFormNo = "PXX-2026/02-8812",
                    QCNo = "QC-HTV-SF-9912",
                    IssuedDate = new DateTime(2026, 2, 12),
                    CONo = "CO-VN-2026-08129",
                    CODate = new DateTime(2026, 2, 11),
                    InvoiceNoFactory = "HDNM-2026-003412",
                    InvoiceFactoryDate = new DateTime(2026, 2, 15),
                    InvoiceFactorySearch = "INV-FCT-SF-129841",
                    InvoiceNoTransferred = "HDCN-2026-000881",
                    InvoiceTransferredDate = new DateTime(2026, 2, 18),
                    InvoiceTransferredSearch = "INV-TRF-SF-129841",
                    InvoiceSpecName = "Ô tô con 07 chỗ Hyundai Santa Fe 2.5 HTRAC",
                    TypeCB = false,
                    MortageStatus = CarMortageStatus.Mortgaged,
                    MortageBankCode = "VCB",
                    MortageBankName = "Ngân hàng TMCP Ngoại thương Việt Nam (Vietcombank)",
                    MortageStartDate = new DateTime(2026, 2, 20),
                    HandOverBankCode = "VCB",
                    HandOverBankName = "Vietcombank - Chi nhánh Thăng Long",
                    BillNo = "VNPOST-BILL-992182",
                    MortageEndDate = new DateTime(2026, 2, 22),
                    GuaranteeNo = "BG2603-VCB-00128",
                    BankGuaranteeNo = "BL-VCB-2026-88129",
                    DateStart = new DateTime(2026, 2, 25),
                    DateExpired = new DateTime(2026, 8, 25),
                    GuaranteeValue = 1320000000m,
                    FlagDtlDiscount = true,
                    FlagGrtExt = false,
                    DocumentsStatus = "1",
                    FlagDocReq = "1",
                    DocDeliveryReqDate = new DateTime(2026, 3, 1),
                    SendMailDocDate = new DateTime(2026, 3, 2),
                    Remark = "Hồ sơ gốc đầy đủ, đã bàn giao ngân hàng Vietcombank, đã mở cờ cho phép đại lý rút hồ sơ",
                    CreatedAt = DateTime.Now.AddDays(-25),
                    CreatedBy = "SYSTEM_ADMIN"
                };

                var vp2 = new CarVinProfile
                {
                    OrgId = orgId,
                    Vin = "KMHE281BBSA987654",
                    CarId = "CAR2026-TU0021",
                    DealerCode = "VN002",
                    DealerName = "Hyundai Nam Trung",
                    ModelCode = "TUCSON",
                    ModelName = "Hyundai Tucson 2.0 AT",
                    SpecCode = "TU20-STD-01",
                    SpecDescription = "Tucson 2.0 AT bản Xăng Tiêu chuẩn",
                    ColorCode = "NKA",
                    ColorName = "Đen ánh kim",
                    EngineNo = "G4NL-9988771",
                    StorageCode = "KHO_DY",
                    StorageName = "Kho Dịch Vọng Cầu Giấy",
                    CQNo = "CQ-2026-TU-00812",
                    CQStartDate = new DateTime(2026, 1, 15),
                    CQEndDate = new DateTime(2029, 1, 15),
                    FGFormNo = "PXX-2026/01-4412",
                    QCNo = "QC-HTV-TU-3312",
                    IssuedDate = new DateTime(2026, 1, 18),
                    CONo = "CO-VN-2026-03312",
                    CODate = new DateTime(2026, 1, 16),
                    InvoiceNoFactory = "HDNM-2026-001198",
                    InvoiceFactoryDate = new DateTime(2026, 1, 20),
                    InvoiceFactorySearch = "INV-FCT-TU-987654",
                    InvoiceNoTransferred = "HDCN-2026-000312",
                    InvoiceTransferredDate = new DateTime(2026, 1, 22),
                    InvoiceTransferredSearch = "INV-TRF-TU-987654",
                    InvoiceSpecName = "Ô tô con 05 chỗ Hyundai Tucson 2.0 AT",
                    TypeCB = false,
                    MortageStatus = CarMortageStatus.Redeemed,
                    MortageBankCode = "TCB",
                    MortageBankName = "Ngân hàng TMCP Kỹ Thương Việt Nam (Techcombank)",
                    MortageStartDate = new DateTime(2026, 1, 25),
                    HandOverBankCode = "TCB",
                    HandOverBankName = "Techcombank - Chi nhánh Hoàn Kiếm",
                    BillNo = "EMS-TCB-881920",
                    MortageEndDate = new DateTime(2026, 1, 27),
                    RedeemDate = new DateTime(2026, 2, 28),
                    LogDateTimeStatusMortageEnd = new DateTime(2026, 2, 28, 14, 30, 0),
                    GuaranteeNo = "BG2603-TCB-00045",
                    BankGuaranteeNo = "BL-TCB-2026-11928",
                    DateStart = new DateTime(2026, 1, 28),
                    DateExpired = new DateTime(2026, 7, 28),
                    GuaranteeValue = 940000000m,
                    DocumentsStatus = "1",
                    FlagDocReq = "1",
                    DocDeliveryReqDate = new DateTime(2026, 2, 26),
                    Remark = "Đại lý đã thanh toán đủ tiền xe, ngân hàng Techcombank đã giải chấp tài sản, giao hồ sơ cho khách hàng",
                    CreatedAt = DateTime.Now.AddDays(-40),
                    CreatedBy = "DEALER_VN002"
                };

                var vp3 = new CarVinProfile
                {
                    OrgId = orgId,
                    Vin = "KMHE281BBSA334455",
                    CarId = "CAR2026-CR0192",
                    DealerCode = "VN001",
                    DealerName = "Hyundai Đông Đô",
                    ModelCode = "CRETA",
                    ModelName = "Hyundai Creta 1.5 Cao Cấp",
                    SpecCode = "CR15-PRE-01",
                    SpecDescription = "Creta 1.5 Xăng Bản Cao Cấp 2 tông màu",
                    ColorCode = "GY1",
                    ColorName = "Xám kim loại",
                    EngineNo = "G4FL-2345678",
                    StorageCode = "KHO_NB",
                    StorageName = "Kho Nhà máy Hyundai Ninh Bình",
                    CQNo = "CQ-2026-CR-00214",
                    CQStartDate = new DateTime(2026, 3, 1),
                    CQEndDate = new DateTime(2029, 3, 1),
                    FGFormNo = "PXX-2026/03-1198",
                    QCNo = "QC-HTV-CR-8821",
                    IssuedDate = new DateTime(2026, 3, 3),
                    CONo = "CO-VN-2026-09912",
                    CODate = new DateTime(2026, 3, 2),
                    TypeCB = false,
                    MortageStatus = CarMortageStatus.Mortgaged,
                    MortageBankCode = "CTG",
                    MortageBankName = "Ngân hàng TMCP Công Thương Việt Nam (VietinBank)",
                    MortageStartDate = new DateTime(2026, 3, 5),
                    HandOverBankCode = "CTG",
                    HandOverBankName = "VietinBank - Chi nhánh Đô Thành",
                    BillNo = "VIETTEL-POST-334129",
                    MortageEndDate = new DateTime(2026, 3, 7),
                    GuaranteeNo = "BG2603-CTG-00088",
                    BankGuaranteeNo = "BL-CTG-2026-99381",
                    DateStart = new DateTime(2026, 3, 8),
                    DateExpired = new DateTime(2026, 9, 8),
                    GuaranteeValue = 720000000m,
                    DocumentsStatus = "0",
                    FlagDocReq = "0",
                    Remark = "Xe đang thế chấp VietinBank, đang chờ xuất hóa đơn nhà máy và phiếu xuất xưởng hoàn chỉnh",
                    CreatedAt = DateTime.Now.AddDays(-12),
                    CreatedBy = "DEALER_VN001"
                };

                var vp4 = new CarVinProfile
                {
                    OrgId = orgId,
                    Vin = "KMHE281BBSA556677",
                    CarId = "CAR2026-H150-01",
                    DealerCode = "VN012",
                    DealerName = "Hyundai Lê Văn Lương",
                    ModelCode = "PORTER",
                    ModelName = "Hyundai New Porter 150",
                    SpecCode = "H150-TK-01",
                    SpecDescription = "Xe tải thương mại Porter H150 thùng đông lạnh tiêu chuẩn Châu Âu",
                    ColorCode = "WH1",
                    ColorName = "Trắng sứ",
                    EngineNo = "D4CB-5544332",
                    StorageCode = "KHO_NB",
                    StorageName = "Kho Nhà máy Hyundai Ninh Bình",
                    CQNo = "CQ-2026-H150-0081",
                    CQStartDate = new DateTime(2026, 2, 1),
                    CQEndDate = new DateTime(2028, 2, 1),
                    FGFormNo = "PXX-2026/02-5519",
                    QCNo = "QC-HTV-H150-221",
                    IssuedDate = new DateTime(2026, 2, 3),
                    CONo = "CO-VN-2026-04421",
                    CODate = new DateTime(2026, 2, 2),
                    InvoiceNoFactory = "HDNM-2026-002281",
                    InvoiceFactoryDate = new DateTime(2026, 2, 5),
                    InvoiceFactorySearch = "INV-FCT-H150-556677",
                    InvoiceNoTransferred = "HDCN-2026-000449",
                    InvoiceTransferredDate = new DateTime(2026, 2, 8),
                    InvoiceTransferredSearch = "INV-TRF-H150-556677",
                    InvoiceSpecName = "Ô tô tải đông lạnh Hyundai New Porter 150 tải trọng 1.49 tấn",
                    TypeCB = true,
                    LoaiThung = "THUNG_DONG_LANH",
                    CabinCertificateNo = "GCN-CB-2026-DL-088",
                    CabinCertificateDate = new DateTime(2026, 2, 20),
                    CabinCONo = "CO-CB-TC-088",
                    CabinInvoiceNo = "HDCB-2026-0012",
                    CabinInvoiceDate = new DateTime(2026, 2, 22),
                    MortageStatus = CarMortageStatus.Mortgaged,
                    MortageBankCode = "BIDV",
                    MortageBankName = "Ngân hàng TMCP Đầu tư và Phát triển Việt Nam (BIDV)",
                    MortageStartDate = new DateTime(2026, 2, 25),
                    HandOverBankCode = "BIDV",
                    HandOverBankName = "BIDV - Chi nhánh Cầu Giấy",
                    BillNo = "VNPOST-BIDV-771290",
                    MortageEndDate = new DateTime(2026, 2, 27),
                    GuaranteeNo = "BG2603-BIDV-00119",
                    BankGuaranteeNo = "BL-BIDV-2026-33128",
                    DateStart = new DateTime(2026, 3, 1),
                    DateExpired = new DateTime(2026, 9, 1),
                    GuaranteeValue = 425000000m,
                    DocumentsStatus = "1",
                    FlagDocReq = "0",
                    Remark = "Xe tải chuyên dùng đã hoàn thiện nghiệm thu thùng đông lạnh, hồ sơ thùng và hồ sơ xe đầy đủ",
                    CreatedAt = DateTime.Now.AddDays(-18),
                    CreatedBy = "DEALER_VN012"
                };

                var vp5 = new CarVinProfile
                {
                    OrgId = orgId,
                    Vin = "KMHE281BBSA900101",
                    CarId = "CAR2026-SG-001",
                    DealerCode = "VS058",
                    DealerName = "Hyundai Sài Gòn",
                    ModelCode = "STARGAZER",
                    ModelName = "Hyundai Stargazer X",
                    SpecCode = "SG15-PRE-01",
                    SpecDescription = "Stargazer X 1.5 Cao Cấp nhập khẩu nguyên chiếc CBU",
                    ColorCode = "SL1",
                    ColorName = "Bạc ánh kim",
                    EngineNo = "G4FL-8899001",
                    StorageCode = "KHO_CANG_CM",
                    StorageName = "Kho Cảng Quốc tế Cái Mép",
                    CONo = "CO-IND-2026-88129",
                    CODate = new DateTime(2026, 3, 10),
                    CODateExpected = new DateTime(2026, 3, 15),
                    TypeCB = false,
                    MortageStatus = CarMortageStatus.Free,
                    DocumentsStatus = "0",
                    FlagDocReq = "0",
                    Remark = "Xe CBU vừa cập cảng Cái Mép, đang làm thủ tục tờ khai hải quan và đăng kiểm chất lượng CQ",
                    CreatedAt = DateTime.Now.AddDays(-5),
                    CreatedBy = "LOGISTICS_ADMIN"
                };

                var vp6 = new CarVinProfile
                {
                    OrgId = orgId,
                    Vin = "KMHE281BBSA778899",
                    CarId = "CAR2026-EX8-01",
                    DealerCode = "VN001",
                    DealerName = "Hyundai Đông Đô",
                    ModelCode = "MIGHTY",
                    ModelName = "Hyundai Mighty EX8 GTL",
                    SpecCode = "EX8-MB-01",
                    SpecDescription = "Xe tải thương mại Mighty EX8 mui bạt 7.2 tấn",
                    ColorCode = "BL1",
                    ColorName = "Xanh dương",
                    EngineNo = "D4CC-1122334",
                    StorageCode = "KHO_NB",
                    StorageName = "Kho Nhà máy Hyundai Ninh Bình",
                    CQNo = "CQ-2026-EX8-0099",
                    CQStartDate = new DateTime(2026, 2, 15),
                    CQEndDate = new DateTime(2028, 2, 15),
                    FGFormNo = "PXX-2026/02-6631",
                    QCNo = "QC-HTV-EX8-551",
                    IssuedDate = new DateTime(2026, 2, 18),
                    CONo = "CO-VN-2026-05512",
                    CODate = new DateTime(2026, 2, 16),
                    InvoiceNoFactory = "HDNM-2026-002819",
                    InvoiceFactoryDate = new DateTime(2026, 2, 20),
                    InvoiceFactorySearch = "INV-FCT-EX8-778899",
                    InvoiceNoTransferred = "HDCN-2026-000551",
                    InvoiceTransferredDate = new DateTime(2026, 2, 23),
                    InvoiceTransferredSearch = "INV-TRF-EX8-778899",
                    InvoiceSpecName = "Ô tô tải có mui Hyundai Mighty EX8 GTL tải trọng 7.2 tấn",
                    TypeCB = true,
                    LoaiThung = "THUNG_BAT",
                    CabinCertificateNo = "GCN-CB-2026-MB-112",
                    CabinCertificateDate = new DateTime(2026, 2, 28),
                    CabinCONo = "CO-CB-MB-112",
                    CabinInvoiceNo = "HDCB-2026-0033",
                    CabinInvoiceDate = new DateTime(2026, 3, 1),
                    MortageStatus = CarMortageStatus.Mortgaged,
                    MortageBankCode = "VPB",
                    MortageBankName = "Ngân hàng TMCP Việt Nam Thịnh Vượng (VPBank)",
                    MortageStartDate = new DateTime(2026, 3, 5),
                    HandOverBankCode = "VPB",
                    HandOverBankName = "VPBank - Trung tâm SME Hà Nội",
                    BillNo = "EMS-VPB-551299",
                    MortageEndDate = new DateTime(2026, 3, 8),
                    GuaranteeNo = "BG2603-VPB-00034",
                    BankGuaranteeNo = "BL-VPB-2026-77812",
                    DateStart = new DateTime(2026, 3, 10),
                    DateExpired = new DateTime(2026, 9, 10),
                    GuaranteeValue = 780000000m,
                    DocumentsStatus = "1",
                    FlagDocReq = "1",
                    DocDeliveryReqDate = new DateTime(2026, 3, 12),
                    Remark = "Xe tải Mighty thùng bạt, đã hoàn thiện đóng thùng và bàn giao hồ sơ cho VPBank, đại lý đã gửi ĐNGT rút hồ sơ",
                    CreatedAt = DateTime.Now.AddDays(-15),
                    CreatedBy = "DEALER_VN001"
                };

                db.CarVinProfiles.AddRange(vp1, vp2, vp3, vp4, vp5, vp6);
            }

            if (!await db.PerformanceInvoices.AnyAsync(o => o.OrgId == orgId))
            {
                var pi1 = new PerformanceInvoice
                {
                    OrgId = orgId,
                    RefNo = "2603PI0001",
                    OrderMonth = "2026-03-01",
                    ProductionMonth = "2026-04-01",
                    ExpectedMonth = "2026-05-01",
                    FlagAutoPL = "1",
                    Status = PerformanceInvoiceStatus.Confirmed,
                    TotalQuantity = 50,
                    TotalAmount = 52000000000m,
                    Remark = "Kế hoạch đặt hàng sản xuất lô xe Santa Fe và Tucson tại nhà máy HMC Asan tháng 4/2026",
                    CreatedBy = "HQ_ORDER_MANAGER",
                    CreatedAt = DateTime.Now.AddDays(-10),
                    ConfirmedBy = "Vũ Đình Hùng (Giám đốc Khối Kế hoạch Bán buôn NPP)",
                    ConfirmedAt = DateTime.Now.AddDays(-7),
                    Details = new List<PerformanceInvoiceDetail>
                    {
                        new()
                        {
                            RefNo = "2603PI0001",
                            LCTemp = "LC2603001",
                            SpecCode = "SF25-PRE-01",
                            ModelCode = "SANTAFE",
                            ModelName = "Hyundai Santa Fe 2.5 HTRAC",
                            ColorCode = "WH1",
                            ColorName = "Trắng ngọc trai",
                            WorkOrderNo = "WO2603001",
                            PortCode = "HPH",
                            PlantCode = "HMC-ASAN",
                            Quantity = 30,
                            UnitPrice = 1200000000m,
                            TotalAmount = 36000000000m,
                            ContractNo = null,
                            FlagAutoPL = "1",
                            Remark = "Đợt 1 nhập cảng Hải Phòng phục vụ đại lý miền Bắc"
                        },
                        new()
                        {
                            RefNo = "2603PI0001",
                            LCTemp = "LC2603001",
                            SpecCode = "TU20-STD-01",
                            ModelCode = "TUCSON",
                            ModelName = "Hyundai Tucson 2.0 AT",
                            ColorCode = "BK1",
                            ColorName = "Đen ánh kim",
                            WorkOrderNo = "WO2603002",
                            PortCode = "HPH",
                            PlantCode = "HMC-ASAN",
                            Quantity = 20,
                            UnitPrice = 800000000m,
                            TotalAmount = 16000000000m,
                            ContractNo = null,
                            FlagAutoPL = "1",
                            Remark = "Đợt 1 Tucson tiêu chuẩn xăng nhập cảng Hải Phòng"
                        }
                    }
                };

                var pi2 = new PerformanceInvoice
                {
                    OrgId = orgId,
                    RefNo = "2602PI0002",
                    OrderMonth = "2026-02-01",
                    ProductionMonth = "2026-03-01",
                    ExpectedMonth = "2026-04-01",
                    FlagAutoPL = "1",
                    Status = PerformanceInvoiceStatus.Contracted,
                    TotalQuantity = 40,
                    TotalAmount = 32800000000m,
                    Remark = "Lô xe Creta và Stargazer nhập khẩu Ulsan đã ký HĐ ngoại thương CT-HMC-2026-001",
                    CreatedBy = "HQ_ORDER_MANAGER",
                    CreatedAt = DateTime.Now.AddDays(-30),
                    ConfirmedBy = "Vũ Đình Hùng (Giám đốc Khối Kế hoạch Bán buôn NPP)",
                    ConfirmedAt = DateTime.Now.AddDays(-28),
                    Details = new List<PerformanceInvoiceDetail>
                    {
                        new()
                        {
                            RefNo = "2602PI0002",
                            LCTemp = "LC2602002",
                            SpecCode = "CR15-PRE-01",
                            ModelCode = "CRETA",
                            ModelName = "Hyundai Creta 1.5 Cao Cấp",
                            ColorCode = "RD1",
                            ColorName = "Đỏ mận",
                            WorkOrderNo = "WO2602010",
                            PortCode = "SGN",
                            PlantCode = "HMC-ULSAN",
                            Quantity = 25,
                            UnitPrice = 800000000m,
                            TotalAmount = 20000000000m,
                            ContractNo = "CT-HMC-2026-001",
                            FlagAutoPL = "1",
                            Remark = "Creta bản cao cấp nhập cảng Sài Gòn"
                        },
                        new()
                        {
                            RefNo = "2602PI0002",
                            LCTemp = "LC2602002",
                            SpecCode = "SG15-PRE-01",
                            ModelCode = "STARGAZER",
                            ModelName = "Hyundai Stargazer X",
                            ColorCode = "SL1",
                            ColorName = "Bạc",
                            WorkOrderNo = "WO2602011",
                            PortCode = "SGN",
                            PlantCode = "HMC-ULSAN",
                            Quantity = 15,
                            UnitPrice = 853333333m,
                            TotalAmount = 12800000000m,
                            ContractNo = "CT-HMC-2026-001",
                            FlagAutoPL = "1",
                            Remark = "Stargazer X nhập cảng Sài Gòn"
                        }
                    }
                };

                var pi3 = new PerformanceInvoice
                {
                    OrgId = orgId,
                    RefNo = "2603PI0003",
                    OrderMonth = "2026-03-01",
                    ProductionMonth = "2026-05-01",
                    ExpectedMonth = "2026-06-01",
                    FlagAutoPL = "1",
                    Status = PerformanceInvoiceStatus.Pending,
                    TotalQuantity = 35,
                    TotalAmount = 38500000000m,
                    Remark = "Bản dự thảo PI đặt xe Custin và Ioniq 5 cho kế hoạch quý 2/2026",
                    CreatedBy = "HQ_ORDER_MANAGER",
                    CreatedAt = DateTime.Now.AddDays(-2),
                    Details = new List<PerformanceInvoiceDetail>
                    {
                        new()
                        {
                            RefNo = "2603PI0003",
                            LCTemp = "LC2603003",
                            SpecCode = "CS20-PRE-01",
                            ModelCode = "CUSTIN",
                            ModelName = "Hyundai Custin 2.0T",
                            ColorCode = "WH1",
                            ColorName = "Trắng tuyết",
                            WorkOrderNo = "WO2603020",
                            PortCode = "HPH",
                            PlantCode = "HMC-ASAN",
                            Quantity = 20,
                            UnitPrice = 950000000m,
                            TotalAmount = 19000000000m,
                            ContractNo = null,
                            FlagAutoPL = "1",
                            Remark = "Custin 2.0T Cao Cấp"
                        },
                        new()
                        {
                            RefNo = "2603PI0003",
                            LCTemp = "LC2603003",
                            SpecCode = "IO5-EXC-01",
                            ModelCode = "IONIQ5",
                            ModelName = "Hyundai Ioniq 5 Exclusive",
                            ColorCode = "GY1",
                            ColorName = "Xám nhám",
                            WorkOrderNo = "WO2603021",
                            PortCode = "HPH",
                            PlantCode = "HMC-ASAN",
                            Quantity = 15,
                            UnitPrice = 1300000000m,
                            TotalAmount = 19500000000m,
                            ContractNo = null,
                            FlagAutoPL = "1",
                            Remark = "Xe điện Ioniq 5 nhập khẩu nguyên chiếc CBU"
                        }
                    }
                };

                var pi4 = new PerformanceInvoice
                {
                    OrgId = orgId,
                    RefNo = "2601PI0004",
                    OrderMonth = "2026-01-01",
                    ProductionMonth = "2026-02-01",
                    ExpectedMonth = "2026-03-01",
                    FlagAutoPL = "0",
                    Status = PerformanceInvoiceStatus.Cancelled,
                    TotalQuantity = 10,
                    TotalAmount = 5400000000m,
                    CancelReason = "Điều chỉnh cơ cấu kế hoạch sản xuất xe lắp ráp trong nước sang lô tiếp theo",
                    CancelledBy = "HQ_ORDER_MANAGER",
                    CancelledAt = DateTime.Now.AddDays(-20),
                    CreatedBy = "HQ_ORDER_MANAGER",
                    CreatedAt = DateTime.Now.AddDays(-45),
                    Details = new List<PerformanceInvoiceDetail>
                    {
                        new()
                        {
                            RefNo = "2601PI0004",
                            LCTemp = "LC2601004",
                            SpecCode = "AC15-SPE-01",
                            ModelCode = "ACCENT",
                            ModelName = "Hyundai Accent 1.5 AT Đặc Biệt",
                            ColorCode = "WH1",
                            ColorName = "Trắng tuyết",
                            WorkOrderNo = "WO2601005",
                            PortCode = "HPH",
                            PlantCode = "HTMV-NB",
                            Quantity = 10,
                            UnitPrice = 540000000m,
                            TotalAmount = 5400000000m,
                            ContractNo = null,
                            FlagAutoPL = "0",
                            Remark = "Đã hủy theo quyết định điều chuyển kế hoạch"
                        }
                    }
                };

                db.PerformanceInvoices.AddRange(pi1, pi2, pi3, pi4);
            }

            if (!await db.ContractOverseas.AnyAsync(o => o.OrgId == orgId))
            {
                var co1 = new ContractOversea
                {
                    OrgId = orgId,
                    ContractNo = "CT-HMC-2026-001",
                    ContractDate = DateTime.Today.AddDays(-28),
                    SupplierCode = "HMC",
                    SupplierName = "Hyundai Motor Company Korea",
                    LCNo = "LC26020001",
                    BankName = "Ngân hàng TMCP Ngoại thương Việt Nam (Vietcombank - CN Thăng Long)",
                    Incoterms = "CIF Sai Gon Port",
                    DestinationPort = "SGN",
                    Currency = "USD",
                    ExpectedDeliveryDate = DateTime.Today.AddDays(15),
                    TotalQuantity = 40,
                    TotalAmount = 1312000m,
                    Status = ContractOverseaStatus.LinkedLC,
                    Remark = "Hợp đồng ngoại thương nhập khẩu lô 40 xe Creta và Stargazer X sản xuất tại Nhà máy Ulsan Hàn Quốc về cảng Sài Gòn",
                    CreatedBy = "HQ_IMPORT_MANAGER",
                    CreatedAt = DateTime.Now.AddDays(-28)
                };

                var co2 = new ContractOversea
                {
                    OrgId = orgId,
                    ContractNo = "2603HMC0002",
                    ContractDate = DateTime.Today.AddDays(-10),
                    SupplierCode = "HMC",
                    SupplierName = "Hyundai Motor Company Korea",
                    LCNo = null,
                    BankName = null,
                    Incoterms = "CIF Hai Phong Port",
                    DestinationPort = "HPH",
                    Currency = "USD",
                    ExpectedDeliveryDate = DateTime.Today.AddDays(45),
                    TotalQuantity = 50,
                    TotalAmount = 2080000m,
                    Status = ContractOverseaStatus.Active,
                    Remark = "Hợp đồng ngoại thương ký kết lô xe Santa Fe và Tucson Asan tháng 3/2026 đang hoàn tất hồ sơ mở L/C tại VietinBank",
                    CreatedBy = "HQ_IMPORT_MANAGER",
                    CreatedAt = DateTime.Now.AddDays(-10)
                };

                var co3 = new ContractOversea
                {
                    OrgId = orgId,
                    ContractNo = "2601HMC0003",
                    ContractDate = DateTime.Today.AddDays(-60),
                    SupplierCode = "HMC",
                    SupplierName = "Hyundai Motor Company Korea",
                    LCNo = "LC26010003",
                    BankName = "Ngân hàng TMCP Đầu tư và Phát triển Việt Nam (BIDV - CN Hà Nội)",
                    Incoterms = "CIF Hai Phong Port",
                    DestinationPort = "HPH",
                    Currency = "USD",
                    ExpectedDeliveryDate = DateTime.Today.AddDays(-10),
                    TotalQuantity = 20,
                    TotalAmount = 780000m,
                    Status = ContractOverseaStatus.Completed,
                    Remark = "Lô xe CBU Palisade nhập khẩu cảng Hải Phòng đã thông quan và bàn giao kho bãi thành công",
                    CreatedBy = "HQ_IMPORT_MANAGER",
                    CreatedAt = DateTime.Now.AddDays(-60)
                };

                var co4 = new ContractOversea
                {
                    OrgId = orgId,
                    ContractNo = "2601HMC0004",
                    ContractDate = DateTime.Today.AddDays(-55),
                    SupplierCode = "HMC",
                    SupplierName = "Hyundai Motor Company Korea",
                    LCNo = null,
                    BankName = null,
                    Incoterms = "CIF Da Nang Port",
                    DestinationPort = "DAN",
                    Currency = "USD",
                    ExpectedDeliveryDate = null,
                    TotalQuantity = 10,
                    TotalAmount = 220000m,
                    Status = ContractOverseaStatus.Cancelled,
                    Remark = "Hợp đồng thử nghiệm dự kiến nhập cảng Đà Nẵng",
                    CancelReason = "Thay đổi phương án logistics gom toàn bộ về cảng Đình Vũ Hải Phòng",
                    CreatedBy = "HQ_IMPORT_MANAGER",
                    CreatedAt = DateTime.Now.AddDays(-55),
                    CancelledBy = "HQ_IMPORT_LEADER",
                    CancelledAt = DateTime.Now.AddDays(-50)
                };

                db.ContractOverseas.AddRange(co1, co2, co3, co4);
            }

            if (!await db.LettersOfCredit.AnyAsync(o => o.OrgId == orgId))
            {
                var lc1 = new LetterOfCredit
                {
                    OrgId = orgId,
                    LCNo = "LC26020001",
                    ContractNo = "CT-HMC-2026-001",
                    BankCode = "VCB",
                    BankName = "Ngân hàng TMCP Ngoại thương Việt Nam (Vietcombank - CN Thăng Long)",
                    BankBranch = "Sở Giao Dịch Hà Nội",
                    LCType = LCType.AtSight,
                    Amount = 1312000m,
                    Currency = "USD",
                    IssueDate = DateTime.Today.AddDays(-25),
                    ExpireDate = DateTime.Today.AddDays(65),
                    LatestShipmentDate = DateTime.Today.AddDays(30),
                    Status = LCStatus.Active,
                    Remark = "Thư tín dụng không hủy ngang L/C At Sight phát hành thanh toán HĐ ngoại CT-HMC-2026-001 qua VCB",
                    CreatedBy = "FINANCE_OFFICER",
                    CreatedAt = DateTime.Now.AddDays(-25)
                };

                var lc2 = new LetterOfCredit
                {
                    OrgId = orgId,
                    LCNo = "LC26010003",
                    ContractNo = "2601HMC0003",
                    BankCode = "BIDV",
                    BankName = "Ngân hàng TMCP Đầu tư và Phát triển Việt Nam (BIDV - CN Hà Nội)",
                    BankBranch = "Chi nhánh Hà Nội",
                    LCType = LCType.Usance,
                    Amount = 780000m,
                    Currency = "USD",
                    IssueDate = DateTime.Today.AddDays(-55),
                    ExpireDate = DateTime.Today.AddDays(35),
                    LatestShipmentDate = DateTime.Today.AddDays(-15),
                    Status = LCStatus.Settled,
                    Remark = "L/C trả chậm Usance 90 ngày đã quyết toán và giải tỏa nghĩa vụ ngân hàng sau khi hoàn tất thông quan",
                    CreatedBy = "FINANCE_OFFICER",
                    CreatedAt = DateTime.Now.AddDays(-55)
                };

                db.LettersOfCredit.AddRange(lc1, lc2);
            }

            if (!await db.TransportInsOrders.AnyAsync(o => o.OrgId == orgId))
            {
                var ti1 = new TransportInsOrder
                {
                    OrgId = orgId,
                    TransportInsNo = "2603TI0001",
                    PmtMonth = "2026-03-01",
                    VAT = 10.0m,
                    TotalCars = 3,
                    TotalTransportCost = 18000000m,
                    TotalDelayPenalty = 300000m,
                    TotalInsuranceCost = 1515000m,
                    TotalAmountVAT = 19215000m,
                    TotalAmount = 17468181.82m,
                    UnitPriceVAT = 1746818.18m,
                    Status = TransportInsStatus.Approved2,
                    HTVSignStatus = TransportInsSignStatus.ChuaKy,
                    TCMSSignStatus = TransportInsSignStatus.ChuaKy,
                    App1DTime = DateTime.Now.AddDays(-3),
                    App1By = "LOGISTICS_HEAD",
                    App2DTime = DateTime.Now.AddDays(-1),
                    App2By = "FINANCE_DIRECTOR",
                    Remark = "Bảng kê đối soát chi phí vận chuyển & bảo hiểm lô xe tháng 03/2026 giữa HTV và TCMS",
                    CreateDateTime = DateTime.Now.AddDays(-5),
                    CreateBy = "TCMS_DISPATCHER",
                    LogLUDateTime = DateTime.Now.AddDays(-1),
                    LogLUBy = "FINANCE_DIRECTOR",
                    Details = new List<TransportInsOrderDetail>
                    {
                        new TransportInsOrderDetail
                        {
                            TransportInsNo = "2603TI0001",
                            Vin = "KMHE281BBSA129841",
                            CarId = "CAR2026-SF001",
                            DlvMnNo = "BBBG2603001",
                            TranspReqType = "CARTRANSPORT",
                            ModelCode = "SANTAFE",
                            ModelName = "Santa Fe 2.5 HTRAC",
                            SpecCode = "SF25-PRE-01",
                            ColorName = "Đen",
                            EngineNo = "G4KP-092182",
                            FStorageCode = "KHO_NB",
                            FProvinceName = "Ninh Bình",
                            TStorageCode = "VN001",
                            TProvinceName = "Hà Nội",
                            InvStartDate = DateTime.Today.AddDays(-12),
                            ExpectedDays = 2,
                            ExpectedDlvEndDate = DateTime.Today.AddDays(-10),
                            InvEndDate = DateTime.Today.AddDays(-10),
                            DelayDate = 0,
                            TransportCost = 3500000m,
                            DelayPenaty = 0m,
                            PriceCar = 1350000000m,
                            InsurancePercent = 0.0005m,
                            InsuranceContractNo = "BHVT-2026-PTI",
                            InsuranceCost = 675000m,
                            TotalPrice = 4175000m,
                            Status = TransportInsDetailStatus.Approved,
                            Remark = "Giao xe đúng hẹn cho Đại lý Hyundai Đông Đô"
                        },
                        new TransportInsOrderDetail
                        {
                            TransportInsNo = "2603TI0001",
                            Vin = "KMHE281BBSA987654",
                            CarId = "CAR2026-TU002",
                            DlvMnNo = "BBBG2603002",
                            TranspReqType = "CARTRANSPORT",
                            ModelCode = "TUCSON",
                            ModelName = "Tucson 2.0 AT",
                            SpecCode = "TU20-STD-01",
                            ColorName = "Trắng",
                            EngineNo = "G4NL-983102",
                            FStorageCode = "KHO_NB",
                            FProvinceName = "Ninh Bình",
                            TStorageCode = "VN003",
                            TProvinceName = "Đà Nẵng",
                            InvStartDate = DateTime.Today.AddDays(-11),
                            ExpectedDays = 3,
                            ExpectedDlvEndDate = DateTime.Today.AddDays(-8),
                            InvEndDate = DateTime.Today.AddDays(-6),
                            DelayDate = 2,
                            TransportCost = 6000000m,
                            DelayPenaty = 300000m,
                            PriceCar = 950000000m,
                            InsurancePercent = 0.0005m,
                            InsuranceContractNo = "BHVT-2026-PTI",
                            InsuranceCost = 475000m,
                            TotalPrice = 6175000m,
                            Status = TransportInsDetailStatus.Approved,
                            StandardRemark = "Mưa bão đèo Hải Vân chậm 2 ngày",
                            Remark = "Trừ phạt chậm 2 ngày x 150k"
                        },
                        new TransportInsOrderDetail
                        {
                            TransportInsNo = "2603TI0001",
                            Vin = "KMHE281BBSA556677",
                            CarId = "CAR2026-CR003",
                            DlvMnNo = "BBBG2603003",
                            TranspReqType = "CARTRANSPORT",
                            ModelCode = "CRETA",
                            ModelName = "Creta 1.5 Cao Cấp",
                            SpecCode = "CR15-PRE-01",
                            ColorName = "Đỏ",
                            EngineNo = "G4FL-881920",
                            FStorageCode = "KHO_NB",
                            FProvinceName = "Ninh Bình",
                            TStorageCode = "VN002",
                            TProvinceName = "TP. Hồ Chí Minh",
                            InvStartDate = DateTime.Today.AddDays(-9),
                            ExpectedDays = 4,
                            ExpectedDlvEndDate = DateTime.Today.AddDays(-5),
                            InvEndDate = DateTime.Today.AddDays(-5),
                            DelayDate = 0,
                            TransportCost = 8500000m,
                            DelayPenaty = 0m,
                            PriceCar = 730000000m,
                            InsurancePercent = 0.0005m,
                            InsuranceContractNo = "BHVT-2026-PTI",
                            InsuranceCost = 365000m,
                            TotalPrice = 8865000m,
                            Status = TransportInsDetailStatus.Approved,
                            Remark = "Bàn giao xe đại lý Hyundai Nam Trung an toàn đúng hẹn"
                        }
                    }
                };

                var ti2 = new TransportInsOrder
                {
                    OrgId = orgId,
                    TransportInsNo = "2602TI0002",
                    PmtMonth = "2026-02-01",
                    VAT = 10.0m,
                    TotalCars = 2,
                    TotalTransportCost = 7000000m,
                    TotalDelayPenalty = 0m,
                    TotalInsuranceCost = 550000m,
                    TotalAmountVAT = 7550000m,
                    TotalAmount = 6863636.36m,
                    UnitPriceVAT = 686363.64m,
                    Status = TransportInsStatus.Finished,
                    HTVSignStatus = TransportInsSignStatus.DaKy,
                    HTVSignDTime = DateTime.Now.AddDays(-20),
                    HTVSignBy = "HTV_REP_KIM",
                    TCMSSignStatus = TransportInsSignStatus.DaKy,
                    TCMSSignDTime = DateTime.Now.AddDays(-21),
                    TCMSSignBy = "TCMS_REP_TRAN",
                    FilePath = "https://cdn.dms.sales/signs/202602/TI26020002_signed.pdf",
                    App1DTime = DateTime.Now.AddDays(-24),
                    App1By = "LOGISTICS_HEAD",
                    App2DTime = DateTime.Now.AddDays(-22),
                    App2By = "FINANCE_DIRECTOR",
                    Remark = "Bảng kê chi phí vận tải tháng 02/2026 đã ký số 2 bên hoàn tất quyết toán",
                    CreateDateTime = DateTime.Now.AddDays(-25),
                    CreateBy = "TCMS_DISPATCHER",
                    LogLUDateTime = DateTime.Now.AddDays(-20),
                    LogLUBy = "HTV_REP_KIM",
                    Details = new List<TransportInsOrderDetail>
                    {
                        new TransportInsOrderDetail
                        {
                            TransportInsNo = "2602TI0002",
                            Vin = "KMHE281BBSA112233",
                            CarId = "CAR2026-AC001",
                            DlvMnNo = "BBBG2602001",
                            TranspReqType = "CARTRANSPORT",
                            ModelCode = "ACCENT",
                            ModelName = "Accent 1.5 AT",
                            SpecCode = "AC15-AT-01",
                            ColorName = "Bạc",
                            EngineNo = "G4FA-109283",
                            FStorageCode = "KHO_NB",
                            FProvinceName = "Ninh Bình",
                            TStorageCode = "VN001",
                            TProvinceName = "Hà Nội",
                            InvStartDate = DateTime.Today.AddDays(-30),
                            ExpectedDays = 2,
                            ExpectedDlvEndDate = DateTime.Today.AddDays(-28),
                            InvEndDate = DateTime.Today.AddDays(-28),
                            DelayDate = 0,
                            TransportCost = 3500000m,
                            DelayPenaty = 0m,
                            PriceCar = 550000000m,
                            InsurancePercent = 0.0005m,
                            InsuranceContractNo = "BHVT-2026-PTI",
                            InsuranceCost = 275000m,
                            TotalPrice = 3775000m,
                            Status = TransportInsDetailStatus.Approved,
                            Remark = "Đã thanh lý quyết toán xong"
                        },
                        new TransportInsOrderDetail
                        {
                            TransportInsNo = "2602TI0002",
                            Vin = "KMHE281BBSA445566",
                            CarId = "CAR2026-SG002",
                            DlvMnNo = "BBBG2602002",
                            TranspReqType = "CARTRANSPORT",
                            ModelCode = "STARGAZER",
                            ModelName = "Stargazer X",
                            SpecCode = "SGX-PRE-01",
                            ColorName = "Trắng Mờ",
                            EngineNo = "G4FL-772910",
                            FStorageCode = "KHO_NB",
                            FProvinceName = "Ninh Bình",
                            TStorageCode = "VN001",
                            TProvinceName = "Hà Nội",
                            InvStartDate = DateTime.Today.AddDays(-29),
                            ExpectedDays = 2,
                            ExpectedDlvEndDate = DateTime.Today.AddDays(-27),
                            InvEndDate = DateTime.Today.AddDays(-27),
                            DelayDate = 0,
                            TransportCost = 3500000m,
                            DelayPenaty = 0m,
                            PriceCar = 550000000m,
                            InsurancePercent = 0.0005m,
                            InsuranceContractNo = "BHVT-2026-PTI",
                            InsuranceCost = 275000m,
                            TotalPrice = 3775000m,
                            Status = TransportInsDetailStatus.Approved,
                            Remark = "Đã thanh lý quyết toán xong"
                        }
                    }
                };

                db.TransportInsOrders.AddRange(ti1, ti2);
            }

            // Bảng kê Quyết toán Chi phí Thiết bị AVN (Audio-Visual Navigation) Xe Ô tô HTV - TCMS (Pmt_PaymentAVN)
            if (!await db.AvnUnitPrices.AnyAsync())
            {
                db.AvnUnitPrices.AddRange(
                    new AvnUnitPriceMaster { AvnCode = "AVN-STF-GEN5-PREM", AvnName = "AVN SantaFe Gen5 Premium 12.3 inch", ModelCode = "SANTAFE", UnitPrice = 12500000m, EffStartDate = new DateTime(2026, 1, 1), IsActive = true, Remark = "Đơn giá thiết bị AVN quy định theo dòng xe" },
                    new AvnUnitPriceMaster { AvnCode = "AVN-TUC-GEN5-1025", AvnName = "AVN Tucson Gen5 10.25 inch", ModelCode = "TUCSON", UnitPrice = 11000000m, EffStartDate = new DateTime(2026, 1, 1), IsActive = true, Remark = "Đơn giá thiết bị AVN quy định theo dòng xe" },
                    new AvnUnitPriceMaster { AvnCode = "AVN-CRT-1025-NAV", AvnName = "AVN Creta 10.25 inch Navi", ModelCode = "CRETA", UnitPrice = 9500000m, EffStartDate = new DateTime(2026, 1, 1), IsActive = true, Remark = "Đơn giá thiết bị AVN quy định theo dòng xe" },
                    new AvnUnitPriceMaster { AvnCode = "AVN-ACC-GEN5-8IN", AvnName = "AVN Accent Gen5 8 inch", ModelCode = "ACCENT", UnitPrice = 8500000m, EffStartDate = new DateTime(2026, 1, 1), IsActive = true, Remark = "Đơn giá thiết bị AVN quy định theo dòng xe" },
                    new AvnUnitPriceMaster { AvnCode = "AVN-ELN-1025-WIDESCREEN", AvnName = "AVN Elantra 10.25 inch Widescreen", ModelCode = "ELANTRA", UnitPrice = 9800000m, EffStartDate = new DateTime(2026, 1, 1), IsActive = true, Remark = "Đơn giá thiết bị AVN quy định theo dòng xe" }
                );
            }

            if (!await db.PaymentAVNOrders.AnyAsync(o => o.OrgId == orgId))
            {
                var avn1 = new PaymentAVNOrder
                {
                    OrgId = orgId,
                    PaymentAVNNo = "2603AVN0001",
                    PmtMonth = "2026-03",
                    SupplierCode = "MOBIS-VN",
                    SupplierName = "Công ty TNHH Mobis Auto Parts Việt Nam",
                    TotalCars = 3,
                    AmountTotal = 32000000m,
                    VatRate = 10m,
                    UnitPriceVAT = 3200000m,
                    TotalAmountVAT = 35200000m,
                    Status = PaymentAVNStatus.HTVApproved,
                    TCMSSignStatus = PaymentAVNSignStatus.ChuaKy,
                    HTVSignStatus = PaymentAVNSignStatus.ChuaKy,
                    Remark = "Bảng kê chi phí thiết bị AVN tháng 03/2026 chờ ký số 2 bên",
                    CreatedBy = "TCMS_TELEMATICS_MANAGER",
                    CreatedAt = DateTime.Now.AddDays(-8),
                    Approved1At = DateTime.Now.AddDays(-6),
                    Approved1By = "TCMS_TELEMATICS_MANAGER",
                    Approved2At = DateTime.Now.AddDays(-4),
                    Approved2By = "HTV_SALES_MANAGER",
                    LUDateTime = DateTime.Now.AddDays(-4),
                    LUBy = "HTV_SALES_MANAGER",
                    Details = new List<PaymentAVNDetail>
                    {
                        new PaymentAVNDetail
                        {
                            PaymentAVNNo = "2603AVN0001",
                            Vin = "KMHGN41DBPU112233",
                            CarId = "CAR2026-ST001",
                            EngineNo = "G4KE-112233",
                            ModelCode = "SANTAFE",
                            ModelName = "SantaFe 2.5 Premium",
                            SpecCode = "STF-PRE-01",
                            ColorCode = "WHT",
                            ColorName = "Trắng",
                            AVNCode = "AVN-STF-GEN5-PREM",
                            SerialNo = "SN-STF-112233",
                            UnitPriceAVN = 12500000m,
                            AVNDate = DateTime.Today.AddDays(-12),
                            InStorageDate = DateTime.Today.AddDays(-10),
                            Status = PaymentAVNDetailStatus.Approved,
                            Remark = "Lắp AVN Gen5 Premium 12.3 inch"
                        },
                        new PaymentAVNDetail
                        {
                            PaymentAVNNo = "2603AVN0001",
                            Vin = "KMHJ381BBPU445566",
                            CarId = "CAR2026-TU002",
                            EngineNo = "G4NN-445566",
                            ModelCode = "TUCSON",
                            ModelName = "Tucson 1.6 Turbo",
                            SpecCode = "TUC-TURBO-01",
                            ColorCode = "BLK",
                            ColorName = "Đen",
                            AVNCode = "AVN-TUC-GEN5-1025",
                            SerialNo = "SN-TUC-445566",
                            UnitPriceAVN = 11000000m,
                            AVNDate = DateTime.Today.AddDays(-11),
                            InStorageDate = DateTime.Today.AddDays(-9),
                            Status = PaymentAVNDetailStatus.Approved,
                            Remark = "Lắp AVN Tucson Gen5 10.25 inch"
                        },
                        new PaymentAVNDetail
                        {
                            PaymentAVNNo = "2603AVN0001",
                            Vin = "KMHE281BBSA556677",
                            CarId = "CAR2026-CR003",
                            EngineNo = "G4FL-881920",
                            ModelCode = "CRETA",
                            ModelName = "Creta 1.5 Cao Cấp",
                            SpecCode = "CR15-PRE-01",
                            ColorCode = "RED",
                            ColorName = "Đỏ",
                            AVNCode = "AVN-CRT-1025-NAV",
                            SerialNo = "SN-CRT-556677",
                            UnitPriceAVN = 8500000m,
                            AVNDate = DateTime.Today.AddDays(-10),
                            InStorageDate = DateTime.Today.AddDays(-8),
                            Status = PaymentAVNDetailStatus.Approved,
                            Remark = "Lắp AVN Creta 10.25 inch Navi"
                        }
                    }
                };

                var avn2 = new PaymentAVNOrder
                {
                    OrgId = orgId,
                    PaymentAVNNo = "2602AVN0002",
                    PmtMonth = "2026-02",
                    SupplierCode = "MOTREX-VN",
                    SupplierName = "Công ty TNHH Motrex Việt Nam",
                    TotalCars = 2,
                    AmountTotal = 19500000m,
                    VatRate = 10m,
                    UnitPriceVAT = 1950000m,
                    TotalAmountVAT = 21450000m,
                    Status = PaymentAVNStatus.Settled,
                    TCMSSignStatus = PaymentAVNSignStatus.DaKy,
                    TCMSSignDate = DateTime.Now.AddDays(-22),
                    TCMSSignBy = "TCMS_TELEMATICS_DIRECTOR",
                    HTVSignStatus = PaymentAVNSignStatus.DaKy,
                    HTVSignDate = DateTime.Now.AddDays(-21),
                    HTVSignBy = "HTV_REP_KIM",
                    FilePath = "/reports/payment-avn/2602AVN0002_FullySigned.pdf",
                    BankTxnRef = "UNC-20260228-0007",
                    Remark = "Bảng kê chi phí thiết bị AVN tháng 02/2026 đã ký số 2 bên và quyết toán xong",
                    CreatedBy = "TCMS_TELEMATICS_MANAGER",
                    CreatedAt = DateTime.Now.AddDays(-28),
                    Approved1At = DateTime.Now.AddDays(-26),
                    Approved1By = "TCMS_TELEMATICS_MANAGER",
                    Approved2At = DateTime.Now.AddDays(-24),
                    Approved2By = "HTV_SALES_MANAGER",
                    SettledAt = DateTime.Now.AddDays(-20),
                    SettledBy = "HTV_CHIEF_ACCOUNTANT",
                    LUDateTime = DateTime.Now.AddDays(-20),
                    LUBy = "HTV_CHIEF_ACCOUNTANT",
                    Details = new List<PaymentAVNDetail>
                    {
                        new PaymentAVNDetail
                        {
                            PaymentAVNNo = "2602AVN0002",
                            Vin = "KMHE281BBSA112233",
                            CarId = "CAR2026-AC001",
                            EngineNo = "G4FA-109283",
                            ModelCode = "ACCENT",
                            ModelName = "Accent 1.5 AT",
                            SpecCode = "AC15-AT-01",
                            ColorCode = "SIL",
                            ColorName = "Bạc",
                            AVNCode = "AVN-ACC-GEN5-8IN",
                            SerialNo = "SN-ACC-112233",
                            UnitPriceAVN = 8500000m,
                            AVNDate = DateTime.Today.AddDays(-35),
                            InStorageDate = DateTime.Today.AddDays(-33),
                            Status = PaymentAVNDetailStatus.Approved,
                            Remark = "Đã quyết toán xong"
                        },
                        new PaymentAVNDetail
                        {
                            PaymentAVNNo = "2602AVN0002",
                            Vin = "KMHLN4AJBPU778899",
                            CarId = "CAR2026-EL004",
                            EngineNo = "G4FM-778899",
                            ModelCode = "ELANTRA",
                            ModelName = "Elantra 2.0 Widescreen",
                            SpecCode = "ELN-20-01",
                            ColorCode = "GRY",
                            ColorName = "Xám",
                            AVNCode = "AVN-ELN-1025-WIDESCREEN",
                            SerialNo = "SN-ELN-778899",
                            UnitPriceAVN = 11000000m,
                            AVNDate = DateTime.Today.AddDays(-34),
                            InStorageDate = DateTime.Today.AddDays(-32),
                            Status = PaymentAVNDetailStatus.Approved,
                            Remark = "Đã quyết toán xong"
                        }
                    }
                };

                db.PaymentAVNOrders.AddRange(avn1, avn2);
            }

            // Khách hàng Đại lý (DLS_DealerCustomer)
            if (!await db.DealerCustomers.AnyAsync(o => o.OrgId == orgId))
            {
                db.DealerCustomers.AddRange(
                    new DealerCustomer
                    {
                        OrgId = orgId,
                        CustomerCode = "2606CTM06125",
                        DealerCode = "VS058",
                        DealerName = "Hyundai Bình Dương",
                        FullName = "Nguyễn Văn An",
                        FullNameEN = "Nguyen Van An",
                        Gender = DealerCustomerGender.Male,
                        DateOfBirth = new DateTime(1990, 1, 1),
                        IDCardType = "CCCD",
                        IDCardNo = "079090001234",
                        PhoneNo = "0901234567",
                        Email = "nguyenvanan@email.com",
                        Address = "123 Đường ABC, Phường XYZ",
                        ProvinceCode = "BD",
                        ProvinceName = "Bình Dương",
                        DistrictCode = "BD001",
                        DistrictName = "TP. Thủ Dầu Một",
                        CustomerBaseCode = "CTM_CAN_NHAN",
                        CustomerBaseName = "Khách hàng cá nhân",
                        CustomerType = DealerCustomerType.Personal,
                        CreatedAt = DateTime.Now.AddDays(-20),
                        CreatedBy = "VS058"
                    },
                    new DealerCustomer
                    {
                        OrgId = orgId,
                        CustomerCode = "2606CTM06126",
                        DealerCode = "VS058",
                        DealerName = "Hyundai Bình Dương",
                        FullName = "Công ty TNHH Thương mại Dịch vụ Phú Thịnh",
                        FullNameEN = "Phu Thinh Trading Service Co., Ltd",
                        Gender = DealerCustomerGender.Other,
                        DateOfBirth = new DateTime(2015, 6, 12),
                        IDCardType = "MST",
                        IDCardNo = "3701234567",
                        PhoneNo = "02743888999",
                        Email = "contact@phuthinh.vn",
                        Address = "456 Đại lộ Bình Dương, KCN Sóng Thần",
                        ProvinceCode = "BD",
                        ProvinceName = "Bình Dương",
                        DistrictCode = "BD002",
                        DistrictName = "TP. Dĩ An",
                        CustomerBaseCode = "CTM_DOANH_NGHIEP",
                        CustomerBaseName = "Khách hàng doanh nghiệp",
                        CustomerType = DealerCustomerType.Business,
                        TaxCode = "3701234567",
                        RepresentName = "Trần Thị Bích",
                        Position = "Giám đốc",
                        CusAccountBank = "0071000123456",
                        CreatedAt = DateTime.Now.AddDays(-15),
                        CreatedBy = "VS058"
                    },
                    new DealerCustomer
                    {
                        OrgId = orgId,
                        CustomerCode = "2606CTM06127",
                        DealerCode = "VN001",
                        DealerName = "Hyundai Đông Đô",
                        FullName = "Lê Hoàng Nam",
                        FullNameEN = "Le Hoang Nam",
                        Gender = DealerCustomerGender.Male,
                        DateOfBirth = new DateTime(1985, 9, 20),
                        IDCardType = "CCCD",
                        IDCardNo = "001085009876",
                        PhoneNo = "0912345678",
                        Email = "lehoangnam@email.com",
                        Address = "789 Phố Kim Mã, Quận Ba Đình",
                        ProvinceCode = "HN",
                        ProvinceName = "Hà Nội",
                        DistrictCode = "HN001",
                        DistrictName = "Quận Ba Đình",
                        CustomerBaseCode = "CTM_CAN_NHAN",
                        CustomerBaseName = "Khách hàng cá nhân",
                        CustomerType = DealerCustomerType.Personal,
                        CreatedAt = DateTime.Now.AddDays(-10),
                        CreatedBy = "VN001"
                    }
                );
            }

            // Nhật ký cập nhật giá xe ô tô (CarUpdPriceController / Car_Car_Update01_HQ)
            if (!await db.CarPriceUpdateLogs.AnyAsync(o => o.OrgId == orgId))
            {
                db.CarPriceUpdateLogs.AddRange(
                    new CarPriceUpdateLog
                    {
                        OrgId = orgId,
                        CarId = "CAR2026-SF2501",
                        Vin = null,
                        ModelCode = "SF25",
                        DealerCode = "VN001",
                        Action = CarPriceUpdateAction.UpdatePrice,
                        OldUnitPriceActual = 1330000000m,
                        NewUnitPriceActual = 1350000000m,
                        OldPaymentStatus = "P",
                        NewPaymentStatus = "P",
                        Remark = "Điều chỉnh giá bán buôn thực tế theo chính sách bán hàng tháng",
                        PerformedBy = "CHUYEN_VIEN_DIEU_PHOI_NPP",
                        PerformedAt = DateTime.Now.AddDays(-5)
                    },
                    new CarPriceUpdateLog
                    {
                        OrgId = orgId,
                        CarId = "CAR2026-TU2005",
                        Vin = null,
                        ModelCode = "TU20",
                        DealerCode = "VN002",
                        Action = CarPriceUpdateAction.UpdateMapVINRanking,
                        OldPaymentStatus = "P",
                        NewPaymentStatus = "P",
                        Remark = "Cập nhật thứ tự ưu tiên Map VIN cho xe chờ phân bổ",
                        PerformedBy = "CHUYEN_VIEN_DIEU_PHOI_NPP",
                        PerformedAt = DateTime.Now.AddDays(-3)
                    },
                    new CarPriceUpdateLog
                    {
                        OrgId = orgId,
                        CarId = "CAR2026-AC1409",
                        Vin = null,
                        ModelCode = "AC14",
                        DealerCode = "VN003",
                        Action = CarPriceUpdateAction.LockChangeVIN,
                        OldPaymentStatus = "P",
                        NewPaymentStatus = "P",
                        Remark = "Khóa đổi số khung VIN theo yêu cầu điều phối",
                        PerformedBy = "CHUYEN_VIEN_DIEU_PHOI_NPP",
                        PerformedAt = DateTime.Now.AddDays(-2)
                    }
                );
            }

            // Định mức bảo hành xe ô tô theo dòng xe (Mst_WarrantyExpires / Master.cs)
            if (!await db.WarrantyExpires.AnyAsync(o => o.OrgId == orgId))
            {
                db.WarrantyExpires.AddRange(
                    new WarrantyExpiresMaster { OrgId = orgId, ModelCode = "SF25", ModelName = "Santa Fe 2.5 HTRAC", WarrantyExpires = 60m, WarrantyKM = 100000m, FlagActive = "1", Remark = "Bảo hành 5 năm hoặc 100.000 km", LogLUDateTime = DateTime.Now.AddDays(-30), LogLUBy = "CHUYEN_VIEN_NPP" },
                    new WarrantyExpiresMaster { OrgId = orgId, ModelCode = "TU20", ModelName = "Tucson 2.0 AT", WarrantyExpires = 60m, WarrantyKM = 100000m, FlagActive = "1", Remark = "Bảo hành 5 năm hoặc 100.000 km", LogLUDateTime = DateTime.Now.AddDays(-30), LogLUBy = "CHUYEN_VIEN_NPP" },
                    new WarrantyExpiresMaster { OrgId = orgId, ModelCode = "CR15", ModelName = "Creta 1.5 Cao Cấp", WarrantyExpires = 60m, WarrantyKM = 100000m, FlagActive = "1", Remark = "Bảo hành 5 năm hoặc 100.000 km", LogLUDateTime = DateTime.Now.AddDays(-30), LogLUBy = "CHUYEN_VIEN_NPP" },
                    new WarrantyExpiresMaster { OrgId = orgId, ModelCode = "AC14", ModelName = "Accent 1.4 AT", WarrantyExpires = 36m, WarrantyKM = 100000m, FlagActive = "1", Remark = "Bảo hành 3 năm hoặc 100.000 km", LogLUDateTime = DateTime.Now.AddDays(-30), LogLUBy = "CHUYEN_VIEN_NPP" }
                );
            }

            // Phân vùng HTV (Mst_Zone / DealerZone.cs / FrmMst_Zone)
            if (!await db.Zones.AnyAsync(o => o.OrgId == orgId))
            {
                db.Zones.AddRange(
                    new ZoneMaster { OrgId = orgId, ZoneCode = "MB", ZoneName = "Miền Bắc", Remark = "Phân vùng kinh doanh miền Bắc", FlagActive = "1", LogLUDTime = DateTime.Now.AddDays(-60), LogLUBy = "SYSADMIN" },
                    new ZoneMaster { OrgId = orgId, ZoneCode = "MT", ZoneName = "Miền Trung", Remark = "Phân vùng kinh doanh miền Trung", FlagActive = "1", LogLUDTime = DateTime.Now.AddDays(-60), LogLUBy = "SYSADMIN" },
                    new ZoneMaster { OrgId = orgId, ZoneCode = "MN", ZoneName = "Miền Nam", Remark = "Phân vùng kinh doanh miền Nam", FlagActive = "1", LogLUDTime = DateTime.Now.AddDays(-60), LogLUBy = "SYSADMIN" }
                );
            }

            // Thiết lập phân vùng HTV cho đại lý (Mst_DealerZone / DealerZone.cs / FrmMst_DealerZone)
            if (!await db.DealerZones.AnyAsync(o => o.OrgId == orgId))
            {
                db.DealerZones.AddRange(
                    new DealerZone { OrgId = orgId, DealerCode = "VN001", DealerName = "Hyundai Đông Đô", ZoneCode = "MB", ZoneName = "Miền Bắc", Remark = "Đại lý khu vực Hà Nội", FlagActive = "1", LogLUDTime = DateTime.Now.AddDays(-45), LogLUBy = "SYSADMIN" },
                    new DealerZone { OrgId = orgId, DealerCode = "VN002", DealerName = "Hyundai Nam Trung", ZoneCode = "MN", ZoneName = "Miền Nam", Remark = "Đại lý khu vực phía Nam", FlagActive = "1", LogLUDTime = DateTime.Now.AddDays(-45), LogLUBy = "SYSADMIN" }
                );
            }

            // Hạn mức độ trễ vận tải theo Kho & Đại lý (Mst_DelayTransports / Master.1.cs / MstDelayTransportsController)
            if (!await db.DelayTransports.AnyAsync(o => o.OrgId == orgId))
            {
                db.DelayTransports.AddRange(
                    new DelayTransportMaster { OrgId = orgId, StorageCode = "KHO_TONG_HN", StorageName = "Kho Tổng Hyundai Ninh Bình", DealerCode = "VN001", DealerName = "Hyundai Đông Đô", DelayTransport = 3m, FlagActive = "1", LogLUDateTime = DateTime.Now.AddDays(-30), LogLUBy = "CHUYEN_VIEN_NPP" },
                    new DelayTransportMaster { OrgId = orgId, StorageCode = "KHO_TONG_HN", StorageName = "Kho Tổng Hyundai Ninh Bình", DealerCode = "VN002", DealerName = "Hyundai Nam Trung", DelayTransport = 5m, FlagActive = "1", LogLUDateTime = DateTime.Now.AddDays(-30), LogLUBy = "CHUYEN_VIEN_NPP" },
                    new DelayTransportMaster { OrgId = orgId, StorageCode = "KHO_TONG_SG", StorageName = "Kho Tổng Nam Bộ Hiệp Phước", DealerCode = "VN002", DealerName = "Hyundai Nam Trung", DelayTransport = 2m, FlagActive = "1", LogLUDateTime = DateTime.Now.AddDays(-20), LogLUBy = "CHUYEN_VIEN_NPP" }
                );
            }

            // Danh mục Dòng xe (Model) (Mst_CarModel / Master.Car.cs / Mst_CarModel_Get|Create|Update|Delete|Import)
            if (!await db.CarModels.AnyAsync(o => o.OrgId == orgId))
            {
                db.CarModels.AddRange(
                    new CarModelMaster { OrgId = orgId, ModelCode = "SF25", ModelProductionCode = "MX5", ModelName = "Santa Fe 2.5 HTRAC", FlagBusinessPlan = "1", FlagActive = "1", LogLUDateTime = DateTime.Now.AddDays(-90), LogLUBy = "SYSADMIN" },
                    new CarModelMaster { OrgId = orgId, ModelCode = "TU20", ModelProductionCode = "NX4", ModelName = "Tucson 2.0 AT", FlagBusinessPlan = "1", FlagActive = "1", LogLUDateTime = DateTime.Now.AddDays(-90), LogLUBy = "SYSADMIN" },
                    new CarModelMaster { OrgId = orgId, ModelCode = "CR15", ModelProductionCode = "SU2", ModelName = "Creta 1.5 Cao Cấp", FlagBusinessPlan = "1", FlagActive = "1", LogLUDateTime = DateTime.Now.AddDays(-90), LogLUBy = "SYSADMIN" },
                    new CarModelMaster { OrgId = orgId, ModelCode = "AC14", ModelProductionCode = "HC", ModelName = "Accent 1.4 AT", FlagBusinessPlan = "0", FlagActive = "1", LogLUDateTime = DateTime.Now.AddDays(-90), LogLUBy = "SYSADMIN" }
                );
            }

            // Danh mục Màu sắc xe (Mst_CarColor / Master.Car.cs / Mst_CarColor_Get|Create|Update|Delete|Import)
            if (!await db.CarColors.AnyAsync(o => o.OrgId == orgId))
            {
                db.CarColors.AddRange(
                    new CarColorMaster { OrgId = orgId, ModelCode = "SF25", ColorCode = "WHT", ColorExtType = "SOLID", ColorExtCode = "WHT", ColorExtName = "White", ColorExtNameVN = "Trắng", ColorIntCode = "BLK", ColorIntName = "Black", ColorIntNameVN = "Đen", ColorFee = 0m, FlagActive = "1", LogLUDateTime = DateTime.Now.AddDays(-90), LogLUBy = "SYSADMIN" },
                    new CarColorMaster { OrgId = orgId, ModelCode = "SF25", ColorCode = "BLK", ColorExtType = "METALLIC", ColorExtCode = "BLK", ColorExtName = "Black", ColorExtNameVN = "Đen", ColorIntCode = "BLK", ColorIntName = "Black", ColorIntNameVN = "Đen", ColorFee = 0m, FlagActive = "1", LogLUDateTime = DateTime.Now.AddDays(-90), LogLUBy = "SYSADMIN" },
                    new CarColorMaster { OrgId = orgId, ModelCode = "TU20", ColorCode = "GRY", ColorExtType = "METALLIC", ColorExtCode = "GRY", ColorExtName = "Grey", ColorExtNameVN = "Xám", ColorIntCode = "BLK", ColorIntName = "Black", ColorIntNameVN = "Đen", ColorFee = 5000000m, Remark = "Phụ phí màu xám", FlagActive = "1", LogLUDateTime = DateTime.Now.AddDays(-90), LogLUBy = "SYSADMIN" },
                    new CarColorMaster { OrgId = orgId, ModelCode = "CR15", ColorCode = "RED", ColorExtType = "METALLIC", ColorExtCode = "RED", ColorExtName = "Red", ColorExtNameVN = "Đỏ", ColorIntCode = "BLK", ColorIntName = "Black", ColorIntNameVN = "Đen", ColorFee = 0m, FlagActive = "1", LogLUDateTime = DateTime.Now.AddDays(-90), LogLUBy = "SYSADMIN" }
                );
            }

            // Phiên bản bảng cước phí vận tải (Mst_TranspFeeVer / Mst_TranspFee / Mst_TranspFeeHist / MstTranspFeeController)
            if (!await db.TransportFeeVersions.AnyAsync(o => o.OrgId == orgId))
            {
                var tfv = new TransportFeeVersion
                {
                    OrgId = orgId,
                    TFVCode = "TFV001",
                    FlagActive = "1",
                    CreatedDate = DateTime.Now.AddDays(-30),
                    LogLUDateTime = DateTime.Now.AddDays(-30),
                    LogLUBy = "CHUYEN_VIEN_NPP"
                };
                tfv.Details.AddRange(new[]
                {
                    new TransportFeeDetail { TFVCode = "TFV001", ProvinceCodeFrom = "74", DistrictCodeFrom = "742", ProvinceCodeTo = "79", DistrictCodeTo = "760", TransporterCode = "DVVT001", ModelCode = "BN7I-CKD", ValFee = 5000000m, ExpectedDays = 3, IsReverseRoute = false, LogLUDateTime = DateTime.Now.AddDays(-30), LogLUBy = "CHUYEN_VIEN_NPP" },
                    new TransportFeeDetail { TFVCode = "TFV001", ProvinceCodeFrom = "79", DistrictCodeFrom = "760", ProvinceCodeTo = "74", DistrictCodeTo = "742", TransporterCode = "DVVT001", ModelCode = "BN7I-CKD", ValFee = 5000000m, ExpectedDays = 3, IsReverseRoute = true, LogLUDateTime = DateTime.Now.AddDays(-30), LogLUBy = "CHUYEN_VIEN_NPP" },
                    new TransportFeeDetail { TFVCode = "TFV001", ProvinceCodeFrom = "01", DistrictCodeFrom = "001", ProvinceCodeTo = "79", DistrictCodeTo = "760", TransporterCode = "DVVT001", ModelCode = "SF25", ValFee = 8000000m, ExpectedDays = 5, IsReverseRoute = false, LogLUDateTime = DateTime.Now.AddDays(-30), LogLUBy = "CHUYEN_VIEN_NPP" },
                    new TransportFeeDetail { TFVCode = "TFV001", ProvinceCodeFrom = "79", DistrictCodeFrom = "760", ProvinceCodeTo = "01", DistrictCodeTo = "001", TransporterCode = "DVVT001", ModelCode = "SF25", ValFee = 8000000m, ExpectedDays = 5, IsReverseRoute = true, LogLUDateTime = DateTime.Now.AddDays(-30), LogLUBy = "CHUYEN_VIEN_NPP" }
                });
                db.TransportFeeVersions.Add(tfv);
                db.TransportFeeHistories.AddRange(
                    new TransportFeeHistory { OrgId = orgId, TFVCode = "TFV001", ProvinceCodeFrom = "74", DistrictCodeFrom = "742", ProvinceCodeTo = "79", DistrictCodeTo = "760", TransporterCodeList = "DVVT001", ModelCodeList = "BN7I-CKD", ValFee = 5000000m, ExpectedDays = 3, LogLUDateTime = DateTime.Now.AddDays(-30), LogLUBy = "CHUYEN_VIEN_NPP" },
                    new TransportFeeHistory { OrgId = orgId, TFVCode = "TFV001", ProvinceCodeFrom = "01", DistrictCodeFrom = "001", ProvinceCodeTo = "79", DistrictCodeTo = "760", TransporterCodeList = "DVVT001", ModelCodeList = "SF25", ValFee = 8000000m, ExpectedDays = 5, LogLUDateTime = DateTime.Now.AddDays(-30), LogLUBy = "CHUYEN_VIEN_NPP" }
                );
            }

            // Chế tài / Vi phạm Nhân viên Bán hàng (HR_SalesManViolate / MasterData.HR.cs)
            if (!await db.SalesManViolates.AnyAsync(o => o.OrgId == orgId))
            {
                db.SalesManViolates.AddRange(
                    new SalesManViolate { OrgId = orgId, SMCode = "SM001", ViolateNumber = 1, DealerCode = "VN001", DealerName = "Hyundai Đông Đô", ViolateType = ViolateType.Temporary, ViolateDateStart = DateTime.Today.AddDays(-20), ViolateDateEnd = DateTime.Today.AddDays(10), SMHyundaiCode = "HD001", SMName = "Nguyễn Văn An", SMDateOfBirth = new DateTime(1990, 5, 12), IdentityCardNo = "001090012345", SMPhoneNo = "0912345678", SMType = "TVBH", SMStatus = "CHINGTHUC", Remark = "Vi phạm quy trình tư vấn bán hàng", FlagActive = "1", CreatedBy = "CHUYEN_VIEN_NPP", CreatedAt = DateTime.Now.AddDays(-20), LogLUDateTime = DateTime.Now.AddDays(-20), LogLUBy = "CHUYEN_VIEN_NPP" },
                    new SalesManViolate { OrgId = orgId, SMCode = "SM002", ViolateNumber = 1, DealerCode = "VN002", DealerName = "Hyundai Nam Trung", ViolateType = ViolateType.Permanent, ViolateDateStart = DateTime.Today.AddDays(-15), ViolateDateEnd = null, SMHyundaiCode = "HD002", SMName = "Trần Thị Bình", SMDateOfBirth = new DateTime(1992, 8, 3), IdentityCardNo = "079092023456", SMPhoneNo = "0987654321", SMType = "TVBH", SMStatus = "CHINGTHUC", Remark = "Vi phạm nghiêm trọng — gian lận doanh số", FlagActive = "1", CreatedBy = "CHUYEN_VIEN_NPP", CreatedAt = DateTime.Now.AddDays(-15), LogLUDateTime = DateTime.Now.AddDays(-15), LogLUBy = "CHUYEN_VIEN_NPP" }
                );
            }

            // Lượt khách đến thăm đại lý (Dlr_CtmVisit / DlrCtmVisitController)
            if (!await db.CustomerVisits.AnyAsync(o => o.OrgId == orgId))
            {
                db.CustomerVisits.AddRange(
                    new CustomerVisit { OrgId = orgId, CtmVisitCode = "CV26090001", DealerCode = "VN001", DealerName = "Hyundai Đông Đô", Gender = CustomerVisitGender.Male, RangeAgeCode = "25-34", ModelCode = "SF25", ModelName = "Santa Fe 2.5 HTRAC", VisitDTime = DateTime.Now.AddDays(-5), FlagActive = "1", Remark = "Khách quan tâm Santa Fe", CreatedBy = "CHUYEN_VIEN_DL", CreatedAt = DateTime.Now.AddDays(-5), LogLUBy = "CHUYEN_VIEN_DL", LogLUDTime = DateTime.Now.AddDays(-5) },
                    new CustomerVisit { OrgId = orgId, CtmVisitCode = "CV26090002", DealerCode = "VN001", DealerName = "Hyundai Đông Đô", Gender = CustomerVisitGender.Female, RangeAgeCode = "35-44", ModelCode = "TU20", ModelName = "Tucson 2.0 AT", VisitDTime = DateTime.Now.AddDays(-3), FlagActive = "1", Remark = "Khách xem Tucson", CreatedBy = "CHUYEN_VIEN_DL", CreatedAt = DateTime.Now.AddDays(-3), LogLUBy = "CHUYEN_VIEN_DL", LogLUDTime = DateTime.Now.AddDays(-3) },
                    new CustomerVisit { OrgId = orgId, CtmVisitCode = "CV26090003", DealerCode = "VN002", DealerName = "Hyundai Nam Trung", Gender = CustomerVisitGender.Male, RangeAgeCode = "45-54", ModelCode = "CR15", ModelName = "Creta 1.5 Cao Cấp", VisitDTime = DateTime.Now.AddDays(-1), FlagActive = "1", Remark = "Khách xem Creta", CreatedBy = "CHUYEN_VIEN_DL", CreatedAt = DateTime.Now.AddDays(-1), LogLUBy = "CHUYEN_VIEN_DL", LogLUDTime = DateTime.Now.AddDays(-1) }
                );
            }

            // Hạn mức đặt hàng xe theo Đại lý & Quy cách (Mng_Quota / Master.cs / Mng_Quota_Get|Create|Update|Delete|Import)
            if (!await db.Quotas.AnyAsync(o => o.OrgId == orgId))
            {
                db.Quotas.AddRange(
                    new QuotaMaster { OrgId = orgId, DealerCode = "VN001", DealerName = "Hyundai Đông Đô", SpecCode = "SF25-2.5T-AWD", SpecDescription = "Santa Fe 2.5 Turbo AWD Calligraphy", ModelCode = "SF25", ModelName = "Santa Fe 2.5 HTRAC", QtyQuota = 20m, FlagActive = "1", UpdateBy = "CHUYEN_VIEN_NPP", UpdateDTime = DateTime.Now.AddDays(-20), LogLUDateTime = DateTime.Now.AddDays(-20), LogLUBy = "CHUYEN_VIEN_NPP" },
                    new QuotaMaster { OrgId = orgId, DealerCode = "VN001", DealerName = "Hyundai Đông Đô", SpecCode = "TU20-2.0AT", SpecDescription = "Tucson 2.0 AT Tiêu chuẩn", ModelCode = "TU20", ModelName = "Tucson 2.0 AT", QtyQuota = 35m, FlagActive = "1", UpdateBy = "CHUYEN_VIEN_NPP", UpdateDTime = DateTime.Now.AddDays(-20), LogLUDateTime = DateTime.Now.AddDays(-20), LogLUBy = "CHUYEN_VIEN_NPP" },
                    new QuotaMaster { OrgId = orgId, DealerCode = "VN002", DealerName = "Hyundai Nam Trung", SpecCode = "CR15-1.5AT", SpecDescription = "Creta 1.5 AT Cao Cấp", ModelCode = "CR15", ModelName = "Creta 1.5 Cao Cấp", QtyQuota = 25m, FlagActive = "1", UpdateBy = "CHUYEN_VIEN_NPP", UpdateDTime = DateTime.Now.AddDays(-15), LogLUDateTime = DateTime.Now.AddDays(-15), LogLUBy = "CHUYEN_VIEN_NPP" },
                    new QuotaMaster { OrgId = orgId, DealerCode = "VN002", DealerName = "Hyundai Nam Trung", SpecCode = "AC14-1.4AT", SpecDescription = "Accent 1.4 AT Đặc biệt", ModelCode = "AC14", ModelName = "Accent 1.4 AT", QtyQuota = 40m, FlagActive = "1", UpdateBy = "CHUYEN_VIEN_NPP", UpdateDTime = DateTime.Now.AddDays(-15), LogLUDateTime = DateTime.Now.AddDays(-15), LogLUBy = "CHUYEN_VIEN_NPP" }
                );
            }

            // Danh mục Ngân hàng đối tác (Mst_Bank / Master.cs / Mst_Bank_Get|Create|Update|Delete|Import)
            if (!await db.Banks.AnyAsync(o => o.OrgId == orgId))
            {
                db.Banks.AddRange(
                    new BankMaster { OrgId = orgId, BankCode = "VCB", BankCodeParent = "ALL", ProvinceCode = "HN", BankName = "Ngân hàng TMCP Ngoại thương Việt Nam", PhoneNo = "02439341367", FaxNo = "02439341368", BenBankCode = "VCB", Address = "198 Trần Quang Khải, Hoàn Kiếm, Hà Nội", PICEmail = "cskh@vietcombank.com.vn", PICName = "Nguyễn Thị Hoa", FlagPaymentBank = "1", FlagMortageBank = "1", FlagMonitorBank = "1", Remark = "Ngân hàng bảo lãnh chính", FlagActive = "1", LogLUDateTime = DateTime.Now.AddDays(-60), LogLUBy = "SYSADMIN" },
                    new BankMaster { OrgId = orgId, BankCode = "BIDV", BankCodeParent = "ALL", ProvinceCode = "HN", BankName = "Ngân hàng TMCP Đầu tư và Phát triển Việt Nam", PhoneNo = "02422205588", FaxNo = "02422205589", BenBankCode = "BIDV", Address = "35 Hàng Vôi, Hoàn Kiếm, Hà Nội", PICEmail = "cskh@bidv.com.vn", PICName = "Trần Văn Nam", FlagPaymentBank = "1", FlagMortageBank = "1", FlagMonitorBank = "1", Remark = "Ngân hàng tài trợ vốn", FlagActive = "1", LogLUDateTime = DateTime.Now.AddDays(-60), LogLUBy = "SYSADMIN" },
                    new BankMaster { OrgId = orgId, BankCode = "TCB", BankCodeParent = "ALL", ProvinceCode = "HN", BankName = "Ngân hàng TMCP Kỹ thương Việt Nam", PhoneNo = "02436366699", FaxNo = "02436366698", BenBankCode = "TCB", Address = "191 Bà Triệu, Hai Bà Trưng, Hà Nội", PICEmail = "cskh@techcombank.com.vn", PICName = "Lê Thị Mai", FlagPaymentBank = "1", FlagMortageBank = "1", FlagMonitorBank = "0", Remark = "Ngân hàng thanh toán", FlagActive = "1", LogLUDateTime = DateTime.Now.AddDays(-45), LogLUBy = "SYSADMIN" },
                    new BankMaster { OrgId = orgId, BankCode = "VPB", BankCodeParent = "ALL", ProvinceCode = "HN", BankName = "Ngân hàng TMCP Việt Nam Thịnh Vượng", PhoneNo = "02439366688", FaxNo = "02439366689", BenBankCode = "VPB", Address = "89 Láng Hạ, Đống Đa, Hà Nội", PICEmail = "cskh@vpbank.com.vn", PICName = "Phạm Văn Dũng", FlagPaymentBank = "1", FlagMortageBank = "0", FlagMonitorBank = "0", Remark = "Ngân hàng giải ngân", FlagActive = "1", LogLUDateTime = DateTime.Now.AddDays(-30), LogLUBy = "SYSADMIN" },
                    new BankMaster { OrgId = orgId, BankCode = "VIB", BankCodeParent = "ALL", ProvinceCode = "HN", BankName = "Ngân hàng TMCP Quốc tế Việt Nam", PhoneNo = "02439366655", FaxNo = "02439366656", BenBankCode = "VIB", Address = "Tòa nhà Sailing Tower, 111A Pasteur, Q.1, TP.HCM", PICEmail = "cskh@vib.com.vn", PICName = "Hoàng Thị Lan", FlagPaymentBank = "0", FlagMortageBank = "1", FlagMonitorBank = "0", Remark = "Ngân hàng thế chấp", FlagActive = "1", LogLUDateTime = DateTime.Now.AddDays(-20), LogLUBy = "SYSADMIN" }
                );
            }

            // Yêu cầu vận chuyển khi chuyển kho (Sto_RearrangeTranspReq / Storage.cs / FrmMngRearrangeTranspReq)
            if (!await db.RearrangeTransportRequests.AnyAsync(o => o.OrgId == orgId))
            {
                db.RearrangeTransportRequests.AddRange(
                    new RearrangeTransportRequest
                    {
                        OrgId = orgId,
                        SRTReqNo = "SRT26090001",
                        TransporterCode = "VTL001",
                        TransporterName = "Công ty Vận tải Hòa Bình",
                        TransportContractNo = "HĐVT-2026-001",
                        StorageCodeFrom = "KHO_HPH",
                        StorageCodeTo = "KHO_TT",
                        Status = RearrangeTranspReqStatus.Pending,
                        TotalCars = 2,
                        PlateNo = "29H-123.45",
                        DriverName = "Nguyễn Văn Tài",
                        DriverPhone = "0912345678",
                        ExpectedStartDate = DateTime.Today.AddDays(1),
                        ExpectedEndDate = DateTime.Today.AddDays(2),
                        Remark = "Điều chuyển xe từ kho cảng về kho trung tâm",
                        CreatedBy = "HQ_LOGISTICS_USER",
                        CreatedAt = DateTime.Now.AddDays(-1),
                        Details = new List<RearrangeTransportRequestDetail>
                        {
                            new RearrangeTransportRequestDetail { SRTReqNo = "SRT26090001", Vin = "KMHXX00A1NU123456", Model = "Santa Fe 2.5 HTRAC", RefOrdNo = "SR26090001", StorageCodeFrom = "KHO_HPH", StorageCodeTo = "KHO_TT", Status = RearrangeTranspReqDtlStatus.Pending },
                            new RearrangeTransportRequestDetail { SRTReqNo = "SRT26090001", Vin = "KMHXX00A1NU123457", Model = "Tucson 2.0 AT", RefOrdNo = "SR26090001", StorageCodeFrom = "KHO_HPH", StorageCodeTo = "KHO_TT", Status = RearrangeTranspReqDtlStatus.Pending }
                        }
                    },
                    new RearrangeTransportRequest
                    {
                        OrgId = orgId,
                        SRTReqNo = "SRT26090002",
                        TransporterCode = "VTL002",
                        TransporterName = "Công ty Vận tải Thăng Long",
                        TransportContractNo = "HĐVT-2026-002",
                        StorageCodeFrom = "KHO_TT",
                        StorageCodeTo = "KHO_DL",
                        Status = RearrangeTranspReqStatus.Approved,
                        TotalCars = 1,
                        PlateNo = "29H-678.90",
                        DriverName = "Trần Văn Lái",
                        DriverPhone = "0987654321",
                        ExpectedStartDate = DateTime.Today.AddDays(-2),
                        ExpectedEndDate = DateTime.Today.AddDays(-1),
                        Remark = "Điều chuyển xe về kho đại lý",
                        CreatedBy = "HQ_LOGISTICS_USER",
                        CreatedAt = DateTime.Now.AddDays(-3),
                        ApprovedBy = "TP_DIEUPHOI_LOGISTICS",
                        ApprovedAt = DateTime.Now.AddDays(-2),
                        Details = new List<RearrangeTransportRequestDetail>
                        {
                            new RearrangeTransportRequestDetail { SRTReqNo = "SRT26090002", Vin = "KMHXX00A1NU123458", Model = "Creta 1.5 Cao Cấp", RefOrdNo = "SR26090002", StorageCodeFrom = "KHO_TT", StorageCodeTo = "KHO_DL", Status = RearrangeTranspReqDtlStatus.Approved }
                        }
                    }
                );
            }

            // Phân bổ Nhu cầu / Cung cấp đơn đặt hàng xe (DMS40_Ord_SalesOrderRoot_ApprAuto / DMS40.0.30.Order.cs / 04_DON_HANG_DOANH_SO.md)
            if (!await db.OrderAllocationSessions.AnyAsync(o => o.OrgId == orgId))
            {
                db.OrderAllocationSessions.AddRange(
                    new OrderAllocationSession
                    {
                        OrgId = orgId,
                        AllocationNo = "ALC26090001",
                        PeriodMonth = "202601",
                        SalesPolicyCode = "CSC",
                        Status = OrderAllocationStatus.Draft,
                        TotalDemandQty = 30,
                        TotalSupplyQty = 20,
                        TotalAllocatedQty = 0,
                        TotalUnmetQty = 30,
                        Remark = "Phân bổ nhu cầu/cung kỳ 202601 cho đại lý miền Bắc",
                        CreatedBy = "HQ_SALES_USER",
                        CreatedAt = DateTime.Now.AddDays(-2),
                        Demands = new List<OrderDemandAllocation>
                        {
                            new OrderDemandAllocation { AllocationNo = "ALC26090001", DealerCode = "VN001", PeriodMonth = "202601", SpecCode = "SF25-2.5T-AWD", ModelCode = "SF25", ColorCode = "WHT", AssemblyStatus = "CBU", QtyInit = 10, QtyProcess = 0, QtyRemain = 10, Status = OrderDemandStatus.Pending },
                            new OrderDemandAllocation { AllocationNo = "ALC26090001", DealerCode = "VN002", PeriodMonth = "202601", SpecCode = "SF25-2.5T-AWD", ModelCode = "SF25", ColorCode = "WHT", AssemblyStatus = "CBU", QtyInit = 20, QtyProcess = 0, QtyRemain = 20, Status = OrderDemandStatus.Pending }
                        },
                        Supplies = new List<OrderSupplyAllocation>
                        {
                            new OrderSupplyAllocation { AllocationNo = "ALC26090001", PeriodMonth = "202601", SpecCode = "SF25-2.5T-AWD", ModelCode = "SF25", ColorCode = "WHT", QtyInit = 20, QtyProcess = 0, QtyRemain = 20, Status = OrderSupplyStatus.Pending }
                        }
                    },
                    new OrderAllocationSession
                    {
                        OrgId = orgId,
                        AllocationNo = "ALC26090002",
                        PeriodMonth = "202602",
                        SalesPolicyCode = "STANDARD",
                        Status = OrderAllocationStatus.Allocated,
                        TotalDemandQty = 15,
                        TotalSupplyQty = 15,
                        TotalAllocatedQty = 15,
                        TotalUnmetQty = 0,
                        Remark = "Phân bổ nhu cầu/cung kỳ 202602 đã chạy phân bổ",
                        CreatedBy = "HQ_SALES_USER",
                        CreatedAt = DateTime.Now.AddDays(-5),
                        AllocatedBy = "HQ_SALES_USER",
                        AllocatedAt = DateTime.Now.AddDays(-4),
                        Demands = new List<OrderDemandAllocation>
                        {
                            new OrderDemandAllocation { AllocationNo = "ALC26090002", DealerCode = "VN001", PeriodMonth = "202602", SpecCode = "TU20-2.0AT", ModelCode = "TU20", ColorCode = "BLK", AssemblyStatus = "CKD", QtyInit = 15, QtyProcess = 15, QtyRemain = 0, DemandPercent = 1m, Rank = 1, Status = OrderDemandStatus.Allocated }
                        },
                        Supplies = new List<OrderSupplyAllocation>
                        {
                            new OrderSupplyAllocation { AllocationNo = "ALC26090002", PeriodMonth = "202602", SpecCode = "TU20-2.0AT", ModelCode = "TU20", ColorCode = "BLK", QtyInit = 15, QtyProcess = 15, QtyRemain = 0, SupplyPercent = 1m, Rank = 1, Status = OrderSupplyStatus.Allocated }
                        }
                    }
                );
            }

            await db.SaveChangesAsync();
        }
    }
}