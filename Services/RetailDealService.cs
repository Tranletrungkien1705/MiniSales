using Microsoft.EntityFrameworkCore;
using MiniSales.Data;
using MiniSales.Models;

namespace MiniSales.Services;

public record CreateRetailDealCarDto(
    string CarId,
    string Vin,
    string Model,
    string? SpecCode,
    string? ColorCode,
    decimal Price,
    string? PlateNo,
    string? CusInvoiceNo,
    DateTime? CusInvoiceDate,
    string? Remark
);

public record CreateRetailDealAttachDto(
    string? FileType,
    string FileName,
    string? FilePath,
    string? Remark
);

public record CreateRetailDealDto(
    string? DealNo,
    string? DealNoUser,
    string DealerCode,
    string? DealerName,
    string? DealerCodeBuyer,
    string? DlrContractNo,
    string? SalesType,
    string? CustomerType,
    DateTime? DealDate,
    string CustomerCodeBuyer,
    string BuyerFullName,
    string BuyerPhone,
    string BuyerIdCardNo,
    string? BuyerAddress,
    string? CustomerCodeHolder,
    string? HolderFullName,
    string? CustomerCodeDriver,
    string? DriverFullName,
    string SMCode,
    string? SMName,
    string? PaymentType,
    string? BankCode,
    string? BankName,
    decimal? BankLoanAmount,
    bool? FlagPDI,
    string? ReasonNotPDI,
    string? Remark,
    List<CreateRetailDealCarDto>? Items,
    List<CreateRetailDealAttachDto>? Attachments
);

public record UpdateRetailDealDto(
    string? DealNoUser,
    string? SalesType,
    string? CustomerType,
    string? BuyerFullName,
    string? BuyerPhone,
    string? BuyerAddress,
    string? CustomerCodeHolder,
    string? HolderFullName,
    string? CustomerCodeDriver,
    string? DriverFullName,
    string? SMCode,
    string? SMName,
    string? PaymentType,
    string? BankCode,
    string? BankName,
    decimal? BankLoanAmount,
    bool? FlagPDI,
    string? ReasonNotPDI,
    string? Remark,
    List<CreateRetailDealCarDto>? Items
);

public record UpdateDealPlateNoDto(
    string? CarId,
    string? Vin,
    string PlateNo
);

public record UpdateDealCusInvoiceDto(
    string? CarId,
    string? Vin,
    string CusInvoiceNo,
    DateTime CusInvoiceDate
);

public record VerifyDealCtmCareDto(
    string? VerifiedBy,
    string? Note
);

public record DeliverDealCarDto(
    string? CarId,
    string? Vin,
    DateTime? DeliveryDate,
    string? Remark
);

public record CancelDealerDealDto(
    string Reason,
    string? CancelledBy
);

public record AddDealerDealCarDto(
    string CarId,
    string Vin,
    string Model,
    string? SpecCode,
    string? ColorCode,
    decimal Price,
    string? PlateNo,
    string? CusInvoiceNo,
    DateTime? CusInvoiceDate,
    string? Remark
);

public record AddDealAttachDto(
    string? FileType,
    string FileName,
    string? FilePath,
    string? Remark
);

public interface IRetailDealService
{
    Task<object> CreateAsync(CreateRetailDealDto dto);
    Task<object?> UpdateAsync(string dealNo, UpdateRetailDealDto dto);
    Task<object?> UpdatePlateNoAsync(string dealNo, UpdateDealPlateNoDto dto);
    Task<object?> UpdateCusInvoiceAsync(string dealNo, UpdateDealCusInvoiceDto dto);
    Task<object?> AddAttachmentAsync(string dealNo, AddDealAttachDto dto);
    Task<object?> RemoveAttachmentAsync(string dealNo, long attachId);
    Task<object?> VerifyCtmCareAsync(string dealNo, VerifyDealCtmCareDto? dto);
    Task<object?> DeliverCarAsync(string dealNo, DeliverDealCarDto dto);
    Task<object?> CancelAsync(string dealNo, CancelDealerDealDto dto);
    Task<object?> DeleteDraftAsync(string dealNo);
    Task<object?> AddCarAsync(string dealNo, AddDealerDealCarDto dto);
    Task<object?> RemoveCarAsync(string dealNo, long detailId);
    Task<object?> DetailAsync(string dealNo);
    Task<object> ListAsync(string? status, string? dealer, string? vin, string? customer, string? ctmCare, string? dealNo);
    Task<object> StatsAsync();
}

public sealed class RetailDealService(AppDbContext db, ITenantContext tenant) : IRetailDealService
{
    private Guid Org => tenant.OrgId;

