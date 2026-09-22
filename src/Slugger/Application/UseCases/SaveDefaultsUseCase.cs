#region Usings declarations

using Slugger.Application.Abstractions;
using Slugger.Application.Options;

#endregion

namespace Slugger.Application.UseCases;

/// <summary>
///     <c>--init</c>: persist every other option on the same command line as the defaults of
///     future runs. No option gets special treatment.
/// </summary>
internal sealed class SaveDefaultsUseCase(IConfigStore config) {

    private IConfigStore Config { get; } = config;

    /// <summary>Persists the rest of the command line as the defaults of future runs.</summary>
    /// <param name="options">Every other option passed alongside --init.</param>
    internal void Execute(SluggerOptions options) {
        ArgumentNullException.ThrowIfNull(options);

        // Laid over whatever was saved before, so a second --init adds to the config rather than
        // wiping the options this command line says nothing about.
        Config.Save(OptionResolver.Merge(options, Config.Load()));
    }

}