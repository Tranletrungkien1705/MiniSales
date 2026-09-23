using Microsoft.EntityFrameworkCore;
using MiniSales.Data;
using MiniSales.Models;

namespace MiniSales.Services;

public record CreateCarVinProfileDto(
    string Vin,
    string DealerCode,
    string ModelCode,
    string? CarId = null,
    string? DealerName = null,
    string? ModelName = null,
    string? SpecCode = null,
    string? SpecDescription = null,
    string? ColorCode = null,
    string? ColorName = null,
    string? EngineNo = null,
    string? StorageCode = null,
    string? StorageName = null,
    string? CQNo = null,
    DateTime? CQStartDate = null,
    DateTime? CQEndDate = null,
    string? FGFormNo = null,
    string? QCNo = null,
    DateTime? IssuedDate = null,
    string? CONo = null,
    DateTime? CODate = null,
    DateTime? CODateExpected = null,
    string? InvoiceNoFactory = null,
    DateTime? InvoiceFactoryDate = null,
    string? InvoiceFactorySearch = null,
    string? InvoiceNoTransferred = null,
    DateTime? InvoiceTransferredDate = null,
    string? InvoiceTransferredSearch = null,
    string? InvoiceSpecName = null,
    bool TypeCB = false,
    string? LoaiThung = null,
    string? CabinCertificateNo = null,
    DateTime? CabinCertificateDate = null,
    string? CabinCONo = null,
    string? CabinInvoiceNo = null,
    DateTime? CabinInvoiceDate = null,
    CarMortageStatus MortageStatus = CarMortageStatus.Free,
    string? MortageBankCode = null,
    string? MortageBankName = null,
    DateTime? MortageStartDate = null,
    string? HandOverBankCode = null,
    string? HandOverBankName = null,
    string? BillNo = null,
    DateTime? MortageEndDate = null,
    string? GuaranteeNo = null,
    string? BankGuaranteeNo = null,
    DateTime? DateStart = null,
    DateTime? DateExpired = null,
    decimal? GuaranteeValue = null,
    string DocumentsStatus = "0",
    string FlagDocReq = "0",
    string? Remark = null,
    string? CreatedBy = null
);

public record UpdateCarVinProfileDto(
    string? CarId = null,
    string? DealerCode = null,
    string? DealerName = null,
    string? ModelCode = null,
    string? ModelName = null,
    string? SpecCode = null,
    string? SpecDescription = null,
    string? ColorCode = null,
    string? ColorName = null,
    string? EngineNo = null,
    string? StorageCode = null,
    string? StorageName = null,
    string? CQNo = null,
    DateTime? CQStartDate = null,
    DateTime? CQEndDate = null,
    string? FGFormNo = null,
    string? QCNo = null,
    DateTime? IssuedDate = null,
    string? CONo = null,
    DateTime? CODate = null,
    DateTime? CODateExpected = null,
    string? InvoiceNoFactory = null,
    DateTime? InvoiceFactoryDate = null,
    string? InvoiceFactorySearch = null,
    string? InvoiceNoTransferred = null,
    DateTime? InvoiceTransferredDate = null,
    string? InvoiceTransferredSearch = null,
    string? InvoiceSpecName = null,
    bool? TypeCB = null,
    string? LoaiThung = null,
    string? CabinCertificateNo = null,
    DateTime? CabinCertificateDate = null,
    string? CabinCONo = null,
    string? CabinInvoiceNo = null,
    DateTime? CabinInvoiceDate = null,
    CarMortageStatus? MortageStatus = null,
    string? MortageBankCode = null,
    string? MortageBankName = null,
    DateTime? MortageStartDate = null,
    string? HandOverBankCode = null,
    string? HandOverBankName = null,
    string? BillNo = null,
    DateTime? MortageEndDate = null,
    string? GuaranteeNo = null,
    string? BankGuaranteeNo = null,
    DateTime? DateStart = null,
    DateTime? DateExpired = null,
    decimal? GuaranteeValue = null,
    string? DocumentsStatus = null,
    string? FlagDocReq = null,
    string? Remark = null,
    string? UpdatedBy = null
);

public record UpdateVinDocumentsDto(
    string? CQNo = null,
    DateTime? CQStartDate = null,
    DateTime? CQEndDate = null,
    string? FGFormNo = null,
    string? QCNo = null,
    DateTime? IssuedDate = null,
    string? CONo = null,
    DateTime? CODate = null,
    DateTime? CODateExpected = null,
    string DocumentsStatus = "1",
    string? Remark = null,
    string? UpdatedBy = null
);

public record UpdateVinCabinDto(
    string LoaiThung,
    string? CabinCertificateNo = null,
    DateTime? CabinCertificateDate = null,
    string? CabinCONo = null,
    string? CabinInvoiceNo = null,
    DateTime? CabinInvoiceDate = null,
    string? UpdatedBy = null
);

public record BatchUpdateCabinItemDto(
    string Vin,
    string LoaiThung,
    string? CabinCertificateNo = null,
    DateTime? CabinCertificateDate = null,
    string? CabinCONo = null,
    string? CabinInvoiceNo = null,
    DateTime? CabinInvoiceDate = null
);

