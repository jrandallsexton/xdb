# 13 - The sportDeets Import (began as the Venue walkthrough)

Status: **implemented; paused 2026-10-26** (`14-pause-point.md`). Written 2026-10-24 as a plan that walked one asset type, Venue, end to end before any code; then built and extended to Franchise, Person, Football Athlete and Coach (PRs #1-#4). Sections 1-6 keep the original plan, with notes where the build went another way; section 7 holds the modelling decisions. The "metadata-driven lens" in `12-concept-live-source.md` comes later. Related: `09-concept-sportdeets-overlay.md`.

**As built:** `src/Xdb.Api/Program.ImportSportDeets.cs`, run with `import-sportdeets [--reset]` in the SportDeets environment against the `XdbSportDeets` database only. It reads the Producer database directly (read-only; no export files), declares each type as an `ImportType` record (levels, properties, groups, view, source query, row mapping), creates what is missing by name through the Create* slices, and writes each type's rows as one `CreateAssets` batch.

## 1. Approach

- **Export** (not built; the importer reads the Producer database directly instead): read-only SQL scripts against a copy of Producer's database (100% canonical data; user data lives in API's database and is never involved). Output: one NDJSON file per type plus a manifest. The scripts live in the sports-data repo, next to the schema they read.
- **Import:** all code, inside XDB. Types, properties, picklists and views are defined in code and created idempotently; rows are written through XDB's application commands, not raw SQL into the EAV tables.

## 2. Source row (Producer `Venue`)

```
Id, Name, ShortName, IsGrass, IsIndoor, Slug, Capacity, City, State, PostalCode,
Country, Latitude, Longitude, CreatedUtc, ModifiedUtc, CreatedBy, ModifiedBy
```

## 3. Constraints found in XDB

- **No Boolean data type.** `DataType` (and `dbo.DataTypes`) runs from Date to Html with nothing for true/false. Booleans become picklists.
- **No external-id column.** Nothing records where an asset came from; a map table is needed (section 6).

## 4. Column mapping

| Producer column | XDB | Data type | Notes |
|---|---|---|---|
| Id | Asset Id | - | Reuse the sportDeets GUID as the AssetId; re-imports match without a lookup |
| Name | Asset name | - | System name field, not a property |
| ShortName | Property | String | |
| IsGrass | Property "Surface" | PickList: Grass / Artificial | Meaningful values over Yes/No |
| IsIndoor | Property "Setting" | PickList: Indoor / Outdoor | Same |
| Slug | Property | String | Link back to sportDeets; could later become a computed Url |
| Capacity | Property | Int | `0` = unknown: write no value |
| City | Property | String | Too many distinct values for a picklist |
| State | Property | PickList | Values = distinct values in the export |
| Country | Property | PickList | Values = distinct values in the export; optional Country -> State filter via `PickListDependencies` |
| PostalCode | Property | String | Not Int: leading zeros, foreign formats |
| Latitude / Longitude | Property | Float, Precision 7 | |
| CreatedUtc / CreatedBy | Not imported | - | Source audit; the XDB asset is created by the import user |
| ModifiedUtc / ModifiedBy | Not a property | - | `ModifiedUtc` goes in the map table so unchanged rows are skipped |

**Property groups:** Facility (ShortName, Surface, Setting, Capacity); Location (City, State, PostalCode, Country, Latitude, Longitude).

**Picklist rules:** Surface and Setting have fixed values defined in code. State and Country are seeded from the export with `AllowNewValues = 0`; the importer adds a missing value when one appears and logs it.

## 5. Import flow

