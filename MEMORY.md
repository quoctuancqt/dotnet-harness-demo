# Project Memory

Architectural and process decisions for HarnessDemo, with the *why* behind them — not obvious from reading the code alone. See `CLAUDE.md` for operational rules (build/test commands, hard constraints). Add an entry here whenever a non-obvious decision is made; don't duplicate what's already visible by reading the code or `.claude/` config directly.

## Automation scope (2026-09-22)

Implemented the automations recommended by `AI Harness Engineering & Claude Code.md`, but deliberately scoped down rather than building the full pattern each section describes:

- **Hooks** (`.claude/settings.json`): `PreToolUse` blocks edits to `appsettings.Production.json` and CI workflow files; `PostToolUse` runs `dotnet format` only.
  - Decided **not** to extend `PostToolUse` to also run `dotnet build`/`dotnet test` (the doc's full "Verify-and-Repair Loop", Part 2 §3) — would add multi-second latency to every single file edit in a repo this small.
- **Subagents**: `reviewer` (`.claude/agents/reviewer.md`, git diff + test output → Pass/Fail) and `test-writer` (`.claude/agents/test-writer.md`, xUnit coverage generation). Tested end-to-end: `reviewer` correctly passed a clean change and failed a deliberately weakened assertion.
  - Decided **not** to add a `Researcher` agent or wire an automatic critique-and-refine retry loop (the doc's full Part 3 §5 pattern) — called "enough for now" given the repo's size.
- **Revisit this scope** if the repo grows (more endpoints, more contributors) or slow feedback loops start costing more than the latency they'd add.

## Test coverage

- `/weatherforecast` has integration test coverage via `WebApplicationFactory<Program>` in `WeatherForecastEndpointTests.cs` (added via the `test-writer` subagent). `Program.cs` carries a `public partial class Program {}` shim to support this.
- `/register` has integration test coverage in `RegisterEndpointTests.cs` (success path, each validation branch, duplicate-username conflict).
- `/health` has no dedicated test coverage yet.

## User registration (2026-09-22)

`POST /register` (`Program.cs`) stores users in an in-memory `ConcurrentDictionary` (via a singleton `UserStore`), hashed with PBKDF2 using only built-in `System.Security.Cryptography` APIs — no OAuth provider and no new package dependency, per explicit request to keep it simple.

- **Why**: no database is wired up in this repo yet, and the ask was for a minimal, dependency-free implementation.
- **Not durable**: registered users are lost on restart. Revisit with real persistence (and a login/auth flow, which doesn't exist yet either) before this is anything but a demo.
