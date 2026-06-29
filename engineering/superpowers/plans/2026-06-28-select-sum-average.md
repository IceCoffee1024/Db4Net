# Select Sum and Average Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add `Sum` and explicit-result `Average` aggregate methods to `SelectAggregateFrom<T>()`.

**Architecture:** Extend the existing scalar aggregate pipeline instead of adding new query builders. `SelectAggregateQueryBuilder<T>` chooses the projection and result type; `ScalarSqlRenderer` renders the SQL; existing scalar builder/executor handles filters and Dapper execution.

**Tech Stack:** C#/.NET, Dapper, xUnit, VitePress Markdown.

---

### Task 1: Red Tests

**Files:**
- Modify: `tests/Db4Net.Tests/CommandBuilderTests.cs`
- Modify: `tests/Db4Net.Tests/ApiContractTests.cs`
- Modify: `tests/Db4Net.Tests/SqliteIntegrationTests.cs`

- [ ] Add SQL rendering tests for `SUM(...)` and `AVG(...)`.
- [ ] Add API contract tests for `Sum` and `Average`, including no non-generic `Average`.
- [ ] Add SQLite execution tests for inferred sum, explicit generic sum value type, explicit average, and empty-set sum/average null results.
- [ ] Run targeted tests and confirm failure is caused by missing API/projection support.

### Task 2: Implementation

**Files:**
- Modify: `src/Db4Net/Query/ScalarQueryModel.cs`
- Modify: `src/Db4Net/Rendering/ScalarSqlRenderer.cs`
- Modify: `src/Db4Net/Query/SelectAggregateQueryBuilder.cs`

- [ ] Add `Sum` and `Average` enum values.
- [ ] Render `SUM(column)` and `AVG(column)`.
- [ ] Add public `Sum` and explicit-result `Average` methods.
- [ ] Run targeted tests and confirm green.

### Task 3: Documentation And Verification

**Files:**
- Modify: `README.md`
- Modify: `src/Db4Net/README.md`
- Modify: `docs/select.md`
- Modify: `docs/zh/select.md`
- Modify: `CHANGELOG.md`
- Modify: `src/Db4Net/Db4Net.csproj`
- Modify: `tests/Db4Net.Tests/PackageMetadataTests.cs`

- [ ] Document inferred or explicitly typed `Sum` and explicit-result `Average`.
- [ ] Update changelog and package release notes.
- [ ] Run `dotnet test`.
- [ ] Run `dotnet build src\Db4Net\Db4Net.csproj -c Release`.
- [ ] Run `pnpm run docs:build`.
- [ ] Run `git diff --check`.
