using Microsoft.EntityFrameworkCore;
using MiniSales.Data;
using MiniSales.Models;

namespace MiniSales.Services;

public record CreateDealerContractCancelBankMDDto(
    string? CancelBankMDNo,
    string ContractNo,
    string? RemarkDlr,
    string? CreatedBy
);

public record UpdateDealerContractCancelBankMDDto(
    string? RemarkDlr,
    string? UpdatedBy
);

public record BankApproveDealerContractCancelBankMDDto(
    string? ApprovedBy,
    string? RemarkBank
);

public record FinishDealerContractCancelBankMDDto(
    string? FinishedBy,
    string? RemarkHQ
);

public record RejectDealerContractCancelBankMDDto(
    string RejectReason,
    string? RejectedBy
);

public record CancelDealerContractCancelBankMDDto(
    string CancelReason,
    string? CancelledBy
);

public record DealerContractCancelBankMDStatsDto(
    int TotalRequests,
    int PendingRequests,
    int BankApprovedRequests,
    int FinishedRequests,
    int RejectedRequests,
    int CancelledRequests,
    int TotalCars,
    decimal TotalAmount,
    int DistinctDealersCount
);

public interface IDealerContractCancelBankMDService
{
    Task<object> CreateAsync(CreateDealerContractCancelBankMDDto dto);
    Task<object> ListAsync(string? status, string? dealer, string? contractNo, string? cancelBankMDNo, string? bankCodeMD);
    Task<object?> DetailAsync(string cancelBankMDNo);
    Task<object?> UpdateAsync(string cancelBankMDNo, UpdateDealerContractCancelBankMDDto dto);
    Task<object?> DeleteDraftAsync(string cancelBankMDNo);
    Task<object?> BankApproveAsync(string cancelBankMDNo, BankApproveDealerContractCancelBankMDDto? dto);
    Task<object?> FinishHqAsync(string cancelBankMDNo, FinishDealerContractCancelBankMDDto? dto);
    Task<object?> RejectHqAsync(string cancelBankMDNo, RejectDealerContractCancelBankMDDto dto);
    Task<object?> CancelDealerAsync(string cancelBankMDNo, CancelDealerContractCancelBankMDDto dto);
    Task<object> StatsAsync();
}

public sealed class DealerContractCancelBankMDService(AppDbContext db, ITenantContext tenant) : IDealerContractCancelBankMDService
{
    private Guid Org => tenant.OrgId;

    public async Task<object> CreateAsync(CreateDealerContractCancelBankMDDto dto)
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

        // 2. Kiểm tra trạng thái hợp đồng: chỉ cho phép hủy chọn ngân hàng khi hợp đồng đã ký kết có hiệu lực (Signed)
        if (contract.Status == DealerContractStatus.Cancelled)
            throw new InvalidOperationException($"Hợp đồng {contractNo} đã ở trạng thái đã hủy (Cancelled).");

        if (contract.Status != DealerContractStatus.Signed)
            throw new InvalidOperationException($"Hợp đồng {contractNo} đang ở trạng thái {contract.Status}. Chỉ được lập đề nghị hủy chọn ngân hàng bảo lãnh khi hợp đồng đã ký kết có hiệu lực (Signed).");

        // 3. Kiểm tra hợp đồng có gắn ngân hàng bảo lãnh hay không
        if (string.IsNullOrWhiteSpace(contract.BankCode))
            throw new InvalidOperationException($"Hợp đồng {contractNo} hiện tại không có ngân hàng bảo lãnh (BankCode rỗng), không thể thực hiện hủy chọn ngân hàng.");

        // 4. Kiểm tra ràng buộc Lệnh giao xe / Lệnh xuất xe (DeliveryOrder)
        var contractVins = contract.Details.Select(d => d.Vin).Where(v => !string.IsNullOrEmpty(v)).ToList();
        var hasActiveDeliveryOrder = await db.DeliveryOrders.AnyAsync(x =>
            x.OrgId == Org &&
            x.Status != DeliveryOrderStatus.Cancelled &&
            (x.OrderCode == contractNo || contractVins.Contains(x.Vin ?? "")));

        if (hasActiveDeliveryOrder)
            throw new InvalidOperationException($"Hợp đồng {contractNo} còn lệnh giao xe / xuất xe đang hoạt động. Vui lòng kiểm tra và hủy các lệnh xuất xe trước khi lập đề nghị hủy chọn ngân hàng bảo lãnh.");

