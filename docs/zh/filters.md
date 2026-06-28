# 筛选

`Where(...)` 添加 `AND` 条件，`OrWhere(...)` 添加 `OR` 条件。类型化重载使用 CLR 成员选择器，并根据映射转换为数据库列名。

```csharp
var users = db.SelectFrom<User>()
    .Where(u => u.Id, Op.Eq, 1)
    .Where(u => u.Name, Op.Like, "A%")
    .Where(u => u.Id, Op.In, new[] { 1, 2, 3 })
    .Where(u => u.DeletedAt, Op.IsNull)
    .OrWhere(u => u.Name, Op.IsNotNull)
    .Query();
```

`Op.Eq` 搭配 `null` 会渲染为 `IS NULL`，`Op.NotEq` 搭配 `null` 会渲染为 `IS NOT NULL`。不需要值时，优先使用 `Op.IsNull` 和 `Op.IsNotNull`。

## 分组

混用 `AND` / `OR` 且需要明确括号时，使用 `WhereGroup(...)` 或 `OrWhereGroup(...)`：

```csharp
var users = db.SelectFrom<User>()
    .WhereGroup(group => group
        .Where(u => u.Id, Op.Eq, 1)
        .OrWhere(u => u.Name, Op.Eq, "Alice"))
    .Where(u => u.Id, Op.Gt, 0)
    .Query();
```

这类写法会渲染类似：

```sql
WHERE ([Id] = @p0 OR [Name] = @p1) AND [Id] > @p2
```

普通 `Where(...)` 与 `OrWhere(...)` 链会遵循 SQL 默认运算符优先级，不会自动添加括号。

::: tip 提示
分组构建器只暴露筛选方法，排序、分页和命令渲染仍保留在外层构建器上。
:::
