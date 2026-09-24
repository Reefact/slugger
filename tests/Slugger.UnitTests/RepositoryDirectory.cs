#region Usings declarations

using System.Reflection;

#endregion

namespace Slugger.UnitTests;

/// <summary>
///     Where the repository is, for the tests that answer for its files rather than for a fixture.
/// </summary>
/// <remarks>
///     Read from the assembly rather than from where the test happens to be running. Walking up to
///     slugger.slnx would find the repository from the ordinary build output and from nowhere else,
///     so a tool that copies that output elsewhere to run it - KillMutants sandboxes every test run -
///     loses the directory, and every test that reads a repository file then fails for a reason that
///     has nothing to do with what it is testing (measured).
/// </remarks>
internal static class RepositoryDirectory {

    #region Static members

    /// <summary>The repository's <c>themes/</c>, which is not staged into the test output.</summary>
    /// <remarks>
    ///     A copy staged at build time would answer for the state of the last build, where the point
    ///     of these guards is the state of the repository as it stands.
    /// </remarks>
    internal static string Themes => Path.Combine(Root, "themes");

    private static string Root => typeof(RepositoryDirectory).Assembly
                                                             .GetCustomAttributes<AssemblyMetadataAttribute>()
                                                             .FirstOrDefault(attribute => attribute.Key == "RepositoryRoot")
                                                            ?.Value
                               ?? throw new InvalidOperationException(
                                      "The test assembly carries no RepositoryRoot, so the repository's files cannot be "
                                    + "found. It is written in by Slugger.UnitTests.csproj at build time.");

    /// <summary>The theme files, ordered so a failure reports them the same way twice.</summary>
    internal static string[] ThemeFiles() {
        return [.. Directory.EnumerateFiles(Themes, "*.json").Order(StringComparer.Ordinal)];
    }

    #endregion

}