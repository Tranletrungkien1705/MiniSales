using Microsoft.EntityFrameworkCore;
using MiniSales.Data;
using MiniSales.Models;

namespace MiniSales.Services;

// ===== DTOs =====
public record StorageTransactionRowDto(
    string? Vin,
    string? RefNo,
    string? RefType,
    string? StorageCode,
    string? StorageCodeTo,
    DateTime? DTimeFrom,
    DateTime? DTimeTo,
    string? RefNoTo,
    string? Remark
);

public record StorageTransactionDto(
    long Id,
    string Vin,
    string RefNo,
    string RefType,
    string StorageCode,
    string? StorageCodeTo,
    DateTime DTimeFrom,
    DateTime? DTimeTo,
    string? RefNoTo,
    string FlagInDay,
    bool IsInDay,
    string? Remark,
    string? CreatedBy,
    DateTime CreatedDTime,
    string? LogLUBy,
    DateTime LogLUDateTime
);

public record StorageTransactionCreateResultDto(
    int TotalRows,
    int Inserted,
    List<string> Keys
);

public record StorageTransactionStatsDto(
    int TotalTransactions,
    int TotalVins,
    int TotalInDay,
    int TotalOut,
    int TotalInStorage,
    Dictionary<string, int> ByRefType,
    Dictionary<string, int> ByStorage
);

public interface IStorageTransactionService
{
    Task<List<StorageTransactionDto>> SearchAsync(
        string? vin, string? refNo, string? refType, string? storageCode, string? storageCodeTo,
        string? refNoTo, DateTime? createdFrom, DateTime? createdTo,
        DateTime? dTimeFrom, DateTime? dTimeFromTo, DateTime? dTimeToFrom, DateTime? dTimeTo);
    Task<StorageTransactionDto?> GetAsync(string vin, string refNo);
    Task<StorageTransactionCreateResultDto> CreateAsync(List<StorageTransactionRowDto> rows, string? actor);
    Task<StorageTransactionStatsDto> GetStatsAsync();
}

/// <summary>
/// Lịch sử giao dịch kho xe (DMS.Sales Sto_StorageTransaction / StoStorageTransactionController / Storage.cs /
/// Sto_StorageTransaction_AddX|_Check|_Get_HQ): lưu vết xe nằm ở kho nào, từ ngày nào tới ngày nào (nhập/xuất kho),
/// phục vụ dựng lại lịch sử lưu kho và tính phí lưu kho.
/// - Khóa nghiệp vụ = (VIN, RefNo). Cấm ghi lại cặp (VIN, RefNo) đã tồn tại (Sto_StorageTransaction_Check, FlagExist = Inactive).
/// - Bắt buộc: VIN, RefNo, RefType, StorageCode, DTimeFrom. StorageCodeTo + DTimeTo KHÔNG bắt buộc (nguồn đã comment guard).
/// - Cấm trùng VIN trong cùng một lô (Sto_StorageTransaction_AddX_DuplicateVIN).
/// - Cờ FlagInDay: nếu xe đã có giao dịch xuất kho (StorageCodeTo + DTimeTo) cùng ngày nhập của kho nhập thì đánh dấu
///   nhập-xuất trong ngày ('1') — khi đó chỉ tính phí lưu kho cho kho xuất.
/// </summary>
public sealed class StorageTransactionService(AppDbContext db, ITenantContext tenant) : IStorageTransactionService
{
    private Guid Org => tenant.OrgId;

