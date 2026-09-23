using Microsoft.EntityFrameworkCore;
using MiniSales.Data;
using MiniSales.Models;

namespace MiniSales.Services;

// ===== DTOs =====
public record CreateInsuranceCompanyDto(
    string InsCompanyCode = "",
    string InsCompanyName = "",
    string FlagActive = "1",
    string? Remark = null,
    string? ActionBy = null
);

public record UpdateInsuranceCompanyDto(
    string? InsCompanyName = null,
    string FlagActive = "1",
    string? Remark = null,
    string? ActionBy = null
);

public record InsCompanyDto(
    long Id,
    string InsCompanyCode,
    string InsCompanyName,
    string FlagActive,
    bool IsActive,
    string? Remark,
    DateTime LogLUDateTime,
    string? LogLUBy
);

public record InsuranceCompanyImportRowDto(
    string? InsCompanyCode,
    string? InsCompanyName,
    string? FlagActive,
    string? Remark
);

public record CreateInsuranceTypeDto(
    string InsCompanyCode = "",
    string InsTypeCode = "",
    DateTime? EffectiveDate = null,
    string InsTypeName = "",
    decimal Rate = 0,
    string FlagActive = "1",
    string? Remark = null,
    string? ActionBy = null
);

public record UpdateInsuranceTypeDto(
    string? InsTypeName = null,
    decimal? Rate = null,
    string FlagActive = "1",
    string? Remark = null,
    string? ActionBy = null
);

public record InsTypeDto(
    long Id,
    string InsCompanyCode,
    string? InsCompanyName,
    string InsTypeCode,
    DateTime EffectiveDate,
    string InsTypeName,
    decimal Rate,
    string FlagActive,
    bool IsActive,
    string? Remark,
    DateTime LogLUDateTime,
    string? LogLUBy
);

public record InsuranceTypeImportRowDto(
    string? InsCompanyCode,
    string? InsTypeCode,
    DateTime? EffectiveDate,
    string? InsTypeName,
    decimal? Rate,
    string? FlagActive,
    string? Remark
);

public record InsuranceImportResultDto(
    int TotalRows,
    int Inserted,
    int Updated,
    List<string> Keys
);

public record InsuranceMasterStatsDto(
    int TotalCompanies,
    int TotalActiveCompanies,
    int TotalTypes,
    int TotalActiveTypes,
    decimal AvgRate,
    decimal MaxRate,
    Dictionary<string, int> TypesByCompany
);

public interface IInsuranceMasterService
{
    // Mst_InsuranceCompany
    Task<List<InsCompanyDto>> SearchCompaniesAsync(string? keyWord, string? flagActive);
    Task<InsCompanyDto?> GetCompanyAsync(string insCompanyCode);
    Task<InsCompanyDto> CreateCompanyAsync(CreateInsuranceCompanyDto dto);
    Task<InsCompanyDto?> UpdateCompanyAsync(string insCompanyCode, UpdateInsuranceCompanyDto dto);
    Task<bool> DeleteCompanyAsync(string insCompanyCode);
    Task<InsuranceImportResultDto> ImportCompaniesAsync(List<InsuranceCompanyImportRowDto> rows);

    // Mst_InsuranceType
    Task<List<InsTypeDto>> SearchTypesAsync(string? keyWord, string? insCompanyCode, string? flagActive);
    Task<InsTypeDto?> GetTypeAsync(string insCompanyCode, string insTypeCode, DateTime effectiveDate);
    Task<InsTypeDto> CreateTypeAsync(CreateInsuranceTypeDto dto);
    Task<InsTypeDto?> UpdateTypeAsync(string insCompanyCode, string insTypeCode, DateTime effectiveDate, UpdateInsuranceTypeDto dto);
    Task<bool> DeleteTypeAsync(string insCompanyCode, string insTypeCode, DateTime effectiveDate);
    Task<InsuranceImportResultDto> ImportTypesAsync(List<InsuranceTypeImportRowDto> rows);

