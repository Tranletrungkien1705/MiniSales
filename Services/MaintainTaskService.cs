using Microsoft.EntityFrameworkCore;
using MiniSales.Data;
using MiniSales.Models;

namespace MiniSales.Services;

// ===== DTOs =====
public record CreateMaintainTaskDto(
    string MtnTkCode = "",
    string MtnTkName = "",
    string? MtnTkType = null,
    string FlagActive = "1",
    string? ActionBy = null
);

public record UpdateMaintainTaskDto(
    string? MtnTkName = null,
    string? MtnTkType = null,
    string FlagActive = "1",
    string? ActionBy = null
);

public record MaintainTaskDto(
    long Id,
    string MtnTkCode,
    string MtnTkName,
    string? MtnTkType,
    string FlagActive,
    bool IsActive,
    DateTime LogLUDateTime,
    string? LogLUBy
);

public record MaintainTaskImportRowDto(
    string? MtnTkCode,
    string? MtnTkName,
    string? MtnTkType
);

public record CreateMaintainTaskItemDto(
    string MtnTkCode = "",
    string MtnTkItemCode = "",
    string MtnTkItemName = "",
    int ViewIdx = 0,
    string FlagActive = "1",
    string? ActionBy = null
);

public record UpdateMaintainTaskItemDto(
    string? MtnTkItemName = null,
    int? ViewIdx = null,
    string FlagActive = "1",
    string? ActionBy = null
);

public record MaintainTaskItemDto(
    long Id,
    string MtnTkCode,
    string? MtnTkName,
    string MtnTkItemCode,
    string MtnTkItemName,
    int ViewIdx,
    string FlagActive,
    bool IsActive,
    DateTime LogLUDateTime,
    string? LogLUBy
);

public record MaintainTaskItemImportRowDto(
    string? MtnTkCode,
    string? MtnTkItemCode,
    string? MtnTkItemName,
    int? ViewIdx
);

public record MaintainTaskImportResultDto(
    int TotalRows,
    int Inserted,
    int Updated,
    List<string> MtnTkCodes
);

public record MaintainTaskItemImportResultDto(
    int TotalRows,
    int Inserted,
    int Updated,
    List<string> Keys
);

public record MaintainTaskStatsDto(
    int TotalTasks,
    int TotalActiveTasks,
    int TotalItems,
    int TotalActiveItems,
    Dictionary<string, int> ItemsByTask
);

public interface IMaintainTaskService
{
    // Mst_MaintainTask
    Task<List<MaintainTaskDto>> SearchTasksAsync(string? keyWord, string? flagActive);
    Task<MaintainTaskDto?> GetTaskAsync(string mtnTkCode);
    Task<MaintainTaskDto> CreateTaskAsync(CreateMaintainTaskDto dto);
    Task<MaintainTaskDto?> UpdateTaskAsync(string mtnTkCode, UpdateMaintainTaskDto dto);
    Task<bool> DeleteTaskAsync(string mtnTkCode);
    Task<MaintainTaskImportResultDto> ImportTasksAsync(List<MaintainTaskImportRowDto> rows);

    // Mst_MaintainTaskItem
    Task<List<MaintainTaskItemDto>> SearchItemsAsync(string? keyWord, string? mtnTkCode, string? flagActive);
    Task<MaintainTaskItemDto?> GetItemAsync(string mtnTkCode, string mtnTkItemCode);
    Task<MaintainTaskItemDto> CreateItemAsync(CreateMaintainTaskItemDto dto);
    Task<MaintainTaskItemDto?> UpdateItemAsync(string mtnTkCode, string mtnTkItemCode, UpdateMaintainTaskItemDto dto);
    Task<bool> DeleteItemAsync(string mtnTkCode, string mtnTkItemCode);
    Task<MaintainTaskItemImportResultDto> ImportItemsAsync(List<MaintainTaskItemImportRowDto> rows);

    Task<MaintainTaskStatsDto> GetStatsAsync();
}

