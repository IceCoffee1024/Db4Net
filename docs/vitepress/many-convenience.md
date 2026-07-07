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

These methods execute one validated, parameterized Dapper command per entity and return the total affected row count. Empty sequences return `0`. They do not return per-row generated keys; use regular single-row `Insert(entity).ExecuteReturnKey<TResult>()` when generated key readback is required.

## Inspect Commands

Many command builders expose `ToCommands()` for per-entity SQL inspection.

```csharp
var commands = db.InsertMany(users)
    .ToCommands();
```

## Transactions

Many APIs do not self-wrap in a transaction. Use `BeginTransaction()` or `ExecuteInTransaction(...)` when the whole sequence must be atomic.

```csharp
var db = connection.UseDb4Net(Db4NetOptions.SqlServer);

db.ExecuteInTransaction(tx =>
{
    tx.InsertMany(users).Execute();
    tx.UpdateMany(users).Execute();
});
```

For an existing transaction, keep using `Db4NetExecutionOptions.Transaction`. Db4Net forwards that transaction to Dapper and does not own its lifetime.

::: warning
`Many` APIs are Dapper multi-execute conveniences, not provider-native copy/import APIs, set-based synchronization, or optimized batching.
:::
