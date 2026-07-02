# Repository Pattern

Db4Net can sit inside a repository layer when you want application code to call business-shaped data access methods instead of composing SQL builders directly.

The repository should hide Db4Net builders from callers. Expose methods such as `FindByIdAsync`, `ExistsByEmailAsync`, or `FindActivePageAsync`; keep `SelectFrom<T>()`, `Query*`, and `Execute*` inside the data access layer.

## Repository Class

```csharp
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Db4Net;

[Table("Users")]
public sealed class User
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public long Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public bool IsActive { get; set; }
}

public sealed class UserRepository
{
    private readonly Db4NetDatabase _db;

    public UserRepository(Db4NetDatabase db)
    {
        _db = db;
    }

    public Task<User?> FindByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        return _db
            .SelectFrom<User>()
            .Where(u => u.Id, Op.Eq, id)
            .QuerySingleOrDefaultAsync(cancellationToken: cancellationToken);
    }

    public Task<PagedResult<User>> FindActivePageAsync(
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        return _db
            .SelectFrom<User>()
            .Where(u => u.IsActive, Op.Eq, true)
            .OrderBy(u => u.Id)
            .QueryPageAsync(pageNumber, pageSize, cancellationToken: cancellationToken);
    }

    public Task<bool> ExistsByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        return _db
            .SelectExistsFrom<User>()
            .Where(u => u.Email, Op.Eq, email)
            .ExecuteAsync(cancellationToken: cancellationToken);
    }

    public Task<long> AddAsync(User user, CancellationToken cancellationToken = default)
    {
        return _db
            .Insert(user)
            .ExecuteReturnKeyAsync<long>(u => u.Id, cancellationToken: cancellationToken);
    }

    public Task<int> RenameAsync(long id, string name, CancellationToken cancellationToken = default)
    {
        return _db
            .Update<User>()
            .Set(u => u.Name, name)
            .Where(u => u.Id, Op.Eq, id)
            .ExecuteAsync(cancellationToken: cancellationToken);
    }

    public Task<int> DeleteAsync(User user, CancellationToken cancellationToken = default)
    {
        return _db
            .Delete(user)
            .ExecuteAsync(cancellationToken: cancellationToken);
    }
}
```

This shape keeps the repository easy to test at the API boundary: callers see repository methods and return types, not Db4Net builder types.

## Method Naming

Use repository method names for data access intent, and service method names for business use cases.

Common repository names:

- `FindByIdAsync(...)`: returns one row or `null` when it does not exist.
- `GetByIdAsync(...)`: expects the row to exist; use `QuerySingleAsync()` or throw when it does not.
- `ListBy...Async(...)`: returns a list for a simple filter.
- `SearchAsync(...)`: uses richer criteria such as keywords, optional filters, or sort options.
- `GetPageAsync(...)`: returns a `PagedResult<T>` or another paged DTO.
- `ExistsBy...Async(...)`: returns a boolean existence check.
- `CountAsync(...)`: returns a matching row count.
- `AddAsync(...)`, `UpdateAsync(...)`, and `DeleteAsync(...)`: perform data changes.

Keep Db4Net execution names such as `Query*` and `Execute*` inside the repository. Service methods should describe the business operation instead, for example `RegisterUserAsync(...)`, `DisableUserAsync(...)`, or `ChangeEmailAsync(...)`.

## Connection Scope