    Task<InsuranceMasterStatsDto> GetStatsAsync();
}

/// <summary>
/// Quản lý Danh mục Công ty Bảo hiểm & Loại Bảo hiểm (DMS.Sales Mst_InsuranceCompany + Mst_InsuranceType /
/// Master.1.cs / Mst_InsuranceCompany_Get|Update|Delete / Mst_InsuranceType_Get|Update|Delete):
/// - Mst_InsuranceCompany: danh mục công ty bảo hiểm đối tác cung cấp dịch vụ bảo hiểm xe.
///   InsCompanyCode là khóa nghiệp vụ duy nhất; bắt buộc InsCompanyCode + InsCompanyName + FlagActive
///   (Mst_InsuranceCompany_Update_InvalidInput).
/// - Mst_InsuranceType: gói bảo hiểm cụ thể do một công ty bảo hiểm cung cấp.
///   Khóa nghiệp vụ = (InsCompanyCode, InsTypeCode, EffectiveDate); FK InsCompanyCode phải tồn tại trong
///   Mst_InsuranceCompany; bắt buộc InsCompanyCode + InsTypeCode + EffectiveDate + InsTypeName + Rate (0..100)
///   + FlagActive (Mst_InsuranceType_Update_*NullOrEmpty / _RateInvalid / _FlagActiveInvalid).
///   Hỗ trợ nhập hàng loạt (Import) theo cơ chế upsert cho cả 2 bảng.
/// </summary>
public sealed class InsuranceMasterService(AppDbContext db, ITenantContext tenant) : IInsuranceMasterService
{
    private Guid Org => tenant.OrgId;

    // ===== Mst_InsuranceCompany =====
    public async Task<List<InsCompanyDto>> SearchCompaniesAsync(string? keyWord, string? flagActive)
    {
        var q = db.InsuranceCompanies.AsNoTracking().Where(x => x.OrgId == Org);
        if (!string.IsNullOrWhiteSpace(keyWord))
        {
            var kw = keyWord.Trim();
            q = q.Where(x => x.InsCompanyCode.Contains(kw) || x.InsCompanyName.Contains(kw));
        }
        if (!string.IsNullOrWhiteSpace(flagActive))
        {
            var fa = flagActive.Trim();
            q = q.Where(x => x.FlagActive == fa);
        }
        var list = await q.OrderBy(x => x.InsCompanyCode).ToListAsync();
        return list.Select(ToCompanyDto).ToList();
    }

    public async Task<InsCompanyDto?> GetCompanyAsync(string insCompanyCode)
    {
        if (string.IsNullOrWhiteSpace(insCompanyCode)) return null;
        var code = insCompanyCode.Trim();
        var e = await db.InsuranceCompanies.AsNoTracking()
            .FirstOrDefaultAsync(x => x.OrgId == Org && x.InsCompanyCode == code);
        return e is null ? null : ToCompanyDto(e);
    }

    public async Task<InsCompanyDto> CreateCompanyAsync(CreateInsuranceCompanyDto dto)
    {
        var code = (dto.InsCompanyCode ?? "").Trim();
        if (code.Length < 1)
            throw new InvalidOperationException("Mã công ty bảo hiểm (InsCompanyCode) không được để trống.");

        // InsCompanyCode là khóa nghiệp vụ duy nhất (Mst_InsuranceCompany_CheckDB - FlagExistToCheck = No)
        if (await db.InsuranceCompanies.AnyAsync(x => x.OrgId == Org && x.InsCompanyCode == code))
            throw new InvalidOperationException($"Công ty bảo hiểm '{code}' đã tồn tại.");

        var name = (dto.InsCompanyName ?? "").Trim();
        if (name.Length < 1)
            throw new InvalidOperationException("Tên công ty bảo hiểm (InsCompanyName) không được để trống.");

        var user = string.IsNullOrWhiteSpace(dto.ActionBy) ? "CHUYEN_VIEN_NPP" : dto.ActionBy.Trim();
        var entity = new InsuranceCompany
        {
            OrgId = Org,
            InsCompanyCode = code,
            InsCompanyName = name,
            FlagActive = NormalizeFlag(dto.FlagActive),
            Remark = dto.Remark,
            LogLUDateTime = DateTime.Now,
            LogLUBy = user
        };
        db.InsuranceCompanies.Add(entity);
        await db.SaveChangesAsync();
        return ToCompanyDto(entity);
    }

