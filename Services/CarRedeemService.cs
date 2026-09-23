using Microsoft.EntityFrameworkCore;
using MiniSales.Data;
using MiniSales.Models;

namespace MiniSales.Services;

public record CreateCarRedeemDetailDto(
    string CarId,
    string Vin,
    string ModelCode = "",
    string ModelName = "",
    string? SpecCode = null,
    string? SpecDescription = null,
    string? ColorCode = null,
    string? ColorName = null,
    string? EngineNo = null,
    string? CQNo = null,
    string? CONo = null,
    string? DeclarationNo = null,
    string MortageBankCode = "",
    string? MortageBankName = null,
    CarRedeemType TypeDMReq = CarRedeemType.Direct,
    string? DealerCode = null,
    string? DealerName = null,
    string? GuaranteeNo = null,
    string? DRListCode = null,
    string? DocumentsStatus = "Full",
    string? Remark = null
);

public record CreateCarRedeemRequestDto(
    string? ReqDMNo = null,
    string DealerCode = "",
    string? DealerName = null,
    CarRedeemType RedeemType = CarRedeemType.Direct,
    string? BankCode = null,
    string? BankName = null,
    string? Remark = null,
    string? CreatedBy = null,
    List<CreateCarRedeemDetailDto>? Cars = null
);

public record UpdateCarRedeemRequestDto(
    string? DealerCode = null,
    string? DealerName = null,
    CarRedeemType? RedeemType = null,
    string? BankCode = null,
    string? BankName = null,
    string? Remark = null
);

public record ApproveRedeemDetailDto(
    string? ApprovedBy = null,
    string? Remark = null
);

public record ApproveRedeemRequestDto(
    string? ApprovedBy = null,
    string? Remark = null
);

public record RejectRedeemRequestDto(
    string Reason,
    string? RejectedBy = null
);

public record CancelRedeemRequestDto(
    string Reason,
    string? CancelledBy = null
);

public record AddCarRedeemDetailDto(
    string CarId,
    string Vin,
    string ModelCode = "",
    string ModelName = "",
    string? SpecCode = null,
    string? SpecDescription = null,
    string? ColorCode = null,
    string? ColorName = null,
    string? EngineNo = null,
    string? CQNo = null,
    string? CONo = null,
    string? DeclarationNo = null,
    string MortageBankCode = "",
    string? MortageBankName = null,
    CarRedeemType TypeDMReq = CarRedeemType.Direct,
    string? DealerCode = null,
    string? DealerName = null,
    string? GuaranteeNo = null,
    string? DRListCode = null,
    string? DocumentsStatus = "Full",
    string? Remark = null
);

public record CarRedeemDetailDto(
    long Id,
    string ReqDMNo,
    string CarId,
    string Vin,
    string ModelCode,
    string ModelName,
    string? SpecCode,
    string? SpecDescription,
    string? ColorCode,
    string? ColorName,
    string? EngineNo,
    string? CQNo,
    string? CONo,
    string? DeclarationNo,
    string MortageBankCode,
    string? MortageBankName,
    string TypeDMReq,
    string DealerCode,
    string? DealerName,
    string? GuaranteeNo,
    string? DRListCode,
    string? DocumentsStatus,
    string Status,
    DateTime? ApprovedAt,
    string? ApprovedBy,
    string? Remark
);

public record CarRedeemRequestDto(
    long Id,
    string ReqDMNo,
    string DealerCode,
    string? DealerName,
    string Status,
    string RedeemType,
    int TotalCars,
    int ApprovedCars,
    string? BankCode,
    string? BankName,
    string? Remark,
    string? CreatedBy,
    DateTime CreatedAt,
    string? ApprovedBy,
    DateTime? ApprovedAt,
    string? RejectedBy,
    DateTime? RejectedAt,
    string? RejectReason,
    string? CancelledBy,
    DateTime? CancelledAt,
    string? CancelReason,
    List<CarRedeemDetailDto> Details
);

public record EligibleRedeemCarDto(
    string CarId,
    string Vin,
    string ModelCode,
    string ModelName,
    string? SpecCode,
    string? SpecDescription,
    string? ColorCode,
    string? ColorName,
    string? EngineNo,
    string? CQNo,
    string? CONo,
    string? DeclarationNo,
    string MortageBankCode,
    string? MortageBankName,
    string DealerCode,
    string? DealerName,
    string? DRListCode,
    string? GuaranteeNo,
    string? DocumentsStatus,
    string SuggestedType
);

public record CarRedeemStatsDto(
    int TotalRequests,
    int TotalApproved,
    int TotalPending,
    int TotalRejected,
    int TotalCancelled,
    int TotalCars,
    int TotalCarsApproved,
    Dictionary<string, int> ByStatus,
    Dictionary<string, int> ByType,
    Dictionary<string, int> ByBank,
    Dictionary<string, int> ByDealer
);

