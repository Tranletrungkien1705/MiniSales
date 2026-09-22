using Microsoft.EntityFrameworkCore;
using MiniSales.Data;
using MiniSales.Models;

namespace MiniSales.Services;

public record CreateRetailContractCarDto(
    string Model,
    string? SpecCode,
    string? ColorCode,
    int? ProductionYear,
    int Qty,
    decimal UnitPrice,
    decimal? Discount,
    DateTime? DlvExpectedDate,
    string? Remark
);

public record CreateRetailContractDto(
    string? ContractNo,
    string? ContractNoUser,
    string DealerCode,
    string? DealerName,
    string? DealerCodeBuyer,
    string CustomerCode,
    string CustomerName,
    string CustomerPhone,
    string? CustomerIdCardType,
    string CustomerIdCardNo,
    DateTime? CustomerDateOfBirth,
    string? CustomerAddress,
    string? CustomerType,
    string? TransactorCode,
    string? TransactorName,
    string? TransactorPhone,
    string? DriverCode,
    string? DealerSaleCode,
    string? SalesType,
    string SMCode,
    string? SMName,
    DateTime? ContractDate,
    string? PaymentType,
    string? BankCode,
    string? BankName,
    decimal? BankLoanAmount,
    decimal? DepositAmount,
    string? Remark,
    List<CreateRetailContractCarDto>? Items
);

public record UpdateRetailContractDto(
    string? ContractNoUser,
    string? CustomerName,
    string? CustomerPhone,
    string? CustomerAddress,
    string? TransactorCode,
    string? TransactorName,
    string? TransactorPhone,
    string? DriverCode,
    string? DealerSaleCode,
    string? SalesType,
    string? SMCode,
    string? SMName,
    string? PaymentType,
    string? BankCode,
    string? BankName,
    decimal? BankLoanAmount,
    decimal? DepositAmount,
    decimal? PaidAmount,
    string? Remark,
    List<CreateRetailContractCarDto>? Items
);

public record UpdateRetailContractBankDto(
    string BankCode,
    string? BankName,
    decimal? BankLoanAmount
);

public record UpdateRetailContractSMDto(
    string SMCode,
    string? SMName
);

public record ApproveRetailContractDto(
    string? ApprovedBy,
    string? Note
);

public record CancelRetailContractDto(
    string Reason,
    string? CancelledBy
);

public record AssignRetailCarVinDto(
    string? CtrCarId,
    string? Model,
    string? SpecCode,
    string? ColorCode,
    string Vin
);

public record DeliverRetailCarDto(
    string CtrCarId,
    DateTime? DeliveryDate,
    string? Remark
);

public record BatchApproveRetailContractDto(
    List<string> ContractNos,
    string? ApprovedBy
);

public record BatchCancelRetailContractDto(
    List<string> ContractNos,
    string Reason,
    string? CancelledBy
);

public interface IRetailContractService
{
    Task<object> CreateAsync(CreateRetailContractDto dto);
    Task<object?> UpdateAsync(string contractNo, UpdateRetailContractDto dto);
    Task<object?> UpdateBankAsync(string contractNo, UpdateRetailContractBankDto dto);
    Task<object?> UpdateSalesmanAsync(string contractNo, UpdateRetailContractSMDto dto);
    Task<object> ListAsync(string? status, string? dealer, string? customer, string? paymentType, string? contractNo);
    Task<object?> DetailAsync(string contractNo);
    Task<object?> ApproveAsync(string contractNo, ApproveRetailContractDto? dto);
    Task<object?> CancelAsync(string contractNo, CancelRetailContractDto dto);
    Task<object?> DeleteDraftAsync(string contractNo);
    Task<object?> AssignVinAsync(string contractNo, AssignRetailCarVinDto dto);
    Task<object?> DeliverCarAsync(string contractNo, DeliverRetailCarDto dto);
    Task<object> ApproveMultiAsync(BatchApproveRetailContractDto dto);
    Task<object> CancelMultiAsync(BatchCancelRetailContractDto dto);
    Task<object> StatsAsync();
}

public sealed class RetailContractService(AppDbContext db, ITenantContext tenant) : IRetailContractService
{
    private Guid Org => tenant.OrgId;

