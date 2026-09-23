using Microsoft.EntityFrameworkCore;
using MiniSales.Data;
using MiniSales.Models;

namespace MiniSales.Services;

public record SalesPolicyDetailInputDto(
    string DealerCode,
    string ModelCode,
    string? ModelName = null,
    string? SpecCode = null,
    string? SpecDescription = null,
    string? YearOfManufacture = null,
    decimal AmountSupport = 0,
    decimal SupportPercent = 0,
    int MinQty = 1,
    string? Remark = null
);

public record CreateSalesPolicyDto(
    string SPNo,
    DateTime StartDate,
    DateTime EndDate,
    SalesPolicyType SPSRType = SalesPolicyType.Retail,
    BusinessSupportForm FormBusinessSupportCode = BusinessSupportForm.Cash,
    string? SPSRCode = null,
    string? SPSRRoot = null,
    string? FilePath = null,
    string? FileName = null,
    string? Remark = null,
    string? CreatedBy = null,
    List<SalesPolicyDetailInputDto>? Details = null
);

public record UpdateSalesPolicyDto(
    string SPNo,
    DateTime StartDate,
    DateTime EndDate,
    SalesPolicyType SPSRType,
    BusinessSupportForm FormBusinessSupportCode,
    string? SPSRRoot = null,
    string? FilePath = null,
    string? FileName = null,
    string? Remark = null,
    string? FlagMstValid = null,
    List<SalesPolicyDetailInputDto>? Details = null
);

public record UpdatePolicyStatusDto(
    string FlagMstValid, // "1" = Valid, "0" = Inactive/Cancelled
    string? Remark = null
);

public record SupportRetailInputDto(
    string DealerCode,
    string SPSRCode,
    string Vin,
    decimal? AmountSupport = null,
    DateTime? DateSupport = null,
    DateTime? HTCDatePayment = null,
    string? Remark = null
);

public record CreateSupportRetailDto(
    List<SupportRetailInputDto> Items,
    string? CreatedBy = null
);

public record ApproveSupportRetailDto(
    decimal? AmountHTCAppr = null,
    string? ApprovedBy = null,
    string? Remark = null
);

public record BatchApproveSupportRetailDto(
    List<string> SupportCodes,
    string? ApprovedBy = null
);

public record PaySupportRetailDto(
    DateTime? HTCDatePayment = null,
    string? Remark = null
);

public record RejectSupportRetailDto(
    string Reason,
    string? RejectedBy = null
);

public record CancelSupportRetailDto(
    string Reason,
    string? CancelledBy = null
);

public record CalcDateFullStatusRequestDto(
    List<string> Vins,
    string? SPSRCode = null
);

public record FullStatusCalculatedRowDto(
    string Vin,
    string? CarId,
    string? DealerCode,
    string? DealerName,
    string? ModelCode,
    string? ModelName,
    decimal SuggestedSupportAmount,
    DateTime? OSODApprovedDate,
    string? OSODSOCode,
    string? HTCInvoiceNo,
    DateTime? HTCInvoiceDate,
    DateTime? CDODDeliveryOutDate,
    DateTime? DateFullStatus,
    string? PRDiscountNo,
    string? SPSRCode,
    string? SPNo,
    bool IsDutyCompleted,
    string? Remark
);

public record EligibleCarForPolicyDto(
    string Vin,
    string CarId,
    string ModelCode,
    string ModelName,
    string DealerCode,
    string DealerName,
    decimal UnitPriceActual,
    decimal SuggestedSupportAmount,
    string SPSRCode,
    string SPNo,
    bool HasExistingSupport,
    string? ExistingSupportCode,
    string? ExistingStatus
);

public interface ISalesPolicyService
{
    Task<string> GetNextPolicySeqAsync();
    Task<string> GetNextSupportSeqAsync();
    Task<object> ListPoliciesAsync(
        string? spsrCode = null,
        string? spNo = null,
        DateTime? startDateFrom = null,
        DateTime? startDateTo = null,
        SalesPolicyType? spsrType = null,
        BusinessSupportForm? supportForm = null,
        string? modelCode = null,
        string? dealerCode = null,
        SalesPolicyStatus? status = null,
        int page = 0,
        int pageSize = 10
    );
    Task<object?> GetPolicyByCodeAsync(string spsrCode);
    Task<object> CreatePolicyAsync(CreateSalesPolicyDto dto);
    Task<object?> UpdatePolicyAsync(string spsrCode, UpdateSalesPolicyDto dto);
    Task<object?> UpdatePolicyStatusAsync(string spsrCode, UpdatePolicyStatusDto dto);
    Task<bool> DeletePolicyAsync(string spsrCode);
    Task<object> GetEligibleCarsForPolicyAsync(string spsrCode, string? dealerCode = null);

    Task<object> CalcDateFullStatusAsync(CalcDateFullStatusRequestDto dto);
    Task<object> CreateSupportRetailAsync(CreateSupportRetailDto dto);
    Task<object?> DetailSupportRetailAsync(string supportCode);
    Task<object?> DetailSupportByVinAsync(string spsrCode, string vin);
    Task<object> ListSupportRetailsAsync(
        string? spsrCode = null,
        string? spNo = null,
        string? dealerCode = null,
        string? vin = null,
        DateTime? dateSupport = null,
        DateTime? createdDateFrom = null,
        DateTime? createdDateTo = null,
        SupportRetailStatus? status = null,
        int page = 0,
        int pageSize = 10
    );
    Task<object?> ApproveSupportRetailAsync(string supportCode, ApproveSupportRetailDto? dto = null);
    Task<object> BatchApproveSupportRetailAsync(BatchApproveSupportRetailDto dto);
    Task<object?> PaySupportRetailAsync(string supportCode, PaySupportRetailDto? dto = null);
    Task<object?> RejectSupportRetailAsync(string supportCode, RejectSupportRetailDto dto);
    Task<object?> CancelSupportRetailAsync(string supportCode, CancelSupportRetailDto dto);
    Task<bool> DeleteSupportRetailDraftAsync(string supportCode);
    Task<object> StatsAsync(string? dealerCode = null);
}

public class SalesPolicyService : ISalesPolicyService
{
    private readonly AppDbContext _db;
    private readonly ITenantContext _tenant;

