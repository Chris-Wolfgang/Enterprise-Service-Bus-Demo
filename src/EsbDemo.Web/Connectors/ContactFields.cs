using System.Diagnostics.CodeAnalysis;

namespace EsbDemo.Web.Connectors;

/// <summary>
/// Shared handling of the optional <c>Email</c> and <c>Phone</c> fields of the canonical account messages.
/// A value that is <see langword="null"/> or empty means "not set": it is left out of an outbound message,
/// and an inbound message never writes it over a value the receiving system already holds.
/// </summary>
internal static class ContactFields
{
    /// <summary>
    /// Adds <paramref name="key"/> to <paramref name="fields"/> only when <paramref name="value"/> is set,
    /// so receivers that look the key up with <c>TryGetValue</c> keep their own value.
    /// </summary>
    /// <param name="fields">The outbound message fields.</param>
    /// <param name="key">The canonical field name.</param>
    /// <param name="value">The source system's value, or <see langword="null"/> when it has none.</param>
    /// <returns><paramref name="fields"/>, for chaining.</returns>
    internal static Dictionary<string, string> AddIfSet(this Dictionary<string, string> fields, string key, string? value)
    {
        if (!string.IsNullOrEmpty(value))
            fields[key] = value;
        return fields;
    }



    /// <summary>
    /// Reads an optional field from an inbound message, treating an empty value the same as a missing one.
    /// </summary>
    /// <param name="fields">The inbound message fields.</param>
    /// <param name="key">The canonical field name.</param>
    /// <param name="value">The value when it is set; otherwise <see langword="null"/>.</param>
    /// <returns><see langword="true"/> when the message carries a non-empty value for <paramref name="key"/>.</returns>
    internal static bool TryGetSet(this Dictionary<string, string> fields, string key, [NotNullWhen(true)] out string? value)
    {
        if (fields.TryGetValue(key, out var raw) && !string.IsNullOrEmpty(raw))
        {
            value = raw;
            return true;
        }

        value = null;
        return false;
    }
}
