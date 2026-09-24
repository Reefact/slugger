#!/bin/sh
# slugger — coding-rules hook.
#
# Wired from .claude/settings.json on PostToolUse for the file-editing tools, so
# an agent learns it broke a coding rule at the moment it wrote the file — not
# once CI is red, and not once a human is reading the diff.
#
# Which rules live here. A rule belongs in this hook when it is deterministic,
# cheap to check from the edited file alone, and neither dotnet format nor
# jb cleanupcode already enforces it. CLAUDE.md's "Code style" section names the
# guard-clause collapse as exactly that case: KEEP_EXISTING_EMBEDDED_BLOCK_ARRANGEMENT
# does not undo a block someone already wrote as multi-line, so nothing catches
# it without this hook. Judgement calls — is this file's split into two types
# actually earned — stay in prose on purpose.
#
# The hook never rewrites anything: it reads the file that was just written and
# reports. Fixing is the agent's job.
#
# Adding a rule. Write a `rule_<name>` function that prints one line per offence
# (empty output means clean) and a matching `<name>_hint`, then add it to the rule
# list for the file types it applies to, in the dispatch below. Every rule must be
# a pure read of a file already on disk and cost milliseconds: this runs after
# every single edit. Anything needing a build or a solution-wide analysis belongs
# in CI, not here.

set -u

# Swept over the whole tree rather than fired on one edit. The hook reads the file path from the
# harness payload, so a file written by a shell redirection is invisible to it - which is most of
# them when an agent writes with heredocs. CLAUDE.md names this in the pre-push check.
if [ "${1:-}" = '--all' ]; then
  status=0
  for swept in $(find src tests -name '*.cs' -not -path '*/bin/*' -not -path '*/obj/*' 2>/dev/null); do
    printf '{"tool_input":{"file_path":"%s"}}' "$swept" | sh "$0" || status=2
  done
  exit "$status"
fi

# Always drain stdin (the harness pipes the hook payload). Draining avoids any
# broken-pipe noise on the writer side even when we exit early.
payload="$(cat 2>/dev/null || true)"

# The file that was just written. jq when available, a raw scan otherwise, so the
# hook degrades to silence rather than to false positives.
file=''
if command -v jq >/dev/null 2>&1; then
  file="$(printf '%s' "$payload" | jq -r '.tool_input.file_path // empty' 2>/dev/null || true)"
fi
[ -n "$file" ] || exit 0
[ -f "$file" ] || exit 0                 # deleted, moved, or a path we cannot resolve

# Which rules apply to the file that was just written. A file type nobody has a
# rule for exits silently.
case "$file" in
  *.cs) RULES='guard_clause_collapse guard_clause_run' ;;
  *)    exit 0 ;;
esac

display="${file##*/}"

# --- rules --------------------------------------------------------------------

# Guard clause collapse: a three-line "if (cond) { <single statement> }" block
# whose body is nothing but a return, throw, break or continue collapses onto one
# line (CLAUDE.md, "Code style"). Matched as a fixed three-line window rather than
# with a brace-depth parser: a body split across more lines, or followed by a
# blank line before the closing brace, is left alone rather than risk a false
# positive — this rule is meant to catch the common, unambiguous case cheaply.
# shellcheck disable=SC2317  # reached through the `"rule_${rule}"` dispatch in the run section below
rule_guard_clause_collapse() {
  awk -v name="$display" '
    { lines[NR] = $0 }
    END {
      for (i = 1; i <= NR; i++) {
        line = lines[i]
        trimmed = line
        sub(/^[ \t]*/, "", trimmed)
        if (trimmed ~ /^\/\//) continue                        # // comment

        if (line !~ /if[ \t]*\(.*\)[ \t]*\{[ \t]*$/) continue   # not an opening "if (...) {"
        if (i + 2 > NR) continue

        body = lines[i + 1]
        sub(/^[ \t]*/, "", body)
        sub(/[ \t]*$/, "", body)

        closer = lines[i + 2]
        sub(/^[ \t]*/, "", closer)
        sub(/[ \t]*$/, "", closer)

        if (closer == "}" && body ~ /^(return|throw|break|continue)([ \t;].*)?;$/) {
          printf "  %s:%d  %s\n", name, i, body
        }
      }
    }
  ' "$file"
}

# shellcheck disable=SC2317  # reached through the `"${rule}_hint"` dispatch in the run section below
guard_clause_collapse_hint() {
  printf '%s' "Coding rule — guard clause collapse (CLAUDE.md, \"Code style\"). This file now
declares a guard clause across three lines:

${1}
A short guard clause collapses onto one line: \`if (x is null) { return null; }\`
rather than three, whenever the body is a single return, throw, break or continue
with nothing else going on. Neither dotnet format nor jb cleanupcode performs this
collapse, so it is applied by hand.
"
}

# Guard clause run: consecutive one-line guards read as one block, so no blank
# line separates them (CLAUDE.md, "Code style"). Only a blank line between two
# guards is reported - one before the first or after the last separates the block
# from the work around it and is left alone.
# shellcheck disable=SC2317  # reached through the `"rule_${rule}"` dispatch in the run section below
rule_guard_clause_run() {
  awk -v name="$display" '
    function is_guard(line) {
      return line ~ /if[ \t]*\(.*\)[ \t]*\{[ \t]*(return|throw|break|continue)[^}]*;[ \t]*\}[ \t]*$/
    }
    { lines[NR] = $0 }
    END {
      for (i = 1; i + 2 <= NR; i++) {
        blank = lines[i + 1]
        sub(/^[ \t]*$/, "", blank)
        if (blank != "") continue
        if (is_guard(lines[i]) && is_guard(lines[i + 2])) {
          guard = lines[i]
          sub(/^[ \t]*/, "", guard)
          printf "  %s:%d  %s\n", name, i + 1, guard
        }
      }
    }
  ' "$file"
}

# shellcheck disable=SC2317  # reached through the `"${rule}_hint"` dispatch in the run section below
guard_clause_run_hint() {
  printf '%s' "Coding rule — guard clause run (CLAUDE.md, \"Code style\"). This file now puts a
blank line between two one-line guards:

${1}
Consecutive guards read as one block and are written as one, with no blank line
between them. A blank line before the first or after the last is what separates
that block from the work around it.
"
}

# --- run ----------------------------------------------------------------------

report=''
for rule in $RULES; do
  offences="$("rule_${rule}" 2>/dev/null || true)"
  [ -n "$offences" ] || continue
  report="${report}$("${rule}_hint" "$offences")
"
done

[ -n "$report" ] || exit 0               # clean: stay silent

printf '%s' "$report" >&2
exit 2                                   # advisory; PostToolUse surfaces it to the agent
