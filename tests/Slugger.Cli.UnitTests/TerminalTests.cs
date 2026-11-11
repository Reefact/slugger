#region Usings declarations

using Slugger.Cli.Rendering;

#endregion

namespace Slugger.Cli.UnitTests;

public sealed class TerminalTests {

    [Fact]
    public void Writes_nothing_but_the_text_where_colours_are_off() {
        // Setup
        StringWriter written = new();

        // Exercise
        new Terminal(written, false).Write([new DrawnLine("refused", Ink.Refusal), new DrawnLine("aside", Ink.Faint)]);

        // Verify
        Assert.Equal($"refused{Environment.NewLine}aside{Environment.NewLine}", written.ToString());
    }

    [Fact]
    public void Wraps_an_inked_line_in_its_colour_and_a_reset_where_colours_are_on() {
        // Setup
        StringWriter written = new();

        // Exercise
        new Terminal(written, true).Write([new DrawnLine("refused", Ink.Refusal)]);

        // Verify
        Assert.Equal($"\u001b[1;91mrefused\u001b[0m{Environment.NewLine}", written.ToString());
    }

    [Fact]
    public void Leaves_plain_text_and_blank_lines_without_a_code_where_colours_are_on() {
        // Setup
        StringWriter written = new();

        // Exercise
        new Terminal(written, true).Write([new DrawnLine("plain", Ink.Plain), new DrawnLine(string.Empty, Ink.Refusal)]);

        // Verify
        Assert.Equal($"plain{Environment.NewLine}{Environment.NewLine}", written.ToString());
    }

    [Fact]
    public void Draws_no_colour_on_a_stream_that_is_redirected() {
        // Verify - a slug piped onwards must arrive as nothing but itself.
        Assert.False(Terminal.Allows(true, null, false, null));
    }

    [Fact]
    public void Draws_no_colour_when_NO_COLOR_is_set() {
        // Verify
        Assert.False(Terminal.Allows(false, "1", false, null));
    }

    [Fact]
    public void Draws_colours_on_a_terminal_outside_Windows() {
        // Verify
        Assert.True(Terminal.Allows(false, null, false, null));
    }

    [Fact]
    public void Draws_colours_on_Windows_only_in_Windows_Terminal() {
        // Verify
        Assert.True(Terminal.Allows(false, null, true, "a-session"));
        Assert.False(Terminal.Allows(false, null, true, null));
    }

}