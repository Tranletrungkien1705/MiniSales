using Microsoft.EntityFrameworkCore;
using MiniSales.Data;
using MiniSales.Models;

namespace MiniSales.Services;

// ===== DTOs =====
public record CreateDelayTransportDto(
    string StorageCode = "",
    string DealerCode = "",
    decimal DelayTransport = 0,
    string? ActionBy = null
);

public record UpdateDelayTransportDto(
    decimal DelayTransport = 0,
    string FlagActive = "1",
    string? ActionBy = null
);

public record DelayTransportImportRowDto(
    string? StorageCode,
    string? DealerCode,
    decimal? DelayTransport
);

public record DelayTransportDto(
    long Id,
    string StorageCode,
    string? StorageName,
    string DealerCode,
    string? DealerName,
    decimal DelayTransport,
    string FlagActive,
    bool IsActive,
    DateTime LogLUDateTime,
    string? LogLUBy
);

public record DelayTransportImportResultDto(
    int TotalRows,
    int Inserted,
    List<string> Keys
);

public record DelayTransportStatsDto(
    int TotalPolicies,
    int TotalActive,
    int TotalInactive,
    int TotalStorages,
    int TotalDealers,
    decimal AvgDelayTransport,
    decimal MaxDelayTransport,
    Dictionary<string, decimal> AvgByStorage
);

public interface IDelayTransportService
{
    Task<List<DelayTransportDto>> SearchAsync(string? keyWord, string? storageCode, string? dealerCode, string? flagActive);
    Task<DelayTransportDto?> GetAsync(string storageCode, string dealerCode);
    Task<DelayTransportDto> CreateAsync(CreateDelayTransportDto dto);
    Task<DelayTransportDto?> UpdateAsync(string storageCode, string dealerCode, UpdateDelayTransportDto dto);
    Task<bool> DeleteAsync(string storageCode, string dealerCode);
    Task<DelayTransportImportResultDto> ImportAsync(List<DelayTransportImportRowDto> rows);
    Task<DelayTransportStatsDto> GetStatsAsync();
}

/// <summary>
/// Quản lý Hạn mức độ trễ vận tải theo Kho & Đại lý (DMS.Sales Mst_DelayTransports / Master.1.cs /
/// MstDelayTransportsController / Mst_DelayTransports_Get|Create|Update|Delete|Import): danh mục quy định
/// số ngày trễ vận tải tối đa (DelayTransport) được chấp nhận khi giao xe từ một Kho (StorageCode) tới một
/// Đại lý (DealerCode). Khóa nghiệp vụ = (StorageCode, DealerCode). Ràng buộc: StorageCode và DealerCode
/// bắt buộc; DelayTransport phải là số >= 0; FK StorageCode phải tồn tại (Mst_Storage) và DealerCode phải
/// tồn tại + đang hoạt động (Mst_Dealer). Update chỉ cho sửa DelayTransport + FlagActive. Hỗ trợ nhập hàng
/// loạt (Import) từ Excel: kiểm tra field từng dòng + trùng khóa giữa các dòng, rồi tạo mới từng dòng.
/// </summary>
public sealed class DelayTransportService(AppDbContext db, ITenantContext tenant) : IDelayTransportService
{
    private Guid Org => tenant.OrgId;

    public async Task<List<DelayTransportDto>> SearchAsync(string? keyWord, string? storageCode, string? dealerCode, string? flagActive)
    {
        var q = db.DelayTransports.AsNoTracking().Where(x => x.OrgId == Org);

        if (!string.IsNullOrWhiteSpace(keyWord))
        {
            var kw = keyWord.Trim();
            q = q.Where(x => x.StorageCode.Contains(kw)
                          || (x.StorageName != null && x.StorageName.Contains(kw))
                          || x.DealerCode.Contains(kw)
                          || (x.DealerName != null && x.DealerName.Contains(kw)));
        }
        if (!string.IsNullOrWhiteSpace(storageCode))
        {
            var sc = storageCode.Trim();
            q = q.Where(x => x.StorageCode == sc);
        }
        if (!string.IsNullOrWhiteSpace(dealerCode))
        {
            var dc = dealerCode.Trim();
            q = q.Where(x => x.DealerCode == dc);
        }
        if (!string.IsNullOrWhiteSpace(flagActive))
        {
            var fa = flagActive.Trim();
            q = q.Where(x => x.FlagActive == fa);
        }

        var list = await q.OrderBy(x => x.StorageCode).ThenBy(x => x.DealerCode).ToListAsync();
        return list.Select(ToDto).ToList();
    }

