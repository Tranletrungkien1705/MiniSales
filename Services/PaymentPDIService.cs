using Microsoft.EntityFrameworkCore;
using MiniSales.Data;
using MiniSales.Models;

namespace MiniSales.Services;

public record PaymentPdiCarItemDto(
    string Vin,
    string? CarId = null,
    string ModelCode = "",
    string? ModelName = null,
    string? SpecCode = null,
    string? ColorCode = null,
    string? PdiType = null,          // "N" = CKD nội địa, "X" = CBU nhập khẩu
    string? RefNoInit = null,
    string? StorageCodeInit = null,
    DateTime? StorageDateInit = null,
    string? InvoiceFactoryDate = null,
    string? RefNoCost = null,
    string? StorageCodeCost = null,
    string? RefNoOut = null,
    string? StorageCodeOut = null,
    DateTime? StorageDateOut = null,
    string? DeliveryOrderNo = null,
    string? DealerCode = null,
    string? DealerName = null,
    decimal? PdinAmount = null,
    decimal? PdixAmount = null,
    decimal? RxAmount = null,
    string? Remark = null
);

public record CreatePaymentPDIDto(
    string PmtMonth,
    DateTime PmtPeriodStartDTime,
    DateTime PmtPeriodEndDTime,
    string? PmtPDINo = null,
    decimal Vat = 10m,
    string? Remark = null,
    string? CreatedBy = null,
    List<PaymentPdiCarItemDto>? Cars = null
);

public record UpdatePaymentPDIDto(
    string? Remark = null,
    decimal? Vat = null,
    string? UpdatedBy = null
);

public record AddPaymentPDICarsDto(
    List<PaymentPdiCarItemDto> Cars,
    string? UpdatedBy = null
);

public record Approve1PaymentPDIDto(
    string? Approved1By = null,
    string? Remark = null
);

public record Approve2PaymentPDIDto(
    string? Approved2By = null,
    string? Remark = null
);

public record SignPaymentPDIDto(
    string? SignedBy = null,
    string? PdiFileName = null,
    string? RxFileName = null,
    string? Remark = null
);

public record UploadInvoicePaymentPDIDto(
    string InvoiceType,              // "PDI" | "RX"
    string InvoiceNo,
    DateTime? InvoiceDate = null,
    string? FileName = null,
    string? UploadedBy = null
);

public record RejectPaymentPDIDto(
    string Reason,
    string? RejectedBy = null
);

public record CancelPaymentPDIDto(
    string Reason,
    string? CancelledBy = null
);

public record PaymentPdiDetailItemDto(
    long Id,
    string PmtPDINo,
    string Vin,
    string? CarId,
    string ModelCode,
    string ModelName,
    string? SpecCode,
    string? ColorCode,
    string PdiType,
    string? RefNoInit,
    string? StorageCodeInit,
    DateTime? StorageDateInit,
    string? InvoiceFactoryDate,
    string? RefNoCost,
    string? StorageCodeCost,
    string? RefNoOut,
    string? StorageCodeOut,
    DateTime? StorageDateOut,
    string? DeliveryOrderNo,
    string? DealerCode,
    string? DealerName,
    decimal PdinAmount,
    decimal PdixAmount,
    decimal PdiAmount,
    decimal RxAmount,
    decimal TotalAmount,
    string? Remark
);

public record PaymentPdiEligibleCarDto(
    string Vin,
    string? CarId,
    string ModelCode,
    string ModelName,
    string? SpecCode,
    string? ColorCode,
    string PdiType,
    string? StorageCodeInit,
    DateTime? StorageDateInit,
    string? InvoiceFactoryDate,
    decimal PdinAmount,
    decimal PdixAmount,
    decimal RxAmount
);

public record PaymentPdiSummaryDto(
    long Id,
    string PmtPDINo,
    string PmtMonth,
    DateTime PmtPeriodStartDTime,
    DateTime PmtPeriodEndDTime,
    int TotalCars,
    decimal PdinTotalAmount,
    decimal PdixTotalAmount,
    decimal PdiTotalAmount,
    decimal RxTotalAmount,
    decimal TotalAmount,
    decimal Vat,
    decimal AmountVat,
    decimal TotalAmountAfterVat,
    PaymentPDIStatus Status,
    PaymentPDISignStatus HtvSignStatus,
    PaymentPDISignStatus TcmsSignStatus,
    string? Remark,
    string? CreatedBy,
    DateTime CreatedAt
);

public record PaymentPdiDetailDto(
    long Id,
    string PmtPDINo,
    string PmtMonth,
    DateTime PmtPeriodStartDTime,
    DateTime PmtPeriodEndDTime,
    int TotalCars,
    decimal PdinTotalAmount,
    decimal PdixTotalAmount,
    decimal PdiTotalAmount,
    decimal RxTotalAmount,
    decimal TotalAmount,
    decimal Vat,
    decimal AmountVat,
    decimal TotalAmountAfterVat,
    PaymentPDIStatus Status,
    PaymentPDISignStatus HtvSignStatus,
    DateTime? HtvSignDTime,
    string? HtvSignBy,
    PaymentPDISignStatus TcmsSignStatus,
    DateTime? TcmsSignDTime,
    string? TcmsSignBy,
    string? PdiFilePath,
    string? RxFilePath,
    string? PdiInvoiceNo,
    DateTime? PdiInvoiceDate,
    string? RxInvoiceNo,
    DateTime? RxInvoiceDate,
    string? Remark,
    string? RejectReason,
    string? CancelReason,
    DateTime CreatedAt,
    string? CreatedBy,
    DateTime? Approve1At,
    string? Approve1By,
    DateTime? Approve2At,
    string? Approve2By,
    DateTime? RejectedAt,
    string? RejectedBy,
    DateTime? CancelledAt,
    string? CancelledBy,
    DateTime LogLUDateTime,
    string? LogLUBy,
    List<PaymentPdiDetailItemDto> Details
);

