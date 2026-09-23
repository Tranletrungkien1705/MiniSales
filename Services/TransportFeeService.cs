using Microsoft.EntityFrameworkCore;
using MiniSales.Data;
using MiniSales.Models;

namespace MiniSales.Services;

// ===== DTOs =====
public record TransportFeeRouteDto(
    string ProvinceCodeFrom,
    string DistrictCodeFrom,
    string ProvinceCodeTo,
    string DistrictCodeTo,
    string TransporterCode,
    string ModelCode,
    decimal ValFee,
    int ExpectedDays
);

public record CreateTransportFeeVersionDto(
    string? TFVCode,
    List<TransportFeeRouteDto>? Routes,
    string? ActionBy
);

public record TransportFeeDetailDto(
    long Id,
    string TFVCode,
    string ProvinceCodeFrom,
    string DistrictCodeFrom,
    string ProvinceCodeTo,
    string DistrictCodeTo,
    string TransporterCode,
    string ModelCode,
    decimal ValFee,
    int ExpectedDays,
    bool IsReverseRoute,
    DateTime LogLUDateTime,
    string? LogLUBy
);

public record TransportFeeVersionDto(
    long Id,
    string TFVCode,
    string FlagActive,
    bool IsActive,
    DateTime CreatedDate,
    DateTime LogLUDateTime,
    string? LogLUBy,
    int TotalRoutes
);

public record TransportFeeVersionDetailDto(
    long Id,
    string TFVCode,
    string FlagActive,
    bool IsActive,
    DateTime CreatedDate,
    DateTime LogLUDateTime,
    string? LogLUBy,
    List<TransportFeeDetailDto> Routes
);

public record TransportFeeHistoryDto(
    long Id,
    string TFVCode,
    string ProvinceCodeFrom,
    string DistrictCodeFrom,
    string ProvinceCodeTo,
    string DistrictCodeTo,
    string? TransporterCodeList,
    string? ModelCodeList,
    decimal ValFee,
    int ExpectedDays,
    DateTime LogLUDateTime,
    string? LogLUBy
);

public record TransportFeeLookupDto(
    string TFVCode,
    string ProvinceCodeFrom,
    string DistrictCodeFrom,
    string ProvinceCodeTo,
    string DistrictCodeTo,
    string TransporterCode,
    string ModelCode,
    decimal ValFee,
    int ExpectedDays
);

public record TransportFeeStatsDto(
    int TotalVersions,
    int TotalActiveVersions,
    int TotalRoutes,
    int TotalReverseRoutes,
    int TotalTransporters,
    int TotalModels,
    int TotalProvinces,
    decimal AvgValFee,
    decimal MaxValFee,
    decimal MinValFee,
    Dictionary<string, decimal> AvgFeeByModel
);

public interface ITransportFeeService
{
    Task<string> GetTFVCodeAsync();
    Task<List<TransportFeeVersionDto>> SearchAsync(string? tfvCode, string? flagActive, DateTime? createdDateFrom, DateTime? createdDateTo);
    Task<TransportFeeVersionDetailDto?> GetByTFVCodeAsync(string tfvCode);
    Task<List<TransportFeeHistoryDto>> GetHistByTFVCodeAsync(string tfvCode);
    Task<TransportFeeVersionDetailDto> CreateAsync(CreateTransportFeeVersionDto dto);
    Task<bool> DeleteAsync(string tfvCode);
    Task<TransportFeeLookupDto?> LookupAsync(string provinceCodeFrom, string districtCodeFrom, string provinceCodeTo, string districtCodeTo, string transporterCode, string modelCode);
    Task<TransportFeeStatsDto> GetStatsAsync();
}