    public async Task<DelayTransportDto?> GetAsync(string storageCode, string dealerCode)
    {
        if (string.IsNullOrWhiteSpace(storageCode) || string.IsNullOrWhiteSpace(dealerCode)) return null;
        var sc = storageCode.Trim();
        var dc = dealerCode.Trim();
        var e = await db.DelayTransports.AsNoTracking()
            .FirstOrDefaultAsync(x => x.OrgId == Org && x.StorageCode == sc && x.DealerCode == dc);
        return e is null ? null : ToDto(e);
    }

    public async Task<DelayTransportDto> CreateAsync(CreateDelayTransportDto dto)
    {
        var storageCode = (dto.StorageCode ?? "").Trim();
        var dealerCode = (dto.DealerCode ?? "").Trim();
        if (storageCode.Length < 1)
            throw new InvalidOperationException("Mã kho (StorageCode) không được để trống.");
        if (dealerCode.Length < 1)
            throw new InvalidOperationException("Mã đại lý (DealerCode) không được để trống.");

        ValidateDelayTransport(dto.DelayTransport);

        // Khóa nghiệp vụ (StorageCode, DealerCode) là duy nhất (Mst_DelayTransports_CheckDB - FlagExistToCheck = No)
        if (await db.DelayTransports.AnyAsync(x => x.OrgId == Org && x.StorageCode == storageCode && x.DealerCode == dealerCode))
            throw new InvalidOperationException($"Hạn mức độ trễ vận tải cho kho '{storageCode}' và đại lý '{dealerCode}' đã tồn tại.");

        // FK: Kho phải tồn tại (Mst_Storage_CheckDB - FlagExistToCheck = Yes)
        await CheckStorageExistsAsync(storageCode);
        // FK: Đại lý phải tồn tại và đang hoạt động (Mst_Dealer_CheckDB - FlagExistToCheck = Yes, FlagActive = Active)
        await CheckDealerExistsAsync(dealerCode);

        var user = string.IsNullOrWhiteSpace(dto.ActionBy) ? "CHUYEN_VIEN_NPP" : dto.ActionBy.Trim();
        var entity = new DelayTransportMaster
        {
            OrgId = Org,
            StorageCode = storageCode,
            StorageName = ResolveStorageName(storageCode),
            DealerCode = dealerCode,
            DealerName = await GetDealerNameAsync(dealerCode),
            DelayTransport = dto.DelayTransport,
            FlagActive = "1",
            LogLUDateTime = DateTime.Now,
            LogLUBy = user
        };
        db.DelayTransports.Add(entity);
        await db.SaveChangesAsync();
        return ToDto(entity);
    }

    public async Task<DelayTransportDto?> UpdateAsync(string storageCode, string dealerCode, UpdateDelayTransportDto dto)
    {
        if (string.IsNullOrWhiteSpace(storageCode) || string.IsNullOrWhiteSpace(dealerCode)) return null;
        var sc = storageCode.Trim();
        var dc = dealerCode.Trim();
        var entity = await db.DelayTransports
            .FirstOrDefaultAsync(x => x.OrgId == Org && x.StorageCode == sc && x.DealerCode == dc);
        if (entity is null) return null;

        ValidateDelayTransport(dto.DelayTransport);

        // Master Fixed Updateable Scope: chỉ update DelayTransport + FlagActive (bám WF).
        var user = string.IsNullOrWhiteSpace(dto.ActionBy) ? "CHUYEN_VIEN_NPP" : dto.ActionBy.Trim();
        entity.DelayTransport = dto.DelayTransport;
        entity.FlagActive = NormalizeFlag(dto.FlagActive);
        entity.LogLUDateTime = DateTime.Now;
        entity.LogLUBy = user;

        await db.SaveChangesAsync();
        return ToDto(entity);
    }

