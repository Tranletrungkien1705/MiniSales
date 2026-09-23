using Microsoft.EntityFrameworkCore;
using MiniSales.Data;
using MiniSales.Models;

namespace MiniSales.Services;

public record ManualMapVinDto(
    string CarId,
    string Vin,
    string? StorageCode = null,
    string? Remark = null,
    string? ActionBy = null
);

public record AutoMapVinRunDto(
    MapVinMethod Method = MapVinMethod.Normal,
    string? DealerCode = null,
    string? ModelCode = null,
    int? MaxCount = null,
    string? Remark = null,
    string? ActionBy = null
);

public record UnmapVinDto(
    string CarId,
    string Reason,
    string? ActionBy = null
);

public record BatchUnmapVinDto(
    List<string> CarIds,
    string Reason,
    string? ActionBy = null
);

public record BatchUnmapResultDto(
    int TotalRequested,
    int SuccessCount,
    int FailureCount,
    List<string> SucceededCarIds,
    List<string> FailedCarIds,
    List<string> Errors
);

public record CarVinInventoryDto(
    long Id,
    string Vin,
    string ModelCode,
    string ModelName,
    string? SpecCode,
    string? SpecDescription,
    string? ColorCode,
    string? ColorName,
    string? EngineNo,
    string StorageCode,
    string? StorageName,
    string AssemblyStatus,
    string? ProductionMonth,
    string Status,
    string? MappedCarId,
    DateTime CreatedAt
);

public record PendingMapCarDto(
    string CarId,
    string? SoCode,
    string DealerCode,
    string? DealerName,
    string ModelCode,
    string ModelName,
    string? SpecCode,
    string? SpecDescription,
    string? ColorCode,
    string? ColorName,
    string? EngineNo,
    string? StorageCode,
    string? StorageName,
    decimal PriceActual,
    decimal PaymentDepositAmount,
    decimal GuaranteeAmount,
    decimal DutyCompletedAmount,
    double DepositPercent,
    string FlagActive,
    string FlagAllowChangeVIN,
    string FlagEarlyCancel,
    string FlagMapVIN,
    DateTime CreatedAt
);

public record MapVinDetailDto(
    long Id,
    string CarId,
    string? SoCode,
    string DealerCode,
    string? DealerName,
    string ModelCode,
    string ModelName,
    string? SpecCode,
    string? ColorCode,
    string? ColorName,
    string? Vin,
    string? EngineNo,
    string? StorageCode,
    string? StorageName,
    string Status,
    string? RejectReason,
    DateTime? MappedAt,
    string? MappedBy
);

public record MapVinSessionDto(
    long Id,
    string SessionNo,
    string MapType,
    string Method,
    string Status,
    string? DealerCode,
    string? ModelCode,
    int TotalRequested,
    int TotalMapped,
    int TotalUnmapped,
    string? Remark,
    string? CreatedBy,
    DateTime CreatedAt,
    string? ApprovedBy,
    DateTime? ApprovedAt,
    List<MapVinDetailDto> Details
);

public record MapVinAuditLogDto(
    long Id,
    string CarId,
    string? Vin,
    string Action,
    string? SessionNo,
    string? Remark,
    string? PerformedBy,
    DateTime PerformedAt
);

public record MapVinStatsDto(
    int TotalActiveCars,
    int TotalMappedCars,
    int TotalPendingCars,
    double VinCoverageRate,
    int AvailableVinStock,
    int TotalSessions,
    Dictionary<string, int> MappedByModel,
    Dictionary<string, int> MappedByDealer,
    Dictionary<string, int> StockByModel
);

public interface IMapVinService
{
    Task<List<MapVinSessionDto>> GetSessionsAsync(string? status, string? method, string? dealerCode);
    Task<MapVinSessionDto?> GetSessionByNoAsync(string sessionNo);
    Task<MapVinSessionDto> RunAutoMapAsync(AutoMapVinRunDto dto);
    Task<object> ManualMapAsync(ManualMapVinDto dto);
    Task<object> UnmapAsync(UnmapVinDto dto);
    Task<BatchUnmapResultDto> BatchUnmapAsync(BatchUnmapVinDto dto);
    Task<List<CarVinInventoryDto>> GetAvailableVinsAsync(string? modelCode, string? colorCode, string? storageCode);
    Task<List<PendingMapCarDto>> GetPendingCarsAsync(string? dealerCode, string? modelCode);
    Task<MapVinStatsDto> GetStatsAsync();
    Task<List<MapVinAuditLogDto>> GetLogsAsync(string? carId, string? vin);
}

