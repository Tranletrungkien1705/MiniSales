using Microsoft.EntityFrameworkCore;
using MiniSales.Data;
using MiniSales.Models;

namespace MiniSales.Services;

public record CustomsDeclarationCarItemDto(
    string Vin,
    string? EngineNo = null,
    string ModelCode = "",
    string? ModelName = null,
    string? SpecCode = null,
    string? SpecDescription = null,
    string? ColorCode = null,
    string? ColorName = null,
    string? PackingListNo = null,
    string? LCNo = null,
    string? ContractNo = null,
    decimal? DutiableValue = null,
    string? Remark = null
);

public record CreateCustomsDeclarationDto(
    string PortCode,
    string? DeclarationNo = null,
    string? PortName = null,
    string? ContractNo = null,
    DateTime? OpenDate = null,
    CustomsChannel Channel = CustomsChannel.Yellow,
    string? CustomsOffice = null,
    string? TaxPayerBank = null,
    string? Remark = null,
    string? CreatedBy = null,
    List<CustomsDeclarationCarItemDto>? Cars = null
);

public record UpdateCustomsDeclarationDto(
    string? PortCode = null,
    string? PortName = null,
    string? ContractNo = null,
    DateTime? OpenDate = null,
    CustomsChannel? Channel = null,
    string? CustomsOffice = null,
    string? TaxPayerBank = null,
    string? Remark = null,
    string? UpdatedBy = null
);

public record SubmitCustomsDeclarationDto(
    CustomsChannel Channel = CustomsChannel.Yellow,
    string? SubmittedBy = null,
    string? Remark = null
);

public record UpdateTaxPaymentDateDto(
    DateTime TaxPaymentDate,
    string? TaxReceiptNo = null,
    string? TaxPayerBank = null,
    string? PaidBy = null,
    string? Remark = null
);

public record ClearCustomsDto(
    DateTime? CustomsClearanceDate = null,
    string? CustomsOfficer = null,
    string? ClearedBy = null,
    string? Remark = null
);

public record AddDeclarationCarsDto(
    List<CustomsDeclarationCarItemDto> Cars,
    string? UpdatedBy = null
);

public record CancelCustomsDeclarationDto(
    string Reason,
    string? CancelledBy = null
);

public record PortItemDto(
    string PortCode,
    string PortName,
    string Region,
    string CustomsOfficeDefault
);

public record CustomsEligibleVinDto(
    string Vin,
    string ModelCode,
    string ModelName,
    string? SpecCode,
    string? SpecDescription,
    string? ColorCode,
    string? ColorName,
    string? EngineNo,
    string StorageCode,
    string? StorageName,
    string AssemblyStatus,
    string? PackingListNo,
    string? LCNo,
    string? ContractNo,
    decimal EstimatedDutiableValue
);

public record CustomsTaxBreakdownDto(
    decimal DutiableValue,
    decimal ImportTaxRate,
    decimal ImportTax,
    decimal SctRate,
    decimal SpecialConsumptionTax,
    decimal VatRate,
    decimal Vat,
    decimal TotalTax
);

public record CustomsDeclarationDetailDto(
    long Id,
    long DeclarationId,
    string DeclarationNo,
    string Vin,
    string? EngineNo,
    string ModelCode,
    string ModelName,
    string? SpecCode,
    string? SpecDescription,
    string? ColorCode,
    string? ColorName,
    string? PackingListNo,
    string? LCNo,
    string? ContractNo,
    decimal DutiableValue,
    decimal ImportTax,
    decimal SpecialConsumptionTax,
    decimal Vat,
    decimal TotalTax,
    CustomsDeclarationDetailStatus Status,
    DateTime? TaxPaymentDate,
    DateTime? ClearedAt,
    string? Remark
);

public record CustomsDeclarationDto(
    long Id,
    string DeclarationNo,
    string PortCode,
    string? PortName,
    string? ContractNo,
    DateTime OpenDate,
    DateTime? TaxPaymentDate,
    string? TaxReceiptNo,
    string? TaxPayerBank,
    string? CustomsOffice,
    CustomsChannel Channel,
    CustomsDeclarationStatus Status,
    int TotalCars,
    decimal DutiableAmount,
    decimal ImportTaxAmount,
    decimal SpecialConsumptionTaxAmount,
    decimal VatAmount,
    decimal TotalTaxAmount,
    DateTime? CustomsClearanceDate,
    string? CustomsOfficer,
    string? Remark,
    string? CreatedBy,
    DateTime CreatedAt,
    string? SubmittedBy,
    DateTime? SubmittedAt,
    string? TaxPaidBy,
    string? ClearedBy,
    string? CancelledBy,
    DateTime? CancelledAt,
    string? CancelReason,
    List<CustomsDeclarationDetailDto> Details
);

public record CustomsStatsDto(
    int TotalDeclarations,
    int TotalCars,
    int DraftCount,
    int SubmittedCount,
    int TaxPaidCount,
    int ClearedCount,
    int CancelledCount,
    decimal TotalDutiableAmount,
    decimal TotalImportTaxAmount,
    decimal TotalSctAmount,
    decimal TotalVatAmount,
    decimal TotalTaxPaidAmount,
    Dictionary<string, int> ByPort,
    Dictionary<string, int> ByChannel,
    Dictionary<string, int> ByStatus
);

