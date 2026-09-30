using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GmlClinicaApi.Data;
using GmlClinicaApi.Models;
using System.Data;

namespace GmlClinicaApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AgendamentosController : ControllerBase
    {
        private readonly AppDbContext _context;
        public AgendamentosController(AppDbContext context)
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
                cmd.CommandText = @"SELECT a.*, p.nome_completo AS paciente, u.nome_completo AS medico
                    FROM agendamentos a
                    JOIN pacientes p ON p.id = a.paciente_id
                    LEFT JOIN utilizadores u ON u.id = a.medico_id
                    WHERE a.is_deleted = 0
                    ORDER BY a.data_hora_inicio DESC;";
                if (cmd.Connection!.State != ConnectionState.Open)
                    await cmd.Connection.OpenAsync();
                using var r = await cmd.ExecuteReaderAsync();
                while (await r.ReadAsync())
                {
                    lista.Add(new
                    {
                        id = Convert.ToInt64(r["id"]),
                        pacienteId = Convert.ToInt64(r["paciente_id"]),
                        paciente = S(r, "paciente"),
                        medicoId = r["medico_id"] == DBNull.Value ? (long?)null : Convert.ToInt64(r["medico_id"]),
                        medico = S(r, "medico"),
                        servicoId = r["servico_id"] == DBNull.Value ? (long?)null : Convert.ToInt64(r["servico_id"]),
                        dataHoraInicio = Convert.ToDateTime(r["data_hora_inicio"]).ToString("yyyy-MM-ddTHH:mm"),
                        dataHoraFim = Convert.ToDateTime(r["data_hora_fim"]).ToString("yyyy-MM-ddTHH:mm"),
                        tipoConsulta = S(r, "tipo_consulta"),
                        estado = S(r, "estado"),
                        motivoCancelamento = S(r, "motivo_cancelamento"),
                        observacoes = S(r, "observacoes")
                    });
                }
                return Ok(lista);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { erro = ex.Message });
            }
        }

        private async Task<bool> HaConflito(long? medicoId, DateTime ini, DateTime fim, long ignorarId)
        {
            if (medicoId == null) return false;
            using var cmd = _context.Database.GetDbConnection().CreateCommand();
            cmd.CommandText = @"SELECT COUNT(*) FROM agendamentos
                WHERE medico_id=@m AND is_deleted=0 AND id<>@id
                AND estado NOT IN ('CANCELADO','FALTOU')
                AND data_hora_inicio < @fim AND data_hora_fim > @ini;";
            Par(cmd, "@m", medicoId);
            Par(cmd, "@id", ignorarId);
            Par(cmd, "@ini", ini);
            Par(cmd, "@fim", fim);
            if (cmd.Connection!.State != ConnectionState.Open)
                await cmd.Connection.OpenAsync();
            return Convert.ToInt32(await cmd.ExecuteScalarAsync()) > 0;
        }

        [HttpPost]
        public async Task<IActionResult> Criar([FromBody] Agendamentos a)
        {
            try
            {
                if (a.DataHoraFim <= a.DataHoraInicio)
                    return BadRequest(new { erro = "A hora de fim deve ser depois da hora de início." });
                if (await HaConflito(a.MedicoId, a.DataHoraInicio, a.DataHoraFim, 0))
                    return BadRequest(new { erro = "O médico já tem um agendamento neste horário." });
                using var cmd = _context.Database.GetDbConnection().CreateCommand();
                cmd.CommandText = @"INSERT INTO agendamentos
                    (uuid, paciente_id, medico_id, servico_id, data_hora_inicio, data_hora_fim, tipo_consulta, estado, observacoes)
                    VALUES (@u, @p, @m, @s, @ini, @fim, @t, @e, @o);
                    SELECT LAST_INSERT_ID();";
                Par(cmd, "@u", Guid.NewGuid().ToString());
                Par(cmd, "@p", a.PacienteId);
                Par(cmd, "@m", a.MedicoId);
                Par(cmd, "@s", a.ServicoId);
                Par(cmd, "@ini", a.DataHoraInicio);
                Par(cmd, "@fim", a.DataHoraFim);
                Par(cmd, "@t", a.TipoConsulta);
                Par(cmd, "@e", a.Estado);
                Par(cmd, "@o", a.Observacoes);
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
        public async Task<IActionResult> Editar(long id, [FromBody] Agendamentos a)
        {
            try
            {
                if (a.DataHoraFim <= a.DataHoraInicio)
                    return BadRequest(new { erro = "A hora de fim deve ser depois da hora de início." });
                if (await HaConflito(a.MedicoId, a.DataHoraInicio, a.DataHoraFim, id))
                    return BadRequest(new { erro = "O médico já tem um agendamento neste horário." });
                using var cmd = _context.Database.GetDbConnection().CreateCommand();
                cmd.CommandText = @"UPDATE agendamentos SET
                    paciente_id=@p, medico_id=@m, servico_id=@s, data_hora_inicio=@ini, data_hora_fim=@fim,
                    tipo_consulta=@t, estado=@e, motivo_cancelamento=@mc, observacoes=@o
                    WHERE id=@id AND is_deleted=0;";
                Par(cmd, "@p", a.PacienteId);
                Par(cmd, "@m", a.MedicoId);
                Par(cmd, "@s", a.ServicoId);
                Par(cmd, "@ini", a.DataHoraInicio);
                Par(cmd, "@fim", a.DataHoraFim);
                Par(cmd, "@t", a.TipoConsulta);
                Par(cmd, "@e", a.Estado);
                Par(cmd, "@mc", a.MotivoCancelamento);
                Par(cmd, "@o", a.Observacoes);
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
        public async Task<IActionResult> Cancelar(long id, [FromQuery] string? motivo)
        {
            try
            {
                using var cmd = _context.Database.GetDbConnection().CreateCommand();
                cmd.CommandText = "UPDATE agendamentos SET estado='CANCELADO', motivo_cancelamento=@mc WHERE id=@id;";
                Par(cmd, "@mc", motivo);
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