public interface ICarRedeemService
{
    Task<List<CarRedeemRequestDto>> ListAsync(
        string? status,
        string? redeemType,
        string? dealerCode,
        string? bankCode,
        string? vin,
        string? reqDMNo,
        DateTime? dateFrom,
        DateTime? dateTo
    );
    Task<CarRedeemRequestDto?> GetByNoAsync(string reqDMNo);
    Task<CarRedeemRequestDto> CreateAsync(CreateCarRedeemRequestDto dto);
    Task<CarRedeemRequestDto> UpdateAsync(string reqDMNo, UpdateCarRedeemRequestDto dto);
    Task<bool> DeleteDraftAsync(string reqDMNo);
    Task<CarRedeemRequestDto> ApproveDtlAsync(string reqDMNo, string vin, ApproveRedeemDetailDto? dto);
    Task<CarRedeemRequestDto> ApproveAllAsync(string reqDMNo, ApproveRedeemRequestDto? dto);
    Task<CarRedeemRequestDto> RejectAsync(string reqDMNo, RejectRedeemRequestDto dto);
    Task<CarRedeemRequestDto> CancelAsync(string reqDMNo, CancelRedeemRequestDto dto);
    Task<CarRedeemRequestDto> AddCarAsync(string reqDMNo, AddCarRedeemDetailDto dto);
    Task<CarRedeemRequestDto?> RemoveCarAsync(string reqDMNo, string vin);
    Task<List<EligibleRedeemCarDto>> GetEligibleCarsAsync(string? dealerCode, string? bankCode);
    Task<CarRedeemStatsDto> GetStatsAsync();
}

public sealed class CarRedeemService(AppDbContext db, ITenantContext tenant) : ICarRedeemService
{
    private Guid Org => tenant.OrgId;

    public async Task<List<CarRedeemRequestDto>> ListAsync(
        string? status,
        string? redeemType,
        string? dealerCode,
        string? bankCode,
        string? vin,
        string? reqDMNo,
        DateTime? dateFrom,
        DateTime? dateTo)
    {
        var q = db.CarRedeemRequests
            .Include(x => x.Details)
            .Where(x => x.OrgId == Org);

        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<CarRedeemStatus>(status, true, out var st))
            q = q.Where(x => x.Status == st);

        if (!string.IsNullOrWhiteSpace(redeemType) && Enum.TryParse<CarRedeemType>(redeemType, true, out var rt))
            q = q.Where(x => x.RedeemType == rt);

        if (!string.IsNullOrWhiteSpace(dealerCode))
            q = q.Where(x => x.DealerCode == dealerCode.Trim());

        if (!string.IsNullOrWhiteSpace(bankCode))
            q = q.Where(x => x.BankCode == bankCode.Trim());

        if (!string.IsNullOrWhiteSpace(reqDMNo))
            q = q.Where(x => x.ReqDMNo.Contains(reqDMNo.Trim()));

        if (!string.IsNullOrWhiteSpace(vin))
            q = q.Where(x => x.Details.Any(d => d.Vin.Contains(vin.Trim())));

        if (dateFrom.HasValue)
            q = q.Where(x => x.CreatedAt >= dateFrom.Value.Date);

        if (dateTo.HasValue)
            q = q.Where(x => x.CreatedAt < dateTo.Value.Date.AddDays(1));

