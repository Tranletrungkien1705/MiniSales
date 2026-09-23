using Microsoft.EntityFrameworkCore;
using MiniSales.Data;
using MiniSales.Models;

namespace MiniSales.Services;

public record PaymentGpsCarItemDto(
    string Vin,
    string? CarId = null,
    string ModelCode = "",
    string? ModelName = null,
    string? SpecCode = null,
    string? ColorCode = null,
    string? PlateNo = null,
    string? DealerCode = null,
    string? DealerName = null,
    string GPSDvNo = "",
    DateTime? GPSStartDate = null,
    DateTime? RetailDate = null,
    DateTime? CostGPSStartDate = null,
    DateTime? CostGPSEndDate = null,
    int DeductDate = 0,
    decimal? PriceGPS = null,
    string? Remark = null
);

public record CreatePaymentGPSDto(
    string PmtMonth,
    string? PaymentGPSNo = null,
    DateTime? FromDate = null,
    DateTime? ToDate = null,
    decimal VatRate = 10m,
    string? Remark = null,
    string? CreatedBy = null,
    List<PaymentGpsCarItemDto>? Cars = null
);

public record UpdatePaymentGPSDto(
    string? Remark = null,
    decimal? VatRate = null,
    string? UpdatedBy = null
);

public record AddPaymentGPSCarsDto(
    List<PaymentGpsCarItemDto> Cars,
    string? UpdatedBy = null
);

public record Approve1PaymentGPSDto(
    string? Approved1By = null,
    string? Remark = null
);

public record Approve2PaymentGPSDto(
    string? Approved2By = null,
    string? Remark = null
);

public record TCMSSignPaymentGPSDto(
    string? SignedBy = null,
    string? Remark = null
);

public record RejectPaymentGPSDto(
    string Reason,
    string? RejectedBy = null
);

public record CancelPaymentGPSDto(
    string Reason,
    string? CancelledBy = null
);

public record PaymentGpsPreviewRequestDto(
    string PmtMonth,
    DateTime? FromDate = null,
    DateTime? ToDate = null,
    decimal VatRate = 10m,
    List<PaymentGpsCarItemDto>? Cars = null
);

public record PaymentGpsDetailItemDto(
    long Id,
    string PaymentGPSNo,
    string Vin,
    string? CarId,
    string ModelCode,
    string ModelName,
    string? SpecCode,
    string? ColorCode,
    string? PlateNo,
    string? DealerCode,
    string? DealerName,
    string GPSDvNo,
    DateTime GPSStartDate,
    DateTime? RetailDate,
    DateTime CostGPSStartDate,
    DateTime CostGPSEndDate,
    int PlanCostGPSDate,
    int DeductDate,
    int ActualCostGPSDate,
    decimal PriceGPS,
    decimal AmountGPS,
    string? Remark
);

public record PaymentGpsPreviewResultDto(
    string PmtMonth,
    DateTime FromDate,
    DateTime ToDate,
    int TotalCars,
    decimal AmountTotal,
    decimal VatRate,
    decimal UnitPriceVAT,
    decimal TotalAmountVAT,
    List<PaymentGpsDetailItemDto> Items
);

public record PaymentGpsEligibleCarDto(
    string Vin,
    string? CarId,
    string ModelCode,
    string ModelName,
    string? SpecCode,
    string? ColorCode,
    string? PlateNo,
    string? DealerCode,
    string? DealerName,
    string GPSDvNo,
    DateTime GPSStartDate,
    DateTime? RetailDate,
    DateTime SuggestedCostStartDate,
    DateTime SuggestedCostEndDate,
    int SuggestedPlanDays,
    decimal SuggestedPrice,
    decimal SuggestedAmount
);

public record PaymentGpsSummaryDto(
    long Id,
    string PaymentGPSNo,
    string PmtMonth,
    DateTime FromDate,
    DateTime ToDate,
    int TotalCars,
    decimal AmountTotal,
    decimal VatRate,
    decimal UnitPriceVAT,
    decimal TotalAmountVAT,
    PaymentGPSStatus Status,
    PaymentGPSSignStatus HTVSignStatus,
    PaymentGPSSignStatus TCMSSignStatus,
    DateTime? HTVSignDate,
    string? HTVSignBy,
    DateTime? TCMSSignDate,
    string? TCMSSignBy,
    string? FilePath,
    string? Remark,
    string? CreatedBy,
    DateTime CreatedAt
);

public record PaymentGpsDetailDto(
    long Id,
    string PaymentGPSNo,
    string PmtMonth,
    DateTime FromDate,
    DateTime ToDate,
    int TotalCars,
    decimal AmountTotal,
    decimal VatRate,
    decimal UnitPriceVAT,
    decimal TotalAmountVAT,
    PaymentGPSStatus Status,
    PaymentGPSSignStatus HTVSignStatus,
    DateTime? HTVSignDate,
    string? HTVSignBy,
    PaymentGPSSignStatus TCMSSignStatus,
    DateTime? TCMSSignDate,
    string? TCMSSignBy,
    string? FilePath,
    string? Remark,
    string? RejectReason,
    string? CancelReason,
    DateTime CreatedAt,
    string? CreatedBy,
    DateTime? Approved1At,
    string? Approved1By,
    DateTime? Approved2At,
    string? Approved2By,
    DateTime? RejectedAt,
    string? RejectedBy,
    DateTime? CancelledAt,
    string? CancelledBy,
    DateTime? LUDateTime,
    string? LUBy,
    List<PaymentGpsDetailItemDto> Details
);

public record MonthlyPaymentGpsStatDto(
    string PmtMonth,
    int TotalOrders,
    int TotalCars,
    decimal TotalAmountVat
);

