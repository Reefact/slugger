namespace Slugger;

/// <summary>
///     The reason behind every analyzer suppression in this assembly, written once.
/// </summary>
/// <remarks>
///     <para>
///         A suppression's rule and category go through DiagnosticCatalog so the compiler resolves
///         them; leaving the third argument as a literal would keep exactly the problem that solves -
///         a string nothing validates, retyped at each site and free to drift between them. These are
///         constants, so a typo is a build error and a reason is stated in one place rather than seven.
///     </para>
///     <para>
///         Each one has to stand on its own: a reader meeting a suppression should learn why it is
///         acceptable without opening anything else. What is specific to one type - which member its
///         body will read - belongs to that type's own documentation, not repeated here.
///     </para>
/// </remarks>
internal static class SuppressionJustifications {

    /// <summary>For S2245 on the random source behind slug generation.</summary>
    internal const string NotASecurityContext =
        "Slug generation is not a security context: the output names things, it never authenticates "
      + "or authorises anything. A cryptographic generator would also defeat --seed, whose entire "
      + "purpose is that the same seed replays the same slugs.";

    /// <summary>For S3218 on the nested error codes.</summary>
    internal const string CodesMirrorTheirFactories =
        "Each constant deliberately carries the name of the factory it belongs to, which is what makes "
      + "SluggerThemeError.Codes.PoolTooSmall read next to SluggerThemeError.PoolTooSmall(...). The shadowing the "
      + "rule guards against cannot bite here: nothing inside Codes refers to those names unqualified, "
      + "and every use from outside spells the full path.";

    /// <summary>For S2326 on the type parameter of an error context key.</summary>
    internal const string TheTypeParameterTypesTheRead =
        "The type parameter is the whole of what the key is for: it is what makes ErrorContext.TryGet "
      + "hand back a fact already typed, so that a key declared for a number cannot read a string. A "
      + "member using it would exist only to quiet the rule.";

}