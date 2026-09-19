using Slugger.Domain;
using System.Diagnostics.CodeAnalysis;
using DiagnosticCatalog.Sonar;

namespace Slugger.Infrastructure.Serialization;

/// <summary>
/// Reads a theme file into the domain model with System.Text.Json, which ships with the SDK
/// and so keeps this layer free of external NuGet dependencies.
/// </summary>
/// <remarks>
/// Two things happen on the way in, both invisible to the theme's author: every value is put
/// through <see cref="Domain.Normalization.WordNormalizer"/>, and every string goes through
/// the shared <see cref="StringInternPool"/>.
/// </remarks>
[SuppressMessage(
    SonarRule.S2325.Category,
    SonarRule.S2325.Id,
    Justification = "Scaffolding: the bodies still throw, so the shared Pool is not read yet. Every load has to intern through it, which is the whole reason the pool is handed in rather than created per file.")]
public sealed class JsonThemeSerializer
{
    /// <param name="pool">The run's shared intern pool. A fresh one per serializer would only deduplicate within a single file.</param>
    public JsonThemeSerializer(StringInternPool? pool = null) => Pool = pool ?? new StringInternPool();

    /// <summary>The pool every string read here passes through.</summary>
    public StringInternPool Pool { get; }

    /// <param name="name">The theme name, which comes from the file name rather than the JSON.</param>
    /// <param name="json">The raw file contents.</param>
    public Theme Deserialize(string name, string json) => throw new NotImplementedException();

    /// <param name="name">The theme name, which comes from the file name rather than the JSON.</param>
    /// <param name="stream">The raw file contents.</param>
    public Theme Deserialize(string name, Stream stream) => throw new NotImplementedException();

    /// <summary>Writes a theme back out, for the copy performed by <c>--register</c>.</summary>
    public string Serialize(Theme theme) => throw new NotImplementedException();
}
