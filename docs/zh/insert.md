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
