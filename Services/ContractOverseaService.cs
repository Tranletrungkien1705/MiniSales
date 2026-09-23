using Microsoft.EntityFrameworkCore;
using MiniSales.Data;
using MiniSales.Models;

namespace MiniSales.Services;

public record CreateContractOverseaPiDetailDto(
    string RefNo,
    string LCTemp,
    long? DetailId = null
);

public record CreateContractOverseaDto(
    string? ContractNo = null,
    DateTime? ContractDate = null,
    string? SupplierCode = null,
    string? SupplierName = null,
    string? Incoterms = null,
    string? DestinationPort = null,
    string? Currency = null,
    DateTime? ExpectedDeliveryDate = null,
    string? Remark = null,
    string? CreatedBy = null,
    List<CreateContractOverseaPiDetailDto>? PiDetails = null
);

public record UpdateContractOverseaDto(
    string? Incoterms = null,
    string? DestinationPort = null,
    DateTime? ExpectedDeliveryDate = null,
    string? Remark = null,
    string? UpdatedBy = null
);

public record CancelContractOverseaDto(
    string CancelReason,
    string? CancelledBy = null
);

public record AddContractOverseaPiDetailsDto(
    List<CreateContractOverseaPiDetailDto> Details,
    string? UpdatedBy = null
);

public record CreateLetterOfCreditDto(
    string? LCNo = null,
    string ContractNo = "",
    string? BankCode = null,
    string? BankName = null,
    string? BankBranch = null,
    LCType LCType = LCType.AtSight,
    decimal Amount = 0,
    string? Currency = "USD",
    DateTime? IssueDate = null,
    DateTime? ExpireDate = null,
    DateTime? LatestShipmentDate = null,
    string? Remark = null,
    string? CreatedBy = null
);

public record ExportContractOverseaDto(
    List<string>? ContractNos = null
);

public record ContractOverseaDetailVm(
    ContractOversea Contract,
    LetterOfCredit? LetterOfCredit,
    List<PerformanceInvoiceDetail> Details
);

public record ContractOverseaStatsVm(
    int TotalContracts,
    int ActiveContracts,
    int LinkedLCContracts,
    int CompletedContracts,
    int CancelledContracts,
    int TotalVehicles,
    decimal TotalAmountUSD,
    decimal TotalLCAmountUSD,
    int TotalLCs,
    Dictionary<string, int> ByDestinationPort,
    Dictionary<string, int> BySupplier,
    Dictionary<string, int> ByStatus
);

public record ExportContractOverseaResultVm(
    DateTime ExportDate,
    int TotalContracts,
    int TotalDetails,
    List<ContractOversea> Contracts,
    List<PerformanceInvoiceDetail> Details
);

public interface IContractOverseaService
{
    Task<string> GetNextContractNoAsync();
    Task<string> GetNextLCNoAsync();
    Task<object> SearchAsync(
        string? contractNo,
        string? refNo,
        string? lcNo,
        string? supplierCode,
        string? destinationPort,
        ContractOverseaStatus? status,
        DateTime? createdFrom,
        DateTime? createdTo,
        int pageIndex = 0,
        int pageSize = 50
    );
    Task<List<ContractOversea>> SearchAllWithoutLCAsync();
    Task<ContractOverseaDetailVm?> DetailAsync(string contractNo);
    Task<ContractOverseaDetailVm> CreateAsync(CreateContractOverseaDto dto);
    Task<ContractOversea?> UpdateAsync(string contractNo, UpdateContractOverseaDto dto);
    Task<bool> DeleteAsync(string contractNo);
    Task<ContractOversea?> CancelAsync(string contractNo, CancelContractOverseaDto dto);
    Task<ContractOverseaDetailVm?> AddPIDetailsAsync(string contractNo, AddContractOverseaPiDetailsDto dto);
    Task<ContractOverseaDetailVm?> RemovePIDetailAsync(string contractNo, long detailId);
    Task<List<PerformanceInvoiceDetail>> GetEligiblePIDetailsAsync(string? refNo, string? modelCode, string? portCode);

    // L/C operations:
    Task<LetterOfCredit> CreateLCAsync(CreateLetterOfCreditDto dto);
    Task<object> ListLCAsync(
        string? lcNo,
        string? contractNo,
        string? bankCode,
        string? bankName,
        LCStatus? status,
        DateTime? createdFrom,
        DateTime? createdTo,
        int pageIndex = 0,
        int pageSize = 50
    );
    Task<LetterOfCredit?> DetailLCAsync(string lcNo);
    Task<bool> DeleteLCAsync(string lcNo);

    // Export & Stats:
    Task<ExportContractOverseaResultVm> ExportAsync(ExportContractOverseaDto? dto);
    Task<ContractOverseaStatsVm> StatsAsync();
}

public sealed class ContractOverseaService(AppDbContext db, ITenantContext tenant) : IContractOverseaService
{
    private const int MinLengthCode = 5;

