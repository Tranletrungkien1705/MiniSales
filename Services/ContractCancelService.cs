using Microsoft.EntityFrameworkCore;
using MiniSales.Data;
using MiniSales.Models;

namespace MiniSales.Services;

public record CancelReasonItemDto(
    string CtrCTDNo,
    string CtrCTDName,
    string CtrCType,
    string GroupName,
    bool RequireRemark
);

public record CreateContractCancelCarDto(
    string OrderCode,
    string? Vin,
    string Model,
    string? SpecCode,
    string? ColorCode,
    string CtrCTDNo,
    string? Remark
);

public record CreateContractCancelDto(
    string? ContractCNo,
    string DealerCode,
    string? DealerName,
    string? Remark,
    List<CreateContractCancelCarDto>? Cars
);

public record ApproveContractCancelDto(
    string? ApprovedBy,
    string? Remark
);

public record RejectContractCancelDto(
    string Reason,
    string? RejectedBy
);

public record CancelContractCancelDto(
    string? Reason
);

public record AddContractCancelCarDto(
    string OrderCode,
    string? Vin,
    string Model,
    string? SpecCode,
    string? ColorCode,
    string CtrCTDNo,
    string? Remark
);

public interface IContractCancelService
{
    Task<object> GetReasonsAsync();
    Task<object> CreateAsync(CreateContractCancelDto dto);
    Task<object> ListAsync(string? status, string? dealer, string? orderCode, string? contractCNo);
    Task<object?> DetailAsync(string contractCNo);
    Task<object?> ApproveAsync(string contractCNo, ApproveContractCancelDto? dto);
    Task<object?> RejectAsync(string contractCNo, RejectContractCancelDto dto);
    Task<object?> CancelAsync(string contractCNo, CancelContractCancelDto? dto);
    Task<object?> AddCarAsync(string contractCNo, AddContractCancelCarDto dto);
    Task<object?> RemoveCarAsync(string contractCNo, long carId);
    Task<object> StatsAsync();
}

public sealed class ContractCancelService(AppDbContext db, ITenantContext tenant) : IContractCancelService
{
    private Guid Org => tenant.OrgId;

    // Danh mục chuẩn lý do hủy xe / hợp đồng theo DMS.Sales Mst_CtrCancelTypeDtl
    private static readonly List<CancelReasonItemDto> StaticReasons = new()
    {
        new("RC1.PRICE", "Biến động giá xe / Thay đổi chính sách ưu đãi", "RC1", "Giá bán & Khuyến mại", false),
        new("RC2.COLOR", "Khách hàng muốn đổi màu xe hoặc chuyển dòng xe khác", "RC2", "Màu sắc & Phiên bản", false),
        new("RC3.STOPBUY", "Khách hàng tạm dừng mua xe / đổi kế hoạch tài chính cá nhân", "RC3", "Nhu cầu khách hàng", false),
        new("RC4.LOANFAIL", "Ngân hàng từ chối cho vay trả góp / hồ sơ tín dụng không đạt", "RC4", "Tín dụng ngân hàng", false),
        new("RC5.OTHER", "Lý do khác (yêu cầu bắt buộc giải trình)", "RC5", "Lý do khác", true)
    };

    public Task<object> GetReasonsAsync()
    {
        return Task.FromResult<object>(new
        {
            total = StaticReasons.Count,
            items = StaticReasons
        });
    }

    private static CancelReasonItemDto ResolveReason(string code)
    {
        var item = StaticReasons.FirstOrDefault(x => string.Equals(x.CtrCTDNo, code, StringComparison.OrdinalIgnoreCase));
        if (item is not null) return item;

        // Cho phép mã tự do nếu chưa có trong danh mục chuẩn
        var isRc5 = code.StartsWith("RC5", StringComparison.OrdinalIgnoreCase);
        return new CancelReasonItemDto(code.Trim().ToUpperInvariant(), code.Trim(), isRc5 ? "RC5" : "RC_OTHER", "Lý do chung", isRc5);
    }

    private static List<ContractCancelDtl> DeriveDetails(List<ContractCancelCar> cars)
    {
        return cars
            .GroupBy(c => new
            {
                c.OrderCode,
                c.Model,
                SpecCode = c.SpecCode ?? "",
                ColorCode = c.ColorCode ?? ""
            })
            .Select(g => new ContractCancelDtl
            {
                OrderCode = g.Key.OrderCode,
                Model = g.Key.Model,
                SpecCode = string.IsNullOrWhiteSpace(g.Key.SpecCode) ? null : g.Key.SpecCode,
                ColorCode = string.IsNullOrWhiteSpace(g.Key.ColorCode) ? null : g.Key.ColorCode,
                Qty = g.Count(),
                Remark = $"Gom tự động từ {g.Count()} xe đề nghị hủy"
            })
            .ToList();
    }

