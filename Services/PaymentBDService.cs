using Microsoft.EntityFrameworkCore;
using MiniSales.Data;
using MiniSales.Models;

namespace MiniSales.Services;

public record PaymentBDCarItemDto(
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
    string? InvoiceFactoryDate = null,
    DateTime? LatePmtBDDate = null,
    DateTime? BDDate1 = null,
    DateTime? BDDate2 = null,
    int BDCount = 0,
    decimal? UnitPrice = null,
    decimal? BDAmount = null,
    string? DealerCode = null,
    string? DealerName = null,
    string? PackingListNo = null,
    string? DeliveryOrderNo = null,
    string? StorageRearrangeNoIn = null,
    string? StorageRearrangeNoOut = null,
    string? RetrieveOrderNo = null,
    string? Remark = null
);

public record CreatePaymentBDDto(
    string PmtMonth,
    DateTime PmtPeriodStartDTime,
    DateTime PmtPeriodEndDTime,
    string? PaymentBDNo = null,
    decimal VAT = 10m,
    string? Remark = null,
    string? CreatedBy = null,
    List<PaymentBDCarItemDto>? Cars = null
);

public record UpdatePaymentBDDto(
    decimal? VAT = null,
    string? Remark = null,
    string? UpdatedBy = null,
    List<PaymentBDCarItemDto>? Cars = null
);

public record AddPaymentBDCarsDto(
    List<PaymentBDCarItemDto> Cars,
    string? UpdatedBy = null
);

public record Approve1PaymentBDDto(
    string? Approved1By = null,
    string? Remark = null
);

public record Approve2PaymentBDDto(
    string? Approved2By = null,
    string? Remark = null
);

public record BatchApprove2PaymentBDDto(
    List<string> PaymentBDNos,
    string? Approved2By = null
);

public record TCMSSignPaymentBDDto(
    string? SignedBy = null,
    string? EContractFileName = null,
    string? Remark = null
);

public record HTVESignPaymentBDDto(
    string? SignedBy = null,
    string? EContractFileName = null,
    string? FilePath = null,
    string? Remark = null
);

public record RejectPaymentBDDto(
    string Reason,
    string? RejectedBy = null
);

public record CancelPaymentBDDto(
    string Reason,
    string? CancelledBy = null
);

public record PaymentBDPreviewRequestDto(
    string PmtMonth,
    DateTime PmtPeriodStartDTime,
    DateTime PmtPeriodEndDTime,
    decimal VAT = 10m,
    string? StorageCode = null,
    List<PaymentBDCarItemDto>? Cars = null
);

public record PaymentBDDetailItemDto(
    long Id,
    string PaymentBDNo,
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
    string? InvoiceFactoryDate,
    DateTime? LatePmtBDDate,
    DateTime? BDDate1,
    DateTime? BDDate2,
    int BDCount,
    decimal UnitPrice,
    decimal BDAmount,
    decimal AmountTotal,
    string? DealerCode,
    string? DealerName,
    string? PackingListNo,
    string? DeliveryOrderNo,
    string? StorageRearrangeNoIn,
    string? StorageRearrangeNoOut,
    string? RetrieveOrderNo,
    string? Remark
);

public record PaymentBDOrderDto(
    long Id,
    string PaymentBDNo,
    string PmtMonth,
    DateTime PmtPeriodStartDTime,
    DateTime PmtPeriodEndDTime,
    decimal VAT,
    int TotalCars,
    int TotalBDCount,
    decimal AmountTotal,
    decimal AmountVAT,
    decimal AmountVATTotal,
    PaymentBDStatus Status,
    string StatusName,
    PaymentBDSignStatus HTVSignStatus,
    DateTime? HTVSignDTime,
    string? HTVSignBy,
    PaymentBDSignStatus TCMSSignStatus,
    DateTime? TCMSSignDTime,
    string? TCMSSignBy,
    string? FilePath,
    string? FileUrl,
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
    List<PaymentBDDetailItemDto> Details
);

public record PaymentBDPreviewResponseDto(
    string PmtMonth,
    DateTime PmtPeriodStartDTime,
    DateTime PmtPeriodEndDTime,
    decimal VAT,
    int TotalCars,
    int TotalBDCount,
    decimal AmountTotal,
    decimal AmountVAT,
    decimal AmountVATTotal,
    List<PaymentBDDetailItemDto> Details
);

public record PaymentBDStatsDto(
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
    int TotalCarsCount,
    int TotalBDCount
);

public record PaymentBDPrintDto(
    PaymentBDOrderDto Order,
    Dictionary<string, int> ModelSummary,
    Dictionary<string, decimal> StorageSummary
);

