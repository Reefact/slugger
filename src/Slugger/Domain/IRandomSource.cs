namespace Slugger.Domain;

/// <summary>
/// The only non-determinism the domain is allowed, abstracted so that <c>--seed</c> gives a
/// reproducible run and so that generation stays unit testable without stubbing the clock.
/// </summary>
public interface IRandomSource
{
    /// <summary>A number in the range [0, <paramref name="exclusiveUpperBound"/>).</summary>
    int Next(int exclusiveUpperBound);
}
