namespace Slugger.Cli.CommandLine;

/// <summary>A switch that may carry a value: absent, given bare, or given one.</summary>
/// <typeparam name="T">The value's type.</typeparam>
internal sealed class FlagValue<T> {

    public bool IsSet { get; init; }

    public T? Value { get; init; }

}