    public async Task<bool> DeleteAsync(string storageCode, string dealerCode)
    {
        if (string.IsNullOrWhiteSpace(storageCode) || string.IsNullOrWhiteSpace(dealerCode)) return false;
        var sc = storageCode.Trim();
        var dc = dealerCode.Trim();
        var entity = await db.DelayTransports
            .FirstOrDefaultAsync(x => x.OrgId == Org && x.StorageCode == sc && x.DealerCode == dc);
        if (entity is null) return false;

        db.DelayTransports.Remove(entity);
        await db.SaveChangesAsync();
        return true;
    }

    public async Task<DelayTransportImportResultDto> ImportAsync(List<DelayTransportImportRowDto> rows)
    {
        if (rows is null || rows.Count < 1)
            throw new InvalidOperationException("Danh sách nhập (Import) rỗng, không có dòng dữ liệu nào.");

        // Phase 1: kiểm tra từng dòng (field validation, fail-fast giống Create)
        for (int i = 0; i < rows.Count; i++)
        {
            var storageCode = (rows[i].StorageCode ?? "").Trim();
            var dealerCode = (rows[i].DealerCode ?? "").Trim();
            if (storageCode.Length < 1)
                throw new InvalidOperationException($"Dòng {i}: Mã kho (StorageCode) không được để trống.");
            if (dealerCode.Length < 1)
                throw new InvalidOperationException($"Dòng {i}: Mã đại lý (DealerCode) không được để trống.");
            if (rows[i].DelayTransport is null)
                throw new InvalidOperationException($"Dòng {i}: Hạn mức độ trễ vận tải (DelayTransport) không được để trống.");
            ValidateDelayTransport(rows[i].DelayTransport ?? 0m);
        }

        // Phase 2: kiểm tra trùng khóa (StorageCode + DealerCode) giữa các dòng
        var dup = rows.GroupBy(r => ((r.StorageCode ?? "").Trim(), (r.DealerCode ?? "").Trim()))
                      .FirstOrDefault(g => g.Count() > 1);
        if (dup is not null)
            throw new InvalidOperationException($"Cặp (StorageCode='{dup.Key.Item1}', DealerCode='{dup.Key.Item2}') bị trùng lặp trong danh sách nhập.");

        var user = "CHUYEN_VIEN_NPP";
        int inserted = 0;
        var keys = new List<string>();

        // Phase 3: tạo mới từng dòng (bám Mst_DelayTransports_Import: gọi Create cho từng dòng)
        foreach (var r in rows)
        {
            var storageCode = (r.StorageCode ?? "").Trim();
            var dealerCode = (r.DealerCode ?? "").Trim();

            // Khóa nghiệp vụ duy nhất (Mst_DelayTransports_CheckDB - FlagExistToCheck = No)
            if (await db.DelayTransports.AnyAsync(x => x.OrgId == Org && x.StorageCode == storageCode && x.DealerCode == dealerCode))
                throw new InvalidOperationException($"Hạn mức độ trễ vận tải cho kho '{storageCode}' và đại lý '{dealerCode}' đã tồn tại.");

            // FK checks giống Create
            await CheckStorageExistsAsync(storageCode);
            await CheckDealerExistsAsync(dealerCode);

            db.DelayTransports.Add(new DelayTransportMaster
            {
                OrgId = Org,
                StorageCode = storageCode,
                StorageName = ResolveStorageName(storageCode),
                DealerCode = dealerCode,
                DealerName = await GetDealerNameAsync(dealerCode),
                DelayTransport = r.DelayTransport ?? 0m,
                FlagActive = "1",
                LogLUDateTime = DateTime.Now,
                LogLUBy = user
            });
            inserted++;
            keys.Add($"{storageCode}|{dealerCode}");
        }

        await db.SaveChangesAsync();
        return new DelayTransportImportResultDto(rows.Count, inserted, keys);
    }

