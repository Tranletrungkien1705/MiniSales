using Microsoft.EntityFrameworkCore;
using MiniSales.Data;
using MiniSales.Models;

namespace MiniSales.Services;

// ===== DTOs =====
public record CreateSalesManDto(
    string SMCode = "",
    string DealerCode = "",
    string SMName = "",
    string SMGender = "M",
    DateTime? SMDateOfBirth = null,
    string? SMPhoneNo = null,
    string? SMEmail = null,
    string? SMAddress = null,
    string? ProvinceCode = null,
    string? QualificationCode = null,
    string? SMSpecialized = null,
    string? SMYearExperence = null,
    DateTime? SMStartDate = null,
    DateTime? SMEndDate = null,
    string? DepartmentCode = null,
    string? SMPosition = null,
    string? SMPostionCode = null,
    SalesManType SMType = SalesManType.SalesConsultant,
    string? CertificateCode = null,
    SalesManStatus SMStatus = SalesManStatus.Probation,
    string? SMHyundaiCode = null,
    string? IdentityCardNo = null,
    string? WebsiteLink = null,
    string? FacebookLink = null,
    string? FanpageLink = null,
    string? GroupLink = null,
    string? ZaloLink = null,
    string? DealerSaleCode = null,
    string? ActionBy = null
);

public record UpdateSalesManDto(
    string SMName = "",
    string SMGender = "M",
    DateTime? SMDateOfBirth = null,
    string? SMPhoneNo = null,
    string? SMEmail = null,
    string? SMAddress = null,
    string? ProvinceCode = null,
    string? QualificationCode = null,
    string? SMSpecialized = null,
    string? SMYearExperence = null,
    DateTime? SMStartDate = null,
    DateTime? SMEndDate = null,
    string? DepartmentCode = null,
    string? SMPosition = null,
    string? SMPostionCode = null,
    SalesManType SMType = SalesManType.SalesConsultant,
    string? CertificateCode = null,
    string? SMHyundaiCode = null,
    string? IdentityCardNo = null,
    string? WebsiteLink = null,
    string? FacebookLink = null,
    string? FanpageLink = null,
    string? GroupLink = null,
    string? ZaloLink = null,
    string? DealerSaleCode = null,
    string? ActionBy = null
);

public record UpdateSalesManStatusDto(
    SalesManStatus SMStatus = SalesManStatus.Probation,
    DateTime? SMStartDate = null,
    DateTime? SMEndDate = null,
    string? SMReason = null,
    string? SMDesc = null,
    string? ActionBy = null
);

public record SalesManDto(
    long Id,
    string SMCode,
    string? SMHyundaiCode,
    string DealerCode,
    string? DealerName,
    string SMName,
    string SMGender,
    DateTime? SMDateOfBirth,
    string? SMPhoneNo,
    string? SMEmail,
    string? SMAddress,
    string? ProvinceCode,
    string? QualificationCode,
    string? SMSpecialized,
    string? SMYearExperence,
    DateTime? SMStartDate,
    DateTime? SMEndDate,
    string? DepartmentCode,
    string? SMPosition,
    string? SMPostionCode,
    SalesManType SMType,
    string SMTypeName,
    string? CertificateCode,
    SalesManStatus SMStatus,
    string SMStatusName,
    string? SMReason,
    string? SMDesc,
    string? IdentityCardNo,
    string? WebsiteLink,
    string? FacebookLink,
    string? FanpageLink,
    string? GroupLink,
    string? ZaloLink,
    string? DealerSaleCode,
    string FlagActive,
    bool IsActive,
    string? CreatedBy,
    DateTime CreatedAt,
    string? UpdateBy,
    DateTime? UpdateDTime,
    string? UpdateStatusBy,
    DateTime? UpdateStatusDtime,
    DateTime LogLUDateTime,
    string? LogLUBy
);

public record SalesManHistoryInactiveDto(
    long Id,
    string SMCode,
    string? SMHyundaiCode,
    string DealerCode,
    SalesManStatus SMStatus,
    string SMStatusName,
    string? IdentityCardNo,
    string SMFlagActive,
    DateTime? SMStartDate,
    DateTime? SMEndDate,
    string? SMReason,
    string? SMDesc,
    DateTime InactiveDateTime,
    string? InactiveBy
);

public record SalesManStatsDto(
    int TotalSalesMen,
    int TotalActive,
    int TotalInactive,
    int TotalOfficial,
    int TotalProbation,
    int TotalCollaborator,
    int TotalResigned,
    int TotalDealers,
    Dictionary<string, int> ByDealer,
    Dictionary<string, int> ByStatus,
    Dictionary<string, int> ByType
);

