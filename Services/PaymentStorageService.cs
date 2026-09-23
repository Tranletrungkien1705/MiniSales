using Microsoft.EntityFrameworkCore;
using MiniSales.Data;
using MiniSales.Models;

namespace MiniSales.Services;

public record PaymentStorageCarItemDto(
    string Vin,
    string RefNo,
    string? CarId = null,
    string ModelCode = "",
    string? ModelName = null,
    string? SpecCode = null,
    string? ColorCode = null,
    string StorageCode = "KHO_NB",
    string? StorageName = null,
    DateTime? StorageDateIn = null,
    DateTime? StorageDateOut = null,
    DateTime? DateBegin = null,
    DateTime? DateEnd = null,
    int DelayTransport = 0,
    decimal? PriceBCP = null,
    decimal? PriceLK = null,
    decimal? AmountBCP = null,
    decimal? AmountLK = null,
    string? DealerCode = null,
    string? DealerName = null,
    string? PackingListNo = null,
    string? DeliveryOrderNo = null,
    string? StorageRearrangeNoIn = null,
    string? StorageRearrangeNoOut = null,
    string? RetrieveOrderNo = null,
    string? InvoiceFactoryDate = null,
    string? Remark = null
);

public record CreatePaymentStorageDto(
    string PmtMonth,
    DateTime PmtPeriodStartDTime,
    DateTime PmtPeriodEndDTime,
    string? PaymentStorageNo = null,
    decimal VAT = 10m,
    string? Remark = null,
    string? CreatedBy = null,
    List<PaymentStorageCarItemDto>? Cars = null
);

public record UpdatePaymentStorageDto(
    decimal? VAT = null,
    string? Remark = null,
    string? UpdatedBy = null,
    List<PaymentStorageCarItemDto>? Cars = null
);

public record AddPaymentStorageCarsDto(
    List<PaymentStorageCarItemDto> Cars,
    string? UpdatedBy = null
);

public record Approve1PaymentStorageDto(
    string? Approved1By = null,
    string? Remark = null
);

public record Approve2PaymentStorageDto(
    string? Approved2By = null,
    string? Remark = null
);

public record BatchApprove2PaymentStorageDto(
    List<string> PaymentStorageNos,
    string? Approved2By = null
);

public record TCMSSignPaymentStorageDto(
    string? SignedBy = null,
    string? EContractFileName = null,
    string? Remark = null
);

public record HTVESignPaymentStorageDto(
    string? SignedBy = null,
    string? EContractFileName = null,
    string? FilePath = null,
    string? Remark = null
);

public record UploadPaymentStorageInvoiceDto(
    string FileInvoiceName,
    string? FileInvoiceUrl = null,
    string? InvoiceNo = null,
    DateTime? InvoiceDate = null,
    string? UploadedBy = null
);

public record RejectPaymentStorageDto(
    string Reason,
    string? RejectedBy = null
);

public record CancelPaymentStorageDto(
    string Reason,
    string? CancelledBy = null
);

public record PaymentStoragePreviewRequestDto(
    string PmtMonth,
    DateTime PmtPeriodStartDTime,
    DateTime PmtPeriodEndDTime,
    decimal VAT = 10m,
    string? StorageCode = null,
    List<PaymentStorageCarItemDto>? Cars = null
);

public record PaymentStorageDetailItemDto(
    long Id,
    string PaymentStorageNo,
    string Vin,
    string RefNo,
    string? CarId,
    string ModelCode,
    string ModelName,
    string? SpecCode,
    string? ColorCode,
    string StorageCode,
    string StorageName,
    DateTime? StorageDateIn,
    DateTime? StorageDateOut,
    DateTime DateBegin,
    DateTime DateEnd,
    int DelayTransport,
    int DateCount,
    decimal PriceBCP,
    decimal PriceLK,
    decimal AmountBCP,
    decimal AmountLK,
    decimal AmountTotal,
    string? DealerCode,
    string? DealerName,
    string? PackingListNo,
    string? DeliveryOrderNo,
    string? StorageRearrangeNoIn,
    string? StorageRearrangeNoOut,
    string? RetrieveOrderNo,
    string? InvoiceFactoryDate,
    string? Remark
);

public record PaymentStorageOrderDto(
    long Id,
    string PaymentStorageNo,
    string PmtMonth,
    DateTime PmtPeriodStartDTime,
    DateTime PmtPeriodEndDTime,
    decimal VAT,
    int TotalCars,
    decimal AmountBCP,
    decimal AmountLK,
    decimal AmountTotal,
    decimal AmountVAT,
    decimal AmountVATTotal,
    PaymentStorageStatus Status,
    string StatusName,
    PaymentStorageSignStatus HTVSignStatus,
    DateTime? HTVSignDTime,
    string? HTVSignBy,
    PaymentStorageSignStatus TCMSSignStatus,
    DateTime? TCMSSignDTime,
    string? TCMSSignBy,
    string? FilePath,
    string? FileUrl,
    string? FileInvPath,
    string? FileInvUrl,
    string? InvoiceNo,
    DateTime? InvoiceDate,
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
    List<PaymentStorageDetailItemDto> Details
);

public record PaymentStoragePreviewResponseDto(
    string PmtMonth,
    DateTime PmtPeriodStartDTime,
    DateTime PmtPeriodEndDTime,
    decimal VAT,
    int TotalCars,
    decimal AmountBCP,
    decimal AmountLK,
    decimal AmountTotal,
    decimal AmountVAT,
    decimal AmountVATTotal,
    List<PaymentStorageDetailItemDto> Details
);

public record PaymentStorageStatsDto(
    int TotalOrders,
    int PendingOrders,
    int Approved1Orders,
    int Approved2Orders,
    int FinishedOrders,
    int RejectedOrders,
    int CancelledOrders,
    decimal TotalAmountBeforeVAT,
    decimal TotalAmountVAT,
    decimal TotalAmountAfterVAT,
    decimal TotalBCPAmount,
    decimal TotalLKAmount,
    int TotalCarsCount
);

public record PaymentStoragePrintDto(
    PaymentStorageOrderDto Order,
    Dictionary<string, int> ModelSummary,
    Dictionary<string, decimal> StorageSummary
);

