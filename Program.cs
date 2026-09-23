using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using MiniSales.Data;
using MiniSales.Models;
using MiniSales.Services;
using Serilog;

JwtSecurityTokenHandler.DefaultMapInboundClaims = false;
AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
FleetObs.ConfigureLogger("minisales");

var builder = WebApplication.CreateBuilder(args);
builder.Host.UseSerilog();
builder.WebHost.UseUrls($"http://0.0.0.0:{Environment.GetEnvironmentVariable("PORT") ?? "8080"}");

var conn = Environment.GetEnvironmentVariable("CONNECTION_STRING")
    ?? builder.Configuration.GetConnectionString("DefaultConnection") ?? "Data Source=minisales.db";
builder.Services.AddDbContext<AppDbContext>(o =>
{
    if (DbUtil.IsPostgres(conn)) o.UseNpgsql(DbUtil.ToNpgsql(conn));
    else o.UseSqlite(conn);
});
builder.Services.AddScoped<ITenantContext, TenantContext>();
builder.Services.AddScoped<ISalesService, SalesService>();
builder.Services.AddScoped<IDeliveryOrderService, DeliveryOrderService>();
builder.Services.AddScoped<IDealerOrderService, DealerOrderService>();
builder.Services.AddScoped<IPaymentGuaranteeService, PaymentGuaranteeService>();
builder.Services.AddScoped<ICarDocRequestService, CarDocRequestService>();
builder.Services.AddScoped<ICarInvoiceService, CarInvoiceService>();
builder.Services.AddScoped<ICarRetrieveService, CarRetrieveService>();
builder.Services.AddScoped<IContractCancelService, ContractCancelService>();
builder.Services.AddScoped<ICarTransportMinutesService, CarTransportMinutesService>();
builder.Services.AddScoped<IDealerContractService, DealerContractService>();
builder.Services.AddScoped<IRetailContractService, RetailContractService>();
builder.Services.AddScoped<IDealerPaymentService, DealerPaymentService>();
builder.Services.AddScoped<IRetailDealService, RetailDealService>();
builder.Services.AddScoped<IStorageRearrangeService, StorageRearrangeService>();
builder.Services.AddScoped<ITransportRequestService, TransportRequestService>();
builder.Services.AddScoped<IPaymentGuaranteeExtService, PaymentGuaranteeExtService>();
builder.Services.AddScoped<IPdiRequestService, PdiRequestService>();
builder.Services.AddScoped<IPaymentDiscountService, PaymentDiscountService>();
builder.Services.AddScoped<IDealerContractCancelMinutesService, DealerContractCancelMinutesService>();
builder.Services.AddScoped<IDealerContractCancelBankMDService, DealerContractCancelBankMDService>();
builder.Services.AddScoped<IPaymentGuaranteeClaimService, PaymentGuaranteeClaimService>();
builder.Services.AddScoped<IBankBillMinutesService, BankBillMinutesService>();
builder.Services.AddScoped<ICarTestCarService, CarTestCarService>();
builder.Services.AddScoped<ICarBodyRequestService, CarBodyRequestService>();
builder.Services.AddScoped<IDealerDriveTestService, DealerDriveTestService>();
builder.Services.AddScoped<IStorageRearrangeCBService, StorageRearrangeCBService>();
builder.Services.AddScoped<ICarCancelService, CarCancelService>();
builder.Services.AddScoped<IMapVinService, MapVinService>();
builder.Services.AddScoped<IRetailInvoiceService, RetailInvoiceService>();
builder.Services.AddScoped<ICarRedeemService, CarRedeemService>();
builder.Services.AddScoped<ICarInsuranceService, CarInsuranceService>();
builder.Services.AddScoped<ITransportPlanService, TransportPlanService>();
builder.Services.AddScoped<ISaleAwardMinutesService, SaleAwardMinutesService>();
builder.Services.AddScoped<IBusinessPlanService, BusinessPlanService>();
builder.Services.AddScoped<ICalcFnExpPmDcService, CalcFnExpPmDcService>();
builder.Services.AddScoped<ISalesPolicyService, SalesPolicyService>();
builder.Services.AddScoped<IPlanEstimateOrderService, PlanEstimateOrderService>();

var ssoAuthority = Environment.GetEnvironmentVariable("SSO_AUTHORITY") ?? "https://minisso.onrender.com";
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(o =>
{
    o.Authority = ssoAuthority;
    o.RequireHttpsMetadata = ssoAuthority.StartsWith("https");
    o.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true, ValidIssuer = ssoAuthority,
        ValidateAudience = false, ValidateLifetime = true, NameClaimType = "name", RoleClaimType = "role"
    };
});
builder.Services.AddAuthorization();
builder.Services.AddFleetObs();

var app = builder.Build();
using (var scope = app.Services.CreateScope())
    await Seeder.SeedAsync(scope.ServiceProvider.GetRequiredService<AppDbContext>());

app.UseFleetObs();
FleetObs.ReportLicense(ssoAuthority, "minisales");
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/api/whoami", (ClaimsPrincipal u) => Results.Ok(new
{
    app = "minisales",
    sub = u.FindFirst("sub")?.Value, name = u.Identity?.Name ?? u.FindFirst("name")?.Value,
    email = u.FindFirst("email")?.Value, tenant = u.FindFirst("tenant")?.Value,
    roles = u.FindAll("role").Select(c => c.Value)
})).RequireAuthorization();

app.Use(async (ctx, next) =>
{
    var key = ctx.Request.Headers["X-Api-Key"].FirstOrDefault();
    if (string.IsNullOrWhiteSpace(key)) ctx.Request.Cookies.TryGetValue(TenantContext.CookieName, out key);
    if (!string.IsNullOrWhiteSpace(key))
    {
        using var lookup = app.Services.CreateScope();
        var ldb = lookup.ServiceProvider.GetRequiredService<AppDbContext>();
        var org = await ldb.Orgs.FirstOrDefaultAsync(o => o.ApiKey == key);
        if (org != null) ctx.RequestServices.GetRequiredService<ITenantContext>().OrgId = org.Id;
    }
    await next();
});

app.UseDefaultFiles();
app.UseStaticFiles();
app.MapGet("/healthz", () => "ok");

// ===== Hợp đồng bán xe =====
app.MapPost("/api/orders", async (CreateOrderDto dto, ISalesService svc) =>
{
    if (string.IsNullOrWhiteSpace(dto.CustomerName) || string.IsNullOrWhiteSpace(dto.Model))
        return Results.BadRequest(new { error = "Cần CustomerName và Model." });
    try { return Results.Ok(await svc.CreateAsync(dto)); }
    catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
}).RequireAuthorization();

app.MapGet("/api/orders", async (ISalesService svc, string? status, string? dealer) =>
    Results.Ok(await svc.ListAsync(status, dealer))).RequireAuthorization();

app.MapGet("/api/orders/{code}", async (string code, ISalesService svc) =>
{
    var r = await svc.DetailAsync(code);
    return r is null ? Results.NotFound(new { code }) : Results.Ok(r);
}).RequireAuthorization();

app.MapPost("/api/orders/{code}/deposit", async (string code, PayDto dto, ISalesService svc) =>
{
    try { var r = await svc.PayAsync(code, "Deposit", dto.Amount, dto.RefNo); return r is null ? Results.NotFound(new { code }) : Results.Ok(r); }
    catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
}).RequireAuthorization();

app.MapPost("/api/orders/{code}/pay", async (string code, PayDto dto, ISalesService svc) =>
{
    try { var r = await svc.PayAsync(code, "Payment", dto.Amount, dto.RefNo); return r is null ? Results.NotFound(new { code }) : Results.Ok(r); }
    catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
}).RequireAuthorization();

app.MapPost("/api/orders/{code}/deliver", async (string code, ISalesService svc) =>
{
    try { var r = await svc.DeliverAsync(code); return r is null ? Results.NotFound(new { code }) : Results.Ok(r); }
    catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
}).RequireAuthorization();

app.MapPost("/api/orders/{code}/cancel", async (string code, ISalesService svc) =>
{
    try { var r = await svc.CancelAsync(code); return r is null ? Results.NotFound(new { code }) : Results.Ok(r); }
    catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
}).RequireAuthorization();

app.MapGet("/api/stats", async (ISalesService svc) => Results.Ok(await svc.StatsAsync())).RequireAuthorization();

// ===== Lệnh xuất/giao xe (Car Delivery Order - DMS.Sales) =====
app.MapPost("/api/delivery-orders", async (CreateDeliveryOrderDto dto, IDeliveryOrderService svc) =>
{
    try { return Results.Ok(await svc.CreateAsync(dto)); }
    catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
}).RequireAuthorization();

app.MapGet("/api/delivery-orders", async (IDeliveryOrderService svc, string? status, string? dealer, string? orderCode) =>
    Results.Ok(await svc.ListAsync(status, dealer, orderCode))).RequireAuthorization();

app.MapGet("/api/delivery-orders/stats", async (IDeliveryOrderService svc) =>
    Results.Ok(await svc.StatsAsync())).RequireAuthorization();

app.MapGet("/api/delivery-orders/{doNo}", async (string doNo, IDeliveryOrderService svc) =>
{
    var r = await svc.DetailAsync(doNo);
    return r is null ? Results.NotFound(new { deliveryOrderNo = doNo }) : Results.Ok(r);
}).RequireAuthorization();

app.MapPost("/api/delivery-orders/{doNo}/approve", async (string doNo, IDeliveryOrderService svc) =>
{
    try
    {
        var r = await svc.ApproveAsync(doNo);
        return r is null ? Results.NotFound(new { deliveryOrderNo = doNo }) : Results.Ok(r);
    }
    catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
}).RequireAuthorization();

app.MapPost("/api/delivery-orders/{doNo}/complete", async (string doNo, CompleteDeliveryOrderDto dto, IDeliveryOrderService svc) =>
{
    try
    {
        var r = await svc.CompleteAsync(doNo, dto);
        return r is null ? Results.NotFound(new { deliveryOrderNo = doNo }) : Results.Ok(r);
    }
    catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
}).RequireAuthorization();

app.MapPost("/api/delivery-orders/{doNo}/cancel", async (string doNo, CancelDeliveryOrderDto? dto, IDeliveryOrderService svc) =>
{
    try
    {
        var r = await svc.CancelAsync(doNo, dto?.Reason);
        return r is null ? Results.NotFound(new { deliveryOrderNo = doNo }) : Results.Ok(r);
    }
    catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
}).RequireAuthorization();

// ===== Đơn đặt hàng xe Đại lý (Dealer Purchase Order - DMS.Sales Ord_SalesOrderRoot) =====
app.MapPost("/api/dealer-orders", async (CreateDealerOrderDto dto, IDealerOrderService svc) =>
{
    try { return Results.Ok(await svc.CreateAsync(dto)); }
    catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
}).RequireAuthorization();

app.MapGet("/api/dealer-orders", async (IDealerOrderService svc, string? status, string? dealer, string? orderType, string? month) =>
    Results.Ok(await svc.ListAsync(status, dealer, orderType, month))).RequireAuthorization();

app.MapGet("/api/dealer-orders/stats", async (IDealerOrderService svc) =>
    Results.Ok(await svc.StatsAsync())).RequireAuthorization();

app.MapGet("/api/dealer-orders/{orderNo}", async (string orderNo, IDealerOrderService svc) =>
{
    var r = await svc.DetailAsync(orderNo);
    return r is null ? Results.NotFound(new { orderNo }) : Results.Ok(r);
}).RequireAuthorization();

app.MapPost("/api/dealer-orders/{orderNo}/approve1", async (string orderNo, ApproveDealerOrderDto? dto, IDealerOrderService svc) =>
{
    try
    {
        var r = await svc.Approve1Async(orderNo, dto);
        return r is null ? Results.NotFound(new { orderNo }) : Results.Ok(r);
    }
    catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
}).RequireAuthorization();

app.MapPost("/api/dealer-orders/{orderNo}/approve2", async (string orderNo, IDealerOrderService svc) =>
{
    try
    {
        var r = await svc.Approve2Async(orderNo);
        return r is null ? Results.NotFound(new { orderNo }) : Results.Ok(r);
    }
    catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
}).RequireAuthorization();

app.MapPost("/api/dealer-orders/{orderNo}/reject", async (string orderNo, RejectDealerOrderDto dto, IDealerOrderService svc) =>
{
    try
    {
        var r = await svc.RejectAsync(orderNo, dto);
        return r is null ? Results.NotFound(new { orderNo }) : Results.Ok(r);
    }
    catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
}).RequireAuthorization();

app.MapPost("/api/dealer-orders/{orderNo}/cancel", async (string orderNo, CancelDealerOrderDto? dto, IDealerOrderService svc) =>
{
    try
    {
        var r = await svc.CancelAsync(orderNo, dto);
        return r is null ? Results.NotFound(new { orderNo }) : Results.Ok(r);
    }
    catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
}).RequireAuthorization();

// ===== Bảo lãnh thanh toán Ngân hàng (Payment Guarantee - DMS.Sales Pmt_Guarantee) =====
app.MapPost("/api/guarantees", async (CreatePaymentGuaranteeDto dto, IPaymentGuaranteeService svc) =>
{
    try { return Results.Ok(await svc.CreateAsync(dto)); }
    catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
}).RequireAuthorization();

app.MapGet("/api/guarantees", async (IPaymentGuaranteeService svc, string? status, string? dealer, string? bankCode, string? guaranteeType) =>
    Results.Ok(await svc.ListAsync(status, dealer, bankCode, guaranteeType))).RequireAuthorization();

app.MapGet("/api/guarantees/stats", async (IPaymentGuaranteeService svc) =>
    Results.Ok(await svc.StatsAsync())).RequireAuthorization();

app.MapGet("/api/guarantees/{guaranteeNo}", async (string guaranteeNo, IPaymentGuaranteeService svc) =>
{
    var r = await svc.DetailAsync(guaranteeNo);
    return r is null ? Results.NotFound(new { guaranteeNo }) : Results.Ok(r);
}).RequireAuthorization();

app.MapPost("/api/guarantees/{guaranteeNo}/approve", async (string guaranteeNo, ApprovePaymentGuaranteeDto? dto, IPaymentGuaranteeService svc) =>
{
    try
    {
        var r = await svc.ApproveAsync(guaranteeNo, dto);
        return r is null ? Results.NotFound(new { guaranteeNo }) : Results.Ok(r);
    }
    catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
}).RequireAuthorization();

app.MapPost("/api/guarantees/{guaranteeNo}/reject", async (string guaranteeNo, RejectPaymentGuaranteeDto dto, IPaymentGuaranteeService svc) =>
{
    try
    {
        var r = await svc.RejectAsync(guaranteeNo, dto);
        return r is null ? Results.NotFound(new { guaranteeNo }) : Results.Ok(r);
    }
    catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
}).RequireAuthorization();

app.MapPost("/api/guarantees/{guaranteeNo}/cancel", async (string guaranteeNo, CancelPaymentGuaranteeDto? dto, IPaymentGuaranteeService svc) =>
{
    try
    {
        var r = await svc.CancelAsync(guaranteeNo, dto);
        return r is null ? Results.NotFound(new { guaranteeNo }) : Results.Ok(r);
    }
    catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
}).RequireAuthorization();

app.MapPost("/api/guarantees/{guaranteeNo}/cars", async (string guaranteeNo, AllocateCarDto dto, IPaymentGuaranteeService svc) =>
{
    try
    {
        var r = await svc.AllocateCarAsync(guaranteeNo, dto);
        return r is null ? Results.NotFound(new { guaranteeNo }) : Results.Ok(r);
    }
    catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
}).RequireAuthorization();

app.MapDelete("/api/guarantees/{guaranteeNo}/cars/{detailId:long}", async (string guaranteeNo, long detailId, IPaymentGuaranteeService svc) =>
{
    try
    {
        var r = await svc.RemoveCarAsync(guaranteeNo, detailId);
        return r is null ? Results.NotFound(new { guaranteeNo, detailId }) : Results.Ok(r);
    }
    catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
}).RequireAuthorization();

// ===== Đề nghị giao hồ sơ xe / Rút hồ sơ / Giải chấp ngân hàng (Car Document Request - DMS.Sales Car_DocReqList) =====
app.MapPost("/api/doc-requests", async (CreateCarDocRequestDto dto, ICarDocRequestService svc) =>
{
    try { return Results.Ok(await svc.CreateAsync(dto)); }
    catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
}).RequireAuthorization();

app.MapGet("/api/doc-requests", async (ICarDocRequestService svc, string? status, string? dealer, string? requestType) =>
    Results.Ok(await svc.ListAsync(status, dealer, requestType))).RequireAuthorization();

app.MapGet("/api/doc-requests/stats", async (ICarDocRequestService svc) =>
    Results.Ok(await svc.StatsAsync())).RequireAuthorization();

app.MapGet("/api/doc-requests/{drCode}", async (string drCode, ICarDocRequestService svc) =>
{
    var r = await svc.DetailAsync(drCode);
    return r is null ? Results.NotFound(new { drListCode = drCode }) : Results.Ok(r);
}).RequireAuthorization();

app.MapPost("/api/doc-requests/{drCode}/approve1", async (string drCode, Approve1DocRequestDto? dto, ICarDocRequestService svc) =>
{
    try
    {
        var r = await svc.Approve1Async(drCode, dto);
        return r is null ? Results.NotFound(new { drListCode = drCode }) : Results.Ok(r);
    }
    catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
}).RequireAuthorization();

app.MapPost("/api/doc-requests/{drCode}/approve2", async (string drCode, Approve2DocRequestDto? dto, ICarDocRequestService svc) =>
{
    try
    {
        var r = await svc.Approve2Async(drCode, dto);
        return r is null ? Results.NotFound(new { drListCode = drCode }) : Results.Ok(r);
    }
    catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
}).RequireAuthorization();

app.MapPost("/api/doc-requests/{drCode}/reject", async (string drCode, RejectDocRequestDto dto, ICarDocRequestService svc) =>
{
    try
    {
        var r = await svc.RejectAsync(drCode, dto);
        return r is null ? Results.NotFound(new { drListCode = drCode }) : Results.Ok(r);
    }
    catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
}).RequireAuthorization();

app.MapPost("/api/doc-requests/{drCode}/cancel", async (string drCode, CancelDocRequestDto? dto, ICarDocRequestService svc) =>
{
    try
    {
        var r = await svc.CancelAsync(drCode, dto);
        return r is null ? Results.NotFound(new { drListCode = drCode }) : Results.Ok(r);
    }
    catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
}).RequireAuthorization();

app.MapPost("/api/doc-requests/{drCode}/cars", async (string drCode, AddCarDocRequestItemDto dto, ICarDocRequestService svc) =>
{
    try
    {
        var r = await svc.AddCarAsync(drCode, dto);
        return r is null ? Results.NotFound(new { drListCode = drCode }) : Results.Ok(r);
    }
    catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
}).RequireAuthorization();

app.MapDelete("/api/doc-requests/{drCode}/cars/{detailId:long}", async (string drCode, long detailId, ICarDocRequestService svc) =>
{
    try
    {
        var r = await svc.RemoveCarAsync(drCode, detailId);
        return r is null ? Results.NotFound(new { drListCode = drCode, detailId }) : Results.Ok(r);
    }
    catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
}).RequireAuthorization();

// ===== Hóa đơn VAT Bán xe NPP xuất Đại lý (VAT Car Invoice - DMS.Sales VAT_HTCInvoice) =====
app.MapPost("/api/invoices", async (CreateCarInvoiceDto dto, ICarInvoiceService svc) =>
{
    try { return Results.Ok(await svc.CreateAsync(dto)); }
    catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
}).RequireAuthorization();

app.MapGet("/api/invoices", async (ICarInvoiceService svc, string? status, string? dealer, string? invoiceType, string? issuer) =>
    Results.Ok(await svc.ListAsync(status, dealer, invoiceType, issuer))).RequireAuthorization();

app.MapGet("/api/invoices/stats", async (ICarInvoiceService svc) =>
    Results.Ok(await svc.StatsAsync())).RequireAuthorization();

app.MapGet("/api/invoices/{invoiceCode}", async (string invoiceCode, ICarInvoiceService svc) =>
{
    var r = await svc.DetailAsync(invoiceCode);
    return r is null ? Results.NotFound(new { invoiceCode }) : Results.Ok(r);
}).RequireAuthorization();

app.MapPost("/api/invoices/{invoiceCode}/issue", async (string invoiceCode, IssueCarInvoiceDto? dto, ICarInvoiceService svc) =>
{
    try
    {
        var r = await svc.IssueAsync(invoiceCode, dto);
        return r is null ? Results.NotFound(new { invoiceCode }) : Results.Ok(r);
    }
    catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
}).RequireAuthorization();

app.MapPost("/api/invoices/{invoiceCode}/adjust", async (string invoiceCode, CreateAdjustInvoiceDto dto, ICarInvoiceService svc) =>
{
    try
    {
        var r = await svc.CreateAdjustAsync(invoiceCode, dto);
        return r is null ? Results.NotFound(new { invoiceCode }) : Results.Ok(r);
    }
    catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
}).RequireAuthorization();

app.MapPost("/api/invoices/{invoiceCode}/replace", async (string invoiceCode, CreateReplaceInvoiceDto dto, ICarInvoiceService svc) =>
{
    try
    {
        var r = await svc.CreateReplaceAsync(invoiceCode, dto);
        return r is null ? Results.NotFound(new { invoiceCode }) : Results.Ok(r);
    }
    catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
}).RequireAuthorization();

app.MapPost("/api/invoices/{invoiceCode}/cancel", async (string invoiceCode, CancelCarInvoiceDto dto, ICarInvoiceService svc) =>
{
    try
    {
        var r = await svc.CancelAsync(invoiceCode, dto);
        return r is null ? Results.NotFound(new { invoiceCode }) : Results.Ok(r);
    }
    catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
}).RequireAuthorization();

