using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GmlClinicaApi.Models
{
    [Table("fila_atendimento")]
    public class FilaAtendimento
    {
        [Key][Column("id")] public long Id { get; set; }
        [Column("agendamento_id")] public long? AgendamentoId { get; set; }
        [Column("paciente_id")] public long PacienteId { get; set; }
        [Column("codigo_senha")] public string CodigoSenha { get; set; } = string.Empty;
        [Column("prioridade")] public string Prioridade { get; set; } = "NORMAL";
        [Column("etapa_atual")] public string EtapaAtual { get; set; } = "AGUARDANDO_TRIAGEM";
        [Column("data_chegada")] public DateTime DataChegada { get; set; }
        [Column("data_chamada")] public DateTime? DataChamada { get; set; }
        [Column("data_conclusao")] public DateTime? DataConclusao { get; set; }
    }
}
