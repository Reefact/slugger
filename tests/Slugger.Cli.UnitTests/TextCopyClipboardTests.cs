#region Usings declarations

using Slugger.Cli.Adapters;

#endregion

namespace Slugger.Cli.UnitTests;

/// <summary>
///     What the warning says when a copy fails. The copy itself is not run here - it would replace
///     the clipboard of whoever runs the suite, and fail or not depending on their machine - so these
///     hand the reason what TextCopy raises and read what it keeps of it.
/// </summary>
public sealed class TextCopyClipboardTests {

    #region Static members

    private static string AnyWord() {
        return Any.String().WithChars("abcdefghijklmnopqrstuvwxyz").WithLengthBetween(3, 12).Generate();
    }

    #endregion

    /// <summary>
    ///     Literal on purpose: it is TextCopy's own message on a Linux machine without xsel, as
    ///     measured, and the sentence the reason is read out of.
    /// </summary>
    [Fact]
    public void Says_xsel_is_not_installed_when_the_shell_cannot_find_it() {
        // Setup
        Exception failure = new(
            "Could not execute process. Command line: bash -c \"cat /tmp/tmpBJpTaY.tmp | xsel -i --clipboard \".\n"
          + "Output: \n\n"
          + "Error: bash: line 1: xsel: command not found\n\n");

        // Exercise
        string reason = TextCopyClipboard.Reason(failure);

        // Verify
        Assert.Equal("xsel is not installed", reason);
    }

    /// <summary>The tool's own complaint, rather than the shell command TextCopy ran to reach it.</summary>
    [Fact]
    public void Keeps_what_the_clipboard_tool_complained_of() {
        // Setup
        string    complaint = $"{AnyWord()}: {AnyWord()}";
        Exception failure   = new(
            $"Could not execute process. Command line: bash -c \"{AnyWord()}\".\nOutput: \n\nError: {complaint}\n\n");

        // Exercise
        string reason = TextCopyClipboard.Reason(failure);

        // Verify
        Assert.Equal(complaint, reason);
    }

    /// <summary>
    ///     What the platform raises under TextCopy - no bash to run at all, a Windows clipboard another
    ///     program holds open - is cut to its first line.
    /// </summary>
    [Fact]
    public void Keeps_the_first_line_of_any_other_failure() {
        // Setup
        string    first   = AnyWord();
        Exception failure = new($"{first}\n{AnyWord()}");

        // Exercise
        string reason = TextCopyClipboard.Reason(failure);

        // Verify
        Assert.Equal(first, reason);
    }

    /// <summary>
    ///     An "Error:" with nothing after it is no complaint at all, and a warning ending on a colon
    ///     would say nothing: the reason falls back to the failure's first line.
    /// </summary>
    [Fact]
    public void Ignores_an_empty_complaint() {
        // Setup
        string    first   = AnyWord();
        Exception failure = new($"{first}\nError:\n");

        // Exercise
        string reason = TextCopyClipboard.Reason(failure);

        // Verify
        Assert.Equal(first, reason);
    }

    [Fact]
    public void Names_the_failure_when_it_carries_no_message() {
        // Exercise
        string reason = TextCopyClipboard.Reason(new InvalidOperationException(string.Empty));

        // Verify
        Assert.Equal(nameof(InvalidOperationException), reason);
    }

}