public interface IPaymentBDService
{
    Task<string> GetNextSeqAsync();
    Task<List<InventoryCostMaster>> GetMasterCostsAsync(string? storageCode = null);
    Task<List<PaymentBDCarItemDto>> GetEligibleCarsAsync(DateTime pmtPeriodStart, DateTime pmtPeriodEnd, string? storageCode = null, string? modelCode = null);
    Task<PaymentBDPreviewResponseDto> PreviewCalculationAsync(PaymentBDPreviewRequestDto dto);
    Task<List<PaymentBDOrderDto>> ListAsync(string? pmtMonth = null, PaymentBDStatus? status = null, string? paymentBDNo = null, string? storageCode = null, string? vin = null, PaymentBDSignStatus? htvSignStatus = null, PaymentBDSignStatus? tcmsSignStatus = null, int pageIndex = 0, int pageSize = 50);
    Task<PaymentBDOrderDto?> DetailAsync(string paymentBDNo);
    Task<PaymentBDOrderDto> CreateAsync(CreatePaymentBDDto dto);
    Task<PaymentBDOrderDto?> UpdateAsync(string paymentBDNo, UpdatePaymentBDDto dto);
    Task<PaymentBDOrderDto?> AddCarsAsync(string paymentBDNo, AddPaymentBDCarsDto dto);
    Task<PaymentBDOrderDto?> RemoveCarAsync(string paymentBDNo, string vin);
    Task<PaymentBDOrderDto?> Approve1Async(string paymentBDNo, Approve1PaymentBDDto? dto);
    Task<PaymentBDOrderDto?> Approve2Async(string paymentBDNo, Approve2PaymentBDDto? dto);
    Task<List<string>> BatchApprove2Async(BatchApprove2PaymentBDDto dto);
    Task<PaymentBDOrderDto?> TCMSESignAsync(string paymentBDNo, TCMSSignPaymentBDDto dto);
    Task<PaymentBDOrderDto?> HTVESignAsync(string paymentBDNo, HTVESignPaymentBDDto dto);
    Task<PaymentBDOrderDto?> RejectAsync(string paymentBDNo, RejectPaymentBDDto dto);
    Task<PaymentBDOrderDto?> CancelAsync(string paymentBDNo, CancelPaymentBDDto dto);
    Task<bool> DeleteDraftAsync(string paymentBDNo);
    Task<PaymentBDPrintDto?> GetPrintDataAsync(string paymentBDNo);
    Task<PaymentBDStatsDto> GetStatsAsync(string? pmtMonth = null);
}

public class PaymentBDService(AppDbContext db, ITenantContext tenant) : IPaymentBDService
{
    private Guid OrgId => tenant.OrgId;

    // Mốc bảo dưỡng định kỳ: lần 1 sau 15 ngày lưu kho, lần 2 sau 30 ngày lưu kho.
    private const int FirstBDDays = 15;
    private const int SecondBDDays = 30;

    public async Task<string> GetNextSeqAsync()
    {
        var prefix = DateTime.Now.ToString("yyMM") + "PBD";
        var count = await db.PaymentBDOrders
            .Where(x => x.OrgId == OrgId && x.PaymentBDNo.StartsWith(prefix))
            .CountAsync();
        return $"{prefix}{(count + 1):D5}";
    }

    public async Task<List<InventoryCostMaster>> GetMasterCostsAsync(string? storageCode = null)
    {
        var q = db.InventoryCosts.Where(x => x.IsActive && x.CostTypeCode == "BD");
        if (!string.IsNullOrWhiteSpace(storageCode))
            q = q.Where(x => x.StorageCode == storageCode);
        return await q.OrderBy(x => x.StorageCode).ThenBy(x => x.ModelCode).ToListAsync();
    }

    public async Task<List<PaymentBDCarItemDto>> GetEligibleCarsAsync(DateTime pmtPeriodStart, DateTime pmtPeriodEnd, string? storageCode = null, string? modelCode = null)
    {
        var start = pmtPeriodStart.Date;
        var end = pmtPeriodEnd.Date;

        var existingVinsInPeriod = await db.PaymentBDDetails
            .Where(d => db.PaymentBDOrders.Any(o => o.Id == d.PaymentBDId && o.OrgId == OrgId &&
                o.Status != PaymentBDStatus.Cancelled && o.Status != PaymentBDStatus.Rejected &&
                o.PmtPeriodStartDTime <= pmtPeriodEnd && o.PmtPeriodEndDTime >= pmtPeriodStart))
            .Select(d => d.Vin)
            .Distinct()
            .ToListAsync();

        var masterCosts = await db.InventoryCosts.Where(x => x.IsActive && x.CostTypeCode == "BD").ToListAsync();

        var q = db.CarVinInventories.Where(x => x.OrgId == OrgId && !existingVinsInPeriod.Contains(x.Vin));
        if (!string.IsNullOrWhiteSpace(storageCode))
            q = q.Where(x => x.StorageCode == storageCode);
        if (!string.IsNullOrWhiteSpace(modelCode))
            q = q.Where(x => x.ModelCode == modelCode);

        var vins = await q.Take(50).ToListAsync();
        var result = new List<PaymentBDCarItemDto>();

        int refIdx = 1;
        foreach (var v in vins)
        {
            var stCode = string.IsNullOrWhiteSpace(v.StorageCode) ? "KHO_NB" : v.StorageCode;
            var unitPrice = masterCosts.FirstOrDefault(c => c.StorageCode == stCode && c.ModelCode == v.ModelCode)?.UnitPrice ?? 500000m;

            // Ngày bắt đầu tính bảo dưỡng: ngày nhập kho (hoặc ngày hóa đơn nhà máy nếu muộn hơn).
            var dateBegin = v.CreatedAt.Date;
            if (dateBegin < start) dateBegin = start;

            var dateCount = Math.Max(0, (end - dateBegin).Days);
            var bdDate1 = dateCount >= FirstBDDays ? dateBegin.AddDays(FirstBDDays) : (DateTime?)null;
            var bdDate2 = dateCount >= SecondBDDays ? dateBegin.AddDays(SecondBDDays) : (DateTime?)null;
            var bdCount = bdDate1 == null ? 0 : (bdDate2 == null ? 1 : 2);
            var bdAmount = bdCount * unitPrice;

            result.Add(new PaymentBDCarItemDto(
                Vin: v.Vin,
                RefNo: $"REF-{DateTime.Now:yyMM}{refIdx++:D4}",
                CarId: $"CAR-{v.Vin[..Math.Min(8, v.Vin.Length)]}",
                ModelCode: v.ModelCode,
                ModelName: v.ModelName,
                SpecCode: v.SpecCode,
                ColorCode: v.ColorCode,
                StorageCode: stCode,
                StorageName: v.StorageName ?? (stCode == "KHO_NB" ? "Kho Nhà máy Ninh Bình" : stCode),
                StorageDateIn: v.CreatedAt.Date,
                StorageDateOut: null,
                InvoiceFactoryDate: null,
                LatePmtBDDate: null,
                BDDate1: bdDate1,
                BDDate2: bdDate2,
                BDCount: bdCount,
                UnitPrice: unitPrice,
                BDAmount: bdAmount,
                DealerCode: "VN001",
                DealerName: "Hyundai Đông Đô",
                Remark: "Xe tồn bãi đủ điều kiện tính chi phí bảo dưỡng kỳ này"
            ));
        }

        return result;
    }

