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

使用 `SelectExistsFrom<T>()` 执行存在性检查。它是受支持的存在性检查 API；如果只关心是否存在，优先使用它，而不是 `SelectCountFrom<T>().ExecuteScalar() > 0`：

```csharp
var exists = db
    .SelectExistsFrom<User>()
    .Where(u => u.Id, Op.Eq, id)
    .ExecuteScalar();

var existsInArchive = await db
    .SelectExistsFrom<User>("users_2026")
    .Where(u => u.Name, Op.Like, "A%")
    .ExecuteScalarAsync();
```

需要匹配行数时，使用 `SelectCountFrom<T>()`：

```csharp
var count = db
    .SelectCountFrom<User>()
    .Where(u => u.Id, Op.Gt, 0)
    .ExecuteScalar();

var matchingCount = await db
    .SelectCountFrom<User>("users_2026")
    .Where(u => u.Name, Op.Like, "A%")
    .ExecuteScalarAsync();
```

需要列级标量聚合时，使用 `SelectAggregateFrom<T>()`。`Max(...)`、`Min(...)`、`Sum(...)`、`Average(...)` 和 `CountDistinct(...)` 会构建标量聚合投影。显式结果类型放在终端 `ExecuteScalar<TResult>()` 或 `ExecuteScalarAsync<TResult>()` 调用上，例如 `Max(selector).ExecuteScalar<TResult>()` 或 `CountDistinct(selector).ExecuteScalarAsync<long>()`；需要保留 SQL 在空结果集上返回的 `NULL` 时，使用可空的 `TResult`。

`Max(...)` 和 `Min(...)` 要求值类型成员选择器。`Sum(...)` 和 `Average(...)` 不在 Db4Net 侧验证所选列是否为数值列；聚合由数据库执行，Dapper 按终端 `TResult` 读取结果，因此请选择与数据库返回值匹配的结果类型。

```csharp
var latestId = db
    .SelectAggregateFrom<User>()
    .Max(u => u.Id)
    .Where(u => u.Name, Op.Like, "A%")
    .ExecuteScalar<int?>();

var distinctNames = await db
    .SelectAggregateFrom<User>("users_2026")
    .CountDistinct(u => u.Name)
    .ExecuteScalarAsync<long>();

var totalAmount = db
    .SelectAggregateFrom<OrderMetric>()
    .Sum(o => o.Amount)
    .ExecuteScalar<decimal>();

var totalQuantity = db
    .SelectAggregateFrom<OrderMetric>()
    .Sum(o => o.Quantity)
    .ExecuteScalar<long>();

var averageQuantity = db
    .SelectAggregateFrom<OrderMetric>()
    .Average(o => o.Quantity)
    .ExecuteScalar<decimal>();
```

不要用 `Select("COUNT(*)")`、`Select("MAX(...)")`、`Select("SUM(...)")`、`Select("AVG(...)")` 或类似字符串表达标量查询。字符串选择值会被当作已验证的标识符，而不是原始 SQL 表达式。

## 分页

需要一页数据和同一组条件下的总数时，使用 `QueryPage(...)`：

```csharp
var page = await db
    .SelectFrom<User>()
    .Where(u => u.Name, Op.Like, "A%")
    .OrderBy(u => u.Id)
    .QueryPageAsync(pageNumber: 2, pageSize: 20);

var users = page.Items;
var totalCount = page.TotalCount;
var totalPages = page.TotalPages;
```

`QueryPage(...)` 是分页便捷终结方法。它内部会执行一次 count 查询和一次分页行查询，并对两条命令使用同一组执行选项。它自己负责分页，所以不要在 `QueryPage(...)` 前调用 `Limit(...)`、`Offset(...)` 或 `Page(...)`。

## 条件过滤

搜索条件可选时，可以用 `When(...)`、`WhereIf(...)`、`OrWhereIf(...)` 和 `WhereGroupIf(...)` 让动态查询保持在同一条 fluent chain 中：

