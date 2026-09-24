#region Usings declarations

using System.Text.Json;

using FirstClassErrors;

using Slugger.Domain;
using Slugger.Domain.Normalization;
using Slugger.Domain.Validation;

#endregion

namespace Slugger.Infrastructure.Serialization;

/// <summary>
///     Reads a theme file into the domain model with System.Text.Json, which ships with the SDK
///     and so keeps this layer free of external NuGet dependencies.
/// </summary>
/// <remarks>
///     <para>
///         Two things happen on the way in, both invisible to the theme's author: every value is put
///         through <see cref="WordNormalizer" />, and every string goes through the shared
///         <see cref="StringInternPool" />.
///     </para>
///     <para>
///         Malformed <i>syntax</i> is terminal - nothing can be read from a document that did not
///         parse - but a malformed <i>shape</i> is not: every section is walked and every complaint
///         collected, so a file with four problems reports four, not the first one.
///     </para>
/// </remarks>
internal sealed class JsonThemeSerializer {

    #region Static members

    /// <summary>
    ///     "maxLength": an object of shape to ceiling, both keys optional. At the root rather than in
    ///     "defaults" on purpose (DEC0018): "defaults" are switched off as soon as several themes are
    ///     in scope, and a promise that lapses when a second theme is added is not a promise.
    /// </summary>
    private static MaxLength ReadMaxLength(JsonElement root, List<DomainError> errors) {
        if (!root.TryGetProperty("maxLength", out JsonElement element)) { return MaxLength.None; }

        if (element.ValueKind != JsonValueKind.Object) {
            errors.Add(ThemeErrors.MalformedSection("maxLength", "an object of shape to ceiling"));

            return MaxLength.None;
        }

        return new MaxLength(
            ReadCeiling(element, "twoWords", errors),
            ReadCeiling(element, "threeWords", errors));
    }

    private static int? ReadCeiling(JsonElement maxLength, string shape, List<DomainError> errors) {
        if (!maxLength.TryGetProperty(shape, out JsonElement element)) { return null; }
        if (element.ValueKind == JsonValueKind.Number && element.TryGetInt32(out int value) && value > 0) { return value; }

        errors.Add(ThemeErrors.MalformedSection($"maxLength.{shape}", "a whole number of characters above zero"));

        return null;
    }

    /// <summary>
    ///     "meta": descriptive information about the theme itself, none of it consulted by
    ///     generation. Every field is optional and read as plain text - unlike the word lists, it
    ///     does not go through <see cref="WordNormalizer" />, since it is prose rather than slug
    ///     material.
    /// </summary>
    private static ThemeMetadata ReadMetadata(JsonElement root, List<DomainError> errors) {
        if (!root.TryGetProperty("meta", out JsonElement element)) { return ThemeMetadata.Empty; }

        if (element.ValueKind != JsonValueKind.Object) {
            errors.Add(ThemeErrors.MalformedSection("meta", "an object"));

            return ThemeMetadata.Empty;
        }

        return new ThemeMetadata {
            Title       = ReadOptionalString(element, "meta", "title", errors),
            Description = ReadOptionalString(element, "meta", "description", errors),
            Version     = ReadOptionalString(element, "meta", "version", errors),
            Author      = ReadOptionalString(element, "meta", "author", errors),
            Source      = ReadOptionalString(element, "meta", "source", errors),
            CreatedAt   = ReadOptionalString(element, "meta", "createdAt", errors),
            PublishedAt = ReadOptionalString(element, "meta", "publishedAt", errors)
        };
    }

    private static string? ReadOptionalString(JsonElement owner, string section, string property, List<DomainError> errors) {
        if (!owner.TryGetProperty(property, out JsonElement element)) { return null; }
        if (element.ValueKind == JsonValueKind.String) { return element.GetString(); }

        errors.Add(ThemeErrors.MalformedSection($"{section}.{property}", "a string"));

        return null;
    }

    private static ThemeDefaults ReadDefaults(JsonElement root, List<DomainError> errors) {
        if (!root.TryGetProperty("defaults", out JsonElement element)) { return ThemeDefaults.Empty; }

        if (element.ValueKind != JsonValueKind.Object) {
            errors.Add(ThemeErrors.MalformedSection("defaults", "an object"));

            return ThemeDefaults.Empty;
        }

        return new ThemeDefaults {
            Separator       = ReadSeparator(element, errors),
            WordSeparator   = ReadWordSeparator(element, errors),
            Casing          = ReadEnum<Casing>(element, "casing", errors),
            SegmentMode     = ReadEnum<SegmentMode>(element, "segmentMode", errors),
            MaxSegmentWords = ReadOptionalInt(element, "maxSegmentWords", errors),
            TokenLength     = ReadOptionalInt(element, "tokenLength", errors),
            TokenChance     = ReadOptionalInt(element, "tokenChance", errors),
            FoldAccents     = ReadOptionalBoolean(element, "foldAccents", errors, "defaults."),
            Ascii           = ReadOptionalBoolean(element, "ascii", errors, "defaults."),
            TokenHex        = ReadOptionalBoolean(element, "tokenHex", errors, "defaults."),
            TokenGlued      = ReadOptionalBoolean(element, "tokenGlued", errors, "defaults.")
        };
    }

