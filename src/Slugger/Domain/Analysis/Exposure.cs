namespace Slugger.Domain.Analysis;

/// <summary>How many nouns can draw one word - the spread between a common word and a rare one.</summary>
/// <param name="Word">The word.</param>
/// <param name="Nouns">How many nouns reach it.</param>
internal sealed record Exposure(string Word, int Nouns);