/// <summary>
/// Quản lý Danh mục Hạng mục Công việc Bảo dưỡng (DMS.Sales Mst_MaintainTask + Mst_MaintainTaskItem /
/// Master.cs / Master.1.cs / Mst_MaintainTask_Get|Create|Update|Delete|Import):
/// - Mst_MaintainTask: danh mục loại công việc bảo dưỡng xe. MtnTkCode là khóa nghiệp vụ duy nhất;
///   bắt buộc MtnTkCode và MtnTkName (Mst_MaintainTask_Create kiểm tra 2 trường bắt buộc).
/// - Mst_MaintainTaskItem: nội dung công việc cụ thể thuộc một loại công việc bảo dưỡng.
///   Khóa nghiệp vụ = (MtnTkCode, MtnTkItemCode); FK MtnTkCode phải tồn tại trong Mst_MaintainTask;
///   bắt buộc MtnTkItemName và ViewIdx. Hỗ trợ nhập hàng loạt (Import) theo cơ chế upsert cho cả 2 bảng.
/// </summary>
public sealed class MaintainTaskService(AppDbContext db, ITenantContext tenant) : IMaintainTaskService
{
    private Guid Org => tenant.OrgId;

    // ===== Mst_MaintainTask =====
    public async Task<List<MaintainTaskDto>> SearchTasksAsync(string? keyWord, string? flagActive)
    {
        var q = db.MaintainTasks.AsNoTracking().Where(x => x.OrgId == Org);
        if (!string.IsNullOrWhiteSpace(keyWord))
        {
            var kw = keyWord.Trim();
            q = q.Where(x => x.MtnTkCode.Contains(kw) || x.MtnTkName.Contains(kw));
        }
        if (!string.IsNullOrWhiteSpace(flagActive))
        {
            var fa = flagActive.Trim();
            q = q.Where(x => x.FlagActive == fa);
        }
        var list = await q.OrderBy(x => x.MtnTkCode).ToListAsync();
        return list.Select(ToTaskDto).ToList();
    }

    public async Task<MaintainTaskDto?> GetTaskAsync(string mtnTkCode)
    {
        if (string.IsNullOrWhiteSpace(mtnTkCode)) return null;
        var code = mtnTkCode.Trim();
        var e = await db.MaintainTasks.AsNoTracking().FirstOrDefaultAsync(x => x.OrgId == Org && x.MtnTkCode == code);
        return e is null ? null : ToTaskDto(e);
    }

    public async Task<MaintainTaskDto> CreateTaskAsync(CreateMaintainTaskDto dto)
    {
        var code = (dto.MtnTkCode ?? "").Trim();
        if (code.Length < 1)
            throw new InvalidOperationException("Mã hạng mục công việc (MtnTkCode) không được để trống.");

        // MtnTkCode là khóa nghiệp vụ duy nhất (Mst_MaintainTask_CheckDB - FlagExistToCheck = No)
        if (await db.MaintainTasks.AnyAsync(x => x.OrgId == Org && x.MtnTkCode == code))
            throw new InvalidOperationException($"Hạng mục công việc bảo dưỡng '{code}' đã tồn tại.");

        var name = (dto.MtnTkName ?? "").Trim();
        if (name.Length < 1)
            throw new InvalidOperationException("Tên hạng mục công việc (MtnTkName) không được để trống.");

        var user = string.IsNullOrWhiteSpace(dto.ActionBy) ? "CHUYEN_VIEN_NPP" : dto.ActionBy.Trim();
        var entity = new MaintainTask
        {
            OrgId = Org,
            MtnTkCode = code,
            MtnTkName = name,
            MtnTkType = dto.MtnTkType?.Trim(),
            FlagActive = NormalizeFlag(dto.FlagActive),
            LogLUDateTime = DateTime.Now,
            LogLUBy = user
        };
        db.MaintainTasks.Add(entity);
        await db.SaveChangesAsync();
        return ToTaskDto(entity);
    }

    public async Task<MaintainTaskDto?> UpdateTaskAsync(string mtnTkCode, UpdateMaintainTaskDto dto)
    {
        if (string.IsNullOrWhiteSpace(mtnTkCode)) return null;
        var code = mtnTkCode.Trim();
        var entity = await db.MaintainTasks.FirstOrDefaultAsync(x => x.OrgId == Org && x.MtnTkCode == code);
        if (entity is null) return null;

        if (dto.MtnTkName is not null)
        {
            var name = dto.MtnTkName.Trim();
            if (name.Length < 1)
                throw new InvalidOperationException("Tên hạng mục công việc (MtnTkName) không được để trống.");
            entity.MtnTkName = name;
        }
        if (dto.MtnTkType is not null) entity.MtnTkType = dto.MtnTkType.Trim();

        var user = string.IsNullOrWhiteSpace(dto.ActionBy) ? "CHUYEN_VIEN_NPP" : dto.ActionBy.Trim();
        entity.FlagActive = NormalizeFlag(dto.FlagActive);
        entity.LogLUDateTime = DateTime.Now;
        entity.LogLUBy = user;

        await db.SaveChangesAsync();
        return ToTaskDto(entity);
    }

