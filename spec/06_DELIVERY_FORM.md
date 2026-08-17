# Delivery Form Generalization

## Purpose

Keep the equipment delivery form and PDF generation as a product feature without institutional content.

## Current State

- `Views/Admin/DeliveryForm.cshtml`: institutional application checklist now renders from configuration (`DeliveryForm:ApplicationChecklist:Sections`), replacing the fixed HTML.
- `Services/DeliveryFormChecklistConfig.cs`: parses sections/items from config with generic defaults; each item maps to a checkbox bound by name to a `Validation*`/`App*`/`Admin*` bool property in `EquipmentDeliveryFormViewModel`.
- `Services/DeliveryFormTemplateBuilder.cs`: generates a generic, parameterizable DOCX template in memory when no template file is configured.
- `Services/EquipmentDeliveryDocumentService.cs`: resolves the template (configured `DeliveryForm:TemplatePath` → default `Templates/FormularioEntregaEquipo.docx` → generated generic template); fills the applications table from the configured checklist; converts to PDF via LibreOffice (`DeliveryForm:SofficePath`, default `soffice`).
- Institution name read from configuration (`DeliveryForm:Institution`, `AdminController.cs`), not a constant.
- PDF conversion via LibreOffice (`EquipmentDeliveryDocumentService.cs`); LibreOffice installed in `Dockerfile` (prod) and `Dockerfile.dev`.

## Required Changes

- Read the institution name from configuration or equipment data, not a constant.
- Make the application checklist configurable.
- Replace the client DOCX template with a generic, parameterizable template.
- Keep LibreOffice conversion and PDF handling.

## Rules

- PDF layout must remain user-testable after template changes.
- No institutional name may be hardcoded.

## Acceptance Criteria

- Changing the configured institution reflects in the generated form and PDF.
- The checklist renders from configuration.
- The delivery form flow works with a generic template.

## Decisiones de implementación

- El nombre de la institución sale de `appsettings` (`Institution`), con default genérico.
- El checklist de aplicaciones se modela como configuración (lista de secciones e ítems), reemplazando el HTML fijo.
- La plantilla DOCX genérica se genera en memoria (`DeliveryFormTemplateBuilder`); si se configura `TemplatePath` y el archivo existe, se usa esa plantilla en su lugar.
- El template envía el checklist **vacío** (`ApplicationChecklist.Sections = []` en `appsettings` y `GetDefaultSections()` retorna lista vacía): el contenido de aplicaciones lo define cada instancia. El formulario, el PDF y la tabla de aplicaciones del DOCX se renderizan correctamente sin secciones. El usuario decidirá el contenido final del checklist.
