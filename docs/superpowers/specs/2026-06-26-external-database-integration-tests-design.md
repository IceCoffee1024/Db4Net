# 外部数据库集成测试设计

## 背景

Db4Net 目前已有 SQLite 内存集成测试和四类方言渲染测试。PostgreSQL、MySQL、SQL Server 方言已经覆盖 SQL 字符串形态，但还缺少真实数据库执行验证，尤其是 Dapper 对参数、分页语法、标识符引用和 `[Column]` 映射的实际兼容性。

## 目标

新增可选真实数据库集成测试，覆盖 PostgreSQL、MySQL、SQL Server 三类外部数据库。默认 `dotnet test` 不要求本机安装或启动这些数据库；只有提供对应连接字符串时才执行真实数据库验证。

## 启用方式

测试通过环境变量启用：

- `DB4NET_POSTGRESQL_CONNECTION_STRING`
- `DB4NET_MYSQL_CONNECTION_STRING`
- `DB4NET_SQLSERVER_CONNECTION_STRING`

未设置连接字符串时，测试应动态跳过，而不是失败。

## 测试范围

每个数据库至少覆盖三类行为：

- 基础查询：创建临时测试表，插入数据，使用 `SelectFrom<T>(table).Where(...)` 查询，验证参数化 SQL 能通过 Dapper 正常执行。
- 字段映射：创建包含 `display_name` 的表，使用 `[Column("display_name")]` 和 `Select("DisplayName").From<T>(table)` 查询，验证 Db4Net 生成别名后 Dapper 能映射回 CLR 属性。
- 分页排序：插入多行数据，使用 `OrderBy(...).Offset(...).Limit(...)` 查询，验证真实数据库接受对应方言分页语法并返回正确顺序。

## 数据隔离

每个测试使用随机表名，格式为 `db4net_<provider>_<purpose>_<guid>`。表名只由测试代码生成，不接受外部输入。测试用 `try/finally` 清理表，避免在失败时留下持久对象。

## 依赖

测试项目新增真实数据库客户端依赖：

- PostgreSQL 使用 `Npgsql`
- MySQL 使用 `MySqlConnector`
- SQL Server 使用 `Microsoft.Data.SqlClient`
- 动态跳过使用 `Xunit.SkippableFact`

生产项目不新增依赖。

## 非目标

- 不引入 Testcontainers 或 Docker orchestration。
- 不在默认 CI 中强制启动外部数据库。
- 不测试数据库服务安装、建库和账号权限配置。
- 不改变 Db4Net 生产 API。

## 验收标准

- 未设置外部数据库连接字符串时，`dotnet test` 通过，并显示相关测试被跳过。
- 设置任一连接字符串时，对应数据库的真实集成测试会创建表、执行 Db4Net+Dapper 查询、断言结果并清理表。
- PostgreSQL、MySQL、SQL Server 三者的 quoting、parameter execution、mapped column alias、paging 都有真实执行覆盖。
