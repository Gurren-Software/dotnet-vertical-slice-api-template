# Vertical Slice Architecture Template

A pragmatic Vertical Slice Architecture starter for **.NET 10**. A single Web API project, organized by feature instead of by layer. Batteries included, opinionated where it matters, and easy to extend.

## What's included in the template?

A single `src/Web.Api` project (root namespace `Web.Api`) with everything organized around features:

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
  - Unit testing (`Web.Api.UnitTests`)
  - Integration testing with **Testcontainers** (`IntegrationTests`)

## Getting started

```bash
docker compose up -d        # PostgreSQL + Grafana/Loki/Tempo
dotnet run --project src/Web.Api
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
