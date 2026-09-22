---
name: slugger
description: Run the slugger CLI directly with the arguments given after /slugger, unchanged. Use when the user wants to try out or test the CLI (e.g. "/slugger --theme docker --count 3", "/slugger --list-themes").
---

Run the following command from the repository root, passing the skill's arguments through verbatim after the `--`:

```bash
dotnet run --project src/Slugger.Cli -- {{args}}
```

If no arguments were given, run the command with no arguments after `--` (i.e. `dotnet run --project src/Slugger.Cli --`).

Do not reinterpret, reorder, or validate the arguments — pass them exactly as typed. Show the command's raw stdout/stderr output to the user.
