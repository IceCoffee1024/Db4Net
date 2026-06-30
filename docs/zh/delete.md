# 删除

使用 `DeleteFrom<T>()` 和显式筛选条件构建删除命令：

```csharp
var affected = await connection
    .UseDb4Net(Db4NetOptions.Sqlite)
    .DeleteFrom<User>()
    .Where(u => u.Id, Op.Eq, 1)
    .ExecuteAsync();
```

筛选条件使用和 SELECT 查询相同的 `Where(...)`、`OrWhere(...)`、`WhereGroup(...)` 和 `OrWhereGroup(...)` API。

## 按键删除

当 key 元数据应该成为删除谓词时，使用 `WhereKey(entity)`：

```csharp
var affected = db.DeleteFrom<User>()
    .WhereKey(user)
    .Execute();
```

`Delete(entity)` 和 `WhereKey(entity)` 要求模型有且只有一个映射 key，并且 key 值不是 `null`、`0` 或对应类型的默认值。复合 key 模型请使用显式 `Where(...)` 条件。

## 所有行

`DELETE` 默认要求 `WHERE` 条件。

```csharp
var affected = db.DeleteFrom<User>()
    .AllowAllRows()
    .Execute();
```

::: warning
只有确认要删除每一行时，才调用 `AllowAllRows()`。
:::