public record PaymentGpsStatsDto(
    int TotalOrders,
    int TotalDraft,
    int TotalPending,
    int TotalApproved1,
    int TotalApproved2,
    int TotalTCMSSigned,
    int TotalRejected,
    int TotalCancelled,
    int TotalCars,
    decimal TotalAmount,
    decimal TotalVat,
    decimal TotalAmountVat,
    List<MonthlyPaymentGpsStatDto> MonthlyStats
);

public record PaymentGpsPrintDto(
    string PaymentGPSNo,
    string PmtMonth,
    string PeriodText,
    string PayerUnit,
    string ReceiverUnit,
    int TotalCars,
    decimal AmountTotal,
    decimal VatRate,
    decimal UnitPriceVAT,
    decimal TotalAmountVAT,
    string StatusText,
    string HTVSignText,
    string TCMSSignText,
    string? HTVSignBy,
    DateTime? HTVSignDate,
    string? TCMSSignBy,
    DateTime? TCMSSignDate,
    List<PaymentGpsDetailItemDto> Cars
);

public interface IPaymentGPSService
{
    Task<string> GetNextSeqAsync();
    Task<IReadOnlyList<GpsUnitPriceMaster>> GetUnitPricesAsync();
    Task<IReadOnlyList<PaymentGpsEligibleCarDto>> GetEligibleCarsAsync(string pmtMonth, string? modelCode = null);
    Task<PaymentGpsPreviewResultDto> PreviewCalculationAsync(PaymentGpsPreviewRequestDto dto);
    Task<PaymentGpsStatsDto> GetStatsAsync(string? pmtMonth = null);
    Task<IReadOnlyList<PaymentGpsSummaryDto>> ListAsync(string? pmtMonth, PaymentGPSStatus? status, string? paymentGPSNo, PaymentGPSSignStatus? htvSignStatus, PaymentGPSSignStatus? tcmsSignStatus, int pageIndex = 0, int pageSize = 50);
    Task<PaymentGpsDetailDto?> DetailAsync(string paymentGPSNo);
    Task<PaymentGpsDetailDto> CreateAsync(CreatePaymentGPSDto dto);
    Task<PaymentGpsDetailDto?> UpdateAsync(string paymentGPSNo, UpdatePaymentGPSDto dto);
    Task<PaymentGpsDetailDto?> AddCarsAsync(string paymentGPSNo, AddPaymentGPSCarsDto dto);
    Task<PaymentGpsDetailDto?> RemoveCarAsync(string paymentGPSNo, string vin);
    Task<PaymentGpsDetailDto?> Approve1Async(string paymentGPSNo, Approve1PaymentGPSDto? dto);
    Task<PaymentGpsDetailDto?> Approve2Async(string paymentGPSNo, Approve2PaymentGPSDto? dto);
    Task<PaymentGpsDetailDto?> TCMSSignAsync(string paymentGPSNo, TCMSSignPaymentGPSDto? dto);
    Task<PaymentGpsDetailDto?> RejectAsync(string paymentGPSNo, RejectPaymentGPSDto dto);
    Task<PaymentGpsDetailDto?> CancelAsync(string paymentGPSNo, CancelPaymentGPSDto dto);
    Task<bool> DeleteDraftAsync(string paymentGPSNo);
    Task<PaymentGpsPrintDto?> GetPrintDataAsync(string paymentGPSNo);
}

public sealed class PaymentGPSService(AppDbContext db, ITenantContext tenant) : IPaymentGPSService
{
    private const decimal DefaultDailyRate = 1500m;

    public async Task<string> GetNextSeqAsync()
    {
        var ym = DateTime.Now.ToString("yyMM");
        var prefix = $"{ym}PG";
        var count = await db.PaymentGPSOrders
            .CountAsync(x => x.OrgId == tenant.OrgId && x.PaymentGPSNo.StartsWith(prefix));
        return $"{prefix}{(count + 1):D4}";
    }

    public async Task<IReadOnlyList<GpsUnitPriceMaster>> GetUnitPricesAsync()
    {
        var prices = await db.GpsUnitPrices.AsNoTracking().ToListAsync();
        if (prices.Count == 0)
        {
            var defaultPrice = new GpsUnitPriceMaster
            {
                PriceCode = "GPS-STD-2026",
                PriceName = "Gói cước giám sát định vị tiêu chuẩn ô tô HTV",
                DailyRate = DefaultDailyRate,
                MonthlyRate = 45000m,
                EffStartDate = new DateTime(2026, 1, 1),
                IsActive = true,
                Remark = "Đơn giá quy định quản lý thiết bị định vị theo ngày 1,500 đ/ngày"
            };
            db.GpsUnitPrices.Add(defaultPrice);
            await db.SaveChangesAsync();
            return [defaultPrice];
        }
        return prices;
    }

