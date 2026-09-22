using Microsoft.EntityFrameworkCore;
using MiniSales.Data;
using MiniSales.Models;

namespace MiniSales.Services;

public record CreatePaymentGuaranteeExtDetailDto(
    string? CarId,
    string Vin,
    string Model,
    string? SpecCode,
    string? ColorCode,
    string? ContractNo,
    string? GuaranteeNo,
    string? BankGuaranteeNo,
    string? BankCode,
    string? BankCodeMonitor,
    string? GuaranteeType,
    DateTime? DateOpen,
    DateTime? GrtDateStart,
    DateTime? GrtDateExpired,
    decimal GuaranteeValue,
    decimal? UnitPriceActual,
    string? Remark
);

public record CreatePaymentGuaranteeExtDto(
    string? GrtClaimExtNo,
    string DealerCode,
    string? DealerName,
    int? NumberOfGuaranteeExt,
    string? FlagisHTC, // "1" = HTC, "2" = HTCLD
    string? Remark,
    string? CreatedBy,
    List<CreatePaymentGuaranteeExtDetailDto>? Details
);

public record UpdatePaymentGuaranteeExtDto(
    int? NumberOfGuaranteeExt,
    string? FlagisHTC,
    string? Remark,
    string? UpdatedBy
);

public record SignPaymentGuaranteeExtDto(
    string? FileName,
    string? SignBy,
    string? Remark
);

public record CancelPaymentGuaranteeExtDto(
    string Reason,
    string? CancelBy
);

public record AddCarToGuaranteeExtDto(
    string? CarId,
    string Vin,
    string Model,
    string? SpecCode,
    string? ColorCode,
    string? ContractNo,
    string? GuaranteeNo,
    string? BankGuaranteeNo,
    string? BankCode,
    string? BankCodeMonitor,
    string? GuaranteeType,
    DateTime? DateOpen,
    DateTime? GrtDateStart,
    DateTime? GrtDateExpired,
    decimal GuaranteeValue,
    decimal? UnitPriceActual,
    string? Remark
);

public interface IPaymentGuaranteeExtService
{
    Task<object> CreateAsync(CreatePaymentGuaranteeExtDto dto);
    Task<object> ListAsync(string? status, string? dealer, string? bankCode, string? flagisHTC, string? vin, string? grtClaimExtNo);
    Task<object?> DetailAsync(string grtClaimExtNo);
    Task<object?> UpdateAsync(string grtClaimExtNo, UpdatePaymentGuaranteeExtDto dto);
    Task<object?> SignAndApproveAsync(string grtClaimExtNo, SignPaymentGuaranteeExtDto? dto);
    Task<object?> CancelAsync(string grtClaimExtNo, CancelPaymentGuaranteeExtDto dto);
    Task<object?> DeleteDraftAsync(string grtClaimExtNo);
    Task<object?> AddCarAsync(string grtClaimExtNo, AddCarToGuaranteeExtDto dto);
    Task<object?> RemoveCarAsync(string grtClaimExtNo, string vin);
    Task<object> StatsAsync();
}

public sealed class PaymentGuaranteeExtService(AppDbContext db, ITenantContext tenant) : IPaymentGuaranteeExtService
{
    private Guid Org => tenant.OrgId;

