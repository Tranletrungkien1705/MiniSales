using Microsoft.EntityFrameworkCore;
using MiniSales.Data;
using MiniSales.Models;

namespace MiniSales.Services;

public record CreateRetailInvoiceCarDto(
    string? CtrCarId,
    string Vin,
    string? BrandName = "Hyundai",
    string? CarType = "Ô tô con",
    string ModelCode = "",
    string ModelName = "",
    string? SpecCode = null,
    string? SpecDescription = null,
    string? ColorCode = null,
    string? ColorName = null,
    string? EngineNo = null,
    int NumberOfSeats = 5,
    string? HTCInvoiceNo = null,
    DateTime? HTCInvoiceDate = null,
    string? ProductionYearActual = null,
    string? CQNo = null,
    string? FGFormNo = null,
    decimal UPBeforeVAT = 0,
    decimal VatRate = 10m,
    string? Remark = null
);

public record CreateRetailInvoiceProductDto(
    string ProductCode,
    string ProductName,
    string Unit = "Gói",
    int Qty = 1,
    decimal VatRate = 10m,
    decimal UnitPrice = 0,
    bool FlagIsProduct = true,
    string? Remark = null
);

public record CreateRetailInvoiceRequestDto(
    string DealerCode,
    string? DealerName = null,
    string? DlrContractNo = null,
    string? DealNo = null,
    string? CustomerCode = null,
    string CustomerName = "",
    string CustomerType = "INDIVIDUAL",
    string? CustomerAddress = null,
    string? CustomerEmail = null,
    string? CustomerPhone = null,
    string? MST = null,
    string? FormNo = null,
    string? InvoiceSign = null,
    string? Remark = null,
    string? CreatedBy = null,
    List<CreateRetailInvoiceCarDto>? Cars = null,
    List<CreateRetailInvoiceProductDto>? Products = null
);

public record CreateAdjustedRetailInvoiceDto(
    string ReqInvoiceBaseNo,
    RetailInvoiceAdjType AdjType,
    string? Remark = null,
    string? CreatedBy = null,
    List<CreateRetailInvoiceCarDto>? Cars = null,
    List<CreateRetailInvoiceProductDto>? Products = null
);

public record CreateReplacedRetailInvoiceDto(
    string ReqInvoiceBaseNo,
    string? Remark = null,
    string? CreatedBy = null,
    List<CreateRetailInvoiceCarDto>? Cars = null,
    List<CreateRetailInvoiceProductDto>? Products = null
);

public record UpdateRetailInvoiceRequestDto(
    string? CustomerName = null,
    string? CustomerAddress = null,
    string? CustomerEmail = null,
    string? CustomerPhone = null,
    string? MST = null,
    string? Remark = null,
    string? UpdatedBy = null,
    List<CreateRetailInvoiceCarDto>? Cars = null,
    List<CreateRetailInvoiceProductDto>? Products = null
);

public record IssueRetailInvoiceDto(
    string? FormNo = null,
    string? InvoiceSign = null,
    string? InvoiceNo = null,
    DateTime? InvoiceDate = null,
    string? LookupCode = null,
    string? IssuedBy = null
);

public record SignAndTransmitRetailInvoiceDto(
    string? ApprovedBy = null,
    string? TaxAuthorityCode = null,
    string? Remark = null
);

public record CancelRetailInvoiceDto(
    string Reason,
    string? CancelledBy = null
);

public record RetailInvoiceDetailDto(
    long Id,
    string ReqInvoiceNo,
    string CtrCarId,
    string Vin,
    string BrandName,
    string CarType,
    string ModelCode,
    string ModelName,
    string? SpecCode,
    string? SpecDescription,
    string? ColorCode,
    string? ColorName,
    string? EngineNo,
    int NumberOfSeats,
    string? HTCInvoiceNo,
    DateTime? HTCInvoiceDate,
    string? ProductionYearActual,
    string? CQNo,
    string? FGFormNo,
    decimal UPBeforeVAT,
    decimal VatRate,
    decimal ValVAT,
    decimal UPAfterVAT,
    string Status,
    string? Remark
);

public record RetailInvoiceProductDto(
    long Id,
    string ReqInvoiceNo,
    int Idx,
    string ProductCode,
    string ProductName,
    string Unit,
    int Qty,
    decimal VatRate,
    decimal UnitPrice,
    decimal TPBeforeVAT,
    decimal ValVAT,
    decimal TPAfterVAT,
    bool FlagIsProduct,
    string? Remark
);

public record RetailInvoiceRequestDto(
    long Id,
    string ReqInvoiceNo,
    string DealerCode,
    string? DealerName,
    string ReqInvoiceType,
    string? ReqInvoiceBaseNo,
    string? DlrContractNo,
    string? DealNo,
    string CustomerCode,
    string CustomerName,
    string CustomerType,
    string? CustomerAddress,
    string? CustomerEmail,
    string? CustomerPhone,
    string? MST,
    string? TInvoiceCode,
    string? InvoiceCode,
    string? InvoiceNo,
    DateTime? InvoiceDate,
    string? InvoiceSign,
    string? FormNo,
    string InvoiceStatus,
    string? InvoiceBaseNo,
    string AdjType,
    string? Remark,
    string ReqInvoiceStatus,
    string FlagInvoice,
    int FlagIsQInvoice,
    int QtyCtrCarId,
    decimal TotalTPBeforeVAT,
    decimal TotalValVAT,
    decimal TotalTPAfterVAT,
    string? TaxAuthorityCode,
    string? InvoiceFileUrl,
    string? InvoiceFileName,
    string? CreatedBy,
    DateTime CreatedAt,
    string? IssuedBy,
    DateTime? IssuedAt,
    string? ApprovedBy,
    DateTime? ApprovedAt,
    string? TaxTransmittedBy,
    DateTime? TaxTransmittedAt,
    string? CancelReason,
    string? CancelledBy,
    DateTime? CancelledAt,
    List<RetailInvoiceDetailDto> Details,
    List<RetailInvoiceProductDto> Products
);