app.MapDelete("/api/invoices/{invoiceCode}", async (string invoiceCode, ICarInvoiceService svc) =>
{
    try
    {
        var r = await svc.DeleteDraftAsync(invoiceCode);
        return r is null ? Results.NotFound(new { invoiceCode }) : Results.Ok(r);
    }
    catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
}).RequireAuthorization();

// ===== Lệnh thu hồi xe (Car Retrieve Order - DMS.Sales Sto_CarRetrieve) =====
app.MapPost("/api/retrievals", async (CreateCarRetrieveDto dto, ICarRetrieveService svc) =>
{
    try { return Results.Ok(await svc.CreateAsync(dto)); }
    catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
}).RequireAuthorization();

app.MapGet("/api/retrievals", async (ICarRetrieveService svc, string? status, string? dealer, string? vin, string? carId) =>
    Results.Ok(await svc.ListAsync(status, dealer, vin, carId))).RequireAuthorization();

app.MapGet("/api/retrievals/stats", async (ICarRetrieveService svc) =>
    Results.Ok(await svc.StatsAsync())).RequireAuthorization();

app.MapGet("/api/retrievals/{orderNo}", async (string orderNo, ICarRetrieveService svc) =>
{
    var r = await svc.DetailAsync(orderNo);
    return r is null ? Results.NotFound(new { retrieveOrderNo = orderNo }) : Results.Ok(r);
}).RequireAuthorization();

app.MapPost("/api/retrievals/{orderNo}/approve", async (string orderNo, ApproveCarRetrieveDto? dto, ICarRetrieveService svc) =>
{
    try
    {
        var r = await svc.ApproveAsync(orderNo, dto);
        return r is null ? Results.NotFound(new { retrieveOrderNo = orderNo }) : Results.Ok(r);
    }
    catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
}).RequireAuthorization();

app.MapPost("/api/retrievals/{orderNo}/reject", async (string orderNo, RejectCarRetrieveDto dto, ICarRetrieveService svc) =>
{
    try
    {
        var r = await svc.RejectAsync(orderNo, dto);
        return r is null ? Results.NotFound(new { retrieveOrderNo = orderNo }) : Results.Ok(r);
    }
    catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
}).RequireAuthorization();

app.MapPost("/api/retrievals/{orderNo}/cancel", async (string orderNo, CancelCarRetrieveDto? dto, ICarRetrieveService svc) =>
{
    try
    {
        var r = await svc.CancelAsync(orderNo, dto);
        return r is null ? Results.NotFound(new { retrieveOrderNo = orderNo }) : Results.Ok(r);
    }
    catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
}).RequireAuthorization();

app.MapPost("/api/retrievals/{orderNo}/complete", async (string orderNo, CompleteCarRetrieveDto? dto, ICarRetrieveService svc) =>
{
    try
    {
        var r = await svc.CompleteAsync(orderNo, dto);
        return r is null ? Results.NotFound(new { retrieveOrderNo = orderNo }) : Results.Ok(r);
    }
    catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
}).RequireAuthorization();

app.MapPost("/api/retrievals/{orderNo}/cars", async (string orderNo, AddCarRetrieveDetailDto dto, ICarRetrieveService svc) =>
{
    try
    {
        var r = await svc.AddCarAsync(orderNo, dto);
        return r is null ? Results.NotFound(new { retrieveOrderNo = orderNo }) : Results.Ok(r);
    }
    catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
}).RequireAuthorization();

app.MapDelete("/api/retrievals/{orderNo}/cars/{detailId:long}", async (string orderNo, long detailId, ICarRetrieveService svc) =>
{
    try
    {
        var r = await svc.RemoveCarAsync(orderNo, detailId);
        return r is null ? Results.NotFound(new { retrieveOrderNo = orderNo, detailId }) : Results.Ok(r);
    }
    catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
}).RequireAuthorization();

// ===== Quản lý Đề nghị Hủy Hợp đồng Bán lẻ Xe (Dlr_ContractCancel - DMS.Sales DlrContractCancelController) =====
app.MapGet("/api/contract-cancels/reasons", async (IContractCancelService svc) =>
    Results.Ok(await svc.GetReasonsAsync())).RequireAuthorization();

app.MapPost("/api/contract-cancels", async (CreateContractCancelDto dto, IContractCancelService svc) =>
{
    try { return Results.Ok(await svc.CreateAsync(dto)); }
    catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
}).RequireAuthorization();

app.MapGet("/api/contract-cancels", async (IContractCancelService svc, string? status, string? dealer, string? orderCode, string? contractCNo) =>
    Results.Ok(await svc.ListAsync(status, dealer, orderCode, contractCNo))).RequireAuthorization();

app.MapGet("/api/contract-cancels/stats", async (IContractCancelService svc) =>
    Results.Ok(await svc.StatsAsync())).RequireAuthorization();

app.MapGet("/api/contract-cancels/{contractCNo}", async (string contractCNo, IContractCancelService svc) =>
{
    var r = await svc.DetailAsync(contractCNo);
    return r is null ? Results.NotFound(new { contractCNo }) : Results.Ok(r);
}).RequireAuthorization();

app.MapPost("/api/contract-cancels/{contractCNo}/approve", async (string contractCNo, ApproveContractCancelDto? dto, IContractCancelService svc) =>
{
    try
    {
        var r = await svc.ApproveAsync(contractCNo, dto);
        return r is null ? Results.NotFound(new { contractCNo }) : Results.Ok(r);
    }
    catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
}).RequireAuthorization();

app.MapPost("/api/contract-cancels/{contractCNo}/reject", async (string contractCNo, RejectContractCancelDto dto, IContractCancelService svc) =>
{
    try
    {
        var r = await svc.RejectAsync(contractCNo, dto);
        return r is null ? Results.NotFound(new { contractCNo }) : Results.Ok(r);
    }
    catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
}).RequireAuthorization();

app.MapPost("/api/contract-cancels/{contractCNo}/cancel", async (string contractCNo, CancelContractCancelDto? dto, IContractCancelService svc) =>
{
    try
    {
        var r = await svc.CancelAsync(contractCNo, dto);
        return r is null ? Results.NotFound(new { contractCNo }) : Results.Ok(r);
    }
    catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
}).RequireAuthorization();

app.MapPost("/api/contract-cancels/{contractCNo}/cars", async (string contractCNo, AddContractCancelCarDto dto, IContractCancelService svc) =>
{
    try
    {
        var r = await svc.AddCarAsync(contractCNo, dto);
        return r is null ? Results.NotFound(new { contractCNo }) : Results.Ok(r);
    }
    catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
}).RequireAuthorization();

app.MapDelete("/api/contract-cancels/{contractCNo}/cars/{carId:long}", async (string contractCNo, long carId, IContractCancelService svc) =>
{
    try
    {
        var r = await svc.RemoveCarAsync(contractCNo, carId);
        return r is null ? Results.NotFound(new { contractCNo, carId }) : Results.Ok(r);
    }
    catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
}).RequireAuthorization();

// ===== Biên bản Bàn giao xe / Giao nhận xe (Car Transport Minutes - BBBG - DMS.Sales Car_TransportMinutes) =====
app.MapPost("/api/transport-minutes", async (CreateTransportMinutesDto dto, ICarTransportMinutesService svc) =>
{
    try { return Results.Ok(await svc.CreateAsync(dto)); }
    catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
}).RequireAuthorization();

app.MapGet("/api/transport-minutes", async (ICarTransportMinutesService svc, string? status, string? dealer, string? vin, string? doNo, string? minutesNo) =>
    Results.Ok(await svc.ListAsync(status, dealer, vin, doNo, minutesNo))).RequireAuthorization();

app.MapGet("/api/transport-minutes/stats", async (ICarTransportMinutesService svc) =>
    Results.Ok(await svc.StatsAsync())).RequireAuthorization();

app.MapGet("/api/transport-minutes/{minutesNo}", async (string minutesNo, ICarTransportMinutesService svc) =>
{
    var r = await svc.DetailAsync(minutesNo);
    return r is null ? Results.NotFound(new { transportMinutesNo = minutesNo }) : Results.Ok(r);
}).RequireAuthorization();

app.MapPost("/api/transport-minutes/{minutesNo}/approve-dl", async (string minutesNo, ApproveDLTransportMinutesDto? dto, ICarTransportMinutesService svc) =>
{
    try
    {
        var r = await svc.ApproveDLAsync(minutesNo, dto);
        return r is null ? Results.NotFound(new { transportMinutesNo = minutesNo }) : Results.Ok(r);
    }
    catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
}).RequireAuthorization();

app.MapPost("/api/transport-minutes/{minutesNo}/approve-hq", async (string minutesNo, ApproveHQTransportMinutesDto? dto, ICarTransportMinutesService svc) =>
{
    try
    {
        var r = await svc.ApproveHQAsync(minutesNo, dto);
        return r is null ? Results.NotFound(new { transportMinutesNo = minutesNo }) : Results.Ok(r);
    }
    catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
}).RequireAuthorization();

app.MapPost("/api/transport-minutes/{minutesNo}/cancel", async (string minutesNo, CancelTransportMinutesDto dto, ICarTransportMinutesService svc) =>
{
    try
    {
        var r = await svc.CancelAsync(minutesNo, dto);
        return r is null ? Results.NotFound(new { transportMinutesNo = minutesNo }) : Results.Ok(r);
    }
    catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
}).RequireAuthorization();

app.MapDelete("/api/transport-minutes/{minutesNo}", async (string minutesNo, ICarTransportMinutesService svc) =>
{
    try
    {
        var r = await svc.DeleteDraftAsync(minutesNo);
        return r is null ? Results.NotFound(new { transportMinutesNo = minutesNo }) : Results.Ok(r);
    }
    catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
}).RequireAuthorization();

app.MapPost("/api/transport-minutes/{minutesNo}/cars", async (string minutesNo, AddCarToTransportMinutesDto dto, ICarTransportMinutesService svc) =>
{
    try
    {
        var r = await svc.AddCarAsync(minutesNo, dto);
        return r is null ? Results.NotFound(new { transportMinutesNo = minutesNo }) : Results.Ok(r);
    }
    catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
}).RequireAuthorization();

app.MapDelete("/api/transport-minutes/{minutesNo}/cars/{detailId:long}", async (string minutesNo, long detailId, ICarTransportMinutesService svc) =>
{
    try
    {
        var r = await svc.RemoveCarAsync(minutesNo, detailId);
        return r is null ? Results.NotFound(new { transportMinutesNo = minutesNo, detailId }) : Results.Ok(r);
    }
    catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
}).RequireAuthorization();

// ===== Hợp đồng Mua bán buôn xe Đại lý - NPP (Wholesale Dealer Contract - DMS.Sales CT_DealerContract / DMS40_CT_DealerContract) =====
app.MapPost("/api/dealer-contracts", async (CreateDealerContractDto dto, IDealerContractService svc) =>
{
    try { return Results.Ok(await svc.CreateAsync(dto)); }
    catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
}).RequireAuthorization();

app.MapPost("/api/dealer-contracts/adjust", async (CreateAdjustDealerContractDto dto, IDealerContractService svc) =>
{
    try { return Results.Ok(await svc.CreateAdjustAsync(dto)); }
    catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
}).RequireAuthorization();

app.MapGet("/api/dealer-contracts", async (IDealerContractService svc, string? status, string? dealer, string? paymentType, string? contractType, string? contractNo) =>
    Results.Ok(await svc.ListAsync(status, dealer, paymentType, contractType, contractNo))).RequireAuthorization();

app.MapGet("/api/dealer-contracts/stats", async (IDealerContractService svc) =>
    Results.Ok(await svc.StatsAsync())).RequireAuthorization();

app.MapGet("/api/dealer-contracts/{contractNo}", async (string contractNo, IDealerContractService svc) =>
{
    var r = await svc.DetailAsync(contractNo);
    return r is null ? Results.NotFound(new { contractNo }) : Results.Ok(r);
}).RequireAuthorization();

app.MapPut("/api/dealer-contracts/{contractNo}", async (string contractNo, UpdateDealerContractDto dto, IDealerContractService svc) =>
{
    try
    {
        var r = await svc.UpdateAsync(contractNo, dto);
        return r is null ? Results.NotFound(new { contractNo }) : Results.Ok(r);
    }
    catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
}).RequireAuthorization();

app.MapPost("/api/dealer-contracts/{contractNo}/bank", async (string contractNo, UpdateContractBankDto dto, IDealerContractService svc) =>
{
    try
    {
        var r = await svc.UpdateBankCodeAsync(contractNo, dto);
        return r is null ? Results.NotFound(new { contractNo }) : Results.Ok(r);
    }
    catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
}).RequireAuthorization();

app.MapPost("/api/dealer-contracts/{contractNo}/approve1", async (string contractNo, Approve1DealerContractDto? dto, IDealerContractService svc) =>
{
    try
    {
        var r = await svc.Approve1HQAsync(contractNo, dto);
        return r is null ? Results.NotFound(new { contractNo }) : Results.Ok(r);
    }
    catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
}).RequireAuthorization();

app.MapPost("/api/dealer-contracts/{contractNo}/approve-dl", async (string contractNo, SignDealerContractDto? dto, IDealerContractService svc) =>
{
    try
    {
        var r = await svc.ApproveDLAsync(contractNo, dto);
        return r is null ? Results.NotFound(new { contractNo }) : Results.Ok(r);
    }
    catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
}).RequireAuthorization();

app.MapPost("/api/dealer-contracts/{contractNo}/approve2", async (string contractNo, Approve2HQDealerContractDto? dto, IDealerContractService svc) =>
{
    try
    {
        var r = await svc.Approve2HQAsync(contractNo, dto);
        return r is null ? Results.NotFound(new { contractNo }) : Results.Ok(r);
    }
    catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
}).RequireAuthorization();

app.MapPost("/api/dealer-contracts/{contractNo}/reject", async (string contractNo, RejectDealerContractDto dto, IDealerContractService svc) =>
{
    try
    {
        var r = await svc.RejectHQAsync(contractNo, dto);
        return r is null ? Results.NotFound(new { contractNo }) : Results.Ok(r);
    }
    catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
}).RequireAuthorization();

app.MapPost("/api/dealer-contracts/{contractNo}/cancel", async (string contractNo, CancelDealerContractDto dto, IDealerContractService svc) =>
{
    try
    {
        var r = await svc.CancelDLAsync(contractNo, dto);
        return r is null ? Results.NotFound(new { contractNo }) : Results.Ok(r);
    }
    catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
}).RequireAuthorization();

app.MapDelete("/api/dealer-contracts/{contractNo}", async (string contractNo, IDealerContractService svc) =>
{
    try
    {
        var r = await svc.DeleteDraftAsync(contractNo);
        return r is null ? Results.NotFound(new { contractNo }) : Results.Ok(r);
    }
    catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
}).RequireAuthorization();

app.MapPost("/api/dealer-contracts/{contractNo}/cars", async (string contractNo, AddDealerContractCarDto dto, IDealerContractService svc) =>
{
    try
    {
        var r = await svc.AddCarAsync(contractNo, dto);
        return r is null ? Results.NotFound(new { contractNo }) : Results.Ok(r);
    }
    catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
}).RequireAuthorization();

app.MapDelete("/api/dealer-contracts/{contractNo}/cars/{detailId:long}", async (string contractNo, long detailId, IDealerContractService svc) =>
{
    try
    {
        var r = await svc.RemoveCarAsync(contractNo, detailId);
        return r is null ? Results.NotFound(new { contractNo, detailId }) : Results.Ok(r);
    }
    catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
}).RequireAuthorization();

// ===== Hợp đồng Bán lẻ xe Ô tô Đại lý (Retail Contract - DMS.Sales Dlr_Contract / DlrContractController / DEALER_RETAIL_CONTRACT_FLOW.md) =====
app.MapPost("/api/retail-contracts", async (CreateRetailContractDto dto, IRetailContractService svc) =>
{
    try { return Results.Ok(await svc.CreateAsync(dto)); }
    catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
}).RequireAuthorization();

app.MapGet("/api/retail-contracts", async (IRetailContractService svc, string? status, string? dealer, string? customer, string? paymentType, string? contractNo) =>
    Results.Ok(await svc.ListAsync(status, dealer, customer, paymentType, contractNo))).RequireAuthorization();

app.MapGet("/api/retail-contracts/stats", async (IRetailContractService svc) =>
    Results.Ok(await svc.StatsAsync())).RequireAuthorization();

app.MapGet("/api/retail-contracts/{contractNo}", async (string contractNo, IRetailContractService svc) =>
{
    var r = await svc.DetailAsync(contractNo);
    return r is null ? Results.NotFound(new { contractNo }) : Results.Ok(r);
}).RequireAuthorization();

app.MapPut("/api/retail-contracts/{contractNo}", async (string contractNo, UpdateRetailContractDto dto, IRetailContractService svc) =>
{
    try
    {
        var r = await svc.UpdateAsync(contractNo, dto);
        return r is null ? Results.NotFound(new { contractNo }) : Results.Ok(r);
    }
    catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
}).RequireAuthorization();

app.MapPost("/api/retail-contracts/{contractNo}/bank", async (string contractNo, UpdateRetailContractBankDto dto, IRetailContractService svc) =>
{
    try
    {
        var r = await svc.UpdateBankAsync(contractNo, dto);
        return r is null ? Results.NotFound(new { contractNo }) : Results.Ok(r);
    }
    catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
}).RequireAuthorization();

app.MapPost("/api/retail-contracts/{contractNo}/salesman", async (string contractNo, UpdateRetailContractSMDto dto, IRetailContractService svc) =>
{
    try
    {
        var r = await svc.UpdateSalesmanAsync(contractNo, dto);
        return r is null ? Results.NotFound(new { contractNo }) : Results.Ok(r);
    }
    catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
}).RequireAuthorization();

app.MapPost("/api/retail-contracts/{contractNo}/approve", async (string contractNo, ApproveRetailContractDto? dto, IRetailContractService svc) =>
{
    try
    {
        var r = await svc.ApproveAsync(contractNo, dto);
        return r is null ? Results.NotFound(new { contractNo }) : Results.Ok(r);
    }
    catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
}).RequireAuthorization();

app.MapPost("/api/retail-contracts/{contractNo}/cancel", async (string contractNo, CancelRetailContractDto dto, IRetailContractService svc) =>
{
    try
    {
        var r = await svc.CancelAsync(contractNo, dto);
        return r is null ? Results.NotFound(new { contractNo }) : Results.Ok(r);
    }
    catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
}).RequireAuthorization();

app.MapDelete("/api/retail-contracts/{contractNo}", async (string contractNo, IRetailContractService svc) =>
{
    try
    {
        var r = await svc.DeleteDraftAsync(contractNo);
        return r is null ? Results.NotFound(new { contractNo }) : Results.Ok(r);
    }
    catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
}).RequireAuthorization();

app.MapPost("/api/retail-contracts/{contractNo}/assign-vin", async (string contractNo, AssignRetailCarVinDto dto, IRetailContractService svc) =>
{
    try
    {
        var r = await svc.AssignVinAsync(contractNo, dto);
        return r is null ? Results.NotFound(new { contractNo }) : Results.Ok(r);
    }
    catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
}).RequireAuthorization();

app.MapPost("/api/retail-contracts/{contractNo}/deliver", async (string contractNo, DeliverRetailCarDto dto, IRetailContractService svc) =>
{
    try
    {
        var r = await svc.DeliverCarAsync(contractNo, dto);
        return r is null ? Results.NotFound(new { contractNo }) : Results.Ok(r);
    }
    catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
}).RequireAuthorization();

app.MapPost("/api/retail-contracts/approve-multi", async (BatchApproveRetailContractDto dto, IRetailContractService svc) =>
{
    try { return Results.Ok(await svc.ApproveMultiAsync(dto)); }
    catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
}).RequireAuthorization();

app.MapPost("/api/retail-contracts/cancel-multi", async (BatchCancelRetailContractDto dto, IRetailContractService svc) =>
{
    try { return Results.Ok(await svc.CancelMultiAsync(dto)); }
    catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
}).RequireAuthorization();

// ===== Quản lý Thanh toán tiền xe Đại lý - NPP / Ủy nhiệm chi UNC (Dealer Payment - DMS.Sales Pmt_Payment / PmtPaymentController) =====
app.MapPost("/api/dealer-payments", async (CreateDealerPaymentDto dto, IDealerPaymentService svc) =>
{
    try { return Results.Ok(await svc.CreateAsync(dto)); }
    catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
}).RequireAuthorization();

app.MapGet("/api/dealer-payments", async (IDealerPaymentService svc, string? status, string? dealer, string? paymentType, string? contractNo, string? paymentNo) =>
    Results.Ok(await svc.ListAsync(status, dealer, paymentType, contractNo, paymentNo))).RequireAuthorization();

app.MapGet("/api/dealer-payments/stats", async (IDealerPaymentService svc) =>
    Results.Ok(await svc.StatsAsync())).RequireAuthorization();

app.MapGet("/api/dealer-payments/{paymentNo}", async (string paymentNo, IDealerPaymentService svc) =>
{
    var r = await svc.DetailAsync(paymentNo);
    return r is null ? Results.NotFound(new { paymentNo }) : Results.Ok(r);
}).RequireAuthorization();

app.MapPut("/api/dealer-payments/{paymentNo}", async (string paymentNo, UpdateDealerPaymentDto dto, IDealerPaymentService svc) =>
{
    try
    {
        var r = await svc.UpdateAsync(paymentNo, dto);
        return r is null ? Results.NotFound(new { paymentNo }) : Results.Ok(r);
    }
    catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
}).RequireAuthorization();

