using Slugger.Application.Abstractions;
using Slugger.Application.Options;

namespace Slugger.Application.UseCases;

/// <summary>
/// <c>--init</c>: persist every other option on the same command line as the defaults of
/// future runs. No option gets special treatment.
/// </summary>
public sealed class SaveDefaultsUseCase(IConfigStore config)
{
    private IConfigStore Config { get; } = config;

    public void Execute(SluggerOptions options) => throw new NotImplementedException();
}
