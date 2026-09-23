using Microsoft.EntityFrameworkCore;
using MiniSales.Data;
using MiniSales.Models;

namespace MiniSales.Services;

// ===== DTOs =====
public record CreateTransporterDto(
    string TransporterCode = "",
    string TransporterName = "",
    string TransportContractNo = "",
    string? Address = null,
    string? PhoneNo = null,
    string? FaxNo = null,
    string? DirectorFullName = null,
    string? DirectorPhoneNo = null,
    string? ContactorFullName = null,
    string? ContactorPhoneNo = null,
    string? Remark = null,
    string FlagActive = "1",
    string? ActionBy = null
);

public record UpdateTransporterDto(
    string? TransporterName = null,
    string? TransportContractNo = null,
    string? Address = null,
    string? PhoneNo = null,
    string? FaxNo = null,
    string? DirectorFullName = null,
    string? DirectorPhoneNo = null,
    string? ContactorFullName = null,
    string? ContactorPhoneNo = null,
    string? Remark = null,
    string FlagActive = "1",
    string? ActionBy = null
);

public record TransporterDto(
    long Id,
    string TransporterCode,
    string TransporterName,
    string TransportContractNo,
    string? Address,
    string? PhoneNo,
    string? FaxNo,
    string? DirectorFullName,
    string? DirectorPhoneNo,
    string? ContactorFullName,
    string? ContactorPhoneNo,
    string? Remark,
    string FlagActive,
    bool IsActive,
    DateTime LogLUDateTime,
    string? LogLUBy
);

public record CreateTransporterCarDto(
    string TransporterCode = "",
    string PlateNo = "",
    string FlagActive = "1",
    string? ActionBy = null
);

public record UpdateTransporterCarDto(
    string FlagActive = "1",
    string? ActionBy = null
);

public record TransporterCarDto(
    long Id,
    string TransporterCode,
    string? TransporterName,
    string PlateNo,
    string FlagActive,
    bool IsActive,
    DateTime LogLUDateTime,
    string? LogLUBy
);

public record CreateTransporterDriverDto(
    string TransporterCode = "",
    string DriverId = "",
    string DriverFullName = "",
    string DriverLicenseNo = "",
    string? DriverPhoneNo = null,
    string FlagActive = "1",
    string? ActionBy = null
);

public record UpdateTransporterDriverDto(
    string? DriverFullName = null,
    string? DriverLicenseNo = null,
    string? DriverPhoneNo = null,
    string FlagActive = "1",
    string? ActionBy = null
);

public record TransporterDriverDto(
    long Id,
    string TransporterCode,
    string? TransporterName,
    string DriverId,
    string DriverFullName,
    string DriverLicenseNo,
    string? DriverPhoneNo,
    string FlagActive,
    bool IsActive,
    DateTime LogLUDateTime,
    string? LogLUBy
);

public record TransporterImportRowDto(
    string? TransporterCode,
    string? TransporterName,
    string? TransportContractNo,
    string? Address,
    string? PhoneNo,
    string? FaxNo,
    string? DirectorFullName,
    string? DirectorPhoneNo,
    string? ContactorFullName,
    string? ContactorPhoneNo,
    string? Remark
);

public record TransporterImportResultDto(
    int TotalRows,
    int Inserted,
    int Updated,
    List<string> TransporterCodes
);

public record TransporterStatsDto(
    int TotalTransporters,
    int TotalActiveTransporters,
    int TotalCars,
    int TotalActiveCars,
    int TotalDrivers,
    int TotalActiveDrivers,
    Dictionary<string, int> CarsByTransporter,
    Dictionary<string, int> DriversByTransporter
);

public interface ITransporterService
{
    // Mst_Transporter
    Task<List<TransporterDto>> SearchTransportersAsync(string? transporterCode, string? transporterName, string? flagActive);
    Task<TransporterDto?> GetTransporterAsync(string transporterCode);
    Task<TransporterDto> CreateTransporterAsync(CreateTransporterDto dto);
    Task<TransporterDto?> UpdateTransporterAsync(string transporterCode, UpdateTransporterDto dto);
    Task<bool> DeleteTransporterAsync(string transporterCode);
    Task<TransporterImportResultDto> ImportTransportersAsync(List<TransporterImportRowDto> rows);

