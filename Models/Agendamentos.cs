using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GmlClinicaApi.Models
{
    [Table("agendamentos")]
    public class Agendamentos
    {
        [Key][Column("id")] public long Id { get; set; }
        [Column("uuid")] public string? Uuid { get; set; }
        [Column("paciente_id")] public long PacienteId { get; set; }
        [Column("medico_id")] public long? MedicoId { get; set; }
        [Column("servico_id")] public long? ServicoId { get; set; }
        [Column("data_hora_inicio")] public DateTime DataHoraInicio { get; set; }
        [Column("data_hora_fim")] public DateTime DataHoraFim { get; set; }
        [Column("tipo_consulta")] public string TipoConsulta { get; set; } = "PRIMEIRA_CONSULTA";
        [Column("estado")] public string Estado { get; set; } = "AGENDADO";
        [Column("motivo_cancelamento")] public string? MotivoCancelamento { get; set; }
        [Column("observacoes")] public string? Observacoes { get; set; }
        [Column("created_at")] public DateTime CreatedAt { get; set; }
        [Column("updated_at")] public DateTime UpdatedAt { get; set; }
        [Column("is_deleted")] public bool IsDeleted { get; set; }
    }
}
