# CUD Builders Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add safe Insert, Update, and Delete command builders while removing `Execute` from the SELECT builder.

**Architecture:** Keep SELECT query building separate from CUD command building. Add focused command models and renderers for Insert, Update, and Delete, reusing model metadata, filter semantics, dialect identifier quoting, Dapper `CommandDefinition`, and `Db4NetCommandOptions`.

**Tech Stack:** C#/.NET 8, Dapper, xUnit, Microsoft.Data.Sqlite, existing Db4Net dialect and metadata infrastructure.

---

### Task 1: Clean SELECT Terminal API

**Files:**
- Modify: `src/Db4Net/Query/SelectQueryBuilder.cs`
- Modify: `tests/Db4Net.Tests/SqliteIntegrationTests.cs`
- Modify: `README.md`
- Modify: `src/Db4Net/README.md`
- Modify: `docs/engineering/decisions/2026-06-26-project-direction.md`

- [x] Remove `SelectQueryBuilder.Execute(...)` and `SelectQueryBuilder.ExecuteAsync(...)`.
- [x] Remove SQLite integration tests that assert `Execute` on SELECT returns `-1`.
- [x] Remove `Execute` and `ExecuteAsync` from SELECT terminal method documentation.
- [x] Run `dotnet test` and verify failures are only from CUD methods not yet implemented, or that the SELECT cleanup is green before moving on.

### Task 2: Shared Command Infrastructure

**Files:**
- Create: `src/Db4Net/Commands/CommandBuilderBase.cs`
- Create: `src/Db4Net/Commands/CommandModels.cs`
- Create: `src/Db4Net/Rendering/CommandSqlRenderer.cs`
- Modify: `src/Db4Net/Db4NetDatabase.cs`

- [x] Add command models for `InsertCommandModel`, `UpdateCommandModel`, and `DeleteCommandModel`.
- [x] Add shared `AssignmentClause` records for insert/update values.
- [x] Add a shared command builder base with `ToCommand()`, `Execute()`, and `ExecuteAsync(...)`.
- [x] Add shared WHERE rendering for update/delete using existing `FilterClause` and `Op`.
- [x] Add `Db4NetDatabase.DeleteFrom<T>()`, `Update<T>()`, and `InsertInto<T>()` entry points.

### Task 3: Delete Builder

**Files:**
- Create: `src/Db4Net/Commands/DeleteCommandBuilder.cs`
- Modify: `src/Db4Net/Rendering/CommandSqlRenderer.cs`
- Create or modify: `tests/Db4Net.Tests/CommandBuilderTests.cs`

- [x] Add failing tests for SQL Server delete SQL, parameter order, `[Column]` mapping, string property validation, `[NotMapped]` rejection, default no-where rejection, and `AllowAllRows()`.
- [x] Implement `DeleteCommandBuilder<T>` with typed and string `Where` / `OrWhere` overloads matching SELECT where behavior.
- [x] Render `DELETE FROM [Table] WHERE ...`.
- [x] Verify tests pass.

### Task 4: Update Builder

**Files:**
- Create: `src/Db4Net/Commands/UpdateCommandBuilder.cs`
- Modify: `src/Db4Net/Rendering/CommandSqlRenderer.cs`
- Modify: `tests/Db4Net.Tests/CommandBuilderTests.cs`

- [x] Add failing tests for typed `Set`, string property `Set`, parameter order with `Set` before `Where`, `[Column]` mapping, `[NotMapped]` rejection, missing set rejection, default no-where rejection, and `AllowAllRows()`.
- [x] Implement `UpdateCommandBuilder<T>`.
- [x] Render `UPDATE [Table] SET [Column] = @p0 WHERE ...`.
- [x] Verify tests pass.

### Task 5: Insert Builder

**Files:**
- Create: `src/Db4Net/Commands/InsertCommandBuilder.cs`
- Modify: `src/Db4Net/Rendering/CommandSqlRenderer.cs`
- Modify: `tests/Db4Net.Tests/CommandBuilderTests.cs`

- [x] Add failing tests for typed `Value`, string property `Value`, parameter order, `[Column]` mapping, `[NotMapped]` rejection, and missing value rejection.
- [x] Implement `InsertCommandBuilder<T>`.
- [x] Render `INSERT INTO [Table] ([Column]) VALUES (@p0)`.
- [x] Verify tests pass.

### Task 6: SQLite Integration Coverage

**Files:**
- Modify: `tests/Db4Net.Tests/SqliteIntegrationTests.cs`

- [x] Add SQLite integration tests for Insert, Update, Delete, and async command execution.
- [x] Use Dapper query terminal methods to verify resulting rows.
- [x] Run `dotnet test`.

### Task 7: Documentation

**Files:**
- Modify: `README.md`
- Modify: `src/Db4Net/README.md`
- Modify: `docs/engineering/decisions/2026-06-26-project-direction.md`

- [x] Document Insert/Update/Delete examples.
- [x] Document that `Execute` is for command builders, not SELECT builders.
- [x] Document `AllowAllRows()` for Update/Delete and the default safety guard.
- [x] Update current status and scope sections.

### Task 8: Final Verification

**Files:**
- No source edits expected.

- [x] Run `dotnet test`.
- [x] Run `dotnet build -c Release`.
- [x] Run `dotnet pack src\Db4Net\Db4Net.csproj -c Release`.
- [x] Run `git diff --check`.
- [x] Review `git status -sb` and summarize uncommitted changes.