    // Mst_TransporterCar
    Task<List<TransporterCarDto>> SearchCarsAsync(string? transporterCode, string? plateNo, string? flagActive);
    Task<TransporterCarDto?> GetCarAsync(string transporterCode, string plateNo);
    Task<TransporterCarDto> CreateCarAsync(CreateTransporterCarDto dto);
    Task<TransporterCarDto?> UpdateCarAsync(string transporterCode, string plateNo, UpdateTransporterCarDto dto);
    Task<bool> DeleteCarAsync(string transporterCode, string plateNo);

    // Mst_TransporterDriver
    Task<List<TransporterDriverDto>> SearchDriversAsync(string? transporterCode, string? driverId, string? flagActive);
    Task<TransporterDriverDto?> GetDriverAsync(string transporterCode, string driverId);
    Task<TransporterDriverDto> CreateDriverAsync(CreateTransporterDriverDto dto);
    Task<TransporterDriverDto?> UpdateDriverAsync(string transporterCode, string driverId, UpdateTransporterDriverDto dto);
    Task<bool> DeleteDriverAsync(string transporterCode, string driverId);

    Task<TransporterStatsDto> GetStatsAsync();
}

/// <summary>
/// Quản lý Nhà vận chuyển (DMS.Sales Mst_Transporter + Mst_TransporterCar + Mst_TransporterDriver / Master.1.cs):
/// - Mst_Transporter: danh mục công ty vận tải xe. TransporterCode là khóa nghiệp vụ duy nhất; bắt buộc
///   TransporterName và TransportContractNo (Mst_Transporter_Create kiểm tra 3 trường bắt buộc).
/// - Mst_TransporterCar: biển số xe chuyên dùng của nhà vận chuyển. Khóa nghiệp vụ = (TransporterCode, PlateNo);
///   FK TransporterCode phải tồn tại + đang hoạt động (Mst_Transporter_CheckDB - FlagExistToCheck = Yes, FlagActive = Active).
/// - Mst_TransporterDriver: tài xế của nhà vận chuyển. Khóa nghiệp vụ = (TransporterCode, DriverId);
///   bắt buộc DriverFullName và DriverLicenseNo. Hỗ trợ nhập hàng loạt (Import) cho Mst_Transporter.
/// </summary>
public sealed class TransporterService(AppDbContext db, ITenantContext tenant) : ITransporterService
{
    private Guid Org => tenant.OrgId;

    // ===== Mst_Transporter =====
    public async Task<List<TransporterDto>> SearchTransportersAsync(string? transporterCode, string? transporterName, string? flagActive)
    {
        var q = db.Transporters.AsNoTracking().Where(x => x.OrgId == Org);
        if (!string.IsNullOrWhiteSpace(transporterCode))
        {
            var tc = transporterCode.Trim();
            q = q.Where(x => x.TransporterCode == tc);
        }
        if (!string.IsNullOrWhiteSpace(transporterName))
        {
            var tn = transporterName.Trim();
            q = q.Where(x => x.TransporterName.Contains(tn));
        }
        if (!string.IsNullOrWhiteSpace(flagActive))
        {
            var fa = flagActive.Trim();
            q = q.Where(x => x.FlagActive == fa);
        }
        var list = await q.OrderBy(x => x.TransporterCode).ToListAsync();
        return list.Select(ToTransporterDto).ToList();
    }

    public async Task<TransporterDto?> GetTransporterAsync(string transporterCode)
    {
        if (string.IsNullOrWhiteSpace(transporterCode)) return null;
        var tc = transporterCode.Trim();
        var e = await db.Transporters.AsNoTracking().FirstOrDefaultAsync(x => x.OrgId == Org && x.TransporterCode == tc);
        return e is null ? null : ToTransporterDto(e);
    }

