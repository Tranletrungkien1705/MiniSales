using Microsoft.EntityFrameworkCore;
using MiniSales.Data;
using MiniSales.Models;

namespace MiniSales.Services;

public record InsuranceCompanyDto(
    string Code,
    string Name,
    string ShortName,
    string? TaxNo = null,
    string? Phone = null,
    string? Address = null
);

public record InsuranceTypeDto(
    string Code,
    string Name,
    string Description,
    decimal DefaultRate
);

public record CreateCarInsuranceDetailDto(
    string Vin,
    string? CarId = null,
    string? ModelCode = null,
    string? ModelName = null,
    string? SpecCode = null,
    string? SpecDescription = null,
    string? ColorCode = null,
    string? ColorName = null,
    string? EngineNo = null,
    CarInsuranceRefOrdType RefOrdType = CarInsuranceRefOrdType.CarTransport,
    string RefOrdNo = "",
    string? TransporterCode = null,
    string? TransporterName = null,
    string? LocationFrom = null,
    string? LocationTo = null,
    DateTime? ExpectedStartDate = null,
    decimal Price = 0,
    decimal Rate = 0.15m,
    int InsuranceDay = 30,
    decimal InsAmount = 0,
    string? Remark = null
);

public record CreateCarInsuranceRequestDto(
    string? InsReqNo = null,
    string InsCompanyCode = "",
    string? InsCompanyName = null,
    string InsTypeCode = "",
    string? InsTypeName = null,
    DateTime? EffectiveDate = null,
    DateTime? ExpiryDate = null,
    string? Remark = null,
    string? CreatedBy = null,
    List<CreateCarInsuranceDetailDto>? Cars = null
);

public record UpdateCarInsuranceDetailDto(
    int InsuranceDay,
    decimal InsAmount,
    string? Remark = null
);

public record ApproveCarInsuranceRequestDto(
    string? ApprovedBy = null,
    string? Remark = null,
    string? PolicyNo = null,
    DateTime? PolicyDate = null
);

public record RejectCarInsuranceRequestDto(
    string Reason,
    string? RejectedBy = null
);

public record CancelCarInsuranceRequestDto(
    string Reason,
    string? CancelledBy = null
);

public record AddCarInsuranceDetailDto(
    string Vin,
    string? CarId = null,
    string? ModelCode = null,
    string? ModelName = null,
    string? SpecCode = null,
    string? SpecDescription = null,
    string? ColorCode = null,
    string? ColorName = null,
    string? EngineNo = null,
    CarInsuranceRefOrdType RefOrdType = CarInsuranceRefOrdType.CarTransport,
    string RefOrdNo = "",
    string? TransporterCode = null,
    string? TransporterName = null,
    string? LocationFrom = null,
    string? LocationTo = null,
    DateTime? ExpectedStartDate = null,
    decimal Price = 0,
    decimal Rate = 0.15m,
    int InsuranceDay = 30,
    decimal InsAmount = 0,
    string? Remark = null
);

public record CarInsuranceDetailDto(
    long Id,
    string InsReqNo,
    string CarId,
    string Vin,
    string? ModelCode,
    string ModelName,
    string? SpecCode,
    string? SpecDescription,
    string? ColorCode,
    string? ColorName,
    string? EngineNo,
    string RefOrdType,
    string RefOrdNo,
    string? TransporterCode,
    string? TransporterName,
    string? LocationFrom,
    string? LocationTo,
    DateTime? ExpectedStartDate,
    decimal Price,
    decimal Rate,
    int InsuranceDay,
    decimal InsAmount,
    string? PolicyNo,
    string Status,
    string? Remark,
    string? ApprovedBy,
    DateTime? ApprovedDate
);

public record CarInsuranceRequestDto(
    long Id,
    string InsReqNo,
    string InsCompanyCode,
    string? InsCompanyName,
    string InsTypeCode,
    string? InsTypeName,
    DateTime EffectiveDate,
    DateTime? ExpiryDate,
    string Status,
    int TotalCars,
    int ApprovedCars,
    decimal TotalAmount,
    decimal TotalPremiumAmount,
    string? PolicyNo,
    DateTime? PolicyDate,
    string? Remark,
    string? CreatedBy,
    DateTime CreatedDate,
    string? ApprovedBy,
    DateTime? ApprovedDate,
    string? RejectedBy,
    DateTime? RejectedDate,
    string? RejectReason,
    string? CancelledBy,
    DateTime? CancelledDate,
    string? CancelReason,
    List<CarInsuranceDetailDto> Details
);

public record EligibleInsuranceCarDto(
    string Vin,
    string CarId,
    string? ModelCode,
    string ModelName,
    string? SpecCode,
    string? SpecDescription,
    string? ColorCode,
    string? ColorName,
    string RefOrdType,
    string RefOrdNo,
    string? LocationFrom,
    string? LocationTo,
    string? TransporterCode,
    string? TransporterName,
    decimal Price,
    DateTime? ExpectedStartDate
);

public record CarInsuranceStatsDto(
    int TotalRequests,
    int TotalApproved,
    int TotalPending,
    int TotalRejected,
    int TotalCancelled,
    int TotalCars,
    int TotalCarsApproved,
    decimal TotalInsuredValue,
    decimal TotalPremiumAmount,
    Dictionary<string, int> ByStatus,
    Dictionary<string, int> ByCompany,
    Dictionary<string, int> ByType,
    Dictionary<string, int> ByRefOrdType
);

