using Microsoft.EntityFrameworkCore;
using MiniSales.Data;
using MiniSales.Models;

namespace MiniSales.Services;

// ===== DTOs =====
public record UpdateDealerInventoryThresholdDto(
    decimal Qty = 0,
    string? FlagActive = null,
    string? ActionBy = null
);

public record DealerInventoryThresholdImportRowDto(
    string? DealerCode,
    string? ModelCode,
    decimal? Qty
);

public record DealerInventoryThresholdDto(
    long Id,
    string DealerCode,
    string? DealerName,
    string ModelCode,
    string? ModelName,
    decimal Qty,
    string FlagActive,
    bool IsActive,
    DateTime LogLUDateTime,
    string? LogLUBy
);

public record DealerInventoryThresholdImportResultDto(
    int TotalRows,
    int Inserted,
    int Reactivated,
    List<string> Keys
);

public record DealerInventoryThresholdStatsDto(
    int TotalThresholds,
    int TotalActive,
    int TotalInactive,
    int TotalDealers,
    int TotalModels,
    decimal TotalQty,
    decimal AvgQty,
    decimal MaxQty,
    Dictionary<string, decimal> QtyByDealer
);

public interface IDealerInventoryThresholdService
{
    Task<List<DealerInventoryThresholdDto>> SearchAsync(string? keyWord, string? dealerCode, string? modelCode, string? flagActive);
    Task<DealerInventoryThresholdDto?> GetAsync(string dealerCode, string modelCode);
    Task<DealerInventoryThresholdDto?> UpdateAsync(string dealerCode, string modelCode, UpdateDealerInventoryThresholdDto dto);
    Task<bool> DeleteAsync(string dealerCode, string modelCode);
    Task<DealerInventoryThresholdImportResultDto> ImportAsync(List<DealerInventoryThresholdImportRowDto> rows);
    Task<DealerInventoryThresholdStatsDto> GetStatsAsync();
}

/// <summary>
/// Quản lý Ngưỡng tồn kho đại lý theo model (DMS.Sales Mst_DealerInventoryThreshold / Master.cs /
/// Mst_DealerInventoryThreshold_Get|Update|Import|Delete): danh mục quy định số lượng tồn kho tối thiểu (Qty)
/// mà mỗi Đại lý cần duy trì cho từng dòng xe (ModelCode). Khóa nghiệp vụ = (DealerCode, ModelCode).
/// Ràng buộc: DealerCode và ModelCode bắt buộc; Qty phải là số >= 0; FlagActive chỉ nhận '1'/'0';
/// FK DealerCode phải tồn tại và đang hoạt động (Mst_Dealer); FK ModelCode phải tồn tại và đang hoạt động
/// (Mst_CarModel). Update chỉ cho sửa Qty + FlagActive (Master Fixed Updateable Scope). Hỗ trợ nhập hàng loạt
/// (Import) theo cơ chế upsert: mapping đã tồn tại + active → lỗi; inactive → reactivate + cập nhật Qty;
/// chưa có → insert mới.
/// </summary>
public sealed class DealerInventoryThresholdService(AppDbContext db, ITenantContext tenant) : IDealerInventoryThresholdService
{
    private Guid Org => tenant.OrgId;

    public async Task<List<DealerInventoryThresholdDto>> SearchAsync(string? keyWord, string? dealerCode, string? modelCode, string? flagActive)
    {
        var q = db.DealerInventoryThresholds.AsNoTracking().Where(x => x.OrgId == Org);

        if (!string.IsNullOrWhiteSpace(keyWord))
        {
            var kw = keyWord.Trim();
            q = q.Where(x => x.DealerCode.Contains(kw) || x.ModelCode.Contains(kw));
        }
        if (!string.IsNullOrWhiteSpace(dealerCode))
        {
            var dc = dealerCode.Trim();
            q = q.Where(x => x.DealerCode == dc);
        }
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

        var list = await q.OrderBy(x => x.DealerCode).ThenBy(x => x.ModelCode).ToListAsync();
        return list.Select(ToDto).ToList();
    }

    public async Task<DealerInventoryThresholdDto?> GetAsync(string dealerCode, string modelCode)
    {
        if (string.IsNullOrWhiteSpace(dealerCode) || string.IsNullOrWhiteSpace(modelCode)) return null;
        var dc = dealerCode.Trim();
        var mc = modelCode.Trim();
        var e = await db.DealerInventoryThresholds.AsNoTracking()
            .FirstOrDefaultAsync(x => x.OrgId == Org && x.DealerCode == dc && x.ModelCode == mc);
        return e is null ? null : ToDto(e);
    }

