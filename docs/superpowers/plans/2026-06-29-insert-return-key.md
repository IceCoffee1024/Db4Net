# Insert Return Key Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add single-row insert return-key support for `InsertCommandBuilder<T>` across supported dialects.

**Architecture:** Keep ordinary affected-row command execution unchanged. Extend `InsertCommandModel` with one optional return-key column, render provider-specific scalar-return SQL only when a return key is requested, add direct `ExecuteReturnKey` terminals to `InsertCommandBuilder<T>`, and add a dedicated `InsertReturnKeyCommandBuilder<T>` for `.ReturnKey(...).Execute<TResult>()`.

**Tech Stack:** C#/.NET, Dapper, xUnit, Microsoft.Data.Sqlite, VitePress Markdown.

---

### Task 1: Red Tests For API And SQL Rendering

**Files:**
- Modify: `tests/Db4Net.Tests/CommandBuilderTests.cs`
- Modify: `tests/Db4Net.Tests/ApiContractTests.cs`

- [ ] Add API contract coverage showing `InsertCommandBuilder<T>` exposes `ReturnKey`, `ExecuteReturnKey`, and `ExecuteReturnKeyAsync`.
- [ ] Add API contract coverage showing `ReturnKey(...)` returns `InsertReturnKeyCommandBuilder<T>`.
- [ ] Add API contract coverage showing `InsertReturnKeyCommandBuilder<T>` exposes only `ToCommand`, generic `Execute<TResult>`, and generic `ExecuteAsync<TResult>`.
- [ ] Add API contract coverage showing `InsertManyCommandBuilder<T>`, `InsertOrIgnoreCommandBuilder<T>`, and `InsertOrUpdateCommandBuilder<T>` do not expose return-key terminals.
- [ ] Add SQL Server rendering test:

```csharp
var command = Db4NetDatabase
    .Create(Db4NetOptions.SqlServer)
    .InsertInto<GeneratedKeyUser>()
    .Values(new GeneratedKeyUser { Name = "Alice" })
    .ReturnKey(u => u.Id)
    .ToCommand();

Assert.Equal("INSERT INTO [generated_users] ([Name]) OUTPUT INSERTED.[Id] VALUES (@p0)", command.Sql);
```

- [ ] Add PostgreSQL and SQLite rendering tests that append `RETURNING "Id"`.
- [ ] Add MySQL generated-key rendering test that appends `; SELECT LAST_INSERT_ID()`.
- [ ] Add MySQL explicit-key rendering test that appends `; SELECT @p0`.
- [ ] Add validation tests for no default key, multiple default keys, non-key selector, and no public multi-`ReturnKey(...)` chain on the return-key builder.
- [ ] Run targeted tests and confirm failures are caused by missing return-key API/rendering.

### Task 2: Red Tests For SQLite Execution

**Files:**
- Modify: `tests/Db4Net.Tests/SqliteIntegrationTests.cs`

- [ ] Add a generated-user SQLite fixture:

```sql
create table generated_users (Id integer primary key autoincrement, Name text not null);
create table generated_users_staging (Id integer primary key autoincrement, Name text not null);
```

- [ ] Add sync test for `db.Insert(entity).ExecuteReturnKey<long>()`.
- [ ] Add sync test for `db.Insert(entity, table: "...").ExecuteReturnKey<long>()`.
- [ ] Add async test for `ExecuteReturnKeyAsync<long>(u => u.Id)`.
- [ ] Add builder test for `.ReturnKey(u => u.Id).Execute<long>()`.
- [ ] Add transaction-options test proving returned-key insert participates in rollback.
- [ ] Run targeted SQLite tests and confirm failures are caused by missing return-key execution.

### Task 3: Implement Metadata, Rendering, And Scalar Execution

**Files:**
- Modify: `src/Db4Net/Commands/CommandModels.cs`
- Modify: `src/Db4Net/Commands/DapperCommandExecutor.cs`
- Modify: `src/Db4Net/Commands/CommandBuilderBase.cs`
- Modify: `src/Db4Net/Commands/InsertCommandBuilder.cs`
- Create: `src/Db4Net/Commands/InsertReturnKeyCommandBuilder.cs`
- Modify: `src/Db4Net/Rendering/CommandSqlRenderer.cs`

- [ ] Add optional `ColumnMetadata? ReturnKey` to `InsertCommandModel`.
- [ ] Add scalar execution methods to `DapperCommandExecutor` and protected scalar helpers to `CommandBuilderBase`.
- [ ] Add `ReturnKey(Expression<Func<T, object?>> keySelector)` to `InsertCommandBuilder<T>`.
- [ ] Add `ExecuteReturnKey<TResult>()`, `ExecuteReturnKey<TResult>(Expression<Func<T, object?>> keySelector, ...)`, and async equivalents.
- [ ] Add `InsertReturnKeyCommandBuilder<T>` with `ToCommand()`, `Execute<TResult>()`, and `ExecuteAsync<TResult>()`.
- [ ] Render SQL Server `OUTPUT INSERTED.<key>`, PostgreSQL/SQLite `RETURNING <key>`, and MySQL `SELECT LAST_INSERT_ID()` / `SELECT @parameter`.
- [ ] Run targeted command and SQLite tests and confirm green.

### Task 4: Documentation And Package Metadata

**Files:**
- Modify: `README.md`
- Modify: `src/Db4Net/README.md`
- Modify: `docs/vitepress/insert.md`
- Modify: `docs/vitepress/zh/insert.md`
- Modify: `docs/vitepress/entity-convenience.md`
- Modify: `docs/vitepress/zh/entity-convenience.md`
- Modify: `docs/vitepress/mapping.md`
- Modify: `docs/vitepress/zh/mapping.md`
- Modify: `CHANGELOG.md`
- Modify: `src/Db4Net/Db4Net.csproj`
- Modify: `tests/Db4Net.Tests/PackageMetadataTests.cs`

- [ ] Document the convenience and SQL-shaped return-key APIs.
- [ ] Explain `[Key]` vs `[DatabaseGenerated(DatabaseGeneratedOption.Identity)]` in the insert-return-key context.
- [ ] State that many inserts and conflict inserts do not return keys in this release.
- [ ] Update changelog and package release notes.
- [ ] Run package metadata tests.

### Task 5: Final Verification

**Files:**
- Verify all changed source, tests, docs, and project metadata.

- [ ] Run `dotnet test`.
- [ ] Run `dotnet build src\Db4Net\Db4Net.csproj -c Release`.
- [ ] Run `pnpm run docs:build`.
- [ ] Run `dotnet pack src\Db4Net\Db4Net.csproj -c Release --no-build`.
- [ ] Run `git diff --check`.
- [ ] Review `git status --short` and summarize changed files.
