using Microsoft.EntityFrameworkCore;
using MiniSales.Data;
using MiniSales.Models;

namespace MiniSales.Services;

public record CreateDocRequestDetailDto(
    string Vin,
    string Model,
    string? EngineNo,
    string? OrderNo,
    string? DeliveryOrderNo,
    string? BankCode,
    string? BankGuaranteeNo,
    string? DocumentsStatus,
    DateTime? ExpectedDate,
    string? RecipientName,
    string? RecipientPhone,
    string? Remark
);

public record CreateCarDocRequestDto(
    string? DRListCode,
    string DealerCode,
    string? DealerName,
    string? RequestType, // "Dealer" | "Normal" | "Special"
    string? LetterRepresentationNo,
    DateTime? LetterRepresentationDate,
    int? LoanSupportDay,
    string? Remark,
    List<CreateDocRequestDetailDto>? Details
);

public record Approve1DocRequestDto(
    string? Remark
);

public record Approve2DocRequestDto(
    DateTime? HandoverDate,
    string? RecipientName,
    string? RecipientPhone,
    string? Remark
);

public record RejectDocRequestDto(
    string Reason
);

public record CancelDocRequestDto(
    string? Reason
);

public record AddCarDocRequestItemDto(
    string Vin,
    string Model,
    string? EngineNo,
    string? OrderNo,
    string? DeliveryOrderNo,
    string? BankCode,
    string? BankGuaranteeNo,
    string? DocumentsStatus,
    DateTime? ExpectedDate,
    string? RecipientName,
    string? RecipientPhone,
    string? Remark
);

public interface ICarDocRequestService
{
    Task<object> CreateAsync(CreateCarDocRequestDto dto);
    Task<object> ListAsync(string? status, string? dealer, string? requestType);
    Task<object?> DetailAsync(string drCode);
    Task<object?> Approve1Async(string drCode, Approve1DocRequestDto? dto);
    Task<object?> Approve2Async(string drCode, Approve2DocRequestDto? dto);
    Task<object?> RejectAsync(string drCode, RejectDocRequestDto dto);
    Task<object?> CancelAsync(string drCode, CancelDocRequestDto? dto);
    Task<object?> AddCarAsync(string drCode, AddCarDocRequestItemDto dto);
    Task<object?> RemoveCarAsync(string drCode, long detailId);
    Task<object> StatsAsync();
}

public sealed class CarDocRequestService(AppDbContext db, ITenantContext tenant) : ICarDocRequestService
{
    private Guid Org => tenant.OrgId;