public interface ICustomsDeclarationService
{
    Task<string> GetNextSeqAsync();
    IReadOnlyList<PortItemDto> GetPorts();
    Task<IReadOnlyList<CustomsEligibleVinDto>> GetEligibleVinsAsync();
    Task<IReadOnlyList<CustomsDeclarationDto>> ListAsync(
        string? status = null,
        string? portCode = null,
        string? contractNo = null,
        string? declarationNo = null,
        DateTime? openDateFrom = null,
        DateTime? openDateTo = null,
        CustomsChannel? channel = null);
    Task<CustomsDeclarationDto?> DetailAsync(string declarationNo);
    Task<CustomsDeclarationDto> CreateAsync(CreateCustomsDeclarationDto dto);
    Task<CustomsDeclarationDto?> UpdateAsync(string declarationNo, UpdateCustomsDeclarationDto dto);
    Task<CustomsDeclarationDto?> SubmitAsync(string declarationNo, SubmitCustomsDeclarationDto? dto = null);
    Task<CustomsDeclarationDto?> UpdateTaxPaymentDateAsync(string declarationNo, UpdateTaxPaymentDateDto dto);
    Task<CustomsDeclarationDto?> ClearCustomsAsync(string declarationNo, ClearCustomsDto? dto = null);
    Task<CustomsDeclarationDto?> AddCarsAsync(string declarationNo, AddDeclarationCarsDto dto);
    Task<CustomsDeclarationDto?> RemoveCarAsync(string declarationNo, string vin);
    Task<CustomsDeclarationDto?> CancelAsync(string declarationNo, CancelCustomsDeclarationDto dto);
    Task<bool> DeleteDraftAsync(string declarationNo);
    Task<CustomsStatsDto> StatsAsync();
}

public sealed class CustomsDeclarationService(AppDbContext db, ITenantContext tenant) : ICustomsDeclarationService
{
    private static readonly List<PortItemDto> AvailablePorts = new()
    {
        new("HPH", "Cảng Hải Phòng (Đình Vũ / Lạch Huyện)", "Miền Bắc", "Chi cục Hải quan Cửa khẩu Cảng Hải Phòng KV3"),
        new("SGN", "Cảng Cát Lái - TP. Hồ Chí Minh", "Miền Nam", "Chi cục Hải quan Cửa khẩu Cảng Sài Gòn KV1"),
        new("CM", "Cảng Quốc tế Cái Mép - Bà Rịa Vũng Tàu", "Miền Nam", "Chi cục Hải quan Cửa khẩu Cảng Cái Mép"),
        new("DAN", "Cảng Tiên Sa - Đà Nẵng", "Miền Trung", "Chi cục Hải quan Cửa khẩu Cảng Đà Nẵng"),
        new("NGS", "Cảng Quốc tế Nghi Sơn - Thanh Hóa", "Miền Trung", "Chi cục Hải quan Cửa khẩu Cảng Nghi Sơn")
    };

    public IReadOnlyList<PortItemDto> GetPorts() => AvailablePorts;

    public async Task<string> GetNextSeqAsync()
    {
        var ym = DateTime.Now.ToString("yyMM");
        var prefix = $"{ym}TK";
        var count = await db.CustomsDeclarations
            .CountAsync(x => x.OrgId == tenant.OrgId && x.DeclarationNo.StartsWith(prefix));
        return $"{prefix}{(count + 1):D5}";
    }

    public async Task<IReadOnlyList<CustomsEligibleVinDto>> GetEligibleVinsAsync()
    {
        // Lấy danh sách VIN xe nhập khẩu CBU có trong hệ thống kho hoặc Packing List, chưa có tờ khai đang hoạt động
        var activeDeclaredVins = await db.CustomsDeclarationDetails
            .Where(d => d.Status != CustomsDeclarationDetailStatus.Cancelled)
            .Join(db.CustomsDeclarations.Where(c => c.OrgId == tenant.OrgId && c.Status != CustomsDeclarationStatus.Cancelled),
                d => d.DeclarationId, c => c.Id, (d, c) => d.Vin)
            .Distinct()
            .ToListAsync();

        var vinHashSet = new HashSet<string>(activeDeclaredVins, StringComparer.OrdinalIgnoreCase);

        // 1. Tìm từ CarVinInventory (loại xe CBU)
        var cbuInventories = await db.CarVinInventories
            .Where(x => x.OrgId == tenant.OrgId && (x.AssemblyStatus == "CBU" || x.AssemblyStatus == "Import"))
            .ToListAsync();

        var result = new List<CustomsEligibleVinDto>();

        foreach (var inv in cbuInventories)
        {
            if (vinHashSet.Contains(inv.Vin)) continue;

            // Tìm thông tin Packing List / Contract / LC nếu có
            var plDetail = await db.PackingListDetails
                .FirstOrDefaultAsync(p => p.Vin == inv.Vin);

            string? plNo = plDetail?.PackingListNo;
            string? lcNo = null;
            string? contractNo = null;

            if (plDetail != null)
            {
                var plMaster = await db.PackingLists.FirstOrDefaultAsync(p => p.Id == plDetail.PackingListId);
                lcNo = plMaster?.LCNo;
                contractNo = plMaster?.ContractNo;
            }

            var tax = CalculateTax(inv.ModelCode, null);

            result.Add(new CustomsEligibleVinDto(
                Vin: inv.Vin,
                ModelCode: inv.ModelCode,
                ModelName: inv.ModelName,
                SpecCode: inv.SpecCode,
                SpecDescription: inv.SpecDescription,
                ColorCode: inv.ColorCode,
                ColorName: inv.ColorName,
                EngineNo: inv.EngineNo,
                StorageCode: inv.StorageCode,
                StorageName: inv.StorageName,
                AssemblyStatus: inv.AssemblyStatus,
                PackingListNo: plNo,
                LCNo: lcNo,
                ContractNo: contractNo,
                EstimatedDutiableValue: tax.DutiableValue
            ));
        }

        // 2. Nếu trong CarVinInventory chưa có nhiều xe CBU, bổ sung từ PackingListDetails (loại CBU)
        var plCbuDetails = await db.PackingListDetails
            .Where(d => d.Status != PackingListDetailStatus.Cancelled)
            .ToListAsync();

        foreach (var pl in plCbuDetails)
        {
            if (result.Any(r => r.Vin.Equals(pl.Vin, StringComparison.OrdinalIgnoreCase))) continue;
            if (vinHashSet.Contains(pl.Vin)) continue;

            var plMaster = await db.PackingLists.FirstOrDefaultAsync(p => p.Id == pl.PackingListId);
            if (plMaster != null && plMaster.PLType == PackingListType.CKD) continue; // Chỉ nhận CBU

            var tax = CalculateTax(pl.ModelCode, null);

            result.Add(new CustomsEligibleVinDto(
                Vin: pl.Vin,
                ModelCode: pl.ModelCode,
                ModelName: pl.ModelName ?? pl.ModelCode,
                SpecCode: pl.SpecCode,
                SpecDescription: pl.SpecDescription,
                ColorCode: pl.ColorCode,
                ColorName: pl.ColorName,
                EngineNo: pl.EngineNo,
                StorageCode: pl.StorageCodeCurrent ?? "KHO-CANG",
                StorageName: pl.StorageCodeCurrent ?? "Kho Cảng Biển Hải Quan",
                AssemblyStatus: "CBU",
                PackingListNo: pl.PackingListNo,
                LCNo: plMaster?.LCNo,
                ContractNo: plMaster?.ContractNo,
                EstimatedDutiableValue: tax.DutiableValue
            ));
        }

        return result;
    }

