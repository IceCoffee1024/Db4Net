# VitePress Documentation Site Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build the first bilingual VitePress documentation skeleton for Db4Net.

**Architecture:** `docs/` is the public VitePress site root. English routes live at the root; Simplified Chinese routes live under `docs/zh/`. Internal implementation notes remain in `engineering/`.

**Tech Stack:** VitePress 2 alpha, pnpm, Markdown, TypeScript config.

---

### Task 1: Configure VitePress

**Files:**
- Modify: `docs/.vitepress/config.mts`
- Delete: `docs/api-examples.md`
- Delete: `docs/markdown-examples.md`

- [ ] Replace scaffold navigation with Db4Net navigation.
- [ ] Configure English root locale and Simplified Chinese `zh` locale.
- [ ] Add matching English and Chinese sidebar groups.
- [ ] Point GitHub social link to `https://github.com/IceCoffee1024/Db4Net`.
- [ ] Remove VitePress scaffold example pages.

### Task 2: Create English Documentation Pages

**Files:**
- Modify: `docs/index.md`
- Create: `docs/getting-started.md`
- Create: `docs/select.md`
- Create: `docs/filters.md`
- Create: `docs/ordering-and-paging.md`
- Create: `docs/insert.md`
- Create: `docs/update.md`
- Create: `docs/delete.md`
- Create: `docs/entity-convenience.md`
- Create: `docs/many-convenience.md`
- Create: `docs/conflict-inserts.md`
- Create: `docs/table-overrides.md`
- Create: `docs/mapping.md`
- Create: `docs/dialects.md`
- Create: `docs/execution-options.md`
- Create: `docs/testing.md`
- Create: `docs/limitations.md`
- Create: `docs/changelog.md`

- [ ] Replace the scaffold home page with a Db4Net entry page.
- [ ] Pull concise examples from `README.md` and `src/Db4Net/README.md`.
- [ ] Keep pages accurate to the current public API.
- [ ] Use VitePress containers for safety notes.

### Task 3: Create Simplified Chinese Documentation Pages

**Files:**
- Create: `docs/zh/index.md`
- Create: `docs/zh/getting-started.md`
- Create: `docs/zh/select.md`
- Create: `docs/zh/filters.md`
- Create: `docs/zh/ordering-and-paging.md`
- Create: `docs/zh/insert.md`
- Create: `docs/zh/update.md`
- Create: `docs/zh/delete.md`
- Create: `docs/zh/entity-convenience.md`
- Create: `docs/zh/many-convenience.md`
- Create: `docs/zh/conflict-inserts.md`
- Create: `docs/zh/table-overrides.md`
- Create: `docs/zh/mapping.md`
- Create: `docs/zh/dialects.md`
- Create: `docs/zh/execution-options.md`
- Create: `docs/zh/testing.md`
- Create: `docs/zh/limitations.md`
- Create: `docs/zh/changelog.md`

- [ ] Mirror the English route set.
- [ ] Use Chinese task labels and explanations.
- [ ] Keep C# examples consistent with English pages.

### Task 4: Verify

**Files:**
- Inspect: generated docs and git diff

- [ ] Run `pnpm run docs:build`.
- [ ] Run `dotnet test`.
- [ ] Run `git diff --check`.
- [ ] Fix any docs build, formatting, or test issue before reporting completion.
