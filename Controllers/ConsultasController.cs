using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GmlClinicaApi.Data;
using GmlClinicaApi.Models;
using System.Data;

namespace GmlClinicaApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ConsultasController : ControllerBase
    {
        private readonly AppDbContext _context;
        public ConsultasController(AppDbContext context)
        {
            _context = context;
        }

        private static string? S(IDataReader r, string c)
        {
            var v = r[c];
            return v == DBNull.Value ? null : v.ToString();
        }

        private static string? DT(IDataReader r, string c)
        {
            return r[c] == DBNull.Value ? null : Convert.ToDateTime(r[c]).ToString("yyyy-MM-dd HH:mm");
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
                cmd.CommandText = @"SELECT c.*, p.nome_completo AS paciente, m.nome_completo AS medico
                    FROM consultas_atendimentos c
                    JOIN pacientes p ON p.id = c.paciente_id
                    LEFT JOIN utilizadores m ON m.id = c.medico_id
                    WHERE c.is_deleted = 0
                    AND c.created_at >= NOW(3) - INTERVAL 1 DAY
                    ORDER BY (c.estado = 'EM_ANDAMENTO'), c.data_inicio DESC;";
                if (cmd.Connection!.State != ConnectionState.Open)
                    await cmd.Connection.OpenAsync();
                using var r = await cmd.ExecuteReaderAsync();
                while (await r.ReadAsync())
                {
                    lista.Add(new
                    {
                        id = Convert.ToInt64(r["id"]),
                        uuid = S(r, "uuid"),
                        agendamentoId = r["agendamento_id"] == DBNull.Value ? (long?)null : Convert.ToInt64(r["agendamento_id"]),
                        triagemId = r["triagem_id"] == DBNull.Value ? (long?)null : Convert.ToInt64(r["triagem_id"]),
                        pacienteId = Convert.ToInt64(r["paciente_id"]),
                        paciente = S(r, "paciente"),
                        medicoId = Convert.ToInt64(r["medico_id"]),
                        medico = S(r, "medico"),
                        anamneseHistoria = S(r, "anamnese_historia"),
                        dataInicio = DT(r, "data_inicio"),
                        dataFim = DT(r, "data_fim"),
                        exameFisico = S(r, "exame_fisico"),
                        hipoteseDiagnostica = S(r, "hipotese_diagnostica"),
                        condutaPlano = S(r, "conduta_plano"),
                        estado = S(r, "estado"),
                        createdAt = DT(r, "created_at"),
                        updatedAt = DT(r, "updated_at"),
                        createdBy = r["created_by"] == DBNull.Value ? (long?)null : Convert.ToInt64(r["created_by"]),
                        updatedBy = r["updated_by"] == DBNull.Value ? (long?)null : Convert.ToInt64(r["updated_by"])
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
        public async Task<IActionResult> Criar([FromBody] Consultas c)
        {
            try
            {
                if (c.PacienteId <= 0) return BadRequest(new { erro = "Informe o paciente." });
                if (c.MedicoId <= 0) return BadRequest(new { erro = "Informe o médico." });
                if (string.IsNullOrWhiteSpace(c.AnamneseHistoria))
                    return BadRequest(new { erro = "Informe a anamnese/história." });
                var conn = _context.Database.GetDbConnection();
                if (conn.State != ConnectionState.Open) await conn.OpenAsync();
                var pacienteId = c.PacienteId;
                if (c.TriagemId != null)
                {
                    using var c1 = conn.CreateCommand();
                    c1.CommandText = "SELECT paciente_id FROM triagens WHERE id=@t;";
                    Par(c1, "@t", c.TriagemId);
                    var p = await c1.ExecuteScalarAsync();
                    if (p == null || p == DBNull.Value)
                        return BadRequest(new { erro = "Registro de triagem não encontrado." });
                    pacienteId = Convert.ToInt64(p);
                }
                if (c.AgendamentoId != null)
                {
                    using var c2 = conn.CreateCommand();
                    c2.CommandText = "SELECT id FROM agendamentos WHERE id=@a AND is_deleted=0;";
                    Par(c2, "@a", c.AgendamentoId);
                    var a = await c2.ExecuteScalarAsync();
                    if (a == null || a == DBNull.Value)
                        return BadRequest(new { erro = "Agendamento não encontrado." });
                }
                using var cmd = conn.CreateCommand();
                cmd.CommandText = @"INSERT INTO consultas_atendimentos
                    (uuid, agendamento_id, triagem_id, paciente_id, medico_id, anamnese_historia,
                    data_inicio, exame_fisico, hipotese_diagnostica, conduta_plano, estado,
                    created_at, updated_at, created_by, updated_by, is_deleted)
                    VALUES (@u, @a, @t, @pac, @m, @an, @now, @ex, @hip, @con, 'EM_ANDAMENTO',
                    @now, @now, @cb, @ub, 0);
                    SELECT LAST_INSERT_ID();";
                Par(cmd, "@u", Guid.NewGuid().ToString());
                Par(cmd, "@a", c.AgendamentoId);
                Par(cmd, "@t", c.TriagemId);
                Par(cmd, "@pac", pacienteId);
                Par(cmd, "@m", c.MedicoId);
                Par(cmd, "@an", c.AnamneseHistoria);
                var now = DateTime.Now;
                Par(cmd, "@now", now);
                Par(cmd, "@ex", c.ExameFisico);
                Par(cmd, "@hip", c.HipoteseDiagnostica);
                Par(cmd, "@con", c.CondutaPlano);
                Par(cmd, "@cb", c.MedicoId);
                Par(cmd, "@ub", c.MedicoId);
                var id = Convert.ToInt64(await cmd.ExecuteScalarAsync());
                return Ok(new { id });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { erro = ex.Message });
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Editar(long id, [FromBody] Consultas c)
        {
            try
            {
                if (c.MedicoId <= 0) return BadRequest(new { erro = "Informe o médico." });
                if (string.IsNullOrWhiteSpace(c.AnamneseHistoria))
                    return BadRequest(new { erro = "Informe a anamnese/história." });
                var now = DateTime.Now;
                using var cmd = _context.Database.GetDbConnection().CreateCommand();
                cmd.CommandText = @"UPDATE consultas_atendimentos SET
                    medico_id=@m, anamnese_historia=@an, exame_fisico=@ex,
                    hipotese_diagnostica=@hip, conduta_plano=@con,
                    updated_at=@now, updated_by=@ub
                    WHERE id=@id AND is_deleted=0;";
                Par(cmd, "@m", c.MedicoId);
                Par(cmd, "@an", c.AnamneseHistoria);
                Par(cmd, "@ex", c.ExameFisico);
                Par(cmd, "@hip", c.HipoteseDiagnostica);
                Par(cmd, "@con", c.CondutaPlano);
                Par(cmd, "@now", now);
                Par(cmd, "@ub", c.MedicoId);
                Par(cmd, "@id", id);
                if (cmd.Connection!.State != ConnectionState.Open)
                    await cmd.Connection.OpenAsync();
                var linhas = await cmd.ExecuteNonQueryAsync();
                if (linhas == 0) return NotFound(new { erro = "Registro não encontrado." });
                return Ok(new { id });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { erro = ex.Message });
            }
        }

        [HttpPut("{id}/estado")]
        public async Task<IActionResult> MudarEstado(long id, [FromQuery] string estado)
        {
            try
            {
                var validos = new[] { "CONCLUIDO", "CANCELADO" };
                if (!validos.Contains(estado)) return BadRequest(new { erro = "Estado inválido." });
                var conn = _context.Database.GetDbConnection();
                if (conn.State != ConnectionState.Open) await conn.OpenAsync();
                using var c1 = conn.CreateCommand();
                c1.CommandText = "SELECT estado FROM consultas_atendimentos WHERE id=@id AND is_deleted=0;";
                Par(c1, "@id", id);
                var atual = await c1.ExecuteScalarAsync();
                if (atual == null || atual == DBNull.Value)
                    return NotFound(new { erro = "Registro não encontrado." });
                if (atual.ToString() != "EM_ANDAMENTO")
                    return BadRequest(new { erro = "Consulta já finalizada." });
                using var c2 = conn.CreateCommand();
                c2.CommandText = @"UPDATE consultas_atendimentos SET estado=@e, data_fim=@now,
                    updated_at=@now, updated_by=medico_id WHERE id=@id AND is_deleted=0;";
                Par(c2, "@e", estado);
                Par(c2, "@now", DateTime.Now);
                Par(c2, "@id", id);
                await c2.ExecuteNonQueryAsync();
                return Ok(new { id, estado });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { erro = ex.Message });
            }
        }
    }
}
