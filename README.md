# Db4Net

Db4Net is a lightweight fluent SQL builder for safe, parameterized Dapper queries and commands.

It is designed for developers who want more structure than hand-written SQL string concatenation, while still keeping Dapper in charge of execution and object mapping. Db4Net is not an ORM and does not try to become a LINQ provider.

The API is intentionally SQL-shaped: `SelectFrom<T>()`, `SelectExistsFrom<T>()`, `SelectCountFrom<T>()`, `SelectAggregateFrom<T>()`, `InsertInto<T>()`, `Update<T>()`, and `DeleteFrom<T>()` keep statement order recognizable while still validating identifiers and parameterizing values.

## Status

Current version: `0.1.0-alpha.1`

This alpha focuses on safe, SQL-shaped query and command builders for Dapper, including typed `SELECT`, scalar aggregate queries, single-column `IN` subquery filters, `INSERT`, single-row insert key return, `UPDATE`, `DELETE`, entity conveniences, many-entity conveniences, conflict-aware inserts, explicit filter grouping, and dialect-aware rendering for SQL Server, SQLite, PostgreSQL, and MySQL.

NuGet packages include XML documentation and a symbols package for source debugging.

Package assets target `net8.0` and `netstandard2.0`.

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
    .SelectFrom<User>(u => u.Id, u => u.Name)
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

Use `SelectExistsFrom<T>()` for existence checks. It is the supported existence-check API and is preferable to `SelectCountFrom<T>().Execute() > 0` when only existence matters:

```csharp
var exists = connection
    .UseDb4Net(Db4NetOptions.SqlServer)
    .SelectExistsFrom<User>()
    .Where(u => u.Id, Op.Eq, id)
    .Execute();

var existsInArchive = await connection
    .UseDb4Net(Db4NetOptions.SqlServer)
    .SelectExistsFrom<User>("users_2026")
    .Where(u => u.Name, Op.Like, "A%")
    .ExecuteAsync();
```

Use `SelectCountFrom<T>()` when you need the number of matching rows:

```csharp
var count = connection
    .UseDb4Net(Db4NetOptions.SqlServer)
    .SelectCountFrom<User>()
    .Where(u => u.Id, Op.Gt, 0)
    .Execute();

var matchingCount = await connection
    .UseDb4Net(Db4NetOptions.SqlServer)
    .SelectCountFrom<User>("users_2026")
    .Where(u => u.Name, Op.Like, "A%")
    .ExecuteAsync();
```

Use `QueryPage(...)` when UI pagination needs both page rows and the total count for the same filters:

```csharp
var page = await connection
    .UseDb4Net(Db4NetOptions.SqlServer)
    .SelectFrom<User>()
    .Where(u => u.Name, Op.Like, "A%")
    .OrderBy(u => u.Id)
    .QueryPageAsync(pageNumber: 2, pageSize: 20);

var users = page.Items;
var totalCount = page.TotalCount;
var totalPages = page.TotalPages;
```

`QueryPage(...)` executes a count query and a paged row query internally. It owns paging, so do not call `Limit(...)`, `Offset(...)`, or `Page(...)` before `QueryPage(...)`.

Use `SelectAggregateFrom<T>()` for column-level scalar aggregates. `Max(...)`, `Min(...)`, `Sum(...)`, `Average(...)`, and `CountDistinct(...)` build scalar aggregate projections. Put explicit result typing on the terminal `Execute<TResult>()` or `ExecuteAsync<TResult>()` call, for example `Max(selector).Execute<TResult>()` or `CountDistinct(selector).ExecuteAsync<long>()`; use a nullable `TResult` when you need to preserve SQL `NULL` for empty result sets.

`Max(...)` and `Min(...)` require value-type member selectors. `Sum(...)` and `Average(...)` do not validate that the selected column is numeric; choose a terminal `TResult` that matches your provider's aggregate result.

