using Microsoft.EntityFrameworkCore;
using MiniSales.Data;
using MiniSales.Models;

namespace MiniSales.Services;

public record PlanEstimateOrderDetailInputDto(
    string ModelCode,
    string? ModelName = null,
    string? SpecCode = null,
    string? SpecDescription = null,
    int QtySellCustomer = 0,
    int QtySellDealer = 0,
    int QtyInStock = 0,
    int QtyOnWay = 0,
    int QtyBuyDealer = 0,
    int QtyUnknown = 0,
    int QtyBOChuaXuatKho = 0,
    int QtyBODangMapVIN = 0,
    int QtyBOKhongVINQuaKhu = 0,
    int QtyBOKhongVINHienTai = 0,
    int QtyBOKhongVINTuongLai = 0,
    int QtyOrdCarID = 0,
    int QtyOrdNotCarID = 0,
    int QtyEOrdN1 = 0,
    int QtyESellCusN0 = 0,
    int QtyESellCusN1 = 0,
    int QtyESellCusN2 = 0,
    int QtyESellCusN3 = 0,
    string? Remark = null
);

public record CreatePlanEstimateOrderDto(
    string DealerCode,
    string MonthEstimate, // yyyy-MM vd: "2026-04"
    string? DealerName = null,
    string? BusinessPlanCode = null,
    string? AreaRootCode = null,
    string? Remark = null,
    string? CreatedBy = null,
    List<PlanEstimateOrderDetailInputDto>? Details = null
);

public record UpdatePlanEstimateOrderDto(
    string? BusinessPlanCode = null,
    string? AreaRootCode = null,
    string? Remark = null,
    string? UpdatedBy = null,
    List<PlanEstimateOrderDetailInputDto>? Details = null
);

public record CalculateEstimateInputDto(
    string DealerCode,
    string MonthEstimate, // yyyy-MM
    string? BusinessPlanCode = null
);

public record CalculatedEstimateRowDto(
    string ModelCode,
    string ModelName,
    string? SpecCode,
    string? SpecDescription,
    int QtyInStock,
    int QtyOnWay,
    int QtyBuyDealer,
    int QtyBODangMapVIN,
    int QtyBOChuaXuatKho,
    int TongHangCo,
    decimal TiLeHangCo,
    int QtyESellCusN0,
    int QtyESellCusN1,
    int QtyESellCusN2,
    int QtyESellCusN3,
    int TongDuKienBanLe,
    int QtyEOrdN1,
    decimal TiLeDatTN1,
    int DuPhongTonKho,
    decimal TiLeDuPhongTonKho
);

public record CalculateEstimateResultDto(
    string DealerCode,
    string MonthEstimate,
    string? BusinessPlanCode,
    int TotalQtyN1,
    int TotalSellCusN0,
    int TotalSellCusN1,
    int TotalSellCusN2,
    int TotalSellCusN3,
    List<CalculatedEstimateRowDto> Rows
);

public record CancelPlanEstimateOrderDto(
    string Reason,
    string? CancelledBy = null
);

public record PlanEstimateOrderStatsDto(
    int TotalOrders,
    int PendingCount,
    int Approved1Count,
    int Approved2Count,
    int CancelledCount,
    int TotalDemandN1Cars,
    Dictionary<string, int> DemandByModel,
    Dictionary<string, int> DemandByDealer,
    Dictionary<string, int> DemandByArea
);

public interface IPlanEstimateOrderService
{
    Task<string> GetNextPLEOrdNoSeqAsync();
    Task<List<PlanEstimateOrder>> SearchAsync(
        string? dealerCode = null,
        string? areaRootCode = null,
        string? monthEstimate = null,
        PlanEstimateOrderStatus? status = null,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        int pageIndex = 0,
        int pageSize = 50
    );
    Task<PlanEstimateOrder?> GetByPLEOrdNoAsync(string pleOrdNo);
    Task<CalculateEstimateResultDto> CalculateEstimateAsync(CalculateEstimateInputDto dto);
    Task<PlanEstimateOrder> CreateAsync(CreatePlanEstimateOrderDto dto);
    Task<PlanEstimateOrder?> UpdateAsync(string pleOrdNo, UpdatePlanEstimateOrderDto dto);
    Task<PlanEstimateOrder?> Approve1Async(string pleOrdNo, string? approvedBy = null);
    Task<PlanEstimateOrder?> Approve2Async(string pleOrdNo, string? approvedBy = null);
    Task<PlanEstimateOrder?> CancelAsync(string pleOrdNo, CancelPlanEstimateOrderDto dto);
    Task<bool> DeleteDraftAsync(string pleOrdNo);
    Task<PlanEstimateOrderStatsDto> GetStatsAsync();
}

