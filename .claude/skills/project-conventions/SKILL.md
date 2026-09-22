---
name: project-conventions
description: Coding and architectural conventions for HarnessDemo. Load automatically when writing or reviewing C# code in this repository (endpoints, tests, DI, error handling).
user-invocable: false
---

# HarnessDemo Project Conventions

## Endpoints (`src/HarnessDemo.Api`)
- This is a Minimal API project (net10.0). Endpoints are currently declared inline in `Program.cs` via `app.MapGet`/`app.MapPost`, each with a `.WithName(...)` call.
- As the endpoint count grows, extract route groups into `Endpoints/<Feature>Endpoints.cs` with a static `MapXxxEndpoints(this WebApplication app)` extension method, and call it from `Program.cs`. Do not extract prematurely for a single endpoint.
- Nullable reference types are enabled (`<Nullable>enable</Nullable>`) — do not suppress with `!` unless a comment explains why the compiler can't prove non-null.
- Prefer typed results (`Results.Ok(...)`, `TypedResults`) over anonymous objects once an endpoint's response shape stabilizes; anonymous objects (as used today for `/health`) are fine for simple, stable responses.

## Tests (`tests/HarnessDemo.Api.Tests`)
- xUnit with `[Fact]`/`[Theory]`. The test project references the API project directly (no separate contracts package).
- Name test classes after the endpoint or type under test (e.g. `WeatherForecastEndpointTests`), not generic names like `UnitTest1` — replace the placeholder `UnitTest1.cs` the first time a real test is added for that area.
- New endpoints should ship with at least one test covering the success path before being considered done, per `CLAUDE.md`'s "write or run a reproducing test first" rule for bug fixes.

## Formatting & verification
- Run `dotnet format HarnessDemo.slnx` before considering C# changes complete (also auto-applied via the PostToolUse hook in `.claude/settings.json`).
- Verify with `dotnet build HarnessDemo.slnx` then `dotnet test HarnessDemo.slnx` — see `CLAUDE.md` for exact commands.

## Hard constraints (also in CLAUDE.md — repeated here for emphasis)
- Never modify `appsettings.Production.json` or CI configuration without explicit approval (also enforced by a PreToolUse hook).
- Never delete or weaken existing test assertions to make a failing test pass.