public interface ISalesManService
{
    Task<List<SalesManDto>> SearchAsync(string? smCode, string? dealerCode, string? smStatus, string? smType, string? flagActive);
    Task<SalesManDto?> GetAsync(string smCode);
    Task<SalesManDto> CreateAsync(CreateSalesManDto dto);
    Task<List<SalesManDto>> CreateMultiAsync(List<CreateSalesManDto> rows);
    Task<SalesManDto?> UpdateAsync(string smCode, UpdateSalesManDto dto);
    Task<SalesManDto?> UpdateStatusAsync(string smCode, UpdateSalesManStatusDto dto);
    Task<bool> DeleteAsync(string smCode);
    Task<List<SalesManHistoryInactiveDto>> GetHistoryAsync(string? smCode, string? smHyundaiCode);
    Task<SalesManStatsDto> GetStatsAsync();
}

/// <summary>
/// Quản lý Nhân viên Bán hàng Đại lý (DMS.Sales Mst_SalesMan / MasterData.HR.cs /
/// Mst_SalesMan_Get|CreateMulti|Update|UpdateStatus|CreateAfterApprove|UpdateMultiByHTV):
/// - Hồ sơ nhân sự bán hàng của đại lý (tư vấn bán hàng, trưởng phòng, giám đốc...). Khóa nghiệp vụ = SMCode.
/// - Trạng thái SMStatus: NGHIVIEC ('0') / CHINGTHUC ('1') / THUVIEC ('2') / CTVIEN ('3').
///   NGHIVIEC -> FlagActive='0' (đã nghỉ việc); các trạng thái khác -> FlagActive='1'.
/// - Quy tắc nghiệp vụ (giữ nguyên từ legacy Mst_SalesMan_UpdateStatusX):
///   + CHINGTHUC chỉ chuyển về CHINGTHUC hoặc NGHIVIEC.
///   + THUVIEC / CTVIEN chỉ chuyển về chính nó, CHINGTHUC hoặc NGHIVIEC.
///   + Khi NGHIVIEC bắt buộc có SMReason, SMDesc, SMEndDate; SMEndDate phải > SMStartDate.
///   + Ngày bắt đầu làm lại (SMStartDate) phải LỚN HƠN ngày nghỉ việc gần nhất (Mst_SalesManHistoryInactive).
///   + Nhân viên đang vi phạm chế tài Tạm thời (TT) còn hiệu lực hoặc Vĩnh viễn (VV) thì không được chuyển trạng thái.
/// - Khi chuyển sang NGHIVIEC, ghi 1 dòng snapshot vào Mst_SalesManHistoryInactive.
/// </summary>
public sealed class SalesManService(AppDbContext db, ITenantContext tenant) : ISalesManService
{
    private Guid Org => tenant.OrgId;

    public async Task<List<SalesManDto>> SearchAsync(string? smCode, string? dealerCode, string? smStatus, string? smType, string? flagActive)
    {
        var q = db.SalesMen.AsNoTracking().Where(x => x.OrgId == Org);
        if (!string.IsNullOrWhiteSpace(smCode))
        {
            var sc = smCode.Trim();
            q = q.Where(x => x.SMCode == sc);
        }
        if (!string.IsNullOrWhiteSpace(dealerCode))
        {
            var dc = dealerCode.Trim();
            q = q.Where(x => x.DealerCode == dc);
        }
        if (!string.IsNullOrWhiteSpace(smStatus))
        {
            var st = ParseStatus(smStatus);
            q = q.Where(x => x.SMStatus == st);
        }
        if (!string.IsNullOrWhiteSpace(smType))
        {
            var tp = ParseType(smType);
            q = q.Where(x => x.SMType == tp);
        }
        if (!string.IsNullOrWhiteSpace(flagActive))
        {
            var fa = flagActive.Trim();
            q = q.Where(x => x.FlagActive == fa);
        }

        var list = await q.OrderBy(x => x.DealerCode).ThenBy(x => x.SMCode).ToListAsync();
        return list.Select(ToDto).ToList();
    }

    public async Task<SalesManDto?> GetAsync(string smCode)
    {
        if (string.IsNullOrWhiteSpace(smCode)) return null;
        var sc = smCode.Trim();
        var e = await db.SalesMen.AsNoTracking().FirstOrDefaultAsync(x => x.OrgId == Org && x.SMCode == sc);
        return e is null ? null : ToDto(e);
    }