1. **Read** the type's rows from the Producer database (planned: export `venue.ndjson` + `manifest.json`; not built).
2. **Ensure schema (idempotent):** a code-defined type (as built: an `ImportType` record, matched by name rather than fixed GUIDs) creates the AssetType (definition-only; see 7.1), picklists and values, definition-level properties, groups and the default view, with fixed GUIDs so re-runs are no-ops.
3. **Upsert rows** through application commands as a dedicated "sportDeets import" system user; submittals auto-approved. Each type goes in as one `CreateAssets` batch: checks are set-based (links, pick list values, referenced assets, taken ids and definitions are read once per batch), rows are written with `SqlBulkCopy` 5,000 assets per transaction, and the type's gen tables are rebuilt once (a batch of up to 200 assets is merged asset by asset instead). A rejected row is logged and the rest are still written.
4. **Record** each row in the map table (not built; see section 6).
5. **Recompile** the gen tables (as built: part of the `CreateAssets` batch); verify in the UI.

## 6. Map table (new; not built)

As built, assets keep their sportDeets ids and a rerun skips assets that already exist, so changed source rows are not updated. The table below is still the way to detect changes.

`ImportedAssets(Source, SourceId, AssetId, SourceModifiedUtc, ImportedUtc)`

- Upsert key `(Source, SourceId)`; skip the row when `SourceModifiedUtc` is unchanged.
- Doubles as the identity bridge for the later lens (`12`).

## 7. Open decisions

1. **Definition or Instance: revised (2026-10-25). A definition is the lasting identity; an instance is one occurrence of it.** The first import followed the legacy reading (definitions are *kinds*, instances the real things) and put every venue under one "Venue" definition, whose `InstanceOf` then said nothing. sportDeets has few kinds but many occurrences, so:
   - **Venue: definition-only** (no `InstanceLabel`). Each row is a definition; properties, groups and the default view (`DefaultViewId`) are at the definition level.
   - **Franchise: definition = the franchise, instance = a FranchiseSeason.** One `Franchise` type labelled Franchise / Season; each season's `InstanceOfId` is its franchise (sportDeets `FranchiseId`). Franchise properties (names, colors, Home Venue) are definition-level; season properties (year, conference, record, scoring) instance-level. No property is linked at both levels of one type.
   - **People: `Person` is the identity, roles refer to it.** Person (definition-only: names, birth date, birthplace as city plus the shared State and Country pick lists, height, weight) is its own asset; `Football Athlete` (definition = the athlete, with a Person reference and slug; instance = an AthleteSeason whose Team refers to its FranchiseSeason) and `Coach` (definition = the coach, with a Person reference; instance = a CoachSeason whose Team refers to its FranchiseSeason) each refer to a Person. CoachSeason.Title is not imported: it holds the coach's last name on every row, a Producer mapping bug. A type hierarchy (Person -> Football Athlete, Person -> Coach) cannot do this: an asset has exactly one type, so the same human would be two unrelated assets. With a shared Person, a coach is linked to their playing career. The Producer does not link athletes and coaches yet, so each athlete and each coach brings a Person of their own (id derived from the source id); when that link exists, from the Producer or curated in XDB, roles share one Person. All athlete seasons are imported (`AthleteSeasonsFrom`/`To` can narrow it for a quick partial run). An athlete's name falls back from the display name to first and last name; 13 Producer athletes have neither, so they, their Person and their 14 seasons are rejected and logged.
   - **References** between imported types are Asset properties whose values are the referenced row's sportDeets id, which its asset keeps (Franchise Home Venue, FranchiseSeason Venue).
2. **Fixed GUIDs:** sportDeets ids for assets (built). Types, properties and pick lists are matched by name instead of fixed GUIDs; a Person's id is derived from its source row's id (name-based MD5), so reruns find it.
3. **Country -> State dependency:** not set up.
4. **Default view columns:** built as each level's default view, made from its property groups.
5. **Boolean data type: deferred to the new UI.** The legacy data stored booleans in shared Yes/No picklists (74 properties), with the expected drift (an "N/A" third state, a stray value). A real Boolean also changes the UI (edit control, display, filters, the property-creation dialog), which would be built twice if done in the legacy UI. Until then, imported flags use one fixed, code-defined Yes/No picklist; converting later is a data migration (Yes -> true, No -> false, no value -> unset) plus a data-type switch.
