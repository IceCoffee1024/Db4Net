# Db4Net

Db4Net is a lightweight fluent SQL builder for Dapper. It focuses on safe, parameterized single-table queries and commands while leaving execution and SELECT result materialization to Dapper.

The API is intentionally SQL-shaped: `SelectFrom<T>()`, `SelectExistsFrom<T>()`, `SelectCountFrom<T>()`, `SelectAggregateFrom<T>()`, `InsertInto<T>()`, `Update<T>()`, and `DeleteFrom<T>()` keep statement order recognizable while still validating identifiers and parameterizing values.

Db4Net is not an ORM and does not try to become a LINQ provider.

## Install

```bash
dotnet add package Db4Net --prerelease
```

Package assets target `net8.0` and `netstandard2.0`.

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

Use `SelectFrom<T>(...)` when querying specific mapped properties:

```csharp
var users = connection
    .UseDb4Net(Db4NetOptions.Sqlite)
    .SelectFrom<User>(u => u.Id, u => u.Name)
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

Use `SelectAggregateFrom<T>()` for column-level scalar aggregates. `Max(...)`, `Min(...)`, `Sum(...)`, `Average(...)`, and `CountDistinct(...)` build scalar aggregate projections. Put explicit result typing on the terminal `Execute<TResult>()` or `ExecuteAsync<TResult>()` call, for example `Max(selector).Execute<TResult>()` or `CountDistinct(selector).ExecuteAsync<long>()`; use a nullable `TResult` when you need to preserve SQL `NULL` for empty result sets.

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

Use `SelectFrom<T>("view_or_table")` or `Select(...).From<T>("view_or_table")` when the SELECT target is a view, tenant table, or time-partitioned table but the column mapping still comes from `T`.

Use entity command conveniences for common single-row commands while keeping the same SQL-shaped builder model. Here, entity means a mapped CLR object used as a value source, not a tracked ORM entity:

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

Single-entity convenience methods reject sequence values such as `List<User>` or `User[]`; use the matching `Many` method instead.

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

Use conflict-aware insert conveniences when inserts should ignore or update rows that already match a conflict target:

```csharp
db.InsertOrUpdate(user)
  .OnConflict(u => u.Email)
  .Update(u => u.Name, u => u.UpdatedAt)
  .Execute();

db.InsertOrIgnoreMany(users, table: "users_staging")
  .OnConflict(u => u.Email)
  .Execute();
