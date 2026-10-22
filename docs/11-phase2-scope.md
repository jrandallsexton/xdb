# 11 - Phase 2 Scope: .NET 10 Port

Status: **approved 2026-10-21; M0, M1 and M2 done (2026-10-21); M3 next.** Builds on `10-plan-run-as-is.md`.

Goal: replace the original WCF REST service with a .NET 10 API that the **unmodified** original jQuery UI can use. Each ported operation must be shown equivalent by replaying recorded traffic against both the original (still running locally) and the port, then diffing the responses. The code follows the conventions of the author's current production system: thin controllers, handlers injected per action, CQRS with validation and `Result<T>`.

## 1. What the port has to cover

The deployed 1.5.0.5 service contract has **299 operations**, all RPC-style (`Service.svc/Op_Name?param=...`), with 251 GET and 48 POST.

| Bucket | Ops | Treatment |
|---|---|---|
| Referenced by UI code | ~249 | Port |
| Config key exists, but no UI code calls it | 11 | Drop. Return 404 like any unknown operation |
| Not referenced anywhere | 28 | Drop, after confirming no background module calls it (a few look like they did) |
| Environment-bound (directory/AD, remote WMI, Windows service control, cross-environment metadata migration) | ~25 | Stub with fixed responses; not ported |
| URLs the UI calls that **no** service version implements | 11 | Must keep returning 404 (the UI tolerates it) |

That leaves about **225 operations to port for real**, in twelve functional areas:

| Area | Ops | Size | Why |
|---|---|---|---|
| Assets and values | 47 | L | Submittal/approval save path, read-table refresh side effects, reads that also write (cache and hit logging) |
| Asset types and properties metadata | 38 | M | Many small reads; inheritance resolution; 4 operations exist only in the 1.5.0.5 binaries and must be decompiled |
| Views, property groups, processes | 35 | M | Mostly CRUD; the cached client view model must serialize byte-for-byte |
| Picklists and lookups | 14 | S | CRUD and cascading lookups |
| Reports | 19 | L | Runtime view DDL, jqGrid paging over dynamic columns, hand-built JSON, xlsx export |
| Charts, dashboards, maps | 16 | M | Several response shapes; one background save |
| Roles and security | 21 | M | Row-security view generation |
| Members and users | 28 | M | Identity rules, directory stubs, parallel fan-out in `UserClient_Get` |
| Submittals, history, triggers | 10 | M | RSS feeds; narrative output |
| Files, images, bulk upload | 17 | L | Custom multipart parsing, 202 plus a polled background job, binary and xlsx streams |
| Search | 7 | S | Few operations, parallel queries |
| Admin and system | 47 | M | About a third are dead or stubbed |

## 2. The wire contract is the hard part

The jQuery UI is very particular about what it accepts. Details the port must reproduce:

- **Routing:**
  - Operation names match case-insensitively.
  - Unknown query parameters are ignored (jQuery's `_=` cache buster, jqGrid's `_search/nd/rows/page/sidx/sord`).
  - A missing parameter binds to its default (`Guid.Empty`, `false`, `0`).
  - A malformed GUID returns 400.
- **Request bodies:** most are bare JSON. 11 operations use a "wrapped" body (`{"paramName": ...}`). 3 are multipart uploads.
- **Two serializers, two casings:**
  - **Typed returns (about 200 operations)** follow the data-contract serializer's rules:
    - Member names exactly as declared: PascalCase for domain objects, camelCase for some client DTOs.
    - Dictionaries as `[{"Key":..,"Value":..}]`.
    - Nulls emitted.
    - Enums as integers.
    - Dates as `"\/Date(ms-0500)\/"` with a local offset, in both requests and responses.
  - **Hand-built JSON (83 operations)** is returned as raw streams:
    - Lowercase `[{"key":..,"value":..}]`.
    - Only `"` escaped.
    - ASCII encoding.
    - 53 of them are sent as `text/html`; the UI parses them anyway.
- **XML responses:** 5 RSS 2.0 feeds, plus xlsx streams.
- **Errors:**
  - A business failure returns **500** with the normal return value as the body and an `ErrorId: <guid>` header. The UI then calls `Error_Get?id=`.
  - An unhandled failure returns 500 `{"Detail": "..."}`.
  - An unknown operation returns the framework's HTML 404.
- **Identity:** 86 operations trust a `userId` supplied by the client; about 30 resolve the user on the server. The port reproduces this **for local use only**. The service must not be exposed beyond localhost until the auth rework (later phase).

## 3. Architecture: clean core, legacy wire adapter

