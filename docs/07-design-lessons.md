# 07 - Design Lessons from XDB v1

Status as of 2026-10-21: written when the project was **shelved**; a revival has since started (see the README and `10-plan-run-as-is.md`). The excavation preserved the design (`00-original-system.md`, plus private excavation notes, not published). A rebuild was at first considered and declined: the general "store any object" capability is now a commodity (Airtable, NocoDB, Baserow, Teable, Grist, Directus, Dataverse; PostgreSQL JSONB natively), and the specialized feature set (CMDB/ITAM) has a free incumbent (iTop, GLPI, Snipe-IT) and dominant commercial ITSM products (one of which is what replaced the original). This document keeps the lessons, so they can be reused in other systems without reviving this one.

---

## Part 1 - Patterns worth reusing

### 1. Separate the model record from the physical unit (Definition / Instance)
Most systems flatten "product model" and "serial-numbered unit" into one record and duplicate model data across every unit. The original gave every type two levels, bound each attribute to one level, and showed definition values on instances as inherited, read-only values. **Reuse whenever a domain has catalog-vs-occurrence semantics:** product/unit, course/section, policy/claim, game template/game, recipe/batch.

### 2. Type inheritance with polymorphic reads
Attributes declared on a parent type flow to children; a query on the parent returns all descendants. The original implemented this by writing each object into the read model of every type in its ancestor stack. Today: either the same fan-out into projections, or a single projection with a type-path column (`ltree` in PostgreSQL) and `WHERE type_path <@ 'hardware'`.

### 3. Built-in attributes are just attributes with reserved ids
Exposing Name, Type, Created, Owner, etc. as properties with well-known ids meant one form engine, one filter engine, one report engine. No special cases for "system" columns anywhere in the UI. **Cheap to do on day one, painful to retrofit.**

### 4. Writes are change sets, not updates
Every save was a submittal: an append-only group of value rows with author, time, notes, a generated narrative, and an approval state. That one decision produced history, as-of reporting, an approval workflow, and an audit trail for free. This is event sourcing scoped to attribute values, and it remains the best way to build "who changed what, when, and was it approved" into a data store rather than onto it.

### 5. Flexible writes, flat reads (compile the metadata)
EAV for writes, generated per-type flat tables for reads, generated views for reports and security. This is CQRS with projections, done by hand in 2010. **The lesson is the shape, not the mechanism:** keep the write model generic; derive read models from metadata; treat the derived objects as disposable and fully regenerable.

### 6. Authorization as set membership
A role's per-type, per-permission filter compiled to a view, and "may this user edit this object?" became "is this id in that set?". That makes row-level security consistent between single-object checks, list screens and reports. Today: PostgreSQL row-level security policies, or the same filter compiled to a predicate/expression and applied in the query layer.

### 7. Form semantics belong to the context, not the field
Required / read-only / default were attributes of a property *within a property group within a view*, not of the property. The same data captured by a requester, an approver and an auditor needs different rules. Most form builders get this wrong.

### 8. The server describes, the client renders
The API sent metadata (view -> groups -> properties -> data types) and the client mapped data types to controls through one registry, with one generic lookup endpoint for every reference type. That kept the UI small relative to the surface it covered. Still the right shape for any schema-driven UI (now: a typed field registry + schema-driven form library).

### 9. Inline creation from pickers ("[Enter New]")
Letting a user create a missing picklist value, person, or referenced object without leaving the form removed a large class of "go create X first, then come back" friction. Gate it by a per-property flag and by permissions.

---

## Part 2 - Mistakes not to repeat

### 1. Derived objects must regenerate themselves
Incremental per-object refresh was automatic, but regenerating gen tables after a *metadata* change required running an NUnit test as an admin script. The read model silently drifted, and a "fix assets missing from gen tables" routine had to exist. **Rule: any object derived from metadata is rebuilt by the system, on metadata change, with a version stamp and a drift check - never by a human remembering to.**

### 2. Background work needs a queue, not the thread pool
Projection refreshes were fire-and-forget on the thread pool with no retry and no visibility. Use a durable queue / outbox with retries and a dead-letter path.

