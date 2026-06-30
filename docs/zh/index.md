---
layout: home

hero:
  name: "Db4Net"
  text: "面向 Dapper 的安全 SQL 风格构建器"
  tagline: "轻量、参数化、SQL 风格，不接管 Dapper 的执行与对象映射。"
  actions:
    - theme: brand
      text: 快速开始
      link: /zh/getting-started
    - theme: alt
      text: 查看查询示例
      link: /zh/select

features:
  - title: SQL 风格 API
    details: SelectFrom<T>()、InsertInto<T>()、Update<T>()、DeleteFrom<T>() 保持语句顺序清晰。
  - title: 安全参数化
    details: 标识符由方言验证并引用，值始终通过 Dapper 参数传递。
  - title: 不做 ORM
    details: 不提供跟踪、关系加载、迁移或 SaveChanges()，复杂 SQL 仍交给 Dapper。
  - title: 类型化映射
    details: Table、Column、Key、NotMapped 等标准属性驱动表名、列名和键元数据。
  - title: 实体便捷方法
    details: 单实体、多实体和冲突感知命令便捷方法复用同一套已验证 builder。
  - title: 轻量事务作用域
    details: 可以传入已有事务，也可以用 BeginTransaction() 组合多条显式操作。
  - title: 多方言渲染
    details: 支持 SQL Server、SQLite、PostgreSQL 和 MySQL 的标识符引用、分页和冲突 SQL。
---
