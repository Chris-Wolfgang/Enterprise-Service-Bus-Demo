using System.Globalization;

namespace EsbDemo.Core.Services;

/// <summary>
/// Expands the placeholders used in field defaults into concrete sample values.
/// </summary>
public static class PlaceholderResolver
{
    private const string DateToken = "{yyyy-MM-dd}";
    private const string TimeToken = "{hh:mm}";
    private const string FourDigitToken = "{####}";
    private const string TwoDigitToken = "{##}";



    /// <summary>
    /// Replaces every placeholder in <paramref name="template"/> with a value.
    /// </summary>
    /// <remarks>
    /// Supported placeholders: <c>{yyyy-MM-dd}</c> and <c>{hh:mm}</c> (the date and 24-hour time of
    /// <paramref name="now"/>), <c>{####}</c> (a random number from 1000 to 9999) and <c>{##}</c>
    /// (a random number from 10 to 99). A random number is drawn only when its placeholder is present,
    /// so a template with only <c>{##}</c> receives the first number from <paramref name="random"/>.
    /// </remarks>
    /// <param name="template">The text to expand. A <see langword="null"/> or empty value is returned unchanged.</param>
    /// <param name="now">The moment used for the date and time placeholders.</param>
    /// <param name="random">The source of the random-number placeholders.</param>
    /// <returns>The expanded text.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="random"/> is <see langword="null"/>.</exception>
    /// <example>
    /// <code>
    /// var name = PlaceholderResolver.Resolve("Acme Corp {##}", DateTimeOffset.Now, Random.Shared);
    /// </code>
    /// </example>
    public static string Resolve(string template, DateTimeOffset now, Random random)
    {
        if (random is null)
            throw new ArgumentNullException(nameof(random));

        if (string.IsNullOrEmpty(template))
            return template;

        if (template.IndexOf('{') < 0)
            return template;

        var result = template;

        if (Contains(result, DateToken))
            result = result.Replace(DateToken, now.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));

        if (Contains(result, TimeToken))
            result = result.Replace(TimeToken, now.ToString("HH:mm", CultureInfo.InvariantCulture));

        if (Contains(result, FourDigitToken))
            result = result.Replace(FourDigitToken, random.Next(1000, 10000).ToString(CultureInfo.InvariantCulture));

        if (Contains(result, TwoDigitToken))
            result = result.Replace(TwoDigitToken, random.Next(10, 100).ToString(CultureInfo.InvariantCulture));

        return result;
    }



    private static bool Contains(string text, string token) =>
        text.IndexOf(token, StringComparison.Ordinal) >= 0;
}
