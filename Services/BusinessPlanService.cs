using Microsoft.EntityFrameworkCore;
using MiniSales.Data;
using MiniSales.Models;

namespace MiniSales.Services;

public record BusinessPlanDetailInputDto(
    string ModelCode,
    string? ModelName = null,
    int Rtl_QtyM1 = 0,
    int Rtl_QtyM2 = 0,
    int Rtl_QtyM3 = 0,
    int Rtl_QtyM4 = 0,
    int Rtl_QtyM5 = 0,
    int Rtl_QtyM6 = 0,
    int Rtl_QtyM7 = 0,
    int Rtl_QtyM8 = 0,
    int Rtl_QtyM9 = 0,
    int Rtl_QtyM10 = 0,
    int Rtl_QtyM11 = 0,
    int Rtl_QtyM12 = 0,
    int Rtl_QtyPre = 0,
    int Ord_QtyM1 = 0,
    int Ord_QtyM2 = 0,
    int Ord_QtyM3 = 0,
    int Ord_QtyM4 = 0,
    int Ord_QtyM5 = 0,
    int Ord_QtyM6 = 0,
    int Ord_QtyM7 = 0,
    int Ord_QtyM8 = 0,
    int Ord_QtyM9 = 0,
    int Ord_QtyM10 = 0,
    int Ord_QtyM11 = 0,
    int Ord_QtyM12 = 0,
    int Ord_QtyPre = 0,
    int BO_TotalQtyBO = 0,
    string? Remark = null
);

public record CreateBusinessPlanDto(
    string DealerCode,
    string? DealerName = null,
    int YearPlan = 2026,
    string? HTCStaffInCharge = null,
    string? Remark = null,
    string? CreatedBy = null,
    List<BusinessPlanDetailInputDto>? Details = null
);

public record UpdateBusinessPlanDto(
    string? HTCStaffInCharge = null,
    string? Remark = null,
    string? UpdatedBy = null,
    List<BusinessPlanDetailInputDto>? Details = null
);

public record Approve1BusinessPlanDto(
    string? ApprovedBy = null,
    int? TimesPlan = null,
    string? Remark = null
);

public record Approve2BusinessPlanDto(
    string? ApprovedBy = null,
    string? Remark = null
);

public record UnApproveBusinessPlanDto(
    string? Reason = null,
    string? UnApprovedBy = null
);

public record CancelBusinessPlanDto(
    string Reason,
    string? CancelledBy = null
);

public record CalcPlanRequestDto(
    string DealerCode,
    int YearPlan = 2026,
    int EstimatedTotalRtl = 1200
);

public record StandardModelDto(
    string ModelCode,
    string ModelName,
    string Segment,
    int DefaultPreYearRtl,
    int DefaultPreYearOrd
);

public interface IBusinessPlanService
{
    Task<object> ListAsync(
        int? yearPlan = null,
        string? dealerCode = null,
        BusinessPlanStatus? status = null,
        string? version = null,
        int page = 0,
        int pageSize = 10
    );

    Task<object?> DetailAsync(string planCode);
    Task<object> GetSeqAsync();
    Task<object> GetStandardModelsAsync();
    Task<object> CalculateInitialPlanAsync(CalcPlanRequestDto dto);
    Task<object> CreateAsync(CreateBusinessPlanDto dto);
    Task<object?> UpdateAsync(string planCode, UpdateBusinessPlanDto dto);
    Task<object?> Approve1Async(string planCode, Approve1BusinessPlanDto? dto = null);
    Task<object?> Approve2Async(string planCode, Approve2BusinessPlanDto? dto = null);
    Task<object?> UnApproveAsync(string planCode, UnApproveBusinessPlanDto? dto = null);
    Task<object?> CancelAsync(string planCode, CancelBusinessPlanDto dto);
    Task<bool> DeleteDraftAsync(string planCode);
    Task<object> StatsAsync(int? yearPlan = null);
}

public class BusinessPlanService : IBusinessPlanService
{
    private readonly AppDbContext _db;
    private readonly ITenantContext _tenant;

