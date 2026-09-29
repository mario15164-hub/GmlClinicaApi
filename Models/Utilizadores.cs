using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace GmlClinicaApi.Models
{
    [Table("utilizadores")]
    public class Utilizadores
    {
        [Key][Column("id")] public long Id { get; set; }
        [Column("uuid")] public string? Uuid { get; set; }
        [Column("nome_completo")] public string NomeCompleto { get; set; } = string.Empty;
        [Column("email")] public string Email { get; set; } = string.Empty;
        [JsonIgnore][Column("senha_hash")] public string SenhaHash { get; set; } = string.Empty;
        [NotMapped] public string? Senha { get; set; }
        [Column("telefone")] public string? Telefone { get; set; }
        [Column("numero_registro_profissional")] public string? NumeroRegistroProfissional { get; set; }
        [Column("estado")] public string Estado { get; set; } = "PENDENTE";
        [Column("ultimo_login")] public DateTime? UltimoLogin { get; set; }
        [Column("created_at")] public DateTime CreatedAt { get; set; }
        [Column("updated_at")] public DateTime UpdatedAt { get; set; }
        [Column("is_deleted")] public bool IsDeleted { get; set; }
    }
}
