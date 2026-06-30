# Select

Use `SelectFrom<T>()` when selecting all mapped columns for an entity model.

```csharp
var users = connection
    .UseDb4Net(Db4NetOptions.SqlServer)
    .SelectFrom<User>()
    .Where(u => u.Name, Op.Like, "A%")
    .OrderBy(u => u.Id)
    .Query();
```

Use `SelectFrom<T>(...)` when selecting specific mapped properties:

```csharp
var users = connection
    .UseDb4Net(Db4NetOptions.Sqlite)
    .SelectFrom<User>(u => u.Id, u => u.Name)
    .Where(u => u.Id, Op.In, new[] { 1, 2, 3 })
    .Query();
```

## Dynamic Property Names

When the result model is known but the selected fields are dynamic, pass CLR property names and then bind the model with `From<T>()`.

```csharp
var rows = connection
    .UseDb4Net(Db4NetOptions.SqlServer)
    .Select("Id", "Name")
    .From<User>()
    .Where("Name", Op.IsNotNull)
    .Query();
```

String fields are CLR property names, not database column names or SQL fragments. If `[Column("display_name")]` maps `DisplayName`, pass `"DisplayName"`.

## Existence, Count, and Aggregate Queries

Use `SelectExistsFrom<T>()` for existence checks. It is the supported existence-check API and is preferable to `SelectCountFrom<T>().Execute() > 0` when only existence matters:

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

Use `SelectCountFrom<T>()` when you need the number of matching rows:

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

Use `SelectAggregateFrom<T>()` for column-level scalar aggregates. `Max(...)`, `Min(...)`, `Sum(...)`, `Average(...)`, and `CountDistinct(...)` build scalar aggregate projections. Put explicit result typing on the terminal `Execute<TResult>()` or `ExecuteAsync<TResult>()` call, for example `Max(selector).Execute<TResult>()` or `CountDistinct(selector).ExecuteAsync<long>()`; use a nullable `TResult` when you need to preserve SQL `NULL` for empty result sets.

`Max(...)` and `Min(...)` require value-type member selectors. `Sum(...)` and `Average(...)` do not validate that the selected column is numeric; the database executes the aggregate and Dapper reads it as your terminal `TResult`, so choose a result type that matches your provider's aggregate result.

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

Do not use `Select("COUNT(*)")`, `Select("MAX(...)")`, `Select("SUM(...)")`, `Select("AVG(...)")`, or similar strings for scalar queries. String select values are validated identifiers, not raw SQL expressions.

## Paging

Use `QueryPage(...)` when you need one page of rows and the total count of rows matching the same table and filters:

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

`QueryPage(...)` is a convenience terminal. It executes a count query and a paged row query internally, using the same execution options for both commands. It owns paging, so do not call `Limit(...)`, `Offset(...)`, or `Page(...)` before `QueryPage(...)`.

Use `Page(...)` for one-based page pagination when you only need rows, or combine `Limit(...)` with `Offset(...)` when you need direct row counts:

```csharp
var page = db
    .SelectFrom<User>()
    .OrderBy(u => u.Id)
    .Page(pageNumber: 2, pageSize: 20)
    .Query();
```

`Offset(...)` must be paired with `Limit(...)`. SQL Server paging also requires at least one `OrderBy(...)` because `OFFSET` / `FETCH` is invalid without `ORDER BY`.

## Terminal Methods

Typed select builders materialize `T`:

- `Query()`
- `QueryFirstOrDefault()`
- `QuerySingleOrDefault()`
- `QueryAsync()`
- `QueryFirstOrDefaultAsync()`
- `QuerySingleOrDefaultAsync()`
- `QueryPage()`
- `QueryPageAsync()`

The non-generic select builder also exposes explicit result-type overloads such as `Query<T>()`, `QueryAsync<T>()`, `QueryPage<T>()`, and `QueryPageAsync<T>()`.

Existence query builders return a `bool` through `Execute()` and `ExecuteAsync()`. Count query builders return the count through `Execute()` and `ExecuteAsync()`. For `SelectAggregateFrom<T>()` aggregate queries, specify the scalar read type with terminal `Execute<TResult>()` or `ExecuteAsync<TResult>()`.
