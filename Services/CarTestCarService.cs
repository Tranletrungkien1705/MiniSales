using Microsoft.EntityFrameworkCore;
using MiniSales.Data;
using MiniSales.Models;

namespace MiniSales.Services;

public record CarTestCarItemInputDto(
    string CarId,
    string Vin,
    string Model,
    string? ModelCode = null,
    string? SpecCode = null,
    string? SpecDescription = null,
    string? ColorCode = null,
    string? ColorName = null,
    string? EngineNo = null,
    string? SOCode = null,
    decimal UnitPriceActual = 0,
    DateTime? EffDateStart = null,
    DateTime? EffDateEnd = null,
    string? Remark = null
);

public record CreateCarTestCarDto(
    string? TestCarCode,
    string DealerCode,
    string? DealerName,
    string? Remark,
    string? CreatedBy,
    DateTime? EffDateStart = null,
    DateTime? EffDateEnd = null,
    List<CarTestCarItemInputDto>? Items = null,
    List<string>? Vins = null
);

public record UpdateCarTestCarDto(
    string? DealerName,
    string? Remark,
    string? UpdatedBy,
    DateTime? EffDateStart = null,
    DateTime? EffDateEnd = null
);

public record Approve1CarTestCarDto(
    string? ApprovedBy,
    string? Remark
);

public record Approve2CarTestCarDto(
    string? FinishedBy,
    string? Remark
);

public record RejectCarTestCarDto(
    string RejectReason,
    string? RejectedBy
);

public record CancelCarTestCarDto(
    string CancelReason,
    string? CancelledBy
);

public record AddTestCarItemDto(
    string CarId,
    string Vin,
    string Model,
    string? ModelCode = null,
    string? SpecCode = null,
    string? SpecDescription = null,
    string? ColorCode = null,
    string? ColorName = null,
    string? EngineNo = null,
    string? SOCode = null,
    decimal UnitPriceActual = 0,
    DateTime? EffDateStart = null,
    DateTime? EffDateEnd = null,
    string? Remark = null
);

public record CarTestCarStatsDto(
    int TotalProposals,
    int PendingCount,
    int ApprovedCount,
    int FinishedCount,
    int RejectedCount,
    int CancelledCount,
    int TotalCars,
    decimal TotalAmount,
    int DistinctDealersCount,
    int ActiveTestCarsCount
);

public record EligibleCarForTestDriveDto(
    string CarId,
    string Vin,
    string Model,
    string? ModelCode,
    string? SpecCode,
    string? SpecDescription,
    string? ColorCode,
    string? ColorName,
    string? EngineNo,
    string? SOCode,
    decimal UnitPriceActual,
    string DealerCode,
    string DealerName
);

public interface ICarTestCarService
{
    Task<CarTestCar> CreateAsync(CreateCarTestCarDto dto);
    Task<List<CarTestCar>> ListAsync(
        string? status = null,
        string? dealerCode = null,
        string? model = null,
        string? vin = null,
        string? testCarCode = null,
        DateTime? dateFrom = null,
        DateTime? dateTo = null
    );
    Task<CarTestCar?> DetailAsync(string testCarCode);
    Task<CarTestCar?> UpdateAsync(string testCarCode, UpdateCarTestCarDto dto);
    Task<bool> DeleteDraftAsync(string testCarCode);
    Task<CarTestCar?> Approve1HqAsync(string testCarCode, Approve1CarTestCarDto? dto);
    Task<CarTestCar?> Approve2HqAsync(string testCarCode, Approve2CarTestCarDto? dto);
    Task<CarTestCar?> RejectHqAsync(string testCarCode, RejectCarTestCarDto dto);
    Task<CarTestCar?> CancelAsync(string testCarCode, CancelCarTestCarDto dto);
    Task<CarTestCar?> AddCarsAsync(string testCarCode, List<AddTestCarItemDto> items);
    Task<CarTestCar?> RemoveCarAsync(string testCarCode, string vin);
    Task<List<EligibleCarForTestDriveDto>> GetEligibleCarsAsync(string? dealerCode = null);
    Task<CarTestCarStatsDto> StatsAsync();
}

