using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using MiniSales.Data;
using MiniSales.Models;

namespace MiniSales.Services;

public record CreateCarInvoiceDetailDto(
    string Vin,
    string Model,
    string? SpecCode,
    string? ColorCode,
    string? EngineNo,
    decimal UnitPrice,
    decimal? VatRate,
    string? OrderNo,
    string? DeliveryOrderNo,
    string? Remark
);

public record CreateCarInvoiceDto(
    string? InvoiceCode,
    string? InvoiceSerial,
    string DealerCode,
    string? DealerName,
    string? Issuer, // HTC | HTCLD
    string? BankCode,
    string? BankName,
    string? BuyerTaxCode,
    string? BuyerAddress,
    decimal? VatRate,
    DateTime? InvoiceDate,
    string? Remark,
    List<CreateCarInvoiceDetailDto>? Details
);

public record IssueCarInvoiceDto(
    string? InvoiceNo,
    string? InvoiceSerial,
    DateTime? InvoiceDate,
    string? LookupCode,
    string? Remark
);

public record CreateAdjustInvoiceDto(
    string AdjType, // "Increase" | "Decrease"
    string Reason,
    DateTime? InvoiceDate,
    string? Remark,
    decimal? DiffSubTotal,
    decimal? DiffVatAmount,
    List<CreateCarInvoiceDetailDto>? Details
);

public record CreateReplaceInvoiceDto(
    string Reason,
    string? InvoiceSerial,
    DateTime? InvoiceDate,
    string? Remark,
    List<CreateCarInvoiceDetailDto>? Details
);

public record CancelCarInvoiceDto(
    string Reason
);

public interface ICarInvoiceService
{
    Task<object> CreateAsync(CreateCarInvoiceDto dto);
    Task<object> ListAsync(string? status, string? dealer, string? invoiceType, string? issuer);
    Task<object?> DetailAsync(string invoiceCode);
    Task<object?> IssueAsync(string invoiceCode, IssueCarInvoiceDto? dto);
    Task<object?> CreateAdjustAsync(string invoiceCode, CreateAdjustInvoiceDto dto);
    Task<object?> CreateReplaceAsync(string invoiceCode, CreateReplaceInvoiceDto dto);
    Task<object?> CancelAsync(string invoiceCode, CancelCarInvoiceDto dto);
    Task<object?> DeleteDraftAsync(string invoiceCode);
    Task<object> StatsAsync();
}

public sealed class CarInvoiceService(AppDbContext db, ITenantContext tenant) : ICarInvoiceService
{
    private Guid Org => tenant.OrgId;

