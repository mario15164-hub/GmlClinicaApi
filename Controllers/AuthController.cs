using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GmlClinicaApi.Data;
using GmlClinicaApi.Models;

namespace GmlClinicaApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _context;

        public AuthController(AppDbContext context)
        {
            _context = context;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] Utilizadores user)
        {
            if (await _context.Utilizadores.AnyAsync(u => u.Email == user.Email))
                return BadRequest("Email já cadastrado.");

            user.SenhaHash = BCrypt.Net.BCrypt.HashPassword(user.SenhaHash);
            user.CreatedAt = DateTime.UtcNow;

            _context.Utilizadores.Add(user);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Utilizador cadastrado com sucesso!" });
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto login)
        {
            var user = await _context.Utilizadores.FirstOrDefaultAsync(u => u.Email == login.Email);
            if (user == null || !BCrypt.Net.BCrypt.Verify(login.Senha, user.SenhaHash))
                return Unauthorized("Credenciais inválidas.");

            user.UltimoLogin = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return Ok(new { message = "Login efetuado com sucesso!", user = new { user.Id, user.NomeCompleto, user.Email } });
        }
    }

    public class LoginDto
    {
        public string Email { get; set; } = string.Empty;
        public string Senha { get; set; } = string.Empty;
    }
}
