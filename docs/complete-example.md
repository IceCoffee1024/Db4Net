# Complete Example

This page shows one practical Db4Net workflow from connection setup to reads, writes, transactions, and the point where raw Dapper remains the better tool.

## Model

```csharp
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

[Table("Users")]
public sealed class User
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public long Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public bool IsActive { get; set; }

    public DateTime UpdatedAt { get; set; }
}
```

`[Key]` identifies the key used by entity conveniences such as `Update(user)` and `Delete(user)`. `[DatabaseGenerated(DatabaseGeneratedOption.Identity)]` tells Db4Net to omit the column from entity inserts and to use provider-specific generated-key readback when you call `ExecuteReturnKey<TResult>()`.

## Create the Facade

```csharp
using Db4Net;
using Microsoft.Data.Sqlite;

await using var connection = new SqliteConnection(connectionString);
await connection.OpenAsync();

var db = connection.UseDb4Net(Db4NetOptions.Sqlite);
```

`UseDb4Net(...)` binds Db4Net to the same `IDbConnection` that Dapper uses. Db4Net builds parameterized SQL and terminal methods execute through Dapper.

## Insert and Read Back the Key

```csharp
var userId = await db
    .Insert(new User
    {
        Name = "Alice",
        IsActive = true,
        UpdatedAt = DateTime.UtcNow
    })
    .ExecuteReturnKeyAsync<long>();
```

Use generated-key terminals for single-row inserts. `InsertMany(...)`, `InsertOrIgnore(...)`, and `InsertOrUpdate(...)` return affected row counts.

## Query the Row

```csharp
var user = await db
    .SelectFrom<User>()
    .Where(u => u.Id, Op.Eq, userId)
    .QuerySingleOrDefaultAsync();
```

Use `SelectFrom<T>()` when you want mapped entity rows. Use `SelectFrom<T>(...)` when you only need selected mapped columns.

## Check Existence and Counts

```csharp
var exists = await db
    .SelectExistsFrom<User>()
    .Where(u => u.Id, Op.Eq, userId)
    .ExecuteAsync();

var activeCount = await db
    .SelectCountFrom<User>()
    .Where(u => u.IsActive, Op.Eq, true)
    .ExecuteAsync();
```

Prefer `SelectExistsFrom<T>()` for existence checks instead of counting rows and comparing with zero.

## Query a Page With Total Count

```csharp
var page = await db
    .SelectFrom<User>()
    .Where(u => u.IsActive, Op.Eq, true)
    .OrderBy(u => u.Id)
    .QueryPageAsync(pageNumber: 1, pageSize: 20);

var users = page.Items;
var totalCount = page.TotalCount;
var totalPages = page.TotalPages;
```

`QueryPage(...)` keeps the row query and count query aligned by reusing the same table and filters. It executes two commands internally, so use it for convenience and consistency rather than as a single-query optimization.

## Update in a Transaction

```csharp
await db.ExecuteInTransactionAsync(async tx =>
{
    await tx
        .Update(new User
        {
            Id = userId,
            Name = "Alice Updated",
            IsActive = true,
            UpdatedAt = DateTime.UtcNow
        })
        .ExecuteAsync();

    await tx
        .InsertMany(
        [
            new User { Name = "Bob", IsActive = true, UpdatedAt = DateTime.UtcNow },
            new User { Name = "Charlie", IsActive = false, UpdatedAt = DateTime.UtcNow }
        ])
        .ExecuteAsync();
});
```

`ExecuteInTransaction(...)` and `ExecuteInTransactionAsync(...)` commit when the delegate succeeds and roll back when it throws. Db4Net still does not track entities or add `SaveChanges()`.

## Use SQL-Shaped Builders for Partial Updates

```csharp
var affected = await db
    .Update<User>()
    .Set(u => u.Name, "Alice Final")
    .Where(u => u.Id, Op.Eq, userId)
    .ExecuteAsync();
```

Use entity conveniences for full mapped entity commands. Use SQL-shaped builders when you need selected fields, complex filters, or explicit all-row behavior.

## Use Dapper for Complex SQL

```csharp
using Dapper;

var rows = await connection.QueryAsync<UserActivityRow>(
    """
    SELECT u.Id, u.Name, COUNT(a.Id) AS ActivityCount
    FROM Users u
    LEFT JOIN Activities a ON a.UserId = u.Id
    GROUP BY u.Id, u.Name
    ORDER BY ActivityCount DESC
    """);
```

Db4Net intentionally does not cover joins, CTEs, window functions, provider-specific hints, or full SQL expression construction. Keep those queries in Dapper raw SQL.

## Recommended Patterns

- Use `Insert(user)`, `Update(user)`, and `Delete(user)` for common single-entity commands.
- Use `InsertInto<T>()`, `Update<T>()`, and `DeleteFrom<T>()` when the command should read like SQL.
- Use `SelectExistsFrom<T>()` for existence checks and `SelectCountFrom<T>()` only when the count matters.
- Use `QueryPage(...)` when UI pagination needs both page rows and total count.
- Use `SelectAggregateFrom<T>()` for scalar aggregates such as `Max`, `Min`, `Sum`, `Average`, and `CountDistinct`.
- Use `table:` overloads for staging tables, archive tables, sharded tables, or views that share the same model mapping.
- Use explicit transactions for workflows with more than one write.
- Use Dapper raw SQL for joins and provider-specific SQL.
