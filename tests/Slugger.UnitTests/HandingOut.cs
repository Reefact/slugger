#region Usings declarations

using Slugger.Domain;

#endregion

namespace Slugger.UnitTests;

/// <summary>
///     A theme built in memory, handed out the way <see cref="Themes" /> hands out a loaded one - so a
///     test can give the public generation API a theme no file describes. It is not marked as accepted
///     at load: a <see cref="Slugger.Domain.Generation.SlugGenerator" /> still checks it (DEC0031).
/// </summary>
internal static class HandingOut {

    #region Static members

    internal static SluggerTheme HandedOut(this ThemeDocument document) {
        return new SluggerTheme(document);
    }

    #endregion

}
