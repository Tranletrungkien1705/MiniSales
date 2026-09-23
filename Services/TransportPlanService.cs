using Microsoft.EntityFrameworkCore;
using MiniSales.Data;
using MiniSales.Models;

namespace MiniSales.Services;

public record CreateTransportPlanDto(
    string? PlanNo = null,
    string? VINPlan = null,
    string? VIN = null,
    string? CarId = null,
    string? RefNo = null,
    string? LCTemp = null,
    string ModelCode = "",
    string? ModelName = null,
    string? SpecCode = null,
    string? ColorCode = null,
    string? ColorName = null,
    string StorageCode = "",
    string? StorageName = null,
    string DealerCode = "",
    string? DealerName = null,
    DateTime? CQStartDate = null,
    DateTime? ExpectedDate = null,
    string? TransporterCode = null,
    string? TransporterName = null,
    TransportPlanType TPType = TransportPlanType.Road,
    string? FProvinceCode = null,
    string? FProvinceName = null,
    string? FDistrictCode = null,
    string? FDistrictName = null,
    string? TProvinceCode = null,
    string? TProvinceName = null,
    string? TDistrictCode = null,
    string? TDistrictName = null,
    string? Remark = null,
    string? CreatedBy = null
);

public record UpdateByKeHoachDto(
    DateTime ExpectedDate,
    string? StorageCode = null,
    string? StorageName = null,
    string? Remark = null,
    string? UpdatedBy = null
);

public record UpdateByLogisticDto(
    string? TransporterCode = null,
    string? TransporterName = null,
    TransportPlanType? TPType = null,
    string? FProvinceCode = null,
    string? FProvinceName = null,
    string? FDistrictCode = null,
    string? FDistrictName = null,
    string? TProvinceCode = null,
    string? TProvinceName = null,
    string? TDistrictCode = null,
    string? TDistrictName = null,
    TransporterPlanStatus? TransporterStatus = null,
    string? Remark = null,
    string? UpdatedBy = null
);

public record UpdateByBanHangDto(
    string? DealerCode = null,
    string? DealerName = null,
    string? CarId = null,
    string? RefNo = null,
    string? LCTemp = null,
    DateTime? CQStartDate = null,
    string? Remark = null,
    string? UpdatedBy = null
);

public record MapVinRealDto(
    string Vin,
    string? CarId = null,
    string? Remark = null,
    string? UpdatedBy = null
);

public record ApproveTransportPlanDto(
    string? ApprovedBy = null,
    string? Remark = null
);

public record CancelTransportPlanDto(
    string Reason,
    string? CancelledBy = null
);

public record CompleteTransportPlanDto(
    DateTime? ActualDate = null,
    string? Note = null,
    string? FinishedBy = null
);

public record TransporterInfoDto(
    string Code,
    string Name,
    string Phone,
    string TransType,
    string Status
);

public interface ITransportPlanService
{
    Task<object> ListAsync(
        string? status = null,
        string? transporterCode = null,
        string? storageCode = null,
        string? dealerCode = null,
        string? modelCode = null,
        bool? flagRealVin = null,
        DateTime? dateFrom = null,
        DateTime? dateTo = null,
        string? vinPlan = null,
        string? vin = null
    );
    Task<object?> GetByPlanNoAsync(string planNo);
    Task<TransportPlan> CreateAsync(CreateTransportPlanDto dto);
    Task<object?> UpdateByKeHoachAsync(string planNo, UpdateByKeHoachDto dto);
    Task<object?> UpdateByLogisticAsync(string planNo, UpdateByLogisticDto dto);
    Task<object?> UpdateByBanHangAsync(string planNo, UpdateByBanHangDto dto);
    Task<object?> MapVinRealAsync(string planNo, MapVinRealDto dto);
    Task<object?> ApproveAsync(string planNo, ApproveTransportPlanDto? dto);
    Task<object?> CompleteAsync(string planNo, CompleteTransportPlanDto? dto);
    Task<object?> CancelAsync(string planNo, CancelTransportPlanDto dto);
    Task<bool> DeleteAsync(string planNo);
    Task<object> GetEligibleCarsForPlanAsync(string? storageCode = null, string? keyword = null);
    List<TransporterInfoDto> GetTransporters();
    Task<object> GetStatsAsync();
}