    private static char? ReadSeparator(JsonElement defaults, List<DomainError> errors) {
        if (!defaults.TryGetProperty("sep", out JsonElement element)) { return null; }

        string? separator = element.ValueKind == JsonValueKind.String ? element.GetString() : null;
        if (separator is not { Length: 1 }) {
            errors.Add(ThemeErrors.MalformedSection("defaults.sep", "a single character"));

            return null;
        }

        return separator[0];
    }

    /// <summary>
    ///     Unlike "sep", nothing is a value here: "" glues a compound value's words together.
    /// </summary>
    private static string? ReadWordSeparator(JsonElement defaults, List<DomainError> errors) {
        if (!defaults.TryGetProperty("wordSep", out JsonElement element)) { return null; }

        string? separator = element.ValueKind == JsonValueKind.String ? element.GetString() : null;
        if (separator is not { Length: <= 1 }) {
            errors.Add(ThemeErrors.MalformedSection("defaults.wordSep", "a single character or nothing"));

            return null;
        }

        return separator;
    }

    private static TEnum? ReadEnum<TEnum>(JsonElement defaults, string property, List<DomainError> errors)
        where TEnum : struct, Enum {
        if (!defaults.TryGetProperty(property, out JsonElement element)) { return null; }
        if (element.ValueKind == JsonValueKind.String && Enum.TryParse(element.GetString(), true, out TEnum parsed)) { return parsed; }

        errors.Add(ThemeErrors.MalformedSection(
                       $"defaults.{property}",
                       $"one of {string.Join(", ", Spelling.All<TEnum>())}"));

        return null;
    }

    private static int? ReadOptionalInt(JsonElement owner, string property, List<DomainError> errors) {
        if (!owner.TryGetProperty(property, out JsonElement element)) { return null; }
        if (element.ValueKind == JsonValueKind.Number && element.TryGetInt32(out int value)) { return value; }

        errors.Add(ThemeErrors.MalformedSection($"defaults.{property}", "a whole number"));

        return null;
    }

    /// <param name="owner">The object the property sits in.</param>
    /// <param name="property">The key to read.</param>
    /// <param name="errors">Where a malformed value is reported.</param>
    /// <param name="prefix">
    ///     What the report calls the section, when the key is not at the top level. Unlike
    ///     <see cref="ReadOptionalInt" />, which only ever serves "defaults", this one is shared with
    ///     "allowSmall" - so the caller says where the key lives rather than the reader assuming it.
    /// </param>
    private static bool? ReadOptionalBoolean(JsonElement owner, string property, List<DomainError> errors, string prefix = "") {
        if (!owner.TryGetProperty(property, out JsonElement element)) { return null; }
        if (element.ValueKind is JsonValueKind.True or JsonValueKind.False) { return element.GetBoolean(); }

        errors.Add(ThemeErrors.MalformedSection(prefix + property, "true or false"));

        return null;
    }

    #endregion

    #region Constructors & Destructor

    /// <param name="pool">
    ///     The run's shared intern pool. A fresh one per serializer would only deduplicate within a single
    ///     file.
    /// </param>
    public JsonThemeSerializer(StringInternPool? pool = null) {
        Pool = pool ?? new StringInternPool();
    }

    #endregion

    /// <summary>The pool every string read here passes through.</summary>
    public StringInternPool Pool { get; }

    /// <param name="name">The theme name, which comes from the file name rather than the JSON.</param>
    /// <param name="json">The raw file contents.</param>
    public ThemeParseResult Deserialize(string name, string json) {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(json);

        JsonDocument document;
        try {
            document = JsonDocument.Parse(json);
        } catch (JsonException malformed) {
            return new ThemeParseResult(
                null,
                [ThemeErrors.MalformedJson(malformed.Message, malformed.LineNumber)],
                false);
        }

        using (document) {
            return ReadTheme(name, document.RootElement);
        }
    }