public interface IPaymentStorageService
{
    Task<string> GetNextSeqAsync();
    Task<List<InventoryCostMaster>> GetMasterCostsAsync(string? storageCode = null, string? costTypeCode = null);
    Task<List<PaymentStorageCarItemDto>> GetEligibleCarsAsync(DateTime pmtPeriodStart, DateTime pmtPeriodEnd, string? storageCode = null, string? modelCode = null);
    Task<PaymentStoragePreviewResponseDto> PreviewCalculationAsync(PaymentStoragePreviewRequestDto dto);
    Task<List<PaymentStorageOrderDto>> ListAsync(string? pmtMonth = null, PaymentStorageStatus? status = null, string? paymentStorageNo = null, string? storageCode = null, string? vin = null, PaymentStorageSignStatus? htvSignStatus = null, PaymentStorageSignStatus? tcmsSignStatus = null, int pageIndex = 0, int pageSize = 50);
    Task<PaymentStorageOrderDto?> DetailAsync(string paymentStorageNo);
    Task<PaymentStorageOrderDto> CreateAsync(CreatePaymentStorageDto dto);
    Task<PaymentStorageOrderDto?> UpdateAsync(string paymentStorageNo, UpdatePaymentStorageDto dto);
    Task<PaymentStorageOrderDto?> AddCarsAsync(string paymentStorageNo, AddPaymentStorageCarsDto dto);
    Task<PaymentStorageOrderDto?> RemoveCarAsync(string paymentStorageNo, string vin);
    Task<PaymentStorageOrderDto?> Approve1Async(string paymentStorageNo, Approve1PaymentStorageDto? dto);
    Task<PaymentStorageOrderDto?> Approve2Async(string paymentStorageNo, Approve2PaymentStorageDto? dto);
    Task<List<string>> BatchApprove2Async(BatchApprove2PaymentStorageDto dto);
    Task<PaymentStorageOrderDto?> TCMSESignAsync(string paymentStorageNo, TCMSSignPaymentStorageDto dto);
    Task<PaymentStorageOrderDto?> HTVESignAsync(string paymentStorageNo, HTVESignPaymentStorageDto dto);
    Task<PaymentStorageOrderDto?> UploadInvoiceAsync(string paymentStorageNo, UploadPaymentStorageInvoiceDto dto);
    Task<PaymentStorageOrderDto?> RejectAsync(string paymentStorageNo, RejectPaymentStorageDto dto);
    Task<PaymentStorageOrderDto?> CancelAsync(string paymentStorageNo, CancelPaymentStorageDto dto);
    Task<bool> DeleteDraftAsync(string paymentStorageNo);
    Task<PaymentStoragePrintDto?> GetPrintDataAsync(string paymentStorageNo);
    Task<PaymentStorageStatsDto> GetStatsAsync(string? pmtMonth = null);
}

public class PaymentStorageService(AppDbContext db, ITenantContext tenant) : IPaymentStorageService
{
    private Guid OrgId => tenant.OrgId;

    public async Task<string> GetNextSeqAsync()
    {
        var prefix = DateTime.Now.ToString("yyMM") + "PST";
        var count = await db.PaymentStorageOrders
            .Where(x => x.OrgId == OrgId && x.PaymentStorageNo.StartsWith(prefix))
            .CountAsync();
        return $"{prefix}{(count + 1):D5}";
    }

    public async Task<List<InventoryCostMaster>> GetMasterCostsAsync(string? storageCode = null, string? costTypeCode = null)
    {
        var q = db.InventoryCosts.Where(x => x.IsActive);
        if (!string.IsNullOrWhiteSpace(storageCode))
            q = q.Where(x => x.StorageCode == storageCode);
        if (!string.IsNullOrWhiteSpace(costTypeCode))
            q = q.Where(x => x.CostTypeCode == costTypeCode);
        return await q.OrderBy(x => x.StorageCode).ThenBy(x => x.CostTypeCode).ThenBy(x => x.ModelCode).ToListAsync();
    }

    public async Task<List<PaymentStorageCarItemDto>> GetEligibleCarsAsync(DateTime pmtPeriodStart, DateTime pmtPeriodEnd, string? storageCode = null, string? modelCode = null)
    {
        var existingVinsInPeriod = await db.PaymentStorageDetails
            .Include(d => d.PaymentStorageId)
            .Where(d => db.PaymentStorageOrders.Any(o => o.Id == d.PaymentStorageId && o.OrgId == OrgId &&
                o.Status != PaymentStorageStatus.Cancelled && o.Status != PaymentStorageStatus.Rejected &&
                o.PmtPeriodStartDTime <= pmtPeriodEnd && o.PmtPeriodEndDTime >= pmtPeriodStart))
            .Select(d => d.Vin)
            .Distinct()
            .ToListAsync();

        var masterCosts = await db.InventoryCosts.Where(x => x.IsActive).ToListAsync();

        var q = db.CarVinInventories.Where(x => x.OrgId == OrgId && !existingVinsInPeriod.Contains(x.Vin));
        if (!string.IsNullOrWhiteSpace(storageCode))
            q = q.Where(x => x.StorageCode == storageCode);
        if (!string.IsNullOrWhiteSpace(modelCode))
            q = q.Where(x => x.ModelCode == modelCode);

        var vins = await q.Take(50).ToListAsync();
        var result = new List<PaymentStorageCarItemDto>();

        int refIdx = 1;
        foreach (var v in vins)
        {
            var stCode = string.IsNullOrWhiteSpace(v.StorageCode) ? "KHO_NB" : v.StorageCode;
            var bcpRate = masterCosts.FirstOrDefault(c => c.StorageCode == stCode && c.CostTypeCode == "BCP" && c.ModelCode == v.ModelCode)?.UnitPrice ?? 30000m;
            var lkRate = masterCosts.FirstOrDefault(c => c.StorageCode == stCode && c.CostTypeCode == "LK" && c.ModelCode == v.ModelCode)?.UnitPrice ?? 45000m;

            var inDate = v.ProductionMonth != null ? pmtPeriodStart.AddDays(-10) : pmtPeriodStart;
            var outDate = (DateTime?)null;

            var dateBegin = inDate > pmtPeriodStart ? inDate : pmtPeriodStart;
            var dateEnd = pmtPeriodEnd;
            var dateCount = Math.Max(0, (dateEnd.Date - dateBegin.Date).Days + 1);

            result.Add(new PaymentStorageCarItemDto(
                Vin: v.Vin,
                RefNo: $"REF-{DateTime.Now:yyMM}{refIdx++:D4}",
                CarId: $"CAR-{v.Vin[..Math.Min(8, v.Vin.Length)]}",
                ModelCode: v.ModelCode,
                ModelName: v.ModelName,
                SpecCode: v.SpecCode,
                ColorCode: v.ColorCode,
                StorageCode: stCode,
                StorageName: v.StorageName ?? (stCode == "KHO_NB" ? "Kho Nhà máy Ninh Bình" : stCode),
                StorageDateIn: inDate,
                StorageDateOut: outDate,
                DateBegin: dateBegin,
                DateEnd: dateEnd,
                DelayTransport: 0,
                PriceBCP: bcpRate,
                PriceLK: lkRate,
                AmountBCP: dateCount * bcpRate,
                AmountLK: dateCount * lkRate,
                DealerCode: "VN001",
                DealerName: "Hyundai Đông Đô",
                PackingListNo: null,
                DeliveryOrderNo: null,
                Remark: "Xe tồn bãi đủ điều kiện tính chi phí lưu kho kỳ này"
            ));
        }

        return result;
    }

