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

## Count Queries

Use `SelectCountFrom<T>()` for count queries:

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

Do not use `Select("COUNT(*)")` for count queries. String select values are validated identifiers, not raw SQL expressions.

## Terminal Methods

Typed select builders materialize `T`:

- `Query()`
- `QueryFirstOrDefault()`
- `QuerySingleOrDefault()`
- `QueryAsync()`
- `QueryFirstOrDefaultAsync()`
- `QuerySingleOrDefaultAsync()`

The non-generic select builder also exposes explicit result-type overloads such as `Query<T>()` and `QueryAsync<T>()`.

Count query builders return the count through `Execute()` and `ExecuteAsync()`.
