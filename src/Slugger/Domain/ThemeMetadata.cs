namespace Slugger.Domain;

/// <summary>
///     The <c>meta</c> block of a theme file: descriptive information about the theme itself, never
///     consulted by generation. <c>null</c> always means "this theme says nothing about it".
/// </summary>
/// <remarks>
///     Identity stays with the file name (see <see cref="ThemeDocument.Name" />): <see cref="Title" /> is a
///     display label, not a key, and nothing in the catalog or the lookup path reads it.
/// </remarks>
public sealed record ThemeMetadata {

    #region Static members

    /// <summary>A theme that declares no metadata at all.</summary>
    public static ThemeMetadata Empty { get; } = new();

    #endregion

    /// <summary>A human-friendly label to show alongside the file name, e.g. "Docker".</summary>
    public string? Title { get; init; }

    /// <summary>What the theme is or where it is meant to be used.</summary>
    public string? Description { get; init; }

    /// <summary>The theme's own version, free-form - never compared or enforced.</summary>
    public string? Version { get; init; }

    /// <summary>Who wrote the theme.</summary>
    public string? Author { get; init; }

    /// <summary>Where the theme comes from, e.g. a repository or a page.</summary>
    public string? Source { get; init; }

    /// <summary>
    ///     When the theme was first written, free-form - never parsed as a date. A copy that outlives
    ///     its git history has nowhere else to keep this.
    /// </summary>
    public string? CreatedAt { get; init; }

    /// <summary>When this <see cref="Version" /> was published, free-form - same reason as <see cref="CreatedAt" />.</summary>
    public string? PublishedAt { get; init; }

}