    public async Task<PaymentBDPreviewResponseDto> PreviewCalculationAsync(PaymentBDPreviewRequestDto dto)
    {
        var start = dto.PmtPeriodStartDTime.Date;
        var end = dto.PmtPeriodEndDTime.Date;
        if (start > end)
            throw new InvalidOperationException("Ngày bắt đầu kỳ tính phí không được sau ngày kết thúc.");

        var masterCosts = await db.InventoryCosts.Where(x => x.IsActive && x.CostTypeCode == "BD").ToListAsync();
        var cars = dto.Cars ?? await GetEligibleCarsAsync(start, end, dto.StorageCode);

        var details = new List<PaymentBDDetailItemDto>();
        decimal sumAmount = 0m;
        int sumBDCount = 0;
        long dummyId = 1;

        foreach (var c in cars)
        {
            var stCode = string.IsNullOrWhiteSpace(c.StorageCode) ? "KHO_NB" : c.StorageCode;
            var unitPrice = c.UnitPrice ?? masterCosts.FirstOrDefault(x => x.StorageCode == stCode && x.ModelCode == c.ModelCode)?.UnitPrice ?? 500000m;

            var bdCount = c.BDCount;
            var bdAmount = c.BDAmount ?? (bdCount * unitPrice);

            sumAmount += bdAmount;
            sumBDCount += bdCount;

            details.Add(new PaymentBDDetailItemDto(
                Id: dummyId++,
                PaymentBDNo: "PREVIEW",
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
                InvoiceFactoryDate: c.InvoiceFactoryDate,
                LatePmtBDDate: c.LatePmtBDDate,
                BDDate1: c.BDDate1,
                BDDate2: c.BDDate2,
                BDCount: bdCount,
                UnitPrice: unitPrice,
                BDAmount: bdAmount,
                AmountTotal: bdAmount,
                DealerCode: c.DealerCode,
                DealerName: c.DealerName,
                PackingListNo: c.PackingListNo,
                DeliveryOrderNo: c.DeliveryOrderNo,
                StorageRearrangeNoIn: c.StorageRearrangeNoIn,
                StorageRearrangeNoOut: c.StorageRearrangeNoOut,
                RetrieveOrderNo: c.RetrieveOrderNo,
                Remark: c.Remark
            ));
        }

        var vatAmount = Math.Round(sumAmount * dto.VAT / 100m, 0);
        var totalAfterVAT = sumAmount + vatAmount;

        return new PaymentBDPreviewResponseDto(
            PmtMonth: dto.PmtMonth,
            PmtPeriodStartDTime: start,
            PmtPeriodEndDTime: end,
            VAT: dto.VAT,
            TotalCars: details.Count,
            TotalBDCount: sumBDCount,
            AmountTotal: sumAmount,
            AmountVAT: vatAmount,
            AmountVATTotal: totalAfterVAT,
            Details: details
        );
    }

