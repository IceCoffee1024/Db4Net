# Insert

Use `InsertInto<T>()` with `Value(...)` when inserting explicit mapped properties.

```csharp
var affected = await connection
    .UseDb4Net(Db4NetOptions.Sqlite)
    .InsertInto<User>()
    .Value(u => u.Id, 3)
    .Value(u => u.Name, "Charlie")
    .ExecuteAsync();
```

`Value(...)` accepts typed member selectors. Values are sent as Dapper parameters.

## Insert Entity Values

Use `Values(entity)` when you still want an `InsertInto<T>()` builder but want mapped values read from an object.

```csharp
var command = db.InsertInto<User>()
    .Values(user)
    .ToCommand();
```

Database-generated mapped members are omitted by `Values(entity)`.

## Execute or Inspect

Insert builders support:

- `Execute()`
- `ExecuteAsync()`
- `ToCommand()`

::: tip
For common entity inserts, `Insert(user)` is shorter and builds the same kind of validated, parameterized command.
:::
