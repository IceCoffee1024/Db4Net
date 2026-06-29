# Changelog

All notable changes to Db4Net will be documented in this file.

## 0.1.0-alpha.1 - 2026-06-29

### Added

- Typed single-table `SELECT` builders with mapped projections, Dapper-style query terminal methods, async terminal methods, and explicit SQL inspection through `ToCommand()`.
- Typed existence query builders through `SelectExistsFrom<T>()` and `table` override overloads.
- Typed count query builders through `SelectCountFrom<T>()` and `table` override overloads.
- Paged SELECT terminal methods through `QueryPage(...)` and `QueryPageAsync(...)`, returning `PagedResult<T>` with rows, total count, page number, page size, and total pages.
- Typed scalar aggregate query builders through `SelectAggregateFrom<T>()`, including `Max`, `Min`, `Sum`, `Average`, `CountDistinct`, terminal result typing for all scalar aggregates, and `table` override overloads.
- Single-column SELECT subquery filters through `WhereIn`, `OrWhereIn`, `WhereNotIn`, and `OrWhereNotIn` on `SelectQueryBuilder`.
- Typed `INSERT`, `UPDATE`, and `DELETE` command builders with `Execute()` / `ExecuteAsync()`.
- Single-row INSERT generated-key terminals for regular insert builders: `ExecuteReturnKey<TResult>()`, `ExecuteReturnKeyAsync<TResult>()`, `ReturnKey(...).Execute<TResult>()`, and `ReturnKey(...).ExecuteAsync<TResult>()`.
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
- Bilingual complete example documentation page with recommended usage patterns.
- Bilingual repository pattern documentation page for data access layering, scoped DI registration, and transaction scopes.
- Documentation-site changelog entry that links back to this root `CHANGELOG.md`.
- Documentation-site favicon and navbar logo.

### Changed

- Public API naming is SQL-shaped and intentionally avoids ORM-style `Save`, `SaveChanges`, `Merge`, `Upsert`, and `Bulk` names.
- String field names are CLR property names after a model is bound; database column names are supplied through `[Column]`.
- `UPDATE` and `DELETE` require a `WHERE` clause by default unless `AllowAllRows()` is called explicitly.
- Single-entity command conveniences now reject sequence values with messages that point to the matching `Many` API.
- Entity-driven updates now omit database-generated non-key columns, while explicit `.Set(...)` calls remain caller-controlled.
- Conflict-aware insert defaults now allow composite key metadata; entity update/delete and many update/delete conveniences still require a single key.
- Transaction documentation now separates existing transaction pass-through from Db4Net-owned lightweight transaction scopes.
- `SELECT` rendering now rejects `Offset(...)` without `Limit(...)` and SQL Server paging without `OrderBy(...)` instead of ignoring or rendering invalid paging SQL.
- Typed projected `SELECT` entry points now use `SelectFrom<T>(...)`; the older facade-level generic `Select` projection entry point was removed before the first public package.

### Known Limitations

- No joins; use database views for stable read models or Dapper raw SQL for complex provider-specific SQL.
- No LINQ provider or full predicate expression translation such as `Where(u => u.Id == 1)`.
- No change tracking, relationship loading, migrations, automatic concurrency tokens, or unit-of-work behavior.
- Many-entity conveniences are not optimized bulk import/copy or set-based synchronization APIs.
- Generated-key readback does not apply to `InsertMany`, conflict-aware inserts, or full generated/computed value refresh.
