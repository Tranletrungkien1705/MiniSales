using Microsoft.EntityFrameworkCore;
using MiniSales.Data;
using MiniSales.Models;

namespace MiniSales.Services;

public record CarBodyRequestItemInputDto(
    string CarId,
    string Vin,
    string Model,
    string? ModelCode = null,
    string? SpecCode = null,
    string? SpecDescription = null,
    string? ColorCode = null,
    string? ColorName = null,
    string? EngineNo = null,
    string StorageCodeFrom = "KHO_TONG_HN",
    string? StorageNameFrom = null,
    string StorageCodeTo = "KHO_DT_01",
    string? StorageNameTo = null,
    string LoaiThung = "Thùng mui bạt",
    string? ActualSpec = null,
    string? Remark = null
);

public record CreateCarBodyRequestDto(
    string? CBReqNo,
    string StorageCodeTo,
    string? StorageNameTo = null,
    string? Remark = null,
    string? CreatedBy = null,
    string? LoaiThung = null,
    List<CarBodyRequestItemInputDto>? Items = null,
    List<string>? Vins = null
);

public record UpdateCarBodyRequestDto(
    string? StorageCodeTo,
    string? StorageNameTo,
    string? Remark,
    string? UpdatedBy
);

public record UpdateCarBodyDetailLineDto(
    string? StorageCodeTo,
    string? StorageNameTo,
    string? LoaiThung,
    string? ActualSpec,
    string? Remark
);

public record AddCarBodyDetailLineDto(
    string CarId,
    string Vin,
    string Model,
    string? ModelCode = null,
    string? SpecCode = null,
    string? SpecDescription = null,
    string? ColorCode = null,
    string? ColorName = null,
    string? EngineNo = null,
    string StorageCodeFrom = "KHO_TONG_HN",
    string? StorageNameFrom = null,
    string StorageCodeTo = "KHO_DT_01",
    string? StorageNameTo = null,
    string LoaiThung = "Thùng mui bạt",
    string? ActualSpec = null,
    string? Remark = null
);

public record ApproveCarBodyRequestDto(
    string? ApprovedBy,
    string? Remark
);

public record RejectCarBodyRequestDto(
    string RejectReason,
    string? RejectedBy
);

public record CancelCarBodyRequestDto(
    string CancelReason,
    string? CancelledBy
);

public record CompleteCarBodyRequestDto(
    string? CompletedBy,
    string? InspectionResult,
    string? SerialNoPrefix,
    string? Remark
);

public record EligibleCarForBodyBuildingDto(
    string CarId,
    string Vin,
    string Model,
    string? ModelCode,
    string? SpecCode,
    string? SpecDescription,
    string? ColorCode,
    string? ColorName,
    string? EngineNo,
    string StorageCodeCurrent,
    string StorageNameCurrent,
    string TypeCB
);

public record CarBodyRequestStatsDto(
    int TotalRequests,
    int PendingCount,
    int ApprovedCount,
    int CompletedCount,
    int RejectedCount,
    int CancelledCount,
    int TotalCars,
    int DistinctWorkshopsCount,
    int ConvertedCarsCount,
    Dictionary<string, int> BodyTypeBreakdown
);

public interface ICarBodyRequestService
{
    Task<CarBodyRequest> CreateAsync(CreateCarBodyRequestDto dto);
    Task<List<CarBodyRequest>> ListAsync(
        string? status = null,
        string? vin = null,
        string? storageCodeTo = null,
        string? cbReqNo = null,
        DateTime? dateFrom = null,
        DateTime? dateTo = null
    );
    Task<CarBodyRequest?> DetailAsync(string cbReqNo);
    Task<CarBodyRequest?> UpdateAsync(string cbReqNo, UpdateCarBodyRequestDto dto);
    Task<bool> DeleteDraftAsync(string cbReqNo);
    Task<CarBodyRequest?> DetailUpdateAsync(string cbReqNo, string vin, UpdateCarBodyDetailLineDto dto);
    Task<CarBodyRequest?> AddCarsAsync(string cbReqNo, List<AddCarBodyDetailLineDto> items);
    Task<CarBodyRequest?> RemoveCarAsync(string cbReqNo, string vin);
    Task<CarBodyRequest?> ApproveAsync(string cbReqNo, ApproveCarBodyRequestDto? dto);
    Task<CarBodyRequest?> RejectAsync(string cbReqNo, RejectCarBodyRequestDto dto);
    Task<CarBodyRequest?> CancelAsync(string cbReqNo, CancelCarBodyRequestDto dto);
    Task<CarBodyRequest?> CompleteAsync(string cbReqNo, CompleteCarBodyRequestDto? dto);
    Task<List<EligibleCarForBodyBuildingDto>> GetEligibleCarsAsync(string? storageCode = null);
    Task<CarBodyRequestStatsDto> StatsAsync();
}

