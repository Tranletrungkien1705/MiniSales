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
