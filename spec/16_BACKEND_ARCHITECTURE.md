# Backend Architecture

## Stack

- ASP.NET Core 8
- EF Core + SQLite
- MVC / Razor for the admin dashboard
- REST API for the map
- Razor Runtime Compilation in development
- Swagger

## Structure

Backend/
  Controllers/     MVC and REST API.
  Data/            DbContext, seed and schema initializers.
  Infrastructure/  Cross-cutting infrastructure.
  Models/          Persistent entities and domain constants.
  Services/        Reusable business logic.
  Templates/       DOCX templates.
  ViewModels/      Razor view models and complex requests.
  Views/           Razor views (dashboard and auth).
  Program.cs       Bootstrap, DI, middleware, auth and routes.

## Dependency Direction

Controllers
  -> Services
  -> Models / Data (EF)

## Key Middleware

- Security headers (CSP, X-Frame-Options, X-Content-Type-Options, Referrer-Policy, Permissions-Policy).
- CORS restrictive by origin.
- URL rewrite between `/dashboard` and `/admin`.
- Audit of authenticated 403 responses.

## Rules

- Keep controllers thin; prefer services for reusable or critical logic.
- Every sensitive mutation must be audited.
- The database file is not versioned.
