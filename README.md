# VerticalSlice

ASP.NET Core minimal API on **.NET 10**, organized by feature with Vertical Slice Architecture. One project, one file per use case, PostgreSQL, JWT auth, observability and tests ready from the first commit.

<!--#if (false) -->
This repo is the Gurren Software API template. You don't clone it to start a project: you install it once and run `dotnet new gurren-vsa`. You get a renamed project with the CI pipelines, the Docker setup, the coding conventions and the Claude Code skills.

## Use the template

### Install

The package lives on the Gurren Software GitHub Packages feed. Create a GitHub token with the `read:packages` scope only, export it, then register the feed once:

```bash
export GH_PACKAGES_TOKEN=<token>
dotnet nuget add source https://nuget.pkg.github.com/Gurren-Software/index.json \
  --name gurren --username <github-user> --password "%GH_PACKAGES_TOKEN%" \
  --store-password-in-clear-text
dotnet new install GurrenSoftware.Templates.VerticalSliceApi
```

NuGet stores `%GH_PACKAGES_TOKEN%`, the variable name, and reads the token from your environment at each call. Keep the `export` in your shell profile or secret manager.

Update to the latest version with `dotnet new update`. To try local changes to the template, install from a clone: `dotnet new install ./ --force`.

### Create a project

```bash
dotnet new gurren-vsa -n Taxo
cd Taxo
dotnet ef migrations add Initial --project src/Taxo.Api --startup-project src/Taxo.Api --output-dir Database/Migrations
docker compose up -d
dotnet run --project src/Taxo.Api
```

| Option | Default | Effect |
|---|---|---|
| `-n <Name>` | folder name | `VerticalSlice.Api` becomes `<Name>.Api`: folders, namespaces, assembly, `.slnx`. The lowercase form (`taxo`, `acme-billing`) names the database, the JWT issuer, the Docker image and the compose project. |
| `--sample` | `false` | Keeps the Todos feature, its tests and its migrations. Without it you start with Users and auth only, and you create the first migration yourself. |

### Work on the template

- `main` holds the released template, `dev` collects the next changes. Open PRs against `dev`.
- Every PR runs two workflows. `Build` builds and tests this repo. `Template` generates a project with and without the sample, checks no template name is left, then builds and tests both.
- Template-only blocks (HTML or XML comments with an `if false` condition, in Markdown and project files) only exist in this repo. `dotnet new` strips them.

### Release

Merge `dev` into `main`, then push a tag:

```bash
git tag -a v1.1.0 -m "v1.1.0"
git push origin v1.1.0
```

The `Template` workflow runs the checks, packs `GurrenSoftware.Templates.VerticalSliceApi` with the tag's version and publishes it to GitHub Packages.

<!--#endif -->
## What's inside

A single `src/VerticalSlice.Api` project, root namespace `VerticalSlice.Api`:

- **Features** (`Features/{Entity}/`): each use case is one file. A `public static class {UseCase}` nests the command or query, its validator, its handler and its endpoint.
- **CQRS without MediatR**: small `ICommand`/`IQuery` and handler interfaces (`Common/Messaging/`), registered by Scrutor assembly scanning.
- **Cross-cutting behaviors**: logging and validation run as decorators (`Common/Behaviors/`). Expected failures return `Result`/`Error` instead of throwing.
- **Persistence** (`Database/`): EF Core and PostgreSQL with snake_case naming and migrations. Handlers inject `ApplicationDbContext` directly, with no repository layer. A dispatcher publishes domain events.
- **Authentication**: JWT with refresh-token rotation.
- **Authorization**: permission-based policies.
- **Caching**: HybridCache with per-feature keys and invalidation.
- **Web API**: auto-discovered minimal API endpoints, rate limiting, idempotency keys, security headers, `ProblemDetails` error responses, Scalar API reference with JWT support.
- **Observability**: Serilog structured logs and OpenTelemetry traces and metrics, shipped over OTLP to a bundled Grafana, Loki, Tempo and Prometheus stack (`grafana/otel-lgtm`).
- **Tests**: unit tests (`VerticalSlice.Api.UnitTests`), integration tests on a real PostgreSQL with Testcontainers (`IntegrationTests`), architecture tests (`ArchitectureTests`).
- **CI**: GitHub Actions builds and tests every push and PR on `main` and `dev`. Dependabot keeps NuGet packages up to date.
- **Claude Code**: `CLAUDE.md` describes the conventions, and `.claude/skills/` adds `/add-feature`, `/add-entity`, `/add-tests`, `/add-migration`, `/add-background-job` and `/vsa-review`.
- **Gurren agent setup**: `.claude/settings.json` enables the shared `gurren` and `mattpocock-skills` plugins, `docs/agents/` describes the issue system, and `roadmap-automation.yml` syncs issues with the org board. Set the `ROADMAP_APP_ID` and `ROADMAP_APP_KEY` secrets on the new repo. Run `/gurren:gurren-repo-setup` later to bring a project back in line with the shared setup.

## Getting started

You need the .NET 10 SDK and Docker.

```bash
docker compose up -d                          # PostgreSQL + Grafana stack
dotnet run --project src/VerticalSlice.Api
```

In Development, the app applies pending migrations on startup. If `src/VerticalSlice.Api/Database/Migrations` is empty, create the first one:

```bash
dotnet tool install --global dotnet-ef
dotnet ef migrations add Initial --project src/VerticalSlice.Api --startup-project src/VerticalSlice.Api --output-dir Database/Migrations
```

Grafana runs at http://localhost:3000.

Run the tests. Docker must run, since the integration tests start a throwaway PostgreSQL container:

```bash
dotnet test
```

To target .NET 8 or .NET 9, follow the notes in `Directory.Build.props`.

## Credits

The Gurren Software Vertical Slice template, the base of this project, builds on the work of [Milan Jovanović](https://milanjovanovic.tech), whose templates and [Pragmatic Clean Architecture](https://milanjovanovic.tech/pragmatic-clean-architecture) course shaped its structure and many of its building blocks.

>Built with passion. Crafted for impact.