    public async Task<SalesManDto> CreateAsync(CreateSalesManDto dto)
    {
        var entity = await BuildNewAsync(dto);
        db.SalesMen.Add(entity);
        await db.SaveChangesAsync();
        return ToDto(entity);
    }

    public async Task<List<SalesManDto>> CreateMultiAsync(List<CreateSalesManDto> rows)
    {
        if (rows is null || rows.Count < 1)
            throw new InvalidOperationException("Danh sách nhân viên bán hàng rỗng, không có dòng dữ liệu nào.");

        // Kiểm tra trùng khóa SMCode giữa các dòng trong danh sách nhập.
        var dup = rows.GroupBy(r => (r.SMCode ?? "").Trim())
                      .FirstOrDefault(g => g.Count() > 1);
        if (dup is not null)
            throw new InvalidOperationException($"Mã nhân viên bán hàng '{dup.Key}' bị trùng lặp trong danh sách nhập.");

        var created = new List<SalesMan>();
        foreach (var r in rows)
        {
            var entity = await BuildNewAsync(r);
            db.SalesMen.Add(entity);
            created.Add(entity);
        }
        await db.SaveChangesAsync();
        return created.Select(ToDto).ToList();
    }

    public async Task<SalesManDto?> UpdateAsync(string smCode, UpdateSalesManDto dto)
    {
        if (string.IsNullOrWhiteSpace(smCode)) return null;
        var sc = smCode.Trim();
        var entity = await db.SalesMen.FirstOrDefaultAsync(x => x.OrgId == Org && x.SMCode == sc);
        if (entity is null) return null;

        var smName = (dto.SMName ?? "").Trim();
        if (smName.Length < 1)
            throw new InvalidOperationException("Họ tên nhân viên (SMName) không được để trống.");
        var gender = NormalizeGender(dto.SMGender);
        if (dto.SMDateOfBirth is null)
            throw new InvalidOperationException("Ngày sinh (SMDateOfBirth) không được để trống.");
        if (string.IsNullOrWhiteSpace(dto.SMPhoneNo))
            throw new InvalidOperationException("Số điện thoại (SMPhoneNo) không được để trống.");
        if (string.IsNullOrWhiteSpace(dto.SMAddress))
            throw new InvalidOperationException("Địa chỉ (SMAddress) không được để trống.");
        if (string.IsNullOrWhiteSpace(dto.SMSpecialized))
            throw new InvalidOperationException("Chuyên môn (SMSpecialized) không được để trống.");
        if (dto.SMStartDate is null)
            throw new InvalidOperationException("Ngày bắt đầu làm việc (SMStartDate) không được để trống.");
        if (string.IsNullOrWhiteSpace(dto.SMPosition))
            throw new InvalidOperationException("Chức vụ (SMPosition) không được để trống.");
        if (string.IsNullOrWhiteSpace(dto.SMPostionCode))
            throw new InvalidOperationException("Mã vị trí (SMPostionCode) không được để trống.");
        if (string.IsNullOrWhiteSpace(dto.SMHyundaiCode))
            throw new InvalidOperationException("Mã nhân viên Hyundai (SMHyundaiCode) không được để trống.");
        if (string.IsNullOrWhiteSpace(dto.IdentityCardNo))
            throw new InvalidOperationException("Số CMND/CCCD (IdentityCardNo) không được để trống.");

        // Ràng buộc theo loại nhân viên (TVBH / GVDTNB bắt buộc có điểm bán hàng; TVBH bắt buộc có link mạng xã hội).
        await ValidateTypeRulesAsync(dto.SMType, entity.DealerCode, dto.DealerSaleCode,
            dto.WebsiteLink, dto.FacebookLink, dto.FanpageLink, dto.GroupLink, dto.ZaloLink);

        if (dto.SMEndDate is not null && dto.SMStartDate.Value.Date > dto.SMEndDate.Value.Date)
            throw new InvalidOperationException("Ngày bắt đầu làm việc (SMStartDate) phải nhỏ hơn hoặc bằng ngày kết thúc (SMEndDate).");

        var user = string.IsNullOrWhiteSpace(dto.ActionBy) ? "CHUYEN_VIEN_NPP" : dto.ActionBy.Trim();
        entity.SMName = smName;
        entity.SMGender = gender;
        entity.SMDateOfBirth = dto.SMDateOfBirth;
        entity.SMPhoneNo = dto.SMPhoneNo?.Trim();
        entity.SMEmail = dto.SMEmail?.Trim();
        entity.SMAddress = dto.SMAddress?.Trim();
        entity.ProvinceCode = dto.ProvinceCode?.Trim();
        entity.QualificationCode = dto.QualificationCode?.Trim();
        entity.SMSpecialized = dto.SMSpecialized?.Trim();
        entity.SMYearExperence = dto.SMYearExperence?.Trim();
        entity.SMStartDate = dto.SMStartDate;
        entity.SMEndDate = dto.SMEndDate;
        entity.DepartmentCode = dto.DepartmentCode?.Trim();
        entity.SMPosition = dto.SMPosition?.Trim();
        entity.SMPostionCode = dto.SMPostionCode?.Trim();
        entity.SMType = dto.SMType;
        entity.CertificateCode = dto.CertificateCode?.Trim();
        entity.SMHyundaiCode = dto.SMHyundaiCode?.Trim();
        entity.IdentityCardNo = dto.IdentityCardNo?.Trim();
        entity.WebsiteLink = dto.WebsiteLink?.Trim();
        entity.FacebookLink = dto.FacebookLink?.Trim();
        entity.FanpageLink = dto.FanpageLink?.Trim();
        entity.GroupLink = dto.GroupLink?.Trim();
        entity.ZaloLink = dto.ZaloLink?.Trim();
        entity.DealerSaleCode = dto.DealerSaleCode?.Trim();
        entity.UpdateBy = user;
        entity.UpdateDTime = DateTime.Now;
        entity.LogLUDateTime = DateTime.Now;
        entity.LogLUBy = user;

        await db.SaveChangesAsync();
        return ToDto(entity);
    }

