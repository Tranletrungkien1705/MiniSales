using Microsoft.EntityFrameworkCore;
using MiniSales.Data;
using MiniSales.Models;

namespace MiniSales.Services;

public record StoRearrangeCBItemInputDto(
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
    string CBReqNo = "",
    DateTime? ExpectedStartDate = null,
    DateTime? ExpectedEndDate = null,
    string LoaiThung = "Thùng mui bạt",
    string? ActualSpec = null,
    string? Remark = null
);

public record CreateStoRearrangeCBDto(
    string? StoRearCBNo,
    string StorageCodeTo,
    string? StorageNameTo = null,
    string? Remark = null,
    string? CreatedBy = null,
    List<StoRearrangeCBItemInputDto>? Items = null,
    List<string>? Vins = null
);

public record UpdateStoRearrangeCBDto(
    string? StorageCodeTo,
    string? StorageNameTo,
    string? Remark,
    string? UpdatedBy
);

public record UpdateStoRearrangeCBLineDto(
    DateTime? ExpectedEndDate,
    string? Remark,
    string? UpdatedBy
);

public record ApproveStoRearrangeCBDto(
    string? ApprovedBy,
    string? Remark
);

public record RejectStoRearrangeCBDto(
    string RejectReason,
    string? RejectedBy
);

public record CancelStoRearrangeCBDto(
    string CancelReason,
    string? CancelledBy
);

public record EligibleCarForRearrangeCBDto(
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
    string? StorageNameCurrent,
    string StorageCodeTo,
    string? StorageNameTo,
    string CBReqNo,
    string LoaiThung,
    string? ActualSpec,
    string TypeCB
);

public record StoRearrangeCBStatsDto(
    int TotalOrders,
    int PendingCount,
    int ApprovedCount,
    int RejectedCount,
    int CancelledCount,
    int TotalCars,
    int DistinctWorkshopsCount,
    Dictionary<string, int> WorkshopBreakdown,
    Dictionary<string, int> BodyTypeBreakdown
);

public interface IStorageRearrangeCBService
{
    Task<StorageRearrangeCBOrder> CreateAsync(CreateStoRearrangeCBDto dto);
    Task<List<StorageRearrangeCBOrder>> ListAsync(
        string? status = null,
        string? vin = null,
        string? storageCodeTo = null,
        string? stoRearCBNo = null,
        DateTime? dateFrom = null,
        DateTime? dateTo = null
    );
    Task<StorageRearrangeCBOrder?> DetailAsync(string stoRearCBNo);
    Task<StorageRearrangeCBOrder?> UpdateAsync(string stoRearCBNo, UpdateStoRearrangeCBDto dto);
    Task<bool> DeleteDraftAsync(string stoRearCBNo);
    Task<StorageRearrangeCBOrder?> DetailUpdateAsync(string stoRearCBNo, string vin, UpdateStoRearrangeCBLineDto dto);
    Task<StorageRearrangeCBOrder?> AddCarsAsync(string stoRearCBNo, List<StoRearrangeCBItemInputDto> items);
    Task<StorageRearrangeCBOrder?> RemoveCarAsync(string stoRearCBNo, string vin);
    Task<StorageRearrangeCBOrder?> ApproveAsync(string stoRearCBNo, ApproveStoRearrangeCBDto? dto);
    Task<StorageRearrangeCBOrder?> RejectAsync(string stoRearCBNo, RejectStoRearrangeCBDto dto);
    Task<StorageRearrangeCBOrder?> CancelAsync(string stoRearCBNo, CancelStoRearrangeCBDto dto);
    Task<List<EligibleCarForRearrangeCBDto>> GetEligibleCarsAsync(string? storageCodeTo = null);
    Task<StoRearrangeCBStatsDto> StatsAsync();
}

