using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GmlClinicaApi.Models
{
    [Table("prescricoes")]
    public class Prescricoes
    {
        [Key][Column("id")] public long Id { get; set; }
        [Column("uuid")] public string? Uuid { get; set; }
        [Column("consulta_id")] public long ConsultaId { get; set; }
        [Column("paciente_id")] public long PacienteId { get; set; }
        [Column("medico_id")] public long MedicoId { get; set; }
        [Column("data_prescricao")] public DateTime DataPrescricao { get; set; }
        [Column("validade_receita_dias")] public int ValidadeReceitaDias { get; set; } = 30;
        [Column("estado")] public string Estado { get; set; } = "PENDENTE";
        [Column("observacoes_medicas")] public string? ObservacoesMedicas { get; set; }
        [Column("created_at")] public DateTime CreatedAt { get; set; }
        [Column("updated_at")] public DateTime UpdatedAt { get; set; }
        [Column("created_by")] public long? CreatedBy { get; set; }
        [Column("updated_by")] public long? UpdatedBy { get; set; }
    }
}
