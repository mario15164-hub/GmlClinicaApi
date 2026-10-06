using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GmlClinicaApi.Data;
using GmlClinicaApi.Models;
using System.Data;

namespace GmlClinicaApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class FilaAtendimentoController : ControllerBase
    {
        private readonly AppDbContext _context;
        public FilaAtendimentoController(AppDbContext context)
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

        [HttpGet]
        public async Task<IActionResult> Listar()
        {
            try
            {
                var lista = new List<object>();
                using var cmd = _context.Database.GetDbConnection().CreateCommand();
                cmd.CommandText = @"SELECT f.*, p.nome_completo AS paciente
                    FROM fila_atendimento f
                    JOIN pacientes p ON p.id = f.paciente_id
                    WHERE f.data_chegada >= NOW(3) - INTERVAL 1 DAY
                    ORDER BY (f.etapa_atual IN ('ATENDIDO','DESISTENCIA')),
                    FIELD(f.prioridade,'EMERGENCIA','PRIORITARIO','NORMAL'), f.data_chegada;";
                if (cmd.Connection!.State != ConnectionState.Open)
                    await cmd.Connection.OpenAsync();
                using var r = await cmd.ExecuteReaderAsync();
                while (await r.ReadAsync())
                {
                    lista.Add(new
                    {
                        id = Convert.ToInt64(r["id"]),
                        agendamentoId = r["agendamento_id"] == DBNull.Value ? (long?)null : Convert.ToInt64(r["agendamento_id"]),
                        pacienteId = Convert.ToInt64(r["paciente_id"]),
                        paciente = S(r, "paciente"),
                        codigoSenha = S(r, "codigo_senha"),
                        prioridade = S(r, "prioridade"),
                        etapaAtual = S(r, "etapa_atual"),
                        dataChegada = Convert.ToDateTime(r["data_chegada"]).ToString("yyyy-MM-dd HH:mm"),
                        dataChamada = r["data_chamada"] == DBNull.Value ? null : Convert.ToDateTime(r["data_chamada"]).ToString("yyyy-MM-dd HH:mm"),
                        dataConclusao = r["data_conclusao"] == DBNull.Value ? null : Convert.ToDateTime(r["data_conclusao"]).ToString("yyyy-MM-dd HH:mm")
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
        public async Task<IActionResult> Chegada([FromBody] FilaAtendimento f)
        {
            try
            {
                var letra = f.Prioridade == "EMERGENCIA" ? "E" : f.Prioridade == "PRIORITARIO" ? "P" : "N";
                var conn = _context.Database.GetDbConnection();
                if (conn.State != ConnectionState.Open) await conn.OpenAsync();
                using var c1 = conn.CreateCommand();
                c1.CommandText = "SELECT COUNT(*) FROM fila_atendimento WHERE codigo_senha LIKE @l AND data_chegada >= CURDATE();";
                Par(c1, "@l", letra + "%");
                var n = Convert.ToInt32(await c1.ExecuteScalarAsync()) + 1;
                var senha = letra + n.ToString("D3");
                using var c2 = conn.CreateCommand();
                c2.CommandText = @"INSERT INTO fila_atendimento (agendamento_id, paciente_id, codigo_senha, prioridade)
                    VALUES (@a, @p, @s, @pr); SELECT LAST_INSERT_ID();";
                Par(c2, "@a", f.AgendamentoId);
                Par(c2, "@p", f.PacienteId);
                Par(c2, "@s", senha);
                Par(c2, "@pr", f.Prioridade);
                var id = Convert.ToInt64(await c2.ExecuteScalarAsync());
                if (f.AgendamentoId != null)
                {
                    using var c3 = conn.CreateCommand();
                    c3.CommandText = "UPDATE agendamentos SET estado='EM_ESPERA' WHERE id=@a AND is_deleted=0;";
                    Par(c3, "@a", f.AgendamentoId);
                    await c3.ExecuteNonQueryAsync();
                }
                return Ok(new { id, codigoSenha = senha });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { erro = ex.Message });
            }
        }

        [HttpPut("{id}/etapa")]
        public async Task<IActionResult> AvancarEtapa(long id, [FromQuery] string etapa)
        {
            try
            {
                var validas = new[] { "AGUARDANDO_TRIAGEM", "EM_TRIAGEM", "AGUARDANDO_CONSULTA", "EM_CONSULTA", "ATENDIDO", "DESISTENCIA" };
                if (!validas.Contains(etapa)) return BadRequest(new { erro = "Etapa inválida." });
                var conn = _context.Database.GetDbConnection();
                if (conn.State != ConnectionState.Open) await conn.OpenAsync();
                using var c1 = conn.CreateCommand();
                c1.CommandText = @"UPDATE fila_atendimento SET etapa_atual=@e,
                    data_chamada = CASE WHEN @e IN ('EM_TRIAGEM','EM_CONSULTA') THEN NOW(3) ELSE data_chamada END,
                    data_conclusao = CASE WHEN @e IN ('ATENDIDO','DESISTENCIA') THEN NOW(3) ELSE data_conclusao END
                    WHERE id=@id;";
                Par(c1, "@e", etapa);
                Par(c1, "@id", id);
                var linhas = await c1.ExecuteNonQueryAsync();
                if (linhas == 0) return NotFound(new { erro = "Registro não encontrado." });
                string? novo = etapa == "EM_CONSULTA" ? "EM_ATENDIMENTO" : etapa == "ATENDIDO" ? "CONCLUIDO" : null;
                if (novo != null)
                {
                    using var c2 = conn.CreateCommand();
                    c2.CommandText = "UPDATE agendamentos SET estado=@s WHERE id=(SELECT agendamento_id FROM fila_atendimento WHERE id=@id) AND is_deleted=0;";
                    Par(c2, "@s", novo);
                    Par(c2, "@id", id);
                    await c2.ExecuteNonQueryAsync();
                }
                return Ok(new { id, etapa });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { erro = ex.Message });
            }
        }
    }
}