    private static readonly Dictionary<string, string> DealerNames = new(StringComparer.OrdinalIgnoreCase)
    {
        { "VS058", "Hyundai Bình Dương" },
        { "VN012", "Hyundai Hà Đông" },
        { "VN065", "Hyundai Phạm Văn Đồng" },
        { "VN001", "Hyundai Ngọc An" },
        { "VN033", "Hyundai Sông Hàn Đà Nẵng" },
        { "ALL", "Toàn hệ thống Đại lý" }
    };

    public SalesPolicyService(AppDbContext db, ITenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    public async Task<string> GetNextPolicySeqAsync()
    {
        var datePrefix = DateTime.Now.ToString("yyMM");
        var count = await _db.SalesPolicies
            .Where(x => x.OrgId == _tenant.OrgId && x.SPSRCode.StartsWith("SPSR" + datePrefix))
            .CountAsync();
        return $"SPSR{datePrefix}{count + 1:D4}";
    }

    public async Task<string> GetNextSupportSeqAsync()
    {
        var datePrefix = DateTime.Now.ToString("yyMM");
        var count = await _db.SupportRetails
            .Where(x => x.OrgId == _tenant.OrgId && x.SupportCode.StartsWith(datePrefix + "SPR"))
            .CountAsync();
        return $"{datePrefix}SPR{count + 1:D5}";
    }

    public async Task<object> ListPoliciesAsync(
        string? spsrCode = null,
        string? spNo = null,
        DateTime? startDateFrom = null,
        DateTime? startDateTo = null,
        SalesPolicyType? spsrType = null,
        BusinessSupportForm? supportForm = null,
        string? modelCode = null,
        string? dealerCode = null,
        SalesPolicyStatus? status = null,
        int page = 0,
        int pageSize = 10)
    {
        var q = _db.SalesPolicies
            .Include(x => x.Details)
            .Where(x => x.OrgId == _tenant.OrgId);

        if (!string.IsNullOrWhiteSpace(spsrCode))
        {
            var code = spsrCode.Trim();
            q = q.Where(x => EF.Functions.Like(x.SPSRCode, $"%{code}%"));
        }

        if (!string.IsNullOrWhiteSpace(spNo))
        {
            var no = spNo.Trim();
            q = q.Where(x => EF.Functions.Like(x.SPNo, $"%{no}%"));
        }

        if (startDateFrom.HasValue)
            q = q.Where(x => x.StartDate >= startDateFrom.Value.Date);

        if (startDateTo.HasValue)
            q = q.Where(x => x.StartDate <= startDateTo.Value.Date.AddDays(1).AddTicks(-1));

        if (spsrType.HasValue)
            q = q.Where(x => x.SPSRType == spsrType.Value);

        if (supportForm.HasValue)
            q = q.Where(x => x.FormBusinessSupportCode == supportForm.Value);

        if (status.HasValue)
            q = q.Where(x => x.Status == status.Value);

        if (!string.IsNullOrWhiteSpace(modelCode))
        {
            var mCode = modelCode.Trim().ToUpperInvariant();
            q = q.Where(x => x.Details.Any(d => d.ModelCode.ToUpper() == mCode));
        }

        if (!string.IsNullOrWhiteSpace(dealerCode))
        {
            var dCode = dealerCode.Trim().ToUpperInvariant();
            q = q.Where(x => x.Details.Any(d => d.DealerCode.ToUpper() == dCode || d.DealerCode == "ALL"));
        }

        var total = await q.CountAsync();
        var items = await q.OrderByDescending(x => x.CreatedAt)
            .Skip(page * pageSize)
            .Take(pageSize)
            .Select(x => new
            {
                x.Id,
                x.SPSRCode,
                x.SPNo,
                x.StartDate,
                x.EndDate,
                SPSRType = x.SPSRType.ToString(),
                FormBusinessSupportCode = x.FormBusinessSupportCode.ToString(),
                x.SPSRRoot,
                x.FilePath,
                x.FileName,
                x.FlagMstValid,
                Status = x.Status.ToString(),
                x.Remark,
                x.ApprovedAt,
                x.ApprovedBy,
                x.CreatedAt,
                x.CreatedBy,
                DetailCount = x.Details.Count,
                Details = x.Details.Select(d => new
                {
                    d.Id,
                    d.DealerCode,
                    DealerName = DealerNames.GetValueOrDefault(d.DealerCode, d.DealerCode),
                    d.ModelCode,
                    d.ModelName,
                    d.SpecCode,
                    d.SpecDescription,
                    d.YearOfManufacture,
                    d.AmountSupport,
                    d.SupportPercent,
                    d.MinQty,
                    d.Remark
                }).ToList()
            })
            .ToListAsync();

        return new
        {
            total,
            page,
            pageSize,
            items
        };
    }

    public async Task<object?> GetPolicyByCodeAsync(string spsrCode)
    {
        var policy = await _db.SalesPolicies
            .Include(x => x.Details)
            .FirstOrDefaultAsync(x => x.OrgId == _tenant.OrgId && x.SPSRCode == spsrCode.Trim());

        if (policy is null) return null;

        return new
        {
            policy.Id,
            policy.SPSRCode,
            policy.SPNo,
            policy.StartDate,
            policy.EndDate,
            SPSRType = policy.SPSRType.ToString(),
            FormBusinessSupportCode = policy.FormBusinessSupportCode.ToString(),
            policy.SPSRRoot,
            policy.FilePath,
            policy.FileName,
            policy.FlagMstValid,
            Status = policy.Status.ToString(),
            policy.Remark,
            policy.ApprovedAt,
            policy.ApprovedBy,
            policy.CreatedAt,
            policy.CreatedBy,
            policy.LUDateTime,
            policy.LUBy,
            Details = policy.Details.Select(d => new
            {
                d.Id,
                d.DealerCode,
                DealerName = DealerNames.GetValueOrDefault(d.DealerCode, d.DealerCode),
                d.ModelCode,
                d.ModelName,
                d.SpecCode,
                d.SpecDescription,
                d.YearOfManufacture,
                d.AmountSupport,
                d.SupportPercent,
                d.MinQty,
                d.Remark
            }).ToList()
        };
    }

    public async Task<object> CreatePolicyAsync(CreateSalesPolicyDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.SPNo))
            throw new InvalidOperationException("Số hiệu văn bản quyết định (SPNo) là bắt buộc.");

