# 11 - Phase 2 Scope: .NET 10 Port

Status: **approved 2026-10-21; M0, M1 and M2 done (2026-10-21); M3 to M6 done (2026-10-22); M7 and M8 done (2026-10-23); M9 backend done, UI cut-over in progress.** Builds on `10-plan-run-as-is.md`.

Goal: replace the original WCF REST service with a .NET 10 API that reproduces the original wire contract exactly, so the original jQuery UI works against it. The UI is **not** pointed at the port where it stands: at cut-over (M9) it is copied into this repository in its entirety and modified there (author decision 2026-10-23). Until then the unmodified original UI and its recorded traffic define the contract. Each ported operation must be shown equivalent by replaying recorded traffic against both the original (still running locally) and the port, then diffing the responses. The code follows the conventions of the author's current production system: thin controllers, handlers injected per action, CQRS with validation and `Result<T>`.

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
- **The deployed application has no "regenerate all", and never builds the read tables at all.** It builds a report's view when the report is saved and a role's views when the role is saved. The read tables and their MERGE procedures were built once, in 2013, by a developer tool in the source tree (which also re-saved every report and role). See the M3 result.

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
4. **`RegenerateAll()`**, ported from the original C# generators, rebuilds the read tables and MERGE procs, then the report views, then the row-security views. Generated objects are never part of migrations: an existing database keeps its own, and a fresh install builds them at startup (M3).
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
  - fixing the shipped `Role_SaveViewRelations` null-body 500 (declined 2026-10-22: kept byte-identical);
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

### M3 result (DONE 2026-10-22)

**Finding that reshaped the milestone:** the deployed release never creates the gen tables or their MERGE procedures. Saving an asset type only saves metadata; the generator method is compiled in but nothing calls it. All of the existing read tables and procedures were made by a developer test fixture in 2013 (one batch, plus one later run for two types). The deployed release builds only the report views (on report save) and the role views (on role save). Regenerating from today's metadata therefore cannot reproduce every existing object, because the metadata has changed since 2013.

**Decision (author, 2026-10-22): port and adopt.**
- All three generators are ported exactly, from the deployed release where it has them (views) and from the source tree's version of the same template where it does not (read tables and procedures).
- An existing database keeps its generated objects as they are; nothing regenerates on startup.
- A fresh install (no gen tables) builds everything at startup, after the migrations.
- `dotnet run --project src/Xdb.Api -- regenerate-all` applies migrations, rebuilds everything and exits (exit code 1 if any object failed). It is a command-line verb rather than an endpoint until the port has authentication.
- Building an asset type's read tables when the type is saved (the source tree's behaviour, absent from the deployed release) arrives with the metadata write paths, as a recorded deviation.

