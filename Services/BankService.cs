using Microsoft.EntityFrameworkCore;
using MiniSales.Data;
using MiniSales.Models;

namespace MiniSales.Services;

// ===== DTOs =====
public record CreateBankDto(
    string BankCode = "",
    string BankName = "",
    string? BankCodeParent = null,
    string? ProvinceCode = null,
    string? PhoneNo = null,
    string? FaxNo = null,
    string? BenBankCode = null,
    string? Address = null,
    string? PICEmail = null,
    string? PICName = null,
    string FlagPaymentBank = "0",
    string FlagMortageBank = "0",
    string FlagMonitorBank = "0",
    string? Remark = null,
    string? ActionBy = null
);

public record UpdateBankDto(
    string? BankName = null,
    string? PhoneNo = null,
    string? FaxNo = null,
    string? BenBankCode = null,
    string? Address = null,
    string? PICEmail = null,
    string? PICName = null,
    string FlagPaymentBank = "0",
    string FlagMortageBank = "0",
    string FlagMonitorBank = "0",
    string? Remark = null,
    string FlagActive = "1",
    string? ActionBy = null
);

public record BankImportRowDto(
    string? BankCode,
    string? BankName,
    string? BankCodeParent,
    string? ProvinceCode,
    string? PhoneNo,
    string? FaxNo,
    string? BenBankCode,
    string? Address,
    string? PICEmail,
    string? PICName,
    string? FlagPaymentBank,
    string? FlagMortageBank,
    string? FlagMonitorBank,
    string? Remark
);

public record BankDto(
    long Id,
    string BankCode,
    string? BankCodeParent,
    string? ProvinceCode,
    string BankName,
    string? PhoneNo,
    string? FaxNo,
    string? BenBankCode,
    string? Address,
    string? PICEmail,
    string? PICName,
    string FlagPaymentBank,
    string FlagMortageBank,
    string FlagMonitorBank,
    string? Remark,
    string FlagActive,
    bool IsActive,
    DateTime LogLUDateTime,
    string? LogLUBy
);

public record BankImportResultDto(
    int TotalRows,
    int Inserted,
    int Updated,
    List<string> BankCodes
);

public record BankStatsDto(
    int TotalBanks,
    int TotalActive,
    int TotalInactive,
    int TotalPaymentBanks,
    int TotalMortageBanks,
    int TotalMonitorBanks,
    Dictionary<string, int> BanksByParent
);

public interface IBankService
{
    Task<List<BankDto>> SearchAsync(string? keyWord, string? bankCode, string? flagActive);
    Task<BankDto?> GetAsync(string bankCode);
    Task<BankDto> CreateAsync(CreateBankDto dto);
    Task<BankDto?> UpdateAsync(string bankCode, UpdateBankDto dto);
    Task<bool> DeleteAsync(string bankCode);
    Task<BankImportResultDto> ImportAsync(List<BankImportRowDto> rows);
    Task<BankStatsDto> GetStatsAsync();
}

/// <summary>
/// Quản lý Danh mục Ngân hàng đối tác (DMS.Sales Mst_Bank / Master.cs /
/// Mst_Bank_Get|Create|Update|Delete|Import): danh mục ngân hàng cấp bảo lãnh / tài trợ cho đại lý mua xe.
/// BankCode là khóa nghiệp vụ duy nhất; bắt buộc BankName. Các cờ nghiệp vụ: FlagPaymentBank (NH thanh toán),
/// FlagMortageBank (NH hỗ trợ thế chấp/giải chấp), FlagMonitorBank (NH giám sát — chỉ chi hội sở
/// BankCodeParent='ALL' mới được bật). Update chỉ cho sửa các cột nghiệp vụ (không sửa BankCode/BankCodeParent/
/// ProvinceCode). Hỗ trợ nhập hàng loạt (Import) từ Excel: kiểm tra field từng dòng + trùng khóa giữa các dòng,
/// rồi upsert (dòng đã có thì cập nhật, dòng mới thì thêm).
/// </summary>
public sealed class BankService(AppDbContext db, ITenantContext tenant) : IBankService
{
    private Guid Org => tenant.OrgId;

    public async Task<List<BankDto>> SearchAsync(string? keyWord, string? bankCode, string? flagActive)
    {
        var q = db.Banks.AsNoTracking().Where(x => x.OrgId == Org);

        if (!string.IsNullOrWhiteSpace(keyWord))
        {
            var kw = keyWord.Trim();
            q = q.Where(x => x.BankCode.Contains(kw)
                          || x.BankName.Contains(kw)
                          || (x.BankCodeParent != null && x.BankCodeParent.Contains(kw))
                          || (x.Address != null && x.Address.Contains(kw)));
        }
        if (!string.IsNullOrWhiteSpace(bankCode))
        {
            var bc = bankCode.Trim();
            q = q.Where(x => x.BankCode == bc);
        }
        if (!string.IsNullOrWhiteSpace(flagActive))
        {
            var fa = flagActive.Trim();
            q = q.Where(x => x.FlagActive == fa);
        }

        var list = await q.OrderBy(x => x.BankCode).ToListAsync();
        return list.Select(ToDto).ToList();
    }

