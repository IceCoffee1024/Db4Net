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

Db4Net 只会把事务传给 Dapper，不会自动开启、提交或回滚事务。事务应来自调用 `UseDb4Net(...)` 的同一个连接；需要参与同一事务的每个终结方法都要显式传入同一个 `Db4NetExecutionOptions`，最后由调用方自行提交或回滚。

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

::: tip 提示
多实体便捷方法不会自动创建事务。需要原子性时，把事务放入 `Db4NetExecutionOptions` 并传给执行方法。
:::
