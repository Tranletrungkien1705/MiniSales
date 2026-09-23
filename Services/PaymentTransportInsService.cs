using Microsoft.EntityFrameworkCore;
using MiniSales.Data;
using MiniSales.Models;

namespace MiniSales.Services;

public record TransportInsCarItemDto(
    string Vin,
    string? CarId = null,
    string? DlvMnNo = null,
    string? TranspReqType = "CARTRANSPORT",
    string? ModelCode = null,
    string? ModelName = null,
    string? SpecCode = null,
    string? ColorName = null,
    string? EngineNo = null,
    string? FStorageCode = null,
    string? FProvinceName = null,
    string? TStorageCode = null,
    string? TProvinceName = null,
    DateTime? InvStartDate = null,
    int ExpectedDays = 2,
    DateTime? ExpectedDlvEndDate = null,
    DateTime? InvEndDate = null,
    int? DelayDate = null,
    decimal TransportCost = 0,
    decimal DelayPenaty = 0,
    decimal PriceCar = 0,
    decimal InsurancePercent = 0.0005m,
    string? InsuranceContractNo = null,
    string? FProvinceRemark = null,
    string? StandardRemark = null,
    string? Remark = null
);

public record CreatePaymentTransportInsDto(
    string PmtMonth,
    string? TransportInsNo = null,
    decimal VAT = 10.0m,
    string? Remark = null,
    string? CreatedBy = null,
    List<TransportInsCarItemDto>? Cars = null
);

public record UpdatePaymentTransportInsDto(
    decimal? VAT = null,
    string? Remark = null,
    string? UpdatedBy = null,
    List<TransportInsCarItemDto>? Cars = null
);

public record AddPaymentTransportInsCarsDto(
    List<TransportInsCarItemDto> Cars,
    string? UpdatedBy = null
);

public record Approve1PaymentTransportInsDto(
    string? Approved1By = null,
    string? Remark = null
);

public record Approve2PaymentTransportInsDto(
    string? Approved2By = null,
    string? Remark = null
);

public record BatchApprove2PaymentTransportInsDto(
    List<string> TransportInsNos,
    string? Approved2By = null
);

public record TCMSSignPaymentTransportInsDto(
    string? SignedBy = null,
    string? Remark = null
);

public record HTVESignPaymentTransportInsDto(
    string? SignedBy = null,
    string? FilePath = null,
    string? Remark = null
);

public record CancelPaymentTransportInsDto(
    string Reason,
    string? CancelledBy = null
);

public record PaymentTransportInsPreviewRequestDto(
    string PmtMonth,
    decimal VAT = 10.0m,
    List<TransportInsCarItemDto>? Cars = null
);

public record TransportInsOrderDetailDto(
    long Id,
    string TransportInsNo,
    string Vin,
    string? CarId,
    string? DlvMnNo,
    string? TranspReqType,
    string? ModelCode,
    string? ModelName,
    string? SpecCode,
    string? ColorName,
    string? EngineNo,
    string? FStorageCode,
    string? FProvinceName,
    string? TStorageCode,
    string? TProvinceName,
    DateTime? InvStartDate,
    int ExpectedDays,
    DateTime? ExpectedDlvEndDate,
    DateTime? InvEndDate,
    int DelayDate,
    decimal TransportCost,
    decimal DelayPenaty,
    decimal PriceCar,
    decimal InsurancePercent,
    string? InsuranceContractNo,
    decimal InsuranceCost,
    decimal TotalPrice,
    TransportInsDetailStatus Status,
    string? FProvinceRemark,
    string? StandardRemark,
    string? Remark,
    DateTime LogLUDateTime,
    string? LogLUBy
);

public record TransportInsOrderDto(
    long Id,
    string TransportInsNo,
    string PmtMonth,
    decimal TotalAmount,
    decimal VAT,
    decimal UnitPriceVAT,
    decimal TotalAmountVAT,
    int TotalCars,
    decimal TotalTransportCost,
    decimal TotalDelayPenalty,
    decimal TotalInsuranceCost,
    TransportInsStatus Status,
    TransportInsSignStatus HTVSignStatus,
    DateTime? HTVSignDTime,
    string? HTVSignBy,
    TransportInsSignStatus TCMSSignStatus,
    DateTime? TCMSSignDTime,
    string? TCMSSignBy,
    string? FilePath,
    DateTime? App1DTime,
    string? App1By,
    DateTime? App2DTime,
    string? App2By,
    DateTime? CancelDTime,
    string? CancelBy,
    string? CancelReason,
    string? Remark,
    DateTime CreateDateTime,
    string? CreateBy,
    DateTime LogLUDateTime,
    string? LogLUBy,
    List<TransportInsOrderDetailDto> Details
);

public record PaymentTransportInsPreviewResponseDto(
    string PmtMonth,
    decimal VAT,
    int TotalCars,
    decimal TotalTransportCost,
    decimal TotalDelayPenalty,
    decimal TotalInsuranceCost,
    decimal TotalAmount,
    decimal UnitPriceVAT,
    decimal TotalAmountVAT,
    List<TransportInsOrderDetailDto> Details
);

public record PaymentTransportInsPrintDto(
    TransportInsOrderDto Order,
    Dictionary<string, int> RouteSummary,
    Dictionary<string, int> ModelSummary,
    decimal NetSettlementAmount
);

public record PaymentTransportInsExportDto(
    List<TransportInsOrderDto> Orders,
    List<TransportInsOrderDetailDto> Details
);

