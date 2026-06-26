# Db4Net

Db4Net is a lightweight fluent SQL builder for Dapper. It focuses on safe, parameterized single-table queries and commands while leaving execution and SELECT result materialization to Dapper.

The API is intentionally SQL-shaped: `SelectFrom<T>()`, `InsertInto<T>()`, `Update<T>()`, and `DeleteFrom<T>()` keep statement order recognizable while still validating identifiers and parameterizing values.

## Install

```bash
dotnet add package Db4Net --prerelease
```

## Quick Start

```csharp
using Db4Net;

var user = await connection
    .UseDb4Net(Db4NetOptions.Sqlite)
    .SelectFrom<User>()
    .Where(u => u.Id, Op.Eq, 1)
    .QuerySingleOrDefaultAsync();
```

## Recommended APIs

Use `SelectFrom<T>()` when querying a whole mapped entity:

```csharp
var users = connection
    .UseDb4Net(Db4NetOptions.SqlServer)
    .SelectFrom<User>()
    .Where(u => u.Name, Op.Like, "A%")
    .OrderBy(u => u.Id)
    .Query();
```

Use `Select<T>(...)` when querying specific mapped properties:

```csharp
var users = connection
    .UseDb4Net(Db4NetOptions.Sqlite)
    .Select<User>(u => u.Id, u => u.Name)
    .Where(u => u.Id, Op.In, new[] { 1, 2, 3 })
    .Query();
```

Use dynamic CLR property names when the column set is dynamic but the result model is known, such as user-selected grid columns, dynamic forms, export templates, or field-level permissions:

```csharp
var rows = connection
    .UseDb4Net(Db4NetOptions.SqlServer)
    .Select("Id", "Name")
    .From<User>()
    .Where("Name", Op.IsNotNull)
    .Query();
```

String fields are CLR property names, not database column names or SQL fragments. They are validated against the mapped CLR model and converted to database column names. For example, use `"DisplayName"` rather than `"display_name"` when `[Column("display_name")]` is applied. Table or view names can be overridden with `SelectFrom<T>("view_name")` or `From<T>("view_name")`; those identifiers are validated and quoted by the configured dialect. Values are always passed as Dapper parameters.

Use `InsertInto<T>()` when inserting explicit mapped properties:

```csharp
var affected = await connection
    .UseDb4Net(Db4NetOptions.Sqlite)
    .InsertInto<User>()
    .Value(u => u.Id, 3)
    .Value(u => u.Name, "Charlie")
    .ExecuteAsync();
```

Use `Update<T>()` with `Set(...)` and an explicit filter:

```csharp
var affected = connection
    .UseDb4Net(Db4NetOptions.SqlServer)
    .Update<User>()
    .Set(u => u.Name, "Alice")
    .Where(u => u.Id, Op.Eq, 1)
    .Execute();
```

Use `DeleteFrom<T>()` with an explicit filter:

```csharp
var affected = await connection
    .UseDb4Net(Db4NetOptions.Sqlite)
    .DeleteFrom<User>()
    .Where(u => u.Id, Op.Eq, 1)
    .ExecuteAsync();
```

`UPDATE` and `DELETE` require a `WHERE` clause by default. Call `AllowAllRows()` only when intentionally affecting every row.

Use command table overloads when the same CLR model maps to tenant, time-partitioned, staging, or archive tables:

```csharp
var affected = connection
    .UseDb4Net(Db4NetOptions.SqlServer)
    .Update<User>("users_tenant_001")
    .Set(u => u.Name, "Alice")
    .Where(u => u.Id, Op.Eq, 1)
    .Execute();
```

`InsertInto<T>("users_staging")`, `Update<T>("users_2026")`, and `DeleteFrom<T>("users_2026")` only override the SQL target table. Property-to-column mapping still comes from `T`, and the table identifier is validated and quoted by the configured dialect.

## Mapping

Db4Net supports standard mapping attributes:

```csharp
using System.ComponentModel.DataAnnotations.Schema;

[Table("app_users")]
public sealed class User
{
    public int Id { get; set; }

    [Column("display_name")]
    public string Name { get; set; } = "";

    [NotMapped]
    public string DisplayOnly { get; set; } = "";
}
```

