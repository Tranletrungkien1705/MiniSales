using Microsoft.EntityFrameworkCore;
using MiniSales.Data;
using MiniSales.Models;

namespace MiniSales.Services;

// ===== DTOs =====
public record CreateSalesManViolateDto(
    string SMCode = "",
    string DealerCode = "",
    ViolateType ViolateType = ViolateType.Temporary,
    DateTime? ViolateDateStart = null,
    DateTime? ViolateDateEnd = null,
    string? SMHyundaiCode = null,
    string? SMName = null,
    DateTime? SMDateOfBirth = null,
    string? IdentityCardNo = null,
    string? SMPhoneNo = null,
    string? SMType = null,
    string? SMStatus = null,
    string? Remark = null,
    string? ActionBy = null
);

public record UpdateSalesManViolateDto(
    ViolateType ViolateType = ViolateType.Temporary,
    DateTime? ViolateDateStart = null,
    DateTime? ViolateDateEnd = null,
    string? Remark = null,
    string FlagActive = "1",
    string? ActionBy = null
);

public record SalesManViolateDto(
    long Id,
    string SMCode,
    int ViolateNumber,
    string DealerCode,
    string? DealerName,
    ViolateType ViolateType,
    string ViolateTypeName,
    DateTime ViolateDateStart,
    DateTime? ViolateDateEnd,
    string? SMHyundaiCode,
    string? SMName,
    DateTime? SMDateOfBirth,
    string? IdentityCardNo,
    string? SMPhoneNo,
    string? SMType,
    string? SMStatus,
    string? Remark,
    string FlagActive,
    bool IsActive,
    string? CreatedBy,
    DateTime CreatedAt,
    string? UpdateBy,
    DateTime? UpdateDTime,
    DateTime LogLUDateTime,
    string? LogLUBy
);

public record SalesManViolateStatsDto(
    int TotalViolates,
    int TotalActive,
    int TotalInactive,
    int TotalTemporary,
    int TotalPermanent,
    int TotalSalesMen,
    int TotalDealers,
    Dictionary<string, int> ByDealer,
    Dictionary<string, int> ByType
);

public interface ISalesManViolateService
{
    Task<List<SalesManViolateDto>> SearchAsync(string? smCode, string? dealerCode, string? violateType, string? flagActive);
    Task<SalesManViolateDto?> GetAsync(string smCode, int violateNumber);
    Task<SalesManViolateDto> CreateAsync(CreateSalesManViolateDto dto);
    Task<SalesManViolateDto?> UpdateAsync(string smCode, int violateNumber, UpdateSalesManViolateDto dto);
    Task<bool> DeleteAsync(string smCode, int violateNumber);
    Task<SalesManViolateStatsDto> GetStatsAsync();
}

/// <summary>
/// Quản lý Chế tài / Vi phạm Nhân viên Bán hàng (DMS.Sales HR_SalesManViolate / MasterData.HR.cs /
/// HR_SalesManViolate_Get|Create|Update):
/// - Ghi nhận vi phạm chế tài của nhân viên bán hàng đại lý. Khóa nghiệp vụ = (SMCode, ViolateNumber);
///   ViolateNumber tự tăng theo từng nhân viên (lần vi phạm thứ n).
/// - Loại vi phạm: TT = Tạm thời (có thời hạn, bắt buộc ViolateDateEnd), VV = Vĩnh viễn (không thời hạn).
/// - Quy tắc nghiệp vụ (giữ nguyên từ legacy HR_SalesManViolate_CreateX):
///   + Nhân viên đã có vi phạm Vĩnh viễn (VV) thì KHÔNG được lập vi phạm mới.
///   + Vi phạm Tạm thời (TT) bắt buộc có ngày kết thúc và phải LỚN HƠN ngày kết thúc vi phạm gần nhất.
/// </summary>
public sealed class SalesManViolateService(AppDbContext db, ITenantContext tenant) : ISalesManViolateService
{
    private Guid Org => tenant.OrgId;

