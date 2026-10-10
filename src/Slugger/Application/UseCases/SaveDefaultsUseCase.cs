#region Usings declarations

using Slugger.Application.Abstractions;
using Slugger.Application.Options;

#endregion

namespace Slugger.Application.UseCases;

/// <summary>
///     <c>--init</c>: persist every other option on the same command line as the defaults of
///     future runs. Every option is saved as it was given, except a theme directory, which is saved
///     as the absolute path it named.
/// </summary>
internal sealed class SaveDefaultsUseCase(IConfigStore config) {

    #region Static members

    /// <summary>
    ///     The same options with the theme directory made absolute. A relative <c>--theme-dir</c> is
    ///     read from wherever the command runs, and a later run starts somewhere else: saved as
    ///     written, it would name another directory - or none - depending on where slugger is run.
    /// </summary>
    /// <param name="options">What the command line asked for alongside --init.</param>
    private static SluggerOptions Anchored(SluggerOptions options) {
        if (options.ThemeDirectory is not { Length: > 0 } directory) { return options; }

        return options with { ThemeDirectory = Path.GetFullPath(directory) };
    }

    #endregion

    private IConfigStore Config { get; } = config;

    /// <summary>Persists the rest of the command line as the defaults of future runs.</summary>
    /// <param name="options">Every other option passed alongside --init.</param>
    internal void Execute(SluggerOptions options) {
        ArgumentNullException.ThrowIfNull(options);

        // Laid over whatever was saved before, so a second --init adds to the config rather than
        // wiping the options this command line says nothing about.
        Config.Save(OptionResolver.Merge(Anchored(options), Config.Load()));
    }

}
