using Microsoft.EntityFrameworkCore;
using MiniSales.Data;
using MiniSales.Models;

namespace MiniSales.Services;

public record CreatePdiRequestDetailDto(
    string Vin,
    string Model,
    string? ModelCode,
    string? SpecCode,
    string? ColorCode,
    string? ColorName,
    string DlrContractNo,
    string CtrCarId,
    string? CustomerName,
    string? CustomerPhone,
    string? DealNo,
    DateTime? DlvExpectedDate,
    string? Remark
);

public record CreatePdiRequestDto(
    string? DlrPdiReqNo,
    string DealerCode,
    string? DealerName,
    bool? FlagAccessory,
    string? Remark,
    string? CreatedBy,
    List<CreatePdiRequestDetailDto>? Details
);

public record UpdatePdiRequestDto(
    bool? FlagAccessory,
    string? Remark,
    string? UpdatedBy
);

public record ApprovePdiRequestDto(
    string? ApprovedBy,
    string? Remark
);

public record SyncPdiRoDto(
    string Vin,
    string RONo,
    DateTime? ROCreatedDate,
    DateTime? ROFinishedDate,
    string? ROStatus,
    string? InspectionResult,
    string? Remark
);

public record CancelPdiRequestDto(
    string CancelReason,
    string? CancelBy
);

public record AddPdiCarDto(
    string Vin,
    string Model,
    string? ModelCode,
    string? SpecCode,
    string? ColorCode,
    string? ColorName,
    string DlrContractNo,
    string CtrCarId,
    string? CustomerName,
    string? CustomerPhone,
    string? DealNo,
    DateTime? DlvExpectedDate,
    string? Remark
);

public record PdiRequestStatsDto(
    int TotalRequests,
    int PendingRequests,
    int ApprovedRequests,
    int CancelledRequests,
    int TotalCars,
    int TotalCarsApproved,
    int TotalCarsPending,
    int AccessoryRequestsCount,
    int ROCompletedCarsCount
);

public interface IPdiRequestService
{
    Task<object> CreateAsync(CreatePdiRequestDto dto);
    Task<object> ListAsync(string? status, string? dealer, string? pdiReqNo, string? vin, string? contractNo, string? roStatus, bool? flagAccessory);
    Task<object?> DetailAsync(string dlrPdiReqNo);
    Task<object> GetEligibleCarsAsync(string? dealerCode, string? contractNo, string? vin);
    Task<object?> UpdateAsync(string dlrPdiReqNo, UpdatePdiRequestDto dto);
    Task<object?> ApproveAsync(string dlrPdiReqNo, ApprovePdiRequestDto? dto);
    Task<object?> SyncROAsync(string dlrPdiReqNo, SyncPdiRoDto dto);
    Task<object?> CancelAsync(string dlrPdiReqNo, CancelPdiRequestDto dto);
    Task<object?> DeleteDraftAsync(string dlrPdiReqNo);
    Task<object?> AddCarAsync(string dlrPdiReqNo, AddPdiCarDto dto);
    Task<object?> RemoveCarAsync(string dlrPdiReqNo, string vin);
    Task<object> StatsAsync();
}

public sealed class PdiRequestService(AppDbContext db, ITenantContext tenant) : IPdiRequestService
{
    private Guid Org => tenant.OrgId;

