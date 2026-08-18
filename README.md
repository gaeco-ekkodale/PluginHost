<div align="center">
  <img src="https://raw.githubusercontent.com/gaeco-ekkodale/.github/main/assets/gaeco_logo_horizontal_color.png" width="200" alt="gaeco logo">

  # PluginHost

  <em>The gaeco shell application that discovers, loads and hosts the platform's micro-frontends.</em>

  [![License](https://img.shields.io/badge/license-fair--code-blue.svg)](LICENSE.md)
  [![Version](https://img.shields.io/github/v/release/gaeco-ekkodale/PluginHost)](../../releases)

  [gaeco-ekkodale Organization](https://github.com/gaeco-ekkodale) · [All Repos](https://github.com/orgs/gaeco-ekkodale/repositories)
</div>

---

gaeco (Graphs for Architecture, Engineering, Construction, Operations) is an event-driven microservice platform for BIM data management. It translates external building-industry standards (IFC, IBPDI, Brick Schema, ASHRAE 223 and others) into a shared, versioned classification and relationship model (Guideline + Ontology) and exposes consistent, graph-based building data (Instance) across use cases and departments — without forcing every consumer onto one rigid schema. Built for organizations managing building/portfolio data across disconnected departmental systems (construction, facilities management, leasing, accounting) that need automatic, reliable data propagation instead of manual, error-prone hand-offs.

> This project is licensed under the [Source Available](LICENSE.md). Source code is viewable and usable; commercial use is restricted.

---

## What this application does

The PluginHost is the entry point users see. It is the shell of gaeco's micro-frontend architecture: every functional UI in the platform — access rights, use cases, instance data, platform configuration, the start page — is a self-contained plugin that is developed, deployed and versioned independently, and the PluginHost loads it at runtime.

Its responsibilities:

- **Plugin management**: discovering available plugins and loading them from MinIO
- **Rendering**: mounting each plugin at its route and presenting them in one consistent navigation
- **Authentication**: authenticating the user against Keycloak and performing a token exchange per plugin, so each plugin receives a token scoped to its own backend

Because plugins are only bound at runtime, a new module can be added to a running platform without rebuilding or redeploying the shell.

## Repository Structure

- `Server/Api/`: ASP.NET Core Web API and SignalR hub
- `Server/Domain/`: domain models and contracts
- `Server/Infrastructure/`: EF Core data access and MinIO integration
- `Server/Api.Tests/`, `Server/Infrastructure.Tests/`: unit tests
- `Client/`: React shell application (Module Federation host)
- `_docker/`: Compose definition, env schemas and the App Registry package manifest
- `_docu/`: developer and user documentation
- `_pipeline/`: Azure DevOps CI/CD pipeline definitions
- `build/`: NUKE build scripts

## Tech Stack

- **Backend**: .NET 8, ASP.NET Core, Entity Framework Core, SignalR, AutoMapper, FluentValidation
- **Frontend**: React, TypeScript, Vite, Tailwind CSS, React Router, React OIDC Context, Module Federation
- **Infrastructure**: PostgreSQL, MinIO (plugin storage), Keycloak, Docker
- **Build**: NUKE

## Plugin Integration

Plugins reach the PluginHost in two ways:

- **Uploaded through the PluginManager** into MinIO, from where the shell loads them.
- **Discovered from running containers**: a client container declares its micro-frontend metadata through `app.mfe.*` labels, which the [AppOrchestrator](https://github.com/gaeco-ekkodale/AppOrchestrator) picks up and binds into the PluginHost automatically.

The start page is a special case: the PluginHost selects the start page plugin by substring match on the plugin id and renders it at route `/` in place of its own built-in start page (see `Client/src/pages/hooks/usePlugins.ts` and `Mainpage.tsx`). The [Homepage](https://github.com/gaeco-ekkodale/Homepage) plugin relies on this, and is rendered *without* plugin props.

## Local Development

### Prerequisites

- Docker Desktop
- .NET 8 SDK
- Node.js 20+
- Keycloak and MinIO — see [`_docu/user/01-Installation.md`](_docu/user/01-Installation.md)

### Start with Docker Compose

```bash
cd _docker
docker compose -p pluginhost -f docker-compose.yml -f docker-compose-override.yml up -d
```

Ports are driven by the `PLUGINHOST_*_OUTERPORT` variables in the environment files; the API exposes Swagger at `/swagger`.

### Run the client locally

```bash
cd Client
npm ci
npm run dev
```

## Build and Test

```bash
./build.sh     # Linux/macOS
.\build.ps1    # Windows
```

- Backend tests: `dotnet test` from the repository root
- Frontend lint: `npm run lint` in `Client/`
- Frontend build: `npm run build` in `Client/`

## Documentation

- [Concepts](_docu/developer/01-Concepts.md)
- [Patterns](_docu/developer/02-Patterns.md)
- [Used Technologies](_docu/developer/03-Used-Technologies.md)
- [Data Model](_docu/developer/04-Data-Model.md)
- [Software Architecture](_docu/developer/05-Software-Architecture.md)
- [Installation](_docu/user/01-Installation.md) · [User Manual](_docu/user/02-User-Manual.md)
