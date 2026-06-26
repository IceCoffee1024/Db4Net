# Db4Net

Db4Net is a lightweight fluent SQL builder for Dapper. It focuses on safe, parameterized `SELECT` queries while leaving execution and object mapping to Dapper.

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
    .QuerySingleOrDefaultAsync<User>();
```

## Recommended APIs

Use `SelectFrom<T>()` when querying a whole mapped entity:

```csharp
var users = connection
    .UseDb4Net(Db4NetOptions.SqlServer)
    .SelectFrom<User>()
    .Where(u => u.Name, Op.Like, "A%")
    .OrderBy(u => u.Id)
    .Query<User>();
```

Use `Select<T>(...)` when querying specific mapped properties:

```csharp
var users = connection
    .UseDb4Net(Db4NetOptions.Sqlite)
    .Select<User>(u => u.Id, u => u.Name)
    .Where(u => u.Id, Op.In, new[] { 1, 2, 3 })
    .Query<User>();
```

Use string property names when the column set is dynamic but the result model is known:

```csharp
var rows = connection
    .UseDb4Net(Db4NetOptions.SqlServer)
    .Select("Id", "Name")
    .From<User>()
    .Where("Name", Op.IsNotNull)
    .Query<User>();
```

String property names are validated against the mapped CLR model and converted to database column names. Table or view names can be overridden with `SelectFrom<T>("view_name")` or `From<T>("view_name")`; those identifiers are validated and quoted by the configured dialect. Values are always passed as Dapper parameters.

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

`[NotMapped]` members are excluded from `SelectFrom<T>()` and rejected in typed `Select`, `Where`, and `OrderBy` member selectors.

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
    .Query<User>();
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

## Terminal Methods

Db4Net provides Dapper-style terminal methods:

- `Query<T>()`
- `QueryFirstOrDefault<T>()`
- `QuerySingleOrDefault<T>()`
- `Execute()`
- `QueryAsync<T>()`
- `QueryFirstOrDefaultAsync<T>()`
- `QuerySingleOrDefaultAsync<T>()`
- `ExecuteAsync()`

## Execution Options

Pass `Db4NetCommandOptions` when you need a transaction, command timeout, or command type:

```csharp
using var transaction = connection.BeginTransaction();

var users = connection
    .UseDb4Net(Db4NetOptions.SqlServer)
    .SelectFrom<User>()
    .Where(u => u.Id, Op.Gt, 0)
    .Query<User>(new Db4NetCommandOptions
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
    .QueryAsync<User>(
        new Db4NetCommandOptions { CommandTimeout = 30 },
        cancellationToken);
```

## Tests

SQLite integration tests run by default with an in-memory database. PostgreSQL, MySQL, and SQL Server integration tests are opt-in and skipped unless these environment variables are set:

- `DB4NET_POSTGRESQL_CONNECTION_STRING`
- `DB4NET_MYSQL_CONNECTION_STRING`
- `DB4NET_SQLSERVER_CONNECTION_STRING`

## Scope

Current scope is focused on typed `SELECT` builders and dynamic property-name projection for SQL Server, SQLite, PostgreSQL, and MySQL. Joins, inserts, updates, deletes, and full predicate expression translation are intentionally out of scope for this early version.