    public async Task<List<SalesManViolateDto>> SearchAsync(string? smCode, string? dealerCode, string? violateType, string? flagActive)
    {
        var q = db.SalesManViolates.AsNoTracking().Where(x => x.OrgId == Org);
        if (!string.IsNullOrWhiteSpace(smCode))
        {
            var sc = smCode.Trim();
            q = q.Where(x => x.SMCode == sc);
        }
        if (!string.IsNullOrWhiteSpace(dealerCode))
        {
            var dc = dealerCode.Trim();
            q = q.Where(x => x.DealerCode == dc);
        }
        if (!string.IsNullOrWhiteSpace(violateType))
        {
            var vt = ParseViolateType(violateType);
            q = q.Where(x => x.ViolateType == vt);
        }
        if (!string.IsNullOrWhiteSpace(flagActive))
        {
            var fa = flagActive.Trim();
            q = q.Where(x => x.FlagActive == fa);
        }

        var list = await q.OrderBy(x => x.SMCode).ThenBy(x => x.ViolateNumber).ToListAsync();
        return list.Select(ToDto).ToList();
    }

    public async Task<SalesManViolateDto?> GetAsync(string smCode, int violateNumber)
    {
        if (string.IsNullOrWhiteSpace(smCode) || violateNumber < 1) return null;
        var sc = smCode.Trim();
        var e = await db.SalesManViolates.AsNoTracking()
            .FirstOrDefaultAsync(x => x.OrgId == Org && x.SMCode == sc && x.ViolateNumber == violateNumber);
        return e is null ? null : ToDto(e);
    }

    public async Task<SalesManViolateDto> CreateAsync(CreateSalesManViolateDto dto)
    {
        var smCode = (dto.SMCode ?? "").Trim();
        var dealerCode = (dto.DealerCode ?? "").Trim();
        if (smCode.Length < 1)
            throw new InvalidOperationException("Mã nhân viên bán hàng (SMCode) không được để trống.");
        if (dealerCode.Length < 1)
            throw new InvalidOperationException("Mã đại lý (DealerCode) không được để trống.");

        // FK: Đại lý phải tồn tại (Mst_Dealer_CheckDB - FlagExistToCheck = Yes)
        await CheckDealerExistsAsync(dealerCode);

        var start = dto.ViolateDateStart ?? DateTime.Today;

        // Quy tắc: nhân viên đã vi phạm Vĩnh viễn (VV) thì không được lập vi phạm mới.
        var hasPermanent = await db.SalesManViolates.AsNoTracking()
            .AnyAsync(x => x.OrgId == Org && x.SMCode == smCode && x.ViolateType == ViolateType.Permanent);
        if (hasPermanent)
            throw new InvalidOperationException($"Nhân viên '{smCode}' đã có vi phạm Vĩnh viễn (VV), không thể lập thêm vi phạm mới.");

        // Quy tắc: vi phạm Tạm thời (TT) bắt buộc có ngày kết thúc và phải lớn hơn ngày kết thúc vi phạm gần nhất.
        if (dto.ViolateType == ViolateType.Temporary)
        {
            if (dto.ViolateDateEnd is null)
                throw new InvalidOperationException("Vi phạm Tạm thời (TT) bắt buộc phải có ngày kết thúc (ViolateDateEnd).");
            if (dto.ViolateDateEnd.Value.Date <= start.Date)
                throw new InvalidOperationException("Ngày kết thúc vi phạm (ViolateDateEnd) phải lớn hơn ngày bắt đầu (ViolateDateStart).");

            var lastEnd = await db.SalesManViolates.AsNoTracking()
                .Where(x => x.OrgId == Org && x.SMCode == smCode && x.ViolateDateEnd != null)
                .OrderByDescending(x => x.ViolateDateEnd)
                .Select(x => x.ViolateDateEnd)
                .FirstOrDefaultAsync();
            if (lastEnd is not null && dto.ViolateDateEnd.Value.Date <= lastEnd.Value.Date)
                throw new InvalidOperationException($"Ngày kết thúc vi phạm mới ({dto.ViolateDateEnd.Value:yyyy-MM-dd}) phải lớn hơn ngày kết thúc vi phạm gần nhất ({lastEnd.Value:yyyy-MM-dd}).");
        }

        // ViolateNumber tự tăng theo từng nhân viên (lần vi phạm thứ n).
        var maxNo = await db.SalesManViolates.AsNoTracking()
            .Where(x => x.OrgId == Org && x.SMCode == smCode)
            .Select(x => (int?)x.ViolateNumber)
            .MaxAsync() ?? 0;
        var violateNumber = maxNo + 1;

        var user = string.IsNullOrWhiteSpace(dto.ActionBy) ? "CHUYEN_VIEN_NPP" : dto.ActionBy.Trim();
        var entity = new SalesManViolate
        {
            OrgId = Org,
            SMCode = smCode,
            ViolateNumber = violateNumber,
            DealerCode = dealerCode,
            DealerName = await GetDealerNameAsync(dealerCode),
            ViolateType = dto.ViolateType,
            ViolateDateStart = start,
            ViolateDateEnd = dto.ViolateType == ViolateType.Permanent ? null : dto.ViolateDateEnd,
            SMHyundaiCode = dto.SMHyundaiCode?.Trim(),
            SMName = dto.SMName?.Trim(),
            SMDateOfBirth = dto.SMDateOfBirth,
            IdentityCardNo = dto.IdentityCardNo?.Trim(),
            SMPhoneNo = dto.SMPhoneNo?.Trim(),
            SMType = dto.SMType?.Trim(),
            SMStatus = dto.SMStatus?.Trim(),
            Remark = dto.Remark,
            FlagActive = "1",
            CreatedBy = user,
            CreatedAt = DateTime.Now,
            LogLUDateTime = DateTime.Now,
            LogLUBy = user
        };
        db.SalesManViolates.Add(entity);
        await db.SaveChangesAsync();
        return ToDto(entity);
    }