        if (dto.EndDate < dto.StartDate)
            throw new InvalidOperationException("Ngày kết thúc hiệu lực chính sách (EndDate) phải >= ngày bắt đầu (StartDate).");

        var code = string.IsNullOrWhiteSpace(dto.SPSRCode) ? await GetNextPolicySeqAsync() : dto.SPSRCode.Trim().ToUpperInvariant();

        if (await _db.SalesPolicies.AnyAsync(x => x.OrgId == _tenant.OrgId && x.SPSRCode == code))
            throw new InvalidOperationException($"Mã chính sách {code} đã tồn tại trong hệ thống.");

        var policy = new SalesPolicyMaster
        {
            OrgId = _tenant.OrgId,
            SPSRCode = code,
            SPNo = dto.SPNo.Trim(),
            StartDate = dto.StartDate,
            EndDate = dto.EndDate,
            SPSRType = dto.SPSRType,
            FormBusinessSupportCode = dto.FormBusinessSupportCode,
            SPSRRoot = dto.SPSRRoot?.Trim(),
            FilePath = dto.FilePath?.Trim(),
            FileName = dto.FileName?.Trim(),
            FlagMstValid = "1",
            Status = SalesPolicyStatus.Valid,
            Remark = dto.Remark?.Trim(),
            ApprovedAt = DateTime.Now,
            ApprovedBy = dto.CreatedBy ?? "HTC_LEADER",
            CreatedAt = DateTime.Now,
            CreatedBy = dto.CreatedBy ?? "HTC_SALES_SPECIALIST"
        };

        if (dto.Details != null && dto.Details.Count > 0)
        {
            foreach (var d in dto.Details)
            {
                var dCode = string.IsNullOrWhiteSpace(d.DealerCode) ? "ALL" : d.DealerCode.Trim().ToUpperInvariant();
                var mCode = d.ModelCode.Trim().ToUpperInvariant();
                policy.Details.Add(new SalesPolicyDetail
                {
                    SPSRCode = code,
                    DealerCode = dCode,
                    ModelCode = mCode,
                    ModelName = d.ModelName?.Trim() ?? mCode,
                    SpecCode = d.SpecCode?.Trim(),
                    SpecDescription = d.SpecDescription?.Trim(),
                    YearOfManufacture = d.YearOfManufacture?.Trim(),
                    AmountSupport = d.AmountSupport,
                    SupportPercent = d.SupportPercent,
                    MinQty = d.MinQty > 0 ? d.MinQty : 1,
                    Remark = d.Remark?.Trim()
                });
            }
        }

        _db.SalesPolicies.Add(policy);
        await _db.SaveChangesAsync();