public record UpdateVinHandoverBankDto(
    string BillNo,
    string HandOverBankCode,
    DateTime MortageEndDate,
    string? HandOverBankName = null,
    string? UpdatedBy = null
);

public record UpdateVinInvoiceFactoryDto(
    string InvoiceNoFactory,
    DateTime InvoiceFactoryDate,
    string? InvoiceFactorySearch = null,
    string? UpdatedBy = null
);

public record BatchUpdateInvFactoryItemDto(
    string Vin,
    string InvoiceNoFactory,
    DateTime InvoiceFactoryDate,
    string? InvoiceFactorySearch = null
);

public record UpdateVinInvoiceTransferredDto(
    string InvoiceNoTransferred,
    DateTime InvoiceTransferredDate,
    string? InvoiceTransferredSearch = null,
    string? UpdatedBy = null
);

public record BatchUpdateInvTransferredItemDto(
    string Vin,
    string InvoiceNoTransferred,
    DateTime InvoiceTransferredDate,
    string? InvoiceTransferredSearch = null
);

public record UpdateVinDateStartDto(
    DateTime DateStart,
    DateTime? DateExpired = null,
    string? GuaranteeNo = null,
    decimal? GuaranteeValueNew = null,
    bool? FlagDtlDiscount = null,
    bool? FlagGrtExt = null,
    string? UpdatedBy = null
);

public record BatchUpdateFlagDocReqDto(
    List<string> Vins,
    string FlagDocReq, // "0" hoặc "1"
    string? UpdatedBy = null
);

public record RedeemVinProfileDto(
    DateTime RedeemDate,
    string? Remark = null,
    string? UpdatedBy = null
);

public record SendMailVinDocDto(
    List<string> Vins,
    string? SentBy = null
);

public record CarVinProfileSearchFilterDto(
    string? Vin = null,
    string? CarId = null,
    string? DealerCode = null,
    string? ModelCode = null,
    string? StorageCode = null,
    CarMortageStatus? MortageStatus = null,
    string? MortageBankCode = null,
    string? HandOverBankCode = null,
    string? FlagDocReq = null,
    string? DocumentsStatus = null,
    string? FlagMortageEndDate = null, // "1" = đã có ngày bàn giao hồ sơ, "0" = chưa
    string? FlagInvoiceNoFactory = null, // "1" = đã có HĐ nhà máy, "0" = chưa
    string? DateStartStatus = null, // "1" = đã kích hoạt bảo hành, "0" = chưa
    bool? TypeCB = null,
    DateTime? CreatedDateFrom = null,
    DateTime? CreatedDateTo = null,
    int PageIndex = 0,
    int PageSize = 50
);

public record CarVinProfileStatsDto(
    int TotalProfiles,
    int TotalFullDocs,
    int TotalMortgaged,
    int TotalRedeemed,
    int TotalFree,
    int TotalMissingFactoryInvoice,
    int TotalDocReqAllowed,
    int TotalCommercialCabin,
    Dictionary<string, int> ByDealer,
    Dictionary<string, int> ByBank,
    Dictionary<string, int> ByModel
);

public interface ICarVinProfileService
{
    Task<object> SearchAsync(CarVinProfileSearchFilterDto filter);
    Task<CarVinProfile?> GetByVinAsync(string vin);
    Task<CarVinProfileStatsDto> GetStatsAsync(string? dealerCode = null);
    Task<CarVinProfile> CreateAsync(CreateCarVinProfileDto dto);
    Task<CarVinProfile?> UpdateAsync(string vin, UpdateCarVinProfileDto dto);
    Task<CarVinProfile?> UpdateDocumentsAsync(string vin, UpdateVinDocumentsDto dto);
    Task<CarVinProfile?> UpdateCabinAsync(string vin, UpdateVinCabinDto dto);
    Task<int> BatchUpdateCabinAsync(List<BatchUpdateCabinItemDto> items, string? updatedBy);
    Task<CarVinProfile?> UpdateHandoverBankAsync(string vin, UpdateVinHandoverBankDto dto);
    Task<CarVinProfile?> UpdateInvoiceFactoryAsync(string vin, UpdateVinInvoiceFactoryDto dto);
    Task<int> BatchUpdateInvoiceFactoryAsync(List<BatchUpdateInvFactoryItemDto> items, string? updatedBy);
    Task<CarVinProfile?> UpdateInvoiceTransferredAsync(string vin, UpdateVinInvoiceTransferredDto dto);
    Task<int> BatchUpdateInvoiceTransferredAsync(List<BatchUpdateInvTransferredItemDto> items, string? updatedBy);
    Task<CarVinProfile?> UpdateDateStartAsync(string vin, UpdateVinDateStartDto dto);
    Task<int> BatchUpdateFlagDocReqAsync(BatchUpdateFlagDocReqDto dto);
    Task<CarVinProfile?> RedeemAsync(string vin, RedeemVinProfileDto dto);
    Task<int> SendMailDocReminderAsync(SendMailVinDocDto dto);
    Task<bool> DeleteAsync(string vin);
}