    public async Task<SalesManViolateDto?> UpdateAsync(string smCode, int violateNumber, UpdateSalesManViolateDto dto)
    {
        if (string.IsNullOrWhiteSpace(smCode) || violateNumber < 1) return null;
        var sc = smCode.Trim();
        var entity = await db.SalesManViolates
            .FirstOrDefaultAsync(x => x.OrgId == Org && x.SMCode == sc && x.ViolateNumber == violateNumber);
        if (entity is null) return null;

        var start = dto.ViolateDateStart ?? entity.ViolateDateStart;

        if (dto.ViolateType == ViolateType.Temporary)
        {
            if (dto.ViolateDateEnd is null)
                throw new InvalidOperationException("Vi phạm Tạm thời (TT) bắt buộc phải có ngày kết thúc (ViolateDateEnd).");
            if (dto.ViolateDateEnd.Value.Date <= start.Date)
                throw new InvalidOperationException("Ngày kết thúc vi phạm (ViolateDateEnd) phải lớn hơn ngày bắt đầu (ViolateDateStart).");
        }

        var user = string.IsNullOrWhiteSpace(dto.ActionBy) ? "CHUYEN_VIEN_NPP" : dto.ActionBy.Trim();
        entity.ViolateType = dto.ViolateType;
        entity.ViolateDateStart = start;
        entity.ViolateDateEnd = dto.ViolateType == ViolateType.Permanent ? null : dto.ViolateDateEnd;
        if (dto.Remark is not null) entity.Remark = dto.Remark;
        entity.FlagActive = NormalizeFlag(dto.FlagActive);
        entity.UpdateBy = user;
        entity.UpdateDTime = DateTime.Now;
        entity.LogLUDateTime = DateTime.Now;
        entity.LogLUBy = user;

        await db.SaveChangesAsync();
        return ToDto(entity);
    }

