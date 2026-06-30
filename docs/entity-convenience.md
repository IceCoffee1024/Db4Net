# Entity Convenience

Entity convenience methods read mapped values from a CLR object and build the same validated, parameterized commands as the SQL-shaped builders. They do not track changes.

```csharp
await connection
    .UseDb4Net(Db4NetOptions.Sqlite)
    .Insert(user)
    .ExecuteAsync();

var affected = connection
    .UseDb4Net(Db4NetOptions.SqlServer)
    .Update(user)
    .Execute();

await connection
    .UseDb4Net(Db4NetOptions.Sqlite)
    .Delete(user)
    .ExecuteAsync();
```

For single-row inserts, use the generated-key terminal when you need the inserted key:

```csharp
var id = await connection
    .UseDb4Net(Db4NetOptions.Sqlite)
    .Insert(user)
    .ExecuteReturnKeyAsync<long>();
```

## Table Overloads

Entity conveniences can target an explicit table while keeping mapping from `T`.

```csharp
var affected = db.Update(user, table: "users_2026")
    .Execute();
```

Available single-entity conveniences:

- `Insert(entity)`
- `Insert(entity, table)`
- `Update(entity)`
- `Update(entity, table)`
- `Delete(entity)`
- `Delete(entity, table)`

`Update(entity)` and `Delete(entity)` use key metadata for the `WHERE` clause. `Update(entity)`, `Delete(entity)`, `UpdateMany(...)`, and `DeleteMany(...)` require exactly one mapped key and a non-default key value; models without a key or with composite keys should use SQL-shaped builders with explicit `Where(...)` clauses.

`Insert(entity).ExecuteReturnKey<TResult>()` uses the model's only mapped key. If a model has multiple keys, pass an explicit key selector such as `ExecuteReturnKey<long>(u => u.Id)`.

Single-entity conveniences reject sequence values such as `List<User>` or `User[]`. Use `InsertMany(...)`, `UpdateMany(...)`, `DeleteMany(...)`, `InsertOrIgnoreMany(...)`, or `InsertOrUpdateMany(...)` for multiple objects.

::: tip
Here, entity means a mapped CLR object used as a value source. Db4Net does not add identity maps, dirty checking, lazy loading, or `SaveChanges()`.
:::
