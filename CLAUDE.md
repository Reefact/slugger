# Working on slugger

The rules that apply to everyone who works here — how to build and test, the pre-push check, the
code style, the tests, value objects and errors, layering, documentation, commits, branches, pull
requests, mutation testing and releasing — live in the documents imported below, written for
people. Each rule is written once, there; this file adds only what is specific to an agent.

@CONTRIBUTING.md
@docs/mutation-testing.md
@docs/releasing.md

## Specific to agents

### A new rule goes where a person would read it

A house rule that applies to everyone is written in `CONTRIBUTING.md`, or in the document it
belongs to — never here, and never in a code comment. This file takes only what concerns an agent
and nobody else.

### The coding-rules hook, and what it cannot see

`.claude/hooks/coding-rules.sh` runs after every `Edit` or `Write` of a `.cs` file
(`.claude/settings.json`, `PostToolUse`, matcher `Edit|Write`) and reports back to you the two rules
a script can check: a short guard clause written on three lines, and a blank line between two
consecutive guards.

**It only sees what the file-editing tools write.** It reads the path out of that tool's payload,
so a file written by a shell redirection never reaches it — which is most of them when an agent
writes with a heredoc. `sh .claude/hooks/coding-rules.sh --all` sweeps the tree instead, and
belongs in your pre-push check beside the build and the suite, as `CONTRIBUTING.md` lists it.
Measured: the sweep found one violation the per-edit hook had never been shown.

### The branch name proposed at session start

`.claude/hooks/session-start-branch-name.sh` draws a slug at the start of every session and
proposes `claude/<slug>` as the branch. Switch to it before starting work, unless a branch was
explicitly assigned for the task. `CONTRIBUTING.md` (*Branch*) says why branches are named this way.

### Pull request descriptions carry no Claude Code footer

Neither the `🤖 Generated with Claude Code` line nor the `https://claude.ai/code/session_…` link.
Leaving it out is not enough: the pull-request creation tool adds its own, so
`.github/workflows/strip-claude-footer.yml` removes it afterwards, and from a comment too when a
`[session:…]` marker precedes it, since the marker already says a session wrote it. What it
recognises is in `.github/scripts/claude-footer.mjs`, pinned by
`node --test .github/scripts/claude-footer.test.mjs`, which the `ci` workflow runs.

### The review skills are imported, never edited

`.claude/skills/review-pull-request` (for the reviewer) and `.claude/skills/respond-to-review` (for
the author) are copied unchanged from the `skills/` directory of
[`Reefact/conventional-comments-toolkit`](https://github.com/Reefact/conventional-comments-toolkit).
Update them by copying the toolkit's directories again, never by editing them here. Use them to
write or answer a review in Conventional Comments.