    /// <param name="name">The theme name, which comes from the file name rather than the JSON.</param>
    /// <param name="stream">The raw file contents.</param>
    public ThemeParseResult Deserialize(string name, Stream stream) {
        ArgumentNullException.ThrowIfNull(stream);

        using StreamReader reader = new(stream);

        return Deserialize(name, reader.ReadToEnd());
    }

    private ThemeParseResult ReadTheme(string name, JsonElement root) {
        if (root.ValueKind != JsonValueKind.Object) {
            return new ThemeParseResult(
                null,
                [ThemeErrors.MalformedSection("(document)", "an object")],
                false);
        }

        List<DomainError> errors           = [];
        bool              adjectivesUsable = TryReadWordGroups(root, "adjectives", true, errors, out Dictionary<string, IReadOnlyList<string>> adjectives);
        TryReadWordGroups(root, "participles", false, errors, out Dictionary<string, IReadOnlyList<string>> participles);
        bool                                      nounsUsable  = TryReadNouns(root, errors, out List<NounEntry> nouns);
        ThemeDefaults                             defaults     = ReadDefaults(root, errors);
        bool                                      allowSmall   = ReadOptionalBoolean(root, "allowSmall", errors) ?? false;
        Dictionary<string, IReadOnlyList<string>> incompatible = ReadIncompatibilities(root, errors);

        Theme theme = new(name, adjectives, participles, nouns, defaults, allowSmall) {
            Incompatible = incompatible,
            MaxLength    = ReadMaxLength(root, errors),
            Metadata     = ReadMetadata(root, errors)
        };

        return new ThemeParseResult(theme, errors, adjectivesUsable && nounsUsable);
    }

    private bool TryReadWordGroups(JsonElement                                   root,
                                   string                                        section,
                                   bool                                          required,
                                   List<DomainError>                             errors,
                                   out Dictionary<string, IReadOnlyList<string>> groups) {
        int before = errors.Count;
        groups = ReadWordGroups(root, section, required, errors);

        return errors.Count == before;
    }

    private bool TryReadNouns(JsonElement root, List<DomainError> errors, out List<NounEntry> nouns) {
        bool present = root.TryGetProperty("nouns", out JsonElement element) && element.ValueKind == JsonValueKind.Array;
        nouns = ReadNouns(root, errors);

        return present;
    }

    private Dictionary<string, IReadOnlyList<string>> ReadWordGroups(JsonElement       root,
                                                                     string            section,
                                                                     bool              required,
                                                                     List<DomainError> errors) {
        Dictionary<string, IReadOnlyList<string>> groups = new(StringComparer.Ordinal);

        if (!root.TryGetProperty(section, out JsonElement element)) {
            if (required) {
                errors.Add(ThemeErrors.MalformedSection(section, "an object of category to words"));
            }

            return groups;
        }

        if (element.ValueKind != JsonValueKind.Object) {
            errors.Add(ThemeErrors.MalformedSection(section, "an object of category to words"));

            return groups;
        }

        foreach (JsonProperty category in element.EnumerateObject()) {
            if (category.Value.ValueKind != JsonValueKind.Array) {
                errors.Add(ThemeErrors.MalformedSection($"{section}.{category.Name}", "an array of strings"));

                continue;
            }

            List<string> words = [];
            foreach (JsonElement word in category.Value.EnumerateArray()) {
                if (word.ValueKind != JsonValueKind.String) {
                    errors.Add(ThemeErrors.MalformedSection($"{section}.{category.Name}", "an array of strings"));

                    break;
                }

                string canonical = Take(word.GetString());
                if (canonical.Length == 0) {
                    errors.Add(ThemeErrors.MalformedSection($"{section}.{category.Name}", "an array of words, each holding a letter or a digit"));

                    break;
                }

                words.Add(canonical);
            }

            groups[Pool.Intern(category.Name)] = words;
        }

        return groups;
    }

    /// <summary>
    ///     "incompatible": an object of adjective to the participles it refuses beside it. Both the
    ///     key and the words go through the same normalization as the word lists, for the reason
    ///     <see cref="ReadExclusions" /> gives: a pair that missed on casing would fail open, and a
    ///     pair that fails open is worse than no pair at all.
    /// </summary>
    private Dictionary<string, IReadOnlyList<string>> ReadIncompatibilities(JsonElement root, List<DomainError> errors) {
        Dictionary<string, IReadOnlyList<string>> pairs = new(StringComparer.Ordinal);

        if (!root.TryGetProperty("incompatible", out JsonElement element)) { return pairs; }

        if (element.ValueKind != JsonValueKind.Object) {
            errors.Add(ThemeErrors.MalformedSection("incompatible", "an object of adjective to participles"));

            return pairs;
        }

        foreach (JsonProperty entry in element.EnumerateObject()) {
            string adjective = Take(entry.Name);
            if (adjective.Length == 0) {
                errors.Add(ThemeErrors.MalformedSection("incompatible", "keys that each hold a letter or a digit"));

                continue;
            }

            if (ReadRefusedParticiples(entry, errors) is { } refused) {
                pairs[adjective] = refused;
            }
        }

        return pairs;
    }

