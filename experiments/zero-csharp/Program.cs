// A Docker-style name generator in Zero C#: an adjective, a separator, a scientist's surname.
//
//   zeroslugger                 one name, different on every run
//   zeroslugger --count 3       three names
//   zeroslugger --seed 42       the same names on every run and every target
//   zeroslugger --sep -         humming-dirac instead of humming_dirac
//
// What it is: a size experiment, not a port. It draws from the docker theme the real command
// embeds, with the same words, and nothing else of slugger: no categories, no participles, no
// token, no other theme, no JSON. Its random generator is Zero C#'s own, so a seed gives other
// names than `slugger --seed` does.

using System;

namespace ZeroSlugger;

public static class Program
{
    const string Help =
        "zeroslugger: Docker-style names, e.g. humming_dirac\n" +
        "  --count N   how many names to print (default 1)\n" +
        "  --seed N    fixed seed, same names every run (default: drawn from the clock)\n" +
        "  --sep C     the separator, one character (default _)\n" +
        "  --oneshot   accepted and ignored: this command always draws once and exits\n" +
        "  --help      this text";

    public static int Main(string[] args)
    {
        int count = 1;
        ulong seed = 0;
        bool seeded = false;
        char separator = '_';

        for (int i = 0; i < args.Length; i++)
        {
            string arg = args[i];
            if (arg == "--help") { Out.Line(Help); return 0; }
            if (arg == "--oneshot") { continue; }

            if (i + 1 >= args.Length) { return Refuse("a value is missing after " + arg); }

            string value = args[i + 1];
            i++;

            if (arg == "--count")
            {
                if (!Str.TryParse(value.AsSpan(), out count) || count < 1) { return Refuse("--count needs a whole number from 1"); }
            }
            else if (arg == "--seed")
            {
                if (!Str.TryParse(value.AsSpan(), out ulong parsed)) { return Refuse("--seed needs a whole number"); }

                seed = parsed;
                seeded = true;
            }
            else if (arg == "--sep")
            {
                if (value.Length != 1) { return Refuse("--sep needs exactly one character"); }

                separator = value[0];
            }
            else
            {
                return Refuse("unknown option " + arg);
            }
        }

        Random random = seeded ? new Random(seed) : Random.FromClock();

        for (int n = 0; n < count; n++)
        {
            ReadOnlySpan<char> adjective = Pick(DockerWords.Adjectives, DockerWords.AdjectiveCount, ref random);
            ReadOnlySpan<char> noun = Pick(DockerWords.Nouns, DockerWords.NounCount, ref random);

            Out.Write(adjective);
            Out.Write(separator);
            Out.Write(noun);
            Out.Line();
        }

        Out.Flush();

        return 0;
    }

    /// <summary>The word at a drawn position in a list of single-space-separated words.</summary>
    static ReadOnlySpan<char> Pick(string words, int wordCount, ref Random random)
    {
        int target = random.Next(wordCount);
        int index = 0;

        foreach (ReadOnlySpan<char> word in words.Split(' '))
        {
            if (index == target) { return word; }

            index++;
        }

        return default;
    }

    /// <summary>
    /// Says why on standard output, since this build has no error stream, and returns the exit code
    /// for a refused command line.
    /// </summary>
    static int Refuse(string reason)
    {
        Out.Line("zeroslugger: " + reason + ". Try --help.");

        return 2;
    }
}
