using Microsoft.EntityFrameworkCore;
using MiniSales.Data;
using MiniSales.Models;

namespace MiniSales.Services;

public record FnExpCarInputDto(
    string CarId,
    string Vin,
    string? ModelCode = null,
    string? ModelName = null,
    decimal UnitPriceActual = 0,
    DateTime? SodApprovedDate = null,
    DateTime? SodDepositDutyEndDate = null,
    DateTime? DateStart = null,
    DateTime? DateEnd = null,
    DateTime? TotalCompletedDate = null,
    decimal? TermActual = null,
    string? Remark = null
);

public record CreateFnExpCalcSheetDto(
    string DealerCode,
    string? DealerName = null,
    DateTime? TermFrom = null,
    DateTime? TermTo = null,
    DateTime? TermPrevFrom = null,
    DateTime? TermPrevTo = null,
    decimal FnExpPercent = 5.5m,
    decimal PmtDsTCGPercent = 2.0m,
    string? CAName = null,
    string? FlagEarlyCancel = null,
    string? FlagisHTC = null,
    string? Remark = null,
    string? CreatedBy = null,
    List<FnExpCarInputDto>? Cars = null
);

public record PreviewFnExpCalcSheetDto(
    string DealerCode,
    string? DealerName = null,
    DateTime? TermFrom = null,
    DateTime? TermTo = null,
    DateTime? TermPrevFrom = null,
    DateTime? TermPrevTo = null,
    decimal FnExpPercent = 5.5m,
    decimal PmtDsTCGPercent = 2.0m,
    List<FnExpCarInputDto>? Cars = null
);

public record ConfirmStep1DealerDto(
    string? ApprovedBy = null,
    string? Remark = null
);

public record ApproveStep1HqDto(
    string? ApprovedBy = null,
    string? Remark = null
);

public record ApproveStep2HqDto(
    string? ApprovedBy = null,
    string? FilePathCPTC = null,
    string? FilePathCKTT = null,
    string? Remark = null
);

public record ApproveStep2DealerDto(
    string? ApprovedBy = null,
    string? Remark = null
);

public record CancelFnExpCalcSheetDto(
    string Reason,
    string? CancelledBy = null
);

public record EligibleCarForFnExpDto(
    string CarId,
    string Vin,
    string ModelCode,
    string ModelName,
    string DealerCode,
    decimal UnitPriceActual,
    DateTime? SodApprovedDate,
    DateTime? SodDepositDutyEndDate,
    DateTime? DateStart,
    DateTime? DateEnd,
    DateTime? TotalCompletedDate,
    decimal TermActual,
    string? ContractNo = null
);

public interface ICalcFnExpPmDcService
{
    Task<object> GetSeqAsync(string? dealerCode = null);
    Task<object> GetEligibleCarsAsync(string? dealerCode = null, DateTime? termFrom = null, DateTime? termTo = null);
    Task<object> CalculatePreviewAsync(PreviewFnExpCalcSheetDto dto);
    Task<object> CreateAsync(CreateFnExpCalcSheetDto dto);
    Task<object> ListAsync(
        string? dealerCode = null,
        string? caNo = null,
        FnExpStatus? status = null,
        FnExpSignStatus? dlrSignStatus = null,
        FnExpSignStatus? htcSignStatus = null,
        DateTime? termFrom = null,
        DateTime? termTo = null,
        int page = 0,
        int pageSize = 10
    );
    Task<object?> DetailAsync(string caNo);
    Task<object?> ConfirmStep1DealerAsync(string caNo, ConfirmStep1DealerDto? dto = null);
    Task<object?> ApproveStep1HqAsync(string caNo, ApproveStep1HqDto? dto = null);
    Task<object?> ApproveStep2HqAsync(string caNo, ApproveStep2HqDto? dto = null);
    Task<object?> ApproveStep2DealerAsync(string caNo, ApproveStep2DealerDto? dto = null);
    Task<object?> CancelAsync(string caNo, CancelFnExpCalcSheetDto dto);
    Task<bool> DeleteDraftAsync(string caNo);
    Task<object?> PrintCptcAsync(string caNo);
    Task<object?> PrintCkttAsync(string caNo);
    Task<object> StatsAsync(string? dealerCode = null);
}

public class CalcFnExpPmDcService : ICalcFnExpPmDcService
{
    private readonly AppDbContext _db;
    private readonly ITenantContext _tenant;

    public const decimal DEFAULT_DEPOSIT_RATIO_CKD = 0.15m;
    public const decimal DAYS_IN_YEAR = 360m;

