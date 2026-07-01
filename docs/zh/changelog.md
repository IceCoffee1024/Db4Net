# 变更日志

## 0.1.0-alpha.3 - 2026-07-01

### 新增

- `Db4NetTransaction.Connection` 和 `Db4NetTransaction.DbTransaction`，用于让 Dapper 原生 SQL 参与 Db4Net 创建的事务。

### 变更

- 应用模式文档现在补充大型 DI 应用、transaction runner、通过 `ActivatorUtilities` 创建仓储，以及 Dapper 原生 SQL 共用事务的建议。

## 0.1.0-alpha.2 - 2026-07-01

### 新增

- 抛异常语义的 SELECT 终结方法：`QueryFirst`、`QueryFirstAsync`、`QuerySingle` 和 `QuerySingleAsync`。

### 变更

- 文档现在说明 `FindByIdAsync` 与 `GetByIdAsync` 的仓储命名区别，以及 SELECT 终结方法语义。

## 0.1.0-alpha.1 - 2026-06-29

### 新增

- 贴近 SQL 顺序的类型化 `SELECT`、`INSERT`、`UPDATE` 和 `DELETE` builder。
- existence、count、paging、scalar aggregate 和 subquery filter API。
- 单实体和多实体 command 便捷方法。
- 面向 SQL Server、SQLite、PostgreSQL 和 MySQL 的冲突插入。
- 显式筛选分组和 CLR 属性名字符串 API。
- 轻量事务 scope 和 Dapper 执行选项。
- 包含 XML 文档和 symbols 的 `net8.0` / `netstandard2.0` 包资产。
- 英文和简体中文文档。

### 变更

- 公共 API 使用贴近 SQL 的命名，并刻意避免 ORM 风格的 `Save`、`SaveChanges`、`Merge`、`Upsert` 和 `Bulk` 名称。
- 模型绑定后的字符串字段名表示 CLR 属性名。
- `UPDATE` 和 `DELETE` 默认要求 `WHERE` 条件，除非显式调用 `AllowAllRows()`。
- 实体驱动 update 会跳过数据库生成的非 key 列。
- 分页验证会拒绝无效的 `Offset(...)` 和 SQL Server paging 组合。

### 已知限制

- 不支持 join；复杂 provider-specific SQL 请使用数据库视图或原始 Dapper SQL。
- 不提供 LINQ provider 或完整谓词表达式翻译。
- 不提供 change tracking、关系加载、migration、自动并发 token 或 unit-of-work 行为。
- 多实体便捷方法不是 provider 原生 bulk import/copy API。
- 生成键回读不适用于 `InsertMany`、冲突插入或完整 generated/computed value refresh。

::: tip 提示
根目录 [`CHANGELOG.md`](https://github.com/IceCoffee1024/Db4Net/blob/main/CHANGELOG.md) 仍是用于发布准备和 NuGet 打包的权威详细变更日志。
:::
