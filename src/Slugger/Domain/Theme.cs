using System.Collections.ObjectModel;

namespace Slugger.Domain;

/// <summary>
/// A loaded theme: one JSON file, one completely sealed namespace. Two themes may use the
/// same category name without any relationship between them, and a draw never crosses a
/// theme boundary.
/// </summary>
public sealed class Theme
{
    /// <param name="name">The file name without its extension; a theme is never named by a field inside its JSON.</param>
    /// <param name="adjectives">Category name to adjectives.</param>
    /// <param name="participles">Category name to participles; empty when the theme declares none.</param>
    /// <param name="nouns">Every noun, with the categories it belongs to.</param>
    /// <param name="defaults">The theme's own formatting preferences, or null for none.</param>
    /// <param name="allowSmall">Whether its author opted out of the minimum size rules.</param>
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

    /// <summary>Every noun in the theme.</summary>
    public IReadOnlyList<Noun> Nouns { get; }

    /// <summary>The theme's formatting preferences, applied only when the style is being mimicked.</summary>
    public ThemeDefaults Defaults { get; }

    /// <summary>Set by the theme's author to opt out of the minimum size rules for good.</summary>
    public bool AllowSmall { get; }

    /// <summary>
    /// What the theme promises about the length of its slugs, per shape (DEC0018). Checked when
    /// the theme is loaded, so a word too long for the promise is a refusal rather than a slug
    /// the destination rejects.
    /// </summary>
    public MaxLength MaxLength { get; init; } = MaxLength.None;

    /// <summary>
    /// Adjective to the participles it refuses beside it (DEC0017). One way round on purpose:
    /// the adjective is the key and a word declared in both sections refuses nothing as a
    /// participle. Empty when the theme declares none, which is the ordinary case.
    /// </summary>
    public IReadOnlyDictionary<string, IReadOnlyList<string>> Incompatible { get; init; } =
        ReadOnlyDictionary<string, IReadOnlyList<string>>.Empty;

    /// <summary>The theme's own "meta" block - descriptive only, never consulted by generation.</summary>
    public ThemeMetadata Metadata { get; init; } = ThemeMetadata.Empty;

    /// <summary>Whether the theme declares any participle at all, anywhere.</summary>
    public bool HasParticiples => Participles.Count > 0;

    /// <summary>Whether any adjective refuses a participle beside it.</summary>
    public bool HasIncompatibilities => Incompatible.Count > 0;
}
