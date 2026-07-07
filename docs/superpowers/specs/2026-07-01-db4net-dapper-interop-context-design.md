# Db4NetDatabase Dapper Interop Context Design

## Context

Db4Net is intentionally a Dapper-adjacent SQL builder, not a full ORM. Real applications still need raw Dapper for complex joins, CTEs, window functions, provider-specific SQL, and reporting queries. Before this design, repository examples that mixed Db4Net and raw Dapper had to choose between two awkward options:

- Inject extra `IDbConnection` / `IDbTransaction` constructor parameters into repositories that otherwise only need `Db4NetDatabase`.
- Reach through `Db4NetTransaction` directly, which works in transaction scopes but does not give repositories a single constructor shape.

The long-term API should let a repository accept one `Db4NetDatabase` and use it for both Db4Net builders and raw Dapper execution.

## Decision

Expose the current Dapper execution context from `Db4NetDatabase`:

```csharp
public IDbConnection Connection { get; }

public IDbTransaction? DbTransaction { get; }
```

`Connection` is borrowed context. Callers may pass it to Dapper but must not open, close, or dispose it from repository code. A facade created only for SQL rendering with `Db4NetDatabase.Create(options)` still has no connection; accessing `Connection` throws the same `InvalidOperationException` as terminal execution.

`DbTransaction` returns the default transaction used by Db4Net terminal execution, or `null` when no default transaction is configured. This matches Dapper's `transaction:` argument shape.

## Transaction Semantics

`Db4NetTransaction.Database` remains the primary transaction-bound facade:

```csharp
using var tx = db.BeginTransaction();

var users = new UserRepository(tx.Database);
await users.DisableAsync(id, cancellationToken);

tx.Database.Connection.Execute(
    "INSERT INTO AuditLogs (EventName, EntityId) VALUES (@EventName, @EntityId)",
    new { EventName = "UserDisabled", EntityId = id },
    transaction: tx.Database.DbTransaction);

tx.Commit();
```

The transaction-bound facade validates the owning `Db4NetTransaction` before exposing `Connection`, exposing `DbTransaction`, or executing Db4Net terminal methods. After commit, rollback, or dispose, captured builders and captured facades reject further use.

`WithTransaction(IDbTransaction)` should also work from a SQL-only facade when the transaction has an associated connection:

```csharp
using var transaction = connection.BeginTransaction();

var db = Db4NetDatabase
    .Create(Db4NetOptions.Sqlite)
    .WithTransaction(transaction);
```

This keeps externally owned transactions usable without requiring a separate connection parameter.

## Kept APIs

Keep `Db4NetTransaction` and `Db4NetTransactionExtensions`.

`Db4NetTransaction` still owns transactions created by Db4Net and provides explicit `Commit()` / `Rollback()` lifecycle methods. Its direct `Connection` and `DbTransaction` properties remain valid low-level transaction-scope accessors, but documentation should prefer `tx.Database.Connection` and `tx.Database.DbTransaction` for repository and Dapper interop examples.

## Repository Pattern

Repositories should usually accept only `Db4NetDatabase`:

```csharp
public sealed class AuditRepository
{
    private readonly Db4NetDatabase _db;

    public AuditRepository(Db4NetDatabase db)
    {
        _db = db;
    }

    public Task<int> WriteRawAsync(
        string eventName,
        long entityId,
        CancellationToken cancellationToken = default)
    {
        return _db.Connection.ExecuteAsync(
            new CommandDefinition(
                "INSERT INTO AuditLogs (EventName, EntityId) VALUES (@EventName, @EntityId)",
                new { EventName = eventName, EntityId = entityId },
                transaction: _db.DbTransaction,
                cancellationToken: cancellationToken));
    }
}
```

This works for request-scoped repositories, repositories created from `tx.Database`, and DI-created repositories inside an application-side unit of work.

## Non-Goals

This design does not add:

- Ambient transactions.
- Async transaction ownership APIs.
- A raw SQL wrapper over Dapper.
- Automatic transaction detection from arbitrary repositories.
- Multi-database transaction coordination.
- A DbContext-style change tracker or `SaveChanges()` unit of work.

## Tests

The implementation should verify:

- `Db4NetDatabase.Connection` exposes the bound connection.
- SQL-only facades throw on `Connection`.
- `Db4NetDatabase.DbTransaction` is `null` without a default transaction.
- `WithTransaction(IDbTransaction)` exposes the transaction and can derive the connection.
- `Db4NetTransaction.Database.Connection` and `.DbTransaction` reject access after commit, rollback, or dispose.
- Raw Dapper SQL can participate in a Db4Net-owned transaction through `tx.Database.Connection` and `tx.Database.DbTransaction`.

## Documentation

Public docs should describe this as a Dapper interop context:

- Use `_db.Connection` for raw Dapper.
- Pass `transaction: _db.DbTransaction` when the repository was created with a transaction-bound database facade.
- Do not dispose or open the borrowed connection in repository code.
- Keep transaction boundaries in services or application-side units of work.
- For large DI applications, use a generic repository creation helper such as `Repository<TRepository>()` so repositories still keep a single `Db4NetDatabase` constructor.