    public async Task<BankDto?> GetAsync(string bankCode)
    {
        if (string.IsNullOrWhiteSpace(bankCode)) return null;
        var bc = bankCode.Trim();
        var e = await db.Banks.AsNoTracking().FirstOrDefaultAsync(x => x.OrgId == Org && x.BankCode == bc);
        return e is null ? null : ToDto(e);
    }

    public async Task<BankDto> CreateAsync(CreateBankDto dto)
    {
        var bankCode = (dto.BankCode ?? "").Trim();
        if (bankCode.Length < 1)
            throw new InvalidOperationException("Mã ngân hàng (BankCode) không được để trống.");

        // BankCode là khóa nghiệp vụ duy nhất (Mst_Bank_CheckDB - FlagExistToCheck = No)
        if (await db.Banks.AnyAsync(x => x.OrgId == Org && x.BankCode == bankCode))
            throw new InvalidOperationException($"Ngân hàng '{bankCode}' đã tồn tại.");

        var bankName = (dto.BankName ?? "").Trim();
        if (bankName.Length < 1)
            throw new InvalidOperationException("Tên ngân hàng (BankName) không được để trống.");

        var flagPayment = NormalizeFlag(dto.FlagPaymentBank);
        var flagMortage = NormalizeFlag(dto.FlagMortageBank);
        var flagMonitor = NormalizeFlag(dto.FlagMonitorBank);
        var bankCodeParent = dto.BankCodeParent?.Trim();
        ValidateMonitorBank(bankCodeParent, flagMonitor);
        ValidateEmails(dto.PICEmail);

        var user = string.IsNullOrWhiteSpace(dto.ActionBy) ? "CHUYEN_VIEN_NPP" : dto.ActionBy.Trim();
        var entity = new BankMaster
        {
            OrgId = Org,
            BankCode = bankCode,
            BankCodeParent = bankCodeParent,
            ProvinceCode = dto.ProvinceCode?.Trim(),
            BankName = bankName,
            PhoneNo = dto.PhoneNo?.Trim(),
            FaxNo = dto.FaxNo?.Trim(),
            BenBankCode = dto.BenBankCode?.Trim(),
            Address = dto.Address?.Trim(),
            PICEmail = dto.PICEmail?.Trim(),
            PICName = dto.PICName?.Trim(),
            FlagPaymentBank = flagPayment,
            FlagMortageBank = flagMortage,
            FlagMonitorBank = flagMonitor,
            Remark = dto.Remark,
            FlagActive = "1",
            LogLUDateTime = DateTime.Now,
            LogLUBy = user
        };
        db.Banks.Add(entity);
        await db.SaveChangesAsync();
        return ToDto(entity);
    }

    public async Task<BankDto?> UpdateAsync(string bankCode, UpdateBankDto dto)
    {
        if (string.IsNullOrWhiteSpace(bankCode)) return null;
        var bc = bankCode.Trim();
        var entity = await db.Banks.FirstOrDefaultAsync(x => x.OrgId == Org && x.BankCode == bc);
        if (entity is null) return null;

        // Mst_Bank_Update: BankName + 3 cờ + FlagActive bắt buộc (không được để rỗng)
        var bankName = (dto.BankName ?? "").Trim();
        if (bankName.Length < 1)
            throw new InvalidOperationException("Tên ngân hàng (BankName) không được để trống.");

        var flagPayment = NormalizeFlag(dto.FlagPaymentBank);
        var flagMortage = NormalizeFlag(dto.FlagMortageBank);
        var flagMonitor = NormalizeFlag(dto.FlagMonitorBank);
        ValidateMonitorBank(entity.BankCodeParent, flagMonitor);
        ValidateEmails(dto.PICEmail);

        var user = string.IsNullOrWhiteSpace(dto.ActionBy) ? "CHUYEN_VIEN_NPP" : dto.ActionBy.Trim();
        entity.BankName = bankName;
        entity.PhoneNo = dto.PhoneNo?.Trim();
        entity.FaxNo = dto.FaxNo?.Trim();
        entity.BenBankCode = dto.BenBankCode?.Trim();
        entity.Address = dto.Address?.Trim();
        entity.PICEmail = dto.PICEmail?.Trim();
        entity.PICName = dto.PICName?.Trim();
        entity.FlagPaymentBank = flagPayment;
        entity.FlagMortageBank = flagMortage;
        entity.FlagMonitorBank = flagMonitor;
        if (dto.Remark is not null) entity.Remark = dto.Remark;
        entity.FlagActive = NormalizeFlag(dto.FlagActive);
        entity.LogLUDateTime = DateTime.Now;
        entity.LogLUBy = user;

        await db.SaveChangesAsync();
        return ToDto(entity);
    }

