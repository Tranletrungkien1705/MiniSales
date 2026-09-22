using Microsoft.EntityFrameworkCore;
using MiniSales.Data;
using MiniSales.Models;

namespace MiniSales.Services;

public record CarCancelSearchFilterDto(
    string? CarCancelType = null,
    string? CarId = null,
    string? Vin = null,
    string? ModelCode = null,
    string? DealerCode = null,
    string? SoCode = null,
    string? FlagEarlyCancel = null, // ''=tất cả | '1'=xe sắp hủy | '0'=chưa hủy
    string? FlagActive = "0", // '0'=xe đã hủy (mặc định màn QL hủy xe FrmMngCarCancel) | '1'=xe đang hoạt động | 'all'=tất cả
    DateTime? CarCancelDateFrom = null,
    DateTime? CarCancelDateTo = null
);

public record CancelCarInputDto(
    string CarId,
    string CarCancelType, // CCT01..CCT99
    string? CarCancelRemark,
    string? CancelledBy = null
);

public record UpdateCarFlagsDto(
    string? CarCancelRemark,
    string? FlagMapVIN, // '1' / '0'
    string? FlagEarlyCancel, // '1' / '0'
    string? FlagCarDeliveryOrder, // '1' / '0'
    string? FlagTestCar, // '1' / '0'
    string? UpdatedBy = null
);

public record BatchCarIdsDto(
    List<string> CarIds,
    string? ActionBy = null,
    string? Remark = null
);

public record BatchCarCancelDto(
    List<string> CarIds,
    string CarCancelType,
    string? CarCancelRemark = null,
    string? ActionBy = null
);

public record CarRecordDto(
    long Id,
    string CarId,
    string Vin,
    string ModelCode,
    string ModelName,
    string? SpecCode,
    string? SpecDescription,
    string? ColorCode,
    string? ColorName,
    string? EngineNo,
    string DealerCode,
    string? DealerName,
    string? StorageCode,
    string? StorageName,
    string? SoCode,
    decimal PriceActual,
    decimal PaymentDepositAmount,
    decimal GuaranteeAmount,
    decimal DutyCompletedAmount,
    double PaymentDepositPercent,
    double GuaranteePercent,
    double DutyCompletedPercent,
    string FlagActive,
    string FlagAllowChangeVIN,
    string? CarCancelType,
    string? CarCancelTypeName,
    string? CarCancelRemark,
    DateTime? CarCancelDate,
    string? CarCancelBy,
    string FlagEarlyCancel,
    string FlagMapVIN,
    string FlagCarDeliveryOrder,
    string FlagTestCar,
    DateTime CreatedAt,
    DateTime? LogLUDateTime,
    string? LogLUBy
);

public record BatchCarCancelResultDto(
    int TotalRequested,
    int SuccessCount,
    int FailureCount,
    List<string> SucceededCarIds,
    List<string> FailedCarIds,
    List<string> Errors
);

public record CarCancelReasonDto(
    string Code,
    string Name,
    string Description,
    bool FlagActive
);

public record CarCancelLogDto(
    long Id,
    string CarId,
    string Vin,
    string Action,
    string? CancelType,
    string? Remark,
    string? PerformedBy,
    DateTime PerformedAt
);

public record CarCancelStatsDto(
    int TotalCars,
    int ActiveCars,
    int CancelledCars,
    int EarlyCancelWarningCars,
    int DemoTestCars,
    Dictionary<string, int> ByCancelType,
    Dictionary<string, int> ByDealer,
    Dictionary<string, int> ByModel
);

public interface ICarCancelService
{
    Task<List<CarRecordDto>> SearchAsync(CarCancelSearchFilterDto filter);
    Task<CarRecordDto?> GetByIdAsync(string carId);
    Task<CarCancelStatsDto> StatsAsync();
    Task<List<CarCancelReasonDto>> GetReasonsAsync();
    Task<List<CarRecordDto>> GetEligibleToCancelAsync(string? dealerCode, string? modelCode);
    Task<CarRecordDto> CancelAsync(CancelCarInputDto dto);
    Task<CarRecordDto> UpdateFlagsAsync(string carId, UpdateCarFlagsDto dto);
    Task<BatchCarCancelResultDto> RestoreBatchAsync(BatchCarIdsDto dto);
    Task<BatchCarCancelResultDto> CancelBatchAsync(BatchCarCancelDto dto);
    Task<List<CarCancelLogDto>> GetLogsAsync(string? carId, string? vin);
}

