# 限制

Db4Net 当前聚焦于安全、SQL 风格的 Dapper 查询和命令构建。查询构建器仍保持轻量：支持类型化表/视图来源和单列 `IN` 子查询筛选，但不是完整 SQL DSL。

已包含的能力包括：

- 类型化 `SELECT`、`INSERT`、`UPDATE` 和 `DELETE` 构建器
- 通过 `SelectExistsFrom<T>()` 和 `SelectCountFrom<T>()` 提供的存在性与计数查询构建器，并使用明确的 `ExecuteScalar(...)` / `ExecuteScalarAsync(...)` 终结方法
- 通过 `SelectAggregateFrom<T>()` 提供的类型化标量聚合查询构建器
- 通过 `QueryPage(...)` 和 `QueryPageAsync(...)` 提供的分页 SELECT 终结方法
- 动态 CLR 属性名投影并进行模型验证
- 表名和视图覆盖
- 单实体与多实体命令便捷方法
- 常规单行插入键回读
- 冲突感知插入
- `Where`、`OrWhere`、`WhereBetween`、`Op.NotLike`、`Op.NotIn`、单列 `WhereIn` 子查询、`WhereGroup`、`OrWhereGroup`、`OrderBy`、`OrderByDescending`、`Limit`、`Offset` 和 `Page`
- 同步与异步 Dapper 风格查询终结方法
- 已有事务传递、轻量事务作用域、命令超时、命令类型和异步取消令牌

当前有意不包含：

- Join
- provider 原生 copy/import API、集合式同步和优化批处理
- `InsertMany(...)` 或冲突插入的生成键回读
- 自动刷新所有数据库生成值或 computed 值
- MySQL `InsertOrUpdate(...)` 对 MySQL 5.7、MySQL 8.0.0-8.0.18 或 MariaDB 的兼容；生成的 MySQL upsert SQL 跟随 MySQL 8.0.19+ row-alias 语法
- MySQL 对非 auto-increment 的 trigger/default/expression 生成键进行回读
- SQL Server 对要求 `OUTPUT ... INTO` 的启用触发器表进行生成键回读
- SQLite 3.35 以前运行时上的生成键回读
- 变更跟踪、dirty checking、`SaveChanges()` 或 Unit of Work 行为
- 关系加载、级联持久化、延迟加载或代理生成
- 迁移或 schema 管理
- 自动并发令牌
- 完整谓词表达式翻译，例如 `Where(u => u.Id == 1)`
- 完整 LINQ Provider 行为

复杂 join 或数据库特定 SQL，建议直接使用 Dapper 原生 SQL，或通过数据库视图暴露稳定读取模型。