public record MonthlyPaymentPdiStatDto(
    string PmtMonth,
    int TotalOrders,
    int TotalCars,
    decimal TotalAmountAfterVat
);

public record PaymentPdiStatsDto(
    int TotalOrders,
    int TotalPending,
    int TotalApproved1,
    int TotalApproved2,
    int TotalFinished,
    int TotalRejected,
    int TotalCancelled,
    int TotalCars,
    decimal TotalPdin,
    decimal TotalPdix,
    decimal TotalRx,
    decimal TotalAmount,
    decimal TotalVat,
    decimal TotalAmountAfterVat,
    List<MonthlyPaymentPdiStatDto> MonthlyStats
);

public record PaymentPdiPrintDto(
    string PmtPDINo,
    string PmtMonth,
    string PeriodText,
    string PayerUnit,
    string ReceiverUnit,
    int TotalCars,
    decimal PdinTotalAmount,
    decimal PdixTotalAmount,
    decimal PdiTotalAmount,
    decimal RxTotalAmount,
    decimal TotalAmount,
    decimal Vat,
    decimal AmountVat,
    decimal TotalAmountAfterVat,
    string StatusText,
    string HtvSignText,
    string TcmsSignText,
    string? HtvSignBy,
    DateTime? HtvSignDTime,
    string? TcmsSignBy,
    DateTime? TcmsSignDTime,
    List<PaymentPdiDetailItemDto> Cars
);

public interface IPaymentPDIService
{
    Task<string> GetNextSeqAsync();
    Task<IReadOnlyList<PaymentPdiEligibleCarDto>> GetEligibleCarsAsync(DateTime periodStart, DateTime periodEnd);
    Task<PaymentPdiStatsDto> GetStatsAsync(string? pmtMonth = null);
    Task<IReadOnlyList<PaymentPdiSummaryDto>> ListAsync(string? pmtMonth, PaymentPDIStatus? status, string? pmtPDINo, PaymentPDISignStatus? htvSignStatus, PaymentPDISignStatus? tcmsSignStatus, int pageIndex = 0, int pageSize = 50);
    Task<PaymentPdiDetailDto?> DetailAsync(string pmtPDINo);
    Task<PaymentPdiDetailDto> CreateAsync(CreatePaymentPDIDto dto);
    Task<PaymentPdiDetailDto?> UpdateAsync(string pmtPDINo, UpdatePaymentPDIDto dto);
    Task<PaymentPdiDetailDto?> AddCarsAsync(string pmtPDINo, AddPaymentPDICarsDto dto);
    Task<PaymentPdiDetailDto?> RemoveCarAsync(string pmtPDINo, string vin);
    Task<PaymentPdiDetailDto?> Approve1Async(string pmtPDINo, Approve1PaymentPDIDto? dto);
    Task<PaymentPdiDetailDto?> Approve2Async(string pmtPDINo, Approve2PaymentPDIDto? dto);
    Task<PaymentPdiDetailDto?> TCMSSignAsync(string pmtPDINo, SignPaymentPDIDto? dto);
    Task<PaymentPdiDetailDto?> HTVSignAsync(string pmtPDINo, SignPaymentPDIDto? dto);
    Task<PaymentPdiDetailDto?> UploadInvoiceAsync(string pmtPDINo, UploadInvoicePaymentPDIDto dto);
    Task<PaymentPdiDetailDto?> RejectAsync(string pmtPDINo, RejectPaymentPDIDto dto);
    Task<PaymentPdiDetailDto?> CancelAsync(string pmtPDINo, CancelPaymentPDIDto dto);
    Task<bool> DeleteDraftAsync(string pmtPDINo);
    Task<PaymentPdiPrintDto?> GetPrintDataAsync(string pmtPDINo);
}

/// <summary>
/// Dịch vụ Quản lý Bảng kê Quyết toán Chi phí PDI (Pre-Delivery Inspection) & Rửa xe Xe Ô tô HTV - TCMS.
/// Tương ứng khối Pmt_PaymentPDI, Pmt_PaymentPDIDetail và các form FrmQuanLyThanhToanPDI, FrmSuaThanhToanPDI
/// trong BizHTC.Payment (DMS.Sales / PaymentPDI.cs / PmtPaymentPDI.txt / ChiPhiPDI.HTV.TCMS.xlsx).
/// </summary>
public sealed class PaymentPDIService(AppDbContext db, ITenantContext tenant) : IPaymentPDIService
{
    // Đơn giá định mức chi phí PDI & rửa xe (Mst_UnitPricePDI / ChiPhiPDI.HTV.TCMS.xlsx).
    private const decimal DefaultPdinAmount = 350_000m; // PDI xe nội địa CKD
    private const decimal DefaultPdixAmount = 550_000m; // PDI xe nhập khẩu CBU
    private const decimal DefaultRxAmount = 120_000m;   // Rửa xe

    public async Task<string> GetNextSeqAsync()
    {
        var ym = DateTime.Now.ToString("yyMM");
        var prefix = $"{ym}PDI";
        var count = await db.PaymentPDIOrders
            .CountAsync(x => x.OrgId == tenant.OrgId && x.PmtPDINo.StartsWith(prefix));
        return $"{prefix}{(count + 1):D5}";
    }