    public async Task<object> CreateAsync(CreateCarDocRequestDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.DealerCode))
            throw new InvalidOperationException("DealerCode (mã đại lý) không được để trống.");
        if (dto.LoanSupportDay.HasValue && dto.LoanSupportDay.Value < 0)
            throw new InvalidOperationException("LoanSupportDay (số ngày hỗ trợ vay) không được âm.");
        if (dto.Details is null || dto.Details.Count == 0)
            throw new InvalidOperationException("Đề nghị giao hồ sơ xe phải có ít nhất 1 xe trong danh sách.");

        var reqType = dto.RequestType?.Trim().ToLowerInvariant() switch
        {
            "normal" => DocReqType.Normal,
            "special" => DocReqType.Special,
            _ => DocReqType.Dealer
        };

        var drCode = string.IsNullOrWhiteSpace(dto.DRListCode)
            ? "DNGT" + DateTime.Now.ToString("yyMMddHHmmss")
            : dto.DRListCode.Trim().ToUpperInvariant();

        if (await db.CarDocRequests.AnyAsync(x => x.OrgId == Org && x.DRListCode == drCode))
            throw new InvalidOperationException($"Mã đề nghị giao hồ sơ {drCode} đã tồn tại trong hệ thống.");

        var details = new List<CarDocRequestDetail>();
        var seenVins = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var d in dto.Details)
        {
            if (string.IsNullOrWhiteSpace(d.Vin))
                throw new InvalidOperationException("Số VIN của xe trong danh sách không được để trống.");
            if (string.IsNullOrWhiteSpace(d.Model))
                throw new InvalidOperationException($"Tên Model xe (VIN: {d.Vin}) không được để trống.");

            var vinTrimmed = d.Vin.Trim().ToUpperInvariant();
            if (!seenVins.Add(vinTrimmed))
                throw new InvalidOperationException($"Số khung VIN {vinTrimmed} bị trùng lặp trong cùng đề nghị giao hồ sơ.");

            details.Add(new CarDocRequestDetail
            {
                Vin = vinTrimmed,
                Model = d.Model.Trim(),
                EngineNo = d.EngineNo?.Trim(),
                OrderNo = d.OrderNo?.Trim(),
                DeliveryOrderNo = d.DeliveryOrderNo?.Trim(),
                BankCode = d.BankCode?.Trim().ToUpperInvariant(),
                BankGuaranteeNo = d.BankGuaranteeNo?.Trim(),
                DocumentsStatus = string.IsNullOrWhiteSpace(d.DocumentsStatus) ? "Full" : d.DocumentsStatus.Trim(),
                ExpectedDate = d.ExpectedDate,
                RecipientName = d.RecipientName?.Trim(),
                RecipientPhone = d.RecipientPhone?.Trim(),
                Remark = d.Remark?.Trim()
            });
        }

        var request = new CarDocRequest
        {
            OrgId = Org,
            DRListCode = drCode,
            DealerCode = dto.DealerCode.Trim().ToUpperInvariant(),
            DealerName = string.IsNullOrWhiteSpace(dto.DealerName) ? dto.DealerCode.Trim() : dto.DealerName.Trim(),
            RequestType = reqType,
            Status = DocReqStatus.Pending,
            LetterRepresentationNo = dto.LetterRepresentationNo?.Trim(),
            LetterRepresentationDate = dto.LetterRepresentationDate,
            LoanSupportDay = dto.LoanSupportDay ?? 0,
            TotalCars = details.Count,
            Remark = dto.Remark?.Trim(),
            CreatedAt = DateTime.Now,
            Details = details
        };

        db.CarDocRequests.Add(request);
        await db.SaveChangesAsync();

        return new
        {
            request.Id,
            request.DRListCode,
            request.DealerCode,
            request.DealerName,
            RequestType = request.RequestType.ToString(),
            Status = request.Status.ToString(),
            request.TotalCars,
            request.CreatedAt
        };
    }

    public async Task<object> ListAsync(string? status, string? dealer, string? requestType)
    {
        var q = db.CarDocRequests.AsNoTracking().Where(x => x.OrgId == Org);

        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<DocReqStatus>(status, true, out var st))
            q = q.Where(x => x.Status == st);

        if (!string.IsNullOrWhiteSpace(dealer))
        {
            var d = dealer.Trim().ToUpperInvariant();
            q = q.Where(x => x.DealerCode.ToUpper().Contains(d) || x.DealerName.ToUpper().Contains(d));
        }

        if (!string.IsNullOrWhiteSpace(requestType) && Enum.TryParse<DocReqType>(requestType, true, out var rt))
            q = q.Where(x => x.RequestType == rt);

        var items = await q.OrderByDescending(x => x.CreatedAt)
            .Select(x => new
            {
                x.Id,
                x.DRListCode,
                x.DealerCode,
                x.DealerName,
                RequestType = x.RequestType.ToString(),
                Status = x.Status.ToString(),
                x.LetterRepresentationNo,
                x.LetterRepresentationDate,
                x.LoanSupportDay,
                x.TotalCars,
                x.Remark,
                x.RejectReason,
                x.CreatedAt,
                x.Approved1At,
                x.Approved2At,
                x.CancelledAt
            })
            .ToListAsync();

        return items;
    }

    public async Task<object?> DetailAsync(string drCode)
    {
        var code = drCode.Trim().ToUpperInvariant();
        var req = await db.CarDocRequests.AsNoTracking()
            .Include(x => x.Details)
            .FirstOrDefaultAsync(x => x.OrgId == Org && x.DRListCode == code);

        if (req is null) return null;

        return new
        {
            req.Id,
            req.DRListCode,
            req.DealerCode,
            req.DealerName,
            RequestType = req.RequestType.ToString(),
            Status = req.Status.ToString(),
            req.LetterRepresentationNo,
            req.LetterRepresentationDate,
            req.LoanSupportDay,
            req.TotalCars,
            req.Remark,
            req.RejectReason,
            req.CreatedAt,
            req.Approved1At,
            req.Approved2At,
            req.CancelledAt,
            Details = req.Details.Select(d => new
            {
                d.Id,
                d.Vin,
                d.Model,
                d.EngineNo,
                d.OrderNo,
                d.DeliveryOrderNo,
                d.BankCode,
                d.BankGuaranteeNo,
                d.DocumentsStatus,
                d.ExpectedDate,
                d.HandoverDate,
                d.RecipientName,
                d.RecipientPhone,
                d.Remark
            })
        };
    }

    public async Task<object?> Approve1Async(string drCode, Approve1DocRequestDto? dto)
    {
        var code = drCode.Trim().ToUpperInvariant();
        var req = await db.CarDocRequests.FirstOrDefaultAsync(x => x.OrgId == Org && x.DRListCode == code);
        if (req is null) return null;

        if (req.Status != DocReqStatus.Pending)
            throw new InvalidOperationException($"Đề nghị {drCode} đang ở trạng thái {req.Status}, chỉ có thể duyệt cấp 1 (Approve1) khi ở trạng thái Pending.");

        req.Status = DocReqStatus.Approved1;
        req.Approved1At = DateTime.Now;
        if (!string.IsNullOrWhiteSpace(dto?.Remark))
        {
            req.Remark = string.IsNullOrWhiteSpace(req.Remark) ? dto.Remark.Trim() : $"{req.Remark} | {dto.Remark.Trim()}";
        }

        await db.SaveChangesAsync();

        return new
        {
            req.DRListCode,
            Status = req.Status.ToString(),
            req.Approved1At,
            req.Remark
        };
    }

    public async Task<object?> Approve2Async(string drCode, Approve2DocRequestDto? dto)
    {
        var code = drCode.Trim().ToUpperInvariant();
        var req = await db.CarDocRequests
            .Include(x => x.Details)
            .FirstOrDefaultAsync(x => x.OrgId == Org && x.DRListCode == code);

        if (req is null) return null;

        if (req.Status == DocReqStatus.Pending)
            throw new InvalidOperationException($"Đề nghị {drCode} cần được duyệt cấp 1 (Approve1) trước khi duyệt cấp 2 bàn giao hồ sơ.");
        if (req.Status == DocReqStatus.Approved2)
            throw new InvalidOperationException($"Đề nghị {drCode} đã hoàn tất bàn giao hồ sơ (Approved2).");
        if (req.Status == DocReqStatus.Rejected || req.Status == DocReqStatus.Cancelled)
            throw new InvalidOperationException($"Đề nghị {drCode} đã bị {req.Status}, không thể bàn giao hồ sơ.");

        req.Status = DocReqStatus.Approved2;
        req.Approved2At = DateTime.Now;

        var handoverDate = dto?.HandoverDate ?? DateTime.Today;
        foreach (var d in req.Details)
        {
            d.HandoverDate ??= handoverDate;
            if (!string.IsNullOrWhiteSpace(dto?.RecipientName) && string.IsNullOrWhiteSpace(d.RecipientName))
                d.RecipientName = dto.RecipientName.Trim();
            if (!string.IsNullOrWhiteSpace(dto?.RecipientPhone) && string.IsNullOrWhiteSpace(d.RecipientPhone))
                d.RecipientPhone = dto.RecipientPhone.Trim();
        }

        if (!string.IsNullOrWhiteSpace(dto?.Remark))
        {
            req.Remark = string.IsNullOrWhiteSpace(req.Remark) ? dto.Remark.Trim() : $"{req.Remark} | {dto.Remark.Trim()}";
        }

        await db.SaveChangesAsync();

        return new
        {
            req.DRListCode,
            Status = req.Status.ToString(),
            req.Approved2At,
            HandoverDate = handoverDate,
            req.TotalCars
        };
    }

    public async Task<object?> RejectAsync(string drCode, RejectDocRequestDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Reason))
            throw new InvalidOperationException("Cần nhập lý do từ chối (Reason) đề nghị giao hồ sơ.");

        var code = drCode.Trim().ToUpperInvariant();
        var req = await db.CarDocRequests.FirstOrDefaultAsync(x => x.OrgId == Org && x.DRListCode == code);
        if (req is null) return null;

        if (req.Status == DocReqStatus.Approved2)
            throw new InvalidOperationException($"Đề nghị {drCode} đã hoàn tất bàn giao hồ sơ, không thể từ chối.");
        if (req.Status == DocReqStatus.Cancelled)
            throw new InvalidOperationException($"Đề nghị {drCode} đã bị hủy từ trước.");

        req.Status = DocReqStatus.Rejected;
        req.RejectReason = dto.Reason.Trim();

        await db.SaveChangesAsync();

        return new
        {
            req.DRListCode,
            Status = req.Status.ToString(),
            req.RejectReason
        };
    }

    public async Task<object?> CancelAsync(string drCode, CancelDocRequestDto? dto)
    {
        var code = drCode.Trim().ToUpperInvariant();
        var req = await db.CarDocRequests.FirstOrDefaultAsync(x => x.OrgId == Org && x.DRListCode == code);
        if (req is null) return null;

        if (req.Status == DocReqStatus.Approved2)
            throw new InvalidOperationException($"Đề nghị {drCode} đã hoàn tất bàn giao hồ sơ gốc, không thể hủy.");

        req.Status = DocReqStatus.Cancelled;
        req.CancelledAt = DateTime.Now;
        if (!string.IsNullOrWhiteSpace(dto?.Reason))
            req.RejectReason = dto.Reason.Trim();

        await db.SaveChangesAsync();

        return new
        {
            req.DRListCode,
            Status = req.Status.ToString(),
            req.CancelledAt,
            Reason = req.RejectReason
        };
    }

    public async Task<object?> AddCarAsync(string drCode, AddCarDocRequestItemDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Vin))
            throw new InvalidOperationException("Số VIN không được để trống.");
        if (string.IsNullOrWhiteSpace(dto.Model))
            throw new InvalidOperationException("Model xe không được để trống.");

        var code = drCode.Trim().ToUpperInvariant();
        var req = await db.CarDocRequests
            .Include(x => x.Details)
            .FirstOrDefaultAsync(x => x.OrgId == Org && x.DRListCode == code);

        if (req is null) return null;

        if (req.Status != DocReqStatus.Pending && req.Status != DocReqStatus.Approved1)
            throw new InvalidOperationException($"Không thể thêm xe vào đề nghị đang ở trạng thái {req.Status}.");

        var vin = dto.Vin.Trim().ToUpperInvariant();
        if (req.Details.Any(d => string.Equals(d.Vin, vin, StringComparison.OrdinalIgnoreCase)))
            throw new InvalidOperationException($"Số khung VIN {vin} đã tồn tại trong đề nghị này.");

        var item = new CarDocRequestDetail
        {
            RequestId = req.Id,
            Vin = vin,
            Model = dto.Model.Trim(),
            EngineNo = dto.EngineNo?.Trim(),
            OrderNo = dto.OrderNo?.Trim(),
            DeliveryOrderNo = dto.DeliveryOrderNo?.Trim(),
            BankCode = dto.BankCode?.Trim().ToUpperInvariant(),
            BankGuaranteeNo = dto.BankGuaranteeNo?.Trim(),
            DocumentsStatus = string.IsNullOrWhiteSpace(dto.DocumentsStatus) ? "Full" : dto.DocumentsStatus.Trim(),
            ExpectedDate = dto.ExpectedDate,
            RecipientName = dto.RecipientName?.Trim(),
            RecipientPhone = dto.RecipientPhone?.Trim(),
            Remark = dto.Remark?.Trim()
        };

        req.Details.Add(item);
        req.TotalCars = req.Details.Count;

        await db.SaveChangesAsync();

        return new
        {
            req.DRListCode,
            req.TotalCars,
            AddedCar = new
            {
                item.Id,
                item.Vin,
                item.Model,
                item.DocumentsStatus
            }
        };
    }

    public async Task<object?> RemoveCarAsync(string drCode, long detailId)
    {
        var code = drCode.Trim().ToUpperInvariant();
        var req = await db.CarDocRequests
            .Include(x => x.Details)
            .FirstOrDefaultAsync(x => x.OrgId == Org && x.DRListCode == code);

        if (req is null) return null;

        if (req.Status != DocReqStatus.Pending && req.Status != DocReqStatus.Approved1)
            throw new InvalidOperationException($"Không thể xóa xe khỏi đề nghị đang ở trạng thái {req.Status}.");

        var detail = req.Details.FirstOrDefault(x => x.Id == detailId);
        if (detail is null) return null;

        if (req.Details.Count <= 1)
            throw new InvalidOperationException("Không thể xóa xe cuối cùng trong đề nghị giao hồ sơ. Đề nghị phải có ít nhất 1 xe.");

        req.Details.Remove(detail);
        db.CarDocRequestDetails.Remove(detail);
        req.TotalCars = req.Details.Count;

        await db.SaveChangesAsync();

        return new
        {
            req.DRListCode,
            req.TotalCars,
            RemovedDetailId = detailId
        };
    }

    public async Task<object> StatsAsync()
    {
        var list = await db.CarDocRequests.AsNoTracking()
            .Where(x => x.OrgId == Org)
            .Include(x => x.Details)
            .ToListAsync();

        var totalRequests = list.Count;
        var pending = list.Count(x => x.Status == DocReqStatus.Pending);
        var approved1 = list.Count(x => x.Status == DocReqStatus.Approved1);
        var approved2 = list.Count(x => x.Status == DocReqStatus.Approved2);
        var rejected = list.Count(x => x.Status == DocReqStatus.Rejected);
        var cancelled = list.Count(x => x.Status == DocReqStatus.Cancelled);

        var totalCarsInRequests = list.Sum(x => x.TotalCars);
        var totalCarsHandedOver = list.Where(x => x.Status == DocReqStatus.Approved2).Sum(x => x.TotalCars);
        var totalCarsPendingHandover = list.Where(x => x.Status == DocReqStatus.Pending || x.Status == DocReqStatus.Approved1).Sum(x => x.TotalCars);

        var typeDealerCount = list.Count(x => x.RequestType == DocReqType.Dealer);
        var typeNormalCount = list.Count(x => x.RequestType == DocReqType.Normal);
        var typeSpecialCount = list.Count(x => x.RequestType == DocReqType.Special);

        return new
        {
            TotalRequests = totalRequests,
            Pending = pending,
            Approved1 = approved1,
            Approved2 = approved2,
            Rejected = rejected,
            Cancelled = cancelled,
            TotalCarsInRequests = totalCarsInRequests,
            TotalCarsHandedOver = totalCarsHandedOver,
            TotalCarsPendingHandover = totalCarsPendingHandover,
            ByRequestType = new
            {
                Dealer = typeDealerCount,
                Normal = typeNormalCount,
                Special = typeSpecialCount
            }
        };
    }
}