app.MapPost("/api/dealer-payments/{paymentNo}/approve", async (string paymentNo, ApproveDealerPaymentDto? dto, IDealerPaymentService svc) =>
{
    try
    {
        var r = await svc.ApproveAsync(paymentNo, dto);
        return r is null ? Results.NotFound(new { paymentNo }) : Results.Ok(r);
    }
    catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
}).RequireAuthorization();

app.MapPost("/api/dealer-payments/{paymentNo}/confirm-accounting", async (string paymentNo, ConfirmAccountingRecordDto dto, IDealerPaymentService svc) =>
{
    try
    {
        var r = await svc.ConfirmAccountingAsync(paymentNo, dto);
        return r is null ? Results.NotFound(new { paymentNo }) : Results.Ok(r);
    }
    catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
}).RequireAuthorization();

app.MapPost("/api/dealer-payments/{paymentNo}/reject", async (string paymentNo, RejectDealerPaymentDto dto, IDealerPaymentService svc) =>
{
    try
    {
        var r = await svc.RejectAsync(paymentNo, dto);
        return r is null ? Results.NotFound(new { paymentNo }) : Results.Ok(r);
    }
    catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
}).RequireAuthorization();

app.MapPost("/api/dealer-payments/{paymentNo}/cancel", async (string paymentNo, CancelDealerPaymentDto dto, IDealerPaymentService svc) =>
{
    try
    {
        var r = await svc.CancelAsync(paymentNo, dto);
        return r is null ? Results.NotFound(new { paymentNo }) : Results.Ok(r);
    }
    catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
}).RequireAuthorization();

app.MapDelete("/api/dealer-payments/{paymentNo}", async (string paymentNo, IDealerPaymentService svc) =>
{
    try
    {
        var r = await svc.DeleteDraftAsync(paymentNo);
        return r is null ? Results.NotFound(new { paymentNo }) : Results.Ok(r);
    }
    catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
}).RequireAuthorization();

app.MapPost("/api/dealer-payments/{paymentNo}/cars", async (string paymentNo, AddDealerPaymentCarDto dto, IDealerPaymentService svc) =>
{
    try
    {
        var r = await svc.AddCarAsync(paymentNo, dto);
        return r is null ? Results.NotFound(new { paymentNo }) : Results.Ok(r);
    }
    catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
}).RequireAuthorization();

app.MapDelete("/api/dealer-payments/{paymentNo}/cars/{detailId:long}", async (string paymentNo, long detailId, IDealerPaymentService svc) =>
{
    try
    {
        var r = await svc.RemoveCarAsync(paymentNo, detailId);
        return r is null ? Results.NotFound(new { paymentNo, detailId }) : Results.Ok(r);
    }
    catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
}).RequireAuthorization();

// ===== Quản lý Giao dịch bán lẻ xe Ô tô Đại lý (Retail Deal - DMS.Sales DLS_Deal / DlsDealController / DLS_DEAL_CREATE_FLOW.md) =====
app.MapPost("/api/deals", async (CreateRetailDealDto dto, IRetailDealService svc) =>
{
    try { return Results.Ok(await svc.CreateAsync(dto)); }
    catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
}).RequireAuthorization();

app.MapGet("/api/deals", async (IRetailDealService svc, string? status, string? dealer, string? vin, string? customer, string? ctmCare, string? dealNo) =>
    Results.Ok(await svc.ListAsync(status, dealer, vin, customer, ctmCare, dealNo))).RequireAuthorization();

app.MapGet("/api/deals/stats", async (IRetailDealService svc) =>
    Results.Ok(await svc.StatsAsync())).RequireAuthorization();

app.MapGet("/api/deals/{dealNo}", async (string dealNo, IRetailDealService svc) =>
{
    var r = await svc.DetailAsync(dealNo);
    return r is null ? Results.NotFound(new { dealNo }) : Results.Ok(r);
}).RequireAuthorization();

app.MapPut("/api/deals/{dealNo}", async (string dealNo, UpdateRetailDealDto dto, IRetailDealService svc) =>
{
    try
    {
        var r = await svc.UpdateAsync(dealNo, dto);
        return r is null ? Results.NotFound(new { dealNo }) : Results.Ok(r);
    }
    catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
}).RequireAuthorization();

app.MapPost("/api/deals/{dealNo}/plate-no", async (string dealNo, UpdateDealPlateNoDto dto, IRetailDealService svc) =>
{
    try
    {
        var r = await svc.UpdatePlateNoAsync(dealNo, dto);
        return r is null ? Results.NotFound(new { dealNo }) : Results.Ok(r);
    }
    catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
}).RequireAuthorization();

app.MapPost("/api/deals/{dealNo}/cus-invoice", async (string dealNo, UpdateDealCusInvoiceDto dto, IRetailDealService svc) =>
{
    try
    {
        var r = await svc.UpdateCusInvoiceAsync(dealNo, dto);
        return r is null ? Results.NotFound(new { dealNo }) : Results.Ok(r);
    }
    catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
}).RequireAuthorization();

app.MapPost("/api/deals/{dealNo}/verify-ctmcare", async (string dealNo, VerifyDealCtmCareDto? dto, IRetailDealService svc) =>
{
    try
    {
        var r = await svc.VerifyCtmCareAsync(dealNo, dto);
        return r is null ? Results.NotFound(new { dealNo }) : Results.Ok(r);
    }
    catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
}).RequireAuthorization();

app.MapPost("/api/deals/{dealNo}/deliver", async (string dealNo, DeliverDealCarDto dto, IRetailDealService svc) =>
{
    try
    {
        var r = await svc.DeliverCarAsync(dealNo, dto);
        return r is null ? Results.NotFound(new { dealNo }) : Results.Ok(r);
    }
    catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
}).RequireAuthorization();

app.MapPost("/api/deals/{dealNo}/cancel", async (string dealNo, CancelDealerDealDto dto, IRetailDealService svc) =>
{
    try
    {
        var r = await svc.CancelAsync(dealNo, dto);
        return r is null ? Results.NotFound(new { dealNo }) : Results.Ok(r);
    }
    catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
}).RequireAuthorization();

app.MapDelete("/api/deals/{dealNo}", async (string dealNo, IRetailDealService svc) =>
{
    try
    {
        var r = await svc.DeleteDraftAsync(dealNo);
        return r is null ? Results.NotFound(new { dealNo }) : Results.Ok(r);
    }
    catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
}).RequireAuthorization();

app.MapPost("/api/deals/{dealNo}/cars", async (string dealNo, AddDealerDealCarDto dto, IRetailDealService svc) =>
{
    try
    {
        var r = await svc.AddCarAsync(dealNo, dto);
        return r is null ? Results.NotFound(new { dealNo }) : Results.Ok(r);
    }
    catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
}).RequireAuthorization();

app.MapDelete("/api/deals/{dealNo}/cars/{detailId:long}", async (string dealNo, long detailId, IRetailDealService svc) =>
{
    try
    {
        var r = await svc.RemoveCarAsync(dealNo, detailId);
        return r is null ? Results.NotFound(new { dealNo, detailId }) : Results.Ok(r);
    }
    catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
}).RequireAuthorization();

app.MapPost("/api/deals/{dealNo}/attachments", async (string dealNo, AddDealAttachDto dto, IRetailDealService svc) =>
{
    try
    {
        var r = await svc.AddAttachmentAsync(dealNo, dto);
        return r is null ? Results.NotFound(new { dealNo }) : Results.Ok(r);
    }
    catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
}).RequireAuthorization();

app.MapDelete("/api/deals/{dealNo}/attachments/{attachId:long}", async (string dealNo, long attachId, IRetailDealService svc) =>
{
    try
    {
        var r = await svc.RemoveAttachmentAsync(dealNo, attachId);
        return r is null ? Results.NotFound(new { dealNo, attachId }) : Results.Ok(r);
    }
    catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
}).RequireAuthorization();

// ===== Quản lý Lệnh điều chuyển kho xe ô tô (Storage Rearrange - DMS.Sales Sto_StorageRearrange / StoStorageRearrangeController / Storage.1.cs) =====
app.MapPost("/api/storage-rearranges", async (CreateStorageRearrangeDto dto, IStorageRearrangeService svc) =>
{
    try { return Results.Ok(await svc.CreateAsync(dto)); }
    catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
}).RequireAuthorization();

app.MapGet("/api/storage-rearranges", async (IStorageRearrangeService svc, string? status, string? storageFrom, string? storageTo, string? vin, string? carId) =>
    Results.Ok(await svc.ListAsync(status, storageFrom, storageTo, vin, carId))).RequireAuthorization();

app.MapGet("/api/storage-rearranges/stats", async (IStorageRearrangeService svc) =>
    Results.Ok(await svc.StatsAsync())).RequireAuthorization();

app.MapGet("/api/storage-rearranges/{rearrangeNo}", async (string rearrangeNo, IStorageRearrangeService svc) =>
{
    var r = await svc.DetailAsync(rearrangeNo);
    return r is null ? Results.NotFound(new { error = $"Không tìm thấy lệnh điều chuyển kho {rearrangeNo}." }) : Results.Ok(r);
}).RequireAuthorization();

app.MapPut("/api/storage-rearranges/{rearrangeNo}", async (string rearrangeNo, UpdateStorageRearrangeDto dto, IStorageRearrangeService svc) =>
{
    try
    {
        var r = await svc.UpdateAsync(rearrangeNo, dto);
        return r is null ? Results.NotFound(new { error = $"Không tìm thấy lệnh điều chuyển kho {rearrangeNo}." }) : Results.Ok(r);
    }
    catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
}).RequireAuthorization();

app.MapDelete("/api/storage-rearranges/{rearrangeNo}", async (string rearrangeNo, IStorageRearrangeService svc) =>
{
    try
    {
        var r = await svc.DeleteAsync(rearrangeNo);
        return r is null ? Results.NotFound(new { error = $"Không tìm thấy lệnh điều chuyển kho {rearrangeNo}." }) : Results.Ok(r);
    }
    catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
}).RequireAuthorization();

app.MapPost("/api/storage-rearranges/{rearrangeNo}/approve1", async (string rearrangeNo, Approve1RearrangeDto? dto, IStorageRearrangeService svc) =>
{
    try
    {
        var r = await svc.Approve1Async(rearrangeNo, dto);
        return r is null ? Results.NotFound(new { error = $"Không tìm thấy lệnh điều chuyển kho {rearrangeNo}." }) : Results.Ok(r);
    }
    catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
}).RequireAuthorization();

app.MapPost("/api/storage-rearranges/{rearrangeNo}/approve2", async (string rearrangeNo, Approve2RearrangeDto? dto, IStorageRearrangeService svc) =>
{
    try
    {
        var r = await svc.Approve2Async(rearrangeNo, dto);
        return r is null ? Results.NotFound(new { error = $"Không tìm thấy lệnh điều chuyển kho {rearrangeNo}." }) : Results.Ok(r);
    }
    catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
}).RequireAuthorization();

app.MapPost("/api/storage-rearranges/{rearrangeNo}/reject", async (string rearrangeNo, RejectRearrangeDto dto, IStorageRearrangeService svc) =>
{
    try
    {
        var r = await svc.RejectAsync(rearrangeNo, dto);
        return r is null ? Results.NotFound(new { error = $"Không tìm thấy lệnh điều chuyển kho {rearrangeNo}." }) : Results.Ok(r);
    }
    catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
}).RequireAuthorization();

app.MapPost("/api/storage-rearranges/{rearrangeNo}/cancel", async (string rearrangeNo, CancelRearrangeDto dto, IStorageRearrangeService svc) =>
{
    try
    {
        var r = await svc.CancelAsync(rearrangeNo, dto);
        return r is null ? Results.NotFound(new { error = $"Không tìm thấy lệnh điều chuyển kho {rearrangeNo}." }) : Results.Ok(r);
    }
    catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
}).RequireAuthorization();

app.MapPut("/api/storage-rearranges/{rearrangeNo}/cars/{vin}", async (string rearrangeNo, string vin, UpdateRearrangeDetailDto dto, IStorageRearrangeService svc) =>
{
    try
    {
        var r = await svc.UpdateDetailAsync(rearrangeNo, vin, dto);
        return r is null ? Results.NotFound(new { error = $"Không tìm thấy lệnh điều chuyển kho {rearrangeNo}." }) : Results.Ok(r);
    }
    catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
}).RequireAuthorization();

app.MapPost("/api/storage-rearranges/{rearrangeNo}/cars", async (string rearrangeNo, AddRearrangeDetailDto dto, IStorageRearrangeService svc) =>
{
    try
    {
        var r = await svc.AddCarAsync(rearrangeNo, dto);
        return r is null ? Results.NotFound(new { error = $"Không tìm thấy lệnh điều chuyển kho {rearrangeNo}." }) : Results.Ok(r);
    }
    catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
}).RequireAuthorization();

app.MapDelete("/api/storage-rearranges/{rearrangeNo}/cars/{vin}", async (string rearrangeNo, string vin, IStorageRearrangeService svc) =>
{
    try
    {
        var r = await svc.RemoveCarAsync(rearrangeNo, vin);
        return r is null ? Results.NotFound(new { error = $"Không tìm thấy lệnh điều chuyển kho {rearrangeNo}." }) : Results.Ok(r);
    }
    catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
}).RequireAuthorization();

// ===== Quản lý Yêu cầu Vận tải xe (Transportation Request - DMS.Sales Sto_TranspReq / StoTranspReqController / Storage.cs) =====
app.MapPost("/api/transport-requests", async (CreateTransportRequestDto dto, ITransportRequestService svc) =>
{
    try { return Results.Ok(await svc.CreateAsync(dto)); }
    catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
}).RequireAuthorization();

app.MapGet("/api/transport-requests", async (ITransportRequestService svc, string? status, string? transporterCode, string? dealerCode, string? vin, string? refOrdNo, string? transpReqNo) =>
    Results.Ok(await svc.ListAsync(status, transporterCode, dealerCode, vin, refOrdNo, transpReqNo))).RequireAuthorization();

app.MapGet("/api/transport-requests/stats", async (ITransportRequestService svc) =>
    Results.Ok(await svc.StatsAsync())).RequireAuthorization();

app.MapGet("/api/transport-requests/{transpReqNo}", async (string transpReqNo, ITransportRequestService svc) =>
{
    var r = await svc.DetailAsync(transpReqNo);
    return r is null ? Results.NotFound(new { error = $"Không tìm thấy yêu cầu vận tải {transpReqNo}." }) : Results.Ok(r);
}).RequireAuthorization();

app.MapPut("/api/transport-requests/{transpReqNo}", async (string transpReqNo, UpdateTransportRequestDto dto, ITransportRequestService svc) =>
{
    try
    {
        var r = await svc.UpdateAsync(transpReqNo, dto);
        return r is null ? Results.NotFound(new { error = $"Không tìm thấy yêu cầu vận tải {transpReqNo}." }) : Results.Ok(r);
    }
    catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
}).RequireAuthorization();

app.MapDelete("/api/transport-requests/{transpReqNo}", async (string transpReqNo, ITransportRequestService svc) =>
{
    try
    {
        var r = await svc.DeleteAsync(transpReqNo);
        return r is null ? Results.NotFound(new { error = $"Không tìm thấy yêu cầu vận tải {transpReqNo}." }) : Results.Ok(r);
    }
    catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
}).RequireAuthorization();

app.MapPost("/api/transport-requests/{transpReqNo}/approve", async (string transpReqNo, ApproveTransportRequestDto? dto, ITransportRequestService svc) =>
{
    try
    {
        var r = await svc.ApproveAsync(transpReqNo, dto);
        return r is null ? Results.NotFound(new { error = $"Không tìm thấy yêu cầu vận tải {transpReqNo}." }) : Results.Ok(r);
    }
    catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
}).RequireAuthorization();

app.MapPost("/api/transport-requests/{transpReqNo}/reject", async (string transpReqNo, RejectTransportRequestDto dto, ITransportRequestService svc) =>
{
    try
    {
        var r = await svc.RejectAsync(transpReqNo, dto);
        return r is null ? Results.NotFound(new { error = $"Không tìm thấy yêu cầu vận tải {transpReqNo}." }) : Results.Ok(r);
    }
    catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
}).RequireAuthorization();

app.MapPost("/api/transport-requests/{transpReqNo}/cancel", async (string transpReqNo, CancelTransportRequestDto dto, ITransportRequestService svc) =>
{
    try
    {
        var r = await svc.CancelAsync(transpReqNo, dto);
        return r is null ? Results.NotFound(new { error = $"Không tìm thấy yêu cầu vận tải {transpReqNo}." }) : Results.Ok(r);
    }
    catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
}).RequireAuthorization();

app.MapPost("/api/transport-requests/{transpReqNo}/complete", async (string transpReqNo, CompleteTransportRequestDto? dto, ITransportRequestService svc) =>
{
    try
    {
        var r = await svc.CompleteAsync(transpReqNo, dto);
        return r is null ? Results.NotFound(new { error = $"Không tìm thấy yêu cầu vận tải {transpReqNo}." }) : Results.Ok(r);
    }
    catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
}).RequireAuthorization();

app.MapPost("/api/transport-requests/{transpReqNo}/cars", async (string transpReqNo, AddTransportRequestCarDto dto, ITransportRequestService svc) =>
{
    try
    {
        var r = await svc.AddCarAsync(transpReqNo, dto);
        return r is null ? Results.NotFound(new { error = $"Không tìm thấy yêu cầu vận tải {transpReqNo}." }) : Results.Ok(r);
    }
    catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
}).RequireAuthorization();

app.MapDelete("/api/transport-requests/{transpReqNo}/cars/{vin}", async (string transpReqNo, string vin, ITransportRequestService svc) =>
{
    try
    {
        var r = await svc.RemoveCarAsync(transpReqNo, vin);
        return r is null ? Results.NotFound(new { error = $"Không tìm thấy yêu cầu vận tải {transpReqNo}." }) : Results.Ok(r);
    }
    catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
}).RequireAuthorization();

// ===== Quản lý Công văn Gia hạn & Phát hành Bảo lãnh thanh toán mua xe (Guarantee Extension - DMS.Sales Pmt_GrtClaimExt / PmtGrtClaimExtController / PaymentGrtExt.cs) =====
app.MapPost("/api/guarantee-extensions", async (CreatePaymentGuaranteeExtDto dto, IPaymentGuaranteeExtService svc) =>
{
    try { return Results.Ok(await svc.CreateAsync(dto)); }
    catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
}).RequireAuthorization();

app.MapGet("/api/guarantee-extensions", async (IPaymentGuaranteeExtService svc, string? status, string? dealer, string? bankCode, string? flagisHTC, string? vin, string? grtClaimExtNo) =>
    Results.Ok(await svc.ListAsync(status, dealer, bankCode, flagisHTC, vin, grtClaimExtNo))).RequireAuthorization();

app.MapGet("/api/guarantee-extensions/stats", async (IPaymentGuaranteeExtService svc) =>
    Results.Ok(await svc.StatsAsync())).RequireAuthorization();

app.MapGet("/api/guarantee-extensions/{extNo}", async (string extNo, IPaymentGuaranteeExtService svc) =>
{
    var r = await svc.DetailAsync(extNo);
    return r is null ? Results.NotFound(new { error = $"Không tìm thấy công văn gia hạn bảo lãnh {extNo}." }) : Results.Ok(r);
}).RequireAuthorization();

app.MapPut("/api/guarantee-extensions/{extNo}", async (string extNo, UpdatePaymentGuaranteeExtDto dto, IPaymentGuaranteeExtService svc) =>
{
    try
    {
        var r = await svc.UpdateAsync(extNo, dto);
        return r is null ? Results.NotFound(new { error = $"Không tìm thấy công văn gia hạn bảo lãnh {extNo}." }) : Results.Ok(r);
    }
    catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
}).RequireAuthorization();

app.MapPost("/api/guarantee-extensions/{extNo}/sign", async (string extNo, SignPaymentGuaranteeExtDto? dto, IPaymentGuaranteeExtService svc) =>
{
    try
    {
        var r = await svc.SignAndApproveAsync(extNo, dto);
        return r is null ? Results.NotFound(new { error = $"Không tìm thấy công văn gia hạn bảo lãnh {extNo}." }) : Results.Ok(r);
    }
    catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
}).RequireAuthorization();

app.MapPost("/api/guarantee-extensions/{extNo}/cancel", async (string extNo, CancelPaymentGuaranteeExtDto dto, IPaymentGuaranteeExtService svc) =>
{
    try
    {
        var r = await svc.CancelAsync(extNo, dto);
        return r is null ? Results.NotFound(new { error = $"Không tìm thấy công văn gia hạn bảo lãnh {extNo}." }) : Results.Ok(r);
    }
    catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
}).RequireAuthorization();

app.MapDelete("/api/guarantee-extensions/{extNo}", async (string extNo, IPaymentGuaranteeExtService svc) =>
{
    try
    {
        var r = await svc.DeleteDraftAsync(extNo);
        return r is null ? Results.NotFound(new { error = $"Không tìm thấy công văn gia hạn bảo lãnh {extNo}." }) : Results.Ok(r);
    }
    catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
}).RequireAuthorization();

app.MapPost("/api/guarantee-extensions/{extNo}/cars", async (string extNo, AddCarToGuaranteeExtDto dto, IPaymentGuaranteeExtService svc) =>
{
    try
    {
        var r = await svc.AddCarAsync(extNo, dto);
        return r is null ? Results.NotFound(new { error = $"Không tìm thấy công văn gia hạn bảo lãnh {extNo}." }) : Results.Ok(r);
    }
    catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
}).RequireAuthorization();