    public async Task<IReadOnlyList<PaymentGpsEligibleCarDto>> GetEligibleCarsAsync(string pmtMonth, string? modelCode = null)
    {
        if (string.IsNullOrWhiteSpace(pmtMonth))
            pmtMonth = DateTime.Today.ToString("yyyy-MM");

        var (fromDate, toDate) = ParseMonthPeriod(pmtMonth);

        // Các xe đã nằm trong bảng kê tính phí tháng này (trừ bảng kê đã hủy)
        var existingVinsInMonth = await db.PaymentGPSDetails
            .Where(d => db.PaymentGPSOrders.Any(o => o.Id == d.PaymentGPSId && o.OrgId == tenant.OrgId && o.PmtMonth == pmtMonth && o.Status != PaymentGPSStatus.Cancelled))
            .Select(d => d.Vin)
            .Distinct()
            .ToListAsync();

        var vinExclusions = new HashSet<string>(existingVinsInMonth, StringComparer.OrdinalIgnoreCase);

        // Lấy danh sách xe trong CarVinInventory hoặc Cars có VIN
        var vinQuery = db.CarVinInventories.AsNoTracking()
            .Where(x => x.OrgId == tenant.OrgId);

        if (!string.IsNullOrWhiteSpace(modelCode))
            vinQuery = vinQuery.Where(x => x.ModelCode == modelCode.Trim());

        var inventories = await vinQuery.ToListAsync();

        // Tra cứu đơn giá
        var unitPrice = await GetDailyRateForPeriodAsync(fromDate);

        var list = new List<PaymentGpsEligibleCarDto>();

        foreach (var inv in inventories)
        {
            if (vinExclusions.Contains(inv.Vin))
                continue;

            // Giả lập ngày map GPS (nếu chưa có, lấy CreatedAt hoặc 30 ngày trước)
            var mapDate = inv.CreatedAt.Date;
            if (mapDate > toDate) continue; // Xe nhập sau kỳ tính phí

            // Kiểm tra xe đã giao bán lẻ chưa
            DateTime? retailDate = null;
            var retailDtl = await db.RetailDealDetails
                .AsNoTracking()
                .FirstOrDefaultAsync(d => d.Vin == inv.Vin && d.DeliveryStatus == RetailDealDeliveryStatus.Delivered);
            if (retailDtl?.DeliveryDate != null)
            {
                retailDate = retailDtl.DeliveryDate.Value.Date;
                if (retailDate.Value < fromDate) continue; // Đã bán trước tháng này, không tính phí tháng này nữa
            }

            var costStartDate = mapDate >= fromDate ? mapDate : fromDate;
            var costEndDate = (retailDate.HasValue && retailDate.Value <= toDate) ? retailDate.Value : toDate;
            var planDays = costEndDate >= costStartDate ? (costEndDate - costStartDate).Days + 1 : 0;
            var amount = planDays * unitPrice;

            var gpsDvNo = $"GPS-{inv.Vin[Math.Max(0, inv.Vin.Length - 6)..]}";

            list.Add(new PaymentGpsEligibleCarDto(
                inv.Vin,
                $"CAR-{inv.Vin[Math.Max(0, inv.Vin.Length - 6)..]}",
                inv.ModelCode,
                inv.ModelName,
                inv.SpecCode,
                inv.ColorCode,
                null,
                null,
                null,
                gpsDvNo,
                mapDate,
                retailDate,
                costStartDate,
                costEndDate,
                planDays,
                unitPrice,
                amount
            ));
        }

        // Nếu ít hơn 3 xe, bổ sung từ danh sách xe CarRecord
        if (list.Count < 3)
        {
            var carRecords = await db.Cars.AsNoTracking()
                .Where(c => c.OrgId == tenant.OrgId && !string.IsNullOrEmpty(c.Vin) && c.FlagActive == "1")
                .ToListAsync();

            foreach (var c in carRecords)
            {
                if (list.Any(x => x.Vin.Equals(c.Vin, StringComparison.OrdinalIgnoreCase)) || vinExclusions.Contains(c.Vin))
                    continue;

                var mapDate = fromDate.AddDays(-20);
                var costStartDate = fromDate;
                var costEndDate = toDate;
                var planDays = (costEndDate - costStartDate).Days + 1;
                var amount = planDays * unitPrice;
                var gpsDvNo = $"GPS-{c.Vin[Math.Max(0, c.Vin.Length - 6)..]}";

                list.Add(new PaymentGpsEligibleCarDto(
                    c.Vin,
                    c.CarId,
                    c.ModelCode ?? "HYUNDAI",
                    c.ModelName ?? "Xe Hyundai",
                    c.SpecCode,
                    c.ColorCode,
                    null,
                    c.DealerCode,
                    null,
                    gpsDvNo,
                    mapDate,
                    null,
                    costStartDate,
                    costEndDate,
                    planDays,
                    unitPrice,
                    amount
                ));
            }
        }

        return list;
    }

    public async Task<PaymentGpsPreviewResultDto> PreviewCalculationAsync(PaymentGpsPreviewRequestDto dto)
    {
        var (fromDate, toDate) = ParseMonthPeriod(dto.PmtMonth, dto.FromDate, dto.ToDate);
        var unitPrice = await GetDailyRateForPeriodAsync(fromDate);

        var items = new List<PaymentGpsDetailItemDto>();
        decimal totalAmount = 0m;
        long seq = 1;

        if (dto.Cars != null && dto.Cars.Count > 0)
        {
            foreach (var c in dto.Cars)
            {
                var mapDate = (c.GPSStartDate ?? fromDate).Date;
                var retailDate = c.RetailDate?.Date;

                var costStartDate = (c.CostGPSStartDate ?? (mapDate >= fromDate ? mapDate : fromDate)).Date;
                var costEndDate = (c.CostGPSEndDate ?? ((retailDate.HasValue && retailDate.Value <= toDate) ? retailDate.Value : toDate)).Date;

                var planDays = costEndDate >= costStartDate ? (costEndDate - costStartDate).Days + 1 : 0;
                var deduct = Math.Clamp(c.DeductDate, 0, planDays);
                var actualDays = Math.Max(0, planDays - deduct);
                var rate = c.PriceGPS ?? unitPrice;
                var amount = actualDays * rate;

                totalAmount += amount;

                items.Add(new PaymentGpsDetailItemDto(
                    seq++,
                    "PREVIEW",
                    c.Vin.Trim(),
                    c.CarId,
                    c.ModelCode.Trim(),
                    c.ModelName ?? c.ModelCode,
                    c.SpecCode,
                    c.ColorCode,
                    c.PlateNo,
                    c.DealerCode,
                    c.DealerName,
                    string.IsNullOrWhiteSpace(c.GPSDvNo) ? $"GPS-{c.Vin[Math.Max(0, c.Vin.Length - 6)..]}" : c.GPSDvNo.Trim(),
                    mapDate,
                    retailDate,
                    costStartDate,
                    costEndDate,
                    planDays,
                    deduct,
                    actualDays,
                    rate,
                    amount,
                    c.Remark
                ));
            }
        }

        var vat = Math.Round(totalAmount * dto.VatRate / 100m, 0);
        var totalAmountVat = totalAmount + vat;

        return new PaymentGpsPreviewResultDto(
            dto.PmtMonth,
            fromDate,
            toDate,
            items.Count,
            totalAmount,
            dto.VatRate,
            vat,
            totalAmountVat,
            items
        );
    }

