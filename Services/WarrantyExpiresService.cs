using Microsoft.EntityFrameworkCore;
using MiniSales.Data;
using MiniSales.Models;

namespace MiniSales.Services;

// ===== DTOs =====
public record CreateWarrantyExpiresDto(
    string ModelCode = "",
    decimal WarrantyExpires = 0,
    decimal WarrantyKM = 0,
    string FlagActive = "1",
    string? Remark = null,
    string? ActionBy = null
);

public record UpdateWarrantyExpiresDto(
    decimal WarrantyExpires = 0,
    decimal WarrantyKM = 0,
    string FlagActive = "1",
    string? Remark = null,
    string? ActionBy = null
);

public record WarrantyExpiresImportRowDto(
    string? ModelCode,
    decimal? WarrantyExpires,
    decimal? WarrantyKM,
    string? FlagActive
);

public record WarrantyExpiresDto(
    long Id,
    string ModelCode,
    string? ModelName,
    decimal WarrantyExpires,
    decimal WarrantyKM,
    string FlagActive,
    bool IsActive,
    string? Remark,
    DateTime LogLUDateTime,
    string? LogLUBy
);

public record WarrantyExpiresImportResultDto(
    int TotalRows,
    int Inserted,
    int Updated,
    List<string> ModelCodes
);

public record WarrantyExpiresStatsDto(
    int TotalPolicies,
    int TotalActive,
    int TotalInactive,
    decimal AvgWarrantyExpires,
    decimal AvgWarrantyKM,
    decimal MaxWarrantyExpires,
    decimal MaxWarrantyKM,
    Dictionary<string, int> ByModel
);

public interface IWarrantyExpiresService
{
    Task<List<WarrantyExpiresDto>> SearchAsync(string? modelCode, string? flagActive);
    Task<WarrantyExpiresDto?> GetByModelAsync(string modelCode);
    Task<WarrantyExpiresDto> CreateAsync(CreateWarrantyExpiresDto dto);
    Task<WarrantyExpiresDto?> UpdateAsync(string modelCode, UpdateWarrantyExpiresDto dto);
    Task<bool> DeleteAsync(string modelCode);
    Task<WarrantyExpiresImportResultDto> ImportAsync(List<WarrantyExpiresImportRowDto> rows);
    Task<WarrantyExpiresStatsDto> GetStatsAsync();
}

/// <summary>
/// Quản lý Định mức Bảo hành Xe Ô tô theo dòng xe (DMS.Sales Mst_WarrantyExpires / Master.cs /
/// Mst_WarrantyExpires_Get|Create|Update|Delete|Import): danh mục quy định thời hạn bảo hành (số tháng)
/// và số km giới hạn bảo hành cho từng model xe. Ràng buộc: ModelCode bắt buộc và phải tồn tại trong
/// danh mục dòng xe (Mst_CarModel) đang hoạt động; WarrantyExpires và WarrantyKM phải là số >= 0;
/// ModelCode là khóa nghiệp vụ duy nhất (không trùng khi tạo/import). Hỗ trợ nhập hàng loạt (Import)
/// theo cơ chế upsert: dòng đã có thì cập nhật, dòng mới thì thêm.
/// </summary>
public sealed class WarrantyExpiresService(AppDbContext db, ITenantContext tenant) : IWarrantyExpiresService
{
    private Guid Org => tenant.OrgId;

    public async Task<List<WarrantyExpiresDto>> SearchAsync(string? modelCode, string? flagActive)
    {
        var q = db.WarrantyExpires.AsNoTracking().Where(x => x.OrgId == Org);

        if (!string.IsNullOrWhiteSpace(modelCode))
        {
            var mc = modelCode.Trim();
            q = q.Where(x => x.ModelCode == mc);
        }
        if (!string.IsNullOrWhiteSpace(flagActive))
        {
            var fa = flagActive.Trim();
            q = q.Where(x => x.FlagActive == fa);
        }

        var list = await q.OrderBy(x => x.ModelCode).ToListAsync();
        return list.Select(ToDto).ToList();
    }

    public async Task<WarrantyExpiresDto?> GetByModelAsync(string modelCode)
    {
        if (string.IsNullOrWhiteSpace(modelCode)) return null;
        var mc = modelCode.Trim();
        var e = await db.WarrantyExpires.AsNoTracking()
            .FirstOrDefaultAsync(x => x.OrgId == Org && x.ModelCode == mc);
        return e is null ? null : ToDto(e);
    }

