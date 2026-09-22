using Microsoft.EntityFrameworkCore;
using MiniSales.Data;
using MiniSales.Models;

namespace MiniSales.Services;

public record CreateTransportRequestDetailDto(
    string? TranspReqType,
    string RefOrdNo,
    string CarId,
    string Vin,
    string Model,
    string? SpecCode,
    string? ColorCode,
    string? EngineNo,
    string? StorageCodeFrom,
    string? StorageCodeTo,
    string? Remark
);

public record CreateTransportRequestDto(
    string? TranspReqNo,
    string DealerCode,
    string? DealerName,
    string TransporterCode,
    string? TransporterName,
    string? TransportContractNo,
    string? TranspReqType,
    string? PlateNo,
    string? DriverName,
    string? DriverPhone,
    DateTime? ExpectedStartDate,
    DateTime? ExpectedEndDate,
    string? Remark,
    List<CreateTransportRequestDetailDto>? Details
);

public record UpdateTransportRequestDto(
    string? DealerName,
    string? TransporterName,
    string? TransportContractNo,
    string? PlateNo,
    string? DriverName,
    string? DriverPhone,
    DateTime? ExpectedStartDate,
    DateTime? ExpectedEndDate,
    string? Remark
);

public record ApproveTransportRequestDto(
    string? ApprovedBy,
    string? Remark
);

public record RejectTransportRequestDto(
    string Reason,
    string? RejectedBy
);

public record CancelTransportRequestDto(
    string Reason,
    string? CancelledBy
);

public record CompleteTransportRequestDto(
    DateTime? CompletedAt,
    string? Remark
);

public record AddTransportRequestCarDto(
    string? TranspReqType,
    string RefOrdNo,
    string CarId,
    string Vin,
    string Model,
    string? SpecCode,
    string? ColorCode,
    string? EngineNo,
    string? StorageCodeFrom,
    string? StorageCodeTo,
    string? Remark
);

public interface ITransportRequestService
{
    Task<object> CreateAsync(CreateTransportRequestDto dto);
    Task<object> ListAsync(string? status, string? transporterCode, string? dealerCode, string? vin, string? refOrdNo, string? transpReqNo);
    Task<object?> DetailAsync(string transpReqNo);
    Task<object?> UpdateAsync(string transpReqNo, UpdateTransportRequestDto dto);
    Task<object?> ApproveAsync(string transpReqNo, ApproveTransportRequestDto? dto);
    Task<object?> RejectAsync(string transpReqNo, RejectTransportRequestDto dto);
    Task<object?> CancelAsync(string transpReqNo, CancelTransportRequestDto dto);
    Task<object?> CompleteAsync(string transpReqNo, CompleteTransportRequestDto? dto);
    Task<object?> DeleteAsync(string transpReqNo);
    Task<object?> AddCarAsync(string transpReqNo, AddTransportRequestCarDto dto);
    Task<object?> RemoveCarAsync(string transpReqNo, string vin);
    Task<object> StatsAsync();
}

public sealed class TransportRequestService(AppDbContext db, ITenantContext tenant) : ITransportRequestService
{
    private Guid Org => tenant.OrgId;

