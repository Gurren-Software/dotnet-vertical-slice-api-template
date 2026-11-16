# PR Review Template

Read `CLAUDE.md` / `AGENTS.md` first: the project conventions there override anything generic below.

## Review Output Format

```
## Summary
What the PR does, in 1-2 sentences.

## Verdict: ✅ APPROVED | ⚠️ APPROVED WITH SUGGESTIONS | ❌ CHANGES REQUESTED

## Issues Found

### 🔴 Blocking (must fix before merge)
- [file:line] Issue and why it blocks

### 🟡 Suggestions (recommended, not blocking)
- [file:line] Suggestion

### 🟢 Nitpicks (optional)
- [file:line] Nitpick

## Architecture Compliance
- [ ] Follows the project's slice / layer structure
- [ ] Commands and queries separated
- [ ] Result pattern for expected failures (no exception-driven flow)
- [ ] No cross-feature coupling

## Security Checklist
- [ ] Input validated (validator + handler guards)
- [ ] Auth / authorization on new endpoints
- [ ] No raw SQL built from input
- [ ] No secrets, tokens or connection strings in code

## Data & Performance
- [ ] Migration present when the model changed, and safe on existing data
- [ ] No N+1 queries, AsNoTracking on reads, projections instead of entities
- [ ] async/await and CancellationToken passed through

## Tests
- [ ] New behavior covered (unit and/or integration)
- [ ] Smoke tests (`tests/smoke`) updated if a public endpoint changed
```

## Review Rules

- Be concise and actionable. No vague feedback.
- Only flag real issues. Formatting and analyzers are already enforced by CI.
- Pure refactor with no behavior change: focus on correctness and architecture.
- Zero issues: say so and approve.
- Always use the output format above, with the `## Verdict:` line exactly as written.
