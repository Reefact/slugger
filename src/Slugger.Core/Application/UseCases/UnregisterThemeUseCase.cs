using Slugger.Application.Abstractions;
using Slugger.Application.Options;
using System.Diagnostics.CodeAnalysis;
using DiagnosticCatalog.Sonar;

namespace Slugger.Application.UseCases;

/// <summary>
/// <c>--unregister</c>: delete a custom theme file. A built-in theme with no custom file of
/// the same name cannot be unregistered - there is no file to remove, only a name to leave
/// out of <c>--theme</c>.
/// </summary>
[SuppressMessage(
    SonarRule.S2325.Category,
    SonarRule.S2325.Id,
    Justification = "Scaffolding: the body still throws, so the injected Store is not read yet. Implementing it will.")]
public sealed class UnregisterThemeUseCase(IThemeStore store)
{
    private IThemeStore Store { get; } = store;

    /// <summary>Deletes a custom theme file.</summary>
    /// <param name="name">The theme to unregister.</param>
    /// <param name="options">Where --theme-dir points, if anywhere.</param>
    public void Execute(string name, SluggerOptions options) => throw new NotImplementedException();
}