public class PlanEstimateOrderService : IPlanEstimateOrderService
{
    private readonly AppDbContext _db;
    private readonly ITenantContext _tenant;

    private static readonly (string Code, string Name, string Spec, string SpecDesc)[] StandardModels = new[]
    {
        ("SANTAFE", "Hyundai Santa Fe All-New", "SF25-PREM", "Santa Fe 2.5 HTRAC Xăng Cao cấp"),
        ("TUCSON", "Hyundai Tucson Turbo", "TU20-TRB", "Tucson 1.6T HTRAC Turbo"),
        ("CRETA", "Hyundai Creta Smart", "CR15-SMT", "Creta 1.5 Smart CVT"),
        ("ACCENT", "Hyundai Accent Sedan", "AC15-AT", "Accent 1.5 AT Đặc biệt"),
        ("STARGAZER", "Hyundai Stargazer X", "SG-X", "Stargazer X 1.5 CVT MPV"),
        ("CUSTIN", "Hyundai Custin MPV", "CU15-PRE", "Custin 1.5T-GDi Cao Cấp"),
        ("IONIQ5", "Hyundai IONIQ 5 EV", "IQ5-EXT", "IONIQ 5 Prestige EV"),
        ("ELANTRA", "Hyundai Elantra N-Line", "EL16-NL", "Elantra 1.6 Turbo N-Line")
    };

