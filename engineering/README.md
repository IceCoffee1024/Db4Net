# Engineering Notes

This directory contains internal project context. It is not part of the public VitePress documentation site.

## Directory Layout

- `decisions/`
  - Long-lived or historical design decisions.
  - These files explain why Db4Net took a direction, but may not reflect every current API detail.
- `superpowers/specs/`
  - Feature or project design specs.
  - Specs describe intended behavior before implementation.
- `superpowers/plans/`
  - Execution plans derived from specs.
  - Plans are task-oriented and may reference files as they existed at the time.
- `release-checklist.md`
  - Current release preparation checklist.

## Documentation Boundary

- `docs/` is the public VitePress documentation site.
- `README.md` is the GitHub landing page.
- `src/Db4Net/README.md` is the NuGet package README.
- `CHANGELOG.md` is the authoritative release changelog.
- `docs/changelog.md` and `docs/zh/changelog.md` are documentation-site views of release history.

For current public API behavior, prefer README files, VitePress docs, changelog entries, and tests over older engineering notes.
