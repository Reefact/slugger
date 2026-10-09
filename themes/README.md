# Repository themes

The themes `slugger` offers without building them in. Three themes are compiled into the program —
`slugger`, `heroku` and `docker`, whose files are in
[src/Slugger/Infrastructure/Resources/](../src/Slugger/Infrastructure/Resources/) — and are always
available. The ones in this folder are ordinary theme files: you analyse them, register them, copy
them, and read them as examples.

## Using one

Download a file and register it:

```bash
curl -sSfLO https://raw.githubusercontent.com/Reefact/slugger/main/themes/mineralogy.json
slugger --register ./mineralogy.json
slugger --theme mineralogy --count 3 --oneshot
```

From a clone of the repository, you can draw from the folder without registering anything:

```bash
slugger --theme-dir themes --theme mineralogy --count 3 --oneshot
slugger --theme-dir themes --theme '*' --count 3 --oneshot   # every theme here, plus the built-in ones
```

`--theme-info mineralogy` prints what a theme says about itself. Installing `slugger` is described
in [the README](../README.md#install), and writing a theme of your own in
[docs/writing-a-theme.md](../docs/writing-a-theme.md).

## What the repository guarantees

Three tests read **every `.json` in this folder**, so a theme that breaks a rule is a red build
rather than a surprise for whoever downloads it:

- `RepositoryThemeTests` loads each file with the ordinary rules — without `--allow-small-theme`.
  A theme below the floors is refused here as it would be on your machine.
- `DrawnSlugTests` draws from each theme and checks every slug it draws with the theme's own
  categories, `except` and `incompatible`.
- `ThemeGoldenMasterTests` pins what each theme draws under a set of seeded options, one file per
  theme in [tests/Slugger.Cli.UnitTests/GoldenMaster/](../tests/Slugger.Cli.UnitTests/GoldenMaster/).

The floors work like a ratchet: the day one of them goes up, these themes grow with it or leave the
folder. That is deliberate — a catalogue holding files that the tool would refuse is worth nothing.

## Analysis reports are not in git

`--analyze` writes `<theme>-analysis.md` next to the file it measures, and git ignores those reports
in this folder: they can only follow the theme and the rules, never lead them. Generate a fresh one
rather than looking for it in the history:

```bash
slugger --analyze themes/mineralogy.json
```

## Versions and dates

Nothing checks `meta.version`, `meta.createdAt` or `meta.publishedAt` when a theme loads; they are
free text. The discipline is entirely up to whoever edits the file:

- **Only the theme that changes moves.** Changing `mineralogy.json` moves its `version` and its
  `publishedAt`; the other themes have no reason to move with it.
- **`createdAt` never moves** after the first publication: it dates the theme, not its last edit.
- **`publishedAt` moves with every new `version`.** A theme whose content has not changed has no
  reason to move its `version`, and so none to move its `publishedAt` either.
- **`meta.source` is the URL of the file on `main`**:
  `https://github.com/Reefact/slugger/blob/main/themes/<name>.json`. It tells whoever holds a copy
  where the original lives.

Most of the themes here were first published together, which is why they share `1.0.0` and one of
two dates. That is a coincidence of the first release, not a rule to keep: `french-gastronomy` has
already moved on alone.

## Proposing a new theme

The general process — forking, branches, pull requests, the checks that run — is in
[CONTRIBUTING.md](../CONTRIBUTING.md). For a theme, check each of these before opening the pull
request:

- [ ] **The file is `themes/<name>.json`**, with a name in lowercase letters, digits and hyphens —
      no comma, and not the name of a theme that already exists here or of a built-in one
      (`slugger`, `heroku`, `docker`).
- [ ] **`slugger --analyze themes/<name>.json` says it is accepted as it is**, without
      `--allow-small-theme`, and the file does not set `"allowSmall": true`. The test that loads
      this folder honours a theme's own `allowSmall`, so that key would let a small theme through
      — and a theme below the floors is not offered here.
- [ ] **The whole folder still draws**: `slugger --theme-dir themes --theme '*' --oneshot`. Mixed
      with other themes, a theme is drawn and measured under the default segment mode, `both`; if
      it declares participles, every noun needs at least 20 of them, whatever its own
      `segmentMode` says.
- [ ] **Its meaning has been reviewed** with [docs/reviewing-a-theme.md](../docs/reviewing-a-theme.md),
      up to a clean pass. Say in the pull request what you did and what you found.
- [ ] **`meta` is filled in**: `title`, `description`, `version` (`1.0.0` for a new theme),
      `author`, `source` following the convention above, and `createdAt` and `publishedAt` set to
      the same date.
- [ ] **`defaults` holds only the theme's own style**, if anything. Do not copy the program's
      defaults (`"sep": "-"`, `"casing": "kebab"`, `"segmentMode": "both"`): they would override
      the saved preferences of whoever draws from your theme. A few themes here still do; do not
      take them as a model on that point.
- [ ] **The golden master exists**: `tests/Slugger.Cli.UnitTests/GoldenMaster/<name>.verified.txt`.
      For a brand-new theme the test fails with only a message — `Theme "rivers" has no golden
      master. Generate rivers.verified.txt and read what it pins before committing it.` — and
      writes nothing. Create an empty file of that name, run the test again so that it writes
      `<name>.received.txt` beside it, read what it drew, and rename it to `.verified.txt`:

      ```bash
      touch tests/Slugger.Cli.UnitTests/GoldenMaster/<name>.verified.txt
      dotnet test --project tests/Slugger.Cli.UnitTests --filter-class '*ThemeGoldenMasterTests'
      mv tests/Slugger.Cli.UnitTests/GoldenMaster/<name>.received.txt \
         tests/Slugger.Cli.UnitTests/GoldenMaster/<name>.verified.txt
      ```

      This needs the .NET 10 SDK and a clone of the repository. If you do not build .NET, say so in
      the pull request and ask a maintainer to generate the file. More on the golden master in
      [CONTRIBUTING.md](../CONTRIBUTING.md#the-golden-master).
- [ ] **The test suite passes**: `dotnet test --solution slugger.slnx`. `RepositoryThemeTests`,
      `DrawnSlugTests` and `ThemeGoldenMasterTests` are the ones a theme can turn red. If you have
      themes registered on your machine, read
      [CONTRIBUTING.md](../CONTRIBUTING.md#build-test-and-run) first: one test reads your theme
      directory.
- [ ] **Nothing generated is committed**: no `*-analysis.md` and no `*.received.txt` (both are
      ignored by git), only the theme and its `.verified.txt`.
- [ ] **The commit follows [Conventional Commits](https://www.conventionalcommits.org)**, for
      example `feat(themes): add rivers`.
- [ ] **The words are yours**, or come from a source whose licence allows it — say which in the pull
      request, so it can be credited.