Typed projections alias mapped columns so Dapper can map results back to property names:

```sql
SELECT [Id], [display_name] AS [Name] FROM [app_users]
```

`[NotMapped]` members are excluded from `SelectFrom<T>()` and rejected in typed `Select`, `Where`, `OrderBy`, `Value`, and `Set` member selectors.

## Filters

```csharp
.Where(u => u.Id, Op.Eq, 1)
.Where(u => u.Name, Op.Like, "A%")
.Where(u => u.Id, Op.In, new[] { 1, 2, 3 })
.Where(u => u.DeletedAt, Op.IsNull)
.Where(u => u.Name, Op.IsNotNull)
```

`Op.Eq` with `null` renders `IS NULL`, and `Op.NotEq` with `null` renders `IS NOT NULL`. Prefer `Op.IsNull` and `Op.IsNotNull` when no value is needed.

## Paging

```csharp
var page = connection
    .UseDb4Net(Db4NetOptions.SqlServer)
    .SelectFrom<User>()
    .OrderBy(u => u.Id)
    .Page(pageNumber: 2, pageSize: 20)
    .Query();
```

Db4Net renders paging through the configured dialect:

- SQL Server: `OFFSET ... ROWS FETCH NEXT ... ROWS ONLY`
- SQLite: `LIMIT ... OFFSET ...`
- PostgreSQL: `LIMIT ... OFFSET ...`
- MySQL: `LIMIT ... OFFSET ...`

## Inspect SQL

```csharp
var command = Db4NetDatabase
    .Create(Db4NetOptions.SqlServer)
    .Select<User>(u => u.Id, u => u.Name)
    .Where(u => u.Id, Op.Eq, 1)
    .ToCommand();

Console.WriteLine(command.Sql);
```

Output:

```sql
SELECT [Id], [Name] FROM [Users] WHERE [Id] = @p0
```

Command builders also support `ToCommand()`:

```csharp
var command = Db4NetDatabase
    .Create(Db4NetOptions.SqlServer)
    .Update<User>()
    .Set(u => u.Name, "Alice")
    .Where(u => u.Id, Op.Eq, 1)
    .ToCommand();
```

Output:

```sql
UPDATE [Users] SET [Name] = @p0 WHERE [Id] = @p1
```

## Terminal Methods

Typed SELECT builders provide Dapper-style query terminal methods that materialize the bound CLR model:

- `Query()`
- `QueryFirstOrDefault()`
- `QuerySingleOrDefault()`
- `QueryAsync()`
- `QueryFirstOrDefaultAsync()`
- `QuerySingleOrDefaultAsync()`

The non-generic SELECT builder also keeps explicit result-type overloads such as `Query<T>()` and `QueryAsync<T>()` for advanced materialization scenarios.

INSERT, UPDATE, and DELETE builders provide command terminal methods:

- `Execute()`
- `ExecuteAsync()`

## Execution Options

Pass `Db4NetExecutionOptions` when you need a transaction, command timeout, or command type:

```csharp
using var transaction = connection.BeginTransaction();

var users = connection
    .UseDb4Net(Db4NetOptions.SqlServer)
    .SelectFrom<User>()
    .Where(u => u.Id, Op.Gt, 0)
    .Query(new Db4NetExecutionOptions
    {
        Transaction = transaction,
        CommandTimeout = 30
    });
```

Async terminal methods also accept a `CancellationToken`:

```csharp
var users = await connection
    .UseDb4Net(Db4NetOptions.Sqlite)
    .SelectFrom<User>()
    .QueryAsync(
        new Db4NetExecutionOptions { CommandTimeout = 30 },
        cancellationToken);
```

## Tests

SQLite integration tests run by default with an in-memory database. PostgreSQL, MySQL, and SQL Server integration tests are opt-in; see `tests/Db4Net.Tests/README.md` for local `dotnet test --settings tests\Db4Net.Tests\local.runsettings` usage.

## Scope

Current scope is focused on typed single-table `SELECT`, `INSERT`, `UPDATE`, and `DELETE` builders for SQL Server, SQLite, PostgreSQL, and MySQL. Table and view overrides are supported for safe SQL-shaped APIs, but joins, bulk operations, relationship loading, and full predicate expression translation are intentionally out of scope for this early version.