    public PlanEstimateOrderService(AppDbContext db, ITenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    public async Task<string> GetNextPLEOrdNoSeqAsync()
    {
        var prefix = DateTime.Now.ToString("yyMM") + "PLE";
        var count = await _db.PlanEstimateOrders
            .Where(x => x.OrgId == _tenant.OrgId && x.PLEOrdNo.StartsWith(prefix))
            .CountAsync();
        return $"{prefix}{(count + 1):D5}";
    }

    public async Task<List<PlanEstimateOrder>> SearchAsync(
        string? dealerCode = null,
        string? areaRootCode = null,
        string? monthEstimate = null,
        PlanEstimateOrderStatus? status = null,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        int pageIndex = 0,
        int pageSize = 50)
    {
        var query = _db.PlanEstimateOrders
            .Include(x => x.Details)
            .Where(x => x.OrgId == _tenant.OrgId)
            .AsNoTracking();

        if (!string.IsNullOrWhiteSpace(dealerCode))
        {
            var d = dealerCode.Trim();
            query = query.Where(x => x.DealerCode == d);
        }

        if (!string.IsNullOrWhiteSpace(areaRootCode))
        {
            var a = areaRootCode.Trim().ToUpperInvariant();
            query = query.Where(x => x.AreaRootCode == a);
        }

        if (!string.IsNullOrWhiteSpace(monthEstimate))
        {
            var m = monthEstimate.Trim();
            query = query.Where(x => x.MonthEstimate == m);
        }

        if (status.HasValue)
        {
            query = query.Where(x => x.Status == status.Value);
        }

        if (fromDate.HasValue)
        {
            query = query.Where(x => x.CreatedAt >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            query = query.Where(x => x.CreatedAt <= toDate.Value);
        }

        return await query
            .OrderByDescending(x => x.CreatedAt)
            .Skip(pageIndex * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<PlanEstimateOrder?> GetByPLEOrdNoAsync(string pleOrdNo)
    {
        var no = pleOrdNo.Trim();
        return await _db.PlanEstimateOrders
            .Include(x => x.Details)
            .FirstOrDefaultAsync(x => x.OrgId == _tenant.OrgId && x.PLEOrdNo == no);
    }

    public async Task<CalculateEstimateResultDto> CalculateEstimateAsync(CalculateEstimateInputDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.DealerCode))
            throw new InvalidOperationException("Mã đại lý (DealerCode) không được để trống.");
        if (string.IsNullOrWhiteSpace(dto.MonthEstimate))
            throw new InvalidOperationException("Tháng dự kiến (MonthEstimate: yyyy-MM) không được để trống.");

        var dealerCode = dto.DealerCode.Trim();
        var monthEstimate = dto.MonthEstimate.Trim();

        // Parse month
        if (!DateTime.TryParse($"{monthEstimate}-01", out var targetMonth))
        {
            throw new InvalidOperationException("Tháng dự kiến không đúng định dạng yyyy-MM (vd: 2026-04).");
        }

        int year = targetMonth.Year;
        int targetM = targetMonth.Month; // N

        // Check if business plan exists for this dealer
        BusinessPlan? bpl = null;
        if (!string.IsNullOrWhiteSpace(dto.BusinessPlanCode))
        {
            bpl = await _db.BusinessPlans
                .Include(b => b.Details)
                .FirstOrDefaultAsync(b => b.OrgId == _tenant.OrgId && b.BusinessPlanCode == dto.BusinessPlanCode.Trim());
        }
        bpl ??= await _db.BusinessPlans
            .Include(b => b.Details)
            .Where(b => b.OrgId == _tenant.OrgId && b.DealerCode == dealerCode && b.YearPlan == year && b.Status == BusinessPlanStatus.Approved2)
            .OrderByDescending(b => b.TimesPlan)
            .FirstOrDefaultAsync();

        // Fetch stock cars count per model from CarRecord
        var inStockByModel = await _db.Cars
            .Where(c => c.OrgId == _tenant.OrgId && c.DealerCode == dealerCode && c.FlagActive == "1")
            .GroupBy(c => c.ModelCode.ToUpper())
            .Select(g => new { ModelCode = g.Key, Count = g.Count() })
            .ToDictionaryAsync(g => g.ModelCode, g => g.Count);

        var rows = new List<CalculatedEstimateRowDto>();

        foreach (var m in StandardModels)
        {
            var modelKey = m.Code.ToUpper();
            inStockByModel.TryGetValue(modelKey, out var actualStock);
            if (actualStock == 0) actualStock = 3; // Mặc định tồn kho cơ sở

            // Tìm chi tiết kế hoạch kinh doanh cho model
            var bplDtl = bpl?.Details.FirstOrDefault(d => string.Equals(d.ModelCode, m.Code, StringComparison.OrdinalIgnoreCase));

            int rtlN0 = GetBplRtlQty(bplDtl, targetM);
            int rtlN1 = GetBplRtlQty(bplDtl, targetM + 1);
            int rtlN2 = GetBplRtlQty(bplDtl, targetM + 2);
            int rtlN3 = GetBplRtlQty(bplDtl, targetM + 3);

            if (rtlN0 == 0) rtlN0 = 10;
            if (rtlN1 == 0) rtlN1 = 12;
            if (rtlN2 == 0) rtlN2 = 12;
            if (rtlN3 == 0) rtlN3 = 14;

            int onWay = 2;
            int buyDealer = 0;
            int boMapVin = 1;
            int boChuaXuat = 2;

            int tongHangCo = actualStock + onWay + buyDealer + boMapVin;
            int tongDuKien = rtlN0 + rtlN1 + rtlN2 + rtlN3;

            // Đề xuất đặt hàng N+1 theo logic DMS:
            int ordN1Plan = GetBplOrdQty(bplDtl, targetM + 1);
            int qtyEOrdN1 = ordN1Plan > 0 ? ordN1Plan : Math.Max(0, (rtlN0 + rtlN1) - tongHangCo + 2);

            int demandSum2M = rtlN0 + rtlN1;
            decimal tiLeHangCo = demandSum2M > 0 ? Math.Round((decimal)tongHangCo / demandSum2M * 100m, 1) : 100m;
            decimal tiLeDatN1 = rtlN1 > 0 ? Math.Round((decimal)qtyEOrdN1 / rtlN1 * 100m, 1) : 100m;

            int duPhong = tongHangCo + qtyEOrdN1 - demandSum2M;
            decimal tiLeDuPhong = demandSum2M > 0 ? Math.Round((decimal)duPhong / demandSum2M * 100m, 1) : 0m;

            rows.Add(new CalculatedEstimateRowDto(
                ModelCode: m.Code,
                ModelName: m.Name,
                SpecCode: m.Spec,
                SpecDescription: m.SpecDesc,
                QtyInStock: actualStock,
                QtyOnWay: onWay,
                QtyBuyDealer: buyDealer,
                QtyBODangMapVIN: boMapVin,
                QtyBOChuaXuatKho: boChuaXuat,
                TongHangCo: tongHangCo,
                TiLeHangCo: tiLeHangCo,
                QtyESellCusN0: rtlN0,
                QtyESellCusN1: rtlN1,
                QtyESellCusN2: rtlN2,
                QtyESellCusN3: rtlN3,
                TongDuKienBanLe: tongDuKien,
                QtyEOrdN1: qtyEOrdN1,
                TiLeDatTN1: tiLeDatN1,
                DuPhongTonKho: duPhong,
                TiLeDuPhongTonKho: tiLeDuPhong
            ));
        }

        return new CalculateEstimateResultDto(
            DealerCode: dealerCode,
            MonthEstimate: monthEstimate,
            BusinessPlanCode: bpl?.BusinessPlanCode,
            TotalQtyN1: rows.Sum(r => r.QtyEOrdN1),
            TotalSellCusN0: rows.Sum(r => r.QtyESellCusN0),
            TotalSellCusN1: rows.Sum(r => r.QtyESellCusN1),
            TotalSellCusN2: rows.Sum(r => r.QtyESellCusN2),
            TotalSellCusN3: rows.Sum(r => r.QtyESellCusN3),
            Rows: rows
        );
    }

    public async Task<PlanEstimateOrder> CreateAsync(CreatePlanEstimateOrderDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.DealerCode))
            throw new InvalidOperationException("Mã đại lý (DealerCode) không được để trống.");
        if (string.IsNullOrWhiteSpace(dto.MonthEstimate))
            throw new InvalidOperationException("Tháng dự kiến (MonthEstimate: yyyy-MM) không được để trống.");

        var dealerCode = dto.DealerCode.Trim();
        var monthEstimate = dto.MonthEstimate.Trim();

        // Check if an active estimate order already exists for this dealer and month
        var exists = await _db.PlanEstimateOrders.AnyAsync(x =>
            x.OrgId == _tenant.OrgId &&
            x.DealerCode == dealerCode &&
            x.MonthEstimate == monthEstimate &&
            x.Status != PlanEstimateOrderStatus.Cancelled);

        if (exists)
        {
            throw new InvalidOperationException($"Đại lý {dealerCode} đã lập kế hoạch dự kiến đặt hàng cho tháng {monthEstimate}. Không được lập trùng.");
        }

        var pleOrdNo = await GetNextPLEOrdNoSeqAsync();
        var dealerName = dto.DealerName ?? GetDealerName(dealerCode);

        // Derive AreaRootCode if missing
        var areaRootCode = dto.AreaRootCode ?? DeriveAreaRootCode(dealerCode);

        var order = new PlanEstimateOrder
        {
            OrgId = _tenant.OrgId,
            PLEOrdNo = pleOrdNo,
            DealerCode = dealerCode,
            DealerName = dealerName,
            MonthEstimate = monthEstimate,
            BusinessPlanCode = dto.BusinessPlanCode?.Trim(),
            AreaRootCode = areaRootCode,
            Status = PlanEstimateOrderStatus.Pending,
            Remark = dto.Remark,
            CreatedBy = dto.CreatedBy ?? "DEALER_PLAN_USER",
            CreatedAt = DateTime.Now
        };

        if (dto.Details != null && dto.Details.Count > 0)
        {
            foreach (var d in dto.Details)
            {
                var tongHangCo = d.QtyInStock + d.QtyOnWay + d.QtyBuyDealer + d.QtyBODangMapVIN;
                var demand2M = d.QtyESellCusN0 + d.QtyESellCusN1;
                var tiLeHangCo = demand2M > 0 ? Math.Round((decimal)tongHangCo / demand2M * 100m, 1) : 0m;
                var duPhong = tongHangCo + d.QtyEOrdN1 - demand2M;
                var tiLeDuPhong = demand2M > 0 ? Math.Round((decimal)duPhong / demand2M * 100m, 1) : 0m;

                order.Details.Add(new PlanEstimateOrderDetail
                {
                    PLEOrdNo = pleOrdNo,
                    ModelCode = d.ModelCode.Trim(),
                    ModelName = d.ModelName ?? d.ModelCode.Trim(),
                    SpecCode = d.SpecCode?.Trim(),
                    SpecDescription = d.SpecDescription,
                    QtySellCustomer = d.QtySellCustomer,
                    QtySellDealer = d.QtySellDealer,
                    QtyInStock = d.QtyInStock,
                    QtyOnWay = d.QtyOnWay,
                    QtyBuyDealer = d.QtyBuyDealer,
                    QtyUnknown = d.QtyUnknown,
                    QtyBOChuaXuatKho = d.QtyBOChuaXuatKho,
                    QtyBODangMapVIN = d.QtyBODangMapVIN,
                    QtyBOKhongVINQuaKhu = d.QtyBOKhongVINQuaKhu,
                    QtyBOKhongVINHienTai = d.QtyBOKhongVINHienTai,
                    QtyBOKhongVINTuongLai = d.QtyBOKhongVINTuongLai,
                    QtyOrdCarID = d.QtyOrdCarID,
                    QtyOrdNotCarID = d.QtyOrdNotCarID,
                    QtyEOrdN1 = d.QtyEOrdN1,
                    TiLeDatTN1 = d.QtyESellCusN1 > 0 ? Math.Round((decimal)d.QtyEOrdN1 / d.QtyESellCusN1 * 100m, 1) : 100m,
                    QtyESellCusN0 = d.QtyESellCusN0,
                    QtyESellCusN1 = d.QtyESellCusN1,
                    QtyESellCusN2 = d.QtyESellCusN2,
                    QtyESellCusN3 = d.QtyESellCusN3,
                    TongHangCo = tongHangCo,
                    TiLeHangCo = tiLeHangCo,
                    DuPhongTonKho = duPhong,
                    TiLeDuPhongTonKho = tiLeDuPhong,
                    Remark = d.Remark
                });
            }
        }
        else
        {
            // Auto generate from default models if details not provided
            var calc = await CalculateEstimateAsync(new CalculateEstimateInputDto(dealerCode, monthEstimate, dto.BusinessPlanCode));
            foreach (var r in calc.Rows)
            {
                order.Details.Add(new PlanEstimateOrderDetail
                {
                    PLEOrdNo = pleOrdNo,
                    ModelCode = r.ModelCode,
                    ModelName = r.ModelName,
                    SpecCode = r.SpecCode,
                    SpecDescription = r.SpecDescription,
                    QtyInStock = r.QtyInStock,
                    QtyOnWay = r.QtyOnWay,
                    QtyBuyDealer = r.QtyBuyDealer,
                    QtyBODangMapVIN = r.QtyBODangMapVIN,
                    QtyBOChuaXuatKho = r.QtyBOChuaXuatKho,
                    QtyEOrdN1 = r.QtyEOrdN1,
                    TiLeDatTN1 = r.TiLeDatTN1,
                    QtyESellCusN0 = r.QtyESellCusN0,
                    QtyESellCusN1 = r.QtyESellCusN1,
                    QtyESellCusN2 = r.QtyESellCusN2,
                    QtyESellCusN3 = r.QtyESellCusN3,
                    TongHangCo = r.TongHangCo,
                    TiLeHangCo = r.TiLeHangCo,
                    DuPhongTonKho = r.DuPhongTonKho,
                    TiLeDuPhongTonKho = r.TiLeDuPhongTonKho
                });
            }
        }

        order.TotalQtyN1 = order.Details.Sum(x => x.QtyEOrdN1);
        order.TotalSellCusN0 = order.Details.Sum(x => x.QtyESellCusN0);
        order.TotalSellCusN1 = order.Details.Sum(x => x.QtyESellCusN1);
        order.TotalSellCusN2 = order.Details.Sum(x => x.QtyESellCusN2);
        order.TotalSellCusN3 = order.Details.Sum(x => x.QtyESellCusN3);

        _db.PlanEstimateOrders.Add(order);
        await _db.SaveChangesAsync();

        return order;
    }