### 3. Store values typed
Every value was a string; formatting lived in SQL UDFs; multi-values and currency used `^`-delimited encodings on the wire. Typed storage (JSONB with a schema per type, or typed value columns) and typed wire formats remove a whole layer of parsing bugs.

### 4. Never trust the client for identity or authority
Many operations accepted a client-supplied `userId`; the admin menu was hidden by a hard-coded GUID in JavaScript; required fields were enforced mostly client-side. Identity comes from the token; authorization and validation happen on the server; the UI only mirrors them.

### 5. Admin complexity is a product problem, not a training problem
The engine was powerful and the admin experience exposed all of it raw: types vs levels vs inheritance, properties vs bindings vs group overrides, views vs processes, free-text positional filter logic (`(({0}) AND ({1})) OR ({2})`). What was missing: previews ("what will this form look like?"), impact analysis before changes, structured filter builders instead of logic strings, templates for common shapes, and sensible defaults so a new type works before it is fully configured. **For any metadata-driven system, budget as much for the admin experience as for the engine.**

### 6. Don't let schema evolution leave twins
`RoleMembers`/`RolesMembers`, `RoleAssetTypes`/`RolesAssetTypes`, `TriggerTypes`/`lkTriggerTypes`, `Operators`/`lkOperators` all coexisted with only the code knowing which was live. Migrations should finish what they start: move, verify, drop.

### 7. Version the schema from day one
39 core tables existed only in an after-the-fact SMO snapshot with no keys; 11 were in no script at all; DTA-generated indexes were applied directly in production. Any migration tool beats none.

### 8. Keep the engine separate from the deployment
Generic engine code, corporate integrations (jobs, directory, AD distribution lists), and business-unit features were interleaved across ~36 projects and three API generations. That interleaving is the reason every de-branding attempt stalled: the generic part could not be lifted out cleanly. Integrations belong behind plugin boundaries from the start.

### 9. Build the SQL with parameters, the HTML with escaping
String-concatenated SQL in several places, unescaped HTML throughout the client, `eval` of grid column models. Standard, but worth stating because the generated-SQL design made it tempting to concatenate everywhere.

---

## Part 3 - Lessons about the resurrection attempts

- **Four restarts (2013, 2014, Dec 2014, 2017-19) each stopped at the same point:** after renaming the vocabulary and before re-deriving the engine. Renaming felt like progress; the hard, valuable part (the metadata -> read-model compiler, the submittal model) was never rebuilt. If this is ever revisited, start with the engine core and a single end-to-end slice, not with a rename.
- **The XDB README's "compile code from metadata and deploy into itself"** was the C# analogue of what the original already did in SQL. Runtime compilation is unnecessary: interpreting metadata at runtime plus generated database projections gives the same benefit with none of the assembly-loading complexity.
- **The idea was right for its time.** In 2010 there was no Airtable, no JSONB, no mainstream CQRS vocabulary; the original anticipated all three. The market caught up, which is a validation of the design, not a refutation of it.

---

## Part 4 - If it is ever revisited

Minimum viable shape, for reference only:
- .NET 10 + PostgreSQL. Metadata in relational tables; values in JSONB per object, typed by the type's schema; append-only change sets for history and approval.
- Projections (per-type flat tables or materialized views) rebuilt by a background worker on metadata version change, with a drift check.
- Authorization filters compiled to predicates (or PostgreSQL RLS).
- No admin UI until the engine is proven; seed metadata from JSON files.
- A synthetic demo dataset - **never** the production backup or any copy of it (real personal data). A public aviation incident dataset is a good, already-proven candidate.

---

## Housekeeping (outstanding)

- [x] An old public repository from the 2014 rewrite attempt contained a plaintext SQL credential in a test config. Done (2026-10-21): that repository was deleted and this one created in its place. Still worth confirming the credential is not in use anywhere.
- [ ] Plaintext credentials also exist in local copies of the old source trees (a service config and some spike scripts). Local only, but scrub before anything is ever pushed.
- [ ] Keep the excavation notes, the scripted legacy schema and the golden-master captures out of this public repository - they contain former-employer vocabulary and real data.
- [ ] The production backup and its local working copy contain real personal data. Retain or delete per your own obligations; don't copy them anywhere else.
