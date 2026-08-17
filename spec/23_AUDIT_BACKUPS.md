# Audit & Backups

## Purpose

Formal audit logging and scheduled database backups.

## Audit

`AuditLogEntries` records:

- user
- IP
- user-agent
- resource
- result
- severity
- previous and new value

Audited actions:

- Inventory mutations
- Database export/import/restore
- PDF upload/download/delete
- Login / logout / MFA / denied access
- Critical configuration changes
- Map editor mutations (manual buildings, geometry overrides, walking routes)
- Points of interest mutations (create/update/delete, see SPEC 09)

## Backups

- SQLite backups via a hosted service.
- Backup hash and history in `BackupHistories`.
- Retention policy and cleanup of expired backups.
- Manual backup endpoint.
- Database export / import / restore from the dashboard.

## Rules

- The database file is not versioned.
- Backup path and retention are configurable.
- Backup history entries are auditable.