    public async Task<List<PaymentBDOrderDto>> ListAsync(
        string? pmtMonth = null,
        PaymentBDStatus? status = null,
        string? paymentBDNo = null,
        string? storageCode = null,
        string? vin = null,
        PaymentBDSignStatus? htvSignStatus = null,
        PaymentBDSignStatus? tcmsSignStatus = null,
        int pageIndex = 0,
        int pageSize = 50)
    {
        var q = db.PaymentBDOrders
            .Include(o => o.Details)
            .Where(o => o.OrgId == OrgId);

        if (!string.IsNullOrWhiteSpace(pmtMonth))
        {
            var mPrefix = pmtMonth.Length >= 7 ? pmtMonth[..7] : pmtMonth;
            q = q.Where(o => o.PmtMonth.StartsWith(mPrefix));
        }
        if (status.HasValue)
            q = q.Where(o => o.Status == status.Value);
        if (!string.IsNullOrWhiteSpace(paymentBDNo))
            q = q.Where(o => o.PaymentBDNo.Contains(paymentBDNo));
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

    public async Task<PaymentBDOrderDto?> DetailAsync(string paymentBDNo)
    {
        var order = await db.PaymentBDOrders
            .Include(o => o.Details)
            .FirstOrDefaultAsync(o => o.OrgId == OrgId && o.PaymentBDNo == paymentBDNo);
        return order is null ? null : MapOrderToDto(order);
    }

    public async Task<PaymentBDOrderDto> CreateAsync(CreatePaymentBDDto dto)
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
        var lastPeriod = await db.PaymentBDOrders
            .Where(o => o.OrgId == OrgId && o.Status != PaymentBDStatus.Cancelled && o.Status != PaymentBDStatus.Rejected)
            .OrderByDescending(o => o.PmtPeriodEndDTime)
            .FirstOrDefaultAsync();

        if (lastPeriod != null && start <= lastPeriod.PmtPeriodEndDTime)
            throw new InvalidOperationException($"Kỳ thanh toán mới bị trùng lặp với kỳ trước kết thúc ngày {lastPeriod.PmtPeriodEndDTime:yyyy-MM-dd}. Ngày bắt đầu phải sau kỳ trước.");

        var no = !string.IsNullOrWhiteSpace(dto.PaymentBDNo)
            ? dto.PaymentBDNo.Trim()
            : await GetNextSeqAsync();

        if (await db.PaymentBDOrders.AnyAsync(o => o.OrgId == OrgId && o.PaymentBDNo == no))
            throw new InvalidOperationException($"Số bảng kê thanh toán bảo dưỡng '{no}' đã tồn tại.");

        var order = new PaymentBDOrder
        {
            OrgId = OrgId,
            PaymentBDNo = no,
            PmtMonth = dto.PmtMonth.Trim(),
            PmtPeriodStartDTime = start,
            PmtPeriodEndDTime = end,
            VAT = dto.VAT,
            Status = PaymentBDStatus.Pending,
            HTVSignStatus = PaymentBDSignStatus.ChuaKy,
            TCMSSignStatus = PaymentBDSignStatus.ChuaKy,
            Remark = dto.Remark,
            CreatedBy = dto.CreatedBy ?? "SYSTEM_ADMIN",
            CreatedAt = DateTime.Now
        };

        var masterCosts = await db.InventoryCosts.Where(x => x.IsActive && x.CostTypeCode == "BD").ToListAsync();
        var cars = dto.Cars;
        if (cars == null || cars.Count == 0)
        {
            cars = await GetEligibleCarsAsync(start, end);
            if (cars.Count == 0)
                throw new InvalidOperationException("Không tìm thấy dòng xe nào đủ điều kiện để lập bảng kê bảo dưỡng trong kỳ này.");
        }

        decimal sumAmount = 0m;
        int sumBDCount = 0;
        foreach (var c in cars)
        {
            if (string.IsNullOrWhiteSpace(c.Vin))
                throw new InvalidOperationException("Số khung VIN không được để trống trong chi tiết.");
            if (string.IsNullOrWhiteSpace(c.RefNo))
                throw new InvalidOperationException($"Số tham chiếu RefNo không được để trống cho VIN {c.Vin}.");

            var stCode = string.IsNullOrWhiteSpace(c.StorageCode) ? "KHO_NB" : c.StorageCode;
            var unitPrice = c.UnitPrice ?? masterCosts.FirstOrDefault(x => x.StorageCode == stCode && x.ModelCode == c.ModelCode)?.UnitPrice ?? 500000m;
            var bdAmount = c.BDAmount ?? (c.BDCount * unitPrice);

            sumAmount += bdAmount;
            sumBDCount += c.BDCount;

            order.Details.Add(new PaymentBDDetail
            {
                PaymentBDNo = no,
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
                InvoiceFactoryDate = c.InvoiceFactoryDate,
                LatePmtBDDate = c.LatePmtBDDate,
                BDDate1 = c.BDDate1,
                BDDate2 = c.BDDate2,
                BDCount = c.BDCount,
                UnitPrice = unitPrice,
                BDAmount = bdAmount,
                AmountTotal = bdAmount,
                DealerCode = c.DealerCode,
                DealerName = c.DealerName,
                PackingListNo = c.PackingListNo,
                DeliveryOrderNo = c.DeliveryOrderNo,
                StorageRearrangeNoIn = c.StorageRearrangeNoIn,
                StorageRearrangeNoOut = c.StorageRearrangeNoOut,
                RetrieveOrderNo = c.RetrieveOrderNo,
                Remark = c.Remark
            });
        }

        order.TotalCars = order.Details.Count;
        order.TotalBDCount = sumBDCount;
        order.AmountTotal = sumAmount;
        order.AmountVAT = Math.Round(order.AmountTotal * order.VAT / 100m, 0);
        order.AmountVATTotal = order.AmountTotal + order.AmountVAT;

        db.PaymentBDOrders.Add(order);
        await db.SaveChangesAsync();

        return MapOrderToDto(order);
    }

