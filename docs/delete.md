# Delete

Use `DeleteFrom<T>()` with an explicit filter.

```csharp
var affected = await connection
    .UseDb4Net(Db4NetOptions.Sqlite)
    .DeleteFrom<User>()
    .Where(u => u.Id, Op.Eq, 1)
    .ExecuteAsync();
```

Filters use the same `Where(...)`, `OrWhere(...)`, `WhereGroup(...)`, and `OrWhereGroup(...)` APIs as select queries.

## Delete by Key

Use `WhereKey(entity)` when key metadata should become the delete predicate.

```csharp
var affected = db.DeleteFrom<User>()
    .WhereKey(user)
    .Execute();
```

## All Rows

`DELETE` requires a `WHERE` clause by default.

```csharp
var affected = db.DeleteFrom<User>()
    .AllowAllRows()
    .Execute();
```

::: warning
Call `AllowAllRows()` only when intentionally deleting every row.
:::
