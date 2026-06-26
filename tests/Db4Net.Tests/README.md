# Db4Net.Tests

## Default Tests

Run the normal test suite from the repository root:

```powershell
dotnet test
```

SQLite integration tests run by default with an in-memory database. PostgreSQL, MySQL, and SQL Server integration tests are opt-in and skipped unless their connection strings are configured.

## Local External Database Tests

`local.runsettings` contains empty environment variables by default, so external database tests still skip until you fill in real connection strings.

Example values:

```xml
<DB4NET_POSTGRESQL_CONNECTION_STRING>Host=localhost;Port=5432;Database=db4net_tests;Username=postgres;Password=your_password</DB4NET_POSTGRESQL_CONNECTION_STRING>
<DB4NET_MYSQL_CONNECTION_STRING>Server=localhost;Port=3306;Database=db4net_tests;User ID=root;Password=your_password</DB4NET_MYSQL_CONNECTION_STRING>
<DB4NET_SQLSERVER_CONNECTION_STRING>Server=localhost,1433;Database=Db4NetTests;User Id=sa;Password=your_password;TrustServerCertificate=True</DB4NET_SQLSERVER_CONNECTION_STRING>
```

Run all tests with local external database settings:

```powershell
dotnet test --settings tests\Db4Net.Tests\local.runsettings
```

Run one external database test class:

```powershell
dotnet test --settings tests\Db4Net.Tests\local.runsettings --filter FullyQualifiedName~MySqlIntegrationTests
dotnet test --settings tests\Db4Net.Tests\local.runsettings --filter FullyQualifiedName~PostgreSqlIntegrationTests
dotnet test --settings tests\Db4Net.Tests\local.runsettings --filter FullyQualifiedName~SqlServerIntegrationTests
```

The configured database user must be able to create tables, insert rows, select rows, and drop tables. The tests create random table names and clean them up in `finally` blocks.

## Environment Variables

`local.runsettings` injects these environment variables into the test process:

- `DB4NET_POSTGRESQL_CONNECTION_STRING`
- `DB4NET_MYSQL_CONNECTION_STRING`
- `DB4NET_SQLSERVER_CONNECTION_STRING`

You can also set any of these environment variables directly in PowerShell, your IDE test configuration, or CI secrets. It is fine to configure only one database; tests for unconfigured databases remain skipped.
