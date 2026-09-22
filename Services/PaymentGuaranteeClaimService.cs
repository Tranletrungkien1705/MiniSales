using Microsoft.EntityFrameworkCore;
using MiniSales.Data;
using MiniSales.Models;

namespace MiniSales.Services;

public record CreatePaymentGuaranteeClaimDetailDto(
    string? CarId,
    string Vin,
    string Model,
    string? ModelCode,
    string? SpecCode,
    string? SpecDescription,
    string? ColorCode,
    string? ColorName,
    string? ContractNo,
    bool? FlagDealerContractDMS40,
    string? GuaranteeNo,
    string? BankGuaranteeNo,
    string? BankCode,
    string? BankName,
    string? BankCodeMonitor,
    DateTime? DateOpen,
    DateTime? DateStart,
    DateTime? DateEnd,
    decimal? UnitPriceActual,
    decimal? GuaranteeValue,
    decimal ClaimAmount,
    string? Remark
);

public record CreatePaymentGuaranteeClaimDto(
    string? GrtClaimNo,
    string DealerCode,
    string? DealerName,
    string? GrtClaimTypeCode, // "DN" = Đề nghị thực hiện BL, "YC" = YC thực hiện BL thanh toán (mặc định)
    string? FlagisHTC, // "1" = HTC, "2" = HTCLD
    string? BankCode,
    string? BankName,
    string? BankCodeMonitor,
    string? BankAccountNo,
    string? Remark,
    string? CreatedBy,
    List<CreatePaymentGuaranteeClaimDetailDto>? Details
);

public record UpdatePaymentGuaranteeClaimDto(
    string? GrtClaimTypeCode,
    string? FlagisHTC,
    string? BankCode,
    string? BankName,
    string? BankCodeMonitor,
    string? BankAccountNo,
    string? Remark,
    string? UpdatedBy
);

public record SignPaymentGuaranteeClaimDto(
    string? SignBy,
    string? FileName,
    string? FileSigned,
    string? Remark
);

public record RejectPaymentGuaranteeClaimDto(
    string Reason,
    string? RejectBy
);

public record CancelPaymentGuaranteeClaimDto(
    string Reason,
    string? CancelBy
);

public record AddCarToGuaranteeClaimDto(
    string? CarId,
    string Vin,
    string Model,
    string? ModelCode,
    string? SpecCode,
    string? SpecDescription,
    string? ColorCode,
    string? ColorName,
    string? ContractNo,
    bool? FlagDealerContractDMS40,
    string? GuaranteeNo,
    string? BankGuaranteeNo,
    string? BankCode,
    string? BankName,
    string? BankCodeMonitor,
    DateTime? DateOpen,
    DateTime? DateStart,
    DateTime? DateEnd,
    decimal? UnitPriceActual,
    decimal? GuaranteeValue,
    decimal ClaimAmount,
    string? Remark
);

public interface IPaymentGuaranteeClaimService
{
    Task<object> CreateAsync(CreatePaymentGuaranteeClaimDto dto);
    Task<object> ListAsync(string? status, string? dealer, string? bankCode, string? bankCodeMonitor, string? claimType, string? flagisHTC, string? vin, string? claimNo, string? contractNo);
    Task<object?> DetailAsync(string claimNo);
    Task<object?> UpdateAsync(string claimNo, UpdatePaymentGuaranteeClaimDto dto);
    Task<object?> SignAndApproveAsync(string claimNo, SignPaymentGuaranteeClaimDto? dto);
    Task<object?> RejectAsync(string claimNo, RejectPaymentGuaranteeClaimDto dto);
    Task<object?> CancelAsync(string claimNo, CancelPaymentGuaranteeClaimDto dto);
    Task<object?> DeleteDraftAsync(string claimNo);
    Task<object?> AddCarAsync(string claimNo, AddCarToGuaranteeClaimDto dto);
    Task<object?> RemoveCarAsync(string claimNo, string vin);
    Task<object> EligibleCarsAsync(string? dealerCode, string? bankCode, string? bankCodeMonitor);
    Task<object> StatsAsync();
}

public sealed class PaymentGuaranteeClaimService(AppDbContext db, ITenantContext tenant) : IPaymentGuaranteeClaimService
{
    private Guid Org => tenant.OrgId;