    public async Task<bool> DeleteAsync(string bankCode)
    {
        if (string.IsNullOrWhiteSpace(bankCode)) return false;
        var bc = bankCode.Trim();
        var entity = await db.Banks.FirstOrDefaultAsync(x => x.OrgId == Org && x.BankCode == bc);
        if (entity is null) return false;

        db.Banks.Remove(entity);
        await db.SaveChangesAsync();
        return true;
    }

    public async Task<BankImportResultDto> ImportAsync(List<BankImportRowDto> rows)
    {
        if (rows is null || rows.Count < 1)
            throw new InvalidOperationException("Danh sách nhập (Import) rỗng, không có dòng dữ liệu nào.");

        // Phase 1: kiểm tra field từng dòng (fail-fast giống Create; §WF FrmBank.ValidateRow parity)
        for (int i = 0; i < rows.Count; i++)
        {
            var bankCode = (rows[i].BankCode ?? "").Trim();
            var bankName = (rows[i].BankName ?? "").Trim();
            if (bankCode.Length < 1)
                throw new InvalidOperationException($"Dòng {i}: Mã ngân hàng (BankCode) không được để trống.");
            if (bankName.Length < 1)
                throw new InvalidOperationException($"Dòng {i}: Tên ngân hàng (BankName) không được để trống.");

            var flagPayment = (rows[i].FlagPaymentBank ?? "").Trim();
            if (flagPayment != "0" && flagPayment != "1")
                throw new InvalidOperationException($"Dòng {i}: Cờ NH thanh toán (FlagPaymentBank) chỉ nhận giá trị 0 hoặc 1.");
            var flagMortage = (rows[i].FlagMortageBank ?? "").Trim();
            if (flagMortage != "0" && flagMortage != "1")
                throw new InvalidOperationException($"Dòng {i}: Cờ NH thế chấp (FlagMortageBank) chỉ nhận giá trị 0 hoặc 1.");
            var flagMonitor = (rows[i].FlagMonitorBank ?? "").Trim();
            if (flagMonitor != "0" && flagMonitor != "1")
                throw new InvalidOperationException($"Dòng {i}: Cờ NH giám sát (FlagMonitorBank) chỉ nhận giá trị 0 hoặc 1.");

            // Chi hội sở (BankCodeParent='ALL') mới được bật NH giám sát (WF FrmBank.ValidateRow)
            ValidateMonitorBank((rows[i].BankCodeParent ?? "").Trim(), flagMonitor);

            ValidateEmails(rows[i].PICEmail, i);
        }

        // Phase 2: kiểm tra trùng khóa BankCode giữa các dòng (fail-fast)
        var dup = rows.GroupBy(r => (r.BankCode ?? "").Trim())
                      .FirstOrDefault(g => g.Count() > 1);
        if (dup is not null)
            throw new InvalidOperationException($"Mã ngân hàng '{dup.Key}' bị trùng lặp trong danh sách nhập.");

        var user = "CHUYEN_VIEN_NPP";
        int inserted = 0, updated = 0;
        var codes = new List<string>();

        // Phase 3: upsert (dòng đã có thì cập nhật theo mẫu Update, dòng mới thì thêm theo mẫu Create)
        foreach (var r in rows)
        {
            var bankCode = (r.BankCode ?? "").Trim();
            var existing = await db.Banks.FirstOrDefaultAsync(x => x.OrgId == Org && x.BankCode == bankCode);
            if (existing is not null)
            {
                // UPDATE: chỉ cập nhật các cột Update hiện có cho sửa (không sửa BankCodeParent/ProvinceCode/Remark/FlagActive)
                existing.BankName = (r.BankName ?? "").Trim();
                existing.PhoneNo = r.PhoneNo?.Trim();
                existing.FaxNo = r.FaxNo?.Trim();
                existing.BenBankCode = r.BenBankCode?.Trim();
                existing.Address = r.Address?.Trim();
                existing.PICEmail = r.PICEmail?.Trim();
                existing.PICName = r.PICName?.Trim();
                existing.FlagPaymentBank = NormalizeFlag(r.FlagPaymentBank);
                existing.FlagMortageBank = NormalizeFlag(r.FlagMortageBank);
                existing.FlagMonitorBank = NormalizeFlag(r.FlagMonitorBank);
                existing.LogLUDateTime = DateTime.Now;
                existing.LogLUBy = user;
                updated++;
            }
            else
            {
                db.Banks.Add(new BankMaster
                {
                    OrgId = Org,
                    BankCode = bankCode,
                    BankCodeParent = r.BankCodeParent?.Trim(),
                    ProvinceCode = r.ProvinceCode?.Trim(),
                    BankName = (r.BankName ?? "").Trim(),
                    PhoneNo = r.PhoneNo?.Trim(),
                    FaxNo = r.FaxNo?.Trim(),
                    BenBankCode = r.BenBankCode?.Trim(),
                    Address = r.Address?.Trim(),
                    PICEmail = r.PICEmail?.Trim(),
                    PICName = r.PICName?.Trim(),
                    FlagPaymentBank = NormalizeFlag(r.FlagPaymentBank),
                    FlagMortageBank = NormalizeFlag(r.FlagMortageBank),
                    FlagMonitorBank = NormalizeFlag(r.FlagMonitorBank),
                    Remark = r.Remark,
                    FlagActive = "1",
                    LogLUDateTime = DateTime.Now,
                    LogLUBy = user
                });
                inserted++;
            }
            codes.Add(bankCode);
        }

        await db.SaveChangesAsync();
        return new BankImportResultDto(rows.Count, inserted, updated, codes);
    }

