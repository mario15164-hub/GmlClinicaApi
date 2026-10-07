using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GmlClinicaApi.Models
{
    [Table("consultas_atendimentos")]
    public class Consultas
    {
        [Key][Column("id")] public long Id { get; set; }
        [Column("uuid")] public string? Uuid { get; set; }
        [Column("agendamento_id")] public long? AgendamentoId { get; set; }
        [Column("triagem_id")] public long? TriagemId { get; set; }
        [Column("paciente_id")] public long PacienteId { get; set; }
        [Column("medico_id")] public long MedicoId { get; set; }
        [Column("anamnese_historia")] public string AnamneseHistoria { get; set; } = string.Empty;
        [Column("data_inicio")] public DateTime DataInicio { get; set; }
        [Column("data_fim")] public DateTime? DataFim { get; set; }
        [Column("exame_fisico")] public string? ExameFisico { get; set; }
        [Column("hipotese_diagnostica")] public string? HipoteseDiagnostica { get; set; }
        [Column("conduta_plano")] public string? CondutaPlano { get; set; }
        [Column("estado")] public string Estado { get; set; } = "EM_ANDAMENTO";
        [Column("created_at")] public DateTime CreatedAt { get; set; }
        [Column("updated_at")] public DateTime UpdatedAt { get; set; }
        [Column("created_by")] public long? CreatedBy { get; set; }
        [Column("updated_by")] public long? UpdatedBy { get; set; }
        [Column("is_deleted")] public bool IsDeleted { get; set; }
        [Column("deleted_at")] public DateTime? DeletedAt { get; set; }
    }
}
