using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using MiniSales.Data;
using MiniSales.Models;

namespace MiniSales.Services;

// ===== DTOs =====
public record CreateDealerCustomerDto(
    string? CustomerCode = null,
    string DealerCode = "",
    string? DealerName = null,
    string FullName = "",
    string? FullNameEN = null,
    DealerCustomerGender Gender = DealerCustomerGender.Male,
    DateTime? DateOfBirth = null,
    string? IDCardType = null,
    string? IDCardNo = null,
    string PhoneNo = "",
    string? Email = null,
    string? Address = null,
    string? ProvinceCode = null,
    string? ProvinceName = null,
    string? DistrictCode = null,
    string? DistrictName = null,
    string CustomerBaseCode = "",
    string? CustomerBaseName = null,
    DealerCustomerType CustomerType = DealerCustomerType.Personal,
    string? TaxCode = null,
    string? RepresentName = null,
    string? Position = null,
    string? CusAccountBank = null,
    string? CreatedBy = null
);

public record UpdateDealerCustomerDto(
    string? FullName = null,
    string? FullNameEN = null,
    DealerCustomerGender? Gender = null,
    DateTime? DateOfBirth = null,
    string? IDCardType = null,
    string? IDCardNo = null,
    string? PhoneNo = null,
    string? Email = null,
    string? Address = null,
    string? ProvinceCode = null,
    string? ProvinceName = null,
    string? DistrictCode = null,
    string? DistrictName = null,
    string? CustomerBaseCode = null,
    string? CustomerBaseName = null,
    DealerCustomerType? CustomerType = null,
    string? TaxCode = null,
    string? RepresentName = null,
    string? Position = null,
    string? CusAccountBank = null,
    string? UpdatedBy = null
);

public record DealerCustomerDto(
    long Id,
    string CustomerCode,
    string DealerCode,
    string? DealerName,
    string FullName,
    string? FullNameEN,
    string Gender,
    string GenderText,
    DateTime? DateOfBirth,
    string? IDCardType,
    string? IDCardNo,
    string PhoneNo,
    string? Email,
    string? Address,
    string? ProvinceCode,
    string? ProvinceName,
    string? DistrictCode,
    string? DistrictName,
    string CustomerBaseCode,
    string? CustomerBaseName,
    string CustomerType,
    string CustomerTypeText,
    string? TaxCode,
    string? RepresentName,
    string? Position,
    string? CusAccountBank,
    DateTime CreatedAt,
    string? CreatedBy,
    DateTime? UpdatedAt,
    string? UpdatedBy
);

public record DealerCustomerStatsDto(
    int TotalCustomers,
    int TotalPersonal,
    int TotalBusiness,
    int TotalWithEmail,
    int TotalWithTaxCode,
    Dictionary<string, int> ByDealer,
    Dictionary<string, int> ByProvince,
    Dictionary<string, int> ByCustomerBase,
    Dictionary<string, int> ByGender
);

public interface IDealerCustomerService
{
    Task<List<DealerCustomerDto>> SearchAsync(
        string? customerCode,
        string? dealerCode,
        string? fullName,
        string? phoneNo,
        string? idCardNo,
        DateTime? createdFrom,
        DateTime? createdTo);
    Task<DealerCustomerDto?> GetByCodeAsync(string customerCode);
    Task<List<DealerCustomerDto>> GetAllByDealerAsync(string dealerCode);
    Task<string> GetSeqAsync();
    Task<DealerCustomerDto> CreateAsync(CreateDealerCustomerDto dto);
    Task<DealerCustomerDto> UpdateAsync(string customerCode, UpdateDealerCustomerDto dto);
    Task<DealerCustomerDto> UpdateAdminAsync(string customerCode, UpdateDealerCustomerDto dto);
    Task<bool> DeleteAsync(string customerCode);
    Task<DealerCustomerStatsDto> GetStatsAsync();
}

