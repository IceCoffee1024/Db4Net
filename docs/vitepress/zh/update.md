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

筛选条件使用和 SELECT 查询相同的 `Where(...)`、`OrWhere(...)`、`WhereGroup(...)` 和 `OrWhereGroup(...)` API。

## 实体取值与键

当你希望从对象读取映射的非 key 值，并从同一个对象读取 key 谓词时，使用 `Update(entity)`：

```csharp
var affected = db.Update(user)
    .Execute();
```

当只有 key 谓词需要来自对象，而更新字段仍要显式控制时，使用 `WhereKey(entity)`：

```csharp
var affected = db.Update<User>()
    .Set(u => u.Name, "Alice")
    .WhereKey(user)
    .Execute();
```

`Update(entity)` 和 `WhereKey(entity)` 要求模型有且只有一个映射 key，并且 key 值不是 `null`、`0` 或对应类型的默认值。复合 key 模型请使用显式 `Where(...)` 条件。

## 所有行

`UPDATE` 默认要求 `WHERE` 条件。

```csharp
var affected = db.Update<User>()
    .Set(u => u.IsActive, false)
    .AllowAllRows()
    .Execute();
```

::: warning
只有确认要更新每一行时，才调用 `AllowAllRows()`。
:::
