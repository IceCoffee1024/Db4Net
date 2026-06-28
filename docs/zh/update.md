# 更新

使用 `Update<T>()`、`Set(...)` 和显式筛选条件构建更新命令：

```csharp
var affected = connection
    .UseDb4Net(Db4NetOptions.SqlServer)
    .Update<User>()
    .Set(u => u.Name, "Alice")
    .Where(u => u.Id, Op.Eq, 1)
    .Execute();
```

`Set(...)` 使用 CLR 成员选择器，值会作为参数传递。

::: warning 安全默认值
`UPDATE` 默认要求 `WHERE` 条件。只有确认要影响整张表时，才调用 `AllowAllRows()`。
:::

## 按主键更新实体

实体便捷方法会从映射对象读取值，并根据键元数据生成 `WHERE` 条件：

```csharp
var affected = connection
    .UseDb4Net(Db4NetOptions.SqlServer)
    .Update(user)
    .Execute();
```

需要更明确字段或谓词时，优先使用 SQL 风格构建器：

```csharp
db.Update<User>()
    .Set(u => u.Name, "Alice")
    .Where(u => u.Id, Op.Eq, user.Id)
    .Execute();
```