The conventions (thin controllers, CQRS, `Result<T>`) and the wire contract (RPC, odd serialization, errors as 500-with-body) pull in opposite directions. They are kept apart:

```
original jQuery UI ──► Legacy wire controllers ──► handlers (CQRS slices) ──► data
                        /legacy/Service.svc/{Op}     Result<T>                 EF Core / procs
                        LegacyResult formatters
future React UI ─────► Clean REST controllers ───┘  (same handlers, Phase 4+)
```

- **Handlers are written once, cleanly**, the same way as in the current production system:
  - A per-handler interface (`IGetReportDataQueryHandler`) with `Task<Result<T>> ExecuteAsync(query, CancellationToken)`.
  - Queries are positional records; commands use `required init` properties.
  - FluentValidation validators are injected into the handler.
  - Explicit DI registration, one line per handler; validators found by assembly scan.
  - Errors as `Failure<T>` with a `ResultStatus`.
- **Legacy wire controllers stay thin.** Each action takes its handler via `[FromServices]`, builds the query or command, and returns `result.ToLegacyResult(LegacyFormat.X)`, never `ToActionResult()`. The formatters own every wire quirk: date format, casing, dictionary shape, stream versus typed, content type, the ErrorId/500 error convention, and RSS. Controllers are grouped by area, routed under a **neutral base path**; the UI's private config is pointed at it.
- **Later, a clean REST surface for React** sits over the same handlers with normal `ToActionResult()`. Nothing in the handlers knows the legacy wire exists.

**Solution layout** (implicit usings **off** everywhere, nullable on, warnings as errors, central package management):

```
src/Xdb.Api/            Application/<Area>/{Commands|Queries}/<UseCase>/ + Legacy/<Area>WireController.cs
src/Xdb.Core/           Result<T>, IDateTimeProvider, legacy wire formatters, shared DTO conventions
                        Infrastructure/Data/ (DbContext, entities, Migrations/, Scripts/ embedded SQL, generator for RegenerateAll)
tools/Xdb.Compare/      golden-master replay and diff harness (captures read from a private path)
test/unit/Xdb.Api.Tests.Unit/                 xUnit, AutoMocker, FluentAssertions 7, EF InMemory
test/integration/Xdb.Api.Tests.Integration/   WebApplicationFactory + Testcontainers (SQL Server)
```

**Data access:**
- Queries and commands use EF Core (DB-first scaffold, `AsNoTracking`, project to DTOs), with LINQ preferred.
- Where an operation's behavior lives in a non-trivial stored procedure (a multi-result-set read, `ANSI_NULLS OFF` comparison semantics, the filter engine), the handler calls the procedure through a small gateway first. That procedure is then replaced by LINQ once the comparison harness guards the slice.
- Parity first, rewrite second, one slice at a time.

**Background work:**
- The original runs about ten fire-and-forget thread-pool jobs: read-table refresh after asset writes, narrative generation, cache and hit logging, report logs, chart save.
- The port moves them onto an in-process queue (`Channel` + `BackgroundService`). The response contract stays the same and failures are logged instead of lost.
- The bulk-upload job keeps its 202-then-poll contract.
- Durable jobs (Hangfire) wait for Phase 3.

## 4. Baseline migration

The port must first run against the existing working copy, and must also be able to create an empty database from scratch. Facts from the schema analysis:

| Class | Tables | Procs | Functions | Views |
|---|---|---|---|---|
| Core static | 102 | - | - | - |
| Static, called by the app | - | ~306 (about 40 reference tables that no longer exist and fail when called) | 35 | 0 |
| **Generated at runtime** (per-type read tables, their MERGE procs, report views, row-security views) | 178 | 178 | - | 234 |
| Dead | 12 | 97 | 12 | 126 |

Further facts:
- SQL Server, **compatibility level 100**, `SQL_Latin1_General_CP1_CI_AS`.
- 301 of 305 modules were created with `ANSI_NULLS`/`QUOTED_IDENTIFIER` OFF, and their logic depends on it.
- No CLR, Agent jobs, triggers, linked servers or user-defined types.
- Four heaps (no clustered index), including the 1.6M-row values table.
- **No "regenerate all" exists anywhere.** Generated objects are built by C# when a type, report or role is saved.

**Database engine: SQL Server (DECIDED 2026-10-21).** PostgreSQL was considered and rejected for this phase. The original is tied to SQL Server in four ways: ~306 stored procedures (most relying on `ANSI_NULLS OFF` comparison semantics), a C# generator that emits T-SQL DDL at runtime (`SELECT ... INTO`, `MERGE`, views), dependence on the case-insensitive collation and on implicit row order, and a comparison harness that is only meaningful against the same engine and data. Moving engines now would turn the port into a rewrite before equivalence is proven. A later move becomes realistic once the procedures are replaced by LINQ, leaving the generator as the main engine-specific piece.

