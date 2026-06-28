# Testing

Run the default test suite from the repository root:

```bash
dotnet test
```

SQLite integration tests run by default with an in-memory database.

PostgreSQL, MySQL, and SQL Server integration tests are opt-in. They are skipped unless connection strings are configured.

## Local External Database Tests

If `tests/Db4Net.Tests/local.runsettings` exists, the test project can read it automatically during local runs. You can also pass it explicitly:

```bash
dotnet test --settings tests/Db4Net.Tests/local.runsettings
```

The external database connection strings are:

- `DB4NET_POSTGRESQL_CONNECTION_STRING`
- `DB4NET_MYSQL_CONNECTION_STRING`
- `DB4NET_SQLSERVER_CONNECTION_STRING`

Run one external provider test class:

```bash
dotnet test --settings tests/Db4Net.Tests/local.runsettings --filter FullyQualifiedName~MySqlIntegrationTests
dotnet test --settings tests/Db4Net.Tests/local.runsettings --filter FullyQualifiedName~PostgreSqlIntegrationTests
dotnet test --settings tests/Db4Net.Tests/local.runsettings --filter FullyQualifiedName~SqlServerIntegrationTests
```

The configured database user must be able to create tables, insert rows, select rows, and drop tables.
