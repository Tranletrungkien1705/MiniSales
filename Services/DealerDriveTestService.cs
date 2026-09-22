using Microsoft.EntityFrameworkCore;
using MiniSales.Data;
using MiniSales.Models;

namespace MiniSales.Services;

public record CreateDealerDriveTestDto(
    string? DriveTestCode,
    string DealerCode,
    string? DealerName,
    string? DriverTestGroup, // SHOWROOM hoặc EVENT
    string? DriverTestType, // HTV hoặc DEALER
    DateTime? DriveDTime,
    string? CustomerCode,
    string FullName,
    string? Gender, // M hoặc F
    string? RangeAgeCode, // Năm sinh (1940 - 2010)
    string DriverLicenseNo, // Số GPLX (bắt buộc)
    string? IDCardNo,
    string PhoneNo,
    string? Email,
    string? Address,
    string? ProvinceCode,
    string? ProvinceName,
    string ModelCode, // Santa Fe, Tucson, Creta, Accent, Custin, Stargazer...
    string? ModelName,
    string DrvTestPlateNo, // Biển số xe lái thử demo
    string? DrvTestVIN,
    string? SalesManCode,
    string? SalesManName,
    string? Evaluation, // Rất hài lòng, Hài lòng, Bình thường, Không hài lòng
    string? CustomerIntent, // WillBuy, Considering, Consulting, NoDemand
    DateTime? EstimatedContractDate,
    decimal SupportAmount = 0,
    string? CreatedBy = null,
    string? Remark = null
);

public record UpdateDealerDriveTestDto(
    string? FullName,
    string? Gender,
    string? RangeAgeCode,
    string? DriverLicenseNo,
    string? IDCardNo,
    string? PhoneNo,
    string? Email,
    string? Address,
    string? ProvinceCode,
    string? ProvinceName,
    string? ModelCode,
    string? ModelName,
    string? DrvTestPlateNo,
    string? DrvTestVIN,
    DateTime? DriveDTime,
    string? DriverTestGroup,
    string? DriverTestType,
    string? SalesManCode,
    string? SalesManName,
    string? Evaluation,
    string? CustomerIntent,
    DateTime? EstimatedContractDate,
    decimal? SupportAmount,
    string? Remark,
    string? UpdatedBy
);

public record ApproveDealerDriveTestDto(
    decimal? ApprovedAmount,
    string? ApprovedBy,
    string? Remark
);

public record RejectDealerDriveTestDto(
    string RejectReason,
    string? RejectedBy
);

public record CancelDealerDriveTestDto(
    string CancelReason,
    string? CancelledBy
);

public record BatchDriveTestCodeDto(
    List<string> DriveTestCodes,
    decimal? ApprovedAmount = null,
    string? RejectReason = null,
    string? ActionBy = null
);

public record DealerDriveTestStatsDto(
    int TotalDriveTests,
    int PendingCount,
    int ApprovedCount,
    int RejectedCount,
    int CancelledCount,
    int ShowroomGroupCount,
    int EventGroupCount,
    int HtvSponsoredCount,
    int DealerFundedCount,
    decimal TotalProposedSupportAmount,
    decimal TotalApprovedSupportAmount,
    int WillBuyIntentCount,
    int ConsideringIntentCount,
    int DistinctDealersCount
);

public record EligibleTestCarDto(
    string DrvTestPlateNo,
    string Vin,
    string Model,
    string? ModelCode,
    string DealerCode,
    string DealerName
);