    public BusinessPlanService(AppDbContext db, ITenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    private static readonly List<StandardModelDto> StandardModels = new()
    {
        new("SANTAFE", "Hyundai Santa Fe All New", "SUV-D", 280, 290),
        new("TUCSON", "Hyundai Tucson Turbo", "SUV-C", 320, 330),
        new("CRETA", "Hyundai Creta B-SUV", "SUV-B", 380, 400),
        new("ACCENT", "Hyundai Accent Sedan", "SEDAN-B", 450, 470),
        new("ELANTRA", "Hyundai Elantra N-Line", "SEDAN-C", 160, 170),
        new("STARGAZER", "Hyundai Stargazer MPV", "MPV-B", 140, 150),
        new("CUSTIN", "Hyundai Custin MPV VIP", "MPV-C", 120, 130),
        new("PALISADE", "Hyundai Palisade Flagship", "SUV-E", 90, 100),
        new("IONIQ5", "Hyundai Ioniq 5 Electric", "EV", 50, 60),
        new("VENUE", "Hyundai Venue Urban SUV", "SUV-A", 110, 120)
    };

    public Task<object> GetStandardModelsAsync()
    {
        return Task.FromResult<object>(StandardModels);
    }

    public async Task<object> GetSeqAsync()
    {
        var prefix = $"{DateTime.Today:yyMM}BPL";
        var count = await _db.BusinessPlans
            .CountAsync(x => x.OrgId == _tenant.OrgId && x.BusinessPlanCode.StartsWith(prefix));
        var seqNo = $"{prefix}{(count + 1):D4}";
        return new { nextPlanCode = seqNo };
    }

    public async Task<object> ListAsync(
        int? yearPlan = null,
        string? dealerCode = null,
        BusinessPlanStatus? status = null,
        string? version = null,
        int page = 0,
        int pageSize = 10
    )
    {
        var query = _db.BusinessPlans
            .Include(x => x.Details)
            .Where(x => x.OrgId == _tenant.OrgId)
            .AsNoTracking();

        if (yearPlan.HasValue) query = query.Where(x => x.YearPlan == yearPlan.Value);
        if (!string.IsNullOrWhiteSpace(dealerCode)) query = query.Where(x => x.DealerCode == dealerCode.Trim());
        if (status.HasValue) query = query.Where(x => x.Status == status.Value);
        if (!string.IsNullOrWhiteSpace(version)) query = query.Where(x => x.Version == version.Trim());

        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(x => x.YearPlan)
            .ThenByDescending(x => x.CreatedAt)
            .Skip(page * pageSize)
            .Take(pageSize)
            .Select(x => new
            {
                x.Id,
                x.BusinessPlanCode,
                x.DealerCode,
                x.DealerName,
                x.YearPlan,
                x.Version,
                x.TimesPlan,
                Status = x.Status.ToString(),
                StatusCode = (int)x.Status,
                x.HTCStaffInCharge,
                x.TotalRtlTarget,
                x.TotalOrdTarget,
                x.TotalBOInitial,
                TotalModels = x.Details.Count,
                x.Remark,
                x.CreatedAt,
                x.CreatedBy,
                x.Approve1At,
                x.Approve1By,
                x.Approve2At,
                x.Approve2By,
                x.CancelledAt,
                x.CancelReason
            })
            .ToListAsync();

        return new
        {
            total,
            page,
            pageSize,
            totalPages = (int)Math.Ceiling((double)total / pageSize),
            items
        };
    }

    public async Task<object?> DetailAsync(string planCode)
    {
        var plan = await _db.BusinessPlans
            .Include(x => x.Details)
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.OrgId == _tenant.OrgId && x.BusinessPlanCode == planCode);

        if (plan is null) return null;

        return new
        {
            plan.Id,
            plan.BusinessPlanCode,
            plan.DealerCode,
            plan.DealerName,
            plan.YearPlan,
            plan.Version,
            plan.TimesPlan,
            Status = plan.Status.ToString(),
            StatusCode = (int)plan.Status,
            plan.HTCStaffInCharge,
            plan.TotalRtlTarget,
            plan.TotalOrdTarget,
            plan.TotalBOInitial,
            plan.Remark,
            plan.RejectReason,
            plan.CancelReason,
            plan.CancelledBy,
            plan.CancelledAt,
            plan.CreatedAt,
            plan.CreatedBy,
            plan.Approve1At,
            plan.Approve1By,
            plan.Approve2At,
            plan.Approve2By,
            plan.LogLUDateTime,
            plan.LogLUBy,
            QuarterSummary = new
            {
                Rtl = new
                {
                    Q1 = plan.Details.Sum(d => d.Rtl_QtyM1 + d.Rtl_QtyM2 + d.Rtl_QtyM3),
                    Q2 = plan.Details.Sum(d => d.Rtl_QtyM4 + d.Rtl_QtyM5 + d.Rtl_QtyM6),
                    Q3 = plan.Details.Sum(d => d.Rtl_QtyM7 + d.Rtl_QtyM8 + d.Rtl_QtyM9),
                    Q4 = plan.Details.Sum(d => d.Rtl_QtyM10 + d.Rtl_QtyM11 + d.Rtl_QtyM12),
                    Total = plan.TotalRtlTarget
                },
                Ord = new
                {
                    Q1 = plan.Details.Sum(d => d.Ord_QtyM1 + d.Ord_QtyM2 + d.Ord_QtyM3),
                    Q2 = plan.Details.Sum(d => d.Ord_QtyM4 + d.Ord_QtyM5 + d.Ord_QtyM6),
                    Q3 = plan.Details.Sum(d => d.Ord_QtyM7 + d.Ord_QtyM8 + d.Ord_QtyM9),
                    Q4 = plan.Details.Sum(d => d.Ord_QtyM10 + d.Ord_QtyM11 + d.Ord_QtyM12),
                    Total = plan.TotalOrdTarget
                }
            },
            Details = plan.Details.OrderBy(d => d.ModelCode).Select(d => new
            {
                d.Id,
                d.BusinessPlanCode,
                d.ModelCode,
                d.ModelName,
                d.Rtl_TotalQtyDeal,
                Rtl_Monthly = new[] { d.Rtl_QtyM1, d.Rtl_QtyM2, d.Rtl_QtyM3, d.Rtl_QtyM4, d.Rtl_QtyM5, d.Rtl_QtyM6, d.Rtl_QtyM7, d.Rtl_QtyM8, d.Rtl_QtyM9, d.Rtl_QtyM10, d.Rtl_QtyM11, d.Rtl_QtyM12 },
                d.Rtl_QtyPre,
                d.Rtl_GrowthRate,
                d.Ord_TotalQtyOrder,
                Ord_Monthly = new[] { d.Ord_QtyM1, d.Ord_QtyM2, d.Ord_QtyM3, d.Ord_QtyM4, d.Ord_QtyM5, d.Ord_QtyM6, d.Ord_QtyM7, d.Ord_QtyM8, d.Ord_QtyM9, d.Ord_QtyM10, d.Ord_QtyM11, d.Ord_QtyM12 },
                d.Ord_QtyPre,
                d.Ord_GrowthRate,
                d.BO_TotalQtyBO,
                d.Remark
            })
        };
    }

