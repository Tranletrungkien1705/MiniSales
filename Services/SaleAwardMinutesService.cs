using Microsoft.EntityFrameworkCore;
using MiniSales.Data;
using MiniSales.Models;

namespace MiniSales.Services;

public record SaleAwardItemInputDto(
    string Vin,
    string DealerCode,
    string? DealerName = null,
    string DocumentNo = "",
    decimal AmountAward = 0,
    string? ModelCode = null,
    string? ModelName = null,
    string? SpecCode = null,
    string? SpecDescription = null,
    string? ColorCode = null,
    string? ColorName = null,
    int? VinYear = null,
    DateTime? DeliveryDate = null,
    string? DealNo = null,
    string? CarId = null,
    string? Remark = null
);

public record CreateMultiSaleAwardMinutesDto(
    List<SaleAwardItemInputDto> Items,
    string? Remark = null,
    string? CreatedBy = null
);

public record UpdateSaleAwardMinutesDto(
    List<SaleAwardItemInputDto>? Items = null,
    string? Remark = null,
    string? UpdatedBy = null
);

public record ApproveAwardMinutesDto(
    string? ApprovedBy = null,
    string? Remark = null
);

public record FinishAwardMinutesDto(
    string FileName,
    string? SignedBy = null,
    string? Remark = null
);

public record RejectAwardMinutesDto(
    string Reason,
    string? RejectedBy = null
);

public record CancelAwardMinutesDto(
    string Reason,
    string? CancelledBy = null
);

public record CheckVinForAwardDto(
    string Vin,
    string? DocumentNo = null,
    decimal? AmountAward = null,
    string? Remark = null
);

public class EligibleCarItemDto
{
    public string Vin { get; set; } = "";
    public string? CarId { get; set; }
    public string DealerCode { get; set; } = "";
    public string DealerName { get; set; } = "";
    public string? DealNo { get; set; }
    public DateTime? DeliveryDate { get; set; }
    public string? ModelCode { get; set; }
    public string? ModelName { get; set; }
    public string? SpecCode { get; set; }
    public string? SpecDescription { get; set; }
    public string? ColorCode { get; set; }
    public string? ColorName { get; set; }
    public int? VinYear { get; set; }
    public string? DocumentNo { get; set; }
    public decimal? AmountAward { get; set; }
    public string? Remark { get; set; }
    public bool IsEligible { get; set; }
    public string? IneligibleReason { get; set; }
}

public interface ISaleAwardMinutesService
{
    Task<object> ListAsync(
        string? status = null,
        string? dealerCode = null,
        string? documentNo = null,
        string? vin = null,
        DateTime? createDateFrom = null,
        DateTime? createDateTo = null,
        int page = 0,
        int pageSize = 10
    );

    Task<object?> DetailAsync(string saleAwardMinutesNo);
    Task<object> GetSeqAsync();
    Task<object> GetEligibleCarsAsync(List<CheckVinForAwardDto>? checkVins = null, string? dealerCode = null);
    Task<object> CreateMultiAsync(CreateMultiSaleAwardMinutesDto dto);
    Task<object?> UpdateAsync(string saleAwardMinutesNo, UpdateSaleAwardMinutesDto dto);
    Task<object?> Approve1Async(string saleAwardMinutesNo, ApproveAwardMinutesDto? dto = null);
    Task<object?> Approve2Async(string saleAwardMinutesNo, ApproveAwardMinutesDto? dto = null);
    Task<object?> FinishAsync(string saleAwardMinutesNo, FinishAwardMinutesDto dto);
    Task<object?> RejectAsync(string saleAwardMinutesNo, RejectAwardMinutesDto dto);
    Task<object?> CancelAsync(string saleAwardMinutesNo, CancelAwardMinutesDto dto);
    Task<bool> DeleteDraftAsync(string saleAwardMinutesNo);
    Task<object> StatsAsync();
}

public class SaleAwardMinutesService : ISaleAwardMinutesService
{
    private readonly AppDbContext _db;
    private readonly ITenantContext _tenant;

