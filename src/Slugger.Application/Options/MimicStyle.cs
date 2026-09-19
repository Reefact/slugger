namespace Slugger.Application.Options;

/// <summary>
/// <c>--mimic-style</c> is a three state flag, not the presence of a boolean.
/// </summary>
public enum MimicStyle
{
    /// <summary>Flag absent: the theme's defaults apply when exactly one theme is in scope, and not otherwise.</summary>
    Auto,

    /// <summary>Apply the drawn theme's defaults even in multi-theme mode, varying from draw to draw.</summary>
    Force,

    /// <summary>Never apply them, even for a single theme. Back to slugger's own defaults.</summary>
    Off
}