**Migrations: EF Core, in the API project under `Infrastructure/Data/Migrations`, applied by the application at startup (DECIDED 2026-10-21)**, matching the production system's convention: on startup, check pending migrations, then `MigrateAsync`, then an optional seed step; skipped in the Testing environment. **The baseline is a hand-written EF migration that executes embedded SQL scripts** (`Infrastructure/Data/Scripts/0001-0005`) rather than a migration scaffolded from the model. A scaffolded migration would lose the per-module ANSI settings, invent keys for heaps and rename constraints, which would be a fidelity failure on day one. The script approach keeps the database exact while staying inside the EF migration history. The runtime model is scaffolded DB-first from the baseline-created database and becomes the model snapshot, so later migrations are ordinary `dotnet ef migrations add` diffs. Revisit squashing once the stored procedures are retired.

1. **Reconcile, once, privately.** A script run on a backup of the working copy drops the dead objects and the orphaned or obsolete generated views, renames one branded column, and scrubs author comments. It stays outside the repo because it names what it removes.
2. **Baseline scripts** are generated from the schema extract, filtered by classification, and de-branded:
   - `0001_tables` (no FKs)
   - `0002_keys_defaults_indexes`
   - `0003_functions` (dependency order)
   - `0004_procs`, each preceded by its own `SET ANSI_NULLS`/`QUOTED_IDENTIFIER` batch
   - `0005_seed`: system lookups with fixed GUIDs (the binaries hard-code 172 of them), system roles, and an admin member whose user id comes from config
   - Principals, database name and file names are excluded.
3. **Adopting the existing database:** if the core tables exist and `__EFMigrationsHistory` is empty, startup records the baseline migration as applied instead of running it. That is how the existing working copy adopts the baseline.
4. **`RegenerateAll()`**, ported from the original C# generator, rebuilds the read tables and MERGE procs, then the report views, then the row-security views. Generated objects are never part of migrations.
5. **Verification:**
   - Create a fresh database from the baseline.
   - Compare it against the reconciled copy with generated objects dropped, using a schema compare with ANSI settings and column order **not** ignored. Expect zero differences, plus matching seed checksums.
   - Then run `RegenerateAll()` on the reconciled copy and diff the regenerated objects against the originals.
6. **Compatibility level stays 100** until the harness passes. EF Core is told so via `UseCompatibilityLevel(100)`, so `Contains` doesn't emit `OPENJSON`. Raising it is a later, separate migration.

The EF Core migration generator splits `migrationBuilder.Sql` on `GO` lines, which keeps each `SET ANSI_NULLS`/`QUOTED_IDENTIFIER` batch separate from its `CREATE`. (It also joins lines ending in a backslash; the scripts contain none.)

### M1 result (DONE 2026-10-21)