app.MapDelete("/api/guarantee-extensions/{extNo}/cars/{vin}", async (string extNo, string vin, IPaymentGuaranteeExtService svc) =>
{
    try
    {
        var r = await svc.RemoveCarAsync(extNo, vin);
        return r is null ? Results.NotFound(new { error = $"Không tìm thấy công văn gia hạn bảo lãnh {extNo} hoặc xe VIN {vin}." }) : Results.Ok(r);
    }
    catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
}).RequireAuthorization();

// ===== Quản lý Yêu cầu kiểm tra xe trước khi giao PDI (Pre-Delivery Inspection - DMS.Sales Dlr_PDIRequest + Dlr_PDIRequestDtl) =====
app.MapPost("/api/pdi-requests", async (CreatePdiRequestDto dto, IPdiRequestService svc) =>
{
    try { return Results.Ok(await svc.CreateAsync(dto)); }
    catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
}).RequireAuthorization();

app.MapGet("/api/pdi-requests", async (IPdiRequestService svc, string? status, string? dealer, string? pdiReqNo, string? vin, string? contractNo, string? roStatus, bool? flagAccessory) =>
    Results.Ok(await svc.ListAsync(status, dealer, pdiReqNo, vin, contractNo, roStatus, flagAccessory))).RequireAuthorization();

app.MapGet("/api/pdi-requests/stats", async (IPdiRequestService svc) =>
    Results.Ok(await svc.StatsAsync())).RequireAuthorization();

app.MapGet("/api/pdi-requests/eligible-cars", async (IPdiRequestService svc, string? dealer, string? contractNo, string? vin) =>
    Results.Ok(await svc.GetEligibleCarsAsync(dealer, contractNo, vin))).RequireAuthorization();

app.MapGet("/api/pdi-requests/{reqNo}", async (string reqNo, IPdiRequestService svc) =>
{
    var r = await svc.DetailAsync(reqNo);
    return r is null ? Results.NotFound(new { error = $"Không tìm thấy yêu cầu PDI {reqNo}." }) : Results.Ok(r);
}).RequireAuthorization();

app.MapPut("/api/pdi-requests/{reqNo}", async (string reqNo, UpdatePdiRequestDto dto, IPdiRequestService svc) =>
{
    try
    {
        var r = await svc.UpdateAsync(reqNo, dto);
        return r is null ? Results.NotFound(new { error = $"Không tìm thấy yêu cầu PDI {reqNo}." }) : Results.Ok(r);
    }
    catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
}).RequireAuthorization();

app.MapPost("/api/pdi-requests/{reqNo}/approve", async (string reqNo, ApprovePdiRequestDto? dto, IPdiRequestService svc) =>
{
    try
    {
        var r = await svc.ApproveAsync(reqNo, dto);
        return r is null ? Results.NotFound(new { error = $"Không tìm thấy yêu cầu PDI {reqNo}." }) : Results.Ok(r);
    }
    catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
}).RequireAuthorization();

app.MapPost("/api/pdi-requests/{reqNo}/sync-ro", async (string reqNo, SyncPdiRoDto dto, IPdiRequestService svc) =>
{
    try
    {
        var r = await svc.SyncROAsync(reqNo, dto);
        return r is null ? Results.NotFound(new { error = $"Không tìm thấy yêu cầu PDI {reqNo}." }) : Results.Ok(r);
    }
    catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
}).RequireAuthorization();

app.MapPost("/api/pdi-requests/{reqNo}/cancel", async (string reqNo, CancelPdiRequestDto dto, IPdiRequestService svc) =>
{
    try
    {
        var r = await svc.CancelAsync(reqNo, dto);
        return r is null ? Results.NotFound(new { error = $"Không tìm thấy yêu cầu PDI {reqNo}." }) : Results.Ok(r);
    }
    catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
}).RequireAuthorization();

app.MapDelete("/api/pdi-requests/{reqNo}", async (string reqNo, IPdiRequestService svc) =>
{
    try
    {
        var r = await svc.DeleteDraftAsync(reqNo);
        return r is null ? Results.NotFound(new { error = $"Không tìm thấy yêu cầu PDI {reqNo}." }) : Results.Ok(r);
    }
    catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
}).RequireAuthorization();

app.MapPost("/api/pdi-requests/{reqNo}/cars", async (string reqNo, AddPdiCarDto dto, IPdiRequestService svc) =>
{
    try
    {
        var r = await svc.AddCarAsync(reqNo, dto);
        return r is null ? Results.NotFound(new { error = $"Không tìm thấy yêu cầu PDI {reqNo}." }) : Results.Ok(r);
    }
    catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
}).RequireAuthorization();

app.MapDelete("/api/pdi-requests/{reqNo}/cars/{vin}", async (string reqNo, string vin, IPdiRequestService svc) =>
{
    try
    {
        var r = await svc.RemoveCarAsync(reqNo, vin);
        return r is null ? Results.NotFound(new { error = $"Không tìm thấy yêu cầu PDI {reqNo} hoặc xe VIN {vin}." }) : Results.Ok(r);
    }
    catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
}).RequireAuthorization();

// ===== Quản lý Đề nghị Chiết khấu Thanh toán Mua xe Ô tô (Payment Discount Request - DMS.Sales Req_PaymentDiscount + Req_PaymentDiscountDtl) =====
app.MapPost("/api/payment-discounts", async (CreatePaymentDiscountDto dto, IPaymentDiscountService svc) =>
{
    try { return Results.Ok(await svc.CreateAsync(dto)); }
    catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
}).RequireAuthorization();

app.MapGet("/api/payment-discounts", async (IPaymentDiscountService svc, string? status, string? dealer, string? discountNo, string? vin, string? guaranteeNo, DateTime? fromDate, DateTime? toDate) =>
    Results.Ok(await svc.ListAsync(status, dealer, discountNo, vin, guaranteeNo, fromDate, toDate))).RequireAuthorization();

app.MapGet("/api/payment-discounts/stats", async (IPaymentDiscountService svc) =>
    Results.Ok(await svc.StatsAsync())).RequireAuthorization();

app.MapGet("/api/payment-discounts/eligible-cars", async (IPaymentDiscountService svc, string? dealer, string? guaranteeNo, string? vin) =>
    Results.Ok(await svc.GetEligibleCarsAsync(dealer, guaranteeNo, vin))).RequireAuthorization();

app.MapGet("/api/payment-discounts/{discountNo}", async (string discountNo, IPaymentDiscountService svc) =>
{
    var r = await svc.DetailAsync(discountNo);
    return r is null ? Results.NotFound(new { error = $"Không tìm thấy đề nghị chiết khấu {discountNo}." }) : Results.Ok(r);
}).RequireAuthorization();

app.MapPut("/api/payment-discounts/{discountNo}", async (string discountNo, UpdatePaymentDiscountDto dto, IPaymentDiscountService svc) =>
{
    try
    {
        var r = await svc.UpdateAsync(discountNo, dto);
        return r is null ? Results.NotFound(new { error = $"Không tìm thấy đề nghị chiết khấu {discountNo}." }) : Results.Ok(r);
    }
    catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
}).RequireAuthorization();

app.MapPost("/api/payment-discounts/{discountNo}/sign-dl", async (string discountNo, SignDealerPaymentDiscountDto dto, IPaymentDiscountService svc) =>
{
    try
    {
        var r = await svc.SignDealerAsync(discountNo, dto);
        return r is null ? Results.NotFound(new { error = $"Không tìm thấy đề nghị chiết khấu {discountNo}." }) : Results.Ok(r);
    }
    catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
}).RequireAuthorization();

app.MapPost("/api/payment-discounts/{discountNo}/approve", async (string discountNo, ApproveHqPaymentDiscountDto? dto, IPaymentDiscountService svc) =>
{
    try
    {
        var r = await svc.ApproveHqAsync(discountNo, dto);
        return r is null ? Results.NotFound(new { error = $"Không tìm thấy đề nghị chiết khấu {discountNo}." }) : Results.Ok(r);
    }
    catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
}).RequireAuthorization();

app.MapPost("/api/payment-discounts/{discountNo}/sign-hq", async (string discountNo, SignHqPaymentDiscountDto dto, IPaymentDiscountService svc) =>
{
    try
    {
        var r = await svc.SignHqAsync(discountNo, dto);
        return r is null ? Results.NotFound(new { error = $"Không tìm thấy đề nghị chiết khấu {discountNo}." }) : Results.Ok(r);
    }
    catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
}).RequireAuthorization();

app.MapPost("/api/payment-discounts/{discountNo}/reject", async (string discountNo, RejectPaymentDiscountDto dto, IPaymentDiscountService svc) =>
{
    try
    {
        var r = await svc.RejectHqAsync(discountNo, dto);
        return r is null ? Results.NotFound(new { error = $"Không tìm thấy đề nghị chiết khấu {discountNo}." }) : Results.Ok(r);
    }
    catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
}).RequireAuthorization();

app.MapPost("/api/payment-discounts/{discountNo}/cancel", async (string discountNo, CancelPaymentDiscountDto dto, IPaymentDiscountService svc) =>
{
    try
    {
        var r = await svc.CancelAsync(discountNo, dto);
        return r is null ? Results.NotFound(new { error = $"Không tìm thấy đề nghị chiết khấu {discountNo}." }) : Results.Ok(r);
    }
    catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
}).RequireAuthorization();

app.MapDelete("/api/payment-discounts/{discountNo}", async (string discountNo, IPaymentDiscountService svc) =>
{
    try
    {
        var r = await svc.DeleteDraftAsync(discountNo);
        return r is null ? Results.NotFound(new { error = $"Không tìm thấy đề nghị chiết khấu {discountNo}." }) : Results.Ok(r);
    }
    catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
}).RequireAuthorization();

app.MapPost("/api/payment-discounts/{discountNo}/cars", async (string discountNo, AddCarToPaymentDiscountDto dto, IPaymentDiscountService svc) =>
{
    try
    {
        var r = await svc.AddCarAsync(discountNo, dto);
        return r is null ? Results.NotFound(new { error = $"Không tìm thấy đề nghị chiết khấu {discountNo}." }) : Results.Ok(r);
    }
    catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
}).RequireAuthorization();

app.MapDelete("/api/payment-discounts/{discountNo}/cars/{vin}", async (string discountNo, string vin, IPaymentDiscountService svc) =>
{
    try
    {
        var r = await svc.RemoveCarAsync(discountNo, vin);
        return r is null ? Results.NotFound(new { error = $"Không tìm thấy đề nghị chiết khấu {discountNo} hoặc xe VIN {vin}." }) : Results.Ok(r);
    }
    catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
}).RequireAuthorization();

app.MapPut("/api/payment-discounts/{discountNo}/cars/{vin}", async (string discountNo, string vin, UpdatePaymentDiscountLineDto dto, IPaymentDiscountService svc) =>
{
    try
    {
        var r = await svc.UpdateLineAsync(discountNo, vin, dto);
        return r is null ? Results.NotFound(new { error = $"Không tìm thấy đề nghị chiết khấu {discountNo} hoặc xe VIN {vin}." }) : Results.Ok(r);
    }
    catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
}).RequireAuthorization();

// ===== Quản lý Biên bản thanh lý / Hủy Hợp đồng mua bán buôn xe Đại lý - NPP (Wholesale Dealer Contract Cancellation Minutes - DMS.Sales DMS40_DlrCtr_CancelMinutes / DlrCtrCancelMinutesController / DMS40.0.34.Contract.cs) =====
app.MapPost("/api/dealer-contract-cancels", async (CreateDealerContractCancelMinutesDto dto, IDealerContractCancelMinutesService svc) =>
{
    try { return Results.Ok(await svc.CreateAsync(dto)); }
    catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
}).RequireAuthorization();

app.MapGet("/api/dealer-contract-cancels", async (IDealerContractCancelMinutesService svc, string? status, string? dealer, string? contractNo, string? minutesNo, string? dlrSignStatus, string? hqSignStatus) =>
    Results.Ok(await svc.ListAsync(status, dealer, contractNo, minutesNo, dlrSignStatus, hqSignStatus))).RequireAuthorization();

app.MapGet("/api/dealer-contract-cancels/stats", async (IDealerContractCancelMinutesService svc) =>
    Results.Ok(await svc.StatsAsync())).RequireAuthorization();

app.MapGet("/api/dealer-contract-cancels/{minutesNo}", async (string minutesNo, IDealerContractCancelMinutesService svc) =>
{
    var r = await svc.DetailAsync(minutesNo);
    return r is null ? Results.NotFound(new { error = $"Không tìm thấy biên bản hủy hợp đồng {minutesNo}." }) : Results.Ok(r);
}).RequireAuthorization();

app.MapPut("/api/dealer-contract-cancels/{minutesNo}", async (string minutesNo, UpdateDealerContractCancelMinutesDto dto, IDealerContractCancelMinutesService svc) =>
{
    try
    {
        var r = await svc.UpdateAsync(minutesNo, dto);
        return r is null ? Results.NotFound(new { error = $"Không tìm thấy biên bản hủy hợp đồng {minutesNo}." }) : Results.Ok(r);
    }
    catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
}).RequireAuthorization();

app.MapDelete("/api/dealer-contract-cancels/{minutesNo}", async (string minutesNo, IDealerContractCancelMinutesService svc) =>
{
    try
    {
        var r = await svc.DeleteDraftAsync(minutesNo);
        return r is null ? Results.NotFound(new { error = $"Không tìm thấy biên bản hủy hợp đồng {minutesNo}." }) : Results.Ok(r);
    }
    catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
}).RequireAuthorization();

app.MapPost("/api/dealer-contract-cancels/{minutesNo}/approve-dl", async (string minutesNo, ApproveDealerContractCancelMinutesDto dto, IDealerContractCancelMinutesService svc) =>
{
    try
    {
        var r = await svc.ApproveDealerAsync(minutesNo, dto);
        return r is null ? Results.NotFound(new { error = $"Không tìm thấy biên bản hủy hợp đồng {minutesNo}." }) : Results.Ok(r);
    }
    catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
}).RequireAuthorization();

app.MapPost("/api/dealer-contract-cancels/{minutesNo}/approve1-hq", async (string minutesNo, Approve1HqDealerContractCancelMinutesDto? dto, IDealerContractCancelMinutesService svc) =>
{
    try
    {
        var r = await svc.Approve1HqAsync(minutesNo, dto);
        return r is null ? Results.NotFound(new { error = $"Không tìm thấy biên bản hủy hợp đồng {minutesNo}." }) : Results.Ok(r);
    }
    catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
}).RequireAuthorization();

app.MapPost("/api/dealer-contract-cancels/{minutesNo}/approve2-hq", async (string minutesNo, Approve2HqDealerContractCancelMinutesDto dto, IDealerContractCancelMinutesService svc) =>
{
    try
    {
        var r = await svc.Approve2HqAsync(minutesNo, dto);
        return r is null ? Results.NotFound(new { error = $"Không tìm thấy biên bản hủy hợp đồng {minutesNo}." }) : Results.Ok(r);
    }
    catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
}).RequireAuthorization();

app.MapPost("/api/dealer-contract-cancels/{minutesNo}/reject-hq", async (string minutesNo, RejectDealerContractCancelMinutesDto dto, IDealerContractCancelMinutesService svc) =>
{
    try
    {
        var r = await svc.RejectHqAsync(minutesNo, dto);
        return r is null ? Results.NotFound(new { error = $"Không tìm thấy biên bản hủy hợp đồng {minutesNo}." }) : Results.Ok(r);
    }
    catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
}).RequireAuthorization();

app.MapPost("/api/dealer-contract-cancels/{minutesNo}/cancel-dl", async (string minutesNo, CancelDealerContractCancelMinutesDto dto, IDealerContractCancelMinutesService svc) =>
{
    try
    {
        var r = await svc.CancelDealerAsync(minutesNo, dto);
        return r is null ? Results.NotFound(new { error = $"Không tìm thấy biên bản hủy hợp đồng {minutesNo}." }) : Results.Ok(r);
    }
    catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
}).RequireAuthorization();

// ===== Quản lý Công văn Yêu cầu / Đòi tiền thực hiện Bảo lãnh ngân hàng mua bán xe Đại lý - NPP (Payment Guarantee Claim Letter - DMS.Sales Pmt_GrtClaim / PmtGrtClaimController / 17_CONG_VAN_BON_CHUC_NANG.md) =====
app.MapPost("/api/guarantee-claims", async (CreatePaymentGuaranteeClaimDto dto, IPaymentGuaranteeClaimService svc) =>
{
    try { return Results.Ok(await svc.CreateAsync(dto)); }
    catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
}).RequireAuthorization();

app.MapGet("/api/guarantee-claims", async (IPaymentGuaranteeClaimService svc, string? status, string? dealer, string? bankCode, string? bankCodeMonitor, string? claimType, string? flagisHTC, string? vin, string? claimNo, string? contractNo) =>
    Results.Ok(await svc.ListAsync(status, dealer, bankCode, bankCodeMonitor, claimType, flagisHTC, vin, claimNo, contractNo))).RequireAuthorization();

app.MapGet("/api/guarantee-claims/eligible-cars", async (IPaymentGuaranteeClaimService svc, string? dealerCode, string? bankCode, string? bankCodeMonitor) =>
    Results.Ok(await svc.EligibleCarsAsync(dealerCode, bankCode, bankCodeMonitor))).RequireAuthorization();

app.MapGet("/api/guarantee-claims/stats", async (IPaymentGuaranteeClaimService svc) =>
    Results.Ok(await svc.StatsAsync())).RequireAuthorization();

app.MapGet("/api/guarantee-claims/{claimNo}", async (string claimNo, IPaymentGuaranteeClaimService svc) =>
{
    var r = await svc.DetailAsync(claimNo);
    return r is null ? Results.NotFound(new { error = $"Không tìm thấy công văn đòi tiền bảo lãnh {claimNo}." }) : Results.Ok(r);
}).RequireAuthorization();

app.MapPut("/api/guarantee-claims/{claimNo}", async (string claimNo, UpdatePaymentGuaranteeClaimDto dto, IPaymentGuaranteeClaimService svc) =>
{
    try
    {
        var r = await svc.UpdateAsync(claimNo, dto);
        return r is null ? Results.NotFound(new { error = $"Không tìm thấy công văn đòi tiền bảo lãnh {claimNo}." }) : Results.Ok(r);
    }
    catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
}).RequireAuthorization();

app.MapDelete("/api/guarantee-claims/{claimNo}", async (string claimNo, IPaymentGuaranteeClaimService svc) =>
{
    try
    {
        var r = await svc.DeleteDraftAsync(claimNo);
        return r is null ? Results.NotFound(new { error = $"Không tìm thấy công văn đòi tiền bảo lãnh {claimNo}." }) : Results.Ok(r);
    }
    catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
}).RequireAuthorization();

app.MapPost("/api/guarantee-claims/{claimNo}/sign", async (string claimNo, SignPaymentGuaranteeClaimDto? dto, IPaymentGuaranteeClaimService svc) =>
{
    try
    {
        var r = await svc.SignAndApproveAsync(claimNo, dto);
        return r is null ? Results.NotFound(new { error = $"Không tìm thấy công văn đòi tiền bảo lãnh {claimNo}." }) : Results.Ok(r);
    }
    catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
}).RequireAuthorization();

app.MapPost("/api/guarantee-claims/{claimNo}/reject", async (string claimNo, RejectPaymentGuaranteeClaimDto dto, IPaymentGuaranteeClaimService svc) =>
{
    try
    {
        var r = await svc.RejectAsync(claimNo, dto);
        return r is null ? Results.NotFound(new { error = $"Không tìm thấy công văn đòi tiền bảo lãnh {claimNo}." }) : Results.Ok(r);
    }
    catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
}).RequireAuthorization();

app.MapPost("/api/guarantee-claims/{claimNo}/cancel", async (string claimNo, CancelPaymentGuaranteeClaimDto dto, IPaymentGuaranteeClaimService svc) =>
{
    try
    {
        var r = await svc.CancelAsync(claimNo, dto);
        return r is null ? Results.NotFound(new { error = $"Không tìm thấy công văn đòi tiền bảo lãnh {claimNo}." }) : Results.Ok(r);
    }
    catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
}).RequireAuthorization();

app.MapPost("/api/guarantee-claims/{claimNo}/cars", async (string claimNo, AddCarToGuaranteeClaimDto dto, IPaymentGuaranteeClaimService svc) =>
{
    try
    {
        var r = await svc.AddCarAsync(claimNo, dto);
        return r is null ? Results.NotFound(new { error = $"Không tìm thấy công văn đòi tiền bảo lãnh {claimNo}." }) : Results.Ok(r);
    }
    catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
}).RequireAuthorization();

app.MapDelete("/api/guarantee-claims/{claimNo}/cars/{vin}", async (string claimNo, string vin, IPaymentGuaranteeClaimService svc) =>
{
    try
    {
        var r = await svc.RemoveCarAsync(claimNo, vin);
        return r is null ? Results.NotFound(new { error = $"Không tìm thấy công văn đòi tiền bảo lãnh {claimNo} hoặc xe VIN {vin}." }) : Results.Ok(r);
    }
    catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
}).RequireAuthorization();

// ===== Quản lý Đề nghị / Biên bản Hủy chọn Ngân hàng phát hành bảo lãnh hợp đồng bán xe Đại lý - NPP (DMS.Sales DMS40_DlrCtr_CancelBankMD / DlrCtrCancelBankMDController / DMS40.0.34.Contract.cs / CANCEL_BANK_MD_FLOW.md) =====
app.MapPost("/api/dealer-contract-cancel-banks", async (CreateDealerContractCancelBankMDDto dto, IDealerContractCancelBankMDService svc) =>
{
    try { return Results.Ok(await svc.CreateAsync(dto)); }
    catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
}).RequireAuthorization();