```

`InsertOrIgnore`, `InsertOrIgnoreMany`, `InsertOrUpdate`, and `InsertOrUpdateMany` also support `table` overloads. The explicit table changes only the SQL target table; CLR member-to-column mapping still comes from `T`. `OnConflict(...)` and `Update(...)` use mapped CLR member selectors, not database column-name strings or SQL fragments. When `OnConflict(...)` is omitted, Db4Net uses key metadata as the default conflict target; default conflict targets must be non-database-generated keys. The `Many` variants are Dapper multi-execute conveniences that run one parameterized command per entity; they are not provider-native import/copy APIs or set-based synchronization APIs.

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

Single command builders generate inspectable SQL through `ToCommand()`, and `Many` command builders expose `ToCommands()` for inspecting the per-entity commands. None of these APIs add change tracking, relationship loading, identity maps, migrations, or `SaveChanges()` behavior.

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

`[NotMapped]` members are excluded from `SelectFrom<T>()` and rejected in typed `Select`, `Where`, `OrderBy`, `Value`, and `Set` member selectors.

`[Key]` and the `Id` / `<TypeName>Id` convention identify columns used to locate rows for entity command conveniences such as `Update(user)`, `UpdateMany(users)`, `Delete(user)`, `DeleteMany(users)`, and `WhereKey(user)`. Conflict-aware inserts use key metadata as the default conflict target and can use composite `[Key]` metadata; entity update/delete conveniences and many update/delete conveniences require a single key column.

`[Key]` is independent from `[DatabaseGenerated(...)]`. A `[Key]` column can also be `[DatabaseGenerated(DatabaseGeneratedOption.Identity)]`: it is still used in `WHERE` predicates for updates and deletes, but Db4Net omits it from automatic insert values because the database generates it.

`[DatabaseGenerated(DatabaseGeneratedOption.Identity)]` and `[DatabaseGenerated(DatabaseGeneratedOption.Computed)]` mapped properties are omitted by `Values(entity)`, `Insert(entity)`, `InsertMany(users)`, and conflict-aware insert values. Database-generated non-key properties are also omitted from entity-driven update assignments in `Update(entity)` and `UpdateMany(users)`. Database-generated members cannot be used as default or explicit conflict targets, and cannot be selected through `InsertOrUpdate.Update(...)`. Explicit `.Value(...)` and `.Set(...)` calls remain caller-controlled.

## Filters

```csharp
.Where(u => u.Id, Op.Eq, 1)
.Where(u => u.Name, Op.Like, "A%")
.Where(u => u.Id, Op.In, new[] { 1, 2, 3 })
.Where(u => u.DeletedAt, Op.IsNull)
.Where(u => u.Name, Op.IsNotNull)
```

`Op.Eq` with `null` renders `IS NULL`, and `Op.NotEq` with `null` renders `IS NOT NULL`. Prefer `Op.IsNull` and `Op.IsNotNull` when no value is needed.

Use `WhereGroup(...)` / `OrWhereGroup(...)` for nested parentheses. The group builder only exposes filter methods, so ordering, paging, and command rendering stay outside the group.

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

`Offset(...)` must be paired with `Limit(...)`. SQL Server paging also requires at least one `OrderBy(...)` because `OFFSET` / `FETCH` is invalid without `ORDER BY`.

## Inspect SQL

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

Existence query builders return a `bool` through `Execute()` and `ExecuteAsync()`. Count query builders return the count through `Execute()` and `ExecuteAsync()`. For `SelectAggregateFrom<T>()` aggregate queries, specify the scalar read type with terminal `Execute<TResult>()` or `ExecuteAsync<TResult>()`.

INSERT, UPDATE, DELETE, and conflict-aware insert builders provide command terminal methods:

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

Db4Net passes an existing transaction to Dapper when you provide one through `Db4NetExecutionOptions.Transaction`. In that mode it does not own the transaction lifetime.

For a Db4Net-owned lightweight transaction scope, use `BeginTransaction()`:

```csharp
using var tx = connection
    .UseDb4Net(Db4NetOptions.Sqlite)
    .BeginTransaction();

tx.Insert(user).Execute();
tx.Update(otherUser).Execute();
tx.Commit();
```

Disposing a `Db4NetTransaction` without calling `Commit()` rolls it back. `ExecuteInTransaction(...)` commits when the delegate succeeds and rolls back when it throws:

```csharp
connection
    .UseDb4Net(Db4NetOptions.Sqlite)
    .ExecuteInTransaction(tx =>
    {
        tx.Insert(user).Execute();
        tx.Update(otherUser).Execute();
    });
```

The async `ExecuteInTransactionAsync(...)` overloads run async Db4Net operations inside the same transaction. Transaction begin, commit, and rollback use the synchronous `IDbTransaction` APIs because Db4Net is connection-bound through `IDbConnection`.

When raw Dapper SQL must participate in the same transaction, create the `IDbTransaction` yourself and bind it with `WithTransaction(transaction)`.

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

Current scope is focused on typed single-table `SELECT`, scalar aggregate, `INSERT`, `UPDATE`, `DELETE`, and conflict-aware insert builders for SQL Server, SQLite, PostgreSQL, and MySQL. Table and view overrides plus single-entity and many-entity command conveniences are supported for safe SQL-shaped APIs, and lightweight transaction scopes are available for grouping explicit operations. Joins, provider-native copy/import APIs, set-based synchronization, optimized batching, change tracking, relationship loading, `SaveChanges()` style unit-of-work behavior, migrations, and full predicate expression translation are intentionally out of scope for this early version.

SQLite and PostgreSQL render native `ON CONFLICT` syntax. MySQL renders `ON DUPLICATE KEY UPDATE`; explicit `OnConflict(...)` selectors declare Db4Net's intended conflict columns but MySQL itself applies duplicate handling to any primary or unique key violation. SQL Server renders a dialect-specific conflict-aware command; this is not a provider-native import/copy API, optimized batch import, or set-based synchronization abstraction.

For complex joins or database-specific SQL, use Dapper raw SQL directly or expose stable read models through database views.
