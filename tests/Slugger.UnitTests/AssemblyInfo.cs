#region Usings declarations

using JustDummies.Xunit;

#endregion

// Every test in this assembly draws its arbitrary values from a pinned seed, reported only
// when the test goes red. Values still vary between runs - which is what surfaces a test
// secretly leaning on one - but a failure names the seed that reproduces it.
[assembly: Reproducible]