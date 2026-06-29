# Ordering and Paging

Use `OrderBy(...)` and `OrderByDescending(...)` with typed member selectors or CLR property-name strings.

```csharp
var users = connection
    .UseDb4Net(Db4NetOptions.SqlServer)
    .SelectFrom<User>()
    .OrderBy(u => u.Name)
    .OrderByDescending(u => u.Id)
    .Query();
```

## Limit and Offset

`Offset(...)` must be paired with `Limit(...)`.

```csharp
var users = db.SelectFrom<User>()
    .OrderBy(u => u.Id)
    .Limit(20)
    .Offset(40)
    .Query();
```

## Page

`Page(pageNumber, pageSize)` uses one-based page numbers.

```csharp
var page = db.SelectFrom<User>()
    .OrderBy(u => u.Id)
    .Page(pageNumber: 2, pageSize: 20)
    .Query();
```

Db4Net renders paging through the configured dialect:

- SQL Server: `OFFSET ... ROWS FETCH NEXT ... ROWS ONLY`
- SQLite: `LIMIT ... OFFSET ...`
- PostgreSQL: `LIMIT ... OFFSET ...`
- MySQL: `LIMIT ... OFFSET ...`

SQL Server paging requires at least one `OrderBy(...)` because `OFFSET` / `FETCH` is invalid without `ORDER BY`. Stable ordering is recommended for every dialect when paging.
