using AngleSharp.Dom;
using Bunit;
using EsbDemo.Web.Connectors;
using EsbDemo.Web.Data.Crm;
using EsbDemo.Web.Data.CustomerPortal;
using EsbDemo.Web.Data.DataWarehouse;
using EsbDemo.Web.Data.ECommerce;
using EsbDemo.Web.Data.Erp;
using EsbDemo.Web.Models;
using EsbDemo.Web.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Time.Testing;

namespace EsbDemo.Web.Tests.Unit.Components;

/// <summary>
/// Renders components against the same services Program.cs registers: the shipped config, the bus
/// (on a fake clock), all five seeded in-memory databases and the connectors.
/// </summary>
public abstract class ComponentTestBase : BunitContext, IAsyncLifetime
{
    protected FakeTimeProvider Time { get; } = new();



    protected ConfigService Config { get; private set; } = null!;



    protected MonitorService Monitor { get; private set; } = null!;



    public async Task InitializeAsync()
    {
        Config = await ShippedConfig.LoadAsync();
        Monitor = new MonitorService(Config, Time);

        var suffix = Guid.NewGuid().ToString("N");
        Services.AddSingleton(Config);
        Services.AddSingleton(Monitor);
        Services.AddDbContext<CrmDbContext>(o => o.UseInMemoryDatabase("CRM-" + suffix));
        Services.AddDbContext<ErpDbContext>(o => o.UseInMemoryDatabase("ERP-" + suffix));
        Services.AddDbContext<ECommerceDbContext>(o => o.UseInMemoryDatabase("ECommerce-" + suffix));
        Services.AddDbContext<CustomerPortalDbContext>(o => o.UseInMemoryDatabase("Portal-" + suffix));
        Services.AddDbContext<DataWarehouseDbContext>(o => o.UseInMemoryDatabase("DW-" + suffix));
        Services.AddSingleton<CrmConnector>();
        Services.AddSingleton<ErpConnector>();
        Services.AddSingleton<ECommerceConnector>();
        Services.AddSingleton<CustomerPortalConnector>();
        Services.AddSingleton<DataWarehouseConnector>();
        JSInterop.Mode = JSRuntimeMode.Loose;

        using (var scope = Services.CreateScope())
        {
            await scope.ServiceProvider.GetRequiredService<CrmDbContext>().Database.EnsureCreatedAsync();
            await scope.ServiceProvider.GetRequiredService<ErpDbContext>().Database.EnsureCreatedAsync();
            await scope.ServiceProvider.GetRequiredService<ECommerceDbContext>().Database.EnsureCreatedAsync();
            await scope.ServiceProvider.GetRequiredService<CustomerPortalDbContext>().Database.EnsureCreatedAsync();
            await scope.ServiceProvider.GetRequiredService<DataWarehouseDbContext>().Database.EnsureCreatedAsync();
        }

        // As in Program.cs: create the connectors up front so they register their handlers.
        _ = Services.GetRequiredService<CrmConnector>();
        _ = Services.GetRequiredService<ErpConnector>();
        _ = Services.GetRequiredService<ECommerceConnector>();
        _ = Services.GetRequiredService<CustomerPortalConnector>();
        _ = Services.GetRequiredService<DataWarehouseConnector>();
    }



    Task IAsyncLifetime.DisposeAsync() => Task.CompletedTask;



    /// <summary>Runs <paramref name="query"/> against a fresh context, so it sees what was saved.</summary>
    protected T Query<TContext, T>(Func<TContext, T> query)
        where TContext : DbContext
    {
        ArgumentNullException.ThrowIfNull(query);
        using var scope = Services.CreateScope();
        return query(scope.ServiceProvider.GetRequiredService<TContext>());
    }



    /// <summary>The single element matching <paramref name="selector"/> whose trimmed text is <paramref name="text"/>.</summary>
    protected static IElement ByText<TComponent>(IRenderedComponent<TComponent> cut, string selector, string text)
        where TComponent : IComponent =>
        Assert.Single(cut.FindAll(selector), e => string.Equals(e.TextContent.Trim(), text, StringComparison.Ordinal));



    /// <summary>The bus messages <paramref name="app"/> sent.</summary>
    protected MonitorEntry[] SentBy(string app) =>
        Monitor.Entries
            .Where(e => string.Equals(e.AppName, app, StringComparison.Ordinal) && string.Equals(e.Direction, "→ SEND", StringComparison.Ordinal))
            .ToArray();



    protected static Dictionary<string, string> Fields(params (string Key, string Value)[] pairs) =>
        pairs.ToDictionary(p => p.Key, p => p.Value);
}