    public async Task<WarrantyExpiresDto> CreateAsync(CreateWarrantyExpiresDto dto)
    {
        var modelCode = (dto.ModelCode ?? "").Trim();
        if (modelCode.Length < 1)
            throw new InvalidOperationException("Mã dòng xe (ModelCode) không được để trống.");

        // ModelCode là khóa nghiệp vụ duy nhất (Mst_WarrantyExpires_CheckDB - FlagExistToCheck = No)
        var exists = await db.WarrantyExpires.AnyAsync(x => x.OrgId == Org && x.ModelCode == modelCode);
        if (exists)
            throw new InvalidOperationException($"Định mức bảo hành cho dòng xe '{modelCode}' đã tồn tại.");

        // FK: Model phải tồn tại và đang hoạt động (Mst_CarModel_CheckDB - FlagExistToCheck = Yes, FlagActive = Active)
        await CheckModelExistsAsync(modelCode);

        ValidateWarranty(dto.WarrantyExpires, dto.WarrantyKM);

        var user = string.IsNullOrWhiteSpace(dto.ActionBy) ? "CHUYEN_VIEN_NPP" : dto.ActionBy.Trim();
        var entity = new WarrantyExpiresMaster
        {
            OrgId = Org,
            ModelCode = modelCode,
            ModelName = await GetModelNameAsync(modelCode),
            WarrantyExpires = dto.WarrantyExpires,
            WarrantyKM = dto.WarrantyKM,
            FlagActive = NormalizeFlag(dto.FlagActive),
            Remark = dto.Remark,
            LogLUDateTime = DateTime.Now,
            LogLUBy = user
        };
        db.WarrantyExpires.Add(entity);
        await db.SaveChangesAsync();
        return ToDto(entity);
    }

    public async Task<WarrantyExpiresDto?> UpdateAsync(string modelCode, UpdateWarrantyExpiresDto dto)
    {
        if (string.IsNullOrWhiteSpace(modelCode)) return null;
        var mc = modelCode.Trim();
        var entity = await db.WarrantyExpires
            .FirstOrDefaultAsync(x => x.OrgId == Org && x.ModelCode == mc);
        if (entity is null) return null;

        ValidateWarranty(dto.WarrantyExpires, dto.WarrantyKM);

        var user = string.IsNullOrWhiteSpace(dto.ActionBy) ? "CHUYEN_VIEN_NPP" : dto.ActionBy.Trim();
        entity.WarrantyExpires = dto.WarrantyExpires;
        entity.WarrantyKM = dto.WarrantyKM;
        entity.FlagActive = NormalizeFlag(dto.FlagActive);
        if (dto.Remark is not null) entity.Remark = dto.Remark;
        entity.LogLUDateTime = DateTime.Now;
        entity.LogLUBy = user;

        await db.SaveChangesAsync();
        return ToDto(entity);
    }

    public async Task<bool> DeleteAsync(string modelCode)
    {
        if (string.IsNullOrWhiteSpace(modelCode)) return false;
        var mc = modelCode.Trim();
        var entity = await db.WarrantyExpires
            .FirstOrDefaultAsync(x => x.OrgId == Org && x.ModelCode == mc);
        if (entity is null) return false;

        db.WarrantyExpires.Remove(entity);
        await db.SaveChangesAsync();
        return true;
    }

