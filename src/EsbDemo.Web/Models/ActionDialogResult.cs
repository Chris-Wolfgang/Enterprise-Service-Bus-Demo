namespace EsbDemo.Web.Models;

public sealed record ActionDialogResult(
    string Topic,
    string SourceApp,
    Dictionary<string, string> Fields);
