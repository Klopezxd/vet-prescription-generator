// ==========================================================================
// Lógica de Cliente Web: Generador de Recetas Veterinarias (Agrocalidad)
// ==========================================================================

let currentRecipeNumber = "0001";
let searchDebounceTimer = null;

document.addEventListener("DOMContentLoaded", () => {
    initRealtimePreview();
    setTodayDate();
    fetchNextNumber();
    fetchVetConfig();
});

// 1. Vinculación en Tiempo Real (Formulario -> Hoja Agrocalidad)
function initRealtimePreview() {
    const bindField = (inputId, previewIds, defaultValue = "--") => {
        const input = document.getElementById(inputId);
        if (!input) return;
        
        const update = () => {
            const val = input.value.trim() || defaultValue;
            previewIds.forEach(id => {
                const target = document.getElementById(id);
                if (target) target.textContent = val;
            });
        };

        input.addEventListener("input", update);
        input.addEventListener("change", update);
    };

    bindField("input-dia", ["prev-c1-dia", "prev-c2-dia"], "--");
    bindField("input-mes", ["prev-c1-mes", "prev-c2-mes"], "--");
    bindField("input-anio", ["prev-c1-anio", "prev-c2-anio"], "----");

    bindField("input-especie", ["prev-especie"], "--");
    bindField("input-nombre-paciente", ["prev-nombre-paciente"], "--");
    bindField("input-sexo", ["prev-sexo"], "Macho");
    bindField("input-edad", ["prev-edad"], "--");

    bindField("input-nombre-prop", ["prev-nombre-prop"], "--");
    bindField("input-dir-prop", ["prev-dir-prop"], "--");

    bindField("input-prescripcion", ["prev-prescripcion"], "");
    bindField("input-diagnostico", ["prev-diagnostico"], "");
    bindField("input-posologia", ["prev-posologia"], "");
    bindField("input-instrucciones", ["prev-instrucciones"], "Sin novedades");
}

// 2. Autocompletado de Fecha Actual
function setTodayDate() {
    const now = new Date();
    const dia = String(now.getDate()).padStart(2, "0");
    const mes = String(now.getMonth() + 1).padStart(2, "0");
    const anio = String(now.getFullYear());

    document.getElementById("input-dia").value = dia;
    document.getElementById("input-mes").value = mes;
    document.getElementById("input-anio").value = anio;

    document.getElementById("prev-c1-dia").textContent = dia;
    document.getElementById("prev-c2-dia").textContent = dia;
    document.getElementById("prev-c1-mes").textContent = mes;
    document.getElementById("prev-c2-mes").textContent = mes;
    document.getElementById("prev-c1-anio").textContent = anio;
    document.getElementById("prev-c2-anio").textContent = anio;
}

// 3. Atajos de Formulario
function setSpecies(name) {
    const input = document.getElementById("input-especie");
    input.value = name;
    input.dispatchEvent(new Event("input"));

    document.querySelectorAll(".chip-btn").forEach(btn => {
        btn.classList.toggle("active", btn.textContent.trim() === name);
    });
}

function toggleNoAplicaNombre() {
    const chk = document.getElementById("chk-no-aplica");
    const input = document.getElementById("input-nombre-paciente");
    if (chk.checked) {
        input.value = "No aplica";
        input.disabled = true;
    } else {
        input.disabled = false;
        input.value = "";
    }
    input.dispatchEvent(new Event("input"));
}

function setSinNovedades() {
    const textarea = document.getElementById("input-instrucciones");
    textarea.value = "Sin novedades";
    textarea.dispatchEvent(new Event("input"));
}

function limpiarFormulario() {
    document.getElementById("recipe-form").reset();
    setTodayDate();
    document.getElementById("chk-no-aplica").checked = false;
    document.getElementById("input-nombre-paciente").disabled = false;
    document.getElementById("input-instrucciones").value = "Sin novedades";
    
    // Forzar actualización de vista previa
    document.querySelectorAll(".form-input, .form-textarea, .form-select").forEach(el => {
        el.dispatchEvent(new Event("input"));
    });
}

