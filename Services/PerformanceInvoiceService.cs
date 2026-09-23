using System.Globalization;
using Microsoft.EntityFrameworkCore;
using MiniSales.Data;
using MiniSales.Models;

namespace MiniSales.Services;

public record CreatePerformanceInvoiceDetailDto(
    string LCTemp,
    string SpecCode,
    string ModelCode,
    string ColorCode,
    string WorkOrderNo,
    string PortCode,
    string PlantCode,
    int Quantity,
    decimal UnitPrice = 0,
    string? ModelName = null,
    string? ColorName = null,
    string? ContractNo = null,
    string FlagAutoPL = "1",
    string? Remark = null
);

public record CreatePerformanceInvoiceDto(
    string? RefNo,
    string OrderMonth,
    string ProductionMonth,
    string? ExpectedMonth = null,
    string FlagAutoPL = "1",
    string? Remark = null,
    string? CreatedBy = null,
    List<CreatePerformanceInvoiceDetailDto>? Details = null
);

public record UpdatePerformanceInvoiceDetailDto(
    long? Id,
    string LCTemp,
    string SpecCode,
    string ModelCode,
    string ColorCode,
    string WorkOrderNo,
    string PortCode,
    string PlantCode,
    int Quantity,
    decimal UnitPrice = 0,
    string? ModelName = null,
    string? ColorName = null,
    string? ContractNo = null,
    string FlagAutoPL = "1",
    string? Remark = null
);

public record UpdatePerformanceInvoiceDto(
    string? OrderMonth = null,
    string? ProductionMonth = null,
    string? ExpectedMonth = null,
    string? FlagAutoPL = null,
    string? Remark = null,
    string? UpdatedBy = null,
    List<UpdatePerformanceInvoiceDetailDto>? Details = null
);

public record ConfirmPerformanceInvoiceDto(
    string? ConfirmedBy = null,
    string? Remark = null
);

public record CancelPerformanceInvoiceDto(
    string CancelReason,
    string? CancelledBy = null
);

public record LinkContractNoDto(
    string ContractNo,
    List<long>? DetailIds = null,
    string? UpdatedBy = null
);

public record UnlinkContractNoDto(
    List<long>? DetailIds = null,
    string? UpdatedBy = null
);

public record PerformanceInvoiceStatsDto(
    int TotalPIs,
    int TotalQuantity,
    decimal TotalAmount,
    int TotalContractedCars,
    int TotalUncontractedCars,
    Dictionary<string, int> ByModel,
    Dictionary<string, int> ByProductionMonth,
    Dictionary<string, int> ByPort,
    Dictionary<string, int> ByStatus
);

public interface IPerformanceInvoiceService
{
    Task<string> GetNextRefNoAsync();
    Task<object> ListAsync(
        string? refNo,
        string? workOrderNo,
        string? productionMonth,
        string? orderMonth,
        PerformanceInvoiceStatus? status,
        string? modelCode,
        string? portCode,
        string? contractNo,
        DateTime? createdFrom,
        DateTime? createdTo,
        int pageIndex = 0,
        int pageSize = 50
    );
    Task<PerformanceInvoice?> DetailAsync(string refNo);
    Task<List<PerformanceInvoiceDetail>> SearchForCtrOverseaAsync(string? refNo, string? orderMonthFrom, string? orderMonthTo);
    Task<List<PerformanceInvoiceDetail>> SearchForTrspPlanAsync(string? productMonthFrom, string? productMonthTo);
    Task<PerformanceInvoice> CreateAsync(CreatePerformanceInvoiceDto dto);
    Task<PerformanceInvoice?> UpdateAsync(string refNo, UpdatePerformanceInvoiceDto dto);
    Task<PerformanceInvoice?> ConfirmAsync(string refNo, ConfirmPerformanceInvoiceDto? dto);
    Task<PerformanceInvoice?> LinkContractNoAsync(string refNo, LinkContractNoDto dto);
    Task<PerformanceInvoice?> UnlinkContractNoAsync(string refNo, UnlinkContractNoDto dto);
    Task<PerformanceInvoice?> CancelAsync(string refNo, CancelPerformanceInvoiceDto dto);
    Task<bool> DeleteDraftAsync(string refNo);
    Task<PerformanceInvoice?> DeleteDetailAsync(string refNo, long detailId);
    Task<PerformanceInvoiceStatsDto> StatsAsync();
}