public class CarTestCarService(AppDbContext db, ITenantContext tenant) : ICarTestCarService
{
    public async Task<CarTestCar> CreateAsync(CreateCarTestCarDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.DealerCode))
            throw new InvalidOperationException("Mã đại lý (DealerCode) không được để trống.");

        var dealerCode = dto.DealerCode.Trim();
        var dealerName = !string.IsNullOrWhiteSpace(dto.DealerName)
            ? dto.DealerName.Trim()
            : ResolveDealerName(dealerCode);

        // Sinh số đề nghị nếu chưa có: {yyMM}TC{seq:D4} (DMS40 SequenceType: TESTCAR)
        var testCarCode = dto.TestCarCode?.Trim();
        if (string.IsNullOrWhiteSpace(testCarCode))
        {
            var prefix = $"{DateTime.Today:yyMM}TC";
            var maxSeq = await db.TestCars
                .Where(x => x.OrgId == tenant.OrgId && x.TestCarCode.StartsWith(prefix))
                .Select(x => x.TestCarCode)
                .ToListAsync();

            var nextNum = 1;
            foreach (var code in maxSeq)
            {
                if (code.Length >= prefix.Length + 4 && int.TryParse(code.Substring(prefix.Length, 4), out var n))
                {
                    if (n >= nextNum) nextNum = n + 1;
                }
            }
            testCarCode = $"{prefix}{nextNum:D4}";
        }

        var exists = await db.TestCars.AnyAsync(x => x.OrgId == tenant.OrgId && x.TestCarCode == testCarCode);
        if (exists)
            throw new InvalidOperationException($"Số đề nghị xe lái thử {testCarCode} đã tồn tại trong hệ thống.");

        var defaultStart = dto.EffDateStart ?? DateTime.Today;
        var defaultEnd = dto.EffDateEnd ?? defaultStart.AddMonths(6);

        var details = new List<CarTestCarDetail>();
        var seenVins = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        if (dto.Items != null && dto.Items.Count > 0)
        {
            foreach (var it in dto.Items)
            {
                var vin = it.Vin?.Trim().ToUpperInvariant() ?? "";
                if (string.IsNullOrWhiteSpace(vin) || vin.Length != 17)
                    throw new InvalidOperationException($"Số VIN '{it.Vin}' không hợp lệ (yêu cầu đúng 17 ký tự chuẩn VIN).");

                if (!seenVins.Add(vin))
                    throw new InvalidOperationException($"Số khung VIN {vin} bị trùng lặp trong danh sách đăng ký.");

                var carId = !string.IsNullOrWhiteSpace(it.CarId) ? it.CarId.Trim() : $"CAR-{vin.Substring(vin.Length - 6)}";
                var start = it.EffDateStart ?? defaultStart;
                var end = it.EffDateEnd ?? defaultEnd;
                if (start > end)
                    throw new InvalidOperationException($"Thời hạn hiệu lực từ ngày {start:dd/MM/yyyy} phải nhỏ hơn hoặc bằng đến ngày {end:dd/MM/yyyy} cho xe {vin}.");

                details.Add(new CarTestCarDetail
                {
                    TestCarCode = testCarCode,
                    CarId = carId,
                    Vin = vin,
                    Model = it.Model?.Trim() ?? "Chưa xác định",
                    ModelCode = it.ModelCode?.Trim(),
                    SpecCode = it.SpecCode?.Trim(),
                    SpecDescription = it.SpecDescription?.Trim(),
                    ColorCode = it.ColorCode?.Trim(),
                    ColorName = it.ColorName?.Trim(),
                    EngineNo = it.EngineNo?.Trim(),
                    SOCode = it.SOCode?.Trim(),
                    UnitPriceActual = it.UnitPriceActual > 0 ? it.UnitPriceActual : 0m,
                    EffDateStart = start,
                    EffDateEnd = end,
                    FlagTestCar = false,
                    Status = TestCarDetailStatus.Pending,
                    Remark = it.Remark?.Trim()
                });
            }
        }
        else if (dto.Vins != null && dto.Vins.Count > 0)
        {
            var eligible = await GetEligibleCarsAsync(dealerCode);
            var eligibleMap = eligible.ToDictionary(x => x.Vin, StringComparer.OrdinalIgnoreCase);

            foreach (var v in dto.Vins)
            {
                var vin = v.Trim().ToUpperInvariant();
                if (string.IsNullOrWhiteSpace(vin) || vin.Length != 17)
                    throw new InvalidOperationException($"Số khung VIN '{v}' không đúng định dạng 17 ký tự.");

                if (!seenVins.Add(vin))
                    throw new InvalidOperationException($"Số khung VIN {vin} bị trùng lặp trong danh sách đăng ký.");

                if (eligibleMap.TryGetValue(vin, out var ec))
                {
                    details.Add(new CarTestCarDetail
                    {
                        TestCarCode = testCarCode,
                        CarId = ec.CarId,
                        Vin = vin,
                        Model = ec.Model,
                        ModelCode = ec.ModelCode,
                        SpecCode = ec.SpecCode,
                        SpecDescription = ec.SpecDescription,
                        ColorCode = ec.ColorCode,
                        ColorName = ec.ColorName,
                        EngineNo = ec.EngineNo,
                        SOCode = ec.SOCode,
                        UnitPriceActual = ec.UnitPriceActual,
                        EffDateStart = defaultStart,
                        EffDateEnd = defaultEnd,
                        FlagTestCar = false,
                        Status = TestCarDetailStatus.Pending,
                        Remark = "Đăng ký từ danh sách xe đủ điều kiện đại lý"
                    });
                }
                else
                {
                    details.Add(new CarTestCarDetail
                    {
                        TestCarCode = testCarCode,
                        CarId = $"CAR-{vin.Substring(vin.Length - 6)}",
                        Vin = vin,
                        Model = "Hyundai",
                        UnitPriceActual = 0m,
                        EffDateStart = defaultStart,
                        EffDateEnd = defaultEnd,
                        FlagTestCar = false,
                        Status = TestCarDetailStatus.Pending,
                        Remark = "Tự nhập theo VIN"
                    });
                }
            }
        }

        if (details.Count == 0)
            throw new InvalidOperationException("Đề nghị đăng ký xe lái thử cần ít nhất 1 xe trong danh sách.");

        var testCar = new CarTestCar
        {
            OrgId = tenant.OrgId,
            TestCarCode = testCarCode,
            DealerCode = dealerCode,
            DealerName = dealerName,
            TestCarStatus = TestCarStatus.Pending,
            TotalCars = details.Count,
            TotalAmount = details.Sum(x => x.UnitPriceActual),
            Remark = dto.Remark?.Trim(),
            CreatedBy = dto.CreatedBy?.Trim() ?? "DEALER_USER",
            CreatedDate = DateTime.Now,
            Details = details
        };

        db.TestCars.Add(testCar);
        await db.SaveChangesAsync();
        return testCar;
    }

    public async Task<List<CarTestCar>> ListAsync(
        string? status = null,
        string? dealerCode = null,
        string? model = null,
        string? vin = null,
        string? testCarCode = null,
        DateTime? dateFrom = null,
        DateTime? dateTo = null)
    {
        var q = db.TestCars
            .Include(x => x.Details)
            .Where(x => x.OrgId == tenant.OrgId)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(status))
        {
            var st = status.Trim().ToUpperInvariant();
            if (st is "P" or "PENDING") q = q.Where(x => x.TestCarStatus == TestCarStatus.Pending);
            else if (st is "A" or "APPROVED") q = q.Where(x => x.TestCarStatus == TestCarStatus.Approved);
            else if (st is "F" or "FINISHED") q = q.Where(x => x.TestCarStatus == TestCarStatus.Finished);
            else if (st is "R" or "REJECT" or "REJECTED") q = q.Where(x => x.TestCarStatus == TestCarStatus.Rejected);
            else if (st is "C" or "CANCEL" or "CANCELLED") q = q.Where(x => x.TestCarStatus == TestCarStatus.Cancelled);
        }

        if (!string.IsNullOrWhiteSpace(dealerCode))
            q = q.Where(x => x.DealerCode == dealerCode.Trim());

        if (!string.IsNullOrWhiteSpace(testCarCode))
            q = q.Where(x => x.TestCarCode.Contains(testCarCode.Trim()));

        if (!string.IsNullOrWhiteSpace(model))
            q = q.Where(x => x.Details.Any(d => d.Model.Contains(model.Trim())));

        if (!string.IsNullOrWhiteSpace(vin))
            q = q.Where(x => x.Details.Any(d => d.Vin.Contains(vin.Trim().ToUpperInvariant())));

        if (dateFrom.HasValue)
            q = q.Where(x => x.CreatedDate >= dateFrom.Value.Date);

        if (dateTo.HasValue)
        {
            var nextDay = dateTo.Value.Date.AddDays(1);
            q = q.Where(x => x.CreatedDate < nextDay);
        }

        return await q.OrderByDescending(x => x.CreatedDate).ToListAsync();
    }

    public async Task<CarTestCar?> DetailAsync(string testCarCode)
    {
        if (string.IsNullOrWhiteSpace(testCarCode)) return null;
        var code = testCarCode.Trim();
        return await db.TestCars
            .Include(x => x.Details)
            .FirstOrDefaultAsync(x => x.OrgId == tenant.OrgId && x.TestCarCode == code);
    }

    public async Task<CarTestCar?> UpdateAsync(string testCarCode, UpdateCarTestCarDto dto)
    {
        var item = await DetailAsync(testCarCode);
        if (item == null) return null;

        if (item.TestCarStatus != TestCarStatus.Pending)
            throw new InvalidOperationException($"Không thể chỉnh sửa đề nghị ở trạng thái {item.TestCarStatus}. Chỉ cho phép sửa khi còn ở trạng thái Pending.");

        if (!string.IsNullOrWhiteSpace(dto.DealerName))
            item.DealerName = dto.DealerName.Trim();

        if (dto.Remark != null)
            item.Remark = dto.Remark.Trim();

        if (dto.EffDateStart.HasValue || dto.EffDateEnd.HasValue)
        {
            foreach (var d in item.Details)
            {
                if (dto.EffDateStart.HasValue) d.EffDateStart = dto.EffDateStart.Value;
                if (dto.EffDateEnd.HasValue) d.EffDateEnd = dto.EffDateEnd.Value;
                if (d.EffDateStart > d.EffDateEnd)
                    throw new InvalidOperationException($"Thời gian từ ngày {d.EffDateStart:dd/MM/yyyy} không được lớn hơn đến ngày {d.EffDateEnd:dd/MM/yyyy}.");
            }
        }

        item.LUDateTime = DateTime.Now;
        item.LUBy = dto.UpdatedBy?.Trim() ?? "DEALER_USER";
        await db.SaveChangesAsync();
        return item;
    }

    public async Task<bool> DeleteDraftAsync(string testCarCode)
    {
        var item = await DetailAsync(testCarCode);
        if (item == null) return false;

        if (item.TestCarStatus != TestCarStatus.Pending)
            throw new InvalidOperationException($"Không thể xóa đề nghị ở trạng thái {item.TestCarStatus}. Chỉ cho phép xóa khi còn ở trạng thái Pending.");

        db.TestCars.Remove(item);
        await db.SaveChangesAsync();
        return true;
    }

    public async Task<CarTestCar?> Approve1HqAsync(string testCarCode, Approve1CarTestCarDto? dto)
    {
        var item = await DetailAsync(testCarCode);
        if (item == null) return null;

        if (item.TestCarStatus != TestCarStatus.Pending)
            throw new InvalidOperationException($"Chỉ có thể phê duyệt Cấp 1 (ApproveAHQ) khi đề nghị đang ở trạng thái Pending (trạng thái hiện tại: {item.TestCarStatus}).");

        item.TestCarStatus = TestCarStatus.Approved;
        item.ApprovedDate = DateTime.Now;
        item.ApprovedBy = dto?.ApprovedBy?.Trim() ?? "NPP_CHUYENVIEN_HQ";

        if (!string.IsNullOrWhiteSpace(dto?.Remark))
            item.Remark = string.IsNullOrWhiteSpace(item.Remark) ? dto.Remark.Trim() : $"{item.Remark}; {dto.Remark.Trim()}";

        foreach (var d in item.Details.Where(x => x.Status == TestCarDetailStatus.Pending))
        {
            d.Status = TestCarDetailStatus.Approved;
        }

        item.LUDateTime = DateTime.Now;
        item.LUBy = item.ApprovedBy;
        await db.SaveChangesAsync();
        return item;
    }

    public async Task<CarTestCar?> Approve2HqAsync(string testCarCode, Approve2CarTestCarDto? dto)
    {
        var item = await DetailAsync(testCarCode);
        if (item == null) return null;

        if (item.TestCarStatus != TestCarStatus.Approved && item.TestCarStatus != TestCarStatus.Pending)
            throw new InvalidOperationException($"Chỉ có thể phê duyệt Cấp 2 hoàn tất (ApproveFHQ) khi đề nghị đang ở trạng thái Approved hoặc Pending (trạng thái hiện tại: {item.TestCarStatus}).");

        item.TestCarStatus = TestCarStatus.Finished;
        item.FinishedDate = DateTime.Now;
        item.FinishedBy = dto?.FinishedBy?.Trim() ?? "NPP_LANHDAO_HQ";

        if (!item.ApprovedDate.HasValue)
        {
            item.ApprovedDate = DateTime.Now;
            item.ApprovedBy = item.FinishedBy;
        }

        if (!string.IsNullOrWhiteSpace(dto?.Remark))
            item.Remark = string.IsNullOrWhiteSpace(item.Remark) ? dto.Remark.Trim() : $"{item.Remark}; {dto.Remark.Trim()}";

        // Tự động kích hoạt cờ xe lái thử FlagTestCar = true cho tất cả xe
        foreach (var d in item.Details.Where(x => x.Status is TestCarDetailStatus.Pending or TestCarDetailStatus.Approved))
        {
            d.Status = TestCarDetailStatus.Finished;
            d.FlagTestCar = true;
        }

        item.LUDateTime = DateTime.Now;
        item.LUBy = item.FinishedBy;
        await db.SaveChangesAsync();
        return item;
    }

    public async Task<CarTestCar?> RejectHqAsync(string testCarCode, RejectCarTestCarDto dto)
    {
        var item = await DetailAsync(testCarCode);
        if (item == null) return null;

        if (item.TestCarStatus == TestCarStatus.Finished)
            throw new InvalidOperationException("Không thể từ chối đề nghị đã phê duyệt hoàn tất đưa vào sử dụng.");

        if (item.TestCarStatus == TestCarStatus.Cancelled)
            throw new InvalidOperationException("Không thể từ chối đề nghị đã bị hủy.");

        if (string.IsNullOrWhiteSpace(dto.RejectReason))
            throw new InvalidOperationException("Vui lòng nhập lý do từ chối phê duyệt xe lái thử.");

        item.TestCarStatus = TestCarStatus.Rejected;
        item.RejectReason = dto.RejectReason.Trim();
        item.RejectDate = DateTime.Now;
        item.RejectBy = dto.RejectedBy?.Trim() ?? "NPP_THAMDANG_HQ";

        foreach (var d in item.Details)
        {
            if (d.Status != TestCarDetailStatus.Cancelled)
            {
                d.Status = TestCarDetailStatus.Rejected;
                d.FlagTestCar = false;
            }
        }

        item.LUDateTime = DateTime.Now;
        item.LUBy = item.RejectBy;
        await db.SaveChangesAsync();
        return item;
    }

    public async Task<CarTestCar?> CancelAsync(string testCarCode, CancelCarTestCarDto dto)
    {
        var item = await DetailAsync(testCarCode);
        if (item == null) return null;

        if (item.TestCarStatus == TestCarStatus.Cancelled)
            throw new InvalidOperationException("Đề nghị xe lái thử đã ở trạng thái hủy.");

        if (string.IsNullOrWhiteSpace(dto.CancelReason))
            throw new InvalidOperationException("Vui lòng nhập lý do hủy đề nghị xe lái thử.");

        item.TestCarStatus = TestCarStatus.Cancelled;
        item.CancelReason = dto.CancelReason.Trim();
        item.CancelledDate = DateTime.Now;
        item.CancelledBy = dto.CancelledBy?.Trim() ?? "DEALER_USER";

        foreach (var d in item.Details)
        {
            d.Status = TestCarDetailStatus.Cancelled;
            d.FlagTestCar = false;
        }

        item.LUDateTime = DateTime.Now;
        item.LUBy = item.CancelledBy;
        await db.SaveChangesAsync();
        return item;
    }

    public async Task<CarTestCar?> AddCarsAsync(string testCarCode, List<AddTestCarItemDto> items)
    {
        var parent = await DetailAsync(testCarCode);
        if (parent == null) return null;

        if (parent.TestCarStatus != TestCarStatus.Pending)
            throw new InvalidOperationException($"Chỉ có thể thêm xe vào đề nghị khi đang ở trạng thái Pending (trạng thái hiện tại: {parent.TestCarStatus}).");

        if (items == null || items.Count == 0)
            throw new InvalidOperationException("Danh sách xe cần thêm không được rỗng.");

        var existingVins = new HashSet<string>(parent.Details.Select(x => x.Vin), StringComparer.OrdinalIgnoreCase);

        foreach (var it in items)
        {
            var vin = it.Vin?.Trim().ToUpperInvariant() ?? "";
            if (string.IsNullOrWhiteSpace(vin) || vin.Length != 17)
                throw new InvalidOperationException($"Số khung VIN '{it.Vin}' không đúng định dạng 17 ký tự.");

            if (!existingVins.Add(vin))
                throw new InvalidOperationException($"Số khung VIN {vin} đã tồn tại trong đề nghị xe lái thử {testCarCode}.");

            var carId = !string.IsNullOrWhiteSpace(it.CarId) ? it.CarId.Trim() : $"CAR-{vin.Substring(vin.Length - 6)}";
            var start = it.EffDateStart ?? DateTime.Today;
            var end = it.EffDateEnd ?? start.AddMonths(6);

            parent.Details.Add(new CarTestCarDetail
            {
                TestCarCode = parent.TestCarCode,
                CarId = carId,
                Vin = vin,
                Model = it.Model?.Trim() ?? "Chưa xác định",
                ModelCode = it.ModelCode?.Trim(),
                SpecCode = it.SpecCode?.Trim(),
                SpecDescription = it.SpecDescription?.Trim(),
                ColorCode = it.ColorCode?.Trim(),
                ColorName = it.ColorName?.Trim(),
                EngineNo = it.EngineNo?.Trim(),
                SOCode = it.SOCode?.Trim(),
                UnitPriceActual = it.UnitPriceActual > 0 ? it.UnitPriceActual : 0m,
                EffDateStart = start,
                EffDateEnd = end,
                FlagTestCar = false,
                Status = TestCarDetailStatus.Pending,
                Remark = it.Remark?.Trim()
            });
        }

        parent.TotalCars = parent.Details.Count(x => x.Status != TestCarDetailStatus.Cancelled);
        parent.TotalAmount = parent.Details.Where(x => x.Status != TestCarDetailStatus.Cancelled).Sum(x => x.UnitPriceActual);
        parent.LUDateTime = DateTime.Now;
        parent.LUBy = "DEALER_USER";

        await db.SaveChangesAsync();
        return parent;
    }

    public async Task<CarTestCar?> RemoveCarAsync(string testCarCode, string vin)
    {
        var parent = await DetailAsync(testCarCode);
        if (parent == null) return null;

        if (parent.TestCarStatus != TestCarStatus.Pending)
            throw new InvalidOperationException($"Chỉ có thể gỡ xe khỏi đề nghị khi đang ở trạng thái Pending (trạng thái hiện tại: {parent.TestCarStatus}).");

        var cleanVin = vin.Trim().ToUpperInvariant();
        var line = parent.Details.FirstOrDefault(x => x.Vin.Equals(cleanVin, StringComparison.OrdinalIgnoreCase));
        if (line == null)
            throw new InvalidOperationException($"Không tìm thấy xe với số VIN {cleanVin} trong đề nghị {testCarCode}.");

        parent.Details.Remove(line);
        db.TestCarDetails.Remove(line);

        parent.TotalCars = parent.Details.Count(x => x.Status != TestCarDetailStatus.Cancelled);
        parent.TotalAmount = parent.Details.Where(x => x.Status != TestCarDetailStatus.Cancelled).Sum(x => x.UnitPriceActual);
        parent.LUDateTime = DateTime.Now;
        parent.LUBy = "DEALER_USER";

        await db.SaveChangesAsync();
        return parent;
    }

    public async Task<List<EligibleCarForTestDriveDto>> GetEligibleCarsAsync(string? dealerCode = null)
    {
        var dCode = dealerCode?.Trim();

        // Danh sách VIN đang là xe lái thử hoặc đang trong đề nghị Pending/Approved/Finished
        var activeTestVins = await db.TestCarDetails
            .Where(x => x.Status != TestCarDetailStatus.Cancelled && x.Status != TestCarDetailStatus.Rejected)
            .Select(x => x.Vin)
            .Distinct()
            .ToListAsync();

        var activeVinSet = new HashSet<string>(activeTestVins, StringComparer.OrdinalIgnoreCase);

        // Lấy danh sách xe từ Hợp đồng mua bán buôn xe đại lý
        var contractCarsQuery = db.DealerContracts
            .Include(x => x.Details)
            .Where(x => x.OrgId == tenant.OrgId && x.Status == DealerContractStatus.Signed);

        if (!string.IsNullOrWhiteSpace(dCode))
            contractCarsQuery = contractCarsQuery.Where(x => x.DealerCode == dCode);

        var contractList = await contractCarsQuery.ToListAsync();

        var result = new List<EligibleCarForTestDriveDto>();
        var addedVins = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var c in contractList)
        {
            foreach (var d in c.Details)
            {
                if (!string.IsNullOrWhiteSpace(d.Vin) && d.Vin.Length == 17)
                {
                    var vinUpper = d.Vin.ToUpperInvariant();
                    if (!activeVinSet.Contains(vinUpper) && addedVins.Add(vinUpper))
                    {
                        result.Add(new EligibleCarForTestDriveDto(
                            CarId: d.CarId,
                            Vin: vinUpper,
                            Model: d.Model,
                            ModelCode: null,
                            SpecCode: d.SpecCode,
                            SpecDescription: null,
                            ColorCode: d.ColorCode,
                            ColorName: null,
                            EngineNo: null,
                            SOCode: d.OrderNo,
                            UnitPriceActual: d.TotalAmount > 0 ? d.TotalAmount : 800000000m,
                            DealerCode: c.DealerCode,
                            DealerName: c.DealerName
                        ));
                    }
                }
            }
        }

        // Lấy thêm từ SalesOrder nếu chưa đủ
        var ordersQuery = db.Orders.Where(x => x.OrgId == tenant.OrgId && !string.IsNullOrWhiteSpace(x.Vin));
        if (!string.IsNullOrWhiteSpace(dCode))
            ordersQuery = ordersQuery.Where(x => x.DealerCode == dCode);

        var orders = await ordersQuery.ToListAsync();
        foreach (var o in orders)
        {
            if (!string.IsNullOrWhiteSpace(o.Vin) && o.Vin.Length == 17)
            {
                var vinUpper = o.Vin.ToUpperInvariant();
                if (!activeVinSet.Contains(vinUpper) && addedVins.Add(vinUpper))
                {
                    result.Add(new EligibleCarForTestDriveDto(
                        CarId: $"CAR-{vinUpper.Substring(vinUpper.Length - 6)}",
                        Vin: vinUpper,
                        Model: o.Model,
                        ModelCode: null,
                        SpecCode: null,
                        SpecDescription: null,
                        ColorCode: null,
                        ColorName: null,
                        EngineNo: null,
                        SOCode: o.Code,
                        UnitPriceActual: o.NetPrice > 0 ? o.NetPrice : 900000000m,
                        DealerCode: o.DealerCode,
                        DealerName: ResolveDealerName(o.DealerCode)
                    ));
                }
            }
        }

        return result;
    }

    public async Task<CarTestCarStatsDto> StatsAsync()
    {
        var list = await db.TestCars
            .Include(x => x.Details)
            .Where(x => x.OrgId == tenant.OrgId)
            .ToListAsync();

        var total = list.Count;
        var pending = list.Count(x => x.TestCarStatus == TestCarStatus.Pending);
        var approved = list.Count(x => x.TestCarStatus == TestCarStatus.Approved);
        var finished = list.Count(x => x.TestCarStatus == TestCarStatus.Finished);
        var rejected = list.Count(x => x.TestCarStatus == TestCarStatus.Rejected);
        var cancelled = list.Count(x => x.TestCarStatus == TestCarStatus.Cancelled);

        var activeCars = list
            .Where(x => x.TestCarStatus == TestCarStatus.Finished)
            .SelectMany(x => x.Details)
            .Count(d => d.FlagTestCar && d.Status == TestCarDetailStatus.Finished);

        var totalCars = list.Where(x => x.TestCarStatus != TestCarStatus.Cancelled).Sum(x => x.TotalCars);
        var totalAmount = list.Where(x => x.TestCarStatus != TestCarStatus.Cancelled).Sum(x => x.TotalAmount);
        var distinctDealers = list.Select(x => x.DealerCode).Distinct().Count();

        return new CarTestCarStatsDto(
            TotalProposals: total,
            PendingCount: pending,
            ApprovedCount: approved,
            FinishedCount: finished,
            RejectedCount: rejected,
            CancelledCount: cancelled,
            TotalCars: totalCars,
            TotalAmount: totalAmount,
            DistinctDealersCount: distinctDealers,
            ActiveTestCarsCount: activeCars
        );
    }

    private static string ResolveDealerName(string dealerCode) => dealerCode switch
    {
        "VN001" => "Hyundai Đông Đô",
        "VN002" => "Hyundai Nam Trung",
        "VN003" => "Hyundai Tây Hồ",
        "VN004" => "Hyundai Sài Gòn",
        "VN005" => "Hyundai Đà Nẵng",
        _ => $"Đại lý {dealerCode}"
    };
}