public interface ICarInsuranceService
{
    Task<List<CarInsuranceRequestDto>> ListAsync(
        string? status,
        string? insCompanyCode,
        string? insTypeCode,
        string? refOrdType,
        string? vin,
        string? insReqNo,
        DateTime? dateFrom,
        DateTime? dateTo
    );
    Task<CarInsuranceRequestDto?> GetByNoAsync(string insReqNo);
    Task<CarInsuranceRequestDto> CreateAsync(CreateCarInsuranceRequestDto dto);
    Task<CarInsuranceRequestDto> UpdateDetailAsync(string insReqNo, string vin, UpdateCarInsuranceDetailDto dto);
    Task<CarInsuranceRequestDto> ApproveAsync(string insReqNo, ApproveCarInsuranceRequestDto? dto);
    Task<CarInsuranceRequestDto> RejectAsync(string insReqNo, RejectCarInsuranceRequestDto dto);
    Task<CarInsuranceRequestDto> CancelAsync(string insReqNo, CancelCarInsuranceRequestDto dto);
    Task<CarInsuranceRequestDto> AddCarAsync(string insReqNo, AddCarInsuranceDetailDto dto);
    Task<CarInsuranceRequestDto?> RemoveCarAsync(string insReqNo, string vin);
    Task<bool> DeleteDraftAsync(string insReqNo);
    Task<List<EligibleInsuranceCarDto>> GetEligibleCarsAsync(string? refOrdType, string? keyword);
    Task<CarInsuranceStatsDto> GetStatsAsync();
    List<InsuranceCompanyDto> GetCompanies();
    List<InsuranceTypeDto> GetInsuranceTypes();
}

public sealed class CarInsuranceService(AppDbContext db, ITenantContext tenant) : ICarInsuranceService
{
    private Guid Org => tenant.OrgId;

    private static readonly List<InsuranceCompanyDto> Companies =
    [
        new("BIC", "Tổng công ty Bảo hiểm BIDV", "BIDV Insurance", "0101890251", "1900 9456", "Tháp BIDV, 194 Trần Quang Khải, Hoàn Kiếm, Hà Nội"),
        new("PTI", "Tổng công ty Cổ phần Bảo hiểm Bưu điện", "Bưu Điện PTI", "0100792731", "1900 545475", "Tầng 8, Tòa nhà Harec, 4A Láng Hạ, Ba Đình, Hà Nội"),
        new("PJICO", "Tổng công ty Cổ phần Bảo hiểm Petrolimex", "Bảo hiểm PJICO", "0100827290", "1900 545455", "Tầng 21-22, Tòa nhà MIPEC, 229 Tây Sơn, Đống Đa, Hà Nội"),
        new("BV", "Tổng công ty Bảo hiểm Bảo Việt", "Bảo Việt", "0100111761", "1900 558899", "Số 8 Lê Thái Tổ, Hàng Trống, Hoàn Kiếm, Hà Nội"),
        new("MIC", "Tổng công ty Cổ phần Bảo hiểm Quân đội", "Bảo hiểm MIC", "0102422055", "1900 558891", "Tầng 15, Tòa nhà MIPEC Tower, 229 Tây Sơn, Đống Đa, Hà Nội")
    ];

    private static readonly List<InsuranceTypeDto> InsuranceTypes =
    [
        new("BHVC", "Bảo hiểm vật chất xe ô tô", "Bảo hiểm thiệt hại thân vỏ và động cơ xe ô tô thương mại và du lịch", 0.25m),
        new("BHTNDS", "Bảo hiểm TNDS bắt buộc", "Bảo hiểm trách nhiệm dân sự bắt buộc của chủ xe ô tô theo quy chuẩn nhà nước", 0.05m),
        new("BHVT", "Bảo hiểm hàng hóa vận tải", "Bảo hiểm rủi ro mất mát, hư hỏng trong quá trình vận chuyển ô tô đường bộ", 0.15m),
        new("BHTN", "Bảo hiểm tai nạn lái phụ xe", "Bảo hiểm sinh mạng và thân thể người ngồi trên xe trong quá trình chạy thử/vận tải", 0.02m)
    ];

    public List<InsuranceCompanyDto> GetCompanies() => Companies;

    public List<InsuranceTypeDto> GetInsuranceTypes() => InsuranceTypes;

    public async Task<List<CarInsuranceRequestDto>> ListAsync(
        string? status,
        string? insCompanyCode,
        string? insTypeCode,
        string? refOrdType,
        string? vin,
        string? insReqNo,
        DateTime? dateFrom,
        DateTime? dateTo)
    {
        var q = db.InsuranceRequests
            .Include(x => x.Details)
            .Where(x => x.OrgId == Org);

        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<CarInsuranceStatus>(status, true, out var st))
            q = q.Where(x => x.Status == st);

        if (!string.IsNullOrWhiteSpace(insCompanyCode))
            q = q.Where(x => x.InsCompanyCode == insCompanyCode.Trim().ToUpperInvariant());

        if (!string.IsNullOrWhiteSpace(insTypeCode))
            q = q.Where(x => x.InsTypeCode == insTypeCode.Trim().ToUpperInvariant());

        if (!string.IsNullOrWhiteSpace(insReqNo))
            q = q.Where(x => x.InsReqNo.Contains(insReqNo.Trim().ToUpperInvariant()));

        if (!string.IsNullOrWhiteSpace(refOrdType) && Enum.TryParse<CarInsuranceRefOrdType>(refOrdType, true, out var rt))
            q = q.Where(x => x.Details.Any(d => d.RefOrdType == rt));

        if (!string.IsNullOrWhiteSpace(vin))
            q = q.Where(x => x.Details.Any(d => d.Vin.Contains(vin.Trim().ToUpperInvariant())));

        if (dateFrom.HasValue)
            q = q.Where(x => x.CreatedDate >= dateFrom.Value.Date);

        if (dateTo.HasValue)
            q = q.Where(x => x.CreatedDate < dateTo.Value.Date.AddDays(1));

