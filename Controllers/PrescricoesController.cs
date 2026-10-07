using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GmlClinicaApi.Data;
using GmlClinicaApi.Models;
using System.Data;

namespace GmlClinicaApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PrescricoesController : ControllerBase
    {
        private readonly AppDbContext _context;
        public PrescricoesController(AppDbContext context)
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

        private static decimal D(IDataReader r, string c)
        {
            return r[c] == DBNull.Value ? 0m : Convert.ToDecimal(r[c]);
        }

        private static void Par(System.Data.Common.DbCommand c, string n, object? v)
        {
            var p = c.CreateParameter();
            p.ParameterName = n;
            p.Value = v ?? DBNull.Value;
            c.Parameters.Add(p);
        }

        private const string SqlEstadoCalculado =
            @"CASE WHEN p.estado IN ('PENDENTE','PARCIALMENTE_DISPENSADA')
                 AND DATE_ADD(p.data_prescricao, INTERVAL p.validade_receita_dias DAY) < CURDATE()
               THEN 'EXPIRADA' ELSE p.estado END AS estado_calc";

        private async Task<string?> ValidarPedido(System.Data.Common.DbConnection conn, PrescricoesPedido p)
        {
            if (p.ConsultaId <= 0) return "Informe a consulta.";
            if (p.PacienteId <= 0) return "Informe o paciente.";
            if (p.MedicoId <= 0) return "Informe o médico.";
            if (p.ValidadeReceitaDias <= 0) return "Validade da receita inválida.";
            if (p.Itens == null || p.Itens.Count == 0)
                return "Adicione pelo menos um medicamento à prescrição.";
            foreach (var item in p.Itens)
            {
                if (item.MedicamentoId <= 0) return "Informe o medicamento de cada item.";
                if (string.IsNullOrWhiteSpace(item.PosologiaInstrucao))
                    return "Informe a posologia/instrução de cada item.";
                if (item.QuantidadePrescrita <= 0)
                    return "Quantidade prescrita deve ser maior que zero.";
            }
            using var c1 = conn.CreateCommand();
            c1.CommandText = "SELECT paciente_id FROM consultas_atendimentos WHERE id=@c AND is_deleted=0;";
            Par(c1, "@c", p.ConsultaId);
            var pac = await c1.ExecuteScalarAsync();
            if (pac == null || pac == DBNull.Value)
                return "Consulta não encontrada.";
            if (Convert.ToInt64(pac) != p.PacienteId)
                return "Paciente não corresponde à consulta.";
            using var c2 = conn.CreateCommand();
            c2.CommandText = "SELECT id FROM utilizadores WHERE id=@m AND is_deleted=0;";
            Par(c2, "@m", p.MedicoId);
            var med = await c2.ExecuteScalarAsync();
            if (med == null || med == DBNull.Value)
                return "Médico não encontrado.";
            foreach (var item in p.Itens)
            {
                using var c3 = conn.CreateCommand();
                c3.CommandText = "SELECT id FROM medicamentos WHERE id=@me;";
                Par(c3, "@me", item.MedicamentoId);
                var me = await c3.ExecuteScalarAsync();
                if (me == null || me == DBNull.Value)
                    return "Medicamento não encontrado.";
            }
            return null;
        }

        private static async Task InserirItens(System.Data.Common.DbConnection conn,
            System.Data.Common.DbTransaction tx, long prescricaoId, List<PrescricaoItemPedido> itens)
        {
            foreach (var item in itens)
            {
                using var ci = conn.CreateCommand();
                ci.Transaction = tx;
                ci.CommandText = @"INSERT INTO prescricao_itens
                    (prescricao_id, medicamento_id, posologia_instrucao, quantidade_prescrita)
                    VALUES (@pr, @me, @pos, @qp);";
                Par(ci, "@pr", prescricaoId);
                Par(ci, "@me", item.MedicamentoId);
                Par(ci, "@pos", item.PosologiaInstrucao.Trim());
                Par(ci, "@qp", item.QuantidadePrescrita);
                await ci.ExecuteNonQueryAsync();
            }
        }

        [HttpGet]
        public async Task<IActionResult> Listar()
        {
            try
            {
                var lista = new List<object>();
                using var cmd = _context.Database.GetDbConnection().CreateCommand();
                cmd.CommandText = @"SELECT p.*, pa.nome_completo AS paciente, m.nome_completo AS medico,
                    " + SqlEstadoCalculado + @",
                    COALESCE(i.total_itens, 0) AS total_itens
                    FROM prescricoes p
                    JOIN pacientes pa ON pa.id = p.paciente_id
                    LEFT JOIN utilizadores m ON m.id = p.medico_id
                    LEFT JOIN (SELECT prescricao_id, COUNT(*) AS total_itens
                               FROM prescricao_itens GROUP BY prescricao_id) i
                      ON i.prescricao_id = p.id
                    ORDER BY p.data_prescricao DESC, p.id DESC;";
                if (cmd.Connection!.State != ConnectionState.Open)
                    await cmd.Connection.OpenAsync();
                using var r = await cmd.ExecuteReaderAsync();
                while (await r.ReadAsync())
                {
                    lista.Add(new
                    {
                        id = Convert.ToInt64(r["id"]),
                        uuid = S(r, "uuid"),
                        consultaId = Convert.ToInt64(r["consulta_id"]),
                        pacienteId = Convert.ToInt64(r["paciente_id"]),
                        paciente = S(r, "paciente"),
                        medicoId = Convert.ToInt64(r["medico_id"]),
                        medico = S(r, "medico"),
                        dataPrescricao = DT(r, "data_prescricao"),
                        validadeReceitaDias = Convert.ToInt32(r["validade_receita_dias"]),
                        estado = S(r, "estado_calc"),
                        observacoesMedicas = S(r, "observacoes_medicas"),
                        totalItens = Convert.ToInt32(r["total_itens"]),
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

        [HttpGet("{id}")]
        public async Task<IActionResult> Detalhe(long id)
        {
            try
            {
                var conn = _context.Database.GetDbConnection();
                if (conn.State != ConnectionState.Open) await conn.OpenAsync();
                using var c1 = conn.CreateCommand();
                c1.CommandText = @"SELECT p.*, pa.nome_completo AS paciente, m.nome_completo AS medico,
                    " + SqlEstadoCalculado + @"
                    FROM prescricoes p
                    JOIN pacientes pa ON pa.id = p.paciente_id
                    LEFT JOIN utilizadores m ON m.id = p.medico_id
                    WHERE p.id=@id;";
                Par(c1, "@id", id);
                object? cabecalho = null;
                using (var r = await c1.ExecuteReaderAsync())
                {
                    if (await r.ReadAsync())
                    {
                        var idPrescricao = Convert.ToInt64(r["id"]);
                        var uuid = S(r, "uuid");
                        var consultaId = Convert.ToInt64(r["consulta_id"]);
                        var pacienteId = Convert.ToInt64(r["paciente_id"]);
                        var paciente = S(r, "paciente");
                        var medicoId = Convert.ToInt64(r["medico_id"]);
                        var medico = S(r, "medico");
                        var dataPrescricao = DT(r, "data_prescricao");
                        var validade = Convert.ToInt32(r["validade_receita_dias"]);
                        var estado = S(r, "estado_calc");
                        var obs = S(r, "observacoes_medicas");
                        var createdAt = DT(r, "created_at");
                        var updatedAt = DT(r, "updated_at");
                        var createdBy = r["created_by"] == DBNull.Value ? (long?)null : Convert.ToInt64(r["created_by"]);
                        var updatedBy = r["updated_by"] == DBNull.Value ? (long?)null : Convert.ToInt64(r["updated_by"]);
                        r.Close();

                        var itens = new List<object>();
                        using var c2 = conn.CreateCommand();
                        c2.CommandText = @"SELECT i.*, mm.nome_generico, mm.nome_comercial
                            FROM prescricao_itens i
                            LEFT JOIN medicamentos mm ON mm.id = i.medicamento_id
                            WHERE i.prescricao_id=@pr
                            ORDER BY i.id;";
                        Par(c2, "@pr", idPrescricao);
                        using var r2 = await c2.ExecuteReaderAsync();
                        while (await r2.ReadAsync())
                        {
                            itens.Add(new
                            {
                                id = Convert.ToInt64(r2["id"]),
                                medicamentoId = Convert.ToInt64(r2["medicamento_id"]),
                                medicamento = S(r2, "nome_generico"),
                                medicamentoComercial = S(r2, "nome_comercial"),
                                posologiaInstrucao = S(r2, "posologia_instrucao"),
                                quantidadePrescrita = D(r2, "quantidade_prescrita"),
                                quantidadeDispensada = D(r2, "quantidade_dispensada")
                            });
                        }

                        cabecalho = new
                        {
                            id = idPrescricao,
                            uuid = uuid,
                            consultaId = consultaId,
                            pacienteId = pacienteId,
                            paciente = paciente,
                            medicoId = medicoId,
                            medico = medico,
                            dataPrescricao = dataPrescricao,
                            validadeReceitaDias = validade,
                            estado = estado,
                            observacoesMedicas = obs,
                            createdAt = createdAt,
                            updatedAt = updatedAt,
                            createdBy = createdBy,
                            updatedBy = updatedBy,
                            itens = itens
                        };
                    }
                }
                if (cabecalho == null) return NotFound(new { erro = "Registro não encontrado." });
                return Ok(cabecalho);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { erro = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> Criar([FromBody] PrescricoesPedido p)
        {
            System.Data.Common.DbTransaction? tx = null;
            try
            {
                var conn = _context.Database.GetDbConnection();
                if (conn.State != ConnectionState.Open) await conn.OpenAsync();
                var erro = await ValidarPedido(conn, p);
                if (erro != null) return BadRequest(new { erro });
                tx = await conn.BeginTransactionAsync();
                var now = DateTime.Now;
                using var cmd = conn.CreateCommand();
                cmd.Transaction = tx;
                cmd.CommandText = @"INSERT INTO prescricoes
                    (uuid, consulta_id, paciente_id, medico_id, data_prescricao, validade_receita_dias,
                    estado, observacoes_medicas, created_at, updated_at, created_by, updated_by)
                    VALUES (@u, @c, @pac, @m, @now, @v, 'PENDENTE', @obs, @now, @now, @cb, @ub);
                    SELECT LAST_INSERT_ID();";
                Par(cmd, "@u", Guid.NewGuid().ToString());
                Par(cmd, "@c", p.ConsultaId);
                Par(cmd, "@pac", p.PacienteId);
                Par(cmd, "@m", p.MedicoId);
                Par(cmd, "@now", now);
                Par(cmd, "@v", p.ValidadeReceitaDias);
                Par(cmd, "@obs", string.IsNullOrWhiteSpace(p.ObservacoesMedicas) ? null : p.ObservacoesMedicas.Trim());
                Par(cmd, "@cb", p.MedicoId);
                Par(cmd, "@ub", p.MedicoId);
                var id = Convert.ToInt64(await cmd.ExecuteScalarAsync());
                await InserirItens(conn, tx, id, p.Itens);
                await tx.CommitAsync();
                return Ok(new { id });
            }
            catch (Exception ex)
            {
                if (tx != null)
                {
                    try { await tx.RollbackAsync(); } catch { }
                }
                return StatusCode(500, new { erro = ex.Message });
            }
            finally
            {
                if (tx != null) await tx.DisposeAsync();
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Editar(long id, [FromBody] PrescricoesPedido p)
        {
            System.Data.Common.DbTransaction? tx = null;
            try
            {
                var conn = _context.Database.GetDbConnection();
                if (conn.State != ConnectionState.Open) await conn.OpenAsync();
                using var c0 = conn.CreateCommand();
                c0.CommandText = @"SELECT consulta_id, paciente_id, estado, data_prescricao,
                    validade_receita_dias FROM prescricoes WHERE id=@id;";
                Par(c0, "@id", id);
                long consultaAtual = 0, pacienteAtual = 0;
                string estadoAtual = "";
                DateTime dataPrescricao = DateTime.MinValue;
                int validadeAtual = 0;
                using (var r = await c0.ExecuteReaderAsync())
                {
                    if (!await r.ReadAsync()) return NotFound(new { erro = "Registro não encontrado." });
                    consultaAtual = Convert.ToInt64(r["consulta_id"]);
                    pacienteAtual = Convert.ToInt64(r["paciente_id"]);
                    estadoAtual = r["estado"] == DBNull.Value ? "" : r["estado"].ToString() ?? "";
                    dataPrescricao = Convert.ToDateTime(r["data_prescricao"]);
                    validadeAtual = Convert.ToInt32(r["validade_receita_dias"]);
                }
                if (estadoAtual != "PENDENTE")
                    return BadRequest(new { erro = "Apenas prescrições pendentes podem ser editadas." });
                if (dataPrescricao.Date.AddDays(validadeAtual) < DateTime.Now.Date)
                    return BadRequest(new { erro = "Prescrição expirada." });
                if (p.ConsultaId != consultaAtual)
                    return BadRequest(new { erro = "A consulta não pode ser alterada." });
                if (p.PacienteId != pacienteAtual)
                    return BadRequest(new { erro = "O paciente não pode ser alterado." });
                var erro = await ValidarPedido(conn, p);
                if (erro != null) return BadRequest(new { erro });
                tx = await conn.BeginTransactionAsync();
                var now = DateTime.Now;
                using var c1 = conn.CreateCommand();
                c1.Transaction = tx;
                c1.CommandText = @"UPDATE prescricoes SET
                    medico_id=@m, validade_receita_dias=@v, observacoes_medicas=@obs,
                    updated_at=@now, updated_by=@ub
                    WHERE id=@id;";
                Par(c1, "@m", p.MedicoId);
                Par(c1, "@v", p.ValidadeReceitaDias);
                Par(c1, "@obs", string.IsNullOrWhiteSpace(p.ObservacoesMedicas) ? null : p.ObservacoesMedicas.Trim());
                Par(c1, "@now", now);
                Par(c1, "@ub", p.MedicoId);
                Par(c1, "@id", id);
                await c1.ExecuteNonQueryAsync();
                using var c2 = conn.CreateCommand();
                c2.Transaction = tx;
                c2.CommandText = "DELETE FROM prescricao_itens WHERE prescricao_id=@pr;";
                Par(c2, "@pr", id);
                await c2.ExecuteNonQueryAsync();
                await InserirItens(conn, tx, id, p.Itens);
                await tx.CommitAsync();
                return Ok(new { id });
            }
            catch (Exception ex)
            {
                if (tx != null)
                {
                    try { await tx.RollbackAsync(); } catch { }
                }
                return StatusCode(500, new { erro = ex.Message });
            }
            finally
            {
                if (tx != null) await tx.DisposeAsync();
            }
        }

        [HttpPut("{id}/estado")]
        public async Task<IActionResult> MudarEstado(long id, [FromQuery] string estado)
        {
            try
            {
                var validos = new[] { "CANCELADA" };
                if (!validos.Contains(estado)) return BadRequest(new { erro = "Estado inválido." });
                var conn = _context.Database.GetDbConnection();
                if (conn.State != ConnectionState.Open) await conn.OpenAsync();
                using var c1 = conn.CreateCommand();
                c1.CommandText = "SELECT estado FROM prescricoes WHERE id=@id;";
                Par(c1, "@id", id);
                var atual = await c1.ExecuteScalarAsync();
                if (atual == null || atual == DBNull.Value)
                    return NotFound(new { erro = "Registro não encontrado." });
                if (atual.ToString() == "CANCELADA")
                    return BadRequest(new { erro = "Prescrição já cancelada." });
                using var c2 = conn.CreateCommand();
                c2.CommandText = @"UPDATE prescricoes SET estado=@e, updated_at=@now,
                    updated_by=medico_id WHERE id=@id;";
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

    public class PrescricoesPedido
    {
        public long ConsultaId { get; set; }
        public long PacienteId { get; set; }
        public long MedicoId { get; set; }
        public int ValidadeReceitaDias { get; set; } = 30;
        public string? ObservacoesMedicas { get; set; }
        public List<PrescricaoItemPedido> Itens { get; set; } = new();
    }

    public class PrescricaoItemPedido
    {
        public long MedicamentoId { get; set; }
        public string PosologiaInstrucao { get; set; } = string.Empty;
        public decimal QuantidadePrescrita { get; set; }
    }
}
