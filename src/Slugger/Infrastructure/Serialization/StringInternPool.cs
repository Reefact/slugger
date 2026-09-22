namespace Slugger.Infrastructure.Serialization;

/// <summary>
///     One pool for the whole run, created once at the composition root and handed to every
///     load. The theme files stay optimised for a human reader - repeating a word across
///     categories, nouns or themes is normal and free - and deduplication happens invisibly at
///     load time.
/// </summary>
/// <remarks>
///     Never one pool per file: that would only deduplicate inside a single file, which is the
///     case that matters least. This is a memory concern only and creates no logical link
///     between two same named categories in different themes.
/// </remarks>
internal sealed class StringInternPool {

    #region Fields

    private readonly Dictionary<string, string> _pool = new(StringComparer.Ordinal);

    #endregion

    /// <summary>How many distinct strings the pool holds.</summary>
    public int Count => _pool.Count;

    /// <summary>Returns the single shared instance of this value, adding it on first sight.</summary>
    public string Intern(string value) {
        ArgumentNullException.ThrowIfNull(value);

        if (_pool.TryGetValue(value, out string? existing)) {
            return existing;
        }

        _pool[value] = value;

        return value;
    }

}