    public async Task<TransporterDto> CreateTransporterAsync(CreateTransporterDto dto)
    {
        var code = (dto.TransporterCode ?? "").Trim();
        if (code.Length < 1)
            throw new InvalidOperationException("Mã nhà vận chuyển (TransporterCode) không được để trống.");

        // TransporterCode là khóa nghiệp vụ duy nhất (Mst_Transporter_CheckDB - FlagExistToCheck = No)
        if (await db.Transporters.AnyAsync(x => x.OrgId == Org && x.TransporterCode == code))
            throw new InvalidOperationException($"Nhà vận chuyển '{code}' đã tồn tại.");

        var name = (dto.TransporterName ?? "").Trim();
        if (name.Length < 1)
            throw new InvalidOperationException("Tên nhà vận chuyển (TransporterName) không được để trống.");

        var contractNo = (dto.TransportContractNo ?? "").Trim();
        if (contractNo.Length < 1)
            throw new InvalidOperationException("Số hợp đồng vận chuyển (TransportContractNo) không được để trống.");

        var user = string.IsNullOrWhiteSpace(dto.ActionBy) ? "CHUYEN_VIEN_NPP" : dto.ActionBy.Trim();
        var entity = new TransporterMaster
        {
            OrgId = Org,
            TransporterCode = code,
            TransporterName = name,
            TransportContractNo = contractNo,
            Address = dto.Address?.Trim(),
            PhoneNo = dto.PhoneNo?.Trim(),
            FaxNo = dto.FaxNo?.Trim(),
            DirectorFullName = dto.DirectorFullName?.Trim(),
            DirectorPhoneNo = dto.DirectorPhoneNo?.Trim(),
            ContactorFullName = dto.ContactorFullName?.Trim(),
            ContactorPhoneNo = dto.ContactorPhoneNo?.Trim(),
            Remark = dto.Remark,
            FlagActive = NormalizeFlag(dto.FlagActive),
            LogLUDateTime = DateTime.Now,
            LogLUBy = user
        };
        db.Transporters.Add(entity);
        await db.SaveChangesAsync();
        return ToTransporterDto(entity);
    }

    public async Task<TransporterDto?> UpdateTransporterAsync(string transporterCode, UpdateTransporterDto dto)
    {
        if (string.IsNullOrWhiteSpace(transporterCode)) return null;
        var tc = transporterCode.Trim();
        var entity = await db.Transporters.FirstOrDefaultAsync(x => x.OrgId == Org && x.TransporterCode == tc);
        if (entity is null) return null;

        // Mst_Transporter_Update: nếu cập nhật TransporterName/TransportContractNo thì không được để rỗng
        if (dto.TransporterName is not null)
        {
            var name = dto.TransporterName.Trim();
            if (name.Length < 1)
                throw new InvalidOperationException("Tên nhà vận chuyển (TransporterName) không được để trống.");
            entity.TransporterName = name;
        }
        if (dto.TransportContractNo is not null)
        {
            var contractNo = dto.TransportContractNo.Trim();
            if (contractNo.Length < 1)
                throw new InvalidOperationException("Số hợp đồng vận chuyển (TransportContractNo) không được để trống.");
            entity.TransportContractNo = contractNo;
        }

        var user = string.IsNullOrWhiteSpace(dto.ActionBy) ? "CHUYEN_VIEN_NPP" : dto.ActionBy.Trim();
        if (dto.Address is not null) entity.Address = dto.Address.Trim();
        if (dto.PhoneNo is not null) entity.PhoneNo = dto.PhoneNo.Trim();
        if (dto.FaxNo is not null) entity.FaxNo = dto.FaxNo.Trim();
        if (dto.DirectorFullName is not null) entity.DirectorFullName = dto.DirectorFullName.Trim();
        if (dto.DirectorPhoneNo is not null) entity.DirectorPhoneNo = dto.DirectorPhoneNo.Trim();
        if (dto.ContactorFullName is not null) entity.ContactorFullName = dto.ContactorFullName.Trim();
        if (dto.ContactorPhoneNo is not null) entity.ContactorPhoneNo = dto.ContactorPhoneNo.Trim();
        if (dto.Remark is not null) entity.Remark = dto.Remark;
        entity.FlagActive = NormalizeFlag(dto.FlagActive);
        entity.LogLUDateTime = DateTime.Now;
        entity.LogLUBy = user;

        await db.SaveChangesAsync();
        return ToTransporterDto(entity);
    }

