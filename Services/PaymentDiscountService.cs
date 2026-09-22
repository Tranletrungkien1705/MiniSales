using Microsoft.EntityFrameworkCore;
using MiniSales.Data;
using MiniSales.Models;

namespace MiniSales.Services;

public record CreatePaymentDiscountDetailDto(
    string CarId,
    string Vin,
    string Model,
    string? SpecCode,
    string? SpecDescription,
    string? SOCode,
    string GuaranteeNo,
    string? BankGuaranteeNo,
    string? BankCode,
    DateTime? DateOpen,
    int? Term,
    DateTime? DateStart,
    DateTime? DateEnd,
    decimal UnitPrice,
    decimal? UnitPriceActual,
    decimal? GuaranteeValue,
    DateTime? PaymentEndDatePhase1,
    decimal? AmountPhase1,
    int? DiscountDateNumberPhase1,
    decimal? DiscountPercentPhase1,
    decimal? DiscountPricePhase1,
    DateTime? PaymentEndDatePhase2,
    decimal? AmountPhase2,
    int? DiscountDateNumberPhase2,
    decimal? DiscountPercentPhase2,
    decimal? DiscountPricePhase2,
    DateTime? PaymentEndDatePhase3,
    decimal? AmountPhase3,
    int? DiscountDateNumberPhase3,
    decimal? DiscountPercentPhase3,
    decimal? DiscountPricePhase3,
    string? Remark
);

public record CreatePaymentDiscountDto(
    string? PaymentDiscountNo,
    string DealerCode,
    string? DealerName,
    string? CompanyName,
    DateTime? DateEndFrom,
    DateTime? DateEndTo,
    decimal? DiscountPercent,
    decimal? PenaltyPercent,
    string? Remark,
    string? CreatedBy,
    List<CreatePaymentDiscountDetailDto>? Details
);

public record UpdatePaymentDiscountDto(
    DateTime? DateEndFrom,
    DateTime? DateEndTo,
    decimal? DiscountPercent,
    decimal? PenaltyPercent,
    string? Remark,
    string? UpdatedBy
);

public record SignDealerPaymentDiscountDto(
    string? SignedBy,
    string? EContractFileName,
    string? Remark
);

public record ApproveHqPaymentDiscountDto(
    string? ApprovedBy,
    string? Remark
);

public record SignHqPaymentDiscountDto(
    string? SignedBy,
    string? EContractFileName,
    string? Remark
);

public record RejectPaymentDiscountDto(
    string RejectReason,
    string? RejectedBy
);

public record CancelPaymentDiscountDto(
    string CancelReason,
    string? CancelledBy
);

public record AddCarToPaymentDiscountDto(
    string CarId,
    string Vin,
    string Model,
    string? SpecCode,
    string? SpecDescription,
    string? SOCode,
    string GuaranteeNo,
    string? BankGuaranteeNo,
    string? BankCode,
    DateTime? DateOpen,
    int? Term,
    DateTime? DateStart,
    DateTime? DateEnd,
    decimal UnitPrice,
    decimal? UnitPriceActual,
    decimal? GuaranteeValue,
    DateTime? PaymentEndDatePhase1,
    decimal? AmountPhase1,
    int? DiscountDateNumberPhase1,
    decimal? DiscountPercentPhase1,
    decimal? DiscountPricePhase1,
    DateTime? PaymentEndDatePhase2,
    decimal? AmountPhase2,
    int? DiscountDateNumberPhase2,
    decimal? DiscountPercentPhase2,
    decimal? DiscountPricePhase2,
    DateTime? PaymentEndDatePhase3,
    decimal? AmountPhase3,
    int? DiscountDateNumberPhase3,
    decimal? DiscountPercentPhase3,
    decimal? DiscountPricePhase3,
    string? Remark
);

public record UpdatePaymentDiscountLineDto(
    DateTime? PaymentEndDatePhase1,
    decimal? AmountPhase1,
    int? DiscountDateNumberPhase1,
    decimal? DiscountPercentPhase1,
    DateTime? PaymentEndDatePhase2,
    decimal? AmountPhase2,
    int? DiscountDateNumberPhase2,
    decimal? DiscountPercentPhase2,
    DateTime? PaymentEndDatePhase3,
    decimal? AmountPhase3,
    int? DiscountDateNumberPhase3,
    decimal? DiscountPercentPhase3,
    string? Remark
);

public record PaymentDiscountStatsDto(
    int TotalRequests,
    int DraftRequests,
    int ApprovedRequests,
    int SignedRequests,
    int RejectedRequests,
    int CancelledRequests,
    int TotalCars,
    decimal TotalDiscountAmount,
    int DistinctDealersCount
);

public interface IPaymentDiscountService
{
    Task<object> CreateAsync(CreatePaymentDiscountDto dto);
    Task<object> ListAsync(string? status, string? dealer, string? discountNo, string? vin, string? guaranteeNo, DateTime? fromDate, DateTime? toDate);
    Task<object?> DetailAsync(string paymentDiscountNo);
    Task<object> GetEligibleCarsAsync(string? dealerCode, string? guaranteeNo, string? vin);
    Task<object?> UpdateAsync(string paymentDiscountNo, UpdatePaymentDiscountDto dto);
    Task<object?> SignDealerAsync(string paymentDiscountNo, SignDealerPaymentDiscountDto dto);
    Task<object?> ApproveHqAsync(string paymentDiscountNo, ApproveHqPaymentDiscountDto? dto);
    Task<object?> SignHqAsync(string paymentDiscountNo, SignHqPaymentDiscountDto dto);
    Task<object?> RejectHqAsync(string paymentDiscountNo, RejectPaymentDiscountDto dto);
    Task<object?> CancelAsync(string paymentDiscountNo, CancelPaymentDiscountDto dto);
    Task<object?> DeleteDraftAsync(string paymentDiscountNo);
    Task<object?> AddCarAsync(string paymentDiscountNo, AddCarToPaymentDiscountDto dto);
    Task<object?> RemoveCarAsync(string paymentDiscountNo, string vin);
    Task<object?> UpdateLineAsync(string paymentDiscountNo, string vin, UpdatePaymentDiscountLineDto dto);
    Task<object> StatsAsync();
}