A repository that stores a `Db4NetDatabase` should follow the lifetime of the connection or transaction bound to that facade. In request-scoped applications that use `Microsoft.Extensions.DependencyInjection`, register the connection, `Db4NetDatabase`, and repositories as scoped services. See [Application Patterns](./application-patterns.md#request-scoped-di) for the complete DI setup.

For ordinary non-transactional queries, the scoped connection can remain closed and Dapper can open and close it at execution time. When a use case needs a transaction, open the connection at the service, unit-of-work, or session-factory transaction boundary before calling `BeginTransaction()`. If the request scope itself intentionally represents a unit of work, opening the connection in the scoped factory is also valid. It does not conflict with Dapper, but it should not be the default for every ordinary query.

If DI created the connection object, the unit of work should not dispose that connection object; the DI scope owns final disposal. The unit of work should only close a connection that it opened from the closed state. If the connection was already opened in the scoped factory, the unit of work should only dispose the transaction and leave the connection open.

## Multiple Databases

When repositories target different fixed databases, register keyed scoped services for each database. `AddKeyedScoped` and `GetRequiredKeyedService` are .NET 8 `Microsoft.Extensions.DependencyInjection` APIs; if your DI container does not support keyed services, use an application-level factory or the container's named/keyed registration feature.

```csharp
public enum DatabaseId
{
    Main,
    Audit
}

services.AddKeyedScoped<DbConnection>(DatabaseId.Main, (sp, key) =>
{
    var connectionString = sp.GetRequiredService<IConfiguration>()
        .GetConnectionString("Main")!;

    var connection = new SqliteConnection(connectionString);
    connection.Open();
    return connection;
});

services.AddKeyedScoped<Db4NetDatabase>(DatabaseId.Main, (sp, key) =>
{
    var connection = sp.GetRequiredKeyedService<DbConnection>(DatabaseId.Main);
    return connection.UseDb4Net(Db4NetOptions.Sqlite);
});

services.AddKeyedScoped<DbConnection>(DatabaseId.Audit, (sp, key) =>
{
    var connectionString = sp.GetRequiredService<IConfiguration>()
        .GetConnectionString("Audit")!;

    var connection = new SqliteConnection(connectionString);
    connection.Open();
    return connection;
});

services.AddKeyedScoped<Db4NetDatabase>(DatabaseId.Audit, (sp, key) =>
{
    var connection = sp.GetRequiredKeyedService<DbConnection>(DatabaseId.Audit);
    return connection.UseDb4Net(Db4NetOptions.Sqlite);
});
```

Bind each repository to the database it owns:

```csharp
services.AddScoped<UserRepository>(sp =>
{
    var db = sp.GetRequiredKeyedService<Db4NetDatabase>(DatabaseId.Main);
    return new UserRepository(db);
});

services.AddScoped<AuditRepository>(sp =>
{
    var db = sp.GetRequiredKeyedService<Db4NetDatabase>(DatabaseId.Audit);
    return new AuditRepository(db);
});
```

Use keyed scoped services for fixed database boundaries such as `Main` and `Audit`. If the database must be selected at runtime, for example by tenant or request data, put that selection behind an application-level factory instead of binding a repository to one fixed keyed database.

Db4Net transaction scopes are single-connection scopes. Do not assume one `ExecuteInTransaction(...)` call can make work across multiple databases atomic.

For applications without DI, create the connection and Db4Net facade at the application, request, service, or unit-of-work boundary, then pass the facade into repositories for that scope. See [Application Patterns](./application-patterns.md#no-di-program) for a session factory example.

Do not register a repository that captures a connection-bound `Db4NetDatabase` as a singleton. The facade is lightweight and should follow the lifetime of the connection or transaction it is bound to.

## Raw Dapper SQL

Db4Net does not wrap raw SQL. Keep complex joins, CTEs, window functions, and provider-specific SQL in Dapper, using the same scoped connection as Db4Net.

```csharp
using Dapper;
using Db4Net;

public sealed class ReportRepository
{
    private readonly Db4NetDatabase _db;

    public ReportRepository(Db4NetDatabase db)
    {
        _db = db;
    }

    public Task<IEnumerable<UserActivityRow>> GetUserActivityAsync(
        long? userId = null,
        CancellationToken cancellationToken = default)
    {
        return _db.Connection.QueryAsync<UserActivityRow>(
            new CommandDefinition(
                """
                SELECT
                    u.Id,
                    u.Name,
                    COUNT(a.Id) AS ActivityCount
                FROM Users u
                LEFT JOIN Activities a ON a.UserId = u.Id
                WHERE (@UserId IS NULL OR u.Id = @UserId)
                GROUP BY u.Id, u.Name
                ORDER BY ActivityCount DESC
                """,
                new { UserId = userId },
                transaction: _db.DbTransaction,
                cancellationToken: cancellationToken));
    }

    public Task<User?> FindByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        return _db
            .SelectFrom<User>()
            .Where(u => u.Id, Op.Eq, id)
            .QuerySingleOrDefaultAsync(cancellationToken: cancellationToken);
    }
}
```

For keyed database registrations, inject or resolve the matching keyed `Db4NetDatabase`; its `Connection` exposes the matching borrowed connection context. Do not close, dispose, or open that connection inside repositories.

When raw Dapper SQL and Db4Net commands must share a Db4Net-owned transaction, use `_db.Connection` and pass `transaction: _db.DbTransaction` explicitly from a repository created with the transaction-bound `tx.Database` facade. `DbTransaction` is `null` when no transaction is active, so the same repository method runs as an ordinary non-transactional query.

```csharp
using var tx = _db.BeginTransaction();

try
{
    await tx.Database
        .Update<User>()
        .Set(u => u.Name, "Alice")
        .Where(u => u.Id, Op.Eq, userId)
        .ExecuteAsync(cancellationToken: cancellationToken);

    await tx.Database.Connection.ExecuteAsync(
        new CommandDefinition(
            """
            INSERT INTO AuditLogs (EventName, EntityId)
            VALUES (@EventName, @EntityId)
            """,
            new { EventName = "UserRenamed", EntityId = userId },
            transaction: tx.Database.DbTransaction,
            cancellationToken: cancellationToken));

    tx.Commit();
}
catch
{
    tx.Rollback();
    throw;
}
```

If the transaction is created outside Db4Net, pass it to Dapper and bind it to Db4Net with `WithTransaction(...)`.

## Transactions

When several repository calls must be atomic, keep the transaction in the service or unit-of-work layer and build repositories from the transaction-bound facade.

If repositories are created by DI with the request-scoped `Db4NetDatabase`, transaction-specific work can either use `tx.Database` directly in the service layer or create short-lived repositories from `tx.Database` inside the transaction delegate.

`ExecuteInTransactionAsync(...)` commits when the delegate succeeds and rolls back when it throws. Db4Net still does not track entities or provide `SaveChanges()`. See [Application Patterns](./application-patterns.md#service-transaction-boundary) for service-level transaction examples and [Business Unit of Work](./application-patterns.md#business-unit-of-work) for a reusable application helper.

## Guidelines

- Keep Db4Net builder composition inside repositories.
- Return domain models, DTOs, scalar values, `PagedResult<T>`, or affected row counts from repository methods.
- Do not return `SelectQueryBuilder<T>` or command builders from repository methods.
- Use repository methods for stable application use cases; use Db4Net directly in small tools, tests, or scripts when an extra abstraction adds no value.
- Keep complex joins, CTEs, window functions, and provider-specific SQL in raw Dapper queries inside the same data access layer.
