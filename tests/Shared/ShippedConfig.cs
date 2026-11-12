using System.Diagnostics.CodeAnalysis;
using EsbDemo.Web.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.FileProviders;

namespace EsbDemo.Tests.Shared;

// Compiled into both EsbDemo.Web.Tests.Unit and EsbDemo.Tests.Fuzz (see the <Compile Include> link in
// each csproj), so the two suites share one host-environment fake and one config loader.

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



internal static class ShippedConfig
{
    /// <summary>Loads the app's real config/apps.json and config/topics.json, copied to the test output.</summary>
    public static async Task<ConfigService> LoadAsync()
    {
        var config = new ConfigService(new FakeWebHostEnvironment(AppContext.BaseDirectory));
        await config.LoadAsync();
        return config;
    }
}
