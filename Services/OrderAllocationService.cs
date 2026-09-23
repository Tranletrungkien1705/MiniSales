using Microsoft.EntityFrameworkCore;
using MiniSales.Data;
using MiniSales.Models;

namespace MiniSales.Services;

// ===== DTOs =====
public record CreateOrderDemandDto(
    string DealerCode,
    string SpecCode,
    string ModelCode,
    string? ColorCode,
    string? AssemblyStatus,
    int QtyInit,
    string? Remark
);

public record CreateOrderSupplyDto(
    string SpecCode,
    string ModelCode,
    string? ColorCode,
    int QtyInit,
    string? Remark
);

public record CreateOrderAllocationDto(
    string PeriodMonth, // "YYYYMM"
    string? DealerCode,
    string? SalesPolicyCode,
    string? Remark,
    List<CreateOrderDemandDto>? Demands,
    List<CreateOrderSupplyDto>? Supplies
);

public record UpdateOrderAllocationDto(
    string? SalesPolicyCode,
    string? Remark
);

public record RejectOrderAllocationDto(string Reason, string? RejectedBy);
public record CancelOrderAllocationDto(string Reason, string? CancelledBy);

public interface IOrderAllocationService
{
    Task<object> CreateAsync(CreateOrderAllocationDto dto);
    Task<object> ListAsync(string? status, string? periodMonth, string? dealerCode, string? allocationNo);
    Task<object?> DetailAsync(string allocationNo);
    Task<object?> UpdateAsync(string allocationNo, UpdateOrderAllocationDto dto);
    Task<object?> AllocateAsync(string allocationNo);
    Task<object?> FinishAsync(string allocationNo);
    Task<object?> RejectAsync(string allocationNo, RejectOrderAllocationDto dto);
    Task<object?> CancelAsync(string allocationNo, CancelOrderAllocationDto dto);
    Task<object?> DeleteAsync(string allocationNo);
    Task<object> StatsAsync();
}

/// <summary>
/// Phân bổ Nhu cầu / Cung cấp đơn đặt hàng xe ô tô (DMS.Sales DMS40_Ord_SalesOrderRoot_ApprAuto /
/// DMS40_Ord_SalesOrderRoot_ApprDemand / DMS40_Ord_SalesOrderRoot_ApprSupply1|2|3 / DMS40.0.30.Order.cs / 04_DON_HANG_DOANH_SO.md):
/// - Đại lý nhập nhu cầu (Demand) theo Spec/Model/Color trong kỳ tháng; NPP nhập cơ cấu cung (Supply).
/// - Thuật toán phân bổ (Rule 2.2.1.a):
///     + SupplyPercent = QtyRemain(Supply) / TotalQtyRemain(Supply) cùng Spec/Model/Color; Rank theo SupplyPercent giảm dần.
///     + DemandPercent = QtyRemain(Demand của đại lý) / TotalQtyRemain(Demand toàn bộ đại lý) cùng Spec/Model/Color.
///     + QtyDemand = Floor((QtyRemain(Supply) / TotalQtyRemain(Demand)) * QtyRemain(Demand đại lý)) — phân bổ theo tỷ lệ, làm tròn xuống.
///     + Phần dư (do làm tròn) được chia tiếp theo thứ hạng ưu tiên (Rank) cho tới khi hết cung.
/// - Khóa nghiệp vụ = AllocationNo. Chỉ phân bổ khi phiên ở trạng thái Draft.
/// </summary>
public sealed class OrderAllocationService(AppDbContext db, ITenantContext tenant) : IOrderAllocationService
{
    private Guid Org => tenant.OrgId;

