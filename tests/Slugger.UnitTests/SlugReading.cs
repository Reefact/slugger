#region Usings declarations

using Slugger.Domain;

#endregion

namespace Slugger.UnitTests;

/// <summary>
///     One way of reading a slug back: which of its words was the adjective, which the participle,
///     which the noun. A slug can have more than one, and that is the point - <c>silent running</c>
///     is a whole adjective, so reading it as <c>silent</c> plus <c>running</c> would be a fault of
///     the reader rather than of the theme.
/// </summary>
/// <param name="Adjective">The adjective, or null where none was drawn.</param>
/// <param name="Participle">The participle, or null where none was drawn.</param>
/// <param name="Noun">The noun, which every slug carries.</param>
internal sealed record SlugReading(string? Adjective, string? Participle, NounEntry Noun);