using Microsoft.EntityFrameworkCore;
using MiniSales.Data;
using MiniSales.Models;

namespace MiniSales.Services;

public record PackingListDetailInputDto(
    string Vin,
    string? EngineNo = null,
    string? KeyNo = null,
    string ModelCode = "",
    string? ModelName = null,
    string? SpecCode = null,
    string? SpecDescription = null,
    string? ColorCode = null,
    string? ColorName = null,
    string? WorkOrderNo = null,
    string? ProductionMonth = null,
    bool FlagRepair = false,
    string? RepairRemark = null,
    string? StorageCodeCurrent = null,
    string? Remark = null
);

public record CreatePackingListDto(
    string ContractNo,
    string PortCode,
    string? LCNo = null,
    string? PortName = null,
    PackingListType PLType = PackingListType.CBU,
    DateTime? ShippingDateStart = null,
    DateTime? ShippingDateEndExpected = null,
    string? VesselName = null,
    string? VoyageNo = null,
    string? Remark = null,
    string? CreatedBy = null,
    List<PackingListDetailInputDto>? Details = null
);

public record AutoGenerateModelItemDto(
    string ModelCode,
    string? ModelName = null,
    string? SpecCode = null,
    string? SpecDescription = null,
    string? ColorCode = null,
    string? ColorName = null,
    int Quantity = 1
);

public record AutoGeneratePackingListDto(
    string ContractNo,
    string PortCode,
    string ProductionMonth, // yyyy-MM
    string? LCNo = null,
    string? PortName = null,
    string? VesselName = null,
    string? VoyageNo = null,
    DateTime? ShippingDateStart = null,
    DateTime? ShippingDateEndExpected = null,
    string? Remark = null,
    string? CreatedBy = null,
    List<AutoGenerateModelItemDto>? Items = null
);

public record UpdateShipDatesDto(
    DateTime? ShippingDateStart = null,
    DateTime? ShippingDateEnd = null,
    string? VesselName = null,
    string? VoyageNo = null,
    string? UpdatedBy = null
);

public record UpdateExpectedShipDateDto(
    DateTime ShippingDateEndExpected,
    string? UpdatedBy = null
);

public record UpdateVinStatusDto(
    string Vin,
    bool FlagRepair,
    string? RepairRemark = null,
    string? UpdatedBy = null
);

public record BatchUpdateVinStatusDto(
    List<UpdateVinStatusDto> Items,
    string? UpdatedBy = null
);

public record CompleteStockInDto(
    DateTime? StoreDate = null,
    string? StorageCode = null,
    string? StorageName = null,
    string? CompletedBy = null,
    string? Remark = null
);

public record CancelPackingListDto(
    string Reason,
    string? CancelledBy = null
);

public record AddPackingListCarDto(
    string Vin,
    string? EngineNo = null,
    string? KeyNo = null,
    string ModelCode = "",
    string? ModelName = null,
    string? SpecCode = null,
    string? SpecDescription = null,
    string? ColorCode = null,
    string? ColorName = null,
    string? WorkOrderNo = null,
    string? ProductionMonth = null,
    string? Remark = null
);

public record PackingListPortDto(
    string PortCode,
    string PortName,
    string Region
);

public record EligibleVinDto(
    string Vin,
    string ModelCode,
    string ModelName,
    string? SpecCode,
    string? ColorCode,
    string? ContractNo,
    string? ProductionMonth
);

public record PackingListStatsDto(
    int TotalPackingLists,
    int DraftCount,
    int ShippingCount,
    int PortArrivedCount,
    int CompletedCount,
    int CancelledCount,
    int TotalCars,
    int TotalRepairedCars,
    decimal RepairRate,
    Dictionary<string, int> CarsByPort,
    Dictionary<string, int> CarsByModel,
    Dictionary<string, int> PackingListsByType
);

