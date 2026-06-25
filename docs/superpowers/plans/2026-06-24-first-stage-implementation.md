# First Stage Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build the first working Db4Net package milestone: typed/string `SELECT` query building, SQL Server and SQLite rendering, Dapper terminal methods, and a SQLite integration test.

**Architecture:** The library separates fluent query composition from SQL rendering and Dapper execution. `QueryBuilder` stores a small query model, `SqlRenderer` renders SQL plus `DynamicParameters`, dialect classes handle SQL Server/SQLite differences, metadata resolves CLR type/member names, and terminal methods call Dapper.

**Tech Stack:** .NET 8 class library, Dapper 2.1.79, Microsoft.Data.Sqlite for integration tests, xUnit.

---

## File Structure

- Create `Db4Net.sln`: solution file.
- Create `src/Db4Net/Db4Net.csproj`: NuGet-ready class library.
- Create `src/Db4Net/Db4NetOptions.cs`: dialect selection.
- Create `src/Db4Net/Db4NetDatabase.cs`: `Db4Net.Create(...)` entry point.
- Create `src/Db4Net/Db4NetConnectionExtensions.cs`: `IDbConnection` extension entry points.
- Create `src/Db4Net/Op.cs`: supported operators.
- Create `src/Db4Net/SqlCommandDefinition.cs`: rendered SQL and Dapper parameters.
- Create `src/Db4Net/Dialects/ISqlDialect.cs`: dialect contract.
- Create `src/Db4Net/Dialects/SqlServerDialect.cs`: SQL Server quoting and paging.
- Create `src/Db4Net/Dialects/SqliteDialect.cs`: SQLite quoting and paging.
- Create `src/Db4Net/Metadata/ModelMetadataProvider.cs`: type and expression mapping.
- Create `src/Db4Net/Query/QueryModel.cs`: internal query model records.
- Create `src/Db4Net/Query/SelectQueryBuilder.cs`: fluent API and Dapper execution.
- Create `src/Db4Net/Rendering/SqlRenderer.cs`: render SQL and parameters.
- Create `tests/Db4Net.Tests/Db4Net.Tests.csproj`: xUnit test project.
- Create `tests/Db4Net.Tests/SelectQueryBuilderTests.cs`: rendering tests.
- Create `tests/Db4Net.Tests/SqliteIntegrationTests.cs`: Dapper/SQLite execution test.

## Task 1: Scaffold Solution

**Files:**
- Create: `Db4Net.sln`
- Create: `src/Db4Net/Db4Net.csproj`
- Create: `tests/Db4Net.Tests/Db4Net.Tests.csproj`

- [ ] **Step 1: Create solution and projects**

Run:

```powershell
dotnet new sln -n Db4Net
dotnet new classlib -n Db4Net -o src/Db4Net -f net8.0
dotnet new xunit -n Db4Net.Tests -o tests/Db4Net.Tests -f net8.0
dotnet sln add src/Db4Net/Db4Net.csproj
dotnet sln add tests/Db4Net.Tests/Db4Net.Tests.csproj
dotnet add tests/Db4Net.Tests/Db4Net.Tests.csproj reference src/Db4Net/Db4Net.csproj
dotnet add src/Db4Net/Db4Net.csproj package Dapper --version 2.1.79
dotnet add tests/Db4Net.Tests/Db4Net.Tests.csproj package Microsoft.Data.Sqlite
```

Expected: all commands exit 0.

- [ ] **Step 2: Remove template files**

Delete:

```text
src/Db4Net/Class1.cs
tests/Db4Net.Tests/UnitTest1.cs
```

- [ ] **Step 3: Run baseline tests**

Run:

```powershell
dotnet test
```

Expected: build succeeds; there may be no tests after template cleanup.

## Task 2: Rendering Tests First

**Files:**
- Create: `tests/Db4Net.Tests/SelectQueryBuilderTests.cs`

- [ ] **Step 1: Write failing rendering tests**

Add tests that use the intended public API:

```csharp
using System.ComponentModel.DataAnnotations.Schema;
using Db4Net;
using Xunit;

namespace Db4Net.Tests;

public sealed class SelectQueryBuilderTests
{
    [Fact]
    public void Typed_select_renders_sql_server_sql_with_parameter()
    {
        var command = Db4NetDatabase
            .Create(Db4NetOptions.SqlServer)
            .Select("Id", "Name")
            .From<User>()
            .Where(u => u.Id, Op.Eq, 1)
            .ToCommand();

        Assert.Equal("select [Id], [Name] from [Users] where [Id] = @p0", command.Sql);
        Assert.Equal(1, command.Parameters.Get<int>("p0"));
    }

    [Fact]
    public void Select_from_type_renders_sqlite_ordering_and_paging()
    {
        var command = Db4NetDatabase
            .Create(Db4NetOptions.Sqlite)
            .SelectFrom<User>()
            .Where(u => u.Name, Op.Like, "A%")
            .OrderByDescending(u => u.Id)
            .Page(2, 10)
            .ToCommand();

        Assert.Equal("""select * from "Users" where "Name" like @p0 order by "Id" desc limit @p1 offset @p2""", command.Sql);
        Assert.Equal("A%", command.Parameters.Get<string>("p0"));
        Assert.Equal(10, command.Parameters.Get<int>("p1"));
        Assert.Equal(10, command.Parameters.Get<int>("p2"));
    }

    [Fact]
    public void String_table_and_column_identifiers_are_validated()
    {
        var db = Db4NetDatabase.Create(Db4NetOptions.Sqlite);

        var ex = Assert.Throws<ArgumentException>(() =>
            db.SelectFrom("Users;drop table Users")
              .Where("Id", Op.Eq, 1)
              .ToCommand());

        Assert.Contains("Invalid SQL identifier", ex.Message);
    }

    [Fact]
    public void In_operator_expands_parameters()
    {
        var command = Db4NetDatabase
            .Create(Db4NetOptions.SqlServer)
            .SelectFrom<User>()
            .Where(u => u.Id, Op.In, new[] { 1, 2, 3 })
            .ToCommand();

        Assert.Equal("select * from [Users] where [Id] in (@p0, @p1, @p2)", command.Sql);
        Assert.Equal(1, command.Parameters.Get<int>("p0"));
        Assert.Equal(2, command.Parameters.Get<int>("p1"));
        Assert.Equal(3, command.Parameters.Get<int>("p2"));
    }

    [Table("Users")]
    private sealed class User
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
    }
}
```

