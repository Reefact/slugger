using Slugger.Infrastructure.Serialization;

namespace Slugger.Infrastructure.Tests;

public class StringInternPoolTests
{
    [Fact]
    public void The_same_word_seen_twice_is_handed_back_as_one_instance()
    {
        var pool = new StringInternPool();

        // Built at runtime on purpose: two identical literals would already be the same
        // instance thanks to the runtime's own literal interning, which would make the
        // assertion pass without the pool doing anything.
        var first = pool.Intern(Build());
        var second = pool.Intern(Build());

        second.ShouldBeSameAs(first);
        pool.Count.ShouldBe(1);

        static string Build() => new(['g', 'o', 'r', 'g', 'e', 'o', 'u', 's']);
    }

    [Fact]
    public void Distinct_words_each_get_their_own_entry()
    {
        var pool = new StringInternPool();

        pool.Intern("gorgeous");
        pool.Intern("wandering");

        pool.Count.ShouldBe(2);
    }

    [Fact]
    public void Interning_is_case_sensitive_because_normalization_already_ran()
    {
        var pool = new StringInternPool();

        pool.Intern("gorgeous");
        pool.Intern("Gorgeous");

        pool.Count.ShouldBe(2);
    }
}
