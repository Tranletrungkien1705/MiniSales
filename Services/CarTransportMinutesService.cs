using Microsoft.EntityFrameworkCore;
using MiniSales.Data;
using MiniSales.Models;

namespace MiniSales.Services;

public record CreateTransportMinutesCarDto(
    string? CarId,
    string Vin,
    string Model,
    string? SpecCode,
    string? ColorCode,
    string? EngineNo,
    string? DeliveryOrderNo,
    string? OrderNo,
    string? GuaranteeNo,
    int? Odometer,
    string? ConditionNote,
    string? Remark
);

public record CreateTransportMinutesDto(
    string? TransportMinutesNo,
    string DealerCode,
    string? DealerName,
    string? TransporterName,
    string? PlateNo,
    string? DriverName,
    string? DriverPhone,
    string? BankCode,
    string? BankName,
    DateTime? TransportMinutesDate,
    string? Remark,
    List<CreateTransportMinutesCarDto>? Cars
);

public record ApproveDLTransportMinutesDto(
    string? ApprovedBy,
    string? Note
);

public record ApproveHQTransportMinutesDto(
    string? ApprovedBy,
    string? FilePath,
    string? Note
);

public record CancelTransportMinutesDto(
    string Reason,
    string? CancelledBy
);

public record AddCarToTransportMinutesDto(
    string? CarId,
    string Vin,
    string Model,
    string? SpecCode,
    string? ColorCode,
    string? EngineNo,
    string? DeliveryOrderNo,
    string? OrderNo,
    string? GuaranteeNo,
    int? Odometer,
    string? ConditionNote,
    string? Remark
);

public interface ICarTransportMinutesService
{
    Task<object> CreateAsync(CreateTransportMinutesDto dto);
    Task<object> ListAsync(string? status, string? dealer, string? vin, string? doNo, string? minutesNo);
    Task<object?> DetailAsync(string minutesNo);
    Task<object?> ApproveDLAsync(string minutesNo, ApproveDLTransportMinutesDto? dto);
    Task<object?> ApproveHQAsync(string minutesNo, ApproveHQTransportMinutesDto? dto);
    Task<object?> CancelAsync(string minutesNo, CancelTransportMinutesDto dto);
    Task<object?> DeleteDraftAsync(string minutesNo);
    Task<object?> AddCarAsync(string minutesNo, AddCarToTransportMinutesDto dto);
    Task<object?> RemoveCarAsync(string minutesNo, long detailId);
    Task<object> StatsAsync();
}

public sealed class CarTransportMinutesService(AppDbContext db, ITenantContext tenant) : ICarTransportMinutesService
{
    private Guid Org => tenant.OrgId;

