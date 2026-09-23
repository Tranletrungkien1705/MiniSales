using Microsoft.EntityFrameworkCore;
using MiniSales.Data;
using MiniSales.Models;

namespace MiniSales.Services;

// ===== DTOs: Mst_CarModel (Dòng xe / Model) =====
public record CreateCarModelDto(
    string ModelCode = "",
    string ModelProductionCode = "",
    string ModelName = "",
    string FlagBusinessPlan = "1",
    string FlagActive = "1",
    string? ActionBy = null
);

public record UpdateCarModelDto(
    string ModelProductionCode = "",
    string ModelName = "",
    string FlagBusinessPlan = "1",
    string FlagActive = "1",
    string? ActionBy = null
);

public record CarModelDto(
    long Id,
    string ModelCode,
    string ModelProductionCode,
    string ModelName,
    string FlagBusinessPlan,
    bool IsBusinessPlan,
    string FlagActive,
    bool IsActive,
    DateTime LogLUDateTime,
    string? LogLUBy
);

// ===== DTOs: Mst_CarColor (Màu sắc xe) =====
public record CreateCarColorDto(
    string ModelCode = "",
    string ColorCode = "",
    string ColorExtType = "",
    string ColorExtCode = "",
    string ColorExtName = "",
    string ColorExtNameVN = "",
    string ColorIntCode = "",
    string ColorIntName = "",
    string ColorIntNameVN = "",
    decimal ColorFee = 0,
    string? Remark = null,
    string? ActionBy = null
);

public record UpdateCarColorDto(
    string ColorExtName = "",
    string ColorExtNameVN = "",
    string ColorIntName = "",
    string ColorIntNameVN = "",
    decimal ColorFee = 0,
    string? Remark = null,
    string FlagActive = "1",
    string? ActionBy = null
);

public record CarColorDto(
    long Id,
    string ModelCode,
    string? ModelName,
    string ColorCode,
    string ColorExtType,
    string ColorExtCode,
    string ColorExtName,
    string ColorExtNameVN,
    string ColorIntCode,
    string ColorIntName,
    string ColorIntNameVN,
    decimal ColorFee,
    string? Remark,
    string FlagActive,
    bool IsActive,
    DateTime LogLUDateTime,
    string? LogLUBy
);

public record CarModelStatsDto(
    int TotalModels,
    int TotalActiveModels,
    int TotalBusinessPlanModels,
    int TotalColors,
    int TotalActiveColors,
    Dictionary<string, int> ColorsByModel
);

public interface ICarModelService
{
    // Mst_CarModel
    Task<List<CarModelDto>> SearchModelsAsync(string? modelCode, string? flagActive);
    Task<CarModelDto?> GetModelAsync(string modelCode);
    Task<CarModelDto> CreateModelAsync(CreateCarModelDto dto);
    Task<CarModelDto?> UpdateModelAsync(string modelCode, UpdateCarModelDto dto);
    Task<bool> DeleteModelAsync(string modelCode);

    // Mst_CarColor
    Task<List<CarColorDto>> SearchColorsAsync(string? modelCode, string? colorCode, string? flagActive);
    Task<CarColorDto?> GetColorAsync(string modelCode, string colorCode);
    Task<CarColorDto> CreateColorAsync(CreateCarColorDto dto);
    Task<CarColorDto?> UpdateColorAsync(string modelCode, string colorCode, UpdateCarColorDto dto);
    Task<bool> DeleteColorAsync(string modelCode, string colorCode);

    Task<CarModelStatsDto> GetStatsAsync();
}

