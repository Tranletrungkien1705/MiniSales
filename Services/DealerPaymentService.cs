using Microsoft.EntityFrameworkCore;
using MiniSales.Data;
using MiniSales.Models;

namespace MiniSales.Services;

public record CreateDealerPaymentCarDto(
    string CarId,
    string Vin,
    string Model,
    decimal Amount,
    string? GuaranteeNo,
    string? Remark
);

public record CreateDealerPaymentDto(
    string? PaymentNo,
    string DealerCode,
    string? DealerName,
    string? ContractNo,
    string? PaymentType,
    string? PaymentMethod,
    decimal? TotalAmount,
    DateTime? PaymentDate,
    DateTime? PaymentDueDate,
    string? BankPaymentNo,
    string? BankCodeSend,
    string? BankAccountSend,
    string? BankCodeReceive,
    string? BankAccountReceive,
    string? Funds,
    string? BankLending,
    decimal? InterestRate,
    int? LoanPeriod,
    string? GuaranteeType,
    decimal? DepositPercent,
    string? Remark,
    List<CreateDealerPaymentCarDto>? Items
);

public record UpdateDealerPaymentDto(
    string? ContractNo,
    string? PaymentType,
    string? PaymentMethod,
    decimal? TotalAmount,
    DateTime? PaymentDate,
    DateTime? PaymentDueDate,
    string? BankPaymentNo,
    string? BankCodeSend,
    string? BankAccountSend,
    string? BankCodeReceive,
    string? BankAccountReceive,
    string? Funds,
    string? BankLending,
    decimal? InterestRate,
    int? LoanPeriod,
    string? GuaranteeType,
    decimal? DepositPercent,
    string? Remark,
    List<CreateDealerPaymentCarDto>? Items
);

public record ApproveDealerPaymentDto(
    string? ApprovedBy,
    DateTime? PaymentEndDate,
    string? Note
);

public record ConfirmAccountingRecordDto(
    string AccountingRecordNo,
    DateTime? AccountingRecordDate,
    string? FinishedBy,
    string? Note
);

public record RejectDealerPaymentDto(
    string Reason,
    string? RejectedBy
);

public record CancelDealerPaymentDto(
    string Reason,
    string? CancelledBy
);

public record AddDealerPaymentCarDto(
    string CarId,
    string Vin,
    string Model,
    decimal Amount,
    string? GuaranteeNo,
    string? Remark
);

public interface IDealerPaymentService
{
    Task<object> CreateAsync(CreateDealerPaymentDto dto);
    Task<object?> UpdateAsync(string paymentNo, UpdateDealerPaymentDto dto);
    Task<object> ListAsync(string? status, string? dealer, string? paymentType, string? contractNo, string? paymentNo);
    Task<object?> DetailAsync(string paymentNo);
    Task<object?> ApproveAsync(string paymentNo, ApproveDealerPaymentDto? dto);
    Task<object?> ConfirmAccountingAsync(string paymentNo, ConfirmAccountingRecordDto dto);
    Task<object?> RejectAsync(string paymentNo, RejectDealerPaymentDto dto);
    Task<object?> CancelAsync(string paymentNo, CancelDealerPaymentDto dto);
    Task<object?> DeleteDraftAsync(string paymentNo);
    Task<object?> AddCarAsync(string paymentNo, AddDealerPaymentCarDto dto);
    Task<object?> RemoveCarAsync(string paymentNo, long detailId);
    Task<object> StatsAsync();
}

public sealed class DealerPaymentService(AppDbContext db, ITenantContext tenant) : IDealerPaymentService
{
    private Guid Org => tenant.OrgId;

