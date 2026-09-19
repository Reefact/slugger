using System.Globalization;
using Slugger.Domain.Validation;

namespace Slugger.Cli.Rendering;

/// <summary>
/// Turns a refused load into the report a theme author reads: every reason at once, in one
/// place, so the fix is one pass rather than one run per problem.
/// </summary>
/// <remarks>
/// This is the single home for turning facts into prose, which is what makes the spec's
/// promise hold - a theme refused by <c>--register</c> and the same theme refused at runtime
/// produce the same wording, because there is only one wording.
/// </remarks>
public static class ThemeReportRenderer
{
    /// <summary>How many offenders of one kind are named before the rest are counted instead.</summary>
    public const int MaxNamedPerKind = 3;

    /// <summary>Renders the whole report, one line per element, ready to print.</summary>
    /// <param name="result">The refused load.</param>
    public static IReadOnlyList<string> Render(ThemeLoadResult result)
    {
        ArgumentNullException.ThrowIfNull(result);

        if (result.IsLoaded)
        {
            return [$"theme \"{result.ThemeName}\" loaded."];
        }

        List<string> lines =
        [
            result.Errors.Count == 1
                ? $"theme \"{result.ThemeName}\" was refused for 1 reason:"
                : $"theme \"{result.ThemeName}\" was refused for {result.Errors.Count} reasons:",
            string.Empty,
        ];

        foreach (IGrouping<ThemeValidationErrorCode, ThemeValidationError> kind in result.Errors.GroupBy(error => error.Code))
        {
            lines.AddRange(RenderKind(kind));
        }

        return lines;
    }

    private static IEnumerable<string> RenderKind(IGrouping<ThemeValidationErrorCode, ThemeValidationError> kind)
    {
        ThemeValidationError[] all = [.. kind];

        foreach (ThemeValidationError error in all.Take(MaxNamedPerKind))
        {
            yield return $"  - {Describe(error)}";
        }

        int unnamed = all.Length - MaxNamedPerKind;
        if (unnamed > 0)
        {
            yield return $"    ... and {unnamed} more of the same kind";
        }
    }

    private static string Describe(ThemeValidationError error) => error switch
    {
        ThemeValidationError.MalformedJson malformed => $"the file is not valid JSON{At(malformed)}: {malformed.Detail}",

        ThemeValidationError.MalformedSection section =>
            $"\"{section.Section}\" must be {section.Expected}",

        ThemeValidationError.MalformedNoun noun =>
            $"nouns[{noun.Index.ToString(CultureInfo.InvariantCulture)}]: {noun.Detail}",

        ThemeValidationError.UnknownCategory category =>
            $"\"{category.Noun}\" references category \"{category.Category}\", which the theme does not declare "
            + $"(it declares {string.Join(", ", category.KnownCategories)})",

        ThemeValidationError.TooFewNouns few =>
            $"{Count(few.Count, "noun")}, but a theme needs at least {Number(few.Minimum)}",

        ThemeValidationError.PoolTooSmall pool =>
            $"\"{pool.Noun}\" reaches {Count(pool.PoolSize, "adjective")}, but every noun needs at least {Number(pool.Minimum)}",

        ThemeValidationError.CategoryTooPoor poor =>
            $"category \"{poor.Category}\" totals {Number(poor.Combinations)} combinations, but every category needs at least {Number(poor.Minimum)}",

        ThemeValidationError.ParticiplesRequestedButAbsent absent =>
            $"defaults.segmentMode asks for \"{absent.RequestedMode.ToString().ToLowerInvariant()}\", but the theme declares no participle anywhere",

        _ => error.Code.ToString(),
    };

    private static string At(ThemeValidationError.MalformedJson malformed) => malformed.LineNumber is { } line
        ? $" at line {Number(line + 1)}"
        : string.Empty;

    private static string Count(long value, string singular) =>
        value == 1 ? $"1 {singular}" : $"{Number(value)} {singular}s";

    private static string Number(long value) => value.ToString("N0", CultureInfo.InvariantCulture);
}