    public CalcFnExpPmDcService(AppDbContext db, ITenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    private static readonly Dictionary<string, string> DealerNames = new()
    {
        { "VS058", "Hyundai Bình Dương" },
        { "VN012", "Hyundai Hà Đông" },
        { "VN065", "Hyundai Phạm Văn Đồng" },
        { "VN001", "Hyundai Ngọc An" },
        { "VN033", "Hyundai Sông Hàn Đà Nẵng" }
    };

    public async Task<object> GetSeqAsync(string? dealerCode = null)
    {
        var dlCode = string.IsNullOrWhiteSpace(dealerCode) ? "VS058" : dealerCode.Trim().ToUpperInvariant();
        var datePrefix = DateTime.Now.ToString("yyMMdd");

        var countToday = await _db.FnExpCalcSheets
            .Where(x => x.OrgId == _tenant.OrgId && x.DealerCode == dlCode && x.CaNo.StartsWith(datePrefix))
            .CountAsync();

        var seq = countToday + 1;
        var caNo = $"{datePrefix}-{seq:D3}/CPTC/{dlCode}";
        return new { success = true, caNo, dealerCode = dlCode, seq };
    }

    public async Task<object> GetEligibleCarsAsync(string? dealerCode = null, DateTime? termFrom = null, DateTime? termTo = null)
    {
        var dlCode = string.IsNullOrWhiteSpace(dealerCode) ? null : dealerCode.Trim().ToUpperInvariant();
        var fromDate = termFrom ?? DateTime.Now.AddMonths(-1);
        var toDate = termTo ?? DateTime.Now;

        // Tìm các xe bán buôn từ CarRecord và DealerOrders
        var usedVins = await _db.FnExpCalcSheetDetails
            .Where(x => x.Status != FnExpStatus.Cancelled)
            .Select(x => x.Vin)
            .Distinct()
            .ToListAsync();

        var carRecordsQuery = _db.Cars.Where(x => x.OrgId == _tenant.OrgId && !string.IsNullOrEmpty(x.Vin));
        if (!string.IsNullOrEmpty(dlCode))
            carRecordsQuery = carRecordsQuery.Where(x => x.DealerCode == dlCode);

        var carRecords = await carRecordsQuery
            .Where(x => !usedVins.Contains(x.Vin))
            .Take(20)
            .ToListAsync();

        var result = new List<EligibleCarForFnExpDto>();
        foreach (var car in carRecords)
        {
            var modelCode = car.ModelCode ?? "CRETA";
            var modelName = car.ModelName ?? "Hyundai Creta Turbo";
            var sodDate = car.CreatedAt.AddDays(-45);
            var depositEnd = sodDate.AddDays(7);
            var grtStart = sodDate.AddDays(10);
            var grtEnd = grtStart.AddDays(60);
            var totalCompleted = grtStart.AddDays(25); // Thanh toán sớm trước hạn bảo lãnh
            var price = car.PriceActual > 0 ? car.PriceActual : 750000000m;

            result.Add(new EligibleCarForFnExpDto(
                CarId: car.CarId,
                Vin: car.Vin,
                ModelCode: modelCode,
                ModelName: modelName,
                DealerCode: car.DealerCode ?? dlCode ?? "VS058",
                UnitPriceActual: price,
                SodApprovedDate: sodDate,
                SodDepositDutyEndDate: depositEnd,
                DateStart: grtStart,
                DateEnd: grtEnd,
                TotalCompletedDate: totalCompleted,
                TermActual: 60,
                ContractNo: $"CT-{car.DealerCode}-2026-001"
            ));
        }

        // Nếu DB chưa có sẵn CarRecord, tạo dữ liệu mẫu phong phú
        if (result.Count == 0)
        {
            var fallbackDl = dlCode ?? "VS058";
            var now = DateTime.Now;
            var sampleCars = new List<EligibleCarForFnExpDto>
            {
                new(
                    CarId: $"{now:yyMM}C101-ACCENT",
                    Vin: $"KMHCT41BPST{now:MM}0101",
                    ModelCode: "ACCENT",
                    ModelName: "Hyundai Accent Sedan 1.5 AT",
                    DealerCode: fallbackDl,
                    UnitPriceActual: 569000000m,
                    SodApprovedDate: now.AddDays(-40),
                    SodDepositDutyEndDate: now.AddDays(-33),
                    DateStart: now.AddDays(-30),
                    DateEnd: now.AddDays(30),
                    TotalCompletedDate: now.AddDays(-10),
                    TermActual: 60,
                    ContractNo: $"CT-{fallbackDl}-2026-001"
                ),
                new(
                    CarId: $"{now:yyMM}C102-CRETA",
                    Vin: $"KMHCT41BPST{now:MM}0102",
                    ModelCode: "CRETA",
                    ModelName: "Hyundai Creta B-SUV 1.5 Cao Cấp",
                    DealerCode: fallbackDl,
                    UnitPriceActual: 699000000m,
                    SodApprovedDate: now.AddDays(-38),
                    SodDepositDutyEndDate: now.AddDays(-31),
                    DateStart: now.AddDays(-28),
                    DateEnd: now.AddDays(32),
                    TotalCompletedDate: now.AddDays(-5),
                    TermActual: 60,
                    ContractNo: $"CT-{fallbackDl}-2026-001"
                ),
                new(
                    CarId: $"{now:yyMM}C103-TUCSON",
                    Vin: $"KMHCT41BPST{now:MM}0103",
                    ModelCode: "TUCSON",
                    ModelName: "Hyundai Tucson 1.6 Turbo AWD",
                    DealerCode: fallbackDl,
                    UnitPriceActual: 959000000m,
                    SodApprovedDate: now.AddDays(-35),
                    SodDepositDutyEndDate: now.AddDays(-28),
                    DateStart: now.AddDays(-25),
                    DateEnd: now.AddDays(35),
                    TotalCompletedDate: now.AddDays(-2),
                    TermActual: 60,
                    ContractNo: $"CT-{fallbackDl}-2026-002"
                ),
                new(
                    CarId: $"{now:yyMM}C104-SANTAFE",
                    Vin: $"KMHCT41BPST{now:MM}0104",
                    ModelCode: "SANTAFE",
                    ModelName: "Hyundai Santa Fe 2.5 All New",
                    DealerCode: fallbackDl,
                    UnitPriceActual: 1369000000m,
                    SodApprovedDate: now.AddDays(-30),
                    SodDepositDutyEndDate: now.AddDays(-23),
                    DateStart: now.AddDays(-20),
                    DateEnd: now.AddDays(40),
                    TotalCompletedDate: now.AddDays(-1),
                    TermActual: 60,
                    ContractNo: $"CT-{fallbackDl}-2026-002"
                )
            };
            result.AddRange(sampleCars);
        }

        return new { success = true, dealerCode = dlCode, count = result.Count, cars = result };
    }

    private static (decimal fnDepositCountDate, decimal fnGrtCountDate, decimal pdCountDate, decimal fnDepositAmount, decimal fnGrtAmount, decimal fnTotalAmount, decimal pdAmount) CalculateSingleCar(
        decimal unitPriceActual,
        DateTime termFrom,
        DateTime termTo,
        DateTime termPrevTo,
        DateTime? sodDepositDutyEndDate,
        DateTime? dateEnd,
        DateTime? totalCompletedDate,
        decimal fnExpPercent,
        decimal pmtDsTCGPercent)
    {
        decimal fnDepositCountDate = 0;
        decimal fnGrtCountDate = 0;
        decimal pdCountDate = 0;

        // 1. Tính số ngày tính CPTC Cọc chậm (FnDepositCountDate) theo chuẩn DMS40_FnExp_Calc_FnExp_PmDc_Calc_CntDate_Pmt:
        if (totalCompletedDate.HasValue && totalCompletedDate.Value.Date >= termFrom.Date && totalCompletedDate.Value.Date <= termTo.Date)
        {
            if (sodDepositDutyEndDate.HasValue && sodDepositDutyEndDate.Value.Date >= termFrom.Date && sodDepositDutyEndDate.Value.Date <= termTo.Date)
                fnDepositCountDate = (decimal)(totalCompletedDate.Value.Date - sodDepositDutyEndDate.Value.Date).TotalDays;
            else if (sodDepositDutyEndDate.HasValue && sodDepositDutyEndDate.Value.Date < termFrom.Date)
                fnDepositCountDate = (decimal)(totalCompletedDate.Value.Date - termPrevTo.Date).TotalDays;
            else
                fnDepositCountDate = 0;
        }
        else if (!totalCompletedDate.HasValue || totalCompletedDate.Value.Date > termTo.Date)
        {
            if (sodDepositDutyEndDate.HasValue && sodDepositDutyEndDate.Value.Date >= termFrom.Date && sodDepositDutyEndDate.Value.Date <= termTo.Date)
                fnDepositCountDate = (decimal)(termTo.Date - sodDepositDutyEndDate.Value.Date).TotalDays;
            else if (sodDepositDutyEndDate.HasValue && sodDepositDutyEndDate.Value.Date < termFrom.Date)
                fnDepositCountDate = (decimal)(termTo.Date - termPrevTo.Date).TotalDays;
            else
                fnDepositCountDate = 0;
        }
        if (fnDepositCountDate < 0) fnDepositCountDate = 0;

        // 2. Tính số ngày tính CPTC Bảo lãnh chậm (FnGrtCountDate) theo chuẩn DMS40_FnExp_Calc_FnExp_PmDc_Calc_CntDate_Pmt:
        if (totalCompletedDate.HasValue && totalCompletedDate.Value.Date >= termFrom.Date && totalCompletedDate.Value.Date <= termTo.Date)
        {
            if (dateEnd.HasValue && dateEnd.Value.Date >= termFrom.Date && dateEnd.Value.Date <= termTo.Date)
                fnGrtCountDate = (decimal)(totalCompletedDate.Value.Date - dateEnd.Value.Date).TotalDays;
            else if (dateEnd.HasValue && dateEnd.Value.Date < termFrom.Date)
                fnGrtCountDate = (decimal)(totalCompletedDate.Value.Date - termPrevTo.Date).TotalDays;
            else
                fnGrtCountDate = 0;
        }
        else if (!totalCompletedDate.HasValue || totalCompletedDate.Value.Date > termTo.Date)
        {
            if (dateEnd.HasValue && dateEnd.Value.Date >= termFrom.Date && dateEnd.Value.Date <= termTo.Date)
                fnGrtCountDate = (decimal)(termTo.Date - dateEnd.Value.Date).TotalDays;
            else if (dateEnd.HasValue && dateEnd.Value.Date < termFrom.Date)
                fnGrtCountDate = (decimal)(termTo.Date - termPrevTo.Date).TotalDays;
            else
                fnGrtCountDate = 0;
        }
        if (fnGrtCountDate < 0) fnGrtCountDate = 0;

        // 3. Tính số ngày hưởng Chiết khấu thanh toán sớm (PDCountDate):
        // Nếu hoàn thành thanh toán trước ngày hết hạn bảo lãnh DateEnd
        if (totalCompletedDate.HasValue && dateEnd.HasValue && totalCompletedDate.Value.Date < dateEnd.Value.Date)
        {
            pdCountDate = (decimal)(dateEnd.Value.Date - totalCompletedDate.Value.Date).TotalDays;
            if (pdCountDate < 0) pdCountDate = 0;
        }

        // Tiền CPTC cọc chậm (15% giá xe * % lãi suất CPTC * số ngày / 360)
        decimal fnDepositAmount = Math.Round((DEFAULT_DEPOSIT_RATIO_CKD * unitPriceActual * (fnExpPercent / 100m) * fnDepositCountDate) / DAYS_IN_YEAR, 0);

        // Tiền CPTC bảo lãnh chậm (85% giá xe * % lãi suất CPTC * số ngày / 360)
        decimal fnGrtAmount = Math.Round(((1m - DEFAULT_DEPOSIT_RATIO_CKD) * unitPriceActual * (fnExpPercent / 100m) * fnGrtCountDate) / DAYS_IN_YEAR, 0);

        // Tổng CPTC xe
        decimal fnTotalAmount = fnDepositAmount + fnGrtAmount;

        // Tiền CKTT sớm (Giá xe * % chiết khấu TCG * số ngày / 360)
        decimal pdAmount = Math.Round((unitPriceActual * (pmtDsTCGPercent / 100m) * pdCountDate) / DAYS_IN_YEAR, 0);

        return (fnDepositCountDate, fnGrtCountDate, pdCountDate, fnDepositAmount, fnGrtAmount, fnTotalAmount, pdAmount);
    }

    public Task<object> CalculatePreviewAsync(PreviewFnExpCalcSheetDto dto)
    {
        var termFrom = dto.TermFrom ?? new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
        var termTo = dto.TermTo ?? termFrom.AddMonths(1).AddDays(-1);
        var termPrevFrom = dto.TermPrevFrom ?? termFrom.AddMonths(-1);
        var termPrevTo = dto.TermPrevTo ?? termFrom.AddDays(-1);

        var cars = dto.Cars ?? new List<FnExpCarInputDto>();
        var calculatedCars = new List<object>();

        decimal totalFnDeposit = 0;
        decimal totalFnGrt = 0;
        decimal totalFn = 0;
        decimal totalPD = 0;

        foreach (var car in cars)
        {
            var res = CalculateSingleCar(
                car.UnitPriceActual,
                termFrom,
                termTo,
                termPrevTo,
                car.SodDepositDutyEndDate,
                car.DateEnd,
                car.TotalCompletedDate,
                dto.FnExpPercent,
                dto.PmtDsTCGPercent
            );

            totalFnDeposit += res.fnDepositAmount;
            totalFnGrt += res.fnGrtAmount;
            totalFn += res.fnTotalAmount;
            totalPD += res.pdAmount;

            calculatedCars.Add(new
            {
                car.CarId,
                car.Vin,
                car.ModelCode,
                car.ModelName,
                car.UnitPriceActual,
                car.SodApprovedDate,
                car.SodDepositDutyEndDate,
                car.DateStart,
                car.DateEnd,
                car.TotalCompletedDate,
                FnDepositCountDate = res.fnDepositCountDate,
                FnDepositAmount = res.fnDepositAmount,
                FnGrtCountDate = res.fnGrtCountDate,
                FnGrtAmount = res.fnGrtAmount,
                FnTotalAmount = res.fnTotalAmount,
                PDCountDate = res.pdCountDate,
                PDAmount = res.pdAmount,
                NetBenefit = res.pdAmount - res.fnTotalAmount
            });
        }

        return Task.FromResult<object>(new
        {
            success = true,
            dealerCode = dto.DealerCode,
            termFrom,
            termTo,
            termPrevFrom,
            termPrevTo,
            fnExpPercent = dto.FnExpPercent,
            pmtDsTCGPercent = dto.PmtDsTCGPercent,
            totalCars = cars.Count,
            totalFnDepositAmount = totalFnDeposit,
            totalFnGrtAmount = totalFnGrt,
            totalFnAmount = totalFn,
            totalPDAmount = totalPD,
            netSettlementAmount = totalPD - totalFn,
            cars = calculatedCars
        });
    }

    public async Task<object> CreateAsync(CreateFnExpCalcSheetDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.DealerCode))
            throw new InvalidOperationException("Mã đại lý (DealerCode) không được để trống.");

