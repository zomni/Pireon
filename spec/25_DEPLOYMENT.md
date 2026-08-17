# Deployment

## Stack

- ASP.NET Core 8 backend with SQLite.
- Static frontend served from the backend `wwwroot` or as a portable static build.
- LibreOffice headless for PDF conversion.
- Optional Windows collector agent for telemetry.

## Environments

- Development: Razor Runtime Compilation, Swagger, seeded config.
- Production: published, headers enabled, no Swagger, strict settings.

## Configuration

- All instance settings come from configuration (SPEC 01) with sensible defaults.
- Environment variables for secrets.
- A single `dotnet publish` produces the deployable backend.
- The frontend is built via Webpack and copied into `wwwroot`.

## First Run

- Schema is created automatically.
- First admin requires `ADMIN_EMAIL` / `ADMIN_PASSWORD`.
- No demo data is inserted (SPEC 04).