        // 5. Kiểm tra ràng buộc Bảo lãnh thanh toán ngân hàng (PaymentGuarantee)
        var hasActiveGuarantee = await db.PaymentGuaranteeDetails.AnyAsync(x =>
            x.Vin != null && contractVins.Contains(x.Vin) &&
            db.PaymentGuarantees.Any(g => g.Id == x.GuaranteeId && g.OrgId == Org && g.Status != GuaranteeStatus.Cancelled));

        if (hasActiveGuarantee)
            throw new InvalidOperationException($"Hợp đồng {contractNo} còn chứng thư bảo lãnh ngân hàng đang hoạt động. Vui lòng làm thủ tục giải chấp hoặc hủy chứng thư bảo lãnh ngân hàng trước khi hủy chọn ngân hàng.");

        // 6. Kiểm tra ràng buộc Phiếu thanh toán tiền mua buôn xe (DealerPayment)
        var hasActivePayment = await db.DealerPaymentDetails.AnyAsync(x =>
            x.Vin != null && contractVins.Contains(x.Vin) &&
            db.DealerPayments.Any(p => p.Id == x.PaymentId && p.OrgId == Org && p.Status != WholesalePaymentStatus.Cancelled));

        if (hasActivePayment)
            throw new InvalidOperationException($"Hợp đồng {contractNo} còn phiếu thanh toán tiền buôn xe đang hoạt động. Vui lòng kiểm tra và hoàn tất hoặc hủy phiếu thanh toán trước khi hủy chọn ngân hàng bảo lãnh.");

        // 7. Kiểm tra không có biên bản hủy hợp đồng đang hoạt động
        var hasActiveCancelMinutes = await db.DealerContractCancelMinutes.AnyAsync(x =>
            x.OrgId == Org &&
            x.ContractNo == contractNo &&
            x.CancelMinutesStatus != DealerContractCancelMinutesStatus.Cancelled);

        if (hasActiveCancelMinutes)
            throw new InvalidOperationException($"Hợp đồng {contractNo} đang có biên bản thanh lý / hủy hợp đồng chưa kết thúc. Không thể lập đề nghị hủy chọn ngân hàng bảo lãnh.");

        // 8. Kiểm tra không có đề nghị hủy ngân hàng khác đang xử lý (Pending hoặc BankApproved)
        var existingActiveReq = await db.DealerContractCancelBankMDs.AnyAsync(x =>
            x.OrgId == Org &&
            x.ContractNo == contractNo &&
            (x.Status == DealerContractCancelBankMDStatus.Pending || x.Status == DealerContractCancelBankMDStatus.BankApproved));

        if (existingActiveReq)
            throw new InvalidOperationException($"Hợp đồng {contractNo} đã có đề nghị hủy chọn ngân hàng bảo lãnh đang chờ thẩm tra hoặc duyệt hoàn tất.");

        // 9. Sinh số đề nghị / biên bản hủy chọn ngân hàng
        string no;
        if (!string.IsNullOrWhiteSpace(dto.CancelBankMDNo))
        {
            no = dto.CancelBankMDNo.Trim().ToUpperInvariant();
            if (await db.DealerContractCancelBankMDs.AnyAsync(x => x.OrgId == Org && x.CancelBankMDNo == no))
                throw new InvalidOperationException($"Mã đề nghị hủy chọn ngân hàng {no} đã tồn tại trong hệ thống.");
        }
        else
        {
            var seq = await db.DealerContractCancelBankMDs.CountAsync(x => x.OrgId == Org && x.ContractNo == contractNo) + 1;
            no = $"{contractNo}.CB{seq:D2}";
        }

        var entity = new DealerContractCancelBankMD
        {
            OrgId = Org,
            CancelBankMDNo = no,
            ContractNo = contract.ContractNo,
            DealerCode = contract.DealerCode,
            DealerName = contract.DealerName,
            BankCodeMD = contract.BankCode,
            BankName = contract.BankName,
            TotalCars = contract.TotalCars,
            TotalAmount = contract.TotalAmount,
            Status = DealerContractCancelBankMDStatus.Pending,
            RemarkDlr = dto.RemarkDlr?.Trim(),
            CreatedBy = dto.CreatedBy ?? "DEALER_USER",
            CreatedAt = DateTime.Now
        };