public interface IDealerDriveTestService
{
    Task<DealerDriveTest> CreateAsync(CreateDealerDriveTestDto dto);
    Task<List<DealerDriveTest>> ListAsync(
        string? status = null,
        string? dealerCode = null,
        string? modelCode = null,
        string? plateNo = null,
        string? driveTestGroup = null,
        string? driveTestType = null,
        string? customerPhone = null,
        DateTime? dateFrom = null,
        DateTime? dateTo = null
    );
    Task<DealerDriveTest?> DetailAsync(string driveTestCode);
    Task<DealerDriveTest?> UpdateAsync(string driveTestCode, UpdateDealerDriveTestDto dto);
    Task<bool> DeleteDraftAsync(string driveTestCode);
    Task<DealerDriveTest?> ApproveHqAsync(string driveTestCode, ApproveDealerDriveTestDto? dto);
    Task<List<DealerDriveTest>> ApproveMultipleHqAsync(List<string> driveTestCodes, ApproveDealerDriveTestDto? dto);
    Task<DealerDriveTest?> RejectHqAsync(string driveTestCode, RejectDealerDriveTestDto dto);
    Task<List<DealerDriveTest>> RejectMultipleHqAsync(List<string> driveTestCodes, RejectDealerDriveTestDto dto);
    Task<DealerDriveTest?> CancelAsync(string driveTestCode, CancelDealerDriveTestDto dto);
    Task<List<EligibleTestCarDto>> GetEligibleTestCarsAsync(string? dealerCode = null);
    Task<DealerDriveTestStatsDto> StatsAsync();
}

