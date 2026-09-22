using Microsoft.EntityFrameworkCore;
using MiniSales.Data;
using MiniSales.Models;

namespace MiniSales.Services;

public record CreateCarRetrieveDetailDto(
    string CarId,
    string Vin,
    string Model,
    string? DeliveryOrderNo,
    string? StorageCode,
    DateTime? ExpectedStartDate,
    DateTime? ExpectedEndDate,
    string? EngineNo,
    string? Color,
    string? Remark
);

public record CreateCarRetrieveDto(
    string? RetrieveOrderNo,
    string DealerCode,
    string? DealerName,
    string? StorageCode,
    string? StorageName,
    string? Remark,
    List<CreateCarRetrieveDetailDto>? Details
);

public record ApproveCarRetrieveDto(
    string? ApprovedBy,
    string? Remark
);

public record RejectCarRetrieveDto(
    string Reason,
    string? RejectedBy
);

public record CancelCarRetrieveDto(
    string? Reason
);

public record CompleteCarRetrieveDto(
    DateTime? ActualOutDate,
    DateTime? ActualEndDate,
    string? Remark
);

public record AddCarRetrieveDetailDto(
    string CarId,
    string Vin,
    string Model,
    string? DeliveryOrderNo,
    string? StorageCode,
    DateTime? ExpectedStartDate,
    DateTime? ExpectedEndDate,
    string? EngineNo,
    string? Color,
    string? Remark
);

public interface ICarRetrieveService
{
    Task<object> CreateAsync(CreateCarRetrieveDto dto);
    Task<object> ListAsync(string? status, string? dealer, string? vin, string? carId);
    Task<object?> DetailAsync(string retrieveOrderNo);
    Task<object?> ApproveAsync(string retrieveOrderNo, ApproveCarRetrieveDto? dto);
    Task<object?> RejectAsync(string retrieveOrderNo, RejectCarRetrieveDto dto);
    Task<object?> CancelAsync(string retrieveOrderNo, CancelCarRetrieveDto? dto);
    Task<object?> CompleteAsync(string retrieveOrderNo, CompleteCarRetrieveDto? dto);
    Task<object?> AddCarAsync(string retrieveOrderNo, AddCarRetrieveDetailDto dto);
    Task<object?> RemoveCarAsync(string retrieveOrderNo, long detailId);
    Task<object> StatsAsync();
}

public sealed class CarRetrieveService(AppDbContext db, ITenantContext tenant) : ICarRetrieveService
{
    private Guid Org => tenant.OrgId;

