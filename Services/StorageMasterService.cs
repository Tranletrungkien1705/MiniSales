using Microsoft.EntityFrameworkCore;
using MiniSales.Data;
using MiniSales.Models;

namespace MiniSales.Services;

// ===== DTOs =====
public record CreateStorageDto(
    string StorageCode = "",
    string ProvinceCode = "",
    string StorageName = "",
    string? StorageAddress = null,
    StorageType StorageType = StorageType.BT,
    string FlagInventoryCost = "0",
    string? ActionBy = null
);

public record UpdateStorageDto(
    string StorageName = "",
    string? StorageAddress = null,
    string FlagInventoryCost = "0",
    string? ActionBy = null
);

public record StorageDto(
    long Id,
    string StorageCode,
    string ProvinceCode,
    string? ProvinceName,
    string StorageName,
    string? StorageAddress,
    StorageType StorageType,
    string StorageTypeName,
    string FlagInventoryCost,
    bool IsInventoryCost,
    DateTime LogLUDateTime,
    string? LogLUBy
);

public record StorageImportRowDto(
    string? StorageCode,
    string? ProvinceCode,
    string? StorageName,
    string? StorageAddress,
    string? StorageType,
    string? FlagInventoryCost
);

public record StorageImportResultDto(
    int TotalRows,
    int Inserted,
    int Updated,
    List<string> StorageCodes
);

public record StorageStatsDto(
    int TotalStorages,
    int TotalInventoryCost,
    int TotalDT,
    int TotalBT,
    int TotalProvinces,
    Dictionary<string, int> ByType,
    Dictionary<string, int> ByProvince
);

public interface IStorageMasterService
{
    Task<List<StorageDto>> SearchAsync(string? storageCode, string? provinceCode, string? storageType, string? flagInventoryCost);
    Task<StorageDto?> GetAsync(string storageCode);
    Task<StorageDto> CreateAsync(CreateStorageDto dto);
    Task<StorageDto?> UpdateAsync(string storageCode, UpdateStorageDto dto);
    Task<bool> DeleteAsync(string storageCode);
    Task<StorageImportResultDto> ImportAsync(List<StorageImportRowDto> rows);
    Task<StorageStatsDto> GetStatsAsync();
}

/// <summary>
/// Quản lý Kho bãi (DMS.Sales Mst_Storage / Master.cs / Mst_Storage_Get|Create|Update|Delete|Import):
/// - Danh mục các kho chứa xe của HTC (xe lưu tại kho trước khi xuất cho đại lý). StorageCode là khóa nghiệp vụ duy nhất.
/// - Create: bắt buộc StorageCode, ProvinceCode (FK Mst_Province phải tồn tại), StorageName; StorageType chỉ nhận
///   DT (kho đóng thùng) / BT (kho bãi thường); FlagInventoryCost chỉ nhận "0"/"1"; chặn trùng StorageCode.
/// - Update: chỉ cho sửa StorageName / StorageAddress / FlagInventoryCost (ProvinceCode/StorageType readonly sau khi tạo).
/// - Delete: xóa kho theo StorageCode (phải tồn tại).
/// - Import: nhập hàng loạt từ Excel theo cơ chế upsert (dòng đã có thì cập nhật, dòng mới thì thêm), kiểm tra
///   field từng dòng + trùng khóa StorageCode giữa các dòng + FK ProvinceCode tồn tại.
/// </summary>
public sealed class StorageMasterService(AppDbContext db, ITenantContext tenant) : IStorageMasterService
{
    private Guid Org => tenant.OrgId;

    public async Task<List<StorageDto>> SearchAsync(string? storageCode, string? provinceCode, string? storageType, string? flagInventoryCost)
    {
        var q = db.Storages.AsNoTracking().Where(x => x.OrgId == Org);
        if (!string.IsNullOrWhiteSpace(storageCode))
        {
            var sc = storageCode.Trim();
            q = q.Where(x => x.StorageCode == sc);
        }
        if (!string.IsNullOrWhiteSpace(provinceCode))
        {
            var pc = provinceCode.Trim();
            q = q.Where(x => x.ProvinceCode == pc);
        }
        if (!string.IsNullOrWhiteSpace(storageType))
        {
            var st = ParseStorageType(storageType);
            q = q.Where(x => x.StorageType == st);
        }
        if (!string.IsNullOrWhiteSpace(flagInventoryCost))
        {
            var fc = flagInventoryCost.Trim();
            q = q.Where(x => x.FlagInventoryCost == fc);
        }
        var list = await q.OrderBy(x => x.StorageCode).ToListAsync();
        return list.Select(ToDto).ToList();
    }

