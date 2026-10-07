using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GmlClinicaApi.Models
{
    [Table("prescricao_itens")]
    public class PrescricaoItens
    {
        [Key][Column("id")] public long Id { get; set; }
        [Column("prescricao_id")] public long PrescricaoId { get; set; }
        [Column("medicamento_id")] public long MedicamentoId { get; set; }
        [Column("posologia_instrucao")] public string PosologiaInstrucao { get; set; } = string.Empty;
        [Column("quantidade_prescrita")] public decimal QuantidadePrescrita { get; set; }
        [Column("quantidade_dispensada")] public decimal QuantidadeDispensada { get; set; } = 0;
    }
}
