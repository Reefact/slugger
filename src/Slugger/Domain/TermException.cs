#region Usings declarations

using Slugger.Errors;

#endregion

namespace Slugger.Domain;

/// <summary>What <see cref="TermError" /> raises, so a caller can catch this concept by its own name.</summary>
/// <remarks>
///     <b>Not thrown by the engine yet.</b> It belongs to <see cref="Term" />, part of an ongoing
///     refactoring of the library's vocabulary, and may change or disappear before loading or
///     generation use it. It stays internal until they do.
/// </remarks>
internal sealed class TermException : DiagnosableException {

    #region Constructors & Destructor

    internal TermException(TermError error) : base(error) { }

    #endregion

}
