# 测试

运行默认测试套件：

```bash
dotnet test
```

SQLite 集成测试默认使用内存数据库运行。PostgreSQL、MySQL 和 SQL Server 集成测试是可选的，需要通过环境变量或本地 runsettings 启用。

本地构建与打包：

```bash
dotnet build -c Release
dotnet pack src/Db4Net/Db4Net.csproj -c Release --no-build
```

::: tip 提示
测试 Db4Net 调用时，可以使用 `Db4NetDatabase.Create(...)` 和 `ToCommand()` / `ToCommands()` 检查生成的 SQL 与参数，而不必执行真实数据库命令。
:::
