# Vertical Slice Architecture Template

A pragmatic Vertical Slice Architecture starter for **.NET 10**. A single Web API project, organized by feature instead of by layer. Batteries included, opinionated where it matters, and easy to extend.

<!--#if (false) -->
## Use it as a `dotnet new` template

This repo ships as the `gurren-vsa` template. One command gives you a renamed project with the pipelines, Docker setup, conventions and Claude Code skills.

### Install

The package lives on the Gurren Software GitHub Packages feed. Add the feed once with a GitHub token that has `read:packages`:

```bash
dotnet nuget add source https://nuget.pkg.github.com/Gurren-Software/index.json \
  --name gurren --username <github-user> --password <token> --store-password-in-clear-text
dotnet new install GurrenSoftware.Templates.VerticalSliceApi
```

To try local changes, install from a clone: `dotnet new install ./ --force`.

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
| `-n <Name>` | folder name | `VerticalSlice.Api` becomes `<Name>.Api` (folders, namespaces, assembly, `.slnx`). The lowercase form (`taxo`, `acme-billing`) names the database, the Docker image and the compose project. |
| `--sample` | `false` | Keeps the Todos feature, its tests and the sample migrations. Without it you start with Users and auth only, so you create the first migration yourself. |

### Release a new version

Push a tag `vX.Y.Z` on `main`. The `Template` workflow generates a project with and without the sample, builds and tests both, then publishes the package with that version.

<!--#endif -->
## What's included in the template?

A single `src/VerticalSlice.Api` project (root namespace `VerticalSlice.Api`) with everything organized around features:

- **Features** (`Features/{Entity}/`) — each use case is **one file**: a `public static class {UseCase}` with a nested command/query, validator, handler, and endpoint. Sample `Todos` and `Users` features included.
- **CQRS, MediatR-free** — lightweight custom `ICommand`/`IQuery` + `ICommandHandler`/`IQueryHandler` abstractions (`Common/Messaging/`), discovered by Scrutor assembly scanning.
- **Cross-cutting concerns** — logging and validation implemented as decorators (`Common/Behaviors/`); `Result`/`Error` domain primitives and `IEndpoint` conventions in `Common/`.
- **Persistence** (`Database/`) — EF Core + PostgreSQL (snake_case naming, migrations), `ApplicationDbContext` injected directly into handlers (no repository/`IApplicationDbContext` abstraction), entity configurations, and a domain-events dispatcher.
- **Authentication** (`Authentication/`) — JWT authentication with **refresh tokens** (with token rotation).
- **Authorization** (`Authorization/`) — permission-based authorization.
- **HybridCache** for fast, unified caching with cache invalidation.
- **Web API concerns** — Minimal API endpoints (auto-discovered), **rate limiting** (configurable global + authentication policies), **OpenTelemetry** tracing and metrics (ASP.NET Core, HTTP, Npgsql, runtime), global exception handling and `ProblemDetails`, Serilog structured logging, and Scalar API reference (OpenAPI) with JWT support.
- **Grafana + Loki + Tempo + Prometheus** for logs, traces, and metrics, via the
  bundled `grafana/otel-lgtm` image (the app ships telemetry over OTLP)
  - Grafana is available at http://localhost:3000 by default
- **Testing** projects
  - Unit testing (`VerticalSlice.Api.UnitTests`)
  - Integration testing with **Testcontainers** (`IntegrationTests`)

## Getting started

```bash
docker compose up -d        # PostgreSQL + Grafana/Loki/Tempo
dotnet run --project src/VerticalSlice.Api
```

Run the full test suite (the integration tests spin up a throwaway PostgreSQL container, so
Docker must be running):

```bash
dotnet test
```

To target .NET 8 or .NET 9 instead of .NET 10, see the notes in `Directory.Build.props`.

I'm open to hearing your feedback about the template and what you'd like to see in future iterations.

If you're ready to learn more, check out [**Pragmatic Clean Architecture**](https://www.milanjovanovic.tech/pragmatic-clean-architecture?utm_source=ca-template):

- Domain-Driven Design
- Role-based authorization
- Permission-based authorization
- Distributed caching with Redis
- OpenTelemetry
- Outbox pattern
- API Versioning
- Unit testing
- Functional testing
- Integration testing

Stay awesome!