    public async Task<DealerInventoryThresholdDto?> UpdateAsync(string dealerCode, string modelCode, UpdateDealerInventoryThresholdDto dto)
    {
        if (string.IsNullOrWhiteSpace(dealerCode) || string.IsNullOrWhiteSpace(modelCode)) return null;
        var dc = dealerCode.Trim();
        var mc = modelCode.Trim();
        var entity = await db.DealerInventoryThresholds
            .FirstOrDefaultAsync(x => x.OrgId == Org && x.DealerCode == dc && x.ModelCode == mc);
        if (entity is null) return null;

        ValidateQty(dto.Qty);

        // FlagActive chỉ nhận '1'/'0' (Mst_DealerInventoryThreshold_Update_InvalidFlagActive)
        var flagActive = (dto.FlagActive ?? "").Trim();
        if (flagActive != "1" && flagActive != "0")
            throw new InvalidOperationException("Cờ hiệu lực (FlagActive) chỉ nhận giá trị '1' (đang áp dụng) hoặc '0' (ngừng áp dụng).");

        // Master Fixed Updateable Scope: chỉ update Qty + FlagActive (bám WF — WF sửa Qty/FlagActive).
        var user = string.IsNullOrWhiteSpace(dto.ActionBy) ? "CHUYEN_VIEN_NPP" : dto.ActionBy.Trim();
        entity.Qty = dto.Qty;
        entity.FlagActive = flagActive;
        entity.LogLUDateTime = DateTime.Now;
        entity.LogLUBy = user;

        await db.SaveChangesAsync();
        return ToDto(entity);
    }

    public async Task<bool> DeleteAsync(string dealerCode, string modelCode)
    {
        if (string.IsNullOrWhiteSpace(dealerCode) || string.IsNullOrWhiteSpace(modelCode)) return false;
        var dc = dealerCode.Trim();
        var mc = modelCode.Trim();
        var entity = await db.DealerInventoryThresholds
            .FirstOrDefaultAsync(x => x.OrgId == Org && x.DealerCode == dc && x.ModelCode == mc);
        if (entity is null) return false;

        db.DealerInventoryThresholds.Remove(entity);
        await db.SaveChangesAsync();
        return true;
    }

    public async Task<DealerInventoryThresholdImportResultDto> ImportAsync(List<DealerInventoryThresholdImportRowDto> rows)
    {
        if (rows is null || rows.Count < 1)
            throw new InvalidOperationException("Danh sách nhập (Import) rỗng, không có dòng dữ liệu nào.");

        // Phase 1: kiểm tra từng dòng (field validation, fail-fast giống Update)
        for (int i = 0; i < rows.Count; i++)
        {
            var dealerCode = (rows[i].DealerCode ?? "").Trim();
            var modelCode = (rows[i].ModelCode ?? "").Trim();
            if (dealerCode.Length < 1)
                throw new InvalidOperationException($"Dòng {i}: Mã đại lý (DealerCode) không được để trống.");
            if (modelCode.Length < 1)
                throw new InvalidOperationException($"Dòng {i}: Mã dòng xe (ModelCode) không được để trống.");
            if (rows[i].Qty is null || rows[i].Qty < 0)
                throw new InvalidOperationException($"Dòng {i}: Ngưỡng tồn kho (Qty) phải là số >= 0.");
        }

        // Phase 2: kiểm tra trùng khóa (DealerCode + ModelCode) giữa các dòng
        var dup = rows.GroupBy(r => ((r.DealerCode ?? "").Trim(), (r.ModelCode ?? "").Trim()))
                      .FirstOrDefault(g => g.Count() > 1);
        if (dup is not null)
            throw new InvalidOperationException($"Cặp (DealerCode='{dup.Key.Item1}', ModelCode='{dup.Key.Item2}') bị trùng lặp trong danh sách nhập.");

        var user = "CHUYEN_VIEN_NPP";
        int inserted = 0, reactivated = 0;
        var keys = new List<string>();

        // Phase 3: DB check per row + phân luồng (reactivate/update existing inactive, insert mới)
        foreach (var r in rows)
        {
            var dealerCode = (r.DealerCode ?? "").Trim();
            var modelCode = (r.ModelCode ?? "").Trim();
            var qty = r.Qty ?? 0m;

            // FK: đại lý phải tồn tại + active (Mst_Dealer_CheckDB)
            await CheckDealerExistsAsync(dealerCode);
            // FK: model phải tồn tại + active (Mst_CarModel_CheckDB)
            await CheckModelExistsAsync(modelCode);

            var existing = await db.DealerInventoryThresholds
                .FirstOrDefaultAsync(x => x.OrgId == Org && x.DealerCode == dealerCode && x.ModelCode == modelCode);

            if (existing is not null)
            {
                // ĐÃ TỒN TẠI + đang active → lỗi (đã thiết lập rồi)
                if (existing.FlagActive == "1")
                    throw new InvalidOperationException($"Ngưỡng tồn kho cho đại lý '{dealerCode}' và model '{modelCode}' đã tồn tại và đang áp dụng.");

                // ĐÃ TỒN TẠI nhưng inactive → reactivate + cập nhật Qty
                existing.Qty = qty;
                existing.FlagActive = "1";
                existing.LogLUDateTime = DateTime.Now;
                existing.LogLUBy = user;
                reactivated++;
            }
            else
            {
                // CHƯA TỒN TẠI → INSERT
                db.DealerInventoryThresholds.Add(new DealerInventoryThreshold
                {
                    OrgId = Org,
                    DealerCode = dealerCode,
                    DealerName = await GetDealerNameAsync(dealerCode),
                    ModelCode = modelCode,
                    ModelName = await GetModelNameAsync(modelCode),
                    Qty = qty,
                    FlagActive = "1",
                    LogLUDateTime = DateTime.Now,
                    LogLUBy = user
                });
                inserted++;
            }
            keys.Add($"{dealerCode}|{modelCode}");
        }

        await db.SaveChangesAsync();
        return new DealerInventoryThresholdImportResultDto(rows.Count, inserted, reactivated, keys);
    }