    public async Task<PaymentGpsStatsDto> GetStatsAsync(string? pmtMonth = null)
    {
        var q = db.PaymentGPSOrders.AsNoTracking().Where(x => x.OrgId == tenant.OrgId);
        if (!string.IsNullOrWhiteSpace(pmtMonth))
            q = q.Where(x => x.PmtMonth == pmtMonth.Trim());

        var list = await q.ToListAsync();

        var totalOrders = list.Count;
        var totalDraft = list.Count(x => x.Status == PaymentGPSStatus.Draft);
        var totalPending = list.Count(x => x.Status == PaymentGPSStatus.Pending);
        var totalApproved1 = list.Count(x => x.Status == PaymentGPSStatus.Approved1);
        var totalApproved2 = list.Count(x => x.Status == PaymentGPSStatus.Approved2);
        var totalTCMSSigned = list.Count(x => x.Status == PaymentGPSStatus.TCMSSigned);
        var totalRejected = list.Count(x => x.Status == PaymentGPSStatus.Rejected);
        var totalCancelled = list.Count(x => x.Status == PaymentGPSStatus.Cancelled);

        var activeOrders = list.Where(x => x.Status != PaymentGPSStatus.Cancelled).ToList();
        var totalCars = activeOrders.Sum(x => x.TotalCars);
        var totalAmount = activeOrders.Sum(x => x.AmountTotal);
        var totalVat = activeOrders.Sum(x => x.UnitPriceVAT);
        var totalAmountVat = activeOrders.Sum(x => x.TotalAmountVAT);

        var monthlyGroup = await db.PaymentGPSOrders.AsNoTracking()
            .Where(x => x.OrgId == tenant.OrgId && x.Status != PaymentGPSStatus.Cancelled)
            .GroupBy(x => x.PmtMonth)
            .Select(g => new MonthlyPaymentGpsStatDto(
                g.Key,
                g.Count(),
                g.Sum(x => x.TotalCars),
                g.Sum(x => x.TotalAmountVAT)
            ))
            .OrderByDescending(x => x.PmtMonth)
            .Take(12)
            .ToListAsync();

        return new PaymentGpsStatsDto(
            totalOrders,
            totalDraft,
            totalPending,
            totalApproved1,
            totalApproved2,
            totalTCMSSigned,
            totalRejected,
            totalCancelled,
            totalCars,
            totalAmount,
            totalVat,
            totalAmountVat,
            monthlyGroup
        );
    }

    public async Task<IReadOnlyList<PaymentGpsSummaryDto>> ListAsync(
        string? pmtMonth,
        PaymentGPSStatus? status,
        string? paymentGPSNo,
        PaymentGPSSignStatus? htvSignStatus,
        PaymentGPSSignStatus? tcmsSignStatus,
        int pageIndex = 0,
        int pageSize = 50)
    {
        var q = db.PaymentGPSOrders.AsNoTracking().Where(x => x.OrgId == tenant.OrgId);

        if (!string.IsNullOrWhiteSpace(pmtMonth))
            q = q.Where(x => x.PmtMonth == pmtMonth.Trim());

        if (status.HasValue)
            q = q.Where(x => x.Status == status.Value);

        if (!string.IsNullOrWhiteSpace(paymentGPSNo))
            q = q.Where(x => x.PaymentGPSNo.Contains(paymentGPSNo.Trim()));

        if (htvSignStatus.HasValue)
            q = q.Where(x => x.HTVSignStatus == htvSignStatus.Value);

        if (tcmsSignStatus.HasValue)
            q = q.Where(x => x.TCMSSignStatus == tcmsSignStatus.Value);

        var items = await q.OrderByDescending(x => x.CreatedAt)
            .Skip(pageIndex * pageSize)
            .Take(pageSize)
            .Select(x => new PaymentGpsSummaryDto(
                x.Id,
                x.PaymentGPSNo,
                x.PmtMonth,
                x.FromDate,
                x.ToDate,
                x.TotalCars,
                x.AmountTotal,
                x.VatRate,
                x.UnitPriceVAT,
                x.TotalAmountVAT,
                x.Status,
                x.HTVSignStatus,
                x.TCMSSignStatus,
                x.HTVSignDate,
                x.HTVSignBy,
                x.TCMSSignDate,
                x.TCMSSignBy,
                x.FilePath,
                x.Remark,
                x.CreatedBy,
                x.CreatedAt
            ))
            .ToListAsync();

        return items;
    }

    public async Task<PaymentGpsDetailDto?> DetailAsync(string paymentGPSNo)
    {
        var order = await db.PaymentGPSOrders
            .Include(x => x.Details)
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.OrgId == tenant.OrgId && x.PaymentGPSNo == paymentGPSNo.Trim());

