// ==========================================================================
// Lógica de Cliente Web: Generador de Recetas Veterinarias (Agrocalidad)
// ==========================================================================

let currentRecipeNumber = "0001";
let editingRecipeId = null;
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

    document.querySelectorAll(".chip-btn").forEach(btn => {
        btn.classList.remove("active");
    });
    
    // Forzar actualización de vista previa
    document.querySelectorAll(".form-input, .form-textarea, .form-select").forEach(el => {
        el.dispatchEvent(new Event("input"));
    });
}

// 4. Edición de Recetas
async function editarReceta(id) {
    try {
        const res = await fetch(`/api/recetas/${id}`);
        if (!res.ok) throw new Error("No se pudo obtener la receta seleccionada.");
        const r = await res.json();

        editingRecipeId = id;

        // Rellenar campos del formulario
        document.getElementById("input-dia").value = r.dia || "";
        document.getElementById("input-mes").value = r.mes || "";
        document.getElementById("input-anio").value = r.anio || "";
        document.getElementById("input-especie").value = r.especie || "";

        // Actualizar chips de especies
        document.querySelectorAll(".chip-btn").forEach(btn => {
            btn.classList.toggle("active", btn.textContent.trim() === r.especie);
        });

        const pacienteInput = document.getElementById("input-nombre-paciente");
        const chkNoAplica = document.getElementById("chk-no-aplica");
        if (r.nombre_paciente === "No aplica") {
            chkNoAplica.checked = true;
            pacienteInput.value = "No aplica";
            pacienteInput.disabled = true;
        } else {
            chkNoAplica.checked = false;
            pacienteInput.disabled = false;
            pacienteInput.value = r.nombre_paciente || "";
        }

        document.getElementById("input-sexo").value = r.sexo || "Macho";
        document.getElementById("input-edad").value = r.edad || "";
        document.getElementById("input-nombre-prop").value = r.nombre_propietario || "";
        document.getElementById("input-dir-prop").value = r.direccion_propietario || "";
        document.getElementById("input-prescripcion").value = r.prescripcion || "";
        document.getElementById("input-diagnostico").value = r.diagnostico || "";
        document.getElementById("input-posologia").value = r.posologia || "";
        document.getElementById("input-instrucciones").value = r.instrucciones || "";

        // Disparar eventos para actualizar vista previa
        document.querySelectorAll(".form-input, .form-textarea, .form-select").forEach(el => {
            el.dispatchEvent(new Event("input"));
        });

        // Reflejar número de receta en vista previa
        document.getElementById("prev-c1-num").textContent = r.numero_receta;
        document.getElementById("prev-c2-num").textContent = r.numero_receta;

        // Mostrar banner de edición
        const editBanner = document.getElementById("edit-banner");
        if (editBanner) editBanner.style.display = "flex";
        const editNum = document.getElementById("edit-receta-num");
        if (editNum) editNum.textContent = r.numero_receta;

        // Modificar texto del botón de acción
        const submitBtn = document.getElementById("btn-submit-recipe");
        if (submitBtn) submitBtn.textContent = "💾 GUARDAR CAMBIOS EN RECETA";

        // Cambiar a pestaña emitir
        switchTab("emitir");
    } catch (err) {
        alert("❌ Error al cargar receta para edición: " + err);
    }
}

function cancelarEdicion() {
    editingRecipeId = null;

    const editBanner = document.getElementById("edit-banner");
    if (editBanner) editBanner.style.display = "none";

    const submitBtn = document.getElementById("btn-submit-recipe");
    if (submitBtn) submitBtn.textContent = "🖨️ EMITIR E IMPRIMIR RECETA";

    limpiarFormulario();
    fetchNextNumber();
}

// 5. Conexión con Backend FastAPI
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