    public async Task<bool> DeleteTransporterAsync(string transporterCode)
    {
        if (string.IsNullOrWhiteSpace(transporterCode)) return false;
        var tc = transporterCode.Trim();
        var entity = await db.Transporters.FirstOrDefaultAsync(x => x.OrgId == Org && x.TransporterCode == tc);
        if (entity is null) return false;

        // Chặn xóa khi còn xe chuyên dùng / tài xế đang tham chiếu nhà vận chuyển (FK Mst_TransporterCar / Mst_TransporterDriver)
        var hasCars = await db.TransporterCars.AnyAsync(x => x.OrgId == Org && x.TransporterCode == tc);
        var hasDrivers = await db.TransporterDrivers.AnyAsync(x => x.OrgId == Org && x.TransporterCode == tc);
        if (hasCars || hasDrivers)
            throw new InvalidOperationException($"Không thể xóa nhà vận chuyển '{tc}' vì đang có xe chuyên dùng hoặc tài xế tham chiếu.");

        db.Transporters.Remove(entity);
        await db.SaveChangesAsync();
        return true;
    }

    public async Task<TransporterImportResultDto> ImportTransportersAsync(List<TransporterImportRowDto> rows)
    {
        if (rows is null || rows.Count < 1)
            throw new InvalidOperationException("Danh sách nhập (Import) rỗng, không có dòng dữ liệu nào.");

        // Phase 1: kiểm tra từng dòng (field validation)
        for (int i = 0; i < rows.Count; i++)
        {
            var code = (rows[i].TransporterCode ?? "").Trim();
            var name = (rows[i].TransporterName ?? "").Trim();
            var contractNo = (rows[i].TransportContractNo ?? "").Trim();
            if (code.Length < 1)
                throw new InvalidOperationException($"Dòng {i}: Mã nhà vận chuyển (TransporterCode) không được để trống.");
            if (name.Length < 1)
                throw new InvalidOperationException($"Dòng {i}: Tên nhà vận chuyển (TransporterName) không được để trống.");
            if (contractNo.Length < 1)
                throw new InvalidOperationException($"Dòng {i}: Số hợp đồng vận chuyển (TransportContractNo) không được để trống.");
        }

        // Phase 2: kiểm tra trùng khóa TransporterCode giữa các dòng
        var dup = rows.GroupBy(r => (r.TransporterCode ?? "").Trim())
                      .FirstOrDefault(g => g.Count() > 1);
        if (dup is not null)
            throw new InvalidOperationException($"Mã nhà vận chuyển '{dup.Key}' bị trùng lặp trong danh sách nhập.");

        var user = "CHUYEN_VIEN_NPP";
        int inserted = 0, updated = 0;
        var codes = new List<string>();

        // Phase 3: upsert (dòng đã có thì cập nhật, dòng mới thì thêm)
        foreach (var r in rows)
        {
            var code = (r.TransporterCode ?? "").Trim();
            var existing = await db.Transporters.FirstOrDefaultAsync(x => x.OrgId == Org && x.TransporterCode == code);
            if (existing is not null)
            {
                existing.TransporterName = (r.TransporterName ?? "").Trim();
                existing.TransportContractNo = (r.TransportContractNo ?? "").Trim();
                existing.Address = r.Address?.Trim();
                existing.PhoneNo = r.PhoneNo?.Trim();
                existing.FaxNo = r.FaxNo?.Trim();
                existing.DirectorFullName = r.DirectorFullName?.Trim();
                existing.DirectorPhoneNo = r.DirectorPhoneNo?.Trim();
                existing.ContactorFullName = r.ContactorFullName?.Trim();
                existing.ContactorPhoneNo = r.ContactorPhoneNo?.Trim();
                existing.Remark = r.Remark;
                existing.LogLUDateTime = DateTime.Now;
                existing.LogLUBy = user;
                updated++;
            }
            else
            {
                db.Transporters.Add(new TransporterMaster
                {
                    OrgId = Org,
                    TransporterCode = code,
                    TransporterName = (r.TransporterName ?? "").Trim(),
                    TransportContractNo = (r.TransportContractNo ?? "").Trim(),
                    Address = r.Address?.Trim(),
                    PhoneNo = r.PhoneNo?.Trim(),
                    FaxNo = r.FaxNo?.Trim(),
                    DirectorFullName = r.DirectorFullName?.Trim(),
                    DirectorPhoneNo = r.DirectorPhoneNo?.Trim(),
                    ContactorFullName = r.ContactorFullName?.Trim(),
                    ContactorPhoneNo = r.ContactorPhoneNo?.Trim(),
                    Remark = r.Remark,
                    FlagActive = "1",
                    LogLUDateTime = DateTime.Now,
                    LogLUBy = user
                });
                inserted++;
            }
            codes.Add(code);
        }

        await db.SaveChangesAsync();
        return new TransporterImportResultDto(rows.Count, inserted, updated, codes);
    }

