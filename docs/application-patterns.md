# Application Patterns

Db4Net stays focused on query and command builders. Application architecture such as repositories, services, background jobs, and unit-of-work factories should live in your application code.

This page shows practical composition patterns for applications that need repository methods, service-level transactions, singleton/background workers, or no dependency injection at all.

## Layer Responsibilities

Use this split as the default:

- Repository: data access methods such as `FindByIdAsync`, `ExistsByEmailAsync`, `DisableAsync`, and report queries. Keep Db4Net builders inside the repository.
- Service: business use cases, validation, orchestration across repositories, and transaction boundaries.
- Unit of Work: optional application helper that starts a transaction and creates transaction-bound repositories.

Do not make repositories guess an ambient transaction. When a repository must run inside a transaction, create it from the transaction-bound `Db4NetDatabase`.

For the basic repository shape and repository-only examples, see [Repository Pattern](./repository-pattern.md).

## Shared Repositories

```csharp
using Db4Net;

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

    public Task<int> DisableAsync(long id, CancellationToken cancellationToken = default)
    {
        return _db
            .Update<User>()
            .Set(u => u.IsActive, false)
            .Where(u => u.Id, Op.Eq, id)
            .ExecuteAsync(cancellationToken: cancellationToken);
    }
}

public sealed class AuditRepository
{
    private readonly Db4NetDatabase _db;

    public AuditRepository(Db4NetDatabase db)
    {
        _db = db;
    }

    public Task<int> WriteAsync(
        string eventName,
        long entityId,
        CancellationToken cancellationToken = default)
    {
        return _db
            .Insert(new AuditLog
            {
                EventName = eventName,
                EntityId = entityId,
                CreatedAt = DateTime.UtcNow
            })
            .ExecuteAsync(cancellationToken: cancellationToken);
    }
}
```

## Service Transaction Boundary

When several repository calls must be atomic, keep the transaction in the service layer and create short-lived repositories from `tx.Database`.

```csharp
public sealed class RepositoryFactory
{
    public UserRepository Users(Db4NetDatabase db) => new(db);

    public AuditRepository Audit(Db4NetDatabase db) => new(db);
}

public sealed class UserService
{
    private readonly Db4NetDatabase _db;
    private readonly RepositoryFactory _repositories;

    public UserService(Db4NetDatabase db, RepositoryFactory repositories)
    {
        _db = db;
        _repositories = repositories;
    }

    public async Task DisableUserAsync(long userId, CancellationToken cancellationToken = default)
    {
        using var tx = _db.BeginTransaction();

        var users = _repositories.Users(tx.Database);
        var audit = _repositories.Audit(tx.Database);

        await users.DisableAsync(userId, cancellationToken);
        await audit.WriteAsync("UserDisabled", userId, cancellationToken);

        tx.Commit();
    }
}
```

`BeginTransaction()` requires an open connection. If you want to use it from a scoped `Db4NetDatabase`, open the scoped connection when you register it or use an explicit session/unit-of-work factory that opens the connection before beginning the transaction.

## Business Unit of Work

When several services repeat the same transaction setup and repository creation, wrap that composition in an application-side unit of work. This is not a Db4Net `SaveChanges()` abstraction; it is only a small owner for one transaction and the repositories bound to it.

```csharp
public sealed class AppUnitOfWork : IDisposable
{
    private readonly Db4NetTransaction _tx;

    public AppUnitOfWork(Db4NetDatabase db, RepositoryFactory repositories)
    {
        _tx = db.BeginTransaction();

        Users = repositories.Users(_tx.Database);
        Audit = repositories.Audit(_tx.Database);
    }

    public UserRepository Users { get; }

    public AuditRepository Audit { get; }

    public void Commit()
    {
        _tx.Commit();
    }

    public void Dispose()
    {
        _tx.Dispose();
    }
}

public sealed class AppUnitOfWorkFactory
{
    private readonly Db4NetDatabase _db;
    private readonly RepositoryFactory _repositories;

    public AppUnitOfWorkFactory(Db4NetDatabase db, RepositoryFactory repositories)
    {
        _db = db;
        _repositories = repositories;
    }

    public AppUnitOfWork Begin()
    {
        return new AppUnitOfWork(_db, _repositories);
    }
}
```

The service can then depend on the application unit-of-work factory instead of manually creating each transaction-bound repository.

```csharp
public sealed class UserService
{
    private readonly AppUnitOfWorkFactory _unitOfWorkFactory;

    public UserService(AppUnitOfWorkFactory unitOfWorkFactory)
    {
        _unitOfWorkFactory = unitOfWorkFactory;
    }

    public async Task DisableUserAsync(long userId, CancellationToken cancellationToken = default)
    {
        using var uow = _unitOfWorkFactory.Begin();

        await uow.Users.DisableAsync(userId, cancellationToken);
        await uow.Audit.WriteAsync("UserDisabled", userId, cancellationToken);

        uow.Commit();
    }
}
```

