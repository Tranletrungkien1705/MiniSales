using Microsoft.EntityFrameworkCore;
using MiniSales.Data;
using MiniSales.Models;

namespace MiniSales.Services;

// ===== DTOs =====
public record CreateCustomerVisitDto(
    string CtmVisitCode = "",
    string DealerCode = "",
    string Gender = "M",
    string? RangeAgeCode = null,
    string? ModelCode = null,
    string? Remark = null,
    string? ActionBy = null
);

public record UpdateCustomerVisitDto(
    string? Gender = null,
    string? RangeAgeCode = null,
    string? ModelCode = null,
    string? Remark = null,
    string FlagActive = "1",
    string? ActionBy = null
);

public record CustomerVisitDto(
    long Id,
    string CtmVisitCode,
    string DealerCode,
    string? DealerName,
    string Gender,
    string? RangeAgeCode,
    string? ModelCode,
    string? ModelName,
    DateTime VisitDTime,
    string FlagActive,
    bool IsActive,
    string? Remark,
    string? CreatedBy,
    DateTime CreatedAt,
    string? LogLUBy,
    DateTime LogLUDTime
);

public record CustomerVisitStatsDto(
    int TotalVisits,
    int TotalActiveVisits,
    int TotalDealers,
    Dictionary<string, int> ByGender,
    Dictionary<string, int> ByRangeAge,
    Dictionary<string, int> ByModel
);

public interface ICustomerVisitService
{
    Task<List<CustomerVisitDto>> SearchAsync(string? ctmVisitCode, string? dealerCode, string? modelCode, string? gender, string? flagActive);
    Task<CustomerVisitDto?> GetAsync(string ctmVisitCode, string dealerCode);
    Task<CustomerVisitDto> CreateAsync(CreateCustomerVisitDto dto);
    Task<CustomerVisitDto?> UpdateAsync(string ctmVisitCode, string dealerCode, UpdateCustomerVisitDto dto);
    Task<bool> DeleteAsync(string ctmVisitCode, string dealerCode);
    Task<CustomerVisitStatsDto> GetStatsAsync();
}

/// <summary>
/// Quản lý lượt khách đến thăm đại lý (DMS.Sales Dlr_CtmVisit / DlrCtmVisitController / DLR_CtmVisit_Create_DL):
/// - Ghi nhận mỗi lượt khách hàng ghé showroom đại lý để tư vấn xe, phục vụ phân tích nhu cầu thị trường.
/// - Khóa nghiệp vụ = (CtmVisitCode, DealerCode). CtmVisitCode bắt buộc, độ dài tối thiểu MinLengthCode.
/// - Gender chỉ nhận M (Nam) hoặc F (Nữ) — giữ nguyên ràng buộc legacy (TConst.Gender.Male/FeMale).
/// - DealerCode phải tồn tại trong dữ liệu đại lý (myCommon_CheckDealer).
/// </summary>
public sealed class CustomerVisitService(AppDbContext db, ITenantContext tenant) : ICustomerVisitService
{
    private Guid Org => tenant.OrgId;

    // Độ dài tối thiểu mã lượt thăm (TConst.HTCConst.MinLengthCode)
    private const int MinLengthCode = 3;

    public async Task<List<CustomerVisitDto>> SearchAsync(string? ctmVisitCode, string? dealerCode, string? modelCode, string? gender, string? flagActive)
    {
        var q = db.CustomerVisits.AsNoTracking().Where(x => x.OrgId == Org);
        if (!string.IsNullOrWhiteSpace(ctmVisitCode))
        {
            var c = ctmVisitCode.Trim();
            q = q.Where(x => x.CtmVisitCode == c);
        }
        if (!string.IsNullOrWhiteSpace(dealerCode))
        {
            var d = dealerCode.Trim();
            q = q.Where(x => x.DealerCode == d);
        }
        if (!string.IsNullOrWhiteSpace(modelCode))
        {
            var m = modelCode.Trim();
            q = q.Where(x => x.ModelCode == m);
        }
        if (!string.IsNullOrWhiteSpace(gender))
        {
            var g = ParseGender(gender);
            q = q.Where(x => x.Gender == g);
        }
        if (!string.IsNullOrWhiteSpace(flagActive))
        {
            var fa = flagActive.Trim();
            q = q.Where(x => x.FlagActive == fa);
        }
        var list = await q.OrderByDescending(x => x.VisitDTime).ThenBy(x => x.CtmVisitCode).ToListAsync();
        return list.Select(ToDto).ToList();
    }

