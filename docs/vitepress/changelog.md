# Changelog

## 0.1.0-alpha.6 - 2026-07-02

### Added

- Scalar SELECT terminal methods now use `ExecuteScalar(...)` / `ExecuteScalarAsync(...)` on count, exists, and aggregate builders.
- `Op.NotLike`, `Op.NotIn`, and `WhereBetween(...)` / `OrWhereBetween(...)` filter APIs.
- `WhereBetweenIf(...)` and `OrWhereBetweenIf(...)` for conditional range filters.

### Changed

- Breaking change: scalar SELECT builders no longer use `Execute(...)` / `ExecuteAsync(...)`; use `ExecuteScalar(...)` / `ExecuteScalarAsync(...)`.
- MySQL `InsertOrUpdate(...)` now follows MySQL 8.0.19+ row-alias upsert syntax. MySQL 5.7, MySQL 8.0.0-8.0.18, and MariaDB are not compatible with that generated `InsertOrUpdate(...)` SQL.
- Dialect-specific conflict insert SQL is now rendered by the dialect layer.

## 0.1.0-alpha.5 - 2026-07-01

### Added

- Conditional filtering for read-only SELECT builders with `When(...)`, `WhereIf(...)`, `OrWhereIf(...)`, and `WhereGroupIf(...)`, including row, count, exists, and aggregate queries.
- Runtime ORDER BY direction overloads for SELECT builders through `OrderBy(..., bool descending)`.

## 0.1.0-alpha.4 - 2026-07-01

### Added

- `Db4NetDatabase.Connection` and `Db4NetDatabase.DbTransaction` expose borrowed raw Dapper interop context on the database facade.

### Changed

- Repository and unit-of-work documentation now uses a single `Repository<T>()` path: repositories inject only `Db4NetDatabase`, use `_db.Connection` for raw Dapper, and pass `transaction: _db.DbTransaction` explicitly when joining the current transaction.
- Raw Dapper transaction examples now prefer `_db.Connection` / `_db.DbTransaction` or `tx.Database.Connection` / `tx.Database.DbTransaction`; `Db4NetTransaction` remains the transaction scope and `tx.Database` remains the transaction-bound facade.

## 0.1.0-alpha.3 - 2026-07-01

### Added

- `Db4NetTransaction.Connection` and `Db4NetTransaction.DbTransaction` for raw Dapper SQL that must participate in a Db4Net-owned transaction.

### Changed

- Application pattern documentation now covers larger DI applications, transaction runners, repository creation with `ActivatorUtilities`, and raw Dapper transaction sharing.

## 0.1.0-alpha.2 - 2026-07-01

### Added

- Throwing SELECT terminal methods: `QueryFirst`, `QueryFirstAsync`, `QuerySingle`, and `QuerySingleAsync`.

### Changed

- Documentation now clarifies `FindByIdAsync` vs `GetByIdAsync` repository naming and SELECT terminal method semantics.

## 0.1.0-alpha.1 - 2026-06-29

### Added

- SQL-shaped typed `SELECT`, `INSERT`, `UPDATE`, and `DELETE` builders.
- Existence, count, paging, scalar aggregate, and subquery filter APIs.
- Entity and many-entity command conveniences.
- Conflict-aware inserts for SQL Server, SQLite, PostgreSQL, and MySQL.
- Explicit filter grouping and CLR property-name string APIs.
- Lightweight transaction scopes and Dapper execution options.
- `net8.0` and `netstandard2.0` package assets with XML docs and symbols.
- English and Simplified Chinese documentation.

### Changed

- Public API names are SQL-shaped and intentionally avoid ORM-style `Save`, `SaveChanges`, `Merge`, `Upsert`, and `Bulk` names.
- String field names are CLR property names after a model is bound.
- `UPDATE` and `DELETE` require a `WHERE` clause unless `AllowAllRows()` is called.
- Entity-driven updates omit database-generated non-key columns.
- Paging validation rejects invalid `Offset(...)` / SQL Server paging combinations.

### Known Limitations

- No joins; use database views or raw Dapper SQL for complex provider-specific SQL.
- No LINQ provider or full predicate expression translation.
- No change tracking, relationship loading, migrations, automatic concurrency tokens, or unit-of-work behavior.
- Many-entity conveniences are not provider-native bulk import/copy APIs.
- Generated-key readback does not apply to `InsertMany`, conflict-aware inserts, or full generated/computed value refresh.

::: tip
The root [`CHANGELOG.md`](https://github.com/IceCoffee1024/Db4Net/blob/main/CHANGELOG.md) remains the authoritative detailed changelog used for release preparation and NuGet packaging.
:::
