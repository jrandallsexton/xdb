# 13 - Plan: Import Walkthrough, Venue

Status: **plan, for discussion** (2026-10-24). Phase 1 of getting sportDeets data into XDB is export/import; the "metadata-driven lens" in `12-concept-live-source.md` comes later. This doc walks one asset type, Venue, end to end before any code is written. Related: `09-concept-sportdeets-overlay.md`.

## 1. Approach

- **Export:** read-only SQL scripts against a copy of Producer's database (100% canonical data; user data lives in API's database and is never involved). Output: one NDJSON file per type plus a manifest. The scripts live in the sports-data repo, next to the schema they read.
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

1. **Export** `venue.ndjson` + `manifest.json` (types, files, source migration id, export timestamp).
2. **Ensure schema (idempotent):** a code-defined `VenueTypeDefinition` creates the AssetType, the single "Venue" definition asset, picklists and values, instance-level properties, groups and the default instance view, with fixed GUIDs so re-runs are no-ops.
3. **Upsert rows** through application commands as a dedicated "sportDeets import" system user; submittals auto-approved.
4. **Record** each row in the map table.
5. **Recompile** the Venue gen table; verify in the UI.

## 6. Map table (new)

`ImportedAssets(Source, SourceId, AssetId, SourceModifiedUtc, ImportedUtc)`

- Upsert key `(Source, SourceId)`; skip the row when `SourceModifiedUtc` is unchanged.
- Doubles as the identity bridge for the later lens (`12`).

## 7. Open decisions

1. **Definition or Instance: settled, one "Venue" definition; each sportDeets row is an instance.** In XdbDev, definitions are *kinds* and instances are the real things: the four largest two-level types hold ~44.5k instances under 18-111 definitions each (82-304 instances per definition), and single-definition types exist (1 definition with 43 instances; 1 with 9). Every instance has an `InstanceOfId` pointing at a definition of the same type; no type is instance-only.
   - **Definition:** a single "Venue" (sportDeets has no venue-kind column to split on). It holds properties shared by all venues, none yet.
   - **Instances:** one per row, `InstanceOfId` = the Venue definition; all mapped properties are instance-level.
   - **Views:** the main grid is `AssetTypes.DefaultInstanceViewId`; `DefaultViewId` covers the definition list.
   - **Later:** if a meaningful kind appears (Stadium / Arena / Ballpark, or by sport), split the definition and re-parent instances by changing `InstanceOfId`; no data is lost.
   - The same reading applies to Franchise: a franchise is a real thing, so an instance of a "Franchise" definition; FranchiseSeason then relates to it by reference, not through Definition/Instance (to be decided with that type).
2. **Fixed GUIDs:** sportDeets ids for assets (recommended); fixed new GUIDs for the type, properties and picklists.
3. **Country -> State dependency:** now or later.
4. **Default view columns:** suggested Name, City, State, Capacity, Surface, Setting.
5. **Boolean data type: deferred to the new UI.** The legacy data stored booleans in shared Yes/No picklists (74 properties), with the expected drift (an "N/A" third state, a stray value). A real Boolean also changes the UI (edit control, display, filters, the property-creation dialog), which would be built twice if done in the legacy UI. Until then, imported flags use one fixed, code-defined Yes/No picklist; converting later is a data migration (Yes -> true, No -> false, no value -> unset) plus a data-type switch.