    public async Task<object> CreateAsync(CreateRetailDealDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.DealerCode))
            throw new InvalidOperationException("Mã đại lý bán xe (DealerCode) không được để trống.");
        if (string.IsNullOrWhiteSpace(dto.CustomerCodeBuyer))
            throw new InvalidOperationException("Mã khách hàng chủ sở hữu (CustomerCodeBuyer) không được để trống.");
        if (string.IsNullOrWhiteSpace(dto.BuyerFullName))
            throw new InvalidOperationException("Tên khách hàng chủ sở hữu (BuyerFullName) không được để trống.");
        if (string.IsNullOrWhiteSpace(dto.BuyerIdCardNo))
            throw new InvalidOperationException("Số CMND/CCCD/MST chủ sở hữu (BuyerIdCardNo) không được để trống.");
        if (string.IsNullOrWhiteSpace(dto.SMCode))
            throw new InvalidOperationException("Mã nhân viên tư vấn bán hàng (SMCode) không được để trống.");

        var pmtType = ParsePaymentType(dto.PaymentType);
        if (pmtType == RetailPaymentType.Installment && string.IsNullOrWhiteSpace(dto.BankCode))
            throw new InvalidOperationException("Giao dịch bán xe trả góp bắt buộc phải chọn ngân hàng tài trợ (BankCode).");

        bool flagPdi = dto.FlagPDI ?? true;
        if (!flagPdi && string.IsNullOrWhiteSpace(dto.ReasonNotPDI))
            throw new InvalidOperationException("Khi cờ PDI tắt (FlagPDI = 0), bắt buộc phải nhập lý do không làm PDI (ReasonNotPDI).");

        if (dto.Items is null || dto.Items.Count == 0)
            throw new InvalidOperationException("Chi tiết xe trong giao dịch bán lẻ không được để trống (ít nhất 1 xe).");

        foreach (var item in dto.Items)
        {
            if (string.IsNullOrWhiteSpace(item.CarId) || string.IsNullOrWhiteSpace(item.Vin) || string.IsNullOrWhiteSpace(item.Model))
                throw new InvalidOperationException("Dòng xe bắt buộc phải có CarId, VIN và Model.");
            if (item.Price <= 0)
                throw new InvalidOperationException($"Giá bán của xe VIN {item.Vin} phải lớn hơn 0.");
            if (!string.IsNullOrWhiteSpace(item.CusInvoiceNo) && !item.CusInvoiceDate.HasValue)
                throw new InvalidOperationException($"Xe VIN {item.Vin} có số hóa đơn khách nhưng chưa nhập ngày hóa đơn.");
        }

        string dealNo = string.IsNullOrWhiteSpace(dto.DealNo)
            ? await GenerateDealNoAsync()
            : dto.DealNo.Trim();

        if (await db.RetailDeals.AnyAsync(x => x.OrgId == Org && x.DealNo == dealNo))
            throw new InvalidOperationException($"Số giao dịch '{dealNo}' đã tồn tại trong hệ thống.");

        var ctmType = ParseCustomerType(dto.CustomerType);
        var deal = new RetailDeal
        {
            OrgId = Org,
            DealNo = dealNo,
            DealNoUser = string.IsNullOrWhiteSpace(dto.DealNoUser) ? dealNo : dto.DealNoUser.Trim(),
            DealerCode = dto.DealerCode.Trim(),
            DealerName = string.IsNullOrWhiteSpace(dto.DealerName) ? $"Đại lý {dto.DealerCode}" : dto.DealerName.Trim(),
            DealerCodeBuyer = dto.DealerCodeBuyer?.Trim(),
            DlrContractNo = dto.DlrContractNo?.Trim(),
            SalesType = string.IsNullOrWhiteSpace(dto.SalesType) ? "RETAIL" : dto.SalesType.Trim(),
            CustomerType = ctmType,
            DealDate = dto.DealDate ?? DateTime.Today,
            CustomerCodeBuyer = dto.CustomerCodeBuyer.Trim(),
            BuyerFullName = dto.BuyerFullName.Trim(),
            BuyerPhone = dto.BuyerPhone?.Trim() ?? "",
            BuyerIdCardNo = dto.BuyerIdCardNo.Trim(),
            BuyerAddress = dto.BuyerAddress?.Trim(),
            CustomerCodeHolder = string.IsNullOrWhiteSpace(dto.CustomerCodeHolder) ? dto.CustomerCodeBuyer.Trim() : dto.CustomerCodeHolder.Trim(),
            HolderFullName = string.IsNullOrWhiteSpace(dto.HolderFullName) ? dto.BuyerFullName.Trim() : dto.HolderFullName.Trim(),
            CustomerCodeDriver = string.IsNullOrWhiteSpace(dto.CustomerCodeDriver) ? dto.CustomerCodeBuyer.Trim() : dto.CustomerCodeDriver.Trim(),
            DriverFullName = string.IsNullOrWhiteSpace(dto.DriverFullName) ? dto.BuyerFullName.Trim() : dto.DriverFullName.Trim(),
            SMCode = dto.SMCode.Trim(),
            SMName = dto.SMName?.Trim(),
            PaymentType = pmtType,
            BankCode = dto.BankCode?.Trim(),
            BankName = dto.BankName?.Trim(),
            BankLoanAmount = dto.BankLoanAmount ?? 0m,
            FlagPDI = flagPdi,
            ReasonNotPDI = dto.ReasonNotPDI?.Trim(),
            CtmCareFlag = false,
            Status = RetailDealStatus.Pending,
            TotalCars = dto.Items.Count,
            TotalAmount = dto.Items.Sum(x => x.Price),
            Remark = dto.Remark?.Trim(),
            CreatedBy = "DEALER_SALES",
            CreatedAt = DateTime.Now
        };

        foreach (var it in dto.Items)
        {
            deal.Details.Add(new RetailDealDetail
            {
                DealNo = dealNo,
                CarId = it.CarId.Trim(),
                Vin = it.Vin.Trim(),
                Model = it.Model.Trim(),
                SpecCode = it.SpecCode?.Trim(),
                ColorCode = it.ColorCode?.Trim(),
                Price = it.Price,
                PlateNo = it.PlateNo?.Trim(),
                CusInvoiceNo = it.CusInvoiceNo?.Trim(),
                CusInvoiceDate = it.CusInvoiceDate,
                DeliveryStatus = RetailDealDeliveryStatus.Pending,
                Remark = it.Remark?.Trim()
            });
        }

        if (dto.Attachments is not null)
        {
            foreach (var att in dto.Attachments)
            {
                if (!string.IsNullOrWhiteSpace(att.FileName))
                {
                    deal.Attachments.Add(new RetailDealAttach
                    {
                        DealNo = dealNo,
                        FileType = string.IsNullOrWhiteSpace(att.FileType) ? "BILL" : att.FileType.Trim().ToUpperInvariant(),
                        FileName = att.FileName.Trim(),
                        FilePath = att.FilePath?.Trim() ?? $"/storage/deals/{dealNo}/{att.FileName.Trim()}",
                        Remark = att.Remark?.Trim(),
                        UploadedAt = DateTime.Now,
                        UploadedBy = "DEALER_SALES"
                    });
                }
            }
        }

        db.RetailDeals.Add(deal);
        await db.SaveChangesAsync();

        return new
        {
            message = "Tạo giao dịch bán lẻ ô tô thành công.",
            dealNo = deal.DealNo,
            dealNoUser = deal.DealNoUser,
            dealerCode = deal.DealerCode,
            buyer = deal.BuyerFullName,
            totalCars = deal.TotalCars,
            totalAmount = deal.TotalAmount,
            status = deal.Status.ToString(),
            ctmCareFlag = deal.CtmCareFlag,
            flagPDI = deal.FlagPDI
        };
    }

    public async Task<object?> UpdateAsync(string dealNo, UpdateRetailDealDto dto)
    {
        var deal = await db.RetailDeals
            .Include(x => x.Details)
            .FirstOrDefaultAsync(x => x.OrgId == Org && x.DealNo == dealNo);

        if (deal is null) return null;

        if (deal.Status == RetailDealStatus.Cancelled)
            throw new InvalidOperationException($"Giao dịch '{dealNo}' đã bị hủy, không được phép chỉnh sửa.");

        if (deal.CtmCareFlag)
            throw new InvalidOperationException($"Giao dịch '{dealNo}' đã được kiểm chứng CSKH (CtmCareFlag = 1), đã khóa không cho phép sửa thông tin.");

        if (!string.IsNullOrWhiteSpace(dto.DealNoUser)) deal.DealNoUser = dto.DealNoUser.Trim();
        if (!string.IsNullOrWhiteSpace(dto.SalesType)) deal.SalesType = dto.SalesType.Trim();
        if (!string.IsNullOrWhiteSpace(dto.CustomerType)) deal.CustomerType = ParseCustomerType(dto.CustomerType);
        if (!string.IsNullOrWhiteSpace(dto.BuyerFullName)) deal.BuyerFullName = dto.BuyerFullName.Trim();
        if (!string.IsNullOrWhiteSpace(dto.BuyerPhone)) deal.BuyerPhone = dto.BuyerPhone.Trim();
        if (dto.BuyerAddress is not null) deal.BuyerAddress = dto.BuyerAddress.Trim();
        if (!string.IsNullOrWhiteSpace(dto.CustomerCodeHolder)) deal.CustomerCodeHolder = dto.CustomerCodeHolder.Trim();
        if (!string.IsNullOrWhiteSpace(dto.HolderFullName)) deal.HolderFullName = dto.HolderFullName.Trim();
        if (!string.IsNullOrWhiteSpace(dto.CustomerCodeDriver)) deal.CustomerCodeDriver = dto.CustomerCodeDriver.Trim();
        if (!string.IsNullOrWhiteSpace(dto.DriverFullName)) deal.DriverFullName = dto.DriverFullName.Trim();
        if (!string.IsNullOrWhiteSpace(dto.SMCode)) deal.SMCode = dto.SMCode.Trim();
        if (dto.SMName is not null) deal.SMName = dto.SMName.Trim();

        if (!string.IsNullOrWhiteSpace(dto.PaymentType))
        {
            deal.PaymentType = ParsePaymentType(dto.PaymentType);
            if (deal.PaymentType == RetailPaymentType.Installment)
            {
                if (!string.IsNullOrWhiteSpace(dto.BankCode)) deal.BankCode = dto.BankCode.Trim();
                if (string.IsNullOrWhiteSpace(deal.BankCode))
                    throw new InvalidOperationException("Giao dịch trả góp bắt buộc phải chọn ngân hàng tài trợ.");
                if (dto.BankName is not null) deal.BankName = dto.BankName.Trim();
                if (dto.BankLoanAmount.HasValue) deal.BankLoanAmount = dto.BankLoanAmount.Value;
            }
        }

        if (dto.FlagPDI.HasValue)
        {
            deal.FlagPDI = dto.FlagPDI.Value;
            if (!deal.FlagPDI && string.IsNullOrWhiteSpace(dto.ReasonNotPDI))
                throw new InvalidOperationException("Bắt buộc phải nhập lý do không làm PDI (ReasonNotPDI).");
            deal.ReasonNotPDI = dto.ReasonNotPDI?.Trim();
        }

        if (dto.Remark is not null) deal.Remark = dto.Remark.Trim();

        if (dto.Items is not null && dto.Items.Count > 0)
        {
            deal.Details.Clear();
            foreach (var item in dto.Items)
            {
                if (string.IsNullOrWhiteSpace(item.CarId) || string.IsNullOrWhiteSpace(item.Vin) || string.IsNullOrWhiteSpace(item.Model))
                    throw new InvalidOperationException("Dòng xe bắt buộc phải có CarId, VIN và Model.");
                if (item.Price <= 0)
                    throw new InvalidOperationException($"Giá bán của xe VIN {item.Vin} phải lớn hơn 0.");

                deal.Details.Add(new RetailDealDetail
                {
                    DealNo = dealNo,
                    CarId = item.CarId.Trim(),
                    Vin = item.Vin.Trim(),
                    Model = item.Model.Trim(),
                    SpecCode = item.SpecCode?.Trim(),
                    ColorCode = item.ColorCode?.Trim(),
                    Price = item.Price,
                    PlateNo = item.PlateNo?.Trim(),
                    CusInvoiceNo = item.CusInvoiceNo?.Trim(),
                    CusInvoiceDate = item.CusInvoiceDate,
                    DeliveryStatus = RetailDealDeliveryStatus.Pending,
                    Remark = item.Remark?.Trim()
                });
            }
            deal.TotalCars = deal.Details.Count;
            deal.TotalAmount = deal.Details.Sum(x => x.Price);
        }

        await db.SaveChangesAsync();

        return new
        {
            message = "Cập nhật thông tin giao dịch bán lẻ thành công.",
            dealNo = deal.DealNo,
            buyer = deal.BuyerFullName,
            totalCars = deal.TotalCars,
            totalAmount = deal.TotalAmount,
            status = deal.Status.ToString()
        };
    }

    public async Task<object?> UpdatePlateNoAsync(string dealNo, UpdateDealPlateNoDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.PlateNo))
            throw new InvalidOperationException("Biển số xe (PlateNo) không được để trống.");

        var deal = await db.RetailDeals
            .Include(x => x.Details)
            .FirstOrDefaultAsync(x => x.OrgId == Org && x.DealNo == dealNo);

        if (deal is null) return null;

        var detail = deal.Details.FirstOrDefault(d =>
            (!string.IsNullOrWhiteSpace(dto.CarId) && d.CarId == dto.CarId.Trim()) ||
            (!string.IsNullOrWhiteSpace(dto.Vin) && d.Vin == dto.Vin.Trim())
        ) ?? deal.Details.FirstOrDefault();

        if (detail is null)
            throw new InvalidOperationException($"Không tìm thấy xe hợp lệ trong giao dịch '{dealNo}'.");

        detail.PlateNo = dto.PlateNo.Trim().ToUpperInvariant();
        await db.SaveChangesAsync();

        return new
        {
            message = "Cập nhật biển số xe thành công.",
            dealNo = deal.DealNo,
            carId = detail.CarId,
            vin = detail.Vin,
            plateNo = detail.PlateNo
        };
    }

    public async Task<object?> UpdateCusInvoiceAsync(string dealNo, UpdateDealCusInvoiceDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.CusInvoiceNo))
            throw new InvalidOperationException("Số hóa đơn xuất cho khách hàng (CusInvoiceNo) không được để trống.");

        var deal = await db.RetailDeals
            .Include(x => x.Details)
            .FirstOrDefaultAsync(x => x.OrgId == Org && x.DealNo == dealNo);

        if (deal is null) return null;

        var detail = deal.Details.FirstOrDefault(d =>
            (!string.IsNullOrWhiteSpace(dto.CarId) && d.CarId == dto.CarId.Trim()) ||
            (!string.IsNullOrWhiteSpace(dto.Vin) && d.Vin == dto.Vin.Trim())
        ) ?? deal.Details.FirstOrDefault();

        if (detail is null)
            throw new InvalidOperationException($"Không tìm thấy xe hợp lệ trong giao dịch '{dealNo}'.");

        detail.CusInvoiceNo = dto.CusInvoiceNo.Trim();
        detail.CusInvoiceDate = dto.CusInvoiceDate;
        await db.SaveChangesAsync();

        return new
        {
            message = "Cập nhật hóa đơn bán lẻ xuất cho khách hàng thành công.",
            dealNo = deal.DealNo,
            carId = detail.CarId,
            vin = detail.Vin,
            cusInvoiceNo = detail.CusInvoiceNo,
            cusInvoiceDate = detail.CusInvoiceDate?.ToString("yyyy-MM-dd")
        };
    }

    public async Task<object?> AddAttachmentAsync(string dealNo, AddDealAttachDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.FileName))
            throw new InvalidOperationException("Tên file đính kèm (FileName) không được để trống.");

        var deal = await db.RetailDeals
            .Include(x => x.Attachments)
            .FirstOrDefaultAsync(x => x.OrgId == Org && x.DealNo == dealNo);

        if (deal is null) return null;

        var attach = new RetailDealAttach
        {
            DealNo = dealNo,
            FileType = string.IsNullOrWhiteSpace(dto.FileType) ? "BILL" : dto.FileType.Trim().ToUpperInvariant(),
            FileName = dto.FileName.Trim(),
            FilePath = dto.FilePath?.Trim() ?? $"/storage/deals/{dealNo}/{dto.FileName.Trim()}",
            Remark = dto.Remark?.Trim(),
            UploadedAt = DateTime.Now,
            UploadedBy = "USER"
        };

        deal.Attachments.Add(attach);
        await db.SaveChangesAsync();

        return new
        {
            message = "Thêm chứng từ đính kèm thành công.",
            dealNo = deal.DealNo,
            attachId = attach.Id,
            fileType = attach.FileType,
            fileName = attach.FileName,
            filePath = attach.FilePath
        };
    }

    public async Task<object?> RemoveAttachmentAsync(string dealNo, long attachId)
    {
        var deal = await db.RetailDeals
            .Include(x => x.Attachments)
            .FirstOrDefaultAsync(x => x.OrgId == Org && x.DealNo == dealNo);

        if (deal is null) return null;

        var attach = deal.Attachments.FirstOrDefault(a => a.Id == attachId);
        if (attach is null) return null;

        deal.Attachments.Remove(attach);
        await db.SaveChangesAsync();

        return new
        {
            message = "Xóa chứng từ đính kèm thành công.",
            dealNo = deal.DealNo,
            attachId
        };
    }

    public async Task<object?> VerifyCtmCareAsync(string dealNo, VerifyDealCtmCareDto? dto)
    {
        var deal = await db.RetailDeals.FirstOrDefaultAsync(x => x.OrgId == Org && x.DealNo == dealNo);
        if (deal is null) return null;

        if (deal.Status == RetailDealStatus.Cancelled)
            throw new InvalidOperationException($"Giao dịch '{dealNo}' đã bị hủy, không thể kiểm chứng.");

        deal.CtmCareFlag = true;
        deal.CtmCareUpdDate = DateTime.Now;
        deal.CtmCareUpdBy = string.IsNullOrWhiteSpace(dto?.VerifiedBy) ? "NPP_CSKH_AUDITOR" : dto.VerifiedBy.Trim();

        if (deal.Status == RetailDealStatus.Pending)
            deal.Status = RetailDealStatus.Verified;

        if (!string.IsNullOrWhiteSpace(dto?.Note))
            deal.Remark = string.IsNullOrWhiteSpace(deal.Remark) ? dto.Note : $"{deal.Remark} | CSKH: {dto.Note}";

        await db.SaveChangesAsync();

        return new
        {
            message = "Kiểm chứng giao dịch bán lẻ CSKH thành công (khóa sửa giao dịch).",
            dealNo = deal.DealNo,
            ctmCareFlag = deal.CtmCareFlag,
            ctmCareUpdDate = deal.CtmCareUpdDate,
            ctmCareUpdBy = deal.CtmCareUpdBy,
            status = deal.Status.ToString()
        };
    }

    public async Task<object?> DeliverCarAsync(string dealNo, DeliverDealCarDto dto)
    {
        var deal = await db.RetailDeals
            .Include(x => x.Details)
            .FirstOrDefaultAsync(x => x.OrgId == Org && x.DealNo == dealNo);

        if (deal is null) return null;

        if (deal.Status == RetailDealStatus.Cancelled)
            throw new InvalidOperationException($"Giao dịch '{dealNo}' đã bị hủy, không thể giao xe.");

        var targetDetails = deal.Details
            .Where(d =>
                (string.IsNullOrWhiteSpace(dto.CarId) || d.CarId == dto.CarId.Trim()) &&
                (string.IsNullOrWhiteSpace(dto.Vin) || d.Vin == dto.Vin.Trim())
            ).ToList();

        if (targetDetails.Count == 0)
            targetDetails = deal.Details.Where(d => d.DeliveryStatus == RetailDealDeliveryStatus.Pending).ToList();

        if (targetDetails.Count == 0)
            throw new InvalidOperationException($"Tất cả xe trong giao dịch '{dealNo}' đã được bàn giao trước đó.");

        var dlvDate = dto.DeliveryDate ?? DateTime.Now;
        foreach (var det in targetDetails)
        {
            det.DeliveryStatus = RetailDealDeliveryStatus.Delivered;
            det.DeliveryDate = dlvDate;
            if (!string.IsNullOrWhiteSpace(dto.Remark))
                det.Remark = string.IsNullOrWhiteSpace(det.Remark) ? dto.Remark : $"{det.Remark} | {dto.Remark}";
        }

        bool allDelivered = deal.Details.All(d => d.DeliveryStatus == RetailDealDeliveryStatus.Delivered);
        if (allDelivered)
            deal.Status = RetailDealStatus.Delivered;

        // Nếu giao dịch liên kết với Hợp đồng bán lẻ (DlrContractNo), kiểm tra và cập nhật tiến độ hợp đồng
        if (!string.IsNullOrWhiteSpace(deal.DlrContractNo))
        {
            var contract = await db.RetailContracts
                .Include(c => c.Cars)
                .Include(c => c.Details)
                .FirstOrDefaultAsync(c => c.OrgId == Org && c.ContractNo == deal.DlrContractNo);

            if (contract is not null)
            {
                foreach (var det in targetDetails)
                {
                    var cCar = contract.Cars.FirstOrDefault(c => c.Vin == det.Vin || c.Model == det.Model);
                    if (cCar is not null)
                    {
                        cCar.Status = RetailContractCarStatus.Delivered;
                        cCar.DeliveryDate = dlvDate;
                    }
                }

                if (contract.Cars.All(c => c.Status == RetailContractCarStatus.Delivered))
                {
                    contract.FlagDealFinish = true;
                    contract.Status = RetailContractStatus.Finished;
                }
            }
        }

        await db.SaveChangesAsync();

        return new
        {
            message = "Xác nhận bàn giao xe thành công.",
            dealNo = deal.DealNo,
            deliveredCount = targetDetails.Count,
            allDelivered,
            status = deal.Status.ToString()
        };
    }

    public async Task<object?> CancelAsync(string dealNo, CancelDealerDealDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Reason))
            throw new InvalidOperationException("Lý do hủy giao dịch bán lẻ (Reason) không được để trống.");

        var deal = await db.RetailDeals
            .Include(x => x.Details)
            .FirstOrDefaultAsync(x => x.OrgId == Org && x.DealNo == dealNo);

        if (deal is null) return null;

        if (deal.Status == RetailDealStatus.Cancelled)
            throw new InvalidOperationException($"Giao dịch '{dealNo}' đã bị hủy trước đó.");

        if (deal.Details.Any(d => d.DeliveryStatus == RetailDealDeliveryStatus.Delivered))
            throw new InvalidOperationException($"Giao dịch '{dealNo}' đã có xe bàn giao thực tế cho khách hàng, không được phép hủy.");

        deal.Status = RetailDealStatus.Cancelled;
        deal.CancelledBy = string.IsNullOrWhiteSpace(dto.CancelledBy) ? "DEALER_MANAGER" : dto.CancelledBy.Trim();
        deal.CancelledAt = DateTime.Now;
        deal.CancelReason = dto.Reason.Trim();

        await db.SaveChangesAsync();

        return new
        {
            message = "Hủy giao dịch bán lẻ ô tô thành công.",
            dealNo = deal.DealNo,
            status = deal.Status.ToString(),
            cancelledAt = deal.CancelledAt,
            cancelReason = deal.CancelReason
        };
    }

    public async Task<object?> DeleteDraftAsync(string dealNo)
    {
        var deal = await db.RetailDeals
            .Include(x => x.Details)
            .Include(x => x.Attachments)
            .FirstOrDefaultAsync(x => x.OrgId == Org && x.DealNo == dealNo);

        if (deal is null) return null;

        if (deal.Status != RetailDealStatus.Pending)
            throw new InvalidOperationException($"Chỉ được xóa giao dịch ở trạng thái chờ (Pending). Giao dịch hiện tại là '{deal.Status}'.");

        if (deal.CtmCareFlag)
            throw new InvalidOperationException($"Giao dịch '{dealNo}' đã được kiểm chứng CSKH, không được phép xóa.");

        if (deal.Details.Any(d => d.DeliveryStatus == RetailDealDeliveryStatus.Delivered))
            throw new InvalidOperationException($"Giao dịch '{dealNo}' đã có xe bàn giao, không được phép xóa.");

        db.RetailDeals.Remove(deal);
        await db.SaveChangesAsync();

        return new
        {
            message = "Đã xóa giao dịch bán lẻ thành công.",
            dealNo
        };
    }

    public async Task<object?> AddCarAsync(string dealNo, AddDealerDealCarDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.CarId) || string.IsNullOrWhiteSpace(dto.Vin) || string.IsNullOrWhiteSpace(dto.Model))
            throw new InvalidOperationException("Dòng xe bắt buộc phải có CarId, VIN và Model.");
        if (dto.Price <= 0)
            throw new InvalidOperationException("Giá bán xe phải lớn hơn 0.");

        var deal = await db.RetailDeals
            .Include(x => x.Details)
            .FirstOrDefaultAsync(x => x.OrgId == Org && x.DealNo == dealNo);

        if (deal is null) return null;

        if (deal.Status != RetailDealStatus.Pending || deal.CtmCareFlag)
            throw new InvalidOperationException($"Giao dịch '{dealNo}' không ở trạng thái Pending hoặc đã bị khóa kiểm chứng, không thể thêm xe.");

        var detail = new RetailDealDetail
        {
            DealNo = dealNo,
            CarId = dto.CarId.Trim(),
            Vin = dto.Vin.Trim(),
            Model = dto.Model.Trim(),
            SpecCode = dto.SpecCode?.Trim(),
            ColorCode = dto.ColorCode?.Trim(),
            Price = dto.Price,
            PlateNo = dto.PlateNo?.Trim(),
            CusInvoiceNo = dto.CusInvoiceNo?.Trim(),
            CusInvoiceDate = dto.CusInvoiceDate,
            DeliveryStatus = RetailDealDeliveryStatus.Pending,
            Remark = dto.Remark?.Trim()
        };

        deal.Details.Add(detail);
        deal.TotalCars = deal.Details.Count;
        deal.TotalAmount = deal.Details.Sum(x => x.Price);

        await db.SaveChangesAsync();

        return new
        {
            message = "Thêm xe vào giao dịch bán lẻ thành công.",
            dealNo = deal.DealNo,
            carId = detail.CarId,
            vin = detail.Vin,
            totalCars = deal.TotalCars,
            totalAmount = deal.TotalAmount
        };
    }

    public async Task<object?> RemoveCarAsync(string dealNo, long detailId)
    {
        var deal = await db.RetailDeals
            .Include(x => x.Details)
            .FirstOrDefaultAsync(x => x.OrgId == Org && x.DealNo == dealNo);

        if (deal is null) return null;

        if (deal.Status != RetailDealStatus.Pending || deal.CtmCareFlag)
            throw new InvalidOperationException($"Giao dịch '{dealNo}' không ở trạng thái Pending hoặc đã bị khóa kiểm chứng, không thể xóa xe.");

        if (deal.Details.Count <= 1)
            throw new InvalidOperationException("Giao dịch bán lẻ phải có ít nhất 1 xe, không thể xóa dòng cuối cùng.");

        var detail = deal.Details.FirstOrDefault(d => d.Id == detailId);
        if (detail is null) return null;

        deal.Details.Remove(detail);
        deal.TotalCars = deal.Details.Count;
        deal.TotalAmount = deal.Details.Sum(x => x.Price);

        await db.SaveChangesAsync();

        return new
        {
            message = "Gỡ xe khỏi giao dịch bán lẻ thành công.",
            dealNo = deal.DealNo,
            detailId,
            totalCars = deal.TotalCars,
            totalAmount = deal.TotalAmount
        };
    }

    public async Task<object?> DetailAsync(string dealNo)
    {
        var deal = await db.RetailDeals
            .Include(x => x.Details)
            .Include(x => x.Attachments)
            .FirstOrDefaultAsync(x => x.OrgId == Org && x.DealNo == dealNo);

        if (deal is null) return null;

        return new
        {
            deal.Id,
            deal.DealNo,
            deal.DealNoUser,
            deal.DealerCode,
            deal.DealerName,
            deal.DealerCodeBuyer,
            deal.DlrContractNo,
            deal.SalesType,
            customerType = deal.CustomerType.ToString(),
            dealDate = deal.DealDate.ToString("yyyy-MM-dd"),
            deal.CustomerCodeBuyer,
            deal.BuyerFullName,
            deal.BuyerPhone,
            deal.BuyerIdCardNo,
            deal.BuyerAddress,
            deal.CustomerCodeHolder,
            deal.HolderFullName,
            deal.CustomerCodeDriver,
            deal.DriverFullName,
            deal.SMCode,
            deal.SMName,
            paymentType = deal.PaymentType.ToString(),
            deal.BankCode,
            deal.BankName,
            deal.BankLoanAmount,
            deal.FlagPDI,
            deal.ReasonNotPDI,
            deal.CtmCareFlag,
            ctmCareUpdDate = deal.CtmCareUpdDate?.ToString("yyyy-MM-dd HH:mm:ss"),
            deal.CtmCareUpdBy,
            status = deal.Status.ToString(),
            deal.TotalCars,
            deal.TotalAmount,
            deal.Remark,
            deal.CreatedBy,
            createdAt = deal.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss"),
            deal.CancelledBy,
            cancelledAt = deal.CancelledAt?.ToString("yyyy-MM-dd HH:mm:ss"),
            deal.CancelReason,
            details = deal.Details.Select(d => new
            {
                d.Id,
                d.DealNo,
                d.CarId,
                d.Vin,
                d.Model,
                d.SpecCode,
                d.ColorCode,
                d.Price,
                d.PlateNo,
                d.CusInvoiceNo,
                cusInvoiceDate = d.CusInvoiceDate?.ToString("yyyy-MM-dd"),
                deliveryStatus = d.DeliveryStatus.ToString(),
                deliveryDate = d.DeliveryDate?.ToString("yyyy-MM-dd HH:mm:ss"),
                d.Remark
            }),
            attachments = deal.Attachments.Select(a => new
            {
                a.Id,
                a.DealNo,
                a.FileType,
                a.FileName,
                a.FilePath,
                a.Remark,
                uploadedAt = a.UploadedAt.ToString("yyyy-MM-dd HH:mm:ss"),
                a.UploadedBy
            })
        };
    }

    public async Task<object> ListAsync(string? status, string? dealer, string? vin, string? customer, string? ctmCare, string? dealNo)
    {
        var q = db.RetailDeals
            .Include(x => x.Details)
            .Include(x => x.Attachments)
            .Where(x => x.OrgId == Org);

        if (!string.IsNullOrWhiteSpace(dealer))
            q = q.Where(x => x.DealerCode == dealer.Trim() || x.DealerName.Contains(dealer.Trim()));

        if (!string.IsNullOrWhiteSpace(dealNo))
            q = q.Where(x => x.DealNo == dealNo.Trim() || x.DealNoUser.Contains(dealNo.Trim()));

        if (!string.IsNullOrWhiteSpace(customer))
            q = q.Where(x => x.BuyerFullName.Contains(customer.Trim()) || x.BuyerPhone.Contains(customer.Trim()) || x.BuyerIdCardNo.Contains(customer.Trim()));

        if (!string.IsNullOrWhiteSpace(vin))
            q = q.Where(x => x.Details.Any(d => d.Vin.Contains(vin.Trim()) || d.CarId.Contains(vin.Trim())));

        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<RetailDealStatus>(status, true, out var st))
            q = q.Where(x => x.Status == st);

        if (!string.IsNullOrWhiteSpace(ctmCare))
        {
            bool isCare = ctmCare == "1" || ctmCare.Equals("true", StringComparison.OrdinalIgnoreCase);
            q = q.Where(x => x.CtmCareFlag == isCare);
        }

        var list = await q.OrderByDescending(x => x.CreatedAt).ToListAsync();

        return list.Select(deal => new
        {
            deal.Id,
            deal.DealNo,
            deal.DealNoUser,
            deal.DealerCode,
            deal.DealerName,
            deal.DlrContractNo,
            deal.SalesType,
            customerType = deal.CustomerType.ToString(),
            dealDate = deal.DealDate.ToString("yyyy-MM-dd"),
            deal.BuyerFullName,
            deal.BuyerPhone,
            deal.BuyerIdCardNo,
            deal.SMCode,
            deal.SMName,
            paymentType = deal.PaymentType.ToString(),
            deal.BankCode,
            deal.BankName,
            deal.FlagPDI,
            deal.CtmCareFlag,
            ctmCareUpdDate = deal.CtmCareUpdDate?.ToString("yyyy-MM-dd HH:mm:ss"),
            deal.CtmCareUpdBy,
            status = deal.Status.ToString(),
            deal.TotalCars,
            deal.TotalAmount,
            createdAt = deal.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss"),
            cars = deal.Details.Select(d => new
            {
                d.Id,
                d.CarId,
                d.Vin,
                d.Model,
                d.Price,
                d.PlateNo,
                d.CusInvoiceNo,
                cusInvoiceDate = d.CusInvoiceDate?.ToString("yyyy-MM-dd"),
                deliveryStatus = d.DeliveryStatus.ToString()
            }),
            attachmentCount = deal.Attachments.Count
        });
    }

    public async Task<object> StatsAsync()
    {
        var deals = await db.RetailDeals.Where(x => x.OrgId == Org).ToListAsync();

        return new
        {
            totalDeals = deals.Count,
            pendingCount = deals.Count(x => x.Status == RetailDealStatus.Pending),
            verifiedCount = deals.Count(x => x.Status == RetailDealStatus.Verified),
            deliveredCount = deals.Count(x => x.Status == RetailDealStatus.Delivered),
            cancelledCount = deals.Count(x => x.Status == RetailDealStatus.Cancelled),
            ctmCareVerifiedCount = deals.Count(x => x.CtmCareFlag),
            cashCount = deals.Count(x => x.PaymentType == RetailPaymentType.Cash),
            installmentCount = deals.Count(x => x.PaymentType == RetailPaymentType.Installment),
            totalCars = deals.Sum(x => x.TotalCars),
            totalAmount = deals.Sum(x => x.TotalAmount)
        };
    }

    private async Task<string> GenerateDealNoAsync()
    {
        string prefix = $"DEAL{DateTime.Today:yyMM}";
        int count = await db.RetailDeals.CountAsync(x => x.OrgId == Org && x.DealNo.StartsWith(prefix));
        return $"{prefix}{(count + 1):D4}";
    }

    private static RetailPaymentType ParsePaymentType(string? pmt)
    {
        if (string.IsNullOrWhiteSpace(pmt)) return RetailPaymentType.Cash;
        if (pmt == "2" || pmt.Equals("Installment", StringComparison.OrdinalIgnoreCase) || pmt.Equals("TraGop", StringComparison.OrdinalIgnoreCase))
            return RetailPaymentType.Installment;
        return RetailPaymentType.Cash;
    }

    private static RetailCustomerType ParseCustomerType(string? ctm)
    {
        if (string.IsNullOrWhiteSpace(ctm)) return RetailCustomerType.Individual;
        if (ctm == "1" || ctm.Equals("Corporate", StringComparison.OrdinalIgnoreCase) || ctm.Equals("DoanhNghiep", StringComparison.OrdinalIgnoreCase))
            return RetailCustomerType.Corporate;
        return RetailCustomerType.Individual;
    }
}
