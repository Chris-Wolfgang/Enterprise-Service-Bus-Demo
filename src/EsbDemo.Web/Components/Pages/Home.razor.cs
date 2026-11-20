using Microsoft.AspNetCore.Components;

namespace EsbDemo.Web.Components.Pages;

/// <summary>
/// The home page: the message monitor and one tab per simulated system.
/// </summary>
public sealed partial class Home
{
    /// <summary>Gets or sets the logger that receives monitor refresh faults.</summary>
    [Inject]
    private ILogger<Home> Logger { get; set; } = default!;



    [LoggerMessage(EventId = 1, Level = LogLevel.Error, Message = "Refreshing the message monitor failed")]
    private static partial void LogRefreshFault(ILogger logger, Exception exception);
}
