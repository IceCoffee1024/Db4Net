# VitePress Documentation Site Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build the first bilingual VitePress documentation skeleton for Db4Net.

**Architecture:** `docs/vitepress/` is the public VitePress site root. English routes live at the root; Simplified Chinese routes live under `docs/vitepress/zh/`. AI/superpowers plans and specs live under `docs/superpowers/`; internal engineering decisions and release process notes live under `docs/engineering/`.

**Tech Stack:** VitePress 2 alpha, pnpm, Markdown, TypeScript config.

---

### Task 1: Configure VitePress

**Files:**
- Modify: `docs/vitepress/.vitepress/config.mts`
- Delete: `docs/vitepress/api-examples.md`
- Delete: `docs/vitepress/markdown-examples.md`

- [ ] Replace scaffold navigation with Db4Net navigation.
- [ ] Configure English root locale and Simplified Chinese `zh` locale.
- [ ] Add matching English and Chinese sidebar groups.
- [ ] Point GitHub social link to `https://github.com/IceCoffee1024/Db4Net`.
- [ ] Remove VitePress scaffold example pages.

### Task 2: Create English Documentation Pages

**Files:**
- Modify: `docs/vitepress/index.md`
- Create: `docs/vitepress/getting-started.md`
- Create: `docs/vitepress/select.md`
- Create: `docs/vitepress/filters.md`
- Create: `docs/vitepress/ordering-and-paging.md`
- Create: `docs/vitepress/insert.md`
- Create: `docs/vitepress/update.md`
- Create: `docs/vitepress/delete.md`
- Create: `docs/vitepress/entity-convenience.md`
- Create: `docs/vitepress/many-convenience.md`
- Create: `docs/vitepress/conflict-inserts.md`
- Create: `docs/vitepress/table-overrides.md`
- Create: `docs/vitepress/mapping.md`
- Create: `docs/vitepress/dialects.md`
- Create: `docs/vitepress/execution-options.md`
- Create: `docs/vitepress/testing.md`
- Create: `docs/vitepress/limitations.md`
- Create: `docs/vitepress/changelog.md`

- [ ] Replace the scaffold home page with a Db4Net entry page.
- [ ] Pull concise examples from `README.md` and `src/Db4Net/README.md`.
- [ ] Keep pages accurate to the current public API.
- [ ] Use VitePress containers for safety notes.

### Task 3: Create Simplified Chinese Documentation Pages

**Files:**
- Create: `docs/vitepress/zh/index.md`
- Create: `docs/vitepress/zh/getting-started.md`
- Create: `docs/vitepress/zh/select.md`
- Create: `docs/vitepress/zh/filters.md`
- Create: `docs/vitepress/zh/ordering-and-paging.md`
- Create: `docs/vitepress/zh/insert.md`
- Create: `docs/vitepress/zh/update.md`
- Create: `docs/vitepress/zh/delete.md`
- Create: `docs/vitepress/zh/entity-convenience.md`
- Create: `docs/vitepress/zh/many-convenience.md`
- Create: `docs/vitepress/zh/conflict-inserts.md`
- Create: `docs/vitepress/zh/table-overrides.md`
- Create: `docs/vitepress/zh/mapping.md`
- Create: `docs/vitepress/zh/dialects.md`
- Create: `docs/vitepress/zh/execution-options.md`
- Create: `docs/vitepress/zh/testing.md`
- Create: `docs/vitepress/zh/limitations.md`
- Create: `docs/vitepress/zh/changelog.md`

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
