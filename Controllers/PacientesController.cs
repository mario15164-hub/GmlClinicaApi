using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GmlClinicaApi.Data;
using GmlClinicaApi.Models;
using System.Data;

namespace GmlClinicaApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PacientesController : ControllerBase
    {
        private readonly AppDbContext _context;

        public PacientesController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetPacientes()
        {
            try
            {
                var pacientes = new List<Pacientes>();

                using (var command = _context.Database.GetDbConnection().CreateCommand())
                {
                    command.CommandText = "SELECT * FROM pacientes WHERE is_deleted = 0 OR is_deleted IS NULL ORDER BY id DESC;";
                    if (command.Connection!.State != ConnectionState.Open)
                    {
                        await command.Connection.OpenAsync();
                    }

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            var p = new Pacientes
                            {
                                Id = SafeGetLong(reader, "id"),
                                Uuid = SafeGetString(reader, "uuid"),
                                NomeCompleto = SafeGetString(reader, "nome_completo") ?? "",
                                NomeSocial = SafeGetString(reader, "nome_social"),
                                DataNascimento = SafeGetDateTime(reader, "data_nascimento"),
                                Genero = SafeGetString(reader, "genero"),
                                EstadoCivil = SafeGetString(reader, "estado_civil"),
                                NomeMae = SafeGetString(reader, "nome_mae"),
                                NomePai = SafeGetString(reader, "nome_pai"),
                                GrupoSanguineo = SafeGetString(reader, "grupo_sanguineo"),
                                Nacionalidade = SafeGetString(reader, "nacionalidade"),
                                ProvinciaResidencia = SafeGetString(reader, "provincia_residencia"),
                                MunicipioResidencia = SafeGetString(reader, "municipio_residencia"),
                                EnderecoLinha = SafeGetString(reader, "endereco_linha"),
                                TelefonePrincipal = SafeGetString(reader, "telefone_principal"),
                                TelefoneSecundario = SafeGetString(reader, "telefone_secundario"),
                                Email = SafeGetString(reader, "email"),
                                Estado = SafeGetString(reader, "estado") ?? "ATIVO",
                                CreatedAt = SafeGetDateTime(reader, "created_at"),
                                UpdatedAt = SafeGetDateTime(reader, "updated_at"),
                                IsDeleted = SafeGetBool(reader, "is_deleted")
                            };
                            pacientes.Add(p);
                        }
                    }
                }

                return Ok(pacientes);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERRO GET PACIENTES]: {ex}");
                var detalhe = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                return StatusCode(500, new { mensagem = $"Erro ao carregar pacientes: {detalhe}" });
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetPaciente(long id)
        {
            var result = await GetPacientes();
            if (result is OkObjectResult okResult && okResult.Value is List<Pacientes> lista)
            {
                var paciente = lista.FirstOrDefault(p => p.Id == id);
                if (paciente == null) return NotFound("Paciente não encontrado.");
                return Ok(paciente);
            }
            return NotFound("Paciente não encontrado.");
        }

        [HttpPost]
        public async Task<IActionResult> PostPaciente([FromBody] Pacientes paciente)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(paciente.NomeCompleto))
                {
                    return BadRequest(new { mensagem = "O nome completo é obrigatório." });
                }

                NormalizarPaciente(paciente);
                paciente.Uuid = string.IsNullOrWhiteSpace(paciente.Uuid) ? Guid.NewGuid().ToString() : paciente.Uuid;
                paciente.Estado = string.IsNullOrWhiteSpace(paciente.Estado) ? "ATIVO" : paciente.Estado;
                var now = DateTime.UtcNow;

                using (var command = _context.Database.GetDbConnection().CreateCommand())
                {
                    command.CommandText = @"
                        INSERT INTO pacientes (
                            uuid, nome_completo, nome_social, data_nascimento, genero, 
                            estado_civil, nome_mae, nome_pai, grupo_sanguineo, nacionalidade, 
                            provincia_residencia, municipio_residencia, endereco_linha, 
                            telefone_principal, telefone_secundario, email, estado, 
                            created_at, updated_at, is_deleted
                        ) VALUES (
                            @uuid, @nome_completo, @nome_social, @data_nascimento, @genero, 
                            @estado_civil, @nome_mae, @nome_pai, @grupo_sanguineo, @nacionalidade, 
                            @provincia_residencia, @municipio_residencia, @endereco_linha, 
                            @telefone_principal, @telefone_secundario, @email, @estado, 
                            @created_at, @updated_at, 0
                        );
                        SELECT LAST_INSERT_ID();";

                    if (command.Connection!.State != ConnectionState.Open)
                    {
                        await command.Connection.OpenAsync();
                    }

                    AddParam(command, "@uuid", paciente.Uuid);
                    AddParam(command, "@nome_completo", paciente.NomeCompleto);
                    AddParam(command, "@nome_social", paciente.NomeSocial);
                    AddParam(command, "@data_nascimento", paciente.DataNascimento);
                    AddParam(command, "@genero", paciente.Genero);
                    AddParam(command, "@estado_civil", paciente.EstadoCivil);
                    AddParam(command, "@nome_mae", paciente.NomeMae);
                    AddParam(command, "@nome_pai", paciente.NomePai);
                    AddParam(command, "@grupo_sanguineo", paciente.GrupoSanguineo);
                    AddParam(command, "@nacionalidade", paciente.Nacionalidade);
                    AddParam(command, "@provincia_residencia", paciente.ProvinciaResidencia);
                    AddParam(command, "@municipio_residencia", paciente.MunicipioResidencia);
                    AddParam(command, "@endereco_linha", paciente.EnderecoLinha);
                    AddParam(command, "@telefone_principal", paciente.TelefonePrincipal);
                    AddParam(command, "@telefone_secundario", paciente.TelefoneSecundario);
                    AddParam(command, "@email", paciente.Email);
                    AddParam(command, "@estado", paciente.Estado);
                    AddParam(command, "@created_at", now);
                    AddParam(command, "@updated_at", now);

                    var result = await command.ExecuteScalarAsync();
                    if (result != null && result != DBNull.Value)
                    {
                        paciente.Id = Convert.ToInt64(result);
                    }
                    paciente.CreatedAt = now;
                    paciente.UpdatedAt = now;
                    paciente.IsDeleted = false;
                }

                return Ok(paciente);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERRO POST PACIENTE]: {ex}");
                var detalhe = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                return StatusCode(500, new { mensagem = $"Erro ao gravar paciente: {detalhe}" });
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> PutPaciente(long id, [FromBody] Pacientes paciente)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(paciente.NomeCompleto))
                {
                    return BadRequest(new { mensagem = "O nome completo é obrigatório." });
                }

                NormalizarPaciente(paciente);
                var now = DateTime.UtcNow;

                using (var command = _context.Database.GetDbConnection().CreateCommand())
                {
                    command.CommandText = @"
                        UPDATE pacientes SET 
                            nome_completo = @nome_completo,
                            nome_social = @nome_social,
                            data_nascimento = @data_nascimento,
                            genero = @genero,
                            estado_civil = @estado_civil,
                            nome_mae = @nome_mae,
                            nome_pai = @nome_pai,
                            grupo_sanguineo = @grupo_sanguineo,
                            nacionalidade = @nacionalidade,
                            provincia_residencia = @provincia_residencia,
                            municipio_residencia = @municipio_residencia,
                            endereco_linha = @endereco_linha,
                            telefone_principal = @telefone_principal,
                            telefone_secundario = @telefone_secundario,
                            email = @email,
                            updated_at = @updated_at
                        WHERE id = @id AND (is_deleted = 0 OR is_deleted IS NULL);";

                    if (command.Connection!.State != ConnectionState.Open)
                    {
                        await command.Connection.OpenAsync();
                    }

                    AddParam(command, "@id", id);
                    AddParam(command, "@nome_completo", paciente.NomeCompleto);
                    AddParam(command, "@nome_social", paciente.NomeSocial);
                    AddParam(command, "@data_nascimento", paciente.DataNascimento);
                    AddParam(command, "@genero", paciente.Genero);
                    AddParam(command, "@estado_civil", paciente.EstadoCivil);
                    AddParam(command, "@nome_mae", paciente.NomeMae);
                    AddParam(command, "@nome_pai", paciente.NomePai);
                    AddParam(command, "@grupo_sanguineo", paciente.GrupoSanguineo);
                    AddParam(command, "@nacionalidade", paciente.Nacionalidade);
                    AddParam(command, "@provincia_residencia", paciente.ProvinciaResidencia);
                    AddParam(command, "@municipio_residencia", paciente.MunicipioResidencia);
                    AddParam(command, "@endereco_linha", paciente.EnderecoLinha);
                    AddParam(command, "@telefone_principal", paciente.TelefonePrincipal);
                    AddParam(command, "@telefone_secundario", paciente.TelefoneSecundario);
                    AddParam(command, "@email", paciente.Email);
                    AddParam(command, "@updated_at", now);

                    int rowsAffected = await command.ExecuteNonQueryAsync();
                    if (rowsAffected == 0)
                    {
                        return NotFound(new { mensagem = "Paciente não encontrado." });
                    }

                    paciente.Id = id;
                    paciente.UpdatedAt = now;
                }

                return Ok(paciente);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERRO PUT PACIENTE]: {ex}");
                var detalhe = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                return StatusCode(500, new { mensagem = $"Erro ao atualizar paciente: {detalhe}" });
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeletePaciente(long id)
        {
            try
            {
                var now = DateTime.UtcNow;

                using (var command = _context.Database.GetDbConnection().CreateCommand())
                {
                    command.CommandText = @"
                        UPDATE pacientes SET 
                            is_deleted = 1,
                            deleted_at = @deleted_at,
                            updated_at = @updated_at
                        WHERE id = @id AND (is_deleted = 0 OR is_deleted IS NULL);";

                    if (command.Connection!.State != ConnectionState.Open)
                    {
                        await command.Connection.OpenAsync();
                    }

                    AddParam(command, "@id", id);
                    AddParam(command, "@deleted_at", now);
                    AddParam(command, "@updated_at", now);

                    int rowsAffected = await command.ExecuteNonQueryAsync();
                    if (rowsAffected == 0)
                    {
                        return NotFound(new { mensagem = "Paciente não encontrado." });
                    }
                }

                return Ok(new { mensagem = "Paciente eliminado com sucesso." });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERRO DELETE PACIENTE]: {ex}");
                var detalhe = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                return StatusCode(500, new { mensagem = $"Erro ao eliminar paciente: {detalhe}" });
            }
        }

        private static void NormalizarPaciente(Pacientes p)
        {
            if (p.NomeCompleto != null) p.NomeCompleto = p.NomeCompleto.Trim();
            p.NomeSocial = string.IsNullOrWhiteSpace(p.NomeSocial) ? null : p.NomeSocial.Trim();
            p.Genero = string.IsNullOrWhiteSpace(p.Genero) ? null : p.Genero.Trim();
            p.EstadoCivil = string.IsNullOrWhiteSpace(p.EstadoCivil) ? null : p.EstadoCivil.Trim();
            p.NomeMae = string.IsNullOrWhiteSpace(p.NomeMae) ? null : p.NomeMae.Trim();
            p.NomePai = string.IsNullOrWhiteSpace(p.NomePai) ? null : p.NomePai.Trim();
            p.GrupoSanguineo = string.IsNullOrWhiteSpace(p.GrupoSanguineo) ? null : p.GrupoSanguineo.Trim();
            p.Nacionalidade = string.IsNullOrWhiteSpace(p.Nacionalidade) ? null : p.Nacionalidade.Trim();
            p.ProvinciaResidencia = string.IsNullOrWhiteSpace(p.ProvinciaResidencia) ? null : p.ProvinciaResidencia.Trim();
            p.MunicipioResidencia = string.IsNullOrWhiteSpace(p.MunicipioResidencia) ? null : p.MunicipioResidencia.Trim();
            p.EnderecoLinha = string.IsNullOrWhiteSpace(p.EnderecoLinha) ? null : p.EnderecoLinha.Trim();
            p.TelefonePrincipal = string.IsNullOrWhiteSpace(p.TelefonePrincipal) ? null : p.TelefonePrincipal.Trim();
            p.TelefoneSecundario = string.IsNullOrWhiteSpace(p.TelefoneSecundario) ? null : p.TelefoneSecundario.Trim();
            p.Email = string.IsNullOrWhiteSpace(p.Email) ? null : p.Email.Trim();
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
