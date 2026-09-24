#region Usings declarations

using System.Diagnostics;

using FirstClassErrors;

using Value;

#endregion

namespace Slugger.Domain;

/// <summary>
///     How a theme is asked for: the name on <c>--theme</c>, and the stem of the file a catalog
///     serves it from. It is the theme's identity - nothing inside the file is compared, and
///     <c>meta.title</c> is a label rather than a key.
/// </summary>
/// <remarks>
///     <para>
///         <b>A name is a name and never a path.</b> A catalog turns one into a location the only
///         way there is - <c>Path.Combine(directory, $"{name}.json")</c> - and that combination
///         obeys whatever the name asks for: a rooted name replaces the directory outright, and a
///         name carrying <c>..</c> walks out of it. Refusing the separator here is what keeps the
///         theme directory the whole of what a catalog can reach.
///     </para>
///     <para>
///         <b>The case is the file system's business, not the domain's.</b> Unlike
///         <see cref="Category" />, a theme name is matched by whatever holds the themes, and two
///         of those disagree: a directory on Linux tells "Docker" from "docker" and one on Windows
///         does not. Folding the case here would make the domain answer for a file it cannot open.
///     </para>
/// </remarks>
[ValueObject]
[DebuggerDisplay("{ToString()}")]
public sealed class ThemeName : ValueType<ThemeName> {

    #region Static members

    /// <summary>
    ///     What a path is written with, wherever it runs. Refused as a set rather than asking the
    ///     running platform, so that a theme named on one machine is a theme on the next: Linux
    ///     accepts a backslash in a file name and Windows cannot open what it wrote.
    /// </summary>
    private static readonly char[] Separators = ['/', '\\', ':'];

    /// <summary>The theme that value names, or the reason it names none.</summary>
    /// <param name="value">A name, of any characters but a path's. Surrounding whitespace is trimmed off.</param>
    public static Outcome<ThemeName> From(string value) {
        ArgumentNullException.ThrowIfNull(value);

        string trimmed = value.Trim();
        if (trimmed.Length == 0) { return Outcome<ThemeName>.Failure(ThemeNameError.Empty(value)); }

        int separator = trimmed.IndexOfAny(Separators);
        if (separator >= 0) { return Outcome<ThemeName>.Failure(ThemeNameError.CarriesAPathSeparator(trimmed, trimmed[separator])); }
        if (NamesADirectory(trimmed)) { return Outcome<ThemeName>.Failure(ThemeNameError.NamesADirectory(trimmed)); }

        return Outcome<ThemeName>.Success(new ThemeName(trimmed));
    }

    /// <summary>The same, for a caller that has no report to fill.</summary>
    /// <param name="value">A name, of any characters but a path's. Surrounding whitespace is trimmed off.</param>
    /// <exception cref="ThemeNameException">The value names no theme; the exception carries the reason.</exception>
    public static ThemeName FromOrThrow(string value) {
        Outcome<ThemeName> outcome = From(value);
        if (outcome.Error is ThemeNameError refused) { throw refused.ToException(); }

        return outcome.GetResultOrThrow();
    }

    /// <summary>
    ///     Whether the value is what a file system calls a directory rather than a name: the
    ///     current one or the one above. Neither carries a separator, so the test before this one
    ///     lets them through, and <c>..</c> is the half of a traversal that does the walking.
    /// </summary>
    /// <param name="value">The trimmed value, already known to carry no separator.</param>
    private static bool NamesADirectory(string value) {
        return value is "." or "..";
    }

    #endregion

    #region Fields

    private readonly string _name;

    #endregion

    #region Constructors & Destructor

    private ThemeName(string name) {
        _name = name;
    }

    #endregion

    /// <summary>The name it holds.</summary>
    [DehydrationMethod]
    public string Dehydrate() {
        return _name;
    }

    /// <summary>The name, for a human reading a watch window.</summary>
    public override string ToString() {
        return _name;
    }

    /// <summary>Two themes of the same name are the same theme, which is what naming one is for.</summary>
    protected override IEnumerable<object> GetAllAttributesToBeUsedForEquality() {
        yield return _name;
    }

}
