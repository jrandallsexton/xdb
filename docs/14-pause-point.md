# 14 - Pause Point (2026-10-26)

Status: **paused.** Both missions are complete: the legacy application was revived and runs as a comparison oracle (Phase 1), and it is ported to .NET 10 with its UI served by the new API (Phase 2). Work stopped on purpose, until there is a clearer direction for what XDB should become as a product. Nothing is half-built; `main` builds and every test passes (531). This document is the starting point for whoever picks it up again.

## 1. Where things stand

| Area | State |
|---|---|
| Phase 1: original running as-is | Done (2026-10-21). Kept privately with the golden-master captures (`10-plan-run-as-is.md`). |
| Phase 2: .NET 10 port | Done (2026-10-23, M0-M9). Wire contract reproduced and verified by replay; UI copied into `src/ui` and served by the API (`11-phase2-scope.md`). |
| After Phase 2 | Fix forward: defects are fixed in XDB as they are found, not reproduced from the original. |
| sportDeets import | Working end to end for NCAAFB: Venue, Franchise (+ seasons), Person, Football Athlete (+ seasons), Coach (+ seasons), every season the source holds (`13-import-venue.md`). |
| UI | The original jQuery UI, sanitized; the app's own scripts and styles are readable sources (`src/ui/js/xdb.*.js`, `src/ui/css/xdb.*.css`), not minified. Charts is in every user's menu. |
| Repository | Public. PRs #1-#5 merged. No branch protection on `main` and no second review bot yet (planned once work resumes). |

Imported data at the pause (database `XdbSportDeets`):

| Type | Definitions | Instances |
|---|---|---|
| Venue | 919 | - |
| Franchise | 903 | 19,881 seasons |
| Person | 227,216 | - |
| Football Athlete | 226,940 | 619,955 seasons |
| Coach | 276 | 1,683 seasons |

## 2. Resuming: how to run it

`src/Xdb.Api` serves the API (`/legacy/Service.svc/...`) and the UI (site root) on http://localhost:5280. Pick the environment deliberately: a plain `dotnet run` uses the first launch profile (`Xdb.Api`, Development), which has no configured current user, so every page shows "You do not currently have access".

| Launch profile | Environment | Database | Use |
|---|---|---|---|
| `Xdb.Api (Local)` | Local | `XdbLocal`, created by the migrations | Barebones install: no assets, one "Users" view |
| `Xdb.Api (sportDeets)` | SportDeets | `XdbSportDeets` | The UI over the imported sportDeets data |
| `Xdb.Api (sportDeets import)` | SportDeets | `XdbSportDeets` | `import-sportdeets`: adds what is missing, skips existing assets |
| `Xdb.Api (sportDeets reset + import)` | SportDeets | `XdbSportDeets` | `import-sportdeets --reset`: drops the database first, then imports everything (estimated 20 minutes on SSD; not timed end to end) |
| `Xdb.Api (sportDeets regenerate-all)` | SportDeets | `XdbSportDeets` | Rebuilds every generated table, view and procedure |
| `Xdb.Api`, `Xdb.Api (UI)` | Development | From user secrets | Replays against the original; the UI against an existing database |

