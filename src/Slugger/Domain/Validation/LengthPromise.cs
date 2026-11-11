namespace Slugger.Domain.Validation;

/// <summary>
///     One length a theme promises in its <c>maxLength</c> block, measured: the longest slug of that
///     shape it can draw, against the ceiling it promised for it.
/// </summary>
/// <param name="WordsInSlug">Words in a slug of that shape, the noun included: two or three.</param>
/// <param name="Shape">The key the promise is written under: <c>twoWords</c> or <c>threeWords</c>.</param>
/// <param name="Longest">The longest slug of that shape, written as the theme's own style writes it.</param>
/// <param name="Ceiling">The length promised for it.</param>
internal sealed record LengthPromise(int WordsInSlug, string Shape, string Longest, int Ceiling);
