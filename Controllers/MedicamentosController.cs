using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GmlClinicaApi.Data;
using GmlClinicaApi.Models;
using System.Data;

namespace GmlClinicaApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MedicamentosController : ControllerBase
    {
        private readonly AppDbContext _context;

        public MedicamentosController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetMedicamentos()
        {
            try
            {
                var lista = new List<Medicamentos>();

                using (var command = _context.Database.GetDbConnection().CreateCommand())
                {
                    command.CommandText = "SELECT * FROM medicamentos ORDER BY id DESC;";

                    if (command.Connection!.State != ConnectionState.Open)
                    {
                        await command.Connection.OpenAsync();
                    }

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            var m = new Medicamentos
                            {
                                Id = SafeGetLong(reader, "id"),
                                Uuid = SafeGetString(reader, "uuid"),
                                NomeGenerico = SafeGetString(reader, "nome_generico") ?? "",
                                NomeComercial = SafeGetString(reader, "nome_comercial"),
                                Categoria = SafeGetString(reader, "categoria") ?? "",
                                FormaFarmaceutica = SafeGetString(reader, "forma_farmaceutica") ?? "",
                                Concentracao = SafeGetString(reader, "concentracao") ?? "",
                                SujeitoReceitaEspecial = SafeGetBool(reader, "sujeito_receita_especial"),
                                EstoqueMinimoAlerta = SafeGetDecimal(reader, "estoque_minimo_alerta"),
                                Estado = SafeGetString(reader, "estado") ?? "ATIVO",
                                CreatedAt = SafeGetDateTime(reader, "created_at"),
                                UpdatedAt = SafeGetDateTime(reader, "updated_at")
                            };
                            lista.Add(m);
                        }
                    }
                }

                return Ok(lista);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERRO GET MEDICAMENTOS]: {ex}");
                var detalhe = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                return StatusCode(500, new { mensagem = $"Erro ao carregar medicamentos: {detalhe}" });
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetMedicamento(long id)
        {
            var result = await GetMedicamentos();
            if (result is OkObjectResult okResult && okResult.Value is List<Medicamentos> lista)
            {
                var item = lista.FirstOrDefault(m => m.Id == id);
                if (item == null) return NotFound("Medicamento não encontrado.");
                return Ok(item);
            }
            return NotFound("Medicamento não encontrado.");
        }

        [HttpPost]
        public async Task<IActionResult> PostMedicamento([FromBody] Medicamentos medicamento)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(medicamento.NomeGenerico))
                {
                    return BadRequest(new { mensagem = "O nome genérico é obrigatório." });
                }
                if (string.IsNullOrWhiteSpace(medicamento.Categoria))
                {
                    return BadRequest(new { mensagem = "A categoria é obrigatória." });
                }

                NormalizarMedicamento(medicamento);
                medicamento.Uuid = string.IsNullOrWhiteSpace(medicamento.Uuid) ? Guid.NewGuid().ToString() : medicamento.Uuid;
                medicamento.Estado = string.IsNullOrWhiteSpace(medicamento.Estado) ? "ATIVO" : medicamento.Estado;
                var now = DateTime.UtcNow;

                using (var command = _context.Database.GetDbConnection().CreateCommand())
                {
                    command.CommandText = @"
                        INSERT INTO medicamentos (
                            uuid, nome_generico, nome_comercial, categoria, forma_farmaceutica,
                            concentracao, sujeito_receita_especial, estoque_minimo_alerta, estado,
                            created_at, updated_at
                        ) VALUES (
                            @uuid, @nome_generico, @nome_comercial, @categoria, @forma_farmaceutica,
                            @concentracao, @sujeito_receita_especial, @estoque_minimo_alerta, @estado,
                            @created_at, @updated_at
                        );
                        SELECT LAST_INSERT_ID();";

                    if (command.Connection!.State != ConnectionState.Open)
                    {
                        await command.Connection.OpenAsync();
                    }

                    AddParam(command, "@uuid", medicamento.Uuid);
                    AddParam(command, "@nome_generico", medicamento.NomeGenerico);
                    AddParam(command, "@nome_comercial", medicamento.NomeComercial);
                    AddParam(command, "@categoria", medicamento.Categoria);
                    AddParam(command, "@forma_farmaceutica", medicamento.FormaFarmaceutica);
                    AddParam(command, "@concentracao", medicamento.Concentracao);
                    AddParam(command, "@sujeito_receita_especial", medicamento.SujeitoReceitaEspecial);
                    AddParam(command, "@estoque_minimo_alerta", medicamento.EstoqueMinimoAlerta);
                    AddParam(command, "@estado", medicamento.Estado);
                    AddParam(command, "@created_at", now);
                    AddParam(command, "@updated_at", now);

                    var result = await command.ExecuteScalarAsync();
                    if (result != null && result != DBNull.Value)
                    {
                        medicamento.Id = Convert.ToInt64(result);
                    }
                    medicamento.CreatedAt = now;
                    medicamento.UpdatedAt = now;
                }

                return Ok(medicamento);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERRO POST MEDICAMENTO]: {ex}");
                var detalhe = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                return StatusCode(500, new { mensagem = $"Erro ao gravar medicamento: {detalhe}" });
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> PutMedicamento(long id, [FromBody] Medicamentos medicamento)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(medicamento.NomeGenerico))
                {
                    return BadRequest(new { mensagem = "O nome genérico é obrigatório." });
                }

                NormalizarMedicamento(medicamento);
                var now = DateTime.UtcNow;

                using (var command = _context.Database.GetDbConnection().CreateCommand())
                {
                    command.CommandText = @"
                        UPDATE medicamentos SET
                            nome_generico = @nome_generico,
                            nome_comercial = @nome_comercial,
                            categoria = @categoria,
                            forma_farmaceutica = @forma_farmaceutica,
                            concentracao = @concentracao,
                            sujeito_receita_especial = @sujeito_receita_especial,
                            estoque_minimo_alerta = @estoque_minimo_alerta,
                            estado = @estado,
                            updated_at = @updated_at
                        WHERE id = @id;";

                    if (command.Connection!.State != ConnectionState.Open)
                    {
                        await command.Connection.OpenAsync();
                    }

                    AddParam(command, "@id", id);
                    AddParam(command, "@nome_generico", medicamento.NomeGenerico);
                    AddParam(command, "@nome_comercial", medicamento.NomeComercial);
                    AddParam(command, "@categoria", medicamento.Categoria);
                    AddParam(command, "@forma_farmaceutica", medicamento.FormaFarmaceutica);
                    AddParam(command, "@concentracao", medicamento.Concentracao);
                    AddParam(command, "@sujeito_receita_especial", medicamento.SujeitoReceitaEspecial);
                    AddParam(command, "@estoque_minimo_alerta", medicamento.EstoqueMinimoAlerta);
                    AddParam(command, "@estado", string.IsNullOrWhiteSpace(medicamento.Estado) ? "ATIVO" : medicamento.Estado);
                    AddParam(command, "@updated_at", now);

                    int rowsAffected = await command.ExecuteNonQueryAsync();
                    if (rowsAffected == 0)
                    {
                        return NotFound(new { mensagem = "Medicamento não encontrado." });
                    }

                    medicamento.Id = id;
                    medicamento.UpdatedAt = now;
                }

                return Ok(medicamento);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERRO PUT MEDICAMENTO]: {ex}");
                var detalhe = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                return StatusCode(500, new { mensagem = $"Erro ao atualizar medicamento: {detalhe}" });
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteMedicamento(long id)
        {
            try
            {
                var now = DateTime.UtcNow;

                using (var command = _context.Database.GetDbConnection().CreateCommand())
                {
                    command.CommandText = @"
                        UPDATE medicamentos SET
                            estado = 'DESCONTINUADO',
                            updated_at = @updated_at
                        WHERE id = @id;";

                    if (command.Connection!.State != ConnectionState.Open)
                    {
                        await command.Connection.OpenAsync();
                    }

                    AddParam(command, "@id", id);
                    AddParam(command, "@updated_at", now);

                    int rowsAffected = await command.ExecuteNonQueryAsync();
                    if (rowsAffected == 0)
                    {
                        return NotFound(new { mensagem = "Medicamento não encontrado." });
                    }
                }

                return Ok(new { mensagem = "Medicamento marcado como descontinuado." });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERRO DELETE MEDICAMENTO]: {ex}");
                var detalhe = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                return StatusCode(500, new { mensagem = $"Erro ao eliminar medicamento: {detalhe}" });
            }
        }

        private static void NormalizarMedicamento(Medicamentos m)
        {
            if (m.NomeGenerico != null) m.NomeGenerico = m.NomeGenerico.Trim();
            m.NomeComercial = string.IsNullOrWhiteSpace(m.NomeComercial) ? null : m.NomeComercial.Trim();
            m.Categoria = string.IsNullOrWhiteSpace(m.Categoria) ? m.Categoria : m.Categoria.Trim().ToUpperInvariant();
            m.FormaFarmaceutica = m.FormaFarmaceutica?.Trim() ?? "";
            m.Concentracao = m.Concentracao?.Trim() ?? "";
        }

        private static void AddParam(IDbCommand cmd, string name, object? value)
        {
            var p = cmd.CreateParameter();
            p.ParameterName = name;
            if (value == null)
            {
                p.Value = DBNull.Value;
            }
            else if (value is string s)
            {
                p.Value = string.IsNullOrWhiteSpace(s) ? DBNull.Value : s.Trim();
            }
            else if (value is DateTime dt)
            {
                p.Value = dt;
            }
            else
            {
                p.Value = value;
            }
            cmd.Parameters.Add(p);
        }

        private static long SafeGetLong(IDataReader reader, string col)
        {
            try {
                int ord = reader.GetOrdinal(col);
                if (reader.IsDBNull(ord)) return 0;
                return Convert.ToInt64(reader.GetValue(ord));
            } catch { return 0; }
        }

        private static string? SafeGetString(IDataReader reader, string col)
        {
            try {
                int ord = reader.GetOrdinal(col);
                if (reader.IsDBNull(ord)) return null;
                return reader.GetValue(ord)?.ToString();
            } catch { return null; }
        }

        private static decimal SafeGetDecimal(IDataReader reader, string col)
        {
            try {
                int ord = reader.GetOrdinal(col);
                if (reader.IsDBNull(ord)) return 0;
                return Convert.ToDecimal(reader.GetValue(ord));
            } catch { return 0; }
        }

        private static DateTime? SafeGetDateTime(IDataReader reader, string col)
        {
            try {
                int ord = reader.GetOrdinal(col);
                if (reader.IsDBNull(ord)) return null;
                var val = reader.GetValue(ord);
                if (val is DateTime dt) return dt;
                if (DateTime.TryParse(val?.ToString(), out var parsed)) return parsed;
                return null;
            } catch { return null; }
        }

        private static bool SafeGetBool(IDataReader reader, string col)
        {
            try {
                int ord = reader.GetOrdinal(col);
                if (reader.IsDBNull(ord)) return false;
                var val = reader.GetValue(ord);
                return Convert.ToBoolean(val);
            } catch { return false; }
        }
    }
}