        db.DealerContractCancelBankMDs.Add(entity);
        await db.SaveChangesAsync();

        return new
        {
            message = "Tạo đề nghị hủy chọn ngân hàng bảo lãnh thành công.",
            entity.CancelBankMDNo,
            entity.ContractNo,
            entity.DealerCode,
            entity.DealerName,
            entity.BankCodeMD,
            entity.BankName,
            entity.TotalCars,
            entity.TotalAmount,
            Status = entity.Status.ToString(),
            entity.CreatedAt
        };
    }

    public async Task<object> ListAsync(string? status, string? dealer, string? contractNo, string? cancelBankMDNo, string? bankCodeMD)
    {
        var q = db.DealerContractCancelBankMDs.Where(x => x.OrgId == Org);

        if (!string.IsNullOrWhiteSpace(status))
        {
            if (Enum.TryParse<DealerContractCancelBankMDStatus>(status, true, out var st))
                q = q.Where(x => x.Status == st);
            else if (status.Equals("P", StringComparison.OrdinalIgnoreCase))
                q = q.Where(x => x.Status == DealerContractCancelBankMDStatus.Pending);
            else if (status.Equals("A", StringComparison.OrdinalIgnoreCase))
                q = q.Where(x => x.Status == DealerContractCancelBankMDStatus.BankApproved);
            else if (status.Equals("F", StringComparison.OrdinalIgnoreCase))
                q = q.Where(x => x.Status == DealerContractCancelBankMDStatus.Finished);
            else if (status.Equals("R", StringComparison.OrdinalIgnoreCase))
                q = q.Where(x => x.Status == DealerContractCancelBankMDStatus.Rejected);
            else if (status.Equals("C", StringComparison.OrdinalIgnoreCase))
                q = q.Where(x => x.Status == DealerContractCancelBankMDStatus.Cancelled);
        }

        if (!string.IsNullOrWhiteSpace(dealer))
        {
            var d = dealer.Trim().ToUpperInvariant();
            q = q.Where(x => x.DealerCode.ToUpper().Contains(d) || x.DealerName.ToUpper().Contains(d));
        }

        if (!string.IsNullOrWhiteSpace(contractNo))
        {
            var c = contractNo.Trim().ToUpperInvariant();
            q = q.Where(x => x.ContractNo.ToUpper().Contains(c));
        }

        if (!string.IsNullOrWhiteSpace(cancelBankMDNo))
        {
            var n = cancelBankMDNo.Trim().ToUpperInvariant();
            q = q.Where(x => x.CancelBankMDNo.ToUpper().Contains(n));
        }

        if (!string.IsNullOrWhiteSpace(bankCodeMD))
        {
            var b = bankCodeMD.Trim().ToUpperInvariant();
            q = q.Where(x => x.BankCodeMD.ToUpper().Contains(b));
        }

        var list = await q.OrderByDescending(x => x.CreatedAt)
            .Select(x => new
            {
                x.Id,
                x.CancelBankMDNo,
                x.ContractNo,
                x.DealerCode,
                x.DealerName,
                x.BankCodeMD,
                x.BankName,
                x.TotalCars,
                x.TotalAmount,
                Status = x.Status.ToString(),
                StatusCode = (int)x.Status,
                x.RemarkDlr,
                x.RemarkBank,
                x.RemarkHQ,
                x.RejectReason,
                x.CancelReason,
                x.CreatedBy,
                x.CreatedAt,
                x.BankApprovedAt,
                x.BankApprovedBy,
                x.FinishedAt,
                x.FinishedBy,
                x.RejectedAt,
                x.RejectedBy,
                x.CancelledAt,
                x.CancelledBy,
                x.LUDateTime,
                x.LUBy
            })
            .ToListAsync();

        return list;
    }

    public async Task<object?> DetailAsync(string cancelBankMDNo)
    {
        var no = cancelBankMDNo.Trim().ToUpperInvariant();
        var item = await db.DealerContractCancelBankMDs
            .FirstOrDefaultAsync(x => x.OrgId == Org && x.CancelBankMDNo == no);

        if (item is null) return null;

        var contract = await db.DealerContracts
            .Include(x => x.Details)
            .FirstOrDefaultAsync(x => x.OrgId == Org && x.ContractNo == item.ContractNo);

        return new
        {
            item.Id,
            item.CancelBankMDNo,
            item.ContractNo,
            item.DealerCode,
            item.DealerName,
            item.BankCodeMD,
            item.BankName,
            item.TotalCars,
            item.TotalAmount,
            Status = item.Status.ToString(),
            StatusCode = (int)item.Status,
            item.RemarkDlr,
            item.RemarkBank,
            item.RemarkHQ,
            item.RejectReason,
            item.CancelReason,
            item.CreatedBy,
            item.CreatedAt,
            item.BankApprovedAt,
            item.BankApprovedBy,
            item.FinishedAt,
            item.FinishedBy,
            item.RejectedAt,
            item.RejectedBy,
            item.CancelledAt,
            item.CancelledBy,
            item.LUDateTime,
            item.LUBy,
            Contract = contract is null ? null : new
            {
                contract.ContractNo,
                contract.DealerCode,
                contract.DealerName,
                contract.ContractDate,
                PaymentType = contract.PaymentType.ToString(),
                contract.BankCode,
                contract.BankName,
                contract.DepositPercent,
                contract.GuaranteeDays,
                contract.TotalCars,
                contract.TotalAmount,
                ContractStatus = contract.Status.ToString(),
                Cars = contract.Details.Select(d => new
                {
                    d.CarId,
                    d.Vin,
                    d.Model,
                    d.SpecCode,
                    d.ColorCode,
                    d.UnitPrice,
                    d.TotalAmount,
                    d.OrderNo
                }).ToList()
            }
        };
    }

    public async Task<object?> UpdateAsync(string cancelBankMDNo, UpdateDealerContractCancelBankMDDto dto)
    {
        var no = cancelBankMDNo.Trim().ToUpperInvariant();
        var item = await db.DealerContractCancelBankMDs
            .FirstOrDefaultAsync(x => x.OrgId == Org && x.CancelBankMDNo == no);

        if (item is null) return null;

        if (item.Status != DealerContractCancelBankMDStatus.Pending && item.Status != DealerContractCancelBankMDStatus.Rejected)
            throw new InvalidOperationException($"Chỉ được sửa đề nghị khi ở trạng thái Chờ duyệt (Pending) hoặc Bị từ chối (Rejected). Trạng thái hiện tại: {item.Status}.");

        if (dto.RemarkDlr != null)
            item.RemarkDlr = dto.RemarkDlr.Trim();

        item.LUDateTime = DateTime.Now;
        item.LUBy = dto.UpdatedBy ?? "DEALER_USER";

        await db.SaveChangesAsync();

        return new
        {
            message = "Cập nhật đề nghị hủy chọn ngân hàng bảo lãnh thành công.",
            item.CancelBankMDNo,
            item.ContractNo,
            item.RemarkDlr,
            Status = item.Status.ToString(),
            item.LUDateTime,
            item.LUBy
        };
    }

    public async Task<object?> DeleteDraftAsync(string cancelBankMDNo)
    {
        var no = cancelBankMDNo.Trim().ToUpperInvariant();
        var item = await db.DealerContractCancelBankMDs
            .FirstOrDefaultAsync(x => x.OrgId == Org && x.CancelBankMDNo == no);

        if (item is null) return null;

        if (item.Status != DealerContractCancelBankMDStatus.Pending)
            throw new InvalidOperationException($"Chỉ được xóa đề nghị khi ở trạng thái Chờ duyệt (Pending). Trạng thái hiện tại: {item.Status}.");

        db.DealerContractCancelBankMDs.Remove(item);
        await db.SaveChangesAsync();

        return new
        {
            message = $"Đã xóa đề nghị hủy chọn ngân hàng bảo lãnh {no}.",
            deleted = true,
            cancelBankMDNo = no
        };
    }

    public async Task<object?> BankApproveAsync(string cancelBankMDNo, BankApproveDealerContractCancelBankMDDto? dto)
    {
        var no = cancelBankMDNo.Trim().ToUpperInvariant();
        var item = await db.DealerContractCancelBankMDs
            .FirstOrDefaultAsync(x => x.OrgId == Org && x.CancelBankMDNo == no);

        if (item is null) return null;

        if (item.Status != DealerContractCancelBankMDStatus.Pending)
            throw new InvalidOperationException($"Chỉ được duyệt thẩm định khi đề nghị ở trạng thái Chờ duyệt (Pending). Trạng thái hiện tại: {item.Status}.");

        item.Status = DealerContractCancelBankMDStatus.BankApproved;
        item.BankApprovedAt = DateTime.Now;
        item.BankApprovedBy = dto?.ApprovedBy ?? "BANK_OFFICER";
        if (!string.IsNullOrWhiteSpace(dto?.RemarkBank))
            item.RemarkBank = dto.RemarkBank.Trim();

        item.LUDateTime = DateTime.Now;
        item.LUBy = item.BankApprovedBy;

        await db.SaveChangesAsync();

        return new
        {
            message = "Ngân hàng / Bộ phận thẩm tra đã duyệt chấp thuận đề nghị hủy chọn ngân hàng bảo lãnh.",
            item.CancelBankMDNo,
            item.ContractNo,
            item.BankCodeMD,
            Status = item.Status.ToString(),
            item.BankApprovedAt,
            item.BankApprovedBy,
            item.RemarkBank
        };
    }

    public async Task<object?> FinishHqAsync(string cancelBankMDNo, FinishDealerContractCancelBankMDDto? dto)
    {
        var no = cancelBankMDNo.Trim().ToUpperInvariant();
        var item = await db.DealerContractCancelBankMDs
            .FirstOrDefaultAsync(x => x.OrgId == Org && x.CancelBankMDNo == no);

        if (item is null) return null;

        // Cho phép duyệt hoàn tất khi đang ở trạng thái BankApproved (A) hoặc Pending (P - trường hợp rút gọn)
        if (item.Status != DealerContractCancelBankMDStatus.BankApproved && item.Status != DealerContractCancelBankMDStatus.Pending)
            throw new InvalidOperationException($"Chỉ duyệt hoàn tất khi đề nghị ở trạng thái Ngân hàng đã duyệt (BankApproved) hoặc Chờ duyệt (Pending). Trạng thái hiện tại: {item.Status}.");

        var contract = await db.DealerContracts
            .FirstOrDefaultAsync(x => x.OrgId == Org && x.ContractNo == item.ContractNo);

        if (contract is null)
            throw new InvalidOperationException($"Không tìm thấy hợp đồng mua buôn {item.ContractNo} liên quan.");

        // 1. Cập nhật trạng thái đề nghị sang Finished
        item.Status = DealerContractCancelBankMDStatus.Finished;
        item.FinishedAt = DateTime.Now;
        item.FinishedBy = dto?.FinishedBy ?? "HQ_SALES_MANAGER";
        if (!string.IsNullOrWhiteSpace(dto?.RemarkHQ))
            item.RemarkHQ = dto.RemarkHQ.Trim();

        item.LUDateTime = DateTime.Now;
        item.LUBy = item.FinishedBy;

        // 2. Nghiệp vụ then chốt từ DMS.Sales (DMS40.0.34.Contract.cs / _CancelBankMD_Finish):
        // Gỡ bỏ liên kết ngân hàng bảo lãnh (BankCodeMD = NULL) trên DealerContract
        contract.BankCode = null;
        contract.BankName = null;
        contract.Remark = string.IsNullOrEmpty(contract.Remark)
            ? $"Đã hủy liên kết ngân hàng bảo lãnh theo biên bản {item.CancelBankMDNo} ngày {DateTime.Now:yyyy-MM-dd HH:mm}."
            : $"{contract.Remark}; Đã hủy liên kết NH bảo lãnh theo {item.CancelBankMDNo}.";

        await db.SaveChangesAsync();

        return new
        {
            message = "NPP đã phê duyệt hoàn tất hủy chọn ngân hàng bảo lãnh. Hợp đồng mua buôn đã được gỡ bỏ liên kết ngân hàng bảo lãnh thành công.",
            item.CancelBankMDNo,
            item.ContractNo,
            contract.DealerCode,
            Status = item.Status.ToString(),
            item.FinishedAt,
            item.FinishedBy,
            item.RemarkHQ,
            ContractCurrentBankCode = contract.BankCode,
            ContractCurrentBankName = contract.BankName
        };
    }

    public async Task<object?> RejectHqAsync(string cancelBankMDNo, RejectDealerContractCancelBankMDDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.RejectReason))
            throw new InvalidOperationException("Lý do từ chối (RejectReason) không được để trống.");

        var no = cancelBankMDNo.Trim().ToUpperInvariant();
        var item = await db.DealerContractCancelBankMDs
            .FirstOrDefaultAsync(x => x.OrgId == Org && x.CancelBankMDNo == no);

        if (item is null) return null;

        if (item.Status == DealerContractCancelBankMDStatus.Finished)
            throw new InvalidOperationException("Không thể từ chối đề nghị đã hoàn tất duyệt (Finished).");

        if (item.Status == DealerContractCancelBankMDStatus.Cancelled)
            throw new InvalidOperationException("Không thể từ chối đề nghị đã bị hủy (Cancelled).");

        item.Status = DealerContractCancelBankMDStatus.Rejected;
        item.RejectReason = dto.RejectReason.Trim();
        item.RejectedAt = DateTime.Now;
        item.RejectedBy = dto.RejectedBy ?? "HQ_SALES_OFFICER";
        item.LUDateTime = DateTime.Now;
        item.LUBy = item.RejectedBy;

        await db.SaveChangesAsync();

        return new
        {
            message = "Từ chối duyệt đề nghị hủy chọn ngân hàng bảo lãnh thành công.",
            item.CancelBankMDNo,
            item.ContractNo,
            Status = item.Status.ToString(),
            item.RejectReason,
            item.RejectedAt,
            item.RejectedBy
        };
    }

    public async Task<object?> CancelDealerAsync(string cancelBankMDNo, CancelDealerContractCancelBankMDDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.CancelReason))
            throw new InvalidOperationException("Lý do hủy (CancelReason) không được để trống.");

        var no = cancelBankMDNo.Trim().ToUpperInvariant();
        var item = await db.DealerContractCancelBankMDs
            .FirstOrDefaultAsync(x => x.OrgId == Org && x.CancelBankMDNo == no);

        if (item is null) return null;

        if (item.Status == DealerContractCancelBankMDStatus.Finished)
            throw new InvalidOperationException("Không thể hủy đề nghị đã được duyệt hoàn tất (Finished).");

        item.Status = DealerContractCancelBankMDStatus.Cancelled;
        item.CancelReason = dto.CancelReason.Trim();
        item.CancelledAt = DateTime.Now;
        item.CancelledBy = dto.CancelledBy ?? "DEALER_USER";
        item.LUDateTime = DateTime.Now;
        item.LUBy = item.CancelledBy;

        await db.SaveChangesAsync();

        return new
        {
            message = "Hủy đề nghị hủy chọn ngân hàng bảo lãnh thành công.",
            item.CancelBankMDNo,
            item.ContractNo,
            Status = item.Status.ToString(),
            item.CancelReason,
            item.CancelledAt,
            item.CancelledBy
        };
    }

    public async Task<object> StatsAsync()
    {
        var q = db.DealerContractCancelBankMDs.Where(x => x.OrgId == Org);

        var totalRequests = await q.CountAsync();
        var pendingRequests = await q.CountAsync(x => x.Status == DealerContractCancelBankMDStatus.Pending);
        var bankApprovedRequests = await q.CountAsync(x => x.Status == DealerContractCancelBankMDStatus.BankApproved);
        var finishedRequests = await q.CountAsync(x => x.Status == DealerContractCancelBankMDStatus.Finished);
        var rejectedRequests = await q.CountAsync(x => x.Status == DealerContractCancelBankMDStatus.Rejected);
        var cancelledRequests = await q.CountAsync(x => x.Status == DealerContractCancelBankMDStatus.Cancelled);

        var totalCars = await q.SumAsync(x => (int?)x.TotalCars) ?? 0;
        var totalAmount = await q.SumAsync(x => (decimal?)x.TotalAmount) ?? 0m;
        var distinctDealersCount = await q.Select(x => x.DealerCode).Distinct().CountAsync();

        return new DealerContractCancelBankMDStatsDto(
            totalRequests,
            pendingRequests,
            bankApprovedRequests,
            finishedRequests,
            rejectedRequests,
            cancelledRequests,
            totalCars,
            totalAmount,
            distinctDealersCount
        );
    }
}