        if (dto.FnExpPercent < 0 || dto.FnExpPercent > 100)
            throw new InvalidOperationException("Tỷ lệ CPTC (FnExpPercent) phải nằm trong khoảng từ 0% đến 100%.");

        if (dto.PmtDsTCGPercent < 0 || dto.PmtDsTCGPercent > 100)
            throw new InvalidOperationException("Tỷ lệ CKTT (PmtDsTCGPercent) phải nằm trong khoảng từ 0% đến 100%.");

        var dlCode = dto.DealerCode.Trim().ToUpperInvariant();
        var dlName = dto.DealerName ?? (DealerNames.TryGetValue(dlCode, out var n) ? n : $"Đại lý {dlCode}");

        var now = DateTime.Now;
        var termFrom = dto.TermFrom ?? new DateTime(now.Year, now.Month, 1);
        var termTo = dto.TermTo ?? termFrom.AddMonths(1).AddDays(-1);
        var termPrevFrom = dto.TermPrevFrom ?? termFrom.AddMonths(-1);
        var termPrevTo = dto.TermPrevTo ?? termFrom.AddDays(-1);

        // Sinh số CaNo chuẩn: {yyMMdd}-{seq:D3}/CPTC/{DealerCode}
        var datePrefix = now.ToString("yyMMdd");
        var countToday = await _db.FnExpCalcSheets
            .Where(x => x.OrgId == _tenant.OrgId && x.DealerCode == dlCode && x.CaNo.StartsWith(datePrefix))
            .CountAsync();
        var seq = countToday + 1;
        var caNo = $"{datePrefix}-{seq:D3}/CPTC/{dlCode}";