    // ===== Mst_TransporterCar =====
    public async Task<List<TransporterCarDto>> SearchCarsAsync(string? transporterCode, string? plateNo, string? flagActive)
    {
        var q = db.TransporterCars.AsNoTracking().Where(x => x.OrgId == Org);
        if (!string.IsNullOrWhiteSpace(transporterCode))
        {
            var tc = transporterCode.Trim();
            q = q.Where(x => x.TransporterCode == tc);
        }
        if (!string.IsNullOrWhiteSpace(plateNo))
        {
            var pn = plateNo.Trim();
            q = q.Where(x => x.PlateNo.Contains(pn));
        }
        if (!string.IsNullOrWhiteSpace(flagActive))
        {
            var fa = flagActive.Trim();
            q = q.Where(x => x.FlagActive == fa);
        }
        var list = await q.OrderBy(x => x.TransporterCode).ThenBy(x => x.PlateNo).ToListAsync();
        var names = await GetTransporterNamesAsync(list.Select(x => x.TransporterCode).Distinct().ToList());
        return list.Select(x => ToCarDto(x, names.GetValueOrDefault(x.TransporterCode))).ToList();
    }

    public async Task<TransporterCarDto?> GetCarAsync(string transporterCode, string plateNo)
    {
        if (string.IsNullOrWhiteSpace(transporterCode) || string.IsNullOrWhiteSpace(plateNo)) return null;
        var tc = transporterCode.Trim();
        var pn = plateNo.Trim();
        var e = await db.TransporterCars.AsNoTracking()
            .FirstOrDefaultAsync(x => x.OrgId == Org && x.TransporterCode == tc && x.PlateNo == pn);
        if (e is null) return null;
        return ToCarDto(e, await GetTransporterNameAsync(tc));
    }

    public async Task<TransporterCarDto> CreateCarAsync(CreateTransporterCarDto dto)
    {
        var tc = (dto.TransporterCode ?? "").Trim();
        if (tc.Length < 1)
            throw new InvalidOperationException("Mã nhà vận chuyển (TransporterCode) không được để trống.");

        // FK: nhà vận chuyển phải tồn tại + active (Mst_Transporter_CheckDB - FlagExistToCheck = Yes, FlagActive = Active)
        await CheckTransporterExistsAsync(tc);

        var pn = (dto.PlateNo ?? "").Trim();
        if (pn.Length < 1)
            throw new InvalidOperationException("Biển số xe (PlateNo) không được để trống.");

        // Khóa nghiệp vụ (TransporterCode, PlateNo) duy nhất (Mst_TransporterCar_CheckDB - FlagExistToCheck = No)
        if (await db.TransporterCars.AnyAsync(x => x.OrgId == Org && x.TransporterCode == tc && x.PlateNo == pn))
            throw new InvalidOperationException($"Xe '{pn}' của nhà vận chuyển '{tc}' đã tồn tại.");

        var user = string.IsNullOrWhiteSpace(dto.ActionBy) ? "CHUYEN_VIEN_NPP" : dto.ActionBy.Trim();
        var entity = new TransporterCar
        {
            OrgId = Org,
            TransporterCode = tc,
            PlateNo = pn,
            FlagActive = NormalizeFlag(dto.FlagActive),
            LogLUDateTime = DateTime.Now,
            LogLUBy = user
        };
        db.TransporterCars.Add(entity);
        await db.SaveChangesAsync();
        return ToCarDto(entity, await GetTransporterNameAsync(tc));
    }

