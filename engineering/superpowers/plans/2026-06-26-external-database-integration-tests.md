# External Database Integration Tests Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add opt-in real PostgreSQL, MySQL, and SQL Server integration tests for Db4Net+Dapper execution.

**Architecture:** Keep production code unchanged. Add provider-specific xUnit tests that use environment variables for connection strings, dynamic skip when absent, random table names, and `try/finally` cleanup.

**Tech Stack:** .NET 8, xUnit, Dapper through Db4Net terminal APIs, Npgsql, MySqlConnector, Microsoft.Data.SqlClient, Xunit.SkippableFact.

---

### Task 1: Test Dependencies

**Files:**
- Modify: `tests/Db4Net.Tests/Db4Net.Tests.csproj`

- [ ] **Step 1: Add package references**

Add these package references to the existing test project `ItemGroup`:

```xml
<PackageReference Include="Microsoft.Data.SqlClient" Version="7.0.2" />
<PackageReference Include="MySqlConnector" Version="2.6.1" />
<PackageReference Include="Npgsql" Version="10.0.3" />
<PackageReference Include="Xunit.SkippableFact" Version="1.5.61" />
```

- [ ] **Step 2: Restore/build to verify packages resolve**

Run: `dotnet build tests/Db4Net.Tests/Db4Net.Tests.csproj`

Expected: build succeeds after NuGet restore.

### Task 2: Shared External Database Test Helper

**Files:**
- Create: `tests/Db4Net.Tests/ExternalDatabaseTestSupport.cs`

- [ ] **Step 1: Write helper**

Create a small helper for environment-variable lookup and deterministic random test table names:

```csharp
namespace Db4Net.Tests;

internal static class ExternalDatabaseTestSupport
{
    public static string GetRequiredConnectionString(string environmentVariable)
    {
        var connectionString = Environment.GetEnvironmentVariable(environmentVariable);
        Skip.If(string.IsNullOrWhiteSpace(connectionString), $"{environmentVariable} is not set.");
        return connectionString!;
    }

    public static string CreateTableName(string provider, string purpose)
    {
        return $"db4net_{provider}_{purpose}_{Guid.NewGuid():N}";
    }
}
```

- [ ] **Step 2: Build to verify helper compiles**

Run: `dotnet build tests/Db4Net.Tests/Db4Net.Tests.csproj`

Expected: build succeeds.

### Task 3: PostgreSQL Real Integration Tests

**Files:**
- Create: `tests/Db4Net.Tests/PostgreSqlIntegrationTests.cs`

- [ ] **Step 1: Write PostgreSQL tests**

Add three `[SkippableFact]` tests using `NpgsqlConnection`:

```csharp
using System.ComponentModel.DataAnnotations.Schema;
using Npgsql;

namespace Db4Net.Tests;

public sealed class PostgreSqlIntegrationTests
{
    private const string ConnectionStringEnvironmentVariable = "DB4NET_POSTGRESQL_CONNECTION_STRING";

    [SkippableFact]
    public async Task Query_single_or_default_executes_parameterized_sql()
    {
        var table = ExternalDatabaseTestSupport.CreateTableName("postgresql", "users");
        await using var connection = await OpenConnectionAsync();

        try
        {
            await ExecuteAsync(connection, $"""
                CREATE TABLE "{table}" ("Id" integer PRIMARY KEY, "Name" text NOT NULL);
                INSERT INTO "{table}" ("Id", "Name") VALUES (1, 'Alice'), (2, 'Bob');
                """);

            var user = await connection
                .UseDb4Net(Db4NetOptions.PostgreSql)
                .SelectFrom<User>(table)
                .Where(u => u.Id, Op.Eq, 2)
                .QuerySingleOrDefaultAsync<User>();

            Assert.NotNull(user);
            Assert.Equal(2, user.Id);
            Assert.Equal("Bob", user.Name);
        }
        finally
        {
            await DropTableIfExistsAsync(connection, table);
        }
    }

    [SkippableFact]
    public async Task String_select_with_column_attribute_maps_result_to_property()
    {
        var table = ExternalDatabaseTestSupport.CreateTableName("postgresql", "mapped_users");
        await using var connection = await OpenConnectionAsync();

        try
        {
            await ExecuteAsync(connection, $"""
                CREATE TABLE "{table}" ("Id" integer PRIMARY KEY, "display_name" text NOT NULL);
                INSERT INTO "{table}" ("Id", "display_name") VALUES (1, 'Alice');
                """);

            var user = await connection
                .UseDb4Net(Db4NetOptions.PostgreSql)
                .Select("DisplayName")
                .From<MappedUser>(table)
                .QuerySingleOrDefaultAsync<MappedUser>();

            Assert.NotNull(user);
            Assert.Equal("Alice", user.DisplayName);
        }
        finally
        {
            await DropTableIfExistsAsync(connection, table);
        }
    }

    [SkippableFact]
    public async Task Query_with_ordering_and_paging_executes()
    {
        var table = ExternalDatabaseTestSupport.CreateTableName("postgresql", "paged_users");
        await using var connection = await OpenConnectionAsync();

        try
        {
            await ExecuteAsync(connection, $"""
                CREATE TABLE "{table}" ("Id" integer PRIMARY KEY, "Name" text NOT NULL);
                INSERT INTO "{table}" ("Id", "Name") VALUES (1, 'Alice'), (2, 'Bob'), (3, 'Charlie');
                """);

            var users = (await connection
                .UseDb4Net(Db4NetOptions.PostgreSql)
                .SelectFrom<User>(table)
                .OrderBy(u => u.Id)
                .Offset(1)
                .Limit(1)
                .QueryAsync<User>())
                .ToList();

            var user = Assert.Single(users);
            Assert.Equal(2, user.Id);
            Assert.Equal("Bob", user.Name);
        }
        finally
        {
            await DropTableIfExistsAsync(connection, table);
        }
    }

    private static async Task<NpgsqlConnection> OpenConnectionAsync()
    {
        var connection = new NpgsqlConnection(ExternalDatabaseTestSupport.GetRequiredConnectionString(ConnectionStringEnvironmentVariable));
        await connection.OpenAsync();
        return connection;
    }

    private static Task ExecuteAsync(NpgsqlConnection connection, string sql)
    {
        using var command = connection.CreateCommand();
        command.CommandText = sql;
        return command.ExecuteNonQueryAsync();
    }

    private static Task DropTableIfExistsAsync(NpgsqlConnection connection, string table)
    {
        return ExecuteAsync(connection, $"""DROP TABLE IF EXISTS "{table}";""");
    }

    [Table("Users")]
    private sealed class User
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
    }

    private sealed class MappedUser
    {
        public int Id { get; set; }

        [Column("display_name")]
        public string DisplayName { get; set; } = "";
    }
}
```