/// <summary>
/// Quản lý Khách hàng Đại lý (DMS.Sales DLS_DealerCustomer / DLSDealerCustomerController / DealerSales.cs):
/// Đại lý lập hồ sơ khách hàng mua xe (cá nhân/doanh nghiệp), sinh mã {yyMM}CTM{seq:D5},
/// kiểm tra ràng buộc bắt buộc (FullName, Gender, PhoneNo, Address, ProvinceCode, DistrictCode, CustomerBaseCode),
/// số giấy tờ tùy thân chỉ chứa [a-zA-Z0-9] và không trùng theo đại lý, email đúng định dạng,
/// tra cứu phân trang theo mã/tên/điện thoại/CCCD/ngày tạo, cập nhật (đại lý) và cập nhật admin (HQ),
/// xóa khách hàng và thống kê theo đại lý, tỉnh/thành, nguồn khách hàng và giới tính.
/// </summary>
public sealed class DealerCustomerService(AppDbContext db, ITenantContext tenant) : IDealerCustomerService
{
    private Guid Org => tenant.OrgId;

    private static readonly Regex IdCardRegex = new("^[a-zA-Z0-9]+$", RegexOptions.Compiled);
    private static readonly Regex EmailRegex = new(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.Compiled);

    public async Task<List<DealerCustomerDto>> SearchAsync(
        string? customerCode,
        string? dealerCode,
        string? fullName,
        string? phoneNo,
        string? idCardNo,
        DateTime? createdFrom,
        DateTime? createdTo)
    {
        var q = db.DealerCustomers.Where(x => x.OrgId == Org);

        if (!string.IsNullOrWhiteSpace(customerCode))
        {
            var v = customerCode.Trim();
            q = q.Where(x => x.CustomerCode.Contains(v));
        }
        if (!string.IsNullOrWhiteSpace(dealerCode))
        {
            var v = dealerCode.Trim();
            q = q.Where(x => x.DealerCode == v);
        }
        if (!string.IsNullOrWhiteSpace(fullName))
        {
            var v = fullName.Trim();
            q = q.Where(x => x.FullName.Contains(v));
        }
        if (!string.IsNullOrWhiteSpace(phoneNo))
        {
            var v = phoneNo.Trim();
            q = q.Where(x => x.PhoneNo.Contains(v));
        }
        if (!string.IsNullOrWhiteSpace(idCardNo))
        {
            var v = idCardNo.Trim();
            q = q.Where(x => x.IDCardNo != null && x.IDCardNo.Contains(v));
        }
        if (createdFrom.HasValue)
            q = q.Where(x => x.CreatedAt >= createdFrom.Value.Date);
        if (createdTo.HasValue)
            q = q.Where(x => x.CreatedAt < createdTo.Value.Date.AddDays(1));

        var list = await q.OrderByDescending(x => x.CreatedAt).ToListAsync();
        return list.Select(ToDto).ToList();
    }

    public async Task<DealerCustomerDto?> GetByCodeAsync(string customerCode)
    {
        var item = await db.DealerCustomers
            .FirstOrDefaultAsync(x => x.OrgId == Org && x.CustomerCode == customerCode.Trim());
        return item is null ? null : ToDto(item);
    }

    public async Task<List<DealerCustomerDto>> GetAllByDealerAsync(string dealerCode)
    {
        var list = await db.DealerCustomers
            .Where(x => x.OrgId == Org && x.DealerCode == dealerCode.Trim())
            .OrderBy(x => x.FullName)
            .ToListAsync();
        return list.Select(ToDto).ToList();
    }

    public async Task<string> GetSeqAsync() => await GenerateSeqNoAsync();

