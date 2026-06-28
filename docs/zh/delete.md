# 删除

使用 `DeleteFrom<T>()` 和显式筛选条件构建删除命令：

```csharp
var affected = await connection
    .UseDb4Net(Db4NetOptions.Sqlite)
    .DeleteFrom<User>()
    .Where(u => u.Id, Op.Eq, 1)
    .ExecuteAsync();
```

::: warning 安全默认值
`DELETE` 默认要求 `WHERE` 条件。只有确认要删除所有行时，才调用 `AllowAllRows()`。
:::

## 按主键删除实体

实体便捷方法会使用键元数据生成删除条件：

```csharp
await connection
    .UseDb4Net(Db4NetOptions.Sqlite)
    .Delete(user)
    .ExecuteAsync();
```

键来自 `[Key]`，或 `Id` / `<TypeName>Id` 约定。键元数据只用于相等谓词或冲突目标，不代表实体跟踪、级联删除或并发控制。
