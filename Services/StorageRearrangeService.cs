using Microsoft.EntityFrameworkCore;
using MiniSales.Data;
using MiniSales.Models;

namespace MiniSales.Services;

public record CreateRearrangeDetailDto(
    string CarId,
    string Vin,
    string Model,
    string? SpecCode,
    string? ColorCode,
    string? EngineNo,
    DateTime? ExpectedStartDate,
    DateTime? ExpectedEndDate,
    string? Remark
);

public record CreateStorageRearrangeDto(
    string? RearrangeNo,
    string StorageCodeFrom,
    string? StorageNameFrom,
    string StorageCodeTo,
    string? StorageNameTo,
    string? RearrangeType,
    string? TransporterName,
    string? PlateNo,
    string? DriverName,
    string? DriverPhone,
    string? Remark,
    List<CreateRearrangeDetailDto>? Details
);

public record UpdateStorageRearrangeDto(
    string? StorageNameFrom,
    string? StorageNameTo,
    string? RearrangeType,
    string? TransporterName,
    string? PlateNo,
    string? DriverName,
    string? DriverPhone,
    string? Remark
);

public record Approve1RearrangeDto(
    string? ApprovedBy,
    string? Remark
);

public record Approve2RearrangeDto(
    string? ApprovedBy,
    DateTime? ActualOutDate,
    DateTime? ActualInDate,
    string? Remark
);

public record RejectRearrangeDto(
    string Reason,
    string? RejectedBy
);

public record CancelRearrangeDto(
    string Reason,
    string? CancelledBy
);

public record UpdateRearrangeDetailDto(
    DateTime? ExpectedEndDate,
    DateTime? ActualOutDate,
    DateTime? ActualInDate,
    string? ConfirmBy,
    string? Remark
);

public record AddRearrangeDetailDto(
    string CarId,
    string Vin,
    string Model,
    string? SpecCode,
    string? ColorCode,
    string? EngineNo,
    DateTime? ExpectedStartDate,
    DateTime? ExpectedEndDate,
    string? Remark
);

public interface IStorageRearrangeService
{
    Task<object> CreateAsync(CreateStorageRearrangeDto dto);
    Task<object> ListAsync(string? status, string? storageFrom, string? storageTo, string? vin, string? carId);
    Task<object?> DetailAsync(string rearrangeNo);
    Task<object?> UpdateAsync(string rearrangeNo, UpdateStorageRearrangeDto dto);
    Task<object?> Approve1Async(string rearrangeNo, Approve1RearrangeDto? dto);
    Task<object?> Approve2Async(string rearrangeNo, Approve2RearrangeDto? dto);
    Task<object?> RejectAsync(string rearrangeNo, RejectRearrangeDto dto);
    Task<object?> CancelAsync(string rearrangeNo, CancelRearrangeDto dto);
    Task<object?> DeleteAsync(string rearrangeNo);
    Task<object?> UpdateDetailAsync(string rearrangeNo, string vin, UpdateRearrangeDetailDto dto);
    Task<object?> AddCarAsync(string rearrangeNo, AddRearrangeDetailDto dto);
    Task<object?> RemoveCarAsync(string rearrangeNo, string vin);
    Task<object> StatsAsync();
}

public sealed class StorageRearrangeService(AppDbContext db, ITenantContext tenant) : IStorageRearrangeService
{
    private Guid Org => tenant.OrgId;