    public async Task<InsCompanyDto?> UpdateCompanyAsync(string insCompanyCode, UpdateInsuranceCompanyDto dto)
    {
        if (string.IsNullOrWhiteSpace(insCompanyCode)) return null;
        var code = insCompanyCode.Trim();
        var entity = await db.InsuranceCompanies.FirstOrDefaultAsync(x => x.OrgId == Org && x.InsCompanyCode == code);
        if (entity is null) return null;

        if (dto.InsCompanyName is not null)
        {
            var name = dto.InsCompanyName.Trim();
            if (name.Length < 1)
                throw new InvalidOperationException("Tên công ty bảo hiểm (InsCompanyName) không được để trống.");
            entity.InsCompanyName = name;
        }

        var user = string.IsNullOrWhiteSpace(dto.ActionBy) ? "CHUYEN_VIEN_NPP" : dto.ActionBy.Trim();
        entity.FlagActive = NormalizeFlag(dto.FlagActive);
        if (dto.Remark is not null) entity.Remark = dto.Remark;
        entity.LogLUDateTime = DateTime.Now;
        entity.LogLUBy = user;

        await db.SaveChangesAsync();
        return ToCompanyDto(entity);
    }

    public async Task<bool> DeleteCompanyAsync(string insCompanyCode)
    {
        if (string.IsNullOrWhiteSpace(insCompanyCode)) return false;
        var code = insCompanyCode.Trim();
        var entity = await db.InsuranceCompanies.FirstOrDefaultAsync(x => x.OrgId == Org && x.InsCompanyCode == code);
        if (entity is null) return false;

        // Chặn xóa khi còn loại bảo hiểm con tham chiếu (FK Mst_InsuranceType)
        var hasTypes = await db.InsuranceTypes.AnyAsync(x => x.OrgId == Org && x.InsCompanyCode == code);
        if (hasTypes)
            throw new InvalidOperationException($"Không thể xóa công ty bảo hiểm '{code}' vì đang có loại bảo hiểm con tham chiếu.");

        db.InsuranceCompanies.Remove(entity);
        await db.SaveChangesAsync();
        return true;
    }

    public async Task<InsuranceImportResultDto> ImportCompaniesAsync(List<InsuranceCompanyImportRowDto> rows)
    {
        if (rows is null || rows.Count < 1)
            throw new InvalidOperationException("Danh sách nhập (Import) rỗng, không có dòng dữ liệu nào.");

        // Phase 1: kiểm tra từng dòng (field validation)
        for (int i = 0; i < rows.Count; i++)
        {
            var code = (rows[i].InsCompanyCode ?? "").Trim();
            var name = (rows[i].InsCompanyName ?? "").Trim();
            if (code.Length < 1)
                throw new InvalidOperationException($"Dòng {i}: Mã công ty bảo hiểm (InsCompanyCode) không được để trống.");
            if (name.Length < 1)
                throw new InvalidOperationException($"Dòng {i}: Tên công ty bảo hiểm (InsCompanyName) không được để trống.");
        }

        // Phase 2: kiểm tra trùng khóa InsCompanyCode giữa các dòng
        var dup = rows.GroupBy(r => (r.InsCompanyCode ?? "").Trim())
                      .FirstOrDefault(g => g.Count() > 1);
        if (dup is not null)
            throw new InvalidOperationException($"Mã công ty bảo hiểm '{dup.Key}' bị trùng lặp trong danh sách nhập.");

        var user = "CHUYEN_VIEN_NPP";
        int inserted = 0, updated = 0;
        var keys = new List<string>();

        // Phase 3: upsert (dòng đã có thì cập nhật, dòng mới thì thêm)
        foreach (var r in rows)
        {
            var code = (r.InsCompanyCode ?? "").Trim();
            var existing = await db.InsuranceCompanies.FirstOrDefaultAsync(x => x.OrgId == Org && x.InsCompanyCode == code);
            if (existing is not null)
            {
                existing.InsCompanyName = (r.InsCompanyName ?? "").Trim();
                existing.FlagActive = NormalizeFlag(r.FlagActive);
                if (r.Remark is not null) existing.Remark = r.Remark;
                existing.LogLUDateTime = DateTime.Now;
                existing.LogLUBy = user;
                updated++;
            }
            else
            {
                db.InsuranceCompanies.Add(new InsuranceCompany
                {
                    OrgId = Org,
                    InsCompanyCode = code,
                    InsCompanyName = (r.InsCompanyName ?? "").Trim(),
                    FlagActive = NormalizeFlag(r.FlagActive),
                    Remark = r.Remark,
                    LogLUDateTime = DateTime.Now,
                    LogLUBy = user
                });
                inserted++;
            }
            keys.Add(code);
        }

        await db.SaveChangesAsync();
        return new InsuranceImportResultDto(rows.Count, inserted, updated, keys);
    }

