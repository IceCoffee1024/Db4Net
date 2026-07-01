# 仓储模式

在需要仓储层时，Db4Net 可以作为仓储内部的 SQL builder 和 Dapper 执行入口使用。

仓储层应该隐藏 Db4Net builder。对外暴露 `FindByIdAsync`、`EmailExistsAsync`、`FindActivePageAsync` 这类业务语义方法；把 `SelectFrom<T>()`、`Query*` 和 `Execute*` 留在数据访问层内部。

## 仓储类

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

这种形态让仓储的 API 边界保持清晰：调用方看到的是仓储方法和返回类型，而不是 Db4Net builder 类型。

## 方法命名

仓储方法名表达数据访问意图，service 方法名表达业务用例。

常见仓储命名：

- `FindByIdAsync(...)`：返回一条记录；不存在时返回 `null`。
- `GetByIdAsync(...)`：期望记录必须存在；可以使用 `QuerySingleAsync()`，或在不存在时主动抛异常。
- `ListBy...Async(...)`：按简单条件返回列表。
- `SearchAsync(...)`：使用关键词、可选过滤、排序等较复杂条件。
- `GetPageAsync(...)`：返回 `PagedResult<T>` 或其他分页 DTO。
- `ExistsBy...Async(...)`：返回是否存在。
- `CountAsync(...)`：返回匹配行数。
- `AddAsync(...)`、`UpdateAsync(...)`、`DeleteAsync(...)`：执行数据变更。

Db4Net 的 `Query*`、`Execute*` 这类执行命名建议留在仓储内部。Service 方法应该描述业务动作，例如 `RegisterUserAsync(...)`、`DisableUserAsync(...)` 或 `ChangeEmailAsync(...)`。

## 连接作用域

持有 `Db4NetDatabase` 的仓储应该跟随该 facade 绑定的连接或事务生命周期。对于使用 `Microsoft.Extensions.DependencyInjection` 的请求级应用，推荐把连接、`Db4NetDatabase` 和仓储都注册为 scoped service。完整 DI 配置见[应用模式](./application-patterns.md#请求级-di)。

在 scoped factory 中打开连接不会和 Dapper 冲突。Dapper 会使用已经打开的连接，并保持它打开；请求 scope 结束时 DI 容器会释放连接。

## 多数据库

当不同仓储固定访问不同数据库时，为每个数据库注册 keyed scoped service。`AddKeyedScoped` 和 `GetRequiredKeyedService` 是 .NET 8 `Microsoft.Extensions.DependencyInjection` API；如果你的 DI 容器不支持 keyed services，请使用应用层 factory，或使用该容器自己的 named/keyed registration 能力。

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

把每个仓储绑定到它负责的数据库：

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

固定的数据库边界，例如 `Main` 和 `Audit`，适合使用 keyed scoped services。如果数据库需要按租户或请求数据在运行时选择，请把选择逻辑放进应用层 factory，而不是把仓储固定绑定到某个 keyed database。

Db4Net 的事务作用域是单连接作用域。不要假设一次 `ExecuteInTransaction(...)` 可以让跨多个数据库的操作具备原子性。

不使用 DI 的应用，可以在应用、请求、service 或 unit-of-work 边界创建连接和 Db4Net facade，然后把同一个 facade 传给该作用域内的仓储。Session factory 示例见[应用模式](./application-patterns.md#无-di-程序)。

不要把捕获了连接绑定 `Db4NetDatabase` 的仓储注册成 singleton。Db4Net facade 很轻量，它的生命周期应该跟随绑定的连接或事务。

## Dapper 原生 SQL

Db4Net 不包装原生 SQL。复杂 join、CTE、窗口函数和数据库专有 SQL 继续放在 Dapper 中，并和 Db4Net 使用同一个 scoped connection。

```csharp
using System.Data.Common;
using Dapper;
using Db4Net;

public sealed class ReportRepository
{
    private readonly Db4NetDatabase _db;
    private readonly DbConnection _connection;

    public ReportRepository(Db4NetDatabase db, DbConnection connection)
    {
        _db = db;
        _connection = connection;
    }

    public Task<IEnumerable<UserActivityRow>> GetUserActivityAsync()
    {
        return _connection.QueryAsync<UserActivityRow>(
            """
            SELECT u.Id, u.Name, COUNT(a.Id) AS ActivityCount
            FROM Users u
            LEFT JOIN Activities a ON a.UserId = u.Id
            GROUP BY u.Id, u.Name
            ORDER BY ActivityCount DESC
            """);
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

如果使用 keyed database 注册，请注入或解析匹配的 keyed `DbConnection` 和 keyed `Db4NetDatabase`。

当 Dapper 原生 SQL 和 Db4Net 命令需要共用同一个事务时，请自行创建事务，把它传给 Dapper，并通过 `WithTransaction(...)` 绑定到 Db4Net。

```csharp
using var transaction = _connection.BeginTransaction();

try
{
    var txDb = _db.WithTransaction(transaction);

    await txDb
        .Update<User>()
        .Set(u => u.Name, "Alice")
        .Where(u => u.Id, Op.Eq, userId)
        .ExecuteAsync(cancellationToken: cancellationToken);

    await _connection.ExecuteAsync(
        """
        INSERT INTO AuditLogs (EventName, EntityId)
        VALUES (@EventName, @EntityId)
        """,
        new { EventName = "UserRenamed", EntityId = userId },
        transaction);

    transaction.Commit();
}
catch
{
    transaction.Rollback();
    throw;
}
```

## 事务

多个仓储调用需要原子性时，把事务放在 service 或 unit-of-work 层，用事务绑定的 facade 创建仓储。

如果仓储是由 DI 使用请求级 `Db4NetDatabase` 创建的，事务内的特定操作可以在 service 层直接使用 `tx.Database`，也可以在事务委托内部用 `tx.Database` 创建短生命周期的仓储实例。

`ExecuteInTransactionAsync(...)` 会在委托成功时提交，在抛出异常时回滚。Db4Net 仍然不会跟踪实体，也不会提供 `SaveChanges()`。Service 层事务示例见[应用模式](./application-patterns.md#service-事务边界)，可复用的应用侧辅助对象见[业务侧工作单元](./application-patterns.md#业务侧工作单元)。

## 建议

- Db4Net builder 组合留在仓储内部。
- 仓储方法返回领域模型、DTO、标量值、`PagedResult<T>` 或受影响行数。
- 不要从仓储方法返回 `SelectQueryBuilder<T>` 或命令 builder。
- 稳定的应用用例用仓储方法承载；小工具、测试或脚本里如果额外抽象没有价值，可以直接使用 Db4Net。
- 复杂 join、CTE、窗口函数和数据库专有 SQL 继续放在同一数据访问层里的 Dapper 原生 SQL 中。
