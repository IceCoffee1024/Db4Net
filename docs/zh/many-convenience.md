# 多实体便捷方法

对多个映射对象执行命令时，使用 `InsertMany(...)`、`UpdateMany(...)` 和 `DeleteMany(...)`。

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

这些方法会通过 Dapper 执行每个实体对应的已验证、参数化命令，并返回总影响行数。空序列返回 `0`。

::: warning 事务
Db4Net 不会自动创建、提交或回滚事务。如果多实体操作必须原子化，请通过 `Db4NetExecutionOptions` 传入事务，并在应用代码中管理事务生命周期。
:::

## 检查命令

单命令构建器支持 `ToCommand()`，`Many` 命令构建器支持 `ToCommands()`，可检查每个实体对应的命令。

```csharp
var commands = db.InsertMany(users).ToCommands();
```

`Many` 变体是 Dapper multi-execute 便捷方法，不是数据库原生 copy/import API、集合式同步抽象或优化批处理实现。
