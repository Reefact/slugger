using Slugger.Domain;

namespace Slugger.UnitTests;

/// <summary>
/// A random source that hands back a written-down sequence, so a test can say exactly which
/// noun, which adjective and which token digit a generation draws.
/// </summary>
/// <remarks>
/// Every draw is checked against the bound it was asked for. A test whose script no longer
/// matches the order the generator draws in fails on the mismatch rather than quietly
/// asserting something else - which is the failure mode a plain queue would have.
/// </remarks>
internal sealed class ScriptedRandomSource : IRandomSource
{
    private readonly Queue<int> _draws;

    internal ScriptedRandomSource(params int[] draws) => _draws = new Queue<int>(draws);

    /// <summary>How many scripted draws are still unused, so a test can assert it consumed them all.</summary>
    internal int Remaining => _draws.Count;

    public int Next(int exclusiveUpperBound)
    {
        if (_draws.Count == 0)
        {
            throw new InvalidOperationException(
                $"The generator asked for a draw below {exclusiveUpperBound}, and the script has none left.");
        }

        int drawn = _draws.Dequeue();
        if (drawn >= exclusiveUpperBound)
        {
            throw new InvalidOperationException(
                $"The script offers {drawn} where the generator asked for a draw below {exclusiveUpperBound}.");
        }

        return drawn;
    }
}
