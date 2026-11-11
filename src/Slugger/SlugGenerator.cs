#region Usings declarations

using System.Collections.ObjectModel;

using Slugger.Domain;
using Slugger.Domain.Generation;
using Slugger.Domain.Resolution;
using Slugger.Domain.Validation;
using Slugger.Errors;

#endregion

namespace Slugger;

/// <summary>
///     Slugs from one theme or several, drawn in each theme's own style: the theme is reduced once to
///     what the options leave, checked against the floors of the mode it draws, and then drawn from as
///     often as you like, without paying either again.
///     <code>
/// using Slugger;
///
/// SluggerTheme  docker    = SluggerTheme.LoadEmbeddedOrThrow("docker");
/// SlugGenerator generator = SlugGenerator.ForOrThrow(docker);
/// string        slug      = generator.Next();   // for example "vigorous_herschel"
/// </code>
/// </summary>
/// <remarks>
///     <para>
///         The theme's own style - the <c>defaults</c> block of its file - is applied first; the
///         adjustment you pass, if any, starts from it. A limit that leaves the theme too thin is refused
///         when the generator is built, with the reasons the command line gives for the same options, and
///         every slug after it costs what one without a limit costs. Build it at startup and keep it.
///     </para>
///     <para>
///         <see cref="For(SluggerTheme, Func{GenerationOptions, GenerationOptions}, bool)" /> reports a
///         refusal in an <see cref="Outcome{T}" />; <see cref="ForOrThrow(SluggerTheme, Func{GenerationOptions, GenerationOptions}, bool)" />
///         raises it as a <see cref="SluggerThemeException" />. Once built, a generator always draws.
///     </para>
///     <para>
///         It never changes once built and can be shared between threads. The random source is the part
///         to watch: <see cref="Next()" /> draws from <see cref="Random.Shared" />, which is thread-safe,
///         while a seeded <see cref="DefaultRandomSource" /> passed to <see cref="Next(IRandomSource)" />
///         belongs to one thread.
///     </para>
///     <para>
///         See decision records DEC0031 and DEC0036 (in French):
///         https://github.com/Reefact/slugger/blob/main/docs/idr/DEC0031-source-de-slugs-preparee-une-fois.md
///         https://github.com/Reefact/slugger/blob/main/docs/idr/DEC0036-api-publique-de-la-bibliotheque.md
///     </para>
/// </remarks>
public sealed class SlugGenerator {

    #region Static members

    private static readonly IRandomSource Shared = new DefaultRandomSource();

    /// <summary>How the library refuses a theme: by its name, with every reason.</summary>
    private static readonly Func<ThemeDocument, ThemeResolver, IReadOnlyList<SluggerThemeError>, SluggerThemeError> Refused =
        (theme, _, refusals) => SluggerThemeError.Rejected(theme.Name, refusals);

    /// <summary>
    ///     Builds a generator over one theme, drawn in its own style, or says why the options leave the
    ///     theme unfit to draw from.
    /// </summary>
    /// <param name="theme">The theme to draw from.</param>
    /// <param name="adjust">
    ///     Changes the theme's style before drawing - <c>style =&gt; style with { MaxLength = 30 }</c>.
    ///     It receives the theme's own style; return <see cref="GenerationOptions.Default" /> with your
    ///     changes to ignore it. Left out, the theme draws exactly as it describes itself.
    /// </param>
    /// <param name="allowSmall">
    ///     Waives the size floors, as the theme's own <c>"allowSmall": true</c> does. A limit that leaves
    ///     no noun at all is refused anyway.
    /// </param>
    /// <returns>
    ///     The generator, or a failure whose <see cref="SluggerThemeError" /> has the code
    ///     <see cref="SluggerThemeError.Codes.Rejected" /> and lists every reason in its inner errors.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="theme" /> is null, or <paramref name="adjust" /> returns null.</exception>
    public static Outcome<SlugGenerator> For(SluggerTheme                                theme,
                                             Func<GenerationOptions, GenerationOptions>? adjust     = null,
                                             bool                                        allowSmall = false) {
        ArgumentNullException.ThrowIfNull(theme);

        return For([theme], adjust, allowSmall);
    }

