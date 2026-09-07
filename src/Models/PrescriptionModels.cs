using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace RecetarioAgrocalidad.Models;

public class Prescription
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("numero_receta")]
    public string NumeroReceta { get; set; } = string.Empty;

    [JsonPropertyName("numero_entero")]
    public int NumeroEntero { get; set; }

    [JsonPropertyName("fecha_emision")]
    public string FechaEmision { get; set; } = string.Empty;

    [JsonPropertyName("dia")]
    public string Dia { get; set; } = string.Empty;

    [JsonPropertyName("mes")]
    public string Mes { get; set; } = string.Empty;

    [JsonPropertyName("anio")]
    public string Anio { get; set; } = string.Empty;

    [JsonPropertyName("especie")]
    public string Especie { get; set; } = string.Empty;

    [JsonPropertyName("nombre_paciente")]
    public string NombrePaciente { get; set; } = string.Empty;

    [JsonPropertyName("sexo")]
    public string Sexo { get; set; } = "Macho";

    [JsonPropertyName("edad")]
    public string Edad { get; set; } = string.Empty;

    [JsonPropertyName("nombre_propietario")]
    public string NombrePropietario { get; set; } = string.Empty;

    [JsonPropertyName("direccion_propietario")]
    public string DireccionPropietario { get; set; } = string.Empty;

    [JsonPropertyName("prescripcion")]
    public string Prescripcion { get; set; } = string.Empty;

    [JsonPropertyName("diagnostico")]
    public string Diagnostico { get; set; } = string.Empty;

    [JsonPropertyName("posologia")]
    public string Posologia { get; set; } = string.Empty;

    [JsonPropertyName("instrucciones")]
    public string Instrucciones { get; set; } = string.Empty;

    [JsonPropertyName("estado")]
    public string Estado { get; set; } = "EMITIDA";

    [JsonPropertyName("motivo_anulacion")]
    public string? MotivoAnulacion { get; set; }

    [JsonPropertyName("created_at")]
    public string? CreatedAt { get; set; }
}

public class PrescriptionInput
{
    [JsonPropertyName("dia")]
    public string Dia { get; set; } = string.Empty;

    [JsonPropertyName("mes")]
    public string Mes { get; set; } = string.Empty;

    [JsonPropertyName("anio")]
    public string Anio { get; set; } = string.Empty;

    [JsonPropertyName("especie")]
    public string Especie { get; set; } = string.Empty;

    [JsonPropertyName("nombre_paciente")]
    public string? NombrePaciente { get; set; } = "No aplica";

    [JsonPropertyName("sexo")]
    public string Sexo { get; set; } = "Macho";

    [JsonPropertyName("edad")]
    public string? Edad { get; set; } = "No especificada";

    [JsonPropertyName("nombre_propietario")]
    public string NombrePropietario { get; set; } = string.Empty;

    [JsonPropertyName("direccion_propietario")]
    public string? DireccionPropietario { get; set; } = "Particular";

    [JsonPropertyName("prescripcion")]
    public string Prescripcion { get; set; } = string.Empty;

    [JsonPropertyName("diagnostico")]
    public string? Diagnostico { get; set; } = "Evaluación clínica";

    [JsonPropertyName("posologia")]
    public string Posologia { get; set; } = string.Empty;

    [JsonPropertyName("instrucciones")]
    public string? Instrucciones { get; set; } = "Sin novedades";

    /// <summary>
    /// Limpia espacios en blanco y normaliza formatos de fecha y campos por defecto.
    /// </summary>
    public void Normalize()
    {
        Dia = string.IsNullOrWhiteSpace(Dia) ? DateTime.Now.ToString("dd") : Dia.Trim().PadLeft(2, '0');
        Mes = string.IsNullOrWhiteSpace(Mes) ? DateTime.Now.ToString("MM") : Mes.Trim().PadLeft(2, '0');
        Anio = string.IsNullOrWhiteSpace(Anio) ? DateTime.Now.ToString("yyyy") : Anio.Trim();

        Especie = Especie?.Trim() ?? string.Empty;
        NombrePaciente = string.IsNullOrWhiteSpace(NombrePaciente) ? "No aplica" : NombrePaciente.Trim();
        Sexo = string.IsNullOrWhiteSpace(Sexo) ? "Macho" : Sexo.Trim();
        Edad = string.IsNullOrWhiteSpace(Edad) ? "No especificada" : Edad.Trim();
        NombrePropietario = NombrePropietario?.Trim() ?? string.Empty;
        DireccionPropietario = string.IsNullOrWhiteSpace(DireccionPropietario) ? "Particular" : DireccionPropietario.Trim();
        Prescripcion = Prescripcion?.Trim() ?? string.Empty;
        Diagnostico = string.IsNullOrWhiteSpace(Diagnostico) ? "Evaluación clínica" : Diagnostico.Trim();
        Posologia = Posologia?.Trim() ?? string.Empty;
        Instrucciones = string.IsNullOrWhiteSpace(Instrucciones) ? "Sin novedades" : Instrucciones.Trim();
    }