    public async Task<object> CreateAsync(CreatePaymentGuaranteeExtDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.DealerCode))
            throw new InvalidOperationException("DealerCode (mã đại lý) không được để trống.");

        var extDays = dto.NumberOfGuaranteeExt.GetValueOrDefault(30);
        if (extDays <= 0)
            throw new InvalidOperationException("NumberOfGuaranteeExt (số ngày đề nghị gia hạn) phải lớn hơn 0.");

        var flagisHTC = string.IsNullOrWhiteSpace(dto.FlagisHTC) ? "1" : dto.FlagisHTC.Trim();
        if (flagisHTC != "1" && flagisHTC != "2")
            throw new InvalidOperationException("FlagisHTC (pháp nhân phát hành) không hợp lệ (1 = HTC, 2 = HTCLD).");

        var dealerCode = dto.DealerCode.Trim().ToUpperInvariant();
        var datePrefix = DateTime.Today.ToString("yyMMdd");

        string claimExtNo;
        if (!string.IsNullOrWhiteSpace(dto.GrtClaimExtNo))
        {
            claimExtNo = dto.GrtClaimExtNo.Trim().ToUpperInvariant();
        }
        else
        {
            // Tự sinh số hiệu công văn chuẩn DMS 4.0: {yyMMdd}-{seq:D3}/CVGH/{DealerCode}
            var pattern = $"{datePrefix}-%/CVGH/{dealerCode}";
            var existingCount = await db.GuaranteeExts
                .Where(x => x.OrgId == Org && EF.Functions.Like(x.GrtClaimExtNo, pattern))
                .CountAsync();
            var seqNo = (existingCount + 1).ToString("D3");
            claimExtNo = $"{datePrefix}-{seqNo}/CVGH/{dealerCode}";
        }

        if (await db.GuaranteeExts.AnyAsync(x => x.OrgId == Org && x.GrtClaimExtNo == claimExtNo))
            throw new InvalidOperationException($"Công văn số {claimExtNo} đã tồn tại trong hệ thống.");

        var details = new List<PaymentGuaranteeExtDetail>();
        var seenVins = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        int totalNoStart = 0;

        if (dto.Details is not null && dto.Details.Count > 0)
        {
            foreach (var d in dto.Details)
            {
                if (string.IsNullOrWhiteSpace(d.Vin))
                    throw new InvalidOperationException("Số khung (VIN) của xe trong danh sách không được để trống.");
                var vin = d.Vin.Trim().ToUpperInvariant();
                if (vin.Length != 17)
                    throw new InvalidOperationException($"Số khung VIN '{vin}' phải đúng chuẩn 17 ký tự.");
                if (!seenVins.Add(vin))
                    throw new InvalidOperationException($"Số khung VIN '{vin}' bị trùng lặp trong công văn.");

                if (string.IsNullOrWhiteSpace(d.Model))
                    throw new InvalidOperationException($"Model dòng xe cho số VIN '{vin}' không được để trống.");
                if (d.GuaranteeValue <= 0)
                    throw new InvalidOperationException($"Giá trị bảo lãnh cho xe VIN '{vin}' phải lớn hơn 0.");

                bool isNoStart = d.GrtDateStart == null;
                if (isNoStart) totalNoStart++;

                var dateExpired = d.GrtDateExpired ?? DateTime.Today.AddDays(15);
                var newDateEnd = dateExpired.AddDays(extDays);

                details.Add(new PaymentGuaranteeExtDetail
                {
                    GrtClaimExtNo = claimExtNo,
                    CarId = string.IsNullOrWhiteSpace(d.CarId) ? $"CAR-{vin[..Math.Min(vin.Length, 10)]}" : d.CarId.Trim(),
                    Vin = vin,
                    Model = d.Model.Trim(),
                    SpecCode = d.SpecCode?.Trim(),
                    ColorCode = d.ColorCode?.Trim(),
                    ContractNo = d.ContractNo?.Trim(),
                    GuaranteeNo = d.GuaranteeNo?.Trim(),
                    BankGuaranteeNo = d.BankGuaranteeNo?.Trim(),
                    BankCode = d.BankCode?.Trim().ToUpperInvariant() ?? "VCB",
                    BankCodeMonitor = d.BankCodeMonitor?.Trim().ToUpperInvariant(),
                    GuaranteeType = string.IsNullOrWhiteSpace(d.GuaranteeType) ? "BL" : d.GuaranteeType.Trim().ToUpperInvariant(),
                    DateOpen = d.DateOpen ?? DateTime.Today,
                    GrtDateStart = d.GrtDateStart,
                    GrtDateExpired = dateExpired,
                    GrtDateEnd = newDateEnd,
                    GuaranteeValue = d.GuaranteeValue,
                    UnitPriceActual = d.UnitPriceActual ?? d.GuaranteeValue,
                    Status = GuaranteeExtDtlStatus.Pending,
                    Remark = d.Remark?.Trim()
                });
            }
        }

        var ext = new PaymentGuaranteeExt
        {
            OrgId = Org,
            GrtClaimExtNo = claimExtNo,
            DealerCode = dealerCode,
            DealerName = dto.DealerName?.Trim() ?? $"Đại lý {dealerCode}",
            NumberOfGuaranteeExt = extDays,
            TotalCars = details.Count,
            TotalCarNoStart = totalNoStart,
            FlagisHTC = flagisHTC,
            Status = GuaranteeExtStatus.Pending,
            Remark = dto.Remark?.Trim(),
            CreatedBy = dto.CreatedBy?.Trim() ?? "DEALER_USER",
            CreatedDateTime = DateTime.Now,
            Details = details
        };

        db.GuaranteeExts.Add(ext);
        await db.SaveChangesAsync();

        return new
        {
            message = "Lập công văn đề nghị gia hạn / phát hành bảo lãnh thành công.",
            id = ext.Id,
            grtClaimExtNo = ext.GrtClaimExtNo,
            dealerCode = ext.DealerCode,
            numberOfGuaranteeExt = ext.NumberOfGuaranteeExt,
            totalCars = ext.TotalCars,
            totalCarNoStart = ext.TotalCarNoStart,
            flagisHTC = ext.FlagisHTC,
            status = ext.Status.ToString(),
            createdDateTime = ext.CreatedDateTime
        };
    }

    public async Task<object> ListAsync(string? status, string? dealer, string? bankCode, string? flagisHTC, string? vin, string? grtClaimExtNo)
    {
        var q = db.GuaranteeExts
            .AsNoTracking()
            .Include(x => x.Details)
            .Where(x => x.OrgId == Org);

        if (!string.IsNullOrWhiteSpace(grtClaimExtNo))
        {
            var no = grtClaimExtNo.Trim().ToUpperInvariant();
            q = q.Where(x => x.GrtClaimExtNo.Contains(no));
        }

        if (!string.IsNullOrWhiteSpace(dealer))
        {
            var d = dealer.Trim().ToUpperInvariant();
            q = q.Where(x => x.DealerCode.Contains(d) || x.DealerName.Contains(d));
        }

        if (!string.IsNullOrWhiteSpace(flagisHTC))
        {
            var f = flagisHTC.Trim();
            q = q.Where(x => x.FlagisHTC == f);
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            if (Enum.TryParse<GuaranteeExtStatus>(status, true, out var st))
                q = q.Where(x => x.Status == st);
            else if (string.Equals(status, "P", StringComparison.OrdinalIgnoreCase))
                q = q.Where(x => x.Status == GuaranteeExtStatus.Pending);
            else if (string.Equals(status, "S", StringComparison.OrdinalIgnoreCase))
                q = q.Where(x => x.Status == GuaranteeExtStatus.Signed);
            else if (string.Equals(status, "C", StringComparison.OrdinalIgnoreCase))
                q = q.Where(x => x.Status == GuaranteeExtStatus.Cancelled);
        }

        if (!string.IsNullOrWhiteSpace(bankCode))
        {
            var b = bankCode.Trim().ToUpperInvariant();
            q = q.Where(x => x.Details.Any(d => d.BankCode == b));
        }

        if (!string.IsNullOrWhiteSpace(vin))
        {
            var v = vin.Trim().ToUpperInvariant();
            q = q.Where(x => x.Details.Any(d => d.Vin.Contains(v)));
        }

        var list = await q
            .OrderByDescending(x => x.CreatedDateTime)
            .Select(x => new
            {
                x.Id,
                x.GrtClaimExtNo,
                x.DealerCode,
                x.DealerName,
                x.NumberOfGuaranteeExt,
                x.TotalCars,
                x.TotalCarNoStart,
                x.FlagisHTC,
                IssuerName = x.FlagisHTC == "2" ? "Công ty TNHH HTCLD" : "Công ty CP Hyundai Thành Công VN",
                Status = x.Status.ToString(),
                x.SignDateTime,
                x.SignBy,
                x.FileSigned,
                x.Remark,
                x.CreatedBy,
                x.CreatedDateTime,
                x.CancelDateTime,
                x.CancelBy,
                TotalGuaranteeValue = x.Details.Sum(d => d.GuaranteeValue),
                Cars = x.Details.Select(d => new
                {
                    d.Id,
                    d.CarId,
                    d.Vin,
                    d.Model,
                    d.SpecCode,
                    d.ColorCode,
                    d.ContractNo,
                    d.GuaranteeNo,
                    d.BankGuaranteeNo,
                    d.BankCode,
                    d.BankCodeMonitor,
                    d.GuaranteeType,
                    d.DateOpen,
                    d.GrtDateStart,
                    d.GrtDateExpired,
                    d.GrtDateEnd,
                    d.GuaranteeValue,
                    d.UnitPriceActual,
                    Status = d.Status.ToString(),
                    d.Remark
                }).ToList()
            })
            .ToListAsync();

        return list;
    }

    public async Task<object?> DetailAsync(string grtClaimExtNo)
    {
        if (string.IsNullOrWhiteSpace(grtClaimExtNo)) return null;
        var no = grtClaimExtNo.Trim().ToUpperInvariant();

        var x = await db.GuaranteeExts
            .AsNoTracking()
            .Include(e => e.Details)
            .FirstOrDefaultAsync(e => e.OrgId == Org && e.GrtClaimExtNo == no);

        if (x is null) return null;

        return new
        {
            x.Id,
            x.GrtClaimExtNo,
            x.DealerCode,
            x.DealerName,
            x.NumberOfGuaranteeExt,
            x.TotalCars,
            x.TotalCarNoStart,
            x.FlagisHTC,
            IssuerName = x.FlagisHTC == "2" ? "Công ty TNHH HTCLD" : "Công ty CP Hyundai Thành Công VN",
            Status = x.Status.ToString(),
            x.SignDateTime,
            x.SignBy,
            x.FileSigned,
            x.Remark,
            x.CancelReason,
            x.CancelDateTime,
            x.CancelBy,
            x.CreatedBy,
            x.CreatedDateTime,
            x.LUDateTime,
            x.LUBy,
            TotalGuaranteeValue = x.Details.Sum(d => d.GuaranteeValue),
            Details = x.Details.Select(d => new
            {
                d.Id,
                d.CarId,
                d.Vin,
                d.Model,
                d.SpecCode,
                d.ColorCode,
                d.ContractNo,
                d.GuaranteeNo,
                d.BankGuaranteeNo,
                d.BankCode,
                d.BankCodeMonitor,
                d.GuaranteeType,
                d.DateOpen,
                d.GrtDateStart,
                d.GrtDateExpired,
                d.GrtDateEnd,
                d.GuaranteeValue,
                d.UnitPriceActual,
                Status = d.Status.ToString(),
                d.Remark
            }).ToList()
        };
    }

    public async Task<object?> UpdateAsync(string grtClaimExtNo, UpdatePaymentGuaranteeExtDto dto)
    {
        if (string.IsNullOrWhiteSpace(grtClaimExtNo)) return null;
        var no = grtClaimExtNo.Trim().ToUpperInvariant();

        var ext = await db.GuaranteeExts
            .Include(e => e.Details)
            .FirstOrDefaultAsync(e => e.OrgId == Org && e.GrtClaimExtNo == no);

        if (ext is null) return null;
        if (ext.Status != GuaranteeExtStatus.Pending)
            throw new InvalidOperationException($"Chỉ cho phép cập nhật công văn khi ở trạng thái Chờ xử lý (Pending). Trạng thái hiện tại: {ext.Status}.");

        if (dto.NumberOfGuaranteeExt.HasValue)
        {
            if (dto.NumberOfGuaranteeExt.Value <= 0)
                throw new InvalidOperationException("NumberOfGuaranteeExt (số ngày đề nghị gia hạn) phải lớn hơn 0.");
            ext.NumberOfGuaranteeExt = dto.NumberOfGuaranteeExt.Value;

            // Cập nhật lại ngày kết thúc mới cho từng xe
            foreach (var d in ext.Details)
            {
                var baseExpired = d.GrtDateExpired ?? DateTime.Today.AddDays(15);
                d.GrtDateEnd = baseExpired.AddDays(ext.NumberOfGuaranteeExt);
            }
        }

        if (!string.IsNullOrWhiteSpace(dto.FlagisHTC))
        {
            var flag = dto.FlagisHTC.Trim();
            if (flag != "1" && flag != "2")
                throw new InvalidOperationException("FlagisHTC không hợp lệ (1 = HTC, 2 = HTCLD).");
            ext.FlagisHTC = flag;
        }

        if (dto.Remark is not null)
            ext.Remark = dto.Remark.Trim();

        ext.LUDateTime = DateTime.Now;
        ext.LUBy = dto.UpdatedBy?.Trim() ?? "DEALER_USER";

        await db.SaveChangesAsync();

        return new
        {
            message = "Cập nhật công văn gia hạn bảo lãnh thành công.",
            grtClaimExtNo = ext.GrtClaimExtNo,
            numberOfGuaranteeExt = ext.NumberOfGuaranteeExt,
            flagisHTC = ext.FlagisHTC,
            luDateTime = ext.LUDateTime
        };
    }

    public async Task<object?> SignAndApproveAsync(string grtClaimExtNo, SignPaymentGuaranteeExtDto? dto)
    {
        if (string.IsNullOrWhiteSpace(grtClaimExtNo)) return null;
        var no = grtClaimExtNo.Trim().ToUpperInvariant();

        var ext = await db.GuaranteeExts
            .Include(e => e.Details)
            .FirstOrDefaultAsync(e => e.OrgId == Org && e.GrtClaimExtNo == no);

        if (ext is null) return null;
        if (ext.Status != GuaranteeExtStatus.Pending)
            throw new InvalidOperationException($"Chỉ có thể ký duyệt công văn khi ở trạng thái Chờ xử lý (Pending). Trạng thái hiện tại: {ext.Status}.");

        if (ext.Details.Count == 0)
            throw new InvalidOperationException("Công văn chưa có dòng xe nào, không thể ký duyệt.");

        // Kiểm tra ràng buộc nguồn: Toàn bộ xe trong công văn phải ở trạng thái Pending ("P")
        var invalidCar = ext.Details.FirstOrDefault(d => d.Status != GuaranteeExtDtlStatus.Pending);
        if (invalidCar is not null)
            throw new InvalidOperationException($"Xe VIN '{invalidCar.Vin}' đang ở trạng thái {invalidCar.Status}, không hợp lệ để ký duyệt.");

        var signTime = DateTime.Now;
        var signUser = dto?.SignBy?.Trim() ?? "NPP_HQ_DIRECTOR";
        var fileName = dto?.FileName?.Trim() ?? $"{ext.GrtClaimExtNo.Replace('/', '_')}_signed.pdf";
        var filePath = $"/storage/grtclaimext/{fileName}";

        ext.Status = GuaranteeExtStatus.Signed;
        ext.SignDateTime = signTime;
        ext.SignBy = signUser;
        ext.FileSigned = filePath;
        if (!string.IsNullOrWhiteSpace(dto?.Remark))
            ext.Remark = (ext.Remark + " | " + dto.Remark).Trim(' ', '|');
        ext.LUDateTime = signTime;
        ext.LUBy = signUser;

        // Cập nhật trạng thái từng xe sang Signed và đồng bộ gia hạn bảo lãnh
        foreach (var d in ext.Details)
        {
            d.Status = GuaranteeExtDtlStatus.Signed;
            var baseExpired = d.GrtDateExpired ?? DateTime.Today.AddDays(15);
            d.GrtDateEnd = baseExpired.AddDays(ext.NumberOfGuaranteeExt);

            // Đồng bộ sang bảng PaymentGuaranteeDetail nếu tìm thấy xe
            var pmtDtl = await db.PaymentGuaranteeDetails
                .FirstOrDefaultAsync(p => p.Vin == d.Vin || (p.OrderNo == d.ContractNo && p.Model == d.Model));
            if (pmtDtl != null)
            {
                pmtDtl.DateEnd = d.GrtDateEnd;
                if (pmtDtl.DateStart == null)
                    pmtDtl.DateStart = DateTime.Today;
                pmtDtl.NumberOfDaysDeferredPayment += ext.NumberOfGuaranteeExt;
                pmtDtl.Remark = (pmtDtl.Remark + $" | Gia hạn thêm {ext.NumberOfGuaranteeExt} ngày theo CV {ext.GrtClaimExtNo}").Trim(' ', '|');
            }
        }

        await db.SaveChangesAsync();

        return new
        {
            message = "Ký số điện tử và phê duyệt công văn gia hạn bảo lãnh thành công.",
            grtClaimExtNo = ext.GrtClaimExtNo,
            status = ext.Status.ToString(),
            signDateTime = ext.SignDateTime,
            signBy = ext.SignBy,
            fileSigned = ext.FileSigned,
            totalCarsUpdated = ext.Details.Count,
            extendedDays = ext.NumberOfGuaranteeExt
        };
    }

    public async Task<object?> CancelAsync(string grtClaimExtNo, CancelPaymentGuaranteeExtDto dto)
    {
        if (string.IsNullOrWhiteSpace(grtClaimExtNo)) return null;
        var no = grtClaimExtNo.Trim().ToUpperInvariant();

        if (string.IsNullOrWhiteSpace(dto.Reason))
            throw new InvalidOperationException("Lý do hủy công văn không được để trống.");

        var ext = await db.GuaranteeExts
            .Include(e => e.Details)
            .FirstOrDefaultAsync(e => e.OrgId == Org && e.GrtClaimExtNo == no);

        if (ext is null) return null;
        if (ext.Status == GuaranteeExtStatus.Cancelled)
            throw new InvalidOperationException("Công văn này đã bị hủy trước đó.");

        var cancelTime = DateTime.Now;
        var cancelUser = dto.CancelBy?.Trim() ?? "NPP_MANAGER";

        ext.Status = GuaranteeExtStatus.Cancelled;
        ext.CancelReason = dto.Reason.Trim();
        ext.CancelDateTime = cancelTime;
        ext.CancelBy = cancelUser;
        ext.LUDateTime = cancelTime;
        ext.LUBy = cancelUser;

        foreach (var d in ext.Details)
        {
            d.Status = GuaranteeExtDtlStatus.Cancelled;
        }

        await db.SaveChangesAsync();

        return new
        {
            message = "Hủy công văn đề nghị gia hạn bảo lãnh thành công.",
            grtClaimExtNo = ext.GrtClaimExtNo,
            status = ext.Status.ToString(),
            cancelReason = ext.CancelReason,
            cancelDateTime = ext.CancelDateTime,
            cancelBy = ext.CancelBy
        };
    }

    public async Task<object?> DeleteDraftAsync(string grtClaimExtNo)
    {
        if (string.IsNullOrWhiteSpace(grtClaimExtNo)) return null;
        var no = grtClaimExtNo.Trim().ToUpperInvariant();

        var ext = await db.GuaranteeExts
            .Include(e => e.Details)
            .FirstOrDefaultAsync(e => e.OrgId == Org && e.GrtClaimExtNo == no);

        if (ext is null) return null;
        if (ext.Status != GuaranteeExtStatus.Pending)
            throw new InvalidOperationException($"Chỉ có thể xóa công văn khi ở trạng thái Chờ xử lý (Pending). Công văn hiện tại có trạng thái {ext.Status}.");

        db.GuaranteeExts.Remove(ext);
        await db.SaveChangesAsync();

        return new
        {
            message = $"Đã xóa công văn nháp {no} và toàn bộ xe đính kèm thành công.",
            grtClaimExtNo = no
        };
    }

    public async Task<object?> AddCarAsync(string grtClaimExtNo, AddCarToGuaranteeExtDto dto)
    {
        if (string.IsNullOrWhiteSpace(grtClaimExtNo)) return null;
        var no = grtClaimExtNo.Trim().ToUpperInvariant();

        var ext = await db.GuaranteeExts
            .Include(e => e.Details)
            .FirstOrDefaultAsync(e => e.OrgId == Org && e.GrtClaimExtNo == no);

        if (ext is null) return null;
        if (ext.Status != GuaranteeExtStatus.Pending)
            throw new InvalidOperationException($"Chỉ có thể thêm xe vào công văn khi ở trạng thái Chờ xử lý (Pending). Trạng thái hiện tại: {ext.Status}.");

        if (string.IsNullOrWhiteSpace(dto.Vin))
            throw new InvalidOperationException("Số khung (VIN) không được để trống.");
        var vin = dto.Vin.Trim().ToUpperInvariant();
        if (vin.Length != 17)
            throw new InvalidOperationException($"Số khung VIN '{vin}' phải đúng chuẩn 17 ký tự.");

        if (ext.Details.Any(d => string.Equals(d.Vin, vin, StringComparison.OrdinalIgnoreCase)))
            throw new InvalidOperationException($"Xe có số khung VIN '{vin}' đã tồn tại trong công văn này.");

        if (string.IsNullOrWhiteSpace(dto.Model))
            throw new InvalidOperationException("Model dòng xe không được để trống.");
        if (dto.GuaranteeValue <= 0)
            throw new InvalidOperationException("Giá trị bảo lãnh cho xe phải lớn hơn 0.");

        var dateExpired = dto.GrtDateExpired ?? DateTime.Today.AddDays(15);
        var dateEnd = dateExpired.AddDays(ext.NumberOfGuaranteeExt);

        var detail = new PaymentGuaranteeExtDetail
        {
            GrtClaimExtNo = ext.GrtClaimExtNo,
            GrtClaimExtId = ext.Id,
            CarId = string.IsNullOrWhiteSpace(dto.CarId) ? $"CAR-{vin[..Math.Min(vin.Length, 10)]}" : dto.CarId.Trim(),
            Vin = vin,
            Model = dto.Model.Trim(),
            SpecCode = dto.SpecCode?.Trim(),
            ColorCode = dto.ColorCode?.Trim(),
            ContractNo = dto.ContractNo?.Trim(),
            GuaranteeNo = dto.GuaranteeNo?.Trim(),
            BankGuaranteeNo = dto.BankGuaranteeNo?.Trim(),
            BankCode = dto.BankCode?.Trim().ToUpperInvariant() ?? "VCB",
            BankCodeMonitor = dto.BankCodeMonitor?.Trim().ToUpperInvariant(),
            GuaranteeType = string.IsNullOrWhiteSpace(dto.GuaranteeType) ? "BL" : dto.GuaranteeType.Trim().ToUpperInvariant(),
            DateOpen = dto.DateOpen ?? DateTime.Today,
            GrtDateStart = dto.GrtDateStart,
            GrtDateExpired = dateExpired,
            GrtDateEnd = dateEnd,
            GuaranteeValue = dto.GuaranteeValue,
            UnitPriceActual = dto.UnitPriceActual ?? dto.GuaranteeValue,
            Status = GuaranteeExtDtlStatus.Pending,
            Remark = dto.Remark?.Trim()
        };

        ext.Details.Add(detail);
        ext.TotalCars = ext.Details.Count;
        ext.TotalCarNoStart = ext.Details.Count(d => d.GrtDateStart == null);
        ext.LUDateTime = DateTime.Now;
        ext.LUBy = "DEALER_USER";

        await db.SaveChangesAsync();

        return new
        {
            message = $"Thêm xe VIN {vin} vào công văn thành công.",
            grtClaimExtNo = ext.GrtClaimExtNo,
            vin,
            model = detail.Model,
            grtDateEnd = detail.GrtDateEnd,
            totalCars = ext.TotalCars,
            totalCarNoStart = ext.TotalCarNoStart
        };
    }

    public async Task<object?> RemoveCarAsync(string grtClaimExtNo, string vin)
    {
        if (string.IsNullOrWhiteSpace(grtClaimExtNo) || string.IsNullOrWhiteSpace(vin)) return null;
        var no = grtClaimExtNo.Trim().ToUpperInvariant();
        var v = vin.Trim().ToUpperInvariant();

        var ext = await db.GuaranteeExts
            .Include(e => e.Details)
            .FirstOrDefaultAsync(e => e.OrgId == Org && e.GrtClaimExtNo == no);

        if (ext is null) return null;
        if (ext.Status != GuaranteeExtStatus.Pending)
            throw new InvalidOperationException($"Chỉ có thể gỡ xe khỏi công văn khi ở trạng thái Chờ xử lý (Pending). Trạng thái hiện tại: {ext.Status}.");

        var dtl = ext.Details.FirstOrDefault(d => string.Equals(d.Vin, v, StringComparison.OrdinalIgnoreCase) || string.Equals(d.CarId, v, StringComparison.OrdinalIgnoreCase));
        if (dtl is null)
            throw new InvalidOperationException($"Không tìm thấy xe VIN/CarId '{v}' trong công văn {no}.");

        ext.Details.Remove(dtl);
        db.GuaranteeExtDetails.Remove(dtl);

        ext.TotalCars = ext.Details.Count;
        ext.TotalCarNoStart = ext.Details.Count(d => d.GrtDateStart == null);
        ext.LUDateTime = DateTime.Now;
        ext.LUBy = "DEALER_USER";

        await db.SaveChangesAsync();

        return new
        {
            message = $"Đã gỡ xe VIN {v} khỏi công văn {no}.",
            grtClaimExtNo = no,
            vin = v,
            totalCars = ext.TotalCars,
            totalCarNoStart = ext.TotalCarNoStart
        };
    }

    public async Task<object> StatsAsync()
    {
        var exts = await db.GuaranteeExts
            .AsNoTracking()
            .Include(e => e.Details)
            .Where(e => e.OrgId == Org)
            .ToListAsync();

        var total = exts.Count;
        var pending = exts.Count(x => x.Status == GuaranteeExtStatus.Pending);
        var signed = exts.Count(x => x.Status == GuaranteeExtStatus.Signed);
        var cancelled = exts.Count(x => x.Status == GuaranteeExtStatus.Cancelled);

        var totalCars = exts.Sum(x => x.TotalCars);
        var totalNoStart = exts.Sum(x => x.TotalCarNoStart);
        var totalAmount = exts.SelectMany(x => x.Details).Sum(d => d.GuaranteeValue);

        var avgExtDays = total > 0 ? Math.Round((double)exts.Average(x => x.NumberOfGuaranteeExt), 1) : 0;

        var byIssuer = exts
            .GroupBy(x => x.FlagisHTC)
            .Select(g => new
            {
                FlagisHTC = g.Key,
                Issuer = g.Key == "2" ? "Công ty TNHH HTCLD" : "Công ty CP Hyundai Thành Công VN",
                Count = g.Count(),
                Cars = g.Sum(x => x.TotalCars),
                Amount = g.SelectMany(x => x.Details).Sum(d => d.GuaranteeValue)
            }).ToList();

        var byDealer = exts
            .GroupBy(x => new { x.DealerCode, x.DealerName })
            .Select(g => new
            {
                g.Key.DealerCode,
                g.Key.DealerName,
                Count = g.Count(),
                Cars = g.Sum(x => x.TotalCars),
                Amount = g.SelectMany(x => x.Details).Sum(d => d.GuaranteeValue)
            }).ToList();

        var byBank = exts
            .SelectMany(x => x.Details)
            .GroupBy(d => d.BankCode ?? "OTHER")
            .Select(g => new
            {
                BankCode = g.Key,
                Cars = g.Count(),
                Amount = g.Sum(d => d.GuaranteeValue)
            }).ToList();

        return new
        {
            totalRequests = total,
            pendingCount = pending,
            signedCount = signed,
            cancelledCount = cancelled,
            totalCars,
            totalCarsNoStart = totalNoStart,
            totalGuaranteeAmount = totalAmount,
            averageExtensionDays = avgExtDays,
            byIssuer,
            byDealer,
            byBank
        };
    }
}