/// <summary>
/// Quản lý Danh mục Dòng xe (Model) & Màu sắc xe (DMS.Sales Mst_CarModel + Mst_CarColor /
/// Master.Car.cs / Mst_CarModel_Get|Create|Update|Delete + Mst_CarColor_Get|Create|Update|Delete):
/// - Mst_CarModel: cấp cao nhất trong phân cấp sản phẩm Model → Spec → Color. ModelCode là khóa nghiệp vụ
///   duy nhất; ModelProductionCode và ModelName bắt buộc; FlagBusinessPlan/FlagActive là cờ '1'/'0'.
/// - Mst_CarColor: màu sắc cho từng dòng xe, khóa nghiệp vụ = (ModelCode, ColorCode). ModelCode phải tồn tại
///   và đang hoạt động (Mst_CarModel_CheckDB - FlagExistToCheck = Yes, FlagActive = Active). Các trường màu
///   ngoại/nội thất bắt buộc; ColorFee phải là số >= 0.
/// </summary>
public sealed class CarModelService(AppDbContext db, ITenantContext tenant) : ICarModelService
{
    private Guid Org => tenant.OrgId;

    // ===== Mst_CarModel =====
    public async Task<List<CarModelDto>> SearchModelsAsync(string? modelCode, string? flagActive)
    {
        var q = db.CarModels.AsNoTracking().Where(x => x.OrgId == Org);
        if (!string.IsNullOrWhiteSpace(modelCode))
        {
            var mc = modelCode.Trim();
            q = q.Where(x => x.ModelCode == mc);
        }
        if (!string.IsNullOrWhiteSpace(flagActive))
        {
            var fa = flagActive.Trim();
            q = q.Where(x => x.FlagActive == fa);
        }
        var list = await q.OrderBy(x => x.ModelCode).ToListAsync();
        return list.Select(ToModelDto).ToList();
    }

    public async Task<CarModelDto?> GetModelAsync(string modelCode)
    {
        if (string.IsNullOrWhiteSpace(modelCode)) return null;
        var mc = modelCode.Trim();
        var e = await db.CarModels.AsNoTracking().FirstOrDefaultAsync(x => x.OrgId == Org && x.ModelCode == mc);
        return e is null ? null : ToModelDto(e);
    }

    public async Task<CarModelDto> CreateModelAsync(CreateCarModelDto dto)
    {
        var modelCode = (dto.ModelCode ?? "").Trim();
        if (modelCode.Length < 1)
            throw new InvalidOperationException("Mã dòng xe (ModelCode) không được để trống.");

        // ModelCode là khóa nghiệp vụ duy nhất (Mst_CarModel_CheckDB - FlagExistToCheck = No)
        if (await db.CarModels.AnyAsync(x => x.OrgId == Org && x.ModelCode == modelCode))
            throw new InvalidOperationException($"Dòng xe '{modelCode}' đã tồn tại.");

        var productionCode = (dto.ModelProductionCode ?? "").Trim();
        if (productionCode.Length < 1)
            throw new InvalidOperationException("Mã sản xuất (ModelProductionCode) không được để trống.");

        var modelName = (dto.ModelName ?? "").Trim();
        if (modelName.Length < 1)
            throw new InvalidOperationException("Tên dòng xe (ModelName) không được để trống.");

        var user = string.IsNullOrWhiteSpace(dto.ActionBy) ? "CHUYEN_VIEN_NPP" : dto.ActionBy.Trim();
        var entity = new CarModelMaster
        {
            OrgId = Org,
            ModelCode = modelCode,
            ModelProductionCode = productionCode,
            ModelName = modelName,
            FlagBusinessPlan = NormalizeFlag(dto.FlagBusinessPlan),
            FlagActive = NormalizeFlag(dto.FlagActive),
            LogLUDateTime = DateTime.Now,
            LogLUBy = user
        };
        db.CarModels.Add(entity);
        await db.SaveChangesAsync();
        return ToModelDto(entity);
    }