    public async Task<object> CreateAsync(CreateOrderAllocationDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.PeriodMonth))
            throw new InvalidOperationException("Kỳ tháng phân bổ (PeriodMonth, định dạng YYYYMM) không được để trống.");

        var periodMonth = dto.PeriodMonth.Trim();
        if (periodMonth.Length != 6 || !periodMonth.All(char.IsDigit))
            throw new InvalidOperationException("Kỳ tháng phân bổ phải có định dạng YYYYMM (6 chữ số), ví dụ 202601.");

        if (dto.Demands is null || dto.Demands.Count == 0)
            throw new InvalidOperationException("Phiên phân bổ phải có ít nhất 1 dòng nhu cầu (Demand) của đại lý.");

        if (dto.Supplies is null || dto.Supplies.Count == 0)
            throw new InvalidOperationException("Phiên phân bổ phải có ít nhất 1 dòng cơ cấu cung (Supply) của NPP.");

        // Kiểm tra trùng lặp khóa dòng nhu cầu (DealerCode + Spec + Model + Color)
        var demandKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var d in dto.Demands)
        {
            if (string.IsNullOrWhiteSpace(d.DealerCode))
                throw new InvalidOperationException("Mã đại lý (DealerCode) trong dòng nhu cầu không được để trống.");
            if (string.IsNullOrWhiteSpace(d.SpecCode) || string.IsNullOrWhiteSpace(d.ModelCode))
                throw new InvalidOperationException("Dòng nhu cầu bắt buộc phải có SpecCode và ModelCode.");
            if (d.QtyInit <= 0)
                throw new InvalidOperationException($"Số lượng nhu cầu (QtyInit) cho {d.SpecCode} phải lớn hơn 0 (nhập: {d.QtyInit}).");

            var key = $"{d.DealerCode.Trim().ToUpperInvariant()}|{d.SpecCode.Trim().ToUpperInvariant()}|{d.ModelCode.Trim().ToUpperInvariant()}|{(d.ColorCode ?? "").Trim().ToUpperInvariant()}";
            if (!demandKeys.Add(key))
                throw new InvalidOperationException($"Trùng lặp dòng nhu cầu cho đại lý {d.DealerCode} / Spec {d.SpecCode} / Model {d.ModelCode} / Color {d.ColorCode}.");
        }

        // Kiểm tra trùng lặp khóa dòng cung (Spec + Model + Color)
        var supplyKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var s in dto.Supplies)
        {
            if (string.IsNullOrWhiteSpace(s.SpecCode) || string.IsNullOrWhiteSpace(s.ModelCode))
                throw new InvalidOperationException("Dòng cơ cấu cung bắt buộc phải có SpecCode và ModelCode.");
            if (s.QtyInit <= 0)
                throw new InvalidOperationException($"Số lượng cung (QtyInit) cho {s.SpecCode} phải lớn hơn 0 (nhập: {s.QtyInit}).");

            var key = $"{s.SpecCode.Trim().ToUpperInvariant()}|{s.ModelCode.Trim().ToUpperInvariant()}|{(s.ColorCode ?? "").Trim().ToUpperInvariant()}";
            if (!supplyKeys.Add(key))
                throw new InvalidOperationException($"Trùng lặp dòng cơ cấu cung cho Spec {s.SpecCode} / Model {s.ModelCode} / Color {s.ColorCode}.");
        }

        // Sinh mã phiên phân bổ nếu chưa truyền vào
        var prefix = $"ALC{DateTime.Today:yyMM}";
        var countThisMonth = await db.OrderAllocationSessions.CountAsync(x => x.OrgId == Org && x.AllocationNo.StartsWith(prefix));
        var allocationNo = $"{prefix}{(countThisMonth + 1):D4}";

        if (await db.OrderAllocationSessions.AnyAsync(x => x.OrgId == Org && x.AllocationNo == allocationNo))
            throw new InvalidOperationException($"Mã phiên phân bổ {allocationNo} đã tồn tại trong hệ thống.");

        var session = new OrderAllocationSession
        {
            OrgId = Org,
            AllocationNo = allocationNo,
            PeriodMonth = periodMonth,
            DealerCode = dto.DealerCode?.Trim().ToUpperInvariant(),
            SalesPolicyCode = dto.SalesPolicyCode?.Trim(),
            Status = OrderAllocationStatus.Draft,
            TotalDemandQty = dto.Demands.Sum(x => x.QtyInit),
            TotalSupplyQty = dto.Supplies.Sum(x => x.QtyInit),
            TotalAllocatedQty = 0,
            TotalUnmetQty = dto.Demands.Sum(x => x.QtyInit),
            Remark = dto.Remark?.Trim(),
            CreatedBy = "HQ_SALES_USER",
            CreatedAt = DateTime.Now,
            Demands = dto.Demands.Select(d => new OrderDemandAllocation
            {
                AllocationNo = allocationNo,
                DealerCode = d.DealerCode.Trim().ToUpperInvariant(),
                PeriodMonth = periodMonth,
                SpecCode = d.SpecCode.Trim().ToUpperInvariant(),
                ModelCode = d.ModelCode.Trim().ToUpperInvariant(),
                ColorCode = d.ColorCode?.Trim().ToUpperInvariant(),
                AssemblyStatus = d.AssemblyStatus?.Trim().ToUpperInvariant(),
                QtyInit = d.QtyInit,
                QtyProcess = 0,
                QtyRemain = d.QtyInit,
                DemandPercent = 0m,
                Rank = 0,
                Status = OrderDemandStatus.Pending,
                Remark = d.Remark?.Trim()
            }).ToList(),
            Supplies = dto.Supplies.Select(s => new OrderSupplyAllocation
            {
                AllocationNo = allocationNo,
                PeriodMonth = periodMonth,
                SpecCode = s.SpecCode.Trim().ToUpperInvariant(),
                ModelCode = s.ModelCode.Trim().ToUpperInvariant(),
                ColorCode = s.ColorCode?.Trim().ToUpperInvariant(),
                QtyInit = s.QtyInit,
                QtyProcess = 0,
                QtyRemain = s.QtyInit,
                SupplyPercent = 0m,
                Rank = 0,
                Status = OrderSupplyStatus.Pending,
                Remark = s.Remark?.Trim()
            }).ToList()
        };

        db.OrderAllocationSessions.Add(session);
        await db.SaveChangesAsync();

        return new
        {
            success = true,
            message = $"Tạo phiên phân bổ nhu cầu/cung {session.AllocationNo} thành công.",
            allocationNo = session.AllocationNo,
            periodMonth = session.PeriodMonth,
            status = session.Status.ToString(),
            totalDemandQty = session.TotalDemandQty,
            totalSupplyQty = session.TotalSupplyQty,
            demandLines = session.Demands.Count,
            supplyLines = session.Supplies.Count
        };
    }

    public async Task<object> ListAsync(string? status, string? periodMonth, string? dealerCode, string? allocationNo)
    {
        var q = db.OrderAllocationSessions.AsNoTracking().Where(x => x.OrgId == Org);

        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<OrderAllocationStatus>(status, true, out var st))
            q = q.Where(x => x.Status == st);
        if (!string.IsNullOrWhiteSpace(periodMonth))
            q = q.Where(x => x.PeriodMonth == periodMonth.Trim());
        if (!string.IsNullOrWhiteSpace(dealerCode))
            q = q.Where(x => x.DealerCode == dealerCode.Trim().ToUpperInvariant());
        if (!string.IsNullOrWhiteSpace(allocationNo))
            q = q.Where(x => x.AllocationNo.Contains(allocationNo.Trim().ToUpperInvariant()));

        var items = await q.OrderByDescending(x => x.CreatedAt)
            .Select(x => new
            {
                x.Id,
                x.AllocationNo,
                x.PeriodMonth,
                x.DealerCode,
                x.SalesPolicyCode,
                Status = x.Status.ToString(),
                x.TotalDemandQty,
                x.TotalSupplyQty,
                x.TotalAllocatedQty,
                x.TotalUnmetQty,
                x.Remark,
                x.RejectReason,
                x.CancelReason,
                x.CreatedAt,
                x.AllocatedAt,
                x.FinishedAt
            })
            .ToListAsync();

        return new { total = items.Count, data = items };
    }

    public async Task<object?> DetailAsync(string allocationNo)
    {
        var session = await db.OrderAllocationSessions.AsNoTracking()
            .Include(x => x.Demands)
            .Include(x => x.Supplies)
            .FirstOrDefaultAsync(x => x.OrgId == Org && x.AllocationNo == allocationNo.Trim().ToUpperInvariant());

        if (session is null) return null;

        return new
        {
            session.Id,
            session.AllocationNo,
            session.PeriodMonth,
            session.DealerCode,
            session.SalesPolicyCode,
            Status = session.Status.ToString(),
            session.TotalDemandQty,
            session.TotalSupplyQty,
            session.TotalAllocatedQty,
            session.TotalUnmetQty,
            session.Remark,
            session.RejectReason,
            session.CancelReason,
            session.CreatedBy,
            session.CreatedAt,
            session.AllocatedBy,
            session.AllocatedAt,
            session.FinishedBy,
            session.FinishedAt,
            session.CancelledBy,
            session.CancelledAt,
            demands = session.Demands.Select(d => new
            {
                d.Id,
                d.DealerCode,
                d.PeriodMonth,
                d.SpecCode,
                d.ModelCode,
                d.ColorCode,
                d.AssemblyStatus,
                d.QtyInit,
                d.QtyProcess,
                d.QtyRemain,
                d.DemandPercent,
                d.Rank,
                Status = d.Status.ToString(),
                d.Remark
            }).ToList(),
            supplies = session.Supplies.Select(s => new
            {
                s.Id,
                s.PeriodMonth,
                s.SpecCode,
                s.ModelCode,
                s.ColorCode,
                s.QtyInit,
                s.QtyProcess,
                s.QtyRemain,
                s.SupplyPercent,
                s.Rank,
                Status = s.Status.ToString(),
                s.Remark
            }).ToList()
        };
    }

    public async Task<object?> UpdateAsync(string allocationNo, UpdateOrderAllocationDto dto)
    {
        var session = await db.OrderAllocationSessions
            .FirstOrDefaultAsync(x => x.OrgId == Org && x.AllocationNo == allocationNo.Trim().ToUpperInvariant());

        if (session is null) return null;

        if (session.Status != OrderAllocationStatus.Draft)
            throw new InvalidOperationException($"Chỉ cho phép cập nhật phiên phân bổ khi ở trạng thái Nháp (Draft). Trạng thái hiện tại: {session.Status}.");

        if (dto.SalesPolicyCode != null) session.SalesPolicyCode = dto.SalesPolicyCode.Trim();
        if (dto.Remark != null) session.Remark = dto.Remark.Trim();

        await db.SaveChangesAsync();

        return new
        {
            success = true,
            message = $"Cập nhật phiên phân bổ {session.AllocationNo} thành công.",
            allocationNo = session.AllocationNo
        };
    }

    public async Task<object?> AllocateAsync(string allocationNo)
    {
        var session = await db.OrderAllocationSessions
            .Include(x => x.Demands)
            .Include(x => x.Supplies)
            .FirstOrDefaultAsync(x => x.OrgId == Org && x.AllocationNo == allocationNo.Trim().ToUpperInvariant());

        if (session is null) return null;

        if (session.Status != OrderAllocationStatus.Draft)
            throw new InvalidOperationException($"Chỉ có thể chạy phân bổ khi phiên ở trạng thái Nháp (Draft). Trạng thái hiện tại: {session.Status}.");

        if (session.Demands.Count == 0)
            throw new InvalidOperationException("Không thể phân bổ phiên không có dòng nhu cầu (Demand).");
        if (session.Supplies.Count == 0)
            throw new InvalidOperationException("Không thể phân bổ phiên không có dòng cơ cấu cung (Supply).");

        // Reset kết quả phân bổ trước khi chạy lại
        foreach (var d in session.Demands)
        {
            d.QtyProcess = 0;
            d.QtyRemain = d.QtyInit;
            d.DemandPercent = 0m;
            d.Rank = 0;
            d.Status = OrderDemandStatus.Pending;
        }
        foreach (var s in session.Supplies)
        {
            s.QtyProcess = 0;
            s.QtyRemain = s.QtyInit;
            s.SupplyPercent = 0m;
            s.Rank = 0;
            s.Status = OrderSupplyStatus.Pending;
        }

        // Nhóm theo khóa Spec/Model/Color (mỗi nhóm phân bổ độc lập)
        var supplyGroups = session.Supplies
            .GroupBy(s => Key(s.SpecCode, s.ModelCode, s.ColorCode))
            .ToDictionary(g => g.Key, g => g.ToList());

        var demandGroups = session.Demands
            .GroupBy(d => Key(d.SpecCode, d.ModelCode, d.ColorCode))
            .ToDictionary(g => g.Key, g => g.ToList());

        // 1) Tính SupplyPercent + Rank cho từng dòng cung (theo tổng cung cùng Spec/Model/Color)
        foreach (var (_, supplies) in supplyGroups)
        {
            var totalSupply = supplies.Sum(s => s.QtyRemain);
            if (totalSupply <= 0) continue;

            foreach (var s in supplies)
                s.SupplyPercent = Math.Round((decimal)s.QtyRemain / totalSupply, 6);

            var rank = 1;
            foreach (var s in supplies.OrderByDescending(s => s.SupplyPercent))
                s.Rank = rank++;
        }

        // 2) Tính DemandPercent + Rank cho từng dòng nhu cầu (theo tổng nhu cầu cùng Spec/Model/Color)
        foreach (var (_, demands) in demandGroups)
        {
            var totalDemand = demands.Sum(d => d.QtyRemain);
            if (totalDemand <= 0) continue;

            foreach (var d in demands)
                d.DemandPercent = Math.Round((decimal)d.QtyRemain / totalDemand, 6);

            var rank = 1;
            foreach (var d in demands.OrderByDescending(d => d.DemandPercent))
                d.Rank = rank++;
        }

        // 3) Phân bổ theo tỷ lệ (Rule 2.2.1.a): QtyDemand = Floor((Supply / TotalDemand) * DemandDealer)
        foreach (var (key, demands) in demandGroups)
        {
            if (!supplyGroups.TryGetValue(key, out var supplies)) continue;

            var totalDemand = demands.Sum(d => d.QtyRemain);
            if (totalDemand <= 0) continue;

            var totalSupply = supplies.Sum(s => s.QtyRemain);
            if (totalSupply <= 0) continue;

            // Số cung khả dụng để phân bổ cho nhóm này (không vượt quá tổng nhu cầu)
            var allocatable = Math.Min(totalSupply, totalDemand);

            // Bước 3a: phân bổ theo tỷ lệ, làm tròn xuống
            var allocated = 0;
            foreach (var d in demands.OrderBy(d => d.Rank))
            {
                var qty = (int)Math.Floor((decimal)allocatable * d.QtyRemain / totalDemand);
                if (qty > d.QtyRemain) qty = d.QtyRemain;
                d.QtyProcess = qty;
                allocated += qty;
            }

            // Bước 3b: chia phần dư (do làm tròn) theo thứ hạng ưu tiên (Rank) cho tới khi hết cung
            var remainder = allocatable - allocated;
            if (remainder > 0)
            {
                foreach (var d in demands.OrderBy(d => d.Rank))
                {
                    if (remainder <= 0) break;
                    var canAdd = d.QtyRemain - d.QtyProcess;
                    if (canAdd <= 0) continue;
                    var add = Math.Min(canAdd, remainder);
                    d.QtyProcess += add;
                    remainder -= add;
                }
            }

            // Cập nhật QtyRemain + trạng thái dòng nhu cầu
            foreach (var d in demands)
            {
                d.QtyRemain = d.QtyInit - d.QtyProcess;
                d.Status = d.QtyProcess > 0 ? OrderDemandStatus.Allocated : OrderDemandStatus.Pending;
            }

            // Cập nhật QtyProcess/QtyRemain + trạng thái dòng cung (phân bổ theo Rank tới khi hết số đã cấp)
            var supplyToConsume = demands.Sum(d => d.QtyProcess);
            foreach (var s in supplies.OrderBy(s => s.Rank))
            {
                if (supplyToConsume <= 0) break;
                var take = Math.Min(s.QtyRemain, supplyToConsume);
                s.QtyProcess += take;
                s.QtyRemain -= take;
                supplyToConsume -= take;
            }
            foreach (var s in supplies)
                s.Status = s.QtyProcess > 0 ? OrderSupplyStatus.Allocated : OrderSupplyStatus.Pending;
        }

        session.TotalAllocatedQty = session.Demands.Sum(d => d.QtyProcess);
        session.TotalUnmetQty = session.Demands.Sum(d => d.QtyRemain);
        session.Status = OrderAllocationStatus.Allocated;
        session.AllocatedBy = "HQ_SALES_USER";
        session.AllocatedAt = DateTime.Now;

        await db.SaveChangesAsync();

        return new
        {
            success = true,
            message = $"Chạy phân bổ nhu cầu/cung cho phiên {session.AllocationNo} thành công.",
            allocationNo = session.AllocationNo,
            status = session.Status.ToString(),
            totalDemandQty = session.TotalDemandQty,
            totalSupplyQty = session.TotalSupplyQty,
            totalAllocatedQty = session.TotalAllocatedQty,
            totalUnmetQty = session.TotalUnmetQty,
            allocatedAt = session.AllocatedAt
        };
    }

    public async Task<object?> FinishAsync(string allocationNo)
    {
        var session = await db.OrderAllocationSessions
            .Include(x => x.Demands)
            .FirstOrDefaultAsync(x => x.OrgId == Org && x.AllocationNo == allocationNo.Trim().ToUpperInvariant());

        if (session is null) return null;

        if (session.Status != OrderAllocationStatus.Allocated)
            throw new InvalidOperationException($"Chỉ có thể chốt hoàn tất phiên phân bổ đã chạy phân bổ (Allocated). Trạng thái hiện tại: {session.Status}.");

        session.Status = OrderAllocationStatus.Finished;
        session.FinishedBy = "LD_SALES_HQ";
        session.FinishedAt = DateTime.Now;

        await db.SaveChangesAsync();

        return new
        {
            success = true,
            message = $"Chốt hoàn tất phân bổ nhu cầu/cung phiên {session.AllocationNo} thành công.",
            allocationNo = session.AllocationNo,
            status = session.Status.ToString(),
            totalAllocatedQty = session.TotalAllocatedQty,
            totalUnmetQty = session.TotalUnmetQty,
            finishedAt = session.FinishedAt
        };
    }

    public async Task<object?> RejectAsync(string allocationNo, RejectOrderAllocationDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Reason))
            throw new InvalidOperationException("Lý do từ chối không được để trống.");

        var session = await db.OrderAllocationSessions
            .Include(x => x.Demands)
            .Include(x => x.Supplies)
            .FirstOrDefaultAsync(x => x.OrgId == Org && x.AllocationNo == allocationNo.Trim().ToUpperInvariant());

        if (session is null) return null;

        if (session.Status == OrderAllocationStatus.Finished || session.Status == OrderAllocationStatus.Cancelled)
            throw new InvalidOperationException($"Không thể từ chối phiên phân bổ ở trạng thái {session.Status}.");

        session.Status = OrderAllocationStatus.Cancelled;
        session.RejectReason = dto.Reason.Trim();
        session.CancelledBy = dto.RejectedBy?.Trim() ?? "HQ_SALES_USER";
        session.CancelledAt = DateTime.Now;

        foreach (var d in session.Demands)
            d.Status = OrderDemandStatus.Rejected;
        foreach (var s in session.Supplies)
            s.Status = OrderSupplyStatus.Cancelled;

        await db.SaveChangesAsync();

        return new
        {
            success = true,
            message = $"Từ chối phiên phân bổ {session.AllocationNo} thành công.",
            allocationNo = session.AllocationNo,
            status = session.Status.ToString(),
            rejectReason = session.RejectReason
        };
    }

    public async Task<object?> CancelAsync(string allocationNo, CancelOrderAllocationDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Reason))
            throw new InvalidOperationException("Lý do hủy phiên không được để trống.");

        var session = await db.OrderAllocationSessions
            .Include(x => x.Demands)
            .Include(x => x.Supplies)
            .FirstOrDefaultAsync(x => x.OrgId == Org && x.AllocationNo == allocationNo.Trim().ToUpperInvariant());

        if (session is null) return null;

        if (session.Status == OrderAllocationStatus.Finished)
            throw new InvalidOperationException($"Phiên phân bổ {session.AllocationNo} đã chốt hoàn tất — không thể hủy.");
        if (session.Status == OrderAllocationStatus.Cancelled)
            throw new InvalidOperationException($"Phiên phân bổ {session.AllocationNo} đã bị hủy trước đó.");

        session.Status = OrderAllocationStatus.Cancelled;
        session.CancelReason = dto.Reason.Trim();
        session.CancelledBy = dto.CancelledBy?.Trim() ?? "HQ_SALES_USER";
        session.CancelledAt = DateTime.Now;

        foreach (var d in session.Demands)
            d.Status = OrderDemandStatus.Cancelled;
        foreach (var s in session.Supplies)
            s.Status = OrderSupplyStatus.Cancelled;

        await db.SaveChangesAsync();

        return new
        {
            success = true,
            message = $"Hủy phiên phân bổ {session.AllocationNo} thành công.",
            allocationNo = session.AllocationNo,
            status = session.Status.ToString(),
            cancelReason = session.CancelReason
        };
    }

    public async Task<object?> DeleteAsync(string allocationNo)
    {
        var session = await db.OrderAllocationSessions
            .Include(x => x.Demands)
            .Include(x => x.Supplies)
            .FirstOrDefaultAsync(x => x.OrgId == Org && x.AllocationNo == allocationNo.Trim().ToUpperInvariant());

        if (session is null) return null;

        if (session.Status != OrderAllocationStatus.Draft)
            throw new InvalidOperationException($"Chỉ cho phép xóa phiên phân bổ khi ở trạng thái Nháp (Draft). Trạng thái hiện tại: {session.Status}.");

        db.OrderAllocationSessions.Remove(session);
        await db.SaveChangesAsync();

        return new
        {
            success = true,
            message = $"Đã xóa phiên phân bổ {allocationNo} khỏi hệ thống."
        };
    }

    public async Task<object> StatsAsync()
    {
        var q = db.OrderAllocationSessions.Where(x => x.OrgId == Org);
        return new
        {
            total = await q.CountAsync(),
            draft = await q.CountAsync(x => x.Status == OrderAllocationStatus.Draft),
            allocated = await q.CountAsync(x => x.Status == OrderAllocationStatus.Allocated),
            finished = await q.CountAsync(x => x.Status == OrderAllocationStatus.Finished),
            cancelled = await q.CountAsync(x => x.Status == OrderAllocationStatus.Cancelled),
            totalDemandQty = await q.SumAsync(x => (int?)x.TotalDemandQty) ?? 0,
            totalSupplyQty = await q.SumAsync(x => (int?)x.TotalSupplyQty) ?? 0,
            totalAllocatedQty = await q.SumAsync(x => (int?)x.TotalAllocatedQty) ?? 0,
            totalUnmetQty = await q.SumAsync(x => (int?)x.TotalUnmetQty) ?? 0
        };
    }

    private static string Key(string spec, string model, string? color)
        => $"{spec.Trim().ToUpperInvariant()}|{model.Trim().ToUpperInvariant()}|{(color ?? "").Trim().ToUpperInvariant()}";
}