    public async Task<object> CreateAsync(CreateDealerPaymentDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.DealerCode))
            throw new InvalidOperationException("Mã đại lý thanh toán (DealerCode) không được để trống.");

        var pmtType = ParsePaymentType(dto.PaymentType);
        var pmtMethod = ParsePaymentMethod(dto.PaymentMethod);

        if (pmtType == WholesalePaymentType.PMT && pmtMethod == WholesalePaymentMethod.BankTransfer && string.IsNullOrWhiteSpace(dto.BankPaymentNo))
            throw new InvalidOperationException("Thanh toán tiền xe qua chuyển khoản ngân hàng bắt buộc phải có Số ủy nhiệm chi / Mã giao dịch ngân hàng (BankPaymentNo).");

        // Validate nguồn tiền vay giải ngân ngân hàng (Funds = 1 hoặc 2)
        if (dto.Funds == "1" || dto.Funds == "2")
        {
            if (string.IsNullOrWhiteSpace(dto.BankLending))
                throw new InvalidOperationException("Thanh toán sử dụng nguồn vốn vay ngân hàng bắt buộc phải cung cấp Ngân hàng cho vay (BankLending).");
            if (!dto.InterestRate.HasValue || dto.InterestRate.Value <= 0)
                throw new InvalidOperationException("Lãi suất vay ngân hàng (InterestRate) phải lớn hơn 0%.");
            if (!dto.LoanPeriod.HasValue || dto.LoanPeriod.Value <= 0)
                throw new InvalidOperationException("Kỳ hạn vay ngân hàng (LoanPeriod) phải lớn hơn 0 tháng.");
        }

        var paymentNo = dto.PaymentNo?.Trim().ToUpperInvariant();
        if (string.IsNullOrWhiteSpace(paymentNo))
        {
            var count = await db.DealerPayments.CountAsync(x => x.OrgId == Org);
            paymentNo = $"PMT{DateTime.Today:yyMMdd}-{(count + 1):D3}";
        }

        if (await db.DealerPayments.AnyAsync(x => x.OrgId == Org && x.PaymentNo == paymentNo))
            throw new InvalidOperationException($"Mã phiếu thanh toán '{paymentNo}' đã tồn tại trong hệ thống.");

        decimal totalAmount = dto.TotalAmount ?? 0m;
        var details = new List<DealerPaymentDetail>();

        if (dto.Items is { Count: > 0 })
        {
            decimal sumItems = 0m;
            foreach (var item in dto.Items)
            {
                if (string.IsNullOrWhiteSpace(item.CarId) || string.IsNullOrWhiteSpace(item.Vin))
                    throw new InvalidOperationException("Chi tiết xe trong phiếu thanh toán phải có CarId và Vin hợp lệ.");
                if (item.Amount <= 0)
                    throw new InvalidOperationException($"Số tiền thanh toán cho xe {item.Vin} phải lớn hơn 0.");

                sumItems += item.Amount;
                details.Add(new DealerPaymentDetail
                {
                    PaymentNo = paymentNo,
                    CarId = item.CarId.Trim().ToUpperInvariant(),
                    Vin = item.Vin.Trim().ToUpperInvariant(),
                    Model = item.Model.Trim(),
                    Amount = item.Amount,
                    GuaranteeNo = item.GuaranteeNo?.Trim().ToUpperInvariant(),
                    Remark = item.Remark?.Trim()
                });
            }

            if (totalAmount <= 0) totalAmount = sumItems;
        }

        if (totalAmount <= 0)
            throw new InvalidOperationException("Tổng số tiền thanh toán (TotalAmount) phải lớn hơn 0 VNĐ.");

        var payment = new DealerPayment
        {
            OrgId = Org,
            PaymentNo = paymentNo,
            DealerCode = dto.DealerCode.Trim().ToUpperInvariant(),
            DealerName = dto.DealerName?.Trim() ?? dto.DealerCode.Trim().ToUpperInvariant(),
            ContractNo = dto.ContractNo?.Trim().ToUpperInvariant(),
            PaymentType = pmtType,
            PaymentMethod = pmtMethod,
            TotalAmount = totalAmount,
            PaymentDate = dto.PaymentDate ?? DateTime.Today,
            PaymentDueDate = dto.PaymentDueDate,
            Status = WholesalePaymentStatus.Pending,
            BankPaymentNo = dto.BankPaymentNo?.Trim().ToUpperInvariant(),
            BankCodeSend = dto.BankCodeSend?.Trim().ToUpperInvariant(),
            BankAccountSend = dto.BankAccountSend?.Trim(),
            BankCodeReceive = dto.BankCodeReceive?.Trim().ToUpperInvariant() ?? "VCB",
            BankAccountReceive = dto.BankAccountReceive?.Trim() ?? "0011001234567",
            Funds = dto.Funds ?? "0",
            BankLending = dto.BankLending?.Trim(),
            InterestRate = dto.InterestRate,
            LoanPeriod = dto.LoanPeriod,
            GuaranteeType = dto.GuaranteeType?.Trim().ToUpperInvariant(),
            DepositPercent = dto.DepositPercent ?? 10m,
            Remark = dto.Remark?.Trim(),
            CreatedBy = "DEALER_USER",
            CreatedAt = DateTime.Now,
            Details = details
        };

        db.DealerPayments.Add(payment);
        await db.SaveChangesAsync();

        return new
        {
            message = "Lập phiếu thanh toán tiền xe Đại lý - NPP thành công.",
            payment.Id,
            payment.PaymentNo,
            payment.DealerCode,
            payment.DealerName,
            PaymentType = payment.PaymentType.ToString(),
            PaymentMethod = payment.PaymentMethod.ToString(),
            payment.TotalAmount,
            Status = payment.Status.ToString(),
            payment.ContractNo,
            payment.BankPaymentNo,
            payment.PaymentDate,
            TotalCars = details.Count
        };
    }

    public async Task<object?> UpdateAsync(string paymentNo, UpdateDealerPaymentDto dto)
    {
        var payment = await db.DealerPayments
            .Include(x => x.Details)
            .FirstOrDefaultAsync(x => x.OrgId == Org && x.PaymentNo == paymentNo);

        if (payment is null) return null;

        if (payment.Status != WholesalePaymentStatus.Pending)
            throw new InvalidOperationException($"Chỉ có thể sửa phiếu thanh toán ở trạng thái Pending (Chờ xác nhận). Phiếu '{paymentNo}' đang ở trạng thái {payment.Status}.");

        if (!string.IsNullOrWhiteSpace(dto.PaymentType))
            payment.PaymentType = ParsePaymentType(dto.PaymentType);

        if (!string.IsNullOrWhiteSpace(dto.PaymentMethod))
            payment.PaymentMethod = ParsePaymentMethod(dto.PaymentMethod);

        if (!string.IsNullOrWhiteSpace(dto.ContractNo))
            payment.ContractNo = dto.ContractNo.Trim().ToUpperInvariant();

        if (dto.PaymentDate.HasValue)
            payment.PaymentDate = dto.PaymentDate.Value;

        if (dto.PaymentDueDate.HasValue)
            payment.PaymentDueDate = dto.PaymentDueDate.Value;

        if (dto.BankPaymentNo != null)
            payment.BankPaymentNo = dto.BankPaymentNo.Trim().ToUpperInvariant();

        if (dto.BankCodeSend != null)
            payment.BankCodeSend = dto.BankCodeSend.Trim().ToUpperInvariant();

        if (dto.BankAccountSend != null)
            payment.BankAccountSend = dto.BankAccountSend.Trim();

        if (dto.BankCodeReceive != null)
            payment.BankCodeReceive = dto.BankCodeReceive.Trim().ToUpperInvariant();

        if (dto.BankAccountReceive != null)
            payment.BankAccountReceive = dto.BankAccountReceive.Trim();

        if (dto.Funds != null)
            payment.Funds = dto.Funds.Trim();

        if (dto.BankLending != null)
            payment.BankLending = dto.BankLending.Trim();

        if (dto.InterestRate.HasValue)
            payment.InterestRate = dto.InterestRate.Value;

        if (dto.LoanPeriod.HasValue)
            payment.LoanPeriod = dto.LoanPeriod.Value;

        if (dto.GuaranteeType != null)
            payment.GuaranteeType = dto.GuaranteeType.Trim().ToUpperInvariant();

        if (dto.DepositPercent.HasValue)
            payment.DepositPercent = dto.DepositPercent.Value;

        if (dto.Remark != null)
            payment.Remark = dto.Remark.Trim();

        if (dto.Items is { Count: > 0 })
        {
            db.DealerPaymentDetails.RemoveRange(payment.Details);
            payment.Details.Clear();

            decimal sumItems = 0m;
            foreach (var item in dto.Items)
            {
                if (string.IsNullOrWhiteSpace(item.CarId) || string.IsNullOrWhiteSpace(item.Vin))
                    throw new InvalidOperationException("Chi tiết xe phải có CarId và Vin hợp lệ.");
                if (item.Amount <= 0)
                    throw new InvalidOperationException($"Số tiền thanh toán xe {item.Vin} phải lớn hơn 0.");

                sumItems += item.Amount;
                payment.Details.Add(new DealerPaymentDetail
                {
                    PaymentNo = payment.PaymentNo,
                    CarId = item.CarId.Trim().ToUpperInvariant(),
                    Vin = item.Vin.Trim().ToUpperInvariant(),
                    Model = item.Model.Trim(),
                    Amount = item.Amount,
                    GuaranteeNo = item.GuaranteeNo?.Trim().ToUpperInvariant(),
                    Remark = item.Remark?.Trim()
                });
            }

            payment.TotalAmount = dto.TotalAmount ?? sumItems;
        }
        else if (dto.TotalAmount.HasValue && dto.TotalAmount.Value > 0)
        {
            payment.TotalAmount = dto.TotalAmount.Value;
        }

        await db.SaveChangesAsync();

        return new
        {
            message = "Cập nhật thông tin phiếu thanh toán thành công.",
            payment.PaymentNo,
            PaymentType = payment.PaymentType.ToString(),
            PaymentMethod = payment.PaymentMethod.ToString(),
            payment.TotalAmount,
            Status = payment.Status.ToString(),
            payment.BankPaymentNo,
            TotalCars = payment.Details.Count
        };
    }

    public async Task<object> ListAsync(string? status, string? dealer, string? paymentType, string? contractNo, string? paymentNo)
    {
        var q = db.DealerPayments.AsNoTracking().Where(x => x.OrgId == Org);

        if (!string.IsNullOrWhiteSpace(status))
        {
            if (Enum.TryParse<WholesalePaymentStatus>(status, true, out var st))
                q = q.Where(x => x.Status == st);
        }

        if (!string.IsNullOrWhiteSpace(dealer))
        {
            var d = dealer.Trim().ToUpperInvariant();
            q = q.Where(x => x.DealerCode.Contains(d) || x.DealerName.Contains(dealer.Trim()));
        }

        if (!string.IsNullOrWhiteSpace(paymentType))
        {
            if (Enum.TryParse<WholesalePaymentType>(paymentType, true, out var pt))
                q = q.Where(x => x.PaymentType == pt);
        }

        if (!string.IsNullOrWhiteSpace(contractNo))
        {
            var c = contractNo.Trim().ToUpperInvariant();
            q = q.Where(x => x.ContractNo != null && x.ContractNo.Contains(c));
        }

        if (!string.IsNullOrWhiteSpace(paymentNo))
        {
            var p = paymentNo.Trim().ToUpperInvariant();
            q = q.Where(x => x.PaymentNo.Contains(p));
        }

        var items = await q.OrderByDescending(x => x.CreatedAt)
            .Select(x => new
            {
                x.Id,
                x.PaymentNo,
                x.DealerCode,
                x.DealerName,
                x.ContractNo,
                PaymentType = x.PaymentType.ToString(),
                PaymentMethod = x.PaymentMethod.ToString(),
                x.TotalAmount,
                x.PaymentDate,
                x.PaymentDueDate,
                x.PaymentEndDate,
                Status = x.Status.ToString(),
                x.BankPaymentNo,
                x.BankCodeSend,
                x.BankCodeReceive,
                x.AccountingRecordNo,
                x.AccountingRecordDate,
                TotalCars = x.Details.Count,
                x.CreatedAt,
                x.ApprovedAt,
                x.FinishedAt
            }).ToListAsync();

        return new { total = items.Count, items };
    }

    public async Task<object?> DetailAsync(string paymentNo)
    {
        var payment = await db.DealerPayments.AsNoTracking()
            .Include(x => x.Details)
            .FirstOrDefaultAsync(x => x.OrgId == Org && x.PaymentNo == paymentNo);

        if (payment is null) return null;

        return new
        {
            payment.Id,
            payment.PaymentNo,
            payment.DealerCode,
            payment.DealerName,
            payment.ContractNo,
            PaymentType = payment.PaymentType.ToString(),
            PaymentMethod = payment.PaymentMethod.ToString(),
            payment.TotalAmount,
            payment.PaymentDate,
            payment.PaymentDueDate,
            payment.PaymentEndDate,
            Status = payment.Status.ToString(),
            payment.BankPaymentNo,
            payment.BankCodeSend,
            payment.BankAccountSend,
            payment.BankCodeReceive,
            payment.BankAccountReceive,
            payment.Funds,
            payment.BankLending,
            payment.InterestRate,
            payment.LoanPeriod,
            payment.GuaranteeType,
            payment.DepositPercent,
            payment.AccountingRecordNo,
            payment.AccountingRecordDate,
            payment.Remark,
            payment.RejectReason,
            payment.CancelReason,
            payment.CreatedBy,
            payment.CreatedAt,
            payment.ApprovedBy,
            payment.ApprovedAt,
            payment.FinishedBy,
            payment.FinishedAt,
            payment.CancelledBy,
            payment.CancelledAt,
            Cars = payment.Details.Select(d => new
            {
                d.Id,
                d.CarId,
                d.Vin,
                d.Model,
                d.Amount,
                d.GuaranteeNo,
                d.Remark
            }).ToList()
        };
    }

    public async Task<object?> ApproveAsync(string paymentNo, ApproveDealerPaymentDto? dto)
    {
        var payment = await db.DealerPayments
            .Include(x => x.Details)
            .FirstOrDefaultAsync(x => x.OrgId == Org && x.PaymentNo == paymentNo);

        if (payment is null) return null;

        if (payment.Status != WholesalePaymentStatus.Pending)
            throw new InvalidOperationException($"Chỉ phiếu thanh toán ở trạng thái Pending (Chờ xác nhận) mới được duyệt. Phiếu '{paymentNo}' đang ở trạng thái {payment.Status}.");

        payment.Status = WholesalePaymentStatus.Approved;
        payment.ApprovedBy = dto?.ApprovedBy?.Trim() ?? "NPP_SALES_DIRECTOR";
        payment.ApprovedAt = DateTime.Now;
        payment.PaymentEndDate = dto?.PaymentEndDate ?? DateTime.Today.AddDays(30);

        if (!string.IsNullOrWhiteSpace(dto?.Note))
            payment.Remark = string.IsNullOrWhiteSpace(payment.Remark) ? dto.Note.Trim() : $"{payment.Remark} | {dto.Note.Trim()}";

        await db.SaveChangesAsync();

        return new
        {
            message = "NPP đã duyệt chấp thuận phiếu thanh toán xe thành công. Chuyển kế toán hạch toán.",
            payment.PaymentNo,
            Status = payment.Status.ToString(),
            payment.ApprovedBy,
            payment.ApprovedAt,
            payment.PaymentEndDate
        };
    }

    public async Task<object?> ConfirmAccountingAsync(string paymentNo, ConfirmAccountingRecordDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.AccountingRecordNo))
            throw new InvalidOperationException("Kế toán hoàn tất bắt buộc phải cung cấp Số chứng từ hạch toán ERP/SAP (AccountingRecordNo).");

        var payment = await db.DealerPayments
            .Include(x => x.Details)
            .FirstOrDefaultAsync(x => x.OrgId == Org && x.PaymentNo == paymentNo);

        if (payment is null) return null;

        if (payment.Status != WholesalePaymentStatus.Approved)
            throw new InvalidOperationException($"Chỉ phiếu thanh toán đã được duyệt cấp 1 (Approved) mới có thể hạch toán hoàn tất. Phiếu '{paymentNo}' đang ở trạng thái {payment.Status}.");

        payment.Status = WholesalePaymentStatus.Finished;
        payment.AccountingRecordNo = dto.AccountingRecordNo.Trim().ToUpperInvariant();
        payment.AccountingRecordDate = dto.AccountingRecordDate ?? DateTime.Today;
        payment.FinishedBy = dto.FinishedBy?.Trim() ?? "KETOAN_TRUONG_HTC";
        payment.FinishedAt = DateTime.Now;

        if (!string.IsNullOrWhiteSpace(dto.Note))
            payment.Remark = string.IsNullOrWhiteSpace(payment.Remark) ? dto.Note.Trim() : $"{payment.Remark} | {dto.Note.Trim()}";

        await db.SaveChangesAsync();

        return new
        {
            message = "Kế toán NPP đã xác nhận chứng từ hạch toán hoàn tất thanh toán tiền xe.",
            payment.PaymentNo,
            Status = payment.Status.ToString(),
            payment.AccountingRecordNo,
            payment.AccountingRecordDate,
            payment.FinishedBy,
            payment.FinishedAt,
            payment.TotalAmount
        };
    }

    public async Task<object?> RejectAsync(string paymentNo, RejectDealerPaymentDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Reason))
            throw new InvalidOperationException("Lý do từ chối phiếu thanh toán không được để trống.");

        var payment = await db.DealerPayments
            .FirstOrDefaultAsync(x => x.OrgId == Org && x.PaymentNo == paymentNo);

        if (payment is null) return null;

        if (payment.Status != WholesalePaymentStatus.Pending)
            throw new InvalidOperationException($"Chỉ có thể từ chối phiếu thanh toán ở trạng thái Pending. Phiếu '{paymentNo}' đang ở trạng thái {payment.Status}.");

        payment.Status = WholesalePaymentStatus.Rejected;
        payment.RejectReason = dto.Reason.Trim();

        await db.SaveChangesAsync();

        return new
        {
            message = "Đã từ chối phiếu thanh toán xe.",
            payment.PaymentNo,
            Status = payment.Status.ToString(),
            payment.RejectReason
        };
    }

    public async Task<object?> CancelAsync(string paymentNo, CancelDealerPaymentDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Reason))
            throw new InvalidOperationException("Lý do hủy phiếu thanh toán không được để trống.");

        var payment = await db.DealerPayments
            .FirstOrDefaultAsync(x => x.OrgId == Org && x.PaymentNo == paymentNo);

        if (payment is null) return null;

        if (payment.Status == WholesalePaymentStatus.Finished)
            throw new InvalidOperationException($"Phiếu thanh toán '{paymentNo}' đã hoàn tất hạch toán (Finished), không được phép hủy trực tiếp.");

        if (payment.Status == WholesalePaymentStatus.Cancelled)
            throw new InvalidOperationException($"Phiếu thanh toán '{paymentNo}' đã ở trạng thái hủy trước đó.");

        payment.Status = WholesalePaymentStatus.Cancelled;
        payment.CancelReason = dto.Reason.Trim();
        payment.CancelledBy = dto.CancelledBy?.Trim() ?? "HTC_ACCOUNTING";
        payment.CancelledAt = DateTime.Now;

        await db.SaveChangesAsync();

        return new
        {
            message = "Đã hủy phiếu thanh toán xe thành công.",
            payment.PaymentNo,
            Status = payment.Status.ToString(),
            payment.CancelReason,
            payment.CancelledAt
        };
    }

    public async Task<object?> DeleteDraftAsync(string paymentNo)
    {
        var payment = await db.DealerPayments
            .Include(x => x.Details)
            .FirstOrDefaultAsync(x => x.OrgId == Org && x.PaymentNo == paymentNo);

        if (payment is null) return null;

        if (payment.Status != WholesalePaymentStatus.Pending)
            throw new InvalidOperationException($"Chỉ được phép xóa phiếu thanh toán nháp (Pending). Phiếu '{paymentNo}' đang ở trạng thái {payment.Status}.");

        db.DealerPayments.Remove(payment);
        await db.SaveChangesAsync();

        return new
        {
            message = $"Đã xóa thành công phiếu thanh toán '{paymentNo}'.",
            paymentNo
        };
    }

    public async Task<object?> AddCarAsync(string paymentNo, AddDealerPaymentCarDto dto)
    {
        var payment = await db.DealerPayments
            .Include(x => x.Details)
            .FirstOrDefaultAsync(x => x.OrgId == Org && x.PaymentNo == paymentNo);

        if (payment is null) return null;

        if (payment.Status != WholesalePaymentStatus.Pending)
            throw new InvalidOperationException($"Chỉ có thể thêm xe vào phiếu thanh toán ở trạng thái Pending. Phiếu '{paymentNo}' đang ở trạng thái {payment.Status}.");

        if (string.IsNullOrWhiteSpace(dto.CarId) || string.IsNullOrWhiteSpace(dto.Vin))
            throw new InvalidOperationException("Thông tin xe (CarId, Vin) không được để trống.");

        if (dto.Amount <= 0)
            throw new InvalidOperationException("Số tiền phân bổ cho xe phải lớn hơn 0 VNĐ.");

        var vin = dto.Vin.Trim().ToUpperInvariant();
        if (payment.Details.Any(d => d.Vin == vin))
            throw new InvalidOperationException($"Xe có số VIN '{vin}' đã tồn tại trong phiếu thanh toán.");

        var dtl = new DealerPaymentDetail
        {
            PaymentNo = payment.PaymentNo,
            CarId = dto.CarId.Trim().ToUpperInvariant(),
            Vin = vin,
            Model = dto.Model.Trim(),
            Amount = dto.Amount,
            GuaranteeNo = dto.GuaranteeNo?.Trim().ToUpperInvariant(),
            Remark = dto.Remark?.Trim()
        };

        payment.Details.Add(dtl);
        payment.TotalAmount = payment.Details.Sum(x => x.Amount);

        await db.SaveChangesAsync();

        return new
        {
            message = "Thêm dòng xe phân bổ vào phiếu thanh toán thành công.",
            payment.PaymentNo,
            dtl.Id,
            dtl.Vin,
            dtl.Model,
            dtl.Amount,
            NewTotalAmount = payment.TotalAmount,
            TotalCars = payment.Details.Count
        };
    }

    public async Task<object?> RemoveCarAsync(string paymentNo, long detailId)
    {
        var payment = await db.DealerPayments
            .Include(x => x.Details)
            .FirstOrDefaultAsync(x => x.OrgId == Org && x.PaymentNo == paymentNo);

        if (payment is null) return null;

        if (payment.Status != WholesalePaymentStatus.Pending)
            throw new InvalidOperationException($"Chỉ có thể gỡ dòng xe khi phiếu thanh toán ở trạng thái Pending. Phiếu '{paymentNo}' đang ở trạng thái {payment.Status}.");

        var dtl = payment.Details.FirstOrDefault(x => x.Id == detailId);
        if (dtl is null) return null;

        payment.Details.Remove(dtl);
        payment.TotalAmount = payment.Details.Sum(x => x.Amount);

        await db.SaveChangesAsync();

        return new
        {
            message = "Đã gỡ dòng xe khỏi phiếu thanh toán thành công.",
            payment.PaymentNo,
            detailId,
            NewTotalAmount = payment.TotalAmount,
            TotalCars = payment.Details.Count
        };
    }

    public async Task<object> StatsAsync()
    {
        var payments = await db.DealerPayments.AsNoTracking()
            .Where(x => x.OrgId == Org)
            .ToListAsync();

        return new
        {
            TotalPayments = payments.Count,
            TotalAmount = payments.Sum(x => x.TotalAmount),
            PendingCount = payments.Count(x => x.Status == WholesalePaymentStatus.Pending),
            ApprovedCount = payments.Count(x => x.Status == WholesalePaymentStatus.Approved),
            FinishedCount = payments.Count(x => x.Status == WholesalePaymentStatus.Finished),
            RejectedCount = payments.Count(x => x.Status == WholesalePaymentStatus.Rejected),
            CancelledCount = payments.Count(x => x.Status == WholesalePaymentStatus.Cancelled),
            ByPaymentType = new
            {
                PMT = payments.Count(x => x.PaymentType == WholesalePaymentType.PMT),
                PMA = payments.Count(x => x.PaymentType == WholesalePaymentType.PMA),
                PMC = payments.Count(x => x.PaymentType == WholesalePaymentType.PMC)
            },
            ByMethod = new
            {
                BankTransfer = payments.Count(x => x.PaymentMethod == WholesalePaymentMethod.BankTransfer),
                Cash = payments.Count(x => x.PaymentMethod == WholesalePaymentMethod.Cash),
                CashAndTransfer = payments.Count(x => x.PaymentMethod == WholesalePaymentMethod.CashAndTransfer),
                CreditCard = payments.Count(x => x.PaymentMethod == WholesalePaymentMethod.CreditCard)
            }
        };
    }

    private static WholesalePaymentType ParsePaymentType(string? str) => str?.Trim().ToUpperInvariant() switch
    {
        "PMA" or "ADJUST" => WholesalePaymentType.PMA,
        "PMC" or "CLEARING" => WholesalePaymentType.PMC,
        _ => WholesalePaymentType.PMT
    };

    private static WholesalePaymentMethod ParsePaymentMethod(string? str) => str?.Trim().ToUpperInvariant() switch
    {
        "TM" or "CASH" => WholesalePaymentMethod.Cash,
        "TMCK" or "CASHANDTRANSFER" => WholesalePaymentMethod.CashAndTransfer,
        "TTD" or "CREDITCARD" => WholesalePaymentMethod.CreditCard,
        _ => WholesalePaymentMethod.BankTransfer
    };
}
