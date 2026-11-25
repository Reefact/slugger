#!/bin/sh
# Integration tests for zeroslugger: runs the real binary and checks what it prints and returns.
# Usage: integration-tests.sh path/to/ZeroSlugger   (needs jq, like generate-words.sh)
set -u

binary=${1:?usage: integration-tests.sh path/to/ZeroSlugger}
here=$(CDPATH= cd -- "$(dirname -- "$0")" && pwd)
theme="$here/../../src/Slugger/Infrastructure/Resources/docker.json"
work=$(mktemp -d)
trap 'rm -rf "$work"' EXIT

failures=0
checks=0

pass() { checks=$((checks + 1)); }
fail() { checks=$((checks + 1)); failures=$((failures + 1)); echo "FAIL: $1"; }

# expect_eq <name> <expected> <actual>
expect_eq() {
    if [ "$2" = "$3" ]; then pass; else fail "$1: expected [$2], got [$3]"; fi
}

# expect_match <name> <extended regex, whole line> <text>
expect_match() {
    if printf '%s\n' "$3" | grep -Eq "$2"; then pass; else fail "$1: [$3] does not match $2"; fi
}

# run <args...>: stdout in $out, exit code in $code
run() {
    out=$("$binary" "$@" 2>&1)
    code=$?
}

# --- the shape of one name -------------------------------------------------------------------
run --seed 1
expect_eq "one name: exit code" 0 "$code"
expect_match "one name: adjective_noun" '^[a-z]+_[a-z]+$' "$out"
expect_eq "one name: a single line" 1 "$(printf '%s\n' "$out" | wc -l | tr -d ' ')"

# --- --count ---------------------------------------------------------------------------------
run --count 25 --seed 7
expect_eq "--count 25: exit code" 0 "$code"
expect_eq "--count 25: 25 lines" 25 "$(printf '%s\n' "$out" | wc -l | tr -d ' ')"
expect_eq "--count 25: every line is adjective_noun" 25 "$(printf '%s\n' "$out" | grep -Ec '^[a-z]+_[a-z]+$')"

# --- --seed: the same names every time, other names for another seed -------------------------
run --count 10 --seed 42
first=$out
run --count 10 --seed 42
expect_eq "--seed 42 twice: same names" "$first" "$out"
run --count 10 --seed 43
if [ "$first" = "$out" ]; then fail "--seed 43: same names as --seed 42"; else pass; fi
run --count 10
no_seed_a=$out
run --count 10
expect_match "no seed: still well formed" '^[a-z]+_[a-z]+$' "$(printf '%s\n' "$no_seed_a" | head -1)"

# --- --sep -----------------------------------------------------------------------------------
run --sep - --count 5 --seed 3
expect_eq "--sep -: exit code" 0 "$code"
expect_eq "--sep -: every line is adjective-noun" 5 "$(printf '%s\n' "$out" | grep -Ec '^[a-z]+-[a-z]+$')"
run --sep . --seed 3
expect_match "--sep .: adjective.noun" '^[a-z]+\.[a-z]+$' "$out"

# --- --oneshot is accepted and changes nothing ----------------------------------------------
run --seed 5
plain=$out
run --oneshot --seed 5
expect_eq "--oneshot: same as without" "$plain" "$out"

# --- --help ----------------------------------------------------------------------------------
run --help
expect_eq "--help: exit code" 0 "$code"
expect_match "--help: says what the command is" 'zeroslugger' "$out"

# --- the words are the docker theme's, and every one of them can be drawn --------------------
jq -r '.adjectives.common[]' "$theme" | sort > "$work/adjectives.txt"
jq -r '.nouns[].value' "$theme" | sort > "$work/nouns.txt"

"$binary" --count 20000 --seed 99 > "$work/names.txt"
cut -d_ -f1 "$work/names.txt" | sort -u > "$work/drawn-adjectives.txt"
cut -d_ -f2 "$work/names.txt" | sort -u > "$work/drawn-nouns.txt"

expect_eq "no adjective outside the theme" 0 "$(comm -13 "$work/adjectives.txt" "$work/drawn-adjectives.txt" | wc -l | tr -d ' ')"
expect_eq "no noun outside the theme" 0 "$(comm -13 "$work/nouns.txt" "$work/drawn-nouns.txt" | wc -l | tr -d ' ')"
expect_eq "every adjective of the theme is drawn" 0 "$(comm -23 "$work/adjectives.txt" "$work/drawn-adjectives.txt" | wc -l | tr -d ' ')"
expect_eq "every noun of the theme is drawn" 0 "$(comm -23 "$work/nouns.txt" "$work/drawn-nouns.txt" | wc -l | tr -d ' ')"

# --- a refused command line: exit code 2, and it says why ------------------------------------
refused() {
    name=$1
    shift
    run "$@"
    expect_eq "$name: exit code" 2 "$code"
    expect_match "$name: names the program" '^zeroslugger: ' "$out"
}

refused "--count 0" --count 0
refused "--count -3" --count -3
refused "--count abc" --count abc
refused "--seed abc" --seed abc
refused "--sep with two characters" --sep ab
refused "--sep empty" --sep ""
refused "unknown option" --nope 1
refused "missing value" --count

echo "$checks checks, $failures failed"
[ "$failures" -eq 0 ]