        var sheet = new FnExpCalcSheet
        {
            OrgId = _tenant.OrgId,
            CaNo = caNo,
            DealerCode = dlCode,
            DealerName = dlName,
            TermFrom = termFrom,
            TermTo = termTo,
            TermPrevFrom = termPrevFrom,
            TermPrevTo = termPrevTo,
            FnExpPercent = dto.FnExpPercent,
            PmtDsTCGPercent = dto.PmtDsTCGPercent,
            CAName = dto.CAName ?? $"Bảng tính CPTC & CKTT kỳ {termFrom:MM/yyyy} - {dlName}",
            FlagEarlyCancel = dto.FlagEarlyCancel ?? "0",
            FlagisHTC = dto.FlagisHTC ?? "HTC",
            DlrSignStatus = FnExpSignStatus.Pending,
            HTCSignStatus = FnExpSignStatus.Pending,
            Status = FnExpStatus.NotSign,
            Remark = dto.Remark,
            CreatedAt = now,
            CreatedBy = dto.CreatedBy ?? "HQ_SPECIALIST"
        };

        var carsInput = dto.Cars ?? new List<FnExpCarInputDto>();
        decimal totalFnDeposit = 0;
        decimal totalFnGrt = 0;
        decimal totalFn = 0;
        decimal totalPD = 0;

