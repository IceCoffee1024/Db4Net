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

## Paging

- SQL Server: `OFFSET ... ROWS FETCH NEXT ... ROWS ONLY`
- SQLite: `LIMIT ... OFFSET ...`
- PostgreSQL: `LIMIT ... OFFSET ...`
- MySQL: `LIMIT ... OFFSET ...`

## Conflict Inserts

SQLite and PostgreSQL render native `ON CONFLICT` syntax. MySQL renders `ON DUPLICATE KEY UPDATE`; explicit `OnConflict(...)` selectors declare Db4Net's intended conflict columns, but MySQL handles any primary or unique key violation according to its own duplicate-key rules.

SQL Server renders a dialect-specific conflict-aware command. It is not a provider-native import/copy API, optimized batch import, or set-based synchronization abstraction.