    public async Task<SalesManDto?> UpdateStatusAsync(string smCode, UpdateSalesManStatusDto dto)
    {
        if (string.IsNullOrWhiteSpace(smCode)) return null;
        var sc = smCode.Trim();
        var entity = await db.SalesMen.FirstOrDefaultAsync(x => x.OrgId == Org && x.SMCode == sc);
        if (entity is null) return null;

        var cur = entity.SMStatus;
        var target = dto.SMStatus;

        // Quy tắc chuyển trạng thái (Mst_SalesMan_UpdateStatusX).
        if (cur == SalesManStatus.Official && target != SalesManStatus.Official && target != SalesManStatus.Resigned)
            throw new InvalidOperationException("Nhân viên đang ở trạng thái Chính thức (CHINGTHUC) chỉ có thể chuyển về Chính thức hoặc Nghỉ việc (NGHIVIEC).");
        if ((cur == SalesManStatus.Probation || cur == SalesManStatus.Collaborator)
            && target != cur && target != SalesManStatus.Official && target != SalesManStatus.Resigned)
            throw new InvalidOperationException("Nhân viên Thử việc (THUVIEC) / Cộng tác viên (CTVIEN) chỉ có thể chuyển về chính nó, Chính thức (CHINGTHUC) hoặc Nghỉ việc (NGHIVIEC).");

        var start = dto.SMStartDate ?? entity.SMStartDate;
        var end = dto.SMEndDate;

        // Ngày bắt đầu làm lại phải lớn hơn ngày nghỉ việc gần nhất (Mst_SalesManHistoryInactive).
        if (start is not null && !string.IsNullOrWhiteSpace(entity.SMHyundaiCode))
        {
            var lastEnd = await db.SalesManHistoryInactives.AsNoTracking()
                .Where(x => x.OrgId == Org && x.SMHyundaiCode == entity.SMHyundaiCode && x.SMEndDate != null)
                .OrderByDescending(x => x.SMEndDate)
                .Select(x => x.SMEndDate)
                .FirstOrDefaultAsync();
            if (lastEnd is not null && start.Value.Date <= lastEnd.Value.Date)
                throw new InvalidOperationException($"Ngày bắt đầu làm việc ({start.Value:yyyy-MM-dd}) phải lớn hơn ngày nghỉ việc gần nhất ({lastEnd.Value:yyyy-MM-dd}).");
        }

        var flagActive = "1";
        if (target == SalesManStatus.Resigned)
        {
            if (string.IsNullOrWhiteSpace(dto.SMReason))
                throw new InvalidOperationException("Khi nhân viên nghỉ việc (NGHIVIEC) bắt buộc phải nhập lý do (SMReason).");
            if (string.IsNullOrWhiteSpace(dto.SMDesc))
                throw new InvalidOperationException("Khi nhân viên nghỉ việc (NGHIVIEC) bắt buộc phải nhập mô tả chi tiết (SMDesc).");
            if (end is null)
                throw new InvalidOperationException("Khi nhân viên nghỉ việc (NGHIVIEC) bắt buộc phải nhập ngày kết thúc (SMEndDate).");
            if (start is not null && start.Value.Date > end.Value.Date)
                throw new InvalidOperationException("Ngày nghỉ việc (SMEndDate) phải lớn hơn hoặc bằng ngày bắt đầu làm việc (SMStartDate).");
            flagActive = "0";
        }
        else if (target == SalesManStatus.Official)
        {
            if (start is null)
                throw new InvalidOperationException("Khi chuyển nhân viên sang Chính thức (CHINGTHUC) bắt buộc phải có ngày bắt đầu (SMStartDate).");
        }

        // Chặn khi nhân viên đang vi phạm chế tài còn hiệu lực (HR_SalesManViolate).
        if (target != SalesManStatus.Resigned && !string.IsNullOrWhiteSpace(entity.SMHyundaiCode))
        {
            var lastViolate = await db.SalesManViolates.AsNoTracking()
                .Where(x => x.OrgId == Org && x.SMHyundaiCode == entity.SMHyundaiCode)
                .OrderByDescending(x => x.ViolateNumber)
                .FirstOrDefaultAsync();
            if (lastViolate is not null)
            {
                if (lastViolate.ViolateType == ViolateType.Permanent)
                    throw new InvalidOperationException($"Nhân viên '{sc}' đang vi phạm chế tài Vĩnh viễn (VV), không thể chuyển trạng thái.");
                if (lastViolate.ViolateDateEnd is not null && lastViolate.ViolateDateEnd.Value.Date >= DateTime.Today)
                    throw new InvalidOperationException($"Nhân viên '{sc}' đang vi phạm chế tài Tạm thời (TT) còn hiệu lực đến {lastViolate.ViolateDateEnd.Value:yyyy-MM-dd}, không thể chuyển trạng thái.");
            }
        }

        var user = string.IsNullOrWhiteSpace(dto.ActionBy) ? "CHUYEN_VIEN_NPP" : dto.ActionBy.Trim();
        var wasActive = entity.FlagActive == "1";

        entity.SMStatus = target;
        entity.FlagActive = flagActive;
        entity.UpdateStatusBy = user;
        entity.UpdateStatusDtime = DateTime.Now;
        entity.LogLUDateTime = DateTime.Now;
        entity.LogLUBy = user;

        if (flagActive == "0")
        {
            entity.SMReason = dto.SMReason?.Trim();
            entity.SMDesc = dto.SMDesc?.Trim();
            entity.SMEndDate = end;
        }
        else if (cur == SalesManStatus.Resigned)
        {
            // Quay lại làm việc từ NGHIVIEC: xóa lý do nghỉ việc, cập nhật ngày bắt đầu, bỏ ngày kết thúc.
            entity.SMReason = null;
            entity.SMDesc = null;
            entity.SMStartDate = start;
            entity.SMEndDate = null;
        }
        else
        {
            entity.SMEndDate = end;
        }

        // Ghi lịch sử nghỉ việc khi chuyển từ active sang inactive (Mst_SalesManHistoryInactive).
        if (flagActive == "0" && wasActive)
        {
            db.SalesManHistoryInactives.Add(new SalesManHistoryInactive
            {
                OrgId = Org,
                SMCode = entity.SMCode,
                SMHyundaiCode = entity.SMHyundaiCode,
                DealerCode = entity.DealerCode,
                SMStatus = target,
                IdentityCardNo = entity.IdentityCardNo,
                SMFlagActive = "1",
                SMStartDate = entity.SMStartDate,
                SMEndDate = end,
                SMReason = dto.SMReason?.Trim(),
                SMDesc = dto.SMDesc?.Trim(),
                InactiveDateTime = DateTime.Now,
                InactiveBy = user
            });
        }

        await db.SaveChangesAsync();
        return ToDto(entity);
    }