    public async Task<DealerInventoryThresholdStatsDto> GetStatsAsync()
    {
        var list = await db.DealerInventoryThresholds.AsNoTracking().Where(x => x.OrgId == Org).ToListAsync();
        return new DealerInventoryThresholdStatsDto(
            TotalThresholds: list.Count,
            TotalActive: list.Count(x => x.FlagActive == "1"),
            TotalInactive: list.Count(x => x.FlagActive != "1"),
            TotalDealers: list.Select(x => x.DealerCode).Distinct().Count(),
            TotalModels: list.Select(x => x.ModelCode).Distinct().Count(),
            TotalQty: list.Sum(x => x.Qty),
            AvgQty: list.Count > 0 ? Math.Round(list.Average(x => x.Qty), 2) : 0m,
            MaxQty: list.Count > 0 ? list.Max(x => x.Qty) : 0m,
            QtyByDealer: list.GroupBy(x => string.IsNullOrWhiteSpace(x.DealerCode) ? "UNKNOWN" : x.DealerCode)
                             .ToDictionary(g => g.Key, g => g.Sum(x => x.Qty))
        );
    }

    // ===== Helpers =====
    private static void ValidateQty(decimal qty)
    {
        if (qty < 0)
            throw new InvalidOperationException("Ngưỡng tồn kho (Qty) phải là số >= 0.");
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

    /// <summary>Kiểm tra dòng xe tồn tại và đang hoạt động (Mst_CarModel_CheckDB).</summary>
    private async Task CheckModelExistsAsync(string modelCode)
    {
        var model = await db.CarModels.AsNoTracking()
            .Where(m => m.OrgId == Org && m.ModelCode == modelCode && m.FlagActive == "1")
            .Select(m => m.ModelCode)
            .FirstOrDefaultAsync();
        if (model is null)
            throw new InvalidOperationException($"Dòng xe '{modelCode}' không tồn tại hoặc đã ngừng hoạt động trong danh mục dòng xe (Mst_CarModel).");
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

    /// <summary>Tra cứu tên dòng xe từ danh mục dòng xe (Mst_CarModel.ModelName).</summary>
    private async Task<string?> GetModelNameAsync(string modelCode)
    {
        var name = await db.CarModels.AsNoTracking()
            .Where(m => m.OrgId == Org && m.ModelCode == modelCode)
            .Select(m => m.ModelName)
            .FirstOrDefaultAsync();
        return string.IsNullOrWhiteSpace(name) ? null : name;
    }

    private static DealerInventoryThresholdDto ToDto(DealerInventoryThreshold e) => new(
        e.Id, e.DealerCode, e.DealerName, e.ModelCode, e.ModelName,
        e.Qty, e.FlagActive, e.FlagActive == "1", e.LogLUDateTime, e.LogLUBy);
}