# Update

Use `Update<T>()`, `Set(...)`, and an explicit filter.

```csharp
var affected = connection
    .UseDb4Net(Db4NetOptions.SqlServer)
    .Update<User>()
    .Set(u => u.Name, "Alice")
    .Where(u => u.Id, Op.Eq, 1)
    .Execute();
```

`Set(...)` accepts typed member selectors. Filters use the same `Where(...)`, `OrWhere(...)`, `WhereGroup(...)`, and `OrWhereGroup(...)` APIs as select queries.

## Entity Values and Keys

Use `Update(entity)` when you want mapped non-key values and key predicates read from an object.

```csharp
var affected = db.Update(user)
    .Execute();
```

Use `WhereKey(entity)` with an explicit builder when only the key predicate should come from the object.

```csharp
var affected = db.Update<User>()
    .Set(u => u.Name, "Alice")
    .WhereKey(user)
    .Execute();
```

## All Rows

`UPDATE` requires a `WHERE` clause by default.

```csharp
var affected = db.Update<User>()
    .Set(u => u.IsActive, false)
    .AllowAllRows()
    .Execute();
```

::: warning
Call `AllowAllRows()` only when intentionally updating every row.
:::
