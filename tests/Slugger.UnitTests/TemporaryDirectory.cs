namespace Slugger.UnitTests;

/// <summary>
///     A directory of its own per test, removed afterwards, so nothing leaks between them. Held
///     rather than inherited: a sealed test class disposing a field is the whole pattern, where an
///     abstract base would owe the virtual one.
/// </summary>
internal sealed class TemporaryDirectory : IDisposable {

    #region Constructors & Destructor

    internal TemporaryDirectory() {
        Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"slugger-{Guid.NewGuid():N}");
        Directory.CreateDirectory(Path);
    }

    #endregion

    internal string Path { get; }

    public void Dispose() {
        if (Directory.Exists(Path)) {
            Directory.Delete(Path, true);
        }
    }

    /// <summary>Writes a theme that clears every rule, so a test can be about the file system rather than validation.</summary>
    /// <param name="name">The theme name, which the file is named after.</param>
    internal string WriteValidTheme(string name) {
        string path = System.IO.Path.Combine(Path, $"{name}.json");
        File.WriteAllText(path, ThemeFiles.Valid());

        return path;
    }

}