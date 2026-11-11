#region Usings declarations

using System.Collections.ObjectModel;

using Slugger.Domain.Errors;
using Slugger.Domain.Resolution;
using Slugger.Domain.Validation;

#endregion

namespace Slugger.Domain.Generation;

/// <summary>
///     Slugs from one theme or several, each drawn with options settled once: the theme is reduced to
///     what a length limit or a word cap leaves, checked against the floors of the mode it draws, and
///     then drawn from as often as you like, without paying either again.
/// </summary>
/// <remarks>
///     <para>
///         Where <see cref="SlugGenerator.Generate(SluggerTheme, GenerationOptions)" /> reduces the theme
///         on every call and checks nothing, a source does both once, in <see cref="Prepare(SluggerTheme, GenerationOptions, bool)" />:
///         a limit that leaves the theme too thin is refused there, with the reasons the command line
///         gives for the same options, and every slug after it costs what one without a limit costs.
///         Build it at startup and keep it.
///     </para>
///     <para>
///         It never changes once prepared and can be shared between threads. The random source is the
///         part to watch: <see cref="Next()" /> draws from <see cref="Random.Shared" />, which is
///         thread-safe, while a seeded <see cref="DefaultRandomSource" /> passed to
///         <see cref="Next(IRandomSource)" /> belongs to one thread.
///     </para>
///     <para>
///         See decision record DEC0031 (in French):
///         https://github.com/Reefact/slugger/blob/main/docs/idr/DEC0031-source-de-slugs-preparee-une-fois.md
///     </para>
/// </remarks>
public sealed class SlugSource {

    #region Static members

    private static readonly IRandomSource Shared = new DefaultRandomSource();

    /// <summary>How the library refuses a theme: by its name, with every reason.</summary>
    private static readonly Func<ThemeDocument, ThemeResolver, IReadOnlyList<DomainError>, DomainError> Refused =
        (theme, _, refusals) => ThemeErrors.Rejected(theme.Name, refusals);

    /// <summary>
    ///     Prepares a source over one theme, and throws if the options leave it unfit to draw from.
    /// </summary>
    /// <param name="theme">The theme to draw from.</param>
    /// <param name="options">How to draw and how to write the slug out, its limits included.</param>
    /// <param name="allowSmall">
    ///     Waives the size floors, as the theme's own <c>"allowSmall": true</c> does. A limit that leaves
    ///     no noun at all is refused anyway.
    /// </param>
    /// <exception cref="ArgumentNullException"><paramref name="theme" /> or <paramref name="options" /> is null.</exception>
    /// <exception cref="DomainException">
    ///     The options leave the theme short of a floor its mode holds it to. The message names the theme;
    ///     every reason is in the inner errors of <see cref="DiagnosableException.Error" />.
    /// </exception>
    public static SlugSource Prepare(SluggerTheme theme, GenerationOptions options, bool allowSmall = false) {
        return PrepareResult(theme, options, allowSmall).GetResultOrThrow();
    }

    /// <summary>
    ///     Prepares a source over several themes, each drawn with its own options - its own style, say,
    ///     through <see cref="GenerationOptions.WithDefaultsOf(SluggerTheme)" /> - and throws if the options leave one
    ///     of them unfit to draw from.
    /// </summary>
    /// <remarks>
    ///     Each theme is drawn with a probability proportional to the nouns it holds, as
    ///     <see cref="WeightedThemePicker" /> draws, so that every noun of every theme has the same chance.
    ///     List the themes in a stable order when a seeded source has to give the same slugs again.
    /// </remarks>
    /// <param name="themes">The themes to draw from, each once.</param>
    /// <param name="optionsFor">The options each theme is drawn with.</param>
    /// <param name="allowSmall">Waives the size floors of every theme, as for one.</param>
    /// <exception cref="ArgumentNullException">An argument is null, or <paramref name="optionsFor" /> returns null.</exception>
    /// <exception cref="ArgumentException"><paramref name="themes" /> is empty.</exception>
    /// <exception cref="DomainException">
    ///     The options leave a theme unfit to draw from. The first theme refused is the one named.
    /// </exception>
    public static SlugSource Prepare(IReadOnlyList<SluggerTheme> themes, Func<SluggerTheme, GenerationOptions> optionsFor, bool allowSmall = false) {
        return PrepareResult(themes, optionsFor, allowSmall).GetResultOrThrow();
    }

    /// <summary>Prepares a source over one theme, or says why the options leave it unfit to draw from.</summary>
    /// <inheritdoc cref="Prepare(SluggerTheme, GenerationOptions, bool)" />
    public static Outcome<SlugSource> PrepareResult(SluggerTheme theme, GenerationOptions options, bool allowSmall = false) {
        ArgumentNullException.ThrowIfNull(theme);
        ArgumentNullException.ThrowIfNull(options);

        return PrepareResult([theme], _ => options, allowSmall, Refused);
    }

    /// <summary>Prepares a source over several themes, or says why the options leave one unfit to draw from.</summary>
    /// <inheritdoc cref="Prepare(IReadOnlyList{SluggerTheme}, Func{SluggerTheme, GenerationOptions}, bool)" />
    public static Outcome<SlugSource> PrepareResult(IReadOnlyList<SluggerTheme>           themes,
                                                    Func<SluggerTheme, GenerationOptions> optionsFor,
                                                    bool                                  allowSmall = false) {
        return PrepareResult(themes, optionsFor, allowSmall, Refused);
    }