    public async Task<IReadOnlyList<PaymentPdiEligibleCarDto>> GetEligibleCarsAsync(DateTime periodStart, DateTime periodEnd)
    {
        if (periodEnd == default) periodEnd = DateTime.Today;
        if (periodStart == default) periodStart = periodEnd.AddMonths(-1).AddDays(1);

        // Các VIN đã nằm trong bảng kê PDI chưa hủy (tránh trùng kỳ).
        var existingVins = await db.PaymentPDIDetails
            .Where(d => db.PaymentPDIOrders.Any(o => o.Id == d.PaymentPDIId && o.OrgId == tenant.OrgId && o.Status != PaymentPDIStatus.Cancelled))
            .Select(d => d.Vin)
            .Distinct()
            .ToListAsync();

        var vinExclusions = new HashSet<string>(existingVins, StringComparer.OrdinalIgnoreCase);

        var list = new List<PaymentPdiEligibleCarDto>();

        // Nguồn 1: kho xe vật lý có số khung VIN (Car_VIN).
        var inventories = await db.CarVinInventories.AsNoTracking()
            .Where(x => x.OrgId == tenant.OrgId)
            .ToListAsync();

        foreach (var inv in inventories)
        {
            if (vinExclusions.Contains(inv.Vin)) continue;

            var pdiType = string.Equals(inv.AssemblyStatus, "CBU", StringComparison.OrdinalIgnoreCase) ? "X" : "N";
            var (pdin, pdix, rx) = ResolveCost(pdiType);

            list.Add(new PaymentPdiEligibleCarDto(
                inv.Vin,
                inv.MappedCarId,
                inv.ModelCode,
                inv.ModelName,
                inv.SpecCode,
                inv.ColorCode,
                pdiType,
                inv.StorageCode,
                inv.CreatedAt.Date,
                inv.CreatedAt.ToString("yyyy-MM-dd"),
                pdin,
                pdix,
                rx
            ));
        }

        // Nguồn 2: bổ sung từ danh sách xe thương mại (Car_Car) nếu còn ít.
        if (list.Count < 3)
        {
            var carRecords = await db.Cars.AsNoTracking()
                .Where(c => c.OrgId == tenant.OrgId && !string.IsNullOrEmpty(c.Vin) && c.FlagActive == "1")
                .ToListAsync();

            foreach (var c in carRecords)
            {
                if (list.Any(x => x.Vin.Equals(c.Vin, StringComparison.OrdinalIgnoreCase)) || vinExclusions.Contains(c.Vin))
                    continue;

                var pdiType = "N";
                var (pdin, pdix, rx) = ResolveCost(pdiType);

                list.Add(new PaymentPdiEligibleCarDto(
                    c.Vin,
                    c.CarId,
                    c.ModelCode ?? "HYUNDAI",
                    c.ModelName ?? "Xe Hyundai",
                    c.SpecCode,
                    c.ColorCode,
                    pdiType,
                    c.StorageCode,
                    DateTime.Today.AddDays(-20),
                    DateTime.Today.AddDays(-20).ToString("yyyy-MM-dd"),
                    pdin,
                    pdix,
                    rx
                ));
            }
        }

        return list;
    }

    public async Task<PaymentPdiStatsDto> GetStatsAsync(string? pmtMonth = null)
    {
        var q = db.PaymentPDIOrders.AsNoTracking().Where(x => x.OrgId == tenant.OrgId);
        if (!string.IsNullOrWhiteSpace(pmtMonth))
            q = q.Where(x => x.PmtMonth == pmtMonth.Trim());

        var list = await q.ToListAsync();

        var active = list.Where(x => x.Status != PaymentPDIStatus.Cancelled).ToList();

        var monthlyGroup = await db.PaymentPDIOrders.AsNoTracking()
            .Where(x => x.OrgId == tenant.OrgId && x.Status != PaymentPDIStatus.Cancelled)
            .GroupBy(x => x.PmtMonth)
            .Select(g => new MonthlyPaymentPdiStatDto(
                g.Key,
                g.Count(),
                g.Sum(x => x.TotalCars),
                g.Sum(x => x.TotalAmountAfterVAT)
            ))
            .OrderByDescending(x => x.PmtMonth)
            .Take(12)
            .ToListAsync();

        return new PaymentPdiStatsDto(
            list.Count,
            list.Count(x => x.Status == PaymentPDIStatus.Pending),
            list.Count(x => x.Status == PaymentPDIStatus.Approved1),
            list.Count(x => x.Status == PaymentPDIStatus.Approved2),
            list.Count(x => x.Status == PaymentPDIStatus.Finished),
            list.Count(x => x.Status == PaymentPDIStatus.Rejected),
            list.Count(x => x.Status == PaymentPDIStatus.Cancelled),
            active.Sum(x => x.TotalCars),
            active.Sum(x => x.PDINTotalAmount),
            active.Sum(x => x.PDIXTotalAmount),
            active.Sum(x => x.RXTotalAmount),
            active.Sum(x => x.TotalAmount),
            active.Sum(x => x.AmountVAT),
            active.Sum(x => x.TotalAmountAfterVAT),
            monthlyGroup
        );
    }

    public async Task<IReadOnlyList<PaymentPdiSummaryDto>> ListAsync(
        string? pmtMonth,
        PaymentPDIStatus? status,
        string? pmtPDINo,
        PaymentPDISignStatus? htvSignStatus,
        PaymentPDISignStatus? tcmsSignStatus,
        int pageIndex = 0,
        int pageSize = 50)
    {
        var q = db.PaymentPDIOrders.AsNoTracking().Where(x => x.OrgId == tenant.OrgId);

        if (!string.IsNullOrWhiteSpace(pmtMonth))
            q = q.Where(x => x.PmtMonth == pmtMonth.Trim());

        if (status.HasValue)
            q = q.Where(x => x.Status == status.Value);

        if (!string.IsNullOrWhiteSpace(pmtPDINo))
            q = q.Where(x => x.PmtPDINo.Contains(pmtPDINo.Trim()));

        if (htvSignStatus.HasValue)
            q = q.Where(x => x.HTVSignStatus == htvSignStatus.Value);

        if (tcmsSignStatus.HasValue)
            q = q.Where(x => x.TCMSSignStatus == tcmsSignStatus.Value);

        return await q.OrderByDescending(x => x.CreatedAt)
            .Skip(pageIndex * pageSize)
            .Take(pageSize)
            .Select(x => new PaymentPdiSummaryDto(
                x.Id,
                x.PmtPDINo,
                x.PmtMonth,
                x.PmtPeriodStartDTime,
                x.PmtPeriodEndDTime,
                x.TotalCars,
                x.PDINTotalAmount,
                x.PDIXTotalAmount,
                x.PDITotalAmount,
                x.RXTotalAmount,
                x.TotalAmount,
                x.VAT,
                x.AmountVAT,
                x.TotalAmountAfterVAT,
                x.Status,
                x.HTVSignStatus,
                x.TCMSSignStatus,
                x.Remark,
                x.CreatedBy,
                x.CreatedAt
            ))
            .ToListAsync();
    }

