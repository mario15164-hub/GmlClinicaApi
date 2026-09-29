using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GmlClinicaApi.Models
{
    [Table("medicamentos")]
    public class Medicamentos
    {
        [Key]
        [Column("id")]
        public long Id { get; set; }

        [Column("uuid")]
        public string? Uuid { get; set; }

        [Column("nome_generico")]
        public string NomeGenerico { get; set; } = string.Empty;

        [Column("nome_comercial")]
        public string? NomeComercial { get; set; }

        [Column("categoria")]
        public string Categoria { get; set; } = string.Empty;

        [Column("forma_farmaceutica")]
        public string FormaFarmaceutica { get; set; } = string.Empty;

        [Column("concentracao")]
        public string Concentracao { get; set; } = string.Empty;

        [Column("sujeito_receita_especial")]
        public bool SujeitoReceitaEspecial { get; set; } = false;

        [Column("estoque_minimo_alerta")]
        public decimal EstoqueMinimoAlerta { get; set; } = 10.0000m;

        [Column("estado")]
        public string? Estado { get; set; } = "ATIVO";

        [Column("created_at")]
        public DateTime? CreatedAt { get; set; }

        [Column("updated_at")]
        public DateTime? UpdatedAt { get; set; }
    }
}
