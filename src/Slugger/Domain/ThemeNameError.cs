#region Usings declarations

using System.Diagnostics.CodeAnalysis;

using DiagnosticCatalog.Sonar;

using FirstClassErrors;

#endregion

namespace Slugger.Domain;

/// <summary>
///     Every way a value can fail to name a theme: its situations, their codes, their
///     documentation and the exception they raise.
/// </summary>
[ProvidesErrorsFor(
    "ThemeName",
    Description = "Reading a value as a theme name: how a theme is asked for, and the stem of the file it is served from.")]
public sealed class ThemeNameError : Error {

    #region Static members

    /// <summary>The value a refusal is about.</summary>
    public static readonly ErrorContextKey<string> Value = ErrorContextKey.Create<string>("Value", "The value that was read as a theme name.");

    /// <summary>The path separator found inside the value.</summary>
    public static readonly ErrorContextKey<string> Separator = ErrorContextKey.Create<string>("Separator", "The path separator that made the value a path.");

    /// <summary>Nothing was written where a theme name was expected - empty, or whitespace alone.</summary>
    /// <param name="value">What was read, as written, so that a report can point at the line.</param>
    [DocumentedBy(nameof(DescribeEmpty))]
    public static ThemeNameError Empty(string value) {
        return new ThemeNameError(
            Codes.Empty,
            "A theme name cannot be empty.",
            "A theme name is missing.",
            "A theme is asked for by name; name the one to use.",
            context => context.Add(Value, value));
    }

    /// <summary>
    ///     The value is a path rather than a name. Refused rather than trimmed down to its last
    ///     piece: a catalog combines a name with its directory, so a value carrying a separator
    ///     asks to be read somewhere the catalog does not serve.
    /// </summary>
    /// <param name="value">What was read.</param>
    /// <param name="separator">The first path separator it carries.</param>
    [DocumentedBy(nameof(DescribeCarriesAPathSeparator))]
    public static ThemeNameError CarriesAPathSeparator(string value, char separator) {
        return new ThemeNameError(
            Codes.CarriesAPathSeparator,
            $"\"{value}\" is a path, not a theme name: '{separator}' separates a path.",
            "A theme name carries a path separator.",
            "Name the theme alone. To read a theme file elsewhere, point --theme-dir at its directory.",
            context => context.Add(Value, value).Add(Separator, separator.ToString()));
    }

    /// <summary>
    ///     The value names a directory - the current one, or the one above. It carries no
    ///     separator, so nothing before this catches it, and it is what a traversal walks with.
    /// </summary>
    /// <param name="value">What was read.</param>
    [DocumentedBy(nameof(DescribeNamesADirectory))]
    public static ThemeNameError NamesADirectory(string value) {
        return new ThemeNameError(
            Codes.NamesADirectory,
            $"\"{value}\" names a directory, not a theme.",
            "A theme name names a directory.",
            "Name the theme alone. To read a theme file elsewhere, point --theme-dir at its directory.",
            context => context.Add(Value, value));
    }

    private static ErrorDocumentation DescribeEmpty() {
        return DescribeError
              .WithTitle("A theme name cannot be empty")
              .WithDescription(
                   "A theme is identified by its name, which is also the stem of the file a catalog serves "
                 + "it from. An empty value names no theme, so there is nothing to look for and nothing to "
                 + "report as missing.")
              .WithRule("A theme name holds at least one character once its surrounding whitespace is off.")
              .WithDiagnostic(
                   "--theme was given an empty argument, or one of quoted whitespace.",
                   ErrorOrigin.External,
                   "Name the theme to use, or drop the option to let the configured default stand.")
              .AndDiagnostic(
                   "A caller derived a name from a file path that ends in a separator, so the stem it took "
                 + "is empty.",
                   ErrorOrigin.Internal,
                   "Check what the path was before its stem was taken.")
              .WithExamples(() => Empty("   "));
    }

    private static ErrorDocumentation DescribeCarriesAPathSeparator() {
        return DescribeError
              .WithTitle("A theme name is a name, never a path")
              .WithDescription(
                   "A catalog turns a name into a location by combining it with the directory it serves, "
                 + "and that combination obeys whatever the name asks for: a rooted name replaces the "
                 + "directory outright, and one carrying \"..\" walks out of it. Refusing the separator is "
                 + "what keeps the theme directory the whole of what a catalog can reach.")
              .WithRule("A theme name carries none of '/', '\\' or ':'.")
              .WithDiagnostic(
                   "--theme was given a path to a file rather than the name of a theme.",
                   ErrorOrigin.External,
                   "Point --theme-dir at the directory and name the theme alone.")
              .AndDiagnostic(
                   "A caller passed a path straight through where the stem was expected.",
                   ErrorOrigin.Internal,
                   "Take the file name without its extension before naming a theme with it.")
              .WithExamples(() => CarriesAPathSeparator("../../etc/passwd", '/'));
    }

    private static ErrorDocumentation DescribeNamesADirectory() {
        return DescribeError
              .WithTitle("A theme name names a directory")
              .WithDescription(
                   "\".\" and \"..\" are what a file system calls the current directory and the one above. "
                 + "Neither carries a separator, so the rule about paths lets them through, and \"..\" is "
                 + "the half of a traversal that does the walking.")
              .WithRule("A theme name is neither \".\" nor \"..\".")
              .WithDiagnostic(
                   "--theme was given a directory shorthand rather than the name of a theme.",
                   ErrorOrigin.External,
                   "Name the theme to use; --list-themes reports the ones in scope.")
              .AndDiagnostic(
                   "A caller took the stem of a path that names a directory rather than a file.",
                   ErrorOrigin.Internal,
                   "Check that the path names a theme file before taking its stem.")
              .WithExamples(() => NamesADirectory(".."));
    }

    #endregion

    #region Constructors & Destructor

    private ThemeNameError(ErrorCode                   code,
                           string                      diagnosticMessage,
                           string                      shortMessage,
                           string                      detailedMessage,
                           Action<ErrorContextBuilder> configureContext)
        : base(code, diagnosticMessage, shortMessage, detailedMessage, configureContext) { }

    #endregion

    /// <summary>Raised as this concept's own exception, so a caller can catch it by name.</summary>
    public override DiagnosableException ToException() {
        return new ThemeNameException(this);
    }

    /// <summary>The codes the factories above produce, so a caller can match on one.</summary>
    [SuppressMessage(
        SonarRule.S3218.Category,
        SonarRule.S3218.Id,
        Justification = SuppressionJustifications.CodesMirrorTheirFactories)]
    public static class Codes {

        #region Static members

        /// <summary>See <see cref="ThemeNameError.Empty" />.</summary>
        public static readonly ErrorCode Empty = ErrorCode.Create("THEME_NAME_EMPTY");

        /// <summary>See <see cref="ThemeNameError.CarriesAPathSeparator" />.</summary>
        public static readonly ErrorCode CarriesAPathSeparator = ErrorCode.Create("THEME_NAME_CARRIES_A_PATH_SEPARATOR");

        /// <summary>See <see cref="ThemeNameError.NamesADirectory" />.</summary>
        public static readonly ErrorCode NamesADirectory = ErrorCode.Create("THEME_NAME_NAMES_A_DIRECTORY");

        #endregion

    }

}
