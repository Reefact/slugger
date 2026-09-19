using Slugger.Domain;
using System.Diagnostics.CodeAnalysis;
using DiagnosticCatalog.Sonar;

namespace Slugger.Application.Options;

/// <summary>
/// Collapses the precedence chain into the <see cref="GenerationOptions"/> the domain
/// consumes:
/// <code>
/// explicit argument  &gt;  theme defaults  &gt;  config saved by --init  &gt;  program default
/// </code>
/// The theme's defaults only join the chain when the choice of theme is unambiguous - a
/// single <c>--theme</c> - or when <see cref="MimicStyle.Force"/> puts them back in for a
/// multi-theme run. An explicit argument always wins over all of it.
/// </summary>
[SuppressMessage(
    SonarRule.S2325.Category,
    SonarRule.S2325.Id,
    Justification = SuppressionJustifications.ScaffoldedStub)]
public sealed class OptionResolver
{
    /// <param name="commandLine">What this invocation asked for explicitly.</param>
    /// <param name="saved">What --init persisted, or null.</param>
    /// <param name="drawnTheme">The theme actually drawn, whose defaults may apply.</param>
    /// <param name="themesInScope">How many themes --theme put in scope, which is what arms the automatic behaviour.</param>
    public GenerationOptions Resolve(
        SluggerOptions commandLine,
        SluggerOptions? saved,
        Theme drawnTheme,
        int themesInScope) => throw new NotImplementedException();

    /// <summary>Merges the command line over the saved config, for the options that are not per theme.</summary>
    public SluggerOptions Merge(SluggerOptions commandLine, SluggerOptions? saved) => throw new NotImplementedException();
}
