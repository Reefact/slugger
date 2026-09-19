namespace Slugger;

/// <summary>
/// The reason behind every analyzer suppression in this assembly, written once.
/// </summary>
/// <remarks>
/// <para>
/// A suppression's rule and category go through DiagnosticCatalog so the compiler resolves
/// them; leaving the third argument as a literal would keep exactly the problem that solves -
/// a string nothing validates, retyped at each site and free to drift between them. These are
/// constants, so a typo is a build error and a reason is stated in one place rather than seven.
/// </para>
/// <para>
/// Each one has to stand on its own: a reader meeting a suppression should learn why it is
/// acceptable without opening anything else. What is specific to one type - which member its
/// body will read - belongs to that type's own documentation, not repeated here.
/// </para>
/// </remarks>
internal static class SuppressionJustifications
{
    /// <summary>
    /// For S2325 on the types still being scaffolded. Their bodies throw, so nothing reads
    /// instance state yet and the rule is right about the code as it stands - and wrong about
    /// where it is going. Each type documents the member its implementation will read, and the
    /// suppression is meant to be deleted with the stub rather than kept.
    /// </summary>
    internal const string ScaffoldedStub =
        "Scaffolding: the body still throws, so nothing reads instance state yet. The type's own "
        + "documentation names what the implementation will read; this suppression goes with the stub.";

    /// <summary>For S2245 on the random source behind slug generation.</summary>
    internal const string NotASecurityContext =
        "Slug generation is not a security context: the output names things, it never authenticates "
        + "or authorises anything. A cryptographic generator would also defeat --seed, whose entire "
        + "purpose is that the same seed replays the same slugs.";

    /// <summary>For S3218 on the nested error codes.</summary>
    internal const string CodesMirrorTheirFactories =
        "Each constant deliberately carries the name of the factory it belongs to, which is what makes "
        + "ThemeErrors.Codes.PoolTooSmall read next to ThemeErrors.PoolTooSmall(...). The shadowing the "
        + "rule guards against cannot bite here: nothing inside Codes refers to those names unqualified, "
        + "and every use from outside spells the full path.";
}