// 6. Emisión / Actualización e Impresión Directa
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

    if (editingRecipeId) {
        try {
            const res = await fetch(`/api/recetas/${editingRecipeId}`, {
                method: "PUT",
                headers: { "Content-Type": "application/json" },
                body: JSON.stringify(payload)
            });

            const data = await res.json();
            if (res.ok) {
                alert("✅ " + (data.message || "Receta actualizada exitosamente."));
                cancelarEdicion();
                switchTab("historial");
            } else {
                alert("❌ Error al actualizar la receta: " + (data.detail || "Error desconocido"));
            }
        } catch (err) {
            alert("❌ Error al procesar receta: " + err);
        }
        return;
    }

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

// 7. Navegación entre Pestañas
function switchTab(tabName) {
    document.querySelectorAll(".nav-tab").forEach(tab => tab.classList.remove("active"));
    const activeNav = document.getElementById(`tab-${tabName}`);
    if (activeNav) activeNav.classList.add("active");

    const viewEmitir = document.getElementById("view-emitir");
    const viewHistorial = document.getElementById("view-historial");
    const viewConfig = document.getElementById("view-config");

    if (viewEmitir) viewEmitir.style.display = tabName === "emitir" ? "flex" : "none";
    if (viewHistorial) viewHistorial.style.display = tabName === "historial" ? "block" : "none";
    if (viewConfig) viewConfig.style.display = tabName === "config" ? "block" : "none";

    if (tabName === "historial") {
        cargarHistorial();
    }
}

// 8. Historial y Búsqueda para Auditorías
async function cargarHistorial(termino = "") {
    const tbody = document.getElementById("history-table-body");
    tbody.innerHTML = `<tr><td colspan="7" class="empty-state">Buscando recetas...</td></tr>`;

    try {
        const url = termino ? `/api/recetas?q=${encodeURIComponent(termino)}` : "/api/recetas";
        const res = await fetch(url);
        const recetas = await res.json();

        if (recetas.length === 0) {
            tbody.innerHTML = `<tr><td colspan="7" class="empty-state">No se encontraron recetas registradas.</td></tr>`;
            return;
        }

        tbody.innerHTML = recetas.map(r => {
            const isAnulada = r.estado === "ANULADA";
            const badgeEstado = isAnulada
                ? `<span style="display: inline-block; padding: 0.2rem 0.55rem; border-radius: 9999px; font-size: 0.75rem; font-weight: 700; background: #fee2e2; color: #b91c1c; border: 1px solid #fca5a5;" title="${r.motivo_anulacion ? 'Motivo: ' + r.motivo_anulacion : 'Receta anulada'}">🚫 ANULADA</span>`
                : `<span style="display: inline-block; padding: 0.2rem 0.55rem; border-radius: 9999px; font-size: 0.75rem; font-weight: 700; background: #dcfce7; color: #15803d; border: 1px solid #86efac;">✅ EMITIDA</span>`;

            const btnAnular = !isAnulada
                ? `<button class="btn btn-secondary btn-sm" style="color: #b91c1c;" onclick="anularReceta(${r.id}, '${r.numero_receta}')" title="Anular receta">🚫 Anular</button>`
                : "";

            return `
            <tr>
                <td><strong>${r.numero_receta}</strong></td>
                <td>${r.fecha_emision}</td>
                <td><strong>${r.nombre_propietario}</strong></td>
                <td>${r.nombre_paciente} (${r.especie})</td>
                <td style="max-width: 250px; font-size: 0.8rem;">${r.prescripcion}</td>
                <td style="text-align: center;">${badgeEstado}</td>
                <td style="text-align: center;">
                    <div style="display: flex; gap: 0.3rem; justify-content: center; align-items: center; flex-wrap: wrap;">
                        <button class="btn btn-secondary btn-sm" onclick="reimprimirReceta(${r.id})" title="Ver e imprimir">🖨️ Ver</button>
                        <button class="btn btn-secondary btn-sm" onclick="editarReceta(${r.id})" title="Editar receta">✏️ Editar</button>
                        ${btnAnular}
                        <button class="btn btn-secondary btn-sm" style="color: #dc2626;" onclick="eliminarReceta(${r.id}, '${r.numero_receta}')" title="Eliminar receta">🗑️ Eliminar</button>
                    </div>
                </td>
            </tr>
            `;
        }).join("");
    } catch (err) {
        tbody.innerHTML = `<tr><td colspan="7" class="empty-state">Error al cargar historial: ${err}</td></tr>`;
    }
}