public sealed class CarCancelService(AppDbContext db, ITenantContext tenant) : ICarCancelService
{
    private static readonly Dictionary<string, string> DefaultReasons = new(StringComparer.OrdinalIgnoreCase)
    {
        ["CCT01"] = "Lỗi kỹ thuật nhà máy sản xuất HTMV",
        ["CCT02"] = "Khách hàng hủy hợp đồng / từ chối nhận xe",
        ["CCT03"] = "Đại lý quá hạn thanh toán nghĩa vụ cọc / bảo lãnh theo quy chế",
        ["CCT04"] = "Điều phối điều chuyển tái phân bổ đơn hàng lô xe khác",
        ["CCT05"] = "Xe hư hại, trầy xước trong quá trình vận chuyển lưu kho bãi",
        ["CCT99"] = "Lý do hủy khác (ghi chú chi tiết)"
    };

    public async Task<List<CarRecordDto>> SearchAsync(CarCancelSearchFilterDto filter)
    {
        var q = db.Cars.AsNoTracking().Where(c => c.OrgId == tenant.OrgId);

        // Lọc FlagActive: mặc định '0' (xe đã hủy - khớp FrmMngCarCancel L19848)
        if (!string.IsNullOrWhiteSpace(filter.FlagActive) && filter.FlagActive != "all")
        {
            q = q.Where(c => c.FlagActive == filter.FlagActive.Trim());
        }

        // Lọc FlagEarlyCancel: ''=tất cả, '1'=xe sắp hủy, '0'=chưa hủy
        if (!string.IsNullOrWhiteSpace(filter.FlagEarlyCancel))
        {
            q = q.Where(c => c.FlagEarlyCancel == filter.FlagEarlyCancel.Trim());
        }

        // Lọc CarCancelType
        if (!string.IsNullOrWhiteSpace(filter.CarCancelType))
        {
            var cancelType = filter.CarCancelType.Trim().ToUpper();
            q = q.Where(c => c.CarCancelType != null && c.CarCancelType.ToUpper().Contains(cancelType));
        }

        // Lọc CarId
        if (!string.IsNullOrWhiteSpace(filter.CarId))
        {
            var carId = filter.CarId.Trim().ToUpper();
            q = q.Where(c => c.CarId.ToUpper().Contains(carId));
        }

        // Lọc VIN
        if (!string.IsNullOrWhiteSpace(filter.Vin))
        {
            var vin = filter.Vin.Trim().ToUpper();
            q = q.Where(c => c.Vin.ToUpper().Contains(vin));
        }

        // Lọc ModelCode
        if (!string.IsNullOrWhiteSpace(filter.ModelCode))
        {
            var modelCode = filter.ModelCode.Trim().ToUpper();
            q = q.Where(c => c.ModelCode.ToUpper().Contains(modelCode));
        }

        // Lọc DealerCode
        if (!string.IsNullOrWhiteSpace(filter.DealerCode))
        {
            var dealer = filter.DealerCode.Trim().ToUpper();
            q = q.Where(c => c.DealerCode.ToUpper() == dealer);
        }

        // Lọc SoCode
        if (!string.IsNullOrWhiteSpace(filter.SoCode))
        {
            var so = filter.SoCode.Trim().ToUpper();
            q = q.Where(c => c.SoCode != null && c.SoCode.ToUpper().Contains(so));
        }

        // Lọc ngày hủy CarCancelDate From/To
        if (filter.CarCancelDateFrom.HasValue)
        {
            var fromDate = filter.CarCancelDateFrom.Value.Date;
            q = q.Where(c => c.CarCancelDate != null && c.CarCancelDate.Value.Date >= fromDate);
        }

        if (filter.CarCancelDateTo.HasValue)
        {
            var toDate = filter.CarCancelDateTo.Value.Date;
            q = q.Where(c => c.CarCancelDate != null && c.CarCancelDate.Value.Date <= toDate);
        }

        var list = await q.OrderByDescending(c => c.CarCancelDate ?? c.CreatedAt)
                          .ThenBy(c => c.CarId)
                          .ToListAsync();

        return list.Select(ToDto).ToList();
    }

    public async Task<CarRecordDto?> GetByIdAsync(string carId)
    {
        if (string.IsNullOrWhiteSpace(carId)) return null;
        var car = await db.Cars.AsNoTracking()
                               .FirstOrDefaultAsync(c => c.OrgId == tenant.OrgId && (c.CarId == carId || c.Vin == carId));
        return car is null ? null : ToDto(car);
    }

