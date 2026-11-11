namespace Slugger.Cli.CommandLine;

/// <summary>What an option takes after its name, as its template writes it.</summary>
internal enum OptionShape {

    /// <summary><c>--name</c>: nothing.</summary>
    Switch,

    /// <summary><c>--name [true|false]</c>: a value, or nothing.</summary>
    OptionalValue,

    /// <summary><c>--name &lt;VALUE&gt;</c>: a value, always.</summary>
    Value

}