    /// <summary>
    /// Valida los campos obligatorios para emisión conforme a la normativa de Agrocalidad Ecuador.
    /// </summary>
    public bool Validate(out string? errorMessage)
    {
        Normalize();

        if (string.IsNullOrWhiteSpace(NombrePropietario))
        {
            errorMessage = "El nombre del propietario o tenedor es obligatorio.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(Especie))
        {
            errorMessage = "La especie del animal es obligatoria.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(Prescripcion))
        {
            errorMessage = "El detalle de la prescripción farmacológica es obligatorio (principio activo, forma farmacéutica, concentración y unidades).";
            return false;
        }

        if (string.IsNullOrWhiteSpace(Posologia))
        {
            errorMessage = "La posología es obligatoria (vía de administración, dosis por unidad de tiempo y duración).";
            return false;
        }

        if (!int.TryParse(Dia, out int d) || d < 1 || d > 31)
        {
            errorMessage = "El día de emisión no es válido (debe ser entre 01 y 31).";
            return false;
        }

        if (!int.TryParse(Mes, out int m) || m < 1 || m > 12)
        {
            errorMessage = "El mes de emisión no es válido (debe ser entre 01 y 12).";
            return false;
        }

        if (!int.TryParse(Anio, out int a) || a < 2000 || a > 2100)
        {
            errorMessage = "El año de emisión no es válido.";
            return false;
        }

        errorMessage = null;
        return true;
    }
}

public class DoctorConfig
{
    [JsonPropertyName("veterinario_nombre")]
    public string VeterinarioNombre { get; set; } = string.Empty;

    [JsonPropertyName("veterinario_cedula")]
    public string VeterinarioCedula { get; set; } = string.Empty;

    [JsonPropertyName("veterinario_senescyt")]
    public string VeterinarioSenescyt { get; set; } = string.Empty;

    [JsonPropertyName("veterinario_telefono")]
    public string VeterinarioTelefono { get; set; } = string.Empty;

    [JsonPropertyName("establecimiento_nombre")]
    public string EstablecimientoNombre { get; set; } = string.Empty;
}

public class AnularInput
{
    [JsonPropertyName("motivo")]
    public string Motivo { get; set; } = "Anulada por usuario";
}

public class NextNumberResponse
{
    [JsonPropertyName("next_number")]
    public int NextNumber { get; set; }

    [JsonPropertyName("next_number_formatted")]
    public string NextNumberFormatted { get; set; } = "0001";
}

public class CreateRecipeResponse
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("numero_receta")]
    public string NumeroReceta { get; set; } = string.Empty;

    [JsonPropertyName("status")]
    public string Status { get; set; } = "success";

    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;
}

public class GenericResponse
{
    [JsonPropertyName("status")]
    public string Status { get; set; } = "success";

    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;

    [JsonPropertyName("detail")]
    public string? Detail { get; set; }
}

[JsonSourceGenerationOptions(WriteIndented = false)]
[JsonSerializable(typeof(Prescription))]
[JsonSerializable(typeof(List<Prescription>))]
[JsonSerializable(typeof(PrescriptionInput))]
[JsonSerializable(typeof(DoctorConfig))]
[JsonSerializable(typeof(AnularInput))]
[JsonSerializable(typeof(NextNumberResponse))]
[JsonSerializable(typeof(CreateRecipeResponse))]
[JsonSerializable(typeof(GenericResponse))]
[JsonSerializable(typeof(Dictionary<string, string>))]
public partial class AppJsonContext : JsonSerializerContext
{
}
