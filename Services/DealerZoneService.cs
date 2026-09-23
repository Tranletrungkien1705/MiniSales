using Microsoft.EntityFrameworkCore;
using MiniSales.Data;
using MiniSales.Models;

namespace MiniSales.Services;

// ===== DTOs =====
public record CreateZoneDto(
    string ZoneCode = "",
    string? ZoneName = null,
    string? Remark = null,
    string FlagActive = "1",
    string? ActionBy = null
);

public record UpdateZoneDto(
    string? ZoneName = null,
    string? Remark = null,
    string FlagActive = "1",
    string? ActionBy = null
);

public record ZoneDto(
    long Id,
    string ZoneCode,
    string? ZoneName,
    string? Remark,
    string FlagActive,
    bool IsActive,
    DateTime LogLUDTime,
    string? LogLUBy
);

public record CreateDealerZoneDto(
    string DealerCode = "",
    string ZoneCode = "",
    string? Remark = null,
    string FlagActive = "1",
    string? ActionBy = null
);

public record UpdateDealerZoneDto(
    string? Remark = null,
    string FlagActive = "1",
    string? ActionBy = null
);

public record DealerZoneDto(
    long Id,
    string DealerCode,
    string? DealerName,
    string ZoneCode,
    string? ZoneName,
    string? Remark,
    string FlagActive,
    bool IsActive,
    DateTime LogLUDTime,
    string? LogLUBy
);

public record DealerZoneImportRowDto(
    string? DealerCode,
    string? ZoneCode,
    string? Remark
);

public record DealerZoneImportResultDto(
    int TotalRows,
    int Inserted,
    int Reactivated,
    int Inactivated,
    List<string> DealerCodes
);

public record DealerZoneStatsDto(
    int TotalZones,
    int TotalActiveZones,
    int TotalMappings,
    int TotalActiveMappings,
    int TotalDealersAssigned,
    Dictionary<string, int> ByZone
);

public interface IDealerZoneService
{
    // Mst_Zone
    Task<List<ZoneDto>> SearchZonesAsync(string? zoneCode, string? flagActive);
    Task<ZoneDto?> GetZoneAsync(string zoneCode);
    Task<ZoneDto> CreateZoneAsync(CreateZoneDto dto);
    Task<ZoneDto?> UpdateZoneAsync(string zoneCode, UpdateZoneDto dto);
    Task<bool> DeleteZoneAsync(string zoneCode);

    // Mst_DealerZone
    Task<List<DealerZoneDto>> SearchDealerZonesAsync(string? dealerCode, string? zoneCode, string? flagActive);
    Task<DealerZoneDto?> GetDealerZoneAsync(string dealerCode, string zoneCode);
    Task<DealerZoneDto> CreateDealerZoneAsync(CreateDealerZoneDto dto);
    Task<DealerZoneDto?> UpdateDealerZoneAsync(string dealerCode, string zoneCode, UpdateDealerZoneDto dto);
    Task<bool> DeleteDealerZoneAsync(string dealerCode, string zoneCode);
    Task<DealerZoneImportResultDto> ImportDealerZonesAsync(List<DealerZoneImportRowDto> rows);

    Task<DealerZoneStatsDto> GetStatsAsync();
}

/// <summary>
/// Quản lý Phân vùng HTV (DMS.Sales Mst_Zone + Mst_DealerZone / DealerZone.cs / FrmMst_Zone / FrmMst_DealerZone):
/// - Mst_Zone: danh mục phân vùng kinh doanh HTV (MB = Miền Bắc, MN = Miền Nam...). ZoneCode là khóa nghiệp vụ
///   duy nhất; WinForm chỉ cho sửa cờ hiệu lực FlagActive (ZoneCode/ZoneName readonly sau khi tạo).
/// - Mst_DealerZone: gán 1 đại lý vào 1 phân vùng HTV. Khóa nghiệp vụ = (DealerCode, ZoneCode).
///   Quy tắc nghiệp vụ (giữ nguyên từ legacy Mst_DealerZone_AddX): MỖI ĐẠI LÝ chỉ thuộc 1 phân vùng ACTIVE —
///   khi gán phân vùng mới, các mapping active cũ của đại lý đó bị set FlagActive='0', rồi mapping mới được
///   insert/reactivate active. Hỗ trợ nhập hàng loạt (Import) từ Excel.
/// </summary>
public sealed class DealerZoneService(AppDbContext db, ITenantContext tenant) : IDealerZoneService
{
    private Guid Org => tenant.OrgId;