    public async Task<bool> DeleteTaskAsync(string mtnTkCode)
    {
        if (string.IsNullOrWhiteSpace(mtnTkCode)) return false;
        var code = mtnTkCode.Trim();
        var entity = await db.MaintainTasks.FirstOrDefaultAsync(x => x.OrgId == Org && x.MtnTkCode == code);
        if (entity is null) return false;

        // Chặn xóa khi còn hạng mục công việc con tham chiếu (FK Mst_MaintainTaskItem)
        var hasItems = await db.MaintainTaskItems.AnyAsync(x => x.OrgId == Org && x.MtnTkCode == code);
        if (hasItems)
            throw new InvalidOperationException($"Không thể xóa hạng mục công việc '{code}' vì đang có hạng mục công việc con tham chiếu.");

        db.MaintainTasks.Remove(entity);
        await db.SaveChangesAsync();
        return true;
    }

    public async Task<MaintainTaskImportResultDto> ImportTasksAsync(List<MaintainTaskImportRowDto> rows)
    {
        if (rows is null || rows.Count < 1)
            throw new InvalidOperationException("Danh sách nhập (Import) rỗng, không có dòng dữ liệu nào.");

        // Phase 1: kiểm tra từng dòng (field validation)
        for (int i = 0; i < rows.Count; i++)
        {
            var code = (rows[i].MtnTkCode ?? "").Trim();
            var name = (rows[i].MtnTkName ?? "").Trim();
            if (code.Length < 1)
                throw new InvalidOperationException($"Dòng {i}: Mã hạng mục công việc (MtnTkCode) không được để trống.");
            if (name.Length < 1)
                throw new InvalidOperationException($"Dòng {i}: Tên hạng mục công việc (MtnTkName) không được để trống.");
        }

        // Phase 2: kiểm tra trùng khóa MtnTkCode giữa các dòng
        var dup = rows.GroupBy(r => (r.MtnTkCode ?? "").Trim())
                      .FirstOrDefault(g => g.Count() > 1);
        if (dup is not null)
            throw new InvalidOperationException($"Mã hạng mục công việc '{dup.Key}' bị trùng lặp trong danh sách nhập.");

        var user = "CHUYEN_VIEN_NPP";
        int inserted = 0, updated = 0;
        var codes = new List<string>();

        // Phase 3: upsert (dòng đã có thì cập nhật MtnTkName+MtnTkType, dòng mới thì thêm)
        foreach (var r in rows)
        {
            var code = (r.MtnTkCode ?? "").Trim();
            var existing = await db.MaintainTasks.FirstOrDefaultAsync(x => x.OrgId == Org && x.MtnTkCode == code);
            if (existing is not null)
            {
                existing.MtnTkName = (r.MtnTkName ?? "").Trim();
                existing.MtnTkType = r.MtnTkType?.Trim();
                existing.LogLUDateTime = DateTime.Now;
                existing.LogLUBy = user;
                updated++;
            }
            else
            {
                db.MaintainTasks.Add(new MaintainTask
                {
                    OrgId = Org,
                    MtnTkCode = code,
                    MtnTkName = (r.MtnTkName ?? "").Trim(),
                    MtnTkType = r.MtnTkType?.Trim(),
                    FlagActive = "1",
                    LogLUDateTime = DateTime.Now,
                    LogLUBy = user
                });
                inserted++;
            }
            codes.Add(code);
        }

        await db.SaveChangesAsync();
        return new MaintainTaskImportResultDto(rows.Count, inserted, updated, codes);
    }

