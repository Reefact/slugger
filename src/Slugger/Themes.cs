#region Usings declarations

using FirstClassErrors;

using Slugger.Domain;
using Slugger.Domain.Validation;
using Slugger.Infrastructure.Serialization;
using Slugger.Infrastructure.ThemeCatalogs;

#endregion

namespace Slugger;

/// <summary>
///     Loads a theme - one of those compiled into the library, a theme file or a theme held in a
///     string - and returns the <see cref="ThemeDocument" /> every generation call takes:
///     <code>
/// using Slugger;
/// using Slugger.Domain;
/// using Slugger.Domain.Generation;
///
/// ThemeDocument     docker = Themes.LoadEmbedded("docker");
/// GenerationOptions style  = GenerationOptions.Default.WithDefaultsOf(docker);
/// string            slug   = SlugGenerator.Generate(docker, style);   // for example "vigorous_herschel"
/// </code>
/// </summary>
/// <remarks>
///     <para>
///         Every call reads, normalises and validates the theme again: nothing is cached. Load a theme
///         once and keep the <see cref="ThemeDocument" />. It never changes after loading, so one
///         instance can serve every thread of an application.
///     </para>
///     <para>
///         Each entry point comes in two shapes. <c>Load*Result</c> never throws for a refused theme: it
///         returns an <see cref="Outcome{T}" /> whose error has the code
///         <see cref="ThemeErrors.Codes.Rejected" /> and lists <b>every</b> reason in
///         <see cref="Error.InnerErrors" />, each with its own code. The other shape returns the theme or
///         throws a <see cref="DomainException" /> holding that same error; its message only names the
///         theme, so read the reasons from <see cref="DiagnosableException.Error" />.
///     </para>
///     <para>
///         These methods live here rather than on <see cref="ThemeDocument" /> because loading means JSON
///         and the file system, which the types of <c>Slugger.Domain</c> stay free of.
///     </para>
///     <para>
///         See decision record DEC0006 (in French):
///         https://github.com/Reefact/slugger/blob/main/docs/idr/DEC0006-rapport-groupe-des-refus.md
///     </para>
/// </remarks>
public static class Themes {

    #region Static members

    /// <summary>
    ///     Loads one of the themes compiled into the library, and reports every reason it is refused
    ///     rather than throwing.
    /// </summary>
    /// <param name="name">The theme's name, case-sensitive: one of those <see cref="ListEmbedded" /> returns.</param>
    /// <param name="allowSmall">
    ///     True to waive the size floors for this load: the minimum number of nouns, of words each noun
    ///     reaches and of combinations per category. The rules about a broken file still apply.
    /// </param>
    /// <returns>The theme, or a failure whose error lists every reason in its inner errors.</returns>
    /// <remarks>
    ///     A name that matches no embedded theme is reported, in the current version, with the code
    ///     <see cref="ThemeErrors.Codes.MalformedSection" /> rather than <see cref="ThemeErrors.Codes.NotFound" />.
    /// </remarks>
    /// <exception cref="ArgumentException"><paramref name="name" /> is null, empty or white space.</exception>
    public static Outcome<ThemeDocument> LoadEmbeddedResult(string name, bool allowSmall = false) {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        using Stream? stream = EmbeddedThemeCatalog.OpenStream(name);
        if (stream is null) { return Refuse(name, [ThemeErrors.MalformedSection("(file)", $"a theme embedded in the library; there is none called \"{name}\"")]); }

        using StreamReader reader = new(stream);

        return LoadFromJsonResult(reader.ReadToEnd(), name, allowSmall);
    }

    /// <summary>
    ///     Loads a theme file, and reports every reason it is refused rather than throwing.
    /// </summary>
    /// <param name="path">The file to read. The theme is named after the file, without its extension.</param>
    /// <param name="allowSmall">
    ///     True to waive the size floors for this load: the minimum number of nouns, of words each noun
    ///     reaches and of combinations per category. The rules about a broken file still apply.
    /// </param>
    /// <returns>The theme, or a failure whose error lists every reason in its inner errors.</returns>
    /// <remarks>
    ///     A file that does not exist is reported with the code <see cref="ThemeErrors.Codes.MalformedSection" />.
    ///     An error while reading a file that does exist, such as a denied access, is thrown as it comes.
    /// </remarks>
    /// <exception cref="ArgumentException"><paramref name="path" /> is null, empty or white space.</exception>
    public static Outcome<ThemeDocument> LoadFromFileResult(string path, bool allowSmall = false) {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        string name = Path.GetFileNameWithoutExtension(path.AsSpan()).ToString();
        if (name.Length == 0) {
            name = "(unnamed)";
        }

        if (!File.Exists(path)) { return Refuse(name, [ThemeErrors.MalformedSection("(file)", $"a readable file; \"{path}\" does not exist")]); }

        return LoadFromJsonResult(File.ReadAllText(path), name, allowSmall);
    }

