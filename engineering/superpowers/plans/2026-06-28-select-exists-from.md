# SelectExistsFrom Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add a SQL-shaped `SelectExistsFrom<T>()` API that renders and executes efficient existence checks with typed filters, grouped predicates, table overrides, and execution options.

**Architecture:** Mirror the dedicated `SelectCountFrom<T>()` design with a separate exists query builder and renderer. Reuse existing filter nodes, `FilterClauseBuilder`, `FilterSqlRenderer`, SQL parameter writing, Dapper execution option merging, and transaction extensions. Keep the exists builder free of row-query APIs.

**Tech Stack:** C#/.NET, Dapper, xUnit, SQLite integration tests, VitePress Markdown docs.

---

### Task 1: Exists Query API Tests

**Files:**
- Modify: `tests/Db4Net.Tests/CommandBuilderTests.cs`
- Modify: `tests/Db4Net.Tests/SqliteIntegrationTests.cs`
- Modify: `tests/Db4Net.Tests/ApiContractTests.cs`

- [ ] **Step 1: Write failing rendering tests**

Add tests for:

```csharp
var command = Db4NetDatabase
    .Create(Db4NetOptions.Sqlite)
    .SelectExistsFrom<User>()
    .Where(u => u.Id, Op.Eq, 1)
    .ToCommand();

Assert.Equal("""SELECT CASE WHEN EXISTS (SELECT 1 FROM "Users" WHERE "Id" = @p0) THEN 1 ELSE 0 END""", command.Sql);
Assert.Equal(1, command.Parameters.Get<int>("p0"));
```

Also add explicit table + mapped column coverage:

```csharp
var command = Db4NetDatabase
    .Create(Db4NetOptions.Sqlite)
    .SelectExistsFrom<MappedUser>("app_users_staging")
    .Where(u => u.DisplayName, Op.Eq, "Alice")
    .ToCommand();

Assert.Equal("""SELECT CASE WHEN EXISTS (SELECT 1 FROM "app_users_staging" WHERE "display_name" = @p0) THEN 1 ELSE 0 END""", command.Sql);
```

- [ ] **Step 2: Write failing execution tests**

Add SQLite tests for:

```csharp
var exists = connection
    .UseDb4Net(Db4NetOptions.Sqlite)
    .SelectExistsFrom<User>()
    .Where(u => u.Id, Op.Eq, 1)
    .Execute();

Assert.True(exists);
```

and a false case:

```csharp
var exists = connection
    .UseDb4Net(Db4NetOptions.Sqlite)
    .SelectExistsFrom<User>()
    .Where(u => u.Id, Op.Eq, 99)
    .Execute();

Assert.False(exists);
```

Also cover async execution, explicit table, transaction/default execution options, transaction extension execution, and cancellation token.

- [ ] **Step 3: Write failing API contract tests**

Assert `SelectExistsQueryBuilder<T>` exposes `Where`, `OrWhere`, `WhereGroup`, `OrWhereGroup`, `ToCommand`, `Execute`, and `ExecuteAsync`, and does not expose `Select`, `OrderBy`, `Limit`, `Offset`, `Page`, `Query`, `QuerySingle`, or `QuerySingleOrDefault`.

- [ ] **Step 4: Run red tests**

Run:

```powershell
dotnet test tests\Db4Net.Tests\Db4Net.Tests.csproj --filter "FullyQualifiedName~CommandBuilderTests|FullyQualifiedName~SqliteIntegrationTests|FullyQualifiedName~ApiContractTests"
```

Expected: compile failures because `SelectExistsFrom` and `SelectExistsQueryBuilder<T>` do not exist yet.

### Task 2: Exists Query Implementation

**Files:**
- Create: `src/Db4Net/Query/SelectExistsQueryModel.cs`
- Create: `src/Db4Net/Query/SelectExistsQueryBuilder.cs`
- Create: `src/Db4Net/Rendering/SelectExistsSqlRenderer.cs`
- Modify: `src/Db4Net/Db4NetDatabase.cs`
- Modify: `src/Db4Net/Db4NetTransactionExtensions.cs`

- [ ] **Step 1: Add exists query model**

Create an internal model with `Table` and `Filters` only.

- [ ] **Step 2: Add exists SQL renderer**

Render:

```sql
SELECT CASE WHEN EXISTS (SELECT 1 FROM <quoted table><filters>) THEN 1 ELSE 0 END
```

Use `FilterSqlRenderer` for `WHERE` and parameters.

- [ ] **Step 3: Add typed exists builder**

`SelectExistsQueryBuilder<T>` should:

- accept options, optional connection, table, and default execution options
- expose typed and CLR-property-name `Where` / `OrWhere`
- expose `WhereGroup` / `OrWhereGroup`
- expose `ToCommand()`
- expose `Execute()` returning `bool`
- expose `ExecuteAsync()` returning `Task<bool>`
- merge default and per-call `Db4NetExecutionOptions`

- [ ] **Step 4: Add database and transaction entry points**

Add:

```csharp
public SelectExistsQueryBuilder<T> SelectExistsFrom<T>()
public SelectExistsQueryBuilder<T> SelectExistsFrom<T>(string table)
```

and matching `Db4NetTransaction` extension methods.

- [ ] **Step 5: Run green tests**

Run the same filtered command. Expected: selected tests pass.

### Task 3: Documentation and Release Notes

**Files:**
- Modify: `README.md`
- Modify: `src/Db4Net/README.md`
- Modify: `docs/select.md`
- Modify: `docs/zh/select.md`
- Modify: `CHANGELOG.md`
- Modify: `src/Db4Net/Db4Net.csproj`
- Modify: `tests/Db4Net.Tests/PackageMetadataTests.cs`

- [ ] **Step 1: Document the API**

Add examples:

```csharp
var exists = db
    .SelectExistsFrom<User>()
    .Where(u => u.Id, Op.Eq, id)
    .Execute();
```

and table override:

```csharp
var exists = db
    .SelectExistsFrom<User>("users_2026")
    .Where(u => u.Name, Op.Like, "A%")
    .Execute();
```

- [ ] **Step 2: Explain the boundary**

Document that `SelectExistsFrom<T>()` is the supported existence-check API. Use it instead of `SelectCountFrom<T>().Execute() > 0` when only existence matters.

- [ ] **Step 3: Update changelog and package release notes**

Under `Unreleased`, add a bullet noting typed `SELECT EXISTS` query support. Update package release notes to mention existence checks and synchronize package metadata tests.

### Task 4: Final Verification

**Files:**
- All files touched by Tasks 1-3

- [ ] **Step 1: Run focused tests**

```powershell
dotnet test tests\Db4Net.Tests\Db4Net.Tests.csproj --filter "FullyQualifiedName~CommandBuilderTests|FullyQualifiedName~SqliteIntegrationTests|FullyQualifiedName~ApiContractTests|FullyQualifiedName~PackageMetadataTests"
```

- [ ] **Step 2: Run full tests**

```powershell
dotnet test
```

- [ ] **Step 3: Run package build**

```powershell
dotnet build src\Db4Net\Db4Net.csproj -c Release
```

- [ ] **Step 4: Run docs build**

```powershell
pnpm run docs:build
```

- [ ] **Step 5: Check whitespace**

```powershell
git diff --check
```
