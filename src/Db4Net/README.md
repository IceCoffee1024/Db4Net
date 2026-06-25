# Db4Net

Db4Net is a lightweight fluent SQL builder for Dapper.

## Example

```csharp
var user = connection
    .UseDb4Net(Db4NetOptions.Sqlite)
    .SelectFrom<User>()
    .Where(u => u.Id, Op.Eq, 1)
    .Where(u => u.Name, Op.IsNotNull)
    .QuerySingleOrDefault<User>();
```

## Current Scope

- Typed and string-based `SELECT` builders.
- SQL Server and SQLite rendering.
- Parameterized `Where` clauses.
- Ordering and basic paging.
- `ToCommand()` for SQL inspection.
- Dapper terminal methods.