public sealed class PerformanceInvoiceService(AppDbContext db, ITenantContext tenant) : IPerformanceInvoiceService
{
    public async Task<string> GetNextRefNoAsync()
    {
        var prefix = DateTime.Today.ToString("yyMM") + "PI";
        var last = await db.PerformanceInvoices
            .Where(x => x.OrgId == tenant.OrgId && x.RefNo.StartsWith(prefix))
            .OrderByDescending(x => x.RefNo)
            .Select(x => x.RefNo)
            .FirstOrDefaultAsync();

        if (last is not null && last.Length >= prefix.Length + 4 && int.TryParse(last.Substring(prefix.Length, 4), out var seq))
        {
            return $"{prefix}{(seq + 1):D4}";
        }

        return $"{prefix}0001";
    }

    private static string NormalizeMonth(string monthStr)
    {
        if (string.IsNullOrWhiteSpace(monthStr)) return "";
        var trimmed = monthStr.Trim();
        if (DateTime.TryParseExact(trimmed, "yyyy-MM", CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt))
        {
            return dt.ToString("yyyy-MM-01");
        }
        if (DateTime.TryParse(trimmed, out var dtGeneral))
        {
            return new DateTime(dtGeneral.Year, dtGeneral.Month, 1).ToString("yyyy-MM-01");
        }
        return trimmed;
    }

    private static string CalculateExpectedMonth(string productionMonthStr)
    {
        var norm = NormalizeMonth(productionMonthStr);
        if (DateTime.TryParse(norm, out var dt))
        {
            return dt.AddMonths(1).ToString("yyyy-MM-01");
        }
        return norm;
    }