public record EligibleRetailCarDto(
    string CtrCarId,
    string Vin,
    string Model,
    string? SpecCode,
    string? ColorCode,
    string? EngineNo,
    string ContractNo,
    string CustomerName,
    string CustomerPhone,
    string DealerCode,
    string? DealerName,
    decimal Price,
    string Status
);

public record RetailInvoiceStatsDto(
    int TotalRequests,
    int TotalIssued,
    int TotalPending,
    int TotalAdjusted,
    int TotalReplaced,
    int TotalCancelled,
    int TotalCarsInvoiced,
    decimal TotalBeforeVat,
    decimal TotalVatAmount,
    decimal TotalAfterVat,
    decimal TotalCarAmount,
    decimal TotalProductAmount,
    Dictionary<string, int> ByStatus,
    Dictionary<string, int> ByType,
    Dictionary<string, decimal> ByDealer
);

public interface IRetailInvoiceService
{
    Task<List<RetailInvoiceRequestDto>> ListAsync(
        string? status,
        string? invoiceType,
        string? dealerCode,
        string? customerName,
        string? vin,
        string? reqInvoiceNo,
        DateTime? dateFrom,
        DateTime? dateTo
    );
    Task<RetailInvoiceRequestDto?> GetByNoAsync(string reqInvoiceNo);
    Task<RetailInvoiceRequestDto> CreateBaseAsync(CreateRetailInvoiceRequestDto dto);
    Task<RetailInvoiceRequestDto> CreateAdjustedAsync(CreateAdjustedRetailInvoiceDto dto);
    Task<RetailInvoiceRequestDto> CreateReplacedAsync(CreateReplacedRetailInvoiceDto dto);
    Task<RetailInvoiceRequestDto> UpdateAsync(string reqInvoiceNo, UpdateRetailInvoiceRequestDto dto);
    Task<bool> DeleteDraftAsync(string reqInvoiceNo);
    Task<RetailInvoiceRequestDto> IssueEInvoiceAsync(string reqInvoiceNo, IssueRetailInvoiceDto? dto);
    Task<RetailInvoiceRequestDto> SignAndTransmitAsync(string reqInvoiceNo, SignAndTransmitRetailInvoiceDto? dto);
    Task<RetailInvoiceRequestDto> CancelAsync(string reqInvoiceNo, CancelRetailInvoiceDto dto);
    Task<List<EligibleRetailCarDto>> GetEligibleCarsAsync(string? dealerCode, string? contractNo);
    Task<RetailInvoiceStatsDto> GetStatsAsync();
}

public sealed class RetailInvoiceService(AppDbContext db, ITenantContext tenant) : IRetailInvoiceService
{
    private Guid Org => tenant.OrgId;

    public async Task<List<RetailInvoiceRequestDto>> ListAsync(
        string? status,
        string? invoiceType,
        string? dealerCode,
        string? customerName,
        string? vin,
        string? reqInvoiceNo,
        DateTime? dateFrom,
        DateTime? dateTo)
    {
        var q = db.RetailInvoiceRequests
            .Include(r => r.Details)
            .Include(r => r.Products)
            .AsNoTracking()
            .Where(r => r.OrgId == Org);

        if (!string.IsNullOrWhiteSpace(status))
        {
            if (Enum.TryParse<RetailInvoiceRequestStatus>(status, true, out var st))
                q = q.Where(r => r.ReqInvoiceStatus == st);
        }

        if (!string.IsNullOrWhiteSpace(invoiceType))
        {
            if (Enum.TryParse<RetailInvoiceType>(invoiceType, true, out var it))
                q = q.Where(r => r.ReqInvoiceType == it);
        }

        if (!string.IsNullOrWhiteSpace(dealerCode))
        {
            var d = dealerCode.Trim();
            q = q.Where(r => r.DealerCode == d);
        }

        if (!string.IsNullOrWhiteSpace(customerName))
        {
            var cn = customerName.Trim();
            q = q.Where(r => r.CustomerName.Contains(cn));
        }

        if (!string.IsNullOrWhiteSpace(reqInvoiceNo))
        {
            var no = reqInvoiceNo.Trim();
            q = q.Where(r => r.ReqInvoiceNo.Contains(no));
        }

        if (!string.IsNullOrWhiteSpace(vin))
        {
            var v = vin.Trim();
            q = q.Where(r => r.Details.Any(d => d.Vin.Contains(v)));
        }

        if (dateFrom.HasValue)
            q = q.Where(r => r.CreatedAt >= dateFrom.Value);

        if (dateTo.HasValue)
        {
            var end = dateTo.Value.Date.AddDays(1);
            q = q.Where(r => r.CreatedAt < end);
        }

        var list = await q.OrderByDescending(r => r.CreatedAt).ToListAsync();
        return list.Select(ToDto).ToList();
    }

    public async Task<RetailInvoiceRequestDto?> GetByNoAsync(string reqInvoiceNo)
    {
        if (string.IsNullOrWhiteSpace(reqInvoiceNo)) return null;
        var r = await db.RetailInvoiceRequests
            .Include(x => x.Details)
            .Include(x => x.Products)
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.OrgId == Org && x.ReqInvoiceNo == reqInvoiceNo.Trim());