function buscarRecetas() {
    clearTimeout(searchDebounceTimer);
    searchDebounceTimer = setTimeout(() => {
        const input = document.getElementById("search-input");
        const termino = input ? input.value.trim() : "";
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

// 9. Anulación y Eliminación de Recetas
async function anularReceta(id, numero) {
    const motivo = prompt(`¿Motivo de anulación para la Receta N° ${numero}?`, "Error en prescripción / Anulada por el veterinario");
    if (motivo === null) return; // Operación cancelada por el usuario

    try {
        const res = await fetch(`/api/recetas/${id}/anular`, {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify({ motivo: motivo.trim() || "Anulada por usuario" })
        });
        const data = await res.json();
        if (res.ok) {
            alert(`✅ Receta N° ${numero} ha sido anulada exitosamente.`);
            const searchInput = document.getElementById("search-input");
            const termino = searchInput ? searchInput.value.trim() : "";
            cargarHistorial(termino);
        } else {
            alert("❌ Error al anular la receta: " + (data.detail || "Error desconocido"));
        }
    } catch (err) {
        alert("❌ Error de comunicación al anular receta: " + err);
    }
}

async function eliminarReceta(id, numero) {
    const confirmar = confirm(`¿Está seguro de que desea ELIMINAR definitivamente la Receta N° ${numero}?\n\nEsta acción no se puede deshacer y retirará el registro del libro.`);
    if (!confirmar) return;

    try {
        const res = await fetch(`/api/recetas/${id}`, {
            method: "DELETE"
        });
        const data = await res.json();
        if (res.ok) {
            alert(`✅ Receta N° ${numero} eliminada correctamente.`);
            const searchInput = document.getElementById("search-input");
            const termino = searchInput ? searchInput.value.trim() : "";
            cargarHistorial(termino);
        } else {
            alert("❌ Error al eliminar la receta: " + (data.detail || "Error desconocido"));
        }
    } catch (err) {
        alert("❌ Error de comunicación al eliminar receta: " + err);
    }
}

// 10. Restauración de Base de Datos
async function restaurarBaseDatos() {
    const fileInput = document.getElementById("restore-file-input");
    if (!fileInput || !fileInput.files || fileInput.files.length === 0) {
        alert("⚠️ Por favor seleccione un archivo .db para restaurar.");
        return;
    }

    const file = fileInput.files[0];
    const confirmar = confirm(`⚠️ ADVERTENCIA: Esta acción reemplazará la base de datos actual con "${file.name}".\n\n¿Desea continuar con la restauración?`);
    if (!confirmar) return;

    const formData = new FormData();
    formData.append("file", file);

    try {
        const res = await fetch("/api/restore-db", {
            method: "POST",
            body: formData
        });
        const data = await res.json();
        if (res.ok) {
            alert("✅ " + (data.message || "Base de datos restaurada exitosamente."));
            window.location.reload();
        } else {
            alert("❌ Error al restaurar la base de datos: " + (data.detail || "Error desconocido"));
        }
    } catch (err) {
        alert("❌ Error de comunicación al restaurar: " + err);
    }
}

// 11. Heartbeat y Cierre Limpio de la Aplicación de Escritorio
setInterval(() => {
    fetch('/api/heartbeat').catch(() => {});
}, 2500);

window.addEventListener('beforeunload', () => {
    navigator.sendBeacon('/api/shutdown');
});