    public async Task<bool> DeleteAsync(string smCode)
    {
        if (string.IsNullOrWhiteSpace(smCode)) return false;
        var sc = smCode.Trim();
        var entity = await db.SalesMen.FirstOrDefaultAsync(x => x.OrgId == Org && x.SMCode == sc);
        if (entity is null) return false;

        db.SalesMen.Remove(entity);
        await db.SaveChangesAsync();
        return true;
    }

    public async Task<List<SalesManHistoryInactiveDto>> GetHistoryAsync(string? smCode, string? smHyundaiCode)
    {
        var q = db.SalesManHistoryInactives.AsNoTracking().Where(x => x.OrgId == Org);
        if (!string.IsNullOrWhiteSpace(smCode))
        {
            var sc = smCode.Trim();
            q = q.Where(x => x.SMCode == sc);
        }
        if (!string.IsNullOrWhiteSpace(smHyundaiCode))
        {
            var hc = smHyundaiCode.Trim();
            q = q.Where(x => x.SMHyundaiCode == hc);
        }
        var list = await q.OrderByDescending(x => x.InactiveDateTime).ToListAsync();
        return list.Select(ToHistoryDto).ToList();
    }

    public async Task<SalesManStatsDto> GetStatsAsync()
    {
        var list = await db.SalesMen.AsNoTracking().Where(x => x.OrgId == Org).ToListAsync();

        return new SalesManStatsDto(
            TotalSalesMen: list.Count,
            TotalActive: list.Count(x => x.FlagActive == "1"),
            TotalInactive: list.Count(x => x.FlagActive != "1"),
            TotalOfficial: list.Count(x => x.SMStatus == SalesManStatus.Official),
            TotalProbation: list.Count(x => x.SMStatus == SalesManStatus.Probation),
            TotalCollaborator: list.Count(x => x.SMStatus == SalesManStatus.Collaborator),
            TotalResigned: list.Count(x => x.SMStatus == SalesManStatus.Resigned),
            TotalDealers: list.Select(x => x.DealerCode).Distinct().Count(),
            ByDealer: list.GroupBy(x => string.IsNullOrWhiteSpace(x.DealerCode) ? "UNKNOWN" : x.DealerCode)
                          .ToDictionary(g => g.Key, g => g.Count()),
            ByStatus: list.GroupBy(x => StatusName(x.SMStatus))
                          .ToDictionary(g => g.Key, g => g.Count()),
            ByType: list.GroupBy(x => TypeName(x.SMType))
                        .ToDictionary(g => g.Key, g => g.Count())
        );
    }