// 4. Conexión con Backend FastAPI
async function fetchNextNumber() {
    try {
        const res = await fetch("/api/next-number");
        const data = await res.json();
        currentRecipeNumber = data.next_number_formatted;
        document.getElementById("prev-c1-num").textContent = currentRecipeNumber;
        document.getElementById("prev-c2-num").textContent = currentRecipeNumber;
    } catch (err) {
        console.error("Error al obtener número de receta:", err);
    }
}

async function fetchVetConfig() {
    try {
        const res = await fetch("/api/config");
        const cfg = await res.json();

        // Rellenar en vista previa
        document.getElementById("prev-vet-nombre").textContent = cfg.veterinario_nombre || "--";
        document.getElementById("prev-vet-cedula").textContent = cfg.veterinario_cedula || "--";
        document.getElementById("prev-vet-senescyt").textContent = cfg.veterinario_senescyt || "--";
        document.getElementById("prev-vet-telefono").textContent = cfg.veterinario_telefono || "--";

        // Rellenar formulario de configuración
        document.getElementById("cfg-vet-nombre").value = cfg.veterinario_nombre || "";
        document.getElementById("cfg-vet-cedula").value = cfg.veterinario_cedula || "";
        document.getElementById("cfg-vet-senescyt").value = cfg.veterinario_senescyt || "";
        document.getElementById("cfg-vet-telefono").value = cfg.veterinario_telefono || "";
        document.getElementById("cfg-establecimiento").value = cfg.establecimiento_nombre || "";
    } catch (err) {
        console.error("Error al cargar configuración:", err);
    }
}

async function guardarConfiguracion() {
    const payload = {
        veterinario_nombre: document.getElementById("cfg-vet-nombre").value.trim(),
        veterinario_cedula: document.getElementById("cfg-vet-cedula").value.trim(),
        veterinario_senescyt: document.getElementById("cfg-vet-senescyt").value.trim(),
        veterinario_telefono: document.getElementById("cfg-vet-telefono").value.trim(),
        establecimiento_nombre: document.getElementById("cfg-establecimiento").value.trim()
    };

    try {
        const res = await fetch("/api/config", {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify(payload)
        });

        if (res.ok) {
            alert("✅ Datos del Médico Veterinario guardados correctamente.");
            await fetchVetConfig();
            switchTab("emitir");
        } else {
            alert("❌ Error al guardar la configuración.");
        }
    } catch (err) {
        alert("❌ Error de comunicación con el servidor: " + err);
    }
}

// 5. Emisión e Impresión Directa
async function emitirReceta() {
    const payload = {
        dia: document.getElementById("input-dia").value.trim(),
        mes: document.getElementById("input-mes").value.trim(),
        anio: document.getElementById("input-anio").value.trim(),
        especie: document.getElementById("input-especie").value.trim(),
        nombre_paciente: document.getElementById("input-nombre-paciente").value.trim() || "No aplica",
        sexo: document.getElementById("input-sexo").value,
        edad: document.getElementById("input-edad").value.trim() || "No especificada",
        nombre_propietario: document.getElementById("input-nombre-prop").value.trim(),
        direccion_propietario: document.getElementById("input-dir-prop").value.trim() || "Particular",
        prescripcion: document.getElementById("input-prescripcion").value.trim(),
        diagnostico: document.getElementById("input-diagnostico").value.trim() || "Evaluación clínica",
        posologia: document.getElementById("input-posologia").value.trim(),
        instrucciones: document.getElementById("input-instrucciones").value.trim() || "Sin novedades"
    };

    try {
        const res = await fetch("/api/recetas", {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify(payload)
        });

        const data = await res.json();
        if (res.ok) {
            // Actualizar número impreso con el confirmado
            document.getElementById("prev-c1-num").textContent = data.numero_receta;
            document.getElementById("prev-c2-num").textContent = data.numero_receta;

            // Abrir inmediatamente el diálogo de impresión de la hoja A4 apaisada
            window.print();

            // Preparar siguiente número correlativo
            await fetchNextNumber();
        } else {
            alert("❌ Error al emitir la receta: " + (data.detail || "Error desconocido"));
        }
    } catch (err) {
        alert("❌ Error al procesar receta: " + err);
    }
}

