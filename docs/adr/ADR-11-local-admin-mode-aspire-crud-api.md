# 11. Local Admin Mode (.NET Aspire Minimal API & Targeted CRUD Write-back Engine)

* **Status:** Accepted
* **Date:** 2026-08-15
* **Deciders:** Developer (`csmacnz`), Wayfinder Agent

---

## Context and Problem Statement

The Curriculum Vitae (CV) web application requires a local Content Management System (CMS) capability so the site owner can visually edit CV content (Profile, Timeline entries, Projects, Skill Matrix) without manually editing `wwwroot/data/cv-datasource.json` in a text editor.

The production app is hosted for free on GitHub Pages as a static Blazor WebAssembly (.NET 8/9/10) application, where server-side write access is unavailable. We need a clean, secure local editing architecture that:
1. Prevents any administrative UI or backend write capability from leaking into static production release builds.
2. Provides explicit, targeted CRUD capabilities for editing individual data domains.
3. Operates entirely locally with zero production hosting or cloud database costs.

---

## Decision Drivers

* **Zero Production Cost & Footprint:** Administrative code and APIs must have zero runtime overhead or security surface area on public static GitHub Pages deployments.
* **Developer Ergonomics:** Editing via visual form controls should feel immediate, structured, and validated against the domain schema.
* **Local Development Safety:** The backend API must execute strictly on `localhost` during development without requiring complex authentication or user management infrastructure.

---

## Considered Options

1. **Conditional Compilation (`#if DEBUG`) + .NET Aspire Minimal API CRUD Endpoints** *(Chosen)*
2. **Separate `CV.Admin` Blazor Server / WebAPI Project**
3. **Single File Blob Overwrite (`POST /api/cv`)**
4. **WebSocket / File Watcher Bi-directional Sync**

---

## Decision Outcome

**Chosen Option:** **Option 1 — Conditional Compilation (`#if DEBUG`) with .NET Aspire Minimal API Targeted CRUD Endpoints.**

When orchestrating local execution via `.NET Aspire`, an ASP.NET Core Minimal API runs on `localhost`. This backend exposes explicit CRUD endpoints targeting each data domain (`Profile`, `SkillGroup`/`Skill`, `TimelineEntry`/`Project`). Upon receiving updates from the local Blazor interface, the API formats and writes updates directly back to `wwwroot/data/cv-datasource.json`.

All administrative routes, components (`/admin`), and API client services in `CV.Web` are wrapped in `#if DEBUG` preprocessor directives. When compiled in release mode (`dotnet publish`), the administrative UI and write-back client code are completely stripped from the WebAssembly bundle.

---

### Consequences

#### Positive
* **Zero Release Overhead:** Release artifacts deployed to GitHub Pages contain no administrative code, secret key dependencies, or write-back paths.
* **Granular Domain Control:** Exposing targeted CRUD operations (e.g., `PUT /api/admin/profile`, `POST /api/admin/skills`) allows individual UI forms to mutate specific nodes of the JSON graph safely without risk of truncating untouched sections.
* **Simplified Security Model:** Running strictly on `localhost` via `.NET Aspire` service discovery eliminates the need for user authentication databases, identity providers, or session management in local development.

#### Negative / Trade-offs
* **Conditional Code Guards:** Developers must maintain `#if DEBUG` guards around admin routes and services in the shared codebase to ensure compilation parity.
* **Localhost Dependency:** Content updates can only be executed while running the solution locally under `.NET Aspire`.

---

## Validation & Verification

* **Release Build Inspection:** Running `dotnet publish -c Release` verifies that `/admin` routes and administrative client services are omitted from the static WebAssembly bundle.
* **File Persistence:** Submitting updates via the local `/admin` interface updates `wwwroot/data/cv-datasource.json` on disk, which can then be committed directly via Git.