public record PaymentTransportInsStatsDto(
    int TotalOrders,
    int PendingOrders,
    int Approved1Orders,
    int Approved2Orders,
    int FinishedOrders,
    int CancelledOrders,
    int TotalCarsCount,
    decimal TotalTransportCost,
    decimal TotalDelayPenalty,
    decimal TotalInsuranceCost,
    decimal TotalBeforeVAT,
    decimal TotalVAT,
    decimal TotalAfterVAT
);

public interface IPaymentTransportInsService
{
    Task<string> GetNextSeqAsync();
    Task<List<TransportInsCarItemDto>> GetEligibleCarsAsync(string? pmtMonth = null, string? transpReqType = null, string? fStorageCode = null, string? tStorageCode = null);
    Task<PaymentTransportInsPreviewResponseDto> PreviewCalculationAsync(PaymentTransportInsPreviewRequestDto dto);
    Task<List<TransportInsOrderDto>> ListAsync(
        string? transportInsNo = null,
        string? pmtMonth = null,
        DateTime? createFrom = null,
        DateTime? createTo = null,
        TransportInsStatus? status = null,
        string? vin = null,
        TransportInsSignStatus? tcmsSignStatus = null,
        TransportInsSignStatus? htvSignStatus = null,
        int pageIndex = 0,
        int pageSize = 50);
    Task<TransportInsOrderDto?> DetailAsync(string transportInsNo);
    Task<TransportInsOrderDto> CreateAsync(CreatePaymentTransportInsDto dto);
    Task<TransportInsOrderDto?> UpdateAsync(string transportInsNo, UpdatePaymentTransportInsDto dto);
    Task<TransportInsOrderDto?> AddCarsAsync(string transportInsNo, AddPaymentTransportInsCarsDto dto);
    Task<TransportInsOrderDto?> RemoveCarAsync(string transportInsNo, string vin);
    Task<TransportInsOrderDto?> Approve1Async(string transportInsNo, Approve1PaymentTransportInsDto? dto);
    Task<TransportInsOrderDto?> Approve2Async(string transportInsNo, Approve2PaymentTransportInsDto? dto);
    Task<List<string>> BatchApprove2Async(BatchApprove2PaymentTransportInsDto dto);
    Task<TransportInsOrderDto?> TCMSSignAsync(string transportInsNo, TCMSSignPaymentTransportInsDto dto);
    Task<TransportInsOrderDto?> HTVESignAsync(string transportInsNo, HTVESignPaymentTransportInsDto dto);
    Task<TransportInsOrderDto?> CancelAsync(string transportInsNo, CancelPaymentTransportInsDto dto);
    Task<bool> DeleteDraftAsync(string transportInsNo);
    Task<PaymentTransportInsPrintDto?> GetPrintDataAsync(string transportInsNo);
    Task<PaymentTransportInsExportDto> ExportDataAsync(List<string>? transportInsNos = null);
    Task<PaymentTransportInsStatsDto> GetStatsAsync(string? pmtMonth = null);
}

public class PaymentTransportInsService(AppDbContext db, ITenantContext tenant) : IPaymentTransportInsService
{
    private Guid OrgId => tenant.OrgId;

    public async Task<string> GetNextSeqAsync()
    {
        var prefix = DateTime.Now.ToString("yyMM") + "TI";
        var existingNos = await db.TransportInsOrders
            .Where(x => x.OrgId == OrgId && x.TransportInsNo.StartsWith(prefix))
            .Select(x => x.TransportInsNo)
            .ToListAsync();

        int maxSeq = 0;
        foreach (var no in existingNos)
        {
            if (no.Length >= prefix.Length + 4 &&
                int.TryParse(no.Substring(prefix.Length, 4), out var s) && s > maxSeq)
            {
                maxSeq = s;
            }
        }

        return $"{prefix}{(maxSeq + 1):D4}";
    }