    // ===== Mst_Zone =====
    public async Task<List<ZoneDto>> SearchZonesAsync(string? zoneCode, string? flagActive)
    {
        var q = db.Zones.AsNoTracking().Where(x => x.OrgId == Org);
        if (!string.IsNullOrWhiteSpace(zoneCode))
        {
            var zc = zoneCode.Trim();
            q = q.Where(x => x.ZoneCode == zc);
        }
        if (!string.IsNullOrWhiteSpace(flagActive))
        {
            var fa = flagActive.Trim();
            q = q.Where(x => x.FlagActive == fa);
        }
        var list = await q.OrderBy(x => x.ZoneCode).ToListAsync();
        return list.Select(ToZoneDto).ToList();
    }

    public async Task<ZoneDto?> GetZoneAsync(string zoneCode)
    {
        if (string.IsNullOrWhiteSpace(zoneCode)) return null;
        var zc = zoneCode.Trim();
        var e = await db.Zones.AsNoTracking().FirstOrDefaultAsync(x => x.OrgId == Org && x.ZoneCode == zc);
        return e is null ? null : ToZoneDto(e);
    }

    public async Task<ZoneDto> CreateZoneAsync(CreateZoneDto dto)
    {
        var zoneCode = (dto.ZoneCode ?? "").Trim();
        if (zoneCode.Length < 1)
            throw new InvalidOperationException("Mã phân vùng (ZoneCode) không được để trống.");

        // ZoneCode là khóa nghiệp vụ duy nhất (Mst_Zone_CheckDB - FlagExistToCheck = No)
        if (await db.Zones.AnyAsync(x => x.OrgId == Org && x.ZoneCode == zoneCode))
            throw new InvalidOperationException($"Phân vùng '{zoneCode}' đã tồn tại.");

        var user = string.IsNullOrWhiteSpace(dto.ActionBy) ? "CHUYEN_VIEN_NPP" : dto.ActionBy.Trim();
        var entity = new ZoneMaster
        {
            OrgId = Org,
            ZoneCode = zoneCode,
            ZoneName = dto.ZoneName?.Trim(),
            Remark = dto.Remark,
            FlagActive = NormalizeFlag(dto.FlagActive),
            LogLUDTime = DateTime.Now,
            LogLUBy = user
        };
        db.Zones.Add(entity);
        await db.SaveChangesAsync();
        return ToZoneDto(entity);
    }

    public async Task<ZoneDto?> UpdateZoneAsync(string zoneCode, UpdateZoneDto dto)
    {
        if (string.IsNullOrWhiteSpace(zoneCode)) return null;
        var zc = zoneCode.Trim();
        var entity = await db.Zones.FirstOrDefaultAsync(x => x.OrgId == Org && x.ZoneCode == zc);
        if (entity is null) return null;

        var user = string.IsNullOrWhiteSpace(dto.ActionBy) ? "CHUYEN_VIEN_NPP" : dto.ActionBy.Trim();
        if (dto.ZoneName is not null) entity.ZoneName = dto.ZoneName.Trim();
        if (dto.Remark is not null) entity.Remark = dto.Remark;
        entity.FlagActive = NormalizeFlag(dto.FlagActive);
        entity.LogLUDTime = DateTime.Now;
        entity.LogLUBy = user;

        await db.SaveChangesAsync();
        return ToZoneDto(entity);
    }

    public async Task<bool> DeleteZoneAsync(string zoneCode)
    {
        if (string.IsNullOrWhiteSpace(zoneCode)) return false;
        var zc = zoneCode.Trim();
        var entity = await db.Zones.FirstOrDefaultAsync(x => x.OrgId == Org && x.ZoneCode == zc);
        if (entity is null) return false;

        // Chặn xóa khi còn mapping đại lý đang tham chiếu phân vùng (FK Mst_Zone -> Mst_DealerZone)
        var inUse = await db.DealerZones.AnyAsync(x => x.OrgId == Org && x.ZoneCode == zc);
        if (inUse)
            throw new InvalidOperationException($"Không thể xóa phân vùng '{zc}' vì đang có đại lý được thiết lập phân vùng này.");

        db.Zones.Remove(entity);
        await db.SaveChangesAsync();
        return true;
    }

