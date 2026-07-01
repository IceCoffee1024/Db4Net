# Execution Options

Pass `Db4NetExecutionOptions` to terminal methods when you need Dapper execution settings.

```csharp
using var transaction = connection.BeginTransaction();

var users = connection
    .UseDb4Net(Db4NetOptions.SqlServer)
    .SelectFrom<User>()
    .Where(u => u.Id, Op.Gt, 0)
    .Query(new Db4NetExecutionOptions
    {
        Transaction = transaction,
        CommandTimeout = 30
    });
```

Execution options can carry:

- `Transaction`
- `CommandTimeout`
- `CommandType`

## Existing Transactions

Db4Net passes an existing transaction through to Dapper when you provide `Db4NetExecutionOptions.Transaction`. Create the transaction from the same connection used by `UseDb4Net(...)`, pass it to every terminal method that must participate, and commit or roll back it yourself.

You can also bind default execution options to a facade:

```csharp
using var transaction = connection.BeginTransaction();

var db = connection
    .UseDb4Net(Db4NetOptions.Sqlite)
    .WithTransaction(transaction);

db.Insert(user).Execute();
db.Update(otherUser).Execute();
```

`WithExecutionOptions(...)` can also bind default timeout or command type values:

```csharp
var db = connection
    .UseDb4Net(Db4NetOptions.Sqlite)
    .WithExecutionOptions(new Db4NetExecutionOptions
    {
        CommandTimeout = 30
    });
```

The connection extension also accepts default execution options directly:

```csharp
var db = connection.UseDb4Net(
    Db4NetOptions.Sqlite,
    new Db4NetExecutionOptions { CommandTimeout = 30 });
```

## Transaction Scopes

Use `BeginTransaction()` when Db4Net should own a lightweight transaction scope.

```csharp
var db = connection.UseDb4Net(Db4NetOptions.Sqlite);

using var tx = db.BeginTransaction();

tx.Insert(user).Execute();
tx.Update(otherUser).Execute();

tx.Commit();
```

Use `BeginTransaction(isolationLevel)` when the transaction should start with a specific `IsolationLevel`.

Disposing a `Db4NetTransaction` without calling `Commit()` rolls it back. You can also call `Rollback()` explicitly. After `Commit()`, `Rollback()`, or `Dispose()`, `tx.Database` and any transaction-bound builder or facade captured from it reject further execution.

Use `ExecuteInTransaction(...)` for delegate-based scopes:

```csharp
db.ExecuteInTransaction(tx =>
{
    tx.Insert(user).Execute();
    tx.Update(otherUser).Execute();
});
```

`ExecuteInTransaction(...)` commits when the delegate succeeds and rolls back when it throws. `ExecuteInTransactionAsync(...)` runs async Db4Net operations in the same transaction, but transaction begin, commit, and rollback use synchronous `IDbTransaction` APIs because Db4Net is bound through `IDbConnection`.

When raw Dapper SQL must participate in the current Db4Net transaction, use the transaction-bound facade and pass `transaction: tx.Database.DbTransaction` explicitly. In repositories, prefer the `Db4NetDatabase` context passed to the repository: `_db.Connection` and `_db.DbTransaction`. `Connection` is borrowed context; do not close, dispose, or open it from repository code. `DbTransaction` is `null` when no transaction is active. Repository constructors should usually receive only `Db4NetDatabase`.

```csharp
using Dapper;

db.ExecuteInTransaction(tx =>
{
    tx.Database.Connection.Execute(
        "INSERT INTO AuditLogs (EventName, EntityId) VALUES (@EventName, @EntityId)",
        new { EventName = "UserUpdated", EntityId = user.Id },
        transaction: tx.Database.DbTransaction);

    tx.Update(user).Execute();
});
```

When the transaction is created outside Db4Net, pass it to Dapper and bind Db4Net with `WithTransaction(transaction)`.

This is not an ORM unit of work: Db4Net does not track entities, detect changes, batch saves, or add `SaveChanges()`.

## Async Cancellation

Async terminal methods also accept a `CancellationToken`.

```csharp
var users = await connection
    .UseDb4Net(Db4NetOptions.Sqlite)
    .SelectFrom<User>()
    .QueryAsync(
        new Db4NetExecutionOptions { CommandTimeout = 30 },
        cancellationToken);
```

Command builders and many builders also accept options:

```csharp
var affected = await db.UpdateMany(users)
    .ExecuteAsync(
        new Db4NetExecutionOptions { Transaction = transaction },
        cancellationToken);
```

Generated-key insert terminals accept the same options and cancellation pattern:

```csharp
var id = await db.Insert(user)
    .ExecuteReturnKeyAsync<long>(
        new Db4NetExecutionOptions { Transaction = transaction },
        cancellationToken);

var explicitId = db.InsertInto<User>()
    .Values(user)
    .ReturnKey(u => u.Id)
    .Execute<long>(new Db4NetExecutionOptions { CommandTimeout = 30 });
```
