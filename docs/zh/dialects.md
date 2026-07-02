# 方言

Db4Net 当前支持：

- SQL Server
- SQLite
- PostgreSQL
- MySQL

配置方言后，Db4Net 会处理标识符引用、分页语法、冲突插入渲染，以及常规单行插入返回生成键的 SQL。

```csharp
var db = connection.UseDb4Net(Db4NetOptions.SqlServer);
```

## 分页语法

- SQL Server: `OFFSET ... ROWS FETCH NEXT ... ROWS ONLY`
- SQLite: `LIMIT ... OFFSET ...`
- PostgreSQL: `LIMIT ... OFFSET ...`
- MySQL: `LIMIT ... OFFSET ...`

`Offset(...)` 必须与 `Limit(...)` 配套使用。SQL Server 分页还要求至少调用一次 `OrderBy(...)`，因为没有 `ORDER BY` 时 `OFFSET` / `FETCH` 是无效 SQL。

## 冲突插入语法

SQLite 和 PostgreSQL 渲染原生 `ON CONFLICT` 语法。

MySQL 对 `InsertOrIgnore(...)` 渲染 `INSERT IGNORE`。`InsertOrUpdate(...)` 跟随 MySQL 8.0.19+ 的 row-alias upsert 语法：

```sql
INSERT INTO `Users` (`Email`, `Name`) VALUES (@Email, @Name) AS _new
ON DUPLICATE KEY UPDATE `Name` = _new.`Name`
```

这跟随 MySQL 8+ 对已弃用 `VALUES(col)` 引用的替代写法，但生成的 `InsertOrUpdate(...)` SQL 不兼容 MySQL 5.7、MySQL 8.0.0-8.0.18 或 MariaDB。显式 `OnConflict(...)` 选择器表达的是 Db4Net 期望的冲突列，但 MySQL 自身会对任意主键或唯一键冲突应用 duplicate handling。`INSERT IGNORE` 也可能按 MySQL 规则把部分数据错误降级为 warning；请只在接受 MySQL ignore 语义时使用它。

SQL Server 会为冲突感知插入渲染 `MERGE ... WITH (HOLDLOCK)` 命令。它不是 provider 原生导入/copy API，也不是优化批量导入或集合式同步抽象。

## 生成键回读

常规单行插入的生成键终结方法会渲染不同数据库对应的标量 SQL：

- SQL Server: `OUTPUT INSERTED.[Id]`
- SQLite: `RETURNING "Id"`
- PostgreSQL: `RETURNING "Id"`
- MySQL: auto-increment identity 键使用 `SELECT LAST_INSERT_ID()`；如果所选键值已显式包含在插入值中，则使用 `SELECT @p0`

这个能力只适用于常规单行插入。多实体插入和冲突插入仍返回影响行数。

方言注意事项：

- SQLite 需要运行时 SQLite 3.35 或更高版本才支持 `RETURNING`。
- SQL Server 直接 `OUTPUT INSERTED...` 适用于普通目标表；带启用触发器的表可能要求 `OUTPUT ... INTO` 模式，Db4Net 当前不生成该形式。
- MySQL 生成键回读面向 auto-increment identity；由 default、trigger 或表达式生成但不是自增 identity 的键，不能通过 `LAST_INSERT_ID()` 返回。