    public async Task<TransporterCarDto?> UpdateCarAsync(string transporterCode, string plateNo, UpdateTransporterCarDto dto)
    {
        if (string.IsNullOrWhiteSpace(transporterCode) || string.IsNullOrWhiteSpace(plateNo)) return null;
        var tc = transporterCode.Trim();
        var pn = plateNo.Trim();
        var entity = await db.TransporterCars
            .FirstOrDefaultAsync(x => x.OrgId == Org && x.TransporterCode == tc && x.PlateNo == pn);
        if (entity is null) return null;

        var user = string.IsNullOrWhiteSpace(dto.ActionBy) ? "CHUYEN_VIEN_NPP" : dto.ActionBy.Trim();
        entity.FlagActive = NormalizeFlag(dto.FlagActive);
        entity.LogLUDateTime = DateTime.Now;
        entity.LogLUBy = user;

        await db.SaveChangesAsync();
        return ToCarDto(entity, await GetTransporterNameAsync(tc));
    }

    public async Task<bool> DeleteCarAsync(string transporterCode, string plateNo)
    {
        if (string.IsNullOrWhiteSpace(transporterCode) || string.IsNullOrWhiteSpace(plateNo)) return false;
        var tc = transporterCode.Trim();
        var pn = plateNo.Trim();
        var entity = await db.TransporterCars
            .FirstOrDefaultAsync(x => x.OrgId == Org && x.TransporterCode == tc && x.PlateNo == pn);
        if (entity is null) return false;

        db.TransporterCars.Remove(entity);
        await db.SaveChangesAsync();
        return true;
    }

    // ===== Mst_TransporterDriver =====
    public async Task<List<TransporterDriverDto>> SearchDriversAsync(string? transporterCode, string? driverId, string? flagActive)
    {
        var q = db.TransporterDrivers.AsNoTracking().Where(x => x.OrgId == Org);
        if (!string.IsNullOrWhiteSpace(transporterCode))
        {
            var tc = transporterCode.Trim();
            q = q.Where(x => x.TransporterCode == tc);
        }
        if (!string.IsNullOrWhiteSpace(driverId))
        {
            var di = driverId.Trim();
            q = q.Where(x => x.DriverId == di);
        }
        if (!string.IsNullOrWhiteSpace(flagActive))
        {
            var fa = flagActive.Trim();
            q = q.Where(x => x.FlagActive == fa);
        }
        var list = await q.OrderBy(x => x.TransporterCode).ThenBy(x => x.DriverId).ToListAsync();
        var names = await GetTransporterNamesAsync(list.Select(x => x.TransporterCode).Distinct().ToList());
        return list.Select(x => ToDriverDto(x, names.GetValueOrDefault(x.TransporterCode))).ToList();
    }

    public async Task<TransporterDriverDto?> GetDriverAsync(string transporterCode, string driverId)
    {
        if (string.IsNullOrWhiteSpace(transporterCode) || string.IsNullOrWhiteSpace(driverId)) return null;
        var tc = transporterCode.Trim();
        var di = driverId.Trim();
        var e = await db.TransporterDrivers.AsNoTracking()
            .FirstOrDefaultAsync(x => x.OrgId == Org && x.TransporterCode == tc && x.DriverId == di);
        if (e is null) return null;
        return ToDriverDto(e, await GetTransporterNameAsync(tc));
    }

