namespace EsbDemo.Web.Models;

/// <summary>
/// The message a user composed in the action dialog, ready to publish.
/// </summary>
/// <param name="Topic">The topic to publish.</param>
/// <param name="SourceApp">The name of the app the message is published from.</param>
/// <param name="Fields">The payload field values, keyed by field name.</param>
public sealed record ActionDialogResult(
    string Topic,
    string SourceApp,
    Dictionary<string, string> Fields);
