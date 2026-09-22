using Microsoft.EntityFrameworkCore;
using MiniSales.Data;
using MiniSales.Models;

namespace MiniSales.Services;

public record BankBillMinutesCarInputDto(
    string Vin,
    string? CarId = null,
    string? Model = null,
    string? ModelCode = null,
    string? SpecCode = null,
    string? SpecDescription = null,
    string? ColorCode = null,
    string? EngineNo = null,
    string? ContractNo = null,
    string? BankGuaranteeNo = null,
    string? GuaranteeBankCode = null,
    DateTime? GuaranteeDateOpen = null,
    string? HTCInvoiceNo = null,
    string? TCGInvoiceNo = null,
    string? InvoiceNoFactory = null,
    string? TransportMinutesNo = null,
    string? CQNo = null,
    string? CONo = null,
    string? CabinCONo = null,
    string? DeclarationNo = null,
    decimal? ClaimAmount = null,
    string? Remark = null
);

public record CreateBankBillMinutesDto(
    string? BankBillMnNo,
    string BankCode,
    string? BankName,
    string DealerCode,
    string? DealerName,
    DateTime? BankBillDate,
    DateTime? BankBillReceiveDate,
    DateTime? BankBillPrintDate,
    string? BankOfficer,
    string? HTCOfficer,
    string? Remark,
    string? CreatedBy,
    List<BankBillMinutesCarInputDto>? Items = null,
    List<string>? Vins = null
);

public record UpdateBankBillMinutesDto(
    DateTime? BankBillDate,
    DateTime? BankBillReceiveDate,
    DateTime? BankBillPrintDate,
    string? BankOfficer,
    string? HTCOfficer,
    string? Remark,
    string? UpdatedBy
);

public record HandoverBankBillMinutesDto(
    string? HandoverBy,
    string? HTCOfficer,
    string? Remark
);

public record BankReceiveBankBillMinutesDto(
    string? BankReceivedBy,
    string? BankOfficer,
    DateTime? BankBillReceiveDate,
    string? Remark
);

public record SettleBankBillMinutesDto(
    string? SettledBy,
    string? Remark
);

public record CancelBankBillMinutesDto(
    string CancelReason,
    string? CancelledBy
);

public record AddBankBillCarDto(
    string Vin,
    string? CarId = null,
    string? Model = null,
    string? ModelCode = null,
    string? SpecCode = null,
    string? SpecDescription = null,
    string? ColorCode = null,
    string? EngineNo = null,
    string? ContractNo = null,
    string? BankGuaranteeNo = null,
    string? GuaranteeBankCode = null,
    DateTime? GuaranteeDateOpen = null,
    string? HTCInvoiceNo = null,
    string? TCGInvoiceNo = null,
    string? InvoiceNoFactory = null,
    string? TransportMinutesNo = null,
    string? CQNo = null,
    string? CONo = null,
    string? CabinCONo = null,
    string? DeclarationNo = null,
    decimal? ClaimAmount = null,
    string? Remark = null
);

public record BankBillMinutesStatsDto(
    int TotalMinutes,
    int DraftCount,
    int HandoverCount,
    int BankReceivedCount,
    int SettledCount,
    int CancelledCount,
    int TotalCars,
    decimal TotalClaimAmount,
    int DistinctBanksCount,
    int DistinctDealersCount
);

public interface IBankBillMinutesService
{
    Task<object> CreateAsync(CreateBankBillMinutesDto dto);
    Task<object> ListAsync(string? status, string? bankCode, string? dealerCode, string? guaranteeNo, string? vin, string? bankBillMnNo, DateTime? dateFrom, DateTime? dateTo);
    Task<object?> DetailAsync(string bankBillMnNo);
    Task<object?> UpdateAsync(string bankBillMnNo, UpdateBankBillMinutesDto dto);
    Task<object?> DeleteDraftAsync(string bankBillMnNo);
    Task<object?> HandoverAsync(string bankBillMnNo, HandoverBankBillMinutesDto? dto);
    Task<object?> BankReceiveAsync(string bankBillMnNo, BankReceiveBankBillMinutesDto? dto);
    Task<object?> SettleAsync(string bankBillMnNo, SettleBankBillMinutesDto? dto);
    Task<object?> CancelAsync(string bankBillMnNo, CancelBankBillMinutesDto dto);
    Task<object?> AddCarsAsync(string bankBillMnNo, List<AddBankBillCarDto> items);
    Task<object?> RemoveCarAsync(string bankBillMnNo, string vin);
    Task<object> GetEligibleCarsAsync(string? dealerCode, string? bankCode);
    Task<object> StatsAsync();
}

public sealed class BankBillMinutesService(AppDbContext db, ITenantContext tenant) : IBankBillMinutesService
{
    private Guid Org => tenant.OrgId;

