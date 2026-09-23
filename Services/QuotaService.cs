using Microsoft.EntityFrameworkCore;
using MiniSales.Data;
using MiniSales.Models;

namespace MiniSales.Services;

// ===== DTOs =====
public record CreateQuotaDto(
    string DealerCode = "",
    string SpecCode = "",
    decimal QtyQuota = 0,
    string? ActionBy = null
);

public record UpdateQuotaDto(
    decimal QtyQuota = 0,
    string? ActionBy = null
);

public record QuotaImportRowDto(
    string? DealerCode,
    string? SpecCode,
    decimal? QtyQuota
);

public record QuotaDto(
    long Id,
    string DealerCode,
    string? DealerName,
    string SpecCode,
    string? SpecDescription,
    string? ModelCode,
    string? ModelName,
    decimal QtyQuota,
    string FlagActive,
    bool IsActive,
    string? UpdateBy,
    DateTime? UpdateDTime,
    DateTime LogLUDateTime,
    string? LogLUBy
);

public record QuotaImportResultDto(
    int TotalRows,
    int Inserted,
    int Updated,
    List<string> Keys
);

public record QuotaStatsDto(
    int TotalQuotas,
    int TotalActive,
    int TotalInactive,
    int TotalDealers,
    int TotalSpecs,
    decimal TotalQtyQuota,
    decimal AvgQtyQuota,
    decimal MaxQtyQuota,
    Dictionary<string, decimal> QtyByDealer
);

public interface IQuotaService
{
    Task<List<QuotaDto>> SearchAsync(string? keyWord, string? dealerCode, string? specCode, string? flagActive);
    Task<QuotaDto?> GetAsync(string dealerCode, string specCode);
    Task<QuotaDto> CreateAsync(CreateQuotaDto dto);
    Task<QuotaDto?> UpdateAsync(string dealerCode, string specCode, UpdateQuotaDto dto);
    Task<bool> DeleteAsync(string dealerCode, string specCode);
    Task<QuotaImportResultDto> ImportAsync(List<QuotaImportRowDto> rows);
    Task<QuotaStatsDto> GetStatsAsync();
}

/// <summary>
/// Quản lý Hạn mức đặt hàng xe theo Đại lý & Quy cách (DMS.Sales Mng_Quota / Master.cs /
/// Mng_Quota_Get|Create|Update|Delete|Import): danh mục quy định số lượng xe tối đa (QtyQuota) mà mỗi
/// Đại lý được phép đặt cho từng quy cách xe (SpecCode). Khóa nghiệp vụ = (DealerCode, SpecCode).
/// Ràng buộc: DealerCode và SpecCode bắt buộc; QtyQuota phải là số >= 0; FK DealerCode phải tồn tại và
/// đang hoạt động (Mst_Dealer). Update chỉ cho sửa QtyQuota (Master Fixed Updateable Scope). Hỗ trợ nhập
/// hàng loạt (Import) theo cơ chế upsert: dòng đã có thì cập nhật QtyQuota, dòng mới thì thêm.
/// </summary>
public sealed class QuotaService(AppDbContext db, ITenantContext tenant) : IQuotaService
{
    private Guid Org => tenant.OrgId;

    public async Task<List<QuotaDto>> SearchAsync(string? keyWord, string? dealerCode, string? specCode, string? flagActive)
    {
        var q = db.Quotas.AsNoTracking().Where(x => x.OrgId == Org);

        if (!string.IsNullOrWhiteSpace(keyWord))
        {
            var kw = keyWord.Trim();
            q = q.Where(x => x.DealerCode.Contains(kw) || x.SpecCode.Contains(kw));
        }
        if (!string.IsNullOrWhiteSpace(dealerCode))
        {
            var dc = dealerCode.Trim();
            q = q.Where(x => x.DealerCode == dc);
        }
        if (!string.IsNullOrWhiteSpace(specCode))
        {
            var sc = specCode.Trim();
            q = q.Where(x => x.SpecCode == sc);
        }
        if (!string.IsNullOrWhiteSpace(flagActive))
        {
            var fa = flagActive.Trim();
            q = q.Where(x => x.FlagActive == fa);
        }

        var list = await q.OrderBy(x => x.DealerCode).ThenBy(x => x.SpecCode).ToListAsync();
        return list.Select(ToDto).ToList();
    }

    public async Task<QuotaDto?> GetAsync(string dealerCode, string specCode)
    {
        if (string.IsNullOrWhiteSpace(dealerCode) || string.IsNullOrWhiteSpace(specCode)) return null;
        var dc = dealerCode.Trim();
        var sc = specCode.Trim();
        var e = await db.Quotas.AsNoTracking()
            .FirstOrDefaultAsync(x => x.OrgId == Org && x.DealerCode == dc && x.SpecCode == sc);
        return e is null ? null : ToDto(e);
    }