    public async Task<TransporterDriverDto> CreateDriverAsync(CreateTransporterDriverDto dto)
    {
        var tc = (dto.TransporterCode ?? "").Trim();
        if (tc.Length < 1)
            throw new InvalidOperationException("Mã nhà vận chuyển (TransporterCode) không được để trống.");

        var di = (dto.DriverId ?? "").Trim();
        if (di.Length < 1)
            throw new InvalidOperationException("Mã tài xế (DriverId) không được để trống.");

        // Khóa nghiệp vụ (TransporterCode, DriverId) duy nhất (Mst_TransporterDriver_CheckDB - FlagExistToCheck = No)
        if (await db.TransporterDrivers.AnyAsync(x => x.OrgId == Org && x.TransporterCode == tc && x.DriverId == di))
            throw new InvalidOperationException($"Tài xế '{di}' của nhà vận chuyển '{tc}' đã tồn tại.");

        var fullName = (dto.DriverFullName ?? "").Trim();
        if (fullName.Length < 1)
            throw new InvalidOperationException("Tên tài xế (DriverFullName) không được để trống.");

        var licenseNo = (dto.DriverLicenseNo ?? "").Trim();
        if (licenseNo.Length < 1)
            throw new InvalidOperationException("Số giấy phép lái xe (DriverLicenseNo) không được để trống.");

        var user = string.IsNullOrWhiteSpace(dto.ActionBy) ? "CHUYEN_VIEN_NPP" : dto.ActionBy.Trim();
        var entity = new TransporterDriver
        {
            OrgId = Org,
            TransporterCode = tc,
            DriverId = di,
            DriverFullName = fullName,
            DriverLicenseNo = licenseNo,
            DriverPhoneNo = dto.DriverPhoneNo?.Trim(),
            FlagActive = NormalizeFlag(dto.FlagActive),
            LogLUDateTime = DateTime.Now,
            LogLUBy = user
        };
        db.TransporterDrivers.Add(entity);
        await db.SaveChangesAsync();
        return ToDriverDto(entity, await GetTransporterNameAsync(tc));
    }

    public async Task<TransporterDriverDto?> UpdateDriverAsync(string transporterCode, string driverId, UpdateTransporterDriverDto dto)
    {
        if (string.IsNullOrWhiteSpace(transporterCode) || string.IsNullOrWhiteSpace(driverId)) return null;
        var tc = transporterCode.Trim();
        var di = driverId.Trim();
        var entity = await db.TransporterDrivers
            .FirstOrDefaultAsync(x => x.OrgId == Org && x.TransporterCode == tc && x.DriverId == di);
        if (entity is null) return null;

        if (dto.DriverFullName is not null)
        {
            var fullName = dto.DriverFullName.Trim();
            if (fullName.Length < 1)
                throw new InvalidOperationException("Tên tài xế (DriverFullName) không được để trống.");
            entity.DriverFullName = fullName;
        }
        if (dto.DriverLicenseNo is not null)
        {
            var licenseNo = dto.DriverLicenseNo.Trim();
            if (licenseNo.Length < 1)
                throw new InvalidOperationException("Số giấy phép lái xe (DriverLicenseNo) không được để trống.");
            entity.DriverLicenseNo = licenseNo;
        }

        var user = string.IsNullOrWhiteSpace(dto.ActionBy) ? "CHUYEN_VIEN_NPP" : dto.ActionBy.Trim();
        if (dto.DriverPhoneNo is not null) entity.DriverPhoneNo = dto.DriverPhoneNo.Trim();
        entity.FlagActive = NormalizeFlag(dto.FlagActive);
        entity.LogLUDateTime = DateTime.Now;
        entity.LogLUBy = user;

        await db.SaveChangesAsync();
        return ToDriverDto(entity, await GetTransporterNameAsync(tc));
    }

    public async Task<bool> DeleteDriverAsync(string transporterCode, string driverId)
    {
        if (string.IsNullOrWhiteSpace(transporterCode) || string.IsNullOrWhiteSpace(driverId)) return false;
        var tc = transporterCode.Trim();
        var di = driverId.Trim();
        var entity = await db.TransporterDrivers
            .FirstOrDefaultAsync(x => x.OrgId == Org && x.TransporterCode == tc && x.DriverId == di);
        if (entity is null) return false;

        db.TransporterDrivers.Remove(entity);
        await db.SaveChangesAsync();
        return true;
    }

