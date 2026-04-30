using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Rtm.Data;
using Rtm.Models.Entities;

namespace Rtm.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TabsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public TabsController(AppDbContext context)
        {
            _context = context;
        }

        public record CreateTabRequest(Guid UserId, string Name);
        public record UpdateTabRequest(string Name, bool IsActive);

        [HttpGet]
        public async Task<IActionResult> GetUserTabs([FromQuery] Guid userId)
        {
            var tabs = await _context.Tabs
                .Where(t => t.UserId == userId)
                // true > false, тому OrderByDescending(t => t.IsActive) поставить активні зверху
                .OrderByDescending(t => t.IsActive)
                .ThenByDescending(t => t.CreatedAt)
                .ToListAsync();

            return Ok(tabs);
        }

        [HttpPost]
        public async Task<IActionResult> CreateTab([FromBody] CreateTabRequest request)
        {
            var tab = new Tab
            {
                Id = Guid.NewGuid(),
                UserId = request.UserId,
                Name = request.Name,
                IsActive = true
            };

            _context.Tabs.Add(tab);
            LogHistory(tab.Id, null, $"Створено нову вкладку '{tab.Name}'");

            await _context.SaveChangesAsync();
            return Ok(tab);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateTab(Guid id, [FromBody] UpdateTabRequest request)
        {
            var tab = await _context.Tabs.FindAsync(id);
            if (tab == null) return NotFound();

            tab.Name = request.Name;
            tab.IsActive = request.IsActive;

            LogHistory(tab.Id, null, $"Вкладку оновлено. Статус активності: {tab.IsActive}");

            await _context.SaveChangesAsync();
            return Ok(tab);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTab(Guid id)
        {
            var tab = await _context.Tabs.FindAsync(id);
            if (tab == null) return NotFound();

            _context.Tabs.Remove(tab);
            // Історія також буде видалена базою даних (каскадно) або залишиться сиротою. 
            await _context.SaveChangesAsync();
            return NoContent();
        }

        private void LogHistory(Guid tabId, Guid? taskId, string action)
        {
            _context.HistoryLogs.Add(new HistoryLog
            {
                Id = Guid.NewGuid(),
                TabId = tabId,
                TaskId = taskId,
                Action = action
            });
        }
    }
}