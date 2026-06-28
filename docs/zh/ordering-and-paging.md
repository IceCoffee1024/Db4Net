# 排序与分页

使用 `OrderBy(...)` 指定排序列，再使用 `Limit(...)`、`Offset(...)` 或 `Page(...)` 控制返回范围。

```csharp
var page = connection
    .UseDb4Net(Db4NetOptions.SqlServer)
    .SelectFrom<User>()
    .OrderBy(u => u.Id)
    .Page(pageNumber: 2, pageSize: 20)
    .Query();
```

Db4Net 会按配置的 SQL 方言渲染分页语法：

- SQL Server: `OFFSET ... ROWS FETCH NEXT ... ROWS ONLY`
- SQLite: `LIMIT ... OFFSET ...`
- PostgreSQL: `LIMIT ... OFFSET ...`
- MySQL: `LIMIT ... OFFSET ...`

::: warning 注意
分页结果通常应配合稳定排序。没有明确排序时，不同数据库或执行计划可能返回不稳定的页面。
:::