    // ===== Mst_InsuranceType =====
    public async Task<List<InsTypeDto>> SearchTypesAsync(string? keyWord, string? insCompanyCode, string? flagActive)
    {
        var q = db.InsuranceTypes.AsNoTracking().Where(x => x.OrgId == Org);
        if (!string.IsNullOrWhiteSpace(keyWord))
        {
            var kw = keyWord.Trim();
            q = q.Where(x => x.InsTypeCode.Contains(kw) || x.InsTypeName.Contains(kw));
        }
        if (!string.IsNullOrWhiteSpace(insCompanyCode))
        {
            var code = insCompanyCode.Trim();
            q = q.Where(x => x.InsCompanyCode == code);
        }
        if (!string.IsNullOrWhiteSpace(flagActive))
        {
            var fa = flagActive.Trim();
            q = q.Where(x => x.FlagActive == fa);
        }
        var list = await q.OrderBy(x => x.InsCompanyCode).ThenBy(x => x.InsTypeCode).ThenBy(x => x.EffectiveDate).ToListAsync();
        var names = await GetCompanyNamesAsync(list.Select(x => x.InsCompanyCode).Distinct().ToList());
        return list.Select(x => ToTypeDto(x, names.GetValueOrDefault(x.InsCompanyCode))).ToList();
    }

    public async Task<InsTypeDto?> GetTypeAsync(string insCompanyCode, string insTypeCode, DateTime effectiveDate)
    {
        if (string.IsNullOrWhiteSpace(insCompanyCode) || string.IsNullOrWhiteSpace(insTypeCode)) return null;
        var code = insCompanyCode.Trim();
        var typeCode = insTypeCode.Trim();
        var eff = effectiveDate.Date;
        var e = await db.InsuranceTypes.AsNoTracking()
            .FirstOrDefaultAsync(x => x.OrgId == Org && x.InsCompanyCode == code && x.InsTypeCode == typeCode && x.EffectiveDate == eff);
        if (e is null) return null;
        return ToTypeDto(e, await GetCompanyNameAsync(code));
    }