    public async Task<PaymentStoragePreviewResponseDto> PreviewCalculationAsync(PaymentStoragePreviewRequestDto dto)
    {
        var start = dto.PmtPeriodStartDTime.Date;
        var end = dto.PmtPeriodEndDTime.Date;
        if (start > end)
            throw new InvalidOperationException("Ngày bắt đầu kỳ tính phí không được sau ngày kết thúc.");

        var masterCosts = await db.InventoryCosts.Where(x => x.IsActive).ToListAsync();
        var cars = dto.Cars ?? await GetEligibleCarsAsync(start, end, dto.StorageCode);

        var details = new List<PaymentStorageDetailItemDto>();
        decimal sumBCP = 0m;
        decimal sumLK = 0m;
        long dummyId = 1;

        foreach (var c in cars)
        {
            var stCode = string.IsNullOrWhiteSpace(c.StorageCode) ? "KHO_NB" : c.StorageCode;
            var bcpRate = c.PriceBCP ?? masterCosts.FirstOrDefault(x => x.StorageCode == stCode && x.CostTypeCode == "BCP" && x.ModelCode == c.ModelCode)?.UnitPrice ?? 30000m;
            var lkRate = c.PriceLK ?? masterCosts.FirstOrDefault(x => x.StorageCode == stCode && x.CostTypeCode == "LK" && x.ModelCode == c.ModelCode)?.UnitPrice ?? 45000m;

            var dBegin = c.DateBegin?.Date ?? start;
            var dEnd = c.DateEnd?.Date ?? end;
            if (dBegin < start) dBegin = start;
            if (dEnd > end) dEnd = end;

            var days = Math.Max(0, (dEnd - dBegin).Days + 1 - c.DelayTransport);
            var aBCP = c.AmountBCP ?? (days * bcpRate);
            var aLK = c.AmountLK ?? (days * lkRate);
            var aTotal = aBCP + aLK;

            sumBCP += aBCP;
            sumLK += aLK;

            details.Add(new PaymentStorageDetailItemDto(
                Id: dummyId++,
                PaymentStorageNo: "PREVIEW",
                Vin: c.Vin,
                RefNo: c.RefNo,
                CarId: c.CarId,
                ModelCode: c.ModelCode,
                ModelName: c.ModelName ?? c.ModelCode,
                SpecCode: c.SpecCode,
                ColorCode: c.ColorCode,
                StorageCode: stCode,
                StorageName: c.StorageName ?? stCode,
                StorageDateIn: c.StorageDateIn,
                StorageDateOut: c.StorageDateOut,
                DateBegin: dBegin,
                DateEnd: dEnd,
                DelayTransport: c.DelayTransport,
                DateCount: days,
                PriceBCP: bcpRate,
                PriceLK: lkRate,
                AmountBCP: aBCP,
                AmountLK: aLK,
                AmountTotal: aTotal,
                DealerCode: c.DealerCode,
                DealerName: c.DealerName,
                PackingListNo: c.PackingListNo,
                DeliveryOrderNo: c.DeliveryOrderNo,
                StorageRearrangeNoIn: c.StorageRearrangeNoIn,
                StorageRearrangeNoOut: c.StorageRearrangeNoOut,
                RetrieveOrderNo: c.RetrieveOrderNo,
                InvoiceFactoryDate: c.InvoiceFactoryDate,
                Remark: c.Remark
            ));
        }

        var totalBeforeVAT = sumBCP + sumLK;
        var vatAmount = Math.Round(totalBeforeVAT * dto.VAT / 100m, 0);
        var totalAfterVAT = totalBeforeVAT + vatAmount;

        return new PaymentStoragePreviewResponseDto(
            PmtMonth: dto.PmtMonth,
            PmtPeriodStartDTime: start,
            PmtPeriodEndDTime: end,
            VAT: dto.VAT,
            TotalCars: details.Count,
            AmountBCP: sumBCP,
            AmountLK: sumLK,
            AmountTotal: totalBeforeVAT,
            AmountVAT: vatAmount,
            AmountVATTotal: totalAfterVAT,
            Details: details
        );
    }

    public async Task<List<PaymentStorageOrderDto>> ListAsync(
        string? pmtMonth = null,
        PaymentStorageStatus? status = null,
        string? paymentStorageNo = null,
        string? storageCode = null,
        string? vin = null,
        PaymentStorageSignStatus? htvSignStatus = null,
        PaymentStorageSignStatus? tcmsSignStatus = null,
        int pageIndex = 0,
        int pageSize = 50)
    {
        var q = db.PaymentStorageOrders
            .Include(o => o.Details)
            .Where(o => o.OrgId == OrgId);

        if (!string.IsNullOrWhiteSpace(pmtMonth))
        {
            var mPrefix = pmtMonth.Length >= 7 ? pmtMonth[..7] : pmtMonth;
            q = q.Where(o => o.PmtMonth.StartsWith(mPrefix));
        }
        if (status.HasValue)
            q = q.Where(o => o.Status == status.Value);
        if (!string.IsNullOrWhiteSpace(paymentStorageNo))
            q = q.Where(o => o.PaymentStorageNo.Contains(paymentStorageNo));
        if (htvSignStatus.HasValue)
            q = q.Where(o => o.HTVSignStatus == htvSignStatus.Value);
        if (tcmsSignStatus.HasValue)
            q = q.Where(o => o.TCMSSignStatus == tcmsSignStatus.Value);
        if (!string.IsNullOrWhiteSpace(storageCode))
            q = q.Where(o => o.Details.Any(d => d.StorageCode == storageCode));
        if (!string.IsNullOrWhiteSpace(vin))
            q = q.Where(o => o.Details.Any(d => d.Vin.Contains(vin)));

        var items = await q.OrderByDescending(o => o.CreatedAt)
            .Skip(pageIndex * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return items.Select(MapOrderToDto).ToList();
    }

    public async Task<PaymentStorageOrderDto?> DetailAsync(string paymentStorageNo)
    {
        var order = await db.PaymentStorageOrders
            .Include(o => o.Details)
            .FirstOrDefaultAsync(o => o.OrgId == OrgId && o.PaymentStorageNo == paymentStorageNo);
        return order is null ? null : MapOrderToDto(order);
    }

    public async Task<PaymentStorageOrderDto> CreateAsync(CreatePaymentStorageDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.PmtMonth))
            throw new InvalidOperationException("Tháng thanh toán PmtMonth không được để trống.");
        if (dto.VAT < 0)
            throw new InvalidOperationException("Thuế suất VAT không được nhỏ hơn 0.");