    public async Task<object> ListAsync(
        string? refNo,
        string? workOrderNo,
        string? productionMonth,
        string? orderMonth,
        PerformanceInvoiceStatus? status,
        string? modelCode,
        string? portCode,
        string? contractNo,
        DateTime? createdFrom,
        DateTime? createdTo,
        int pageIndex = 0,
        int pageSize = 50)
    {
        var query = db.PerformanceInvoices
            .Include(x => x.Details)
            .Where(x => x.OrgId == tenant.OrgId);

        if (!string.IsNullOrWhiteSpace(refNo))
        {
            var r = refNo.Trim().ToUpper();
            query = query.Where(x => x.RefNo.ToUpper().Contains(r));
        }

        if (!string.IsNullOrWhiteSpace(productionMonth))
        {
            var pNorm = NormalizeMonth(productionMonth);
            query = query.Where(x => x.ProductionMonth.StartsWith(pNorm.Substring(0, Math.Min(7, pNorm.Length))));
        }

        if (!string.IsNullOrWhiteSpace(orderMonth))
        {
            var oNorm = NormalizeMonth(orderMonth);
            query = query.Where(x => x.OrderMonth.StartsWith(oNorm.Substring(0, Math.Min(7, oNorm.Length))));
        }

        if (status.HasValue)
        {
            query = query.Where(x => x.Status == status.Value);
        }

        if (!string.IsNullOrWhiteSpace(workOrderNo))
        {
            var wo = workOrderNo.Trim().ToUpper();
            query = query.Where(x => x.Details.Any(d => d.WorkOrderNo.ToUpper().Contains(wo)));
        }

        if (!string.IsNullOrWhiteSpace(modelCode))
        {
            var m = modelCode.Trim().ToUpper();
            query = query.Where(x => x.Details.Any(d => d.ModelCode.ToUpper() == m));
        }

        if (!string.IsNullOrWhiteSpace(portCode))
        {
            var p = portCode.Trim().ToUpper();
            query = query.Where(x => x.Details.Any(d => d.PortCode.ToUpper() == p));
        }

        if (!string.IsNullOrWhiteSpace(contractNo))
        {
            var c = contractNo.Trim().ToUpper();
            query = query.Where(x => x.Details.Any(d => d.ContractNo != null && d.ContractNo.ToUpper().Contains(c)));
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

    public async Task<PerformanceInvoice?> DetailAsync(string refNo)
    {
        return await db.PerformanceInvoices
            .Include(x => x.Details)
            .FirstOrDefaultAsync(x => x.OrgId == tenant.OrgId && x.RefNo == refNo);
    }

    public async Task<List<PerformanceInvoiceDetail>> SearchForCtrOverseaAsync(string? refNo, string? orderMonthFrom, string? orderMonthTo)
    {
        var query = db.PerformanceInvoiceDetails
            .Join(
                db.PerformanceInvoices.Where(p => p.OrgId == tenant.OrgId && p.Status != PerformanceInvoiceStatus.Cancelled),
                d => d.PerformanceInvoiceId,
                p => p.Id,
                (d, p) => new { Detail = d, Invoice = p }
            )
            .Where(x => string.IsNullOrEmpty(x.Detail.ContractNo));

        if (!string.IsNullOrWhiteSpace(refNo))
        {
            var r = refNo.Trim().ToUpper();
            query = query.Where(x => x.Invoice.RefNo.ToUpper().Contains(r));
        }

        if (!string.IsNullOrWhiteSpace(orderMonthFrom))
        {
            var mFrom = NormalizeMonth(orderMonthFrom);
            query = query.Where(x => string.Compare(x.Invoice.OrderMonth, mFrom) >= 0);
        }

        if (!string.IsNullOrWhiteSpace(orderMonthTo))
        {
            var mTo = NormalizeMonth(orderMonthTo);
            query = query.Where(x => string.Compare(x.Invoice.OrderMonth, mTo) <= 0);
        }

        return await query.Select(x => x.Detail).ToListAsync();
    }

    public async Task<List<PerformanceInvoiceDetail>> SearchForTrspPlanAsync(string? productMonthFrom, string? productMonthTo)
    {
        var query = db.PerformanceInvoiceDetails
            .Join(
                db.PerformanceInvoices.Where(p => p.OrgId == tenant.OrgId && p.Status != PerformanceInvoiceStatus.Cancelled),
                d => d.PerformanceInvoiceId,
                p => p.Id,
                (d, p) => new { Detail = d, Invoice = p }
            );

        if (!string.IsNullOrWhiteSpace(productMonthFrom))
        {
            var mFrom = NormalizeMonth(productMonthFrom);
            query = query.Where(x => string.Compare(x.Invoice.ProductionMonth, mFrom) >= 0);
        }

        if (!string.IsNullOrWhiteSpace(productMonthTo))
        {
            var mTo = NormalizeMonth(productMonthTo);
            query = query.Where(x => string.Compare(x.Invoice.ProductionMonth, mTo) <= 0);
        }

        return await query.Select(x => x.Detail).ToListAsync();
    }

    public async Task<PerformanceInvoice> CreateAsync(CreatePerformanceInvoiceDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.OrderMonth))
            throw new InvalidOperationException("Tháng đặt hàng (OrderMonth) không được để trống (OrdPerformanceInvoiceCreateHQ_OrderMonthNullOrEmpty).");

        if (string.IsNullOrWhiteSpace(dto.ProductionMonth))
            throw new InvalidOperationException("Tháng sản xuất (ProductionMonth) không được để trống (OrdPerformanceInvoiceCreateHQ_ProductionMonthNullOrEmpty).");

        if (dto.Details is null || dto.Details.Count == 0)
            throw new InvalidOperationException("Danh sách chi tiết dòng xe không được để trống (OrdPerformanceInvoiceCreateHQ_DetailNullOrEmpty).");

        var refNo = string.IsNullOrWhiteSpace(dto.RefNo) ? await GetNextRefNoAsync() : dto.RefNo.Trim().ToUpper();

        var exists = await db.PerformanceInvoices.AnyAsync(x => x.OrgId == tenant.OrgId && x.RefNo == refNo);
        if (exists)
            throw new InvalidOperationException($"Số hiệu PI '{refNo}' đã tồn tại trong hệ thống.");

        var orderMonth = NormalizeMonth(dto.OrderMonth);
        var productionMonth = NormalizeMonth(dto.ProductionMonth);
        var expectedMonth = string.IsNullOrWhiteSpace(dto.ExpectedMonth) ? CalculateExpectedMonth(productionMonth) : NormalizeMonth(dto.ExpectedMonth);

        var keys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var details = new List<PerformanceInvoiceDetail>();
        int totalQty = 0;
        decimal totalAmount = 0;

        for (int i = 0; i < dto.Details.Count; i++)
        {
            var d = dto.Details[i];
            int idx = i + 1;

            if (string.IsNullOrWhiteSpace(d.LCTemp))
                throw new InvalidOperationException($"Dòng {idx}: Mã L/C tạm thời (LCTemp) không được để trống (OrdPerformanceInvoiceCreateHQ_DetailLCTempNullOrEmpty).");
            if (string.IsNullOrWhiteSpace(d.SpecCode))
                throw new InvalidOperationException($"Dòng {idx}: Mã cấu hình (SpecCode) không được để trống (OrdPerformanceInvoiceCreateHQ_DetailSpecCodeNullOrEmpty).");
            if (string.IsNullOrWhiteSpace(d.ModelCode))
                throw new InvalidOperationException($"Dòng {idx}: Mã model (ModelCode) không được để trống (OrdPerformanceInvoiceCreateHQ_DetailModelCodeNullOrEmpty).");
            if (string.IsNullOrWhiteSpace(d.ColorCode))
                throw new InvalidOperationException($"Dòng {idx}: Mã màu (ColorCode) không được để trống (OrdPerformanceInvoiceCreateHQ_DetailColorCodeNullOrEmpty).");
            if (string.IsNullOrWhiteSpace(d.WorkOrderNo))
                throw new InvalidOperationException($"Dòng {idx}: Số lệnh sản xuất (WorkOrderNo) không được để trống (OrdPerformanceInvoiceCreateHQ_DetailWorkOrderNoNullOrEmpty).");
            if (string.IsNullOrWhiteSpace(d.PortCode))
                throw new InvalidOperationException($"Dòng {idx}: Mã cảng nhập khẩu (PortCode) không được để trống (OrdPerformanceInvoiceCreateHQ_DetailPortCodeNullOrEmpty).");
            if (string.IsNullOrWhiteSpace(d.PlantCode))
                throw new InvalidOperationException($"Dòng {idx}: Mã nhà máy sản xuất (PlantCode) không được để trống (OrdPerformanceInvoiceCreateHQ_DetailPlantCodeNullOrEmpty).");
            if (d.Quantity <= 0)
                throw new InvalidOperationException($"Dòng {idx}: Số lượng xe (Quantity) phải lớn hơn 0 (OrdPerformanceInvoiceCreateHQ_DetailQuantityInvalid).");

            var key = $"{d.LCTemp.Trim()}_{d.SpecCode.Trim()}_{d.ModelCode.Trim()}_{d.ColorCode.Trim()}_{d.WorkOrderNo.Trim()}";
            if (!keys.Add(key))
                throw new InvalidOperationException($"Dòng {idx}: Trùng lặp thông tin (LCTemp/SpecCode/ModelCode/ColorCode/WorkOrderNo: {key}) trong cùng phiếu PI.");

            var lineAmount = d.Quantity * d.UnitPrice;
            totalQty += d.Quantity;
            totalAmount += lineAmount;

            details.Add(new PerformanceInvoiceDetail
            {
                RefNo = refNo,
                LCTemp = d.LCTemp.Trim().ToUpper(),
                SpecCode = d.SpecCode.Trim().ToUpper(),
                ModelCode = d.ModelCode.Trim().ToUpper(),
                ModelName = d.ModelName ?? d.ModelCode.Trim().ToUpper(),
                ColorCode = d.ColorCode.Trim().ToUpper(),
                ColorName = d.ColorName,
                WorkOrderNo = d.WorkOrderNo.Trim().ToUpper(),
                PortCode = d.PortCode.Trim().ToUpper(),
                PlantCode = d.PlantCode.Trim().ToUpper(),
                Quantity = d.Quantity,
                UnitPrice = d.UnitPrice,
                TotalAmount = lineAmount,
                ContractNo = string.IsNullOrWhiteSpace(d.ContractNo) ? null : d.ContractNo.Trim().ToUpper(),
                FlagAutoPL = string.IsNullOrWhiteSpace(d.FlagAutoPL) ? "1" : d.FlagAutoPL.Trim(),
                Remark = d.Remark
            });
        }

        var pi = new PerformanceInvoice
        {
            OrgId = tenant.OrgId,
            RefNo = refNo,
            OrderMonth = orderMonth,
            ProductionMonth = productionMonth,
            ExpectedMonth = expectedMonth,
            FlagAutoPL = string.IsNullOrWhiteSpace(dto.FlagAutoPL) ? "1" : dto.FlagAutoPL.Trim(),
            Status = PerformanceInvoiceStatus.Pending,
            TotalQuantity = totalQty,
            TotalAmount = totalAmount,
            Remark = dto.Remark,
            CreatedBy = dto.CreatedBy ?? "SYSTEM",
            CreatedAt = DateTime.Now,
            Details = details
        };

        db.PerformanceInvoices.Add(pi);
        await db.SaveChangesAsync();
        return pi;
    }