Register the unit-of-work factory as scoped because it captures the scoped `Db4NetDatabase`.

```csharp
services.AddSingleton<RepositoryFactory>();
services.AddScoped<AppUnitOfWorkFactory>();
services.AddScoped<UserService>();
```

This explicit property style works well for a small feature-specific unit of work. In a larger application, do not put every repository in the system on one global `AppUnitOfWork`; create focused units of work per use case area, or use DI-based repository creation as shown below.

## Large DI Applications

If the application already uses `Microsoft.Extensions.DependencyInjection`, a separate `RepositoryFactory` is optional. Keep `IServiceProvider` inside infrastructure code, create transaction-bound repositories on demand, and do not expose the service provider to business services.

```csharp
using System.Data;
using Db4Net;
using Dapper;
using Microsoft.Extensions.DependencyInjection;

public sealed class DiUnitOfWork : IDisposable
{
    private readonly Db4NetTransaction _tx;
    private readonly IServiceProvider _services;

    public DiUnitOfWork(Db4NetDatabase db, IServiceProvider services)
    {
        _tx = db.BeginTransaction();
        _services = services;
    }

    public TRepository Repository<TRepository>()
        where TRepository : class
    {
        return ActivatorUtilities.CreateInstance<TRepository>(
            _services,
            _tx.Database);
    }

    public TRepository RepositoryWithTransaction<TRepository>()
        where TRepository : class
    {
        return ActivatorUtilities.CreateInstance<TRepository>(
            _services,
            _tx.Database,
            _tx.Connection,
            _tx.DbTransaction);
    }

    public void Commit()
    {
        _tx.Commit();
    }

    public void Dispose()
    {
        _tx.Dispose();
    }
}

public sealed class DiUnitOfWorkFactory
{
    private readonly Db4NetDatabase _db;
    private readonly IServiceProvider _services;

    public DiUnitOfWorkFactory(Db4NetDatabase db, IServiceProvider services)
    {
        _db = db;
        _services = services;
    }

    public DiUnitOfWork Begin()
    {
        return new DiUnitOfWork(_db, _services);
    }
}
```

Repositories should usually be lightweight and stateless, so caching repository instances inside the unit of work is not required. If a repository has expensive state or the application needs same-instance reuse, add a small dictionary cache in the application unit-of-work class.

Use `Repository<TRepository>()` for repositories that only need the Db4Net facade. Use `RepositoryWithTransaction<TRepository>()` only for repositories whose constructor accepts `IDbConnection` and `IDbTransaction` for raw Dapper SQL in the same transaction.

```csharp
public sealed class AuditRepository
{
    private readonly Db4NetDatabase _db;
    private readonly IDbConnection _connection;
    private readonly IDbTransaction? _transaction;

    public AuditRepository(
        Db4NetDatabase db,
        IDbConnection connection,
        IDbTransaction? transaction = null)
    {
        _db = db;
        _connection = connection;
        _transaction = transaction;
    }

    public Task<int> WriteRawAsync(
        string eventName,
        long entityId,
        CancellationToken cancellationToken = default)
    {
        return _connection.ExecuteAsync(
            new CommandDefinition(
                """
                INSERT INTO AuditLogs (EventName, EntityId)
                VALUES (@EventName, @EntityId)
                """,
                new { EventName = eventName, EntityId = entityId },
                transaction: _transaction,
                cancellationToken: cancellationToken));
    }
}
```

The service still depends on the unit-of-work abstraction, not on `IServiceProvider`.

```csharp
public sealed class UserService
{
    private readonly DiUnitOfWorkFactory _unitOfWorkFactory;

    public UserService(DiUnitOfWorkFactory unitOfWorkFactory)
    {
        _unitOfWorkFactory = unitOfWorkFactory;
    }

    public async Task DisableUserAsync(long userId, CancellationToken cancellationToken = default)
    {
        using var uow = _unitOfWorkFactory.Begin();

        var users = uow.Repository<UserRepository>();
        var audit = uow.RepositoryWithTransaction<AuditRepository>();

        await users.DisableAsync(userId, cancellationToken);
        await audit.WriteRawAsync("UserDisabled", userId, cancellationToken);

        uow.Commit();
    }
}
```

Register these helpers as scoped because they capture the scoped `Db4NetDatabase`.

```csharp
services.AddScoped<DiUnitOfWorkFactory>();
services.AddScoped<UserService>();
```

For even smaller business services, use a transaction runner and keep repository creation inside the delegate:

