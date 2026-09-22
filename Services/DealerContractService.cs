using Microsoft.EntityFrameworkCore;
using MiniSales.Data;
using MiniSales.Models;

namespace MiniSales.Services;

public record CreateDealerContractCarDto(
    string? CarId,
    string Vin,
    string Model,
    string? SpecCode,
    string? ColorCode,
    int? ProductionYear,
    decimal UnitPrice,
    decimal? VatRate,
    string? OrderNo,
    string? Remark
);

public record CreateDealerContractDto(
    string? ContractNo,
    string DealerCode,
    string? DealerName,
    DateTime? ContractDate,
    string? PaymentType,
    string? BankCode,
    string? BankName,
    decimal? DepositPercent,
    int? GuaranteeDays,
    string? Remark,
    List<CreateDealerContractCarDto>? Cars
);

public record CreateAdjustDealerContractDto(
    string? ContractNo,
    string ParentContractNo,
    string? Remark,
    List<CreateDealerContractCarDto>? Cars
);

public record UpdateDealerContractDto(
    string? PaymentType,
    string? BankCode,
    string? BankName,
    decimal? DepositPercent,
    int? GuaranteeDays,
    string? Remark
);

public record UpdateContractBankDto(
    string BankCode,
    string? BankName
);

public record Approve1DealerContractDto(
    string? ApprovedBy,
    string? Note
);

public record SignDealerContractDto(
    string? SignedBy,
    string? FilePath,
    string? Note
);

public record Approve2HQDealerContractDto(
    string? SignedBy,
    string? FilePath,
    string? Note
);

public record RejectDealerContractDto(
    string Reason,
    string? RejectedBy
);

public record CancelDealerContractDto(
    string Reason,
    string? CancelledBy
);

public record AddDealerContractCarDto(
    string? CarId,
    string Vin,
    string Model,
    string? SpecCode,
    string? ColorCode,
    int? ProductionYear,
    decimal UnitPrice,
    decimal? VatRate,
    string? OrderNo,
    string? Remark
);

public interface IDealerContractService
{
    Task<object> CreateAsync(CreateDealerContractDto dto);
    Task<object> CreateAdjustAsync(CreateAdjustDealerContractDto dto);
    Task<object?> UpdateAsync(string contractNo, UpdateDealerContractDto dto);
    Task<object?> UpdateBankCodeAsync(string contractNo, UpdateContractBankDto dto);
    Task<object> ListAsync(string? status, string? dealer, string? paymentType, string? contractType, string? contractNo);
    Task<object?> DetailAsync(string contractNo);
    Task<object?> Approve1HQAsync(string contractNo, Approve1DealerContractDto? dto);
    Task<object?> ApproveDLAsync(string contractNo, SignDealerContractDto? dto);
    Task<object?> Approve2HQAsync(string contractNo, Approve2HQDealerContractDto? dto);
    Task<object?> RejectHQAsync(string contractNo, RejectDealerContractDto dto);
    Task<object?> CancelDLAsync(string contractNo, CancelDealerContractDto dto);
    Task<object?> DeleteDraftAsync(string contractNo);
    Task<object?> AddCarAsync(string contractNo, AddDealerContractCarDto dto);
    Task<object?> RemoveCarAsync(string contractNo, long detailId);
    Task<object> StatsAsync();
}

public sealed class DealerContractService(AppDbContext db, ITenantContext tenant) : IDealerContractService
{
    private Guid Org => tenant.OrgId;