    public async Task<List<StorageTransactionDto>> SearchAsync(
        string? vin, string? refNo, string? refType, string? storageCode, string? storageCodeTo,
        string? refNoTo, DateTime? createdFrom, DateTime? createdTo,
        DateTime? dTimeFrom, DateTime? dTimeFromTo, DateTime? dTimeToFrom, DateTime? dTimeTo)
    {
        var q = db.StorageTransactions.AsNoTracking().Where(x => x.OrgId == Org);

        if (!string.IsNullOrWhiteSpace(vin)) { var v = vin.Trim(); q = q.Where(x => x.Vin == v); }
        if (!string.IsNullOrWhiteSpace(refNo)) { var r = refNo.Trim(); q = q.Where(x => x.RefNo == r); }
        if (!string.IsNullOrWhiteSpace(refType) && Enum.TryParse<StorageTransactionRefType>(refType, true, out var rt))
            q = q.Where(x => x.RefType == rt);
        if (!string.IsNullOrWhiteSpace(storageCode)) { var s = storageCode.Trim(); q = q.Where(x => x.StorageCode == s); }
        if (!string.IsNullOrWhiteSpace(storageCodeTo)) { var s = storageCodeTo.Trim(); q = q.Where(x => x.StorageCodeTo == s); }
        if (!string.IsNullOrWhiteSpace(refNoTo)) { var r = refNoTo.Trim(); q = q.Where(x => x.RefNoTo == r); }
        if (createdFrom.HasValue) q = q.Where(x => x.CreatedDTime >= createdFrom.Value);
        if (createdTo.HasValue) q = q.Where(x => x.CreatedDTime <= createdTo.Value);
        if (dTimeFrom.HasValue) q = q.Where(x => x.DTimeFrom >= dTimeFrom.Value);
        if (dTimeFromTo.HasValue) q = q.Where(x => x.DTimeFrom <= dTimeFromTo.Value);
        if (dTimeToFrom.HasValue) q = q.Where(x => x.DTimeTo != null && x.DTimeTo >= dTimeToFrom.Value);
        if (dTimeTo.HasValue) q = q.Where(x => x.DTimeTo != null && x.DTimeTo <= dTimeTo.Value);

        var list = await q.OrderByDescending(x => x.CreatedDTime).ThenByDescending(x => x.Id).ToListAsync();
        return list.Select(ToDto).ToList();
    }

    public async Task<StorageTransactionDto?> GetAsync(string vin, string refNo)
    {
        if (string.IsNullOrWhiteSpace(vin) || string.IsNullOrWhiteSpace(refNo)) return null;
        var v = vin.Trim();
        var r = refNo.Trim();
        var e = await db.StorageTransactions.AsNoTracking()
            .FirstOrDefaultAsync(x => x.OrgId == Org && x.Vin == v && x.RefNo == r);
        return e is null ? null : ToDto(e);
    }

