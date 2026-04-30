using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Rtm.Data;
using Rtm.Models.Entities;

namespace Rtm.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _context;

        public AuthController(AppDbContext context)
        {
            _context = context;
        }

        public record LoginRequest(string Username);

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Username))
                return BadRequest("Username не може бути порожнім.");

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == request.Username);

            if (user == null)
            {
                user = new User { Id = Guid.NewGuid(), Username = request.Username };
                _context.Users.Add(user);
                await _context.SaveChangesAsync();
            }

            // Для спрощення повертаємо самого юзера (його Id потім використовується у запитах)
            return Ok(user);
        }
    }
}