// 6. Navegación entre Pestañas
function switchTab(tabName) {
    document.querySelectorAll(".nav-tab").forEach(tab => tab.classList.remove("active"));
    document.getElementById(`tab-${tabName}`).classList.add("active");

    const viewEmitir = document.getElementById("view-emitir");
    const viewHistorial = document.getElementById("view-historial");
    const viewConfig = document.getElementById("view-config");

    viewEmitir.style.display = tabName === "emitir" ? "flex" : "none";
    viewHistorial.style.display = tabName === "historial" ? "block" : "none";
    viewConfig.style.display = tabName === "config" ? "block" : "none";

    if (tabName === "historial") {
        cargarHistorial();
    }
}

// 7. Historial y Búsqueda para Auditorías
async function cargarHistorial(termino = "") {
    const tbody = document.getElementById("history-table-body");
    tbody.innerHTML = `<tr><td colspan="6" class="empty-state">Buscando recetas...</td></tr>`;

    try {
        const url = termino ? `/api/recetas?q=${encodeURIComponent(termino)}` : "/api/recetas";
        const res = await fetch(url);
        const recetas = await res.json();

        if (recetas.length === 0) {
            tbody.innerHTML = `<tr><td colspan="6" class="empty-state">No se encontraron recetas registradas.</td></tr>`;
            return;
        }

        tbody.innerHTML = recetas.map(r => `
            <tr>
                <td><strong>${r.numero_receta}</strong></td>
                <td>${r.fecha_emision}</td>
                <td><strong>${r.nombre_propietario}</strong></td>
                <td>${r.nombre_paciente} (${r.especie})</td>
                <td style="max-width: 250px; font-size: 0.8rem;">${r.prescripcion}</td>
                <td style="text-align: center;">
                    <button class="btn btn-secondary btn-sm" onclick="reimprimirReceta(${r.id})">
                        🖨️ Ver / Imprimir
                    </button>
                </td>
            </tr>
        `).join("");
    } catch (err) {
        tbody.innerHTML = `<tr><td colspan="6" class="empty-state">Error al cargar historial: ${err}</td></tr>`;
    }
}

function buscarRecetas() {
    clearTimeout(searchDebounceTimer);
    searchDebounceTimer = setTimeout(() => {
        const termino = document.getElementById("search-input").value.trim();
        cargarHistorial(termino);
    }, 250);
}

async function reimprimirReceta(id) {
    try {
        const res = await fetch(`/api/recetas/${id}`);
        if (!res.ok) throw new Error("No se encontró la receta");
        const r = await res.json();

        // Rellenar formulario y vista previa
        document.getElementById("input-dia").value = r.dia;
        document.getElementById("input-mes").value = r.mes;
        document.getElementById("input-anio").value = r.anio;
        document.getElementById("input-especie").value = r.especie;
        document.getElementById("input-nombre-paciente").value = r.nombre_paciente;
        document.getElementById("input-sexo").value = r.sexo;
        document.getElementById("input-edad").value = r.edad;
        document.getElementById("input-nombre-prop").value = r.nombre_propietario;
        document.getElementById("input-dir-prop").value = r.direccion_propietario;
        document.getElementById("input-prescripcion").value = r.prescripcion;
        document.getElementById("input-diagnostico").value = r.diagnostico;
        document.getElementById("input-posologia").value = r.posologia;
        document.getElementById("input-instrucciones").value = r.instrucciones;

        // Disparar eventos para actualizar preview
        document.querySelectorAll(".form-input, .form-textarea, .form-select").forEach(el => {
            el.dispatchEvent(new Event("input"));
        });

        document.getElementById("prev-c1-num").textContent = r.numero_receta;
        document.getElementById("prev-c2-num").textContent = r.numero_receta;

        switchTab("emitir");
        setTimeout(() => window.print(), 300);
    } catch (err) {
        alert("❌ Error al cargar receta: " + err);
    }
}