    public async Task<InsTypeDto> CreateTypeAsync(CreateInsuranceTypeDto dto)
    {
        var code = (dto.InsCompanyCode ?? "").Trim();
        if (code.Length < 1)
            throw new InvalidOperationException("Mã công ty bảo hiểm (InsCompanyCode) không được để trống.");

        // FK: công ty bảo hiểm phải tồn tại (Mst_InsuranceType_CheckDB)
        await CheckCompanyExistsAsync(code);

        var typeCode = (dto.InsTypeCode ?? "").Trim();
        if (typeCode.Length < 1)
            throw new InvalidOperationException("Mã loại bảo hiểm (InsTypeCode) không được để trống.");

        if (dto.EffectiveDate is null)
            throw new InvalidOperationException("Ngày hiệu lực (EffectiveDate) không được để trống.");
        var eff = dto.EffectiveDate.Value.Date;

        var name = (dto.InsTypeName ?? "").Trim();
        if (name.Length < 1)
            throw new InvalidOperationException("Tên loại bảo hiểm (InsTypeName) không được để trống.");

        ValidateRate(dto.Rate);

        // Khóa nghiệp vụ (InsCompanyCode, InsTypeCode, EffectiveDate) duy nhất
        if (await db.InsuranceTypes.AnyAsync(x => x.OrgId == Org && x.InsCompanyCode == code && x.InsTypeCode == typeCode && x.EffectiveDate == eff))
            throw new InvalidOperationException($"Loại bảo hiểm '{typeCode}' của công ty '{code}' hiệu lực {eff:yyyy-MM-dd} đã tồn tại.");

        var user = string.IsNullOrWhiteSpace(dto.ActionBy) ? "CHUYEN_VIEN_NPP" : dto.ActionBy.Trim();
        var entity = new InsuranceType
        {
            OrgId = Org,
            InsCompanyCode = code,
            InsTypeCode = typeCode,
            EffectiveDate = eff,
            InsTypeName = name,
            Rate = dto.Rate,
            FlagActive = NormalizeFlag(dto.FlagActive),
            Remark = dto.Remark,
            LogLUDateTime = DateTime.Now,
            LogLUBy = user
        };
        db.InsuranceTypes.Add(entity);
        await db.SaveChangesAsync();
        return ToTypeDto(entity, await GetCompanyNameAsync(code));
    }

    public async Task<InsTypeDto?> UpdateTypeAsync(string insCompanyCode, string insTypeCode, DateTime effectiveDate, UpdateInsuranceTypeDto dto)
    {
        if (string.IsNullOrWhiteSpace(insCompanyCode) || string.IsNullOrWhiteSpace(insTypeCode)) return null;
        var code = insCompanyCode.Trim();
        var typeCode = insTypeCode.Trim();
        var eff = effectiveDate.Date;
        var entity = await db.InsuranceTypes
            .FirstOrDefaultAsync(x => x.OrgId == Org && x.InsCompanyCode == code && x.InsTypeCode == typeCode && x.EffectiveDate == eff);
        if (entity is null) return null;

        if (dto.InsTypeName is not null)
        {
            var name = dto.InsTypeName.Trim();
            if (name.Length < 1)
                throw new InvalidOperationException("Tên loại bảo hiểm (InsTypeName) không được để trống.");
            entity.InsTypeName = name;
        }
        if (dto.Rate is not null)
        {
            ValidateRate(dto.Rate.Value);
            entity.Rate = dto.Rate.Value;
        }

        var user = string.IsNullOrWhiteSpace(dto.ActionBy) ? "CHUYEN_VIEN_NPP" : dto.ActionBy.Trim();
        entity.FlagActive = NormalizeFlag(dto.FlagActive);
        if (dto.Remark is not null) entity.Remark = dto.Remark;
        entity.LogLUDateTime = DateTime.Now;
        entity.LogLUBy = user;

        await db.SaveChangesAsync();
        return ToTypeDto(entity, await GetCompanyNameAsync(code));
    }

    public async Task<bool> DeleteTypeAsync(string insCompanyCode, string insTypeCode, DateTime effectiveDate)
    {
        if (string.IsNullOrWhiteSpace(insCompanyCode) || string.IsNullOrWhiteSpace(insTypeCode)) return false;
        var code = insCompanyCode.Trim();
        var typeCode = insTypeCode.Trim();
        var eff = effectiveDate.Date;
        var entity = await db.InsuranceTypes
            .FirstOrDefaultAsync(x => x.OrgId == Org && x.InsCompanyCode == code && x.InsTypeCode == typeCode && x.EffectiveDate == eff);
        if (entity is null) return false;

        db.InsuranceTypes.Remove(entity);
        await db.SaveChangesAsync();
        return true;
    }

