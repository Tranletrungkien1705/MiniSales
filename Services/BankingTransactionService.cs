using Microsoft.EntityFrameworkCore;
using MiniSales.Data;
using MiniSales.Models;

namespace MiniSales.Services;

public record BankingTransCarInputDto(
    string CarId,
    string Vin,
    string ModelCode,
    string? ModelName = null,
    string? SpecCode = null,
    string? SpecDescription = null,
    string? ColorCode = null,
    string? DlrCtrNo = null,
    string? SOCode = null,
    string? HTCInvoiceNo = null,
    decimal AmountActual = 0m,
    decimal AllocPercent = 100m,
    decimal? AllocAmount = null,
    string? Remark = null
);

public record BankingTransAttachmentInputDto(
    BankingTransFileType FileType,
    string FileName,
    string FilePath,
    string? FileUrl = null,
    long? FileSize = null,
    string? Remark = null
);

public record CreateBankingTransactionDto(
    string? RQ_BankingTransNo = null,
    string DealerCode = "",
    string? DealerName = null,
    string BankCode = "VPB",
    string? BankName = null,
    string? TaxCode = null,
    BankingTransType TransType = BankingTransType.PMT,
    decimal? TotalAmount = null,
    string? Remark = null,
    string? CreatedBy = null,

    // PMT Fields:
    string? PaymentNo = null,
    string? PaymentType = "Payment",
    BankingTransDisbursementType DisbursementType = BankingTransDisbursementType.LoanDisbursement,
    decimal? TransferAmount = null,
    int? LoanPeriod = 3,
    DateTime? LoanPeriodDate = null,
    decimal? InterestRate = 7.5m,
    string? ReceivingUnit = null,
    string? BankAccountReceive = null,
    string? BankNameReceive = null,
    string? CreditContractNo = null,
    DateTime? DisbursementRequestDate = null,

    // GRT Fields:
    string? GuaranteeType = null,
    int? DateExpiredValue = 90,
    string? GrtForm = null,
    string? GrtReceive = null,
    decimal? GrtAmount = null,
    DateTime? GrtDateStart = null,
    DateTime? GrtDateEnd = null,

    // Items & Attachments:
    List<BankingTransCarInputDto>? Cars = null,
    List<BankingTransAttachmentInputDto>? Attachments = null
);

public record UpdateBankingTransactionDto(
    string? BankCode = null,
    string? BankName = null,
    string? TaxCode = null,
    BankingTransType? TransType = null,
    string? Remark = null,
    string? UpdatedBy = null,

    // PMT Fields:
    string? PaymentNo = null,
    string? PaymentType = null,
    BankingTransDisbursementType? DisbursementType = null,
    decimal? TransferAmount = null,
    int? LoanPeriod = null,
    DateTime? LoanPeriodDate = null,
    decimal? InterestRate = null,
    string? ReceivingUnit = null,
    string? BankAccountReceive = null,
    string? BankNameReceive = null,
    string? CreditContractNo = null,
    DateTime? DisbursementRequestDate = null,

    // GRT Fields:
    string? GuaranteeType = null,
    int? DateExpiredValue = null,
    string? GrtForm = null,
    string? GrtReceive = null,
    decimal? GrtAmount = null,
    DateTime? GrtDateStart = null,
    DateTime? GrtDateEnd = null,

    // Optional replacement lists:
    List<BankingTransCarInputDto>? Cars = null,
    List<BankingTransAttachmentInputDto>? Attachments = null
);

public record PushBankDto(
    string? PushedBy = null,
    string? Note = null
);

public record BankApproveDto(
    string? LDNo = null, // Số khế ước nhận nợ (cho PMT)
    string? MDNo = null, // Số chứng thư bảo lãnh (cho GRT)
    string? BankRemark = null,
    string? BankApprovedBy = null
);

public record DisburseBankingTransDto(
    decimal? DisbursementAmount = null,
    DateTime? DisbursementDate = null,
    string? LDNo = null,
    string? BankRemark = null,
    string? DisbursedBy = null
);

public record BankRejectDto(
    string Reason,
    string? BankRejectedBy = null
);

public record CancelBankingTransDto(
    string Reason,
    string? CancelledBy = null
);

public record AddBankingTransCarDto(
    string CarId,
    string Vin,
    string ModelCode,
    string? ModelName = null,
    string? SpecCode = null,
    string? SpecDescription = null,
    string? ColorCode = null,
    string? DlrCtrNo = null,
    string? SOCode = null,
    string? HTCInvoiceNo = null,
    decimal AmountActual = 0m,
    decimal AllocPercent = 100m,
    decimal? AllocAmount = null,
    string? Remark = null
);

public record AddBankingTransAttachmentDto(
    BankingTransFileType FileType,
    string FileName,
    string FilePath,
    string? FileUrl = null,
    long? FileSize = null,
    string? Remark = null,
    string? UploadedBy = null
);

public record SignBankingTransFileDto(
    long FileId,
    string SerialNumber,
    string CaSubject,
    string? SignedBy = null,
    string? Note = null
);

public interface IBankingTransactionService
{
    Task<string> GetNextSeqAsync();
    Task<object> ListAsync(string? status, string? bankStatus, string? bankCode, string? dealerCode, string? transType, string? transNo);
    Task<object?> DetailAsync(string transNo);
    Task<object> CreateAsync(CreateBankingTransactionDto dto);
    Task<object?> UpdateAsync(string transNo, UpdateBankingTransactionDto dto);
    Task<object?> PushBankAsync(string transNo, PushBankDto? dto);
    Task<object?> BankApproveAsync(string transNo, BankApproveDto dto);
    Task<object?> DisburseAsync(string transNo, DisburseBankingTransDto dto);
    Task<object?> BankRejectAsync(string transNo, BankRejectDto dto);
    Task<object?> CancelAsync(string transNo, CancelBankingTransDto dto);
    Task<object?> DeleteDraftAsync(string transNo);
    Task<object?> AddCarAsync(string transNo, AddBankingTransCarDto dto);
    Task<object?> RemoveCarAsync(string transNo, long detailId);
    Task<object?> AddAttachmentAsync(string transNo, AddBankingTransAttachmentDto dto);
    Task<object?> SignBankFileAsync(string transNo, SignBankingTransFileDto dto);
    Task<object> GetEligibleCarsAsync(string? dealerCode, string? contractNo);
    Task<object> GetStatsAsync();
}