    // ===== Helpers =====
    private async Task<SalesMan> BuildNewAsync(CreateSalesManDto dto)
    {
        var smCode = (dto.SMCode ?? "").Trim();
        var dealerCode = (dto.DealerCode ?? "").Trim();
        if (smCode.Length < 1)
            throw new InvalidOperationException("Mã nhân viên bán hàng (SMCode) không được để trống.");
        if (dealerCode.Length < 1)
            throw new InvalidOperationException("Mã đại lý (DealerCode) không được để trống.");

        // SMCode là khóa nghiệp vụ duy nhất (Mst_SalesMan_CheckDB_ForCreate).
        if (await db.SalesMen.AnyAsync(x => x.OrgId == Org && x.SMCode == smCode))
            throw new InvalidOperationException($"Nhân viên bán hàng '{smCode}' đã tồn tại.");

        // FK: Đại lý phải tồn tại (myCommon_CheckDealer).
        await CheckDealerExistsAsync(dealerCode);

        var smName = (dto.SMName ?? "").Trim();
        if (smName.Length < 1)
            throw new InvalidOperationException("Họ tên nhân viên (SMName) không được để trống.");
        var gender = NormalizeGender(dto.SMGender);
        if (dto.SMDateOfBirth is null)
            throw new InvalidOperationException("Ngày sinh (SMDateOfBirth) không được để trống.");
        if (string.IsNullOrWhiteSpace(dto.SMPhoneNo))
            throw new InvalidOperationException("Số điện thoại (SMPhoneNo) không được để trống.");
        if (string.IsNullOrWhiteSpace(dto.SMAddress))
            throw new InvalidOperationException("Địa chỉ (SMAddress) không được để trống.");
        if (string.IsNullOrWhiteSpace(dto.SMSpecialized))
            throw new InvalidOperationException("Chuyên môn (SMSpecialized) không được để trống.");
        if (dto.SMStartDate is null)
            throw new InvalidOperationException("Ngày bắt đầu làm việc (SMStartDate) không được để trống.");
        if (string.IsNullOrWhiteSpace(dto.SMPosition))
            throw new InvalidOperationException("Chức vụ (SMPosition) không được để trống.");
        if (string.IsNullOrWhiteSpace(dto.SMPostionCode))
            throw new InvalidOperationException("Mã vị trí (SMPostionCode) không được để trống.");
        if (string.IsNullOrWhiteSpace(dto.SMHyundaiCode))
            throw new InvalidOperationException("Mã nhân viên Hyundai (SMHyundaiCode) không được để trống.");
        if (string.IsNullOrWhiteSpace(dto.IdentityCardNo))
            throw new InvalidOperationException("Số CMND/CCCD (IdentityCardNo) không được để trống.");

        // Trạng thái khi tạo chỉ nhận CHINGTHUC / THUVIEC / CTVIEN (không tạo mới ở trạng thái NGHIVIEC).
        if (dto.SMStatus == SalesManStatus.Resigned)
            throw new InvalidOperationException("Không thể tạo mới nhân viên ở trạng thái Nghỉ việc (NGHIVIEC).");

        // Ràng buộc theo loại nhân viên.
        await ValidateTypeRulesAsync(dto.SMType, dealerCode, dto.DealerSaleCode,
            dto.WebsiteLink, dto.FacebookLink, dto.FanpageLink, dto.GroupLink, dto.ZaloLink);

        if (dto.SMEndDate is not null && dto.SMStartDate.Value.Date > dto.SMEndDate.Value.Date)
            throw new InvalidOperationException("Ngày bắt đầu làm việc (SMStartDate) phải nhỏ hơn hoặc bằng ngày kết thúc (SMEndDate).");

        var user = string.IsNullOrWhiteSpace(dto.ActionBy) ? "CHUYEN_VIEN_NPP" : dto.ActionBy.Trim();
        return new SalesMan
        {
            OrgId = Org,
            SMCode = smCode,
            SMHyundaiCode = dto.SMHyundaiCode?.Trim(),
            DealerCode = dealerCode,
            DealerName = await GetDealerNameAsync(dealerCode),
            SMName = smName,
            SMGender = gender,
            SMDateOfBirth = dto.SMDateOfBirth,
            SMPhoneNo = dto.SMPhoneNo?.Trim(),
            SMEmail = dto.SMEmail?.Trim(),
            SMAddress = dto.SMAddress?.Trim(),
            ProvinceCode = dto.ProvinceCode?.Trim(),
            QualificationCode = dto.QualificationCode?.Trim(),
            SMSpecialized = dto.SMSpecialized?.Trim(),
            SMYearExperence = dto.SMYearExperence?.Trim(),
            SMStartDate = dto.SMStartDate,
            SMEndDate = dto.SMEndDate,
            DepartmentCode = dto.DepartmentCode?.Trim(),
            SMPosition = dto.SMPosition?.Trim(),
            SMPostionCode = dto.SMPostionCode?.Trim(),
            SMType = dto.SMType,
            CertificateCode = dto.CertificateCode?.Trim(),
            SMStatus = dto.SMStatus,
            IdentityCardNo = dto.IdentityCardNo?.Trim(),
            WebsiteLink = dto.WebsiteLink?.Trim(),
            FacebookLink = dto.FacebookLink?.Trim(),
            FanpageLink = dto.FanpageLink?.Trim(),
            GroupLink = dto.GroupLink?.Trim(),
            ZaloLink = dto.ZaloLink?.Trim(),
            DealerSaleCode = dto.DealerSaleCode?.Trim(),
            FlagActive = "1",
            CreatedBy = user,
            CreatedAt = DateTime.Now,
            LogLUDateTime = DateTime.Now,
            LogLUBy = user
        };
    }