    public async Task<object> CreateAsync(CreateTransportMinutesDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.DealerCode))
            throw new InvalidOperationException("Mã đại lý (DealerCode) là bắt buộc.");

        var minutesNo = string.IsNullOrWhiteSpace(dto.TransportMinutesNo)
            ? await GenerateMinutesNoAsync()
            : dto.TransportMinutesNo.Trim().ToUpperInvariant();

        if (await db.TransportMinutes.AnyAsync(x => x.OrgId == Org && x.TransportMinutesNo == minutesNo))
            throw new InvalidOperationException($"Số biên bản bàn giao '{minutesNo}' đã tồn tại trong hệ thống.");

        var item = new CarTransportMinutes
        {
            OrgId = Org,
            TransportMinutesNo = minutesNo,
            DealerCode = dto.DealerCode.Trim().ToUpperInvariant(),
            DealerName = string.IsNullOrWhiteSpace(dto.DealerName) ? dto.DealerCode.Trim() : dto.DealerName.Trim(),
            TransporterName = dto.TransporterName?.Trim(),
            PlateNo = dto.PlateNo?.Trim().ToUpperInvariant(),
            DriverName = dto.DriverName?.Trim(),
            DriverPhone = dto.DriverPhone?.Trim(),
            BankCode = string.IsNullOrWhiteSpace(dto.BankCode) ? "DEALER" : dto.BankCode.Trim().ToUpperInvariant(),
            BankName = dto.BankName?.Trim(),
            TransportMinutesDate = dto.TransportMinutesDate ?? DateTime.Today,
            Status = TransportMinutesStatus.Pending,
            DLSignStatus = DLSignStatus.Pending,
            HTCSignStatus = HTCSignStatus.Pending,
            DLCreatedBy = "CURRENT_USER",
            DLCreatedAt = DateTime.Now,
            Remark = dto.Remark?.Trim()
        };

        if (dto.Cars is not null && dto.Cars.Count > 0)
        {
            int carIndex = 1;
            foreach (var c in dto.Cars)
            {
                if (string.IsNullOrWhiteSpace(c.Vin) || string.IsNullOrWhiteSpace(c.Model))
                    continue;

                var vin = c.Vin.Trim().ToUpperInvariant();
                var carId = string.IsNullOrWhiteSpace(c.CarId) ? $"CAR-{DateTime.Now:yyyyMMdd}-{carIndex:D4}" : c.CarId.Trim();
                carIndex++;

                item.Cars.Add(new CarTransportMinutesDetail
                {
                    CarId = carId,
                    Vin = vin,
                    Model = c.Model.Trim(),
                    SpecCode = c.SpecCode?.Trim().ToUpperInvariant(),
                    ColorCode = c.ColorCode?.Trim(),
                    EngineNo = c.EngineNo?.Trim().ToUpperInvariant(),
                    DeliveryOrderNo = c.DeliveryOrderNo?.Trim(),
                    OrderNo = c.OrderNo?.Trim(),
                    GuaranteeNo = c.GuaranteeNo?.Trim(),
                    Odometer = c.Odometer.GetValueOrDefault(10),
                    ConditionNote = c.ConditionNote?.Trim() ?? "Xe mới 100%, đủ 2 chìa smartkey, sổ BH, phụ kiện tiêu chuẩn",
                    Status = TransportMinutesCarStatus.Pending,
                    Remark = c.Remark?.Trim()
                });
            }
        }

        item.TotalCars = item.Cars.Count;

        db.TransportMinutes.Add(item);
        await db.SaveChangesAsync();

        return (await DetailAsync(item.TransportMinutesNo))!;
    }

    public async Task<object> ListAsync(string? status, string? dealer, string? vin, string? doNo, string? minutesNo)
    {
        var q = db.TransportMinutes
            .Include(x => x.Cars)
            .Where(x => x.OrgId == Org)
            .AsNoTracking();

        if (!string.IsNullOrWhiteSpace(status))
        {
            if (Enum.TryParse<TransportMinutesStatus>(status, true, out var parsedStatus))
                q = q.Where(x => x.Status == parsedStatus);
            else if (string.Equals(status, "PendingDL", StringComparison.OrdinalIgnoreCase))
                q = q.Where(x => x.Status == TransportMinutesStatus.Pending && x.DLSignStatus == DLSignStatus.Pending);
            else if (string.Equals(status, "PendingHQ", StringComparison.OrdinalIgnoreCase))
                q = q.Where(x => x.DLSignStatus == DLSignStatus.Approved && x.HTCSignStatus == HTCSignStatus.Pending);
        }

        if (!string.IsNullOrWhiteSpace(dealer))
        {
            var d = dealer.Trim();
            q = q.Where(x => x.DealerCode.Contains(d) || x.DealerName.Contains(d));
        }

        if (!string.IsNullOrWhiteSpace(minutesNo))
        {
            var mn = minutesNo.Trim();
            q = q.Where(x => x.TransportMinutesNo.Contains(mn));
        }

        if (!string.IsNullOrWhiteSpace(doNo))
        {
            var d = doNo.Trim();
            q = q.Where(x => x.Cars.Any(c => c.DeliveryOrderNo != null && c.DeliveryOrderNo.Contains(d)));
        }

        if (!string.IsNullOrWhiteSpace(vin))
        {
            var v = vin.Trim();
            q = q.Where(x => x.Cars.Any(c => c.Vin.Contains(v)));
        }

        var items = await q.OrderByDescending(x => x.DLCreatedAt)
            .Select(x => new
            {
                x.Id,
                x.TransportMinutesNo,
                x.DealerCode,
                x.DealerName,
                x.TransporterName,
                x.PlateNo,
                x.DriverName,
                x.DriverPhone,
                x.BankCode,
                x.BankName,
                x.TransportMinutesDate,
                status = x.Status.ToString(),
                dlSignStatus = x.DLSignStatus.ToString(),
                htcSignStatus = x.HTCSignStatus.ToString(),
                x.TotalCars,
                x.DLCreatedBy,
                x.DLCreatedAt,
                x.DLApprBy,
                x.DLApprAt,
                x.HTCApprBy,
                x.HTCApprAt,
                x.HTCCancelBy,
                x.HTCCancelAt,
                x.CancelReason,
                x.FilePath,
                x.Remark,
                cars = x.Cars.Select(c => new
                {
                    c.Id,
                    c.CarId,
                    c.Vin,
                    c.Model,
                    c.SpecCode,
                    c.ColorCode,
                    c.EngineNo,
                    c.DeliveryOrderNo,
                    c.OrderNo,
                    c.GuaranteeNo,
                    c.Odometer,
                    c.ConditionNote,
                    status = c.Status.ToString(),
                    c.Remark
                }).ToList()
            })
            .ToListAsync();

        return new
        {
            total = items.Count,
            items
        };
    }

    public async Task<object?> DetailAsync(string minutesNo)
    {
        var item = await db.TransportMinutes
            .Include(x => x.Cars)
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.OrgId == Org && x.TransportMinutesNo == minutesNo.Trim());

        if (item is null) return null;

        return new
        {
            item.Id,
            item.TransportMinutesNo,
            item.DealerCode,
            item.DealerName,
            item.TransporterName,
            item.PlateNo,
            item.DriverName,
            item.DriverPhone,
            item.BankCode,
            item.BankName,
            item.TransportMinutesDate,
            status = item.Status.ToString(),
            dlSignStatus = item.DLSignStatus.ToString(),
            htcSignStatus = item.HTCSignStatus.ToString(),
            item.TotalCars,
            item.DLCreatedBy,
            item.DLCreatedAt,
            item.DLApprBy,
            item.DLApprAt,
            item.HTCApprBy,
            item.HTCApprAt,
            item.HTCCancelBy,
            item.HTCCancelAt,
            item.CancelReason,
            item.FilePath,
            item.Remark,
            cars = item.Cars.Select(c => new
            {
                c.Id,
                c.CarId,
                c.Vin,
                c.Model,
                c.SpecCode,
                c.ColorCode,
                c.EngineNo,
                c.DeliveryOrderNo,
                c.OrderNo,
                c.GuaranteeNo,
                c.Odometer,
                c.ConditionNote,
                status = c.Status.ToString(),
                c.Remark
            }).ToList()
        };
    }

    public async Task<object?> ApproveDLAsync(string minutesNo, ApproveDLTransportMinutesDto? dto)
    {
        var item = await db.TransportMinutes
            .Include(x => x.Cars)
            .FirstOrDefaultAsync(x => x.OrgId == Org && x.TransportMinutesNo == minutesNo.Trim());

        if (item is null) return null;

        if (item.Status != TransportMinutesStatus.Pending)
            throw new InvalidOperationException($"Biên bản '{minutesNo}' đang ở trạng thái '{item.Status}', không thể duyệt ký cấp Đại lý.");

        item.DLSignStatus = DLSignStatus.Approved;
        item.DLApprBy = dto?.ApprovedBy ?? "DEALER_AUTHORIZED";
        item.DLApprAt = DateTime.Now;
        item.Status = TransportMinutesStatus.DealerApproved;

        if (!string.IsNullOrWhiteSpace(dto?.Note))
            item.Remark = string.IsNullOrWhiteSpace(item.Remark) ? dto.Note : $"{item.Remark}; {dto.Note}";

        foreach (var c in item.Cars)
        {
            if (c.Status == TransportMinutesCarStatus.Pending)
                c.Status = TransportMinutesCarStatus.Approved;
        }

        await db.SaveChangesAsync();
        return await DetailAsync(item.TransportMinutesNo);
    }

    public async Task<object?> ApproveHQAsync(string minutesNo, ApproveHQTransportMinutesDto? dto)
    {
        var item = await db.TransportMinutes
            .Include(x => x.Cars)
            .FirstOrDefaultAsync(x => x.OrgId == Org && x.TransportMinutesNo == minutesNo.Trim());

        if (item is null) return null;

        if (item.Status == TransportMinutesStatus.Completed)
            throw new InvalidOperationException($"Biên bản '{minutesNo}' đã được NPP duyệt hoàn tất trước đó.");

        if (item.Status == TransportMinutesStatus.Cancelled)
            throw new InvalidOperationException($"Biên bản '{minutesNo}' đã bị hủy, không thể duyệt.");

        if (item.DLSignStatus != DLSignStatus.Approved)
            throw new InvalidOperationException($"Biên bản '{minutesNo}' chưa có xác nhận ký nhận của Đại lý (DLSignStatus = Pending).");

        item.HTCSignStatus = HTCSignStatus.Approved;
        item.HTCApprBy = dto?.ApprovedBy ?? "NPP_HQ_DIRECTOR";
        item.HTCApprAt = DateTime.Now;
        item.Status = TransportMinutesStatus.Completed;

        if (!string.IsNullOrWhiteSpace(dto?.FilePath))
            item.FilePath = dto.FilePath.Trim();

        if (!string.IsNullOrWhiteSpace(dto?.Note))
            item.Remark = string.IsNullOrWhiteSpace(item.Remark) ? dto.Note : $"{item.Remark}; {dto.Note}";

        // Tự động cập nhật DeliveryOrder và SalesOrder sang Delivered nếu có liên kết
        var now = DateTime.Now;
        foreach (var c in item.Cars)
        {
            if (!string.IsNullOrWhiteSpace(c.DeliveryOrderNo))
            {
                var doItem = await db.DeliveryOrders.FirstOrDefaultAsync(d => d.OrgId == Org && d.DeliveryOrderNo == c.DeliveryOrderNo);
                if (doItem != null && doItem.Status != DeliveryOrderStatus.Delivered)
                {
                    doItem.Status = DeliveryOrderStatus.Delivered;
                    doItem.DeliveredAt = now;
                    doItem.HandoverNote = $"Đã bàn giao theo BBBG số {item.TransportMinutesNo}. Odometer: {c.Odometer}km.";
                }
            }

            if (!string.IsNullOrWhiteSpace(c.OrderNo))
            {
                var so = await db.Orders.FirstOrDefaultAsync(o => o.OrgId == Org && o.Code == c.OrderNo);
                if (so != null && so.Status == SalesStatus.Paid)
                {
                    so.Status = SalesStatus.Delivered;
                    so.DeliveredAt = now;
                }
            }
        }

        await db.SaveChangesAsync();
        return await DetailAsync(item.TransportMinutesNo);
    }

    public async Task<object?> CancelAsync(string minutesNo, CancelTransportMinutesDto dto)
    {
        var item = await db.TransportMinutes
            .FirstOrDefaultAsync(x => x.OrgId == Org && x.TransportMinutesNo == minutesNo.Trim());

        if (item is null) return null;

        if (item.Status == TransportMinutesStatus.Completed)
            throw new InvalidOperationException($"Biên bản '{minutesNo}' đã hoàn tất bàn giao, không thể hủy.");

        if (item.Status == TransportMinutesStatus.Cancelled)
            throw new InvalidOperationException($"Biên bản '{minutesNo}' đã bị hủy từ trước.");

        item.Status = TransportMinutesStatus.Cancelled;
        item.CancelReason = string.IsNullOrWhiteSpace(dto.Reason) ? "Hủy theo yêu cầu điều phối" : dto.Reason.Trim();
        item.HTCCancelBy = dto.CancelledBy ?? "USER";
        item.HTCCancelAt = DateTime.Now;

        await db.SaveChangesAsync();
        return await DetailAsync(item.TransportMinutesNo);
    }

    public async Task<object?> DeleteDraftAsync(string minutesNo)
    {
        var item = await db.TransportMinutes
            .Include(x => x.Cars)
            .FirstOrDefaultAsync(x => x.OrgId == Org && x.TransportMinutesNo == minutesNo.Trim());

        if (item is null) return null;

        if (item.Status != TransportMinutesStatus.Pending || item.DLSignStatus != DLSignStatus.Pending)
            throw new InvalidOperationException($"Chỉ được xóa biên bản nháp khi ở trạng thái Chờ duyệt (Pending) và chưa ký.");

        db.TransportMinutes.Remove(item);
        await db.SaveChangesAsync();

        return new { success = true, deletedMinutesNo = minutesNo };
    }

    public async Task<object?> AddCarAsync(string minutesNo, AddCarToTransportMinutesDto dto)
    {
        var item = await db.TransportMinutes
            .Include(x => x.Cars)
            .FirstOrDefaultAsync(x => x.OrgId == Org && x.TransportMinutesNo == minutesNo.Trim());

        if (item is null) return null;

        if (item.Status != TransportMinutesStatus.Pending)
            throw new InvalidOperationException($"Chỉ có thể thêm xe vào biên bản khi ở trạng thái Chờ duyệt (Pending).");

        if (string.IsNullOrWhiteSpace(dto.Vin) || string.IsNullOrWhiteSpace(dto.Model))
            throw new InvalidOperationException("Cần có số VIN và Model của xe.");

        var vin = dto.Vin.Trim().ToUpperInvariant();
        if (item.Cars.Any(c => string.Equals(c.Vin, vin, StringComparison.OrdinalIgnoreCase)))
            throw new InvalidOperationException($"Xe có số VIN '{vin}' đã có trong biên bản.");

        var carId = string.IsNullOrWhiteSpace(dto.CarId)
            ? $"CAR-{DateTime.Now:yyyyMMdd}-{(item.Cars.Count + 1):D4}"
            : dto.CarId.Trim();

        item.Cars.Add(new CarTransportMinutesDetail
        {
            CarId = carId,
            Vin = vin,
            Model = dto.Model.Trim(),
            SpecCode = dto.SpecCode?.Trim().ToUpperInvariant(),
            ColorCode = dto.ColorCode?.Trim(),
            EngineNo = dto.EngineNo?.Trim().ToUpperInvariant(),
            DeliveryOrderNo = dto.DeliveryOrderNo?.Trim(),
            OrderNo = dto.OrderNo?.Trim(),
            GuaranteeNo = dto.GuaranteeNo?.Trim(),
            Odometer = dto.Odometer.GetValueOrDefault(10),
            ConditionNote = dto.ConditionNote?.Trim() ?? "Xe mới 100%, đủ 2 chìa smartkey, sổ BH, phụ kiện tiêu chuẩn",
            Status = TransportMinutesCarStatus.Pending,
            Remark = dto.Remark?.Trim()
        });

        item.TotalCars = item.Cars.Count;
        await db.SaveChangesAsync();

        return await DetailAsync(item.TransportMinutesNo);
    }

    public async Task<object?> RemoveCarAsync(string minutesNo, long detailId)
    {
        var item = await db.TransportMinutes
            .Include(x => x.Cars)
            .FirstOrDefaultAsync(x => x.OrgId == Org && x.TransportMinutesNo == minutesNo.Trim());

        if (item is null) return null;

        if (item.Status != TransportMinutesStatus.Pending)
            throw new InvalidOperationException($"Chỉ có thể xóa xe khỏi biên bản khi ở trạng thái Chờ duyệt (Pending).");

        var car = item.Cars.FirstOrDefault(c => c.Id == detailId);
        if (car is null)
            throw new InvalidOperationException($"Không tìm thấy dòng xe với Id = {detailId} trong biên bản.");

        item.Cars.Remove(car);
        item.TotalCars = item.Cars.Count;

        await db.SaveChangesAsync();
        return await DetailAsync(item.TransportMinutesNo);
    }

    public async Task<object> StatsAsync()
    {
        var q = db.TransportMinutes
            .Where(x => x.OrgId == Org)
            .AsNoTracking();

        var totalMinutes = await q.CountAsync();
        var pendingDL = await q.CountAsync(x => x.Status == TransportMinutesStatus.Pending && x.DLSignStatus == DLSignStatus.Pending);
        var pendingHQ = await q.CountAsync(x => x.DLSignStatus == DLSignStatus.Approved && x.HTCSignStatus == HTCSignStatus.Pending);
        var completed = await q.CountAsync(x => x.Status == TransportMinutesStatus.Completed);
        var cancelled = await q.CountAsync(x => x.Status == TransportMinutesStatus.Cancelled);

        var totalCars = await db.TransportMinutesDetails
            .Where(d => db.TransportMinutes.Any(m => m.Id == d.TransportMinutesId && m.OrgId == Org))
            .CountAsync();

        var totalCarsDelivered = await db.TransportMinutesDetails
            .Where(d => db.TransportMinutes.Any(m => m.Id == d.TransportMinutesId && m.OrgId == Org && m.Status == TransportMinutesStatus.Completed))
            .CountAsync();

        return new
        {
            totalMinutes,
            pendingDL,
            pendingHQ,
            completed,
            cancelled,
            totalCars,
            totalCarsDelivered
        };
    }

    private async Task<string> GenerateMinutesNoAsync()
    {
        var prefix = $"BBBG{DateTime.Now:yyMM}";
        var count = await db.TransportMinutes.CountAsync(x => x.OrgId == Org && x.TransportMinutesNo.StartsWith(prefix));
        return $"{prefix}{(count + 1):D4}";
    }
}