public sealed class BankingTransactionService(AppDbContext db, ITenantContext tenant) : IBankingTransactionService
{
    private Guid Org => tenant.OrgId;

    private static string ResolveBankName(string code) => code.Trim().ToUpperInvariant() switch
    {
        "VPB" or "VPBANK" => "Ngân hàng TMCP Việt Nam Thịnh Vượng (VPBank)",
        "CTG" or "VIETINBANK" => "Ngân hàng TMCP Công Thương Việt Nam (VietinBank)",
        "VIB" => "Ngân hàng TMCP Quốc tế Việt Nam (VIB)",
        "BIDV" => "Ngân hàng TMCP Đầu tư và Phát triển Việt Nam (BIDV)",
        "TCB" or "TECHCOMBANK" => "Ngân hàng TMCP Kỹ Thương Việt Nam (Techcombank)",
        "MB" or "MBBANK" => "Ngân hàng TMCP Quân đội (MBBank)",
        _ => $"Ngân hàng liên kết tài trợ ({code})"
    };

    public async Task<string> GetNextSeqAsync()
    {
        var prefix = DateTime.Today.ToString("yyMM");
        var count = await db.BankingTransactions
            .CountAsync(x => x.OrgId == Org && x.RQ_BankingTransNo.StartsWith(prefix));
        return $"{prefix}BKT{(count + 1):D4}";
    }

    public async Task<object> ListAsync(string? status, string? bankStatus, string? bankCode, string? dealerCode, string? transType, string? transNo)
    {
        var query = db.BankingTransactions
            .AsNoTracking()
            .Where(x => x.OrgId == Org);

        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<BankingTransStatus>(status, true, out var st))
            query = query.Where(x => x.Status == st);

        if (!string.IsNullOrWhiteSpace(bankStatus) && Enum.TryParse<BankingTransBankStatus>(bankStatus, true, out var bst))
            query = query.Where(x => x.BankStatus == bst);

        if (!string.IsNullOrWhiteSpace(bankCode))
        {
            var bc = bankCode.Trim().ToUpperInvariant();
            query = query.Where(x => x.BankCode == bc);
        }

        if (!string.IsNullOrWhiteSpace(dealerCode))
        {
            var dc = dealerCode.Trim().ToUpperInvariant();
            query = query.Where(x => x.DealerCode == dc);
        }

        if (!string.IsNullOrWhiteSpace(transType) && Enum.TryParse<BankingTransType>(transType, true, out var tt))
            query = query.Where(x => x.TransType == tt);

        if (!string.IsNullOrWhiteSpace(transNo))
        {
            var no = transNo.Trim().ToUpperInvariant();
            query = query.Where(x => x.RQ_BankingTransNo.Contains(no));
        }

