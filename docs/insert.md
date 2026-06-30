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

## Return Generated Key

Use `ExecuteReturnKey<TResult>()` when a regular single-row insert should return the inserted key.

```csharp
var id = await db.Insert(user)
    .ExecuteReturnKeyAsync<long>();

var stagedId = db.Insert(user, table: "users_staging")
    .ExecuteReturnKey<long>(u => u.Id);
```

Use the SQL-shaped builder form when you want to inspect the returned-key command:

```csharp
var command = db.InsertInto<User>()
    .Values(user)
    .ReturnKey(u => u.Id)
    .ToCommand();

var id = db.InsertInto<User>()
    .Values(user)
    .ReturnKey(u => u.Id)
    .Execute<long>();
```

`ExecuteReturnKey<TResult>()` uses the model's only mapped key. If a model has multiple `[Key]` properties, pass the key selector explicitly. The selector must target a mapped key column.

Generated-key readback applies to regular single-row `InsertInto<T>()` and `Insert(entity)` commands only. `InsertMany(...)`, `InsertOrIgnore(...)`, and `InsertOrUpdate(...)` continue to return affected row counts. See [Generated Keys](./dialects.md#generated-keys) for provider caveats such as MySQL auto-increment identity semantics, SQLite `RETURNING` version support, and SQL Server trigger limitations.

## Execute or Inspect

Insert builders support:

- `Execute()`
- `ExecuteAsync()`
- `ExecuteReturnKey<TResult>()`
- `ExecuteReturnKeyAsync<TResult>()`
- `ReturnKey(...).Execute<TResult>()`
- `ReturnKey(...).ExecuteAsync<TResult>()`
- `ToCommand()`

::: tip
For common entity inserts, `Insert(user)` is shorter and builds the same kind of validated, parameterized command.
:::