    public async Task<QuotaDto> CreateAsync(CreateQuotaDto dto)
    {
        var dealerCode = (dto.DealerCode ?? "").Trim();
        var specCode = (dto.SpecCode ?? "").Trim();
        if (dealerCode.Length < 1)
            throw new InvalidOperationException("Mã đại lý (DealerCode) không được để trống.");
        if (specCode.Length < 1)
            throw new InvalidOperationException("Mã quy cách xe (SpecCode) không được để trống.");

        // Khóa nghiệp vụ (DealerCode, SpecCode) là duy nhất (Mng_Quota_CheckDB - FlagExistToCheck = No)
        if (await db.Quotas.AnyAsync(x => x.OrgId == Org && x.DealerCode == dealerCode && x.SpecCode == specCode))
            throw new InvalidOperationException($"Hạn mức đặt hàng cho đại lý '{dealerCode}' và quy cách '{specCode}' đã tồn tại.");

        // FK: Đại lý phải tồn tại và đang hoạt động (Mst_Dealer_CheckDB - FlagExistToCheck = Yes, FlagActive = Active)
        await CheckDealerExistsAsync(dealerCode);

        ValidateQtyQuota(dto.QtyQuota);

        var user = string.IsNullOrWhiteSpace(dto.ActionBy) ? "CHUYEN_VIEN_NPP" : dto.ActionBy.Trim();
        var spec = await GetSpecInfoAsync(specCode);
        var entity = new QuotaMaster
        {
            OrgId = Org,
            DealerCode = dealerCode,
            DealerName = await GetDealerNameAsync(dealerCode),
            SpecCode = specCode,
            SpecDescription = spec.Description,
            ModelCode = spec.ModelCode,
            ModelName = spec.ModelName,
            QtyQuota = dto.QtyQuota,
            FlagActive = "1",
            LogLUDateTime = DateTime.Now,
            LogLUBy = user
        };
        db.Quotas.Add(entity);
        await db.SaveChangesAsync();
        return ToDto(entity);
    }

    public async Task<QuotaDto?> UpdateAsync(string dealerCode, string specCode, UpdateQuotaDto dto)
    {
        if (string.IsNullOrWhiteSpace(dealerCode) || string.IsNullOrWhiteSpace(specCode)) return null;
        var dc = dealerCode.Trim();
        var sc = specCode.Trim();
        var entity = await db.Quotas
            .FirstOrDefaultAsync(x => x.OrgId == Org && x.DealerCode == dc && x.SpecCode == sc);
        if (entity is null) return null;

        ValidateQtyQuota(dto.QtyQuota);

        // Master Fixed Updateable Scope: chỉ update QtyQuota (bám WF — WF sửa QtyQuota).
        var user = string.IsNullOrWhiteSpace(dto.ActionBy) ? "CHUYEN_VIEN_NPP" : dto.ActionBy.Trim();
        entity.QtyQuota = dto.QtyQuota;
        entity.UpdateBy = user;
        entity.UpdateDTime = DateTime.Now;
        entity.LogLUDateTime = DateTime.Now;
        entity.LogLUBy = user;

        await db.SaveChangesAsync();
        return ToDto(entity);
    }

    public async Task<bool> DeleteAsync(string dealerCode, string specCode)
    {
        if (string.IsNullOrWhiteSpace(dealerCode) || string.IsNullOrWhiteSpace(specCode)) return false;
        var dc = dealerCode.Trim();
        var sc = specCode.Trim();
        var entity = await db.Quotas
            .FirstOrDefaultAsync(x => x.OrgId == Org && x.DealerCode == dc && x.SpecCode == sc);
        if (entity is null) return false;

        db.Quotas.Remove(entity);
        await db.SaveChangesAsync();
        return true;
    }

