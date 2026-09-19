using System.Diagnostics.CodeAnalysis;
using DiagnosticCatalog.Sonar;

namespace Slugger.Domain;

/// <summary>
/// The BCL backed <see cref="IRandomSource"/>. It lives in the domain rather than in an
/// adapter because <see cref="Random"/> is computation, not I/O, and because the two argument
/// SlugGenerator.Generate overload promised to library consumers has to work without a
/// composition root handing it a source.
/// </summary>
public sealed class DefaultRandomSource : IRandomSource
{
    private readonly Random _random;

    /// <summary>Draws from the shared, time seeded source: a different sequence on every run.</summary>
    public DefaultRandomSource()
        : this(null)
    {
    }

    /// <param name="seed">
    /// The seed behind <c>--seed</c>. Null draws from the shared source instead.
    /// </param>
    [SuppressMessage(
        SonarRule.S2245.Category,
        SonarRule.S2245.Id,
        Justification = SuppressionJustifications.NotASecurityContext)]
    public DefaultRandomSource(int? seed) => _random = seed is { } value ? new Random(value) : Random.Shared;

    /// <inheritdoc />
    public int Next(int exclusiveUpperBound) => _random.Next(exclusiveUpperBound);
}