    public async Task<PaymentBDOrderDto?> UpdateAsync(string paymentBDNo, UpdatePaymentBDDto dto)
    {
        var order = await db.PaymentBDOrders
            .Include(o => o.Details)
            .FirstOrDefaultAsync(o => o.OrgId == OrgId && o.PaymentBDNo == paymentBDNo);

        if (order == null) return null;

        if (order.Status != PaymentBDStatus.Pending)
            throw new InvalidOperationException($"Chỉ có thể cập nhật bảng kê ở trạng thái Chờ duyệt (Pending). Trạng thái hiện tại: {order.Status}");

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
            var masterCosts = await db.InventoryCosts.Where(x => x.IsActive && x.CostTypeCode == "BD").ToListAsync();
            order.Details.Clear();

            decimal sumAmount = 0m;
            int sumBDCount = 0;
            foreach (var c in dto.Cars)
            {
                var stCode = string.IsNullOrWhiteSpace(c.StorageCode) ? "KHO_NB" : c.StorageCode;
                var unitPrice = c.UnitPrice ?? masterCosts.FirstOrDefault(x => x.StorageCode == stCode && x.ModelCode == c.ModelCode)?.UnitPrice ?? 500000m;
                var bdAmount = c.BDAmount ?? (c.BDCount * unitPrice);

                sumAmount += bdAmount;
                sumBDCount += c.BDCount;

                order.Details.Add(new PaymentBDDetail
                {
                    PaymentBDNo = paymentBDNo,
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
                    InvoiceFactoryDate = c.InvoiceFactoryDate,
                    LatePmtBDDate = c.LatePmtBDDate,
                    BDDate1 = c.BDDate1,
                    BDDate2 = c.BDDate2,
                    BDCount = c.BDCount,
                    UnitPrice = unitPrice,
                    BDAmount = bdAmount,
                    AmountTotal = bdAmount,
                    DealerCode = c.DealerCode,
                    DealerName = c.DealerName,
                    PackingListNo = c.PackingListNo,
                    DeliveryOrderNo = c.DeliveryOrderNo,
                    StorageRearrangeNoIn = c.StorageRearrangeNoIn,
                    StorageRearrangeNoOut = c.StorageRearrangeNoOut,
                    RetrieveOrderNo = c.RetrieveOrderNo,
                    Remark = c.Remark
                });
            }

            order.TotalCars = order.Details.Count;
            order.TotalBDCount = sumBDCount;
            order.AmountTotal = sumAmount;
        }

        order.AmountVAT = Math.Round(order.AmountTotal * order.VAT / 100m, 0);
        order.AmountVATTotal = order.AmountTotal + order.AmountVAT;

