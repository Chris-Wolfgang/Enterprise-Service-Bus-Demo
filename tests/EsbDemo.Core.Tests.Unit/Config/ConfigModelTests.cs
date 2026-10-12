using System.Collections.Generic;
using EsbDemo.Core.Config;
using Xunit;

namespace EsbDemo.Core.Tests.Unit.Config;

public sealed class ConfigModelTests
{
    [Fact]
    public void AppConfig_when_created_has_documented_defaults()
    {
        var sut = new AppConfig();

        Assert.Equal(string.Empty, sut.Name);
        Assert.Equal(0, sut.DisplayOrder);
        Assert.True(sut.DefaultOnline);
        Assert.Empty(sut.Publishes);
        Assert.Empty(sut.Subscribes);
        Assert.Empty(sut.AutoResponses);
    }



    [Fact]
    public void AppConfig_when_properties_are_set_returns_the_set_values()
    {
        var autoResponses = new List<AutoResponse> { new AutoResponse { On = "account.create", Publish = "account.created" } };

        var sut = new AppConfig
        {
            Name = "ERP",
            DisplayOrder = 2,
            DefaultOnline = false,
            Publishes = new List<string> { "account.created" },
            Subscribes = new List<string> { "account.create" },
            AutoResponses = autoResponses
        };

        Assert.Equal("ERP", sut.Name);
        Assert.Equal(2, sut.DisplayOrder);
        Assert.False(sut.DefaultOnline);
        Assert.Equal(new[] { "account.created" }, sut.Publishes);
        Assert.Equal(new[] { "account.create" }, sut.Subscribes);
        Assert.Same(autoResponses, sut.AutoResponses);
    }



    [Fact]
    public void AutoResponse_when_created_has_empty_topics()
    {
        var sut = new AutoResponse();

        Assert.Equal(string.Empty, sut.On);
        Assert.Equal(string.Empty, sut.Publish);
    }



    [Fact]
    public void AutoResponse_when_properties_are_set_returns_the_set_values()
    {
        var sut = new AutoResponse { On = "order.place", Publish = "order.placed" };

        Assert.Equal("order.place", sut.On);
        Assert.Equal("order.placed", sut.Publish);
    }



    [Fact]
    public void FieldConfig_when_created_is_an_empty_text_field()
    {
        var sut = new FieldConfig();

        Assert.Equal(string.Empty, sut.Name);
        Assert.Equal("text", sut.Type);
        Assert.Equal(string.Empty, sut.Default);
    }



    [Fact]
    public void FieldConfig_when_properties_are_set_returns_the_set_values()
    {
        var sut = new FieldConfig { Name = "CustomerId", Type = "account", Default = "CUST{####}" };

        Assert.Equal("CustomerId", sut.Name);
        Assert.Equal("account", sut.Type);
        Assert.Equal("CUST{####}", sut.Default);
    }



    [Fact]
    public void TopicConfig_when_created_has_documented_defaults()
    {
        var sut = new TopicConfig();

        Assert.Equal(string.Empty, sut.Topic);
        Assert.Equal(string.Empty, sut.Label);
        Assert.Equal(string.Empty, sut.ColorFamily);
        Assert.Equal(string.Empty, sut.ColorShade);
        Assert.Empty(sut.Fields);
    }



    [Fact]
    public void TopicConfig_when_properties_are_set_returns_the_set_values()
    {
        var fields = new List<FieldConfig> { new FieldConfig { Name = "Name" } };

        var sut = new TopicConfig
        {
            Topic = "account.created",
            Label = "Customer Created",
            ColorFamily = "blue",
            ColorShade = "dark",
            Fields = fields
        };

        Assert.Equal("account.created", sut.Topic);
        Assert.Equal("Customer Created", sut.Label);
        Assert.Equal("blue", sut.ColorFamily);
        Assert.Equal("dark", sut.ColorShade);
        Assert.Same(fields, sut.Fields);
    }
}