    public async Task<object> CreateAsync(CreateTransportRequestDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.DealerCode))
            throw new InvalidOperationException("Mã đại lý (DealerCode) không được để trống.");

        if (string.IsNullOrWhiteSpace(dto.TransporterCode))
            throw new InvalidOperationException("Mã đơn vị vận tải (TransporterCode) không được để trống.");

        if (dto.Details is null || dto.Details.Count == 0)
            throw new InvalidOperationException("Yêu cầu vận tải phải có ít nhất 1 chi tiết xe.");

        // Kiểm tra tính hợp lệ và chống trùng lặp trong cùng yêu cầu
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
            if (string.IsNullOrWhiteSpace(d.RefOrdNo))
                throw new InvalidOperationException($"Xe {d.Vin} bắt buộc phải có số lệnh tham chiếu (RefOrdNo: DO/Retrieve/Rearrange).");

            if (!vinSet.Add(d.Vin.Trim()))
                throw new InvalidOperationException($"Trùng lặp số khung VIN trong yêu cầu vận tải: {d.Vin}");
            if (!carIdSet.Add(d.CarId.Trim()))
                throw new InvalidOperationException($"Trùng lặp mã xe CarId trong yêu cầu vận tải: {d.CarId}");
        }

        // Kiểm tra xem các xe này có đang nằm trong YCVT nào khác chưa hoàn tất không
        var vins = vinSet.ToList();
        var busyCar = await db.TransportRequestDetails
            .Include(x => x.TranspReqNo)
            .Where(x => vins.Contains(x.Vin) &&
                        db.TransportRequests.Any(tr => tr.OrgId == Org && tr.TranspReqNo == x.TranspReqNo &&
                                                       (tr.Status == TransportRequestStatus.Pending ||
                                                        tr.Status == TransportRequestStatus.Approved ||
                                                        tr.Status == TransportRequestStatus.InTransit)))
            .Select(x => new { x.Vin, x.TranspReqNo })
            .FirstOrDefaultAsync();

        if (busyCar != null)
            throw new InvalidOperationException($"Xe có số khung VIN {busyCar.Vin} đang nằm trong yêu cầu vận tải {busyCar.TranspReqNo} chưa hoàn tất.");

        // Tự động sinh mã yêu cầu vận tải nếu chưa truyền vào
        string transpReqNo;
        if (!string.IsNullOrWhiteSpace(dto.TranspReqNo))
        {
            transpReqNo = dto.TranspReqNo.Trim().ToUpperInvariant();
        }
        else
        {
            var prefix = $"TR{DateTime.Today:yyMM}";
            var countThisMonth = await db.TransportRequests
                .CountAsync(x => x.OrgId == Org && x.TranspReqNo.StartsWith(prefix));
            transpReqNo = $"{prefix}{(countThisMonth + 1):D4}";
        }

        if (await db.TransportRequests.AnyAsync(x => x.OrgId == Org && x.TranspReqNo == transpReqNo))
            throw new InvalidOperationException($"Mã yêu cầu vận tải {transpReqNo} đã tồn tại trong hệ thống.");

        var reqType = ParseReqType(dto.TranspReqType);

        var req = new TransportRequest
        {
            OrgId = Org,
            TranspReqNo = transpReqNo,
            DealerCode = dto.DealerCode.Trim().ToUpperInvariant(),
            DealerName = dto.DealerName?.Trim() ?? dto.DealerCode.Trim(),
            TransporterCode = dto.TransporterCode.Trim().ToUpperInvariant(),
            TransporterName = dto.TransporterName?.Trim() ?? dto.TransporterCode.Trim(),
            TransportContractNo = dto.TransportContractNo?.Trim(),
            TranspReqType = reqType,
            Status = TransportRequestStatus.Pending,
            TotalCars = dto.Details.Count,
            PlateNo = dto.PlateNo?.Trim(),
            DriverName = dto.DriverName?.Trim(),
            DriverPhone = dto.DriverPhone?.Trim(),
            ExpectedStartDate = dto.ExpectedStartDate,
            ExpectedEndDate = dto.ExpectedEndDate,
            Remark = dto.Remark?.Trim(),
            CreatedAt = DateTime.Now,
            CreatedBy = "HQ_LOGISTICS_USER",
            Details = dto.Details.Select(d => new TransportRequestDetail
            {
                TranspReqNo = transpReqNo,
                TranspReqType = string.IsNullOrWhiteSpace(d.TranspReqType) ? reqType : ParseReqType(d.TranspReqType),
                RefOrdNo = d.RefOrdNo.Trim().ToUpperInvariant(),
                CarId = d.CarId.Trim().ToUpperInvariant(),
                Vin = d.Vin.Trim().ToUpperInvariant(),
                Model = d.Model.Trim(),
                SpecCode = d.SpecCode?.Trim(),
                ColorCode = d.ColorCode?.Trim(),
                EngineNo = d.EngineNo?.Trim(),
                StorageCodeFrom = d.StorageCodeFrom?.Trim(),
                StorageCodeTo = d.StorageCodeTo?.Trim(),
                Status = TransportRequestDtlStatus.Pending,
                Remark = d.Remark?.Trim()
            }).ToList()
        };

        db.TransportRequests.Add(req);
        await db.SaveChangesAsync();

        return new
        {
            success = true,
            message = $"Tạo yêu cầu vận tải xe {req.TranspReqNo} thành công.",
            transpReqNo = req.TranspReqNo,
            totalCars = req.TotalCars,
            status = req.Status.ToString()
        };
    }

    public async Task<object> ListAsync(string? status, string? transporterCode, string? dealerCode, string? vin, string? refOrdNo, string? transpReqNo)
    {
        var q = db.TransportRequests.AsNoTracking().Where(x => x.OrgId == Org);

        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<TransportRequestStatus>(status, true, out var st))
            q = q.Where(x => x.Status == st);

        if (!string.IsNullOrWhiteSpace(transporterCode))
            q = q.Where(x => x.TransporterCode == transporterCode.Trim().ToUpperInvariant());

        if (!string.IsNullOrWhiteSpace(dealerCode))
            q = q.Where(x => x.DealerCode == dealerCode.Trim().ToUpperInvariant());

        if (!string.IsNullOrWhiteSpace(transpReqNo))
            q = q.Where(x => x.TranspReqNo.Contains(transpReqNo.Trim().ToUpperInvariant()));

        if (!string.IsNullOrWhiteSpace(vin))
            q = q.Where(x => x.Details.Any(d => d.Vin.Contains(vin.Trim().ToUpperInvariant())));

        if (!string.IsNullOrWhiteSpace(refOrdNo))
            q = q.Where(x => x.Details.Any(d => d.RefOrdNo.Contains(refOrdNo.Trim().ToUpperInvariant())));

        var items = await q.OrderByDescending(x => x.CreatedAt)
            .Select(x => new
            {
                x.Id,
                x.TranspReqNo,
                x.DealerCode,
                x.DealerName,
                x.TransporterCode,
                x.TransporterName,
                x.TransportContractNo,
                TranspReqType = x.TranspReqType.ToString(),
                Status = x.Status.ToString(),
                x.TotalCars,
                x.PlateNo,
                x.DriverName,
                x.DriverPhone,
                x.ExpectedStartDate,
                x.ExpectedEndDate,
                x.Remark,
                x.RejectReason,
                x.CancelReason,
                x.CreatedAt,
                x.ApprovedAt,
                x.CompletedAt
            })
            .ToListAsync();

        return new { total = items.Count, data = items };
    }

    public async Task<object?> DetailAsync(string transpReqNo)
    {
        var req = await db.TransportRequests.AsNoTracking()
            .Include(x => x.Details)
            .FirstOrDefaultAsync(x => x.OrgId == Org && x.TranspReqNo == transpReqNo.Trim().ToUpperInvariant());

        if (req is null) return null;

        return new
        {
            req.Id,
            req.TranspReqNo,
            req.DealerCode,
            req.DealerName,
            req.TransporterCode,
            req.TransporterName,
            req.TransportContractNo,
            TranspReqType = req.TranspReqType.ToString(),
            Status = req.Status.ToString(),
            req.TotalCars,
            req.PlateNo,
            req.DriverName,
            req.DriverPhone,
            req.ExpectedStartDate,
            req.ExpectedEndDate,
            req.Remark,
            req.RejectReason,
            req.CancelReason,
            req.CreatedBy,
            req.CreatedAt,
            req.ApprovedBy,
            req.ApprovedAt,
            req.CompletedAt,
            req.CancelledBy,
            req.CancelledAt,
            details = req.Details.Select(d => new
            {
                d.Id,
                d.TranspReqNo,
                TranspReqType = d.TranspReqType.ToString(),
                d.RefOrdNo,
                d.CarId,
                d.Vin,
                d.Model,
                d.SpecCode,
                d.ColorCode,
                d.EngineNo,
                d.StorageCodeFrom,
                d.StorageCodeTo,
                Status = d.Status.ToString(),
                d.ActualOutDate,
                d.ActualInDate,
                d.Remark
            }).ToList()
        };
    }

    public async Task<object?> UpdateAsync(string transpReqNo, UpdateTransportRequestDto dto)
    {
        var req = await db.TransportRequests
            .FirstOrDefaultAsync(x => x.OrgId == Org && x.TranspReqNo == transpReqNo.Trim().ToUpperInvariant());

        if (req is null) return null;

        if (req.Status != TransportRequestStatus.Pending)
            throw new InvalidOperationException($"Chỉ cho phép cập nhật thông tin yêu cầu vận tải khi ở trạng thái Chờ duyệt (Pending). Trạng thái hiện tại: {req.Status}.");

        if (dto.DealerName != null) req.DealerName = dto.DealerName.Trim();
        if (dto.TransporterName != null) req.TransporterName = dto.TransporterName.Trim();
        if (dto.TransportContractNo != null) req.TransportContractNo = dto.TransportContractNo.Trim();
        if (dto.PlateNo != null) req.PlateNo = dto.PlateNo.Trim();
        if (dto.DriverName != null) req.DriverName = dto.DriverName.Trim();
        if (dto.DriverPhone != null) req.DriverPhone = dto.DriverPhone.Trim();
        if (dto.ExpectedStartDate != null) req.ExpectedStartDate = dto.ExpectedStartDate;
        if (dto.ExpectedEndDate != null) req.ExpectedEndDate = dto.ExpectedEndDate;
        if (dto.Remark != null) req.Remark = dto.Remark.Trim();

        await db.SaveChangesAsync();

        return new
        {
            success = true,
            message = $"Cập nhật yêu cầu vận tải {req.TranspReqNo} thành công.",
            transpReqNo = req.TranspReqNo
        };
    }

    public async Task<object?> ApproveAsync(string transpReqNo, ApproveTransportRequestDto? dto)
    {
        var req = await db.TransportRequests
            .Include(x => x.Details)
            .FirstOrDefaultAsync(x => x.OrgId == Org && x.TranspReqNo == transpReqNo.Trim().ToUpperInvariant());

        if (req is null) return null;

        if (req.Status != TransportRequestStatus.Pending)
            throw new InvalidOperationException($"Chỉ có thể phê duyệt yêu cầu vận tải ở trạng thái Chờ duyệt (Pending). Trạng thái hiện tại: {req.Status}.");

        if (req.Details.Count == 0)
            throw new InvalidOperationException("Không thể duyệt yêu cầu vận tải không có chi tiết xe.");

        req.Status = TransportRequestStatus.Approved;
        req.ApprovedBy = dto?.ApprovedBy?.Trim() ?? "TP_DIEUPHOI_LOGISTICS";
        req.ApprovedAt = DateTime.Now;
        if (!string.IsNullOrWhiteSpace(dto?.Remark))
            req.Remark = $"{req.Remark} | [Duyệt HQ: {dto.Remark.Trim()}]";

        foreach (var d in req.Details)
        {
            if (d.Status == TransportRequestDtlStatus.Pending)
                d.Status = TransportRequestDtlStatus.Approved;
        }

        await db.SaveChangesAsync();

        return new
        {
            success = true,
            message = $"Phê duyệt yêu cầu vận tải {req.TranspReqNo} thành công. Lệnh chuyển sang trạng thái Đã duyệt điều xe (Approved).",
            transpReqNo = req.TranspReqNo,
            status = req.Status.ToString(),
            approvedAt = req.ApprovedAt,
            approvedBy = req.ApprovedBy
        };
    }

    public async Task<object?> RejectAsync(string transpReqNo, RejectTransportRequestDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Reason))
            throw new InvalidOperationException("Lý do từ chối không được để trống.");

        var req = await db.TransportRequests
            .Include(x => x.Details)
            .FirstOrDefaultAsync(x => x.OrgId == Org && x.TranspReqNo == transpReqNo.Trim().ToUpperInvariant());

        if (req is null) return null;

        // DMS.Sales Sto_TranspReq_Approve: ngả từ chối cho phép khi ở trạng thái P hoặc A (chưa hoàn tất vận chuyển / giao nhận)
        if (req.Status != TransportRequestStatus.Pending && req.Status != TransportRequestStatus.Approved)
            throw new InvalidOperationException($"Chỉ có thể từ chối yêu cầu vận tải khi ở trạng thái Chờ duyệt (Pending) hoặc Đã duyệt (Approved). Trạng thái hiện tại: {req.Status}.");

        // Kiểm tra xem đã có xe nào hoàn thành hoặc đã ký biên bản bàn giao chưa
        if (req.Details.Any(d => d.Status == TransportRequestDtlStatus.Completed))
            throw new InvalidOperationException("Không thể từ chối yêu cầu vận tải đã có xe hoàn tất vận chuyển hoặc đã lập biên bản bàn giao.");

        req.Status = TransportRequestStatus.Rejected;
        req.RejectReason = dto.Reason.Trim();

        foreach (var d in req.Details)
        {
            d.Status = TransportRequestDtlStatus.Rejected;
        }

        await db.SaveChangesAsync();

        return new
        {
            success = true,
            message = $"Từ chối yêu cầu vận tải {req.TranspReqNo} thành công.",
            transpReqNo = req.TranspReqNo,
            status = req.Status.ToString(),
            rejectReason = req.RejectReason
        };
    }

    public async Task<object?> CancelAsync(string transpReqNo, CancelTransportRequestDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Reason))
            throw new InvalidOperationException("Lý do hủy yêu cầu không được để trống.");

        var req = await db.TransportRequests
            .Include(x => x.Details)
            .FirstOrDefaultAsync(x => x.OrgId == Org && x.TranspReqNo == transpReqNo.Trim().ToUpperInvariant());

        if (req is null) return null;

        if (req.Status != TransportRequestStatus.Pending && req.Status != TransportRequestStatus.Approved)
            throw new InvalidOperationException($"Chỉ có thể hủy yêu cầu vận tải khi ở trạng thái Chờ duyệt (Pending) hoặc Đã duyệt (Approved). Trạng thái hiện tại: {req.Status}.");

        if (req.Details.Any(d => d.Status == TransportRequestDtlStatus.Completed))
            throw new InvalidOperationException("Không thể hủy yêu cầu vận tải đã có xe hoàn tất vận chuyển.");

        req.Status = TransportRequestStatus.Cancelled;
        req.CancelReason = dto.Reason.Trim();
        req.CancelledBy = dto.CancelledBy?.Trim() ?? "HQ_LOGISTICS_USER";
        req.CancelledAt = DateTime.Now;

        await db.SaveChangesAsync();

        return new
        {
            success = true,
            message = $"Hủy yêu cầu vận tải {req.TranspReqNo} thành công.",
            transpReqNo = req.TranspReqNo,
            status = req.Status.ToString(),
            cancelReason = req.CancelReason
        };
    }

    public async Task<object?> CompleteAsync(string transpReqNo, CompleteTransportRequestDto? dto)
    {
        var req = await db.TransportRequests
            .Include(x => x.Details)
            .FirstOrDefaultAsync(x => x.OrgId == Org && x.TranspReqNo == transpReqNo.Trim().ToUpperInvariant());

        if (req is null) return null;

        if (req.Status != TransportRequestStatus.Approved && req.Status != TransportRequestStatus.InTransit)
            throw new InvalidOperationException($"Chỉ có thể xác nhận hoàn tất yêu cầu vận tải khi ở trạng thái Đã duyệt (Approved) hoặc Đang vận chuyển (InTransit). Trạng thái hiện tại: {req.Status}.");

        req.Status = TransportRequestStatus.Completed;
        req.CompletedAt = dto?.CompletedAt ?? DateTime.Now;
        if (!string.IsNullOrWhiteSpace(dto?.Remark))
            req.Remark = $"{req.Remark} | [Hoàn tất: {dto.Remark.Trim()}]";

        foreach (var d in req.Details)
        {
            d.Status = TransportRequestDtlStatus.Completed;
            d.ActualInDate ??= DateTime.Today;
        }

        await db.SaveChangesAsync();

        return new
        {
            success = true,
            message = $"Xác nhận hoàn tất vận chuyển thành công cho yêu cầu {req.TranspReqNo}.",
            transpReqNo = req.TranspReqNo,
            status = req.Status.ToString(),
            completedAt = req.CompletedAt
        };
    }

    public async Task<object?> DeleteAsync(string transpReqNo)
    {
        var req = await db.TransportRequests
            .Include(x => x.Details)
            .FirstOrDefaultAsync(x => x.OrgId == Org && x.TranspReqNo == transpReqNo.Trim().ToUpperInvariant());

        if (req is null) return null;

        if (req.Status != TransportRequestStatus.Pending)
            throw new InvalidOperationException($"Chỉ cho phép xóa yêu cầu vận tải khi ở trạng thái Chờ duyệt (Pending). Trạng thái hiện tại: {req.Status}.");

        db.TransportRequests.Remove(req);
        await db.SaveChangesAsync();

        return new
        {
            success = true,
            message = $"Đã xóa yêu cầu vận tải {transpReqNo} khỏi hệ thống."
        };
    }

    public async Task<object?> AddCarAsync(string transpReqNo, AddTransportRequestCarDto dto)
    {
        var req = await db.TransportRequests
            .Include(x => x.Details)
            .FirstOrDefaultAsync(x => x.OrgId == Org && x.TranspReqNo == transpReqNo.Trim().ToUpperInvariant());

        if (req is null) return null;

        if (req.Status != TransportRequestStatus.Pending)
            throw new InvalidOperationException($"Chỉ cho phép bổ sung xe khi yêu cầu vận tải ở trạng thái Chờ duyệt (Pending). Trạng thái hiện tại: {req.Status}.");

        if (string.IsNullOrWhiteSpace(dto.CarId))
            throw new InvalidOperationException("Mã xe CarId không được để trống.");
        if (string.IsNullOrWhiteSpace(dto.Vin))
            throw new InvalidOperationException("Số khung VIN không được để trống.");
        if (string.IsNullOrWhiteSpace(dto.Model))
            throw new InvalidOperationException("Dòng xe Model không được để trống.");
        if (string.IsNullOrWhiteSpace(dto.RefOrdNo))
            throw new InvalidOperationException("Số lệnh tham chiếu RefOrdNo không được để trống.");

        var vin = dto.Vin.Trim().ToUpperInvariant();
        if (req.Details.Any(d => string.Equals(d.Vin, vin, StringComparison.OrdinalIgnoreCase)))
            throw new InvalidOperationException($"Số khung VIN {vin} đã tồn tại trong yêu cầu vận tải này.");

        var busyCar = await db.TransportRequestDetails
            .Where(x => x.Vin == vin &&
                        db.TransportRequests.Any(tr => tr.OrgId == Org && tr.TranspReqNo == x.TranspReqNo &&
                                                       (tr.Status == TransportRequestStatus.Pending ||
                                                        tr.Status == TransportRequestStatus.Approved ||
                                                        tr.Status == TransportRequestStatus.InTransit)))
            .Select(x => new { x.Vin, x.TranspReqNo })
            .FirstOrDefaultAsync();

        if (busyCar != null)
            throw new InvalidOperationException($"Xe có số khung VIN {busyCar.Vin} đang nằm trong yêu cầu vận tải {busyCar.TranspReqNo} chưa hoàn tất.");

        var reqType = string.IsNullOrWhiteSpace(dto.TranspReqType) ? req.TranspReqType : ParseReqType(dto.TranspReqType);

        var dtl = new TransportRequestDetail
        {
            TranspReqNo = req.TranspReqNo,
            TranspReqType = reqType,
            RefOrdNo = dto.RefOrdNo.Trim().ToUpperInvariant(),
            CarId = dto.CarId.Trim().ToUpperInvariant(),
            Vin = vin,
            Model = dto.Model.Trim(),
            SpecCode = dto.SpecCode?.Trim(),
            ColorCode = dto.ColorCode?.Trim(),
            EngineNo = dto.EngineNo?.Trim(),
            StorageCodeFrom = dto.StorageCodeFrom?.Trim(),
            StorageCodeTo = dto.StorageCodeTo?.Trim(),
            Status = TransportRequestDtlStatus.Pending,
            Remark = dto.Remark?.Trim()
        };

        req.Details.Add(dtl);
        req.TotalCars = req.Details.Count;
        await db.SaveChangesAsync();

        return new
        {
            success = true,
            message = $"Bổ sung xe {vin} vào yêu cầu vận tải {req.TranspReqNo} thành công.",
            transpReqNo = req.TranspReqNo,
            totalCars = req.TotalCars
        };
    }

    public async Task<object?> RemoveCarAsync(string transpReqNo, string vin)
    {
        var req = await db.TransportRequests
            .Include(x => x.Details)
            .FirstOrDefaultAsync(x => x.OrgId == Org && x.TranspReqNo == transpReqNo.Trim().ToUpperInvariant());

        if (req is null) return null;

        var targetVin = vin.Trim().ToUpperInvariant();
        var dtl = req.Details.FirstOrDefault(d => string.Equals(d.Vin, targetVin, StringComparison.OrdinalIgnoreCase));
        if (dtl is null)
            throw new InvalidOperationException($"Không tìm thấy xe {targetVin} trong yêu cầu vận tải {transpReqNo}.");

        // DMS.Sales Sto_TranspReqDtl_DeleteX: xe đã giao nhận hoàn tất không cho xóa
        if (dtl.Status == TransportRequestDtlStatus.Completed)
            throw new InvalidOperationException($"Không thể gỡ xe {targetVin} đã hoàn tất vận chuyển/giao nhận.");

        req.Details.Remove(dtl);
        db.TransportRequestDetails.Remove(dtl);

        // Theo DMS.Sales: khi không còn xe nào trong yêu cầu vận tải thì tự động xóa luôn lệnh
        if (req.Details.Count == 0)
        {
            db.TransportRequests.Remove(req);
            await db.SaveChangesAsync();
            return new
            {
                success = true,
                message = $"Đã gỡ xe {targetVin}. Do không còn xe nào trong yêu cầu vận tải {transpReqNo}, hệ thống đã tự động giải phóng/xóa lệnh vận tải.",
                deletedRequest = true
            };
        }

        req.TotalCars = req.Details.Count;
        await db.SaveChangesAsync();

        return new
        {
            success = true,
            message = $"Đã gỡ xe {targetVin} khỏi yêu cầu vận tải {transpReqNo}.",
            transpReqNo = req.TranspReqNo,
            totalCars = req.TotalCars
        };
    }

    public async Task<object> StatsAsync()
    {
        var total = await db.TransportRequests.CountAsync(x => x.OrgId == Org);
        var pending = await db.TransportRequests.CountAsync(x => x.OrgId == Org && x.Status == TransportRequestStatus.Pending);
        var approved = await db.TransportRequests.CountAsync(x => x.OrgId == Org && x.Status == TransportRequestStatus.Approved);
        var inTransit = await db.TransportRequests.CountAsync(x => x.OrgId == Org && x.Status == TransportRequestStatus.InTransit);
        var completed = await db.TransportRequests.CountAsync(x => x.OrgId == Org && x.Status == TransportRequestStatus.Completed);
        var rejected = await db.TransportRequests.CountAsync(x => x.OrgId == Org && x.Status == TransportRequestStatus.Rejected);
        var cancelled = await db.TransportRequests.CountAsync(x => x.OrgId == Org && x.Status == TransportRequestStatus.Cancelled);
        var totalCars = await db.TransportRequests.Where(x => x.OrgId == Org).SumAsync(x => (int?)x.TotalCars) ?? 0;

        return new
        {
            total,
            pending,
            approved,
            inTransit,
            completed,
            rejected,
            cancelled,
            totalCars
        };
    }

    private static TransportRequestType ParseReqType(string? str)
    {
        if (string.IsNullOrWhiteSpace(str)) return TransportRequestType.CarTransport;

        var upper = str.Trim().ToUpperInvariant();
        return upper switch
        {
            "CARRETRIEVE" or "RETRIEVE" => TransportRequestType.CarRetrieve,
            "STORAGEREARRANGE" or "REARRANGE" => TransportRequestType.StorageRearrange,
            "STORAGEREARRCB" or "REARRCB" => TransportRequestType.StorageRearrCB,
            _ => TransportRequestType.CarTransport
        };
    }
}