        await db.SaveChangesAsync();
        return MapOrderToDto(order);
    }

    public async Task<PaymentBDOrderDto?> AddCarsAsync(string paymentBDNo, AddPaymentBDCarsDto dto)
    {
        var order = await db.PaymentBDOrders
            .Include(o => o.Details)
            .FirstOrDefaultAsync(o => o.OrgId == OrgId && o.PaymentBDNo == paymentBDNo);

        if (order == null) return null;

        if (order.Status != PaymentBDStatus.Pending)
            throw new InvalidOperationException($"Chỉ có thể thêm xe vào bảng kê ở trạng thái Chờ duyệt (Pending). Trạng thái hiện tại: {order.Status}");

        var masterCosts = await db.InventoryCosts.Where(x => x.IsActive && x.CostTypeCode == "BD").ToListAsync();

        foreach (var c in dto.Cars)
        {
            if (order.Details.Any(d => d.Vin == c.Vin))
                throw new InvalidOperationException($"Số khung VIN '{c.Vin}' đã tồn tại trong bảng kê này.");

            var stCode = string.IsNullOrWhiteSpace(c.StorageCode) ? "KHO_NB" : c.StorageCode;
            var unitPrice = c.UnitPrice ?? masterCosts.FirstOrDefault(x => x.StorageCode == stCode && x.ModelCode == c.ModelCode)?.UnitPrice ?? 500000m;
            var bdAmount = c.BDAmount ?? (c.BDCount * unitPrice);

            order.Details.Add(new PaymentBDDetail
            {
                PaymentBDNo = paymentBDNo,
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
                InvoiceFactoryDate = c.InvoiceFactoryDate,
                LatePmtBDDate = c.LatePmtBDDate,
                BDDate1 = c.BDDate1,
                BDDate2 = c.BDDate2,
                BDCount = c.BDCount,
                UnitPrice = unitPrice,
                BDAmount = bdAmount,
                AmountTotal = bdAmount,
                DealerCode = c.DealerCode,
                DealerName = c.DealerName,
                PackingListNo = c.PackingListNo,
                DeliveryOrderNo = c.DeliveryOrderNo,
                StorageRearrangeNoIn = c.StorageRearrangeNoIn,
                StorageRearrangeNoOut = c.StorageRearrangeNoOut,
                RetrieveOrderNo = c.RetrieveOrderNo,
                Remark = c.Remark
            });
        }

        RecalculateTotals(order);
        order.LogLUDateTime = DateTime.Now;
        order.LogLUBy = dto.UpdatedBy ?? "HTC_ACCOUNTANT";

        await db.SaveChangesAsync();
        return MapOrderToDto(order);
    }

    public async Task<PaymentBDOrderDto?> RemoveCarAsync(string paymentBDNo, string vin)
    {
        var order = await db.PaymentBDOrders
            .Include(o => o.Details)
            .FirstOrDefaultAsync(o => o.OrgId == OrgId && o.PaymentBDNo == paymentBDNo);

        if (order == null) return null;

        if (order.Status != PaymentBDStatus.Pending)
            throw new InvalidOperationException($"Chỉ có thể xóa xe khỏi bảng kê ở trạng thái Chờ duyệt (Pending). Trạng thái hiện tại: {order.Status}");

        var detail = order.Details.FirstOrDefault(d => d.Vin == vin);
        if (detail == null)
            throw new InvalidOperationException($"Không tìm thấy xe VIN '{vin}' trong bảng kê {paymentBDNo}.");

        order.Details.Remove(detail);
        if (order.Details.Count == 0)
            throw new InvalidOperationException("Không thể xóa dòng xe cuối cùng của bảng kê.");

        RecalculateTotals(order);
        order.LogLUDateTime = DateTime.Now;

        await db.SaveChangesAsync();
        return MapOrderToDto(order);
    }

    public async Task<PaymentBDOrderDto?> Approve1Async(string paymentBDNo, Approve1PaymentBDDto? dto)
    {
        var order = await db.PaymentBDOrders
            .Include(o => o.Details)
            .FirstOrDefaultAsync(o => o.OrgId == OrgId && o.PaymentBDNo == paymentBDNo);

        if (order == null) return null;

        if (order.Status != PaymentBDStatus.Pending)
            throw new InvalidOperationException($"Chỉ có thể duyệt cấp 1 bảng kê ở trạng thái Chờ duyệt (Pending). Trạng thái hiện tại: {order.Status}");

        order.Status = PaymentBDStatus.Approved1;
        order.Approve1At = DateTime.Now;
        order.Approve1By = dto?.Approved1By ?? "TP_KHO_VAN_HTC";
        if (!string.IsNullOrWhiteSpace(dto?.Remark))
            order.Remark = (order.Remark == null ? "" : order.Remark + " | ") + $"Duyệt cấp 1: {dto.Remark}";

        await db.SaveChangesAsync();
        return MapOrderToDto(order);
    }

    public async Task<PaymentBDOrderDto?> Approve2Async(string paymentBDNo, Approve2PaymentBDDto? dto)
    {
        var order = await db.PaymentBDOrders
            .Include(o => o.Details)
            .FirstOrDefaultAsync(o => o.OrgId == OrgId && o.PaymentBDNo == paymentBDNo);

        if (order == null) return null;

        if (order.Status != PaymentBDStatus.Approved1)
            throw new InvalidOperationException($"Chỉ có thể duyệt cấp 2 bảng kê ở trạng thái Đã duyệt cấp 1 (Approved1). Trạng thái hiện tại: {order.Status}");

        order.Status = PaymentBDStatus.Approved2;
        order.Approve2At = DateTime.Now;
        order.Approve2By = dto?.Approved2By ?? "LANH_DAO_HTC";
        if (!string.IsNullOrWhiteSpace(dto?.Remark))
            order.Remark = (order.Remark == null ? "" : order.Remark + " | ") + $"Duyệt cấp 2: {dto.Remark}";

        await db.SaveChangesAsync();
        return MapOrderToDto(order);
    }

    public async Task<List<string>> BatchApprove2Async(BatchApprove2PaymentBDDto dto)
    {
        var results = new List<string>();
        foreach (var no in dto.PaymentBDNos)
        {
            var order = await db.PaymentBDOrders
                .FirstOrDefaultAsync(o => o.OrgId == OrgId && o.PaymentBDNo == no && o.Status == PaymentBDStatus.Approved1);
            if (order != null)
            {
                order.Status = PaymentBDStatus.Approved2;
                order.Approve2At = DateTime.Now;
                order.Approve2By = dto.Approved2By ?? "LANH_DAO_HTC";
                results.Add(no);
            }
        }
        await db.SaveChangesAsync();
        return results;
    }

    public async Task<PaymentBDOrderDto?> TCMSESignAsync(string paymentBDNo, TCMSSignPaymentBDDto dto)
    {
        var order = await db.PaymentBDOrders
            .Include(o => o.Details)
            .FirstOrDefaultAsync(o => o.OrgId == OrgId && o.PaymentBDNo == paymentBDNo);

        if (order == null) return null;

        if (order.Status != PaymentBDStatus.Approved2 && order.Status != PaymentBDStatus.Finished)
            throw new InvalidOperationException($"TCMS chỉ có thể ký số điện tử khi bảng kê đã được Lãnh đạo HTC duyệt cấp 2 (Approved2). Trạng thái hiện tại: {order.Status}");

        order.TCMSSignStatus = PaymentBDSignStatus.DaKy;
        order.TCMSSignDTime = DateTime.Now;
        order.TCMSSignBy = dto.SignedBy ?? "GD_TCMS_ESIGN";
        if (!string.IsNullOrWhiteSpace(dto.EContractFileName))
            order.FilePath = $"/esign/tcms/{paymentBDNo}_{dto.EContractFileName}";

        if (order.HTVSignStatus == PaymentBDSignStatus.DaKy)
            order.Status = PaymentBDStatus.Finished;

        order.LogLUDateTime = DateTime.Now;
        order.LogLUBy = order.TCMSSignBy;

        await db.SaveChangesAsync();
        return MapOrderToDto(order);
    }

    public async Task<PaymentBDOrderDto?> HTVESignAsync(string paymentBDNo, HTVESignPaymentBDDto dto)
    {
        var order = await db.PaymentBDOrders
            .Include(o => o.Details)
            .FirstOrDefaultAsync(o => o.OrgId == OrgId && o.PaymentBDNo == paymentBDNo);

        if (order == null) return null;

        if (order.Status != PaymentBDStatus.Approved2 && order.Status != PaymentBDStatus.Finished)
            throw new InvalidOperationException($"HTV chỉ có thể ký số điện tử khi bảng kê ở trạng thái Đã duyệt cấp 2 (Approved2). Trạng thái hiện tại: {order.Status}");

        order.HTVSignStatus = PaymentBDSignStatus.DaKy;
        order.HTVSignDTime = DateTime.Now;
        order.HTVSignBy = dto.SignedBy ?? "TGD_HTV_ESIGN";
        order.FilePath = dto.FilePath ?? $"/esign/htv/{paymentBDNo}_contract.pdf";

        if (order.TCMSSignStatus == PaymentBDSignStatus.DaKy)
            order.Status = PaymentBDStatus.Finished;

        order.LogLUDateTime = DateTime.Now;
        order.LogLUBy = order.HTVSignBy;

        // Ghi mốc bảo dưỡng gần nhất (PmtBDDate) về kho xe vật lý Car_VIN cho từng VIN trong bảng kê.
        // VIN trùng nhiều dòng: lấy dòng có ngày bảo dưỡng muộn nhất (BDDate2 nếu có, else BDDate1).
        var vinMilestones = order.Details
            .Where(d => d.BDDate1 != null)
            .GroupBy(d => d.Vin)
            .Select(g => new
            {
                Vin = g.Key,
                EffDate = g.Select(d => d.BDDate2 ?? d.BDDate1).Max()
            })
            .ToList();

        if (vinMilestones.Count > 0)
        {
            var vins = vinMilestones.Select(x => x.Vin).ToList();
            var inventories = await db.CarVinInventories
                .Where(x => x.OrgId == OrgId && vins.Contains(x.Vin))
                .ToListAsync();
            foreach (var inv in inventories)
            {
                var m = vinMilestones.FirstOrDefault(x => x.Vin == inv.Vin);
                if (m?.EffDate != null)
                    inv.LastPmtBDDate = m.EffDate; // mốc bảo dưỡng gần nhất
            }
        }

        await db.SaveChangesAsync();
        return MapOrderToDto(order);
    }

    public async Task<PaymentBDOrderDto?> RejectAsync(string paymentBDNo, RejectPaymentBDDto dto)
    {
        var order = await db.PaymentBDOrders
            .Include(o => o.Details)
            .FirstOrDefaultAsync(o => o.OrgId == OrgId && o.PaymentBDNo == paymentBDNo);

        if (order == null) return null;

        if (order.Status != PaymentBDStatus.Pending && order.Status != PaymentBDStatus.Approved1)
            throw new InvalidOperationException($"Không thể từ chối bảng kê ở trạng thái {order.Status}. Chỉ từ chối khi đang chờ duyệt.");

        order.Status = PaymentBDStatus.Rejected;
        order.RejectReason = dto.Reason;
        order.RejectedAt = DateTime.Now;
        order.RejectedBy = dto.RejectedBy ?? "HTC_AUDITOR";

        await db.SaveChangesAsync();
        return MapOrderToDto(order);
    }

    public async Task<PaymentBDOrderDto?> CancelAsync(string paymentBDNo, CancelPaymentBDDto dto)
    {
        var order = await db.PaymentBDOrders
            .Include(o => o.Details)
            .FirstOrDefaultAsync(o => o.OrgId == OrgId && o.PaymentBDNo == paymentBDNo);

        if (order == null) return null;

        if (order.Status == PaymentBDStatus.Finished)
            throw new InvalidOperationException("Không thể hủy bảng kê thanh toán bảo dưỡng đã ký số hoàn tất quyết toán (Finished).");

        order.Status = PaymentBDStatus.Cancelled;
        order.CancelReason = dto.Reason;
        order.CancelledAt = DateTime.Now;
        order.CancelledBy = dto.CancelledBy ?? "HTC_ADMIN";

        await db.SaveChangesAsync();
        return MapOrderToDto(order);
    }

    public async Task<bool> DeleteDraftAsync(string paymentBDNo)
    {
        var order = await db.PaymentBDOrders
            .Include(o => o.Details)
            .FirstOrDefaultAsync(o => o.OrgId == OrgId && o.PaymentBDNo == paymentBDNo);

        if (order == null) return false;

        if (order.Status != PaymentBDStatus.Pending && order.Status != PaymentBDStatus.Rejected && order.Status != PaymentBDStatus.Cancelled)
            throw new InvalidOperationException($"Chỉ được xóa bảng kê ở trạng thái Chờ duyệt (Pending), Từ chối (Rejected) hoặc Đã hủy (Cancelled). Trạng thái hiện tại: {order.Status}");

        db.PaymentBDOrders.Remove(order);
        await db.SaveChangesAsync();
        return true;
    }

    public async Task<PaymentBDPrintDto?> GetPrintDataAsync(string paymentBDNo)
    {
        var orderDto = await DetailAsync(paymentBDNo);
        if (orderDto == null) return null;

        var modelSummary = orderDto.Details
            .GroupBy(d => d.ModelName)
            .ToDictionary(g => g.Key, g => g.Count());

        var storageSummary = orderDto.Details
            .GroupBy(d => d.StorageName)
            .ToDictionary(g => g.Key, g => g.Sum(d => d.AmountTotal));

        return new PaymentBDPrintDto(orderDto, modelSummary, storageSummary);
    }

    public async Task<PaymentBDStatsDto> GetStatsAsync(string? pmtMonth = null)
    {
        var q = db.PaymentBDOrders.Where(o => o.OrgId == OrgId);
        if (!string.IsNullOrWhiteSpace(pmtMonth))
        {
            var mPrefix = pmtMonth.Length >= 7 ? pmtMonth[..7] : pmtMonth;
            q = q.Where(o => o.PmtMonth.StartsWith(mPrefix));
        }

        var orders = await q.ToListAsync();
        var active = orders.Where(o => o.Status != PaymentBDStatus.Cancelled && o.Status != PaymentBDStatus.Rejected).ToList();

        return new PaymentBDStatsDto(
            TotalOrders: orders.Count,
            PendingOrders: orders.Count(o => o.Status == PaymentBDStatus.Pending),
            Approved1Orders: orders.Count(o => o.Status == PaymentBDStatus.Approved1),
            Approved2Orders: orders.Count(o => o.Status == PaymentBDStatus.Approved2),
            FinishedOrders: orders.Count(o => o.Status == PaymentBDStatus.Finished),
            RejectedOrders: orders.Count(o => o.Status == PaymentBDStatus.Rejected),
            CancelledOrders: orders.Count(o => o.Status == PaymentBDStatus.Cancelled),
            TotalAmountBeforeVAT: active.Sum(o => o.AmountTotal),
            TotalAmountVAT: active.Sum(o => o.AmountVAT),
            TotalAmountAfterVAT: active.Sum(o => o.AmountVATTotal),
            TotalCarsCount: active.Sum(o => o.TotalCars),
            TotalBDCount: active.Sum(o => o.TotalBDCount)
        );
    }

    private static void RecalculateTotals(PaymentBDOrder order)
    {
        order.TotalCars = order.Details.Count;
        order.TotalBDCount = order.Details.Sum(d => d.BDCount);
        order.AmountTotal = order.Details.Sum(d => d.AmountTotal);
        order.AmountVAT = Math.Round(order.AmountTotal * order.VAT / 100m, 0);
        order.AmountVATTotal = order.AmountTotal + order.AmountVAT;
    }

    private static PaymentBDOrderDto MapOrderToDto(PaymentBDOrder o)
    {
        return new PaymentBDOrderDto(
            Id: o.Id,
            PaymentBDNo: o.PaymentBDNo,
            PmtMonth: o.PmtMonth,
            PmtPeriodStartDTime: o.PmtPeriodStartDTime,
            PmtPeriodEndDTime: o.PmtPeriodEndDTime,
            VAT: o.VAT,
            TotalCars: o.TotalCars,
            TotalBDCount: o.TotalBDCount,
            AmountTotal: o.AmountTotal,
            AmountVAT: o.AmountVAT,
            AmountVATTotal: o.AmountVATTotal,
            Status: o.Status,
            StatusName: o.Status switch
            {
                PaymentBDStatus.Pending => "Chờ duyệt cấp 1",
                PaymentBDStatus.Approved1 => "Đã duyệt cấp 1 (Trưởng phòng)",
                PaymentBDStatus.Approved2 => "Đã duyệt cấp 2 (Lãnh đạo)",
                PaymentBDStatus.Finished => "Quyết toán hoàn tất (Đã ký số)",
                PaymentBDStatus.Rejected => "Từ chối duyệt",
                PaymentBDStatus.Cancelled => "Đã hủy",
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
            Details: o.Details.Select(d => new PaymentBDDetailItemDto(
                Id: d.Id,
                PaymentBDNo: d.PaymentBDNo,
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
                InvoiceFactoryDate: d.InvoiceFactoryDate,
                LatePmtBDDate: d.LatePmtBDDate,
                BDDate1: d.BDDate1,
                BDDate2: d.BDDate2,
                BDCount: d.BDCount,
                UnitPrice: d.UnitPrice,
                BDAmount: d.BDAmount,
                AmountTotal: d.AmountTotal,
                DealerCode: d.DealerCode,
                DealerName: d.DealerName,
                PackingListNo: d.PackingListNo,
                DeliveryOrderNo: d.DeliveryOrderNo,
                StorageRearrangeNoIn: d.StorageRearrangeNoIn,
                StorageRearrangeNoOut: d.StorageRearrangeNoOut,
                RetrieveOrderNo: d.RetrieveOrderNo,
                Remark: d.Remark
            )).ToList()
        );
    }
}