    public async Task<List<TransportInsCarItemDto>> GetEligibleCarsAsync(
        string? pmtMonth = null,
        string? transpReqType = null,
        string? fStorageCode = null,
        string? tStorageCode = null)
    {
        // Tra cứu các xe đã có biên bản giao nhận BBBG (CarTransportMinutes / Sto_DlvMinutes)
        // và chưa được đưa vào bảng kê thanh toán đang có hiệu lực (hoặc đã kết thúc/hủy thì được lập lại).
        var activeInsOrders = await db.TransportInsOrders
            .Where(o => o.OrgId == OrgId && o.Status != TransportInsStatus.Cancelled)
            .Select(o => o.TransportInsNo)
            .ToListAsync();

        var billedVins = await db.TransportInsOrderDetails
            .Where(d => activeInsOrders.Contains(d.TransportInsNo))
            .Select(d => d.Vin)
            .Distinct()
            .ToListAsync();

        var minutesList = await db.TransportMinutes
            .Where(m => m.OrgId == OrgId && m.Status == TransportMinutesStatus.Completed)
            .Include(m => m.Cars)
            .AsNoTracking()
            .ToListAsync();

        var results = new List<TransportInsCarItemDto>();

        foreach (var m in minutesList)
        {
            foreach (var car in m.Cars)
            {
                if (billedVins.Contains(car.Vin)) continue;

                var startDate = m.TransportMinutesDate.AddDays(-2);
                var expDays = 2; // Hạn mức mặc định theo định mức vận chuyển
                var expectedEndDate = startDate.AddDays(expDays);
                var actualEndDate = m.TransportMinutesDate;
                var delayDays = actualEndDate.Date > expectedEndDate.Date ? (int)(actualEndDate.Date - expectedEndDate.Date).TotalDays : 0;
                var transportCost = 3500000m;
                var penalty = delayDays > 0 ? delayDays * 150000m : 0m;
                var priceCar = 750000000m;
                var insPercent = 0.0005m; // 0.05%

                results.Add(new TransportInsCarItemDto(
                    Vin: car.Vin,
                    CarId: car.CarId,
                    DlvMnNo: m.TransportMinutesNo,
                    TranspReqType: "CARTRANSPORT",
                    ModelCode: car.Model,
                    ModelName: car.Model,
                    SpecCode: car.SpecCode,
                    ColorName: car.ColorCode,
                    EngineNo: car.EngineNo,
                    FStorageCode: "KHO_NB",
                    FProvinceName: "Ninh Bình",
                    TStorageCode: m.DealerCode,
                    TProvinceName: m.DealerName,
                    InvStartDate: startDate,
                    ExpectedDays: expDays,
                    ExpectedDlvEndDate: expectedEndDate,
                    InvEndDate: actualEndDate,
                    DelayDate: delayDays,
                    TransportCost: transportCost,
                    DelayPenaty: penalty,
                    PriceCar: priceCar,
                    InsurancePercent: insPercent,
                    InsuranceContractNo: "BHVT-2026-PTI",
                    Remark: $"Vận chuyển theo BBBG {m.TransportMinutesNo}"
                ));
            }
        }

        // Nếu danh sách từ BBBG ít, bổ sung thêm từ kho xe tồn CarVinInventory nếu có
        if (results.Count < 5)
        {
            var invCars = await db.CarVinInventories
                .Where(v => v.OrgId == OrgId && !billedVins.Contains(v.Vin))
                .Take(5)
                .AsNoTracking()
                .ToListAsync();

            foreach (var inv in invCars)
            {
                if (results.Any(r => r.Vin == inv.Vin)) continue;

                var st = DateTime.Today.AddDays(-10);
                var exp = st.AddDays(2);
                var actual = DateTime.Today.AddDays(-7);
                var delay = actual.Date > exp.Date ? (int)(actual.Date - exp.Date).TotalDays : 0;

                results.Add(new TransportInsCarItemDto(
                    Vin: inv.Vin,
                    CarId: inv.MappedCarId ?? $"CAR-{inv.ModelCode}-01",
                    DlvMnNo: "BBBG" + DateTime.Now.ToString("yyMM") + "001",
                    TranspReqType: "CARTRANSPORT",
                    ModelCode: inv.ModelCode,
                    ModelName: inv.ModelName,
                    SpecCode: inv.SpecCode,
                    ColorName: inv.ColorName,
                    EngineNo: inv.EngineNo,
                    FStorageCode: inv.StorageCode,
                    FProvinceName: "Ninh Bình",
                    TStorageCode: "VN001",
                    TProvinceName: "Hà Nội",
                    InvStartDate: st,
                    ExpectedDays: 2,
                    ExpectedDlvEndDate: exp,
                    InvEndDate: actual,
                    DelayDate: delay,
                    TransportCost: 4000000m,
                    DelayPenaty: delay * 150000m,
                    PriceCar: 850000000m,
                    InsurancePercent: 0.0005m,
                    InsuranceContractNo: "BHVT-2026-PTI",
                    Remark: "Bổ sung đối soát tồn xe giao đại lý"
                ));
            }
        }

        return results;
    }

    public Task<PaymentTransportInsPreviewResponseDto> PreviewCalculationAsync(PaymentTransportInsPreviewRequestDto dto)
    {
        var cars = dto.Cars ?? new List<TransportInsCarItemDto>();
        var details = new List<TransportInsOrderDetailDto>();

        long tempId = 1;
        foreach (var c in cars)
        {
            var detail = CalculateDetail(tempId++, "PREVIEW", c);
            details.Add(detail);
        }

        var totalTransportCost = details.Sum(d => d.TransportCost);
        var totalDelayPenalty = details.Sum(d => d.DelayPenaty);
        var totalInsuranceCost = details.Sum(d => d.InsuranceCost);
        var totalAmountVAT = details.Sum(d => d.TotalPrice);
        var vatRate = dto.VAT > 0 ? dto.VAT : 10.0m;
        var totalAmount = Math.Round(totalAmountVAT * 100m / (100m + vatRate), 2);
        var unitPriceVAT = totalAmountVAT - totalAmount;

        var result = new PaymentTransportInsPreviewResponseDto(
            PmtMonth: dto.PmtMonth,
            VAT: vatRate,
            TotalCars: details.Count,
            TotalTransportCost: totalTransportCost,
            TotalDelayPenalty: totalDelayPenalty,
            TotalInsuranceCost: totalInsuranceCost,
            TotalAmount: totalAmount,
            UnitPriceVAT: unitPriceVAT,
            TotalAmountVAT: totalAmountVAT,
            Details: details
        );

        return Task.FromResult(result);
    }

