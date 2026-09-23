using Microsoft.EntityFrameworkCore;
using MiniSales.Data;
using MiniSales.Models;

namespace MiniSales.Services;

public record PaymentAvnCarItemDto(
    string Vin,
    string? CarId = null,
    string? EngineNo = null,
    string ModelCode = "",
    string? ModelName = null,
    string? SpecCode = null,
    string? ColorCode = null,
    string? ColorName = null,
    string? AvnCode = null,
    string? SerialNo = null,
    decimal? UnitPriceAVN = null,
    DateTime? AVNDate = null,
    DateTime? InStorageDate = null,
    string? Remark = null
);

public record CreatePaymentAVNDto(
    string PmtMonth,
    string? PaymentAVNNo = null,
    string? SupplierCode = null,
    string? SupplierName = null,
    decimal VatRate = 10m,
    string? Remark = null,
    string? CreatedBy = null,
    List<PaymentAvnCarItemDto>? Cars = null
);

public record UpdatePaymentAVNDto(
    string? SupplierCode = null,
    string? SupplierName = null,
    string? Remark = null,
    decimal? VatRate = null,
    string? UpdatedBy = null
);

public record AddPaymentAVNCarsDto(
    List<PaymentAvnCarItemDto> Cars,
    string? UpdatedBy = null
);

public record Approve1PaymentAVNDto(
    string? Approved1By = null,
    string? Remark = null
);

public record Approve2PaymentAVNDto(
    string? Approved2By = null,
    string? Remark = null
);

public record TCMSSignPaymentAVNDto(
    string? SignedBy = null,
    string? Remark = null
);

public record HTVSignPaymentAVNDto(
    string? SignedBy = null,
    string? Remark = null
);

public record SettlePaymentAVNDto(
    string? BankTxnRef = null,
    string? SettledBy = null,
    string? Remark = null
);

public record RejectPaymentAVNDto(
    string Reason,
    string? RejectedBy = null
);

public record CancelPaymentAVNDto(
    string Reason,
    string? CancelledBy = null
);

public record PaymentAvnPreviewRequestDto(
    string PmtMonth,
    decimal VatRate = 10m,
    List<PaymentAvnCarItemDto>? Cars = null
);

public record PaymentAvnDetailItemDto(
    long Id,
    string PaymentAVNNo,
    string Vin,
    string? CarId,
    string? EngineNo,
    string ModelCode,
    string ModelName,
    string? SpecCode,
    string? ColorCode,
    string? ColorName,
    string AvnCode,
    string SerialNo,
    decimal UnitPriceAVN,
    DateTime? AVNDate,
    DateTime? InStorageDate,
    PaymentAVNDetailStatus Status,
    string? Remark
);

public record PaymentAvnPreviewResultDto(
    string PmtMonth,
    int TotalCars,
    decimal AmountTotal,
    decimal VatRate,
    decimal UnitPriceVAT,
    decimal TotalAmountVAT,
    List<PaymentAvnDetailItemDto> Items
);

public record PaymentAvnEligibleCarDto(
    string Vin,
    string? CarId,
    string? EngineNo,
    string ModelCode,
    string ModelName,
    string? SpecCode,
    string? ColorCode,
    string? ColorName,
    string AvnCode,
    string SerialNo,
    decimal UnitPriceAVN,
    DateTime? AVNDate,
    DateTime? InStorageDate
);

public record PaymentAvnSummaryDto(
    long Id,
    string PaymentAVNNo,
    string PmtMonth,
    string SupplierCode,
    string SupplierName,
    int TotalCars,
    decimal AmountTotal,
    decimal VatRate,
    decimal UnitPriceVAT,
    decimal TotalAmountVAT,
    PaymentAVNStatus Status,
    PaymentAVNSignStatus TCMSSignStatus,
    PaymentAVNSignStatus HTVSignStatus,
    DateTime? TCMSSignDate,
    string? TCMSSignBy,
    DateTime? HTVSignDate,
    string? HTVSignBy,
    string? FilePath,
    string? Remark,
    string? CreatedBy,
    DateTime CreatedAt
);

public record PaymentAvnDetailDto(
    long Id,
    string PaymentAVNNo,
    string PmtMonth,
    string SupplierCode,
    string SupplierName,
    int TotalCars,
    decimal AmountTotal,
    decimal VatRate,
    decimal UnitPriceVAT,
    decimal TotalAmountVAT,
    PaymentAVNStatus Status,
    PaymentAVNSignStatus TCMSSignStatus,
    DateTime? TCMSSignDate,
    string? TCMSSignBy,
    PaymentAVNSignStatus HTVSignStatus,
    DateTime? HTVSignDate,
    string? HTVSignBy,
    string? FilePath,
    string? BankTxnRef,
    string? Remark,
    string? RejectReason,
    string? CancelReason,
    DateTime CreatedAt,
    string? CreatedBy,
    DateTime? Approved1At,
    string? Approved1By,
    DateTime? Approved2At,
    string? Approved2By,
    DateTime? SettledAt,
    string? SettledBy,
    DateTime? RejectedAt,
    string? RejectedBy,
    DateTime? CancelledAt,
    string? CancelledBy,
    DateTime? LUDateTime,
    string? LUBy,
    List<PaymentAvnDetailItemDto> Details
);

public record MonthlyPaymentAvnStatDto(
    string PmtMonth,
    int TotalOrders,
    int TotalCars,
    decimal TotalAmountVat
);

public record PaymentAvnStatsDto(
    int TotalOrders,
    int TotalDraft,
    int TotalTCMSApproved,
    int TotalHTVApproved,
    int TotalSigned,
    int TotalSettled,
    int TotalRejected,
    int TotalCancelled,
    int TotalCars,
    decimal TotalAmount,
    decimal TotalVat,
    decimal TotalAmountVat,
    List<MonthlyPaymentAvnStatDto> MonthlyStats
);