    public Task<object> CalculateInitialPlanAsync(CalcPlanRequestDto dto)
    {
        var totalEstimated = dto.EstimatedTotalRtl > 0 ? dto.EstimatedTotalRtl : 1200;
        var sumPreRtl = StandardModels.Sum(m => m.DefaultPreYearRtl);

        var details = StandardModels.Select(m =>
        {
            var share = (double)m.DefaultPreYearRtl / sumPreRtl;
            var targetModelRtl = (int)Math.Round(totalEstimated * share);
            var targetModelOrd = (int)Math.Round(targetModelRtl * 1.05); // Ord thường nhỉnh hơn bán lẻ ~5% để dự phòng

            // Phân bổ 12 tháng theo mùa vụ (Q1: 20%, Q2: 25%, Q3: 25%, Q4: 30%)
            var q1 = (int)Math.Round(targetModelRtl * 0.20 / 3);
            var q2 = (int)Math.Round(targetModelRtl * 0.25 / 3);
            var q3 = (int)Math.Round(targetModelRtl * 0.25 / 3);
            var q4 = (int)Math.Round(targetModelRtl * 0.30 / 3);

            var ord_q1 = (int)Math.Round(targetModelOrd * 0.20 / 3);
            var ord_q2 = (int)Math.Round(targetModelOrd * 0.25 / 3);
            var ord_q3 = (int)Math.Round(targetModelOrd * 0.25 / 3);
            var ord_q4 = (int)Math.Round(targetModelOrd * 0.30 / 3);

            var rtlM = new int[] { q1, q1, q1, q2, q2, q2, q3, q3, q3, q4, q4, targetModelRtl - (q1 * 3 + q2 * 3 + q3 * 3 + q4 * 2) };
            var ordM = new int[] { ord_q1, ord_q1, ord_q1, ord_q2, ord_q2, ord_q2, ord_q3, ord_q3, ord_q3, ord_q4, ord_q4, targetModelOrd - (ord_q1 * 3 + ord_q2 * 3 + ord_q3 * 3 + ord_q4 * 2) };

            var growthRtl = m.DefaultPreYearRtl > 0
                ? Math.Round(((decimal)targetModelRtl - m.DefaultPreYearRtl) / m.DefaultPreYearRtl * 100m, 2)
                : 0m;
            var growthOrd = m.DefaultPreYearOrd > 0
                ? Math.Round(((decimal)targetModelOrd - m.DefaultPreYearOrd) / m.DefaultPreYearOrd * 100m, 2)
                : 0m;

            return new
            {
                m.ModelCode,
                m.ModelName,
                m.Segment,
                Rtl_TotalQtyDeal = targetModelRtl,
                Rtl_Monthly = rtlM,
                Rtl_QtyPre = m.DefaultPreYearRtl,
                Rtl_GrowthRate = growthRtl,
                Ord_TotalQtyOrder = targetModelOrd,
                Ord_Monthly = ordM,
                Ord_QtyPre = m.DefaultPreYearOrd,
                Ord_GrowthRate = growthOrd,
                BO_TotalQtyBO = (int)Math.Round(m.DefaultPreYearRtl * 0.08)
            };
        }).ToList();

        return Task.FromResult<object>(new
        {
            dto.DealerCode,
            dto.YearPlan,
            TotalRtlTarget = details.Sum(x => x.Rtl_TotalQtyDeal),
            TotalOrdTarget = details.Sum(x => x.Ord_TotalQtyOrder),
            TotalBOInitial = details.Sum(x => x.BO_TotalQtyBO),
            Details = details
        });
    }

