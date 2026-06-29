# 完整示例

这一页用一个实际流程串起 Db4Net 的连接、写入、查询、事务，以及什么时候应该回到 Dapper 原生 SQL。

## 模型

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

`[Key]` 标识实体便捷方法使用的主键，例如 `Update(user)` 和 `Delete(user)`。`[DatabaseGenerated(DatabaseGeneratedOption.Identity)]` 表示该列由数据库生成：实体插入时会跳过该列，并且在调用 `ExecuteReturnKey<TResult>()` 时使用对应数据库的生成键读取方式。

## 创建 Facade

```csharp
using Db4Net;
using Microsoft.Data.Sqlite;

await using var connection = new SqliteConnection(connectionString);
await connection.OpenAsync();

var db = connection.UseDb4Net(Db4NetOptions.Sqlite);
```

`UseDb4Net(...)` 会把 Db4Net 绑定到 Dapper 使用的同一个 `IDbConnection`。Db4Net 负责构建参数化 SQL，终端方法仍通过 Dapper 执行。

## 插入并读取生成键

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

生成键读取只建议用于单行插入。`InsertMany(...)`、`InsertOrIgnore(...)` 和 `InsertOrUpdate(...)` 仍返回受影响行数。

## 查询记录

```csharp
var user = await db
    .SelectFrom<User>()
    .Where(u => u.Id, Op.Eq, userId)
    .QuerySingleOrDefaultAsync();
```

需要完整实体行时使用 `SelectFrom<T>()`。只需要部分映射列时使用 `SelectFrom<T>(...)`。

## 判断存在与统计数量

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

只判断是否存在时，优先使用 `SelectExistsFrom<T>()`，不要先 count 再和 0 比较。

## 查询分页数据和总数

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

`QueryPage(...)` 会复用同一张表和同一组筛选条件，让分页行查询和总数查询保持一致。它内部执行两条命令，所以它的价值是便捷性和一致性，不是单 SQL 优化。

## 在事务中更新

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

`ExecuteInTransaction(...)` 和 `ExecuteInTransactionAsync(...)` 会在委托成功时提交，在抛出异常时回滚。Db4Net 仍然不会跟踪实体，也不会提供 `SaveChanges()`。

## 局部更新使用 SQL 风格 Builder

```csharp
var affected = await db
    .Update<User>()
    .Set(u => u.Name, "Alice Final")
    .Where(u => u.Id, Op.Eq, userId)
    .ExecuteAsync();
```

常规完整实体命令优先使用实体便捷方法。需要局部字段、复杂条件或显式全表操作时，使用 SQL 风格 builder。

## 复杂 SQL 交给 Dapper

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

Db4Net 有意不覆盖 join、CTE、窗口函数、数据库专有 hint 或完整 SQL 表达式构造。这类查询继续使用 Dapper 原生 SQL。

## 推荐实践

- 常见单实体命令优先使用 `Insert(user)`、`Update(user)`、`Delete(user)`。
- 希望命令读起来更接近 SQL 时，使用 `InsertInto<T>()`、`Update<T>()`、`DeleteFrom<T>()`。
- 只判断存在时使用 `SelectExistsFrom<T>()`；确实需要数量时使用 `SelectCountFrom<T>()`。
- UI 分页同时需要行数据和总数时，使用 `QueryPage(...)`。
- 标量聚合使用 `SelectAggregateFrom<T>()`，例如 `Max`、`Min`、`Sum`、`Average`、`CountDistinct`。
- staging 表、归档表、分片表或同模型视图使用 `table:` overload。
- 多个写操作组成一个工作流时使用显式事务。
- join 和数据库专有 SQL 使用 Dapper 原生 SQL。