**Code** (`src/Xdb.Api/Application/GeneratedObjects`):
- `GenTables/GenTableSql` and `Views/ViewSql`: pure builders that reproduce the original SQL character for character (CRLF line endings, the bare LF after the table DROP, unescaped names and values, the original's comma logic, and its bugs, such as the never-joined alias for single-valued Dependency properties).
- `GenTables/GenPropertySet`: column choice and order, including the system relabels and the en-US sort with first-wins de-duplication by name.
- `GenTableGenerator`, `ReportViewGenerator`, `RoleViewGenerator`: load metadata through the original's procedures and inline SQL (`Infrastructure/Data/Procedures/GeneratedObjectProcedures`), then run the DDL statement by statement without a transaction, with the connection's default SET options (which is why every generated module has ANSI_NULLS and QUOTED_IDENTIFIER on).
- `Commands/RegenerateAll`: every live asset type, then every live report, then every active live role; a failure is recorded and the run continues.
- `Application/Setup/Commands/InitializeDatabase`: at startup, creates the configured first administrator (`Seed:FirstAdmin`: user id and name; the original never seeded one, and admin rights are membership in the Admin role), then regenerates if no gen tables exist.
- Migration `SystemSeed`: the system rows the application hard-codes (the User asset type, 14 system properties, the Admin and DB Admin roles, the built-in system administrator with its User asset and Admin membership). Idempotent; an existing database already has them all.
- Configuration `GeneratedObjects:ViewImageUrl`: the original baked an environment-specific image URL into every image column as a literal; regenerated tables bake in this value.

**Verification:**
- Regenerated on a copy of the working database (7.9 minutes, no failures): 182 read tables with their MERGE procedures, 156 report views, 78 role views. The extra four tables belong to a live asset type that never had any.
- Against the original objects:
  - report views: 155 of 156 byte-identical; the other differs only in blank lines at the start and end of the stored text;
  - role views: 75 of 78 byte-identical; the other three belong to roles edited after their views were generated in 2013 (the original never refreshed them);
  - every generated module has the original's SET options; every table has one clustered primary key on `AssetId` (names are generated by SQL Server and cannot match);
  - MERGE procedures: all differences trace to metadata changes since 2013 (above all a system property the 2013 run did not see, which now adds a column to every table). An independent emulator of the original generator, validated against the originals where metadata has not changed, produces exactly the port's text for all 182 procedures over today's metadata.
- Gen table rows differ from the existing ones as expected: the original only ever loaded a table when it was created, then maintained rows incrementally, so existing tables are missing assets created through paths that never refreshed them (for example 1,820 users added by member sync). This is why an existing database is adopted rather than regenerated.
- The working database (adopted, generated objects kept) still replays all 170 reports against the original with no differences (510 calls) after the new migration.
- Integration tests (SQL Server container): a fresh database gets the seed, the first administrator with Admin membership, and the User type's read tables loaded with both administrators; a MERGE procedure refreshes an asset; report and role views build over the tables and return the expected rows; a second start changes nothing.

**Known differences (accepted):**
- Duplicate display names in a property set (the system Created/Created By/Last Modified/Last Modified By pairs on instance tables): the original's choice was undefined; the port prefers the current system properties over the legacy duplicates, which is what most existing tables show.
- Column order uses .NET's en-US culture comparison (ICU). The original ran on Windows NLS, whose "word sort" almost ignores hyphens and apostrophes; the two agree on every display name in the working database.
- Report filters that share an order position: the original's order was undefined and the port keeps that (the same procedure); in the working database the port's order matched the original in all five such views.

### M4 result (DONE 2026-10-22)

**Scope:** every read operation the original UI calls, in three batches, each proved by a synthetic capture (every call template filled from the working database, including deliberate error cases) replayed against the running original:
- A, metadata (asset types, properties, picklists, lookups): 42 operations;
- B, views, property groups, processes, help and roles: 31 operations;
- C, assets (the asset editor, values and history, type-level dictionaries, the filter engine, related items, the relation graph, hits, view asset lists, the chart filter panel): 42 operations, including one deferred from A.

Operations the UI never references (two asset getters) are not ported.

**Code:** `Application/Metadata`, `Application/Views` and `Application/Assets`, each with a wire controller, CQRS handlers and a gateway in `Infrastructure/Data/Procedures` that runs the original's SQL text and reproduces its data-layer helpers.
- The filter engine reuses M3's `ViewSql`. The chart filter panel's SQL (the "SELECT ... INTO" temp-table form) is transcribed separately in `ChartSql`.
- The client caches (`ViewClients`, `AssetClients`) read the original's BinaryFormatter rows through `System.Formats.Nrbf` and store new rows as the response JSON. Consequence: the original service cannot share a database the port has written to.

**Wire facts established by replay and probe** (now part of `LegacyWire` and the binding filter):
- The data-contract serializer on .NET Framework writes control characters as `\u000d`-style escapes; .NET 10 writes `\r`. The port rewrites them.
- An empty query value binds as `""`; only an absent one is null.
- A typed operation returning null answers 200 with an empty body and no Content-Type.
- Request bodies:
  - A POST body without a Content-Type never reaches the operation; the answer is the 1047-byte Request Error page.
  - A body the host's JSON reader rejects at its first token answers 400 with no body; any later syntax error answers the Request Error page; a literal `null` reaches the operation.
  - A wrapped body member that cannot be converted answers 400 with the formatter's message, which names the parameter and the serializer's own error text.
- A runtime SQL error that the server raises after the column metadata (for example a plan-dependent uniqueidentifier conversion) leaves `HasRows` false. The original's guarded readers then return nothing instead of failing. Microsoft.Data.SqlClient behaves the same, so the port guards the same readers.

**Verification (synthetic captures, port against the original, snapshots reverted before each run, 200 ms between calls):**
- A: 7,833 calls. Every difference found was fixed; the final recheck of the affected operations had 0 different.
- B: 4,972 calls. Two operations differed (the missing Content-Type and the guarded reader); both were fixed, and the recheck was 995 of 995 identical.
- C: 13,247 calls. The full re-run covered the first 12,111 calls (including 49 of the 61 relation-graph calls), with 5 differences, all since fixed or explained (below). The remaining operations, and those fixed during that run, were replayed separately: 1,130 calls, all identical or equivalent apart from the known cases below. A last fix (an id that is neither a view nor a report fails, as M2 found) was rechecked: 205 of 205 identical.

**Harness changes:**
- When a capture has no request Content-Type header, the replayer uses the HAR `postData.mimeType`.
- `--timeout-min` raises the HTTP timeout. The relation graph of a well-connected asset takes about 10 minutes on both hosts: the original's algorithm runs 1+n+n·m related-item queries.

**Known differences (accepted):**
- Tie order: lists ordered only by a name, label or count return equal rows in engine order, which differs between two databases. These are compared as unordered.
  - One text/html dictionary is not valid JSON, because the original writes backslashes and control characters raw and the port keeps that. The harness cannot compare it as unordered, so it was checked by hand: same 49,330 entries in a different order.
- Synthesized system values (Asset Name, Asset Type) get a new random id on every call in the original. Property-value ids are therefore not compared for the asset editor and its view values.
- A view with saved filters would take a separate matching path in the view asset lists. No view has filters in the data, so the port fails on that branch instead of porting it (recorded deviation).
- The chart filter panel (`AssetFilterClient_Get`) is not deterministic in the original itself: two identical calls to it return different value lists. In 3 of 177 calls the port's lists differ from the original's by one item.
- The last 12 relation-graph calls (about 10 minutes each) are left for an unattended run.

### M5 result (2026-10-22)

**Scope:** the report builder and administration, charts, dashboards and maps, and role administration: 51 operations the UI references (reports 20, charts 16, roles 15). Each area was specified from the deployed release, its write behaviour was settled by probes on a reverted snapshot of the original, and a synthetic capture was replayed. After each replay, the tables written and the generated views were compared between the two databases.

**Code:** `Application/Charts`, `Application/Roles`, and `Application/Reports` (a separate builder controller beside M2's viewer), each with gateways in `Infrastructure/Data/Procedures`.
- **Charts:** `ChartSql` gains the grouping-count and detail queries that the original stored with each chart. Six operations were stubs in the deployed release and stay stubs, with identical bytes.
- **Roles:** `Role_Save` builds the original's inline batch in a pure, unit-tested `RoleSaveBatch`, including its bugs. A removed report writes `GetDate` without parentheses, which rejects the whole batch. An existing changed asset type fails while the batch is being built. Row-security views are rebuilt from the posted role, as the original did.
- **Reports:**
  - `ReportLoader` (the original's report load, including its reporting-label overwrite);
  - `ReportFilterString`;
  - `ReportSaver`: writes in the original order without a transaction, then reloads and rebuilds the view. If the view create fails, it kills a report posted as new.
- **Request bodies** are read as the original's serializer read them:
  - every member is required at every depth;
  - typed values are strict;
  - dates follow its offset rule;
  - relation lists keep the first item per id and derive `IsDirty` from posted member order.

**Wire facts established (now in `LegacyWire`):**
- A caught logical failure writes an `Errors` row and answers 500 with Content-Type exactly `application/json`, an `ErrorId` header and the body `false`.
- The caller identity of the original (the account its requests ran as) is configured and resolved per call.

**Verification:**
- **Charts:** 533 calls, 0 different. The chart, filter and member tables were identical afterwards, including every stored query text.
- **Roles:** 870 calls. All differences came from a harness bug (fixed below); the recheck was clean.
- **Reports:** 1,501 calls. The only differences were the export (deferred, below) and one cache read after the exports that counts their log rows.
  - The report, property, filter, bookmark, role-link and error tables were identical afterwards.
  - Of the views both databases have, 11 of 157 differ only in the order of filters that share an `[Order]` value. Every save gives filters new random ids, so the order among ties is random on both hosts.

**Harness changes:**
- Unordered arrays are matched with ignored members blanked, and ignore rules apply inside sorted arrays.
- The replayer timeout is configurable.

**Known differences and deviations:**
- `Chart_GetViaFiltering` saves the chart before loading its data. The original saved it on a background thread, where one failure could stop the worker process. The port logs that failure instead; the rows left behind and the response are the same.
- The admin roles page posts `null` to `Role_SaveViewRelations` on every save, and the original answers 500 even though the role was saved. The port keeps that 500 (author decision; the scope's "fix" candidate is not taken).
- `Report_Export` is moved to M8 with the other spreadsheet work (author decision): its file cannot be byte-identical (embedded timestamps and measured column widths), and its bulk-template half already belongs to the uploader.
- `MigrateReport` (other environments' databases) is a stub. The configured own instance re-saves the report, as the original did. Any other target faults without writing.
- Environment strings that the original hard-coded (a chart subtitle and two type names quoted in error pages) come from configuration.

### M6 result (2026-10-22)

**Scope:** asset writes (13 operations: value submittals, asset create/instance/clone/delete, picklist value creation, asset bookmarks, the process type lookup, a migration stub) and, by author decision, the metadata writes (21 operations: asset type, property, picklist, property group and view factories, clones, saves and kills, quick filters, five migration stubs). Each half was specified from the deployed release, then a synthetic capture was replayed against the original and the port on reverted snapshots. After each replay, the written tables were compared between the two databases.

**Code:**
- **Request bodies:** `Application/Common/Legacy/DcjsReader` emulates the original serializer setter by setter. It creates objects without running constructors, applies members in posted order through the original setters (which dirty the object, truncate captions and stamp `LastModified` with the server clock), checks required members at every depth, rejects duplicate members, and applies the lists' add rules. The saves depend on what it produces: a posted `IsDirty: false` almost always ends true.
- **Metadata:** `Application/Metadata/Commands` (asset type, property and picklist savers; migration stubs) and `Application/Views/Commands` (property groups, views, quick filters, clones), with the `MetadataWriteProcedures` gateway. As in the deployed release, none of these saves touches the generated objects.
- **Assets:** `Application/Assets/Writes`:
  - `SubmittalEngine`: there is no pending-approval path. Every save is approved by the acting member in two passes, then relations, renames and the asset's update stamp.
  - `AssetWriter`, `GenTableRefresher` (gen-table insert, refresh and delete as background work) and `NarrativeWriter` (the activity sentences).
  - `AssetWriteProcedures` gateway.
- **Bugs kept as behaviour:** the asset type parent-removal and `DeletedBy` failures, the view filter insert that always fails, the picklist re-save and soft-delete failures, and the parent relation rule that detaches every sibling.

**Wire facts established:**
- A known operation called with the wrong verb answers the host's 405 page (`LegacyWire.MethodNotAllowedPage`). It links the original machine's help page, so the address is configuration.
- A wrapped body whose dictionary member cannot be read resets the connection; the port aborts it too.
- A wrapped body sent as `text/plain` is the Request Error page.

**Verification:**
- **Metadata:** 690 calls. All writes identical; the remaining differences were harness rules for new random ids and the 405 page (both fixed). The written tables were identical afterwards (rows with server-generated ids matched by content), and neither database's object set changed.
- **Assets:** 320 calls. The first run differed only in a serializer type error in `Bookmarks_Get` and the `text/plain` rule; after both fixes, a confirmation run had 0 differences. The written tables were identical afterwards, including 1.6 million property values and the gen-table rows of the created assets, apart from the clone's random id.

**Harness changes:** a connection reset is recorded as a result (status 0); the replay script takes a config file (`compare-m6.json` holds the write-phase rules).

**Known differences and deviations:**
- Background failures that stopped the original's worker process (a submittal for an unknown asset, a missing gen-table procedure, a narrative over 255 characters) are logged by the port instead. No response bytes differ.
- Migration to the own instance runs locally, as the original did; to any other target it faults without writing.
- The asset migration skips the value loader for a live asset (every value already exists, so nothing is written either way).

### M7 result (2026-10-23)

**Scope:** members and users, errors and feedback, alerts and questions (20 operations ported, one environment stub, four dead), and submittals, triggers, activity feeds and search (11 ported, three dead). Each half was specified from the deployed release, its read operations measured on the original, and a synthetic capture replayed against both hosts on reverted snapshots; the written tables were compared afterwards.

**Code:**
- **Users:** `Application/Users` with the `UserProcedures` gateway. `UserClient_Get` is the bootstrap model: the original ran its five dictionary reads in parallel and waited for all of them; the port runs them in turn, and any failure still fails the call. `User_Create` keeps the original's member-save procedure, whose update path is followed by an insert that always fails. `QuestionsRespond` saves every answer before any value work, then replaces a rejected value through the M6 submittal engine.
- **Activity:** `Application/Activity` with the `ActivityProcedures` gateway: submittal reads (values decoded by the M4 loader), trigger reads, the activity feeds, search and tag search.
- **Wire pieces in `LegacyWire`:** an RSS writer (`LegacyRss`: the formatter's element order, escaping and date formats), .NET Framework link escaping (`LegacyUri`), and `LegacyXmlOperation`: an exception in a feed operation is the Request Error page, not the JSON fault.
- **Bugs kept as behaviour:** `TriggerCreate` sends a parameter name its procedure does not declare, so the database rejects every save and no trigger is ever written. `SystemActivity` builds a statement that never compiles. `Domains` reads a picklist that does not exist.

**Wire facts established (measured):**
- A repeated string query parameter binds as its values joined by commas; an empty or white-space value is passed as is. The port's binder had turned both into something else (first value, null); fixed in `LegacyBindingFilter` for every operation.
- A request body is read only with a JSON content type (wrapped and bare alike); `text/plain` is the Request Error page.
- A wrapped body holding a string longer than 8,192 characters (the binding's default reader quota) resets the connection.
- An empty wrapped body for an operation with Int32 parameters is the Request Error page.

**Verification:**
- **Users:** 466 calls, all identical after the body fixes; the written tables (members, assets, client cache, settings, preferences, bugs, suggestions, questions, values, submittals, gen rows) were identical afterwards.
- **Activity:** 385 calls, no port differences. Ten calls differ between runs on the original itself (below). The search log rows and every trigger table were identical afterwards; neither database's object set changed.

**Host time zone:** the wire writes the original host's local dates (serializer offsets, feed dates). The port carries that zone itself so it can run in a Linux container: `LegacyTimeZone` points the process at a bundled zone file holding Windows' "Eastern Standard Time" rules (generated by `tools/LegacyTimeZone`), because the IANA `America/New_York` data differs for old dates (year 1 gets local mean time there). A Windows host uses its own zone and must be set to Eastern.

**Harness changes:** feed items can be compared as a multiset (`unorderedXmlItems`), for feeds whose item order the original does not fix.

**Known differences and deviations:**
- Two queries are nondeterministic on the original: the type-2 search (`TOP 100` with no `ORDER BY`) and the role-based activity feed (`TOP n` over minute-resolution times). Identical calls to the original return different sets; the port's results are among them.
- `SearchTag` ran two reads on parallel threads into one unsynchronized dictionary, which interleaves and, on large results, loses entries. The port runs them in turn: one of the original's possible outputs, never a corrupted one.
- The search highlight compares with ICU rather than NLS; the two differ only for some non-ASCII expansions and ignorable characters.
- `UserData_Get` called a directory service that does not exist in this environment and always faulted; the port faults without the call.
- The feeds' product name and editor address come from configuration.

### M8 result (2026-10-23)

**Scope:** image and document streams and uploads, the bulk upload reads, and `Report_Export` (moved here from M5). Specified from the deployed release in two parts (files and images; bulk upload and workbooks), read operations measured on the original, synthetic captures replayed against both hosts, and the written tables compared afterwards.

**Author decisions (2026-10-23):**
- The port runs in a Linux container: no Windows-only package or reference is taken. Behaviour of the original that needed one is deferred and flagged (below), with a stated interim behaviour and harness rule.
- Bulk uploads are set aside: the workbook upload (`Upload`), the import behind it, the two bulk template generators and the bulk-update export are deferred. The bulk upload reads are ported.
- Image steps that needed GDI+ fail meanwhile, as the original failed for images GDI+ rejected.
- The image re-save bug (the save procedure passes two arguments in swapped order, so re-saving an existing image stores one byte) is kept, as the M5 precedent.

**Code:**
- **Files:** `Application/Files`: `getImageStream`, `Image_GetDimensions`, `getUploadStatus`, `Image_Upload`, `UploadDocument`, `prepareUploadObjectPropertyValue`, with the `FileProcedures` gateway. `LegacyMultipart` reproduces the original's multipart handling as the byte transformation it was (three header lines cut, the closing delimiter removed anywhere); `UploadProgress` replaces the per-process cache that `getUploadStatus` read.
- **Bulk reads:** `Application/BulkUploads`: `BulkUpload_Get`, `BulkUploads_Get`, `BulkUploadLogs_Get`.
- **Export:** `Application/Reports/Export`: `ReportWorkbook` writes the workbook part by part as EPPlus 2.9 did (entry order, BOMs, shared strings, styles), with no spreadsheet package.
- **Wire:** `LegacyRawBody` (an operation that reads its own body: form readers removed); response header values are written as UTF-8, as the original host did.

**Wire facts established (measured):**
- The upload operations declared no response format: any failure is the Request Error page. A JSON-typed upload never reaches the operation.
- Excel sheet names are cut at 31 characters; a report without rows lists only its first column; an empty document property keeps its indentation; a report without properties still exports.

**Verification:**
- **Files:** 792 calls: 557 identical, 235 deferred (calls that reach the deferred image decoder or thumbnails), none different. Images and documents identical afterwards (the deferred image rows left out).
- **Bulk reads and export:** 201 calls: 181 identical, 13 equivalent (workbooks: random relationship ids, the generation time, column widths), 7 deferred (bulk-update export), none different. Database state identical.

**Harness changes:** binary request bodies sent as bytes; binary responses reported by length and hash only; `Content-Disposition` compared; workbooks compared part by part; an expect-different list grades deferred calls as Deferred.

**Deferred Windows-only dependencies (to resolve later):**

| Id | Original behaviour | Component | Port meanwhile |
|---|---|---|---|
| D1 | Image width and height on upload | GDI+ (`System.Drawing`) | upload fails at that step (Request Error page), nothing written |
| D2 | Thumbnails (`getImageStream` with `createThumbnail=true`) | GDI+ | fault |
| D3 | Workbook column widths (export, templates) | GDI+ text measurement | default width for every column |
| D4 | Reading uploaded bulk workbooks | Jet 4.0 OLE DB (32-bit) | `Upload` faults (bulk uploads deferred) |
| D5 | Address-service lookup inside the bulk import | WCF with Windows authentication | not reached while D4 holds |

A portable implementation of D1-D3 will not be byte-identical to GDI+; its comparison rule (dimensions, pixel tolerance, width tolerance) is decided when it is taken up.

### M9 result, backend (2026-10-23)

**Scope:** the administration operations still unported: environment list, help-section creation and save, service-module metadata (create, read, save, list, the upload preparation), the list of hardware query types, and the Windows-only operations. Specified from the deployed release, read operations measured on the original, a synthetic capture replayed against both hosts.

**Code:** `Application/Admin` with the `AdminProcedures` gateway. `DcjsReader` reads byte arrays (an array of 0-255; an object reads as empty) and the service-module body rules (a name over 50 characters keeps 49).

**Author decisions (2026-10-23):**
- Operations of the original without a client stay unrouted (recorded deviation): they answer the unknown-operation page. Operations that change external state (directory groups, the Windows service, an unauthenticated delete) are never routed.
- Deferred Windows-only or external dependencies, routed with a fault that matches the original for every input it answers here: the Windows service status (D6), hardware queries through WMI (D7), mail through the internal relay (D8). Directory-group operations (D9) and service start/stop (D10) are not routed.
- The unknown-operation page: a path under the service that is no operation answers the original's 404 page (the 405 page with "Resource does not exist").

**Verification:** 152 calls: 143 identical, 9 equivalent (new random ids and the creation clock), none different. Help sections and service modules identical afterwards; neither database's object set changed.

**Known differences:** a module saved without a version or class name cannot be read back (as in the original); every module save clears its download time (the original never sent it).

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
| M6 | Asset writes | Submittal/approval save path, read-table refresh, narrative; metadata writes (asset types, properties, picklists, property groups, views, view filters; author decision 2026-10-22) with their generated-object side effects; the asset edit capture matches | 7-10 |
| M7 | Members, submittals and history, search | Including RSS and the parallel user fan-out | 5-7 |
| M8 | Files, images, bulk upload | Multipart parser, 202-then-poll, image streams, xlsx (incl. `Report_Export`, moved from M5) | 4-6 |
| M9 | Admin and stubs; cut-over | Stubs in place; the original UI copied into this repository in its entirety and modified there to run on the port (not redirected in place; author decision 2026-10-23); the original service retired from the local setup | 2-3 |
| | **Total** | | **~41-56** |

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
