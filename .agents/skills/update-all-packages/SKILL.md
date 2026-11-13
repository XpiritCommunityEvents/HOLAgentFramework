---
name: update-all-packages
description: Update all NuGet packages in this repo (central package management via Directory.Packages.props) while staying on the .NET major version selected by global.json. Use when asked to update, bump, or refresh NuGet/package dependencies.
---

# Update all NuGet packages

Versions live only in the root `Directory.Packages.props`. Do the work with the scripts; don't hand-edit versions.

## Steps

0. **Aspire first** (CLI, Aspire packages, agent skill files):
   `pwsh .agents/skills/update-all-packages/scripts/Update-Aspire.ps1`
   Runs `aspire update --self`, `aspire update` (both with `--channel stable --yes --non-interactive`), then `aspire agent init --skill-locations standard` (only `.agents/skills`; never `all`, which adds `.github`/`.claude`/`.opencode` skills). Pinned packages that `aspire update` bumps (e.g. DevUI preview) are restored automatically. Then check `git diff` for the Aspire-driven changes (including `Directory.Packages.props` and refreshed skill files) and make sure they still respect the SDK major and pins below.
1. **Preview** (dry run, no writes):
   `pwsh .agents/skills/update-all-packages/scripts/Update-Packages.ps1`
2. Review the plan. Pins and reasons are in `scripts/package-policy.json`; add/remove entries there (not in the script) when a pin changes.
3. **Apply**:
   `pwsh .agents/skills/update-all-packages/scripts/Update-Packages.ps1 -Apply`
4. **Verify**:
   `pwsh .agents/skills/update-all-packages/scripts/Verify-Packages.ps1`
   Builds every csproj, runs Module 5 tests, lists vulnerable/deprecated NuGet packages, runs `npm audit`.
5. If a package breaks the build/tests, add it to `pinned` in `package-policy.json` with the reason, revert its line in `Directory.Packages.props`, and rerun from step 3.
6. Report: table of old → new versions, skipped/pinned packages, verify results.

## Rules enforced by the script

- Major version comes from `sdk.version` in `global.json` (currently .NET 10). Never change `global.json`, never install .NET 11.
- SDK-aligned packages (`Microsoft.Extensions.*`, `Microsoft.EntityFrameworkCore*`, `Microsoft.AspNetCore.*`, `System.*`, etc.) and any package already on the SDK major (e.g. `Microsoft.Extensions.AI` 10.x) stay on that major.
- Stable versions only move to stable; prerelease packages are only changed if not pinned.
- Pinned: OpenAI (2.14.0), preview-only Agent Framework hosting/DevUI, Azure.AI.Inference, EF Core OTel instrumentation.

## Notes

- Keep project `PackageReference`s versionless; if a project has a `Version=`/`VersionOverride`, flag it to the user.
- Keep existing XML comments in `Directory.Packages.props` (the script only rewrites `Version` attributes).
- Don't force npm audit fixes that downgrade CopilotKit or bump incompatible majors; the frontend's Zod 3 / math-rendering advisories are known.
- If the OpenAI pin is lifted, also bump `Microsoft.Extensions.AI.OpenAI` and run the Responses streaming contract tests.
- Needs network access to `api.nuget.org`; requires PowerShell 7+ (`pwsh`, works on Windows/macOS/Linux) and dotnet.