public class StorageRearrangeCBService(AppDbContext db, ITenantContext tenant) : IStorageRearrangeCBService
{
    public async Task<StorageRearrangeCBOrder> CreateAsync(CreateStoRearrangeCBDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.StorageCodeTo))
            throw new InvalidOperationException("Kho/Xưởng đóng thùng nhận xe (StorageCodeTo) không được để trống.");

        var targetStorageTo = dto.StorageCodeTo.Trim().ToUpperInvariant();
        var targetStorageNameTo = dto.StorageNameTo ?? GetDefaultStorageName(targetStorageTo);

        // Sinh số lệnh nếu chưa có
        var orderNo = dto.StoRearCBNo?.Trim().ToUpperInvariant();
        if (string.IsNullOrWhiteSpace(orderNo))
        {
            var prefix = $"{DateTime.Now:yyMM}RCB";
            var maxSeq = await db.StorageRearrangeCBOrders
                .Where(x => x.OrgId == tenant.OrgId && x.StoRearCBNo.StartsWith(prefix))
                .OrderByDescending(x => x.StoRearCBNo)
                .Select(x => x.StoRearCBNo)
                .FirstOrDefaultAsync();

            int next = 1;
            if (!string.IsNullOrEmpty(maxSeq) && maxSeq.Length >= prefix.Length + 6 &&
                int.TryParse(maxSeq.Substring(prefix.Length, 6), out int parsed))
            {
                next = parsed + 1;
            }
            orderNo = $"{prefix}{next:D6}";
        }

        if (await db.StorageRearrangeCBOrders.AnyAsync(x => x.OrgId == tenant.OrgId && x.StoRearCBNo == orderNo))
            throw new InvalidOperationException($"Số lệnh điều chuyển đóng thùng '{orderNo}' đã tồn tại.");

        // Chuẩn bị danh sách xe
        var itemsToProcess = new List<StoRearrangeCBItemInputDto>();
        if (dto.Items != null && dto.Items.Count > 0)
        {
            itemsToProcess.AddRange(dto.Items);
        }
        else if (dto.Vins != null && dto.Vins.Count > 0)
        {
            // Tự động tìm thông tin từ Yêu cầu đóng thùng đã duyệt
            foreach (var vin in dto.Vins)
            {
                var cleanVin = vin.Trim().ToUpperInvariant();
                var cbrDetail = await db.BodyRequestDetails
                    .Include(d => d.CBReqNo)
                    .Where(d => d.Vin == cleanVin && d.Status == CarBodyRequestDtlStatus.Approved && d.TypeCB == "N")
                    .OrderByDescending(d => d.Id)
                    .FirstOrDefaultAsync();

                if (cbrDetail == null)
                    throw new InvalidOperationException($"Không tìm thấy Yêu cầu đóng thùng đã duyệt (Sto_CBReq) hợp lệ cho xe VIN '{cleanVin}'.");

                itemsToProcess.Add(new StoRearrangeCBItemInputDto(
                    CarId: cbrDetail.CarId,
                    Vin: cbrDetail.Vin,
                    Model: cbrDetail.Model,
                    ModelCode: cbrDetail.ModelCode,
                    SpecCode: cbrDetail.SpecCode,
                    SpecDescription: cbrDetail.SpecDescription,
                    ColorCode: cbrDetail.ColorCode,
                    ColorName: cbrDetail.ColorName,
                    EngineNo: cbrDetail.EngineNo,
                    StorageCodeFrom: cbrDetail.StorageCodeFrom,
                    StorageNameFrom: cbrDetail.StorageNameFrom,
                    StorageCodeTo: string.IsNullOrWhiteSpace(cbrDetail.StorageCodeTo) ? targetStorageTo : cbrDetail.StorageCodeTo,
                    StorageNameTo: string.IsNullOrWhiteSpace(cbrDetail.StorageNameTo) ? targetStorageNameTo : cbrDetail.StorageNameTo,
                    CBReqNo: cbrDetail.CBReqNo,
                    ExpectedStartDate: DateTime.Today.AddDays(1),
                    ExpectedEndDate: DateTime.Today.AddDays(7),
                    LoaiThung: cbrDetail.LoaiThung,
                    ActualSpec: cbrDetail.ActualSpec,
                    Remark: cbrDetail.Remark
                ));
            }
        }

        if (itemsToProcess.Count == 0)
            throw new InvalidOperationException("Lệnh điều chuyển đóng thùng phải có ít nhất 1 xe thương mại.");

        // Kiểm tra trùng VIN trong cùng lệnh
        var duplicateVin = itemsToProcess.GroupBy(x => x.Vin.Trim().ToUpperInvariant()).FirstOrDefault(g => g.Count() > 1);
        if (duplicateVin != null)
            throw new InvalidOperationException($"Số khung VIN '{duplicateVin.Key}' bị trùng lặp trong danh sách.");

        // Kiểm tra và validate từng xe
        var details = new List<StorageRearrangeCBDetail>();
        foreach (var item in itemsToProcess)
        {
            var cleanVin = item.Vin.Trim().ToUpperInvariant();
            if (cleanVin.Length != 17)
                throw new InvalidOperationException($"Số khung VIN '{cleanVin}' không hợp lệ (phải đủ 17 ký tự chuẩn ISO).");

            // Kiểm tra kho đến phải trùng khớp với kho đến của lệnh (1 lệnh chỉ có 1 kho đến)
            var itemStorageTo = (item.StorageCodeTo ?? targetStorageTo).Trim().ToUpperInvariant();
            if (!string.Equals(itemStorageTo, targetStorageTo, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException($"Xe VIN '{cleanVin}' có kho đến '{itemStorageTo}' khác với kho đích của lệnh '{targetStorageTo}'. Mỗi lệnh chỉ cho phép một kho/xưởng đóng thùng đích.");

            // Kiểm tra xe đã có lệnh đóng thùng đang hoạt động chưa (Pending hoặc Approved)
            var hasActiveOrder = await db.StorageRearrangeCBDetails
                .AnyAsync(d => d.Vin == cleanVin && (d.Status == StoRearrangeCBDtlStatus.Pending || d.Status == StoRearrangeCBDtlStatus.Approved));
            if (hasActiveOrder)
                throw new InvalidOperationException($"Xe VIN '{cleanVin}' đang thuộc một lệnh điều chuyển đóng thùng khác chưa hoàn tất.");

            // Kiểm tra Yêu cầu đóng thùng gốc (CBReqNo)
            var cbReqNo = item.CBReqNo?.Trim();
            if (string.IsNullOrWhiteSpace(cbReqNo))
                throw new InvalidOperationException($"Xe VIN '{cleanVin}' chưa có số Yêu cầu đóng thùng (CBReqNo).");

            var cbr = await db.BodyRequests
                .Include(r => r.Details)
                .FirstOrDefaultAsync(r => r.OrgId == tenant.OrgId && r.CBReqNo == cbReqNo);

            if (cbr == null || cbr.Status != CarBodyRequestStatus.Approved)
                throw new InvalidOperationException($"Yêu cầu đóng thùng '{cbReqNo}' không tồn tại hoặc chưa được NPP phê duyệt (Status = Approved).");

            var cbrLine = cbr.Details.FirstOrDefault(d => d.Vin == cleanVin);
            if (cbrLine == null || cbrLine.Status != CarBodyRequestDtlStatus.Approved)
                throw new InvalidOperationException($"Dòng xe VIN '{cleanVin}' trong Yêu cầu đóng thùng '{cbReqNo}' chưa được phê duyệt.");

            if (string.Equals(cbrLine.TypeCB, "Y", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException($"Xe VIN '{cleanVin}' đã hoàn tất đóng thùng (TypeCB = 'Y'), không thể tạo thêm lệnh điều chuyển đóng thùng.");

            var startDate = item.ExpectedStartDate ?? DateTime.Today.AddDays(1);
            var endDate = item.ExpectedEndDate ?? startDate.AddDays(6);
            if (startDate > endDate)
                throw new InvalidOperationException($"Ngày bắt đầu dự kiến ({startDate:yyyy-MM-dd}) không được lớn hơn ngày giao dự kiến ({endDate:yyyy-MM-dd}) cho xe VIN '{cleanVin}'.");

            details.Add(new StorageRearrangeCBDetail
            {
                StoRearCBNo = orderNo,
                CarId = string.IsNullOrWhiteSpace(item.CarId) ? cbrLine.CarId : item.CarId.Trim(),
                Vin = cleanVin,
                Model = string.IsNullOrWhiteSpace(item.Model) ? cbrLine.Model : item.Model.Trim(),
                ModelCode = item.ModelCode ?? cbrLine.ModelCode,
                SpecCode = item.SpecCode ?? cbrLine.SpecCode,
                SpecDescription = item.SpecDescription ?? cbrLine.SpecDescription,
                ColorCode = item.ColorCode ?? cbrLine.ColorCode,
                ColorName = item.ColorName ?? cbrLine.ColorName,
                EngineNo = item.EngineNo ?? cbrLine.EngineNo,
                StorageCodeFrom = string.IsNullOrWhiteSpace(item.StorageCodeFrom) ? cbrLine.StorageCodeFrom : item.StorageCodeFrom.Trim().ToUpperInvariant(),
                StorageNameFrom = item.StorageNameFrom ?? cbrLine.StorageNameFrom ?? GetDefaultStorageName(item.StorageCodeFrom),
                StorageCodeTo = targetStorageTo,
                StorageNameTo = targetStorageNameTo,
                CBReqNo = cbReqNo,
                ExpectedStartDate = startDate,
                ExpectedEndDate = endDate,
                LoaiThung = string.IsNullOrWhiteSpace(item.LoaiThung) ? cbrLine.LoaiThung : item.LoaiThung.Trim(),
                ActualSpec = item.ActualSpec ?? cbrLine.ActualSpec,
                TypeCB = "N",
                Status = StoRearrangeCBDtlStatus.Pending,
                Remark = item.Remark
            });
        }

        var order = new StorageRearrangeCBOrder
        {
            OrgId = tenant.OrgId,
            StoRearCBNo = orderNo,
            Status = StoRearrangeCBStatus.Pending,
            TotalCars = details.Count,
            StorageCodeTo = targetStorageTo,
            StorageNameTo = targetStorageNameTo,
            Remark = dto.Remark,
            CreatedDate = DateTime.Now,
            CreatedBy = dto.CreatedBy ?? "HQ_LOGISTICS",
            LUDateTime = DateTime.Now,
            LUBy = dto.CreatedBy ?? "HQ_LOGISTICS",
            Details = details
        };

        db.StorageRearrangeCBOrders.Add(order);
        await db.SaveChangesAsync();
        return order;
    }

    public async Task<List<StorageRearrangeCBOrder>> ListAsync(
        string? status = null,
        string? vin = null,
        string? storageCodeTo = null,
        string? stoRearCBNo = null,
        DateTime? dateFrom = null,
        DateTime? dateTo = null)
    {
        var q = db.StorageRearrangeCBOrders
            .Include(x => x.Details)
            .Where(x => x.OrgId == tenant.OrgId);

        if (!string.IsNullOrWhiteSpace(status))
        {
            if (Enum.TryParse<StoRearrangeCBStatus>(status, true, out var st))
                q = q.Where(x => x.Status == st);
            else if (string.Equals(status, "P", StringComparison.OrdinalIgnoreCase))
                q = q.Where(x => x.Status == StoRearrangeCBStatus.Pending);
            else if (string.Equals(status, "A", StringComparison.OrdinalIgnoreCase))
                q = q.Where(x => x.Status == StoRearrangeCBStatus.Approved);
            else if (string.Equals(status, "R", StringComparison.OrdinalIgnoreCase))
                q = q.Where(x => x.Status == StoRearrangeCBStatus.Rejected);
            else if (string.Equals(status, "C", StringComparison.OrdinalIgnoreCase))
                q = q.Where(x => x.Status == StoRearrangeCBStatus.Cancelled);
        }

        if (!string.IsNullOrWhiteSpace(vin))
        {
            var cleanVin = vin.Trim().ToUpperInvariant();
            q = q.Where(x => x.Details.Any(d => d.Vin.Contains(cleanVin)));
        }

        if (!string.IsNullOrWhiteSpace(storageCodeTo))
        {
            var s = storageCodeTo.Trim().ToUpperInvariant();
            q = q.Where(x => x.StorageCodeTo == s);
        }

        if (!string.IsNullOrWhiteSpace(stoRearCBNo))
        {
            var no = stoRearCBNo.Trim().ToUpperInvariant();
            q = q.Where(x => x.StoRearCBNo.Contains(no));
        }

        if (dateFrom.HasValue)
            q = q.Where(x => x.CreatedDate >= dateFrom.Value);

        if (dateTo.HasValue)
            q = q.Where(x => x.CreatedDate <= dateTo.Value);

        return await q.OrderByDescending(x => x.CreatedDate).ToListAsync();
    }

    public async Task<StorageRearrangeCBOrder?> DetailAsync(string stoRearCBNo)
    {
        var no = stoRearCBNo.Trim().ToUpperInvariant();
        return await db.StorageRearrangeCBOrders
            .Include(x => x.Details)
            .FirstOrDefaultAsync(x => x.OrgId == tenant.OrgId && x.StoRearCBNo == no);
    }

    public async Task<StorageRearrangeCBOrder?> UpdateAsync(string stoRearCBNo, UpdateStoRearrangeCBDto dto)
    {
        var order = await DetailAsync(stoRearCBNo);
        if (order == null) return null;

        if (order.Status != StoRearrangeCBStatus.Pending)
            throw new InvalidOperationException($"Chỉ có thể sửa Lệnh đóng thùng ở trạng thái Chờ duyệt (Pending). Trạng thái hiện tại: {order.Status}.");

        if (!string.IsNullOrWhiteSpace(dto.StorageCodeTo))
        {
            var newStorageTo = dto.StorageCodeTo.Trim().ToUpperInvariant();
            order.StorageCodeTo = newStorageTo;
            order.StorageNameTo = dto.StorageNameTo ?? GetDefaultStorageName(newStorageTo);
            foreach (var d in order.Details)
            {
                d.StorageCodeTo = newStorageTo;
                d.StorageNameTo = order.StorageNameTo;
            }
        }
        else if (dto.StorageNameTo != null)
        {
            order.StorageNameTo = dto.StorageNameTo;
            foreach (var d in order.Details) d.StorageNameTo = dto.StorageNameTo;
        }

        if (dto.Remark != null) order.Remark = dto.Remark;

        order.LUDateTime = DateTime.Now;
        order.LUBy = dto.UpdatedBy ?? "HQ_LOGISTICS";

        await db.SaveChangesAsync();
        return order;
    }

    public async Task<bool> DeleteDraftAsync(string stoRearCBNo)
    {
        var order = await DetailAsync(stoRearCBNo);
        if (order == null) return false;

        if (order.Status != StoRearrangeCBStatus.Pending)
            throw new InvalidOperationException($"Chỉ có thể xóa Lệnh đóng thùng ở trạng thái Chờ duyệt (Pending). Trạng thái hiện tại: {order.Status}.");

        db.StorageRearrangeCBOrders.Remove(order);
        await db.SaveChangesAsync();
        return true;
    }

    public async Task<StorageRearrangeCBOrder?> DetailUpdateAsync(string stoRearCBNo, string vin, UpdateStoRearrangeCBLineDto dto)
    {
        var order = await DetailAsync(stoRearCBNo);
        if (order == null) return null;

        if (order.Status != StoRearrangeCBStatus.Pending && order.Status != StoRearrangeCBStatus.Approved)
            throw new InvalidOperationException($"Chỉ có thể cập nhật chi tiết xe khi lệnh ở trạng thái Pending hoặc Approved. Trạng thái hiện tại: {order.Status}.");

        var cleanVin = vin.Trim().ToUpperInvariant();
        var detail = order.Details.FirstOrDefault(d => d.Vin == cleanVin);
        if (detail == null)
            throw new InvalidOperationException($"Không tìm thấy xe VIN '{cleanVin}' trong lệnh '{order.StoRearCBNo}'.");

        if (dto.ExpectedEndDate.HasValue)
        {
            if (dto.ExpectedEndDate.Value < detail.ExpectedStartDate)
                throw new InvalidOperationException($"Ngày giao dự kiến ({dto.ExpectedEndDate.Value:yyyy-MM-dd}) không được nhỏ hơn ngày bắt đầu ({detail.ExpectedStartDate:yyyy-MM-dd}).");

            detail.ExpectedEndDate = dto.ExpectedEndDate.Value;
        }

        if (dto.Remark != null) detail.Remark = dto.Remark;

        detail.ConfirmDate = DateTime.Now;
        detail.ConfirmBy = dto.UpdatedBy ?? "HQ_LOGISTICS";

        order.LUDateTime = DateTime.Now;
        order.LUBy = dto.UpdatedBy ?? "HQ_LOGISTICS";

        await db.SaveChangesAsync();
        return order;
    }

    public async Task<StorageRearrangeCBOrder?> AddCarsAsync(string stoRearCBNo, List<StoRearrangeCBItemInputDto> items)
    {
        var order = await DetailAsync(stoRearCBNo);
        if (order == null) return null;

        if (order.Status != StoRearrangeCBStatus.Pending)
            throw new InvalidOperationException($"Chỉ có thể thêm xe vào Lệnh đóng thùng ở trạng thái Chờ duyệt (Pending). Trạng thái hiện tại: {order.Status}.");

        if (items == null || items.Count == 0)
            throw new InvalidOperationException("Danh sách xe bổ sung không được để trống.");

        foreach (var item in items)
        {
            var cleanVin = item.Vin.Trim().ToUpperInvariant();
            if (cleanVin.Length != 17)
                throw new InvalidOperationException($"Số khung VIN '{cleanVin}' không hợp lệ (phải đủ 17 ký tự chuẩn ISO).");

            if (order.Details.Any(d => d.Vin == cleanVin))
                throw new InvalidOperationException($"Xe VIN '{cleanVin}' đã có trong lệnh này.");

            var hasActiveOrder = await db.StorageRearrangeCBDetails
                .AnyAsync(d => d.Vin == cleanVin && (d.Status == StoRearrangeCBDtlStatus.Pending || d.Status == StoRearrangeCBDtlStatus.Approved));
            if (hasActiveOrder)
                throw new InvalidOperationException($"Xe VIN '{cleanVin}' đang thuộc một lệnh điều chuyển đóng thùng khác chưa hoàn tất.");

            var cbReqNo = item.CBReqNo?.Trim();
            if (string.IsNullOrWhiteSpace(cbReqNo))
                throw new InvalidOperationException($"Xe VIN '{cleanVin}' chưa có số Yêu cầu đóng thùng (CBReqNo).");

            var cbr = await db.BodyRequests
                .Include(r => r.Details)
                .FirstOrDefaultAsync(r => r.OrgId == tenant.OrgId && r.CBReqNo == cbReqNo);

            if (cbr == null || cbr.Status != CarBodyRequestStatus.Approved)
                throw new InvalidOperationException($"Yêu cầu đóng thùng '{cbReqNo}' không tồn tại hoặc chưa được NPP phê duyệt.");

            var cbrLine = cbr.Details.FirstOrDefault(d => d.Vin == cleanVin);
            if (cbrLine == null || cbrLine.Status != CarBodyRequestDtlStatus.Approved)
                throw new InvalidOperationException($"Dòng xe VIN '{cleanVin}' trong Yêu cầu đóng thùng '{cbReqNo}' chưa được phê duyệt.");

            var startDate = item.ExpectedStartDate ?? DateTime.Today.AddDays(1);
            var endDate = item.ExpectedEndDate ?? startDate.AddDays(6);
            if (startDate > endDate)
                throw new InvalidOperationException($"Ngày bắt đầu ({startDate:yyyy-MM-dd}) không được lớn hơn ngày giao ({endDate:yyyy-MM-dd}) cho xe VIN '{cleanVin}'.");

            order.Details.Add(new StorageRearrangeCBDetail
            {
                StoRearCBId = order.Id,
                StoRearCBNo = order.StoRearCBNo,
                CarId = string.IsNullOrWhiteSpace(item.CarId) ? cbrLine.CarId : item.CarId.Trim(),
                Vin = cleanVin,
                Model = string.IsNullOrWhiteSpace(item.Model) ? cbrLine.Model : item.Model.Trim(),
                ModelCode = item.ModelCode ?? cbrLine.ModelCode,
                SpecCode = item.SpecCode ?? cbrLine.SpecCode,
                SpecDescription = item.SpecDescription ?? cbrLine.SpecDescription,
                ColorCode = item.ColorCode ?? cbrLine.ColorCode,
                ColorName = item.ColorName ?? cbrLine.ColorName,
                EngineNo = item.EngineNo ?? cbrLine.EngineNo,
                StorageCodeFrom = string.IsNullOrWhiteSpace(item.StorageCodeFrom) ? cbrLine.StorageCodeFrom : item.StorageCodeFrom.Trim().ToUpperInvariant(),
                StorageNameFrom = item.StorageNameFrom ?? cbrLine.StorageNameFrom ?? GetDefaultStorageName(item.StorageCodeFrom),
                StorageCodeTo = order.StorageCodeTo,
                StorageNameTo = order.StorageNameTo,
                CBReqNo = cbReqNo,
                ExpectedStartDate = startDate,
                ExpectedEndDate = endDate,
                LoaiThung = string.IsNullOrWhiteSpace(item.LoaiThung) ? cbrLine.LoaiThung : item.LoaiThung.Trim(),
                ActualSpec = item.ActualSpec ?? cbrLine.ActualSpec,
                TypeCB = "N",
                Status = StoRearrangeCBDtlStatus.Pending,
                Remark = item.Remark
            });
        }

        order.TotalCars = order.Details.Count;
        order.LUDateTime = DateTime.Now;
        order.LUBy = "HQ_LOGISTICS";

        await db.SaveChangesAsync();
        return order;
    }

    public async Task<StorageRearrangeCBOrder?> RemoveCarAsync(string stoRearCBNo, string vin)
    {
        var order = await DetailAsync(stoRearCBNo);
        if (order == null) return null;

        if (order.Status != StoRearrangeCBStatus.Pending)
            throw new InvalidOperationException($"Chỉ có thể xóa xe khỏi Lệnh đóng thùng ở trạng thái Chờ duyệt (Pending). Trạng thái hiện tại: {order.Status}.");

        var cleanVin = vin.Trim().ToUpperInvariant();
        var detail = order.Details.FirstOrDefault(d => d.Vin == cleanVin);
        if (detail == null)
            throw new InvalidOperationException($"Không tìm thấy xe VIN '{cleanVin}' trong lệnh '{order.StoRearCBNo}'.");

        db.StorageRearrangeCBDetails.Remove(detail);
        order.Details.Remove(detail);
        order.TotalCars = order.Details.Count;

        if (order.TotalCars == 0)
        {
            order.Status = StoRearrangeCBStatus.Cancelled;
            order.CancelReason = "Lệnh tự động hủy do đã xóa hết toàn bộ danh sách xe.";
            order.CancelDate = DateTime.Now;
            order.CancelBy = "SYSTEM";
        }

        order.LUDateTime = DateTime.Now;
        order.LUBy = "HQ_LOGISTICS";

        await db.SaveChangesAsync();
        return order;
    }

    public async Task<StorageRearrangeCBOrder?> ApproveAsync(string stoRearCBNo, ApproveStoRearrangeCBDto? dto)
    {
        var order = await DetailAsync(stoRearCBNo);
        if (order == null) return null;

        if (order.Status != StoRearrangeCBStatus.Pending)
            throw new InvalidOperationException($"Chỉ có thể phê duyệt Lệnh đóng thùng ở trạng thái Chờ duyệt (Pending). Trạng thái hiện tại: {order.Status}.");

        if (order.Details.Count == 0)
            throw new InvalidOperationException("Lệnh đóng thùng không có xe nào, không thể phê duyệt.");

        order.Status = StoRearrangeCBStatus.Approved;
        order.ApprovedDate = DateTime.Now;
        order.ApprovedBy = dto?.ApprovedBy ?? "HQ_APPROVER";

        if (!string.IsNullOrWhiteSpace(dto?.Remark))
            order.Remark = dto.Remark;

        foreach (var d in order.Details)
        {
            d.Status = StoRearrangeCBDtlStatus.Approved;
        }

        order.LUDateTime = DateTime.Now;
        order.LUBy = order.ApprovedBy;

        await db.SaveChangesAsync();
        return order;
    }

    public async Task<StorageRearrangeCBOrder?> RejectAsync(string stoRearCBNo, RejectStoRearrangeCBDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.RejectReason))
            throw new InvalidOperationException("Lý do từ chối phê duyệt (RejectReason) không được để trống.");

        var order = await DetailAsync(stoRearCBNo);
        if (order == null) return null;

        if (order.Status != StoRearrangeCBStatus.Pending && order.Status != StoRearrangeCBStatus.Approved)
            throw new InvalidOperationException($"Chỉ có thể từ chối Lệnh đóng thùng ở trạng thái Pending hoặc Approved. Trạng thái hiện tại: {order.Status}.");

        order.Status = StoRearrangeCBStatus.Rejected;
        order.RejectReason = dto.RejectReason.Trim();
        order.RejectDate = DateTime.Now;
        order.RejectBy = dto.RejectedBy ?? "HQ_APPROVER";

        foreach (var d in order.Details)
        {
            d.Status = StoRearrangeCBDtlStatus.Rejected;
        }

        order.LUDateTime = DateTime.Now;
        order.LUBy = order.RejectBy;

        await db.SaveChangesAsync();
        return order;
    }

    public async Task<StorageRearrangeCBOrder?> CancelAsync(string stoRearCBNo, CancelStoRearrangeCBDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.CancelReason))
            throw new InvalidOperationException("Lý do hủy lệnh (CancelReason) không được để trống.");

        var order = await DetailAsync(stoRearCBNo);
        if (order == null) return null;

        if (order.Status != StoRearrangeCBStatus.Pending)
            throw new InvalidOperationException($"Chỉ có thể hủy Lệnh đóng thùng ở trạng thái Chờ duyệt (Pending). Trạng thái hiện tại: {order.Status}.");

        order.Status = StoRearrangeCBStatus.Cancelled;
        order.CancelReason = dto.CancelReason.Trim();
        order.CancelDate = DateTime.Now;
        order.CancelBy = dto.CancelledBy ?? "HQ_LOGISTICS";

        foreach (var d in order.Details)
        {
            d.Status = StoRearrangeCBDtlStatus.Cancelled;
        }

        order.LUDateTime = DateTime.Now;
        order.LUBy = order.CancelBy;

        await db.SaveChangesAsync();
        return order;
    }

    public async Task<List<EligibleCarForRearrangeCBDto>> GetEligibleCarsAsync(string? storageCodeTo = null)
    {
        // Lấy tất cả các xe trong Yêu cầu đóng thùng đã được duyệt (Status = Approved) và TypeCB = 'N'
        var approvedCbrDetails = await db.BodyRequestDetails
            .Include(d => d.CBReqNo)
            .Where(d => d.Status == CarBodyRequestDtlStatus.Approved && d.TypeCB == "N")
            .ToListAsync();

        // Lọc các xe chưa có trong bất kỳ Lệnh đóng thùng nào đang hoạt động (Pending hoặc Approved)
        var activeOrderVins = await db.StorageRearrangeCBDetails
            .Where(d => d.Status == StoRearrangeCBDtlStatus.Pending || d.Status == StoRearrangeCBDtlStatus.Approved)
            .Select(d => d.Vin)
            .ToListAsync();

        var activeVinSet = new HashSet<string>(activeOrderVins, StringComparer.OrdinalIgnoreCase);

        var list = new List<EligibleCarForRearrangeCBDto>();
        foreach (var d in approvedCbrDetails)
        {
            if (activeVinSet.Contains(d.Vin)) continue;

            if (!string.IsNullOrWhiteSpace(storageCodeTo) &&
                !string.Equals(d.StorageCodeTo, storageCodeTo.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            list.Add(new EligibleCarForRearrangeCBDto(
                CarId: d.CarId,
                Vin: d.Vin,
                Model: d.Model,
                ModelCode: d.ModelCode,
                SpecCode: d.SpecCode,
                SpecDescription: d.SpecDescription,
                ColorCode: d.ColorCode,
                ColorName: d.ColorName,
                EngineNo: d.EngineNo,
                StorageCodeCurrent: d.StorageCodeFrom,
                StorageNameCurrent: d.StorageNameFrom,
                StorageCodeTo: d.StorageCodeTo,
                StorageNameTo: d.StorageNameTo,
                CBReqNo: d.CBReqNo,
                LoaiThung: d.LoaiThung,
                ActualSpec: d.ActualSpec,
                TypeCB: d.TypeCB
            ));
        }

        return list;
    }

    public async Task<StoRearrangeCBStatsDto> StatsAsync()
    {
        var orders = await db.StorageRearrangeCBOrders
            .Include(x => x.Details)
            .Where(x => x.OrgId == tenant.OrgId)
            .ToListAsync();

        var totalOrders = orders.Count;
        var pendingCount = orders.Count(x => x.Status == StoRearrangeCBStatus.Pending);
        var approvedCount = orders.Count(x => x.Status == StoRearrangeCBStatus.Approved);
        var rejectedCount = orders.Count(x => x.Status == StoRearrangeCBStatus.Rejected);
        var cancelledCount = orders.Count(x => x.Status == StoRearrangeCBStatus.Cancelled);
        var totalCars = orders.Sum(x => x.TotalCars);

        var allDetails = orders.SelectMany(x => x.Details).ToList();
        var workshopBreakdown = allDetails
            .GroupBy(x => x.StorageCodeTo)
            .ToDictionary(g => g.Key, g => g.Count());

        var bodyTypeBreakdown = allDetails
            .GroupBy(x => x.LoaiThung)
            .ToDictionary(g => g.Key, g => g.Count());

        var distinctWorkshopsCount = workshopBreakdown.Keys.Count;

        return new StoRearrangeCBStatsDto(
            TotalOrders: totalOrders,
            PendingCount: pendingCount,
            ApprovedCount: approvedCount,
            RejectedCount: rejectedCount,
            CancelledCount: cancelledCount,
            TotalCars: totalCars,
            DistinctWorkshopsCount: distinctWorkshopsCount,
            WorkshopBreakdown: workshopBreakdown,
            BodyTypeBreakdown: bodyTypeBreakdown
        );
    }

    private static string GetDefaultStorageName(string? code) => (code ?? "").ToUpperInvariant() switch
    {
        "KHO_TONG_HN" => "Kho Tổng Hà Nội - KCN Đài Tư",
        "KHO_TONG_HP" => "Kho Trung Chuyển Hải Phòng",
        "KHO_TONG_DN" => "Kho Tổng Đà Nẵng - Liên Chiểu",
        "KHO_TONG_SG" => "Kho Tổng Sài Gòn - Tân Vạn",
        "KHO_DT_01" => "Xưởng Đóng Thùng Ô Tô Hiệp Hòa",
        "KHO_DT_02" => "Xưởng Đóng Thùng Ô Tô Nam Việt",
        "KHO_DT_03" => "Xưởng Đóng Thùng Xe Quyền Auto",
        "KHO_DT_04" => "Xưởng Đóng Thùng Chuyên Dùng An Khang",
        _ => !string.IsNullOrEmpty(code) ? $"Xưởng Đóng Thùng ({code})" : "Kho Đóng Thùng Xe"
    };
}
