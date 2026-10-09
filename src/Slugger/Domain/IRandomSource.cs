namespace Slugger.Domain;

/// <summary>
///     Where every random draw of generation comes from. <see cref="DefaultRandomSource" /> is the one to
///     use; implement this interface yourself to script the draws in a test.
/// </summary>
/// <remarks>
///     Which call decides what, and in which order, is an implementation detail that may change between
///     versions of the library. A scripted source written against it belongs in your own tests, ready to
///     be updated.
/// </remarks>
public interface IRandomSource {

    /// <summary>
    ///     Returns a whole number from zero up to, but not including, <paramref name="exclusiveUpperBound" />.
    /// </summary>
    /// <param name="exclusiveUpperBound">How many values the draw chooses among.</param>
    int Next(int exclusiveUpperBound);

}