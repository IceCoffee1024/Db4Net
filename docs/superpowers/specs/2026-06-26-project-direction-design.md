# Project Direction Design

## Goal

Define Db4Net's initial product direction and v1 boundaries before individual feature specs.

Db4Net is a lightweight fluent SQL builder for Dapper. It helps developers compose safe, parameterized, SQL-shaped queries and commands without turning into an ORM, a LINQ provider, or a change-tracking data access framework.

This spec summarizes the intended direction. The longer historical analysis is preserved in `docs/engineering/decisions/2026-06-26-project-direction.md`.

## Product Positioning

Db4Net should serve developers who already want Dapper's execution model and object mapping, but want a safer and more structured way to build common SQL.

The library should:

- Keep Dapper responsible for execution and result materialization.
- Preserve a recognizable SQL mental model in the public API.
- Validate mapped identifiers instead of accepting raw column fragments in typed APIs.
- Parameterize values through Dapper parameters.
- Allow generated SQL inspection through command rendering APIs.

The library should not:

- Track entity changes.
- Add `SaveChanges()`.
- Load relationships automatically.
- Generate migrations.
- Become a full LINQ provider.
- Translate full predicate expressions such as `u => u.Id == 1`.

## Public API Direction

The public API should stay SQL-shaped and statement-oriented:

- `SelectFrom<T>()`
- `SelectExistsFrom<T>()`
- `SelectCountFrom<T>()`
- `SelectAggregateFrom<T>()`
- `InsertInto<T>()`
- `Update<T>()`
- `DeleteFrom<T>()`

Terminal names should match the operation:

- Row queries use `Query*`.
- Scalar queries use `ExecuteScalar*`.
- Commands use `Execute*`.
- Regular single-row insert key readback uses explicit generated-key terminals.

Entity conveniences such as `Insert(entity)`, `Update(entity)`, and `Delete(entity)` are allowed when they remain predictable wrappers over the SQL-shaped builders.

## Scope

The v1 scope is intentionally compact:

- Typed single-table `SELECT`, `INSERT`, `UPDATE`, and `DELETE`.
- Scalar read helpers for exists, count, and bounded aggregate projections.
- Explicit filters, grouped filters, conditional filters, and single-column `IN` subqueries.
- Table and view overrides that still use the CLR model mapping.
- Entity and many-entity conveniences.
- Conflict-aware inserts where provider SQL is well understood.
- Lightweight transaction integration without entity tracking.

Complex SQL remains a Dapper concern:

- Joins
- CTEs
- Window functions
- Provider-specific hints
- Provider-native copy/import APIs
- Set-based synchronization

## Safety Boundaries

Db4Net should prefer explicit APIs over implicit translation:

- Values are always parameters.
- CLR member selectors map to known model metadata.
- Dynamic field names refer to CLR property names, not raw SQL.
- `UPDATE` and `DELETE` require a filter unless the caller explicitly opts into all-row commands.
- Provider-specific SQL differences must be documented when they affect compatibility.

## Documentation Boundaries

Current public behavior is documented in:

- `README.md`
- `src/Db4Net/README.md`
- `docs/vitepress/`
- `CHANGELOG.md`
- tests

Engineering context is documented in:

- `docs/engineering/decisions/`
- `docs/superpowers/specs/`
- `docs/superpowers/plans/`
- `docs/engineering/release-checklist.md`

Historical decision notes are useful context, but they are not the source of truth for the current API surface.
