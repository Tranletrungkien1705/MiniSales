using Microsoft.EntityFrameworkCore;
using MiniSales.Data;
using MiniSales.Models;

namespace MiniSales.Services;

public record CreateGuaranteeDetailDto(
    string? Vin,
    string Model,
    string? OrderNo,
    decimal GuaranteeValue,
    int NumberOfDaysDeferredPayment,
    DateTime? DateStart,
    DateTime? DateEnd,
    string? Remark
);

public record CreatePaymentGuaranteeDto(
    string? GuaranteeNo,
    string BankGuaranteeNo,
    string BankCode,
    string? BankName,
    string DealerCode,
    string? DealerName,
    string? GuaranteeType, // "BL" | "LC"
    decimal TotalAmount,
    int Term,
    DateTime? DateOpen,
    DateTime? DateExpired,
    decimal? Fee,
    string? BankCodeMonitor,
    DateTime? DateRecieveGrtRoot,
    string? Remark,
    List<CreateGuaranteeDetailDto>? Details
);

public record ApprovePaymentGuaranteeDto(
    string? BankCodeMonitor,
    DateTime? DateRecieveGrtRoot,
    string? Remark
);

public record RejectPaymentGuaranteeDto(
    string Reason
);

public record CancelPaymentGuaranteeDto(
    string? Reason
);

public record AllocateCarDto(
    string? Vin,
    string Model,
    string? OrderNo,
    decimal GuaranteeValue,
    int? NumberOfDaysDeferredPayment,
    DateTime? DateStart,
    DateTime? DateEnd,
    string? Remark
);

public interface IPaymentGuaranteeService
{
    Task<object> CreateAsync(CreatePaymentGuaranteeDto dto);
    Task<object> ListAsync(string? status, string? dealer, string? bankCode, string? guaranteeType);
    Task<object?> DetailAsync(string guaranteeNo);
    Task<object?> ApproveAsync(string guaranteeNo, ApprovePaymentGuaranteeDto? dto);
    Task<object?> RejectAsync(string guaranteeNo, RejectPaymentGuaranteeDto dto);
    Task<object?> CancelAsync(string guaranteeNo, CancelPaymentGuaranteeDto? dto);
    Task<object?> AllocateCarAsync(string guaranteeNo, AllocateCarDto dto);
    Task<object?> RemoveCarAsync(string guaranteeNo, long detailId);
    Task<object> StatsAsync();
}

public sealed class PaymentGuaranteeService(AppDbContext db, ITenantContext tenant) : IPaymentGuaranteeService
{
    private Guid Org => tenant.OrgId;

