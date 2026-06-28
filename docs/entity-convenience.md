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

`Update(entity)` and `Delete(entity)` use key metadata for the `WHERE` clause.

::: tip
Here, entity means a mapped CLR object used as a value source. Db4Net does not add identity maps, dirty checking, lazy loading, or `SaveChanges()`.
:::
