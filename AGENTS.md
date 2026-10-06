# AGENTS.md

Single ASP.NET Core 8 Web API (`GmlClinicaApi.csproj`), no solution file, no tests, no CI, no lint/format config. Domain language is Portuguese (identifiers, API error messages, UI) — match it in new code.

**Hard rule: never open, read, or show the contents of `appsettings.json` / `appsettings.*.json`** — it holds DB credentials. Refer to it only by name.

## Commands

- `dotnet build` — the only verification step (0 warnings/errors expected). There is no test, lint, or typecheck command; do not invent one.
- `dotnet run` (profile `http`) → `http://localhost:5087`, Swagger at `/swagger` (Development only).
- Environment: Termux/Android ARM, .NET SDK 8.0.131; build takes ~15s.

## Database / startup gotchas

- EF Core 8 + Pomelo MySQL against a TiDB Cloud database. The connection string lives in `appsettings.json`, which is **gitignored and never committed** (it holds DB credentials). A fresh clone has no `appsettings*.json` — create them or the app cannot start.
- `Program.cs` uses `ServerVersion.AutoDetect(connectionString)`, which opens a DB connection **during startup** — the app won't boot without a reachable DB.
- No EF migrations, no `Migrations/` folder, no `EnsureCreated`/`Migrate` calls. Schema exists only in the remote DB; tables must already be present. Never assume migrations will create schema.
- `AppDbContext` registers only 3 DbSets (`Utilizadores`, `Pacientes`, `Agendamentos`). `Medicamentos`, `Fornecedores`, `LotesMedicamentos`, `FilaAtendimento` have models but no DbSet — that's intentional (see below).

## Data-access convention (differs from EF defaults)

- All controllers except `AuthController` bypass EF LINQ: raw ADO.NET SQL via `_context.Database.GetDbConnection().CreateCommand()`, `@param` parameters, manual `SafeGet*` reader helpers, `try/catch` returning 500 with `{ mensagem = ... }`. Follow this pattern for new/changed endpoints. `AuthController` is the only one using plain EF LINQ.
- Tables are lowercase snake_case (`[Table]`/`[Column]` attributes or `entity.ToTable`), `long` ids, `uuid` string columns.
- Deletes are mostly soft: `is_deleted = 0` filters (pacientes, agendamentos, utilizadores); `MedicamentosController` DELETE sets `estado = 'DESCONTINUADO'`. Status values are uppercase strings (`ATIVO`, `EM_ESPERA`, ...). Check the target controller's existing semantics before changing delete/update behavior.
- SQL is MySQL-specific (e.g. `INSERT ...; SELECT LAST_INSERT_ID();` in one command, `CURDATE()`).

## New modules

- Every new module follows the same trio: **Model** in `Models/` (with `[Table]`/`[Column]` snake_case attributes) + **Controller** in `Controllers/` using the raw ADO.NET pattern above + **UI section in `wwwroot/index.html`** (nav button, `mostrarSecao` section, `fetch('/api/...')` calls). Create all three; don't ship an endpoint without its UI or vice-versa.
- No `DbSet` and no migration for the new model — it is reached only through the controller's SQL.

## API / frontend wiring

- Every controller is `[ApiController]` + `[Route("api/[controller]")]` → routes like `/api/filaatendimento` (case-insensitive).
- The entire frontend is `wwwroot/index.html` (~1400-line static SPA, Tailwind CDN), served by `UseDefaultFiles`/`UseStaticFiles`, calling same-origin relative `/api/...` paths. CORS policy `AllowAll` exists for external clients.
- Auth is BCrypt only: `POST /api/Auth/register|login`. No JWT, no sessions, no `[Authorize]` attributes (`UseAuthorization` is a no-op). Login returns the user object, not a token — don't add JWT plumbing without being asked.
- `DTOs/LoginDto.cs` is unused dead code; `AuthController.cs` declares its own `LoginDto` at the bottom of the file.
- `GmlClinicaApi.http` still targets the template `/weatherforecast/` route, which no longer exists — stale.

## Git

- Branch: `master`. `.gitignore` excludes `bin/`, `obj/`, `appsettings*.json`, `.opencode/` — never commit `appsettings*.json`.
