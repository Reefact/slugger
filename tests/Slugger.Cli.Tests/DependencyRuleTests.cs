using System.Reflection;
using Slugger.Application.Abstractions;
using Slugger.Cli.Adapters;
using Slugger.Domain;
using Slugger.Infrastructure.ThemeCatalogs;

namespace Slugger.Cli.Tests;

/// <summary>
/// The spec makes TextCopy the single external dependency of the CLI, and the engine
/// dependency-free. That is an architectural promise to anyone consuming Slugger.Core as a
/// library, so it is worth a test rather than a comment.
/// </summary>
public class DependencyRuleTests
{
    [Fact]
    public void TextCopy_never_leaks_out_of_the_cli()
    {
        Assembly[] engine =
        [
            typeof(Theme).Assembly,
            typeof(IThemeCatalog).Assembly,
            typeof(EmbeddedThemeCatalog).Assembly,
            typeof(Themes).Assembly,
        ];

        foreach (var assembly in engine)
        {
            assembly.GetReferencedAssemblies()
                .Select(reference => reference.Name)
                .ShouldNotContain("TextCopy", $"{assembly.GetName().Name} must stay free of external dependencies");
        }
    }

    [Fact]
    public void The_cli_is_where_TextCopy_is_referenced()
    {
        typeof(TextCopyClipboard).Assembly
            .GetReferencedAssemblies()
            .Select(reference => reference.Name)
            .ShouldContain("TextCopy");
    }
}
