using System.Diagnostics;
using Bunit;
using EsbDemo.Web.Components.Pages;
using Microsoft.AspNetCore.Http;

namespace EsbDemo.Web.Tests.Unit.Components;

/// <summary>
/// Error-page tests. They set the ambient <see cref="Activity.Current"/>, so they run in a
/// collection that never runs in parallel with other tests.
/// </summary>
[Collection(nameof(AmbientActivityCollection))]
public sealed class ErrorPageTests : ComponentTestBase
{
    [Fact]
    public void ErrorPage_when_there_is_no_request_id_hides_it()
    {
        Activity.Current = null;

        var cut = Render<Error>();

        Assert.Empty(cut.FindAll("code"));
        Assert.Contains("An error occurred", cut.Markup, StringComparison.Ordinal);
    }



    [Fact]
    public void ErrorPage_when_an_activity_or_a_trace_id_exists_shows_it()
    {
        Activity.Current = null;
        var fromContext = Render<Error>(p => p.AddCascadingValue<HttpContext>(new DefaultHttpContext { TraceIdentifier = "trace-42" }));
        Assert.Equal("trace-42", fromContext.Find("code").TextContent);

        using var activity = new Activity("test").Start();
        var fromActivity = Render<Error>();
        Assert.Equal(activity.Id, fromActivity.Find("code").TextContent);
    }
}



/// <summary>Serializes the tests that change <see cref="Activity.Current"/>.</summary>
[CollectionDefinition(nameof(AmbientActivityCollection), DisableParallelization = true)]
public sealed class AmbientActivityCollection
{
}
