namespace Slugger.Cli.UnitTests;

/// <summary>
/// Walks the chain a library consumer walks - the facade down to the embedded resources -
/// through the single reference the spec promises them.
/// </summary>
public sealed class FacadeTests
{
    [Fact]
    public void Lists_the_built_in_themes()
    {
        // Exercise
        IReadOnlyList<string> names = Themes.ListEmbedded();

        // Verify
        Assert.Equal(["docker", "heroku", "slugger"], names);
    }
}
