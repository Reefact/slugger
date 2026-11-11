#region Usings declarations

using Slugger.Domain;
using Slugger.Domain.Analysis;
using Slugger.Domain.Generation;
using Slugger.Errors;
using Slugger.Infrastructure.Serialization;
using Slugger.Infrastructure.ThemeCatalogs;

#endregion

namespace Slugger;

/// <summary>
///     A theme, as the library hands it out: loaded here - one compiled into the library, a theme file
///     or a theme held in a string - and drawn from by a <see cref="SlugGenerator" />.
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
///         Each way of loading comes in two forms. <c>Load*</c> never throws for a refused theme: it
///         returns an <see cref="Outcome{T}" /> whose <see cref="SluggerThemeError" /> has the code
///         <see cref="SluggerThemeError.Codes.Rejected" /> and lists <b>every</b> reason in
///         <see cref="Error.InnerErrors" />, each with its own code. <c>Load*OrThrow</c> returns the theme
///         or throws a <see cref="SluggerThemeException" /> holding that same error; its message only names
///         the theme, so read the reasons from <see cref="DiagnosableException.Error" />. See decision
///         records DEC0006 and DEC0036 (in French).
///     </para>
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
///         See decision records DEC0034 and DEC0036 (in French):
///         https://github.com/Reefact/slugger/blob/main/docs/idr/DEC0034-theme-public-opaque.md
///         https://github.com/Reefact/slugger/blob/main/docs/idr/DEC0036-api-publique-de-la-bibliotheque.md
///     </para>
/// </remarks>
public sealed class SluggerTheme {

    #region Static members

    /// <summary>The names of the themes compiled into the library, each one <see cref="LoadEmbedded" /> accepts.</summary>
    public static IReadOnlyList<string> EmbeddedNames => new EmbeddedThemeCatalog().ListNames();

    /// <summary>Loads one of the themes compiled into the library, and reports every reason it is refused.</summary>
    /// <param name="name">The theme's name, case-sensitive: one of <see cref="EmbeddedNames" />.</param>
    /// <param name="allowSmall">
    ///     True to waive the size floors for this load: the minimum number of nouns, of words each noun
    ///     reaches and of combinations per category. The rules about a broken file still apply.
    /// </param>
    /// <returns>The theme, or a failure whose error lists every reason in its inner errors.</returns>
    /// <remarks>
    ///     A name that matches no embedded theme is reported with the code <see cref="SluggerThemeError.Codes.NotFound" />,
    ///     and the message lists the names that do.
    /// </remarks>
    /// <exception cref="ArgumentException"><paramref name="name" /> is null, empty or white space.</exception>
    public static Outcome<SluggerTheme> LoadEmbedded(string name, bool allowSmall = false) {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        using Stream? stream = EmbeddedThemeCatalog.OpenStream(name);
        if (stream is null) { return Refuse(name, [SluggerThemeError.NotBuiltIn(name, EmbeddedNames)]); }

        return LoadFromJson(ReadToEnd(stream), name, allowSmall);
    }

    /// <summary>Loads a theme file, and reports every reason it is refused.</summary>
    /// <param name="path">The file to read. The theme is named after the file, without its extension.</param>
    /// <param name="allowSmall">True to waive the size floors for this load, as for <see cref="LoadEmbedded" />.</param>
    /// <returns>The theme, or a failure whose error lists every reason in its inner errors.</returns>
    /// <remarks>
    ///     A file that does not exist is reported with the code <see cref="SluggerThemeError.Codes.NotFound" />.
    ///     An error while reading a file that does exist, such as access being denied, propagates unchanged.
    /// </remarks>
    /// <exception cref="ArgumentException"><paramref name="path" /> is null, empty or white space.</exception>
    public static Outcome<SluggerTheme> LoadFromFile(string path, bool allowSmall = false) {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        string name = NameOf(path);
        if (!File.Exists(path)) { return Refuse(name, [SluggerThemeError.NoSuchFile(path)]); }

        return LoadFromJson(File.ReadAllText(path), name, allowSmall);
    }

    /// <summary>
    ///     Loads a theme from its JSON text, and reports every reason it is refused. This is also how to
    ///     build a theme in memory: write it as a file would hold it.
    /// </summary>
    /// <param name="json">The theme, as a theme file would hold it.</param>
    /// <param name="name">What to call the theme, since raw JSON has no file name to take it from.</param>
    /// <param name="allowSmall">True to waive the size floors for this load, as for <see cref="LoadEmbedded" />.</param>
    /// <returns>The theme, or a failure whose error lists every reason in its inner errors.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="json" /> is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="name" /> is null, empty or white space.</exception>
    public static Outcome<SluggerTheme> LoadFromJson(string json, string name = "inline", bool allowSmall = false) {
        ArgumentNullException.ThrowIfNull(json);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        Outcome<ThemeDocument> loaded = ThemeLoader.Load(name, json, allowSmall);
        if (loaded.Error is { } refused) { return Outcome<SluggerTheme>.Failure(refused); }

        ThemeDocument accepted = loaded.GetResultOrThrow();

        return Outcome<SluggerTheme>.Success(new SluggerTheme(accepted));
    }