        return r is null ? null : ToDto(r);
    }

    public async Task<RetailInvoiceRequestDto> CreateBaseAsync(CreateRetailInvoiceRequestDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.DealerCode))
            throw new InvalidOperationException("Mã đại lý (DealerCode) không được để trống.");
        if (string.IsNullOrWhiteSpace(dto.CustomerName))
            throw new InvalidOperationException("Tên khách hàng (CustomerName) không được để trống.");

        var hasCars = dto.Cars != null && dto.Cars.Count > 0;
        var hasProducts = dto.Products != null && dto.Products.Count > 0;
        if (!hasCars && !hasProducts)
            throw new InvalidOperationException("Đề nghị xuất hóa đơn phải có ít nhất 1 dòng xe hoặc 1 dịch vụ/phụ kiện kèm theo.");

        var reqNo = await GenerateReqInvoiceNoAsync();
        var entity = new RetailInvoiceRequest
        {
            OrgId = Org,
            ReqInvoiceNo = reqNo,
            DealerCode = dto.DealerCode.Trim(),
            DealerName = dto.DealerName?.Trim() ?? GetDealerName(dto.DealerCode.Trim()),
            ReqInvoiceType = RetailInvoiceType.Root,
            ReqInvoiceBaseNo = null,
            DlrContractNo = dto.DlrContractNo?.Trim(),
            DealNo = dto.DealNo?.Trim(),
            CustomerCode = dto.CustomerCode?.Trim() ?? "KH" + DateTime.Now.ToString("yyMMddHHmmss"),
            CustomerName = dto.CustomerName.Trim(),
            CustomerType = dto.CustomerType?.Trim().ToUpperInvariant() ?? "INDIVIDUAL",
            CustomerAddress = dto.CustomerAddress?.Trim(),
            CustomerEmail = dto.CustomerEmail?.Trim(),
            CustomerPhone = dto.CustomerPhone?.Trim(),
            MST = dto.MST?.Trim(),
            TInvoiceCode = "1/001",
            InvoiceSign = dto.InvoiceSign?.Trim() ?? "1C" + DateTime.Now.ToString("yy") + "TAA",
            FormNo = dto.FormNo?.Trim() ?? "1/001",
            InvoiceStatus = RetailInvoiceEInvoiceStatus.Draft,
            AdjType = RetailInvoiceAdjType.None,
            Remark = dto.Remark?.Trim() ?? "Đề nghị xuất hóa đơn GTGT bán lẻ xe ô tô và phụ kiện",
            ReqInvoiceStatus = RetailInvoiceRequestStatus.Pending,
            FlagInvoice = "0",
            FlagIsQInvoice = 1,
            CreatedBy = dto.CreatedBy?.Trim() ?? "DEALER_USER",
            CreatedAt = DateTime.Now
        };

        PopulateChildrenAndTotals(entity, dto.Cars, dto.Products);

        db.RetailInvoiceRequests.Add(entity);
        await db.SaveChangesAsync();

        return ToDto(entity);
    }

    public async Task<RetailInvoiceRequestDto> CreateAdjustedAsync(CreateAdjustedRetailInvoiceDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.ReqInvoiceBaseNo))
            throw new InvalidOperationException("Số đề nghị hóa đơn gốc (ReqInvoiceBaseNo) không được để trống khi lập hóa đơn điều chỉnh.");

        var baseInv = await db.RetailInvoiceRequests
            .Include(r => r.Details)
            .Include(r => r.Products)
            .FirstOrDefaultAsync(r => r.OrgId == Org && r.ReqInvoiceNo == dto.ReqInvoiceBaseNo.Trim());

        if (baseInv is null)
            throw new InvalidOperationException($"Không tìm thấy hóa đơn gốc '{dto.ReqInvoiceBaseNo}'.");

        if (baseInv.ReqInvoiceStatus != RetailInvoiceRequestStatus.Issued && baseInv.InvoiceStatus != RetailInvoiceEInvoiceStatus.Issued)
            throw new InvalidOperationException($"Hóa đơn gốc '{dto.ReqInvoiceBaseNo}' phải ở trạng thái Đã phát hành (Issued) mới được lập đề nghị điều chỉnh.");

        var reqNo = await GenerateReqInvoiceNoAsync();
        var entity = new RetailInvoiceRequest
        {
            OrgId = Org,
            ReqInvoiceNo = reqNo,
            DealerCode = baseInv.DealerCode,
            DealerName = baseInv.DealerName,
            ReqInvoiceType = RetailInvoiceType.Adjusted,
            ReqInvoiceBaseNo = baseInv.ReqInvoiceNo,
            InvoiceBaseNo = baseInv.InvoiceNo,
            DlrContractNo = baseInv.DlrContractNo,
            DealNo = baseInv.DealNo,
            CustomerCode = baseInv.CustomerCode,
            CustomerName = baseInv.CustomerName,
            CustomerType = baseInv.CustomerType,
            CustomerAddress = baseInv.CustomerAddress,
            CustomerEmail = baseInv.CustomerEmail,
            CustomerPhone = baseInv.CustomerPhone,
            MST = baseInv.MST,
            TInvoiceCode = baseInv.TInvoiceCode,
            InvoiceSign = baseInv.InvoiceSign,
            FormNo = baseInv.FormNo,
            InvoiceStatus = RetailInvoiceEInvoiceStatus.Draft,
            AdjType = dto.AdjType,
            Remark = dto.Remark?.Trim() ?? $"Điều chỉnh hóa đơn số {baseInv.InvoiceNo ?? baseInv.ReqInvoiceNo}",
            ReqInvoiceStatus = RetailInvoiceRequestStatus.Pending,
            FlagInvoice = "0",
            FlagIsQInvoice = baseInv.FlagIsQInvoice,
            CreatedBy = dto.CreatedBy?.Trim() ?? "DEALER_USER",
            CreatedAt = DateTime.Now
        };

        // Nếu người dùng không truyền chi tiết mới, kế thừa từ hóa đơn gốc
        var cars = dto.Cars ?? baseInv.Details.Select(d => new CreateRetailInvoiceCarDto(
            d.CtrCarId, d.Vin, d.BrandName, d.CarType, d.ModelCode, d.ModelName,
            d.SpecCode, d.SpecDescription, d.ColorCode, d.ColorName, d.EngineNo,
            d.NumberOfSeats, d.HTCInvoiceNo, d.HTCInvoiceDate, d.ProductionYearActual,
            d.CQNo, d.FGFormNo, d.UPBeforeVAT, d.VatRate, d.Remark
        )).ToList();

        var prds = dto.Products ?? baseInv.Products.Select(p => new CreateRetailInvoiceProductDto(
            p.ProductCode, p.ProductName, p.Unit, p.Qty, p.VatRate, p.UnitPrice, p.FlagIsProduct, p.Remark
        )).ToList();

        PopulateChildrenAndTotals(entity, cars, prds);

        db.RetailInvoiceRequests.Add(entity);
        await db.SaveChangesAsync();

        return ToDto(entity);
    }

    public async Task<RetailInvoiceRequestDto> CreateReplacedAsync(CreateReplacedRetailInvoiceDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.ReqInvoiceBaseNo))
            throw new InvalidOperationException("Số đề nghị hóa đơn gốc (ReqInvoiceBaseNo) không được để trống khi lập hóa đơn thay thế.");

        var baseInv = await db.RetailInvoiceRequests
            .Include(r => r.Details)
            .Include(r => r.Products)
            .FirstOrDefaultAsync(r => r.OrgId == Org && r.ReqInvoiceNo == dto.ReqInvoiceBaseNo.Trim());

        if (baseInv is null)
            throw new InvalidOperationException($"Không tìm thấy hóa đơn gốc '{dto.ReqInvoiceBaseNo}'.");

        if (baseInv.ReqInvoiceStatus != RetailInvoiceRequestStatus.Issued && baseInv.InvoiceStatus != RetailInvoiceEInvoiceStatus.Issued)
            throw new InvalidOperationException($"Hóa đơn gốc '{dto.ReqInvoiceBaseNo}' phải ở trạng thái Đã phát hành (Issued) mới được lập đề nghị thay thế.");

        var reqNo = await GenerateReqInvoiceNoAsync();
        var entity = new RetailInvoiceRequest
        {
            OrgId = Org,
            ReqInvoiceNo = reqNo,
            DealerCode = baseInv.DealerCode,
            DealerName = baseInv.DealerName,
            ReqInvoiceType = RetailInvoiceType.Replaced,
            ReqInvoiceBaseNo = baseInv.ReqInvoiceNo,
            InvoiceBaseNo = baseInv.InvoiceNo,
            DlrContractNo = baseInv.DlrContractNo,
            DealNo = baseInv.DealNo,
            CustomerCode = baseInv.CustomerCode,
            CustomerName = baseInv.CustomerName,
            CustomerType = baseInv.CustomerType,
            CustomerAddress = baseInv.CustomerAddress,
            CustomerEmail = baseInv.CustomerEmail,
            CustomerPhone = baseInv.CustomerPhone,
            MST = baseInv.MST,
            TInvoiceCode = baseInv.TInvoiceCode,
            InvoiceSign = baseInv.InvoiceSign,
            FormNo = baseInv.FormNo,
            InvoiceStatus = RetailInvoiceEInvoiceStatus.Draft,
            AdjType = RetailInvoiceAdjType.None,
            Remark = dto.Remark?.Trim() ?? $"Thay thế cho hóa đơn số {baseInv.InvoiceNo ?? baseInv.ReqInvoiceNo} bị sai sót thông tin",
            ReqInvoiceStatus = RetailInvoiceRequestStatus.Pending,
            FlagInvoice = "0",
            FlagIsQInvoice = baseInv.FlagIsQInvoice,
            CreatedBy = dto.CreatedBy?.Trim() ?? "DEALER_USER",
            CreatedAt = DateTime.Now
        };

        var cars = dto.Cars ?? baseInv.Details.Select(d => new CreateRetailInvoiceCarDto(
            d.CtrCarId, d.Vin, d.BrandName, d.CarType, d.ModelCode, d.ModelName,
            d.SpecCode, d.SpecDescription, d.ColorCode, d.ColorName, d.EngineNo,
            d.NumberOfSeats, d.HTCInvoiceNo, d.HTCInvoiceDate, d.ProductionYearActual,
            d.CQNo, d.FGFormNo, d.UPBeforeVAT, d.VatRate, d.Remark
        )).ToList();

        var prds = dto.Products ?? baseInv.Products.Select(p => new CreateRetailInvoiceProductDto(
            p.ProductCode, p.ProductName, p.Unit, p.Qty, p.VatRate, p.UnitPrice, p.FlagIsProduct, p.Remark
        )).ToList();

        PopulateChildrenAndTotals(entity, cars, prds);

        db.RetailInvoiceRequests.Add(entity);
        await db.SaveChangesAsync();

        return ToDto(entity);
    }

    public async Task<RetailInvoiceRequestDto> UpdateAsync(string reqInvoiceNo, UpdateRetailInvoiceRequestDto dto)
    {
        var entity = await db.RetailInvoiceRequests
            .Include(r => r.Details)
            .Include(r => r.Products)
            .FirstOrDefaultAsync(r => r.OrgId == Org && r.ReqInvoiceNo == reqInvoiceNo.Trim());

        if (entity is null)
            throw new InvalidOperationException($"Không tìm thấy đề nghị xuất hóa đơn '{reqInvoiceNo}'.");

        if (entity.ReqInvoiceStatus != RetailInvoiceRequestStatus.Pending)
            throw new InvalidOperationException($"Chỉ cho phép cập nhật đề nghị ở trạng thái Mới tạo (Pending). Trạng thái hiện tại: {entity.ReqInvoiceStatus}.");

        if (!string.IsNullOrWhiteSpace(dto.CustomerName)) entity.CustomerName = dto.CustomerName.Trim();
        if (dto.CustomerAddress != null) entity.CustomerAddress = dto.CustomerAddress.Trim();
        if (dto.CustomerEmail != null) entity.CustomerEmail = dto.CustomerEmail.Trim();
        if (dto.CustomerPhone != null) entity.CustomerPhone = dto.CustomerPhone.Trim();
        if (dto.MST != null) entity.MST = dto.MST.Trim();
        if (dto.Remark != null) entity.Remark = dto.Remark.Trim();

        entity.LUBy = dto.UpdatedBy?.Trim() ?? "DEALER_USER";
        entity.LUDateTime = DateTime.Now;

        if (dto.Cars != null || dto.Products != null)
        {
            db.RetailInvoiceRequestDetails.RemoveRange(entity.Details);
            db.RetailInvoiceRequestProducts.RemoveRange(entity.Products);
            entity.Details.Clear();
            entity.Products.Clear();

            PopulateChildrenAndTotals(entity, dto.Cars, dto.Products);
        }

        await db.SaveChangesAsync();
        return ToDto(entity);
    }

    public async Task<bool> DeleteDraftAsync(string reqInvoiceNo)
    {
        var entity = await db.RetailInvoiceRequests
            .Include(r => r.Details)
            .Include(r => r.Products)
            .FirstOrDefaultAsync(r => r.OrgId == Org && r.ReqInvoiceNo == reqInvoiceNo.Trim());

        if (entity is null) return false;

        if (entity.ReqInvoiceStatus != RetailInvoiceRequestStatus.Pending || entity.InvoiceStatus != RetailInvoiceEInvoiceStatus.Draft)
            throw new InvalidOperationException($"Không thể xóa đề nghị xuất hóa đơn '{reqInvoiceNo}' vì không còn ở trạng thái Nháp (Draft/Pending).");

        db.RetailInvoiceRequests.Remove(entity);
        await db.SaveChangesAsync();
        return true;
    }

    public async Task<RetailInvoiceRequestDto> IssueEInvoiceAsync(string reqInvoiceNo, IssueRetailInvoiceDto? dto)
    {
        var entity = await db.RetailInvoiceRequests
            .Include(r => r.Details)
            .Include(r => r.Products)
            .FirstOrDefaultAsync(r => r.OrgId == Org && r.ReqInvoiceNo == reqInvoiceNo.Trim());

        if (entity is null)
            throw new InvalidOperationException($"Không tìm thấy đề nghị xuất hóa đơn '{reqInvoiceNo}'.");

        if (entity.ReqInvoiceStatus != RetailInvoiceRequestStatus.Pending)
            throw new InvalidOperationException($"Chỉ cấp số hóa đơn điện tử cho đề nghị đang ở trạng thái Pending. Trạng thái hiện tại: {entity.ReqInvoiceStatus}.");

        if (entity.InvoiceStatus != RetailInvoiceEInvoiceStatus.Draft)
            throw new InvalidOperationException($"Hóa đơn điện tử đã được cấp số hoặc xử lý trước đó. Trạng thái hiện tại: {entity.InvoiceStatus}.");

        // Cấp số hóa đơn chính thức (7 chữ số theo chuẩn cơ quan thuế e-Invoice QInvoice)
        var nextSeq = await db.RetailInvoiceRequests
            .Where(r => r.OrgId == Org && r.InvoiceNo != null)
            .CountAsync() + 1;

        var invNo = dto?.InvoiceNo?.Trim() ?? nextSeq.ToString("D7");
        var lookupCode = dto?.LookupCode?.Trim() ?? Guid.NewGuid().ToString("N")[..12].ToUpperInvariant();

        entity.InvoiceNo = invNo;
        entity.InvoiceCode = lookupCode;
        entity.InvoiceDate = dto?.InvoiceDate ?? DateTime.Today;
        if (!string.IsNullOrWhiteSpace(dto?.FormNo)) entity.FormNo = dto.FormNo.Trim();
        if (!string.IsNullOrWhiteSpace(dto?.InvoiceSign)) entity.InvoiceSign = dto.InvoiceSign.Trim();
        entity.InvoiceStatus = RetailInvoiceEInvoiceStatus.Pending; // Đã cấp số, chờ ký số và gửi thuế
        entity.IssuedBy = dto?.IssuedBy?.Trim() ?? "KE_TOAN_HOA_DON";
        entity.IssuedAt = DateTime.Now;
        entity.LUBy = entity.IssuedBy;
        entity.LUDateTime = DateTime.Now;

        await db.SaveChangesAsync();
        return ToDto(entity);
    }

    public async Task<RetailInvoiceRequestDto> SignAndTransmitAsync(string reqInvoiceNo, SignAndTransmitRetailInvoiceDto? dto)
    {
        var entity = await db.RetailInvoiceRequests
            .Include(r => r.Details)
            .Include(r => r.Products)
            .FirstOrDefaultAsync(r => r.OrgId == Org && r.ReqInvoiceNo == reqInvoiceNo.Trim());

        if (entity is null)
            throw new InvalidOperationException($"Không tìm thấy đề nghị xuất hóa đơn '{reqInvoiceNo}'.");

        if (string.IsNullOrWhiteSpace(entity.InvoiceNo))
            throw new InvalidOperationException($"Đề nghị '{reqInvoiceNo}' chưa được cấp số hóa đơn điện tử (gọi /issue trước khi ký và truyền thuế).");

        if (entity.InvoiceStatus != RetailInvoiceEInvoiceStatus.Pending && entity.InvoiceStatus != RetailInvoiceEInvoiceStatus.Draft)
            throw new InvalidOperationException($"Hóa đơn không ở trạng thái chờ ký/truyền. Trạng thái hiện tại: {entity.InvoiceStatus}.");

        var approver = dto?.ApprovedBy?.Trim() ?? "GIAM_DOC_DAI_LY";
        entity.ApprovedBy = approver;
        entity.ApprovedAt = DateTime.Now;

        // Mô phỏng truyền dữ liệu cơ quan thuế Tổng cục Thuế (TCT)
        var tctCode = dto?.TaxAuthorityCode?.Trim() ?? "TCT" + DateTime.Now.ToString("yyMMdd") + Guid.NewGuid().ToString("N")[..8].ToUpperInvariant();
        entity.TaxAuthorityCode = tctCode;
        entity.TaxTransmittedBy = approver;
        entity.TaxTransmittedAt = DateTime.Now;

        entity.InvoiceStatus = RetailInvoiceEInvoiceStatus.Issued;
        entity.ReqInvoiceStatus = RetailInvoiceRequestStatus.Issued;
        entity.FlagInvoice = "1";
        entity.InvoiceFileName = $"{entity.ReqInvoiceNo}_{entity.InvoiceNo}.pdf";
        entity.InvoiceFileUrl = $"/invoices/retail/{entity.ReqInvoiceNo}.pdf";

        if (!string.IsNullOrWhiteSpace(dto?.Remark))
            entity.Remark = (entity.Remark ?? "") + " | " + dto.Remark.Trim();

        entity.LUBy = approver;
        entity.LUDateTime = DateTime.Now;

        // Cập nhật trạng thái từng dòng xe
        foreach (var car in entity.Details)
        {
            car.Status = "Invoiced";
        }

        // Nếu đây là hóa đơn Điều chỉnh hoặc Thay thế, cập nhật trạng thái hóa đơn gốc
        if (!string.IsNullOrWhiteSpace(entity.ReqInvoiceBaseNo))
        {
            var baseInv = await db.RetailInvoiceRequests.FirstOrDefaultAsync(r => r.OrgId == Org && r.ReqInvoiceNo == entity.ReqInvoiceBaseNo);
            if (baseInv != null)
            {
                if (entity.ReqInvoiceType == RetailInvoiceType.Adjusted)
                {
                    baseInv.ReqInvoiceStatus = RetailInvoiceRequestStatus.Adjusted;
                    baseInv.InvoiceStatus = RetailInvoiceEInvoiceStatus.Approved;
                    baseInv.Remark = (baseInv.Remark ?? "") + $" | Bị điều chỉnh bởi HĐ {entity.InvoiceNo} ({entity.ReqInvoiceNo})";
                }
                else if (entity.ReqInvoiceType == RetailInvoiceType.Replaced)
                {
                    baseInv.ReqInvoiceStatus = RetailInvoiceRequestStatus.Replaced;
                    baseInv.InvoiceStatus = RetailInvoiceEInvoiceStatus.Cancelled;
                    baseInv.Remark = (baseInv.Remark ?? "") + $" | Bị thay thế bởi HĐ {entity.InvoiceNo} ({entity.ReqInvoiceNo})";
                }
            }
        }

        await db.SaveChangesAsync();
        return ToDto(entity);
    }

    public async Task<RetailInvoiceRequestDto> CancelAsync(string reqInvoiceNo, CancelRetailInvoiceDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Reason))
            throw new InvalidOperationException("Lý do hủy hóa đơn (Reason) không được để trống.");

        var entity = await db.RetailInvoiceRequests
            .Include(r => r.Details)
            .Include(r => r.Products)
            .FirstOrDefaultAsync(r => r.OrgId == Org && r.ReqInvoiceNo == reqInvoiceNo.Trim());

        if (entity is null)
            throw new InvalidOperationException($"Không tìm thấy đề nghị xuất hóa đơn '{reqInvoiceNo}'.");

        if (entity.ReqInvoiceStatus == RetailInvoiceRequestStatus.Cancelled || entity.InvoiceStatus == RetailInvoiceEInvoiceStatus.Cancelled)
            throw new InvalidOperationException($"Đề nghị xuất hóa đơn '{reqInvoiceNo}' đã ở trạng thái Đã hủy trước đó.");

        entity.ReqInvoiceStatus = RetailInvoiceRequestStatus.Cancelled;
        entity.InvoiceStatus = RetailInvoiceEInvoiceStatus.Cancelled;
        entity.CancelReason = dto.Reason.Trim();
        entity.CancelledBy = dto.CancelledBy?.Trim() ?? "DEALER_ADMIN";
        entity.CancelledAt = DateTime.Now;
        entity.LUBy = entity.CancelledBy;
        entity.LUDateTime = DateTime.Now;

        foreach (var car in entity.Details)
        {
            car.Status = "Cancelled";
        }

        await db.SaveChangesAsync();
        return ToDto(entity);
    }

    public async Task<List<EligibleRetailCarDto>> GetEligibleCarsAsync(string? dealerCode, string? contractNo)
    {
        // Tra cứu xe từ hợp đồng bán lẻ DlrRetailContractCar
        var qCars = db.RetailContractCars
            .AsNoTracking()
            .Join(db.RetailContracts, c => c.ContractId, r => r.Id, (c, r) => new { Car = c, Contract = r })
            .Where(x => x.Contract.OrgId == Org && x.Car.Vin != null && x.Car.Vin != "");

        if (!string.IsNullOrWhiteSpace(dealerCode))
        {
            var d = dealerCode.Trim();
            qCars = qCars.Where(x => x.Contract.DealerCode == d);
        }

        if (!string.IsNullOrWhiteSpace(contractNo))
        {
            var cNo = contractNo.Trim();
            qCars = qCars.Where(x => x.Contract.ContractNo == cNo);
        }

        var contractCars = await qCars.ToListAsync();

        // Lấy danh sách VIN đã xuất hóa đơn hợp lệ (chưa bị hủy)
        var invoicedVins = await db.RetailInvoiceRequestDetails
            .Where(d => d.Status != "Cancelled")
            .Select(d => d.Vin)
            .Distinct()
            .ToListAsync();

        var result = new List<EligibleRetailCarDto>();
        foreach (var item in contractCars)
        {
            if (item.Car.Vin != null && invoicedVins.Contains(item.Car.Vin))
                continue;

            result.Add(new EligibleRetailCarDto(
                CtrCarId: item.Car.CtrCarId,
                Vin: item.Car.Vin ?? "",
                Model: item.Car.Model,
                SpecCode: item.Car.SpecCode,
                ColorCode: item.Car.ColorCode,
                EngineNo: null,
                ContractNo: item.Contract.ContractNo,
                CustomerName: item.Contract.CustomerName,
                CustomerPhone: item.Contract.CustomerPhone,
                DealerCode: item.Contract.DealerCode,
                DealerName: item.Contract.DealerName,
                Price: item.Contract.TotalAmount,
                Status: item.Car.Status.ToString()
            ));
        }

        return result;
    }

    public async Task<RetailInvoiceStatsDto> GetStatsAsync()
    {
        var all = await db.RetailInvoiceRequests
            .Include(r => r.Details)
            .Include(r => r.Products)
            .AsNoTracking()
            .Where(r => r.OrgId == Org)
            .ToListAsync();

        var totalRequests = all.Count;
        var totalIssued = all.Count(r => r.ReqInvoiceStatus == RetailInvoiceRequestStatus.Issued);
        var totalPending = all.Count(r => r.ReqInvoiceStatus == RetailInvoiceRequestStatus.Pending);
        var totalAdjusted = all.Count(r => r.ReqInvoiceType == RetailInvoiceType.Adjusted);
        var totalReplaced = all.Count(r => r.ReqInvoiceType == RetailInvoiceType.Replaced);
        var totalCancelled = all.Count(r => r.ReqInvoiceStatus == RetailInvoiceRequestStatus.Cancelled);

        var totalCarsInvoiced = all.Where(r => r.ReqInvoiceStatus == RetailInvoiceRequestStatus.Issued).Sum(r => r.QtyCtrCarId);
        var totalBeforeVat = all.Where(r => r.ReqInvoiceStatus == RetailInvoiceRequestStatus.Issued).Sum(r => r.TotalTPBeforeVAT);
        var totalVatAmount = all.Where(r => r.ReqInvoiceStatus == RetailInvoiceRequestStatus.Issued).Sum(r => r.TotalValVAT);
        var totalAfterVat = all.Where(r => r.ReqInvoiceStatus == RetailInvoiceRequestStatus.Issued).Sum(r => r.TotalTPAfterVAT);

        var totalCarAmount = all.Where(r => r.ReqInvoiceStatus == RetailInvoiceRequestStatus.Issued)
            .SelectMany(r => r.Details)
            .Sum(d => d.UPAfterVAT);

        var totalProductAmount = all.Where(r => r.ReqInvoiceStatus == RetailInvoiceRequestStatus.Issued)
            .SelectMany(r => r.Products)
            .Sum(p => p.TPAfterVAT);

        var byStatus = all.GroupBy(r => r.ReqInvoiceStatus.ToString())
            .ToDictionary(g => g.Key, g => g.Count());

        var byType = all.GroupBy(r => r.ReqInvoiceType.ToString())
            .ToDictionary(g => g.Key, g => g.Count());

        var byDealer = all.GroupBy(r => r.DealerCode)
            .ToDictionary(g => g.Key, g => g.Sum(r => r.TotalTPAfterVAT));

        return new RetailInvoiceStatsDto(
            TotalRequests: totalRequests,
            TotalIssued: totalIssued,
            TotalPending: totalPending,
            TotalAdjusted: totalAdjusted,
            TotalReplaced: totalReplaced,
            TotalCancelled: totalCancelled,
            TotalCarsInvoiced: totalCarsInvoiced,
            TotalBeforeVat: totalBeforeVat,
            TotalVatAmount: totalVatAmount,
            TotalAfterVat: totalAfterVat,
            TotalCarAmount: totalCarAmount,
            TotalProductAmount: totalProductAmount,
            ByStatus: byStatus,
            ByType: byType,
            ByDealer: byDealer
        );
    }

    private static void PopulateChildrenAndTotals(
        RetailInvoiceRequest entity,
        List<CreateRetailInvoiceCarDto>? cars,
        List<CreateRetailInvoiceProductDto>? products)
    {
        decimal carBeforeVat = 0;
        decimal carVat = 0;
        decimal carAfterVat = 0;

        if (cars != null)
        {
            foreach (var c in cars)
            {
                var vat = Math.Round(c.UPBeforeVAT * (c.VatRate / 100m), 0);
                var total = c.UPBeforeVAT + vat;
                carBeforeVat += c.UPBeforeVAT;
                carVat += vat;
                carAfterVat += total;

                entity.Details.Add(new RetailInvoiceRequestDetail
                {
                    ReqInvoiceNo = entity.ReqInvoiceNo,
                    CtrCarId = c.CtrCarId ?? ("CAR" + Guid.NewGuid().ToString("N")[..8].ToUpperInvariant()),
                    Vin = c.Vin.Trim().ToUpperInvariant(),
                    BrandName = string.IsNullOrWhiteSpace(c.BrandName) ? "Hyundai" : c.BrandName.Trim(),
                    CarType = string.IsNullOrWhiteSpace(c.CarType) ? "Ô tô con" : c.CarType.Trim(),
                    ModelCode = c.ModelCode.Trim(),
                    ModelName = string.IsNullOrWhiteSpace(c.ModelName) ? c.ModelCode.Trim() : c.ModelName.Trim(),
                    SpecCode = c.SpecCode?.Trim(),
                    SpecDescription = c.SpecDescription?.Trim(),
                    ColorCode = c.ColorCode?.Trim(),
                    ColorName = c.ColorName?.Trim(),
                    EngineNo = c.EngineNo?.Trim(),
                    NumberOfSeats = c.NumberOfSeats > 0 ? c.NumberOfSeats : 5,
                    HTCInvoiceNo = c.HTCInvoiceNo?.Trim(),
                    HTCInvoiceDate = c.HTCInvoiceDate,
                    ProductionYearActual = c.ProductionYearActual?.Trim() ?? DateTime.Today.Year.ToString(),
                    CQNo = c.CQNo?.Trim(),
                    FGFormNo = c.FGFormNo?.Trim(),
                    UPBeforeVAT = c.UPBeforeVAT,
                    VatRate = c.VatRate,
                    ValVAT = vat,
                    UPAfterVAT = total,
                    Status = "Pending",
                    Remark = c.Remark?.Trim()
                });
            }
        }

        decimal prdBeforeVat = 0;
        decimal prdVat = 0;
        decimal prdAfterVat = 0;

        if (products != null)
        {
            var idx = 1;
            foreach (var p in products)
            {
                var tpBefore = p.Qty * p.UnitPrice;
                var vat = Math.Round(tpBefore * (p.VatRate / 100m), 0);
                var total = tpBefore + vat;

                prdBeforeVat += tpBefore;
                prdVat += vat;
                prdAfterVat += total;

                entity.Products.Add(new RetailInvoiceRequestProduct
                {
                    ReqInvoiceNo = entity.ReqInvoiceNo,
                    Idx = idx++,
                    ProductCode = p.ProductCode.Trim(),
                    ProductName = p.ProductName.Trim(),
                    Unit = string.IsNullOrWhiteSpace(p.Unit) ? "Gói" : p.Unit.Trim(),
                    Qty = p.Qty > 0 ? p.Qty : 1,
                    VatRate = p.VatRate,
                    UnitPrice = p.UnitPrice,
                    TPBeforeVAT = tpBefore,
                    ValVAT = vat,
                    TPAfterVAT = total,
                    FlagIsProduct = p.FlagIsProduct,
                    Remark = p.Remark?.Trim()
                });
            }
        }

        entity.QtyCtrCarId = entity.Details.Count;
        entity.TotalTPBeforeVAT = carBeforeVat + prdBeforeVat;
        entity.TotalValVAT = carVat + prdVat;
        entity.TotalTPAfterVAT = carAfterVat + prdAfterVat;
    }

    private async Task<string> GenerateReqInvoiceNoAsync()
    {
        var prefix = DateTime.Now.ToString("yyMM") + "RI";
        var count = await db.RetailInvoiceRequests
            .Where(r => r.OrgId == Org && r.ReqInvoiceNo.StartsWith(prefix))
            .CountAsync() + 1;
        return $"{prefix}{count:D4}";
    }

    private static string GetDealerName(string code) => code switch
    {
        "VN001" => "Hyundai Đông Đô",
        "VN002" => "Hyundai Nam Trung",
        "VN003" => "Hyundai Tây Hồ",
        "VS058" => "Hyundai Bình Dương",
        _ => "Đại lý Hyundai Ủy quyền " + code
    };

    private static RetailInvoiceRequestDto ToDto(RetailInvoiceRequest r) => new(
        Id: r.Id,
        ReqInvoiceNo: r.ReqInvoiceNo,
        DealerCode: r.DealerCode,
        DealerName: r.DealerName,
        ReqInvoiceType: r.ReqInvoiceType.ToString(),
        ReqInvoiceBaseNo: r.ReqInvoiceBaseNo,
        DlrContractNo: r.DlrContractNo,
        DealNo: r.DealNo,
        CustomerCode: r.CustomerCode,
        CustomerName: r.CustomerName,
        CustomerType: r.CustomerType,
        CustomerAddress: r.CustomerAddress,
        CustomerEmail: r.CustomerEmail,
        CustomerPhone: r.CustomerPhone,
        MST: r.MST,
        TInvoiceCode: r.TInvoiceCode,
        InvoiceCode: r.InvoiceCode,
        InvoiceNo: r.InvoiceNo,
        InvoiceDate: r.InvoiceDate,
        InvoiceSign: r.InvoiceSign,
        FormNo: r.FormNo,
        InvoiceStatus: r.InvoiceStatus.ToString(),
        InvoiceBaseNo: r.InvoiceBaseNo,
        AdjType: r.AdjType.ToString(),
        Remark: r.Remark,
        ReqInvoiceStatus: r.ReqInvoiceStatus.ToString(),
        FlagInvoice: r.FlagInvoice,
        FlagIsQInvoice: r.FlagIsQInvoice,
        QtyCtrCarId: r.QtyCtrCarId,
        TotalTPBeforeVAT: r.TotalTPBeforeVAT,
        TotalValVAT: r.TotalValVAT,
        TotalTPAfterVAT: r.TotalTPAfterVAT,
        TaxAuthorityCode: r.TaxAuthorityCode,
        InvoiceFileUrl: r.InvoiceFileUrl,
        InvoiceFileName: r.InvoiceFileName,
        CreatedBy: r.CreatedBy,
        CreatedAt: r.CreatedAt,
        IssuedBy: r.IssuedBy,
        IssuedAt: r.IssuedAt,
        ApprovedBy: r.ApprovedBy,
        ApprovedAt: r.ApprovedAt,
        TaxTransmittedBy: r.TaxTransmittedBy,
        TaxTransmittedAt: r.TaxTransmittedAt,
        CancelReason: r.CancelReason,
        CancelledBy: r.CancelledBy,
        CancelledAt: r.CancelledAt,
        Details: r.Details.Select(d => new RetailInvoiceDetailDto(
            Id: d.Id,
            ReqInvoiceNo: d.ReqInvoiceNo,
            CtrCarId: d.CtrCarId,
            Vin: d.Vin,
            BrandName: d.BrandName,
            CarType: d.CarType,
            ModelCode: d.ModelCode,
            ModelName: d.ModelName,
            SpecCode: d.SpecCode,
            SpecDescription: d.SpecDescription,
            ColorCode: d.ColorCode,
            ColorName: d.ColorName,
            EngineNo: d.EngineNo,
            NumberOfSeats: d.NumberOfSeats,
            HTCInvoiceNo: d.HTCInvoiceNo,
            HTCInvoiceDate: d.HTCInvoiceDate,
            ProductionYearActual: d.ProductionYearActual,
            CQNo: d.CQNo,
            FGFormNo: d.FGFormNo,
            UPBeforeVAT: d.UPBeforeVAT,
            VatRate: d.VatRate,
            ValVAT: d.ValVAT,
            UPAfterVAT: d.UPAfterVAT,
            Status: d.Status,
            Remark: d.Remark
        )).ToList(),
        Products: r.Products.Select(p => new RetailInvoiceProductDto(
            Id: p.Id,
            ReqInvoiceNo: p.ReqInvoiceNo,
            Idx: p.Idx,
            ProductCode: p.ProductCode,
            ProductName: p.ProductName,
            Unit: p.Unit,
            Qty: p.Qty,
            VatRate: p.VatRate,
            UnitPrice: p.UnitPrice,
            TPBeforeVAT: p.TPBeforeVAT,
            ValVAT: p.ValVAT,
            TPAfterVAT: p.TPAfterVAT,
            FlagIsProduct: p.FlagIsProduct,
            Remark: p.Remark
        )).ToList()
    );
}
