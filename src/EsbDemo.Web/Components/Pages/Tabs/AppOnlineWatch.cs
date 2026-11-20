using EsbDemo.Web.Services;

namespace EsbDemo.Web.Components.Pages.Tabs;

/// <summary>
/// Tracks whether one system is online for that system's tab, and asks the tab to re-render when
/// the state changes, so the tab stops saving and publishing while the app panel shows the system
/// as offline.
/// </summary>
internal sealed class AppOnlineWatch : IDisposable
{
    private readonly MonitorService _monitor;
    private readonly string _appName;
    private readonly Func<Task> _onStateChanged;
    private volatile bool _isOnline;



    /// <summary>
    /// Initializes a new instance of the <see cref="AppOnlineWatch"/> class and starts listening to
    /// <see cref="MonitorService.OnChanged"/>.
    /// </summary>
    /// <param name="monitor">The bus that owns the online state.</param>
    /// <param name="appName">The system the tab belongs to.</param>
    /// <param name="onStateChanged">
    /// Called after the system goes offline or comes back online; a tab passes
    /// <c>() =&gt; InvokeAsync(StateHasChanged)</c>.
    /// </param>
    internal AppOnlineWatch(MonitorService monitor, string appName, Func<Task> onStateChanged)
    {
        _monitor = monitor;
        _appName = appName;
        _onStateChanged = onStateChanged;
        _isOnline = monitor.IsOnline(appName);
        monitor.OnChanged += OnMonitorChanged;
    }



    /// <summary>Gets a value indicating whether the system is offline.</summary>
    internal bool IsOffline => !_isOnline;



    /// <summary>Stops listening to <see cref="MonitorService.OnChanged"/>.</summary>
    public void Dispose() => _monitor.OnChanged -= OnMonitorChanged;



    // OnChanged fires for every bus message; only a change of this system's state re-renders the tab.
    private void OnMonitorChanged(object? sender, EventArgs e)
    {
        var isOnline = _monitor.IsOnline(_appName);
        if (isOnline == _isOnline)
            return;

        _isOnline = isOnline;
        _ = _onStateChanged();
    }
}
