using Microsoft.EntityFrameworkCore;
using MiniSales.Data;
using MiniSales.Models;

namespace MiniSales.Services;

public record CreateDealerContractCancelMinutesDto(
    string? CancelMinutesNo,
    string ContractNo,
    string? Remark,
    string? CreatedBy
);

public record UpdateDealerContractCancelMinutesDto(
    string? Remark,
    string? UpdatedBy
);

public record ApproveDealerContractCancelMinutesDto(
    string? SignedBy,
    string? SignedFilePath,
    string? Remark
);

public record Approve1HqDealerContractCancelMinutesDto(
    string? ApprovedBy,
    string? Remark
);

public record Approve2HqDealerContractCancelMinutesDto(
    string? SignedBy,
    string? SignedFilePath,
    string? Remark
);

public record RejectDealerContractCancelMinutesDto(
    string RejectReason,
    string? RejectedBy
);

public record CancelDealerContractCancelMinutesDto(
    string CancelReason,
    string? CancelledBy
);

public record DealerContractCancelMinutesStatsDto(
    int TotalMinutes,
    int DraftMinutes,
    int SignedMinutes,
    int CancelledMinutes,
    int DealerSignedWaitingHq,
    int HqApproved1WaitingSign,
    int TotalCars,
    decimal TotalAmount,
    int DistinctDealersCount
);

public interface IDealerContractCancelMinutesService
{
    Task<object> CreateAsync(CreateDealerContractCancelMinutesDto dto);
    Task<object> ListAsync(string? status, string? dealer, string? contractNo, string? minutesNo, string? dlrSignStatus, string? hqSignStatus);
    Task<object?> DetailAsync(string minutesNo);
    Task<object?> UpdateAsync(string minutesNo, UpdateDealerContractCancelMinutesDto dto);
    Task<object?> DeleteDraftAsync(string minutesNo);
    Task<object?> ApproveDealerAsync(string minutesNo, ApproveDealerContractCancelMinutesDto dto);
    Task<object?> Approve1HqAsync(string minutesNo, Approve1HqDealerContractCancelMinutesDto? dto);
    Task<object?> Approve2HqAsync(string minutesNo, Approve2HqDealerContractCancelMinutesDto dto);
    Task<object?> RejectHqAsync(string minutesNo, RejectDealerContractCancelMinutesDto dto);
    Task<object?> CancelDealerAsync(string minutesNo, CancelDealerContractCancelMinutesDto dto);
    Task<object> StatsAsync();
}

public sealed class DealerContractCancelMinutesService(AppDbContext db, ITenantContext tenant) : IDealerContractCancelMinutesService
{
    private Guid Org => tenant.OrgId;