    public async Task<object> CreateAsync(CreateBusinessPlanDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.DealerCode))
            throw new InvalidOperationException("Mã đại lý (DealerCode) không được để trống.");

        if (dto.YearPlan < 2024 || dto.YearPlan > 2035)
            throw new InvalidOperationException($"Năm kế hoạch (YearPlan) {dto.YearPlan} không hợp lệ. Phải trong khoảng 2024 - 2035.");

        var existingPlan = await _db.BusinessPlans
            .AnyAsync(x => x.OrgId == _tenant.OrgId
                && x.DealerCode == dto.DealerCode.Trim()
                && x.YearPlan == dto.YearPlan
                && x.Status != BusinessPlanStatus.Cancelled);

        if (existingPlan)
            throw new InvalidOperationException($"Đại lý {dto.DealerCode} đã có Kế hoạch kinh doanh năm {dto.YearPlan} đang hoạt động trong hệ thống.");

        var seqObj = (dynamic)await GetSeqAsync();
        string planCode = seqObj.nextPlanCode;

        var plan = new BusinessPlan
        {
            OrgId = _tenant.OrgId,
            BusinessPlanCode = planCode,
            DealerCode = dto.DealerCode.Trim(),
            DealerName = !string.IsNullOrWhiteSpace(dto.DealerName) ? dto.DealerName.Trim() : dto.DealerCode.Trim(),
            YearPlan = dto.YearPlan,
            Version = "INIT",
            TimesPlan = 1,
            Status = BusinessPlanStatus.Pending,
            HTCStaffInCharge = dto.HTCStaffInCharge?.Trim(),
            Remark = dto.Remark?.Trim(),
            CreatedBy = dto.CreatedBy?.Trim() ?? "HQ_PLANNING",
            CreatedAt = DateTime.Now,
            LogLUDateTime = DateTime.Now,
            LogLUBy = dto.CreatedBy?.Trim() ?? "HQ_PLANNING"
        };

        var detailsInput = dto.Details;
        if (detailsInput == null || detailsInput.Count == 0)
        {
            // Tự động khởi tạo theo StandardModels nếu người dùng không truyền detail
            var calcObj = (dynamic)await CalculateInitialPlanAsync(new CalcPlanRequestDto(dto.DealerCode, dto.YearPlan));
            var autoDetails = (IEnumerable<dynamic>)calcObj.Details;
            detailsInput = autoDetails.Select(d => new BusinessPlanDetailInputDto(
                ModelCode: d.ModelCode,
                ModelName: d.ModelName,
                Rtl_QtyM1: d.Rtl_Monthly[0], Rtl_QtyM2: d.Rtl_Monthly[1], Rtl_QtyM3: d.Rtl_Monthly[2],
                Rtl_QtyM4: d.Rtl_Monthly[3], Rtl_QtyM5: d.Rtl_Monthly[4], Rtl_QtyM6: d.Rtl_Monthly[5],
                Rtl_QtyM7: d.Rtl_Monthly[6], Rtl_QtyM8: d.Rtl_Monthly[7], Rtl_QtyM9: d.Rtl_Monthly[8],
                Rtl_QtyM10: d.Rtl_Monthly[9], Rtl_QtyM11: d.Rtl_Monthly[10], Rtl_QtyM12: d.Rtl_Monthly[11],
                Rtl_QtyPre: d.Rtl_QtyPre,
                Ord_QtyM1: d.Ord_Monthly[0], Ord_QtyM2: d.Ord_Monthly[1], Ord_QtyM3: d.Ord_Monthly[2],
                Ord_QtyM4: d.Ord_Monthly[3], Ord_QtyM5: d.Ord_Monthly[4], Ord_QtyM6: d.Ord_Monthly[5],
                Ord_QtyM7: d.Ord_Monthly[6], Ord_QtyM8: d.Ord_Monthly[7], Ord_QtyM9: d.Ord_Monthly[8],
                Ord_QtyM10: d.Ord_Monthly[9], Ord_QtyM11: d.Ord_Monthly[10], Ord_QtyM12: d.Ord_Monthly[11],
                Ord_QtyPre: d.Ord_QtyPre,
                BO_TotalQtyBO: d.BO_TotalQtyBO
            )).ToList();
        }

        foreach (var item in detailsInput)
        {
            var rtlTotal = item.Rtl_QtyM1 + item.Rtl_QtyM2 + item.Rtl_QtyM3 + item.Rtl_QtyM4 + item.Rtl_QtyM5 + item.Rtl_QtyM6 +
                           item.Rtl_QtyM7 + item.Rtl_QtyM8 + item.Rtl_QtyM9 + item.Rtl_QtyM10 + item.Rtl_QtyM11 + item.Rtl_QtyM12;
            var ordTotal = item.Ord_QtyM1 + item.Ord_QtyM2 + item.Ord_QtyM3 + item.Ord_QtyM4 + item.Ord_QtyM5 + item.Ord_QtyM6 +
                           item.Ord_QtyM7 + item.Ord_QtyM8 + item.Ord_QtyM9 + item.Ord_QtyM10 + item.Ord_QtyM11 + item.Ord_QtyM12;

            var std = StandardModels.FirstOrDefault(m => m.ModelCode.Equals(item.ModelCode, StringComparison.OrdinalIgnoreCase));
            var modelName = !string.IsNullOrWhiteSpace(item.ModelName) ? item.ModelName : (std?.ModelName ?? item.ModelCode);

            var rtlGrowth = item.Rtl_QtyPre > 0 ? Math.Round(((decimal)rtlTotal - item.Rtl_QtyPre) / item.Rtl_QtyPre * 100m, 2) : 0m;
            var ordGrowth = item.Ord_QtyPre > 0 ? Math.Round(((decimal)ordTotal - item.Ord_QtyPre) / item.Ord_QtyPre * 100m, 2) : 0m;

            plan.Details.Add(new BusinessPlanDetail
            {
                BusinessPlanCode = planCode,
                ModelCode = item.ModelCode.Trim().ToUpper(),
                ModelName = modelName,
                Rtl_TotalQtyDeal = rtlTotal,
                Rtl_QtyM1 = item.Rtl_QtyM1, Rtl_QtyM2 = item.Rtl_QtyM2, Rtl_QtyM3 = item.Rtl_QtyM3,
                Rtl_QtyM4 = item.Rtl_QtyM4, Rtl_QtyM5 = item.Rtl_QtyM5, Rtl_QtyM6 = item.Rtl_QtyM6,
                Rtl_QtyM7 = item.Rtl_QtyM7, Rtl_QtyM8 = item.Rtl_QtyM8, Rtl_QtyM9 = item.Rtl_QtyM9,
                Rtl_QtyM10 = item.Rtl_QtyM10, Rtl_QtyM11 = item.Rtl_QtyM11, Rtl_QtyM12 = item.Rtl_QtyM12,
                Rtl_QtyPre = item.Rtl_QtyPre,
                Rtl_GrowthRate = rtlGrowth,
                Ord_TotalQtyOrder = ordTotal,
                Ord_QtyM1 = item.Ord_QtyM1, Ord_QtyM2 = item.Ord_QtyM2, Ord_QtyM3 = item.Ord_QtyM3,
                Ord_QtyM4 = item.Ord_QtyM4, Ord_QtyM5 = item.Ord_QtyM5, Ord_QtyM6 = item.Ord_QtyM6,
                Ord_QtyM7 = item.Ord_QtyM7, Ord_QtyM8 = item.Ord_QtyM8, Ord_QtyM9 = item.Ord_QtyM9,
                Ord_QtyM10 = item.Ord_QtyM10, Ord_QtyM11 = item.Ord_QtyM11, Ord_QtyM12 = item.Ord_QtyM12,
                Ord_QtyPre = item.Ord_QtyPre,
                Ord_GrowthRate = ordGrowth,
                BO_TotalQtyBO = item.BO_TotalQtyBO,
                Remark = item.Remark
            });
        }

        plan.TotalRtlTarget = plan.Details.Sum(d => d.Rtl_TotalQtyDeal);
        plan.TotalOrdTarget = plan.Details.Sum(d => d.Ord_TotalQtyOrder);
        plan.TotalBOInitial = plan.Details.Sum(d => d.BO_TotalQtyBO);

        _db.BusinessPlans.Add(plan);
        await _db.SaveChangesAsync();

        return (await DetailAsync(planCode))!;
    }

    public async Task<object?> UpdateAsync(string planCode, UpdateBusinessPlanDto dto)
    {
        var plan = await _db.BusinessPlans
            .Include(x => x.Details)
            .FirstOrDefaultAsync(x => x.OrgId == _tenant.OrgId && x.BusinessPlanCode == planCode);

        if (plan is null) return null;

        if (plan.Status != BusinessPlanStatus.Pending)
            throw new InvalidOperationException($"Chỉ được chỉnh sửa kế hoạch kinh doanh ở trạng thái Chờ duyệt (Pending). Trạng thái hiện tại: {plan.Status}");

        if (dto.HTCStaffInCharge != null) plan.HTCStaffInCharge = dto.HTCStaffInCharge.Trim();
        if (dto.Remark != null) plan.Remark = dto.Remark.Trim();
        plan.LogLUDateTime = DateTime.Now;
        plan.LogLUBy = dto.UpdatedBy?.Trim() ?? "HQ_PLANNING";

        if (dto.Details != null && dto.Details.Count > 0)
        {
            _db.BusinessPlanDetails.RemoveRange(plan.Details);
            plan.Details.Clear();

            foreach (var item in dto.Details)
            {
                var rtlTotal = item.Rtl_QtyM1 + item.Rtl_QtyM2 + item.Rtl_QtyM3 + item.Rtl_QtyM4 + item.Rtl_QtyM5 + item.Rtl_QtyM6 +
                               item.Rtl_QtyM7 + item.Rtl_QtyM8 + item.Rtl_QtyM9 + item.Rtl_QtyM10 + item.Rtl_QtyM11 + item.Rtl_QtyM12;
                var ordTotal = item.Ord_QtyM1 + item.Ord_QtyM2 + item.Ord_QtyM3 + item.Ord_QtyM4 + item.Ord_QtyM5 + item.Ord_QtyM6 +
                               item.Ord_QtyM7 + item.Ord_QtyM8 + item.Ord_QtyM9 + item.Ord_QtyM10 + item.Ord_QtyM11 + item.Ord_QtyM12;

                var std = StandardModels.FirstOrDefault(m => m.ModelCode.Equals(item.ModelCode, StringComparison.OrdinalIgnoreCase));
                var modelName = !string.IsNullOrWhiteSpace(item.ModelName) ? item.ModelName : (std?.ModelName ?? item.ModelCode);

                var rtlGrowth = item.Rtl_QtyPre > 0 ? Math.Round(((decimal)rtlTotal - item.Rtl_QtyPre) / item.Rtl_QtyPre * 100m, 2) : 0m;
                var ordGrowth = item.Ord_QtyPre > 0 ? Math.Round(((decimal)ordTotal - item.Ord_QtyPre) / item.Ord_QtyPre * 100m, 2) : 0m;

                plan.Details.Add(new BusinessPlanDetail
                {
                    BusinessPlanCode = planCode,
                    ModelCode = item.ModelCode.Trim().ToUpper(),
                    ModelName = modelName,
                    Rtl_TotalQtyDeal = rtlTotal,
                    Rtl_QtyM1 = item.Rtl_QtyM1, Rtl_QtyM2 = item.Rtl_QtyM2, Rtl_QtyM3 = item.Rtl_QtyM3,
                    Rtl_QtyM4 = item.Rtl_QtyM4, Rtl_QtyM5 = item.Rtl_QtyM5, Rtl_QtyM6 = item.Rtl_QtyM6,
                    Rtl_QtyM7 = item.Rtl_QtyM7, Rtl_QtyM8 = item.Rtl_QtyM8, Rtl_QtyM9 = item.Rtl_QtyM9,
                    Rtl_QtyM10 = item.Rtl_QtyM10, Rtl_QtyM11 = item.Rtl_QtyM11, Rtl_QtyM12 = item.Rtl_QtyM12,
                    Rtl_QtyPre = item.Rtl_QtyPre,
                    Rtl_GrowthRate = rtlGrowth,
                    Ord_TotalQtyOrder = ordTotal,
                    Ord_QtyM1 = item.Ord_QtyM1, Ord_QtyM2 = item.Ord_QtyM2, Ord_QtyM3 = item.Ord_QtyM3,
                    Ord_QtyM4 = item.Ord_QtyM4, Ord_QtyM5 = item.Ord_QtyM5, Ord_QtyM6 = item.Ord_QtyM6,
                    Ord_QtyM7 = item.Ord_QtyM7, Ord_QtyM8 = item.Ord_QtyM8, Ord_QtyM9 = item.Ord_QtyM9,
                    Ord_QtyM10 = item.Ord_QtyM10, Ord_QtyM11 = item.Ord_QtyM11, Ord_QtyM12 = item.Ord_QtyM12,
                    Ord_QtyPre = item.Ord_QtyPre,
                    Ord_GrowthRate = ordGrowth,
                    BO_TotalQtyBO = item.BO_TotalQtyBO,
                    Remark = item.Remark
                });
            }

            plan.TotalRtlTarget = plan.Details.Sum(d => d.Rtl_TotalQtyDeal);
            plan.TotalOrdTarget = plan.Details.Sum(d => d.Ord_TotalQtyOrder);
            plan.TotalBOInitial = plan.Details.Sum(d => d.BO_TotalQtyBO);
        }

        await _db.SaveChangesAsync();
        return (await DetailAsync(planCode))!;
    }

    public async Task<object?> Approve1Async(string planCode, Approve1BusinessPlanDto? dto = null)
    {
        var plan = await _db.BusinessPlans
            .FirstOrDefaultAsync(x => x.OrgId == _tenant.OrgId && x.BusinessPlanCode == planCode);

        if (plan is null) return null;

        if (plan.Status != BusinessPlanStatus.Pending)
            throw new InvalidOperationException($"Chỉ được duyệt cấp 1 khi kế hoạch đang ở trạng thái Chờ duyệt (Pending). Trạng thái hiện tại: {plan.Status}");

        plan.Status = BusinessPlanStatus.Approved1;
        plan.Approve1At = DateTime.Now;
        plan.Approve1By = dto?.ApprovedBy?.Trim() ?? "HQ_SALES_PLAN_MNG";
        if (dto?.TimesPlan.HasValue == true && dto.TimesPlan.Value > 0)
        {
            plan.TimesPlan = dto.TimesPlan.Value;
        }
        if (!string.IsNullOrWhiteSpace(dto?.Remark))
        {
            plan.Remark = $"{plan.Remark} | Duyệt C1: {dto.Remark.Trim()}".Trim(' ', '|');
        }
        plan.LogLUDateTime = DateTime.Now;
        plan.LogLUBy = plan.Approve1By;

        await _db.SaveChangesAsync();
        return (await DetailAsync(planCode))!;
    }

    public async Task<object?> Approve2Async(string planCode, Approve2BusinessPlanDto? dto = null)
    {
        var plan = await _db.BusinessPlans
            .FirstOrDefaultAsync(x => x.OrgId == _tenant.OrgId && x.BusinessPlanCode == planCode);

        if (plan is null) return null;

        if (plan.Status != BusinessPlanStatus.Approved1)
            throw new InvalidOperationException($"Chỉ được duyệt cấp 2 (chốt kế hoạch) khi kế hoạch đã qua duyệt cấp 1 (Approved1). Trạng thái hiện tại: {plan.Status}");

        plan.Status = BusinessPlanStatus.Approved2;
        plan.Version = "ACTUAL";
        plan.Approve2At = DateTime.Now;
        plan.Approve2By = dto?.ApprovedBy?.Trim() ?? "HQ_SALES_DIRECTOR";
        if (!string.IsNullOrWhiteSpace(dto?.Remark))
        {
            plan.Remark = $"{plan.Remark} | Duyệt C2: {dto.Remark.Trim()}".Trim(' ', '|');
        }
        plan.LogLUDateTime = DateTime.Now;
        plan.LogLUBy = plan.Approve2By;

        await _db.SaveChangesAsync();
        return (await DetailAsync(planCode))!;
    }

    public async Task<object?> UnApproveAsync(string planCode, UnApproveBusinessPlanDto? dto = null)
    {
        var plan = await _db.BusinessPlans
            .FirstOrDefaultAsync(x => x.OrgId == _tenant.OrgId && x.BusinessPlanCode == planCode);

        if (plan is null) return null;

        if (plan.Status != BusinessPlanStatus.Approved2)
            throw new InvalidOperationException($"Chỉ được hủy duyệt kế hoạch đã duyệt chốt cấp 2 (Approved2). Trạng thái hiện tại: {plan.Status}");

        plan.Status = BusinessPlanStatus.Pending;
        plan.Version = "INIT";
        plan.Approve1At = null;
        plan.Approve1By = null;
        plan.Approve2At = null;
        plan.Approve2By = null;
        if (!string.IsNullOrWhiteSpace(dto?.Reason))
        {
            plan.Remark = $"{plan.Remark} | Hủy duyệt: {dto.Reason.Trim()}".Trim(' ', '|');
        }
        plan.LogLUDateTime = DateTime.Now;
        plan.LogLUBy = dto?.UnApprovedBy?.Trim() ?? "HQ_SALES_DIRECTOR";

        await _db.SaveChangesAsync();
        return (await DetailAsync(planCode))!;
    }

    public async Task<object?> CancelAsync(string planCode, CancelBusinessPlanDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Reason))
            throw new InvalidOperationException("Lý do hủy kế hoạch kinh doanh không được để trống.");

        var plan = await _db.BusinessPlans
            .FirstOrDefaultAsync(x => x.OrgId == _tenant.OrgId && x.BusinessPlanCode == planCode);

        if (plan is null) return null;

        if (plan.Status == BusinessPlanStatus.Cancelled)
            throw new InvalidOperationException("Kế hoạch kinh doanh đã ở trạng thái Đã hủy (Cancelled).");

        if (plan.Status == BusinessPlanStatus.Approved2)
            throw new InvalidOperationException("Không thể hủy kế hoạch đã duyệt chốt cấp 2 (Approved2). Vui lòng thực hiện Hủy duyệt (UnApprove) trước.");

        plan.Status = BusinessPlanStatus.Cancelled;
        plan.CancelReason = dto.Reason.Trim();
        plan.CancelledBy = dto.CancelledBy?.Trim() ?? "HQ_ADMIN";
        plan.CancelledAt = DateTime.Now;
        plan.LogLUDateTime = DateTime.Now;
        plan.LogLUBy = plan.CancelledBy;

        await _db.SaveChangesAsync();
        return (await DetailAsync(planCode))!;
    }

    public async Task<bool> DeleteDraftAsync(string planCode)
    {
        var plan = await _db.BusinessPlans
            .Include(x => x.Details)
            .FirstOrDefaultAsync(x => x.OrgId == _tenant.OrgId && x.BusinessPlanCode == planCode);

        if (plan is null) return false;

        if (plan.Status != BusinessPlanStatus.Pending)
            throw new InvalidOperationException($"Chỉ được xóa kế hoạch ở trạng thái Chờ duyệt (Pending). Trạng thái hiện tại: {plan.Status}");

        _db.BusinessPlans.Remove(plan);
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<object> StatsAsync(int? yearPlan = null)
    {
        var query = _db.BusinessPlans
            .Include(x => x.Details)
            .Where(x => x.OrgId == _tenant.OrgId)
            .AsNoTracking();

        if (yearPlan.HasValue)
        {
            query = query.Where(x => x.YearPlan == yearPlan.Value);
        }

        var plans = await query.ToListAsync();

        var totalPlans = plans.Count;
        var pendingCount = plans.Count(x => x.Status == BusinessPlanStatus.Pending);
        var approved1Count = plans.Count(x => x.Status == BusinessPlanStatus.Approved1);
        var approved2Count = plans.Count(x => x.Status == BusinessPlanStatus.Approved2);
        var cancelledCount = plans.Count(x => x.Status == BusinessPlanStatus.Cancelled);

        var activePlans = plans.Where(x => x.Status != BusinessPlanStatus.Cancelled).ToList();
        var totalRetailTarget = activePlans.Sum(x => x.TotalRtlTarget);
        var totalOrderTarget = activePlans.Sum(x => x.TotalOrdTarget);
        var totalBOInitial = activePlans.Sum(x => x.TotalBOInitial);

        var modelBreakdown = activePlans
            .SelectMany(p => p.Details)
            .GroupBy(d => d.ModelCode)
            .Select(g => new
            {
                ModelCode = g.Key,
                ModelName = g.First().ModelName,
                TotalRtlTarget = g.Sum(x => x.Rtl_TotalQtyDeal),
                TotalOrdTarget = g.Sum(x => x.Ord_TotalQtyOrder),
                TotalBO = g.Sum(x => x.BO_TotalQtyBO),
                PercentageOfRtl = totalRetailTarget > 0 ? Math.Round((decimal)g.Sum(x => x.Rtl_TotalQtyDeal) / totalRetailTarget * 100m, 2) : 0m
            })
            .OrderByDescending(x => x.TotalRtlTarget)
            .ToList();

        var q1Rtl = activePlans.SelectMany(p => p.Details).Sum(d => d.Rtl_QtyM1 + d.Rtl_QtyM2 + d.Rtl_QtyM3);
        var q2Rtl = activePlans.SelectMany(p => p.Details).Sum(d => d.Rtl_QtyM4 + d.Rtl_QtyM5 + d.Rtl_QtyM6);
        var q3Rtl = activePlans.SelectMany(p => p.Details).Sum(d => d.Rtl_QtyM7 + d.Rtl_QtyM8 + d.Rtl_QtyM9);
        var q4Rtl = activePlans.SelectMany(p => p.Details).Sum(d => d.Rtl_QtyM10 + d.Rtl_QtyM11 + d.Rtl_QtyM12);

        var q1Ord = activePlans.SelectMany(p => p.Details).Sum(d => d.Ord_QtyM1 + d.Ord_QtyM2 + d.Ord_QtyM3);
        var q2Ord = activePlans.SelectMany(p => p.Details).Sum(d => d.Ord_QtyM4 + d.Ord_QtyM5 + d.Ord_QtyM6);
        var q3Ord = activePlans.SelectMany(p => p.Details).Sum(d => d.Ord_QtyM7 + d.Ord_QtyM8 + d.Ord_QtyM9);
        var q4Ord = activePlans.SelectMany(p => p.Details).Sum(d => d.Ord_QtyM10 + d.Ord_QtyM11 + d.Ord_QtyM12);

        return new
        {
            YearFilter = yearPlan ?? DateTime.Today.Year,
            TotalPlans = totalPlans,
            PendingCount = pendingCount,
            Approved1Count = approved1Count,
            Approved2Count = approved2Count,
            CancelledCount = cancelledCount,
            TotalRetailTarget = totalRetailTarget,
            TotalOrderTarget = totalOrderTarget,
            TotalBOInitial = totalBOInitial,
            QuarterlyRtl = new { Q1 = q1Rtl, Q2 = q2Rtl, Q3 = q3Rtl, Q4 = q4Rtl },
            QuarterlyOrd = new { Q1 = q1Ord, Q2 = q2Ord, Q3 = q3Ord, Q4 = q4Ord },
            TopModels = modelBreakdown
        };
    }
}
