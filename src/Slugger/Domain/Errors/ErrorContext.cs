#region Usings declarations

using System.Diagnostics.CodeAnalysis;

#endregion

namespace Slugger.Domain.Errors;

/// <summary>
///     The facts an error is about - the theme, the noun, what was counted and the floor it had to
///     clear - each read with the key that wrote it.
/// </summary>
public sealed class ErrorContext {

    #region Fields

    private readonly IReadOnlyDictionary<string, object?> _facts;

    #endregion

    #region Constructors & Destructor

    internal ErrorContext(IReadOnlyDictionary<string, object?> facts) {
        _facts = facts;
    }

    #endregion

    /// <summary>
    ///     Reads one fact. Answers <c>false</c> when the error carries none under that key, or one of
    ///     another type.
    /// </summary>
    /// <param name="key">The key a catalogue declares, such as <c>ThemeErrors.ThemeName</c>.</param>
    /// <param name="value">The fact, when there is one.</param>
    /// <typeparam name="T">What the fact is read as.</typeparam>
    public bool TryGet<T>(ErrorContextKey<T> key, [MaybeNullWhen(false)] out T value) {
        ArgumentNullException.ThrowIfNull(key);

        value = default;

        if (!_facts.TryGetValue(key.Name, out object? fact)) { return false; }
        if (fact is not T typed) { return false; }

        value = typed;

        return true;
    }

}
