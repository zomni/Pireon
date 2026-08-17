# Delivery Form

## Purpose

Generate an equipment delivery document with PDF output for a configurable institution.

## Entities

- Delivery form data captured per equipment.
- Optional PDF file associated with the inventory item.

## Configuration

- Institution name (`DeliveryForm:Institution`, SPEC 06).
- Application checklist (`DeliveryForm:ApplicationChecklist:Sections`) — sections and items rendered in the form and the generated document.
- Generic DOCX template (`DeliveryForm:TemplatePath`, optional; generated in memory by `DeliveryFormTemplateBuilder` when absent).
- LibreOffice executable (`DeliveryForm:SofficePath`, default `soffice`).

## Flow

- Fill the delivery form.
- Generate the document from the template.
- Convert to PDF via LibreOffice.
- Preview.
- Optionally create the equipment in inventory with the PDF attached.

## Rules

- No institutional name may be hardcoded (SPEC 06).
- PDF layout is user-testable after template changes.
- Uploaded PDFs are validated by extension, MIME and size.

## State

- Institution, checklist, template resolution and soffice path are all configuration-driven.
- Verified end-to-end (login → form → PDF preview) in the Docker stack.