    public async Task<PaymentPdiDetailDto?> DetailAsync(string pmtPDINo)
    {
        var order = await db.PaymentPDIOrders
            .Include(x => x.Details)
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.OrgId == tenant.OrgId && x.PmtPDINo == pmtPDINo.Trim());

        return order is null ? null : MapToDetailDto(order);
    }

    public async Task<PaymentPdiDetailDto> CreateAsync(CreatePaymentPDIDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.PmtMonth))
            throw new InvalidOperationException("Tháng thanh toán (PmtMonth) không được để trống (định dạng yyyy-MM).");

        if (dto.PmtPeriodStartDTime == default || dto.PmtPeriodEndDTime == default)
            throw new InvalidOperationException("Kỳ thanh toán (PmtPeriodStartDTime / PmtPeriodEndDTime) không được để trống.");

        if (dto.PmtPeriodStartDTime.Date > dto.PmtPeriodEndDTime.Date)
            throw new InvalidOperationException("Ngày bắt đầu kỳ thanh toán phải nhỏ hơn hoặc bằng ngày kết thúc kỳ.");

        if (dto.Vat < 0)
            throw new InvalidOperationException("Thuế suất VAT không được âm.");

        var pmtMonth = dto.PmtMonth.Trim();

        var pmtPDINo = dto.PmtPDINo?.Trim();
        if (string.IsNullOrWhiteSpace(pmtPDINo))
        {
            pmtPDINo = await GetNextSeqAsync();
        }
        else
        {
            var exists = await db.PaymentPDIOrders.AnyAsync(x => x.OrgId == tenant.OrgId && x.PmtPDINo == pmtPDINo);
            if (exists)
                throw new InvalidOperationException($"Bảng kê thanh toán PDI {pmtPDINo} đã tồn tại.");
        }

        // Ràng buộc kỳ thanh toán liên tiếp: kỳ mới phải bắt đầu = kỳ cuối (không Cancel/Reject) + 1 ngày.
        var lastPeriod = await db.PaymentPDIOrders.AsNoTracking()
            .Where(x => x.OrgId == tenant.OrgId
                && x.PmtPDINo != pmtPDINo
                && x.Status != PaymentPDIStatus.Cancelled
                && x.Status != PaymentPDIStatus.Rejected)
            .OrderByDescending(x => x.PmtPeriodEndDTime)
            .FirstOrDefaultAsync();

        if (lastPeriod != null)
        {
            var expectedStart = lastPeriod.PmtPeriodEndDTime.Date.AddDays(1);
            if (dto.PmtPeriodStartDTime.Date != expectedStart)
                throw new InvalidOperationException(
                    $"Kỳ thanh toán phải liên tiếp - không được trùng và không được có khoảng trống. " +
                    $"Kỳ mới phải bắt đầu từ {expectedStart:yyyy-MM-dd} (kỳ trước {lastPeriod.PmtPDINo} kết thúc {lastPeriod.PmtPeriodEndDTime:yyyy-MM-dd}).");
        }

        // Các bảng kê trước phải ở trạng thái đã kết thúc (F, C, R).
        var unfinished = await db.PaymentPDIOrders.AsNoTracking()
            .Where(x => x.OrgId == tenant.OrgId
                && x.Status != PaymentPDIStatus.Finished
                && x.Status != PaymentPDIStatus.Cancelled
                && x.Status != PaymentPDIStatus.Rejected)
            .FirstOrDefaultAsync();

        if (unfinished != null)
            throw new InvalidOperationException(
                $"Chỉ tạo được thanh toán khi các thanh toán trước đó đều có trạng thái F - Đã ký. " +
                $"Bảng kê {unfinished.PmtPDINo} đang ở trạng thái {unfinished.Status}.");

        var order = new PaymentPDIOrder
        {
            OrgId = tenant.OrgId,
            PmtPDINo = pmtPDINo,
            PmtMonth = pmtMonth,
            PmtPeriodStartDTime = dto.PmtPeriodStartDTime.Date,
            PmtPeriodEndDTime = dto.PmtPeriodEndDTime.Date,
            VAT = dto.Vat,
            Status = PaymentPDIStatus.Pending,
            HTVSignStatus = PaymentPDISignStatus.ChuaKy,
            TCMSSignStatus = PaymentPDISignStatus.ChuaKy,
            Remark = dto.Remark,
            CreatedBy = dto.CreatedBy ?? "SYSTEM_ADMIN",
            CreatedAt = DateTime.Now,
            LogLUDateTime = DateTime.Now,
            LogLUBy = dto.CreatedBy ?? "SYSTEM_ADMIN"
        };

        if (dto.Cars != null && dto.Cars.Count > 0)
        {
            foreach (var c in dto.Cars)
            {
                if (string.IsNullOrWhiteSpace(c.Vin))
                    throw new InvalidOperationException("Số khung VIN của dòng xe không được để trống.");

                order.Details.Add(BuildDetail(order.PmtPDINo, c));
            }
        }

        RecalculateTotals(order);

        db.PaymentPDIOrders.Add(order);
        await db.SaveChangesAsync();

        return MapToDetailDto(order);
    }

    public async Task<PaymentPdiDetailDto?> UpdateAsync(string pmtPDINo, UpdatePaymentPDIDto dto)
    {
        var order = await db.PaymentPDIOrders
            .Include(x => x.Details)
            .FirstOrDefaultAsync(x => x.OrgId == tenant.OrgId && x.PmtPDINo == pmtPDINo.Trim());

        if (order is null) return null;

        if (order.Status != PaymentPDIStatus.Pending && order.Status != PaymentPDIStatus.Rejected)
            throw new InvalidOperationException($"Chỉ cho phép cập nhật bảng kê PDI ở trạng thái Pending hoặc Rejected (hiện tại: {order.Status}).");

        if (dto.Remark != null) order.Remark = dto.Remark;

        if (dto.Vat.HasValue && dto.Vat.Value >= 0)
        {
            order.VAT = dto.Vat.Value;
            RecalculateTotals(order);
        }

        order.LogLUDateTime = DateTime.Now;
        order.LogLUBy = dto.UpdatedBy ?? "SYSTEM";

        await db.SaveChangesAsync();
        return MapToDetailDto(order);
    }

    public async Task<PaymentPdiDetailDto?> AddCarsAsync(string pmtPDINo, AddPaymentPDICarsDto dto)
    {
        var order = await db.PaymentPDIOrders
            .Include(x => x.Details)
            .FirstOrDefaultAsync(x => x.OrgId == tenant.OrgId && x.PmtPDINo == pmtPDINo.Trim());

        if (order is null) return null;

        if (order.Status != PaymentPDIStatus.Pending)
            throw new InvalidOperationException($"Chỉ có thể thêm xe vào bảng kê PDI ở trạng thái Pending (hiện tại: {order.Status}).");

        if (dto.Cars == null || dto.Cars.Count == 0)
            throw new InvalidOperationException("Danh sách xe bổ sung không được trống.");

        foreach (var c in dto.Cars)
        {
            if (string.IsNullOrWhiteSpace(c.Vin))
                throw new InvalidOperationException("Số khung VIN của dòng xe không được để trống.");

            if (order.Details.Any(x => x.Vin.Equals(c.Vin.Trim(), StringComparison.OrdinalIgnoreCase)))
                continue; // Bỏ qua trùng VIN

            order.Details.Add(BuildDetail(order.PmtPDINo, c));
        }

        RecalculateTotals(order);
        order.LogLUDateTime = DateTime.Now;
        order.LogLUBy = dto.UpdatedBy ?? "SYSTEM";

        await db.SaveChangesAsync();
        return MapToDetailDto(order);
    }

    public async Task<PaymentPdiDetailDto?> RemoveCarAsync(string pmtPDINo, string vin)
    {
        var order = await db.PaymentPDIOrders
            .Include(x => x.Details)
            .FirstOrDefaultAsync(x => x.OrgId == tenant.OrgId && x.PmtPDINo == pmtPDINo.Trim());

        if (order is null) return null;

        if (order.Status != PaymentPDIStatus.Pending)
            throw new InvalidOperationException($"Chỉ có thể gỡ xe khỏi bảng kê PDI ở trạng thái Pending (hiện tại: {order.Status}).");

        var item = order.Details.FirstOrDefault(x => x.Vin.Equals(vin.Trim(), StringComparison.OrdinalIgnoreCase));
        if (item != null)
        {
            order.Details.Remove(item);
            db.PaymentPDIDetails.Remove(item);
            RecalculateTotals(order);
            order.LogLUDateTime = DateTime.Now;
            order.LogLUBy = "SYSTEM";
            await db.SaveChangesAsync();
        }

        return MapToDetailDto(order);
    }

    public async Task<PaymentPdiDetailDto?> Approve1Async(string pmtPDINo, Approve1PaymentPDIDto? dto)
    {
        var order = await db.PaymentPDIOrders
            .Include(x => x.Details)
            .FirstOrDefaultAsync(x => x.OrgId == tenant.OrgId && x.PmtPDINo == pmtPDINo.Trim());

        if (order is null) return null;

        if (order.Status != PaymentPDIStatus.Pending)
            throw new InvalidOperationException($"Bảng kê {pmtPDINo} đang ở trạng thái {order.Status}, không thể thực hiện phê duyệt cấp 1 (yêu cầu Pending).");

        order.Status = PaymentPDIStatus.Approved1;
        order.Approve1At = DateTime.Now;
        order.Approve1By = dto?.Approved1By ?? "NPP_PDI_MANAGER";
        if (!string.IsNullOrWhiteSpace(dto?.Remark))
            order.Remark = (order.Remark != null ? order.Remark + " | " : "") + dto.Remark.Trim();

        order.LogLUDateTime = DateTime.Now;
        order.LogLUBy = order.Approve1By;

        await db.SaveChangesAsync();
        return MapToDetailDto(order);
    }

    public async Task<PaymentPdiDetailDto?> Approve2Async(string pmtPDINo, Approve2PaymentPDIDto? dto)
    {
        var order = await db.PaymentPDIOrders
            .Include(x => x.Details)
            .FirstOrDefaultAsync(x => x.OrgId == tenant.OrgId && x.PmtPDINo == pmtPDINo.Trim());

        if (order is null) return null;

        if (order.Status != PaymentPDIStatus.Approved1)
            throw new InvalidOperationException($"Bảng kê {pmtPDINo} chưa được duyệt cấp 1 (trạng thái hiện tại: {order.Status}).");

        order.Status = PaymentPDIStatus.Approved2;
        order.Approve2At = DateTime.Now;
        order.Approve2By = dto?.Approved2By ?? "NPP_LEADER";
        if (!string.IsNullOrWhiteSpace(dto?.Remark))
            order.Remark = (order.Remark != null ? order.Remark + " | " : "") + dto.Remark.Trim();

        order.LogLUDateTime = DateTime.Now;
        order.LogLUBy = order.Approve2By;

        await db.SaveChangesAsync();
        return MapToDetailDto(order);
    }

    public async Task<PaymentPdiDetailDto?> TCMSSignAsync(string pmtPDINo, SignPaymentPDIDto? dto)
    {
        var order = await db.PaymentPDIOrders
            .Include(x => x.Details)
            .FirstOrDefaultAsync(x => x.OrgId == tenant.OrgId && x.PmtPDINo == pmtPDINo.Trim());

        if (order is null) return null;

        // Workflow PDI: TCMS ký yêu cầu Status=A2, HTVSign=P, TCMSSign=P.
        if (order.Status != PaymentPDIStatus.Approved2)
            throw new InvalidOperationException($"Bảng kê {pmtPDINo} phải được duyệt cấp 2 (A2) trước khi TCMS ký số (trạng thái hiện tại: {order.Status}).");

        if (order.TCMSSignStatus == PaymentPDISignStatus.DaKy)
            throw new InvalidOperationException($"Bảng kê {pmtPDINo} đã được TCMS ký số trước đó.");

        order.TCMSSignStatus = PaymentPDISignStatus.DaKy;
        order.TCMSSignDTime = DateTime.Now;
        order.TCMSSignBy = dto?.SignedBy ?? "TCMS_PDI_DIRECTOR";
        order.PDIFilePath = $"/reports/payment-pdi/{order.PmtPDINo}_PDI_TCMS.pdf";
        order.RXFilePath = $"/reports/payment-pdi/{order.PmtPDINo}_RX_TCMS.pdf";

        if (!string.IsNullOrWhiteSpace(dto?.Remark))
            order.Remark = (order.Remark != null ? order.Remark + " | " : "") + dto.Remark.Trim();

        order.LogLUDateTime = DateTime.Now;
        order.LogLUBy = order.TCMSSignBy;

        await db.SaveChangesAsync();
        return MapToDetailDto(order);
    }

    public async Task<PaymentPdiDetailDto?> HTVSignAsync(string pmtPDINo, SignPaymentPDIDto? dto)
    {
        var order = await db.PaymentPDIOrders
            .Include(x => x.Details)
            .FirstOrDefaultAsync(x => x.OrgId == tenant.OrgId && x.PmtPDINo == pmtPDINo.Trim());

        if (order is null) return null;

        // Workflow PDI: HTV ký SAU (yêu cầu Status=A2, HTVSign=P, TCMSSign=A). Ký xong -> PmtPDIStatus=F.
        if (order.Status != PaymentPDIStatus.Approved2)
            throw new InvalidOperationException($"Bảng kê {pmtPDINo} phải được duyệt cấp 2 (A2) trước khi HTV ký số (trạng thái hiện tại: {order.Status}).");

        if (order.TCMSSignStatus != PaymentPDISignStatus.DaKy)
            throw new InvalidOperationException($"Bảng kê {pmtPDINo} phải được TCMS ký số trước khi HTV ký số.");

        if (order.HTVSignStatus == PaymentPDISignStatus.DaKy)
            throw new InvalidOperationException($"Bảng kê {pmtPDINo} đã được HTV ký số trước đó.");

        order.HTVSignStatus = PaymentPDISignStatus.DaKy;
        order.HTVSignDTime = DateTime.Now;
        order.HTVSignBy = dto?.SignedBy ?? "HTV_REP_KIM";
        order.PDIFilePath = $"/reports/payment-pdi/{order.PmtPDINo}_PDI_FullySigned.pdf";
        order.RXFilePath = $"/reports/payment-pdi/{order.PmtPDINo}_RX_FullySigned.pdf";
        order.Status = PaymentPDIStatus.Finished;

        if (!string.IsNullOrWhiteSpace(dto?.Remark))
            order.Remark = (order.Remark != null ? order.Remark + " | " : "") + dto.Remark.Trim();

        order.LogLUDateTime = DateTime.Now;
        order.LogLUBy = order.HTVSignBy;

        await db.SaveChangesAsync();
        return MapToDetailDto(order);
    }

    public async Task<PaymentPdiDetailDto?> UploadInvoiceAsync(string pmtPDINo, UploadInvoicePaymentPDIDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.InvoiceNo))
            throw new InvalidOperationException("Số hóa đơn GTGT không được để trống.");

        var order = await db.PaymentPDIOrders
            .Include(x => x.Details)
            .FirstOrDefaultAsync(x => x.OrgId == tenant.OrgId && x.PmtPDINo == pmtPDINo.Trim());

        if (order is null) return null;

        var type = (dto.InvoiceType ?? "").Trim().ToUpperInvariant();
        if (type != "PDI" && type != "RX")
            throw new InvalidOperationException("Loại hóa đơn (InvoiceType) chỉ nhận giá trị 'PDI' hoặc 'RX'.");

        var now = DateTime.Now;
        var by = dto.UploadedBy ?? "TCMS_ACCOUNTANT";

        if (type == "PDI")
        {
            order.PDIInvoiceNo = dto.InvoiceNo.Trim();
            order.PDIInvoiceDate = dto.InvoiceDate?.Date ?? now.Date;
            order.PDIFileInvPath = $"/reports/payment-pdi/{order.PmtPDINo}_PDI_Invoice.pdf";
            order.PDIFileInvUpDTime = now;
            order.PDIFileInvUpBy = by;
        }
        else
        {
            order.RXInvoiceNo = dto.InvoiceNo.Trim();
            order.RXInvoiceDate = dto.InvoiceDate?.Date ?? now.Date;
            order.RXFileInvPath = $"/reports/payment-pdi/{order.PmtPDINo}_RX_Invoice.pdf";
            order.RXFileInvUpDTime = now;
            order.RXFileInvUpBy = by;
        }

        order.LogLUDateTime = now;
        order.LogLUBy = by;

        await db.SaveChangesAsync();
        return MapToDetailDto(order);
    }

    public async Task<PaymentPdiDetailDto?> RejectAsync(string pmtPDINo, RejectPaymentPDIDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Reason))
            throw new InvalidOperationException("Vui lòng cung cấp lý do từ chối duyệt bảng kê.");

        var order = await db.PaymentPDIOrders
            .Include(x => x.Details)
            .FirstOrDefaultAsync(x => x.OrgId == tenant.OrgId && x.PmtPDINo == pmtPDINo.Trim());

        if (order is null) return null;

        if (order.Status == PaymentPDIStatus.Finished || order.Status == PaymentPDIStatus.Cancelled)
            throw new InvalidOperationException($"Không thể từ chối bảng kê ở trạng thái {order.Status}.");

        order.Status = PaymentPDIStatus.Rejected;
        order.RejectReason = dto.Reason.Trim();
        order.RejectedAt = DateTime.Now;
        order.RejectedBy = dto.RejectedBy ?? "NPP_REVIEWER";
        order.LogLUDateTime = DateTime.Now;
        order.LogLUBy = order.RejectedBy;

        await db.SaveChangesAsync();
        return MapToDetailDto(order);
    }

    public async Task<PaymentPdiDetailDto?> CancelAsync(string pmtPDINo, CancelPaymentPDIDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Reason))
            throw new InvalidOperationException("Vui lòng cung cấp lý do hủy bảng kê.");

        var order = await db.PaymentPDIOrders
            .Include(x => x.Details)
            .FirstOrDefaultAsync(x => x.OrgId == tenant.OrgId && x.PmtPDINo == pmtPDINo.Trim());

        if (order is null) return null;

        if (order.Status == PaymentPDIStatus.Finished)
            throw new InvalidOperationException("Không thể hủy bảng kê đã hoàn tất ký số quyết toán 2 bên.");

        order.Status = PaymentPDIStatus.Cancelled;
        order.CancelReason = dto.Reason.Trim();
        order.CancelledAt = DateTime.Now;
        order.CancelledBy = dto.CancelledBy ?? "SYSTEM";
        order.LogLUDateTime = DateTime.Now;
        order.LogLUBy = order.CancelledBy;

        await db.SaveChangesAsync();
        return MapToDetailDto(order);
    }

    public async Task<bool> DeleteDraftAsync(string pmtPDINo)
    {
        var order = await db.PaymentPDIOrders
            .Include(x => x.Details)
            .FirstOrDefaultAsync(x => x.OrgId == tenant.OrgId && x.PmtPDINo == pmtPDINo.Trim());

        if (order is null) return false;

        if (order.Status != PaymentPDIStatus.Pending && order.Status != PaymentPDIStatus.Rejected)
            throw new InvalidOperationException($"Chỉ có thể xóa bảng kê ở trạng thái Pending hoặc Rejected (trạng thái hiện tại: {order.Status}).");

        db.PaymentPDIDetails.RemoveRange(order.Details);
        db.PaymentPDIOrders.Remove(order);
        await db.SaveChangesAsync();
        return true;
    }

    public async Task<PaymentPdiPrintDto?> GetPrintDataAsync(string pmtPDINo)
    {
        var order = await db.PaymentPDIOrders
            .Include(x => x.Details)
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.OrgId == tenant.OrgId && x.PmtPDINo == pmtPDINo.Trim());

        if (order is null) return null;

        var periodText = $"Kỳ thanh toán từ {order.PmtPeriodStartDTime:dd/MM/yyyy} đến {order.PmtPeriodEndDTime:dd/MM/yyyy}";
        var payer = "CÔNG TY CỔ PHẦN LIÊN DOANH Ô TÔ HYUNDAI THÀNH CÔNG VIỆT NAM (HTV)";
        var receiver = "CÔNG TY CỔ PHẦN DỊCH VỤ VẬN TẢI VÀ THIẾT BỊ THÔNG MINH TCMS";

        var statusText = order.Status switch
        {
            PaymentPDIStatus.Pending => "Chờ duyệt cấp 1",
            PaymentPDIStatus.Approved1 => "Đã duyệt cấp 1",
            PaymentPDIStatus.Approved2 => "Đã duyệt cấp 2",
            PaymentPDIStatus.Finished => "Hoàn tất ký số 2 bên",
            PaymentPDIStatus.Rejected => "Từ chối duyệt",
            PaymentPDIStatus.Cancelled => "Đã hủy",
            _ => order.Status.ToString()
        };

        var htvSign = order.HTVSignStatus == PaymentPDISignStatus.DaKy ? "ĐÃ KÝ SỐ ĐIỆN TỬ (CA)" : "CHƯA KÝ";
        var tcmsSign = order.TCMSSignStatus == PaymentPDISignStatus.DaKy ? "ĐÃ KÝ SỐ ĐIỆN TỬ (CA)" : "CHƯA KÝ";

        var carList = order.Details.Select(MapDetailItem).ToList();

        return new PaymentPdiPrintDto(
            order.PmtPDINo,
            order.PmtMonth,
            periodText,
            payer,
            receiver,
            order.TotalCars,
            order.PDINTotalAmount,
            order.PDIXTotalAmount,
            order.PDITotalAmount,
            order.RXTotalAmount,
            order.TotalAmount,
            order.VAT,
            order.AmountVAT,
            order.TotalAmountAfterVAT,
            statusText,
            htvSign,
            tcmsSign,
            order.HTVSignBy,
            order.HTVSignDTime,
            order.TCMSSignBy,
            order.TCMSSignDTime,
            carList
        );
    }

    private static (decimal pdin, decimal pdix, decimal rx) ResolveCost(string? pdiType)
    {
        if (string.Equals(pdiType, "X", StringComparison.OrdinalIgnoreCase))
            return (0m, DefaultPdixAmount, DefaultRxAmount);
        return (DefaultPdinAmount, 0m, DefaultRxAmount);
    }

    private static PaymentPDIDetail BuildDetail(string pmtPDINo, PaymentPdiCarItemDto c)
    {
        var pdiType = string.IsNullOrWhiteSpace(c.PdiType)
            ? "N"
            : c.PdiType.Trim().ToUpperInvariant();

        var (defPdin, defPdix, defRx) = ResolveCost(pdiType);

        var pdin = c.PdinAmount ?? defPdin;
        var pdix = c.PdixAmount ?? defPdix;
        var rx = c.RxAmount ?? defRx;

        if (pdin < 0 || pdix < 0 || rx < 0)
            throw new InvalidOperationException($"Chi phí PDI/Rửa xe của VIN {c.Vin} không được âm.");

        var pdiAmount = pdin + pdix;

        return new PaymentPDIDetail
        {
            PmtPDINo = pmtPDINo,
            Vin = c.Vin.Trim(),
            CarId = c.CarId,
            ModelCode = c.ModelCode?.Trim() ?? "",
            ModelName = c.ModelName ?? c.ModelCode ?? "",
            SpecCode = c.SpecCode,
            ColorCode = c.ColorCode,
            PDIType = pdiType,
            RefNoInit = c.RefNoInit,
            StorageCodeInit = c.StorageCodeInit,
            StorageDateInit = c.StorageDateInit?.Date,
            InvoiceFactoryDate = c.InvoiceFactoryDate,
            RefNoCost = c.RefNoCost,
            StorageCodeCost = c.StorageCodeCost,
            RefNoOut = c.RefNoOut,
            StorageCodeOut = c.StorageCodeOut,
            StorageDateOut = c.StorageDateOut?.Date,
            DeliveryOrderNo = c.DeliveryOrderNo,
            DealerCode = c.DealerCode,
            DealerName = c.DealerName,
            PDINAmount = pdin,
            PDIXAmount = pdix,
            PDIAmount = pdiAmount,
            RXAmount = rx,
            TotalAmount = pdiAmount + rx,
            Remark = c.Remark,
            LogLUDateTime = DateTime.Now,
            LogLUBy = "SYSTEM"
        };
    }

    private static void RecalculateTotals(PaymentPDIOrder order)
    {
        order.TotalCars = order.Details.Count;
        order.PDINTotalAmount = order.Details.Sum(x => x.PDINAmount);
        order.PDIXTotalAmount = order.Details.Sum(x => x.PDIXAmount);
        order.PDITotalAmount = order.PDINTotalAmount + order.PDIXTotalAmount;
        order.RXTotalAmount = order.Details.Sum(x => x.RXAmount);
        order.TotalAmount = order.PDITotalAmount + order.RXTotalAmount;
        order.AmountVAT = Math.Round(order.TotalAmount * order.VAT / 100m, 0);
        order.TotalAmountAfterVAT = order.TotalAmount + order.AmountVAT;
    }

    private static PaymentPdiDetailItemDto MapDetailItem(PaymentPDIDetail d) => new(
        d.Id,
        d.PmtPDINo,
        d.Vin,
        d.CarId,
        d.ModelCode,
        d.ModelName,
        d.SpecCode,
        d.ColorCode,
        d.PDIType,
        d.RefNoInit,
        d.StorageCodeInit,
        d.StorageDateInit,
        d.InvoiceFactoryDate,
        d.RefNoCost,
        d.StorageCodeCost,
        d.RefNoOut,
        d.StorageCodeOut,
        d.StorageDateOut,
        d.DeliveryOrderNo,
        d.DealerCode,
        d.DealerName,
        d.PDINAmount,
        d.PDIXAmount,
        d.PDIAmount,
        d.RXAmount,
        d.TotalAmount,
        d.Remark
    );

    private static PaymentPdiDetailDto MapToDetailDto(PaymentPDIOrder order)
    {
        var details = order.Details.Select(MapDetailItem).ToList();

        return new PaymentPdiDetailDto(
            order.Id,
            order.PmtPDINo,
            order.PmtMonth,
            order.PmtPeriodStartDTime,
            order.PmtPeriodEndDTime,
            order.TotalCars,
            order.PDINTotalAmount,
            order.PDIXTotalAmount,
            order.PDITotalAmount,
            order.RXTotalAmount,
            order.TotalAmount,
            order.VAT,
            order.AmountVAT,
            order.TotalAmountAfterVAT,
            order.Status,
            order.HTVSignStatus,
            order.HTVSignDTime,
            order.HTVSignBy,
            order.TCMSSignStatus,
            order.TCMSSignDTime,
            order.TCMSSignBy,
            order.PDIFilePath,
            order.RXFilePath,
            order.PDIInvoiceNo,
            order.PDIInvoiceDate,
            order.RXInvoiceNo,
            order.RXInvoiceDate,
            order.Remark,
            order.RejectReason,
            order.CancelReason,
            order.CreatedAt,
            order.CreatedBy,
            order.Approve1At,
            order.Approve1By,
            order.Approve2At,
            order.Approve2By,
            order.RejectedAt,
            order.RejectedBy,
            order.CancelledAt,
            order.CancelledBy,
            order.LogLUDateTime,
            order.LogLUBy,
            details
        );
    }
}