```csharp
var latestId = connection
    .UseDb4Net(Db4NetOptions.SqlServer)
    .SelectAggregateFrom<User>()
    .Max(u => u.Id)
    .Where(u => u.Name, Op.Like, "A%")
    .Execute<int?>();

var distinctNames = await connection
    .UseDb4Net(Db4NetOptions.SqlServer)
    .SelectAggregateFrom<User>("users_2026")
    .CountDistinct(u => u.Name)
    .ExecuteAsync<long>();

var totalAmount = connection
    .UseDb4Net(Db4NetOptions.SqlServer)
    .SelectAggregateFrom<OrderMetric>()
    .Sum(o => o.Amount)
    .Execute<decimal>();

var totalQuantity = connection
    .UseDb4Net(Db4NetOptions.SqlServer)
    .SelectAggregateFrom<OrderMetric>()
    .Sum(o => o.Quantity)
    .Execute<long>();

var averageQuantity = connection
    .UseDb4Net(Db4NetOptions.SqlServer)
    .SelectAggregateFrom<OrderMetric>()
    .Average(o => o.Quantity)
    .Execute<decimal>();
```

Do not use `Select("COUNT(*)")`, `Select("MAX(...)")`, `Select("SUM(...)")`, `Select("AVG(...)")`, or similar strings for scalar queries. String select values are validated identifiers, not raw SQL expressions.

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

Use `ExecuteReturnKey<TResult>()` when a regular single-row insert should return the inserted key:

```csharp
var id = await connection
    .UseDb4Net(Db4NetOptions.Sqlite)
    .Insert(user)
    .ExecuteReturnKeyAsync<long>();

var stagedId = connection
    .UseDb4Net(Db4NetOptions.SqlServer)
    .Insert(user, table: "users_staging")
    .ExecuteReturnKey<long>(u => u.Id);

var explicitId = connection
    .UseDb4Net(Db4NetOptions.PostgreSql)
    .InsertInto<User>()
    .Values(user)
    .ReturnKey(u => u.Id)
    .Execute<long>();
```

Generated-key terminals are for regular single-row inserts. `InsertMany(...)`, `InsertOrIgnore(...)`, and `InsertOrUpdate(...)` return affected row counts and do not return per-row generated keys.

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

For a single inserted entity with a mapped key, use the generated-key terminals shown above when you need the database-created key instead of the affected row count.

Single-entity convenience methods reject sequence values such as `List<User>` or `User[]`; use the matching `Many` method instead.

`Update(entity)`, `Delete(entity)`, `UpdateMany(...)`, and `DeleteMany(...)` require exactly one mapped key and a non-default key value. Use SQL-shaped builders with explicit `Where(...)` clauses for composite-key models.

For multiple mapped objects, use the `Many` convenience methods. These execute validated, parameterized per-entity commands through Dapper and return the total affected row count. Empty sequences return `0`:

```csharp
var inserted = db
    .InsertMany(users)
    .Execute();

var updated = db
    .UpdateMany(users, table: "users_2026")
    .Execute();

var deleted = db
    .DeleteMany(users)
    .Execute();
```

Use an existing transaction through `Db4NetExecutionOptions.Transaction`, or let Db4Net own a lightweight transaction scope when several operations must be atomic:

```csharp
using var tx = db.BeginTransaction();

tx.Insert(user).Execute();
tx.Update(otherUser).Execute();

tx.Commit();
```

```csharp
db.ExecuteInTransaction(tx =>
{
    tx.Insert(user).Execute();
    tx.Update(otherUser).Execute();
});
```

This is a connection-bound `IDbTransaction` convenience, not an ORM unit of work. Db4Net still does not track entities, detect changes, batch saves, or add `SaveChanges()`.
When raw Dapper SQL must participate in the same transaction, create the `IDbTransaction` yourself and bind it with `WithTransaction(transaction)`.
After `Commit()`, `Rollback()`, or `Dispose()`, `tx.Database` and any transaction-bound builder or facade captured from it reject further execution.

Use conflict-aware insert conveniences when inserts should ignore or update rows that already match a conflict target:

```csharp
db.InsertOrIgnore(user, table: "users_staging")
  .OnConflict(u => u.Email)
  .Execute();

db.InsertOrUpdateMany(users, table: "users_2026")
  .OnConflict(u => u.Email)
  .Update(u => u.Name, u => u.UpdatedAt)
  .Execute();
```

