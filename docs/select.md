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

## Terminal Methods

Typed select builders materialize `T`:

- `Query()`
- `QueryFirstOrDefault()`
- `QuerySingleOrDefault()`
- `QueryAsync()`
- `QueryFirstOrDefaultAsync()`
- `QuerySingleOrDefaultAsync()`

The non-generic select builder also exposes explicit result-type overloads such as `Query<T>()` and `QueryAsync<T>()`.
