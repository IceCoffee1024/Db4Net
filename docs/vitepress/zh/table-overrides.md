# 表名覆盖

当同一个 CLR 模型需要用于租户表、时间分区表、暂存表、归档表或视图时，可以覆盖 SQL 目标表。

```csharp
var affected = connection
    .UseDb4Net(Db4NetOptions.SqlServer)
    .Update<User>("users_tenant_001")
    .Set(u => u.Name, "Alice")
    .Where(u => u.Id, Op.Eq, 1)
    .Execute();
```

常用重载包括：

- `SelectFrom<T>("view_or_table")`
- `Select(...).From<T>("view_or_table")`
- `InsertInto<T>("users_staging")`
- `Update<T>("users_2026")`
- `DeleteFrom<T>("users_2026")`
- `Insert(entity, table)`
- `Update(entity, table)`
- `Delete(entity, table)`
- `InsertMany(users, table)`
- `UpdateMany(users, table)`
- `DeleteMany(users, table)`
- `InsertOrIgnore(..., table)`
- `InsertOrUpdate(..., table)`

::: tip 提示
表名覆盖只改变 SQL 目标表。属性到列的映射仍来自 `T`，表或视图标识符会由配置的方言验证并引用。
:::
