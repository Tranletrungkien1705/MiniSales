using Microsoft.EntityFrameworkCore;
using MiniSales.Data;
using MiniSales.Models;

namespace MiniSales.Services;

// ===== DTOs =====
public record UpdateCarPriceDto(
    string CarId,
    decimal? UnitPriceActualNew = null,
    string? MapVINRankingNew = null,
    bool LockChangeVIN = false,
    bool IsUserFromSales = false,
    string? Remark = null,
    string? ActionBy = null
);

public record CarPriceUpdateLogDto(
    long Id,
    string CarId,
    string? Vin,
    string ModelCode,
    string? DealerCode,
    string Action,
    decimal? OldUnitPriceActual,
    decimal? NewUnitPriceActual,
    string? OldPaymentStatus,
    string? NewPaymentStatus,
    string? Remark,
    string? PerformedBy,
    DateTime PerformedAt
);

public record CarPriceUpdateStatsDto(
    int TotalCars,
    int TotalPendingPayment,
    int TotalFinishedPayment,
    int TotalMappedVin,
    int TotalUnmappedVin,
    int TotalUpdateLogs,
    decimal TotalPriceActual,
    Dictionary<string, int> ByModel,
    Dictionary<string, int> ByDealer,
    Dictionary<string, int> LogsByAction
);

public interface ICarPriceUpdateService
{
    Task<object> UpdatePriceAsync(UpdateCarPriceDto dto);
    Task<List<CarPriceUpdateLogDto>> GetLogsAsync(string? carId, string? vin);
    Task<CarPriceUpdateStatsDto> GetStatsAsync();
}

/// <summary>
/// Quản lý Cập nhật giá xe ô tô (DMS.Sales CarUpdPriceController / Car_Car_Update01_HQ / MapVIN.cs):
/// NPP cập nhật giá bán buôn thực tế (UnitPriceActual) của xe, thứ tự ưu tiên Map VIN (MapVINRanking)
/// và khóa đổi số khung VIN (FlagAllowChangeVIN). Ràng buộc: chỉ cập nhật giá khi xe chưa thanh toán xong
/// (PaymentStatus = 'P'), tự động tính lại trạng thái thanh toán sau khi đổi giá (đủ tiền -> 'F'),
/// chỉ sửa MapVINRanking / khóa đổi VIN khi xe chưa gán số khung VIN, và chặn người dùng phòng Sales
/// khi xe đã vướng bảo lãnh, thanh toán, hợp đồng đại lý hoặc lệnh xuất xe. Lưu vết nhật ký cập nhật giá.
/// </summary>
public sealed class CarPriceUpdateService(AppDbContext db, ITenantContext tenant) : ICarPriceUpdateService
{
    private Guid Org => tenant.OrgId;