public interface IPackingListService
{
    Task<string> GetNextPackingListNoSeqAsync();
    Task<List<PackingList>> SearchAsync(
        string? packingListNo = null,
        string? contractNo = null,
        string? lcNo = null,
        string? portCode = null,
        PackingListType? plType = null,
        PackingListStatus? status = null,
        string? vin = null,
        DateTime? dateFrom = null,
        DateTime? dateTo = null,
        int pageIndex = 0,
        int pageSize = 50);
    Task<PackingList?> GetByPackingListNoAsync(string packingListNo);
    Task<PackingList> CreateAsync(CreatePackingListDto dto);
    Task<PackingList> CreateAutoAsync(AutoGeneratePackingListDto dto);
    Task<PackingList?> UpdateShipDatesAsync(string packingListNo, UpdateShipDatesDto dto);
    Task<PackingList?> UpdateExpectedShipDateAsync(string packingListNo, UpdateExpectedShipDateDto dto);
    Task<PackingList?> UpdateVinStatusAsync(string packingListNo, BatchUpdateVinStatusDto dto);
    Task<PackingList?> CompleteStockInAsync(string packingListNo, CompleteStockInDto dto);
    Task<PackingList?> CancelAsync(string packingListNo, CancelPackingListDto dto);
    Task<bool> DeleteDraftAsync(string packingListNo);
    Task<PackingList?> AddCarsAsync(string packingListNo, List<AddPackingListCarDto> items, string? updatedBy = null);
    Task<PackingList?> RemoveCarAsync(string packingListNo, string vin, string? updatedBy = null);
    Task<List<PackingListPortDto>> GetPortsAsync();
    Task<List<EligibleVinDto>> GetEligibleVINsAsync(string? contractNo = null);
    Task<PackingListStatsDto> GetStatsAsync();
}

public sealed class PackingListService(AppDbContext db, ITenantContext tenant) : IPackingListService
{
    private static readonly List<PackingListPortDto> KnownPorts = new()
    {
        new("CANG_HAI_PHONG", "Cảng Hải Phòng (Tân Vũ / Đình Vũ)", "Miền Bắc"),
        new("CANG_CAT_LAI", "Cảng Cát Lái (TP. Hồ Chí Minh)", "Miền Nam"),
        new("CANG_CAI_MEP", "Cảng Quốc tế Cái Mép - Thị Vải (Bà Rịa - Vũng Tàu)", "Miền Nam"),
        new("CANG_DA_NANG", "Cảng Tiên Sa - Đà Nẵng", "Miền Trung"),
        new("CANG_QUY_NHON", "Cảng Quy Nhơn (Bình Định)", "Miền Trung")
    };

    private static readonly Dictionary<string, string> ModelNames = new(StringComparer.OrdinalIgnoreCase)
    {
        ["SANTAFE"] = "Hyundai Santa Fe All-New",
        ["PALISADE"] = "Hyundai Palisade Exclusive / Prestige",
        ["IONIQ5"] = "Hyundai Ioniq 5 EV Thuần Điện",
        ["TUCSON"] = "Hyundai Tucson Turbo / All New",
        ["CRETA"] = "Hyundai Creta Smart / Premium",
        ["CUSTIN"] = "Hyundai Custin MPV 7 Chỗ",
        ["ACCENT"] = "Hyundai Accent Sedan",
        ["STARGAZER"] = "Hyundai Stargazer X",
        ["ELANTRA"] = "Hyundai Elantra N-Line"
    };

    public async Task<string> GetNextPackingListNoSeqAsync()
    {
        var prefix = $"{DateTime.Today:yyMM}PL";
        var lastNo = await db.PackingLists
            .Where(x => x.OrgId == tenant.OrgId && x.PackingListNo.StartsWith(prefix))
            .OrderByDescending(x => x.PackingListNo)
            .Select(x => x.PackingListNo)
            .FirstOrDefaultAsync();

        var seq = 1;
        if (!string.IsNullOrEmpty(lastNo) && lastNo.Length >= prefix.Length + 4)
        {
            var seqPart = lastNo.Substring(prefix.Length, 4);
            if (int.TryParse(seqPart, out var lastSeq))
                seq = lastSeq + 1;
        }

        return $"{prefix}{seq:D4}";
    }

