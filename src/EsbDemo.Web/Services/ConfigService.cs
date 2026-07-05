using System.Text.Json;
using EsbDemo.Core.Config;

namespace EsbDemo.Web.Services;

public sealed class ConfigService
{
    private readonly IWebHostEnvironment _env;

    public IReadOnlyList<AppConfig> Apps { get; private set; } = Array.Empty<AppConfig>();


    public IReadOnlyList<TopicConfig> Topics { get; private set; } = Array.Empty<TopicConfig>();


    public ConfigService(IWebHostEnvironment env)
    {
        _env = env;
    }


    public async Task LoadAsync()
    {
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

        var appsPath = Path.Combine(_env.ContentRootPath, "config", "apps.json");
        await using var appsStream = File.OpenRead(appsPath);
        Apps = await JsonSerializer.DeserializeAsync<List<AppConfig>>(appsStream, options) ?? new List<AppConfig>();

        var topicsPath = Path.Combine(_env.ContentRootPath, "config", "topics.json");
        await using var topicsStream = File.OpenRead(topicsPath);
        Topics = await JsonSerializer.DeserializeAsync<List<TopicConfig>>(topicsStream, options) ?? new List<TopicConfig>();
    }


    public TopicConfig? GetTopic(string topic) =>
        Topics.FirstOrDefault(t => string.Equals(t.Topic, topic, StringComparison.Ordinal));


    public string GetTopicCssClass(string topic)
    {
        var config = GetTopic(topic);
        if (config is null) return "topic-default";
        return string.IsNullOrEmpty(config.ColorShade)
            ? $"topic-{config.ColorFamily}"
            : $"topic-{config.ColorFamily}-{config.ColorShade}";
    }


    public string GetTopicLabel(string topic)
    {
        var config = GetTopic(topic);
        return config?.Label ?? topic;
    }
}
