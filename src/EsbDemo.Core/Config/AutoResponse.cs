namespace EsbDemo.Core.Config;

/// <summary>
/// A rule that makes an app publish one topic automatically when it receives another.
/// </summary>
/// <example>
/// <code>
/// { "on": "account.create", "publish": "account.created" }
/// </code>
/// </example>
public sealed class AutoResponse
{
    /// <summary>
    /// Gets or sets the received topic that triggers the response.
    /// </summary>
    public string On { get; set; } = string.Empty;


    /// <summary>
    /// Gets or sets the topic the app publishes in response to <see cref="On"/>.
    /// </summary>
    public string Publish { get; set; } = string.Empty;
}
