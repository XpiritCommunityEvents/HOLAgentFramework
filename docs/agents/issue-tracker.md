# Issue tracker: GitHub

Issues and specs for this repo live as GitHub issues. Use the `gh` CLI for all operations.

## Conventions

- **Create an issue**: `gh issue create --title "..." --body "..."`. Use a heredoc for multi-line bodies.
- **Read an issue**: `gh issue view <number> --json number,title,body,labels,comments`.
- **List issues**: `gh issue list --state open --json number,title,body,labels,comments --jq '[.[] | {number, title, body, labels: [.labels[].name], comments: [.comments[].body]}]'` with appropriate `--label` and `--state` filters.
- **Make an issue a sub-issue of a parent**: `gh issue create --parent <parent> ...`, or `gh issue edit <parent> --add-sub-issue <child>` afterwards. For older `gh`, use the GitHub REST API. Without sub-issues, put `Part of #<parent>` at the top of the child body.
- **Blocking**: Prefer GitHub's native issue dependencies. Use `gh issue create --blocked-by <n>,<n> ...` or `gh issue edit <child> --add-blocked-by <blocker>`. Where dependencies aren't available, put `Blocked by: #<n>, #<n>` at the top of the child body. A ticket is unblocked when every blocker is closed.
- **Comment on an issue**: `gh issue comment <number> --body "..."`.
- **Apply / remove labels**: `gh issue edit <number> --add-label "..."` / `--remove-label "..."`.
- **Close**: Post the explanation with `gh issue comment <number> --body "..."`, then run `gh issue close <number>`.

Infer the repo from `git remote -v`; `gh` does this automatically when run inside a clone. File issues only into the user's own repo. Never file into `mattpocock/skills` unless the user owns it. In a fork, check the target with `gh repo view --json nameWithOwner` before the first issue creation and confirm with the user if it is not their repo.

## Pull requests as a triage surface

**PRs as a request surface: no.** Set this to `yes` only if this repo treats external PRs as feature requests.

## When a skill says "publish to the issue tracker"

Create a GitHub issue.

## When a skill says "fetch the relevant ticket"

Read it with `gh issue view <number> --json number,title,body,labels,comments`.