```csharp
var page = await db
    .SelectFrom<User>()
    .When(!string.IsNullOrWhiteSpace(keyword), query =>
        query.Where(u => u.Name, Op.Like, keyword))
    .WhereGroupIf(hasNameRange, group => group
        .WhereIf(!string.IsNullOrWhiteSpace(namePrefix), u => u.Name, Op.Like, namePrefix)
        .OrWhereIf(!string.IsNullOrWhiteSpace(nameSuffix), u => u.Name, Op.Like, nameSuffix))
    .WhereIf(updatedAfter.HasValue, u => u.UpdatedAt, Op.Gte, updatedAfter)
    .OrderBy(u => u.Id)
    .QueryPageAsync(pageNumber, pageSize);
```

同一套条件过滤 API 也可用于只读标量 builder：

```csharp
var total = await db
    .SelectCountFrom<User>()
    .WhereIf(!string.IsNullOrWhiteSpace(keyword), u => u.Name, Op.Like, keyword)
    .WhereIf(updatedAfter.HasValue, u => u.UpdatedAt, Op.Gte, updatedAfter)
    .ExecuteScalarAsync();
```

条件为 false 时，builder 保持不变。`When(...)` 适合分组过滤或其他查询配置；`WhereIf(...)` 和 `OrWhereIf(...)` 是简单可选谓词的快捷写法。条件过滤适用于只读 SELECT builder：行查询、count 查询、exists 查询和聚合标量查询。`UPDATE` 和 `DELETE` builder 也提供范围专用的 `WhereBetweenIf(...)` / `OrWhereBetweenIf(...)`。

只需要分页行数据时，使用 `Page(...)` 进行从 1 开始的分页；需要直接控制行数时，也可以组合 `Limit(...)` 和 `Offset(...)`：

```csharp
var page = db
    .SelectFrom<User>()
    .OrderBy(u => u.Id)
    .Page(pageNumber: 2, pageSize: 20)
    .Query();
```

`Offset(...)` 必须与 `Limit(...)` 配套使用。SQL Server 分页还要求至少调用一次 `OrderBy(...)`，因为没有 `ORDER BY` 时 `OFFSET` / `FETCH` 是无效 SQL。

当排序方向来自请求 DTO 时，可以使用 `OrderBy(..., descending)`，避免在 `OrderBy(...)` 和 `OrderByDescending(...)` 之间写分支：

```csharp
var orderProperty = query.Order?.ToString() ?? nameof(User.UpdatedAt);

var page = await db
    .SelectFrom<User>()
    .OrderBy(orderProperty, descending: query.Desc)
    .OrderBy(u => u.Id, descending: query.Desc)
    .QueryPageAsync(pageNumber, pageSize);
```

## 终结方法

类型化 `SELECT` 构建器提供 Dapper 风格的查询终结方法：

- `Query()`
- `QueryFirst()`
- `QueryFirstOrDefault()`
- `QuerySingle()`
- `QuerySingleOrDefault()`
- `QueryAsync()`
- `QueryFirstAsync()`
- `QueryFirstOrDefaultAsync()`
- `QuerySingleAsync()`
- `QuerySingleOrDefaultAsync()`
- `QueryPage()`
- `QueryPageAsync()`

至少需要一行时使用 `QueryFirst*`。必须刚好一行时使用 `QuerySingle*`。`OrDefault` 变体在没有记录时返回默认值。

非泛型 `SELECT` 构建器也保留了 `Query<T>()`、`QueryFirst<T>()`、`QuerySingle<T>()`、`QueryAsync<T>()`、`QueryFirstAsync<T>()`、`QuerySingleAsync<T>()`、`QueryPage<T>()` 和 `QueryPageAsync<T>()` 等显式结果类型重载，用于高级物化场景。

存在性查询构建器通过 `ExecuteScalar()` 和 `ExecuteScalarAsync()` 返回 `bool`。计数查询构建器通过 `ExecuteScalar()` 和 `ExecuteScalarAsync()` 返回计数。对于 `SelectAggregateFrom<T>()` 聚合查询，请使用终端 `ExecuteScalar<TResult>()` 或 `ExecuteScalarAsync<TResult>()` 指定标量读取类型。
