# Triage roles

The skills speak of five canonical triage roles. In the Gurren system, workflow state lives on the board (Status field), not in labels. This table maps each role to what to do.

| Role in the skills | In the Gurren tracker | How to apply |
|---|---|---|
| `needs-triage` | board Status **Triage** | nothing: new issues land there automatically |
| `needs-info` | label `needs-info` | `gh issue edit <n> --add-label needs-info` |
| `ready-for-agent` | board Status **Ready** | move the board item (see below) |
| `ready-for-human` | board Status **Ready** | move the board item (see below); explain in a comment why it needs a human |
| `wontfix` | closed as not planned | `gh issue close <n> --reason "not planned"` with a comment |

Agents and humans pick from the same **Ready** column: any Ready Task is executable by either.

Category roles map to **issue types**: `bug` is type `Bug`, `enhancement` is type `Task` (or `User Story` in `roadmap`). Set a type with `gh api -X PATCH repos/Gurren-Software/<repo>/issues/<n> -f type=Bug`.

## Moving a board item

```bash
ITEM=$(gh project item-list 1 --owner Gurren-Software --format json --limit 500 \
  --jq '.items[] | select(.content.url=="https://github.com/Gurren-Software/<repo>/issues/<n>") | .id')
FIELD=$(gh project field-list 1 --owner Gurren-Software --format json --jq '.fields[] | select(.name=="Status") | .id')
OPTION=$(gh project field-list 1 --owner Gurren-Software --format json \
  --jq '.fields[] | select(.name=="Status") | .options[] | select(.name=="Ready") | .id')
PROJECT=$(gh project view 1 --owner Gurren-Software --format json --jq .id)
gh project item-edit --id "$ITEM" --field-id "$FIELD" --single-select-option-id "$OPTION" --project-id "$PROJECT"
```

Requires the `project` scope on `gh` (`gh auth refresh -s project`).
