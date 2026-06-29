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

## 连接作用域

对于使用 `Microsoft.Extensions.DependencyInjection` 的请求级应用，推荐把连接和 `Db4NetDatabase` 注册为 scoped service。不要把它们注册成 singleton。

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

在 scoped factory 中打开连接不会和 Dapper 冲突。Dapper 会使用已经打开的连接，并保持它打开；请求 scope 结束时 DI 容器会释放连接。

## 多数据库

当不同仓储固定访问不同数据库时，为每个数据库注册 keyed scoped service。

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

不使用 DI 的应用，可以在应用、请求、service 或 unit-of-work 边界创建连接和 Db4Net facade，然后把同一个 facade 传给该作用域内的仓储。

```csharp
await using var connection = connectionFactory.CreateConnection();
await connection.OpenAsync(cancellationToken);

var db = connection.UseDb4Net(Db4NetOptions.Sqlite);
var users = new UserRepository(db);

var user = await users.FindByIdAsync(1, cancellationToken);
```

不要把捕获了连接绑定 `Db4NetDatabase` 的仓储注册成 singleton。Db4Net facade 很轻量，它的生命周期应该跟随绑定的连接或事务。

## 事务

多个仓储调用需要原子性时，把事务放在 service 或 unit-of-work 层，用事务绑定的 facade 创建仓储。

如果仓储是由 DI 使用请求级 `Db4NetDatabase` 创建的，事务内的特定操作可以在 service 层直接使用 `tx.Database`，也可以在事务委托内部用 `tx.Database` 创建短生命周期的仓储实例。

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

`ExecuteInTransactionAsync(...)` 会在委托成功时提交，在抛出异常时回滚。Db4Net 仍然不会跟踪实体，也不会提供 `SaveChanges()`。

## 建议

- Db4Net builder 组合留在仓储内部。
- 仓储方法返回领域模型、DTO、标量值、`PagedResult<T>` 或受影响行数。
- 不要从仓储方法返回 `SelectQueryBuilder<T>` 或命令 builder。
- 稳定的应用用例用仓储方法承载；小工具、测试或脚本里如果额外抽象没有价值，可以直接使用 Db4Net。
- 复杂 join、CTE、窗口函数和数据库专有 SQL 继续放在同一数据访问层里的 Dapper 原生 SQL 中。
