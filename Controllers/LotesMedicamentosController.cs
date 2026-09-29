using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GmlClinicaApi.Data;
using GmlClinicaApi.Models;
using System.Data;

namespace GmlClinicaApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class LotesMedicamentosController : ControllerBase
    {
        private readonly AppDbContext _context;
        public LotesMedicamentosController(AppDbContext context)
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
                cmd.CommandText = @"SELECT l.*, m.nome_generico, f.nome_razao_social
                    FROM lotes_medicamentos l
                    JOIN medicamentos m ON m.id = l.medicamento_id
                    LEFT JOIN fornecedores f ON f.id = l.fornecedor_id
                    ORDER BY l.id DESC;";
                if (cmd.Connection!.State != ConnectionState.Open)
                    await cmd.Connection.OpenAsync();
                using var r = await cmd.ExecuteReaderAsync();
                while (await r.ReadAsync())
                {
                    lista.Add(new
                    {
                        id = Convert.ToInt64(r["id"]),
                        medicamentoId = Convert.ToInt64(r["medicamento_id"]),
                        medicamento = S(r, "nome_generico"),
                        fornecedorId = r["fornecedor_id"] == DBNull.Value ? (long?)null : Convert.ToInt64(r["fornecedor_id"]),
                        fornecedor = S(r, "nome_razao_social"),
                        numeroLote = S(r, "numero_lote"),
                        dataFabricacao = r["data_fabricacao"] == DBNull.Value ? null : Convert.ToDateTime(r["data_fabricacao"]).ToString("yyyy-MM-dd"),
                        dataValidade = Convert.ToDateTime(r["data_validade"]).ToString("yyyy-MM-dd"),
                        quantidadeInicial = Convert.ToDecimal(r["quantidade_inicial"]),
                        quantidadeAtual = Convert.ToDecimal(r["quantidade_atual"]),
                        precoCustoUnitario = Convert.ToDecimal(r["preco_custo_unitario"]),
                        precoVendaUnitario = Convert.ToDecimal(r["preco_venda_unitario"]),
                        estadoLote = S(r, "estado_lote")
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
        public async Task<IActionResult> Criar([FromBody] LotesMedicamentos l)
        {
            try
            {
                using var cmd = _context.Database.GetDbConnection().CreateCommand();
                cmd.CommandText = @"INSERT INTO lotes_medicamentos
                    (medicamento_id, fornecedor_id, numero_lote, data_fabricacao, data_validade,
                     quantidade_inicial, quantidade_atual, preco_custo_unitario, preco_venda_unitario, estado_lote)
                    VALUES (@m, @f, @n, @df, @dv, @qi, @qi, @pc, @pv, @e);
                    SELECT LAST_INSERT_ID();";
                Par(cmd, "@m", l.MedicamentoId);
                Par(cmd, "@f", l.FornecedorId);
                Par(cmd, "@n", l.NumeroLote);
                Par(cmd, "@df", l.DataFabricacao);
                Par(cmd, "@dv", l.DataValidade);
                Par(cmd, "@qi", l.QuantidadeInicial);
                Par(cmd, "@pc", l.PrecoCustoUnitario);
                Par(cmd, "@pv", l.PrecoVendaUnitario);
                Par(cmd, "@e", l.EstadoLote);
                if (cmd.Connection!.State != ConnectionState.Open)
                    await cmd.Connection.OpenAsync();
                l.Id = Convert.ToInt64(await cmd.ExecuteScalarAsync());
                return Ok(l);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { erro = ex.Message });
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Editar(long id, [FromBody] LotesMedicamentos l)
        {
            try
            {
                using var cmd = _context.Database.GetDbConnection().CreateCommand();
                cmd.CommandText = @"UPDATE lotes_medicamentos SET
                    medicamento_id=@m, fornecedor_id=@f, numero_lote=@n, data_fabricacao=@df,
                    data_validade=@dv, preco_custo_unitario=@pc, preco_venda_unitario=@pv, estado_lote=@e
                    WHERE id=@id;";
                Par(cmd, "@m", l.MedicamentoId);
                Par(cmd, "@f", l.FornecedorId);
                Par(cmd, "@n", l.NumeroLote);
                Par(cmd, "@df", l.DataFabricacao);
                Par(cmd, "@dv", l.DataValidade);
                Par(cmd, "@pc", l.PrecoCustoUnitario);
                Par(cmd, "@pv", l.PrecoVendaUnitario);
                Par(cmd, "@e", l.EstadoLote);
                Par(cmd, "@id", id);
                if (cmd.Connection!.State != ConnectionState.Open)
                    await cmd.Connection.OpenAsync();
                await cmd.ExecuteNonQueryAsync();
                l.Id = id;
                return Ok(l);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { erro = ex.Message });
            }
        }
    }
}
