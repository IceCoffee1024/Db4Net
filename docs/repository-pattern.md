# Repository Pattern

Db4Net can sit inside a repository layer when you want application code to call business-shaped data access methods instead of composing SQL builders directly.

The repository should hide Db4Net builders from callers. Expose methods such as `FindByIdAsync`, `EmailExistsAsync`, or `FindActivePageAsync`; keep `SelectFrom<T>()`, `Query*`, and `Execute*` inside the data access layer.

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

    public Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default)
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

## Connection Scope

For request-scoped applications that use `Microsoft.Extensions.DependencyInjection`, register the connection and `Db4NetDatabase` as scoped services. Do not register them as singletons.

```csharp
using System.Data.Common;
using Db4Net;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

services.AddScoped<DbConnection>(sp =>
{
    var connectionString = sp.GetRequiredService<IConfiguration>()
        .GetConnectionString("Default")!;

    var connection = new SqliteConnection(connectionString);
    connection.Open();
    return connection;
});

services.AddScoped(sp =>
{
    var connection = sp.GetRequiredService<DbConnection>();
    return connection.UseDb4Net(Db4NetOptions.Sqlite);
});

services.AddScoped<UserRepository>();
```

Opening the connection in the scoped factory does not conflict with Dapper. Dapper uses an already-open connection and leaves it open; the DI scope disposes the connection at the end of the request.

For applications without DI, create the connection and Db4Net facade at the application, request, service, or unit-of-work boundary, then pass the facade into repositories for that scope.

```csharp
await using var connection = connectionFactory.CreateConnection();
await connection.OpenAsync(cancellationToken);

var db = connection.UseDb4Net(Db4NetOptions.Sqlite);
var users = new UserRepository(db);

var user = await users.FindByIdAsync(1, cancellationToken);
```

Do not register a repository that captures a connection-bound `Db4NetDatabase` as a singleton. The facade is lightweight and should follow the lifetime of the connection or transaction it is bound to.

## Transactions

When several repository calls must be atomic, keep the transaction in the service or unit-of-work layer and build repositories from the transaction-bound facade.

If repositories are created by DI with the request-scoped `Db4NetDatabase`, transaction-specific work can either use `tx.Database` directly in the service layer or create short-lived repositories from `tx.Database` inside the transaction delegate.

```csharp
await using var connection = connectionFactory.CreateConnection();
await connection.OpenAsync(cancellationToken);

var db = connection.UseDb4Net(Db4NetOptions.Sqlite);

await db.ExecuteInTransactionAsync(async tx =>
{
    var users = new UserRepository(tx.Database);

    var id = await users.AddAsync(
        new User
        {
            Name = "Alice",
            Email = "alice@example.com",
            IsActive = true
        },
        cancellationToken);

    await users.RenameAsync(id, "Alice Updated", cancellationToken);
});
```

`ExecuteInTransactionAsync(...)` commits when the delegate succeeds and rolls back when it throws. Db4Net still does not track entities or provide `SaveChanges()`.

## Guidelines

- Keep Db4Net builder composition inside repositories.
- Return domain models, DTOs, scalar values, `PagedResult<T>`, or affected row counts from repository methods.
- Do not return `SelectQueryBuilder<T>` or command builders from repository methods.
- Use repository methods for stable application use cases; use Db4Net directly in small tools, tests, or scripts when an extra abstraction adds no value.
- Keep complex joins, CTEs, window functions, and provider-specific SQL in raw Dapper queries inside the same data access layer.
