namespace Slugger;

/// <summary>
///     A value object whose whole purpose is to say what a value means. It wraps one and gives it
///     up through <c>Value</c>, so the compiler can tell an adjective from a participle where two
///     terms would have passed for each other.
/// </summary>
/// <remarks>
///     Marked apart from <see cref="ValueObjectAttribute" /> because it breaks that pattern's rule
///     deliberately: an ordinary value object answers questions and keeps its value, while this one
///     exists to hand it over. <c>ValueObjectRulesTests</c> measures both, and the rule about the
///     value staying in lives in CLAUDE.md rather than in a test, which is why nothing here has to
///     be relaxed for them.
/// </remarks>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false, Inherited = true)]
internal sealed class SemanticObjectAttribute : Attribute { }
