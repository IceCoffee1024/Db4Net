# 方言

Db4Net 当前支持：

- SQL Server
- SQLite
- PostgreSQL
- MySQL

配置方言后，Db4Net 会处理标识符引用、分页语法和冲突插入渲染。

```csharp
var db = connection.UseDb4Net(Db4NetOptions.SqlServer);
```

## 分页语法

- SQL Server: `OFFSET ... ROWS FETCH NEXT ... ROWS ONLY`
- SQLite: `LIMIT ... OFFSET ...`
- PostgreSQL: `LIMIT ... OFFSET ...`
- MySQL: `LIMIT ... OFFSET ...`

## 冲突插入语法

SQLite 和 PostgreSQL 渲染原生 `ON CONFLICT` 语法。

MySQL 渲染 `ON DUPLICATE KEY UPDATE`。显式 `OnConflict(...)` 选择器表达的是 Db4Net 期望的冲突列，但 MySQL 自身会对任意主键或唯一键冲突应用 duplicate handling。

SQL Server 渲染方言特定的冲突感知命令。它不是 provider 原生导入/copy API，也不是优化批量导入或集合式同步抽象。