    /// <summary>
    ///     Builds a generator over several themes, each drawn in its own style with the same adjustment,
    ///     or says why the options leave one of them unfit to draw from.
    /// </summary>
    /// <remarks>
    ///     Each theme is drawn with a probability proportional to the nouns it holds, so that every noun
    ///     of every theme has the same chance. List the themes in a stable order when a seeded source has
    ///     to give the same slugs again.
    /// </remarks>
    /// <param name="themes">The themes to draw from, each once.</param>
    /// <param name="adjust">Changes each theme's style before drawing, as for one theme.</param>
    /// <param name="allowSmall">Waives the size floors of every theme, as for one.</param>
    /// <returns>The generator, or a failure naming the first theme refused, with every reason.</returns>
    /// <exception cref="ArgumentNullException">An argument or a theme is null, or <paramref name="adjust" /> returns null.</exception>
    /// <exception cref="ArgumentException"><paramref name="themes" /> is empty.</exception>
    public static Outcome<SlugGenerator> For(IReadOnlyList<SluggerTheme>                 themes,
                                             Func<GenerationOptions, GenerationOptions>? adjust     = null,
                                             bool                                        allowSmall = false) {
        return Prepare(themes, theme => StyleOf(theme, adjust), allowSmall);
    }

    /// <summary>Builds a generator over one theme, drawn in its own style, and throws if it is refused.</summary>
    /// <inheritdoc cref="For(SluggerTheme, Func{GenerationOptions, GenerationOptions}, bool)" />
    /// <exception cref="SluggerThemeException">
    ///     The options leave the theme short of a floor its mode holds it to. The message names the theme;
    ///     every reason is in the inner errors of <see cref="DiagnosableException.Error" />.
    /// </exception>
    public static SlugGenerator ForOrThrow(SluggerTheme                                theme,
                                           Func<GenerationOptions, GenerationOptions>? adjust     = null,
                                           bool                                        allowSmall = false) {
        return For(theme, adjust, allowSmall).GetResultOrThrow();
    }

    /// <summary>Builds a generator over several themes, each in its own style, and throws if one is refused.</summary>
    /// <inheritdoc cref="For(IReadOnlyList{SluggerTheme}, Func{GenerationOptions, GenerationOptions}, bool)" />
    /// <exception cref="SluggerThemeException">
    ///     The options leave a theme unfit to draw from. The first theme refused is the one named.
    /// </exception>
    public static SlugGenerator ForOrThrow(IReadOnlyList<SluggerTheme>                 themes,
                                           Func<GenerationOptions, GenerationOptions>? adjust     = null,
                                           bool                                        allowSmall = false) {
        return For(themes, adjust, allowSmall).GetResultOrThrow();
    }

    /// <summary>Builds a generator over one theme drawn with exactly these options, its own style not applied.</summary>
    internal static Outcome<SlugGenerator> Prepare(SluggerTheme theme, GenerationOptions options, bool allowSmall = false) {
        ArgumentNullException.ThrowIfNull(theme);
        ArgumentNullException.ThrowIfNull(options);

        return Prepare([theme], _ => options, allowSmall, Refused);
    }

    /// <summary>Builds a generator over several themes, each drawn with the options given for it.</summary>
    internal static Outcome<SlugGenerator> Prepare(IReadOnlyList<SluggerTheme>           themes,
                                                   Func<SluggerTheme, GenerationOptions> optionsFor,
                                                   bool                                  allowSmall = false) {
        return Prepare(themes, optionsFor, allowSmall, Refused);
    }

    /// <summary>
    ///     The preparation itself, with the refusal left to the caller: the command line names a theme
    ///     refused among others differently, since it was refused for a mode it does not draw alone.
    /// </summary>
    /// <param name="themes">The themes to draw from.</param>
    /// <param name="optionsFor">The options each theme is drawn with.</param>
    /// <param name="allowSmall">Waives the size floors.</param>
    /// <param name="refused">The error that refuses a theme, given its reduced surface and the reasons.</param>
    internal static Outcome<SlugGenerator> Prepare(IReadOnlyList<SluggerTheme>                                                            themes,
                                                   Func<SluggerTheme, GenerationOptions>                                                  optionsFor,
                                                   bool                                                                                   allowSmall,
                                                   Func<ThemeDocument, ThemeResolver, IReadOnlyList<SluggerThemeError>, SluggerThemeError> refused) {
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
            ThemeResolver     resolver = SlugDrawer.ResolverFor(theme.Content, options);

            if (NeedsChecking(theme.Content, resolver)) {
                IReadOnlyList<SluggerThemeError> refusals = ThemeValidator.Validate(resolver, allowSmall);
                if (refusals.Count > 0) { return Outcome<SlugGenerator>.Failure(refused(theme.Content, resolver, refusals)); }
            }

            drawings[theme] = new Drawing(options, resolver);
        }