/// <summary>
/// Quản lý Cước phí vận tải (DMS.Sales Mst_TranspFeeVer + Mst_TranspFee + Mst_TranspFeeHist / MstTranspFeeController /
/// Master.cs Mst_TranspFeeVer_Get|Create|Delete / Storage.cs GetTFVCode):
/// - Phiên bản bảng cước phí (Mst_TranspFeeVer) quản lý theo TFVCode; mỗi lần Create 1 phiên bản mới, toàn bộ
///   bảng cước phí hiện hành (Mst_TranspFee) bị xóa và thay bằng dữ liệu phiên bản mới (chỉ phiên bản mới nhất có data).
/// - Mỗi dòng cước phí = 1 tuyến (tỉnh/huyện giao -> tỉnh/huyện nhận) + 1 nhà vận chuyển + 1 model xe, kèm
///   cước phí (ValFee > 0) và số ngày định mức (ExpectedDays > 0).
/// - Khi Create: server tự tách chuỗi ModelCode/TransporterCode cách phẩy thành nhiều dòng, kiểm tra trùng khóa
///   (ProvinceFrom+DistrictFrom+ProvinceTo+DistrictTo+Transporter+Model), chặn tuyến A=A (From trùng To), rồi tự
///   sinh thêm tuyến ngược (đảo From/To) — bám #tbl_genAuto trong Mst_TranspFeeVer_CreateX.
/// - Lưu vết mỗi lần tạo vào Mst_TranspFeeHist (giữ nguyên chuỗi TransporterCodeList/ModelCodeList cách phẩy).
/// - Tra cứu cước phí theo tuyến + nhà vận chuyển + model (GetTFVCode) phục vụ tính cước vận tải.
/// </summary>
public sealed class TransportFeeService(AppDbContext db, ITenantContext tenant) : ITransportFeeService
{
    private Guid Org => tenant.OrgId;

    public async Task<string> GetTFVCodeAsync()
    {
        // Sinh mã phiên bản cước phí kế tiếp theo quy chuẩn TFV{seq:D3} (bám MstTranspFee/GetTFVCode).
        var existing = await db.TransportFeeVersions.AsNoTracking()
            .Where(x => x.OrgId == Org && x.TFVCode.StartsWith("TFV"))
            .Select(x => x.TFVCode)
            .ToListAsync();

        var maxSeq = 0;
        foreach (var code in existing)
        {
            var tail = code.Substring(3);
            if (int.TryParse(tail, out var n) && n > maxSeq) maxSeq = n;
        }
        return $"TFV{(maxSeq + 1):D3}";
    }