    public async Task<object> UpdatePriceAsync(UpdateCarPriceDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.CarId))
            throw new InvalidOperationException("Mã xe (CarId) không được để trống.");

        var user = string.IsNullOrWhiteSpace(dto.ActionBy) ? "CHUYÊN VIÊN ĐIỀU PHỐI NPP" : dto.ActionBy.Trim();
        var carId = dto.CarId.Trim();

        // 1. Kiểm tra xe tồn tại và đang hoạt động (myCar_CheckCar)
        var car = await db.Cars.FirstOrDefaultAsync(c => c.OrgId == Org && c.CarId == carId)
            ?? throw new InvalidOperationException($"Không tìm thấy xe có mã '{carId}'.");

        if (car.FlagActive != "1")
            throw new InvalidOperationException($"Xe '{carId}' đã bị hủy (FlagActive = 0), không thể cập nhật giá.");

        var oldPrice = car.PriceActual;
        var oldPaymentStatus = car.PaymentStatus;
        var action = CarPriceUpdateAction.UpdatePrice;
        bool changed = false;

        // 2. Cập nhật giá bán buôn thực tế (UnitPriceActualNew)
        if (dto.UnitPriceActualNew.HasValue)
        {
            if (dto.UnitPriceActualNew.Value <= 0)
                throw new InvalidOperationException("Giá bán buôn thực tế mới (UnitPriceActualNew) phải là số dương lớn hơn 0.");

            // Chỉ cho phép cập nhật giá với xe chưa thanh toán xong (Car_Car_Update01_PaymentStatusNotMatched)
            if (car.PaymentStatus != "P")
                throw new InvalidOperationException($"Xe '{carId}' đã thanh toán xong (PaymentStatus = '{car.PaymentStatus}'), không thể cập nhật giá bán buôn thực tế.");

            // Ràng buộc người dùng phòng Sales (Car_Car_Update01X - strIsUserFromSales)
            if (dto.IsUserFromSales)
                await CheckSalesUserConstraintsAsync(car);

            car.PriceActual = dto.UnitPriceActualNew.Value;
            action = CarPriceUpdateAction.UpdatePrice;
            changed = true;
        }

        // 3. Cập nhật thứ tự ưu tiên Map VIN (MapVINRankingNew) - chỉ khi xe chưa gán VIN
        if (!string.IsNullOrWhiteSpace(dto.MapVINRankingNew))
        {
            if (!string.IsNullOrWhiteSpace(car.Vin))
                throw new InvalidOperationException($"Xe '{carId}' đã được gán số khung VIN ({car.Vin}), không thể cập nhật thứ tự ưu tiên Map VIN.");

            car.MapVINRanking = dto.MapVINRankingNew.Trim();
            if (!changed) action = CarPriceUpdateAction.UpdateMapVINRanking;
            changed = true;
        }

        // 4. Khóa đổi số khung VIN (FlagAllowChangeVINNew) - chỉ cho phép khóa (Inactive) và chỉ khi chưa gán VIN
        if (dto.LockChangeVIN)
        {
            if (!string.IsNullOrWhiteSpace(car.Vin))
                throw new InvalidOperationException($"Xe '{carId}' đã được gán số khung VIN ({car.Vin}), không thể khóa đổi VIN.");

            car.FlagAllowChangeVIN = "0";
            if (!changed) action = CarPriceUpdateAction.LockChangeVIN;
            changed = true;
        }

        if (!changed)
            throw new InvalidOperationException("Không có thông tin nào để cập nhật (cần UnitPriceActualNew, MapVINRankingNew hoặc LockChangeVIN).");

        // 5. Tính lại trạng thái thanh toán của xe sau khi đổi giá (Update PaymentStatus of Car_Car)
        if (dto.UnitPriceActualNew.HasValue)
        {
            var paidTotal = await db.DealerPaymentDetails
                .Where(d => d.CarId == carId
                    && db.DealerPayments.Any(p => p.Id == d.PaymentId && p.OrgId == Org && p.Status == WholesalePaymentStatus.Finished))
                .SumAsync(d => (decimal?)d.Amount) ?? 0m;

            car.PaymentStatus = Math.Abs(car.PriceActual - paidTotal) < 0.001m ? "F" : "P";
        }

        car.LogLUDateTime = DateTime.Now;
        car.LogLUBy = user;

        var log = new CarPriceUpdateLog
        {
            OrgId = Org,
            CarId = car.CarId,
            Vin = string.IsNullOrWhiteSpace(car.Vin) ? null : car.Vin,
            ModelCode = car.ModelCode,
            DealerCode = car.DealerCode,
            Action = action,
            OldUnitPriceActual = dto.UnitPriceActualNew.HasValue ? oldPrice : null,
            NewUnitPriceActual = dto.UnitPriceActualNew.HasValue ? car.PriceActual : null,
            OldPaymentStatus = oldPaymentStatus,
            NewPaymentStatus = car.PaymentStatus,
            Remark = dto.Remark ?? $"Cập nhật {action} cho xe {car.CarId}",
            PerformedBy = user,
            PerformedAt = DateTime.Now
        };
        db.CarPriceUpdateLogs.Add(log);

        await db.SaveChangesAsync();

        return new
        {
            success = true,
            action = action.ToString(),
            carId = car.CarId,
            vin = car.Vin,
            modelCode = car.ModelCode,
            dealerCode = car.DealerCode,
            oldUnitPriceActual = log.OldUnitPriceActual,
            unitPriceActual = car.PriceActual,
            paymentStatus = car.PaymentStatus,
            mapVINRanking = car.MapVINRanking,
            flagAllowChangeVIN = car.FlagAllowChangeVIN,
            performedBy = user,
            performedAt = log.PerformedAt
        };
    }

    public async Task<List<CarPriceUpdateLogDto>> GetLogsAsync(string? carId, string? vin)
    {
        var q = db.CarPriceUpdateLogs.AsNoTracking().Where(l => l.OrgId == Org);

        if (!string.IsNullOrWhiteSpace(carId))
        {
            var cid = carId.Trim();
            q = q.Where(l => l.CarId == cid);
        }
        if (!string.IsNullOrWhiteSpace(vin))
        {
            var v = vin.Trim();
            q = q.Where(l => l.Vin == v);
        }

        var list = await q.OrderByDescending(l => l.PerformedAt).Take(200).ToListAsync();
        return list.Select(l => new CarPriceUpdateLogDto(
            l.Id, l.CarId, l.Vin, l.ModelCode, l.DealerCode, l.Action.ToString(),
            l.OldUnitPriceActual, l.NewUnitPriceActual, l.OldPaymentStatus, l.NewPaymentStatus,
            l.Remark, l.PerformedBy, l.PerformedAt)).ToList();
    }

    public async Task<CarPriceUpdateStatsDto> GetStatsAsync()
    {
        var cars = await db.Cars.AsNoTracking().Where(c => c.OrgId == Org && c.FlagActive == "1").ToListAsync();
        var logs = await db.CarPriceUpdateLogs.AsNoTracking().Where(l => l.OrgId == Org).ToListAsync();

        return new CarPriceUpdateStatsDto(
            TotalCars: cars.Count,
            TotalPendingPayment: cars.Count(c => c.PaymentStatus == "P"),
            TotalFinishedPayment: cars.Count(c => c.PaymentStatus == "F"),
            TotalMappedVin: cars.Count(c => !string.IsNullOrWhiteSpace(c.Vin)),
            TotalUnmappedVin: cars.Count(c => string.IsNullOrWhiteSpace(c.Vin)),
            TotalUpdateLogs: logs.Count,
            TotalPriceActual: cars.Sum(c => c.PriceActual),
            ByModel: cars.GroupBy(c => string.IsNullOrWhiteSpace(c.ModelCode) ? "UNKNOWN" : c.ModelCode)
                         .ToDictionary(g => g.Key, g => g.Count()),
            ByDealer: cars.GroupBy(c => string.IsNullOrWhiteSpace(c.DealerCode) ? "UNKNOWN" : c.DealerCode)
                          .ToDictionary(g => g.Key, g => g.Count()),
            LogsByAction: logs.GroupBy(l => l.Action.ToString()).ToDictionary(g => g.Key, g => g.Count())
        );
    }

    // ===== Helpers =====
    /// <summary>Ràng buộc người dùng phòng Sales (Car_Car_Update01X): xe không được vướng bảo lãnh, thanh toán, hợp đồng đại lý hoặc lệnh xuất xe.</summary>
    private async Task CheckSalesUserConstraintsAsync(CarRecord car)
    {
        // Đã tạo bảo lãnh (mọi trạng thái)
        var hasGuarantee = await db.PaymentGuaranteeDetails
            .AnyAsync(d => d.Vin == car.Vin && db.PaymentGuarantees.Any(g => g.Id == d.GuaranteeId && g.OrgId == Org));
        if (hasGuarantee)
            throw new InvalidOperationException($"Xe '{car.CarId}' đã có chứng thư bảo lãnh ngân hàng, phòng Sales không được cập nhật giá.");

        // Đã tạo thanh toán với tổng tiền > 0 (mọi trạng thái trừ P/R/C)
        var hasPayment = await db.DealerPaymentDetails
            .AnyAsync(d => d.CarId == car.CarId && d.Amount > 0
                && db.DealerPayments.Any(p => p.Id == d.PaymentId && p.OrgId == Org
                    && p.Status != WholesalePaymentStatus.Pending
                    && p.Status != WholesalePaymentStatus.Rejected
                    && p.Status != WholesalePaymentStatus.Cancelled));
        if (hasPayment)
            throw new InvalidOperationException($"Xe '{car.CarId}' đã có phiếu thanh toán, phòng Sales không được cập nhật giá.");

        // Đã tạo hợp đồng đại lý (mọi trạng thái)
        var hasContract = await db.DealerContractDetails
            .AnyAsync(d => d.CarId == car.CarId && db.DealerContracts.Any(c => c.Id == d.ContractId && c.OrgId == Org));
        if (hasContract)
            throw new InvalidOperationException($"Xe '{car.CarId}' đã thuộc hợp đồng mua bán buôn đại lý, phòng Sales không được cập nhật giá.");

        // Đã tạo lệnh xuất xe (mọi trạng thái)
        var hasDeliveryOrder = await db.DeliveryOrders
            .AnyAsync(o => o.OrgId == Org && o.Vin == car.Vin);
        if (hasDeliveryOrder)
            throw new InvalidOperationException($"Xe '{car.CarId}' đã có lệnh xuất xe, phòng Sales không được cập nhật giá.");
    }
}
