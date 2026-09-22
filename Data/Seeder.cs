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
    }
}