    public async Task<PlanEstimateOrder?> UpdateAsync(string pleOrdNo, UpdatePlanEstimateOrderDto dto)
    {
        var order = await _db.PlanEstimateOrders
            .Include(x => x.Details)
            .FirstOrDefaultAsync(x => x.OrgId == _tenant.OrgId && x.PLEOrdNo == pleOrdNo.Trim());

        if (order == null) return null;

        if (order.Status != PlanEstimateOrderStatus.Pending)
        {
            throw new InvalidOperationException($"Chỉ được phép sửa đơn ước tính khi ở trạng thái Chờ duyệt (Pending). Trạng thái hiện tại: {order.Status}");
        }

        if (dto.BusinessPlanCode != null) order.BusinessPlanCode = dto.BusinessPlanCode.Trim();
        if (dto.AreaRootCode != null) order.AreaRootCode = dto.AreaRootCode.Trim().ToUpperInvariant();
        if (dto.Remark != null) order.Remark = dto.Remark;

        order.LogLUDateTime = DateTime.Now;
        order.LogLUBy = dto.UpdatedBy ?? "DEALER_USER";

        if (dto.Details != null && dto.Details.Count > 0)
        {
            _db.PlanEstimateOrderDetails.RemoveRange(order.Details);
            order.Details.Clear();

            foreach (var d in dto.Details)
            {
                var tongHangCo = d.QtyInStock + d.QtyOnWay + d.QtyBuyDealer + d.QtyBODangMapVIN;
                var demand2M = d.QtyESellCusN0 + d.QtyESellCusN1;
                var tiLeHangCo = demand2M > 0 ? Math.Round((decimal)tongHangCo / demand2M * 100m, 1) : 0m;
                var duPhong = tongHangCo + d.QtyEOrdN1 - demand2M;
                var tiLeDuPhong = demand2M > 0 ? Math.Round((decimal)duPhong / demand2M * 100m, 1) : 0m;

                order.Details.Add(new PlanEstimateOrderDetail
                {
                    PLEOrdNo = order.PLEOrdNo,
                    ModelCode = d.ModelCode.Trim(),
                    ModelName = d.ModelName ?? d.ModelCode.Trim(),
                    SpecCode = d.SpecCode?.Trim(),
                    SpecDescription = d.SpecDescription,
                    QtySellCustomer = d.QtySellCustomer,
                    QtySellDealer = d.QtySellDealer,
                    QtyInStock = d.QtyInStock,
                    QtyOnWay = d.QtyOnWay,
                    QtyBuyDealer = d.QtyBuyDealer,
                    QtyUnknown = d.QtyUnknown,
                    QtyBOChuaXuatKho = d.QtyBOChuaXuatKho,
                    QtyBODangMapVIN = d.QtyBODangMapVIN,
                    QtyBOKhongVINQuaKhu = d.QtyBOKhongVINQuaKhu,
                    QtyBOKhongVINHienTai = d.QtyBOKhongVINHienTai,
                    QtyBOKhongVINTuongLai = d.QtyBOKhongVINTuongLai,
                    QtyOrdCarID = d.QtyOrdCarID,
                    QtyOrdNotCarID = d.QtyOrdNotCarID,
                    QtyEOrdN1 = d.QtyEOrdN1,
                    TiLeDatTN1 = d.QtyESellCusN1 > 0 ? Math.Round((decimal)d.QtyEOrdN1 / d.QtyESellCusN1 * 100m, 1) : 100m,
                    QtyESellCusN0 = d.QtyESellCusN0,
                    QtyESellCusN1 = d.QtyESellCusN1,
                    QtyESellCusN2 = d.QtyESellCusN2,
                    QtyESellCusN3 = d.QtyESellCusN3,
                    TongHangCo = tongHangCo,
                    TiLeHangCo = tiLeHangCo,
                    DuPhongTonKho = duPhong,
                    TiLeDuPhongTonKho = tiLeDuPhong,
                    Remark = d.Remark
                });
            }

            order.TotalQtyN1 = order.Details.Sum(x => x.QtyEOrdN1);
            order.TotalSellCusN0 = order.Details.Sum(x => x.QtyESellCusN0);
            order.TotalSellCusN1 = order.Details.Sum(x => x.QtyESellCusN1);
            order.TotalSellCusN2 = order.Details.Sum(x => x.QtyESellCusN2);
            order.TotalSellCusN3 = order.Details.Sum(x => x.QtyESellCusN3);
        }

        await _db.SaveChangesAsync();
        return order;
    }