        var list = await q.OrderByDescending(x => x.CreatedDate).ToListAsync();
        return list.Select(ToDto).ToList();
    }

    public async Task<CarInsuranceRequestDto?> GetByNoAsync(string insReqNo)
    {
        var item = await db.InsuranceRequests
            .Include(x => x.Details)
            .FirstOrDefaultAsync(x => x.OrgId == Org && x.InsReqNo == insReqNo.Trim().ToUpperInvariant());

        return item is null ? null : ToDto(item);
    }

    public async Task<CarInsuranceRequestDto> CreateAsync(CreateCarInsuranceRequestDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.InsCompanyCode))
            throw new InvalidOperationException("Mã công ty bảo hiểm không được để trống.");

        if (string.IsNullOrWhiteSpace(dto.InsTypeCode))
            throw new InvalidOperationException("Mã loại hình bảo hiểm không được để trống.");

        if (dto.Cars is null || dto.Cars.Count == 0)
            throw new InvalidOperationException("Danh sách xe yêu cầu bảo hiểm không được để trống.");

        string reqNo = string.IsNullOrWhiteSpace(dto.InsReqNo)
            ? await GenerateSeqNoAsync()
            : dto.InsReqNo.Trim().ToUpperInvariant();

        if (await db.InsuranceRequests.AnyAsync(x => x.OrgId == Org && x.InsReqNo == reqNo))
            throw new InvalidOperationException($"Mã yêu cầu bảo hiểm '{reqNo}' đã tồn tại trên hệ thống.");

        var comp = Companies.FirstOrDefault(c => string.Equals(c.Code, dto.InsCompanyCode.Trim(), StringComparison.OrdinalIgnoreCase));
        string companyName = dto.InsCompanyName ?? comp?.Name ?? dto.InsCompanyCode.Trim().ToUpperInvariant();

        var insType = InsuranceTypes.FirstOrDefault(t => string.Equals(t.Code, dto.InsTypeCode.Trim(), StringComparison.OrdinalIgnoreCase));
        string typeName = dto.InsTypeName ?? insType?.Name ?? dto.InsTypeCode.Trim().ToUpperInvariant();
        decimal defaultRate = insType?.DefaultRate ?? 0.15m;

        var seenVins = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var c in dto.Cars)
        {
            if (string.IsNullOrWhiteSpace(c.Vin))
                throw new InvalidOperationException("Số khung VIN không được để trống.");

            string vinUpper = c.Vin.Trim().ToUpperInvariant();
            if (!seenVins.Add(vinUpper))
                throw new InvalidOperationException($"Số khung VIN '{vinUpper}' bị trùng lặp trong cùng yêu cầu bảo hiểm.");

            if (c.InsuranceDay < 0)
                throw new InvalidOperationException($"Số ngày bảo hiểm của xe '{vinUpper}' không được âm ({c.InsuranceDay}).");

            bool activeElsewhere = await db.InsuranceRequestDetails
                .AnyAsync(d => d.InsReqNo != reqNo && d.Vin == vinUpper && d.RefOrdNo == c.RefOrdNo.Trim()
                               && d.Status != CarInsuranceDtlStatus.Rejected && d.Status != CarInsuranceDtlStatus.Cancelled);

            if (activeElsewhere)
                throw new InvalidOperationException($"Số khung VIN '{vinUpper}' theo lệnh '{c.RefOrdNo}' đã có yêu cầu bảo hiểm đang xử lý.");
        }

        DateTime effDate = dto.EffectiveDate ?? DateTime.Today;
        DateTime expDate = dto.ExpiryDate ?? effDate.AddMonths(1);

        var details = dto.Cars.Select(c =>
        {
            string vinUpper = c.Vin.Trim().ToUpperInvariant();
            decimal price = c.Price > 0 ? c.Price : 850_000_000m; // Default average car price if 0
            decimal rate = c.Rate > 0 ? c.Rate : defaultRate;
            int days = c.InsuranceDay > 0 ? c.InsuranceDay : 30;
            decimal premium = c.InsAmount > 0 ? c.InsAmount : Math.Round(price * rate / 100m, 0);

            return new CarInsuranceRequestDetail
            {
                InsReqNo = reqNo,
                CarId = string.IsNullOrWhiteSpace(c.CarId) ? $"CAR-{vinUpper}" : c.CarId.Trim().ToUpperInvariant(),
                Vin = vinUpper,
                ModelCode = c.ModelCode?.Trim(),
                ModelName = string.IsNullOrWhiteSpace(c.ModelName) ? "Hyundai Vehicle" : c.ModelName.Trim(),
                SpecCode = c.SpecCode?.Trim(),
                SpecDescription = c.SpecDescription?.Trim(),
                ColorCode = c.ColorCode?.Trim(),
                ColorName = c.ColorName?.Trim(),
                EngineNo = c.EngineNo?.Trim(),
                RefOrdType = c.RefOrdType,
                RefOrdNo = c.RefOrdNo.Trim(),
                TransporterCode = c.TransporterCode?.Trim() ?? "LOG-HTC",
                TransporterName = c.TransporterName?.Trim() ?? "Đội Vận Tải Nội Bộ HTC",
                LocationFrom = c.LocationFrom?.Trim() ?? "Kho Tổng NPP Hyundai Hà Nam",
                LocationTo = c.LocationTo?.Trim() ?? "Đại lý Hyundai",
                ExpectedStartDate = c.ExpectedStartDate ?? effDate,
                Price = price,
                Rate = rate,
                InsuranceDay = days,
                InsAmount = premium,
                Status = CarInsuranceDtlStatus.Pending,
                Remark = c.Remark?.Trim()
            };
        }).ToList();

        var request = new CarInsuranceRequest
        {
            OrgId = Org,
            InsReqNo = reqNo,
            InsCompanyCode = dto.InsCompanyCode.Trim().ToUpperInvariant(),
            InsCompanyName = companyName,
            InsTypeCode = dto.InsTypeCode.Trim().ToUpperInvariant(),
            InsTypeName = typeName,
            EffectiveDate = effDate,
            ExpiryDate = expDate,
            TotalCars = details.Count,
            ApprovedCars = 0,
            TotalAmount = details.Sum(d => d.Price),
            TotalPremiumAmount = details.Sum(d => d.InsAmount),
            Status = CarInsuranceStatus.Pending,
            Remark = dto.Remark?.Trim(),
            CreatedBy = dto.CreatedBy?.Trim() ?? "SALES_OPERATOR",
            CreatedDate = DateTime.Now,
            Details = details
        };

        db.InsuranceRequests.Add(request);
        await db.SaveChangesAsync();

        return ToDto(request);
    }

    public async Task<CarInsuranceRequestDto> UpdateDetailAsync(string insReqNo, string vin, UpdateCarInsuranceDetailDto dto)
    {
        var item = await db.InsuranceRequests
            .Include(x => x.Details)
            .FirstOrDefaultAsync(x => x.OrgId == Org && x.InsReqNo == insReqNo.Trim().ToUpperInvariant())
            ?? throw new InvalidOperationException($"Không tìm thấy yêu cầu bảo hiểm '{insReqNo}'.");

        if (item.Status != CarInsuranceStatus.Pending)
            throw new InvalidOperationException($"Yêu cầu bảo hiểm '{insReqNo}' đang ở trạng thái '{item.Status}' - chỉ được sửa khi đang chờ duyệt (Pending).");

        var detail = item.Details.FirstOrDefault(d => string.Equals(d.Vin, vin.Trim(), StringComparison.OrdinalIgnoreCase))
            ?? throw new InvalidOperationException($"Không tìm thấy xe VIN '{vin}' trong yêu cầu bảo hiểm '{insReqNo}'.");

        if (detail.Status != CarInsuranceDtlStatus.Pending)
            throw new InvalidOperationException($"Dòng xe '{vin}' không ở trạng thái chờ duyệt (Pending).");

        if (dto.InsuranceDay < 0)
            throw new InvalidOperationException("Số ngày bảo hiểm không được âm.");

        detail.InsuranceDay = dto.InsuranceDay;
        detail.InsAmount = dto.InsAmount > 0 ? dto.InsAmount : Math.Round(detail.Price * detail.Rate / 100m, 0);
        if (dto.Remark != null) detail.Remark = dto.Remark.Trim();

        item.TotalPremiumAmount = item.Details.Sum(d => d.InsAmount);
        item.LUDateTime = DateTime.Now;
        item.LUBy = "OPERATOR";

        await db.SaveChangesAsync();
        return ToDto(item);
    }

    public async Task<CarInsuranceRequestDto> ApproveAsync(string insReqNo, ApproveCarInsuranceRequestDto? dto)
    {
        var item = await db.InsuranceRequests
            .Include(x => x.Details)
            .FirstOrDefaultAsync(x => x.OrgId == Org && x.InsReqNo == insReqNo.Trim().ToUpperInvariant())
            ?? throw new InvalidOperationException($"Không tìm thấy yêu cầu bảo hiểm '{insReqNo}'.");

        if (item.Status != CarInsuranceStatus.Pending)
            throw new InvalidOperationException($"Yêu cầu bảo hiểm '{insReqNo}' không ở trạng thái chờ duyệt (Pending) - không thể phê duyệt.");

        string approver = dto?.ApprovedBy?.Trim() ?? "INS_OFFICER";
        DateTime now = DateTime.Now;
        string policyNo = dto?.PolicyNo?.Trim() ?? $"POL-{item.InsCompanyCode}-{DateTime.Today:yyMMdd}-{item.InsReqNo}";
        DateTime policyDate = dto?.PolicyDate ?? DateTime.Today;

        item.Status = CarInsuranceStatus.Approved;
        item.ApprovedBy = approver;
        item.ApprovedDate = now;
        item.PolicyNo = policyNo;
        item.PolicyDate = policyDate;
        item.ApprovedCars = item.Details.Count;
        if (!string.IsNullOrWhiteSpace(dto?.Remark))
            item.Remark = string.IsNullOrWhiteSpace(item.Remark) ? dto.Remark.Trim() : $"{item.Remark} | Duyệt: {dto.Remark.Trim()}";

        foreach (var d in item.Details)
        {
            d.Status = CarInsuranceDtlStatus.Approved;
            d.ApprovedBy = approver;
            d.ApprovedDate = now;
            d.PolicyNo = policyNo;
        }

        item.LUDateTime = now;
        item.LUBy = approver;

        await db.SaveChangesAsync();
        return ToDto(item);
    }

    public async Task<CarInsuranceRequestDto> RejectAsync(string insReqNo, RejectCarInsuranceRequestDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Reason))
            throw new InvalidOperationException("Lý do từ chối không được để trống.");

        var item = await db.InsuranceRequests
            .Include(x => x.Details)
            .FirstOrDefaultAsync(x => x.OrgId == Org && x.InsReqNo == insReqNo.Trim().ToUpperInvariant())
            ?? throw new InvalidOperationException($"Không tìm thấy yêu cầu bảo hiểm '{insReqNo}'.");

        if (item.Status != CarInsuranceStatus.Pending)
            throw new InvalidOperationException($"Yêu cầu bảo hiểm '{insReqNo}' không ở trạng thái chờ duyệt (Pending) - không thể từ chối.");

        string rejecter = dto.RejectedBy?.Trim() ?? "INS_OFFICER";
        DateTime now = DateTime.Now;

        item.Status = CarInsuranceStatus.Rejected;
        item.RejectReason = dto.Reason.Trim();
        item.RejectedBy = rejecter;
        item.RejectedDate = now;
        item.ApprovedCars = 0;

        foreach (var d in item.Details)
        {
            d.Status = CarInsuranceDtlStatus.Rejected;
        }

        item.LUDateTime = now;
        item.LUBy = rejecter;

        await db.SaveChangesAsync();
        return ToDto(item);
    }

    public async Task<CarInsuranceRequestDto> CancelAsync(string insReqNo, CancelCarInsuranceRequestDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Reason))
            throw new InvalidOperationException("Lý do hủy yêu cầu không được để trống.");

        var item = await db.InsuranceRequests
            .Include(x => x.Details)
            .FirstOrDefaultAsync(x => x.OrgId == Org && x.InsReqNo == insReqNo.Trim().ToUpperInvariant())
            ?? throw new InvalidOperationException($"Không tìm thấy yêu cầu bảo hiểm '{insReqNo}'.");

        if (item.Status == CarInsuranceStatus.Cancelled)
            throw new InvalidOperationException($"Yêu cầu bảo hiểm '{insReqNo}' đã bị hủy trước đó.");

        string canceller = dto.CancelledBy?.Trim() ?? "SALES_MANAGER";
        DateTime now = DateTime.Now;

        item.Status = CarInsuranceStatus.Cancelled;
        item.CancelReason = dto.Reason.Trim();
        item.CancelledBy = canceller;
        item.CancelledDate = now;
        item.ApprovedCars = 0;

        foreach (var d in item.Details)
        {
            d.Status = CarInsuranceDtlStatus.Cancelled;
        }

        item.LUDateTime = now;
        item.LUBy = canceller;

        await db.SaveChangesAsync();
        return ToDto(item);
    }

    public async Task<CarInsuranceRequestDto> AddCarAsync(string insReqNo, AddCarInsuranceDetailDto dto)
    {
        var item = await db.InsuranceRequests
            .Include(x => x.Details)
            .FirstOrDefaultAsync(x => x.OrgId == Org && x.InsReqNo == insReqNo.Trim().ToUpperInvariant())
            ?? throw new InvalidOperationException($"Không tìm thấy yêu cầu bảo hiểm '{insReqNo}'.");

        if (item.Status != CarInsuranceStatus.Pending)
            throw new InvalidOperationException($"Yêu cầu bảo hiểm '{insReqNo}' đang ở trạng thái '{item.Status}' - chỉ thêm xe được khi đang chờ duyệt.");

        string vinUpper = dto.Vin.Trim().ToUpperInvariant();
        if (item.Details.Any(d => string.Equals(d.Vin, vinUpper, StringComparison.OrdinalIgnoreCase)))
            throw new InvalidOperationException($"Xe VIN '{vinUpper}' đã có trong yêu cầu bảo hiểm '{insReqNo}'.");

        bool activeElsewhere = await db.InsuranceRequestDetails
            .AnyAsync(d => d.InsReqNo != item.InsReqNo && d.Vin == vinUpper && d.RefOrdNo == dto.RefOrdNo.Trim()
                           && d.Status != CarInsuranceDtlStatus.Rejected && d.Status != CarInsuranceDtlStatus.Cancelled);

        if (activeElsewhere)
            throw new InvalidOperationException($"Số khung VIN '{vinUpper}' theo lệnh '{dto.RefOrdNo}' đã có yêu cầu bảo hiểm đang xử lý.");

        var insType = InsuranceTypes.FirstOrDefault(t => string.Equals(t.Code, item.InsTypeCode, StringComparison.OrdinalIgnoreCase));
        decimal defaultRate = insType?.DefaultRate ?? 0.15m;
        decimal price = dto.Price > 0 ? dto.Price : 850_000_000m;
        decimal rate = dto.Rate > 0 ? dto.Rate : defaultRate;
        int days = dto.InsuranceDay > 0 ? dto.InsuranceDay : 30;
        decimal premium = dto.InsAmount > 0 ? dto.InsAmount : Math.Round(price * rate / 100m, 0);

        var newDetail = new CarInsuranceRequestDetail
        {
            InsReqNo = item.InsReqNo,
            CarId = string.IsNullOrWhiteSpace(dto.CarId) ? $"CAR-{vinUpper}" : dto.CarId.Trim().ToUpperInvariant(),
            Vin = vinUpper,
            ModelCode = dto.ModelCode?.Trim(),
            ModelName = string.IsNullOrWhiteSpace(dto.ModelName) ? "Hyundai Vehicle" : dto.ModelName.Trim(),
            SpecCode = dto.SpecCode?.Trim(),
            SpecDescription = dto.SpecDescription?.Trim(),
            ColorCode = dto.ColorCode?.Trim(),
            ColorName = dto.ColorName?.Trim(),
            EngineNo = dto.EngineNo?.Trim(),
            RefOrdType = dto.RefOrdType,
            RefOrdNo = dto.RefOrdNo.Trim(),
            TransporterCode = dto.TransporterCode?.Trim() ?? "LOG-HTC",
            TransporterName = dto.TransporterName?.Trim() ?? "Đội Vận Tải Nội Bộ HTC",
            LocationFrom = dto.LocationFrom?.Trim() ?? "Kho Tổng NPP Hyundai Hà Nam",
            LocationTo = dto.LocationTo?.Trim() ?? "Đại lý Hyundai",
            ExpectedStartDate = dto.ExpectedStartDate ?? item.EffectiveDate,
            Price = price,
            Rate = rate,
            InsuranceDay = days,
            InsAmount = premium,
            Status = CarInsuranceDtlStatus.Pending,
            Remark = dto.Remark?.Trim()
        };

        item.Details.Add(newDetail);
        item.TotalCars = item.Details.Count;
        item.TotalAmount = item.Details.Sum(d => d.Price);
        item.TotalPremiumAmount = item.Details.Sum(d => d.InsAmount);
        item.LUDateTime = DateTime.Now;
        item.LUBy = "OPERATOR";

        await db.SaveChangesAsync();
        return ToDto(item);
    }

    public async Task<CarInsuranceRequestDto?> RemoveCarAsync(string insReqNo, string vin)
    {
        var item = await db.InsuranceRequests
            .Include(x => x.Details)
            .FirstOrDefaultAsync(x => x.OrgId == Org && x.InsReqNo == insReqNo.Trim().ToUpperInvariant())
            ?? throw new InvalidOperationException($"Không tìm thấy yêu cầu bảo hiểm '{insReqNo}'.");

        if (item.Status != CarInsuranceStatus.Pending && item.Status != CarInsuranceStatus.Approved)
            throw new InvalidOperationException($"Yêu cầu bảo hiểm '{insReqNo}' ở trạng thái '{item.Status}' - không thể xóa dòng xe.");

        var detail = item.Details.FirstOrDefault(d => string.Equals(d.Vin, vin.Trim(), StringComparison.OrdinalIgnoreCase))
            ?? throw new InvalidOperationException($"Không tìm thấy xe VIN '{vin}' trong yêu cầu bảo hiểm '{insReqNo}'.");

        item.Details.Remove(detail);
        db.InsuranceRequestDetails.Remove(detail);

        // Quy tắc kế thừa từ Master.cs L44641: khi xóa hết xe trong chi tiết, tự động xóa luôn yêu cầu bảo hiểm master
        if (item.Details.Count == 0)
        {
            db.InsuranceRequests.Remove(item);
            await db.SaveChangesAsync();
            return null;
        }

        item.TotalCars = item.Details.Count;
        item.ApprovedCars = item.Details.Count(d => d.Status == CarInsuranceDtlStatus.Approved);
        item.TotalAmount = item.Details.Sum(d => d.Price);
        item.TotalPremiumAmount = item.Details.Sum(d => d.InsAmount);
        item.LUDateTime = DateTime.Now;
        item.LUBy = "OPERATOR";

        await db.SaveChangesAsync();
        return ToDto(item);
    }

    public async Task<bool> DeleteDraftAsync(string insReqNo)
    {
        var item = await db.InsuranceRequests
            .Include(x => x.Details)
            .FirstOrDefaultAsync(x => x.OrgId == Org && x.InsReqNo == insReqNo.Trim().ToUpperInvariant())
            ?? throw new InvalidOperationException($"Không tìm thấy yêu cầu bảo hiểm '{insReqNo}'.");

        if (item.Status != CarInsuranceStatus.Pending)
            throw new InvalidOperationException($"Yêu cầu bảo hiểm '{insReqNo}' không ở trạng thái chờ duyệt (Pending) - không thể xóa bỏ.");

        db.InsuranceRequests.Remove(item);
        await db.SaveChangesAsync();
        return true;
    }

    public async Task<List<EligibleInsuranceCarDto>> GetEligibleCarsAsync(string? refOrdType, string? keyword)
    {
        // Tra cứu xe từ 4 nguồn lệnh nghiệp vụ:
        // 1. CARTRANSPORT (Lệnh giao xe DeliveryOrder)
        // 2. CARRETRIEVE (Lệnh thu hồi xe CarRetrieveOrder)
        // 3. STORAGEREARRANGE (Lệnh chuyển kho StorageRearrangeOrder)
        // 4. STORAGEREARRCB (Lệnh chuyển đóng thùng StorageRearrangeCBOrder)
        var result = new List<EligibleInsuranceCarDto>();
        string kw = keyword?.Trim().ToUpperInvariant() ?? "";

        // Lấy danh sách VIN + RefOrdNo đã có bảo hiểm active
        var activePairs = await db.InsuranceRequestDetails
            .Where(d => d.Status != CarInsuranceDtlStatus.Rejected && d.Status != CarInsuranceDtlStatus.Cancelled)
            .Select(d => new { d.Vin, d.RefOrdNo })
            .ToListAsync();

        var activeSet = new HashSet<string>(
            activePairs.Select(p => $"{p.Vin.ToUpperInvariant()}|{p.RefOrdNo.ToUpperInvariant()}"),
            StringComparer.OrdinalIgnoreCase
        );

        bool checkAll = string.IsNullOrWhiteSpace(refOrdType);
        bool filterTransport = checkAll || string.Equals(refOrdType, "CARTRANSPORT", StringComparison.OrdinalIgnoreCase)
                               || string.Equals(refOrdType, "0", StringComparison.OrdinalIgnoreCase);
        bool filterRetrieve = checkAll || string.Equals(refOrdType, "CARRETRIEVE", StringComparison.OrdinalIgnoreCase)
                              || string.Equals(refOrdType, "1", StringComparison.OrdinalIgnoreCase);
        bool filterRearr = checkAll || string.Equals(refOrdType, "STORAGEREARRANGE", StringComparison.OrdinalIgnoreCase)
                           || string.Equals(refOrdType, "2", StringComparison.OrdinalIgnoreCase);
        bool filterRearrCB = checkAll || string.Equals(refOrdType, "STORAGEREARRCB", StringComparison.OrdinalIgnoreCase)
                             || string.Equals(refOrdType, "3", StringComparison.OrdinalIgnoreCase);

        // 1. CARTRANSPORT
        if (filterTransport)
        {
            var delOrders = await db.DeliveryOrders
                .Where(x => x.OrgId == Org && x.Status != DeliveryOrderStatus.Cancelled && !string.IsNullOrWhiteSpace(x.Vin))
                .ToListAsync();

            foreach (var d in delOrders)
            {
                string vin = d.Vin!.Trim().ToUpperInvariant();
                string key = $"{vin}|{d.DeliveryOrderNo.ToUpperInvariant()}";
                if (activeSet.Contains(key)) continue;

                if (!string.IsNullOrWhiteSpace(kw) && !vin.Contains(kw) && !d.DeliveryOrderNo.Contains(kw) && !d.Model.Contains(kw))
                    continue;

                result.Add(new EligibleInsuranceCarDto(
                    Vin: vin,
                    CarId: $"CAR-{vin}",
                    ModelCode: "HTV-CAR",
                    ModelName: d.Model,
                    SpecCode: "SPEC-STD",
                    SpecDescription: $"{d.Model} Tiêu chuẩn",
                    ColorCode: "WH",
                    ColorName: "Trắng",
                    RefOrdType: "CARTRANSPORT",
                    RefOrdNo: d.DeliveryOrderNo,
                    LocationFrom: "Kho Tổng NPP Hyundai Hà Nam",
                    LocationTo: string.IsNullOrWhiteSpace(d.DeliveryAddress) ? $"Đại lý {d.DealerCode}" : d.DeliveryAddress,
                    TransporterCode: string.IsNullOrWhiteSpace(d.TransporterName) ? "LOG-HTC" : "TRP-EXT",
                    TransporterName: d.TransporterName ?? "Đội Vận Tải Nội Bộ HTC",
                    Price: 850_000_000m,
                    ExpectedStartDate: d.DeliveryExpectedDate ?? DateTime.Today
                ));
            }
        }

        // 2. CARRETRIEVE
        if (filterRetrieve)
        {
            var retDetails = await db.CarRetrieveOrderDetails
                .Where(x => x.Status != CarRetrieveDtlStatus.Rejected && !string.IsNullOrWhiteSpace(x.Vin))
                .ToListAsync();

            var retHeaderMap = await db.CarRetrieveOrders
                .Where(x => x.OrgId == Org && x.Status != CarRetrieveStatus.Cancelled)
                .ToDictionaryAsync(x => x.Id, x => x);

            foreach (var rd in retDetails)
            {
                if (!retHeaderMap.TryGetValue(rd.RetrieveOrderId, out var rh)) continue;

                string vin = rd.Vin.Trim().ToUpperInvariant();
                string key = $"{vin}|{rh.RetrieveOrderNo.ToUpperInvariant()}";
                if (activeSet.Contains(key)) continue;

                if (!string.IsNullOrWhiteSpace(kw) && !vin.Contains(kw) && !rh.RetrieveOrderNo.Contains(kw) && !rd.Model.Contains(kw))
                    continue;

                result.Add(new EligibleInsuranceCarDto(
                    Vin: vin,
                    CarId: rd.CarId ?? $"CAR-{vin}",
                    ModelCode: "RET-CAR",
                    ModelName: rd.Model,
                    SpecCode: "SPEC-STD",
                    SpecDescription: $"{rd.Model} Thu hồi",
                    ColorCode: rd.Color,
                    ColorName: rd.Color,
                    RefOrdType: "CARRETRIEVE",
                    RefOrdNo: rh.RetrieveOrderNo,
                    LocationFrom: $"Đại lý {rh.DealerCode}",
                    LocationTo: rh.StorageName,
                    TransporterCode: "LOG-HTC",
                    TransporterName: "Vận Tải Chuyên Dùng HTC",
                    Price: 750_000_000m,
                    ExpectedStartDate: rd.ExpectedStartDate ?? rh.CreatedAt
                ));
            }
        }

        // 3. STORAGEREARRANGE
        if (filterRearr)
        {
            var rearrDetails = await db.StorageRearrangeDetails
                .Where(x => x.Status != StorageRearrangeDtlStatus.Rejected && !string.IsNullOrWhiteSpace(x.Vin))
                .ToListAsync();

            var rearrHeaderMap = await db.StorageRearranges
                .Where(x => x.OrgId == Org && x.Status != StorageRearrangeStatus.Cancelled)
                .ToDictionaryAsync(x => x.Id, x => x);

            foreach (var sd in rearrDetails)
            {
                if (!rearrHeaderMap.TryGetValue(sd.RearrangeId, out var sh)) continue;

                string vin = sd.Vin.Trim().ToUpperInvariant();
                string key = $"{vin}|{sh.RearrangeNo.ToUpperInvariant()}";
                if (activeSet.Contains(key)) continue;

                if (!string.IsNullOrWhiteSpace(kw) && !vin.Contains(kw) && !sh.RearrangeNo.Contains(kw) && !sd.Model.Contains(kw))
                    continue;

                result.Add(new EligibleInsuranceCarDto(
                    Vin: vin,
                    CarId: sd.CarId ?? $"CAR-{vin}",
                    ModelCode: "HTV-CAR",
                    ModelName: sd.Model,
                    SpecCode: sd.SpecCode,
                    SpecDescription: $"{sd.Model} Tiêu chuẩn",
                    ColorCode: sd.ColorCode,
                    ColorName: sd.ColorCode,
                    RefOrdType: "STORAGEREARRANGE",
                    RefOrdNo: sh.RearrangeNo,
                    LocationFrom: sh.StorageCodeFrom,
                    LocationTo: sh.StorageCodeTo,
                    TransporterCode: "LOG-HTC",
                    TransporterName: "Đội Điều Vận Kho Bãi",
                    Price: 820_000_000m,
                    ExpectedStartDate: sd.ExpectedStartDate ?? sh.CreatedAt
                ));
            }
        }

        // 4. STORAGEREARRCB
        if (filterRearrCB)
        {
            var rcbDetails = await db.StorageRearrangeCBDetails
                .Where(x => x.Status != StoRearrangeCBDtlStatus.Cancelled && !string.IsNullOrWhiteSpace(x.Vin))
                .ToListAsync();

            var rcbHeaderMap = await db.StorageRearrangeCBOrders
                .Where(x => x.OrgId == Org && x.Status != StoRearrangeCBStatus.Cancelled)
                .ToDictionaryAsync(x => x.Id, x => x);

            foreach (var cd in rcbDetails)
            {
                if (!rcbHeaderMap.TryGetValue(cd.StoRearCBId, out var ch)) continue;

                string vin = cd.Vin.Trim().ToUpperInvariant();
                string key = $"{vin}|{ch.StoRearCBNo.ToUpperInvariant()}";
                if (activeSet.Contains(key)) continue;

                if (!string.IsNullOrWhiteSpace(kw) && !vin.Contains(kw) && !ch.StoRearCBNo.Contains(kw) && !cd.Model.Contains(kw))
                    continue;

                result.Add(new EligibleInsuranceCarDto(
                    Vin: vin,
                    CarId: cd.CarId ?? $"CAR-{vin}",
                    ModelCode: cd.ModelCode,
                    ModelName: cd.Model,
                    SpecCode: cd.SpecCode,
                    SpecDescription: cd.SpecDescription,
                    ColorCode: cd.ColorCode,
                    ColorName: cd.ColorName,
                    RefOrdType: "STORAGEREARRCB",
                    RefOrdNo: ch.StoRearCBNo,
                    LocationFrom: cd.StorageCodeFrom,
                    LocationTo: cd.StorageCodeTo,
                    TransporterCode: "LOG-HTC",
                    TransporterName: "Đội Vận Chuyển Chassis Chuyên Nghiệp",
                    Price: 620_000_000m,
                    ExpectedStartDate: cd.ExpectedStartDate
                ));
            }
        }

        // Fallback demo/reserve cars from CarVinInventories if total results is small
        if (result.Count < 3)
        {
            var inventories = await db.CarVinInventories
                .Where(x => x.OrgId == Org && x.Status == CarVinStatus.Available)
                .Take(5)
                .ToListAsync();

            int fallbackSeq = 1;
            foreach (var inv in inventories)
            {
                string vin = inv.Vin.Trim().ToUpperInvariant();
                string refNo = $"DO-FALLBACK-{fallbackSeq:D3}";
                string key = $"{vin}|{refNo}";
                if (activeSet.Contains(key)) continue;

                result.Add(new EligibleInsuranceCarDto(
                    Vin: vin,
                    CarId: $"CAR-{vin}",
                    ModelCode: inv.ModelCode,
                    ModelName: inv.ModelName,
                    SpecCode: inv.SpecCode,
                    SpecDescription: $"{inv.ModelName} Bản Đặc Biệt",
                    ColorCode: inv.ColorCode,
                    ColorName: inv.ColorName,
                    RefOrdType: "CARTRANSPORT",
                    RefOrdNo: refNo,
                    LocationFrom: inv.StorageCode,
                    LocationTo: "Hyundai Đông Đô",
                    TransporterCode: "LOG-HTC",
                    TransporterName: "Đội Vận Tải Dự Phòng HTC",
                    Price: 890_000_000m,
                    ExpectedStartDate: DateTime.Today
                ));
                fallbackSeq++;
            }
        }

        return result;
    }

    public async Task<CarInsuranceStatsDto> GetStatsAsync()
    {
        var items = await db.InsuranceRequests
            .Include(x => x.Details)
            .Where(x => x.OrgId == Org)
            .ToListAsync();

        int totalRequests = items.Count;
        int totalApproved = items.Count(x => x.Status == CarInsuranceStatus.Approved);
        int totalPending = items.Count(x => x.Status == CarInsuranceStatus.Pending);
        int totalRejected = items.Count(x => x.Status == CarInsuranceStatus.Rejected);
        int totalCancelled = items.Count(x => x.Status == CarInsuranceStatus.Cancelled);

        int totalCars = items.Sum(x => x.TotalCars);
        int totalCarsApproved = items.Sum(x => x.ApprovedCars);
        decimal totalInsuredValue = items.Sum(x => x.TotalAmount);
        decimal totalPremiumAmount = items.Sum(x => x.TotalPremiumAmount);

        var byStatus = items.GroupBy(x => x.Status.ToString())
            .ToDictionary(g => g.Key, g => g.Count());

        var byCompany = items.GroupBy(x => x.InsCompanyCode)
            .ToDictionary(g => g.Key, g => g.Count());

        var byType = items.GroupBy(x => x.InsTypeCode)
            .ToDictionary(g => g.Key, g => g.Count());

        var byRefOrdType = items.SelectMany(x => x.Details)
            .GroupBy(d => d.RefOrdType.ToString())
            .ToDictionary(g => g.Key, g => g.Count());

        return new CarInsuranceStatsDto(
            TotalRequests: totalRequests,
            TotalApproved: totalApproved,
            TotalPending: totalPending,
            TotalRejected: totalRejected,
            TotalCancelled: totalCancelled,
            TotalCars: totalCars,
            TotalCarsApproved: totalCarsApproved,
            TotalInsuredValue: totalInsuredValue,
            TotalPremiumAmount: totalPremiumAmount,
            ByStatus: byStatus,
            ByCompany: byCompany,
            ByType: byType,
            ByRefOrdType: byRefOrdType
        );
    }

    private async Task<string> GenerateSeqNoAsync()
    {
        string prefix = $"{DateTime.Today:yyMM}IN";
        var last = await db.InsuranceRequests
            .Where(x => x.OrgId == Org && x.InsReqNo.StartsWith(prefix))
            .OrderByDescending(x => x.InsReqNo)
            .Select(x => x.InsReqNo)
            .FirstOrDefaultAsync();

        int nextSeq = 1;
        if (!string.IsNullOrWhiteSpace(last) && last.Length >= prefix.Length + 5)
        {
            string numPart = last.Substring(prefix.Length, 5);
            if (int.TryParse(numPart, out int parsed))
                nextSeq = parsed + 1;
        }

        return $"{prefix}{nextSeq:D5}";
    }

    private static CarInsuranceRequestDto ToDto(CarInsuranceRequest entity)
    {
        return new CarInsuranceRequestDto(
            Id: entity.Id,
            InsReqNo: entity.InsReqNo,
            InsCompanyCode: entity.InsCompanyCode,
            InsCompanyName: entity.InsCompanyName,
            InsTypeCode: entity.InsTypeCode,
            InsTypeName: entity.InsTypeName,
            EffectiveDate: entity.EffectiveDate,
            ExpiryDate: entity.ExpiryDate,
            Status: entity.Status.ToString(),
            TotalCars: entity.TotalCars,
            ApprovedCars: entity.ApprovedCars,
            TotalAmount: entity.TotalAmount,
            TotalPremiumAmount: entity.TotalPremiumAmount,
            PolicyNo: entity.PolicyNo,
            PolicyDate: entity.PolicyDate,
            Remark: entity.Remark,
            CreatedBy: entity.CreatedBy,
            CreatedDate: entity.CreatedDate,
            ApprovedBy: entity.ApprovedBy,
            ApprovedDate: entity.ApprovedDate,
            RejectedBy: entity.RejectedBy,
            RejectedDate: entity.RejectedDate,
            RejectReason: entity.RejectReason,
            CancelledBy: entity.CancelledBy,
            CancelledDate: entity.CancelledDate,
            CancelReason: entity.CancelReason,
            Details: entity.Details.Select(d => new CarInsuranceDetailDto(
                Id: d.Id,
                InsReqNo: d.InsReqNo,
                CarId: d.CarId,
                Vin: d.Vin,
                ModelCode: d.ModelCode,
                ModelName: d.ModelName,
                SpecCode: d.SpecCode,
                SpecDescription: d.SpecDescription,
                ColorCode: d.ColorCode,
                ColorName: d.ColorName,
                EngineNo: d.EngineNo,
                RefOrdType: d.RefOrdType.ToString(),
                RefOrdNo: d.RefOrdNo,
                TransporterCode: d.TransporterCode,
                TransporterName: d.TransporterName,
                LocationFrom: d.LocationFrom,
                LocationTo: d.LocationTo,
                ExpectedStartDate: d.ExpectedStartDate,
                Price: d.Price,
                Rate: d.Rate,
                InsuranceDay: d.InsuranceDay,
                InsAmount: d.InsAmount,
                PolicyNo: d.PolicyNo,
                Status: d.Status.ToString(),
                Remark: d.Remark,
                ApprovedBy: d.ApprovedBy,
                ApprovedDate: d.ApprovedDate
            )).ToList()
        );
    }
}
