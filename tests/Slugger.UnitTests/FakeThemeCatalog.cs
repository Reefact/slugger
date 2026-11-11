#region Usings declarations

using Slugger.Application.Abstractions;
using Slugger.Domain;
using Slugger.Errors;
using Slugger.Infrastructure.Serialization;

#endregion

namespace Slugger.UnitTests;

/// <summary>A catalog holding themes given to it, so a use case can be exercised without a disk.</summary>
internal sealed class FakeThemeCatalog(params ThemeDocument[] themes) : IThemeCatalog {

    #region Fields

    private readonly Dictionary<string, ThemeDocument> _themes = themes.ToDictionary(theme => theme.Name, StringComparer.Ordinal);

    #endregion

    /// <summary>Names this catalog should claim to carry but refuse to load, standing in for a broken file.</summary>
    internal HashSet<string> Broken { get; } = new(StringComparer.Ordinal);

    public bool Contains(string name) {
        return _themes.ContainsKey(name) || Broken.Contains(name);
    }

    /// <summary>
    ///     A real catalog hands out only a theme its load accepted, and says so on the theme (DEC0031):
    ///     the fake says it too, standing in for the load its ready-built themes skipped.
    /// </summary>
    public Outcome<ThemeDocument> Load(string name, bool allowSmall = false) {
        if (Broken.Contains(name)) { return ThemeLoader.Refuse(name, [SluggerThemeError.TooFewNouns(1, 100)]); }
        if (!_themes.TryGetValue(name, out ThemeDocument? theme)) { return ThemeLoader.Refuse(name, [SluggerThemeError.NotFound(name, ListNames())]); }

        theme.MarkAcceptedAtLoad();

        return Outcome<ThemeDocument>.Success(theme);
    }

    /// <summary>A fake holds ready-built themes, so there is no validation step to skip - same as <see cref="Load" />.</summary>
    public Outcome<ThemeDocument> Parse(string name) {
        return Load(name);
    }

    public IReadOnlyList<string> ListNames() {
        return [.. _themes.Keys.Concat(Broken).Order(StringComparer.Ordinal)];
    }

}