    public async Task<WarrantyExpiresImportResultDto> ImportAsync(List<WarrantyExpiresImportRowDto> rows)
    {
        if (rows is null || rows.Count < 1)
            throw new InvalidOperationException("Danh sách nhập (Import) rỗng, không có dòng dữ liệu nào.");

        // Phase 1: kiểm tra từng dòng (field validation)
        foreach (var r in rows)
        {
            var modelCode = (r.ModelCode ?? "").Trim();
            if (modelCode.Length < 1)
                throw new InvalidOperationException("Mã dòng xe (ModelCode) không được để trống.");
            ValidateWarranty(r.WarrantyExpires ?? 0m, r.WarrantyKM ?? 0m);
        }

        // Phase 2: kiểm tra trùng khóa ModelCode giữa các dòng
        var dup = rows.GroupBy(r => (r.ModelCode ?? "").Trim())
                      .FirstOrDefault(g => g.Count() > 1);
        if (dup is not null)
            throw new InvalidOperationException($"Mã dòng xe '{dup.Key}' bị trùng lặp trong danh sách nhập.");

        var user = "CHUYEN_VIEN_NPP";
        int inserted = 0, updated = 0;
        var codes = new List<string>();

        // Phase 3: upsert từng dòng
        foreach (var r in rows)
        {
            var modelCode = (r.ModelCode ?? "").Trim();
            var flag = NormalizeFlag(r.FlagActive);
            var entity = await db.WarrantyExpires
                .FirstOrDefaultAsync(x => x.OrgId == Org && x.ModelCode == modelCode);

            if (entity is not null)
            {
                entity.WarrantyExpires = r.WarrantyExpires ?? 0m;
                entity.WarrantyKM = r.WarrantyKM ?? 0m;
                entity.FlagActive = flag;
                entity.LogLUDateTime = DateTime.Now;
                entity.LogLUBy = user;
                updated++;
            }
            else
            {
                // FK: Model phải tồn tại và đang hoạt động (nhánh Insert)
                await CheckModelExistsAsync(modelCode);
                db.WarrantyExpires.Add(new WarrantyExpiresMaster
                {
                    OrgId = Org,
                    ModelCode = modelCode,
                    ModelName = await GetModelNameAsync(modelCode),
                    WarrantyExpires = r.WarrantyExpires ?? 0m,
                    WarrantyKM = r.WarrantyKM ?? 0m,
                    FlagActive = flag,
                    LogLUDateTime = DateTime.Now,
                    LogLUBy = user
                });
                inserted++;
            }
            codes.Add(modelCode);
        }

        await db.SaveChangesAsync();
        return new WarrantyExpiresImportResultDto(rows.Count, inserted, updated, codes);
    }

    public async Task<WarrantyExpiresStatsDto> GetStatsAsync()
    {
        var list = await db.WarrantyExpires.AsNoTracking().Where(x => x.OrgId == Org).ToListAsync();
        return new WarrantyExpiresStatsDto(
            TotalPolicies: list.Count,
            TotalActive: list.Count(x => x.FlagActive == "1"),
            TotalInactive: list.Count(x => x.FlagActive != "1"),
            AvgWarrantyExpires: list.Count > 0 ? Math.Round(list.Average(x => x.WarrantyExpires), 2) : 0m,
            AvgWarrantyKM: list.Count > 0 ? Math.Round(list.Average(x => x.WarrantyKM), 2) : 0m,
            MaxWarrantyExpires: list.Count > 0 ? list.Max(x => x.WarrantyExpires) : 0m,
            MaxWarrantyKM: list.Count > 0 ? list.Max(x => x.WarrantyKM) : 0m,
            ByModel: list.GroupBy(x => string.IsNullOrWhiteSpace(x.ModelCode) ? "UNKNOWN" : x.ModelCode)
                         .ToDictionary(g => g.Key, g => g.Count())
        );
    }

    // ===== Helpers =====
    private static void ValidateWarranty(decimal warrantyExpires, decimal warrantyKM)
    {
        if (warrantyExpires < 0)
            throw new InvalidOperationException("Thời hạn bảo hành (WarrantyExpires - số tháng) phải là số >= 0.");
        if (warrantyKM < 0)
            throw new InvalidOperationException("Số km giới hạn bảo hành (WarrantyKM) phải là số >= 0.");
    }

    private static string NormalizeFlag(string? flag)
        => string.IsNullOrWhiteSpace(flag) ? "1" : (flag.Trim() == "0" ? "0" : "1");

    /// <summary>Kiểm tra dòng xe tồn tại và đang hoạt động (Mst_CarModel_CheckDB).</summary>
    private async Task CheckModelExistsAsync(string modelCode)
    {
        var model = await db.Cars.AsNoTracking()
            .Where(c => c.OrgId == Org && c.ModelCode == modelCode)
            .Select(c => c.ModelCode)
            .FirstOrDefaultAsync();
        if (model is null)
            throw new InvalidOperationException($"Dòng xe '{modelCode}' không tồn tại trong danh mục dòng xe (Mst_CarModel).");
    }

    private async Task<string?> GetModelNameAsync(string modelCode)
    {
        var name = await db.Cars.AsNoTracking()
            .Where(c => c.OrgId == Org && c.ModelCode == modelCode)
            .Select(c => c.ModelName)
            .FirstOrDefaultAsync();
        return string.IsNullOrWhiteSpace(name) ? null : name;
    }

    private static WarrantyExpiresDto ToDto(WarrantyExpiresMaster e) => new(
        e.Id, e.ModelCode, e.ModelName, e.WarrantyExpires, e.WarrantyKM,
        e.FlagActive, e.FlagActive == "1", e.Remark, e.LogLUDateTime, e.LogLUBy);
}
