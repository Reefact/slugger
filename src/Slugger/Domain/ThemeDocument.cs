#region Usings declarations

using System.Collections.ObjectModel;

#endregion

namespace Slugger.Domain;

/// <summary>
///     A theme as a theme file states it: its nouns, the adjectives and participles they reach through
///     their categories, and its own style. <see cref="Themes" /> returns one, and every generation
///     call takes one.
/// </summary>
/// <remarks>
///     <para>
///         It never changes after it is built, so one instance can be shared by every thread of an
///         application. Load it once and keep it: loading reads and validates the whole file again.
///     </para>
///     <para>
///         Each theme is a closed world. Two themes may use the same category name with no relationship
///         between them, and a draw never crosses from one theme into another.
///     </para>
/// </remarks>
public sealed class ThemeDocument {

    #region Constructors & Destructor

    /// <summary>
    ///     Builds a theme in memory, as it is, without reading a file.
    /// </summary>
    /// <remarks>
    ///     Nothing here normalises or validates: the words are kept exactly as written - write them in
    ///     lowercase, with single spaces - and a theme too small, or naming a category it does not
    ///     declare, is accepted. Pass it to <see cref="Validation.ThemeValidator.Validate(ThemeDocument, bool)" />
    ///     to apply the rules a loaded theme is held to, or load it from JSON with <see cref="Themes" />.
    /// </remarks>
    /// <param name="name">The theme's name. A loaded theme is named after its file, without the extension.</param>
    /// <param name="adjectives">Category name to adjectives.</param>
    /// <param name="participles">Category name to participles; empty when the theme declares none.</param>
    /// <param name="nouns">Every noun, with the categories it belongs to.</param>
    /// <param name="defaults">The theme's own style, or null for none.</param>
    /// <param name="allowSmall">Whether the theme waives the size floors, as <c>"allowSmall": true</c> does in a file.</param>
    /// <exception cref="ArgumentException"><paramref name="name" /> is null, empty or white space.</exception>
    /// <exception cref="ArgumentNullException">One of the dictionaries or the noun list is null.</exception>
    public ThemeDocument(string                                             name,
                 IReadOnlyDictionary<string, IReadOnlyList<string>> adjectives,
                 IReadOnlyDictionary<string, IReadOnlyList<string>> participles,
                 IReadOnlyList<NounEntry>                                nouns,
                 ThemeDefaults?                                     defaults   = null,
                 bool                                               allowSmall = false) {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(adjectives);
        ArgumentNullException.ThrowIfNull(participles);
        ArgumentNullException.ThrowIfNull(nouns);

        Name        = name;
        Adjectives  = adjectives;
        Participles = participles;
        Nouns       = nouns;
        Defaults    = defaults ?? ThemeDefaults.Empty;
        AllowSmall  = allowSmall;
    }

    #endregion

    /// <summary>
    ///     The theme's name. A loaded theme is named after its file, without the extension, and never by
    ///     a field inside the JSON; a theme loaded from a string takes the name the caller gave.
    /// </summary>
    public string Name { get; }

    /// <summary>
    ///     Category name to adjectives. Every noun reaches the adjectives of its own categories and those
    ///     of <c>common</c>, which is otherwise an ordinary category name.
    /// </summary>
    public IReadOnlyDictionary<string, IReadOnlyList<string>> Adjectives { get; }

    /// <summary>
    ///     Category name to participles, reached the same way as <see cref="Adjectives" />. Empty when the
    ///     theme declares no participle.
    /// </summary>
    public IReadOnlyDictionary<string, IReadOnlyList<string>> Participles { get; }

    /// <summary>Every noun in the theme.</summary>
    public IReadOnlyList<NounEntry> Nouns { get; }

    /// <summary>
    ///     The theme's own style - the <c>defaults</c> block of its file - or <see cref="ThemeDefaults.Empty" />
    ///     when it states none.
    /// </summary>
    /// <remarks>
    ///     Generation never applies it on its own. Pass <c>GenerationOptions.Default.WithDefaultsOf(theme)</c>
    ///     to draw in the theme's style: see <see cref="GenerationOptions.WithDefaultsOf" />.
    /// </remarks>
    public ThemeDefaults Defaults { get; }

    /// <summary>
    ///     Whether the theme waives the size floors - the minimum number of nouns, of words per noun and of
    ///     combinations per category - as <c>"allowSmall": true</c> in its file does.
    /// </summary>
    public bool AllowSmall { get; }

    /// <summary>
    ///     The longest slug the theme promises to produce, one figure per shape - the <c>maxLength</c>
    ///     block of its file - or <see cref="Domain.MaxLength.None" /> when it promises nothing.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         The promise is checked when the theme is loaded, measured with the theme's own style, so a
    ///         word too long for it gets the theme refused rather than producing a slug its destination
    ///         rejects. It is not a generation option: to limit the length of your slugs, set
    ///         <see cref="GenerationOptions.MaxLength" />.
    ///     </para>
    ///     <para>
    ///         Checking the promise is the most expensive part of a load: a large theme that declares one
    ///         can take about a second to load.
    ///     </para>
    ///     <para>
    ///         See decision record DEC0018 (in French):
    ///         https://github.com/Reefact/slugger/blob/main/docs/idr/DEC0018-longueur-maximale-tenue-en-retirant-des-mots.md
    ///     </para>
    /// </remarks>
    public MaxLength MaxLength { get; init; } = MaxLength.None;

    /// <summary>
    ///     Adjective to the participles that may never follow it - the <c>incompatible</c> block of the
    ///     file. Empty when the theme declares none, which is the ordinary case.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         One way round on purpose: the adjective is the key, and a word declared in both sections
    ///         refuses nothing when it is drawn as a participle. The participle is drawn after the
    ///         adjective, from what the adjective leaves.
    ///     </para>
    ///     <para>
    ///         See decision record DEC0017 (in French):
    ///         https://github.com/Reefact/slugger/blob/main/docs/idr/DEC0017-refus-d-un-participe-a-cote-d-un-adjectif.md
    ///     </para>
    /// </remarks>
    public IReadOnlyDictionary<string, IReadOnlyList<string>> Incompatible { get; init; } =
        ReadOnlyDictionary<string, IReadOnlyList<string>>.Empty;

    /// <summary>
    ///     The <c>meta</c> block of the file - title, description, version, author - or
    ///     <see cref="ThemeMetadata.Empty" />. Descriptive only: generation never reads it.
    /// </summary>
    public ThemeMetadata Metadata { get; init; } = ThemeMetadata.Empty;

    /// <summary>Whether the theme declares any participle at all, in any category.</summary>
    public bool HasParticiples => Participles.Count > 0;

    /// <summary>Whether any adjective refuses a participle beside it.</summary>
    public bool HasIncompatibilities => Incompatible.Count > 0;

}