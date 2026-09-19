using System.Reflection;
using Slugger.Cli.Adapters;

namespace Slugger.Cli.UnitTests;

/// <summary>
/// The spec makes TextCopy the single external dependency of the CLI, and leaves the engine
/// dependency-free. That is a promise to anyone referencing Slugger.Core as a library, so it
/// is worth a test rather than a comment - and it is the one boundary still enforced by an
/// assembly split rather than by convention.
/// </summary>
public sealed class ClipboardDependencyTests
{
    [Fact]
    public void TextCopy_never_reaches_the_engine()
    {
        // Setup
        Assembly core = typeof(Themes).Assembly;

        // Exercise
        IEnumerable<string?> references = core.GetReferencedAssemblies().Select(reference => reference.Name);

        // Verify
        Assert.DoesNotContain("TextCopy", references);
    }

    [Fact]
    public void The_cli_is_where_TextCopy_is_referenced()
    {
        // Setup
        Assembly cli = typeof(TextCopyClipboard).Assembly;

        // Exercise
        IEnumerable<string?> references = cli.GetReferencedAssemblies().Select(reference => reference.Name);

        // Verify
        Assert.Contains("TextCopy", references);
    }
}
