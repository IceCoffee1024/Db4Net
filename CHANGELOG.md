# Changelog

All notable changes to Db4Net will be documented in this file.

## Unreleased

### Added

- Typed single-table `SELECT` builders with mapped projections, Dapper-style query terminal methods, async terminal methods, and explicit SQL inspection through `ToCommand()`.
- Typed existence query builders through `SelectExistsFrom<T>()` and `table` override overloads.
- Typed count query builders through `SelectCountFrom<T>()` and `table` override overloads.
- Typed scalar aggregate query builders through `SelectAggregateFrom<T>()`, including `Max`, `Min`, `CountDistinct`, and `table` override overloads.
- Typed `INSERT`, `UPDATE`, and `DELETE` command builders with `Execute()` / `ExecuteAsync()`.
- Entity command conveniences: `Insert(entity)`, `Update(entity)`, `Delete(entity)`, and `table` override overloads.
- Many-entity conveniences: `InsertMany`, `UpdateMany`, `DeleteMany`, and `table` override overloads. These use Dapper multi-execute style per-entity commands, not provider-native import/copy APIs.
- Conflict-aware insert commands: `InsertOrIgnore`, `InsertOrIgnoreMany`, `InsertOrUpdate`, `InsertOrUpdateMany`, `OnConflict(...)`, and conflict update column selection.
- Explicit filter grouping through `WhereGroup(...)` and `OrWhereGroup(...)`.
- Mapped CLR property-name string APIs for dynamic grids, forms, exports, and field-level permission scenarios.
- Table/view target overrides for typed `SELECT`, `INSERT`, `UPDATE`, and `DELETE` builders.
- SQL Server, SQLite, PostgreSQL, and MySQL dialect rendering.
- Dapper execution options for transaction, command timeout, command type, and async cancellation tokens.
- Lightweight transaction scopes through `WithTransaction(...)`, `BeginTransaction()`, `ExecuteInTransaction(...)`, and `ExecuteInTransactionAsync(...)`.
- NuGet package assets for `net8.0` and `netstandard2.0`.
- Optional PostgreSQL, MySQL, and SQL Server integration tests via environment variables or local runsettings.
- SQLite integration coverage for single-entity command transaction execution.
- Bilingual VitePress documentation site with English and Simplified Chinese user guides.
- Documentation-site changelog entry that links back to this root `CHANGELOG.md`.
- Documentation-site favicon and navbar logo.

### Changed

- Public API naming is SQL-shaped and intentionally avoids ORM-style `Save`, `SaveChanges`, `Merge`, `Upsert`, and `Bulk` names.
- String field names are CLR property names after a model is bound; database column names are supplied through `[Column]`.
- `UPDATE` and `DELETE` require a `WHERE` clause by default unless `AllowAllRows()` is called explicitly.
- Single-entity command conveniences now reject sequence values with messages that point to the matching `Many` API.
- Transaction documentation now separates existing transaction pass-through from Db4Net-owned lightweight transaction scopes.

### Known Limitations

- No joins; use database views for stable read models or Dapper raw SQL for complex provider-specific SQL.
- No LINQ provider or full predicate expression translation such as `Where(u => u.Id == 1)`.
- No change tracking, relationship loading, migrations, automatic concurrency tokens, or unit-of-work behavior.
- Many-entity conveniences are not optimized bulk import/copy or set-based synchronization APIs.
