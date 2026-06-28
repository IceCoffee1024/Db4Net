# Many Convenience

Use many-entity conveniences when you have multiple mapped CLR objects.

```csharp
var inserted = db
    .InsertMany(users)
    .Execute();

var updated = db
    .UpdateMany(users, table: "users_2026")
    .Execute();

var deleted = db
    .DeleteMany(users)
    .Execute();
```

These methods execute one validated, parameterized Dapper command per entity and return the total affected row count. Empty sequences return `0`.

## Inspect Commands

Many command builders expose `ToCommands()` for per-entity SQL inspection.

```csharp
var commands = db.InsertMany(users)
    .ToCommands();
```

## Transactions

Db4Net does not create an automatic transaction around many operations. Pass one through `Db4NetExecutionOptions` when the whole operation must be atomic.

```csharp
using var transaction = connection.BeginTransaction();

var affected = connection
    .UseDb4Net(Db4NetOptions.SqlServer)
    .UpdateMany(users)
    .Execute(new Db4NetExecutionOptions
    {
        Transaction = transaction
    });
```

Db4Net does not commit or roll back that transaction. Manage the transaction lifetime in application code.

::: warning
`Many` APIs are Dapper multi-execute conveniences, not provider-native copy/import APIs, set-based synchronization, or optimized batching.
:::
