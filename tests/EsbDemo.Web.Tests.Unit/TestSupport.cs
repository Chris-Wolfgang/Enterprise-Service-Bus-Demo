using System.Diagnostics.CodeAnalysis;
using EsbDemo.Web.Connectors;
using EsbDemo.Web.Data.Crm;
using EsbDemo.Web.Data.CustomerPortal;
using EsbDemo.Web.Data.DataWarehouse;
using EsbDemo.Web.Data.ECommerce;
using EsbDemo.Web.Data.Erp;
using EsbDemo.Web.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Time.Testing;

namespace EsbDemo.Web.Tests.Unit;

/// <summary>A host environment whose content root is a chosen folder.</summary>
[ExcludeFromCodeCoverage]
internal sealed class FakeWebHostEnvironment : IWebHostEnvironment
{
    public FakeWebHostEnvironment(string contentRootPath)
    {
        ContentRootPath = contentRootPath;
    }



    public string ContentRootPath { get; set; }



    public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();



    public string ApplicationName { get; set; } = "EsbDemo.Web";



    public string EnvironmentName { get; set; } = "Development";



    public string WebRootPath { get; set; } = string.Empty;



    public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
}



internal static class TestConfig
{
    /// <summary>Loads the app's real config/apps.json and config/topics.json, copied to the test output.</summary>
    public static async Task<ConfigService> LoadRealAsync()
    {
        var config = new ConfigService(new FakeWebHostEnvironment(AppContext.BaseDirectory));
        await config.LoadAsync();
        return config;
    }



    /// <summary>Writes the given JSON as config/apps.json and config/topics.json in a new folder and loads it.</summary>
    public static async Task<ConfigService> LoadAsync(string appsJson, string topicsJson)
    {
        var root = Path.Combine(Path.GetTempPath(), "esb-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(root, "config"));
        await File.WriteAllTextAsync(Path.Combine(root, "config", "apps.json"), appsJson);
        await File.WriteAllTextAsync(Path.Combine(root, "config", "topics.json"), topicsJson);

        var config = new ConfigService(new FakeWebHostEnvironment(root));
        await config.LoadAsync();

        Directory.Delete(root, recursive: true);
        return config;
    }



    /// <summary>Polls until <paramref name="condition"/> holds, failing after <paramref name="timeout"/> (five seconds by default).</summary>
    public static async Task WaitUntilAsync(Func<bool> condition, TimeSpan? timeout = null)
    {
        var deadline = DateTime.UtcNow + (timeout ?? TimeSpan.FromSeconds(5));
        while (!condition())
        {
            Assert.True(DateTime.UtcNow < deadline, "Timed out waiting for the condition.");
            await Task.Delay(10);
        }
    }
}



/// <summary>The bus, all five connectors and their in-memory databases, seeded.</summary>
internal sealed class ConnectorHarness : IDisposable
{
    private readonly ServiceProvider _services;



    private ConnectorHarness(ServiceProvider services, MonitorService monitor, FakeTimeProvider time)
    {
        _services = services;
        Monitor = monitor;
        Time = time;
        Crm = new CrmConnector(monitor, Factory<CrmDbContext>(), time);
        Erp = new ErpConnector(monitor, Factory<ErpDbContext>(), time);
        ECommerce = new ECommerceConnector(monitor, Factory<ECommerceDbContext>(), time);
        Portal = new CustomerPortalConnector(monitor, Factory<CustomerPortalDbContext>(), time);
        Warehouse = new DataWarehouseConnector(monitor, Factory<DataWarehouseDbContext>(), time);
    }



    public MonitorService Monitor { get; }



    public FakeTimeProvider Time { get; }



    public IDbContextFactory<TContext> Factory<TContext>()
        where TContext : DbContext =>
        _services.GetRequiredService<IDbContextFactory<TContext>>();



    public CrmConnector Crm { get; }



    public ErpConnector Erp { get; }



    public ECommerceConnector ECommerce { get; }



    public CustomerPortalConnector Portal { get; }



    public DataWarehouseConnector Warehouse { get; }



    public static async Task<ConnectorHarness> CreateAsync()
    {
        var suffix = Guid.NewGuid().ToString("N");
        var services = new ServiceCollection()
            .AddDbContextFactory<CrmDbContext>(o => o.UseInMemoryDatabase("CRM-" + suffix))
            .AddDbContextFactory<ErpDbContext>(o => o.UseInMemoryDatabase("ERP-" + suffix))
            .AddDbContextFactory<ECommerceDbContext>(o => o.UseInMemoryDatabase("ECommerce-" + suffix))
            .AddDbContextFactory<CustomerPortalDbContext>(o => o.UseInMemoryDatabase("Portal-" + suffix))
            .AddDbContextFactory<DataWarehouseDbContext>(o => o.UseInMemoryDatabase("DW-" + suffix))
            .BuildServiceProvider();

        using (var scope = services.CreateScope())
        {
            await scope.ServiceProvider.GetRequiredService<CrmDbContext>().Database.EnsureCreatedAsync();
            await scope.ServiceProvider.GetRequiredService<ErpDbContext>().Database.EnsureCreatedAsync();
            await scope.ServiceProvider.GetRequiredService<ECommerceDbContext>().Database.EnsureCreatedAsync();
            await scope.ServiceProvider.GetRequiredService<CustomerPortalDbContext>().Database.EnsureCreatedAsync();
            await scope.ServiceProvider.GetRequiredService<DataWarehouseDbContext>().Database.EnsureCreatedAsync();
        }

        var time = new FakeTimeProvider();
        var monitor = new MonitorService(await TestConfig.LoadRealAsync(), time);
        return new ConnectorHarness(services, monitor, time);
    }



    /// <summary>Runs <paramref name="query"/> against a fresh context of type <typeparamref name="TContext"/>.</summary>
    public T Query<TContext, T>(Func<TContext, T> query)
        where TContext : DbContext
    {
        using var scope = _services.CreateScope();
        return query(scope.ServiceProvider.GetRequiredService<TContext>());
    }



    public void Dispose() => _services.Dispose();
}