    public async Task<InsuranceImportResultDto> ImportTypesAsync(List<InsuranceTypeImportRowDto> rows)
    {
        if (rows is null || rows.Count < 1)
            throw new InvalidOperationException("Danh sách nhập (Import) rỗng, không có dòng dữ liệu nào.");

        // Phase 1: kiểm tra từng dòng (field validation)
        for (int i = 0; i < rows.Count; i++)
        {
            var code = (rows[i].InsCompanyCode ?? "").Trim();
            var typeCode = (rows[i].InsTypeCode ?? "").Trim();
            var name = (rows[i].InsTypeName ?? "").Trim();
            if (code.Length < 1)
                throw new InvalidOperationException($"Dòng {i}: Mã công ty bảo hiểm (InsCompanyCode) không được để trống.");
            if (typeCode.Length < 1)
                throw new InvalidOperationException($"Dòng {i}: Mã loại bảo hiểm (InsTypeCode) không được để trống.");
            if (rows[i].EffectiveDate is null)
                throw new InvalidOperationException($"Dòng {i}: Ngày hiệu lực (EffectiveDate) không được để trống.");
            if (name.Length < 1)
                throw new InvalidOperationException($"Dòng {i}: Tên loại bảo hiểm (InsTypeName) không được để trống.");
            ValidateRate(rows[i].Rate ?? 0m);
        }

        // Phase 2: kiểm tra trùng khóa (InsCompanyCode + InsTypeCode + EffectiveDate) giữa các dòng
        var dup = rows.GroupBy(r => ((r.InsCompanyCode ?? "").Trim(), (r.InsTypeCode ?? "").Trim(), r.EffectiveDate?.Date))
                      .FirstOrDefault(g => g.Count() > 1);
        if (dup is not null)
            throw new InvalidOperationException($"Cặp khóa '{dup.Key.Item1} + {dup.Key.Item2} + {dup.Key.Item3:yyyy-MM-dd}' bị trùng lặp trong danh sách nhập.");

        var user = "CHUYEN_VIEN_NPP";
        int inserted = 0, updated = 0;
        var keys = new List<string>();

        // Phase 3: upsert (dòng đã có thì cập nhật, dòng mới thì thêm)
        foreach (var r in rows)
        {
            var code = (r.InsCompanyCode ?? "").Trim();
            var typeCode = (r.InsTypeCode ?? "").Trim();
            var eff = r.EffectiveDate!.Value.Date;
            var existing = await db.InsuranceTypes
                .FirstOrDefaultAsync(x => x.OrgId == Org && x.InsCompanyCode == code && x.InsTypeCode == typeCode && x.EffectiveDate == eff);
            if (existing is not null)
            {
                existing.InsTypeName = (r.InsTypeName ?? "").Trim();
                existing.Rate = r.Rate ?? 0m;
                existing.FlagActive = NormalizeFlag(r.FlagActive);
                if (r.Remark is not null) existing.Remark = r.Remark;
                existing.LogLUDateTime = DateTime.Now;
                existing.LogLUBy = user;
                updated++;
            }
            else
            {
                // FK: công ty bảo hiểm phải tồn tại (nhánh Insert)
                await CheckCompanyExistsAsync(code);
                db.InsuranceTypes.Add(new InsuranceType
                {
                    OrgId = Org,
                    InsCompanyCode = code,
                    InsTypeCode = typeCode,
                    EffectiveDate = eff,
                    InsTypeName = (r.InsTypeName ?? "").Trim(),
                    Rate = r.Rate ?? 0m,
                    FlagActive = NormalizeFlag(r.FlagActive),
                    Remark = r.Remark,
                    LogLUDateTime = DateTime.Now,
                    LogLUBy = user
                });
                inserted++;
            }
            keys.Add($"{code}+{typeCode}+{eff:yyyy-MM-dd}");
        }

        await db.SaveChangesAsync();
        return new InsuranceImportResultDto(rows.Count, inserted, updated, keys);
    }

