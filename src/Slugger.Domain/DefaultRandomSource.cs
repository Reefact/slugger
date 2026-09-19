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

    public DefaultRandomSource()
        : this(null)
    {
    }

    public DefaultRandomSource(int? seed) => _random = seed is { } value ? new Random(value) : Random.Shared;

    public int Next(int exclusiveUpperBound) => _random.Next(exclusiveUpperBound);
}
