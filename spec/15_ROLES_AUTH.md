# Roles & Authentication

## Roles

- admin: full access, MFA required.
- editor: controlled operational edits.
- viewer: read-only.
- auditor: audit, compliance and integrity without modifying inventory.

## Authentication

- Local break-glass users.
- Optional LDAP / LDAPS authentication against an external directory.
- MFA (TOTP) for administrators.
- Session-based cookies plus claims for the frontend API.

## Cookies

- `Pireon.Auth` final session.
- `Pireon.MfaPending` intermediate MFA flow.

## Claims

Namespaced under `pireon:`:

- pireon:remember_me
- pireon:can_manage_users
- pireon:mfa_mode
- pireon:mfa_setup_key
- pireon:mfa_user_id
- pireon:mfa_return_url

## Session API

- GET /api/auth/session — current session and role for the map.
- POST /api/auth/logout — end session.

## Decisiones de implementación

- El admin inicial se crea desde `ADMIN_EMAIL` / `ADMIN_PASSWORD`; la app falla al iniciar si no hay admin y faltan las variables.
- LDAP es opcional y configurable; los usuarios locales break-glass cubren el arranque sin directorio.