`OnConflict(...)` accepts mapped CLR member selectors for the conflict target. `InsertOrUpdate` and `InsertOrUpdateMany` also support `Update(...)` to choose the mapped columns updated on conflict. Update columns cannot be database-generated columns and cannot overlap with the conflict target. When `OnConflict(...)` is omitted, Db4Net uses key metadata as the default conflict target; default conflict targets must be non-database-generated keys. The `Many` variants are Dapper multi-execute conveniences: one validated, parameterized command per entity, not provider-native import/copy APIs or optimized batching.

Use the SQL-shaped command builders when you need explicit fields or predicates:

```csharp
db.Update<User>()
  .Set(u => u.Name, "Alice")
  .Where(u => u.Id, Op.Eq, user.Id)
  .Execute();
```

Use `WhereGroup(...)` and `OrWhereGroup(...)` when mixed `AND` / `OR` filters need explicit parentheses:

```csharp
var users = db.SelectFrom<User>()
  .WhereGroup(group => group
      .Where(u => u.Id, Op.Eq, 1)
      .OrWhere(u => u.Name, Op.Eq, "Alice"))
  .Where(u => u.Id, Op.Gt, 0)
  .Query();
```

This renders grouped SQL such as `WHERE ([Id] = @p0 OR [Name] = @p1) AND [Id] > @p2`. Plain `Where(...)` and `OrWhere(...)` chains follow SQL's normal operator precedence and do not add parentheses automatically.

Use `WhereIn(...)`, `OrWhereIn(...)`, `WhereNotIn(...)`, and `OrWhereNotIn(...)` when an `IN` predicate should read from another single-column Db4Net `SELECT` query:

```csharp
var users = db.SelectFrom<User>()
  .WhereIn(
      u => u.Id,
      db.SelectFrom<Order>(o => o.UserId)
        .Where(o => o.Amount, Op.Gt, 100m))
  .Query();
```

The subquery must select exactly one column. Db4Net renders outer and nested query parameters through the same parameter writer, so parameter names remain collision-free.

Single command builders still support `ToCommand()`, and `Many` command builders support `ToCommands()` for inspecting the per-entity commands. None of these APIs add change tracking, relationship loading, identity maps, migrations, or `SaveChanges()` behavior.

Inspect SQL without executing it:

```csharp
var command = Db4NetDatabase
    .Create(Db4NetOptions.SqlServer)
    .SelectFrom<User>(u => u.Id, u => u.Name)
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
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

[Table("app_users")]
public sealed class User
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Column("display_name")]
    public string Name { get; set; } = "";

    [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
    public DateTime UpdatedAt { get; set; }

    [NotMapped]
    public string DisplayOnly { get; set; } = "";
}
```

Typed projections alias mapped columns so Dapper can map results back to property names:

```sql
SELECT [Id], [display_name] AS [Name] FROM [app_users]
```

`[Key]` and the `Id` / `<TypeName>Id` convention identify columns used to locate rows for entity command conveniences such as `Update(user)`, `UpdateMany(users)`, `Delete(user)`, `DeleteMany(users)`, and `WhereKey(user)`. Conflict-aware inserts use key metadata as the default conflict target and can use composite `[Key]` metadata; entity update/delete conveniences and many update/delete conveniences require a single key column.

`[Key]` is independent from `[DatabaseGenerated(...)]`. A `[Key]` column can also be `[DatabaseGenerated(DatabaseGeneratedOption.Identity)]`: it is still used in `WHERE` predicates for updates and deletes, and it can be returned by explicit single-row insert key terminals, but Db4Net omits it from automatic insert values because the database generates it.

`[DatabaseGenerated(DatabaseGeneratedOption.Identity)]` and `[DatabaseGenerated(DatabaseGeneratedOption.Computed)]` mapped properties are omitted by `Values(entity)`, `Insert(entity)`, `InsertMany(users)`, and conflict-aware insert values. Database-generated non-key properties are also omitted from entity-driven update assignments in `Update(entity)` and `UpdateMany(users)`. Database-generated members cannot be used as default or explicit conflict targets, and cannot be selected through `InsertOrUpdate.Update(...)`. Explicit `.Value(...)` and `.Set(...)` calls remain caller-controlled. Generated key readback is explicit and limited to regular single-row insert terminals; Db4Net does not mutate entity instances or refresh all computed values.

