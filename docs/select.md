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

Use `Select<T>(...)` when selecting specific mapped properties:

```csharp
var users = connection
    .UseDb4Net(Db4NetOptions.Sqlite)
    .Select<User>(u => u.Id, u => u.Name)
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

Use `SelectAggregateFrom<T>()` for column-level scalar aggregates. `Max(...)` and `Min(...)` operate on mapped value-type columns and return nullable values. `CountDistinct(...)` returns a `long`:

```csharp
var latestId = db
    .SelectAggregateFrom<User>()
    .Max(u => u.Id)
    .Where(u => u.Name, Op.Like, "A%")
    .Execute();

var distinctNames = await db
    .SelectAggregateFrom<User>("users_2026")
    .CountDistinct(u => u.Name)
    .ExecuteAsync();
```

Do not use `Select("COUNT(*)")`, `Select("MAX(...)")`, or similar strings for scalar queries. String select values are validated identifiers, not raw SQL expressions.

## Terminal Methods

Typed select builders materialize `T`:

- `Query()`
- `QueryFirstOrDefault()`
- `QuerySingleOrDefault()`
- `QueryAsync()`
- `QueryFirstOrDefaultAsync()`
- `QuerySingleOrDefaultAsync()`

The non-generic select builder also exposes explicit result-type overloads such as `Query<T>()` and `QueryAsync<T>()`.

Existence query builders return a `bool` through `Execute()` and `ExecuteAsync()`. Count query builders return the count through `Execute()` and `ExecuteAsync()`. Aggregate query builders return scalar results through `Execute()` and `ExecuteAsync()`.
