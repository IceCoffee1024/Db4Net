# SelectCountFrom Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add a SQL-shaped `SelectCountFrom<T>()` API that renders and executes `SELECT COUNT(*) FROM ...` with typed filters, grouped predicates, table overrides, and execution options.

**Architecture:** Introduce a dedicated count query builder instead of overloading `SelectFrom<T>()`. Reuse existing filter nodes, filter builders, parameter writing, Dapper execution option merging, and dialect quoting. Keep count queries free of row-query concepts such as selected columns, ordering, paging, and generic materialization.

**Tech Stack:** C#/.NET, Dapper, xUnit, SQLite integration tests, VitePress Markdown docs.

---

### Task 1: Count Query API Tests

**Files:**
- Modify: `tests/Db4Net.Tests/CommandBuilderTests.cs`
- Modify: `tests/Db4Net.Tests/SqliteIntegrationTests.cs`
- Modify: `tests/Db4Net.Tests/ApiContractTests.cs`

- [ ] **Step 1: Write failing rendering tests**

Add tests that express the desired API:

```csharp
var command = Db4NetDatabase
    .Create(Db4NetOptions.Sqlite)
    .SelectCountFrom<CommandUser>()
    .Where(u => u.Id, Op.Gt, 1)
    .ToCommand();

Assert.Equal("""SELECT COUNT(*) FROM "users" WHERE "id" > @p0""", command.Sql);
```

Also add a table override test:

```csharp
var command = Db4NetDatabase
    .Create(Db4NetOptions.Sqlite)
    .SelectCountFrom<CommandUser>("users_2026")
    .Where(u => u.Name, Op.Like, "A%")
    .ToCommand();

Assert.Equal("""SELECT COUNT(*) FROM "users_2026" WHERE "name" LIKE @p0""", command.Sql);
```

- [ ] **Step 2: Write failing execution tests**

Add SQLite tests for:

```csharp
var count = connection
    .UseDb4Net(Db4NetOptions.Sqlite)
    .SelectCountFrom<IntegrationUser>()
    .Where(u => u.IsActive, Op.Eq, true)
    .Execute();
```

and:

```csharp
var count = await connection
    .UseDb4Net(Db4NetOptions.Sqlite)
    .SelectCountFrom<IntegrationUser>()
    .Where(u => u.Id, Op.Gt, 0)
    .ExecuteAsync();
```

- [ ] **Step 3: Write failing API contract tests**

Assert `SelectCountQueryBuilder<T>` exposes `Where`, `OrWhere`, `WhereGroup`, `OrWhereGroup`, `ToCommand`, `Execute`, and `ExecuteAsync`, and does not expose `Select`, `OrderBy`, `Limit`, `Offset`, `Page`, `Query`, `QuerySingle`, or `QuerySingleOrDefault`.

- [ ] **Step 4: Run red tests**

Run:

```powershell
dotnet test tests\Db4Net.Tests\Db4Net.Tests.csproj --filter "FullyQualifiedName~CommandBuilderTests|FullyQualifiedName~SqliteIntegrationTests|FullyQualifiedName~ApiContractTests"
```

Expected: compile failures or test failures because `SelectCountFrom` and `SelectCountQueryBuilder<T>` do not exist yet.

### Task 2: Count Query Implementation

**Files:**
- Create: `src/Db4Net/Query/SelectCountQueryModel.cs`
- Create: `src/Db4Net/Query/SelectCountQueryBuilder.cs`
- Create: `src/Db4Net/Rendering/SelectCountSqlRenderer.cs`
- Modify: `src/Db4Net/Db4NetDatabase.cs`
- Modify: `src/Db4Net/Db4NetTransactionExtensions.cs`

- [ ] **Step 1: Add count query model**

Create an internal model with `Table` and `Filters` only.

- [ ] **Step 2: Add count SQL renderer**

Render:

```sql
SELECT COUNT(*) FROM <quoted table><filters>
```

Use `FilterSqlRenderer` for `WHERE` and parameters. Do not render selected columns, order clauses, or paging.

- [ ] **Step 3: Add typed count builder**

`SelectCountQueryBuilder<T>` should:

- accept options, optional connection, table, and default execution options
- expose typed and CLR-property-name `Where` / `OrWhere`
- expose `WhereGroup` / `OrWhereGroup`
- expose `ToCommand()`
- expose `Execute()` returning `long`
- expose `ExecuteAsync()` returning `Task<long>`
- merge default and per-call `Db4NetExecutionOptions`

- [ ] **Step 4: Add database and transaction entry points**

Add:

```csharp
public SelectCountQueryBuilder<T> SelectCountFrom<T>()
public SelectCountQueryBuilder<T> SelectCountFrom<T>(string table)
```

and matching `Db4NetTransaction` extension methods.

- [ ] **Step 5: Run green tests**

Run the same filtered command. Expected: selected tests pass.

### Task 3: Documentation and Release Notes

**Files:**
- Modify: `README.md`
- Modify: `src/Db4Net/README.md`
- Modify: `docs/querying.md`
- Modify: `docs/zh/querying.md`
- Modify: `CHANGELOG.md`

- [ ] **Step 1: Document the API**

Add examples:

```csharp
var count = db
    .SelectCountFrom<User>()
    .Where(u => u.IsActive, Op.Eq, true)
    .Execute();
```

and table override:

```csharp
var count = db
    .SelectCountFrom<User>("users_2026")
    .Where(u => u.TenantId, Op.Eq, tenantId)
    .Execute();
```

- [ ] **Step 2: Explain the boundary**

Document that `SelectCountFrom<T>()` is the supported count-query API. Do not recommend `Select("COUNT(*)")` because string select values are identifiers, not raw SQL expressions.

- [ ] **Step 3: Update changelog**

Under `Unreleased`, add a bullet noting typed `SELECT COUNT(*)` query support with filters, groups, transactions, and table overrides.

### Task 4: Final Verification

**Files:**
- All files touched by Tasks 1-3

- [ ] **Step 1: Run focused tests**

```powershell
dotnet test tests\Db4Net.Tests\Db4Net.Tests.csproj --filter "FullyQualifiedName~CommandBuilderTests|FullyQualifiedName~SqliteIntegrationTests|FullyQualifiedName~ApiContractTests"
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
