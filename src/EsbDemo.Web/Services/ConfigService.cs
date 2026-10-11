using System.Text.Json;
using EsbDemo.Core.Config;

namespace EsbDemo.Web.Services;

/// <summary>
/// Loads and serves the app and topic configuration from the <c>config</c> folder.
/// </summary>
public sealed class ConfigService
{
    private readonly IWebHostEnvironment _env;

    /// <summary>
    /// Gets the configured apps; empty until <see cref="LoadAsync"/> completes.
    /// </summary>
    public IReadOnlyList<AppConfig> Apps { get; private set; } = Array.Empty<AppConfig>();


    /// <summary>
    /// Gets the configured topics; empty until <see cref="LoadAsync"/> completes.
    /// </summary>
    public IReadOnlyList<TopicConfig> Topics { get; private set; } = Array.Empty<TopicConfig>();


    /// <summary>
    /// Initializes a new instance of the <see cref="ConfigService"/> class.
    /// </summary>
    /// <param name="env">The host environment whose content root holds the <c>config</c> folder.</param>
    public ConfigService(IWebHostEnvironment env)
    {
        _env = env;
    }


    /// <summary>
    /// Reads <c>config/apps.json</c> and <c>config/topics.json</c> into <see cref="Apps"/> and <see cref="Topics"/>.
    /// </summary>
    /// <returns>A task that completes when both files have been read.</returns>
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


    /// <summary>
    /// Finds the configuration of a topic.
    /// </summary>
    /// <param name="topic">The topic name, matched case-sensitively.</param>
    /// <returns>The topic's configuration, or <see langword="null"/> if it is not configured.</returns>
    public TopicConfig? GetTopic(string topic) =>
        Topics.FirstOrDefault(t => string.Equals(t.Topic, topic, StringComparison.Ordinal));


    /// <summary>
    /// Gets the CSS class that colours a topic in the UI.
    /// </summary>
    /// <param name="topic">The topic name.</param>
    /// <returns>
    /// <c>topic-{family}-{shade}</c>, or <c>topic-{family}</c> when the topic has no shade,
    /// or <c>topic-default</c> when the topic is not configured.
    /// </returns>
    public string GetTopicCssClass(string topic)
    {
        var config = GetTopic(topic);
        if (config is null) return "topic-default";
        return string.IsNullOrEmpty(config.ColorShade)
            ? $"topic-{config.ColorFamily}"
            : $"topic-{config.ColorFamily}-{config.ColorShade}";
    }


    /// <summary>
    /// Gets the display label of a topic.
    /// </summary>
    /// <param name="topic">The topic name.</param>
    /// <returns>The configured label, or <paramref name="topic"/> itself when the topic is not configured.</returns>
    public string GetTopicLabel(string topic)
    {
        var config = GetTopic(topic);
        return config?.Label ?? topic;
    }
}
