using System.Globalization;

namespace EsbDemo.Core.Services;

/// <summary>
/// Expands the placeholders used in field defaults into concrete sample values.
/// </summary>
public static class PlaceholderResolver
{
    /// <summary>
    /// Replaces every placeholder in <paramref name="template"/> with a value.
    /// </summary>
    /// <remarks>
    /// Supported placeholders: <c>{yyyy-MM-dd}</c> and <c>{hh:mm}</c> (the date and 24-hour time of
    /// <paramref name="now"/>), <c>{####}</c> (a random number from 1000 to 9998) and <c>{##}</c>
    /// (a random number from 10 to 98).
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

        return template
            .Replace("{yyyy-MM-dd}", now.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture))
            .Replace("{hh:mm}", now.ToString("HH:mm", CultureInfo.InvariantCulture))
            .Replace("{####}", random.Next(1000, 9999).ToString(CultureInfo.InvariantCulture))
            .Replace("{##}", random.Next(10, 99).ToString(CultureInfo.InvariantCulture));
    }
}