    public async Task<TransporterStatsDto> GetStatsAsync()
    {
        var transporters = await db.Transporters.AsNoTracking().Where(x => x.OrgId == Org).ToListAsync();
        var cars = await db.TransporterCars.AsNoTracking().Where(x => x.OrgId == Org).ToListAsync();
        var drivers = await db.TransporterDrivers.AsNoTracking().Where(x => x.OrgId == Org).ToListAsync();

        return new TransporterStatsDto(
            TotalTransporters: transporters.Count,
            TotalActiveTransporters: transporters.Count(x => x.FlagActive == "1"),
            TotalCars: cars.Count,
            TotalActiveCars: cars.Count(x => x.FlagActive == "1"),
            TotalDrivers: drivers.Count,
            TotalActiveDrivers: drivers.Count(x => x.FlagActive == "1"),
            CarsByTransporter: cars.Where(x => x.FlagActive == "1")
                                   .GroupBy(x => string.IsNullOrWhiteSpace(x.TransporterCode) ? "UNKNOWN" : x.TransporterCode)
                                   .ToDictionary(g => g.Key, g => g.Count()),
            DriversByTransporter: drivers.Where(x => x.FlagActive == "1")
                                         .GroupBy(x => string.IsNullOrWhiteSpace(x.TransporterCode) ? "UNKNOWN" : x.TransporterCode)
                                         .ToDictionary(g => g.Key, g => g.Count())
        );
    }

    // ===== Helpers =====
    private static string NormalizeFlag(string? flag)
        => string.IsNullOrWhiteSpace(flag) ? "1" : (flag.Trim() == "0" ? "0" : "1");

    /// <summary>Kiểm tra nhà vận chuyển tồn tại và đang hoạt động (Mst_Transporter_CheckDB).</summary>
    private async Task CheckTransporterExistsAsync(string transporterCode)
    {
        var exists = await db.Transporters.AsNoTracking()
            .Where(x => x.OrgId == Org && x.TransporterCode == transporterCode && x.FlagActive == "1")
            .Select(x => x.TransporterCode)
            .FirstOrDefaultAsync();
        if (exists is null)
            throw new InvalidOperationException($"Nhà vận chuyển '{transporterCode}' không tồn tại hoặc đã ngừng áp dụng (Mst_Transporter).");
    }

    private async Task<string?> GetTransporterNameAsync(string transporterCode)
    {
        var name = await db.Transporters.AsNoTracking()
            .Where(x => x.OrgId == Org && x.TransporterCode == transporterCode)
            .Select(x => x.TransporterName)
            .FirstOrDefaultAsync();
        return string.IsNullOrWhiteSpace(name) ? null : name;
    }

    private async Task<Dictionary<string, string?>> GetTransporterNamesAsync(List<string> codes)
    {
        if (codes.Count < 1) return new Dictionary<string, string?>();
        var rows = await db.Transporters.AsNoTracking()
            .Where(x => x.OrgId == Org && codes.Contains(x.TransporterCode))
            .Select(x => new { x.TransporterCode, x.TransporterName })
            .ToListAsync();
        return rows.GroupBy(x => x.TransporterCode)
                   .ToDictionary(g => g.Key, g => (string?)g.First().TransporterName);
    }

    private static TransporterDto ToTransporterDto(TransporterMaster e) => new(
        e.Id, e.TransporterCode, e.TransporterName, e.TransportContractNo, e.Address, e.PhoneNo, e.FaxNo,
        e.DirectorFullName, e.DirectorPhoneNo, e.ContactorFullName, e.ContactorPhoneNo, e.Remark,
        e.FlagActive, e.FlagActive == "1", e.LogLUDateTime, e.LogLUBy);

    private static TransporterCarDto ToCarDto(TransporterCar e, string? transporterName) => new(
        e.Id, e.TransporterCode, transporterName, e.PlateNo, e.FlagActive, e.FlagActive == "1", e.LogLUDateTime, e.LogLUBy);

    private static TransporterDriverDto ToDriverDto(TransporterDriver e, string? transporterName) => new(
        e.Id, e.TransporterCode, transporterName, e.DriverId, e.DriverFullName, e.DriverLicenseNo, e.DriverPhoneNo,
        e.FlagActive, e.FlagActive == "1", e.LogLUDateTime, e.LogLUBy);
}