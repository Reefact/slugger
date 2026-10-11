namespace Slugger.Infrastructure.Serialization;

/// <summary>
///     Which key slugger reads an unknown one most likely misspells: the same key in another case,
///     or one a slip or two away - a letter dropped, added or changed, or two letters swapped.
/// </summary>
internal static class KeySpelling {

    /// <summary>
    ///     Past two slips a guess is likelier wrong than right, and a key reported bare is better than
    ///     one matched to a word its author never meant.
    /// </summary>
    private const int MostSlips = 2;

    #region Static members

    /// <summary>The known key this one most likely misspells, or null when none is close enough to name.</summary>
    /// <param name="key">The key, as the file writes it.</param>
    /// <param name="known">The keys slugger reads at that place, in the order a tie should favour.</param>
    internal static string? Nearest(string key, IReadOnlyList<string> known) {
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(known);

        string  written = key.ToUpperInvariant();
        string? nearest = null;
        int     fewest  = MostSlips + 1;
        foreach (string candidate in known) {
            int slips = Slips(written, candidate.ToUpperInvariant());
            if (slips >= fewest) { continue; }
            if (!IsCloseEnough(slips, candidate)) { continue; }

            nearest = candidate;
            fewest  = slips;
        }

        return nearest;
    }

    /// <summary>
    ///     A short key has fewer letters to spare: one slip in three letters is already a different
    ///     word, so the slips allowed grow with the key, one for every three letters and never past
    ///     <see cref="MostSlips" />.
    /// </summary>
    private static bool IsCloseEnough(int slips, string candidate) {
        int allowed = Math.Min(MostSlips, candidate.Length / 3);

        return slips <= allowed;
    }

    /// <summary>
    ///     How many letters must be dropped, added, changed or swapped with their neighbour to turn
    ///     one spelling into the other - the optimal string alignment distance.
    /// </summary>
    private static int Slips(string written, string candidate) {
        int[][] distance = [.. Enumerable.Range(0, written.Length + 1).Select(_ => new int[candidate.Length + 1])];
        for (int row = 0; row <= written.Length; row++) {
            distance[row][0] = row;
        }

        for (int column = 0; column <= candidate.Length; column++) {
            distance[0][column] = column;
        }

        for (int row = 1; row <= written.Length; row++) {
            for (int column = 1; column <= candidate.Length; column++) {
                int dropped  = distance[row - 1][column] + 1;
                int added    = distance[row][column - 1] + 1;
                int replaced = distance[row - 1][column - 1] + (written[row - 1] == candidate[column - 1] ? 0 : 1);
                int best     = Math.Min(Math.Min(dropped, added), replaced);
                if (EndsOnASwap(written, candidate, row, column)) {
                    best = Math.Min(best, distance[row - 2][column - 2] + 1);
                }

                distance[row][column] = best;
            }
        }

        return distance[written.Length][candidate.Length];
    }

    /// <summary>
    ///     Whether the last two letters of each prefix are the same two letters the other way round -
    ///     "pe" against "ep" - which costs one slip rather than the two a pair of changes would.
    /// </summary>
    private static bool EndsOnASwap(string written, string candidate, int row, int column) {
        if (row < 2 || column < 2) { return false; }
        if (written[row - 1] != candidate[column - 2]) { return false; }

        return written[row - 2] == candidate[column - 1];
    }

    #endregion

}
