# XDB - a metadata-driven object repository, rebuilt

XDB is the successor to a metadata-driven object repository I designed and built in 2010-2014 (referred to here as "the original system" or "XDB v1"): EAV assets and properties, Definition/Instance types, submittal-based history and approval, SQL compiled from metadata, a jQuery View Engine UI and a WCF REST backend. This repository is about rebuilding it on .NET 10 with, eventually, a React UI.

## Status (2026-10-26): paused

- **Phase 1 - DONE (2026-10-21).** The original's deployed release build runs locally as the comparison oracle: jQuery View Engine UI unmodified, WCF REST service on .NET Framework 4.8, against a working copy of the production backup. Six HAR captures were recorded as the golden master (kept privately; they contain non-publishable data).
- **Phase 2 - DONE (2026-10-23, M0-M9).** The API is ported to .NET 10 with the WCF wire contract, verified by replaying the captures against both the original and the port and diffing the responses. The original jQuery UI is copied into this repository (`src/ui`, sanitized, readable sources) and served by the API. Version reset to **0.1.0**; from here on, defects are fixed in XDB as they are found rather than reproduced from the original.
- **sportDeets import - working (2026-10-26).** NCAAFB venues, franchises and seasons, people, football athletes and seasons, coaches and seasons are imported from the sportDeets Producer database with a bulk write path (`docs/13-import-venue.md`).
- **Paused (2026-10-26)** until the product direction is clearer. `docs/14-pause-point.md` has the state, how to resume, known gaps and lessons.

## Running it locally

`src/Xdb.Api` serves both the API (`/legacy/Service.svc/...`) and the UI (site root, from `src/ui`) on http://localhost:5280. Launch profiles:

| Profile | Database | Use |
|---|---|---|
| `Xdb.Api (Local)` | `XdbLocal`, created from nothing by the migrations | A barebones install: no assets, one "Users" view for the configured first administrator (`appsettings.Local.json`; user secrets are not loaded) |
| `Xdb.Api (UI)` | From user secrets | The UI against an existing database, with the service addresses pointed at the port |
| `Xdb.Api` | From user secrets | Service addresses from user secrets (used when replaying captures against the original) |
| `Xdb.Api (sportDeets)` and its `import` / `reset + import` / `regenerate-all` variants | `XdbSportDeets` | The UI over imported sportDeets data, and the import itself (`docs/14-pause-point.md` section 2) |

A plain `dotnet run` uses the first profile (`Xdb.Api`, Development), which has no configured current user: every page then says "You do not currently have access". Choose a profile, or set `ASPNETCORE_ENVIRONMENT` and pass `--no-launch-profile`.

See `docs/10-plan-run-as-is.md` for the phased plan and `docs/07-design-lessons.md` for what is worth keeping from the original design.

| Document | Contents |
|---|---|
| `docs/00-original-system.md` | What the original system was and how it worked (confirmed accurate by its author) |
| `docs/07-design-lessons.md` | Patterns worth reusing, mistakes not to repeat, outstanding housekeeping |
| `docs/09-concept-sportdeets-overlay.md` | Concept: XDB as a link explorer over sportDeets data (export/import, Definition/Instance mapping, first slice) |
| `docs/10-plan-run-as-is.md` | Plan: run the original as-is on .NET Fx 4.8, then port the API to .NET 10 with the same wire contract |
| `docs/11-phase2-scope.md` | Phase 2 scope: what the .NET 10 port covers, wire-contract rules, architecture, baseline migration, comparison harness, milestones and sizing |
| `docs/12-concept-live-source.md` | Thinking aloud: XDB over a live source database, two-way |
| `docs/13-import-venue.md` | The sportDeets import: approach, column mapping, definition/instance and Person modelling decisions |
| `docs/14-pause-point.md` | **Start here when resuming:** state at the pause, how to run, known gaps, open items, lessons |

## What is not in this repository

The excavation notes (source-tree analysis, database inventory, screenshot catalogue), the golden-master captures, the original source, the database backups and the running legacy deployment all live outside this repo. They contain material that is not publishable (former-employer vocabulary and real data) and will stay private. Where the docs here cite them, they say "(private excavation notes, not published)".
