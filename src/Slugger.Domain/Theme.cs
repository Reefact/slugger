namespace Slugger.Domain;

/// <summary>
/// A loaded theme: one JSON file, one completely sealed namespace. Two themes may use the
/// same category name without any relationship between them, and a draw never crosses a
/// theme boundary.
/// </summary>
public sealed class Theme
{
    public Theme(
        string name,
        IReadOnlyDictionary<string, IReadOnlyList<string>> adjectives,
        IReadOnlyDictionary<string, IReadOnlyList<string>> participles,
        IReadOnlyList<Noun> nouns,
        ThemeDefaults? defaults = null,
        bool allowSmall = false)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(adjectives);
        ArgumentNullException.ThrowIfNull(participles);
        ArgumentNullException.ThrowIfNull(nouns);

        Name = name;
        Adjectives = adjectives;
        Participles = participles;
        Nouns = nouns;
        Defaults = defaults ?? ThemeDefaults.Empty;
        AllowSmall = allowSmall;
    }

    /// <summary>
    /// The theme's file name without its extension. A theme is identified by its file name,
    /// never by a field inside the JSON.
    /// </summary>
    public string Name { get; }

    /// <summary>Category name to adjectives. Category names are arbitrary; "common" is a convention, not a keyword.</summary>
    public IReadOnlyDictionary<string, IReadOnlyList<string>> Adjectives { get; }

    /// <summary>Optional, same shape as <see cref="Adjectives"/>. Empty when the theme declares no participles.</summary>
    public IReadOnlyDictionary<string, IReadOnlyList<string>> Participles { get; }

    public IReadOnlyList<Noun> Nouns { get; }

    public ThemeDefaults Defaults { get; }

    /// <summary>Set by the theme's author to opt out of the minimum size rules for good.</summary>
    public bool AllowSmall { get; }

    public bool HasParticiples => Participles.Count > 0;
}