    public async Task<object> CreateAsync(CreateBankBillMinutesDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.BankCode))
            throw new InvalidOperationException("BankCode (ngân hàng nhận hối phiếu) không được để trống.");
        if (string.IsNullOrWhiteSpace(dto.DealerCode))
            throw new InvalidOperationException("DealerCode (mã đại lý ký phát) không được để trống.");

        var bankCode = dto.BankCode.Trim().ToUpperInvariant();
        var dealerCode = dto.DealerCode.Trim().ToUpperInvariant();

        // 1. Tự động sinh hoặc chuẩn hóa số BankBillMnNo (format: yyMMBBM{seq:D5})
        string mnNo;
        if (!string.IsNullOrWhiteSpace(dto.BankBillMnNo))
        {
            mnNo = dto.BankBillMnNo.Trim().ToUpperInvariant();
        }
        else
        {
            var prefix = DateTime.Now.ToString("yyMM") + "BBM";
            var existingCount = await db.BankBillMinutes.CountAsync(m => m.OrgId == Org && m.BankBillMnNo.StartsWith(prefix));
            mnNo = $"{prefix}{(existingCount + 1):D5}";
        }

        if (await db.BankBillMinutes.AnyAsync(m => m.OrgId == Org && m.BankBillMnNo == mnNo))
            throw new InvalidOperationException($"Số biên bản bàn giao hối phiếu {mnNo} đã tồn tại trong hệ thống.");

        // 2. Thu thập danh sách xe / VIN
        var itemsInput = new List<BankBillMinutesCarInputDto>();
        if (dto.Items != null && dto.Items.Count > 0)
        {
            itemsInput.AddRange(dto.Items);
        }
        else if (dto.Vins != null && dto.Vins.Count > 0)
        {
            itemsInput.AddRange(dto.Vins.Select(v => new BankBillMinutesCarInputDto(v.Trim().ToUpperInvariant())));
        }

        var header = new BankBillMinutes
        {
            OrgId = Org,
            BankBillMnNo = mnNo,
            BankCode = bankCode,
            BankName = dto.BankName?.Trim() ?? GetBankNameDefault(bankCode),
            DealerCode = dealerCode,
            DealerName = dto.DealerName?.Trim() ?? GetDealerNameDefault(dealerCode),
            BankBillDate = dto.BankBillDate ?? DateTime.Today,
            BankBillReceiveDate = dto.BankBillReceiveDate,
            BankBillPrintDate = dto.BankBillPrintDate,
            Status = BankBillMinutesStatus.Draft,
            BankOfficer = dto.BankOfficer?.Trim(),
            HTCOfficer = dto.HTCOfficer?.Trim(),
            Remark = dto.Remark?.Trim(),
            CreatedBy = dto.CreatedBy?.Trim() ?? "HTV_FINANCE",
            CreatedAt = DateTime.Now
        };

        db.BankBillMinutes.Add(header);
        await db.SaveChangesAsync();

        var details = new List<BankBillMinutesDetail>();
        decimal sumClaim = 0;

        foreach (var item in itemsInput)
        {
            if (string.IsNullOrWhiteSpace(item.Vin)) continue;
            var vin = item.Vin.Trim().ToUpperInvariant();
            if (details.Any(d => d.Vin == vin)) continue; // Tránh trùng lặp trong cùng biên bản

            // Tra cứu thông tin xe từ hợp đồng bán buôn, bảo lãnh ngân hàng, hóa đơn HTC và BBBG đã có
            var carInfo = await FindVehicleContextAsync(vin);

            var carId = !string.IsNullOrWhiteSpace(item.CarId) ? item.CarId.Trim() : carInfo.CarId ?? ("CAR-" + vin[^6..]);
            var model = !string.IsNullOrWhiteSpace(item.Model) ? item.Model.Trim() : carInfo.Model ?? "Hyundai Model";
            var modelCode = item.ModelCode ?? carInfo.ModelCode;
            var specCode = item.SpecCode ?? carInfo.SpecCode;
            var specDesc = item.SpecDescription ?? carInfo.SpecDescription ?? model;
            var colorCode = item.ColorCode ?? carInfo.ColorCode;
            var engineNo = item.EngineNo ?? carInfo.EngineNo ?? ("ENG-" + vin[^6..]);
            var contractNo = item.ContractNo ?? carInfo.ContractNo;
            var bgNo = item.BankGuaranteeNo ?? carInfo.BankGuaranteeNo ?? ("BL-" + bankCode + "-" + vin[^4..]);
            var grtBankCode = item.GuaranteeBankCode ?? carInfo.GuaranteeBankCode ?? bankCode;
            var grtDateOpen = item.GuaranteeDateOpen ?? carInfo.GuaranteeDateOpen ?? DateTime.Today.AddDays(-15);
            var htcInvNo = item.HTCInvoiceNo ?? carInfo.HTCInvoiceNo ?? ("HD-HTC-" + DateTime.Now.ToString("yyMM") + "-" + vin[^4..]);
            var tcgInvNo = item.TCGInvoiceNo ?? carInfo.TCGInvoiceNo;
            var invFactory = item.InvoiceNoFactory ?? carInfo.InvoiceNoFactory;
            var tmNo = item.TransportMinutesNo ?? carInfo.TransportMinutesNo ?? ("BBBG-" + DateTime.Now.ToString("yyMM") + "-" + vin[^4..]);
            var cqNo = item.CQNo ?? carInfo.CQNo ?? ("CQ-" + vin[^6..]);
            var coNo = item.CONo ?? carInfo.CONo ?? ("CO-" + vin[^6..]);
            var cabinCoNo = item.CabinCONo ?? carInfo.CabinCONo;
            var decNo = item.DeclarationNo ?? carInfo.DeclarationNo;
            var claimAmt = item.ClaimAmount ?? (carInfo.ClaimAmount > 0 ? carInfo.ClaimAmount : 650000000m);

            var line = new BankBillMinutesDetail
            {
                BankBillMinutesId = header.Id,
                BankBillMnNo = header.BankBillMnNo,
                CarId = carId,
                Vin = vin,
                Model = model,
                ModelCode = modelCode,
                SpecCode = specCode,
                SpecDescription = specDesc,
                ColorCode = colorCode,
                EngineNo = engineNo,
                ContractNo = contractNo,
                BankGuaranteeNo = bgNo,
                GuaranteeBankCode = grtBankCode,
                GuaranteeDateOpen = grtDateOpen,
                HTCInvoiceNo = htcInvNo,
                TCGInvoiceNo = tcgInvNo,
                InvoiceNoFactory = invFactory,
                TransportMinutesNo = tmNo,
                CQNo = cqNo,
                CONo = coNo,
                CabinCONo = cabinCoNo,
                DeclarationNo = decNo,
                ClaimAmount = claimAmt,
                Status = BankBillMinutesDetailStatus.Pending,
                Remark = item.Remark?.Trim()
            };

            details.Add(line);
            sumClaim += claimAmt;
        }

        if (details.Count > 0)
        {
            db.BankBillMinutesDetails.AddRange(details);
            header.TotalCars = details.Count;
            header.TotalClaimAmount = sumClaim;
            await db.SaveChangesAsync();
        }

        return new
        {
            header.Id,
            header.BankBillMnNo,
            header.BankCode,
            header.BankName,
            header.DealerCode,
            header.DealerName,
            header.BankBillDate,
            header.Status,
            header.TotalCars,
            header.TotalClaimAmount,
            header.Remark,
            header.CreatedAt,
            Details = details.Select(FormatDetail).ToList()
        };
    }

    public async Task<object> ListAsync(string? status, string? bankCode, string? dealerCode, string? guaranteeNo, string? vin, string? bankBillMnNo, DateTime? dateFrom, DateTime? dateTo)
    {
        var q = db.BankBillMinutes.Where(m => m.OrgId == Org);

        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<BankBillMinutesStatus>(status, true, out var st))
            q = q.Where(m => m.Status == st);

        if (!string.IsNullOrWhiteSpace(bankCode))
        {
            var b = bankCode.Trim().ToUpperInvariant();
            q = q.Where(m => m.BankCode == b);
        }

        if (!string.IsNullOrWhiteSpace(dealerCode))
        {
            var d = dealerCode.Trim().ToUpperInvariant();
            q = q.Where(m => m.DealerCode == d);
        }

        if (!string.IsNullOrWhiteSpace(bankBillMnNo))
        {
            var no = bankBillMnNo.Trim().ToUpperInvariant();
            q = q.Where(m => m.BankBillMnNo.Contains(no));
        }

        if (dateFrom.HasValue)
            q = q.Where(m => m.BankBillDate >= dateFrom.Value.Date);

        if (dateTo.HasValue)
            q = q.Where(m => m.BankBillDate <= dateTo.Value.Date.AddDays(1).AddTicks(-1));

        if (!string.IsNullOrWhiteSpace(guaranteeNo))
        {
            var g = guaranteeNo.Trim().ToUpperInvariant();
            var ids = await db.BankBillMinutesDetails
                .Where(d => d.BankGuaranteeNo != null && d.BankGuaranteeNo.Contains(g))
                .Select(d => d.BankBillMinutesId)
                .Distinct()
                .ToListAsync();
            q = q.Where(m => ids.Contains(m.Id));
        }

        if (!string.IsNullOrWhiteSpace(vin))
        {
            var v = vin.Trim().ToUpperInvariant();
            var ids = await db.BankBillMinutesDetails
                .Where(d => d.Vin.Contains(v))
                .Select(d => d.BankBillMinutesId)
                .Distinct()
                .ToListAsync();
            q = q.Where(m => ids.Contains(m.Id));
        }

        var list = await q.OrderByDescending(m => m.CreatedAt)
            .Select(m => new
            {
                m.Id,
                m.BankBillMnNo,
                m.BankCode,
                m.BankName,
                m.DealerCode,
                m.DealerName,
                m.BankBillDate,
                m.BankBillReceiveDate,
                m.BankBillPrintDate,
                Status = m.Status.ToString(),
                m.TotalCars,
                m.TotalClaimAmount,
                m.BankOfficer,
                m.HTCOfficer,
                m.Remark,
                m.CancelReason,
                m.CancelledAt,
                m.CancelledBy,
                m.HandoverAt,
                m.HandoverBy,
                m.BankReceivedAt,
                m.BankReceivedBy,
                m.SettledAt,
                m.SettledBy,
                m.CreatedBy,
                m.CreatedAt,
                CarVins = db.BankBillMinutesDetails
                    .Where(d => d.BankBillMinutesId == m.Id)
                    .Select(d => d.Vin)
                    .ToList()
            }).ToListAsync();

        return list;
    }

    public async Task<object?> DetailAsync(string bankBillMnNo)
    {
        var mnNo = bankBillMnNo.Trim().ToUpperInvariant();
        var m = await db.BankBillMinutes
            .Include(x => x.Details)
            .FirstOrDefaultAsync(x => x.OrgId == Org && x.BankBillMnNo == mnNo);

        if (m is null) return null;

        return new
        {
            m.Id,
            m.BankBillMnNo,
            m.BankCode,
            m.BankName,
            m.DealerCode,
            m.DealerName,
            m.BankBillDate,
            m.BankBillReceiveDate,
            m.BankBillPrintDate,
            Status = m.Status.ToString(),
            m.TotalCars,
            m.TotalClaimAmount,
            m.BankOfficer,
            m.HTCOfficer,
            m.Remark,
            m.CancelReason,
            m.CancelledAt,
            m.CancelledBy,
            m.HandoverAt,
            m.HandoverBy,
            m.BankReceivedAt,
            m.BankReceivedBy,
            m.SettledAt,
            m.SettledBy,
            m.CreatedBy,
            m.CreatedAt,
            m.LUDateTime,
            m.LUBy,
            Details = m.Details.Select(FormatDetail).ToList()
        };
    }

    public async Task<object?> UpdateAsync(string bankBillMnNo, UpdateBankBillMinutesDto dto)
    {
        var mnNo = bankBillMnNo.Trim().ToUpperInvariant();
        var m = await db.BankBillMinutes
            .Include(x => x.Details)
            .FirstOrDefaultAsync(x => x.OrgId == Org && x.BankBillMnNo == mnNo);

        if (m is null) return null;

        if (m.Status != BankBillMinutesStatus.Draft)
            throw new InvalidOperationException($"Biên bản {mnNo} đang ở trạng thái {m.Status}, chỉ được chỉnh sửa khi ở trạng thái Draft.");

        if (dto.BankBillDate.HasValue) m.BankBillDate = dto.BankBillDate.Value;
        if (dto.BankBillReceiveDate.HasValue) m.BankBillReceiveDate = dto.BankBillReceiveDate.Value;
        if (dto.BankBillPrintDate.HasValue) m.BankBillPrintDate = dto.BankBillPrintDate.Value;
        if (!string.IsNullOrWhiteSpace(dto.BankOfficer)) m.BankOfficer = dto.BankOfficer.Trim();
        if (!string.IsNullOrWhiteSpace(dto.HTCOfficer)) m.HTCOfficer = dto.HTCOfficer.Trim();
        if (dto.Remark != null) m.Remark = dto.Remark.Trim();

        m.LUDateTime = DateTime.Now;
        m.LUBy = dto.UpdatedBy?.Trim() ?? "HTV_FINANCE";

        await db.SaveChangesAsync();
        return await DetailAsync(mnNo);
    }

    public async Task<object?> DeleteDraftAsync(string bankBillMnNo)
    {
        var mnNo = bankBillMnNo.Trim().ToUpperInvariant();
        var m = await db.BankBillMinutes
            .Include(x => x.Details)
            .FirstOrDefaultAsync(x => x.OrgId == Org && x.BankBillMnNo == mnNo);

        if (m is null) return null;

        if (m.Status != BankBillMinutesStatus.Draft)
            throw new InvalidOperationException($"Không thể xóa biên bản {mnNo} ở trạng thái {m.Status}. Chỉ biên bản nháp (Draft) mới được phép xóa.");

        db.BankBillMinutes.Remove(m);
        await db.SaveChangesAsync();

        return new { success = true, deletedBankBillMnNo = mnNo };
    }

    public async Task<object?> HandoverAsync(string bankBillMnNo, HandoverBankBillMinutesDto? dto)
    {
        var mnNo = bankBillMnNo.Trim().ToUpperInvariant();
        var m = await db.BankBillMinutes
            .Include(x => x.Details)
            .FirstOrDefaultAsync(x => x.OrgId == Org && x.BankBillMnNo == mnNo);

        if (m is null) return null;

        if (m.Status != BankBillMinutesStatus.Draft)
            throw new InvalidOperationException($"Biên bản {mnNo} đang ở trạng thái {m.Status}. Chỉ biên bản nháp (Draft) mới được bàn giao sang Ngân hàng.");

        if (m.Details.Count == 0)
            throw new InvalidOperationException($"Biên bản {mnNo} chưa có danh sách xe bàn giao.");

        m.Status = BankBillMinutesStatus.Handover;
        m.HandoverAt = DateTime.Now;
        m.HandoverBy = dto?.HandoverBy?.Trim() ?? "HTV_FINANCE";
        if (!string.IsNullOrWhiteSpace(dto?.HTCOfficer)) m.HTCOfficer = dto.HTCOfficer.Trim();
        if (!string.IsNullOrWhiteSpace(dto?.Remark)) m.Remark = dto.Remark.Trim();

        m.LUDateTime = DateTime.Now;
        m.LUBy = m.HandoverBy;

        await db.SaveChangesAsync();
        return await DetailAsync(mnNo);
    }

    public async Task<object?> BankReceiveAsync(string bankBillMnNo, BankReceiveBankBillMinutesDto? dto)
    {
        var mnNo = bankBillMnNo.Trim().ToUpperInvariant();
        var m = await db.BankBillMinutes
            .Include(x => x.Details)
            .FirstOrDefaultAsync(x => x.OrgId == Org && x.BankBillMnNo == mnNo);

        if (m is null) return null;

        if (m.Status != BankBillMinutesStatus.Handover)
            throw new InvalidOperationException($"Biên bản {mnNo} đang ở trạng thái {m.Status}. Phải bàn giao hồ sơ sang Ngân hàng (Handover) trước khi Ngân hàng ký tiếp nhận.");

        m.Status = BankBillMinutesStatus.BankReceived;
        m.BankReceivedAt = DateTime.Now;
        m.BankReceivedBy = dto?.BankReceivedBy?.Trim() ?? "BANK_OFFICER";
        if (!string.IsNullOrWhiteSpace(dto?.BankOfficer)) m.BankOfficer = dto.BankOfficer.Trim();
        m.BankBillReceiveDate = dto?.BankBillReceiveDate ?? DateTime.Today;
        if (!string.IsNullOrWhiteSpace(dto?.Remark)) m.Remark = dto.Remark.Trim();

        foreach (var d in m.Details)
        {
            if (d.Status == BankBillMinutesDetailStatus.Pending)
                d.Status = BankBillMinutesDetailStatus.Received;
        }

        m.LUDateTime = DateTime.Now;
        m.LUBy = m.BankReceivedBy;

        await db.SaveChangesAsync();
        return await DetailAsync(mnNo);
    }

    public async Task<object?> SettleAsync(string bankBillMnNo, SettleBankBillMinutesDto? dto)
    {
        var mnNo = bankBillMnNo.Trim().ToUpperInvariant();
        var m = await db.BankBillMinutes
            .Include(x => x.Details)
            .FirstOrDefaultAsync(x => x.OrgId == Org && x.BankBillMnNo == mnNo);

        if (m is null) return null;

        if (m.Status != BankBillMinutesStatus.BankReceived)
            throw new InvalidOperationException($"Biên bản {mnNo} đang ở trạng thái {m.Status}. Chỉ khi Ngân hàng đã tiếp nhận hồ sơ gốc (BankReceived) mới có thể thực hiện quyết toán chiết khấu / giải ngân.");

        m.Status = BankBillMinutesStatus.Settled;
        m.SettledAt = DateTime.Now;
        m.SettledBy = dto?.SettledBy?.Trim() ?? "HTV_ACCOUNTING";
        if (!string.IsNullOrWhiteSpace(dto?.Remark)) m.Remark = dto.Remark.Trim();

        foreach (var d in m.Details)
        {
            d.Status = BankBillMinutesDetailStatus.Settled;
        }

        m.LUDateTime = DateTime.Now;
        m.LUBy = m.SettledBy;

        await db.SaveChangesAsync();
        return await DetailAsync(mnNo);
    }

    public async Task<object?> CancelAsync(string bankBillMnNo, CancelBankBillMinutesDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.CancelReason))
            throw new InvalidOperationException("Lý do hủy biên bản bàn giao hối phiếu không được để trống.");

        var mnNo = bankBillMnNo.Trim().ToUpperInvariant();
        var m = await db.BankBillMinutes
            .Include(x => x.Details)
            .FirstOrDefaultAsync(x => x.OrgId == Org && x.BankBillMnNo == mnNo);

        if (m is null) return null;

        if (m.Status == BankBillMinutesStatus.Settled)
            throw new InvalidOperationException($"Biên bản {mnNo} đã quyết toán giải ngân hoàn tất (Settled), không thể hủy.");

        if (m.Status == BankBillMinutesStatus.Cancelled)
            throw new InvalidOperationException($"Biên bản {mnNo} đã ở trạng thái hủy (Cancelled).");

        m.Status = BankBillMinutesStatus.Cancelled;
        m.CancelReason = dto.CancelReason.Trim();
        m.CancelledAt = DateTime.Now;
        m.CancelledBy = dto.CancelledBy?.Trim() ?? "HTV_FINANCE";

        foreach (var d in m.Details)
        {
            d.Status = BankBillMinutesDetailStatus.Cancelled;
        }

        m.LUDateTime = DateTime.Now;
        m.LUBy = m.CancelledBy;

        await db.SaveChangesAsync();
        return await DetailAsync(mnNo);
    }

    public async Task<object?> AddCarsAsync(string bankBillMnNo, List<AddBankBillCarDto> items)
    {
        if (items is null || items.Count == 0)
            throw new InvalidOperationException("Danh sách xe bổ sung không được để trống.");

        var mnNo = bankBillMnNo.Trim().ToUpperInvariant();
        var m = await db.BankBillMinutes
            .Include(x => x.Details)
            .FirstOrDefaultAsync(x => x.OrgId == Org && x.BankBillMnNo == mnNo);

        if (m is null) return null;

        if (m.Status != BankBillMinutesStatus.Draft)
            throw new InvalidOperationException($"Biên bản {mnNo} đang ở trạng thái {m.Status}. Chỉ được bổ sung xe khi ở trạng thái Draft.");

        int addedCount = 0;
        foreach (var item in items)
        {
            if (string.IsNullOrWhiteSpace(item.Vin)) continue;
            var vin = item.Vin.Trim().ToUpperInvariant();
            if (m.Details.Any(d => d.Vin == vin)) continue;

            var carInfo = await FindVehicleContextAsync(vin);
            var carId = !string.IsNullOrWhiteSpace(item.CarId) ? item.CarId.Trim() : carInfo.CarId ?? ("CAR-" + vin[^6..]);
            var model = !string.IsNullOrWhiteSpace(item.Model) ? item.Model.Trim() : carInfo.Model ?? "Hyundai Model";
            var claimAmt = item.ClaimAmount ?? (carInfo.ClaimAmount > 0 ? carInfo.ClaimAmount : 650000000m);

            var line = new BankBillMinutesDetail
            {
                BankBillMinutesId = m.Id,
                BankBillMnNo = m.BankBillMnNo,
                CarId = carId,
                Vin = vin,
                Model = model,
                ModelCode = item.ModelCode ?? carInfo.ModelCode,
                SpecCode = item.SpecCode ?? carInfo.SpecCode,
                SpecDescription = item.SpecDescription ?? carInfo.SpecDescription ?? model,
                ColorCode = item.ColorCode ?? carInfo.ColorCode,
                EngineNo = item.EngineNo ?? carInfo.EngineNo ?? ("ENG-" + vin[^6..]),
                ContractNo = item.ContractNo ?? carInfo.ContractNo,
                BankGuaranteeNo = item.BankGuaranteeNo ?? carInfo.BankGuaranteeNo ?? ("BL-" + m.BankCode + "-" + vin[^4..]),
                GuaranteeBankCode = item.GuaranteeBankCode ?? carInfo.GuaranteeBankCode ?? m.BankCode,
                GuaranteeDateOpen = item.GuaranteeDateOpen ?? carInfo.GuaranteeDateOpen ?? DateTime.Today.AddDays(-15),
                HTCInvoiceNo = item.HTCInvoiceNo ?? carInfo.HTCInvoiceNo ?? ("HD-HTC-" + DateTime.Now.ToString("yyMM") + "-" + vin[^4..]),
                TCGInvoiceNo = item.TCGInvoiceNo ?? carInfo.TCGInvoiceNo,
                InvoiceNoFactory = item.InvoiceNoFactory ?? carInfo.InvoiceNoFactory,
                TransportMinutesNo = item.TransportMinutesNo ?? carInfo.TransportMinutesNo ?? ("BBBG-" + DateTime.Now.ToString("yyMM") + "-" + vin[^4..]),
                CQNo = item.CQNo ?? carInfo.CQNo ?? ("CQ-" + vin[^6..]),
                CONo = item.CONo ?? carInfo.CONo ?? ("CO-" + vin[^6..]),
                CabinCONo = item.CabinCONo ?? carInfo.CabinCONo,
                DeclarationNo = item.DeclarationNo ?? carInfo.DeclarationNo,
                ClaimAmount = claimAmt,
                Status = BankBillMinutesDetailStatus.Pending,
                Remark = item.Remark?.Trim()
            };

            m.Details.Add(line);
            addedCount++;
        }

        m.TotalCars = m.Details.Count;
        m.TotalClaimAmount = m.Details.Sum(d => d.ClaimAmount);
        m.LUDateTime = DateTime.Now;
        m.LUBy = "HTV_FINANCE";

        await db.SaveChangesAsync();
        return await DetailAsync(mnNo);
    }

    public async Task<object?> RemoveCarAsync(string bankBillMnNo, string vin)
    {
        var mnNo = bankBillMnNo.Trim().ToUpperInvariant();
        var targetVin = vin.Trim().ToUpperInvariant();

        var m = await db.BankBillMinutes
            .Include(x => x.Details)
            .FirstOrDefaultAsync(x => x.OrgId == Org && x.BankBillMnNo == mnNo);

        if (m is null) return null;

        if (m.Status != BankBillMinutesStatus.Draft)
            throw new InvalidOperationException($"Biên bản {mnNo} đang ở trạng thái {m.Status}. Chỉ được gỡ xe khi ở trạng thái Draft.");

        var line = m.Details.FirstOrDefault(d => d.Vin == targetVin);
        if (line is null)
            throw new InvalidOperationException($"Không tìm thấy xe VIN {targetVin} trong biên bản {mnNo}.");

        m.Details.Remove(line);
        db.BankBillMinutesDetails.Remove(line);

        m.TotalCars = m.Details.Count;
        m.TotalClaimAmount = m.Details.Sum(d => d.ClaimAmount);
        m.LUDateTime = DateTime.Now;
        m.LUBy = "HTV_FINANCE";

        await db.SaveChangesAsync();
        return await DetailAsync(mnNo);
    }

    public async Task<object> GetEligibleCarsAsync(string? dealerCode, string? bankCode)
    {
        // Lấy danh sách các VIN đã nằm trong biên bản hối phiếu đang hoạt động (chưa hủy)
        var usedVins = await db.BankBillMinutesDetails
            .Where(d => db.BankBillMinutes.Any(m => m.Id == d.BankBillMinutesId && m.OrgId == Org && m.Status != BankBillMinutesStatus.Cancelled))
            .Select(d => d.Vin)
            .Distinct()
            .ToListAsync();

        // 1. Tìm xe từ chi tiết hợp đồng bán buôn có bảo lãnh ngân hàng
        var contractQ = db.DealerContracts
            .Where(c => c.OrgId == Org && c.Status == DealerContractStatus.Signed && c.PaymentType == DealerContractPaymentType.Guarantee)
            .Include(c => c.Details)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(dealerCode))
            contractQ = contractQ.Where(c => c.DealerCode == dealerCode.Trim().ToUpperInvariant());

        if (!string.IsNullOrWhiteSpace(bankCode))
            contractQ = contractQ.Where(c => c.BankCode == bankCode.Trim().ToUpperInvariant());

        var contracts = await contractQ.ToListAsync();
        var resultList = new List<object>();

        foreach (var c in contracts)
        {
            foreach (var cd in c.Details)
            {
                if (string.IsNullOrWhiteSpace(cd.Vin) || usedVins.Contains(cd.Vin)) continue;

                resultList.Add(new
                {
                    Vin = cd.Vin,
                    CarId = cd.CarId,
                    Model = cd.Model,
                    SpecCode = cd.SpecCode,
                    ColorCode = cd.ColorCode,
                    ContractNo = c.ContractNo,
                    DealerCode = c.DealerCode,
                    DealerName = c.DealerName,
                    BankCode = c.BankCode,
                    BankName = c.BankName,
                    ClaimAmount = cd.TotalAmount > 0 ? cd.TotalAmount : 700000000m,
                    Source = "DealerContract"
                });
            }
        }

        // 2. Bổ sung xe từ bảo lãnh ngân hàng nếu chưa có trong kết quả
        var existingVinsInResult = resultList.Select(r => ((dynamic)r).Vin as string).ToHashSet();
        var grtQ = db.PaymentGuaranteeDetails
            .Where(gd => !string.IsNullOrEmpty(gd.Vin) && !usedVins.Contains(gd.Vin) && !existingVinsInResult.Contains(gd.Vin))
            .AsQueryable();

        var grtDetails = await grtQ.ToListAsync();
        foreach (var gd in grtDetails)
        {
            var grt = await db.PaymentGuarantees.FirstOrDefaultAsync(g => g.Id == gd.GuaranteeId && g.OrgId == Org && g.Status != GuaranteeStatus.Cancelled);
            if (grt is null) continue;
            if (!string.IsNullOrWhiteSpace(dealerCode) && grt.DealerCode != dealerCode.Trim().ToUpperInvariant()) continue;
            if (!string.IsNullOrWhiteSpace(bankCode) && grt.BankCode != bankCode.Trim().ToUpperInvariant()) continue;

            var vinStr = gd.Vin!;
            resultList.Add(new
            {
                Vin = vinStr,
                CarId = "CAR-" + (vinStr.Length >= 6 ? vinStr[^6..] : vinStr),
                Model = gd.Model ?? "Hyundai Vehicle",
                SpecCode = "",
                ColorCode = "",
                ContractNo = "",
                DealerCode = grt.DealerCode,
                DealerName = grt.DealerName,
                BankCode = grt.BankCode,
                BankName = grt.BankName,
                BankGuaranteeNo = grt.GuaranteeNo,
                ClaimAmount = gd.GuaranteeValue > 0 ? gd.GuaranteeValue : 650000000m,
                Source = "PaymentGuarantee"
            });
        }

        return resultList;
    }

    public async Task<object> StatsAsync()
    {
        var q = db.BankBillMinutes.Where(m => m.OrgId == Org);

        var totalMinutes = await q.CountAsync();
        var draftCount = await q.CountAsync(m => m.Status == BankBillMinutesStatus.Draft);
        var handoverCount = await q.CountAsync(m => m.Status == BankBillMinutesStatus.Handover);
        var bankReceivedCount = await q.CountAsync(m => m.Status == BankBillMinutesStatus.BankReceived);
        var settledCount = await q.CountAsync(m => m.Status == BankBillMinutesStatus.Settled);
        var cancelledCount = await q.CountAsync(m => m.Status == BankBillMinutesStatus.Cancelled);

        var totalCars = await q.Where(m => m.Status != BankBillMinutesStatus.Cancelled).SumAsync(m => m.TotalCars);
        var totalClaimAmount = await q.Where(m => m.Status != BankBillMinutesStatus.Cancelled).SumAsync(m => m.TotalClaimAmount);

        var distinctBanks = await q.Select(m => m.BankCode).Distinct().CountAsync();
        var distinctDealers = await q.Select(m => m.DealerCode).Distinct().CountAsync();

        return new BankBillMinutesStatsDto(
            TotalMinutes: totalMinutes,
            DraftCount: draftCount,
            HandoverCount: handoverCount,
            BankReceivedCount: bankReceivedCount,
            SettledCount: settledCount,
            CancelledCount: cancelledCount,
            TotalCars: totalCars,
            TotalClaimAmount: totalClaimAmount,
            DistinctBanksCount: distinctBanks,
            DistinctDealersCount: distinctDealers
        );
    }

    private async Task<VehicleContextHelper> FindVehicleContextAsync(string vin)
    {
        var ctx = new VehicleContextHelper();

        // 1. Tìm từ DealerContractDetails
        var contractDetail = await db.DealerContractDetails
            .Where(cd => cd.Vin == vin)
            .OrderByDescending(cd => cd.Id)
            .FirstOrDefaultAsync();

        if (contractDetail != null)
        {
            ctx.CarId = contractDetail.CarId;
            ctx.Model = contractDetail.Model;
            ctx.SpecCode = contractDetail.SpecCode;
            ctx.ColorCode = contractDetail.ColorCode;
            ctx.ClaimAmount = contractDetail.TotalAmount;

            var contract = await db.DealerContracts.FirstOrDefaultAsync(c => c.Id == contractDetail.ContractId);
            if (contract != null)
            {
                ctx.ContractNo = contract.ContractNo;
                ctx.GuaranteeBankCode = contract.BankCode;
            }
        }

        // 2. Tìm từ GuaranteeDetails
        var grtDetail = await db.PaymentGuaranteeDetails
            .Where(g => g.Vin == vin)
            .OrderByDescending(g => g.Id)
            .FirstOrDefaultAsync();

        if (grtDetail != null)
        {
            if (string.IsNullOrEmpty(ctx.Model)) ctx.Model = grtDetail.Model;
            if (ctx.ClaimAmount == 0) ctx.ClaimAmount = grtDetail.GuaranteeValue;

            var grt = await db.PaymentGuarantees.FirstOrDefaultAsync(g => g.Id == grtDetail.GuaranteeId);
            if (grt != null)
            {
                ctx.BankGuaranteeNo = grt.GuaranteeNo;
                ctx.GuaranteeBankCode ??= grt.BankCode;
                ctx.GuaranteeDateOpen = grt.CreatedAt;
            }
        }

        // 3. Tìm từ CarInvoices
        var invDetail = await db.CarInvoiceDetails
            .Where(i => i.Vin == vin)
            .OrderByDescending(i => i.Id)
            .FirstOrDefaultAsync();

        if (invDetail != null)
        {
            var inv = await db.CarInvoices.FirstOrDefaultAsync(i => i.Id == invDetail.InvoiceId);
            if (inv != null) ctx.HTCInvoiceNo = inv.InvoiceNo ?? inv.InvoiceCode;
            if (string.IsNullOrEmpty(ctx.EngineNo)) ctx.EngineNo = invDetail.EngineNo;
        }

        // 4. Tìm từ CarTransportMinutes
        var tmDetail = await db.TransportMinutesDetails
            .Where(t => t.Vin == vin)
            .OrderByDescending(t => t.Id)
            .FirstOrDefaultAsync();

        if (tmDetail != null)
        {
            var tm = await db.TransportMinutes.FirstOrDefaultAsync(t => t.Id == tmDetail.TransportMinutesId);
            if (tm != null) ctx.TransportMinutesNo = tm.TransportMinutesNo;
            if (string.IsNullOrEmpty(ctx.EngineNo)) ctx.EngineNo = tmDetail.EngineNo;
        }

        return ctx;
    }

    private static object FormatDetail(BankBillMinutesDetail d) => new
    {
        d.Id,
        d.BankBillMinutesId,
        d.BankBillMnNo,
        d.CarId,
        d.Vin,
        d.Model,
        d.ModelCode,
        d.SpecCode,
        d.SpecDescription,
        d.ColorCode,
        d.EngineNo,
        d.ContractNo,
        d.BankGuaranteeNo,
        d.GuaranteeBankCode,
        d.GuaranteeDateOpen,
        d.HTCInvoiceNo,
        d.TCGInvoiceNo,
        d.InvoiceNoFactory,
        d.TransportMinutesNo,
        d.CQNo,
        d.CONo,
        d.CabinCONo,
        d.DeclarationNo,
        d.ClaimAmount,
        Status = d.Status.ToString(),
        d.Remark
    };

    private static string GetBankNameDefault(string bankCode) => bankCode switch
    {
        "VCB" => "Ngân hàng TMCP Ngoại thương Việt Nam (Vietcombank)",
        "BIDV" => "Ngân hàng TMCP Đầu tư và Phát triển Việt Nam (BIDV)",
        "TCB" => "Ngân hàng TMCP Kỹ thương Việt Nam (Techcombank)",
        "VPB" => "Ngân hàng TMCP Việt Nam Thịnh Vượng (VPBank)",
        "CTG" or "VTB" => "Ngân hàng TMCP Công Thương Việt Nam (VietinBank)",
        "MBB" or "MB" => "Ngân hàng TMCP Quân đội (MBBank)",
        _ => $"Ngân hàng {bankCode}"
    };

    private static string GetDealerNameDefault(string dealerCode) => dealerCode switch
    {
        "VN001" => "Hyundai Đông Đô",
        "VN002" => "Hyundai Nam Trung",
        "VN003" => "Hyundai Tây Hồ",
        "VN004" => "Hyundai Phạm Văn Đồng",
        _ => $"Đại lý Hyundai {dealerCode}"
    };

    private sealed class VehicleContextHelper
    {
        public string? CarId { get; set; }
        public string? Model { get; set; }
        public string? ModelCode { get; set; }
        public string? SpecCode { get; set; }
        public string? SpecDescription { get; set; }
        public string? ColorCode { get; set; }
        public string? EngineNo { get; set; }
        public string? ContractNo { get; set; }
        public string? BankGuaranteeNo { get; set; }
        public string? GuaranteeBankCode { get; set; }
        public DateTime? GuaranteeDateOpen { get; set; }
        public string? HTCInvoiceNo { get; set; }
        public string? TCGInvoiceNo { get; set; }
        public string? InvoiceNoFactory { get; set; }
        public string? TransportMinutesNo { get; set; }
        public string? CQNo { get; set; }
        public string? CONo { get; set; }
        public string? CabinCONo { get; set; }
        public string? DeclarationNo { get; set; }
        public decimal ClaimAmount { get; set; }
    }
}
