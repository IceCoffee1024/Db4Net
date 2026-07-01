# 应用模式

Db4Net 专注于 query 和 command builder。仓储、service、后台任务和 unit-of-work factory 这类应用架构应该留在你的应用代码里。

这一页展示几个实际组合方式：仓储方法、service 层事务、singleton/background worker，以及完全不使用依赖注入的程序。

## 职责划分

默认可以按下面方式拆分：

- Repository：封装 `FindByIdAsync`、`ExistsByEmailAsync`、`DisableAsync`、报表查询等数据访问方法。Db4Net builder 留在仓储内部。
- Service：负责业务用例、校验、跨仓储编排和事务边界。
- Unit of Work：可选的应用层辅助对象，用于开启事务并创建事务绑定的仓储。

不要让仓储猜测是否存在 ambient transaction。仓储需要进入事务时，用事务绑定的 `Db4NetDatabase` 创建它。

基础仓储形态和仓储单独使用示例见[仓储模式](./repository-pattern.md)。

## 共享仓储

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

## Service 事务边界

多个仓储调用必须原子化时，把事务放在 service 层，并用 `tx.Database` 创建短生命周期仓储。

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

`BeginTransaction()` 要求连接已经打开。如果要从 scoped `Db4NetDatabase` 直接调用它，请在注册 scoped connection 时打开连接，或者使用显式的 session/unit-of-work factory，在事务入口先打开连接。

## 业务侧工作单元

当多个 service 都重复同样的事务创建和仓储创建代码时，可以在应用侧封装一个 unit of work。它不是 Db4Net 的 `SaveChanges()` 抽象，只是一个很小的对象，用来拥有一次事务，以及绑定到这次事务的仓储。

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

这样 service 可以依赖应用侧 unit-of-work factory，而不是手动创建每个事务绑定的仓储。

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

`AppUnitOfWorkFactory` 捕获 scoped `Db4NetDatabase`，因此应注册为 scoped。

```csharp
services.AddSingleton<RepositoryFactory>();
services.AddScoped<AppUnitOfWorkFactory>();
services.AddScoped<UserService>();
```

这种显式属性方式适合小型、按功能聚焦的工作单元。大型应用里，不建议把系统里的几十上百个仓储都塞进一个全局 `AppUnitOfWork`；更好的做法是按业务区域拆成聚焦的工作单元，或者使用下面的 DI 动态创建方式。如果项目已经使用 `Microsoft.Extensions.DependencyInjection`，下一节的泛型 `Repository<TRepository>()` 通常可以替代 `RepositoryFactory`。

## 大型 DI 应用

如果应用已经使用 `Microsoft.Extensions.DependencyInjection`，单独的 `RepositoryFactory` 就不是必须的。可以把 `IServiceProvider` 封装在基础设施代码里，按需创建事务绑定的仓储，不要把 service provider 暴露给业务 service。

```csharp
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

仓储通常应该是轻量、无状态对象，所以 unit of work 内部一般不需要缓存仓储实例。如果某个仓储有昂贵状态，或者应用确实需要同一次工作单元内复用同一个实例，可以在应用侧 unit-of-work 类里加一个很小的字典缓存。

所有仓储都使用 `Repository<TRepository>()`。需要 Dapper 原生 SQL 的仓储仍然只接收 `Db4NetDatabase`；它们使用 `_db.Connection`，并在需要加入当前事务时显式传入 `transaction: _db.DbTransaction`。这个连接是借用上下文，仓储不要 close、dispose 或 open。

```csharp
public sealed class AuditRepository
{
    private readonly Db4NetDatabase _db;

    public AuditRepository(Db4NetDatabase db)
    {
        _db = db;
    }

    public Task<int> WriteRawAsync(
        string eventName,
        long entityId,
        CancellationToken cancellationToken = default)
    {
        return _db.Connection.ExecuteAsync(
            new CommandDefinition(
                """
                INSERT INTO AuditLogs (EventName, EntityId)
                VALUES (@EventName, @EntityId)
                """,
                new { EventName = eventName, EntityId = entityId },
                transaction: _db.DbTransaction,
                cancellationToken: cancellationToken));
    }
}
```

业务 service 仍然依赖 unit-of-work 抽象，而不是依赖 `IServiceProvider`。

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
        var audit = uow.Repository<AuditRepository>();

        await users.DisableAsync(userId, cancellationToken);
        await audit.WriteRawAsync("UserDisabled", userId, cancellationToken);

        uow.Commit();
    }
}
```

这些辅助对象捕获 scoped `Db4NetDatabase`，因此应注册为 scoped。

```csharp
services.AddScoped<DiUnitOfWorkFactory>();
services.AddScoped<UserService>();
```

如果希望业务 service 更薄，也可以封装一个 transaction runner，在委托里创建仓储：

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

`TransactionRunner` 也应注册为 scoped。这样可以避免一个庞大的仓储工厂，同时仍然让事务边界保持显式。

## 请求级 DI

使用 `Microsoft.Extensions.DependencyInjection` 的请求级应用中，把连接和 `Db4NetDatabase` 注册为 scoped service。`RepositoryFactory` 不捕获连接，可以注册为 singleton。

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

简单 CRUD 的 controller 可以直接注入仓储；当 action 是一个业务用例，或者需要跨多个仓储的事务时，注入 service。

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

## Singleton 和后台任务

Singleton service 和后台任务不要捕获 scoped repository 或 scoped `Db4NetDatabase`。注入 `IServiceScopeFactory`，每次操作创建一个 scope，并在这个 scope 内解析 scoped service。

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

ASP.NET Web API 2 on OWIN 可以使用所选 DI 容器提供的等价 scope factory。规则相同：每次后台操作创建一个 scope，再从这个 scope 里解析 scoped repository 或 service。

## 无 DI 程序

控制台工具、游戏服务端、脚本或其他不使用 DI 的程序，可以在应用侧保留一个很小的 session factory。它负责连接生命周期，并返回绑定到已打开连接的 `Db4NetDatabase`。

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

在操作边界使用 session factory：

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

`Db4NetSessionFactory` 是应用侧辅助对象，不是必须使用的 Db4Net 抽象。先把它留在应用代码里，等需求稳定后再考虑抽成公共组件。

## 如何选择

| 场景 | 推荐模式 |
| --- | --- |
| 请求级 Web action | 注入 repository 或 service |
| 一个用例需要多个仓储 | Service 控制事务，并用 `tx.Database` 创建仓储 |
| 大型 DI 应用，有很多仓储 | 使用聚焦的工作单元，或用 `ActivatorUtilities` 按需创建仓储 |
| DI 应用中的 singleton 或后台任务 | 每次操作使用 `IServiceScopeFactory` 创建 scope |
| 无 DI 应用 | 使用应用侧 `Db4NetSessionFactory` |
| Dapper 原生 SQL 加入当前 Db4Net 上下文 | 使用 `_db.Connection` 或 `tx.Database.Connection`，并显式传入 `transaction: _db.DbTransaction` 或 `tx.Database.DbTransaction` |
| 外部创建并拥有的事务 | 传给 Dapper，并通过 `WithTransaction(...)` 绑定到 Db4Net |

不要把 scoped `DbConnection`、连接绑定的 `Db4NetDatabase` 或捕获它们的仓储注册为 singleton。