    public async Task<List<TransportInsOrderDto>> ListAsync(
        string? transportInsNo = null,
        string? pmtMonth = null,
        DateTime? createFrom = null,
        DateTime? createTo = null,
        TransportInsStatus? status = null,
        string? vin = null,
        TransportInsSignStatus? tcmsSignStatus = null,
        TransportInsSignStatus? htvSignStatus = null,
        int pageIndex = 0,
        int pageSize = 50)
    {
        var q = db.TransportInsOrders
            .Where(x => x.OrgId == OrgId)
            .Include(x => x.Details)
            .AsNoTracking();

        if (!string.IsNullOrWhiteSpace(transportInsNo))
            q = q.Where(x => x.TransportInsNo.Contains(transportInsNo.Trim()));

        if (!string.IsNullOrWhiteSpace(pmtMonth))
            q = q.Where(x => x.PmtMonth.StartsWith(pmtMonth.Trim()));

        if (createFrom.HasValue)
            q = q.Where(x => x.CreateDateTime >= createFrom.Value);

        if (createTo.HasValue)
            q = q.Where(x => x.CreateDateTime <= createTo.Value.AddDays(1));

        if (status.HasValue)
            q = q.Where(x => x.Status == status.Value);

        if (tcmsSignStatus.HasValue)
            q = q.Where(x => x.TCMSSignStatus == tcmsSignStatus.Value);

        if (htvSignStatus.HasValue)
            q = q.Where(x => x.HTVSignStatus == htvSignStatus.Value);

        if (!string.IsNullOrWhiteSpace(vin))
        {
            var targetVin = vin.Trim();
            q = q.Where(x => x.Details.Any(d => d.Vin.Contains(targetVin)));
        }

        var list = await q.OrderByDescending(x => x.CreateDateTime)
            .Skip(pageIndex * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return list.Select(ToOrderDto).ToList();
    }

    public async Task<TransportInsOrderDto?> DetailAsync(string transportInsNo)
    {
        var order = await db.TransportInsOrders
            .Where(x => x.OrgId == OrgId && x.TransportInsNo == transportInsNo)
            .Include(x => x.Details)
            .AsNoTracking()
            .FirstOrDefaultAsync();

        return order is null ? null : ToOrderDto(order);
    }

    public async Task<TransportInsOrderDto> CreateAsync(CreatePaymentTransportInsDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.PmtMonth))
            throw new InvalidOperationException("Tháng thanh toán (PmtMonth) là bắt buộc.");

        var no = string.IsNullOrWhiteSpace(dto.TransportInsNo)
            ? await GetNextSeqAsync()
            : dto.TransportInsNo.Trim();

        var existing = await db.TransportInsOrders
            .AnyAsync(x => x.OrgId == OrgId && x.TransportInsNo == no);
        if (existing)
            throw new InvalidOperationException($"Số bảng kê {no} đã tồn tại trong hệ thống.");

        var vatRate = dto.VAT >= 0 ? dto.VAT : 10.0m;

        var order = new TransportInsOrder
        {
            OrgId = OrgId,
            TransportInsNo = no,
            PmtMonth = dto.PmtMonth.Trim(),
            VAT = vatRate,
            Status = TransportInsStatus.Pending,
            HTVSignStatus = TransportInsSignStatus.ChuaKy,
            TCMSSignStatus = TransportInsSignStatus.ChuaKy,
            Remark = dto.Remark,
            CreateDateTime = DateTime.Now,
            CreateBy = dto.CreatedBy ?? "SYSTEM",
            LogLUDateTime = DateTime.Now,
            LogLUBy = dto.CreatedBy ?? "SYSTEM"
        };

        if (dto.Cars != null && dto.Cars.Count > 0)
        {
            foreach (var c in dto.Cars)
            {
                var detail = CreateEntityDetail(no, c, order.CreateBy);
                order.Details.Add(detail);
            }
        }

        RecalculateTotals(order);

        db.TransportInsOrders.Add(order);
        await db.SaveChangesAsync();