    public async Task<PerformanceInvoice?> UpdateAsync(string refNo, UpdatePerformanceInvoiceDto dto)
    {
        var pi = await db.PerformanceInvoices
            .Include(x => x.Details)
            .FirstOrDefaultAsync(x => x.OrgId == tenant.OrgId && x.RefNo == refNo);

        if (pi is null) return null;

        if (pi.Status == PerformanceInvoiceStatus.Cancelled)
            throw new InvalidOperationException($"Không thể chỉnh sửa phiếu PI '{refNo}' đã bị hủy.");

        if (!string.IsNullOrWhiteSpace(dto.OrderMonth))
            pi.OrderMonth = NormalizeMonth(dto.OrderMonth);

        if (!string.IsNullOrWhiteSpace(dto.ProductionMonth))
        {
            pi.ProductionMonth = NormalizeMonth(dto.ProductionMonth);
            if (string.IsNullOrWhiteSpace(dto.ExpectedMonth))
            {
                pi.ExpectedMonth = CalculateExpectedMonth(pi.ProductionMonth);
            }
        }

        if (!string.IsNullOrWhiteSpace(dto.ExpectedMonth))
            pi.ExpectedMonth = NormalizeMonth(dto.ExpectedMonth);

        if (!string.IsNullOrWhiteSpace(dto.FlagAutoPL))
            pi.FlagAutoPL = dto.FlagAutoPL.Trim();

        if (dto.Remark is not null)
            pi.Remark = dto.Remark;

        if (dto.Details is not null && dto.Details.Count > 0)
        {
            var keys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var updatedDetails = new List<PerformanceInvoiceDetail>();
            int totalQty = 0;
            decimal totalAmount = 0;

            for (int i = 0; i < dto.Details.Count; i++)
            {
                var d = dto.Details[i];
                int idx = i + 1;

                if (string.IsNullOrWhiteSpace(d.LCTemp))
                    throw new InvalidOperationException($"Dòng {idx}: Mã L/C tạm thời (LCTemp) không được để trống.");
                if (string.IsNullOrWhiteSpace(d.SpecCode))
                    throw new InvalidOperationException($"Dòng {idx}: Mã cấu hình (SpecCode) không được để trống.");
                if (string.IsNullOrWhiteSpace(d.ModelCode))
                    throw new InvalidOperationException($"Dòng {idx}: Mã model (ModelCode) không được để trống.");
                if (string.IsNullOrWhiteSpace(d.ColorCode))
                    throw new InvalidOperationException($"Dòng {idx}: Mã màu (ColorCode) không được để trống.");
                if (string.IsNullOrWhiteSpace(d.WorkOrderNo))
                    throw new InvalidOperationException($"Dòng {idx}: Số lệnh sản xuất (WorkOrderNo) không được để trống.");
                if (string.IsNullOrWhiteSpace(d.PortCode))
                    throw new InvalidOperationException($"Dòng {idx}: Mã cảng nhập khẩu (PortCode) không được để trống.");
                if (string.IsNullOrWhiteSpace(d.PlantCode))
                    throw new InvalidOperationException($"Dòng {idx}: Mã nhà máy sản xuất (PlantCode) không được để trống.");
                if (d.Quantity <= 0)
                    throw new InvalidOperationException($"Dòng {idx}: Số lượng xe (Quantity) phải lớn hơn 0.");

                var key = $"{d.LCTemp.Trim()}_{d.SpecCode.Trim()}_{d.ModelCode.Trim()}_{d.ColorCode.Trim()}_{d.WorkOrderNo.Trim()}";
                if (!keys.Add(key))
                    throw new InvalidOperationException($"Dòng {idx}: Trùng lặp thông tin (LCTemp/SpecCode/ModelCode/ColorCode/WorkOrderNo: {key}) trong cùng phiếu PI.");

                var lineAmount = d.Quantity * d.UnitPrice;
                totalQty += d.Quantity;
                totalAmount += lineAmount;

                if (d.Id.HasValue && d.Id.Value > 0)
                {
                    var existing = pi.Details.FirstOrDefault(x => x.Id == d.Id.Value);
                    if (existing is not null)
                    {
                        if (!string.IsNullOrEmpty(existing.ContractNo) && existing.ContractNo != d.ContractNo)
                            throw new InvalidOperationException($"Không thể sửa dòng chi tiết đã vào hợp đồng ngoại '{existing.ContractNo}'.");

                        existing.LCTemp = d.LCTemp.Trim().ToUpper();
                        existing.SpecCode = d.SpecCode.Trim().ToUpper();
                        existing.ModelCode = d.ModelCode.Trim().ToUpper();
                        existing.ModelName = d.ModelName ?? existing.ModelName;
                        existing.ColorCode = d.ColorCode.Trim().ToUpper();
                        existing.ColorName = d.ColorName ?? existing.ColorName;
                        existing.WorkOrderNo = d.WorkOrderNo.Trim().ToUpper();
                        existing.PortCode = d.PortCode.Trim().ToUpper();
                        existing.PlantCode = d.PlantCode.Trim().ToUpper();
                        existing.Quantity = d.Quantity;
                        existing.UnitPrice = d.UnitPrice;
                        existing.TotalAmount = lineAmount;
                        existing.FlagAutoPL = string.IsNullOrWhiteSpace(d.FlagAutoPL) ? existing.FlagAutoPL : d.FlagAutoPL.Trim();
                        existing.Remark = d.Remark;
                        updatedDetails.Add(existing);
                        continue;
                    }
                }

                updatedDetails.Add(new PerformanceInvoiceDetail
                {
                    RefNo = pi.RefNo,
                    PerformanceInvoiceId = pi.Id,
                    LCTemp = d.LCTemp.Trim().ToUpper(),
                    SpecCode = d.SpecCode.Trim().ToUpper(),
                    ModelCode = d.ModelCode.Trim().ToUpper(),
                    ModelName = d.ModelName ?? d.ModelCode.Trim().ToUpper(),
                    ColorCode = d.ColorCode.Trim().ToUpper(),
                    ColorName = d.ColorName,
                    WorkOrderNo = d.WorkOrderNo.Trim().ToUpper(),
                    PortCode = d.PortCode.Trim().ToUpper(),
                    PlantCode = d.PlantCode.Trim().ToUpper(),
                    Quantity = d.Quantity,
                    UnitPrice = d.UnitPrice,
                    TotalAmount = lineAmount,
                    ContractNo = string.IsNullOrWhiteSpace(d.ContractNo) ? null : d.ContractNo.Trim().ToUpper(),
                    FlagAutoPL = string.IsNullOrWhiteSpace(d.FlagAutoPL) ? "1" : d.FlagAutoPL.Trim(),
                    Remark = d.Remark
                });
            }

            // Remove details not in the updated list
            var toRemove = pi.Details.Where(x => !updatedDetails.Any(u => u.Id == x.Id && x.Id > 0)).ToList();
            foreach (var rem in toRemove)
            {
                if (!string.IsNullOrEmpty(rem.ContractNo))
                    throw new InvalidOperationException($"Không thể xóa dòng chi tiết ID {rem.Id} đã được gán vào hợp đồng ngoại '{rem.ContractNo}'.");
                db.PerformanceInvoiceDetails.Remove(rem);
            }

            pi.Details = updatedDetails;
            pi.TotalQuantity = totalQty;
            pi.TotalAmount = totalAmount;
        }

        pi.UpdatedAt = DateTime.Now;
        pi.UpdatedBy = dto.UpdatedBy ?? "SYSTEM";

        await db.SaveChangesAsync();
        return pi;
    }

