# HarnessDemo Operational Playbook

## Build & Test Commands
- Build: `dotnet build HarnessDemo.slnx`
- Run API: `dotnet run --project src/HarnessDemo.Api`
- Run all tests: `dotnet test HarnessDemo.slnx`
- Run a single test: `dotnet test tests/HarnessDemo.Api.Tests --filter FullyQualifiedName~<TestName>`
- Format: `dotnet format HarnessDemo.slnx`

## Architecture & Code Flow
- `src/HarnessDemo.Api` is an ASP.NET Core Minimal API (net10.0). Endpoints are currently defined inline in `Program.cs`.
- `tests/HarnessDemo.Api.Tests` is an xUnit project referencing the API project directly.

## Hard Constraints
- Do NOT modify `appsettings.Production.json` or any CI configuration without explicit approval.
- Do NOT delete or weaken existing test assertions to make a failing test pass.
- When fixing a bug, write or run a reproducing test first.

## Memory

- Architectural and process decisions and their rationale live in `docs/decisions/`, one numbered file per decision (`0001-*.md`, `0002-*.md`, ...), checked into git and visible to the whole team. `MEMORY.md` (repo root) is a short index linking to each one — read it before proposing changes that might revisit a past decision.
- When you make a non-obvious call, add a new `docs/decisions/NNNN-<slug>.md` file (next sequential number) and a one-line entry linking it from `MEMORY.md`. Don't duplicate a decision's content back into `MEMORY.md`, and don't duplicate what's already derivable from the code or `.claude/` config.
- `MEMORY.md` also keeps a running "Test coverage" list — that's status, not a decision, so update it in place rather than adding a new decision file.