    // ===== Mst_DealerZone =====
    public async Task<List<DealerZoneDto>> SearchDealerZonesAsync(string? dealerCode, string? zoneCode, string? flagActive)
    {
        var q = db.DealerZones.AsNoTracking().Where(x => x.OrgId == Org);
        if (!string.IsNullOrWhiteSpace(dealerCode))
        {
            var dc = dealerCode.Trim();
            q = q.Where(x => x.DealerCode == dc);
        }
        if (!string.IsNullOrWhiteSpace(zoneCode))
        {
            var zc = zoneCode.Trim();
            q = q.Where(x => x.ZoneCode == zc);
        }
        if (!string.IsNullOrWhiteSpace(flagActive))
        {
            var fa = flagActive.Trim();
            q = q.Where(x => x.FlagActive == fa);
        }
        var list = await q.OrderBy(x => x.DealerCode).ThenBy(x => x.ZoneCode).ToListAsync();
        return list.Select(ToDealerZoneDto).ToList();
    }

    public async Task<DealerZoneDto?> GetDealerZoneAsync(string dealerCode, string zoneCode)
    {
        if (string.IsNullOrWhiteSpace(dealerCode) || string.IsNullOrWhiteSpace(zoneCode)) return null;
        var dc = dealerCode.Trim();
        var zc = zoneCode.Trim();
        var e = await db.DealerZones.AsNoTracking()
            .FirstOrDefaultAsync(x => x.OrgId == Org && x.DealerCode == dc && x.ZoneCode == zc);
        return e is null ? null : ToDealerZoneDto(e);
    }

    public async Task<DealerZoneDto> CreateDealerZoneAsync(CreateDealerZoneDto dto)
    {
        var dealerCode = (dto.DealerCode ?? "").Trim();
        var zoneCode = (dto.ZoneCode ?? "").Trim();
        if (dealerCode.Length < 1)
            throw new InvalidOperationException("Mã đại lý (DealerCode) không được để trống.");
        if (zoneCode.Length < 1)
            throw new InvalidOperationException("Mã phân vùng (ZoneCode) không được để trống.");

        // FK: phân vùng phải tồn tại + active (Mst_Zone_CheckDB - FlagExistToCheck = Yes, FlagActive = Active)
        await CheckZoneExistsAsync(zoneCode);

        var user = string.IsNullOrWhiteSpace(dto.ActionBy) ? "CHUYEN_VIEN_NPP" : dto.ActionBy.Trim();
        var flag = NormalizeFlag(dto.FlagActive);

        // Quy tắc: mỗi đại lý chỉ 1 phân vùng ACTIVE — inactivate các mapping active cũ của đại lý.
        if (flag == "1")
            await InactivateOtherMappingsAsync(dealerCode, zoneCode, user);

        var entity = await db.DealerZones
            .FirstOrDefaultAsync(x => x.OrgId == Org && x.DealerCode == dealerCode && x.ZoneCode == zoneCode);

        if (entity is not null)
        {
            // Mapping đã tồn tại (inactive) → reactivate
            entity.FlagActive = flag;
            if (dto.Remark is not null) entity.Remark = dto.Remark;
            entity.LogLUDTime = DateTime.Now;
            entity.LogLUBy = user;
        }
        else
        {
            entity = new DealerZone
            {
                OrgId = Org,
                DealerCode = dealerCode,
                DealerName = await GetDealerNameAsync(dealerCode),
                ZoneCode = zoneCode,
                ZoneName = await GetZoneNameAsync(zoneCode),
                Remark = dto.Remark,
                FlagActive = flag,
                LogLUDTime = DateTime.Now,
                LogLUBy = user
            };
            db.DealerZones.Add(entity);
        }

        await db.SaveChangesAsync();
        return ToDealerZoneDto(entity);
    }

