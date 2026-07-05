namespace EsbDemo.Core.Config;

public sealed class AppConfig
{
    public string Name { get; set; } = string.Empty;


    public int DisplayOrder { get; set; }


    public bool DefaultOnline { get; set; } = true;


    public List<string> Publishes { get; set; } = new List<string>();


    public List<string> Subscribes { get; set; } = new List<string>();


    public List<AutoResponse> AutoResponses { get; set; } = new List<AutoResponse>();
}
