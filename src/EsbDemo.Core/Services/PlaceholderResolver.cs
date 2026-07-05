namespace EsbDemo.Core.Services;

public static class PlaceholderResolver
{
    public static string Resolve(string template, DateTimeOffset now, Random random)
    {
        if (random is null)
            throw new ArgumentNullException(nameof(random));

        if (string.IsNullOrEmpty(template))
            return template;

        return template
            .Replace("{yyyy-MM-dd}", now.ToString("yyyy-MM-dd"))
            .Replace("{hh:mm}", now.ToString("HH:mm"))
            .Replace("{####}", random.Next(1000, 9999).ToString())
            .Replace("{##}", random.Next(10, 99).ToString());
    }
}