        var list = await query
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new
            {
                x.Id,
                x.RQ_BankingTransNo,
                x.DealerCode,
                x.DealerName,
                x.BankCode,
                x.BankName,
                x.TaxCode,
                TransType = x.TransType.ToString(),
                Status = x.Status.ToString(),
                BankStatus = x.BankStatus.ToString(),
                x.RefBankCode,
                x.TotalAmount,
                x.TotalCars,
                x.TransferAmount,
                x.LoanPeriod,
                x.InterestRate,
                x.LDNo,
                x.DisbursementAmount,
                x.DisbursementDate,
                x.MDNo,
                x.GrtAmount,
                x.GrtDateStart,
                x.GrtDateEnd,
                x.PushedToBankAt,
                x.BankApprovedAt,
                x.CreatedAt,
                x.CreatedBy,
                x.Remark,
                x.BankRemark
            })
            .ToListAsync();

        return new
        {
            Total = list.Count,
            Items = list
        };
    }

    public async Task<object?> DetailAsync(string transNo)
    {
        var cleanNo = transNo.Trim().ToUpperInvariant();
        var entity = await db.BankingTransactions
            .AsNoTracking()
            .Include(x => x.Details)
            .Include(x => x.Attachments)
            .FirstOrDefaultAsync(x => x.OrgId == Org && x.RQ_BankingTransNo == cleanNo);

        if (entity == null) return null;

        return new
        {
            entity.Id,
            entity.RQ_BankingTransNo,
            entity.DealerCode,
            entity.DealerName,
            entity.BankCode,
            entity.BankName,
            entity.TaxCode,
            TransType = entity.TransType.ToString(),
            Status = entity.Status.ToString(),
            BankStatus = entity.BankStatus.ToString(),
            entity.RefBankCode,
            entity.BankRemark,
            entity.Remark,
            entity.TotalAmount,
            entity.TotalCars,

            // PMT:
            entity.PaymentNo,
            entity.PaymentType,
            DisbursementType = entity.DisbursementType.ToString(),
            entity.TransferAmount,
            entity.LoanPeriod,
            entity.LoanPeriodDate,
            entity.InterestRate,
            entity.ReceivingUnit,
            entity.BankAccountReceive,
            entity.BankNameReceive,
            entity.CreditContractNo,
            entity.DisbursementRequestDate,
            entity.LDNo,
            entity.DisbursementAmount,
            entity.DisbursementDate,

            // GRT:
            entity.GuaranteeType,
            entity.DateExpiredValue,
            entity.GrtForm,
            entity.GrtReceive,
            entity.MDNo,
            entity.GrtAmount,
            entity.GrtDateStart,
            entity.GrtDateEnd,

            // Logs:
            entity.CreatedAt,
            entity.CreatedBy,
            entity.PushedToBankAt,
            entity.PushedToBankBy,
            entity.BankApprovedAt,
            entity.BankApprovedBy,
            entity.DisbursedAt,
            entity.DisbursedBy,
            entity.RejectedAt,
            entity.RejectedBy,
            entity.RejectReason,
            entity.CancelledAt,
            entity.CancelledBy,
            entity.CancelReason,
            entity.LUDateTime,
            entity.LUBy,

            Cars = entity.Details.Select(d => new
            {
                d.Id,
                d.CarId,
                d.Vin,
                d.ModelCode,
                d.ModelName,
                d.SpecCode,
                d.SpecDescription,
                d.ColorCode,
                d.DlrCtrNo,
                d.SOCode,
                d.HTCInvoiceNo,
                d.AmountActual,
                d.AllocPercent,
                d.AllocAmount,
                d.Remark
            }),

            Attachments = entity.Attachments.Select(a => new
            {
                a.Id,
                a.FileIndex,
                FileType = a.FileType.ToString(),
                a.FileName,
                a.FilePath,
                a.FileUrl,
                a.FileSize,
                a.SerialNumber,
                a.CaSubject,
                a.FlagSigned,
                a.SignedAt,
                a.SignedBy,
                a.Remark,
                a.UploadedAt,
                a.UploadedBy
            })
        };
    }

    public async Task<object> CreateAsync(CreateBankingTransactionDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.DealerCode))
            throw new InvalidOperationException("Mã đại lý (DealerCode) không được để trống.");

        if (string.IsNullOrWhiteSpace(dto.BankCode))
            throw new InvalidOperationException("Mã ngân hàng tài trợ (BankCode) không được để trống.");

        var transNo = dto.RQ_BankingTransNo?.Trim().ToUpperInvariant();
        if (string.IsNullOrWhiteSpace(transNo))
        {
            transNo = await GetNextSeqAsync();
        }

        if (await db.BankingTransactions.AnyAsync(x => x.OrgId == Org && x.RQ_BankingTransNo == transNo))
            throw new InvalidOperationException($"Mã đề nghị giao dịch ngân hàng '{transNo}' đã tồn tại trong hệ thống.");

        var bankCode = dto.BankCode.Trim().ToUpperInvariant();
        var bankName = !string.IsNullOrWhiteSpace(dto.BankName) ? dto.BankName.Trim() : ResolveBankName(bankCode);

        // Dealer Name lookup
        var dealerName = dto.DealerName;
        if (string.IsNullOrWhiteSpace(dealerName))
        {
            var dOrder = await db.DealerContracts.FirstOrDefaultAsync(x => x.OrgId == Org && x.DealerCode == dto.DealerCode);
            dealerName = dOrder?.DealerName ?? $"Đại lý Hyundai {dto.DealerCode}";
        }

        var details = new List<BankingTransactionDetail>();
        decimal sumCarsAmount = 0m;

        if (dto.Cars is { Count: > 0 })
        {
            foreach (var c in dto.Cars)
            {
                if (string.IsNullOrWhiteSpace(c.Vin))
                    throw new InvalidOperationException("Số khung xe (Vin) không được để trống.");

                var allocPct = c.AllocPercent > 0 ? c.AllocPercent : 100m;
                var allocAmt = c.AllocAmount ?? Math.Round(c.AmountActual * allocPct / 100m, 0);

                details.Add(new BankingTransactionDetail
                {
                    RQ_BankingTransNo = transNo,
                    CarId = c.CarId ?? $"CAR-{c.Vin[^6..]}",
                    Vin = c.Vin.Trim().ToUpperInvariant(),
                    ModelCode = c.ModelCode.Trim().ToUpperInvariant(),
                    ModelName = c.ModelName ?? c.ModelCode,
                    SpecCode = c.SpecCode,
                    SpecDescription = c.SpecDescription,
                    ColorCode = c.ColorCode,
                    DlrCtrNo = c.DlrCtrNo,
                    SOCode = c.SOCode,
                    HTCInvoiceNo = c.HTCInvoiceNo,
                    AmountActual = c.AmountActual,
                    AllocPercent = allocPct,
                    AllocAmount = allocAmt,
                    Remark = c.Remark
                });

                sumCarsAmount += allocAmt;
            }
        }

        decimal finalTotalAmount = dto.TotalAmount ?? (sumCarsAmount > 0 ? sumCarsAmount : (dto.TransferAmount ?? dto.GrtAmount ?? 0m));

        var attachments = new List<BankingTransactionAttachFile>();
        if (dto.Attachments is { Count: > 0 })
        {
            int idx = 1;
            foreach (var a in dto.Attachments)
            {
                attachments.Add(new BankingTransactionAttachFile
                {
                    RQ_BankingTransNo = transNo,
                    FileIndex = idx++,
                    FileType = a.FileType,
                    FileName = a.FileName,
                    FilePath = a.FilePath,
                    FileUrl = a.FileUrl ?? a.FilePath,
                    FileSize = a.FileSize ?? 102400L,
                    Remark = a.Remark,
                    UploadedAt = DateTime.Now,
                    UploadedBy = dto.CreatedBy ?? "DEALER_STAFF"
                });
            }
        }

        var entity = new BankingTransaction
        {
            OrgId = Org,
            RQ_BankingTransNo = transNo,
            DealerCode = dto.DealerCode.Trim().ToUpperInvariant(),
            DealerName = dealerName,
            BankCode = bankCode,
            BankName = bankName,
            TaxCode = dto.TaxCode,
            TransType = dto.TransType,
            Status = BankingTransStatus.Draft,
            BankStatus = BankingTransBankStatus.Draft,
            Remark = dto.Remark,
            TotalAmount = finalTotalAmount,
            TotalCars = details.Count,

            // PMT:
            PaymentNo = dto.PaymentNo,
            PaymentType = dto.PaymentType ?? "Payment",
            DisbursementType = dto.DisbursementType,
            TransferAmount = dto.TransferAmount ?? finalTotalAmount,
            LoanPeriod = dto.LoanPeriod ?? 3,
            LoanPeriodDate = dto.LoanPeriodDate ?? DateTime.Today.AddMonths(dto.LoanPeriod ?? 3),
            InterestRate = dto.InterestRate ?? 7.5m,
            ReceivingUnit = dto.ReceivingUnit ?? "CÔNG TY CỔ PHẦN LIÊN DOANH Ô TÔ HYUNDAI THÀNH CÔNG VIỆT NAM",
            BankAccountReceive = dto.BankAccountReceive ?? "111000123456",
            BankNameReceive = dto.BankNameReceive ?? "VietinBank - Chi nhánh Hà Nội",
            CreditContractNo = dto.CreditContractNo,
            DisbursementRequestDate = dto.DisbursementRequestDate ?? DateTime.Today.AddDays(2),

            // GRT:
            GuaranteeType = dto.GuaranteeType ?? (dto.TransType == BankingTransType.GRT_LC ? "Bảo lãnh mở thư tín dụng L/C mua buôn xe" : "Bảo lãnh thanh toán mua buôn xe ô tô"),
            DateExpiredValue = dto.DateExpiredValue ?? 90,
            GrtForm = dto.GrtForm ?? "Thư bảo lãnh điện tử ký số CA",
            GrtReceive = dto.GrtReceive ?? "HYUNDAI THANH CONG VIETNAM",
            GrtAmount = dto.GrtAmount ?? finalTotalAmount,
            GrtDateStart = dto.GrtDateStart ?? DateTime.Today,
            GrtDateEnd = dto.GrtDateEnd ?? DateTime.Today.AddDays(dto.DateExpiredValue ?? 90),

            CreatedBy = dto.CreatedBy ?? "DEALER_STAFF",
            CreatedAt = DateTime.Now,
            Details = details,
            Attachments = attachments
        };

        db.BankingTransactions.Add(entity);
        await db.SaveChangesAsync();

        return new
        {
            Success = true,
            Message = $"Lập Đề nghị giao dịch ngân hàng '{transNo}' thành công.",
            RQ_BankingTransNo = transNo,
            TransType = entity.TransType.ToString(),
            entity.TotalAmount,
            entity.TotalCars,
            Status = entity.Status.ToString(),
            BankStatus = entity.BankStatus.ToString()
        };
    }

    public async Task<object?> UpdateAsync(string transNo, UpdateBankingTransactionDto dto)
    {
        var cleanNo = transNo.Trim().ToUpperInvariant();
        var entity = await db.BankingTransactions
            .Include(x => x.Details)
            .Include(x => x.Attachments)
            .FirstOrDefaultAsync(x => x.OrgId == Org && x.RQ_BankingTransNo == cleanNo);

        if (entity == null) return null;

        if (entity.Status != BankingTransStatus.Draft && entity.Status != BankingTransStatus.Rejected)
            throw new InvalidOperationException($"Chỉ cho phép chỉnh sửa đề nghị ở trạng thái Draft hoặc Rejected (Hiện tại: {entity.Status}).");

        if (!string.IsNullOrWhiteSpace(dto.BankCode))
        {
            entity.BankCode = dto.BankCode.Trim().ToUpperInvariant();
            entity.BankName = !string.IsNullOrWhiteSpace(dto.BankName) ? dto.BankName.Trim() : ResolveBankName(entity.BankCode);
        }

        if (dto.TaxCode != null) entity.TaxCode = dto.TaxCode;
        if (dto.TransType.HasValue) entity.TransType = dto.TransType.Value;
        if (dto.Remark != null) entity.Remark = dto.Remark;

        // PMT:
        if (dto.PaymentNo != null) entity.PaymentNo = dto.PaymentNo;
        if (dto.PaymentType != null) entity.PaymentType = dto.PaymentType;
        if (dto.DisbursementType.HasValue) entity.DisbursementType = dto.DisbursementType.Value;
        if (dto.TransferAmount.HasValue) entity.TransferAmount = dto.TransferAmount.Value;
        if (dto.LoanPeriod.HasValue) entity.LoanPeriod = dto.LoanPeriod.Value;
        if (dto.LoanPeriodDate.HasValue) entity.LoanPeriodDate = dto.LoanPeriodDate.Value;
        if (dto.InterestRate.HasValue) entity.InterestRate = dto.InterestRate.Value;
        if (dto.ReceivingUnit != null) entity.ReceivingUnit = dto.ReceivingUnit;
        if (dto.BankAccountReceive != null) entity.BankAccountReceive = dto.BankAccountReceive;
        if (dto.BankNameReceive != null) entity.BankNameReceive = dto.BankNameReceive;
        if (dto.CreditContractNo != null) entity.CreditContractNo = dto.CreditContractNo;
        if (dto.DisbursementRequestDate.HasValue) entity.DisbursementRequestDate = dto.DisbursementRequestDate.Value;

        // GRT:
        if (dto.GuaranteeType != null) entity.GuaranteeType = dto.GuaranteeType;
        if (dto.DateExpiredValue.HasValue) entity.DateExpiredValue = dto.DateExpiredValue.Value;
        if (dto.GrtForm != null) entity.GrtForm = dto.GrtForm;
        if (dto.GrtReceive != null) entity.GrtReceive = dto.GrtReceive;
        if (dto.GrtAmount.HasValue) entity.GrtAmount = dto.GrtAmount.Value;
        if (dto.GrtDateStart.HasValue) entity.GrtDateStart = dto.GrtDateStart.Value;
        if (dto.GrtDateEnd.HasValue) entity.GrtDateEnd = dto.GrtDateEnd.Value;

        // Replace cars if provided
        if (dto.Cars != null)
        {
            db.BankingTransactionDetails.RemoveRange(entity.Details);
            entity.Details.Clear();

            decimal sumCars = 0m;
            foreach (var c in dto.Cars)
            {
                var allocPct = c.AllocPercent > 0 ? c.AllocPercent : 100m;
                var allocAmt = c.AllocAmount ?? Math.Round(c.AmountActual * allocPct / 100m, 0);

                var d = new BankingTransactionDetail
                {
                    BankingTransactionId = entity.Id,
                    RQ_BankingTransNo = cleanNo,
                    CarId = c.CarId ?? $"CAR-{c.Vin[^6..]}",
                    Vin = c.Vin.Trim().ToUpperInvariant(),
                    ModelCode = c.ModelCode.Trim().ToUpperInvariant(),
                    ModelName = c.ModelName ?? c.ModelCode,
                    SpecCode = c.SpecCode,
                    SpecDescription = c.SpecDescription,
                    ColorCode = c.ColorCode,
                    DlrCtrNo = c.DlrCtrNo,
                    SOCode = c.SOCode,
                    HTCInvoiceNo = c.HTCInvoiceNo,
                    AmountActual = c.AmountActual,
                    AllocPercent = allocPct,
                    AllocAmount = allocAmt,
                    Remark = c.Remark
                };
                entity.Details.Add(d);
                sumCars += allocAmt;
            }

            entity.TotalCars = entity.Details.Count;
            entity.TotalAmount = sumCars > 0 ? sumCars : entity.TransferAmount;
        }

        // Replace attachments if provided
        if (dto.Attachments != null)
        {
            db.BankingTransactionAttachFiles.RemoveRange(entity.Attachments);
            entity.Attachments.Clear();

            int idx = 1;
            foreach (var a in dto.Attachments)
            {
                entity.Attachments.Add(new BankingTransactionAttachFile
                {
                    BankingTransactionId = entity.Id,
                    RQ_BankingTransNo = cleanNo,
                    FileIndex = idx++,
                    FileType = a.FileType,
                    FileName = a.FileName,
                    FilePath = a.FilePath,
                    FileUrl = a.FileUrl ?? a.FilePath,
                    FileSize = a.FileSize ?? 102400L,
                    Remark = a.Remark,
                    UploadedAt = DateTime.Now,
                    UploadedBy = dto.UpdatedBy ?? "DEALER_STAFF"
                });
            }
        }

        entity.LUDateTime = DateTime.Now;
        entity.LUBy = dto.UpdatedBy ?? "DEALER_STAFF";

        await db.SaveChangesAsync();

        return new
        {
            Success = true,
            Message = $"Cập nhật Đề nghị giao dịch ngân hàng '{cleanNo}' thành công.",
            RQ_BankingTransNo = cleanNo,
            entity.TotalAmount,
            entity.TotalCars
        };
    }

    public async Task<object?> PushBankAsync(string transNo, PushBankDto? dto)
    {
        var cleanNo = transNo.Trim().ToUpperInvariant();
        var entity = await db.BankingTransactions
            .Include(x => x.Details)
            .FirstOrDefaultAsync(x => x.OrgId == Org && x.RQ_BankingTransNo == cleanNo);

        if (entity == null) return null;

        if (entity.Status != BankingTransStatus.Draft && entity.Status != BankingTransStatus.Rejected)
            throw new InvalidOperationException($"Chỉ có thể đẩy sang ngân hàng khi đề nghị ở trạng thái Draft hoặc Rejected (Hiện tại: {entity.Status}).");

        if (entity.Details.Count == 0 && entity.TotalAmount <= 0)
            throw new InvalidOperationException("Hồ sơ đề nghị chưa có danh sách xe phân bổ hoặc tổng số tiền bằng 0.");

        entity.Status = BankingTransStatus.Pending;
        entity.BankStatus = BankingTransBankStatus.BankReceived;
        entity.PushedToBankAt = DateTime.Now;
        entity.PushedToBankBy = dto?.PushedBy ?? "DEALER_FINANCE";
        entity.RefBankCode = $"{entity.BankCode}-{DateTime.Now:yyMMddHHmm}-{new Random().Next(1000, 9999)}";
        entity.BankRemark = $"Ngân hàng {entity.BankName} đã tiếp nhận điện giao dịch qua Open Banking API lúc {DateTime.Now:dd/MM/yyyy HH:mm:ss}.";
        if (!string.IsNullOrWhiteSpace(dto?.Note))
            entity.Remark = string.IsNullOrWhiteSpace(entity.Remark) ? dto.Note : $"{entity.Remark} | {dto.Note}";

        entity.LUDateTime = DateTime.Now;
        entity.LUBy = dto?.PushedBy ?? "DEALER_FINANCE";

        await db.SaveChangesAsync();

        return new
        {
            Success = true,
            Message = $"Đã đẩy đề nghị giao dịch '{cleanNo}' sang Ngân hàng {entity.BankName} thành công.",
            RQ_BankingTransNo = cleanNo,
            Status = entity.Status.ToString(),
            BankStatus = entity.BankStatus.ToString(),
            entity.RefBankCode,
            entity.PushedToBankAt
        };
    }

    public async Task<object?> BankApproveAsync(string transNo, BankApproveDto dto)
    {
        var cleanNo = transNo.Trim().ToUpperInvariant();
        var entity = await db.BankingTransactions
            .FirstOrDefaultAsync(x => x.OrgId == Org && x.RQ_BankingTransNo == cleanNo);

        if (entity == null) return null;

        if (entity.Status != BankingTransStatus.Pending)
            throw new InvalidOperationException($"Chỉ thẩm định phê duyệt khi đề nghị đang ở trạng thái Pending (Hiện tại: {entity.Status}).");

        entity.BankStatus = BankingTransBankStatus.BankApproved;
        entity.BankApprovedAt = DateTime.Now;
        entity.BankApprovedBy = dto.BankApprovedBy ?? $"BANK_OFFICER_{entity.BankCode}";
        entity.BankRemark = dto.BankRemark ?? $"Ngân hàng {entity.BankName} đã thẩm định đủ điều kiện cấp tín dụng/bảo lãnh.";

        if (entity.TransType == BankingTransType.PMT)
        {
            entity.LDNo = !string.IsNullOrWhiteSpace(dto.LDNo)
                ? dto.LDNo.Trim().ToUpperInvariant()
                : $"LD{DateTime.Now:yyMM}-{entity.BankCode}-{new Random().Next(1000, 9999)}";
        }
        else // GRT or GRT_LC
        {
            entity.MDNo = !string.IsNullOrWhiteSpace(dto.MDNo)
                ? dto.MDNo.Trim().ToUpperInvariant()
                : $"MD{DateTime.Now:yyMM}-{entity.BankCode}-{new Random().Next(1000, 9999)}";
            entity.Status = BankingTransStatus.Approved; // Bảo lãnh duyệt là đã có hiệu lực
        }

        entity.LUDateTime = DateTime.Now;
        entity.LUBy = entity.BankApprovedBy;

        await db.SaveChangesAsync();

        return new
        {
            Success = true,
            Message = $"Ngân hàng {entity.BankName} đã phê duyệt đề nghị giao dịch '{cleanNo}'.",
            RQ_BankingTransNo = cleanNo,
            Status = entity.Status.ToString(),
            BankStatus = entity.BankStatus.ToString(),
            entity.LDNo,
            entity.MDNo,
            entity.BankApprovedAt
        };
    }

    public async Task<object?> DisburseAsync(string transNo, DisburseBankingTransDto dto)
    {
        var cleanNo = transNo.Trim().ToUpperInvariant();
        var entity = await db.BankingTransactions
            .FirstOrDefaultAsync(x => x.OrgId == Org && x.RQ_BankingTransNo == cleanNo);

        if (entity == null) return null;

        if (entity.BankStatus != BankingTransBankStatus.BankApproved && entity.Status != BankingTransStatus.Pending && entity.Status != BankingTransStatus.Approved)
            throw new InvalidOperationException($"Chỉ có thể giải ngân khi ngân hàng đã phê duyệt (BankStatus: {entity.BankStatus}).");

        decimal disburseAmt = dto.DisbursementAmount ?? (entity.TransferAmount > 0 ? entity.TransferAmount : entity.TotalAmount);
        DateTime disburseDate = dto.DisbursementDate ?? DateTime.Today;

        entity.DisbursementAmount = disburseAmt;
        entity.DisbursementDate = disburseDate;
        entity.DisbursedAt = DateTime.Now;
        entity.DisbursedBy = dto.DisbursedBy ?? $"BANK_TELLER_{entity.BankCode}";
        entity.Status = BankingTransStatus.Disbursed;
        entity.BankStatus = BankingTransBankStatus.Disbursed;

        if (!string.IsNullOrWhiteSpace(dto.LDNo))
            entity.LDNo = dto.LDNo.Trim().ToUpperInvariant();

        if (!string.IsNullOrWhiteSpace(dto.BankRemark))
            entity.BankRemark = dto.BankRemark;
        else
            entity.BankRemark = $"Đã giải ngân thành công số tiền {disburseAmt:N0} VNĐ vào tài khoản NPP tại {entity.BankNameReceive} lúc {DateTime.Now:dd/MM/yyyy HH:mm}.";

        entity.LUDateTime = DateTime.Now;
        entity.LUBy = entity.DisbursedBy;

        await db.SaveChangesAsync();

        return new
        {
            Success = true,
            Message = $"Giải ngân thanh toán thành công cho đề nghị '{cleanNo}'.",
            RQ_BankingTransNo = cleanNo,
            Status = entity.Status.ToString(),
            BankStatus = entity.BankStatus.ToString(),
            entity.DisbursementAmount,
            entity.DisbursementDate,
            entity.LDNo,
            entity.DisbursedAt
        };
    }

    public async Task<object?> BankRejectAsync(string transNo, BankRejectDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Reason))
            throw new InvalidOperationException("Lý do ngân hàng từ chối không được để trống.");

        var cleanNo = transNo.Trim().ToUpperInvariant();
        var entity = await db.BankingTransactions
            .FirstOrDefaultAsync(x => x.OrgId == Org && x.RQ_BankingTransNo == cleanNo);

        if (entity == null) return null;

        if (entity.Status != BankingTransStatus.Pending)
            throw new InvalidOperationException($"Chỉ có thể từ chối khi đề nghị đang chờ xử lý (Hiện tại: {entity.Status}).");

        entity.Status = BankingTransStatus.Rejected;
        entity.BankStatus = BankingTransBankStatus.BankRejected;
        entity.RejectedAt = DateTime.Now;
        entity.RejectedBy = dto.BankRejectedBy ?? $"BANK_OFFICER_{entity.BankCode}";
        entity.RejectReason = dto.Reason;
        entity.BankRemark = $"Ngân hàng từ chối duyệt: {dto.Reason}";

        entity.LUDateTime = DateTime.Now;
        entity.LUBy = entity.RejectedBy;

        await db.SaveChangesAsync();

        return new
        {
            Success = true,
            Message = $"Ngân hàng đã từ chối đề nghị giao dịch '{cleanNo}'.",
            RQ_BankingTransNo = cleanNo,
            Status = entity.Status.ToString(),
            BankStatus = entity.BankStatus.ToString(),
            entity.RejectReason,
            entity.RejectedAt
        };
    }

    public async Task<object?> CancelAsync(string transNo, CancelBankingTransDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Reason))
            throw new InvalidOperationException("Lý do hủy đề nghị không được để trống.");

        var cleanNo = transNo.Trim().ToUpperInvariant();
        var entity = await db.BankingTransactions
            .FirstOrDefaultAsync(x => x.OrgId == Org && x.RQ_BankingTransNo == cleanNo);

        if (entity == null) return null;

        if (entity.Status == BankingTransStatus.Disbursed)
            throw new InvalidOperationException("Không thể hủy đề nghị giao dịch đã được ngân hàng giải ngân hoàn tất.");

        if (entity.Status == BankingTransStatus.Cancelled)
            throw new InvalidOperationException("Đề nghị giao dịch này đã bị hủy trước đó.");

        entity.Status = BankingTransStatus.Cancelled;
        entity.CancelledAt = DateTime.Now;
        entity.CancelledBy = dto.CancelledBy ?? "DEALER_STAFF";
        entity.CancelReason = dto.Reason;

        entity.LUDateTime = DateTime.Now;
        entity.LUBy = entity.CancelledBy;

        await db.SaveChangesAsync();

        return new
        {
            Success = true,
            Message = $"Hủy Đề nghị giao dịch ngân hàng '{cleanNo}' thành công.",
            RQ_BankingTransNo = cleanNo,
            Status = entity.Status.ToString(),
            entity.CancelReason,
            entity.CancelledAt
        };
    }

    public async Task<object?> DeleteDraftAsync(string transNo)
    {
        var cleanNo = transNo.Trim().ToUpperInvariant();
        var entity = await db.BankingTransactions
            .Include(x => x.Details)
            .Include(x => x.Attachments)
            .FirstOrDefaultAsync(x => x.OrgId == Org && x.RQ_BankingTransNo == cleanNo);

        if (entity == null) return null;

        if (entity.Status != BankingTransStatus.Draft)
            throw new InvalidOperationException($"Chỉ được phép xóa đề nghị khi ở trạng thái Draft nháp (Hiện tại: {entity.Status}).");

        db.BankingTransactions.Remove(entity);
        await db.SaveChangesAsync();

        return new
        {
            Success = true,
            Message = $"Đã xóa đề nghị nháp '{cleanNo}' khỏi hệ thống."
        };
    }

    public async Task<object?> AddCarAsync(string transNo, AddBankingTransCarDto dto)
    {
        var cleanNo = transNo.Trim().ToUpperInvariant();
        var entity = await db.BankingTransactions
            .Include(x => x.Details)
            .FirstOrDefaultAsync(x => x.OrgId == Org && x.RQ_BankingTransNo == cleanNo);

        if (entity == null) return null;

        if (entity.Status != BankingTransStatus.Draft)
            throw new InvalidOperationException($"Chỉ có thể thêm xe khi đề nghị ở trạng thái Draft (Hiện tại: {entity.Status}).");

        if (string.IsNullOrWhiteSpace(dto.Vin))
            throw new InvalidOperationException("Số khung xe (Vin) không được để trống.");

        var cleanVin = dto.Vin.Trim().ToUpperInvariant();
        if (entity.Details.Any(x => x.Vin == cleanVin))
            throw new InvalidOperationException($"Xe với số khung '{cleanVin}' đã có trong đề nghị giao dịch này.");

        var allocPct = dto.AllocPercent > 0 ? dto.AllocPercent : 100m;
        var allocAmt = dto.AllocAmount ?? Math.Round(dto.AmountActual * allocPct / 100m, 0);

        var detail = new BankingTransactionDetail
        {
            BankingTransactionId = entity.Id,
            RQ_BankingTransNo = cleanNo,
            CarId = dto.CarId ?? $"CAR-{cleanVin[^6..]}",
            Vin = cleanVin,
            ModelCode = dto.ModelCode.Trim().ToUpperInvariant(),
            ModelName = dto.ModelName ?? dto.ModelCode,
            SpecCode = dto.SpecCode,
            SpecDescription = dto.SpecDescription,
            ColorCode = dto.ColorCode,
            DlrCtrNo = dto.DlrCtrNo,
            SOCode = dto.SOCode,
            HTCInvoiceNo = dto.HTCInvoiceNo,
            AmountActual = dto.AmountActual,
            AllocPercent = allocPct,
            AllocAmount = allocAmt,
            Remark = dto.Remark
        };

        entity.Details.Add(detail);
        entity.TotalCars = entity.Details.Count;
        entity.TotalAmount = entity.Details.Sum(x => x.AllocAmount);
        if (entity.TransType == BankingTransType.PMT)
            entity.TransferAmount = entity.TotalAmount;
        else
            entity.GrtAmount = entity.TotalAmount;

        entity.LUDateTime = DateTime.Now;
        entity.LUBy = "DEALER_STAFF";

        await db.SaveChangesAsync();

        return new
        {
            Success = true,
            Message = $"Đã thêm xe '{cleanVin}' vào đề nghị giao dịch '{cleanNo}'.",
            DetailId = detail.Id,
            entity.TotalCars,
            entity.TotalAmount
        };
    }

    public async Task<object?> RemoveCarAsync(string transNo, long detailId)
    {
        var cleanNo = transNo.Trim().ToUpperInvariant();
        var entity = await db.BankingTransactions
            .Include(x => x.Details)
            .FirstOrDefaultAsync(x => x.OrgId == Org && x.RQ_BankingTransNo == cleanNo);

        if (entity == null) return null;

        if (entity.Status != BankingTransStatus.Draft)
            throw new InvalidOperationException($"Chỉ có thể xóa xe khi đề nghị ở trạng thái Draft (Hiện tại: {entity.Status}).");

        var detail = entity.Details.FirstOrDefault(x => x.Id == detailId);
        if (detail == null)
            throw new InvalidOperationException($"Không tìm thấy dòng xe với ID {detailId} trong đề nghị '{cleanNo}'.");

        entity.Details.Remove(detail);
        db.BankingTransactionDetails.Remove(detail);

        entity.TotalCars = entity.Details.Count;
        entity.TotalAmount = entity.Details.Sum(x => x.AllocAmount);
        if (entity.TransType == BankingTransType.PMT)
            entity.TransferAmount = entity.TotalAmount;
        else
            entity.GrtAmount = entity.TotalAmount;

        entity.LUDateTime = DateTime.Now;
        entity.LUBy = "DEALER_STAFF";

        await db.SaveChangesAsync();

        return new
        {
            Success = true,
            Message = $"Đã gỡ xe khỏi đề nghị giao dịch '{cleanNo}'.",
            entity.TotalCars,
            entity.TotalAmount
        };
    }

    public async Task<object?> AddAttachmentAsync(string transNo, AddBankingTransAttachmentDto dto)
    {
        var cleanNo = transNo.Trim().ToUpperInvariant();
        var entity = await db.BankingTransactions
            .Include(x => x.Attachments)
            .FirstOrDefaultAsync(x => x.OrgId == Org && x.RQ_BankingTransNo == cleanNo);

        if (entity == null) return null;

        int nextIdx = (entity.Attachments.MaxBy(x => x.FileIndex)?.FileIndex ?? 0) + 1;
        var att = new BankingTransactionAttachFile
        {
            BankingTransactionId = entity.Id,
            RQ_BankingTransNo = cleanNo,
            FileIndex = nextIdx,
            FileType = dto.FileType,
            FileName = dto.FileName,
            FilePath = dto.FilePath,
            FileUrl = dto.FileUrl ?? dto.FilePath,
            FileSize = dto.FileSize ?? 102400L,
            Remark = dto.Remark,
            UploadedAt = DateTime.Now,
            UploadedBy = dto.UploadedBy ?? "DEALER_STAFF"
        };

        entity.Attachments.Add(att);
        entity.LUDateTime = DateTime.Now;
        entity.LUBy = dto.UploadedBy ?? "DEALER_STAFF";

        await db.SaveChangesAsync();

        return new
        {
            Success = true,
            Message = $"Đã đính kèm tài liệu '{dto.FileName}' vào đề nghị '{cleanNo}'.",
            AttachmentId = att.Id,
            TotalAttachments = entity.Attachments.Count
        };
    }

    public async Task<object?> SignBankFileAsync(string transNo, SignBankingTransFileDto dto)
    {
        var cleanNo = transNo.Trim().ToUpperInvariant();
        var entity = await db.BankingTransactions
            .Include(x => x.Attachments)
            .FirstOrDefaultAsync(x => x.OrgId == Org && x.RQ_BankingTransNo == cleanNo);

        if (entity == null) return null;

        var att = entity.Attachments.FirstOrDefault(x => x.Id == dto.FileId);
        if (att == null)
            throw new InvalidOperationException($"Không tìm thấy tài liệu ID {dto.FileId} trong đề nghị '{cleanNo}'.");

        att.FlagSigned = true;
        att.SerialNumber = dto.SerialNumber;
        att.CaSubject = dto.CaSubject;
        att.SignedAt = DateTime.Now;
        att.SignedBy = dto.SignedBy ?? "CHỦ TỊCH / GIÁM ĐỐC ĐẠI LÝ";
        if (!string.IsNullOrWhiteSpace(dto.Note))
            att.Remark = string.IsNullOrWhiteSpace(att.Remark) ? dto.Note : $"{att.Remark} | {dto.Note}";

        entity.LUDateTime = DateTime.Now;
        entity.LUBy = att.SignedBy;

        await db.SaveChangesAsync();

        return new
        {
            Success = true,
            Message = $"Ký số điện tử thành công tài liệu '{att.FileName}'.",
            AttachmentId = att.Id,
            att.FlagSigned,
            att.SerialNumber,
            att.SignedAt,
            att.SignedBy
        };
    }

    public async Task<object> GetEligibleCarsAsync(string? dealerCode, string? contractNo)
    {
        var dQuery = db.DealerContracts
            .AsNoTracking()
            .Where(x => x.OrgId == Org && (x.Status == DealerContractStatus.Signed || x.Status == DealerContractStatus.Adjusted));

        if (!string.IsNullOrWhiteSpace(dealerCode))
        {
            var dc = dealerCode.Trim().ToUpperInvariant();
            dQuery = dQuery.Where(x => x.DealerCode == dc);
        }

        if (!string.IsNullOrWhiteSpace(contractNo))
        {
            var cn = contractNo.Trim().ToUpperInvariant();
            dQuery = dQuery.Where(x => x.ContractNo == cn);
        }

        var contracts = await dQuery
            .Include(x => x.Details)
            .OrderByDescending(x => x.ContractDate)
            .Take(20)
            .ToListAsync();

        var eligibleCars = new List<object>();

        foreach (var c in contracts)
        {
            foreach (var d in c.Details)
            {
                eligibleCars.Add(new
                {
                    c.ContractNo,
                    c.DealerCode,
                    c.DealerName,
                    c.PaymentType,
                    c.BankCode,
                    d.CarId,
                    d.Vin,
                    d.Model,
                    d.SpecCode,
                    d.ColorCode,
                    d.UnitPrice,
                    d.TotalAmount,
                    d.OrderNo
                });
            }
        }

        return new
        {
            Total = eligibleCars.Count,
            EligibleCars = eligibleCars
        };
    }

    public async Task<object> GetStatsAsync()
    {
        var all = await db.BankingTransactions
            .AsNoTracking()
            .Where(x => x.OrgId == Org)
            .ToListAsync();

        var byStatus = all
            .GroupBy(x => x.Status.ToString())
            .ToDictionary(g => g.Key, g => g.Count());

        var byBankStatus = all
            .GroupBy(x => x.BankStatus.ToString())
            .ToDictionary(g => g.Key, g => g.Count());

        var byTransType = all
            .GroupBy(x => x.TransType.ToString())
            .ToDictionary(g => g.Key, g => new
            {
                Count = g.Count(),
                TotalAmount = g.Sum(x => x.TotalAmount)
            });

        var byBank = all
            .GroupBy(x => x.BankCode)
            .ToDictionary(g => g.Key, g => new
            {
                BankName = g.First().BankName,
                Count = g.Count(),
                TotalAmount = g.Sum(x => x.TotalAmount),
                DisbursedAmount = g.Where(x => x.Status == BankingTransStatus.Disbursed).Sum(x => x.DisbursementAmount)
            });

        return new
        {
            TotalTransactions = all.Count,
            TotalRequestedAmount = all.Sum(x => x.TotalAmount),
            TotalDisbursedAmount = all.Where(x => x.Status == BankingTransStatus.Disbursed).Sum(x => x.DisbursementAmount),
            TotalGuaranteeAmount = all.Where(x => x.TransType == BankingTransType.GRT || x.TransType == BankingTransType.GRT_LC).Sum(x => x.GrtAmount),
            ByStatus = byStatus,
            ByBankStatus = byBankStatus,
            ByTransType = byTransType,
            ByBank = byBank
        };
    }
}