        return ToOrderDto(order);
    }

    public async Task<TransportInsOrderDto?> UpdateAsync(string transportInsNo, UpdatePaymentTransportInsDto dto)
    {
        var order = await db.TransportInsOrders
            .Where(x => x.OrgId == OrgId && x.TransportInsNo == transportInsNo)
            .Include(x => x.Details)
            .FirstOrDefaultAsync();

        if (order is null) return null;

        if (order.Status == TransportInsStatus.Approved2 ||
            order.Status == TransportInsStatus.Finished ||
            order.Status == TransportInsStatus.Cancelled)
        {
            throw new InvalidOperationException($"Không thể sửa bảng kê ở trạng thái '{order.Status}'.");
        }

        if (dto.VAT.HasValue && dto.VAT.Value >= 0)
            order.VAT = dto.VAT.Value;

        if (dto.Remark != null)
            order.Remark = dto.Remark;

        if (dto.Cars != null)
        {
            order.Details.Clear();
            foreach (var c in dto.Cars)
            {
                var d = CreateEntityDetail(order.TransportInsNo, c, dto.UpdatedBy ?? "HQ_LOGISTICS");
                order.Details.Add(d);
            }
        }

        order.LogLUDateTime = DateTime.Now;
        order.LogLUBy = dto.UpdatedBy ?? "HQ_LOGISTICS";

        RecalculateTotals(order);
        await db.SaveChangesAsync();

        return ToOrderDto(order);
    }

    public async Task<TransportInsOrderDto?> AddCarsAsync(string transportInsNo, AddPaymentTransportInsCarsDto dto)
    {
        var order = await db.TransportInsOrders
            .Where(x => x.OrgId == OrgId && x.TransportInsNo == transportInsNo)
            .Include(x => x.Details)
            .FirstOrDefaultAsync();

        if (order is null) return null;

        if (order.Status == TransportInsStatus.Approved2 ||
            order.Status == TransportInsStatus.Finished ||
            order.Status == TransportInsStatus.Cancelled)
        {
            throw new InvalidOperationException($"Không thể thêm xe vào bảng kê ở trạng thái '{order.Status}'.");
        }

        foreach (var c in dto.Cars)
        {
            if (order.Details.Any(x => x.Vin == c.Vin)) continue;

            var d = CreateEntityDetail(order.TransportInsNo, c, dto.UpdatedBy ?? "HQ_LOGISTICS");
            order.Details.Add(d);
        }

        order.LogLUDateTime = DateTime.Now;
        order.LogLUBy = dto.UpdatedBy ?? "HQ_LOGISTICS";

        RecalculateTotals(order);
        await db.SaveChangesAsync();

        return ToOrderDto(order);
    }

    public async Task<TransportInsOrderDto?> RemoveCarAsync(string transportInsNo, string vin)
    {
        var order = await db.TransportInsOrders
            .Where(x => x.OrgId == OrgId && x.TransportInsNo == transportInsNo)
            .Include(x => x.Details)
            .FirstOrDefaultAsync();

        if (order is null) return null;

        if (order.Status == TransportInsStatus.Approved2 ||
            order.Status == TransportInsStatus.Finished ||
            order.Status == TransportInsStatus.Cancelled)
        {
            throw new InvalidOperationException($"Không thể gỡ xe khỏi bảng kê ở trạng thái '{order.Status}'.");
        }

        var item = order.Details.FirstOrDefault(x => x.Vin == vin.Trim());
        if (item is null)
            throw new InvalidOperationException($"Không tìm thấy xe VIN {vin} trong bảng kê {transportInsNo}.");

        order.Details.Remove(item);
        db.TransportInsOrderDetails.Remove(item);

        order.LogLUDateTime = DateTime.Now;
        order.LogLUBy = "HQ_LOGISTICS";

        RecalculateTotals(order);
        await db.SaveChangesAsync();

        return ToOrderDto(order);
    }

    public async Task<TransportInsOrderDto?> Approve1Async(string transportInsNo, Approve1PaymentTransportInsDto? dto)
    {
        var order = await db.TransportInsOrders
            .Where(x => x.OrgId == OrgId && x.TransportInsNo == transportInsNo)
            .Include(x => x.Details)
            .FirstOrDefaultAsync();

        if (order is null) return null;

        if (order.Status != TransportInsStatus.Pending)
            throw new InvalidOperationException($"Chỉ có thể duyệt cấp 1 khi bảng kê ở trạng thái Chờ duyệt (Pending), hiện tại là: '{order.Status}'.");

        order.Status = TransportInsStatus.Approved1;
        order.App1DTime = DateTime.Now;
        order.App1By = dto?.Approved1By ?? "LOGISTICS_MANAGER";
        order.LogLUDateTime = DateTime.Now;
        order.LogLUBy = order.App1By;

        foreach (var d in order.Details)
        {
            d.Status = TransportInsDetailStatus.Approved;
            d.LogLUDateTime = DateTime.Now;
            d.LogLUBy = order.App1By;
        }

        if (!string.IsNullOrWhiteSpace(dto?.Remark))
            order.Remark = (order.Remark + " | " + dto.Remark).Trim(' ', '|');

        await db.SaveChangesAsync();
        return ToOrderDto(order);
    }

    public async Task<TransportInsOrderDto?> Approve2Async(string transportInsNo, Approve2PaymentTransportInsDto? dto)
    {
        var order = await db.TransportInsOrders
            .Where(x => x.OrgId == OrgId && x.TransportInsNo == transportInsNo)
            .Include(x => x.Details)
            .FirstOrDefaultAsync();

        if (order is null) return null;

        if (order.Status != TransportInsStatus.Approved1)
            throw new InvalidOperationException($"Chỉ có thể duyệt cấp 2 khi bảng kê đã qua duyệt cấp 1 (Approved1), hiện tại là: '{order.Status}'.");

        order.Status = TransportInsStatus.Approved2;
        order.App2DTime = DateTime.Now;
        order.App2By = dto?.Approved2By ?? "FINANCE_DIRECTOR";
        order.LogLUDateTime = DateTime.Now;
        order.LogLUBy = order.App2By;

        if (!string.IsNullOrWhiteSpace(dto?.Remark))
            order.Remark = (order.Remark + " | " + dto.Remark).Trim(' ', '|');

        await db.SaveChangesAsync();
        return ToOrderDto(order);
    }

    public async Task<List<string>> BatchApprove2Async(BatchApprove2PaymentTransportInsDto dto)
    {
        if (dto.TransportInsNos == null || dto.TransportInsNos.Count == 0)
            return new List<string>();

        var orders = await db.TransportInsOrders
            .Where(x => x.OrgId == OrgId && dto.TransportInsNos.Contains(x.TransportInsNo) && x.Status == TransportInsStatus.Approved1)
            .Include(x => x.Details)
            .ToListAsync();

        var approvedList = new List<string>();
        var user = dto.Approved2By ?? "FINANCE_DIRECTOR";

        foreach (var o in orders)
        {
            o.Status = TransportInsStatus.Approved2;
            o.App2DTime = DateTime.Now;
            o.App2By = user;
            o.LogLUDateTime = DateTime.Now;
            o.LogLUBy = user;
            approvedList.Add(o.TransportInsNo);
        }

        await db.SaveChangesAsync();
        return approvedList;
    }

    public async Task<TransportInsOrderDto?> TCMSSignAsync(string transportInsNo, TCMSSignPaymentTransportInsDto dto)
    {
        var order = await db.TransportInsOrders
            .Where(x => x.OrgId == OrgId && x.TransportInsNo == transportInsNo)
            .Include(x => x.Details)
            .FirstOrDefaultAsync();

        if (order is null) return null;

        if (order.Status == TransportInsStatus.Cancelled)
            throw new InvalidOperationException("Không thể ký số bảng kê đã bị hủy.");

        order.TCMSSignStatus = TransportInsSignStatus.DaKy;
        order.TCMSSignDTime = DateTime.Now;
        order.TCMSSignBy = dto.SignedBy ?? "TCMS_REPRESENTATIVE";
        order.LogLUDateTime = DateTime.Now;
        order.LogLUBy = order.TCMSSignBy;

        // Nếu cả HTV và TCMS đều đã ký số thì hoàn tất quyết toán
        if (order.HTVSignStatus == TransportInsSignStatus.DaKy)
        {
            order.Status = TransportInsStatus.Finished;
        }

        if (!string.IsNullOrWhiteSpace(dto.Remark))
            order.Remark = (order.Remark + " | TCMS: " + dto.Remark).Trim(' ', '|');

        await db.SaveChangesAsync();
        return ToOrderDto(order);
    }

    public async Task<TransportInsOrderDto?> HTVESignAsync(string transportInsNo, HTVESignPaymentTransportInsDto dto)
    {
        var order = await db.TransportInsOrders
            .Where(x => x.OrgId == OrgId && x.TransportInsNo == transportInsNo)
            .Include(x => x.Details)
            .FirstOrDefaultAsync();

        if (order is null) return null;

        if (order.Status == TransportInsStatus.Cancelled)
            throw new InvalidOperationException("Không thể ký số bảng kê đã bị hủy.");

        order.HTVSignStatus = TransportInsSignStatus.DaKy;
        order.HTVSignDTime = DateTime.Now;
        order.HTVSignBy = dto.SignedBy ?? "HTV_REPRESENTATIVE";
        order.LogLUDateTime = DateTime.Now;
        order.LogLUBy = order.HTVSignBy;

        if (!string.IsNullOrWhiteSpace(dto.FilePath))
            order.FilePath = dto.FilePath;

        // Nếu cả HTV và TCMS đều đã ký số thì hoàn tất quyết toán
        if (order.TCMSSignStatus == TransportInsSignStatus.DaKy)
        {
            order.Status = TransportInsStatus.Finished;
        }

        if (!string.IsNullOrWhiteSpace(dto.Remark))
            order.Remark = (order.Remark + " | HTV: " + dto.Remark).Trim(' ', '|');

        await db.SaveChangesAsync();
        return ToOrderDto(order);
    }

    public async Task<TransportInsOrderDto?> CancelAsync(string transportInsNo, CancelPaymentTransportInsDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Reason))
            throw new InvalidOperationException("Lý do hủy là bắt buộc.");

        var order = await db.TransportInsOrders
            .Where(x => x.OrgId == OrgId && x.TransportInsNo == transportInsNo)
            .Include(x => x.Details)
            .FirstOrDefaultAsync();

        if (order is null) return null;

        if (order.Status == TransportInsStatus.Finished)
            throw new InvalidOperationException("Không thể hủy bảng kê đã hoàn tất quyết toán (Finished).");

        order.Status = TransportInsStatus.Cancelled;
        order.CancelDTime = DateTime.Now;
        order.CancelBy = dto.CancelledBy ?? "SYSTEM";
        order.CancelReason = dto.Reason.Trim();
        order.LogLUDateTime = DateTime.Now;
        order.LogLUBy = order.CancelBy;

        await db.SaveChangesAsync();
        return ToOrderDto(order);
    }

    public async Task<bool> DeleteDraftAsync(string transportInsNo)
    {
        var order = await db.TransportInsOrders
            .Where(x => x.OrgId == OrgId && x.TransportInsNo == transportInsNo)
            .Include(x => x.Details)
            .FirstOrDefaultAsync();

        if (order is null) return false;

        if (order.Status != TransportInsStatus.Pending ||
            order.HTVSignStatus == TransportInsSignStatus.DaKy ||
            order.TCMSSignStatus == TransportInsSignStatus.DaKy)
        {
            throw new InvalidOperationException($"Chỉ có thể xóa bảng kê nháp ở trạng thái Chờ duyệt (Pending) và chưa ký số.");
        }

        db.TransportInsOrders.Remove(order);
        await db.SaveChangesAsync();
        return true;
    }

    public async Task<PaymentTransportInsPrintDto?> GetPrintDataAsync(string transportInsNo)
    {
        var order = await DetailAsync(transportInsNo);
        if (order is null) return null;

        var routeSummary = order.Details
            .GroupBy(d => $"{d.FProvinceName ?? "Ninh Bình"} -> {d.TProvinceName ?? "Đại lý"}")
            .ToDictionary(g => g.Key, g => g.Count());

        var modelSummary = order.Details
            .GroupBy(d => d.ModelName ?? d.ModelCode ?? "Khác")
            .ToDictionary(g => g.Key, g => g.Count());

        return new PaymentTransportInsPrintDto(
            Order: order,
            RouteSummary: routeSummary,
            ModelSummary: modelSummary,
            NetSettlementAmount: order.TotalAmountVAT
        );
    }

    public async Task<PaymentTransportInsExportDto> ExportDataAsync(List<string>? transportInsNos = null)
    {
        var q = db.TransportInsOrders
            .Where(x => x.OrgId == OrgId)
            .Include(x => x.Details)
            .AsNoTracking();

        if (transportInsNos != null && transportInsNos.Count > 0)
            q = q.Where(x => transportInsNos.Contains(x.TransportInsNo));

        var orders = await q.OrderByDescending(x => x.CreateDateTime).ToListAsync();
        var orderDtos = orders.Select(ToOrderDto).ToList();
        var allDetails = orderDtos.SelectMany(o => o.Details).ToList();

        return new PaymentTransportInsExportDto(
            Orders: orderDtos,
            Details: allDetails
        );
    }

    public async Task<PaymentTransportInsStatsDto> GetStatsAsync(string? pmtMonth = null)
    {
        var q = db.TransportInsOrders
            .Where(x => x.OrgId == OrgId)
            .AsNoTracking();

        if (!string.IsNullOrWhiteSpace(pmtMonth))
            q = q.Where(x => x.PmtMonth.StartsWith(pmtMonth.Trim()));

        var orders = await q.ToListAsync();

        return new PaymentTransportInsStatsDto(
            TotalOrders: orders.Count,
            PendingOrders: orders.Count(x => x.Status == TransportInsStatus.Pending),
            Approved1Orders: orders.Count(x => x.Status == TransportInsStatus.Approved1),
            Approved2Orders: orders.Count(x => x.Status == TransportInsStatus.Approved2),
            FinishedOrders: orders.Count(x => x.Status == TransportInsStatus.Finished),
            CancelledOrders: orders.Count(x => x.Status == TransportInsStatus.Cancelled),
            TotalCarsCount: orders.Sum(x => x.TotalCars),
            TotalTransportCost: orders.Sum(x => x.TotalTransportCost),
            TotalDelayPenalty: orders.Sum(x => x.TotalDelayPenalty),
            TotalInsuranceCost: orders.Sum(x => x.TotalInsuranceCost),
            TotalBeforeVAT: orders.Sum(x => x.TotalAmount),
            TotalVAT: orders.Sum(x => x.UnitPriceVAT),
            TotalAfterVAT: orders.Sum(x => x.TotalAmountVAT)
        );
    }

    // --- Private Helper Methods ---

    private static TransportInsOrderDetail CreateEntityDetail(string orderNo, TransportInsCarItemDto dto, string? user)
    {
        var startDate = dto.InvStartDate ?? DateTime.Today.AddDays(-2);
        var expDays = dto.ExpectedDays > 0 ? dto.ExpectedDays : 2;
        var expEndDate = dto.ExpectedDlvEndDate ?? startDate.AddDays(expDays);
        var actualEndDate = dto.InvEndDate ?? startDate.AddDays(expDays);

        int delayDays;
        if (dto.DelayDate.HasValue)
        {
            delayDays = dto.DelayDate.Value;
        }
        else
        {
            delayDays = actualEndDate.Date > expEndDate.Date
                ? (int)(actualEndDate.Date - expEndDate.Date).TotalDays
                : 0;
        }

        var penalty = dto.DelayPenaty >= 0 ? dto.DelayPenaty : (delayDays * 150000m);
        var transportCost = dto.TransportCost >= 0 ? dto.TransportCost : 3500000m;
        var priceCar = dto.PriceCar >= 0 ? dto.PriceCar : 750000000m;
        var insPercent = dto.InsurancePercent >= 0 ? dto.InsurancePercent : 0.0005m;
        var insCost = Math.Round(priceCar * insPercent, 2);
        var totalPrice = Math.Max(0m, insCost + transportCost - penalty);

        return new TransportInsOrderDetail
        {
            TransportInsNo = orderNo,
            Vin = dto.Vin.Trim(),
            CarId = dto.CarId,
            DlvMnNo = dto.DlvMnNo,
            TranspReqType = string.IsNullOrWhiteSpace(dto.TranspReqType) ? "CARTRANSPORT" : dto.TranspReqType.Trim(),
            ModelCode = dto.ModelCode,
            ModelName = dto.ModelName,
            SpecCode = dto.SpecCode,
            ColorName = dto.ColorName,
            EngineNo = dto.EngineNo,
            FStorageCode = dto.FStorageCode ?? "KHO_NB",
            FProvinceName = dto.FProvinceName ?? "Ninh Bình",
            TStorageCode = dto.TStorageCode,
            TProvinceName = dto.TProvinceName,
            InvStartDate = startDate,
            ExpectedDays = expDays,
            ExpectedDlvEndDate = expEndDate,
            InvEndDate = actualEndDate,
            DelayDate = delayDays,
            TransportCost = transportCost,
            DelayPenaty = penalty,
            PriceCar = priceCar,
            InsurancePercent = insPercent,
            InsuranceContractNo = dto.InsuranceContractNo ?? "BHVT-2026-PTI",
            InsuranceCost = insCost,
            TotalPrice = totalPrice,
            Status = TransportInsDetailStatus.Pending,
            FProvinceRemark = dto.FProvinceRemark,
            StandardRemark = dto.StandardRemark,
            Remark = dto.Remark,
            LogLUDateTime = DateTime.Now,
            LogLUBy = user ?? "SYSTEM"
        };
    }

    private static TransportInsOrderDetailDto CalculateDetail(long id, string orderNo, TransportInsCarItemDto dto)
    {
        var startDate = dto.InvStartDate ?? DateTime.Today.AddDays(-2);
        var expDays = dto.ExpectedDays > 0 ? dto.ExpectedDays : 2;
        var expEndDate = dto.ExpectedDlvEndDate ?? startDate.AddDays(expDays);
        var actualEndDate = dto.InvEndDate ?? startDate.AddDays(expDays);

        int delayDays;
        if (dto.DelayDate.HasValue)
        {
            delayDays = dto.DelayDate.Value;
        }
        else
        {
            delayDays = actualEndDate.Date > expEndDate.Date
                ? (int)(actualEndDate.Date - expEndDate.Date).TotalDays
                : 0;
        }

        var penalty = dto.DelayPenaty >= 0 ? dto.DelayPenaty : (delayDays * 150000m);
        var transportCost = dto.TransportCost >= 0 ? dto.TransportCost : 3500000m;
        var priceCar = dto.PriceCar >= 0 ? dto.PriceCar : 750000000m;
        var insPercent = dto.InsurancePercent >= 0 ? dto.InsurancePercent : 0.0005m;
        var insCost = Math.Round(priceCar * insPercent, 2);
        var totalPrice = Math.Max(0m, insCost + transportCost - penalty);

        return new TransportInsOrderDetailDto(
            Id: id,
            TransportInsNo: orderNo,
            Vin: dto.Vin.Trim(),
            CarId: dto.CarId,
            DlvMnNo: dto.DlvMnNo,
            TranspReqType: dto.TranspReqType ?? "CARTRANSPORT",
            ModelCode: dto.ModelCode,
            ModelName: dto.ModelName,
            SpecCode: dto.SpecCode,
            ColorName: dto.ColorName,
            EngineNo: dto.EngineNo,
            FStorageCode: dto.FStorageCode ?? "KHO_NB",
            FProvinceName: dto.FProvinceName ?? "Ninh Bình",
            TStorageCode: dto.TStorageCode,
            TProvinceName: dto.TProvinceName,
            InvStartDate: startDate,
            ExpectedDays: expDays,
            ExpectedDlvEndDate: expEndDate,
            InvEndDate: actualEndDate,
            DelayDate: delayDays,
            TransportCost: transportCost,
            DelayPenaty: penalty,
            PriceCar: priceCar,
            InsurancePercent: insPercent,
            InsuranceContractNo: dto.InsuranceContractNo ?? "BHVT-2026-PTI",
            InsuranceCost: insCost,
            TotalPrice: totalPrice,
            Status: TransportInsDetailStatus.Pending,
            FProvinceRemark: dto.FProvinceRemark,
            StandardRemark: dto.StandardRemark,
            Remark: dto.Remark,
            LogLUDateTime: DateTime.Now,
            LogLUBy: "SYSTEM"
        );
    }

    private static void RecalculateTotals(TransportInsOrder order)
    {
        order.TotalCars = order.Details.Count;
        order.TotalTransportCost = order.Details.Sum(d => d.TransportCost);
        order.TotalDelayPenalty = order.Details.Sum(d => d.DelayPenaty);
        order.TotalInsuranceCost = order.Details.Sum(d => d.InsuranceCost);
        order.TotalAmountVAT = order.Details.Sum(d => d.TotalPrice);

        var vatRate = order.VAT >= 0 ? order.VAT : 10.0m;
        order.TotalAmount = Math.Round(order.TotalAmountVAT * 100m / (100m + vatRate), 2);
        order.UnitPriceVAT = order.TotalAmountVAT - order.TotalAmount;
    }

    private static TransportInsOrderDto ToOrderDto(TransportInsOrder o)
    {
        var details = o.Details.Select(d => new TransportInsOrderDetailDto(
            Id: d.Id,
            TransportInsNo: d.TransportInsNo,
            Vin: d.Vin,
            CarId: d.CarId,
            DlvMnNo: d.DlvMnNo,
            TranspReqType: d.TranspReqType,
            ModelCode: d.ModelCode,
            ModelName: d.ModelName,
            SpecCode: d.SpecCode,
            ColorName: d.ColorName,
            EngineNo: d.EngineNo,
            FStorageCode: d.FStorageCode,
            FProvinceName: d.FProvinceName,
            TStorageCode: d.TStorageCode,
            TProvinceName: d.TProvinceName,
            InvStartDate: d.InvStartDate,
            ExpectedDays: d.ExpectedDays,
            ExpectedDlvEndDate: d.ExpectedDlvEndDate,
            InvEndDate: d.InvEndDate,
            DelayDate: d.DelayDate,
            TransportCost: d.TransportCost,
            DelayPenaty: d.DelayPenaty,
            PriceCar: d.PriceCar,
            InsurancePercent: d.InsurancePercent,
            InsuranceContractNo: d.InsuranceContractNo,
            InsuranceCost: d.InsuranceCost,
            TotalPrice: d.TotalPrice,
            Status: d.Status,
            FProvinceRemark: d.FProvinceRemark,
            StandardRemark: d.StandardRemark,
            Remark: d.Remark,
            LogLUDateTime: d.LogLUDateTime,
            LogLUBy: d.LogLUBy
        )).ToList();

        return new TransportInsOrderDto(
            Id: o.Id,
            TransportInsNo: o.TransportInsNo,
            PmtMonth: o.PmtMonth,
            TotalAmount: o.TotalAmount,
            VAT: o.VAT,
            UnitPriceVAT: o.UnitPriceVAT,
            TotalAmountVAT: o.TotalAmountVAT,
            TotalCars: o.TotalCars,
            TotalTransportCost: o.TotalTransportCost,
            TotalDelayPenalty: o.TotalDelayPenalty,
            TotalInsuranceCost: o.TotalInsuranceCost,
            Status: o.Status,
            HTVSignStatus: o.HTVSignStatus,
            HTVSignDTime: o.HTVSignDTime,
            HTVSignBy: o.HTVSignBy,
            TCMSSignStatus: o.TCMSSignStatus,
            TCMSSignDTime: o.TCMSSignDTime,
            TCMSSignBy: o.TCMSSignBy,
            FilePath: o.FilePath,
            App1DTime: o.App1DTime,
            App1By: o.App1By,
            App2DTime: o.App2DTime,
            App2By: o.App2By,
            CancelDTime: o.CancelDTime,
            CancelBy: o.CancelBy,
            CancelReason: o.CancelReason,
            Remark: o.Remark,
            CreateDateTime: o.CreateDateTime,
            CreateBy: o.CreateBy,
            LogLUDateTime: o.LogLUDateTime,
            LogLUBy: o.LogLUBy,
            Details: details
        );
    }
}