    public async Task<object> CreateAsync(CreatePaymentGuaranteeDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.DealerCode))
            throw new InvalidOperationException("DealerCode (mã đại lý) không được để trống.");
        if (string.IsNullOrWhiteSpace(dto.BankGuaranteeNo))
            throw new InvalidOperationException("BankGuaranteeNo (số thư bảo lãnh ngân hàng) không được để trống.");
        if (string.IsNullOrWhiteSpace(dto.BankCode))
            throw new InvalidOperationException("BankCode (mã ngân hàng phát hành bảo lãnh) không được để trống.");
        if (dto.TotalAmount <= 0)
            throw new InvalidOperationException("TotalAmount (hạn mức bảo lãnh) phải lớn hơn 0.");

        var gType = string.Equals(dto.GuaranteeType, "LC", StringComparison.OrdinalIgnoreCase)
            ? GuaranteeType.LC
            : GuaranteeType.BL;

        var guaranteeNo = string.IsNullOrWhiteSpace(dto.GuaranteeNo)
            ? "GRT" + DateTime.Now.ToString("yyMMddHHmmss")
            : dto.GuaranteeNo.Trim().ToUpperInvariant();

        if (await db.PaymentGuarantees.AnyAsync(x => x.OrgId == Org && x.GuaranteeNo == guaranteeNo))
            throw new InvalidOperationException($"Số bảo lãnh {guaranteeNo} đã tồn tại trong hệ thống.");

        var dateOpen = dto.DateOpen ?? DateTime.Today;
        var dateExpired = dto.DateExpired ?? dateOpen.AddMonths(dto.Term > 0 ? dto.Term : 12);
        if (dateExpired < dateOpen)
            throw new InvalidOperationException("DateExpired (ngày hết hạn) không thể nhỏ hơn DateOpen (ngày mở bảo lãnh).");

        decimal allocated = 0m;
        var details = new List<PaymentGuaranteeDetail>();
        if (dto.Details is not null && dto.Details.Count > 0)
        {
            foreach (var dt in dto.Details)
            {
                if (string.IsNullOrWhiteSpace(dt.Model))
                    throw new InvalidOperationException("Model xe trong chi tiết bảo lãnh không được để trống.");
                if (dt.GuaranteeValue <= 0)
                    throw new InvalidOperationException($"Giá trị bảo lãnh cho xe {dt.Model} phải lớn hơn 0.");

                allocated += dt.GuaranteeValue;
                details.Add(new PaymentGuaranteeDetail
                {
                    Vin = dt.Vin?.Trim(),
                    Model = dt.Model.Trim(),
                    OrderNo = dt.OrderNo?.Trim(),
                    GuaranteeValue = dt.GuaranteeValue,
                    NumberOfDaysDeferredPayment = dt.NumberOfDaysDeferredPayment > 0 ? dt.NumberOfDaysDeferredPayment : 30,
                    DateStart = dt.DateStart ?? dateOpen,
                    DateEnd = dt.DateEnd ?? dateExpired,
                    Remark = dt.Remark?.Trim()
                });
            }

            if (allocated > dto.TotalAmount)
                throw new InvalidOperationException($"Tổng giá trị phân bổ ({allocated:N0}) vượt quá hạn mức bảo lãnh ({dto.TotalAmount:N0}).");
        }

        var guarantee = new PaymentGuarantee
        {
            OrgId = Org,
            GuaranteeNo = guaranteeNo,
            BankGuaranteeNo = dto.BankGuaranteeNo.Trim().ToUpperInvariant(),
            BankCode = dto.BankCode.Trim().ToUpperInvariant(),
            BankName = string.IsNullOrWhiteSpace(dto.BankName) ? dto.BankCode.Trim().ToUpperInvariant() : dto.BankName.Trim(),
            DealerCode = dto.DealerCode.Trim().ToUpperInvariant(),
            DealerName = string.IsNullOrWhiteSpace(dto.DealerName) ? dto.DealerCode.Trim().ToUpperInvariant() : dto.DealerName.Trim(),
            GuaranteeType = gType,
            Status = GuaranteeStatus.Pending,
            TotalAmount = dto.TotalAmount,
            AllocatedAmount = allocated,
            Term = dto.Term > 0 ? dto.Term : 12,
            DateOpen = dateOpen,
            DateExpired = dateExpired,
            Fee = dto.Fee ?? 0m,
            BankCodeMonitor = dto.BankCodeMonitor?.Trim().ToUpperInvariant(),
            DateRecieveGrtRoot = dto.DateRecieveGrtRoot,
            Remark = dto.Remark?.Trim(),
            CreatedAt = DateTime.Now,
            Details = details
        };

        db.PaymentGuarantees.Add(guarantee);
        await db.SaveChangesAsync();

        return new
        {
            guaranteeNo = guarantee.GuaranteeNo,
            bankGuaranteeNo = guarantee.BankGuaranteeNo,
            bankCode = guarantee.BankCode,
            bankName = guarantee.BankName,
            dealerCode = guarantee.DealerCode,
            dealerName = guarantee.DealerName,
            guaranteeType = guarantee.GuaranteeType.ToString(),
            status = guarantee.Status.ToString(),
            totalAmount = guarantee.TotalAmount,
            allocatedAmount = guarantee.AllocatedAmount,
            availableAmount = guarantee.TotalAmount - guarantee.AllocatedAmount,
            dateOpen = guarantee.DateOpen,
            dateExpired = guarantee.DateExpired,
            detailCount = guarantee.Details.Count,
            createdAt = guarantee.CreatedAt
        };
    }

    public async Task<object> ListAsync(string? status, string? dealer, string? bankCode, string? guaranteeType)
    {
        var q = db.PaymentGuarantees.Where(x => x.OrgId == Org);

        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<GuaranteeStatus>(status, true, out var st))
            q = q.Where(x => x.Status == st);
        if (!string.IsNullOrWhiteSpace(dealer))
            q = q.Where(x => x.DealerCode == dealer.Trim().ToUpperInvariant());
        if (!string.IsNullOrWhiteSpace(bankCode))
            q = q.Where(x => x.BankCode == bankCode.Trim().ToUpperInvariant());
        if (!string.IsNullOrWhiteSpace(guaranteeType) && Enum.TryParse<GuaranteeType>(guaranteeType, true, out var gt))
            q = q.Where(x => x.GuaranteeType == gt);

        var items = await q.OrderByDescending(x => x.Id).Take(500).Select(x => new
        {
            x.GuaranteeNo,
            x.BankGuaranteeNo,
            x.BankCode,
            x.BankName,
            x.DealerCode,
            x.DealerName,
            guaranteeType = x.GuaranteeType.ToString(),
            status = x.Status.ToString(),
            x.TotalAmount,
            x.AllocatedAmount,
            availableAmount = x.TotalAmount - x.AllocatedAmount,
            x.Term,
            x.DateOpen,
            x.DateExpired,
            x.BankCodeMonitor,
            x.CreatedAt,
            x.ApprovedAt
        }).ToListAsync();

        return new { count = items.Count, items };
    }

    private async Task<PaymentGuarantee?> GetByNo(string guaranteeNo)
    {
        var code = guaranteeNo.Trim().ToUpperInvariant();
        return await db.PaymentGuarantees
            .Include(x => x.Details)
            .FirstOrDefaultAsync(x => x.OrgId == Org && x.GuaranteeNo == code);
    }

    public async Task<object?> DetailAsync(string guaranteeNo)
    {
        var item = await GetByNo(guaranteeNo);
        if (item is null) return null;

        return new
        {
            item.GuaranteeNo,
            item.BankGuaranteeNo,
            item.BankCode,
            item.BankName,
            item.DealerCode,
            item.DealerName,
            guaranteeType = item.GuaranteeType.ToString(),
            status = item.Status.ToString(),
            item.TotalAmount,
            item.AllocatedAmount,
            availableAmount = item.TotalAmount - item.AllocatedAmount,
            item.Term,
            item.DateOpen,
            item.DateExpired,
            item.Fee,
            item.BankCodeMonitor,
            item.DateRecieveGrtRoot,
            item.Remark,
            item.RemarkReject,
            item.CreatedAt,
            item.ApprovedAt,
            item.CancelledAt,
            details = item.Details.Select(d => new
            {
                d.Id,
                d.Vin,
                d.Model,
                d.OrderNo,
                d.GuaranteeValue,
                d.NumberOfDaysDeferredPayment,
                d.DateStart,
                d.DateEnd,
                d.Remark
            }).ToList()
        };
    }

    public async Task<object?> ApproveAsync(string guaranteeNo, ApprovePaymentGuaranteeDto? dto)
    {
        var item = await GetByNo(guaranteeNo);
        if (item is null) return null;

        if (item.Status != GuaranteeStatus.Pending)
            throw new InvalidOperationException($"Bảo lãnh {item.GuaranteeNo} không ở trạng thái Chờ duyệt (hiện tại: {item.Status}).");

        item.Status = GuaranteeStatus.Approved;
        item.ApprovedAt = DateTime.Now;
        if (!string.IsNullOrWhiteSpace(dto?.BankCodeMonitor))
            item.BankCodeMonitor = dto.BankCodeMonitor.Trim().ToUpperInvariant();
        if (dto?.DateRecieveGrtRoot.HasValue == true)
            item.DateRecieveGrtRoot = dto.DateRecieveGrtRoot.Value;
        if (!string.IsNullOrWhiteSpace(dto?.Remark))
        {
            item.Remark = string.IsNullOrWhiteSpace(item.Remark)
                ? dto.Remark.Trim()
                : $"{item.Remark} | Duyệt: {dto.Remark.Trim()}";
        }

        await db.SaveChangesAsync();

        return new
        {
            item.GuaranteeNo,
            item.BankGuaranteeNo,
            status = item.Status.ToString(),
            item.BankCodeMonitor,
            item.ApprovedAt,
            item.TotalAmount,
            item.AllocatedAmount
        };
    }

    public async Task<object?> RejectAsync(string guaranteeNo, RejectPaymentGuaranteeDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Reason))
            throw new InvalidOperationException("Cần cung cấp lý do từ chối bảo lãnh.");

        var item = await GetByNo(guaranteeNo);
        if (item is null) return null;

        if (item.Status != GuaranteeStatus.Pending)
            throw new InvalidOperationException($"Bảo lãnh {item.GuaranteeNo} ở trạng thái {item.Status} — không thể từ chối.");

        item.Status = GuaranteeStatus.Rejected;
        item.RemarkReject = dto.Reason.Trim();
        await db.SaveChangesAsync();

        return new
        {
            item.GuaranteeNo,
            status = item.Status.ToString(),
            item.RemarkReject
        };
    }

    public async Task<object?> CancelAsync(string guaranteeNo, CancelPaymentGuaranteeDto? dto)
    {
        var item = await GetByNo(guaranteeNo);
        if (item is null) return null;

        if (item.Status == GuaranteeStatus.Cancelled)
            throw new InvalidOperationException($"Bảo lãnh {item.GuaranteeNo} đã bị hủy trước đó.");

        item.Status = GuaranteeStatus.Cancelled;
        item.CancelledAt = DateTime.Now;
        if (!string.IsNullOrWhiteSpace(dto?.Reason))
        {
            item.Remark = string.IsNullOrWhiteSpace(item.Remark)
                ? $"[Hủy: {dto.Reason.Trim()}]"
                : $"{item.Remark} [Hủy: {dto.Reason.Trim()}]";
        }

        await db.SaveChangesAsync();

        return new
        {
            item.GuaranteeNo,
            status = item.Status.ToString(),
            item.CancelledAt,
            item.Remark
        };
    }

    public async Task<object?> AllocateCarAsync(string guaranteeNo, AllocateCarDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Model))
            throw new InvalidOperationException("Model xe phân bổ không được để trống.");
        if (dto.GuaranteeValue <= 0)
            throw new InvalidOperationException("Giá trị bảo lãnh cho xe phải lớn hơn 0.");

        var item = await GetByNo(guaranteeNo);
        if (item is null) return null;

        if (item.Status != GuaranteeStatus.Approved && item.Status != GuaranteeStatus.Pending)
            throw new InvalidOperationException($"Bảo lãnh {item.GuaranteeNo} ở trạng thái {item.Status} — không thể gán thêm xe.");

        var newAllocated = item.AllocatedAmount + dto.GuaranteeValue;
        if (newAllocated > item.TotalAmount)
            throw new InvalidOperationException($"Vượt hạn mức bảo lãnh! Hiện đã phân bổ {item.AllocatedAmount:N0} / {item.TotalAmount:N0}. Số tiền cần thêm {dto.GuaranteeValue:N0} vượt hạn mức còn lại {item.TotalAmount - item.AllocatedAmount:N0}.");

        var detail = new PaymentGuaranteeDetail
        {
            GuaranteeId = item.Id,
            Vin = dto.Vin?.Trim(),
            Model = dto.Model.Trim(),
            OrderNo = dto.OrderNo?.Trim(),
            GuaranteeValue = dto.GuaranteeValue,
            NumberOfDaysDeferredPayment = dto.NumberOfDaysDeferredPayment.GetValueOrDefault(30),
            DateStart = dto.DateStart ?? item.DateOpen,
            DateEnd = dto.DateEnd ?? item.DateExpired,
            Remark = dto.Remark?.Trim()
        };

        item.Details.Add(detail);
        item.AllocatedAmount = newAllocated;

        await db.SaveChangesAsync();

        return new
        {
            item.GuaranteeNo,
            allocatedDetailId = detail.Id,
            detail.Vin,
            detail.Model,
            detail.OrderNo,
            detail.GuaranteeValue,
            item.TotalAmount,
            item.AllocatedAmount,
            availableAmount = item.TotalAmount - item.AllocatedAmount
        };
    }

    public async Task<object?> RemoveCarAsync(string guaranteeNo, long detailId)
    {
        var item = await GetByNo(guaranteeNo);
        if (item is null) return null;

        var detail = item.Details.FirstOrDefault(x => x.Id == detailId);
        if (detail is null)
            throw new InvalidOperationException($"Không tìm thấy xe chi tiết ID {detailId} trong bảo lãnh {item.GuaranteeNo}.");

        item.AllocatedAmount = Math.Max(0, item.AllocatedAmount - detail.GuaranteeValue);
        item.Details.Remove(detail);
        db.PaymentGuaranteeDetails.Remove(detail);

        await db.SaveChangesAsync();

        return new
        {
            item.GuaranteeNo,
            removedDetailId = detailId,
            item.TotalAmount,
            item.AllocatedAmount,
            availableAmount = item.TotalAmount - item.AllocatedAmount
        };
    }

    public async Task<object> StatsAsync()
    {
        var q = db.PaymentGuarantees.Where(x => x.OrgId == Org);
        var totalAmount = await q.SumAsync(x => x.TotalAmount);
        var allocatedAmount = await q.SumAsync(x => x.AllocatedAmount);

        return new
        {
            total = await q.CountAsync(),
            pending = await q.CountAsync(x => x.Status == GuaranteeStatus.Pending),
            approved = await q.CountAsync(x => x.Status == GuaranteeStatus.Approved),
            rejected = await q.CountAsync(x => x.Status == GuaranteeStatus.Rejected),
            cancelled = await q.CountAsync(x => x.Status == GuaranteeStatus.Cancelled),
            totalAmount,
            totalAllocated = allocatedAmount,
            totalAvailable = totalAmount - allocatedAmount
        };
    }
}
