# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## MCP Tools: code-review-graph

**IMPORTANT: This project has a knowledge graph. ALWAYS use the
code-review-graph MCP tools BEFORE using Grep/Glob/Read to explore
the codebase.** The graph is faster, cheaper (fewer tokens), and gives
you structural context (callers, dependents, test coverage) that file
scanning cannot.

### When to use graph tools FIRST

- **Exploring code**: `semantic_search_nodes` or `query_graph` instead of Grep
- **Understanding impact**: `get_impact_radius` instead of manually tracing imports
- **Code review**: `detect_changes` + `get_review_context` instead of reading entire files
- **Finding relationships**: `query_graph` with callers_of/callees_of/imports_of/tests_for
- **Architecture questions**: `get_architecture_overview` + `list_communities`

Fall back to Grep/Glob/Read **only** when the graph doesn't cover what you need.

### Key Tools

| Tool | Use when |
| ------ | ---------- |
| `detect_changes` | Reviewing code changes — gives risk-scored analysis |
| `get_review_context` | Need source snippets for review — token-efficient |
| `get_impact_radius` | Understanding blast radius of a change |
| `get_affected_flows` | Finding which execution paths are impacted |
| `query_graph` | Tracing callers, callees, imports, tests, dependencies |
| `semantic_search_nodes` | Finding functions/classes by name or keyword |
| `get_architecture_overview` | Understanding high-level codebase structure |
| `refactor_tool` | Planning renames, finding dead code |

### Workflow

1. The graph auto-updates on file changes (via hooks).
2. Use `detect_changes` for code review.
3. Use `get_affected_flows` to understand impact.
4. Use `query_graph` pattern="tests_for" to check coverage.

## Build & Test Commands

```bash
# Restore and build the entire solution
dotnet restore
dotnet build

# Run all tests (must be sequential — see build/tests.runsettings)
dotnet test --no-build -s build/tests.runsettings

# Run a single test class or method
dotnet test --no-build -s build/tests.runsettings --filter "FullyQualifiedName~Piranha.Tests.Pages"

# Run with coverage
dotnet test --no-build -s build/tests.runsettings --verbosity quiet \
  /p:CollectCoverage=true /p:CoverletOutputFormat="json,lcov" \
  /p:CoverletOutput=../../coverage/lcov

# Run the MvcWeb example app
cd examples/MvcWeb && dotnet run
```

Tests use SQLite (`./piranha.tests.db`) and **must run sequentially** — `build/tests.runsettings` enforces `MaxCpuCount=1` and `DisableParallelization=true`. Do not remove these settings.

### Manager UI Assets (Vue 2 + Gulp)

The Manager UI compiles `.vue` files via a custom Babel-based Gulp pipeline (not Vue CLI):

```bash
cd core/Piranha.Manager
npm install
gulp min:js     # compile Vue components + bundle JS
gulp min:css    # bundle CSS
gulp rtl:min:css  # RTL variant
```

Run these after touching any `.vue` or `.scss` file under `core/Piranha.Manager/assets/`.

## Architecture

This is a **security-hardened fork** (v1.0.1) of [PiranhaCMS/piranha.core](https://github.com/PiranhaCMS/piranha.core), a decoupled CMS for ASP.NET Core. Targets **net8.0 / net9.0 / net10.0** (multi-targeted via `Directory.Build.props`).

### Top-level layout

```
core/       Core NuGet packages (CMS logic, ASP.NET integration, Manager UI)
data/       EF Core data providers (SQLite, SQL Server, MySQL, PostgreSQL)
identity/   ASP.NET Core Identity integrations (one per DB provider)
test/       xUnit test projects
examples/   MvcWeb and RazorWeb runnable demos
build/      runsettings and CI helpers
```

### Core service layer (`core/Piranha/`)

`IApi` is the **single entry point** for all CMS operations. It exposes typed service properties:

| Property | Service | Manages |
|---|---|---|
| `Pages` | `IPageService` | Pages and page hierarchy |
| `Posts` | `IPostService` | Blog posts |
| `Sites` | `ISiteService` | Multi-site |
| `Media` | `IMediaService` | File/image storage |
| `Content` | `IContentService` | Generic content |
| `Archives` | `IArchiveService` | Post archives |
| `Aliases` | `IAliasService` | URL redirects |
| `Params` | `IParamService` | Key/value settings |
| `Languages` | `ILanguageService` | Localization |
| `*Types` | `IXxxTypeService` | Content type definitions |

Each service delegates to an `IXxxRepository` interface. The `data/Piranha.Data.EF.*` packages implement those repositories against EF Core.

### Content type system (`core/Piranha/Extend/`)

Content types (pages, posts, sites) are defined in C# via attribute decorators and registered at startup via `Piranha.AttributeBuilder`. Fields (`IField`) and Blocks (`Block`) are the extension points. Regions group fields within a content type.

### ASP.NET Core integration (`core/Piranha.AspNetCore/`)

Uses `builder.AddPiranha(options => ...)` / `app.UsePiranha(...)` as the setup API. Routing resolves CMS content via middleware before MVC. `ModelLoader` gates draft/preview access — **auth bypass fixes here are security-critical**.

### Manager UI (`core/Piranha.Manager/`)

- Backend: ASP.NET Core controllers under `Areas/Manager/`
- Frontend: Vue 2.x components (`.vue` files) compiled by the custom `gulpfile.js` into a single `piranha.min.js` bundle. No hot-reload; rebuild with Gulp after changes.
- Auth: `Piranha.Manager.LocalAuth` provides a simple local username/password login; `Piranha.AspNetCore.Identity.*` packages provide full ASP.NET Identity.

### Security notes (fork-specific)

These files carry security-critical patches — review them carefully before changing:

| File | Fix |
|---|---|
| `core/Piranha.AspNetCore/Security/ModelLoader.cs` | Explicit `return null` on auth failure; hardened draft-state check |
| `core/Piranha.Manager/Controllers/AuthController.cs` | `Secure=true`, `SameSite=Strict` on XSRF cookie |
| `examples/MvcWeb/Controllers/CmsController.cs` | `[ValidateAntiForgeryToken]` on `SavePostComment` |