    public async Task<object> CreateAsync(CreatePdiRequestDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.DealerCode))
            throw new InvalidOperationException("DealerCode (mã đại lý) không được để trống.");

        if (dto.Details is null || dto.Details.Count == 0)
            throw new InvalidOperationException("Yêu cầu PDI phải có ít nhất 1 xe trong danh sách.");

        var dealerCode = dto.DealerCode.Trim().ToUpperInvariant();

        // Kiểm tra trùng lặp VIN trong cùng phiếu
        var vinSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var d in dto.Details)
        {
            if (string.IsNullOrWhiteSpace(d.Vin))
                throw new InvalidOperationException("Số khung VIN của xe không được để trống.");
            if (string.IsNullOrWhiteSpace(d.DlrContractNo))
                throw new InvalidOperationException($"Số hợp đồng bán lẻ của xe VIN {d.Vin} không được để trống.");
            if (string.IsNullOrWhiteSpace(d.CtrCarId))
                throw new InvalidOperationException($"Mã xe theo hợp đồng (CtrCarId) của xe VIN {d.Vin} không được để trống.");

            var cleanVin = d.Vin.Trim().ToUpperInvariant();
            if (!vinSet.Add(cleanVin))
                throw new InvalidOperationException($"Số khung VIN {cleanVin} bị trùng lặp trong danh sách yêu cầu PDI.");
        }

        string reqNo;
        if (!string.IsNullOrWhiteSpace(dto.DlrPdiReqNo))
        {
            reqNo = dto.DlrPdiReqNo.Trim().ToUpperInvariant();
            var exists = await db.PdiRequests.AnyAsync(x => x.OrgId == Org && x.DlrPdiReqNo == reqNo);
            if (exists)
                throw new InvalidOperationException($"Số phiếu yêu cầu PDI {reqNo} đã tồn tại.");
        }
        else
        {
            // Tự sinh số phiếu chuẩn DMS Sales: {yyMM}PRN{seq:D5} (ví dụ: 2609PRN00001)
            var prefix = DateTime.Today.ToString("yyMM") + "PRN";
            var count = await db.PdiRequests.CountAsync(x => x.OrgId == Org && EF.Functions.Like(x.DlrPdiReqNo, prefix + "%"));
            reqNo = $"{prefix}{(count + 1):D5}";
        }

        var header = new PdiRequest
        {
            OrgId = Org,
            DlrPdiReqNo = reqNo,
            DealerCode = dealerCode,
            DealerName = dto.DealerName?.Trim() ?? $"Đại lý {dealerCode}",
            FlagAccessory = dto.FlagAccessory ?? false,
            Status = PdiRequestStatus.Pending,
            TotalCars = dto.Details.Count,
            Remark = dto.Remark?.Trim(),
            CreatedBy = dto.CreatedBy?.Trim() ?? "DEALER_STAFF",
            CreatedDate = DateTime.Now,
            Details = dto.Details.Select(d => new PdiRequestDetail
            {
                DlrPdiReqNo = reqNo,
                Vin = d.Vin.Trim().ToUpperInvariant(),
                Model = d.Model.Trim(),
                ModelCode = d.ModelCode?.Trim(),
                SpecCode = d.SpecCode?.Trim(),
                ColorCode = d.ColorCode?.Trim(),
                ColorName = d.ColorName?.Trim(),
                DlrContractNo = d.DlrContractNo.Trim().ToUpperInvariant(),
                CtrCarId = d.CtrCarId.Trim(),
                CustomerName = d.CustomerName?.Trim(),
                CustomerPhone = d.CustomerPhone?.Trim(),
                DealNo = d.DealNo?.Trim(),
                DlvExpectedDate = d.DlvExpectedDate,
                RONo = null,
                ROCreatedDate = null,
                ROFinishedDate = null,
                ROStatus = "NORE", // Chưa tạo báo giá/lệnh sửa chữa xưởng dịch vụ
                Status = PdiRequestDtlStatus.Pending,
                InspectionResult = null,
                Remark = d.Remark?.Trim()
            }).ToList()
        };

        db.PdiRequests.Add(header);
        await db.SaveChangesAsync();

        return new
        {
            success = true,
            dlrPdiReqNo = header.DlrPdiReqNo,
            dealerCode = header.DealerCode,
            totalCars = header.TotalCars,
            flagAccessory = header.FlagAccessory,
            status = header.Status.ToString(),
            createdDate = header.CreatedDate,
            message = $"Tạo thành công yêu cầu PDI số {header.DlrPdiReqNo} với {header.TotalCars} xe."
        };
    }

    public async Task<object> ListAsync(string? status, string? dealer, string? pdiReqNo, string? vin, string? contractNo, string? roStatus, bool? flagAccessory)
    {
        var q = db.PdiRequests.AsNoTracking().Where(x => x.OrgId == Org);

        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<PdiRequestStatus>(status, true, out var st))
            q = q.Where(x => x.Status == st);

        if (!string.IsNullOrWhiteSpace(dealer))
        {
            var d = dealer.Trim().ToUpperInvariant();
            q = q.Where(x => x.DealerCode == d || x.DealerName.Contains(d));
        }

        if (!string.IsNullOrWhiteSpace(pdiReqNo))
        {
            var no = pdiReqNo.Trim().ToUpperInvariant();
            q = q.Where(x => x.DlrPdiReqNo.Contains(no));
        }

        if (flagAccessory.HasValue)
            q = q.Where(x => x.FlagAccessory == flagAccessory.Value);

        if (!string.IsNullOrWhiteSpace(vin))
        {
            var v = vin.Trim().ToUpperInvariant();
            q = q.Where(x => x.Details.Any(d => d.Vin.Contains(v)));
        }

        if (!string.IsNullOrWhiteSpace(contractNo))
        {
            var c = contractNo.Trim().ToUpperInvariant();
            q = q.Where(x => x.Details.Any(d => d.DlrContractNo.Contains(c)));
        }

        if (!string.IsNullOrWhiteSpace(roStatus))
        {
            var ro = roStatus.Trim().ToUpperInvariant();
            q = q.Where(x => x.Details.Any(d => d.ROStatus == ro));
        }

        var list = await q.OrderByDescending(x => x.CreatedDate)
            .Select(x => new
            {
                x.Id,
                x.DlrPdiReqNo,
                x.DealerCode,
                x.DealerName,
                x.FlagAccessory,
                Status = x.Status.ToString(),
                StatusCode = (int)x.Status,
                x.TotalCars,
                x.Remark,
                x.ApprovedDate,
                x.ApprovedBy,
                x.CancelledDate,
                x.CancelledBy,
                x.CancelReason,
                x.CreatedDate,
                x.CreatedBy,
                Cars = x.Details.Select(d => new
                {
                    d.Id,
                    d.Vin,
                    d.Model,
                    d.SpecCode,
                    d.ColorCode,
                    d.ColorName,
                    d.DlrContractNo,
                    d.CtrCarId,
                    d.CustomerName,
                    d.RONo,
                    d.ROStatus,
                    d.InspectionResult,
                    Status = d.Status.ToString()
                }).ToList()
            })
            .ToListAsync();

        return list;
    }

    public async Task<object?> DetailAsync(string dlrPdiReqNo)
    {
        var reqNo = dlrPdiReqNo.Trim().ToUpperInvariant();
        var item = await db.PdiRequests.AsNoTracking()
            .Include(x => x.Details)
            .FirstOrDefaultAsync(x => x.OrgId == Org && x.DlrPdiReqNo == reqNo);

        if (item is null) return null;

        return new
        {
            item.Id,
            item.DlrPdiReqNo,
            item.DealerCode,
            item.DealerName,
            item.FlagAccessory,
            Status = item.Status.ToString(),
            StatusCode = (int)item.Status,
            item.TotalCars,
            item.Remark,
            item.ApprovedDate,
            item.ApprovedBy,
            item.CancelledDate,
            item.CancelledBy,
            item.CancelReason,
            item.CreatedDate,
            item.CreatedBy,
            item.LUDateTime,
            item.LUBy,
            Details = item.Details.Select(d => new
            {
                d.Id,
                d.Vin,
                d.Model,
                d.ModelCode,
                d.SpecCode,
                d.ColorCode,
                d.ColorName,
                d.DlrContractNo,
                d.CtrCarId,
                d.CustomerName,
                d.CustomerPhone,
                d.DealNo,
                d.DlvExpectedDate,
                d.RONo,
                d.ROCreatedDate,
                d.ROFinishedDate,
                d.ROStatus,
                Status = d.Status.ToString(),
                StatusCode = (int)d.Status,
                d.InspectionResult,
                d.Remark
            }).ToList()
        };
    }

    public async Task<object> GetEligibleCarsAsync(string? dealerCode, string? contractNo, string? vin)
    {
        // Truy vấn xe từ Hợp đồng bán lẻ đại lý (Dlr_ContractCar): xe đã có số khung VIN, hợp đồng đã duyệt và chưa bàn giao
        var q = db.RetailContracts.AsNoTracking()
            .Where(c => c.OrgId == Org && c.Status == RetailContractStatus.Approved);

        if (!string.IsNullOrWhiteSpace(dealerCode))
        {
            var d = dealerCode.Trim().ToUpperInvariant();
            q = q.Where(c => c.DealerCode == d);
        }

        if (!string.IsNullOrWhiteSpace(contractNo))
        {
            var no = contractNo.Trim().ToUpperInvariant();
            q = q.Where(c => c.ContractNo.Contains(no));
        }

        var contracts = await q.Include(c => c.Cars).ToListAsync();

        var cars = contracts.SelectMany(c => c.Cars.Where(car => car.Status == RetailContractCarStatus.Pending && !string.IsNullOrWhiteSpace(car.Vin))
            .Select(car => new
            {
                ContractNo = c.ContractNo,
                DealerCode = c.DealerCode,
                DealerName = c.DealerName,
                CustomerName = c.CustomerName,
                CustomerPhone = c.CustomerPhone,
                car.CtrCarId,
                car.Vin,
                car.Model,
                car.SpecCode,
                car.ColorCode,
                Status = car.Status.ToString()
            })).ToList();

        if (!string.IsNullOrWhiteSpace(vin))
        {
            var v = vin.Trim().ToUpperInvariant();
            cars = cars.Where(c => c.Vin!.Contains(v)).ToList();
        }

        return cars;
    }

    public async Task<object?> UpdateAsync(string dlrPdiReqNo, UpdatePdiRequestDto dto)
    {
        var reqNo = dlrPdiReqNo.Trim().ToUpperInvariant();
        var item = await db.PdiRequests.FirstOrDefaultAsync(x => x.OrgId == Org && x.DlrPdiReqNo == reqNo);
        if (item is null) return null;

        if (item.Status != PdiRequestStatus.Pending)
            throw new InvalidOperationException($"Chỉ có thể cập nhật phiếu PDI ở trạng thái chờ duyệt (Pending). Trạng thái hiện tại: {item.Status}.");

        if (dto.FlagAccessory.HasValue)
            item.FlagAccessory = dto.FlagAccessory.Value;

        if (dto.Remark is not null)
            item.Remark = dto.Remark.Trim();

        item.LUDateTime = DateTime.Now;
        item.LUBy = dto.UpdatedBy?.Trim() ?? "DEALER_STAFF";

        await db.SaveChangesAsync();

        return new
        {
            success = true,
            dlrPdiReqNo = item.DlrPdiReqNo,
            flagAccessory = item.FlagAccessory,
            remark = item.Remark,
            updatedAt = item.LUDateTime,
            message = $"Cập nhật thông tin phiếu PDI {item.DlrPdiReqNo} thành công."
        };
    }

    public async Task<object?> ApproveAsync(string dlrPdiReqNo, ApprovePdiRequestDto? dto)
    {
        var reqNo = dlrPdiReqNo.Trim().ToUpperInvariant();
        var item = await db.PdiRequests
            .Include(x => x.Details)
            .FirstOrDefaultAsync(x => x.OrgId == Org && x.DlrPdiReqNo == reqNo);

        if (item is null) return null;

        if (item.Status != PdiRequestStatus.Pending)
            throw new InvalidOperationException($"Chỉ có thể duyệt nghiệm thu phiếu PDI ở trạng thái chờ duyệt (Pending). Trạng thái hiện tại: {item.Status}.");

        item.Status = PdiRequestStatus.Approved;
        item.ApprovedDate = DateTime.Now;
        item.ApprovedBy = dto?.ApprovedBy?.Trim() ?? "NPP_PDI_DIRECTOR";
        if (!string.IsNullOrWhiteSpace(dto?.Remark))
            item.Remark = string.IsNullOrWhiteSpace(item.Remark) ? dto.Remark.Trim() : $"{item.Remark} | Duyệt: {dto.Remark.Trim()}";

        item.LUDateTime = DateTime.Now;
        item.LUBy = item.ApprovedBy;

        // Đồng bộ toàn bộ dòng xe trong phiếu sang Approved / PASSED
        foreach (var detail in item.Details)
        {
            detail.Status = PdiRequestDtlStatus.Approved;
            if (string.IsNullOrWhiteSpace(detail.InspectionResult))
                detail.InspectionResult = "PASSED";
        }

        await db.SaveChangesAsync();

        return new
        {
            success = true,
            dlrPdiReqNo = item.DlrPdiReqNo,
            status = item.Status.ToString(),
            approvedDate = item.ApprovedDate,
            approvedBy = item.ApprovedBy,
            totalCarsApproved = item.Details.Count,
            message = $"Duyệt nghiệm thu PDI thành công cho phiếu {item.DlrPdiReqNo} ({item.Details.Count} xe đạt chuẩn)."
        };
    }

    public async Task<object?> SyncROAsync(string dlrPdiReqNo, SyncPdiRoDto dto)
    {
        var reqNo = dlrPdiReqNo.Trim().ToUpperInvariant();
        var item = await db.PdiRequests
            .Include(x => x.Details)
            .FirstOrDefaultAsync(x => x.OrgId == Org && x.DlrPdiReqNo == reqNo);

        if (item is null) return null;

        var vin = dto.Vin.Trim().ToUpperInvariant();
        var detail = item.Details.FirstOrDefault(d => d.Vin.Equals(vin, StringComparison.OrdinalIgnoreCase));
        if (detail is null)
            throw new InvalidOperationException($"Không tìm thấy xe có số khung VIN {vin} trong phiếu PDI {reqNo}.");

        detail.RONo = dto.RONo.Trim();
        detail.ROCreatedDate = dto.ROCreatedDate ?? DateTime.Now;
        detail.ROFinishedDate = dto.ROFinishedDate ?? DateTime.Now;
        detail.ROStatus = string.IsNullOrWhiteSpace(dto.ROStatus) ? "COMPLETED" : dto.ROStatus.Trim().ToUpperInvariant();
        detail.InspectionResult = string.IsNullOrWhiteSpace(dto.InspectionResult) ? "PASSED" : dto.InspectionResult.Trim().ToUpperInvariant();

        if (!string.IsNullOrWhiteSpace(dto.Remark))
            detail.Remark = string.IsNullOrWhiteSpace(detail.Remark) ? dto.Remark.Trim() : $"{detail.Remark} | RO: {dto.Remark.Trim()}";

        if (detail.InspectionResult == "PASSED")
            detail.Status = PdiRequestDtlStatus.Approved;

        item.LUDateTime = DateTime.Now;
        item.LUBy = "SERVICE_RO_SYNC";

        await db.SaveChangesAsync();

        return new
        {
            success = true,
            dlrPdiReqNo = item.DlrPdiReqNo,
            vin = detail.Vin,
            roNo = detail.RONo,
            roStatus = detail.ROStatus,
            inspectionResult = detail.InspectionResult,
            detailStatus = detail.Status.ToString(),
            message = $"Đồng bộ lệnh dịch vụ RO {detail.RONo} cho xe VIN {detail.Vin} thành công."
        };
    }

    public async Task<object?> CancelAsync(string dlrPdiReqNo, CancelPdiRequestDto dto)
    {
        var reqNo = dlrPdiReqNo.Trim().ToUpperInvariant();
        var item = await db.PdiRequests
            .Include(x => x.Details)
            .FirstOrDefaultAsync(x => x.OrgId == Org && x.DlrPdiReqNo == reqNo);

        if (item is null) return null;

        if (item.Status == PdiRequestStatus.Cancelled)
            throw new InvalidOperationException($"Phiếu PDI {reqNo} đã bị hủy trước đó.");

        if (string.IsNullOrWhiteSpace(dto.CancelReason))
            throw new InvalidOperationException("Lý do hủy yêu cầu PDI (CancelReason) không được để trống.");

        item.Status = PdiRequestStatus.Cancelled;
        item.CancelledDate = DateTime.Now;
        item.CancelledBy = dto.CancelBy?.Trim() ?? "DEALER_USER";
        item.CancelReason = dto.CancelReason.Trim();

        item.LUDateTime = DateTime.Now;
        item.LUBy = item.CancelledBy;

        foreach (var detail in item.Details)
        {
            detail.Status = PdiRequestDtlStatus.Cancelled;
        }

        await db.SaveChangesAsync();

        return new
        {
            success = true,
            dlrPdiReqNo = item.DlrPdiReqNo,
            status = item.Status.ToString(),
            cancelledDate = item.CancelledDate,
            cancelledBy = item.CancelledBy,
            cancelReason = item.CancelReason,
            message = $"Hủy yêu cầu PDI {item.DlrPdiReqNo} thành công."
        };
    }

    public async Task<object?> DeleteDraftAsync(string dlrPdiReqNo)
    {
        var reqNo = dlrPdiReqNo.Trim().ToUpperInvariant();
        var item = await db.PdiRequests
            .Include(x => x.Details)
            .FirstOrDefaultAsync(x => x.OrgId == Org && x.DlrPdiReqNo == reqNo);

        if (item is null) return null;

        if (item.Status != PdiRequestStatus.Pending)
            throw new InvalidOperationException($"Chỉ có thể xóa phiếu PDI khi còn ở trạng thái nháp / chờ duyệt (Pending). Trạng thái hiện tại: {item.Status}.");

        db.PdiRequests.Remove(item);
        await db.SaveChangesAsync();

        return new
        {
            success = true,
            dlrPdiReqNo = reqNo,
            message = $"Đã xóa phiếu yêu cầu PDI nháp {reqNo} thành công."
        };
    }

    public async Task<object?> AddCarAsync(string dlrPdiReqNo, AddPdiCarDto dto)
    {
        var reqNo = dlrPdiReqNo.Trim().ToUpperInvariant();
        var item = await db.PdiRequests
            .Include(x => x.Details)
            .FirstOrDefaultAsync(x => x.OrgId == Org && x.DlrPdiReqNo == reqNo);

        if (item is null) return null;

        if (item.Status != PdiRequestStatus.Pending)
            throw new InvalidOperationException($"Chỉ có thể thêm xe vào phiếu PDI ở trạng thái chờ duyệt (Pending). Trạng thái hiện tại: {item.Status}.");

        var cleanVin = dto.Vin.Trim().ToUpperInvariant();
        if (item.Details.Any(d => d.Vin.Equals(cleanVin, StringComparison.OrdinalIgnoreCase)))
            throw new InvalidOperationException($"Xe có số khung VIN {cleanVin} đã tồn tại trong phiếu PDI {reqNo}.");

        var detail = new PdiRequestDetail
        {
            PdiRequestId = item.Id,
            DlrPdiReqNo = reqNo,
            Vin = cleanVin,
            Model = dto.Model.Trim(),
            ModelCode = dto.ModelCode?.Trim(),
            SpecCode = dto.SpecCode?.Trim(),
            ColorCode = dto.ColorCode?.Trim(),
            ColorName = dto.ColorName?.Trim(),
            DlrContractNo = dto.DlrContractNo.Trim().ToUpperInvariant(),
            CtrCarId = dto.CtrCarId.Trim(),
            CustomerName = dto.CustomerName?.Trim(),
            CustomerPhone = dto.CustomerPhone?.Trim(),
            DealNo = dto.DealNo?.Trim(),
            DlvExpectedDate = dto.DlvExpectedDate,
            RONo = null,
            ROCreatedDate = null,
            ROFinishedDate = null,
            ROStatus = "NORE",
            Status = PdiRequestDtlStatus.Pending,
            InspectionResult = null,
            Remark = dto.Remark?.Trim()
        };

        item.Details.Add(detail);
        item.TotalCars = item.Details.Count;
        item.LUDateTime = DateTime.Now;
        item.LUBy = "DEALER_STAFF";

        await db.SaveChangesAsync();

        return new
        {
            success = true,
            dlrPdiReqNo = reqNo,
            vin = detail.Vin,
            model = detail.Model,
            totalCars = item.TotalCars,
            message = $"Thêm xe VIN {detail.Vin} vào phiếu PDI {reqNo} thành công."
        };
    }

    public async Task<object?> RemoveCarAsync(string dlrPdiReqNo, string vin)
    {
        var reqNo = dlrPdiReqNo.Trim().ToUpperInvariant();
        var item = await db.PdiRequests
            .Include(x => x.Details)
            .FirstOrDefaultAsync(x => x.OrgId == Org && x.DlrPdiReqNo == reqNo);

        if (item is null) return null;

        if (item.Status != PdiRequestStatus.Pending)
            throw new InvalidOperationException($"Chỉ có thể gỡ xe khỏi phiếu PDI ở trạng thái chờ duyệt (Pending). Trạng thái hiện tại: {item.Status}.");

        var cleanVin = vin.Trim().ToUpperInvariant();
        var detail = item.Details.FirstOrDefault(d => d.Vin.Equals(cleanVin, StringComparison.OrdinalIgnoreCase));
        if (detail is null)
            throw new InvalidOperationException($"Không tìm thấy xe có số khung VIN {cleanVin} trong phiếu PDI {reqNo}.");

        if (item.Details.Count <= 1)
            throw new InvalidOperationException("Phiếu PDI phải có ít nhất 1 xe. Vui lòng xóa phiếu nếu muốn gỡ toàn bộ xe.");

        item.Details.Remove(detail);
        db.PdiRequestDetails.Remove(detail);
        item.TotalCars = item.Details.Count;
        item.LUDateTime = DateTime.Now;
        item.LUBy = "DEALER_STAFF";

        await db.SaveChangesAsync();

        return new
        {
            success = true,
            dlrPdiReqNo = reqNo,
            vin = cleanVin,
            totalCars = item.TotalCars,
            message = $"Gỡ xe VIN {cleanVin} khỏi phiếu PDI {reqNo} thành công."
        };
    }

    public async Task<object> StatsAsync()
    {
        var q = db.PdiRequests.AsNoTracking().Where(x => x.OrgId == Org);

        var total = await q.CountAsync();
        var pending = await q.CountAsync(x => x.Status == PdiRequestStatus.Pending);
        var approved = await q.CountAsync(x => x.Status == PdiRequestStatus.Approved);
        var cancelled = await q.CountAsync(x => x.Status == PdiRequestStatus.Cancelled);
        var accessory = await q.CountAsync(x => x.FlagAccessory);

        var detailsQ = db.PdiRequestDetails.AsNoTracking().Where(d => db.PdiRequests.Any(p => p.Id == d.PdiRequestId && p.OrgId == Org));
        var totalCars = await detailsQ.CountAsync();
        var carsApproved = await detailsQ.CountAsync(d => d.Status == PdiRequestDtlStatus.Approved);
        var carsPending = await detailsQ.CountAsync(d => d.Status == PdiRequestDtlStatus.Pending);
        var roCompleted = await detailsQ.CountAsync(d => d.ROStatus == "COMPLETED");

        return new PdiRequestStatsDto(
            TotalRequests: total,
            PendingRequests: pending,
            ApprovedRequests: approved,
            CancelledRequests: cancelled,
            TotalCars: totalCars,
            TotalCarsApproved: carsApproved,
            TotalCarsPending: carsPending,
            AccessoryRequestsCount: accessory,
            ROCompletedCarsCount: roCompleted
        );
    }
}