public sealed class MapVinService(AppDbContext db, ITenantContext tenant) : IMapVinService
{
    private Guid Org => tenant.OrgId;

    public async Task<List<MapVinSessionDto>> GetSessionsAsync(string? status, string? method, string? dealerCode)
    {
        var q = db.MapVinSessions
            .Include(s => s.Details)
            .AsNoTracking()
            .Where(s => s.OrgId == Org);

        if (!string.IsNullOrWhiteSpace(status))
        {
            if (Enum.TryParse<MapVinSessionStatus>(status, true, out var st))
                q = q.Where(s => s.Status == st);
        }

        if (!string.IsNullOrWhiteSpace(method))
        {
            if (Enum.TryParse<MapVinMethod>(method, true, out var m))
                q = q.Where(s => s.Method == m);
        }

        if (!string.IsNullOrWhiteSpace(dealerCode))
        {
            var d = dealerCode.Trim();
            q = q.Where(s => s.DealerCode == d);
        }

        var list = await q.OrderByDescending(s => s.CreatedAt).ToListAsync();
        return list.Select(ToSessionDto).ToList();
    }

    public async Task<MapVinSessionDto?> GetSessionByNoAsync(string sessionNo)
    {
        if (string.IsNullOrWhiteSpace(sessionNo)) return null;
        var s = await db.MapVinSessions
            .Include(x => x.Details)
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.OrgId == Org && x.SessionNo == sessionNo.Trim());
        return s is null ? null : ToSessionDto(s);
    }

    public async Task<MapVinSessionDto> RunAutoMapAsync(AutoMapVinRunDto dto)
    {
        var user = string.IsNullOrWhiteSpace(dto.ActionBy) ? "HỆ THỐNG PHÂN BỔ MAP VIN" : dto.ActionBy.Trim();

        // 1. Lọc danh sách xe CarRecord đang chờ gán VIN
        var q = db.Cars.Where(c => c.OrgId == Org
            && c.FlagActive == "1"
            && c.FlagMapVIN == "1"
            && c.FlagEarlyCancel == "0"
            && (c.Vin == null || c.Vin == ""));

        if (!string.IsNullOrWhiteSpace(dto.DealerCode))
        {
            var dl = dto.DealerCode.Trim();
            q = q.Where(c => c.DealerCode == dl);
        }

        if (!string.IsNullOrWhiteSpace(dto.ModelCode))
        {
            var mc = dto.ModelCode.Trim();
            q = q.Where(c => c.ModelCode == mc);
        }

        // Sắp xếp theo phương thức phân bổ
        var ordered = dto.Method switch
        {
            MapVinMethod.PriorityDeposit => q.OrderByDescending(c => c.PriceActual > 0 ? (double)(c.PaymentDepositAmount / c.PriceActual) : 0)
                                             .ThenByDescending(c => c.DutyCompletedAmount),
            MapVinMethod.NewDealer => q.OrderBy(c => c.DealerCode).ThenBy(c => c.CreatedAt),
            MapVinMethod.NewSpec => q.OrderByDescending(c => c.SpecCode).ThenBy(c => c.CreatedAt),
            MapVinMethod.Special => q.OrderByDescending(c => c.DutyCompletedAmount).ThenBy(c => c.CreatedAt),
            _ => q.OrderBy(c => c.CreatedAt) // Normal FIFO
        };

        if (dto.MaxCount.HasValue && dto.MaxCount.Value > 0)
        {
            ordered = (IOrderedQueryable<CarRecord>)ordered.Take(dto.MaxCount.Value);
        }

        var eligibleCars = await ordered.ToListAsync();

        // 2. Tạo mã phiên ATMVNo (Auto Map VIN No)
        var prefix = $"{DateTime.Today:yyMM}MV";
        var countToday = await db.MapVinSessions.CountAsync(s => s.OrgId == Org && s.SessionNo.StartsWith(prefix));
        var sessionNo = $"{prefix}{(countToday + 1):D4}";

        var session = new MapVinSession
        {
            OrgId = Org,
            SessionNo = sessionNo,
            MapType = MapVinType.Auto,
            Method = dto.Method,
            Status = MapVinSessionStatus.Approved,
            DealerCode = dto.DealerCode?.Trim(),
            ModelCode = dto.ModelCode?.Trim(),
            TotalRequested = eligibleCars.Count,
            TotalMapped = 0,
            TotalUnmapped = 0,
            Remark = dto.Remark ?? $"Chạy tự động phân bổ Map VIN phương thức {dto.Method}",
            CreatedBy = user,
            CreatedAt = DateTime.Now,
            ApprovedBy = user,
            ApprovedAt = DateTime.Now
        };

        // 3. Lấy kho xe vật lý có số VIN khả dụng
        var availableVins = await db.CarVinInventories
            .Where(v => v.OrgId == Org && v.Status == CarVinStatus.Available)
            .OrderBy(v => v.CreatedAt)
            .ToListAsync();

        int mapped = 0;
        int unmapped = 0;
        var logs = new List<MapVinAuditLog>();

        foreach (var car in eligibleCars)
        {
            // Tìm số khung cùng model (ưu tiên khớp màu nếu có)
            var matchedVin = availableVins
                .FirstOrDefault(v => v.ModelCode.Equals(car.ModelCode, StringComparison.OrdinalIgnoreCase)
                                  && (string.IsNullOrEmpty(car.ColorCode) || v.ColorCode == car.ColorCode))
                ?? availableVins.FirstOrDefault(v => v.ModelCode.Equals(car.ModelCode, StringComparison.OrdinalIgnoreCase));

            if (matchedVin != null)
            {
                // Gán VIN vào CarRecord
                car.Vin = matchedVin.Vin;
                if (!string.IsNullOrWhiteSpace(matchedVin.EngineNo))
                    car.EngineNo = matchedVin.EngineNo;
                car.StorageCode = matchedVin.StorageCode;
                car.StorageName = matchedVin.StorageName;
                car.LogLUDateTime = DateTime.Now;
                car.LogLUBy = user;

                // Cập nhật trạng thái xe kho
                matchedVin.Status = CarVinStatus.Mapped;
                matchedVin.MappedCarId = car.CarId;
                availableVins.Remove(matchedVin);

                session.Details.Add(new MapVinSessionDetail
                {
                    CarId = car.CarId,
                    SoCode = car.SoCode,
                    DealerCode = car.DealerCode,
                    DealerName = car.DealerName,
                    ModelCode = car.ModelCode,
                    ModelName = car.ModelName,
                    SpecCode = car.SpecCode,
                    ColorCode = car.ColorCode,
                    ColorName = car.ColorName,
                    Vin = matchedVin.Vin,
                    EngineNo = matchedVin.EngineNo,
                    StorageCode = matchedVin.StorageCode,
                    StorageName = matchedVin.StorageName,
                    Status = MapVinDetailStatus.Mapped,
                    MappedAt = DateTime.Now,
                    MappedBy = user
                });

                logs.Add(new MapVinAuditLog
                {
                    OrgId = Org,
                    CarId = car.CarId,
                    Vin = matchedVin.Vin,
                    Action = "AutoMap",
                    SessionNo = sessionNo,
                    Remark = $"Tự động ghép số khung VIN {matchedVin.Vin} theo phiên {sessionNo} (phương thức {dto.Method})",
                    PerformedBy = user,
                    PerformedAt = DateTime.Now
                });

                mapped++;
            }
            else
            {
                session.Details.Add(new MapVinSessionDetail
                {
                    CarId = car.CarId,
                    SoCode = car.SoCode,
                    DealerCode = car.DealerCode,
                    DealerName = car.DealerName,
                    ModelCode = car.ModelCode,
                    ModelName = car.ModelName,
                    SpecCode = car.SpecCode,
                    ColorCode = car.ColorCode,
                    ColorName = car.ColorName,
                    Vin = null,
                    Status = MapVinDetailStatus.Rejected,
                    RejectReason = "Kho tạm thời hết số khung VIN khả dụng cùng model xe",
                    MappedAt = DateTime.Now,
                    MappedBy = user
                });

                unmapped++;
            }
        }

        session.TotalMapped = mapped;
        session.TotalUnmapped = unmapped;

        db.MapVinSessions.Add(session);
        if (logs.Count > 0) db.MapVinAuditLogs.AddRange(logs);

        await db.SaveChangesAsync();
        return ToSessionDto(session);
    }

    public async Task<object> ManualMapAsync(ManualMapVinDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.CarId))
            throw new InvalidOperationException("Mã xe thương mại CarId không được để trống.");
        if (string.IsNullOrWhiteSpace(dto.Vin))
            throw new InvalidOperationException("Số khung VIN không được để trống.");

        var user = string.IsNullOrWhiteSpace(dto.ActionBy) ? "CHUYÊN VIÊN ĐIỀU PHỐI NPP" : dto.ActionBy.Trim();
        var targetVin = dto.Vin.Trim().ToUpperInvariant();

        // 1. Kiểm tra xe CarRecord
        var car = await db.Cars.FirstOrDefaultAsync(c => c.OrgId == Org && c.CarId == dto.CarId.Trim());
        if (car == null)
            throw new InvalidOperationException($"Không tìm thấy xe thương mại có mã '{dto.CarId}'.");

        if (car.FlagActive != "1")
            throw new InvalidOperationException($"Xe '{car.CarId}' đã bị hủy (FlagActive = 0), không thể thực hiện gán VIN.");

        if (car.FlagMapVIN != "1")
            throw new InvalidOperationException($"Xe '{car.CarId}' đang bị khóa tính năng gán VIN (FlagMapVIN = 0).");

        if (!string.IsNullOrWhiteSpace(car.Vin) && car.FlagAllowChangeVIN == "0")
            throw new InvalidOperationException($"Xe '{car.CarId}' đã có VIN ({car.Vin}) và bị khóa không cho phép đổi VIN (FlagAllowChangeVIN = 0).");

        // Ràng buộc lệnh xuất xe
        var hasActiveDO = await db.DeliveryOrders.AnyAsync(o => o.OrgId == Org && o.Vin == car.Vin && (o.Status == DeliveryOrderStatus.Approved || o.Status == DeliveryOrderStatus.Delivered));
        if (hasActiveDO)
            throw new InvalidOperationException($"Không thể gán/đổi VIN cho xe '{car.CarId}' vì đã có Lệnh xuất xe đã duyệt hoặc bàn giao.");

        // 2. Kiểm tra xe tồn kho CarVinInventory
        var vinInv = await db.CarVinInventories.FirstOrDefaultAsync(v => v.OrgId == Org && v.Vin == targetVin);
        if (vinInv == null)
            throw new InvalidOperationException($"Không tìm thấy số khung VIN '{targetVin}' trong kho hệ thống.");

        if (vinInv.Status != CarVinStatus.Available && vinInv.MappedCarId != car.CarId)
            throw new InvalidOperationException($"Số khung VIN '{targetVin}' đang ở trạng thái '{vinInv.Status}' và đã được gán cho xe khác.");

        if (!vinInv.ModelCode.Equals(car.ModelCode, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException($"Model của số khung VIN ({vinInv.ModelCode}) không khớp với Model của xe thương mại ({car.ModelCode}).");

        // Nếu xe đã có VIN cũ khác với VIN mới thì giải phóng VIN cũ
        if (!string.IsNullOrWhiteSpace(car.Vin) && !car.Vin.Equals(targetVin, StringComparison.OrdinalIgnoreCase))
        {
            var oldVinInv = await db.CarVinInventories.FirstOrDefaultAsync(v => v.OrgId == Org && v.Vin == car.Vin);
            if (oldVinInv != null)
            {
                oldVinInv.Status = CarVinStatus.Available;
                oldVinInv.MappedCarId = null;
            }
        }

        // 3. Thực hiện gán
        var oldVin = car.Vin;
        car.Vin = vinInv.Vin;
        if (!string.IsNullOrWhiteSpace(vinInv.EngineNo))
            car.EngineNo = vinInv.EngineNo;
        car.StorageCode = string.IsNullOrWhiteSpace(dto.StorageCode) ? vinInv.StorageCode : dto.StorageCode.Trim();
        car.StorageName = vinInv.StorageName;
        car.LogLUDateTime = DateTime.Now;
        car.LogLUBy = user;

        vinInv.Status = CarVinStatus.Mapped;
        vinInv.MappedCarId = car.CarId;

        // Lưu audit log
        var action = string.IsNullOrWhiteSpace(oldVin) ? "ManualMap" : "SwapVin";
        var audit = new MapVinAuditLog
        {
            OrgId = Org,
            CarId = car.CarId,
            Vin = vinInv.Vin,
            Action = action,
            Remark = dto.Remark ?? $"Thao tác {action}: gán số khung VIN {vinInv.Vin} cho xe {car.CarId}",
            PerformedBy = user,
            PerformedAt = DateTime.Now
        };
        db.MapVinAuditLogs.Add(audit);

        await db.SaveChangesAsync();

        return new
        {
            success = true,
            action,
            carId = car.CarId,
            vin = car.Vin,
            modelCode = car.ModelCode,
            dealerCode = car.DealerCode,
            storageCode = car.StorageCode,
            performedBy = user,
            performedAt = audit.PerformedAt
        };
    }

    public async Task<object> UnmapAsync(UnmapVinDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.CarId))
            throw new InvalidOperationException("Mã xe thương mại CarId không được để trống.");
        if (string.IsNullOrWhiteSpace(dto.Reason))
            throw new InvalidOperationException("Lý do gỡ VIN không được để trống.");

        var user = string.IsNullOrWhiteSpace(dto.ActionBy) ? "CHUYÊN VIÊN ĐIỀU PHỐI NPP" : dto.ActionBy.Trim();

        var car = await db.Cars.FirstOrDefaultAsync(c => c.OrgId == Org && c.CarId == dto.CarId.Trim());
        if (car == null)
            throw new InvalidOperationException($"Không tìm thấy xe thương mại có mã '{dto.CarId}'.");

        if (car.FlagActive != "1")
            throw new InvalidOperationException($"Xe '{car.CarId}' đã bị hủy (FlagActive = 0), không thể thực hiện gỡ VIN.");

        if (car.FlagAllowChangeVIN != "1")
            throw new InvalidOperationException($"Xe '{car.CarId}' bị khóa không cho phép đổi/gỡ VIN (FlagAllowChangeVIN = 0).");

        if (string.IsNullOrWhiteSpace(car.Vin))
            throw new InvalidOperationException($"Xe '{car.CarId}' hiện tại chưa được gán số khung VIN.");

        // Ràng buộc kiểm tra nguồn DMS.Sales (Car_VIN_UnmapX):
        // 1. Không vướng CarDocRequest active
        var hasDocReq = await db.CarDocRequests.AnyAsync(r => r.OrgId == Org
            && r.Status != DocReqStatus.Cancelled
            && r.Status != DocReqStatus.Rejected
            && r.Details.Any(d => d.Vin == car.Vin));
        if (hasDocReq)
            throw new InvalidOperationException($"Không thể gỡ VIN '{car.Vin}' vì đang có Đề nghị rút/giao hồ sơ xe (CarDocRequest) chưa hoàn tất.");

        // 2. Không vướng DeliveryOrder active
        var hasDO = await db.DeliveryOrders.AnyAsync(o => o.OrgId == Org && o.Vin == car.Vin && (o.Status == DeliveryOrderStatus.Pending || o.Status == DeliveryOrderStatus.Approved || o.Status == DeliveryOrderStatus.Delivered));
        if (hasDO)
            throw new InvalidOperationException($"Không thể gỡ VIN '{car.Vin}' vì đang có Lệnh xuất xe (DeliveryOrder) đang xử lý hoặc đã bàn giao.");

        // 3. Không vướng CarInvoice phát hành
        var hasInv = await db.CarInvoices.AnyAsync(i => i.OrgId == Org && i.Status == InvoiceStatus.Issued && i.Details.Any(d => d.Vin == car.Vin));
        if (hasInv)
            throw new InvalidOperationException($"Không thể gỡ VIN '{car.Vin}' vì xe đã được phát hành Hóa đơn VAT bán buôn.");

        var oldVin = car.Vin;
        car.Vin = "";
        car.LogLUDateTime = DateTime.Now;
        car.LogLUBy = user;

        var vinInv = await db.CarVinInventories.FirstOrDefaultAsync(v => v.OrgId == Org && v.Vin == oldVin);
        if (vinInv != null)
        {
            vinInv.Status = CarVinStatus.Available;
            vinInv.MappedCarId = null;
        }

        var audit = new MapVinAuditLog
        {
            OrgId = Org,
            CarId = car.CarId,
            Vin = oldVin,
            Action = "Unmap",
            Remark = $"Gỡ số khung VIN: {dto.Reason.Trim()}",
            PerformedBy = user,
            PerformedAt = DateTime.Now
        };
        db.MapVinAuditLogs.Add(audit);

        await db.SaveChangesAsync();

        return new
        {
            success = true,
            action = "Unmap",
            carId = car.CarId,
            unmappedVin = oldVin,
            reason = dto.Reason,
            performedBy = user,
            performedAt = audit.PerformedAt
        };
    }

    public async Task<BatchUnmapResultDto> BatchUnmapAsync(BatchUnmapVinDto dto)
    {
        if (dto.CarIds == null || dto.CarIds.Count == 0)
            throw new InvalidOperationException("Danh sách xe cần gỡ VIN không được để trống.");
        if (string.IsNullOrWhiteSpace(dto.Reason))
            throw new InvalidOperationException("Lý do gỡ VIN hàng loạt không được để trống.");

        var succeeded = new List<string>();
        var failed = new List<string>();
        var errors = new List<string>();

        foreach (var carId in dto.CarIds)
        {
            try
            {
                await UnmapAsync(new UnmapVinDto(carId, dto.Reason, dto.ActionBy));
                succeeded.Add(carId);
            }
            catch (Exception ex)
            {
                failed.Add(carId);
                errors.Add($"[{carId}]: {ex.Message}");
            }
        }

        return new BatchUnmapResultDto(
            dto.CarIds.Count,
            succeeded.Count,
            failed.Count,
            succeeded,
            failed,
            errors
        );
    }

    public async Task<List<CarVinInventoryDto>> GetAvailableVinsAsync(string? modelCode, string? colorCode, string? storageCode)
    {
        var q = db.CarVinInventories
            .AsNoTracking()
            .Where(v => v.OrgId == Org && v.Status == CarVinStatus.Available);

        if (!string.IsNullOrWhiteSpace(modelCode))
        {
            var m = modelCode.Trim();
            q = q.Where(v => v.ModelCode == m);
        }

        if (!string.IsNullOrWhiteSpace(colorCode))
        {
            var c = colorCode.Trim();
            q = q.Where(v => v.ColorCode == c);
        }

        if (!string.IsNullOrWhiteSpace(storageCode))
        {
            var s = storageCode.Trim();
            q = q.Where(v => v.StorageCode == s);
        }

        var list = await q.OrderBy(v => v.ModelCode).ThenBy(v => v.CreatedAt).ToListAsync();
        return list.Select(v => new CarVinInventoryDto(
            v.Id,
            v.Vin,
            v.ModelCode,
            v.ModelName,
            v.SpecCode,
            v.SpecDescription,
            v.ColorCode,
            v.ColorName,
            v.EngineNo,
            v.StorageCode,
            v.StorageName,
            v.AssemblyStatus,
            v.ProductionMonth,
            v.Status.ToString(),
            v.MappedCarId,
            v.CreatedAt
        )).ToList();
    }

    public async Task<List<PendingMapCarDto>> GetPendingCarsAsync(string? dealerCode, string? modelCode)
    {
        var q = db.Cars
            .AsNoTracking()
            .Where(c => c.OrgId == Org && c.FlagActive == "1" && (c.Vin == null || c.Vin == ""));

        if (!string.IsNullOrWhiteSpace(dealerCode))
        {
            var d = dealerCode.Trim();
            q = q.Where(c => c.DealerCode == d);
        }

        if (!string.IsNullOrWhiteSpace(modelCode))
        {
            var m = modelCode.Trim();
            q = q.Where(c => c.ModelCode == m);
        }

        var list = await q.OrderByDescending(c => c.PaymentDepositAmount).ThenBy(c => c.CreatedAt).ToListAsync();
        return list.Select(c =>
        {
            var depPct = c.PriceActual > 0 ? Math.Round((double)(c.PaymentDepositAmount / c.PriceActual) * 100, 2) : 0;
            return new PendingMapCarDto(
                c.CarId,
                c.SoCode,
                c.DealerCode,
                c.DealerName,
                c.ModelCode,
                c.ModelName,
                c.SpecCode,
                c.SpecDescription,
                c.ColorCode,
                c.ColorName,
                c.EngineNo,
                c.StorageCode,
                c.StorageName,
                c.PriceActual,
                c.PaymentDepositAmount,
                c.GuaranteeAmount,
                c.DutyCompletedAmount,
                depPct,
                c.FlagActive,
                c.FlagAllowChangeVIN,
                c.FlagEarlyCancel,
                c.FlagMapVIN,
                c.CreatedAt
            );
        }).ToList();
    }

    public async Task<MapVinStatsDto> GetStatsAsync()
    {
        var activeCars = await db.Cars.AsNoTracking().Where(c => c.OrgId == Org && c.FlagActive == "1").ToListAsync();
        var totalActive = activeCars.Count;
        var mappedCars = activeCars.Count(c => !string.IsNullOrWhiteSpace(c.Vin));
        var pendingCars = totalActive - mappedCars;
        var coverage = totalActive > 0 ? Math.Round((double)mappedCars / totalActive * 100, 2) : 0;

        var availableVinStock = await db.CarVinInventories.AsNoTracking().CountAsync(v => v.OrgId == Org && v.Status == CarVinStatus.Available);
        var totalSessions = await db.MapVinSessions.AsNoTracking().CountAsync(s => s.OrgId == Org);

        var mappedByModel = activeCars
            .Where(c => !string.IsNullOrWhiteSpace(c.Vin))
            .GroupBy(c => string.IsNullOrWhiteSpace(c.ModelCode) ? "UNKNOWN" : c.ModelCode)
            .ToDictionary(g => g.Key, g => g.Count());

        var mappedByDealer = activeCars
            .Where(c => !string.IsNullOrWhiteSpace(c.Vin))
            .GroupBy(c => string.IsNullOrWhiteSpace(c.DealerCode) ? "UNKNOWN" : c.DealerCode)
            .ToDictionary(g => g.Key, g => g.Count());

        var stockByModel = await db.CarVinInventories
            .AsNoTracking()
            .Where(v => v.OrgId == Org && v.Status == CarVinStatus.Available)
            .GroupBy(v => v.ModelCode)
            .Select(g => new { g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Key, x => x.Count);

        return new MapVinStatsDto(
            totalActive,
            mappedCars,
            pendingCars,
            coverage,
            availableVinStock,
            totalSessions,
            mappedByModel,
            mappedByDealer,
            stockByModel
        );
    }

    public async Task<List<MapVinAuditLogDto>> GetLogsAsync(string? carId, string? vin)
    {
        var q = db.MapVinAuditLogs.AsNoTracking().Where(l => l.OrgId == Org);

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

        var list = await q.OrderByDescending(l => l.PerformedAt).Take(100).ToListAsync();
        return list.Select(l => new MapVinAuditLogDto(
            l.Id,
            l.CarId,
            l.Vin,
            l.Action,
            l.SessionNo,
            l.Remark,
            l.PerformedBy,
            l.PerformedAt
        )).ToList();
    }

    private static MapVinSessionDto ToSessionDto(MapVinSession s) => new(
        s.Id,
        s.SessionNo,
        s.MapType.ToString(),
        s.Method.ToString(),
        s.Status.ToString(),
        s.DealerCode,
        s.ModelCode,
        s.TotalRequested,
        s.TotalMapped,
        s.TotalUnmapped,
        s.Remark,
        s.CreatedBy,
        s.CreatedAt,
        s.ApprovedBy,
        s.ApprovedAt,
        s.Details.Select(d => new MapVinDetailDto(
            d.Id,
            d.CarId,
            d.SoCode,
            d.DealerCode,
            d.DealerName,
            d.ModelCode,
            d.ModelName,
            d.SpecCode,
            d.ColorCode,
            d.ColorName,
            d.Vin,
            d.EngineNo,
            d.StorageCode,
            d.StorageName,
            d.Status.ToString(),
            d.RejectReason,
            d.MappedAt,
            d.MappedBy
        )).ToList()
    );
}