    public async Task<StorageTransactionCreateResultDto> CreateAsync(List<StorageTransactionRowDto> rows, string? actor)
    {
        // Guard nguồn (Sto_StorageTransaction_AddX_TableBeBlank): lô rỗng thì không ghi.
        if (rows is null || rows.Count < 1)
            throw new InvalidOperationException("Danh sách giao dịch kho rỗng, không có dòng dữ liệu nào để ghi.");

        var user = string.IsNullOrWhiteSpace(actor) ? "HQ_STORAGE_USER" : actor.Trim();
        var now = DateTime.Now;

        // Cấm trùng VIN trong cùng một lô (Sto_StorageTransaction_AddX_DuplicateVIN).
        var seenVin = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var prepared = new List<StorageTransaction>();
        var keys = new List<string>();

        foreach (var row in rows)
        {
            var vin = (row.Vin ?? "").Trim();
            var refNo = (row.RefNo ?? "").Trim();
            var refTypeRaw = (row.RefType ?? "").Trim();
            var storageCode = (row.StorageCode ?? "").Trim();
            var storageCodeTo = string.IsNullOrWhiteSpace(row.StorageCodeTo) ? null : row.StorageCodeTo.Trim();
            var refNoTo = string.IsNullOrWhiteSpace(row.RefNoTo) ? null : row.RefNoTo.Trim();

            // Check input empty (Sto_StorageTransaction_AddX_*IsEmpty).
            if (vin.Length < 1)
                throw new InvalidOperationException("Số VIN (VIN) không được để trống.");
            if (refNo.Length < 1)
                throw new InvalidOperationException($"Mã nghiệp vụ tạo (RefNo) của VIN {vin} không được để trống.");
            if (refTypeRaw.Length < 1)
                throw new InvalidOperationException($"Loại nghiệp vụ tạo (RefType) của VIN {vin} không được để trống.");
            if (storageCode.Length < 1)
                throw new InvalidOperationException($"Mã kho nhập (StorageCode) của VIN {vin} không được để trống.");
            if (row.DTimeFrom is null)
                throw new InvalidOperationException($"Ngày nhập kho (DTimeFrom) của VIN {vin} không được để trống.");

            if (!Enum.TryParse<StorageTransactionRefType>(refTypeRaw, true, out var refType))
                throw new InvalidOperationException($"Loại nghiệp vụ tạo (RefType) của VIN {vin} không hợp lệ (chỉ nhận PL hoặc BBGN).");

            if (!seenVin.Add(vin))
                throw new InvalidOperationException($"Số VIN {vin} bị trùng lặp trong cùng một lô giao dịch kho.");

            // Guard nguồn (Sto_StorageTransaction_Check, FlagExist = Inactive): cặp (VIN, RefNo) đã tồn tại thì CẤM ghi lại.
            var dup = await db.StorageTransactions.AnyAsync(x => x.OrgId == Org && x.Vin == vin && x.RefNo == refNo);
            if (dup)
                throw new InvalidOperationException($"VIN {vin} đã có giao dịch kho với chứng từ {refNo}.");

            // Cờ FlagInDay: nếu xe đã có giao dịch xuất kho (StorageCodeTo + DTimeTo) cùng ngày nhập của kho nhập.
            var day = row.DTimeFrom.Value.Date;
            var inDay = await db.StorageTransactions.AnyAsync(x => x.OrgId == Org && x.Vin == vin
                && x.StorageCodeTo == storageCode && x.DTimeTo != null && x.DTimeTo.Value.Date == day);

            prepared.Add(new StorageTransaction
            {
                OrgId = Org,
                Vin = vin,
                RefNo = refNo,
                RefType = refType,
                StorageCode = storageCode,
                StorageCodeTo = storageCodeTo,
                DTimeFrom = row.DTimeFrom.Value,
                DTimeTo = row.DTimeTo,
                RefNoTo = refNoTo,
                FlagInDay = inDay ? "1" : "0",
                Remark = string.IsNullOrWhiteSpace(row.Remark) ? null : row.Remark.Trim(),
                CreatedBy = user,
                CreatedDTime = now,
                LogLUBy = user,
                LogLUDateTime = now
            });
            keys.Add($"{vin}|{refNo}");
        }

        db.StorageTransactions.AddRange(prepared);
        await db.SaveChangesAsync();

        return new StorageTransactionCreateResultDto(rows.Count, prepared.Count, keys);
    }

    public async Task<StorageTransactionStatsDto> GetStatsAsync()
    {
        var list = await db.StorageTransactions.AsNoTracking().Where(x => x.OrgId == Org).ToListAsync();
        return new StorageTransactionStatsDto(
            TotalTransactions: list.Count,
            TotalVins: list.Select(x => x.Vin).Distinct().Count(),
            TotalInDay: list.Count(x => x.FlagInDay == "1"),
            TotalOut: list.Count(x => x.DTimeTo != null),
            TotalInStorage: list.Count(x => x.DTimeTo == null),
            ByRefType: list.GroupBy(x => x.RefType.ToString())
                           .ToDictionary(g => g.Key, g => g.Count()),
            ByStorage: list.GroupBy(x => string.IsNullOrWhiteSpace(x.StorageCode) ? "UNKNOWN" : x.StorageCode)
                           .ToDictionary(g => g.Key, g => g.Count())
        );
    }

    private static StorageTransactionDto ToDto(StorageTransaction e) => new(
        e.Id, e.Vin, e.RefNo, e.RefType.ToString(), e.StorageCode, e.StorageCodeTo,
        e.DTimeFrom, e.DTimeTo, e.RefNoTo, e.FlagInDay, e.FlagInDay == "1",
        e.Remark, e.CreatedBy, e.CreatedDTime, e.LogLUBy, e.LogLUDateTime);
}