    public async Task<IReadOnlyList<CustomsDeclarationDto>> ListAsync(
        string? status = null,
        string? portCode = null,
        string? contractNo = null,
        string? declarationNo = null,
        DateTime? openDateFrom = null,
        DateTime? openDateTo = null,
        CustomsChannel? channel = null)
    {
        var q = db.CustomsDeclarations
            .Include(x => x.Details)
            .Where(x => x.OrgId == tenant.OrgId);

        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<CustomsDeclarationStatus>(status, true, out var st))
            q = q.Where(x => x.Status == st);

        if (!string.IsNullOrWhiteSpace(portCode))
        {
            var p = portCode.Trim().ToUpperInvariant();
            q = q.Where(x => x.PortCode.ToUpper() == p);
        }

        if (!string.IsNullOrWhiteSpace(contractNo))
        {
            var c = contractNo.Trim().ToLowerInvariant();
            q = q.Where(x => x.ContractNo != null && x.ContractNo.ToLower().Contains(c));
        }

        if (!string.IsNullOrWhiteSpace(declarationNo))
        {
            var d = declarationNo.Trim().ToLowerInvariant();
            q = q.Where(x => x.DeclarationNo.ToLower().Contains(d));
        }

        if (openDateFrom.HasValue)
            q = q.Where(x => x.OpenDate >= openDateFrom.Value);

        if (openDateTo.HasValue)
            q = q.Where(x => x.OpenDate <= openDateTo.Value);

        if (channel.HasValue)
            q = q.Where(x => x.Channel == channel.Value);