    public SaleAwardMinutesService(AppDbContext db, ITenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    public async Task<object> GetSeqAsync()
    {
        var nextNo = await GenerateSequenceNoAsync();
        return new { saleAwardMinutesNo = nextNo };
    }

    private async Task<string> GenerateSequenceNoAsync()
    {
        var prefix = $"{DateTime.Today:yyMM}SAM";
        var last = await _db.SaleAwardMinutes
            .Where(m => m.OrgId == _tenant.OrgId && m.SaleAwardMinutesNo.StartsWith(prefix))
            .OrderByDescending(m => m.SaleAwardMinutesNo)
            .Select(m => m.SaleAwardMinutesNo)
            .FirstOrDefaultAsync();

        int seq = 1;
        if (!string.IsNullOrEmpty(last) && last.Length >= prefix.Length + 5)
        {
            var seqPart = last.Substring(prefix.Length);
            if (int.TryParse(seqPart, out var parsed))
            {
                seq = parsed + 1;
            }
        }

        return $"{prefix}{seq:D5}";
    }

    public async Task<object> ListAsync(
        string? status = null,
        string? dealerCode = null,
        string? documentNo = null,
        string? vin = null,
        DateTime? createDateFrom = null,
        DateTime? createDateTo = null,
        int page = 0,
        int pageSize = 10
    )
    {
        if (pageSize <= 0) pageSize = 10;
        if (pageSize > 100) pageSize = 100;
        if (page < 0) page = 0;

        var q = _db.SaleAwardMinutes
            .Include(m => m.Details)
            .Where(m => m.OrgId == _tenant.OrgId)
            .AsNoTracking();

        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<SaleAwardMinutesStatus>(status, true, out var st))
        {
            q = q.Where(m => m.SaleAwardMinutesStatus == st);
        }

        if (!string.IsNullOrWhiteSpace(dealerCode))
        {
            var dl = dealerCode.Trim().ToUpper();
            q = q.Where(m => m.DealerCode.ToUpper() == dl);
        }

        if (!string.IsNullOrWhiteSpace(documentNo))
        {
            var doc = documentNo.Trim().ToUpper();
            q = q.Where(m => m.Details.Any(d => d.DocumentNo.ToUpper().Contains(doc)));
        }

        if (!string.IsNullOrWhiteSpace(vin))
        {
            var v = vin.Trim().ToUpper();
            q = q.Where(m => m.Details.Any(d => d.Vin.ToUpper().Contains(v)));
        }

        if (createDateFrom.HasValue)
        {
            q = q.Where(m => m.CreateDTime >= createDateFrom.Value);
        }

        if (createDateTo.HasValue)
        {
            var toDate = createDateTo.Value.Date.AddDays(1).AddTicks(-1);
            q = q.Where(m => m.CreateDTime <= toDate);
        }

        var total = await q.CountAsync();
        var items = await q
            .OrderByDescending(m => m.CreateDTime)
            .Skip(page * pageSize)
            .Take(pageSize)
            .Select(m => new
            {
                m.Id,
                m.SaleAwardMinutesNo,
                m.DealerCode,
                m.DealerName,
                Status = m.SaleAwardMinutesStatus.ToString(),
                StatusInt = (int)m.SaleAwardMinutesStatus,
                m.TotalCars,
                m.TotalAwardAmount,
                m.CreateDTime,
                m.CreateBy,
                m.Appr1DTime,
                m.Appr1By,
                m.Appr2DTime,
                m.Appr2By,
                m.FinishDTime,
                m.FinishBy,
                m.FileName,
                m.FilePath,
                m.FileUrl,
                m.RejectReason,
                m.RejectDTime,
                m.RejectBy,
                m.CancelReason,
                m.CancelDTime,
                m.CancelBy,
                m.Remark,
                m.LogLUDateTime,
                m.LogLUBy,
                DetailCount = m.Details.Count,
                Vins = m.Details.Select(d => d.Vin).ToList(),
                Documents = m.Details.Select(d => d.DocumentNo).Distinct().ToList()
            })
            .ToListAsync();

        return new
        {
            total,
            page,
            pageSize,
            totalPages = (int)Math.Ceiling(total / (double)pageSize),
            items
        };
    }

    public async Task<object?> DetailAsync(string saleAwardMinutesNo)
    {
        var cleanNo = saleAwardMinutesNo.Trim().ToUpper();
        var m = await _db.SaleAwardMinutes
            .Include(x => x.Details)
            .Where(x => x.OrgId == _tenant.OrgId && x.SaleAwardMinutesNo.ToUpper() == cleanNo)
            .AsNoTracking()
            .FirstOrDefaultAsync();

        if (m is null) return null;

        return new
        {
            m.Id,
            m.SaleAwardMinutesNo,
            m.DealerCode,
            m.DealerName,
            Status = m.SaleAwardMinutesStatus.ToString(),
            StatusInt = (int)m.SaleAwardMinutesStatus,
            m.TotalCars,
            m.TotalAwardAmount,
            m.CreateDTime,
            m.CreateBy,
            m.Appr1DTime,
            m.Appr1By,
            m.Appr2DTime,
            m.Appr2By,
            m.FinishDTime,
            m.FinishBy,
            m.FileName,
            m.FilePath,
            m.FileUrl,
            m.RejectReason,
            m.RejectDTime,
            m.RejectBy,
            m.CancelReason,
            m.CancelDTime,
            m.CancelBy,
            m.Remark,
            m.LogLUDateTime,
            m.LogLUBy,
            Details = m.Details.Select(d => new
            {
                d.Id,
                d.SaleAwardMinutesNo,
                d.Vin,
                d.CarId,
                d.DealerCode,
                d.DealerName,
                d.DocumentNo,
                d.AmountAward,
                d.ModelCode,
                d.ModelName,
                d.SpecCode,
                d.SpecDescription,
                d.ColorCode,
                d.ColorName,
                d.VinYear,
                d.DeliveryDate,
                d.DealNo,
                d.Remark,
                d.LogLUDateTime,
                d.LogLUBy
            }).ToList()
        };
    }