    public async Task<CarModelDto?> UpdateModelAsync(string modelCode, UpdateCarModelDto dto)
    {
        if (string.IsNullOrWhiteSpace(modelCode)) return null;
        var mc = modelCode.Trim();
        var entity = await db.CarModels.FirstOrDefaultAsync(x => x.OrgId == Org && x.ModelCode == mc);
        if (entity is null) return null;

        var productionCode = (dto.ModelProductionCode ?? "").Trim();
        if (productionCode.Length < 1)
            throw new InvalidOperationException("Mã sản xuất (ModelProductionCode) không được để trống.");

        var modelName = (dto.ModelName ?? "").Trim();
        if (modelName.Length < 1)
            throw new InvalidOperationException("Tên dòng xe (ModelName) không được để trống.");

        var user = string.IsNullOrWhiteSpace(dto.ActionBy) ? "CHUYEN_VIEN_NPP" : dto.ActionBy.Trim();
        entity.ModelProductionCode = productionCode;
        entity.ModelName = modelName;
        entity.FlagBusinessPlan = NormalizeFlag(dto.FlagBusinessPlan);
        entity.FlagActive = NormalizeFlag(dto.FlagActive);
        entity.LogLUDateTime = DateTime.Now;
        entity.LogLUBy = user;

        await db.SaveChangesAsync();
        return ToModelDto(entity);
    }

    public async Task<bool> DeleteModelAsync(string modelCode)
    {
        if (string.IsNullOrWhiteSpace(modelCode)) return false;
        var mc = modelCode.Trim();
        var entity = await db.CarModels.FirstOrDefaultAsync(x => x.OrgId == Org && x.ModelCode == mc);
        if (entity is null) return false;

        // Chặn xóa khi còn màu sắc đang tham chiếu dòng xe (FK Mst_CarModel -> Mst_CarColor)
        var inUse = await db.CarColors.AnyAsync(x => x.OrgId == Org && x.ModelCode == mc);
        if (inUse)
            throw new InvalidOperationException($"Không thể xóa dòng xe '{mc}' vì đang có màu sắc được thiết lập cho dòng xe này.");

        db.CarModels.Remove(entity);
        await db.SaveChangesAsync();
        return true;
    }

    // ===== Mst_CarColor =====
    public async Task<List<CarColorDto>> SearchColorsAsync(string? modelCode, string? colorCode, string? flagActive)
    {
        var q = db.CarColors.AsNoTracking().Where(x => x.OrgId == Org);
        if (!string.IsNullOrWhiteSpace(modelCode))
        {
            var mc = modelCode.Trim();
            q = q.Where(x => x.ModelCode == mc);
        }
        if (!string.IsNullOrWhiteSpace(colorCode))
        {
            var cc = colorCode.Trim();
            q = q.Where(x => x.ColorCode == cc);
        }
        if (!string.IsNullOrWhiteSpace(flagActive))
        {
            var fa = flagActive.Trim();
            q = q.Where(x => x.FlagActive == fa);
        }
        var list = await q.OrderBy(x => x.ModelCode).ThenBy(x => x.ColorCode).ToListAsync();
        return list.Select(ToColorDto).ToList();
    }

    public async Task<CarColorDto?> GetColorAsync(string modelCode, string colorCode)
    {
        if (string.IsNullOrWhiteSpace(modelCode) || string.IsNullOrWhiteSpace(colorCode)) return null;
        var mc = modelCode.Trim();
        var cc = colorCode.Trim();
        var e = await db.CarColors.AsNoTracking()
            .FirstOrDefaultAsync(x => x.OrgId == Org && x.ModelCode == mc && x.ColorCode == cc);
        return e is null ? null : ToColorDto(e);
    }

