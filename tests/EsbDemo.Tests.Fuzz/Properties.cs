using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text.RegularExpressions;
using CsCheck;
using EsbDemo.Core.Services;
using EsbDemo.Web.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Time.Testing;

namespace EsbDemo.Tests.Fuzz;

/// <summary>
/// Property-based fuzz tests for the placeholder resolver and the bus.
/// </summary>
/// <remarks>
/// <para>
/// The unit suites assert chosen examples. These assert invariants over generated templates,
/// clocks, topics, sources and payloads.
/// </para>
/// <para>
/// Case count comes from <c>FUZZ_ITERATIONS</c> and the per-property time budget from
/// <c>FUZZ_TIME_SECONDS</c>, so the same properties serve as a fast per-PR pass and as the long
/// scheduled run in <c>fuzz.yaml</c>. The defaults are small so <c>dotnet test</c> stays quick.
/// </para>
/// <para>
/// <c>Sample</c> evaluates cases on several threads, so every property is a pure function of its
/// generated input: each case builds its own <see cref="MonitorService"/> and fake clock, and the
/// only shared object is the loaded, read-only <see cref="ConfigService"/>.
/// </para>
/// </remarks>
public sealed class Properties
{
    private static readonly string[] Tokens = ["{yyyy-MM-dd}", "{hh:mm}", "{####}", "{##}"];

    private static readonly TimeSpan RegexTimeout = TimeSpan.FromSeconds(1);



    /// <summary>Text that can never form a placeholder: no braces, and no digits so resolved numbers stay unambiguous.</summary>
    private static readonly Gen<string> Literal =
        Gen.String[Gen.Char["abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ -_.@:/#"], 0, 12];



    /// <summary>A template built from literal text and placeholders in any order.</summary>
    private static readonly Gen<string> AnyTemplate =
        Gen.OneOf(Literal, Gen.OneOfConst(Tokens)).Array[0, 8].Select(string.Concat);



    private static readonly Gen<DateTimeOffset> AnyNow =
        Gen.Long[DateTimeOffset.MinValue.UtcTicks, DateTimeOffset.MaxValue.UtcTicks].Select(t => new DateTimeOffset(t, TimeSpan.Zero));



    private static readonly Gen<Dictionary<string, string>> AnyPayload =
        Gen.Select(Gen.String[Gen.Char["abcdefXYZ"], 1, 6], Gen.String)
            .Array[0, 6]
            .Select
            (
                pairs =>
                {
                    var fields = new Dictionary<string, string>(StringComparer.Ordinal);
                    foreach (var (key, value) in pairs)
                        fields[key] = value;
                    return fields;
                }
            );



    private static int FuzzIterations => GetEnvironmentInt("FUZZ_ITERATIONS", 1_000);



    private static int FuzzTimeSeconds => GetEnvironmentInt("FUZZ_TIME_SECONDS", -1);