        var list = await q.OrderByDescending(x => x.CreatedAt).ToListAsync();
        return list.Select(ToDto).ToList();
    }

    public async Task<CarRedeemRequestDto?> GetByNoAsync(string reqDMNo)
    {
        var item = await db.CarRedeemRequests
            .Include(x => x.Details)
            .FirstOrDefaultAsync(x => x.OrgId == Org && x.ReqDMNo == reqDMNo.Trim());

        return item is null ? null : ToDto(item);
    }

    public async Task<CarRedeemRequestDto> CreateAsync(CreateCarRedeemRequestDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.DealerCode))
            throw new InvalidOperationException("Mã đại lý đề nghị không được để trống.");

        if (dto.Cars is null || dto.Cars.Count == 0)
            throw new InvalidOperationException("Danh sách xe đề nghị giải chấp không được để trống.");

        string reqNo = string.IsNullOrWhiteSpace(dto.ReqDMNo)
            ? await GenerateSeqNoAsync()
            : dto.ReqDMNo.Trim();

        if (await db.CarRedeemRequests.AnyAsync(x => x.OrgId == Org && x.ReqDMNo == reqNo))
            throw new InvalidOperationException($"Số đề nghị giải chấp '{reqNo}' đã tồn tại trên hệ thống.");

        var seenVins = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var c in dto.Cars)
        {
            if (string.IsNullOrWhiteSpace(c.Vin))
                throw new InvalidOperationException("Số khung VIN xe không được để trống.");

            if (!seenVins.Add(c.Vin.Trim()))
                throw new InvalidOperationException($"Số khung VIN '{c.Vin}' bị trùng lặp trong đề nghị giải chấp.");

            string mortBank = (c.MortageBankCode ?? dto.BankCode ?? "").Trim();
            if (string.Equals(mortBank, "HTC.HO", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException($"Số khung VIN '{c.Vin}' có MortageBankCode là 'HTC.HO' - xe đã giải chấp về NPP hoặc không thế chấp ngân hàng.");

            bool activeElsewhere = await db.CarRedeemRequestDetails
                .AnyAsync(d => d.ReqDMNo != reqNo && d.Vin == c.Vin.Trim() && d.Status != CarRedeemDtlStatus.Rejected && d.Status != CarRedeemDtlStatus.Cancelled);

            if (activeElsewhere)
                throw new InvalidOperationException($"Số khung VIN '{c.Vin}' đang nằm trong một đề nghị giải chấp khác chưa kết thúc.");
        }

        string defaultBank = dto.BankCode?.Trim() ?? dto.Cars.FirstOrDefault()?.MortageBankCode?.Trim() ?? "VCB";

        var entity = new CarRedeemRequest
        {
            OrgId = Org,
            ReqDMNo = reqNo,
            DealerCode = dto.DealerCode.Trim(),
            DealerName = dto.DealerName?.Trim(),
            Status = CarRedeemStatus.Pending,
            RedeemType = dto.RedeemType,
            TotalCars = dto.Cars.Count,
            ApprovedCars = 0,
            BankCode = defaultBank,
            BankName = dto.BankName?.Trim(),
            Remark = dto.Remark?.Trim(),
            CreatedBy = dto.CreatedBy?.Trim() ?? "DEALER_USER",
            CreatedAt = DateTime.Now,
            Details = dto.Cars.Select(c => new CarRedeemRequestDetail
            {
                ReqDMNo = reqNo,
                CarId = string.IsNullOrWhiteSpace(c.CarId) ? $"CAR-{c.Vin.Trim()}" : c.CarId.Trim(),
                Vin = c.Vin.Trim(),
                ModelCode = c.ModelCode?.Trim() ?? "",
                ModelName = c.ModelName?.Trim() ?? "",
                SpecCode = c.SpecCode?.Trim(),
                SpecDescription = c.SpecDescription?.Trim(),
                ColorCode = c.ColorCode?.Trim(),
                ColorName = c.ColorName?.Trim(),
                EngineNo = c.EngineNo?.Trim(),
                CQNo = c.CQNo?.Trim(),
                CONo = c.CONo?.Trim(),
                DeclarationNo = c.DeclarationNo?.Trim(),
                MortageBankCode = string.IsNullOrWhiteSpace(c.MortageBankCode) ? defaultBank : c.MortageBankCode.Trim(),
                MortageBankName = c.MortageBankName?.Trim(),
                TypeDMReq = c.TypeDMReq,
                DealerCode = string.IsNullOrWhiteSpace(c.DealerCode) ? dto.DealerCode.Trim() : c.DealerCode.Trim(),
                DealerName = c.DealerName?.Trim() ?? dto.DealerName?.Trim(),
                GuaranteeNo = c.GuaranteeNo?.Trim(),
                DRListCode = c.DRListCode?.Trim(),
                DocumentsStatus = c.DocumentsStatus?.Trim() ?? "Full",
                Status = CarRedeemDtlStatus.Pending,
                Remark = c.Remark?.Trim()
            }).ToList()
        };

        db.CarRedeemRequests.Add(entity);
        await db.SaveChangesAsync();

        return ToDto(entity);
    }

    public async Task<CarRedeemRequestDto> UpdateAsync(string reqDMNo, UpdateCarRedeemRequestDto dto)
    {
        var item = await db.CarRedeemRequests
            .Include(x => x.Details)
            .FirstOrDefaultAsync(x => x.OrgId == Org && x.ReqDMNo == reqDMNo.Trim())
            ?? throw new InvalidOperationException($"Không tìm thấy đề nghị giải chấp '{reqDMNo}'.");

        if (item.Status != CarRedeemStatus.Pending)
            throw new InvalidOperationException($"Chỉ có thể sửa đề nghị giải chấp ở trạng thái Chờ duyệt (Pending). Trạng thái hiện tại: {item.Status}.");

        if (!string.IsNullOrWhiteSpace(dto.DealerCode)) item.DealerCode = dto.DealerCode.Trim();
        if (dto.DealerName != null) item.DealerName = dto.DealerName.Trim();
        if (dto.RedeemType.HasValue) item.RedeemType = dto.RedeemType.Value;
        if (dto.BankCode != null) item.BankCode = dto.BankCode.Trim();
        if (dto.BankName != null) item.BankName = dto.BankName.Trim();
        if (dto.Remark != null) item.Remark = dto.Remark.Trim();

        await db.SaveChangesAsync();
        return ToDto(item);
    }

    public async Task<bool> DeleteDraftAsync(string reqDMNo)
    {
        var item = await db.CarRedeemRequests
            .Include(x => x.Details)
            .FirstOrDefaultAsync(x => x.OrgId == Org && x.ReqDMNo == reqDMNo.Trim())
            ?? throw new InvalidOperationException($"Không tìm thấy đề nghị giải chấp '{reqDMNo}'.");

        if (item.Status != CarRedeemStatus.Pending)
            throw new InvalidOperationException($"Chỉ có thể xóa đề nghị giải chấp ở trạng thái Chờ duyệt (Pending). Trạng thái hiện tại: {item.Status}.");

        db.CarRedeemRequests.Remove(item);
        await db.SaveChangesAsync();
        return true;
    }

    public async Task<CarRedeemRequestDto> ApproveDtlAsync(string reqDMNo, string vin, ApproveRedeemDetailDto? dto)
    {
        var item = await db.CarRedeemRequests
            .Include(x => x.Details)
            .FirstOrDefaultAsync(x => x.OrgId == Org && x.ReqDMNo == reqDMNo.Trim())
            ?? throw new InvalidOperationException($"Không tìm thấy đề nghị giải chấp '{reqDMNo}'.");

        if (item.Status == CarRedeemStatus.Cancelled || item.Status == CarRedeemStatus.Rejected)
            throw new InvalidOperationException($"Không thể duyệt chi tiết cho đề nghị đã hủy hoặc từ chối.");

        var dtl = item.Details.FirstOrDefault(x => string.Equals(x.Vin, vin.Trim(), StringComparison.OrdinalIgnoreCase))
            ?? throw new InvalidOperationException($"Không tìm thấy xe VIN '{vin}' trong đề nghị giải chấp '{reqDMNo}'.");

        if (dtl.Status == CarRedeemDtlStatus.Approved)
            throw new InvalidOperationException($"Xe VIN '{vin}' đã được duyệt giải chấp trước đó.");

        string approver = dto?.ApprovedBy?.Trim() ?? "HTC_SUPERVISOR";
        DateTime now = DateTime.Now;

        dtl.Status = CarRedeemDtlStatus.Approved;
        dtl.ApprovedAt = now;
        dtl.ApprovedBy = approver;
        dtl.MortageBankCode = "HTC.HO"; // Đổi MortageBankCode về HTC.HO (giải phóng thế chấp theo quy chuẩn DMS.Sales)
        if (!string.IsNullOrWhiteSpace(dto?.Remark))
            dtl.Remark = (dtl.Remark is null ? "" : dtl.Remark + " | ") + dto.Remark.Trim();

        item.ApprovedCars = item.Details.Count(x => x.Status == CarRedeemDtlStatus.Approved);

        // Nếu tất cả các xe trong đề nghị đã được duyệt giải chấp -> chuyển cha sang Approved
        if (item.ApprovedCars == item.Details.Count)
        {
            item.Status = CarRedeemStatus.Approved;
            item.ApprovedAt = now;
            item.ApprovedBy = approver;
        }

        await db.SaveChangesAsync();
        return ToDto(item);
    }

    public async Task<CarRedeemRequestDto> ApproveAllAsync(string reqDMNo, ApproveRedeemRequestDto? dto)
    {
        var item = await db.CarRedeemRequests
            .Include(x => x.Details)
            .FirstOrDefaultAsync(x => x.OrgId == Org && x.ReqDMNo == reqDMNo.Trim())
            ?? throw new InvalidOperationException($"Không tìm thấy đề nghị giải chấp '{reqDMNo}'.");

        if (item.Status == CarRedeemStatus.Cancelled || item.Status == CarRedeemStatus.Rejected)
            throw new InvalidOperationException($"Không thể duyệt đề nghị đã hủy hoặc từ chối.");

        string approver = dto?.ApprovedBy?.Trim() ?? "HTC_SUPERVISOR";
        DateTime now = DateTime.Now;

        foreach (var dtl in item.Details.Where(x => x.Status == CarRedeemDtlStatus.Pending))
        {
            dtl.Status = CarRedeemDtlStatus.Approved;
            dtl.ApprovedAt = now;
            dtl.ApprovedBy = approver;
            dtl.MortageBankCode = "HTC.HO";
        }

        item.ApprovedCars = item.Details.Count(x => x.Status == CarRedeemDtlStatus.Approved);
        item.Status = CarRedeemStatus.Approved;
        item.ApprovedAt = now;
        item.ApprovedBy = approver;
        if (!string.IsNullOrWhiteSpace(dto?.Remark))
            item.Remark = (item.Remark is null ? "" : item.Remark + " | ") + dto.Remark.Trim();

        await db.SaveChangesAsync();
        return ToDto(item);
    }

    public async Task<CarRedeemRequestDto> RejectAsync(string reqDMNo, RejectRedeemRequestDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Reason))
            throw new InvalidOperationException("Bắt buộc phải nhập lý do từ chối đề nghị giải chấp.");

        var item = await db.CarRedeemRequests
            .Include(x => x.Details)
            .FirstOrDefaultAsync(x => x.OrgId == Org && x.ReqDMNo == reqDMNo.Trim())
            ?? throw new InvalidOperationException($"Không tìm thấy đề nghị giải chấp '{reqDMNo}'.");

        if (item.Status != CarRedeemStatus.Pending)
            throw new InvalidOperationException($"Chỉ có thể từ chối đề nghị giải chấp ở trạng thái Chờ duyệt (Pending). Trạng thái hiện tại: {item.Status}.");

        DateTime now = DateTime.Now;
        string rejecter = dto.RejectedBy?.Trim() ?? "HTC_SUPERVISOR";

        item.Status = CarRedeemStatus.Rejected;
        item.RejectedAt = now;
        item.RejectedBy = rejecter;
        item.RejectReason = dto.Reason.Trim();

        foreach (var dtl in item.Details.Where(x => x.Status == CarRedeemDtlStatus.Pending))
        {
            dtl.Status = CarRedeemDtlStatus.Rejected;
            dtl.Remark = (dtl.Remark is null ? "" : dtl.Remark + " | ") + $"Từ chối: {dto.Reason.Trim()}";
        }

        await db.SaveChangesAsync();
        return ToDto(item);
    }

    public async Task<CarRedeemRequestDto> CancelAsync(string reqDMNo, CancelRedeemRequestDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Reason))
            throw new InvalidOperationException("Bắt buộc phải nhập lý do hủy đề nghị giải chấp.");

        var item = await db.CarRedeemRequests
            .Include(x => x.Details)
            .FirstOrDefaultAsync(x => x.OrgId == Org && x.ReqDMNo == reqDMNo.Trim())
            ?? throw new InvalidOperationException($"Không tìm thấy đề nghị giải chấp '{reqDMNo}'.");

        if (item.Status == CarRedeemStatus.Approved)
            throw new InvalidOperationException("Không thể hủy đề nghị giải chấp đã được duyệt hoàn tất (Approved).");

        DateTime now = DateTime.Now;
        string canceller = dto.CancelledBy?.Trim() ?? "DEALER_USER";

        item.Status = CarRedeemStatus.Cancelled;
        item.CancelledAt = now;
        item.CancelledBy = canceller;
        item.CancelReason = dto.Reason.Trim();

        foreach (var dtl in item.Details.Where(x => x.Status == CarRedeemDtlStatus.Pending))
        {
            dtl.Status = CarRedeemDtlStatus.Cancelled;
            dtl.Remark = (dtl.Remark is null ? "" : dtl.Remark + " | ") + $"Hủy: {dto.Reason.Trim()}";
        }

        await db.SaveChangesAsync();
        return ToDto(item);
    }

    public async Task<CarRedeemRequestDto> AddCarAsync(string reqDMNo, AddCarRedeemDetailDto dto)
    {
        var item = await db.CarRedeemRequests
            .Include(x => x.Details)
            .FirstOrDefaultAsync(x => x.OrgId == Org && x.ReqDMNo == reqDMNo.Trim())
            ?? throw new InvalidOperationException($"Không tìm thấy đề nghị giải chấp '{reqDMNo}'.");

        if (item.Status != CarRedeemStatus.Pending)
            throw new InvalidOperationException($"Chỉ có thể thêm xe vào đề nghị giải chấp ở trạng thái Chờ duyệt (Pending).");

        if (string.IsNullOrWhiteSpace(dto.Vin))
            throw new InvalidOperationException("Số khung VIN xe không được để trống.");

        if (item.Details.Any(x => string.Equals(x.Vin, dto.Vin.Trim(), StringComparison.OrdinalIgnoreCase)))
            throw new InvalidOperationException($"Số khung VIN '{dto.Vin}' đã có trong đề nghị giải chấp này.");

        string mortBank = (dto.MortageBankCode ?? item.BankCode ?? "").Trim();
        if (string.Equals(mortBank, "HTC.HO", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException($"Số khung VIN '{dto.Vin}' có MortageBankCode là 'HTC.HO' - xe không thế chấp hoặc đã giải chấp.");

        bool activeElsewhere = await db.CarRedeemRequestDetails
            .AnyAsync(d => d.ReqDMNo != reqDMNo && d.Vin == dto.Vin.Trim() && d.Status != CarRedeemDtlStatus.Rejected && d.Status != CarRedeemDtlStatus.Cancelled);

        if (activeElsewhere)
            throw new InvalidOperationException($"Số khung VIN '{dto.Vin}' đang nằm trong đề nghị giải chấp khác chưa kết thúc.");

        var dtl = new CarRedeemRequestDetail
        {
            ReqDMNo = reqDMNo,
            CarId = string.IsNullOrWhiteSpace(dto.CarId) ? $"CAR-{dto.Vin.Trim()}" : dto.CarId.Trim(),
            Vin = dto.Vin.Trim(),
            ModelCode = dto.ModelCode?.Trim() ?? "",
            ModelName = dto.ModelName?.Trim() ?? "",
            SpecCode = dto.SpecCode?.Trim(),
            SpecDescription = dto.SpecDescription?.Trim(),
            ColorCode = dto.ColorCode?.Trim(),
            ColorName = dto.ColorName?.Trim(),
            EngineNo = dto.EngineNo?.Trim(),
            CQNo = dto.CQNo?.Trim(),
            CONo = dto.CONo?.Trim(),
            DeclarationNo = dto.DeclarationNo?.Trim(),
            MortageBankCode = string.IsNullOrWhiteSpace(dto.MortageBankCode) ? (item.BankCode ?? "VCB") : dto.MortageBankCode.Trim(),
            MortageBankName = dto.MortageBankName?.Trim(),
            TypeDMReq = dto.TypeDMReq,
            DealerCode = string.IsNullOrWhiteSpace(dto.DealerCode) ? item.DealerCode : dto.DealerCode.Trim(),
            DealerName = dto.DealerName?.Trim() ?? item.DealerName,
            GuaranteeNo = dto.GuaranteeNo?.Trim(),
            DRListCode = dto.DRListCode?.Trim(),
            DocumentsStatus = dto.DocumentsStatus?.Trim() ?? "Full",
            Status = CarRedeemDtlStatus.Pending,
            Remark = dto.Remark?.Trim()
        };

        item.Details.Add(dtl);
        item.TotalCars = item.Details.Count;
        item.ApprovedCars = item.Details.Count(x => x.Status == CarRedeemDtlStatus.Approved);

        await db.SaveChangesAsync();
        return ToDto(item);
    }

    public async Task<CarRedeemRequestDto?> RemoveCarAsync(string reqDMNo, string vin)
    {
        var item = await db.CarRedeemRequests
            .Include(x => x.Details)
            .FirstOrDefaultAsync(x => x.OrgId == Org && x.ReqDMNo == reqDMNo.Trim())
            ?? throw new InvalidOperationException($"Không tìm thấy đề nghị giải chấp '{reqDMNo}'.");

        if (item.Status != CarRedeemStatus.Pending)
            throw new InvalidOperationException($"Chỉ có thể gỡ xe khỏi đề nghị giải chấp ở trạng thái Chờ duyệt (Pending).");

        var dtl = item.Details.FirstOrDefault(x => string.Equals(x.Vin, vin.Trim(), StringComparison.OrdinalIgnoreCase))
            ?? throw new InvalidOperationException($"Không tìm thấy xe VIN '{vin}' trong đề nghị giải chấp '{reqDMNo}'.");

        if (dtl.Status != CarRedeemDtlStatus.Pending)
            throw new InvalidOperationException($"Chỉ có thể gỡ xe đang ở trạng thái Chờ duyệt (Pending).");

        item.Details.Remove(dtl);
        db.CarRedeemRequestDetails.Remove(dtl);

        // Nếu đề nghị không còn dòng xe nào -> xóa / hủy luôn đề nghị cha theo quy chuẩn DMS.Sales
        if (item.Details.Count == 0)
        {
            db.CarRedeemRequests.Remove(item);
            await db.SaveChangesAsync();
            return null;
        }

        item.TotalCars = item.Details.Count;
        item.ApprovedCars = item.Details.Count(x => x.Status == CarRedeemDtlStatus.Approved);

        await db.SaveChangesAsync();
        return ToDto(item);
    }

    public async Task<List<EligibleRedeemCarDto>> GetEligibleCarsAsync(string? dealerCode, string? bankCode)
    {
        // Tra cứu xe từ CarVinInventory và CarDocRequestDetail
        var activeRedeemVins = await db.CarRedeemRequestDetails
            .Where(d => d.Status != CarRedeemDtlStatus.Rejected && d.Status != CarRedeemDtlStatus.Cancelled)
            .Select(d => d.Vin)
            .ToListAsync();

        var activeVinSet = new HashSet<string>(activeRedeemVins, StringComparer.OrdinalIgnoreCase);

        var docDetails = await db.CarDocRequests
            .Where(r => r.OrgId == Org && (r.Status == DocReqStatus.Approved1 || r.Status == DocReqStatus.Approved2))
            .SelectMany(r => r.Details.Select(d => new
            {
                r.DealerCode,
                r.DealerName,
                r.RequestType,
                r.DRListCode,
                d.Vin,
                d.Model,
                d.EngineNo,
                d.BankCode,
                d.BankGuaranteeNo,
                d.DocumentsStatus
            }))
            .ToListAsync();

        var vinList = new List<EligibleRedeemCarDto>();
        var addedVins = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var doc in docDetails)
        {
            if (string.IsNullOrWhiteSpace(doc.Vin) || activeVinSet.Contains(doc.Vin) || addedVins.Contains(doc.Vin))
                continue;

            string mBank = string.IsNullOrWhiteSpace(doc.BankCode) ? "VCB" : doc.BankCode.Trim();
            if (string.Equals(mBank, "HTC.HO", StringComparison.OrdinalIgnoreCase))
                continue;

            if (!string.IsNullOrWhiteSpace(dealerCode) && !string.Equals(doc.DealerCode, dealerCode.Trim(), StringComparison.OrdinalIgnoreCase))
                continue;

            if (!string.IsNullOrWhiteSpace(bankCode) && !string.Equals(mBank, bankCode.Trim(), StringComparison.OrdinalIgnoreCase))
                continue;

            addedVins.Add(doc.Vin);
            vinList.Add(new EligibleRedeemCarDto(
                CarId: $"CAR-{doc.Vin}",
                Vin: doc.Vin,
                ModelCode: doc.Model.Contains("Santa Fe") ? "SF25" : (doc.Model.Contains("Tucson") ? "TU20" : "CR15"),
                ModelName: doc.Model,
                SpecCode: null,
                SpecDescription: doc.Model,
                ColorCode: "WH",
                ColorName: "Trắng",
                EngineNo: doc.EngineNo,
                CQNo: $"CQ-2026-{doc.Vin.Substring(Math.Max(0, doc.Vin.Length - 6))}",
                CONo: $"CO-2026-HQ{doc.Vin.Substring(Math.Max(0, doc.Vin.Length - 5))}",
                DeclarationNo: $"TKHQ-2026-{doc.Vin.Substring(Math.Max(0, doc.Vin.Length - 4))}",
                MortageBankCode: mBank,
                MortageBankName: GetBankName(mBank),
                DealerCode: doc.DealerCode,
                DealerName: doc.DealerName,
                DRListCode: doc.DRListCode,
                GuaranteeNo: doc.BankGuaranteeNo,
                DocumentsStatus: doc.DocumentsStatus ?? "Full",
                SuggestedType: doc.RequestType == DocReqType.Normal && !string.IsNullOrWhiteSpace(doc.BankGuaranteeNo)
                    ? "Guarantee"
                    : "Direct"
            ));
        }

        // Bổ sung các xe từ CarVinInventory chưa giải chấp
        var invCars = await db.CarVinInventories
            .Where(x => x.OrgId == Org && x.Status != CarVinStatus.Delivered)
            .ToListAsync();

        foreach (var inv in invCars)
        {
            if (activeVinSet.Contains(inv.Vin) || addedVins.Contains(inv.Vin))
                continue;

            string mBank = "TCB"; // Mặc định xe kho chưa giải chấp thế chấp ngân hàng bảo lãnh Techcombank
            if (!string.IsNullOrWhiteSpace(bankCode) && !string.Equals(mBank, bankCode.Trim(), StringComparison.OrdinalIgnoreCase))
                continue;

            addedVins.Add(inv.Vin);
            vinList.Add(new EligibleRedeemCarDto(
                CarId: inv.MappedCarId ?? $"CAR-{inv.Vin}",
                Vin: inv.Vin,
                ModelCode: inv.ModelCode,
                ModelName: inv.ModelName,
                SpecCode: inv.SpecCode,
                SpecDescription: inv.SpecDescription,
                ColorCode: inv.ColorCode,
                ColorName: inv.ColorName,
                EngineNo: inv.EngineNo,
                CQNo: $"CQ-2026-{inv.Vin.Substring(Math.Max(0, inv.Vin.Length - 6))}",
                CONo: $"CO-2026-HQ{inv.Vin.Substring(Math.Max(0, inv.Vin.Length - 5))}",
                DeclarationNo: $"TKHQ-2026-{inv.Vin.Substring(Math.Max(0, inv.Vin.Length - 4))}",
                MortageBankCode: mBank,
                MortageBankName: GetBankName(mBank),
                DealerCode: dealerCode ?? "VN001",
                DealerName: "Hyundai Đông Đô",
                DRListCode: null,
                GuaranteeNo: null,
                DocumentsStatus: "Full",
                SuggestedType: "Direct"
            ));
        }

        return vinList;
    }

    public async Task<CarRedeemStatsDto> GetStatsAsync()
    {
        var list = await db.CarRedeemRequests
            .Include(x => x.Details)
            .Where(x => x.OrgId == Org)
            .ToListAsync();

        int totalReq = list.Count;
        int totalApp = list.Count(x => x.Status == CarRedeemStatus.Approved);
        int totalPen = list.Count(x => x.Status == CarRedeemStatus.Pending);
        int totalRej = list.Count(x => x.Status == CarRedeemStatus.Rejected);
        int totalCan = list.Count(x => x.Status == CarRedeemStatus.Cancelled);

        int totalCars = list.Sum(x => x.TotalCars);
        int totalCarsApp = list.Sum(x => x.ApprovedCars);

        var byStatus = list.GroupBy(x => x.Status.ToString()).ToDictionary(g => g.Key, g => g.Count());
        var byType = list.GroupBy(x => x.RedeemType.ToString()).ToDictionary(g => g.Key, g => g.Count());
        var byBank = list.GroupBy(x => x.BankCode ?? "OTHER").ToDictionary(g => g.Key, g => g.Count());
        var byDealer = list.GroupBy(x => x.DealerCode).ToDictionary(g => g.Key, g => g.Count());

        return new CarRedeemStatsDto(
            totalReq,
            totalApp,
            totalPen,
            totalRej,
            totalCan,
            totalCars,
            totalCarsApp,
            byStatus,
            byType,
            byBank,
            byDealer
        );
    }

    private async Task<string> GenerateSeqNoAsync()
    {
        string prefix = $"{DateTime.Today:yyMM}RDM";
        var last = await db.CarRedeemRequests
            .Where(x => x.OrgId == Org && x.ReqDMNo.StartsWith(prefix))
            .OrderByDescending(x => x.ReqDMNo)
            .Select(x => x.ReqDMNo)
            .FirstOrDefaultAsync();

        int nextSeq = 1;
        if (!string.IsNullOrEmpty(last) && last.Length >= prefix.Length + 5)
        {
            if (int.TryParse(last.Substring(prefix.Length, 5), out int s))
                nextSeq = s + 1;
        }

        return $"{prefix}{nextSeq:D5}";
    }

    private static string GetBankName(string code) => code.ToUpperInvariant() switch
    {
        "VCB" => "Ngân hàng Ngoại thương Việt Nam (Vietcombank)",
        "BIDV" => "Ngân hàng Đầu tư và Phát triển Việt Nam (BIDV)",
        "TCB" => "Ngân hàng Kỹ thương Việt Nam (Techcombank)",
        "CTG" => "Ngân hàng Công thương Việt Nam (VietinBank)",
        "MB" => "Ngân hàng Quân đội (MBBank)",
        "VIB" => "Ngân hàng Quốc tế Việt Nam (VIB)",
        "VPB" or "VPBANK" => "Ngân hàng Việt Nam Thịnh vượng (VPBank)",
        "HTC.HO" => "Nhà phân phối ô tô Hyundai (HTC Head Office - Không thế chấp)",
        _ => $"Ngân hàng đối tác {code}"
    };

    private static CarRedeemRequestDto ToDto(CarRedeemRequest r) => new(
        r.Id,
        r.ReqDMNo,
        r.DealerCode,
        r.DealerName,
        r.Status.ToString(),
        r.RedeemType.ToString(),
        r.TotalCars,
        r.ApprovedCars,
        r.BankCode,
        r.BankName,
        r.Remark,
        r.CreatedBy,
        r.CreatedAt,
        r.ApprovedBy,
        r.ApprovedAt,
        r.RejectedBy,
        r.RejectedAt,
        r.RejectReason,
        r.CancelledBy,
        r.CancelledAt,
        r.CancelReason,
        r.Details.Select(d => new CarRedeemDetailDto(
            d.Id,
            d.ReqDMNo,
            d.CarId,
            d.Vin,
            d.ModelCode,
            d.ModelName,
            d.SpecCode,
            d.SpecDescription,
            d.ColorCode,
            d.ColorName,
            d.EngineNo,
            d.CQNo,
            d.CONo,
            d.DeclarationNo,
            d.MortageBankCode,
            d.MortageBankName,
            d.TypeDMReq.ToString(),
            d.DealerCode,
            d.DealerName,
            d.GuaranteeNo,
            d.DRListCode,
            d.DocumentsStatus,
            d.Status.ToString(),
            d.ApprovedAt,
            d.ApprovedBy,
            d.Remark
        )).ToList()
    );
}