public sealed class PaymentDiscountService(AppDbContext db, ITenantContext tenant) : IPaymentDiscountService
{
    private Guid Org => tenant.OrgId;

    public async Task<object> CreateAsync(CreatePaymentDiscountDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.DealerCode))
            throw new InvalidOperationException("DealerCode (mã đại lý) không được để trống.");

        if (dto.Details is null || dto.Details.Count == 0)
            throw new InvalidOperationException("Đề nghị chiết khấu thanh toán phải có ít nhất 1 xe trong danh sách.");

        var dealerCode = dto.DealerCode.Trim().ToUpperInvariant();

        // Kiểm tra trùng lặp VIN trong cùng đề nghị
        var vinSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var d in dto.Details)
        {
            if (string.IsNullOrWhiteSpace(d.Vin))
                throw new InvalidOperationException("Mỗi xe phải có số khung VIN.");
            if (!vinSet.Add(d.Vin.Trim()))
                throw new InvalidOperationException($"Số khung VIN {d.Vin.Trim()} bị trùng lặp trong đề nghị.");
        }

        // Sinh số đề nghị nếu chưa có: {yyyyMMdd}-{seq:D3}/DNCK/{DealerCode} (Chuẩn DMS.Sales ReqPaymentDiscount)
        string discountNo;
        if (!string.IsNullOrWhiteSpace(dto.PaymentDiscountNo))
        {
            discountNo = dto.PaymentDiscountNo.Trim().ToUpperInvariant();
        }
        else
        {
            var datePrefix = DateTime.Today.ToString("yyyyMMdd");
            var prefix = $"{datePrefix}-";
            var suffix = $"/DNCK/{dealerCode}";

            var maxSeq = await db.PaymentDiscounts
                .Where(x => x.OrgId == Org && x.DealerCode == dealerCode && x.PaymentDiscountNo.StartsWith(prefix) && x.PaymentDiscountNo.EndsWith(suffix))
                .Select(x => x.PaymentDiscountNo)
                .ToListAsync();

            int nextSeq = 1;
            if (maxSeq.Count > 0)
            {
                foreach (var s in maxSeq)
                {
                    var middle = s.Substring(prefix.Length, Math.Max(0, s.Length - prefix.Length - suffix.Length));
                    if (int.TryParse(middle, out var parsed) && parsed >= nextSeq)
                        nextSeq = parsed + 1;
                }
            }

            discountNo = $"{prefix}{nextSeq:D3}{suffix}";
        }

        if (await db.PaymentDiscounts.AnyAsync(x => x.OrgId == Org && x.PaymentDiscountNo == discountNo))
            throw new InvalidOperationException($"Số đề nghị chiết khấu {discountNo} đã tồn tại trong hệ thống.");

        var defaultDiscountPercent = dto.DiscountPercent ?? 0.5m;
        var defaultPenaltyPercent = dto.PenaltyPercent ?? 0.05m;

        var entity = new PaymentDiscount
        {
            OrgId = Org,
            PaymentDiscountNo = discountNo,
            DealerCode = dealerCode,
            DealerName = string.IsNullOrWhiteSpace(dto.DealerName) ? dealerCode : dto.DealerName.Trim(),
            CompanyName = dto.CompanyName?.Trim(),
            DateEndFrom = dto.DateEndFrom,
            DateEndTo = dto.DateEndTo,
            DiscountPercent = defaultDiscountPercent,
            PenaltyPercent = defaultPenaltyPercent,
            Status = PaymentDiscountStatus.Draft,
            DealerSignStatus = PaymentDiscountSignStatus.Pending,
            HQSignStatus = PaymentDiscountSignStatus.Pending,
            Remark = dto.Remark?.Trim(),
            CreatedBy = dto.CreatedBy ?? "SYSTEM",
            CreatedAt = DateTime.Now
        };

        decimal sumTotalDiscount = 0m;
        foreach (var d in dto.Details)
        {
            var actualPrice = d.UnitPriceActual ?? d.UnitPrice;
            var grtValue = d.GuaranteeValue ?? actualPrice;

            var amt1 = d.AmountPhase1 ?? grtValue;
            var days1 = d.DiscountDateNumberPhase1 ?? 10;
            var pct1 = d.DiscountPercentPhase1 ?? defaultDiscountPercent;
            var disc1 = d.DiscountPricePhase1 ?? Math.Round(amt1 * pct1 / 100m, 0);

            var amt2 = d.AmountPhase2 ?? 0m;
            var days2 = d.DiscountDateNumberPhase2 ?? 0;
            var pct2 = d.DiscountPercentPhase2 ?? 0m;
            var disc2 = d.DiscountPricePhase2 ?? Math.Round(amt2 * pct2 / 100m, 0);

            var amt3 = d.AmountPhase3 ?? 0m;
            var days3 = d.DiscountDateNumberPhase3 ?? 0;
            var pct3 = d.DiscountPercentPhase3 ?? 0m;
            var disc3 = d.DiscountPricePhase3 ?? Math.Round(amt3 * pct3 / 100m, 0);

            var totalAmt = amt1 + amt2 + amt3;
            var lineTotalDiscount = disc1 + disc2 + disc3;
            sumTotalDiscount += lineTotalDiscount;

            entity.Details.Add(new PaymentDiscountDetail
            {
                PaymentDiscountNo = discountNo,
                CarId = string.IsNullOrWhiteSpace(d.CarId) ? $"CAR-{d.Vin.Trim()}" : d.CarId.Trim(),
                Vin = d.Vin.Trim().ToUpperInvariant(),
                Model = d.Model.Trim(),
                SpecCode = d.SpecCode?.Trim(),
                SpecDescription = d.SpecDescription?.Trim(),
                SOCode = d.SOCode?.Trim(),
                GuaranteeNo = string.IsNullOrWhiteSpace(d.GuaranteeNo) ? $"BG-{d.Vin.Trim()}" : d.GuaranteeNo.Trim(),
                BankGuaranteeNo = d.BankGuaranteeNo?.Trim(),
                BankCode = d.BankCode?.Trim().ToUpperInvariant(),
                DateOpen = d.DateOpen,
                Term = d.Term ?? 30,
                DateStart = d.DateStart,
                DateEnd = d.DateEnd,
                UnitPrice = d.UnitPrice,
                UnitPriceActual = actualPrice,
                GuaranteeValue = grtValue,
                PaymentEndDatePhase1 = d.PaymentEndDatePhase1 ?? DateTime.Today.AddDays(10),
                AmountPhase1 = amt1,
                DiscountDateNumberPhase1 = days1,
                DiscountPercentPhase1 = pct1,
                DiscountPricePhase1 = disc1,
                PaymentEndDatePhase2 = d.PaymentEndDatePhase2,
                AmountPhase2 = amt2,
                DiscountDateNumberPhase2 = days2,
                DiscountPercentPhase2 = pct2,
                DiscountPricePhase2 = disc2,
                PaymentEndDatePhase3 = d.PaymentEndDatePhase3,
                AmountPhase3 = amt3,
                DiscountDateNumberPhase3 = days3,
                DiscountPercentPhase3 = pct3,
                DiscountPricePhase3 = disc3,
                TotalAmount = totalAmt,
                TotalDiscountPrice = lineTotalDiscount,
                Remark = d.Remark?.Trim()
            });
        }

        entity.QtyCar = entity.Details.Count;
        entity.SumTotalDiscountPrice = sumTotalDiscount;

        db.PaymentDiscounts.Add(entity);
        await db.SaveChangesAsync();

        return new
        {
            message = "Tạo đề nghị chiết khấu thanh toán thành công.",
            paymentDiscountNo = entity.PaymentDiscountNo,
            dealerCode = entity.DealerCode,
            qtyCar = entity.QtyCar,
            sumTotalDiscountPrice = entity.SumTotalDiscountPrice,
            status = entity.Status.ToString(),
            createdAt = entity.CreatedAt
        };
    }

    public async Task<object> ListAsync(string? status, string? dealer, string? discountNo, string? vin, string? guaranteeNo, DateTime? fromDate, DateTime? toDate)
    {
        var q = db.PaymentDiscounts
            .Include(x => x.Details)
            .Where(x => x.OrgId == Org);

        if (!string.IsNullOrWhiteSpace(status))
        {
            if (Enum.TryParse<PaymentDiscountStatus>(status, true, out var parsedStatus))
                q = q.Where(x => x.Status == parsedStatus);
            else if (string.Equals(status, "NotSign", StringComparison.OrdinalIgnoreCase))
                q = q.Where(x => x.Status == PaymentDiscountStatus.Draft);
            else if (string.Equals(status, "Sign", StringComparison.OrdinalIgnoreCase))
                q = q.Where(x => x.Status == PaymentDiscountStatus.Signed);
        }

        if (!string.IsNullOrWhiteSpace(dealer))
        {
            var d = dealer.Trim().ToUpperInvariant();
            q = q.Where(x => x.DealerCode.ToUpper().Contains(d) || x.DealerName.ToUpper().Contains(d));
        }

        if (!string.IsNullOrWhiteSpace(discountNo))
        {
            var dn = discountNo.Trim().ToUpperInvariant();
            q = q.Where(x => x.PaymentDiscountNo.ToUpper().Contains(dn));
        }

        if (!string.IsNullOrWhiteSpace(vin))
        {
            var v = vin.Trim().ToUpperInvariant();
            q = q.Where(x => x.Details.Any(dtl => dtl.Vin.ToUpper().Contains(v)));
        }

        if (!string.IsNullOrWhiteSpace(guaranteeNo))
        {
            var gn = guaranteeNo.Trim().ToUpperInvariant();
            q = q.Where(x => x.Details.Any(dtl => dtl.GuaranteeNo.ToUpper().Contains(gn) || (dtl.BankGuaranteeNo != null && dtl.BankGuaranteeNo.ToUpper().Contains(gn))));
        }

        if (fromDate.HasValue)
            q = q.Where(x => x.CreatedAt >= fromDate.Value);

        if (toDate.HasValue)
            q = q.Where(x => x.CreatedAt <= toDate.Value);

        var list = await q.OrderByDescending(x => x.CreatedAt).ToListAsync();

        return list.Select(x => new
        {
            x.Id,
            x.PaymentDiscountNo,
            x.DealerCode,
            x.DealerName,
            x.CompanyName,
            x.DateEndFrom,
            x.DateEndTo,
            x.QtyCar,
            x.SumTotalDiscountPrice,
            x.DiscountPercent,
            x.PenaltyPercent,
            Status = x.Status.ToString(),
            DealerSignStatus = x.DealerSignStatus.ToString(),
            x.DealerSignDate,
            x.DealerSignBy,
            x.DealerSignFile,
            HQSignStatus = x.HQSignStatus.ToString(),
            x.HQApproveDate,
            x.HQApproveBy,
            x.HQSignDate,
            x.HQSignBy,
            x.HQSignFile,
            x.RejectReason,
            x.CancelReason,
            x.Remark,
            x.CreatedBy,
            x.CreatedAt,
            Cars = x.Details.Select(d => new
            {
                d.CarId,
                d.Vin,
                d.Model,
                d.SpecCode,
                d.GuaranteeNo,
                d.BankGuaranteeNo,
                d.BankCode,
                d.UnitPriceActual,
                d.GuaranteeValue,
                d.TotalAmount,
                d.TotalDiscountPrice
            })
        });
    }

    public async Task<object?> DetailAsync(string paymentDiscountNo)
    {
        var targetNo = paymentDiscountNo.Trim().ToUpperInvariant();
        var entity = await db.PaymentDiscounts
            .Include(x => x.Details)
            .FirstOrDefaultAsync(x => x.OrgId == Org && x.PaymentDiscountNo.ToUpper() == targetNo);

        if (entity is null) return null;

        return new
        {
            entity.Id,
            entity.PaymentDiscountNo,
            entity.DealerCode,
            entity.DealerName,
            entity.CompanyName,
            entity.DateEndFrom,
            entity.DateEndTo,
            entity.QtyCar,
            entity.SumTotalDiscountPrice,
            entity.DiscountPercent,
            entity.PenaltyPercent,
            Status = entity.Status.ToString(),
            DealerSignStatus = entity.DealerSignStatus.ToString(),
            entity.DealerSignDate,
            entity.DealerSignBy,
            entity.DealerSignFile,
            HQSignStatus = entity.HQSignStatus.ToString(),
            entity.HQApproveDate,
            entity.HQApproveBy,
            entity.HQSignDate,
            entity.HQSignBy,
            entity.HQSignFile,
            entity.RejectReason,
            entity.CancelReason,
            entity.Remark,
            entity.CreatedBy,
            entity.CreatedAt,
            entity.LUDateTime,
            entity.LUBy,
            Details = entity.Details.Select(d => new
            {
                d.Id,
                d.PaymentDiscountNo,
                d.CarId,
                d.Vin,
                d.Model,
                d.SpecCode,
                d.SpecDescription,
                d.SOCode,
                d.GuaranteeNo,
                d.BankGuaranteeNo,
                d.BankCode,
                d.DateOpen,
                d.Term,
                d.DateStart,
                d.DateEnd,
                d.UnitPrice,
                d.UnitPriceActual,
                d.GuaranteeValue,
                d.PaymentEndDatePhase1,
                d.AmountPhase1,
                d.DiscountDateNumberPhase1,
                d.DiscountPercentPhase1,
                d.DiscountPricePhase1,
                d.PaymentEndDatePhase2,
                d.AmountPhase2,
                d.DiscountDateNumberPhase2,
                d.DiscountPercentPhase2,
                d.DiscountPricePhase2,
                d.PaymentEndDatePhase3,
                d.AmountPhase3,
                d.DiscountDateNumberPhase3,
                d.DiscountPercentPhase3,
                d.DiscountPricePhase3,
                d.TotalAmount,
                d.TotalDiscountPrice,
                d.Remark
            })
        };
    }

    public async Task<object> GetEligibleCarsAsync(string? dealerCode, string? guaranteeNo, string? vin)
    {
        // Tra cứu các xe đã được giải ngân bảo lãnh ngân hàng (PaymentGuaranteeDetails),
        // đã có ngày hiệu lực bảo lãnh (DateStart != null) và bảo lãnh đã được duyệt (GuaranteeStatus.Approved)
        var q = from g in db.PaymentGuarantees
                where g.OrgId == Org && g.Status == GuaranteeStatus.Approved
                from d in g.Details
                where d.DateStart != null && !string.IsNullOrEmpty(d.Vin)
                select new
                {
                    g.GuaranteeNo,
                    g.BankGuaranteeNo,
                    g.BankCode,
                    g.DealerCode,
                    g.DealerName,
                    Vin = d.Vin!,
                    d.Model,
                    d.GuaranteeValue,
                    d.DateStart,
                    d.DateEnd
                };

        if (!string.IsNullOrWhiteSpace(dealerCode))
        {
            var dc = dealerCode.Trim().ToUpperInvariant();
            q = q.Where(x => x.DealerCode.ToUpper() == dc);
        }

        if (!string.IsNullOrWhiteSpace(guaranteeNo))
        {
            var gn = guaranteeNo.Trim().ToUpperInvariant();
            q = q.Where(x => x.GuaranteeNo.ToUpper().Contains(gn) || x.BankGuaranteeNo.ToUpper().Contains(gn));
        }

        if (!string.IsNullOrWhiteSpace(vin))
        {
            var v = vin.Trim().ToUpperInvariant();
            q = q.Where(x => x.Vin.ToUpper().Contains(v));
        }

        var results = await q.ToListAsync();

        // Lấy danh sách VIN đã nằm trong các đề nghị chiết khấu đang mở (Draft / Approved / Signed)
        var existingVins = await db.PaymentDiscountDetails
            .Where(x => db.PaymentDiscounts.Any(p => p.Id == x.PaymentDiscountId && p.OrgId == Org && (p.Status == PaymentDiscountStatus.Draft || p.Status == PaymentDiscountStatus.Approved || p.Status == PaymentDiscountStatus.Signed)))
            .Select(x => x.Vin)
            .Distinct()
            .ToListAsync();

        var existingVinSet = new HashSet<string>(existingVins, StringComparer.OrdinalIgnoreCase);

        return results.Select(d => new
        {
            CarId = $"CAR-{d.Vin}",
            d.Vin,
            d.Model,
            SpecCode = (string?)null,
            ColorCode = (string?)null,
            d.GuaranteeNo,
            d.BankGuaranteeNo,
            d.BankCode,
            d.GuaranteeValue,
            d.DateStart,
            d.DateEnd,
            d.DealerCode,
            d.DealerName,
            IsAlreadyRequested = existingVinSet.Contains(d.Vin),
            EligibleForDiscount = d.DateStart.HasValue && !existingVinSet.Contains(d.Vin)
        });
    }

    public async Task<object?> UpdateAsync(string paymentDiscountNo, UpdatePaymentDiscountDto dto)
    {
        var targetNo = paymentDiscountNo.Trim().ToUpperInvariant();
        var entity = await db.PaymentDiscounts
            .Include(x => x.Details)
            .FirstOrDefaultAsync(x => x.OrgId == Org && x.PaymentDiscountNo.ToUpper() == targetNo);

        if (entity is null) return null;

        if (entity.Status != PaymentDiscountStatus.Draft)
            throw new InvalidOperationException($"Chỉ được phép cập nhật đề nghị ở trạng thái Draft. Hiện tại là {entity.Status}.");

        if (entity.DealerSignStatus != PaymentDiscountSignStatus.Pending)
            throw new InvalidOperationException("Đại lý đã ký số đề nghị, không thể sửa đổi nội dung.");

        if (dto.DateEndFrom.HasValue) entity.DateEndFrom = dto.DateEndFrom.Value;
        if (dto.DateEndTo.HasValue) entity.DateEndTo = dto.DateEndTo.Value;
        if (dto.DiscountPercent.HasValue) entity.DiscountPercent = dto.DiscountPercent.Value;
        if (dto.PenaltyPercent.HasValue) entity.PenaltyPercent = dto.PenaltyPercent.Value;
        if (dto.Remark is not null) entity.Remark = dto.Remark.Trim();

        entity.LUDateTime = DateTime.Now;
        entity.LUBy = dto.UpdatedBy ?? "DEALER_USER";

        await db.SaveChangesAsync();

        return new
        {
            message = "Cập nhật đề nghị chiết khấu thanh toán thành công.",
            entity.PaymentDiscountNo,
            entity.DiscountPercent,
            entity.DateEndFrom,
            entity.DateEndTo,
            entity.Remark
        };
    }

    public async Task<object?> SignDealerAsync(string paymentDiscountNo, SignDealerPaymentDiscountDto dto)
    {
        var targetNo = paymentDiscountNo.Trim().ToUpperInvariant();
        var entity = await db.PaymentDiscounts.FirstOrDefaultAsync(x => x.OrgId == Org && x.PaymentDiscountNo.ToUpper() == targetNo);
        if (entity is null) return null;

        if (entity.Status != PaymentDiscountStatus.Draft)
            throw new InvalidOperationException($"Không thể ký số đề nghị có trạng thái {entity.Status}. Yêu cầu trạng thái Draft.");

        if (entity.DealerSignStatus == PaymentDiscountSignStatus.Signed)
            throw new InvalidOperationException("Đại lý đã ký số đề nghị này trước đó.");

        entity.DealerSignStatus = PaymentDiscountSignStatus.Signed;
        entity.DealerSignDate = DateTime.Now;
        entity.DealerSignBy = dto.SignedBy ?? "DEALER_DIRECTOR";
        entity.DealerSignFile = string.IsNullOrWhiteSpace(dto.EContractFileName)
            ? $"DNCK_SIGNED_DL_{entity.PaymentDiscountNo.Replace("/", "_")}.pdf"
            : dto.EContractFileName.Trim();

        if (!string.IsNullOrWhiteSpace(dto.Remark))
            entity.Remark = (entity.Remark ?? "") + " | DL ký: " + dto.Remark.Trim();

        entity.LUDateTime = DateTime.Now;
        entity.LUBy = entity.DealerSignBy;

        await db.SaveChangesAsync();

        return new
        {
            message = "Đại lý ký số đề nghị chiết khấu thanh toán thành công. Đã trình NPP thẩm tra duyệt.",
            entity.PaymentDiscountNo,
            dealerSignStatus = entity.DealerSignStatus.ToString(),
            entity.DealerSignDate,
            entity.DealerSignBy,
            entity.DealerSignFile,
            status = entity.Status.ToString()
        };
    }

    public async Task<object?> ApproveHqAsync(string paymentDiscountNo, ApproveHqPaymentDiscountDto? dto)
    {
        var targetNo = paymentDiscountNo.Trim().ToUpperInvariant();
        var entity = await db.PaymentDiscounts.FirstOrDefaultAsync(x => x.OrgId == Org && x.PaymentDiscountNo.ToUpper() == targetNo);
        if (entity is null) return null;

        if (entity.Status != PaymentDiscountStatus.Draft)
            throw new InvalidOperationException($"Không thể duyệt đề nghị có trạng thái {entity.Status}. Yêu cầu trạng thái Draft.");

        if (entity.DealerSignStatus != PaymentDiscountSignStatus.Signed)
            throw new InvalidOperationException("Đại lý chưa ký số văn bản đề nghị, NPP chưa thể phê duyệt.");

        entity.Status = PaymentDiscountStatus.Approved;
        entity.HQApproveDate = DateTime.Now;
        entity.HQApproveBy = dto?.ApprovedBy ?? "NPP_FINANCE_DIRECTOR";

        if (!string.IsNullOrWhiteSpace(dto?.Remark))
            entity.Remark = (entity.Remark ?? "") + " | NPP duyệt: " + dto.Remark.Trim();

        entity.LUDateTime = DateTime.Now;
        entity.LUBy = entity.HQApproveBy;

        await db.SaveChangesAsync();

        return new
        {
            message = "NPP đã thẩm tra và phê duyệt đề nghị chiết khấu thanh toán. Chuyển cấp Lãnh đạo ký số hoàn tất.",
            entity.PaymentDiscountNo,
            status = entity.Status.ToString(),
            entity.HQApproveDate,
            entity.HQApproveBy
        };
    }

    public async Task<object?> SignHqAsync(string paymentDiscountNo, SignHqPaymentDiscountDto dto)
    {
        var targetNo = paymentDiscountNo.Trim().ToUpperInvariant();
        var entity = await db.PaymentDiscounts.FirstOrDefaultAsync(x => x.OrgId == Org && x.PaymentDiscountNo.ToUpper() == targetNo);
        if (entity is null) return null;

        if (entity.Status != PaymentDiscountStatus.Approved)
            throw new InvalidOperationException($"Chỉ đề nghị đã được NPP sơ duyệt (Approved) mới được ký số hoàn tất. Hiện tại: {entity.Status}.");

        if (entity.DealerSignStatus != PaymentDiscountSignStatus.Signed)
            throw new InvalidOperationException("Đại lý chưa hoàn tất ký số.");

        entity.Status = PaymentDiscountStatus.Signed;
        entity.HQSignStatus = PaymentDiscountSignStatus.Signed;
        entity.HQSignDate = DateTime.Now;
        entity.HQSignBy = dto.SignedBy ?? "NPP_GENERAL_DIRECTOR";
        entity.HQSignFile = string.IsNullOrWhiteSpace(dto.EContractFileName)
            ? $"DNCK_FINAL_SIGNED_{entity.PaymentDiscountNo.Replace("/", "_")}.pdf"
            : dto.EContractFileName.Trim();

        if (!string.IsNullOrWhiteSpace(dto.Remark))
            entity.Remark = (entity.Remark ?? "") + " | NPP ký số: " + dto.Remark.Trim();

        entity.LUDateTime = DateTime.Now;
        entity.LUBy = entity.HQSignBy;

        await db.SaveChangesAsync();

        return new
        {
            message = "Lãnh đạo NPP đã ký số hoàn tất quyết toán chiết khấu thanh toán. Hợp đồng quyết toán có hiệu lực pháp lý.",
            entity.PaymentDiscountNo,
            status = entity.Status.ToString(),
            hqSignStatus = entity.HQSignStatus.ToString(),
            entity.HQSignDate,
            entity.HQSignBy,
            entity.HQSignFile,
            entity.SumTotalDiscountPrice
        };
    }

    public async Task<object?> RejectHqAsync(string paymentDiscountNo, RejectPaymentDiscountDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.RejectReason))
            throw new InvalidOperationException("Lý do từ chối (RejectReason) không được để trống.");

        var targetNo = paymentDiscountNo.Trim().ToUpperInvariant();
        var entity = await db.PaymentDiscounts.FirstOrDefaultAsync(x => x.OrgId == Org && x.PaymentDiscountNo.ToUpper() == targetNo);
        if (entity is null) return null;

        if (entity.Status == PaymentDiscountStatus.Signed)
            throw new InvalidOperationException("Đề nghị đã hoàn tất ký số, không thể từ chối.");

        if (entity.Status == PaymentDiscountStatus.Cancelled)
            throw new InvalidOperationException("Đề nghị đã bị hủy trước đó.");

        entity.Status = PaymentDiscountStatus.Rejected;
        entity.RejectReason = dto.RejectReason.Trim();
        entity.LUDateTime = DateTime.Now;
        entity.LUBy = dto.RejectedBy ?? "NPP_FINANCE_DIRECTOR";

        await db.SaveChangesAsync();

        return new
        {
            message = "NPP đã từ chối phê duyệt đề nghị chiết khấu thanh toán.",
            entity.PaymentDiscountNo,
            status = entity.Status.ToString(),
            entity.RejectReason,
            rejectedAt = entity.LUDateTime
        };
    }

    public async Task<object?> CancelAsync(string paymentDiscountNo, CancelPaymentDiscountDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.CancelReason))
            throw new InvalidOperationException("Lý do hủy (CancelReason) không được để trống.");

        var targetNo = paymentDiscountNo.Trim().ToUpperInvariant();
        var entity = await db.PaymentDiscounts.FirstOrDefaultAsync(x => x.OrgId == Org && x.PaymentDiscountNo.ToUpper() == targetNo);
        if (entity is null) return null;

        if (entity.Status == PaymentDiscountStatus.Signed)
            throw new InvalidOperationException("Đề nghị đã hoàn tất ký số hai bên, không thể hủy.");

        entity.Status = PaymentDiscountStatus.Cancelled;
        entity.CancelReason = dto.CancelReason.Trim();
        entity.LUDateTime = DateTime.Now;
        entity.LUBy = dto.CancelledBy ?? "USER_CANCEL";

        await db.SaveChangesAsync();

        return new
        {
            message = "Đề nghị chiết khấu thanh toán đã được hủy thành công.",
            entity.PaymentDiscountNo,
            status = entity.Status.ToString(),
            entity.CancelReason,
            cancelledAt = entity.LUDateTime
        };
    }

    public async Task<object?> DeleteDraftAsync(string paymentDiscountNo)
    {
        var targetNo = paymentDiscountNo.Trim().ToUpperInvariant();
        var entity = await db.PaymentDiscounts
            .Include(x => x.Details)
            .FirstOrDefaultAsync(x => x.OrgId == Org && x.PaymentDiscountNo.ToUpper() == targetNo);

        if (entity is null) return null;

        if (entity.Status != PaymentDiscountStatus.Draft)
            throw new InvalidOperationException($"Chỉ được phép xóa đề nghị khi ở trạng thái Draft. Hiện tại là {entity.Status}.");

        if (entity.DealerSignStatus != PaymentDiscountSignStatus.Pending)
            throw new InvalidOperationException("Đại lý đã ký số đề nghị, không thể xóa trực tiếp.");

        db.PaymentDiscounts.Remove(entity);
        await db.SaveChangesAsync();

        return new
        {
            message = $"Đã xóa thành công đề nghị chiết khấu thanh toán {paymentDiscountNo} và các dòng xe chi tiết.",
            paymentDiscountNo
        };
    }

    public async Task<object?> AddCarAsync(string paymentDiscountNo, AddCarToPaymentDiscountDto dto)
    {
        var targetNo = paymentDiscountNo.Trim().ToUpperInvariant();
        var entity = await db.PaymentDiscounts
            .Include(x => x.Details)
            .FirstOrDefaultAsync(x => x.OrgId == Org && x.PaymentDiscountNo.ToUpper() == targetNo);

        if (entity is null) return null;

        if (entity.Status != PaymentDiscountStatus.Draft)
            throw new InvalidOperationException($"Chỉ được phép thêm xe khi đề nghị ở trạng thái Draft. Hiện tại là {entity.Status}.");

        if (entity.DealerSignStatus != PaymentDiscountSignStatus.Pending)
            throw new InvalidOperationException("Đại lý đã ký số đề nghị, không thể sửa đổi danh sách xe.");

        if (string.IsNullOrWhiteSpace(dto.Vin))
            throw new InvalidOperationException("Số khung VIN không được để trống.");

        var vin = dto.Vin.Trim().ToUpperInvariant();
        if (entity.Details.Any(x => x.Vin.Equals(vin, StringComparison.OrdinalIgnoreCase)))
            throw new InvalidOperationException($"Số khung VIN {vin} đã tồn tại trong đề nghị này.");

        var actualPrice = dto.UnitPriceActual ?? dto.UnitPrice;
        var grtValue = dto.GuaranteeValue ?? actualPrice;
        var defaultPct = entity.DiscountPercent;

        var amt1 = dto.AmountPhase1 ?? grtValue;
        var days1 = dto.DiscountDateNumberPhase1 ?? 10;
        var pct1 = dto.DiscountPercentPhase1 ?? defaultPct;
        var disc1 = dto.DiscountPricePhase1 ?? Math.Round(amt1 * pct1 / 100m, 0);

        var amt2 = dto.AmountPhase2 ?? 0m;
        var days2 = dto.DiscountDateNumberPhase2 ?? 0;
        var pct2 = dto.DiscountPercentPhase2 ?? 0m;
        var disc2 = dto.DiscountPricePhase2 ?? Math.Round(amt2 * pct2 / 100m, 0);

        var amt3 = dto.AmountPhase3 ?? 0m;
        var days3 = dto.DiscountDateNumberPhase3 ?? 0;
        var pct3 = dto.DiscountPercentPhase3 ?? 0m;
        var disc3 = dto.DiscountPricePhase3 ?? Math.Round(amt3 * pct3 / 100m, 0);

        var totalAmt = amt1 + amt2 + amt3;
        var lineTotalDiscount = disc1 + disc2 + disc3;

        var newDtl = new PaymentDiscountDetail
        {
            PaymentDiscountNo = entity.PaymentDiscountNo,
            CarId = string.IsNullOrWhiteSpace(dto.CarId) ? $"CAR-{vin}" : dto.CarId.Trim(),
            Vin = vin,
            Model = dto.Model.Trim(),
            SpecCode = dto.SpecCode?.Trim(),
            SpecDescription = dto.SpecDescription?.Trim(),
            SOCode = dto.SOCode?.Trim(),
            GuaranteeNo = string.IsNullOrWhiteSpace(dto.GuaranteeNo) ? $"BG-{vin}" : dto.GuaranteeNo.Trim(),
            BankGuaranteeNo = dto.BankGuaranteeNo?.Trim(),
            BankCode = dto.BankCode?.Trim().ToUpperInvariant(),
            DateOpen = dto.DateOpen,
            Term = dto.Term ?? 30,
            DateStart = dto.DateStart,
            DateEnd = dto.DateEnd,
            UnitPrice = dto.UnitPrice,
            UnitPriceActual = actualPrice,
            GuaranteeValue = grtValue,
            PaymentEndDatePhase1 = dto.PaymentEndDatePhase1 ?? DateTime.Today.AddDays(10),
            AmountPhase1 = amt1,
            DiscountDateNumberPhase1 = days1,
            DiscountPercentPhase1 = pct1,
            DiscountPricePhase1 = disc1,
            PaymentEndDatePhase2 = dto.PaymentEndDatePhase2,
            AmountPhase2 = amt2,
            DiscountDateNumberPhase2 = days2,
            DiscountPercentPhase2 = pct2,
            DiscountPricePhase2 = disc2,
            PaymentEndDatePhase3 = dto.PaymentEndDatePhase3,
            AmountPhase3 = amt3,
            DiscountDateNumberPhase3 = days3,
            DiscountPercentPhase3 = pct3,
            DiscountPricePhase3 = disc3,
            TotalAmount = totalAmt,
            TotalDiscountPrice = lineTotalDiscount,
            Remark = dto.Remark?.Trim()
        };

        entity.Details.Add(newDtl);
        entity.QtyCar = entity.Details.Count;
        entity.SumTotalDiscountPrice = entity.Details.Sum(x => x.TotalDiscountPrice);
        entity.LUDateTime = DateTime.Now;

        await db.SaveChangesAsync();

        return new
        {
            message = $"Đã thêm xe VIN {vin} vào đề nghị chiết khấu {entity.PaymentDiscountNo}.",
            entity.PaymentDiscountNo,
            addedVin = vin,
            qtyCar = entity.QtyCar,
            sumTotalDiscountPrice = entity.SumTotalDiscountPrice
        };
    }

    public async Task<object?> RemoveCarAsync(string paymentDiscountNo, string vin)
    {
        var targetNo = paymentDiscountNo.Trim().ToUpperInvariant();
        var entity = await db.PaymentDiscounts
            .Include(x => x.Details)
            .FirstOrDefaultAsync(x => x.OrgId == Org && x.PaymentDiscountNo.ToUpper() == targetNo);

        if (entity is null) return null;

        if (entity.Status != PaymentDiscountStatus.Draft)
            throw new InvalidOperationException($"Chỉ được phép gỡ xe khi đề nghị ở trạng thái Draft. Hiện tại là {entity.Status}.");

        if (entity.DealerSignStatus != PaymentDiscountSignStatus.Pending)
            throw new InvalidOperationException("Đại lý đã ký số đề nghị, không thể gỡ xe.");

        var targetVin = vin.Trim().ToUpperInvariant();
        var dtl = entity.Details.FirstOrDefault(x => x.Vin.Equals(targetVin, StringComparison.OrdinalIgnoreCase));
        if (dtl is null)
            throw new InvalidOperationException($"Không tìm thấy xe VIN {targetVin} trong đề nghị {entity.PaymentDiscountNo}.");

        if (entity.Details.Count <= 1)
            throw new InvalidOperationException("Đề nghị phải có ít nhất 1 xe. Nếu muốn hủy bỏ vui lòng xóa toàn bộ đề nghị.");

        entity.Details.Remove(dtl);
        entity.QtyCar = entity.Details.Count;
        entity.SumTotalDiscountPrice = entity.Details.Sum(x => x.TotalDiscountPrice);
        entity.LUDateTime = DateTime.Now;

        await db.SaveChangesAsync();

        return new
        {
            message = $"Đã gỡ bỏ xe VIN {targetVin} khỏi đề nghị chiết khấu {entity.PaymentDiscountNo}.",
            entity.PaymentDiscountNo,
            removedVin = targetVin,
            qtyCar = entity.QtyCar,
            sumTotalDiscountPrice = entity.SumTotalDiscountPrice
        };
    }

    public async Task<object?> UpdateLineAsync(string paymentDiscountNo, string vin, UpdatePaymentDiscountLineDto dto)
    {
        var targetNo = paymentDiscountNo.Trim().ToUpperInvariant();
        var entity = await db.PaymentDiscounts
            .Include(x => x.Details)
            .FirstOrDefaultAsync(x => x.OrgId == Org && x.PaymentDiscountNo.ToUpper() == targetNo);

        if (entity is null) return null;

        if (entity.Status != PaymentDiscountStatus.Draft)
            throw new InvalidOperationException($"Chỉ được phép sửa đổi dòng xe khi đề nghị ở trạng thái Draft. Hiện tại là {entity.Status}.");

        if (entity.DealerSignStatus != PaymentDiscountSignStatus.Pending)
            throw new InvalidOperationException("Đại lý đã ký số đề nghị, không thể sửa đổi dòng xe.");

        var targetVin = vin.Trim().ToUpperInvariant();
        var dtl = entity.Details.FirstOrDefault(x => x.Vin.Equals(targetVin, StringComparison.OrdinalIgnoreCase));
        if (dtl is null)
            throw new InvalidOperationException($"Không tìm thấy xe VIN {targetVin} trong đề nghị {entity.PaymentDiscountNo}.");

        if (dto.PaymentEndDatePhase1.HasValue) dtl.PaymentEndDatePhase1 = dto.PaymentEndDatePhase1.Value;
        if (dto.AmountPhase1.HasValue) dtl.AmountPhase1 = dto.AmountPhase1.Value;
        if (dto.DiscountDateNumberPhase1.HasValue) dtl.DiscountDateNumberPhase1 = dto.DiscountDateNumberPhase1.Value;
        if (dto.DiscountPercentPhase1.HasValue) dtl.DiscountPercentPhase1 = dto.DiscountPercentPhase1.Value;
        dtl.DiscountPricePhase1 = Math.Round(dtl.AmountPhase1 * dtl.DiscountPercentPhase1 / 100m, 0);

        if (dto.PaymentEndDatePhase2.HasValue) dtl.PaymentEndDatePhase2 = dto.PaymentEndDatePhase2.Value;
        if (dto.AmountPhase2.HasValue) dtl.AmountPhase2 = dto.AmountPhase2.Value;
        if (dto.DiscountDateNumberPhase2.HasValue) dtl.DiscountDateNumberPhase2 = dto.DiscountDateNumberPhase2.Value;
        if (dto.DiscountPercentPhase2.HasValue) dtl.DiscountPercentPhase2 = dto.DiscountPercentPhase2.Value;
        dtl.DiscountPricePhase2 = Math.Round(dtl.AmountPhase2 * dtl.DiscountPercentPhase2 / 100m, 0);

        if (dto.PaymentEndDatePhase3.HasValue) dtl.PaymentEndDatePhase3 = dto.PaymentEndDatePhase3.Value;
        if (dto.AmountPhase3.HasValue) dtl.AmountPhase3 = dto.AmountPhase3.Value;
        if (dto.DiscountDateNumberPhase3.HasValue) dtl.DiscountDateNumberPhase3 = dto.DiscountDateNumberPhase3.Value;
        if (dto.DiscountPercentPhase3.HasValue) dtl.DiscountPercentPhase3 = dto.DiscountPercentPhase3.Value;
        dtl.DiscountPricePhase3 = Math.Round(dtl.AmountPhase3 * dtl.DiscountPercentPhase3 / 100m, 0);

        if (dto.Remark is not null) dtl.Remark = dto.Remark.Trim();

        dtl.TotalAmount = dtl.AmountPhase1 + dtl.AmountPhase2 + dtl.AmountPhase3;
        dtl.TotalDiscountPrice = dtl.DiscountPricePhase1 + dtl.DiscountPricePhase2 + dtl.DiscountPricePhase3;

        entity.SumTotalDiscountPrice = entity.Details.Sum(x => x.TotalDiscountPrice);
        entity.LUDateTime = DateTime.Now;

        await db.SaveChangesAsync();

        return new
        {
            message = $"Đã cập nhật các đợt thanh toán và chiết khấu cho xe VIN {targetVin}.",
            entity.PaymentDiscountNo,
            vin = dtl.Vin,
            dtl.TotalAmount,
            dtl.TotalDiscountPrice,
            sumTotalDiscountPrice = entity.SumTotalDiscountPrice
        };
    }

    public async Task<object> StatsAsync()
    {
        var all = await db.PaymentDiscounts
            .Where(x => x.OrgId == Org)
            .ToListAsync();

        var total = all.Count;
        var draft = all.Count(x => x.Status == PaymentDiscountStatus.Draft);
        var approved = all.Count(x => x.Status == PaymentDiscountStatus.Approved);
        var signed = all.Count(x => x.Status == PaymentDiscountStatus.Signed);
        var rejected = all.Count(x => x.Status == PaymentDiscountStatus.Rejected);
        var cancelled = all.Count(x => x.Status == PaymentDiscountStatus.Cancelled);
        var totalCars = all.Sum(x => x.QtyCar);
        var totalDiscount = all.Where(x => x.Status == PaymentDiscountStatus.Signed || x.Status == PaymentDiscountStatus.Approved).Sum(x => x.SumTotalDiscountPrice);
        var distinctDealers = all.Select(x => x.DealerCode).Distinct(StringComparer.OrdinalIgnoreCase).Count();

        return new PaymentDiscountStatsDto(
            total,
            draft,
            approved,
            signed,
            rejected,
            cancelled,
            totalCars,
            totalDiscount,
            distinctDealers
        );
    }
}