public record PaymentAvnPrintDto(
    string PaymentAVNNo,
    string PmtMonth,
    string PeriodText,
    string PayerUnit,
    string ReceiverUnit,
    string SupplierName,
    int TotalCars,
    decimal AmountTotal,
    decimal VatRate,
    decimal UnitPriceVAT,
    decimal TotalAmountVAT,
    string StatusText,
    string TCMSSignText,
    string HTVSignText,
    string? TCMSSignBy,
    DateTime? TCMSSignDate,
    string? HTVSignBy,
    DateTime? HTVSignDate,
    List<PaymentAvnDetailItemDto> Cars
);

public interface IPaymentAVNService
{
    Task<string> GetNextSeqAsync();
    Task<IReadOnlyList<AvnUnitPriceMaster>> GetUnitPricesAsync();
    Task<IReadOnlyList<PaymentAvnEligibleCarDto>> GetEligibleCarsAsync(string pmtMonth, string? modelCode = null);
    Task<PaymentAvnPreviewResultDto> PreviewCalculationAsync(PaymentAvnPreviewRequestDto dto);
    Task<PaymentAvnStatsDto> GetStatsAsync(string? pmtMonth = null);
    Task<IReadOnlyList<PaymentAvnSummaryDto>> ListAsync(string? pmtMonth, PaymentAVNStatus? status, string? paymentAVNNo, PaymentAVNSignStatus? htvSignStatus, PaymentAVNSignStatus? tcmsSignStatus, int pageIndex = 0, int pageSize = 50);
    Task<PaymentAvnDetailDto?> DetailAsync(string paymentAVNNo);
    Task<PaymentAvnDetailDto> CreateAsync(CreatePaymentAVNDto dto);
    Task<PaymentAvnDetailDto?> UpdateAsync(string paymentAVNNo, UpdatePaymentAVNDto dto);
    Task<PaymentAvnDetailDto?> AddCarsAsync(string paymentAVNNo, AddPaymentAVNCarsDto dto);
    Task<PaymentAvnDetailDto?> RemoveCarAsync(string paymentAVNNo, string vin);
    Task<PaymentAvnDetailDto?> Approve1Async(string paymentAVNNo, Approve1PaymentAVNDto? dto);
    Task<PaymentAvnDetailDto?> Approve2Async(string paymentAVNNo, Approve2PaymentAVNDto? dto);
    Task<PaymentAvnDetailDto?> TCMSSignAsync(string paymentAVNNo, TCMSSignPaymentAVNDto? dto);
    Task<PaymentAvnDetailDto?> HTVSignAsync(string paymentAVNNo, HTVSignPaymentAVNDto? dto);
    Task<PaymentAvnDetailDto?> SettleAsync(string paymentAVNNo, SettlePaymentAVNDto? dto);
    Task<PaymentAvnDetailDto?> RejectAsync(string paymentAVNNo, RejectPaymentAVNDto dto);
    Task<PaymentAvnDetailDto?> CancelAsync(string paymentAVNNo, CancelPaymentAVNDto dto);
    Task<bool> DeleteDraftAsync(string paymentAVNNo);
    Task<PaymentAvnPrintDto?> GetPrintDataAsync(string paymentAVNNo);
}

/// <summary>
/// Dịch vụ Quản lý Bảng kê Quyết toán Chi phí Thiết bị AVN (Audio-Visual Navigation) trên Xe Ô tô HTV - TCMS.
/// Tương ứng khối Pmt_PaymentAVN, Pmt_PaymentAVNDetail, Mst_UnitPriceAVN và các form FrmQuanLyThanhToanAVN,
/// FrmTaoThanhToanAVN, FrmTaoThanhToanAVN_AddCar trong BizHTC.Payment (DMS.Sales).
/// </summary>
public sealed class PaymentAVNService(AppDbContext db, ITenantContext tenant) : IPaymentAVNService
{
    private const decimal DefaultUnitPrice = 9_500_000m;

    // Bảng giá thiết bị AVN tiêu chuẩn theo dòng xe (Mst_UnitPriceAVN / FrmMst_AVNPrice).
    private static readonly Dictionary<string, (string AvnCode, string AvnName, decimal Price)> DefaultAvnPrices = new(StringComparer.OrdinalIgnoreCase)
    {
        ["SANTAFE"] = ("AVN-STF-GEN5-PREM", "AVN SantaFe Gen5 Premium 12.3 inch", 12_500_000m),
        ["SANTAFE-CAL"] = ("AVN-STF-CAL-12.3IN", "AVN SantaFe Calligraphy 12.3 inch", 14_000_000m),
        ["TUCSON"] = ("AVN-TUC-GEN5-1025", "AVN Tucson Gen5 10.25 inch", 11_000_000m),
        ["TUCSON-TURBO"] = ("AVN-TUC-TURBO-1025", "AVN Tucson Turbo 10.25 inch", 11_500_000m),
        ["CRETA"] = ("AVN-CRT-1025-NAV", "AVN Creta 10.25 inch Navi", 9_500_000m),
        ["CRETA-PRE"] = ("AVN-CRT-PREM-1025", "AVN Creta Premium 10.25 inch", 9_800_000m),
        ["ACCENT"] = ("AVN-ACC-GEN5-8IN", "AVN Accent Gen5 8 inch", 8_500_000m),
        ["ACCENT-1.5AT"] = ("AVN-ACC-PREM-8IN", "AVN Accent Premium 8 inch", 8_900_000m),
        ["ELANTRA"] = ("AVN-ELN-1025-WIDESCREEN", "AVN Elantra 10.25 inch Widescreen", 9_800_000m),
        ["ELANTRA-NLINE"] = ("AVN-ELN-NLINE-SPORT", "AVN Elantra N-Line Sport", 10_500_000m),
        ["PALISADE"] = ("AVN-PAL-DUAL-12.3IN", "AVN Palisade Dual 12.3 inch", 16_500_000m),
        ["CUSTIN"] = ("AVN-CST-PORTRAIT-104", "AVN Custin Portrait 10.4 inch", 14_000_000m),
        ["IONIQ-5"] = ("AVN-IQ5-EV-DUAL-12.3", "AVN Ioniq 5 EV Dual 12.3 inch", 18_000_000m)
    };

