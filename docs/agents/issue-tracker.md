# Issue tracker: GitHub, Gurren issue system

Work is tracked as GitHub issues across the `Gurren-Software` org, on one org board ("Gurren Roadmap", project 1). Use the `gh` CLI. The full system is in `Gurren-Software/roadmap` > `docs/issue-system.md`.

## Where issues go

- **Epics and User Stories** live in `Gurren-Software/roadmap`, never in this repo.
- **Tasks and Bugs** for this repo's code live in this repo.
- A Task is a **sub-issue of a User Story** in `roadmap` (except standalone infra or docs work).
- Every issue has an **issue type**: `Epic`, `User Story`, `Task` or `Bug`.
- Titles carry a prefix: `[EPIC]:`, `[US]:`, `[BACKEND]:`, `[FRONTEND]:`, `[MOBILE]:`, `[INFRA]:`, `[DESIGN]:`, `[DOCS]:`, `[BUG]:`.
- Bodies use the `###` headings of the org issue forms (see the `gurren-issue-planner` skill, `references/templates.md`). Automations read them to set the Product, Surface and Origin fields. Never set those fields or the board Status by hand.

For planning a feature end to end (Epic, stories per client, tasks per surface), prefer the `gurren-issue-planner` skill.

## Conventions

- **Create an issue with its type**: `gh api repos/Gurren-Software/<repo>/issues -f title='[BACKEND]: ...' -f type=Task -f body="$BODY" --jq .number`
- **Read an issue**: `gh issue view <number> --json number,title,body,labels,comments`. For an issue in another repo, add `--repo Gurren-Software/<repo>`.
- **List issues**: `gh issue list --state open --json number,title,body,labels` with `--label` / `--state` filters.
- **Sub-issue of a parent**: `gh api -X POST repos/Gurren-Software/<parent-repo>/issues/<parent>/sub_issues -F sub_issue_id=<child-db-id>`, where the database id comes from `gh api repos/Gurren-Software/<repo>/issues/<n> --jq .id`.
- **Blocking**: native issue dependencies. `gh api -X POST repos/Gurren-Software/<repo>/issues/<child>/dependencies/blocked_by -F issue_id=<blocker-db-id>`. A ticket is unblocked when every blocker is closed.
- **Count open blockers**: `gh api repos/Gurren-Software/<repo>/issues/<n> --jq .issue_dependencies_summary.blocked_by`.
- **Comment**: `gh issue comment <number> --body "..."`.
- **Close**: comment the explanation, then `gh issue close <number>`. Use `--reason "not planned"` for anything not done.
- **Cross-repo references** always use the full form: `Gurren-Software/roadmap#12`.

File issues only into `Gurren-Software` repos, never into the repos the skills come from.

## Pull requests as a triage surface

**PRs as a request surface: no.**

## When a skill says "publish to the issue tracker"

- A spec (from `/to-spec`): a User Story or an Epic in `roadmap`, depending on size. Long specs go to `roadmap/specs/<product>/<slug>.md`, linked from the Epic.
- Tickets (from `/to-tickets`): one `Task` per ticket in the repo that owns the code, each a sub-issue of the User Story it implements, with native "blocked by" links between them. Use the Task body headings (Surface, Goal, Context, Acceptance criteria, Verification, Out of scope). Do not add triage labels: new issues land in the board's Triage column on their own.

## When a skill says "fetch the relevant ticket"

Read it as in **Read an issue** above, following the parent User Story in `roadmap` when the ticket is a Task.
