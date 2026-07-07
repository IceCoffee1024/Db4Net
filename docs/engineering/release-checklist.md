# Release Checklist

Use this checklist before publishing a Db4Net prerelease or stable package.

## 1. Version Metadata

- [ ] Update `src/Db4Net/Db4Net.csproj`:
  - [ ] `Version`
  - [ ] `PackageReleaseNotes`
- [ ] Update `tests/Db4Net.Tests/PackageMetadataTests.cs` so package metadata assertions match the new version and release notes.

## 2. Changelog

- [ ] Update root `CHANGELOG.md`.
  - This is the authoritative changelog for release preparation, GitHub, and NuGet package contents.
- [ ] Sync `docs/vitepress/changelog.md`.
  - Keep it suitable for the English VitePress documentation site.
- [ ] Sync `docs/vitepress/zh/changelog.md`.
  - Keep it suitable for the Chinese VitePress documentation site.

## 3. Readme And Documentation

- [ ] Update root `README.md`.
- [ ] Update package README at `src/Db4Net/README.md`.
- [ ] Update affected VitePress pages under `docs/vitepress/` and `docs/vitepress/zh/`.
- [ ] Check that examples use the current public API names.
- [ ] Check that provider-specific caveats are documented when SQL differs by database.

## 4. Verification

- [ ] Run the default test suite:

```bash
dotnet test
```

- [ ] Build the VitePress documentation:

```bash
npm run docs:build
```

- [ ] For release confidence, build the package in Release mode:

```bash
dotnet pack src/Db4Net/Db4Net.csproj -c Release -o artifacts/packages
```

- [ ] If external database environment variables are configured, confirm provider integration tests run instead of being skipped.

## 5. Pre-Publish Review

- [ ] Inspect the git diff:

```bash
git diff
git status --short
```

- [ ] Confirm no generated test output, local secrets, or temporary files are included.
- [ ] Confirm the package version has not already been published to NuGet.
- [ ] Confirm breaking changes are explicitly called out in changelog and docs.

## 6. Publish

- [ ] Publish the `.nupkg` to NuGet:

```bash
dotnet nuget push artifacts/packages/Db4Net.<version>.nupkg --source https://api.nuget.org/v3/index.json --api-key <NUGET_API_KEY>
```

- [ ] Do not push `.snupkg` manually unless the publishing flow requires it; NuGet normally accepts the symbols package alongside the main package when configured.
- [ ] Deploy the static documentation site after the package is available.

## 7. Post-Publish

- [ ] Verify the NuGet package page shows the new version, README, release notes, license, repository URL, and symbols.
- [ ] Verify `https://db4net.icecoffee1024.com` shows the updated documentation.
- [ ] Commit or tag the release state according to the current repository workflow.
