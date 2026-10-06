using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GmlClinicaApi.Models
{
    [Table("triagens")]
    public class Triagens
    {
        [Key][Column("id")] public long Id { get; set; }
        [Column("uuid")] public string? Uuid { get; set; }
        [Column("fila_id")] public long FilaId { get; set; }
        [Column("paciente_id")] public long PacienteId { get; set; }
        [Column("enfermeiro_id")] public long? EnfermeiroId { get; set; }
        [Column("pressao_sistolica")] public int? PressaoSistolica { get; set; }
        [Column("pressao_diastolica")] public int? PressaoDiastolica { get; set; }
        [Column("frequencia_cardiaca")] public int? FrequenciaCardiaca { get; set; }
        [Column("frequencia_respiratoria")] public int? FrequenciaRespiratoria { get; set; }
        [Column("temperatura")] public decimal? Temperatura { get; set; }
        [Column("saturacao_oxigenio")] public int? SaturacaoOxigenio { get; set; }
        [Column("peso")] public decimal? Peso { get; set; }
        [Column("altura")] public decimal? Altura { get; set; }
        [Column("imc")] public decimal? Imc { get; set; }
        [Column("glicemia_capilar")] public int? GlicemiaCapilar { get; set; }
        [Column("nivel_consciencia")] public string? NivelConsciencia { get; set; }
        [Column("classificacao_risco")] public string ClassificacaoRisco { get; set; } = "AZUL_NAO_URGENTE";
        [Column("queixa_principal")] public string? QueixaPrincipal { get; set; }
        [Column("created_at")] public DateTime CreatedAt { get; set; }
        [Column("updated_at")] public DateTime UpdatedAt { get; set; }
        [Column("created_by")] public long? CreatedBy { get; set; }
        [Column("updated_by")] public long? UpdatedBy { get; set; }
    }
}
