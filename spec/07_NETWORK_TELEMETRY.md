# Network Telemetry Generalization

## Purpose

Keep the network telemetry subsystem (scheduled scans, agent, panel, reports) without client defaults.

## Current State

- Timezone and locale are configuration-driven: `NetworkTelemetrySettings:DisplayTimeZone` (default `UTC`) and `DisplayLocale` (default `es-CL`), resolved centrally via `Services/TelemetryTimeSettings.cs`.
- `NetworkTelemetryService.cs` uses instance timezone/culture; `NetworkTelemetryLiveScanHostedService.cs` and `ExtendedSchemaInitializer.cs` resolve them from configuration.
- Views `Admin/Index.cshtml`, `Admin/NetworkTelemetry.cshtml`, `Auth/MfaSetup.cshtml` and the frontend (`appConfig.js`, `networkTelemetryPanel.js`, `featureDisplay.js`) read the configured timezone/locale.
- `NetworkTelemetrySettings` defaults: neutral CIDRs (empty), `IngestApiKey="CHANGE_ME"`, UTC timezone.
- `tools/SoteroMap.NetworkCollector` documented as an optional generic tool with generic configuration.

## Required Changes

- Move timezone and locale to configuration.
- Neutralize telemetry defaults (CIDRs, API key, crons, timezone).
- Document the Windows collector as an optional generic tool with generic configuration.

## Rules

- Disabling the feature must not break the rest of the application.
- Defaults must be valid for a blank installation.

## Acceptance Criteria

- Changing the telemetry timezone and locale configuration updates all displayed timestamps.
- A fresh install has no client CIDR or API key values.
- The agent tool runs with generic configuration.

## Decisiones de implementación

- La zona horaria se centraliza en configuración (default UTC); `es-CL` se usa solo como locale por defecto del template.
- El agente recolector se mantiene como herramienta opcional documentada, con `appsettings` genérico.