    // ===== Mst_MaintainTaskItem =====
    public async Task<List<MaintainTaskItemDto>> SearchItemsAsync(string? keyWord, string? mtnTkCode, string? flagActive)
    {
        var q = db.MaintainTaskItems.AsNoTracking().Where(x => x.OrgId == Org);
        if (!string.IsNullOrWhiteSpace(keyWord))
        {
            var kw = keyWord.Trim();
            q = q.Where(x => x.MtnTkItemCode.Contains(kw) || x.MtnTkItemName.Contains(kw));
        }
        if (!string.IsNullOrWhiteSpace(mtnTkCode))
        {
            var code = mtnTkCode.Trim();
            q = q.Where(x => x.MtnTkCode == code);
        }
        if (!string.IsNullOrWhiteSpace(flagActive))
        {
            var fa = flagActive.Trim();
            q = q.Where(x => x.FlagActive == fa);
        }
        var list = await q.OrderBy(x => x.MtnTkCode).ThenBy(x => x.ViewIdx).ThenBy(x => x.MtnTkItemCode).ToListAsync();
        var names = await GetTaskNamesAsync(list.Select(x => x.MtnTkCode).Distinct().ToList());
        return list.Select(x => ToItemDto(x, names.GetValueOrDefault(x.MtnTkCode))).ToList();
    }

    public async Task<MaintainTaskItemDto?> GetItemAsync(string mtnTkCode, string mtnTkItemCode)
    {
        if (string.IsNullOrWhiteSpace(mtnTkCode) || string.IsNullOrWhiteSpace(mtnTkItemCode)) return null;
        var code = mtnTkCode.Trim();
        var itemCode = mtnTkItemCode.Trim();
        var e = await db.MaintainTaskItems.AsNoTracking()
            .FirstOrDefaultAsync(x => x.OrgId == Org && x.MtnTkCode == code && x.MtnTkItemCode == itemCode);
        if (e is null) return null;
        return ToItemDto(e, await GetTaskNameAsync(code));
    }

    public async Task<MaintainTaskItemDto> CreateItemAsync(CreateMaintainTaskItemDto dto)
    {
        var code = (dto.MtnTkCode ?? "").Trim();
        if (code.Length < 1)
            throw new InvalidOperationException("Mã loại công việc bảo dưỡng (MtnTkCode) không được để trống.");

        // FK: loại công việc bảo dưỡng phải tồn tại (Mst_MaintainTaskItem_Create_InvalidMtnTkCode)
        await CheckTaskExistsAsync(code);

        var itemCode = (dto.MtnTkItemCode ?? "").Trim();
        if (itemCode.Length < 1)
            throw new InvalidOperationException("Mã hạng mục công việc (MtnTkItemCode) không được để trống.");

        // Khóa nghiệp vụ (MtnTkCode, MtnTkItemCode) duy nhất
        if (await db.MaintainTaskItems.AnyAsync(x => x.OrgId == Org && x.MtnTkCode == code && x.MtnTkItemCode == itemCode))
            throw new InvalidOperationException($"Hạng mục công việc '{itemCode}' của loại '{code}' đã tồn tại.");

        var name = (dto.MtnTkItemName ?? "").Trim();
        if (name.Length < 1)
            throw new InvalidOperationException("Tên/nội dung hạng mục công việc (MtnTkItemName) không được để trống.");

        var user = string.IsNullOrWhiteSpace(dto.ActionBy) ? "CHUYEN_VIEN_NPP" : dto.ActionBy.Trim();
        var entity = new MaintainTaskItem
        {
            OrgId = Org,
            MtnTkCode = code,
            MtnTkItemCode = itemCode,
            MtnTkItemName = name,
            ViewIdx = dto.ViewIdx,
            FlagActive = NormalizeFlag(dto.FlagActive),
            LogLUDateTime = DateTime.Now,
            LogLUBy = user
        };
        db.MaintainTaskItems.Add(entity);
        await db.SaveChangesAsync();
        return ToItemDto(entity, await GetTaskNameAsync(code));
    }

