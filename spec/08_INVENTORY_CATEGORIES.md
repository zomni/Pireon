# Configurable Inventory Categories

## Purpose

Make the inferred inventory categories configurable and remove the client acronym dependency.

## Current State

- `Services/InventoryCategoriesConfig.cs`: parses `InventoryCategories:Categories` and `InventoryCategories:Statuses` (Name + Label + Tokens) with generic fallbacks (`other`/`active`).
- `ExcelInventoryImportService.cs`: `InferCategory`/`InferStatus` delegate to `InventoryCategoriesConfig` (hardcoded token lists removed).
- `AdminController.cs`: inventory category/status option lists come from configuration.
- `frontend/src/config/appConfig.js` + `frontend/src/views/featureDisplay.js`: category order and labels config-driven.
- `\bHSR\b` stripping removed (verified: no matches in the codebase).

## Required Changes

- Model categories and statuses as a configurable list (configuration or admin-managed).
- Keep `InferredCategory` and `InferredStatus` fields and alias rules generic.
- Remove the `\bHSR\b` stripping.

## Rules

- Categories must be addable and editable without code changes.
- Import mapping must keep working with a generic category list.

## Acceptance Criteria

- Adding a category to configuration makes it usable in import and filtering.
- No client acronym logic remains.

## Decisiones de implementación

- Las categorías se definen en configuración con un default genérico razonable; la importación las valida contra esa lista.
- El stripping de `HSR` se elimina; la normalización de texto queda genérica.
