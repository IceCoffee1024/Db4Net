# 实体便捷方法

Db4Net 提供单实体命令便捷方法。这里的“实体”只是一个已映射 CLR 对象，用作取值来源，不是被 Db4Net 跟踪的 ORM 实体。

```csharp
await connection
    .UseDb4Net(Db4NetOptions.Sqlite)
    .Insert(user)
    .ExecuteAsync();

var affected = connection
    .UseDb4Net(Db4NetOptions.SqlServer)
    .Update(user, table: "users_2026")
    .Execute();

await connection
    .UseDb4Net(Db4NetOptions.Sqlite)
    .Delete(user)
    .ExecuteAsync();
```

这些方法会读取对象上的映射值，构建与 SQL 风格构建器相同的已验证、参数化命令。

单行插入需要返回插入键时，使用生成键终结方法：

```csharp
var id = await connection
    .UseDb4Net(Db4NetOptions.Sqlite)
    .Insert(user)
    .ExecuteReturnKeyAsync<long>();
```

单实体便捷方法会拒绝 `List<User>` 或 `User[]` 这类序列值。多个对象请使用 `InsertMany(...)`、`UpdateMany(...)`、`DeleteMany(...)`、`InsertOrIgnoreMany(...)` 或 `InsertOrUpdateMany(...)`。

## 键元数据

`[Key]` 以及 `Id` / `<TypeName>Id` 约定用于：

- `Update(user)`
- `Delete(user)`
- `WhereKey(user)`
- `UpdateMany(users)`
- `DeleteMany(users)`
- 冲突插入未显式调用 `OnConflict(...)` 时的默认冲突目标

::: warning 注意
键元数据不代表变更跟踪、生成值回读、关系身份映射或自动并发行为。
:::

`Insert(entity).ExecuteReturnKey<TResult>()` 只会显式回读单行插入的一个键值，不会修改实体实例，也不会刷新所有 computed/generated 列。模型有多个键时，请传入显式键选择器，例如 `ExecuteReturnKey<long>(u => u.Id)`。