    public async Task<QuotaImportResultDto> ImportAsync(List<QuotaImportRowDto> rows)
    {
        if (rows is null || rows.Count < 1)
            throw new InvalidOperationException("Danh sách nhập (Import) rỗng, không có dòng dữ liệu nào.");

        // Phase 1: kiểm tra từng dòng (field validation, fail-fast giống Create)
        for (int i = 0; i < rows.Count; i++)
        {
            var dealerCode = (rows[i].DealerCode ?? "").Trim();
            var specCode = (rows[i].SpecCode ?? "").Trim();
            if (dealerCode.Length < 1)
                throw new InvalidOperationException($"Dòng {i}: Mã đại lý (DealerCode) không được để trống.");
            if (specCode.Length < 1)
                throw new InvalidOperationException($"Dòng {i}: Mã quy cách xe (SpecCode) không được để trống.");
            if (rows[i].QtyQuota is null || rows[i].QtyQuota < 0)
                throw new InvalidOperationException($"Dòng {i}: Số lượng hạn mức (QtyQuota) phải là số >= 0.");
        }

        // Phase 2: kiểm tra trùng khóa (DealerCode + SpecCode) giữa các dòng
        var dup = rows.GroupBy(r => ((r.DealerCode ?? "").Trim(), (r.SpecCode ?? "").Trim()))
                      .FirstOrDefault(g => g.Count() > 1);
        if (dup is not null)
            throw new InvalidOperationException($"Cặp (DealerCode='{dup.Key.Item1}', SpecCode='{dup.Key.Item2}') bị trùng lặp trong danh sách nhập.");

        var user = "CHUYEN_VIEN_NPP";
        int inserted = 0, updated = 0;
        var keys = new List<string>();

        // Phase 3: DB check per row (không ép tồn tại/chưa tồn tại vì đây là luồng Upsert) + phân luồng
        foreach (var r in rows)
        {
            var dealerCode = (r.DealerCode ?? "").Trim();
            var specCode = (r.SpecCode ?? "").Trim();
            var qty = r.QtyQuota ?? 0m;

            var existing = await db.Quotas
                .FirstOrDefaultAsync(x => x.OrgId == Org && x.DealerCode == dealerCode && x.SpecCode == specCode);

            if (existing is not null)
            {
                // ĐÃ TỒN TẠI -> UPDATE (mẫu Mng_Quota_Update: chỉ QtyQuota + UpdateBy/UpdateDTime)
                existing.QtyQuota = qty;
                existing.UpdateBy = user;
                existing.UpdateDTime = DateTime.Now;
                existing.LogLUDateTime = DateTime.Now;
                existing.LogLUBy = user;
                updated++;
            }
            else
            {
                // CHƯA TỒN TẠI -> FK check giống Create (Mst_Dealer phải tồn tại + active), rồi INSERT
                await CheckDealerExistsAsync(dealerCode);
                var spec = await GetSpecInfoAsync(specCode);
                db.Quotas.Add(new QuotaMaster
                {
                    OrgId = Org,
                    DealerCode = dealerCode,
                    DealerName = await GetDealerNameAsync(dealerCode),
                    SpecCode = specCode,
                    SpecDescription = spec.Description,
                    ModelCode = spec.ModelCode,
                    ModelName = spec.ModelName,
                    QtyQuota = qty,
                    FlagActive = "1",
                    LogLUDateTime = DateTime.Now,
                    LogLUBy = user
                });
                inserted++;
            }
            keys.Add($"{dealerCode}|{specCode}");
        }

        await db.SaveChangesAsync();
        return new QuotaImportResultDto(rows.Count, inserted, updated, keys);
    }

    public async Task<QuotaStatsDto> GetStatsAsync()
    {
        var list = await db.Quotas.AsNoTracking().Where(x => x.OrgId == Org).ToListAsync();
        return new QuotaStatsDto(
            TotalQuotas: list.Count,
            TotalActive: list.Count(x => x.FlagActive == "1"),
            TotalInactive: list.Count(x => x.FlagActive != "1"),
            TotalDealers: list.Select(x => x.DealerCode).Distinct().Count(),
            TotalSpecs: list.Select(x => x.SpecCode).Distinct().Count(),
            TotalQtyQuota: list.Sum(x => x.QtyQuota),
            AvgQtyQuota: list.Count > 0 ? Math.Round(list.Average(x => x.QtyQuota), 2) : 0m,
            MaxQtyQuota: list.Count > 0 ? list.Max(x => x.QtyQuota) : 0m,
            QtyByDealer: list.GroupBy(x => string.IsNullOrWhiteSpace(x.DealerCode) ? "UNKNOWN" : x.DealerCode)
                             .ToDictionary(g => g.Key, g => g.Sum(x => x.QtyQuota))
        );
    }

    // ===== Helpers =====
    private static void ValidateQtyQuota(decimal qtyQuota)
    {
        if (qtyQuota < 0)
            throw new InvalidOperationException("Số lượng hạn mức (QtyQuota) phải là số >= 0.");
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

    /// <summary>Tra cứu thông tin quy cách xe (Mst_CarSpec.SpecDescription + Mst_CarModel.ModelCode/ModelName) từ dữ liệu xe đã có.</summary>
    private async Task<(string? Description, string? ModelCode, string? ModelName)> GetSpecInfoAsync(string specCode)
    {
        var car = await db.Cars.AsNoTracking()
            .Where(c => c.OrgId == Org && c.SpecCode == specCode)
            .Select(c => new { c.SpecDescription, c.ModelCode, c.ModelName })
            .FirstOrDefaultAsync();
        return car is null ? (null, null, null) : (car.SpecDescription, car.ModelCode, car.ModelName);
    }

    private static QuotaDto ToDto(QuotaMaster e) => new(
        e.Id, e.DealerCode, e.DealerName, e.SpecCode, e.SpecDescription, e.ModelCode, e.ModelName,
        e.QtyQuota, e.FlagActive, e.FlagActive == "1", e.UpdateBy, e.UpdateDTime, e.LogLUDateTime, e.LogLUBy);
}