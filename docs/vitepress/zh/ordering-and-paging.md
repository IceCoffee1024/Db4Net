# 排序与分页

使用 `OrderBy(...)` 指定排序列，再使用 `Limit(...)`、`Offset(...)` 或 `Page(...)` 控制返回范围。`Offset(...)` 必须与 `Limit(...)` 配套使用。

```csharp
var page = connection
    .UseDb4Net(Db4NetOptions.SqlServer)
    .SelectFrom<User>()
    .OrderBy(u => u.Id)
    .Page(pageNumber: 2, pageSize: 20)
    .Query();
```

需要分页行数据和同一组筛选条件下的总数时，使用 `QueryPage(pageNumber, pageSize)`：

```csharp
var page = await db.SelectFrom<User>()
    .Where(u => u.Name, Op.Like, "A%")
    .OrderBy(u => u.Id)
    .QueryPageAsync(pageNumber: 2, pageSize: 20);

var users = page.Items;
var totalCount = page.TotalCount;
var totalPages = page.TotalPages;
```

`QueryPage(...)` 内部会执行一次 count 查询和一次分页行查询。它会自己应用分页；不要在 `QueryPage(...)` 前调用 `Limit(...)`、`Offset(...)` 或 `Page(...)`。

Db4Net 会按配置的 SQL 方言渲染分页语法：

- SQL Server: `OFFSET ... ROWS FETCH NEXT ... ROWS ONLY`
- SQLite: `LIMIT ... OFFSET ...`
- PostgreSQL: `LIMIT ... OFFSET ...`
- MySQL: `LIMIT ... OFFSET ...`

::: warning 注意
SQL Server 分页必须至少调用一次 `OrderBy(...)`，因为没有 `ORDER BY` 时 `OFFSET` / `FETCH` 是无效 SQL。其他方言分页也建议配合稳定排序；没有明确排序时，不同数据库或执行计划可能返回不稳定的页面。
:::