        var start = dto.PmtPeriodStartDTime.Date;
        var end = dto.PmtPeriodEndDTime.Date;
        if (start > end)
            throw new InvalidOperationException("Ngày bắt đầu kỳ tính phí không được sau ngày kết thúc.");

        // Kiểm tra tính liên tục của các kỳ tính (không trùng lặp, không có khoảng trống)
        var lastPeriod = await db.PaymentStorageOrders
            .Where(o => o.OrgId == OrgId && o.Status != PaymentStorageStatus.Cancelled && o.Status != PaymentStorageStatus.Rejected)
            .OrderByDescending(o => o.PmtPeriodEndDTime)
            .FirstOrDefaultAsync();

        if (lastPeriod != null && start <= lastPeriod.PmtPeriodEndDTime)
            throw new InvalidOperationException($"Kỳ thanh toán mới bị trùng lặp với kỳ trước kết thúc ngày {lastPeriod.PmtPeriodEndDTime:yyyy-MM-dd}. Ngày bắt đầu phải sau kỳ trước.");

        var no = !string.IsNullOrWhiteSpace(dto.PaymentStorageNo)
            ? dto.PaymentStorageNo.Trim()
            : await GetNextSeqAsync();

        if (await db.PaymentStorageOrders.AnyAsync(o => o.OrgId == OrgId && o.PaymentStorageNo == no))
            throw new InvalidOperationException($"Số phiếu thanh toán lưu kho '{no}' đã tồn tại.");

        var order = new PaymentStorageOrder
        {
            OrgId = OrgId,
            PaymentStorageNo = no,
            PmtMonth = dto.PmtMonth.Trim(),
            PmtPeriodStartDTime = start,
            PmtPeriodEndDTime = end,
            VAT = dto.VAT,
            Status = PaymentStorageStatus.Pending,
            HTVSignStatus = PaymentStorageSignStatus.ChuaKy,
            TCMSSignStatus = PaymentStorageSignStatus.ChuaKy,
            Remark = dto.Remark,
            CreatedBy = dto.CreatedBy ?? "SYSTEM_ADMIN",
            CreatedAt = DateTime.Now
        };

        var masterCosts = await db.InventoryCosts.Where(x => x.IsActive).ToListAsync();
        var cars = dto.Cars;
        if (cars == null || cars.Count == 0)
        {
            cars = await GetEligibleCarsAsync(start, end);
            if (cars.Count == 0)
                throw new InvalidOperationException("Không tìm thấy dòng xe nào đủ điều kiện để lập phiếu lưu kho trong kỳ này.");
        }

        decimal sumBCP = 0m, sumLK = 0m;
        foreach (var c in cars)
        {
            if (string.IsNullOrWhiteSpace(c.Vin))
                throw new InvalidOperationException("Số khung VIN không được để trống trong chi tiết.");
            if (string.IsNullOrWhiteSpace(c.RefNo))
                throw new InvalidOperationException($"Số tham chiếu RefNo không được để trống cho VIN {c.Vin}.");

            var stCode = string.IsNullOrWhiteSpace(c.StorageCode) ? "KHO_NB" : c.StorageCode;
            var bcpRate = c.PriceBCP ?? masterCosts.FirstOrDefault(x => x.StorageCode == stCode && x.CostTypeCode == "BCP" && x.ModelCode == c.ModelCode)?.UnitPrice ?? 30000m;
            var lkRate = c.PriceLK ?? masterCosts.FirstOrDefault(x => x.StorageCode == stCode && x.CostTypeCode == "LK" && x.ModelCode == c.ModelCode)?.UnitPrice ?? 45000m;

            var dBegin = c.DateBegin?.Date ?? start;
            var dEnd = c.DateEnd?.Date ?? end;
            if (dBegin < start) dBegin = start;
            if (dEnd > end) dEnd = end;

            var days = Math.Max(0, (dEnd - dBegin).Days + 1 - c.DelayTransport);
            var aBCP = c.AmountBCP ?? (days * bcpRate);
            var aLK = c.AmountLK ?? (days * lkRate);
            var aTotal = aBCP + aLK;

            sumBCP += aBCP;
            sumLK += aLK;

            order.Details.Add(new PaymentStorageDetail
            {
                PaymentStorageNo = no,
                Vin = c.Vin.Trim(),
                RefNo = c.RefNo.Trim(),
                CarId = c.CarId,
                ModelCode = c.ModelCode,
                ModelName = c.ModelName ?? c.ModelCode,
                SpecCode = c.SpecCode,
                ColorCode = c.ColorCode,
                StorageCode = stCode,
                StorageName = c.StorageName ?? stCode,
                StorageDateIn = c.StorageDateIn,
                StorageDateOut = c.StorageDateOut,
                DateBegin = dBegin,
                DateEnd = dEnd,
                DelayTransport = c.DelayTransport,
                DateCount = days,
                PriceBCP = bcpRate,
                PriceLK = lkRate,
                AmountBCP = aBCP,
                AmountLK = aLK,
                AmountTotal = aTotal,
                DealerCode = c.DealerCode,
                DealerName = c.DealerName,
                PackingListNo = c.PackingListNo,
                DeliveryOrderNo = c.DeliveryOrderNo,
                StorageRearrangeNoIn = c.StorageRearrangeNoIn,
                StorageRearrangeNoOut = c.StorageRearrangeNoOut,
                RetrieveOrderNo = c.RetrieveOrderNo,
                InvoiceFactoryDate = c.InvoiceFactoryDate,
                Remark = c.Remark
            });
        }

        order.TotalCars = order.Details.Count;
        order.AmountBCP = sumBCP;
        order.AmountLK = sumLK;
        order.AmountTotal = sumBCP + sumLK;
        order.AmountVAT = Math.Round(order.AmountTotal * order.VAT / 100m, 0);
        order.AmountVATTotal = order.AmountTotal + order.AmountVAT;

        db.PaymentStorageOrders.Add(order);
        await db.SaveChangesAsync();