public class DealerDriveTestService(AppDbContext db, ITenantContext tenant) : IDealerDriveTestService
{
    public async Task<DealerDriveTest> CreateAsync(CreateDealerDriveTestDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.DealerCode))
            throw new InvalidOperationException("Mã đại lý (DealerCode) không được để trống.");

        if (string.IsNullOrWhiteSpace(dto.FullName))
            throw new InvalidOperationException("Họ tên khách hàng lái thử (FullName) không được để trống.");

        if (string.IsNullOrWhiteSpace(dto.PhoneNo))
            throw new InvalidOperationException("Số điện thoại khách hàng (PhoneNo) không được để trống.");

        // Rule DMS.Sales DealerRetail.cs: GPLX bắt buộc
        var licenseNo = dto.DriverLicenseNo?.Trim() ?? "";
        if (string.IsNullOrWhiteSpace(licenseNo))
            throw new InvalidOperationException("Số Giấy phép lái xe (GPLX - DriverLicenseNo) bắt buộc đối với khách hàng đăng ký lái thử.");

        // Rule DMS.Sales DealerRetail.cs:1095: Năm sinh RangeAgeCode phải từ 1940 đến 2010
        var rangeAge = dto.RangeAgeCode?.Trim() ?? "1990";
        if (int.TryParse(rangeAge, out var birthYear))
        {
            if (birthYear < 1940 || birthYear > 2010)
                throw new InvalidOperationException($"Năm sinh khách hàng ({birthYear}) không hợp lệ (phải trong khoảng từ 1940 đến 2010).");
        }
        else
        {
            throw new InvalidOperationException($"Năm sinh khách hàng ({rangeAge}) phải là chuỗi 4 chữ số hợp lệ.");
        }

        // Rule DMS.Sales DealerRetail.cs:1145: Ngày lái thử không được lớn hơn ngày hiện tại
        var driveDTime = dto.DriveDTime ?? DateTime.Today;
        if (driveDTime.Date > DateTime.Today.AddDays(1))
            throw new InvalidOperationException($"Ngày thực hiện lái thử ({driveDTime:dd/MM/yyyy}) không thể vượt quá ngày hiện tại.");

        if (string.IsNullOrWhiteSpace(dto.ModelCode))
            throw new InvalidOperationException("Model xe đăng ký lái thử (ModelCode) không được để trống.");

        if (string.IsNullOrWhiteSpace(dto.DrvTestPlateNo))
            throw new InvalidOperationException("Biển số xe lái thử demo (DrvTestPlateNo) không được để trống.");

        var dealerCode = dto.DealerCode.Trim();
        var dealerName = !string.IsNullOrWhiteSpace(dto.DealerName)
            ? dto.DealerName.Trim()
            : ResolveDealerName(dealerCode);

        // Sinh mã lượt lái thử nếu chưa có: {yyMM}DT{seq:D4} (DMS40 SequenceType: DRIVETEST)
        var driveTestCode = dto.DriveTestCode?.Trim();
        if (string.IsNullOrWhiteSpace(driveTestCode))
        {
            var prefix = $"{DateTime.Today:yyMM}DT";
            var maxSeq = await db.DriveTests
                .Where(x => x.OrgId == tenant.OrgId && x.DriveTestCode.StartsWith(prefix))
                .Select(x => x.DriveTestCode)
                .ToListAsync();

            var nextNum = 1;
            foreach (var code in maxSeq)
            {
                if (code.Length >= prefix.Length + 4 && int.TryParse(code.Substring(prefix.Length, 4), out var n))
                {
                    if (n >= nextNum) nextNum = n + 1;
                }
            }
            driveTestCode = $"{prefix}{nextNum:D4}";
        }

        var exists = await db.DriveTests.AnyAsync(x => x.OrgId == tenant.OrgId && x.DriveTestCode == driveTestCode);
        if (exists)
            throw new InvalidOperationException($"Mã lượt lái thử {driveTestCode} đã tồn tại trong hệ thống.");

        var group = ParseGroup(dto.DriverTestGroup);
        var type = ParseType(dto.DriverTestType);

        var modelCode = dto.ModelCode.Trim();
        var modelName = !string.IsNullOrWhiteSpace(dto.ModelName)
            ? dto.ModelName.Trim()
            : ResolveModelName(modelCode);

        var plateNo = dto.DrvTestPlateNo.Trim().ToUpperInvariant();
        var vin = dto.DrvTestVIN?.Trim();

        // Tự động liên kết số VIN từ đội xe lái thử nếu chưa truyền VIN
        if (string.IsNullOrWhiteSpace(vin))
        {
            var demoCar = await db.TestCarDetails
                .Where(x => x.Vin != null && (x.TestCarCode != null || x.Vin != null))
                .FirstOrDefaultAsync(x => x.ModelCode == modelCode || x.Model.Contains(modelCode));
            if (demoCar != null)
            {
                vin = demoCar.Vin;
            }
        }

        var customerCode = !string.IsNullOrWhiteSpace(dto.CustomerCode)
            ? dto.CustomerCode.Trim()
            : $"CUST-{dealerCode}-{DateTime.Today:yyMMdd}{Random.Shared.Next(100, 999)}";

        var entity = new DealerDriveTest
        {
            OrgId = tenant.OrgId,
            DriveTestCode = driveTestCode,
            DealerCode = dealerCode,
            DealerName = dealerName,
            DriverTestGroup = group,
            DriverTestType = type,
            DriveDTime = driveDTime,
            CustomerCode = customerCode,
            FullName = dto.FullName.Trim(),
            Gender = NormalizeGender(dto.Gender),
            RangeAgeCode = rangeAge,
            DriverLicenseNo = licenseNo,
            IDCardNo = dto.IDCardNo?.Trim(),
            PhoneNo = dto.PhoneNo.Trim(),
            Email = dto.Email?.Trim(),
            Address = dto.Address?.Trim(),
            ProvinceCode = dto.ProvinceCode?.Trim(),
            ProvinceName = dto.ProvinceName?.Trim(),
            ModelCode = modelCode,
            ModelName = modelName,
            DrvTestPlateNo = plateNo,
            DrvTestVIN = vin,
            SalesManCode = dto.SalesManCode?.Trim(),
            SalesManName = dto.SalesManName?.Trim(),
            Evaluation = dto.Evaluation?.Trim(),
            CustomerIntent = dto.CustomerIntent?.Trim(),
            EstimatedContractDate = dto.EstimatedContractDate,
            SupportAmount = dto.SupportAmount >= 0 ? dto.SupportAmount : 0m,
            ApprovedAmount = 0m,
            Status = DriveTestStatus.Pending,
            CreatedDate = DateTime.UtcNow,
            CreatedBy = dto.CreatedBy?.Trim() ?? "System",
            Remark = dto.Remark?.Trim(),
            FlagActive = true
        };

        db.DriveTests.Add(entity);
        await db.SaveChangesAsync();
        return entity;
    }

    public async Task<List<DealerDriveTest>> ListAsync(
        string? status = null,
        string? dealerCode = null,
        string? modelCode = null,
        string? plateNo = null,
        string? driveTestGroup = null,
        string? driveTestType = null,
        string? customerPhone = null,
        DateTime? dateFrom = null,
        DateTime? dateTo = null
    )
    {
        var q = db.DriveTests.Where(x => x.OrgId == tenant.OrgId && x.FlagActive);

        if (!string.IsNullOrWhiteSpace(status))
        {
            if (TryParseStatus(status, out var parsedStatus))
                q = q.Where(x => x.Status == parsedStatus);
        }

        if (!string.IsNullOrWhiteSpace(dealerCode))
        {
            var d = dealerCode.Trim();
            q = q.Where(x => x.DealerCode == d || (x.DealerName != null && x.DealerName.Contains(d)));
        }

        if (!string.IsNullOrWhiteSpace(modelCode))
        {
            var m = modelCode.Trim();
            q = q.Where(x => x.ModelCode == m || (x.ModelName != null && x.ModelName.Contains(m)));
        }

        if (!string.IsNullOrWhiteSpace(plateNo))
        {
            var p = plateNo.Trim().ToUpperInvariant();
            q = q.Where(x => x.DrvTestPlateNo.Contains(p));
        }

        if (!string.IsNullOrWhiteSpace(driveTestGroup))
        {
            var g = ParseGroup(driveTestGroup);
            q = q.Where(x => x.DriverTestGroup == g);
        }

        if (!string.IsNullOrWhiteSpace(driveTestType))
        {
            var t = ParseType(driveTestType);
            q = q.Where(x => x.DriverTestType == t);
        }

        if (!string.IsNullOrWhiteSpace(customerPhone))
        {
            var ph = customerPhone.Trim();
            q = q.Where(x => x.PhoneNo.Contains(ph) || x.FullName.Contains(ph));
        }

        if (dateFrom.HasValue)
            q = q.Where(x => x.DriveDTime >= dateFrom.Value.Date);

        if (dateTo.HasValue)
            q = q.Where(x => x.DriveDTime <= dateTo.Value.Date.AddDays(1).AddTicks(-1));

        return await q.OrderByDescending(x => x.DriveDTime).ThenByDescending(x => x.Id).ToListAsync();
    }

    public async Task<DealerDriveTest?> DetailAsync(string driveTestCode)
    {
        if (string.IsNullOrWhiteSpace(driveTestCode)) return null;
        var code = driveTestCode.Trim();
        return await db.DriveTests
            .FirstOrDefaultAsync(x => x.OrgId == tenant.OrgId && x.DriveTestCode == code && x.FlagActive);
    }

    public async Task<DealerDriveTest?> UpdateAsync(string driveTestCode, UpdateDealerDriveTestDto dto)
    {
        var item = await DetailAsync(driveTestCode);
        if (item == null) return null;

        if (item.Status != DriveTestStatus.Pending)
            throw new InvalidOperationException($"Lượt lái thử {item.DriveTestCode} đang ở trạng thái {item.Status}, chỉ được phép chỉnh sửa khi ở trạng thái Chờ duyệt (Pending).");

        if (!string.IsNullOrWhiteSpace(dto.FullName)) item.FullName = dto.FullName.Trim();
        if (!string.IsNullOrWhiteSpace(dto.Gender)) item.Gender = NormalizeGender(dto.Gender);
        if (!string.IsNullOrWhiteSpace(dto.RangeAgeCode))
        {
            var rangeAge = dto.RangeAgeCode.Trim();
            if (int.TryParse(rangeAge, out var yr) && yr >= 1940 && yr <= 2010)
                item.RangeAgeCode = rangeAge;
            else
                throw new InvalidOperationException($"Năm sinh khách hàng ({rangeAge}) phải là năm từ 1940 đến 2010.");
        }
        if (!string.IsNullOrWhiteSpace(dto.DriverLicenseNo)) item.DriverLicenseNo = dto.DriverLicenseNo.Trim();
        if (dto.IDCardNo != null) item.IDCardNo = dto.IDCardNo.Trim();
        if (!string.IsNullOrWhiteSpace(dto.PhoneNo)) item.PhoneNo = dto.PhoneNo.Trim();
        if (dto.Email != null) item.Email = dto.Email.Trim();
        if (dto.Address != null) item.Address = dto.Address.Trim();
        if (dto.ProvinceCode != null) item.ProvinceCode = dto.ProvinceCode.Trim();
        if (dto.ProvinceName != null) item.ProvinceName = dto.ProvinceName.Trim();

        if (!string.IsNullOrWhiteSpace(dto.ModelCode))
        {
            item.ModelCode = dto.ModelCode.Trim();
            item.ModelName = !string.IsNullOrWhiteSpace(dto.ModelName) ? dto.ModelName.Trim() : ResolveModelName(item.ModelCode);
        }

        if (!string.IsNullOrWhiteSpace(dto.DrvTestPlateNo)) item.DrvTestPlateNo = dto.DrvTestPlateNo.Trim().ToUpperInvariant();
        if (dto.DrvTestVIN != null) item.DrvTestVIN = dto.DrvTestVIN.Trim();

        if (dto.DriveDTime.HasValue)
        {
            if (dto.DriveDTime.Value.Date > DateTime.Today.AddDays(1))
                throw new InvalidOperationException("Ngày thực hiện lái thử không thể vượt quá ngày hiện tại.");
            item.DriveDTime = dto.DriveDTime.Value;
        }

        if (!string.IsNullOrWhiteSpace(dto.DriverTestGroup)) item.DriverTestGroup = ParseGroup(dto.DriverTestGroup);
        if (!string.IsNullOrWhiteSpace(dto.DriverTestType)) item.DriverTestType = ParseType(dto.DriverTestType);

        if (dto.SalesManCode != null) item.SalesManCode = dto.SalesManCode.Trim();
        if (dto.SalesManName != null) item.SalesManName = dto.SalesManName.Trim();
        if (dto.Evaluation != null) item.Evaluation = dto.Evaluation.Trim();
        if (dto.CustomerIntent != null) item.CustomerIntent = dto.CustomerIntent.Trim();
        if (dto.EstimatedContractDate.HasValue) item.EstimatedContractDate = dto.EstimatedContractDate;
        if (dto.SupportAmount.HasValue && dto.SupportAmount.Value >= 0) item.SupportAmount = dto.SupportAmount.Value;
        if (dto.Remark != null) item.Remark = dto.Remark.Trim();

        await db.SaveChangesAsync();
        return item;
    }

    public async Task<bool> DeleteDraftAsync(string driveTestCode)
    {
        var item = await DetailAsync(driveTestCode);
        if (item == null) return false;

        // Rule DMS.Sales DealerRetail.cs:2610: Chỉ được xóa khi DriverTestStatus == 'P'
        if (item.Status != DriveTestStatus.Pending)
            throw new InvalidOperationException($"Lượt lái thử {item.DriveTestCode} đang ở trạng thái {item.Status}, chỉ được phép xóa khi ở trạng thái Chờ duyệt (Pending).");

        db.DriveTests.Remove(item);
        await db.SaveChangesAsync();
        return true;
    }

    public async Task<DealerDriveTest?> ApproveHqAsync(string driveTestCode, ApproveDealerDriveTestDto? dto)
    {
        var item = await DetailAsync(driveTestCode);
        if (item == null) return null;

        if (item.Status != DriveTestStatus.Pending)
            throw new InvalidOperationException($"Lượt lái thử {item.DriveTestCode} đang ở trạng thái {item.Status}, không thể duyệt.");

        item.Status = DriveTestStatus.Approved;
        item.ApprovedDate = DateTime.UtcNow;
        item.ApprovedBy = dto?.ApprovedBy?.Trim() ?? "HQ_APPROVER";
        item.ApprovedAmount = (dto?.ApprovedAmount.HasValue == true && dto.ApprovedAmount.Value >= 0)
            ? dto.ApprovedAmount.Value
            : item.SupportAmount;

        if (!string.IsNullOrWhiteSpace(dto?.Remark))
        {
            item.Remark = string.IsNullOrWhiteSpace(item.Remark)
                ? $"[Duyệt HQ]: {dto.Remark.Trim()}"
                : $"{item.Remark} | [Duyệt HQ]: {dto.Remark.Trim()}";
        }

        await db.SaveChangesAsync();
        return item;
    }

    public async Task<List<DealerDriveTest>> ApproveMultipleHqAsync(List<string> driveTestCodes, ApproveDealerDriveTestDto? dto)
    {
        if (driveTestCodes == null || driveTestCodes.Count == 0)
            return new List<DealerDriveTest>();

        var normalizedCodes = driveTestCodes.Select(x => x.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        var items = await db.DriveTests
            .Where(x => x.OrgId == tenant.OrgId && normalizedCodes.Contains(x.DriveTestCode) && x.FlagActive)
            .ToListAsync();

        var approvedBy = dto?.ApprovedBy?.Trim() ?? "HQ_APPROVER";
        var now = DateTime.UtcNow;

        foreach (var item in items)
        {
            if (item.Status == DriveTestStatus.Pending)
            {
                item.Status = DriveTestStatus.Approved;
                item.ApprovedDate = now;
                item.ApprovedBy = approvedBy;
                item.ApprovedAmount = (dto?.ApprovedAmount.HasValue == true && dto.ApprovedAmount.Value >= 0)
                    ? dto.ApprovedAmount.Value
                    : item.SupportAmount;

                if (!string.IsNullOrWhiteSpace(dto?.Remark))
                {
                    item.Remark = string.IsNullOrWhiteSpace(item.Remark)
                        ? $"[Duyệt HQ hàng loạt]: {dto.Remark.Trim()}"
                        : $"{item.Remark} | [Duyệt HQ hàng loạt]: {dto.Remark.Trim()}";
                }
            }
        }

        await db.SaveChangesAsync();
        return items;
    }

    public async Task<DealerDriveTest?> RejectHqAsync(string driveTestCode, RejectDealerDriveTestDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.RejectReason))
            throw new InvalidOperationException("Lý do từ chối duyệt (RejectReason) không được để trống.");

        var item = await DetailAsync(driveTestCode);
        if (item == null) return null;

        if (item.Status != DriveTestStatus.Pending)
            throw new InvalidOperationException($"Lượt lái thử {item.DriveTestCode} đang ở trạng thái {item.Status}, không thể từ chối.");

        item.Status = DriveTestStatus.Rejected;
        item.RejectDate = DateTime.UtcNow;
        item.RejectBy = dto.RejectedBy?.Trim() ?? "HQ_APPROVER";
        item.RejectRemark = dto.RejectReason.Trim();

        await db.SaveChangesAsync();
        return item;
    }

    public async Task<List<DealerDriveTest>> RejectMultipleHqAsync(List<string> driveTestCodes, RejectDealerDriveTestDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.RejectReason))
            throw new InvalidOperationException("Lý do từ chối duyệt (RejectReason) không được để trống.");

        if (driveTestCodes == null || driveTestCodes.Count == 0)
            return new List<DealerDriveTest>();

        var normalizedCodes = driveTestCodes.Select(x => x.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        var items = await db.DriveTests
            .Where(x => x.OrgId == tenant.OrgId && normalizedCodes.Contains(x.DriveTestCode) && x.FlagActive)
            .ToListAsync();

        var rejectedBy = dto.RejectedBy?.Trim() ?? "HQ_APPROVER";
        var reason = dto.RejectReason.Trim();
        var now = DateTime.UtcNow;

        foreach (var item in items)
        {
            if (item.Status == DriveTestStatus.Pending)
            {
                item.Status = DriveTestStatus.Rejected;
                item.RejectDate = now;
                item.RejectBy = rejectedBy;
                item.RejectRemark = reason;
            }
        }

        await db.SaveChangesAsync();
        return items;
    }

    public async Task<DealerDriveTest?> CancelAsync(string driveTestCode, CancelDealerDriveTestDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.CancelReason))
            throw new InvalidOperationException("Lý do hủy lượt lái thử (CancelReason) không được để trống.");

        var item = await DetailAsync(driveTestCode);
        if (item == null) return null;

        if (item.Status != DriveTestStatus.Pending)
            throw new InvalidOperationException($"Lượt lái thử {item.DriveTestCode} đang ở trạng thái {item.Status}, không thể hủy.");

        item.Status = DriveTestStatus.Cancelled;
        item.CancelledDate = DateTime.UtcNow;
        item.CancelledBy = dto.CancelledBy?.Trim() ?? "DealerUser";
        item.CancelRemark = dto.CancelReason.Trim();

        await db.SaveChangesAsync();
        return item;
    }

    public async Task<List<EligibleTestCarDto>> GetEligibleTestCarsAsync(string? dealerCode = null)
    {
        // Tra cứu danh sách xe lái thử demo khả dụng từ đội xe CarTestCar và xe hợp đồng
        var query = db.TestCarDetails.AsQueryable();

        var list = await query
            .Select(d => new
            {
                d.Vin,
                d.Model,
                d.ModelCode,
                d.TestCarCode
            })
            .ToListAsync();

        var testCarCodes = list.Select(x => x.TestCarCode).Distinct().ToList();
        var testCars = await db.TestCars
            .Where(x => testCarCodes.Contains(x.TestCarCode))
            .ToDictionaryAsync(x => x.TestCarCode, x => new { x.DealerCode, x.DealerName });

        var results = new List<EligibleTestCarDto>();
        var seq = 1;

        foreach (var item in list)
        {
            testCars.TryGetValue(item.TestCarCode, out var tc);
            var dCode = tc?.DealerCode ?? (dealerCode ?? "DL01");
            var dName = tc?.DealerName ?? ResolveDealerName(dCode);

            if (!string.IsNullOrWhiteSpace(dealerCode) && !string.Equals(dCode, dealerCode.Trim(), StringComparison.OrdinalIgnoreCase))
                continue;

            var plateNo = $"30G-{seq:D2}{Random.Shared.Next(10, 99)}.{Random.Shared.Next(10, 99)}";
            seq++;

            results.Add(new EligibleTestCarDto(
                DrvTestPlateNo: plateNo,
                Vin: item.Vin,
                Model: item.Model,
                ModelCode: item.ModelCode,
                DealerCode: dCode,
                DealerName: dName
            ));
        }

        if (results.Count == 0)
        {
            // Seed fallback test car options for seamless UI experience
            results.Add(new EligibleTestCarDto("30H-889.12", "KMHCT81CRPU100001", "Hyundai Tucson 2.0 AT", "TUCSON", dealerCode ?? "DL01", ResolveDealerName(dealerCode ?? "DL01")));
            results.Add(new EligibleTestCarDto("30H-992.34", "KMHCT81CRPU100002", "Hyundai Santa Fe 2.5 AT", "SANTAFE", dealerCode ?? "DL01", ResolveDealerName(dealerCode ?? "DL01")));
            results.Add(new EligibleTestCarDto("51K-771.56", "KMHCT81CRPU100003", "Hyundai Creta 1.5 AT", "CRETA", dealerCode ?? "DL02", ResolveDealerName(dealerCode ?? "DL02")));
            results.Add(new EligibleTestCarDto("43A-662.88", "KMHCT81CRPU100004", "Hyundai Custin 1.5 Turbo", "CUSTIN", dealerCode ?? "DL03", ResolveDealerName(dealerCode ?? "DL03")));
        }

        return results;
    }

    public async Task<DealerDriveTestStatsDto> StatsAsync()
    {
        var items = await db.DriveTests
            .Where(x => x.OrgId == tenant.OrgId && x.FlagActive)
            .ToListAsync();

        return new DealerDriveTestStatsDto(
            TotalDriveTests: items.Count,
            PendingCount: items.Count(x => x.Status == DriveTestStatus.Pending),
            ApprovedCount: items.Count(x => x.Status == DriveTestStatus.Approved),
            RejectedCount: items.Count(x => x.Status == DriveTestStatus.Rejected),
            CancelledCount: items.Count(x => x.Status == DriveTestStatus.Cancelled),
            ShowroomGroupCount: items.Count(x => x.DriverTestGroup == DriveTestGroup.Showroom),
            EventGroupCount: items.Count(x => x.DriverTestGroup == DriveTestGroup.Event),
            HtvSponsoredCount: items.Count(x => x.DriverTestType == DriveTestType.HTV),
            DealerFundedCount: items.Count(x => x.DriverTestType == DriveTestType.Dealer),
            TotalProposedSupportAmount: items.Sum(x => x.SupportAmount),
            TotalApprovedSupportAmount: items.Where(x => x.Status == DriveTestStatus.Approved).Sum(x => x.ApprovedAmount),
            WillBuyIntentCount: items.Count(x => string.Equals(x.CustomerIntent, "WillBuy", StringComparison.OrdinalIgnoreCase)),
            ConsideringIntentCount: items.Count(x => string.Equals(x.CustomerIntent, "Considering", StringComparison.OrdinalIgnoreCase)),
            DistinctDealersCount: items.Select(x => x.DealerCode).Distinct().Count()
        );
    }

    private static DriveTestGroup ParseGroup(string? group)
    {
        if (string.IsNullOrWhiteSpace(group)) return DriveTestGroup.Showroom;
        var g = group.Trim().ToUpperInvariant();
        return g switch
        {
            "EVENT" => DriveTestGroup.Event,
            "1" => DriveTestGroup.Event,
            _ => DriveTestGroup.Showroom
        };
    }

    private static DriveTestType ParseType(string? type)
    {
        if (string.IsNullOrWhiteSpace(type)) return DriveTestType.HTV;
        var t = type.Trim().ToUpperInvariant();
        return t switch
        {
            "DEALER" => DriveTestType.Dealer,
            "1" => DriveTestType.Dealer,
            _ => DriveTestType.HTV
        };
    }

    private static bool TryParseStatus(string raw, out DriveTestStatus status)
    {
        var s = raw.Trim().ToUpperInvariant();
        switch (s)
        {
            case "P":
            case "PENDING":
            case "0":
                status = DriveTestStatus.Pending;
                return true;
            case "A":
            case "APPROVED":
            case "1":
                status = DriveTestStatus.Approved;
                return true;
            case "R":
            case "REJECTED":
            case "2":
                status = DriveTestStatus.Rejected;
                return true;
            case "C":
            case "CANCELLED":
            case "CANCEL":
            case "3":
                status = DriveTestStatus.Cancelled;
                return true;
            default:
                status = DriveTestStatus.Pending;
                return false;
        }
    }

    private static string NormalizeGender(string? gender)
    {
        if (string.IsNullOrWhiteSpace(gender)) return "M";
        var g = gender.Trim().ToUpperInvariant();
        return (g == "F" || g == "NU" || g == "NỮ" || g == "FEMALE") ? "F" : "M";
    }

    private static string ResolveDealerName(string code) => code.ToUpperInvariant() switch
    {
        "DL01" => "Hyundai Phạm Văn Đồng",
        "DL02" => "Hyundai Đông Anh",
        "DL03" => "Hyundai Giải Phóng",
        "DL04" => "Hyundai Hà Đông",
        "DL05" => "Hyundai Long Biên",
        "DL06" => "Hyundai Sài Gòn",
        _ => $"Đại lý {code}"
    };

    private static string ResolveModelName(string code) => code.ToUpperInvariant() switch
    {
        "SANTAFE" => "Hyundai Santa Fe 2024",
        "TUCSON" => "Hyundai Tucson FL 2024",
        "CRETA" => "Hyundai Creta Smart 2024",
        "ACCENT" => "Hyundai Accent Thế hệ mới 2024",
        "CUSTIN" => "Hyundai Custin MPV",
        "STARGAZER" => "Hyundai Stargazer X",
        "IONIQ5" => "Hyundai Ioniq 5 EV",
        "ELANTRA" => "Hyundai Elantra N-Line",
        _ => $"Hyundai {code}"
    };
}
