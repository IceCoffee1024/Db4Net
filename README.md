# Db4Net

Db4Net is a lightweight fluent SQL builder for safe, parameterized Dapper `SELECT` queries.

It is designed for developers who want more structure than hand-written SQL string concatenation, while still keeping Dapper in charge of execution and object mapping. Db4Net is not an ORM and does not try to become a LINQ provider.

## Status

Current version: `0.1.0-alpha.1`

The first alpha focuses on typed `SELECT` builders, mapped property selection, safe value parameters, Dapper-style terminal methods, and dialect-aware rendering for SQL Server, SQLite, PostgreSQL, and MySQL.

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

Select specific mapped properties:

```csharp
var users = connection
    .UseDb4Net(Db4NetOptions.SqlServer)
    .Select<User>(u => u.Id, u => u.Name)
    .Where(u => u.Id, Op.In, new[] { 1, 2, 3 })
    .OrderBy(u => u.Id)
    .Query<User>();
```

Use dynamic property names when the result model is known:

```csharp
var rows = connection
    .UseDb4Net(Db4NetOptions.SqlServer)
    .Select("Id", "Name")
    .From<User>()
    .Where("Name", Op.IsNotNull)
    .Query<User>();
```

String property names are validated against the mapped CLR model and converted to quoted database column names. Values are always passed as Dapper parameters.

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

## Supported Dialects

- SQL Server
- SQLite
- PostgreSQL
- MySQL

Db4Net handles identifier quoting and paging syntax through the configured dialect.

## Scope

Included in the current alpha:

- Typed `SELECT` builders
- Dynamic property-name projection with model validation
- `Where`, `OrWhere`, `OrderBy`, `Limit`, `Offset`, and `Page`
- Sync and async Dapper-style terminal methods
- Transaction, command timeout, command type, and async cancellation token support

Intentionally out of scope for now:

- Joins
- Inserts, updates, and deletes
- Full predicate expression translation such as `Where(u => u.Id == 1)`
- Full LINQ provider behavior

For complex joins or database-specific SQL, use Dapper raw SQL directly or expose stable read models through database views.

## Repository Layout

- `src/Db4Net`: library source and NuGet package README
- `tests/Db4Net.Tests`: unit and integration tests
- `docs`: design notes and project analysis

## Tests

Run the default test suite:

```bash
dotnet test
```

SQLite integration tests run by default with an in-memory database. PostgreSQL, MySQL, and SQL Server integration tests are opt-in through environment variables or a local runsettings file. See `tests/Db4Net.Tests/README.md`.

## License

Db4Net is licensed under the MIT license. See `LICENSE`.
