# Documentation Workspace

This directory is the project documentation workspace.

## Layout

- `vitepress/`
  - Public VitePress documentation site.
  - Built by `npm run docs:build`.
- `superpowers/`
  - AI/superpowers-generated specs and implementation plans.
  - These files preserve project memory for future planning and implementation work.
- `product/`
  - Product notes, PRDs, prototypes, user stories, and early requirement drafts.
  - These files describe product intent before it becomes an engineering spec or implementation plan.
- `engineering/`
  - Internal engineering decisions, historical notes, and release process documents.
  - `engineering/decisions/`: long-lived or historical design decisions.
  - `engineering/release-checklist.md`: release preparation checklist.

## Documentation Boundary

Only `docs/vitepress/` is used as the VitePress site root. Files under `docs/superpowers/`, `docs/product/`, and `docs/engineering/` are internal project documentation and are not part of the public documentation site.

- `README.md` is the GitHub landing page.
- `src/Db4Net/README.md` is the NuGet package README.
- `CHANGELOG.md` is the authoritative release changelog.
- `docs/vitepress/changelog.md` and `docs/vitepress/zh/changelog.md` are documentation-site views of release history.

For current public API behavior, prefer README files, VitePress docs, changelog entries, and tests over older engineering notes.