    public async Task<DealerZoneDto?> UpdateDealerZoneAsync(string dealerCode, string zoneCode, UpdateDealerZoneDto dto)
    {
        if (string.IsNullOrWhiteSpace(dealerCode) || string.IsNullOrWhiteSpace(zoneCode)) return null;
        var dc = dealerCode.Trim();
        var zc = zoneCode.Trim();
        var entity = await db.DealerZones
            .FirstOrDefaultAsync(x => x.OrgId == Org && x.DealerCode == dc && x.ZoneCode == zc);
        if (entity is null) return null;

        var user = string.IsNullOrWhiteSpace(dto.ActionBy) ? "CHUYEN_VIEN_NPP" : dto.ActionBy.Trim();
        var flag = NormalizeFlag(dto.FlagActive);

        if (flag == "1")
            await InactivateOtherMappingsAsync(dc, zc, user);

        entity.FlagActive = flag;
        if (dto.Remark is not null) entity.Remark = dto.Remark;
        entity.LogLUDTime = DateTime.Now;
        entity.LogLUBy = user;

        await db.SaveChangesAsync();
        return ToDealerZoneDto(entity);
    }

    public async Task<bool> DeleteDealerZoneAsync(string dealerCode, string zoneCode)
    {
        if (string.IsNullOrWhiteSpace(dealerCode) || string.IsNullOrWhiteSpace(zoneCode)) return false;
        var dc = dealerCode.Trim();
        var zc = zoneCode.Trim();
        var entity = await db.DealerZones
            .FirstOrDefaultAsync(x => x.OrgId == Org && x.DealerCode == dc && x.ZoneCode == zc);
        if (entity is null) return false;

        db.DealerZones.Remove(entity);
        await db.SaveChangesAsync();
        return true;
    }

    public async Task<DealerZoneImportResultDto> ImportDealerZonesAsync(List<DealerZoneImportRowDto> rows)
    {
        if (rows is null || rows.Count < 1)
            throw new InvalidOperationException("Danh sách nhập (Import) rỗng, không có dòng dữ liệu nào.");

        // Phase 1: kiểm tra từng dòng (field validation)
        for (int i = 0; i < rows.Count; i++)
        {
            var dealerCode = (rows[i].DealerCode ?? "").Trim();
            var zoneCode = (rows[i].ZoneCode ?? "").Trim();
            if (dealerCode.Length < 1)
                throw new InvalidOperationException($"Dòng {i}: Mã đại lý (DealerCode) không được để trống.");
            if (zoneCode.Length < 1)
                throw new InvalidOperationException($"Dòng {i}: Mã phân vùng (ZoneCode) không được để trống.");
        }

        // Phase 2: kiểm tra trùng khóa (DealerCode + ZoneCode) giữa các dòng
        var dup = rows.GroupBy(r => ((r.DealerCode ?? "").Trim(), (r.ZoneCode ?? "").Trim()))
                      .FirstOrDefault(g => g.Count() > 1);
        if (dup is not null)
            throw new InvalidOperationException($"Cặp (DealerCode='{dup.Key.Item1}', ZoneCode='{dup.Key.Item2}') bị trùng lặp trong danh sách nhập.");

        var user = "CHUYEN_VIEN_NPP";
        int inserted = 0, reactivated = 0, inactivated = 0;
        var dealerCodes = new List<string>();

        // Phase 3: DB checks per row + upsert (inactivate cũ + reactivate/insert mới)
        foreach (var r in rows)
        {
            var dealerCode = (r.DealerCode ?? "").Trim();
            var zoneCode = (r.ZoneCode ?? "").Trim();

            // FK: phân vùng phải tồn tại + active
            await CheckZoneExistsAsync(zoneCode);

            // Nếu mapping đã tồn tại VÀ đang active → lỗi (đã thiết lập rồi). Nếu inactive → sẽ reactivate.
            var existing = await db.DealerZones
                .FirstOrDefaultAsync(x => x.OrgId == Org && x.DealerCode == dealerCode && x.ZoneCode == zoneCode);
            if (existing is not null && existing.FlagActive == "1")
                throw new InvalidOperationException($"Đại lý '{dealerCode}' đã được thiết lập phân vùng '{zoneCode}' (đang hoạt động).");

            // Inactivate mọi mapping active cũ của đại lý (1 đại lý chỉ 1 zone active)
            inactivated += await InactivateOtherMappingsAsync(dealerCode, zoneCode, user);

            if (existing is not null)
            {
                existing.FlagActive = "1";
                if (r.Remark is not null) existing.Remark = r.Remark;
                existing.LogLUDTime = DateTime.Now;
                existing.LogLUBy = user;
                reactivated++;
            }
            else
            {
                db.DealerZones.Add(new DealerZone
                {
                    OrgId = Org,
                    DealerCode = dealerCode,
                    DealerName = await GetDealerNameAsync(dealerCode),
                    ZoneCode = zoneCode,
                    ZoneName = await GetZoneNameAsync(zoneCode),
                    Remark = r.Remark,
                    FlagActive = "1",
                    LogLUDTime = DateTime.Now,
                    LogLUBy = user
                });
                inserted++;
            }
            dealerCodes.Add(dealerCode);
        }

        await db.SaveChangesAsync();
        return new DealerZoneImportResultDto(rows.Count, inserted, reactivated, inactivated, dealerCodes);
    }

