namespace Slugger.Cli.Tests;

/// <summary>
/// Walks the whole chain a library consumer walks - Core to Infrastructure to the embedded
/// resources - through the one reference the spec promises them.
/// </summary>
public class FacadeTests
{
    [Fact]
    public void The_facade_lists_the_built_in_themes() =>
        Themes.ListEmbedded().ShouldBe(["docker", "heroku", "slugger"]);
}