    public async Task<MaintainTaskItemDto?> UpdateItemAsync(string mtnTkCode, string mtnTkItemCode, UpdateMaintainTaskItemDto dto)
    {
        if (string.IsNullOrWhiteSpace(mtnTkCode) || string.IsNullOrWhiteSpace(mtnTkItemCode)) return null;
        var code = mtnTkCode.Trim();
        var itemCode = mtnTkItemCode.Trim();
        var entity = await db.MaintainTaskItems
            .FirstOrDefaultAsync(x => x.OrgId == Org && x.MtnTkCode == code && x.MtnTkItemCode == itemCode);
        if (entity is null) return null;

        if (dto.MtnTkItemName is not null)
        {
            var name = dto.MtnTkItemName.Trim();
            if (name.Length < 1)
                throw new InvalidOperationException("Tên/nội dung hạng mục công việc (MtnTkItemName) không được để trống.");
            entity.MtnTkItemName = name;
        }
        if (dto.ViewIdx is not null) entity.ViewIdx = dto.ViewIdx.Value;

        var user = string.IsNullOrWhiteSpace(dto.ActionBy) ? "CHUYEN_VIEN_NPP" : dto.ActionBy.Trim();
        entity.FlagActive = NormalizeFlag(dto.FlagActive);
        entity.LogLUDateTime = DateTime.Now;
        entity.LogLUBy = user;

        await db.SaveChangesAsync();
        return ToItemDto(entity, await GetTaskNameAsync(code));
    }

    public async Task<bool> DeleteItemAsync(string mtnTkCode, string mtnTkItemCode)
    {
        if (string.IsNullOrWhiteSpace(mtnTkCode) || string.IsNullOrWhiteSpace(mtnTkItemCode)) return false;
        var code = mtnTkCode.Trim();
        var itemCode = mtnTkItemCode.Trim();
        var entity = await db.MaintainTaskItems
            .FirstOrDefaultAsync(x => x.OrgId == Org && x.MtnTkCode == code && x.MtnTkItemCode == itemCode);
        if (entity is null) return false;

        db.MaintainTaskItems.Remove(entity);
        await db.SaveChangesAsync();
        return true;
    }

    public async Task<MaintainTaskItemImportResultDto> ImportItemsAsync(List<MaintainTaskItemImportRowDto> rows)
    {
        if (rows is null || rows.Count < 1)
            throw new InvalidOperationException("Danh sách nhập (Import) rỗng, không có dòng dữ liệu nào.");

        // Phase 1: kiểm tra từng dòng (field validation)
        for (int i = 0; i < rows.Count; i++)
        {
            var code = (rows[i].MtnTkCode ?? "").Trim();
            var itemCode = (rows[i].MtnTkItemCode ?? "").Trim();
            var name = (rows[i].MtnTkItemName ?? "").Trim();
            if (code.Length < 1)
                throw new InvalidOperationException($"Dòng {i}: Mã loại công việc bảo dưỡng (MtnTkCode) không được để trống.");
            if (itemCode.Length < 1)
                throw new InvalidOperationException($"Dòng {i}: Mã hạng mục công việc (MtnTkItemCode) không được để trống.");
            if (name.Length < 1)
                throw new InvalidOperationException($"Dòng {i}: Tên/nội dung hạng mục công việc (MtnTkItemName) không được để trống.");
        }

        // Phase 2: kiểm tra trùng khóa (MtnTkCode + MtnTkItemCode) giữa các dòng
        var dup = rows.GroupBy(r => ((r.MtnTkCode ?? "").Trim(), (r.MtnTkItemCode ?? "").Trim()))
                      .FirstOrDefault(g => g.Count() > 1);
        if (dup is not null)
            throw new InvalidOperationException($"Cặp khóa '{dup.Key.Item1} + {dup.Key.Item2}' bị trùng lặp trong danh sách nhập.");

        var user = "CHUYEN_VIEN_NPP";
        int inserted = 0, updated = 0;
        var keys = new List<string>();

        // Phase 3: upsert (dòng đã có thì cập nhật MtnTkItemName+ViewIdx, dòng mới thì thêm)
        foreach (var r in rows)
        {
            var code = (r.MtnTkCode ?? "").Trim();
            var itemCode = (r.MtnTkItemCode ?? "").Trim();
            var existing = await db.MaintainTaskItems
                .FirstOrDefaultAsync(x => x.OrgId == Org && x.MtnTkCode == code && x.MtnTkItemCode == itemCode);
            if (existing is not null)
            {
                existing.MtnTkItemName = (r.MtnTkItemName ?? "").Trim();
                existing.ViewIdx = r.ViewIdx ?? 0;
                existing.LogLUDateTime = DateTime.Now;
                existing.LogLUBy = user;
                updated++;
            }
            else
            {
                // FK: loại công việc bảo dưỡng phải tồn tại (nhánh Insert)
                await CheckTaskExistsAsync(code);
                db.MaintainTaskItems.Add(new MaintainTaskItem
                {
                    OrgId = Org,
                    MtnTkCode = code,
                    MtnTkItemCode = itemCode,
                    MtnTkItemName = (r.MtnTkItemName ?? "").Trim(),
                    ViewIdx = r.ViewIdx ?? 0,
                    FlagActive = "1",
                    LogLUDateTime = DateTime.Now,
                    LogLUBy = user
                });
                inserted++;
            }
            keys.Add($"{code}+{itemCode}");
        }

        await db.SaveChangesAsync();
        return new MaintainTaskItemImportResultDto(rows.Count, inserted, updated, keys);
    }