    /// <summary>
    ///     The preparation itself, with the refusal left to the caller: the command line names a theme
    ///     refused among others differently, since it was refused for a mode it does not draw alone.
    /// </summary>
    /// <param name="themes">The themes to draw from.</param>
    /// <param name="optionsFor">The options each theme is drawn with.</param>
    /// <param name="allowSmall">Waives the size floors.</param>
    /// <param name="refused">The error that refuses a theme, given its reduced surface and the reasons.</param>
    internal static Outcome<SlugSource> PrepareResult(IReadOnlyList<SluggerTheme>                                                 themes,
                                                      Func<SluggerTheme, GenerationOptions>                                       optionsFor,
                                                      bool                                                                        allowSmall,
                                                      Func<ThemeDocument, ThemeResolver, IReadOnlyList<DomainError>, DomainError> refused) {
        ArgumentNullException.ThrowIfNull(themes);
        ArgumentNullException.ThrowIfNull(optionsFor);
        if (themes.Count == 0) { throw new ArgumentException("At least one theme is needed to draw from.", nameof(themes)); }

        // A copy, so that a list the caller changes afterwards changes nothing here: the picker and
        // the drawings are built from the same themes, and stay so. Read-only, because the picker
        // hands it back through Themes, and an array there could be cast back and written to.
        ReadOnlyCollection<SluggerTheme>  snapshot = Array.AsReadOnly<SluggerTheme>([.. themes]);
        Dictionary<SluggerTheme, Drawing> drawings = new(ReferenceEqualityComparer.Instance);
        foreach (SluggerTheme theme in snapshot) {
            ArgumentNullException.ThrowIfNull(theme);
            if (drawings.ContainsKey(theme)) { continue; }

            GenerationOptions options  = optionsFor(theme) ?? throw new ArgumentNullException(nameof(optionsFor), $"No options for theme \"{theme.Name}\".");
            ThemeResolver     resolver = SlugGenerator.ResolverFor(theme.Content, options);

            if (NeedsChecking(theme.Content, resolver)) {
                IReadOnlyList<DomainError> refusals = ThemeValidator.Validate(resolver, allowSmall);
                if (refusals.Count > 0) { return Outcome<SlugSource>.Failure(refused(theme.Content, resolver, refusals)); }
            }

            drawings[theme] = new Drawing(options, resolver);
        }

        return Outcome<SlugSource>.Success(new SlugSource(new WeightedThemePicker(snapshot), drawings));
    }

    /// <summary>
    ///     Whether the surface drawn is not one a load accepted: the theme was built in memory, where
    ///     nothing checked it; a limit narrowed it (DEC0018, DEC0023); or the mode drawn holds it to other
    ///     floors than its own (DEC0016). Otherwise the load already checked exactly this, and checking
    ///     it again would cost a theme of thousands of words for nothing.
    /// </summary>
    private static bool NeedsChecking(ThemeDocument theme, ThemeResolver resolver) {
        if (!theme.AcceptedAtLoad) { return true; }
        if (resolver.Narrows) { return true; }

        return ThemeValidator.DrawnMode(resolver) != ThemeValidator.DrawnMode(theme);
    }

    #endregion

    #region Fields

    private readonly IReadOnlyDictionary<SluggerTheme, Drawing> _drawings;
    private readonly WeightedThemePicker                        _picker;

    #endregion

    #region Constructors & Destructor

    private SlugSource(WeightedThemePicker picker, IReadOnlyDictionary<SluggerTheme, Drawing> drawings) {
        _picker   = picker;
        _drawings = drawings;
    }

    #endregion

    /// <summary>The themes it draws from, in the order given.</summary>
    public IReadOnlyList<SluggerTheme> Themes => _picker.Themes;

    /// <summary>The options a theme of this source is drawn with.</summary>
    /// <param name="theme">One of <see cref="Themes" />.</param>
    /// <exception cref="ArgumentNullException"><paramref name="theme" /> is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="theme" /> is not one of <see cref="Themes" />.</exception>
    public GenerationOptions OptionsFor(SluggerTheme theme) {
        ArgumentNullException.ThrowIfNull(theme);
        if (!_drawings.TryGetValue(theme, out Drawing? drawing)) { throw new ArgumentException($"Theme \"{theme.Name}\" is not one this source draws from.", nameof(theme)); }

        return drawing.Options;
    }

    /// <summary>The next slug, drawn from <see cref="Random.Shared" />: thread-safe, and different on every run.</summary>
    public string Next() {
        return Next(Shared);
    }

    /// <summary>
    ///     The next slug, every draw - the theme's included - taken from <paramref name="random" />, so a
    ///     seeded source gives the same slugs again.
    /// </summary>
    /// <param name="random">Where every draw comes from; a seeded one belongs to one thread.</param>
    /// <exception cref="ArgumentNullException"><paramref name="random" /> is null.</exception>
    public string Next(IRandomSource random) {
        ArgumentNullException.ThrowIfNull(random);

        SluggerTheme theme   = _picker.Pick(random);
        Drawing       drawing = _drawings[theme];

        return SlugGenerator.Generate(drawing.Resolver, drawing.Options, random);
    }

    #region Nested types

    /// <summary>What a theme draws with, settled once.</summary>
    /// <param name="Options">The options it is drawn with.</param>
    /// <param name="Resolver">Its surface, narrowed to the limits in the options.</param>
    private sealed record Drawing(GenerationOptions Options, ThemeResolver Resolver);

    #endregion

}
