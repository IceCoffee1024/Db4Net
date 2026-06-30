# 测试

运行默认测试套件：

```bash
dotnet test
```

SQLite 集成测试默认使用内存数据库运行。PostgreSQL、MySQL 和 SQL Server 集成测试是可选的，需要通过环境变量或本地 runsettings 启用。

## 本地外部数据库测试

如果存在 `tests/Db4Net.Tests/local.runsettings`，测试项目会在本地运行时自动读取它。你也可以显式传入：

```bash
dotnet test --settings tests/Db4Net.Tests/local.runsettings
```

外部数据库连接字符串包括：

- `DB4NET_POSTGRESQL_CONNECTION_STRING`
- `DB4NET_MYSQL_CONNECTION_STRING`
- `DB4NET_SQLSERVER_CONNECTION_STRING`

运行单个外部 provider 测试类：

```bash
dotnet test --settings tests/Db4Net.Tests/local.runsettings --filter FullyQualifiedName~MySqlIntegrationTests
dotnet test --settings tests/Db4Net.Tests/local.runsettings --filter FullyQualifiedName~PostgreSqlIntegrationTests
dotnet test --settings tests/Db4Net.Tests/local.runsettings --filter FullyQualifiedName~SqlServerIntegrationTests
```

配置的数据库用户需要能够创建表、插入行、查询行和删除表。
