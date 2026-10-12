using EsbDemo.Web.Components;
using EsbDemo.Web.Connectors;
using EsbDemo.Web.Data.Crm;
using EsbDemo.Web.Data.CustomerPortal;
using EsbDemo.Web.Data.DataWarehouse;
using EsbDemo.Web.Data.ECommerce;
using EsbDemo.Web.Data.Erp;
using EsbDemo.Web.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddSingleton<ConfigService>();
builder.Services.AddSingleton<MonitorService>();
builder.Services.AddSingleton<CrmConnector>();
builder.Services.AddSingleton<ErpConnector>();
builder.Services.AddSingleton<DataWarehouseConnector>();
builder.Services.AddSingleton<ECommerceConnector>();
builder.Services.AddSingleton<CustomerPortalConnector>();

builder.Services.AddDbContext<CrmDbContext>(o => o.UseInMemoryDatabase("CRM"));
builder.Services.AddDbContext<ErpDbContext>(o => o.UseInMemoryDatabase("ERP"));
builder.Services.AddDbContext<ECommerceDbContext>(o => o.UseInMemoryDatabase("ECommerce"));
builder.Services.AddDbContext<CustomerPortalDbContext>(o => o.UseInMemoryDatabase("CustomerPortal"));
builder.Services.AddDbContext<DataWarehouseDbContext>(o => o.UseInMemoryDatabase("DataWarehouse"));

var app = builder.Build();

var configService = app.Services.GetRequiredService<ConfigService>();
await configService.LoadAsync().ConfigureAwait(false);

var scope = app.Services.CreateAsyncScope();
await using (scope.ConfigureAwait(false))
{
    await scope.ServiceProvider.GetRequiredService<CrmDbContext>().Database.EnsureCreatedAsync().ConfigureAwait(false);
    await scope.ServiceProvider.GetRequiredService<ErpDbContext>().Database.EnsureCreatedAsync().ConfigureAwait(false);
    await scope.ServiceProvider.GetRequiredService<ECommerceDbContext>().Database.EnsureCreatedAsync().ConfigureAwait(false);
    await scope.ServiceProvider.GetRequiredService<CustomerPortalDbContext>().Database.EnsureCreatedAsync().ConfigureAwait(false);
    await scope.ServiceProvider.GetRequiredService<DataWarehouseDbContext>().Database.EnsureCreatedAsync().ConfigureAwait(false);
}

_ = app.Services.GetRequiredService<MonitorService>();
// Eagerly resolve connectors so they register their handlers with MonitorService at startup
_ = app.Services.GetRequiredService<CrmConnector>();
_ = app.Services.GetRequiredService<ErpConnector>();
_ = app.Services.GetRequiredService<DataWarehouseConnector>();
_ = app.Services.GetRequiredService<ECommerceConnector>();
_ = app.Services.GetRequiredService<CustomerPortalConnector>();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();
app.UseAntiforgery();
app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

await app.RunAsync().ConfigureAwait(false);
