using Microsoft.EntityFrameworkCore;
using MiniSales.Data;
using MiniSales.Models;

namespace MiniSales.Services;

// ===== DTOs =====
public record CreateRearrangeTranspReqDetailDto(
    string Vin,
    string? Model,
    string? RefOrdNo,
    string? StorageCodeFrom,
    string? StorageCodeTo,
    string? Remark
);

public record CreateRearrangeTranspReqDto(
    string? SRTReqNo,
    string TransporterCode,
    string? TransporterName,
    string? TransportContractNo,
    string? DealerCode,
    string? StorageCodeFrom,
    string? StorageCodeTo,
    string? PlateNo,
    string? DriverName,
    string? DriverPhone,
    DateTime? ExpectedStartDate,
    DateTime? ExpectedEndDate,
    string? Remark,
    List<CreateRearrangeTranspReqDetailDto>? Details
);

public record UpdateRearrangeTranspReqDto(
    string? TransporterName,
    string? TransportContractNo,
    string? PlateNo,
    string? DriverName,
    string? DriverPhone,
    DateTime? ExpectedStartDate,
    DateTime? ExpectedEndDate,
    string? Remark
);

public record ApproveRearrangeTranspReqDto(string? ApprovedBy, string? Remark);
public record RejectRearrangeTranspReqDto(string Reason, string? RejectedBy);
public record CancelRearrangeTranspReqDto(string Reason, string? CancelledBy);
public record AddRearrangeTranspReqCarDto(
    string Vin,
    string? Model,
    string? RefOrdNo,
    string? StorageCodeFrom,
    string? StorageCodeTo,
    string? Remark
);

public interface IRearrangeTransportRequestService
{
    Task<object> CreateAsync(CreateRearrangeTranspReqDto dto);
    Task<object> ListAsync(string? status, string? transporterCode, string? srtReqNo, string? vin);
    Task<object?> DetailAsync(string srtReqNo);
    Task<object?> UpdateAsync(string srtReqNo, UpdateRearrangeTranspReqDto dto);
    Task<object?> ApproveAsync(string srtReqNo, ApproveRearrangeTranspReqDto? dto);
    Task<object?> RejectAsync(string srtReqNo, RejectRearrangeTranspReqDto dto);
    Task<object?> CancelAsync(string srtReqNo, CancelRearrangeTranspReqDto dto);
    Task<object?> DeleteAsync(string srtReqNo);
    Task<object?> AddCarAsync(string srtReqNo, AddRearrangeTranspReqCarDto dto);
    Task<object?> RemoveCarAsync(string srtReqNo, string vin);
    Task<object> StatsAsync();
}

/// <summary>
/// Quản lý Yêu cầu vận chuyển khi chuyển kho xe ô tô (DMS.Sales Sto_RearrangeTranspReq / Sto_RearrangeTranspReqDtl / Storage.cs / FrmMngRearrangeTranspReq):
/// - Gom nhóm các xe VIN (mỗi VIN gắn 1 lệnh điều chuyển kho Sto_StorageRearrange đã duyệt A2) vào 1 yêu cầu vận chuyển theo nhà vận chuyển.
/// - Khóa nghiệp vụ = SRTReqNo. Nhà vận chuyển phải tồn tại + đang hoạt động (Mst_Transporter.FlagActive='1').
/// - Chặn tạo YCVT mới cho VIN đang nằm trong YCVT khác chưa hoàn tất (P/A) — khớp Sto_TranspReq_Create_VINInUse_DC / InvalidStatus.
/// - Duyệt (Approve) chỉ khi ở trạng thái Pending; Từ chối (Reject) cho phép khi P hoặc A (chưa có BBGN).
/// </summary>
public sealed class RearrangeTransportRequestService(AppDbContext db, ITenantContext tenant) : IRearrangeTransportRequestService
{
    private Guid Org => tenant.OrgId;

