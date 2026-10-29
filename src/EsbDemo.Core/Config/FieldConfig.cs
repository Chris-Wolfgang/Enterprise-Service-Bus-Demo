namespace EsbDemo.Core.Config;

/// <summary>
/// One field of a topic's message payload, as configured in <c>config/topics.json</c>.
/// </summary>
public sealed class FieldConfig
{
    /// <summary>
    /// Gets or sets the field name, used as the payload key and the input label.
    /// </summary>
    public string Name { get; set; } = string.Empty;



    /// <summary>
    /// Gets or sets the field's input type. <c>text</c> (the default) is a free-text input;
    /// <c>account</c> is a picker over existing customer accounts.
    /// </summary>
    public string Type { get; set; } = "text";



    /// <summary>
    /// Gets or sets the field's default value, which may contain placeholders that
    /// <see cref="Services.PlaceholderResolver.Resolve"/> expands.
    /// </summary>
    public string Default { get; set; } = string.Empty;
}
