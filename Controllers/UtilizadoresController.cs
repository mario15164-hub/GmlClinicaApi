using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GmlClinicaApi.Data;
using GmlClinicaApi.Models;
using System.Data;
using System.Security.Cryptography;

namespace GmlClinicaApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UtilizadoresController : ControllerBase
    {
        private readonly AppDbContext _context;
        public UtilizadoresController(AppDbContext context)
        {
            _context = context;
        }

        private static string? S(IDataReader r, string c)
        {
            var v = r[c];
            return v == DBNull.Value ? null : v.ToString();
        }

        private static void Par(System.Data.Common.DbCommand c, string n, object? v)
        {
            var p = c.CreateParameter();
            p.ParameterName = n;
            p.Value = v ?? DBNull.Value;
            c.Parameters.Add(p);
        }

        private static string GerarHash(string senha)
        {
            var salt = RandomNumberGenerator.GetBytes(16);
            var hash = Rfc2898DeriveBytes.Pbkdf2(senha, salt, 100000, HashAlgorithmName.SHA256, 32);
            return "pbkdf2$100000$" + Convert.ToBase64String(salt) + "$" + Convert.ToBase64String(hash);
        }

        [HttpGet]
        public async Task<IActionResult> Listar()
        {
            try
            {
                var lista = new List<object>();
                using var cmd = _context.Database.GetDbConnection().CreateCommand();
                cmd.CommandText = "SELECT id, nome_completo, email, telefone, numero_registro_profissional, estado, ultimo_login FROM utilizadores WHERE is_deleted = 0 ORDER BY id DESC;";
                if (cmd.Connection!.State != ConnectionState.Open)
                    await cmd.Connection.OpenAsync();
                using var r = await cmd.ExecuteReaderAsync();
                while (await r.ReadAsync())
                {
                    lista.Add(new
                    {
                        id = Convert.ToInt64(r["id"]),
                        nomeCompleto = S(r, "nome_completo"),
                        email = S(r, "email"),
                        telefone = S(r, "telefone"),
                        numeroRegistroProfissional = S(r, "numero_registro_profissional"),
                        estado = S(r, "estado"),
                        ultimoLogin = r["ultimo_login"] == DBNull.Value ? null : Convert.ToDateTime(r["ultimo_login"]).ToString("yyyy-MM-dd HH:mm")
                    });
                }
                return Ok(lista);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { erro = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> Criar([FromBody] Utilizadores u)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(u.Senha) || u.Senha.Length < 6)
                    return BadRequest(new { erro = "A senha deve ter pelo menos 6 caracteres." });
                using var cmd = _context.Database.GetDbConnection().CreateCommand();
                cmd.CommandText = @"INSERT INTO utilizadores
                    (uuid, nome_completo, email, senha_hash, telefone, numero_registro_profissional, estado)
                    VALUES (@u, @n, @e, @s, @t, @r, @es);
                    SELECT LAST_INSERT_ID();";
                Par(cmd, "@u", Guid.NewGuid().ToString());
                Par(cmd, "@n", u.NomeCompleto);
                Par(cmd, "@e", u.Email);
                Par(cmd, "@s", GerarHash(u.Senha));
                Par(cmd, "@t", u.Telefone);
                Par(cmd, "@r", u.NumeroRegistroProfissional);
                Par(cmd, "@es", u.Estado);
                if (cmd.Connection!.State != ConnectionState.Open)
                    await cmd.Connection.OpenAsync();
                var id = Convert.ToInt64(await cmd.ExecuteScalarAsync());
                return Ok(new { id });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { erro = ex.Message });
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Editar(long id, [FromBody] Utilizadores u)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(u.Senha) && u.Senha.Length < 6)
                    return BadRequest(new { erro = "A senha deve ter pelo menos 6 caracteres." });
                var sql = "UPDATE utilizadores SET nome_completo=@n, email=@e, telefone=@t, numero_registro_profissional=@r, estado=@es";
                if (!string.IsNullOrWhiteSpace(u.Senha)) sql += ", senha_hash=@s";
                sql += " WHERE id=@id AND is_deleted=0;";
                using var cmd = _context.Database.GetDbConnection().CreateCommand();
                cmd.CommandText = sql;
                Par(cmd, "@n", u.NomeCompleto);
                Par(cmd, "@e", u.Email);
                Par(cmd, "@t", u.Telefone);
                Par(cmd, "@r", u.NumeroRegistroProfissional);
                Par(cmd, "@es", u.Estado);
                if (!string.IsNullOrWhiteSpace(u.Senha)) Par(cmd, "@s", GerarHash(u.Senha));
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

        [HttpDelete("{id}")]
        public async Task<IActionResult> Eliminar(long id)
        {
            try
            {
                using var cmd = _context.Database.GetDbConnection().CreateCommand();
                cmd.CommandText = "UPDATE utilizadores SET is_deleted=1, deleted_at=NOW(3) WHERE id=@id;";
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