        return order is null ? null : MapToDetailDto(order);
    }

    public async Task<PaymentGpsDetailDto> CreateAsync(CreatePaymentGPSDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.PmtMonth))
            throw new InvalidOperationException("Tháng quyết toán (PmtMonth) không được để trống (định dạng yyyy-MM).");

        var pmtMonth = dto.PmtMonth.Trim();
        var (fromDate, toDate) = ParseMonthPeriod(pmtMonth, dto.FromDate, dto.ToDate);

        var paymentGPSNo = dto.PaymentGPSNo?.Trim();
        if (string.IsNullOrWhiteSpace(paymentGPSNo))
        {
            paymentGPSNo = await GetNextSeqAsync();
        }
        else
        {
            var exists = await db.PaymentGPSOrders.AnyAsync(x => x.OrgId == tenant.OrgId && x.PaymentGPSNo == paymentGPSNo);
            if (exists)
                throw new InvalidOperationException($"Bảng kê thanh toán GPS {paymentGPSNo} đã tồn tại.");
        }

        var unitPrice = await GetDailyRateForPeriodAsync(fromDate);

        var order = new PaymentGPSOrder
        {
            OrgId = tenant.OrgId,
            PaymentGPSNo = paymentGPSNo,
            PmtMonth = pmtMonth,
            FromDate = fromDate,
            ToDate = toDate,
            VatRate = dto.VatRate,
            Status = PaymentGPSStatus.Pending, // Mặc định bảng kê được tạo ở trạng thái Pending sẵn sàng duyệt
            Remark = dto.Remark,
            CreatedBy = dto.CreatedBy ?? "SYSTEM_ADMIN",
            CreatedAt = DateTime.Now
        };

        decimal totalAmount = 0m;

        if (dto.Cars != null && dto.Cars.Count > 0)
        {
            foreach (var c in dto.Cars)
            {
                var mapDate = (c.GPSStartDate ?? fromDate).Date;
                var retailDate = c.RetailDate?.Date;

                var costStartDate = (c.CostGPSStartDate ?? (mapDate >= fromDate ? mapDate : fromDate)).Date;
                var costEndDate = (c.CostGPSEndDate ?? ((retailDate.HasValue && retailDate.Value <= toDate) ? retailDate.Value : toDate)).Date;

                var planDays = costEndDate >= costStartDate ? (costEndDate - costStartDate).Days + 1 : 0;
                var deduct = Math.Clamp(c.DeductDate, 0, planDays);
                var actualDays = Math.Max(0, planDays - deduct);
                var rate = c.PriceGPS ?? unitPrice;
                var amount = actualDays * rate;

                totalAmount += amount;

                order.Details.Add(new PaymentGPSDetail
                {
                    PaymentGPSNo = paymentGPSNo,
                    Vin = c.Vin.Trim(),
                    CarId = c.CarId,
                    ModelCode = c.ModelCode.Trim(),
                    ModelName = c.ModelName ?? c.ModelCode,
                    SpecCode = c.SpecCode,
                    ColorCode = c.ColorCode,
                    PlateNo = c.PlateNo,
                    DealerCode = c.DealerCode,
                    DealerName = c.DealerName,
                    GPSDvNo = string.IsNullOrWhiteSpace(c.GPSDvNo) ? $"GPS-{c.Vin[Math.Max(0, c.Vin.Length - 6)..]}" : c.GPSDvNo.Trim(),
                    GPSStartDate = mapDate,
                    RetailDate = retailDate,
                    CostGPSStartDate = costStartDate,
                    CostGPSEndDate = costEndDate,
                    PlanCostGPSDate = planDays,
                    DeductDate = deduct,
                    ActualCostGPSDate = actualDays,
                    PriceGPS = rate,
                    AmountGPS = amount,
                    Remark = c.Remark
                });
            }
        }

        order.TotalCars = order.Details.Count;
        order.AmountTotal = totalAmount;
        order.UnitPriceVAT = Math.Round(totalAmount * order.VatRate / 100m, 0);
        order.TotalAmountVAT = totalAmount + order.UnitPriceVAT;

        db.PaymentGPSOrders.Add(order);
        await db.SaveChangesAsync();

        return MapToDetailDto(order);
    }

    public async Task<PaymentGpsDetailDto?> UpdateAsync(string paymentGPSNo, UpdatePaymentGPSDto dto)
    {
        var order = await db.PaymentGPSOrders
            .Include(x => x.Details)
            .FirstOrDefaultAsync(x => x.OrgId == tenant.OrgId && x.PaymentGPSNo == paymentGPSNo.Trim());

        if (order is null) return null;

        if (order.Status != PaymentGPSStatus.Draft && order.Status != PaymentGPSStatus.Pending && order.Status != PaymentGPSStatus.Rejected)
            throw new InvalidOperationException($"Chỉ cho phép cập nhật bảng kê GPS ở trạng thái Draft, Pending hoặc Rejected (hiện tại: {order.Status}).");

        if (dto.Remark != null)
            order.Remark = dto.Remark;

        if (dto.VatRate.HasValue && dto.VatRate.Value >= 0)
        {
            order.VatRate = dto.VatRate.Value;
            order.UnitPriceVAT = Math.Round(order.AmountTotal * order.VatRate / 100m, 0);
            order.TotalAmountVAT = order.AmountTotal + order.UnitPriceVAT;
        }

        order.LUDateTime = DateTime.Now;
        order.LUBy = dto.UpdatedBy ?? "SYSTEM";

        await db.SaveChangesAsync();
        return MapToDetailDto(order);
    }

    public async Task<PaymentGpsDetailDto?> AddCarsAsync(string paymentGPSNo, AddPaymentGPSCarsDto dto)
    {
        var order = await db.PaymentGPSOrders
            .Include(x => x.Details)
            .FirstOrDefaultAsync(x => x.OrgId == tenant.OrgId && x.PaymentGPSNo == paymentGPSNo.Trim());

        if (order is null) return null;

        if (order.Status != PaymentGPSStatus.Draft && order.Status != PaymentGPSStatus.Pending)
            throw new InvalidOperationException($"Chỉ có thể thêm xe vào bảng kê ở trạng thái Draft hoặc Pending (hiện tại: {order.Status}).");

        if (dto.Cars == null || dto.Cars.Count == 0)
            throw new InvalidOperationException("Danh sách xe bổ sung không được trống.");

        var unitPrice = await GetDailyRateForPeriodAsync(order.FromDate);

        foreach (var c in dto.Cars)
        {
            if (order.Details.Any(x => x.Vin.Equals(c.Vin.Trim(), StringComparison.OrdinalIgnoreCase)))
                continue; // Bỏ qua trùng VIN

            var mapDate = (c.GPSStartDate ?? order.FromDate).Date;
            var retailDate = c.RetailDate?.Date;

            var costStartDate = (c.CostGPSStartDate ?? (mapDate >= order.FromDate ? mapDate : order.FromDate)).Date;
            var costEndDate = (c.CostGPSEndDate ?? ((retailDate.HasValue && retailDate.Value <= order.ToDate) ? retailDate.Value : order.ToDate)).Date;

            var planDays = costEndDate >= costStartDate ? (costEndDate - costStartDate).Days + 1 : 0;
            var deduct = Math.Clamp(c.DeductDate, 0, planDays);
            var actualDays = Math.Max(0, planDays - deduct);
            var rate = c.PriceGPS ?? unitPrice;
            var amount = actualDays * rate;

            order.Details.Add(new PaymentGPSDetail
            {
                PaymentGPSId = order.Id,
                PaymentGPSNo = order.PaymentGPSNo,
                Vin = c.Vin.Trim(),
                CarId = c.CarId,
                ModelCode = c.ModelCode.Trim(),
                ModelName = c.ModelName ?? c.ModelCode,
                SpecCode = c.SpecCode,
                ColorCode = c.ColorCode,
                PlateNo = c.PlateNo,
                DealerCode = c.DealerCode,
                DealerName = c.DealerName,
                GPSDvNo = string.IsNullOrWhiteSpace(c.GPSDvNo) ? $"GPS-{c.Vin[Math.Max(0, c.Vin.Length - 6)..]}" : c.GPSDvNo.Trim(),
                GPSStartDate = mapDate,
                RetailDate = retailDate,
                CostGPSStartDate = costStartDate,
                CostGPSEndDate = costEndDate,
                PlanCostGPSDate = planDays,
                DeductDate = deduct,
                ActualCostGPSDate = actualDays,
                PriceGPS = rate,
                AmountGPS = amount,
                Remark = c.Remark
            });
        }

        RecalculateTotals(order);
        order.LUDateTime = DateTime.Now;
        order.LUBy = dto.UpdatedBy ?? "SYSTEM";

        await db.SaveChangesAsync();
        return MapToDetailDto(order);
    }

    public async Task<PaymentGpsDetailDto?> RemoveCarAsync(string paymentGPSNo, string vin)
    {
        var order = await db.PaymentGPSOrders
            .Include(x => x.Details)
            .FirstOrDefaultAsync(x => x.OrgId == tenant.OrgId && x.PaymentGPSNo == paymentGPSNo.Trim());

        if (order is null) return null;

        if (order.Status != PaymentGPSStatus.Draft && order.Status != PaymentGPSStatus.Pending)
            throw new InvalidOperationException($"Chỉ có thể gỡ xe khỏi bảng kê ở trạng thái Draft hoặc Pending (hiện tại: {order.Status}).");

        var item = order.Details.FirstOrDefault(x => x.Vin.Equals(vin.Trim(), StringComparison.OrdinalIgnoreCase));
        if (item != null)
        {
            order.Details.Remove(item);
            db.PaymentGPSDetails.Remove(item);
            RecalculateTotals(order);
            order.LUDateTime = DateTime.Now;
            order.LUBy = "SYSTEM";
            await db.SaveChangesAsync();
        }

        return MapToDetailDto(order);
    }

    public async Task<PaymentGpsDetailDto?> Approve1Async(string paymentGPSNo, Approve1PaymentGPSDto? dto)
    {
        var order = await db.PaymentGPSOrders
            .Include(x => x.Details)
            .FirstOrDefaultAsync(x => x.OrgId == tenant.OrgId && x.PaymentGPSNo == paymentGPSNo.Trim());

        if (order is null) return null;

        if (order.Status != PaymentGPSStatus.Pending && order.Status != PaymentGPSStatus.Draft)
            throw new InvalidOperationException($"Bảng kê {paymentGPSNo} đang ở trạng thái {order.Status}, không thể thực hiện phê duyệt cấp 1 (yêu cầu Draft hoặc Pending).");

        order.Status = PaymentGPSStatus.Approved1;
        order.Approved1At = DateTime.Now;
        order.Approved1By = dto?.Approved1By ?? "HTV_SALES_MANAGER";
        if (!string.IsNullOrWhiteSpace(dto?.Remark))
            order.Remark = (order.Remark != null ? order.Remark + " | " : "") + dto.Remark.Trim();

        order.LUDateTime = DateTime.Now;
        order.LUBy = order.Approved1By;

        await db.SaveChangesAsync();
        return MapToDetailDto(order);
    }

    public async Task<PaymentGpsDetailDto?> Approve2Async(string paymentGPSNo, Approve2PaymentGPSDto? dto)
    {
        var order = await db.PaymentGPSOrders
            .Include(x => x.Details)
            .FirstOrDefaultAsync(x => x.OrgId == tenant.OrgId && x.PaymentGPSNo == paymentGPSNo.Trim());

        if (order is null) return null;

        if (order.Status != PaymentGPSStatus.Approved1)
            throw new InvalidOperationException($"Bảng kê {paymentGPSNo} chưa được Bán hàng HTV duyệt cấp 1 (trạng thái hiện tại: {order.Status}).");

        order.Status = PaymentGPSStatus.Approved2;
        order.Approved2At = DateTime.Now;
        order.Approved2By = dto?.Approved2By ?? "HTV_CHIEF_ACCOUNTANT";

        // Tự động ký số phía HTV và sinh file báo cáo PDF
        order.HTVSignStatus = PaymentGPSSignStatus.DaKy;
        order.HTVSignDate = DateTime.Now;
        order.HTVSignBy = order.Approved2By;
        order.FilePath = $"/reports/payment-gps/{order.PaymentGPSNo}_HTVSigned.pdf";

        if (!string.IsNullOrWhiteSpace(dto?.Remark))
            order.Remark = (order.Remark != null ? order.Remark + " | " : "") + dto.Remark.Trim();

        order.LUDateTime = DateTime.Now;
        order.LUBy = order.Approved2By;

        await db.SaveChangesAsync();
        return MapToDetailDto(order);
    }

    public async Task<PaymentGpsDetailDto?> TCMSSignAsync(string paymentGPSNo, TCMSSignPaymentGPSDto? dto)
    {
        var order = await db.PaymentGPSOrders
            .Include(x => x.Details)
            .FirstOrDefaultAsync(x => x.OrgId == tenant.OrgId && x.PaymentGPSNo == paymentGPSNo.Trim());

        if (order is null) return null;

        if (order.Status != PaymentGPSStatus.Approved2)
            throw new InvalidOperationException($"Bảng kê {paymentGPSNo} phải được HTV duyệt cấp 2 trước khi đối tác TCMS ký số (trạng thái hiện tại: {order.Status}).");

        order.Status = PaymentGPSStatus.TCMSSigned;
        order.TCMSSignStatus = PaymentGPSSignStatus.DaKy;
        order.TCMSSignDate = DateTime.Now;
        order.TCMSSignBy = dto?.SignedBy ?? "TCMS_TELEMATICS_DIRECTOR";
        order.FilePath = $"/reports/payment-gps/{order.PaymentGPSNo}_FullySigned.pdf";

        if (!string.IsNullOrWhiteSpace(dto?.Remark))
            order.Remark = (order.Remark != null ? order.Remark + " | " : "") + dto.Remark.Trim();

        order.LUDateTime = DateTime.Now;
        order.LUBy = order.TCMSSignBy;

        await db.SaveChangesAsync();
        return MapToDetailDto(order);
    }

    public async Task<PaymentGpsDetailDto?> RejectAsync(string paymentGPSNo, RejectPaymentGPSDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Reason))
            throw new InvalidOperationException("Vui lòng cung cấp lý do từ chối duyệt bảng kê.");

        var order = await db.PaymentGPSOrders
            .Include(x => x.Details)
            .FirstOrDefaultAsync(x => x.OrgId == tenant.OrgId && x.PaymentGPSNo == paymentGPSNo.Trim());

        if (order is null) return null;

        if (order.Status == PaymentGPSStatus.TCMSSigned || order.Status == PaymentGPSStatus.Cancelled)
            throw new InvalidOperationException($"Không thể từ chối bảng kê ở trạng thái {order.Status}.");

        order.Status = PaymentGPSStatus.Rejected;
        order.RejectReason = dto.Reason.Trim();
        order.RejectedAt = DateTime.Now;
        order.RejectedBy = dto.RejectedBy ?? "HTV_REVIEWER";
        order.LUDateTime = DateTime.Now;
        order.LUBy = order.RejectedBy;

        await db.SaveChangesAsync();
        return MapToDetailDto(order);
    }

    public async Task<PaymentGpsDetailDto?> CancelAsync(string paymentGPSNo, CancelPaymentGPSDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Reason))
            throw new InvalidOperationException("Vui lòng cung cấp lý do hủy bảng kê.");

        var order = await db.PaymentGPSOrders
            .Include(x => x.Details)
            .FirstOrDefaultAsync(x => x.OrgId == tenant.OrgId && x.PaymentGPSNo == paymentGPSNo.Trim());

        if (order is null) return null;

        if (order.Status == PaymentGPSStatus.TCMSSigned)
            throw new InvalidOperationException("Không thể hủy bảng kê đã hoàn tất ký số quyết toán 2 bên (TCMSSigned).");

        order.Status = PaymentGPSStatus.Cancelled;
        order.CancelReason = dto.Reason.Trim();
        order.CancelledAt = DateTime.Now;
        order.CancelledBy = dto.CancelledBy ?? "SYSTEM";
        order.LUDateTime = DateTime.Now;
        order.LUBy = order.CancelledBy;

        await db.SaveChangesAsync();
        return MapToDetailDto(order);
    }

    public async Task<bool> DeleteDraftAsync(string paymentGPSNo)
    {
        var order = await db.PaymentGPSOrders
            .Include(x => x.Details)
            .FirstOrDefaultAsync(x => x.OrgId == tenant.OrgId && x.PaymentGPSNo == paymentGPSNo.Trim());

        if (order is null) return false;

        if (order.Status != PaymentGPSStatus.Draft && order.Status != PaymentGPSStatus.Rejected)
            throw new InvalidOperationException($"Chỉ có thể xóa bảng kê ở trạng thái Draft hoặc Rejected (trạng thái hiện tại: {order.Status}).");

        db.PaymentGPSDetails.RemoveRange(order.Details);
        db.PaymentGPSOrders.Remove(order);
        await db.SaveChangesAsync();
        return true;
    }

    public async Task<PaymentGpsPrintDto?> GetPrintDataAsync(string paymentGPSNo)
    {
        var order = await db.PaymentGPSOrders
            .Include(x => x.Details)
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.OrgId == tenant.OrgId && x.PaymentGPSNo == paymentGPSNo.Trim());

        if (order is null) return null;

        var periodText = $"Từ ngày {order.FromDate:dd/MM/yyyy} đến ngày {order.ToDate:dd/MM/yyyy} (Kỳ {order.PmtMonth})";
        var payer = "CÔNG TY CỔ PHẦN LIÊN DOANH Ô TÔ HYUNDAI THÀNH CÔNG VIỆT NAM (HTV)";
        var receiver = "CÔNG TY CỔ PHẦN DỊCH VỤ VẬN TẢI VÀ THIẾT BỊ THÔNG MINH TCMS";

        var statusText = order.Status switch
        {
            PaymentGPSStatus.Draft => "Bản nháp",
            PaymentGPSStatus.Pending => "Chờ duyệt",
            PaymentGPSStatus.Approved1 => "Bán hàng HTV đã duyệt",
            PaymentGPSStatus.Approved2 => "TCKT HTV đã duyệt & ký số",
            PaymentGPSStatus.TCMSSigned => "Hoàn tất quyết toán 2 bên",
            PaymentGPSStatus.Rejected => "Từ chối duyệt",
            PaymentGPSStatus.Cancelled => "Đã hủy",
            _ => order.Status.ToString()
        };

        var htvSign = order.HTVSignStatus == PaymentGPSSignStatus.DaKy ? "ĐÃ KÝ SỐ ĐIỆN TỬ (CA)" : "CHƯA KÝ";
        var tcmsSign = order.TCMSSignStatus == PaymentGPSSignStatus.DaKy ? "ĐÃ KÝ SỐ ĐIỆN TỬ (CA)" : "CHƯA KÝ";

        var carList = order.Details.Select(d => new PaymentGpsDetailItemDto(
            d.Id,
            d.PaymentGPSNo,
            d.Vin,
            d.CarId,
            d.ModelCode,
            d.ModelName,
            d.SpecCode,
            d.ColorCode,
            d.PlateNo,
            d.DealerCode,
            d.DealerName,
            d.GPSDvNo,
            d.GPSStartDate,
            d.RetailDate,
            d.CostGPSStartDate,
            d.CostGPSEndDate,
            d.PlanCostGPSDate,
            d.DeductDate,
            d.ActualCostGPSDate,
            d.PriceGPS,
            d.AmountGPS,
            d.Remark
        )).ToList();

        return new PaymentGpsPrintDto(
            order.PaymentGPSNo,
            order.PmtMonth,
            periodText,
            payer,
            receiver,
            order.TotalCars,
            order.AmountTotal,
            order.VatRate,
            order.UnitPriceVAT,
            order.TotalAmountVAT,
            statusText,
            htvSign,
            tcmsSign,
            order.HTVSignBy,
            order.HTVSignDate,
            order.TCMSSignBy,
            order.TCMSSignDate,
            carList
        );
    }

    private static (DateTime fromDate, DateTime toDate) ParseMonthPeriod(string pmtMonth, DateTime? explicitFrom = null, DateTime? explicitTo = null)
    {
        int year = DateTime.Today.Year;
        int month = DateTime.Today.Month;

        var parts = pmtMonth.Split('-');
        if (parts.Length == 2 && int.TryParse(parts[0], out var y) && int.TryParse(parts[1], out var m) && m >= 1 && m <= 12)
        {
            year = y;
            month = m;
        }

        var defaultFrom = new DateTime(year, month, 1);
        var defaultTo = new DateTime(year, month, DateTime.DaysInMonth(year, month));

        return (explicitFrom?.Date ?? defaultFrom, explicitTo?.Date ?? defaultTo);
    }

    private async Task<decimal> GetDailyRateForPeriodAsync(DateTime date)
    {
        var price = await db.GpsUnitPrices
            .AsNoTracking()
            .Where(x => x.IsActive && x.EffStartDate <= date && (x.EffEndDate == null || x.EffEndDate >= date))
            .OrderByDescending(x => x.EffStartDate)
            .FirstOrDefaultAsync();

        return price?.DailyRate ?? DefaultDailyRate;
    }

    private static void RecalculateTotals(PaymentGPSOrder order)
    {
        order.TotalCars = order.Details.Count;
        order.AmountTotal = order.Details.Sum(x => x.AmountGPS);
        order.UnitPriceVAT = Math.Round(order.AmountTotal * order.VatRate / 100m, 0);
        order.TotalAmountVAT = order.AmountTotal + order.UnitPriceVAT;
    }

    private static PaymentGpsDetailDto MapToDetailDto(PaymentGPSOrder order)
    {
        var details = order.Details.Select(d => new PaymentGpsDetailItemDto(
            d.Id,
            d.PaymentGPSNo,
            d.Vin,
            d.CarId,
            d.ModelCode,
            d.ModelName,
            d.SpecCode,
            d.ColorCode,
            d.PlateNo,
            d.DealerCode,
            d.DealerName,
            d.GPSDvNo,
            d.GPSStartDate,
            d.RetailDate,
            d.CostGPSStartDate,
            d.CostGPSEndDate,
            d.PlanCostGPSDate,
            d.DeductDate,
            d.ActualCostGPSDate,
            d.PriceGPS,
            d.AmountGPS,
            d.Remark
        )).ToList();

        return new PaymentGpsDetailDto(
            order.Id,
            order.PaymentGPSNo,
            order.PmtMonth,
            order.FromDate,
            order.ToDate,
            order.TotalCars,
            order.AmountTotal,
            order.VatRate,
            order.UnitPriceVAT,
            order.TotalAmountVAT,
            order.Status,
            order.HTVSignStatus,
            order.HTVSignDate,
            order.HTVSignBy,
            order.TCMSSignStatus,
            order.TCMSSignDate,
            order.TCMSSignBy,
            order.FilePath,
            order.Remark,
            order.RejectReason,
            order.CancelReason,
            order.CreatedAt,
            order.CreatedBy,
            order.Approved1At,
            order.Approved1By,
            order.Approved2At,
            order.Approved2By,
            order.RejectedAt,
            order.RejectedBy,
            order.CancelledAt,
            order.CancelledBy,
            order.LUDateTime,
            order.LUBy,
            details
        );
    }
}