    public async Task<CustomerVisitDto?> GetAsync(string ctmVisitCode, string dealerCode)
    {
        if (string.IsNullOrWhiteSpace(ctmVisitCode) || string.IsNullOrWhiteSpace(dealerCode)) return null;
        var c = ctmVisitCode.Trim();
        var d = dealerCode.Trim();
        var e = await db.CustomerVisits.AsNoTracking()
            .FirstOrDefaultAsync(x => x.OrgId == Org && x.CtmVisitCode == c && x.DealerCode == d);
        return e is null ? null : ToDto(e);
    }

    public async Task<CustomerVisitDto> CreateAsync(CreateCustomerVisitDto dto)
    {
        var ctmVisitCode = (dto.CtmVisitCode ?? "").Trim();
        var dealerCode = (dto.DealerCode ?? "").Trim();

        // Check Input (DlrCtmVisitController.CreateDL): CtmVisitCode bắt buộc
        if (ctmVisitCode.Length < 1)
            throw new InvalidOperationException("Mã lượt thăm (CtmVisitCode) không được để trống.");
        // DLR_CtmVisit_CreateX: CtmVisitCode.Length < MinLengthCode
        if (ctmVisitCode.Length < MinLengthCode)
            throw new InvalidOperationException($"Mã lượt thăm (CtmVisitCode) phải có tối thiểu {MinLengthCode} ký tự.");
        if (dealerCode.Length < 1)
            throw new InvalidOperationException("Mã đại lý (DealerCode) không được để trống.");

        // Gender chỉ nhận M/F (TConst.Gender.Male/FeMale)
        var gender = ParseGender(dto.Gender);

        // DLR_CtmVisit_CheckDB (FlagExistToCheck = Inactive): chặn trùng khóa (CtmVisitCode, DealerCode)
        if (await db.CustomerVisits.AnyAsync(x => x.OrgId == Org && x.CtmVisitCode == ctmVisitCode && x.DealerCode == dealerCode))
            throw new InvalidOperationException($"Lượt thăm '{ctmVisitCode}' của đại lý '{dealerCode}' đã tồn tại.");

        // myCommon_CheckDealer: đại lý phải tồn tại
        var dealerName = await GetDealerNameAsync(dealerCode);
        if (dealerName is null)
            throw new InvalidOperationException($"Đại lý '{dealerCode}' không tồn tại trong hệ thống.");

        var user = string.IsNullOrWhiteSpace(dto.ActionBy) ? "CHUYEN_VIEN_DL" : dto.ActionBy.Trim();
        var now = DateTime.Now;
        var entity = new CustomerVisit
        {
            OrgId = Org,
            CtmVisitCode = ctmVisitCode,
            DealerCode = dealerCode,
            DealerName = dealerName,
            Gender = gender,
            RangeAgeCode = dto.RangeAgeCode?.Trim(),
            ModelCode = dto.ModelCode?.Trim(),
            ModelName = await GetModelNameAsync(dto.ModelCode),
            VisitDTime = now,
            FlagActive = "1",
            Remark = dto.Remark,
            CreatedBy = user,
            CreatedAt = now,
            LogLUBy = user,
            LogLUDTime = now
        };
        db.CustomerVisits.Add(entity);
        await db.SaveChangesAsync();
        return ToDto(entity);
    }

    public async Task<CustomerVisitDto?> UpdateAsync(string ctmVisitCode, string dealerCode, UpdateCustomerVisitDto dto)
    {
        if (string.IsNullOrWhiteSpace(ctmVisitCode) || string.IsNullOrWhiteSpace(dealerCode)) return null;
        var c = ctmVisitCode.Trim();
        var d = dealerCode.Trim();
        var entity = await db.CustomerVisits
            .FirstOrDefaultAsync(x => x.OrgId == Org && x.CtmVisitCode == c && x.DealerCode == d);
        if (entity is null) return null;

        var user = string.IsNullOrWhiteSpace(dto.ActionBy) ? "CHUYEN_VIEN_DL" : dto.ActionBy.Trim();
        if (dto.Gender is not null) entity.Gender = ParseGender(dto.Gender);
        if (dto.RangeAgeCode is not null) entity.RangeAgeCode = dto.RangeAgeCode.Trim();
        if (dto.ModelCode is not null)
        {
            entity.ModelCode = dto.ModelCode.Trim();
            entity.ModelName = await GetModelNameAsync(dto.ModelCode);
        }
        if (dto.Remark is not null) entity.Remark = dto.Remark;
        entity.FlagActive = NormalizeFlag(dto.FlagActive);
        entity.LogLUBy = user;
        entity.LogLUDTime = DateTime.Now;

        await db.SaveChangesAsync();
        return ToDto(entity);
    }

