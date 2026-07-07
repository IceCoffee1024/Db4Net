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

Use `SelectExistsFrom<T>()` for existence checks. It is the supported existence-check API and is preferable to `SelectCountFrom<T>().ExecuteScalar() > 0` when only existence matters:

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

Use `SelectCountFrom<T>()` when you need the number of matching rows:

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

Use `SelectAggregateFrom<T>()` for column-level scalar aggregates. `Max(...)`, `Min(...)`, `Sum(...)`, `Average(...)`, and `CountDistinct(...)` build scalar aggregate projections. Put explicit result typing on the terminal `ExecuteScalar<TResult>()` or `ExecuteScalarAsync<TResult>()` call, for example `Max(selector).ExecuteScalar<TResult>()` or `CountDistinct(selector).ExecuteScalarAsync<long>()`; use a nullable `TResult` when you need to preserve SQL `NULL` for empty result sets.

`Max(...)` and `Min(...)` require value-type member selectors. `Sum(...)` and `Average(...)` do not validate that the selected column is numeric; the database executes the aggregate and Dapper reads it as your terminal `TResult`, so choose a result type that matches your provider's aggregate result.

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

## Conditional Filters

For optional search criteria, use `When(...)`, `WhereIf(...)`, `OrWhereIf(...)`, and `WhereGroupIf(...)` to keep dynamic queries in one fluent chain:

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

The same conditional filter API is available on read-only scalar builders:

```csharp
var total = await db
    .SelectCountFrom<User>()
    .WhereIf(!string.IsNullOrWhiteSpace(keyword), u => u.Name, Op.Like, keyword)
    .WhereIf(updatedAfter.HasValue, u => u.UpdatedAt, Op.Gte, updatedAfter)
    .ExecuteScalarAsync();
```

False conditions leave the builder unchanged. `When(...)` is the general escape hatch for grouped filters or other query configuration; `WhereIf(...)` and `OrWhereIf(...)` are shorthand for simple optional predicates. Conditional filters are available on read-only SELECT builders: row queries, count queries, exists queries, and aggregate scalar queries. Range-specific `WhereBetweenIf(...)` and `OrWhereBetweenIf(...)` are also available on `UPDATE` and `DELETE` builders.

Use `Page(...)` for one-based page pagination when you only need rows, or combine `Limit(...)` with `Offset(...)` when you need direct row counts:

```csharp
var page = db
    .SelectFrom<User>()
    .OrderBy(u => u.Id)
    .Page(pageNumber: 2, pageSize: 20)
    .Query();
```

`Offset(...)` must be paired with `Limit(...)`. SQL Server paging also requires at least one `OrderBy(...)` because `OFFSET` / `FETCH` is invalid without `ORDER BY`.

When the sort direction comes from a request DTO, use `OrderBy(..., descending)` instead of branching between `OrderBy(...)` and `OrderByDescending(...)`:

```csharp
var orderProperty = query.Order?.ToString() ?? nameof(User.UpdatedAt);

var page = await db
    .SelectFrom<User>()
    .OrderBy(orderProperty, descending: query.Desc)
    .OrderBy(u => u.Id, descending: query.Desc)
    .QueryPageAsync(pageNumber, pageSize);
```

## Terminal Methods

Typed select builders materialize `T`:

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

Use `QueryFirst*` when at least one row must exist. Use `QuerySingle*` when exactly one row must exist. The `OrDefault` variants return the default value when no row exists.

The non-generic select builder also exposes explicit result-type overloads such as `Query<T>()`, `QueryFirst<T>()`, `QuerySingle<T>()`, `QueryAsync<T>()`, `QueryFirstAsync<T>()`, `QuerySingleAsync<T>()`, `QueryPage<T>()`, and `QueryPageAsync<T>()`.

Existence query builders return a `bool` through `ExecuteScalar()` and `ExecuteScalarAsync()`. Count query builders return the count through `ExecuteScalar()` and `ExecuteScalarAsync()`. For `SelectAggregateFrom<T>()` aggregate queries, specify the scalar read type with terminal `ExecuteScalar<TResult>()` or `ExecuteScalarAsync<TResult>()`.