    public async Task<string> GetNextContractNoAsync()
    {
        var prefix = DateTime.Today.ToString("yyMM") + "HMC";
        var last = await db.ContractOverseas
            .Where(x => x.OrgId == tenant.OrgId && x.ContractNo.StartsWith(prefix))
            .OrderByDescending(x => x.ContractNo)
            .Select(x => x.ContractNo)
            .FirstOrDefaultAsync();

        if (last is not null && last.Length >= prefix.Length + 4 && int.TryParse(last.Substring(prefix.Length, 4), out var seq))
        {
            return $"{prefix}{(seq + 1):D4}";
        }

        return $"{prefix}0001";
    }

    public async Task<string> GetNextLCNoAsync()
    {
        var prefix = "LC" + DateTime.Today.ToString("yyMM");
        var last = await db.LettersOfCredit
            .Where(x => x.OrgId == tenant.OrgId && x.LCNo.StartsWith(prefix))
            .OrderByDescending(x => x.LCNo)
            .Select(x => x.LCNo)
            .FirstOrDefaultAsync();

        if (last is not null && last.Length >= prefix.Length + 4 && int.TryParse(last.Substring(prefix.Length, 4), out var seq))
        {
            return $"{prefix}{(seq + 1):D4}";
        }

        return $"{prefix}0001";
    }

    public async Task<object> SearchAsync(
        string? contractNo,
        string? refNo,
        string? lcNo,
        string? supplierCode,
        string? destinationPort,
        ContractOverseaStatus? status,
        DateTime? createdFrom,
        DateTime? createdTo,
        int pageIndex = 0,
        int pageSize = 50)
    {
        var query = db.ContractOverseas.Where(x => x.OrgId == tenant.OrgId);

        if (!string.IsNullOrWhiteSpace(contractNo))
        {
            var cNo = contractNo.Trim().ToUpper();
            query = query.Where(x => x.ContractNo.Contains(cNo));
        }

        if (!string.IsNullOrWhiteSpace(lcNo))
        {
            var lNo = lcNo.Trim().ToUpper();
            query = query.Where(x => x.LCNo != null && x.LCNo.Contains(lNo));
        }

        if (!string.IsNullOrWhiteSpace(supplierCode))
        {
            var sc = supplierCode.Trim().ToUpper();
            query = query.Where(x => x.SupplierCode == sc);
        }

        if (!string.IsNullOrWhiteSpace(destinationPort))
        {
            var dp = destinationPort.Trim().ToUpper();
            query = query.Where(x => x.DestinationPort == dp);
        }

        if (status.HasValue)
        {
            query = query.Where(x => x.Status == status.Value);
        }

        if (createdFrom.HasValue)
        {
            query = query.Where(x => x.CreatedAt >= createdFrom.Value);
        }

        if (createdTo.HasValue)
        {
            query = query.Where(x => x.CreatedAt <= createdTo.Value);
        }

        if (!string.IsNullOrWhiteSpace(refNo))
        {
            var rNo = refNo.Trim().ToUpper();
            var contractsWithRef = await db.PerformanceInvoiceDetails
                .Where(d => d.RefNo.Contains(rNo) && !string.IsNullOrEmpty(d.ContractNo))
                .Select(d => d.ContractNo!)
                .Distinct()
                .ToListAsync();

            query = query.Where(x => contractsWithRef.Contains(x.ContractNo));
        }

        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(x => x.CreatedAt)
            .Skip(pageIndex * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new
        {
            total,
            pageIndex,
            pageSize,
            items
        };
    }

    public async Task<List<ContractOversea>> SearchAllWithoutLCAsync()
    {
        // Trả về tất cả hợp đồng ngoại chưa có LC (LCNo rỗng) và chưa bị hủy, phục vụ chọn HĐ khi mở L/C mới
        return await db.ContractOverseas
            .Where(x => x.OrgId == tenant.OrgId && (x.LCNo == null || x.LCNo == "") && x.Status != ContractOverseaStatus.Cancelled)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync();
    }

    public async Task<ContractOverseaDetailVm?> DetailAsync(string contractNo)
    {
        var contract = await db.ContractOverseas
            .FirstOrDefaultAsync(x => x.OrgId == tenant.OrgId && x.ContractNo == contractNo);

        if (contract is null) return null;

        LetterOfCredit? lc = null;
        if (!string.IsNullOrEmpty(contract.LCNo))
        {
            lc = await db.LettersOfCredit
                .FirstOrDefaultAsync(x => x.OrgId == tenant.OrgId && x.LCNo == contract.LCNo);
        }

        var details = await db.PerformanceInvoiceDetails
            .Where(x => x.ContractNo == contractNo)
            .ToListAsync();

        return new ContractOverseaDetailVm(contract, lc, details);
    }

    public async Task<ContractOverseaDetailVm> CreateAsync(CreateContractOverseaDto dto)
    {
        if (dto.PiDetails is null || dto.PiDetails.Count == 0)
            throw new InvalidOperationException("Hợp đồng ngoại phải chứa ít nhất 01 dòng xe chi tiết từ Phiếu hiệu suất (PI).");

        var contractNo = string.IsNullOrWhiteSpace(dto.ContractNo)
            ? await GetNextContractNoAsync()
            : dto.ContractNo.Trim().ToUpper();

        if (contractNo.Length < MinLengthCode)
            throw new InvalidOperationException($"Số hợp đồng ngoại '{contractNo}' không hợp lệ (độ dài tối thiểu {MinLengthCode} ký tự).");

        var exists = await db.ContractOverseas
            .AnyAsync(x => x.OrgId == tenant.OrgId && x.ContractNo == contractNo);

        if (exists)
            throw new InvalidOperationException($"Hợp đồng ngoại '{contractNo}' đã tồn tại trong hệ thống.");

        // Kiểm tra trùng lặp khóa RefNo + LCTemp trong input
        var keys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var p in dto.PiDetails)
        {
            var k = $"{p.RefNo.Trim()}|{p.LCTemp.Trim()}";
            if (!keys.Add(k))
                throw new InvalidOperationException($"Trùng lặp dòng xe PI ({p.RefNo} / {p.LCTemp}) trong danh sách yêu cầu.");
        }

        // Lấy danh sách chi tiết PI và kiểm tra
        var matchedDetails = new List<PerformanceInvoiceDetail>();
        var affectedRefNos = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var p in dto.PiDetails)
        {
            PerformanceInvoiceDetail? detail = null;
            if (p.DetailId.HasValue && p.DetailId.Value > 0)
            {
                detail = await db.PerformanceInvoiceDetails
                    .FirstOrDefaultAsync(x => x.Id == p.DetailId.Value);
            }
            else
            {
                detail = await db.PerformanceInvoiceDetails
                    .FirstOrDefaultAsync(x => x.RefNo == p.RefNo.Trim() && x.LCTemp == p.LCTemp.Trim());
            }

            if (detail is null)
                throw new InvalidOperationException($"Không tìm thấy dòng chi tiết PI ({p.RefNo} / {p.LCTemp}) trong hệ thống.");

            if (!string.IsNullOrEmpty(detail.ContractNo))
                throw new InvalidOperationException($"Dòng PI ({detail.RefNo} / {detail.LCTemp} - {detail.ModelCode}) đã thuộc hợp đồng ngoại '{detail.ContractNo}'.");

            detail.ContractNo = contractNo;
            matchedDetails.Add(detail);
            affectedRefNos.Add(detail.RefNo);
        }