    public async Task<bool> DeleteAsync(string ctmVisitCode, string dealerCode)
    {
        if (string.IsNullOrWhiteSpace(ctmVisitCode) || string.IsNullOrWhiteSpace(dealerCode)) return false;
        var c = ctmVisitCode.Trim();
        var d = dealerCode.Trim();
        var entity = await db.CustomerVisits
            .FirstOrDefaultAsync(x => x.OrgId == Org && x.CtmVisitCode == c && x.DealerCode == d);
        if (entity is null) return false;

        db.CustomerVisits.Remove(entity);
        await db.SaveChangesAsync();
        return true;
    }

    public async Task<CustomerVisitStatsDto> GetStatsAsync()
    {
        var visits = await db.CustomerVisits.AsNoTracking().Where(x => x.OrgId == Org).ToListAsync();
        var active = visits.Where(x => x.FlagActive == "1").ToList();

        return new CustomerVisitStatsDto(
            TotalVisits: visits.Count,
            TotalActiveVisits: active.Count,
            TotalDealers: active.Select(x => x.DealerCode).Distinct().Count(),
            ByGender: active.GroupBy(x => x.Gender == CustomerVisitGender.Female ? "F" : "M")
                            .ToDictionary(g => g.Key, g => g.Count()),
            ByRangeAge: active.GroupBy(x => string.IsNullOrWhiteSpace(x.RangeAgeCode) ? "UNKNOWN" : x.RangeAgeCode)
                            .ToDictionary(g => g.Key, g => g.Count()),
            ByModel: active.GroupBy(x => string.IsNullOrWhiteSpace(x.ModelCode) ? "UNKNOWN" : x.ModelCode)
                            .ToDictionary(g => g.Key, g => g.Count())
        );
    }

    // ===== Helpers =====
    private static string NormalizeFlag(string? flag)
        => string.IsNullOrWhiteSpace(flag) ? "1" : (flag.Trim() == "0" ? "0" : "1");

    /// <summary>Chuẩn hóa giới tính: chỉ nhận M (Nam) hoặc F (Nữ) — giữ nguyên ràng buộc legacy.</summary>
    private static CustomerVisitGender ParseGender(string? gender)
    {
        var g = (gender ?? "").Trim().ToUpperInvariant();
        return g switch
        {
            "M" => CustomerVisitGender.Male,
            "F" => CustomerVisitGender.Female,
            _ => throw new InvalidOperationException("Giới tính (Gender) không hợp lệ, chỉ nhận 'M' (Nam) hoặc 'F' (Nữ).")
        };
    }

    /// <summary>Tra cứu tên đại lý từ dữ liệu đại lý đã có trong hệ thống (myCommon_CheckDealer).</summary>
    private async Task<string?> GetDealerNameAsync(string dealerCode)
    {
        var name = await db.DealerCustomers.AsNoTracking()
            .Where(c => c.OrgId == Org && c.DealerCode == dealerCode && c.DealerName != null)
            .Select(c => c.DealerName)
            .FirstOrDefaultAsync();
        return string.IsNullOrWhiteSpace(name) ? null : name;
    }

    /// <summary>Tra cứu tên dòng xe quan tâm từ danh mục model (Mst_CarModel).</summary>
    private async Task<string?> GetModelNameAsync(string? modelCode)
    {
        if (string.IsNullOrWhiteSpace(modelCode)) return null;
        var mc = modelCode.Trim();
        var name = await db.Cars.AsNoTracking()
            .Where(c => c.OrgId == Org && c.ModelCode == mc && c.ModelName != null)
            .Select(c => c.ModelName)
            .FirstOrDefaultAsync();
        return string.IsNullOrWhiteSpace(name) ? null : name;
    }

    private static CustomerVisitDto ToDto(CustomerVisit e) => new(
        e.Id, e.CtmVisitCode, e.DealerCode, e.DealerName,
        e.Gender == CustomerVisitGender.Female ? "F" : "M",
        e.RangeAgeCode, e.ModelCode, e.ModelName, e.VisitDTime,
        e.FlagActive, e.FlagActive == "1", e.Remark,
        e.CreatedBy, e.CreatedAt, e.LogLUBy, e.LogLUDTime);
}