    public async Task<StorageDto?> GetAsync(string storageCode)
    {
        if (string.IsNullOrWhiteSpace(storageCode)) return null;
        var sc = storageCode.Trim();
        var e = await db.Storages.AsNoTracking().FirstOrDefaultAsync(x => x.OrgId == Org && x.StorageCode == sc);
        return e is null ? null : ToDto(e);
    }

    public async Task<StorageDto> CreateAsync(CreateStorageDto dto)
    {
        var storageCode = (dto.StorageCode ?? "").Trim();
        var provinceCode = (dto.ProvinceCode ?? "").Trim();
        var storageName = (dto.StorageName ?? "").Trim();

        // Step 3: Validate master fields (Mst_Storage_Create).
        if (storageCode.Length < 1)
            throw new InvalidOperationException("Mã kho (StorageCode) không được để trống.");
        if (provinceCode.Length < 1)
            throw new InvalidOperationException("Mã tỉnh/thành (ProvinceCode) không được để trống.");
        if (storageName.Length < 1)
            throw new InvalidOperationException("Tên kho (StorageName) không được để trống.");

        var flagInventoryCost = NormalizeFlag(dto.FlagInventoryCost);

        // Step 4: CheckDB own PK — StorageCode PHẢI chưa tồn tại (Mst_Storage_CheckDB - FlagExistToCheck = No).
        if (await db.Storages.AnyAsync(x => x.OrgId == Org && x.StorageCode == storageCode))
            throw new InvalidOperationException($"Kho '{storageCode}' đã tồn tại.");

        // Step 5: CheckDB FK — ProvinceCode REFERENCES Mst_Province (phải tồn tại).
        await CheckProvinceExistsAsync(provinceCode);

        var user = string.IsNullOrWhiteSpace(dto.ActionBy) ? "CHUYEN_VIEN_NPP" : dto.ActionBy.Trim();
        var entity = new StorageMaster
        {
            OrgId = Org,
            StorageCode = storageCode,
            ProvinceCode = provinceCode,
            ProvinceName = await GetProvinceNameAsync(provinceCode),
            StorageName = storageName,
            StorageAddress = dto.StorageAddress?.Trim(),
            StorageType = dto.StorageType,
            FlagInventoryCost = flagInventoryCost,
            LogLUDateTime = DateTime.Now,
            LogLUBy = user
        };
        db.Storages.Add(entity);
        await db.SaveChangesAsync();
        return ToDto(entity);
    }

    public async Task<StorageDto?> UpdateAsync(string storageCode, UpdateStorageDto dto)
    {
        if (string.IsNullOrWhiteSpace(storageCode)) return null;
        var sc = storageCode.Trim();
        var entity = await db.Storages.FirstOrDefaultAsync(x => x.OrgId == Org && x.StorageCode == sc);
        if (entity is null) return null;

        // Nghiệp vụ Update hardcode 3 field updateable: StorageName, StorageAddress, FlagInventoryCost.
        var storageName = (dto.StorageName ?? "").Trim();
        if (storageName.Length < 1)
            throw new InvalidOperationException("Tên kho (StorageName) không được để trống.");

        var user = string.IsNullOrWhiteSpace(dto.ActionBy) ? "CHUYEN_VIEN_NPP" : dto.ActionBy.Trim();
        entity.StorageName = storageName;
        entity.StorageAddress = dto.StorageAddress?.Trim();
        entity.FlagInventoryCost = NormalizeFlag(dto.FlagInventoryCost);
        entity.LogLUDateTime = DateTime.Now;
        entity.LogLUBy = user;

        await db.SaveChangesAsync();
        return ToDto(entity);
    }