    /// <summary>
    ///     Loads a theme from a stream holding its JSON - a resource embedded in your own assembly, say -
    ///     and reports every reason it is refused. The stream is read to its end and left open.
    /// </summary>
    /// <param name="stream">The theme, as a theme file would hold it, in UTF-8.</param>
    /// <param name="name">What to call the theme, since a stream has no file name to take it from.</param>
    /// <param name="allowSmall">True to waive the size floors for this load, as for <see cref="LoadEmbedded" />.</param>
    /// <returns>The theme, or a failure whose error lists every reason in its inner errors.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="stream" /> is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="name" /> is null, empty or white space.</exception>
    public static Outcome<SluggerTheme> LoadFromStream(Stream stream, string name, bool allowSmall = false) {
        ArgumentNullException.ThrowIfNull(stream);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        return LoadFromJson(ReadToEnd(stream), name, allowSmall);
    }

    /// <summary>
    ///     Measures one of the themes compiled into the library: its margins on every rule, what it would
    ///     be refused for, if anything, and what is worth a second look. Never throws for the theme's sake.
    /// </summary>
    /// <param name="name">The theme's name, case-sensitive: one of <see cref="EmbeddedNames" />.</param>
    /// <param name="allowSmall">True to judge the theme with its size floors waived; the margins still compare it with them.</param>
    /// <returns>The analysis; a name that matches no embedded theme is reported in its refusals, unmeasured.</returns>
    /// <exception cref="ArgumentException"><paramref name="name" /> is null, empty or white space.</exception>
    public static ThemeAnalysis AnalyzeEmbedded(string name, bool allowSmall = false) {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        using Stream? stream = EmbeddedThemeCatalog.OpenStream(name);
        if (stream is null) { return Unmeasured(name, SluggerThemeError.NotBuiltIn(name, EmbeddedNames)); }

        return AnalyzeFromJson(ReadToEnd(stream), name, allowSmall);
    }

    /// <summary>Measures a theme file, whether or not it would load. Never throws for the theme's sake.</summary>
    /// <param name="path">The file to read. The theme is named after the file, without its extension.</param>
    /// <param name="allowSmall">True to judge the theme with its size floors waived; the margins still compare it with them.</param>
    /// <returns>The analysis; a file that does not exist is reported in its refusals, unmeasured.</returns>
    /// <remarks>An error while reading a file that does exist, such as access being denied, propagates unchanged.</remarks>
    /// <exception cref="ArgumentException"><paramref name="path" /> is null, empty or white space.</exception>
    public static ThemeAnalysis AnalyzeFromFile(string path, bool allowSmall = false) {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        string name = NameOf(path);
        if (!File.Exists(path)) { return Unmeasured(name, SluggerThemeError.NoSuchFile(path)); }

        return AnalyzeFromJson(File.ReadAllText(path), name, allowSmall);
    }

    /// <summary>
    ///     Measures a theme from its JSON text, whether or not it would load - the theme an author is still
    ///     writing, say. Never throws for the theme's sake.
    /// </summary>
    /// <param name="json">The theme, as a theme file would hold it.</param>
    /// <param name="name">What to call the theme, since raw JSON has no file name to take it from.</param>
    /// <param name="allowSmall">True to judge the theme with its size floors waived; the margins still compare it with them.</param>
    /// <returns>
    ///     The analysis. A theme that can be read is measured even when it is refused; one that cannot -
    ///     not JSON, a section of the wrong shape, no noun - carries its refusals and no measurements.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="json" /> is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="name" /> is null, empty or white space.</exception>
    public static ThemeAnalysis AnalyzeFromJson(string json, string name = "inline", bool allowSmall = false) {
        ArgumentNullException.ThrowIfNull(json);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        Outcome<ThemeDocument> loaded = ThemeLoader.Load(name, json, allowSmall: true);
        if (loaded.Error is not { } refused) { return Measured(loaded.GetResultOrThrow(), allowSmall); }
        if (Measurable(name, json) is { } incoherent) { return Measured(incoherent, allowSmall); }

        return new ThemeAnalysis(ThemeAnalyzer.Unmeasured(name, refused.InnerErrors));
    }