public class TransportPlanService(AppDbContext db, ITenantContext tenant) : ITransportPlanService
{
    private static readonly Dictionary<string, (string ProvinceCode, string ProvinceName, string DistrictCode, string DistrictName)> StorageRoutes = new(StringComparer.OrdinalIgnoreCase)
    {
        ["KHO_TONG"] = ("HN", "Hà Nội", "HN-DA", "Đông Anh"),
        ["KHO_DONG_ANH"] = ("HN", "Hà Nội", "HN-DA", "Đông Anh"),
        ["KHO_HAI_PHONG"] = ("HP", "Hải Phòng", "HP-HB", "Hồng Bàng"),
        ["KHO_NINH_BINH"] = ("NB", "Ninh Bình", "NB-GK", "Gia Viễn"),
        ["KHO_HCM"] = ("HCM", "TP. Hồ Chí Minh", "HCM-TD", "Thủ Đức")
    };

    private static readonly Dictionary<string, (string ProvinceCode, string ProvinceName, string DistrictCode, string DistrictName)> DealerRoutes = new(StringComparer.OrdinalIgnoreCase)
    {
        ["DL_HANOI"] = ("HN", "Hà Nội", "HN-CG", "Cầu Giấy"),
        ["VN001"] = ("HN", "Hà Nội", "HN-CG", "Cầu Giấy"),
        ["VN065"] = ("HN", "Hà Nội", "HN-DD", "Đống Đa"),
        ["DL_DONGDO"] = ("HN", "Hà Nội", "HN-DD", "Đống Đa"),
        ["DL_BINHDUONG"] = ("BD", "Bình Dương", "BD-TDM", "Thủ Dầu Một"),
        ["VS058"] = ("BD", "Bình Dương", "BD-TDM", "Thủ Dầu Một"),
        ["DL_SAIGON"] = ("HCM", "TP. Hồ Chí Minh", "HCM-Q1", "Quận 1"),
        ["VN002"] = ("HCM", "TP. Hồ Chí Minh", "HCM-Q1", "Quận 1"),
        ["DL_DANANG"] = ("DN", "Đà Nẵng", "DN-HC", "Hải Châu"),
        ["VN003"] = ("DN", "Đà Nẵng", "DN-HC", "Hải Châu")
    };

    private static readonly List<TransporterInfoDto> PredefinedTransporters = new()
    {
        new("TRP-SAOMAI", "Công ty Vận tải Sao Mai Express", "0903123456", "ROAD", "Active"),
        new("TRP-ANVIET", "Đội xe Chuyên dùng An Việt Logistics", "0912987654", "ROAD", "Active"),
        new("TRP-TRUONGHAI", "Tổng công ty Vận tải Biển & Đường bộ Trường Hải", "0988654321", "WATER", "Active"),
        new("TRP-DUONGTAT", "Công ty CP Vận tải Đường sắt Yên Viên", "0977223344", "RAIL", "Active")
    };

