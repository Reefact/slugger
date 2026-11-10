#region Usings declarations

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

using DiagnosticCatalog.Sonar;

#endregion

namespace Slugger.Domain.Errors;

/// <summary>
///     One fact an error can carry, and the type it is read as: <see cref="ErrorContext.TryGet{T}" />
///     takes the key a catalogue declares and hands the value back already typed.
/// </summary>
/// <typeparam name="T">What the fact is read as.</typeparam>
/// <remarks>
///     A fact is found by its key's <see cref="Name" />, so two catalogues declaring a key of the same
///     name and type read each other's facts.
/// </remarks>
[DebuggerDisplay("{ToString()}")]
[SuppressMessage(
    SonarRule.S2326.Category,
    SonarRule.S2326.Id,
    Justification = SuppressionJustifications.TheTypeParameterTypesTheRead)]
public sealed class ErrorContextKey<T> {

    #region Constructors & Destructor

    internal ErrorContextKey(string name) {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        Name = name;
    }

    #endregion

    /// <summary>What the fact is called, such as <c>ThemeName</c>.</summary>
    public string Name { get; }

    /// <summary>The name, for a human reading a watch window.</summary>
    public override string ToString() {
        return Name;
    }

}