        foreach (var car in carsInput)
        {
            var res = CalculateSingleCar(
                car.UnitPriceActual,
                termFrom,
                termTo,
                termPrevTo,
                car.SodDepositDutyEndDate,
                car.DateEnd,
                car.TotalCompletedDate,
                dto.FnExpPercent,
                dto.PmtDsTCGPercent
            );

            totalFnDeposit += res.fnDepositAmount;
            totalFnGrt += res.fnGrtAmount;
            totalFn += res.fnTotalAmount;
            totalPD += res.pdAmount;

            var dtl = new FnExpCalcSheetDetail
            {
                CaNo = caNo,
                CarId = car.CarId,
                Vin = car.Vin,
                ModelCode = car.ModelCode ?? "MODEL",
                ModelName = car.ModelName ?? "Xe Hyundai",
                UnitPriceActual = car.UnitPriceActual,
                SodApprovedDate = car.SodApprovedDate,
                SodDepositDutyEndDate = car.SodDepositDutyEndDate,
                DateStart = car.DateStart,
                DateEnd = car.DateEnd,
                TotalCompletedDate = car.TotalCompletedDate,
                TermActual = car.TermActual ?? 60,
                FnDepositCountDate = res.fnDepositCountDate,
                FnDepositAmount = res.fnDepositAmount,
                FnGrtCountDate = res.fnGrtCountDate,
                FnGrtAmount = res.fnGrtAmount,
                FnTotalAmount = res.fnTotalAmount,
                PDCountDate = res.pdCountDate,
                PDAmount = res.pdAmount,
                Status = FnExpStatus.NotSign,
                Remark = car.Remark
            };
            sheet.Details.Add(dtl);
        }

        sheet.TotalCars = sheet.Details.Count;
        sheet.TotalFnDepositAmount = totalFnDeposit;
        sheet.TotalFnGrtAmount = totalFnGrt;
        sheet.TotalFnAmount = totalFn;
        sheet.TotalPDAmount = totalPD;
        sheet.NetSettlementAmount = totalPD - totalFn;

        _db.FnExpCalcSheets.Add(sheet);
        await _db.SaveChangesAsync();

