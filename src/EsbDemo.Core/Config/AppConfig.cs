namespace EsbDemo.Core.Config;

/// <summary>
/// One simulated line-of-business system, as configured in <c>config/apps.json</c>.
/// </summary>
/// <remarks>
/// An app publishes the topics in <see cref="Publishes"/>, receives the topics in
/// <see cref="Subscribes"/>, and answers some received topics automatically through
/// <see cref="AutoResponses"/>.
/// </remarks>
public sealed class AppConfig
{
    /// <summary>
    /// Gets or sets the app's display name, which also identifies it on the bus and in the monitor.
    /// </summary>
    public string Name { get; set; } = string.Empty;


    /// <summary>
    /// Gets or sets the position of the app's panel in the UI; lower values are shown first.
    /// </summary>
    public int DisplayOrder { get; set; }


    /// <summary>
    /// Gets or sets a value indicating whether the app starts online. Defaults to <see langword="true"/>.
    /// </summary>
    public bool DefaultOnline { get; set; } = true;


    /// <summary>
    /// Gets or sets the topics this app can publish.
    /// </summary>
    public List<string> Publishes { get; set; } = new List<string>();


    /// <summary>
    /// Gets or sets the topics this app receives from the bus.
    /// </summary>
    public List<string> Subscribes { get; set; } = new List<string>();


    /// <summary>
    /// Gets or sets the rules that make this app publish a reply when it receives a given topic.
    /// </summary>
    public List<AutoResponse> AutoResponses { get; set; } = new List<AutoResponse>();
}