    public async Task<object> CreateAsync(CreateCarRetrieveDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.DealerCode))
            throw new InvalidOperationException("DealerCode (mã đại lý bị thu hồi xe) không được để trống.");

        if (dto.Details is null || dto.Details.Count == 0)
            throw new InvalidOperationException("Lệnh thu hồi xe phải có ít nhất 1 chi tiết xe.");

        // Chống trùng xe trong danh sách
        var carIdSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var vinSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var d in dto.Details)
        {
            if (string.IsNullOrWhiteSpace(d.CarId))
                throw new InvalidOperationException("Mỗi chi tiết xe bắt buộc phải có CarId.");
            if (string.IsNullOrWhiteSpace(d.Vin))
                throw new InvalidOperationException("Mỗi chi tiết xe bắt buộc phải có số VIN.");

            if (!carIdSet.Add(d.CarId.Trim()))
                throw new InvalidOperationException($"Trùng mã xe CarId trong danh sách thu hồi: {d.CarId}");
            if (!vinSet.Add(d.Vin.Trim()))
                throw new InvalidOperationException($"Trùng số khung VIN trong danh sách thu hồi: {d.Vin}");

            if (d.ExpectedStartDate.HasValue && d.ExpectedEndDate.HasValue && d.ExpectedStartDate > d.ExpectedEndDate)
                throw new InvalidOperationException($"Ngày dự kiến bắt đầu ({d.ExpectedStartDate:yyyy-MM-dd}) không được lớn hơn ngày dự kiến kết thúc ({d.ExpectedEndDate:yyyy-MM-dd}) cho xe {d.Vin}.");
        }

        // Sinh số lệnh thu hồi nếu không truyền vào
        string orderNo;
        if (!string.IsNullOrWhiteSpace(dto.RetrieveOrderNo))
        {
            orderNo = dto.RetrieveOrderNo.Trim().ToUpperInvariant();
        }
        else
        {
            var datePrefix = DateTime.Now.ToString("yyMMdd");
            var countToday = await db.CarRetrieveOrders
                .CountAsync(x => x.OrgId == Org && x.RetrieveOrderNo.StartsWith("RO" + datePrefix));
            orderNo = $"RO{datePrefix}{(countToday + 1):D4}";
        }

        if (await db.CarRetrieveOrders.AnyAsync(x => x.OrgId == Org && x.RetrieveOrderNo == orderNo))
            throw new InvalidOperationException($"Số lệnh thu hồi xe {orderNo} đã tồn tại trong hệ thống.");

        var order = new CarRetrieveOrder
        {
            OrgId = Org,
            RetrieveOrderNo = orderNo,
            DealerCode = dto.DealerCode.Trim().ToUpperInvariant(),
            DealerName = string.IsNullOrWhiteSpace(dto.DealerName) ? dto.DealerCode.Trim() : dto.DealerName.Trim(),
            StorageCode = string.IsNullOrWhiteSpace(dto.StorageCode) ? "OTHER" : dto.StorageCode.Trim().ToUpperInvariant(),
            StorageName = string.IsNullOrWhiteSpace(dto.StorageName) ? "Kho Trung Tâm Phân Phối" : dto.StorageName.Trim(),
            Status = CarRetrieveStatus.Pending,
            TotalCars = dto.Details.Count,
            Remark = dto.Remark?.Trim(),
            CreatedBy = "NPP_DISPATCHER",
            CreatedAt = DateTime.Now,
            Details = dto.Details.Select(d => new CarRetrieveOrderDetail
            {
                CarId = d.CarId.Trim(),
                Vin = d.Vin.Trim().ToUpperInvariant(),
                Model = d.Model.Trim(),
                DeliveryOrderNo = d.DeliveryOrderNo?.Trim(),
                StorageCode = string.IsNullOrWhiteSpace(d.StorageCode) ? (string.IsNullOrWhiteSpace(dto.StorageCode) ? "OTHER" : dto.StorageCode.Trim().ToUpperInvariant()) : d.StorageCode.Trim().ToUpperInvariant(),
                Status = CarRetrieveDtlStatus.Pending,
                ExpectedStartDate = d.ExpectedStartDate,
                ExpectedEndDate = d.ExpectedEndDate,
                EngineNo = d.EngineNo?.Trim(),
                Color = d.Color?.Trim(),
                Remark = d.Remark?.Trim()
            }).ToList()
        };

        db.CarRetrieveOrders.Add(order);
        await db.SaveChangesAsync();

        return new
        {
            id = order.Id,
            retrieveOrderNo = order.RetrieveOrderNo,
            dealerCode = order.DealerCode,
            dealerName = order.DealerName,
            status = order.Status.ToString(),
            totalCars = order.TotalCars,
            createdAt = order.CreatedAt
        };
    }

    public async Task<object> ListAsync(string? status, string? dealer, string? vin, string? carId)
    {
        var q = db.CarRetrieveOrders
            .Include(x => x.Details)
            .Where(x => x.OrgId == Org)
            .AsNoTracking();

        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<CarRetrieveStatus>(status, true, out var st))
            q = q.Where(x => x.Status == st);

        if (!string.IsNullOrWhiteSpace(dealer))
        {
            var d = dealer.Trim();
            q = q.Where(x => EF.Functions.Like(x.DealerCode, $"%{d}%") || EF.Functions.Like(x.DealerName, $"%{d}%"));
        }

        if (!string.IsNullOrWhiteSpace(vin))
        {
            var v = vin.Trim();
            q = q.Where(x => x.Details.Any(dt => EF.Functions.Like(dt.Vin, $"%{v}%")));
        }

        if (!string.IsNullOrWhiteSpace(carId))
        {
            var cid = carId.Trim();
            q = q.Where(x => x.Details.Any(dt => EF.Functions.Like(dt.CarId, $"%{cid}%")));
        }

        var list = await q.OrderByDescending(x => x.CreatedAt).ToListAsync();

        return list.Select(x => new
        {
            x.Id,
            x.RetrieveOrderNo,
            x.DealerCode,
            x.DealerName,
            Status = x.Status.ToString(),
            x.StorageCode,
            x.StorageName,
            x.TotalCars,
            x.Remark,
            x.RejectReason,
            x.CreatedBy,
            x.CreatedAt,
            x.ApprovedBy,
            x.ApprovedAt,
            x.CompletedAt,
            x.CancelledAt,
            Cars = x.Details.Select(d => new
            {
                d.Id,
                d.CarId,
                d.Vin,
                d.Model,
                d.DeliveryOrderNo,
                d.StorageCode,
                Status = d.Status.ToString(),
                d.ExpectedStartDate,
                d.ExpectedEndDate,
                d.ActualOutDate,
                d.ActualEndDate,
                d.EngineNo,
                d.Color,
                d.Remark
            })
        });
    }

    public async Task<object?> DetailAsync(string retrieveOrderNo)
    {
        var order = await db.CarRetrieveOrders
            .Include(x => x.Details)
            .FirstOrDefaultAsync(x => x.OrgId == Org && x.RetrieveOrderNo == retrieveOrderNo);

        if (order is null) return null;

        return new
        {
            order.Id,
            order.RetrieveOrderNo,
            order.DealerCode,
            order.DealerName,
            Status = order.Status.ToString(),
            order.StorageCode,
            order.StorageName,
            order.TotalCars,
            order.Remark,
            order.RejectReason,
            order.CreatedBy,
            order.CreatedAt,
            order.ApprovedBy,
            order.ApprovedAt,
            order.CompletedAt,
            order.CancelledAt,
            Details = order.Details.Select(d => new
            {
                d.Id,
                d.CarId,
                d.Vin,
                d.Model,
                d.DeliveryOrderNo,
                d.StorageCode,
                Status = d.Status.ToString(),
                d.ExpectedStartDate,
                d.ExpectedEndDate,
                d.ActualOutDate,
                d.ActualEndDate,
                d.EngineNo,
                d.Color,
                d.Remark
            })
        };
    }

    public async Task<object?> ApproveAsync(string retrieveOrderNo, ApproveCarRetrieveDto? dto)
    {
        var order = await db.CarRetrieveOrders
            .Include(x => x.Details)
            .FirstOrDefaultAsync(x => x.OrgId == Org && x.RetrieveOrderNo == retrieveOrderNo);

        if (order is null) return null;

        if (order.Status != CarRetrieveStatus.Pending)
            throw new InvalidOperationException($"Lệnh thu hồi {retrieveOrderNo} đang ở trạng thái {order.Status}, chỉ có thể duyệt khi ở trạng thái Chờ duyệt (Pending).");

        order.Status = CarRetrieveStatus.Approved;
        order.ApprovedAt = DateTime.Now;
        order.ApprovedBy = string.IsNullOrWhiteSpace(dto?.ApprovedBy) ? "NPP_HQ_APPROVER" : dto.ApprovedBy.Trim();
        if (!string.IsNullOrWhiteSpace(dto?.Remark))
            order.Remark = string.IsNullOrWhiteSpace(order.Remark) ? dto.Remark.Trim() : $"{order.Remark} | {dto.Remark.Trim()}";

        // Đồng bộ trạng thái duyệt cho tất cả dòng xe
        foreach (var d in order.Details)
        {
            d.Status = CarRetrieveDtlStatus.Approved;
        }

        await db.SaveChangesAsync();

        return new
        {
            order.RetrieveOrderNo,
            status = order.Status.ToString(),
            order.ApprovedBy,
            order.ApprovedAt,
            totalCars = order.TotalCars,
            message = $"Lệnh thu hồi xe {retrieveOrderNo} đã được NPP phê duyệt thành công."
        };
    }

    public async Task<object?> RejectAsync(string retrieveOrderNo, RejectCarRetrieveDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Reason))
            throw new InvalidOperationException("Bắt buộc phải có lý do từ chối lệnh thu hồi xe.");

        var order = await db.CarRetrieveOrders
            .Include(x => x.Details)
            .FirstOrDefaultAsync(x => x.OrgId == Org && x.RetrieveOrderNo == retrieveOrderNo);

        if (order is null) return null;

        if (order.Status != CarRetrieveStatus.Pending)
            throw new InvalidOperationException($"Lệnh thu hồi {retrieveOrderNo} đang ở trạng thái {order.Status}, chỉ có thể từ chối khi ở trạng thái Chờ duyệt (Pending).");

        order.Status = CarRetrieveStatus.Rejected;
        order.RejectReason = dto.Reason.Trim();
        order.ApprovedAt = DateTime.Now;
        order.ApprovedBy = string.IsNullOrWhiteSpace(dto.RejectedBy) ? "NPP_HQ_APPROVER" : dto.RejectedBy.Trim();

        foreach (var d in order.Details)
        {
            d.Status = CarRetrieveDtlStatus.Rejected;
        }

        await db.SaveChangesAsync();

        return new
        {
            order.RetrieveOrderNo,
            status = order.Status.ToString(),
            order.RejectReason,
            order.ApprovedBy,
            order.ApprovedAt,
            message = $"Lệnh thu hồi xe {retrieveOrderNo} đã bị từ chối."
        };
    }

    public async Task<object?> CancelAsync(string retrieveOrderNo, CancelCarRetrieveDto? dto)
    {
        var order = await db.CarRetrieveOrders
            .Include(x => x.Details)
            .FirstOrDefaultAsync(x => x.OrgId == Org && x.RetrieveOrderNo == retrieveOrderNo);

        if (order is null) return null;

        if (order.Status != CarRetrieveStatus.Pending)
            throw new InvalidOperationException($"Chỉ có thể hủy lệnh thu hồi xe khi ở trạng thái Chờ duyệt (Pending). Trạng thái hiện tại: {order.Status}.");

        order.Status = CarRetrieveStatus.Cancelled;
        order.CancelledAt = DateTime.Now;
        if (!string.IsNullOrWhiteSpace(dto?.Reason))
            order.Remark = string.IsNullOrWhiteSpace(order.Remark) ? dto.Reason.Trim() : $"{order.Remark} | Hủy: {dto.Reason.Trim()}";

        await db.SaveChangesAsync();

        return new
        {
            order.RetrieveOrderNo,
            status = order.Status.ToString(),
            order.CancelledAt,
            message = $"Lệnh thu hồi xe {retrieveOrderNo} đã được hủy."
        };
    }

    public async Task<object?> CompleteAsync(string retrieveOrderNo, CompleteCarRetrieveDto? dto)
    {
        var order = await db.CarRetrieveOrders
            .Include(x => x.Details)
            .FirstOrDefaultAsync(x => x.OrgId == Org && x.RetrieveOrderNo == retrieveOrderNo);

        if (order is null) return null;

        if (order.Status != CarRetrieveStatus.Approved)
            throw new InvalidOperationException($"Chỉ có thể xác nhận hoàn tất thu hồi xe về kho khi lệnh đã được duyệt (Approved). Trạng thái hiện tại: {order.Status}.");

        var outDate = dto?.ActualOutDate ?? DateTime.Today;
        var inDate = dto?.ActualEndDate ?? DateTime.Today;

        order.Status = CarRetrieveStatus.Completed;
        order.CompletedAt = DateTime.Now;
        if (!string.IsNullOrWhiteSpace(dto?.Remark))
            order.Remark = string.IsNullOrWhiteSpace(order.Remark) ? dto.Remark.Trim() : $"{order.Remark} | {dto.Remark.Trim()}";

        foreach (var d in order.Details)
        {
            d.Status = CarRetrieveDtlStatus.Completed;
            d.ActualOutDate ??= outDate;
            d.ActualEndDate ??= inDate;
        }

        await db.SaveChangesAsync();

        return new
        {
            order.RetrieveOrderNo,
            status = order.Status.ToString(),
            order.CompletedAt,
            totalCars = order.TotalCars,
            message = $"Đã xác nhận nhập kho NPP hoàn tất {order.TotalCars} xe theo lệnh {retrieveOrderNo}."
        };
    }

    public async Task<object?> AddCarAsync(string retrieveOrderNo, AddCarRetrieveDetailDto dto)
    {
        var order = await db.CarRetrieveOrders
            .Include(x => x.Details)
            .FirstOrDefaultAsync(x => x.OrgId == Org && x.RetrieveOrderNo == retrieveOrderNo);

        if (order is null) return null;

        if (order.Status != CarRetrieveStatus.Pending)
            throw new InvalidOperationException($"Chỉ có thể thêm xe vào lệnh thu hồi khi lệnh đang Chờ duyệt (Pending).");

        if (string.IsNullOrWhiteSpace(dto.CarId) || string.IsNullOrWhiteSpace(dto.Vin))
            throw new InvalidOperationException("Bắt buộc phải có CarId và VIN.");

        if (order.Details.Any(x => string.Equals(x.CarId, dto.CarId.Trim(), StringComparison.OrdinalIgnoreCase)))
            throw new InvalidOperationException($"Xe CarId {dto.CarId} đã có trong lệnh thu hồi.");

        if (order.Details.Any(x => string.Equals(x.Vin, dto.Vin.Trim(), StringComparison.OrdinalIgnoreCase)))
            throw new InvalidOperationException($"Số khung VIN {dto.Vin} đã có trong lệnh thu hồi.");

        if (dto.ExpectedStartDate.HasValue && dto.ExpectedEndDate.HasValue && dto.ExpectedStartDate > dto.ExpectedEndDate)
            throw new InvalidOperationException("Ngày dự kiến bắt đầu không được lớn hơn ngày dự kiến kết thúc.");

        var item = new CarRetrieveOrderDetail
        {
            RetrieveOrderId = order.Id,
            CarId = dto.CarId.Trim(),
            Vin = dto.Vin.Trim().ToUpperInvariant(),
            Model = dto.Model.Trim(),
            DeliveryOrderNo = dto.DeliveryOrderNo?.Trim(),
            StorageCode = string.IsNullOrWhiteSpace(dto.StorageCode) ? order.StorageCode : dto.StorageCode.Trim().ToUpperInvariant(),
            Status = CarRetrieveDtlStatus.Pending,
            ExpectedStartDate = dto.ExpectedStartDate,
            ExpectedEndDate = dto.ExpectedEndDate,
            EngineNo = dto.EngineNo?.Trim(),
            Color = dto.Color?.Trim(),
            Remark = dto.Remark?.Trim()
        };

        order.Details.Add(item);
        order.TotalCars = order.Details.Count;
        await db.SaveChangesAsync();

        return new
        {
            detailId = item.Id,
            order.RetrieveOrderNo,
            item.CarId,
            item.Vin,
            item.Model,
            order.TotalCars
        };
    }

    public async Task<object?> RemoveCarAsync(string retrieveOrderNo, long detailId)
    {
        var order = await db.CarRetrieveOrders
            .Include(x => x.Details)
            .FirstOrDefaultAsync(x => x.OrgId == Org && x.RetrieveOrderNo == retrieveOrderNo);

        if (order is null) return null;

        if (order.Status != CarRetrieveStatus.Pending)
            throw new InvalidOperationException("Chỉ có thể xóa xe khỏi lệnh thu hồi khi lệnh đang Chờ duyệt (Pending).");

        var detail = order.Details.FirstOrDefault(x => x.Id == detailId);
        if (detail is null) return null;

        order.Details.Remove(detail);
        db.CarRetrieveOrderDetails.Remove(detail);

        // Chuẩn parity DMS.Sales Sto_CarRetrieveDetail_DelX: nếu xóa hết chi tiết xe thì xóa luôn lệnh thu hồi
        if (order.Details.Count == 0)
        {
            db.CarRetrieveOrders.Remove(order);
            await db.SaveChangesAsync();
            return new
            {
                order.RetrieveOrderNo,
                removedDetailId = detailId,
                totalCars = 0,
                orderDeleted = true,
                message = $"Đã xóa dòng xe cuối cùng. Lệnh thu hồi {retrieveOrderNo} đã được tự động giải phóng/xóa hoàn toàn."
            };
        }

        order.TotalCars = order.Details.Count;
        await db.SaveChangesAsync();

        return new
        {
            order.RetrieveOrderNo,
            removedDetailId = detailId,
            order.TotalCars,
            orderDeleted = false,
            message = $"Đã xóa dòng xe {detail.Vin} khỏi lệnh {retrieveOrderNo}."
        };
    }

    public async Task<object> StatsAsync()
    {
        var orders = await db.CarRetrieveOrders
            .Where(x => x.OrgId == Org)
            .AsNoTracking()
            .ToListAsync();

        return new
        {
            TotalOrders = orders.Count,
            TotalCars = orders.Sum(x => x.TotalCars),
            PendingOrders = orders.Count(x => x.Status == CarRetrieveStatus.Pending),
            ApprovedOrders = orders.Count(x => x.Status == CarRetrieveStatus.Approved),
            CompletedOrders = orders.Count(x => x.Status == CarRetrieveStatus.Completed),
            RejectedOrders = orders.Count(x => x.Status == CarRetrieveStatus.Rejected),
            CancelledOrders = orders.Count(x => x.Status == CarRetrieveStatus.Cancelled)
        };
    }
}