    public async Task<PlanEstimateOrder?> Approve1Async(string pleOrdNo, string? approvedBy = null)
    {
        var order = await _db.PlanEstimateOrders
            .Include(x => x.Details)
            .FirstOrDefaultAsync(x => x.OrgId == _tenant.OrgId && x.PLEOrdNo == pleOrdNo.Trim());

        if (order == null) return null;

        if (order.Status != PlanEstimateOrderStatus.Pending)
        {
            throw new InvalidOperationException($"Chỉ được duyệt cấp 1 khi đơn ở trạng thái Pending. Trạng thái hiện tại: {order.Status}");
        }

        order.Status = PlanEstimateOrderStatus.Approved1;
        order.Approve1At = DateTime.Now;
        order.Approve1By = approvedBy ?? "HTC_SALES_LEADER";
        order.LogLUDateTime = DateTime.Now;
        order.LogLUBy = order.Approve1By;

        await _db.SaveChangesAsync();
        return order;
    }

    public async Task<PlanEstimateOrder?> Approve2Async(string pleOrdNo, string? approvedBy = null)
    {
        var order = await _db.PlanEstimateOrders
            .Include(x => x.Details)
            .FirstOrDefaultAsync(x => x.OrgId == _tenant.OrgId && x.PLEOrdNo == pleOrdNo.Trim());

        if (order == null) return null;

        if (order.Status != PlanEstimateOrderStatus.Approved1)
        {
            throw new InvalidOperationException($"Chỉ được duyệt cấp 2 khi đơn đã duyệt cấp 1 (Approved1). Trạng thái hiện tại: {order.Status}");
        }

        order.Status = PlanEstimateOrderStatus.Approved2;
        order.Approve2At = DateTime.Now;
        order.Approve2By = approvedBy ?? "HTC_DIV_DIRECTOR";
        order.LogLUDateTime = DateTime.Now;
        order.LogLUBy = order.Approve2By;

        await _db.SaveChangesAsync();
        return order;
    }