    public async Task<List<CarRecordDto>> GetEligibleToCancelAsync(string? dealerCode, string? modelCode)
    {
        // Xe đủ điều kiện hủy: Đang hoạt động FlagActive == '1', chưa phát sinh tiền cọc, không vướng lệnh xuất xe
        var q = db.Cars.AsNoTracking().Where(c => c.OrgId == tenant.OrgId && c.FlagActive == "1" && c.PaymentDepositAmount == 0);

        if (!string.IsNullOrWhiteSpace(dealerCode))
        {
            var d = dealerCode.Trim().ToUpper();
            q = q.Where(c => c.DealerCode.ToUpper() == d);
        }

        if (!string.IsNullOrWhiteSpace(modelCode))
        {
            var m = modelCode.Trim().ToUpper();
            q = q.Where(c => c.ModelCode.ToUpper().Contains(m));
        }

        var list = await q.OrderBy(c => c.CarId).ToListAsync();

        // Lọc thêm các xe không có Lệnh xuất xe đang hoạt động
        var activeDeliveryVins = await db.DeliveryOrders.AsNoTracking()
            .Where(d => d.OrgId == tenant.OrgId && d.Vin != null && (d.Status == DeliveryOrderStatus.Pending || d.Status == DeliveryOrderStatus.Approved || d.Status == DeliveryOrderStatus.Delivered))
            .Select(d => d.Vin!)
            .ToListAsync();

        var vinSet = new HashSet<string>(activeDeliveryVins, StringComparer.OrdinalIgnoreCase);

        return list.Where(c => !vinSet.Contains(c.Vin)).Select(ToDto).ToList();
    }

