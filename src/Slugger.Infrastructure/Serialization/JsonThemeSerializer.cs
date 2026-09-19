using Slugger.Domain;

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
public sealed class JsonThemeSerializer
{
    public JsonThemeSerializer(StringInternPool? pool = null) => Pool = pool ?? new StringInternPool();

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
