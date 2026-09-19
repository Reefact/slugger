# Working on slugger

## Build and test

```bash
dotnet build
dotnet test
dotnet run --project src/Slugger.Cli -- --theme docker --count 3
dotnet run --project src/Slugger.Cli -- --list-themes
dotnet run --project src/Slugger.Cli -- --register ./porno.json
```

The warning ratchet is scoped to CI, following the chapter's convention, so a local build stays
friendly to a half-finished refactoring. **No CI workflow is wired yet** — until one exists,
`GITHUB_ACTIONS=true dotnet build` is how you get the answer CI will give. Run it before pushing.

## Writing a unit test

Every test is split into three commented blocks, in this order. A block with nothing to say is
left out rather than written empty — most tests have no setup worth naming.

```csharp
[Fact]
public void Glues_a_token_straight_onto_the_last_segment()
{
    // Setup
    GenerationOptions options = new() { Separator = '_', TokenGlued = true };

    // Exercise
    string slug = SlugFormatter.Format(["focused", "turing"], "3", options);

    // Verify
    Assert.Equal("focused_turing3", slug);
}
```

- **Name the behaviour, not the method.** `Glues_a_token_straight_onto_the_last_segment`, not
  `Format_Returns_Correct_Value`. The name is read in a failure report by someone who did not
  write it.
- **xUnit v3 and `Assert`.** No fluent assertion library: it is what the rest of the chapter's
  repositories use, and it is one dependency the test projects do not carry.
- **Explicit types, never `var`** — `IDE0008` is a warning here, as everywhere else in the repo.
- **A value the test does not care about is drawn, not written.** See below.
- **A comment earns its place by saying why, never what.** `// Setup` blocks explain the shape
  of the fixture when it is not obvious; a comment restating the next line is noise.

### Testing internals

`Application` and `Infrastructure` are `internal`, and the test projects reach them through
`InternalsVisibleTo` — declared in `Slugger.csproj` and `Slugger.Cli.csproj`. **Test an internal
type directly rather than only through the facade**: twenty-five of the twenty-seven internal types
are covered that way today - the two left are the CLI entry point and a class of constants.

One C# rule bites, and it is worth knowing before you hit it: **a public method may not name an
internal type in its signature**, and xUnit v3 discovers only public test classes and public test
methods — an `internal` class or method silently runs zero tests, with no error to explain it
(measured, twice).

So a `[Theory]` over an internal type cannot declare it as a parameter. Two ways out, in order of
preference:

1. **Split into named `[Fact]`s.** Usually better anyway: `Force_applies_the_drawn_themes_style_even_among_several()`
   reads in a failure report where a table row does not.
2. **Widen the signature, keep the data typed.** `TheoryData<object, ...>` holds real values and
   the method casts them back:

```csharp
public static TheoryData<object, int, bool> Cases => new()
{
    { MimicStyle.Force, 3, true },   // written typed, where a mistake is caught
};

[Theory]
[MemberData(nameof(Cases))]
public void The_flag_decides(object flag, int themesInScope, bool applies) =>
    Assert.Equal(applies, OptionResolver.AppliesTheStyleOf((MimicStyle)flag, themesInScope));
```

Never make a type public just to test it. That publishes it forever to get a table today.

### Arbitrary values

A literal reads as load-bearing whether or not it is, so nobody dares change one and the test
only ever covers that case. Where the value genuinely does not matter, draw it with
[JustDummies](https://github.com/Reefact/just-dummies), stating the invariant it must satisfy:

```csharp
int seed = Any.Int32().Between(1, 100_000).Generate();
string word = Dummies.AnyWord();   // lowercase letters, 3 to 10 of them
```

`Dummies` holds the shapes this codebase treats as arbitrary — add to it rather than repeating a
constraint. **A constraint states a domain invariant, never what the test asserts.**

`[assembly: Reproducible]` pins every case to a seed that is reported **only when the test goes
red**, so values vary between runs — which is what surfaces a test secretly leaning on one — and
a failure is still replayable value for value.

Keep a literal when the literal *is* the case, and say so:

```csharp
/// <summary>
/// Literal on purpose: the accent is the whole point of the case, so an arbitrary value would
/// say nothing.
/// </summary>
[Fact]
public void Preserves_accents_instead_of_transliterating_them() =>
    Assert.Equal("rené dupont", WordNormalizer.Canonicalize(" René     Dupont "));
```

### Controlling randomness

Generation draws through `IRandomSource`. When a test needs an exact draw, use
`ScriptedRandomSource`, which hands back a written-down sequence and **fails on a mismatch**
rather than quietly asserting something else:

```csharp
ScriptedRandomSource random = new(0, 1);   // first noun, second adjective
```

Use `DefaultRandomSource(seed)` instead when the test is about a distribution rather than a
single draw, and assert a band rather than an exact count.

### What to assert on

- Prefer the real theme files over a fixture when the point is the shipped data: running the
  actual rules over `docker.json` is what caught that it contradicted the spec.
- Pin a spec claim to the file that has to honour it, and say which claim in the summary. Those
  tests are how a spec correction gets noticed instead of silently drifting.

## Suppressing an analyzer warning

Fix it first. A warning that is right about the code gets a fix, not an attribute — `S2325` and
`CA1859` were both real findings.

When the rule is genuinely wrong about this code, suppress it through the catalogue, never with
literal strings:

```csharp
[SuppressMessage(
    SonarRule.S2325.Category,
    SonarRule.S2325.Id,
    Justification = SuppressionJustifications.ScaffoldedStub)]
```

All three arguments are compile-checked constants, so a typo is a build error rather than a
suppression that silently matches nothing. Add the reason to `SuppressionJustifications` rather
than writing it inline; if an existing one fits, reuse it.

Check the suppression carries its weight: remove it, confirm the warning comes back.

## Layering

`Slugger` is one assembly with three namespaces. Dependencies point inwards only, and
`NamespaceDependencyTests` fails if `Domain` ever names something from `Application` or
`Infrastructure`.

`Application` and `Infrastructure` are `internal` — they are how the engine is built, not what it
offers. The CLI and the tests reach them through `InternalsVisibleTo`. Publishing a type later is
easy; unpublishing one is a breaking change.

## Commits

[Conventional Commits](https://www.conventionalcommits.org), one intent per commit. The branch
must be green when it becomes mergeable, not every commit along the way.