    public async Task<object> CreateAsync(CreateCarInvoiceDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.DealerCode))
            throw new InvalidOperationException("DealerCode (mã đại lý mua hàng) không được để trống.");
        if (dto.Details is null || dto.Details.Count == 0)
            throw new InvalidOperationException("Hóa đơn bán buôn ô tô phải có ít nhất 1 xe (VIN) chi tiết.");

        var invCode = string.IsNullOrWhiteSpace(dto.InvoiceCode)
            ? "INV" + DateTime.Now.ToString("yyMMddHHmmss")
            : dto.InvoiceCode.Trim().ToUpperInvariant();

        if (await db.CarInvoices.AnyAsync(x => x.OrgId == Org && x.InvoiceCode == invCode))
            throw new InvalidOperationException($"Mã tham chiếu hóa đơn {invCode} đã tồn tại trong hệ thống.");

        var defaultVatRate = dto.VatRate ?? 10m;
        var issuer = string.Equals(dto.Issuer?.Trim(), "HTCLD", StringComparison.OrdinalIgnoreCase) ? "HTCLD" : "HTC";
        var bankCode = string.IsNullOrWhiteSpace(dto.BankCode) ? "DEALER" : dto.BankCode.Trim().ToUpperInvariant();

        var details = new List<CarInvoiceDetail>();
        var seenVins = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        decimal subTotal = 0;
        decimal vatTotal = 0;

        foreach (var d in dto.Details)
        {
            if (string.IsNullOrWhiteSpace(d.Vin))
                throw new InvalidOperationException("Số VIN của xe trong hóa đơn không được để trống.");
            if (string.IsNullOrWhiteSpace(d.Model))
                throw new InvalidOperationException($"Model xe (VIN: {d.Vin}) không được để trống.");
            if (d.UnitPrice <= 0)
                throw new InvalidOperationException($"Đơn giá xe (VIN: {d.Vin}) phải lớn hơn 0.");

            var vinTrim = d.Vin.Trim().ToUpperInvariant();
            if (!seenVins.Add(vinTrim))
                throw new InvalidOperationException($"Số VIN {vinTrim} bị lặp lại trong danh sách xe của hóa đơn.");

            var lineVatRate = d.VatRate ?? defaultVatRate;
            var lineVatAmount = Math.Round(d.UnitPrice * lineVatRate / 100m, 0);
            var lineTotal = d.UnitPrice + lineVatAmount;

            subTotal += d.UnitPrice;
            vatTotal += lineVatAmount;

            details.Add(new CarInvoiceDetail
            {
                Vin = vinTrim,
                Model = d.Model.Trim(),
                SpecCode = d.SpecCode?.Trim(),
                ColorCode = d.ColorCode?.Trim(),
                EngineNo = d.EngineNo?.Trim(),
                UnitPrice = d.UnitPrice,
                VatRate = lineVatRate,
                VatAmount = lineVatAmount,
                TotalAmount = lineTotal,
                OrderNo = d.OrderNo?.Trim(),
                DeliveryOrderNo = d.DeliveryOrderNo?.Trim(),
                Remark = d.Remark?.Trim()
            });
        }

        var invoice = new CarInvoice
        {
            OrgId = Org,
            InvoiceCode = invCode,
            InvoiceSerial = string.IsNullOrWhiteSpace(dto.InvoiceSerial) ? "1C26TBB" : dto.InvoiceSerial.Trim().ToUpperInvariant(),
            InvoiceNo = null,
            LookupCode = null,
            DealerCode = dto.DealerCode.Trim().ToUpperInvariant(),
            DealerName = dto.DealerName?.Trim() ?? dto.DealerCode.Trim().ToUpperInvariant(),
            Issuer = issuer,
            BankCode = bankCode,
            BankName = dto.BankName?.Trim(),
            InvoiceType = InvoiceType.Root,
            AdjType = InvoiceAdjType.Normal,
            RefInvoiceCode = null,
            Status = InvoiceStatus.Draft,
            VatRate = defaultVatRate,
            SubTotal = subTotal,
            VatAmount = vatTotal,
            TotalAmount = subTotal + vatTotal,
            TotalCars = details.Count,
            BuyerTaxCode = dto.BuyerTaxCode?.Trim(),
            BuyerAddress = dto.BuyerAddress?.Trim(),
            Remark = dto.Remark?.Trim(),
            InvoiceDate = dto.InvoiceDate ?? DateTime.Today,
            CreatedAt = DateTime.Now,
            Details = details
        };

        db.CarInvoices.Add(invoice);
        await db.SaveChangesAsync();

        return new
        {
            message = "Tạo hóa đơn VAT nháp thành công",
            invoice.InvoiceCode,
            invoice.InvoiceSerial,
            invoice.DealerCode,
            invoice.DealerName,
            invoice.Issuer,
            invoice.BankCode,
            invoice.Status,
            invoice.TotalCars,
            invoice.SubTotal,
            invoice.VatAmount,
            invoice.TotalAmount,
            invoice.InvoiceDate,
            invoice.CreatedAt
        };
    }

    public async Task<object> ListAsync(string? status, string? dealer, string? invoiceType, string? issuer)
    {
        var q = db.CarInvoices
            .AsNoTracking()
            .Include(x => x.Details)
            .Where(x => x.OrgId == Org);

        if (!string.IsNullOrWhiteSpace(status))
        {
            var stLower = status.Trim().ToLowerInvariant();
            if (stLower is "draft" or "p" or "0") q = q.Where(x => x.Status == InvoiceStatus.Draft);
            else if (stLower is "issued" or "f" or "1") q = q.Where(x => x.Status == InvoiceStatus.Issued);
            else if (stLower is "cancelled" or "c" or "2") q = q.Where(x => x.Status == InvoiceStatus.Cancelled);
        }

        if (!string.IsNullOrWhiteSpace(dealer))
        {
            var d = dealer.Trim().ToLowerInvariant();
            q = q.Where(x => x.DealerCode.ToLower().Contains(d) || x.DealerName.ToLower().Contains(d));
        }

        if (!string.IsNullOrWhiteSpace(invoiceType))
        {
            var tLower = invoiceType.Trim().ToLowerInvariant();
            if (tLower is "root" or "invoiceroot" or "0") q = q.Where(x => x.InvoiceType == InvoiceType.Root);
            else if (tLower is "adjust" or "invoiceadj" or "1") q = q.Where(x => x.InvoiceType == InvoiceType.Adjust);
            else if (tLower is "replace" or "invoicereplace" or "2") q = q.Where(x => x.InvoiceType == InvoiceType.Replace);
        }

        if (!string.IsNullOrWhiteSpace(issuer))
        {
            var iss = issuer.Trim().ToUpperInvariant();
            q = q.Where(x => x.Issuer == iss);
        }

        var list = await q.OrderByDescending(x => x.CreatedAt)
            .Select(x => new
            {
                x.Id,
                x.InvoiceCode,
                x.InvoiceNo,
                x.InvoiceSerial,
                x.LookupCode,
                x.DealerCode,
                x.DealerName,
                x.Issuer,
                x.BankCode,
                x.BankName,
                InvoiceType = x.InvoiceType.ToString(),
                AdjType = x.AdjType.ToString(),
                x.RefInvoiceCode,
                Status = x.Status.ToString(),
                x.VatRate,
                x.SubTotal,
                x.VatAmount,
                x.TotalAmount,
                x.TotalCars,
                x.InvoiceDate,
                x.CreatedAt,
                x.IssuedAt,
                x.CancelledAt,
                x.Reason,
                x.Remark,
                Cars = x.Details.Select(d => new
                {
                    d.Id,
                    d.Vin,
                    d.Model,
                    d.SpecCode,
                    d.UnitPrice,
                    d.VatAmount,
                    d.TotalAmount,
                    d.OrderNo,
                    d.DeliveryOrderNo
                })
            })
            .ToListAsync();

        return list;
    }

    public async Task<object?> DetailAsync(string invoiceCode)
    {
        var inv = await db.CarInvoices
            .AsNoTracking()
            .Include(x => x.Details)
            .FirstOrDefaultAsync(x => x.OrgId == Org && x.InvoiceCode == invoiceCode.Trim().ToUpperInvariant());

        if (inv == null) return null;

        return new
        {
            inv.Id,
            inv.InvoiceCode,
            inv.InvoiceNo,
            inv.InvoiceSerial,
            inv.LookupCode,
            inv.DealerCode,
            inv.DealerName,
            inv.Issuer,
            inv.BankCode,
            inv.BankName,
            InvoiceType = inv.InvoiceType.ToString(),
            AdjType = inv.AdjType.ToString(),
            inv.RefInvoiceCode,
            Status = inv.Status.ToString(),
            inv.VatRate,
            inv.SubTotal,
            inv.VatAmount,
            inv.TotalAmount,
            inv.TotalCars,
            inv.BuyerTaxCode,
            inv.BuyerAddress,
            inv.Reason,
            inv.Remark,
            inv.InvoiceDate,
            inv.CreatedAt,
            inv.IssuedAt,
            inv.CancelledAt,
            Details = inv.Details.Select(d => new
            {
                d.Id,
                d.Vin,
                d.Model,
                d.SpecCode,
                d.ColorCode,
                d.EngineNo,
                d.UnitPrice,
                d.VatRate,
                d.VatAmount,
                d.TotalAmount,
                d.OrderNo,
                d.DeliveryOrderNo,
                d.Remark
            })
        };
    }

    public async Task<object?> IssueAsync(string invoiceCode, IssueCarInvoiceDto? dto)
    {
        var inv = await db.CarInvoices
            .Include(x => x.Details)
            .FirstOrDefaultAsync(x => x.OrgId == Org && x.InvoiceCode == invoiceCode.Trim().ToUpperInvariant());

        if (inv == null) return null;

        if (inv.Status != InvoiceStatus.Draft)
            throw new InvalidOperationException($"Chỉ có thể phát hành hóa đơn ở trạng thái Nháp (Draft). Trạng thái hiện tại: {inv.Status}.");

        if (inv.Details.Count == 0 && inv.TotalCars == 0)
            throw new InvalidOperationException("Hóa đơn không có danh sách xe chi tiết để phát hành.");

        // Sinh số hóa đơn điện tử VAT (DMS.Sales IssueHQ -> Qinvoice)
        if (!string.IsNullOrWhiteSpace(dto?.InvoiceNo))
        {
            inv.InvoiceNo = dto.InvoiceNo.Trim().PadLeft(7, '0');
        }
        else
        {
            var issuedCount = await db.CarInvoices.CountAsync(x => x.OrgId == Org && x.Status == InvoiceStatus.Issued);
            inv.InvoiceNo = (issuedCount + 1).ToString().PadLeft(7, '0');
        }

        if (!string.IsNullOrWhiteSpace(dto?.InvoiceSerial))
            inv.InvoiceSerial = dto.InvoiceSerial.Trim().ToUpperInvariant();

        if (dto?.InvoiceDate.HasValue == true)
            inv.InvoiceDate = dto.InvoiceDate.Value;

        // Sinh mã tra cứu điện tử (OS_HDDT_InvoiceCode)
        inv.LookupCode = string.IsNullOrWhiteSpace(dto?.LookupCode)
            ? GenerateLookupCode()
            : dto.LookupCode.Trim().ToUpperInvariant();

        inv.Status = InvoiceStatus.Issued;
        inv.IssuedAt = DateTime.Now;
        if (!string.IsNullOrWhiteSpace(dto?.Remark))
            inv.Remark = string.IsNullOrEmpty(inv.Remark) ? dto.Remark.Trim() : $"{inv.Remark} | {dto.Remark.Trim()}";

        await db.SaveChangesAsync();

        return new
        {
            message = "Phát hành hóa đơn điện tử VAT thành công",
            inv.InvoiceCode,
            inv.InvoiceNo,
            inv.InvoiceSerial,
            inv.LookupCode,
            inv.DealerCode,
            inv.Issuer,
            Status = inv.Status.ToString(),
            inv.TotalCars,
            inv.SubTotal,
            inv.VatAmount,
            inv.TotalAmount,
            inv.InvoiceDate,
            inv.IssuedAt
        };
    }

    public async Task<object?> CreateAdjustAsync(string invoiceCode, CreateAdjustInvoiceDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Reason))
            throw new InvalidOperationException("Lý do điều chỉnh hóa đơn (Reason) không được để trống.");

        var rootInv = await db.CarInvoices
            .AsNoTracking()
            .Include(x => x.Details)
            .FirstOrDefaultAsync(x => x.OrgId == Org && x.InvoiceCode == invoiceCode.Trim().ToUpperInvariant());

        if (rootInv == null) return null;

        if (rootInv.Status != InvoiceStatus.Issued)
            throw new InvalidOperationException("Chỉ có thể lập hóa đơn điều chỉnh từ hóa đơn đã phát hành (Issued).");

        var adjType = dto.AdjType?.Trim().ToLowerInvariant() switch
        {
            "decrease" => InvoiceAdjType.Decrease,
            _ => InvoiceAdjType.Increase
        };

        var adjSign = adjType == InvoiceAdjType.Decrease ? -1m : 1m;
        var newCode = "ADJ" + DateTime.Now.ToString("yyMMddHHmmss");

        decimal subTotal = 0;
        decimal vatTotal = 0;
        var details = new List<CarInvoiceDetail>();

        if (dto.Details != null && dto.Details.Count > 0)
        {
            foreach (var d in dto.Details)
            {
                var price = Math.Abs(d.UnitPrice) * adjSign;
                var rate = d.VatRate ?? rootInv.VatRate;
                var vat = Math.Round(price * rate / 100m, 0);
                var total = price + vat;

                subTotal += price;
                vatTotal += vat;

                details.Add(new CarInvoiceDetail
                {
                    Vin = d.Vin.Trim().ToUpperInvariant(),
                    Model = d.Model.Trim(),
                    SpecCode = d.SpecCode?.Trim(),
                    ColorCode = d.ColorCode?.Trim(),
                    EngineNo = d.EngineNo?.Trim(),
                    UnitPrice = price,
                    VatRate = rate,
                    VatAmount = vat,
                    TotalAmount = total,
                    OrderNo = d.OrderNo?.Trim(),
                    DeliveryOrderNo = d.DeliveryOrderNo?.Trim(),
                    Remark = d.Remark?.Trim()
                });
            }
        }
        else
        {
            var diffSub = (dto.DiffSubTotal ?? 0) * adjSign;
            var diffVat = (dto.DiffVatAmount ?? Math.Round(diffSub * rootInv.VatRate / 100m, 0));
            subTotal = diffSub;
            vatTotal = diffVat;

            // Kế thừa danh sách VIN từ HĐ gốc ghi chú điều chỉnh
            foreach (var r in rootInv.Details)
            {
                details.Add(new CarInvoiceDetail
                {
                    Vin = r.Vin,
                    Model = r.Model,
                    SpecCode = r.SpecCode,
                    ColorCode = r.ColorCode,
                    EngineNo = r.EngineNo,
                    UnitPrice = 0,
                    VatRate = r.VatRate,
                    VatAmount = 0,
                    TotalAmount = 0,
                    OrderNo = r.OrderNo,
                    DeliveryOrderNo = r.DeliveryOrderNo,
                    Remark = $"Điều chỉnh theo HĐ gốc {rootInv.InvoiceCode} ({adjType})"
                });
            }
        }

        var adjustInv = new CarInvoice
        {
            OrgId = Org,
            InvoiceCode = newCode,
            InvoiceSerial = rootInv.InvoiceSerial,
            InvoiceNo = null,
            LookupCode = null,
            DealerCode = rootInv.DealerCode,
            DealerName = rootInv.DealerName,
            Issuer = rootInv.Issuer,
            BankCode = rootInv.BankCode,
            BankName = rootInv.BankName,
            InvoiceType = InvoiceType.Adjust,
            AdjType = adjType,
            RefInvoiceCode = rootInv.InvoiceCode,
            Status = InvoiceStatus.Draft,
            VatRate = rootInv.VatRate,
            SubTotal = subTotal,
            VatAmount = vatTotal,
            TotalAmount = subTotal + vatTotal,
            TotalCars = rootInv.TotalCars,
            BuyerTaxCode = rootInv.BuyerTaxCode,
            BuyerAddress = rootInv.BuyerAddress,
            Reason = dto.Reason.Trim(),
            Remark = dto.Remark?.Trim(),
            InvoiceDate = dto.InvoiceDate ?? DateTime.Today,
            CreatedAt = DateTime.Now,
            Details = details
        };

        db.CarInvoices.Add(adjustInv);
        await db.SaveChangesAsync();

        return new
        {
            message = "Tạo hóa đơn điều chỉnh thành công",
            adjustInv.InvoiceCode,
            RefInvoiceCode = rootInv.InvoiceCode,
            RootInvoiceNo = rootInv.InvoiceNo,
            InvoiceType = adjustInv.InvoiceType.ToString(),
            AdjType = adjustInv.AdjType.ToString(),
            adjustInv.Reason,
            adjustInv.SubTotal,
            adjustInv.VatAmount,
            adjustInv.TotalAmount,
            Status = adjustInv.Status.ToString(),
            adjustInv.CreatedAt
        };
    }

    public async Task<object?> CreateReplaceAsync(string invoiceCode, CreateReplaceInvoiceDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Reason))
            throw new InvalidOperationException("Lý do thay thế hóa đơn (Reason) không được để trống.");

        var rootInv = await db.CarInvoices
            .Include(x => x.Details)
            .FirstOrDefaultAsync(x => x.OrgId == Org && x.InvoiceCode == invoiceCode.Trim().ToUpperInvariant());

        if (rootInv == null) return null;

        if (rootInv.Status != InvoiceStatus.Issued)
            throw new InvalidOperationException("Chỉ có thể lập hóa đơn thay thế từ hóa đơn đã phát hành (Issued).");

        // Hủy hóa đơn gốc và ghi nhận lý do thay thế
        rootInv.Status = InvoiceStatus.Cancelled;
        rootInv.CancelledAt = DateTime.Now;
        rootInv.Reason = $"Thay thế bởi hóa đơn mới. Lý do: {dto.Reason.Trim()}";

        var newCode = "REP" + DateTime.Now.ToString("yyMMddHHmmss");
        var details = new List<CarInvoiceDetail>();
        decimal subTotal = 0;
        decimal vatTotal = 0;

        if (dto.Details != null && dto.Details.Count > 0)
        {
            foreach (var d in dto.Details)
            {
                var rate = d.VatRate ?? rootInv.VatRate;
                var vat = Math.Round(d.UnitPrice * rate / 100m, 0);
                var total = d.UnitPrice + vat;

                subTotal += d.UnitPrice;
                vatTotal += vat;

                details.Add(new CarInvoiceDetail
                {
                    Vin = d.Vin.Trim().ToUpperInvariant(),
                    Model = d.Model.Trim(),
                    SpecCode = d.SpecCode?.Trim(),
                    ColorCode = d.ColorCode?.Trim(),
                    EngineNo = d.EngineNo?.Trim(),
                    UnitPrice = d.UnitPrice,
                    VatRate = rate,
                    VatAmount = vat,
                    TotalAmount = total,
                    OrderNo = d.OrderNo?.Trim(),
                    DeliveryOrderNo = d.DeliveryOrderNo?.Trim(),
                    Remark = d.Remark?.Trim()
                });
            }
        }
        else
        {
            // Tái sử dụng danh sách xe từ HĐ gốc
            foreach (var r in rootInv.Details)
            {
                subTotal += r.UnitPrice;
                vatTotal += r.VatAmount;
                details.Add(new CarInvoiceDetail
                {
                    Vin = r.Vin,
                    Model = r.Model,
                    SpecCode = r.SpecCode,
                    ColorCode = r.ColorCode,
                    EngineNo = r.EngineNo,
                    UnitPrice = r.UnitPrice,
                    VatRate = r.VatRate,
                    VatAmount = r.VatAmount,
                    TotalAmount = r.TotalAmount,
                    OrderNo = r.OrderNo,
                    DeliveryOrderNo = r.DeliveryOrderNo,
                    Remark = r.Remark
                });
            }
        }

        var replaceInv = new CarInvoice
        {
            OrgId = Org,
            InvoiceCode = newCode,
            InvoiceSerial = string.IsNullOrWhiteSpace(dto.InvoiceSerial) ? rootInv.InvoiceSerial : dto.InvoiceSerial.Trim().ToUpperInvariant(),
            InvoiceNo = null,
            LookupCode = null,
            DealerCode = rootInv.DealerCode,
            DealerName = rootInv.DealerName,
            Issuer = rootInv.Issuer,
            BankCode = rootInv.BankCode,
            BankName = rootInv.BankName,
            InvoiceType = InvoiceType.Replace,
            AdjType = InvoiceAdjType.Normal,
            RefInvoiceCode = rootInv.InvoiceCode,
            Status = InvoiceStatus.Draft,
            VatRate = rootInv.VatRate,
            SubTotal = subTotal,
            VatAmount = vatTotal,
            TotalAmount = subTotal + vatTotal,
            TotalCars = details.Count,
            BuyerTaxCode = rootInv.BuyerTaxCode,
            BuyerAddress = rootInv.BuyerAddress,
            Reason = dto.Reason.Trim(),
            Remark = dto.Remark?.Trim(),
            InvoiceDate = dto.InvoiceDate ?? DateTime.Today,
            CreatedAt = DateTime.Now,
            Details = details
        };

        db.CarInvoices.Add(replaceInv);
        await db.SaveChangesAsync();

        return new
        {
            message = "Tạo hóa đơn thay thế thành công (hóa đơn gốc đã được đánh dấu hủy)",
            replaceInv.InvoiceCode,
            RefInvoiceCode = rootInv.InvoiceCode,
            RootInvoiceNo = rootInv.InvoiceNo,
            RootInvoiceStatus = rootInv.Status.ToString(),
            InvoiceType = replaceInv.InvoiceType.ToString(),
            replaceInv.Reason,
            replaceInv.TotalCars,
            replaceInv.SubTotal,
            replaceInv.VatAmount,
            replaceInv.TotalAmount,
            Status = replaceInv.Status.ToString(),
            replaceInv.CreatedAt
        };
    }

    public async Task<object?> CancelAsync(string invoiceCode, CancelCarInvoiceDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Reason))
            throw new InvalidOperationException("Lý do hủy/thu hồi hóa đơn không được để trống.");

        var inv = await db.CarInvoices
            .FirstOrDefaultAsync(x => x.OrgId == Org && x.InvoiceCode == invoiceCode.Trim().ToUpperInvariant());

        if (inv == null) return null;

        if (inv.Status == InvoiceStatus.Cancelled)
            throw new InvalidOperationException($"Hóa đơn {inv.InvoiceCode} đã bị hủy từ trước.");

        if (inv.Status != InvoiceStatus.Issued)
            throw new InvalidOperationException($"Chỉ có thể thu hồi/hủy hóa đơn đã phát hành (Issued). Với hóa đơn nháp, hãy dùng lệnh xóa (DELETE).");

        inv.Status = InvoiceStatus.Cancelled;
        inv.CancelledAt = DateTime.Now;
        inv.Reason = dto.Reason.Trim();

        await db.SaveChangesAsync();

        return new
        {
            message = "Thu hồi/hủy hóa đơn điện tử thành công",
            inv.InvoiceCode,
            inv.InvoiceNo,
            inv.DealerCode,
            Status = inv.Status.ToString(),
            inv.Reason,
            inv.CancelledAt
        };
    }

    public async Task<object?> DeleteDraftAsync(string invoiceCode)
    {
        var inv = await db.CarInvoices
            .Include(x => x.Details)
            .FirstOrDefaultAsync(x => x.OrgId == Org && x.InvoiceCode == invoiceCode.Trim().ToUpperInvariant());

        if (inv == null) return null;

        if (inv.Status != InvoiceStatus.Draft)
            throw new InvalidOperationException($"Không thể xóa hóa đơn ở trạng thái {inv.Status}. Chỉ hóa đơn nháp (Draft) mới được phép xóa.");

        db.CarInvoices.Remove(inv);
        await db.SaveChangesAsync();

        return new
        {
            message = "Xóa hóa đơn nháp thành công",
            invoiceCode = inv.InvoiceCode
        };
    }

    public async Task<object> StatsAsync()
    {
        var all = await db.CarInvoices
            .AsNoTracking()
            .Where(x => x.OrgId == Org)
            .ToListAsync();

        var total = all.Count;
        var draft = all.Count(x => x.Status == InvoiceStatus.Draft);
        var issued = all.Count(x => x.Status == InvoiceStatus.Issued);
        var cancelled = all.Count(x => x.Status == InvoiceStatus.Cancelled);

        var issuedInvoices = all.Where(x => x.Status == InvoiceStatus.Issued).ToList();
        var totalSubTotal = issuedInvoices.Sum(x => x.SubTotal);
        var totalVatAmount = issuedInvoices.Sum(x => x.VatAmount);
        var totalAmount = issuedInvoices.Sum(x => x.TotalAmount);
        var totalCars = issuedInvoices.Sum(x => x.TotalCars);

        return new
        {
            total,
            draft,
            issued,
            cancelled,
            issuedSummary = new
            {
                totalCars,
                totalSubTotal,
                totalVatAmount,
                totalAmount
            }
        };
    }

    private static string GenerateLookupCode()
    {
        const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
        var bytes = RandomNumberGenerator.GetBytes(10);
        var sb = new char[10];
        for (int i = 0; i < 10; i++)
        {
            sb[i] = chars[bytes[i] % chars.Length];
        }
        return new string(sb);
    }
}