## Supported Dialects

- SQL Server
- SQLite
- PostgreSQL
- MySQL

Db4Net handles identifier quoting and paging syntax through the configured dialect.
For `SELECT` paging, `Offset(...)` must be paired with `Limit(...)`, and SQL Server paging requires at least one `OrderBy(...)`.

SQLite and PostgreSQL render native `ON CONFLICT` syntax. MySQL renders `INSERT IGNORE` for `InsertOrIgnore(...)` and `ON DUPLICATE KEY UPDATE` for `InsertOrUpdate(...)`; explicit `OnConflict(...)` selectors declare Db4Net's intended conflict columns, but MySQL itself applies duplicate handling to any primary or unique key violation. MySQL `INSERT IGNORE` can also turn some data errors into warnings according to MySQL rules. SQL Server renders `MERGE ... WITH (HOLDLOCK)` for conflict-aware inserts; this is not a provider-native import/copy API, optimized batch import, or set-based synchronization abstraction.

Generated-key insert terminals use SQL Server `OUTPUT INSERTED...`, SQLite/PostgreSQL `RETURNING`, and MySQL `LAST_INSERT_ID()` for auto-increment identity keys. SQLite requires runtime SQLite 3.35 or newer for `RETURNING`; SQL Server trigger-enabled tables can require an `OUTPUT ... INTO` pattern that Db4Net does not currently generate; MySQL does not return trigger/default/expression-generated non-identity keys through `LAST_INSERT_ID()`.

## Scope

Included in the current alpha:

- Typed `SELECT` builders
- Typed existence query builders through `SelectExistsFrom<T>()`
- Typed count query builders through `SelectCountFrom<T>()`
- Paged SELECT terminal methods through `QueryPage(...)` and `QueryPageAsync(...)`
- Typed scalar aggregate query builders through `SelectAggregateFrom<T>()` with `Max`, `Min`, `Sum`, `Average`, `CountDistinct`, and terminal result typing for all scalar aggregates
- Typed `INSERT`, `UPDATE`, and `DELETE` builders
- Single-row insert key return through `ExecuteReturnKey<TResult>()`, `ExecuteReturnKeyAsync<TResult>()`, and `ReturnKey(...).Execute<TResult>()`
- SQL-shaped command target overrides such as `InsertInto<T>("users_staging")`, `Update<T>("users_2026")`, and `DeleteFrom<T>("users_2026")`
- Entity command conveniences such as `Values(entity)`, `WhereKey(entity)`, `Insert(entity)`, `Insert(entity, table)`, `Update(entity)`, `Update(entity, table)`, `Delete(entity)`, and `Delete(entity, table)`
- Many entity command conveniences such as `InsertMany(users)`, `InsertMany(users, table)`, `UpdateMany(users)`, `UpdateMany(users, table)`, `DeleteMany(users)`, and `DeleteMany(users, table)`
- Conflict-aware insert conveniences such as `InsertOrIgnore(user)`, `InsertOrIgnoreMany(users)`, `InsertOrUpdate(user)`, `InsertOrUpdateMany(users)`, and their `table` overloads
- Dynamic property-name projection with model validation
- `Where`, `OrWhere`, single-column `WhereIn` subqueries, `WhereGroup`, `OrWhereGroup`, `OrderBy`, `OrderByDescending`, `Limit`, `Offset`, and `Page`
- `Value`, `Set`, `Execute`, and `ExecuteAsync` for command builders
- Sync and async Dapper-style query terminal methods
- Existing transaction pass-through, lightweight transaction scopes, command timeout, command type, and async cancellation token support

Intentionally out of scope for now:

- Joins
- Provider-native copy/import APIs, set-based synchronization, and optimized batching
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
- `engineering`: internal analysis notes, design records, and implementation plans

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