    public async Task<List<PackingList>> SearchAsync(
        string? packingListNo = null,
        string? contractNo = null,
        string? lcNo = null,
        string? portCode = null,
        PackingListType? plType = null,
        PackingListStatus? status = null,
        string? vin = null,
        DateTime? dateFrom = null,
        DateTime? dateTo = null,
        int pageIndex = 0,
        int pageSize = 50)
    {
        var q = db.PackingLists
            .Include(x => x.Details)
            .Where(x => x.OrgId == tenant.OrgId)
            .AsNoTracking();

        if (!string.IsNullOrWhiteSpace(packingListNo))
            q = q.Where(x => x.PackingListNo.Contains(packingListNo.Trim()));

        if (!string.IsNullOrWhiteSpace(contractNo))
            q = q.Where(x => x.ContractNo.Contains(contractNo.Trim()));

        if (!string.IsNullOrWhiteSpace(lcNo))
            q = q.Where(x => x.LCNo != null && x.LCNo.Contains(lcNo.Trim()));

        if (!string.IsNullOrWhiteSpace(portCode))
            q = q.Where(x => x.PortCode == portCode.Trim());

        if (plType.HasValue)
            q = q.Where(x => x.PLType == plType.Value);

        if (status.HasValue)
            q = q.Where(x => x.Status == status.Value);

        if (!string.IsNullOrWhiteSpace(vin))
        {
            var cleanVin = vin.Trim().ToUpperInvariant();
            q = q.Where(x => x.Details.Any(d => d.Vin.Contains(cleanVin)));
        }

        if (dateFrom.HasValue)
            q = q.Where(x => x.CreatedAt >= dateFrom.Value);

        if (dateTo.HasValue)
            q = q.Where(x => x.CreatedAt <= dateTo.Value);

        return await q
            .OrderByDescending(x => x.CreatedAt)
            .Skip(pageIndex * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<PackingList?> GetByPackingListNoAsync(string packingListNo)
    {
        return await db.PackingLists
            .Include(x => x.Details)
            .FirstOrDefaultAsync(x => x.OrgId == tenant.OrgId && x.PackingListNo == packingListNo);
    }

    public async Task<PackingList> CreateAsync(CreatePackingListDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.ContractNo))
            throw new InvalidOperationException("ContractNo (Số hợp đồng ngoại) không được để trống.");

        if (string.IsNullOrWhiteSpace(dto.PortCode))
            throw new InvalidOperationException("PortCode (Mã cảng đến) không được để trống.");

        var plNo = await GetNextPackingListNoSeqAsync();

        var port = KnownPorts.FirstOrDefault(p => p.PortCode.Equals(dto.PortCode, StringComparison.OrdinalIgnoreCase));
        var portName = dto.PortName ?? port?.PortName ?? dto.PortCode;

        var status = PackingListStatus.Draft;
        if (dto.ShippingDateStart.HasValue)
            status = PackingListStatus.Shipping;

        var details = new List<PackingListDetail>();
        var totalRepaired = 0;

        if (dto.Details != null && dto.Details.Count > 0)
        {
            var seenVins = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var d in dto.Details)
            {
                if (string.IsNullOrWhiteSpace(d.Vin))
                    continue;

                var vin = d.Vin.Trim().ToUpperInvariant();
                if (seenVins.Contains(vin))
                    throw new InvalidOperationException($"Số khung VIN {vin} bị trùng lặp trong danh sách.");
                seenVins.Add(vin);

                var existsVin = await db.PackingListDetails
                    .AnyAsync(x => x.Vin == vin && x.Status != PackingListDetailStatus.Cancelled);
                if (existsVin)
                    throw new InvalidOperationException($"Số khung VIN {vin} đã tồn tại trong một Packing List khác.");

                var mName = d.ModelName ?? (ModelNames.TryGetValue(d.ModelCode, out var name) ? name : d.ModelCode);
                var dStatus = status == PackingListStatus.Shipping ? PackingListDetailStatus.OnBoard : PackingListDetailStatus.Draft;

                if (d.FlagRepair)
                    totalRepaired++;

                details.Add(new PackingListDetail
                {
                    PackingListNo = plNo,
                    Vin = vin,
                    EngineNo = d.EngineNo?.Trim().ToUpperInvariant(),
                    KeyNo = d.KeyNo?.Trim().ToUpperInvariant(),
                    ModelCode = d.ModelCode.Trim().ToUpperInvariant(),
                    ModelName = mName,
                    SpecCode = d.SpecCode?.Trim().ToUpperInvariant(),
                    SpecDescription = d.SpecDescription,
                    ColorCode = d.ColorCode?.Trim().ToUpperInvariant(),
                    ColorName = d.ColorName,
                    WorkOrderNo = d.WorkOrderNo?.Trim(),
                    ProductionMonth = d.ProductionMonth?.Trim(),
                    FlagRepair = d.FlagRepair,
                    RepairRemark = d.RepairRemark,
                    StorageCodeCurrent = d.StorageCodeCurrent,
                    Status = dStatus,
                    Remark = d.Remark
                });
            }
        }