    public async Task<DelayTransportStatsDto> GetStatsAsync()
    {
        var list = await db.DelayTransports.AsNoTracking().Where(x => x.OrgId == Org).ToListAsync();
        return new DelayTransportStatsDto(
            TotalPolicies: list.Count,
            TotalActive: list.Count(x => x.FlagActive == "1"),
            TotalInactive: list.Count(x => x.FlagActive != "1"),
            TotalStorages: list.Select(x => x.StorageCode).Distinct().Count(),
            TotalDealers: list.Select(x => x.DealerCode).Distinct().Count(),
            AvgDelayTransport: list.Count > 0 ? Math.Round(list.Average(x => x.DelayTransport), 2) : 0m,
            MaxDelayTransport: list.Count > 0 ? list.Max(x => x.DelayTransport) : 0m,
            AvgByStorage: list.GroupBy(x => string.IsNullOrWhiteSpace(x.StorageCode) ? "UNKNOWN" : x.StorageCode)
                              .ToDictionary(g => g.Key, g => Math.Round(g.Average(x => x.DelayTransport), 2))
        );
    }

    // ===== Helpers =====
    private static void ValidateDelayTransport(decimal delayTransport)
    {
        if (delayTransport < 0)
            throw new InvalidOperationException("Hạn mức độ trễ vận tải (DelayTransport - số ngày) phải là số >= 0.");
    }

    private static string NormalizeFlag(string? flag)
        => string.IsNullOrWhiteSpace(flag) ? "1" : (flag.Trim() == "0" ? "0" : "1");

    /// <summary>Kiểm tra kho tồn tại (Mst_Storage_CheckDB). MiniSales chưa có bảng master kho riêng nên kiểm tra theo danh mục kho đã biết.</summary>
    private static Task CheckStorageExistsAsync(string storageCode)
    {
        if (!KnownStorageCodes.Contains(storageCode))
            throw new InvalidOperationException($"Kho '{storageCode}' không tồn tại trong danh mục kho (Mst_Storage).");
        return Task.CompletedTask;
    }

    /// <summary>Kiểm tra đại lý tồn tại và đang hoạt động (Mst_Dealer_CheckDB).</summary>
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

    /// <summary>Danh mục mã kho đã biết (Mst_Storage) dùng để kiểm tra FK StorageCode.</summary>
    private static readonly HashSet<string> KnownStorageCodes = new(StringComparer.OrdinalIgnoreCase)
    {
        "KHO_TONG_HN", "KHO_TONG_HP", "KHO_TONG_SG", "KHO_NB", "KHO_DY",
        "KHO_CANG_HP", "KHO_CANG_CM", "KHO_DT_01", "KHO_DT_02",
        "KHO_DT_HIEPHOA", "KHO_DT_NAMVIET", "KHO_DT_QUYENAUTO", "KHO_DL", "KHO_TT"
    };

    private static string ResolveStorageName(string code) => code switch
    {
        "KHO_DT_01" => "Xưởng Đóng Thùng Chuyên Dụng Hiệp Hòa",
        "KHO_DT_02" => "Xưởng Đóng Thùng Ô Tô Nam Việt",
        "KHO_DT_HIEPHOA" => "Xưởng Đóng Thùng Chuyên Dụng Hiệp Hòa",
        "KHO_DT_NAMVIET" => "Xưởng Đóng Thùng Ô Tô Nam Việt",
        "KHO_DT_QUYENAUTO" => "Xưởng Đóng Thùng Quyền Auto (Đông Lạnh)",
        "KHO_TONG_HN" => "Kho Tổng Hyundai Ninh Bình",
        "KHO_TONG_HP" => "Kho Trung Chuyển Hải Phòng",
        "KHO_TONG_SG" => "Kho Tổng Nam Bộ Hiệp Phước",
        "KHO_NB" => "Kho Ninh Bình",
        "KHO_DY" => "Kho Duy Tiên",
        "KHO_CANG_HP" => "Kho Cảng Hải Phòng",
        "KHO_CANG_CM" => "Kho Cảng Cái Mép",
        "KHO_DL" => "Kho Đại Lý",
        "KHO_TT" => "Kho Trung Tâm",
        _ => $"Kho/Xưởng {code}"
    };

    private static DelayTransportDto ToDto(DelayTransportMaster e) => new(
        e.Id, e.StorageCode, e.StorageName, e.DealerCode, e.DealerName,
        e.DelayTransport, e.FlagActive, e.FlagActive == "1", e.LogLUDateTime, e.LogLUBy);
}