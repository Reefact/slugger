using Slugger.Application.Options;

namespace Slugger.Application.Abstractions;

/// <summary>The defaults persisted by <c>--init</c>, one layer of the precedence chain.</summary>
public interface IConfigStore
{
    /// <summary>The saved options, or null when nothing was ever persisted.</summary>
    SluggerOptions? Load();

    void Save(SluggerOptions options);
}