    public async Task<string> GetNextSeqAsync()
    {
        var ym = DateTime.Now.ToString("yyMM");
        var prefix = $"{ym}AVN";
        var count = await db.PaymentAVNOrders
            .CountAsync(x => x.OrgId == tenant.OrgId && x.PaymentAVNNo.StartsWith(prefix));
        return $"{prefix}{(count + 1):D4}";
    }

    public async Task<IReadOnlyList<AvnUnitPriceMaster>> GetUnitPricesAsync()
    {
        var prices = await db.AvnUnitPrices.AsNoTracking().ToListAsync();
        if (prices.Count == 0)
        {
            var seeded = DefaultAvnPrices.Select(kv => new AvnUnitPriceMaster
            {
                AvnCode = kv.Value.AvnCode,
                AvnName = kv.Value.AvnName,
                ModelCode = kv.Key,
                UnitPrice = kv.Value.Price,
                EffStartDate = new DateTime(2026, 1, 1),
                IsActive = true,
                Remark = "Đơn giá thiết bị AVN quy định theo dòng xe"
            }).ToList();
            db.AvnUnitPrices.AddRange(seeded);
            await db.SaveChangesAsync();
            return seeded;
        }
        return prices;
    }

    public async Task<IReadOnlyList<PaymentAvnEligibleCarDto>> GetEligibleCarsAsync(string pmtMonth, string? modelCode = null)
    {
        if (string.IsNullOrWhiteSpace(pmtMonth))
            pmtMonth = DateTime.Today.ToString("yyyy-MM");

        // Các xe đã nằm trong bảng kê tính phí tháng này (trừ bảng kê đã hủy)
        var existingVinsInMonth = await db.PaymentAVNDetails
            .Where(d => db.PaymentAVNOrders.Any(o => o.Id == d.PaymentAVNId && o.OrgId == tenant.OrgId && o.PmtMonth == pmtMonth && o.Status != PaymentAVNStatus.Cancelled))
            .Select(d => d.Vin)
            .Distinct()
            .ToListAsync();

        var vinExclusions = new HashSet<string>(existingVinsInMonth, StringComparer.OrdinalIgnoreCase);

        var vinQuery = db.CarVinInventories.AsNoTracking().Where(x => x.OrgId == tenant.OrgId);
        if (!string.IsNullOrWhiteSpace(modelCode))
            vinQuery = vinQuery.Where(x => x.ModelCode == modelCode.Trim());

        var inventories = await vinQuery.ToListAsync();

        var list = new List<PaymentAvnEligibleCarDto>();

        foreach (var inv in inventories)
        {
            if (vinExclusions.Contains(inv.Vin))
                continue;

            var (avnCode, _, price) = ResolveAvnPrice(inv.ModelCode);

            list.Add(new PaymentAvnEligibleCarDto(
                inv.Vin,
                $"CAR-{inv.Vin[Math.Max(0, inv.Vin.Length - 6)..]}",
                null,
                inv.ModelCode,
                inv.ModelName,
                inv.SpecCode,
                inv.ColorCode,
                null,
                avnCode,
                $"SN-{inv.Vin[Math.Max(0, inv.Vin.Length - 6)..]}",
                price,
                inv.CreatedAt.Date,
                inv.CreatedAt.Date
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

                var (avnCode, _, price) = ResolveAvnPrice(c.ModelCode);

                list.Add(new PaymentAvnEligibleCarDto(
                    c.Vin,
                    c.CarId,
                    null,
                    c.ModelCode ?? "HYUNDAI",
                    c.ModelName ?? "Xe Hyundai",
                    c.SpecCode,
                    c.ColorCode,
                    null,
                    avnCode,
                    $"SN-{c.Vin[Math.Max(0, c.Vin.Length - 6)..]}",
                    price,
                    DateTime.Today.AddDays(-20),
                    DateTime.Today.AddDays(-20)
                ));
            }
        }

        return list;
    }

    public async Task<PaymentAvnPreviewResultDto> PreviewCalculationAsync(PaymentAvnPreviewRequestDto dto)
    {
        var items = new List<PaymentAvnDetailItemDto>();
        decimal totalAmount = 0m;
        long seq = 1;

        if (dto.Cars != null && dto.Cars.Count > 0)
        {
            foreach (var c in dto.Cars)
            {
                var (avnCode, _, defaultPrice) = ResolveAvnPrice(c.ModelCode);
                var price = c.UnitPriceAVN ?? defaultPrice;
                totalAmount += price;

                items.Add(new PaymentAvnDetailItemDto(
                    seq++,
                    "PREVIEW",
                    c.Vin.Trim(),
                    c.CarId,
                    c.EngineNo,
                    c.ModelCode.Trim(),
                    c.ModelName ?? c.ModelCode,
                    c.SpecCode,
                    c.ColorCode,
                    c.ColorName,
                    string.IsNullOrWhiteSpace(c.AvnCode) ? avnCode : c.AvnCode.Trim(),
                    string.IsNullOrWhiteSpace(c.SerialNo) ? $"SN-{c.Vin[Math.Max(0, c.Vin.Length - 6)..]}" : c.SerialNo.Trim(),
                    price,
                    c.AVNDate?.Date,
                    c.InStorageDate?.Date,
                    PaymentAVNDetailStatus.Pending,
                    c.Remark
                ));
            }
        }

        var vat = Math.Round(totalAmount * dto.VatRate / 100m, 0);
        var totalAmountVat = totalAmount + vat;

        return new PaymentAvnPreviewResultDto(
            dto.PmtMonth,
            items.Count,
            totalAmount,
            dto.VatRate,
            vat,
            totalAmountVat,
            items
        );
    }

    public async Task<PaymentAvnStatsDto> GetStatsAsync(string? pmtMonth = null)
    {
        var q = db.PaymentAVNOrders.AsNoTracking().Where(x => x.OrgId == tenant.OrgId);
        if (!string.IsNullOrWhiteSpace(pmtMonth))
            q = q.Where(x => x.PmtMonth == pmtMonth.Trim());

        var list = await q.ToListAsync();

        var totalOrders = list.Count;
        var totalDraft = list.Count(x => x.Status == PaymentAVNStatus.Draft);
        var totalTCMSApproved = list.Count(x => x.Status == PaymentAVNStatus.TCMSApproved);
        var totalHTVApproved = list.Count(x => x.Status == PaymentAVNStatus.HTVApproved);
        var totalSigned = list.Count(x => x.Status == PaymentAVNStatus.Signed);
        var totalSettled = list.Count(x => x.Status == PaymentAVNStatus.Settled);
        var totalRejected = list.Count(x => x.Status == PaymentAVNStatus.Rejected);
        var totalCancelled = list.Count(x => x.Status == PaymentAVNStatus.Cancelled);

        var activeOrders = list.Where(x => x.Status != PaymentAVNStatus.Cancelled).ToList();
        var totalCars = activeOrders.Sum(x => x.TotalCars);
        var totalAmount = activeOrders.Sum(x => x.AmountTotal);
        var totalVat = activeOrders.Sum(x => x.UnitPriceVAT);
        var totalAmountVat = activeOrders.Sum(x => x.TotalAmountVAT);

        var monthlyGroup = await db.PaymentAVNOrders.AsNoTracking()
            .Where(x => x.OrgId == tenant.OrgId && x.Status != PaymentAVNStatus.Cancelled)
            .GroupBy(x => x.PmtMonth)
            .Select(g => new MonthlyPaymentAvnStatDto(
                g.Key,
                g.Count(),
                g.Sum(x => x.TotalCars),
                g.Sum(x => x.TotalAmountVAT)
            ))
            .OrderByDescending(x => x.PmtMonth)
            .Take(12)
            .ToListAsync();

        return new PaymentAvnStatsDto(
            totalOrders,
            totalDraft,
            totalTCMSApproved,
            totalHTVApproved,
            totalSigned,
            totalSettled,
            totalRejected,
            totalCancelled,
            totalCars,
            totalAmount,
            totalVat,
            totalAmountVat,
            monthlyGroup
        );
    }

    public async Task<IReadOnlyList<PaymentAvnSummaryDto>> ListAsync(
        string? pmtMonth,
        PaymentAVNStatus? status,
        string? paymentAVNNo,
        PaymentAVNSignStatus? htvSignStatus,
        PaymentAVNSignStatus? tcmsSignStatus,
        int pageIndex = 0,
        int pageSize = 50)
    {
        var q = db.PaymentAVNOrders.AsNoTracking().Where(x => x.OrgId == tenant.OrgId);

        if (!string.IsNullOrWhiteSpace(pmtMonth))
            q = q.Where(x => x.PmtMonth == pmtMonth.Trim());

        if (status.HasValue)
            q = q.Where(x => x.Status == status.Value);

        if (!string.IsNullOrWhiteSpace(paymentAVNNo))
            q = q.Where(x => x.PaymentAVNNo.Contains(paymentAVNNo.Trim()));

        if (htvSignStatus.HasValue)
            q = q.Where(x => x.HTVSignStatus == htvSignStatus.Value);

        if (tcmsSignStatus.HasValue)
            q = q.Where(x => x.TCMSSignStatus == tcmsSignStatus.Value);

        var items = await q.OrderByDescending(x => x.CreatedAt)
            .Skip(pageIndex * pageSize)
            .Take(pageSize)
            .Select(x => new PaymentAvnSummaryDto(
                x.Id,
                x.PaymentAVNNo,
                x.PmtMonth,
                x.SupplierCode,
                x.SupplierName,
                x.TotalCars,
                x.AmountTotal,
                x.VatRate,
                x.UnitPriceVAT,
                x.TotalAmountVAT,
                x.Status,
                x.TCMSSignStatus,
                x.HTVSignStatus,
                x.TCMSSignDate,
                x.TCMSSignBy,
                x.HTVSignDate,
                x.HTVSignBy,
                x.FilePath,
                x.Remark,
                x.CreatedBy,
                x.CreatedAt
            ))
            .ToListAsync();

        return items;
    }

    public async Task<PaymentAvnDetailDto?> DetailAsync(string paymentAVNNo)
    {
        var order = await db.PaymentAVNOrders
            .Include(x => x.Details)
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.OrgId == tenant.OrgId && x.PaymentAVNNo == paymentAVNNo.Trim());

        return order is null ? null : MapToDetailDto(order);
    }

