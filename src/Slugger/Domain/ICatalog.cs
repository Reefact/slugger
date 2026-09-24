#region Usings declarations

using FirstClassErrors;

#endregion

namespace Slugger.Domain;

/// <summary>
///     The themes the domain can reach, whatever actually holds them - the resources compiled into
///     the assembly, the theme directory, or the chain of the two.
/// </summary>
/// <remarks>
///     <para>
///         <b>It hands back valid themes, and says so twice.</b> A repository is supposed to serve
///         whole aggregates, but what it reads can have been corrupted since it was written - a
///         file edited by hand, a theme registered by an older version. So <see cref="Get" />
///         reports, for a caller that has a report to fill and a reader to answer to, and
///         <see cref="GetOrThrow" /> throws, for the rest of the domain, which never meets an
///         invalid theme and has nothing to say about one.
///     </para>
///     <para>
///         <b>Absence is a refusal like any other.</b> A theme that is not there and a theme that
///         cannot be read are both reasons <see cref="Get" /> hands back none, and neither is a
///         null: DEC0006 wants every reason a theme was refused, and a missing return value carries
///         exactly one.
///     </para>
/// </remarks>
[Repository]
internal interface ICatalog {

    /// <summary>Whether this catalog carries a theme of that name, valid or not.</summary>
    /// <param name="name">The theme to look for.</param>
    bool Contains(ThemeName name);

    /// <summary>The theme, or every reason it cannot be served.</summary>
    /// <param name="name">The theme to read.</param>
    Outcome<Theme> Get(ThemeName name);

    /// <summary>The same, for a caller with no report to fill.</summary>
    /// <param name="name">The theme to read.</param>
    /// <exception cref="DiagnosableException">
    ///     The theme is not there, or is not one; the exception carries the first reason.
    /// </exception>
    Theme GetOrThrow(ThemeName name);

    /// <summary>Every theme this catalog can serve, for <c>--list-themes</c> and for the draw across all of them.</summary>
    IReadOnlyList<ThemeName> Names();

}
