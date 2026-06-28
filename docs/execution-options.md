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

Db4Net passes the transaction through to Dapper. It does not begin, commit, or roll back transactions. Create the transaction from the same connection used by `UseDb4Net(...)`, pass it to every terminal method that must participate, and commit or roll back it yourself.

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
