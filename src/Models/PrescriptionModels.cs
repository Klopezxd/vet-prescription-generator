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
