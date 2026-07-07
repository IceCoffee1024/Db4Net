# VitePress Documentation Site Design

## Goal

Build a public, bilingual VitePress documentation site for Db4Net under `docs/vitepress/`, with English as the root language and Simplified Chinese under `docs/vitepress/zh/`.

## Context

`docs/vitepress/` now belongs to public user documentation. AI/superpowers plans and specs live under `docs/superpowers/`. Internal engineering decisions and release process notes live under `docs/engineering/`. The VitePress site was initialized with:

- Site root: `docs/vitepress`
- Config: `docs/vitepress/.vitepress/config.mts`
- npm scripts: `docs:dev`, `docs:build`, `docs:preview`
- Package manager: `pnpm`

## Official VitePress References

The design follows these VitePress documents requested for this work:

- `https://vitepress.dev/zh/guide/getting-started`
- `https://vitepress.dev/zh/guide/routing`
- `https://vitepress.dev/zh/guide/markdown`
- `https://vitepress.dev/zh/guide/asset-handling`
- `https://vitepress.dev/zh/guide/frontmatter`
- `https://vitepress.dev/zh/guide/using-vue`
- `https://vitepress.dev/zh/guide/i18n`
- `https://vitepress.dev/zh/reference/site-config`

## Documentation Architecture

Use VitePress' file-based routing:

- English pages live directly under `docs/vitepress/`.
- Chinese pages live under `docs/vitepress/zh/`.
- `docs/vitepress/index.md` is the English home page.
- `docs/vitepress/zh/index.md` is the Chinese home page.

The initial page set is intentionally task-oriented instead of source-module-oriented:

```text
docs/vitepress/
  index.md
  getting-started.md
  select.md
  filters.md
  ordering-and-paging.md
  insert.md
  update.md
  delete.md
  entity-convenience.md
  many-convenience.md
  conflict-inserts.md
  table-overrides.md
  mapping.md
  dialects.md
  execution-options.md
  testing.md
  limitations.md
  changelog.md
  zh/
    index.md
    getting-started.md
    select.md
    filters.md
    ordering-and-paging.md
    insert.md
    update.md
    delete.md
    entity-convenience.md
    many-convenience.md
    conflict-inserts.md
    table-overrides.md
    mapping.md
    dialects.md
    execution-options.md
    testing.md
    limitations.md
    changelog.md
```

## Navigation

Use English as the default locale and Simplified Chinese as the secondary locale:

- `locales.root`: English
- `locales.zh`: Simplified Chinese

The sidebar should group pages by user workflow:

- Introduction
- Basic Usage
- Advanced Usage
- Reference

This keeps the site aligned with how users discover a SQL builder: install, query, mutate data, then inspect mapping/dialects/options/limits.

## Content Rules

- Root `README.md` remains the concise GitHub/NuGet landing page.
- `src/Db4Net/README.md` remains the NuGet package README.
- VitePress pages become the full user manual.
- Scaffold pages from `pnpm vitepress init` are removed or replaced.
- English and Chinese pages should cover the same route set even if the first pass is concise.
- API examples must reflect the current Db4Net API surface:
  - SQL-shaped builders: `SelectFrom<T>()`, `InsertInto<T>()`, `Update<T>()`, `DeleteFrom<T>()`
  - Entity conveniences: `Insert(entity)`, `Update(entity)`, `Delete(entity)`
  - Many conveniences: `InsertMany`, `UpdateMany`, `DeleteMany`
  - Conflict inserts: `InsertOrIgnore`, `InsertOrUpdate`, and many variants
  - Filter grouping: `WhereGroup`, `OrWhereGroup`
  - String field names are CLR property names, not database column names or SQL fragments

## Markdown And Assets

- Use plain Markdown first.
- Use fenced C# and SQL code blocks for examples.
- Use VitePress containers such as `::: tip` and `::: warning` for safety rules and limitations.
- Do not add custom Vue components in the first pass.
- If assets are needed later, put public assets under `docs/vitepress/public/`.
- Keep `docs/vitepress/.vitepress/dist/` and `docs/vitepress/.vitepress/cache/` ignored by git.

## Out Of Scope For This Pass

- Custom theme work
- DocFX-generated API reference
- Search integration
- Versioned docs
- Automated README-to-docs synchronization
- Duplicating the root `CHANGELOG.md` into the VitePress site
- Complex Vue components or interactive examples

## Verification

Run:

```bash
pnpm run docs:build
dotnet test
git diff --check
```

`pnpm run docs:build` is the primary verification for the VitePress site. `dotnet test` ensures the documentation work did not accidentally change project behavior.
