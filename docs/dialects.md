# Dialects

Db4Net renders SQL through the configured dialect.

```csharp
var db = connection.UseDb4Net(Db4NetOptions.SqlServer);
```

Supported dialect presets:

- `Db4NetOptions.SqlServer`
- `Db4NetOptions.Sqlite`
- `Db4NetOptions.PostgreSql`
- `Db4NetOptions.MySql`

Dialects control identifier quoting, paging syntax, and conflict-aware insert SQL.
They also control how regular single-row inserts return generated keys.

## Paging

- SQL Server: `OFFSET ... ROWS FETCH NEXT ... ROWS ONLY`
- SQLite: `LIMIT ... OFFSET ...`
- PostgreSQL: `LIMIT ... OFFSET ...`
- MySQL: `LIMIT ... OFFSET ...`

`Offset(...)` must be paired with `Limit(...)`. SQL Server paging also requires at least one `OrderBy(...)` because `OFFSET` / `FETCH` is invalid without `ORDER BY`.

## Conflict Inserts

SQLite and PostgreSQL render native `ON CONFLICT` syntax. MySQL renders `ON DUPLICATE KEY UPDATE`; explicit `OnConflict(...)` selectors declare Db4Net's intended conflict columns, but MySQL handles any primary or unique key violation according to its own duplicate-key rules.

SQL Server renders a dialect-specific conflict-aware command. It is not a provider-native import/copy API, optimized batch import, or set-based synchronization abstraction.

## Generated Keys

Regular single-row insert key terminals render provider-specific scalar SQL:

- SQL Server: `OUTPUT INSERTED.[Id]`
- SQLite: `RETURNING "Id"`
- PostgreSQL: `RETURNING "Id"`
- MySQL: `SELECT LAST_INSERT_ID()` for auto-increment identity keys, or `SELECT @p0` when the selected key value was explicitly included in the insert values

This support is for regular single-row inserts only. Many inserts and conflict-aware inserts still return affected row counts.

Provider caveats:

- SQLite requires runtime SQLite 3.35 or newer for `RETURNING`.
- SQL Server direct `OUTPUT INSERTED...` is intended for ordinary target tables; tables with enabled triggers can require an `OUTPUT ... INTO` pattern that Db4Net does not currently generate.
- MySQL generated-key readback is identity-oriented; keys produced by defaults, triggers, or expressions that are not auto-increment identities are not returned through `LAST_INSERT_ID()`.
