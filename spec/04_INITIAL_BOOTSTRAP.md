# Initial Bootstrap & Empty Start

## Purpose

A new installation starts practically empty. Only the initial administrator is created from environment configuration.

## Current State

- `Data/SeedData.cs` keeps 6 Locations and 10 Equipment items, but runs only when `DemoData:Enabled` (env `DEMO_DATA`) is explicitly `true`.
- `docker-compose.yml` no longer sets seeded credentials. It maps `ADMIN_EMAIL` / `ADMIN_PASSWORD` to `AuthSettings__AdminUsername` / `AuthSettings__AdminPassword` (`backend/.env.example` documents both).
- `BackendAuthService.EnsureInitialAdminAsync` creates only the initial Administrator from env/config, and only when no active admin exists (idempotent). Missing vars fail fast with a clear `InvalidOperationException`.
- No viewer is seeded. Legacy viewer rows (if present) are normalized to active state but never auto-created.

## Rules

- Startup must fail if no Administrator can be created and the required env vars are missing (existing rule, keep).
- Demo data, if kept, is clearly separated and never enabled by default.

## Acceptance Criteria

- A fresh install boots with only the initial Administrator.
- No seed locations, buildings, equipment or campus data are created by default.
- Missing admin-bootstrap env vars fail fast with a clear error.

## Decisiones de implementación

- `EnsureInitialAdminAsync` se ejecuta en el arranque (Program.cs) tras la migración del esquema.
- Si ya existe un admin activo, el arranque solo normaliza roles legacy y continúa (no toca credenciales).
- Los datos demo viven en un módulo aparte activado por `DEMO_DATA=true`.
- La DB con el esquema anterior (PKs INTEGER) no es migrable automáticamente; una instalación nueva crea el esquema GUID vía EF migrations (`InitialCreate`).
