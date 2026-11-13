using EsbDemo.Web.Services;

namespace EsbDemo.Web.Tests.Unit.Services;

public sealed class ConfigServiceTests
{
    private const string Topics = """
        [
          { "topic": "shaded",   "label": "Shaded Topic", "colorFamily": "blue", "colorShade": "dark" },
          { "topic": "unshaded", "label": "Plain Topic",  "colorFamily": "red",  "colorShade": "" }
        ]
        """;



    [Fact]
    public void Apps_and_Topics_when_not_loaded_are_empty()
    {
        var sut = new ConfigService(new FakeWebHostEnvironment(AppContext.BaseDirectory));

        Assert.Empty(sut.Apps);
        Assert.Empty(sut.Topics);
    }



    [Fact]
    public async Task LoadAsync_when_reading_the_shipped_config_loads_every_app_and_topic()
    {
        var sut = await ShippedConfig.LoadAsync();

        Assert.Equal
        (
            new[] { "CRM", "ERP", "eCommerce", "WMS", "Customer Portal", "Data Warehouse" },
            sut.Apps.Select(a => a.Name)
        );
        Assert.Contains(sut.Topics, t => string.Equals(t.Topic, "account.created", StringComparison.Ordinal) && string.Equals(t.Label, "Customer Created", StringComparison.Ordinal));
    }



    [Fact]
    public async Task LoadAsync_when_the_files_hold_json_null_loads_empty_lists()
    {
        var sut = await TestConfig.LoadAsync("null", "null");

        Assert.Empty(sut.Apps);
        Assert.Empty(sut.Topics);
    }



    [Fact]
    public async Task GetTopic_when_topic_is_configured_returns_it()
    {
        var sut = await TestConfig.LoadAsync("[]", Topics);

        Assert.Equal("Shaded Topic", sut.GetTopic("shaded")?.Label);
    }



    [Fact]
    public async Task GetTopic_when_topic_differs_only_by_case_returns_null()
    {
        var sut = await TestConfig.LoadAsync("[]", Topics);

        Assert.Null(sut.GetTopic("SHADED"));
    }



    [Fact]
    public async Task GetTopicCssClass_when_topic_has_a_shade_returns_family_and_shade()
    {
        var sut = await TestConfig.LoadAsync("[]", Topics);

        Assert.Equal("topic-blue-dark", sut.GetTopicCssClass("shaded"));
    }



    [Fact]
    public async Task GetTopicCssClass_when_topic_has_no_shade_returns_family_only()
    {
        var sut = await TestConfig.LoadAsync("[]", Topics);

        Assert.Equal("topic-red", sut.GetTopicCssClass("unshaded"));
    }



    [Fact]
    public async Task GetTopicCssClass_when_topic_is_unknown_returns_the_default_class()
    {
        var sut = await TestConfig.LoadAsync("[]", Topics);

        Assert.Equal("topic-default", sut.GetTopicCssClass("unknown"));
    }



    [Fact]
    public async Task GetTopicLabel_when_topic_is_configured_returns_its_label()
    {
        var sut = await TestConfig.LoadAsync("[]", Topics);

        Assert.Equal("Plain Topic", sut.GetTopicLabel("unshaded"));
    }



    [Fact]
    public async Task GetTopicLabel_when_topic_is_unknown_returns_the_topic_name()
    {
        var sut = await TestConfig.LoadAsync("[]", Topics);

        Assert.Equal("unknown.topic", sut.GetTopicLabel("unknown.topic"));
    }
}