    private static int GetEnvironmentInt(string name, int fallback)
    {
        var raw = Environment.GetEnvironmentVariable(name);

        return int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed) && parsed > 0 ? parsed : fallback;
    }



    private static async Task<ConfigService> LoadConfigAsync()
    {
        var config = new ConfigService(new FuzzHostEnvironment(AppContext.BaseDirectory));
        await config.LoadAsync();
        return config;
    }



    private static bool SameEntries(IReadOnlyDictionary<string, string> expected, IReadOnlyDictionary<string, string> actual) =>
        expected.Count == actual.Count
        && expected.All(kv => actual.TryGetValue(kv.Key, out var value) && string.Equals(value, kv.Value, StringComparison.Ordinal));



    // ── PlaceholderResolver ──────────────────────────────────────────────

    [Fact]
    public void Resolve_leaves_text_without_placeholders_unchanged()
    {
        Gen.Select(Gen.String, Gen.Int, AnyNow)
            .Where(t => !Tokens.Any(token => t.Item1.Contains(token, StringComparison.Ordinal)))
            .Sample
            (
                (template, seed, now) => string.Equals(PlaceholderResolver.Resolve(template, now, new Random(seed)), template, StringComparison.Ordinal),
                iter: FuzzIterations,
                time: FuzzTimeSeconds
            );
    }



    [Fact]
    public void Resolve_replaces_every_placeholder()
    {
        Gen.Select(AnyTemplate, Gen.Int, AnyNow)
            .Sample
            (
                (template, seed, now) =>
                {
                    var resolved = PlaceholderResolver.Resolve(template, now, new Random(seed));
                    return !resolved.Contains('{', StringComparison.Ordinal) && !resolved.Contains('}', StringComparison.Ordinal);
                },
                iter: FuzzIterations,
                time: FuzzTimeSeconds
            );
    }



    [Fact]
    public void Resolve_puts_four_and_two_digit_numbers_in_their_ranges()
    {
        Gen.Select(Literal, Literal, Literal, Gen.Int)
            .Sample
            (
                (a, b, c, seed) =>
                {
                    var resolved = PlaceholderResolver.Resolve(a + "{####}" + b + "{##}" + c, DateTimeOffset.UnixEpoch, new Random(seed));
                    var match = Regex.Match
                    (
                        resolved,
                        "^" + Regex.Escape(a) + "([0-9]{4})" + Regex.Escape(b) + "([0-9]{2})" + Regex.Escape(c) + "$",
                        RegexOptions.None,
                        RegexTimeout
                    );
                    return match.Success
                        && int.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture) is >= 1000 and <= 9998
                        && int.Parse(match.Groups[2].Value, CultureInfo.InvariantCulture) is >= 10 and <= 98;
                },
                iter: FuzzIterations,
                time: FuzzTimeSeconds
            );
    }



    [Fact]
    public void Resolve_renders_the_date_and_time_placeholders_from_now()
    {
        Gen.Select(AnyNow, Literal)
            .Sample
            (
                (now, text) =>
                {
                    var expected = text + now.ToString("yyyy-MM-dd", CultureInfo.CurrentCulture) + text + now.ToString("HH:mm", CultureInfo.CurrentCulture);
                    var actual = PlaceholderResolver.Resolve(text + "{yyyy-MM-dd}" + text + "{hh:mm}", now, new Random(0));
                    return string.Equals(expected, actual, StringComparison.Ordinal);
                },
                iter: FuzzIterations,
                time: FuzzTimeSeconds
            );
    }



    [Fact]
    public void Resolve_is_deterministic_for_a_given_seed_and_clock()
    {
        Gen.Select(AnyTemplate, Gen.Int, AnyNow)
            .Sample
            (
                (template, seed, now) => string.Equals
                (
                    PlaceholderResolver.Resolve(template, now, new Random(seed)),
                    PlaceholderResolver.Resolve(template, now, new Random(seed)),
                    StringComparison.Ordinal
                ),
                iter: FuzzIterations,
                time: FuzzTimeSeconds
            );
    }



    [Fact]
    public void Resolve_rejects_a_null_random_for_any_template()
    {
        Gen.String.Sample
        (
            template => Assert.Throws<ArgumentNullException>(() => PlaceholderResolver.Resolve(template, DateTimeOffset.UnixEpoch, null!)),
            iter: FuzzIterations,
            time: FuzzTimeSeconds
        );
    }



    // ── MonitorService ───────────────────────────────────────────────────

    [Fact]
    public async Task Publish_logs_one_send_and_one_receive_per_subscriber_and_hands_each_an_equal_copy()
    {
        var config = await LoadConfigAsync();
        var topics = config.Topics.Select(t => t.Topic).ToArray();
        var sources = config.Apps.Select(a => a.Name).Append("Tester").ToArray();

        Gen.Select(Gen.OneOfConst(topics), Gen.OneOfConst(sources), AnyPayload)
            .Sample
            (
                (topic, source, fields) =>
                {
                    var sut = new MonitorService(config, new FakeTimeProvider());
                    var handled = new List<(string App, Dictionary<string, string> Fields)>();
                    foreach (var app in config.Apps)
                    {
                        var name = app.Name;
                        sut.RegisterHandler(name, (_, f) => { handled.Add((name, f)); return Task.CompletedTask; });
                    }

                    sut.Publish(topic, source, "corr", fields);

                    var subscribers = config.Apps.Where(a => a.Subscribes.Contains(topic)).Select(a => a.Name).ToArray();
                    var sends = sut.Entries.Count(e => string.Equals(e.Direction, "→ SEND", StringComparison.Ordinal));
                    var receivers = sut.Entries.Where(e => string.Equals(e.Direction, "← RECV", StringComparison.Ordinal)).Select(e => e.AppName);

                    return sends == 1
                        && receivers.SequenceEqual(subscribers, StringComparer.Ordinal)
                        && handled.Select(h => h.App).SequenceEqual(subscribers.Where(s => !string.Equals(s, source, StringComparison.OrdinalIgnoreCase)), StringComparer.Ordinal)
                        && handled.All(h => !ReferenceEquals(h.Fields, fields) && SameEntries(fields, h.Fields))
                        && handled.Select(h => h.Fields).Distinct(ReferenceEqualityComparer.Instance).Count() == handled.Count;
                },
                iter: FuzzIterations,
                time: FuzzTimeSeconds
            );
    }



    [Fact]
    public async Task Auto_reply_keeps_the_request_values_and_mints_an_eight_character_customer_id()
    {
        var config = await LoadConfigAsync();
        var value = Gen.String[Gen.Char["abcdefghijklmnopqrstuvwxyz @.-0123456789"], 1, 20];
        var customerId = new Regex("^CUST[0-9]{4}$", RegexOptions.None, RegexTimeout);

        Gen.Select(value, value, value)
            .Sample
            (
                (name, email, phone) =>
                {
                    var time = new FakeTimeProvider();
                    var sut = new MonitorService(config, time);
                    var fields = new Dictionary<string, string>(StringComparer.Ordinal) { ["Name"] = name, ["Email"] = email, ["Phone"] = phone };

                    sut.Publish("account.create", "CRM", "corr", fields, isUserInitiated: true);
                    time.Advance(MonitorService.AutoResponseDelay);
                    var resolved = SpinWait.SpinUntil(() => string.Equals(sut.Accounts.Single().Status, "Active", StringComparison.Ordinal), TimeSpan.FromSeconds(5));

                    var account = sut.Accounts.Single();
                    return resolved
                        && customerId.IsMatch(account.CustomerId ?? string.Empty)
                        && string.Equals(account.Fields["Name"], name, StringComparison.Ordinal)
                        && string.Equals(account.Fields["Email"], email, StringComparison.Ordinal)
                        && string.Equals(account.Fields["Phone"], phone, StringComparison.Ordinal);
                },
                iter: FuzzIterations,
                time: FuzzTimeSeconds
            );
    }
}



/// <summary>A host environment whose content root is the test output folder.</summary>
[ExcludeFromCodeCoverage]
internal sealed class FuzzHostEnvironment : IWebHostEnvironment
{
    public FuzzHostEnvironment(string contentRootPath)
    {
        ContentRootPath = contentRootPath;
    }



    public string ContentRootPath { get; set; }



    public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();



    public string ApplicationName { get; set; } = "EsbDemo.Web";



    public string EnvironmentName { get; set; } = "Development";



    public string WebRootPath { get; set; } = string.Empty;



    public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
}
