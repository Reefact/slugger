namespace Slugger.Cli.Rendering;

/// <summary>How a line is drawn where the terminal draws colours, and ignored where it does not.</summary>
internal enum Ink {

    /// <summary>The terminal's own text.</summary>
    Plain,

    /// <summary>An aside, drawn fainter: the count standing in for the reasons not listed.</summary>
    Faint,

    /// <summary>The verdict of a refusal.</summary>
    Refusal,

    /// <summary>The verdict of a theme accepted.</summary>
    Acceptance,

    /// <summary>Something that went through and should not pass unread.</summary>
    Warning,

    /// <summary>A heading of the help.</summary>
    Heading

}