        var pl = new PackingList
        {
            OrgId = tenant.OrgId,
            PackingListNo = plNo,
            ContractNo = dto.ContractNo.Trim(),
            LCNo = dto.LCNo?.Trim(),
            PortCode = dto.PortCode.Trim(),
            PortName = portName,
            PLType = dto.PLType,
            Status = status,
            ShippingDateStart = dto.ShippingDateStart,
            ShippingDateEndExpected = dto.ShippingDateEndExpected,
            VesselName = dto.VesselName?.Trim(),
            VoyageNo = dto.VoyageNo?.Trim(),
            TotalCars = details.Count,
            TotalRepaired = totalRepaired,
            Remark = dto.Remark,
            CreatedBy = dto.CreatedBy ?? "IMPORT_LOGISTICS_HQ",
            CreatedAt = DateTime.Now,
            Details = details
        };

        db.PackingLists.Add(pl);
        await db.SaveChangesAsync();
        return pl;
    }

    public async Task<PackingList> CreateAutoAsync(AutoGeneratePackingListDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.ContractNo))
            throw new InvalidOperationException("ContractNo (Số hợp đồng ngoại) không được để trống.");

        if (string.IsNullOrWhiteSpace(dto.PortCode))
            throw new InvalidOperationException("PortCode (Mã cảng đến) không được để trống.");

        if (string.IsNullOrWhiteSpace(dto.ProductionMonth))
            throw new InvalidOperationException("ProductionMonth (Tháng sản xuất) không được để trống (định dạng yyyy-MM).");

        if (dto.Items == null || dto.Items.Count == 0)
            throw new InvalidOperationException("Cần chỉ định ít nhất 1 dòng cấu hình model xe để sinh Packing List tự động.");

        var plNo = await GetNextPackingListNoSeqAsync();

        var port = KnownPorts.FirstOrDefault(p => p.PortCode.Equals(dto.PortCode, StringComparison.OrdinalIgnoreCase));
        var portName = dto.PortName ?? port?.PortName ?? dto.PortCode;

        var status = dto.ShippingDateStart.HasValue ? PackingListStatus.Shipping : PackingListStatus.Draft;
        var dStatus = status == PackingListStatus.Shipping ? PackingListDetailStatus.OnBoard : PackingListDetailStatus.Draft;

        var details = new List<PackingListDetail>();
        var random = new Random();
        var currentYear = DateTime.Today.Year;
        var monthCode = DateTime.Today.Month.ToString("D2");

        var counter = 1000 + random.Next(1, 8000);

        foreach (var item in dto.Items)
        {
            var qty = item.Quantity <= 0 ? 1 : item.Quantity;
            var mCode = item.ModelCode.Trim().ToUpperInvariant();
            var mName = item.ModelName ?? (ModelNames.TryGetValue(mCode, out var name) ? name : mCode);
            var sCode = item.SpecCode ?? $"{mCode}-STD-01";
            var cCode = item.ColorCode ?? "WW2";
            var cName = item.ColorName ?? "Trắng";

            for (var i = 0; i < qty; i++)
            {
                counter++;
                var vin = $"KMHE{mCode[0..Math.Min(2, mCode.Length)]}{monthCode}{counter:D6}";
                var engNo = $"G4{mCode[0..Math.Min(2, mCode.Length)]}-{counter:D6}";
                var keyNo = $"K{counter:D4}";
                var woNo = $"WO-HMC-{dto.ProductionMonth.Replace("-", "")}-{counter % 100:D3}";

                details.Add(new PackingListDetail
                {
                    PackingListNo = plNo,
                    Vin = vin,
                    EngineNo = engNo,
                    KeyNo = keyNo,
                    ModelCode = mCode,
                    ModelName = mName,
                    SpecCode = sCode,
                    SpecDescription = item.SpecDescription ?? $"{mName} ({sCode})",
                    ColorCode = cCode,
                    ColorName = cName,
                    WorkOrderNo = woNo,
                    ProductionMonth = dto.ProductionMonth,
                    FlagRepair = false,
                    StorageCodeCurrent = null,
                    Status = dStatus,
                    Remark = "Sinh tự động theo kế hoạch sản xuất HMC"
                });
            }
        }

        var pl = new PackingList
        {
            OrgId = tenant.OrgId,
            PackingListNo = plNo,
            ContractNo = dto.ContractNo.Trim(),
            LCNo = dto.LCNo?.Trim(),
            PortCode = dto.PortCode.Trim(),
            PortName = portName,
            PLType = PackingListType.CBU,
            Status = status,
            ShippingDateStart = dto.ShippingDateStart,
            ShippingDateEndExpected = dto.ShippingDateEndExpected,
            VesselName = dto.VesselName ?? "GLOVIS COURAGE",
            VoyageNo = dto.VoyageNo ?? $"V{DateTime.Today:yyMM}S",
            TotalCars = details.Count,
            TotalRepaired = 0,
            Remark = dto.Remark ?? $"Packing List sinh tự động từ kế hoạch sản xuất tháng {dto.ProductionMonth}",
            CreatedBy = dto.CreatedBy ?? "AUTO_PLANNER_HMC",
            CreatedAt = DateTime.Now,
            Details = details
        };

        db.PackingLists.Add(pl);
        await db.SaveChangesAsync();
        return pl;
    }

    public async Task<PackingList?> UpdateShipDatesAsync(string packingListNo, UpdateShipDatesDto dto)
    {
        var pl = await db.PackingLists
            .Include(x => x.Details)
            .FirstOrDefaultAsync(x => x.OrgId == tenant.OrgId && x.PackingListNo == packingListNo);

        if (pl == null) return null;

        if (pl.Status == PackingListStatus.Completed)
            throw new InvalidOperationException($"Packing List {packingListNo} đã hoàn tất nghiệm thu nhập kho, không được sửa ngày tàu.");

        if (pl.Status == PackingListStatus.Cancelled)
            throw new InvalidOperationException($"Packing List {packingListNo} đã bị hủy.");

        if (dto.ShippingDateStart.HasValue)
        {
            pl.ShippingDateStart = dto.ShippingDateStart.Value;
            if (pl.Status == PackingListStatus.Draft)
            {
                pl.Status = PackingListStatus.Shipping;
                foreach (var d in pl.Details.Where(x => x.Status == PackingListDetailStatus.Draft))
                    d.Status = PackingListDetailStatus.OnBoard;
            }
        }

        if (dto.ShippingDateEnd.HasValue)
        {
            pl.ShippingDateEnd = dto.ShippingDateEnd.Value;
            pl.Status = PackingListStatus.PortArrived;
            foreach (var d in pl.Details.Where(x => x.Status == PackingListDetailStatus.OnBoard || x.Status == PackingListDetailStatus.Draft))
                d.Status = PackingListDetailStatus.PortArrived;
        }

        if (!string.IsNullOrWhiteSpace(dto.VesselName))
            pl.VesselName = dto.VesselName.Trim();

        if (!string.IsNullOrWhiteSpace(dto.VoyageNo))
            pl.VoyageNo = dto.VoyageNo.Trim();

        pl.LUDateTime = DateTime.Now;
        pl.LUBy = dto.UpdatedBy ?? "IMPORT_LOGISTICS_HQ";

        await db.SaveChangesAsync();
        return pl;
    }

    public async Task<PackingList?> UpdateExpectedShipDateAsync(string packingListNo, UpdateExpectedShipDateDto dto)
    {
        var pl = await db.PackingLists
            .FirstOrDefaultAsync(x => x.OrgId == tenant.OrgId && x.PackingListNo == packingListNo);

        if (pl == null) return null;

        if (pl.Status == PackingListStatus.Completed || pl.Status == PackingListStatus.Cancelled)
            throw new InvalidOperationException($"Packing List {packingListNo} ở trạng thái {pl.Status}, không thể điều chỉnh ngày dự kiến.");

        pl.ShippingDateEndExpected = dto.ShippingDateEndExpected;
        pl.LUDateTime = DateTime.Now;
        pl.LUBy = dto.UpdatedBy ?? "IMPORT_LOGISTICS_HQ";

        await db.SaveChangesAsync();
        return pl;
    }

    public async Task<PackingList?> UpdateVinStatusAsync(string packingListNo, BatchUpdateVinStatusDto dto)
    {
        var pl = await db.PackingLists
            .Include(x => x.Details)
            .FirstOrDefaultAsync(x => x.OrgId == tenant.OrgId && x.PackingListNo == packingListNo);

        if (pl == null) return null;

        if (pl.Status == PackingListStatus.Cancelled)
            throw new InvalidOperationException($"Packing List {packingListNo} đã bị hủy.");

        var dict = dto.Items.ToDictionary(k => k.Vin.Trim().ToUpperInvariant(), v => v, StringComparer.OrdinalIgnoreCase);

        foreach (var d in pl.Details)
        {
            if (dict.TryGetValue(d.Vin, out var item))
            {
                d.FlagRepair = item.FlagRepair;
                if (!string.IsNullOrWhiteSpace(item.RepairRemark))
                    d.RepairRemark = item.RepairRemark.Trim();
                else if (!item.FlagRepair)
                    d.RepairRemark = null;
            }
        }

        pl.TotalRepaired = pl.Details.Count(x => x.FlagRepair);
        pl.LUDateTime = DateTime.Now;
        pl.LUBy = dto.UpdatedBy ?? "QC_INSPECTOR_PORT";

        await db.SaveChangesAsync();
        return pl;
    }

    public async Task<PackingList?> CompleteStockInAsync(string packingListNo, CompleteStockInDto dto)
    {
        var pl = await db.PackingLists
            .Include(x => x.Details)
            .FirstOrDefaultAsync(x => x.OrgId == tenant.OrgId && x.PackingListNo == packingListNo);

        if (pl == null) return null;

        if (pl.Status == PackingListStatus.Completed)
            throw new InvalidOperationException($"Packing List {packingListNo} đã hoàn tất nhập kho trước đó.");

        if (pl.Status == PackingListStatus.Cancelled)
            throw new InvalidOperationException($"Packing List {packingListNo} đã bị hủy, không thể nhập kho.");

        var storeDate = dto.StoreDate ?? DateTime.Today;
        var storageCode = dto.StorageCode ?? (pl.PortCode.Contains("HAI_PHONG") ? "KHO_TONG_HN" : "KHO_TONG_SG");
        var storageName = dto.StorageName ?? (storageCode == "KHO_TONG_HN" ? "Kho Tổng Miền Bắc - Đài Tư" : "Kho Tổng Nam Bộ - Hiệp Phước");

        pl.Status = PackingListStatus.Completed;
        if (!pl.ShippingDateEnd.HasValue)
            pl.ShippingDateEnd = storeDate;

        pl.LUDateTime = DateTime.Now;
        pl.LUBy = dto.CompletedBy ?? "WAREHOUSE_MANAGER_HQ";
        if (!string.IsNullOrWhiteSpace(dto.Remark))
            pl.Remark = string.IsNullOrEmpty(pl.Remark) ? dto.Remark : $"{pl.Remark}; {dto.Remark}";

        foreach (var d in pl.Details.Where(x => x.Status != PackingListDetailStatus.Cancelled))
        {
            d.Status = PackingListDetailStatus.StockIn;
            d.StoreDate = storeDate;
            d.StorageCodeCurrent = storageCode;

            // Tự động cấp số khung vào CarVinInventory nếu chưa có
            var exists = await db.CarVinInventories
                .AnyAsync(v => v.OrgId == tenant.OrgId && v.Vin == d.Vin);

            if (!exists)
            {
                db.CarVinInventories.Add(new CarVinInventory
                {
                    OrgId = tenant.OrgId,
                    Vin = d.Vin,
                    ModelCode = d.ModelCode,
                    ModelName = d.ModelName,
                    SpecCode = d.SpecCode,
                    SpecDescription = d.SpecDescription,
                    ColorCode = d.ColorCode,
                    ColorName = d.ColorName,
                    EngineNo = d.EngineNo,
                    StorageCode = storageCode,
                    StorageName = storageName,
                    AssemblyStatus = pl.PLType == PackingListType.CBU ? "CBU" : "CKD",
                    ProductionMonth = d.ProductionMonth?.Replace("-", "") ?? DateTime.Today.ToString("yyyyMM"),
                    Status = CarVinStatus.Available,
                    MappedCarId = null,
                    CreatedAt = DateTime.Now
                });
            }
        }

        await db.SaveChangesAsync();
        return pl;
    }

    public async Task<PackingList?> CancelAsync(string packingListNo, CancelPackingListDto dto)
    {
        var pl = await db.PackingLists
            .Include(x => x.Details)
            .FirstOrDefaultAsync(x => x.OrgId == tenant.OrgId && x.PackingListNo == packingListNo);

        if (pl == null) return null;

        if (pl.Status == PackingListStatus.Completed)
            throw new InvalidOperationException($"Packing List {packingListNo} đã hoàn tất nghiệm thu nhập kho, không thể hủy.");

        if (pl.Status == PackingListStatus.Cancelled)
            throw new InvalidOperationException($"Packing List {packingListNo} đã ở trạng thái hủy.");

        if (string.IsNullOrWhiteSpace(dto.Reason))
            throw new InvalidOperationException("Lý do hủy Packing List không được để trống.");

        pl.Status = PackingListStatus.Cancelled;
        pl.CancelReason = dto.Reason.Trim();
        pl.CancelledBy = dto.CancelledBy ?? "IMPORT_DIRECTOR_HQ";
        pl.CancelledAt = DateTime.Now;
        pl.LUDateTime = DateTime.Now;
        pl.LUBy = pl.CancelledBy;

        foreach (var d in pl.Details)
            d.Status = PackingListDetailStatus.Cancelled;

        await db.SaveChangesAsync();
        return pl;
    }

    public async Task<bool> DeleteDraftAsync(string packingListNo)
    {
        var pl = await db.PackingLists
            .Include(x => x.Details)
            .FirstOrDefaultAsync(x => x.OrgId == tenant.OrgId && x.PackingListNo == packingListNo);

        if (pl == null) return false;

        if (pl.Status != PackingListStatus.Draft)
            throw new InvalidOperationException($"Chỉ cho phép xóa bản nháp Packing List (trạng thái hiện tại: {pl.Status}).");

        db.PackingLists.Remove(pl);
        await db.SaveChangesAsync();
        return true;
    }

    public async Task<PackingList?> AddCarsAsync(string packingListNo, List<AddPackingListCarDto> items, string? updatedBy = null)
    {
        var pl = await db.PackingLists
            .Include(x => x.Details)
            .FirstOrDefaultAsync(x => x.OrgId == tenant.OrgId && x.PackingListNo == packingListNo);

        if (pl == null) return null;

        if (pl.Status == PackingListStatus.Completed || pl.Status == PackingListStatus.Cancelled)
            throw new InvalidOperationException($"Packing List {packingListNo} ở trạng thái {pl.Status}, không thể thêm xe.");

        var seenVins = pl.Details.Select(x => x.Vin).ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var car in items)
        {
            if (string.IsNullOrWhiteSpace(car.Vin)) continue;

            var vin = car.Vin.Trim().ToUpperInvariant();
            if (seenVins.Contains(vin))
                throw new InvalidOperationException($"Số khung VIN {vin} đã có trong Packing List này.");
            seenVins.Add(vin);

            var existsVin = await db.PackingListDetails
                .AnyAsync(x => x.Vin == vin && x.Status != PackingListDetailStatus.Cancelled);
            if (existsVin)
                throw new InvalidOperationException($"Số khung VIN {vin} đã tồn tại trong một Packing List khác.");

            var mName = car.ModelName ?? (ModelNames.TryGetValue(car.ModelCode, out var name) ? name : car.ModelCode);
            var dStatus = pl.Status switch
            {
                PackingListStatus.Shipping => PackingListDetailStatus.OnBoard,
                PackingListStatus.PortArrived => PackingListDetailStatus.PortArrived,
                _ => PackingListDetailStatus.Draft
            };

            pl.Details.Add(new PackingListDetail
            {
                PackingListNo = packingListNo,
                Vin = vin,
                EngineNo = car.EngineNo?.Trim().ToUpperInvariant(),
                KeyNo = car.KeyNo?.Trim().ToUpperInvariant(),
                ModelCode = car.ModelCode.Trim().ToUpperInvariant(),
                ModelName = mName,
                SpecCode = car.SpecCode?.Trim().ToUpperInvariant(),
                SpecDescription = car.SpecDescription,
                ColorCode = car.ColorCode?.Trim().ToUpperInvariant(),
                ColorName = car.ColorName,
                WorkOrderNo = car.WorkOrderNo?.Trim(),
                ProductionMonth = car.ProductionMonth?.Trim(),
                FlagRepair = false,
                Status = dStatus,
                Remark = car.Remark
            });
        }

        pl.TotalCars = pl.Details.Count(x => x.Status != PackingListDetailStatus.Cancelled);
        pl.TotalRepaired = pl.Details.Count(x => x.FlagRepair && x.Status != PackingListDetailStatus.Cancelled);
        pl.LUDateTime = DateTime.Now;
        pl.LUBy = updatedBy ?? "IMPORT_LOGISTICS_HQ";

        await db.SaveChangesAsync();
        return pl;
    }

    public async Task<PackingList?> RemoveCarAsync(string packingListNo, string vin, string? updatedBy = null)
    {
        var pl = await db.PackingLists
            .Include(x => x.Details)
            .FirstOrDefaultAsync(x => x.OrgId == tenant.OrgId && x.PackingListNo == packingListNo);

        if (pl == null) return null;

        if (pl.Status == PackingListStatus.Completed || pl.Status == PackingListStatus.Cancelled)
            throw new InvalidOperationException($"Packing List {packingListNo} ở trạng thái {pl.Status}, không thể xóa xe.");

        var cleanVin = vin.Trim().ToUpperInvariant();
        var d = pl.Details.FirstOrDefault(x => x.Vin.Equals(cleanVin, StringComparison.OrdinalIgnoreCase));
        if (d == null)
            throw new InvalidOperationException($"Không tìm thấy xe có số khung VIN {vin} trong Packing List {packingListNo}.");

        pl.Details.Remove(d);
        pl.TotalCars = pl.Details.Count(x => x.Status != PackingListDetailStatus.Cancelled);
        pl.TotalRepaired = pl.Details.Count(x => x.FlagRepair && x.Status != PackingListDetailStatus.Cancelled);
        pl.LUDateTime = DateTime.Now;
        pl.LUBy = updatedBy ?? "IMPORT_LOGISTICS_HQ";

        await db.SaveChangesAsync();
        return pl;
    }

    public Task<List<PackingListPortDto>> GetPortsAsync() => Task.FromResult(KnownPorts);

    public async Task<List<EligibleVinDto>> GetEligibleVINsAsync(string? contractNo = null)
    {
        var assignedVins = await db.PackingListDetails
            .Where(x => x.Status != PackingListDetailStatus.Cancelled)
            .Select(x => x.Vin)
            .ToListAsync();

        var query = db.CarVinInventories
            .Where(x => x.OrgId == tenant.OrgId && !assignedVins.Contains(x.Vin))
            .AsNoTracking();

        var result = await query
            .Take(30)
            .Select(x => new EligibleVinDto(
                x.Vin,
                x.ModelCode,
                x.ModelName,
                x.SpecCode,
                x.ColorCode,
                contractNo ?? "HMC-2026-VN01",
                x.ProductionMonth
            ))
            .ToListAsync();

        return result;
    }

    public async Task<PackingListStatsDto> GetStatsAsync()
    {
        var pls = await db.PackingLists
            .Include(x => x.Details)
            .Where(x => x.OrgId == tenant.OrgId)
            .AsNoTracking()
            .ToListAsync();

        var totalPls = pls.Count;
        var draft = pls.Count(x => x.Status == PackingListStatus.Draft);
        var shipping = pls.Count(x => x.Status == PackingListStatus.Shipping);
        var arrived = pls.Count(x => x.Status == PackingListStatus.PortArrived);
        var completed = pls.Count(x => x.Status == PackingListStatus.Completed);
        var cancelled = pls.Count(x => x.Status == PackingListStatus.Cancelled);

        var totalCars = pls.Where(x => x.Status != PackingListStatus.Cancelled).Sum(x => x.TotalCars);
        var totalRepaired = pls.Where(x => x.Status != PackingListStatus.Cancelled).Sum(x => x.TotalRepaired);
        var repairRate = totalCars > 0 ? Math.Round((decimal)totalRepaired * 100 / totalCars, 2) : 0m;

        var carsByPort = pls
            .Where(x => x.Status != PackingListStatus.Cancelled)
            .GroupBy(x => x.PortCode)
            .ToDictionary(g => g.Key, g => g.Sum(x => x.TotalCars));

        var carsByModel = pls
            .Where(x => x.Status != PackingListStatus.Cancelled)
            .SelectMany(x => x.Details.Where(d => d.Status != PackingListDetailStatus.Cancelled))
            .GroupBy(d => d.ModelCode)
            .ToDictionary(g => g.Key, g => g.Count());

        var byType = pls
            .GroupBy(x => x.PLType.ToString())
            .ToDictionary(g => g.Key, g => g.Count());

        return new PackingListStatsDto(
            totalPls,
            draft,
            shipping,
            arrived,
            completed,
            cancelled,
            totalCars,
            totalRepaired,
            repairRate,
            carsByPort,
            carsByModel,
            byType
        );
    }
}
