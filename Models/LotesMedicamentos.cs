using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GmlClinicaApi.Models
{
    [Table("lotes_medicamentos")]
    public class LotesMedicamentos
    {
        [Key][Column("id")] public long Id { get; set; }
        [Column("medicamento_id")] public long MedicamentoId { get; set; }
        [Column("fornecedor_id")] public long? FornecedorId { get; set; }
        [Column("numero_lote")] public string NumeroLote { get; set; } = string.Empty;
        [Column("data_fabricacao")] public DateTime? DataFabricacao { get; set; }
        [Column("data_validade")] public DateTime DataValidade { get; set; }
        [Column("quantidade_inicial")] public decimal QuantidadeInicial { get; set; }
        [Column("quantidade_atual")] public decimal QuantidadeAtual { get; set; }
        [Column("preco_custo_unitario")] public decimal PrecoCustoUnitario { get; set; }
        [Column("preco_venda_unitario")] public decimal PrecoVendaUnitario { get; set; }
        [Column("estado_lote")] public string EstadoLote { get; set; } = "DISPONIVEL";
        [Column("created_at")] public DateTime CreatedAt { get; set; }
        [Column("updated_at")] public DateTime UpdatedAt { get; set; }
    }
}
