namespace Slugger.Application.Options;

/// <summary>
/// What one layer of the precedence chain says about <c>--max-segment-words</c>: a number, or
/// explicitly none. Carried on <see cref="SluggerOptions.MaxSegmentWords"/> itself nullable, so
/// three states are reachable rather than two - absent (the layer says nothing, DEC0004's
/// ordinary shape), <see cref="None"/> (explicitly no cap, overriding a layer below such as the
/// drawn theme's own <c>defaults</c>), or <see cref="Of"/> (capped at that many words).
/// </summary>
/// <remarks>
/// Without this, an <c>int?</c> would have to mean both "this layer is silent" and "this layer
/// asks for no cap" at once - the same collision <c>--mimic-style</c> already ran into as a bare
/// boolean (DEC0004), solved the same way: a value that can say "explicitly off" rather than
/// only "on" or "unsaid".
/// </remarks>
internal readonly record struct SegmentWordsCap
{
    /// <summary>
    /// Public, and <see cref="Words"/> an <c>init</c> property rather than a plain getter, so
    /// that <c>--init</c>'s config round-trips through <c>System.Text.Json</c> without a
    /// hand-written converter: a single public constructor whose parameter matches a public
    /// settable property is what the default reflection contract binds a JSON object onto.
    /// <see cref="Of"/> is still where the "at least one word" invariant is enforced for every
    /// caller inside this assembly.
    /// </summary>
    /// <param name="words">The cap, or null for <see cref="None"/>.</param>
    public SegmentWordsCap(int? words) => Words = words;

    /// <summary>Explicitly no cap, overriding whatever a layer below would otherwise apply.</summary>
    public static SegmentWordsCap None { get; } = new(words: null);

    /// <summary>The cap itself, or null for <see cref="None"/>.</summary>
    public int? Words { get; init; }

    /// <summary>Capped at this many words.</summary>
    /// <param name="words">At least one - a cap of zero would leave nothing to draw.</param>
    public static SegmentWordsCap Of(int words)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(words, 1);

        return new SegmentWordsCap(words);
    }
}