public sealed class CarVinProfileService(AppDbContext db, ITenantContext tenant) : ICarVinProfileService
{
    public async Task<object> SearchAsync(CarVinProfileSearchFilterDto filter)
    {
        var query = db.CarVinProfiles.Where(x => x.OrgId == tenant.OrgId);

        if (!string.IsNullOrWhiteSpace(filter.Vin))
        {
            var v = filter.Vin.Trim().ToUpper();
            query = query.Where(x => x.Vin.Contains(v));
        }

        if (!string.IsNullOrWhiteSpace(filter.CarId))
        {
            var cid = filter.CarId.Trim().ToUpper();
            query = query.Where(x => x.CarId != null && x.CarId.Contains(cid));
        }

        if (!string.IsNullOrWhiteSpace(filter.DealerCode))
        {
            var dc = filter.DealerCode.Trim().ToUpper();
            query = query.Where(x => x.DealerCode.ToUpper() == dc);
        }

        if (!string.IsNullOrWhiteSpace(filter.ModelCode))
        {
            var mc = filter.ModelCode.Trim().ToUpper();
            query = query.Where(x => x.ModelCode.ToUpper() == mc);
        }

        if (!string.IsNullOrWhiteSpace(filter.StorageCode))
        {
            var sc = filter.StorageCode.Trim().ToUpper();
            query = query.Where(x => x.StorageCode != null && x.StorageCode.ToUpper() == sc);
        }

        if (filter.MortageStatus.HasValue)
        {
            query = query.Where(x => x.MortageStatus == filter.MortageStatus.Value);
        }

        if (!string.IsNullOrWhiteSpace(filter.MortageBankCode))
        {
            var bc = filter.MortageBankCode.Trim().ToUpper();
            query = query.Where(x => x.MortageBankCode != null && x.MortageBankCode.ToUpper() == bc);
        }

        if (!string.IsNullOrWhiteSpace(filter.HandOverBankCode))
        {
            var hbc = filter.HandOverBankCode.Trim().ToUpper();
            query = query.Where(x => x.HandOverBankCode != null && x.HandOverBankCode.ToUpper() == hbc);
        }

        if (!string.IsNullOrWhiteSpace(filter.FlagDocReq))
        {
            query = query.Where(x => x.FlagDocReq == filter.FlagDocReq.Trim());
        }

        if (!string.IsNullOrWhiteSpace(filter.DocumentsStatus))
        {
            query = query.Where(x => x.DocumentsStatus == filter.DocumentsStatus.Trim());
        }

        if (!string.IsNullOrWhiteSpace(filter.FlagMortageEndDate))
        {
            if (filter.FlagMortageEndDate.Trim() == "1")
                query = query.Where(x => x.MortageEndDate != null);
            else if (filter.FlagMortageEndDate.Trim() == "0")
                query = query.Where(x => x.MortageEndDate == null);
        }

        if (!string.IsNullOrWhiteSpace(filter.FlagInvoiceNoFactory))
        {
            if (filter.FlagInvoiceNoFactory.Trim() == "1")
                query = query.Where(x => !string.IsNullOrEmpty(x.InvoiceNoFactory));
            else if (filter.FlagInvoiceNoFactory.Trim() == "0")
                query = query.Where(x => string.IsNullOrEmpty(x.InvoiceNoFactory));
        }

        if (!string.IsNullOrWhiteSpace(filter.DateStartStatus))
        {
            if (filter.DateStartStatus.Trim() == "1")
                query = query.Where(x => x.DateStart != null);
            else if (filter.DateStartStatus.Trim() == "0")
                query = query.Where(x => x.DateStart == null);
        }

        if (filter.TypeCB.HasValue)
        {
            query = query.Where(x => x.TypeCB == filter.TypeCB.Value);
        }

        if (filter.CreatedDateFrom.HasValue)
        {
            query = query.Where(x => x.CreatedAt >= filter.CreatedDateFrom.Value);
        }

        if (filter.CreatedDateTo.HasValue)
        {
            query = query.Where(x => x.CreatedAt <= filter.CreatedDateTo.Value.AddDays(1).AddTicks(-1));
        }

        var total = await query.CountAsync();
        var pageIndex = filter.PageIndex < 0 ? 0 : filter.PageIndex;
        var pageSize = filter.PageSize <= 0 ? 50 : filter.PageSize;

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
            totalPages = (int)Math.Ceiling((double)total / pageSize),
            data = items
        };
    }

    public async Task<CarVinProfile?> GetByVinAsync(string vin)
    {
        if (string.IsNullOrWhiteSpace(vin)) return null;
        var v = vin.Trim().ToUpper();
        return await db.CarVinProfiles.FirstOrDefaultAsync(x => x.OrgId == tenant.OrgId && x.Vin == v);
    }

    public async Task<CarVinProfileStatsDto> GetStatsAsync(string? dealerCode = null)
    {
        var query = db.CarVinProfiles.Where(x => x.OrgId == tenant.OrgId);
        if (!string.IsNullOrWhiteSpace(dealerCode))
        {
            var dc = dealerCode.Trim().ToUpper();
            query = query.Where(x => x.DealerCode.ToUpper() == dc);
        }

        var list = await query.ToListAsync();

        var total = list.Count;
        var fullDocs = list.Count(x => x.DocumentsStatus == "1");
        var mortgaged = list.Count(x => x.MortageStatus == CarMortageStatus.Mortgaged);
        var redeemed = list.Count(x => x.MortageStatus == CarMortageStatus.Redeemed);
        var free = list.Count(x => x.MortageStatus == CarMortageStatus.Free);
        var missingInv = list.Count(x => string.IsNullOrWhiteSpace(x.InvoiceNoFactory));
        var docReqAllowed = list.Count(x => x.FlagDocReq == "1");
        var cabin = list.Count(x => x.TypeCB);

        var byDealer = list
            .GroupBy(x => string.IsNullOrWhiteSpace(x.DealerCode) ? "UNKNOWN" : x.DealerCode)
            .ToDictionary(g => g.Key, g => g.Count());

        var byBank = list
            .Where(x => !string.IsNullOrWhiteSpace(x.MortageBankCode) || !string.IsNullOrWhiteSpace(x.HandOverBankCode))
            .GroupBy(x => !string.IsNullOrWhiteSpace(x.HandOverBankCode) ? x.HandOverBankCode! : x.MortageBankCode!)
            .ToDictionary(g => g.Key, g => g.Count());

        var byModel = list
            .GroupBy(x => string.IsNullOrWhiteSpace(x.ModelCode) ? "UNKNOWN" : x.ModelCode)
            .ToDictionary(g => g.Key, g => g.Count());

        return new CarVinProfileStatsDto(
            total,
            fullDocs,
            mortgaged,
            redeemed,
            free,
            missingInv,
            docReqAllowed,
            cabin,
            byDealer,
            byBank,
            byModel
        );
    }

    public async Task<CarVinProfile> CreateAsync(CreateCarVinProfileDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Vin))
            throw new InvalidOperationException("Số khung xe (VIN) không được để trống.");

        var vin = dto.Vin.Trim().ToUpper();
        if (vin.Length != 17)
            throw new InvalidOperationException($"Số khung VIN '{vin}' phải có độ dài chuẩn 17 ký tự.");

        if (string.IsNullOrWhiteSpace(dto.DealerCode))
            throw new InvalidOperationException("Mã đại lý quản lý xe không được để trống.");

        if (string.IsNullOrWhiteSpace(dto.ModelCode))
            throw new InvalidOperationException("Mã model xe không được để trống.");

        var exists = await db.CarVinProfiles.AnyAsync(x => x.OrgId == tenant.OrgId && x.Vin == vin);
        if (exists)
            throw new InvalidOperationException($"Hồ sơ xe với số khung VIN '{vin}' đã tồn tại trong hệ thống.");

        var entity = new CarVinProfile
        {
            OrgId = tenant.OrgId,
            Vin = vin,
            CarId = dto.CarId?.Trim().ToUpper(),
            DealerCode = dto.DealerCode.Trim().ToUpper(),
            DealerName = dto.DealerName?.Trim(),
            ModelCode = dto.ModelCode.Trim().ToUpper(),
            ModelName = dto.ModelName?.Trim() ?? dto.ModelCode.Trim().ToUpper(),
            SpecCode = dto.SpecCode?.Trim().ToUpper(),
            SpecDescription = dto.SpecDescription?.Trim(),
            ColorCode = dto.ColorCode?.Trim().ToUpper(),
            ColorName = dto.ColorName?.Trim(),
            EngineNo = dto.EngineNo?.Trim().ToUpper(),
            StorageCode = dto.StorageCode?.Trim().ToUpper(),
            StorageName = dto.StorageName?.Trim(),
            CQNo = dto.CQNo?.Trim(),
            CQStartDate = dto.CQStartDate,
            CQEndDate = dto.CQEndDate,
            FGFormNo = dto.FGFormNo?.Trim(),
            QCNo = dto.QCNo?.Trim(),
            IssuedDate = dto.IssuedDate,
            CONo = dto.CONo?.Trim(),
            CODate = dto.CODate,
            CODateExpected = dto.CODateExpected,
            InvoiceNoFactory = dto.InvoiceNoFactory?.Trim(),
            InvoiceFactoryDate = dto.InvoiceFactoryDate,
            InvoiceFactorySearch = dto.InvoiceFactorySearch?.Trim(),
            InvoiceNoTransferred = dto.InvoiceNoTransferred?.Trim(),
            InvoiceTransferredDate = dto.InvoiceTransferredDate,
            InvoiceTransferredSearch = dto.InvoiceTransferredSearch?.Trim(),
            InvoiceSpecName = dto.InvoiceSpecName?.Trim(),
            TypeCB = dto.TypeCB || !string.IsNullOrWhiteSpace(dto.LoaiThung),
            LoaiThung = dto.LoaiThung?.Trim().ToUpper(),
            CabinCertificateNo = dto.CabinCertificateNo?.Trim(),
            CabinCertificateDate = dto.CabinCertificateDate,
            CabinCONo = dto.CabinCONo?.Trim(),
            CabinInvoiceNo = dto.CabinInvoiceNo?.Trim(),
            CabinInvoiceDate = dto.CabinInvoiceDate,
            MortageStatus = dto.MortageStatus,
            MortageBankCode = dto.MortageBankCode?.Trim().ToUpper(),
            MortageBankName = dto.MortageBankName?.Trim(),
            MortageStartDate = dto.MortageStartDate,
            HandOverBankCode = dto.HandOverBankCode?.Trim().ToUpper(),
            HandOverBankName = dto.HandOverBankName?.Trim(),
            BillNo = dto.BillNo?.Trim(),
            MortageEndDate = dto.MortageEndDate,
            GuaranteeNo = dto.GuaranteeNo?.Trim(),
            BankGuaranteeNo = dto.BankGuaranteeNo?.Trim(),
            DateStart = dto.DateStart,
            DateExpired = dto.DateExpired,
            GuaranteeValue = dto.GuaranteeValue,
            DocumentsStatus = dto.DocumentsStatus?.Trim() ?? "0",
            FlagDocReq = dto.FlagDocReq?.Trim() ?? "0",
            Remark = dto.Remark?.Trim(),
            CreatedBy = dto.CreatedBy?.Trim(),
            CreatedAt = DateTime.Now
        };

        db.CarVinProfiles.Add(entity);
        await db.SaveChangesAsync();
        return entity;
    }

    public async Task<CarVinProfile?> UpdateAsync(string vin, UpdateCarVinProfileDto dto)
    {
        var entity = await GetByVinAsync(vin);
        if (entity is null) return null;

        if (dto.CarId != null) entity.CarId = dto.CarId.Trim().ToUpper();
        if (dto.DealerCode != null) entity.DealerCode = dto.DealerCode.Trim().ToUpper();
        if (dto.DealerName != null) entity.DealerName = dto.DealerName.Trim();
        if (dto.ModelCode != null) entity.ModelCode = dto.ModelCode.Trim().ToUpper();
        if (dto.ModelName != null) entity.ModelName = dto.ModelName.Trim();
        if (dto.SpecCode != null) entity.SpecCode = dto.SpecCode.Trim().ToUpper();
        if (dto.SpecDescription != null) entity.SpecDescription = dto.SpecDescription.Trim();
        if (dto.ColorCode != null) entity.ColorCode = dto.ColorCode.Trim().ToUpper();
        if (dto.ColorName != null) entity.ColorName = dto.ColorName.Trim();
        if (dto.EngineNo != null) entity.EngineNo = dto.EngineNo.Trim().ToUpper();
        if (dto.StorageCode != null) entity.StorageCode = dto.StorageCode.Trim().ToUpper();
        if (dto.StorageName != null) entity.StorageName = dto.StorageName.Trim();

        if (dto.CQNo != null) entity.CQNo = dto.CQNo.Trim();
        if (dto.CQStartDate.HasValue) entity.CQStartDate = dto.CQStartDate.Value;
        if (dto.CQEndDate.HasValue) entity.CQEndDate = dto.CQEndDate.Value;
        if (dto.FGFormNo != null) entity.FGFormNo = dto.FGFormNo.Trim();
        if (dto.QCNo != null) entity.QCNo = dto.QCNo.Trim();
        if (dto.IssuedDate.HasValue) entity.IssuedDate = dto.IssuedDate.Value;

        if (dto.CONo != null) entity.CONo = dto.CONo.Trim();
        if (dto.CODate.HasValue) entity.CODate = dto.CODate.Value;
        if (dto.CODateExpected.HasValue) entity.CODateExpected = dto.CODateExpected.Value;

        if (dto.InvoiceNoFactory != null) entity.InvoiceNoFactory = dto.InvoiceNoFactory.Trim();
        if (dto.InvoiceFactoryDate.HasValue) entity.InvoiceFactoryDate = dto.InvoiceFactoryDate.Value;
        if (dto.InvoiceFactorySearch != null) entity.InvoiceFactorySearch = dto.InvoiceFactorySearch.Trim();

        if (dto.InvoiceNoTransferred != null) entity.InvoiceNoTransferred = dto.InvoiceNoTransferred.Trim();
        if (dto.InvoiceTransferredDate.HasValue) entity.InvoiceTransferredDate = dto.InvoiceTransferredDate.Value;
        if (dto.InvoiceTransferredSearch != null) entity.InvoiceTransferredSearch = dto.InvoiceTransferredSearch.Trim();
        if (dto.InvoiceSpecName != null) entity.InvoiceSpecName = dto.InvoiceSpecName.Trim();

        if (dto.TypeCB.HasValue) entity.TypeCB = dto.TypeCB.Value;
        if (dto.LoaiThung != null)
        {
            entity.LoaiThung = dto.LoaiThung.Trim().ToUpper();
            entity.TypeCB = true;
        }
        if (dto.CabinCertificateNo != null) entity.CabinCertificateNo = dto.CabinCertificateNo.Trim();
        if (dto.CabinCertificateDate.HasValue) entity.CabinCertificateDate = dto.CabinCertificateDate.Value;
        if (dto.CabinCONo != null) entity.CabinCONo = dto.CabinCONo.Trim();
        if (dto.CabinInvoiceNo != null) entity.CabinInvoiceNo = dto.CabinInvoiceNo.Trim();
        if (dto.CabinInvoiceDate.HasValue) entity.CabinInvoiceDate = dto.CabinInvoiceDate.Value;

        if (dto.MortageStatus.HasValue) entity.MortageStatus = dto.MortageStatus.Value;
        if (dto.MortageBankCode != null) entity.MortageBankCode = dto.MortageBankCode.Trim().ToUpper();
        if (dto.MortageBankName != null) entity.MortageBankName = dto.MortageBankName.Trim();
        if (dto.MortageStartDate.HasValue) entity.MortageStartDate = dto.MortageStartDate.Value;
        if (dto.HandOverBankCode != null) entity.HandOverBankCode = dto.HandOverBankCode.Trim().ToUpper();
        if (dto.HandOverBankName != null) entity.HandOverBankName = dto.HandOverBankName.Trim();
        if (dto.BillNo != null) entity.BillNo = dto.BillNo.Trim();
        if (dto.MortageEndDate.HasValue) entity.MortageEndDate = dto.MortageEndDate.Value;

        if (dto.GuaranteeNo != null) entity.GuaranteeNo = dto.GuaranteeNo.Trim();
        if (dto.BankGuaranteeNo != null) entity.BankGuaranteeNo = dto.BankGuaranteeNo.Trim();
        if (dto.DateStart.HasValue) entity.DateStart = dto.DateStart.Value;
        if (dto.DateExpired.HasValue) entity.DateExpired = dto.DateExpired.Value;
        if (dto.GuaranteeValue.HasValue) entity.GuaranteeValue = dto.GuaranteeValue.Value;

        if (dto.DocumentsStatus != null) entity.DocumentsStatus = dto.DocumentsStatus.Trim();
        if (dto.FlagDocReq != null) entity.FlagDocReq = dto.FlagDocReq.Trim();
        if (dto.Remark != null) entity.Remark = dto.Remark.Trim();

        entity.UpdatedAt = DateTime.Now;
        entity.UpdatedBy = dto.UpdatedBy?.Trim();

        await db.SaveChangesAsync();
        return entity;
    }

    public async Task<CarVinProfile?> UpdateDocumentsAsync(string vin, UpdateVinDocumentsDto dto)
    {
        var entity = await GetByVinAsync(vin);
        if (entity is null) return null;

        if (dto.CQNo != null) entity.CQNo = dto.CQNo.Trim();
        if (dto.CQStartDate.HasValue) entity.CQStartDate = dto.CQStartDate.Value;
        if (dto.CQEndDate.HasValue) entity.CQEndDate = dto.CQEndDate.Value;
        if (dto.FGFormNo != null) entity.FGFormNo = dto.FGFormNo.Trim();
        if (dto.QCNo != null) entity.QCNo = dto.QCNo.Trim();
        if (dto.IssuedDate.HasValue) entity.IssuedDate = dto.IssuedDate.Value;

        if (dto.CONo != null) entity.CONo = dto.CONo.Trim();
        if (dto.CODate.HasValue) entity.CODate = dto.CODate.Value;
        if (dto.CODateExpected.HasValue) entity.CODateExpected = dto.CODateExpected.Value;

        entity.DocumentsStatus = dto.DocumentsStatus.Trim();
        if (dto.Remark != null) entity.Remark = dto.Remark.Trim();

        entity.UpdatedAt = DateTime.Now;
        entity.UpdatedBy = dto.UpdatedBy?.Trim();

        await db.SaveChangesAsync();
        return entity;
    }

    public async Task<CarVinProfile?> UpdateCabinAsync(string vin, UpdateVinCabinDto dto)
    {
        var entity = await GetByVinAsync(vin);
        if (entity is null) return null;

        if (string.IsNullOrWhiteSpace(dto.LoaiThung))
            throw new InvalidOperationException("Loại thùng xe không được để trống.");

        entity.TypeCB = true;
        entity.LoaiThung = dto.LoaiThung.Trim().ToUpper();
        if (dto.CabinCertificateNo != null) entity.CabinCertificateNo = dto.CabinCertificateNo.Trim();
        if (dto.CabinCertificateDate.HasValue) entity.CabinCertificateDate = dto.CabinCertificateDate.Value;
        if (dto.CabinCONo != null) entity.CabinCONo = dto.CabinCONo.Trim();
        if (dto.CabinInvoiceNo != null) entity.CabinInvoiceNo = dto.CabinInvoiceNo.Trim();
        if (dto.CabinInvoiceDate.HasValue) entity.CabinInvoiceDate = dto.CabinInvoiceDate.Value;

        entity.UpdatedAt = DateTime.Now;
        entity.UpdatedBy = dto.UpdatedBy?.Trim();

        await db.SaveChangesAsync();
        return entity;
    }

    public async Task<int> BatchUpdateCabinAsync(List<BatchUpdateCabinItemDto> items, string? updatedBy)
    {
        if (items == null || items.Count == 0) return 0;
        int count = 0;
        foreach (var it in items)
        {
            if (string.IsNullOrWhiteSpace(it.Vin)) continue;
            var vin = it.Vin.Trim().ToUpper();
            var profile = await db.CarVinProfiles.FirstOrDefaultAsync(x => x.OrgId == tenant.OrgId && x.Vin == vin);
            if (profile != null)
            {
                profile.TypeCB = true;
                if (!string.IsNullOrWhiteSpace(it.LoaiThung)) profile.LoaiThung = it.LoaiThung.Trim().ToUpper();
                if (it.CabinCertificateNo != null) profile.CabinCertificateNo = it.CabinCertificateNo.Trim();
                if (it.CabinCertificateDate.HasValue) profile.CabinCertificateDate = it.CabinCertificateDate.Value;
                if (it.CabinCONo != null) profile.CabinCONo = it.CabinCONo.Trim();
                if (it.CabinInvoiceNo != null) profile.CabinInvoiceNo = it.CabinInvoiceNo.Trim();
                if (it.CabinInvoiceDate.HasValue) profile.CabinInvoiceDate = it.CabinInvoiceDate.Value;
                profile.UpdatedAt = DateTime.Now;
                profile.UpdatedBy = updatedBy?.Trim();
                count++;
            }
        }
        await db.SaveChangesAsync();
        return count;
    }

    public async Task<CarVinProfile?> UpdateHandoverBankAsync(string vin, UpdateVinHandoverBankDto dto)
    {
        var entity = await GetByVinAsync(vin);
        if (entity is null) return null;

        if (string.IsNullOrWhiteSpace(dto.BillNo))
            throw new InvalidOperationException("Số vận đơn / biên nhận bàn giao hồ sơ (BillNo) không được để trống.");

        if (string.IsNullOrWhiteSpace(dto.HandOverBankCode))
            throw new InvalidOperationException("Mã ngân hàng bàn giao hồ sơ không được để trống.");

        entity.BillNo = dto.BillNo.Trim();
        entity.HandOverBankCode = dto.HandOverBankCode.Trim().ToUpper();
        entity.HandOverBankName = dto.HandOverBankName?.Trim() ?? entity.HandOverBankCode;
        entity.MortageEndDate = dto.MortageEndDate;

        // Khi đã bàn giao hồ sơ sang ngân hàng, cập nhật trạng thái thế chấp
        if (entity.MortageStatus == CarMortageStatus.Free)
        {
            entity.MortageStatus = CarMortageStatus.Mortgaged;
            entity.MortageBankCode ??= entity.HandOverBankCode;
            entity.MortageBankName ??= entity.HandOverBankName;
            entity.MortageStartDate ??= dto.MortageEndDate;
        }

        entity.UpdatedAt = DateTime.Now;
        entity.UpdatedBy = dto.UpdatedBy?.Trim();

        await db.SaveChangesAsync();
        return entity;
    }

    public async Task<CarVinProfile?> UpdateInvoiceFactoryAsync(string vin, UpdateVinInvoiceFactoryDto dto)
    {
        var entity = await GetByVinAsync(vin);
        if (entity is null) return null;

        if (string.IsNullOrWhiteSpace(dto.InvoiceNoFactory))
            throw new InvalidOperationException("Số hóa đơn nhà máy không được để trống.");

        entity.InvoiceNoFactory = dto.InvoiceNoFactory.Trim();
        entity.InvoiceFactoryDate = dto.InvoiceFactoryDate;
        if (dto.InvoiceFactorySearch != null) entity.InvoiceFactorySearch = dto.InvoiceFactorySearch.Trim();

        entity.UpdatedAt = DateTime.Now;
        entity.UpdatedBy = dto.UpdatedBy?.Trim();

        await db.SaveChangesAsync();
        return entity;
    }

    public async Task<int> BatchUpdateInvoiceFactoryAsync(List<BatchUpdateInvFactoryItemDto> items, string? updatedBy)
    {
        if (items == null || items.Count == 0) return 0;
        int count = 0;
        foreach (var it in items)
        {
            if (string.IsNullOrWhiteSpace(it.Vin) || string.IsNullOrWhiteSpace(it.InvoiceNoFactory)) continue;
            var vin = it.Vin.Trim().ToUpper();
            var profile = await db.CarVinProfiles.FirstOrDefaultAsync(x => x.OrgId == tenant.OrgId && x.Vin == vin);
            if (profile != null)
            {
                profile.InvoiceNoFactory = it.InvoiceNoFactory.Trim();
                profile.InvoiceFactoryDate = it.InvoiceFactoryDate;
                if (it.InvoiceFactorySearch != null) profile.InvoiceFactorySearch = it.InvoiceFactorySearch.Trim();
                profile.UpdatedAt = DateTime.Now;
                profile.UpdatedBy = updatedBy?.Trim();
                count++;
            }
        }
        await db.SaveChangesAsync();
        return count;
    }

    public async Task<CarVinProfile?> UpdateInvoiceTransferredAsync(string vin, UpdateVinInvoiceTransferredDto dto)
    {
        var entity = await GetByVinAsync(vin);
        if (entity is null) return null;

        if (string.IsNullOrWhiteSpace(dto.InvoiceNoTransferred))
            throw new InvalidOperationException("Số hóa đơn chuyển giao không được để trống.");

        entity.InvoiceNoTransferred = dto.InvoiceNoTransferred.Trim();
        entity.InvoiceTransferredDate = dto.InvoiceTransferredDate;
        if (dto.InvoiceTransferredSearch != null) entity.InvoiceTransferredSearch = dto.InvoiceTransferredSearch.Trim();

        entity.UpdatedAt = DateTime.Now;
        entity.UpdatedBy = dto.UpdatedBy?.Trim();

        await db.SaveChangesAsync();
        return entity;
    }

    public async Task<int> BatchUpdateInvoiceTransferredAsync(List<BatchUpdateInvTransferredItemDto> items, string? updatedBy)
    {
        if (items == null || items.Count == 0) return 0;
        int count = 0;
        foreach (var it in items)
        {
            if (string.IsNullOrWhiteSpace(it.Vin) || string.IsNullOrWhiteSpace(it.InvoiceNoTransferred)) continue;
            var vin = it.Vin.Trim().ToUpper();
            var profile = await db.CarVinProfiles.FirstOrDefaultAsync(x => x.OrgId == tenant.OrgId && x.Vin == vin);
            if (profile != null)
            {
                profile.InvoiceNoTransferred = it.InvoiceNoTransferred.Trim();
                profile.InvoiceTransferredDate = it.InvoiceTransferredDate;
                if (it.InvoiceTransferredSearch != null) profile.InvoiceTransferredSearch = it.InvoiceTransferredSearch.Trim();
                profile.UpdatedAt = DateTime.Now;
                profile.UpdatedBy = updatedBy?.Trim();
                count++;
            }
        }
        await db.SaveChangesAsync();
        return count;
    }

    public async Task<CarVinProfile?> UpdateDateStartAsync(string vin, UpdateVinDateStartDto dto)
    {
        var entity = await GetByVinAsync(vin);
        if (entity is null) return null;

        entity.DateStart = dto.DateStart;
        if (dto.DateExpired.HasValue) entity.DateExpired = dto.DateExpired.Value;
        if (dto.GuaranteeNo != null) entity.GuaranteeNo = dto.GuaranteeNo.Trim();
        if (dto.GuaranteeValueNew.HasValue && dto.GuaranteeValueNew.Value >= 0) entity.GuaranteeValue = dto.GuaranteeValueNew.Value;
        if (dto.FlagDtlDiscount.HasValue) entity.FlagDtlDiscount = dto.FlagDtlDiscount.Value;
        if (dto.FlagGrtExt.HasValue) entity.FlagGrtExt = dto.FlagGrtExt.Value;

        entity.UpdatedAt = DateTime.Now;
        entity.UpdatedBy = dto.UpdatedBy?.Trim();

        await db.SaveChangesAsync();
        return entity;
    }

    public async Task<int> BatchUpdateFlagDocReqAsync(BatchUpdateFlagDocReqDto dto)
    {
        if (dto.Vins == null || dto.Vins.Count == 0) return 0;
        var flag = dto.FlagDocReq?.Trim() == "1" ? "1" : "0";

        int count = 0;
        foreach (var v in dto.Vins)
        {
            if (string.IsNullOrWhiteSpace(v)) continue;
            var vin = v.Trim().ToUpper();
            var profile = await db.CarVinProfiles.FirstOrDefaultAsync(x => x.OrgId == tenant.OrgId && x.Vin == vin);
            if (profile != null)
            {
                profile.FlagDocReq = flag;
                if (flag == "1" && profile.DocDeliveryReqDate == null)
                {
                    profile.DocDeliveryReqDate = DateTime.Now;
                }
                profile.UpdatedAt = DateTime.Now;
                profile.UpdatedBy = dto.UpdatedBy?.Trim();
                count++;
            }
        }
        await db.SaveChangesAsync();
        return count;
    }

    public async Task<CarVinProfile?> RedeemAsync(string vin, RedeemVinProfileDto dto)
    {
        var entity = await GetByVinAsync(vin);
        if (entity is null) return null;

        entity.MortageStatus = CarMortageStatus.Redeemed;
        entity.RedeemDate = dto.RedeemDate;
        entity.LogDateTimeStatusMortageEnd = DateTime.Now;
        if (dto.Remark != null) entity.Remark = dto.Remark.Trim();

        entity.UpdatedAt = DateTime.Now;
        entity.UpdatedBy = dto.UpdatedBy?.Trim();

        await db.SaveChangesAsync();
        return entity;
    }

    public async Task<int> SendMailDocReminderAsync(SendMailVinDocDto dto)
    {
        if (dto.Vins == null || dto.Vins.Count == 0) return 0;

        int count = 0;
        var now = DateTime.Now;
        foreach (var v in dto.Vins)
        {
            if (string.IsNullOrWhiteSpace(v)) continue;
            var vin = v.Trim().ToUpper();
            var profile = await db.CarVinProfiles.FirstOrDefaultAsync(x => x.OrgId == tenant.OrgId && x.Vin == vin);
            if (profile != null)
            {
                profile.SendMailDocDate = now;
                profile.UpdatedAt = now;
                profile.UpdatedBy = dto.SentBy?.Trim();
                count++;
            }
        }
        await db.SaveChangesAsync();
        return count;
    }

    public async Task<bool> DeleteAsync(string vin)
    {
        var entity = await GetByVinAsync(vin);
        if (entity is null) return false;

        if (entity.MortageStatus == CarMortageStatus.Mortgaged)
            throw new InvalidOperationException($"Không thể xóa hồ sơ xe {vin} khi đang thế chấp ngân hàng ({entity.MortageBankCode}).");

        db.CarVinProfiles.Remove(entity);
        await db.SaveChangesAsync();
        return true;
    }
}
