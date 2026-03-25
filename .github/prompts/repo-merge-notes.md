# Repo-specific merge notes for vs-solutionpersistence

This file contains notes specific to this repo that help resolve Library.Template merge conflicts.
It is referenced by `update-library-template.prompt.md` and kept separate to avoid merge conflicts with template-owned files.

## Files deleted locally that the template will try to update

These show as "deleted by us" conflicts. Here are the known cases:

* **GitHub Actions CI files** (`.github/workflows/docs.yml`, `.github/workflows/docs_validate.yml`, `.github/actions/publish-artifacts/action.yaml`): This repo uses Azure Pipelines, not GitHub Actions for CI/CD. Keep these deleted.
* **`.github/workflows/libtemplate-update.yml`**: Automatic Library.Template merge workflow. Should be **kept/restored** — it was mistakenly deleted in a past merge and is useful infrastructure.
* **`.github/renovate.json`**: Dependency update automation. Should be kept/restored if missing.
* **`test/Library.Tests/Library.Tests.csproj`**: Template's generic test project. Re-delete it, but apply any relevant changes to `test/Microsoft.VisualStudio.SolutionPersistence.Tests/`.

## `azure-pipelines/build.yml`

The template includes `expand-template.yml` references in build.yml (Windows, Linux, macOS jobs).
This repo is not a template and does not have `Expand-Template.ps1`, so always remove these lines after accepting template changes:
```yaml
  - template: expand-template.yml
```

## `azure-pipelines/vs-insertion.yml`

The template has a placeholder for `InsertionReviewers`. This repo customizes it to include the team:
```yaml
InsertionReviewers: $(Build.RequestedFor),VS Core - Solution Experience
```
When merging, keep the team name but accept any other new properties the template adds (e.g. `CustomScriptExecutionCommand`).

## Researching template changes

When a conflict is hard to understand, check the Library.Template commit history for the specific file:
```
https://github.com/aarnott/Library.Template/commits/microbuild/<path/to/file>
```
This shows *why* changes were made and helps decide whether to accept them.
You can also compare the template's full current version of a file:
```bash
git show libtemplate/microbuild:<path/to/file>
```

## History of past merge mistakes

The commit `2fb9c3d` ("Remove github actions", Feb 2025) bulk-deleted all GitHub workflow files.
Some were genuinely unused (GitHub Actions CI, GitHub Pages deployment), but it also deleted useful automation:
* `.github/workflows/libtemplate-update.yml` — Automatic weekly Library.Template merge PRs
* `.github/workflows/docs_validate.yml` — Docfx link validation
* `.github/renovate.json` — Renovate bot dependency updates

These have been restored. If they show up as conflicts again in future merges, keep them.