        var list = await q.OrderByDescending(x => x.OpenDate).ThenByDescending(x => x.Id).ToListAsync();
        return list.Select(MapToDto).ToList();
    }

    public async Task<CustomsDeclarationDto?> DetailAsync(string declarationNo)
    {
        var dec = await db.CustomsDeclarations
            .Include(x => x.Details)
            .FirstOrDefaultAsync(x => x.OrgId == tenant.OrgId && x.DeclarationNo == declarationNo);

        return dec == null ? null : MapToDto(dec);
    }

    public async Task<CustomsDeclarationDto> CreateAsync(CreateCustomsDeclarationDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.PortCode))
            throw new InvalidOperationException("Mã cảng biển làm thủ tục hải quan (PortCode) là bắt buộc.");

        var portInfo = AvailablePorts.FirstOrDefault(p => p.PortCode.Equals(dto.PortCode.Trim(), StringComparison.OrdinalIgnoreCase));
        var portName = dto.PortName ?? portInfo?.PortName ?? dto.PortCode.Trim().ToUpperInvariant();
        var customsOffice = dto.CustomsOffice ?? portInfo?.CustomsOfficeDefault ?? "Chi cục Hải quan Cửa khẩu Cảng biển";

        string declNo = string.IsNullOrWhiteSpace(dto.DeclarationNo)
            ? await GetNextSeqAsync()
            : dto.DeclarationNo.Trim().ToUpperInvariant();

        if (await db.CustomsDeclarations.AnyAsync(x => x.OrgId == tenant.OrgId && x.DeclarationNo == declNo))
            throw new InvalidOperationException($"Số tờ khai hải quan '{declNo}' đã tồn tại trong hệ thống.");

        if (dto.Cars == null || dto.Cars.Count == 0)
            throw new InvalidOperationException("Tờ khai hải quan phải có ít nhất 01 xe ô tô nguyên chiếc (CBU).");

        // Kiểm tra trùng VIN trong input
        var dupVins = dto.Cars
            .GroupBy(x => x.Vin.Trim().ToUpperInvariant())
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();

        if (dupVins.Count > 0)
            throw new InvalidOperationException($"Danh sách xe có số VIN trùng lặp trong tờ khai: {string.Join(", ", dupVins)}");

        // Kiểm tra VIN đã thuộc tờ khai khác đang hoạt động chưa
        var inputVinList = dto.Cars.Select(c => c.Vin.Trim().ToUpperInvariant()).ToList();
        var existingVins = await db.CustomsDeclarationDetails
            .Where(d => d.Status != CustomsDeclarationDetailStatus.Cancelled && inputVinList.Contains(d.Vin.ToUpper()))
            .Join(db.CustomsDeclarations.Where(c => c.OrgId == tenant.OrgId && c.Status != CustomsDeclarationStatus.Cancelled),
                d => d.DeclarationId, c => c.Id, (d, c) => new { d.Vin, c.DeclarationNo })
            .ToListAsync();

        if (existingVins.Count > 0)
        {
            var firstConflict = existingVins.First();
            throw new InvalidOperationException($"Số khung VIN '{firstConflict.Vin}' đã được gắn vào tờ khai hải quan '{firstConflict.DeclarationNo}' đang hoạt động.");
        }

        var dec = new CustomsDeclaration
        {
            OrgId = tenant.OrgId,
            DeclarationNo = declNo,
            PortCode = dto.PortCode.Trim().ToUpperInvariant(),
            PortName = portName,
            ContractNo = dto.ContractNo?.Trim(),
            OpenDate = dto.OpenDate ?? DateTime.Today,
            Channel = dto.Channel,
            CustomsOffice = customsOffice,
            TaxPayerBank = dto.TaxPayerBank?.Trim() ?? "Vietcombank",
            Status = CustomsDeclarationStatus.Draft,
            Remark = dto.Remark?.Trim(),
            CreatedBy = dto.CreatedBy ?? "HQ",
            CreatedAt = DateTime.Now
        };

        decimal totalDutiable = 0m;
        decimal totalImportTax = 0m;
        decimal totalSct = 0m;
        decimal totalVat = 0m;

        foreach (var c in dto.Cars)
        {
            var cleanVin = c.Vin.Trim().ToUpperInvariant();
            var tax = CalculateTax(c.ModelCode, c.DutiableValue);

            var detail = new CustomsDeclarationDetail
            {
                DeclarationNo = declNo,
                Vin = cleanVin,
                EngineNo = c.EngineNo?.Trim(),
                ModelCode = c.ModelCode.Trim().ToUpperInvariant(),
                ModelName = !string.IsNullOrWhiteSpace(c.ModelName) ? c.ModelName.Trim() : c.ModelCode.Trim().ToUpperInvariant(),
                SpecCode = c.SpecCode?.Trim(),
                SpecDescription = c.SpecDescription?.Trim(),
                ColorCode = c.ColorCode?.Trim(),
                ColorName = c.ColorName?.Trim(),
                PackingListNo = c.PackingListNo?.Trim(),
                LCNo = c.LCNo?.Trim(),
                ContractNo = c.ContractNo?.Trim() ?? dto.ContractNo?.Trim(),
                DutiableValue = tax.DutiableValue,
                ImportTax = tax.ImportTax,
                SpecialConsumptionTax = tax.SpecialConsumptionTax,
                Vat = tax.Vat,
                TotalTax = tax.TotalTax,
                Status = CustomsDeclarationDetailStatus.Pending,
                Remark = c.Remark?.Trim()
            };

            totalDutiable += tax.DutiableValue;
            totalImportTax += tax.ImportTax;
            totalSct += tax.SpecialConsumptionTax;
            totalVat += tax.Vat;

            dec.Details.Add(detail);

            // Cập nhật DeclarationNo vào CarVinInventory nếu tồn tại
            var inv = await db.CarVinInventories.FirstOrDefaultAsync(x => x.OrgId == tenant.OrgId && x.Vin == cleanVin);
            if (inv != null)
            {
                inv.DeclarationNo = declNo;
            }
        }

        dec.TotalCars = dec.Details.Count;
        dec.DutiableAmount = totalDutiable;
        dec.ImportTaxAmount = totalImportTax;
        dec.SpecialConsumptionTaxAmount = totalSct;
        dec.VatAmount = totalVat;
        dec.TotalTaxAmount = totalImportTax + totalSct + totalVat;

        db.CustomsDeclarations.Add(dec);
        await db.SaveChangesAsync();

        return MapToDto(dec);
    }

    public async Task<CustomsDeclarationDto?> UpdateAsync(string declarationNo, UpdateCustomsDeclarationDto dto)
    {
        var dec = await db.CustomsDeclarations
            .Include(x => x.Details)
            .FirstOrDefaultAsync(x => x.OrgId == tenant.OrgId && x.DeclarationNo == declarationNo);

        if (dec == null) return null;

        if (dec.Status != CustomsDeclarationStatus.Draft && dec.Status != CustomsDeclarationStatus.Submitted)
            throw new InvalidOperationException($"Không thể sửa tờ khai ở trạng thái '{dec.Status}'. Chỉ cho phép sửa khi Draft hoặc Submitted.");

        if (!string.IsNullOrWhiteSpace(dto.PortCode))
        {
            dec.PortCode = dto.PortCode.Trim().ToUpperInvariant();
            var portInfo = AvailablePorts.FirstOrDefault(p => p.PortCode.Equals(dec.PortCode, StringComparison.OrdinalIgnoreCase));
            dec.PortName = dto.PortName ?? portInfo?.PortName ?? dec.PortName;
        }
        else if (!string.IsNullOrWhiteSpace(dto.PortName))
        {
            dec.PortName = dto.PortName.Trim();
        }

        if (dto.ContractNo != null) dec.ContractNo = dto.ContractNo.Trim();
        if (dto.OpenDate.HasValue) dec.OpenDate = dto.OpenDate.Value;
        if (dto.Channel.HasValue) dec.Channel = dto.Channel.Value;
        if (!string.IsNullOrWhiteSpace(dto.CustomsOffice)) dec.CustomsOffice = dto.CustomsOffice.Trim();
        if (!string.IsNullOrWhiteSpace(dto.TaxPayerBank)) dec.TaxPayerBank = dto.TaxPayerBank.Trim();
        if (dto.Remark != null) dec.Remark = dto.Remark.Trim();

        dec.LUDateTime = DateTime.Now;
        dec.LUBy = dto.UpdatedBy ?? "HQ";

        await db.SaveChangesAsync();
        return MapToDto(dec);
    }

    public async Task<CustomsDeclarationDto?> SubmitAsync(string declarationNo, SubmitCustomsDeclarationDto? dto = null)
    {
        var dec = await db.CustomsDeclarations
            .Include(x => x.Details)
            .FirstOrDefaultAsync(x => x.OrgId == tenant.OrgId && x.DeclarationNo == declarationNo);

        if (dec == null) return null;

        if (dec.Status != CustomsDeclarationStatus.Draft)
            throw new InvalidOperationException($"Chỉ có thể nộp tờ khai ở trạng thái Draft. Trạng thái hiện tại: {dec.Status}");

        dec.Status = CustomsDeclarationStatus.Submitted;
        dec.Channel = dto?.Channel ?? dec.Channel;
        dec.SubmittedAt = DateTime.Now;
        dec.SubmittedBy = dto?.SubmittedBy ?? "HQ";
        if (!string.IsNullOrWhiteSpace(dto?.Remark)) dec.Remark = dto.Remark.Trim();
        dec.LUDateTime = DateTime.Now;
        dec.LUBy = dec.SubmittedBy;

        await db.SaveChangesAsync();
        return MapToDto(dec);
    }

    public async Task<CustomsDeclarationDto?> UpdateTaxPaymentDateAsync(string declarationNo, UpdateTaxPaymentDateDto dto)
    {
        var dec = await db.CustomsDeclarations
            .Include(x => x.Details)
            .FirstOrDefaultAsync(x => x.OrgId == tenant.OrgId && x.DeclarationNo == declarationNo);

        if (dec == null) return null;

        if (dec.Status == CustomsDeclarationStatus.Cancelled)
            throw new InvalidOperationException("Không thể cập nhật ngày nộp thuế cho tờ khai đã bị hủy.");

        if (dec.Status == CustomsDeclarationStatus.Cleared)
            throw new InvalidOperationException("Tờ khai đã hoàn tất thông quan, không thể chỉnh sửa nộp thuế.");

        dec.TaxPaymentDate = dto.TaxPaymentDate;
        dec.TaxReceiptNo = dto.TaxReceiptNo?.Trim() ?? dec.TaxReceiptNo ?? $"BLT-{dto.TaxPaymentDate:yyyyMMdd}-{dec.DeclarationNo}";
        if (!string.IsNullOrWhiteSpace(dto.TaxPayerBank)) dec.TaxPayerBank = dto.TaxPayerBank.Trim();
        dec.TaxPaidBy = dto.PaidBy ?? "HQ";
        dec.Status = CustomsDeclarationStatus.TaxPaid;
        dec.LUDateTime = DateTime.Now;
        dec.LUBy = dec.TaxPaidBy;
        if (!string.IsNullOrWhiteSpace(dto.Remark)) dec.Remark = dto.Remark.Trim();

        foreach (var dtl in dec.Details)
        {
            if (dtl.Status != CustomsDeclarationDetailStatus.Cancelled)
            {
                dtl.Status = CustomsDeclarationDetailStatus.TaxPaid;
                dtl.TaxPaymentDate = dto.TaxPaymentDate;
            }
        }

        await db.SaveChangesAsync();
        return MapToDto(dec);
    }

    public async Task<CustomsDeclarationDto?> ClearCustomsAsync(string declarationNo, ClearCustomsDto? dto = null)
    {
        var dec = await db.CustomsDeclarations
            .Include(x => x.Details)
            .FirstOrDefaultAsync(x => x.OrgId == tenant.OrgId && x.DeclarationNo == declarationNo);

        if (dec == null) return null;

        if (dec.Status == CustomsDeclarationStatus.Cancelled)
            throw new InvalidOperationException("Tờ khai đã bị hủy, không thể thông quan.");

        if (dec.Status == CustomsDeclarationStatus.Cleared)
            throw new InvalidOperationException("Tờ khai này đã được thông quan trước đó.");

        if (dec.Status != CustomsDeclarationStatus.TaxPaid && dec.Channel != CustomsChannel.Green)
            throw new InvalidOperationException("Tờ khai chưa hoàn thành nộp thuế (TaxPaid), không thể hoàn tất thông quan trừ khi luồng xanh được chỉ định miễn trước.");

        var clearDate = dto?.CustomsClearanceDate ?? DateTime.Now;
        var clearedBy = dto?.ClearedBy ?? "CustomsOfficer";
        var officer = dto?.CustomsOfficer ?? "Chi cục Hải quan Cửa khẩu";

        dec.Status = CustomsDeclarationStatus.Cleared;
        dec.CustomsClearanceDate = clearDate;
        dec.CustomsOfficer = officer;
        dec.ClearedBy = clearedBy;
        dec.LUDateTime = DateTime.Now;
        dec.LUBy = clearedBy;
        if (!string.IsNullOrWhiteSpace(dto?.Remark)) dec.Remark = dto.Remark.Trim();

        foreach (var dtl in dec.Details)
        {
            if (dtl.Status != CustomsDeclarationDetailStatus.Cancelled)
            {
                dtl.Status = CustomsDeclarationDetailStatus.Cleared;
                dtl.ClearedAt = clearDate;

                // Đồng bộ ngày thông quan vào CarVinInventory
                var inv = await db.CarVinInventories.FirstOrDefaultAsync(x => x.OrgId == tenant.OrgId && x.Vin == dtl.Vin);
                if (inv != null)
                {
                    inv.CustomsClearanceDate = clearDate;
                    inv.DeclarationNo = dec.DeclarationNo;
                }
            }
        }

        await db.SaveChangesAsync();
        return MapToDto(dec);
    }

    public async Task<CustomsDeclarationDto?> AddCarsAsync(string declarationNo, AddDeclarationCarsDto dto)
    {
        var dec = await db.CustomsDeclarations
            .Include(x => x.Details)
            .FirstOrDefaultAsync(x => x.OrgId == tenant.OrgId && x.DeclarationNo == declarationNo);

        if (dec == null) return null;

        if (dec.Status != CustomsDeclarationStatus.Draft && dec.Status != CustomsDeclarationStatus.Submitted)
            throw new InvalidOperationException($"Chỉ có thể thêm xe vào tờ khai khi ở trạng thái Draft hoặc Submitted. Trạng thái hiện tại: {dec.Status}");

        if (dto.Cars == null || dto.Cars.Count == 0)
            throw new InvalidOperationException("Danh sách xe cần thêm không được rỗng.");

        var currentVins = dec.Details.Select(d => d.Vin.ToUpper()).ToHashSet(StringComparer.OrdinalIgnoreCase);

        var newVinList = dto.Cars.Select(c => c.Vin.Trim().ToUpperInvariant()).ToList();
        var existingVins = await db.CustomsDeclarationDetails
            .Where(d => d.Status != CustomsDeclarationDetailStatus.Cancelled && newVinList.Contains(d.Vin.ToUpper()))
            .Join(db.CustomsDeclarations.Where(c => c.OrgId == tenant.OrgId && c.Status != CustomsDeclarationStatus.Cancelled),
                d => d.DeclarationId, c => c.Id, (d, c) => new { d.Vin, c.DeclarationNo })
            .ToListAsync();

        if (existingVins.Count > 0)
        {
            var conflict = existingVins.First();
            throw new InvalidOperationException($"Số khung VIN '{conflict.Vin}' đã được gắn vào tờ khai hải quan '{conflict.DeclarationNo}'.");
        }

        foreach (var c in dto.Cars)
        {
            var cleanVin = c.Vin.Trim().ToUpperInvariant();
            if (currentVins.Contains(cleanVin))
                throw new InvalidOperationException($"Số khung VIN '{cleanVin}' đã tồn tại trong tờ khai này.");

            var tax = CalculateTax(c.ModelCode, c.DutiableValue);

            var detail = new CustomsDeclarationDetail
            {
                DeclarationId = dec.Id,
                DeclarationNo = dec.DeclarationNo,
                Vin = cleanVin,
                EngineNo = c.EngineNo?.Trim(),
                ModelCode = c.ModelCode.Trim().ToUpperInvariant(),
                ModelName = !string.IsNullOrWhiteSpace(c.ModelName) ? c.ModelName.Trim() : c.ModelCode.Trim().ToUpperInvariant(),
                SpecCode = c.SpecCode?.Trim(),
                SpecDescription = c.SpecDescription?.Trim(),
                ColorCode = c.ColorCode?.Trim(),
                ColorName = c.ColorName?.Trim(),
                PackingListNo = c.PackingListNo?.Trim(),
                LCNo = c.LCNo?.Trim(),
                ContractNo = c.ContractNo?.Trim() ?? dec.ContractNo,
                DutiableValue = tax.DutiableValue,
                ImportTax = tax.ImportTax,
                SpecialConsumptionTax = tax.SpecialConsumptionTax,
                Vat = tax.Vat,
                TotalTax = tax.TotalTax,
                Status = CustomsDeclarationDetailStatus.Pending,
                Remark = c.Remark?.Trim()
            };

            dec.Details.Add(detail);
            currentVins.Add(cleanVin);

            var inv = await db.CarVinInventories.FirstOrDefaultAsync(x => x.OrgId == tenant.OrgId && x.Vin == cleanVin);
            if (inv != null) inv.DeclarationNo = dec.DeclarationNo;
        }

        RecalculateTotals(dec);
        dec.LUDateTime = DateTime.Now;
        dec.LUBy = dto.UpdatedBy ?? "HQ";

        await db.SaveChangesAsync();
        return MapToDto(dec);
    }

    public async Task<CustomsDeclarationDto?> RemoveCarAsync(string declarationNo, string vin)
    {
        var dec = await db.CustomsDeclarations
            .Include(x => x.Details)
            .FirstOrDefaultAsync(x => x.OrgId == tenant.OrgId && x.DeclarationNo == declarationNo);

        if (dec == null) return null;

        if (dec.Status != CustomsDeclarationStatus.Draft && dec.Status != CustomsDeclarationStatus.Submitted)
            throw new InvalidOperationException($"Không thể gỡ xe khi tờ khai ở trạng thái '{dec.Status}'.");

        var cleanVin = vin.Trim().ToUpperInvariant();
        var dtl = dec.Details.FirstOrDefault(x => x.Vin.Equals(cleanVin, StringComparison.OrdinalIgnoreCase));
        if (dtl == null)
            throw new InvalidOperationException($"Không tìm thấy số khung VIN '{cleanVin}' trong tờ khai {declarationNo}.");

        dec.Details.Remove(dtl);
        db.CustomsDeclarationDetails.Remove(dtl);

        // Giải phóng DeclarationNo trên CarVinInventory
        var inv = await db.CarVinInventories.FirstOrDefaultAsync(x => x.OrgId == tenant.OrgId && x.Vin == cleanVin);
        if (inv != null && inv.DeclarationNo == declarationNo)
        {
            inv.DeclarationNo = null;
        }

        RecalculateTotals(dec);
        dec.LUDateTime = DateTime.Now;
        dec.LUBy = "HQ";

        await db.SaveChangesAsync();
        return MapToDto(dec);
    }

    public async Task<CustomsDeclarationDto?> CancelAsync(string declarationNo, CancelCustomsDeclarationDto dto)
    {
        var dec = await db.CustomsDeclarations
            .Include(x => x.Details)
            .FirstOrDefaultAsync(x => x.OrgId == tenant.OrgId && x.DeclarationNo == declarationNo);

        if (dec == null) return null;

        if (dec.Status == CustomsDeclarationStatus.Cleared)
            throw new InvalidOperationException("Không thể hủy tờ khai đã thông quan thành công.");

        if (dec.Status == CustomsDeclarationStatus.TaxPaid)
            throw new InvalidOperationException("Tờ khai đã nộp thuế vào NSNN, cần làm thủ tục hoàn thuế / hủy nghiệp vụ trước khi hủy tờ khai.");

        if (dec.Status == CustomsDeclarationStatus.Cancelled)
            throw new InvalidOperationException("Tờ khai này đã được hủy trước đó.");

        dec.Status = CustomsDeclarationStatus.Cancelled;
        dec.CancelledAt = DateTime.Now;
        dec.CancelledBy = dto.CancelledBy ?? "HQ";
        dec.CancelReason = dto.Reason;
        dec.LUDateTime = DateTime.Now;
        dec.LUBy = dec.CancelledBy;

        foreach (var dtl in dec.Details)
        {
            dtl.Status = CustomsDeclarationDetailStatus.Cancelled;

            var inv = await db.CarVinInventories.FirstOrDefaultAsync(x => x.OrgId == tenant.OrgId && x.Vin == dtl.Vin);
            if (inv != null && inv.DeclarationNo == declarationNo)
            {
                inv.DeclarationNo = null;
            }
        }

        await db.SaveChangesAsync();
        return MapToDto(dec);
    }

    public async Task<bool> DeleteDraftAsync(string declarationNo)
    {
        var dec = await db.CustomsDeclarations
            .Include(x => x.Details)
            .FirstOrDefaultAsync(x => x.OrgId == tenant.OrgId && x.DeclarationNo == declarationNo);

        if (dec == null) return false;

        if (dec.Status != CustomsDeclarationStatus.Draft)
            throw new InvalidOperationException($"Chỉ có thể xóa tờ khai ở trạng thái Draft. Trạng thái hiện tại là '{dec.Status}'. Hãy dùng chức năng Hủy.");

        foreach (var dtl in dec.Details)
        {
            var inv = await db.CarVinInventories.FirstOrDefaultAsync(x => x.OrgId == tenant.OrgId && x.Vin == dtl.Vin);
            if (inv != null && inv.DeclarationNo == declarationNo)
            {
                inv.DeclarationNo = null;
            }
        }

        db.CustomsDeclarations.Remove(dec);
        await db.SaveChangesAsync();
        return true;
    }

    public async Task<CustomsStatsDto> StatsAsync()
    {
        var list = await db.CustomsDeclarations
            .Include(x => x.Details)
            .Where(x => x.OrgId == tenant.OrgId)
            .ToListAsync();

        var totalDeclarations = list.Count;
        var totalCars = list.Where(x => x.Status != CustomsDeclarationStatus.Cancelled).Sum(x => x.TotalCars);

        var draftCount = list.Count(x => x.Status == CustomsDeclarationStatus.Draft);
        var submittedCount = list.Count(x => x.Status == CustomsDeclarationStatus.Submitted);
        var taxPaidCount = list.Count(x => x.Status == CustomsDeclarationStatus.TaxPaid);
        var clearedCount = list.Count(x => x.Status == CustomsDeclarationStatus.Cleared);
        var cancelledCount = list.Count(x => x.Status == CustomsDeclarationStatus.Cancelled);

        var activeList = list.Where(x => x.Status != CustomsDeclarationStatus.Cancelled).ToList();
        var totalDutiable = activeList.Sum(x => x.DutiableAmount);
        var totalImportTax = activeList.Sum(x => x.ImportTaxAmount);
        var totalSct = activeList.Sum(x => x.SpecialConsumptionTaxAmount);
        var totalVat = activeList.Sum(x => x.VatAmount);
        var totalTaxPaid = list.Where(x => x.Status == CustomsDeclarationStatus.TaxPaid || x.Status == CustomsDeclarationStatus.Cleared).Sum(x => x.TotalTaxAmount);

        var byPort = list.GroupBy(x => x.PortCode).ToDictionary(g => g.Key, g => g.Count());
        var byChannel = list.GroupBy(x => x.Channel.ToString()).ToDictionary(g => g.Key, g => g.Count());
        var byStatus = list.GroupBy(x => x.Status.ToString()).ToDictionary(g => g.Key, g => g.Count());

        return new CustomsStatsDto(
            TotalDeclarations: totalDeclarations,
            TotalCars: totalCars,
            DraftCount: draftCount,
            SubmittedCount: submittedCount,
            TaxPaidCount: taxPaidCount,
            ClearedCount: clearedCount,
            CancelledCount: cancelledCount,
            TotalDutiableAmount: totalDutiable,
            TotalImportTaxAmount: totalImportTax,
            TotalSctAmount: totalSct,
            TotalVatAmount: totalVat,
            TotalTaxPaidAmount: totalTaxPaid,
            ByPort: byPort,
            ByChannel: byChannel,
            ByStatus: byStatus
        );
    }

    private static void RecalculateTotals(CustomsDeclaration dec)
    {
        var validDetails = dec.Details.Where(x => x.Status != CustomsDeclarationDetailStatus.Cancelled).ToList();
        dec.TotalCars = validDetails.Count;
        dec.DutiableAmount = validDetails.Sum(x => x.DutiableValue);
        dec.ImportTaxAmount = validDetails.Sum(x => x.ImportTax);
        dec.SpecialConsumptionTaxAmount = validDetails.Sum(x => x.SpecialConsumptionTax);
        dec.VatAmount = validDetails.Sum(x => x.Vat);
        dec.TotalTaxAmount = dec.ImportTaxAmount + dec.SpecialConsumptionTaxAmount + dec.VatAmount;
    }

    public static CustomsTaxBreakdownDto CalculateTax(string modelCode, decimal? customDutiableValue)
    {
        var code = (modelCode ?? "").ToUpperInvariant().Trim();
        decimal dutiable;

        if (customDutiableValue.HasValue && customDutiableValue.Value > 0)
        {
            dutiable = customDutiableValue.Value;
        }
        else
        {
            if (code.Contains("PALISADE")) dutiable = 850_000_000m;
            else if (code.Contains("IONIQ")) dutiable = 900_000_000m;
            else if (code.Contains("CUSTIN")) dutiable = 620_000_000m;
            else if (code.Contains("STARGAZER")) dutiable = 420_000_000m;
            else if (code.Contains("SANTAFE")) dutiable = 750_000_000m;
            else if (code.Contains("TUCSON")) dutiable = 600_000_000m;
            else if (code.Contains("CRETA")) dutiable = 480_000_000m;
            else if (code.Contains("ACCENT")) dutiable = 380_000_000m;
            else if (code.Contains("ELANTRA")) dutiable = 460_000_000m;
            else dutiable = 500_000_000m;
        }

        // Thuế suất nhập khẩu CBU
        decimal importRate = 0.50m; // 50%
        if (code.Contains("IONIQ")) importRate = 0.50m; // CBU EV

        var importTax = Math.Round(dutiable * importRate, 0);

        // Thuế suất tiêu thụ đặc biệt (SCT)
        decimal sctRate;
        if (code.Contains("IONIQ")) sctRate = 0.03m; // EV chỉ 3%
        else if (code.Contains("PALISADE")) sctRate = 0.60m; // 3.8L V6 -> 60%
        else if (code.Contains("SANTAFE") || code.Contains("CUSTIN")) sctRate = 0.40m; // 2.5L / 2.0T -> 40%
        else sctRate = 0.35m; // 1.5L / 1.6L -> 35%

        var sctTaxBase = dutiable + importTax;
        var sctTax = Math.Round(sctTaxBase * sctRate, 0);

        // Thuế GTGT VAT 10%
        var vatBase = dutiable + importTax + sctTax;
        var vatTax = Math.Round(vatBase * 0.10m, 0);

        var totalTax = importTax + sctTax + vatTax;

        return new CustomsTaxBreakdownDto(
            DutiableValue: dutiable,
            ImportTaxRate: importRate,
            ImportTax: importTax,
            SctRate: sctRate,
            SpecialConsumptionTax: sctTax,
            VatRate: 0.10m,
            Vat: vatTax,
            TotalTax: totalTax
        );
    }

    private static CustomsDeclarationDto MapToDto(CustomsDeclaration x) => new(
        Id: x.Id,
        DeclarationNo: x.DeclarationNo,
        PortCode: x.PortCode,
        PortName: x.PortName,
        ContractNo: x.ContractNo,
        OpenDate: x.OpenDate,
        TaxPaymentDate: x.TaxPaymentDate,
        TaxReceiptNo: x.TaxReceiptNo,
        TaxPayerBank: x.TaxPayerBank,
        CustomsOffice: x.CustomsOffice,
        Channel: x.Channel,
        Status: x.Status,
        TotalCars: x.TotalCars,
        DutiableAmount: x.DutiableAmount,
        ImportTaxAmount: x.ImportTaxAmount,
        SpecialConsumptionTaxAmount: x.SpecialConsumptionTaxAmount,
        VatAmount: x.VatAmount,
        TotalTaxAmount: x.TotalTaxAmount,
        CustomsClearanceDate: x.CustomsClearanceDate,
        CustomsOfficer: x.CustomsOfficer,
        Remark: x.Remark,
        CreatedBy: x.CreatedBy,
        CreatedAt: x.CreatedAt,
        SubmittedBy: x.SubmittedBy,
        SubmittedAt: x.SubmittedAt,
        TaxPaidBy: x.TaxPaidBy,
        ClearedBy: x.ClearedBy,
        CancelledBy: x.CancelledBy,
        CancelledAt: x.CancelledAt,
        CancelReason: x.CancelReason,
        Details: x.Details.Select(d => new CustomsDeclarationDetailDto(
            Id: d.Id,
            DeclarationId: d.DeclarationId,
            DeclarationNo: d.DeclarationNo,
            Vin: d.Vin,
            EngineNo: d.EngineNo,
            ModelCode: d.ModelCode,
            ModelName: d.ModelName,
            SpecCode: d.SpecCode,
            SpecDescription: d.SpecDescription,
            ColorCode: d.ColorCode,
            ColorName: d.ColorName,
            PackingListNo: d.PackingListNo,
            LCNo: d.LCNo,
            ContractNo: d.ContractNo,
            DutiableValue: d.DutiableValue,
            ImportTax: d.ImportTax,
            SpecialConsumptionTax: d.SpecialConsumptionTax,
            Vat: d.Vat,
            TotalTax: d.TotalTax,
            Status: d.Status,
            TaxPaymentDate: d.TaxPaymentDate,
            ClearedAt: d.ClearedAt,
            Remark: d.Remark
        )).ToList()
    );
}