    public async Task<bool> DeleteAsync(string smCode, int violateNumber)
    {
        if (string.IsNullOrWhiteSpace(smCode) || violateNumber < 1) return false;
        var sc = smCode.Trim();
        var entity = await db.SalesManViolates
            .FirstOrDefaultAsync(x => x.OrgId == Org && x.SMCode == sc && x.ViolateNumber == violateNumber);
        if (entity is null) return false;

        db.SalesManViolates.Remove(entity);
        await db.SaveChangesAsync();
        return true;
    }

    public async Task<SalesManViolateStatsDto> GetStatsAsync()
    {
        var list = await db.SalesManViolates.AsNoTracking().Where(x => x.OrgId == Org).ToListAsync();

        return new SalesManViolateStatsDto(
            TotalViolates: list.Count,
            TotalActive: list.Count(x => x.FlagActive == "1"),
            TotalInactive: list.Count(x => x.FlagActive != "1"),
            TotalTemporary: list.Count(x => x.ViolateType == ViolateType.Temporary),
            TotalPermanent: list.Count(x => x.ViolateType == ViolateType.Permanent),
            TotalSalesMen: list.Select(x => x.SMCode).Distinct().Count(),
            TotalDealers: list.Select(x => x.DealerCode).Distinct().Count(),
            ByDealer: list.GroupBy(x => string.IsNullOrWhiteSpace(x.DealerCode) ? "UNKNOWN" : x.DealerCode)
                          .ToDictionary(g => g.Key, g => g.Count()),
            ByType: list.GroupBy(x => x.ViolateType == ViolateType.Permanent ? "VV" : "TT")
                        .ToDictionary(g => g.Key, g => g.Count())
        );
    }

    // ===== Helpers =====
    private static string NormalizeFlag(string? flag)
        => string.IsNullOrWhiteSpace(flag) ? "1" : (flag.Trim() == "0" ? "0" : "1");

    private static ViolateType ParseViolateType(string value)
        => value.Trim().Equals("VV", StringComparison.OrdinalIgnoreCase) || value.Trim() == "1"
            ? ViolateType.Permanent
            : ViolateType.Temporary;

    private static string ViolateTypeName(ViolateType t)
        => t == ViolateType.Permanent ? "Vĩnh viễn" : "Tạm thời";

    /// <summary>Kiểm tra đại lý tồn tại trong danh mục đại lý (Mst_Dealer_CheckDB).</summary>
    private async Task CheckDealerExistsAsync(string dealerCode)
    {
        var dealer = await db.DealerCustomers.AsNoTracking()
            .Where(c => c.OrgId == Org && c.DealerCode == dealerCode)
            .Select(c => c.DealerCode)
            .FirstOrDefaultAsync();
        if (dealer is null)
            throw new InvalidOperationException($"Đại lý '{dealerCode}' không tồn tại trong danh mục đại lý (Mst_Dealer).");
    }

    /// <summary>Tra cứu tên đại lý từ dữ liệu đại lý đã có trong hệ thống (Mst_Dealer.DealerName).</summary>
    private async Task<string?> GetDealerNameAsync(string dealerCode)
    {
        var name = await db.DealerCustomers.AsNoTracking()
            .Where(c => c.OrgId == Org && c.DealerCode == dealerCode && c.DealerName != null)
            .Select(c => c.DealerName)
            .FirstOrDefaultAsync();
        return string.IsNullOrWhiteSpace(name) ? null : name;
    }

    private static SalesManViolateDto ToDto(SalesManViolate e) => new(
        e.Id, e.SMCode, e.ViolateNumber, e.DealerCode, e.DealerName,
        e.ViolateType, ViolateTypeName(e.ViolateType), e.ViolateDateStart, e.ViolateDateEnd,
        e.SMHyundaiCode, e.SMName, e.SMDateOfBirth, e.IdentityCardNo, e.SMPhoneNo, e.SMType, e.SMStatus,
        e.Remark, e.FlagActive, e.FlagActive == "1",
        e.CreatedBy, e.CreatedAt, e.UpdateBy, e.UpdateDTime, e.LogLUDateTime, e.LogLUBy);
}