    public async Task<object> CreateAsync(CreateRearrangeTranspReqDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.TransporterCode))
            throw new InvalidOperationException("Mã nhà vận chuyển (TransporterCode) không được để trống.");

        if (dto.Details is null || dto.Details.Count == 0)
            throw new InvalidOperationException("Yêu cầu vận chuyển chuyển kho phải có ít nhất 1 dòng xe (VIN).");

        // Nhà vận chuyển phải tồn tại + đang hoạt động (Sto_RearrangeTranspReq_Create_TransporterNotExistOrInactive)
        var transporterCode = dto.TransporterCode.Trim().ToUpperInvariant();
        var transporter = await db.Transporters.AsNoTracking()
            .FirstOrDefaultAsync(x => x.OrgId == Org && x.TransporterCode == transporterCode);
        if (transporter is null || transporter.FlagActive != "1")
            throw new InvalidOperationException($"Nhà vận chuyển '{transporterCode}' không tồn tại hoặc đã ngừng hoạt động (Mst_Transporter).");

        // Kiểm tra trùng lặp VIN trong cùng yêu cầu
        var vinSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var d in dto.Details)
        {
            if (string.IsNullOrWhiteSpace(d.Vin))
                throw new InvalidOperationException("Mỗi dòng xe bắt buộc phải có số khung VIN.");
            if (!vinSet.Add(d.Vin.Trim().ToUpperInvariant()))
                throw new InvalidOperationException($"Trùng lặp số khung VIN trong yêu cầu vận chuyển: {d.Vin}");
        }

        var vins = vinSet.ToList();

        // Chặn VIN đang nằm trong YCVT chuyển kho khác chưa hoàn tất (P/A) — Sto_TranspReq_Create_VINInUse_DC
        var busy = await db.RearrangeTransportRequestDetails.AsNoTracking()
            .Where(x => vins.Contains(x.Vin) &&
                        db.RearrangeTransportRequests.Any(r => r.OrgId == Org && r.SRTReqNo == x.SRTReqNo &&
                            (r.Status == RearrangeTranspReqStatus.Pending || r.Status == RearrangeTranspReqStatus.Approved)))
            .Select(x => new { x.Vin, x.SRTReqNo })
            .FirstOrDefaultAsync();
        if (busy != null)
            throw new InvalidOperationException($"Xe có số khung VIN {busy.Vin} đang nằm trong yêu cầu vận chuyển chuyển kho {busy.SRTReqNo} chưa hoàn tất.");

        // Sinh mã yêu cầu nếu chưa truyền vào
        string srtReqNo;
        if (!string.IsNullOrWhiteSpace(dto.SRTReqNo))
        {
            srtReqNo = dto.SRTReqNo.Trim().ToUpperInvariant();
        }
        else
        {
            var prefix = $"SRT{DateTime.Today:yyMM}";
            var countThisMonth = await db.RearrangeTransportRequests
                .CountAsync(x => x.OrgId == Org && x.SRTReqNo.StartsWith(prefix));
            srtReqNo = $"{prefix}{(countThisMonth + 1):D4}";
        }

        if (await db.RearrangeTransportRequests.AnyAsync(x => x.OrgId == Org && x.SRTReqNo == srtReqNo))
            throw new InvalidOperationException($"Mã yêu cầu vận chuyển chuyển kho {srtReqNo} đã tồn tại trong hệ thống.");

        var req = new RearrangeTransportRequest
        {
            OrgId = Org,
            SRTReqNo = srtReqNo,
            TransporterCode = transporterCode,
            TransporterName = dto.TransporterName?.Trim() ?? transporter.TransporterName,
            TransportContractNo = dto.TransportContractNo?.Trim() ?? transporter.TransportContractNo,
            DealerCode = dto.DealerCode?.Trim(),
            StorageCodeFrom = dto.StorageCodeFrom?.Trim(),
            StorageCodeTo = dto.StorageCodeTo?.Trim(),
            Status = RearrangeTranspReqStatus.Pending,
            TotalCars = dto.Details.Count,
            PlateNo = dto.PlateNo?.Trim(),
            DriverName = dto.DriverName?.Trim(),
            DriverPhone = dto.DriverPhone?.Trim(),
            ExpectedStartDate = dto.ExpectedStartDate,
            ExpectedEndDate = dto.ExpectedEndDate,
            Remark = dto.Remark?.Trim(),
            CreatedBy = "HQ_LOGISTICS_USER",
            CreatedAt = DateTime.Now,
            Details = dto.Details.Select(d => new RearrangeTransportRequestDetail
            {
                SRTReqNo = srtReqNo,
                Vin = d.Vin.Trim().ToUpperInvariant(),
                Model = d.Model?.Trim(),
                RefOrdNo = d.RefOrdNo?.Trim().ToUpperInvariant(),
                StorageCodeFrom = d.StorageCodeFrom?.Trim(),
                StorageCodeTo = d.StorageCodeTo?.Trim(),
                Status = RearrangeTranspReqDtlStatus.Pending,
                Remark = d.Remark?.Trim()
            }).ToList()
        };

        db.RearrangeTransportRequests.Add(req);
        await db.SaveChangesAsync();

        return new
        {
            success = true,
            message = $"Tạo yêu cầu vận chuyển chuyển kho {req.SRTReqNo} thành công.",
            srtReqNo = req.SRTReqNo,
            totalCars = req.TotalCars,
            status = req.Status.ToString()
        };
    }

    public async Task<object> ListAsync(string? status, string? transporterCode, string? srtReqNo, string? vin)
    {
        var q = db.RearrangeTransportRequests.AsNoTracking().Where(x => x.OrgId == Org);

        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<RearrangeTranspReqStatus>(status, true, out var st))
            q = q.Where(x => x.Status == st);

        if (!string.IsNullOrWhiteSpace(transporterCode))
            q = q.Where(x => x.TransporterCode == transporterCode.Trim().ToUpperInvariant());

        if (!string.IsNullOrWhiteSpace(srtReqNo))
            q = q.Where(x => x.SRTReqNo.Contains(srtReqNo.Trim().ToUpperInvariant()));

        if (!string.IsNullOrWhiteSpace(vin))
            q = q.Where(x => x.Details.Any(d => d.Vin.Contains(vin.Trim().ToUpperInvariant())));

        var items = await q.OrderByDescending(x => x.CreatedAt)
            .Select(x => new
            {
                x.Id,
                x.SRTReqNo,
                x.TransporterCode,
                x.TransporterName,
                x.TransportContractNo,
                x.DealerCode,
                x.StorageCodeFrom,
                x.StorageCodeTo,
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
                x.ApprovedAt
            })
            .ToListAsync();

        return new { total = items.Count, data = items };
    }

    public async Task<object?> DetailAsync(string srtReqNo)
    {
        var req = await db.RearrangeTransportRequests.AsNoTracking()
            .Include(x => x.Details)
            .FirstOrDefaultAsync(x => x.OrgId == Org && x.SRTReqNo == srtReqNo.Trim().ToUpperInvariant());

        if (req is null) return null;

        return new
        {
            req.Id,
            req.SRTReqNo,
            req.TransporterCode,
            req.TransporterName,
            req.TransportContractNo,
            req.DealerCode,
            req.StorageCodeFrom,
            req.StorageCodeTo,
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
            req.CancelledBy,
            req.CancelledAt,
            details = req.Details.Select(d => new
            {
                d.Id,
                d.SRTReqNo,
                d.Vin,
                d.Model,
                d.RefOrdNo,
                d.StorageCodeFrom,
                d.StorageCodeTo,
                Status = d.Status.ToString(),
                d.Remark
            }).ToList()
        };
    }

    public async Task<object?> UpdateAsync(string srtReqNo, UpdateRearrangeTranspReqDto dto)
    {
        var req = await db.RearrangeTransportRequests
            .FirstOrDefaultAsync(x => x.OrgId == Org && x.SRTReqNo == srtReqNo.Trim().ToUpperInvariant());

        if (req is null) return null;

        if (req.Status != RearrangeTranspReqStatus.Pending)
            throw new InvalidOperationException($"Chỉ cho phép cập nhật yêu cầu vận chuyển khi ở trạng thái Chờ duyệt (Pending). Trạng thái hiện tại: {req.Status}.");

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
            message = $"Cập nhật yêu cầu vận chuyển chuyển kho {req.SRTReqNo} thành công.",
            srtReqNo = req.SRTReqNo
        };
    }

    public async Task<object?> ApproveAsync(string srtReqNo, ApproveRearrangeTranspReqDto? dto)
    {
        var req = await db.RearrangeTransportRequests
            .Include(x => x.Details)
            .FirstOrDefaultAsync(x => x.OrgId == Org && x.SRTReqNo == srtReqNo.Trim().ToUpperInvariant());

        if (req is null) return null;

        // Sto_TranspReq_Approve_InvalidSRTReqStatus: chỉ duyệt khi ở trạng thái P
        if (req.Status != RearrangeTranspReqStatus.Pending)
            throw new InvalidOperationException($"Chỉ có thể phê duyệt yêu cầu vận chuyển ở trạng thái Chờ duyệt (Pending). Trạng thái hiện tại: {req.Status}.");

        if (req.Details.Count == 0)
            throw new InvalidOperationException("Không thể duyệt yêu cầu vận chuyển không có dòng xe.");

        req.Status = RearrangeTranspReqStatus.Approved;
        req.ApprovedBy = dto?.ApprovedBy?.Trim() ?? "TP_DIEUPHOI_LOGISTICS";
        req.ApprovedAt = DateTime.Now;
        if (!string.IsNullOrWhiteSpace(dto?.Remark))
            req.Remark = $"{req.Remark} | [Duyệt HQ: {dto.Remark.Trim()}]";

        foreach (var d in req.Details)
        {
            if (d.Status == RearrangeTranspReqDtlStatus.Pending)
                d.Status = RearrangeTranspReqDtlStatus.Approved;
        }

        await db.SaveChangesAsync();

        return new
        {
            success = true,
            message = $"Phê duyệt yêu cầu vận chuyển chuyển kho {req.SRTReqNo} thành công.",
            srtReqNo = req.SRTReqNo,
            status = req.Status.ToString(),
            approvedAt = req.ApprovedAt,
            approvedBy = req.ApprovedBy
        };
    }

    public async Task<object?> RejectAsync(string srtReqNo, RejectRearrangeTranspReqDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Reason))
            throw new InvalidOperationException("Lý do từ chối không được để trống.");

        var req = await db.RearrangeTransportRequests
            .Include(x => x.Details)
            .FirstOrDefaultAsync(x => x.OrgId == Org && x.SRTReqNo == srtReqNo.Trim().ToUpperInvariant());

        if (req is null) return null;

        // Sto_TranspReq_Approve: ngả từ chối cho phép khi ở trạng thái P hoặc A (chưa có BBGN)
        if (req.Status != RearrangeTranspReqStatus.Pending && req.Status != RearrangeTranspReqStatus.Approved)
            throw new InvalidOperationException($"Chỉ có thể từ chối yêu cầu vận chuyển khi ở trạng thái Chờ duyệt (Pending) hoặc Đã duyệt (Approved). Trạng thái hiện tại: {req.Status}.");

        req.Status = RearrangeTranspReqStatus.Rejected;
        req.RejectReason = dto.Reason.Trim();

        foreach (var d in req.Details)
            d.Status = RearrangeTranspReqDtlStatus.Rejected;

        await db.SaveChangesAsync();

        return new
        {
            success = true,
            message = $"Từ chối yêu cầu vận chuyển chuyển kho {req.SRTReqNo} thành công.",
            srtReqNo = req.SRTReqNo,
            status = req.Status.ToString(),
            rejectReason = req.RejectReason
        };
    }

    public async Task<object?> CancelAsync(string srtReqNo, CancelRearrangeTranspReqDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Reason))
            throw new InvalidOperationException("Lý do hủy yêu cầu không được để trống.");

        var req = await db.RearrangeTransportRequests
            .Include(x => x.Details)
            .FirstOrDefaultAsync(x => x.OrgId == Org && x.SRTReqNo == srtReqNo.Trim().ToUpperInvariant());

        if (req is null) return null;

        if (req.Status != RearrangeTranspReqStatus.Pending && req.Status != RearrangeTranspReqStatus.Approved)
            throw new InvalidOperationException($"Chỉ có thể hủy yêu cầu vận chuyển khi ở trạng thái Chờ duyệt (Pending) hoặc Đã duyệt (Approved). Trạng thái hiện tại: {req.Status}.");

        req.Status = RearrangeTranspReqStatus.Cancelled;
        req.CancelReason = dto.Reason.Trim();
        req.CancelledBy = dto.CancelledBy?.Trim() ?? "HQ_LOGISTICS_USER";
        req.CancelledAt = DateTime.Now;

        foreach (var d in req.Details)
            d.Status = RearrangeTranspReqDtlStatus.Cancelled;

        await db.SaveChangesAsync();

        return new
        {
            success = true,
            message = $"Hủy yêu cầu vận chuyển chuyển kho {req.SRTReqNo} thành công.",
            srtReqNo = req.SRTReqNo,
            status = req.Status.ToString(),
            cancelReason = req.CancelReason
        };
    }

    public async Task<object?> DeleteAsync(string srtReqNo)
    {
        var req = await db.RearrangeTransportRequests
            .Include(x => x.Details)
            .FirstOrDefaultAsync(x => x.OrgId == Org && x.SRTReqNo == srtReqNo.Trim().ToUpperInvariant());

        if (req is null) return null;

        if (req.Status != RearrangeTranspReqStatus.Pending)
            throw new InvalidOperationException($"Chỉ cho phép xóa yêu cầu vận chuyển khi ở trạng thái Chờ duyệt (Pending). Trạng thái hiện tại: {req.Status}.");

        db.RearrangeTransportRequests.Remove(req);
        await db.SaveChangesAsync();

        return new
        {
            success = true,
            message = $"Đã xóa yêu cầu vận chuyển chuyển kho {srtReqNo} khỏi hệ thống."
        };
    }

    public async Task<object?> AddCarAsync(string srtReqNo, AddRearrangeTranspReqCarDto dto)
    {
        var req = await db.RearrangeTransportRequests
            .Include(x => x.Details)
            .FirstOrDefaultAsync(x => x.OrgId == Org && x.SRTReqNo == srtReqNo.Trim().ToUpperInvariant());

        if (req is null) return null;

        if (req.Status != RearrangeTranspReqStatus.Pending)
            throw new InvalidOperationException($"Chỉ cho phép bổ sung xe khi yêu cầu vận chuyển ở trạng thái Chờ duyệt (Pending). Trạng thái hiện tại: {req.Status}.");

        if (string.IsNullOrWhiteSpace(dto.Vin))
            throw new InvalidOperationException("Số khung VIN không được để trống.");

        var vin = dto.Vin.Trim().ToUpperInvariant();
        if (req.Details.Any(d => string.Equals(d.Vin, vin, StringComparison.OrdinalIgnoreCase)))
            throw new InvalidOperationException($"Số khung VIN {vin} đã tồn tại trong yêu cầu vận chuyển này.");

        var busy = await db.RearrangeTransportRequestDetails.AsNoTracking()
            .Where(x => x.Vin == vin &&
                        db.RearrangeTransportRequests.Any(r => r.OrgId == Org && r.SRTReqNo == x.SRTReqNo &&
                            (r.Status == RearrangeTranspReqStatus.Pending || r.Status == RearrangeTranspReqStatus.Approved)))
            .Select(x => new { x.Vin, x.SRTReqNo })
            .FirstOrDefaultAsync();
        if (busy != null)
            throw new InvalidOperationException($"Xe có số khung VIN {busy.Vin} đang nằm trong yêu cầu vận chuyển chuyển kho {busy.SRTReqNo} chưa hoàn tất.");

        req.Details.Add(new RearrangeTransportRequestDetail
        {
            SRTReqNo = req.SRTReqNo,
            Vin = vin,
            Model = dto.Model?.Trim(),
            RefOrdNo = dto.RefOrdNo?.Trim().ToUpperInvariant(),
            StorageCodeFrom = dto.StorageCodeFrom?.Trim(),
            StorageCodeTo = dto.StorageCodeTo?.Trim(),
            Status = RearrangeTranspReqDtlStatus.Pending,
            Remark = dto.Remark?.Trim()
        });
        req.TotalCars = req.Details.Count;
        await db.SaveChangesAsync();

        return new
        {
            success = true,
            message = $"Bổ sung xe {vin} vào yêu cầu vận chuyển {req.SRTReqNo} thành công.",
            srtReqNo = req.SRTReqNo,
            totalCars = req.TotalCars
        };
    }

    public async Task<object?> RemoveCarAsync(string srtReqNo, string vin)
    {
        var req = await db.RearrangeTransportRequests
            .Include(x => x.Details)
            .FirstOrDefaultAsync(x => x.OrgId == Org && x.SRTReqNo == srtReqNo.Trim().ToUpperInvariant());

        if (req is null) return null;

        if (req.Status != RearrangeTranspReqStatus.Pending)
            throw new InvalidOperationException($"Chỉ cho phép gỡ xe khi yêu cầu vận chuyển ở trạng thái Chờ duyệt (Pending). Trạng thái hiện tại: {req.Status}.");

        var targetVin = vin.Trim().ToUpperInvariant();
        var dtl = req.Details.FirstOrDefault(d => string.Equals(d.Vin, targetVin, StringComparison.OrdinalIgnoreCase));
        if (dtl is null)
            throw new InvalidOperationException($"Không tìm thấy xe {targetVin} trong yêu cầu vận chuyển {srtReqNo}.");

        req.Details.Remove(dtl);
        db.RearrangeTransportRequestDetails.Remove(dtl);

        // Theo DMS.Sales: khi không còn xe nào thì tự động giải phóng/xóa luôn yêu cầu
        if (req.Details.Count == 0)
        {
            db.RearrangeTransportRequests.Remove(req);
            await db.SaveChangesAsync();
            return new
            {
                success = true,
                message = $"Đã gỡ xe {targetVin}. Do không còn xe nào trong yêu cầu vận chuyển {srtReqNo}, hệ thống đã tự động giải phóng/xóa yêu cầu.",
                deletedRequest = true
            };
        }

        req.TotalCars = req.Details.Count;
        await db.SaveChangesAsync();

        return new
        {
            success = true,
            message = $"Đã gỡ xe {targetVin} khỏi yêu cầu vận chuyển {srtReqNo}.",
            srtReqNo = req.SRTReqNo,
            totalCars = req.TotalCars
        };
    }

    public async Task<object> StatsAsync()
    {
        var total = await db.RearrangeTransportRequests.CountAsync(x => x.OrgId == Org);
        var pending = await db.RearrangeTransportRequests.CountAsync(x => x.OrgId == Org && x.Status == RearrangeTranspReqStatus.Pending);
        var approved = await db.RearrangeTransportRequests.CountAsync(x => x.OrgId == Org && x.Status == RearrangeTranspReqStatus.Approved);
        var rejected = await db.RearrangeTransportRequests.CountAsync(x => x.OrgId == Org && x.Status == RearrangeTranspReqStatus.Rejected);
        var cancelled = await db.RearrangeTransportRequests.CountAsync(x => x.OrgId == Org && x.Status == RearrangeTranspReqStatus.Cancelled);
        var totalCars = await db.RearrangeTransportRequests.Where(x => x.OrgId == Org).SumAsync(x => (int?)x.TotalCars) ?? 0;

        return new
        {
            total,
            pending,
            approved,
            rejected,
            cancelled,
            totalCars
        };
    }
}