    /// <summary>
    ///     Loads a theme from its JSON text, and reports every reason it is refused rather than throwing.
    /// </summary>
    /// <param name="json">The theme, as a theme file would hold it.</param>
    /// <param name="name">What to call the theme, since raw JSON has no file name to take it from.</param>
    /// <param name="allowSmall">
    ///     True to waive the size floors for this load: the minimum number of nouns, of words each noun
    ///     reaches and of combinations per category. The rules about a broken file still apply.
    /// </param>
    /// <returns>The theme, or a failure whose error lists every reason in its inner errors.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="json" /> is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="name" /> is null, empty or white space.</exception>
    public static Outcome<ThemeDocument> LoadFromJsonResult(string json, string name = "inline", bool allowSmall = false) {
        ArgumentNullException.ThrowIfNull(json);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        return ThemeLoader.Load(name, json, allowSmall);
    }

    /// <summary>Loads one of the themes compiled into the library, and throws if it is refused.</summary>
    /// <param name="name">The theme's name, case-sensitive: one of those <see cref="ListEmbedded" /> returns.</param>
    /// <remarks>
    ///     There is no <c>allowSmall</c> argument here: the size floors are waived only by
    ///     <see cref="LoadEmbeddedResult" /> or by the theme's own <c>"allowSmall": true</c>.
    /// </remarks>
    /// <exception cref="DomainException">
    ///     The theme was refused. The message only names the theme; every reason is in the inner errors
    ///     of <see cref="DiagnosableException.Error" />.
    /// </exception>
    /// <exception cref="ArgumentException"><paramref name="name" /> is null, empty or white space.</exception>
    public static ThemeDocument LoadEmbedded(string name) {
        return LoadEmbeddedResult(name).GetResultOrThrow();
    }

    /// <summary>Loads a theme file, and throws if it is refused.</summary>
    /// <param name="path">The file to read. The theme is named after the file, without its extension.</param>
    /// <remarks>
    ///     There is no <c>allowSmall</c> argument here: the size floors are waived only by
    ///     <see cref="LoadFromFileResult" /> or by the theme's own <c>"allowSmall": true</c>.
    /// </remarks>
    /// <exception cref="DomainException">
    ///     The theme was refused, or the file does not exist. The message only names the theme; every
    ///     reason is in the inner errors of <see cref="DiagnosableException.Error" />.
    /// </exception>
    /// <exception cref="ArgumentException"><paramref name="path" /> is null, empty or white space.</exception>
    public static ThemeDocument LoadFromFile(string path) {
        return LoadFromFileResult(path).GetResultOrThrow();
    }

    /// <summary>Loads a theme from its JSON text, and throws if it is refused.</summary>
    /// <param name="json">The theme, as a theme file would hold it.</param>
    /// <param name="name">What to call the theme, since raw JSON has no file name to take it from.</param>
    /// <remarks>
    ///     There is no <c>allowSmall</c> argument here: the size floors are waived only by
    ///     <see cref="LoadFromJsonResult" /> or by the theme's own <c>"allowSmall": true</c>.
    /// </remarks>
    /// <exception cref="DomainException">
    ///     The theme was refused. The message only names the theme; every reason is in the inner errors
    ///     of <see cref="DiagnosableException.Error" />.
    /// </exception>
    /// <exception cref="ArgumentNullException"><paramref name="json" /> is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="name" /> is null, empty or white space.</exception>
    public static ThemeDocument LoadFromJson(string json, string name = "inline") {
        return LoadFromJsonResult(json, name).GetResultOrThrow();
    }

    /// <summary>The names of the themes compiled into the library, as <see cref="LoadEmbedded" /> accepts them.</summary>
    public static IReadOnlyList<string> ListEmbedded() {
        return new EmbeddedThemeCatalog().ListNames();
    }

    private static Outcome<ThemeDocument> Refuse(string name, IReadOnlyList<DomainError> reasons) {
        return Outcome<ThemeDocument>.Failure(ThemeErrors.Rejected(name, reasons));
    }

    #endregion

}