    public async Task<DealerZoneStatsDto> GetStatsAsync()
    {
        var zones = await db.Zones.AsNoTracking().Where(x => x.OrgId == Org).ToListAsync();
        var mappings = await db.DealerZones.AsNoTracking().Where(x => x.OrgId == Org).ToListAsync();

        return new DealerZoneStatsDto(
            TotalZones: zones.Count,
            TotalActiveZones: zones.Count(x => x.FlagActive == "1"),
            TotalMappings: mappings.Count,
            TotalActiveMappings: mappings.Count(x => x.FlagActive == "1"),
            TotalDealersAssigned: mappings.Where(x => x.FlagActive == "1").Select(x => x.DealerCode).Distinct().Count(),
            ByZone: mappings.Where(x => x.FlagActive == "1")
                            .GroupBy(x => string.IsNullOrWhiteSpace(x.ZoneCode) ? "UNKNOWN" : x.ZoneCode)
                            .ToDictionary(g => g.Key, g => g.Count())
        );
    }

    // ===== Helpers =====
    private static string NormalizeFlag(string? flag)
        => string.IsNullOrWhiteSpace(flag) ? "1" : (flag.Trim() == "0" ? "0" : "1");

    /// <summary>Kiểm tra phân vùng tồn tại và đang hoạt động (Mst_Zone_CheckDB).</summary>
    private async Task CheckZoneExistsAsync(string zoneCode)
    {
        var zone = await db.Zones.AsNoTracking()
            .Where(z => z.OrgId == Org && z.ZoneCode == zoneCode && z.FlagActive == "1")
            .Select(z => z.ZoneCode)
            .FirstOrDefaultAsync();
        if (zone is null)
            throw new InvalidOperationException($"Phân vùng '{zoneCode}' không tồn tại hoặc đã ngừng áp dụng (Mst_Zone).");
    }

    /// <summary>Set FlagActive='0' cho mọi mapping active khác của đại lý (1 đại lý chỉ 1 zone active). Trả về số dòng bị inactivate.</summary>
    private async Task<int> InactivateOtherMappingsAsync(string dealerCode, string keepZoneCode, string user)
    {
        var others = await db.DealerZones
            .Where(x => x.OrgId == Org && x.DealerCode == dealerCode && x.ZoneCode != keepZoneCode && x.FlagActive == "1")
            .ToListAsync();
        foreach (var o in others)
        {
            o.FlagActive = "0";
            o.LogLUDTime = DateTime.Now;
            o.LogLUBy = user;
        }
        return others.Count;
    }

    private async Task<string?> GetZoneNameAsync(string zoneCode)
    {
        var name = await db.Zones.AsNoTracking()
            .Where(z => z.OrgId == Org && z.ZoneCode == zoneCode)
            .Select(z => z.ZoneName)
            .FirstOrDefaultAsync();
        return string.IsNullOrWhiteSpace(name) ? null : name;
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

    private static ZoneDto ToZoneDto(ZoneMaster e) => new(
        e.Id, e.ZoneCode, e.ZoneName, e.Remark, e.FlagActive, e.FlagActive == "1", e.LogLUDTime, e.LogLUBy);

    private static DealerZoneDto ToDealerZoneDto(DealerZone e) => new(
        e.Id, e.DealerCode, e.DealerName, e.ZoneCode, e.ZoneName, e.Remark,
        e.FlagActive, e.FlagActive == "1", e.LogLUDTime, e.LogLUBy);
}