    public async Task<PlanEstimateOrder?> CancelAsync(string pleOrdNo, CancelPlanEstimateOrderDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Reason))
            throw new InvalidOperationException("Cần nhập lý do hủy kế hoạch dự kiến đặt hàng.");

        var order = await _db.PlanEstimateOrders
            .Include(x => x.Details)
            .FirstOrDefaultAsync(x => x.OrgId == _tenant.OrgId && x.PLEOrdNo == pleOrdNo.Trim());

        if (order == null) return null;

        if (order.Status == PlanEstimateOrderStatus.Cancelled)
        {
            throw new InvalidOperationException("Kế hoạch này đã bị hủy trước đó.");
        }

        order.Status = PlanEstimateOrderStatus.Cancelled;
        order.CancelReason = dto.Reason.Trim();
        order.CancelledAt = DateTime.Now;
        order.CancelledBy = dto.CancelledBy ?? "HTC_HQ_USER";
        order.LogLUDateTime = DateTime.Now;
        order.LogLUBy = order.CancelledBy;

        await _db.SaveChangesAsync();
        return order;
    }

    public async Task<bool> DeleteDraftAsync(string pleOrdNo)
    {
        var order = await _db.PlanEstimateOrders
            .Include(x => x.Details)
            .FirstOrDefaultAsync(x => x.OrgId == _tenant.OrgId && x.PLEOrdNo == pleOrdNo.Trim());

        if (order == null) return false;

        if (order.Status != PlanEstimateOrderStatus.Pending)
        {
            throw new InvalidOperationException($"Không thể xóa kế hoạch đã duyệt ({order.Status}). Chỉ xóa được bản nháp Pending.");
        }

        _db.PlanEstimateOrders.Remove(order);
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<PlanEstimateOrderStatsDto> GetStatsAsync()
    {
        var orders = await _db.PlanEstimateOrders
            .Include(x => x.Details)
            .Where(x => x.OrgId == _tenant.OrgId)
            .AsNoTracking()
            .ToListAsync();

        var total = orders.Count;
        var pending = orders.Count(x => x.Status == PlanEstimateOrderStatus.Pending);
        var app1 = orders.Count(x => x.Status == PlanEstimateOrderStatus.Approved1);
        var app2 = orders.Count(x => x.Status == PlanEstimateOrderStatus.Approved2);
        var cancelled = orders.Count(x => x.Status == PlanEstimateOrderStatus.Cancelled);

        var totalDemandCars = orders.Where(x => x.Status != PlanEstimateOrderStatus.Cancelled).Sum(x => x.TotalQtyN1);

        var demandByModel = orders
            .Where(x => x.Status != PlanEstimateOrderStatus.Cancelled)
            .SelectMany(x => x.Details)
            .GroupBy(d => d.ModelCode.ToUpper())
            .ToDictionary(g => g.Key, g => g.Sum(d => d.QtyEOrdN1));

        var demandByDealer = orders
            .Where(x => x.Status != PlanEstimateOrderStatus.Cancelled)
            .GroupBy(x => x.DealerCode)
            .ToDictionary(g => g.Key, g => g.Sum(x => x.TotalQtyN1));

        var demandByArea = orders
            .Where(x => x.Status != PlanEstimateOrderStatus.Cancelled)
            .GroupBy(x => string.IsNullOrWhiteSpace(x.AreaRootCode) ? "UNKNOWN" : x.AreaRootCode)
            .ToDictionary(g => g.Key, g => g.Sum(x => x.TotalQtyN1));

        return new PlanEstimateOrderStatsDto(
            TotalOrders: total,
            PendingCount: pending,
            Approved1Count: app1,
            Approved2Count: app2,
            CancelledCount: cancelled,
            TotalDemandN1Cars: totalDemandCars,
            DemandByModel: demandByModel,
            DemandByDealer: demandByDealer,
            DemandByArea: demandByArea
        );
    }

    private static int GetBplRtlQty(BusinessPlanDetail? dtl, int month)
    {
        if (dtl == null) return 0;
        int m = ((month - 1) % 12) + 1;
        return m switch
        {
            1 => dtl.Rtl_QtyM1,
            2 => dtl.Rtl_QtyM2,
            3 => dtl.Rtl_QtyM3,
            4 => dtl.Rtl_QtyM4,
            5 => dtl.Rtl_QtyM5,
            6 => dtl.Rtl_QtyM6,
            7 => dtl.Rtl_QtyM7,
            8 => dtl.Rtl_QtyM8,
            9 => dtl.Rtl_QtyM9,
            10 => dtl.Rtl_QtyM10,
            11 => dtl.Rtl_QtyM11,
            12 => dtl.Rtl_QtyM12,
            _ => 0
        };
    }

    private static int GetBplOrdQty(BusinessPlanDetail? dtl, int month)
    {
        if (dtl == null) return 0;
        int m = ((month - 1) % 12) + 1;
        return m switch
        {
            1 => dtl.Ord_QtyM1,
            2 => dtl.Ord_QtyM2,
            3 => dtl.Ord_QtyM3,
            4 => dtl.Ord_QtyM4,
            5 => dtl.Ord_QtyM5,
            6 => dtl.Ord_QtyM6,
            7 => dtl.Ord_QtyM7,
            8 => dtl.Ord_QtyM8,
            9 => dtl.Ord_QtyM9,
            10 => dtl.Ord_QtyM10,
            11 => dtl.Ord_QtyM11,
            12 => dtl.Ord_QtyM12,
            _ => 0
        };
    }

    private static string GetDealerName(string dealerCode) => dealerCode.ToUpperInvariant() switch
    {
        "VS058" => "Hyundai Bình Dương",
        "VN001" => "Hyundai Đông Đô",
        "VN002" => "Hyundai Nam Trung",
        "VN012" => "Hyundai Hà Đông",
        "VN065" => "Hyundai Phạm Văn Đồng",
        _ => $"Đại lý {dealerCode}"
    };

    private static string DeriveAreaRootCode(string dealerCode) => dealerCode.ToUpperInvariant() switch
    {
        "VS058" => "MN",
        "VN001" or "VN002" or "VN012" or "VN065" => "MB",
        _ => "MB"
    };
}
