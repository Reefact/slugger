using System.Reflection;
using Slugger.Cli.Adapters;

namespace Slugger.Cli.UnitTests;

/// <summary>
/// DEC0007 keeps the engine free of a dependency a consumer would inherit, and the clipboard is
/// the case that pushes back: copying cannot be done without a platform library. So it sits on
/// the CLI's side of the line, which is the side DEC0007 does not reach - a command bundles what
/// it needs. That boundary is a promise to whoever references Slugger, so it is worth a test
/// rather than a comment, and it is the one still held by an assembly split rather than by
/// convention.
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
