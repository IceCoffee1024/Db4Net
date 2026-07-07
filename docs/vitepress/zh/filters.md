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

常见操作符包括相等、比较、`Like`、`NotLike`、`In`、`NotIn` 和空值判断：

```csharp
.Where(u => u.Id, Op.In, new[] { 1, 2, 3 })
.Where(u => u.Id, Op.NotIn, blockedIds)
.Where(u => u.Name, Op.NotLike, "test%")
.Where(u => u.DeletedAt, Op.IsNull)
.Where(u => u.Name, Op.IsNotNull)
```

`Op.Eq` 搭配 `null` 会渲染为 `IS NULL`，`Op.NotEq` 搭配 `null` 会渲染为 `IS NOT NULL`。不需要值时，优先使用 `Op.IsNull` 和 `Op.IsNotNull`。

`Op.In` 和 `Op.NotIn` 要求传入非字符串、且至少包含一个元素的 enumerable。

## 范围筛选

使用 `WhereBetween(...)` 和 `OrWhereBetween(...)` 表达包含边界的 `BETWEEN` 谓词：

```csharp
var users = db.SelectFrom<User>()
    .WhereBetween(u => u.UpdatedAt, from, to)
    .OrWhereBetween(u => u.CreatedAt, archiveFrom, archiveTo)
    .Query();
```

上下界都会渲染为参数，并且不能为 `null`。可选范围条件可以使用 `WhereBetweenIf(...)` 和 `OrWhereBetweenIf(...)`。

## `IN` 子查询

当 `IN` 的值来自另一段单列 Db4Net `SELECT` 查询时，使用 `WhereIn(...)`、`OrWhereIn(...)`、`WhereNotIn(...)` 或 `OrWhereNotIn(...)`：

```csharp
var users = db.SelectFrom<User>()
    .WhereIn(
        u => u.Id,
        db.SelectFrom<Order>(o => o.UserId)
            .Where(o => o.Amount, Op.Gt, 100m))
    .Query();
```

子查询必须只选择一列。Db4Net 会把子查询渲染到 SQL 中，并让外层查询和子查询共享同一组参数编号。

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