    private List<string>? ReadRefusedParticiples(JsonProperty entry, List<DomainError> errors) {
        string section = $"incompatible.{entry.Name}";
        if (entry.Value.ValueKind != JsonValueKind.Array) {
            errors.Add(ThemeErrors.MalformedSection(section, "an array of participles"));

            return null;
        }

        List<string> refused = [];
        foreach (JsonElement word in entry.Value.EnumerateArray()) {
            if (word.ValueKind != JsonValueKind.String) {
                errors.Add(ThemeErrors.MalformedSection(section, "an array of participles"));

                return null;
            }

            string canonical = Take(word.GetString());
            if (canonical.Length == 0) {
                errors.Add(ThemeErrors.MalformedSection(section, "an array of words, each holding a letter or a digit"));

                return null;
            }

            refused.Add(canonical);
        }

        return refused;
    }

    private List<NounEntry> ReadNouns(JsonElement root, List<DomainError> errors) {
        List<NounEntry> nouns = [];

        if (!root.TryGetProperty("nouns", out JsonElement element) || element.ValueKind != JsonValueKind.Array) {
            errors.Add(ThemeErrors.MalformedSection("nouns", "an array of { value, categories }"));

            return nouns;
        }

        int index = 0;
        foreach (JsonElement entry in element.EnumerateArray()) {
            if (entry.ValueKind != JsonValueKind.Object) {
                errors.Add(ThemeErrors.MalformedNoun(index, "not an object"));
                index++;

                continue;
            }

            if (!entry.TryGetProperty("value", out JsonElement value)
             || value.ValueKind != JsonValueKind.String
             || string.IsNullOrWhiteSpace(value.GetString())) {
                errors.Add(ThemeErrors.MalformedNoun(index, "no non-empty \"value\""));
                index++;

                continue;
            }

            // Normalization reduces anything that is not a letter or a digit to a boundary, so a
            // value written entirely of punctuation passes the check above and arrives empty.
            string canonical = Take(value.GetString());
            if (canonical.Length == 0) {
                errors.Add(ThemeErrors.MalformedNoun(index, $"\"{value.GetString()}\" holds no letter or digit"));

                continue;
            }

            nouns.Add(new NounEntry(canonical, ReadCategories(entry, index, errors)) {
                Exclusions = ReadExclusions(entry, index, errors)
            });
            index++;
        }

        return nouns;
    }

    /// <summary>
    ///     Read through the same normalization as the word lists, so "Boring" written here matches
    ///     "boring" declared there - an exclusion that missed on casing would fail open.
    /// </summary>
    private List<string> ReadExclusions(JsonElement entry, int index, List<DomainError> errors) {
        if (!entry.TryGetProperty("except", out JsonElement exclusions)) { return []; }

        if (exclusions.ValueKind != JsonValueKind.Array) {
            errors.Add(ThemeErrors.MalformedNoun(index, "\"except\" is not an array"));

            return [];
        }

        List<string> words = [];
        foreach (JsonElement word in exclusions.EnumerateArray()) {
            if (word.ValueKind != JsonValueKind.String) {
                errors.Add(ThemeErrors.MalformedNoun(index, "\"except\" holds something other than a string"));

                break;
            }

            words.Add(Take(word.GetString()));
        }

        return words;
    }

    private List<string> ReadCategories(JsonElement entry, int index, List<DomainError> errors) {
        if (!entry.TryGetProperty("categories", out JsonElement categories)) { return []; }

        if (categories.ValueKind != JsonValueKind.Array) {
            errors.Add(ThemeErrors.MalformedNoun(index, "\"categories\" is not an array"));

            return [];
        }

        List<string> names = [];
        foreach (JsonElement category in categories.EnumerateArray()) {
            if (category.ValueKind != JsonValueKind.String) {
                errors.Add(ThemeErrors.MalformedNoun(index, "\"categories\" holds something other than a string"));

                break;
            }

            names.Add(Pool.Intern(category.GetString()!));
        }

        return names;
    }

    private string Take(string? value) {
        return Pool.Intern(WordNormalizer.Canonicalize(value ?? string.Empty));
    }

}