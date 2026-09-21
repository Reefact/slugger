namespace Slugger.Domain;

/// <summary>
/// How an option's value is written outside the code - in a theme file, on the command line, and
/// in the message that names what was expected.
/// </summary>
/// <remarks>
/// Camel case rather than lower case, because a name of several words read back in one piece
/// names nothing: "threeOrTwo" is what a theme file writes and "threeortwo" is what a refusal
/// used to offer back. Both parsers read case-insensitively, so this changes what is shown and
/// never what is accepted - and the four single word modes are spelled exactly as before.
/// </remarks>
internal static class Spelling
{
    /// <summary>One value, as a theme file and a command line write it.</summary>
    /// <param name="value">The value to spell.</param>
    internal static string Of<TEnum>(TEnum value)
        where TEnum : struct, Enum =>
        Camel(value.ToString());

    /// <summary>Every value of the type, in declaration order, for a message listing the choices.</summary>
    internal static IEnumerable<string> All<TEnum>()
        where TEnum : struct, Enum =>
        Enum.GetNames<TEnum>().Select(Camel);

    private static string Camel(string name) => char.ToLowerInvariant(name[0]) + name[1..];
}