    public async Task<MaintainTaskStatsDto> GetStatsAsync()
    {
        var tasks = await db.MaintainTasks.AsNoTracking().Where(x => x.OrgId == Org).ToListAsync();
        var items = await db.MaintainTaskItems.AsNoTracking().Where(x => x.OrgId == Org).ToListAsync();

        return new MaintainTaskStatsDto(
            TotalTasks: tasks.Count,
            TotalActiveTasks: tasks.Count(x => x.FlagActive == "1"),
            TotalItems: items.Count,
            TotalActiveItems: items.Count(x => x.FlagActive == "1"),
            ItemsByTask: items.Where(x => x.FlagActive == "1")
                              .GroupBy(x => string.IsNullOrWhiteSpace(x.MtnTkCode) ? "UNKNOWN" : x.MtnTkCode)
                              .ToDictionary(g => g.Key, g => g.Count())
        );
    }

    // ===== Helpers =====
    private static string NormalizeFlag(string? flag)
        => string.IsNullOrWhiteSpace(flag) ? "1" : (flag.Trim() == "0" ? "0" : "1");

    /// <summary>Kiểm tra loại công việc bảo dưỡng tồn tại (Mst_MaintainTaskItem_Create_InvalidMtnTkCode).</summary>
    private async Task CheckTaskExistsAsync(string mtnTkCode)
    {
        var exists = await db.MaintainTasks.AsNoTracking()
            .Where(x => x.OrgId == Org && x.MtnTkCode == mtnTkCode)
            .Select(x => x.MtnTkCode)
            .FirstOrDefaultAsync();
        if (exists is null)
            throw new InvalidOperationException($"Loại công việc bảo dưỡng '{mtnTkCode}' không tồn tại trong danh mục (Mst_MaintainTask).");
    }

    private async Task<string?> GetTaskNameAsync(string mtnTkCode)
    {
        var name = await db.MaintainTasks.AsNoTracking()
            .Where(x => x.OrgId == Org && x.MtnTkCode == mtnTkCode)
            .Select(x => x.MtnTkName)
            .FirstOrDefaultAsync();
        return string.IsNullOrWhiteSpace(name) ? null : name;
    }

    private async Task<Dictionary<string, string?>> GetTaskNamesAsync(List<string> codes)
    {
        if (codes.Count < 1) return new Dictionary<string, string?>();
        var rows = await db.MaintainTasks.AsNoTracking()
            .Where(x => x.OrgId == Org && codes.Contains(x.MtnTkCode))
            .Select(x => new { x.MtnTkCode, x.MtnTkName })
            .ToListAsync();
        return rows.GroupBy(x => x.MtnTkCode)
                   .ToDictionary(g => g.Key, g => (string?)g.First().MtnTkName);
    }

    private static MaintainTaskDto ToTaskDto(MaintainTask e) => new(
        e.Id, e.MtnTkCode, e.MtnTkName, e.MtnTkType, e.FlagActive, e.FlagActive == "1", e.LogLUDateTime, e.LogLUBy);

    private static MaintainTaskItemDto ToItemDto(MaintainTaskItem e, string? taskName) => new(
        e.Id, e.MtnTkCode, taskName, e.MtnTkItemCode, e.MtnTkItemName, e.ViewIdx,
        e.FlagActive, e.FlagActive == "1", e.LogLUDateTime, e.LogLUBy);
}