namespace Slugger.Domain;

/// <summary>
///     A theme, as the library hands it out: <see cref="Themes" /> loads one, and every generation call
///     takes one.
/// </summary>
/// <remarks>
///     <para>
///         It is opaque on purpose. What it says about itself - its name, its metadata, its style, its
///         length promise - can be read; the words it draws from cannot. Only the library makes one, and
///         only once the theme has passed every rule, so a <see cref="SluggerTheme" /> in hand is always
///         a theme the engine accepted.
///     </para>
///     <para>
///         It never changes after it is loaded, so one instance can be shared by every thread of an
///         application. Load it once and keep it: loading reads and validates the whole file again.
///     </para>
///     <para>
///         Each theme is a closed world. Two themes may use the same category name with no relationship
///         between them, and a draw never crosses from one theme into another.
///     </para>
///     <para>
///         See decision record DEC0034 (in French):
///         https://github.com/Reefact/slugger/blob/main/docs/idr/DEC0034-theme-public-opaque.md
///     </para>
/// </remarks>
public sealed class SluggerTheme {

    #region Constructors & Destructor

    internal SluggerTheme(ThemeDocument content) {
        ArgumentNullException.ThrowIfNull(content);

        Content = content;
    }

    #endregion

    /// <summary>
    ///     The theme's name. A loaded theme is named after its file, without the extension, and never by
    ///     a field inside the JSON; a theme loaded from a string takes the name the caller gave.
    /// </summary>
    public string Name => Content.Name;

    /// <summary>
    ///     The <c>meta</c> block of the file - title, description, version, author - or
    ///     <see cref="ThemeMetadata.Empty" />. Descriptive only: generation never reads it.
    /// </summary>
    public ThemeMetadata Metadata => Content.Metadata;

    /// <summary>
    ///     The theme's own style - the <c>defaults</c> block of its file - or <see cref="ThemeDefaults.Empty" />
    ///     when it states none.
    /// </summary>
    /// <remarks>
    ///     Generation never applies it on its own. Pass <c>GenerationOptions.Default.WithDefaultsOf(theme)</c>
    ///     to draw in the theme's style: see <see cref="GenerationOptions.WithDefaultsOf(SluggerTheme)" />.
    /// </remarks>
    public ThemeDefaults Defaults => Content.Defaults;

    /// <summary>
    ///     The longest slug the theme promises to produce, one figure per shape - the <c>maxLength</c>
    ///     block of its file - or <see cref="Domain.MaxLength.None" /> when it promises nothing.
    /// </summary>
    /// <remarks>
    ///     The promise was checked when the theme was loaded, measured with the theme's own style. It is
    ///     not a generation option: to limit the length of your slugs, set
    ///     <see cref="GenerationOptions.MaxLength" />.
    /// </remarks>
    public MaxLength MaxLength => Content.MaxLength;

    /// <summary>The theme as its file states it, which the engine draws from.</summary>
    internal ThemeDocument Content { get; }

    /// <inheritdoc />
    public override string ToString() {
        return Name;
    }

}