- **Reconcile** (private script, run on a restored copy of the working database): dropped 131 unreferenced static objects and 120 dead generated views; renamed one branded column. Every drop was checked against the deployed binaries' strings, `sys.sql_expression_dependencies` and the text of every kept module. Five FK lookup tables and two side-app tables that looked unreferenced were kept.
- **Scripts** (`src/Xdb.Api/Infrastructure/Data/Scripts/Baseline/`), generated by a private tool from the reconciled database with generated objects removed: tables with keys, defaults, indexes and statistics via SMO; foreign keys; 35 functions in dependency order and 306 procedures copied verbatim from `sys.sql_modules`, each preceded by its own SET options, plus module extended properties; 743 lookup rows in 11 tables. Scanned for employer vocabulary, hosts, URLs, UNC paths, addresses and emails: the only URLs are public attribution links in 2004-2008 helper functions.
- **Runtime model**: 102 entities and `XdbDataContext` scaffolded DB-first into `Infrastructure/Data`; the `Baseline` migration's snapshot is that model, its `Up` runs the scripts. The scaffold was then brought onto the production system's conventions (model unchanged, verified by EF's pending-model-changes check): no `partial` classes; each entity configures itself in a nested `EntityConfiguration : IEntityTypeConfiguration<T>` applied with `ApplyConfigurationsFromAssembly`; `EntityBase<TKey>` on the 98 entities keyed by `Id`; paired audit interfaces (`IHasCreated`/`IHasCreatedBy`, `ISoftDeletable`/`IHasDeletedBy`, `IHasLastModified`/`IHasLastModifiedBy`) declared only where the columns exist with matching types. Audit columns keep their original names and local-time database defaults; no global soft-delete filter (some original reads include deleted rows).
- **Verification**:
  - Fresh database created by the app at startup (about 4 s).
  - Schema compare against the reconciled reference with ANSI settings, quoted identifier, column order, fill factor and NOCHECK not ignored: **no differences** other than EF's history table.
  - All 341 modules have identical SET options and identical text apart from whitespace. EF trims or adds blank lines at the start and end of a batch, and joins lines with the host's newline (CRLF on Windows, LF on Linux). This has no behavioral effect.
  - Seed tables: identical row counts and checksums.
  - Adoption: the existing working copy recorded the baseline as applied without running it; a second start reported "up to date".
  - CI guard: an integration test (Testcontainers, SQL Server 2022) migrates an empty database and checks object counts, compat level, collation, 301 `ANSI_NULLS OFF` modules, 4 heaps, seed counts and restart idempotency.
- **Deferred to M3**: seeding the system asset type, properties, roles and images (they pull in views, images and picklists through foreign keys) and the admin member (user id from config). M3 is the first milestone that needs a working fresh install.
- **Known noise**: EF warns at startup that the `byte` identity key on `AssetRelationTypes` cannot use temporary values (seed-only table; harmless), and that the compat-level statement runs outside a transaction (by design).
- **Dependency pin**: `Microsoft.Data.SqlClient` 6.1.7 brings `System.Security.Cryptography.Pkcs` 9.0.11 with two published advisories; it is pinned to 10.0.13 through central transitive pinning until SqlClient moves past it.

## 5. Proving equivalence: the comparison harness

- `Xdb.Compare` replays each recorded request against the original and the port and diffs the responses. Before diffing, it normalizes:
  - server-generated GUIDs;
  - timestamps;
  - `ErrorId`;
  - key order where the original's order is unspecified.
- **State:** writes change the database, so each run starts from a SQL Server database snapshot, reverted between runs, for both the original and the port.
- **Captures stay private.** They contain real data. The harness reads them from a configured path outside the repo, and the repo contains only harness code and synthetic fixtures.
- **Coverage today: 40 of 299 operations (13%), from six captures.** Nothing covers files, search, picklist or view administration, or users. Before each area is ported, the author records a capture that exercises it.
- **Known deviations are written down, not hidden.** Each is an allowed-diff rule plus a line in a deviations list. Candidates:
  - fixing the shipped `Role_SaveViewRelations` null-body 500;
  - UTF-8 instead of ASCII-with-`?` in hand-built JSON;
  - fixing the duplicate malformed maps script tag (a UI-side issue).

### M2 result (DONE 2026-10-21)

**Legacy wire layer** (`src/Xdb.Core/LegacyWire`):
- `LegacyWireControllerBase` is deliberately not `[ApiController]`: missing query values bind to defaults and there is no automatic 400.
- `LegacyFaultFilter` turns any exception into the host's generic 500 `{"Detail":...}`, and logs it, which the original did not.
- `LegacyWire.JsonStream` (hand-built JSON: ASCII bytes, `application/json` without charset), `LegacyWire.Typed` (.NET 10's `DataContractJsonSerializer`, verified to match the original's dates, `\/` escaping, member order and dictionaries), `NullStream`, and `Result<T>.ToLegacyResult`.
- Neutral base route `legacy/Service.svc`.

**First slice** (`src/Xdb.Api/Application/Reports`): `ReportClient_Get`, `Report_GetData`, `Report_GetExecutionStats`.
- Clean query handlers returning `Result<T>`.
- A Dapper gateway (`Infrastructure/Data/Procedures`) holds every stored-procedure call and inline SQL.
- An in-process background queue replaces the original's thread-pool work (the report-log close).
- Wire formatting stays in the slice's `Legacy/` folder: Json.NET for the one operation the original serialized with it, and a literal transcription of the original's jqGrid string builder, so its quirks (spacing, string numbers, invalid JSON for zero rows, sequential placeholder replacement) reproduce by construction.
- Unit tests cover those quirks and the handlers' rules.

**Comparison harness** (`tools/Xdb.Compare`):
- `replay` sends each recorded call to the original and the port, grading Identical (bytes), Equivalent (only configured differences) or Different, with values hidden unless asked.
- `snapshot create|revert|drop` resets both databases to identical data.
- Configuration (addresses, databases, captures) lives outside the repo, with three kinds of allowed difference:
  - path ignores;
  - regex masks (e.g. the elapsed time inside the execution-stats message);
  - unordered arrays, for results whose order the original itself does not keep stable from run to run (checked by comparing the original with itself).

**Results:**
- The reports capture: 6 of 9 calls byte-identical; 3 equivalent (elapsed time masked).
- A synthetic capture calling all three operations for **every report in the database** (170 reports, deleted ones and error paths included): 510 calls, **0 different**, 350 byte-identical and 160 equivalent (149 masked elapsed times, 11 row-order-only on large reports without a deterministic sort).
- That run found two real bugs, both now fixed and covered by tests:
  - a report with no properties must skip the property lookup;
  - a stored-procedure report takes its headers from the procedure's columns.

**Found and fixed along the way:** the pre-baseline cleanup had dropped three report procedures (and three functions they call) that stored report definitions name *as data*. Migration `RestoreQuestionReports` restores them verbatim, and is idempotent. A sweep of every metadata string column for the other dropped names found no further data references.

**Known differences (accepted):**
- Response framing (`Content-Length` instead of chunked) and the original host's informational headers.
- Exceptions are logged.
- An unparseable typed query parameter returns the generic fault. The original's exact response in that case was never captured.

## 6. Milestones and sizing

Sizes are in **focused sessions**: a few hours of Claude implementing with the author reviewing. They are rough, and the L areas carry the most uncertainty.

| # | Milestone | Exit criteria | Sessions |
|---|---|---|---|
| M0 | Repo skeleton and conventions | Solution builds; `Result<T>`, handler and validator pattern, DI, tests run; CI builds on push | 1 |
| M1 | Baseline database | Reconcile script run privately; baseline EF migration running scripts 0001-0005, applied at startup; the working copy adopts it via the history table; fresh DB passes the schema compare | 2-3 |
| M2 | Wire layer and harness | Formatters, error convention, routing, identity, neutral base path, snapshot reset; the **reports** capture replays clean through the port | 3-4 |
| M3 | `RegenerateAll()` | Regenerated objects match the originals on the reconciled copy; fresh DB plus seed plus regenerate gives a working app | 2-3 |
| M4 | Read paths: metadata, then assets | Types/properties, views, picklists reads; `AssetClient_Get` and `ViewClient_Get` byte-identical | 6-8 |
| M5 | Reports, charts, roles | Report builder save (view DDL), charts, role save (row-security views) match their captures | 9-11 |
| M6 | Asset writes | Submittal/approval save path, read-table refresh, narrative; the asset edit capture matches | 4-6 |
| M7 | Members, submittals and history, search | Including RSS and the parallel user fan-out | 5-7 |
| M8 | Files, images, bulk upload | Multipart parser, 202-then-poll, image streams, xlsx | 4-6 |
| M9 | Admin and stubs; cut-over | Stubs in place; the original UI runs entirely on the port; the original service retired from the local setup | 2-3 |
| | **Total** | | **~38-52** |

**Calendar:** at three to four sessions a week, that is roughly **3-4 months**. The author's new captures (M4 onward) run alongside and are not counted.

Ordering rationale:
- The harness and reports (M2) come first because reports have full capture coverage and exercise the wire quirks: hand-built JSON, paging and dynamic columns.
- `RegenerateAll` (M3) comes before writes because asset, report and role saves all depend on the generator.
- Files come last because they are the most self-contained and the least covered.

## 7. Decisions (all APPROVED 2026-10-21)

1. **Wire adapter split** (§3): handlers clean, legacy quirks only in formatters and thin wire controllers. **Approved.**
2. **Stored procedures first, LINQ second** (§3), with a small proc gateway for complex procedures during parity. Alternative: LINQ from the start, which is slower to reach parity and riskier. Gateway technology: Dapper (handles multiple result sets) or EF raw SQL (no new dependency, but single result set only). **Approved: Dapper, limited to the gateway.**
3. **DECIDED - database and migrations:** SQL Server; EF Core migrations under `Infrastructure/Data/Migrations` in the API project, applied by the app at startup; the baseline migration runs embedded SQL scripts (§4). A standalone migrator outside the app is the likely end state; in-app keeps it simple for now.
4. **Central package management** (`Directory.Packages.props`), which the production repo does not use. **Approved.**
5. **Deviations policy** (§5): fix shipped bugs and the ASCII encoding as documented deviations, or reproduce them exactly. **Approved: document and fix.**
6. **Test database for integration tests:** Testcontainers SQL Server (Linux container, compat level 100 is supported), or the local server. **Approved: Testcontainers.**
7. **Capture plan:** which areas the author records first. **Approved:** picklist and view admin, then users, then files.