app.MapGet("/api/dealer-contract-cancel-banks", async (IDealerContractCancelBankMDService svc, string? status, string? dealer, string? contractNo, string? cancelBankMDNo, string? bankCodeMD) =>
    Results.Ok(await svc.ListAsync(status, dealer, contractNo, cancelBankMDNo, bankCodeMD))).RequireAuthorization();

app.MapGet("/api/dealer-contract-cancel-banks/stats", async (IDealerContractCancelBankMDService svc) =>
    Results.Ok(await svc.StatsAsync())).RequireAuthorization();

app.MapGet("/api/dealer-contract-cancel-banks/{cancelBankMDNo}", async (string cancelBankMDNo, IDealerContractCancelBankMDService svc) =>
{
    var r = await svc.DetailAsync(cancelBankMDNo);
    return r is null ? Results.NotFound(new { error = $"Không tìm thấy đề nghị hủy chọn ngân hàng bảo lãnh {cancelBankMDNo}." }) : Results.Ok(r);
}).RequireAuthorization();

app.MapPut("/api/dealer-contract-cancel-banks/{cancelBankMDNo}", async (string cancelBankMDNo, UpdateDealerContractCancelBankMDDto dto, IDealerContractCancelBankMDService svc) =>
{
    try
    {
        var r = await svc.UpdateAsync(cancelBankMDNo, dto);
        return r is null ? Results.NotFound(new { error = $"Không tìm thấy đề nghị hủy chọn ngân hàng bảo lãnh {cancelBankMDNo}." }) : Results.Ok(r);
    }
    catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
}).RequireAuthorization();

app.MapDelete("/api/dealer-contract-cancel-banks/{cancelBankMDNo}", async (string cancelBankMDNo, IDealerContractCancelBankMDService svc) =>
{
    try
    {
        var r = await svc.DeleteDraftAsync(cancelBankMDNo);
        return r is null ? Results.NotFound(new { error = $"Không tìm thấy đề nghị hủy chọn ngân hàng bảo lãnh {cancelBankMDNo}." }) : Results.Ok(r);
    }
    catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
}).RequireAuthorization();

app.MapPost("/api/dealer-contract-cancel-banks/{cancelBankMDNo}/bank-approve", async (string cancelBankMDNo, BankApproveDealerContractCancelBankMDDto? dto, IDealerContractCancelBankMDService svc) =>
{
    try
    {
        var r = await svc.BankApproveAsync(cancelBankMDNo, dto);
        return r is null ? Results.NotFound(new { error = $"Không tìm thấy đề nghị hủy chọn ngân hàng bảo lãnh {cancelBankMDNo}." }) : Results.Ok(r);
    }
    catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
}).RequireAuthorization();

app.MapPost("/api/dealer-contract-cancel-banks/{cancelBankMDNo}/finish-hq", async (string cancelBankMDNo, FinishDealerContractCancelBankMDDto? dto, IDealerContractCancelBankMDService svc) =>
{
    try
    {
        var r = await svc.FinishHqAsync(cancelBankMDNo, dto);
        return r is null ? Results.NotFound(new { error = $"Không tìm thấy đề nghị hủy chọn ngân hàng bảo lãnh {cancelBankMDNo}." }) : Results.Ok(r);
    }
    catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
}).RequireAuthorization();

app.MapPost("/api/dealer-contract-cancel-banks/{cancelBankMDNo}/reject-hq", async (string cancelBankMDNo, RejectDealerContractCancelBankMDDto dto, IDealerContractCancelBankMDService svc) =>
{
    try
    {
        var r = await svc.RejectHqAsync(cancelBankMDNo, dto);
        return r is null ? Results.NotFound(new { error = $"Không tìm thấy đề nghị hủy chọn ngân hàng bảo lãnh {cancelBankMDNo}." }) : Results.Ok(r);
    }
    catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
}).RequireAuthorization();

app.MapPost("/api/dealer-contract-cancel-banks/{cancelBankMDNo}/cancel-dl", async (string cancelBankMDNo, CancelDealerContractCancelBankMDDto dto, IDealerContractCancelBankMDService svc) =>
{
    try
    {
        var r = await svc.CancelDealerAsync(cancelBankMDNo, dto);
        return r is null ? Results.NotFound(new { error = $"Không tìm thấy đề nghị hủy chọn ngân hàng bảo lãnh {cancelBankMDNo}." }) : Results.Ok(r);
    }
    catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
}).RequireAuthorization();

// ===== Quản lý Biên bản bàn giao hóa đơn & chứng từ xe theo Hối phiếu Ngân hàng (Car Bank Bill Minutes - DMS.Sales Car_BankBillMinutes / CarBankBillMinutesController / PaymentGrtExt.cs) =====
app.MapPost("/api/bank-bill-minutes", async (CreateBankBillMinutesDto dto, IBankBillMinutesService svc) =>
{
    try { return Results.Ok(await svc.CreateAsync(dto)); }
    catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
}).RequireAuthorization();

app.MapGet("/api/bank-bill-minutes", async (IBankBillMinutesService svc, string? status, string? bankCode, string? dealerCode, string? guaranteeNo, string? vin, string? bankBillMnNo, DateTime? dateFrom, DateTime? dateTo) =>
    Results.Ok(await svc.ListAsync(status, bankCode, dealerCode, guaranteeNo, vin, bankBillMnNo, dateFrom, dateTo))).RequireAuthorization();

app.MapGet("/api/bank-bill-minutes/eligible-cars", async (IBankBillMinutesService svc, string? dealerCode, string? bankCode) =>
    Results.Ok(await svc.GetEligibleCarsAsync(dealerCode, bankCode))).RequireAuthorization();

app.MapGet("/api/bank-bill-minutes/stats", async (IBankBillMinutesService svc) =>
    Results.Ok(await svc.StatsAsync())).RequireAuthorization();

app.MapGet("/api/bank-bill-minutes/{bankBillMnNo}", async (string bankBillMnNo, IBankBillMinutesService svc) =>
{
    var r = await svc.DetailAsync(bankBillMnNo);
    return r is null ? Results.NotFound(new { error = $"Không tìm thấy biên bản bàn giao hối phiếu {bankBillMnNo}." }) : Results.Ok(r);
}).RequireAuthorization();

app.MapPut("/api/bank-bill-minutes/{bankBillMnNo}", async (string bankBillMnNo, UpdateBankBillMinutesDto dto, IBankBillMinutesService svc) =>
{
    try
    {
        var r = await svc.UpdateAsync(bankBillMnNo, dto);
        return r is null ? Results.NotFound(new { error = $"Không tìm thấy biên bản bàn giao hối phiếu {bankBillMnNo}." }) : Results.Ok(r);
    }
    catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
}).RequireAuthorization();

app.MapDelete("/api/bank-bill-minutes/{bankBillMnNo}", async (string bankBillMnNo, IBankBillMinutesService svc) =>
{
    try
    {
        var r = await svc.DeleteDraftAsync(bankBillMnNo);
        return r is null ? Results.NotFound(new { error = $"Không tìm thấy biên bản bàn giao hối phiếu {bankBillMnNo}." }) : Results.Ok(r);
    }
    catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
}).RequireAuthorization();

app.MapPost("/api/bank-bill-minutes/{bankBillMnNo}/handover", async (string bankBillMnNo, HandoverBankBillMinutesDto? dto, IBankBillMinutesService svc) =>
{
    try
    {
        var r = await svc.HandoverAsync(bankBillMnNo, dto);
        return r is null ? Results.NotFound(new { error = $"Không tìm thấy biên bản bàn giao hối phiếu {bankBillMnNo}." }) : Results.Ok(r);
    }
    catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
}).RequireAuthorization();

app.MapPost("/api/bank-bill-minutes/{bankBillMnNo}/bank-receive", async (string bankBillMnNo, BankReceiveBankBillMinutesDto? dto, IBankBillMinutesService svc) =>
{
    try
    {
        var r = await svc.BankReceiveAsync(bankBillMnNo, dto);
        return r is null ? Results.NotFound(new { error = $"Không tìm thấy biên bản bàn giao hối phiếu {bankBillMnNo}." }) : Results.Ok(r);
    }
    catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
}).RequireAuthorization();

app.MapPost("/api/bank-bill-minutes/{bankBillMnNo}/settle", async (string bankBillMnNo, SettleBankBillMinutesDto? dto, IBankBillMinutesService svc) =>
{
    try
    {
        var r = await svc.SettleAsync(bankBillMnNo, dto);
        return r is null ? Results.NotFound(new { error = $"Không tìm thấy biên bản bàn giao hối phiếu {bankBillMnNo}." }) : Results.Ok(r);
    }
    catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
}).RequireAuthorization();

app.MapPost("/api/bank-bill-minutes/{bankBillMnNo}/cancel", async (string bankBillMnNo, CancelBankBillMinutesDto dto, IBankBillMinutesService svc) =>
{
    try
    {
        var r = await svc.CancelAsync(bankBillMnNo, dto);
        return r is null ? Results.NotFound(new { error = $"Không tìm thấy biên bản bàn giao hối phiếu {bankBillMnNo}." }) : Results.Ok(r);
    }
    catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
}).RequireAuthorization();

app.MapPost("/api/bank-bill-minutes/{bankBillMnNo}/cars", async (string bankBillMnNo, List<AddBankBillCarDto> items, IBankBillMinutesService svc) =>
{
    try
    {
        var r = await svc.AddCarsAsync(bankBillMnNo, items);
        return r is null ? Results.NotFound(new { error = $"Không tìm thấy biên bản bàn giao hối phiếu {bankBillMnNo}." }) : Results.Ok(r);
    }
    catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
}).RequireAuthorization();

app.MapDelete("/api/bank-bill-minutes/{bankBillMnNo}/cars/{vin}", async (string bankBillMnNo, string vin, IBankBillMinutesService svc) =>
{
    try
    {
        var r = await svc.RemoveCarAsync(bankBillMnNo, vin);
        return r is null ? Results.NotFound(new { error = $"Không tìm thấy biên bản bàn giao hối phiếu {bankBillMnNo} hoặc xe VIN {vin}." }) : Results.Ok(r);
    }
    catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
}).RequireAuthorization();

// ===== Quản lý Đề nghị & Phê duyệt Đăng ký Xe Lái Thử Đại lý (Car Test Car / Demo Car - DMS.Sales Car_TestCar / CarTestCarController / 05_QUAN_LY_XE.md) =====
app.MapPost("/api/test-cars", async (CreateCarTestCarDto dto, ICarTestCarService svc) =>
{
    try { return Results.Ok(await svc.CreateAsync(dto)); }
    catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
}).RequireAuthorization();

app.MapGet("/api/test-cars", async (ICarTestCarService svc, string? status, string? dealerCode, string? model, string? vin, string? testCarCode, DateTime? dateFrom, DateTime? dateTo) =>
    Results.Ok(await svc.ListAsync(status, dealerCode, model, vin, testCarCode, dateFrom, dateTo))).RequireAuthorization();

app.MapGet("/api/test-cars/eligible-cars", async (ICarTestCarService svc, string? dealerCode) =>
    Results.Ok(await svc.GetEligibleCarsAsync(dealerCode))).RequireAuthorization();

app.MapGet("/api/test-cars/stats", async (ICarTestCarService svc) =>
    Results.Ok(await svc.StatsAsync())).RequireAuthorization();

app.MapGet("/api/test-cars/{testCarCode}", async (string testCarCode, ICarTestCarService svc) =>
{
    var r = await svc.DetailAsync(testCarCode);
    return r is null ? Results.NotFound(new { error = $"Không tìm thấy đề nghị xe lái thử {testCarCode}." }) : Results.Ok(r);
}).RequireAuthorization();

app.MapPut("/api/test-cars/{testCarCode}", async (string testCarCode, UpdateCarTestCarDto dto, ICarTestCarService svc) =>
{
    try
    {
        var r = await svc.UpdateAsync(testCarCode, dto);
        return r is null ? Results.NotFound(new { error = $"Không tìm thấy đề nghị xe lái thử {testCarCode}." }) : Results.Ok(r);
    }
    catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
}).RequireAuthorization();

app.MapDelete("/api/test-cars/{testCarCode}", async (string testCarCode, ICarTestCarService svc) =>
{
    try
    {
        var ok = await svc.DeleteDraftAsync(testCarCode);
        return !ok ? Results.NotFound(new { error = $"Không tìm thấy đề nghị xe lái thử {testCarCode}." }) : Results.Ok(new { success = true, testCarCode });
    }
    catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
}).RequireAuthorization();

app.MapPost("/api/test-cars/{testCarCode}/approve1-hq", async (string testCarCode, Approve1CarTestCarDto? dto, ICarTestCarService svc) =>
{
    try
    {
        var r = await svc.Approve1HqAsync(testCarCode, dto);
        return r is null ? Results.NotFound(new { error = $"Không tìm thấy đề nghị xe lái thử {testCarCode}." }) : Results.Ok(r);
    }
    catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
}).RequireAuthorization();

app.MapPost("/api/test-cars/{testCarCode}/approve2-hq", async (string testCarCode, Approve2CarTestCarDto? dto, ICarTestCarService svc) =>
{
    try
    {
        var r = await svc.Approve2HqAsync(testCarCode, dto);
        return r is null ? Results.NotFound(new { error = $"Không tìm thấy đề nghị xe lái thử {testCarCode}." }) : Results.Ok(r);
    }
    catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
}).RequireAuthorization();

app.MapPost("/api/test-cars/{testCarCode}/reject-hq", async (string testCarCode, RejectCarTestCarDto dto, ICarTestCarService svc) =>
{
    try
    {
        var r = await svc.RejectHqAsync(testCarCode, dto);
        return r is null ? Results.NotFound(new { error = $"Không tìm thấy đề nghị xe lái thử {testCarCode}." }) : Results.Ok(r);
    }
    catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
}).RequireAuthorization();

app.MapPost("/api/test-cars/{testCarCode}/cancel", async (string testCarCode, CancelCarTestCarDto dto, ICarTestCarService svc) =>
{
    try
    {
        var r = await svc.CancelAsync(testCarCode, dto);
        return r is null ? Results.NotFound(new { error = $"Không tìm thấy đề nghị xe lái thử {testCarCode}." }) : Results.Ok(r);
    }
    catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
}).RequireAuthorization();

app.MapPost("/api/test-cars/{testCarCode}/cars", async (string testCarCode, List<AddTestCarItemDto> items, ICarTestCarService svc) =>
{
    try
    {
        var r = await svc.AddCarsAsync(testCarCode, items);
        return r is null ? Results.NotFound(new { error = $"Không tìm thấy đề nghị xe lái thử {testCarCode}." }) : Results.Ok(r);
    }
    catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
}).RequireAuthorization();

app.MapDelete("/api/test-cars/{testCarCode}/cars/{vin}", async (string testCarCode, string vin, ICarTestCarService svc) =>
{
    try
    {
        var r = await svc.RemoveCarAsync(testCarCode, vin);
        return r is null ? Results.NotFound(new { error = $"Không tìm thấy đề nghị xe lái thử {testCarCode} hoặc xe VIN {vin}." }) : Results.Ok(r);
    }
    catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
}).RequireAuthorization();

// ===== Quản lý Yêu cầu Đóng Thùng Xe Ô tô Thương Mại (Car Body Building Request - DMS.Sales Sto_CBReq / StoCBReqController / Storage.cs) =====
app.MapPost("/api/body-requests", async (CreateCarBodyRequestDto dto, ICarBodyRequestService svc) =>
{
    try { return Results.Ok(await svc.CreateAsync(dto)); }
    catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
}).RequireAuthorization();

app.MapGet("/api/body-requests", async (ICarBodyRequestService svc, string? status, string? vin, string? storageCodeTo, string? cbReqNo, DateTime? dateFrom, DateTime? dateTo) =>
    Results.Ok(await svc.ListAsync(status, vin, storageCodeTo, cbReqNo, dateFrom, dateTo))).RequireAuthorization();

app.MapGet("/api/body-requests/eligible-cars", async (ICarBodyRequestService svc, string? storageCode) =>
    Results.Ok(await svc.GetEligibleCarsAsync(storageCode))).RequireAuthorization();

app.MapGet("/api/body-requests/stats", async (ICarBodyRequestService svc) =>
    Results.Ok(await svc.StatsAsync())).RequireAuthorization();

app.MapGet("/api/body-requests/{cbReqNo}", async (string cbReqNo, ICarBodyRequestService svc) =>
{
    var r = await svc.DetailAsync(cbReqNo);
    return r is null ? Results.NotFound(new { error = $"Không tìm thấy yêu cầu đóng thùng {cbReqNo}." }) : Results.Ok(r);
}).RequireAuthorization();

app.MapPut("/api/body-requests/{cbReqNo}", async (string cbReqNo, UpdateCarBodyRequestDto dto, ICarBodyRequestService svc) =>
{
    try
    {
        var r = await svc.UpdateAsync(cbReqNo, dto);
        return r is null ? Results.NotFound(new { error = $"Không tìm thấy yêu cầu đóng thùng {cbReqNo}." }) : Results.Ok(r);
    }
    catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
}).RequireAuthorization();

app.MapDelete("/api/body-requests/{cbReqNo}", async (string cbReqNo, ICarBodyRequestService svc) =>
{
    try
    {
        var ok = await svc.DeleteDraftAsync(cbReqNo);
        return !ok ? Results.NotFound(new { error = $"Không tìm thấy yêu cầu đóng thùng {cbReqNo}." }) : Results.Ok(new { success = true, cbReqNo });
    }
    catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
}).RequireAuthorization();

app.MapPut("/api/body-requests/{cbReqNo}/cars/{vin}", async (string cbReqNo, string vin, UpdateCarBodyDetailLineDto dto, ICarBodyRequestService svc) =>
{
    try
    {
        var r = await svc.DetailUpdateAsync(cbReqNo, vin, dto);
        return r is null ? Results.NotFound(new { error = $"Không tìm thấy yêu cầu đóng thùng {cbReqNo} hoặc xe VIN {vin}." }) : Results.Ok(r);
    }
    catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
}).RequireAuthorization();

app.MapPost("/api/body-requests/{cbReqNo}/cars", async (string cbReqNo, List<AddCarBodyDetailLineDto> items, ICarBodyRequestService svc) =>
{
    try
    {
        var r = await svc.AddCarsAsync(cbReqNo, items);
        return r is null ? Results.NotFound(new { error = $"Không tìm thấy yêu cầu đóng thùng {cbReqNo}." }) : Results.Ok(r);
    }
    catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
}).RequireAuthorization();

app.MapDelete("/api/body-requests/{cbReqNo}/cars/{vin}", async (string cbReqNo, string vin, ICarBodyRequestService svc) =>
{
    try
    {
        var r = await svc.RemoveCarAsync(cbReqNo, vin);
        return r is null ? Results.NotFound(new { error = $"Không tìm thấy yêu cầu đóng thùng {cbReqNo} hoặc xe VIN {vin}." }) : Results.Ok(r);
    }
    catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
}).RequireAuthorization();

app.MapPost("/api/body-requests/{cbReqNo}/approve", async (string cbReqNo, ApproveCarBodyRequestDto? dto, ICarBodyRequestService svc) =>
{
    try
    {
        var r = await svc.ApproveAsync(cbReqNo, dto);
        return r is null ? Results.NotFound(new { error = $"Không tìm thấy yêu cầu đóng thùng {cbReqNo}." }) : Results.Ok(r);
    }
    catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
}).RequireAuthorization();

app.MapPost("/api/body-requests/{cbReqNo}/reject", async (string cbReqNo, RejectCarBodyRequestDto dto, ICarBodyRequestService svc) =>
{
    try
    {
        var r = await svc.RejectAsync(cbReqNo, dto);
        return r is null ? Results.NotFound(new { error = $"Không tìm thấy yêu cầu đóng thùng {cbReqNo}." }) : Results.Ok(r);
    }
    catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
}).RequireAuthorization();

app.MapPost("/api/body-requests/{cbReqNo}/cancel", async (string cbReqNo, CancelCarBodyRequestDto dto, ICarBodyRequestService svc) =>
{
    try
    {
        var r = await svc.CancelAsync(cbReqNo, dto);
        return r is null ? Results.NotFound(new { error = $"Không tìm thấy yêu cầu đóng thùng {cbReqNo}." }) : Results.Ok(r);
    }
    catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
}).RequireAuthorization();

app.MapPost("/api/body-requests/{cbReqNo}/complete", async (string cbReqNo, CompleteCarBodyRequestDto? dto, ICarBodyRequestService svc) =>
{
    try
    {
        var r = await svc.CompleteAsync(cbReqNo, dto);
        return r is null ? Results.NotFound(new { error = $"Không tìm thấy yêu cầu đóng thùng {cbReqNo}." }) : Results.Ok(r);
    }
    catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
}).RequireAuthorization();

// ---- Quản lý Lượt lái thử xe Khách hàng Đại lý (DMS.Sales Dlr_DriveTest / DlrDriveTestController / DealerRetail.cs) ----
app.MapPost("/api/drive-tests", async (CreateDealerDriveTestDto dto, IDealerDriveTestService svc) =>
{
    try { return Results.Ok(await svc.CreateAsync(dto)); }
    catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
}).RequireAuthorization();

app.MapGet("/api/drive-tests", async (IDealerDriveTestService svc, string? status, string? dealerCode, string? modelCode, string? plateNo, string? driveTestGroup, string? driveTestType, string? customerPhone, DateTime? dateFrom, DateTime? dateTo) =>
    Results.Ok(await svc.ListAsync(status, dealerCode, modelCode, plateNo, driveTestGroup, driveTestType, customerPhone, dateFrom, dateTo))).RequireAuthorization();

app.MapGet("/api/drive-tests/eligible-test-cars", async (IDealerDriveTestService svc, string? dealerCode) =>
    Results.Ok(await svc.GetEligibleTestCarsAsync(dealerCode))).RequireAuthorization();