    public async Task<PerformanceInvoice?> ConfirmAsync(string refNo, ConfirmPerformanceInvoiceDto? dto)
    {
        var pi = await db.PerformanceInvoices
            .Include(x => x.Details)
            .FirstOrDefaultAsync(x => x.OrgId == tenant.OrgId && x.RefNo == refNo);

        if (pi is null) return null;

        if (pi.Status == PerformanceInvoiceStatus.Cancelled)
            throw new InvalidOperationException($"Phiếu PI '{refNo}' đã bị hủy, không thể chốt xác nhận.");

        if (pi.Details.Count == 0)
            throw new InvalidOperationException($"Phiếu PI '{refNo}' chưa có dòng chi tiết xe nào để chốt kế hoạch.");

        pi.Status = PerformanceInvoiceStatus.Confirmed;
        pi.ConfirmedBy = dto?.ConfirmedBy ?? "SYSTEM";
        pi.ConfirmedAt = DateTime.Now;
        if (!string.IsNullOrWhiteSpace(dto?.Remark))
        {
            pi.Remark = string.IsNullOrWhiteSpace(pi.Remark) ? dto.Remark : $"{pi.Remark} | {dto.Remark}";
        }

        await db.SaveChangesAsync();
        return pi;
    }

    public async Task<PerformanceInvoice?> LinkContractNoAsync(string refNo, LinkContractNoDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.ContractNo))
            throw new InvalidOperationException("Số hợp đồng ngoại thương (ContractNo) không được để trống.");

        var pi = await db.PerformanceInvoices
            .Include(x => x.Details)
            .FirstOrDefaultAsync(x => x.OrgId == tenant.OrgId && x.RefNo == refNo);

        if (pi is null) return null;

        if (pi.Status == PerformanceInvoiceStatus.Cancelled)
            throw new InvalidOperationException($"Phiếu PI '{refNo}' đã bị hủy, không thể gán hợp đồng ngoại.");

        var contractNo = dto.ContractNo.Trim().ToUpper();
        var targetDetails = (dto.DetailIds is not null && dto.DetailIds.Count > 0)
            ? pi.Details.Where(x => dto.DetailIds.Contains(x.Id)).ToList()
            : pi.Details;

        if (targetDetails.Count == 0)
            throw new InvalidOperationException("Không tìm thấy dòng chi tiết nào để gán hợp đồng ngoại.");

        foreach (var d in targetDetails)
        {
            d.ContractNo = contractNo;
        }

        if (pi.Details.All(x => !string.IsNullOrEmpty(x.ContractNo)))
        {
            pi.Status = PerformanceInvoiceStatus.Contracted;
        }

        pi.UpdatedAt = DateTime.Now;
        pi.UpdatedBy = dto.UpdatedBy ?? "SYSTEM";

        await db.SaveChangesAsync();
        return pi;
    }

    public async Task<PerformanceInvoice?> UnlinkContractNoAsync(string refNo, UnlinkContractNoDto dto)
    {
        var pi = await db.PerformanceInvoices
            .Include(x => x.Details)
            .FirstOrDefaultAsync(x => x.OrgId == tenant.OrgId && x.RefNo == refNo);

        if (pi is null) return null;

        var targetDetails = (dto.DetailIds is not null && dto.DetailIds.Count > 0)
            ? pi.Details.Where(x => dto.DetailIds.Contains(x.Id)).ToList()
            : pi.Details;

        foreach (var d in targetDetails)
        {
            d.ContractNo = null;
        }

        if (pi.Status == PerformanceInvoiceStatus.Contracted)
        {
            pi.Status = PerformanceInvoiceStatus.Confirmed;
        }

        pi.UpdatedAt = DateTime.Now;
        pi.UpdatedBy = dto.UpdatedBy ?? "SYSTEM";

        await db.SaveChangesAsync();
        return pi;
    }

    public async Task<PerformanceInvoice?> CancelAsync(string refNo, CancelPerformanceInvoiceDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.CancelReason))
            throw new InvalidOperationException("Lý do hủy (CancelReason) không được để trống.");

        var pi = await db.PerformanceInvoices
            .Include(x => x.Details)
            .FirstOrDefaultAsync(x => x.OrgId == tenant.OrgId && x.RefNo == refNo);

        if (pi is null) return null;

        if (pi.Status == PerformanceInvoiceStatus.Cancelled)
            return pi;

        if (pi.Details.Any(x => !string.IsNullOrEmpty(x.ContractNo)))
            throw new InvalidOperationException($"Không thể hủy PI '{refNo}' vì đã có dòng chi tiết được liên kết vào hợp đồng ngoại. Hãy gỡ hợp đồng trước khi hủy.");

        pi.Status = PerformanceInvoiceStatus.Cancelled;
        pi.CancelReason = dto.CancelReason;
        pi.CancelledBy = dto.CancelledBy ?? "SYSTEM";
        pi.CancelledAt = DateTime.Now;

        await db.SaveChangesAsync();
        return pi;
    }

    public async Task<bool> DeleteDraftAsync(string refNo)
    {
        var pi = await db.PerformanceInvoices
            .Include(x => x.Details)
            .FirstOrDefaultAsync(x => x.OrgId == tenant.OrgId && x.RefNo == refNo);

        if (pi is null) return false;

        if (pi.Status != PerformanceInvoiceStatus.Pending && pi.Status != PerformanceInvoiceStatus.Cancelled)
            throw new InvalidOperationException($"Chỉ có thể xóa phiếu PI ở trạng thái Nháp (Pending) hoặc Đã hủy (Cancelled). Phiếu '{refNo}' đang ở trạng thái {pi.Status}.");

        if (pi.Details.Any(x => !string.IsNullOrEmpty(x.ContractNo)))
            throw new InvalidOperationException($"Không thể xóa phiếu PI '{refNo}' vì có chi tiết đã vào hợp đồng ngoại.");

        db.PerformanceInvoices.Remove(pi);
        await db.SaveChangesAsync();
        return true;
    }

    public async Task<PerformanceInvoice?> DeleteDetailAsync(string refNo, long detailId)
    {
        var pi = await db.PerformanceInvoices
            .Include(x => x.Details)
            .FirstOrDefaultAsync(x => x.OrgId == tenant.OrgId && x.RefNo == refNo);

        if (pi is null) return null;

        var dtl = pi.Details.FirstOrDefault(x => x.Id == detailId);
        if (dtl is null)
            throw new InvalidOperationException($"Không tìm thấy dòng chi tiết ID {detailId} trong phiếu PI '{refNo}'.");

        if (!string.IsNullOrEmpty(dtl.ContractNo))
            throw new InvalidOperationException($"Không thể xóa dòng chi tiết đã vào hợp đồng ngoại '{dtl.ContractNo}'.");

        pi.Details.Remove(dtl);
        db.PerformanceInvoiceDetails.Remove(dtl);

        pi.TotalQuantity = pi.Details.Sum(x => x.Quantity);
        pi.TotalAmount = pi.Details.Sum(x => x.TotalAmount);
        pi.UpdatedAt = DateTime.Now;
        pi.UpdatedBy = "SYSTEM";

        await db.SaveChangesAsync();
        return pi;
    }

    public async Task<PerformanceInvoiceStatsDto> StatsAsync()
    {
        var list = await db.PerformanceInvoices
            .Include(x => x.Details)
            .Where(x => x.OrgId == tenant.OrgId)
            .ToListAsync();

        int totalPIs = list.Count;
        int totalQty = list.Sum(x => x.TotalQuantity);
        decimal totalAmount = list.Sum(x => x.TotalAmount);

        int contracted = list.SelectMany(x => x.Details).Where(d => !string.IsNullOrEmpty(d.ContractNo)).Sum(d => d.Quantity);
        int uncontracted = list.SelectMany(x => x.Details).Where(d => string.IsNullOrEmpty(d.ContractNo)).Sum(d => d.Quantity);

        var byModel = list.SelectMany(x => x.Details)
            .GroupBy(x => x.ModelCode)
            .ToDictionary(g => g.Key, g => g.Sum(x => x.Quantity));

        var byMonth = list
            .GroupBy(x => x.ProductionMonth.Length >= 7 ? x.ProductionMonth.Substring(0, 7) : x.ProductionMonth)
            .ToDictionary(g => g.Key, g => g.Sum(x => x.TotalQuantity));

        var byPort = list.SelectMany(x => x.Details)
            .GroupBy(x => x.PortCode)
            .ToDictionary(g => g.Key, g => g.Sum(x => x.Quantity));

        var byStatus = list
            .GroupBy(x => x.Status.ToString())
            .ToDictionary(g => g.Key, g => g.Count());

        return new PerformanceInvoiceStatsDto(
            totalPIs,
            totalQty,
            totalAmount,
            contracted,
            uncontracted,
            byModel,
            byMonth,
            byPort,
            byStatus
        );
    }
}
