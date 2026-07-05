namespace EsbDemo.Core.Config;

public sealed class TopicConfig
{
    public string Topic { get; set; } = string.Empty;


    public string Label { get; set; } = string.Empty;


    public string ColorFamily { get; set; } = string.Empty;


    public string ColorShade { get; set; } = string.Empty;


    public List<FieldConfig> Fields { get; set; } = new List<FieldConfig>();
}
