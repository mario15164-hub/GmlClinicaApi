using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GmlClinicaApi.Data;
using GmlClinicaApi.Models;
using System.Data;

namespace GmlClinicaApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class FornecedoresController : ControllerBase
    {
        private readonly AppDbContext _context;
        public FornecedoresController(AppDbContext context)
        {
            _context = context;
        }

        private static string? S(IDataReader r, string c)
        {
            var v = r[c];
            return v == DBNull.Value ? null : v.ToString();
        }

        private static Fornecedores Map(IDataReader r)
        {
            return new Fornecedores
            {
                Id = Convert.ToInt64(r["id"]),
                NomeRazaoSocial = S(r, "nome_razao_social") ?? "",
                Nif = S(r, "nif"),
                Telefone = S(r, "telefone"),
                Email = S(r, "email"),
                Endereco = S(r, "endereco"),
                Estado = S(r, "estado") ?? "ATIVO",
                CreatedAt = Convert.ToDateTime(r["created_at"]),
                UpdatedAt = Convert.ToDateTime(r["updated_at"])
            };
        }

        [HttpGet]
        public async Task<IActionResult> GetFornecedores()
        {
            try
            {
                var lista = new List<Fornecedores>();
                using var cmd = _context.Database.GetDbConnection().CreateCommand();
                cmd.CommandText = "SELECT * FROM fornecedores ORDER BY id DESC;";
                if (cmd.Connection!.State != ConnectionState.Open)
                    await cmd.Connection.OpenAsync();
                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                    lista.Add(Map(reader));
                return Ok(lista);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { erro = ex.Message });
            }
        }

        private static void Par(System.Data.Common.DbCommand c, string n, object? v)
        {
            var p = c.CreateParameter();
            p.ParameterName = n;
            p.Value = v ?? DBNull.Value;
            c.Parameters.Add(p);
        }

        [HttpPost]
        public async Task<IActionResult> Criar([FromBody] Fornecedores f)
        {
            try
            {
                using var cmd = _context.Database.GetDbConnection().CreateCommand();
                cmd.CommandText = "INSERT INTO fornecedores (nome_razao_social, nif, telefone, email, endereco, estado) VALUES (@n, @nif, @t, @e, @en, @es); SELECT LAST_INSERT_ID();";
                Par(cmd, "@n", f.NomeRazaoSocial);
                Par(cmd, "@nif", f.Nif);
                Par(cmd, "@t", f.Telefone);
                Par(cmd, "@e", f.Email);
                Par(cmd, "@en", f.Endereco);
                Par(cmd, "@es", f.Estado);
                if (cmd.Connection!.State != ConnectionState.Open)
                    await cmd.Connection.OpenAsync();
                f.Id = Convert.ToInt64(await cmd.ExecuteScalarAsync());
                return Ok(f);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { erro = ex.Message });
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Editar(long id, [FromBody] Fornecedores f)
        {
            try
            {
                using var cmd = _context.Database.GetDbConnection().CreateCommand();
                cmd.CommandText = "UPDATE fornecedores SET nome_razao_social=@n, nif=@nif, telefone=@t, email=@e, endereco=@en, estado=@es WHERE id=@id;";
                Par(cmd, "@n", f.NomeRazaoSocial);
                Par(cmd, "@nif", f.Nif);
                Par(cmd, "@t", f.Telefone);
                Par(cmd, "@e", f.Email);
                Par(cmd, "@en", f.Endereco);
                Par(cmd, "@es", f.Estado);
                Par(cmd, "@id", id);
                if (cmd.Connection!.State != ConnectionState.Open)
                    await cmd.Connection.OpenAsync();
                await cmd.ExecuteNonQueryAsync();
                f.Id = id;
                return Ok(f);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { erro = ex.Message });
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Desativar(long id)
        {
            try
            {
                using var cmd = _context.Database.GetDbConnection().CreateCommand();
                cmd.CommandText = "UPDATE fornecedores SET estado='INATIVO' WHERE id=@id;";
                Par(cmd, "@id", id);
                if (cmd.Connection!.State != ConnectionState.Open)
                    await cmd.Connection.OpenAsync();
                await cmd.ExecuteNonQueryAsync();
                return Ok(new { id });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { erro = ex.Message });
            }
        }
    }
}