    public async Task<object> CreateAsync(CreateStorageRearrangeDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.StorageCodeFrom))
            throw new InvalidOperationException("Kho xuất (StorageCodeFrom) không được để trống.");

        if (string.IsNullOrWhiteSpace(dto.StorageCodeTo))
            throw new InvalidOperationException("Kho đến (StorageCodeTo) không được để trống.");

        if (string.Equals(dto.StorageCodeFrom.Trim(), dto.StorageCodeTo.Trim(), StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Kho xuất phát và kho nhận xe không được trùng nhau.");

        if (dto.Details is null || dto.Details.Count == 0)
            throw new InvalidOperationException("Lệnh điều chuyển kho phải có ít nhất 1 chi tiết xe.");

        // Kiểm tra tính hợp lệ và chống trùng lặp từng xe
        var vinSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var carIdSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var d in dto.Details)
        {
            if (string.IsNullOrWhiteSpace(d.CarId))
                throw new InvalidOperationException("Mỗi dòng xe bắt buộc phải có CarId.");
            if (string.IsNullOrWhiteSpace(d.Vin))
                throw new InvalidOperationException("Mỗi dòng xe bắt buộc phải có số khung VIN.");
            if (string.IsNullOrWhiteSpace(d.Model))
                throw new InvalidOperationException($"Xe {d.Vin} bắt buộc phải có thông tin Model dòng xe.");

            if (!vinSet.Add(d.Vin.Trim()))
                throw new InvalidOperationException($"Trùng lặp số khung VIN trong lệnh điều chuyển: {d.Vin}");
            if (!carIdSet.Add(d.CarId.Trim()))
                throw new InvalidOperationException($"Trùng lặp mã xe CarId trong lệnh điều chuyển: {d.CarId}");

            if (d.ExpectedStartDate.HasValue && d.ExpectedEndDate.HasValue && d.ExpectedStartDate > d.ExpectedEndDate)
                throw new InvalidOperationException($"Ngày dự kiến xuất ({d.ExpectedStartDate:yyyy-MM-dd}) không được lớn hơn ngày dự kiến nhận ({d.ExpectedEndDate:yyyy-MM-dd}) cho xe {d.Vin}.");
        }

        // Tự động sinh mã lệnh điều chuyển nếu không truyền vào
        string rearrangeNo;
        if (!string.IsNullOrWhiteSpace(dto.RearrangeNo))
        {
            rearrangeNo = dto.RearrangeNo.Trim().ToUpperInvariant();
        }
        else
        {
            var datePrefix = DateTime.Now.ToString("yyMMdd");
            var countToday = await db.StorageRearranges
                .CountAsync(x => x.OrgId == Org && x.RearrangeNo.StartsWith("SR" + datePrefix));
            rearrangeNo = $"SR{datePrefix}{(countToday + 1):D4}";
        }

        if (await db.StorageRearranges.AnyAsync(x => x.OrgId == Org && x.RearrangeNo == rearrangeNo))
            throw new InvalidOperationException($"Mã lệnh điều chuyển kho '{rearrangeNo}' đã tồn tại trên hệ thống.");

        var rearrangeType = dto.RearrangeType?.Trim().ToUpperInvariant() switch
        {
            "URGENT" => StorageRearrangeType.Urgent,
            "SHOWROOM" => StorageRearrangeType.Showroom,
            "MAINTENANCE" => StorageRearrangeType.Maintenance,
            _ => StorageRearrangeType.Normal
        };

        var order = new StorageRearrangeOrder
        {
            OrgId = Org,
            RearrangeNo = rearrangeNo,
            StorageCodeFrom = dto.StorageCodeFrom.Trim().ToUpperInvariant(),
            StorageNameFrom = string.IsNullOrWhiteSpace(dto.StorageNameFrom) ? $"Kho {dto.StorageCodeFrom.Trim().ToUpperInvariant()}" : dto.StorageNameFrom.Trim(),
            StorageCodeTo = dto.StorageCodeTo.Trim().ToUpperInvariant(),
            StorageNameTo = string.IsNullOrWhiteSpace(dto.StorageNameTo) ? $"Kho {dto.StorageCodeTo.Trim().ToUpperInvariant()}" : dto.StorageNameTo.Trim(),
            RearrangeType = rearrangeType,
            Status = StorageRearrangeStatus.Pending,
            TotalCars = dto.Details.Count,
            TransporterName = dto.TransporterName?.Trim(),
            PlateNo = dto.PlateNo?.Trim(),
            DriverName = dto.DriverName?.Trim(),
            DriverPhone = dto.DriverPhone?.Trim(),
            Remark = dto.Remark?.Trim(),
            CreatedBy = "NPP_DISPATCHER",
            CreatedAt = DateTime.Now
        };

        foreach (var it in dto.Details)
        {
            order.Details.Add(new StorageRearrangeDetail
            {
                RearrangeNo = rearrangeNo,
                CarId = it.CarId.Trim(),
                Vin = it.Vin.Trim().ToUpperInvariant(),
                Model = it.Model.Trim(),
                SpecCode = it.SpecCode?.Trim(),
                ColorCode = it.ColorCode?.Trim(),
                EngineNo = it.EngineNo?.Trim(),
                StorageCodeFrom = order.StorageCodeFrom,
                StorageCodeTo = order.StorageCodeTo,
                ExpectedStartDate = it.ExpectedStartDate ?? DateTime.Today,
                ExpectedEndDate = it.ExpectedEndDate ?? DateTime.Today.AddDays(3),
                Status = StorageRearrangeDtlStatus.Pending,
                Remark = it.Remark?.Trim()
            });
        }

        db.StorageRearranges.Add(order);
        await db.SaveChangesAsync();

        return new
        {
            message = "Tạo lệnh điều chuyển kho xe thành công.",
            rearrangeNo = order.RearrangeNo,
            storageCodeFrom = order.StorageCodeFrom,
            storageCodeTo = order.StorageCodeTo,
            rearrangeType = order.RearrangeType.ToString(),
            status = order.Status.ToString(),
            totalCars = order.TotalCars,
            createdAt = order.CreatedAt
        };
    }

    public async Task<object> ListAsync(string? status, string? storageFrom, string? storageTo, string? vin, string? carId)
    {
        var q = db.StorageRearranges.Include(x => x.Details).Where(x => x.OrgId == Org);

        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<StorageRearrangeStatus>(status, true, out var stEnum))
            q = q.Where(x => x.Status == stEnum);

        if (!string.IsNullOrWhiteSpace(storageFrom))
            q = q.Where(x => x.StorageCodeFrom == storageFrom.Trim().ToUpperInvariant());

        if (!string.IsNullOrWhiteSpace(storageTo))
            q = q.Where(x => x.StorageCodeTo == storageTo.Trim().ToUpperInvariant());

        if (!string.IsNullOrWhiteSpace(vin))
        {
            var vTrim = vin.Trim();
            q = q.Where(x => x.Details.Any(d => EF.Functions.Like(d.Vin, $"%{vTrim}%")));
        }

        if (!string.IsNullOrWhiteSpace(carId))
        {
            var cTrim = carId.Trim();
            q = q.Where(x => x.Details.Any(d => EF.Functions.Like(d.CarId, $"%{cTrim}%")));
        }

        var list = await q.OrderByDescending(x => x.CreatedAt).ToListAsync();

        return list.Select(x => new
        {
            x.Id,
            x.RearrangeNo,
            x.StorageCodeFrom,
            x.StorageNameFrom,
            x.StorageCodeTo,
            x.StorageNameTo,
            rearrangeType = x.RearrangeType.ToString(),
            status = x.Status.ToString(),
            x.TotalCars,
            x.TransporterName,
            x.PlateNo,
            x.DriverName,
            x.DriverPhone,
            x.Remark,
            x.RejectReason,
            x.CancelReason,
            x.CreatedBy,
            x.CreatedAt,
            x.Approved1By,
            x.Approved1At,
            x.Approved2By,
            x.Approved2At,
            x.CancelledBy,
            x.CancelledAt,
            itemsCount = x.Details.Count
        });
    }

    public async Task<object?> DetailAsync(string rearrangeNo)
    {
        var ord = await db.StorageRearranges
            .Include(x => x.Details)
            .FirstOrDefaultAsync(x => x.OrgId == Org && x.RearrangeNo == rearrangeNo.Trim().ToUpperInvariant());

        if (ord is null) return null;

        return new
        {
            ord.Id,
            ord.RearrangeNo,
            ord.StorageCodeFrom,
            ord.StorageNameFrom,
            ord.StorageCodeTo,
            ord.StorageNameTo,
            rearrangeType = ord.RearrangeType.ToString(),
            status = ord.Status.ToString(),
            ord.TotalCars,
            ord.TransporterName,
            ord.PlateNo,
            ord.DriverName,
            ord.DriverPhone,
            ord.Remark,
            ord.RejectReason,
            ord.CancelReason,
            ord.CreatedBy,
            ord.CreatedAt,
            ord.Approved1By,
            ord.Approved1At,
            ord.Approved2By,
            ord.Approved2At,
            ord.CancelledBy,
            ord.CancelledAt,
            details = ord.Details.Select(d => new
            {
                d.Id,
                d.CarId,
                d.Vin,
                d.Model,
                d.SpecCode,
                d.ColorCode,
                d.EngineNo,
                d.StorageCodeFrom,
                d.StorageCodeTo,
                d.ExpectedStartDate,
                d.ExpectedEndDate,
                d.ActualOutDate,
                d.ActualInDate,
                status = d.Status.ToString(),
                d.ConfirmDate,
                d.ConfirmBy,
                d.Remark
            })
        };
    }

    public async Task<object?> UpdateAsync(string rearrangeNo, UpdateStorageRearrangeDto dto)
    {
        var ord = await db.StorageRearranges
            .FirstOrDefaultAsync(x => x.OrgId == Org && x.RearrangeNo == rearrangeNo.Trim().ToUpperInvariant());

        if (ord is null) return null;

        if (ord.Status != StorageRearrangeStatus.Pending)
            throw new InvalidOperationException($"Chỉ cho phép cập nhật khi lệnh ở trạng thái 'Pending'. Trạng thái hiện tại: {ord.Status}.");

        if (!string.IsNullOrWhiteSpace(dto.StorageNameFrom)) ord.StorageNameFrom = dto.StorageNameFrom.Trim();
        if (!string.IsNullOrWhiteSpace(dto.StorageNameTo)) ord.StorageNameTo = dto.StorageNameTo.Trim();
        if (!string.IsNullOrWhiteSpace(dto.TransporterName)) ord.TransporterName = dto.TransporterName.Trim();
        if (!string.IsNullOrWhiteSpace(dto.PlateNo)) ord.PlateNo = dto.PlateNo.Trim();
        if (!string.IsNullOrWhiteSpace(dto.DriverName)) ord.DriverName = dto.DriverName.Trim();
        if (!string.IsNullOrWhiteSpace(dto.DriverPhone)) ord.DriverPhone = dto.DriverPhone.Trim();
        if (dto.Remark != null) ord.Remark = dto.Remark.Trim();

        if (!string.IsNullOrWhiteSpace(dto.RearrangeType))
        {
            ord.RearrangeType = dto.RearrangeType.Trim().ToUpperInvariant() switch
            {
                "URGENT" => StorageRearrangeType.Urgent,
                "SHOWROOM" => StorageRearrangeType.Showroom,
                "MAINTENANCE" => StorageRearrangeType.Maintenance,
                _ => StorageRearrangeType.Normal
            };
        }

        await db.SaveChangesAsync();

        return new
        {
            message = "Cập nhật lệnh điều chuyển kho thành công.",
            rearrangeNo = ord.RearrangeNo,
            storageNameFrom = ord.StorageNameFrom,
            storageNameTo = ord.StorageNameTo,
            rearrangeType = ord.RearrangeType.ToString(),
            status = ord.Status.ToString()
        };
    }

    public async Task<object?> Approve1Async(string rearrangeNo, Approve1RearrangeDto? dto)
    {
        var ord = await db.StorageRearranges
            .Include(x => x.Details)
            .FirstOrDefaultAsync(x => x.OrgId == Org && x.RearrangeNo == rearrangeNo.Trim().ToUpperInvariant());

        if (ord is null) return null;

        if (ord.Status != StorageRearrangeStatus.Pending)
            throw new InvalidOperationException($"Chỉ có thể duyệt cấp 1 (Approve1HQ) khi lệnh đang ở trạng thái 'Pending'. Hiện tại: {ord.Status}.");

        ord.Status = StorageRearrangeStatus.Approved1;
        ord.Approved1By = string.IsNullOrWhiteSpace(dto?.ApprovedBy) ? "NPP_DISPATCH_LEAD" : dto.ApprovedBy.Trim();
        ord.Approved1At = DateTime.Now;

        if (!string.IsNullOrWhiteSpace(dto?.Remark))
        {
            ord.Remark = string.IsNullOrWhiteSpace(ord.Remark)
                ? $"[Approve1] {dto.Remark.Trim()}"
                : $"{ord.Remark} | [Approve1] {dto.Remark.Trim()}";
        }

        foreach (var d in ord.Details)
        {
            if (d.Status == StorageRearrangeDtlStatus.Pending)
                d.Status = StorageRearrangeDtlStatus.Approved1;
        }

        await db.SaveChangesAsync();

        return new
        {
            message = "Phê duyệt cấp 1 (kế hoạch điều chuyển) thành công.",
            rearrangeNo = ord.RearrangeNo,
            status = ord.Status.ToString(),
            approved1By = ord.Approved1By,
            approved1At = ord.Approved1At,
            totalCars = ord.TotalCars
        };
    }

    public async Task<object?> Approve2Async(string rearrangeNo, Approve2RearrangeDto? dto)
    {
        var ord = await db.StorageRearranges
            .Include(x => x.Details)
            .FirstOrDefaultAsync(x => x.OrgId == Org && x.RearrangeNo == rearrangeNo.Trim().ToUpperInvariant());

        if (ord is null) return null;

        if (ord.Status != StorageRearrangeStatus.Approved1)
            throw new InvalidOperationException($"Chỉ có thể duyệt cấp 2 (Approve2HQ - Hoàn tất xuất nhập kho) khi lệnh đã được duyệt cấp 1 'Approved1'. Hiện tại: {ord.Status}.");

        ord.Status = StorageRearrangeStatus.Approved2;
        ord.Approved2By = string.IsNullOrWhiteSpace(dto?.ApprovedBy) ? "NPP_WAREHOUSE_DIRECTOR" : dto.ApprovedBy.Trim();
        ord.Approved2At = DateTime.Now;

        if (!string.IsNullOrWhiteSpace(dto?.Remark))
        {
            ord.Remark = string.IsNullOrWhiteSpace(ord.Remark)
                ? $"[Approve2] {dto.Remark.Trim()}"
                : $"{ord.Remark} | [Approve2] {dto.Remark.Trim()}";
        }

        var actualOut = dto?.ActualOutDate ?? DateTime.Today;
        var actualIn = dto?.ActualInDate ?? DateTime.Today.AddDays(1);

        foreach (var d in ord.Details)
        {
            d.Status = StorageRearrangeDtlStatus.Completed;
            d.ActualOutDate ??= actualOut;
            d.ActualInDate ??= actualIn;
            d.ConfirmDate ??= DateTime.Now;
            d.ConfirmBy ??= ord.Approved2By;
        }

        await db.SaveChangesAsync();

        return new
        {
            message = "Phê duyệt cấp 2 và hoàn tất xuất nhập kho điều chuyển xe thành công.",
            rearrangeNo = ord.RearrangeNo,
            status = ord.Status.ToString(),
            approved2By = ord.Approved2By,
            approved2At = ord.Approved2At,
            totalCars = ord.TotalCars,
            storageCodeTo = ord.StorageCodeTo
        };
    }

    public async Task<object?> RejectAsync(string rearrangeNo, RejectRearrangeDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Reason))
            throw new InvalidOperationException("Bắt buộc phải cung cấp lý do từ chối (Reason).");

        var ord = await db.StorageRearranges
            .Include(x => x.Details)
            .FirstOrDefaultAsync(x => x.OrgId == Org && x.RearrangeNo == rearrangeNo.Trim().ToUpperInvariant());

        if (ord is null) return null;

        if (ord.Status != StorageRearrangeStatus.Pending && ord.Status != StorageRearrangeStatus.Approved1)
            throw new InvalidOperationException($"Chỉ có thể từ chối duyệt khi lệnh đang ở trạng thái 'Pending' hoặc 'Approved1'. Hiện tại: {ord.Status}.");

        ord.Status = StorageRearrangeStatus.Rejected;
        ord.RejectReason = dto.Reason.Trim();

        foreach (var d in ord.Details)
        {
            d.Status = StorageRearrangeDtlStatus.Rejected;
        }

        await db.SaveChangesAsync();

        return new
        {
            message = "Từ chối duyệt lệnh điều chuyển kho thành công.",
            rearrangeNo = ord.RearrangeNo,
            status = ord.Status.ToString(),
            rejectReason = ord.RejectReason
        };
    }

    public async Task<object?> CancelAsync(string rearrangeNo, CancelRearrangeDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Reason))
            throw new InvalidOperationException("Bắt buộc phải cung cấp lý do hủy lệnh (Reason).");

        var ord = await db.StorageRearranges
            .Include(x => x.Details)
            .FirstOrDefaultAsync(x => x.OrgId == Org && x.RearrangeNo == rearrangeNo.Trim().ToUpperInvariant());

        if (ord is null) return null;

        if (ord.Status != StorageRearrangeStatus.Pending && ord.Status != StorageRearrangeStatus.Approved1)
            throw new InvalidOperationException($"Chỉ có thể hủy lệnh khi chưa xuất kho hoàn tất (trạng thái 'Pending' hoặc 'Approved1'). Hiện tại: {ord.Status}.");

        ord.Status = StorageRearrangeStatus.Cancelled;
        ord.CancelReason = dto.Reason.Trim();
        ord.CancelledBy = string.IsNullOrWhiteSpace(dto.CancelledBy) ? "NPP_OPERATOR" : dto.CancelledBy.Trim();
        ord.CancelledAt = DateTime.Now;

        foreach (var d in ord.Details)
        {
            d.Status = StorageRearrangeDtlStatus.Rejected;
        }

        await db.SaveChangesAsync();

        return new
        {
            message = "Hủy lệnh điều chuyển kho thành công.",
            rearrangeNo = ord.RearrangeNo,
            status = ord.Status.ToString(),
            cancelReason = ord.CancelReason,
            cancelledBy = ord.CancelledBy,
            cancelledAt = ord.CancelledAt
        };
    }

    public async Task<object?> DeleteAsync(string rearrangeNo)
    {
        var ord = await db.StorageRearranges
            .Include(x => x.Details)
            .FirstOrDefaultAsync(x => x.OrgId == Org && x.RearrangeNo == rearrangeNo.Trim().ToUpperInvariant());

        if (ord is null) return null;

        if (ord.Status != StorageRearrangeStatus.Pending)
            throw new InvalidOperationException($"Chỉ có thể xóa lệnh nháp khi còn ở trạng thái 'Pending'. Hiện tại: {ord.Status}.");

        db.StorageRearranges.Remove(ord);
        await db.SaveChangesAsync();

        return new
        {
            message = "Đã xóa lệnh điều chuyển kho nháp thành công.",
            rearrangeNo = ord.RearrangeNo
        };
    }

    public async Task<object?> UpdateDetailAsync(string rearrangeNo, string vin, UpdateRearrangeDetailDto dto)
    {
        var ord = await db.StorageRearranges
            .Include(x => x.Details)
            .FirstOrDefaultAsync(x => x.OrgId == Org && x.RearrangeNo == rearrangeNo.Trim().ToUpperInvariant());

        if (ord is null) return null;

        if (ord.Status == StorageRearrangeStatus.Cancelled || ord.Status == StorageRearrangeStatus.Rejected)
            throw new InvalidOperationException($"Không thể cập nhật chi tiết xe khi lệnh đã ở trạng thái '{ord.Status}'.");

        var d = ord.Details.FirstOrDefault(x => string.Equals(x.Vin, vin.Trim(), StringComparison.OrdinalIgnoreCase));
        if (d is null)
            throw new InvalidOperationException($"Không tìm thấy xe có số khung VIN '{vin}' trong lệnh điều chuyển {ord.RearrangeNo}.");

        if (dto.ExpectedEndDate.HasValue)
        {
            if (d.ExpectedStartDate.HasValue && dto.ExpectedEndDate < d.ExpectedStartDate)
                throw new InvalidOperationException("Ngày dự kiến nhận xe không được nhỏ hơn ngày dự kiến xuất xe.");
            d.ExpectedEndDate = dto.ExpectedEndDate;
        }

        if (dto.ActualOutDate.HasValue) d.ActualOutDate = dto.ActualOutDate;
        if (dto.ActualInDate.HasValue) d.ActualInDate = dto.ActualInDate;

        if (!string.IsNullOrWhiteSpace(dto.ConfirmBy))
        {
            d.ConfirmBy = dto.ConfirmBy.Trim();
            d.ConfirmDate = DateTime.Now;
        }

        if (dto.Remark != null) d.Remark = dto.Remark.Trim();

        await db.SaveChangesAsync();

        return new
        {
            message = "Cập nhật chi tiết xe điều chuyển thành công.",
            rearrangeNo = ord.RearrangeNo,
            vin = d.Vin,
            expectedEndDate = d.ExpectedEndDate,
            actualOutDate = d.ActualOutDate,
            actualInDate = d.ActualInDate,
            confirmBy = d.ConfirmBy,
            confirmDate = d.ConfirmDate,
            remark = d.Remark
        };
    }

    public async Task<object?> AddCarAsync(string rearrangeNo, AddRearrangeDetailDto dto)
    {
        var ord = await db.StorageRearranges
            .Include(x => x.Details)
            .FirstOrDefaultAsync(x => x.OrgId == Org && x.RearrangeNo == rearrangeNo.Trim().ToUpperInvariant());

        if (ord is null) return null;

        if (ord.Status != StorageRearrangeStatus.Pending)
            throw new InvalidOperationException($"Chỉ có thể thêm xe vào lệnh khi ở trạng thái 'Pending'. Hiện tại: {ord.Status}.");

        if (string.IsNullOrWhiteSpace(dto.CarId))
            throw new InvalidOperationException("CarId không được để trống.");
        if (string.IsNullOrWhiteSpace(dto.Vin))
            throw new InvalidOperationException("VIN không được để trống.");
        if (string.IsNullOrWhiteSpace(dto.Model))
            throw new InvalidOperationException("Model xe không được để trống.");

        if (ord.Details.Any(x => string.Equals(x.Vin, dto.Vin.Trim(), StringComparison.OrdinalIgnoreCase)))
            throw new InvalidOperationException($"Số khung VIN '{dto.Vin}' đã tồn tại trong lệnh điều chuyển này.");

        if (ord.Details.Any(x => string.Equals(x.CarId, dto.CarId.Trim(), StringComparison.OrdinalIgnoreCase)))
            throw new InvalidOperationException($"Mã xe CarId '{dto.CarId}' đã tồn tại trong lệnh điều chuyển này.");

        if (dto.ExpectedStartDate.HasValue && dto.ExpectedEndDate.HasValue && dto.ExpectedStartDate > dto.ExpectedEndDate)
            throw new InvalidOperationException("Ngày dự kiến xuất kho không được lớn hơn ngày dự kiến nhận xe.");

        var d = new StorageRearrangeDetail
        {
            RearrangeNo = ord.RearrangeNo,
            CarId = dto.CarId.Trim(),
            Vin = dto.Vin.Trim().ToUpperInvariant(),
            Model = dto.Model.Trim(),
            SpecCode = dto.SpecCode?.Trim(),
            ColorCode = dto.ColorCode?.Trim(),
            EngineNo = dto.EngineNo?.Trim(),
            StorageCodeFrom = ord.StorageCodeFrom,
            StorageCodeTo = ord.StorageCodeTo,
            ExpectedStartDate = dto.ExpectedStartDate ?? DateTime.Today,
            ExpectedEndDate = dto.ExpectedEndDate ?? DateTime.Today.AddDays(3),
            Status = StorageRearrangeDtlStatus.Pending,
            Remark = dto.Remark?.Trim()
        };

        ord.Details.Add(d);
        ord.TotalCars = ord.Details.Count;

        await db.SaveChangesAsync();

        return new
        {
            message = "Thêm xe vào lệnh điều chuyển kho thành công.",
            rearrangeNo = ord.RearrangeNo,
            vin = d.Vin,
            carId = d.CarId,
            totalCars = ord.TotalCars
        };
    }

    public async Task<object?> RemoveCarAsync(string rearrangeNo, string vin)
    {
        var ord = await db.StorageRearranges
            .Include(x => x.Details)
            .FirstOrDefaultAsync(x => x.OrgId == Org && x.RearrangeNo == rearrangeNo.Trim().ToUpperInvariant());

        if (ord is null) return null;

        if (ord.Status != StorageRearrangeStatus.Pending)
            throw new InvalidOperationException($"Chỉ có thể gỡ xe khỏi lệnh khi ở trạng thái 'Pending'. Hiện tại: {ord.Status}.");

        var d = ord.Details.FirstOrDefault(x => string.Equals(x.Vin, vin.Trim(), StringComparison.OrdinalIgnoreCase));
        if (d is null)
            throw new InvalidOperationException($"Không tìm thấy xe có số VIN '{vin}' trong lệnh điều chuyển {ord.RearrangeNo}.");

        if (ord.Details.Count <= 1)
            throw new InvalidOperationException("Lệnh điều chuyển kho phải có ít nhất 1 xe. Không thể gỡ dòng xe cuối cùng (hãy dùng chức năng Xóa lệnh nếu muốn hủy hoàn toàn).");

        ord.Details.Remove(d);
        ord.TotalCars = ord.Details.Count;

        await db.SaveChangesAsync();

        return new
        {
            message = "Gỡ xe khỏi lệnh điều chuyển kho thành công.",
            rearrangeNo = ord.RearrangeNo,
            removedVin = vin.Trim().ToUpperInvariant(),
            totalCars = ord.TotalCars
        };
    }

    public async Task<object> StatsAsync()
    {
        var list = await db.StorageRearranges
            .Include(x => x.Details)
            .Where(x => x.OrgId == Org)
            .ToListAsync();

        var totalOrders = list.Count;
        var pending = list.Count(x => x.Status == StorageRearrangeStatus.Pending);
        var approved1 = list.Count(x => x.Status == StorageRearrangeStatus.Approved1);
        var approved2 = list.Count(x => x.Status == StorageRearrangeStatus.Approved2);
        var rejected = list.Count(x => x.Status == StorageRearrangeStatus.Rejected);
        var cancelled = list.Count(x => x.Status == StorageRearrangeStatus.Cancelled);

        var totalVehicles = list.Sum(x => x.TotalCars);
        var completedVehicles = list.Where(x => x.Status == StorageRearrangeStatus.Approved2).Sum(x => x.TotalCars);

        var bySourceWarehouse = list
            .GroupBy(x => x.StorageCodeFrom)
            .Select(g => new { storageCode = g.Key, storageName = g.First().StorageNameFrom, ordersCount = g.Count(), carsCount = g.Sum(x => x.TotalCars) })
            .OrderByDescending(x => x.carsCount)
            .ToList();

        var byTargetWarehouse = list
            .GroupBy(x => x.StorageCodeTo)
            .Select(g => new { storageCode = g.Key, storageName = g.First().StorageNameTo, ordersCount = g.Count(), carsCount = g.Sum(x => x.TotalCars) })
            .OrderByDescending(x => x.carsCount)
            .ToList();

        return new
        {
            totalOrders,
            pending,
            approved1,
            approved2,
            rejected,
            cancelled,
            totalVehicles,
            completedVehicles,
            bySourceWarehouse,
            byTargetWarehouse
        };
    }
}
