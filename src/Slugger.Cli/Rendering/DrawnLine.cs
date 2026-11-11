namespace Slugger.Cli.Rendering;

/// <summary>A line of text and the ink it is drawn in.</summary>
/// <param name="Text">The line, written as it is: nothing in it is read as markup.</param>
/// <param name="Ink">How it is drawn where colours are.</param>
internal sealed record DrawnLine(string Text, Ink Ink);