    public async Task<InsuranceMasterStatsDto> GetStatsAsync()
    {
        var companies = await db.InsuranceCompanies.AsNoTracking().Where(x => x.OrgId == Org).ToListAsync();
        var types = await db.InsuranceTypes.AsNoTracking().Where(x => x.OrgId == Org).ToListAsync();

        return new InsuranceMasterStatsDto(
            TotalCompanies: companies.Count,
            TotalActiveCompanies: companies.Count(x => x.FlagActive == "1"),
            TotalTypes: types.Count,
            TotalActiveTypes: types.Count(x => x.FlagActive == "1"),
            AvgRate: types.Count > 0 ? Math.Round(types.Average(x => x.Rate), 2) : 0m,
            MaxRate: types.Count > 0 ? types.Max(x => x.Rate) : 0m,
            TypesByCompany: types.Where(x => x.FlagActive == "1")
                                 .GroupBy(x => string.IsNullOrWhiteSpace(x.InsCompanyCode) ? "UNKNOWN" : x.InsCompanyCode)
                                 .ToDictionary(g => g.Key, g => g.Count())
        );
    }

    // ===== Helpers =====
    private static string NormalizeFlag(string? flag)
        => string.IsNullOrWhiteSpace(flag) ? "1" : (flag.Trim() == "0" ? "0" : "1");

    /// <summary>Tỷ lệ phí bảo hiểm phải là số trong khoảng 0..100 (Mst_InsuranceType_Update_RateInvalid).</summary>
    private static void ValidateRate(decimal rate)
    {
        if (rate < 0m || rate > 100m)
            throw new InvalidOperationException("Tỷ lệ phí bảo hiểm (Rate) phải là số trong khoảng 0..100.");
    }

    /// <summary>Kiểm tra công ty bảo hiểm tồn tại (Mst_InsuranceType_CheckDB).</summary>
    private async Task CheckCompanyExistsAsync(string insCompanyCode)
    {
        var exists = await db.InsuranceCompanies.AsNoTracking()
            .Where(x => x.OrgId == Org && x.InsCompanyCode == insCompanyCode)
            .Select(x => x.InsCompanyCode)
            .FirstOrDefaultAsync();
        if (exists is null)
            throw new InvalidOperationException($"Công ty bảo hiểm '{insCompanyCode}' không tồn tại trong danh mục (Mst_InsuranceCompany).");
    }

    private async Task<string?> GetCompanyNameAsync(string insCompanyCode)
    {
        var name = await db.InsuranceCompanies.AsNoTracking()
            .Where(x => x.OrgId == Org && x.InsCompanyCode == insCompanyCode)
            .Select(x => x.InsCompanyName)
            .FirstOrDefaultAsync();
        return string.IsNullOrWhiteSpace(name) ? null : name;
    }

    private async Task<Dictionary<string, string?>> GetCompanyNamesAsync(List<string> codes)
    {
        if (codes.Count < 1) return new Dictionary<string, string?>();
        var rows = await db.InsuranceCompanies.AsNoTracking()
            .Where(x => x.OrgId == Org && codes.Contains(x.InsCompanyCode))
            .Select(x => new { x.InsCompanyCode, x.InsCompanyName })
            .ToListAsync();
        return rows.GroupBy(x => x.InsCompanyCode)
                   .ToDictionary(g => g.Key, g => (string?)g.First().InsCompanyName);
    }

    private static InsCompanyDto ToCompanyDto(InsuranceCompany e) => new(
        e.Id, e.InsCompanyCode, e.InsCompanyName, e.FlagActive, e.FlagActive == "1", e.Remark, e.LogLUDateTime, e.LogLUBy);

    private static InsTypeDto ToTypeDto(InsuranceType e, string? companyName) => new(
        e.Id, e.InsCompanyCode, companyName, e.InsTypeCode, e.EffectiveDate, e.InsTypeName, e.Rate,
        e.FlagActive, e.FlagActive == "1", e.Remark, e.LogLUDateTime, e.LogLUBy);
}
