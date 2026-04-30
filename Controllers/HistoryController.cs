using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Rtm.Data;

namespace Rtm.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class HistoryController : ControllerBase
    {
        private readonly AppDbContext _context;

        public HistoryController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet("by-tab/{tabId}")]
        public async Task<IActionResult> GetTabHistory(Guid tabId)
        {
            var logs = await _context.HistoryLogs
                .Where(h => h.TabId == tabId)
                .OrderByDescending(h => h.CreatedAt) // Від найновіших до найстаріших
                .ToListAsync();

            return Ok(logs);
        }
    }
}