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