    public async Task<object> CreateAsync(CreatePaymentGuaranteeClaimDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.DealerCode))
            throw new InvalidOperationException("DealerCode (mã đại lý) không được để trống.");

        var dealerCode = dto.DealerCode.Trim().ToUpperInvariant();
        var claimType = string.IsNullOrWhiteSpace(dto.GrtClaimTypeCode) ? "YC" : dto.GrtClaimTypeCode.Trim().ToUpperInvariant();
        if (claimType != "DN" && claimType != "YC")
            throw new InvalidOperationException("GrtClaimTypeCode không hợp lệ (DN: Đề nghị thực hiện bảo lãnh, YC: Yêu cầu thực hiện bảo lãnh thanh toán).");

        var flagisHTC = string.IsNullOrWhiteSpace(dto.FlagisHTC) ? "1" : dto.FlagisHTC.Trim();
        if (flagisHTC != "1" && flagisHTC != "2")
            throw new InvalidOperationException("FlagisHTC (pháp nhân) không hợp lệ (1: HTC, 2: HTCLD).");

        var datePrefix = DateTime.Today.ToString("yyMMdd");
        string claimNo;
        if (!string.IsNullOrWhiteSpace(dto.GrtClaimNo))
        {
            claimNo = dto.GrtClaimNo.Trim().ToUpperInvariant();
        }
        else
        {
            // Tự sinh số hiệu công văn chuẩn DMS 4.0: CV-{yyMMdd}-{seq:D3}/{DealerCode}
            var pattern = $"CV-{datePrefix}-%/{dealerCode}";
            var existingCount = await db.GuaranteeClaims
                .Where(x => x.OrgId == Org && EF.Functions.Like(x.GrtClaimNo, pattern))
                .CountAsync();
            claimNo = $"CV-{datePrefix}-{(existingCount + 1):D3}/{dealerCode}";
        }

        if (await db.GuaranteeClaims.AnyAsync(x => x.OrgId == Org && x.GrtClaimNo == claimNo))
            throw new InvalidOperationException($"Công văn số {claimNo} đã tồn tại trong hệ thống.");

        var details = new List<PaymentGuaranteeClaimDetail>();
        decimal sumClaimAmount = 0m;
        decimal sumGuaranteeValue = 0m;

        if (dto.Details != null && dto.Details.Count > 0)
        {
            var seenVins = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var d in dto.Details)
            {
                if (string.IsNullOrWhiteSpace(d.Vin))
                    throw new InvalidOperationException("Số khung xe (VIN) trong danh sách không được để trống.");

                var vin = d.Vin.Trim().ToUpperInvariant();
                if (!seenVins.Add(vin))
                    throw new InvalidOperationException($"Số khung xe VIN {vin} bị trùng lặp trong công văn.");

                if (d.ClaimAmount <= 0)
                    throw new InvalidOperationException($"Số tiền yêu cầu đòi bảo lãnh cho xe VIN {vin} phải lớn hơn 0.");

                var carId = string.IsNullOrWhiteSpace(d.CarId) ? $"CAR-{vin[..Math.Min(vin.Length, 10)]}" : d.CarId.Trim();
                var grtVal = d.GuaranteeValue ?? d.ClaimAmount;

                details.Add(new PaymentGuaranteeClaimDetail
                {
                    GrtClaimNo = claimNo,
                    CarId = carId,
                    Vin = vin,
                    Model = d.Model.Trim(),
                    ModelCode = d.ModelCode?.Trim(),
                    SpecCode = d.SpecCode?.Trim(),
                    SpecDescription = d.SpecDescription?.Trim(),
                    ColorCode = d.ColorCode?.Trim(),
                    ColorName = d.ColorName?.Trim(),
                    ContractNo = d.ContractNo?.Trim(),
                    FlagDealerContractDMS40 = d.FlagDealerContractDMS40 ?? true,
                    GuaranteeNo = d.GuaranteeNo?.Trim(),
                    BankGuaranteeNo = d.BankGuaranteeNo?.Trim(),
                    BankCode = d.BankCode?.Trim() ?? dto.BankCode?.Trim(),
                    BankName = d.BankName?.Trim() ?? dto.BankName?.Trim(),
                    BankCodeMonitor = d.BankCodeMonitor?.Trim() ?? dto.BankCodeMonitor?.Trim(),
                    DateOpen = d.DateOpen,
                    DateStart = d.DateStart,
                    DateEnd = d.DateEnd,
                    UnitPriceActual = d.UnitPriceActual ?? grtVal,
                    GuaranteeValue = grtVal,
                    ClaimAmount = d.ClaimAmount,
                    Status = ClaimVinSignStatus.Pending,
                    Remark = d.Remark?.Trim()
                });

                sumClaimAmount += d.ClaimAmount;
                sumGuaranteeValue += grtVal;
            }
        }

        var headerBankCode = dto.BankCode?.Trim() ?? details.FirstOrDefault()?.BankCode;
        var headerBankName = dto.BankName?.Trim() ?? details.FirstOrDefault()?.BankName;
        var headerBankCodeMonitor = dto.BankCodeMonitor?.Trim() ?? details.FirstOrDefault()?.BankCodeMonitor;

        var claim = new PaymentGuaranteeClaim
        {
            OrgId = Org,
            GrtClaimNo = claimNo,
            DealerCode = dealerCode,
            DealerName = dto.DealerName?.Trim() ?? (dealerCode == "VN001" ? "Hyundai Đông Đô" : dealerCode == "VN002" ? "Hyundai Nam Trung" : $"Đại lý {dealerCode}"),
            GrtClaimTypeCode = claimType,
            FlagisHTC = flagisHTC,
            BankCode = headerBankCode,
            BankName = headerBankName,
            BankCodeMonitor = headerBankCodeMonitor,
            BankAccountNo = dto.BankAccountNo?.Trim(),
            TotalCars = details.Count,
            TotalClaimAmount = sumClaimAmount,
            TotalGuaranteeValue = sumGuaranteeValue,
            Status = GuaranteeClaimStatus.Pending,
            VinSignStatus = ClaimVinSignStatus.Pending,
            Remark = dto.Remark?.Trim(),
            CreatedBy = dto.CreatedBy?.Trim() ?? "DEALER_SALES_ADMIN",
            CreatedDate = DateTime.Today,
            CreatedDateTime = DateTime.Now,
            Details = details
        };

        db.GuaranteeClaims.Add(claim);
        await db.SaveChangesAsync();

        return new
        {
            id = claim.Id,
            grtClaimNo = claim.GrtClaimNo,
            dealerCode = claim.DealerCode,
            dealerName = claim.DealerName,
            claimTypeCode = claim.GrtClaimTypeCode,
            claimTypeName = claim.GrtClaimTypeCode == "DN" ? "Đề nghị thực hiện bảo lãnh" : "Yêu cầu thực hiện bảo lãnh thanh toán",
            flagisHTC = claim.FlagisHTC,
            flagisHTCName = claim.FlagisHTC == "1" ? "HTC (Hyundai Thành Công VN)" : "HTCLD (Liên Doanh)",
            bankCode = claim.BankCode,
            bankCodeMonitor = claim.BankCodeMonitor,
            totalCars = claim.TotalCars,
            totalClaimAmount = claim.TotalClaimAmount,
            status = claim.Status.ToString(),
            message = "Lập công văn yêu cầu / đòi tiền thực hiện bảo lãnh ngân hàng thành công."
        };
    }

    public async Task<object> ListAsync(string? status, string? dealer, string? bankCode, string? bankCodeMonitor, string? claimType, string? flagisHTC, string? vin, string? claimNo, string? contractNo)
    {
        var q = db.GuaranteeClaims
            .Include(e => e.Details)
            .Where(x => x.OrgId == Org);

        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<GuaranteeClaimStatus>(status, true, out var stEnum))
            q = q.Where(x => x.Status == stEnum);

        if (!string.IsNullOrWhiteSpace(dealer))
        {
            var d = dealer.Trim().ToUpperInvariant();
            q = q.Where(x => x.DealerCode == d || x.DealerName.Contains(d));
        }

        if (!string.IsNullOrWhiteSpace(bankCode))
        {
            var bc = bankCode.Trim().ToUpperInvariant();
            q = q.Where(x => x.BankCode == bc || x.Details.Any(dt => dt.BankCode == bc));
        }

        if (!string.IsNullOrWhiteSpace(bankCodeMonitor))
        {
            var bcm = bankCodeMonitor.Trim().ToUpperInvariant();
            q = q.Where(x => x.BankCodeMonitor == bcm || x.Details.Any(dt => dt.BankCodeMonitor == bcm));
        }

        if (!string.IsNullOrWhiteSpace(claimType))
        {
            var ct = claimType.Trim().ToUpperInvariant();
            q = q.Where(x => x.GrtClaimTypeCode == ct);
        }

        if (!string.IsNullOrWhiteSpace(flagisHTC))
        {
            var f = flagisHTC.Trim();
            q = q.Where(x => x.FlagisHTC == f);
        }

        if (!string.IsNullOrWhiteSpace(claimNo))
        {
            var no = claimNo.Trim().ToUpperInvariant();
            q = q.Where(x => x.GrtClaimNo.Contains(no));
        }

        if (!string.IsNullOrWhiteSpace(vin))
        {
            var v = vin.Trim().ToUpperInvariant();
            q = q.Where(x => x.Details.Any(dt => dt.Vin.Contains(v)));
        }

        if (!string.IsNullOrWhiteSpace(contractNo))
        {
            var c = contractNo.Trim().ToUpperInvariant();
            q = q.Where(x => x.Details.Any(dt => dt.ContractNo != null && dt.ContractNo.Contains(c)));
        }

        var list = await q.OrderByDescending(x => x.CreatedDateTime).ToListAsync();

        return list.Select(x => new
        {
            x.Id,
            x.GrtClaimNo,
            x.DealerCode,
            x.DealerName,
            x.GrtClaimTypeCode,
            GrtClaimTypeName = x.GrtClaimTypeCode == "DN" ? "Đề nghị thực hiện bảo lãnh" : "Yêu cầu thực hiện bảo lãnh thanh toán",
            x.FlagisHTC,
            FlagisHTCName = x.FlagisHTC == "1" ? "HTC" : "HTCLD",
            x.BankCode,
            x.BankName,
            x.BankCodeMonitor,
            x.BankAccountNo,
            x.TotalCars,
            x.TotalClaimAmount,
            x.TotalGuaranteeValue,
            Status = x.Status.ToString(),
            VinSignStatus = x.VinSignStatus.ToString(),
            x.SignDate,
            x.SignBy,
            x.FileSigned,
            x.FileName,
            x.Remark,
            x.RejectReason,
            x.CancelReason,
            x.CreatedBy,
            x.CreatedDate,
            x.CreatedDateTime,
            Cars = x.Details.Select(d => new
            {
                d.Id,
                d.CarId,
                d.Vin,
                d.Model,
                d.ModelCode,
                d.SpecCode,
                d.ColorCode,
                d.ColorName,
                d.ContractNo,
                d.BankGuaranteeNo,
                d.BankCode,
                d.BankName,
                d.BankCodeMonitor,
                d.DateStart,
                d.DateEnd,
                d.UnitPriceActual,
                d.GuaranteeValue,
                d.ClaimAmount,
                Status = d.Status.ToString(),
                d.Remark
            })
        });
    }

    public async Task<object?> DetailAsync(string claimNo)
    {
        if (string.IsNullOrWhiteSpace(claimNo)) return null;
        var no = claimNo.Trim().ToUpperInvariant();

        var x = await db.GuaranteeClaims
            .Include(e => e.Details)
            .FirstOrDefaultAsync(e => e.OrgId == Org && e.GrtClaimNo == no);

        if (x == null) return null;

        return new
        {
            x.Id,
            x.GrtClaimNo,
            x.DealerCode,
            x.DealerName,
            x.GrtClaimTypeCode,
            GrtClaimTypeName = x.GrtClaimTypeCode == "DN" ? "Đề nghị thực hiện bảo lãnh" : "Yêu cầu thực hiện bảo lãnh thanh toán",
            x.FlagisHTC,
            FlagisHTCName = x.FlagisHTC == "1" ? "HTC (Hyundai Thành Công VN)" : "HTCLD (Liên Doanh)",
            x.BankCode,
            x.BankName,
            x.BankCodeMonitor,
            x.BankAccountNo,
            x.TotalCars,
            x.TotalClaimAmount,
            x.TotalGuaranteeValue,
            Status = x.Status.ToString(),
            VinSignStatus = x.VinSignStatus.ToString(),
            x.SignDate,
            x.SignBy,
            x.FileSigned,
            x.FileName,
            x.Remark,
            x.RejectReason,
            x.RejectDateTime,
            x.RejectBy,
            x.CancelReason,
            x.CancelDateTime,
            x.CancelBy,
            x.CreatedBy,
            x.CreatedDate,
            x.CreatedDateTime,
            x.LUDateTime,
            x.LUBy,
            Details = x.Details.Select(d => new
            {
                d.Id,
                d.CarId,
                d.Vin,
                d.Model,
                d.ModelCode,
                d.SpecCode,
                d.SpecDescription,
                d.ColorCode,
                d.ColorName,
                d.ContractNo,
                d.FlagDealerContractDMS40,
                d.GuaranteeNo,
                d.BankGuaranteeNo,
                d.BankCode,
                d.BankName,
                d.BankCodeMonitor,
                d.DateOpen,
                d.DateStart,
                d.DateEnd,
                d.UnitPriceActual,
                d.GuaranteeValue,
                d.ClaimAmount,
                Status = d.Status.ToString(),
                d.Remark
            })
        };
    }

    public async Task<object?> UpdateAsync(string claimNo, UpdatePaymentGuaranteeClaimDto dto)
    {
        if (string.IsNullOrWhiteSpace(claimNo)) return null;
        var no = claimNo.Trim().ToUpperInvariant();

        var claim = await db.GuaranteeClaims
            .Include(e => e.Details)
            .FirstOrDefaultAsync(e => e.OrgId == Org && e.GrtClaimNo == no);

        if (claim == null) return null;

        if (claim.Status != GuaranteeClaimStatus.Pending)
            throw new InvalidOperationException($"Chỉ có thể chỉnh sửa công văn ở trạng thái Pending (hiện tại: {claim.Status}).");

        if (!string.IsNullOrWhiteSpace(dto.GrtClaimTypeCode))
        {
            var ct = dto.GrtClaimTypeCode.Trim().ToUpperInvariant();
            if (ct != "DN" && ct != "YC")
                throw new InvalidOperationException("GrtClaimTypeCode không hợp lệ (DN: Đề nghị, YC: Yêu cầu đòi tiền).");
            claim.GrtClaimTypeCode = ct;
        }

        if (!string.IsNullOrWhiteSpace(dto.FlagisHTC))
        {
            var f = dto.FlagisHTC.Trim();
            if (f != "1" && f != "2")
                throw new InvalidOperationException("FlagisHTC không hợp lệ (1: HTC, 2: HTCLD).");
            claim.FlagisHTC = f;
        }

        if (!string.IsNullOrWhiteSpace(dto.BankCode)) claim.BankCode = dto.BankCode.Trim();
        if (!string.IsNullOrWhiteSpace(dto.BankName)) claim.BankName = dto.BankName.Trim();
        if (!string.IsNullOrWhiteSpace(dto.BankCodeMonitor)) claim.BankCodeMonitor = dto.BankCodeMonitor.Trim();
        if (!string.IsNullOrWhiteSpace(dto.BankAccountNo)) claim.BankAccountNo = dto.BankAccountNo.Trim();
        if (dto.Remark != null) claim.Remark = dto.Remark.Trim();

        claim.LUDateTime = DateTime.Now;
        claim.LUBy = dto.UpdatedBy?.Trim() ?? "DEALER_SALES_ADMIN";

        await db.SaveChangesAsync();

        return new
        {
            message = "Cập nhật thông tin công văn yêu cầu / đòi tiền bảo lãnh thành công.",
            grtClaimNo = claim.GrtClaimNo,
            claimTypeCode = claim.GrtClaimTypeCode,
            flagisHTC = claim.FlagisHTC,
            totalCars = claim.TotalCars,
            totalClaimAmount = claim.TotalClaimAmount
        };
    }

    public async Task<object?> SignAndApproveAsync(string claimNo, SignPaymentGuaranteeClaimDto? dto)
    {
        if (string.IsNullOrWhiteSpace(claimNo)) return null;
        var no = claimNo.Trim().ToUpperInvariant();

        var claim = await db.GuaranteeClaims
            .Include(e => e.Details)
            .FirstOrDefaultAsync(e => e.OrgId == Org && e.GrtClaimNo == no);

        if (claim == null) return null;

        if (claim.Status != GuaranteeClaimStatus.Pending)
            throw new InvalidOperationException($"Không thể ký duyệt công văn đang ở trạng thái {claim.Status}. Chỉ công văn Pending mới được ký duyệt.");

        if (claim.Details.Count == 0)
            throw new InvalidOperationException("Công văn chưa có dòng xe nào, không thể ký duyệt phát hành.");

        var signUser = dto?.SignBy?.Trim() ?? "NPP_HQ_DIRECTOR (Lãnh đạo Phân phối HTC)";
        var fileName = dto?.FileName?.Trim() ?? $"{claim.GrtClaimNo.Replace('/', '_')}_signed.pdf";
        var filePath = dto?.FileSigned?.Trim() ?? $"/storage/grtclaim/{fileName}";

        claim.Status = GuaranteeClaimStatus.Approved;
        claim.VinSignStatus = ClaimVinSignStatus.Approved;
        claim.SignDate = DateTime.Now;
        claim.SignBy = signUser;
        claim.FileName = fileName;
        claim.FileSigned = filePath;
        if (!string.IsNullOrWhiteSpace(dto?.Remark))
            claim.Remark = $"{claim.Remark} | Ký duyệt: {dto.Remark.Trim()}";

        claim.LUDateTime = DateTime.Now;
        claim.LUBy = signUser;

        foreach (var d in claim.Details)
        {
            d.Status = ClaimVinSignStatus.Approved;
        }

        await db.SaveChangesAsync();

        return new
        {
            message = "Thẩm định và Ký số điện tử E-Sign công văn yêu cầu / đòi tiền bảo lãnh ngân hàng thành công.",
            grtClaimNo = claim.GrtClaimNo,
            status = claim.Status.ToString(),
            vinSignStatus = claim.VinSignStatus.ToString(),
            signDate = claim.SignDate,
            signBy = claim.SignBy,
            fileName = claim.FileName,
            fileSigned = claim.FileSigned,
            totalCars = claim.TotalCars,
            totalClaimAmount = claim.TotalClaimAmount,
            notification = $"Đã phát hành công văn tới ngân hàng giám sát {claim.BankCodeMonitor} và ngân hàng phát hành bảo lãnh {claim.BankCode}."
        };
    }

    public async Task<object?> RejectAsync(string claimNo, RejectPaymentGuaranteeClaimDto dto)
    {
        if (string.IsNullOrWhiteSpace(claimNo)) return null;
        var no = claimNo.Trim().ToUpperInvariant();

        var claim = await db.GuaranteeClaims
            .FirstOrDefaultAsync(e => e.OrgId == Org && e.GrtClaimNo == no);

        if (claim == null) return null;

        if (claim.Status != GuaranteeClaimStatus.Pending)
            throw new InvalidOperationException($"Không thể từ chối công văn đang ở trạng thái {claim.Status}. Chỉ từ chối được công văn Pending.");

        if (string.IsNullOrWhiteSpace(dto.Reason))
            throw new InvalidOperationException("Bắt buộc nhập lý do từ chối công văn.");

        var rejectUser = dto.RejectBy?.Trim() ?? "NPP_LEGAL_EXPERT";

        claim.Status = GuaranteeClaimStatus.Rejected;
        claim.VinSignStatus = ClaimVinSignStatus.Cancelled;
        claim.RejectReason = dto.Reason.Trim();
        claim.RejectDateTime = DateTime.Now;
        claim.RejectBy = rejectUser;
        claim.LUDateTime = DateTime.Now;
        claim.LUBy = rejectUser;

        await db.SaveChangesAsync();

        return new
        {
            message = "Từ chối duyệt công văn yêu cầu / đòi tiền bảo lãnh thành công.",
            grtClaimNo = claim.GrtClaimNo,
            status = claim.Status.ToString(),
            rejectReason = claim.RejectReason,
            rejectBy = claim.RejectBy,
            rejectDateTime = claim.RejectDateTime
        };
    }

    public async Task<object?> CancelAsync(string claimNo, CancelPaymentGuaranteeClaimDto dto)
    {
        if (string.IsNullOrWhiteSpace(claimNo)) return null;
        var no = claimNo.Trim().ToUpperInvariant();

        var claim = await db.GuaranteeClaims
            .Include(e => e.Details)
            .FirstOrDefaultAsync(e => e.OrgId == Org && e.GrtClaimNo == no);

        if (claim == null) return null;

        if (claim.Status == GuaranteeClaimStatus.Cancelled)
            throw new InvalidOperationException("Công văn này đã bị hủy trước đó.");

        if (string.IsNullOrWhiteSpace(dto.Reason))
            throw new InvalidOperationException("Bắt buộc nhập lý do hủy công văn.");

        var cancelUser = dto.CancelBy?.Trim() ?? "DEALER_SALES_ADMIN";

        claim.Status = GuaranteeClaimStatus.Cancelled;
        claim.VinSignStatus = ClaimVinSignStatus.Cancelled;
        claim.CancelReason = dto.Reason.Trim();
        claim.CancelDateTime = DateTime.Now;
        claim.CancelBy = cancelUser;
        claim.LUDateTime = DateTime.Now;
        claim.LUBy = cancelUser;

        foreach (var d in claim.Details)
        {
            d.Status = ClaimVinSignStatus.Cancelled;
        }

        await db.SaveChangesAsync();

        return new
        {
            message = "Hủy công văn yêu cầu / đòi tiền bảo lãnh thành công.",
            grtClaimNo = claim.GrtClaimNo,
            status = claim.Status.ToString(),
            cancelReason = claim.CancelReason,
            cancelDateTime = claim.CancelDateTime,
            cancelBy = claim.CancelBy
        };
    }

    public async Task<object?> DeleteDraftAsync(string claimNo)
    {
        if (string.IsNullOrWhiteSpace(claimNo)) return null;
        var no = claimNo.Trim().ToUpperInvariant();

        var claim = await db.GuaranteeClaims
            .Include(e => e.Details)
            .FirstOrDefaultAsync(e => e.OrgId == Org && e.GrtClaimNo == no);

        if (claim == null) return null;

        if (claim.Status != GuaranteeClaimStatus.Pending)
            throw new InvalidOperationException($"Chỉ có thể xóa công văn nháp ở trạng thái Pending. Công văn {claim.GrtClaimNo} đang ở trạng thái {claim.Status}.");

        db.GuaranteeClaims.Remove(claim);
        await db.SaveChangesAsync();

        return new
        {
            message = $"Đã xóa thành công công văn nháp {no} và toàn bộ dòng xe chi tiết.",
            grtClaimNo = no
        };
    }

    public async Task<object?> AddCarAsync(string claimNo, AddCarToGuaranteeClaimDto dto)
    {
        if (string.IsNullOrWhiteSpace(claimNo)) return null;
        var no = claimNo.Trim().ToUpperInvariant();

        var claim = await db.GuaranteeClaims
            .Include(e => e.Details)
            .FirstOrDefaultAsync(e => e.OrgId == Org && e.GrtClaimNo == no);

        if (claim == null) return null;

        if (claim.Status != GuaranteeClaimStatus.Pending)
            throw new InvalidOperationException($"Không thể thêm xe vào công văn đang ở trạng thái {claim.Status}. Chỉ thao tác khi Pending.");

        if (string.IsNullOrWhiteSpace(dto.Vin))
            throw new InvalidOperationException("Số khung xe (VIN) không được để trống.");

        var vin = dto.Vin.Trim().ToUpperInvariant();
        if (claim.Details.Any(d => d.Vin.Equals(vin, StringComparison.OrdinalIgnoreCase)))
            throw new InvalidOperationException($"Xe có số khung VIN {vin} đã tồn tại trong công văn này.");

        if (dto.ClaimAmount <= 0)
            throw new InvalidOperationException("Số tiền yêu cầu đòi bảo lãnh cho xe phải lớn hơn 0.");

        var carId = string.IsNullOrWhiteSpace(dto.CarId) ? $"CAR-{vin[..Math.Min(vin.Length, 10)]}" : dto.CarId.Trim();
        var grtVal = dto.GuaranteeValue ?? dto.ClaimAmount;

        var detail = new PaymentGuaranteeClaimDetail
        {
            GrtClaimNo = claim.GrtClaimNo,
            CarId = carId,
            Vin = vin,
            Model = dto.Model.Trim(),
            ModelCode = dto.ModelCode?.Trim(),
            SpecCode = dto.SpecCode?.Trim(),
            SpecDescription = dto.SpecDescription?.Trim(),
            ColorCode = dto.ColorCode?.Trim(),
            ColorName = dto.ColorName?.Trim(),
            ContractNo = dto.ContractNo?.Trim(),
            FlagDealerContractDMS40 = dto.FlagDealerContractDMS40 ?? true,
            GuaranteeNo = dto.GuaranteeNo?.Trim() ?? claim.Details.FirstOrDefault()?.GuaranteeNo,
            BankGuaranteeNo = dto.BankGuaranteeNo?.Trim() ?? claim.Details.FirstOrDefault()?.BankGuaranteeNo,
            BankCode = dto.BankCode?.Trim() ?? claim.BankCode,
            BankName = dto.BankName?.Trim() ?? claim.BankName,
            BankCodeMonitor = dto.BankCodeMonitor?.Trim() ?? claim.BankCodeMonitor,
            DateOpen = dto.DateOpen,
            DateStart = dto.DateStart,
            DateEnd = dto.DateEnd,
            UnitPriceActual = dto.UnitPriceActual ?? grtVal,
            GuaranteeValue = grtVal,
            ClaimAmount = dto.ClaimAmount,
            Status = ClaimVinSignStatus.Pending,
            Remark = dto.Remark?.Trim()
        };

        claim.Details.Add(detail);
        claim.TotalCars = claim.Details.Count;
        claim.TotalClaimAmount = claim.Details.Sum(d => d.ClaimAmount);
        claim.TotalGuaranteeValue = claim.Details.Sum(d => d.GuaranteeValue);
        claim.LUDateTime = DateTime.Now;
        claim.LUBy = "DEALER_SALES_ADMIN";

        await db.SaveChangesAsync();

        return new
        {
            message = $"Thêm xe VIN {vin} vào công văn {claim.GrtClaimNo} thành công.",
            grtClaimNo = claim.GrtClaimNo,
            vin = detail.Vin,
            model = detail.Model,
            claimAmount = detail.ClaimAmount,
            totalCars = claim.TotalCars,
            totalClaimAmount = claim.TotalClaimAmount
        };
    }

    public async Task<object?> RemoveCarAsync(string claimNo, string vin)
    {
        if (string.IsNullOrWhiteSpace(claimNo) || string.IsNullOrWhiteSpace(vin)) return null;
        var no = claimNo.Trim().ToUpperInvariant();
        var v = vin.Trim().ToUpperInvariant();

        var claim = await db.GuaranteeClaims
            .Include(e => e.Details)
            .FirstOrDefaultAsync(e => e.OrgId == Org && e.GrtClaimNo == no);

        if (claim == null) return null;

        if (claim.Status != GuaranteeClaimStatus.Pending)
            throw new InvalidOperationException($"Không thể gỡ xe khỏi công văn đang ở trạng thái {claim.Status}. Chỉ thao tác khi Pending.");

        var dtl = claim.Details.FirstOrDefault(d => d.Vin.Equals(v, StringComparison.OrdinalIgnoreCase));
        if (dtl == null)
            throw new InvalidOperationException($"Không tìm thấy xe VIN {v} trong công văn {claim.GrtClaimNo}.");

        claim.Details.Remove(dtl);
        claim.TotalCars = claim.Details.Count;
        claim.TotalClaimAmount = claim.Details.Sum(d => d.ClaimAmount);
        claim.TotalGuaranteeValue = claim.Details.Sum(d => d.GuaranteeValue);
        claim.LUDateTime = DateTime.Now;
        claim.LUBy = "DEALER_SALES_ADMIN";

        await db.SaveChangesAsync();

        return new
        {
            message = $"Đã gỡ bỏ xe VIN {v} khỏi công văn {claim.GrtClaimNo}.",
            grtClaimNo = claim.GrtClaimNo,
            totalCars = claim.TotalCars,
            totalClaimAmount = claim.TotalClaimAmount
        };
    }

    public async Task<object> EligibleCarsAsync(string? dealerCode, string? bankCode, string? bankCodeMonitor)
    {
        // Tra cứu xe đủ điều kiện lập công văn đòi tiền bảo lãnh (DMS.Sales CarCarSearchWHForGrtClaim / Car_CarForGrtClaim):
        // 1. Xe thuộc các Thư bảo lãnh đã duyệt (PaymentGuarantees status = Approved)
        // 2. Lấy chi tiết xe trong bảo lãnh
        var qGrt = db.PaymentGuarantees
            .Include(g => g.Details)
            .Where(g => g.OrgId == Org && g.Status == GuaranteeStatus.Approved);

        if (!string.IsNullOrWhiteSpace(dealerCode))
        {
            var d = dealerCode.Trim().ToUpperInvariant();
            qGrt = qGrt.Where(g => g.DealerCode == d);
        }

        if (!string.IsNullOrWhiteSpace(bankCode))
        {
            var bc = bankCode.Trim().ToUpperInvariant();
            qGrt = qGrt.Where(g => g.BankCode == bc);
        }

        if (!string.IsNullOrWhiteSpace(bankCodeMonitor))
        {
            var bcm = bankCodeMonitor.Trim().ToUpperInvariant();
            qGrt = qGrt.Where(g => g.BankCodeMonitor == bcm);
        }

        var activeGrts = await qGrt.ToListAsync();

        // Danh sách các VIN đã nằm trong công văn đòi bảo lãnh đang chờ duyệt hoặc đã duyệt
        var claimedVins = await db.GuaranteeClaimDetails
            .Where(dt => dt.Status != ClaimVinSignStatus.Cancelled)
            .Select(dt => dt.Vin)
            .Distinct()
            .ToListAsync();

        var claimedVinSet = new HashSet<string>(claimedVins, StringComparer.OrdinalIgnoreCase);

        var eligibleCars = new List<object>();

        foreach (var grt in activeGrts)
        {
            foreach (var dt in grt.Details)
            {
                if (string.IsNullOrWhiteSpace(dt.Vin) || claimedVinSet.Contains(dt.Vin)) continue;

                var vin = dt.Vin.Trim().ToUpperInvariant();
                var carId = $"CAR-{vin[..Math.Min(vin.Length, 10)]}";

                eligibleCars.Add(new
                {
                    carId,
                    vin,
                    model = dt.Model,
                    modelCode = "TM",
                    specCode = "SPEC-STD",
                    specDescription = $"{dt.Model} Tiêu Chuẩn",
                    colorCode = "WH1",
                    colorName = "Trắng / Đen",
                    contractNo = "2603DRC00001",
                    flagDealerContractDMS40 = true,
                    guaranteeNo = grt.GuaranteeNo,
                    bankGuaranteeNo = grt.BankGuaranteeNo,
                    bankCode = grt.BankCode,
                    bankName = grt.BankCode == "VCB" ? "Vietcombank Thăng Long" : grt.BankCode == "BIDV" ? "BIDV Hà Nội" : "Techcombank",
                    bankCodeMonitor = grt.BankCodeMonitor ?? "VCB",
                    dealerCode = grt.DealerCode,
                    dateOpen = grt.DateOpen,
                    dateStart = dt.DateStart ?? grt.DateOpen,
                    dateEnd = dt.DateEnd ?? grt.DateExpired,
                    unitPriceActual = dt.GuaranteeValue,
                    guaranteeValue = dt.GuaranteeValue,
                    claimAmountSuggested = dt.GuaranteeValue,
                    flagisHTC = "1",
                    status = "ReadyForClaim"
                });
            }
        }

        return eligibleCars;
    }

    public async Task<object> StatsAsync()
    {
        var claims = await db.GuaranteeClaims
            .Include(e => e.Details)
            .Where(x => x.OrgId == Org)
            .ToListAsync();

        var total = claims.Count;
        var pending = claims.Count(x => x.Status == GuaranteeClaimStatus.Pending);
        var approved = claims.Count(x => x.Status == GuaranteeClaimStatus.Approved);
        var rejected = claims.Count(x => x.Status == GuaranteeClaimStatus.Rejected);
        var cancelled = claims.Count(x => x.Status == GuaranteeClaimStatus.Cancelled);

        var countDemandDN = claims.Count(x => x.GrtClaimTypeCode == "DN");
        var countClaimYC = claims.Count(x => x.GrtClaimTypeCode == "YC");

        var countHTC = claims.Count(x => x.FlagisHTC == "1");
        var countHTCLD = claims.Count(x => x.FlagisHTC == "2");

        var totalCars = claims.Sum(x => x.TotalCars);
        var totalClaimAmount = claims.Sum(x => x.TotalClaimAmount);
        var totalApprovedClaimAmount = claims.Where(x => x.Status == GuaranteeClaimStatus.Approved).Sum(x => x.TotalClaimAmount);

        var byBank = claims
            .GroupBy(x => x.BankCode ?? "OTHER")
            .Select(g => new
            {
                bankCode = g.Key,
                count = g.Count(),
                totalAmount = g.Sum(x => x.TotalClaimAmount),
                totalCars = g.Sum(x => x.TotalCars)
            })
            .OrderByDescending(x => x.totalAmount)
            .ToList();

        var byDealer = claims
            .GroupBy(x => new { x.DealerCode, x.DealerName })
            .Select(g => new
            {
                dealerCode = g.Key.DealerCode,
                dealerName = g.Key.DealerName,
                count = g.Count(),
                totalAmount = g.Sum(x => x.TotalClaimAmount),
                totalCars = g.Sum(x => x.TotalCars)
            })
            .OrderByDescending(x => x.totalAmount)
            .ToList();

        return new
        {
            totalClaims = total,
            pendingClaims = pending,
            approvedClaims = approved,
            rejectedClaims = rejected,
            cancelledClaims = cancelled,
            countDemandDN,
            countClaimYC,
            countHTC,
            countHTCLD,
            totalCars,
            totalClaimAmount,
            totalApprovedClaimAmount,
            byBank,
            byDealer
        };
    }
}
