namespace Slugger.Domain;

/// <summary>
///     A key a theme file holds where slugger reads no such key, and the one it most likely
///     misspells. Never a refusal (DEC0027): a key a later version adds must not stop an earlier one
///     from loading the file, so the key is reported and the file still loads.
/// </summary>
/// <param name="Path">Where the key sits - <c>nouns[0]</c>, <c>defaults</c> - or empty at the root of the file.</param>
/// <param name="Key">The key, as the file writes it.</param>
/// <param name="Meant">The key slugger reads there that this one most likely misspells, or null when none is close.</param>
internal sealed record UnknownKey(string Path, string Key, string? Meant);
