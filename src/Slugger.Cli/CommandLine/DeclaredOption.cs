#region Usings declarations

using System.ComponentModel;
using System.Reflection;

#endregion

namespace Slugger.Cli.CommandLine;

/// <summary>One option, as the attributes of a <see cref="SluggerSettings" /> property declare it.</summary>
/// <param name="Property">Where its text is bound.</param>
/// <param name="Template">The option as <c>--help</c> prints it.</param>
/// <param name="Name">What is typed to give it.</param>
/// <param name="Shape">What it takes after its name.</param>
/// <param name="Description">What <c>--help</c> says of it.</param>
internal sealed record DeclaredOption(
    PropertyInfo Property,
    string       Template,
    string       Name,
    OptionShape  Shape,
    string       Description) {

    #region Static members

    /// <summary>The option a property declares.</summary>
    /// <param name="property">A property carrying <see cref="CommandOptionAttribute" />.</param>
    internal static DeclaredOption Of(PropertyInfo property) {
        ArgumentNullException.ThrowIfNull(property);

        string template    = property.GetCustomAttribute<CommandOptionAttribute>()!.Template;
        string description = property.GetCustomAttribute<DescriptionAttribute>()?.Description ?? string.Empty;

        return new DeclaredOption(property, template, template.Split(' ', 2)[0], ShapeOf(template), description);
    }

    private static OptionShape ShapeOf(string template) {
        if (template.Contains('<', StringComparison.Ordinal)) { return OptionShape.Value; }
        if (template.Contains('[', StringComparison.Ordinal)) { return OptionShape.OptionalValue; }

        return OptionShape.Switch;
    }

    #endregion

    /// <summary>Whether it may be given more than once, each value kept.</summary>
    internal bool IsRepeatable => Property.PropertyType == typeof(string[]);

}