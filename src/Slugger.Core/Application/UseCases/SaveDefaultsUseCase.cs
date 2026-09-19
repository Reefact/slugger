using Slugger.Application.Abstractions;
using Slugger.Application.Options;
using System.Diagnostics.CodeAnalysis;
using DiagnosticCatalog.Sonar;

namespace Slugger.Application.UseCases;

/// <summary>
/// <c>--init</c>: persist every other option on the same command line as the defaults of
/// future runs. No option gets special treatment.
/// </summary>
[SuppressMessage(
    SonarRule.S2325.Category,
    SonarRule.S2325.Id,
    Justification = "Scaffolding: the body still throws, so the injected Config is not read yet. Implementing it will.")]
public sealed class SaveDefaultsUseCase(IConfigStore config)
{
    private IConfigStore Config { get; } = config;

    /// <summary>Persists the rest of the command line as the defaults of future runs.</summary>
    /// <param name="options">Every other option passed alongside --init.</param>
    public void Execute(SluggerOptions options) => throw new NotImplementedException();
}