From a shell, choose the environment and skip the launch profile, for example `dotnet run --no-launch-profile -- import-sportdeets` with `ASPNETCORE_ENVIRONMENT=SportDeets` set (without `--no-launch-profile` the first profile's environment wins). The import refuses to run against any database but `XdbSportDeets`. The Producer connection string comes from the user-secrets store `xdb-sportdeets` (`SportDeetsProducer`); it is read-only access to the sportDeets Producer database.

Tests: `dotnet test` from the repository root runs the unit suites and the integration suite (SQL Server, a fresh database per fixture).

## 3. Known gaps and open items

**Data gaps exposed by the import** (sportDeets side; surfaced, not patched):

- `CoachSeason.Title` holds the coach's last name on every row (a mapping bug); `Coach.Title` and `CoachSeasonRecord` are empty. Most coaches have no birth date (204 of 276) or experience (242 of 276). ESPN offers little coach data and no history, so coaches are complete for now.
- 13 athletes have no display, first or last name; they, their Person and their 14 seasons are rejected and logged on every run.
- Athletes and coaches are never linked in the source, so each brings its own Person. 104 coaches share a first and last name with an athlete; these are candidates for a curated link, not proof.
- Birth dates are present for very few athletes; 179 franchises have no venue.

**XDB items, none started:**

1. **Gen-table rebuild is the slow stage.** Generated tables call the scalar function `dbo.PropertyValue` once per cell; rebuilding the athlete type took 389 s of an 11-minute run. A set-based (pivot) select in the generator would cut it sharply; it changes generated SQL, so it needs the schema-compare and replay checks.
2. **Four foreign keys are disabled.** The baseline creates `FK_Assets_Assets`, `FK_PropertyValueSubmittals_Assets`, `FK_PropertyValueSubmittals_PickListValues` and `FK_PropertyValues_Assets` `WITH NOCHECK` and disabled (inherited). Enabling them needs a data check first; until then the write handlers' own checks are the guard.
3. **Next sportDeets type: Contest** (a game; definition-only, with Home, Away and Venue as references).
4. **Boolean data type** is deferred to the new UI; imported flags use a fixed Yes/No pick list (`13-import-venue.md` 7.5).
5. **Country -> State pick list dependency** not set up.
6. **No import map table.** `13-import-venue.md` section 6 planned `ImportedAssets` for change detection; the import instead reuses sportDeets ids as asset ids and skips assets that exist, so changed source rows are not updated.
7. **No production minification** of the UI scripts; add it as a publish step if it is ever wanted.
8. **The import lives in `Program.ImportSportDeets.cs`** and reads the Producer database directly; `09` and `12` describe where it should eventually live (an export contract, or a live source).

## 4. Lessons worth keeping

- **Model for the links the domain has, not the ones the source stores.** People are one `Person` asset referenced by role types; an XDB asset has exactly one type, so a type hierarchy would split one human into unrelated assets.
- **A definition is the lasting identity, an instance is one occurrence of it** (franchise / season, athlete / season), not the legacy "definition = kind" reading.
- **The UI caches its menu in the browser** until `User_Modified` reports the member changed; anything that changes what a member sees (a new view, a menu rule) must touch `Members.LastModified` or the change stays invisible.
- **Dapper's untyped `ExecuteScalar` maps a SQL NULL to null**, so it cannot tell NULL from "no row". The port's scalar helpers read a row instead.
- **Properties created by the new slices have a NULL `SystemTypeId`**, where the original data never did; code ported against the original data may assume otherwise.
- **Write-heavy databases belong on SSD.** Bulk inserts into a heap with random GUID keys are random I/O: the same import ran about 12x slower on a hard disk. The import database uses SIMPLE recovery; it can always be rebuilt.
- **Bulk copy must check constraints** (`SqlBulkCopyOptions.CheckConstraints`), or SQL Server marks the foreign keys untrusted.
- **Verify a change with the real stack.** Several bugs only showed in the UI against imported data (the chart property list), never against the original's data.

## 5. Document map

| Document | Contents |
|---|---|
| `00-original-system.md` | What the original system was and how it worked |
| `07-design-lessons.md` | Patterns worth reusing, mistakes not to repeat |
| `09-concept-sportdeets-overlay.md` | Concept: XDB as a link explorer over sportDeets data |
| `10-plan-run-as-is.md` | Phase 1 plan: run the original as-is |
| `11-phase2-scope.md` | Phase 2: the .NET 10 port, milestone by milestone |
| `12-concept-live-source.md` | Thinking aloud: XDB over a live source database (two-way) |
| `13-import-venue.md` | The sportDeets import: approach, mapping, modelling decisions |
| `14-pause-point.md` | This document |
