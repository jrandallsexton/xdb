# XDB - a metadata-driven object repository, rebuilt

XDB is the successor to a metadata-driven object repository I designed and built in 2010-2014 (referred to here as "the original system" or "XDB v1"): EAV assets and properties, Definition/Instance types, submittal-based history and approval, SQL compiled from metadata, a jQuery View Engine UI and a WCF REST backend. This repository is about rebuilding it on .NET 10 with, eventually, a React UI.

## Status (2026-10-23)

- **Phase 1 - DONE (2026-10-21).** The original's deployed release build runs locally as the comparison oracle: jQuery View Engine UI unmodified, WCF REST service on .NET Framework 4.8, against a working copy of the production backup. Six HAR captures were recorded as the golden master (kept privately; they contain non-publishable data).
- **Phase 2 - DONE (2026-10-23, M0-M9).** The API is ported to .NET 10 with the WCF wire contract, verified by replaying the captures against both the original and the port and diffing the responses. The original jQuery UI is copied into this repository (`src/ui`, sanitized) and served by the API. Version reset to **0.1.0**; from here on, defects are fixed in XDB as they are found rather than reproduced from the original.
- Later: simplify, then React - as an explorer over data imported from sportDeets.

## Running it locally

`src/Xdb.Api` serves both the API (`/legacy/Service.svc/...`) and the UI (site root, from `src/ui`) on http://localhost:5280. Launch profiles:

| Profile | Database | Use |
|---|---|---|
| `Xdb.Api (Local)` | `XdbLocal`, created from nothing by the migrations | A barebones install: no assets, one "Users" view for the configured first administrator (`appsettings.Local.json`; user secrets are not loaded) |
| `Xdb.Api (UI)` | From user secrets | The UI against an existing database, with the service addresses pointed at the port |
| `Xdb.Api` | From user secrets | Service addresses from user secrets (used when replaying captures against the original) |

See `docs/10-plan-run-as-is.md` for the phased plan and `docs/07-design-lessons.md` for what is worth keeping from the original design.

| Document | Contents |
|---|---|
| `docs/00-original-system.md` | What the original system was and how it worked (confirmed accurate by its author) |
| `docs/07-design-lessons.md` | Patterns worth reusing, mistakes not to repeat, outstanding housekeeping |
| `docs/09-concept-sportdeets-overlay.md` | Concept: XDB as a link explorer over sportDeets data (export/import, Definition/Instance mapping, first slice) |
| `docs/10-plan-run-as-is.md` | Plan: run the original as-is on .NET Fx 4.8, then port the API to .NET 10 with the same wire contract |
| `docs/11-phase2-scope.md` | Phase 2 scope: what the .NET 10 port covers, wire-contract rules, architecture, baseline migration, comparison harness, milestones and sizing |

## What is not in this repository

The excavation notes (source-tree analysis, database inventory, screenshot catalogue), the golden-master captures, the original source, the database backups and the running legacy deployment all live outside this repo. They contain material that is not publishable (former-employer vocabulary and real data) and will stay private. Where the docs here cite them, they say "(private excavation notes, not published)".
