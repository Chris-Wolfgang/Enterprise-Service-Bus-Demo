using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;

namespace EsbDemo.Web.Tests.Unit;

/// <summary>Runs the real Program.cs startup and request pipeline in memory.</summary>
public sealed class ProgramTests
{
    [Fact]
    public async Task Host_in_development_serves_the_prerendered_home_page()
    {
        await using var factory = new WebApplicationFactory<Program>();
        using var client = factory.CreateClient();

        var response = await client.GetAsync(new Uri("/", UriKind.Relative));
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("<title>ESB Demo</title>", html, StringComparison.Ordinal);
        Assert.Contains("Create Customer", html, StringComparison.Ordinal);
        Assert.Contains("No messages yet", html, StringComparison.Ordinal);
    }



    [Fact]
    public async Task Host_when_the_route_is_unknown_renders_the_not_found_page()
    {
        await using var factory = new WebApplicationFactory<Program>();
        using var client = factory.CreateClient();

        var response = await client.GetAsync(new Uri("/no-such-page", UriKind.Relative));
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Contains("Sorry, the content you are looking for does not exist.", html, StringComparison.Ordinal);
    }



    [Fact]
    public async Task Host_in_production_serves_the_home_page_and_the_error_page()
    {
        await using var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(b => b.UseEnvironment("Production"));
        using var client = factory.CreateClient();

        var home = await client.GetAsync(new Uri("/", UriKind.Relative));
        var error = await client.GetAsync(new Uri("/Error", UriKind.Relative));

        Assert.Equal(HttpStatusCode.OK, home.StatusCode);
        Assert.Contains("An error occurred while processing your request.", await error.Content.ReadAsStringAsync(), StringComparison.Ordinal);
    }
}
