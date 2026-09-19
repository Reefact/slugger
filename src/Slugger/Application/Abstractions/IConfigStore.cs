using Slugger.Application.Options;

namespace Slugger.Application.Abstractions;

/// <summary>The defaults persisted by <c>--init</c>, one layer of the precedence chain.</summary>
internal interface IConfigStore
{
    /// <summary>The saved options, or null when nothing was ever persisted.</summary>
    SluggerOptions? Load();

    /// <summary>Persists these options as the defaults of future runs.</summary>
    /// <param name="options">Everything the command line asked for alongside --init.</param>
    void Save(SluggerOptions options);
}
