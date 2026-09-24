namespace Slugger;

/// <summary>
///     The one way a value object gives up what it holds, in primitives. Marked rather than merely
///     named so that the door is greppable, countable, and checked - a value object answers
///     questions everywhere else, and this says where the exception is.
/// </summary>
/// <remarks>
///     Ours, like <see cref="SemanticObjectAttribute" /> and unlike
///     <see cref="ValueObjectAttribute" />: the pattern catalogue has no dehydration, so this one
///     is not waiting on a package.
///
///     It is the inverse of the type's factory: what <c>From</c> hydrates, this dehydrates, and a
///     value that goes through both comes back unchanged. A composite dehydrates to its parts
///     dehydrated, which is why a slug gives back the terms and the token a formatter already
///     takes.
/// </remarks>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
internal sealed class DehydrationMethodAttribute : Attribute { }