    public async Task<List<TransportFeeVersionDto>> SearchAsync(string? tfvCode, string? flagActive, DateTime? createdDateFrom, DateTime? createdDateTo)
    {
        var q = db.TransportFeeVersions.AsNoTracking().Where(x => x.OrgId == Org);

        if (!string.IsNullOrWhiteSpace(tfvCode))
        {
            var c = tfvCode.Trim();
            q = q.Where(x => x.TFVCode.Contains(c));
        }
        if (!string.IsNullOrWhiteSpace(flagActive))
        {
            var fa = flagActive.Trim();
            q = q.Where(x => x.FlagActive == fa);
        }
        if (createdDateFrom.HasValue)
            q = q.Where(x => x.CreatedDate >= createdDateFrom.Value.Date);
        if (createdDateTo.HasValue)
            q = q.Where(x => x.CreatedDate < createdDateTo.Value.Date.AddDays(1));

        var versions = await q.OrderByDescending(x => x.CreatedDate).ToListAsync();
        var codes = versions.Select(v => v.TFVCode).ToList();
        var routeCounts = await db.TransportFeeDetails.AsNoTracking()
            .Where(d => d.TFVCode != null && codes.Contains(d.TFVCode))
            .GroupBy(d => d.TFVCode)
            .Select(g => new { TFVCode = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.TFVCode, x => x.Count);

        return versions.Select(v => new TransportFeeVersionDto(
            v.Id, v.TFVCode, v.FlagActive, v.FlagActive == "1", v.CreatedDate, v.LogLUDateTime, v.LogLUBy,
            routeCounts.TryGetValue(v.TFVCode, out var c) ? c : 0)).ToList();
    }

    public async Task<TransportFeeVersionDetailDto?> GetByTFVCodeAsync(string tfvCode)
    {
        if (string.IsNullOrWhiteSpace(tfvCode)) return null;
        var code = tfvCode.Trim().ToUpperInvariant();
        var version = await db.TransportFeeVersions.AsNoTracking()
            .FirstOrDefaultAsync(x => x.OrgId == Org && x.TFVCode == code);
        if (version is null) return null;

        var routes = await db.TransportFeeDetails.AsNoTracking()
            .Where(d => d.TFVCode == code)
            .OrderBy(d => d.ProvinceCodeFrom).ThenBy(d => d.DistrictCodeFrom)
            .ThenBy(d => d.ProvinceCodeTo).ThenBy(d => d.DistrictCodeTo)
            .ThenBy(d => d.TransporterCode).ThenBy(d => d.ModelCode)
            .ToListAsync();

        return new TransportFeeVersionDetailDto(
            version.Id, version.TFVCode, version.FlagActive, version.FlagActive == "1",
            version.CreatedDate, version.LogLUDateTime, version.LogLUBy,
            routes.Select(ToDetailDto).ToList());
    }

    public async Task<List<TransportFeeHistoryDto>> GetHistByTFVCodeAsync(string tfvCode)
    {
        if (string.IsNullOrWhiteSpace(tfvCode)) return new List<TransportFeeHistoryDto>();
        var code = tfvCode.Trim().ToUpperInvariant();
        var hist = await db.TransportFeeHistories.AsNoTracking()
            .Where(x => x.OrgId == Org && x.TFVCode == code)
            .OrderBy(x => x.ProvinceCodeFrom).ThenBy(x => x.DistrictCodeFrom)
            .ThenBy(x => x.ProvinceCodeTo).ThenBy(x => x.DistrictCodeTo)
            .ToListAsync();

        return hist.Select(h => new TransportFeeHistoryDto(
            h.Id, h.TFVCode, h.ProvinceCodeFrom, h.DistrictCodeFrom, h.ProvinceCodeTo, h.DistrictCodeTo,
            h.TransporterCodeList, h.ModelCodeList, h.ValFee, h.ExpectedDays, h.LogLUDateTime, h.LogLUBy)).ToList();
    }

    public async Task<TransportFeeVersionDetailDto> CreateAsync(CreateTransportFeeVersionDto dto)
    {
        if (dto.Routes is null || dto.Routes.Count < 1)
            throw new InvalidOperationException("Bảng cước phí phải có ít nhất 1 dòng tuyến cước (Mst_TranspFee).");

        // Sinh mã phiên bản nếu chưa truyền vào (bám MstTranspFee/GetTFVCode).
        var tfvCode = string.IsNullOrWhiteSpace(dto.TFVCode)
            ? await GetTFVCodeAsync()
            : dto.TFVCode.Trim().ToUpperInvariant();

        if (tfvCode.Length < 3)
            throw new InvalidOperationException("Mã phiên bản cước phí (TFVCode) không hợp lệ (tối thiểu 3 ký tự).");

        if (await db.TransportFeeVersions.AnyAsync(x => x.OrgId == Org && x.TFVCode == tfvCode))
            throw new InvalidOperationException($"Mã phiên bản cước phí {tfvCode} đã tồn tại trong hệ thống.");

        var user = string.IsNullOrWhiteSpace(dto.ActionBy) ? "CHUYEN_VIEN_NPP" : dto.ActionBy.Trim();
        var now = DateTime.Now;

        // Bước 1: tách chuỗi ModelCode/TransporterCode cách phẩy thành từng dòng đơn (bám Mst_TranspFeeVer_CreateX).
        var expanded = new List<TransportFeeRouteDto>();
        foreach (var r in dto.Routes)
        {
            if (string.IsNullOrWhiteSpace(r.ProvinceCodeFrom) || string.IsNullOrWhiteSpace(r.DistrictCodeFrom)
                || string.IsNullOrWhiteSpace(r.ProvinceCodeTo) || string.IsNullOrWhiteSpace(r.DistrictCodeTo))
                throw new InvalidOperationException("Dòng tuyến cước bắt buộc phải có đủ ProvinceCodeFrom/DistrictCodeFrom/ProvinceCodeTo/DistrictCodeTo.");

            if (string.IsNullOrWhiteSpace(r.TransporterCode))
                throw new InvalidOperationException("Dòng tuyến cước bắt buộc phải có TransporterCode (mã nhà vận chuyển).");
            if (string.IsNullOrWhiteSpace(r.ModelCode))
                throw new InvalidOperationException("Dòng tuyến cước bắt buộc phải có ModelCode (mã model xe).");

            if (r.ValFee <= 0)
                throw new InvalidOperationException($"Cước phí (ValFee) cho tuyến {r.ProvinceCodeFrom}-{r.DistrictCodeFrom} -> {r.ProvinceCodeTo}-{r.DistrictCodeTo} phải lớn hơn 0 (nhập: {r.ValFee}).");
            if (r.ExpectedDays <= 0)
                throw new InvalidOperationException($"Số ngày định mức (ExpectedDays) cho tuyến {r.ProvinceCodeFrom}-{r.DistrictCodeFrom} -> {r.ProvinceCodeTo}-{r.DistrictCodeTo} phải lớn hơn 0 (nhập: {r.ExpectedDays}).");

            var models = r.ModelCode.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            var transporters = r.TransporterCode.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (models.Length == 0)
                throw new InvalidOperationException("Dòng tuyến cước bắt buộc phải có ModelCode (mã model xe).");
            if (transporters.Length == 0)
                throw new InvalidOperationException("Dòng tuyến cước bắt buộc phải có TransporterCode (mã nhà vận chuyển).");

            foreach (var model in models)
                foreach (var transporter in transporters)
                    expanded.Add(new TransportFeeRouteDto(
                        r.ProvinceCodeFrom.Trim().ToUpperInvariant(), r.DistrictCodeFrom.Trim().ToUpperInvariant(),
                        r.ProvinceCodeTo.Trim().ToUpperInvariant(), r.DistrictCodeTo.Trim().ToUpperInvariant(),
                        transporter.ToUpperInvariant(), model.ToUpperInvariant(), r.ValFee, r.ExpectedDays));
        }

        // Bước 2: kiểm tra trùng khóa dòng + chặn tuyến A=A (From trùng To) — bám Mst_TranspFeeVer_CreateX.
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var e in expanded)
        {
            if (string.Equals(e.DistrictCodeFrom, e.DistrictCodeTo, StringComparison.OrdinalIgnoreCase)
                && string.Equals(e.ProvinceCodeFrom, e.ProvinceCodeTo, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException($"Tuyến không hợp lệ: điểm giao và điểm nhận trùng nhau ({e.ProvinceCodeFrom}-{e.DistrictCodeFrom}).");

            var key = Key(e.ProvinceCodeFrom, e.DistrictCodeFrom, e.ProvinceCodeTo, e.DistrictCodeTo, e.TransporterCode, e.ModelCode);
            if (!seen.Add(key))
                throw new InvalidOperationException($"Trùng lặp dòng cước phí cho tuyến {e.ProvinceCodeFrom}-{e.DistrictCodeFrom} -> {e.ProvinceCodeTo}-{e.DistrictCodeTo} / Nhà vận chuyển {e.TransporterCode} / Model {e.ModelCode}.");
        }

        // Bước 3: tự sinh tuyến ngược (đảo From/To) — bám #tbl_genAuto (UNION ALL #tbl_goc + #tbl_genAuto).
        var finalRoutes = new List<(TransportFeeRouteDto Route, bool IsReverse)>();
        foreach (var e in expanded)
        {
            finalRoutes.Add((e, false));
            var reverse = new TransportFeeRouteDto(
                e.ProvinceCodeTo, e.DistrictCodeTo, e.ProvinceCodeFrom, e.DistrictCodeFrom,
                e.TransporterCode, e.ModelCode, e.ValFee, e.ExpectedDays);
            var reverseKey = Key(reverse.ProvinceCodeFrom, reverse.DistrictCodeFrom, reverse.ProvinceCodeTo, reverse.DistrictCodeTo, reverse.TransporterCode, reverse.ModelCode);
            if (seen.Add(reverseKey))
                finalRoutes.Add((reverse, true));
        }

        // Bước 4: xóa toàn bộ bảng cước phí hiện hành rồi ghi phiên bản mới (bám DELETE FROM Mst_TranspFee + insert).
        var oldDetails = await db.TransportFeeDetails.Where(d => d.TFVCode != null).ToListAsync();
        if (oldDetails.Count > 0)
            db.TransportFeeDetails.RemoveRange(oldDetails);

        var version = new TransportFeeVersion
        {
            OrgId = Org,
            TFVCode = tfvCode,
            FlagActive = "1",
            CreatedDate = now,
            LogLUDateTime = now,
            LogLUBy = user
        };
        version.Details = finalRoutes.Select(f => new TransportFeeDetail
        {
            TFVCode = tfvCode,
            ProvinceCodeFrom = f.Route.ProvinceCodeFrom,
            DistrictCodeFrom = f.Route.DistrictCodeFrom,
            ProvinceCodeTo = f.Route.ProvinceCodeTo,
            DistrictCodeTo = f.Route.DistrictCodeTo,
            TransporterCode = f.Route.TransporterCode,
            ModelCode = f.Route.ModelCode,
            ValFee = f.Route.ValFee,
            ExpectedDays = f.Route.ExpectedDays,
            IsReverseRoute = f.IsReverse,
            LogLUDateTime = now,
            LogLUBy = user
        }).ToList();

        db.TransportFeeVersions.Add(version);

        // Bước 5: lưu vết lịch sử (Mst_TranspFeeHist) giữ nguyên chuỗi cách phẩy như dữ liệu nhập gốc.
        foreach (var r in dto.Routes)
        {
            db.TransportFeeHistories.Add(new TransportFeeHistory
            {
                OrgId = Org,
                TFVCode = tfvCode,
                ProvinceCodeFrom = r.ProvinceCodeFrom.Trim().ToUpperInvariant(),
                DistrictCodeFrom = r.DistrictCodeFrom.Trim().ToUpperInvariant(),
                ProvinceCodeTo = r.ProvinceCodeTo.Trim().ToUpperInvariant(),
                DistrictCodeTo = r.DistrictCodeTo.Trim().ToUpperInvariant(),
                TransporterCodeList = r.TransporterCode.Trim().ToUpperInvariant(),
                ModelCodeList = r.ModelCode.Trim().ToUpperInvariant(),
                ValFee = r.ValFee,
                ExpectedDays = r.ExpectedDays,
                LogLUDateTime = now,
                LogLUBy = user
            });
        }

        await db.SaveChangesAsync();

        return (await GetByTFVCodeAsync(tfvCode))!;
    }

    public async Task<bool> DeleteAsync(string tfvCode)
    {
        if (string.IsNullOrWhiteSpace(tfvCode)) return false;
        var code = tfvCode.Trim().ToUpperInvariant();
        var version = await db.TransportFeeVersions
            .Include(x => x.Details)
            .FirstOrDefaultAsync(x => x.OrgId == Org && x.TFVCode == code);
        if (version is null) return false;

        db.TransportFeeVersions.Remove(version);
        await db.SaveChangesAsync();
        return true;
    }

    public async Task<TransportFeeLookupDto?> LookupAsync(string provinceCodeFrom, string districtCodeFrom, string provinceCodeTo, string districtCodeTo, string transporterCode, string modelCode)
    {
        if (string.IsNullOrWhiteSpace(provinceCodeFrom) || string.IsNullOrWhiteSpace(districtCodeFrom)
            || string.IsNullOrWhiteSpace(provinceCodeTo) || string.IsNullOrWhiteSpace(districtCodeTo)
            || string.IsNullOrWhiteSpace(transporterCode) || string.IsNullOrWhiteSpace(modelCode))
            return null;

        // Chỉ tra cứu trên phiên bản đang hoạt động (bám GetTFVCode: INNER JOIN Mst_TranspFeeVer ... FlagActive='1').
        var activeCodes = await db.TransportFeeVersions.AsNoTracking()
            .Where(v => v.OrgId == Org && v.FlagActive == "1")
            .Select(v => v.TFVCode)
            .ToListAsync();

        var pFrom = provinceCodeFrom.Trim().ToUpperInvariant();
        var dFrom = districtCodeFrom.Trim().ToUpperInvariant();
        var pTo = provinceCodeTo.Trim().ToUpperInvariant();
        var dTo = districtCodeTo.Trim().ToUpperInvariant();
        var trans = transporterCode.Trim().ToUpperInvariant();
        var model = modelCode.Trim().ToUpperInvariant();

        var fee = await db.TransportFeeDetails.AsNoTracking()
            .Where(d => activeCodes.Contains(d.TFVCode)
                     && d.ProvinceCodeFrom == pFrom && d.DistrictCodeFrom == dFrom
                     && d.ProvinceCodeTo == pTo && d.DistrictCodeTo == dTo
                     && d.TransporterCode == trans && d.ModelCode == model)
            .FirstOrDefaultAsync();

        if (fee is null) return null;

        return new TransportFeeLookupDto(
            fee.TFVCode, fee.ProvinceCodeFrom, fee.DistrictCodeFrom, fee.ProvinceCodeTo, fee.DistrictCodeTo,
            fee.TransporterCode, fee.ModelCode, fee.ValFee, fee.ExpectedDays);
    }

    public async Task<TransportFeeStatsDto> GetStatsAsync()
    {
        var versions = await db.TransportFeeVersions.AsNoTracking().Where(x => x.OrgId == Org).ToListAsync();
        var details = await db.TransportFeeDetails.AsNoTracking().Where(x => x.TFVCode != null).ToListAsync();

        return new TransportFeeStatsDto(
            TotalVersions: versions.Count,
            TotalActiveVersions: versions.Count(x => x.FlagActive == "1"),
            TotalRoutes: details.Count,
            TotalReverseRoutes: details.Count(x => x.IsReverseRoute),
            TotalTransporters: details.Select(x => x.TransporterCode).Distinct().Count(),
            TotalModels: details.Select(x => x.ModelCode).Distinct().Count(),
            TotalProvinces: details.Select(x => x.ProvinceCodeFrom).Concat(details.Select(x => x.ProvinceCodeTo)).Distinct().Count(),
            AvgValFee: details.Count > 0 ? Math.Round(details.Average(x => x.ValFee), 2) : 0m,
            MaxValFee: details.Count > 0 ? details.Max(x => x.ValFee) : 0m,
            MinValFee: details.Count > 0 ? details.Min(x => x.ValFee) : 0m,
            AvgFeeByModel: details.GroupBy(x => string.IsNullOrWhiteSpace(x.ModelCode) ? "UNKNOWN" : x.ModelCode)
                                  .ToDictionary(g => g.Key, g => Math.Round(g.Average(x => x.ValFee), 2))
        );
    }

    // ===== Helpers =====
    private static string Key(string pFrom, string dFrom, string pTo, string dTo, string transporter, string model)
        => $"{pFrom}|{dFrom}|{pTo}|{dTo}|{transporter}|{model}";

    private static TransportFeeDetailDto ToDetailDto(TransportFeeDetail d) => new(
        d.Id, d.TFVCode, d.ProvinceCodeFrom, d.DistrictCodeFrom, d.ProvinceCodeTo, d.DistrictCodeTo,
        d.TransporterCode, d.ModelCode, d.ValFee, d.ExpectedDays, d.IsReverseRoute, d.LogLUDateTime, d.LogLUBy);
}