namespace Slugger.Domain.Errors;

/// <summary>Gathers the facts an error is about while its factory builds it.</summary>
internal sealed class ErrorContextBuilder {

    #region Static members

    /// <summary>The context the factory's facts make, or an empty one when it has none.</summary>
    /// <param name="configure">What the factory adds, when it adds anything.</param>
    internal static ErrorContext Build(Action<ErrorContextBuilder>? configure) {
        ErrorContextBuilder builder = new();
        configure?.Invoke(builder);

        return new ErrorContext(builder._facts);
    }

    #endregion

    #region Fields

    private readonly Dictionary<string, object?> _facts = new(StringComparer.Ordinal);

    #endregion

    /// <summary>Records one fact, replacing any the same key recorded before.</summary>
    /// <param name="key">The key a catalogue declares.</param>
    /// <param name="value">The fact.</param>
    /// <typeparam name="T">What the fact is read back as.</typeparam>
    internal ErrorContextBuilder Add<T>(ErrorContextKey<T> key, T value) {
        ArgumentNullException.ThrowIfNull(key);

        _facts[key.Name] = value;

        return this;
    }

}