- [ ] **Step 2: Run PostgreSQL test class**

Run: `dotnet test tests/Db4Net.Tests/Db4Net.Tests.csproj --filter FullyQualifiedName~PostgreSqlIntegrationTests`

Expected without env var: tests skipped. Expected with env var: tests pass against PostgreSQL.

### Task 4: MySQL Real Integration Tests

**Files:**
- Create: `tests/Db4Net.Tests/MySqlIntegrationTests.cs`

- [ ] **Step 1: Write MySQL tests**

Add the same three scenarios using `MySqlConnector.MySqlConnection`, backtick quoting in setup/cleanup SQL, `Db4NetOptions.MySql`, and environment variable `DB4NET_MYSQL_CONNECTION_STRING`.

- [ ] **Step 2: Run MySQL test class**

Run: `dotnet test tests/Db4Net.Tests/Db4Net.Tests.csproj --filter FullyQualifiedName~MySqlIntegrationTests`

Expected without env var: tests skipped. Expected with env var: tests pass against MySQL.

### Task 5: SQL Server Real Integration Tests

**Files:**
- Create: `tests/Db4Net.Tests/SqlServerIntegrationTests.cs`

- [ ] **Step 1: Write SQL Server tests**

Add the same three scenarios using `Microsoft.Data.SqlClient.SqlConnection`, bracket quoting in setup/cleanup SQL, `Db4NetOptions.SqlServer`, and environment variable `DB4NET_SQLSERVER_CONNECTION_STRING`.

For paging tests, always call `.OrderBy(u => u.Id).Offset(1).Limit(1)` because SQL Server requires `ORDER BY` with `OFFSET`.

- [ ] **Step 2: Run SQL Server test class**

Run: `dotnet test tests/Db4Net.Tests/Db4Net.Tests.csproj --filter FullyQualifiedName~SqlServerIntegrationTests`

Expected without env var: tests skipped. Expected with env var: tests pass against SQL Server.

### Task 6: Documentation and Verification

**Files:**
- Modify: `src/Db4Net/README.md`

- [ ] **Step 1: Document opt-in external tests**

Add a short test section listing the three environment variables and noting that external tests are skipped unless configured.

- [ ] **Step 2: Run full verification**

Run:

```powershell
dotnet test
dotnet build -c Release
dotnet pack src/Db4Net/Db4Net.csproj -c Release --no-build
git diff --check
git status --short
```

Expected: tests pass with external tests skipped when env vars are absent, release build passes, pack succeeds, whitespace check passes.

- [ ] **Step 3: Commit implementation**

Run:

```powershell
git add tests/Db4Net.Tests/Db4Net.Tests.csproj tests/Db4Net.Tests/ExternalDatabaseTestSupport.cs tests/Db4Net.Tests/PostgreSqlIntegrationTests.cs tests/Db4Net.Tests/MySqlIntegrationTests.cs tests/Db4Net.Tests/SqlServerIntegrationTests.cs src/Db4Net/README.md engineering/superpowers/plans/2026-06-26-external-database-integration-tests.md
git commit -m "test: add optional external database integration tests"
```