        var totalQty = matchedDetails.Sum(x => x.Quantity);
        var totalAmount = matchedDetails.Sum(x => x.TotalAmount);
        var destPort = !string.IsNullOrWhiteSpace(dto.DestinationPort)
            ? dto.DestinationPort.Trim().ToUpper()
            : (matchedDetails.FirstOrDefault()?.PortCode ?? "HPH");

        var contract = new ContractOversea
        {
            OrgId = tenant.OrgId,
            ContractNo = contractNo,
            ContractDate = dto.ContractDate ?? DateTime.Today,
            SupplierCode = string.IsNullOrWhiteSpace(dto.SupplierCode) ? "HMC" : dto.SupplierCode.Trim().ToUpper(),
            SupplierName = string.IsNullOrWhiteSpace(dto.SupplierName) ? "Hyundai Motor Company Korea" : dto.SupplierName.Trim(),
            Incoterms = string.IsNullOrWhiteSpace(dto.Incoterms) ? "CIF Hai Phong" : dto.Incoterms.Trim(),
            DestinationPort = destPort,
            Currency = string.IsNullOrWhiteSpace(dto.Currency) ? "USD" : dto.Currency.Trim().ToUpper(),
            ExpectedDeliveryDate = dto.ExpectedDeliveryDate,
            TotalQuantity = totalQty,
            TotalAmount = totalAmount,
            Status = ContractOverseaStatus.Active,
            Remark = dto.Remark,
            CreatedBy = dto.CreatedBy ?? "SYSTEM",
            CreatedAt = DateTime.Now
        };

        db.ContractOverseas.Add(contract);

        // Kiểm tra và cập nhật trạng thái các PI cha sang Contracted nếu tất cả dòng của PI đó đã vào HĐ
        foreach (var rNo in affectedRefNos)
        {
            var pi = await db.PerformanceInvoices
                .Include(x => x.Details)
                .FirstOrDefaultAsync(x => x.OrgId == tenant.OrgId && x.RefNo == rNo);

            if (pi is not null && pi.Details.All(x => !string.IsNullOrEmpty(x.ContractNo)))
            {
                pi.Status = PerformanceInvoiceStatus.Contracted;
                pi.UpdatedAt = DateTime.Now;
                pi.UpdatedBy = dto.CreatedBy ?? "SYSTEM";
            }
        }