app.MapGet("/api/drive-tests/stats", async (IDealerDriveTestService svc) =>
    Results.Ok(await svc.StatsAsync())).RequireAuthorization();

app.MapGet("/api/drive-tests/{driveTestCode}", async (string driveTestCode, IDealerDriveTestService svc) =>
{
    var r = await svc.DetailAsync(driveTestCode);
    return r is null ? Results.NotFound(new { error = $"Không tìm thấy lượt lái thử {driveTestCode}." }) : Results.Ok(r);
}).RequireAuthorization();

app.MapPut("/api/drive-tests/{driveTestCode}", async (string driveTestCode, UpdateDealerDriveTestDto dto, IDealerDriveTestService svc) =>
{
    try
    {
        var r = await svc.UpdateAsync(driveTestCode, dto);
        return r is null ? Results.NotFound(new { error = $"Không tìm thấy lượt lái thử {driveTestCode}." }) : Results.Ok(r);
    }
    catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
}).RequireAuthorization();

app.MapDelete("/api/drive-tests/{driveTestCode}", async (string driveTestCode, IDealerDriveTestService svc) =>
{
    try
    {
        var ok = await svc.DeleteDraftAsync(driveTestCode);
        return ok ? Results.Ok(new { message = $"Đã xóa nháp lượt lái thử {driveTestCode} thành công." }) : Results.NotFound(new { error = $"Không tìm thấy lượt lái thử {driveTestCode}." });
    }
    catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
}).RequireAuthorization();

app.MapPost("/api/drive-tests/{driveTestCode}/approve-hq", async (string driveTestCode, ApproveDealerDriveTestDto? dto, IDealerDriveTestService svc) =>
{
    try
    {
        var r = await svc.ApproveHqAsync(driveTestCode, dto);
        return r is null ? Results.NotFound(new { error = $"Không tìm thấy lượt lái thử {driveTestCode}." }) : Results.Ok(r);
    }
    catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
}).RequireAuthorization();

app.MapPost("/api/drive-tests/approve-multiple", async (BatchDriveTestCodeDto dto, IDealerDriveTestService svc) =>
{
    try
    {
        var r = await svc.ApproveMultipleHqAsync(dto.DriveTestCodes, new ApproveDealerDriveTestDto(dto.ApprovedAmount, dto.ActionBy, null));
        return Results.Ok(new { message = $"Đã duyệt thành công {r.Count} lượt lái thử.", items = r });
    }
    catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
}).RequireAuthorization();

app.MapPost("/api/drive-tests/{driveTestCode}/reject-hq", async (string driveTestCode, RejectDealerDriveTestDto dto, IDealerDriveTestService svc) =>
{
    try
    {
        var r = await svc.RejectHqAsync(driveTestCode, dto);
        return r is null ? Results.NotFound(new { error = $"Không tìm thấy lượt lái thử {driveTestCode}." }) : Results.Ok(r);
    }
    catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
}).RequireAuthorization();

app.MapPost("/api/drive-tests/reject-multiple", async (BatchDriveTestCodeDto dto, IDealerDriveTestService svc) =>
{
    try
    {
        var r = await svc.RejectMultipleHqAsync(dto.DriveTestCodes, new RejectDealerDriveTestDto(dto.RejectReason ?? "NPP từ chối duyệt hàng loạt.", dto.ActionBy));
        return Results.Ok(new { message = $"Đã từ chối duyệt {r.Count} lượt lái thử.", items = r });
    }
    catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
}).RequireAuthorization();

app.MapPost("/api/drive-tests/{driveTestCode}/cancel", async (string driveTestCode, CancelDealerDriveTestDto dto, IDealerDriveTestService svc) =>
{
    try
    {
        var r = await svc.CancelAsync(driveTestCode, dto);
        return r is null ? Results.NotFound(new { error = $"Không tìm thấy lượt lái thử {driveTestCode}." }) : Results.Ok(r);
    }
    catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
}).RequireAuthorization();

// ===== Quản lý Lệnh Điều Chuyển Đóng Thùng Xe Ô Tô Thương Mại (Commercial Vehicle Storage Rearrange & Body Building Order - DMS.Sales Sto_RearrangeCB / StoRearrangeCBController / Storage.cs) =====
app.MapPost("/api/rearrange-cb", async (CreateStoRearrangeCBDto dto, IStorageRearrangeCBService svc) =>
{
    try { return Results.Ok(await svc.CreateAsync(dto)); }
    catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
}).RequireAuthorization();

app.MapGet("/api/rearrange-cb", async (IStorageRearrangeCBService svc, string? status, string? vin, string? storageCodeTo, string? stoRearCBNo, DateTime? dateFrom, DateTime? dateTo) =>
    Results.Ok(await svc.ListAsync(status, vin, storageCodeTo, stoRearCBNo, dateFrom, dateTo))).RequireAuthorization();

app.MapGet("/api/rearrange-cb/eligible-cars", async (IStorageRearrangeCBService svc, string? storageCodeTo) =>
    Results.Ok(await svc.GetEligibleCarsAsync(storageCodeTo))).RequireAuthorization();

app.MapGet("/api/rearrange-cb/stats", async (IStorageRearrangeCBService svc) =>
    Results.Ok(await svc.StatsAsync())).RequireAuthorization();

app.MapGet("/api/rearrange-cb/{stoRearCBNo}", async (string stoRearCBNo, IStorageRearrangeCBService svc) =>
{
    var r = await svc.DetailAsync(stoRearCBNo);
    return r is null ? Results.NotFound(new { error = $"Không tìm thấy lệnh điều chuyển đóng thùng {stoRearCBNo}." }) : Results.Ok(r);
}).RequireAuthorization();

app.MapPut("/api/rearrange-cb/{stoRearCBNo}", async (string stoRearCBNo, UpdateStoRearrangeCBDto dto, IStorageRearrangeCBService svc) =>
{
    try
    {
        var r = await svc.UpdateAsync(stoRearCBNo, dto);
        return r is null ? Results.NotFound(new { error = $"Không tìm thấy lệnh điều chuyển đóng thùng {stoRearCBNo}." }) : Results.Ok(r);
    }
    catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
}).RequireAuthorization();

app.MapDelete("/api/rearrange-cb/{stoRearCBNo}", async (string stoRearCBNo, IStorageRearrangeCBService svc) =>
{
    try
    {
        var ok = await svc.DeleteDraftAsync(stoRearCBNo);
        return !ok ? Results.NotFound(new { error = $"Không tìm thấy lệnh điều chuyển đóng thùng {stoRearCBNo}." }) : Results.Ok(new { success = true, stoRearCBNo });
    }
    catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
}).RequireAuthorization();

app.MapPut("/api/rearrange-cb/{stoRearCBNo}/cars/{vin}", async (string stoRearCBNo, string vin, UpdateStoRearrangeCBLineDto dto, IStorageRearrangeCBService svc) =>
{
    try
    {
        var r = await svc.DetailUpdateAsync(stoRearCBNo, vin, dto);
        return r is null ? Results.NotFound(new { error = $"Không tìm thấy lệnh điều chuyển đóng thùng {stoRearCBNo} hoặc xe VIN {vin}." }) : Results.Ok(r);
    }
    catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
}).RequireAuthorization();

app.MapPost("/api/rearrange-cb/{stoRearCBNo}/cars", async (string stoRearCBNo, List<StoRearrangeCBItemInputDto> items, IStorageRearrangeCBService svc) =>
{
    try
    {
        var r = await svc.AddCarsAsync(stoRearCBNo, items);
        return r is null ? Results.NotFound(new { error = $"Không tìm thấy lệnh điều chuyển đóng thùng {stoRearCBNo}." }) : Results.Ok(r);
    }
    catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
}).RequireAuthorization();

app.MapDelete("/api/rearrange-cb/{stoRearCBNo}/cars/{vin}", async (string stoRearCBNo, string vin, IStorageRearrangeCBService svc) =>
{
    try
    {
        var r = await svc.RemoveCarAsync(stoRearCBNo, vin);
        return r is null ? Results.NotFound(new { error = $"Không tìm thấy lệnh điều chuyển đóng thùng {stoRearCBNo} hoặc xe VIN {vin}." }) : Results.Ok(r);
    }
    catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
}).RequireAuthorization();

app.MapPost("/api/rearrange-cb/{stoRearCBNo}/approve", async (string stoRearCBNo, ApproveStoRearrangeCBDto? dto, IStorageRearrangeCBService svc) =>
{
    try
    {
        var r = await svc.ApproveAsync(stoRearCBNo, dto);
        return r is null ? Results.NotFound(new { error = $"Không tìm thấy lệnh điều chuyển đóng thùng {stoRearCBNo}." }) : Results.Ok(r);
    }
    catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
}).RequireAuthorization();

app.MapPost("/api/rearrange-cb/{stoRearCBNo}/reject", async (string stoRearCBNo, RejectStoRearrangeCBDto dto, IStorageRearrangeCBService svc) =>
{
    try
    {
        var r = await svc.RejectAsync(stoRearCBNo, dto);
        return r is null ? Results.NotFound(new { error = $"Không tìm thấy lệnh điều chuyển đóng thùng {stoRearCBNo}." }) : Results.Ok(r);
    }
    catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
}).RequireAuthorization();

app.MapPost("/api/rearrange-cb/{stoRearCBNo}/cancel", async (string stoRearCBNo, CancelStoRearrangeCBDto dto, IStorageRearrangeCBService svc) =>
{
    try
    {
        var r = await svc.CancelAsync(stoRearCBNo, dto);
        return r is null ? Results.NotFound(new { error = $"Không tìm thấy lệnh điều chuyển đóng thùng {stoRearCBNo}." }) : Results.Ok(r);
    }
    catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
}).RequireAuthorization();

// ===== Quản lý Hủy Xe Ô tô & Khôi phục Xe Hủy / Điều Hành Cờ Xe (Vehicle Cancellation Management - DMS.Sales CarCancelController / Car.1.cs / FrmMngCarCancel / FrmCarCancel / FrmCapNhatTTHuyXe) =====
app.MapGet("/api/car-cancels", async (ICarCancelService svc, string? carCancelType, string? carId, string? vin, string? modelCode, string? dealerCode, string? soCode, string? flagEarlyCancel, string? flagActive, DateTime? carCancelDateFrom, DateTime? carCancelDateTo) =>
    Results.Ok(await svc.SearchAsync(new CarCancelSearchFilterDto(carCancelType, carId, vin, modelCode, dealerCode, soCode, flagEarlyCancel, flagActive ?? "0", carCancelDateFrom, carCancelDateTo)))).RequireAuthorization();

app.MapGet("/api/car-cancels/stats", async (ICarCancelService svc) =>
    Results.Ok(await svc.StatsAsync())).RequireAuthorization();

app.MapGet("/api/car-cancels/reasons", async (ICarCancelService svc) =>
    Results.Ok(await svc.GetReasonsAsync())).RequireAuthorization();

app.MapGet("/api/car-cancels/eligible-cars", async (ICarCancelService svc, string? dealerCode, string? modelCode) =>
    Results.Ok(await svc.GetEligibleToCancelAsync(dealerCode, modelCode))).RequireAuthorization();

app.MapGet("/api/car-cancels/logs", async (ICarCancelService svc, string? carId, string? vin) =>
    Results.Ok(await svc.GetLogsAsync(carId, vin))).RequireAuthorization();

app.MapGet("/api/car-cancels/{carId}", async (string carId, ICarCancelService svc) =>
{
    var r = await svc.GetByIdAsync(carId);
    return r is null ? Results.NotFound(new { error = $"Không tìm thấy thông tin xe với mã định danh hoặc số khung '{carId}'." }) : Results.Ok(r);
}).RequireAuthorization();

app.MapPost("/api/car-cancels", async (CancelCarInputDto dto, ICarCancelService svc) =>
{
    try { return Results.Ok(await svc.CancelAsync(dto)); }
    catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
}).RequireAuthorization();

app.MapPut("/api/car-cancels/{carId}/flags", async (string carId, UpdateCarFlagsDto dto, ICarCancelService svc) =>
{
    try
    {
        var r = await svc.UpdateFlagsAsync(carId, dto);
        return r is null ? Results.NotFound(new { error = $"Không tìm thấy thông tin xe với mã định danh '{carId}'." }) : Results.Ok(r);
    }
    catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
}).RequireAuthorization();

app.MapPost("/api/car-cancels/restore-batch", async (BatchCarIdsDto dto, ICarCancelService svc) =>
{
    try { return Results.Ok(await svc.RestoreBatchAsync(dto)); }
    catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
}).RequireAuthorization();

app.MapPost("/api/car-cancels/cancel-batch", async (BatchCarCancelDto dto, ICarCancelService svc) =>
{
    try { return Results.Ok(await svc.CancelBatchAsync(dto)); }
    catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
}).RequireAuthorization();

// ===== Quản lý Phân bổ & Gán số khung VIN xe ô tô Đại lý - NPP (Auto & Manual Map VIN / Unmap VIN Management - DMS.Sales Auto_MapVIN + Car_VIN_Map_HQ + Car_VIN_Unmap_HQ / AutoMapVINController / MapVIN.cs) =====
app.MapGet("/api/map-vins/sessions", async (IMapVinService svc, string? status, string? method, string? dealerCode) =>
    Results.Ok(await svc.GetSessionsAsync(status, method, dealerCode))).RequireAuthorization();

app.MapGet("/api/map-vins/sessions/{sessionNo}", async (string sessionNo, IMapVinService svc) =>
{
    var r = await svc.GetSessionByNoAsync(sessionNo);
    return r is null ? Results.NotFound(new { error = $"Không tìm thấy phiên Map VIN {sessionNo}." }) : Results.Ok(r);
}).RequireAuthorization();

app.MapPost("/api/map-vins/sessions/auto", async (AutoMapVinRunDto dto, IMapVinService svc) =>
{
    try { return Results.Ok(await svc.RunAutoMapAsync(dto)); }
    catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
}).RequireAuthorization();

app.MapPost("/api/map-vins/manual-map", async (ManualMapVinDto dto, IMapVinService svc) =>
{
    try { return Results.Ok(await svc.ManualMapAsync(dto)); }
    catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
}).RequireAuthorization();

app.MapPost("/api/map-vins/unmap", async (UnmapVinDto dto, IMapVinService svc) =>
{
    try { return Results.Ok(await svc.UnmapAsync(dto)); }
    catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
}).RequireAuthorization();

app.MapPost("/api/map-vins/unmap-batch", async (BatchUnmapVinDto dto, IMapVinService svc) =>
{
    try { return Results.Ok(await svc.BatchUnmapAsync(dto)); }
    catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
}).RequireAuthorization();

app.MapGet("/api/map-vins/available-vins", async (IMapVinService svc, string? modelCode, string? colorCode, string? storageCode) =>
    Results.Ok(await svc.GetAvailableVinsAsync(modelCode, colorCode, storageCode))).RequireAuthorization();

app.MapGet("/api/map-vins/pending-cars", async (IMapVinService svc, string? dealerCode, string? modelCode) =>
    Results.Ok(await svc.GetPendingCarsAsync(dealerCode, modelCode))).RequireAuthorization();

app.MapGet("/api/map-vins/stats", async (IMapVinService svc) =>
    Results.Ok(await svc.GetStatsAsync())).RequireAuthorization();

app.MapGet("/api/map-vins/logs", async (IMapVinService svc, string? carId, string? vin) =>
    Results.Ok(await svc.GetLogsAsync(carId, vin))).RequireAuthorization();

// ===== Quản lý Đề nghị xuất Hóa đơn GTGT Bán lẻ Xe ô tô Khách hàng Đại lý (Retail VAT Invoice Request - DMS.Sales Req_RequestInvoice + Req_RequestInvoiceDtl / ReqRequestInvoiceController / ReqInvoice.cs) =====
app.MapGet("/api/retail-invoices", async (IRetailInvoiceService svc, string? status, string? invoiceType, string? dealerCode, string? customerName, string? vin, string? reqInvoiceNo, DateTime? dateFrom, DateTime? dateTo) =>
    Results.Ok(await svc.ListAsync(status, invoiceType, dealerCode, customerName, vin, reqInvoiceNo, dateFrom, dateTo))).RequireAuthorization();

app.MapGet("/api/retail-invoices/stats", async (IRetailInvoiceService svc) =>
    Results.Ok(await svc.GetStatsAsync())).RequireAuthorization();

app.MapGet("/api/retail-invoices/eligible-cars", async (IRetailInvoiceService svc, string? dealerCode, string? contractNo) =>
    Results.Ok(await svc.GetEligibleCarsAsync(dealerCode, contractNo))).RequireAuthorization();

app.MapGet("/api/retail-invoices/{reqInvoiceNo}", async (string reqInvoiceNo, IRetailInvoiceService svc) =>
{
    var r = await svc.GetByNoAsync(reqInvoiceNo);
    return r is null ? Results.NotFound(new { error = $"Không tìm thấy đề nghị xuất hóa đơn bán lẻ '{reqInvoiceNo}'." }) : Results.Ok(r);
}).RequireAuthorization();

app.MapPost("/api/retail-invoices/base", async (CreateRetailInvoiceRequestDto dto, IRetailInvoiceService svc) =>
{
    try { return Results.Ok(await svc.CreateBaseAsync(dto)); }
    catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
}).RequireAuthorization();

app.MapPost("/api/retail-invoices/adjust", async (CreateAdjustedRetailInvoiceDto dto, IRetailInvoiceService svc) =>
{
    try { return Results.Ok(await svc.CreateAdjustedAsync(dto)); }
    catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
}).RequireAuthorization();

app.MapPost("/api/retail-invoices/replace", async (CreateReplacedRetailInvoiceDto dto, IRetailInvoiceService svc) =>
{
    try { return Results.Ok(await svc.CreateReplacedAsync(dto)); }
    catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
}).RequireAuthorization();

app.MapPut("/api/retail-invoices/{reqInvoiceNo}", async (string reqInvoiceNo, UpdateRetailInvoiceRequestDto dto, IRetailInvoiceService svc) =>
{
    try
    {
        var r = await svc.UpdateAsync(reqInvoiceNo, dto);
        return Results.Ok(r);
    }
    catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
}).RequireAuthorization();

app.MapDelete("/api/retail-invoices/{reqInvoiceNo}", async (string reqInvoiceNo, IRetailInvoiceService svc) =>
{
    try
    {
        var ok = await svc.DeleteDraftAsync(reqInvoiceNo);
        return ok
            ? Results.Ok(new { message = $"Đã xóa đề nghị xuất hóa đơn bán lẻ '{reqInvoiceNo}' thành công." })
            : Results.NotFound(new { error = $"Không tìm thấy đề nghị xuất hóa đơn '{reqInvoiceNo}'." });
    }
    catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
}).RequireAuthorization();

app.MapPost("/api/retail-invoices/{reqInvoiceNo}/issue", async (string reqInvoiceNo, IssueRetailInvoiceDto? dto, IRetailInvoiceService svc) =>
{
    try
    {
        var r = await svc.IssueEInvoiceAsync(reqInvoiceNo, dto);
        return Results.Ok(r);
    }
    catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
}).RequireAuthorization();

app.MapPost("/api/retail-invoices/{reqInvoiceNo}/sign-transmit", async (string reqInvoiceNo, SignAndTransmitRetailInvoiceDto? dto, IRetailInvoiceService svc) =>
{
    try
    {
        var r = await svc.SignAndTransmitAsync(reqInvoiceNo, dto);
        return Results.Ok(r);
    }
    catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
}).RequireAuthorization();

app.MapPost("/api/retail-invoices/{reqInvoiceNo}/cancel", async (string reqInvoiceNo, CancelRetailInvoiceDto dto, IRetailInvoiceService svc) =>
{
    try
    {
        var r = await svc.CancelAsync(reqInvoiceNo, dto);
        return Results.Ok(r);
    }
    catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
}).RequireAuthorization();

// ===== Quản lý Đề nghị Giải chấp Xe ô tô Thế chấp Ngân hàng / Nhà phân phối (Car Mortgage Redemption Management - DMS.Sales RD_ReqRedeem + RD_ReqRedeemDtl / RDReqRedeemController / Redeem.cs) =====
app.MapGet("/api/car-redeems", async (ICarRedeemService svc, string? status, string? redeemType, string? dealerCode, string? bankCode, string? vin, string? reqDMNo, DateTime? dateFrom, DateTime? dateTo) =>
    Results.Ok(await svc.ListAsync(status, redeemType, dealerCode, bankCode, vin, reqDMNo, dateFrom, dateTo))).RequireAuthorization();

app.MapGet("/api/car-redeems/stats", async (ICarRedeemService svc) =>
    Results.Ok(await svc.GetStatsAsync())).RequireAuthorization();

app.MapGet("/api/car-redeems/eligible-cars", async (ICarRedeemService svc, string? dealerCode, string? bankCode) =>
    Results.Ok(await svc.GetEligibleCarsAsync(dealerCode, bankCode))).RequireAuthorization();

app.MapGet("/api/car-redeems/{reqDMNo}", async (string reqDMNo, ICarRedeemService svc) =>
{
    var r = await svc.GetByNoAsync(reqDMNo);
    return r is null ? Results.NotFound(new { error = $"Không tìm thấy đề nghị giải chấp '{reqDMNo}'." }) : Results.Ok(r);
}).RequireAuthorization();

app.MapPost("/api/car-redeems", async (CreateCarRedeemRequestDto dto, ICarRedeemService svc) =>
{
    try
    {
        var r = await svc.CreateAsync(dto);
        return Results.Created($"/api/car-redeems/{r.ReqDMNo}", r);
    }
    catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
}).RequireAuthorization();