    public async Task<DealerCustomerDto> CreateAsync(CreateDealerCustomerDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.DealerCode))
            throw new InvalidOperationException("Mã đại lý (DealerCode) không được để trống.");

        string dealerCode = dto.DealerCode.Trim();

        // Sinh mã khách hàng nếu để trống (SequenceType = "CTM")
        string customerCode = string.IsNullOrWhiteSpace(dto.CustomerCode)
            ? await GenerateSeqNoAsync()
            : dto.CustomerCode.Trim();

        if (customerCode.Length < 5)
            throw new InvalidOperationException("Mã khách hàng (CustomerCode) không hợp lệ (độ dài tối thiểu 5 ký tự).");

        if (await db.DealerCustomers.AnyAsync(x => x.OrgId == Org && x.CustomerCode == customerCode))
            throw new InvalidOperationException($"Mã khách hàng '{customerCode}' đã tồn tại trên hệ thống.");

        ValidateRequired(dto.FullName, dto.PhoneNo, dto.Address, dto.ProvinceCode, dto.DistrictCode, dto.CustomerBaseCode);
        ValidateIdCard(dto.IDCardNo);
        ValidateEmail(dto.Email);

        // Số giấy tờ tùy thân không được trùng trong cùng đại lý (DMS.Sales DLS_DealerCustomer_Create_ExistIDCardNo)
        if (!string.IsNullOrWhiteSpace(dto.IDCardNo))
        {
            string idNo = dto.IDCardNo.Trim();
            bool dup = await db.DealerCustomers
                .AnyAsync(x => x.OrgId == Org && x.DealerCode == dealerCode && x.IDCardNo == idNo);
            if (dup)
                throw new InvalidOperationException($"Số giấy tờ tùy thân '{idNo}' đã tồn tại cho đại lý '{dealerCode}'.");
        }

        var entity = new DealerCustomer
        {
            OrgId = Org,
            CustomerCode = customerCode,
            DealerCode = dealerCode,
            DealerName = dto.DealerName?.Trim(),
            FullName = dto.FullName.Trim(),
            FullNameEN = dto.FullNameEN?.Trim(),
            Gender = dto.Gender,
            DateOfBirth = dto.DateOfBirth,
            IDCardType = dto.IDCardType?.Trim(),
            IDCardNo = dto.IDCardNo?.Trim(),
            PhoneNo = dto.PhoneNo.Trim(),
            Email = dto.Email?.Trim(),
            Address = dto.Address?.Trim(),
            ProvinceCode = dto.ProvinceCode?.Trim(),
            ProvinceName = dto.ProvinceName?.Trim(),
            DistrictCode = dto.DistrictCode?.Trim(),
            DistrictName = dto.DistrictName?.Trim(),
            CustomerBaseCode = dto.CustomerBaseCode.Trim(),
            CustomerBaseName = dto.CustomerBaseName?.Trim(),
            CustomerType = dto.CustomerType,
            TaxCode = dto.TaxCode?.Trim(),
            RepresentName = dto.RepresentName?.Trim(),
            Position = dto.Position?.Trim(),
            CusAccountBank = dto.CusAccountBank?.Trim(),
            CreatedAt = DateTime.Now,
            CreatedBy = dto.CreatedBy?.Trim() ?? "DEALER_USER"
        };

        db.DealerCustomers.Add(entity);
        await db.SaveChangesAsync();
        return ToDto(entity);
    }

    public async Task<DealerCustomerDto> UpdateAsync(string customerCode, UpdateDealerCustomerDto dto)
    {
        var item = await db.DealerCustomers
            .FirstOrDefaultAsync(x => x.OrgId == Org && x.CustomerCode == customerCode.Trim())
            ?? throw new InvalidOperationException($"Không tìm thấy khách hàng '{customerCode}'.");

        // Không cho đổi DealerCode khi cập nhật (DMS.Sales DLS_DealerCustomer_Update)
        ApplyUpdate(item, dto, isAdmin: false);
        await db.SaveChangesAsync();
        return ToDto(item);
    }

    public async Task<DealerCustomerDto> UpdateAdminAsync(string customerCode, UpdateDealerCustomerDto dto)
    {
        var item = await db.DealerCustomers
            .FirstOrDefaultAsync(x => x.OrgId == Org && x.CustomerCode == customerCode.Trim())
            ?? throw new InvalidOperationException($"Không tìm thấy khách hàng '{customerCode}'.");

        // UpdateAdmin: HQ/admin cập nhật (DMS.Sales DLS_DealerCustomer_UpdateAdmin_New20260602)
        ApplyUpdate(item, dto, isAdmin: true);
        await db.SaveChangesAsync();
        return ToDto(item);
    }

    public async Task<bool> DeleteAsync(string customerCode)
    {
        if (string.IsNullOrWhiteSpace(customerCode))
            throw new InvalidOperationException("Mã khách hàng (CustomerCode) không được để trống.");

        var item = await db.DealerCustomers
            .FirstOrDefaultAsync(x => x.OrgId == Org && x.CustomerCode == customerCode.Trim())
            ?? throw new InvalidOperationException($"Không tìm thấy khách hàng '{customerCode}'.");

        db.DealerCustomers.Remove(item);
        await db.SaveChangesAsync();
        return true;
    }

    public async Task<DealerCustomerStatsDto> GetStatsAsync()
    {
        var list = await db.DealerCustomers.Where(x => x.OrgId == Org).ToListAsync();

        return new DealerCustomerStatsDto(
            TotalCustomers: list.Count,
            TotalPersonal: list.Count(x => x.CustomerType == DealerCustomerType.Personal),
            TotalBusiness: list.Count(x => x.CustomerType == DealerCustomerType.Business),
            TotalWithEmail: list.Count(x => !string.IsNullOrWhiteSpace(x.Email)),
            TotalWithTaxCode: list.Count(x => !string.IsNullOrWhiteSpace(x.TaxCode)),
            ByDealer: list.GroupBy(x => x.DealerCode).ToDictionary(g => g.Key, g => g.Count()),
            ByProvince: list.GroupBy(x => x.ProvinceCode ?? "OTHER").ToDictionary(g => g.Key, g => g.Count()),
            ByCustomerBase: list.GroupBy(x => x.CustomerBaseCode).ToDictionary(g => g.Key, g => g.Count()),
            ByGender: list.GroupBy(x => x.Gender.ToString()).ToDictionary(g => g.Key, g => g.Count())
        );
    }

    // ===== Helpers =====
    private void ApplyUpdate(DealerCustomer item, UpdateDealerCustomerDto dto, bool isAdmin)
    {
        if (dto.FullName != null)
        {
            if (string.IsNullOrWhiteSpace(dto.FullName))
                throw new InvalidOperationException("Họ tên khách hàng (FullName) không được để trống.");
            item.FullName = dto.FullName.Trim();
        }
        if (dto.FullNameEN != null) item.FullNameEN = dto.FullNameEN.Trim();
        if (dto.Gender.HasValue) item.Gender = dto.Gender.Value;
        if (dto.DateOfBirth.HasValue) item.DateOfBirth = dto.DateOfBirth;
        if (dto.IDCardType != null) item.IDCardType = dto.IDCardType.Trim();
        if (dto.IDCardNo != null)
        {
            ValidateIdCard(dto.IDCardNo);
            item.IDCardNo = dto.IDCardNo.Trim();
        }
        if (dto.PhoneNo != null)
        {
            if (string.IsNullOrWhiteSpace(dto.PhoneNo))
                throw new InvalidOperationException("Số điện thoại (PhoneNo) không được để trống.");
            item.PhoneNo = dto.PhoneNo.Trim();
        }
        if (dto.Email != null)
        {
            ValidateEmail(dto.Email);
            item.Email = dto.Email.Trim();
        }
        if (dto.Address != null) item.Address = dto.Address.Trim();
        if (dto.ProvinceCode != null) item.ProvinceCode = dto.ProvinceCode.Trim();
        if (dto.ProvinceName != null) item.ProvinceName = dto.ProvinceName.Trim();
        if (dto.DistrictCode != null) item.DistrictCode = dto.DistrictCode.Trim();
        if (dto.DistrictName != null) item.DistrictName = dto.DistrictName.Trim();
        if (dto.CustomerBaseCode != null)
        {
            if (string.IsNullOrWhiteSpace(dto.CustomerBaseCode))
                throw new InvalidOperationException("Mã nguồn khách hàng (CustomerBaseCode) không được để trống.");
            item.CustomerBaseCode = dto.CustomerBaseCode.Trim();
        }
        if (dto.CustomerBaseName != null) item.CustomerBaseName = dto.CustomerBaseName.Trim();
        if (dto.CustomerType.HasValue) item.CustomerType = dto.CustomerType.Value;
        if (dto.TaxCode != null) item.TaxCode = dto.TaxCode.Trim();
        if (dto.RepresentName != null) item.RepresentName = dto.RepresentName.Trim();
        if (dto.Position != null) item.Position = dto.Position.Trim();
        if (dto.CusAccountBank != null) item.CusAccountBank = dto.CusAccountBank.Trim();

        item.UpdatedAt = DateTime.Now;
        item.UpdatedBy = dto.UpdatedBy?.Trim() ?? (isAdmin ? "HTC_ADMIN" : "DEALER_USER");
    }

    private static void ValidateRequired(string fullName, string phoneNo, string? address, string? provinceCode, string? districtCode, string customerBaseCode)
    {
        if (string.IsNullOrWhiteSpace(fullName))
            throw new InvalidOperationException("Họ tên khách hàng (FullName) không được để trống.");
        if (string.IsNullOrWhiteSpace(phoneNo))
            throw new InvalidOperationException("Số điện thoại (PhoneNo) không được để trống.");
        if (string.IsNullOrWhiteSpace(address))
            throw new InvalidOperationException("Địa chỉ (Address) không được để trống.");
        if (string.IsNullOrWhiteSpace(provinceCode))
            throw new InvalidOperationException("Mã tỉnh/thành (ProvinceCode) không được để trống.");
        if (string.IsNullOrWhiteSpace(districtCode))
            throw new InvalidOperationException("Mã quận/huyện - xã/phường (DistrictCode) không được để trống.");
        if (string.IsNullOrWhiteSpace(customerBaseCode))
            throw new InvalidOperationException("Mã nguồn khách hàng (CustomerBaseCode) không được để trống.");
    }

    private static void ValidateIdCard(string? idCardNo)
    {
        if (string.IsNullOrWhiteSpace(idCardNo)) return;
        string v = idCardNo.Trim();
        if (v.Length < 5)
            throw new InvalidOperationException("Số giấy tờ tùy thân (IDCardNo) không hợp lệ (độ dài tối thiểu 5 ký tự).");
        if (!IdCardRegex.IsMatch(v))
            throw new InvalidOperationException("Số giấy tờ tùy thân (IDCardNo) chỉ được chứa ký tự chữ và số [a-zA-Z0-9].");
    }

    private static void ValidateEmail(string? email)
    {
        if (string.IsNullOrWhiteSpace(email)) return;
        if (!EmailRegex.IsMatch(email.Trim()))
            throw new InvalidOperationException($"Email '{email}' không đúng định dạng.");
    }

    private async Task<string> GenerateSeqNoAsync()
    {
        string prefix = $"{DateTime.Today:yyMM}CTM";
        var last = await db.DealerCustomers
            .Where(x => x.OrgId == Org && x.CustomerCode.StartsWith(prefix))
            .OrderByDescending(x => x.CustomerCode)
            .Select(x => x.CustomerCode)
            .FirstOrDefaultAsync();

        int nextSeq = 1;
        if (!string.IsNullOrEmpty(last) && last.Length >= prefix.Length + 5)
        {
            if (int.TryParse(last.Substring(prefix.Length, 5), out int s))
                nextSeq = s + 1;
        }

        return $"{prefix}{nextSeq:D5}";
    }

    private static string GenderText(DealerCustomerGender g) => g switch
    {
        DealerCustomerGender.Male => "Nam",
        DealerCustomerGender.Female => "Nữ",
        _ => "Khác"
    };

    private static string CustomerTypeText(DealerCustomerType t) => t switch
    {
        DealerCustomerType.Business => "Doanh nghiệp",
        _ => "Cá nhân"
    };

    private static DealerCustomerDto ToDto(DealerCustomer c) => new(
        c.Id,
        c.CustomerCode,
        c.DealerCode,
        c.DealerName,
        c.FullName,
        c.FullNameEN,
        c.Gender.ToString(),
        GenderText(c.Gender),
        c.DateOfBirth,
        c.IDCardType,
        c.IDCardNo,
        c.PhoneNo,
        c.Email,
        c.Address,
        c.ProvinceCode,
        c.ProvinceName,
        c.DistrictCode,
        c.DistrictName,
        c.CustomerBaseCode,
        c.CustomerBaseName,
        c.CustomerType.ToString(),
        CustomerTypeText(c.CustomerType),
        c.TaxCode,
        c.RepresentName,
        c.Position,
        c.CusAccountBank,
        c.CreatedAt,
        c.CreatedBy,
        c.UpdatedAt,
        c.UpdatedBy
    );
}