    public async Task<bool> DeleteAsync(string storageCode)
    {
        if (string.IsNullOrWhiteSpace(storageCode)) return false;
        var sc = storageCode.Trim();
        var entity = await db.Storages.FirstOrDefaultAsync(x => x.OrgId == Org && x.StorageCode == sc);
        if (entity is null) return false;

        db.Storages.Remove(entity);
        await db.SaveChangesAsync();
        return true;
    }

    public async Task<StorageImportResultDto> ImportAsync(List<StorageImportRowDto> rows)
    {
        if (rows is null || rows.Count < 1)
            throw new InvalidOperationException("Danh sách nhập (Import) rỗng, không có dòng dữ liệu nào.");

        // Phase 1: Field syntax validation per row (fail-fast — throw dòng đầu tiên gặp lỗi, giống Create).
        for (int i = 0; i < rows.Count; i++)
        {
            var storageCode = (rows[i].StorageCode ?? "").Trim();
            var provinceCode = (rows[i].ProvinceCode ?? "").Trim();
            var storageName = (rows[i].StorageName ?? "").Trim();
            if (storageCode.Length < 1)
                throw new InvalidOperationException($"Dòng {i}: Mã kho (StorageCode) không được để trống.");
            if (provinceCode.Length < 1)
                throw new InvalidOperationException($"Dòng {i}: Mã tỉnh/thành (ProvinceCode) không được để trống.");
            if (storageName.Length < 1)
                throw new InvalidOperationException($"Dòng {i}: Tên kho (StorageName) không được để trống.");
            // StorageType: NOT NULL, chỉ accept "DT" / "BT".
            ParseStorageTypeStrict(rows[i].StorageType, i);
            // FlagInventoryCost: NOT NULL, chỉ accept "0" / "1".
            NormalizeFlagStrict(rows[i].FlagInventoryCost, i);
        }

        // Phase 2: Cross-row duplicate StorageCode trong danh sách (fail-fast).
        var dup = rows.GroupBy(r => (r.StorageCode ?? "").Trim())
                      .FirstOrDefault(g => g.Count() > 1);
        if (dup is not null)
            throw new InvalidOperationException($"Mã kho '{dup.Key}' bị trùng lặp trong danh sách nhập.");

        var user = "CHUYEN_VIEN_NPP";
        int inserted = 0, updated = 0;
        var storageCodes = new List<string>();

        // Phase 3: DB CheckDB per row (FK ProvinceCode tồn tại) + phân luồng UPSERT.
        foreach (var r in rows)
        {
            var storageCode = (r.StorageCode ?? "").Trim();
            var provinceCode = (r.ProvinceCode ?? "").Trim();

            // Check FK ProvinceCode — PHẢI TỒN TẠI.
            await CheckProvinceExistsAsync(provinceCode);

            var existing = await db.Storages
                .FirstOrDefaultAsync(x => x.OrgId == Org && x.StorageCode == storageCode);

            if (existing is not null)
            {
                // ĐÃ TỒN TẠI -> UPDATE (mẫu Mst_Storage_Update: chỉ sửa StorageName/StorageAddress/FlagInventoryCost).
                existing.StorageName = (r.StorageName ?? "").Trim();
                existing.StorageAddress = r.StorageAddress?.Trim();
                existing.FlagInventoryCost = NormalizeFlag(r.FlagInventoryCost);
                existing.LogLUDateTime = DateTime.Now;
                existing.LogLUBy = user;
                updated++;
            }
            else
            {
                // CHƯA TỒN TẠI -> INSERT (mẫu Mst_Storage_Create: ghi đủ cột).
                db.Storages.Add(new StorageMaster
                {
                    OrgId = Org,
                    StorageCode = storageCode,
                    ProvinceCode = provinceCode,
                    ProvinceName = await GetProvinceNameAsync(provinceCode),
                    StorageName = (r.StorageName ?? "").Trim(),
                    StorageAddress = r.StorageAddress?.Trim(),
                    StorageType = ParseStorageTypeStrict(r.StorageType, -1),
                    FlagInventoryCost = NormalizeFlag(r.FlagInventoryCost),
                    LogLUDateTime = DateTime.Now,
                    LogLUBy = user
                });
                inserted++;
            }
            storageCodes.Add(storageCode);
        }

        await db.SaveChangesAsync();
        return new StorageImportResultDto(rows.Count, inserted, updated, storageCodes);
    }