    /// <summary>Ràng buộc theo loại nhân viên (Mst_SalesMan_CreateMultiX): TVBH bắt buộc link mạng xã hội + điểm bán hàng; GVDTNB bắt buộc điểm bán hàng.</summary>
    private async Task ValidateTypeRulesAsync(SalesManType type, string dealerCode, string? dealerSaleCode,
        string? website, string? facebook, string? fanpage, string? group, string? zalo)
    {
        if (type == SalesManType.SalesConsultant)
        {
            if (string.IsNullOrWhiteSpace(website))
                throw new InvalidOperationException("Tư vấn bán hàng (TVBH) bắt buộc phải có link Website (WebsiteLink).");
            if (string.IsNullOrWhiteSpace(facebook))
                throw new InvalidOperationException("Tư vấn bán hàng (TVBH) bắt buộc phải có link Facebook (FacebookLink).");
            if (string.IsNullOrWhiteSpace(fanpage))
                throw new InvalidOperationException("Tư vấn bán hàng (TVBH) bắt buộc phải có link Fanpage (FanpageLink).");
            if (string.IsNullOrWhiteSpace(group))
                throw new InvalidOperationException("Tư vấn bán hàng (TVBH) bắt buộc phải có link Group (GroupLink).");
            if (string.IsNullOrWhiteSpace(zalo))
                throw new InvalidOperationException("Tư vấn bán hàng (TVBH) bắt buộc phải có link Zalo (ZaloLink).");
        }

        if (type == SalesManType.SalesConsultant || type == SalesManType.Trainer)
        {
            if (string.IsNullOrWhiteSpace(dealerSaleCode))
                throw new InvalidOperationException("Mã điểm bán hàng (DealerSaleCode) không được để trống với loại nhân viên này.");
        }
    }