    public async Task<CarColorDto> CreateColorAsync(CreateCarColorDto dto)
    {
        var modelCode = (dto.ModelCode ?? "").Trim();
        if (modelCode.Length < 1)
            throw new InvalidOperationException("Mã dòng xe (ModelCode) không được để trống.");

        // FK: dòng xe phải tồn tại và đang hoạt động (Mst_CarModel_CheckDB - FlagExistToCheck = Yes, FlagActive = Active)
        await CheckModelExistsAsync(modelCode);

        var colorCode = (dto.ColorCode ?? "").Trim();
        if (colorCode.Length < 1)
            throw new InvalidOperationException("Mã màu (ColorCode) không được để trống.");

        // Khóa nghiệp vụ (ModelCode, ColorCode) duy nhất (Mst_CarColor_CheckDB - FlagExistToCheck = No)
        if (await db.CarColors.AnyAsync(x => x.OrgId == Org && x.ModelCode == modelCode && x.ColorCode == colorCode))
            throw new InvalidOperationException($"Màu '{colorCode}' của dòng xe '{modelCode}' đã tồn tại.");

        var extType = (dto.ColorExtType ?? "").Trim();
        if (extType.Length < 1)
            throw new InvalidOperationException("Loại màu ngoại thất (ColorExtType) không được để trống.");
        var extCode = (dto.ColorExtCode ?? "").Trim();
        if (extCode.Length < 1)
            throw new InvalidOperationException("Mã màu ngoại thất (ColorExtCode) không được để trống.");
        var extName = (dto.ColorExtName ?? "").Trim();
        if (extName.Length < 1)
            throw new InvalidOperationException("Tên màu ngoại thất (ColorExtName) không được để trống.");
        var extNameVN = (dto.ColorExtNameVN ?? "").Trim();
        if (extNameVN.Length < 1)
            throw new InvalidOperationException("Tên màu ngoại thất tiếng Việt (ColorExtNameVN) không được để trống.");
        var intCode = (dto.ColorIntCode ?? "").Trim();
        if (intCode.Length < 1)
            throw new InvalidOperationException("Mã màu nội thất (ColorIntCode) không được để trống.");
        var intName = (dto.ColorIntName ?? "").Trim();
        if (intName.Length < 1)
            throw new InvalidOperationException("Tên màu nội thất (ColorIntName) không được để trống.");
        var intNameVN = (dto.ColorIntNameVN ?? "").Trim();
        if (intNameVN.Length < 1)
            throw new InvalidOperationException("Tên màu nội thất tiếng Việt (ColorIntNameVN) không được để trống.");
        if (dto.ColorFee < 0)
            throw new InvalidOperationException("Phụ phí màu (ColorFee) phải là số >= 0.");

        var user = string.IsNullOrWhiteSpace(dto.ActionBy) ? "CHUYEN_VIEN_NPP" : dto.ActionBy.Trim();
        var entity = new CarColorMaster
        {
            OrgId = Org,
            ModelCode = modelCode,
            ColorCode = colorCode,
            ColorExtType = extType,
            ColorExtCode = extCode,
            ColorExtName = extName,
            ColorExtNameVN = extNameVN,
            ColorIntCode = intCode,
            ColorIntName = intName,
            ColorIntNameVN = intNameVN,
            ColorFee = dto.ColorFee,
            Remark = dto.Remark,
            FlagActive = "1", // Mst_CarColor_Create luôn ghi FlagActive = Active
            LogLUDateTime = DateTime.Now,
            LogLUBy = user
        };
        db.CarColors.Add(entity);
        await db.SaveChangesAsync();
        return ToColorDto(entity);
    }

    public async Task<CarColorDto?> UpdateColorAsync(string modelCode, string colorCode, UpdateCarColorDto dto)
    {
        if (string.IsNullOrWhiteSpace(modelCode) || string.IsNullOrWhiteSpace(colorCode)) return null;
        var mc = modelCode.Trim();
        var cc = colorCode.Trim();
        var entity = await db.CarColors
            .FirstOrDefaultAsync(x => x.OrgId == Org && x.ModelCode == mc && x.ColorCode == cc);
        if (entity is null) return null;

        var extName = (dto.ColorExtName ?? "").Trim();
        if (extName.Length < 1)
            throw new InvalidOperationException("Tên màu ngoại thất (ColorExtName) không được để trống.");
        var extNameVN = (dto.ColorExtNameVN ?? "").Trim();
        if (extNameVN.Length < 1)
            throw new InvalidOperationException("Tên màu ngoại thất tiếng Việt (ColorExtNameVN) không được để trống.");
        var intName = (dto.ColorIntName ?? "").Trim();
        if (intName.Length < 1)
            throw new InvalidOperationException("Tên màu nội thất (ColorIntName) không được để trống.");
        var intNameVN = (dto.ColorIntNameVN ?? "").Trim();
        if (intNameVN.Length < 1)
            throw new InvalidOperationException("Tên màu nội thất tiếng Việt (ColorIntNameVN) không được để trống.");
        if (dto.ColorFee < 0)
            throw new InvalidOperationException("Phụ phí màu (ColorFee) phải là số >= 0.");

        var user = string.IsNullOrWhiteSpace(dto.ActionBy) ? "CHUYEN_VIEN_NPP" : dto.ActionBy.Trim();
        entity.ColorExtName = extName;
        entity.ColorExtNameVN = extNameVN;
        entity.ColorIntName = intName;
        entity.ColorIntNameVN = intNameVN;
        entity.ColorFee = dto.ColorFee;
        if (dto.Remark is not null) entity.Remark = dto.Remark;
        entity.FlagActive = NormalizeFlag(dto.FlagActive);
        entity.LogLUDateTime = DateTime.Now;
        entity.LogLUBy = user;

        await db.SaveChangesAsync();
        return ToColorDto(entity);
    }