    public async Task<object> GetEligibleCarsAsync(List<CheckVinForAwardDto>? checkVins = null, string? dealerCode = null)
    {
        // 1. Get all active VINs already in active SaleAwardMinutes (status not Canceled and not Rejected)
        var activeAwardedVins = await _db.SaleAwardMinutesDetails
            .Where(d => _db.SaleAwardMinutes.Any(m =>
                m.OrgId == _tenant.OrgId &&
                m.SaleAwardMinutesNo == d.SaleAwardMinutesNo &&
                m.SaleAwardMinutesStatus != SaleAwardMinutesStatus.Canceled &&
                m.SaleAwardMinutesStatus != SaleAwardMinutesStatus.Rejected))
            .Select(d => d.Vin.ToUpper())
            .Distinct()
            .ToListAsync();

        var activeAwardedSet = new HashSet<string>(activeAwardedVins, StringComparer.OrdinalIgnoreCase);

        // 2. Fetch delivered/retail cars from Deals / Orders / VinInventory / CarRecords
        // RetailDeals: exclude sales types D3 (test car), F7 (lateral transfer), F6 (handicap org)
        var deals = await _db.RetailDeals
            .Include(d => d.Details)
            .Where(d => d.OrgId == _tenant.OrgId &&
                        d.Status != RetailDealStatus.Cancelled &&
                        d.SalesType != "D3" && d.SalesType != "F7" && d.SalesType != "F6")
            .AsNoTracking()
            .ToListAsync();

        var eligibleCarsList = new List<EligibleCarItemDto>();

        foreach (var deal in deals)
        {
            if (!string.IsNullOrWhiteSpace(dealerCode) &&
                !deal.DealerCode.Equals(dealerCode.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            foreach (var dtl in deal.Details)
            {
                if (string.IsNullOrWhiteSpace(dtl.Vin)) continue;
                var vinUpper = dtl.Vin.Trim().ToUpper();

                // Check if already claimed
                bool isClaimed = activeAwardedSet.Contains(vinUpper);

                eligibleCarsList.Add(new EligibleCarItemDto
                {
                    Vin = vinUpper,
                    CarId = dtl.CarId,
                    DealerCode = deal.DealerCode,
                    DealerName = deal.DealerName,
                    DealNo = deal.DealNo,
                    DeliveryDate = dtl.DeliveryDate ?? deal.DealDate,
                    ModelCode = dtl.Model,
                    ModelName = dtl.Model,
                    SpecCode = dtl.SpecCode,
                    SpecDescription = dtl.SpecCode != null ? $"Bản {dtl.SpecCode}" : null,
                    ColorCode = dtl.ColorCode,
                    ColorName = dtl.ColorCode,
                    VinYear = DateTime.Today.Year,
                    IsEligible = !isClaimed,
                    IneligibleReason = isClaimed ? "VIN đã nằm trong biên bản đối soát thưởng đang hoạt động" : null
                });
            }
        }

        // Also cross check with CarRecords if available and not yet in list
        var knownVins = new HashSet<string>(eligibleCarsList.Select(x => x.Vin), StringComparer.OrdinalIgnoreCase);

        var carRecords = await _db.Cars
            .Where(c => c.OrgId == _tenant.OrgId && c.FlagActive == "1" && !string.IsNullOrEmpty(c.Vin))
            .AsNoTracking()
            .ToListAsync();

        foreach (var car in carRecords)
        {
            if (string.IsNullOrWhiteSpace(car.Vin)) continue;
            var vinUpper = car.Vin.Trim().ToUpper();
            if (knownVins.Contains(vinUpper)) continue;

            if (!string.IsNullOrWhiteSpace(dealerCode) &&
                !car.DealerCode.Equals(dealerCode.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            bool isClaimed = activeAwardedSet.Contains(vinUpper);

            eligibleCarsList.Add(new EligibleCarItemDto
            {
                Vin = vinUpper,
                CarId = car.CarId,
                DealerCode = car.DealerCode,
                DealerName = car.DealerCode == "VS058" ? "Hyundai Bình Dương" :
                             car.DealerCode == "VN065" ? "Hyundai Đông Đô" :
                             car.DealerCode == "VN012" ? "Hyundai Hà Đông" : $"Hyundai Đại lý {car.DealerCode}",
                DealNo = null,
                DeliveryDate = DateTime.Today.AddDays(-15),
                ModelCode = car.ModelCode,
                ModelName = car.ModelName,
                SpecCode = car.SpecCode,
                SpecDescription = car.SpecDescription,
                ColorCode = car.ColorCode,
                ColorName = car.ColorName,
                VinYear = DateTime.Today.Year,
                IsEligible = !isClaimed,
                IneligibleReason = isClaimed ? "VIN đã nằm trong biên bản đối soát thưởng đang hoạt động" : null
            });
            knownVins.Add(vinUpper);
        }

        // If specific checkVins were passed in
        if (checkVins != null && checkVins.Count > 0)
        {
            var verifiedList = new List<EligibleCarItemDto>();
            foreach (var chk in checkVins)
            {
                if (string.IsNullOrWhiteSpace(chk.Vin)) continue;
                var vUpper = chk.Vin.Trim().ToUpper();

                var found = eligibleCarsList.FirstOrDefault(x => string.Equals(x.Vin, vUpper, StringComparison.OrdinalIgnoreCase));
                bool isClaimed = activeAwardedSet.Contains(vUpper);

                if (found != null)
                {
                    verifiedList.Add(new EligibleCarItemDto
                    {
                        Vin = vUpper,
                        CarId = found.CarId,
                        DealerCode = found.DealerCode,
                        DealerName = found.DealerName,
                        DealNo = found.DealNo,
                        DeliveryDate = found.DeliveryDate,
                        ModelCode = found.ModelCode,
                        ModelName = found.ModelName,
                        SpecCode = found.SpecCode,
                        SpecDescription = found.SpecDescription,
                        ColorCode = found.ColorCode,
                        ColorName = found.ColorName,
                        VinYear = found.VinYear,
                        DocumentNo = chk.DocumentNo ?? "",
                        AmountAward = chk.AmountAward ?? 0,
                        Remark = chk.Remark,
                        IsEligible = !isClaimed,
                        IneligibleReason = isClaimed ? "VIN đã nằm trong biên bản đối soát thưởng đang hoạt động" : null
                    });
                }
                else
                {
                    verifiedList.Add(new EligibleCarItemDto
                    {
                        Vin = vUpper,
                        CarId = null,
                        DealerCode = dealerCode ?? "VS058",
                        DealerName = dealerCode == "VS058" ? "Hyundai Bình Dương" : "Đại lý Hyundai",
                        DealNo = null,
                        DeliveryDate = null,
                        ModelCode = "HYUNDAI",
                        ModelName = "Hyundai Car",
                        SpecCode = null,
                        SpecDescription = null,
                        ColorCode = null,
                        ColorName = null,
                        VinYear = DateTime.Today.Year,
                        DocumentNo = chk.DocumentNo ?? "",
                        AmountAward = chk.AmountAward ?? 0,
                        Remark = chk.Remark,
                        IsEligible = !isClaimed,
                        IneligibleReason = isClaimed ? "VIN đã nằm trong biên bản đối soát thưởng đang hoạt động" : "Không tìm thấy dữ liệu giao dịch bán lẻ hợp lệ cho số khung VIN này"
                    });
                }
            }

            return new
            {
                totalChecked = verifiedList.Count,
                eligibleCount = verifiedList.Count(x => x.IsEligible),
                ineligibleCount = verifiedList.Count(x => !x.IsEligible),
                results = verifiedList
            };
        }

        return new
        {
            total = eligibleCarsList.Count,
            eligibleCount = eligibleCarsList.Count(x => x.IsEligible),
            ineligibleCount = eligibleCarsList.Count(x => !x.IsEligible),
            items = eligibleCarsList
        };
    }

    public async Task<object> CreateMultiAsync(CreateMultiSaleAwardMinutesDto dto)
    {
        if (dto.Items == null || dto.Items.Count == 0)
        {
            throw new InvalidOperationException("Danh sách chi tiết xe (Items) không được rỗng.");
        }

        // Validate each item
        var seenVins = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var item in dto.Items)
        {
            if (string.IsNullOrWhiteSpace(item.Vin))
                throw new InvalidOperationException("Số khung xe (VIN) không được để trống.");

            var vinUpper = item.Vin.Trim().ToUpper();
            if (vinUpper.Length != 17)
                throw new InvalidOperationException($"Số khung VIN '{item.Vin}' không đúng định dạng chuẩn 17 ký tự.");

            if (seenVins.Contains(vinUpper))
                throw new InvalidOperationException($"Số khung VIN '{item.Vin}' bị trùng lặp trong cùng yêu cầu tạo biên bản.");
            seenVins.Add(vinUpper);

            if (string.IsNullOrWhiteSpace(item.DealerCode))
                throw new InvalidOperationException($"Mã đại lý cho xe '{item.Vin}' không được để trống.");

            if (string.IsNullOrWhiteSpace(item.DocumentNo))
                throw new InvalidOperationException($"Số công văn thưởng (DocumentNo) cho xe '{item.Vin}' không được để trống.");

            if (item.AmountAward <= 0)
                throw new InvalidOperationException($"Số tiền thưởng (AmountAward) cho xe '{item.Vin}' phải lớn hơn 0.");
        }

        // Check against active SaleAwardMinutes in DB
        var activeAwardedVins = await _db.SaleAwardMinutesDetails
            .Where(d => _db.SaleAwardMinutes.Any(m =>
                m.OrgId == _tenant.OrgId &&
                m.SaleAwardMinutesNo == d.SaleAwardMinutesNo &&
                m.SaleAwardMinutesStatus != SaleAwardMinutesStatus.Canceled &&
                m.SaleAwardMinutesStatus != SaleAwardMinutesStatus.Rejected))
            .Select(d => new { d.Vin, d.SaleAwardMinutesNo })
            .ToListAsync();

        var activeAwardedDict = activeAwardedVins
            .GroupBy(x => x.Vin.ToUpper())
            .ToDictionary(g => g.Key, g => g.First().SaleAwardMinutesNo, StringComparer.OrdinalIgnoreCase);

        foreach (var item in dto.Items)
        {
            var vUpper = item.Vin.Trim().ToUpper();
            if (activeAwardedDict.TryGetValue(vUpper, out var existingNo))
            {
                throw new InvalidOperationException($"Số khung VIN '{item.Vin}' đã tồn tại trong biên bản đối soát thưởng '{existingNo}' đang hoạt động.");
            }
        }

        // Group by DealerCode
        var groupedByDealer = dto.Items.GroupBy(x => x.DealerCode.Trim().ToUpper()).ToList();
        var createdMinutesList = new List<SaleAwardMinutes>();

        // Pre-fetch next sequence
        var prefix = $"{DateTime.Today:yyMM}SAM";
        var last = await _db.SaleAwardMinutes
            .Where(m => m.OrgId == _tenant.OrgId && m.SaleAwardMinutesNo.StartsWith(prefix))
            .OrderByDescending(m => m.SaleAwardMinutesNo)
            .Select(m => m.SaleAwardMinutesNo)
            .FirstOrDefaultAsync();

        int currentSeq = 1;
        if (!string.IsNullOrEmpty(last) && last.Length >= prefix.Length + 5)
        {
            var seqPart = last.Substring(prefix.Length);
            if (int.TryParse(seqPart, out var parsed))
            {
                currentSeq = parsed + 1;
            }
        }

        var now = DateTime.Now;

        foreach (var grp in groupedByDealer)
        {
            var dealerCode = grp.Key;
            var dealerName = grp.First().DealerName;
            if (string.IsNullOrWhiteSpace(dealerName))
            {
                dealerName = dealerCode switch
                {
                    "VS058" => "Hyundai Bình Dương",
                    "VN065" => "Hyundai Đông Đô",
                    "VN012" => "Hyundai Hà Đông",
                    _ => $"Hyundai Đại lý {dealerCode}"
                };
            }

            var minutesNo = $"{prefix}{currentSeq:D5}";
            currentSeq++;

            var minutes = new SaleAwardMinutes
            {
                OrgId = _tenant.OrgId,
                SaleAwardMinutesNo = minutesNo,
                DealerCode = dealerCode,
                DealerName = dealerName,
                SaleAwardMinutesStatus = SaleAwardMinutesStatus.Pending,
                TotalCars = grp.Count(),
                TotalAwardAmount = grp.Sum(x => x.AmountAward),
                Remark = dto.Remark,
                CreateDTime = now,
                CreateBy = dto.CreatedBy ?? "HQ_OPERATOR",
                LogLUDateTime = now,
                LogLUBy = dto.CreatedBy ?? "HQ_OPERATOR"
            };

            foreach (var item in grp)
            {
                var dtl = new SaleAwardMinutesDetail
                {
                    SaleAwardMinutesNo = minutesNo,
                    Vin = item.Vin.Trim().ToUpper(),
                    CarId = item.CarId ?? $"CAR-{item.Vin.Trim().ToUpper().Substring(Math.Max(0, item.Vin.Trim().Length - 8))}",
                    DealerCode = dealerCode,
                    DealerName = dealerName,
                    DocumentNo = item.DocumentNo.Trim(),
                    AmountAward = item.AmountAward,
                    ModelCode = item.ModelCode ?? "HYUNDAI",
                    ModelName = item.ModelName ?? item.ModelCode ?? "Hyundai Vehicle",
                    SpecCode = item.SpecCode,
                    SpecDescription = item.SpecDescription,
                    ColorCode = item.ColorCode,
                    ColorName = item.ColorName,
                    VinYear = item.VinYear ?? now.Year,
                    DeliveryDate = item.DeliveryDate ?? now.AddDays(-10),
                    DealNo = item.DealNo,
                    Remark = item.Remark,
                    LogLUDateTime = now,
                    LogLUBy = dto.CreatedBy ?? "HQ_OPERATOR"
                };
                minutes.Details.Add(dtl);
            }

            createdMinutesList.Add(minutes);
            _db.SaleAwardMinutes.Add(minutes);
        }

        await _db.SaveChangesAsync();

        return new
        {
            success = true,
            totalCreated = createdMinutesList.Count,
            minutes = createdMinutesList.Select(m => new
            {
                m.Id,
                m.SaleAwardMinutesNo,
                m.DealerCode,
                m.DealerName,
                Status = m.SaleAwardMinutesStatus.ToString(),
                m.TotalCars,
                m.TotalAwardAmount,
                m.CreateDTime,
                m.CreateBy
            }).ToList()
        };
    }

    public async Task<object?> UpdateAsync(string saleAwardMinutesNo, UpdateSaleAwardMinutesDto dto)
    {
        var cleanNo = saleAwardMinutesNo.Trim().ToUpper();
        var m = await _db.SaleAwardMinutes
            .Include(x => x.Details)
            .FirstOrDefaultAsync(x => x.OrgId == _tenant.OrgId && x.SaleAwardMinutesNo.ToUpper() == cleanNo);

        if (m is null) return null;

        if (m.SaleAwardMinutesStatus != SaleAwardMinutesStatus.Pending)
        {
            throw new InvalidOperationException($"Chỉ được phép cập nhật biên bản khi đang ở trạng thái 'Pending'. Hiện tại là: {m.SaleAwardMinutesStatus}");
        }

        var now = DateTime.Now;

        if (dto.Remark != null)
        {
            m.Remark = dto.Remark;
        }

        if (dto.Items != null && dto.Items.Count > 0)
        {
            // Validate items
            var seenVins = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var item in dto.Items)
            {
                if (string.IsNullOrWhiteSpace(item.Vin))
                    throw new InvalidOperationException("Số khung xe (VIN) không được để trống.");

                var vinUpper = item.Vin.Trim().ToUpper();
                if (vinUpper.Length != 17)
                    throw new InvalidOperationException($"Số khung VIN '{item.Vin}' không đúng định dạng chuẩn 17 ký tự.");

                if (seenVins.Contains(vinUpper))
                    throw new InvalidOperationException($"Số khung VIN '{item.Vin}' bị trùng lặp trong cùng biên bản.");
                seenVins.Add(vinUpper);

                if (string.IsNullOrWhiteSpace(item.DocumentNo))
                    throw new InvalidOperationException($"Số công văn thưởng (DocumentNo) cho xe '{item.Vin}' không được để trống.");

                if (item.AmountAward <= 0)
                    throw new InvalidOperationException($"Số tiền thưởng (AmountAward) cho xe '{item.Vin}' phải lớn hơn 0.");
            }

            // Check against active SaleAwardMinutes other than this one
            var otherActiveAwardedVins = await _db.SaleAwardMinutesDetails
                .Where(d => d.SaleAwardMinutesNo != m.SaleAwardMinutesNo &&
                            _db.SaleAwardMinutes.Any(x =>
                                x.OrgId == _tenant.OrgId &&
                                x.SaleAwardMinutesNo == d.SaleAwardMinutesNo &&
                                x.SaleAwardMinutesStatus != SaleAwardMinutesStatus.Canceled &&
                                x.SaleAwardMinutesStatus != SaleAwardMinutesStatus.Rejected))
                .Select(d => new { d.Vin, d.SaleAwardMinutesNo })
                .ToListAsync();

            var otherActiveDict = otherActiveAwardedVins
                .GroupBy(x => x.Vin.ToUpper())
                .ToDictionary(g => g.Key, g => g.First().SaleAwardMinutesNo, StringComparer.OrdinalIgnoreCase);

            foreach (var item in dto.Items)
            {
                var vUpper = item.Vin.Trim().ToUpper();
                if (otherActiveDict.TryGetValue(vUpper, out var existingNo))
                {
                    throw new InvalidOperationException($"Số khung VIN '{item.Vin}' đã tồn tại trong biên bản đối soát thưởng '{existingNo}' khác.");
                }
            }

            // Remove existing details and replace
            _db.SaleAwardMinutesDetails.RemoveRange(m.Details);
            m.Details.Clear();

            foreach (var item in dto.Items)
            {
                var dtl = new SaleAwardMinutesDetail
                {
                    SaleAwardMinutesId = m.Id,
                    SaleAwardMinutesNo = m.SaleAwardMinutesNo,
                    Vin = item.Vin.Trim().ToUpper(),
                    CarId = item.CarId ?? $"CAR-{item.Vin.Trim().ToUpper().Substring(Math.Max(0, item.Vin.Trim().Length - 8))}",
                    DealerCode = m.DealerCode,
                    DealerName = m.DealerName,
                    DocumentNo = item.DocumentNo.Trim(),
                    AmountAward = item.AmountAward,
                    ModelCode = item.ModelCode ?? "HYUNDAI",
                    ModelName = item.ModelName ?? item.ModelCode ?? "Hyundai Vehicle",
                    SpecCode = item.SpecCode,
                    SpecDescription = item.SpecDescription,
                    ColorCode = item.ColorCode,
                    ColorName = item.ColorName,
                    VinYear = item.VinYear ?? now.Year,
                    DeliveryDate = item.DeliveryDate ?? now.AddDays(-10),
                    DealNo = item.DealNo,
                    Remark = item.Remark,
                    LogLUDateTime = now,
                    LogLUBy = dto.UpdatedBy ?? "HQ_OPERATOR"
                };
                m.Details.Add(dtl);
            }

            m.TotalCars = m.Details.Count;
            m.TotalAwardAmount = m.Details.Sum(x => x.AmountAward);
        }

        m.LogLUDateTime = now;
        m.LogLUBy = dto.UpdatedBy ?? "HQ_OPERATOR";

        await _db.SaveChangesAsync();
        return await DetailAsync(saleAwardMinutesNo);
    }

    public async Task<object?> Approve1Async(string saleAwardMinutesNo, ApproveAwardMinutesDto? dto = null)
    {
        var cleanNo = saleAwardMinutesNo.Trim().ToUpper();
        var m = await _db.SaleAwardMinutes
            .FirstOrDefaultAsync(x => x.OrgId == _tenant.OrgId && x.SaleAwardMinutesNo.ToUpper() == cleanNo);

        if (m is null) return null;

        if (m.SaleAwardMinutesStatus != SaleAwardMinutesStatus.Pending)
        {
            throw new InvalidOperationException($"Chỉ được phép phê duyệt cấp 1 khi biên bản đang ở trạng thái 'Pending'. Hiện tại là: {m.SaleAwardMinutesStatus}");
        }

        var now = DateTime.Now;
        m.SaleAwardMinutesStatus = SaleAwardMinutesStatus.Approved1;
        m.Appr1DTime = now;
        m.Appr1By = dto?.ApprovedBy ?? "HQ_SALES_MANAGER";
        if (!string.IsNullOrWhiteSpace(dto?.Remark))
        {
            m.Remark = string.IsNullOrWhiteSpace(m.Remark) ? dto.Remark : $"{m.Remark} | Duyệt C1: {dto.Remark}";
        }
        m.LogLUDateTime = now;
        m.LogLUBy = dto?.ApprovedBy ?? "HQ_SALES_MANAGER";

        await _db.SaveChangesAsync();
        return await DetailAsync(saleAwardMinutesNo);
    }

    public async Task<object?> Approve2Async(string saleAwardMinutesNo, ApproveAwardMinutesDto? dto = null)
    {
        var cleanNo = saleAwardMinutesNo.Trim().ToUpper();
        var m = await _db.SaleAwardMinutes
            .FirstOrDefaultAsync(x => x.OrgId == _tenant.OrgId && x.SaleAwardMinutesNo.ToUpper() == cleanNo);

        if (m is null) return null;

        if (m.SaleAwardMinutesStatus != SaleAwardMinutesStatus.Approved1)
        {
            throw new InvalidOperationException($"Chỉ được phép phê duyệt cấp 2 khi biên bản đã được duyệt cấp 1 'Approved1'. Hiện tại là: {m.SaleAwardMinutesStatus}");
        }

        var now = DateTime.Now;
        m.SaleAwardMinutesStatus = SaleAwardMinutesStatus.Approved2;
        m.Appr2DTime = now;
        m.Appr2By = dto?.ApprovedBy ?? "HQ_SALES_DIRECTOR";
        if (!string.IsNullOrWhiteSpace(dto?.Remark))
        {
            m.Remark = string.IsNullOrWhiteSpace(m.Remark) ? dto.Remark : $"{m.Remark} | Duyệt C2: {dto.Remark}";
        }
        m.LogLUDateTime = now;
        m.LogLUBy = dto?.ApprovedBy ?? "HQ_SALES_DIRECTOR";

        await _db.SaveChangesAsync();
        return await DetailAsync(saleAwardMinutesNo);
    }

    public async Task<object?> FinishAsync(string saleAwardMinutesNo, FinishAwardMinutesDto dto)
    {
        var cleanNo = saleAwardMinutesNo.Trim().ToUpper();
        var m = await _db.SaleAwardMinutes
            .FirstOrDefaultAsync(x => x.OrgId == _tenant.OrgId && x.SaleAwardMinutesNo.ToUpper() == cleanNo);

        if (m is null) return null;

        if (m.SaleAwardMinutesStatus != SaleAwardMinutesStatus.Approved2)
        {
            throw new InvalidOperationException($"Chỉ được phép hoàn tất / Đại lý ký khi biên bản đã được NPP duyệt cấp 2 'Approved2'. Hiện tại là: {m.SaleAwardMinutesStatus}");
        }

        if (string.IsNullOrWhiteSpace(dto.FileName))
        {
            throw new InvalidOperationException("Tên file biên bản đã ký (FileName) không được để trống.");
        }

        var ext = Path.GetExtension(dto.FileName).ToUpper();
        if (ext != ".PDF")
        {
            throw new InvalidOperationException($"File đính kèm biên bản ký phải là định dạng PDF (.pdf), file hiện tại: {dto.FileName}");
        }

        var now = DateTime.Now;
        m.SaleAwardMinutesStatus = SaleAwardMinutesStatus.Finished;
        m.FinishDTime = now;
        m.FinishBy = dto.SignedBy ?? "DEALER_DIRECTOR";
        m.FileName = dto.FileName;
        m.FilePath = $"/uploads/sale_awards/{cleanNo}/{dto.FileName}";
        m.FileUrl = $"/api/files/download/{dto.FileName}";

        if (!string.IsNullOrWhiteSpace(dto.Remark))
        {
            m.Remark = string.IsNullOrWhiteSpace(m.Remark) ? dto.Remark : $"{m.Remark} | Ký hoàn tất: {dto.Remark}";
        }

        m.LogLUDateTime = now;
        m.LogLUBy = dto.SignedBy ?? "DEALER_DIRECTOR";

        await _db.SaveChangesAsync();
        return await DetailAsync(saleAwardMinutesNo);
    }

    public async Task<object?> RejectAsync(string saleAwardMinutesNo, RejectAwardMinutesDto dto)
    {
        var cleanNo = saleAwardMinutesNo.Trim().ToUpper();
        var m = await _db.SaleAwardMinutes
            .FirstOrDefaultAsync(x => x.OrgId == _tenant.OrgId && x.SaleAwardMinutesNo.ToUpper() == cleanNo);

        if (m is null) return null;

        if (m.SaleAwardMinutesStatus != SaleAwardMinutesStatus.Pending &&
            m.SaleAwardMinutesStatus != SaleAwardMinutesStatus.Approved1)
        {
            throw new InvalidOperationException($"Chỉ được phép từ chối khi biên bản đang ở trạng thái 'Pending' hoặc 'Approved1'. Hiện tại là: {m.SaleAwardMinutesStatus}");
        }

        if (string.IsNullOrWhiteSpace(dto.Reason))
        {
            throw new InvalidOperationException("Lý do từ chối (Reason) không được để trống.");
        }

        var now = DateTime.Now;
        m.SaleAwardMinutesStatus = SaleAwardMinutesStatus.Rejected;
        m.RejectReason = dto.Reason.Trim();
        m.RejectDTime = now;
        m.RejectBy = dto.RejectedBy ?? "HQ_REVIEWER";
        m.LogLUDateTime = now;
        m.LogLUBy = dto.RejectedBy ?? "HQ_REVIEWER";

        await _db.SaveChangesAsync();
        return await DetailAsync(saleAwardMinutesNo);
    }

    public async Task<object?> CancelAsync(string saleAwardMinutesNo, CancelAwardMinutesDto dto)
    {
        var cleanNo = saleAwardMinutesNo.Trim().ToUpper();
        var m = await _db.SaleAwardMinutes
            .FirstOrDefaultAsync(x => x.OrgId == _tenant.OrgId && x.SaleAwardMinutesNo.ToUpper() == cleanNo);

        if (m is null) return null;

        if (m.SaleAwardMinutesStatus != SaleAwardMinutesStatus.Pending &&
            m.SaleAwardMinutesStatus != SaleAwardMinutesStatus.Approved1)
        {
            throw new InvalidOperationException($"Chỉ được phép hủy khi biên bản đang ở trạng thái 'Pending' hoặc 'Approved1'. Hiện tại là: {m.SaleAwardMinutesStatus}");
        }

        if (string.IsNullOrWhiteSpace(dto.Reason))
        {
            throw new InvalidOperationException("Lý do hủy biên bản (Reason) không được để trống.");
        }

        var now = DateTime.Now;
        m.SaleAwardMinutesStatus = SaleAwardMinutesStatus.Canceled;
        m.CancelReason = dto.Reason.Trim();
        m.CancelDTime = now;
        m.CancelBy = dto.CancelledBy ?? "HQ_OPERATOR";
        m.LogLUDateTime = now;
        m.LogLUBy = dto.CancelledBy ?? "HQ_OPERATOR";

        await _db.SaveChangesAsync();
        return await DetailAsync(saleAwardMinutesNo);
    }

    public async Task<bool> DeleteDraftAsync(string saleAwardMinutesNo)
    {
        var cleanNo = saleAwardMinutesNo.Trim().ToUpper();
        var m = await _db.SaleAwardMinutes
            .Include(x => x.Details)
            .FirstOrDefaultAsync(x => x.OrgId == _tenant.OrgId && x.SaleAwardMinutesNo.ToUpper() == cleanNo);

        if (m is null) return false;

        if (m.SaleAwardMinutesStatus != SaleAwardMinutesStatus.Pending)
        {
            throw new InvalidOperationException($"Chỉ được phép xóa biên bản khi còn ở trạng thái nháp 'Pending'. Hiện tại là: {m.SaleAwardMinutesStatus}");
        }

        _db.SaleAwardMinutesDetails.RemoveRange(m.Details);
        _db.SaleAwardMinutes.Remove(m);
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<object> StatsAsync()
    {
        var list = await _db.SaleAwardMinutes
            .Include(m => m.Details)
            .Where(m => m.OrgId == _tenant.OrgId)
            .AsNoTracking()
            .ToListAsync();

        var totalMinutes = list.Count;
        var totalCars = list.Sum(m => m.TotalCars);
        var totalAwardAmount = list.Sum(m => m.TotalAwardAmount);

        var byStatus = Enum.GetValues<SaleAwardMinutesStatus>()
            .Select(st => new
            {
                Status = st.ToString(),
                StatusInt = (int)st,
                Count = list.Count(m => m.SaleAwardMinutesStatus == st),
                Cars = list.Where(m => m.SaleAwardMinutesStatus == st).Sum(m => m.TotalCars),
                AwardAmount = list.Where(m => m.SaleAwardMinutesStatus == st).Sum(m => m.TotalAwardAmount)
            }).ToList();

        var byDealer = list
            .GroupBy(m => new { m.DealerCode, m.DealerName })
            .Select(g => new
            {
                g.Key.DealerCode,
                g.Key.DealerName,
                Count = g.Count(),
                Cars = g.Sum(m => m.TotalCars),
                AwardAmount = g.Sum(m => m.TotalAwardAmount),
                FinishedAmount = g.Where(m => m.SaleAwardMinutesStatus == SaleAwardMinutesStatus.Finished).Sum(m => m.TotalAwardAmount)
            })
            .OrderByDescending(x => x.AwardAmount)
            .ToList();

        var allDetails = list.SelectMany(m => m.Details).ToList();
        var byDocument = allDetails
            .GroupBy(d => d.DocumentNo)
            .Select(g => new
            {
                DocumentNo = g.Key,
                CarCount = g.Count(),
                TotalAward = g.Sum(d => d.AmountAward)
            })
            .OrderByDescending(x => x.TotalAward)
            .ToList();

        return new
        {
            totalMinutes,
            totalCars,
            totalAwardAmount,
            byStatus,
            byDealer,
            byDocument
        };
    }
}
