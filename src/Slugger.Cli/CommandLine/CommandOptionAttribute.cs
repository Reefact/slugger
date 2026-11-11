namespace Slugger.Cli.CommandLine;

/// <summary>
///     Declares a property of <see cref="SluggerSettings" /> as an option, with its spelling and the
///     shape of its value: <c>--name &lt;VALUE&gt;</c>, <c>--name [true|false]</c> or <c>--name</c>.
/// </summary>
/// <param name="template">The option as <c>--help</c> prints it.</param>
[AttributeUsage(AttributeTargets.Property)]
internal sealed class CommandOptionAttribute(string template) : Attribute {

    public string Template { get; } = template;

}