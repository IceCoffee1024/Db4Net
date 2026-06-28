# 限制

Db4Net 当前聚焦于安全、SQL 风格、单表优先的 Dapper 查询和命令构建。

已包含的能力包括：

- 类型化 `SELECT` 构建器
- 类型化 `INSERT`、`UPDATE`、`DELETE` 构建器
- 表名和视图覆盖
- 单实体与多实体命令便捷方法
- 冲突感知插入
- 动态 CLR 属性名投影并进行模型验证
- `Where`、`OrWhere`、`WhereGroup`、`OrWhereGroup`
- `OrderBy`、`Limit`、`Offset`、`Page`
- `Value`、`Set`、`Execute`、`ExecuteAsync`
- 同步与异步 Dapper 风格查询终结方法
- 已有事务传递、轻量事务作用域、命令超时、命令类型和异步取消令牌

当前有意不包含：

- Join
- provider 原生 copy/import API、集合式同步和优化批处理
- 变更跟踪、dirty checking、`SaveChanges()` 或 Unit of Work 行为
- 关系加载、级联持久化、延迟加载或代理生成
- 迁移或 schema 管理
- 自动并发令牌
- 完整谓词表达式翻译，例如 `Where(u => u.Id == 1)`
- 完整 LINQ Provider 行为

复杂 join 或数据库特定 SQL，建议直接使用 Dapper 原生 SQL，或通过数据库视图暴露稳定读取模型。