    /// <summary>
    ///     Measures a theme from a stream holding its JSON, whether or not it would load. The stream is
    ///     read to its end and left open. Never throws for the theme's sake.
    /// </summary>
    /// <param name="stream">The theme, as a theme file would hold it, in UTF-8.</param>
    /// <param name="name">What to call the theme, since a stream has no file name to take it from.</param>
    /// <param name="allowSmall">True to judge the theme with its size floors waived; the margins still compare it with them.</param>
    /// <exception cref="ArgumentNullException"><paramref name="stream" /> is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="name" /> is null, empty or white space.</exception>
    public static ThemeAnalysis AnalyzeFromStream(Stream stream, string name, bool allowSmall = false) {
        ArgumentNullException.ThrowIfNull(stream);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        return AnalyzeFromJson(ReadToEnd(stream), name, allowSmall);
    }

    /// <summary>Loads one of the themes compiled into the library, and throws if it is refused.</summary>
    /// <inheritdoc cref="LoadEmbedded" />
    /// <exception cref="SluggerThemeException">
    ///     The theme was refused. The message only names the theme; every reason is in the inner errors
    ///     of <see cref="DiagnosableException.Error" />.
    /// </exception>
    public static SluggerTheme LoadEmbeddedOrThrow(string name, bool allowSmall = false) {
        return LoadEmbedded(name, allowSmall).GetResultOrThrow();
    }

    /// <summary>Loads a theme file, and throws if it is refused or does not exist.</summary>
    /// <inheritdoc cref="LoadFromFile" />
    /// <exception cref="SluggerThemeException">
    ///     The theme was refused, or the file does not exist. The message only names the theme; every
    ///     reason is in the inner errors of <see cref="DiagnosableException.Error" />.
    /// </exception>
    public static SluggerTheme LoadFromFileOrThrow(string path, bool allowSmall = false) {
        return LoadFromFile(path, allowSmall).GetResultOrThrow();
    }

    /// <summary>Loads a theme from its JSON text, and throws if it is refused.</summary>
    /// <inheritdoc cref="LoadFromJson" />
    /// <exception cref="SluggerThemeException">
    ///     The theme was refused. The message only names the theme; every reason is in the inner errors
    ///     of <see cref="DiagnosableException.Error" />.
    /// </exception>
    public static SluggerTheme LoadFromJsonOrThrow(string json, string name = "inline", bool allowSmall = false) {
        return LoadFromJson(json, name, allowSmall).GetResultOrThrow();
    }

    /// <summary>Loads a theme from a stream holding its JSON, and throws if it is refused.</summary>
    /// <inheritdoc cref="LoadFromStream" />
    /// <exception cref="SluggerThemeException">
    ///     The theme was refused. The message only names the theme; every reason is in the inner errors
    ///     of <see cref="DiagnosableException.Error" />.
    /// </exception>
    public static SluggerTheme LoadFromStreamOrThrow(Stream stream, string name, bool allowSmall = false) {
        return LoadFromStream(stream, name, allowSmall).GetResultOrThrow();
    }

    /// <summary>
    ///     A theme that does not load but can still be read - well-formed, with nouns - is measured as it
    ///     is, so that an author sees by how much it misses; anything less leaves nothing to measure.
    /// </summary>
    private static ThemeDocument? Measurable(string name, string json) {
        ThemeParseResult parsed = new JsonThemeSerializer().Deserialize(name, json);
        if (parsed.Document is not { } theme) { return null; }
        if (parsed.ShapeErrors.Count > 0) { return null; }

        return theme.Nouns.Count > 0 ? theme : null;
    }

    /// <summary>The theme measured in its own style, as a load holds it to its floors.</summary>
    private static ThemeAnalysis Measured(ThemeDocument theme, bool allowSmall) {
        GenerationOptions style = GenerationOptions.Default.WithStyle(theme.Defaults);

        return new ThemeAnalysis(ThemeAnalyzer.Analyze(SlugDrawer.ResolverFor(theme, style), style, allowSmall));
    }

    private static ThemeAnalysis Unmeasured(string name, SluggerThemeError reason) {
        return new ThemeAnalysis(ThemeAnalyzer.Unmeasured(name, [reason]));
    }

    private static string NameOf(string path) {
        string name = Path.GetFileNameWithoutExtension(path.AsSpan()).ToString();

        return name.Length == 0 ? "(unnamed)" : name;
    }

    private static string ReadToEnd(Stream stream) {
        using StreamReader reader = new(stream, leaveOpen: true);

        return reader.ReadToEnd();
    }

    private static Outcome<SluggerTheme> Refuse(string name, IReadOnlyList<SluggerThemeError> reasons) {
        return Outcome<SluggerTheme>.Failure(SluggerThemeError.Rejected(name, reasons));
    }

    #endregion

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
    ///     A <see cref="SlugGenerator" /> draws in it, and the adjustment you pass starts from it.
    /// </remarks>
    public ThemeDefaults Defaults => Content.Defaults;

    /// <summary>
    ///     The longest slug the theme promises to produce, one figure per shape - the <c>maxLength</c>
    ///     block of its file - or <see cref="Slugger.MaxLength.None" /> when it promises nothing.
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
