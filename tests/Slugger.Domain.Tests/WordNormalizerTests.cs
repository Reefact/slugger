using Slugger.Domain.Normalization;

namespace Slugger.Domain.Tests;

public class WordNormalizerTests
{
    [Theory]
    [InlineData("  gorgeous  ", "gorgeous")]
    [InlineData("GORGEOUS", "gorgeous")]
    [InlineData("John     Doe", "john doe")]
    [InlineData(" John     Doe             ", "john doe")]
    [InlineData("", "")]
    [InlineData("   ", "")]
    public void Trims_collapses_runs_of_spaces_and_lowercases(string raw, string expected) =>
        WordNormalizer.Canonicalize(raw).ShouldBe(expected);

    [Fact]
    public void Preserves_accents_instead_of_transliterating_them() =>
        WordNormalizer.Canonicalize(" René     Dupont ").ShouldBe("rené dupont");

    [Fact]
    public void Leaves_the_separator_alone_because_it_belongs_to_the_formatter() =>
        WordNormalizer.Canonicalize("John Doe").ShouldBe("john doe");
}
