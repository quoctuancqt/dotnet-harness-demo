# Project Memory

Index of architectural and process decisions for HarnessDemo, with the *why* behind them — not obvious from reading the code alone. See `CLAUDE.md` for operational rules (build/test commands, hard constraints).

Each decision is its own numbered file under [`docs/decisions/`](docs/decisions/). This file stays a short index so it's cheap to read in full before proposing changes; add a one-line entry here whenever a new decision file is added, and don't duplicate a decision's content back into this index.

## Decisions

- [0001 - Automation scope](docs/decisions/0001-automation-scope.md) — scoped-down hooks/subagents setup vs. the full harness-engineering doc pattern.
- [0002 - User registration storage](docs/decisions/0002-user-registration-storage.md) — in-memory, dependency-free `/register` implementation (no DB, no OAuth).

## Test coverage

- `/weatherforecast` has integration test coverage via `WebApplicationFactory<Program>` in `WeatherForecastEndpointTests.cs` (added via the `test-writer` subagent). `Program.cs` carries a `public partial class Program {}` shim to support this.
- `/register` has integration test coverage in `RegisterEndpointTests.cs` (success path, each validation branch, duplicate-username conflict).
- `/health` has no dedicated test coverage yet.