    public async Task<object> ListAsync(
        string? status = null,
        string? transporterCode = null,
        string? storageCode = null,
        string? dealerCode = null,
        string? modelCode = null,
        bool? flagRealVin = null,
        DateTime? dateFrom = null,
        DateTime? dateTo = null,
        string? vinPlan = null,
        string? vin = null)
    {
        var q = db.TransportPlans.Where(x => x.OrgId == tenant.OrgId).AsNoTracking();

        if (!string.IsNullOrWhiteSpace(status))
        {
            if (Enum.TryParse<TransportPlanStatus>(status, true, out var stEnum))
                q = q.Where(x => x.Status == stEnum);
            else if (int.TryParse(status, out var stInt) && Enum.IsDefined(typeof(TransportPlanStatus), stInt))
                q = q.Where(x => x.Status == (TransportPlanStatus)stInt);
        }

        if (!string.IsNullOrWhiteSpace(transporterCode))
            q = q.Where(x => x.TransporterCode == transporterCode);

        if (!string.IsNullOrWhiteSpace(storageCode))
            q = q.Where(x => x.StorageCode == storageCode);

        if (!string.IsNullOrWhiteSpace(dealerCode))
            q = q.Where(x => x.DealerCode == dealerCode);

        if (!string.IsNullOrWhiteSpace(modelCode))
            q = q.Where(x => x.ModelCode == modelCode);

        if (flagRealVin.HasValue)
            q = q.Where(x => x.FlagRealVin == flagRealVin.Value);

        if (dateFrom.HasValue)
            q = q.Where(x => x.ExpectedDate >= dateFrom.Value.Date);

        if (dateTo.HasValue)
            q = q.Where(x => x.ExpectedDate <= dateTo.Value.Date.AddDays(1).AddTicks(-1));

        if (!string.IsNullOrWhiteSpace(vinPlan))
            q = q.Where(x => x.VINPlan.Contains(vinPlan.Trim()));

        if (!string.IsNullOrWhiteSpace(vin))
            q = q.Where(x => x.VIN != null && x.VIN.Contains(vin.Trim()));

        var items = await q.OrderByDescending(x => x.CreatedDate).ToListAsync();
        return new
        {
            total = items.Count,
            items
        };
    }

    public async Task<object?> GetByPlanNoAsync(string planNo)
    {
        return await db.TransportPlans
            .Where(x => x.OrgId == tenant.OrgId && x.PlanNo == planNo)
            .AsNoTracking()
            .FirstOrDefaultAsync();
    }

