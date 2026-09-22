# 0002 - User registration storage

Date: 2026-09-22

## Decision

`POST /register` (`Program.cs`) stores users in an in-memory `ConcurrentDictionary` (via a singleton `UserStore`), hashed with PBKDF2 using only built-in `System.Security.Cryptography` APIs — no OAuth provider and no new package dependency, per explicit request to keep it simple.

## Why

No database is wired up in this repo yet, and the ask was for a minimal, dependency-free implementation.

## Consequences

Not durable: registered users are lost on restart. Revisit with real persistence (and a login/auth flow, which doesn't exist yet either) before this is anything but a demo.