        return Outcome<SlugGenerator>.Success(new SlugGenerator(new WeightedThemePicker(snapshot), drawings));
    }

    /// <summary>Builds a generator over one theme drawn with exactly these options, and throws if it is refused.</summary>
    internal static SlugGenerator PrepareOrThrow(SluggerTheme theme, GenerationOptions options, bool allowSmall = false) {
        return Prepare(theme, options, allowSmall).GetResultOrThrow();
    }

    /// <summary>Builds a generator over several themes, each drawn with the options given for it, and throws if one is refused.</summary>
    internal static SlugGenerator PrepareOrThrow(IReadOnlyList<SluggerTheme>           themes,
                                                 Func<SluggerTheme, GenerationOptions> optionsFor,
                                                 bool                                  allowSmall = false) {
        return Prepare(themes, optionsFor, allowSmall).GetResultOrThrow();
    }

    /// <summary>The theme's own style, then the caller's adjustment of it.</summary>
    private static GenerationOptions StyleOf(SluggerTheme theme, Func<GenerationOptions, GenerationOptions>? adjust) {
        GenerationOptions style = GenerationOptions.Default.WithStyle(theme.Defaults);
        if (adjust is null) { return style; }

        return adjust(style) ?? throw new ArgumentNullException(nameof(adjust), $"No options for theme \"{theme.Name}\".");
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

    private readonly Lazy<long>                                 _combinations;
    private readonly IReadOnlyDictionary<SluggerTheme, Drawing> _drawings;
    private readonly WeightedThemePicker                        _picker;

    #endregion

    #region Constructors & Destructor

    private SlugGenerator(WeightedThemePicker picker, IReadOnlyDictionary<SluggerTheme, Drawing> drawings) {
        _picker       = picker;
        _drawings     = drawings;
        _combinations = new Lazy<long>(CountCombinations);
    }

    #endregion

    /// <summary>
    ///     How many distinct slugs this generator can draw, token aside: the combinations each theme
    ///     leaves under its options, in the mode it draws, added up.
    /// </summary>
    /// <remarks>
    ///     It counts combinations, not strings: an incompatible pair is not subtracted, and two
    ///     combinations that happen to write the same string count twice. Read it as an order of
    ///     magnitude. A token multiplies it by the number of values the token can take. Counted once, on
    ///     first reading.
    /// </remarks>
    public long Combinations => _combinations.Value;

    /// <summary>The themes it draws from, in the order given.</summary>
    internal IReadOnlyList<SluggerTheme> Themes => _picker.Themes;

    /// <summary>The options a theme of this generator is drawn with.</summary>
    /// <param name="theme">One of <see cref="Themes" />.</param>
    /// <exception cref="ArgumentNullException"><paramref name="theme" /> is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="theme" /> is not one of <see cref="Themes" />.</exception>
    internal GenerationOptions OptionsFor(SluggerTheme theme) {
        ArgumentNullException.ThrowIfNull(theme);
        if (!_drawings.TryGetValue(theme, out Drawing? drawing)) { throw new ArgumentException($"Theme \"{theme.Name}\" is not one this generator draws from.", nameof(theme)); }

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
        Drawing      drawing = _drawings[theme];

        return SlugDrawer.Generate(drawing.Resolver, drawing.Options, random);
    }

    private long CountCombinations() {
        return _drawings.Values.Sum(drawing => new ThemeCombinatorics(drawing.Resolver).Total(drawing.Options.SegmentMode));
    }

    #region Nested types

    /// <summary>What a theme draws with, settled once.</summary>
    /// <param name="Options">The options it is drawn with.</param>
    /// <param name="Resolver">Its surface, narrowed to the limits in the options.</param>
    private sealed record Drawing(GenerationOptions Options, ThemeResolver Resolver);

    #endregion

}
