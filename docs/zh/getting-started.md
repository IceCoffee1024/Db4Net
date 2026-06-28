# 快速开始

Db4Net 是面向 Dapper 的轻量级流式 SQL 构建器。它专注于安全、参数化的单表查询和命令，同时让 Dapper 继续负责执行与 `SELECT` 结果物化。

Db4Net 不是 ORM，也不是 LINQ Provider。它的 API 有意保持 SQL 风格，例如 `SelectFrom<T>()`、`InsertInto<T>()`、`Update<T>()` 和 `DeleteFrom<T>()`。

## 安装

```bash
dotnet add package Db4Net --prerelease
```

NuGet 包提供 `net8.0` 和 `netstandard2.0` 两组目标框架资产。

## 第一个查询

```csharp
using Db4Net;

var user = await connection
    .UseDb4Net(Db4NetOptions.Sqlite)
    .SelectFrom<User>()
    .Where(u => u.Id, Op.Eq, 1)
    .QuerySingleOrDefaultAsync();
```

上面的调用会生成参数化 SQL，并通过 Dapper 执行。

::: tip 提示
如果只想检查生成的 SQL，而不需要真实连接执行命令，可以使用 `Db4NetDatabase.Create(...)`。
:::

```csharp
var command = Db4NetDatabase
    .Create(Db4NetOptions.SqlServer)
    .Select<User>(u => u.Id, u => u.Name)
    .Where(u => u.Id, Op.Eq, 1)
    .ToCommand();

Console.WriteLine(command.Sql);
```

```sql
SELECT [Id], [Name] FROM [Users] WHERE [Id] = @p0
```

## 适用范围

当前 alpha 版本聚焦于 SQL Server、SQLite、PostgreSQL 和 MySQL 的类型化单表 `SELECT`、`INSERT`、`UPDATE`、`DELETE`，以及实体便捷方法、多实体便捷方法、冲突插入、表名覆盖、筛选分组、排序分页和执行选项。

复杂 join、数据库特定 SQL 或高度优化的导入/同步场景，建议直接使用 Dapper 原生 SQL，或通过数据库视图暴露稳定的读取模型。