public class CarBodyRequestService(AppDbContext db, ITenantContext tenant) : ICarBodyRequestService
{
    public async Task<CarBodyRequest> CreateAsync(CreateCarBodyRequestDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.StorageCodeTo))
            throw new InvalidOperationException("Kho/Xưởng đóng thùng đích (StorageCodeTo) không được để trống.");

        var storageCodeTo = dto.StorageCodeTo.Trim().ToUpperInvariant();
        var storageNameTo = !string.IsNullOrWhiteSpace(dto.StorageNameTo)
            ? dto.StorageNameTo.Trim()
            : ResolveStorageName(storageCodeTo);

        // Sinh số yêu cầu nếu chưa có: {yyMM}CBR{seq:D6} (DMS.Sales Sto_CBReq GetSeqForStoCBReq)
        var cbReqNo = dto.CBReqNo?.Trim();
        if (string.IsNullOrWhiteSpace(cbReqNo))
        {
            var yyMM = DateTime.Now.ToString("yyMM");
            var prefix = $"{yyMM}CBR";
            var count = await db.BodyRequests.CountAsync(x => x.OrgId == tenant.OrgId && x.CBReqNo.StartsWith(prefix));
            var seq = count + 1;
            do
            {
                cbReqNo = $"{prefix}{seq:D6}";
                seq++;
            } while (await db.BodyRequests.AnyAsync(x => x.OrgId == tenant.OrgId && x.CBReqNo == cbReqNo));
        }
        else
        {
            if (await db.BodyRequests.AnyAsync(x => x.OrgId == tenant.OrgId && x.CBReqNo == cbReqNo))
                throw new InvalidOperationException($"Số yêu cầu đóng thùng {cbReqNo} đã tồn tại trên hệ thống.");
        }

        // Chuẩn bị danh sách xe
        var details = new List<CarBodyRequestDetail>();
        var seenVins = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        if (dto.Items != null && dto.Items.Count > 0)
        {
            foreach (var item in dto.Items)
            {
                var vin = item.Vin.Trim().ToUpperInvariant();
                if (string.IsNullOrWhiteSpace(vin))
                    throw new InvalidOperationException("Số khung VIN không được để trống.");

                if (!seenVins.Add(vin))
                    throw new InvalidOperationException($"Số VIN {vin} bị trùng lặp trong danh sách yêu cầu.");

                var storageFrom = string.IsNullOrWhiteSpace(item.StorageCodeFrom) ? "KHO_TONG_HN" : item.StorageCodeFrom.Trim().ToUpperInvariant();
                var storageTo = string.IsNullOrWhiteSpace(item.StorageCodeTo) ? storageCodeTo : item.StorageCodeTo.Trim().ToUpperInvariant();

                if (string.Equals(storageFrom, storageTo, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException($"VIN {vin}: Kho xuất ({storageFrom}) không được trùng kho/xưởng nhận đóng thùng ({storageTo}).");

                await CheckVinAvailableForBodyRequestAsync(vin);

                details.Add(new CarBodyRequestDetail
                {
                    CBReqNo = cbReqNo,
                    CarId = !string.IsNullOrWhiteSpace(item.CarId) ? item.CarId.Trim() : $"CAR-{vin}",
                    Vin = vin,
                    Model = item.Model.Trim(),
                    ModelCode = item.ModelCode?.Trim(),
                    SpecCode = item.SpecCode?.Trim(),
                    SpecDescription = item.SpecDescription?.Trim(),
                    ColorCode = item.ColorCode?.Trim(),
                    ColorName = item.ColorName?.Trim(),
                    EngineNo = item.EngineNo?.Trim(),
                    StorageCodeFrom = storageFrom,
                    StorageNameFrom = !string.IsNullOrWhiteSpace(item.StorageNameFrom) ? item.StorageNameFrom.Trim() : ResolveStorageName(storageFrom),
                    StorageCodeTo = storageTo,
                    StorageNameTo = !string.IsNullOrWhiteSpace(item.StorageNameTo) ? item.StorageNameTo.Trim() : ResolveStorageName(storageTo),
                    LoaiThung = !string.IsNullOrWhiteSpace(item.LoaiThung) ? item.LoaiThung.Trim() : (dto.LoaiThung?.Trim() ?? "Thùng mui bạt"),
                    ActualSpec = item.ActualSpec?.Trim(),
                    TypeCB = "N",
                    Status = CarBodyRequestDtlStatus.Pending,
                    Remark = item.Remark?.Trim()
                });
            }
        }
        else if (dto.Vins != null && dto.Vins.Count > 0)
        {
            var eligibleMap = (await GetEligibleCarsInternalAsync())
                .ToDictionary(x => x.Vin, StringComparer.OrdinalIgnoreCase);

            foreach (var rawVin in dto.Vins)
            {
                var vin = rawVin.Trim().ToUpperInvariant();
                if (string.IsNullOrWhiteSpace(vin)) continue;

                if (!seenVins.Add(vin))
                    throw new InvalidOperationException($"Số VIN {vin} bị trùng lặp trong danh sách.");

                await CheckVinAvailableForBodyRequestAsync(vin);

                if (eligibleMap.TryGetValue(vin, out var car))
                {
                    var storageFrom = car.StorageCodeCurrent;
                    if (string.Equals(storageFrom, storageCodeTo, StringComparison.OrdinalIgnoreCase))
                        throw new InvalidOperationException($"VIN {vin}: Kho xuất ({storageFrom}) không được trùng xưởng đóng thùng ({storageCodeTo}).");

                    details.Add(new CarBodyRequestDetail
                    {
                        CBReqNo = cbReqNo,
                        CarId = car.CarId,
                        Vin = vin,
                        Model = car.Model,
                        ModelCode = car.ModelCode,
                        SpecCode = car.SpecCode,
                        SpecDescription = car.SpecDescription,
                        ColorCode = car.ColorCode,
                        ColorName = car.ColorName,
                        EngineNo = car.EngineNo,
                        StorageCodeFrom = storageFrom,
                        StorageNameFrom = car.StorageNameCurrent,
                        StorageCodeTo = storageCodeTo,
                        StorageNameTo = storageNameTo,
                        LoaiThung = dto.LoaiThung?.Trim() ?? "Thùng mui bạt",
                        TypeCB = "N",
                        Status = CarBodyRequestDtlStatus.Pending
                    });
                }
                else
                {
                    details.Add(new CarBodyRequestDetail
                    {
                        CBReqNo = cbReqNo,
                        CarId = $"CAR-{vin}",
                        Vin = vin,
                        Model = "Hyundai Commercial Truck",
                        StorageCodeFrom = "KHO_TONG_HN",
                        StorageNameFrom = ResolveStorageName("KHO_TONG_HN"),
                        StorageCodeTo = storageCodeTo,
                        StorageNameTo = storageNameTo,
                        LoaiThung = dto.LoaiThung?.Trim() ?? "Thùng mui bạt",
                        TypeCB = "N",
                        Status = CarBodyRequestDtlStatus.Pending
                    });
                }
            }
        }

        if (details.Count == 0)
            throw new InvalidOperationException("Yêu cầu đóng thùng phải chứa ít nhất 1 xe ô tô sắt xi.");

        var entity = new CarBodyRequest
        {
            OrgId = tenant.OrgId,
            CBReqNo = cbReqNo,
            Status = CarBodyRequestStatus.Pending,
            TotalCars = details.Count,
            StorageCodeTo = storageCodeTo,
            StorageNameTo = storageNameTo,
            Remark = dto.Remark?.Trim(),
            CreatedBy = dto.CreatedBy?.Trim() ?? "NPP_PLANNING",
            CreatedDate = DateTime.Now,
            Details = details
        };

        db.BodyRequests.Add(entity);
        await db.SaveChangesAsync();

        return entity;
    }

    public async Task<List<CarBodyRequest>> ListAsync(
        string? status = null,
        string? vin = null,
        string? storageCodeTo = null,
        string? cbReqNo = null,
        DateTime? dateFrom = null,
        DateTime? dateTo = null)
    {
        var q = db.BodyRequests
            .Include(x => x.Details)
            .Where(x => x.OrgId == tenant.OrgId)
            .AsNoTracking();

        if (!string.IsNullOrWhiteSpace(status))
        {
            if (Enum.TryParse<CarBodyRequestStatus>(status, true, out var st))
                q = q.Where(x => x.Status == st);
            else if (status.Equals("P", StringComparison.OrdinalIgnoreCase))
                q = q.Where(x => x.Status == CarBodyRequestStatus.Pending);
            else if (status.Equals("A", StringComparison.OrdinalIgnoreCase))
                q = q.Where(x => x.Status == CarBodyRequestStatus.Approved);
            else if (status.Equals("D", StringComparison.OrdinalIgnoreCase))
                q = q.Where(x => x.Status == CarBodyRequestStatus.Completed);
            else if (status.Equals("R", StringComparison.OrdinalIgnoreCase))
                q = q.Where(x => x.Status == CarBodyRequestStatus.Rejected);
            else if (status.Equals("C", StringComparison.OrdinalIgnoreCase))
                q = q.Where(x => x.Status == CarBodyRequestStatus.Cancelled);
        }

        if (!string.IsNullOrWhiteSpace(cbReqNo))
        {
            var no = cbReqNo.Trim().ToLowerInvariant();
            q = q.Where(x => x.CBReqNo.ToLower().Contains(no));
        }

        if (!string.IsNullOrWhiteSpace(storageCodeTo))
        {
            var sc = storageCodeTo.Trim().ToUpperInvariant();
            q = q.Where(x => x.StorageCodeTo.ToUpper().Contains(sc));
        }

        if (!string.IsNullOrWhiteSpace(vin))
        {
            var v = vin.Trim().ToUpperInvariant();
            q = q.Where(x => x.Details.Any(d => d.Vin.ToUpper().Contains(v)));
        }

        if (dateFrom.HasValue)
            q = q.Where(x => x.CreatedDate >= dateFrom.Value.Date);

        if (dateTo.HasValue)
            q = q.Where(x => x.CreatedDate <= dateTo.Value.Date.AddDays(1).AddTicks(-1));

        return await q.OrderByDescending(x => x.CreatedDate).ToListAsync();
    }

    public async Task<CarBodyRequest?> DetailAsync(string cbReqNo)
    {
        if (string.IsNullOrWhiteSpace(cbReqNo)) return null;
        var code = cbReqNo.Trim().ToUpperInvariant();

        return await db.BodyRequests
            .Include(x => x.Details)
            .FirstOrDefaultAsync(x => x.OrgId == tenant.OrgId && x.CBReqNo.ToUpper() == code);
    }

    public async Task<CarBodyRequest?> UpdateAsync(string cbReqNo, UpdateCarBodyRequestDto dto)
    {
        var entity = await DetailAsync(cbReqNo);
        if (entity is null) return null;

        if (entity.Status != CarBodyRequestStatus.Pending)
            throw new InvalidOperationException($"Chỉ được sửa yêu cầu đóng thùng ở trạng thái Chờ duyệt (Pending). Trạng thái hiện tại: {entity.Status}.");

        if (!string.IsNullOrWhiteSpace(dto.StorageCodeTo))
        {
            var newStorageTo = dto.StorageCodeTo.Trim().ToUpperInvariant();
            entity.StorageCodeTo = newStorageTo;
            entity.StorageNameTo = !string.IsNullOrWhiteSpace(dto.StorageNameTo)
                ? dto.StorageNameTo.Trim()
                : ResolveStorageName(newStorageTo);

            // Cập nhật lại kho đến cho các dòng xe chưa tùy biến kho riêng
            foreach (var d in entity.Details)
            {
                if (string.Equals(d.StorageCodeFrom, newStorageTo, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException($"VIN {d.Vin}: Kho xuất ({d.StorageCodeFrom}) không được trùng xưởng đóng thùng mới ({newStorageTo}).");

                d.StorageCodeTo = newStorageTo;
                d.StorageNameTo = entity.StorageNameTo;
            }
        }

        if (dto.Remark != null)
            entity.Remark = dto.Remark.Trim();

        entity.LUDateTime = DateTime.Now;
        entity.LUBy = dto.UpdatedBy?.Trim() ?? "NPP_USER";

        await db.SaveChangesAsync();
        return entity;
    }

    public async Task<bool> DeleteDraftAsync(string cbReqNo)
    {
        var entity = await DetailAsync(cbReqNo);
        if (entity is null) return false;

        if (entity.Status != CarBodyRequestStatus.Pending)
            throw new InvalidOperationException($"Chỉ được xóa yêu cầu đóng thùng ở trạng thái Chờ duyệt (Pending). Trạng thái hiện tại: {entity.Status}.");

        db.BodyRequests.Remove(entity);
        await db.SaveChangesAsync();
        return true;
    }

    public async Task<CarBodyRequest?> DetailUpdateAsync(string cbReqNo, string vin, UpdateCarBodyDetailLineDto dto)
    {
        var entity = await DetailAsync(cbReqNo);
        if (entity is null) return null;

        if (entity.Status != CarBodyRequestStatus.Pending)
            throw new InvalidOperationException($"Chỉ được cập nhật chi tiết xe khi yêu cầu đang Chờ duyệt (Pending). Trạng thái hiện tại: {entity.Status}.");

        var dtl = entity.Details.FirstOrDefault(x => string.Equals(x.Vin, vin.Trim(), StringComparison.OrdinalIgnoreCase));
        if (dtl is null)
            throw new InvalidOperationException($"Không tìm thấy xe VIN {vin} trong yêu cầu đóng thùng {cbReqNo}.");

        if (!string.IsNullOrWhiteSpace(dto.StorageCodeTo))
        {
            var codeTo = dto.StorageCodeTo.Trim().ToUpperInvariant();
            if (string.Equals(dtl.StorageCodeFrom, codeTo, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException($"Kho xuất ({dtl.StorageCodeFrom}) không được trùng kho/xưởng nhận đóng thùng ({codeTo}).");

            dtl.StorageCodeTo = codeTo;
            dtl.StorageNameTo = !string.IsNullOrWhiteSpace(dto.StorageNameTo)
                ? dto.StorageNameTo.Trim()
                : ResolveStorageName(codeTo);
        }

        if (!string.IsNullOrWhiteSpace(dto.LoaiThung))
            dtl.LoaiThung = dto.LoaiThung.Trim();

        if (dto.ActualSpec != null)
            dtl.ActualSpec = dto.ActualSpec.Trim();

        if (dto.Remark != null)
            dtl.Remark = dto.Remark.Trim();

        entity.LUDateTime = DateTime.Now;
        entity.LUBy = "NPP_USER";

        await db.SaveChangesAsync();
        return entity;
    }

    public async Task<CarBodyRequest?> AddCarsAsync(string cbReqNo, List<AddCarBodyDetailLineDto> items)
    {
        var entity = await DetailAsync(cbReqNo);
        if (entity is null) return null;

        if (entity.Status != CarBodyRequestStatus.Pending)
            throw new InvalidOperationException($"Chỉ được thêm xe vào yêu cầu đóng thùng khi còn ở trạng thái Chờ duyệt (Pending). Trạng thái hiện tại: {entity.Status}.");

        var existingVins = entity.Details.Select(x => x.Vin).ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var item in items)
        {
            var vin = item.Vin.Trim().ToUpperInvariant();
            if (existingVins.Contains(vin))
                throw new InvalidOperationException($"VIN {vin} đã có trong yêu cầu đóng thùng này.");

            await CheckVinAvailableForBodyRequestAsync(vin);

            var storageFrom = string.IsNullOrWhiteSpace(item.StorageCodeFrom) ? "KHO_TONG_HN" : item.StorageCodeFrom.Trim().ToUpperInvariant();
            var storageTo = string.IsNullOrWhiteSpace(item.StorageCodeTo) ? entity.StorageCodeTo : item.StorageCodeTo.Trim().ToUpperInvariant();

            if (string.Equals(storageFrom, storageTo, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException($"VIN {vin}: Kho xuất ({storageFrom}) không được trùng xưởng đóng thùng ({storageTo}).");

            entity.Details.Add(new CarBodyRequestDetail
            {
                CBReqNo = entity.CBReqNo,
                CarId = !string.IsNullOrWhiteSpace(item.CarId) ? item.CarId.Trim() : $"CAR-{vin}",
                Vin = vin,
                Model = item.Model.Trim(),
                ModelCode = item.ModelCode?.Trim(),
                SpecCode = item.SpecCode?.Trim(),
                SpecDescription = item.SpecDescription?.Trim(),
                ColorCode = item.ColorCode?.Trim(),
                ColorName = item.ColorName?.Trim(),
                EngineNo = item.EngineNo?.Trim(),
                StorageCodeFrom = storageFrom,
                StorageNameFrom = !string.IsNullOrWhiteSpace(item.StorageNameFrom) ? item.StorageNameFrom.Trim() : ResolveStorageName(storageFrom),
                StorageCodeTo = storageTo,
                StorageNameTo = !string.IsNullOrWhiteSpace(item.StorageNameTo) ? item.StorageNameTo.Trim() : ResolveStorageName(storageTo),
                LoaiThung = !string.IsNullOrWhiteSpace(item.LoaiThung) ? item.LoaiThung.Trim() : "Thùng mui bạt",
                ActualSpec = item.ActualSpec?.Trim(),
                TypeCB = "N",
                Status = CarBodyRequestDtlStatus.Pending,
                Remark = item.Remark?.Trim()
            });

            existingVins.Add(vin);
        }

        entity.TotalCars = entity.Details.Count(x => x.Status != CarBodyRequestDtlStatus.Cancelled && x.Status != CarBodyRequestDtlStatus.Rejected);
        entity.LUDateTime = DateTime.Now;
        entity.LUBy = "NPP_USER";

        await db.SaveChangesAsync();
        return entity;
    }

    public async Task<CarBodyRequest?> RemoveCarAsync(string cbReqNo, string vin)
    {
        var entity = await DetailAsync(cbReqNo);
        if (entity is null) return null;

        if (entity.Status != CarBodyRequestStatus.Pending)
            throw new InvalidOperationException($"Chỉ được gỡ xe khỏi yêu cầu đóng thùng khi còn ở trạng thái Chờ duyệt (Pending). Trạng thái hiện tại: {entity.Status}.");

        var dtl = entity.Details.FirstOrDefault(x => string.Equals(x.Vin, vin.Trim(), StringComparison.OrdinalIgnoreCase));
        if (dtl is null)
            throw new InvalidOperationException($"Không tìm thấy xe VIN {vin} trong yêu cầu {cbReqNo}.");

        entity.Details.Remove(dtl);

        if (entity.Details.Count == 0)
        {
            // Tự động hủy yêu cầu nếu gỡ hết xe (giống logic Sto_CBReqDetailDelete của DMS.Sales)
            entity.Status = CarBodyRequestStatus.Cancelled;
            entity.CancelReason = "Tự động hủy yêu cầu do đã gỡ toàn bộ danh sách xe";
            entity.CancelDate = DateTime.Now;
            entity.CancelBy = "SYSTEM";
            entity.TotalCars = 0;
        }
        else
        {
            entity.TotalCars = entity.Details.Count(x => x.Status != CarBodyRequestDtlStatus.Cancelled && x.Status != CarBodyRequestDtlStatus.Rejected);
        }

        entity.LUDateTime = DateTime.Now;
        entity.LUBy = "NPP_USER";

        await db.SaveChangesAsync();
        return entity;
    }

    public async Task<CarBodyRequest?> ApproveAsync(string cbReqNo, ApproveCarBodyRequestDto? dto)
    {
        var entity = await DetailAsync(cbReqNo);
        if (entity is null) return null;

        if (entity.Status != CarBodyRequestStatus.Pending)
            throw new InvalidOperationException($"Chỉ được duyệt yêu cầu đóng thùng ở trạng thái Chờ duyệt (Pending). Trạng thái hiện tại: {entity.Status}.");

        if (entity.Details.Count == 0)
            throw new InvalidOperationException("Không thể duyệt yêu cầu đóng thùng không có danh sách xe.");

        entity.Status = CarBodyRequestStatus.Approved;
        entity.ApprovedDate = DateTime.Now;
        entity.ApprovedBy = dto?.ApprovedBy?.Trim() ?? "NPP_HQ_MANAGER";
        if (!string.IsNullOrWhiteSpace(dto?.Remark))
            entity.Remark = string.IsNullOrWhiteSpace(entity.Remark) ? dto.Remark.Trim() : $"{entity.Remark} | {dto.Remark.Trim()}";

        foreach (var d in entity.Details.Where(x => x.Status == CarBodyRequestDtlStatus.Pending))
        {
            d.Status = CarBodyRequestDtlStatus.Approved;
        }

        entity.LUDateTime = DateTime.Now;
        entity.LUBy = entity.ApprovedBy;

        await db.SaveChangesAsync();
        return entity;
    }

    public async Task<CarBodyRequest?> RejectAsync(string cbReqNo, RejectCarBodyRequestDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.RejectReason))
            throw new InvalidOperationException("Lý do từ chối (RejectReason) không được để trống.");

        var entity = await DetailAsync(cbReqNo);
        if (entity is null) return null;

        if (entity.Status != CarBodyRequestStatus.Pending && entity.Status != CarBodyRequestStatus.Approved)
            throw new InvalidOperationException($"Chỉ được từ chối yêu cầu đóng thùng ở trạng thái Chờ duyệt (Pending) hoặc Đã duyệt (Approved). Trạng thái hiện tại: {entity.Status}.");

        entity.Status = CarBodyRequestStatus.Rejected;
        entity.RejectReason = dto.RejectReason.Trim();
        entity.RejectDate = DateTime.Now;
        entity.RejectBy = dto.RejectedBy?.Trim() ?? "NPP_HQ_MANAGER";

        foreach (var d in entity.Details)
        {
            d.Status = CarBodyRequestDtlStatus.Rejected;
        }

        entity.LUDateTime = DateTime.Now;
        entity.LUBy = entity.RejectBy;

        await db.SaveChangesAsync();
        return entity;
    }

    public async Task<CarBodyRequest?> CancelAsync(string cbReqNo, CancelCarBodyRequestDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.CancelReason))
            throw new InvalidOperationException("Lý do hủy yêu cầu (CancelReason) không được để trống.");

        var entity = await DetailAsync(cbReqNo);
        if (entity is null) return null;

        if (entity.Status != CarBodyRequestStatus.Pending)
            throw new InvalidOperationException($"Chỉ được hủy yêu cầu đóng thùng khi còn ở trạng thái Chờ duyệt (Pending). Trạng thái hiện tại: {entity.Status}.");

        entity.Status = CarBodyRequestStatus.Cancelled;
        entity.CancelReason = dto.CancelReason.Trim();
        entity.CancelDate = DateTime.Now;
        entity.CancelBy = dto.CancelledBy?.Trim() ?? "NPP_PLANNING";

        foreach (var d in entity.Details)
        {
            d.Status = CarBodyRequestDtlStatus.Cancelled;
        }

        entity.LUDateTime = DateTime.Now;
        entity.LUBy = entity.CancelBy;

        await db.SaveChangesAsync();
        return entity;
    }

    public async Task<CarBodyRequest?> CompleteAsync(string cbReqNo, CompleteCarBodyRequestDto? dto)
    {
        var entity = await DetailAsync(cbReqNo);
        if (entity is null) return null;

        if (entity.Status != CarBodyRequestStatus.Approved)
            throw new InvalidOperationException($"Chỉ được nghiệm thu hoàn tất khi yêu cầu đóng thùng đã được duyệt (Approved). Trạng thái hiện tại: {entity.Status}.");

        entity.Status = CarBodyRequestStatus.Completed;
        entity.CompletedDate = DateTime.Now;
        entity.CompletedBy = dto?.CompletedBy?.Trim() ?? "KY_THUAT_XUONG_DT";

        var prefix = dto?.SerialNoPrefix?.Trim() ?? "CB-2026-";
        var idx = 1;
        var inspectionResult = dto?.InspectionResult?.Trim() ?? "PASSED - Đạt quy chuẩn an toàn kỹ thuật và bảo vệ môi trường";

        foreach (var d in entity.Details.Where(x => x.Status == CarBodyRequestDtlStatus.Approved))
        {
            d.Status = CarBodyRequestDtlStatus.Completed;
            d.TypeCB = "Y"; // Đã hoàn thành đóng thùng
            d.InspectionResult = inspectionResult;
            d.SerialNo = string.IsNullOrWhiteSpace(d.SerialNo) ? $"{prefix}{DateTime.Now:yyMM}-{idx:D4}" : d.SerialNo;
            if (string.IsNullOrWhiteSpace(d.ActualSpec))
                d.ActualSpec = $"{d.Model} {d.LoaiThung}";
            idx++;
        }

        if (!string.IsNullOrWhiteSpace(dto?.Remark))
            entity.Remark = string.IsNullOrWhiteSpace(entity.Remark) ? dto.Remark.Trim() : $"{entity.Remark} | {dto.Remark.Trim()}";

        entity.LUDateTime = DateTime.Now;
        entity.LUBy = entity.CompletedBy;

        await db.SaveChangesAsync();
        return entity;
    }

    public async Task<List<EligibleCarForBodyBuildingDto>> GetEligibleCarsAsync(string? storageCode = null)
    {
        var cars = await GetEligibleCarsInternalAsync();
        if (!string.IsNullOrWhiteSpace(storageCode))
        {
            var sc = storageCode.Trim().ToUpperInvariant();
            cars = cars.Where(x => x.StorageCodeCurrent.ToUpper() == sc).ToList();
        }
        return cars;
    }

    public async Task<CarBodyRequestStatsDto> StatsAsync()
    {
        var all = await db.BodyRequests
            .Include(x => x.Details)
            .Where(x => x.OrgId == tenant.OrgId)
            .AsNoTracking()
            .ToListAsync();

        var totalRequests = all.Count;
        var pendingCount = all.Count(x => x.Status == CarBodyRequestStatus.Pending);
        var approvedCount = all.Count(x => x.Status == CarBodyRequestStatus.Approved);
        var completedCount = all.Count(x => x.Status == CarBodyRequestStatus.Completed);
        var rejectedCount = all.Count(x => x.Status == CarBodyRequestStatus.Rejected);
        var cancelledCount = all.Count(x => x.Status == CarBodyRequestStatus.Cancelled);

        var totalCars = all.SelectMany(x => x.Details).Count(x => x.Status != CarBodyRequestDtlStatus.Cancelled && x.Status != CarBodyRequestDtlStatus.Rejected);
        var distinctWorkshops = all.Select(x => x.StorageCodeTo).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).Count();
        var convertedCarsCount = all.SelectMany(x => x.Details).Count(x => x.Status == CarBodyRequestDtlStatus.Completed);

        var breakdown = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var d in all.SelectMany(x => x.Details).Where(x => x.Status != CarBodyRequestDtlStatus.Cancelled && x.Status != CarBodyRequestDtlStatus.Rejected))
        {
            var key = string.IsNullOrWhiteSpace(d.LoaiThung) ? "Khác" : d.LoaiThung.Trim();
            breakdown[key] = breakdown.GetValueOrDefault(key, 0) + 1;
        }

        return new CarBodyRequestStatsDto(
            totalRequests,
            pendingCount,
            approvedCount,
            completedCount,
            rejectedCount,
            cancelledCount,
            totalCars,
            distinctWorkshops,
            convertedCarsCount,
            breakdown
        );
    }

    private async Task CheckVinAvailableForBodyRequestAsync(string vin)
    {
        var activeReq = await db.BodyRequestDetails
            .Include(x => x.CBRequestId)
            .AnyAsync(x => x.Vin.ToUpper() == vin.ToUpper()
                        && (x.Status == CarBodyRequestDtlStatus.Pending || x.Status == CarBodyRequestDtlStatus.Approved));

        if (activeReq)
            throw new InvalidOperationException($"Số khung VIN {vin} đang nằm trong một Yêu cầu đóng thùng khác đang chờ duyệt hoặc đang thực hiện.");
    }

    private async Task<List<EligibleCarForBodyBuildingDto>> GetEligibleCarsInternalAsync()
    {
        var busyVins = await db.BodyRequestDetails
            .Where(x => x.Status == CarBodyRequestDtlStatus.Pending || x.Status == CarBodyRequestDtlStatus.Approved)
            .Select(x => x.Vin.ToUpper())
            .Distinct()
            .ToListAsync();

        var busySet = new HashSet<string>(busyVins, StringComparer.OrdinalIgnoreCase);

        // Nguồn xe sắt xi tiêu chuẩn trong chuỗi cung ứng xe thương mại Hyundai
        var pool = new List<EligibleCarForBodyBuildingDto>
        {
            new("CAR2026-EX8-001", "KMHEC81BBSA601001", "Hyundai Mighty EX8 GT S2", "EX8", "EX8-CHASSIS-01", "Mighty EX8 GT S2 Sắt xi bản dài", "WH1", "Trắng", "D4CC-601001", "KHO_TONG_HN", "Kho Tổng Hyundai Ninh Bình", "N"),
            new("CAR2026-EX8-002", "KMHEC81BBSA601002", "Hyundai Mighty EX8 GT S2", "EX8", "EX8-CHASSIS-01", "Mighty EX8 GT S2 Sắt xi bản dài", "WH1", "Trắng", "D4CC-601002", "KHO_TONG_HN", "Kho Tổng Hyundai Ninh Bình", "N"),
            new("CAR2026-H150-01", "KMHEB81BBSA501003", "Hyundai New Porter H150", "H150", "H150-CHASSIS-02", "New Porter H150 Sắt xi cabin đơn", "BL1", "Xanh Hyundai", "D4CB-501003", "KHO_TONG_HP", "Kho Trung Chuyển Hải Phòng", "N"),
            new("CAR2026-H150-02", "KMHEB81BBSA501004", "Hyundai New Porter H150", "H150", "H150-CHASSIS-02", "New Porter H150 Sắt xi cabin đơn", "WH1", "Trắng", "D4CB-501004", "KHO_TONG_SG", "Kho Tổng Nam Bộ Hiệp Phước", "N"),
            new("CAR2026-110SP-01", "KMHEE81BBSA701005", "Hyundai New Mighty 110SP", "110SP", "110SP-CHASSIS-01", "Mighty 110SP 7 tấn sắt xi", "BL1", "Xanh Hyundai", "D4GA-701005", "KHO_TONG_HN", "Kho Tổng Hyundai Ninh Bình", "N"),
            new("CAR2026-W11S-01", "KMHEF81BBSA801006", "Hyundai Mighty W11S", "W11S", "W11S-CHASSIS-01", "Mighty W11S Tải trung sắt xi", "WH1", "Trắng", "D4GA-801006", "KHO_TONG_SG", "Kho Tổng Nam Bộ Hiệp Phước", "N"),
            new("CAR2026-EX8L-01", "KMHEC81BBSA601007", "Hyundai Mighty EX8 GTL", "EX8L", "EX8L-CHASSIS-01", "Mighty EX8 GTL Sắt xi siêu dài", "WH1", "Trắng", "D4CC-601007", "KHO_TONG_HP", "Kho Trung Chuyển Hải Phòng", "N")
        };

        return pool.Where(x => !busySet.Contains(x.Vin)).ToList();
    }

    private static string ResolveStorageName(string code) => code switch
    {
        "KHO_DT_01" => "Xưởng Đóng Thùng Chuyên Dụng Hiệp Hòa",
        "KHO_DT_02" => "Xưởng Đóng Thùng Ô Tô Nam Việt",
        "KHO_DT_HIEPHOA" => "Xưởng Đóng Thùng Chuyên Dụng Hiệp Hòa",
        "KHO_DT_NAMVIET" => "Xưởng Đóng Thùng Ô Tô Nam Việt",
        "KHO_DT_QUYENAUTO" => "Xưởng Đóng Thùng Quyền Auto (Đông Lạnh)",
        "KHO_TONG_HN" => "Kho Tổng Hyundai Ninh Bình",
        "KHO_TONG_HP" => "Kho Trung Chuyển Hải Phòng",
        "KHO_TONG_SG" => "Kho Tổng Nam Bộ Hiệp Phước",
        _ => $"Kho/Xưởng {code}"
    };
}