    public async Task<CarRecordDto> CancelAsync(CancelCarInputDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.CarId))
            throw new InvalidOperationException("Mã xe (CarId) không được để trống.");

        var carId = dto.CarId.Trim();
        var car = await db.Cars.FirstOrDefaultAsync(c => c.OrgId == tenant.OrgId && (c.CarId == carId || c.Vin == carId));
        if (car is null)
            throw new InvalidOperationException($"Không tìm thấy xe ô tô với mã định danh hoặc số khung '{carId}'.");

        if (car.FlagActive == "0")
            throw new InvalidOperationException($"Xe '{car.CarId}' (VIN: {car.Vin}) đã ở trạng thái ĐÃ HỦY vào ngày {car.CarCancelDate:dd/MM/yyyy} bởi {car.CarCancelBy}.");

        var cancelType = (dto.CarCancelType ?? "").Trim().ToUpper();
        if (string.IsNullOrWhiteSpace(cancelType) || cancelType == "NONE")
            throw new InvalidOperationException("Loại hủy xe (CarCancelType) không hợp lệ. Vui lòng chọn lý do hủy xe trong danh mục.");

        // Ràng buộc 1 (Car_Car_Cancel_PaymentMustBeZero - Car.1.cs:460): Tổng tiền thanh toán đã duyệt/cọc phải bằng 0
        if (car.PaymentDepositAmount > 0)
        {
            throw new InvalidOperationException($"Không thể hủy xe {car.CarId} vì xe đã phát sinh thanh toán cọc/tiền xe ({car.PaymentDepositAmount:N0} VNĐ). Cần hủy hoặc hoàn phiếu thanh toán trước khi hủy xe.");
        }

        // Ràng buộc 2 (Car_Car_Cancel_CarDeliveryOrderExisting - Car.1.cs:495): Xe không được tồn tại trong Lệnh xuất xe đang hoạt động
        var deliveryOrder = await db.DeliveryOrders.AsNoTracking()
            .FirstOrDefaultAsync(d => d.OrgId == tenant.OrgId && d.Vin == car.Vin && (d.Status == DeliveryOrderStatus.Pending || d.Status == DeliveryOrderStatus.Approved || d.Status == DeliveryOrderStatus.Delivered));

        if (deliveryOrder is not null)
        {
            throw new InvalidOperationException($"Không thể hủy xe {car.CarId} vì xe đang nằm trong Lệnh giao xe {deliveryOrder.DeliveryOrderNo} (trạng thái: {deliveryOrder.Status}). Cần hủy lệnh giao xe trước.");
        }

        var user = dto.CancelledBy?.Trim() ?? "NPP_STAFF";
        var remark = dto.CarCancelRemark?.Trim() ?? "";

        // Cập nhật trạng thái hủy
        car.FlagActive = "0";
        car.CarCancelType = cancelType;
        car.CarCancelRemark = remark;
        car.CarCancelDate = DateTime.Today;
        car.CarCancelBy = user;
        car.FlagEarlyCancel = "0";
        car.FlagMapVIN = "0"; // Khóa/giải phóng ghép VIN
        car.FlagCarDeliveryOrder = "0"; // Khóa lệnh xuất xe
        car.LogLUDateTime = DateTime.Now;
        car.LogLUBy = user;

        db.CarCancelLogs.Add(new CarCancelLog
        {
            OrgId = tenant.OrgId,
            CarId = car.CarId,
            Vin = car.Vin,
            Action = "Cancel",
            CancelType = cancelType,
            Remark = string.IsNullOrWhiteSpace(remark) ? $"Hủy xe loại {cancelType}" : remark,
            PerformedBy = user,
            PerformedAt = DateTime.Now
        });

        await db.SaveChangesAsync();
        return ToDto(car);
    }

    public async Task<CarRecordDto> UpdateFlagsAsync(string carId, UpdateCarFlagsDto dto)
    {
        if (string.IsNullOrWhiteSpace(carId))
            throw new InvalidOperationException("Mã xe (CarId) không được để trống.");

        var id = carId.Trim();
        var car = await db.Cars.FirstOrDefaultAsync(c => c.OrgId == tenant.OrgId && (c.CarId == id || c.Vin == id));
        if (car is null)
            throw new InvalidOperationException($"Không tìm thấy xe ô tô với mã định danh hoặc số khung '{id}'.");

        var user = dto.UpdatedBy?.Trim() ?? "NPP_STAFF";

        if (dto.CarCancelRemark is not null)
            car.CarCancelRemark = dto.CarCancelRemark.Trim();

        if (!string.IsNullOrWhiteSpace(dto.FlagMapVIN))
            car.FlagMapVIN = dto.FlagMapVIN.Trim() == "1" ? "1" : "0";

        if (!string.IsNullOrWhiteSpace(dto.FlagEarlyCancel))
            car.FlagEarlyCancel = dto.FlagEarlyCancel.Trim() == "1" ? "1" : "0";

        if (!string.IsNullOrWhiteSpace(dto.FlagCarDeliveryOrder))
            car.FlagCarDeliveryOrder = dto.FlagCarDeliveryOrder.Trim() == "1" ? "1" : "0";

        if (!string.IsNullOrWhiteSpace(dto.FlagTestCar))
            car.FlagTestCar = dto.FlagTestCar.Trim() == "1" ? "1" : "0";

        car.LogLUDateTime = DateTime.Now;
        car.LogLUBy = user;

        db.CarCancelLogs.Add(new CarCancelLog
        {
            OrgId = tenant.OrgId,
            CarId = car.CarId,
            Vin = car.Vin,
            Action = "UpdateFlags",
            CancelType = car.CarCancelType,
            Remark = $"Cập nhật cờ điều hành: MapVIN={car.FlagMapVIN}, EarlyCancel={car.FlagEarlyCancel}, CarDO={car.FlagCarDeliveryOrder}, TestCar={car.FlagTestCar}. Ghi chú: {car.CarCancelRemark}",
            PerformedBy = user,
            PerformedAt = DateTime.Now
        });

        await db.SaveChangesAsync();
        return ToDto(car);
    }

    public async Task<BatchCarCancelResultDto> RestoreBatchAsync(BatchCarIdsDto dto)
    {
        if (dto.CarIds is null || dto.CarIds.Count == 0)
            throw new InvalidOperationException("Danh sách mã xe cần phục hồi không được rỗng.");

        var user = dto.ActionBy?.Trim() ?? "NPP_STAFF";
        var succeeded = new List<string>();
        var failed = new List<string>();
        var errors = new List<string>();

        foreach (var rawId in dto.CarIds)
        {
            if (string.IsNullOrWhiteSpace(rawId)) continue;
            var id = rawId.Trim();
            var car = await db.Cars.FirstOrDefaultAsync(c => c.OrgId == tenant.OrgId && (c.CarId == id || c.Vin == id));
            if (car is null)
            {
                failed.Add(id);
                errors.Add($"Xe '{id}': Không tìm thấy trong hệ thống.");
                continue;
            }

            if (car.FlagActive == "1")
            {
                failed.Add(id);
                errors.Add($"Xe '{id}': Đang ở trạng thái hoạt động bình thường, không cần phục hồi.");
                continue;
            }

            // Kích hoạt lại xe đã hủy (Car_Car_UpdSpecial_PhucHoi_HQ - Car.1.cs:600)
            car.FlagActive = "1";
            car.CarCancelType = "NONE";
            car.CarCancelRemark = "";
            car.CarCancelDate = null;
            car.CarCancelBy = null;
            car.FlagMapVIN = "1";
            car.FlagCarDeliveryOrder = "1";
            car.LogLUDateTime = DateTime.Now;
            car.LogLUBy = user;

            db.CarCancelLogs.Add(new CarCancelLog
            {
                OrgId = tenant.OrgId,
                CarId = car.CarId,
                Vin = car.Vin,
                Action = "Restore",
                CancelType = "NONE",
                Remark = dto.Remark ?? "Phục hồi xe đã hủy về trạng thái hoạt động bình thường",
                PerformedBy = user,
                PerformedAt = DateTime.Now
            });

            succeeded.Add(car.CarId);
        }

        await db.SaveChangesAsync();
        return new BatchCarCancelResultDto(
            dto.CarIds.Count,
            succeeded.Count,
            failed.Count,
            succeeded,
            failed,
            errors
        );
    }

    public async Task<BatchCarCancelResultDto> CancelBatchAsync(BatchCarCancelDto dto)
    {
        if (dto.CarIds is null || dto.CarIds.Count == 0)
            throw new InvalidOperationException("Danh sách mã xe cần hủy không được rỗng.");

        var cancelType = (dto.CarCancelType ?? "").Trim().ToUpper();
        if (string.IsNullOrWhiteSpace(cancelType) || cancelType == "NONE")
            throw new InvalidOperationException("Loại hủy xe (CarCancelType) không hợp lệ.");

        var user = dto.ActionBy?.Trim() ?? "NPP_STAFF";
        var remark = dto.CarCancelRemark?.Trim() ?? "";
        var succeeded = new List<string>();
        var failed = new List<string>();
        var errors = new List<string>();

        foreach (var rawId in dto.CarIds)
        {
            if (string.IsNullOrWhiteSpace(rawId)) continue;
            var id = rawId.Trim();
            var car = await db.Cars.FirstOrDefaultAsync(c => c.OrgId == tenant.OrgId && (c.CarId == id || c.Vin == id));
            if (car is null)
            {
                failed.Add(id);
                errors.Add($"Xe '{id}': Không tìm thấy trong hệ thống.");
                continue;
            }

            if (car.FlagActive == "0")
            {
                failed.Add(id);
                errors.Add($"Xe '{id}': Đã ở trạng thái hủy trước đó.");
                continue;
            }

            if (car.PaymentDepositAmount > 0)
            {
                failed.Add(id);
                errors.Add($"Xe '{id}': Đã có thanh toán cọc {car.PaymentDepositAmount:N0} VNĐ, không thể hủy.");
                continue;
            }

            var deliveryOrder = await db.DeliveryOrders.AsNoTracking()
                .FirstOrDefaultAsync(d => d.OrgId == tenant.OrgId && d.Vin == car.Vin && (d.Status == DeliveryOrderStatus.Pending || d.Status == DeliveryOrderStatus.Approved || d.Status == DeliveryOrderStatus.Delivered));

            if (deliveryOrder is not null)
            {
                failed.Add(id);
                errors.Add($"Xe '{id}': Vướng Lệnh giao xe {deliveryOrder.DeliveryOrderNo}, không thể hủy.");
                continue;
            }

            car.FlagActive = "0";
            car.CarCancelType = cancelType;
            car.CarCancelRemark = remark;
            car.CarCancelDate = DateTime.Today;
            car.CarCancelBy = user;
            car.FlagEarlyCancel = "0";
            car.FlagMapVIN = "0";
            car.FlagCarDeliveryOrder = "0";
            car.LogLUDateTime = DateTime.Now;
            car.LogLUBy = user;

            db.CarCancelLogs.Add(new CarCancelLog
            {
                OrgId = tenant.OrgId,
                CarId = car.CarId,
                Vin = car.Vin,
                Action = "Cancel",
                CancelType = cancelType,
                Remark = string.IsNullOrWhiteSpace(remark) ? $"Hủy hàng loạt loại {cancelType}" : remark,
                PerformedBy = user,
                PerformedAt = DateTime.Now
            });

            succeeded.Add(car.CarId);
        }

        await db.SaveChangesAsync();
        return new BatchCarCancelResultDto(
            dto.CarIds.Count,
            succeeded.Count,
            failed.Count,
            succeeded,
            failed,
            errors
        );
    }

    public async Task<List<CarCancelReasonDto>> GetReasonsAsync()
    {
        var masters = await db.CarCancelReasons.AsNoTracking()
            .Where(r => r.FlagActive)
            .OrderBy(r => r.Code)
            .ToListAsync();

        if (masters.Count > 0)
        {
            return masters.Select(m => new CarCancelReasonDto(m.Code, m.Name, m.Description, m.FlagActive)).ToList();
        }

        return DefaultReasons.Select(kv => new CarCancelReasonDto(kv.Key, kv.Value, kv.Value, true)).ToList();
    }

    public async Task<List<CarCancelLogDto>> GetLogsAsync(string? carId, string? vin)
    {
        var q = db.CarCancelLogs.AsNoTracking().Where(l => l.OrgId == tenant.OrgId);

        if (!string.IsNullOrWhiteSpace(carId))
        {
            var id = carId.Trim().ToUpper();
            q = q.Where(l => l.CarId.ToUpper() == id);
        }

        if (!string.IsNullOrWhiteSpace(vin))
        {
            var v = vin.Trim().ToUpper();
            q = q.Where(l => l.Vin.ToUpper() == v);
        }

        var list = await q.OrderByDescending(l => l.PerformedAt).Take(100).ToListAsync();
        return list.Select(l => new CarCancelLogDto(l.Id, l.CarId, l.Vin, l.Action, l.CancelType, l.Remark, l.PerformedBy, l.PerformedAt)).ToList();
    }

    public async Task<CarCancelStatsDto> StatsAsync()
    {
        var cars = await db.Cars.AsNoTracking().Where(c => c.OrgId == tenant.OrgId).ToListAsync();

        var total = cars.Count;
        var active = cars.Count(c => c.FlagActive == "1");
        var cancelled = cars.Count(c => c.FlagActive == "0");
        var earlyCancel = cars.Count(c => c.FlagEarlyCancel == "1");
        var testCars = cars.Count(c => c.FlagTestCar == "1");

        var byType = cars.Where(c => c.FlagActive == "0" && !string.IsNullOrWhiteSpace(c.CarCancelType))
                         .GroupBy(c => c.CarCancelType!)
                         .ToDictionary(g => g.Key, g => g.Count());

        var byDealer = cars.Where(c => c.FlagActive == "0")
                           .GroupBy(c => c.DealerCode)
                           .ToDictionary(g => g.Key, g => g.Count());

        var byModel = cars.Where(c => c.FlagActive == "0")
                          .GroupBy(c => c.ModelCode)
                          .ToDictionary(g => g.Key, g => g.Count());

        return new CarCancelStatsDto(
            total,
            active,
            cancelled,
            earlyCancel,
            testCars,
            byType,
            byDealer,
            byModel
        );
    }

    private static CarRecordDto ToDto(CarRecord c)
    {
        double pmtDepositPercent = 0;
        double grtPercent = 0;
        double dutyPercent = 0;

        if (c.PriceActual > 0)
        {
            pmtDepositPercent = Math.Round((double)(c.PaymentDepositAmount * 100 / c.PriceActual), 2);
            grtPercent = Math.Round((double)(c.GuaranteeAmount * 100 / c.PriceActual), 2);
            dutyPercent = Math.Round((double)(c.DutyCompletedAmount * 100 / c.PriceActual), 2);
        }

        string? typeName = null;
        if (!string.IsNullOrWhiteSpace(c.CarCancelType) && DefaultReasons.TryGetValue(c.CarCancelType, out var name))
        {
            typeName = name;
        }

        return new CarRecordDto(
            c.Id,
            c.CarId,
            c.Vin,
            c.ModelCode,
            c.ModelName,
            c.SpecCode,
            c.SpecDescription,
            c.ColorCode,
            c.ColorName,
            c.EngineNo,
            c.DealerCode,
            c.DealerName,
            c.StorageCode,
            c.StorageName,
            c.SoCode,
            c.PriceActual,
            c.PaymentDepositAmount,
            c.GuaranteeAmount,
            c.DutyCompletedAmount,
            pmtDepositPercent,
            grtPercent,
            dutyPercent,
            c.FlagActive,
            c.FlagAllowChangeVIN,
            c.CarCancelType,
            typeName,
            c.CarCancelRemark,
            c.CarCancelDate,
            c.CarCancelBy,
            c.FlagEarlyCancel,
            c.FlagMapVIN,
            c.FlagCarDeliveryOrder,
            c.FlagTestCar,
            c.CreatedAt,
            c.LogLUDateTime,
            c.LogLUBy
        );
    }
}
