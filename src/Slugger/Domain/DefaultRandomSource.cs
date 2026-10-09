#region Usings declarations

using System.Diagnostics.CodeAnalysis;

using DiagnosticCatalog.Sonar;

#endregion

namespace Slugger.Domain;

/// <summary>
///     The <see cref="IRandomSource" /> backed by <see cref="Random" />: seeded, for the same sequence of
///     draws on every run, or the shared, unseeded <see cref="Random.Shared" />, for a different one.
/// </summary>
/// <remarks>
///     <para>
///         <b>Not a cryptographic source.</b> Its draws can be predicted, and a theme holds few enough
///         combinations to try them all: never use a slug as a password, an access token or anything
///         else that has to stay secret.
///     </para>
///     <para>
///         <b>A seeded instance is not thread-safe</b>, because <see cref="Random" /> is not: shared between
///         threads, it can break and return the same draw from then on. Give each thread its own. The
///         instance without a seed draws from <see cref="Random.Shared" />, which is thread-safe.
///     </para>
/// </remarks>
public sealed class DefaultRandomSource : IRandomSource {

    #region Fields

    private readonly Random _random;

    #endregion

    #region Constructors & Destructor

    /// <summary>
    ///     Draws from <see cref="Random.Shared" />: a different sequence on every run, and safe to share
    ///     between threads.
    /// </summary>
    public DefaultRandomSource()
        : this(null) { }

    /// <summary>
    ///     Draws from a new <see cref="Random" /> seeded with <paramref name="seed" />, or from
    ///     <see cref="Random.Shared" /> when it is null.
    /// </summary>
    /// <remarks>
    ///     Share one seeded instance across every call of a run to replay the run:
    ///     <c>new DefaultRandomSource(42)</c> reproduces the command line's <c>--seed 42</c>. A seed is not
    ///     promised to give the same slugs with another version of the library or of the theme.
    /// </remarks>
    /// <param name="seed">The seed, or null for the shared, unseeded <see cref="Random.Shared" />.</param>
    [SuppressMessage(
        SonarRule.S2245.Category,
        SonarRule.S2245.Id,
        Justification = SuppressionJustifications.NotASecurityContext)]
    public DefaultRandomSource(int? seed) {
        _random = seed is { } value ? new Random(value) : Random.Shared;
    }

    #endregion

    /// <inheritdoc />
    public int Next(int exclusiveUpperBound) {
        return _random.Next(exclusiveUpperBound);
    }

}