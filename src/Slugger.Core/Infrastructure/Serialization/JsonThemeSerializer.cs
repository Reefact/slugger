using System.Text.Json;
using Slugger.Domain;
using Slugger.Domain.Normalization;
using Slugger.Domain.Validation;

namespace Slugger.Infrastructure.Serialization;

/// <summary>
/// Reads a theme file into the domain model with System.Text.Json, which ships with the SDK
/// and so keeps this layer free of external NuGet dependencies.
/// </summary>
/// <remarks>
/// <para>
/// Two things happen on the way in, both invisible to the theme's author: every value is put
/// through <see cref="WordNormalizer"/>, and every string goes through the shared
/// <see cref="StringInternPool"/>.
/// </para>
/// <para>
/// Malformed <i>syntax</i> is terminal - nothing can be read from a document that did not
/// parse - but a malformed <i>shape</i> is not: every section is walked and every complaint
/// collected, so a file with four problems reports four, not the first one.
/// </para>
/// </remarks>
public sealed class JsonThemeSerializer
{
    /// <param name="pool">The run's shared intern pool. A fresh one per serializer would only deduplicate within a single file.</param>
    public JsonThemeSerializer(StringInternPool? pool = null) => Pool = pool ?? new StringInternPool();

    /// <summary>The pool every string read here passes through.</summary>
    public StringInternPool Pool { get; }

    /// <param name="name">The theme name, which comes from the file name rather than the JSON.</param>
    /// <param name="json">The raw file contents.</param>
    public ThemeParseResult Deserialize(string name, string json)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(json);

        JsonDocument document;
        try
        {
            document = JsonDocument.Parse(json);
        }
        catch (JsonException malformed)
        {
            return new ThemeParseResult(
                null,
                [new ThemeValidationError.MalformedJson(malformed.Message, malformed.LineNumber, malformed.BytePositionInLine)],
                RulesCanRun: false);
        }