        return MapOrderToDto(order);
    }

    public async Task<PaymentStorageOrderDto?> UpdateAsync(string paymentStorageNo, UpdatePaymentStorageDto dto)
    {
        var order = await db.PaymentStorageOrders
            .Include(o => o.Details)
            .FirstOrDefaultAsync(o => o.OrgId == OrgId && o.PaymentStorageNo == paymentStorageNo);

        if (order == null) return null;

        if (order.Status != PaymentStorageStatus.Pending)
            throw new InvalidOperationException($"Chỉ có thể cập nhật phiếu ở trạng thái Chờ duyệt (Pending). Trạng thái hiện tại: {order.Status}");

        if (dto.VAT.HasValue)
        {
            if (dto.VAT.Value < 0) throw new InvalidOperationException("Thuế suất VAT không được nhỏ hơn 0.");
            order.VAT = dto.VAT.Value;
        }

        if (dto.Remark != null) order.Remark = dto.Remark;
        order.LogLUDateTime = DateTime.Now;
        order.LogLUBy = dto.UpdatedBy ?? "HTC_ACCOUNTANT";

        if (dto.Cars != null && dto.Cars.Count > 0)
        {
            var masterCosts = await db.InventoryCosts.Where(x => x.IsActive).ToListAsync();
            order.Details.Clear();

            decimal sumBCP = 0m, sumLK = 0m;
            foreach (var c in dto.Cars)
            {
                var stCode = string.IsNullOrWhiteSpace(c.StorageCode) ? "KHO_NB" : c.StorageCode;
                var bcpRate = c.PriceBCP ?? masterCosts.FirstOrDefault(x => x.StorageCode == stCode && x.CostTypeCode == "BCP" && x.ModelCode == c.ModelCode)?.UnitPrice ?? 30000m;
                var lkRate = c.PriceLK ?? masterCosts.FirstOrDefault(x => x.StorageCode == stCode && x.CostTypeCode == "LK" && x.ModelCode == c.ModelCode)?.UnitPrice ?? 45000m;

                var dBegin = c.DateBegin?.Date ?? order.PmtPeriodStartDTime.Date;
                var dEnd = c.DateEnd?.Date ?? order.PmtPeriodEndDTime.Date;
                var days = Math.Max(0, (dEnd - dBegin).Days + 1 - c.DelayTransport);
                var aBCP = c.AmountBCP ?? (days * bcpRate);
                var aLK = c.AmountLK ?? (days * lkRate);
                var aTotal = aBCP + aLK;

                sumBCP += aBCP;
                sumLK += aLK;

                order.Details.Add(new PaymentStorageDetail
                {
                    PaymentStorageNo = paymentStorageNo,
                    Vin = c.Vin.Trim(),
                    RefNo = c.RefNo.Trim(),
                    CarId = c.CarId,
                    ModelCode = c.ModelCode,
                    ModelName = c.ModelName ?? c.ModelCode,
                    SpecCode = c.SpecCode,
                    ColorCode = c.ColorCode,
                    StorageCode = stCode,
                    StorageName = c.StorageName ?? stCode,
                    StorageDateIn = c.StorageDateIn,
                    StorageDateOut = c.StorageDateOut,
                    DateBegin = dBegin,
                    DateEnd = dEnd,
                    DelayTransport = c.DelayTransport,
                    DateCount = days,
                    PriceBCP = bcpRate,
                    PriceLK = lkRate,
                    AmountBCP = aBCP,
                    AmountLK = aLK,
                    AmountTotal = aTotal,
                    DealerCode = c.DealerCode,
                    DealerName = c.DealerName,
                    PackingListNo = c.PackingListNo,
                    DeliveryOrderNo = c.DeliveryOrderNo,
                    StorageRearrangeNoIn = c.StorageRearrangeNoIn,
                    StorageRearrangeNoOut = c.StorageRearrangeNoOut,
                    RetrieveOrderNo = c.RetrieveOrderNo,
                    InvoiceFactoryDate = c.InvoiceFactoryDate,
                    Remark = c.Remark
                });
            }

            order.TotalCars = order.Details.Count;
            order.AmountBCP = sumBCP;
            order.AmountLK = sumLK;
            order.AmountTotal = sumBCP + sumLK;
        }

        order.AmountVAT = Math.Round(order.AmountTotal * order.VAT / 100m, 0);
        order.AmountVATTotal = order.AmountTotal + order.AmountVAT;

        await db.SaveChangesAsync();
        return MapOrderToDto(order);
    }

    public async Task<PaymentStorageOrderDto?> AddCarsAsync(string paymentStorageNo, AddPaymentStorageCarsDto dto)
    {
        var order = await db.PaymentStorageOrders
            .Include(o => o.Details)
            .FirstOrDefaultAsync(o => o.OrgId == OrgId && o.PaymentStorageNo == paymentStorageNo);

        if (order == null) return null;

        if (order.Status != PaymentStorageStatus.Pending)
            throw new InvalidOperationException($"Chỉ có thể thêm xe vào phiếu ở trạng thái Chờ duyệt (Pending). Trạng thái hiện tại: {order.Status}");

        var masterCosts = await db.InventoryCosts.Where(x => x.IsActive).ToListAsync();

        foreach (var c in dto.Cars)
        {
            if (order.Details.Any(d => d.Vin == c.Vin))
                throw new InvalidOperationException($"Số khung VIN '{c.Vin}' đã tồn tại trong phiếu này.");

            var stCode = string.IsNullOrWhiteSpace(c.StorageCode) ? "KHO_NB" : c.StorageCode;
            var bcpRate = c.PriceBCP ?? masterCosts.FirstOrDefault(x => x.StorageCode == stCode && x.CostTypeCode == "BCP" && x.ModelCode == c.ModelCode)?.UnitPrice ?? 30000m;
            var lkRate = c.PriceLK ?? masterCosts.FirstOrDefault(x => x.StorageCode == stCode && x.CostTypeCode == "LK" && x.ModelCode == c.ModelCode)?.UnitPrice ?? 45000m;

            var dBegin = c.DateBegin?.Date ?? order.PmtPeriodStartDTime.Date;
            var dEnd = c.DateEnd?.Date ?? order.PmtPeriodEndDTime.Date;
            var days = Math.Max(0, (dEnd - dBegin).Days + 1 - c.DelayTransport);
            var aBCP = c.AmountBCP ?? (days * bcpRate);
            var aLK = c.AmountLK ?? (days * lkRate);
            var aTotal = aBCP + aLK;

            order.Details.Add(new PaymentStorageDetail
            {
                PaymentStorageNo = paymentStorageNo,
                Vin = c.Vin.Trim(),
                RefNo = c.RefNo.Trim(),
                CarId = c.CarId,
                ModelCode = c.ModelCode,
                ModelName = c.ModelName ?? c.ModelCode,
                SpecCode = c.SpecCode,
                ColorCode = c.ColorCode,
                StorageCode = stCode,
                StorageName = c.StorageName ?? stCode,
                StorageDateIn = c.StorageDateIn,
                StorageDateOut = c.StorageDateOut,
                DateBegin = dBegin,
                DateEnd = dEnd,
                DelayTransport = c.DelayTransport,
                DateCount = days,
                PriceBCP = bcpRate,
                PriceLK = lkRate,
                AmountBCP = aBCP,
                AmountLK = aLK,
                AmountTotal = aTotal,
                DealerCode = c.DealerCode,
                DealerName = c.DealerName,
                PackingListNo = c.PackingListNo,
                DeliveryOrderNo = c.DeliveryOrderNo,
                StorageRearrangeNoIn = c.StorageRearrangeNoIn,
                StorageRearrangeNoOut = c.StorageRearrangeNoOut,
                RetrieveOrderNo = c.RetrieveOrderNo,
                InvoiceFactoryDate = c.InvoiceFactoryDate,
                Remark = c.Remark
            });
        }

        RecalculateTotals(order);
        order.LogLUDateTime = DateTime.Now;
        order.LogLUBy = dto.UpdatedBy ?? "HTC_ACCOUNTANT";

        await db.SaveChangesAsync();
        return MapOrderToDto(order);
    }

    public async Task<PaymentStorageOrderDto?> RemoveCarAsync(string paymentStorageNo, string vin)
    {
        var order = await db.PaymentStorageOrders
            .Include(o => o.Details)
            .FirstOrDefaultAsync(o => o.OrgId == OrgId && o.PaymentStorageNo == paymentStorageNo);

        if (order == null) return null;

        if (order.Status != PaymentStorageStatus.Pending)
            throw new InvalidOperationException($"Chỉ có thể xóa xe khỏi phiếu ở trạng thái Chờ duyệt (Pending). Trạng thái hiện tại: {order.Status}");

        var detail = order.Details.FirstOrDefault(d => d.Vin == vin);
        if (detail == null)
            throw new InvalidOperationException($"Không tìm thấy xe VIN '{vin}' trong phiếu {paymentStorageNo}.");

        order.Details.Remove(detail);
        if (order.Details.Count == 0)
            throw new InvalidOperationException("Không thể xóa dòng xe cuối cùng của phiếu.");

        RecalculateTotals(order);
        order.LogLUDateTime = DateTime.Now;

        await db.SaveChangesAsync();
        return MapOrderToDto(order);
    }

    public async Task<PaymentStorageOrderDto?> Approve1Async(string paymentStorageNo, Approve1PaymentStorageDto? dto)
    {
        var order = await db.PaymentStorageOrders
            .Include(o => o.Details)
            .FirstOrDefaultAsync(o => o.OrgId == OrgId && o.PaymentStorageNo == paymentStorageNo);

        if (order == null) return null;

        if (order.Status != PaymentStorageStatus.Pending)
            throw new InvalidOperationException($"Chỉ có thể duyệt cấp 1 phiếu ở trạng thái Chờ duyệt (Pending). Trạng thái hiện tại: {order.Status}");

        order.Status = PaymentStorageStatus.Approved1;
        order.Approve1At = DateTime.Now;
        order.Approve1By = dto?.Approved1By ?? "TP_KHO_VAN_HTC";
        if (!string.IsNullOrWhiteSpace(dto?.Remark))
            order.Remark = (order.Remark == null ? "" : order.Remark + " | ") + $"Duyệt cấp 1: {dto.Remark}";

        await db.SaveChangesAsync();
        return MapOrderToDto(order);
    }

    public async Task<PaymentStorageOrderDto?> Approve2Async(string paymentStorageNo, Approve2PaymentStorageDto? dto)
    {
        var order = await db.PaymentStorageOrders
            .Include(o => o.Details)
            .FirstOrDefaultAsync(o => o.OrgId == OrgId && o.PaymentStorageNo == paymentStorageNo);

        if (order == null) return null;

        if (order.Status != PaymentStorageStatus.Approved1)
            throw new InvalidOperationException($"Chỉ có thể duyệt cấp 2 phiếu ở trạng thái Đã duyệt cấp 1 (Approved1). Trạng thái hiện tại: {order.Status}");

        order.Status = PaymentStorageStatus.Approved2;
        order.Approve2At = DateTime.Now;
        order.Approve2By = dto?.Approved2By ?? "LANH_DAO_HTC";
        if (!string.IsNullOrWhiteSpace(dto?.Remark))
            order.Remark = (order.Remark == null ? "" : order.Remark + " | ") + $"Duyệt cấp 2: {dto.Remark}";

        await db.SaveChangesAsync();
        return MapOrderToDto(order);
    }

    public async Task<List<string>> BatchApprove2Async(BatchApprove2PaymentStorageDto dto)
    {
        var results = new List<string>();
        foreach (var no in dto.PaymentStorageNos)
        {
            var order = await db.PaymentStorageOrders
                .FirstOrDefaultAsync(o => o.OrgId == OrgId && o.PaymentStorageNo == no && o.Status == PaymentStorageStatus.Approved1);
            if (order != null)
            {
                order.Status = PaymentStorageStatus.Approved2;
                order.Approve2At = DateTime.Now;
                order.Approve2By = dto.Approved2By ?? "LANH_DAO_HTC";
                results.Add(no);
            }
        }
        await db.SaveChangesAsync();
        return results;
    }

    public async Task<PaymentStorageOrderDto?> TCMSESignAsync(string paymentStorageNo, TCMSSignPaymentStorageDto dto)
    {
        var order = await db.PaymentStorageOrders
            .Include(o => o.Details)
            .FirstOrDefaultAsync(o => o.OrgId == OrgId && o.PaymentStorageNo == paymentStorageNo);

        if (order == null) return null;

        if (order.Status != PaymentStorageStatus.Approved2 && order.Status != PaymentStorageStatus.Finished)
            throw new InvalidOperationException($"TCMS chỉ có thể ký số điện tử khi phiếu đã được Lãnh đạo HTC duyệt cấp 2 (Approved2). Trạng thái hiện tại: {order.Status}");

        order.TCMSSignStatus = PaymentStorageSignStatus.DaKy;
        order.TCMSSignDTime = DateTime.Now;
        order.TCMSSignBy = dto.SignedBy ?? "GD_TCMS_ESIGN";
        if (!string.IsNullOrWhiteSpace(dto.EContractFileName))
            order.FilePath = $"/esign/tcms/{paymentStorageNo}_{dto.EContractFileName}";

        if (order.HTVSignStatus == PaymentStorageSignStatus.DaKy)
            order.Status = PaymentStorageStatus.Finished;

        order.LogLUDateTime = DateTime.Now;
        order.LogLUBy = order.TCMSSignBy;

        await db.SaveChangesAsync();
        return MapOrderToDto(order);
    }

    public async Task<PaymentStorageOrderDto?> HTVESignAsync(string paymentStorageNo, HTVESignPaymentStorageDto dto)
    {
        var order = await db.PaymentStorageOrders
            .Include(o => o.Details)
            .FirstOrDefaultAsync(o => o.OrgId == OrgId && o.PaymentStorageNo == paymentStorageNo);

        if (order == null) return null;

        if (order.Status != PaymentStorageStatus.Approved2 && order.Status != PaymentStorageStatus.Finished)
            throw new InvalidOperationException($"HTV chỉ có thể ký số điện tử khi phiếu ở trạng thái Đã duyệt cấp 2 (Approved2). Trạng thái hiện tại: {order.Status}");

        order.HTVSignStatus = PaymentStorageSignStatus.DaKy;
        order.HTVSignDTime = DateTime.Now;
        order.HTVSignBy = dto.SignedBy ?? "TGĐ_HTV_ESIGN";
        order.FilePath = dto.FilePath ?? $"/esign/htv/{paymentStorageNo}_contract.pdf";

        if (order.TCMSSignStatus == PaymentStorageSignStatus.DaKy)
            order.Status = PaymentStorageStatus.Finished;

        order.LogLUDateTime = DateTime.Now;
        order.LogLUBy = order.HTVSignBy;

        await db.SaveChangesAsync();
        return MapOrderToDto(order);
    }

    public async Task<PaymentStorageOrderDto?> UploadInvoiceAsync(string paymentStorageNo, UploadPaymentStorageInvoiceDto dto)
    {
        var order = await db.PaymentStorageOrders
            .Include(o => o.Details)
            .FirstOrDefaultAsync(o => o.OrgId == OrgId && o.PaymentStorageNo == paymentStorageNo);

        if (order == null) return null;

        order.FileInvPath = dto.FileInvoiceName;
        order.FileInvUrl = dto.FileInvoiceUrl ?? $"/invoices/storage/{dto.FileInvoiceName}";
        if (!string.IsNullOrWhiteSpace(dto.InvoiceNo))
            order.InvoiceNo = dto.InvoiceNo;
        if (dto.InvoiceDate.HasValue)
            order.InvoiceDate = dto.InvoiceDate.Value;

        order.LogLUDateTime = DateTime.Now;
        order.LogLUBy = dto.UploadedBy ?? "TCMS_ACCOUNTANT";

        await db.SaveChangesAsync();
        return MapOrderToDto(order);
    }

    public async Task<PaymentStorageOrderDto?> RejectAsync(string paymentStorageNo, RejectPaymentStorageDto dto)
    {
        var order = await db.PaymentStorageOrders
            .Include(o => o.Details)
            .FirstOrDefaultAsync(o => o.OrgId == OrgId && o.PaymentStorageNo == paymentStorageNo);

        if (order == null) return null;

        if (order.Status != PaymentStorageStatus.Pending && order.Status != PaymentStorageStatus.Approved1)
            throw new InvalidOperationException($"Không thể từ chối phiếu ở trạng thái {order.Status}. Chỉ từ chối khi đang chờ duyệt.");

        order.Status = PaymentStorageStatus.Rejected;
        order.RejectReason = dto.Reason;
        order.RejectedAt = DateTime.Now;
        order.RejectedBy = dto.RejectedBy ?? "HTC_AUDITOR";

        await db.SaveChangesAsync();
        return MapOrderToDto(order);
    }

    public async Task<PaymentStorageOrderDto?> CancelAsync(string paymentStorageNo, CancelPaymentStorageDto dto)
    {
        var order = await db.PaymentStorageOrders
            .Include(o => o.Details)
            .FirstOrDefaultAsync(o => o.OrgId == OrgId && o.PaymentStorageNo == paymentStorageNo);

        if (order == null) return null;

        if (order.Status == PaymentStorageStatus.Finished)
            throw new InvalidOperationException("Không thể hủy phiếu thanh toán lưu kho đã ký số hoàn tất quyết toán (Finished).");

        order.Status = PaymentStorageStatus.Cancelled;
        order.CancelReason = dto.Reason;
        order.CancelledAt = DateTime.Now;
        order.CancelledBy = dto.CancelledBy ?? "HTC_ADMIN";

        await db.SaveChangesAsync();
        return MapOrderToDto(order);
    }

    public async Task<bool> DeleteDraftAsync(string paymentStorageNo)
    {
        var order = await db.PaymentStorageOrders
            .Include(o => o.Details)
            .FirstOrDefaultAsync(o => o.OrgId == OrgId && o.PaymentStorageNo == paymentStorageNo);

        if (order == null) return false;

        if (order.Status != PaymentStorageStatus.Pending && order.Status != PaymentStorageStatus.Rejected && order.Status != PaymentStorageStatus.Cancelled)
            throw new InvalidOperationException($"Chỉ được xóa phiếu ở trạng thái Chờ duyệt (Pending), Từ chối (Rejected) hoặc Đã hủy (Cancelled). Trạng thái hiện tại: {order.Status}");

        db.PaymentStorageOrders.Remove(order);
        await db.SaveChangesAsync();
        return true;
    }

    public async Task<PaymentStoragePrintDto?> GetPrintDataAsync(string paymentStorageNo)
    {
        var orderDto = await DetailAsync(paymentStorageNo);
        if (orderDto == null) return null;

        var modelSummary = orderDto.Details
            .GroupBy(d => d.ModelName)
            .ToDictionary(g => g.Key, g => g.Count());

        var storageSummary = orderDto.Details
            .GroupBy(d => d.StorageName)
            .ToDictionary(g => g.Key, g => g.Sum(d => d.AmountTotal));

        return new PaymentStoragePrintDto(orderDto, modelSummary, storageSummary);
    }

    public async Task<PaymentStorageStatsDto> GetStatsAsync(string? pmtMonth = null)
    {
        var q = db.PaymentStorageOrders.Where(o => o.OrgId == OrgId);
        if (!string.IsNullOrWhiteSpace(pmtMonth))
        {
            var mPrefix = pmtMonth.Length >= 7 ? pmtMonth[..7] : pmtMonth;
            q = q.Where(o => o.PmtMonth.StartsWith(mPrefix));
        }

        var orders = await q.ToListAsync();

        return new PaymentStorageStatsDto(
            TotalOrders: orders.Count,
            PendingOrders: orders.Count(o => o.Status == PaymentStorageStatus.Pending),
            Approved1Orders: orders.Count(o => o.Status == PaymentStorageStatus.Approved1),
            Approved2Orders: orders.Count(o => o.Status == PaymentStorageStatus.Approved2),
            FinishedOrders: orders.Count(o => o.Status == PaymentStorageStatus.Finished),
            RejectedOrders: orders.Count(o => o.Status == PaymentStorageStatus.Rejected),
            CancelledOrders: orders.Count(o => o.Status == PaymentStorageStatus.Cancelled),
            TotalAmountBeforeVAT: orders.Where(o => o.Status != PaymentStorageStatus.Cancelled && o.Status != PaymentStorageStatus.Rejected).Sum(o => o.AmountTotal),
            TotalAmountVAT: orders.Where(o => o.Status != PaymentStorageStatus.Cancelled && o.Status != PaymentStorageStatus.Rejected).Sum(o => o.AmountVAT),
            TotalAmountAfterVAT: orders.Where(o => o.Status != PaymentStorageStatus.Cancelled && o.Status != PaymentStorageStatus.Rejected).Sum(o => o.AmountVATTotal),
            TotalBCPAmount: orders.Where(o => o.Status != PaymentStorageStatus.Cancelled && o.Status != PaymentStorageStatus.Rejected).Sum(o => o.AmountBCP),
            TotalLKAmount: orders.Where(o => o.Status != PaymentStorageStatus.Cancelled && o.Status != PaymentStorageStatus.Rejected).Sum(o => o.AmountLK),
            TotalCarsCount: orders.Where(o => o.Status != PaymentStorageStatus.Cancelled && o.Status != PaymentStorageStatus.Rejected).Sum(o => o.TotalCars)
        );
    }

    private static void RecalculateTotals(PaymentStorageOrder order)
    {
        order.TotalCars = order.Details.Count;
        order.AmountBCP = order.Details.Sum(d => d.AmountBCP);
        order.AmountLK = order.Details.Sum(d => d.AmountLK);
        order.AmountTotal = order.Details.Sum(d => d.AmountTotal);
        order.AmountVAT = Math.Round(order.AmountTotal * order.VAT / 100m, 0);
        order.AmountVATTotal = order.AmountTotal + order.AmountVAT;
    }

    private static PaymentStorageOrderDto MapOrderToDto(PaymentStorageOrder o)
    {
        return new PaymentStorageOrderDto(
            Id: o.Id,
            PaymentStorageNo: o.PaymentStorageNo,
            PmtMonth: o.PmtMonth,
            PmtPeriodStartDTime: o.PmtPeriodStartDTime,
            PmtPeriodEndDTime: o.PmtPeriodEndDTime,
            VAT: o.VAT,
            TotalCars: o.TotalCars,
            AmountBCP: o.AmountBCP,
            AmountLK: o.AmountLK,
            AmountTotal: o.AmountTotal,
            AmountVAT: o.AmountVAT,
            AmountVATTotal: o.AmountVATTotal,
            Status: o.Status,
            StatusName: o.Status switch
            {
                PaymentStorageStatus.Pending => "Chờ duyệt cấp 1",
                PaymentStorageStatus.Approved1 => "Đã duyệt cấp 1 (Trưởng phòng)",
                PaymentStorageStatus.Approved2 => "Đã duyệt cấp 2 (Lãnh đạo)",
                PaymentStorageStatus.Finished => "Quyết toán hoàn tất (Đã ký số)",
                PaymentStorageStatus.Rejected => "Từ chối duyệt",
                PaymentStorageStatus.Cancelled => "Đã hủy",
                _ => o.Status.ToString()
            },
            HTVSignStatus: o.HTVSignStatus,
            HTVSignDTime: o.HTVSignDTime,
            HTVSignBy: o.HTVSignBy,
            TCMSSignStatus: o.TCMSSignStatus,
            TCMSSignDTime: o.TCMSSignDTime,
            TCMSSignBy: o.TCMSSignBy,
            FilePath: o.FilePath,
            FileUrl: o.FileUrl ?? o.FilePath,
            FileInvPath: o.FileInvPath,
            FileInvUrl: o.FileInvUrl,
            InvoiceNo: o.InvoiceNo,
            InvoiceDate: o.InvoiceDate,
            Remark: o.Remark,
            RejectReason: o.RejectReason,
            CancelReason: o.CancelReason,
            CreatedAt: o.CreatedAt,
            CreatedBy: o.CreatedBy,
            Approve1At: o.Approve1At,
            Approve1By: o.Approve1By,
            Approve2At: o.Approve2At,
            Approve2By: o.Approve2By,
            RejectedAt: o.RejectedAt,
            RejectedBy: o.RejectedBy,
            CancelledAt: o.CancelledAt,
            CancelledBy: o.CancelledBy,
            Details: o.Details.Select(d => new PaymentStorageDetailItemDto(
                Id: d.Id,
                PaymentStorageNo: d.PaymentStorageNo,
                Vin: d.Vin,
                RefNo: d.RefNo,
                CarId: d.CarId,
                ModelCode: d.ModelCode,
                ModelName: d.ModelName,
                SpecCode: d.SpecCode,
                ColorCode: d.ColorCode,
                StorageCode: d.StorageCode,
                StorageName: d.StorageName ?? d.StorageCode,
                StorageDateIn: d.StorageDateIn,
                StorageDateOut: d.StorageDateOut,
                DateBegin: d.DateBegin,
                DateEnd: d.DateEnd,
                DelayTransport: d.DelayTransport,
                DateCount: d.DateCount,
                PriceBCP: d.PriceBCP,
                PriceLK: d.PriceLK,
                AmountBCP: d.AmountBCP,
                AmountLK: d.AmountLK,
                AmountTotal: d.AmountTotal,
                DealerCode: d.DealerCode,
                DealerName: d.DealerName,
                PackingListNo: d.PackingListNo,
                DeliveryOrderNo: d.DeliveryOrderNo,
                StorageRearrangeNoIn: d.StorageRearrangeNoIn,
                StorageRearrangeNoOut: d.StorageRearrangeNoOut,
                RetrieveOrderNo: d.RetrieveOrderNo,
                InvoiceFactoryDate: d.InvoiceFactoryDate,
                Remark: d.Remark
            )).ToList()
        );
    }
}
