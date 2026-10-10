#region Usings declarations

using Slugger.Application.Options;

#endregion

namespace Slugger.Application.Abstractions;

/// <summary>
///     What reading the config saved by <c>--init</c> produced: the options it holds, and what about
///     the file did not read the way its author probably meant - handed back rather than printed, so
///     the caller decides where a warning goes.
/// </summary>
/// <param name="Options">The saved options, or null when nothing was saved or the file could not be read.</param>
/// <param name="Remarks">Each problem with the file, as a sentence naming it; never a refusal.</param>
internal sealed record SavedConfig(SluggerOptions? Options, IReadOnlyList<string> Remarks);