        using (document)
        {
            return ReadTheme(name, document.RootElement);
        }
    }

    /// <param name="name">The theme name, which comes from the file name rather than the JSON.</param>
    /// <param name="stream">The raw file contents.</param>
    public ThemeParseResult Deserialize(string name, Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);

        using StreamReader reader = new(stream);

        return Deserialize(name, reader.ReadToEnd());
    }

    private ThemeParseResult ReadTheme(string name, JsonElement root)
    {
        if (root.ValueKind != JsonValueKind.Object)
        {
            return new ThemeParseResult(
                null,
                [new ThemeValidationError.MalformedSection("(document)", "an object")],
                RulesCanRun: false);
        }

        List<ThemeValidationError> errors = [];
        bool adjectivesUsable = TryReadWordGroups(root, "adjectives", required: true, errors, out Dictionary<string, IReadOnlyList<string>> adjectives);
        TryReadWordGroups(root, "participles", required: false, errors, out Dictionary<string, IReadOnlyList<string>> participles);
        bool nounsUsable = TryReadNouns(root, errors, out List<Noun> nouns);
        ThemeDefaults defaults = ReadDefaults(root, errors);
        bool allowSmall = ReadOptionalBoolean(root, "allowSmall", errors) ?? false;

        Theme theme = new(name, adjectives, participles, nouns, defaults, allowSmall);

        return new ThemeParseResult(theme, errors, adjectivesUsable && nounsUsable);
    }

    private bool TryReadWordGroups(
        JsonElement root,
        string section,
        bool required,
        List<ThemeValidationError> errors,
        out Dictionary<string, IReadOnlyList<string>> groups)
    {
        int before = errors.Count;
        groups = ReadWordGroups(root, section, required, errors);

        return errors.Count == before;
    }

    private bool TryReadNouns(JsonElement root, List<ThemeValidationError> errors, out List<Noun> nouns)
    {
        bool present = root.TryGetProperty("nouns", out JsonElement element) && element.ValueKind == JsonValueKind.Array;
        nouns = ReadNouns(root, errors);

        return present;
    }

    private Dictionary<string, IReadOnlyList<string>> ReadWordGroups(
        JsonElement root,
        string section,
        bool required,
        List<ThemeValidationError> errors)
    {
        Dictionary<string, IReadOnlyList<string>> groups = new(StringComparer.Ordinal);

        if (!root.TryGetProperty(section, out JsonElement element))
        {
            if (required)
            {
                errors.Add(new ThemeValidationError.MalformedSection(section, "an object of category to words"));
            }

            return groups;
        }

        if (element.ValueKind != JsonValueKind.Object)
        {
            errors.Add(new ThemeValidationError.MalformedSection(section, "an object of category to words"));

            return groups;
        }

        foreach (JsonProperty category in element.EnumerateObject())
        {
            if (category.Value.ValueKind != JsonValueKind.Array)
            {
                errors.Add(new ThemeValidationError.MalformedSection($"{section}.{category.Name}", "an array of strings"));

                continue;
            }

            List<string> words = [];
            foreach (JsonElement word in category.Value.EnumerateArray())
            {
                if (word.ValueKind != JsonValueKind.String)
                {
                    errors.Add(new ThemeValidationError.MalformedSection($"{section}.{category.Name}", "an array of strings"));

                    break;
                }

                words.Add(Take(word.GetString()));
            }

            groups[Pool.Intern(category.Name)] = words;
        }

        return groups;
    }

    private List<Noun> ReadNouns(JsonElement root, List<ThemeValidationError> errors)
    {
        List<Noun> nouns = [];

        if (!root.TryGetProperty("nouns", out JsonElement element) || element.ValueKind != JsonValueKind.Array)
        {
            errors.Add(new ThemeValidationError.MalformedSection("nouns", "an array of { value, categories }"));

            return nouns;
        }

        int index = 0;
        foreach (JsonElement entry in element.EnumerateArray())
        {
            if (entry.ValueKind != JsonValueKind.Object)
            {
                errors.Add(new ThemeValidationError.MalformedNoun(index, "not an object"));
                index++;

                continue;
            }

            if (!entry.TryGetProperty("value", out JsonElement value)
                || value.ValueKind != JsonValueKind.String
                || string.IsNullOrWhiteSpace(value.GetString()))
            {
                errors.Add(new ThemeValidationError.MalformedNoun(index, "no non-empty \"value\""));
                index++;

                continue;
            }

            nouns.Add(new Noun(Take(value.GetString()), ReadCategories(entry, index, errors)));
            index++;
        }

        return nouns;
    }

    private List<string> ReadCategories(JsonElement entry, int index, List<ThemeValidationError> errors)
    {
        if (!entry.TryGetProperty("categories", out JsonElement categories))
        {
            return [];
        }

        if (categories.ValueKind != JsonValueKind.Array)
        {
            errors.Add(new ThemeValidationError.MalformedNoun(index, "\"categories\" is not an array"));

            return [];
        }

        List<string> names = [];
        foreach (JsonElement category in categories.EnumerateArray())
        {
            if (category.ValueKind != JsonValueKind.String)
            {
                errors.Add(new ThemeValidationError.MalformedNoun(index, "\"categories\" holds something other than a string"));

                break;
            }

            names.Add(Pool.Intern(category.GetString()!));
        }

        return names;
    }

    private static ThemeDefaults ReadDefaults(JsonElement root, List<ThemeValidationError> errors)
    {
        if (!root.TryGetProperty("defaults", out JsonElement element))
        {
            return ThemeDefaults.Empty;
        }

        if (element.ValueKind != JsonValueKind.Object)
        {
            errors.Add(new ThemeValidationError.MalformedSection("defaults", "an object"));

            return ThemeDefaults.Empty;
        }

        return new ThemeDefaults
        {
            Separator = ReadSeparator(element, errors),
            Casing = ReadEnum<Casing>(element, "casing", errors),
            SegmentMode = ReadEnum<SegmentMode>(element, "segmentMode", errors),
            TokenLength = ReadOptionalInt(element, "tokenLength", errors),
            TokenChance = ReadOptionalInt(element, "tokenChance", errors),
            TokenHex = ReadOptionalBoolean(element, "tokenHex", errors),
            TokenGlued = ReadOptionalBoolean(element, "tokenGlued", errors),
        };
    }

    private static char? ReadSeparator(JsonElement defaults, List<ThemeValidationError> errors)
    {
        if (!defaults.TryGetProperty("sep", out JsonElement element))
        {
            return null;
        }

        string? separator = element.ValueKind == JsonValueKind.String ? element.GetString() : null;
        if (separator is not { Length: 1 })
        {
            errors.Add(new ThemeValidationError.MalformedSection("defaults.sep", "a single character"));

            return null;
        }

        return separator[0];
    }

    private static TEnum? ReadEnum<TEnum>(JsonElement defaults, string property, List<ThemeValidationError> errors)
        where TEnum : struct, Enum
    {
        if (!defaults.TryGetProperty(property, out JsonElement element))
        {
            return null;
        }

        if (element.ValueKind == JsonValueKind.String && Enum.TryParse(element.GetString(), ignoreCase: true, out TEnum parsed))
        {
            return parsed;
        }

        errors.Add(new ThemeValidationError.MalformedSection(
            $"defaults.{property}",
            $"one of {string.Join(", ", Enum.GetNames<TEnum>().Select(name => name.ToLowerInvariant()))}"));

        return null;
    }

    private static int? ReadOptionalInt(JsonElement owner, string property, List<ThemeValidationError> errors)
    {
        if (!owner.TryGetProperty(property, out JsonElement element))
        {
            return null;
        }

        if (element.ValueKind == JsonValueKind.Number && element.TryGetInt32(out int value))
        {
            return value;
        }

        errors.Add(new ThemeValidationError.MalformedSection($"defaults.{property}", "a whole number"));

        return null;
    }

    private static bool? ReadOptionalBoolean(JsonElement owner, string property, List<ThemeValidationError> errors)
    {
        if (!owner.TryGetProperty(property, out JsonElement element))
        {
            return null;
        }

        if (element.ValueKind is JsonValueKind.True or JsonValueKind.False)
        {
            return element.GetBoolean();
        }

        errors.Add(new ThemeValidationError.MalformedSection(property, "true or false"));

        return null;
    }

    private string Take(string? value) => Pool.Intern(WordNormalizer.Canonicalize(value ?? string.Empty));
}