        return new
        {
            success = true,
            spsrCode = policy.SPSRCode,
            spNo = policy.SPNo,
            detailCount = policy.Details.Count,
            message = "Tạo mới chính sách bán hàng & hỗ trợ kinh doanh thành công."
        };
    }

    public async Task<object?> UpdatePolicyAsync(string spsrCode, UpdateSalesPolicyDto dto)
    {
        var policy = await _db.SalesPolicies
            .Include(x => x.Details)
            .FirstOrDefaultAsync(x => x.OrgId == _tenant.OrgId && x.SPSRCode == spsrCode.Trim());

        if (policy is null) return null;

        if (dto.EndDate < dto.StartDate)
            throw new InvalidOperationException("Ngày kết thúc hiệu lực chính sách (EndDate) phải >= ngày bắt đầu (StartDate).");

        policy.SPNo = dto.SPNo.Trim();
        policy.StartDate = dto.StartDate;
        policy.EndDate = dto.EndDate;
        policy.SPSRType = dto.SPSRType;
        policy.FormBusinessSupportCode = dto.FormBusinessSupportCode;
        policy.SPSRRoot = dto.SPSRRoot?.Trim();
        policy.FilePath = dto.FilePath?.Trim() ?? policy.FilePath;
        policy.FileName = dto.FileName?.Trim() ?? policy.FileName;
        policy.Remark = dto.Remark?.Trim();
        if (!string.IsNullOrWhiteSpace(dto.FlagMstValid))
        {
            policy.FlagMstValid = dto.FlagMstValid.Trim();
            policy.Status = policy.FlagMstValid == "1" ? SalesPolicyStatus.Valid : SalesPolicyStatus.Cancelled;
        }
        policy.LUDateTime = DateTime.Now;
        policy.LUBy = "HTC_SALES_ADMIN";

        if (dto.Details != null)
        {
            _db.SalesPolicyDetails.RemoveRange(policy.Details);
            policy.Details.Clear();

            foreach (var d in dto.Details)
            {
                var dCode = string.IsNullOrWhiteSpace(d.DealerCode) ? "ALL" : d.DealerCode.Trim().ToUpperInvariant();
                var mCode = d.ModelCode.Trim().ToUpperInvariant();
                policy.Details.Add(new SalesPolicyDetail
                {
                    SalesPolicyId = policy.Id,
                    SPSRCode = policy.SPSRCode,
                    DealerCode = dCode,
                    ModelCode = mCode,
                    ModelName = d.ModelName?.Trim() ?? mCode,
                    SpecCode = d.SpecCode?.Trim(),
                    SpecDescription = d.SpecDescription?.Trim(),
                    YearOfManufacture = d.YearOfManufacture?.Trim(),
                    AmountSupport = d.AmountSupport,
                    SupportPercent = d.SupportPercent,
                    MinQty = d.MinQty > 0 ? d.MinQty : 1,
                    Remark = d.Remark?.Trim()
                });
            }
        }

        await _db.SaveChangesAsync();

        return new
        {
            success = true,
            spsrCode = policy.SPSRCode,
            spNo = policy.SPNo,
            detailCount = policy.Details.Count,
            message = "Cập nhật chính sách bán hàng thành công."
        };
    }

    public async Task<object?> UpdatePolicyStatusAsync(string spsrCode, UpdatePolicyStatusDto dto)
    {
        var policy = await _db.SalesPolicies
            .FirstOrDefaultAsync(x => x.OrgId == _tenant.OrgId && x.SPSRCode == spsrCode.Trim());

        if (policy is null) return null;

        var flag = dto.FlagMstValid.Trim();
        policy.FlagMstValid = flag;
        policy.Status = flag == "1" ? SalesPolicyStatus.Valid : SalesPolicyStatus.Cancelled;
        if (!string.IsNullOrWhiteSpace(dto.Remark))
            policy.Remark = (policy.Remark != null ? policy.Remark + "; " : "") + dto.Remark.Trim();
        policy.LUDateTime = DateTime.Now;
        policy.LUBy = "HTC_SALES_ADMIN";

        await _db.SaveChangesAsync();

        return new
        {
            success = true,
            spsrCode = policy.SPSRCode,
            flagMstValid = policy.FlagMstValid,
            status = policy.Status.ToString(),
            message = flag == "1" ? "Kích hoạt hiệu lực chính sách thành công." : "Dừng hiệu lực chính sách thành công."
        };
    }

    public async Task<bool> DeletePolicyAsync(string spsrCode)
    {
        var policy = await _db.SalesPolicies
            .Include(x => x.Details)
            .FirstOrDefaultAsync(x => x.OrgId == _tenant.OrgId && x.SPSRCode == spsrCode.Trim());

        if (policy is null) return false;

        var hasSupports = await _db.SupportRetails
            .AnyAsync(x => x.OrgId == _tenant.OrgId && x.SPSRCode == policy.SPSRCode);

        if (hasSupports)
            throw new InvalidOperationException($"Không thể xóa chính sách {policy.SPSRCode} vì đã có hồ sơ đề nghị quyết toán hỗ trợ bán lẻ liên kết.");

        _db.SalesPolicies.Remove(policy);
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<object> GetEligibleCarsForPolicyAsync(string spsrCode, string? dealerCode = null)
    {
        var policy = await _db.SalesPolicies
            .Include(x => x.Details)
            .FirstOrDefaultAsync(x => x.OrgId == _tenant.OrgId && x.SPSRCode == spsrCode.Trim());

        if (policy is null)
            throw new InvalidOperationException($"Không tìm thấy chính sách {spsrCode}.");

        var policyDealers = policy.Details.Select(d => d.DealerCode.ToUpper()).ToHashSet();
        var isAllDealers = policyDealers.Contains("ALL");
        var policyModels = policy.Details.Select(d => d.ModelCode.ToUpper()).ToHashSet();

        // Query cars in database
        var carsQuery = _db.Cars.Where(x => x.OrgId == _tenant.OrgId && !string.IsNullOrEmpty(x.Vin));

        if (!string.IsNullOrWhiteSpace(dealerCode))
        {
            var dCode = dealerCode.Trim().ToUpperInvariant();
            carsQuery = carsQuery.Where(x => x.DealerCode.ToUpper() == dCode);
        }
        else if (!isAllDealers)
        {
            carsQuery = carsQuery.Where(x => policyDealers.Contains(x.DealerCode.ToUpper()));
        }

        if (policyModels.Count > 0)
        {
            carsQuery = carsQuery.Where(x => policyModels.Contains(x.ModelCode.ToUpper()));
        }

        var cars = await carsQuery.Take(30).ToListAsync();

        // Check existing supports for these VINs and this policy
        var vins = cars.Select(c => c.Vin).ToList();
        var existingSupports = await _db.SupportRetails
            .Where(x => x.OrgId == _tenant.OrgId && x.SPSRCode == policy.SPSRCode && vins.Contains(x.Vin))
            .ToDictionaryAsync(x => x.Vin, x => x);

        var list = new List<EligibleCarForPolicyDto>();
        foreach (var c in cars)
        {
            var detailMatch = policy.Details.FirstOrDefault(d =>
                (d.DealerCode == "ALL" || d.DealerCode.Equals(c.DealerCode, StringComparison.OrdinalIgnoreCase)) &&
                d.ModelCode.Equals(c.ModelCode, StringComparison.OrdinalIgnoreCase));

            var suggestedAmount = detailMatch?.AmountSupport ?? 15_000_000m;
            var hasExisting = existingSupports.TryGetValue(c.Vin, out var ext);

            list.Add(new EligibleCarForPolicyDto(
                Vin: c.Vin,
                CarId: c.CarId,
                ModelCode: c.ModelCode,
                ModelName: c.ModelName,
                DealerCode: c.DealerCode,
                DealerName: DealerNames.GetValueOrDefault(c.DealerCode, c.DealerName ?? c.DealerCode),
                UnitPriceActual: c.PriceActual > 0 ? c.PriceActual : 750_000_000m,
                SuggestedSupportAmount: suggestedAmount,
                SPSRCode: policy.SPSRCode,
                SPNo: policy.SPNo,
                HasExistingSupport: hasExisting && ext?.Status != SupportRetailStatus.Cancelled && ext?.Status != SupportRetailStatus.Rejected,
                ExistingSupportCode: ext?.SupportCode,
                ExistingStatus: ext?.Status.ToString()
            ));
        }

        return new
        {
            spsrCode = policy.SPSRCode,
            spNo = policy.SPNo,
            totalEligible = list.Count,
            cars = list
        };
    }

    public async Task<List<FullStatusCalculatedRowDto>> CalculateFullStatusRowsAsync(List<string> vins, string? spsrCode = null)
    {
        if (vins == null || vins.Count == 0)
            return new List<FullStatusCalculatedRowDto>();

        SalesPolicyMaster? policy = null;
        if (!string.IsNullOrWhiteSpace(spsrCode))
        {
            policy = await _db.SalesPolicies
                .Include(x => x.Details)
                .FirstOrDefaultAsync(x => x.OrgId == _tenant.OrgId && x.SPSRCode == spsrCode.Trim());
        }

        var cleanVins = vins.Select(v => v.Trim().ToUpperInvariant()).Distinct().ToList();

        var cars = await _db.Cars
            .Where(x => x.OrgId == _tenant.OrgId && cleanVins.Contains(x.Vin.ToUpper()))
            .ToDictionaryAsync(x => x.Vin.ToUpper(), x => x);

        // Fetch related info: Invoices, DeliveryOrders, Payments, Discounts
        var invoiceDtls = await _db.CarInvoiceDetails
            .Where(x => cleanVins.Contains(x.Vin.ToUpper()))
            .ToListAsync();
        var invoiceIds = invoiceDtls.Select(x => x.InvoiceId).Distinct().ToList();
        var invoiceMap = await _db.CarInvoices
            .Where(x => invoiceIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, x => x);
        var invoiceByVin = invoiceDtls.GroupBy(x => x.Vin.ToUpper()).ToDictionary(g => g.Key, g => g.OrderByDescending(i => i.Id).First());

        var deliveryOrders = await _db.DeliveryOrders
            .Where(x => x.OrgId == _tenant.OrgId && x.Vin != null && cleanVins.Contains(x.Vin.ToUpper()))
            .ToListAsync();
        var deliveryByVin = deliveryOrders.GroupBy(x => x.Vin!.ToUpper()).ToDictionary(g => g.Key, g => g.OrderByDescending(d => d.Id).First());

        var discountDtls = await _db.PaymentDiscountDetails
            .Where(x => cleanVins.Contains(x.Vin.ToUpper()))
            .ToListAsync();
        var discountByVin = discountDtls.GroupBy(x => x.Vin.ToUpper()).ToDictionary(g => g.Key, g => g.OrderByDescending(d => d.Id).First());

        var result = new List<FullStatusCalculatedRowDto>();

        foreach (var vin in cleanVins)
        {
            cars.TryGetValue(vin, out var car);
            invoiceByVin.TryGetValue(vin, out var invDtl);
            deliveryByVin.TryGetValue(vin, out var deliv);
            discountByVin.TryGetValue(vin, out var discDtl);

            CarInvoice? inv = null;
            if (invDtl != null) invoiceMap.TryGetValue(invDtl.InvoiceId, out inv);

            var carId = car?.CarId ?? $"CAR-{vin[..Math.Min(8, vin.Length)]}";
            var dCode = car?.DealerCode ?? (deliv?.DealerCode ?? (inv?.DealerCode ?? "VS058"));
            var dName = DealerNames.GetValueOrDefault(dCode, car?.DealerName ?? (inv?.DealerName ?? dCode));
            var mCode = car?.ModelCode ?? (invDtl?.Model ?? "SANTAFE");
            var mName = car?.ModelName ?? (invDtl?.Model ?? mCode);

            // Suggested support amount from policy if given
            decimal suggestedAmount = 0;
            if (policy != null)
            {
                var match = policy.Details.FirstOrDefault(d =>
                    (d.DealerCode == "ALL" || d.DealerCode.Equals(dCode, StringComparison.OrdinalIgnoreCase)) &&
                    d.ModelCode.Equals(mCode, StringComparison.OrdinalIgnoreCase));
                suggestedAmount = match?.AmountSupport ?? 20_000_000m;
            }

            // SO Code & Approved Date
            var soCode = car?.SoCode ?? (invDtl?.OrderNo ?? (deliv?.OrderCode ?? "SO2603010001"));
            var soDate = car?.CreatedAt.AddDays(-30);

            // HTC Invoice No & Date
            var htcInvNo = inv?.InvoiceNo ?? (inv?.InvoiceCode ?? (invDtl?.OrderNo != null ? $"INV-{invDtl.InvoiceId:D6}" : "0001245"));
            var htcInvDate = inv?.InvoiceDate ?? car?.CreatedAt.AddDays(-15);

            // CDOD Delivery Out Date
            var delivDate = deliv?.DeliveredAt ?? (deliv?.ApprovedAt ?? (deliv?.DeliveryExpectedDate ?? car?.CreatedAt.AddDays(-5)));

            // Date Full Status (Duty completion date: 100% payment / guarantee)
            var isDutyCompleted = (car != null && (car.DutyCompletedAmount >= car.PriceActual || car.PaymentDepositAmount > 0)) || deliv != null;
            DateTime? dateFullStatus = isDutyCompleted ? (delivDate ?? DateTime.Today.AddDays(-3)) : null;

            var prDiscountNo = discDtl?.PaymentDiscountNo;

            result.Add(new FullStatusCalculatedRowDto(
                Vin: vin,
                CarId: carId,
                DealerCode: dCode,
                DealerName: dName,
                ModelCode: mCode,
                ModelName: mName,
                SuggestedSupportAmount: suggestedAmount,
                OSODApprovedDate: soDate,
                OSODSOCode: soCode,
                HTCInvoiceNo: htcInvNo,
                HTCInvoiceDate: htcInvDate,
                CDODDeliveryOutDate: delivDate,
                DateFullStatus: dateFullStatus,
                PRDiscountNo: prDiscountNo,
                SPSRCode: policy?.SPSRCode,
                SPNo: policy?.SPNo,
                IsDutyCompleted: isDutyCompleted,
                Remark: isDutyCompleted ? "Đủ điều kiện thanh toán hỗ trợ theo chính sách" : "Chờ hoàn tất nghĩa vụ tài chính xe"
            ));
        }

        return result;
    }

    public async Task<object> CalcDateFullStatusAsync(CalcDateFullStatusRequestDto dto)
    {
        var items = await CalculateFullStatusRowsAsync(dto.Vins, dto.SPSRCode);
        return new
        {
            success = true,
            total = items.Count,
            items
        };
    }

    public async Task<object> CreateSupportRetailAsync(CreateSupportRetailDto dto)
    {
        if (dto.Items == null || dto.Items.Count == 0)
            throw new InvalidOperationException("Danh sách xe đề nghị hỗ trợ không được để trống.");

        var createdList = new List<SupportRetail>();

        foreach (var item in dto.Items)
        {
            if (string.IsNullOrWhiteSpace(item.SPSRCode))
                throw new InvalidOperationException("Mã chính sách (SPSRCode) không được để trống.");
            if (string.IsNullOrWhiteSpace(item.Vin))
                throw new InvalidOperationException("Số khung VIN không được để trống.");

            var cleanVin = item.Vin.Trim().ToUpperInvariant();
            var cleanPolicyCode = item.SPSRCode.Trim().ToUpperInvariant();
            var cleanDealerCode = string.IsNullOrWhiteSpace(item.DealerCode) ? "VS058" : item.DealerCode.Trim().ToUpperInvariant();

            // Find policy
            var policy = await _db.SalesPolicies
                .Include(x => x.Details)
                .FirstOrDefaultAsync(x => x.OrgId == _tenant.OrgId && x.SPSRCode == cleanPolicyCode);

            if (policy is null)
                throw new InvalidOperationException($"Chính sách {cleanPolicyCode} không tồn tại.");

            if (policy.Status != SalesPolicyStatus.Valid)
                throw new InvalidOperationException($"Chính sách {cleanPolicyCode} hiện không ở trạng thái hiệu lực ({policy.Status}).");

            // Check duplicate active support
            var existing = await _db.SupportRetails
                .FirstOrDefaultAsync(x => x.OrgId == _tenant.OrgId && x.SPSRCode == cleanPolicyCode && x.Vin == cleanVin);

            if (existing != null && existing.Status != SupportRetailStatus.Cancelled && existing.Status != SupportRetailStatus.Rejected)
                throw new InvalidOperationException($"Số khung VIN {cleanVin} đã được lập hồ sơ đề nghị hỗ trợ ({existing.SupportCode}) cho chính sách này.");

            // Calculate cross-service details
            var calcList = await CalculateFullStatusRowsAsync(new List<string> { cleanVin }, cleanPolicyCode);
            var calcRow = calcList.FirstOrDefault();

            var supportSeq = await GetNextSupportSeqAsync();
            var supportAmount = item.AmountSupport.HasValue && item.AmountSupport > 0
                ? item.AmountSupport.Value
                : (calcRow?.SuggestedSupportAmount ?? 20_000_000m);

            var support = new SupportRetail
            {
                OrgId = _tenant.OrgId,
                SupportCode = supportSeq,
                SPSRCode = cleanPolicyCode,
                SPNo = policy.SPNo,
                DealerCode = cleanDealerCode,
                DealerName = DealerNames.GetValueOrDefault(cleanDealerCode, calcRow?.DealerName ?? cleanDealerCode),
                Vin = cleanVin,
                CarId = calcRow?.CarId ?? $"CAR-{cleanVin[..Math.Min(8, cleanVin.Length)]}",
                ModelCode = calcRow?.ModelCode ?? "SANTAFE",
                ModelName = calcRow?.ModelName ?? "Hyundai Santa Fe 2.5 HTRAC",
                AmountSupport = supportAmount,
                AmountHTCAppr = 0, // NPP sẽ duyệt sau
                DateSupport = item.DateSupport ?? new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1),
                HTCDatePayment = item.HTCDatePayment,
                Status = SupportRetailStatus.Pending,
                Remark = item.Remark?.Trim() ?? "Đề nghị quyết toán hỗ trợ bán lẻ theo chính sách",
                OSODApprovedDate = calcRow?.OSODApprovedDate,
                OSODSOCode = calcRow?.OSODSOCode,
                HTCInvoiceNo = calcRow?.HTCInvoiceNo,
                HTCInvoiceDate = calcRow?.HTCInvoiceDate,
                CDODDeliveryOutDate = calcRow?.CDODDeliveryOutDate,
                DateFullStatus = calcRow?.DateFullStatus,
                PRDiscountNo = calcRow?.PRDiscountNo,
                CreatedAt = DateTime.Now,
                CreatedBy = dto.CreatedBy ?? "DEALER_SALES_STAFF"
            };

            _db.SupportRetails.Add(support);
            createdList.Add(support);
        }

        await _db.SaveChangesAsync();

        return new
        {
            success = true,
            totalCreated = createdList.Count,
            items = createdList.Select(x => new
            {
                x.Id,
                x.SupportCode,
                x.SPSRCode,
                x.SPNo,
                x.DealerCode,
                x.DealerName,
                x.Vin,
                x.CarId,
                x.ModelCode,
                x.ModelName,
                x.AmountSupport,
                Status = x.Status.ToString(),
                x.DateSupport,
                x.CreatedAt
            }).ToList(),
            message = $"Đã lập thành công {createdList.Count} hồ sơ đề nghị quyết toán hỗ trợ bán lẻ."
        };
    }

    public async Task<object?> DetailSupportRetailAsync(string supportCode)
    {
        var item = await _db.SupportRetails
            .FirstOrDefaultAsync(x => x.OrgId == _tenant.OrgId && x.SupportCode == supportCode.Trim());

        if (item is null) return null;

        return new
        {
            item.Id,
            item.SupportCode,
            item.SPSRCode,
            item.SPNo,
            item.DealerCode,
            item.DealerName,
            item.Vin,
            item.CarId,
            item.ModelCode,
            item.ModelName,
            item.AmountSupport,
            item.AmountHTCAppr,
            item.DateSupport,
            item.HTCDatePayment,
            Status = item.Status.ToString(),
            item.Remark,
            item.OSODApprovedDate,
            item.OSODSOCode,
            item.HTCInvoiceNo,
            item.HTCInvoiceDate,
            item.CDODDeliveryOutDate,
            item.DateFullStatus,
            item.PRDiscountNo,
            item.ApprovedAt,
            item.ApprovedBy,
            item.RejectedAt,
            item.RejectedBy,
            item.RejectReason,
            item.CancelledAt,
            item.CancelledBy,
            item.CancelReason,
            item.CreatedAt,
            item.CreatedBy
        };
    }

    public async Task<object?> DetailSupportByVinAsync(string spsrCode, string vin)
    {
        var item = await _db.SupportRetails
            .FirstOrDefaultAsync(x => x.OrgId == _tenant.OrgId &&
                                      x.SPSRCode == spsrCode.Trim() &&
                                      x.Vin == vin.Trim().ToUpperInvariant());

        if (item is null) return null;

        return await DetailSupportRetailAsync(item.SupportCode);
    }

    public async Task<object> ListSupportRetailsAsync(
        string? spsrCode = null,
        string? spNo = null,
        string? dealerCode = null,
        string? vin = null,
        DateTime? dateSupport = null,
        DateTime? createdDateFrom = null,
        DateTime? createdDateTo = null,
        SupportRetailStatus? status = null,
        int page = 0,
        int pageSize = 10)
    {
        var q = _db.SupportRetails.Where(x => x.OrgId == _tenant.OrgId);

        if (!string.IsNullOrWhiteSpace(spsrCode))
        {
            var code = spsrCode.Trim();
            q = q.Where(x => EF.Functions.Like(x.SPSRCode, $"%{code}%"));
        }

        if (!string.IsNullOrWhiteSpace(spNo))
        {
            var no = spNo.Trim();
            q = q.Where(x => EF.Functions.Like(x.SPNo, $"%{no}%"));
        }

        if (!string.IsNullOrWhiteSpace(dealerCode))
        {
            var dCode = dealerCode.Trim().ToUpperInvariant();
            q = q.Where(x => x.DealerCode.ToUpper() == dCode);
        }

        if (!string.IsNullOrWhiteSpace(vin))
        {
            var v = vin.Trim().ToUpperInvariant();
            q = q.Where(x => EF.Functions.Like(x.Vin, $"%{v}%"));
        }

        if (dateSupport.HasValue)
        {
            var startOfMonth = new DateTime(dateSupport.Value.Year, dateSupport.Value.Month, 1);
            var endOfMonth = startOfMonth.AddMonths(1).AddTicks(-1);
            q = q.Where(x => x.DateSupport >= startOfMonth && x.DateSupport <= endOfMonth);
        }

        if (createdDateFrom.HasValue)
            q = q.Where(x => x.CreatedAt >= createdDateFrom.Value.Date);

        if (createdDateTo.HasValue)
            q = q.Where(x => x.CreatedAt <= createdDateTo.Value.Date.AddDays(1).AddTicks(-1));

        if (status.HasValue)
            q = q.Where(x => x.Status == status.Value);

        var total = await q.CountAsync();
        var items = await q.OrderByDescending(x => x.CreatedAt)
            .Skip(page * pageSize)
            .Take(pageSize)
            .Select(x => new
            {
                x.Id,
                x.SupportCode,
                x.SPSRCode,
                x.SPNo,
                x.DealerCode,
                x.DealerName,
                x.Vin,
                x.CarId,
                x.ModelCode,
                x.ModelName,
                x.AmountSupport,
                x.AmountHTCAppr,
                x.DateSupport,
                x.HTCDatePayment,
                Status = x.Status.ToString(),
                x.Remark,
                x.OSODApprovedDate,
                x.OSODSOCode,
                x.HTCInvoiceNo,
                x.HTCInvoiceDate,
                x.CDODDeliveryOutDate,
                x.DateFullStatus,
                x.PRDiscountNo,
                x.ApprovedAt,
                x.ApprovedBy,
                x.CreatedAt,
                x.CreatedBy
            })
            .ToListAsync();

        return new
        {
            total,
            page,
            pageSize,
            items
        };
    }

    public async Task<object?> ApproveSupportRetailAsync(string supportCode, ApproveSupportRetailDto? dto = null)
    {
        var item = await _db.SupportRetails
            .FirstOrDefaultAsync(x => x.OrgId == _tenant.OrgId && x.SupportCode == supportCode.Trim());

        if (item is null) return null;

        if (item.Status != SupportRetailStatus.Pending)
            throw new InvalidOperationException($"Chỉ có thể duyệt hồ sơ hỗ trợ ở trạng thái Chờ duyệt (Pending). Trạng thái hiện tại: {item.Status}");

        item.AmountHTCAppr = dto?.AmountHTCAppr.HasValue == true && dto.AmountHTCAppr.Value > 0
            ? dto.AmountHTCAppr.Value
            : item.AmountSupport;

        item.Status = SupportRetailStatus.Approved;
        item.ApprovedAt = DateTime.Now;
        item.ApprovedBy = dto?.ApprovedBy ?? "HTC_SALES_MANAGER";
        if (!string.IsNullOrWhiteSpace(dto?.Remark))
            item.Remark = (item.Remark != null ? item.Remark + "; " : "") + dto.Remark.Trim();

        await _db.SaveChangesAsync();

        return new
        {
            success = true,
            supportCode = item.SupportCode,
            status = item.Status.ToString(),
            amountHTCAppr = item.AmountHTCAppr,
            approvedAt = item.ApprovedAt,
            approvedBy = item.ApprovedBy,
            message = $"Đã phê duyệt hồ sơ hỗ trợ bán lẻ {item.SupportCode} với số tiền {item.AmountHTCAppr:N0} VNĐ."
        };
    }

    public async Task<object> BatchApproveSupportRetailAsync(BatchApproveSupportRetailDto dto)
    {
        if (dto.SupportCodes == null || dto.SupportCodes.Count == 0)
            throw new InvalidOperationException("Danh sách mã hồ sơ cần duyệt không được rỗng.");

        var cleanCodes = dto.SupportCodes.Select(c => c.Trim()).ToList();
        var items = await _db.SupportRetails
            .Where(x => x.OrgId == _tenant.OrgId && cleanCodes.Contains(x.SupportCode))
            .ToListAsync();

        int approvedCount = 0;
        decimal totalApprovedAmount = 0;

        foreach (var item in items)
        {
            if (item.Status == SupportRetailStatus.Pending)
            {
                item.AmountHTCAppr = item.AmountSupport;
                item.Status = SupportRetailStatus.Approved;
                item.ApprovedAt = DateTime.Now;
                item.ApprovedBy = dto.ApprovedBy ?? "HTC_SALES_MANAGER";
                approvedCount++;
                totalApprovedAmount += item.AmountHTCAppr;
            }
        }

        await _db.SaveChangesAsync();

        return new
        {
            success = true,
            requested = cleanCodes.Count,
            approvedCount,
            totalApprovedAmount,
            message = $"Đã phê duyệt hàng loạt {approvedCount}/{cleanCodes.Count} hồ sơ hỗ trợ bán lẻ."
        };
    }

    public async Task<object?> PaySupportRetailAsync(string supportCode, PaySupportRetailDto? dto = null)
    {
        var item = await _db.SupportRetails
            .FirstOrDefaultAsync(x => x.OrgId == _tenant.OrgId && x.SupportCode == supportCode.Trim());

        if (item is null) return null;

        if (item.Status != SupportRetailStatus.Approved)
            throw new InvalidOperationException($"Chỉ có thể giải ngân thanh toán cho hồ sơ đã được duyệt (Approved). Trạng thái hiện tại: {item.Status}");

        item.Status = SupportRetailStatus.Paid;
        item.HTCDatePayment = dto?.HTCDatePayment ?? DateTime.Today;
        if (!string.IsNullOrWhiteSpace(dto?.Remark))
            item.Remark = (item.Remark != null ? item.Remark + "; " : "") + dto.Remark.Trim();

        await _db.SaveChangesAsync();

        return new
        {
            success = true,
            supportCode = item.SupportCode,
            status = item.Status.ToString(),
            htcDatePayment = item.HTCDatePayment,
            amountPaid = item.AmountHTCAppr,
            message = $"Đã ghi nhận thanh toán chi trả hỗ trợ {item.SupportCode} ngày {item.HTCDatePayment:dd/MM/yyyy}."
        };
    }

    public async Task<object?> RejectSupportRetailAsync(string supportCode, RejectSupportRetailDto dto)
    {
        var item = await _db.SupportRetails
            .FirstOrDefaultAsync(x => x.OrgId == _tenant.OrgId && x.SupportCode == supportCode.Trim());

        if (item is null) return null;

        if (item.Status != SupportRetailStatus.Pending)
            throw new InvalidOperationException($"Chỉ có thể từ chối hồ sơ đang chờ duyệt (Pending). Trạng thái hiện tại: {item.Status}");

        if (string.IsNullOrWhiteSpace(dto.Reason))
            throw new InvalidOperationException("Lý do từ chối hỗ trợ là bắt buộc.");

        item.Status = SupportRetailStatus.Rejected;
        item.RejectReason = dto.Reason.Trim();
        item.RejectedBy = dto.RejectedBy ?? "HTC_SALES_MANAGER";
        item.RejectedAt = DateTime.Now;

        await _db.SaveChangesAsync();

        return new
        {
            success = true,
            supportCode = item.SupportCode,
            status = item.Status.ToString(),
            rejectReason = item.RejectReason,
            message = $"Đã từ chối hỗ trợ hồ sơ {item.SupportCode}."
        };
    }

    public async Task<object?> CancelSupportRetailAsync(string supportCode, CancelSupportRetailDto dto)
    {
        var item = await _db.SupportRetails
            .FirstOrDefaultAsync(x => x.OrgId == _tenant.OrgId && x.SupportCode == supportCode.Trim());

        if (item is null) return null;

        if (item.Status == SupportRetailStatus.Paid)
            throw new InvalidOperationException("Không thể hủy hồ sơ đã được giải ngân thanh toán (Paid).");

        if (string.IsNullOrWhiteSpace(dto.Reason))
            throw new InvalidOperationException("Lý do hủy hồ sơ đề nghị là bắt buộc.");

        item.Status = SupportRetailStatus.Cancelled;
        item.CancelReason = dto.Reason.Trim();
        item.CancelledBy = dto.CancelledBy ?? "DEALER_MANAGER";
        item.CancelledAt = DateTime.Now;

        await _db.SaveChangesAsync();

        return new
        {
            success = true,
            supportCode = item.SupportCode,
            status = item.Status.ToString(),
            cancelReason = item.CancelReason,
            message = $"Đã hủy hồ sơ đề nghị hỗ trợ {item.SupportCode}."
        };
    }

    public async Task<bool> DeleteSupportRetailDraftAsync(string supportCode)
    {
        var item = await _db.SupportRetails
            .FirstOrDefaultAsync(x => x.OrgId == _tenant.OrgId && x.SupportCode == supportCode.Trim());

        if (item is null) return false;

        if (item.Status != SupportRetailStatus.Pending && item.Status != SupportRetailStatus.Rejected)
            throw new InvalidOperationException($"Chỉ được xóa hồ sơ ở trạng thái Chờ duyệt (Pending) hoặc Đã từ chối (Rejected). Trạng thái hiện tại: {item.Status}");

        _db.SupportRetails.Remove(item);
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<object> StatsAsync(string? dealerCode = null)
    {
        var policiesQuery = _db.SalesPolicies.Where(x => x.OrgId == _tenant.OrgId);
        var supportsQuery = _db.SupportRetails.Where(x => x.OrgId == _tenant.OrgId);

        if (!string.IsNullOrWhiteSpace(dealerCode))
        {
            var dCode = dealerCode.Trim().ToUpperInvariant();
            supportsQuery = supportsQuery.Where(x => x.DealerCode.ToUpper() == dCode);
            policiesQuery = policiesQuery.Where(x => x.Details.Any(d => d.DealerCode.ToUpper() == dCode || d.DealerCode == "ALL"));
        }

        var totalPolicies = await policiesQuery.CountAsync();
        var validPolicies = await policiesQuery.CountAsync(x => x.Status == SalesPolicyStatus.Valid);

        var supports = await supportsQuery.ToListAsync();

        var pendingCount = supports.Count(x => x.Status == SupportRetailStatus.Pending);
        var approvedCount = supports.Count(x => x.Status == SupportRetailStatus.Approved);
        var paidCount = supports.Count(x => x.Status == SupportRetailStatus.Paid);
        var rejectedCount = supports.Count(x => x.Status == SupportRetailStatus.Rejected);
        var cancelledCount = supports.Count(x => x.Status == SupportRetailStatus.Cancelled);

        var totalSupportAmount = supports.Sum(x => x.AmountSupport);
        var totalApprovedAmount = supports.Where(x => x.Status == SupportRetailStatus.Approved || x.Status == SupportRetailStatus.Paid).Sum(x => x.AmountHTCAppr);
        var totalPaidAmount = supports.Where(x => x.Status == SupportRetailStatus.Paid).Sum(x => x.AmountHTCAppr);

        var byPolicy = supports.GroupBy(x => x.SPSRCode)
            .Select(g => new
            {
                spsrCode = g.Key,
                spNo = g.First().SPNo,
                totalCars = g.Count(),
                totalAmount = g.Sum(x => x.AmountSupport),
                paidAmount = g.Where(x => x.Status == SupportRetailStatus.Paid).Sum(x => x.AmountHTCAppr)
            })
            .ToList();

        var byDealer = supports.GroupBy(x => x.DealerCode)
            .Select(g => new
            {
                dealerCode = g.Key,
                dealerName = DealerNames.GetValueOrDefault(g.Key, g.First().DealerName ?? g.Key),
                totalCars = g.Count(),
                totalAmount = g.Sum(x => x.AmountSupport),
                paidAmount = g.Where(x => x.Status == SupportRetailStatus.Paid).Sum(x => x.AmountHTCAppr)
            })
            .ToList();

        return new
        {
            totalPolicies,
            validPolicies,
            totalSupports = supports.Count,
            statusCounts = new
            {
                pending = pendingCount,
                approved = approvedCount,
                paid = paidCount,
                rejected = rejectedCount,
                cancelled = cancelledCount
            },
            financials = new
            {
                totalSupportAmount,
                totalApprovedAmount,
                totalPaidAmount
            },
            byPolicy,
            byDealer
        };
    }
}
