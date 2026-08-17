# Network Telemetry

## Purpose

Scheduled network scans, a collector agent, snapshots and reports.

## Entities

- NetworkTelemetrySnapshot: one scheduled scan result for a target.
- NetworkTelemetryObservation: a single probe result.
- ScheduledScanRun: run history.

## Components

- Scheduled live scans (hosted service).
- Windows collector agent (optional, generic tool).
- Scan control and heartbeat files via a shared path.
- Telemetry panel in the map.
- Telemetry dashboard and export.

## Configuration

- Timezone and locale (configurable, SPEC 07).
- Target CIDRs.
- Scan ports.
- Scan crons.
- Ingest API key.

## Rules

- Defaults must be neutral and valid for a blank installation (SPEC 07).
- Disabling the feature must not break the rest of the application.
- Scan control uses a shared path with request/status/heartbeat files.
