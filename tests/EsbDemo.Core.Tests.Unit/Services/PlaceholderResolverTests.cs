using System;
using System.Globalization;
using EsbDemo.Core.Services;

namespace EsbDemo.Core.Tests.Unit.Services;

public sealed class PlaceholderResolverTests
{
    private static readonly DateTimeOffset Now = new DateTimeOffset(2026, 3, 7, 14, 5, 9, TimeSpan.Zero);



    [Fact]
    public void Resolve_when_random_is_null_throws_ArgumentNullException()
    {
        var ex = Assert.Throws<ArgumentNullException>
        (
            () => PlaceholderResolver.Resolve("CUST{####}", Now, null!)
        );

        Assert.Equal("random", ex.ParamName);
    }



    [Fact]
    public void Resolve_when_template_is_null_returns_null()
    {
        Assert.Null(PlaceholderResolver.Resolve(null!, Now, new Random(1)));
    }



    [Fact]
    public void Resolve_when_template_is_empty_returns_empty()
    {
        Assert.Equal(string.Empty, PlaceholderResolver.Resolve(string.Empty, Now, new Random(1)));
    }



    [Fact]
    public void Resolve_when_template_has_no_placeholders_returns_it_unchanged()
    {
        Assert.Equal("Acme Corp", PlaceholderResolver.Resolve("Acme Corp", Now, new Random(1)));
    }



    [Fact]
    public void Resolve_when_template_has_date_and_time_placeholders_formats_now()
    {
        var expected = Now.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)
            + " "
            + Now.ToString("HH:mm", CultureInfo.InvariantCulture);

        var actual = PlaceholderResolver.Resolve("{yyyy-MM-dd} {hh:mm}", Now, new Random(1));

        Assert.Equal(expected, actual);
    }



    [Fact]
    public void Resolve_when_template_has_four_digit_placeholder_uses_the_next_number_from_1000_to_9998()
    {
        var expected = new Random(42).Next(1000, 9999);

        var actual = PlaceholderResolver.Resolve("CUST{####}", Now, new Random(42));

        Assert.Equal("CUST" + expected.ToString(CultureInfo.InvariantCulture), actual);
    }



    [Fact]
    public void Resolve_when_template_has_two_digit_placeholder_uses_a_number_from_10_to_98()
    {
        // Resolve always draws the four-digit value first, even when the template has no {####},
        // so {##} receives the second number from the generator.
        var random = new Random(7);
        _ = random.Next(1000, 9999);
        var expected = random.Next(10, 99);

        var actual = PlaceholderResolver.Resolve("Acme Corp {##}", Now, new Random(7));

        Assert.Equal("Acme Corp " + expected.ToString(CultureInfo.InvariantCulture), actual);
        Assert.InRange(expected, 10, 98);
    }



    [Fact]
    public void Resolve_when_a_placeholder_repeats_uses_one_value_for_every_occurrence()
    {
        var actual = PlaceholderResolver.Resolve("{##}-{##}", Now, new Random(3));

        var parts = actual.Split('-');
        Assert.Equal(parts[0], parts[1]);
    }



    [Fact]
    public void Resolve_when_both_number_placeholders_are_present_resolves_four_digits_before_two()
    {
        var random = new Random(11);
        var expectedFour = random.Next(1000, 9999);
        var expectedTwo = random.Next(10, 99);

        var actual = PlaceholderResolver.Resolve("{####}/{##}", Now, new Random(11));

        Assert.Equal
        (
            expectedFour.ToString(CultureInfo.InvariantCulture) + "/" + expectedTwo.ToString(CultureInfo.InvariantCulture),
            actual
        );
    }


    [Fact]
    public void Resolve_when_current_culture_uses_a_non_Gregorian_calendar_still_formats_a_Gregorian_date()
    {
        var original = CultureInfo.CurrentCulture;
        try
        {
            // th-TH defaults to the Thai Buddhist calendar (2026 renders as 2569).
            CultureInfo.CurrentCulture = new CultureInfo("th-TH");

            var actual = PlaceholderResolver.Resolve("{yyyy-MM-dd} {hh:mm}", Now, new Random(1));

            Assert.Equal("2026-03-07 14:05", actual);
        }
        finally
        {
            CultureInfo.CurrentCulture = original;
        }
    }
}
