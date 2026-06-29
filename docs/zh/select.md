# 查询

使用 `SelectFrom<T>()` 查询完整映射实体。Db4Net 会选择所有已映射列，并为 Dapper 映射回 CLR 属性名提供别名。

```csharp
var users = connection
    .UseDb4Net(Db4NetOptions.SqlServer)
    .SelectFrom<User>()
    .Where(u => u.Name, Op.Like, "A%")
    .OrderBy(u => u.Id)
    .Query();
```

使用 `SelectFrom<T>(...)` 查询指定映射属性：

```csharp
var users = connection
    .UseDb4Net(Db4NetOptions.Sqlite)
    .SelectFrom<User>(u => u.Id, u => u.Name)
    .Where(u => u.Id, Op.In, new[] { 1, 2, 3 })
    .Query();
```

当列集合由用户选择、导出模板或字段权限动态决定，但结果模型仍然已知时，可以使用 CLR 属性名字符串：

```csharp
var rows = connection
    .UseDb4Net(Db4NetOptions.SqlServer)
    .Select("Id", "Name")
    .From<User>()
    .Where("Name", Op.IsNotNull)
    .Query();
```

::: warning 注意
字符串字段是 CLR 属性名，不是数据库列名或 SQL 片段。应用 `[Column("display_name")]` 后，仍应写 `"Name"` 或你的 CLR 属性名，而不是 `"display_name"`。
:::

## 存在性、计数和聚合查询

使用 `SelectExistsFrom<T>()` 执行存在性检查。它是受支持的存在性检查 API；如果只关心是否存在，优先使用它，而不是 `SelectCountFrom<T>().Execute() > 0`：

```csharp
var exists = db
    .SelectExistsFrom<User>()
    .Where(u => u.Id, Op.Eq, id)
    .Execute();

var existsInArchive = await db
    .SelectExistsFrom<User>("users_2026")
    .Where(u => u.Name, Op.Like, "A%")
    .ExecuteAsync();
```

需要匹配行数时，使用 `SelectCountFrom<T>()`：

```csharp
var count = db
    .SelectCountFrom<User>()
    .Where(u => u.Id, Op.Gt, 0)
    .Execute();

var matchingCount = await db
    .SelectCountFrom<User>("users_2026")
    .Where(u => u.Name, Op.Like, "A%")
    .ExecuteAsync();
```

需要列级标量聚合时，使用 `SelectAggregateFrom<T>()`。`Max(...)`、`Min(...)`、`Sum(...)`、`Average(...)` 和 `CountDistinct(...)` 会构建标量聚合投影。显式结果类型放在终端 `Execute<TResult>()` 或 `ExecuteAsync<TResult>()` 调用上，例如 `Max(selector).Execute<TResult>()` 或 `CountDistinct(selector).ExecuteAsync<long>()`；需要保留 SQL 在空结果集上返回的 `NULL` 时，使用可空的 `TResult`。

```csharp
var latestId = db
    .SelectAggregateFrom<User>()
    .Max(u => u.Id)
    .Where(u => u.Name, Op.Like, "A%")
    .Execute<int?>();

var distinctNames = await db
    .SelectAggregateFrom<User>("users_2026")
    .CountDistinct(u => u.Name)
    .ExecuteAsync<long>();

var totalAmount = db
    .SelectAggregateFrom<OrderMetric>()
    .Sum(o => o.Amount)
    .Execute<decimal>();

var totalQuantity = db
    .SelectAggregateFrom<OrderMetric>()
    .Sum(o => o.Quantity)
    .Execute<long>();

var averageQuantity = db
    .SelectAggregateFrom<OrderMetric>()
    .Average(o => o.Quantity)
    .Execute<decimal>();
```

不要用 `Select("COUNT(*)")`、`Select("MAX(...)")`、`Select("SUM(...)")`、`Select("AVG(...)")` 或类似字符串表达标量查询。字符串选择值会被当作已验证的标识符，而不是原始 SQL 表达式。

## 分页

使用 `Page(...)` 进行从 1 开始的分页；需要直接控制行数时，也可以组合 `Limit(...)` 和 `Offset(...)`：

```csharp
var page = db
    .SelectFrom<User>()
    .OrderBy(u => u.Id)
    .Page(pageNumber: 2, pageSize: 20)
    .Query();
```

`Offset(...)` 必须与 `Limit(...)` 配套使用。SQL Server 分页还要求至少调用一次 `OrderBy(...)`，因为没有 `ORDER BY` 时 `OFFSET` / `FETCH` 是无效 SQL。

## 终结方法

类型化 `SELECT` 构建器提供 Dapper 风格的查询终结方法：

- `Query()`
- `QueryFirstOrDefault()`
- `QuerySingleOrDefault()`
- `QueryAsync()`
- `QueryFirstOrDefaultAsync()`
- `QuerySingleOrDefaultAsync()`

非泛型 `SELECT` 构建器也保留了 `Query<T>()` 和 `QueryAsync<T>()` 等显式结果类型重载，用于高级物化场景。

存在性查询构建器通过 `Execute()` 和 `ExecuteAsync()` 返回 `bool`。计数查询构建器通过 `Execute()` 和 `ExecuteAsync()` 返回计数。对于 `SelectAggregateFrom<T>()` 聚合查询，请使用终端 `Execute<TResult>()` 或 `ExecuteAsync<TResult>()` 指定标量读取类型。