app.MapPut("/api/car-redeems/{reqDMNo}", async (string reqDMNo, UpdateCarRedeemRequestDto dto, ICarRedeemService svc) =>
{
    try
    {
        var r = await svc.UpdateAsync(reqDMNo, dto);
        return Results.Ok(r);
    }
    catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
}).RequireAuthorization();

app.MapDelete("/api/car-redeems/{reqDMNo}", async (string reqDMNo, ICarRedeemService svc) =>
{
    try
    {
        var r = await svc.DeleteDraftAsync(reqDMNo);
        return Results.Ok(new { success = r, message = $"Đã xóa đề nghị giải chấp '{reqDMNo}'." });
    }
    catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
}).RequireAuthorization();

app.MapPost("/api/car-redeems/{reqDMNo}/approve-detail/{vin}", async (string reqDMNo, string vin, ApproveRedeemDetailDto? dto, ICarRedeemService svc) =>
{
    try
    {
        var r = await svc.ApproveDtlAsync(reqDMNo, vin, dto);
        return Results.Ok(r);
    }
    catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
}).RequireAuthorization();

app.MapPost("/api/car-redeems/{reqDMNo}/approve-all", async (string reqDMNo, ApproveRedeemRequestDto? dto, ICarRedeemService svc) =>
{
    try
    {
        var r = await svc.ApproveAllAsync(reqDMNo, dto);
        return Results.Ok(r);
    }
    catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
}).RequireAuthorization();

app.MapPost("/api/car-redeems/{reqDMNo}/reject", async (string reqDMNo, RejectRedeemRequestDto dto, ICarRedeemService svc) =>
{
    try
    {
        var r = await svc.RejectAsync(reqDMNo, dto);
        return Results.Ok(r);
    }
    catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
}).RequireAuthorization();

app.MapPost("/api/car-redeems/{reqDMNo}/cancel", async (string reqDMNo, CancelRedeemRequestDto dto, ICarRedeemService svc) =>
{
    try
    {
        var r = await svc.CancelAsync(reqDMNo, dto);
        return Results.Ok(r);
    }
    catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
}).RequireAuthorization();

app.MapPost("/api/car-redeems/{reqDMNo}/cars", async (string reqDMNo, AddCarRedeemDetailDto dto, ICarRedeemService svc) =>
{
    try
    {
        var r = await svc.AddCarAsync(reqDMNo, dto);
        return Results.Ok(r);
    }
    catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
}).RequireAuthorization();

app.MapDelete("/api/car-redeems/{reqDMNo}/cars/{vin}", async (string reqDMNo, string vin, ICarRedeemService svc) =>
{
    try
    {
        var r = await svc.RemoveCarAsync(reqDMNo, vin);
        return Results.Ok(new { success = true, result = r });
    }
    catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
}).RequireAuthorization();

// ===== Quản lý Yêu cầu Bảo hiểm Xe ô tô Vận chuyển & Giao nhận (Vehicle Insurance Request Management - DMS.Sales Ins_InsuranceReq + Ins_InsuranceReqDtl / Master.cs / 08_BAO_HIEM.md) =====
app.MapGet("/api/car-insurances", async (ICarInsuranceService svc, string? status, string? insCompanyCode, string? insTypeCode, string? refOrdType, string? vin, string? insReqNo, DateTime? dateFrom, DateTime? dateTo) =>
    Results.Ok(await svc.ListAsync(status, insCompanyCode, insTypeCode, refOrdType, vin, insReqNo, dateFrom, dateTo))).RequireAuthorization();

app.MapGet("/api/car-insurances/stats", async (ICarInsuranceService svc) =>
    Results.Ok(await svc.GetStatsAsync())).RequireAuthorization();

app.MapGet("/api/car-insurances/eligible-cars", async (ICarInsuranceService svc, string? refOrdType, string? keyword) =>
    Results.Ok(await svc.GetEligibleCarsAsync(refOrdType, keyword))).RequireAuthorization();

app.MapGet("/api/car-insurances/companies", (ICarInsuranceService svc) =>
    Results.Ok(svc.GetCompanies())).RequireAuthorization();

app.MapGet("/api/car-insurances/types", (ICarInsuranceService svc) =>
    Results.Ok(svc.GetInsuranceTypes())).RequireAuthorization();

app.MapGet("/api/car-insurances/{insReqNo}", async (string insReqNo, ICarInsuranceService svc) =>
{
    var item = await svc.GetByNoAsync(insReqNo);
    return item is null ? Results.NotFound(new { error = $"Không tìm thấy yêu cầu bảo hiểm '{insReqNo}'." }) : Results.Ok(item);
}).RequireAuthorization();

app.MapPost("/api/car-insurances", async (CreateCarInsuranceRequestDto dto, ICarInsuranceService svc) =>
{
    try
    {
        var result = await svc.CreateAsync(dto);
        return Results.Created($"/api/car-insurances/{result.InsReqNo}", result);
    }
    catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
}).RequireAuthorization();

app.MapPut("/api/car-insurances/{insReqNo}/cars/{vin}", async (string insReqNo, string vin, UpdateCarInsuranceDetailDto dto, ICarInsuranceService svc) =>
{
    try
    {
        var result = await svc.UpdateDetailAsync(insReqNo, vin, dto);
        return Results.Ok(result);
    }
    catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
}).RequireAuthorization();

app.MapDelete("/api/car-insurances/{insReqNo}", async (string insReqNo, ICarInsuranceService svc) =>
{
    try
    {
        var ok = await svc.DeleteDraftAsync(insReqNo);
        return Results.Ok(new { success = ok });
    }
    catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
}).RequireAuthorization();

app.MapPost("/api/car-insurances/{insReqNo}/approve", async (string insReqNo, ApproveCarInsuranceRequestDto? dto, ICarInsuranceService svc) =>
{
    try
    {
        var result = await svc.ApproveAsync(insReqNo, dto);
        return Results.Ok(result);
    }
    catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
}).RequireAuthorization();

app.MapPost("/api/car-insurances/{insReqNo}/reject", async (string insReqNo, RejectCarInsuranceRequestDto dto, ICarInsuranceService svc) =>
{
    try
    {
        var result = await svc.RejectAsync(insReqNo, dto);
        return Results.Ok(result);
    }
    catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
}).RequireAuthorization();

app.MapPost("/api/car-insurances/{insReqNo}/cancel", async (string insReqNo, CancelCarInsuranceRequestDto dto, ICarInsuranceService svc) =>
{
    try
    {
        var result = await svc.CancelAsync(insReqNo, dto);
        return Results.Ok(result);
    }
    catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
}).RequireAuthorization();

app.MapPost("/api/car-insurances/{insReqNo}/cars", async (string insReqNo, AddCarInsuranceDetailDto dto, ICarInsuranceService svc) =>
{
    try
    {
        var result = await svc.AddCarAsync(insReqNo, dto);
        return Results.Ok(result);
    }
    catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
}).RequireAuthorization();

app.MapDelete("/api/car-insurances/{insReqNo}/cars/{vin}", async (string insReqNo, string vin, ICarInsuranceService svc) =>
{
    try
    {
        var result = await svc.RemoveCarAsync(insReqNo, vin);
        return Results.Ok(new { success = true, result });
    }
    catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
}).RequireAuthorization();

// ==================== QUẢN LÝ KẾ HOẠCH VẬN TẢI XE Ô TÔ (Sto_TranspPlan / TranspPlan.cs) ====================
app.MapGet("/api/transport-plans", async (
    string? status, string? transporterCode, string? storageCode, string? dealerCode,
    string? modelCode, bool? flagRealVin, DateTime? dateFrom, DateTime? dateTo,
    string? vinPlan, string? vin, ITransportPlanService svc) =>
{
    var res = await svc.ListAsync(status, transporterCode, storageCode, dealerCode, modelCode, flagRealVin, dateFrom, dateTo, vinPlan, vin);
    return Results.Ok(res);
}).RequireAuthorization();

app.MapGet("/api/transport-plans/stats", async (ITransportPlanService svc) =>
{
    var res = await svc.GetStatsAsync();
    return Results.Ok(res);
}).RequireAuthorization();

app.MapGet("/api/transport-plans/eligible-cars", async (string? storageCode, string? keyword, ITransportPlanService svc) =>
{
    var res = await svc.GetEligibleCarsForPlanAsync(storageCode, keyword);
    return Results.Ok(res);
}).RequireAuthorization();

app.MapGet("/api/transport-plans/transporters", (ITransportPlanService svc) =>
{
    var res = svc.GetTransporters();
    return Results.Ok(res);
}).RequireAuthorization();

app.MapGet("/api/transport-plans/{planNo}", async (string planNo, ITransportPlanService svc) =>
{
    var res = await svc.GetByPlanNoAsync(planNo);
    return res is null ? Results.NotFound(new { error = $"Không tìm thấy kế hoạch vận tải {planNo}" }) : Results.Ok(res);
}).RequireAuthorization();

app.MapPost("/api/transport-plans", async (CreateTransportPlanDto dto, ITransportPlanService svc) =>
{
    try
    {
        var plan = await svc.CreateAsync(dto);
        return Results.Created($"/api/transport-plans/{plan.PlanNo}", plan);
    }
    catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
}).RequireAuthorization();

app.MapPut("/api/transport-plans/{planNo}/by-kehoach", async (string planNo, UpdateByKeHoachDto dto, ITransportPlanService svc) =>
{
    try
    {
        var res = await svc.UpdateByKeHoachAsync(planNo, dto);
        return res is null ? Results.NotFound(new { error = $"Không tìm thấy kế hoạch {planNo}" }) : Results.Ok(res);
    }
    catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
}).RequireAuthorization();

app.MapPut("/api/transport-plans/{planNo}/by-logistic", async (string planNo, UpdateByLogisticDto dto, ITransportPlanService svc) =>
{
    try
    {
        var res = await svc.UpdateByLogisticAsync(planNo, dto);
        return res is null ? Results.NotFound(new { error = $"Không tìm thấy kế hoạch {planNo}" }) : Results.Ok(res);
    }
    catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
}).RequireAuthorization();

app.MapPut("/api/transport-plans/{planNo}/by-banhang", async (string planNo, UpdateByBanHangDto dto, ITransportPlanService svc) =>
{
    try
    {
        var res = await svc.UpdateByBanHangAsync(planNo, dto);
        return res is null ? Results.NotFound(new { error = $"Không tìm thấy kế hoạch {planNo}" }) : Results.Ok(res);
    }
    catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
}).RequireAuthorization();

app.MapPost("/api/transport-plans/{planNo}/map-vin-real", async (string planNo, MapVinRealDto dto, ITransportPlanService svc) =>
{
    try
    {
        var res = await svc.MapVinRealAsync(planNo, dto);
        return res is null ? Results.NotFound(new { error = $"Không tìm thấy kế hoạch {planNo}" }) : Results.Ok(res);
    }
    catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
}).RequireAuthorization();

app.MapPost("/api/transport-plans/{planNo}/approve", async (string planNo, ApproveTransportPlanDto? dto, ITransportPlanService svc) =>
{
    try
    {
        var res = await svc.ApproveAsync(planNo, dto);
        return res is null ? Results.NotFound(new { error = $"Không tìm thấy kế hoạch {planNo}" }) : Results.Ok(res);
    }
    catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
}).RequireAuthorization();

app.MapPost("/api/transport-plans/{planNo}/complete", async (string planNo, CompleteTransportPlanDto? dto, ITransportPlanService svc) =>
{
    try
    {
        var res = await svc.CompleteAsync(planNo, dto);
        return res is null ? Results.NotFound(new { error = $"Không tìm thấy kế hoạch {planNo}" }) : Results.Ok(res);
    }
    catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
}).RequireAuthorization();

app.MapPost("/api/transport-plans/{planNo}/cancel", async (string planNo, CancelTransportPlanDto dto, ITransportPlanService svc) =>
{
    try
    {
        var res = await svc.CancelAsync(planNo, dto);
        return res is null ? Results.NotFound(new { error = $"Không tìm thấy kế hoạch {planNo}" }) : Results.Ok(res);
    }
    catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
}).RequireAuthorization();

app.MapDelete("/api/transport-plans/{planNo}", async (string planNo, ITransportPlanService svc) =>
{
    try
    {
        var deleted = await svc.DeleteAsync(planNo);
        return deleted ? Results.Ok(new { success = true, planNo }) : Results.NotFound(new { error = $"Không tìm thấy kế hoạch {planNo}" });
    }
    catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
}).RequireAuthorization();

// ===== Quản lý Biên bản Đối soát Thưởng Bán hàng Xe Ô tô Đại lý - NPP (Sale Award Minutes - Rec_SaleAwardMinutes / RecSaleAwardMinutesController / Minutes.cs) =====
app.MapGet("/api/sale-award-minutes/seq", async (ISaleAwardMinutesService svc) =>
    Results.Ok(await svc.GetSeqAsync())).RequireAuthorization();

app.MapGet("/api/sale-award-minutes/stats", async (ISaleAwardMinutesService svc) =>
    Results.Ok(await svc.StatsAsync())).RequireAuthorization();

app.MapGet("/api/sale-award-minutes/eligible-cars", async (ISaleAwardMinutesService svc, string? dealerCode) =>
    Results.Ok(await svc.GetEligibleCarsAsync(null, dealerCode))).RequireAuthorization();

app.MapPost("/api/sale-award-minutes/check-cars", async (List<CheckVinForAwardDto> checkVins, ISaleAwardMinutesService svc, string? dealerCode) =>
    Results.Ok(await svc.GetEligibleCarsAsync(checkVins, dealerCode))).RequireAuthorization();

app.MapGet("/api/sale-award-minutes", async (
    ISaleAwardMinutesService svc,
    string? status,
    string? dealerCode,
    string? documentNo,
    string? vin,
    DateTime? createDateFrom,
    DateTime? createDateTo,
    int page = 0,
    int pageSize = 10
) =>
    Results.Ok(await svc.ListAsync(status, dealerCode, documentNo, vin, createDateFrom, createDateTo, page, pageSize))
).RequireAuthorization();

app.MapGet("/api/sale-award-minutes/{minutesNo}", async (string minutesNo, ISaleAwardMinutesService svc) =>
{
    var res = await svc.DetailAsync(minutesNo);
    return res is null ? Results.NotFound(new { error = $"Không tìm thấy biên bản đối soát thưởng {minutesNo}" }) : Results.Ok(res);
}).RequireAuthorization();

app.MapPost("/api/sale-award-minutes/multi", async (CreateMultiSaleAwardMinutesDto dto, ISaleAwardMinutesService svc) =>
{
    try
    {
        var res = await svc.CreateMultiAsync(dto);
        return Results.Ok(res);
    }
    catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
}).RequireAuthorization();

app.MapPut("/api/sale-award-minutes/{minutesNo}", async (string minutesNo, UpdateSaleAwardMinutesDto dto, ISaleAwardMinutesService svc) =>
{
    try
    {
        var res = await svc.UpdateAsync(minutesNo, dto);
        return res is null ? Results.NotFound(new { error = $"Không tìm thấy biên bản đối soát thưởng {minutesNo}" }) : Results.Ok(res);
    }
    catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
}).RequireAuthorization();

app.MapPost("/api/sale-award-minutes/{minutesNo}/approve1", async (string minutesNo, ApproveAwardMinutesDto? dto, ISaleAwardMinutesService svc) =>
{
    try
    {
        var res = await svc.Approve1Async(minutesNo, dto);
        return res is null ? Results.NotFound(new { error = $"Không tìm thấy biên bản đối soát thưởng {minutesNo}" }) : Results.Ok(res);
    }
    catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
}).RequireAuthorization();

app.MapPost("/api/sale-award-minutes/{minutesNo}/approve2", async (string minutesNo, ApproveAwardMinutesDto? dto, ISaleAwardMinutesService svc) =>
{
    try
    {
        var res = await svc.Approve2Async(minutesNo, dto);
        return res is null ? Results.NotFound(new { error = $"Không tìm thấy biên bản đối soát thưởng {minutesNo}" }) : Results.Ok(res);
    }
    catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
}).RequireAuthorization();

app.MapPost("/api/sale-award-minutes/{minutesNo}/finish", async (string minutesNo, FinishAwardMinutesDto dto, ISaleAwardMinutesService svc) =>
{
    try
    {
        var res = await svc.FinishAsync(minutesNo, dto);
        return res is null ? Results.NotFound(new { error = $"Không tìm thấy biên bản đối soát thưởng {minutesNo}" }) : Results.Ok(res);
    }
    catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
}).RequireAuthorization();

app.MapPost("/api/sale-award-minutes/{minutesNo}/reject", async (string minutesNo, RejectAwardMinutesDto dto, ISaleAwardMinutesService svc) =>
{
    try
    {
        var res = await svc.RejectAsync(minutesNo, dto);
        return res is null ? Results.NotFound(new { error = $"Không tìm thấy biên bản đối soát thưởng {minutesNo}" }) : Results.Ok(res);
    }
    catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
}).RequireAuthorization();

app.MapPost("/api/sale-award-minutes/{minutesNo}/cancel", async (string minutesNo, CancelAwardMinutesDto dto, ISaleAwardMinutesService svc) =>
{
    try
    {
        var res = await svc.CancelAsync(minutesNo, dto);
        return res is null ? Results.NotFound(new { error = $"Không tìm thấy biên bản đối soát thưởng {minutesNo}" }) : Results.Ok(res);
    }
    catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
}).RequireAuthorization();

app.MapDelete("/api/sale-award-minutes/{minutesNo}", async (string minutesNo, ISaleAwardMinutesService svc) =>
{
    try
    {
        var deleted = await svc.DeleteDraftAsync(minutesNo);
        return deleted ? Results.Ok(new { success = true, minutesNo }) : Results.NotFound(new { error = $"Không tìm thấy biên bản đối soát thưởng {minutesNo}" });
    }
    catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
}).RequireAuthorization();

// ==================== KẾ HOẠCH KINH DOANH BÁN BUÔN / BÁN LẺ ĐẠI LÝ (BPL_BusinessPlan) ====================
app.MapGet("/api/business-plans/seq", async (IBusinessPlanService svc) => Results.Ok(await svc.GetSeqAsync())).RequireAuthorization();

app.MapGet("/api/business-plans/models", async (IBusinessPlanService svc) => Results.Ok(await svc.GetStandardModelsAsync())).RequireAuthorization();

app.MapPost("/api/business-plans/calculate", async (CalcPlanRequestDto dto, IBusinessPlanService svc) =>
{
    try
    {
        var res = await svc.CalculateInitialPlanAsync(dto);
        return Results.Ok(res);
    }
    catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
}).RequireAuthorization();

app.MapGet("/api/business-plans", async (int? yearPlan, string? dealerCode, BusinessPlanStatus? status, string? version, int page, int pageSize, IBusinessPlanService svc) =>
{
    var p = page < 0 ? 0 : page;
    var ps = pageSize <= 0 ? 10 : pageSize;
    return Results.Ok(await svc.ListAsync(yearPlan, dealerCode, status, version, p, ps));
}).RequireAuthorization();

app.MapGet("/api/business-plans/stats", async (int? yearPlan, IBusinessPlanService svc) => Results.Ok(await svc.StatsAsync(yearPlan))).RequireAuthorization();

app.MapGet("/api/business-plans/{planCode}", async (string planCode, IBusinessPlanService svc) =>
{
    var res = await svc.DetailAsync(planCode);
    return res is null ? Results.NotFound(new { error = $"Không tìm thấy kế hoạch kinh doanh {planCode}" }) : Results.Ok(res);
}).RequireAuthorization();

app.MapPost("/api/business-plans", async (CreateBusinessPlanDto dto, IBusinessPlanService svc) =>
{
    try
    {
        var res = await svc.CreateAsync(dto);
        return Results.Ok(res);
    }
    catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
}).RequireAuthorization();

app.MapPut("/api/business-plans/{planCode}", async (string planCode, UpdateBusinessPlanDto dto, IBusinessPlanService svc) =>
{
    try
    {
        var res = await svc.UpdateAsync(planCode, dto);
        return res is null ? Results.NotFound(new { error = $"Không tìm thấy kế hoạch kinh doanh {planCode}" }) : Results.Ok(res);
    }
    catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
}).RequireAuthorization();

app.MapPost("/api/business-plans/{planCode}/approve1", async (string planCode, Approve1BusinessPlanDto? dto, IBusinessPlanService svc) =>
{
    try
    {
        var res = await svc.Approve1Async(planCode, dto);
        return res is null ? Results.NotFound(new { error = $"Không tìm thấy kế hoạch kinh doanh {planCode}" }) : Results.Ok(res);
    }
    catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
}).RequireAuthorization();

app.MapPost("/api/business-plans/{planCode}/approve2", async (string planCode, Approve2BusinessPlanDto? dto, IBusinessPlanService svc) =>
{
    try
    {
        var res = await svc.Approve2Async(planCode, dto);
        return res is null ? Results.NotFound(new { error = $"Không tìm thấy kế hoạch kinh doanh {planCode}" }) : Results.Ok(res);
    }
    catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
}).RequireAuthorization();

app.MapPost("/api/business-plans/{planCode}/unapprove", async (string planCode, UnApproveBusinessPlanDto? dto, IBusinessPlanService svc) =>
{
    try
    {
        var res = await svc.UnApproveAsync(planCode, dto);
        return res is null ? Results.NotFound(new { error = $"Không tìm thấy kế hoạch kinh doanh {planCode}" }) : Results.Ok(res);
    }
    catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
}).RequireAuthorization();

app.MapPost("/api/business-plans/{planCode}/cancel", async (string planCode, CancelBusinessPlanDto dto, IBusinessPlanService svc) =>
{
    try
    {
        var res = await svc.CancelAsync(planCode, dto);
        return res is null ? Results.NotFound(new { error = $"Không tìm thấy kế hoạch kinh doanh {planCode}" }) : Results.Ok(res);
    }
    catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
}).RequireAuthorization();

