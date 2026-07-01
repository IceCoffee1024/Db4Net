# 执行选项

需要事务、命令超时或命令类型时，向终结方法传入 `Db4NetExecutionOptions`。

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

## 使用已有事务

通过 `Db4NetExecutionOptions.Transaction` 提供事务时，Db4Net 会把这个已有事务传给 Dapper。事务应来自调用 `UseDb4Net(...)` 的同一个连接；需要参与同一事务的每个终结方法都要显式传入同一个 `Db4NetExecutionOptions`，最后由调用方自行提交或回滚。

也可以把默认事务绑定到一个 facade：

```csharp
using var transaction = connection.BeginTransaction();

var db = connection
    .UseDb4Net(Db4NetOptions.Sqlite)
    .WithTransaction(transaction);

db.Insert(user).Execute();
db.Update(otherUser).Execute();
```

`WithExecutionOptions(...)` 也可以绑定默认命令超时或命令类型：

```csharp
var db = connection
    .UseDb4Net(Db4NetOptions.Sqlite)
    .WithExecutionOptions(new Db4NetExecutionOptions
    {
        CommandTimeout = 30
    });
```

连接扩展方法也可以直接接收默认执行选项：

```csharp
var db = connection.UseDb4Net(
    Db4NetOptions.Sqlite,
    new Db4NetExecutionOptions { CommandTimeout = 30 });
```

## 事务作用域

需要由 Db4Net 拥有一个轻量事务作用域时，使用 `BeginTransaction()`：

```csharp
var db = connection.UseDb4Net(Db4NetOptions.Sqlite);

using var tx = db.BeginTransaction();

tx.Insert(user).Execute();
tx.Update(otherUser).Execute();

tx.Commit();
```

需要指定隔离级别时，使用 `BeginTransaction(isolationLevel)`。

`Db4NetTransaction` 如果在没有调用 `Commit()` 的情况下被释放，会执行回滚。也可以显式调用 `Rollback()`。调用 `Commit()`、`Rollback()` 或 `Dispose()` 之后，`tx.Database` 以及从它捕获的事务绑定 builder/facade 都会拒绝继续执行。

也可以使用委托式作用域：

```csharp
db.ExecuteInTransaction(tx =>
{
    tx.Insert(user).Execute();
    tx.Update(otherUser).Execute();
});
```

`ExecuteInTransaction(...)` 在委托成功时提交，委托抛异常时回滚。`ExecuteInTransactionAsync(...)` 可以在同一个事务里运行异步 Db4Net 操作，但事务开启、提交和回滚仍使用同步 `IDbTransaction` API，因为 Db4Net 是通过 `IDbConnection` 绑定连接的。

如果 Dapper 原生 SQL 要加入 Db4Net 创建的事务，使用 `tx.Connection`，并把 `tx.DbTransaction` 传给 Dapper。

```csharp
using Dapper;

db.ExecuteInTransaction(tx =>
{
    tx.Connection.Execute(
        "INSERT INTO AuditLogs (EventName, EntityId) VALUES (@EventName, @EntityId)",
        new { EventName = "UserUpdated", EntityId = user.Id },
        transaction: tx.DbTransaction);

    tx.Update(user).Execute();
});
```

如果事务由 Db4Net 外部创建并拥有，再把它传给 Dapper，并通过 `WithTransaction(transaction)` 绑定到 Db4Net。

这不是 ORM Unit of Work：Db4Net 不跟踪实体、不检测变更、不批量保存，也不提供 `SaveChanges()`。

异步终结方法也接受 `CancellationToken`：

```csharp
var users = await connection
    .UseDb4Net(Db4NetOptions.Sqlite)
    .SelectFrom<User>()
    .QueryAsync(
        new Db4NetExecutionOptions { CommandTimeout = 30 },
        cancellationToken);
```

`INSERT`、`UPDATE`、`DELETE` 和冲突插入构建器提供命令终结方法：

- `Execute()`
- `ExecuteAsync()`

常规单行插入的生成键终结方法也接受相同的执行选项和取消令牌：

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

::: tip 提示
多实体便捷方法不会自己包一层事务。需要原子性时，使用 `BeginTransaction()` / `ExecuteInTransaction(...)`，或者通过 `Db4NetExecutionOptions.Transaction` 传入已有事务。
:::