    public async Task<StorageStatsDto> GetStatsAsync()
    {
        var list = await db.Storages.AsNoTracking().Where(x => x.OrgId == Org).ToListAsync();

        return new StorageStatsDto(
            TotalStorages: list.Count,
            TotalInventoryCost: list.Count(x => x.FlagInventoryCost == "1"),
            TotalDT: list.Count(x => x.StorageType == StorageType.DT),
            TotalBT: list.Count(x => x.StorageType == StorageType.BT),
            TotalProvinces: list.Select(x => x.ProvinceCode).Distinct().Count(),
            ByType: list.GroupBy(x => TypeName(x.StorageType))
                        .ToDictionary(g => g.Key, g => g.Count()),
            ByProvince: list.GroupBy(x => string.IsNullOrWhiteSpace(x.ProvinceCode) ? "UNKNOWN" : x.ProvinceCode)
                            .ToDictionary(g => g.Key, g => g.Count())
        );
    }

    // ===== Helpers =====
    private static string NormalizeFlag(string? flag)
        => string.IsNullOrWhiteSpace(flag) ? "0" : (flag.Trim() == "1" ? "1" : "0");

    private static string NormalizeFlagStrict(string? flag, int rowIndex)
    {
        var f = (flag ?? "").Trim();
        if (f != "0" && f != "1")
        {
            var prefix = rowIndex >= 0 ? $"Dòng {rowIndex}: " : "";
            throw new InvalidOperationException($"{prefix}Cờ tính chi phí lưu kho (FlagInventoryCost) chỉ nhận giá trị '0' hoặc '1' (nhập: '{flag}').");
        }
        return f;
    }

    private static StorageType ParseStorageType(string value)
    {
        var v = value.Trim().ToUpperInvariant();
        return v == "DT" ? StorageType.DT : StorageType.BT;
    }

    private static StorageType ParseStorageTypeStrict(string? value, int rowIndex)
    {
        var v = (value ?? "").Trim().ToUpperInvariant();
        if (v != "DT" && v != "BT")
        {
            var prefix = rowIndex >= 0 ? $"Dòng {rowIndex}: " : "";
            throw new InvalidOperationException($"{prefix}Loại kho (StorageType) chỉ nhận giá trị 'DT' (kho đóng thùng) hoặc 'BT' (kho bãi thường) (nhập: '{value}').");
        }
        return v == "DT" ? StorageType.DT : StorageType.BT;
    }

    private static string TypeName(StorageType t) => t switch
    {
        StorageType.DT => "Kho đóng thùng",
        StorageType.BT => "Kho bãi thường",
        _ => "Không xác định"
    };

    /// <summary>Kiểm tra tỉnh/thành tồn tại trong danh mục (Mst_Province_CheckDB - FlagExistToCheck = Yes).</summary>
    private async Task CheckProvinceExistsAsync(string provinceCode)
    {
        var exists = await db.Provinces.AsNoTracking()
            .AnyAsync(p => p.OrgId == Org && p.ProvinceCode == provinceCode);
        if (!exists)
            throw new InvalidOperationException($"Tỉnh/thành '{provinceCode}' không tồn tại trong danh mục (Mst_Province).");
    }

    /// <summary>Tra cứu tên tỉnh/thành từ danh mục (Mst_Province.ProvinceName).</summary>
    private async Task<string?> GetProvinceNameAsync(string provinceCode)
    {
        var name = await db.Provinces.AsNoTracking()
            .Where(p => p.OrgId == Org && p.ProvinceCode == provinceCode)
            .Select(p => p.ProvinceName)
            .FirstOrDefaultAsync();
        return string.IsNullOrWhiteSpace(name) ? null : name;
    }

    private static StorageDto ToDto(StorageMaster e) => new(
        e.Id, e.StorageCode, e.ProvinceCode, e.ProvinceName, e.StorageName, e.StorageAddress,
        e.StorageType, TypeName(e.StorageType), e.FlagInventoryCost, e.FlagInventoryCost == "1",
        e.LogLUDateTime, e.LogLUBy);
}
