#region Usings declarations

using System.Reflection;

#endregion

namespace Slugger.Cli.CommandLine;

/// <summary>
///     Binds a command line onto <see cref="SluggerSettings" />, keeping to text: converting is
///     <see cref="CommandLineReader" />'s, which accumulates every complaint, so whatever this cannot
///     place is handed over rather than refused here (DEC0006, DEC0032).
/// </summary>
internal static class CommandLineParser {

    /// <summary>The token that ends the options: whatever follows it is attached to none.</summary>
    private const string EndOfOptions = "--";

    #region Static members

    /// <summary>Every option, read once from the attributes of <see cref="SluggerSettings" />.</summary>
    internal static IReadOnlyList<DeclaredOption> Options { get; } = [
        .. typeof(SluggerSettings).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                                  .Where(property => property.IsDefined(typeof(CommandOptionAttribute)))
                                  .Select(DeclaredOption.Of)
    ];

    /// <summary>The line, bound.</summary>
    /// <param name="arguments">The command line as the runtime handed it over.</param>
    internal static ParsedLine Parse(IReadOnlyList<string> arguments) {
        ArgumentNullException.ThrowIfNull(arguments);

        IEnumerable<string> options = arguments.TakeWhile(argument => argument != EndOfOptions);
        if (options.Any(IsHelp)) { return Answered(LineIntent.Help); }
        if (options.Any(IsVersion)) { return Answered(LineIntent.Version); }

        Binding binding = new();
        int     read    = 0;
        while (read < arguments.Count) {
            string argument = arguments[read];
            read++;

            if (argument == EndOfOptions) {
                binding.Unattached.AddRange(arguments.Skip(read));

                break;
            }

            if (!IsOption(argument)) {
                binding.Unattached.Add(argument);

                continue;
            }

            read = binding.Bind(argument, arguments, read);
        }

        return new ParsedLine(LineIntent.Run, binding.Settings(), binding.Leftovers());
    }

    /// <summary>
    ///     Whether a token names an option rather than gives a value: two dashes, or one dash and a
    ///     letter. A lone dash and a negative number are values - <c>--sep -</c> joins with a dash,
    ///     <c>--seed -3</c> seeds with minus three.
    /// </summary>
    /// <param name="token">A token of the command line.</param>
    internal static bool IsOption(string token) {
        ArgumentNullException.ThrowIfNull(token);

        if (token.StartsWith("--", StringComparison.Ordinal)) { return true; }

        return token.Length > 1 && token[0] == '-' && char.IsLetter(token[1]);
    }

    /// <summary>A line slugger answers alone, nothing bound.</summary>
    private static ParsedLine Answered(LineIntent intent) {
        return new ParsedLine(intent, new SluggerSettings(), Leftovers.None);
    }

    private static bool IsHelp(string token) {
        return token is "-h" or "--help";
    }

    private static bool IsVersion(string token) {
        return token is "-v" or "--version";
    }

    #endregion

    #region Nested types

    /// <summary>What has been bound so far, and what could not be.</summary>
    private sealed class Binding {

        #region Static members

        /// <summary>
        ///     Whether the word after an option is its value: when the option was not given one after
        ///     an equals sign, and the word is not itself an option. An option slugger does not have
        ///     takes it too, so that <c>--Count 2</c> is one complaint about the option rather than a
        ///     second about a "2" attached to nothing.
        /// </summary>
        private static bool TakesTheNextWord(string? inline, IReadOnlyList<string> arguments, int next) {
            if (inline is not null) { return false; }
            if (next >= arguments.Count) { return false; }

            return !IsOption(arguments[next]);
        }

        #endregion

        #region Fields

        private readonly Dictionary<DeclaredOption, object> _bound   = [];
        private readonly List<string>                       _unknown = [];
        private readonly List<string>                       _refused = [];

        #endregion

        internal List<string> Unattached { get; } = [];

        /// <summary>Binds the option a token names, and returns where reading resumes.</summary>
        /// <param name="token">The token naming the option, with its value after an equals sign or not.</param>
        /// <param name="arguments">The whole command line.</param>
        /// <param name="next">Where the token after it sits.</param>
        internal int Bind(string token, IReadOnlyList<string> arguments, int next) {
            string[] parts  = token.Split('=', 2);
            string   name   = parts[0];
            string?  inline = parts.Length == 2 ? parts[1] : null;

            DeclaredOption? option = Options.FirstOrDefault(declared => declared.Name == name);
            if (option is null) {
                _unknown.Add(name);

                return TakesTheNextWord(inline, arguments, next) ? next + 1 : next;
            }

            if (option.Shape == OptionShape.Switch) {
                BindSwitch(option, name, inline);

                return next;
            }

            string? value = inline;
            if (TakesTheNextWord(inline, arguments, next)) {
                value = arguments[next];
                next++;
            }

            if (option.Shape == OptionShape.OptionalValue) {
                _bound[option] = new FlagValue<string> { IsSet = true, Value = value };

                return next;
            }

            if (value is null) {
                _refused.Add($"\"{name}\" was given no value");

                return next;
            }

            Keep(option, value);

            return next;
        }

        internal SluggerSettings Settings() {
            SluggerSettings settings = new();
            foreach ((DeclaredOption option, object value) in _bound) {
                object written = value is List<string> values ? values.ToArray() : value;
                option.Property.SetValue(settings, written);
            }

            return settings;
        }

        internal Leftovers Leftovers() {
            return new Leftovers([.. Unattached], [.. _unknown.Distinct(StringComparer.Ordinal)], [.. _refused]);
        }

        /// <summary>
        ///     A command switch takes no word after it, but reads a value after its equals sign, as it
        ///     did under Spectre: <c>--init=false</c> asks for nothing - it must not overwrite the saved
        ///     defaults - and any value but <c>true</c> or <c>false</c> is refused. The last one given
        ///     wins, so <c>--init --init=false</c> asks for nothing either (measured under Spectre).
        /// </summary>
        private void BindSwitch(DeclaredOption option, string name, string? inline) {
            if (inline is null or "true") {
                _bound[option] = true;

                return;
            }

            if (inline == "false") {
                _bound[option] = false;

                return;
            }

            _refused.Add($"\"{name}\" accepts true or false; \"{inline}\" is none of them");
        }

        /// <summary>A repeatable option keeps every value; any other keeps the last, as a later word overrides an earlier one.</summary>
        private void Keep(DeclaredOption option, string value) {
            if (!option.IsRepeatable) {
                _bound[option] = value;

                return;
            }

            if (_bound.TryGetValue(option, out object? kept)) {
                ((List<string>)kept).Add(value);

                return;
            }

            _bound[option] = new List<string> { value };
        }

    }

    #endregion

}