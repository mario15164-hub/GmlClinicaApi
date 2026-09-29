using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GmlClinicaApi.Models
{
    [Table("pacientes")]
    public class Pacientes
    {
        [Key]
        [Column("id")]
        public long Id { get; set; }

        [Column("uuid")]
        public string? Uuid { get; set; }

        [Column("nome_completo")]
        public string NomeCompleto { get; set; } = string.Empty;

        [Column("nome_social")]
        public string? NomeSocial { get; set; }

        [Column("data_nascimento")]
        public DateTime? DataNascimento { get; set; }

        [Column("genero")]
        public string? Genero { get; set; }

        [Column("estado_civil")]
        public string? EstadoCivil { get; set; }

        [Column("nome_mae")]
        public string? NomeMae { get; set; }

        [Column("nome_pai")]
        public string? NomePai { get; set; }

        [Column("grupo_sanguineo")]
        public string? GrupoSanguineo { get; set; }

        [Column("nacionalidade")]
        public string? Nacionalidade { get; set; }

        [Column("provincia_residencia")]
        public string? ProvinciaResidencia { get; set; }

        [Column("municipio_residencia")]
        public string? MunicipioResidencia { get; set; }

        [Column("endereco_linha")]
        public string? EnderecoLinha { get; set; }

        [Column("telefone_principal")]
        public string? TelefonePrincipal { get; set; }

        [Column("telefone_secundario")]
        public string? TelefoneSecundario { get; set; }

        [Column("email")]
        public string? Email { get; set; }

        [Column("estado")]
        public string? Estado { get; set; } = "ATIVO";

        [Column("created_at")]
        public DateTime? CreatedAt { get; set; }

        [Column("updated_at")]
        public DateTime? UpdatedAt { get; set; }

        [Column("is_deleted")]
        public bool IsDeleted { get; set; } = false;

        [Column("deleted_at")]
        public DateTime? DeletedAt { get; set; }
    }
}
