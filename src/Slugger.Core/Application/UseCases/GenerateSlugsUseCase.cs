using Slugger.Application.Abstractions;
using Slugger.Application.Options;
using System.Diagnostics.CodeAnalysis;
using DiagnosticCatalog.Sonar;

namespace Slugger.Application.UseCases;

/// <summary>
/// The main path: resolve the themes in scope, draw, format, optionally copy. Drives the
/// REPL and the oneshot mode alike - the loop belongs to the CLI, a batch of slugs belongs
/// here.
/// </summary>
[SuppressMessage(
    SonarRule.S2325.Category,
    SonarRule.S2325.Id,
    Justification = SuppressionJustifications.ScaffoldedStub)]
public sealed class GenerateSlugsUseCase(IThemeCatalog catalog, IConfigStore config, IClipboard clipboard)
{
    private IThemeCatalog Catalog { get; } = catalog;
    private IConfigStore Config { get; } = config;
    private IClipboard Clipboard { get; } = clipboard;

    /// <summary>Generates <c>--count</c> slugs in one go.</summary>
    public IReadOnlyList<string> Execute(SluggerOptions options) => throw new NotImplementedException();
}
