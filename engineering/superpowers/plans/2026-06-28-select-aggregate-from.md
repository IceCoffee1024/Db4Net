# Select Aggregate From Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add a bounded `SelectAggregateFrom<T>()` scalar aggregate API with `Max`, `Min`, and `CountDistinct`.

**Architecture:** Existing count and exists builders remain public, but share a new internal scalar model, renderer, builder state, and Dapper scalar executor. Aggregate selection uses an initial selector builder and one terminal-typed scalar aggregate builder.

**Tech Stack:** C#/.NET, Dapper, xUnit, VitePress Markdown.

---

### Task 1: Red Tests

**Files:**
- Modify: `tests/Db4Net.Tests/CommandBuilderTests.cs`
- Modify: `tests/Db4Net.Tests/ApiContractTests.cs`
- Modify: `tests/Db4Net.Tests/SqliteIntegrationTests.cs`

- [ ] Add rendering tests for `Max`, `Min`, `CountDistinct`, grouped filters, table override, and string mapped filters.
- [ ] Add API contract tests for public aggregate builders and absence of `SelectMaxFrom`, `SelectMinFrom`, and `SelectCountDistinctFrom`.
- [ ] Add SQLite execution tests for value aggregates, null empty-set aggregate, count distinct, and transaction extension.
- [ ] Run targeted tests and confirm failure is caused by missing API/types.

### Task 2: Internal Scalar Infrastructure

**Files:**
- Add: `src/Db4Net/Query/ScalarQueryModel.cs`
- Add: `src/Db4Net/Query/ScalarQueryBuilderState.cs`
- Add: `src/Db4Net/Rendering/ScalarSqlRenderer.cs`
- Add: `src/Db4Net/Query/DapperScalarExecutor.cs`
- Modify: `src/Db4Net/Query/SelectCountQueryBuilder.cs`
- Modify: `src/Db4Net/Query/SelectExistsQueryBuilder.cs`
- Delete: `src/Db4Net/Query/SelectCountQueryModel.cs`
- Delete: `src/Db4Net/Query/SelectExistsQueryModel.cs`
- Delete: `src/Db4Net/Rendering/SelectCountSqlRenderer.cs`
- Delete: `src/Db4Net/Rendering/SelectExistsSqlRenderer.cs`

- [ ] Implement the scalar projection enum/model.
- [ ] Implement shared typed filter state.
- [ ] Implement shared scalar SQL renderer.
- [ ] Implement shared Dapper scalar executor.
- [ ] Refactor count and exists builders to use the shared internals without changing public behavior.

### Task 3: Aggregate Public API

**Files:**
- Add: `src/Db4Net/Query/SelectAggregateQueryBuilder.cs`
- Add: `src/Db4Net/Query/SelectAggregateScalarQueryBuilder.cs`
- Modify: `src/Db4Net/Db4NetDatabase.cs`
- Modify: `src/Db4Net/Db4NetTransactionExtensions.cs`

- [ ] Add `SelectAggregateFrom<T>()` and `SelectAggregateFrom<T>(string)`.
- [ ] Add `Max`, `Min`, and `CountDistinct` selector methods.
- [ ] Add a scalar aggregate terminal builder with filter methods, `ToCommand`, `Execute<TResult>`, and `ExecuteAsync<TResult>`.
- [ ] Wire transaction extension methods.
- [ ] Run targeted tests and confirm green.

### Task 4: Documentation And Verification

**Files:**
- Modify: `README.md`
- Modify: `src/Db4Net/README.md`
- Modify: `docs/select.md`
- Modify: `docs/zh/select.md`
- Modify: `CHANGELOG.md`
- Modify: `src/Db4Net/Db4Net.csproj`

- [ ] Document `SelectAggregateFrom<T>()` as the unified aggregate entry.
- [ ] Update release notes and changelog.
- [ ] Run `dotnet test`.
- [ ] Run `dotnet build src\Db4Net\Db4Net.csproj -c Release`.
- [ ] Run `pnpm run docs:build`.
- [ ] Run `git diff --check`.
