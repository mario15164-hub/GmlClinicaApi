using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GmlClinicaApi.Models
{
    [Table("fornecedores")]
    public class Fornecedores
    {
        [Key][Column("id")] public long Id { get; set; }
        [Column("nome_razao_social")] public string NomeRazaoSocial { get; set; } = string.Empty;
        [Column("nif")] public string? Nif { get; set; }
        [Column("telefone")] public string? Telefone { get; set; }
        [Column("email")] public string? Email { get; set; }
        [Column("endereco")] public string? Endereco { get; set; }
        [Column("estado")] public string Estado { get; set; } = "ATIVO";
        [Column("created_at")] public DateTime CreatedAt { get; set; }
        [Column("updated_at")] public DateTime UpdatedAt { get; set; }
    }
}
