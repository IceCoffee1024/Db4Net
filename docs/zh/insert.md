# 插入

使用 `InsertInto<T>()` 插入显式映射属性：

```csharp
var affected = await connection
    .UseDb4Net(Db4NetOptions.Sqlite)
    .InsertInto<User>()
    .Value(u => u.Id, 3)
    .Value(u => u.Name, "Charlie")
    .ExecuteAsync();
```

`Value(...)` 接收 CLR 成员选择器，Db4Net 会根据映射生成列名，并将值作为 Dapper 参数传递。

## 从实体取值

如果要把映射对象作为值来源，可以使用 `Values(entity)`：

```csharp
var affected = db.InsertInto<User>()
    .Values(user)
    .Execute();
```

带有 `[DatabaseGenerated(DatabaseGeneratedOption.Identity)]` 或 `[DatabaseGenerated(DatabaseGeneratedOption.Computed)]` 的映射属性会被 `Values(entity)`、`Insert(entity)`、`InsertMany(users)` 和冲突插入值跳过。

## 返回数据库生成键

常规单行插入需要返回插入键时，使用 `ExecuteReturnKey<TResult>()`：

```csharp
var id = await db.Insert(user)
    .ExecuteReturnKeyAsync<long>();

var stagedId = db.Insert(user, table: "users_staging")
    .ExecuteReturnKey<long>(u => u.Id);
```

如果需要 SQL 风格 builder 并检查命令，可以使用 `ReturnKey(...)`：

```csharp
var command = db.InsertInto<User>()
    .Values(user)
    .ReturnKey(u => u.Id)
    .ToCommand();

var id = db.InsertInto<User>()
    .Values(user)
    .ReturnKey(u => u.Id)
    .Execute<long>();
```

`ExecuteReturnKey<TResult>()` 默认使用模型唯一的映射键。模型有多个 `[Key]` 属性时，需要显式传入键选择器。选择器必须指向映射键列。

生成键回读只适用于常规单行 `InsertInto<T>()` 和 `Insert(entity)`。`InsertMany(...)`、`InsertOrIgnore(...)` 和 `InsertOrUpdate(...)` 仍返回影响行数。方言注意事项见[生成键回读](./dialects.md#生成键回读)，例如 MySQL auto-increment identity 语义、SQLite `RETURNING` 版本要求，以及 SQL Server 触发器限制。

## 检查 SQL

命令构建器支持 `ToCommand()`：

```csharp
var command = Db4NetDatabase
    .Create(Db4NetOptions.SqlServer)
    .InsertInto<User>()
    .Value(u => u.Id, 3)
    .Value(u => u.Name, "Charlie")
    .ToCommand();
```