    public async Task<TransportPlan> CreateAsync(CreateTransportPlanDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.ModelCode))
            throw new InvalidOperationException("Mã Model xe (ModelCode) là bắt buộc.");
        if (string.IsNullOrWhiteSpace(dto.StorageCode))
            throw new InvalidOperationException("Kho xuất phát (StorageCode) là bắt buộc.");
        if (string.IsNullOrWhiteSpace(dto.DealerCode))
            throw new InvalidOperationException("Đại lý nhận xe (DealerCode) là bắt buộc.");

        var expectedDate = dto.ExpectedDate ?? DateTime.Today.AddDays(3);

        // Validation 1: Ngày vận chuyển dự kiến >= ngày kiểm tra chất lượng CQ (TranspPlan.cs:1115)
        if (dto.CQStartDate.HasValue && dto.CQStartDate.Value.Date > expectedDate.Date)
            throw new InvalidOperationException($"Ngày vận chuyển dự kiến ({expectedDate:yyyy-MM-dd}) phải lớn hơn hoặc bằng ngày kiểm tra chất lượng CQ ({dto.CQStartDate:yyyy-MM-dd}).");

        // Validation 2: Ngày vận chuyển dự kiến >= Ngày hiện tại (TranspPlan.cs:1130)
        if (expectedDate.Date < DateTime.Today)
            throw new InvalidOperationException($"Ngày vận tải dự kiến ({expectedDate:yyyy-MM-dd}) phải lớn hơn hoặc bằng ngày hiện tại ({DateTime.Today:yyyy-MM-dd}).");

        // Generate PlanNo if not provided
        var yyMM = DateTime.Today.ToString("yyMM");
        string planNo = dto.PlanNo?.Trim() ?? "";
        if (string.IsNullOrWhiteSpace(planNo))
        {
            var countThisMonth = await db.TransportPlans.CountAsync(x => x.OrgId == tenant.OrgId && x.PlanNo.StartsWith(yyMM + "TP")) + 1;
            planNo = $"{yyMM}TP{countThisMonth:D4}";
        }

        // Generate VINPlan if not provided
        string vinPlan = dto.VINPlan?.Trim() ?? "";
        if (string.IsNullOrWhiteSpace(vinPlan))
        {
            vinPlan = $"PL-{dto.ModelCode}-{yyMM}{new Random().Next(1000, 9999)}";
        }

        // Validation 3: Trùng key VINPlan + ExpectedDate (TranspPlan.cs:1210)
        var isDuplicate = await db.TransportPlans.AnyAsync(x =>
            x.OrgId == tenant.OrgId &&
            x.VINPlan == vinPlan &&
            x.ExpectedDate.Date == expectedDate.Date
        );
        if (isDuplicate)
            throw new InvalidOperationException($"Đã tồn tại kế hoạch vận tải cho số khung kế hoạch {vinPlan} vào ngày {expectedDate:yyyy-MM-dd}.");

        // Derive route if not supplied (Sto_TranspPlan_GenProv_Distr_HQ TranspPlan.cs:4255)
        string fProv = dto.FProvinceCode ?? "";
        string fProvName = dto.FProvinceName ?? "";
        string fDist = dto.FDistrictCode ?? "";
        string fDistName = dto.FDistrictName ?? "";
        if (string.IsNullOrWhiteSpace(fProv) && StorageRoutes.TryGetValue(dto.StorageCode, out var sRoute))
        {
            fProv = sRoute.ProvinceCode;
            fProvName = sRoute.ProvinceName;
            fDist = sRoute.DistrictCode;
            fDistName = sRoute.DistrictName;
        }

        string tProv = dto.TProvinceCode ?? "";
        string tProvName = dto.TProvinceName ?? "";
        string tDist = dto.TDistrictCode ?? "";
        string tDistName = dto.TDistrictName ?? "";
        if (string.IsNullOrWhiteSpace(tProv) && DealerRoutes.TryGetValue(dto.DealerCode, out var dRoute))
        {
            tProv = dRoute.ProvinceCode;
            tProvName = dRoute.ProvinceName;
            tDist = dRoute.DistrictCode;
            tDistName = dRoute.DistrictName;
        }

        bool flagRealVin = false;
        string? realVin = null;
        if (!string.IsNullOrWhiteSpace(dto.VIN))
        {
            realVin = dto.VIN.Trim().ToUpperInvariant();
            if (realVin.Length != 17)
                throw new InvalidOperationException("Số khung thực tế (VIN) phải đủ 17 ký tự.");
            flagRealVin = true;
        }

        var plan = new TransportPlan
        {
            OrgId = tenant.OrgId,
            PlanNo = planNo,
            VINPlan = vinPlan,
            VIN = realVin,
            FlagRealVin = flagRealVin,
            CarId = dto.CarId,
            RefNo = dto.RefNo,
            LCTemp = dto.LCTemp,
            ModelCode = dto.ModelCode.Trim().ToUpperInvariant(),
            ModelName = dto.ModelName ?? dto.ModelCode,
            SpecCode = dto.SpecCode,
            ColorCode = dto.ColorCode,
            ColorName = dto.ColorName,
            StorageCode = dto.StorageCode.Trim().ToUpperInvariant(),
            StorageName = dto.StorageName ?? dto.StorageCode,
            DealerCode = dto.DealerCode.Trim().ToUpperInvariant(),
            DealerName = dto.DealerName ?? dto.DealerCode,
            CQStartDate = dto.CQStartDate,
            ExpectedDate = expectedDate,
            TransporterCode = dto.TransporterCode,
            TransporterName = dto.TransporterName,
            TPType = dto.TPType,
            FProvinceCode = fProv,
            FProvinceName = fProvName,
            FDistrictCode = fDist,
            FDistrictName = fDistName,
            TProvinceCode = tProv,
            TProvinceName = tProvName,
            TDistrictCode = tDist,
            TDistrictName = tDistName,
            Status = TransportPlanStatus.Pending,
            TransporterStatus = TransporterPlanStatus.Pending,
            Remark = dto.Remark,
            CreatedBy = dto.CreatedBy ?? "HQ_PLANNER",
            CreatedDate = DateTime.Now
        };

        db.TransportPlans.Add(plan);
        await db.SaveChangesAsync();
        return plan;
    }

    public async Task<object?> UpdateByKeHoachAsync(string planNo, UpdateByKeHoachDto dto)
    {
        var plan = await db.TransportPlans.FirstOrDefaultAsync(x => x.OrgId == tenant.OrgId && x.PlanNo == planNo);
        if (plan is null) return null;

        if (plan.Status != TransportPlanStatus.Pending)
            throw new InvalidOperationException("Góc nhìn Kế hoạch chỉ được điều chỉnh khi kế hoạch ở trạng thái Chờ duyệt (Pending).");

        if (dto.ExpectedDate.Date < DateTime.Today)
            throw new InvalidOperationException($"Ngày vận chuyển dự kiến ({dto.ExpectedDate:yyyy-MM-dd}) không được nhỏ hơn ngày hiện tại.");

        if (plan.CQStartDate.HasValue && dto.ExpectedDate.Date < plan.CQStartDate.Value.Date)
            throw new InvalidOperationException($"Ngày vận chuyển dự kiến ({dto.ExpectedDate:yyyy-MM-dd}) không được nhỏ hơn ngày kiểm tra chất lượng ({plan.CQStartDate:yyyy-MM-dd}).");

        plan.ExpectedDate = dto.ExpectedDate;
        if (!string.IsNullOrWhiteSpace(dto.StorageCode))
        {
            plan.StorageCode = dto.StorageCode.Trim().ToUpperInvariant();
            plan.StorageName = dto.StorageName ?? dto.StorageCode;
            if (StorageRoutes.TryGetValue(plan.StorageCode, out var route))
            {
                plan.FProvinceCode = route.ProvinceCode;
                plan.FProvinceName = route.ProvinceName;
                plan.FDistrictCode = route.DistrictCode;
                plan.FDistrictName = route.DistrictName;
            }
        }

        if (!string.IsNullOrWhiteSpace(dto.Remark))
            plan.Remark = dto.Remark;

        plan.LUDateTime = DateTime.Now;
        plan.LUBy = dto.UpdatedBy ?? "HQ_KEHOACH";

        await db.SaveChangesAsync();
        return plan;
    }

    public async Task<object?> UpdateByLogisticAsync(string planNo, UpdateByLogisticDto dto)
    {
        var plan = await db.TransportPlans.FirstOrDefaultAsync(x => x.OrgId == tenant.OrgId && x.PlanNo == planNo);
        if (plan is null) return null;

        if (plan.Status != TransportPlanStatus.Pending)
            throw new InvalidOperationException("Góc nhìn Logistics chỉ được điều chỉnh khi kế hoạch ở trạng thái Chờ duyệt (Pending).");

        if (!string.IsNullOrWhiteSpace(dto.TransporterCode))
        {
            plan.TransporterCode = dto.TransporterCode;
            plan.TransporterName = dto.TransporterName ?? PredefinedTransporters.FirstOrDefault(t => t.Code == dto.TransporterCode)?.Name ?? dto.TransporterCode;
        }

        if (dto.TPType.HasValue)
            plan.TPType = dto.TPType.Value;

        if (!string.IsNullOrWhiteSpace(dto.FProvinceCode)) plan.FProvinceCode = dto.FProvinceCode;
        if (!string.IsNullOrWhiteSpace(dto.FProvinceName)) plan.FProvinceName = dto.FProvinceName;
        if (!string.IsNullOrWhiteSpace(dto.FDistrictCode)) plan.FDistrictCode = dto.FDistrictCode;
        if (!string.IsNullOrWhiteSpace(dto.FDistrictName)) plan.FDistrictName = dto.FDistrictName;

        if (!string.IsNullOrWhiteSpace(dto.TProvinceCode)) plan.TProvinceCode = dto.TProvinceCode;
        if (!string.IsNullOrWhiteSpace(dto.TProvinceName)) plan.TProvinceName = dto.TProvinceName;
        if (!string.IsNullOrWhiteSpace(dto.TDistrictCode)) plan.TDistrictCode = dto.TDistrictCode;
        if (!string.IsNullOrWhiteSpace(dto.TDistrictName)) plan.TDistrictName = dto.TDistrictName;

        if (dto.TransporterStatus.HasValue)
            plan.TransporterStatus = dto.TransporterStatus.Value;

        if (!string.IsNullOrWhiteSpace(dto.Remark))
            plan.Remark = dto.Remark;

        plan.LUDateTime = DateTime.Now;
        plan.LUBy = dto.UpdatedBy ?? "HQ_LOGISTICS";

        await db.SaveChangesAsync();
        return plan;
    }

    public async Task<object?> UpdateByBanHangAsync(string planNo, UpdateByBanHangDto dto)
    {
        var plan = await db.TransportPlans.FirstOrDefaultAsync(x => x.OrgId == tenant.OrgId && x.PlanNo == planNo);
        if (plan is null) return null;

        if (plan.Status != TransportPlanStatus.Pending)
            throw new InvalidOperationException("Góc nhìn Bán hàng chỉ được điều chỉnh khi kế hoạch ở trạng thái Chờ duyệt (Pending).");

        if (!string.IsNullOrWhiteSpace(dto.DealerCode))
        {
            plan.DealerCode = dto.DealerCode.Trim().ToUpperInvariant();
            plan.DealerName = dto.DealerName ?? dto.DealerCode;
            if (DealerRoutes.TryGetValue(plan.DealerCode, out var route))
            {
                plan.TProvinceCode = route.ProvinceCode;
                plan.TProvinceName = route.ProvinceName;
                plan.TDistrictCode = route.DistrictCode;
                plan.TDistrictName = route.DistrictName;
            }
        }

        if (dto.CQStartDate.HasValue)
        {
            if (dto.CQStartDate.Value.Date > plan.ExpectedDate.Date)
                throw new InvalidOperationException($"Ngày bắt đầu kiểm tra CQ ({dto.CQStartDate:yyyy-MM-dd}) không được lớn hơn ngày vận chuyển dự kiến ({plan.ExpectedDate:yyyy-MM-dd}).");
            plan.CQStartDate = dto.CQStartDate.Value;
        }

        if (!string.IsNullOrWhiteSpace(dto.CarId)) plan.CarId = dto.CarId;
        if (!string.IsNullOrWhiteSpace(dto.RefNo)) plan.RefNo = dto.RefNo;
        if (!string.IsNullOrWhiteSpace(dto.LCTemp)) plan.LCTemp = dto.LCTemp;
        if (!string.IsNullOrWhiteSpace(dto.Remark)) plan.Remark = dto.Remark;

        plan.LUDateTime = DateTime.Now;
        plan.LUBy = dto.UpdatedBy ?? "HQ_BANHANG";

        await db.SaveChangesAsync();
        return plan;
    }

    public async Task<object?> MapVinRealAsync(string planNo, MapVinRealDto dto)
    {
        var plan = await db.TransportPlans.FirstOrDefaultAsync(x => x.OrgId == tenant.OrgId && x.PlanNo == planNo);
        if (plan is null) return null;

        if (string.IsNullOrWhiteSpace(dto.Vin) || dto.Vin.Trim().Length != 17)
            throw new InvalidOperationException("Số khung thực tế (VIN) phải đúng 17 ký tự.");

        var cleanVin = dto.Vin.Trim().ToUpperInvariant();

        // Check if VIN already in another pending or approved transport plan
        var existingPlan = await db.TransportPlans.AnyAsync(x =>
            x.OrgId == tenant.OrgId &&
            x.PlanNo != planNo &&
            x.VIN == cleanVin &&
            (x.Status == TransportPlanStatus.Pending || x.Status == TransportPlanStatus.Approved)
        );
        if (existingPlan)
            throw new InvalidOperationException($"Số khung VIN {cleanVin} đã được gán cho một kế hoạch vận tải khác đang hoạt động.");

        plan.VIN = cleanVin;
        plan.FlagRealVin = true;
        if (!string.IsNullOrWhiteSpace(dto.CarId)) plan.CarId = dto.CarId;
        if (!string.IsNullOrWhiteSpace(dto.Remark)) plan.Remark = dto.Remark;

        plan.LUDateTime = DateTime.Now;
        plan.LUBy = dto.UpdatedBy ?? "HQ_MAPVIN";

        await db.SaveChangesAsync();
        return plan;
    }

    public async Task<object?> ApproveAsync(string planNo, ApproveTransportPlanDto? dto)
    {
        var plan = await db.TransportPlans.FirstOrDefaultAsync(x => x.OrgId == tenant.OrgId && x.PlanNo == planNo);
        if (plan is null) return null;

        if (plan.Status != TransportPlanStatus.Pending)
            throw new InvalidOperationException($"Kế hoạch vận tải đang ở trạng thái {plan.Status}, không thể phê duyệt lại.");

        if (string.IsNullOrWhiteSpace(plan.TransporterCode))
            throw new InvalidOperationException("Chưa chỉ định đơn vị vận tải (TransporterCode). Vui lòng cập nhật đơn vị vận tải trước khi phê duyệt.");

        plan.Status = TransportPlanStatus.Approved;
        plan.ApprovedDate = DateTime.Now;
        plan.ApprovedBy = dto?.ApprovedBy ?? "HQ_LOGISTICS_DIRECTOR";

        if (plan.TransporterStatus == TransporterPlanStatus.Pending)
            plan.TransporterStatus = TransporterPlanStatus.Accepted;

        if (!string.IsNullOrWhiteSpace(dto?.Remark))
            plan.Remark = dto.Remark;

        plan.LUDateTime = DateTime.Now;
        plan.LUBy = plan.ApprovedBy;

        await db.SaveChangesAsync();
        return plan;
    }

    public async Task<object?> CompleteAsync(string planNo, CompleteTransportPlanDto? dto)
    {
        var plan = await db.TransportPlans.FirstOrDefaultAsync(x => x.OrgId == tenant.OrgId && x.PlanNo == planNo);
        if (plan is null) return null;

        if (plan.Status != TransportPlanStatus.Approved)
            throw new InvalidOperationException("Chỉ kế hoạch đã duyệt (Approved) mới có thể xác nhận hoàn tất giao xe.");

        plan.Status = TransportPlanStatus.Finished;
        plan.TransporterStatus = TransporterPlanStatus.Finished;
        plan.ActualDate = dto?.ActualDate ?? DateTime.Now;
        plan.FinishedDate = DateTime.Now;
        plan.FinishedBy = dto?.FinishedBy ?? "TRANSPORTER_DISPATCHER";

        if (!string.IsNullOrWhiteSpace(dto?.Note))
            plan.Remark = string.IsNullOrWhiteSpace(plan.Remark) ? dto.Note : $"{plan.Remark} | {dto.Note}";

        plan.LUDateTime = DateTime.Now;
        plan.LUBy = plan.FinishedBy;

        await db.SaveChangesAsync();
        return plan;
    }

    public async Task<object?> CancelAsync(string planNo, CancelTransportPlanDto dto)
    {
        var plan = await db.TransportPlans.FirstOrDefaultAsync(x => x.OrgId == tenant.OrgId && x.PlanNo == planNo);
        if (plan is null) return null;

        if (plan.Status == TransportPlanStatus.Finished)
            throw new InvalidOperationException("Không thể hủy kế hoạch vận tải đã hoàn thành giao hàng.");

        plan.Status = TransportPlanStatus.Cancelled;
        plan.CancelReason = dto.Reason;
        plan.CancelledDate = DateTime.Now;
        plan.CancelledBy = dto.CancelledBy ?? "HQ_OPERATOR";
        plan.LUDateTime = DateTime.Now;
        plan.LUBy = plan.CancelledBy;

        await db.SaveChangesAsync();
        return plan;
    }

    public async Task<bool> DeleteAsync(string planNo)
    {
        var plan = await db.TransportPlans.FirstOrDefaultAsync(x => x.OrgId == tenant.OrgId && x.PlanNo == planNo);
        if (plan is null) return false;

        // Validation: Sto_TranspPlan_DelX (TranspPlan.cs:4205)
        if (plan.Status != TransportPlanStatus.Pending)
            throw new InvalidOperationException("Kế hoạch vận tải chỉ được xóa khi ở trạng thái Chờ duyệt (Pending).");

        if (plan.TransporterStatus == TransporterPlanStatus.Delivering || plan.TransporterStatus == TransporterPlanStatus.Finished)
            throw new InvalidOperationException("Không thể xóa kế hoạch đang vận chuyển hoặc đã giao xe xong.");

        db.TransportPlans.Remove(plan);
        await db.SaveChangesAsync();
        return true;
    }

    public async Task<object> GetEligibleCarsForPlanAsync(string? storageCode = null, string? keyword = null)
    {
        var q = db.Cars.Where(x => x.OrgId == tenant.OrgId && x.FlagActive == "1").AsNoTracking();

        if (!string.IsNullOrWhiteSpace(storageCode))
        {
            q = q.Where(x => x.StorageCode == storageCode);
        }

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var kw = keyword.Trim().ToLower();
            q = q.Where(x => (x.Vin != null && x.Vin.ToLower().Contains(kw)) ||
                             x.ModelName.ToLower().Contains(kw) ||
                             x.CarId.ToLower().Contains(kw));
        }

        var cars = await q.Take(30).ToListAsync();
        return cars.Select(c => new
        {
            c.CarId,
            c.Vin,
            Model = c.ModelName,
            c.ModelCode,
            c.SpecCode,
            c.ColorCode,
            UnitPriceActual = c.PriceActual,
            c.DealerCode,
            c.StorageCode,
            c.FlagMapVIN,
            FlagDeliveryOrder = c.FlagCarDeliveryOrder
        });
    }

    public List<TransporterInfoDto> GetTransporters() => PredefinedTransporters;

    public async Task<object> GetStatsAsync()
    {
        var plans = await db.TransportPlans
            .Where(x => x.OrgId == tenant.OrgId)
            .AsNoTracking()
            .ToListAsync();

        var total = plans.Count;
        var pending = plans.Count(x => x.Status == TransportPlanStatus.Pending);
        var approved = plans.Count(x => x.Status == TransportPlanStatus.Approved);
        var finished = plans.Count(x => x.Status == TransportPlanStatus.Finished);
        var cancelled = plans.Count(x => x.Status == TransportPlanStatus.Cancelled);

        var realVinCount = plans.Count(x => x.FlagRealVin);
        var planVinCount = plans.Count(x => !x.FlagRealVin);

        var byTransporter = plans
            .Where(x => !string.IsNullOrWhiteSpace(x.TransporterName))
            .GroupBy(x => x.TransporterName!)
            .Select(g => new { Transporter = g.Key, Count = g.Count() })
            .ToList();

        var byStorage = plans
            .Where(x => !string.IsNullOrWhiteSpace(x.StorageName))
            .GroupBy(x => x.StorageName!)
            .Select(g => new { Storage = g.Key, Count = g.Count() })
            .ToList();

        var byDealer = plans
            .Where(x => !string.IsNullOrWhiteSpace(x.DealerName))
            .GroupBy(x => x.DealerName!)
            .Select(g => new { Dealer = g.Key, Count = g.Count() })
            .ToList();

        return new
        {
            total,
            pending,
            approved,
            finished,
            cancelled,
            realVinCount,
            planVinCount,
            byTransporter,
            byStorage,
            byDealer
        };
    }
}
