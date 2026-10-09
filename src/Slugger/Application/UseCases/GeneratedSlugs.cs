namespace Slugger.Application.UseCases;

/// <summary>
///     What a batch produced: the slugs, and why the last of them is not on the clipboard when it was
///     meant to be.
/// </summary>
/// <param name="Slugs">The slugs, in the order they were drawn.</param>
/// <param name="ClipboardFailure">
///     Null when the clipboard was not asked for, or when the copy went through; otherwise why it did
///     not, in a few words. Never a refusal: the slugs were drawn, and the caller is the one to say
///     that the copy was not made.
/// </param>
internal sealed record GeneratedSlugs(IReadOnlyList<string> Slugs, string? ClipboardFailure = null);