    public async Task<PaymentAvnDetailDto> CreateAsync(CreatePaymentAVNDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.PmtMonth))
            throw new InvalidOperationException("Tháng quyết toán (PmtMonth) không được để trống (định dạng yyyy-MM).");

        var pmtMonth = dto.PmtMonth.Trim();

        var paymentAVNNo = dto.PaymentAVNNo?.Trim();
        if (string.IsNullOrWhiteSpace(paymentAVNNo))
        {
            paymentAVNNo = await GetNextSeqAsync();
        }
        else
        {
            var exists = await db.PaymentAVNOrders.AnyAsync(x => x.OrgId == tenant.OrgId && x.PaymentAVNNo == paymentAVNNo);
            if (exists)
                throw new InvalidOperationException($"Bảng kê thanh toán AVN {paymentAVNNo} đã tồn tại.");
        }

        var order = new PaymentAVNOrder
        {
            OrgId = tenant.OrgId,
            PaymentAVNNo = paymentAVNNo,
            PmtMonth = pmtMonth,
            SupplierCode = string.IsNullOrWhiteSpace(dto.SupplierCode) ? "MOBIS-VN" : dto.SupplierCode.Trim(),
            SupplierName = string.IsNullOrWhiteSpace(dto.SupplierName) ? "Công ty TNHH Mobis Auto Parts Việt Nam" : dto.SupplierName.Trim(),
            VatRate = dto.VatRate,
            Status = PaymentAVNStatus.Draft,
            Remark = dto.Remark,
            CreatedBy = dto.CreatedBy ?? "SYSTEM_ADMIN",
            CreatedAt = DateTime.Now
        };

        decimal totalAmount = 0m;

        if (dto.Cars != null && dto.Cars.Count > 0)
        {
            foreach (var c in dto.Cars)
            {
                var (avnCode, _, defaultPrice) = ResolveAvnPrice(c.ModelCode);
                var price = c.UnitPriceAVN ?? defaultPrice;
                totalAmount += price;

                order.Details.Add(new PaymentAVNDetail
                {
                    PaymentAVNNo = paymentAVNNo,
                    Vin = c.Vin.Trim(),
                    CarId = c.CarId,
                    EngineNo = c.EngineNo,
                    ModelCode = c.ModelCode.Trim(),
                    ModelName = c.ModelName ?? c.ModelCode,
                    SpecCode = c.SpecCode,
                    ColorCode = c.ColorCode,
                    ColorName = c.ColorName,
                    AVNCode = string.IsNullOrWhiteSpace(c.AvnCode) ? avnCode : c.AvnCode.Trim(),
                    SerialNo = string.IsNullOrWhiteSpace(c.SerialNo) ? $"SN-{c.Vin[Math.Max(0, c.Vin.Length - 6)..]}" : c.SerialNo.Trim(),
                    UnitPriceAVN = price,
                    AVNDate = c.AVNDate?.Date,
                    InStorageDate = c.InStorageDate?.Date,
                    Status = PaymentAVNDetailStatus.Pending,
                    Remark = c.Remark
                });
            }
        }

        order.TotalCars = order.Details.Count;
        order.AmountTotal = totalAmount;
        order.UnitPriceVAT = Math.Round(totalAmount * order.VatRate / 100m, 0);
        order.TotalAmountVAT = totalAmount + order.UnitPriceVAT;

        db.PaymentAVNOrders.Add(order);
        await db.SaveChangesAsync();

        return MapToDetailDto(order);
    }

    public async Task<PaymentAvnDetailDto?> UpdateAsync(string paymentAVNNo, UpdatePaymentAVNDto dto)
    {
        var order = await db.PaymentAVNOrders
            .Include(x => x.Details)
            .FirstOrDefaultAsync(x => x.OrgId == tenant.OrgId && x.PaymentAVNNo == paymentAVNNo.Trim());

        if (order is null) return null;

        if (order.Status != PaymentAVNStatus.Draft && order.Status != PaymentAVNStatus.Rejected)
            throw new InvalidOperationException($"Chỉ cho phép cập nhật bảng kê AVN ở trạng thái Draft hoặc Rejected (hiện tại: {order.Status}).");

        if (dto.SupplierCode != null) order.SupplierCode = dto.SupplierCode.Trim();
        if (dto.SupplierName != null) order.SupplierName = dto.SupplierName.Trim();
        if (dto.Remark != null) order.Remark = dto.Remark;

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

    public async Task<PaymentAvnDetailDto?> AddCarsAsync(string paymentAVNNo, AddPaymentAVNCarsDto dto)
    {
        var order = await db.PaymentAVNOrders
            .Include(x => x.Details)
            .FirstOrDefaultAsync(x => x.OrgId == tenant.OrgId && x.PaymentAVNNo == paymentAVNNo.Trim());

        if (order is null) return null;

        if (order.Status != PaymentAVNStatus.Draft)
            throw new InvalidOperationException($"Chỉ có thể thêm xe vào bảng kê AVN ở trạng thái Draft (hiện tại: {order.Status}).");

        if (dto.Cars == null || dto.Cars.Count == 0)
            throw new InvalidOperationException("Danh sách xe bổ sung không được trống.");

        foreach (var c in dto.Cars)
        {
            if (order.Details.Any(x => x.Vin.Equals(c.Vin.Trim(), StringComparison.OrdinalIgnoreCase)))
                continue; // Bỏ qua trùng VIN

            var (avnCode, _, defaultPrice) = ResolveAvnPrice(c.ModelCode);
            var price = c.UnitPriceAVN ?? defaultPrice;

            order.Details.Add(new PaymentAVNDetail
            {
                PaymentAVNId = order.Id,
                PaymentAVNNo = order.PaymentAVNNo,
                Vin = c.Vin.Trim(),
                CarId = c.CarId,
                EngineNo = c.EngineNo,
                ModelCode = c.ModelCode.Trim(),
                ModelName = c.ModelName ?? c.ModelCode,
                SpecCode = c.SpecCode,
                ColorCode = c.ColorCode,
                ColorName = c.ColorName,
                AVNCode = string.IsNullOrWhiteSpace(c.AvnCode) ? avnCode : c.AvnCode.Trim(),
                SerialNo = string.IsNullOrWhiteSpace(c.SerialNo) ? $"SN-{c.Vin[Math.Max(0, c.Vin.Length - 6)..]}" : c.SerialNo.Trim(),
                UnitPriceAVN = price,
                AVNDate = c.AVNDate?.Date,
                InStorageDate = c.InStorageDate?.Date,
                Status = PaymentAVNDetailStatus.Pending,
                Remark = c.Remark
            });
        }

        RecalculateTotals(order);
        order.LUDateTime = DateTime.Now;
        order.LUBy = dto.UpdatedBy ?? "SYSTEM";

        await db.SaveChangesAsync();
        return MapToDetailDto(order);
    }

    public async Task<PaymentAvnDetailDto?> RemoveCarAsync(string paymentAVNNo, string vin)
    {
        var order = await db.PaymentAVNOrders
            .Include(x => x.Details)
            .FirstOrDefaultAsync(x => x.OrgId == tenant.OrgId && x.PaymentAVNNo == paymentAVNNo.Trim());

        if (order is null) return null;

        if (order.Status != PaymentAVNStatus.Draft)
            throw new InvalidOperationException($"Chỉ có thể gỡ xe khỏi bảng kê AVN ở trạng thái Draft (hiện tại: {order.Status}).");

        var item = order.Details.FirstOrDefault(x => x.Vin.Equals(vin.Trim(), StringComparison.OrdinalIgnoreCase));
        if (item != null)
        {
            order.Details.Remove(item);
            db.PaymentAVNDetails.Remove(item);
            RecalculateTotals(order);
            order.LUDateTime = DateTime.Now;
            order.LUBy = "SYSTEM";
            await db.SaveChangesAsync();
        }

        return MapToDetailDto(order);
    }

    public async Task<PaymentAvnDetailDto?> Approve1Async(string paymentAVNNo, Approve1PaymentAVNDto? dto)
    {
        var order = await db.PaymentAVNOrders
            .Include(x => x.Details)
            .FirstOrDefaultAsync(x => x.OrgId == tenant.OrgId && x.PaymentAVNNo == paymentAVNNo.Trim());

        if (order is null) return null;

        if (order.Status != PaymentAVNStatus.Draft && order.Status != PaymentAVNStatus.Rejected)
            throw new InvalidOperationException($"Bảng kê {paymentAVNNo} đang ở trạng thái {order.Status}, không thể thực hiện phê duyệt cấp 1 (yêu cầu Draft hoặc Rejected).");

        order.Status = PaymentAVNStatus.TCMSApproved;
        order.Approved1At = DateTime.Now;
        order.Approved1By = dto?.Approved1By ?? "TCMS_TELEMATICS_MANAGER";
        if (!string.IsNullOrWhiteSpace(dto?.Remark))
            order.Remark = (order.Remark != null ? order.Remark + " | " : "") + dto.Remark.Trim();

        order.LUDateTime = DateTime.Now;
        order.LUBy = order.Approved1By;

        await db.SaveChangesAsync();
        return MapToDetailDto(order);
    }

    public async Task<PaymentAvnDetailDto?> Approve2Async(string paymentAVNNo, Approve2PaymentAVNDto? dto)
    {
        var order = await db.PaymentAVNOrders
            .Include(x => x.Details)
            .FirstOrDefaultAsync(x => x.OrgId == tenant.OrgId && x.PaymentAVNNo == paymentAVNNo.Trim());

        if (order is null) return null;

        if (order.Status != PaymentAVNStatus.TCMSApproved)
            throw new InvalidOperationException($"Bảng kê {paymentAVNNo} chưa được TCMS duyệt cấp 1 (trạng thái hiện tại: {order.Status}).");

        order.Status = PaymentAVNStatus.HTVApproved;
        order.Approved2At = DateTime.Now;
        order.Approved2By = dto?.Approved2By ?? "HTV_SALES_MANAGER";

        if (!string.IsNullOrWhiteSpace(dto?.Remark))
            order.Remark = (order.Remark != null ? order.Remark + " | " : "") + dto.Remark.Trim();

        order.LUDateTime = DateTime.Now;
        order.LUBy = order.Approved2By;

        await db.SaveChangesAsync();
        return MapToDetailDto(order);
    }

    public async Task<PaymentAvnDetailDto?> TCMSSignAsync(string paymentAVNNo, TCMSSignPaymentAVNDto? dto)
    {
        var order = await db.PaymentAVNOrders
            .Include(x => x.Details)
            .FirstOrDefaultAsync(x => x.OrgId == tenant.OrgId && x.PaymentAVNNo == paymentAVNNo.Trim());

        if (order is null) return null;

        if (order.Status != PaymentAVNStatus.HTVApproved)
            throw new InvalidOperationException($"Bảng kê {paymentAVNNo} phải được HTV duyệt cấp 2 trước khi TCMS ký số (trạng thái hiện tại: {order.Status}).");

        order.TCMSSignStatus = PaymentAVNSignStatus.DaKy;
        order.TCMSSignDate = DateTime.Now;
        order.TCMSSignBy = dto?.SignedBy ?? "TCMS_TELEMATICS_DIRECTOR";
        order.FilePath = $"/reports/payment-avn/{order.PaymentAVNNo}_TCMSSigned.pdf";

        if (!string.IsNullOrWhiteSpace(dto?.Remark))
            order.Remark = (order.Remark != null ? order.Remark + " | " : "") + dto.Remark.Trim();

        order.LUDateTime = DateTime.Now;
        order.LUBy = order.TCMSSignBy;

        await db.SaveChangesAsync();
        return MapToDetailDto(order);
    }

    public async Task<PaymentAvnDetailDto?> HTVSignAsync(string paymentAVNNo, HTVSignPaymentAVNDto? dto)
    {
        var order = await db.PaymentAVNOrders
            .Include(x => x.Details)
            .FirstOrDefaultAsync(x => x.OrgId == tenant.OrgId && x.PaymentAVNNo == paymentAVNNo.Trim());

        if (order is null) return null;

        if (order.Status != PaymentAVNStatus.HTVApproved)
            throw new InvalidOperationException($"Bảng kê {paymentAVNNo} phải được HTV duyệt cấp 2 trước khi HTV ký số (trạng thái hiện tại: {order.Status}).");

        order.HTVSignStatus = PaymentAVNSignStatus.DaKy;
        order.HTVSignDate = DateTime.Now;
        order.HTVSignBy = dto?.SignedBy ?? "HTV_REP_KIM";

        // Hoàn tất quyết toán khi cả 2 bên đã ký số điện tử
        if (order.TCMSSignStatus == PaymentAVNSignStatus.DaKy)
        {
            order.Status = PaymentAVNStatus.Signed;
            order.FilePath = $"/reports/payment-avn/{order.PaymentAVNNo}_FullySigned.pdf";
        }

        if (!string.IsNullOrWhiteSpace(dto?.Remark))
            order.Remark = (order.Remark != null ? order.Remark + " | " : "") + dto.Remark.Trim();

        order.LUDateTime = DateTime.Now;
        order.LUBy = order.HTVSignBy;

        await db.SaveChangesAsync();
        return MapToDetailDto(order);
    }

    public async Task<PaymentAvnDetailDto?> SettleAsync(string paymentAVNNo, SettlePaymentAVNDto? dto)
    {
        var order = await db.PaymentAVNOrders
            .Include(x => x.Details)
            .FirstOrDefaultAsync(x => x.OrgId == tenant.OrgId && x.PaymentAVNNo == paymentAVNNo.Trim());

        if (order is null) return null;

        if (order.Status != PaymentAVNStatus.Signed)
            throw new InvalidOperationException($"Bảng kê {paymentAVNNo} phải hoàn tất ký số 2 bên (Signed) trước khi quyết toán/giải ngân (trạng thái hiện tại: {order.Status}).");

        order.Status = PaymentAVNStatus.Settled;
        order.SettledAt = DateTime.Now;
        order.SettledBy = dto?.SettledBy ?? "HTV_CHIEF_ACCOUNTANT";
        if (!string.IsNullOrWhiteSpace(dto?.BankTxnRef))
            order.BankTxnRef = dto.BankTxnRef.Trim();

        if (!string.IsNullOrWhiteSpace(dto?.Remark))
            order.Remark = (order.Remark != null ? order.Remark + " | " : "") + dto.Remark.Trim();

        order.LUDateTime = DateTime.Now;
        order.LUBy = order.SettledBy;

        await db.SaveChangesAsync();
        return MapToDetailDto(order);
    }

    public async Task<PaymentAvnDetailDto?> RejectAsync(string paymentAVNNo, RejectPaymentAVNDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Reason))
            throw new InvalidOperationException("Vui lòng cung cấp lý do từ chối duyệt bảng kê.");

        var order = await db.PaymentAVNOrders
            .Include(x => x.Details)
            .FirstOrDefaultAsync(x => x.OrgId == tenant.OrgId && x.PaymentAVNNo == paymentAVNNo.Trim());

        if (order is null) return null;

        if (order.Status == PaymentAVNStatus.Signed || order.Status == PaymentAVNStatus.Settled || order.Status == PaymentAVNStatus.Cancelled)
            throw new InvalidOperationException($"Không thể từ chối bảng kê ở trạng thái {order.Status}.");

        order.Status = PaymentAVNStatus.Rejected;
        order.RejectReason = dto.Reason.Trim();
        order.RejectedAt = DateTime.Now;
        order.RejectedBy = dto.RejectedBy ?? "HTV_REVIEWER";
        order.LUDateTime = DateTime.Now;
        order.LUBy = order.RejectedBy;

        await db.SaveChangesAsync();
        return MapToDetailDto(order);
    }

    public async Task<PaymentAvnDetailDto?> CancelAsync(string paymentAVNNo, CancelPaymentAVNDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Reason))
            throw new InvalidOperationException("Vui lòng cung cấp lý do hủy bảng kê.");

        var order = await db.PaymentAVNOrders
            .Include(x => x.Details)
            .FirstOrDefaultAsync(x => x.OrgId == tenant.OrgId && x.PaymentAVNNo == paymentAVNNo.Trim());

        if (order is null) return null;

        if (order.Status == PaymentAVNStatus.Signed || order.Status == PaymentAVNStatus.Settled)
            throw new InvalidOperationException("Không thể hủy bảng kê đã hoàn tất ký số quyết toán 2 bên hoặc đã quyết toán.");

        order.Status = PaymentAVNStatus.Cancelled;
        order.CancelReason = dto.Reason.Trim();
        order.CancelledAt = DateTime.Now;
        order.CancelledBy = dto.CancelledBy ?? "SYSTEM";
        order.LUDateTime = DateTime.Now;
        order.LUBy = order.CancelledBy;

        await db.SaveChangesAsync();
        return MapToDetailDto(order);
    }

    public async Task<bool> DeleteDraftAsync(string paymentAVNNo)
    {
        var order = await db.PaymentAVNOrders
            .Include(x => x.Details)
            .FirstOrDefaultAsync(x => x.OrgId == tenant.OrgId && x.PaymentAVNNo == paymentAVNNo.Trim());

        if (order is null) return false;

        if (order.Status != PaymentAVNStatus.Draft && order.Status != PaymentAVNStatus.Rejected)
            throw new InvalidOperationException($"Chỉ có thể xóa bảng kê ở trạng thái Draft hoặc Rejected (trạng thái hiện tại: {order.Status}).");

        db.PaymentAVNDetails.RemoveRange(order.Details);
        db.PaymentAVNOrders.Remove(order);
        await db.SaveChangesAsync();
        return true;
    }

    public async Task<PaymentAvnPrintDto?> GetPrintDataAsync(string paymentAVNNo)
    {
        var order = await db.PaymentAVNOrders
            .Include(x => x.Details)
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.OrgId == tenant.OrgId && x.PaymentAVNNo == paymentAVNNo.Trim());

        if (order is null) return null;

        var periodText = $"Kỳ thanh toán tháng {order.PmtMonth}";
        var payer = "CÔNG TY CỔ PHẦN LIÊN DOANH Ô TÔ HYUNDAI THÀNH CÔNG VIỆT NAM (HTV)";
        var receiver = "CÔNG TY CỔ PHẦN DỊCH VỤ VẬN TẢI VÀ THIẾT BỊ THÔNG MINH TCMS";

        var statusText = order.Status switch
        {
            PaymentAVNStatus.Draft => "Bản nháp",
            PaymentAVNStatus.TCMSApproved => "TCMS đã duyệt cấp 1",
            PaymentAVNStatus.HTVApproved => "HTV đã duyệt cấp 2",
            PaymentAVNStatus.Signed => "Hoàn tất quyết toán 2 bên",
            PaymentAVNStatus.Settled => "Đã quyết toán / giải ngân",
            PaymentAVNStatus.Rejected => "Từ chối duyệt",
            PaymentAVNStatus.Cancelled => "Đã hủy",
            _ => order.Status.ToString()
        };

        var tcmsSign = order.TCMSSignStatus == PaymentAVNSignStatus.DaKy ? "ĐÃ KÝ SỐ ĐIỆN TỬ (CA)" : "CHƯA KÝ";
        var htvSign = order.HTVSignStatus == PaymentAVNSignStatus.DaKy ? "ĐÃ KÝ SỐ ĐIỆN TỬ (CA)" : "CHƯA KÝ";

        var carList = order.Details.Select(MapDetailItem).ToList();

        return new PaymentAvnPrintDto(
            order.PaymentAVNNo,
            order.PmtMonth,
            periodText,
            payer,
            receiver,
            order.SupplierName,
            order.TotalCars,
            order.AmountTotal,
            order.VatRate,
            order.UnitPriceVAT,
            order.TotalAmountVAT,
            statusText,
            tcmsSign,
            htvSign,
            order.TCMSSignBy,
            order.TCMSSignDate,
            order.HTVSignBy,
            order.HTVSignDate,
            carList
        );
    }

    private static (string avnCode, string avnName, decimal price) ResolveAvnPrice(string? modelCode)
    {
        if (!string.IsNullOrWhiteSpace(modelCode) && DefaultAvnPrices.TryGetValue(modelCode.Trim(), out var p))
            return p;
        return ("AVN-GEN-1025-NAV", "AVN tiêu chuẩn 10.25 inch Navi", DefaultUnitPrice);
    }

    private static void RecalculateTotals(PaymentAVNOrder order)
    {
        order.TotalCars = order.Details.Count;
        order.AmountTotal = order.Details.Sum(x => x.UnitPriceAVN);
        order.UnitPriceVAT = Math.Round(order.AmountTotal * order.VatRate / 100m, 0);
        order.TotalAmountVAT = order.AmountTotal + order.UnitPriceVAT;
    }

    private static PaymentAvnDetailItemDto MapDetailItem(PaymentAVNDetail d) => new(
        d.Id,
        d.PaymentAVNNo,
        d.Vin,
        d.CarId,
        d.EngineNo,
        d.ModelCode,
        d.ModelName,
        d.SpecCode,
        d.ColorCode,
        d.ColorName,
        d.AVNCode,
        d.SerialNo,
        d.UnitPriceAVN,
        d.AVNDate,
        d.InStorageDate,
        d.Status,
        d.Remark
    );

    private static PaymentAvnDetailDto MapToDetailDto(PaymentAVNOrder order)
    {
        var details = order.Details.Select(MapDetailItem).ToList();

        return new PaymentAvnDetailDto(
            order.Id,
            order.PaymentAVNNo,
            order.PmtMonth,
            order.SupplierCode,
            order.SupplierName,
            order.TotalCars,
            order.AmountTotal,
            order.VatRate,
            order.UnitPriceVAT,
            order.TotalAmountVAT,
            order.Status,
            order.TCMSSignStatus,
            order.TCMSSignDate,
            order.TCMSSignBy,
            order.HTVSignStatus,
            order.HTVSignDate,
            order.HTVSignBy,
            order.FilePath,
            order.BankTxnRef,
            order.Remark,
            order.RejectReason,
            order.CancelReason,
            order.CreatedAt,
            order.CreatedBy,
            order.Approved1At,
            order.Approved1By,
            order.Approved2At,
            order.Approved2By,
            order.SettledAt,
            order.SettledBy,
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