    private static string NormalizeGender(string? gender)
        => string.IsNullOrWhiteSpace(gender) ? "M" : (gender.Trim().Equals("F", StringComparison.OrdinalIgnoreCase) ? "F" : "M");

    private static string NormalizeFlag(string? flag)
        => string.IsNullOrWhiteSpace(flag) ? "1" : (flag.Trim() == "0" ? "0" : "1");

    private static SalesManStatus ParseStatus(string value)
    {
        var v = value.Trim().ToUpperInvariant();
        return v switch
        {
            "0" or "NGHIVIEC" => SalesManStatus.Resigned,
            "1" or "CHINGTHUC" => SalesManStatus.Official,
            "3" or "CTVIEN" => SalesManStatus.Collaborator,
            _ => SalesManStatus.Probation
        };
    }

    private static SalesManType ParseType(string value)
    {
        var v = value.Trim().ToUpperInvariant();
        return v switch
        {
            "GD" or "0" => SalesManType.Director,
            "TPBH" or "1" => SalesManType.SalesManager,
            "TPDV" or "2" => SalesManType.ServiceManager,
            "GVDTNB" or "4" => SalesManType.Trainer,
            _ => SalesManType.SalesConsultant
        };
    }

    private static string StatusName(SalesManStatus s) => s switch
    {
        SalesManStatus.Resigned => "Nghỉ việc",
        SalesManStatus.Official => "Chính thức",
        SalesManStatus.Probation => "Thử việc",
        SalesManStatus.Collaborator => "Cộng tác viên",
        _ => "Không xác định"
    };

    private static string TypeName(SalesManType t) => t switch
    {
        SalesManType.Director => "Giám đốc",
        SalesManType.SalesManager => "Trưởng phòng bán hàng",
        SalesManType.ServiceManager => "Trưởng phòng dịch vụ",
        SalesManType.SalesConsultant => "Tư vấn bán hàng",
        SalesManType.Trainer => "Giảng viên đào tạo nội bộ",
        _ => "Không xác định"
    };

    /// <summary>Kiểm tra đại lý tồn tại trong danh mục đại lý (myCommon_CheckDealer).</summary>
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

    private static SalesManDto ToDto(SalesMan e) => new(
        e.Id, e.SMCode, e.SMHyundaiCode, e.DealerCode, e.DealerName, e.SMName, e.SMGender, e.SMDateOfBirth,
        e.SMPhoneNo, e.SMEmail, e.SMAddress, e.ProvinceCode, e.QualificationCode, e.SMSpecialized, e.SMYearExperence,
        e.SMStartDate, e.SMEndDate, e.DepartmentCode, e.SMPosition, e.SMPostionCode,
        e.SMType, TypeName(e.SMType), e.CertificateCode,
        e.SMStatus, StatusName(e.SMStatus), e.SMReason, e.SMDesc, e.IdentityCardNo,
        e.WebsiteLink, e.FacebookLink, e.FanpageLink, e.GroupLink, e.ZaloLink, e.DealerSaleCode,
        e.FlagActive, e.FlagActive == "1",
        e.CreatedBy, e.CreatedAt, e.UpdateBy, e.UpdateDTime, e.UpdateStatusBy, e.UpdateStatusDtime,
        e.LogLUDateTime, e.LogLUBy);

    private static SalesManHistoryInactiveDto ToHistoryDto(SalesManHistoryInactive e) => new(
        e.Id, e.SMCode, e.SMHyundaiCode, e.DealerCode, e.SMStatus, StatusName(e.SMStatus), e.IdentityCardNo,
        e.SMFlagActive, e.SMStartDate, e.SMEndDate, e.SMReason, e.SMDesc, e.InactiveDateTime, e.InactiveBy);
}