    public async Task<object> CreateAsync(CreateDealerContractCancelMinutesDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.ContractNo))
            throw new InvalidOperationException("ContractNo (số hợp đồng mua buôn đại lý) không được để trống.");

        var contractNo = dto.ContractNo.Trim().ToUpperInvariant();

        // 1. Kiểm tra Hợp đồng bán buôn đại lý tồn tại
        var contract = await db.DealerContracts
            .Include(x => x.Details)
            .FirstOrDefaultAsync(x => x.OrgId == Org && x.ContractNo == contractNo);

        if (contract is null)
            throw new InvalidOperationException($"Không tìm thấy hợp đồng mua buôn số {contractNo}.");

        // 2. Kiểm tra trạng thái hợp đồng: chỉ cho phép hủy khi đã ký kết hiệu lực (Signed)
        if (contract.Status == DealerContractStatus.Cancelled)
            throw new InvalidOperationException($"Hợp đồng {contractNo} đã ở trạng thái đã hủy (Cancelled).");

        if (contract.Status != DealerContractStatus.Signed)
            throw new InvalidOperationException($"Hợp đồng {contractNo} đang ở trạng thái {contract.Status}. Chỉ được lập biên bản hủy khi hợp đồng đã ký kết có hiệu lực (Signed).");

        // 3. Kiểm tra ràng buộc Lệnh giao xe / Lệnh xuất xe (DeliveryOrder)
        // Không được có lệnh xuất xe nào đang chờ duyệt, đã duyệt xuất bãi hoặc đã giao thực tế mà chưa bị hủy
        var contractVins = contract.Details.Select(d => d.Vin).Where(v => !string.IsNullOrEmpty(v)).ToList();
        var hasActiveDeliveryOrder = await db.DeliveryOrders.AnyAsync(x =>
            x.OrgId == Org &&
            x.Status != DeliveryOrderStatus.Cancelled &&
            (x.OrderCode == contractNo || contractVins.Contains(x.Vin ?? "")));

        if (hasActiveDeliveryOrder)
            throw new InvalidOperationException($"Hợp đồng {contractNo} còn lệnh giao xe / xuất xe đang hoạt động. Vui lòng kiểm tra và hủy các lệnh xuất xe trước khi lập biên bản hủy hợp đồng.");

        // 4. Kiểm tra ràng buộc Bảo lãnh ngân hàng (PaymentGuarantee)
        // Nếu các xe trong hợp đồng đang thuộc chứng thư bảo lãnh ngân hàng còn hiệu lực
        var hasActiveGuarantee = await db.PaymentGuaranteeDetails.AnyAsync(x =>
            x.Vin != null && contractVins.Contains(x.Vin) &&
            db.PaymentGuarantees.Any(g => g.Id == x.GuaranteeId && g.OrgId == Org && g.Status != GuaranteeStatus.Cancelled));

        if (hasActiveGuarantee)
            throw new InvalidOperationException($"Hợp đồng {contractNo} còn chứng thư bảo lãnh ngân hàng đang hoạt động. Vui lòng làm thủ tục giải chấp hoặc hủy bảo lãnh ngân hàng trước khi lập biên bản hủy hợp đồng.");

        // 5. Kiểm tra không có biên bản hủy nào khác chưa kết thúc cho hợp đồng này
        var existingActiveMinutes = await db.DealerContractCancelMinutes.AnyAsync(x =>
            x.OrgId == Org &&
            x.ContractNo == contractNo &&
            x.CancelMinutesStatus != DealerContractCancelMinutesStatus.Cancelled);

        if (existingActiveMinutes)
            throw new InvalidOperationException($"Hợp đồng {contractNo} đã có biên bản hủy đang trong quy trình xử lý chưa kết thúc.");

        // 6. Sinh mã biên bản hủy nếu chưa chỉ định: {ContractNo}.CM{seq:D2} (Chuẩn DMS.Sales DMS40_DlrCtr_CancelMinutes)
        string minutesNo;
        if (!string.IsNullOrWhiteSpace(dto.CancelMinutesNo))
        {
            minutesNo = dto.CancelMinutesNo.Trim().ToUpperInvariant();
        }
        else
        {
            var prefix = $"{contractNo}.CM";
            var maxSeq = await db.DealerContractCancelMinutes
                .Where(x => x.OrgId == Org && x.ContractNo == contractNo && x.CancelMinutesNo.StartsWith(prefix))
                .Select(x => x.CancelMinutesNo)
                .ToListAsync();

            int nextSeq = 1;
            if (maxSeq.Count > 0)
            {
                foreach (var s in maxSeq)
                {
                    var suffix = s.Substring(prefix.Length);
                    if (int.TryParse(suffix, out var parsed) && parsed >= nextSeq)
                        nextSeq = parsed + 1;
                }
            }

            minutesNo = $"{prefix}{nextSeq:D2}";
        }

        if (await db.DealerContractCancelMinutes.AnyAsync(x => x.OrgId == Org && x.CancelMinutesNo == minutesNo))
            throw new InvalidOperationException($"Số biên bản hủy {minutesNo} đã tồn tại trong hệ thống.");

        var entity = new DealerContractCancelMinutes
        {
            OrgId = Org,
            CancelMinutesNo = minutesNo,
            ContractNo = contractNo,
            DealerCode = contract.DealerCode,
            DealerName = contract.DealerName,
            ContractDate = contract.ContractDate,
            TotalCars = contract.TotalCars,
            TotalAmount = contract.TotalAmount,
            Remark = dto.Remark?.Trim(),
            CancelMinutesStatus = DealerContractCancelMinutesStatus.Draft,
            DealerSignStatus = DealerContractCancelMinutesSignStatus.Pending,
            HQSignStatus = DealerContractCancelMinutesSignStatus.Pending,
            CreatedBy = dto.CreatedBy ?? "DEALER_CONTRACT_ADMIN",
            CreatedAt = DateTime.Now
        };

        db.DealerContractCancelMinutes.Add(entity);
        await db.SaveChangesAsync();

        return entity;
    }

    public async Task<object> ListAsync(string? status, string? dealer, string? contractNo, string? minutesNo, string? dlrSignStatus, string? hqSignStatus)
    {
        var q = db.DealerContractCancelMinutes.Where(x => x.OrgId == Org);

        if (!string.IsNullOrWhiteSpace(status))
        {
            if (Enum.TryParse<DealerContractCancelMinutesStatus>(status, true, out var parsedStatus))
                q = q.Where(x => x.CancelMinutesStatus == parsedStatus);
        }

        if (!string.IsNullOrWhiteSpace(dealer))
        {
            var d = dealer.Trim().ToUpperInvariant();
            q = q.Where(x => x.DealerCode.ToUpper() == d || x.DealerName.Contains(dealer));
        }

        if (!string.IsNullOrWhiteSpace(contractNo))
        {
            var c = contractNo.Trim().ToUpperInvariant();
            q = q.Where(x => x.ContractNo.ToUpper().Contains(c));
        }

        if (!string.IsNullOrWhiteSpace(minutesNo))
        {
            var m = minutesNo.Trim().ToUpperInvariant();
            q = q.Where(x => x.CancelMinutesNo.ToUpper().Contains(m));
        }

        if (!string.IsNullOrWhiteSpace(dlrSignStatus))
        {
            if (Enum.TryParse<DealerContractCancelMinutesSignStatus>(dlrSignStatus, true, out var parsedDlrSign))
                q = q.Where(x => x.DealerSignStatus == parsedDlrSign);
        }

        if (!string.IsNullOrWhiteSpace(hqSignStatus))
        {
            if (Enum.TryParse<DealerContractCancelMinutesSignStatus>(hqSignStatus, true, out var parsedHqSign))
                q = q.Where(x => x.HQSignStatus == parsedHqSign);
        }

        return await q.OrderByDescending(x => x.CreatedAt).ToListAsync();
    }

    public async Task<object?> DetailAsync(string minutesNo)
    {
        var cleanNo = minutesNo.Trim().ToUpperInvariant();
        var entity = await db.DealerContractCancelMinutes
            .FirstOrDefaultAsync(x => x.OrgId == Org && x.CancelMinutesNo == cleanNo);

        if (entity is null) return null;

        var contract = await db.DealerContracts
            .Include(x => x.Details)
            .FirstOrDefaultAsync(x => x.OrgId == Org && x.ContractNo == entity.ContractNo);

        return new
        {
            CancelMinutes = entity,
            Contract = contract
        };
    }

    public async Task<object?> UpdateAsync(string minutesNo, UpdateDealerContractCancelMinutesDto dto)
    {
        var cleanNo = minutesNo.Trim().ToUpperInvariant();
        var entity = await db.DealerContractCancelMinutes
            .FirstOrDefaultAsync(x => x.OrgId == Org && x.CancelMinutesNo == cleanNo);

        if (entity is null) return null;

        if (entity.CancelMinutesStatus != DealerContractCancelMinutesStatus.Draft ||
            entity.DealerSignStatus != DealerContractCancelMinutesSignStatus.Pending)
        {
            throw new InvalidOperationException($"Biên bản {cleanNo} đã ký hoặc không ở trạng thái nháp. Không thể chỉnh sửa.");
        }

        if (dto.Remark is not null) entity.Remark = dto.Remark.Trim();
        entity.LUDateTime = DateTime.Now;
        entity.LUBy = dto.UpdatedBy ?? "DEALER_USER";

        await db.SaveChangesAsync();
        return entity;
    }

    public async Task<object?> DeleteDraftAsync(string minutesNo)
    {
        var cleanNo = minutesNo.Trim().ToUpperInvariant();
        var entity = await db.DealerContractCancelMinutes
            .FirstOrDefaultAsync(x => x.OrgId == Org && x.CancelMinutesNo == cleanNo);

        if (entity is null) return null;

        if (entity.CancelMinutesStatus != DealerContractCancelMinutesStatus.Draft ||
            entity.DealerSignStatus != DealerContractCancelMinutesSignStatus.Pending)
        {
            throw new InvalidOperationException($"Chỉ được xóa biên bản nháp chưa thực hiện ký số.");
        }

        db.DealerContractCancelMinutes.Remove(entity);
        await db.SaveChangesAsync();
        return new { success = true, deleted = cleanNo };
    }

    public async Task<object?> ApproveDealerAsync(string minutesNo, ApproveDealerContractCancelMinutesDto dto)
    {
        var cleanNo = minutesNo.Trim().ToUpperInvariant();
        var entity = await db.DealerContractCancelMinutes
            .FirstOrDefaultAsync(x => x.OrgId == Org && x.CancelMinutesNo == cleanNo);

        if (entity is null) return null;

        if (entity.CancelMinutesStatus != DealerContractCancelMinutesStatus.Draft)
            throw new InvalidOperationException($"Biên bản {cleanNo} không ở trạng thái hợp lệ để ký số (hiện tại: {entity.CancelMinutesStatus}).");

        if (entity.DealerSignStatus != DealerContractCancelMinutesSignStatus.Pending)
            throw new InvalidOperationException($"Đại lý đã ký số biên bản {cleanNo} trước đó rồi.");

        entity.DealerSignStatus = DealerContractCancelMinutesSignStatus.Approved;
        entity.DealerSignedAt = DateTime.Now;
        entity.DealerSignedBy = dto.SignedBy ?? "DEALER_LEGAL_REPRESENTATIVE";
        entity.SignedFilePath = dto.SignedFilePath ?? $"/storage/cancel_minutes/{cleanNo.Replace("/", "_")}_DL_Signed.pdf";

        if (!string.IsNullOrWhiteSpace(dto.Remark))
            entity.Remark = dto.Remark.Trim();

        entity.LUDateTime = DateTime.Now;
        entity.LUBy = entity.DealerSignedBy;

        await db.SaveChangesAsync();
        return entity;
    }

    public async Task<object?> Approve1HqAsync(string minutesNo, Approve1HqDealerContractCancelMinutesDto? dto)
    {
        var cleanNo = minutesNo.Trim().ToUpperInvariant();
        var entity = await db.DealerContractCancelMinutes
            .FirstOrDefaultAsync(x => x.OrgId == Org && x.CancelMinutesNo == cleanNo);

        if (entity is null) return null;

        if (entity.CancelMinutesStatus != DealerContractCancelMinutesStatus.Draft)
            throw new InvalidOperationException($"Biên bản {cleanNo} không ở trạng thái hợp lệ (hiện tại: {entity.CancelMinutesStatus}).");

        if (entity.DealerSignStatus != DealerContractCancelMinutesSignStatus.Approved)
            throw new InvalidOperationException($"Đại lý chưa ký số duyệt biên bản hủy {cleanNo}.");

        if (entity.HQSignStatus != DealerContractCancelMinutesSignStatus.Pending)
            throw new InvalidOperationException($"NPP đã duyệt hoặc xử lý biên bản này rồi (trạng thái: {entity.HQSignStatus}).");

        entity.HQSignStatus = DealerContractCancelMinutesSignStatus.Approved1;
        entity.HQAppr1At = DateTime.Now;
        entity.HQAppr1By = dto?.ApprovedBy ?? "NPP_CONTRACT_SPECIALIST";

        if (!string.IsNullOrWhiteSpace(dto?.Remark))
            entity.Remark = dto.Remark.Trim();

        entity.LUDateTime = DateTime.Now;
        entity.LUBy = entity.HQAppr1By;

        await db.SaveChangesAsync();
        return entity;
    }

    public async Task<object?> Approve2HqAsync(string minutesNo, Approve2HqDealerContractCancelMinutesDto dto)
    {
        var cleanNo = minutesNo.Trim().ToUpperInvariant();
        var entity = await db.DealerContractCancelMinutes
            .FirstOrDefaultAsync(x => x.OrgId == Org && x.CancelMinutesNo == cleanNo);

        if (entity is null) return null;

        if (entity.CancelMinutesStatus != DealerContractCancelMinutesStatus.Draft)
            throw new InvalidOperationException($"Biên bản {cleanNo} không ở trạng thái hợp lệ (hiện tại: {entity.CancelMinutesStatus}).");

        if (entity.DealerSignStatus != DealerContractCancelMinutesSignStatus.Approved)
            throw new InvalidOperationException($"Đại lý chưa hoàn tất ký số biên bản hủy.");

        if (entity.HQSignStatus != DealerContractCancelMinutesSignStatus.Approved1)
            throw new InvalidOperationException($"Biên bản {cleanNo} cần được NPP thẩm tra cấp 1 (Approve1HQ) trước khi Lãnh đạo ký số hoàn tất.");

        // Hoàn tất ký số cấp 2 NPP
        entity.HQSignStatus = DealerContractCancelMinutesSignStatus.Approved2;
        entity.CancelMinutesStatus = DealerContractCancelMinutesStatus.Signed;
        entity.HQAppr2At = DateTime.Now;
        entity.HQAppr2By = dto.SignedBy ?? "NPP_LEADERSHIP_REPRESENTATIVE";
        entity.SignedFilePath = dto.SignedFilePath ?? $"/storage/cancel_minutes/{cleanNo.Replace("/", "_")}_Full_Signed.pdf";

        if (!string.IsNullOrWhiteSpace(dto.Remark))
            entity.Remark = dto.Remark.Trim();

        entity.LUDateTime = DateTime.Now;
        entity.LUBy = entity.HQAppr2By;

        // Tự động cập nhật Hợp đồng bán buôn đại lý (DealerContract) sang trạng thái Cancelled
        var contract = await db.DealerContracts
            .FirstOrDefaultAsync(x => x.OrgId == Org && x.ContractNo == entity.ContractNo);

        if (contract is not null)
        {
            contract.Status = DealerContractStatus.Cancelled;
            contract.CancelledAt = DateTime.Now;
            contract.CancelReason = $"Hủy theo Biên bản thanh lý / hủy HĐ số {entity.CancelMinutesNo} đã ký hoàn tất 2 bên. Lý do: {entity.Remark ?? "Hai bên thỏa thuận chấm dứt hợp đồng"}";
        }

        await db.SaveChangesAsync();
        return entity;
    }

    public async Task<object?> RejectHqAsync(string minutesNo, RejectDealerContractCancelMinutesDto dto)
    {
        var cleanNo = minutesNo.Trim().ToUpperInvariant();
        var entity = await db.DealerContractCancelMinutes
            .FirstOrDefaultAsync(x => x.OrgId == Org && x.CancelMinutesNo == cleanNo);

        if (entity is null) return null;

        if (string.IsNullOrWhiteSpace(dto.RejectReason))
            throw new InvalidOperationException("RejectReason (lý do từ chối) không được để trống.");

        if (entity.CancelMinutesStatus != DealerContractCancelMinutesStatus.Draft)
            throw new InvalidOperationException($"Biên bản {cleanNo} không ở trạng thái có thể từ chối (hiện tại: {entity.CancelMinutesStatus}).");

        if (entity.HQSignStatus == DealerContractCancelMinutesSignStatus.Approved2)
            throw new InvalidOperationException($"Biên bản {cleanNo} đã ký số hoàn tất, không thể từ chối.");

        entity.HQSignStatus = DealerContractCancelMinutesSignStatus.Rejected;
        entity.CancelMinutesStatus = DealerContractCancelMinutesStatus.Cancelled;
        entity.RejectAt = DateTime.Now;
        entity.RejectBy = dto.RejectedBy ?? "NPP_OFFICIAL";
        entity.RejectReason = dto.RejectReason.Trim();

        entity.LUDateTime = DateTime.Now;
        entity.LUBy = entity.RejectBy;

        await db.SaveChangesAsync();
        return entity;
    }

    public async Task<object?> CancelDealerAsync(string minutesNo, CancelDealerContractCancelMinutesDto dto)
    {
        var cleanNo = minutesNo.Trim().ToUpperInvariant();
        var entity = await db.DealerContractCancelMinutes
            .FirstOrDefaultAsync(x => x.OrgId == Org && x.CancelMinutesNo == cleanNo);

        if (entity is null) return null;

        if (string.IsNullOrWhiteSpace(dto.CancelReason))
            throw new InvalidOperationException("CancelReason (lý do hủy biên bản) không được để trống.");

        if (entity.CancelMinutesStatus != DealerContractCancelMinutesStatus.Draft)
            throw new InvalidOperationException($"Biên bản {cleanNo} đã được thanh lý hoặc đã hủy trước đó.");

        if (entity.HQSignStatus == DealerContractCancelMinutesSignStatus.Approved2)
            throw new InvalidOperationException($"Biên bản {cleanNo} đã hoàn tất ký số NPP, không thể hủy bỏ.");

        entity.CancelMinutesStatus = DealerContractCancelMinutesStatus.Cancelled;
        entity.CancelledAt = DateTime.Now;
        entity.CancelledBy = dto.CancelledBy ?? "DEALER_CONTRACT_ADMIN";
        entity.CancelReason = dto.CancelReason.Trim();

        entity.LUDateTime = DateTime.Now;
        entity.LUBy = entity.CancelledBy;

        await db.SaveChangesAsync();
        return entity;
    }

    public async Task<object> StatsAsync()
    {
        var list = await db.DealerContractCancelMinutes
            .Where(x => x.OrgId == Org)
            .ToListAsync();

        return new DealerContractCancelMinutesStatsDto(
            TotalMinutes: list.Count,
            DraftMinutes: list.Count(x => x.CancelMinutesStatus == DealerContractCancelMinutesStatus.Draft),
            SignedMinutes: list.Count(x => x.CancelMinutesStatus == DealerContractCancelMinutesStatus.Signed),
            CancelledMinutes: list.Count(x => x.CancelMinutesStatus == DealerContractCancelMinutesStatus.Cancelled),
            DealerSignedWaitingHq: list.Count(x => x.CancelMinutesStatus == DealerContractCancelMinutesStatus.Draft && x.DealerSignStatus == DealerContractCancelMinutesSignStatus.Approved && x.HQSignStatus == DealerContractCancelMinutesSignStatus.Pending),
            HqApproved1WaitingSign: list.Count(x => x.CancelMinutesStatus == DealerContractCancelMinutesStatus.Draft && x.HQSignStatus == DealerContractCancelMinutesSignStatus.Approved1),
            TotalCars: list.Sum(x => x.TotalCars),
            TotalAmount: list.Sum(x => x.TotalAmount),
            DistinctDealersCount: list.Select(x => x.DealerCode).Distinct().Count()
        );
    }
}