    public async Task<bool> DeleteColorAsync(string modelCode, string colorCode)
    {
        if (string.IsNullOrWhiteSpace(modelCode) || string.IsNullOrWhiteSpace(colorCode)) return false;
        var mc = modelCode.Trim();
        var cc = colorCode.Trim();
        var entity = await db.CarColors
            .FirstOrDefaultAsync(x => x.OrgId == Org && x.ModelCode == mc && x.ColorCode == cc);
        if (entity is null) return false;

        db.CarColors.Remove(entity);
        await db.SaveChangesAsync();
        return true;
    }

    public async Task<CarModelStatsDto> GetStatsAsync()
    {
        var models = await db.CarModels.AsNoTracking().Where(x => x.OrgId == Org).ToListAsync();
        var colors = await db.CarColors.AsNoTracking().Where(x => x.OrgId == Org).ToListAsync();

        return new CarModelStatsDto(
            TotalModels: models.Count,
            TotalActiveModels: models.Count(x => x.FlagActive == "1"),
            TotalBusinessPlanModels: models.Count(x => x.FlagBusinessPlan == "1"),
            TotalColors: colors.Count,
            TotalActiveColors: colors.Count(x => x.FlagActive == "1"),
            ColorsByModel: colors.GroupBy(x => string.IsNullOrWhiteSpace(x.ModelCode) ? "UNKNOWN" : x.ModelCode)
                                 .ToDictionary(g => g.Key, g => g.Count())
        );
    }

    // ===== Helpers =====
    private static string NormalizeFlag(string? flag)
        => string.IsNullOrWhiteSpace(flag) ? "1" : (flag.Trim() == "0" ? "0" : "1");

    /// <summary>Kiểm tra dòng xe tồn tại và đang hoạt động (Mst_CarModel_CheckDB).</summary>
    private async Task CheckModelExistsAsync(string modelCode)
    {
        var model = await db.CarModels.AsNoTracking()
            .Where(m => m.OrgId == Org && m.ModelCode == modelCode && m.FlagActive == "1")
            .Select(m => m.ModelCode)
            .FirstOrDefaultAsync();
        if (model is null)
            throw new InvalidOperationException($"Dòng xe '{modelCode}' không tồn tại hoặc đã ngừng áp dụng (Mst_CarModel).");
    }

    private static CarModelDto ToModelDto(CarModelMaster e) => new(
        e.Id, e.ModelCode, e.ModelProductionCode, e.ModelName,
        e.FlagBusinessPlan, e.FlagBusinessPlan == "1", e.FlagActive, e.FlagActive == "1",
        e.LogLUDateTime, e.LogLUBy);

    private static CarColorDto ToColorDto(CarColorMaster e) => new(
        e.Id, e.ModelCode, null, e.ColorCode, e.ColorExtType, e.ColorExtCode,
        e.ColorExtName, e.ColorExtNameVN, e.ColorIntCode, e.ColorIntName, e.ColorIntNameVN,
        e.ColorFee, e.Remark, e.FlagActive, e.FlagActive == "1", e.LogLUDateTime, e.LogLUBy);
}