    public async Task<object> CreateAsync(CreateContractCancelDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.DealerCode))
            throw new InvalidOperationException("DealerCode (mã đại lý đề nghị hủy) không được để trống.");

        if (dto.Cars is null || dto.Cars.Count == 0)
            throw new InvalidOperationException("Đề nghị hủy hợp đồng bán lẻ phải có ít nhất 1 chi tiết xe.");

        // Sinh số đề nghị hủy (format CCNyyMMdd0001) nếu người dùng không truyền
        string cancelNo;
        if (!string.IsNullOrWhiteSpace(dto.ContractCNo))
        {
            cancelNo = dto.ContractCNo.Trim().ToUpperInvariant();
        }
        else
        {
            var datePrefix = DateTime.Now.ToString("yyMMdd");
            var countToday = await db.ContractCancels
                .CountAsync(x => x.OrgId == Org && x.ContractCNo.StartsWith("CCN" + datePrefix));
            cancelNo = $"CCN{datePrefix}{(countToday + 1):D4}";
        }

        if (await db.ContractCancels.AnyAsync(x => x.OrgId == Org && x.ContractCNo == cancelNo))
            throw new InvalidOperationException($"Số đề nghị hủy {cancelNo} đã tồn tại trong hệ thống.");

        var cars = new List<ContractCancelCar>();
        var seenCarKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var c in dto.Cars)
        {
            if (string.IsNullOrWhiteSpace(c.OrderCode))
                throw new InvalidOperationException("Mỗi chi tiết xe bắt buộc phải có OrderCode (số hợp đồng bán lẻ).");
            if (string.IsNullOrWhiteSpace(c.Model))
                throw new InvalidOperationException("Mỗi chi tiết xe bắt buộc phải có Model.");
            if (string.IsNullOrWhiteSpace(c.CtrCTDNo))
                throw new InvalidOperationException($"Xe {c.Model} (HĐ: {c.OrderCode}) chưa chọn lý do hủy (CtrCTDNo).");

            var order = await db.Orders.FirstOrDefaultAsync(o => o.OrgId == Org && o.Code == c.OrderCode.Trim());
            if (order is null)
                throw new InvalidOperationException($"Không tìm thấy hợp đồng bán lẻ '{c.OrderCode}'.");

            if (order.Status == SalesStatus.Delivered)
                throw new InvalidOperationException($"Hợp đồng '{c.OrderCode}' đã hoàn tất bàn giao xe (Delivered) — không được lập đề nghị hủy.");

            if (order.Status == SalesStatus.Cancelled)
                throw new InvalidOperationException($"Hợp đồng '{c.OrderCode}' đã bị hủy trước đó (Cancelled).");

            // Ràng buộc kiểm tra đã có đề nghị hủy ở trạng thái Pending cho xe/hợp đồng này chưa
            var hasPendingCancel = await db.ContractCancelCars
                .Include(x => x.ContractCancelId)
                .AnyAsync(x => db.ContractCancels.Any(m => m.Id == x.ContractCancelId && m.OrgId == Org && m.Status == ContractCancelStatus.Pending)
                            && x.OrderCode == c.OrderCode.Trim()
                            && (c.Vin == null || x.Vin == c.Vin.Trim()));
            if (hasPendingCancel)
                throw new InvalidOperationException($"Hợp đồng '{c.OrderCode}' đã có một đề nghị hủy khác đang ở trạng thái Chờ duyệt (Pending).");

            var reason = ResolveReason(c.CtrCTDNo);
            if (reason.RequireRemark && string.IsNullOrWhiteSpace(c.Remark))
                throw new InvalidOperationException($"Lý do hủy '{reason.CtrCTDName}' ({reason.CtrCTDNo}) thuộc nhóm RC5 bắt buộc phải nhập ghi chú giải trình (Remark).");

            var carKey = $"{c.OrderCode.Trim()}::{c.Vin?.Trim()}::{c.Model.Trim()}";
            if (!seenCarKeys.Add(carKey))
                throw new InvalidOperationException($"Trùng lặp dòng xe trong danh sách đề nghị hủy: {c.Model} (VIN: {c.Vin}, HĐ: {c.OrderCode}).");

            cars.Add(new ContractCancelCar
            {
                OrderCode = c.OrderCode.Trim(),
                OrderId = order.Id,
                Vin = string.IsNullOrWhiteSpace(c.Vin) ? order.Vin : c.Vin.Trim(),
                Model = c.Model.Trim(),
                SpecCode = c.SpecCode?.Trim(),
                ColorCode = c.ColorCode?.Trim(),
                CtrCTDNo = reason.CtrCTDNo,
                CtrCTDName = reason.CtrCTDName,
                CtrCType = reason.CtrCType,
                Remark = c.Remark?.Trim()
            });
        }

        var details = DeriveDetails(cars);

        var cancelOrder = new ContractCancelOrder
        {
            OrgId = Org,
            ContractCNo = cancelNo,
            DealerCode = dto.DealerCode.Trim().ToUpperInvariant(),
            DealerName = string.IsNullOrWhiteSpace(dto.DealerName) ? dto.DealerCode.Trim().ToUpperInvariant() : dto.DealerName.Trim(),
            Status = ContractCancelStatus.Pending,
            TotalCars = cars.Count,
            Remark = dto.Remark?.Trim(),
            CreatedBy = "DEALER_USER",
            CreatedAt = DateTime.Now,
            Cars = cars,
            Details = details
        };

        db.ContractCancels.Add(cancelOrder);
        await db.SaveChangesAsync();

        return new
        {
            contractCNo = cancelOrder.ContractCNo,
            dealerCode = cancelOrder.DealerCode,
            dealerName = cancelOrder.DealerName,
            status = cancelOrder.Status.ToString(),
            totalCars = cancelOrder.TotalCars,
            remark = cancelOrder.Remark,
            createdAt = cancelOrder.CreatedAt,
            carsCount = cancelOrder.Cars.Count,
            detailsCount = cancelOrder.Details.Count
        };
    }

    public async Task<object> ListAsync(string? status, string? dealer, string? orderCode, string? contractCNo)
    {
        var q = db.ContractCancels
            .Include(x => x.Cars)
            .Include(x => x.Details)
            .Where(x => x.OrgId == Org)
            .AsNoTracking();

        if (!string.IsNullOrWhiteSpace(dealer))
        {
            var d = dealer.Trim().ToUpperInvariant();
            q = q.Where(x => x.DealerCode == d || x.DealerName.Contains(d));
        }

        if (!string.IsNullOrWhiteSpace(contractCNo))
        {
            var cno = contractCNo.Trim().ToUpperInvariant();
            q = q.Where(x => x.ContractCNo.Contains(cno));
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            if (Enum.TryParse<ContractCancelStatus>(status, true, out var st))
                q = q.Where(x => x.Status == st);
        }

        if (!string.IsNullOrWhiteSpace(orderCode))
        {
            var ord = orderCode.Trim();
            q = q.Where(x => x.Cars.Any(c => c.OrderCode == ord));
        }

        var list = await q.OrderByDescending(x => x.CreatedAt).ToListAsync();
        return list.Select(x => new
        {
            id = x.Id,
            contractCNo = x.ContractCNo,
            dealerCode = x.DealerCode,
            dealerName = x.DealerName,
            status = x.Status.ToString(),
            totalCars = x.TotalCars,
            remark = x.Remark,
            rejectReason = x.RejectReason,
            createdBy = x.CreatedBy,
            createdAt = x.CreatedAt,
            approvedBy = x.ApprovedBy,
            approvedAt = x.ApprovedAt,
            cancelledAt = x.CancelledAt,
            cars = x.Cars.Select(c => new
            {
                id = c.Id,
                orderCode = c.OrderCode,
                vin = c.Vin,
                model = c.Model,
                specCode = c.SpecCode,
                colorCode = c.ColorCode,
                ctrCTDNo = c.CtrCTDNo,
                ctrCTDName = c.CtrCTDName,
                ctrCType = c.CtrCType,
                remark = c.Remark
            }),
            details = x.Details.Select(d => new
            {
                id = d.Id,
                orderCode = d.OrderCode,
                model = d.Model,
                specCode = d.SpecCode,
                colorCode = d.ColorCode,
                qty = d.Qty,
                remark = d.Remark
            })
        });
    }

    public async Task<object?> DetailAsync(string contractCNo)
    {
        var x = await db.ContractCancels
            .Include(m => m.Cars)
            .Include(m => m.Details)
            .FirstOrDefaultAsync(m => m.OrgId == Org && m.ContractCNo == contractCNo.Trim().ToUpperInvariant());

        if (x is null) return null;

        // Lấy thông tin các hợp đồng bán xe liên quan
        var orderCodes = x.Cars.Select(c => c.OrderCode).Distinct().ToList();
        var relatedOrders = await db.Orders
            .Where(o => o.OrgId == Org && orderCodes.Contains(o.Code))
            .Select(o => new
            {
                orderCode = o.Code,
                customerName = o.CustomerName,
                phone = o.Phone,
                status = o.Status.ToString(),
                netPrice = o.NetPrice,
                paid = o.Paid
            })
            .ToListAsync();

        return new
        {
            id = x.Id,
            contractCNo = x.ContractCNo,
            dealerCode = x.DealerCode,
            dealerName = x.DealerName,
            status = x.Status.ToString(),
            totalCars = x.TotalCars,
            remark = x.Remark,
            rejectReason = x.RejectReason,
            createdBy = x.CreatedBy,
            createdAt = x.CreatedAt,
            approvedBy = x.ApprovedBy,
            approvedAt = x.ApprovedAt,
            cancelledAt = x.CancelledAt,
            cars = x.Cars.Select(c => new
            {
                id = c.Id,
                orderCode = c.OrderCode,
                orderId = c.OrderId,
                vin = c.Vin,
                model = c.Model,
                specCode = c.SpecCode,
                colorCode = c.ColorCode,
                ctrCTDNo = c.CtrCTDNo,
                ctrCTDName = c.CtrCTDName,
                ctrCType = c.CtrCType,
                remark = c.Remark
            }),
            details = x.Details.Select(d => new
            {
                id = d.Id,
                orderCode = d.OrderCode,
                model = d.Model,
                specCode = d.SpecCode,
                colorCode = d.ColorCode,
                qty = d.Qty,
                remark = d.Remark
            }),
            relatedOrders
        };
    }

    public async Task<object?> ApproveAsync(string contractCNo, ApproveContractCancelDto? dto)
    {
        var x = await db.ContractCancels
            .Include(m => m.Cars)
            .Include(m => m.Details)
            .FirstOrDefaultAsync(m => m.OrgId == Org && m.ContractCNo == contractCNo.Trim().ToUpperInvariant());

        if (x is null) return null;

        if (x.Status != ContractCancelStatus.Pending)
            throw new InvalidOperationException($"Chỉ có thể duyệt đề nghị hủy ở trạng thái Chờ duyệt (Pending). Trạng thái hiện tại: {x.Status}.");

        // Cập nhật trạng thái đề nghị hủy
        x.Status = ContractCancelStatus.Approved;
        x.ApprovedAt = DateTime.Now;
        x.ApprovedBy = string.IsNullOrWhiteSpace(dto?.ApprovedBy) ? "HQ_APPROVER" : dto.ApprovedBy.Trim();
        if (!string.IsNullOrWhiteSpace(dto?.Remark))
        {
            x.Remark = string.IsNullOrWhiteSpace(x.Remark) ? dto.Remark.Trim() : $"{x.Remark} | {dto.Remark.Trim()}";
        }

        // Cập nhật trạng thái các hợp đồng bán lẻ tương ứng -> Cancelled (giải phóng xe/hợp đồng)
        var orderCodes = x.Cars.Select(c => c.OrderCode).Distinct().ToList();
        var affectedOrders = await db.Orders
            .Where(o => o.OrgId == Org && orderCodes.Contains(o.Code))
            .ToListAsync();

        foreach (var ord in affectedOrders)
        {
            if (ord.Status != SalesStatus.Delivered)
            {
                ord.Status = SalesStatus.Cancelled;
            }
        }

        await db.SaveChangesAsync();

        return new
        {
            contractCNo = x.ContractCNo,
            status = x.Status.ToString(),
            approvedBy = x.ApprovedBy,
            approvedAt = x.ApprovedAt,
            affectedOrders = affectedOrders.Select(o => new { o.Code, status = o.Status.ToString() })
        };
    }

    public async Task<object?> RejectAsync(string contractCNo, RejectContractCancelDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Reason))
            throw new InvalidOperationException("Lý do từ chối (Reason) không được để trống.");

        var x = await db.ContractCancels
            .FirstOrDefaultAsync(m => m.OrgId == Org && m.ContractCNo == contractCNo.Trim().ToUpperInvariant());

        if (x is null) return null;

        if (x.Status != ContractCancelStatus.Pending)
            throw new InvalidOperationException($"Chỉ có thể từ chối đề nghị hủy ở trạng thái Chờ duyệt (Pending). Trạng thái hiện tại: {x.Status}.");

        x.Status = ContractCancelStatus.Rejected;
        x.RejectReason = dto.Reason.Trim();
        x.ApprovedBy = string.IsNullOrWhiteSpace(dto.RejectedBy) ? "HQ_APPROVER" : dto.RejectedBy.Trim();
        x.ApprovedAt = DateTime.Now;

        await db.SaveChangesAsync();

        return new
        {
            contractCNo = x.ContractCNo,
            status = x.Status.ToString(),
            rejectReason = x.RejectReason,
            rejectedBy = x.ApprovedBy,
            rejectedAt = x.ApprovedAt
        };
    }

    public async Task<object?> CancelAsync(string contractCNo, CancelContractCancelDto? dto)
    {
        var x = await db.ContractCancels
            .FirstOrDefaultAsync(m => m.OrgId == Org && m.ContractCNo == contractCNo.Trim().ToUpperInvariant());

        if (x is null) return null;

        if (x.Status != ContractCancelStatus.Pending)
            throw new InvalidOperationException($"Chỉ có thể hủy đề nghị khi đang ở trạng thái Chờ duyệt (Pending). Trạng thái hiện tại: {x.Status}.");

        x.Status = ContractCancelStatus.Cancelled;
        x.CancelledAt = DateTime.Now;
        if (!string.IsNullOrWhiteSpace(dto?.Reason))
        {
            x.Remark = string.IsNullOrWhiteSpace(x.Remark) ? $"Hủy: {dto.Reason.Trim()}" : $"{x.Remark} | Hủy: {dto.Reason.Trim()}";
        }

        await db.SaveChangesAsync();

        return new
        {
            contractCNo = x.ContractCNo,
            status = x.Status.ToString(),
            cancelledAt = x.CancelledAt,
            remark = x.Remark
        };
    }

    public async Task<object?> AddCarAsync(string contractCNo, AddContractCancelCarDto dto)
    {
        var x = await db.ContractCancels
            .Include(m => m.Cars)
            .Include(m => m.Details)
            .FirstOrDefaultAsync(m => m.OrgId == Org && m.ContractCNo == contractCNo.Trim().ToUpperInvariant());

        if (x is null) return null;

        if (x.Status != ContractCancelStatus.Pending)
            throw new InvalidOperationException($"Chỉ có thể thêm xe khi đề nghị đang ở trạng thái Chờ duyệt (Pending). Trạng thái hiện tại: {x.Status}.");

        if (string.IsNullOrWhiteSpace(dto.OrderCode))
            throw new InvalidOperationException("OrderCode (số hợp đồng bán lẻ) không được để trống.");
        if (string.IsNullOrWhiteSpace(dto.Model))
            throw new InvalidOperationException("Model không được để trống.");
        if (string.IsNullOrWhiteSpace(dto.CtrCTDNo))
            throw new InvalidOperationException("Lý do hủy (CtrCTDNo) không được để trống.");

        var order = await db.Orders.FirstOrDefaultAsync(o => o.OrgId == Org && o.Code == dto.OrderCode.Trim());
        if (order is null)
            throw new InvalidOperationException($"Không tìm thấy hợp đồng bán lẻ '{dto.OrderCode}'.");

        if (order.Status == SalesStatus.Delivered)
            throw new InvalidOperationException($"Hợp đồng '{dto.OrderCode}' đã giao xe — không được hủy.");
        if (order.Status == SalesStatus.Cancelled)
            throw new InvalidOperationException($"Hợp đồng '{dto.OrderCode}' đã hủy trước đó.");

        var reason = ResolveReason(dto.CtrCTDNo);
        if (reason.RequireRemark && string.IsNullOrWhiteSpace(dto.Remark))
            throw new InvalidOperationException($"Lý do hủy '{reason.CtrCTDName}' ({reason.CtrCTDNo}) thuộc nhóm RC5 bắt buộc phải có ghi chú giải trình (Remark).");

        // Kiểm tra trùng xe trong đề nghị
        var vin = string.IsNullOrWhiteSpace(dto.Vin) ? order.Vin : dto.Vin.Trim();
        var exists = x.Cars.Any(c => c.OrderCode == dto.OrderCode.Trim() && c.Vin == vin);
        if (exists)
            throw new InvalidOperationException($"Xe với VIN '{vin}' thuộc HĐ '{dto.OrderCode}' đã có trong đề nghị hủy này.");

        var car = new ContractCancelCar
        {
            ContractCancelId = x.Id,
            OrderCode = dto.OrderCode.Trim(),
            OrderId = order.Id,
            Vin = vin,
            Model = dto.Model.Trim(),
            SpecCode = dto.SpecCode?.Trim(),
            ColorCode = dto.ColorCode?.Trim(),
            CtrCTDNo = reason.CtrCTDNo,
            CtrCTDName = reason.CtrCTDName,
            CtrCType = reason.CtrCType,
            Remark = dto.Remark?.Trim()
        };

        x.Cars.Add(car);
        x.TotalCars = x.Cars.Count;

        // Dẫn xuất lại Details
        db.ContractCancelDtls.RemoveRange(x.Details);
        x.Details = DeriveDetails(x.Cars);

        await db.SaveChangesAsync();

        return new
        {
            contractCNo = x.ContractCNo,
            totalCars = x.TotalCars,
            addedCar = new
            {
                id = car.Id,
                orderCode = car.OrderCode,
                vin = car.Vin,
                model = car.Model,
                ctrCTDNo = car.CtrCTDNo,
                ctrCTDName = car.CtrCTDName
            },
            details = x.Details.Select(d => new
            {
                model = d.Model,
                specCode = d.SpecCode,
                colorCode = d.ColorCode,
                qty = d.Qty
            })
        };
    }

    public async Task<object?> RemoveCarAsync(string contractCNo, long carId)
    {
        var x = await db.ContractCancels
            .Include(m => m.Cars)
            .Include(m => m.Details)
            .FirstOrDefaultAsync(m => m.OrgId == Org && m.ContractCNo == contractCNo.Trim().ToUpperInvariant());

        if (x is null) return null;

        if (x.Status != ContractCancelStatus.Pending)
            throw new InvalidOperationException($"Chỉ có thể xóa xe khi đề nghị đang ở trạng thái Chờ duyệt (Pending). Trạng thái hiện tại: {x.Status}.");

        var car = x.Cars.FirstOrDefault(c => c.Id == carId);
        if (car is null)
            throw new InvalidOperationException($"Không tìm thấy dòng xe với Id={carId} trong đề nghị hủy.");

        x.Cars.Remove(car);
        x.TotalCars = x.Cars.Count;

        if (x.TotalCars == 0)
        {
            // Nếu xóa hết xe, tự động chuyển đề nghị sang Cancelled
            x.Status = ContractCancelStatus.Cancelled;
            x.CancelledAt = DateTime.Now;
            x.Remark = string.IsNullOrWhiteSpace(x.Remark) ? "Đã xóa toàn bộ xe khỏi đề nghị" : $"{x.Remark} | Đã xóa toàn bộ xe";
            db.ContractCancelDtls.RemoveRange(x.Details);
            x.Details.Clear();
        }
        else
        {
            db.ContractCancelDtls.RemoveRange(x.Details);
            x.Details = DeriveDetails(x.Cars);
        }

        await db.SaveChangesAsync();

        return new
        {
            contractCNo = x.ContractCNo,
            status = x.Status.ToString(),
            totalCars = x.TotalCars,
            remainingCarsCount = x.Cars.Count,
            details = x.Details.Select(d => new
            {
                model = d.Model,
                specCode = d.SpecCode,
                colorCode = d.ColorCode,
                qty = d.Qty
            })
        };
    }

    public async Task<object> StatsAsync()
    {
        var q = db.ContractCancels.Where(x => x.OrgId == Org);
        return new
        {
            total = await q.CountAsync(),
            pending = await q.CountAsync(x => x.Status == ContractCancelStatus.Pending),
            approved = await q.CountAsync(x => x.Status == ContractCancelStatus.Approved),
            rejected = await q.CountAsync(x => x.Status == ContractCancelStatus.Rejected),
            cancelled = await q.CountAsync(x => x.Status == ContractCancelStatus.Cancelled),
            totalCarsCancelled = await db.ContractCancelCars
                .Where(c => db.ContractCancels.Any(m => m.Id == c.ContractCancelId && m.OrgId == Org && m.Status == ContractCancelStatus.Approved))
                .CountAsync(),
            totalCarsPending = await db.ContractCancelCars
                .Where(c => db.ContractCancels.Any(m => m.Id == c.ContractCancelId && m.OrgId == Org && m.Status == ContractCancelStatus.Pending))
                .CountAsync()
        };
    }
}
