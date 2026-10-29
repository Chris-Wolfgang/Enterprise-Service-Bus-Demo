namespace EsbDemo.Core.Config;

/// <summary>
/// A message topic on the bus, with its display settings and payload fields,
/// as configured in <c>config/topics.json</c>.
/// </summary>
public sealed class TopicConfig
{
    /// <summary>
    /// Gets or sets the topic name, such as <c>account.created</c>.
    /// </summary>
    public string Topic { get; set; } = string.Empty;



    /// <summary>
    /// Gets or sets the human-readable label shown for the topic in the UI.
    /// </summary>
    public string Label { get; set; } = string.Empty;



    /// <summary>
    /// Gets or sets the colour family used to style the topic, such as <c>blue</c>.
    /// </summary>
    public string ColorFamily { get; set; } = string.Empty;



    /// <summary>
    /// Gets or sets the optional shade within <see cref="ColorFamily"/>, such as <c>light</c> or <c>dark</c>.
    /// </summary>
    public string ColorShade { get; set; } = string.Empty;



    /// <summary>
    /// Gets or sets the fields that make up the topic's message payload.
    /// </summary>
    public List<FieldConfig> Fields { get; set; } = new List<FieldConfig>();
}
