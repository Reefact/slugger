#region Usings declarations

using Slugger.Application.Options;

#endregion

namespace Slugger.Application.Abstractions;

/// <summary>The defaults persisted by <c>--init</c>, one layer of the precedence chain.</summary>
internal interface IConfigStore {

    /// <summary>The saved options, or null when nothing was ever persisted.</summary>
    SluggerOptions? Load();

    /// <summary>
    ///     The saved options, with every remark about the file they were read from: a file that is
    ///     not JSON, a key that means nothing. What <see cref="Load" /> returns, for the one caller
    ///     that tells the user about the file.
    /// </summary>
    SavedConfig Read();

    /// <summary>Persists these options as the defaults of future runs.</summary>
    /// <param name="options">Everything the command line asked for alongside --init.</param>
    void Save(SluggerOptions options);

}
