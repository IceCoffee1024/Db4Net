# Db4Net

Db4Net is a lightweight fluent SQL builder for safe, parameterized Dapper queries and commands.

It is designed for developers who want more structure than hand-written SQL string concatenation, while still keeping Dapper in charge of execution and object mapping. Db4Net is not an ORM and does not try to become a LINQ provider.

The API is intentionally SQL-shaped: `SelectFrom<T>()`, `InsertInto<T>()`, `Update<T>()`, and `DeleteFrom<T>()` keep statement order recognizable while still validating identifiers and parameterizing values.

## Status

Current version: `0.1.0-alpha.1`

The first alpha focuses on typed single-table `SELECT`, `INSERT`, `UPDATE`, and `DELETE` builders, mapped property selection, safe value parameters, Dapper-style terminal methods, and dialect-aware rendering for SQL Server, SQLite, PostgreSQL, and MySQL.

NuGet packages include XML documentation and a symbols package for source debugging.

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

The call above renders a parameterized command and executes it through Dapper. Use `Db4NetDatabase.Create(...)` instead when you only want to inspect generated SQL.

Select specific mapped properties:

```csharp
var users = connection
    .UseDb4Net(Db4NetOptions.SqlServer)
    .Select<User>(u => u.Id, u => u.Name)
    .Where(u => u.Id, Op.In, new[] { 1, 2, 3 })
    .OrderBy(u => u.Id)
    .Query();
```

Use dynamic CLR property names when the result model is known, such as user-selected grid columns, dynamic forms, export templates, or field-level permissions:

```csharp
var rows = connection
    .UseDb4Net(Db4NetOptions.SqlServer)
    .Select("Id", "Name")
    .From<User>()
    .Where("Name", Op.IsNotNull)
    .Query();
```

String fields are CLR property names, not database column names or SQL fragments. They are validated against the mapped CLR model and converted to quoted database column names. For example, use `"DisplayName"` rather than `"display_name"` when `[Column("display_name")]` is applied. Values are always passed as Dapper parameters.

Insert, update, and delete rows with command builders:

```csharp
var inserted = await connection
    .UseDb4Net(Db4NetOptions.Sqlite)
    .InsertInto<User>()
    .Value(u => u.Id, 3)
    .Value(u => u.Name, "Charlie")
    .ExecuteAsync();

var updated = connection
    .UseDb4Net(Db4NetOptions.SqlServer)
    .Update<User>()
    .Set(u => u.Name, "Alice")
    .Where(u => u.Id, Op.Eq, 1)
    .Execute();

var deleted = await connection
    .UseDb4Net(Db4NetOptions.Sqlite)
    .DeleteFrom<User>()
    .Where(u => u.Id, Op.Eq, 1)
    .ExecuteAsync();
```

`UPDATE` and `DELETE` require a `WHERE` clause by default. Call `AllowAllRows()` only when intentionally affecting every row.

Override command target tables when the CLR model mapping is reused for tenant, time-partitioned, staging, or archive tables:

```csharp
var affected = connection
    .UseDb4Net(Db4NetOptions.SqlServer)
    .Update<User>("users_tenant_001")
    .Set(u => u.Name, "Alice")
    .Where(u => u.Id, Op.Eq, 1)
    .Execute();
```

The table overload changes only the SQL target table. Property-to-column mapping still comes from `User`, and the table identifier is validated and quoted by the configured dialect.

Entity-based command conveniences are available for common single-row commands. Here, entity means a mapped CLR object used as a value source, not a tracked ORM entity. These methods read mapped values from the object and build the same validated, parameterized SQL command produced by the SQL-shaped builders:

```csharp
await connection
    .UseDb4Net(Db4NetOptions.Sqlite)
    .Insert(user)
    .ExecuteAsync();

var affected = connection
    .UseDb4Net(Db4NetOptions.SqlServer)
    .Update(user, table: "users_2026")
    .Execute();

await connection
    .UseDb4Net(Db4NetOptions.Sqlite)
    .Delete(user)
    .ExecuteAsync();
```

Use the SQL-shaped command builders when you need explicit fields or predicates:

```csharp
db.Update<User>()
  .Set(u => u.Name, "Alice")
  .Where(u => u.Id, Op.Eq, user.Id)
  .Execute();
```

They still support `ToCommand()` and do not add change tracking, relationship loading, identity maps, migrations, or `SaveChanges()` behavior.

Inspect SQL without executing it:

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

## Mapping

Db4Net supports standard mapping attributes:

```csharp
using System.ComponentModel.DataAnnotations.Schema;

[Table("app_users")]
public sealed class User
{
    [Key]
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

`[Key]` and the `Id` / `<TypeName>Id` convention are used by entity command conveniences such as `Update(user)`, `Delete(user)`, and `WhereKey(user)`. Key metadata identifies mapped columns for equality predicates; it does not imply entity tracking, generated value readback, relationship identity maps, or automatic concurrency behavior. `[DatabaseGenerated(DatabaseGeneratedOption.Identity)]` and `[DatabaseGenerated(DatabaseGeneratedOption.Computed)]` mapped properties are omitted by `Values(entity)` and `Insert(entity)`. Explicit `.Value(...)` calls remain caller-controlled.

## Supported Dialects

- SQL Server
- SQLite
- PostgreSQL
- MySQL

Db4Net handles identifier quoting and paging syntax through the configured dialect.

## Scope

Included in the current alpha:

- Typed `SELECT` builders
- Typed `INSERT`, `UPDATE`, and `DELETE` builders
- SQL-shaped command target overrides such as `InsertInto<T>("users_staging")`, `Update<T>("users_2026")`, and `DeleteFrom<T>("users_2026")`
- Entity command conveniences such as `Values(entity)`, `WhereKey(entity)`, `Insert(entity)`, `Insert(entity, table)`, `Update(entity)`, `Update(entity, table)`, `Delete(entity)`, and `Delete(entity, table)`
- Dynamic property-name projection with model validation
- `Where`, `OrWhere`, `OrderBy`, `Limit`, `Offset`, and `Page`
- `Value`, `Set`, `Execute`, and `ExecuteAsync` for command builders
- Sync and async Dapper-style query terminal methods
- Transaction, command timeout, command type, and async cancellation token support

Intentionally out of scope for now:

- Joins
- Bulk operations
- Change tracking, dirty checking, `SaveChanges()`, or unit-of-work behavior
- Relationship loading, cascade persistence, lazy loading, or proxy generation
- Migrations or schema management
- Automatic concurrency tokens
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

Build and pack locally:

```bash
dotnet build -c Release
dotnet pack src/Db4Net/Db4Net.csproj -c Release --no-build
```

## License

Db4Net is licensed under the MIT license. See `LICENSE`.