    public async Task<object> CreateAsync(CreateDealerContractDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.DealerCode))
            throw new InvalidOperationException("Mã đại lý (DealerCode) là bắt buộc.");

        if (dto.Cars is null || dto.Cars.Count == 0)
            throw new InvalidOperationException("Hợp đồng bán buôn phải có ít nhất 01 dòng xe.");

        var contractNo = string.IsNullOrWhiteSpace(dto.ContractNo)
            ? await GenerateContractNoAsync(false)
            : dto.ContractNo.Trim().ToUpperInvariant();

        if (await db.DealerContracts.AnyAsync(x => x.OrgId == Org && x.ContractNo == contractNo))
            throw new InvalidOperationException($"Số hợp đồng '{contractNo}' đã tồn tại trong hệ thống.");

        var pmtType = ParsePaymentType(dto.PaymentType);
        var bankCode = string.IsNullOrWhiteSpace(dto.BankCode)
            ? (pmtType == DealerContractPaymentType.Cash ? "DEALER" : "BIDV")
            : dto.BankCode.Trim().ToUpperInvariant();

        var contract = new DealerContract
        {
            OrgId = Org,
            ContractNo = contractNo,
            DealerCode = dto.DealerCode.Trim().ToUpperInvariant(),
            DealerName = string.IsNullOrWhiteSpace(dto.DealerName) ? dto.DealerCode.Trim() : dto.DealerName.Trim(),
            ContractDate = dto.ContractDate ?? DateTime.Today,
            ContractType = DealerContractType.Standard,
            PaymentType = pmtType,
            BankCode = bankCode,
            BankName = dto.BankName?.Trim(),
            DepositPercent = dto.DepositPercent ?? 10m,
            GuaranteeDays = dto.GuaranteeDays ?? (pmtType == DealerContractPaymentType.Guarantee ? 15 : (pmtType == DealerContractPaymentType.LC ? 30 : 0)),
            Status = DealerContractStatus.Draft,
            DealerSignStatus = DealerContractSignStatus.Pending,
            HQSignStatus = DealerContractSignStatus.Pending,
            Remark = dto.Remark?.Trim(),
            CreatedBy = "DEALER_USER",
            CreatedAt = DateTime.Now
        };

        foreach (var c in dto.Cars)
        {
            if (string.IsNullOrWhiteSpace(c.Vin) || string.IsNullOrWhiteSpace(c.Model))
                throw new InvalidOperationException("Mỗi dòng xe trong hợp đồng bắt buộc phải có VIN và Model.");

            var rate = c.VatRate ?? 10m;
            var vatAmount = Math.Round(c.UnitPrice * rate / 100m, 2);
            var total = c.UnitPrice + vatAmount;

            contract.Details.Add(new DealerContractDetail
            {
                CarId = string.IsNullOrWhiteSpace(c.CarId) ? "CAR-" + c.Vin.Trim().ToUpperInvariant() : c.CarId.Trim().ToUpperInvariant(),
                Vin = c.Vin.Trim().ToUpperInvariant(),
                Model = c.Model.Trim(),
                SpecCode = c.SpecCode?.Trim(),
                ColorCode = c.ColorCode?.Trim(),
                ProductionYear = c.ProductionYear ?? DateTime.Today.Year,
                UnitPrice = c.UnitPrice,
                VatRate = rate,
                TotalAmount = total,
                OrderNo = c.OrderNo?.Trim(),
                Remark = c.Remark?.Trim()
            });
        }

        contract.TotalCars = contract.Details.Count;
        contract.TotalAmount = contract.Details.Sum(x => x.TotalAmount);

        db.DealerContracts.Add(contract);
        await db.SaveChangesAsync();

        return new
        {
            message = "Tạo hợp đồng bán buôn xe đại lý thành công.",
            contractNo = contract.ContractNo,
            dealerCode = contract.DealerCode,
            totalCars = contract.TotalCars,
            totalAmount = contract.TotalAmount,
            status = contract.Status.ToString(),
            contractDate = contract.ContractDate.ToString("yyyy-MM-dd")
        };
    }

    public async Task<object> CreateAdjustAsync(CreateAdjustDealerContractDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.ParentContractNo))
            throw new InvalidOperationException("Số hợp đồng gốc (ParentContractNo) là bắt buộc khi tạo HĐ điều chỉnh.");

        var parentNo = dto.ParentContractNo.Trim().ToUpperInvariant();
        var parent = await db.DealerContracts
            .Include(x => x.Details)
            .FirstOrDefaultAsync(x => x.OrgId == Org && x.ContractNo == parentNo);

        if (parent is null)
            throw new InvalidOperationException($"Không tìm thấy hợp đồng gốc '{parentNo}'.");

        if (parent.Status != DealerContractStatus.Signed)
            throw new InvalidOperationException($"Chỉ có thể lập HĐ điều chỉnh từ hợp đồng gốc đã ký kết hiệu lực (Signed). Trạng thái hiện tại: {parent.Status}.");

        var contractNo = string.IsNullOrWhiteSpace(dto.ContractNo)
            ? await GenerateContractNoAsync(true)
            : dto.ContractNo.Trim().ToUpperInvariant();

        if (await db.DealerContracts.AnyAsync(x => x.OrgId == Org && x.ContractNo == contractNo))
            throw new InvalidOperationException($"Số hợp đồng điều chỉnh '{contractNo}' đã tồn tại.");

        var adjust = new DealerContract
        {
            OrgId = Org,
            ContractNo = contractNo,
            DealerCode = parent.DealerCode,
            DealerName = parent.DealerName,
            ContractDate = DateTime.Today,
            ContractType = DealerContractType.Adjust,
            ParentContractNo = parent.ContractNo,
            PaymentType = parent.PaymentType,
            BankCode = parent.BankCode,
            BankName = parent.BankName,
            DepositPercent = parent.DepositPercent,
            GuaranteeDays = parent.GuaranteeDays,
            Status = DealerContractStatus.Draft,
            DealerSignStatus = DealerContractSignStatus.Pending,
            HQSignStatus = DealerContractSignStatus.Pending,
            Remark = string.IsNullOrWhiteSpace(dto.Remark) ? $"HĐ điều chỉnh bổ sung/thay đổi cho HĐ gốc {parent.ContractNo}" : dto.Remark.Trim(),
            CreatedBy = "HQ_ADJUSTER",
            CreatedAt = DateTime.Now
        };

        var carsToUse = dto.Cars is not null && dto.Cars.Count > 0 ? dto.Cars : parent.Details.Select(d => new CreateDealerContractCarDto(
            d.CarId, d.Vin, d.Model, d.SpecCode, d.ColorCode, d.ProductionYear, d.UnitPrice, d.VatRate, d.OrderNo, d.Remark
        )).ToList();

        foreach (var c in carsToUse)
        {
            var rate = c.VatRate ?? 10m;
            var vatAmount = Math.Round(c.UnitPrice * rate / 100m, 2);
            var total = c.UnitPrice + vatAmount;

            adjust.Details.Add(new DealerContractDetail
            {
                CarId = string.IsNullOrWhiteSpace(c.CarId) ? "CAR-" + c.Vin.Trim().ToUpperInvariant() : c.CarId.Trim().ToUpperInvariant(),
                Vin = c.Vin.Trim().ToUpperInvariant(),
                Model = c.Model.Trim(),
                SpecCode = c.SpecCode?.Trim(),
                ColorCode = c.ColorCode?.Trim(),
                ProductionYear = c.ProductionYear ?? DateTime.Today.Year,
                UnitPrice = c.UnitPrice,
                VatRate = rate,
                TotalAmount = total,
                OrderNo = c.OrderNo?.Trim(),
                Remark = c.Remark?.Trim()
            });
        }

        adjust.TotalCars = adjust.Details.Count;
        adjust.TotalAmount = adjust.Details.Sum(x => x.TotalAmount);

        db.DealerContracts.Add(adjust);
        await db.SaveChangesAsync();

        return new
        {
            message = "Tạo hợp đồng điều chỉnh bán buôn xe thành công.",
            contractNo = adjust.ContractNo,
            parentContractNo = adjust.ParentContractNo,
            dealerCode = adjust.DealerCode,
            totalCars = adjust.TotalCars,
            totalAmount = adjust.TotalAmount,
            status = adjust.Status.ToString()
        };
    }

    public async Task<object?> UpdateAsync(string contractNo, UpdateDealerContractDto dto)
    {
        var code = contractNo.Trim().ToUpperInvariant();
        var item = await db.DealerContracts.FirstOrDefaultAsync(x => x.OrgId == Org && x.ContractNo == code);
        if (item is null) return null;

        if (item.Status != DealerContractStatus.Draft)
            throw new InvalidOperationException($"Chỉ được phép sửa đổi hợp đồng khi ở trạng thái Draft. Trạng thái hiện tại: {item.Status}.");

        if (!string.IsNullOrWhiteSpace(dto.PaymentType))
            item.PaymentType = ParsePaymentType(dto.PaymentType);

        if (!string.IsNullOrWhiteSpace(dto.BankCode))
            item.BankCode = dto.BankCode.Trim().ToUpperInvariant();

        if (dto.BankName != null)
            item.BankName = dto.BankName.Trim();

        if (dto.DepositPercent.HasValue)
            item.DepositPercent = dto.DepositPercent.Value;

        if (dto.GuaranteeDays.HasValue)
            item.GuaranteeDays = dto.GuaranteeDays.Value;

        if (dto.Remark != null)
            item.Remark = dto.Remark.Trim();

        await db.SaveChangesAsync();

        return new
        {
            message = "Cập nhật hợp đồng thành công.",
            contractNo = item.ContractNo,
            paymentType = item.PaymentType.ToString(),
            bankCode = item.BankCode,
            depositPercent = item.DepositPercent,
            guaranteeDays = item.GuaranteeDays,
            remark = item.Remark
        };
    }

    public async Task<object?> UpdateBankCodeAsync(string contractNo, UpdateContractBankDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.BankCode))
            throw new InvalidOperationException("Mã ngân hàng (BankCode) là bắt buộc.");

        var code = contractNo.Trim().ToUpperInvariant();
        var item = await db.DealerContracts.FirstOrDefaultAsync(x => x.OrgId == Org && x.ContractNo == code);
        if (item is null) return null;

        if (item.Status == DealerContractStatus.Cancelled || item.Status == DealerContractStatus.Adjusted)
            throw new InvalidOperationException($"Không thể cập nhật ngân hàng cho hợp đồng ở trạng thái {item.Status}.");

        item.BankCode = dto.BankCode.Trim().ToUpperInvariant();
        if (!string.IsNullOrWhiteSpace(dto.BankName))
            item.BankName = dto.BankName.Trim();

        await db.SaveChangesAsync();

        return new
        {
            message = "Cập nhật ngân hàng tài trợ/bảo lãnh thành công.",
            contractNo = item.ContractNo,
            bankCode = item.BankCode,
            bankName = item.BankName
        };
    }

    public async Task<object> ListAsync(string? status, string? dealer, string? paymentType, string? contractType, string? contractNo)
    {
        var q = db.DealerContracts.AsNoTracking().Where(x => x.OrgId == Org);

        if (!string.IsNullOrWhiteSpace(status))
        {
            if (Enum.TryParse<DealerContractStatus>(status.Trim(), true, out var st))
                q = q.Where(x => x.Status == st);
        }

        if (!string.IsNullOrWhiteSpace(dealer))
        {
            var d = dealer.Trim().ToUpperInvariant();
            q = q.Where(x => x.DealerCode.ToUpper().Contains(d) || x.DealerName.ToUpper().Contains(d));
        }

        if (!string.IsNullOrWhiteSpace(paymentType))
        {
            if (Enum.TryParse<DealerContractPaymentType>(paymentType.Trim(), true, out var pt))
                q = q.Where(x => x.PaymentType == pt);
        }

        if (!string.IsNullOrWhiteSpace(contractType))
        {
            if (Enum.TryParse<DealerContractType>(contractType.Trim(), true, out var ct))
                q = q.Where(x => x.ContractType == ct);
        }

        if (!string.IsNullOrWhiteSpace(contractNo))
        {
            var cn = contractNo.Trim().ToUpperInvariant();
            q = q.Where(x => x.ContractNo.Contains(cn));
        }

        var list = await q.OrderByDescending(x => x.CreatedAt)
            .Select(x => new
            {
                x.Id,
                x.ContractNo,
                x.DealerCode,
                x.DealerName,
                contractDate = x.ContractDate.ToString("yyyy-MM-dd"),
                contractType = x.ContractType.ToString(),
                x.ParentContractNo,
                paymentType = x.PaymentType.ToString(),
                x.BankCode,
                x.BankName,
                x.DepositPercent,
                x.GuaranteeDays,
                x.TotalCars,
                x.TotalAmount,
                status = x.Status.ToString(),
                dealerSignStatus = x.DealerSignStatus.ToString(),
                hqSignStatus = x.HQSignStatus.ToString(),
                createdAt = x.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss")
            })
            .ToListAsync();

        return list;
    }

    public async Task<object?> DetailAsync(string contractNo)
    {
        var code = contractNo.Trim().ToUpperInvariant();
        var item = await db.DealerContracts.AsNoTracking()
            .Include(x => x.Details)
            .FirstOrDefaultAsync(x => x.OrgId == Org && x.ContractNo == code);

        if (item is null) return null;

        return new
        {
            item.Id,
            item.ContractNo,
            item.DealerCode,
            item.DealerName,
            contractDate = item.ContractDate.ToString("yyyy-MM-dd"),
            contractType = item.ContractType.ToString(),
            item.ParentContractNo,
            paymentType = item.PaymentType.ToString(),
            item.BankCode,
            item.BankName,
            item.DepositPercent,
            item.GuaranteeDays,
            item.TotalCars,
            item.TotalAmount,
            status = item.Status.ToString(),
            dealerSignStatus = item.DealerSignStatus.ToString(),
            hqSignStatus = item.HQSignStatus.ToString(),
            item.DealerSignedBy,
            dealerSignedAt = item.DealerSignedAt?.ToString("yyyy-MM-dd HH:mm:ss"),
            item.HQSignedBy,
            hqSignedAt = item.HQSignedAt?.ToString("yyyy-MM-dd HH:mm:ss"),
            approved1At = item.Approved1At?.ToString("yyyy-MM-dd HH:mm:ss"),
            item.SignedFilePath,
            item.RejectReason,
            item.CancelReason,
            item.Remark,
            item.CreatedBy,
            createdAt = item.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss"),
            cancelledAt = item.CancelledAt?.ToString("yyyy-MM-dd HH:mm:ss"),
            details = item.Details.Select(d => new
            {
                d.Id,
                d.CarId,
                d.Vin,
                d.Model,
                d.SpecCode,
                d.ColorCode,
                d.ProductionYear,
                d.UnitPrice,
                d.VatRate,
                d.TotalAmount,
                d.OrderNo,
                d.Remark
            }).ToList()
        };
    }

    public async Task<object?> Approve1HQAsync(string contractNo, Approve1DealerContractDto? dto)
    {
        var code = contractNo.Trim().ToUpperInvariant();
        var item = await db.DealerContracts.FirstOrDefaultAsync(x => x.OrgId == Org && x.ContractNo == code);
        if (item is null) return null;

        if (item.Status != DealerContractStatus.Draft)
            throw new InvalidOperationException($"Chỉ thẩm tra duyệt cấp 1 cho hợp đồng ở trạng thái Draft. Trạng thái hiện tại: {item.Status}.");

        item.Status = DealerContractStatus.PreliminaryApproved;
        item.Approved1At = DateTime.Now;
        if (!string.IsNullOrWhiteSpace(dto?.Note))
            item.Remark = string.IsNullOrWhiteSpace(item.Remark) ? dto.Note.Trim() : $"{item.Remark}; HQ1: {dto.Note.Trim()}";

        await db.SaveChangesAsync();

        return new
        {
            message = "NPP thẩm tra & duyệt sơ bộ cấp 1 thành công. Hợp đồng chuyển sang chờ Đại lý ký số.",
            contractNo = item.ContractNo,
            status = item.Status.ToString(),
            approved1At = item.Approved1At?.ToString("yyyy-MM-dd HH:mm:ss")
        };
    }

    public async Task<object?> ApproveDLAsync(string contractNo, SignDealerContractDto? dto)
    {
        var code = contractNo.Trim().ToUpperInvariant();
        var item = await db.DealerContracts.FirstOrDefaultAsync(x => x.OrgId == Org && x.ContractNo == code);
        if (item is null) return null;

        if (item.Status != DealerContractStatus.PreliminaryApproved && item.Status != DealerContractStatus.Draft)
            throw new InvalidOperationException($"Đại lý chỉ có thể ký số khi hợp đồng ở trạng thái Draft hoặc PreliminaryApproved. Trạng thái hiện tại: {item.Status}.");

        item.DealerSignStatus = DealerContractSignStatus.Signed;
        item.DealerSignedAt = DateTime.Now;
        item.DealerSignedBy = string.IsNullOrWhiteSpace(dto?.SignedBy) ? "DEALER_AUTHORIZED_SIGNER" : dto.SignedBy.Trim();

        if (!string.IsNullOrWhiteSpace(dto?.FilePath))
            item.SignedFilePath = dto.FilePath.Trim();

        if (!string.IsNullOrWhiteSpace(dto?.Note))
            item.Remark = string.IsNullOrWhiteSpace(item.Remark) ? dto.Note.Trim() : $"{item.Remark}; DL: {dto.Note.Trim()}";

        await db.SaveChangesAsync();

        return new
        {
            message = "Đại lý ký số xác nhận hợp đồng thành công. Chờ lãnh đạo NPP phê duyệt lần 2 & ký hoàn tất.",
            contractNo = item.ContractNo,
            dealerSignStatus = item.DealerSignStatus.ToString(),
            dealerSignedBy = item.DealerSignedBy,
            dealerSignedAt = item.DealerSignedAt?.ToString("yyyy-MM-dd HH:mm:ss")
        };
    }

    public async Task<object?> Approve2HQAsync(string contractNo, Approve2HQDealerContractDto? dto)
    {
        var code = contractNo.Trim().ToUpperInvariant();
        var item = await db.DealerContracts.FirstOrDefaultAsync(x => x.OrgId == Org && x.ContractNo == code);
        if (item is null) return null;

        if (item.DealerSignStatus != DealerContractSignStatus.Signed)
            throw new InvalidOperationException("Đại lý phải ký số (DealerSignStatus = Signed) trước khi NPP duyệt cấp 2 và ký hoàn tất.");

        if (item.Status == DealerContractStatus.Cancelled || item.Status == DealerContractStatus.Adjusted)
            throw new InvalidOperationException($"Không thể duyệt cấp 2 cho hợp đồng đã ở trạng thái {item.Status}.");

        item.Status = DealerContractStatus.Signed;
        item.HQSignStatus = DealerContractSignStatus.Signed;
        item.HQSignedAt = DateTime.Now;
        item.HQSignedBy = string.IsNullOrWhiteSpace(dto?.SignedBy) ? "HQ_AUTHORIZED_SIGNER" : dto.SignedBy.Trim();

        if (!string.IsNullOrWhiteSpace(dto?.FilePath))
            item.SignedFilePath = dto.FilePath.Trim();

        if (!string.IsNullOrWhiteSpace(dto?.Note))
            item.Remark = string.IsNullOrWhiteSpace(item.Remark) ? dto.Note.Trim() : $"{item.Remark}; HQ2: {dto.Note.Trim()}";

        // Nếu đây là hợp đồng điều chỉnh, cập nhật hợp đồng gốc sang trạng thái Adjusted
        if (item.ContractType == DealerContractType.Adjust && !string.IsNullOrWhiteSpace(item.ParentContractNo))
        {
            var parent = await db.DealerContracts.FirstOrDefaultAsync(x => x.OrgId == Org && x.ContractNo == item.ParentContractNo);
            if (parent != null)
            {
                parent.Status = DealerContractStatus.Adjusted;
                parent.Remark = string.IsNullOrWhiteSpace(parent.Remark)
                    ? $"[Đã điều chỉnh theo HĐ {item.ContractNo}]"
                    : $"{parent.Remark} [Đã điều chỉnh theo HĐ {item.ContractNo}]";
            }
        }

        await db.SaveChangesAsync();

        return new
        {
            message = "NPP phê duyệt cấp 2 và ký số hoàn tất hợp đồng thành công. Hợp đồng chính thức có hiệu lực.",
            contractNo = item.ContractNo,
            status = item.Status.ToString(),
            hqSignStatus = item.HQSignStatus.ToString(),
            hqSignedBy = item.HQSignedBy,
            hqSignedAt = item.HQSignedAt?.ToString("yyyy-MM-dd HH:mm:ss"),
            signedFilePath = item.SignedFilePath
        };
    }

    public async Task<object?> RejectHQAsync(string contractNo, RejectDealerContractDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Reason))
            throw new InvalidOperationException("Lý do từ chối (Reason) là bắt buộc.");

        var code = contractNo.Trim().ToUpperInvariant();
        var item = await db.DealerContracts.FirstOrDefaultAsync(x => x.OrgId == Org && x.ContractNo == code);
        if (item is null) return null;

        if (item.Status == DealerContractStatus.Signed)
            throw new InvalidOperationException("Không thể từ chối hợp đồng đã ký kết hoàn tất và có hiệu lực.");

        item.Status = DealerContractStatus.Draft;
        item.DealerSignStatus = DealerContractSignStatus.Pending;
        item.HQSignStatus = DealerContractSignStatus.Pending;
        item.RejectReason = dto.Reason.Trim();
        item.Remark = string.IsNullOrWhiteSpace(item.Remark) ? $"Từ chối: {dto.Reason.Trim()}" : $"{item.Remark}; Từ chối: {dto.Reason.Trim()}";

        await db.SaveChangesAsync();

        return new
        {
            message = "NPP đã từ chối duyệt hợp đồng và trả về cho đại lý chỉnh sửa.",
            contractNo = item.ContractNo,
            status = item.Status.ToString(),
            rejectReason = item.RejectReason
        };
    }

    public async Task<object?> CancelDLAsync(string contractNo, CancelDealerContractDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Reason))
            throw new InvalidOperationException("Lý do hủy hợp đồng (Reason) là bắt buộc.");

        var code = contractNo.Trim().ToUpperInvariant();
        var item = await db.DealerContracts.FirstOrDefaultAsync(x => x.OrgId == Org && x.ContractNo == code);
        if (item is null) return null;

        if (item.Status == DealerContractStatus.Signed)
            throw new InvalidOperationException("Hợp đồng đã ký kết hiệu lực (Signed) không thể hủy một chiều; phải lập Biên bản hủy HĐ hoặc HĐ điều chỉnh.");

        item.Status = DealerContractStatus.Cancelled;
        item.CancelReason = dto.Reason.Trim();
        item.CancelledAt = DateTime.Now;

        await db.SaveChangesAsync();

        return new
        {
            message = "Đã hủy hợp đồng bán buôn thành công.",
            contractNo = item.ContractNo,
            status = item.Status.ToString(),
            cancelReason = item.CancelReason,
            cancelledAt = item.CancelledAt?.ToString("yyyy-MM-dd HH:mm:ss")
        };
    }

    public async Task<object?> DeleteDraftAsync(string contractNo)
    {
        var code = contractNo.Trim().ToUpperInvariant();
        var item = await db.DealerContracts
            .Include(x => x.Details)
            .FirstOrDefaultAsync(x => x.OrgId == Org && x.ContractNo == code);
        if (item is null) return null;

        if (item.Status != DealerContractStatus.Draft)
            throw new InvalidOperationException($"Chỉ có thể xóa hợp đồng nháp (Draft). Trạng thái hiện tại: {item.Status}.");

        db.DealerContracts.Remove(item);
        await db.SaveChangesAsync();

        return new
        {
            message = "Đã xóa thành công hợp đồng nháp.",
            contractNo = code
        };
    }

    public async Task<object?> AddCarAsync(string contractNo, AddDealerContractCarDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Vin) || string.IsNullOrWhiteSpace(dto.Model))
            throw new InvalidOperationException("VIN và Model xe là bắt buộc.");

        var code = contractNo.Trim().ToUpperInvariant();
        var item = await db.DealerContracts
            .Include(x => x.Details)
            .FirstOrDefaultAsync(x => x.OrgId == Org && x.ContractNo == code);
        if (item is null) return null;

        if (item.Status != DealerContractStatus.Draft)
            throw new InvalidOperationException($"Chỉ được thêm xe khi hợp đồng ở trạng thái Draft. Trạng thái hiện tại: {item.Status}.");

        var vin = dto.Vin.Trim().ToUpperInvariant();
        if (item.Details.Any(d => d.Vin == vin))
            throw new InvalidOperationException($"Số VIN '{vin}' đã có trong hợp đồng này.");

        var rate = dto.VatRate ?? 10m;
        var vatAmount = Math.Round(dto.UnitPrice * rate / 100m, 2);
        var total = dto.UnitPrice + vatAmount;

        var detail = new DealerContractDetail
        {
            ContractId = item.Id,
            CarId = string.IsNullOrWhiteSpace(dto.CarId) ? "CAR-" + vin : dto.CarId.Trim().ToUpperInvariant(),
            Vin = vin,
            Model = dto.Model.Trim(),
            SpecCode = dto.SpecCode?.Trim(),
            ColorCode = dto.ColorCode?.Trim(),
            ProductionYear = dto.ProductionYear ?? DateTime.Today.Year,
            UnitPrice = dto.UnitPrice,
            VatRate = rate,
            TotalAmount = total,
            OrderNo = dto.OrderNo?.Trim(),
            Remark = dto.Remark?.Trim()
        };

        item.Details.Add(detail);
        item.TotalCars = item.Details.Count;
        item.TotalAmount = item.Details.Sum(x => x.TotalAmount);

        await db.SaveChangesAsync();

        return new
        {
            message = "Thêm dòng xe vào hợp đồng thành công.",
            contractNo = item.ContractNo,
            detailId = detail.Id,
            carId = detail.CarId,
            vin = detail.Vin,
            totalCars = item.TotalCars,
            totalAmount = item.TotalAmount
        };
    }

    public async Task<object?> RemoveCarAsync(string contractNo, long detailId)
    {
        var code = contractNo.Trim().ToUpperInvariant();
        var item = await db.DealerContracts
            .Include(x => x.Details)
            .FirstOrDefaultAsync(x => x.OrgId == Org && x.ContractNo == code);
        if (item is null) return null;

        if (item.Status != DealerContractStatus.Draft)
            throw new InvalidOperationException($"Chỉ được gỡ xe khi hợp đồng ở trạng thái Draft. Trạng thái hiện tại: {item.Status}.");

        var detail = item.Details.FirstOrDefault(d => d.Id == detailId);
        if (detail is null) return null;

        if (item.Details.Count <= 1)
            throw new InvalidOperationException("Hợp đồng phải có ít nhất 01 dòng xe; không thể gỡ dòng cuối cùng (hãy dùng xóa hợp đồng nháp).");

        item.Details.Remove(detail);
        item.TotalCars = item.Details.Count;
        item.TotalAmount = item.Details.Sum(x => x.TotalAmount);

        await db.SaveChangesAsync();

        return new
        {
            message = "Gỡ dòng xe khỏi hợp đồng thành công.",
            contractNo = item.ContractNo,
            detailId,
            totalCars = item.TotalCars,
            totalAmount = item.TotalAmount
        };
    }

    public async Task<object> StatsAsync()
    {
        var q = db.DealerContracts.AsNoTracking().Where(x => x.OrgId == Org);

        var totalContracts = await q.CountAsync();
        var draftCount = await q.CountAsync(x => x.Status == DealerContractStatus.Draft);
        var preliminaryCount = await q.CountAsync(x => x.Status == DealerContractStatus.PreliminaryApproved);
        var signedCount = await q.CountAsync(x => x.Status == DealerContractStatus.Signed);
        var adjustedCount = await q.CountAsync(x => x.Status == DealerContractStatus.Adjusted);
        var cancelledCount = await q.CountAsync(x => x.Status == DealerContractStatus.Cancelled);

        var totalCars = await q.SumAsync(x => (int?)x.TotalCars) ?? 0;
        var totalAmount = await q.SumAsync(x => (decimal?)x.TotalAmount) ?? 0m;
        var signedAmount = await q.Where(x => x.Status == DealerContractStatus.Signed).SumAsync(x => (decimal?)x.TotalAmount) ?? 0m;

        var cashContracts = await q.CountAsync(x => x.PaymentType == DealerContractPaymentType.Cash);
        var guaranteeContracts = await q.CountAsync(x => x.PaymentType == DealerContractPaymentType.Guarantee);
        var lcContracts = await q.CountAsync(x => x.PaymentType == DealerContractPaymentType.LC || x.PaymentType == DealerContractPaymentType.UpasLC);

        return new
        {
            totalContracts,
            draftCount,
            preliminaryCount,
            signedCount,
            adjustedCount,
            cancelledCount,
            totalCars,
            totalAmount,
            signedAmount,
            byPaymentType = new
            {
                cash = cashContracts,
                guarantee = guaranteeContracts,
                letterOfCredit = lcContracts
            }
        };
    }

    private static DealerContractPaymentType ParsePaymentType(string? str)
    {
        if (string.IsNullOrWhiteSpace(str)) return DealerContractPaymentType.Cash;
        var s = str.Trim().ToUpperInvariant();
        return s switch
        {
            "BL" or "GUARANTEE" => DealerContractPaymentType.Guarantee,
            "LC" => DealerContractPaymentType.LC,
            "UPAS" or "UPASLC" or "UPAS_LC" => DealerContractPaymentType.UpasLC,
            _ => DealerContractPaymentType.Cash
        };
    }

    private async Task<string> GenerateContractNoAsync(bool isAdjust)
    {
        var prefix = DateTime.Today.ToString("yyMM") + (isAdjust ? "ADJ" : "DRC");
        var latest = await db.DealerContracts
            .Where(x => x.OrgId == Org && x.ContractNo.StartsWith(prefix))
            .OrderByDescending(x => x.ContractNo)
            .Select(x => x.ContractNo)
            .FirstOrDefaultAsync();

        var seq = 1;
        if (!string.IsNullOrEmpty(latest) && latest.Length >= prefix.Length + 4)
        {
            var suffix = latest.Substring(prefix.Length);
            if (int.TryParse(suffix, out var num))
                seq = num + 1;
        }

        return $"{prefix}{seq:D5}";
    }
}