```csharp
public sealed class TransactionRunner
{
    private readonly Db4NetDatabase _db;
    private readonly IServiceProvider _services;

    public TransactionRunner(Db4NetDatabase db, IServiceProvider services)
    {
        _db = db;
        _services = services;
    }

    public async Task ExecuteAsync(
        Func<DiUnitOfWork, CancellationToken, Task> work,
        CancellationToken cancellationToken = default)
    {
        using var uow = new DiUnitOfWork(_db, _services);

        await work(uow, cancellationToken);

        uow.Commit();
    }
}
```

Register `TransactionRunner` as scoped as well. This avoids a large repository factory while still making the transaction boundary explicit.

## Request-Scoped DI

In request-scoped applications that use `Microsoft.Extensions.DependencyInjection`, register the connection and `Db4NetDatabase` as scoped services. Register the repository factory as singleton because it does not capture a connection.

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

services.AddSingleton<RepositoryFactory>();
services.AddScoped<UserRepository>();
services.AddScoped<UserService>();
```

Simple controllers can inject repositories directly. Use services when the action is a business use case or needs a transaction across multiple repositories.

```csharp
public sealed class UsersController
{
    private readonly UserService _users;

    public UsersController(UserService users)
    {
        _users = users;
    }

    public Task Disable(long id, CancellationToken cancellationToken)
    {
        return _users.DisableUserAsync(id, cancellationToken);
    }
}
```

## Singleton and Background Jobs

Singleton services and background jobs must not capture scoped repositories or a scoped `Db4NetDatabase`. Inject `IServiceScopeFactory`, create a scope per operation, and resolve the scoped service inside that scope.

```csharp
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

public sealed class UserCleanupJob : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;

    public UserCleanupJob(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(5));

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            using var scope = _scopeFactory.CreateScope();

            var users = scope.ServiceProvider.GetRequiredService<UserService>();
            await users.DisableUserAsync(1, stoppingToken);
        }
    }
}
```

For ASP.NET Web API 2 on OWIN, use the equivalent scope factory from your DI container. The rule is the same: create a scope per background operation and resolve scoped repositories or services from that scope.

## No DI Program

For console tools, game servers, scripts, or other programs without DI, keep a small application-side session factory. It owns the connection lifetime and returns a `Db4NetDatabase` bound to an opened connection.

```csharp
public sealed class Db4NetSessionFactory
{
    private readonly Func<DbConnection> _createConnection;
    private readonly Db4NetOptions _options;

    public Db4NetSessionFactory(Func<DbConnection> createConnection, Db4NetOptions options)
    {
        _createConnection = createConnection;
        _options = options;
    }

    public async Task<Db4NetSession> OpenAsync(CancellationToken cancellationToken = default)
    {
        var connection = _createConnection();
        await connection.OpenAsync(cancellationToken);

        return new Db4NetSession(connection, connection.UseDb4Net(_options));
    }
}

public sealed class Db4NetSession : IAsyncDisposable
{
    private readonly DbConnection _connection;

    public Db4NetSession(DbConnection connection, Db4NetDatabase db)
    {
        _connection = connection;
        Db = db;
    }

    public Db4NetDatabase Db { get; }

    public ValueTask DisposeAsync()
    {
        return _connection.DisposeAsync();
    }
}
```

Use the session factory at the operation boundary:

```csharp
var sessions = new Db4NetSessionFactory(
    () => new SqliteConnection("Data Source=app.db"),
    Db4NetOptions.Sqlite);

await using var session = await sessions.OpenAsync(cancellationToken);
using var tx = session.Db.BeginTransaction();

var repositories = new RepositoryFactory();
var users = repositories.Users(tx.Database);
var audit = repositories.Audit(tx.Database);

await users.DisableAsync(1, cancellationToken);
await audit.WriteAsync("UserDisabled", 1, cancellationToken);

tx.Commit();
```

`Db4NetSessionFactory` is an application helper, not a required Db4Net abstraction. Keep it local until your application has a stable need for it.

## Choosing a Pattern

| Scenario | Recommended pattern |
| --- | --- |
| Request-scoped web action | Inject repository or service |
| Multiple repositories in one use case | Service controls transaction and creates repositories from `tx.Database` |
| Large DI application with many repositories | Use focused units of work or DI-based repository creation with `ActivatorUtilities` |
| Singleton or background worker in a DI application | Use `IServiceScopeFactory` per operation |
| No DI application | Use an application-side `Db4NetSessionFactory` |
| Raw Dapper inside a Db4Net-owned transaction | Use `tx.Connection` and `tx.DbTransaction` |
| Externally owned transaction | Pass it to Dapper and bind Db4Net with `WithTransaction(...)` |

Do not register scoped `DbConnection`, connection-bound `Db4NetDatabase`, or repositories that capture them as singletons.
