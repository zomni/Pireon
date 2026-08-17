import { BACKEND_API_URL, map } from "../views/map.js";
import { getActiveCampus, applyCampusZoomRange } from "@app/goToCampus";
import { getSite, updateSiteViewport } from "../config/siteConfig.js";
import {
  getAdminMapToolsButtons,
  removeAdminMapToolsPanelIfEmpty,
} from "./adminMapToolsPanel.js";
import { identifiers } from "../utils/identifiers.js";
import { validateZoomRange } from "../utils/viewportRules.js";

const controlsClass = "site-viewport-controls";
const statusClass = "site-viewport-status";

const loadSession = async () => {
  try {
    const response = await fetch(`${BACKEND_API_URL}/api/auth/session`, {
      credentials: "include",
      cache: "no-store",
    });

    return response.ok ? await response.json() : null;
  } catch {
    return null;
  }
};

const showStatus = (message, isError = false) => {
  const status = document.querySelector(`.${controlsClass} .${statusClass}`);
  if (status) {
    status.textContent = message || "";
    status.classList.toggle("is-error", isError);
  }
};

const getInputs = () => {
  const minInput = document.querySelector(`.${controlsClass} [data-site-viewport-min]`);
  const maxInput = document.querySelector(`.${controlsClass} [data-site-viewport-max]`);
  const saveButton = document.querySelector(`.${controlsClass} [data-site-viewport-save]`);
  const useMinButton = document.querySelector(`.${controlsClass} [data-site-viewport-use-min]`);
  const useMaxButton = document.querySelector(`.${controlsClass} [data-site-viewport-use-max]`);
  return { minInput, maxInput, saveButton, useMinButton, useMaxButton };
};

const setInputsDisabled = (disabled) => {
  const { minInput, maxInput, saveButton, useMinButton, useMaxButton } = getInputs();
  [minInput, maxInput, saveButton, useMinButton, useMaxButton].forEach((elm) => {
    if (elm) elm.disabled = disabled;
  });
};

const syncControls = () => {
  const campus = getActiveCampus();
  const site = campus ? getSite(campus) : null;
  const { minInput, maxInput } = getInputs();

  if (!site) {
    if (minInput) minInput.value = "";
    if (maxInput) maxInput.value = "";
    setInputsDisabled(true);
    showStatus("");
    return;
  }

  if (minInput) minInput.value = site.minZoom;
  if (maxInput) maxInput.value = site.maxZoom;
  setInputsDisabled(false);
  showStatus("");
};

const saveViewport = async () => {
  const campus = getActiveCampus();
  const site = campus ? getSite(campus) : null;
  if (!site) {
    showStatus("Selecciona un sitio primero.", true);
    return;
  }

  const { minInput, maxInput } = getInputs();
  const minZoom = parseInt(minInput?.value, 10);
  const maxZoom = parseInt(maxInput?.value, 10);
  const validationError = validateZoomRange(minZoom, maxZoom);
  if (validationError) {
    showStatus(validationError, true);
    return;
  }

  setInputsDisabled(true);
  showStatus("Guardando...");

  try {
    const response = await fetch(`${BACKEND_API_URL}/api/sites/${encodeURIComponent(campus)}/viewport`, {
      method: "PUT",
      headers: { "Content-Type": "application/json" },
      credentials: "include",
      cache: "no-store",
      body: JSON.stringify({ minZoom, maxZoom }),
    });

    const data = await response.json().catch(() => null);
    if (!response.ok) {
      showStatus(data?.message || `No se pudo guardar el zoom (${response.status}).`, true);
      return;
    }

    if (updateSiteViewport(campus, data)) {
      applyCampusZoomRange(campus);
      showStatus("Zoom guardado y sincronizado.");
    } else {
      showStatus("No se pudo actualizar el sitio local.", true);
    }
  } catch (error) {
    console.error("Error guardando el rango de zoom:", error);
    showStatus("Error de red al guardar el zoom.", true);
  } finally {
    setInputsDisabled(false);
  }
};

const bindControls = () => {
  const { saveButton, useMinButton, useMaxButton } = getInputs();
  if (saveButton && saveButton.dataset.bound !== "true") {
    saveButton.dataset.bound = "true";
    saveButton.addEventListener("click", saveViewport);
  }
  if (useMinButton && useMinButton.dataset.bound !== "true") {
    useMinButton.dataset.bound = "true";
    useMinButton.addEventListener("click", () => {
      const { minInput } = getInputs();
      if (minInput) minInput.value = String(Math.round(map.getZoom()));
    });
  }
  if (useMaxButton && useMaxButton.dataset.bound !== "true") {
    useMaxButton.dataset.bound = "true";
    useMaxButton.addEventListener("click", () => {
      const { maxInput } = getInputs();
      if (maxInput) maxInput.value = String(Math.round(map.getZoom()));
    });
  }

  const toggle = document.querySelector(`.${controlsClass} [data-site-viewport-toggle]`);
  const body = document.querySelector(`.${controlsClass} [data-site-viewport-body]`);
  if (toggle && body && toggle.dataset.bound !== "true") {
    toggle.dataset.bound = "true";
    toggle.addEventListener("click", () => {
      const willOpen = body.hidden;
      body.hidden = !willOpen;
      toggle.setAttribute("aria-expanded", willOpen ? "true" : "false");
      toggle.classList.toggle("is-expanded", willOpen);
    });
  }
};

const createSiteViewportControls = () => {
  const buttons = getAdminMapToolsButtons();
  if (!buttons) return;

  if (!document.querySelector(`.${controlsClass}`)) {
    buttons.insertAdjacentHTML(
      "beforeend",
      `
      <div class="${controlsClass} admin-map-tools-group">
        <button type="button" class="dashboard-link site-viewport-toggle" data-site-viewport-toggle aria-expanded="false">
          <span class="site-viewport-toggle-icon" aria-hidden="true">+</span> Zoom del sitio
        </button>
        <div class="site-viewport-body" data-site-viewport-body hidden>
          <label class="site-viewport-field">M&iacute;n
            <input type="number" min="0" max="21" step="1" data-site-viewport-min />
          </label>
          <label class="site-viewport-field">M&aacute;x
            <input type="number" min="0" max="21" step="1" data-site-viewport-max />
          </label>
          <div class="site-viewport-actions">
            <button type="button" class="dashboard-link" data-site-viewport-use-min title="Usar el zoom actual como m&iacute;nimo">Zoom actual &rarr; m&iacute;n</button>
            <button type="button" class="dashboard-link" data-site-viewport-use-max title="Usar el zoom actual como m&aacute;ximo">Zoom actual &rarr; m&aacute;x</button>
            <button type="button" class="dashboard-link" data-site-viewport-save>Guardar zoom</button>
          </div>
          <div class="${statusClass}"></div>
        </div>
      </div>
      `
    );
  }

  bindControls();
  syncControls();
};

const removeSiteViewportControls = () => {
  document.querySelector(`.${controlsClass}`)?.remove();
  removeAdminMapToolsPanelIfEmpty();
};

export const syncSiteViewportPanelForSession = (session) => {
  if (session?.isAdmin) {
    createSiteViewportControls();
  } else {
    removeSiteViewportControls();
  }
};

export const initSiteViewportPanel = async () => {
  const session = await loadSession();
  syncSiteViewportPanelForSession(session);
};

window.addEventListener(identifiers.events.sessionChanged, (event) => {
  syncSiteViewportPanelForSession(event.detail || {});
});

window.addEventListener(identifiers.events.campusChanged, syncControls);