    public async Task<BankStatsDto> GetStatsAsync()
    {
        var list = await db.Banks.AsNoTracking().Where(x => x.OrgId == Org).ToListAsync();
        return new BankStatsDto(
            TotalBanks: list.Count,
            TotalActive: list.Count(x => x.FlagActive == "1"),
            TotalInactive: list.Count(x => x.FlagActive != "1"),
            TotalPaymentBanks: list.Count(x => x.FlagPaymentBank == "1"),
            TotalMortageBanks: list.Count(x => x.FlagMortageBank == "1"),
            TotalMonitorBanks: list.Count(x => x.FlagMonitorBank == "1"),
            BanksByParent: list.Where(x => x.FlagActive == "1")
                               .GroupBy(x => string.IsNullOrWhiteSpace(x.BankCodeParent) ? "UNKNOWN" : x.BankCodeParent!)
                               .ToDictionary(g => g.Key, g => g.Count())
        );
    }

    // ===== Helpers =====
    private static string NormalizeFlag(string? flag)
        => string.IsNullOrWhiteSpace(flag) ? "0" : (flag.Trim() == "1" ? "1" : "0");

    /// <summary>Chỉ chi hội sở (BankCodeParent='ALL') mới được bật cờ NH giám sát (WF FrmBank.ValidateRow).</summary>
    private static void ValidateMonitorBank(string? bankCodeParent, string flagMonitor)
    {
        if (flagMonitor == "1" && !string.Equals((bankCodeParent ?? "").Trim(), "ALL", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Chỉ chi hội sở (BankCodeParent = 'ALL') mới được bật cờ Ngân hàng giám sát (FlagMonitorBank).");
    }

    /// <summary>Kiểm tra danh sách email người liên lạc (tách ',' từng phần phải hợp lệ) (WF FrmBank.ValidateRow).</summary>
    private static void ValidateEmails(string? picEmail, int? rowIndex = null)
    {
        if (string.IsNullOrWhiteSpace(picEmail)) return;
        foreach (var mail in picEmail.Split(','))
        {
            var cur = mail.Trim();
            if (cur.Length < 1) continue;
            if (!IsValidEmail(cur))
            {
                var prefix = rowIndex.HasValue ? $"Dòng {rowIndex}: " : "";
                throw new InvalidOperationException($"{prefix}Email người liên lạc (PICEmail) không hợp lệ: '{cur}'.");
            }
        }
    }

    private static bool IsValidEmail(string email)
    {
        var at = email.IndexOf('@');
        if (at <= 0 || at == email.Length - 1) return false;
        var domain = email[(at + 1)..];
        if (domain.Length < 1 || domain.Contains('@')) return false;
        var dot = domain.IndexOf('.');
        return dot > 0 && dot < domain.Length - 1;
    }

    private static BankDto ToDto(BankMaster e) => new(
        e.Id, e.BankCode, e.BankCodeParent, e.ProvinceCode, e.BankName, e.PhoneNo, e.FaxNo, e.BenBankCode,
        e.Address, e.PICEmail, e.PICName, e.FlagPaymentBank, e.FlagMortageBank, e.FlagMonitorBank, e.Remark,
        e.FlagActive, e.FlagActive == "1", e.LogLUDateTime, e.LogLUBy);
}
