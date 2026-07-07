# QueryPage Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add `QueryPage` / `QueryPageAsync` terminal methods that return rows plus total count through a `PagedResult<T>` value.

**Architecture:** `QueryPage` is a logical convenience over two rendered commands: a `COUNT(*)` command built from the current table and filters, and a row query with the requested page applied. The API must not mutate the existing builder and must reject pre-applied `Limit`, `Offset`, or `Page` to avoid ambiguous count semantics.

**Tech Stack:** .NET, Dapper, xUnit, VitePress documentation.

---

### Task 1: Add API Contract and Behavior Tests

**Files:**
- Modify: `tests/Db4Net.Tests/ApiContractTests.cs`
- Modify: `tests/Db4Net.Tests/SqliteIntegrationTests.cs`
- Modify: `tests/Db4Net.Tests/SelectQueryBuilderTests.cs`

- [ ] **Step 1: Write failing API contract tests**

Add reflection assertions for exported `PagedResult<>` and `QueryPage` / `QueryPageAsync` methods on `SelectQueryBuilder` and `SelectQueryBuilder<>`.

- [ ] **Step 2: Write failing integration tests**

Add SQLite tests proving `QueryPage` returns `Items`, `TotalCount`, `PageNumber`, `PageSize`, and `TotalPages`, and proving `QueryPageAsync` works.

- [ ] **Step 3: Write failing ambiguity test**

Add a test proving `QueryPage` rejects a builder that already has `Limit`, `Offset`, or `Page` applied.

- [ ] **Step 4: Run targeted tests**

Run:

```powershell
dotnet test --filter "FullyQualifiedName~ApiContractTests|FullyQualifiedName~SqliteIntegrationTests|FullyQualifiedName~SelectQueryBuilderTests"
```

Expected: tests fail because `PagedResult<T>` and `QueryPage` APIs do not exist yet.

### Task 2: Implement QueryPage

**Files:**
- Create: `src/Db4Net/PagedResult.cs`
- Modify: `src/Db4Net/Query/SelectQueryModel.cs`
- Modify: `src/Db4Net/Query/SelectQueryBuilder.Execution.cs`
- Modify: `src/Db4Net/Query/SelectQueryBuilder.Typed.cs`

- [ ] **Step 1: Create `PagedResult<T>`**

Add immutable public properties: `Items`, `TotalCount`, `PageNumber`, `PageSize`, and computed `TotalPages`.

- [ ] **Step 2: Add count model cloning**

Add an internal `ToCountModel()` method on `SelectQueryModel` that preserves table and cloned filters while discarding selected columns, ordering, limit, and offset.

- [ ] **Step 3: Add base builder terminal methods**

Add `QueryPage<TResult>` and `QueryPageAsync<TResult>` to `SelectQueryBuilder`. They validate page arguments, reject existing paging, render both count and page commands before executing, and use the same execution options for both commands.

- [ ] **Step 4: Add typed builder terminal methods**

Add `QueryPage` and `QueryPageAsync` to `SelectQueryBuilder<T>` that delegate to the base generic methods.

- [ ] **Step 5: Run targeted tests**

Run the same targeted test command and expect passing results.

### Task 3: Document QueryPage

**Files:**
- Modify: `docs/vitepress/select.md`
- Modify: `docs/vitepress/zh/select.md`
- Modify: `docs/vitepress/ordering-and-paging.md`
- Modify: `docs/vitepress/zh/ordering-and-paging.md`
- Modify: `docs/vitepress/complete-example.md`
- Modify: `docs/vitepress/zh/complete-example.md`
- Modify: `README.md`
- Modify: `src/Db4Net/README.md`
- Modify: `CHANGELOG.md`

- [ ] **Step 1: Add examples**

Document `QueryPage(pageNumber, pageSize)` and `QueryPageAsync(pageNumber, pageSize)` as convenience APIs that execute count plus row queries internally.

- [ ] **Step 2: State constraints**

Document that `QueryPage` owns paging and should not be combined with prior `Limit`, `Offset`, or `Page`.

- [ ] **Step 3: Build docs**

Run:

```powershell
pnpm docs:build
```

Expected: VitePress build succeeds.

### Task 4: Final Verification

**Files:**
- All modified files

- [ ] **Step 1: Run full test suite**

Run:

```powershell
dotnet test
```

Expected: all tests pass.

- [ ] **Step 2: Run docs build**

Run:

```powershell
pnpm docs:build
```

Expected: VitePress build succeeds.

- [ ] **Step 3: Run whitespace check**

Run:

```powershell
git diff --check
```

Expected: no whitespace errors.