        await db.SaveChangesAsync();
        return new ContractOverseaDetailVm(contract, null, matchedDetails);
    }

    public async Task<ContractOversea?> UpdateAsync(string contractNo, UpdateContractOverseaDto dto)
    {
        var contract = await db.ContractOverseas
            .FirstOrDefaultAsync(x => x.OrgId == tenant.OrgId && x.ContractNo == contractNo);

        if (contract is null) return null;

        if (contract.Status == ContractOverseaStatus.Cancelled)
            throw new InvalidOperationException($"Hợp đồng ngoại '{contractNo}' đã bị hủy, không thể cập nhật.");

        if (contract.Status == ContractOverseaStatus.Completed)
            throw new InvalidOperationException($"Hợp đồng ngoại '{contractNo}' đã hoàn tất, không thể chỉnh sửa.");

        if (!string.IsNullOrWhiteSpace(dto.Incoterms))
            contract.Incoterms = dto.Incoterms.Trim();

        if (!string.IsNullOrWhiteSpace(dto.DestinationPort))
            contract.DestinationPort = dto.DestinationPort.Trim().ToUpper();

        if (dto.ExpectedDeliveryDate.HasValue)
            contract.ExpectedDeliveryDate = dto.ExpectedDeliveryDate.Value;

        if (dto.Remark is not null)
            contract.Remark = dto.Remark;

        contract.UpdatedAt = DateTime.Now;
        contract.UpdatedBy = dto.UpdatedBy ?? "SYSTEM";

        await db.SaveChangesAsync();
        return contract;
    }

    public async Task<bool> DeleteAsync(string contractNo)
    {
        var contract = await db.ContractOverseas
            .FirstOrDefaultAsync(x => x.OrgId == tenant.OrgId && x.ContractNo == contractNo);

        if (contract is null) return false;

        // Kiểm tra nếu có LC hiệu lực đang gắn vào
        if (!string.IsNullOrEmpty(contract.LCNo))
        {
            var hasActiveLC = await db.LettersOfCredit
                .AnyAsync(x => x.OrgId == tenant.OrgId && x.LCNo == contract.LCNo && x.Status == LCStatus.Active);

            if (hasActiveLC)
                throw new InvalidOperationException($"Hợp đồng ngoại '{contractNo}' đang có Thư tín dụng L/C ({contract.LCNo}) hiệu lực. Cần hủy hoặc xóa L/C trước khi xóa hợp đồng.");
        }

        // Kiểm tra Packing List hoặc Customs Declaration liên quan
        var hasPL = await db.PackingLists
            .AnyAsync(x => x.OrgId == tenant.OrgId && x.ContractNo == contractNo && x.Status != PackingListStatus.Cancelled);
        if (hasPL)
            throw new InvalidOperationException($"Hợp đồng ngoại '{contractNo}' đã phát sinh lô Packing List, không thể xóa.");

        var hasTKHQ = await db.CustomsDeclarations
            .AnyAsync(x => x.OrgId == tenant.OrgId && x.ContractNo == contractNo && x.Status != CustomsDeclarationStatus.Cancelled);
        if (hasTKHQ)
            throw new InvalidOperationException($"Hợp đồng ngoại '{contractNo}' đã mở tờ khai Hải quan, không thể xóa.");

        // Giải phóng các dòng PerformanceInvoiceDetail
        var details = await db.PerformanceInvoiceDetails
            .Where(x => x.ContractNo == contractNo)
            .ToListAsync();

        var affectedRefNos = details.Select(x => x.RefNo).Distinct().ToList();

        foreach (var d in details)
        {
            d.ContractNo = null;
        }

        // Revert trạng thái PI cha về Confirmed nếu đang là Contracted
        foreach (var rNo in affectedRefNos)
        {
            var pi = await db.PerformanceInvoices
                .Include(x => x.Details)
                .FirstOrDefaultAsync(x => x.OrgId == tenant.OrgId && x.RefNo == rNo);

            if (pi is not null && pi.Status == PerformanceInvoiceStatus.Contracted)
            {
                pi.Status = PerformanceInvoiceStatus.Confirmed;
                pi.UpdatedAt = DateTime.Now;
                pi.UpdatedBy = "SYSTEM";
            }
        }

        db.ContractOverseas.Remove(contract);
        await db.SaveChangesAsync();
        return true;
    }

    public async Task<ContractOversea?> CancelAsync(string contractNo, CancelContractOverseaDto dto)
    {
        var contract = await db.ContractOverseas
            .FirstOrDefaultAsync(x => x.OrgId == tenant.OrgId && x.ContractNo == contractNo);

        if (contract is null) return null;

        if (contract.Status == ContractOverseaStatus.Cancelled)
            throw new InvalidOperationException($"Hợp đồng ngoại '{contractNo}' đã bị hủy trước đó.");

        if (contract.Status == ContractOverseaStatus.Completed)
            throw new InvalidOperationException($"Hợp đồng ngoại '{contractNo}' đã hoàn tất nhận hàng và thanh lý, không thể hủy.");

        if (string.IsNullOrWhiteSpace(dto.CancelReason))
            throw new InvalidOperationException("Lý do hủy hợp đồng ngoại không được để trống.");

        // Giải phóng các dòng PI chi tiết
        var details = await db.PerformanceInvoiceDetails
            .Where(x => x.ContractNo == contractNo)
            .ToListAsync();

        var affectedRefNos = details.Select(x => x.RefNo).Distinct().ToList();

        foreach (var d in details)
        {
            d.ContractNo = null;
        }

        // Revert trạng thái PI cha
        foreach (var rNo in affectedRefNos)
        {
            var pi = await db.PerformanceInvoices
                .Include(x => x.Details)
                .FirstOrDefaultAsync(x => x.OrgId == tenant.OrgId && x.RefNo == rNo);

            if (pi is not null && pi.Status == PerformanceInvoiceStatus.Contracted)
            {
                pi.Status = PerformanceInvoiceStatus.Confirmed;
                pi.UpdatedAt = DateTime.Now;
                pi.UpdatedBy = dto.CancelledBy ?? "SYSTEM";
            }
        }

        // Hủy L/C nếu có
        if (!string.IsNullOrEmpty(contract.LCNo))
        {
            var lc = await db.LettersOfCredit
                .FirstOrDefaultAsync(x => x.OrgId == tenant.OrgId && x.LCNo == contract.LCNo);
            if (lc is not null && lc.Status == LCStatus.Active)
            {
                lc.Status = LCStatus.Cancelled;
                lc.Remark = (lc.Remark ?? "") + $" | Hủy theo HĐ ngoại {contractNo}: {dto.CancelReason}";
                lc.UpdatedAt = DateTime.Now;
                lc.UpdatedBy = dto.CancelledBy ?? "SYSTEM";
            }
        }

        contract.Status = ContractOverseaStatus.Cancelled;
        contract.CancelReason = dto.CancelReason.Trim();
        contract.CancelledBy = dto.CancelledBy ?? "SYSTEM";
        contract.CancelledAt = DateTime.Now;
        contract.UpdatedAt = DateTime.Now;
        contract.UpdatedBy = dto.CancelledBy ?? "SYSTEM";

        await db.SaveChangesAsync();
        return contract;
    }

    public async Task<ContractOverseaDetailVm?> AddPIDetailsAsync(string contractNo, AddContractOverseaPiDetailsDto dto)
    {
        var contract = await db.ContractOverseas
            .FirstOrDefaultAsync(x => x.OrgId == tenant.OrgId && x.ContractNo == contractNo);

        if (contract is null) return null;

        if (contract.Status == ContractOverseaStatus.Cancelled || contract.Status == ContractOverseaStatus.Completed)
            throw new InvalidOperationException($"Không thể thêm dòng xe vào hợp đồng ở trạng thái '{contract.Status}'.");

        if (dto.Details is null || dto.Details.Count == 0)
            throw new InvalidOperationException("Danh sách chi tiết thêm mới không được rỗng.");

        var addedDetails = new List<PerformanceInvoiceDetail>();
        var affectedRefNos = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var p in dto.Details)
        {
            PerformanceInvoiceDetail? detail = null;
            if (p.DetailId.HasValue && p.DetailId.Value > 0)
            {
                detail = await db.PerformanceInvoiceDetails
                    .FirstOrDefaultAsync(x => x.Id == p.DetailId.Value);
            }
            else
            {
                detail = await db.PerformanceInvoiceDetails
                    .FirstOrDefaultAsync(x => x.RefNo == p.RefNo.Trim() && x.LCTemp == p.LCTemp.Trim());
            }

            if (detail is null)
                throw new InvalidOperationException($"Không tìm thấy dòng chi tiết PI ({p.RefNo} / {p.LCTemp}).");

            if (!string.IsNullOrEmpty(detail.ContractNo))
                throw new InvalidOperationException($"Dòng PI ({detail.RefNo} / {detail.LCTemp}) đã thuộc hợp đồng ngoại '{detail.ContractNo}'.");

            detail.ContractNo = contractNo;
            addedDetails.Add(detail);
            affectedRefNos.Add(detail.RefNo);
        }

        // Cập nhật lại số lượng và tổng tiền của hợp đồng
        var allContractDetails = await db.PerformanceInvoiceDetails
            .Where(x => x.ContractNo == contractNo)
            .ToListAsync();

        contract.TotalQuantity = allContractDetails.Sum(x => x.Quantity);
        contract.TotalAmount = allContractDetails.Sum(x => x.TotalAmount);
        contract.UpdatedAt = DateTime.Now;
        contract.UpdatedBy = dto.UpdatedBy ?? "SYSTEM";

        // Cập nhật PI cha nếu tất cả dòng đã vào HĐ
        foreach (var rNo in affectedRefNos)
        {
            var pi = await db.PerformanceInvoices
                .Include(x => x.Details)
                .FirstOrDefaultAsync(x => x.OrgId == tenant.OrgId && x.RefNo == rNo);

            if (pi is not null && pi.Details.All(x => !string.IsNullOrEmpty(x.ContractNo)))
            {
                pi.Status = PerformanceInvoiceStatus.Contracted;
                pi.UpdatedAt = DateTime.Now;
                pi.UpdatedBy = dto.UpdatedBy ?? "SYSTEM";
            }
        }

        await db.SaveChangesAsync();

        LetterOfCredit? lc = null;
        if (!string.IsNullOrEmpty(contract.LCNo))
        {
            lc = await db.LettersOfCredit
                .FirstOrDefaultAsync(x => x.OrgId == tenant.OrgId && x.LCNo == contract.LCNo);
        }

        return new ContractOverseaDetailVm(contract, lc, allContractDetails);
    }

    public async Task<ContractOverseaDetailVm?> RemovePIDetailAsync(string contractNo, long detailId)
    {
        var contract = await db.ContractOverseas
            .FirstOrDefaultAsync(x => x.OrgId == tenant.OrgId && x.ContractNo == contractNo);

        if (contract is null) return null;

        if (contract.Status == ContractOverseaStatus.Cancelled || contract.Status == ContractOverseaStatus.Completed)
            throw new InvalidOperationException($"Không thể gỡ dòng xe khỏi hợp đồng ở trạng thái '{contract.Status}'.");

        var detail = await db.PerformanceInvoiceDetails
            .FirstOrDefaultAsync(x => x.Id == detailId && x.ContractNo == contractNo);

        if (detail is null)
            throw new InvalidOperationException($"Không tìm thấy dòng xe {detailId} trong hợp đồng '{contractNo}'.");

        var refNo = detail.RefNo;
        detail.ContractNo = null;

        // Cập nhật lại số lượng và tổng tiền
        var remainingDetails = await db.PerformanceInvoiceDetails
            .Where(x => x.ContractNo == contractNo && x.Id != detailId)
            .ToListAsync();

        contract.TotalQuantity = remainingDetails.Sum(x => x.Quantity);
        contract.TotalAmount = remainingDetails.Sum(x => x.TotalAmount);
        contract.UpdatedAt = DateTime.Now;
        contract.UpdatedBy = "SYSTEM";

        // Revert PI cha về Confirmed
        var pi = await db.PerformanceInvoices
            .Include(x => x.Details)
            .FirstOrDefaultAsync(x => x.OrgId == tenant.OrgId && x.RefNo == refNo);

        if (pi is not null && pi.Status == PerformanceInvoiceStatus.Contracted)
        {
            pi.Status = PerformanceInvoiceStatus.Confirmed;
            pi.UpdatedAt = DateTime.Now;
            pi.UpdatedBy = "SYSTEM";
        }

        await db.SaveChangesAsync();

        LetterOfCredit? lc = null;
        if (!string.IsNullOrEmpty(contract.LCNo))
        {
            lc = await db.LettersOfCredit
                .FirstOrDefaultAsync(x => x.OrgId == tenant.OrgId && x.LCNo == contract.LCNo);
        }

        return new ContractOverseaDetailVm(contract, lc, remainingDetails);
    }

    public async Task<List<PerformanceInvoiceDetail>> GetEligiblePIDetailsAsync(string? refNo, string? modelCode, string? portCode)
    {
        // Tra cứu các dòng PI chưa vào hợp đồng ngoại (ContractNo == null hoặc "") từ các PI đã được chốt (Confirmed)
        var query = from pi in db.PerformanceInvoices
                    join d in db.PerformanceInvoiceDetails on pi.Id equals d.PerformanceInvoiceId
                    where pi.OrgId == tenant.OrgId
                          && pi.Status == PerformanceInvoiceStatus.Confirmed
                          && (d.ContractNo == null || d.ContractNo == "")
                    select d;

        if (!string.IsNullOrWhiteSpace(refNo))
        {
            var rNo = refNo.Trim().ToUpper();
            query = query.Where(x => x.RefNo.Contains(rNo));
        }

        if (!string.IsNullOrWhiteSpace(modelCode))
        {
            var mc = modelCode.Trim().ToUpper();
            query = query.Where(x => x.ModelCode == mc);
        }

        if (!string.IsNullOrWhiteSpace(portCode))
        {
            var pc = portCode.Trim().ToUpper();
            query = query.Where(x => x.PortCode == pc);
        }

        return await query.OrderBy(x => x.RefNo).ThenBy(x => x.LCTemp).ToListAsync();
    }

    // =================================== L/C OPERATIONS ===================================

    public async Task<LetterOfCredit> CreateLCAsync(CreateLetterOfCreditDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.ContractNo))
            throw new InvalidOperationException("Số hợp đồng ngoại (ContractNo) không được để trống khi mở L/C.");

        var contractNo = dto.ContractNo.Trim().ToUpper();
        var contract = await db.ContractOverseas
            .FirstOrDefaultAsync(x => x.OrgId == tenant.OrgId && x.ContractNo == contractNo);

        if (contract is null)
            throw new InvalidOperationException($"Không tìm thấy hợp đồng ngoại '{contractNo}' trong hệ thống.");

        if (contract.Status == ContractOverseaStatus.Cancelled)
            throw new InvalidOperationException($"Hợp đồng ngoại '{contractNo}' đã bị hủy, không thể mở L/C.");

        if (contract.Status == ContractOverseaStatus.Completed)
            throw new InvalidOperationException($"Hợp đồng ngoại '{contractNo}' đã hoàn tất thanh lý.");

        var lcNo = string.IsNullOrWhiteSpace(dto.LCNo)
            ? await GetNextLCNoAsync()
            : dto.LCNo.Trim().ToUpper();

        if (lcNo.Length < MinLengthCode)
            throw new InvalidOperationException($"Số thư tín dụng L/C '{lcNo}' không hợp lệ (tối thiểu {MinLengthCode} ký tự).");

        var lcExists = await db.LettersOfCredit
            .AnyAsync(x => x.OrgId == tenant.OrgId && x.LCNo == lcNo);

        if (lcExists)
            throw new InvalidOperationException($"Thư tín dụng L/C '{lcNo}' đã tồn tại trong hệ thống.");

        var issueDate = dto.IssueDate ?? DateTime.Today;
        var expireDate = dto.ExpireDate ?? issueDate.AddDays(90);

        if (expireDate < issueDate)
            throw new InvalidOperationException("Ngày hết hạn L/C (ExpireDate) phải lớn hơn hoặc bằng ngày mở L/C (IssueDate).");

        var amount = dto.Amount > 0 ? dto.Amount : contract.TotalAmount;
        var bankName = !string.IsNullOrWhiteSpace(dto.BankName)
            ? dto.BankName.Trim()
            : "Ngân hàng TMCP Ngoại thương Việt Nam (Vietcombank)";

        var lc = new LetterOfCredit
        {
            OrgId = tenant.OrgId,
            LCNo = lcNo,
            ContractNo = contractNo,
            BankCode = string.IsNullOrWhiteSpace(dto.BankCode) ? "VCB" : dto.BankCode.Trim().ToUpper(),
            BankName = bankName,
            BankBranch = dto.BankBranch?.Trim() ?? "Sở Giao Dịch Hà Nội",
            LCType = dto.LCType,
            Amount = amount,
            Currency = string.IsNullOrWhiteSpace(dto.Currency) ? "USD" : dto.Currency.Trim().ToUpper(),
            IssueDate = issueDate,
            ExpireDate = expireDate,
            LatestShipmentDate = dto.LatestShipmentDate ?? issueDate.AddDays(45),
            Status = LCStatus.Active,
            Remark = dto.Remark,
            CreatedBy = dto.CreatedBy ?? "SYSTEM",
            CreatedAt = DateTime.Now
        };

        db.LettersOfCredit.Add(lc);

        // Cập nhật hợp đồng ngoại
        contract.LCNo = lcNo;
        contract.BankName = bankName;
        contract.Status = ContractOverseaStatus.LinkedLC;
        contract.UpdatedAt = DateTime.Now;
        contract.UpdatedBy = dto.CreatedBy ?? "SYSTEM";

        await db.SaveChangesAsync();
        return lc;
    }

    public async Task<object> ListLCAsync(
        string? lcNo,
        string? contractNo,
        string? bankCode,
        string? bankName,
        LCStatus? status,
        DateTime? createdFrom,
        DateTime? createdTo,
        int pageIndex = 0,
        int pageSize = 50)
    {
        var query = db.LettersOfCredit.Where(x => x.OrgId == tenant.OrgId);

        if (!string.IsNullOrWhiteSpace(lcNo))
        {
            var lNo = lcNo.Trim().ToUpper();
            query = query.Where(x => x.LCNo.Contains(lNo));
        }

        if (!string.IsNullOrWhiteSpace(contractNo))
        {
            var cNo = contractNo.Trim().ToUpper();
            query = query.Where(x => x.ContractNo.Contains(cNo));
        }

        if (!string.IsNullOrWhiteSpace(bankCode))
        {
            var bc = bankCode.Trim().ToUpper();
            query = query.Where(x => x.BankCode == bc);
        }

        if (!string.IsNullOrWhiteSpace(bankName))
        {
            var bn = bankName.Trim();
            query = query.Where(x => x.BankName.Contains(bn));
        }

        if (status.HasValue)
        {
            query = query.Where(x => x.Status == status.Value);
        }

        if (createdFrom.HasValue)
        {
            query = query.Where(x => x.CreatedAt >= createdFrom.Value);
        }

        if (createdTo.HasValue)
        {
            query = query.Where(x => x.CreatedAt <= createdTo.Value);
        }

        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(x => x.CreatedAt)
            .Skip(pageIndex * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new
        {
            total,
            pageIndex,
            pageSize,
            items
        };
    }

    public async Task<LetterOfCredit?> DetailLCAsync(string lcNo)
    {
        return await db.LettersOfCredit
            .FirstOrDefaultAsync(x => x.OrgId == tenant.OrgId && x.LCNo == lcNo);
    }

    public async Task<bool> DeleteLCAsync(string lcNo)
    {
        var lc = await db.LettersOfCredit
            .FirstOrDefaultAsync(x => x.OrgId == tenant.OrgId && x.LCNo == lcNo);

        if (lc is null) return false;

        // Cập nhật lại hợp đồng ngoại nếu đang gắn LC này
        var contract = await db.ContractOverseas
            .FirstOrDefaultAsync(x => x.OrgId == tenant.OrgId && x.LCNo == lcNo);

        if (contract is not null)
        {
            contract.LCNo = null;
            if (contract.Status == ContractOverseaStatus.LinkedLC)
            {
                contract.Status = ContractOverseaStatus.Active;
            }
            contract.UpdatedAt = DateTime.Now;
            contract.UpdatedBy = "SYSTEM";
        }

        db.LettersOfCredit.Remove(lc);
        await db.SaveChangesAsync();
        return true;
    }

    // =================================== EXPORT & STATS ===================================

    public async Task<ExportContractOverseaResultVm> ExportAsync(ExportContractOverseaDto? dto)
    {
        var query = db.ContractOverseas.Where(x => x.OrgId == tenant.OrgId);

        if (dto?.ContractNos is not null && dto.ContractNos.Count > 0)
        {
            var filterNos = dto.ContractNos.Select(x => x.Trim().ToUpper()).ToList();
            query = query.Where(x => filterNos.Contains(x.ContractNo));
        }

        var contracts = await query.OrderByDescending(x => x.CreatedAt).ToListAsync();
        var cNos = contracts.Select(x => x.ContractNo).ToList();

        var details = await db.PerformanceInvoiceDetails
            .Where(x => cNos.Contains(x.ContractNo!))
            .OrderBy(x => x.ContractNo)
            .ThenBy(x => x.RefNo)
            .ToListAsync();

        return new ExportContractOverseaResultVm(
            DateTime.Now,
            contracts.Count,
            details.Count,
            contracts,
            details
        );
    }

    public async Task<ContractOverseaStatsVm> StatsAsync()
    {
        var contracts = await db.ContractOverseas
            .Where(x => x.OrgId == tenant.OrgId)
            .ToListAsync();

        var lcs = await db.LettersOfCredit
            .Where(x => x.OrgId == tenant.OrgId)
            .ToListAsync();

        var totalVehicles = contracts.Where(x => x.Status != ContractOverseaStatus.Cancelled).Sum(x => x.TotalQuantity);
        var totalAmount = contracts.Where(x => x.Status != ContractOverseaStatus.Cancelled).Sum(x => x.TotalAmount);
        var totalLCAmount = lcs.Where(x => x.Status != LCStatus.Cancelled).Sum(x => x.Amount);

        var byPort = contracts
            .GroupBy(x => x.DestinationPort ?? "OTHER")
            .ToDictionary(g => g.Key, g => g.Count());

        var bySupplier = contracts
            .GroupBy(x => x.SupplierCode ?? "OTHER")
            .ToDictionary(g => g.Key, g => g.Count());

        var byStatus = contracts
            .GroupBy(x => x.Status.ToString())
            .ToDictionary(g => g.Key, g => g.Count());

        return new ContractOverseaStatsVm(
            TotalContracts: contracts.Count,
            ActiveContracts: contracts.Count(x => x.Status == ContractOverseaStatus.Active),
            LinkedLCContracts: contracts.Count(x => x.Status == ContractOverseaStatus.LinkedLC),
            CompletedContracts: contracts.Count(x => x.Status == ContractOverseaStatus.Completed),
            CancelledContracts: contracts.Count(x => x.Status == ContractOverseaStatus.Cancelled),
            TotalVehicles: totalVehicles,
            TotalAmountUSD: totalAmount,
            TotalLCAmountUSD: totalLCAmount,
            TotalLCs: lcs.Count,
            ByDestinationPort: byPort,
            BySupplier: bySupplier,
            ByStatus: byStatus
        );
    }
}