- [ ] **Step 2: Run tests to verify RED**

Run:

```powershell
dotnet test tests/Db4Net.Tests/Db4Net.Tests.csproj --filter SelectQueryBuilderTests
```

Expected: FAIL because `Db4NetDatabase`, `Db4NetOptions`, and query APIs do not exist.

## Task 3: Implement Rendering

**Files:**
- Create: `src/Db4Net/Db4NetOptions.cs`
- Create: `src/Db4Net/Db4NetDatabase.cs`
- Create: `src/Db4Net/Op.cs`
- Create: `src/Db4Net/SqlCommandDefinition.cs`
- Create: `src/Db4Net/Dialects/ISqlDialect.cs`
- Create: `src/Db4Net/Dialects/SqlServerDialect.cs`
- Create: `src/Db4Net/Dialects/SqliteDialect.cs`
- Create: `src/Db4Net/Metadata/ModelMetadataProvider.cs`
- Create: `src/Db4Net/Query/QueryModel.cs`
- Create: `src/Db4Net/Query/SelectQueryBuilder.cs`
- Create: `src/Db4Net/Rendering/SqlRenderer.cs`

- [ ] **Step 1: Implement the minimal public API and renderer**

Implement:

```csharp
Db4NetDatabase.Create(Db4NetOptions.SqlServer)
Db4NetDatabase.Create(Db4NetOptions.Sqlite)
Select(...)
SelectFrom<T>()
SelectFrom(string)
From<T>()
From(string)
Where(...)
OrWhere(...)
OrderBy(...)
OrderByDescending(...)
Limit(...)
Offset(...)
Page(...)
ToCommand()
```

The renderer must produce the exact SQL strings asserted in Task 2.

- [ ] **Step 2: Run rendering tests to verify GREEN**

Run:

```powershell
dotnet test tests/Db4Net.Tests/Db4Net.Tests.csproj --filter SelectQueryBuilderTests
```

Expected: PASS.

## Task 4: Dapper Execution And SQLite Integration

**Files:**
- Create: `tests/Db4Net.Tests/SqliteIntegrationTests.cs`
- Modify: `src/Db4Net/Query/SelectQueryBuilder.cs`
- Create: `src/Db4Net/Db4NetConnectionExtensions.cs`

- [ ] **Step 1: Write failing SQLite integration test**

Add:

```csharp
using Db4Net;
using Microsoft.Data.Sqlite;
using Xunit;

namespace Db4Net.Tests;

public sealed class SqliteIntegrationTests
{
    [Fact]
    public void Query_single_or_default_executes_parameterized_sql_with_dapper()
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        using var setup = connection.CreateCommand();
        setup.CommandText = """
            create table Users (Id integer primary key, Name text not null);
            insert into Users (Id, Name) values (1, 'Alice');
            insert into Users (Id, Name) values (2, 'Bob');
            """;
        setup.ExecuteNonQuery();

        var user = connection
            .UseDb4Net(Db4NetOptions.Sqlite)
            .SelectFrom<User>()
            .Where(u => u.Id, Op.Eq, 2)
            .QuerySingleOrDefault<User>();

        Assert.NotNull(user);
        Assert.Equal(2, user.Id);
        Assert.Equal("Bob", user.Name);
    }

    private sealed class User
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
    }
}
```

- [ ] **Step 2: Run integration test to verify RED**

Run:

```powershell
dotnet test tests/Db4Net.Tests/Db4Net.Tests.csproj --filter SqliteIntegrationTests
```

Expected: FAIL because Dapper terminal methods or `UseDb4Net` do not exist.

- [ ] **Step 3: Implement execution methods**

Implement:

```csharp
IEnumerable<T> Query<T>()
T? QueryFirstOrDefault<T>()
T? QuerySingleOrDefault<T>()
int Execute()
```

on builders that have an `IDbConnection`, and implement:

```csharp
connection.UseDb4Net(Db4NetOptions.Sqlite)
```

- [ ] **Step 4: Run integration test to verify GREEN**

Run:

```powershell
dotnet test tests/Db4Net.Tests/Db4Net.Tests.csproj --filter SqliteIntegrationTests
```

Expected: PASS.

## Task 5: Full Verification

**Files:**
- All project files.

- [ ] **Step 1: Run full test suite**

Run:

```powershell
dotnet test
```

Expected: PASS.

- [ ] **Step 2: Run build in Release**

Run:

```powershell
dotnet build -c Release
```

Expected: PASS.

- [ ] **Step 3: Scan for placeholders**

Run:

```powershell
Get-ChildItem -Recurse -Include *.cs,*.md | Select-String -Pattern 'TODO|TBD|FIXME|placeholder'
```

Expected: no project placeholders except this plan's "No placeholders" wording if searched.