        return new
        {
            success = true,
            caNo = sheet.CaNo,
            sheetId = sheet.Id,
            dealerCode = sheet.DealerCode,
            dealerName = sheet.DealerName,
            termFrom = sheet.TermFrom,
            termTo = sheet.TermTo,
            totalCars = sheet.TotalCars,
            totalFnAmount = sheet.TotalFnAmount,
            totalPDAmount = sheet.TotalPDAmount,
            netSettlementAmount = sheet.NetSettlementAmount,
            status = sheet.Status.ToString(),
            dlrSignStatus = sheet.DlrSignStatus.ToString(),
            htcSignStatus = sheet.HTCSignStatus.ToString(),
            message = "Tạo bảng tính CPTC & CKTT thành công."
        };
    }

    public async Task<object> ListAsync(
        string? dealerCode = null,
        string? caNo = null,
        FnExpStatus? status = null,
        FnExpSignStatus? dlrSignStatus = null,
        FnExpSignStatus? htcSignStatus = null,
        DateTime? termFrom = null,
        DateTime? termTo = null,
        int page = 0,
        int pageSize = 10)
    {
        var q = _db.FnExpCalcSheets.Include(x => x.Details).Where(x => x.OrgId == _tenant.OrgId);

        if (!string.IsNullOrWhiteSpace(dealerCode))
            q = q.Where(x => x.DealerCode == dealerCode.Trim().ToUpperInvariant());

        if (!string.IsNullOrWhiteSpace(caNo))
            q = q.Where(x => x.CaNo.Contains(caNo.Trim()));

        if (status.HasValue)
            q = q.Where(x => x.Status == status.Value);

        if (dlrSignStatus.HasValue)
            q = q.Where(x => x.DlrSignStatus == dlrSignStatus.Value);

        if (htcSignStatus.HasValue)
            q = q.Where(x => x.HTCSignStatus == htcSignStatus.Value);

        if (termFrom.HasValue)
            q = q.Where(x => x.TermFrom >= termFrom.Value);

        if (termTo.HasValue)
            q = q.Where(x => x.TermTo <= termTo.Value);

        var total = await q.CountAsync();
        var items = await q
            .OrderByDescending(x => x.CreatedAt)
            .Skip(page * pageSize)
            .Take(pageSize)
            .Select(x => new
            {
                x.Id,
                x.CaNo,
                x.DealerCode,
                x.DealerName,
                x.TermFrom,
                x.TermTo,
                x.FnExpPercent,
                x.PmtDsTCGPercent,
                x.CAName,
                Status = x.Status.ToString(),
                DlrSignStatus = x.DlrSignStatus.ToString(),
                HTCSignStatus = x.HTCSignStatus.ToString(),
                x.TotalCars,
                x.TotalFnDepositAmount,
                x.TotalFnGrtAmount,
                x.TotalFnAmount,
                x.TotalPDAmount,
                x.NetSettlementAmount,
                x.FilePathFnExp,
                x.FilePathPmtDC,
                x.CreatedAt,
                x.CreatedBy,
                x.DlrAppr1At,
                x.DlrAppr1By,
                x.HTCAppr1At,
                x.HTCAppr1By,
                x.DlrAppr2At,
                x.DlrAppr2By,
                x.HTCAppr2At,
                x.HTCAppr2By
            })
            .ToListAsync();

        return new { success = true, total, page, pageSize, items };
    }

    public async Task<object?> DetailAsync(string caNo)
    {
        var sheet = await _db.FnExpCalcSheets
            .Include(x => x.Details)
            .FirstOrDefaultAsync(x => x.OrgId == _tenant.OrgId && x.CaNo == caNo.Trim());

        if (sheet == null) return null;

        return new
        {
            success = true,
            sheet.Id,
            sheet.CaNo,
            sheet.DealerCode,
            sheet.DealerName,
            sheet.TermFrom,
            sheet.TermTo,
            sheet.TermPrevFrom,
            sheet.TermPrevTo,
            sheet.FnExpPercent,
            sheet.PmtDsTCGPercent,
            sheet.CAName,
            sheet.FlagEarlyCancel,
            sheet.FlagisHTC,
            Status = sheet.Status.ToString(),
            DlrSignStatus = sheet.DlrSignStatus.ToString(),
            HTCSignStatus = sheet.HTCSignStatus.ToString(),
            sheet.TotalCars,
            sheet.TotalFnDepositAmount,
            sheet.TotalFnGrtAmount,
            sheet.TotalFnAmount,
            sheet.TotalPDAmount,
            sheet.NetSettlementAmount,
            sheet.FilePathFnExp,
            sheet.FilePathPmtDC,
            sheet.CreatedAt,
            sheet.CreatedBy,
            sheet.DlrAppr1At,
            sheet.DlrAppr1By,
            sheet.HTCAppr1At,
            sheet.HTCAppr1By,
            sheet.DlrAppr2At,
            sheet.DlrAppr2By,
            sheet.HTCAppr2At,
            sheet.HTCAppr2By,
            sheet.CancelReason,
            sheet.CancelledBy,
            sheet.CancelledAt,
            sheet.Remark,
            Details = sheet.Details.Select(d => new
            {
                d.Id,
                d.CarId,
                d.Vin,
                d.ModelCode,
                d.ModelName,
                d.UnitPriceActual,
                d.SodApprovedDate,
                d.SodDepositDutyEndDate,
                d.DateStart,
                d.DateEnd,
                d.TotalCompletedDate,
                d.TermActual,
                d.FnDepositCountDate,
                d.FnDepositAmount,
                d.FnGrtCountDate,
                d.FnGrtAmount,
                d.FnTotalAmount,
                d.PDCountDate,
                d.PDAmount,
                NetBenefit = d.PDAmount - d.FnTotalAmount,
                Status = d.Status.ToString(),
                d.Remark
            }).ToList()
        };
    }

    public async Task<object?> ConfirmStep1DealerAsync(string caNo, ConfirmStep1DealerDto? dto = null)
    {
        var sheet = await _db.FnExpCalcSheets
            .FirstOrDefaultAsync(x => x.OrgId == _tenant.OrgId && x.CaNo == caNo.Trim());

        if (sheet == null) return null;

        if (sheet.Status == FnExpStatus.Cancelled)
            throw new InvalidOperationException($"Bảng tính {caNo} đã bị hủy, không thể xác nhận.");

        if (sheet.DlrSignStatus != FnExpSignStatus.Pending)
            throw new InvalidOperationException($"Bảng tính {caNo} đã được đại lý xác nhận bước 1 trước đó (Trạng thái hiện tại: {sheet.DlrSignStatus}).");

        sheet.DlrSignStatus = FnExpSignStatus.Approved1;
        sheet.DlrAppr1At = DateTime.Now;
        sheet.DlrAppr1By = dto?.ApprovedBy ?? $"{sheet.DealerCode}_ACCOUNTANT";
        sheet.LogLUDateTime = DateTime.Now;
        sheet.LogLUBy = sheet.DlrAppr1By;

        await _db.SaveChangesAsync();

        return new
        {
            success = true,
            caNo = sheet.CaNo,
            dlrSignStatus = sheet.DlrSignStatus.ToString(),
            htcSignStatus = sheet.HTCSignStatus.ToString(),
            status = sheet.Status.ToString(),
            sheet.DlrAppr1At,
            sheet.DlrAppr1By,
            message = "Đại lý đã đối soát và xác nhận bước 1 thành công."
        };
    }

    public async Task<object?> ApproveStep1HqAsync(string caNo, ApproveStep1HqDto? dto = null)
    {
        var sheet = await _db.FnExpCalcSheets
            .FirstOrDefaultAsync(x => x.OrgId == _tenant.OrgId && x.CaNo == caNo.Trim());

        if (sheet == null) return null;

        if (sheet.Status == FnExpStatus.Cancelled)
            throw new InvalidOperationException($"Bảng tính {caNo} đã bị hủy, không thể duyệt.");

        if (sheet.DlrSignStatus != FnExpSignStatus.Approved1)
            throw new InvalidOperationException($"Bảng tính {caNo} chưa được Đại lý xác nhận bước 1. Vui lòng chờ Đại lý đối soát.");

        if (sheet.HTCSignStatus != FnExpSignStatus.Pending)
            throw new InvalidOperationException($"Bảng tính {caNo} đã được HTC duyệt bước 1 trước đó (Trạng thái hiện tại: {sheet.HTCSignStatus}).");

        sheet.HTCSignStatus = FnExpSignStatus.Approved1;
        sheet.HTCAppr1At = DateTime.Now;
        sheet.HTCAppr1By = dto?.ApprovedBy ?? "HTC_FIN_SPECIALIST";
        sheet.LogLUDateTime = DateTime.Now;
        sheet.LogLUBy = sheet.HTCAppr1By;

        await _db.SaveChangesAsync();

        return new
        {
            success = true,
            caNo = sheet.CaNo,
            dlrSignStatus = sheet.DlrSignStatus.ToString(),
            htcSignStatus = sheet.HTCSignStatus.ToString(),
            status = sheet.Status.ToString(),
            sheet.HTCAppr1At,
            sheet.HTCAppr1By,
            message = "HTC đã thẩm duyệt bước 1 thành công."
        };
    }

    public async Task<object?> ApproveStep2HqAsync(string caNo, ApproveStep2HqDto? dto = null)
    {
        var sheet = await _db.FnExpCalcSheets
            .Include(x => x.Details)
            .FirstOrDefaultAsync(x => x.OrgId == _tenant.OrgId && x.CaNo == caNo.Trim());

        if (sheet == null) return null;

        if (sheet.Status == FnExpStatus.Cancelled)
            throw new InvalidOperationException($"Bảng tính {caNo} đã bị hủy, không thể duyệt.");

        if (sheet.HTCSignStatus != FnExpSignStatus.Approved1)
            throw new InvalidOperationException($"Bảng tính {caNo} chưa được thẩm duyệt bước 1 bởi HTC.");

        var now = DateTime.Now;
        sheet.HTCSignStatus = FnExpSignStatus.Approved2;
        sheet.HTCAppr2At = now;
        sheet.HTCAppr2By = dto?.ApprovedBy ?? "HTC_FINANCE_DIRECTOR";

        var safeCaNo = sheet.CaNo.Replace('/', '_');
        sheet.FilePathFnExp = dto?.FilePathCPTC ?? $"https://ftest.ecore.vn/{now:yyyyMMdd}/CPTC_{safeCaNo}.xlsx";
        sheet.FilePathPmtDC = dto?.FilePathCKTT ?? $"https://ftest.ecore.vn/{now:yyyyMMdd}/CKTT_{safeCaNo}.xlsx";

        // Nếu cả Đại lý cũng đã Approved2, chốt trạng thái tổng thể Approved
        if (sheet.DlrSignStatus == FnExpSignStatus.Approved2)
        {
            sheet.Status = FnExpStatus.Approved;
            foreach (var d in sheet.Details)
                d.Status = FnExpStatus.Approved;
        }

        sheet.LogLUDateTime = now;
        sheet.LogLUBy = sheet.HTCAppr2By;

        await _db.SaveChangesAsync();

        return new
        {
            success = true,
            caNo = sheet.CaNo,
            dlrSignStatus = sheet.DlrSignStatus.ToString(),
            htcSignStatus = sheet.HTCSignStatus.ToString(),
            status = sheet.Status.ToString(),
            sheet.HTCAppr2At,
            sheet.HTCAppr2By,
            sheet.FilePathFnExp,
            sheet.FilePathPmtDC,
            message = "Lãnh đạo HTC đã phê duyệt cấp 2 và xuất hóa đơn/chứng từ CPTC & CKTT."
        };
    }

    public async Task<object?> ApproveStep2DealerAsync(string caNo, ApproveStep2DealerDto? dto = null)
    {
        var sheet = await _db.FnExpCalcSheets
            .Include(x => x.Details)
            .FirstOrDefaultAsync(x => x.OrgId == _tenant.OrgId && x.CaNo == caNo.Trim());

        if (sheet == null) return null;

        if (sheet.Status == FnExpStatus.Cancelled)
            throw new InvalidOperationException($"Bảng tính {caNo} đã bị hủy, không thể duyệt.");

        if (sheet.DlrSignStatus != FnExpSignStatus.Approved1)
            throw new InvalidOperationException($"Đại lý chưa xác nhận bước 1.");

        if (sheet.HTCSignStatus != FnExpSignStatus.Approved2)
            throw new InvalidOperationException($"Lãnh đạo HTC chưa phê duyệt cấp 2 bảng tính {caNo}.");

        var now = DateTime.Now;
        sheet.DlrSignStatus = FnExpSignStatus.Approved2;
        sheet.DlrAppr2At = now;
        sheet.DlrAppr2By = dto?.ApprovedBy ?? $"{sheet.DealerCode}_DIRECTOR";

        // Cả 2 bên đã duyệt cấp 2 -> Chốt trạng thái Approved
        sheet.Status = FnExpStatus.Approved;
        foreach (var d in sheet.Details)
            d.Status = FnExpStatus.Approved;

        sheet.LogLUDateTime = now;
        sheet.LogLUBy = sheet.DlrAppr2By;

        await _db.SaveChangesAsync();

        return new
        {
            success = true,
            caNo = sheet.CaNo,
            dlrSignStatus = sheet.DlrSignStatus.ToString(),
            htcSignStatus = sheet.HTCSignStatus.ToString(),
            status = sheet.Status.ToString(),
            sheet.DlrAppr2At,
            sheet.DlrAppr2By,
            message = "Lãnh đạo Đại lý đã ký số phê duyệt cấp 2. Bảng tính CPTC & CKTT chính thức hoàn tất."
        };
    }

    public async Task<object?> CancelAsync(string caNo, CancelFnExpCalcSheetDto dto)
    {
        var sheet = await _db.FnExpCalcSheets
            .Include(x => x.Details)
            .FirstOrDefaultAsync(x => x.OrgId == _tenant.OrgId && x.CaNo == caNo.Trim());

        if (sheet == null) return null;

        if (sheet.Status == FnExpStatus.Cancelled)
            throw new InvalidOperationException($"Bảng tính {caNo} đã ở trạng thái hủy.");

        if (sheet.Status == FnExpStatus.Approved)
            throw new InvalidOperationException($"Bảng tính {caNo} đã hoàn tất duyệt 2 bên, không thể hủy.");

        sheet.Status = FnExpStatus.Cancelled;
        sheet.DlrSignStatus = FnExpSignStatus.Cancelled;
        sheet.HTCSignStatus = FnExpSignStatus.Cancelled;
        sheet.CancelReason = dto.Reason;
        sheet.CancelledBy = dto.CancelledBy ?? "HTC_ADMIN";
        sheet.CancelledAt = DateTime.Now;
        sheet.LogLUDateTime = DateTime.Now;
        sheet.LogLUBy = sheet.CancelledBy;

        foreach (var d in sheet.Details)
            d.Status = FnExpStatus.Cancelled;

        await _db.SaveChangesAsync();

        return new
        {
            success = true,
            caNo = sheet.CaNo,
            status = sheet.Status.ToString(),
            sheet.CancelReason,
            sheet.CancelledBy,
            sheet.CancelledAt,
            message = $"Đã hủy bảng tính CPTC & CKTT {caNo}."
        };
    }

    public async Task<bool> DeleteDraftAsync(string caNo)
    {
        var sheet = await _db.FnExpCalcSheets
            .Include(x => x.Details)
            .FirstOrDefaultAsync(x => x.OrgId == _tenant.OrgId && x.CaNo == caNo.Trim());

        if (sheet == null) return false;

        if (sheet.DlrSignStatus != FnExpSignStatus.Pending || sheet.HTCSignStatus != FnExpSignStatus.Pending)
            throw new InvalidOperationException($"Bảng tính {caNo} đã vào quy trình ký duyệt, không thể xóa nháp.");

        _db.FnExpCalcSheets.Remove(sheet);
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<object?> PrintCptcAsync(string caNo)
    {
        var sheet = await _db.FnExpCalcSheets
            .Include(x => x.Details)
            .FirstOrDefaultAsync(x => x.OrgId == _tenant.OrgId && x.CaNo == caNo.Trim());

        if (sheet == null) return null;

        return new
        {
            success = true,
            documentType = "CPTC",
            documentTitle = "BẢNG KÊ QUYẾT TOÁN CHI PHÍ TÀI CHÍNH (CPTC) ĐẠI LÝ",
            sheet.CaNo,
            sheet.DealerCode,
            sheet.DealerName,
            TermPeriod = $"{sheet.TermFrom:dd/MM/yyyy} - {sheet.TermTo:dd/MM/yyyy}",
            InterestRateAnnual = $"{sheet.FnExpPercent}%/năm",
            sheet.TotalCars,
            sheet.TotalFnDepositAmount,
            sheet.TotalFnGrtAmount,
            GrandTotalCPTC = sheet.TotalFnAmount,
            sheet.FilePathFnExp,
            Signatures = new
            {
                Preparer = sheet.CreatedBy,
                DealerSign = sheet.DlrAppr2By ?? sheet.DlrAppr1By ?? "Chưa ký",
                HTCSign = sheet.HTCAppr2By ?? sheet.HTCAppr1By ?? "Chưa duyệt"
            },
            CarLines = sheet.Details.Select(d => new
            {
                d.CarId,
                d.Vin,
                d.ModelName,
                d.UnitPriceActual,
                SodDepositDutyEndDate = d.SodDepositDutyEndDate?.ToString("dd/MM/yyyy"),
                DateEnd = d.DateEnd?.ToString("dd/MM/yyyy"),
                TotalCompletedDate = d.TotalCompletedDate?.ToString("dd/MM/yyyy"),
                d.FnDepositCountDate,
                d.FnDepositAmount,
                d.FnGrtCountDate,
                d.FnGrtAmount,
                TotalCPTC = d.FnTotalAmount
            }).ToList()
        };
    }

    public async Task<object?> PrintCkttAsync(string caNo)
    {
        var sheet = await _db.FnExpCalcSheets
            .Include(x => x.Details)
            .FirstOrDefaultAsync(x => x.OrgId == _tenant.OrgId && x.CaNo == caNo.Trim());

        if (sheet == null) return null;

        return new
        {
            success = true,
            documentType = "CKTT",
            documentTitle = "BẢNG KÊ QUYẾT TOÁN CHIẾT KHẤU THANH TOÁN SỚM (CKTT) TCG",
            sheet.CaNo,
            sheet.DealerCode,
            sheet.DealerName,
            TermPeriod = $"{sheet.TermFrom:dd/MM/yyyy} - {sheet.TermTo:dd/MM/yyyy}",
            DiscountRate = $"{sheet.PmtDsTCGPercent}%",
            sheet.TotalCars,
            GrandTotalCKTT = sheet.TotalPDAmount,
            NetSettlementAmount = sheet.NetSettlementAmount,
            sheet.FilePathPmtDC,
            Signatures = new
            {
                Preparer = sheet.CreatedBy,
                DealerSign = sheet.DlrAppr2By ?? sheet.DlrAppr1By ?? "Chưa ký",
                HTCSign = sheet.HTCAppr2By ?? sheet.HTCAppr1By ?? "Chưa duyệt"
            },
            CarLines = sheet.Details.Select(d => new
            {
                d.CarId,
                d.Vin,
                d.ModelName,
                d.UnitPriceActual,
                GuaranteeEndDate = d.DateEnd?.ToString("dd/MM/yyyy"),
                PaymentCompletedDate = d.TotalCompletedDate?.ToString("dd/MM/yyyy"),
                EarlyDays = d.PDCountDate,
                DiscountAmount = d.PDAmount
            }).ToList()
        };
    }

    public async Task<object> StatsAsync(string? dealerCode = null)
    {
        var q = _db.FnExpCalcSheets.Where(x => x.OrgId == _tenant.OrgId);

        if (!string.IsNullOrWhiteSpace(dealerCode))
            q = q.Where(x => x.DealerCode == dealerCode.Trim().ToUpperInvariant());

        var totalSheets = await q.CountAsync();
        var pendingCount = await q.CountAsync(x => x.Status == FnExpStatus.NotSign);
        var approvedCount = await q.CountAsync(x => x.Status == FnExpStatus.Approved);
        var cancelledCount = await q.CountAsync(x => x.Status == FnExpStatus.Cancelled);

        var totalCars = await q.Where(x => x.Status != FnExpStatus.Cancelled).SumAsync(x => x.TotalCars);
        var totalFnAmount = await q.Where(x => x.Status != FnExpStatus.Cancelled).SumAsync(x => x.TotalFnAmount);
        var totalPDAmount = await q.Where(x => x.Status != FnExpStatus.Cancelled).SumAsync(x => x.TotalPDAmount);
        var netSettlement = totalPDAmount - totalFnAmount;

        var byDealer = await q
            .Where(x => x.Status != FnExpStatus.Cancelled)
            .GroupBy(x => new { x.DealerCode, x.DealerName })
            .Select(g => new
            {
                g.Key.DealerCode,
                g.Key.DealerName,
                SheetsCount = g.Count(),
                CarsCount = g.Sum(x => x.TotalCars),
                TotalFnAmount = g.Sum(x => x.TotalFnAmount),
                TotalPDAmount = g.Sum(x => x.TotalPDAmount),
                NetSettlementAmount = g.Sum(x => x.NetSettlementAmount)
            })
            .ToListAsync();

        return new
        {
            success = true,
            dealerCode,
            totalSheets,
            pendingCount,
            approvedCount,
            cancelledCount,
            totalCars,
            totalFnAmount,
            totalPDAmount,
            netSettlement,
            byDealer
        };
    }
}