    public async Task<object> CreateAsync(CreateRetailContractDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.DealerCode))
            throw new InvalidOperationException("Mã đại lý (DealerCode) không được để trống.");
        if (string.IsNullOrWhiteSpace(dto.CustomerCode))
            throw new InvalidOperationException("Mã khách hàng (CustomerCode) không được để trống.");
        if (string.IsNullOrWhiteSpace(dto.CustomerName))
            throw new InvalidOperationException("Tên khách hàng (CustomerName) không được để trống.");
        if (string.IsNullOrWhiteSpace(dto.CustomerIdCardNo))
            throw new InvalidOperationException("Số CMND/CCCD/MST (CustomerIdCardNo) của khách hàng không được để trống.");
        if (string.IsNullOrWhiteSpace(dto.SMCode))
            throw new InvalidOperationException("Mã tư vấn bán hàng (SMCode) không được để trống.");

        var pmtType = ParsePaymentType(dto.PaymentType);
        if (pmtType == RetailPaymentType.Installment && string.IsNullOrWhiteSpace(dto.BankCode))
            throw new InvalidOperationException("Hợp đồng mua xe trả góp (Installment) bắt buộc phải có thông tin ngân hàng tài trợ (BankCode).");

        if (dto.Items is null || dto.Items.Count == 0)
            throw new InvalidOperationException("Chi tiết dòng xe trong hợp đồng bán lẻ không được để trống (TableDetailBeBlank).");

        var contractDate = dto.ContractDate ?? DateTime.Today;
        if (contractDate > DateTime.Today.AddDays(1))
            throw new InvalidOperationException("Ngày hợp đồng không thể vượt quá ngày hiện tại.");

        string contractNo;
        if (!string.IsNullOrWhiteSpace(dto.ContractNo))
        {
            contractNo = dto.ContractNo.Trim().ToUpperInvariant();
            if (await db.RetailContracts.AnyAsync(x => x.OrgId == Org && x.ContractNo == contractNo))
                throw new InvalidOperationException($"Số hợp đồng bán lẻ '{contractNo}' đã tồn tại trong hệ thống.");
        }
        else
        {
            var datePart = DateTime.Today.ToString("yyMMdd");
            var countToday = await db.RetailContracts.CountAsync(x => x.OrgId == Org && x.ContractNo.StartsWith($"RC{datePart}"));
            contractNo = $"RC{datePart}{(countToday + 1):D4}";
        }

        var custType = dto.CustomerType?.Trim().ToLowerInvariant() switch
        {
            "corporate" or "company" or "c" or "1" => RetailCustomerType.Corporate,
            _ => RetailCustomerType.Individual
        };

        var contract = new DlrRetailContract
        {
            OrgId = Org,
            ContractNo = contractNo,
            ContractNoUser = string.IsNullOrWhiteSpace(dto.ContractNoUser) ? contractNo : dto.ContractNoUser.Trim(),
            DealerCode = dto.DealerCode.Trim().ToUpperInvariant(),
            DealerName = string.IsNullOrWhiteSpace(dto.DealerName) ? dto.DealerCode.Trim().ToUpperInvariant() : dto.DealerName.Trim(),
            DealerCodeBuyer = dto.DealerCodeBuyer?.Trim().ToUpperInvariant(),
            CustomerCode = dto.CustomerCode.Trim(),
            CustomerName = dto.CustomerName.Trim(),
            CustomerPhone = dto.CustomerPhone?.Trim() ?? "",
            CustomerIdCardType = string.IsNullOrWhiteSpace(dto.CustomerIdCardType) ? "CCCD" : dto.CustomerIdCardType.Trim().ToUpperInvariant(),
            CustomerIdCardNo = dto.CustomerIdCardNo.Trim(),
            CustomerDateOfBirth = dto.CustomerDateOfBirth,
            CustomerAddress = dto.CustomerAddress?.Trim(),
            CustomerType = custType,
            TransactorCode = dto.TransactorCode?.Trim(),
            TransactorName = dto.TransactorName?.Trim(),
            TransactorPhone = dto.TransactorPhone?.Trim(),
            DriverCode = dto.DriverCode?.Trim(),
            DealerSaleCode = dto.DealerSaleCode?.Trim(),
            SalesType = string.IsNullOrWhiteSpace(dto.SalesType) ? "RETAIL" : dto.SalesType.Trim().ToUpperInvariant(),
            SMCode = dto.SMCode.Trim().ToUpperInvariant(),
            SMName = dto.SMName?.Trim(),
            ContractDate = contractDate,
            PaymentType = pmtType,
            BankCode = pmtType == RetailPaymentType.Installment ? dto.BankCode?.Trim().ToUpperInvariant() : null,
            BankName = pmtType == RetailPaymentType.Installment ? dto.BankName?.Trim() : null,
            BankLoanAmount = dto.BankLoanAmount ?? 0m,
            DepositAmount = dto.DepositAmount ?? 0m,
            PaidAmount = dto.DepositAmount ?? 0m,
            Status = RetailContractStatus.Pending,
            FlagDealFinish = false,
            VersionCount = 1,
            VersionDTimeCurr = DateTime.Now,
            Remark = dto.Remark?.Trim(),
            CreatedBy = "DEALER_USER",
            CreatedAt = DateTime.Now
        };

        int carSeq = 1;
        int totalCars = 0;
        decimal totalAmount = 0m;

        foreach (var item in dto.Items)
        {
            if (string.IsNullOrWhiteSpace(item.Model))
                throw new InvalidOperationException("Tên dòng xe (Model) không được để trống.");
            if (item.Qty <= 0)
                throw new InvalidOperationException($"Số lượng xe của model '{item.Model}' phải lớn hơn 0.");
            if (item.UnitPrice <= 0)
                throw new InvalidOperationException($"Đơn giá xe của model '{item.Model}' phải lớn hơn 0.");

            var discount = item.Discount ?? 0m;
            var lineTotal = (item.Qty * item.UnitPrice) - discount;
            if (lineTotal < 0) lineTotal = 0;

            var detail = new DlrRetailContractDetail
            {
                ContractNo = contractNo,
                Model = item.Model.Trim(),
                SpecCode = item.SpecCode?.Trim().ToUpperInvariant(),
                ColorCode = item.ColorCode?.Trim().ToUpperInvariant(),
                ProductionYear = item.ProductionYear ?? DateTime.Today.Year,
                Qty = item.Qty,
                QtyDelivery = 0,
                QtyCancel = 0,
                UnitPrice = item.UnitPrice,
                Discount = discount,
                TotalAmount = lineTotal,
                DlvExpectedDate = item.DlvExpectedDate,
                Remark = item.Remark?.Trim()
            };
            contract.Details.Add(detail);

            // Nở dòng xe từng chiếc (Dlr_ContractCar) theo mã CtrCarId = <SốHĐ>.01, .02...
            for (int j = 0; j < item.Qty; j++)
            {
                contract.Cars.Add(new DlrRetailContractCar
                {
                    ContractNo = contractNo,
                    CtrCarId = $"{contractNo}.{carSeq:D2}",
                    Model = item.Model.Trim(),
                    SpecCode = item.SpecCode?.Trim().ToUpperInvariant(),
                    ColorCode = item.ColorCode?.Trim().ToUpperInvariant(),
                    Status = RetailContractCarStatus.Pending,
                    Remark = item.Remark?.Trim()
                });
                carSeq++;
            }

            // Ghi nhận snapshot lịch sử phiên bản đầu tiên (Version 1)
            contract.Histories.Add(new DlrRetailContractDtlHis
            {
                ContractNo = contractNo,
                VersionCount = 1,
                VersionDTimeCurr = contract.VersionDTimeCurr,
                Model = item.Model.Trim(),
                SpecCode = item.SpecCode?.Trim().ToUpperInvariant(),
                ColorCode = item.ColorCode?.Trim().ToUpperInvariant(),
                Qty = item.Qty,
                UnitPrice = item.UnitPrice,
                TotalAmount = lineTotal,
                DlvExpectedDate = item.DlvExpectedDate,
                Remark = item.Remark?.Trim(),
                LoggedBy = "DEALER_USER",
                LogDateTime = DateTime.Now
            });

            totalCars += item.Qty;
            totalAmount += lineTotal;
        }

        contract.TotalCars = totalCars;
        contract.TotalAmount = totalAmount;

        db.RetailContracts.Add(contract);
        await db.SaveChangesAsync();

        return new
        {
            contractNo = contract.ContractNo,
            contractNoUser = contract.ContractNoUser,
            dealerCode = contract.DealerCode,
            customerName = contract.CustomerName,
            customerPhone = contract.CustomerPhone,
            paymentType = contract.PaymentType.ToString(),
            bankCode = contract.BankCode,
            totalCars = contract.TotalCars,
            totalAmount = contract.TotalAmount,
            depositAmount = contract.DepositAmount,
            status = contract.Status.ToString(),
            versionCount = contract.VersionCount,
            createdAt = contract.CreatedAt,
            carsCount = contract.Cars.Count
        };
    }

    public async Task<object?> UpdateAsync(string contractNo, UpdateRetailContractDto dto)
    {
        var contract = await db.RetailContracts
            .Include(x => x.Details)
            .Include(x => x.Cars)
            .Include(x => x.Histories)
            .FirstOrDefaultAsync(x => x.OrgId == Org && x.ContractNo == contractNo.Trim().ToUpperInvariant());

        if (contract is null) return null;

        // Quy tắc bất di bất dịch của DMS.Sales: chỉ được phép sửa chi tiết khi hợp đồng ở trạng thái Pending (P)
        if (contract.Status != RetailContractStatus.Pending)
            throw new InvalidOperationException($"Chỉ cho phép cập nhật hợp đồng khi ở trạng thái 'Pending' (Chờ duyệt). Hiện tại là '{contract.Status}'.");

        if (!string.IsNullOrWhiteSpace(dto.ContractNoUser)) contract.ContractNoUser = dto.ContractNoUser.Trim();
        if (!string.IsNullOrWhiteSpace(dto.CustomerName)) contract.CustomerName = dto.CustomerName.Trim();
        if (!string.IsNullOrWhiteSpace(dto.CustomerPhone)) contract.CustomerPhone = dto.CustomerPhone.Trim();
        if (dto.CustomerAddress is not null) contract.CustomerAddress = dto.CustomerAddress.Trim();
        if (dto.TransactorCode is not null) contract.TransactorCode = dto.TransactorCode.Trim();
        if (dto.TransactorName is not null) contract.TransactorName = dto.TransactorName.Trim();
        if (dto.TransactorPhone is not null) contract.TransactorPhone = dto.TransactorPhone.Trim();
        if (dto.DriverCode is not null) contract.DriverCode = dto.DriverCode.Trim();
        if (dto.DealerSaleCode is not null) contract.DealerSaleCode = dto.DealerSaleCode.Trim();
        if (!string.IsNullOrWhiteSpace(dto.SalesType)) contract.SalesType = dto.SalesType.Trim().ToUpperInvariant();
        if (!string.IsNullOrWhiteSpace(dto.SMCode)) contract.SMCode = dto.SMCode.Trim().ToUpperInvariant();
        if (dto.SMName is not null) contract.SMName = dto.SMName.Trim();
        if (dto.Remark is not null) contract.Remark = dto.Remark.Trim();
        if (dto.DepositAmount.HasValue) contract.DepositAmount = dto.DepositAmount.Value;
        if (dto.PaidAmount.HasValue) contract.PaidAmount = dto.PaidAmount.Value;

        if (!string.IsNullOrWhiteSpace(dto.PaymentType))
        {
            var pmtType = ParsePaymentType(dto.PaymentType);
            contract.PaymentType = pmtType;
            if (pmtType == RetailPaymentType.Installment)
            {
                var bankCode = dto.BankCode ?? contract.BankCode;
                if (string.IsNullOrWhiteSpace(bankCode))
                    throw new InvalidOperationException("Phương thức trả góp (Installment) bắt buộc phải có ngân hàng tài trợ (BankCode).");
                contract.BankCode = bankCode.Trim().ToUpperInvariant();
                if (dto.BankName is not null) contract.BankName = dto.BankName.Trim();
                if (dto.BankLoanAmount.HasValue) contract.BankLoanAmount = dto.BankLoanAmount.Value;
            }
            else
            {
                contract.BankCode = null;
                contract.BankName = null;
                contract.BankLoanAmount = 0m;
            }
        }
        else
        {
            if (dto.BankCode is not null) contract.BankCode = dto.BankCode.Trim().ToUpperInvariant();
            if (dto.BankName is not null) contract.BankName = dto.BankName.Trim();
            if (dto.BankLoanAmount.HasValue) contract.BankLoanAmount = dto.BankLoanAmount.Value;
        }

        // Nếu cập nhật danh sách xe
        if (dto.Items is not null && dto.Items.Count > 0)
        {
            // Tăng số phiên bản và lưu mốc thời gian phiên bản
            contract.VersionCount += 1;
            contract.VersionDTimeCurr = DateTime.Now;

            // Xóa chi tiết cũ và danh sách xe chưa giao
            db.RetailContractDetails.RemoveRange(contract.Details);
            db.RetailContractCars.RemoveRange(contract.Cars.Where(c => c.Status == RetailContractCarStatus.Pending));

            int carSeq = 1;
            int totalCars = 0;
            decimal totalAmount = 0m;

            foreach (var item in dto.Items)
            {
                if (string.IsNullOrWhiteSpace(item.Model))
                    throw new InvalidOperationException("Tên dòng xe (Model) không được để trống.");
                if (item.Qty <= 0)
                    throw new InvalidOperationException($"Số lượng xe của model '{item.Model}' phải lớn hơn 0.");
                if (item.UnitPrice <= 0)
                    throw new InvalidOperationException($"Đơn giá xe của model '{item.Model}' phải lớn hơn 0.");

                var discount = item.Discount ?? 0m;
                var lineTotal = (item.Qty * item.UnitPrice) - discount;
                if (lineTotal < 0) lineTotal = 0;

                var detail = new DlrRetailContractDetail
                {
                    ContractId = contract.Id,
                    ContractNo = contract.ContractNo,
                    Model = item.Model.Trim(),
                    SpecCode = item.SpecCode?.Trim().ToUpperInvariant(),
                    ColorCode = item.ColorCode?.Trim().ToUpperInvariant(),
                    ProductionYear = item.ProductionYear ?? DateTime.Today.Year,
                    Qty = item.Qty,
                    QtyDelivery = 0,
                    QtyCancel = 0,
                    UnitPrice = item.UnitPrice,
                    Discount = discount,
                    TotalAmount = lineTotal,
                    DlvExpectedDate = item.DlvExpectedDate,
                    Remark = item.Remark?.Trim()
                };
                contract.Details.Add(detail);

                for (int j = 0; j < item.Qty; j++)
                {
                    contract.Cars.Add(new DlrRetailContractCar
                    {
                        ContractId = contract.Id,
                        ContractNo = contract.ContractNo,
                        CtrCarId = $"{contract.ContractNo}.{carSeq:D2}",
                        Model = item.Model.Trim(),
                        SpecCode = item.SpecCode?.Trim().ToUpperInvariant(),
                        ColorCode = item.ColorCode?.Trim().ToUpperInvariant(),
                        Status = RetailContractCarStatus.Pending,
                        Remark = item.Remark?.Trim()
                    });
                    carSeq++;
                }

                contract.Histories.Add(new DlrRetailContractDtlHis
                {
                    ContractId = contract.Id,
                    ContractNo = contract.ContractNo,
                    VersionCount = contract.VersionCount,
                    VersionDTimeCurr = contract.VersionDTimeCurr,
                    Model = item.Model.Trim(),
                    SpecCode = item.SpecCode?.Trim().ToUpperInvariant(),
                    ColorCode = item.ColorCode?.Trim().ToUpperInvariant(),
                    Qty = item.Qty,
                    UnitPrice = item.UnitPrice,
                    TotalAmount = lineTotal,
                    DlvExpectedDate = item.DlvExpectedDate,
                    Remark = item.Remark?.Trim(),
                    LoggedBy = "DEALER_USER",
                    LogDateTime = DateTime.Now
                });

                totalCars += item.Qty;
                totalAmount += lineTotal;
            }

            contract.TotalCars = totalCars;
            contract.TotalAmount = totalAmount;
        }

        await db.SaveChangesAsync();

        return new
        {
            contractNo = contract.ContractNo,
            contractNoUser = contract.ContractNoUser,
            customerName = contract.CustomerName,
            status = contract.Status.ToString(),
            versionCount = contract.VersionCount,
            totalCars = contract.TotalCars,
            totalAmount = contract.TotalAmount,
            versionDTimeCurr = contract.VersionDTimeCurr
        };
    }

    public async Task<object?> UpdateBankAsync(string contractNo, UpdateRetailContractBankDto dto)
    {
        var contract = await db.RetailContracts
            .FirstOrDefaultAsync(x => x.OrgId == Org && x.ContractNo == contractNo.Trim().ToUpperInvariant());

        if (contract is null) return null;
        if (contract.Status == RetailContractStatus.Cancelled || contract.Status == RetailContractStatus.Finished)
            throw new InvalidOperationException($"Không thể đổi ngân hàng khi hợp đồng ở trạng thái '{contract.Status}'.");

        if (string.IsNullOrWhiteSpace(dto.BankCode))
            throw new InvalidOperationException("Mã ngân hàng (BankCode) không được để trống.");

        contract.PaymentType = RetailPaymentType.Installment;
        contract.BankCode = dto.BankCode.Trim().ToUpperInvariant();
        if (dto.BankName is not null) contract.BankName = dto.BankName.Trim();
        if (dto.BankLoanAmount.HasValue) contract.BankLoanAmount = dto.BankLoanAmount.Value;

        await db.SaveChangesAsync();

        return new
        {
            contractNo = contract.ContractNo,
            paymentType = contract.PaymentType.ToString(),
            bankCode = contract.BankCode,
            bankName = contract.BankName,
            bankLoanAmount = contract.BankLoanAmount
        };
    }

    public async Task<object?> UpdateSalesmanAsync(string contractNo, UpdateRetailContractSMDto dto)
    {
        var contract = await db.RetailContracts
            .FirstOrDefaultAsync(x => x.OrgId == Org && x.ContractNo == contractNo.Trim().ToUpperInvariant());

        if (contract is null) return null;
        if (contract.Status == RetailContractStatus.Cancelled)
            throw new InvalidOperationException("Không thể cập nhật nhân viên tư vấn cho hợp đồng đã bị hủy.");

        if (string.IsNullOrWhiteSpace(dto.SMCode))
            throw new InvalidOperationException("Mã tư vấn bán hàng (SMCode) không được để trống.");

        contract.SMCode = dto.SMCode.Trim().ToUpperInvariant();
        if (dto.SMName is not null) contract.SMName = dto.SMName.Trim();

        await db.SaveChangesAsync();

        return new
        {
            contractNo = contract.ContractNo,
            smCode = contract.SMCode,
            smName = contract.SMName
        };
    }

    public async Task<object> ListAsync(string? status, string? dealer, string? customer, string? paymentType, string? contractNo)
    {
        var q = db.RetailContracts
            .Include(x => x.Details)
            .Include(x => x.Cars)
            .Where(x => x.OrgId == Org)
            .AsNoTracking();

        if (!string.IsNullOrWhiteSpace(contractNo))
        {
            var cn = contractNo.Trim().ToUpperInvariant();
            q = q.Where(x => x.ContractNo.Contains(cn) || x.ContractNoUser.Contains(cn));
        }

        if (!string.IsNullOrWhiteSpace(dealer))
        {
            var d = dealer.Trim().ToUpperInvariant();
            q = q.Where(x => x.DealerCode == d);
        }

        if (!string.IsNullOrWhiteSpace(customer))
        {
            var c = customer.Trim().ToLowerInvariant();
            q = q.Where(x => x.CustomerName.ToLower().Contains(c) || x.CustomerPhone.Contains(c) || x.CustomerIdCardNo.Contains(c));
        }

        if (!string.IsNullOrWhiteSpace(paymentType))
        {
            var pt = ParsePaymentType(paymentType);
            q = q.Where(x => x.PaymentType == pt);
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            if (Enum.TryParse<RetailContractStatus>(status.Trim(), true, out var st))
                q = q.Where(x => x.Status == st);
            else if (status.Trim().ToUpperInvariant() == "P")
                q = q.Where(x => x.Status == RetailContractStatus.Pending);
            else if (status.Trim().ToUpperInvariant() == "A")
                q = q.Where(x => x.Status == RetailContractStatus.Approved);
            else if (status.Trim().ToUpperInvariant() == "F")
                q = q.Where(x => x.Status == RetailContractStatus.Finished);
            else if (status.Trim().ToUpperInvariant() == "C")
                q = q.Where(x => x.Status == RetailContractStatus.Cancelled);
        }

        var list = await q.OrderByDescending(x => x.CreatedAt).ToListAsync();

        return list.Select(x => new
        {
            id = x.Id,
            contractNo = x.ContractNo,
            contractNoUser = x.ContractNoUser,
            dealerCode = x.DealerCode,
            dealerName = x.DealerName,
            customerName = x.CustomerName,
            customerPhone = x.CustomerPhone,
            customerIdCardNo = x.CustomerIdCardNo,
            customerType = x.CustomerType.ToString(),
            salesType = x.SalesType,
            smCode = x.SMCode,
            smName = x.SMName,
            contractDate = x.ContractDate,
            paymentType = x.PaymentType.ToString(),
            bankCode = x.BankCode,
            bankName = x.BankName,
            totalCars = x.TotalCars,
            totalAmount = x.TotalAmount,
            depositAmount = x.DepositAmount,
            paidAmount = x.PaidAmount,
            status = x.Status.ToString(),
            flagDealFinish = x.FlagDealFinish,
            versionCount = x.VersionCount,
            createdAt = x.CreatedAt,
            approvedAt = x.ApprovedAt,
            cars = x.Cars.Select(c => new
            {
                ctrCarId = c.CtrCarId,
                model = c.Model,
                specCode = c.SpecCode,
                colorCode = c.ColorCode,
                vin = c.Vin,
                status = c.Status.ToString(),
                deliveryDate = c.DeliveryDate
            })
        });
    }

    public async Task<object?> DetailAsync(string contractNo)
    {
        var x = await db.RetailContracts
            .Include(m => m.Details)
            .Include(m => m.Cars)
            .Include(m => m.Histories)
            .FirstOrDefaultAsync(m => m.OrgId == Org && m.ContractNo == contractNo.Trim().ToUpperInvariant());

        if (x is null) return null;

        return new
        {
            id = x.Id,
            contractNo = x.ContractNo,
            contractNoUser = x.ContractNoUser,
            dealerCode = x.DealerCode,
            dealerName = x.DealerName,
            dealerCodeBuyer = x.DealerCodeBuyer,
            customerCode = x.CustomerCode,
            customerName = x.CustomerName,
            customerPhone = x.CustomerPhone,
            customerIdCardType = x.CustomerIdCardType,
            customerIdCardNo = x.CustomerIdCardNo,
            customerDateOfBirth = x.CustomerDateOfBirth,
            customerAddress = x.CustomerAddress,
            customerType = x.CustomerType.ToString(),
            transactorCode = x.TransactorCode,
            transactorName = x.TransactorName,
            transactorPhone = x.TransactorPhone,
            driverCode = x.DriverCode,
            dealerSaleCode = x.DealerSaleCode,
            salesType = x.SalesType,
            smCode = x.SMCode,
            smName = x.SMName,
            contractDate = x.ContractDate,
            paymentType = x.PaymentType.ToString(),
            bankCode = x.BankCode,
            bankName = x.BankName,
            bankLoanAmount = x.BankLoanAmount,
            depositAmount = x.DepositAmount,
            paidAmount = x.PaidAmount,
            totalCars = x.TotalCars,
            totalAmount = x.TotalAmount,
            status = x.Status.ToString(),
            flagDealFinish = x.FlagDealFinish,
            versionCount = x.VersionCount,
            versionDTimeCurr = x.VersionDTimeCurr,
            approvedAt = x.ApprovedAt,
            approvedBy = x.ApprovedBy,
            cancelledAt = x.CancelledAt,
            cancelledBy = x.CancelledBy,
            cancelReason = x.CancelReason,
            remark = x.Remark,
            createdBy = x.CreatedBy,
            createdAt = x.CreatedAt,
            details = x.Details.Select(d => new
            {
                id = d.Id,
                model = d.Model,
                specCode = d.SpecCode,
                colorCode = d.ColorCode,
                productionYear = d.ProductionYear,
                qty = d.Qty,
                qtyDelivery = d.QtyDelivery,
                qtyCancel = d.QtyCancel,
                unitPrice = d.UnitPrice,
                discount = d.Discount,
                totalAmount = d.TotalAmount,
                dlvExpectedDate = d.DlvExpectedDate,
                remark = d.Remark
            }),
            cars = x.Cars.Select(c => new
            {
                id = c.Id,
                ctrCarId = c.CtrCarId,
                model = c.Model,
                specCode = c.SpecCode,
                colorCode = c.ColorCode,
                vin = c.Vin,
                status = c.Status.ToString(),
                deliveryDate = c.DeliveryDate,
                remark = c.Remark
            }),
            histories = x.Histories.OrderByDescending(h => h.VersionCount).Select(h => new
            {
                id = h.Id,
                versionCount = h.VersionCount,
                versionDTimeCurr = h.VersionDTimeCurr,
                model = h.Model,
                specCode = h.SpecCode,
                colorCode = h.ColorCode,
                qty = h.Qty,
                unitPrice = h.UnitPrice,
                totalAmount = h.TotalAmount,
                dlvExpectedDate = h.DlvExpectedDate,
                loggedBy = h.LoggedBy,
                logDateTime = h.LogDateTime,
                remark = h.Remark
            })
        };
    }

    public async Task<object?> ApproveAsync(string contractNo, ApproveRetailContractDto? dto)
    {
        var contract = await db.RetailContracts
            .FirstOrDefaultAsync(x => x.OrgId == Org && x.ContractNo == contractNo.Trim().ToUpperInvariant());

        if (contract is null) return null;

        if (contract.Status != RetailContractStatus.Pending)
            throw new InvalidOperationException($"Chỉ hợp đồng ở trạng thái 'Pending' mới có thể duyệt. Trạng thái hiện tại: '{contract.Status}'.");

        contract.Status = RetailContractStatus.Approved;
        contract.ApprovedAt = DateTime.Now;
        contract.ApprovedBy = string.IsNullOrWhiteSpace(dto?.ApprovedBy) ? "DEALER_MANAGER" : dto.ApprovedBy.Trim();
        if (!string.IsNullOrWhiteSpace(dto?.Note))
            contract.Remark = string.IsNullOrWhiteSpace(contract.Remark) ? dto.Note.Trim() : $"{contract.Remark} | {dto.Note.Trim()}";

        await db.SaveChangesAsync();

        return new
        {
            contractNo = contract.ContractNo,
            status = contract.Status.ToString(),
            approvedAt = contract.ApprovedAt,
            approvedBy = contract.ApprovedBy,
            remark = contract.Remark
        };
    }

    public async Task<object?> CancelAsync(string contractNo, CancelRetailContractDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Reason))
            throw new InvalidOperationException("Lý do hủy hợp đồng (Reason) không được để trống.");

        var contract = await db.RetailContracts
            .Include(x => x.Cars)
            .Include(x => x.Details)
            .FirstOrDefaultAsync(x => x.OrgId == Org && x.ContractNo == contractNo.Trim().ToUpperInvariant());

        if (contract is null) return null;

        if (contract.Status == RetailContractStatus.Finished)
            throw new InvalidOperationException("Hợp đồng đã hoàn thành bàn giao toàn bộ xe (Finished), không thể hủy trực tiếp.");
        if (contract.Status == RetailContractStatus.Cancelled)
            throw new InvalidOperationException("Hợp đồng đã ở trạng thái hủy (Cancelled).");

        contract.Status = RetailContractStatus.Cancelled;
        contract.CancelledAt = DateTime.Now;
        contract.CancelledBy = string.IsNullOrWhiteSpace(dto.CancelledBy) ? "DEALER_USER" : dto.CancelledBy.Trim();
        contract.CancelReason = dto.Reason.Trim();

        // Đánh dấu các xe chưa giao sang trạng thái Cancelled
        foreach (var car in contract.Cars.Where(c => c.Status == RetailContractCarStatus.Pending))
        {
            car.Status = RetailContractCarStatus.Cancelled;
        }

        // Cập nhật QtyCancel trong details
        foreach (var dtl in contract.Details)
        {
            dtl.QtyCancel = dtl.Qty - dtl.QtyDelivery;
        }

        await db.SaveChangesAsync();

        return new
        {
            contractNo = contract.ContractNo,
            status = contract.Status.ToString(),
            cancelledAt = contract.CancelledAt,
            cancelledBy = contract.CancelledBy,
            cancelReason = contract.CancelReason
        };
    }

    public async Task<object?> DeleteDraftAsync(string contractNo)
    {
        var contract = await db.RetailContracts
            .FirstOrDefaultAsync(x => x.OrgId == Org && x.ContractNo == contractNo.Trim().ToUpperInvariant());

        if (contract is null) return null;

        if (contract.Status != RetailContractStatus.Pending)
            throw new InvalidOperationException($"Chỉ hợp đồng nháp 'Pending' mới được phép xóa. Trạng thái hiện tại: '{contract.Status}'.");

        db.RetailContracts.Remove(contract);
        await db.SaveChangesAsync();

        return new { contractNo = contract.ContractNo, deleted = true };
    }

    public async Task<object?> AssignVinAsync(string contractNo, AssignRetailCarVinDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Vin))
            throw new InvalidOperationException("Số khung (Vin) không được để trống.");

        var contract = await db.RetailContracts
            .Include(x => x.Cars)
            .FirstOrDefaultAsync(x => x.OrgId == Org && x.ContractNo == contractNo.Trim().ToUpperInvariant());

        if (contract is null) return null;

        if (contract.Status == RetailContractStatus.Cancelled)
            throw new InvalidOperationException("Hợp đồng đã bị hủy, không thể gán số VIN.");

        var vin = dto.Vin.Trim().ToUpperInvariant();

        DlrRetailContractCar? targetCar = null;

        if (!string.IsNullOrWhiteSpace(dto.CtrCarId))
        {
            targetCar = contract.Cars.FirstOrDefault(c => c.CtrCarId == dto.CtrCarId.Trim());
            if (targetCar is null)
                throw new InvalidOperationException($"Không tìm thấy xe có mã CtrCarId '{dto.CtrCarId}' trong hợp đồng.");
        }
        else
        {
            targetCar = contract.Cars.FirstOrDefault(c =>
                c.Status == RetailContractCarStatus.Pending &&
                (string.IsNullOrWhiteSpace(dto.Model) || c.Model.Equals(dto.Model.Trim(), StringComparison.OrdinalIgnoreCase)) &&
                (string.IsNullOrWhiteSpace(dto.SpecCode) || c.SpecCode == dto.SpecCode.Trim().ToUpperInvariant()) &&
                (string.IsNullOrWhiteSpace(dto.ColorCode) || c.ColorCode == dto.ColorCode.Trim().ToUpperInvariant()) &&
                string.IsNullOrWhiteSpace(c.Vin)
            );

            if (targetCar is null)
                targetCar = contract.Cars.FirstOrDefault(c => c.Status == RetailContractCarStatus.Pending && string.IsNullOrWhiteSpace(c.Vin));

            if (targetCar is null)
                throw new InvalidOperationException("Không còn dòng xe chờ gán VIN phù hợp trong hợp đồng.");
        }

        targetCar.Vin = vin;
        await db.SaveChangesAsync();

        return new
        {
            contractNo = contract.ContractNo,
            ctrCarId = targetCar.CtrCarId,
            model = targetCar.Model,
            specCode = targetCar.SpecCode,
            colorCode = targetCar.ColorCode,
            vin = targetCar.Vin,
            status = targetCar.Status.ToString()
        };
    }

    public async Task<object?> DeliverCarAsync(string contractNo, DeliverRetailCarDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.CtrCarId))
            throw new InvalidOperationException("Mã xe bàn giao (CtrCarId) không được để trống.");

        var contract = await db.RetailContracts
            .Include(x => x.Cars)
            .Include(x => x.Details)
            .FirstOrDefaultAsync(x => x.OrgId == Org && x.ContractNo == contractNo.Trim().ToUpperInvariant());

        if (contract is null) return null;

        if (contract.Status != RetailContractStatus.Approved && contract.Status != RetailContractStatus.Finished)
            throw new InvalidOperationException($"Chỉ hợp đồng đã duyệt ('Approved') mới có thể thực hiện bàn giao xe. Trạng thái hiện tại: '{contract.Status}'.");

        var car = contract.Cars.FirstOrDefault(c => c.CtrCarId == dto.CtrCarId.Trim());
        if (car is null)
            throw new InvalidOperationException($"Không tìm thấy mã xe '{dto.CtrCarId}' trong hợp đồng.");

        if (car.Status == RetailContractCarStatus.Delivered)
            throw new InvalidOperationException($"Xe '{dto.CtrCarId}' đã được bàn giao trước đó.");
        if (car.Status == RetailContractCarStatus.Cancelled)
            throw new InvalidOperationException($"Xe '{dto.CtrCarId}' đã bị hủy, không thể bàn giao.");

        car.Status = RetailContractCarStatus.Delivered;
        car.DeliveryDate = dto.DeliveryDate ?? DateTime.Now;
        if (!string.IsNullOrWhiteSpace(dto.Remark))
            car.Remark = dto.Remark.Trim();

        // Cập nhật QtyDelivery cho dòng cấu hình tương ứng trong Details
        var detail = contract.Details.FirstOrDefault(d =>
            d.Model.Equals(car.Model, StringComparison.OrdinalIgnoreCase) &&
            (string.IsNullOrWhiteSpace(d.SpecCode) || d.SpecCode == car.SpecCode) &&
            (string.IsNullOrWhiteSpace(d.ColorCode) || d.ColorCode == car.ColorCode)
        );
        if (detail is not null)
        {
            detail.QtyDelivery += 1;
        }

        // Rule 4.4 DMS.Sales: Hoàn thành hợp đồng khi tất cả các xe đã giao đủ (FlagDealFinish = "Y")
        bool allDelivered = contract.Cars.All(c => c.Status == RetailContractCarStatus.Delivered || c.Status == RetailContractCarStatus.Cancelled);
        int totalDelivered = contract.Cars.Count(c => c.Status == RetailContractCarStatus.Delivered);

        if (allDelivered && totalDelivered > 0)
        {
            contract.FlagDealFinish = true;
            contract.Status = RetailContractStatus.Finished;
        }

        await db.SaveChangesAsync();

        return new
        {
            contractNo = contract.ContractNo,
            ctrCarId = car.CtrCarId,
            vin = car.Vin,
            carStatus = car.Status.ToString(),
            deliveryDate = car.DeliveryDate,
            contractStatus = contract.Status.ToString(),
            flagDealFinish = contract.FlagDealFinish,
            totalDelivered,
            totalCars = contract.TotalCars
        };
    }

    public async Task<object> ApproveMultiAsync(BatchApproveRetailContractDto dto)
    {
        if (dto.ContractNos is null || dto.ContractNos.Count == 0)
            throw new InvalidOperationException("Danh sách số hợp đồng (ContractNos) không được rỗng.");

        var cleanNos = dto.ContractNos.Select(x => x.Trim().ToUpperInvariant()).Distinct().ToList();
        var contracts = await db.RetailContracts
            .Where(x => x.OrgId == Org && cleanNos.Contains(x.ContractNo))
            .ToListAsync();

        var approvedList = new List<string>();
        var skippedList = new List<object>();

        foreach (var c in contracts)
        {
            if (c.Status == RetailContractStatus.Pending)
            {
                c.Status = RetailContractStatus.Approved;
                c.ApprovedAt = DateTime.Now;
                c.ApprovedBy = string.IsNullOrWhiteSpace(dto.ApprovedBy) ? "DEALER_MANAGER" : dto.ApprovedBy.Trim();
                approvedList.Add(c.ContractNo);
            }
            else
            {
                skippedList.Add(new { contractNo = c.ContractNo, status = c.Status.ToString(), reason = "Không ở trạng thái Pending" });
            }
        }

        await db.SaveChangesAsync();

        return new
        {
            totalRequested = cleanNos.Count,
            approvedCount = approvedList.Count,
            approvedContracts = approvedList,
            skipped = skippedList
        };
    }

    public async Task<object> CancelMultiAsync(BatchCancelRetailContractDto dto)
    {
        if (dto.ContractNos is null || dto.ContractNos.Count == 0)
            throw new InvalidOperationException("Danh sách số hợp đồng (ContractNos) không được rỗng.");
        if (string.IsNullOrWhiteSpace(dto.Reason))
            throw new InvalidOperationException("Lý do hủy hợp đồng (Reason) không được để trống.");

        var cleanNos = dto.ContractNos.Select(x => x.Trim().ToUpperInvariant()).Distinct().ToList();
        var contracts = await db.RetailContracts
            .Include(x => x.Cars)
            .Include(x => x.Details)
            .Where(x => x.OrgId == Org && cleanNos.Contains(x.ContractNo))
            .ToListAsync();

        var cancelledList = new List<string>();
        var skippedList = new List<object>();

        foreach (var c in contracts)
        {
            if (c.Status == RetailContractStatus.Pending || c.Status == RetailContractStatus.Approved)
            {
                c.Status = RetailContractStatus.Cancelled;
                c.CancelledAt = DateTime.Now;
                c.CancelledBy = string.IsNullOrWhiteSpace(dto.CancelledBy) ? "DEALER_USER" : dto.CancelledBy.Trim();
                c.CancelReason = dto.Reason.Trim();

                foreach (var car in c.Cars.Where(x => x.Status == RetailContractCarStatus.Pending))
                    car.Status = RetailContractCarStatus.Cancelled;

                foreach (var dtl in c.Details)
                    dtl.QtyCancel = dtl.Qty - dtl.QtyDelivery;

                cancelledList.Add(c.ContractNo);
            }
            else
            {
                skippedList.Add(new { contractNo = c.ContractNo, status = c.Status.ToString(), reason = "Hợp đồng đã Finished hoặc Cancelled" });
            }
        }

        await db.SaveChangesAsync();

        return new
        {
            totalRequested = cleanNos.Count,
            cancelledCount = cancelledList.Count,
            cancelledContracts = cancelledList,
            skipped = skippedList
        };
    }

    public async Task<object> StatsAsync()
    {
        var contracts = await db.RetailContracts
            .Where(x => x.OrgId == Org)
            .AsNoTracking()
            .ToListAsync();

        var total = contracts.Count;
        var pending = contracts.Count(x => x.Status == RetailContractStatus.Pending);
        var approved = contracts.Count(x => x.Status == RetailContractStatus.Approved);
        var finished = contracts.Count(x => x.Status == RetailContractStatus.Finished);
        var cancelled = contracts.Count(x => x.Status == RetailContractStatus.Cancelled);

        var totalAmount = contracts.Where(x => x.Status != RetailContractStatus.Cancelled).Sum(x => x.TotalAmount);
        var totalDeposit = contracts.Sum(x => x.DepositAmount);
        var totalPaid = contracts.Sum(x => x.PaidAmount);
        var totalCars = contracts.Where(x => x.Status != RetailContractStatus.Cancelled).Sum(x => x.TotalCars);

        var cashCount = contracts.Count(x => x.PaymentType == RetailPaymentType.Cash);
        var installmentCount = contracts.Count(x => x.PaymentType == RetailPaymentType.Installment);

        var details = await db.RetailContractDetails
            .Where(d => db.RetailContracts.Any(c => c.OrgId == Org && c.Id == d.ContractId && c.Status != RetailContractStatus.Cancelled))
            .AsNoTracking()
            .ToListAsync();

        var topModels = details
            .GroupBy(d => d.Model)
            .Select(g => new { model = g.Key, quantity = g.Sum(x => x.Qty), revenue = g.Sum(x => x.TotalAmount) })
            .OrderByDescending(x => x.quantity)
            .Take(5)
            .ToList();

        return new
        {
            totalContracts = total,
            totalCars,
            totalAmount,
            totalDeposit,
            totalPaid,
            statusBreakdown = new
            {
                pending,
                approved,
                finished,
                cancelled
            },
            paymentBreakdown = new
            {
                cash = cashCount,
                installment = installmentCount
            },
            topModels
        };
    }

    private static RetailPaymentType ParsePaymentType(string? pmtType)
    {
        if (string.IsNullOrWhiteSpace(pmtType)) return RetailPaymentType.Cash;
        var s = pmtType.Trim().ToLowerInvariant();
        return s switch
        {
            "installment" or "2" or "tragop" or "bank" or "loan" => RetailPaymentType.Installment,
            _ => RetailPaymentType.Cash
        };
    }
}
