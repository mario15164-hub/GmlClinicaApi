using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GmlClinicaApi.Data;
using GmlClinicaApi.Models;
using System.Data;

namespace GmlClinicaApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TriagensController : ControllerBase
    {
        private readonly AppDbContext _context;
        public TriagensController(AppDbContext context)
        {
            _context = context;
        }

        private static string? S(IDataReader r, string c)
        {
            var v = r[c];
            return v == DBNull.Value ? null : v.ToString();
        }

        private static int? I(IDataReader r, string c)
        {
            return r[c] == DBNull.Value ? null : Convert.ToInt32(r[c]);
        }

        private static decimal? D(IDataReader r, string c)
        {
            return r[c] == DBNull.Value ? null : Convert.ToDecimal(r[c]);
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

        private static decimal? CalcularImc(decimal? peso, decimal? altura)
        {
            if (peso is not > 0 || altura is not > 0) return null;
            var m = altura.Value / 100m;
            return Math.Round(peso.Value / (m * m), 1);
        }

        private static string? ValidarSignos(Triagens t)
        {
            if (t.Altura is not null)
            {
                if (t.Altura != Math.Floor(t.Altura.Value))
                    return "Altura deve ser um número inteiro entre 50 e 250 cm.";
                if (t.Altura < 50 || t.Altura > 250)
                    return "Altura deve estar entre 50 e 250 cm.";
            }
            if (t.Peso is not null && (t.Peso < 1 || t.Peso > 500))
                return "Peso deve estar entre 1 e 500 kg.";
            if (t.Temperatura is not null && (t.Temperatura < 25 || t.Temperatura > 45))
                return "Temperatura deve estar entre 25 e 45 °C.";
            if (t.SaturacaoOxigenio is not null && (t.SaturacaoOxigenio < 0 || t.SaturacaoOxigenio > 100))
                return "SpO2 deve estar entre 0 e 100%.";
            return null;
        }

        [HttpGet]
        public async Task<IActionResult> Listar()
        {
            try
            {
                var lista = new List<object>();
                using var cmd = _context.Database.GetDbConnection().CreateCommand();
                cmd.CommandText = @"SELECT t.*, p.nome_completo AS paciente, e.nome_completo AS enfermeiro
                    FROM triagens t
                    JOIN pacientes p ON p.id = t.paciente_id
                    LEFT JOIN utilizadores e ON e.id = t.enfermeiro_id
                    WHERE t.created_at >= NOW(3) - INTERVAL 1 DAY
                    ORDER BY FIELD(t.classificacao_risco,'VERMELHO_EMERGENCIA','LARANJA_MUITO_URGENTE',
                        'AMARELO_URGENTE','VERDE_POUCO_URGENTE','AZUL_NAO_URGENTE'),
                    t.created_at DESC;";
                if (cmd.Connection!.State != ConnectionState.Open)
                    await cmd.Connection.OpenAsync();
                using var r = await cmd.ExecuteReaderAsync();
                while (await r.ReadAsync())
                {
                    lista.Add(new
                    {
                        id = Convert.ToInt64(r["id"]),
                        uuid = S(r, "uuid"),
                        filaId = Convert.ToInt64(r["fila_id"]),
                        pacienteId = Convert.ToInt64(r["paciente_id"]),
                        paciente = S(r, "paciente"),
                        enfermeiroId = r["enfermeiro_id"] == DBNull.Value ? (long?)null : Convert.ToInt64(r["enfermeiro_id"]),
                        enfermeiro = S(r, "enfermeiro"),
                        pressaoSistolica = I(r, "pressao_sistolica"),
                        pressaoDiastolica = I(r, "pressao_diastolica"),
                        frequenciaCardiaca = I(r, "frequencia_cardiaca"),
                        frequenciaRespiratoria = I(r, "frequencia_respiratoria"),
                        temperatura = D(r, "temperatura"),
                        saturacaoOxigenio = I(r, "saturacao_oxigenio"),
                        peso = D(r, "peso"),
                        altura = D(r, "altura"),
                        imc = D(r, "imc"),
                        glicemiaCapilar = I(r, "glicemia_capilar"),
                        nivelConsciencia = S(r, "nivel_consciencia"),
                        classificacaoRisco = S(r, "classificacao_risco"),
                        queixaPrincipal = S(r, "queixa_principal"),
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
        public async Task<IActionResult> Criar([FromBody] Triagens t)
        {
            try
            {
                if (t.FilaId <= 0) return BadRequest(new { erro = "Informe o registro da fila." });
                var erroSignos = ValidarSignos(t);
                if (erroSignos != null) return BadRequest(new { erro = erroSignos });
                var conn = _context.Database.GetDbConnection();
                if (conn.State != ConnectionState.Open) await conn.OpenAsync();
                using var c0 = conn.CreateCommand();
                c0.CommandText = "SELECT paciente_id FROM fila_atendimento WHERE id=@f;";
                Par(c0, "@f", t.FilaId);
                var p = await c0.ExecuteScalarAsync();
                if (p == null || p == DBNull.Value)
                    return BadRequest(new { erro = "Registro da fila não encontrado." });
                var pacienteId = Convert.ToInt64(p);
                var now = DateTime.Now;
                using var c1 = conn.CreateCommand();
                c1.CommandText = @"INSERT INTO triagens
                    (uuid, fila_id, paciente_id, enfermeiro_id, pressao_sistolica, pressao_diastolica,
                    frequencia_cardiaca, frequencia_respiratoria, temperatura, saturacao_oxigenio,
                    peso, altura, imc, glicemia_capilar, nivel_consciencia, classificacao_risco,
                    queixa_principal, created_at, updated_at, created_by, updated_by)
                    VALUES (@u, @f, @pac, @e, @ps, @pd, @fcr, @frr, @t, @sao2, @peso, @alt, @imc,
                    @gc, @nc, @cr, @qp, @now, @now, @cb, @ub);
                    SELECT LAST_INSERT_ID();";
                Par(c1, "@u", Guid.NewGuid().ToString());
                Par(c1, "@f", t.FilaId);
                Par(c1, "@pac", pacienteId);
                Par(c1, "@e", t.EnfermeiroId);
                Par(c1, "@ps", t.PressaoSistolica);
                Par(c1, "@pd", t.PressaoDiastolica);
                Par(c1, "@fcr", t.FrequenciaCardiaca);
                Par(c1, "@frr", t.FrequenciaRespiratoria);
                Par(c1, "@t", t.Temperatura);
                Par(c1, "@sao2", t.SaturacaoOxigenio);
                Par(c1, "@peso", t.Peso);
                Par(c1, "@alt", t.Altura);
                Par(c1, "@imc", CalcularImc(t.Peso, t.Altura));
                Par(c1, "@gc", t.GlicemiaCapilar);
                Par(c1, "@nc", t.NivelConsciencia);
                Par(c1, "@cr", string.IsNullOrWhiteSpace(t.ClassificacaoRisco) ? "AZUL_NAO_URGENTE" : t.ClassificacaoRisco);
                Par(c1, "@qp", t.QueixaPrincipal);
                Par(c1, "@now", now);
                Par(c1, "@cb", t.EnfermeiroId);
                Par(c1, "@ub", t.EnfermeiroId);
                var id = Convert.ToInt64(await c1.ExecuteScalarAsync());
                using var c2 = conn.CreateCommand();
                c2.CommandText = "UPDATE fila_atendimento SET etapa_atual='AGUARDANDO_CONSULTA' WHERE id=@f;";
                Par(c2, "@f", t.FilaId);
                await c2.ExecuteNonQueryAsync();
                return Ok(new { id });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { erro = ex.Message });
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Editar(long id, [FromBody] Triagens t)
        {
            try
            {
                var erroSignos = ValidarSignos(t);
                if (erroSignos != null) return BadRequest(new { erro = erroSignos });
                var now = DateTime.Now;
                using var cmd = _context.Database.GetDbConnection().CreateCommand();
                cmd.CommandText = @"UPDATE triagens SET
                    enfermeiro_id=@e, pressao_sistolica=@ps, pressao_diastolica=@pd,
                    frequencia_cardiaca=@fcr, frequencia_respiratoria=@frr, temperatura=@t,
                    saturacao_oxigenio=@sao2, peso=@peso, altura=@alt, imc=@imc,
                    glicemia_capilar=@gc, nivel_consciencia=@nc, classificacao_risco=@cr,
                    queixa_principal=@qp, updated_at=@now, updated_by=@ub
                    WHERE id=@id;";
                Par(cmd, "@e", t.EnfermeiroId);
                Par(cmd, "@ps", t.PressaoSistolica);
                Par(cmd, "@pd", t.PressaoDiastolica);
                Par(cmd, "@fcr", t.FrequenciaCardiaca);
                Par(cmd, "@frr", t.FrequenciaRespiratoria);
                Par(cmd, "@t", t.Temperatura);
                Par(cmd, "@sao2", t.SaturacaoOxigenio);
                Par(cmd, "@peso", t.Peso);
                Par(cmd, "@alt", t.Altura);
                Par(cmd, "@imc", CalcularImc(t.Peso, t.Altura));
                Par(cmd, "@gc", t.GlicemiaCapilar);
                Par(cmd, "@nc", t.NivelConsciencia);
                Par(cmd, "@cr", string.IsNullOrWhiteSpace(t.ClassificacaoRisco) ? "AZUL_NAO_URGENTE" : t.ClassificacaoRisco);
                Par(cmd, "@qp", t.QueixaPrincipal);
                Par(cmd, "@now", now);
                Par(cmd, "@ub", t.EnfermeiroId);
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
    }
}