app.MapDelete("/api/business-plans/{planCode}", async (string planCode, IBusinessPlanService svc) =>
{
    try
    {
        var deleted = await svc.DeleteDraftAsync(planCode);
        return deleted ? Results.Ok(new { success = true, planCode }) : Results.NotFound(new { error = $"Không tìm thấy kế hoạch kinh doanh {planCode}" });
    }
    catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
}).RequireAuthorization();

// ===== Bảng tính Chi phí Tài chính (CPTC) & Chiết khấu Thanh toán (CKTT) Mua buôn Xe Ô tô Đại lý - NPP (DMS.Sales DMS40_FnExp_Calc_FnExp_PmDc + DMS40_FnExp_Calc_FnExp_PmDcDtl / CalcFnExpPmDcController / DMS40.0.40.FnExp_PmtDisc.cs / CalcFnExpPmDc.txt) =====
app.MapGet("/api/fnexp-calc/seq", async (string? dealerCode, ICalcFnExpPmDcService svc) =>
    Results.Ok(await svc.GetSeqAsync(dealerCode))).RequireAuthorization();

app.MapGet("/api/fnexp-calc/eligible-cars", async (string? dealerCode, DateTime? termFrom, DateTime? termTo, ICalcFnExpPmDcService svc) =>
    Results.Ok(await svc.GetEligibleCarsAsync(dealerCode, termFrom, termTo))).RequireAuthorization();

app.MapPost("/api/fnexp-calc/preview", async (PreviewFnExpCalcSheetDto dto, ICalcFnExpPmDcService svc) =>
{
    try { return Results.Ok(await svc.CalculatePreviewAsync(dto)); }
    catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
}).RequireAuthorization();

app.MapGet("/api/fnexp-calc", async (
    ICalcFnExpPmDcService svc,
    string? dealerCode,
    string? caNo,
    FnExpStatus? status,
    FnExpSignStatus? dlrSignStatus,
    FnExpSignStatus? htcSignStatus,
    DateTime? termFrom,
    DateTime? termTo,
    int page = 0,
    int pageSize = 10) =>
    Results.Ok(await svc.ListAsync(dealerCode, caNo, status, dlrSignStatus, htcSignStatus, termFrom, termTo, page, pageSize))).RequireAuthorization();

app.MapGet("/api/fnexp-calc/stats", async (string? dealerCode, ICalcFnExpPmDcService svc) =>
    Results.Ok(await svc.StatsAsync(dealerCode))).RequireAuthorization();

app.MapGet("/api/fnexp-calc/{caNo}", async (string caNo, ICalcFnExpPmDcService svc) =>
{
    var r = await svc.DetailAsync(caNo);
    return r is null ? Results.NotFound(new { error = $"Không tìm thấy bảng tính CPTC & CKTT {caNo}" }) : Results.Ok(r);
}).RequireAuthorization();

app.MapPost("/api/fnexp-calc", async (CreateFnExpCalcSheetDto dto, ICalcFnExpPmDcService svc) =>
{
    try { return Results.Ok(await svc.CreateAsync(dto)); }
    catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
}).RequireAuthorization();

app.MapPost("/api/fnexp-calc/{caNo}/confirm-dlr1", async (string caNo, ConfirmStep1DealerDto? dto, ICalcFnExpPmDcService svc) =>
{
    try
    {
        var r = await svc.ConfirmStep1DealerAsync(caNo, dto);
        return r is null ? Results.NotFound(new { error = $"Không tìm thấy bảng tính {caNo}" }) : Results.Ok(r);
    }
    catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
}).RequireAuthorization();

app.MapPost("/api/fnexp-calc/{caNo}/approve-hq1", async (string caNo, ApproveStep1HqDto? dto, ICalcFnExpPmDcService svc) =>
{
    try
    {
        var r = await svc.ApproveStep1HqAsync(caNo, dto);
        return r is null ? Results.NotFound(new { error = $"Không tìm thấy bảng tính {caNo}" }) : Results.Ok(r);
    }
    catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
}).RequireAuthorization();

app.MapPost("/api/fnexp-calc/{caNo}/approve-hq2", async (string caNo, ApproveStep2HqDto? dto, ICalcFnExpPmDcService svc) =>
{
    try
    {
        var r = await svc.ApproveStep2HqAsync(caNo, dto);
        return r is null ? Results.NotFound(new { error = $"Không tìm thấy bảng tính {caNo}" }) : Results.Ok(r);
    }
    catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
}).RequireAuthorization();

app.MapPost("/api/fnexp-calc/{caNo}/approve-dlr2", async (string caNo, ApproveStep2DealerDto? dto, ICalcFnExpPmDcService svc) =>
{
    try
    {
        var r = await svc.ApproveStep2DealerAsync(caNo, dto);
        return r is null ? Results.NotFound(new { error = $"Không tìm thấy bảng tính {caNo}" }) : Results.Ok(r);
    }
    catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
}).RequireAuthorization();

app.MapPost("/api/fnexp-calc/{caNo}/cancel", async (string caNo, CancelFnExpCalcSheetDto dto, ICalcFnExpPmDcService svc) =>
{
    try
    {
        var r = await svc.CancelAsync(caNo, dto);
        return r is null ? Results.NotFound(new { error = $"Không tìm thấy bảng tính {caNo}" }) : Results.Ok(r);
    }
    catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
}).RequireAuthorization();

app.MapDelete("/api/fnexp-calc/{caNo}", async (string caNo, ICalcFnExpPmDcService svc) =>
{
    try
    {
        var deleted = await svc.DeleteDraftAsync(caNo);
        return deleted ? Results.Ok(new { success = true, caNo }) : Results.NotFound(new { error = $"Không tìm thấy bảng tính {caNo}" });
    }
    catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
}).RequireAuthorization();

app.MapGet("/api/fnexp-calc/{caNo}/print-cptc", async (string caNo, ICalcFnExpPmDcService svc) =>
{
    var r = await svc.PrintCptcAsync(caNo);
    return r is null ? Results.NotFound(new { error = $"Không tìm thấy bảng tính {caNo}" }) : Results.Ok(r);
}).RequireAuthorization();

app.MapGet("/api/fnexp-calc/{caNo}/print-cktt", async (string caNo, ICalcFnExpPmDcService svc) =>
{
    var r = await svc.PrintCkttAsync(caNo);
    return r is null ? Results.NotFound(new { error = $"Không tìm thấy bảng tính {caNo}" }) : Results.Ok(r);
}).RequireAuthorization();

// ===== Quản lý Chính sách Bán hàng & Hỗ trợ Quyết toán Bán lẻ Xe Ô tô Đại lý - NPP (Sales Policy & Support Retail - SPL_SalesPolicyMst + SPL_SalesPolicyMstDetail + SPL_SPSupportRetail · SPLSalesPolicyMstController / SPLSPSupportRetailController / SalesPolicy.cs / SPLSalesPolicyMst.txt / SPLSPSupportRetail.txt) =====

// --- Sales Policy Master ---
app.MapGet("/api/sales-policies/seq", async (ISalesPolicyService svc) =>
    Results.Ok(new { success = true, spsrCode = await svc.GetNextPolicySeqAsync() })).RequireAuthorization();

app.MapGet("/api/sales-policies", async (
    ISalesPolicyService svc,
    string? spsrCode,
    string? spNo,
    DateTime? startDateFrom,
    DateTime? startDateTo,
    SalesPolicyType? spsrType,
    BusinessSupportForm? supportForm,
    string? modelCode,
    string? dealerCode,
    SalesPolicyStatus? status,
    int page = 0,
    int pageSize = 10) =>
    Results.Ok(await svc.ListPoliciesAsync(spsrCode, spNo, startDateFrom, startDateTo, spsrType, supportForm, modelCode, dealerCode, status, page, pageSize))).RequireAuthorization();

app.MapGet("/api/sales-policies/{spsrCode}", async (string spsrCode, ISalesPolicyService svc) =>
{
    var r = await svc.GetPolicyByCodeAsync(spsrCode);
    return r is null ? Results.NotFound(new { error = $"Không tìm thấy chính sách {spsrCode}" }) : Results.Ok(r);
}).RequireAuthorization();

app.MapPost("/api/sales-policies", async (CreateSalesPolicyDto dto, ISalesPolicyService svc) =>
{
    try { return Results.Ok(await svc.CreatePolicyAsync(dto)); }
    catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
}).RequireAuthorization();

app.MapPut("/api/sales-policies/{spsrCode}", async (string spsrCode, UpdateSalesPolicyDto dto, ISalesPolicyService svc) =>
{
    try
    {
        var r = await svc.UpdatePolicyAsync(spsrCode, dto);
        return r is null ? Results.NotFound(new { error = $"Không tìm thấy chính sách {spsrCode}" }) : Results.Ok(r);
    }
    catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
}).RequireAuthorization();

app.MapPatch("/api/sales-policies/{spsrCode}/status", async (string spsrCode, UpdatePolicyStatusDto dto, ISalesPolicyService svc) =>
{
    var r = await svc.UpdatePolicyStatusAsync(spsrCode, dto);
    return r is null ? Results.NotFound(new { error = $"Không tìm thấy chính sách {spsrCode}" }) : Results.Ok(r);
}).RequireAuthorization();

app.MapDelete("/api/sales-policies/{spsrCode}", async (string spsrCode, ISalesPolicyService svc) =>
{
    try
    {
        var ok = await svc.DeletePolicyAsync(spsrCode);
        return ok ? Results.Ok(new { success = true, message = $"Đã xóa chính sách {spsrCode}" }) : Results.NotFound(new { error = $"Không tìm thấy chính sách {spsrCode}" });
    }
    catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
}).RequireAuthorization();

app.MapGet("/api/sales-policies/{spsrCode}/eligible-cars", async (string spsrCode, string? dealerCode, ISalesPolicyService svc) =>
{
    try { return Results.Ok(await svc.GetEligibleCarsForPolicyAsync(spsrCode, dealerCode)); }
    catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
}).RequireAuthorization();

// --- Support Retail ---
app.MapGet("/api/support-retails/seq", async (ISalesPolicyService svc) =>
    Results.Ok(new { success = true, supportCode = await svc.GetNextSupportSeqAsync() })).RequireAuthorization();

app.MapGet("/api/support-retails", async (
    ISalesPolicyService svc,
    string? spsrCode,
    string? spNo,
    string? dealerCode,
    string? vin,
    DateTime? dateSupport,
    DateTime? createdDateFrom,
    DateTime? createdDateTo,
    SupportRetailStatus? status,
    int page = 0,
    int pageSize = 10) =>
    Results.Ok(await svc.ListSupportRetailsAsync(spsrCode, spNo, dealerCode, vin, dateSupport, createdDateFrom, createdDateTo, status, page, pageSize))).RequireAuthorization();

app.MapGet("/api/support-retails/stats", async (string? dealerCode, ISalesPolicyService svc) =>
    Results.Ok(await svc.StatsAsync(dealerCode))).RequireAuthorization();

app.MapGet("/api/support-retails/{supportCode}", async (string supportCode, ISalesPolicyService svc) =>
{
    var r = await svc.DetailSupportRetailAsync(supportCode);
    return r is null ? Results.NotFound(new { error = $"Không tìm thấy hồ sơ hỗ trợ {supportCode}" }) : Results.Ok(r);
}).RequireAuthorization();

app.MapGet("/api/support-retails/by-vin", async (string spsrCode, string vin, ISalesPolicyService svc) =>
{
    var r = await svc.DetailSupportByVinAsync(spsrCode, vin);
    return r is null ? Results.NotFound(new { error = $"Không tìm thấy hồ sơ hỗ trợ cho VIN {vin} theo chính sách {spsrCode}" }) : Results.Ok(r);
}).RequireAuthorization();

app.MapPost("/api/support-retails/calc-date-full-status", async (CalcDateFullStatusRequestDto dto, ISalesPolicyService svc) =>
    Results.Ok(await svc.CalcDateFullStatusAsync(dto))).RequireAuthorization();

app.MapPost("/api/support-retails", async (CreateSupportRetailDto dto, ISalesPolicyService svc) =>
{
    try { return Results.Ok(await svc.CreateSupportRetailAsync(dto)); }
    catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
}).RequireAuthorization();

app.MapPost("/api/support-retails/{supportCode}/approve", async (string supportCode, ApproveSupportRetailDto? dto, ISalesPolicyService svc) =>
{
    try
    {
        var r = await svc.ApproveSupportRetailAsync(supportCode, dto);
        return r is null ? Results.NotFound(new { error = $"Không tìm thấy hồ sơ hỗ trợ {supportCode}" }) : Results.Ok(r);
    }
    catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
}).RequireAuthorization();

app.MapPost("/api/support-retails/batch-approve", async (BatchApproveSupportRetailDto dto, ISalesPolicyService svc) =>
{
    try { return Results.Ok(await svc.BatchApproveSupportRetailAsync(dto)); }
    catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
}).RequireAuthorization();

app.MapPost("/api/support-retails/{supportCode}/pay", async (string supportCode, PaySupportRetailDto? dto, ISalesPolicyService svc) =>
{
    try
    {
        var r = await svc.PaySupportRetailAsync(supportCode, dto);
        return r is null ? Results.NotFound(new { error = $"Không tìm thấy hồ sơ hỗ trợ {supportCode}" }) : Results.Ok(r);
    }
    catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
}).RequireAuthorization();

app.MapPost("/api/support-retails/{supportCode}/reject", async (string supportCode, RejectSupportRetailDto dto, ISalesPolicyService svc) =>
{
    try
    {
        var r = await svc.RejectSupportRetailAsync(supportCode, dto);
        return r is null ? Results.NotFound(new { error = $"Không tìm thấy hồ sơ hỗ trợ {supportCode}" }) : Results.Ok(r);
    }
    catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
}).RequireAuthorization();

app.MapPost("/api/support-retails/{supportCode}/cancel", async (string supportCode, CancelSupportRetailDto dto, ISalesPolicyService svc) =>
{
    try
    {
        var r = await svc.CancelSupportRetailAsync(supportCode, dto);
        return r is null ? Results.NotFound(new { error = $"Không tìm thấy hồ sơ hỗ trợ {supportCode}" }) : Results.Ok(r);
    }
    catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
}).RequireAuthorization();

app.MapDelete("/api/support-retails/{supportCode}", async (string supportCode, ISalesPolicyService svc) =>
{
    try
    {
        var ok = await svc.DeleteSupportRetailDraftAsync(supportCode);
        return ok ? Results.Ok(new { success = true, message = $"Đã xóa hồ sơ hỗ trợ {supportCode}" }) : Results.NotFound(new { error = $"Không tìm thấy hồ sơ hỗ trợ {supportCode}" });
    }
    catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
}).RequireAuthorization();

// PlanEstimateOrder - Quản lý Kế hoạch Dự kiến Đặt hàng Xe Ô tô Đại lý - NPP (Plan_EstimateOrder / PlanEstimateOrderController)
app.MapGet("/api/plan-estimate-orders/seq", async (IPlanEstimateOrderService svc) =>
    Results.Ok(new { success = true, pleOrdNo = await svc.GetNextPLEOrdNoSeqAsync() })).RequireAuthorization();

app.MapGet("/api/plan-estimate-orders", async (
    IPlanEstimateOrderService svc,
    string? dealerCode,
    string? areaRootCode,
    string? monthEstimate,
    PlanEstimateOrderStatus? status,
    DateTime? fromDate,
    DateTime? toDate,
    int pageIndex = 0,
    int pageSize = 50) =>
    Results.Ok(await svc.SearchAsync(dealerCode, areaRootCode, monthEstimate, status, fromDate, toDate, pageIndex, pageSize))).RequireAuthorization();

app.MapGet("/api/plan-estimate-orders/stats", async (IPlanEstimateOrderService svc) =>
    Results.Ok(await svc.GetStatsAsync())).RequireAuthorization();

app.MapGet("/api/plan-estimate-orders/{pleOrdNo}", async (string pleOrdNo, IPlanEstimateOrderService svc) =>
{
    var item = await svc.GetByPLEOrdNoAsync(pleOrdNo);
    return item is null ? Results.NotFound(new { error = $"Không tìm thấy kế hoạch dự kiến đặt hàng {pleOrdNo}" }) : Results.Ok(item);
}).RequireAuthorization();

app.MapPost("/api/plan-estimate-orders/calculate", async (CalculateEstimateInputDto dto, IPlanEstimateOrderService svc) =>
{
    try { return Results.Ok(await svc.CalculateEstimateAsync(dto)); }
    catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
}).RequireAuthorization();

app.MapPost("/api/plan-estimate-orders", async (CreatePlanEstimateOrderDto dto, IPlanEstimateOrderService svc) =>
{
    try { return Results.Ok(await svc.CreateAsync(dto)); }
    catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
}).RequireAuthorization();

app.MapPut("/api/plan-estimate-orders/{pleOrdNo}", async (string pleOrdNo, UpdatePlanEstimateOrderDto dto, IPlanEstimateOrderService svc) =>
{
    try
    {
        var item = await svc.UpdateAsync(pleOrdNo, dto);
        return item is null ? Results.NotFound(new { error = $"Không tìm thấy kế hoạch dự kiến đặt hàng {pleOrdNo}" }) : Results.Ok(item);
    }
    catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
}).RequireAuthorization();

app.MapPost("/api/plan-estimate-orders/{pleOrdNo}/approve1", async (string pleOrdNo, string? approvedBy, IPlanEstimateOrderService svc) =>
{
    try
    {
        var item = await svc.Approve1Async(pleOrdNo, approvedBy);
        return item is null ? Results.NotFound(new { error = $"Không tìm thấy kế hoạch dự kiến đặt hàng {pleOrdNo}" }) : Results.Ok(item);
    }
    catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
}).RequireAuthorization();

app.MapPost("/api/plan-estimate-orders/{pleOrdNo}/approve2", async (string pleOrdNo, string? approvedBy, IPlanEstimateOrderService svc) =>
{
    try
    {
        var item = await svc.Approve2Async(pleOrdNo, approvedBy);
        return item is null ? Results.NotFound(new { error = $"Không tìm thấy kế hoạch dự kiến đặt hàng {pleOrdNo}" }) : Results.Ok(item);
    }
    catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
}).RequireAuthorization();

app.MapPost("/api/plan-estimate-orders/{pleOrdNo}/cancel", async (string pleOrdNo, CancelPlanEstimateOrderDto dto, IPlanEstimateOrderService svc) =>
{
    try
    {
        var item = await svc.CancelAsync(pleOrdNo, dto);
        return item is null ? Results.NotFound(new { error = $"Không tìm thấy kế hoạch dự kiến đặt hàng {pleOrdNo}" }) : Results.Ok(item);
    }
    catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
}).RequireAuthorization();

app.MapDelete("/api/plan-estimate-orders/{pleOrdNo}", async (string pleOrdNo, IPlanEstimateOrderService svc) =>
{
    try
    {
        var ok = await svc.DeleteDraftAsync(pleOrdNo);
        return ok ? Results.Ok(new { success = true, message = $"Đã xóa bản nháp kế hoạch dự kiến đặt hàng {pleOrdNo}" }) : Results.NotFound(new { error = $"Không tìm thấy kế hoạch {pleOrdNo}" });
    }
    catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
}).RequireAuthorization();

// Import hàng loạt hợp đồng thật (SQL nguồn DLS_Deal+Dls_DealDetail+DLS_DealerCustomer+Car_Car, 2010.HTC).
// Dedupe theo Code (DealNo). Status luôn Draft/Paid=0 — nguồn DeliveryStatus không đủ rõ nghĩa để map an toàn
// sang state machine (Deposited/Paid/Delivered) nên KHÔNG suy diễn, để user tự vận hành qua API deposit/pay/deliver.
app.MapPost("/api/import/orders", async (List<ImportOrderRowDto> rows, AppDbContext db, ITenantContext tenant) =>
{
    if (rows is null || rows.Count == 0) return Results.BadRequest(new { error = "Không có dữ liệu import." });
    int added = 0, skipped = 0;
    foreach (var r in rows)
    {
        if (string.IsNullOrWhiteSpace(r.DealNo) || string.IsNullOrWhiteSpace(r.CustomerName) || string.IsNullOrWhiteSpace(r.Model)) { skipped++; continue; }
        if (await db.Orders.AnyAsync(o => o.OrgId == tenant.OrgId && o.Code == r.DealNo)) { skipped++; continue; }
        db.Orders.Add(new SalesOrder
        {
            OrgId = tenant.OrgId, Code = r.DealNo.Trim(), CustomerName = r.CustomerName.Trim(), Phone = r.Phone ?? "",
            Vin = r.Vin, Model = r.Model.Trim(), DealerCode = r.DealerCode ?? "", ListPrice = r.ListPrice,
            Discount = 0, NetPrice = r.ListPrice, Paid = 0, Status = SalesStatus.Draft
        });
        added++;
    }
    await db.SaveChangesAsync();
    return Results.Ok(new { added, skipped, total = rows.Count });
}).RequireAuthorization();

app.MapPost("/api/orgs/register", async (RegisterOrgDto dto, AppDbContext db) =>
{
    if (string.IsNullOrWhiteSpace(dto.Name)) return Results.BadRequest(new { error = "Cần Name." });
    var org = new Org { Name = dto.Name.Trim(), ApiKey = "sls_" + Guid.NewGuid().ToString("N") };
    db.Orgs.Add(org); await db.SaveChangesAsync();
    return Results.Ok(new { orgId = org.Id, apiKey = org.ApiKey });
});

app.Run();

record RegisterOrgDto(string Name);
record ImportOrderRowDto(string? DealNo, string? DealerCode, string? SalesType, string? CustomerName, string? Phone, string? Vin, string? Model, decimal ListPrice, string? DeliveryStatus);
