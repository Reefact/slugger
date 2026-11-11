#region Usings declarations

using Slugger.Errors;

#endregion

namespace Slugger.UnitTests;

/// <summary>A key is found by its name, so a name is what it cannot do without.</summary>
public sealed class ErrorContextKeyTests {

    [Fact]
    public void Is_named_as_it_was_declared() {
        // Setup
        string name = Dummies.AnyWord();

        // Exercise
        ErrorContextKey<string> key = new(name);

        // Verify
        Assert.Equal(name, key.Name);
        Assert.Equal(name, key.ToString());
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Refuses_a_blank_name(string name) {
        // Exercise
        Assert.Throws<ArgumentException>(() => new ErrorContextKey<string>(name));
    }

}
